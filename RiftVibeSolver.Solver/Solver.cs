using System;
using System.Collections.Generic;
using RiftEventCapture.Common;

namespace RiftVibeSolver.Solver;

public static class Solver {
    private const double VIBE_LENGTH = 5d;

    public static List<Activation> Solve(SolverData data, out int totalScore) {
        if (data.Hits.Count == 0) {
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
        var beatData = data.BeatData;
        var hits = data.Hits;
        var startTimestamp = beatData.GetTimestampFromTime(startTime);
        int startIndex = 0;

        while (startIndex < hits.Count && hits[startIndex].Time.Time < startTime)
            startIndex++;

        if (startIndex < hits.Count)
            return GetSpanStartingAt(data, startTimestamp, startIndex, vibesUsed);

        double endTime = startTime + vibesUsed * VIBE_LENGTH;

        return new ActivationSpan(startTimestamp, beatData.GetTimestampFromTime(endTime), startIndex, startIndex);
    }

    public static List<ActivationSpan> GetSpansEndingBefore(SolverData data, double endTime, int vibesUsed) {
        var beatData = data.BeatData;
        var hits = data.Hits;
        var endTimestamp = beatData.GetTimestampFromTime(endTime);
        int endIndex = hits.Count;

        while (endIndex > 0 && hits[endIndex - 1].Time.Time >= endTime)
            endIndex--;

        if (endIndex > 0) {
            var spans = new List<ActivationSpan>();

            GetSpansEndingBefore(spans, data, endTimestamp, endIndex, vibesUsed);
            spans.Sort();

            return spans;
        }

        double startTime = endTime - vibesUsed * VIBE_LENGTH;

        return new List<ActivationSpan> { new(beatData.GetTimestampFromTime(startTime), endTimestamp, endIndex, endIndex) };
    }

    public static List<VibePathSegment> GetVibePath(SolverData data, ActivationSpan span, int vibesUsed) {
        var beatData = data.BeatData;
        var hits = data.Hits;
        var segments = new List<VibePathSegment>();
        double currentTime = span.StartTime.Time;
        double vibeRemaining = vibesUsed * VIBE_LENGTH;

        for (int i = span.StartIndex; i < span.EndIndex; i++) {
            var hit = hits[i];

            if (!hit.GivesVibe)
                continue;

            segments.Add(new VibePathSegment(beatData.GetTimestampFromTime(currentTime), hit.Time, vibeRemaining, Math.Max(0d, vibeRemaining - (hit.Time.Time - currentTime))));
            vibeRemaining = Math.Max(VIBE_LENGTH, Math.Min(vibeRemaining - (hit.Time.Time - currentTime) + VIBE_LENGTH, 2d * VIBE_LENGTH));
            currentTime = hit.Time.Time;
        }

        segments.Add(new VibePathSegment(beatData.GetTimestampFromTime(currentTime), beatData.GetTimestampFromTime(currentTime + vibeRemaining), vibeRemaining, 0d));

        return segments;
    }

    private static ActivationSpan GetSpanStartingAt(SolverData data, Timestamp startTime, int startIndex, int vibesUsed) {
        var beatData = data.BeatData;
        var hits = data.Hits;
        double currentTime = startTime.Time;
        double vibeRemaining = vibesUsed * VIBE_LENGTH;
        double vibeEndTime = currentTime + vibeRemaining;
        double endTime = GetMaxEndTime();
        int endIndex = startIndex;

        while (endIndex < hits.Count) {
            var endHit = hits[endIndex];

            if (endHit.Time.Time >= endTime)
                break;

            if (endHit.GivesVibe) {
                vibeRemaining = Math.Min(Math.Max(VIBE_LENGTH, vibeRemaining - (endHit.Time.Time - currentTime) + VIBE_LENGTH), 2d * VIBE_LENGTH);
                currentTime = endHit.Time.Time;
                vibeEndTime = currentTime + vibeRemaining;
                endTime = GetMaxEndTime();
            }
            else if (endHit.Time.Time >= vibeEndTime) {
                endTime = endHit.Time.Time;
                endIndex++;

                break;
            }

            endIndex++;
        }

        return new ActivationSpan(startTime, beatData.GetTimestampFromTime(endTime), startIndex, endIndex);

        double GetMaxEndTime() {
            double vibeEndBeat = beatData.GetBeatFromTime(vibeEndTime);
            double hitWindowInBeats = 0.175d / beatData.GetBeatLengthAtTime(vibeEndTime);
            double maxEndBeat = vibeEndBeat + hitWindowInBeats - Math.Floor(hitWindowInBeats * beatData.BeatDivisions) / beatData.BeatDivisions;

            return beatData.GetTimeFromBeat(maxEndBeat);
        }
    }

    private static void GetSpansWhereVibeHitsZeroBefore(List<ActivationSpan> spans, SolverData data, double vibeZeroTime, Timestamp endTime, int endIndex, int vibesUsed) {
        var hits = data.Hits;
        var beatData = data.BeatData;
        double vibeNeeded = 0d;
        double minStartTime = vibeZeroTime - 2d * VIBE_LENGTH;
        double startTime = vibeZeroTime - vibesUsed * VIBE_LENGTH;
        int startIndex = endIndex;

        while (true) {
            if (startIndex < endIndex && hits[startIndex].Time.Time >= startTime && (startIndex == 0 || hits[startIndex - 1].Time.Time < startTime))
                spans.Add(new ActivationSpan(beatData.GetTimestampFromTime(startTime), endTime, startIndex, endIndex));

            startIndex--;

            if (startIndex < 0 || hits[startIndex].Time.Time < minStartTime)
                break;

            var startHit = hits[startIndex];

            if (!startHit.GivesVibe)
                continue;

            vibeNeeded = Math.Min(vibeNeeded + (vibeZeroTime - startHit.Time.Time) - VIBE_LENGTH, 2d * VIBE_LENGTH);

            if (vibeNeeded <= 0d)
                break;

            vibeZeroTime = startHit.Time.Time;
            minStartTime = Math.Min(vibeZeroTime - (2d * VIBE_LENGTH - vibeNeeded), vibeZeroTime);
            startTime = Math.Min(vibeZeroTime - (vibesUsed * VIBE_LENGTH - vibeNeeded), vibeZeroTime);
        }
    }

    private static void GetSpansWhereVibeHitsZeroBeforeBeat(List<ActivationSpan> spans, SolverData data, int currentBeat, int vibesUsed) {
        var hits = data.Hits;
        var beatData = data.BeatData;
        double vibeZeroTime = beatData.GetTimeFromBeat(currentBeat);
        int endIndex = hits.Count;

        while (endIndex > 0 && hits[endIndex - 1].Time.Time >= vibeZeroTime)
            endIndex--;

        double hitWindowInBeats = 0.175d / beatData.GetBeatLengthForBeat(currentBeat - 1);
        double endBeat = currentBeat + hitWindowInBeats - Math.Floor(hitWindowInBeats * beatData.BeatDivisions) / beatData.BeatDivisions;
        double endTime = beatData.GetTimeFromBeat(endBeat);

        if (endIndex < hits.Count && hits[endIndex].Time.Time < endTime) {
            endTime = hits[endIndex].Time.Time;
            endIndex++;
        }

        GetSpansWhereVibeHitsZeroBefore(spans, data, vibeZeroTime, beatData.GetTimestampFromTime(endTime), endIndex, vibesUsed);
    }

    private static void GetSpansEndingBefore(List<ActivationSpan> spans, SolverData data, Timestamp endTime, int endIndex, int vibesUsed) {
        var beatData = data.BeatData;
        var hits = data.Hits;
        int minEndBeat = endIndex > 0 ? (int) beatData.GetBeatFromTime(hits[endIndex - 1].Time.Time) : 1;

        for (int endBeat = (int) beatData.GetBeatFromTime(endTime.Time); endBeat >= minEndBeat; endBeat--) {
            double hitWindowInBeats = 0.175d / beatData.GetBeatLengthForBeat(endBeat);
            double vibeEndBeat = endTime.Beat - hitWindowInBeats + Math.Floor(hitWindowInBeats * beatData.BeatDivisions) / beatData.BeatDivisions;
            double currentTime = beatData.GetTimeFromBeat(vibeEndBeat);

            if (currentTime < beatData.GetTimeFromBeat(endBeat) || currentTime >= beatData.GetTimeFromBeat(endBeat + 1))
                continue;

            if (endIndex > 0 && currentTime <= hits[endIndex - 1].Time.Time)
                currentTime = hits[endIndex - 1].Time.Time;

            GetSpansWhereVibeHitsZeroBefore(spans, data, currentTime, endTime, endIndex, vibesUsed);
        }
    }

    private static List<ActivationSpan> GetForwardComputedSpans(SolverData data, int vibesUsed) {
        var hits = data.Hits;
        var spans = new List<ActivationSpan>();

        for (int i = 0; i < hits.Count; i++)
            spans.Add(GetSpanStartingAt(data, hits[i].Time, i, vibesUsed));

        spans.Sort();

        return spans;
    }

    private static List<ActivationSpan> GetBackwardComputedSpans(SolverData data, int vibesUsed) {
        var hits = data.Hits;
        var beatData = data.BeatData;
        var spans = new List<ActivationSpan>();

        for (int endIndex = 0; endIndex < hits.Count; endIndex++)
            GetSpansEndingBefore(spans, data, hits[endIndex].Time, endIndex, vibesUsed);

        var beatTimings = beatData.BeatTimings;

        for (int i = 2; i < beatTimings.Count; i++) {
            if (beatData.GetBeatLengthForBeat(i - 1) != beatData.GetBeatLengthForBeat(i))
                GetSpansWhereVibeHitsZeroBeforeBeat(spans, data, i, vibesUsed);
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

            if (forwardSpan.StartTime.Time < backwardSpan.StartTime.Time) {
                TryAddSpan(forwardSpan);
                forwardSpanIndex++;

                continue;
            }

            if (backwardSpan.StartTime.Time < forwardSpan.StartTime.Time)
                TryAddSpan(backwardSpan);

            backwardSpanIndex++;
        }

        spans.Sort();

        return spans;

        void TryAddSpan(ActivationSpan span) {
            if (spans.Count > 0 && span.StartTime.Time <= spans[spans.Count - 1].StartTime.Time)
                return;

            if (spans.Count > 0 && span.StartIndex == spans[spans.Count - 1].StartIndex && span.EndIndex == spans[spans.Count - 1].EndIndex)
                spans.RemoveAt(spans.Count - 1);

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

                if (nextStrategy.Activation.VibesUsed == (nextActivation.StartIndex <= secondVibeIndex ? 1 : 2) && nextStrategy.Score == bestNextScore)
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