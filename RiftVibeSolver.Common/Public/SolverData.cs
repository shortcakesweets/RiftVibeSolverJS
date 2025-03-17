using System;
using System.Collections.Generic;

namespace RiftVibeSolver.Common;

public class SolverData {
    public int BPM { get; }
    public int BeatDivisions { get; }
    public bool HasBeatTimings => beatTimings.Count > 1;
    public IReadOnlyList<double> BeatTimings => beatTimings;
    public int HitCount => hits.Count;
    public IReadOnlyList<Hit> Hits => hits;

    private readonly List<double> beatTimings;
    private readonly List<Hit> hits;
    private readonly int[] nextVibes;
    private readonly int[] previousVibes;

    public SolverData(int bpm, int beatDivisions, List<double> beatTimings, List<Hit> hits) {
        BPM = bpm;
        BeatDivisions = beatDivisions;
        this.beatTimings = beatTimings;
        this.hits = hits;
        nextVibes = new int[hits.Count];

        int nextVibe = hits.Count;

        for (int i = hits.Count - 1; i >= 0; i--) {
            if (hits[i].GivesVibe)
                nextVibe = i;

            nextVibes[i] = nextVibe;
        }

        previousVibes = new int[hits.Count];

        int previousVibe = -1;

        for (int i = 0; i < hits.Count; i++) {
            previousVibes[i] = previousVibe;

            if (hits[i].GivesVibe)
                previousVibe = i;
        }
    }

    public double GetBeatFromTime(double time) {
        if (double.IsPositiveInfinity(time))
            return double.PositiveInfinity;

        if (double.IsNegativeInfinity(time))
            return double.NegativeInfinity;

        if (beatTimings.Count <= 1)
            return time / (60d / Math.Max(1, BPM)) + 1d;

        int beatIndex = GetBeatNumberFromTime(time);
        double previous = beatTimings[beatIndex];
        double next = beatTimings[beatIndex + 1];

        return beatIndex + 1 + (time - previous) / (next - previous);
    }

    public int GetBeatNumberFromTime(double time) {
        int min = 0;
        int max = beatTimings.Count - 1;

        while (max >= min) {
            int mid = (min + max) / 2;

            if (beatTimings[mid] > time)
                max = mid - 1;
            else
                min = mid + 1;
        }

        return Math.Max(0, Math.Min(max, beatTimings.Count - 2));
    }

    public double GetTimeFromBeat(double beat) {
        if (double.IsPositiveInfinity(beat))
            return double.PositiveInfinity;

        if (double.IsNegativeInfinity(beat))
            return double.NegativeInfinity;

        if (beatTimings.Count <= 1)
            return 60d / Math.Max(1, BPM) * (beat - 1d);

        if (beat <= 1d) {
            double first = beatTimings[0];
            double second = beatTimings[1];

            return first - (second - first) * (1d - beat);
        }

        if (beat < beatTimings.Count) {
            double previous = beatTimings[(int) beat - 1];
            double next = beatTimings[(int) beat];

            return previous + (next - previous) * (beat % 1d);
        }

        double last = beatTimings[beatTimings.Count - 1];
        double secondToLast = beatTimings[beatTimings.Count - 2];

        return last + (last - secondToLast) * (beat - beatTimings.Count);
    }

    public double GetTimeFromBeat(int beat) {
        if (beatTimings.Count <= 1)
            return 60d / Math.Max(1, BPM) * (beat - 1);

        if (beat < 1) {
            double first = beatTimings[0];
            double second = beatTimings[1];

            return first - (second - first) * (1 - beat);
        }

        if (beat <= beatTimings.Count)
            return beatTimings[beat - 1];

        double last = beatTimings[beatTimings.Count - 1];
        double secondToLast = beatTimings[beatTimings.Count - 2];

        return last + (last - secondToLast) * (beat - beatTimings.Count);
    }

    public double GetBeatLengthAtTime(double time) {
        if (beatTimings.Count <= 1)
            return 60d / Math.Max(1, BPM);

        int beatIndex = GetBeatNumberFromTime(time);

        return beatTimings[beatIndex + 1] - beatTimings[beatIndex];
    }

    public double GetBeatLengthForBeat(int beat) {
        if (beatTimings.Count <= 1)
            return 60d / Math.Max(1, BPM);

        if (beat < 1)
            return beatTimings[1] - beatTimings[0];

        if (beat < beatTimings.Count)
            return beatTimings[beat] - beatTimings[beat - 1];

        return beatTimings[beatTimings.Count - 1] - beatTimings[beatTimings.Count - 2];
    }

    public double GetHitTime(int hitIndex) {
        if (hitIndex < 0)
            return double.NegativeInfinity;

        if (hitIndex >= hits.Count)
            return double.PositiveInfinity;

        return hits[hitIndex].Time;
    }

    public int GetNextVibe(int hitIndex) {
        if (hitIndex < 0)
            hitIndex = 0;

        return hitIndex < nextVibes.Length ? nextVibes[hitIndex] : nextVibes.Length;
    }

    public int GetPreviousVibe(int hitIndex) {
        if (hitIndex >= previousVibes.Length)
            hitIndex = previousVibes.Length - 1;

        return hitIndex >= 0 ? previousVibes[hitIndex] : -1;
    }

    public int GetFirstHitIndexAfter(double time) {
        int min = 0;
        int max = hits.Count;

        while (max >= min && min < hits.Count) {
            int mid = (min + max) / 2;

            if (hits[mid].Time > time)
                max = mid - 1;
            else
                min = mid + 1;
        }

        return min;
    }
}