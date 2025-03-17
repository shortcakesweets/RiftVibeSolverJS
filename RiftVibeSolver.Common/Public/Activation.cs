using System;

namespace RiftVibeSolver.Common;

public readonly struct Activation : IComparable<Activation> {
    public readonly double MinStartTime;
    public readonly double MaxStartTime;
    public readonly int Score;
    public readonly int VibesUsed;

    public Activation(double minStartTime, double maxStartTime, int score, int vibesUsed) {
        MinStartTime = minStartTime;
        MaxStartTime = maxStartTime;
        Score = score;
        VibesUsed = vibesUsed;
    }

    public int CompareTo(Activation other) {
        int minStartTimeComparison = MinStartTime.CompareTo(other.MinStartTime);

        if (minStartTimeComparison != 0)
            return minStartTimeComparison;

        return VibesUsed.CompareTo(other.VibesUsed);
    }
}