// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Collections.Tests
{
    public class Perf_BitArray
    {
        const int IterationCount = 100_000;

        private static Random s_random = new Random(42);

        [Benchmark(InnerIterationCount = IterationCount)]
        [InlineData(1, true)]
        [InlineData(10, true)]
        [InlineData(32, true)]
        [InlineData(64, true)]
        [InlineData(100, true)]
        [InlineData(128, true)]
        [InlineData(1000, true)]
        [InlineData(1, false)]
        [InlineData(10, false)]
        [InlineData(32, false)]
        [InlineData(64, false)]
        [InlineData(100, false)]
        [InlineData(128, false)]
        [InlineData(1000, false)]
        public void BitArrayLengthCtor(int size, bool value)
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        var local = new BitArray(size, value);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = IterationCount)]
        [InlineData(1, true)]
        [InlineData(2, true)]
        [InlineData(3, true)]
        [InlineData(4, true)]
        [InlineData(10, true)]
        [InlineData(32, true)]
        [InlineData(64, true)]
        [InlineData(100, true)]
        [InlineData(128, true)]
        [InlineData(1000, true)]
        [InlineData(1, false)]
        [InlineData(2, false)]
        [InlineData(3, false)]
        [InlineData(4, false)]
        [InlineData(10, false)]
        [InlineData(32, false)]
        [InlineData(64, false)]
        [InlineData(100, false)]
        [InlineData(128, false)]
        [InlineData(1000, false)]
        public void BitArrayBitArrayCtor(int size, bool value)
        {
            var original = new BitArray(size, value);

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        var local = new BitArray(original);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = IterationCount)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(10)]
        [InlineData(32)]
        [InlineData(64)]
        [InlineData(100)]
        [InlineData(128)]
        [InlineData(1000)]
        public void BitArrayBoolArrayCtor(int size)
        {
            var bools = new bool[size];

            for (int i = 0; i < bools.Length; i++)
            {
                bools[i] = s_random.NextDouble() >= 0.5;
            }

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        var local = new BitArray(bools);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = IterationCount)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(10)]
        [InlineData(32)]
        [InlineData(64)]
        [InlineData(100)]
        [InlineData(128)]
        [InlineData(1000)]
        public void BitArrayByteArrayCtor(int size)
        {
            var bytes = new byte[size];
            s_random.NextBytes(bytes);

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        var local = new BitArray(bytes);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = IterationCount)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(10)]
        [InlineData(32)]
        [InlineData(64)]
        [InlineData(100)]
        [InlineData(128)]
        [InlineData(1000)]
        public void BitArrayIntArrayCtor(int size)
        {
            var ints = new int[size];
            for (int i = 0; i < ints.Length; i++)
            {
                ints[i] = s_random.Next();
            }

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        var local = new BitArray(ints);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = IterationCount)]
        [InlineData(1, true)]
        [InlineData(2, true)]
        [InlineData(3, true)]
        [InlineData(4, true)]
        [InlineData(10, true)]
        [InlineData(32, true)]
        [InlineData(64, true)]
        [InlineData(100, true)]
        [InlineData(128, true)]
        [InlineData(1000, true)]
        [InlineData(1, false)]
        [InlineData(2, false)]
        [InlineData(3, false)]
        [InlineData(4, false)]
        [InlineData(10, false)]
        [InlineData(32, false)]
        [InlineData(64, false)]
        [InlineData(100, false)]
        [InlineData(128, false)]
        [InlineData(1000, false)]
        public void BitArraySetAll(int size, bool value)
        {
            var bytes = new byte[size];
            s_random.NextBytes(bytes);
            var original = new BitArray(bytes);

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        original.SetAll(value);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = IterationCount)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(10)]
        [InlineData(32)]
        [InlineData(64)]
        [InlineData(100)]
        [InlineData(128)]
        [InlineData(1000)]
        public void BitArrayNot(int size)
        {
            var bytes = new byte[size];
            s_random.NextBytes(bytes);
            var original = new BitArray(bytes);

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        original.Not();
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = IterationCount)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(10)]
        [InlineData(32)]
        [InlineData(64)]
        [InlineData(100)]
        [InlineData(128)]
        [InlineData(1000)]
        public void BitArrayGet(int size)
        {
            var bytes = new byte[size];
            s_random.NextBytes(bytes);
            var original = new BitArray(bytes);
            bool local = false;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        for (int j = 0; j < original.Length; j++)
                        {
                            local ^= original.Get(j);
                        }
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = IterationCount)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(10)]
        [InlineData(32)]
        [InlineData(64)]
        [InlineData(100)]
        [InlineData(128)]
        [InlineData(1000)]
        public void BitArraySet(int size)
        {
            var bytes = new byte[size];
            s_random.NextBytes(bytes);
            var original = new BitArray(bytes);

            var values = new bool[original.Length];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = s_random.NextDouble() >= 0.5;
            }

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        for (int j = 0; j < original.Length; j++)
                        {
                            original.Set(j, values[j]);
                        }
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = IterationCount)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(10)]
        [InlineData(32)]
        [InlineData(64)]
        [InlineData(100)]
        [InlineData(128)]
        [InlineData(1000)]
        public void BitArraySetLengthGrow(int size)
        {
            var bytes = new byte[size];
            s_random.NextBytes(bytes);

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        var original = new BitArray(bytes);
                        original.Length = original.Length * 2;
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = IterationCount)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(10)]
        [InlineData(32)]
        [InlineData(64)]
        [InlineData(100)]
        [InlineData(128)]
        [InlineData(1000)]
        public void BitArraySetLengthShrink(int size)
        {
            var bytes = new byte[size];
            s_random.NextBytes(bytes);

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        var original = new BitArray(bytes);
                        original.Length = original.Length / 2;
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = IterationCount)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(10)]
        [InlineData(32)]
        [InlineData(64)]
        [InlineData(100)]
        [InlineData(128)]
        [InlineData(1000)]
        public void BitArrayCopyToIntArray(int size)
        {
            var bytes = new byte[size];
            s_random.NextBytes(bytes);
            var original = new BitArray(bytes);

            var array = new int[size * 32];

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        original.CopyTo(array, 0);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = IterationCount)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(10)]
        [InlineData(32)]
        [InlineData(64)]
        [InlineData(100)]
        [InlineData(128)]
        [InlineData(1000)]
        public void BitArrayCopyToByteArray(int size)
        {
            var bytes = new byte[size];
            s_random.NextBytes(bytes);
            var original = new BitArray(bytes);

            var array = new byte[size * 32];

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        original.CopyTo(array, 0);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = IterationCount)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(10)]
        [InlineData(32)]
        [InlineData(64)]
        [InlineData(100)]
        [InlineData(128)]
        [InlineData(1000)]
        public void BitArrayCopyToBoolArray(int size)
        {
            var bytes = new byte[size];
            s_random.NextBytes(bytes);
            var original = new BitArray(bytes);

            var array = new bool[size * 32];

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        original.CopyTo(array, 0);
                    }
                }
            }
        }
    }
}
