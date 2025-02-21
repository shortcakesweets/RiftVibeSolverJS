using System.IO;

namespace RiftVibeSolver.Solver;

public class SolverData {
    public readonly BeatData BeatData;
    public readonly Hit[] Hits;

    private int[] nextVibes;

    public SolverData(BeatData beatData, Hit[] hits) {
        BeatData = beatData;
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

    public void SaveToFile(string path) {
        using var writer = new BinaryWriter(File.Create(path));

        writer.Write(BeatData.BPM);
        writer.Write(BeatData.BeatDivisions);
        writer.Write(BeatData.HitWindow);

        double[] beatTimings = BeatData.BeatTimings;

        writer.Write(beatTimings.Length);

        foreach (double time in beatTimings)
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
        int beatDivisions = reader.ReadInt32();
        float hitWindow = reader.ReadSingle();
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

        return new SolverData(new BeatData(bpm, beatDivisions, hitWindow, beatTimings), hits);
    }
}