// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Microsoft.Xunit.Performance;

namespace System.Tests
{
    public class Perf_HashCode
    {
        private static volatile int _valueStorage;

        // Prevents the jitter from eliminating code that
        // we want to test.

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void DontDiscard(int value)
        {
            _valueStorage = value;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int DontFold(int value)
        {
            return value + _valueStorage;
        }

        [Benchmark]
        public void Add()
        { 
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            { 
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 100; i++)
                    { 
                        var hc = new HashCode();
                        for (int j = 0; j < 100; j++)
                        {
                            hc.Add(i); hc.Add(i); hc.Add(i);
                            hc.Add(i); hc.Add(i); hc.Add(i);
                            hc.Add(i); hc.Add(i); hc.Add(i);
                        }
                        DontDiscard(hc.ToHashCode());
                    }
                }
            }
        }

        
        [Benchmark]
        public void Combine_1()
        { 
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            { 
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 10000; i++)
                    { 
                        DontDiscard(HashCode.Combine(
                            DontFold(i)));
                    }
                }
            }
        }


        [Benchmark]
        public void Combine_2()
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 10000; i++)
                    {
                        DontDiscard(HashCode.Combine(
                            DontFold(i),
                            DontFold(i)));
                    }
                }
            }
        }

        [Benchmark]
        public void Combine_3()
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 10000; i++)
                    {
                        DontDiscard(HashCode.Combine(
                            DontFold(i),
                            DontFold(i),
                            DontFold(i)));
                    }
                }
            }
        }

        [Benchmark]
        public void Combine_4()
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 10000; i++)
                    {
                        DontDiscard(HashCode.Combine(
                            DontFold(i),
                            DontFold(i),
                            DontFold(i),
                            DontFold(i)));
                    }
                }
            }
        }

        [Benchmark]
        public void Combine_5()
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 10000; i++)
                    {
                        DontDiscard(HashCode.Combine(
                            DontFold(i),
                            DontFold(i),
                            DontFold(i),
                            DontFold(i),
                            DontFold(i)));
                    }
                }
            }
        }

        [Benchmark]
        public void Combine_6()
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 10000; i++)
                    {
                        DontDiscard(HashCode.Combine(
                            DontFold(i),
                            DontFold(i),
                            DontFold(i),
                            DontFold(i),
                            DontFold(i),
                            DontFold(i)));
                    }
                }
            }
        }

        [Benchmark]
        public void Combine_7()
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 10000; i++)
                    {
                        DontDiscard(HashCode.Combine(
                            DontFold(i),
                            DontFold(i),
                            DontFold(i),
                            DontFold(i),
                            DontFold(i),
                            DontFold(i),
                            DontFold(i)));
                    }
                }
            }
        }

        [Benchmark]
        public void Combine_8()
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < 10000; i++)
                    {
                        DontDiscard(HashCode.Combine(
                            DontFold(i),
                            DontFold(i),
                            DontFold(i),
                            DontFold(i),
                            DontFold(i),
                            DontFold(i),
                            DontFold(i),
                            DontFold(i)));
                    }
                }
            }
        }
    }
}
