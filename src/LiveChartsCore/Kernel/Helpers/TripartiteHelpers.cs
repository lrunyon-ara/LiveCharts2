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

using System;
using LiveChartsCore.Drawing;
using LiveChartsCore.Measure;

namespace LiveChartsCore.Kernel.Helpers;

public static class TripartiteHelpers
{
    public static string FormatNumber(double number)
    {
        string formattedNumber;

        if (Math.Abs(number) < 0.0001)
        {
            // Convert to scientific notation with up to 4 decimal places
            formattedNumber = number.ToString("E4");

            // remove trailing zeros in the scientific notation part (before the exponent)
            var exponentIndex = formattedNumber.IndexOf('E');
            var coefficient = formattedNumber.Substring(0, exponentIndex).TrimEnd('0').TrimEnd('.');
            var exponent = formattedNumber.Substring(exponentIndex + 1);
            var exponentValue = int.Parse(exponent);

            formattedNumber = $"{coefficient}E{exponentValue}";
        }
        else
        {
            // Round to 4 decimal places and remove trailing zeros after decimal point
            formattedNumber = Math.Round(number, 4).ToString("0.####");
        }

        return formattedNumber;
    }

    public static double GetDisplacement(double x, double y, TripartiteUnit tripartiteUnits)
    {
        return tripartiteUnits.DisplacementScale * y / x;
    }

    public static double GetPseudoVelocityFromDisplacement(
        double x,
        double d,
        TripartiteUnit tripartiteUnits
    )
    {
        return d * x / tripartiteUnits.DisplacementScale;
    }

    public static double GetFrequencyFromDisplacement(
        double y,
        double d,
        TripartiteUnit tripartiteUnits
    )
    {
        return y * tripartiteUnits.DisplacementScale / d;
    }

    public static double GetAcceleration(double x, double y, TripartiteUnit tripartiteUnits)
    {
        return tripartiteUnits.AccelerationScale * x * y;
    }

    public static double GetFrequencyFromAcceleration(
        double y,
        double a,
        TripartiteUnit tripartiteUnits
    )
    {
        return a / (tripartiteUnits.AccelerationScale * y);
    }

    public static double GetPseudoVelocityFromAcceleration(
        double x,
        double a,
        TripartiteUnit tripartiteUnits
    )
    {
        return a / (tripartiteUnits.AccelerationScale * x);
    }

    public static string GetFormattedDisplacement(
        double x,
        double y,
        TripartiteUnit tripartiteUnits
    )
    {
        return $"{TripartiteHelpers.FormatNumber(TripartiteHelpers.GetDisplacement(
            x,
            y,
            tripartiteUnits
        ))} {tripartiteUnits.DisplacementUnit}";
    }

    public static string GetFormattedAcceleration(
        double x,
        double y,
        TripartiteUnit tripartiteUnits
    )
    {
        return $"{TripartiteHelpers.FormatNumber(TripartiteHelpers.GetAcceleration(
            x,
            y,
            tripartiteUnits
        ))} {tripartiteUnits.AccelerationUnit}";
    }
}
