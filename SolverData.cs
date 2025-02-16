using VibeOptimize;

namespace RiftVibeSolver;

public class SolverData {
    public readonly int BPM;
    public readonly double[] BeatTimings;
    public readonly Hit[] Hits;
    public readonly Timestamp[] VibeTimes;

    public SolverData(int bpm, double[] beatTimings, Hit[] hits, Timestamp[] vibeTimes) {
        BPM = bpm;
        BeatTimings = beatTimings;
        Hits = hits;
        VibeTimes = vibeTimes;
    }
}