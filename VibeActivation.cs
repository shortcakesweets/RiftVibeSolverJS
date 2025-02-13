using System;

namespace RiftVibeSolver;

public class VibeActivation : IComparable<VibeActivation> {
    public readonly float StartBeat;
    public readonly float EndBeat;
    public readonly int EndIndex;
    public readonly int Score;
    public readonly int VibesUsed;
    public readonly float Tolerance;

    public VibeActivation(float startBeat, float endBeat, int endIndex, int score, int vibesUsed, float tolerance) {
        StartBeat = startBeat;
        EndBeat = endBeat;
        EndIndex = endIndex;
        Score = score;
        VibesUsed = vibesUsed;
        Tolerance = tolerance;
    }

    public override string ToString() => $"Beat {StartBeat:F} -> {EndBeat:F} (-{Tolerance:F}s) [{VibesUsed} vibe{(VibesUsed > 1 ? "s" : "")} -> {Score} points]";


    public int CompareTo(VibeActivation other) {
        int beatComparison = StartBeat.CompareTo(other.StartBeat);

        if (beatComparison != 0)
            return beatComparison;

        return VibesUsed.CompareTo(other.VibesUsed);
    }
}