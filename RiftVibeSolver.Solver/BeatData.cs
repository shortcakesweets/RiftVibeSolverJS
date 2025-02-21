using System;

namespace RiftVibeSolver.Solver;

public class BeatData {
    public readonly int BPM;
    public readonly int BeatDivisions;
    public readonly float HitWindow;
    public readonly double[] BeatTimings;

    public BeatData(int bpm, int beatDivisions, float hitWindow, double[] beatTimings) {
        BPM = bpm;
        BeatDivisions = beatDivisions;
        HitWindow = hitWindow;
        BeatTimings = beatTimings;
    }

    public double GetBeatFromTime(double time) {
        if (BeatTimings.Length <= 1)
            return time / (60d / Math.Max(1, BPM)) + 1d;

        for (int i = 0; i < BeatTimings.Length - 1; i++) {
            if (time >= BeatTimings[i + 1])
                continue;

            double previous = BeatTimings[i];
            double next = BeatTimings[i + 1];

            return i + 1 + (time - previous) / (next - previous);
        }

        double last = BeatTimings[BeatTimings.Length - 1];
        double secondToLast = BeatTimings[BeatTimings.Length - 2];

        return BeatTimings.Length + (time - last) / (last - secondToLast);
    }

    public double GetTimeFromBeat(double beat) {
        if (beat <= 1d)
            return BeatTimings.Length > 0 ? BeatTimings[0] : 0d;

        if (BeatTimings.Length <= 1)
            return 60d / Math.Max(1, BPM) * (beat - 1d);

        if ((int) beat < BeatTimings.Length) {
            double previous = BeatTimings[(int) beat - 1];
            double next = BeatTimings[(int) beat];

            return previous + (next - previous) * (beat % 1d);
        }

        double last = BeatTimings[BeatTimings.Length - 1];
        double secondToLast = BeatTimings[BeatTimings.Length - 2];

        return last + (last - secondToLast) * (beat - BeatTimings.Length);
    }

    public double GetTimeFromBeat(int beat) {
        if (beat <= 1)
            return BeatTimings.Length > 0 ? BeatTimings[0] : 0d;

        if (BeatTimings.Length <= 1)
            return 60d / Math.Max(1, BPM) * (beat - 1);

        if (beat <= BeatTimings.Length)
            return BeatTimings[beat - 1];

        double last = BeatTimings[BeatTimings.Length - 1];
        double secondToLast = BeatTimings[BeatTimings.Length - 2];

        return last + (last - secondToLast) * (beat - BeatTimings.Length);
    }

    public Timestamp GetTimestampFromTime(double time) => new(time, GetBeatFromTime(time));

    public Timestamp GetTimestampFromBeat(double beat) => new(GetTimeFromBeat(beat), beat);

    public double GetBeatLengthAtTime(double time) {
        if (BeatTimings.Length <= 1)
            return 60d / Math.Max(1, BPM);

        for (int i = 0; i < BeatTimings.Length - 1; i++) {
            if (time < BeatTimings[i + 1])
                return BeatTimings[i + 1] - BeatTimings[i];
        }

        return BeatTimings[BeatTimings.Length - 1] - BeatTimings[BeatTimings.Length - 2];
    }

    public double GetBeatLengthForBeat(int beat) {
        if (BeatTimings.Length <= 1)
            return 60d / Math.Max(1, BPM);

        if (beat < 1)
            return BeatTimings[1] - BeatTimings[0];

        if (beat < BeatTimings.Length)
            return BeatTimings[beat] - BeatTimings[beat - 1];

        return BeatTimings[BeatTimings.Length - 1] - BeatTimings[BeatTimings.Length - 2];
    }
}