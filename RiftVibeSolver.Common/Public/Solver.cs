using System;
using System.Collections.Generic;

namespace RiftVibeSolver.Common;

public static class Solver {
    private const double VIBE_LENGTH = 5d;

    public static SolverResult Solve(SolverData data) {
        if (data.HitCount == 0)
            return new SolverResult(0, Array.Empty<Activation>(), Array.Empty<Activation>(), Array.Empty<Activation>(), Array.Empty<Activation>());

        var singleVibeActivationData = GetActivationData(data, 1);
        var doubleVibeActivationData = GetActivationData(data, 2);
        var singleVibeActivations = new Activation[singleVibeActivationData.Count];
        var doubleVibeActivations = new Activation[doubleVibeActivationData.Count];

        for (int i = 0; i < singleVibeActivationData.Count; i++)
            singleVibeActivations[i] = GetActivationFromData(singleVibeActivationData, i, 1);

        for (int i = 0; i < doubleVibeActivationData.Count; i++)
            doubleVibeActivations[i] = GetActivationFromData(doubleVibeActivationData, i, 2);

        var bestNextActivationsByFirstVibeIndex = GetBestNextActivationsByFirstVibeIndex(data, singleVibeActivationData, doubleVibeActivationData, out int totalScore);
        var (bestSingleVibeActivationIndices, bestDoubleVibeActivationIndices) = GetBestActivationIndices(data, singleVibeActivationData, doubleVibeActivationData, bestNextActivationsByFirstVibeIndex);
        var bestSingleVibeActivations = new Activation[bestSingleVibeActivationIndices.Count];
        var bestDoubleVibeActivations = new Activation[bestDoubleVibeActivationIndices.Count];
        int index = 0;

        foreach (int i in bestSingleVibeActivationIndices) {
            bestSingleVibeActivations[index] = singleVibeActivations[i];
            index++;
        }

        index = 0;

        foreach (int i in bestDoubleVibeActivationIndices) {
            bestDoubleVibeActivations[index] = doubleVibeActivations[i];
            index++;
        }

        return new SolverResult(totalScore, singleVibeActivations, doubleVibeActivations, bestSingleVibeActivations, bestDoubleVibeActivations);
    }

    public static VibePath GetVibePath(SolverData data, double startTime, int vibesUsed) {
        var hits = data.Hits;
        var span = GetSpanStartingAt(data, startTime, data.GetFirstHitIndexAfter(startTime), vibesUsed);
        var segments = new List<VibePathSegment>();
        double currentTime = span.StartTime;
        double vibeRemaining = vibesUsed * VIBE_LENGTH;

        for (int i = span.StartIndex; i < span.EndIndex; i++) {
            var hit = hits[i];

            if (!hit.GivesVibe)
                continue;

            segments.Add(new VibePathSegment(currentTime, hit.Time, vibeRemaining, Math.Max(0d, vibeRemaining - (hit.Time - currentTime))));
            vibeRemaining = Math.Max(VIBE_LENGTH, Math.Min(vibeRemaining - (hit.Time - currentTime) + VIBE_LENGTH, 2d * VIBE_LENGTH));
            currentTime = hit.Time;
        }

        segments.Add(new VibePathSegment(currentTime, currentTime + vibeRemaining, vibeRemaining, 0d));
        currentTime += vibeRemaining;

        double maxEndTime = data.GetTimeFromBeat(data.GetBeatFromTime(currentTime) + GetVibeExtensionForBeatLength(data.GetBeatLengthAtTime(currentTime), data.BeatDivisions));
        double endTime = Math.Min(maxEndTime, data.GetHitTime(data.GetFirstHitIndexAfter(currentTime)));

        int score = 0;

        for (int i = span.StartIndex; i < span.EndIndex; i++)
            score += hits[i].Score;

        return new VibePath(startTime, endTime, span.StartIndex, span.EndIndex, score, segments);
    }

