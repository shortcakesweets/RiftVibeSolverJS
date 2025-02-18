using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace RiftVibeSolver.Visualizer;

static class Program {
    private static readonly object LOCK = new();
    private static readonly Regex MATCH_PARAM = new(@"(""(.+?)""|(\w+)?)\s?");

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main() {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Form1());
    }

    public static void Execute(Action action) {
        lock (LOCK)
            action.Invoke();
    }
}