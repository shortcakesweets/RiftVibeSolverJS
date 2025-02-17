using System;
using System.Collections.Generic;

namespace RiftVibeSolver.Solver;

public static class Solver {
    private const double VIBE_LENGTH = 5d;

    public static List<Activation> Solve(SolverData data, out int bestScore) {
        if (data.Hits.Length == 0) {
            bestScore = 0;

            return new List<Activation>();
        }

        int[] nextVibes = GetNextVibes(data);
        var singleVibeForwardComputedSpans = GetForwardComputedSpans(data, 1);
        var singleVibeBackwardComputedSpans = GetBackwardComputedSpans(data, 1);
        var singleVibeSpans = MergeSpans(singleVibeForwardComputedSpans, singleVibeBackwardComputedSpans);
        var doubleVibeForwardComputedSpans = GetForwardComputedSpans(data, 2);
        var doubleVibeBackwardComputedSpans = GetBackwardComputedSpans(data, 2);
        var doubleVibeSpans = MergeSpans(doubleVibeForwardComputedSpans, doubleVibeBackwardComputedSpans);
        var activations = GetActivations(data, singleVibeForwardComputedSpans, doubleVibeForwardComputedSpans, nextVibes);
        var strategies = GetBestStrategies(activations, nextVibes, out bestScore);

        return GetViableActivations(strategies);
    }

    private static int[] GetNextVibes(SolverData data) {
        var hits = data.Hits;
        int[] nextVibes = new int[hits.Length];
        int nextVibe = hits.Length;

        for (int i = hits.Length - 1; i >= 0; i--) {
            if (hits[i].GivesVibe)
                nextVibe = i;

            nextVibes[i] = nextVibe;
        }

        return nextVibes;
    }

    private static List<ActivationSpan> GetForwardComputedSpans(SolverData data, int vibesUsed) {
        var hits = data.Hits;
        var spans = new List<ActivationSpan>();

        for (int startIndex = 0; startIndex < hits.Length; startIndex++) {
            var startHit = hits[startIndex];
            double currentTime = startHit.Timestamp.Time;
            double vibeRemaining = vibesUsed * VIBE_LENGTH;
            double endTime = startHit.Timestamp.Time + vibeRemaining;
            int endIndex = startIndex;

            while (endIndex < hits.Length) {
                var endHit = hits[endIndex];

                if (endHit.Timestamp.Time >= endTime)
                    break;

                if (endHit.GivesVibe) {
                    vibeRemaining = Math.Min(vibeRemaining - (endHit.Timestamp.Time - currentTime) + VIBE_LENGTH, 2d * VIBE_LENGTH);
                    currentTime = endHit.Timestamp.Time;
                    endTime = currentTime + vibeRemaining;
                }

                endIndex++;
            }

            spans.Add(new ActivationSpan(startHit.Timestamp, new Timestamp(endTime, Util.GetBeatFromTime(data.BPM, data.BeatTimings, endTime)), startIndex, endIndex));
        }

        spans.Sort();

        return spans;
    }

    private static List<ActivationSpan> GetBackwardComputedSpans(SolverData data, int vibesUsed) {
        var hits = data.Hits;
        var spans = new List<ActivationSpan>();

        for (int endIndex = 0; endIndex < hits.Length; endIndex++) {
            var endHit = hits[endIndex];
            double currentTime = endHit.Timestamp.Time;
            double vibeNeeded = 0d;
            double minStartTime = endHit.Timestamp.Time - 2d * VIBE_LENGTH;
            double startTime = endHit.Timestamp.Time - vibesUsed * VIBE_LENGTH;
            int startIndex = endIndex;

            while (true) {
                if (hits[startIndex].Timestamp.Time >= startTime && (startIndex == 0 || hits[startIndex - 1].Timestamp.Time < startTime))
                    spans.Add(new ActivationSpan(new Timestamp(startTime, Util.GetBeatFromTime(data.BPM, data.BeatTimings, startTime)), endHit.Timestamp, startIndex, endIndex));

                startIndex--;

                if (startIndex < 0 || hits[startIndex].Timestamp.Time < minStartTime)
                    break;

                var startHit = hits[startIndex];

                if (!startHit.GivesVibe)
                    continue;

                vibeNeeded = vibeNeeded + (currentTime - startHit.Timestamp.Time) - VIBE_LENGTH;
                currentTime = startHit.Timestamp.Time;
                minStartTime = currentTime - (2d * VIBE_LENGTH - vibeNeeded);
                startTime = currentTime - (vibesUsed * VIBE_LENGTH - vibeNeeded);

                if (vibeNeeded <= 0d)
                    break;
            }
        }

        spans.Sort();

        return spans;
    }

