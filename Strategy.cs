namespace RiftVibeSolver;

public class Strategy {
    public readonly int Score;
    public readonly VibeActivation Activation;
    public readonly Strategy[] NextStrategies;

    public Strategy(int score, VibeActivation activation, Strategy[] nextStrategies) {
        Score = score;
        Activation = activation;
        NextStrategies = nextStrategies;
    }
}