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

    public SolidColorPaint DiagonalAxesPaint = new SolidColorPaint()
    {
        Color = SKColors.LightGray,
        ZIndex = -1,
        StrokeThickness = 1,
    };

    public Axis[] XAxes { get; set; } =
        {
            new Axis
            {
                SeparatorsPaint = new SolidColorPaint(SKColors.Blue)
                {
                    ZIndex = 3,
                    StrokeThickness = 2,
                },
            },
        };

    public Axis[] YAxes { get; set; } =
        {
            new Axis
            {
                SeparatorsPaint = new SolidColorPaint(SKColors.Blue)
                {
                    ZIndex = 3,
                    StrokeThickness = 2,
                },
            },
        };

    public LabelVisual Title { get; set; } =
        new LabelVisual
        {
            Text = "My chart title",
            TextSize = 18,
            Padding = new LiveChartsCore.Drawing.Padding(15),
            Paint = new SolidColorPaint(SKColors.DarkSlateGray)
        };
}
