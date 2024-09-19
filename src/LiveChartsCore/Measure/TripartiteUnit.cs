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

namespace LiveChartsCore.Measure;

/// <summary>
/// Defines the Triparitite units.
/// </summary>
public class TripartiteUnit
{
    public string XUnit { get; set; }
    public string YUnit { get; set; }
    public string DisplacementUnit { get; set; }
    public string AccelerationUnit { get; set; }

    //TODO: figure out which we need and don't need
    public double XScale { get; set; } = 1.0;
    public double YScale { get; set; } = 1.0;
    public double DisplacementScale { get; set; } = 1.0;
    public double AccelerationScale { get; set; } = 1.0;

    //TODO: comment
    public TripartiteUnit(
        string xUnit,
        string yUnit,
        string displacementUnit,
        string dccelerationUnit,
        double xScale = 1.0,
        double yScale = 1.0,
        double displacementScale = 1.0,
        double accelerationScale = 1.0
    )
    {
        XUnit = xUnit;
        YUnit = yUnit;
        DisplacementUnit = displacementUnit;
        AccelerationUnit = dccelerationUnit;
        XScale = xScale;
        YScale = yScale;
        DisplacementScale = displacementScale;
        AccelerationScale = accelerationScale;
    }
}
