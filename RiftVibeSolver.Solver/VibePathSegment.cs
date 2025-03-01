using RiftEventCapture.Common;

namespace RiftVibeSolver.Solver;

public readonly struct VibePathSegment {
    public readonly Timestamp StartTime;
    public readonly Timestamp EndTime;
    public readonly double StartVibe;
    public readonly double EndVibe;

    public VibePathSegment(Timestamp startTime, Timestamp endTime, double startVibe, double endVibe) {
        StartTime = startTime;
        EndTime = endTime;
        StartVibe = startVibe;
        EndVibe = endVibe;
    }
}