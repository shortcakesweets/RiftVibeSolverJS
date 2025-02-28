using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RhythmRift;
using RhythmRift.Enemies;
using RiftVibeSolver.Solver;
using Shared;
using Shared.Pins;
using Shared.RhythmEngine;
using Shared.SceneLoading.Payloads;

namespace RiftVibeSolver.Plugin;

[BepInPlugin("programmatic.riftVibeSolver", "RiftVibeSolver", "1.2.2.0")]
public class Plugin : BaseUnityPlugin {
    public new static ManualLogSource Logger { get; private set; }

    private static bool shouldRecordEvents;
    private static BeatData beatData;
    private static readonly List<Hit> hits = new();

    private void Awake() {
        Logger = base.Logger;
        Logger.LogInfo("Loaded RiftVibeSolver");

        typeof(RRStageController).CreateMethodHook(nameof(RRStageController.BeginPlay), RRStageController_BeginPlay);
        typeof(RRStageController).CreateMethodHook(nameof(RRStageController.ShowResultsScreen), RRStageController_ShowResultsScreen);
        typeof(RRStageController).CreateILHook("ProcessHitData", RRStageController_ProcessHitData_IL);
        typeof(RRStageController).CreateILHook(nameof(RRStageController.HandleKilledBoundEnemy), RRStageController_HandleKilledBoundEnemy_IL);
    }

    private static List<Hit> MergeHits() {
        var newHits = new List<Hit>();
        int currentScore = 0;
        bool currentGivesVibe = false;

        for (int i = 0; i < hits.Count; i++) {
            var hit = hits[i];

            currentScore += hit.Score;
            currentGivesVibe |= hit.GivesVibe;

            if (i < hits.Count - 1 && hit.Time.Time == hits[i + 1].Time.Time)
                continue;

            newHits.Add(new Hit(hit.Time, currentScore, currentGivesVibe));
            currentScore = 0;
            currentGivesVibe = false;
        }

        return newHits;
    }

    private static void WriteVibeData(string name, SolverData data) {
        var activations = Solver.Solver.Solve(data, out int score);
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{name}_Vibes.txt");
        using var writer = new StreamWriter(File.Create(path));

        foreach (var activation in activations)
            writer.WriteLine(activation.ToString());

        writer.WriteLine();
        writer.WriteLine($"Total bonus score: {score}");
        Logger.LogInfo($"Written vibe data to {path}");
    }

    private static void WriteEventData(string name, SolverData data) {
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{name}_Events.bin");

        data.SaveToFile(path);
        Logger.LogInfo($"Written event data to {path}");
    }

    private static bool TryGetBeatData(RRStageController rrStageController) {
        if (beatData != null)
            return true;

        var beatmap = rrStageController.BeatmapPlayer._activeBeatmap;
        float hitWindow = rrStageController.BeatmapPlayer.ActiveInputRatingsDefinition.AfterBeatHitWindow;

        if (beatmap == null)
            return false;

        beatData = new BeatData(beatmap.bpm, beatmap.beatDivisions, hitWindow, beatmap.BeatTimings.ToArray());

        return true;
    }

    private static void OnRecordInput(RRStageController rrStageController, RREnemyController.EnemyHitData hitData, int inputScore) {
        if (!shouldRecordEvents || !RiftUtilityHelper.IsInputSuccess(hitData.InputRating) || !TryGetBeatData(rrStageController))
            return;

        var stageInputRecord = rrStageController._stageInputRecord;
        int comboMultiplier = stageInputRecord._stageScoringDefinition.GetComboMultiplier(stageInputRecord.CurrentComboCount);
        var timestamp = beatData.GetTimestampFromBeat(hitData.TargetBeat);

        hits.Add(new Hit(timestamp, inputScore * comboMultiplier, false));
    }

    private static void OnVibeChainSuccessFromHit(RRStageController rrStageController, RREnemyController.EnemyHitData hitData)
        => OnVibeChainSuccess(rrStageController, hitData.TargetBeat);

    private static void OnVibeChainSuccessFromKill(RRStageController rrStageController, IRREnemyDataAccessor boundEnemy) {
        var enemy = (RREnemy) boundEnemy;

        OnVibeChainSuccess(rrStageController, enemy.TargetHitBeatNumber + Math.Max(0, enemy.EnemyLength - 1));
    }

    private static void OnVibeChainSuccess(RRStageController rrStageController, float beat) {
        if (shouldRecordEvents && TryGetBeatData(rrStageController))
            hits.Add(new Hit(beatData.GetTimestampFromBeat(beat), 0, true));
    }

    private static void RRStageController_BeginPlay(Action<RRStageController> beginPlay, RRStageController rrStageController) {
        beginPlay(rrStageController);

        if (rrStageController._stageScenePayload is not RhythmRiftScenePayload || !PinsController.IsPinActive("GoldenLute")) {
            shouldRecordEvents = false;

            return;
        }

        Logger.LogInfo($"Begin playing {rrStageController._stageFlowUiController._stageContextInfo.StageDisplayName}");
        hits.Clear();
        beatData = null;
        shouldRecordEvents = true;
    }

    private static void RRStageController_ShowResultsScreen(Action<RRStageController, bool, float, int, bool, bool> showResultsScreen,
        RRStageController rrStageController, bool isNewHighScore, float trackProgressPercentage, int awardedDiamonds = 0,
        bool didNotFinish = false, bool cheatsDetected = false) {
        showResultsScreen(rrStageController, isNewHighScore, trackProgressPercentage, awardedDiamonds, didNotFinish, cheatsDetected);

        if (!shouldRecordEvents || didNotFinish)
            return;

        Logger.LogInfo("Completed stage");
        hits.Sort();

        if (beatData != null) {
            var newHits = MergeHits();
            var data = new SolverData(beatData, newHits.ToArray());
            string name = rrStageController._stageFlowUiController._stageContextInfo.StageDisplayName;

            WriteEventData(name, data);
            WriteVibeData(name, data);
        }

        hits.Clear();
        beatData = null;
        shouldRecordEvents = false;
    }

    private static void RRStageController_ProcessHitData_IL(ILContext il) {
        var cursor = new ILCursor(il);

        cursor.GotoNext(MoveType.After, instr => instr.MatchCall<StageInputRecord>(nameof(StageInputRecord.RecordInput)));

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldloc_S, (byte) 6);
        cursor.Emit(OpCodes.Ldloc_S, (byte) 8);
        cursor.EmitCall(OnRecordInput);

        cursor.GotoNext(MoveType.After, instr => instr.MatchCall<RRStageController>(nameof(RRStageController.VibeChainSuccess)));

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldloc_S, (byte) 22);
        cursor.EmitCall(OnVibeChainSuccessFromHit);
    }

    private static void RRStageController_HandleKilledBoundEnemy_IL(ILContext il) {
        var cursor = new ILCursor(il);

        cursor.GotoNext(MoveType.After, instr => instr.MatchCall<RRStageController>(nameof(RRStageController.VibeChainSuccess)));

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldarg_1);
        cursor.EmitCall(OnVibeChainSuccessFromKill);
    }
}