﻿// The MIT License(MIT)

// Copyright(c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using LiveChartsCore.Kernel;
using LiveChartsCore.Drawing;
using LiveChartsCore.Drawing.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LiveChartsCore.Measure;

namespace LiveChartsCore
{
    public class Axis<TDrawingContext, TTextGeometry, TLineGeometry> : IAxis<TDrawingContext>
        where TDrawingContext : DrawingContext
        where TTextGeometry : ILabelGeometry<TDrawingContext>, new()
        where TLineGeometry : ILineGeometry<TDrawingContext>, new()
    {
        private const float wedgeLength = 8;
        internal AxisOrientation orientation;
        private double step = double.NaN;
        private Bounds? dataBounds = null;
        private Bounds? previousDataBounds = null;
        private double labelsRotation;
        private readonly Dictionary<string, AxisVisualSeprator<TDrawingContext>> activeSeparators =
            new Dictionary<string, AxisVisualSeprator<TDrawingContext>>();
        // xo (x origin) and yo (y origin) are the distance to the center of the axis to the control bounds
        internal float xo = 0f, yo = 0f;
        private AxisPosition position = AxisPosition.LeftOrBottom;
        private Func<double, AxisTick, string>? labeler;
        private Padding padding = new Padding { Left = 8, Top = 8, Bottom = 8, Right = 9 };
        private double? minValue = null;
        private double? maxValue = null;
        private IDrawableTask<TDrawingContext>? textBrush;

        public Bounds? PreviousDataBounds => previousDataBounds;

        public Bounds DataBounds
        {
            get => dataBounds ??= new Bounds();
            private set
            {
                previousDataBounds = dataBounds;
                dataBounds = value;
            }
        }

        public AxisOrientation Orientation { get => orientation; }
        float IAxis.Xo { get => xo; set => xo = value; }
        float IAxis.Yo { get => yo; set => yo = value; }

        public Padding Padding { get => padding; set => padding = value; }
        public Func<double, AxisTick, string> Labeler { get => labeler ?? Labelers.Default; set => labeler = value; }

        public double Step { get => step; set => step = value; }

        public double? MinValue { get => minValue; set => minValue = value; }
        public double? MaxValue { get => maxValue; set => maxValue = value; }

        public double UnitWith { get; set; } = 1;

        public AxisPosition Position { get => position; set => position = value; }
        public double LabelsRotation { get => labelsRotation; set => labelsRotation = value; }

        public IDrawableTask<TDrawingContext>? TextBrush
        {
            get => textBrush;
            set
            {
                textBrush = value;
            }
        }
        public double TextSize { get; set; } = 16;

        public IDrawableTask<TDrawingContext>? SeparatorsBrush { get; set; }

        public bool ShowSeparatorLines { get; set; } = true;
        public bool ShowSeparatorWedges { get; set; } = true;

        public IDrawableTask<TDrawingContext>? AlternativeSeparatorForeground { get; set; }

        public bool IsInverted { get; set; }

        public void Measure(CartesianChart<TDrawingContext> chart)
        {
            var controlSize = chart.ControlSize;
            var drawLocation = chart.DrawMaringLocation;
            var drawMarginSize = chart.DrawMarginSize;
            var labeler = Labeler;

            var scale = new Scaler(drawLocation, drawMarginSize, orientation, DataBounds, IsInverted);
            var previousSacale = previousDataBounds == null
                ? null
                : new Scaler(drawLocation, drawMarginSize, orientation, previousDataBounds, IsInverted);
            var axisTick = this.GetTick(drawMarginSize);

            var s = double.IsNaN(step) || step == 0
                ? axisTick.Value
                : step;

            if (TextBrush != null)
            {
                TextBrush.ZIndex = -1;
                chart.Canvas.AddDrawableTask(TextBrush);
            }
            if (SeparatorsBrush != null)
            {
                SeparatorsBrush.ZIndex = -1;
                chart.Canvas.AddDrawableTask(SeparatorsBrush);
            }

            var lyi = drawLocation.Y;
            var lyj = drawLocation.Y + drawMarginSize.Height;
            var lxi = drawLocation.X;
            var lxj = drawLocation.X + drawMarginSize.Width;

            float xoo = 0f, yoo = 0f;

            if (orientation == AxisOrientation.X)
            {
                yoo = position == AxisPosition.LeftOrBottom
                     ? controlSize.Height - yo
                     : yo;
            }
            else
            {
                xoo = position == AxisPosition.LeftOrBottom
                    ? xo
                    : controlSize.Width - xo;
            }

            var size = (float)TextSize;
            var r = (float)labelsRotation;
            var hasRotation = Math.Abs(r) > 0.01f;

            var start = Math.Truncate(DataBounds.min / s) * s;

            for (var i = start; i <= DataBounds.max; i += s)
            {
                if (i < DataBounds.min) continue;

                var label = labeler(i, axisTick);
                float x, y;
                if (orientation == AxisOrientation.X)
                {
                    x = scale.ToPixels((float)i);
                    y = yoo;
                }
                else
                {
                    x = xoo;
                    y = scale.ToPixels((float)i);
                }

                if (!activeSeparators.TryGetValue(label, out var visualSeparator))
                {
                    visualSeparator = new AxisVisualSeprator<TDrawingContext>() { Value = (float)i };

                    if (TextBrush != null)
                    {
                        var textGeometry = new TTextGeometry { TextSize = size };
                        visualSeparator.Text = textGeometry;
                        if (hasRotation) textGeometry.Rotation = r;

                        textGeometry
                            .TransitionateProperties(
                                nameof(textGeometry.X),
                                nameof(textGeometry.Y))
                            .WithAnimation(animation =>
                                animation
                                    .WithDuration(chart.AnimationsSpeed)
                                    .WithEasingFunction(chart.EasingFunction));

                        if (previousSacale != null)
                        {
                            float xi, yi;

                            if (orientation == AxisOrientation.X)
                            {
                                xi = previousSacale.ToPixels((float)i);
                                yi = yoo;
                            }
                            else
                            {
                                xi = xoo;
                                yi = previousSacale.ToPixels((float)i);
                            }

                            textGeometry.X = xi;
                            textGeometry.Y = yi;
                            textGeometry.CompleteAllTransitions();
                        }

                        TextBrush.AddGeometyToPaintTask(textGeometry);
                    }

                    if (SeparatorsBrush != null)
                    {
                        var lineGeometry = new TLineGeometry();

                        visualSeparator.Line = lineGeometry;

                        lineGeometry
                            .TransitionateProperties(
                                nameof(lineGeometry.X), nameof(lineGeometry.X1),
                                nameof(lineGeometry.Y), nameof(lineGeometry.Y1))
                            .WithAnimation(animation =>
                                animation
                                    .WithDuration(chart.AnimationsSpeed)
                                    .WithEasingFunction(chart.EasingFunction));

                        if (previousSacale != null)
                        {
                            float xi, yi;

                            if (orientation == AxisOrientation.X)
                            {
                                xi = previousSacale.ToPixels((float)i);
                                yi = yoo;
                            }
                            else
                            {
                                xi = xoo;
                                yi = previousSacale.ToPixels((float)i);
                            }

                            if (orientation == AxisOrientation.X)
                            {
                                lineGeometry.X = xi;
                                lineGeometry.X1 = xi;
                                lineGeometry.Y = lyi;
                                lineGeometry.Y1 = lyj;
                            }
                            else
                            {
                                lineGeometry.X = lxi;
                                lineGeometry.X1 = lxj;
                                lineGeometry.Y = yi;
                                lineGeometry.Y1 = yi;
                            }

                            lineGeometry.CompleteAllTransitions();
                        }

                        SeparatorsBrush.AddGeometyToPaintTask(lineGeometry);
                    }

                    activeSeparators.Add(label, visualSeparator);
                }

                if (visualSeparator.Text != null)
                {
                    visualSeparator.Text.Text = label;
                    visualSeparator.Text.Padding = padding;
                    visualSeparator.Text.X = x;
                    visualSeparator.Text.Y = y;
                    if (hasRotation) visualSeparator.Text.Rotation = r;

                    if (previousDataBounds == null) visualSeparator.Text.CompleteAllTransitions();
                }

                if (visualSeparator.Line != null)
                {
                    if (orientation == AxisOrientation.X)
                    {
                        visualSeparator.Line.X = x;
                        visualSeparator.Line.X1 = x;
                        visualSeparator.Line.Y = lyi;
                        visualSeparator.Line.Y1 = lyj;
                    }
                    else
                    {
                        visualSeparator.Line.X = lxi;
                        visualSeparator.Line.X1 = lxj;
                        visualSeparator.Line.Y = y;
                        visualSeparator.Line.Y1 = y;
                    }

                    if (previousDataBounds == null) visualSeparator.Line.CompleteAllTransitions();
                }

                if (visualSeparator.Text != null) chart.MeasuredDrawables.Add(visualSeparator.Text);
                if (visualSeparator.Line != null) chart.MeasuredDrawables.Add(visualSeparator.Line);
            }

            foreach (var separator in activeSeparators.ToArray())
            {
                var usedLabel = separator.Value.Text != null && chart.MeasuredDrawables.Contains(separator.Value.Text);
                var usedLine = separator.Value.Line != null && chart.MeasuredDrawables.Contains(separator.Value.Line);
                if (usedLine || usedLabel)
                {
                    continue;
                }

                float x, y;
                if (orientation == AxisOrientation.X)
                {
                    x = scale.ToPixels((float)separator.Value.Value);
                    y = yoo;
                }
                else
                {
                    x = xoo;
                    y = scale.ToPixels((float)separator.Value.Value);
                }

                if (separator.Value.Line != null)
                {
                    if (orientation == AxisOrientation.X)
                    {
                        separator.Value.Line.X = x;
                        separator.Value.Line.X1 = x;
                        separator.Value.Line.Y = lyi;
                        separator.Value.Line.Y1 = lyj;
                    }
                    else
                    {
                        separator.Value.Line.X = lxi;
                        separator.Value.Line.X1 = lxj;
                        separator.Value.Line.Y = y;
                        separator.Value.Line.Y1 = y;
                    }

                    separator.Value.Line.RemoveOnCompleted = true;
                }

                if (separator.Value.Text != null)
                {
                    separator.Value.Text.X = x;
                    separator.Value.Text.Y = y;
                    separator.Value.Text.RemoveOnCompleted = true;
                }

                activeSeparators.Remove(separator.Key);
            }
        }

        public SizeF GetPossibleSize(CartesianChart<TDrawingContext> chart)
        {
            if (TextBrush == null) return new SizeF(0f, 0f);

            var ts = (float)TextSize;
            var labeler = Labeler;
            var axisTick = this.GetTick(chart.DrawMarginSize);
            var s = double.IsNaN(step) || step == 0
                ? axisTick.Value
                : step;
            var start = Math.Truncate(DataBounds.min / s) * s;

            var w = 0f;
            var h = 0f;
            var r = (float)LabelsRotation;

            for (var i = start; i <= DataBounds.max; i += s)
            {
                var textGeometry = new TTextGeometry
                {
                    Text = labeler(i, axisTick),
                    TextSize = ts,
                    Rotation = r,
                    Padding = padding
                };
                var m = textGeometry.Measure(TextBrush); // TextBrush.MeasureText(labeler(i, axisTick));
                if (m.Width > w) w = m.Width;
                if (m.Height > h) h = m.Height;
            }

            return new SizeF(w, h);
        }

        public void Initialize(AxisOrientation orientation)
        {
            this.orientation = orientation;
            DataBounds = new Bounds();
        }

        public void Dispose()
        {
        }
    }
}