using System.Drawing;

namespace RiftVibeSolver.Visualizer;

public class HitMarker : Drawable {
    private readonly Brush brush;
    private readonly float y;
    private readonly bool givesVibe;
    private readonly Visualizer visualizer;

    public HitMarker(double x, float y, bool givesVibe, Visualizer visualizer) : base(x, x, DrawLayer.HitMarker) {
        brush = givesVibe ? Brushes.Gold : Brushes.White;
        this.y = y;
        this.givesVibe = givesVibe;
        this.visualizer = visualizer;
    }

    public override void Draw(GraphicsPanel panel, Graphics graphics) {
        float drawX = panel.TimeToX(Start);

        if (givesVibe)
            graphics.DrawLine(Pens.Gold, drawX, panel.ValueToY(1f), drawX, panel.ValueToY(0f));

        var span = visualizer.CurrentSpan;
        bool isInSpan = span is not null && Start >= span.Value.StartTime.Time && Start < span.Value.EndTime.Time;

        if (y > 0f)
            graphics.FillRectangle(isInSpan ? Brushes.Red : brush, panel.TimeToX(Start) - 3, panel.ValueToY(y) - 3, 7, 7);
    }
}