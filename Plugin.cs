using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RhythmRift;
using Shared;
using Shared.Pins;
using Shared.RhythmEngine;
using Shared.SceneLoading.Payloads;
using VibeOptimize;

namespace RiftVibeSolver;

[BepInPlugin("programmatic.riftVibeSolver", "RiftVibeSolver", "1.1.0.0")]
public class Plugin : BaseUnityPlugin {
    public new static ManualLogSource Logger { get; private set; }

    private static bool shouldRecordEvents;
    private static Beatmap beatmap;
    private static readonly List<Hit> hits = new();
    private static readonly List<Timestamp> vibeTimes = new();

    private void Awake() {
        Logger = base.Logger;
        Logger.LogInfo("Loaded RiftVibeSolver");

        typeof(RRStageController).CreateMethodHook(nameof(RRStageController.BeginPlay), RRStageController_BeginPlay);
        typeof(RRStageController).CreateMethodHook(nameof(RRStageController.ShowResultsScreen), RRStageController_ShowResultsScreen);
        typeof(RRStageController).CreateILHook(nameof(RRStageController.ProcessHitData), RRStageController_ProcessHitData_IL);
    }

    private static void OnRecordInput(RRStageController rrStageController, RREnemyController.EnemyHitData hitData, int inputScore) {
        if (!shouldRecordEvents || !RiftUtilityHelper.IsInputSuccess(hitData.InputRating))
            return;

        var stageInputRecord = rrStageController._stageInputRecord;
        int comboMultiplier = stageInputRecord._stageScoringDefinition.GetComboMultiplier(stageInputRecord.CurrentComboCount);

        beatmap ??= new Beatmap {
            bpm = rrStageController._beatmapPlayer._activeBeatmap.bpm,
            BeatTimings = new List<double>(rrStageController._beatmapPlayer._activeBeatmap.BeatTimings)
        };

        float time = beatmap.GetTimeFromBeatNumber(hitData.TargetBeat);

        hits.Add(new Hit(new Timestamp(time, hitData.TargetBeat), inputScore * comboMultiplier));

        // Logger.LogInfo($"Gained {totalScore} points at time {time:F}, beat {targetBeatNumber:F}");
    }

    private static void OnVibeChainSuccess(RRStageController rrStageController, RREnemyController.EnemyHitData hitData) {
        if (!shouldRecordEvents)
            return;

        beatmap ??= new Beatmap {
            bpm = rrStageController._beatmapPlayer._activeBeatmap.bpm,
            BeatTimings = new List<double>(rrStageController._beatmapPlayer._activeBeatmap.BeatTimings)
        };

        float time = beatmap.GetTimeFromBeatNumber(hitData.TargetBeat);

        vibeTimes.Add(new Timestamp(time, hitData.TargetBeat));

        // Logger.LogInfo($"Gained Vibe at time {time:F}, beat {beat:F}");
    }

    private static void RRStageController_BeginPlay(Action<RRStageController> beginPlay, RRStageController rrStageController) {
        beginPlay(rrStageController);

        if (rrStageController._stageScenePayload is not RhythmRiftScenePayload || !PinsController.IsPinActive("GoldenLute")) {
            shouldRecordEvents = false;

            return;
        }

        shouldRecordEvents = true;
        Logger.LogInfo($"Begin playing {rrStageController._stageFlowUiController._stageContextInfo.StageDisplayName}");
        hits.Clear();
        vibeTimes.Clear();
    }

    private static void RRStageController_ShowResultsScreen(Action<RRStageController, bool, float, int, bool, bool> showResultsScreen,
        RRStageController rrStageController, bool isNewHighScore, float trackProgressPercentage, int awardedDiamonds = 0,
        bool didNotFinish = false, bool cheatsDetected = false) {
        showResultsScreen(rrStageController, isNewHighScore, trackProgressPercentage, awardedDiamonds, didNotFinish, cheatsDetected);

        if (!shouldRecordEvents || didNotFinish)
            return;

        Logger.LogInfo("Completed stage");

        var newHits = new List<Hit>();
        int currentScore = 0;

        for (int i = 0; i < hits.Count; i++) {
            var hit = hits[i];

            currentScore += hit.Score;

            if (i < hits.Count - 1 && hit.Timestamp.Time == hits[i + 1].Timestamp.Time)
                continue;

            newHits.Add(new Hit(hit.Timestamp, currentScore));
            currentScore = 0;
        }

        hits.Clear();

        var solverData = new SolverData(beatmap.bpm, beatmap.BeatTimings.ToArray(), newHits.ToArray(), vibeTimes.ToArray());
        var activations = Solver.Solve(solverData, out int score);
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{rrStageController._stageFlowUiController._stageContextInfo.StageDisplayName}_Vibes.txt");
        using var writer = new StreamWriter(File.Create(path));

        foreach (var activation in activations)
            writer.WriteLine(activation.ToString());

        writer.WriteLine();
        writer.WriteLine($"Total bonus score: {score}");
        Logger.LogInfo($"Written vibe data to {path}");

        // string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{rrStageController._stageFlowUiController._stageContextInfo.StageDisplayName}_Events.txt");
        // using var writer = new BinaryWriter(File.Create(path));
        //
        // writer.Write(beatmap.bpm);
        //
        // var beatTimings = beatmap.BeatTimings;
        //
        // writer.Write(beatTimings.Count);
        //
        // foreach (double time in beatTimings)
        //     writer.Write(time);
        //
        // hits.Sort();
        // writer.Write(hits.Count);
        //
        // foreach (var hit in hits) {
        //     writer.Write(hit.Timestamp.Time);
        //     writer.Write(hit.Timestamp.Beat);
        //     writer.Write(hit.Score);
        // }
        //
        // vibeTimes.Sort();
        // writer.Write(vibeTimes.Count);
        //
        // foreach (var timestamp in vibeTimes) {
        //     writer.Write(timestamp.Time);
        //     writer.Write(timestamp.Beat);
        // }
        //
        // writer.Write(hits.Count);
        //
        // Logger.LogInfo($"Written event data to {path}");

        hits.Clear();
        vibeTimes.Clear();
        beatmap = null;
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
        cursor.EmitCall(OnVibeChainSuccess);
    }
}