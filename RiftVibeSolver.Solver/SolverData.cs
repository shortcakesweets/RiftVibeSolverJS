using System.Collections.Generic;
using RiftEventCapture.Common;

namespace RiftVibeSolver.Solver;

public class SolverData {
    public readonly BeatData BeatData;
    public readonly List<Hit> Hits;

    private int[] nextVibes;

    public SolverData(CaptureResult captureResult) {
        BeatData = captureResult.BeatData;
        Hits = new List<Hit>();

        int currentScore = 0;
        bool currentGivesVibe = false;
        var riftEvents = captureResult.RiftEvents;

        for (int i = 0; i < riftEvents.Count; i++) {
            var riftEvent = riftEvents[i];

            switch (riftEvent.EventType) {
                case EventType.EnemyHit:
                    currentScore += riftEvent.BaseMultiplier * riftEvent.BaseScore;
                    break;
                case EventType.VibeGained:
                    currentGivesVibe = true;
                    break;
                default:
                    continue;
            }

            if (i < riftEvents.Count - 1 && riftEvent.TargetTime.Time == riftEvents[i + 1].TargetTime.Time)
                continue;

            Hits.Add(new Hit(riftEvent.TargetTime, currentScore, currentGivesVibe));
            currentScore = 0;
            currentGivesVibe = false;
        }
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