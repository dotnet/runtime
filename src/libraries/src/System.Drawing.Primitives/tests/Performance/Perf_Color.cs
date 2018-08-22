// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.Xunit.Performance;

namespace System.Drawing.Tests
{
    public class Perf_Color : RemoteExecutorTestBase
    {
        public static readonly Color[] AllKnownColors;

        static Perf_Color()
        {
            AllKnownColors = typeof(Color)
                .GetProperties(BindingFlags.Static | BindingFlags.Public)
                .Where(p => p.PropertyType == typeof(Color))
                .Select(p => (Color)p.GetValue(null))
                .ToArray();
        }

        [Benchmark(InnerIterationCount = 10_000_000)]
        public void FromArgb_Channels()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        int val = i & 0xFF;
                        Color.FromArgb(byte.MaxValue, val, byte.MinValue, val);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 10_000_000)]
        public void FromArgb_AlphaColor()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                var baseColor = Color.DarkSalmon;

                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Color.FromArgb(i & 0xFF, baseColor);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 100_000)]
        public void GetBrightness()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                var colors = AllKnownColors;

                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        for (int j = 0; j < colors.Length; j++)
                        {
                            colors[j].GetBrightness();
                        }
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 100_000)]
        public void GetHue()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                var colors = AllKnownColors;

                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        for (int j = 0; j < colors.Length; j++)
                        {
                            colors[j].GetHue();
                        }
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 100_000)]
        public void GetSaturation()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                var colors = AllKnownColors;

                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        for (int j = 0; j < colors.Length; j++)
                        {
                            colors[j].GetSaturation();
                        }
                    }
                }
            }
        }
    }
}
