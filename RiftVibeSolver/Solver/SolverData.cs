using System.Collections.Generic;
using RiftEventCapture.Common;

namespace RiftVibeSolver;

public class SolverData {
    public readonly BeatData BeatData;
    public readonly List<Hit> Hits;

    private int[] nextVibes;

    public SolverData(CaptureResult captureResult) {
        BeatData = captureResult.BeatData;
        Hits = new List<Hit>();

        var riftEvents = captureResult.RiftEvents;

        if (riftEvents.Count == 0)
            return;

        var currentTime = new Timestamp(double.MinValue, double.MinValue);
        int currentScore = 0;
        bool currentGivesVibe = false;

        foreach (var riftEvent in riftEvents) {
            if (riftEvent.EventType is not (EventType.EnemyHit or EventType.VibeGained))
                continue;

            if (riftEvent.TargetTime.Time > currentTime.Time) {
                if (currentScore > 0 || currentGivesVibe)
                    Hits.Add(new Hit(currentTime, currentScore, currentGivesVibe));

                currentTime = riftEvent.TargetTime;
                currentScore = 0;
                currentGivesVibe = false;
            }

            switch (riftEvent.EventType) {
                case EventType.EnemyHit:
                    currentScore += riftEvent.BaseMultiplier * riftEvent.BaseScore;
                    break;
                case EventType.VibeGained:
                    currentGivesVibe = true;
                    break;
            }
        }

        if (currentScore > 0 || currentGivesVibe)
            Hits.Add(new Hit(currentTime, currentScore, currentGivesVibe));
    }

    public int GetNextVibe(int index) {
        if (nextVibes != null)
            return index < nextVibes.Length ? nextVibes[index] : nextVibes.Length;

        nextVibes = new int[Hits.Count];

        int nextVibe = Hits.Count;

        for (int i = Hits.Count - 1; i >= 0; i--) {
            if (Hits[i].GivesVibe)
                nextVibe = i;

            nextVibes[i] = nextVibe;
        }

        return index < nextVibes.Length ? nextVibes[index] : nextVibes.Length;
    }
}