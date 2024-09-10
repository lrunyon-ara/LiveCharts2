using System.Collections.ObjectModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.VisualElements;
using SkiaSharp;

namespace ViewModelsSamples.Tripartite.Basic;

public partial class ViewModel : ObservableObject
{
    public ISeries[] Series { get; set; } =
        {
            new LineSeries<double> { Values = new double[] { 2, 1, 3, 5, 3, 4, 6 }, Fill = null }
        };

    public DrawMarginFrame DrawMarginFrame { get; set; } =
        new DrawMarginFrame { Stroke = new SolidColorPaint(SKColors.Black, 3), };

    public TripartiteAxis[] AccelerationAxes { get; set; } =
        {
            new TripartiteAxis
            {
                SeparatorsPaint = new SolidColorPaint(SKColors.Red)
                {
                    ZIndex = 3,
                    StrokeThickness = 5,
                },
            },
        };

    public TripartiteAxis[] VelocityAxes { get; set; } =
        {
            new TripartiteAxis
            {
                SeparatorsPaint = new SolidColorPaint(SKColors.Green)
                {
                    ZIndex = 3,
                    StrokeThickness = 5,
                },
            },
        };

    public LabelVisual Title { get; set; } =
        new LabelVisual
        {
            Text = "My chart title",
            TextSize = 25,
            Padding = new LiveChartsCore.Drawing.Padding(15),
            Paint = new SolidColorPaint(SKColors.DarkSlateGray)
        };
}
