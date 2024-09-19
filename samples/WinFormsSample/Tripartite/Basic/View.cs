using System.Windows.Forms;
using LiveChartsCore.SkiaSharpView.WinForms;
using ViewModelsSamples.Tripartite.Basic;

namespace WinFormsSample.Tripartite.Basic;

public partial class View : UserControl
{
    private readonly TripartiteChart tripartiteChart;

    public View()
    {
        InitializeComponent();
        Size = new System.Drawing.Size(70, 70);

        var viewModel = new ViewModel();

        tripartiteChart = new TripartiteChart
        {
            Series = viewModel.Series,
            XAxes = viewModel.XAxes,
            YAxes = viewModel.YAxes,
            DiagonalSeparators = viewModel.DiagonalSeparators,
            TripartiteUnits = viewModel.TripartiteUnits,
            Title = viewModel.Title,
            DrawMarginFrame = viewModel.DrawMarginFrame,
            Location = new System.Drawing.Point(0, 0),
            Size = new System.Drawing.Size(50, 50),
            // TODO:
            ZoomMode = viewModel.ZoomMode,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom
        };

        Controls.Add(tripartiteChart);
    }
}
