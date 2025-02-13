using System;

namespace RiftVibeSolver;

public readonly struct Hit : IComparable<Hit> {
    public readonly float Time;

    public readonly float Beat;

    public readonly int Score;

    public readonly bool GivesVibe;

    public Hit(float time, float beat, int score, bool givesVibe) {
        Time = time;
        Beat = beat;
        Score = score;
        GivesVibe = givesVibe;
    }

    public int CompareTo(Hit other) => Time.CompareTo(other.Time);
}