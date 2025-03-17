using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using RiftEventCapture.Common;
using RiftVibeSolver.Common;

namespace RiftVibeSolver.Visualizer;

public class Visualizer {
    private static readonly Brush ONE_VIBE_BRUSH = new SolidBrush(Color.FromArgb(96, 160, 0));
    private static readonly Brush TWO_VIBE_BRUSH = new SolidBrush(Color.FromArgb(0, 128, 128));

    private readonly GraphicsPanel panel;
    private readonly List<Drawable> vibePathDrawables = new();

    public VibePath CurrentPath { get; private set; }

    private readonly OpenFileDialog openFileDialog;
    private readonly Label currentSpanLabel = new(0f, 20f, "");

    private SolverData data;

    public Visualizer(GraphicsPanel panel) {
        this.panel = panel;
        openFileDialog = new OpenFileDialog();
        panel.OnClick += (time, value) => DrawVibePath(time, value > 0.5f ? 1 : 2);
        panel.OnEnter += ShowFileDialog;
        ShowFileDialog();
    }

    private void ShowFileDialog() {
        if (openFileDialog.ShowDialog() == DialogResult.OK)
            LoadEvents(openFileDialog.FileName);
    }

    private void DrawVibePath(double time, int vibesUsed) {
        foreach (var drawable in vibePathDrawables)
            panel.RemoveDrawable(drawable);

        vibePathDrawables.Clear();

        if (data == null)
            return;

        CurrentPath = Solver.GetVibePath(data, time, vibesUsed);
        currentSpanLabel.SetLabel($"Beat {data.GetBeatFromTime(CurrentPath.StartTime):F}, {CurrentPath.Score} points");

        var points = new List<PointD>();

        foreach (var segment in CurrentPath.Segments) {
            points.Add(new PointD(segment.StartTime, segment.StartVibe));
            points.Add(new PointD(segment.EndTime, segment.EndVibe));
        }

        var graph = new LineGraph(0f, 1f, 10f, 0f, points);

        vibePathDrawables.Add(graph);
        panel.AddDrawable(graph);
        panel.Redraw();
    }

    private void LoadEvents(string path) {
        var captureResult = CaptureResult.LoadFromFile(path);
        var beatData = captureResult.BeatData;

        data = new SolverData(beatData.BPM, beatData.BeatDivisions, new List<double>(beatData.BeatTimings), Hit.MergeHits(GetHitsFromCaptureResult(captureResult)));
        DrawEvents();
    }

    private void DrawEvents() {
        CurrentPath = null;
        currentSpanLabel.SetLabel("");
        vibePathDrawables.Clear();
        panel.Clear();
        panel.AddDrawable(currentSpanLabel);
        panel.AddDrawable(new BeatGrid(0f, 1f, 7, 4, 60d / data.BPM, data.BeatTimings));

        for (int i = 0; i < data.Hits.Count; i++) {
            var hit = data.Hits[i];

            panel.AddDrawable(new HitMarker(hit.Time, 1f - hit.Score / 6660f, i, hit.GivesVibe, this));
        }

        var result = Solver.Solve(data);
        var singleVibeActivations = result.SingleVibeActivations;
        var doubleVibeActivations = result.DoubleVibeActivations;

        if (singleVibeActivations.Count == 0 && doubleVibeActivations.Count == 0) {
            panel.Redraw();

            return;
        }

        int maxScore = 0;

        foreach (var activation in singleVibeActivations)
            maxScore = Math.Max(maxScore, activation.Score);

        foreach (var activation in doubleVibeActivations)
            maxScore = Math.Max(maxScore, activation.Score);

        var points = new List<PointD>();

        foreach (var activation in doubleVibeActivations)
            points.Add(new PointD(activation.MinStartTime, activation.Score));

        panel.AddDrawable(new BarGraph(1f, 0f, 0f, maxScore, TWO_VIBE_BRUSH, points.ToArray()));
        points.Clear();

        foreach (var activation in singleVibeActivations)
            points.Add(new PointD(activation.MinStartTime, activation.Score));

        panel.AddDrawable(new BarGraph(1f, 0f, 0f, maxScore, ONE_VIBE_BRUSH, points.ToArray()));
        panel.AddDrawable(new Label(0f, 0f, $"Optimal bonus: {result.TotalScore}"));

        var pairs = new List<(PointD, double)>();
        var bestSingleVibeActivations = result.BestSingleVibeActivations;
        var bestDoubleVibeActivations = result.BestDoubleVibeActivations;
        var allBestActivations = new List<Activation>(bestSingleVibeActivations.Count + bestDoubleVibeActivations.Count);

        allBestActivations.AddRange(bestSingleVibeActivations);
        allBestActivations.AddRange(bestDoubleVibeActivations);
        allBestActivations.Sort();

        foreach (var activation in allBestActivations)
            pairs.Add((new PointD(activation.MinStartTime, activation.Score), activation.MaxStartTime));

        panel.AddDrawable(new OptimalActivationMarkers(1f, 0f, 0f, maxScore, pairs));
        panel.Redraw();
    }

    private static IEnumerable<Hit> GetHitsFromCaptureResult(CaptureResult captureResult) {
        foreach (var riftEvent in captureResult.RiftEvents) {
            if (riftEvent.EventType is not (EventType.EnemyHit or EventType.VibeGained))
                continue;

            switch (riftEvent.EventType) {
                case EventType.EnemyHit:
                    yield return new Hit(riftEvent.TargetTime.Time, riftEvent.BaseMultiplier * riftEvent.BaseScore, false);
                    break;
                case EventType.VibeGained:
                    yield return new Hit(riftEvent.TargetTime.Time, 0, true);
                    break;
            }
        }
    }
}