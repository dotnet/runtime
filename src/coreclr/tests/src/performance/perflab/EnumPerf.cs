// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance;
using System;
using System.Reflection;
using Xunit;

namespace PerfLabTests
{
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
        [Benchmark(InnerIterationCount = 300000)]
        [InlineData(Color.Red)]
        public static void EnumCompareTo(Color color)
        {
            Color white = Color.White;

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        color.CompareTo(white);
        }

        [Benchmark(InnerIterationCount = 300000)]
        public static Type ObjectGetType()
        {
            Type tmp = null;
            Color black = Color.Black;

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        tmp = black.GetType();

            return tmp;
        }

        [Benchmark(InnerIterationCount = 300000)]
        public static Type ObjectGetTypeNoBoxing()
        {
            Type tmp = null;
            object black = Color.Black;

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        tmp = black.GetType();

            return tmp;
        }

        [Benchmark(InnerIterationCount = 300000)]
        public static bool EnumEquals()
        {
            Color black = Color.Black;
            Color white = Color.White;
            bool tmp = false;

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        tmp = black.Equals(white);

            return tmp;
        }
    }
}
