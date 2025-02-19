using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using RiftVibeSolver.Solver;

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

        CurrentSpan = Solver.Solver.GetSpanStartingAt(data, time, vibesUsed);

        var hits = data.Hits;
        int score = 0;

        for (int i = CurrentSpan.Value.StartIndex; i < CurrentSpan.Value.EndIndex; i++)
            score += hits[i].Score;

        currentSpanLabel.SetLabel($"Beat {CurrentSpan.Value.StartTime.Beat:F}, {score} points");

        var path = Solver.Solver.GetVibePath(data, CurrentSpan.Value, vibesUsed);
        var points = new List<PointD>();

        foreach (var segment in path) {
            points.Add(new PointD(segment.StartTime.Time, segment.StartVibe));
            points.Add(new PointD(segment.EndTime.Time, segment.EndVibe));
        }

        var graph = new LineGraph(0f, 1f, 10f, 0f, points);

        vibePathDrawables.Add(graph);
        panel.AddDrawable(graph);
        panel.Redraw();
    }

    private void LoadEvents(string path) {
        data = SolverData.LoadFromFile(path);
        DrawEvents();
    }

    private void DrawEvents() {
        CurrentSpan = null;
        currentSpanLabel.SetLabel("");
        vibePathDrawables.Clear();
        panel.Clear();
        panel.AddDrawable(currentSpanLabel);
        panel.AddDrawable(new BeatGrid(0f, 1f, 7, 4, data.BPM, data.BeatTimings));

        foreach (var hit in data.Hits)
            panel.AddDrawable(new HitMarker(hit.Time.Time, 1f - hit.Score / 6660f, hit.GivesVibe, this));

        var singleVibeActivations = Solver.Solver.GetAllActivations(data, 1);
        var doubleVibeActivations = Solver.Solver.GetAllActivations(data, 2);

        if (singleVibeActivations.Count == 0 && doubleVibeActivations.Count == 0) {
            panel.Redraw();

            return;
        }

        var allActivations = new List<Activation>(singleVibeActivations);
        int maxScore = 0;

        allActivations.AddRange(doubleVibeActivations);
        allActivations.Sort();

        foreach (var activation in allActivations)
            maxScore = Math.Max(maxScore, activation.Score);

        var points = new List<PointD>();

        points.Add(new PointD(doubleVibeActivations[0].StartTime.Time - doubleVibeActivations[0].Tolerance, doubleVibeActivations[0].Score));

        for (int i = 0; i < doubleVibeActivations.Count - 1; i++)
            points.Add(new PointD(doubleVibeActivations[i].StartTime.Time, doubleVibeActivations[i + 1].Score));

        panel.AddDrawable(new BarGraph(1f, 0f, 0f, maxScore, TWO_VIBE_BRUSH, points.ToArray()));
        points.Clear();
        points.Add(new PointD(singleVibeActivations[0].StartTime.Time - singleVibeActivations[0].Tolerance, singleVibeActivations[0].Score));

        for (int i = 0; i < singleVibeActivations.Count - 1; i++)
            points.Add(new PointD(singleVibeActivations[i].StartTime.Time, singleVibeActivations[i + 1].Score));

        panel.AddDrawable(new BarGraph(1f, 0f, 0f, maxScore, ONE_VIBE_BRUSH, points.ToArray()));

        var bestActivations = Solver.Solver.GetBestActivations(data, allActivations, out int totalScore);

        panel.AddDrawable(new Label(0f, 0f, $"Optimal bonus: {totalScore}"));

        var pairs = new List<(PointD, double)>();

        foreach (var activation in bestActivations)
            pairs.Add((new PointD(activation.StartTime.Time, activation.Score), activation.Tolerance));

        panel.AddDrawable(new OptimalActivationMarkers(1f, 0f, 0f, maxScore, pairs));
        panel.Redraw();
    }
}