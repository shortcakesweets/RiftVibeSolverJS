using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using RiftEventCapture.Common;

namespace RiftVibeSolver.Visualizer;

public class Visualizer {
    private static readonly Brush ONE_VIBE_BRUSH = new SolidBrush(Color.FromArgb(96, 160, 0));
    private static readonly Brush TWO_VIBE_BRUSH = new SolidBrush(Color.FromArgb(0, 128, 128));

    private readonly GraphicsPanel panel;
    private readonly List<Drawable> vibePathDrawables = new();

    public ActivationSpan? CurrentSpan { get; private set; }

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

        CurrentSpan = Solver.GetSpanStartingAt(data, time, vibesUsed);

        var hits = data.Hits;
        int score = 0;

        for (int i = CurrentSpan.Value.StartIndex; i < CurrentSpan.Value.EndIndex; i++)
            score += hits[i].Score;

        currentSpanLabel.SetLabel($"Beat {data.BeatData.GetBeatFromTime(CurrentSpan.Value.StartTime):F}, {score} points");

        var path = Solver.GetVibePath(data, CurrentSpan.Value, vibesUsed);
        var points = new List<PointD>();

        foreach (var segment in path) {
            points.Add(new PointD(segment.StartTime, segment.StartVibe));
            points.Add(new PointD(segment.EndTime, segment.EndVibe));
        }

        var graph = new LineGraph(0f, 1f, 10f, 0f, points);

        vibePathDrawables.Add(graph);
        panel.AddDrawable(graph);
        panel.Redraw();
    }

    private void LoadEvents(string path) {
        data = SolverData.CreateFromCaptureResult(CaptureResult.LoadFromFile(path));
        DrawEvents();
    }

    private void DrawEvents() {
        CurrentSpan = null;
        currentSpanLabel.SetLabel("");
        vibePathDrawables.Clear();
        panel.Clear();
        panel.AddDrawable(currentSpanLabel);

        var beatData = data.BeatData;

        panel.AddDrawable(new BeatGrid(0f, 1f, 7, 4, 60d / beatData.BPM, beatData.BeatTimings));

        for (int i = 0; i < data.Hits.Count; i++) {
            var hit = data.Hits[i];

            panel.AddDrawable(new HitMarker(hit.Time, 1f - hit.Score / 6660f, i, hit.GivesVibe, this));
        }

        var singleVibeActivations = Solver.GetActivations(data, 1);
        var doubleVibeActivations = Solver.GetActivations(data, 2);

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

        var bestActivations = Solver.GetBestActivations(data, singleVibeActivations, doubleVibeActivations, out int totalScore);

        panel.AddDrawable(new Label(0f, 0f, $"Optimal bonus: {totalScore}"));

        var pairs = new List<(PointD, double)>();

        foreach (var range in bestActivations)
            pairs.Add((new PointD(range.StartTime, range.Score), range.EndTime));

        panel.AddDrawable(new OptimalActivationMarkers(1f, 0f, 0f, maxScore, pairs));
        panel.Redraw();
    }
}