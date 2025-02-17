using System;

namespace RiftVibeSolver.Solver;

public static class Util {
    public static double GetBeatFromTime(int bpm, double[] beatTimings, double time) {
        if (beatTimings.Length <= 1)
            return time / (60d / Math.Max(1, bpm)) + 1d;

        for (int i = 0; i < beatTimings.Length - 1; i++) {
            if (time < beatTimings[i + 1])
                return i + 1 + (time - beatTimings[i]) / (beatTimings[i + 1] - beatTimings[i]);
        }

        return beatTimings.Length + (time - beatTimings[beatTimings.Length - 1]) / (beatTimings[beatTimings.Length - 1] - beatTimings[beatTimings.Length - 2]);
    }

    public static double GetTimeFromBeat(int bpm, double[] beatTimings, double beat) {
        beat--;

        if (beat <= 0d)
            return 0d;

        if (beatTimings.Length <= 1)
            return 60d / Math.Max(1, bpm) * beat;


        if ((int) beat > beatTimings.Length - 2) {
            double last = beatTimings[beatTimings.Length - 1];
            double secondToLast = beatTimings[beatTimings.Length - 2];

            return last + (last - secondToLast) * (beat - (beatTimings.Length - 1));
        }

        double previous = beatTimings[(int) beat];
        double next = beatTimings[(int) beat + 1];

        return previous + (next - previous) * (beat % 1d);
    }
}