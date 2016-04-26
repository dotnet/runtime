// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;

namespace System.Tests
{
    public class Perf_DateTime
    {
        [Benchmark]
        public void GetNow()
        {
            DateTime dt;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                    {
                        dt = DateTime.Now; dt = DateTime.Now; dt = DateTime.Now;
                        dt = DateTime.Now; dt = DateTime.Now; dt = DateTime.Now;
                        dt = DateTime.Now; dt = DateTime.Now; dt = DateTime.Now;
                    }
        }

        [Benchmark]
        public void GetUtcNow()
        {
            DateTime dt;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                    {
                        dt = DateTime.UtcNow; dt = DateTime.UtcNow; dt = DateTime.UtcNow;
                        dt = DateTime.UtcNow; dt = DateTime.UtcNow; dt = DateTime.UtcNow;
                        dt = DateTime.UtcNow; dt = DateTime.UtcNow; dt = DateTime.UtcNow;
                    }
        }

        [Benchmark]
        public void ToString_str()
        {
            DateTime dt = DateTime.Now;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                    {
                        dt.ToString("g"); dt.ToString("g"); dt.ToString("g");
                        dt.ToString("g"); dt.ToString("g"); dt.ToString("g");
                        dt.ToString("g"); dt.ToString("g"); dt.ToString("g");
                    }
        }

        [Benchmark]
        public void op_Subtraction()
        {
            TimeSpan result;
            DateTime date1 = new DateTime(1996, 6, 3, 22, 15, 0);
            DateTime date2 = new DateTime(1996, 12, 6, 13, 2, 0);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 100000; i++)
                    {
                        result = date1 - date2; result = date1 - date2; result = date1 - date2;
                        result = date1 - date2; result = date1 - date2; result = date1 - date2;
                        result = date1 - date2; result = date1 - date2; result = date1 - date2;
                    }
        }
    }
}
