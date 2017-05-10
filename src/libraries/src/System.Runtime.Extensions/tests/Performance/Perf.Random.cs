// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.Xunit.Performance;

namespace System.Tests
{
    public class Perf_Random
    {
        [Benchmark]
        public void ctor()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 40000; i++)
                    {
                        new Random(); new Random(); new Random();
                        new Random(); new Random(); new Random();
                        new Random(); new Random(); new Random();
                    }
        }

        [Benchmark]
        public void Next_int()
        {
            Random rand = new Random(123456);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 40000; i++)
                    {
                        rand.Next(10000); rand.Next(10000); rand.Next(10000);
                        rand.Next(10000); rand.Next(10000); rand.Next(10000);
                        rand.Next(10000); rand.Next(10000); rand.Next(10000);
                    }
        }

        [Benchmark]
        public void Next_int_int()
        {
            Random rand = new Random(123456);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 40000; i++)
                    {
                        rand.Next(100, 10000); rand.Next(100, 10000); rand.Next(100, 10000);
                        rand.Next(100, 10000); rand.Next(100, 10000); rand.Next(100, 10000);
                        rand.Next(100, 10000); rand.Next(100, 10000); rand.Next(100, 10000);
                    }
        }

        [Benchmark]
        public void NextBytes()
        {
            Random rand = new Random(123456);
            byte[] b1 = new byte[1000];
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                    {
                        rand.NextBytes(b1); rand.NextBytes(b1); rand.NextBytes(b1);
                        rand.NextBytes(b1); rand.NextBytes(b1); rand.NextBytes(b1);
                        rand.NextBytes(b1); rand.NextBytes(b1); rand.NextBytes(b1);
                    }
        }

        [Benchmark]
        public void NextDouble()
        {
            Random rand = new Random(123456);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 40000; i++)
                    {
                        rand.NextDouble(); rand.NextDouble(); rand.NextDouble();
                        rand.NextDouble(); rand.NextDouble(); rand.NextDouble();
                        rand.NextDouble(); rand.NextDouble(); rand.NextDouble();
                    }
        }
    }
}
