using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using RhythmRift;
using Shared;
using Shared.Pins;
using Shared.RhythmEngine;
using Shared.SceneLoading.Payloads;

namespace RiftVibeSolver;

[BepInPlugin("programmatic.riftVibeSolver", "RiftVibeSolver", "1.0.0.0")]
public class Plugin : BaseUnityPlugin {
    public new static ManualLogSource Logger { get; private set; }

    private static bool shouldRecordEvents;
    private static readonly List<EventData> events = new();

    private void Awake() {
        Logger = base.Logger;
        Logger.LogInfo("Loaded RiftVibeSolver");

        typeof(RRStageController).CreateMethodHook(nameof(RRStageController.BeginPlay), RRStageController_BeginPlay);
        typeof(RRStageController).CreateMethodHook(nameof(RRStageController.ShowResultsScreen), RRStageController_ShowResultsScreen);
        typeof(RRStageController).CreateMethodHook(nameof(RRStageController.VibeChainSuccess), RRStageController_VibeChainSuccess);
        typeof(RRStageController).CreateMethodHook(nameof(RRStageController.HandleHoldEnemyPerBeatHeldBonusRecorded), RRStageController_HandleHoldEnemyPerBeatHeldBonusRecorded);
        typeof(StageInputRecord).CreateMethodHook(nameof(StageInputRecord.RecordInput), StageInputRecord_RecordInput);
    }

    private static void RRStageController_BeginPlay(Action<RRStageController> beginPlay, RRStageController rrStageController) {
        beginPlay(rrStageController);

        if (rrStageController._stageScenePayload is not RhythmRiftScenePayload || !PinsController.IsPinActive("GoldenLute")) {
            shouldRecordEvents = false;

            return;
        }

        shouldRecordEvents = true;
        Logger.LogInfo($"Begin playing {rrStageController._stageFlowUiController._stageContextInfo.StageDisplayName}");
        events.Clear();
    }

    private static void RRStageController_ShowResultsScreen(Action<RRStageController, bool, float, int, bool, bool> showResultsScreen,
        RRStageController rrStageController, bool isNewHighScore, float trackProgressPercentage, int awardedDiamonds = 0,
        bool didNotFinish = false, bool cheatsDetected = false) {
        showResultsScreen(rrStageController, isNewHighScore, trackProgressPercentage, awardedDiamonds, didNotFinish, cheatsDetected);

        if (!shouldRecordEvents || didNotFinish)
            return;

        Logger.LogInfo("Completed stage");

        var activations = Solver.Solve(events, out int score);
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{rrStageController._stageFlowUiController._stageContextInfo.StageDisplayName}_Vibes.txt");
        using var writer = new StreamWriter(File.Create(path));

        foreach (var activation in activations)
            writer.WriteLine(activation.ToString());

        writer.WriteLine();
        writer.WriteLine($"Total bonus score: {score}");

        // using var writer = new BinaryWriter(File.Create(path));
        //
        // events.Sort();
        // writer.Write(events.Count);
        //
        // foreach (var eventData in events) {
        //     writer.Write((byte) eventData.EventType);
        //     writer.Write(eventData.Time);
        //     writer.Write(eventData.Beat);
        //     writer.Write((short) eventData.TotalScore);
        //     writer.Write((short) eventData.BaseScore);
        //     writer.Write((byte) eventData.ComboMultiplier);
        //     writer.Write((byte) eventData.VibeMultiplier);
        //     writer.Write((byte) eventData.PerfectBonus);
        // }
        //

        Logger.LogInfo($"Written vibe data to {path}");
        events.Clear();
        shouldRecordEvents = false;
    }

    private static void RRStageController_VibeChainSuccess(Action<RRStageController, RRStageController.VibeChain> vibeChainSuccess,
        RRStageController rrStageController, RRStageController.VibeChain vibeChain) {
        vibeChainSuccess(rrStageController, vibeChain);

        if (!shouldRecordEvents)
            return;

        var timeCapsule = rrStageController.BeatmapPlayer.FmodTimeCapsule;
        float time = timeCapsule.Time;
        float beat = timeCapsule.TrueBeatNumber;

        // Logger.LogInfo($"Gained Vibe at time {time:F}, beat {beat:F}");
        events.Add(new EventData(EventType.Vibe, time, beat, 0, 0, 1, 1, 0));
    }

    private static void StageInputRecord_RecordInput(Action<StageInputRecord, InputRating, int, float, float, float, FmodTimeCapsule, bool, bool, int> recordInput,
        StageInputRecord stageInputRecord, InputRating inputRating, int inputScore, float ratingPercent, float inputBeatNumber,
        float targetBeatNumber, FmodTimeCapsule fmodTimeCapsule, bool shouldContributeToCombo, bool wasPlayerInput, int perfectBonusScore) {
        recordInput(stageInputRecord, inputRating, inputScore, ratingPercent, inputBeatNumber, targetBeatNumber, fmodTimeCapsule, shouldContributeToCombo, wasPlayerInput, perfectBonusScore);

        if (!shouldRecordEvents || !RiftUtilityHelper.IsInputSuccess(inputRating))
            return;

        int comboMultiplier = stageInputRecord._stageScoringDefinition.GetComboMultiplier(stageInputRecord.CurrentComboCount);
        int vibeMultiplier = 1;

        if (stageInputRecord._isVibePowerActive)
            vibeMultiplier = 2;

        int totalScore = inputScore * comboMultiplier * vibeMultiplier + perfectBonusScore;
        float time = fmodTimeCapsule.Time;

        // Logger.LogInfo($"Gained {totalScore} points at time {time:F}, beat {targetBeatNumber:F}");
        events.Add(new EventData(EventType.HitPoints, time, targetBeatNumber, totalScore, inputScore, comboMultiplier, vibeMultiplier, perfectBonusScore));
    }

    private static void RRStageController_HandleHoldEnemyPerBeatHeldBonusRecorded(Action<RRStageController> handleHoldEnemyPerBeatHeldBonusRecorded, RRStageController rrStageController) {
        handleHoldEnemyPerBeatHeldBonusRecorded(rrStageController);

        if (!shouldRecordEvents)
            return;

        int score = rrStageController._stageScoringDefinition.HoldNotePerBeatHeldBonus;
        var timeCapsule = rrStageController.BeatmapPlayer.FmodTimeCapsule;
        float time = timeCapsule.Time;
        float beat = timeCapsule.TrueBeatNumber;

        // Logger.LogInfo($"Gained {score} hold points at time {time:F}, beat {beat:F}");
        events.Add(new EventData(EventType.HoldPoints, time, beat, score, score, 1, 1, 0));
    }
}