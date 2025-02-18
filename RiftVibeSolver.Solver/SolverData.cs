using System;
using System.IO;

namespace RiftVibeSolver.Solver;

public class SolverData {
    public readonly int BPM;
    public readonly double[] BeatTimings;
    public readonly Hit[] Hits;

    private int[] nextVibes;

    public SolverData(int bpm, double[] beatTimings, Hit[] hits) {
        BPM = bpm;
        BeatTimings = beatTimings;
        Hits = hits;
    }

    public int GetNextVibe(int index) {
        if (nextVibes != null)
            return index < nextVibes.Length ? nextVibes[index] : nextVibes.Length;

        nextVibes = new int[Hits.Length];

        int nextVibe = Hits.Length;

        for (int i = Hits.Length - 1; i >= 0; i--) {
            if (Hits[i].GivesVibe)
                nextVibe = i;

            nextVibes[i] = nextVibe;
        }

        return index < nextVibes.Length ? nextVibes[index] : nextVibes.Length;
    }

    public Timestamp GetTimestampFromTime(double time) {
        if (BeatTimings.Length <= 1)
            return new Timestamp(time, time / (60d / Math.Max(1, BPM)) + 1d);

        for (int i = 0; i < BeatTimings.Length - 1; i++) {
            if (time < BeatTimings[i + 1])
                return new Timestamp(time, i + 1 + (time - BeatTimings[i]) / (BeatTimings[i + 1] - BeatTimings[i]));
        }

        return new Timestamp(time, BeatTimings.Length + (time - BeatTimings[BeatTimings.Length - 1]) / (BeatTimings[BeatTimings.Length - 1] - BeatTimings[BeatTimings.Length - 2]));
    }

    public Timestamp GetTimestampFromBeat(double beat) {
        beat--;

        if (beat <= 0d)
            return new Timestamp(BeatTimings.Length > 0 ? BeatTimings[0] : 0d, 0d);

        if (BeatTimings.Length <= 1)
            return new Timestamp(60d / Math.Max(1, BPM) * beat, beat);


        if ((int) beat > BeatTimings.Length - 2) {
            double last = BeatTimings[BeatTimings.Length - 1];
            double secondToLast = BeatTimings[BeatTimings.Length - 2];

            return new Timestamp(last + (last - secondToLast) * (beat - (BeatTimings.Length - 1)), beat);
        }

        double previous = BeatTimings[(int) beat];
        double next = BeatTimings[(int) beat + 1];

        return new Timestamp(previous + (next - previous) * (beat % 1d), beat);
    }

    public void SaveToFile(string path) {
        using var writer = new BinaryWriter(File.Create(path));

        writer.Write(BPM);
        writer.Write(BeatTimings.Length);

        foreach (double time in BeatTimings)
            writer.Write(time);

        writer.Write(Hits.Length);

        foreach (var hit in Hits) {
            writer.Write(hit.Time.Time);
            writer.Write(hit.Time.Beat);
            writer.Write(hit.Score);
            writer.Write(hit.GivesVibe);
        }
    }

    public static SolverData LoadFromFile(string path) {
        using var reader = new BinaryReader(File.OpenRead(path));

        int bpm = reader.ReadInt32();
        int beatTimingsCount = reader.ReadInt32();
        double[] beatTimings = new double[beatTimingsCount];

        for (int i = 0; i < beatTimingsCount; i++)
            beatTimings[i] = reader.ReadDouble();

        int hitsCount = reader.ReadInt32();
        var hits = new Hit[hitsCount];

        for (int i = 0; i < hitsCount; i++) {
            double time = reader.ReadDouble();
            double beat = reader.ReadDouble();
            int score = reader.ReadInt32();
            bool givesVibe = reader.ReadBoolean();

            hits[i] = new Hit(new Timestamp(time, beat), score, givesVibe);
        }

        return new SolverData(bpm, beatTimings, hits);
    }
}