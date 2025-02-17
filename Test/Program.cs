using System;
using System.IO;
using RiftVibeSolver.Solver;

namespace RiftVibeSolver.Test;

internal class Program {
    public static void Main(string[] args) {
        string path;

        do {
            Console.Write("Enter score data file path: ");
            path = (Console.ReadLine() ?? string.Empty).Trim(' ', '"');
        } while (!File.Exists(path));

        Console.WriteLine();

        var data = GetData(path);

        if (data.Hits.Length == 0)
            return;

        var activations = Solver.Solver.Solve(data, out int bestScore);

        foreach (var activation in activations)
            Console.WriteLine(activation.ToString());

        Console.WriteLine();
        Console.WriteLine($"Overall bonus: {bestScore}");
        Console.ReadLine();
    }

    private static SolverData GetData(string path) {
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