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

        // TODO: make this user facing
        var DiagonalSubseperatorsPaint = DiagonalSeparatorsPaint;

        // chart bounds
        var drawMarginLocation = chart.DrawMarginLocation;
        var drawMarginSize = chart.DrawMarginSize;

        // fed into scalers
        var xAxis = tripartiteChart.XAxes[0];
        var yAxis = tripartiteChart.YAxes[0];

        var tripartiteUnits = tripartiteChart.TripartiteUnits;

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

        // TODO: would like to optimize these heavily
        // generates all our displacement/acceleration lines and labels
        var lines = !tripartiteUnits.IsXReciprocal
            ? GenerateAccelerationLines(
                    minFrequency,
                    maxFrequency,
                    minPseudoVelocity,
                    maxPseudoVelocity,
                    tripartiteUnits,
                    xAxis.LogBase ?? 10
                )
                .Concat(
                    GenerateDisplacementLines(
                        minFrequency,
                        maxFrequency,
                        minPseudoVelocity,
                        maxPseudoVelocity,
                        tripartiteUnits,
                        xAxis.LogBase ?? 10
                    )
                )
                .ToList()
            : GenerateAccelerationReciprocalLines(
                    minFrequency,
                    maxFrequency,
                    minPseudoVelocity,
                    maxPseudoVelocity,
                    tripartiteUnits,
                    xAxis.LogBase ?? 10
                )
                .Concat(
                    GenerateDisplacementReciprocalLines(
                        minFrequency,
                        maxFrequency,
                        minPseudoVelocity,
                        maxPseudoVelocity,
                        tripartiteUnits,
                        xAxis.LogBase ?? 10
                    )
                )
                .ToList();

        float x,
            y,
            x1,
            y1;
        float? lastX = null,
            lastY = null,
            lastX1 = null,
            lastY1 = null;
        var index = 0;

        foreach (var item in lines)
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

            //if (DiagonalSeparatorsPaint is not null)
            //    for (int i = 2; i < 6; i++)
            //    {
            //        if (lastX is null || lastY is null || lastX1 is null || lastY1 is null)
            //            return;

            //        // none of these should every equal 0 because of above
            //        float sX = (x - lastX) / i + lastX ?? 0;
            //        float sY = (x - lastY) / i + lastY ?? 0;
            //        float sX1 = (x - lastX1) / i + lastX1 ?? 0;
            //        float sY1 = (x - lastY1) / i + lastY1 ?? 0;

            //        _lineGeometry = new TLineGeometry
            //        {
            //            X = sX,
            //            Y = sY,
            //            X1 = sX1,
            //            Y1 = sY1,
            //        };
            //        DiagonalSubseperatorsPaint.AddGeometryToPaintTask(
            //            tripartiteChart.Canvas,
            //            _lineGeometry
            //        );
            //        tripartiteChart.Canvas.AddDrawableTask(DiagonalSubseperatorsPaint);
            //        _lineGeometry.CompleteTransition(null);
            //    }

            var lx = (x1 - x) / 2 + x;
            var ly = (y1 - y) / 2 + y;
            var padding = 20;

            if (
                LabelsPaint is not null
                // ensure that our middle lines do not have labels
                && index != Math.Ceiling((lines.Count - 1) * .25)
                && index != Math.Floor((lines.Count - 1) * .25)
                && index != Math.Ceiling((lines.Count - 1) * .75)
                && index != Math.Floor((lines.Count - 1) * .75)
                // ensure that our outside lines don't clip
                && lx > drawMarginLocation.X + padding
                && ly > drawMarginLocation.Y + padding
                && lx < (drawMarginLocation.X + drawMarginSize.Width - padding)
                && ly < (drawMarginLocation.Y + drawMarginSize.Height - padding)
            )
            {
                _textGeometry = new TTextGeometry
                {
                    Text = item.Label,
                    TextSize = 16,
                    X = lx,
                    Y = ly,
                    RotateTransform = (float)(
                        Math.Atan(lines.Count > 1 ? (y1 - y) / (x1 - x) : 0.7002) * (180 / Math.PI)
                    ),
                };

                LabelsPaint.AddGeometryToPaintTask(chart.Canvas, _textGeometry);
                chart.Canvas.AddDrawableTask(LabelsPaint);
                _textGeometry.CompleteTransition(null);
            }

            index++;
            lastX = x;
            lastY = y;
            lastX1 = x1;
            lastY1 = y1;
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
        TripartiteUnit tripartiteUnit,
        double logBase = 10
    )
    {
        // initialize return array
        var diagonalLines = new List<DiagonalLine>();

        // Ensure valid input
        if (
            //numberOfLines <= 0 ||
            minFrequency >= maxFrequency
            || minPseudoVelocity >= maxPseudoVelocity
        )
        {
            throw new ArgumentException("Invalid bounds or number of lines.");
        }

        var numberOfLines = 10;

        // d = v / (2 * pi * f).
        // step corresponds to the "slices" in between lines
        var minDisplacement = TripartiteHelpers.GetDisplacement(
            maxFrequency,
            minPseudoVelocity,
            tripartiteUnit
        );
        var maxDisplacement = TripartiteHelpers.GetDisplacement(
            minFrequency,
            maxPseudoVelocity,
            tripartiteUnit
        );

        double startFrequency,
            startPseudoVelocity,
            endPseudoVelocity,
            endFrequency;

        // finally, generate lines
        foreach (
            var currentDisplacement in GetLogarithmicAndHalfSteps(
                logBase,
                minDisplacement,
                maxDisplacement
            )
        )
        {
            startFrequency = minFrequency;
            startPseudoVelocity = TripartiteHelpers.GetPseudoVelocityFromDisplacement(
                startFrequency,
                currentDisplacement,
                tripartiteUnit
            );

            // to prevent clipping on bottom of chart, if our acceleration is below the minimum
            if (startPseudoVelocity < minPseudoVelocity)
            {
                startPseudoVelocity = minPseudoVelocity;
                startFrequency = TripartiteHelpers.GetFrequencyFromDisplacement(
                    startPseudoVelocity,
                    currentDisplacement,
                    tripartiteUnit
                );
            }

            endPseudoVelocity = maxPseudoVelocity;
            endFrequency = TripartiteHelpers.GetFrequencyFromDisplacement(
                endPseudoVelocity,
                currentDisplacement,
                tripartiteUnit
            );

            // to prevent clipping on top of chart, if our frequency is above the maximum
            if (endFrequency > maxFrequency)
            {
                endFrequency = maxFrequency;
                endPseudoVelocity = TripartiteHelpers.GetPseudoVelocityFromDisplacement(
                    endFrequency,
                    currentDisplacement,
                    tripartiteUnit
                );
            }

            // Create diagonal line from min frequency to max frequency
            var startPoint = new LvcPoint(startFrequency, startPseudoVelocity);
            var endPoint = new LvcPoint(endFrequency, endPseudoVelocity);

            diagonalLines.Add(
                new DiagonalLine(
                    startPoint,
                    endPoint,
                    $"{TripartiteHelpers.FormatNumber(currentDisplacement)} {tripartiteUnit.DisplacementUnit}"
                )
            );
        }

        // filter out lines where both points are the same
        return diagonalLines.Where(line => line.Start != line.End).ToList();
    }

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
    public static List<DiagonalLine> GenerateDisplacementReciprocalLines(
        double minFrequency,
        double maxFrequency,
        double minPseudoVelocity,
        double maxPseudoVelocity,
        TripartiteUnit tripartiteUnit,
        double logBase = 10
    )
    {
        // initialize return array
        var diagonalLines = new List<DiagonalLine>();

        // Ensure valid input
        if (minFrequency >= maxFrequency || minPseudoVelocity >= maxPseudoVelocity)
        {
            throw new ArgumentException("Invalid bounds or number of lines.");
        }

        // a = -2 * pi * f * v
        // step corresponds to the "slices" in between lines
        var minDisplacement = TripartiteHelpers.GetDisplacement(
            minFrequency,
            minPseudoVelocity,
            tripartiteUnit
        );
        var maxDisplacement = TripartiteHelpers.GetDisplacement(
            maxFrequency,
            maxPseudoVelocity,
            tripartiteUnit
        );

        double startFrequency,
            startPseudoVelocity,
            endPseudoVelocity,
            endFrequency;

        // finally, generate lines
        foreach (
            var currentDisplacement in GetLogarithmicAndHalfSteps(
                logBase,
                minDisplacement,
                maxDisplacement
            )
        )
        {
            // start at the minimum frequency, calculate pseudo-acceleration for a fixed displacement
            startFrequency = minFrequency;
            startPseudoVelocity = TripartiteHelpers.GetPseudoVelocityFromDisplacement(
                startFrequency,
                currentDisplacement,
                tripartiteUnit
            );

            // to prevent clipping on top of chart, if our acceleration is above the maximum
            if (startPseudoVelocity > maxPseudoVelocity)
            {
                startPseudoVelocity = maxPseudoVelocity;
                startFrequency = TripartiteHelpers.GetFrequencyFromDisplacement(
                    startPseudoVelocity,
                    currentDisplacement,
                    tripartiteUnit
                );
            }

            endPseudoVelocity = minPseudoVelocity;
            endFrequency = TripartiteHelpers.GetFrequencyFromDisplacement(
                endPseudoVelocity,
                currentDisplacement,
                tripartiteUnit
            );

            // to prevent clipping on top of chart, if our frequency is above the maximum
            if (endFrequency > maxFrequency)
            {
                endFrequency = maxFrequency;
                endPseudoVelocity = TripartiteHelpers.GetPseudoVelocityFromDisplacement(
                    endFrequency,
                    currentDisplacement,
                    tripartiteUnit
                );
            }

            // Create diagonal line from min frequency to max frequency
            var startPoint = new LvcPoint(startFrequency, startPseudoVelocity);
            var endPoint = new LvcPoint(endFrequency, endPseudoVelocity);

            diagonalLines.Add(
                new DiagonalLine(
                    startPoint,
                    endPoint,
                    $"{TripartiteHelpers.FormatNumber(currentDisplacement)} {tripartiteUnit.DisplacementUnit}"
                )
            );
        }

        // filter out lines where both points are the same
        return diagonalLines.Where(line => line.Start != line.End).ToList();
    }

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
        TripartiteUnit tripartiteUnit,
        double logBase = 10
    )
    {
        // initialize return array
        var diagonalLines = new List<DiagonalLine>();

        // Ensure valid input
        if (minFrequency >= maxFrequency || minPseudoVelocity >= maxPseudoVelocity)
        {
            throw new ArgumentException("Invalid bounds or number of lines.");
        }

        // a = -2 * pi * f * v
        // step corresponds to the "slices" in between lines
        var minAcceleration = TripartiteHelpers.GetAcceleration(
            minFrequency,
            minPseudoVelocity,
            tripartiteUnit
        );
        var maxAcceleration = TripartiteHelpers.GetAcceleration(
            maxFrequency,
            maxPseudoVelocity,
            tripartiteUnit
        );

        double startFrequency,
            startPseudoVelocity,
            endPseudoVelocity,
            endFrequency;

        // finally, generate lines
        foreach (
            var currentAcceleration in GetLogarithmicAndHalfSteps(
                logBase,
                minAcceleration,
                maxAcceleration
            )
        )
        {
            // start at the minimum frequency, calculate pseudo-acceleration for a fixed displacement
            startFrequency = minFrequency;
            startPseudoVelocity = TripartiteHelpers.GetPseudoVelocityFromAcceleration(
                startFrequency,
                currentAcceleration,
                tripartiteUnit
            );

            // to prevent clipping on top of chart, if our acceleration is above the maximum
            if (startPseudoVelocity > maxPseudoVelocity)
            {
                startPseudoVelocity = maxPseudoVelocity;
                startFrequency = TripartiteHelpers.GetFrequencyFromAcceleration(
                    startPseudoVelocity,
                    currentAcceleration,
                    tripartiteUnit
                );
            }

            endPseudoVelocity = minPseudoVelocity;
            endFrequency = TripartiteHelpers.GetFrequencyFromAcceleration(
                endPseudoVelocity,
                currentAcceleration,
                tripartiteUnit
            );

            // to prevent clipping on top of chart, if our frequency is above the maximum
            if (endFrequency > maxFrequency)
            {
                endFrequency = maxFrequency;
                endPseudoVelocity = TripartiteHelpers.GetPseudoVelocityFromAcceleration(
                    endFrequency,
                    currentAcceleration,
                    tripartiteUnit
                );
            }

            // Create diagonal line from min frequency to max frequency
            var startPoint = new LvcPoint(startFrequency, startPseudoVelocity);
            var endPoint = new LvcPoint(endFrequency, endPseudoVelocity);

            diagonalLines.Add(
                new DiagonalLine(
                    startPoint,
                    endPoint,
                    $"{TripartiteHelpers.FormatNumber(currentAcceleration)} {tripartiteUnit.AccelerationUnit}"
                )
            );
        }

        // filter out lines where both points are the same
        return diagonalLines.Where(line => line.Start != line.End).ToList();
    }

    // <summary>
    /// Function to generate acceleration when frequency is in seconds
    /// </summary>
    /// <param name="minFrequency"></param>
    /// <param name="maxFrequency"></param>
    /// <param name="minPseudoVelocity"></param>
    /// <param name="maxPseudoVelocity"></param>
    /// <param name="numberOfLines"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static List<DiagonalLine> GenerateAccelerationReciprocalLines(
        double minFrequency,
        double maxFrequency,
        double minPseudoVelocity,
        double maxPseudoVelocity,
        TripartiteUnit tripartiteUnit,
        double logBase = 10
    )
    {
        // initialize return array
        var diagonalLines = new List<DiagonalLine>();

        // Ensure valid input
        if (minFrequency >= maxFrequency || minPseudoVelocity >= maxPseudoVelocity)
        {
            throw new ArgumentException("Invalid bounds or number of lines.");
        }

        // d = v / (2 * pi * f).
        // step corresponds to the "slices" in between lines
        var minAcceleration = TripartiteHelpers.GetAcceleration(
            maxFrequency,
            minPseudoVelocity,
            tripartiteUnit
        );
        var maxAcceleration = TripartiteHelpers.GetAcceleration(
            minFrequency,
            maxPseudoVelocity,
            tripartiteUnit
        );

        double startFrequency,
            startPseudoVelocity,
            endPseudoVelocity,
            endFrequency;

        // finally, generate lines
        foreach (
            var currentAcceleration in GetLogarithmicAndHalfSteps(
                logBase,
                minAcceleration,
                maxAcceleration
            )
        )
        {
            // start at the minimum frequency, calculate pseudo-acceleration for a fixed displacement
            startFrequency = minFrequency;
            startPseudoVelocity = TripartiteHelpers.GetPseudoVelocityFromAcceleration(
                startFrequency,
                currentAcceleration,
                tripartiteUnit
            );

            // to prevent clipping on bottom of chart, if our acceleration is below the minimum
            if (startPseudoVelocity < minPseudoVelocity)
            {
                startPseudoVelocity = minPseudoVelocity;
                startFrequency = TripartiteHelpers.GetFrequencyFromAcceleration(
                    startPseudoVelocity,
                    currentAcceleration,
                    tripartiteUnit
                );
            }

            endPseudoVelocity = maxPseudoVelocity;
            endFrequency = TripartiteHelpers.GetFrequencyFromAcceleration(
                endPseudoVelocity,
                currentAcceleration,
                tripartiteUnit
            );

            // to prevent clipping on top of chart, if our frequency is above the maximum
            if (endFrequency > maxFrequency)
            {
                endFrequency = maxFrequency;
                endPseudoVelocity = TripartiteHelpers.GetPseudoVelocityFromAcceleration(
                    endFrequency,
                    currentAcceleration,
                    tripartiteUnit
                );
            }

            // Create diagonal line from min frequency to max frequency
            var startPoint = new LvcPoint(startFrequency, startPseudoVelocity);
            var endPoint = new LvcPoint(endFrequency, endPseudoVelocity);

            diagonalLines.Add(
                new DiagonalLine(
                    startPoint,
                    endPoint,
                    $"{TripartiteHelpers.FormatNumber(currentAcceleration)} {tripartiteUnit.AccelerationUnit}"
                )
            );
        }

        // filter out lines where both points are the same
        return diagonalLines.Where(line => line.Start != line.End).ToList();
    }

    // takes a min and max number and finds all the full log steps
    public static List<double> GetLogarithmicSteps(double logBase, double start, double end)
    {
        List<double> logarithmicSteps = new List<double>();

        if (logBase <= 1 || start <= 0 || end <= 0 || start >= end)
        {
            throw new ArgumentException("Invalid base or range");
        }

        // Add the start value to the list
        //logarithmicSteps.Add(start);

        // Calculate the logarithmic values of start and end based on the provided base
        double logStart = Math.Log(start, logBase);
        double logEnd = Math.Log(end, logBase);

        // Round the logarithmic values to find powers of the base between start and end
        int firstPower = (int)Math.Ceiling(logStart);
        int lastPower = (int)Math.Floor(logEnd);

        // Generate values using powers of the logBase between the start and end values
        for (int i = firstPower; i <= lastPower; i++)
        {
            logarithmicSteps.Add(Math.Pow(logBase, i));
        }

        // Add the end value to the list
        //logarithmicSteps.Add(end);

        return logarithmicSteps;
    }

    // takes a min and max number and finds all the half log steps
    public static List<double> GetLogarithmicAndHalfSteps(double logBase, double start, double end)
    {
        List<double> steps = new List<double>();

        if (logBase <= 1 || start <= 0 || end <= 0 || start >= end)
        {
            throw new ArgumentException("Invalid base or range");
        }

        // Add the start value to the list
        steps.Add(start);

        // Calculate the logarithmic values of start and end based on the provided base
        double logStart = Math.Log(start, logBase);
        double logEnd = Math.Log(end, logBase);

        // Round the logarithmic values to find powers of the base between start and end
        int firstPower = (int)Math.Ceiling(logStart);
        int lastPower = (int)Math.Floor(logEnd);

        // Generate values using powers of the logBase
        for (int i = firstPower; i <= lastPower; i++)
        {
            double fullStep = Math.Pow(logBase, i);

            // Only add the power of the base if it's between start and end
            if (fullStep > start && fullStep < end)
            {
                steps.Add(fullStep);

                // Add the half step (e.g., 0.5 * current step) if it's in range
                double halfStep = fullStep * 0.5;
                if (halfStep > start && halfStep < end)
                {
                    steps.Add(halfStep);
                }
            }
        }

        // Add the end value to the list
        steps.Add(end);

        // Sort the steps before returning to ensure correct order
        steps.Sort();

        return steps;
    }
    #endregion
}
