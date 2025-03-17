using System.Collections.Generic;

namespace RiftVibeSolver.Common;

public class VibePath {
    public double StartTime { get; }
    public double EndTime { get; }
    public int StartIndex { get; }
    public int EndIndex { get; }
    public int Score { get; }
    public IReadOnlyList<VibePathSegment> Segments { get; }

    public VibePath(double startTime, double endTime, int startIndex, int endIndex, int score, IReadOnlyList<VibePathSegment> segments) {
        StartTime = startTime;
        EndTime = endTime;
        StartIndex = startIndex;
        EndIndex = endIndex;
        Score = score;
        Segments = segments;
    }
}