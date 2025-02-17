using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RhythmRift;
using RiftVibeSolver.Solver;
using Shared;
using Shared.Pins;
using Shared.RhythmEngine;
using Shared.SceneLoading.Payloads;

namespace RiftVibeSolver.Plugin;

[BepInPlugin("programmatic.riftVibeSolver", "RiftVibeSolver", "1.1.0.0")]
public class Plugin : BaseUnityPlugin {
    public new static ManualLogSource Logger { get; private set; }

    private static bool shouldRecordEvents;
    private static int bpm;
    private static double[] beatTimings;
    private static readonly List<Hit> hits = new();

    private void Awake() {
        Logger = base.Logger;
        Logger.LogInfo("Loaded RiftVibeSolver");

        typeof(RRStageController).CreateMethodHook(nameof(RRStageController.BeginPlay), RRStageController_BeginPlay);
        typeof(RRStageController).CreateMethodHook(nameof(RRStageController.ShowResultsScreen), RRStageController_ShowResultsScreen);
        typeof(RRStageController).CreateILHook("ProcessHitData", RRStageController_ProcessHitData_IL);
    }

    private static List<Hit> MergeHits() {
        var newHits = new List<Hit>();
        int currentScore = 0;
        bool currentGivesVibe = false;

        for (int i = 0; i < hits.Count; i++) {
            var hit = hits[i];

            currentScore += hit.Score;
            currentGivesVibe |= hit.GivesVibe;

            if (i < hits.Count - 1 && hit.Timestamp.Time == hits[i + 1].Timestamp.Time)
                continue;

            newHits.Add(new Hit(hit.Timestamp, currentScore, currentGivesVibe));
            currentScore = 0;
            currentGivesVibe = false;
        }

        return newHits;
    }

    private static void WriteVibeData(string name, List<Hit> hits) {
        var solverData = new SolverData(bpm, beatTimings, hits.ToArray());
        var activations = Solver.Solver.Solve(solverData, out int score);
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{name}_Vibes.txt");
        using var writer = new StreamWriter(File.Create(path));

        foreach (var activation in activations)
            writer.WriteLine(activation.ToString());

        writer.WriteLine();
        writer.WriteLine($"Total bonus score: {score}");
        Logger.LogInfo($"Written vibe data to {path}");
    }

    private static void WriteEventData(string name, List<Hit> hits) {
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{name}_Events.txt");
        using var writer = new BinaryWriter(File.Create(path));

        writer.Write(bpm);
        writer.Write(beatTimings.Length);

        foreach (double time in beatTimings)
            writer.Write(time);

        writer.Write(hits.Count);

        foreach (var hit in hits) {
            writer.Write(hit.Timestamp.Time);
            writer.Write(hit.Timestamp.Beat);
            writer.Write(hit.Score);
            writer.Write(hit.GivesVibe);
        }

        Logger.LogInfo($"Written event data to {path}");
    }

    private static void OnRecordInput(RRStageController rrStageController, RREnemyController.EnemyHitData hitData, int inputScore) {
        if (!shouldRecordEvents || !RiftUtilityHelper.IsInputSuccess(hitData.InputRating))
            return;

        var stageInputRecord = rrStageController._stageInputRecord;
        int comboMultiplier = stageInputRecord._stageScoringDefinition.GetComboMultiplier(stageInputRecord.CurrentComboCount);
        var beatmap = rrStageController._beatmapPlayer._activeBeatmap;

        bpm = beatmap.bpm;
        beatTimings ??= beatmap.BeatTimings.ToArray();

        double time = Util.GetTimeFromBeat(bpm, beatTimings, hitData.TargetBeat);

        hits.Add(new Hit(new Timestamp(time, hitData.TargetBeat), inputScore * comboMultiplier, false));

        // Logger.LogInfo($"Gained {totalScore} points at time {time:F}, beat {targetBeatNumber:F}");
    }

    private static void OnVibeChainSuccess(RRStageController rrStageController, RREnemyController.EnemyHitData hitData) {
        if (!shouldRecordEvents)
            return;

        var beatmap = rrStageController._beatmapPlayer._activeBeatmap;

        bpm = beatmap.bpm;
        beatTimings ??= beatmap.BeatTimings.ToArray();

        double time = Util.GetTimeFromBeat(bpm, beatTimings, hitData.TargetBeat);

        hits.Add(new Hit(new Timestamp(time, hitData.TargetBeat), 0, true));

        // Logger.LogInfo($"Gained Vibe at time {time:F}, beat {beat:F}");
    }

    private static void RRStageController_BeginPlay(Action<RRStageController> beginPlay, RRStageController rrStageController) {
        beginPlay(rrStageController);

        if (rrStageController._stageScenePayload is not RhythmRiftScenePayload || !PinsController.IsPinActive("GoldenLute")) {
            shouldRecordEvents = false;

            return;
        }

        Logger.LogInfo($"Begin playing {rrStageController._stageFlowUiController._stageContextInfo.StageDisplayName}");
        hits.Clear();
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

        var newHits = MergeHits();
        string name = rrStageController._stageFlowUiController._stageContextInfo.StageDisplayName;

        WriteEventData(name, newHits);
        WriteVibeData(name, newHits);

        hits.Clear();
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