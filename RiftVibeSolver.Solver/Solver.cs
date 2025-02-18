using System;
using System.Collections.Generic;

namespace RiftVibeSolver.Solver;

public static class Solver {
    private const double VIBE_LENGTH = 5d;

    public static List<Activation> Solve(SolverData data, out int totalScore) {
        if (data.Hits.Length == 0) {
            totalScore = 0;

            return new List<Activation>();
        }

        var activations = GetAllActivations(data, 1);

        activations.AddRange(GetAllActivations(data, 2));
        activations.Sort();

        var strategies = GetBestStrategies(data, activations, out totalScore);

        return GetViableActivations(strategies);
    }

    public static List<Activation> GetAllActivations(SolverData data, int vibesUsed) {
        var forwardComputedSpans = GetForwardComputedSpans(data, vibesUsed);
        var backwardComputedSpans = GetBackwardComputedSpans(data, vibesUsed);
        var spans = MergeSpans(forwardComputedSpans, backwardComputedSpans);

        return GetActivations(data, spans, vibesUsed);
    }

    public static List<Activation> GetBestActivations(SolverData data, List<Activation> activations, out int totalScore) {
        var strategies = GetBestStrategies(data, activations, out totalScore);

        return GetViableActivations(strategies);
    }

    public static ActivationSpan GetSpanStartingAt(SolverData data, double startTime, int vibesUsed) {
        var startTimestamp = data.GetTimestampFromTime(startTime);
        var hits = data.Hits;
        int startIndex = 0;

        while (startIndex < hits.Length && hits[startIndex].Time.Time < startTime)
            startIndex++;

        if (startIndex < hits.Length)
            return GetSpanStartingAt(data, startTimestamp, startIndex, vibesUsed);

        double endTime = startTime + vibesUsed * VIBE_LENGTH;

        return new ActivationSpan(startTimestamp, data.GetTimestampFromTime(endTime), startIndex, startIndex);
    }

    public static List<ActivationSpan> GetSpansEndingAt(SolverData data, double endTime, int vibesUsed) {
        var endTimestamp = data.GetTimestampFromTime(endTime);
        var hits = data.Hits;
        int endIndex = hits.Length;

        while (endIndex > 0 && hits[endIndex - 1].Time.Time >= endTime)
            endIndex--;

        if (endIndex > 0) {
            var spans = new List<ActivationSpan>();

            GetSpansEndingAt(spans, data, endTimestamp, endIndex, vibesUsed);
            spans.Sort();

            return spans;
        }

        double startTime = endTime - vibesUsed * VIBE_LENGTH;

        return new List<ActivationSpan> { new(data.GetTimestampFromTime(startTime), endTimestamp, endIndex, endIndex) };
    }

    public static List<VibePathSegment> GetVibePath(SolverData data, ActivationSpan span, int vibesUsed) {
        var hits = data.Hits;
        var segments = new List<VibePathSegment>();
        double currentTime = span.StartTime.Time;
        double vibeRemaining = vibesUsed * VIBE_LENGTH;

        for (int i = span.StartIndex; i < span.EndIndex; i++) {
            var hit = hits[i];

            if (!hit.GivesVibe)
                continue;

            segments.Add(new VibePathSegment(data.GetTimestampFromTime(currentTime), hit.Time, vibeRemaining, Math.Max(0d, vibeRemaining - (hit.Time.Time - currentTime))));
            vibeRemaining = Math.Max(VIBE_LENGTH, Math.Min(vibeRemaining - (hit.Time.Time - currentTime) + VIBE_LENGTH, 2d * VIBE_LENGTH));
            currentTime = hit.Time.Time;
        }

        segments.Add(new VibePathSegment(data.GetTimestampFromTime(currentTime), data.GetTimestampFromTime(currentTime + vibeRemaining), vibeRemaining, 0d));

        return segments;
    }

    private static ActivationSpan GetSpanStartingAt(SolverData data, Timestamp startTime, int startIndex, int vibesUsed) {
        var hits = data.Hits;
        double currentTime = startTime.Time;
        double vibeRemaining = vibesUsed * VIBE_LENGTH;
        double endTime = currentTime + vibeRemaining;
        int endIndex = startIndex;

        while (endIndex < hits.Length) {
            var endHit = hits[endIndex];

            if (endHit.Time.Time >= endTime)
                break;

            if (endHit.GivesVibe) {
                vibeRemaining = Math.Min(Math.Max(VIBE_LENGTH, vibeRemaining - (endHit.Time.Time - currentTime) + VIBE_LENGTH), 2d * VIBE_LENGTH);
                currentTime = endHit.Time.Time;
                endTime = currentTime + vibeRemaining;
            }

            endIndex++;
        }

        return new ActivationSpan(startTime, data.GetTimestampFromTime(endTime), startIndex, endIndex);
    }