    private static List<ActivationSpan> MergeSpans(List<ActivationSpan> forwardComputedSpans, List<ActivationSpan> backwardComputedSpans) {
        var spans = new List<ActivationSpan>();
        int forwardSpanIndex = 0;
        int backwardSpanIndex = 0;

        while (forwardSpanIndex < forwardComputedSpans.Count || backwardSpanIndex < backwardComputedSpans.Count) {
            if (forwardSpanIndex == forwardComputedSpans.Count) {
                TryAddSpan(backwardComputedSpans[backwardSpanIndex]);
                backwardSpanIndex++;

                continue;
            }

            if (backwardSpanIndex == backwardComputedSpans.Count) {
                TryAddSpan(forwardComputedSpans[forwardSpanIndex]);
                forwardSpanIndex++;

                continue;
            }

            var forwardSpan = forwardComputedSpans[forwardSpanIndex];
            var backwardSpan = backwardComputedSpans[backwardSpanIndex];
            double forwardStartTime = forwardSpan.StartTime.Time;
            double backwardStartTime = backwardSpan.StartTime.Time;

            if (forwardStartTime < backwardStartTime) {
                TryAddSpan(forwardSpan);
                forwardSpanIndex++;

                continue;
            }

            if (backwardStartTime < forwardStartTime && backwardSpan.EndTime.Time <= forwardSpan.EndTime.Time)
                TryAddSpan(backwardSpan);

            backwardSpanIndex++;
        }

        spans.Sort();

        return spans;

        void TryAddSpan(ActivationSpan span) {
            if (spans.Count > 0) {
                var lastSpan = spans[spans.Count - 1];

                if (span.StartTime.Time <= lastSpan.StartTime.Time || span.EndTime.Time < lastSpan.EndTime.Time)
                    return;
            }

            spans.Add(span);
        }
    }

    private static List<Activation> GetActivations(SolverData data, List<ActivationSpan> singleVibeSpans, List<ActivationSpan> doubleVibeSpans, int[] nextVibes) {
        int firstVibeIndex = GetNextVibe(nextVibes, 0);
        int secondVibeIndex = GetNextVibe(nextVibes, firstVibeIndex + 1);
        var hits = data.Hits;
        var activations = new List<Activation>();

        AddActivations(singleVibeSpans, 1, firstVibeIndex);
        AddActivations(doubleVibeSpans, 2, secondVibeIndex);
        activations.Sort();

        return activations;

        void AddActivations(List<ActivationSpan> spans, int vibesUsed, int fromIndex) {
            for (int i = 1; i < spans.Count; i++) {
                var currentSpan = spans[i];

                if (currentSpan.StartIndex <= fromIndex)
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

    private static List<Strategy> GetBestStrategies(List<Activation> activations, int[] nextVibes, out int bestScore) {
        int firstVibeIndex = GetNextVibe(nextVibes, 0);
        int secondVibeIndex = GetNextVibe(nextVibes, firstVibeIndex + 1);
        var strategies = new Strategy[activations.Count];
        var bestNextStrategies = new List<Strategy>();

        bestScore = 0;

        for (int i = activations.Count - 1; i >= 0; i--) {
            var activation = activations[i];

            if (activation.StartIndex <= firstVibeIndex)
                break;

            var strategy = ComputeStrategy(activation);

            strategies[i] = strategy;

            if (activation.VibesUsed == (i <= secondVibeIndex ? 1 : 2))
                bestScore = Math.Max(bestScore, strategy.Score);
        }

        var overallBestStrategies = new List<Strategy>();

        for (int i = strategies.Length - 1; i >= 0; i--) {
            var strategy = strategies[i];
            var activation = strategy.Activation;

            if (activation.StartIndex <= firstVibeIndex)
                break;

            if (activation.VibesUsed == (i <= secondVibeIndex ? 1 : 2) && strategy.Score == bestScore)
                overallBestStrategies.Add(strategy);
        }

        return overallBestStrategies;

        Strategy ComputeStrategy(Activation activation) {
            int firstVibeIndex = GetNextVibe(nextVibes, activation.EndIndex);
            int secondVibeIndex = GetNextVibe(nextVibes, firstVibeIndex + 1);
            int bestNextScore = 0;

            for (int i = strategies.Length - 1; i >= 0; i--) {
                var nextStrategy = strategies[i];

                if (nextStrategy == null)
                    break;

                var nextActivation = nextStrategy.Activation;

                if (nextActivation.StartIndex <= firstVibeIndex)
                    break;

                if (nextStrategy.Activation.VibesUsed == (nextActivation.StartIndex <= secondVibeIndex ? 1 : 2))
                    bestNextScore = Math.Max(bestNextScore, nextStrategy.Score);
            }

            for (int i = strategies.Length - 1; i >= 0; i--) {
                var nextStrategy = strategies[i];

                if (nextStrategy == null)
                    break;

                var nextActivation = nextStrategy.Activation;

                if (nextActivation.StartIndex <= firstVibeIndex)
                    break;

                if (nextStrategy.Activation.VibesUsed == (i <= secondVibeIndex ? 1 : 2) && nextStrategy.Score == bestNextScore)
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
}