// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Drawing.Drawing2D;
using Microsoft.Xunit.Performance;
using Microsoft.DotNet.XUnitExtensions;

namespace System.Drawing.Tests
{
    public class Perf_Graphics_Transforms : RemoteExecutorTestBase
    {
        [Benchmark(InnerIterationCount = 10000)]
        [ConditionalBenchmark(typeof(Helpers), nameof(Helpers.GetIsDrawingSupported), nameof(Helpers.IsNotUnix))] // Graphics.TransformPoints is not implemented in libgdiplus yet. See dotnet/corefx 20884
        public void TransformPoints()
        {
            Point[] points =
            {
                new Point(10, 10), new Point(20, 1), new Point(35, 5), new Point(50, 10),
                new Point(60, 15), new Point(65, 25), new Point(50, 30)
            };

            using (Bitmap image = new Bitmap(100, 100))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                foreach (var iteration in Benchmark.Iterations)
                {

                    using (iteration.StartMeasurement())
                    {
                        for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        {
                            graphics.TransformPoints(CoordinateSpace.World, CoordinateSpace.Page, points);
                            graphics.TransformPoints(CoordinateSpace.Device, CoordinateSpace.World, points);
                            graphics.TransformPoints(CoordinateSpace.Page, CoordinateSpace.Device, points);
                        }
                    }
                }
            }
        }
    }
}
