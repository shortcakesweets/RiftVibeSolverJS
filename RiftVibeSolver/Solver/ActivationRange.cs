using System;

namespace RiftVibeSolver;

public readonly struct ActivationRange : IComparable<ActivationRange> {
    public readonly double StartTime;
    public readonly double EndTime;
    public readonly int Score;
    public readonly int VibesUsed;

    public ActivationRange(double startTime, double endTime, int score, int vibesUsed) {
        StartTime = startTime;
        EndTime = endTime;
        Score = score;
        VibesUsed = vibesUsed;
    }

    public int CompareTo(ActivationRange other) {
        int startTimeComparison = StartTime.CompareTo(other.StartTime);

        if (startTimeComparison != 0)
            return startTimeComparison;

        return VibesUsed.CompareTo(other.VibesUsed);
    }
}