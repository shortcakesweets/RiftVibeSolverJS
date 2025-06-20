class SolverResult {
  constructor(totalScore, singleVibeActivations, doubleVibeActivations, bestSingleVibeActivations, bestDoubleVibeActivations) {
    this.totalScore = totalScore;
    this.singleVibeActivations = singleVibeActivations;
    this.doubleVibeActivations = doubleVibeActivations;
    this.bestSingleVibeActivations = bestSingleVibeActivations;
    this.bestDoubleVibeActivations = bestDoubleVibeActivations;
  }
}

module.exports = SolverResult;
