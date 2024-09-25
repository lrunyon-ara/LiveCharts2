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

namespace LiveChartsCore.Measure;

/// <summary>
/// Defines the Triparitite units.
/// </summary>
public class TripartiteUnitProvider
{
    public static TripartiteUnit GetUnits(TripartiteUnitOption option)
    {
        switch (option)
        {
            case TripartiteUnitOption.A:
                //1g=9.81m/s²×39.37inches/meter≈386.1in/sec².
                return new TripartiteUnit("in.", "g", 0.1591549, 0.01627397);
            case TripartiteUnitOption.B:
                //1g=9.81m/s²×3.28084feet/meter≈32.2ft/sec².
                return new TripartiteUnit("ft.", "g", 0.1591549, 0.19528768);
            case TripartiteUnitOption.C:
                //1g=9.81m/s²×1000mm/meter=9810mm/sec².
                return new TripartiteUnit("mm.", "g", 0.1591549, 0.00064071);
            case TripartiteUnitOption.D:
                //1g=9.81m/s²×100cm/meter=981cm/sec².
                return new TripartiteUnit("cm.", "g", 0.15915491, 0.00640707);
            case TripartiteUnitOption.E:
                //1g=9.81m/s²=9.81cm/sec².
                return new TripartiteUnit("m.", "g", 0.15915491, 0.64070654);
            case TripartiteUnitOption.F:
                return new TripartiteUnit("g", "in", 0.01627397, 0.1591549);
            case TripartiteUnitOption.G:
                return new TripartiteUnit("g", "ft.", 0.19528768, 0.1591549);
            case TripartiteUnitOption.H:
                return new TripartiteUnit("g", "mm.", 0.00064071, 0.1591549);
            case TripartiteUnitOption.I:
                return new TripartiteUnit("g", "cm.", 0.00064071, 0.1591549);
            case TripartiteUnitOption.J:
                return new TripartiteUnit("g", "m.", 0.64070654, 0.1591549);
            default:
                return GetUnits(TripartiteUnitOption.A);
        }
    }
}