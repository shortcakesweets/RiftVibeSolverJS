using System;
using System.Collections.Generic;

namespace RiftVibeSolver.Common;

public readonly struct Hit : IComparable<Hit> {
    public readonly double Time;
    public readonly int Score;
    public readonly bool GivesVibe;

    public Hit(double time, int score, bool givesVibe) {
        Time = time;
        Score = score;
        GivesVibe = givesVibe;
    }

    public int CompareTo(Hit other) => Time.CompareTo(other.Time);

    public static List<Hit> MergeHits(IEnumerable<Hit> hits) {
        var newHits = new List<Hit>();
        double currentTime = double.MinValue;
        int currentScore = 0;
        bool currentGivesVibe = false;

        foreach (var hit in hits) {
            if (hit.Time > currentTime) {
                if (currentScore > 0 || currentGivesVibe)
                    newHits.Add(new Hit(currentTime, currentScore, currentGivesVibe));

                currentTime = hit.Time;
                currentScore = 0;
                currentGivesVibe = false;
            }

            currentScore += hit.Score;
            currentGivesVibe |= hit.GivesVibe;
        }

        if (currentScore > 0 || currentGivesVibe)
            newHits.Add(new Hit(currentTime, currentScore, currentGivesVibe));

        return newHits;
    }
}