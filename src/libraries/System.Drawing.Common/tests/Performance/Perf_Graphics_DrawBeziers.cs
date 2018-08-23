// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.Xunit.Performance;
using Microsoft.DotNet.XUnitExtensions;

namespace System.Drawing.Tests
{
    public class Perf_Graphics_DrawBeziers : RemoteExecutorTestBase
    {
        [Benchmark(InnerIterationCount = 10000)]
        [ConditionalBenchmark(typeof(Helpers), nameof(Helpers.GetGdiplusIsAvailable))]
        public void DrawBezier_Point()
        {
            Random r = new Random(1942);

            using (Bitmap image = new Bitmap(100, 100))
            using (Pen pen = new Pen(Color.White))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                foreach (var iteration in Benchmark.Iterations)
                {
                    using (iteration.StartMeasurement())
                    {
                        for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        {
                            graphics.DrawBezier(pen, new Point(r.Next(100), r.Next(100)), new Point(r.Next(100), r.Next(100)), new Point(r.Next(100), r.Next(100)), new Point(r.Next(100), r.Next(100)));
                        }
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 10000)]
        [ConditionalBenchmark(typeof(Helpers), nameof(Helpers.GetGdiplusIsAvailable))]
        public void DrawBezier_Points()
        {
            Point[] points =
            {
                new Point(10, 10), new Point(20, 1), new Point(35, 5), new Point(50, 10),
                new Point(60, 15), new Point(65, 25), new Point(50, 30)
            };

            using (Bitmap image = new Bitmap(100, 100))
            using (Pen pen = new Pen(Color.Blue))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                foreach (var iteration in Benchmark.Iterations)
                {

                    using (iteration.StartMeasurement())
                    {
                        for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        {
                            graphics.DrawBeziers(pen, points);
                        }
                    }
                }
            }
        }
    }
}
