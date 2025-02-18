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

        var data = SolverData.LoadFromFile(path);

        if (data.Hits.Length == 0)
            return;

        var activations = Solver.Solver.Solve(data, out int bestScore);

        foreach (var activation in activations)
            Console.WriteLine(activation.ToString());

        Console.WriteLine();
        Console.WriteLine($"Overall bonus: {bestScore}");
        Console.ReadLine();
    }
}