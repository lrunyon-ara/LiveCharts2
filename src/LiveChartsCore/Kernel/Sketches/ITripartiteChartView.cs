using System;
using System.Collections.Generic;
using LiveChartsCore.Drawing;
using LiveChartsCore.Measure;

namespace LiveChartsCore.Kernel.Sketches;

/// <summary>
/// Defines a Tripartite chart view, this view is able to host one or many series in a Tripartite coordinate system.
/// </summary>
/// <typeparam name="TDrawingContext">The type of the drawing context.</typeparam>
/// <typeparam name="TLineGeometry">The type of the line geometry.</typeparam>
/// <seealso cref="IChartView{TDrawingContext}" />
public interface ITripartiteChartView<TDrawingContext, TLineGeometry, TTextGeometry>
    : ICartesianChartView<TDrawingContext>
    where TDrawingContext : DrawingContext
    where TLineGeometry : class, ILineGeometry<TDrawingContext>, new()
    where TTextGeometry : ILabelGeometry<TDrawingContext>, new()
{
    /// <summary>
    /// Gets the core.
    /// </summary>
    /// <value>
    /// The core.
    /// </value>
    new TripartiteChart<TDrawingContext, TLineGeometry, TTextGeometry> Core { get; }

    /// <summary>
    /// Gets or sets the diagonal separators.
    /// </summary>
    /// <value>
    /// The diagonal separators.
    /// </value>
    DiagonalSeparators<
        TDrawingContext,
        TLineGeometry,
        TTextGeometry
    >? DiagonalSeparators { get; set; }

    /// <summary>
    /// Gets or sets the diagonal separators.
    /// </summary>
    /// <value>
    /// The diagonal separators.
    /// </value>
    TripartiteUnitOption TripartiteUnits { get; set; }
}
