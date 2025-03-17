using System.Collections.Generic;

namespace RiftVibeSolver.Common;

public class SolverResult {
    public int TotalScore { get; }
    public IReadOnlyList<Activation> SingleVibeActivations { get; }
    public IReadOnlyList<Activation> DoubleVibeActivations { get; }
    public IReadOnlyList<Activation> BestSingleVibeActivations { get; }
    public IReadOnlyList<Activation> BestDoubleVibeActivations { get; }

    public SolverResult(int totalScore, IReadOnlyList<Activation> singleVibeActivations, IReadOnlyList<Activation> doubleVibeActivations, IReadOnlyList<Activation> bestSingleVibeActivations, IReadOnlyList<Activation> bestDoubleVibeActivations) {
        TotalScore = totalScore;
        SingleVibeActivations = singleVibeActivations;
        DoubleVibeActivations = doubleVibeActivations;
        BestSingleVibeActivations = bestSingleVibeActivations;
        BestDoubleVibeActivations = bestDoubleVibeActivations;
    }
}