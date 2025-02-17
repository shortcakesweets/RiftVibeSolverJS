namespace RiftVibeSolver.Solver;

public class SolverData {
    public readonly int BPM;
    public readonly double[] BeatTimings;
    public readonly Hit[] Hits;

    public SolverData(int bpm, double[] beatTimings, Hit[] hits) {
        BPM = bpm;
        BeatTimings = beatTimings;
        Hits = hits;
    }
}