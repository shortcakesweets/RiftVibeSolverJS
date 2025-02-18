using System.Drawing;

namespace RiftVibeSolver.Visualizer;

public class BeatGrid : Drawable {
    private static readonly Pen PEN = new(Color.Black);

    private readonly float bottom;
    private readonly float top;
    private readonly float horizontalBars;
    private readonly int ticksPerMajorTick;
    private readonly double beatLength;
    private readonly double[] beatTimings;

    public BeatGrid(float bottom, float top, float horizontalBars, int ticksPerMajorTick, int beatLength, double[] beatTimings) : base(0d, double.PositiveInfinity, DrawLayer.Grid) {
        this.top = top;
        this.bottom = bottom;
        this.horizontalBars = horizontalBars;
        this.ticksPerMajorTick = ticksPerMajorTick;
        this.beatLength = beatLength;
        this.beatTimings = beatTimings;
    }

    public override void Draw(GraphicsPanel panel, Graphics graphics) {
        float bottomY = panel.ValueToY(bottom);
        float topY = panel.ValueToY(top);

        for (int i = 0;; i++) {
            double time;

            if (beatTimings.Length <= 1)
                time = i * beatLength;
            else if (i < beatTimings.Length)
                time = beatTimings[i];
            else
                time = beatTimings[beatTimings.Length - 1] + (i - (beatTimings.Length - 1)) * (beatTimings[beatTimings.Length - 1] - beatTimings[beatTimings.Length - 2]);

            if (time < panel.Scroll)
                continue;

            if (time > panel.RightBound)
                break;

            float x = panel.TimeToX(time);

            if (i % ticksPerMajorTick == 0)
                PEN.Color = Color.DimGray;
            else
                PEN.Color = Color.FromArgb(255, 32, 32, 32);

            graphics.DrawLine(PEN, x, bottomY, x, topY);
        }

        PEN.Color = Color.FromArgb(255, 32, 32, 32);

        float spacing = (top - bottom) / (horizontalBars - 1);

        for (int i = 0; i < horizontalBars; i++) {
            float y = panel.ValueToY(bottom + spacing * i);

            graphics.DrawLine(PEN, panel.PaddingX, y, panel.PaddingX + panel.Width, y);
        }
    }
}