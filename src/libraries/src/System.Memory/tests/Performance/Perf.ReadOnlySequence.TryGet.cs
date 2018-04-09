// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Memory.Tests;
using System.MemoryTests;
using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Buffers.Tests
{
    public class Perf_ReadOnlySequence_TryGet
    {
        private const int InnerCount = 100_000;
        volatile static int _volatileInt = 0;

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void Byte_Array(int bufSize, int bufOffset)
        {
            var buffer = new ReadOnlySequence<byte>(new byte[bufSize], bufOffset, bufSize - 2 * bufOffset);

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        SequencePosition p = buffer.Start;
                        while (buffer.TryGet(ref p, out ReadOnlyMemory<byte> memory))
                            localInt ^= memory.Length;
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void Byte_Memory(int bufSize, int bufOffset)
        {
            var manager = new CustomMemoryForTest<byte>(new byte[bufSize], bufOffset, bufSize - 2 * bufOffset);
            var buffer = new ReadOnlySequence<byte>(manager.Memory);

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        SequencePosition p = buffer.Start;
                        while (buffer.TryGet(ref p, out ReadOnlyMemory<byte> memory))
                            localInt ^= memory.Length;
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void Byte_SingleSegment(int bufSize, int bufOffset)
        {
            var segment1 = new BufferSegment<byte>(new byte[bufSize]);
            var buffer = new ReadOnlySequence<byte>(segment1, bufOffset, segment1, bufSize - bufOffset);

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        SequencePosition p = buffer.Start;
                        while (buffer.TryGet(ref p, out ReadOnlyMemory<byte> memory))
                            localInt ^= memory.Length;
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount / 10)]
        [InlineData(10_000, 100)]
        private static void Byte_MultiSegment(int bufSize, int bufOffset)
        {
            var segment1 = new BufferSegment<byte>(new byte[bufSize / 10]);
            BufferSegment<byte> segment2 = segment1;
            for (int j = 0; j < 10; j++)
                segment2 = segment2.Append(new byte[bufSize / 10]);
            var buffer = new ReadOnlySequence<byte>(segment1, bufOffset, segment2, bufSize / 10 - bufOffset);

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        SequencePosition p = buffer.Start;
                        while (buffer.TryGet(ref p, out ReadOnlyMemory<byte> memory))
                            localInt ^= memory.Length;
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        private static void Byte_Empty()
        {
            ReadOnlySequence<byte> buffer = ReadOnlySequence<byte>.Empty;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        SequencePosition p = buffer.Start;
                        while (buffer.TryGet(ref p, out ReadOnlyMemory<byte> memory))
                            localInt ^= memory.Length;
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        private static void Byte_Default()
        {
            ReadOnlySequence<byte> buffer = default;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        SequencePosition p = buffer.Start;
                        while (buffer.TryGet(ref p, out ReadOnlyMemory<byte> memory))
                            localInt ^= memory.Length;
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void Char_Array(int bufSize, int bufOffset)
        {
            var buffer = new ReadOnlySequence<char>(new char[bufSize], bufOffset, bufSize - 2 * bufOffset);

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        SequencePosition p = buffer.Start;
                        while (buffer.TryGet(ref p, out ReadOnlyMemory<char> memory))
                            localInt ^= memory.Length;
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void Char_Memory(int bufSize, int bufOffset)
        {
            var manager = new CustomMemoryForTest<char>(new char[bufSize], bufOffset, bufSize - 2 * bufOffset);
            var buffer = new ReadOnlySequence<char>(manager.Memory);

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        SequencePosition p = buffer.Start;
                        while (buffer.TryGet(ref p, out ReadOnlyMemory<char> memory))
                            localInt ^= memory.Length;
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void Char_SingleSegment(int bufSize, int bufOffset)
        {
            var segment1 = new BufferSegment<char>(new char[bufSize]);
            var buffer = new ReadOnlySequence<char>(segment1, bufOffset, segment1, bufSize - bufOffset);

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        SequencePosition p = buffer.Start;
                        while (buffer.TryGet(ref p, out ReadOnlyMemory<char> memory))
                            localInt ^= memory.Length;
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount / 10)]
        [InlineData(10_000, 100)]
        private static void Char_MultiSegment(int bufSize, int bufOffset)
        {
            var segment1 = new BufferSegment<char>(new char[bufSize / 10]);
            BufferSegment<char> segment2 = segment1;
            for (int j = 0; j < 10; j++)
                segment2 = segment2.Append(new char[bufSize / 10]);
            var buffer = new ReadOnlySequence<char>(segment1, bufOffset, segment2, bufSize / 10 - bufOffset);

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        SequencePosition p = buffer.Start;
                        while (buffer.TryGet(ref p, out ReadOnlyMemory<char> memory))
                            localInt ^= memory.Length;
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void String(int bufSize, int bufOffset)
        {
            ReadOnlyMemory<char> strMemory = new string('a', bufSize).AsMemory();
            strMemory = strMemory.Slice(bufOffset, bufSize - bufOffset);
            var buffer = new ReadOnlySequence<char>(strMemory);

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        SequencePosition p = buffer.Start;
                        while (buffer.TryGet(ref p, out ReadOnlyMemory<char> memory))
                            localInt ^= memory.Length;
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        private static void Char_Empty()
        {
            ReadOnlySequence<char> buffer = ReadOnlySequence<char>.Empty;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        SequencePosition p = buffer.Start;
                        while (buffer.TryGet(ref p, out ReadOnlyMemory<char> memory))
                            localInt ^= memory.Length;
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        private static void Char_Default()
        {
            ReadOnlySequence<char> buffer = default;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        SequencePosition p = buffer.Start;
                        while (buffer.TryGet(ref p, out ReadOnlyMemory<char> memory))
                            localInt ^= memory.Length;
                    }
                }
                _volatileInt = localInt;
            }
        }

    }
}
