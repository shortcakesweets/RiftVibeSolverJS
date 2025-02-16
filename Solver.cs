using System;
using System.Collections.Generic;
using UnityEngine;
using VibeOptimize;

namespace RiftVibeSolver;

public static class Solver {
    private const double VIBE_LENGTH = 5d;

    public static List<Activation> Solve(SolverData data, out int bestScore) {
        if (data.Hits.Length == 0 || data.VibeTimes.Length == 0) {
            bestScore = 0;

            return new List<Activation>();
        }

        int[] nextVibes = GetNextVibes(data);
        var activations = GetActivations(data, nextVibes);
        var strategies = GetBestStrategies(data, activations, nextVibes, out bestScore);

        return GetViableActivations(strategies);
    }

    private static int[] GetNextVibes(SolverData data) {
        var hits = data.Hits;
        var vibeTimes = data.VibeTimes;
        int[] nextVibes = new int[hits.Length];
        int nextVibe = 0;

        for (int i = 0; i < hits.Length; i++) {
            while (nextVibe < vibeTimes.Length && hits[i].Timestamp.Time > vibeTimes[nextVibe].Time)
                nextVibe++;

            nextVibes[i] = nextVibe;
        }

        return nextVibes;
    }

    private static List<Activation> GetActivations(SolverData data, int[] nextVibes) {
        var hits = data.Hits;
        var vibeTimes = data.VibeTimes;
        var singleActivationSpans = GetSpans(1);
        var doubleActivationSpans = GetSpans(2);
        var activations = new List<Activation>();

        AddActivations(singleActivationSpans, 1);
        AddActivations(doubleActivationSpans, 2);
        activations.Sort();

        return activations;

        List<ActivationSpan> GetSpans(int vibesUsed) {
            var spans = new List<ActivationSpan>();

            for (int i = 0; i < hits.Length; i++) {
                AddSpanStartingAtIndex(i);
                AddSpansEndingAtIndex(i);
            }

            spans.Sort();

            for (int i = 0; i < spans.Count - 1;) {
                if (spans[i].StartIndex == spans[i + 1].StartIndex && spans[i].EndIndex == spans[i + 1].EndIndex)
                    spans.RemoveAt(i);
                else
                    i++;
            }

            return spans;

            void AddSpanStartingAtIndex(int startIndex) {
                var startHit = hits[startIndex];
                int nextVibeIndex = GetNextVibe(nextVibes, startIndex);
                double nextVibeTime = GetVibeTime(vibeTimes, nextVibeIndex);
                double currentTime = startHit.Timestamp.Time;
                double vibeRemaining = vibesUsed * VIBE_LENGTH;

                while (vibeRemaining >= nextVibeTime - currentTime) {
                    vibeRemaining = Math.Min(vibeRemaining - (nextVibeTime - currentTime) + VIBE_LENGTH, 2d * VIBE_LENGTH);
                    currentTime = nextVibeTime;
                    nextVibeIndex++;
                    nextVibeTime = GetVibeTime(vibeTimes, nextVibeIndex);
                }

                double endTime = currentTime + vibeRemaining;
                int endIndex = startIndex;

                while (endIndex < hits.Length && hits[endIndex].Timestamp.Time < endTime)
                    endIndex++;

                spans.Add(new ActivationSpan(startHit.Timestamp, new Timestamp(endTime, GetBeatFromTime(data, endTime)), startIndex, endIndex));
            }

            void AddSpansEndingAtIndex(int endIndex) {
                var endHit = hits[endIndex];
                int previousVibeIndex = GetNextVibe(nextVibes, endIndex) - 1;
                double previousVibeTime = GetVibeTime(vibeTimes, previousVibeIndex);
                double currentTime = endHit.Timestamp.Time;
                double vibeNeeded = 0f;
                int startIndex = endIndex;

                do {
                    double startTime = currentTime - (vibesUsed * VIBE_LENGTH - vibeNeeded);

                    while (startIndex > 0 && hits[startIndex - 1].Timestamp.Time >= startTime)
                        startIndex--;

                    if (startTime > previousVibeTime && startTime <= currentTime)
                        spans.Add(new ActivationSpan(new Timestamp(startTime, GetBeatFromTime(data, startTime)), endHit.Timestamp, startIndex, endIndex));

                    double minStartTime = currentTime - (2d * VIBE_LENGTH - vibeNeeded);

                    if (minStartTime > previousVibeTime)
                        break;

                    vibeNeeded += currentTime - previousVibeTime - VIBE_LENGTH;
                    currentTime = previousVibeTime;
                    previousVibeIndex--;
                    previousVibeTime = GetVibeTime(vibeTimes, previousVibeIndex);
                } while (vibeNeeded > 0d);
            }
        }

        void AddActivations(List<ActivationSpan> spans, int vibesUsed) {
            double firstVibeTime = GetVibeTime(vibeTimes, vibesUsed - 1);

            for (int i = 1; i < spans.Count; i++) {
                var currentSpan = spans[i];

                if (currentSpan.StartTime.Time <= firstVibeTime)
                    continue;

                var previousSpan = spans[i - 1];
                int score = 0;

                for (int j = currentSpan.StartIndex; j < currentSpan.EndIndex; j++)
                    score += hits[j].Score;

                activations.Add(new Activation(
                    currentSpan.StartTime,
                    currentSpan.EndTime,
                    currentSpan.StartIndex,
                    currentSpan.EndIndex,
                    score,
                    vibesUsed,
                    currentSpan.StartTime.Time - previousSpan.StartTime.Time));
            }
        }
    }

