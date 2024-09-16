// The MIT License(MIT)
//
// Copyright(c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;

namespace LiveChartsCore;

/// <summary>
/// Defines a set a diagonal lines on a tripartite chart.
/// </summary>
/// <typeparam name="TDrawingContext">The type of the drawing context.</typeparam>
public abstract class DiagonalSeparators<TDrawingContext>
    : ChartElement<TDrawingContext>,
        INotifyPropertyChanged
    where TDrawingContext : DrawingContext
{
    private IPaint<TDrawingContext>? _diagonalSeparatorsPaint = null;
    private IPaint<TDrawingContext>? _labelsPaint = null;

    /// <summary>
    /// Gets or sets the diagonal serparators paint.
    /// </summary>
    /// <value>
    /// The diagonal serparators paint.
    /// </value>
    public IPaint<TDrawingContext>? DiagonalSeparatorsPaint
    {
        get => _diagonalSeparatorsPaint;
        set => SetPaintProperty(ref _diagonalSeparatorsPaint, value);
    }

    /// <summary>
    /// Gets or sets the labels paint.
    /// </summary>
    /// <value>
    /// The labels paint.
    /// </value>
    public IPaint<TDrawingContext>? LabelsPaint
    {
        get => _labelsPaint;
        set => SetPaintProperty(ref _labelsPaint, value);
    }

    /// <summary>
    /// Gets the paint tasks.
    /// </summary>
    /// <returns></returns>
    internal override IPaint<TDrawingContext>?[] GetPaintTasks()
    {
        return new[] { _diagonalSeparatorsPaint, _labelsPaint };
    }

    /// <summary>
    /// Called when the fill changes.
    /// </summary>
    /// <param name="propertyName"></param>
    protected override void OnPaintChanged(string? propertyName)
    {
        base.OnPaintChanged(propertyName);
        OnPropertyChanged(propertyName);
    }
}

