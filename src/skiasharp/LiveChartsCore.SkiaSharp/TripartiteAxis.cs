using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView.Drawing;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;

namespace LiveChartsCore.SkiaSharpView;

/// <inheritdoc cref="ITripartiteAxis" />
public class TripartiteAxis
    : TripartiteCoreAxis<SkiaSharpDrawingContext, LabelGeometry, LineGeometry> { }
