namespace RiftVibeSolver.Solver;

public readonly struct Timestamp {
    public readonly double Time;
    public readonly double Beat;

    public Timestamp(double time, double beat) {
        Time = time;
        Beat = beat;
    }
}