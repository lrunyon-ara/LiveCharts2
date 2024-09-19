﻿// The MIT License(MIT)
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Helpers;
using LiveChartsCore.Measure;

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
    /// used to store start and end points
    /// </summary>
    public class DiagonalLine
    {
        public LvcPoint Start { get; set; }
        public LvcPoint End { get; set; }

        public string Label { get; set; }

        public DiagonalLine(LvcPoint start, LvcPoint end, string displacement)
        {
            Start = start;
            End = end;
            Label = displacement;
        }
    }

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

        // chart bounds
        var drawMarginLocation = chart.DrawMarginLocation;
        var drawMarginSize = chart.DrawMarginSize;

        // fed into scalers
        var xAxis = tripartiteChart.XAxes[0];
        var yAxis = tripartiteChart.YAxes[0];

        // if neither axis is visible then we don't draw our diagonal lines
        if (!xAxis.IsVisible || !yAxis.IsVisible)
            return;

        // the scalers that translate our pixel values to unit values on chart, and vice versa
        var xScaler = new Scaler(drawMarginLocation, drawMarginSize, xAxis);
        var yScaler = new Scaler(drawMarginLocation, drawMarginSize, yAxis);

        // we calculate the minimum and maximums for each of our axes
        var minFrequency = xAxis.LogBase is not null
            ? Math.Pow(xAxis.LogBase ?? 10, xScaler.ToChartValues(drawMarginLocation.X))
            : xScaler.ToChartValues(drawMarginLocation.X);
        var maxFrequency = xAxis.LogBase is not null
            ? Math.Pow(
                xAxis.LogBase ?? 10,
                xScaler.ToChartValues(drawMarginSize.Width + drawMarginLocation.X)
            )
            : xScaler.ToChartValues(drawMarginSize.Width + drawMarginLocation.X);
        var maxPseudoVelocity = yAxis.LogBase is not null
            ? Math.Pow(yAxis.LogBase ?? 10, yScaler.ToChartValues(drawMarginLocation.Y))
            : yScaler.ToChartValues(drawMarginLocation.Y);
        var minPseudoVelocity = yAxis.LogBase is not null
            ? Math.Pow(
                yAxis.LogBase ?? 10,
                yScaler.ToChartValues(drawMarginSize.Height + drawMarginLocation.Y)
            )
            : yScaler.ToChartValues(drawMarginSize.Height + drawMarginLocation.Y);

        // generates all our displacement/acceleration lines and labels
        var displacementLines = GenerateAccelerationLines(
                minFrequency,
                maxFrequency,
                minPseudoVelocity,
                maxPseudoVelocity,
                10
            )
            .Concat(
                GenerateDisplacementLines(
                    minFrequency,
                    maxFrequency,
                    minPseudoVelocity,
                    maxPseudoVelocity,
                    10
                )
            )
            .ToList();

        float x,
            y,
            x1,
            y1;
        var index = 0;

        foreach (var item in displacementLines)
        {
            x = xAxis.LogBase is not null
                ? (float)xScaler.ToPixels(Math.Log(item.Start.X, xAxis.LogBase ?? 10))
                : xScaler.ToPixels(item.Start.X);
            y = yAxis.LogBase is not null
                ? (float)yScaler.ToPixels(Math.Log(item.Start.Y, yAxis.LogBase ?? 10))
                : yScaler.ToPixels(item.Start.Y);
            x1 = xAxis.LogBase is not null
                ? (float)xScaler.ToPixels(Math.Log(item.End.X, xAxis.LogBase ?? 10))
                : xScaler.ToPixels(item.End.X);
            y1 = yAxis.LogBase is not null
                ? (float)yScaler.ToPixels(Math.Log(item.End.Y, yAxis.LogBase ?? 10))
                : yScaler.ToPixels(item.End.Y);

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

            var angleDegrees = (float)(
                Math.Atan(displacementLines.Count > 1 ? (y1 - y) / (x1 - x) : 0.7002)
                * (180 / Math.PI)
            );

            if (
                LabelsPaint is not null
                // ensure that our middle lines do not have labels
                && index != Math.Ceiling((displacementLines.Count - 1) * .25)
                && index != Math.Floor((displacementLines.Count - 1) * .25)
                && index != Math.Ceiling((displacementLines.Count - 1) * .75)
                && index != Math.Floor((displacementLines.Count - 1) * .75)
            )
            {
                _textGeometry = new TTextGeometry
                {
                    Text = item.Label,
                    TextSize = 16,
                    X = (x1 - x) / 2 + x,
                    Y = (y1 - y) / 2 + y,
                    RotateTransform = angleDegrees,
                };

                LabelsPaint.AddGeometryToPaintTask(chart.Canvas, _textGeometry);
                chart.Canvas.AddDrawableTask(LabelsPaint);
                _textGeometry.CompleteTransition(null);
            }

            index++;
        }

        if (!_isInitialized)
        {
            _lineGeometry?.Animate(chart);
            _textGeometry?.Animate(chart);
            _isInitialized = true;
        }
    }

    #region Helper Functions
    /// <summary>
    /// Function to generate displacement lines
    /// </summary>
    /// <param name="minFrequency"></param>
    /// <param name="maxFrequency"></param>
    /// <param name="minPseudoVelocity"></param>
    /// <param name="maxPseudoVelocity"></param>
    /// <param name="numberOfLines"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static List<DiagonalLine> GenerateDisplacementLines(
        double minFrequency,
        double maxFrequency,
        double minPseudoVelocity,
        double maxPseudoVelocity,
        int numberOfLines
    )
    {
        // initialize return array
        var diagonalLines = new List<DiagonalLine>();

        // Ensure valid input
        if (
            numberOfLines <= 0
            || minFrequency >= maxFrequency
            || minPseudoVelocity >= maxPseudoVelocity
        )
        {
            throw new ArgumentException("Invalid bounds or number of lines.");
        }

        // d = v / (2 * pi * f).
        // step corresponds to the "slices" in between lines
        var minDisplacement = minPseudoVelocity / (2 * Math.PI * maxFrequency);
        var maxDisplacement = maxPseudoVelocity / (2 * Math.PI * minFrequency);
        var displacementStep =
            (Math.Log10(maxDisplacement) - Math.Log10(minDisplacement)) / (numberOfLines + 1);

        double currentDisplacement,
            startFrequency,
            startPseudoVelocity,
            endPseudoVelocity,
            endFrequency;

        // finally, generate lines
        for (var i = 0; i < numberOfLines + 2; i++)
        {
            currentDisplacement = Math.Pow(10, Math.Log10(minDisplacement) + i * displacementStep);

            // start at the minimum frequency, calculate pseudo-acceleration for a fixed displacement
            startFrequency = minFrequency;
            startPseudoVelocity = currentDisplacement * (2 * Math.PI * startFrequency);

            // to prevent clipping on bottom of chart, if our acceleration is below the minimum
            if (startPseudoVelocity < minPseudoVelocity)
            {
                startPseudoVelocity = minPseudoVelocity;
                startFrequency = startPseudoVelocity / (2 * Math.PI * currentDisplacement);
            }

            endPseudoVelocity = maxPseudoVelocity;
            endFrequency = endPseudoVelocity / (2 * Math.PI * currentDisplacement);

            // to prevent clipping on top of chart, if our frequency is above the maximum
            if (endFrequency > maxFrequency)
            {
                endFrequency = maxFrequency;
                endPseudoVelocity = currentDisplacement * (2 * Math.PI * endFrequency);
            }

            // Create diagonal line from min frequency to max frequency
            var startPoint = new LvcPoint(startFrequency, startPseudoVelocity);
            var endPoint = new LvcPoint(endFrequency, endPseudoVelocity);

            diagonalLines.Add(
                new DiagonalLine(
                    startPoint,
                    endPoint,
                    $"{TripartiteHelpers.FormatNumber(currentDisplacement)} in."
                )
            );
        }

        // filter out lines where both points are the same
        return diagonalLines.Where(line => line.Start != line.End).ToList();
    }

    // 1g ≈ 386.4in/sec ^ 2
    private const double GRAVITY = 386.4;

    /// <summary>
    /// Function to generate acceleration lines
    /// </summary>
    /// <param name="minFrequency"></param>
    /// <param name="maxFrequency"></param>
    /// <param name="minPseudoVelocity"></param>
    /// <param name="maxPseudoVelocity"></param>
    /// <param name="numberOfLines"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static List<DiagonalLine> GenerateAccelerationLines(
        double minFrequency,
        double maxFrequency,
        double minPseudoVelocity,
        double maxPseudoVelocity,
        int numberOfLines
    )
    {
        // initialize return array
        var diagonalLines = new List<DiagonalLine>();

        // Ensure valid input
        if (
            numberOfLines <= 0
            || minFrequency >= maxFrequency
            || minPseudoVelocity >= maxPseudoVelocity
        )
        {
            throw new ArgumentException("Invalid bounds or number of lines.");
        }

        // a = -2 * pi * f * v
        // step corresponds to the "slices" in between lines
        var minAcceleration = minPseudoVelocity * (2 * Math.PI * minFrequency) / GRAVITY;
        var maxAcceleration = maxPseudoVelocity * (2 * Math.PI * maxFrequency) / GRAVITY;

        var accelerationStep =
            (Math.Log10(maxAcceleration) - Math.Log10(minAcceleration)) / (numberOfLines + 1);

        double currentAcceleration,
            startFrequency,
            startPseudoVelocity,
            endPseudoVelocity,
            endFrequency;

        // finally, generate lines
        for (var i = 0; i < numberOfLines + 2; i++)
        {
            currentAcceleration = Math.Pow(10, Math.Log10(minAcceleration) + i * accelerationStep);

            // start at the minimum frequency, calculate pseudo-acceleration for a fixed displacement
            startFrequency = minFrequency;
            startPseudoVelocity = currentAcceleration * GRAVITY / (2 * Math.PI * startFrequency);

            // to prevent clipping on top of chart, if our acceleration is above the maximum
            if (startPseudoVelocity > maxPseudoVelocity)
            {
                startPseudoVelocity = maxPseudoVelocity;
                startFrequency =
                    currentAcceleration * GRAVITY / (2 * Math.PI * startPseudoVelocity);
            }

            endPseudoVelocity = minPseudoVelocity;
            endFrequency = currentAcceleration * GRAVITY / (2 * Math.PI * endPseudoVelocity);

            // to prevent clipping on top of chart, if our frequency is above the maximum
            if (endFrequency > maxFrequency)
            {
                endFrequency = maxFrequency;
                endPseudoVelocity = currentAcceleration * GRAVITY / (2 * Math.PI * endFrequency);
            }

            // Create diagonal line from min frequency to max frequency
            var startPoint = new LvcPoint(startFrequency, startPseudoVelocity);
            var endPoint = new LvcPoint(endFrequency, endPseudoVelocity);

            diagonalLines.Add(
                new DiagonalLine(
                    startPoint,
                    endPoint,
                    $"{TripartiteHelpers.FormatNumber(currentAcceleration)} g"
                )
            );
        }

        // filter out lines where both points are the same
        return diagonalLines.Where(line => line.Start != line.End).ToList();
    }
    #endregion
}
