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
    public string DisplacementUnit { get; set; }
    public double DisplacementScale { get; set; } = 1.0;
    public string AccelerationUnit { get; set; }
    public double AccelerationScale { get; set; } = 1.0;

    public TripartiteUnitOption? TripartiteUnitOption { get; } = null;

    public TripartiteUnit(
        string displacementUnit,
        string accelerationUnit,
        double displacementScale = 1.0,
        double accelerationScale = 1.0
    )
    {
        DisplacementUnit = displacementUnit;
        AccelerationUnit = accelerationUnit;
        DisplacementScale = displacementScale;
        AccelerationScale = accelerationScale;
    }

    public TripartiteUnit(TripartiteUnitOption option)
    {
        var tripartiteUnit = TripartiteUnitProvider.GetUnits(option);
        DisplacementUnit = tripartiteUnit.DisplacementUnit;
        AccelerationUnit = tripartiteUnit.AccelerationUnit;
        DisplacementScale = tripartiteUnit.DisplacementScale;
        AccelerationScale = tripartiteUnit.AccelerationScale;
        TripartiteUnitOption = option;
    }
}
