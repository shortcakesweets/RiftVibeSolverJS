using System;

namespace RiftVibeSolver.Solver;

public readonly struct Hit : IComparable<Hit> {
    public readonly Timestamp Timestamp;
    public readonly int Score;
    public readonly bool GivesVibe;

    public Hit(Timestamp timestamp, int score, bool givesVibe) {
        Timestamp = timestamp;
        Score = score;
        GivesVibe = givesVibe;
    }

    public int CompareTo(Hit other) => Timestamp.Beat.CompareTo(other.Timestamp.Beat);
}