    private static List<ActivationData> GetActivationData(SolverData data, int vibesUsed) {
        var activations = new List<ActivationData>();
        int fromIndex = data.GetNextVibe(0);

        if (vibesUsed == 2)
            fromIndex = data.GetNextVibe(fromIndex + 1);

        if (fromIndex == data.HitCount)
            return activations;

        var hits = data.Hits;
        var spans = new List<ActivationSpan>(2 * hits.Count);

        for (int i = fromIndex; i < hits.Count; i++)
            spans.Add(GetSpanStartingAfterHit(data, i, vibesUsed));

        for (int i = fromIndex; i < hits.Count; i++)
            GetSpansEndingOnHit(spans, data, i, vibesUsed);

        GetSpansEndingOnBeats(spans, data, vibesUsed);

        if (spans.Count == 0)
            return activations;

        spans.Sort();

        int currentStartIndex = spans[0].StartIndex;
        int currentEndIndex = spans[0].StartIndex;
        int score = 0;

        foreach (var span in spans) {
            if (span.StartIndex <= fromIndex)
                continue;

            while (currentStartIndex < span.StartIndex) {
                score -= hits[currentStartIndex].Score;
                currentStartIndex++;
            }

            while (currentEndIndex < span.EndIndex) {
                score += hits[currentEndIndex].Score;
                currentEndIndex++;
            }

            while (currentEndIndex > span.EndIndex) {
                currentEndIndex--;
                score -= hits[currentEndIndex].Score;
            }

            if (activations.Count > 0 && activations[activations.Count - 1].MinStartTime == span.StartTime) {
                if (activations[activations.Count - 1].EndIndex >= span.EndIndex)
                    continue;

                activations.RemoveAt(activations.Count - 1);
            }

            activations.Add(new ActivationData(span.StartTime, span.StartIndex, span.EndIndex, score));
        }

        return activations;
    }

    private static ActivationSpan GetSpanStartingAt(SolverData data, double startTime, int firstHitIndex, int vibesUsed) {
        double currentTime = startTime;
        double vibeRemaining = vibesUsed * VIBE_LENGTH;
        double vibeZeroTime = currentTime + vibeRemaining;
        double endTime = GetEndTime();
        int nextVibeIndex = data.GetNextVibe(firstHitIndex);

        while (nextVibeIndex < data.HitCount) {
            double nextVibeTime = data.GetHitTime(nextVibeIndex);

            if (nextVibeTime > endTime)
                break;

            vibeRemaining = Math.Max(VIBE_LENGTH, Math.Min(vibeRemaining - (nextVibeTime - currentTime) + VIBE_LENGTH, 2d * VIBE_LENGTH));
            currentTime = nextVibeTime;
            vibeZeroTime = currentTime + vibeRemaining;
            endTime = GetEndTime();
            nextVibeIndex = data.GetNextVibe(nextVibeIndex + 1);
        }

        return new ActivationSpan(startTime, firstHitIndex, data.GetFirstHitIndexAfter(endTime));

        double GetEndTime() {
            double maxEndTime = data.GetTimeFromBeat(data.GetBeatFromTime(vibeZeroTime) + GetVibeExtensionForBeatLength(data.GetBeatLengthAtTime(vibeZeroTime), data.BeatDivisions));

            return Math.Min(maxEndTime, data.GetHitTime(data.GetFirstHitIndexAfter(vibeZeroTime)));
        }
    }

    private static ActivationSpan GetSpanStartingAfterHit(SolverData data, int hitIndex, int vibesUsed) => GetSpanStartingAt(data, data.GetHitTime(hitIndex), hitIndex + 1, vibesUsed);

