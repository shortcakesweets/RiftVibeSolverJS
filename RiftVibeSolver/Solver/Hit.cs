using System;
using RiftEventCapture.Common;

namespace RiftVibeSolver;

public readonly struct Hit : IComparable<Hit> {
    public readonly Timestamp Time;
    public readonly int Score;
    public readonly bool GivesVibe;

    public Hit(Timestamp time, int score, bool givesVibe) {
        Time = time;
        Score = score;
        GivesVibe = givesVibe;
    }

    public int CompareTo(Hit other) => Time.Beat.CompareTo(other.Time.Beat);
}