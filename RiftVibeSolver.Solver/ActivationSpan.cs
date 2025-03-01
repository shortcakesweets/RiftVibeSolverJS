using System;
using RiftEventCapture.Common;

namespace RiftVibeSolver.Solver;

public readonly struct ActivationSpan : IComparable<ActivationSpan> {
    public readonly Timestamp StartTime;
    public readonly Timestamp EndTime;
    public readonly int StartIndex;
    public readonly int EndIndex;

    public ActivationSpan(Timestamp startTime, Timestamp endTime, int startIndex, int endIndex) {
        StartTime = startTime;
        EndTime = endTime;
        StartIndex = startIndex;
        EndIndex = endIndex;
    }

    public int CompareTo(ActivationSpan other) {
        int startComparison = StartTime.Time.CompareTo(other.StartTime.Time);

        if (startComparison != 0)
            return startComparison;

        return EndTime.Time.CompareTo(other.EndTime.Time);
    }
}