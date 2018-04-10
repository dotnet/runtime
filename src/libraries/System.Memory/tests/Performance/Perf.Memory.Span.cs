// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.MemoryTests;
using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Memory.Tests
{
    public class Perf_Memory_Span
    {
        private const int InnerCount = 1_000_000;

        [Benchmark(InnerIterationCount = InnerCount)]
        public static void SpanFromDefaultIntegerMemory()
        {
            Memory<int> memory = default;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Span<int> span = memory.Span;
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        public static void SpanFromIntegerArrayBackedMemory()
        {
            int[] a = { 91, 92, -93, 94 };
            var memory = new Memory<int>(a);

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Span<int> span = memory.Span;
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        public static void SpanFromCharArrayBackedMemory()
        {
            char[] a = "9192-9394".ToCharArray();
            var memory = new Memory<char>(a);

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Span<char> span = memory.Span;
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        public static void SpanFromObjectArrayBackedMemory()
        {
            object o1 = new object();
            object o2 = new object();
            object[] a = { o1, o2 };
            var memory = new Memory<object>(a);

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Span<object> span = memory.Span;
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        public static void SpanFromStringBackedMemory()
        {
            string a = "9192-9394";
            ReadOnlyMemory<char> memory = a.AsMemory();

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySpan<char> span = memory.Span;
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        public static void SpanFromIntegerMemoryManager()
        {
            int[] a = { 91, 92, -93, 94 };
            var memory = new Memory<int>(a);
            MemoryManager<int> manager = new CustomMemoryForTest<int>(a);

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Span<int> span = manager.Memory.Span;
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        public static void SpanFromCharMemoryManager()
        {
            char[] a = "9192-9394".ToCharArray();
            var memory = new Memory<char>(a);
            MemoryManager<char> manager = new CustomMemoryForTest<char>(a);

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Span<char> span = manager.Memory.Span;
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        public static void SpanFromObjectMemoryManager()
        {
            object o1 = new object();
            object o2 = new object();
            object[] a = { o1, o2 };
            var memory = new Memory<object>(a);
            MemoryManager<object> manager = new CustomMemoryForTest<object>(a);

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Span<object> span = manager.Memory.Span;
                    }
                }
            }
        }
    }
}