    private static void GetSpansEndingOnHit(List<ActivationSpan> spans, SolverData data, int hitIndex, int vibesUsed) {
        double endTime = data.GetHitTime(hitIndex);
        double endBeat = data.GetBeatFromTime(endTime);
        double previousHitTime = data.GetHitTime(hitIndex - 1);
        int endIndex;

        if (data.Hits[hitIndex].GivesVibe)
            endIndex = GetSpanStartingAfterHit(data, hitIndex, 1).EndIndex;
        else
            endIndex = hitIndex + 1;

        if (!data.HasBeatTimings) {
            double vibeZeroTime = data.GetTimeFromBeat(endBeat - GetVibeExtensionForBeatLength(data.GetBeatLengthForBeat((int) endBeat), data.BeatDivisions));

            vibeZeroTime = Math.Max(vibeZeroTime, previousHitTime);
            GetSpansWhereVibeHitsZeroAt(spans, data, vibeZeroTime, endIndex, vibesUsed);

            return;
        }

        for (int i = (int) endBeat; i >= 1; i--) {
            double beatTime = data.GetTimeFromBeat(i);
            double nextBeatTime = data.GetTimeFromBeat(i + 1);
            double vibeZeroTime = data.GetTimeFromBeat(endBeat - GetVibeExtensionForBeatLength(nextBeatTime - beatTime, data.BeatDivisions));

            vibeZeroTime = Math.Max(vibeZeroTime, previousHitTime);

            if (vibeZeroTime > beatTime && vibeZeroTime < nextBeatTime)
                GetSpansWhereVibeHitsZeroAt(spans, data, vibeZeroTime, endIndex, vibesUsed);

            if (beatTime <= previousHitTime)
                break;
        }
    }

    private static void GetSpansEndingOnBeats(List<ActivationSpan> spans, SolverData data, int vibesUsed) {
        var beatTimings = data.BeatTimings;

        if (beatTimings.Count <= 1)
            return;

        for (int i = 1; i < beatTimings.Count - 1; i++) {
            double beatTime = beatTimings[i];
            double endTimeWithPreviousBeatLength = data.GetTimeFromBeat(i + 1 + GetVibeExtensionForBeatLength(beatTime - beatTimings[i - 1], data.BeatDivisions));
            double endTimeWithNextBeatLength = data.GetTimeFromBeat(i + 1 + GetVibeExtensionForBeatLength(beatTimings[i + 1] - beatTime, data.BeatDivisions));
            int nextHitIndex = data.GetFirstHitIndexAfter(beatTime);
            double nextHitTime = data.GetHitTime(nextHitIndex);

            if (!(endTimeWithPreviousBeatLength >= nextHitTime ^ endTimeWithNextBeatLength >= nextHitTime))
                continue;

            int hitIndex = nextHitIndex;

            if (endTimeWithNextBeatLength < nextHitTime)
                hitIndex--;

            int endIndex;

            if (hitIndex < data.HitCount && data.Hits[hitIndex].GivesVibe)
                endIndex = GetSpanStartingAfterHit(data, hitIndex, 1).EndIndex;
            else
                endIndex = hitIndex + 1;

            GetSpansWhereVibeHitsZeroAt(spans, data, beatTime, endIndex, vibesUsed);
        }
    }

    private static void GetSpansWhereVibeHitsZeroAt(List<ActivationSpan> spans, SolverData data, double vibeZeroTime, int endIndex, int vibesUsed) {
        double currentTime = vibeZeroTime;
        double vibeNeeded = 0d;
        int previousVibeIndex = data.GetPreviousVibe(data.GetFirstHitIndexAfter(currentTime));

        do {
            double previousVibeTime = data.GetHitTime(previousVibeIndex);
            double possibleStartTime = currentTime - (vibesUsed * VIBE_LENGTH - vibeNeeded);
            double needFullVibeAt = currentTime - (2d * VIBE_LENGTH - vibeNeeded);

            if (possibleStartTime > previousVibeTime)
                spans.Add(new ActivationSpan(possibleStartTime, data.GetFirstHitIndexAfter(possibleStartTime), endIndex));

            if (previousVibeTime < needFullVibeAt)
                break;

            vibeNeeded = Math.Min(vibeNeeded + (currentTime - previousVibeTime) - VIBE_LENGTH, 2d * VIBE_LENGTH);

            if (vibeNeeded <= 0d)
                break;

            currentTime = previousVibeTime;
            previousVibeIndex = data.GetPreviousVibe(previousVibeIndex);
        } while (previousVibeIndex >= 0);
    }

    private static double GetVibeExtensionForBeatLength(double beatLength, int beatDivisions) {
        double hitWindowInBeats = 0.175d / beatLength;

        return hitWindowInBeats - Math.Floor(hitWindowInBeats * beatDivisions) / beatDivisions;
    }

