using System;
using System.Collections.Generic;

namespace RiftVibeSolver;

public static class Solver {
    private const float VIBE_LENGTH = 5f;

    public static List<VibeActivation> Solve(List<EventData> events, out int bestScore) {
        var hits = GetHits(events);

        if (hits.Count == 0) {
            bestScore = 0;

            return new List<VibeActivation>();
        }

        int[] nextVibes = GetNextVibes(hits);
        var activations = GetActivations(hits);
        var strategies = GetBestStrategies(hits, activations, nextVibes, out bestScore);

        return GetViableActivations(strategies);
    }

    private static List<Hit> GetHits(List<EventData> events) {
        var hits = new List<Hit>();
        var vibeTimes = new List<float>();

        foreach (var data in events) {
            if (data.EventType == EventType.HitPoints)
                hits.Add(new Hit(data.Time, data.Beat, data.BaseScore * data.ComboMultiplier, false));
            else if (data.EventType == EventType.Vibe)
                vibeTimes.Add(data.Time);
        }

        hits.Sort();
        vibeTimes.Sort();

        var newHits = new List<Hit>();
        int nextVibe = 0;
        int currentScore = 0;

        for (int i = 0; i < hits.Count; i++) {
            var hit = hits[i];

            currentScore += hit.Score;

            if (i < hits.Count - 1 && hit.Time == hits[i + 1].Time)
                continue;

            bool givesVibe = nextVibe < vibeTimes.Count && hit.Time >= vibeTimes[nextVibe];

            if (givesVibe)
                nextVibe++;

            newHits.Add(new Hit(hit.Time, hit.Beat, currentScore, givesVibe));
            currentScore = 0;
        }

        return newHits;
    }

    private static int[] GetNextVibes(List<Hit> hits) {
        int[] nextVibes = new int[hits.Count + 1];
        int nextVibe = hits.Count;

        nextVibes[hits.Count] = hits.Count;

        for (int i = hits.Count - 1; i >= 0; i--) {
            if (hits[i].GivesVibe)
                nextVibe = i;

            nextVibes[i] = nextVibe;
        }

        return nextVibes;
    }

    private static VibeActivation[,] GetActivations(List<Hit> hits) {
        var activations = new VibeActivation[hits.Count, 2];
        var foundVibeTimes = new List<float>();

        for (int i = 0; i < hits.Count; i++) {
            activations[i, 0] = GetActivation(i, 1);
            activations[i, 1] = GetActivation(i, 2);
        }

        return activations;

        VibeActivation GetActivation(int hitIndex, int vibesUsed) {
            var startHit = hits[hitIndex];
            float endTime = startHit.Time + vibesUsed * VIBE_LENGTH;
            int endIndex = hitIndex;
            int totalScore = 0;

            while (endIndex < hits.Count && hits[endIndex].Time <= endTime) {
                var endHit = hits[endIndex];

                totalScore += endHit.Score;

                if (endHit.GivesVibe) {
                    endTime = Math.Min(endTime + VIBE_LENGTH, endHit.Time + 2f * VIBE_LENGTH);
                    foundVibeTimes.Add(endHit.Time);
                }

                endIndex++;
            }

            float startTime = hits[endIndex - 1].Time - vibesUsed * VIBE_LENGTH;

            for (int i = foundVibeTimes.Count - 1; i >= 0; i--)
                startTime = Math.Max(foundVibeTimes[i] - vibesUsed * VIBE_LENGTH, startTime - VIBE_LENGTH);

            startTime = Math.Max(hitIndex > 0 ? hits[hitIndex - 1].Time : 0f, startTime);
            foundVibeTimes.Clear();

            return new VibeActivation(startHit.Beat, hits[endIndex - 1].Beat, endIndex, totalScore, vibesUsed, startHit.Time - startTime);
        }
    }

    private static List<Strategy> GetBestStrategies(List<Hit> hits, VibeActivation[,] activations, int[] nextVibes, out int bestScore) {
        int firstVibeIndex = nextVibes[0];
        int secondVibeIndex = firstVibeIndex < hits.Count ? nextVibes[firstVibeIndex + 1] : hits.Count;
        var strategies = new Strategy[hits.Count, 2];
        var bestNextStrategies = new List<Strategy>();

        bestScore = 0;

        for (int i = hits.Count - 1; i > firstVibeIndex; i--) {
            bestScore = Math.Max(bestScore, ComputeStrategy(i, 1));

            if (i > secondVibeIndex)
                bestScore = Math.Max(bestScore, ComputeStrategy(i, 2));
        }

        var overallBestStrategies = new List<Strategy>();

        for (int i = firstVibeIndex + 1; i < hits.Count; i++) {
            if (strategies[i, 0].Score == bestScore)
                overallBestStrategies.Add(strategies[i, 0]);

            if (i > secondVibeIndex && strategies[i, 1].Score == bestScore)
                overallBestStrategies.Add(strategies[i, 1]);
        }

        return overallBestStrategies;

        int ComputeStrategy(int hitIndex, int vibesUsed) {
            var activation = activations[hitIndex, vibesUsed - 1];
            int firstVibeIndex = nextVibes[activation.EndIndex];
            int secondVibeIndex = firstVibeIndex < hits.Count ? nextVibes[firstVibeIndex + 1] : hits.Count;
            int bestNextScore = 0;

            for (int i = firstVibeIndex + 1; i < hits.Count; i++) {
                bestNextScore = Math.Max(bestNextScore, strategies[i, 0].Score);

                if (i > secondVibeIndex)
                    bestNextScore = Math.Max(bestNextScore, strategies[i, 1].Score);
            }

            for (int i = firstVibeIndex + 1; i < hits.Count; i++) {
                if (strategies[i, 0].Score == bestNextScore)
                    bestNextStrategies.Add(strategies[i, 0]);

                if (i > secondVibeIndex && strategies[i, 1].Score == bestNextScore)
                    bestNextStrategies.Add(strategies[i, 1]);
            }

            int score = activation.Score + bestNextScore;
            var strategy = new Strategy(score, activation, bestNextStrategies.ToArray());

            bestNextStrategies.Clear();
            strategies[hitIndex, vibesUsed - 1] = strategy;

            return score;
        }
    }

    private static List<VibeActivation> GetViableActivations(List<Strategy> strategies) {
        var viableStrategies = new HashSet<Strategy>();

        foreach (var strategy in strategies)
            Recurse(strategy);

        var viableActivations = new List<VibeActivation>();

        foreach (var strategy in viableStrategies)
            viableActivations.Add(strategy.Activation);

        viableActivations.Sort();

        return viableActivations;

        void Recurse(Strategy strategy) {
            if (viableStrategies.Contains(strategy))
                return;

            viableStrategies.Add(strategy);

            foreach (var nextStrategy in strategy.NextStrategies)
                Recurse(nextStrategy);
        }
    }
}