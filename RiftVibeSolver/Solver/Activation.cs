using System;
using RiftEventCapture.Common;

namespace RiftVibeSolver;

public class Activation : IComparable<Activation> {
    public readonly Timestamp StartTime;
    public readonly Timestamp EndTime;
    public readonly int StartIndex;
    public readonly int EndIndex;
    public readonly int Score;
    public readonly int VibesUsed;
    public readonly double Tolerance;

    public Activation(Timestamp startTime, Timestamp endTime, int startIndex, int endIndex, int score, int vibesUsed, double tolerance) {
        Score = score;
        VibesUsed = vibesUsed;
        Tolerance = tolerance;
        StartTime = startTime;
        EndTime = endTime;
        StartIndex = startIndex;
        EndIndex = endIndex;
    }

    public override string ToString() => $"Beat {StartTime.Beat:F} -> {EndTime.Beat:F} (-{(int) (1000d * Tolerance)}ms) [{VibesUsed} vibe{(VibesUsed > 1 ? "s" : "")} -> {Score} points]";

    public int CompareTo(Activation other) {
        int timeComparison = StartTime.Time.CompareTo(other.StartTime.Time);

        if (timeComparison != 0)
            return timeComparison;

        return VibesUsed.CompareTo(other.VibesUsed);
    }
}