    private static Dictionary<int, BestNextActivations> GetBestNextActivationsByFirstVibeIndex(SolverData data, List<ActivationData> singleVibeActivations, List<ActivationData> doubleVibeActivations, out int totalScore) {
        var bestNextActivationsByFirstVibeIndex = new Dictionary<int, BestNextActivations>();

        totalScore = GetBestNextActivations(0).BestNextValue;

        return bestNextActivationsByFirstVibeIndex;

        BestNextActivations GetBestNextActivations(int fromIndex) {
            int firstVibeIndex = data.GetNextVibe(fromIndex);

            if (bestNextActivationsByFirstVibeIndex.TryGetValue(firstVibeIndex, out var bestNextActivations))
                return bestNextActivations;

            int secondVibeIndex = data.GetNextVibe(firstVibeIndex + 1);
            int bestNextValue = 0;
            var bestNextSingleVibeActivations = new List<int>();
            var bestNextDoubleVibeActivations = new List<int>();

            for (int i = 0; i < singleVibeActivations.Count; i++) {
                var nextActivation = singleVibeActivations[i];

                if (nextActivation.StartIndex <= firstVibeIndex)
                    continue;

                if (nextActivation.StartIndex > secondVibeIndex)
                    break;

                int nextActivationValue = nextActivation.Score + GetBestNextActivations(nextActivation.EndIndex).BestNextValue;

                if (nextActivationValue > bestNextValue) {
                    bestNextValue = nextActivationValue;
                    bestNextSingleVibeActivations.Clear();
                }

                if (nextActivationValue == bestNextValue)
                    bestNextSingleVibeActivations.Add(i);
            }

            for (int i = 0; i < doubleVibeActivations.Count; i++) {
                var nextActivation = doubleVibeActivations[i];

                if (nextActivation.StartIndex <= secondVibeIndex)
                    continue;

                int nextActivationValue = nextActivation.Score + GetBestNextActivations(nextActivation.EndIndex).BestNextValue;

                if (nextActivationValue > bestNextValue) {
                    bestNextValue = nextActivationValue;
                    bestNextSingleVibeActivations.Clear();
                    bestNextDoubleVibeActivations.Clear();
                }

                if (nextActivationValue == bestNextValue)
                    bestNextDoubleVibeActivations.Add(i);
            }

            bestNextActivations = new BestNextActivations(bestNextValue, bestNextSingleVibeActivations, bestNextDoubleVibeActivations);
            bestNextActivationsByFirstVibeIndex.Add(firstVibeIndex, bestNextActivations);

            return bestNextActivations;
        }
    }

    private static (HashSet<int>, HashSet<int>) GetBestActivationIndices(SolverData data, List<ActivationData> singleVibeActivationData, List<ActivationData> doubleVibeActivationData, Dictionary<int, BestNextActivations> bestNextActivationsByFirstVibeIndex) {
        var bestSingleVibeActivationIndices = new HashSet<int>();
        var bestDoubleVibeActivationIndices = new HashSet<int>();

        Traverse(0);

        return (bestSingleVibeActivationIndices, bestDoubleVibeActivationIndices);

        void Traverse(int fromIndex) {
            var bestNextActivations = bestNextActivationsByFirstVibeIndex[data.GetNextVibe(fromIndex)];

            foreach (int index in bestNextActivations.BestNextSingleVibeActivations) {
                if (bestSingleVibeActivationIndices.Contains(index))
                    continue;

                bestSingleVibeActivationIndices.Add(index);
                Traverse(singleVibeActivationData[index].EndIndex);
            }

            foreach (int index in bestNextActivations.BestNextDoubleVibeActivations) {
                if (bestDoubleVibeActivationIndices.Contains(index))
                    continue;

                bestDoubleVibeActivationIndices.Add(index);
                Traverse(doubleVibeActivationData[index].EndIndex);
            }
        }
    }

    private static Activation GetActivationFromData(List<ActivationData> activationData, int index, int vibesUsed) {
        var activation = activationData[index];

        return new Activation(
            activation.MinStartTime,
            index < activationData.Count - 1 ? activationData[index + 1].MinStartTime : activation.MinStartTime + 1d,
            activation.Score,
            vibesUsed);
    }
}