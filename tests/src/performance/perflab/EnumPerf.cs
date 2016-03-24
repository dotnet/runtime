// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance;
using System;
using System.Reflection;
using Xunit;

public enum Color
{
    Black,
    White,
    Red,
    Brown,
    Yellow,
    Purple,
    Orange
}

public class EnumPerf
{
    [Benchmark]
    [InlineData(Color.Red)]
    public static void EnumCompareTo(Color color)
    {
        Color white = Color.White;

        foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
                color.CompareTo(white);
    }

    [Benchmark]
    public static Type ObjectGetType()
    {
        Type tmp = null;
        Color black = Color.Black;

        foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
                tmp = black.GetType();

        return tmp;
    }

    [Benchmark]
    public static Type ObjectGetTypeNoBoxing()
    {
        Type tmp = null;
        object black = Color.Black;

        foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
                tmp = black.GetType();

        return tmp;
    }

    [Benchmark]
    public static bool EnumEquals()
    {
        Color black = Color.Black;
        Color white = Color.White;
        bool tmp = false;

        foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
                tmp = black.Equals(white);

        return tmp;
    }
}
