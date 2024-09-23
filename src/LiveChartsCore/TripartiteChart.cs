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
using System.Diagnostics;
using System.Linq;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.Motion;

namespace LiveChartsCore;

/// <summary>
/// Defines a Tripartite chart.
/// </summary>
/// <typeparam name="TDrawingContext">The type of the drawing context.</typeparam>
/// <seealso cref="Chart{TDrawingContext}" />
/// <typeparam name="TLineGeometry">The type of the line geometry.</typeparam>
/// <typeparam name="TTextGeometry">The type of the text geometry.</typeparam>
public class TripartiteChart<TDrawingContext, TLineGeometry, TTextGeometry>
    : CartesianChart<TDrawingContext>
    where TDrawingContext : DrawingContext
    where TLineGeometry : class, ILineGeometry<TDrawingContext>, new()
    where TTextGeometry : ILabelGeometry<TDrawingContext>, new()
{
    private readonly ITripartiteChartView<TDrawingContext, TLineGeometry, TTextGeometry> _chartView;
    private int _nextSeries = 0;
    private double _zoomingSpeed = 0;
    private ZoomAndPanMode _zoomMode;
    private DrawMarginFrame<TDrawingContext>? _previousDrawMarginFrame;
    private const double MaxAxisBound = 0.05;
    private const double MaxAxisActiveBound = 0.15;
    private HashSet<ICartesianAxis<TDrawingContext>> _crosshair = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TripartiteChart{TDrawingContext, TLineGeometry, TTextGeometry}"/> class.
    /// </summary>
    /// <param name="view">The view.</param>
    /// <param name="defaultPlatformConfig">The default platform configuration.</param>
    /// <param name="canvas">The canvas.</param>
    /// <param name="zoomingSection">The zooming section.</param>
    public TripartiteChart(
        ITripartiteChartView<TDrawingContext, TLineGeometry, TTextGeometry> view,
        Action<LiveChartsSettings> defaultPlatformConfig,
        MotionCanvas<TDrawingContext> canvas,
        ISizedGeometry<TDrawingContext>? zoomingSection
    )
        : base(view, defaultPlatformConfig, canvas, zoomingSection)
    {
        _chartView = view;
    }

    ///<inheritdoc cref="Chart{TDrawingContext}.Series"/>
    public override IEnumerable<IChartSeries<TDrawingContext>> Series =>
        _chartView.Series.Cast<IChartSeries<TDrawingContext>>();

    ///<inheritdoc cref="Chart{TDrawingContext}.VisibleSeries"/>
    public override IEnumerable<IChartSeries<TDrawingContext>> VisibleSeries =>
        Series.Where(x => x.IsVisible);

    /// <summary>
    /// Gets the view.
    /// </summary>
    /// <value>
    /// The view.
    /// </value>
    public override IChartView<TDrawingContext> View => _chartView;

    /// <summary>
    /// Gets or sets a value indicating whether this instance is zooming or panning.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance is zooming or panning; otherwise, <c>false</c>.
    /// </value>
    public TripartiteUnit TripartiteUnits { get; private set; } =
        new TripartiteUnit(TripartiteUnitOption.A);

    /// <summary>
    /// Adds a visual element to the chart.
    /// </summary>
    public override void AddVisual(ChartElement<TDrawingContext> element)
    {
        base.AddVisual(element);
    }

    /// <summary>
    /// Finds the points near to the specified location.
    /// </summary>
    /// <param name="pointerPosition">The pointer position.</param>
    /// <returns></returns>
    public override IEnumerable<ChartPoint> FindHoveredPointsBy(LvcPoint pointerPosition)
    {
        var actualStrategy = TooltipFindingStrategy;

        if (actualStrategy == TooltipFindingStrategy.Automatic)
            actualStrategy = VisibleSeries.GetTooltipFindingStrategy();

        return VisibleSeries
            .Where(series => series.IsHoverable)
            .SelectMany(series => series.FindHitPoints(this, pointerPosition, actualStrategy));
    }

    /// <summary>
    /// Scales the specified point to the UI.
    /// </summary>
    /// <param name="point">The point.</param>
    /// <param name="xAxisIndex">Index of the x axis.</param>
    /// <param name="yAxisIndex">Index of the y axis.</param>
    /// <returns></returns>
    public new double[] ScaleUIPoint(LvcPoint point, int xAxisIndex = 0, int yAxisIndex = 0)
    {
        var xAxis = XAxes[xAxisIndex];
        var yAxis = YAxes[yAxisIndex];

        var xScaler = new Scaler(DrawMarginLocation, DrawMarginSize, xAxis);
        var yScaler = new Scaler(DrawMarginLocation, DrawMarginSize, yAxis);

        return new double[] { xScaler.ToChartValues(point.X), yScaler.ToChartValues(point.Y) };
    }

    /// <summary>
    /// Measures this chart.
    /// </summary>
    /// <returns></returns>
    protected internal override void Measure()
    {
#if DEBUG
        if (LiveCharts.EnableLogging)
        {
            Trace.WriteLine(
                $"[Tripartite chart measured]".PadRight(60)
                    + $"tread: {Environment.CurrentManagedThreadId}"
            );
        }
#endif

        if (!IsLoaded)
            return; // <- prevents a visual glitch where the visual call the measure method
        // while they are not visible, the problem is when the control is visible again
        // the animations are not as expected because previously it ran in an invalid case.

        InvokeOnMeasuring();

        if (_preserveFirstDraw)
        {
            _isFirstDraw = true;
            _preserveFirstDraw = false;
        }

        MeasureWork = new object();

        #region shallow copy the current data in the view

        var viewDrawMargin = _chartView.DrawMargin;
        ControlSize = _chartView.ControlSize;

        YAxes = _chartView.YAxes.Cast<ICartesianAxis<TDrawingContext>>().ToArray();
        XAxes = _chartView.XAxes.Cast<ICartesianAxis<TDrawingContext>>().ToArray();

        _zoomingSpeed = _chartView.ZoomingSpeed;
        _zoomMode = _chartView.ZoomMode;

        var theme = LiveCharts.DefaultSettings.GetTheme<TDrawingContext>();

        LegendPosition = _chartView.LegendPosition;
        Legend = _chartView.Legend;

        TooltipPosition = _chartView.TooltipPosition;
        TooltipFindingStrategy = _chartView.TooltipFindingStrategy;
        Tooltip = _chartView.Tooltip;

        TripartiteUnits = _chartView.TripartiteUnits;

        AnimationsSpeed = _chartView.AnimationsSpeed;
        EasingFunction = _chartView.EasingFunction;

        Sections =
            _chartView.Sections?.Where(x => x.IsVisible) ?? Array.Empty<Section<TDrawingContext>>();
        VisualElements = _chartView.VisualElements ?? Array.Empty<ChartElement<TDrawingContext>>();

        #endregion

        SeriesContext = new SeriesContext<TDrawingContext>(VisibleSeries, this);
        var isNewTheme = LiveCharts.DefaultSettings.CurrentThemeId != ThemeId;

        // restart axes bounds and meta data
        foreach (var axis in XAxes)
        {
            var ce = (ChartElement<TDrawingContext>)axis;
            ce._isInternalSet = true;
            axis.Initialize(AxisOrientation.X);
            if (!ce._isThemeSet || isNewTheme)
            {
                theme.ApplyStyleToAxis((IPlane<TDrawingContext>)axis);
                ce._isThemeSet = true;
            }
            ce._isInternalSet = false;
            if (axis.CrosshairPaint is not null)
                _crosshair.Add(axis);
        }
        foreach (var axis in YAxes)
        {
            var ce = (ChartElement<TDrawingContext>)axis;
            ce._isInternalSet = true;
            axis.Initialize(AxisOrientation.Y);
            if (!ce._isThemeSet || isNewTheme)
            {
                theme.ApplyStyleToAxis((IPlane<TDrawingContext>)axis);
                ce._isThemeSet = true;
            }
            ce._isInternalSet = false;
            if (axis.CrosshairPaint is not null)
                _crosshair.Add(axis);
        }

        // get seriesBounds
        SetDrawMargin(ControlSize, new Margin());

        foreach (var series in VisibleSeries.Cast<ICartesianSeries<TDrawingContext>>())
        {
            if (series.SeriesId == -1)
                series.SeriesId = _nextSeries++;

            var ce = (ChartElement<TDrawingContext>)series;
            ce._isInternalSet = true;
            if (!ce._isThemeSet || isNewTheme)
            {
                theme.ApplyStyleToSeries(series);
                ce._isThemeSet = true;
            }

            var xAxis = XAxes[series.ScalesXAt];
            var yAxis = YAxes[series.ScalesYAt];

            var seriesBounds = series.GetBounds(this, xAxis, yAxis).Bounds;
            if (seriesBounds.IsEmpty)
                continue;

            AppendLimits(xAxis, yAxis, seriesBounds);

            ce._isInternalSet = false;
        }

        #region empty bounds

        // prevent the bounds are not empty...

        foreach (var axis in XAxes)
        {
            var ce = (ChartElement<TDrawingContext>)axis;
            ce._isInternalSet = true;

            if (!axis.DataBounds.IsEmpty)
            {
                ce._isInternalSet = false;
                continue;
            }

            var min = 0;
            var max = 10d * axis.UnitWidth;

            axis.DataBounds.AppendValue(max);
            axis.DataBounds.AppendValue(min);
            axis.VisibleDataBounds.AppendValue(max);
            axis.VisibleDataBounds.AppendValue(min);

            if (axis.DataBounds.MinDelta < max)
                axis.DataBounds.MinDelta = max;
            ce._isInternalSet = false;
        }
        foreach (var axis in YAxes)
        {
            var ce = (ChartElement<TDrawingContext>)axis;
            ce._isInternalSet = true;

            if (!axis.DataBounds.IsEmpty)
            {
                ce._isInternalSet = false;
                continue;
            }

            var min = 0;
            var max = 10d * axis.UnitWidth;

            axis.DataBounds.AppendValue(max);
            axis.DataBounds.AppendValue(min);
            axis.VisibleDataBounds.AppendValue(max);
            axis.VisibleDataBounds.AppendValue(min);

            if (axis.DataBounds.MinDelta < max)
                axis.DataBounds.MinDelta = max;
            ce._isInternalSet = false;
        }

        #endregion

        InitializeVisualsCollector();

        // measure and draw title.
        var title = View.Title;
        var m = new Margin();
        float ts = 0f,
            bs = 0f,
            ls = 0f,
            rs = 0f;
        if (title is not null)
        {
            title.ClippingMode = ClipMode.None;
            var titleSize = title.Measure(this);
            m.Top = titleSize.Height;
            ts = titleSize.Height;
            _titleHeight = titleSize.Height;
        }

        // measure and draw legend.
        DrawLegend(ref ts, ref bs, ref ls, ref rs);

        m.Top = ts;
        m.Bottom = bs;
        m.Left = ls;
        m.Right = rs;

        SetDrawMargin(ControlSize, m);

        foreach (var axis in XAxes)
        {
            if (!axis.IsVisible)
                continue;

            if (axis.DataBounds.Max == axis.DataBounds.Min)
            {
                var c = axis.UnitWidth * 0.5;
                axis.DataBounds.Min = axis.DataBounds.Min - c;
                axis.DataBounds.Max = axis.DataBounds.Max + c;
                axis.VisibleDataBounds.Min = axis.VisibleDataBounds.Min - c;
                axis.VisibleDataBounds.Max = axis.VisibleDataBounds.Max + c;
            }

            var drawablePlane = (IPlane<TDrawingContext>)axis;
            var ns = drawablePlane.GetNameLabelSize(this);
            var s = drawablePlane.GetPossibleSize(this);
            axis.Size = s;

            if (axis.Position == AxisPosition.Start)
            {
                if (axis.InLineNamePlacement)
                {
                    var h = s.Height > ns.Height ? s.Height : ns.Height;

                    // X Bottom
                    axis.NameDesiredSize = new LvcRectangle(
                        new LvcPoint(0, ControlSize.Height - h),
                        new LvcSize(ns.Width, h)
                    );
                    axis.LabelsDesiredSize = new LvcRectangle(
                        new LvcPoint(0, axis.NameDesiredSize.Y - h),
                        new LvcSize(ControlSize.Width, s.Height)
                    );

                    axis.Yo = m.Bottom + h * 0.5f;
                    bs = h;
                    m.Bottom = bs;
                    m.Left = ns.Width;
                }
                else
                {
                    // X Bottom
                    axis.NameDesiredSize = new LvcRectangle(
                        new LvcPoint(0, ControlSize.Height - bs - ns.Height),
                        new LvcSize(ControlSize.Width, ns.Height)
                    );
                    axis.LabelsDesiredSize = new LvcRectangle(
                        new LvcPoint(0, axis.NameDesiredSize.Y - s.Height),
                        new LvcSize(ControlSize.Width, s.Height)
                    );

                    axis.Yo = m.Bottom + s.Height * 0.5f + ns.Height;
                    bs += s.Height + ns.Height;
                    m.Bottom = bs;
                    if (s.Width * 0.5f > m.Left)
                        m.Left = s.Width * 0.5f;
                    if (s.Width * 0.5f > m.Right)
                        m.Right = s.Width * 0.5f;
                }
            }
            else
            {
                if (axis.InLineNamePlacement)
                {
                    var h = s.Height > ns.Height ? s.Height : ns.Height;

                    // X Bottom
                    axis.NameDesiredSize = new LvcRectangle(
                        new LvcPoint(0, 0),
                        new LvcSize(ns.Width, h)
                    );
                    axis.LabelsDesiredSize = new LvcRectangle(
                        new LvcPoint(0, axis.NameDesiredSize.Y - h),
                        new LvcSize(ControlSize.Width, s.Height)
                    );

                    axis.Yo = m.Top + h * 0.5f;
                    ts = h;
                    m.Top = ts;
                    m.Left = ns.Width;
                }
                else
                {
                    // X Top
                    axis.NameDesiredSize = new LvcRectangle(
                        new LvcPoint(0, ts),
                        new LvcSize(ControlSize.Width, ns.Height)
                    );
                    axis.LabelsDesiredSize = new LvcRectangle(
                        new LvcPoint(0, ts + ns.Height),
                        new LvcSize(ControlSize.Width, s.Height)
                    );

                    axis.Yo = ts + s.Height * 0.5f + ns.Height;
                    ts += s.Height + ns.Height;
                    m.Top = ts;
                    if (ls + s.Width * 0.5f > m.Left)
                        m.Left = ls + s.Width * 0.5f;
                    if (rs + s.Width * 0.5f > m.Right)
                        m.Right = rs + s.Width * 0.5f;
                }
            }
        }
        foreach (var axis in YAxes)
        {
            if (!axis.IsVisible)
                continue;

            if (axis.DataBounds.Max == axis.DataBounds.Min)
            {
                var c = axis.UnitWidth * 0.5;
                axis.DataBounds.Min = axis.DataBounds.Min - c;
                axis.DataBounds.Max = axis.DataBounds.Max + c;
                axis.VisibleDataBounds.Min = axis.VisibleDataBounds.Min - c;
                axis.VisibleDataBounds.Max = axis.VisibleDataBounds.Max + c;
            }

            var drawablePlane = (IPlane<TDrawingContext>)axis;
            var ns = drawablePlane.GetNameLabelSize(this);
            var s = drawablePlane.GetPossibleSize(this);
            axis.Size = s;
            var w = s.Width;

            if (axis.Position == AxisPosition.Start)
            {
                if (axis.InLineNamePlacement)
                {
                    if (w < ns.Width)
                        w = ns.Width;

                    // Y Left
                    axis.NameDesiredSize = new LvcRectangle(
                        new LvcPoint(ls, 0),
                        new LvcSize(ns.Width, ns.Height)
                    );
                    axis.LabelsDesiredSize = new LvcRectangle(
                        new LvcPoint(ls, 0),
                        new LvcSize(s.Width, ControlSize.Height)
                    );

                    axis.Xo = ls + w * 0.5f;
                    ls += w;
                    m.Top = ns.Height;
                    m.Left = ls;
                }
                else
                {
                    // Y Left
                    axis.NameDesiredSize = new LvcRectangle(
                        new LvcPoint(ls, 0),
                        new LvcSize(ns.Width, ControlSize.Height)
                    );
                    axis.LabelsDesiredSize = new LvcRectangle(
                        new LvcPoint(ls + ns.Width, 0),
                        new LvcSize(s.Width, ControlSize.Height)
                    );

                    axis.Xo = ls + w * 0.5f + ns.Width;
                    ls += w + ns.Width;
                    m.Left = ls;
                    if (s.Height * 0.5f > m.Top)
                    {
                        m.Top = s.Height * 0.5f;
                    }
                    if (s.Height * 0.5f > m.Bottom)
                    {
                        m.Bottom = s.Height * 0.5f;
                    }
                }
            }
            else
            {
                if (axis.InLineNamePlacement)
                {
                    if (w < ns.Width)
                        w = ns.Width;

                    // Y Left
                    axis.NameDesiredSize = new LvcRectangle(
                        new LvcPoint(ControlSize.Width - rs - ns.Width, 0),
                        new LvcSize(ns.Width, ns.Height)
                    );
                    axis.LabelsDesiredSize = new LvcRectangle(
                        new LvcPoint(axis.NameDesiredSize.X - s.Width, 0),
                        new LvcSize(s.Width, ControlSize.Height)
                    );

                    axis.Xo = rs + w * 0.5f;
                    rs += w;
                    m.Top = ns.Height;
                    m.Right = rs;
                }
                else
                {
                    // Y Right
                    axis.NameDesiredSize = new LvcRectangle(
                        new LvcPoint(ControlSize.Width - rs - ns.Width, 0),
                        new LvcSize(ns.Width, ControlSize.Height)
                    );
                    axis.LabelsDesiredSize = new LvcRectangle(
                        new LvcPoint(axis.NameDesiredSize.X - s.Width, 0),
                        new LvcSize(s.Width, ControlSize.Height)
                    );

                    axis.Xo = rs + w * 0.5f + ns.Width;
                    rs += w + ns.Width;
                    m.Right = rs;
                    if (ts + s.Height * 0.5f > m.Top)
                        m.Top = ts + s.Height * 0.5f;
                    if (bs + s.Height * 0.5f > m.Bottom)
                        m.Bottom = bs + s.Height * 0.5f;
                }
            }
        }

        var rm = viewDrawMargin ?? new Margin(Margin.Auto);

        var actualMargin = new Margin(
            Margin.IsAuto(rm.Left) ? m.Left : rm.Left,
            Margin.IsAuto(rm.Top) ? m.Top : rm.Top,
            Margin.IsAuto(rm.Right) ? m.Right : rm.Right,
            Margin.IsAuto(rm.Bottom) ? m.Bottom : rm.Bottom
        );

        SetDrawMargin(ControlSize, actualMargin);

        // invalid dimensions, probably the chart is too small
        // or it is initializing in the UI and has no dimensions yet
        if (DrawMarginSize.Width <= 0 || DrawMarginSize.Height <= 0)
            return;

        UpdateBounds();

        if (title is not null)
        {
            var titleSize = title.Measure(this);
            title.AlignToTopLeftCorner();
            title.X = ControlSize.Width * 0.5f - titleSize.Width * 0.5f;
            title.Y = 0;
            AddVisual(title);
        }

        var totalAxes = XAxes.Concat(YAxes);

        foreach (var axis in totalAxes)
        {
            if (axis.DataBounds.Max == axis.DataBounds.Min)
            {
                var c = axis.DataBounds.Min * 0.3;
                axis.DataBounds.Min = axis.DataBounds.Min - c;
                axis.DataBounds.Max = axis.DataBounds.Max + c;
            }

            // apply padding
            if (axis.MinLimit is null)
            {
                var s = new Scaler(DrawMarginLocation, DrawMarginSize, axis);
                // correction by geometry size
                var p = Math.Abs(
                    s.ToChartValues(axis.DataBounds.RequestedGeometrySize) - s.ToChartValues(0)
                );
                if (axis.DataBounds.PaddingMin > p)
                    p = axis.DataBounds.PaddingMin;
                var ce = (ChartElement<TDrawingContext>)axis;
                ce._isInternalSet = true;
                axis.DataBounds.Min = axis.DataBounds.Min - p;
                axis.VisibleDataBounds.Min = axis.VisibleDataBounds.Min - p;
                ce._isInternalSet = false;
            }

            // apply padding
            if (axis.MaxLimit is null)
            {
                var s = new Scaler(DrawMarginLocation, DrawMarginSize, axis);
                // correction by geometry size
                var p = Math.Abs(
                    s.ToChartValues(axis.DataBounds.RequestedGeometrySize) - s.ToChartValues(0)
                );
                if (axis.DataBounds.PaddingMax > p)
                    p = axis.DataBounds.PaddingMax;
                var ce = (ChartElement<TDrawingContext>)axis;
                ce._isInternalSet = true;
                axis.DataBounds.Max = axis.DataBounds.Max + p;
                axis.VisibleDataBounds.Max = axis.VisibleDataBounds.Max + p;
                ce._isInternalSet = false;
            }

            if (axis.IsVisible)
                AddVisual((ChartElement<TDrawingContext>)axis);
            ((ChartElement<TDrawingContext>)axis).RemoveOldPaints(View); // <- this is probably obsolete.
            // the probable issue is the "IsVisible" property
        }
        foreach (var section in Sections)
            AddVisual(section);
        foreach (var visual in VisualElements)
            AddVisual(visual);
        foreach (var series in VisibleSeries)
        {
            AddVisual((ChartElement<TDrawingContext>)series);
            _drawnSeries.Add(series.SeriesId);
        }

        if (
            _previousDrawMarginFrame is not null
            && _chartView.DrawMarginFrame != _previousDrawMarginFrame
        )
        {
            // probably obsolete?
            // this should be handled by the RegisterAndInvalidateVisual() method.
            _previousDrawMarginFrame.RemoveFromUI(this);
            _previousDrawMarginFrame = null;
        }
        if (_chartView.DrawMarginFrame is not null)
        {
            var ce = (ChartElement<TDrawingContext>)_chartView.DrawMarginFrame;
            if (!ce._isThemeSet || isNewTheme)
            {
                ce._isInternalSet = true;
                theme.ApplyStyleToDrawMargin(_chartView.DrawMarginFrame);
                ce._isThemeSet = true;
                ce._isInternalSet = false;
            }

            AddVisual(_chartView.DrawMarginFrame);
            _previousDrawMarginFrame = _chartView.DrawMarginFrame;
        }

        CollectVisuals();

        foreach (var axis in totalAxes)
        {
            if (!axis.IsVisible)
                continue;

            var ce = (ChartElement<TDrawingContext>)axis;
            ce._isInternalSet = true;
            axis.ActualBounds.HasPreviousState = true;
            ce._isInternalSet = false;
        }

        if (_chartView.DiagonalSeparators is not null)
        {
            var ce = (ChartElement<TDrawingContext>)_chartView.DiagonalSeparators;

            AddVisual(_chartView.DiagonalSeparators);
        }

        ActualBounds.HasPreviousState = true;

        IsZoomingOrPanning = false;
        InvokeOnUpdateStarted();

        if (_isToolTipOpen)
            DrawToolTip();
        ThemeId = LiveCharts.DefaultSettings.CurrentThemeId;

        Canvas.Invalidate();
        _isFirstDraw = false;
    }

    /// <inheritdoc cref="Chart{TDrawingContext}.Unload"/>
    public override void Unload()
    {
        base.Unload();
        _crosshair = new();
        _isFirstDraw = true;
    }

    private LvcPoint? _sectionZoomingStart = null;

    internal override void InvokePointerDown(LvcPoint point, bool isSecondaryAction)
    {
        var tripartiteView =
            (ITripartiteChartView<TDrawingContext, TLineGeometry, TTextGeometry>)View;
        if ((tripartiteView.ZoomMode & ZoomAndPanMode.InvertPanningPointerTrigger) != 0)
            isSecondaryAction = !isSecondaryAction;

        if (isSecondaryAction && _zoomMode != ZoomAndPanMode.None)
        {
            _sectionZoomingStart = point;

            var x = point.X;
            var y = point.Y;

            if (
                x < DrawMarginLocation.X
                || x > DrawMarginLocation.X + DrawMarginSize.Width
                || y < DrawMarginLocation.Y
                || y > DrawMarginLocation.Y + DrawMarginSize.Height
            )
            {
                _sectionZoomingStart = null;
                return;
            }

            _zoomingSection.X = x;
            _zoomingSection.Y = y;

            var xMode = (_zoomMode & ZoomAndPanMode.X) == ZoomAndPanMode.X;
            var yMode = (_zoomMode & ZoomAndPanMode.Y) == ZoomAndPanMode.Y;

            if (!xMode)
            {
                _zoomingSection.X = DrawMarginLocation.X;
                _zoomingSection.Width = DrawMarginSize.Width;
            }

            if (!yMode)
            {
                _zoomingSection.Y = DrawMarginLocation.Y;
                _zoomingSection.Height = DrawMarginSize.Height;
            }

            return;
        }

        base.InvokePointerDown(point, isSecondaryAction);
    }

    internal override void InvokePointerMove(LvcPoint point)
    {
        foreach (var axis in _crosshair)
        {
            axis.InvalidateCrosshair(this, point);
        }

        if (_sectionZoomingStart is not null)
        {
            var xMode = (_zoomMode & ZoomAndPanMode.X) == ZoomAndPanMode.X;
            var yMode = (_zoomMode & ZoomAndPanMode.Y) == ZoomAndPanMode.Y;

            var x = point.X;
            var y = point.Y;

            if (x < DrawMarginLocation.X)
                x = DrawMarginLocation.X;
            if (x > DrawMarginLocation.X + DrawMarginSize.Width)
                x = DrawMarginLocation.X + DrawMarginSize.Width;
            if (y < DrawMarginLocation.Y)
                y = DrawMarginLocation.Y;
            if (y > DrawMarginLocation.Y + DrawMarginSize.Height)
                y = DrawMarginLocation.Y + DrawMarginSize.Height;

            if (xMode)
                _zoomingSection.Width = x - _sectionZoomingStart.Value.X;
            if (yMode)
                _zoomingSection.Height = y - _sectionZoomingStart.Value.Y;

            Canvas.Invalidate();
            return;
        }

        base.InvokePointerMove(point);
    }

    internal override void InvokePointerUp(LvcPoint point, bool isSecondaryAction)
    {
        if (_sectionZoomingStart is not null)
        {
            var xy = Math.Sqrt(
                Math.Pow(point.X - _sectionZoomingStart.Value.X, 2)
                    + Math.Pow(point.Y - _sectionZoomingStart.Value.Y, 2)
            );
            if (xy < 15)
            {
                _zoomingSection.X = -1;
                _zoomingSection.Y = -1;
                _zoomingSection.Width = 0;
                _zoomingSection.Height = 0;
                Update();
                _sectionZoomingStart = null;
                return;
            }
            if ((_zoomMode & ZoomAndPanMode.X) == ZoomAndPanMode.X)
            {
                for (var i = 0; i < XAxes.Length; i++)
                {
                    var x = XAxes[i];

                    var xi = ScaleUIPoint(_sectionZoomingStart.Value, i, 0)[0];
                    var xj = ScaleUIPoint(point, i, 0)[0];

                    double xMax,
                        xMin;

                    if (xi > xj)
                    {
                        xMax = xi;
                        xMin = xj;
                    }
                    else
                    {
                        xMax = xj;
                        xMin = xi;
                    }

                    if (xMax > (x.MaxLimit ?? double.MaxValue))
                        xMax = x.MaxLimit ?? double.MaxValue;
                    if (xMin < (x.MinLimit ?? double.MinValue))
                        xMin = x.MinLimit ?? double.MinValue;

                    var min = x.MinZoomDelta ?? x.DataBounds.MinDelta * 3;

                    if (xMax - xMin > min)
                    {
                        x.MinLimit = xMin;
                        x.MaxLimit = xMax;
                    }
                    else
                    {
                        if (x.MaxLimit is not null && x.MinLimit is not null)
                        {
                            var d = xMax - xMin;
                            var ad = x.MaxLimit - x.MinLimit;
                            var c = (ad - d) * 0.5;

                            x.MinLimit = xMin - c;
                            x.MaxLimit = xMax + c;
                        }
                    }
                }
            }

            if ((_zoomMode & ZoomAndPanMode.Y) == ZoomAndPanMode.Y)
            {
                for (var i = 0; i < YAxes.Length; i++)
                {
                    var y = YAxes[i];

                    var yi = ScaleUIPoint(_sectionZoomingStart.Value, 0, i)[1];
                    var yj = ScaleUIPoint(point, 0, i)[1];

                    double yMax,
                        yMin;

                    if (yi > yj)
                    {
                        yMax = yi;
                        yMin = yj;
                    }
                    else
                    {
                        yMax = yj;
                        yMin = yi;
                    }

                    if (yMax > (y.MaxLimit ?? double.MaxValue))
                        yMax = y.MaxLimit ?? double.MaxValue;
                    if (yMin < (y.MinLimit ?? double.MinValue))
                        yMin = y.MinLimit ?? double.MinValue;

                    var min = y.MinZoomDelta ?? y.DataBounds.MinDelta * 3;

                    if (yMax - yMin > min)
                    {
                        y.MinLimit = yMin;
                        y.MaxLimit = yMax;
                    }
                    else
                    {
                        if (y.MaxLimit is not null && y.MinLimit is not null)
                        {
                            var d = yMax - yMin;
                            var ad = y.MaxLimit - y.MinLimit;
                            var c = (ad - d) * 0.5;

                            y.MinLimit = yMin - c;
                            y.MaxLimit = yMax + c;
                        }
                    }
                }
            }

            _zoomingSection.X = -1;
            _zoomingSection.Y = -1;
            _zoomingSection.Width = 0;
            _zoomingSection.Height = 0;
            _sectionZoomingStart = null;
            return;
        }

        base.InvokePointerUp(point, isSecondaryAction);
    }

    private static void AppendLimits(ICartesianAxis x, ICartesianAxis y, DimensionalBounds bounds)
    {
        x.DataBounds.AppendValue(bounds.SecondaryBounds);
        x.VisibleDataBounds.AppendValue(bounds.VisibleSecondaryBounds);
        y.DataBounds.AppendValue(bounds.PrimaryBounds);
        y.VisibleDataBounds.AppendValue(bounds.VisiblePrimaryBounds);

        foreach (var sharedX in x.SharedWith ?? Enumerable.Empty<ICartesianAxis>())
        {
            sharedX.DataBounds.AppendValue(bounds.SecondaryBounds);
            sharedX.VisibleDataBounds.AppendValue(bounds.VisibleSecondaryBounds);
        }

        foreach (var sharedY in y.SharedWith ?? Enumerable.Empty<ICartesianAxis>())
        {
            sharedY.DataBounds.AppendValue(bounds.PrimaryBounds);
            sharedY.VisibleDataBounds.AppendValue(bounds.VisiblePrimaryBounds);
        }
    }
}
