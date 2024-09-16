using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView.Drawing;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.VisualElements;
using SkiaSharp;

namespace LiveChartsCore.SkiaSharpView.WinForms;

/// <inheritdoc cref="ITripartiteChartView{TDrawingContext}" />
public class TripartiteChart
    : CartesianChart,
        ITripartiteChartView<SkiaSharpDrawingContext, LineGeometry>
{
    private readonly CollectionDeepObserver<ISeries> _seriesObserver;
    private readonly CollectionDeepObserver<ICartesianAxis> _xObserver;
    private readonly CollectionDeepObserver<ICartesianAxis> _yObserver;
    private readonly CollectionDeepObserver<Section<SkiaSharpDrawingContext>> _sectionsObserver;
    private IEnumerable<ISeries> _series = new List<ISeries>();
    private IEnumerable<ICartesianAxis> _xAxes = new List<Axis> { new() };
    private IEnumerable<ICartesianAxis> _yAxes = new List<Axis> { new() };
    private IEnumerable<Section<SkiaSharpDrawingContext>> _sections =
        new List<Section<SkiaSharpDrawingContext>>();
    private DrawMarginFrame<SkiaSharpDrawingContext>? _drawMarginFrame;
    private TooltipFindingStrategy _tooltipFindingStrategy = LiveCharts
        .DefaultSettings
        .TooltipFindingStrategy;
    private IPaint<SkiaSharpDrawingContext>? _diagonalAxesPaint;

    /// <summary>
    /// Initializes a new instance of the <see cref="TripartiteChart"/> class.
    /// </summary>
    public TripartiteChart()
        : this(null, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TripartiteChart"/> class.
    /// </summary>
    /// <param name="tooltip">The default tool tip control.</param>
    /// <param name="legend">The default legend control.</param>
    public TripartiteChart(
        IChartTooltip<SkiaSharpDrawingContext>? tooltip = null,
        IChartLegend<SkiaSharpDrawingContext>? legend = null
    )
        : base(tooltip, legend)
    {
        _seriesObserver = new CollectionDeepObserver<ISeries>(
            OnDeepCollectionChanged,
            OnDeepCollectionPropertyChanged,
            true
        );
        _xObserver = new CollectionDeepObserver<ICartesianAxis>(
            OnDeepCollectionChanged,
            OnDeepCollectionPropertyChanged,
            true
        );
        _yObserver = new CollectionDeepObserver<ICartesianAxis>(
            OnDeepCollectionChanged,
            OnDeepCollectionPropertyChanged,
            true
        );
        _sectionsObserver = new CollectionDeepObserver<Section<SkiaSharpDrawingContext>>(
            OnDeepCollectionChanged,
            OnDeepCollectionPropertyChanged,
            true
        );
        XAxes = new List<ICartesianAxis>()
        {
            LiveCharts
                .DefaultSettings.GetProvider<SkiaSharpDrawingContext>()
                .GetDefaultCartesianAxis()
        };
        YAxes = new List<ICartesianAxis>()
        {
            LiveCharts
                .DefaultSettings.GetProvider<SkiaSharpDrawingContext>()
                .GetDefaultCartesianAxis()
        };
        Series = new ObservableCollection<ISeries>();
        VisualElements = new ObservableCollection<ChartElement<SkiaSharpDrawingContext>>();
        DiagonalAxesPaint = new SolidColorPaint();

        var c = Controls[0].Controls[0];

        c.MouseDown += OnMouseDown;
        c.MouseWheel += OnMouseWheel;
        c.MouseUp += OnMouseUp;
    }

    TripartiteChart<SkiaSharpDrawingContext, LineGeometry> ITripartiteChartView<
        SkiaSharpDrawingContext,
        LineGeometry
    >.Core =>
        core is null
            ? throw new Exception("core not found")
            : (TripartiteChart<SkiaSharpDrawingContext, LineGeometry>)core;

    /// <inheritdoc cref="ITripartiteChartView{TDrawingContext}.Series" />
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IEnumerable<ISeries> Series
    {
        get => _series;
        set
        {
            _seriesObserver?.Dispose(_series);
            _seriesObserver?.Initialize(value);
            _series = value;
            OnPropertyChanged();
        }
    }

    /// <inheritdoc cref="ITripartiteChartView{TDrawingContext}.XAxes" />
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IEnumerable<ICartesianAxis> XAxes
    {
        get => _xAxes;
        set
        {
            _xObserver?.Dispose(_xAxes);
            _xObserver?.Initialize(value);
            _xAxes = value;
            OnPropertyChanged();
        }
    }

    /// <inheritdoc cref="ITripartiteChartView{TDrawingContext}.YAxes" />
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IEnumerable<ICartesianAxis> YAxes
    {
        get => _yAxes;
        set
        {
            _yObserver?.Dispose(_yAxes);
            _yObserver?.Initialize(value);
            _yAxes = value;
            OnPropertyChanged();
        }
    }

    /// <inheritdoc cref="ITripartiteChartView{TDrawingContext}.Sections" />
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IEnumerable<Section<SkiaSharpDrawingContext>> Sections
    {
        get => _sections;
        set
        {
            _sectionsObserver?.Dispose(_sections);
            _sectionsObserver?.Initialize(value);
            _sections = value;
            OnPropertyChanged();
        }
    }

    /// <inheritdoc cref="ITripartiteChartView{TDrawingContext}.DrawMarginFrame" />
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DrawMarginFrame<SkiaSharpDrawingContext>? DrawMarginFrame
    {
        get => _drawMarginFrame;
        set
        {
            _drawMarginFrame = value;
            OnPropertyChanged();
        }
    }

    /// <inheritdoc cref="ITripartiteChartView{TDrawingContext}.DiagonalAxesPaint" />
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IPaint<SkiaSharpDrawingContext>? DiagonalAxesPaint
    {
        get => _diagonalAxesPaint;
        set
        {
            _diagonalAxesPaint = value;
            OnPropertyChanged();
        }
    }

    /// <inheritdoc cref="ITripartiteChartView{TDrawingContext}.ZoomMode" />
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ZoomAndPanMode ZoomMode { get; set; } = LiveCharts.DefaultSettings.ZoomMode;

    /// <inheritdoc cref="ITripartiteChartView{TDrawingContext}.ZoomingSpeed" />
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double ZoomingSpeed { get; set; } = LiveCharts.DefaultSettings.ZoomSpeed;

    /// <inheritdoc cref="ITripartiteChartView{TDrawingContext}.TooltipFindingStrategy" />
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TooltipFindingStrategy TooltipFindingStrategy
    {
        get => _tooltipFindingStrategy;
        set
        {
            _tooltipFindingStrategy = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Initializes the core.
    /// </summary>
    protected override void InitializeCore()
    {
        var zoomingSection = new RectangleGeometry();
        // TODO: controls the color of the hightlight when you drag zoom on a chart
        var zoomingSectionPaint = new SolidColorPaint
        {
            IsFill = true,
            Color = new SkiaSharp.SKColor(33, 150, 243, 50),
            //Color = SKColors.Red,
            ZIndex = int.MaxValue
        };
        // TODO: investigate this after 7
        zoomingSectionPaint.AddGeometryToPaintTask(motionCanvas.CanvasCore, zoomingSection);
        motionCanvas.CanvasCore.AddDrawableTask(zoomingSectionPaint);

        core = new TripartiteChart<SkiaSharpDrawingContext, LineGeometry>(
            this,
            config => config.UseDefaults(),
            motionCanvas.CanvasCore,
            zoomingSection
        );
        if (((IChartView)this).DesignerMode)
            return;

        core.Update();
    }

    /// <inheritdoc cref="ITripartiteChartView{TDrawingContext}.ScaleUIPoint(LvcPoint, int, int)" />
    [Obsolete("Use the ScalePixelsToData method instead.")]
    public double[] ScaleUIPoint(LvcPoint point, int xAxisIndex = 0, int yAxisIndex = 0)
    {
        if (core is null)
            throw new Exception("core not found");
        var cartesianCore = (TripartiteChart<SkiaSharpDrawingContext, LineGeometry>)core;
        return cartesianCore.ScaleUIPoint(point, xAxisIndex, yAxisIndex);
    }

    /// <inheritdoc cref="ITripartiteChartView{TDrawingContext}.ScalePixelsToData(LvcPointD, int, int)"/>
    public LvcPointD ScalePixelsToData(LvcPointD point, int xAxisIndex = 0, int yAxisIndex = 0)
    {
        if (core is not TripartiteChart<SkiaSharpDrawingContext, LineGeometry> cc)
            throw new Exception("core not found");
        var xScaler = new Scaler(cc.DrawMarginLocation, cc.DrawMarginSize, cc.XAxes[xAxisIndex]);
        var yScaler = new Scaler(cc.DrawMarginLocation, cc.DrawMarginSize, cc.YAxes[yAxisIndex]);

        return new LvcPointD
        {
            X = xScaler.ToChartValues(point.X),
            Y = yScaler.ToChartValues(point.Y)
        };
    }

    /// <inheritdoc cref="ITripartiteChartView{TDrawingContext}.ScaleDataToPixels(LvcPointD, int, int)"/>
    public LvcPointD ScaleDataToPixels(LvcPointD point, int xAxisIndex = 0, int yAxisIndex = 0)
    {
        if (core is not TripartiteChart<SkiaSharpDrawingContext, LineGeometry> cc)
            throw new Exception("core not found");

        var xScaler = new Scaler(cc.DrawMarginLocation, cc.DrawMarginSize, cc.XAxes[xAxisIndex]);
        var yScaler = new Scaler(cc.DrawMarginLocation, cc.DrawMarginSize, cc.YAxes[yAxisIndex]);

        return new LvcPointD { X = xScaler.ToPixels(point.X), Y = yScaler.ToPixels(point.Y) };
    }

    /// <inheritdoc cref="IChartView{TDrawingContext}.GetPointsAt(LvcPoint, TooltipFindingStrategy)"/>
    public override IEnumerable<ChartPoint> GetPointsAt(
        LvcPoint point,
        TooltipFindingStrategy strategy = TooltipFindingStrategy.Automatic
    )
    {
        if (core is not TripartiteChart<SkiaSharpDrawingContext, LineGeometry> cc)
            throw new Exception("core not found");

        if (strategy == TooltipFindingStrategy.Automatic)
            strategy = cc.Series.GetTooltipFindingStrategy();

        return cc.Series.SelectMany(series => series.FindHitPoints(cc, point, strategy));
    }

    /// <inheritdoc cref="IChartView{TDrawingContext}.GetVisualsAt(LvcPoint)"/>
    public override IEnumerable<VisualElement<SkiaSharpDrawingContext>> GetVisualsAt(LvcPoint point)
    {
        return core is not TripartiteChart<SkiaSharpDrawingContext, LineGeometry> cc
            ? throw new Exception("core not found")
            : cc.VisualElements.SelectMany(visual =>
                ((VisualElement<SkiaSharpDrawingContext>)visual).IsHitBy(core, point)
            );
    }

    private void OnDeepCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged();
    }

    private void OnDeepCollectionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged();
    }

    private void OnMouseWheel(object? sender, MouseEventArgs e)
    {
        if (core is null)
            throw new Exception("core not found");
        var c = (TripartiteChart<SkiaSharpDrawingContext, LineGeometry>)core;
        var p = e.Location;
        c.Zoom(new LvcPoint(p.X, p.Y), e.Delta > 0 ? ZoomDirection.ZoomIn : ZoomDirection.ZoomOut);
    }

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (ModifierKeys > 0)
            return;
        core?.InvokePointerDown(
            new LvcPoint(e.Location.X, e.Location.Y),
            e.Button == MouseButtons.Right
        );
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        base.OnMouseUp(e);
        core?.InvokePointerUp(
            new LvcPoint(e.Location.X, e.Location.Y),
            e.Button == MouseButtons.Right
        );
    }
}
