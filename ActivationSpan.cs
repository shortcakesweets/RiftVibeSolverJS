using System;
using VibeOptimize;

namespace RiftVibeSolver;

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

    public int CompareTo(ActivationSpan other) => StartTime.Time.CompareTo(other.StartTime.Time);
}