/// <summary>
/// Defines a draw margin frame visual in a chart.
/// </summary>
/// <typeparam name="TDrawingContext">The type of the drawing context.</typeparam>
/// <typeparam name="TLineGeometry">The type of line geometry.</typeparam>
/// <typeparam name="TTextGeometry">The type of label geometry.</typeparam>
public abstract class DiagonalSeparators<TDrawingContext, TLineGeometry, TTextGeometry>
    : DiagonalSeparators<TDrawingContext>
    where TDrawingContext : DrawingContext
    where TLineGeometry : class, ILineGeometry<TDrawingContext>, new()
    where TTextGeometry : ILabelGeometry<TDrawingContext>, new()
{
    private TLineGeometry? _lineGeometry;
    private TTextGeometry? _textGeometry;
    private bool _isInitialized = false;

    /// <summary>
    /// Invalidates the specified chart.
    /// </summary>
    /// <param name="chart">The chart.</param>
    public override void Invalidate(Chart<TDrawingContext> chart)
    {
        if (
            chart
            is not TripartiteChart<TDrawingContext, TLineGeometry, TTextGeometry> tripartiteChart
        )
            return;

        if (DiagonalSeparatorsPaint is null)
            return;

        var drawMarginLocation = chart.DrawMarginLocation;
        var drawMarginSize = chart.DrawMarginSize;

        var xAxis = tripartiteChart.XAxes[0];
        var yAxis = tripartiteChart.YAxes[0];

        // if neither axis is visible then we don't draw our diagonal lines
        if (!xAxis.IsVisible || !yAxis.IsVisible)
            return;

        // we may yet need this
        //var xScaler = new Scaler(DrawMarginLocation, DrawMarginSize, xAxis);
        //var yScaler = new Scaler(DrawMarginLocation, DrawMarginSize, yAxis);

        var labelAngle = (float)(
            Math.Atan2(
                drawMarginSize.Height - drawMarginLocation.Y,
                drawMarginSize.Width - drawMarginLocation.X
            ) * (180.0 / Math.PI)
        );

        float x,
            y,
            x1,
            y1;
        for (double i = 0; i <= 1; i += 0.1)
        {
            // acceleration go bottom left to top right
            // a = -2*pi*f*v

            // acceleration center to top right
            x = (float)(drawMarginSize.Width * i) + drawMarginLocation.X;
            y = drawMarginLocation.Y;
            x1 = drawMarginSize.Width + drawMarginLocation.X;
            y1 = -(float)(drawMarginSize.Height * i) + drawMarginSize.Height + drawMarginLocation.Y;

            _lineGeometry = new TLineGeometry
            {
                X = x,
                Y = y,
                X1 = x1,
                Y1 = y1,
            };

            DiagonalSeparatorsPaint.AddGeometryToPaintTask(tripartiteChart.Canvas, _lineGeometry);
            tripartiteChart.Canvas.AddDrawableTask(DiagonalSeparatorsPaint);
            _lineGeometry.CompleteTransition(null);

            AddLabelTask(chart, x, y, x1, y1, labelAngle, i);

            // acceleration center to bottom left
            x = (float)(drawMarginSize.Width * i) + drawMarginLocation.X;
            y = drawMarginLocation.Y + drawMarginSize.Height;
            x1 = drawMarginLocation.X;
            y1 = -(float)(drawMarginSize.Height * i) + drawMarginSize.Height + drawMarginLocation.Y;

            _lineGeometry = new TLineGeometry
            {
                X = x,
                Y = y,
                X1 = x1,
                Y1 = y1,
            };

            DiagonalSeparatorsPaint.AddGeometryToPaintTask(tripartiteChart.Canvas, _lineGeometry);
            tripartiteChart.Canvas.AddDrawableTask(DiagonalSeparatorsPaint);
            _lineGeometry.CompleteTransition(null);

            AddLabelTask(chart, x, y, x1, y1, labelAngle, i);

            // displacement go top left to bottom right
            // d = v/(2*pi*f)

            // displacement center to top left
            x = drawMarginLocation.X;
            y = -(float)(drawMarginSize.Height * i) + drawMarginSize.Height + drawMarginLocation.Y;
            x1 = (float)(drawMarginSize.Width * (1 - i)) + drawMarginLocation.X;
            y1 = drawMarginLocation.Y;

            _lineGeometry = new TLineGeometry
            {
                X = x,
                Y = y,
                X1 = x1,
                Y1 = y1,
            };

            // displacement center to bottom right
            DiagonalSeparatorsPaint.AddGeometryToPaintTask(tripartiteChart.Canvas, _lineGeometry);
            tripartiteChart.Canvas.AddDrawableTask(DiagonalSeparatorsPaint);
            _lineGeometry.CompleteTransition(null);

            AddLabelTask(chart, x, y, x1, y1, -labelAngle, i);

            x = drawMarginLocation.X + drawMarginSize.Width;
            y = -(float)(drawMarginSize.Height * i) + drawMarginSize.Height + drawMarginLocation.Y;
            x1 = (float)(drawMarginSize.Width * (1 - i)) + drawMarginLocation.X;
            y1 = drawMarginLocation.Y + drawMarginSize.Height;

            _lineGeometry = new TLineGeometry
            {
                X = x,
                Y = y,
                X1 = x1,
                Y1 = y1,
            };

            DiagonalSeparatorsPaint.AddGeometryToPaintTask(tripartiteChart.Canvas, _lineGeometry);
            tripartiteChart.Canvas.AddDrawableTask(DiagonalSeparatorsPaint);
            _lineGeometry.CompleteTransition(null);

            AddLabelTask(chart, x, y, x1, y1, -labelAngle, i);
        }

        if (!_isInitialized)
        {
            _lineGeometry?.Animate(chart);
            _textGeometry?.Animate(chart);
            _isInitialized = true;
        }
    }

    private const double LOWERBOUNDS = .1;
    private const double UPPERBOUNDS = .8;

    private void AddLabelTask(
        Chart<TDrawingContext> chart,
        float x,
        float y,
        float x1,
        float y1,
        float angle,
        double iteration
    )
    {
        // the numbers keep the labels from clipping
        if (LabelsPaint is not null && iteration > LOWERBOUNDS && iteration < UPPERBOUNDS)
        {
            _textGeometry = new TTextGeometry
            {
                Text = "xxxxxx in.",
                X = (x1 - x) / 2 + x,
                Y = (y1 - y) / 2 + y,
                RotateTransform = angle,
            };

            LabelsPaint.AddGeometryToPaintTask(chart.Canvas, _textGeometry);
            chart.Canvas.AddDrawableTask(LabelsPaint);
            _textGeometry.CompleteTransition(null);
        }
    }
}