    private static void GetSpansEndingAt(List<ActivationSpan> spans, SolverData data, Timestamp endTime, int endIndex, int vibesUsed) {
        var hits = data.Hits;
        double currentTime = endTime.Time;
        double vibeNeeded = 0d;
        double minStartTime = currentTime - 2d * VIBE_LENGTH;
        double startTime = currentTime - vibesUsed * VIBE_LENGTH;
        int startIndex = endIndex;

        while (true) {
            if (hits[startIndex].Time.Time >= startTime && (startIndex == 0 || hits[startIndex - 1].Time.Time < startTime))
                spans.Add(new ActivationSpan(data.GetTimestampFromTime(startTime), endTime, startIndex, endIndex));

            startIndex--;

            if (startIndex < 0 || hits[startIndex].Time.Time < minStartTime)
                break;

            var startHit = hits[startIndex];

            if (!startHit.GivesVibe)
                continue;

            vibeNeeded = Math.Min(vibeNeeded + (currentTime - startHit.Time.Time) - VIBE_LENGTH, 2d * VIBE_LENGTH);

            if (vibeNeeded <= 0d)
                break;

            currentTime = startHit.Time.Time;
            minStartTime = Math.Min(currentTime - (2d * VIBE_LENGTH - vibeNeeded), currentTime);
            startTime = Math.Min(currentTime - (vibesUsed * VIBE_LENGTH - vibeNeeded), currentTime);
        }
    }

    private static List<ActivationSpan> GetForwardComputedSpans(SolverData data, int vibesUsed) {
        var hits = data.Hits;
        var spans = new List<ActivationSpan>();

        for (int i = 0; i < hits.Length; i++)
            spans.Add(GetSpanStartingAt(data, hits[i].Time, i, vibesUsed));

        spans.Sort();

        return spans;
    }

    private static List<ActivationSpan> GetBackwardComputedSpans(SolverData data, int vibesUsed) {
        var hits = data.Hits;
        var spans = new List<ActivationSpan>();

        for (int endIndex = 0; endIndex < hits.Length; endIndex++)
            GetSpansEndingAt(spans, data, hits[endIndex].Time, endIndex, vibesUsed);

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

            if (backwardStartTime < forwardStartTime)
                TryAddSpan(backwardSpan);

            backwardSpanIndex++;
        }

        spans.Sort();

        return spans;

        void TryAddSpan(ActivationSpan span) {
            if (spans.Count == 0 || span.StartTime.Time > spans[spans.Count - 1].StartTime.Time)
                spans.Add(span);
        }
    }

    private static List<Activation> GetActivations(SolverData data, List<ActivationSpan> spans, int vibesUsed) {
        int fromIndex = data.GetNextVibe(0);

        if (vibesUsed == 2)
            fromIndex = data.GetNextVibe(fromIndex + 1);

        var hits = data.Hits;
        var activations = new List<Activation>();

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

        activations.Sort();

        return activations;
    }

    private static List<Strategy> GetBestStrategies(SolverData data, List<Activation> activations, out int bestScore) {
        int firstVibeIndex = data.GetNextVibe(0);
        int secondVibeIndex = data.GetNextVibe(firstVibeIndex + 1);
        var strategies = new Strategy[activations.Count];
        var bestNextStrategies = new List<Strategy>();

        bestScore = 0;

        for (int i = strategies.Length - 1; i >= 0; i--) {
            var activation = activations[i];
            var strategy = ComputeStrategy(activation, i);

            strategies[i] = strategy;

            if (activation.VibesUsed == (activation.StartIndex <= secondVibeIndex ? 1 : 2))
                bestScore = Math.Max(bestScore, strategy.Score);
        }

        var overallBestStrategies = new List<Strategy>();

        for (int i = strategies.Length - 1; i >= 0; i--) {
            var strategy = strategies[i];
            var activation = strategy.Activation;

            if (activation.VibesUsed == (activation.StartIndex <= secondVibeIndex ? 1 : 2) && strategy.Score == bestScore)
                overallBestStrategies.Add(strategy);
        }

        return overallBestStrategies;

        Strategy ComputeStrategy(Activation activation, int index) {
            int firstVibeIndex = data.GetNextVibe(activation.EndIndex);
            int secondVibeIndex = data.GetNextVibe(firstVibeIndex + 1);
            int bestNextScore = 0;

            for (int i = strategies.Length - 1; i > index; i--) {
                var nextStrategy = strategies[i];
                var nextActivation = nextStrategy.Activation;

                if (nextActivation.StartIndex <= firstVibeIndex)
                    break;

                if (nextStrategy.Activation.VibesUsed == (nextActivation.StartIndex <= secondVibeIndex ? 1 : 2))
                    bestNextScore = Math.Max(bestNextScore, nextStrategy.Score);
            }

            for (int i = strategies.Length - 1; i > index; i--) {
                var nextStrategy = strategies[i];
                var nextActivation = nextStrategy.Activation;

                if (nextActivation.StartIndex <= firstVibeIndex)
                    break;

                if (nextStrategy.Activation.VibesUsed == (activation.StartIndex <= secondVibeIndex ? 1 : 2) && nextStrategy.Score == bestNextScore)
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
}