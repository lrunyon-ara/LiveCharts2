using System;
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
    private static int s_logBase = 10;

    public ISeries[] Series { get; set; } =
        new[]
        {
            new LineSeries<LiveChartsCore.Defaults.ObservablePoint>
            {
                Values = new[]
                {
                    new LiveChartsCore.Defaults.ObservablePoint
                    {
                        X = Math.Log(.3, s_logBase),
                        Y = Math.Log(.2, s_logBase),
                    },
                    new LiveChartsCore.Defaults.ObservablePoint
                    {
                        X = Math.Log(3, s_logBase),
                        Y = Math.Log(4, s_logBase),
                    },
                    new LiveChartsCore.Defaults.ObservablePoint
                    {
                        X = Math.Log(500, s_logBase),
                        Y = Math.Log(.1, s_logBase),
                    },
                },
                Stroke = new SolidColorPaint(SKColors.Red, 2),
                Fill = null,
                GeometrySize = 10,
                GeometryStroke = new SolidColorPaint(SKColors.DarkBlue, 2),
                GeometryFill = new SolidColorPaint(SKColors.White, 2),
                LineSmoothness = 0,
                Name = "Sample Data",
                ClippingMode = LiveChartsCore.Measure.ClipMode.XY,
                ZIndex = 3,
            }
        };

    public DrawMarginFrame DrawMarginFrame { get; set; } =
        new DrawMarginFrame { Stroke = new SolidColorPaint(SKColors.Black, 3), };

    public DiagonalSeparators DiagonalSeparators { get; set; } =
        new DiagonalSeparators
        {
            DiagonalSeparatorsPaint = new SolidColorPaint()
            {
                Color = SKColors.LightGray,
                ZIndex = -1,
                StrokeThickness = 1,
            },
            LabelsPaint = new SolidColorPaint()
            {
                Color = SKColors.LightGray,
                ZIndex = -1,
                StrokeThickness = 1,
            }
        };

    public Axis[] XAxes { get; set; } =
        {
            new LogaritmicAxis(s_logBase)
            {
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { ZIndex = 2, },
                SubseparatorsPaint = new SolidColorPaint(SKColors.LightSlateGray) { ZIndex = 2, },
                MaxLimit = Math.Log(1000, s_logBase),
                MinLimit = Math.Log(.1, s_logBase),
            }
        };

    public Axis[] YAxes { get; set; } =
        {
            new LogaritmicAxis(s_logBase)
            {
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { ZIndex = 2, },
                SubseparatorsPaint = new SolidColorPaint(SKColors.LightSlateGray) { ZIndex = 2, },
                MaxLimit = Math.Log(10, s_logBase),
                MinLimit = Math.Log(.001, s_logBase),
            }
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