    private static List<Strategy> GetBestStrategies(SolverData data, List<Activation> activations, int[] nextVibes, out int bestScore) {
        var vibeTimes = data.VibeTimes;
        double secondVibeTime = GetVibeTime(vibeTimes, 1);
        var strategies = new Strategy[activations.Count];
        var bestNextStrategies = new List<Strategy>();

        bestScore = 0;

        for (int i = activations.Count - 1; i >= 0; i--) {
            var activation = activations[i];
            var strategy = ComputeStrategy(activation);

            strategies[i] = strategy;

            if (activation.VibesUsed == (activation.StartTime.Time <= secondVibeTime ? 1 : 2))
                bestScore = Math.Max(bestScore, strategy.Score);
        }

        var overallBestStrategies = new List<Strategy>();

        foreach (var strategy in strategies) {
            var activation = strategy.Activation;

            if (activation.VibesUsed == (activation.StartTime.Time <= secondVibeTime ? 1 : 2) && strategy.Score == bestScore)
                overallBestStrategies.Add(strategy);
        }

        return overallBestStrategies;

        Strategy ComputeStrategy(Activation activation) {
            int firstVibeIndex = GetNextVibe(nextVibes, activation.EndIndex);
            double firstVibeTime = GetVibeTime(vibeTimes, firstVibeIndex);
            double secondVibeTime = GetVibeTime(vibeTimes, firstVibeIndex + 1);
            int bestNextScore = 0;

            for (int i = strategies.Length - 1; i >= 0; i--) {
                var nextStrategy = strategies[i];

                if (nextStrategy == null)
                    break;

                var nextActivation = nextStrategy.Activation;
                double startTime = nextActivation.StartTime.Time;

                if (startTime <= firstVibeTime)
                    break;

                if (nextActivation.VibesUsed == (startTime <= secondVibeTime ? 1 : 2))
                    bestNextScore = Math.Max(bestNextScore, nextStrategy.Score);
            }

            for (int i = strategies.Length - 1; i >= 0; i--) {
                var nextStrategy = strategies[i];

                if (nextStrategy == null)
                    break;

                var nextActivation = nextStrategy.Activation;
                double startTime = nextActivation.StartTime.Time;

                if (startTime <= firstVibeTime)
                    break;

                if (nextActivation.VibesUsed == (startTime <= secondVibeTime ? 1 : 2) && nextStrategy.Score == bestNextScore)
                    bestNextStrategies.Add(nextStrategy);
            }

            var strategy = new Strategy(activation.Score + bestNextScore, activation, bestNextStrategies.ToArray());

            bestNextStrategies.Clear();

            return strategy;
        }
    }

    private static List<Activation> GetViableActivations(List<Strategy> strategies) {
        var viableStrategies = new HashSet<Strategy>();

        foreach (var strategy in strategies)
            Recurse(strategy);

        var viableActivations = new List<Activation>();

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

    private static int GetNextVibe(int[] nextVibes, int index) => index < nextVibes.Length ? nextVibes[index] : nextVibes.Length;

    private static double GetVibeTime(Timestamp[] vibeTimes, int index) {
        if (index < 0)
            return double.MinValue;

        if (index >= vibeTimes.Length)
            return double.MaxValue;

        return vibeTimes[index].Time;
    }

    private static double GetBeatFromTime(SolverData data, double time) {
        double[] beatTimings = data.BeatTimings;

        if (beatTimings.Length <= 1)
            return time / (60d / Mathf.Max(1, data.BPM)) + 1d;

        for (int i = 0; i < beatTimings.Length - 1; i++) {
            if (time < beatTimings[i + 1])
                return i + 1 + (time - beatTimings[i]) / (beatTimings[i + 1] - beatTimings[i]);
        }

        return beatTimings.Length + (time - beatTimings[beatTimings.Length - 1]) / (beatTimings[beatTimings.Length - 1] - beatTimings[beatTimings.Length - 2]);
    }
}