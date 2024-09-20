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
        // Check if the number is less than 0.0001
        if (Math.Abs(number) < 0.0001)
        {
            // Convert to scientific notation
            formattedNumber = number.ToString("E4"); // "E4" means scientific notation with 4 decimal places
        }
        else
        {
            // Use the original number as a string
            formattedNumber = Math.Round(number, 4).ToString();
        }
        return formattedNumber;
    }

    public static double GetDisplacement(double x, double y, TripartiteUnit tripartiteUnits)
    {
        // d = v / (2 * pi * f).
        return y
            / (2 * Math.PI * (tripartiteUnits.IsXReciprocal ? 1 / x : x))
            / tripartiteUnits.DisplacementScale;
    }

    public static double GetPseudoVelocityFromDisplacement(
        double x,
        double d,
        TripartiteUnit tripartiteUnits
    )
    {
        // y = d * 2 * pi * x .
        return d
            * 2
            * Math.PI
            * (tripartiteUnits.IsXReciprocal ? 1 / x : x)
            * tripartiteUnits.DisplacementScale;
    }

    public static double GetFrequencyFromDisplacement(
        double y,
        double d,
        TripartiteUnit tripartiteUnits
    )
    {
        // x = (y / d * 2 * pi ).
        return tripartiteUnits.IsXReciprocal
            ? d * 2 * Math.PI * tripartiteUnits.DisplacementScale / y
            : y / (d * 2 * Math.PI * tripartiteUnits.DisplacementScale);
    }

    public static double GetAcceleration(double x, double y, TripartiteUnit tripartiteUnits)
    {
        // a = -2 * pi * f * v
        return y
            * (2 * Math.PI * (tripartiteUnits.IsXReciprocal ? 1 / x : x))
            / tripartiteUnits.AccelerationScale;
    }

    public static double GetFrequencyFromAcceleration(
        double y,
        double a,
        TripartiteUnit tripartiteUnits
    )
    {
        // x = a / (y * 2 * pi)
        return (
            tripartiteUnits.IsXReciprocal
                ? y * 2 * Math.PI / (a * tripartiteUnits.AccelerationScale)
                : a * tripartiteUnits.AccelerationScale / (y * 2 * Math.PI)
        );
    }

    public static double GetPseudoVelocityFromAcceleration(
        double x,
        double a,
        TripartiteUnit tripartiteUnits
    )
    {
        // y = a  / (2 * pi * x)
        return a
            * tripartiteUnits.AccelerationScale
            / (2 * Math.PI * (tripartiteUnits.IsXReciprocal ? 1 / x : x));
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
