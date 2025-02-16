using System;
using VibeOptimize;

namespace RiftVibeSolver;

public readonly struct Hit : IComparable<Hit> {
    public readonly Timestamp Timestamp;
    public readonly int Score;

    public Hit(Timestamp timestamp, int score) {
        Timestamp = timestamp;
        Score = score;
    }

    public int CompareTo(Hit other) => Timestamp.Beat.CompareTo(other.Timestamp.Beat);
}