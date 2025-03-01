using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using RiftEventCapture.Plugin;
using RiftVibeSolver.Solver;

namespace RiftVibeSolver.Plugin;

[BepInDependency("programmatic.riftEventCapture", "1.0.0.0")]
[BepInPlugin("programmatic.riftVibeSolver", "RiftVibeSolver", "1.3.0.0")]
public class Plugin : BaseUnityPlugin {
    public new static ManualLogSource Logger { get; private set; }

    private void Awake() {
        Logger = base.Logger;
        Logger.LogInfo("Loaded RiftVibeSolver");

        CaptureSession.NewSession += OnNewSession;
    }

    private static void WriteVibeData(string name, SolverData data) {
        var activations = Solver.Solver.Solve(data, out int score);

        string path;
        int num = 0;

        do {
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RiftVibeSolver", $"{name}_{num}.bin");
            num++;
        } while (!File.Exists(path));

        using var writer = new StreamWriter(File.Create(path));

        foreach (var activation in activations)
            writer.WriteLine(activation.ToString());

        writer.WriteLine();
        writer.WriteLine($"Total bonus score: {score}");
        Logger.LogInfo($"Written vibe data to {path}");
    }

    private static void OnNewSession(CaptureSession session) => session.SessionCompleted += result => WriteVibeData($"{result.Metadata.Name}_{result.Metadata.Difficulty}", new SolverData(result));
}