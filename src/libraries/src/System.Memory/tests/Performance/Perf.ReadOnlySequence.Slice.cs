// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Memory.Tests;
using System.MemoryTests;
using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Buffers.Tests
{
    public class Perf_ReadOnlySequence_Slice
    {
        private const int InnerCount = 10_000;
        volatile static int _volatileInt = 0;

        #region Byte_Array

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void Byte_Array_Long(int bufSize, int bufOffset)
        {
            var buffer = new ReadOnlySequence<byte>(new byte[bufSize], bufOffset, bufSize - 2 * bufOffset);
            long offset = buffer.Length / 10;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySequence<byte> sliced = buffer;
                        long sliceLen = sliced.Length;
                        while (sliceLen > 0)
                        {
                            sliceLen -= offset;
                            sliced = sliced.Slice(offset);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void Byte_Array_LongLong(int bufSize, int bufOffset)
        {
            var buffer = new ReadOnlySequence<byte>(new byte[bufSize], bufOffset, bufSize - 2 * bufOffset);
            long offset = buffer.Length / 20;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySequence<byte> sliced = buffer;
                        long sliceLen = sliced.Length;
                        while (sliceLen > 0)
                        {
                            sliceLen -= 2 * offset;
                            sliced = sliced.Slice(offset, sliceLen);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void Byte_Array_LongPos(int bufSize, int bufOffset)
        {
            var buffer = new ReadOnlySequence<byte>(new byte[bufSize], bufOffset, bufSize - 2 * bufOffset);
            long offset = buffer.Length / 20;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySequence<byte> sliced = buffer;
                        long sliceLen = sliced.Length;
                        SequencePosition end = sliced.GetPosition(0, sliced.End);
                        while (sliceLen > 0)
                        {
                            sliceLen -= 2 * offset;
                            end =  new SequencePosition(end.GetObject(), end.GetInteger() - (int)offset);
                            sliced = sliced.Slice(offset, end);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void Byte_Array_Pos(int bufSize, int bufOffset)
        {
            var buffer = new ReadOnlySequence<byte>(new byte[bufSize], bufOffset, bufSize - 2 * bufOffset);
            long offset = buffer.Length / 10;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySequence<byte> sliced = buffer;
                        long sliceLen = sliced.Length;
                        SequencePosition start = sliced.GetPosition(0);
                        while (sliceLen > 0)
                        {
                            sliceLen -= offset;
                            start = new SequencePosition(start.GetObject(), start.GetInteger() + (int)offset);
                            sliced = sliced.Slice(start);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void Byte_Array_PosLong(int bufSize, int bufOffset)
        {
            var buffer = new ReadOnlySequence<byte>(new byte[bufSize], bufOffset, bufSize - 2 * bufOffset);
            long offset = buffer.Length / 20;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySequence<byte> sliced = buffer;
                        long sliceLen = sliced.Length;
                        SequencePosition start = sliced.GetPosition(0);
                        while (sliceLen > 0)
                        {
                            sliceLen -= 2 * offset;
                            start = new SequencePosition(start.GetObject(), start.GetInteger() + (int)offset);
                            sliced = sliced.Slice(start, sliceLen);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void Byte_Array_PosPos(int bufSize, int bufOffset)
        {
            var buffer = new ReadOnlySequence<byte>(new byte[bufSize], bufOffset, bufSize - 2 * bufOffset);
            long offset = buffer.Length / 20;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySequence<byte> sliced = buffer;
                        long sliceLen = sliced.Length;
                        SequencePosition start = sliced.GetPosition(0);
                        SequencePosition end = sliced.GetPosition(0, sliced.End);
                        while (sliceLen > 0)
                        {
                            sliceLen -= 2 * offset;
                            start = new SequencePosition(start.GetObject(), start.GetInteger() + (int)offset);
                            end = new SequencePosition(end.GetObject(), end.GetInteger() - (int)offset);
                            sliced = sliced.Slice(start, end);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        #endregion

        #region Byte_Memory

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void Byte_Memory_Long(int bufSize, int bufOffset)
        {
            var manager = new CustomMemoryForTest<byte>(new byte[bufSize], bufOffset, bufSize - 2 * bufOffset);
            var buffer = new ReadOnlySequence<byte>(manager.Memory);
            long offset = buffer.Length / 10;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySequence<byte> sliced = buffer;
                        long sliceLen = sliced.Length;
                        while (sliceLen > 0)
                        {
                            sliceLen -= offset;
                            sliced = sliced.Slice(offset);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void Byte_Memory_LongLong(int bufSize, int bufOffset)
        {
            var manager = new CustomMemoryForTest<byte>(new byte[bufSize], bufOffset, bufSize - 2 * bufOffset);
            var buffer = new ReadOnlySequence<byte>(manager.Memory);
            long offset = buffer.Length / 20;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySequence<byte> sliced = buffer;
                        long sliceLen = sliced.Length;
                        while (sliceLen > 0)
                        {
                            sliceLen -= 2 * offset;
                            sliced = sliced.Slice(offset, sliceLen);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void Byte_Memory_LongPos(int bufSize, int bufOffset)
        {
            var manager = new CustomMemoryForTest<byte>(new byte[bufSize], bufOffset, bufSize - 2 * bufOffset);
            var buffer = new ReadOnlySequence<byte>(manager.Memory);
            long offset = buffer.Length / 20;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySequence<byte> sliced = buffer;
                        long sliceLen = sliced.Length;
                        SequencePosition end = sliced.GetPosition(0, sliced.End);
                        while (sliceLen > 0)
                        {
                            sliceLen -= 2 * offset;
                            end =  new SequencePosition(end.GetObject(), end.GetInteger() - (int)offset);
                            sliced = sliced.Slice(offset, end);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void Byte_Memory_Pos(int bufSize, int bufOffset)
        {
            var manager = new CustomMemoryForTest<byte>(new byte[bufSize], bufOffset, bufSize - 2 * bufOffset);
            var buffer = new ReadOnlySequence<byte>(manager.Memory);
            long offset = buffer.Length / 10;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySequence<byte> sliced = buffer;
                        long sliceLen = sliced.Length;
                        SequencePosition start = sliced.GetPosition(0);
                        while (sliceLen > 0)
                        {
                            sliceLen -= offset;
                            start = new SequencePosition(start.GetObject(), start.GetInteger() + (int)offset);
                            sliced = sliced.Slice(start);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void Byte_Memory_PosLong(int bufSize, int bufOffset)
        {
            var manager = new CustomMemoryForTest<byte>(new byte[bufSize], bufOffset, bufSize - 2 * bufOffset);
            var buffer = new ReadOnlySequence<byte>(manager.Memory);
            long offset = buffer.Length / 20;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySequence<byte> sliced = buffer;
                        long sliceLen = sliced.Length;
                        SequencePosition start = sliced.GetPosition(0);
                        while (sliceLen > 0)
                        {
                            sliceLen -= 2 * offset;
                            start = new SequencePosition(start.GetObject(), start.GetInteger() + (int)offset);
                            sliced = sliced.Slice(start, sliceLen);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void Byte_Memory_PosPos(int bufSize, int bufOffset)
        {
            var manager = new CustomMemoryForTest<byte>(new byte[bufSize], bufOffset, bufSize - 2 * bufOffset);
            var buffer = new ReadOnlySequence<byte>(manager.Memory);
            long offset = buffer.Length / 20;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySequence<byte> sliced = buffer;
                        long sliceLen = sliced.Length;
                        SequencePosition start = sliced.GetPosition(0);
                        SequencePosition end = sliced.GetPosition(0, sliced.End);
                        while (sliceLen > 0)
                        {
                            sliceLen -= 2 * offset;
                            start = new SequencePosition(start.GetObject(), start.GetInteger() + (int)offset);
                            end = new SequencePosition(end.GetObject(), end.GetInteger() - (int)offset);
                            sliced = sliced.Slice(start, end);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        #endregion

        #region Byte_SingleSegment

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void Byte_SingleSegment_Long(int bufSize, int bufOffset)
        {
            var segment1 = new BufferSegment<byte>(new byte[bufSize]);
            var buffer = new ReadOnlySequence<byte>(segment1, bufOffset, segment1, bufSize - bufOffset);
            long offset = buffer.Length / 10;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySequence<byte> sliced = buffer;
                        long sliceLen = sliced.Length;
                        while (sliceLen > 0)
                        {
                            sliceLen -= offset;
                            sliced = sliced.Slice(offset);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void Byte_SingleSegment_LongLong(int bufSize, int bufOffset)
        {
            var segment1 = new BufferSegment<byte>(new byte[bufSize]);
            var buffer = new ReadOnlySequence<byte>(segment1, bufOffset, segment1, bufSize - bufOffset);
            long offset = buffer.Length / 20;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySequence<byte> sliced = buffer;
                        long sliceLen = sliced.Length;
                        while (sliceLen > 0)
                        {
                            sliceLen -= 2 * offset;
                            sliced = sliced.Slice(offset, sliceLen);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void Byte_SingleSegment_LongPos(int bufSize, int bufOffset)
        {
            var segment1 = new BufferSegment<byte>(new byte[bufSize]);
            var buffer = new ReadOnlySequence<byte>(segment1, bufOffset, segment1, bufSize - bufOffset);
            long offset = buffer.Length / 20;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySequence<byte> sliced = buffer;
                        long sliceLen = sliced.Length;
                        SequencePosition end = sliced.GetPosition(0, sliced.End);
                        while (sliceLen > 0)
                        {
                            sliceLen -= 2 * offset;
                            end =  new SequencePosition(end.GetObject(), end.GetInteger() - (int)offset);
                            sliced = sliced.Slice(offset, end);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void Byte_SingleSegment_Pos(int bufSize, int bufOffset)
        {
            var segment1 = new BufferSegment<byte>(new byte[bufSize]);
            var buffer = new ReadOnlySequence<byte>(segment1, bufOffset, segment1, bufSize - bufOffset);
            long offset = buffer.Length / 10;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySequence<byte> sliced = buffer;
                        long sliceLen = sliced.Length;
                        SequencePosition start = sliced.GetPosition(0);
                        while (sliceLen > 0)
                        {
                            sliceLen -= offset;
                            start = new SequencePosition(start.GetObject(), start.GetInteger() + (int)offset);
                            sliced = sliced.Slice(start);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void Byte_SingleSegment_PosLong(int bufSize, int bufOffset)
        {
            var segment1 = new BufferSegment<byte>(new byte[bufSize]);
            var buffer = new ReadOnlySequence<byte>(segment1, bufOffset, segment1, bufSize - bufOffset);
            long offset = buffer.Length / 20;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySequence<byte> sliced = buffer;
                        long sliceLen = sliced.Length;
                        SequencePosition start = sliced.GetPosition(0);
                        while (sliceLen > 0)
                        {
                            sliceLen -= 2 * offset;
                            start = new SequencePosition(start.GetObject(), start.GetInteger() + (int)offset);
                            sliced = sliced.Slice(start, sliceLen);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void Byte_SingleSegment_PosPos(int bufSize, int bufOffset)
        {
            var segment1 = new BufferSegment<byte>(new byte[bufSize]);
            var buffer = new ReadOnlySequence<byte>(segment1, bufOffset, segment1, bufSize - bufOffset);
            long offset = buffer.Length / 20;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySequence<byte> sliced = buffer;
                        long sliceLen = sliced.Length;
                        SequencePosition start = sliced.GetPosition(0);
                        SequencePosition end = sliced.GetPosition(0, sliced.End);
                        while (sliceLen > 0)
                        {
                            sliceLen -= 2 * offset;
                            start = new SequencePosition(start.GetObject(), start.GetInteger() + (int)offset);
                            end = new SequencePosition(end.GetObject(), end.GetInteger() - (int)offset);
                            sliced = sliced.Slice(start, end);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        #endregion

        #region Byte_MultiSegment

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void Byte_MultiSegment_Long(int bufSize, int bufOffset)
        {
            var segment1 = new BufferSegment<byte>(new byte[bufSize / 10]);
            BufferSegment<byte> segment2 = segment1;
            for (int j = 0; j < 10; j++)
                segment2 = segment2.Append(new byte[bufSize / 10]);
            var buffer = new ReadOnlySequence<byte>(segment1, bufOffset, segment2, bufSize / 10 - bufOffset);
            long offset = buffer.Length / 10;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySequence<byte> sliced = buffer;
                        long sliceLen = sliced.Length;
                        while (sliceLen > 0)
                        {
                            sliceLen -= offset;
                            sliced = sliced.Slice(offset);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void Byte_MultiSegment_LongLong(int bufSize, int bufOffset)
        {
            var segment1 = new BufferSegment<byte>(new byte[bufSize / 10]);
            BufferSegment<byte> segment2 = segment1;
            for (int j = 0; j < 10; j++)
                segment2 = segment2.Append(new byte[bufSize / 10]);
            var buffer = new ReadOnlySequence<byte>(segment1, bufOffset, segment2, bufSize / 10 - bufOffset);
            long offset = buffer.Length / 20;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySequence<byte> sliced = buffer;
                        long sliceLen = sliced.Length;
                        while (sliceLen > 0)
                        {
                            sliceLen -= 2 * offset;
                            sliced = sliced.Slice(offset, sliceLen);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void Byte_MultiSegment_LongPos(int bufSize, int bufOffset)
        {
            var segment1 = new BufferSegment<byte>(new byte[bufSize / 10]);
            BufferSegment<byte> segment2 = segment1;
            for (int j = 0; j < 10; j++)
                segment2 = segment2.Append(new byte[bufSize / 10]);
            var buffer = new ReadOnlySequence<byte>(segment1, bufOffset, segment2, bufSize / 10 - bufOffset);
            long offset = buffer.Length / 20;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySequence<byte> sliced = buffer;
                        long sliceLen = sliced.Length;
                        while (sliceLen > 0)
                        {
                            sliceLen -= offset;
                            SequencePosition end = sliced.GetPosition(sliceLen);
                            sliceLen -= offset;
                            sliced = sliced.Slice(offset, end);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void Byte_MultiSegment_Pos(int bufSize, int bufOffset)
        {
            var segment1 = new BufferSegment<byte>(new byte[bufSize / 10]);
            BufferSegment<byte> segment2 = segment1;
            for (int j = 0; j < 10; j++)
                segment2 = segment2.Append(new byte[bufSize / 10]);
            var buffer = new ReadOnlySequence<byte>(segment1, bufOffset, segment2, bufSize / 10 - bufOffset);
            long offset = buffer.Length / 10;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySequence<byte> sliced = buffer;
                        long sliceLen = sliced.Length;
                        while (sliceLen > 0)
                        {
                            sliceLen -= offset;
                            SequencePosition start = sliced.GetPosition(offset);
                            sliced = sliced.Slice(start);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void Byte_MultiSegment_PosLong(int bufSize, int bufOffset)
        {
            var segment1 = new BufferSegment<byte>(new byte[bufSize / 10]);
            BufferSegment<byte> segment2 = segment1;
            for (int j = 0; j < 10; j++)
                segment2 = segment2.Append(new byte[bufSize / 10]);
            var buffer = new ReadOnlySequence<byte>(segment1, bufOffset, segment2, bufSize / 10 - bufOffset);
            long offset = buffer.Length / 20;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySequence<byte> sliced = buffer;
                        long sliceLen = sliced.Length;
                        while (sliceLen > 0)
                        {
                            sliceLen -= 2 * offset;
                            SequencePosition start = sliced.GetPosition(offset);
                            sliced = sliced.Slice(start, sliceLen);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void Byte_MultiSegment_PosPos(int bufSize, int bufOffset)
        {
            var segment1 = new BufferSegment<byte>(new byte[bufSize / 10]);
            BufferSegment<byte> segment2 = segment1;
            for (int j = 0; j < 10; j++)
                segment2 = segment2.Append(new byte[bufSize / 10]);
            var buffer = new ReadOnlySequence<byte>(segment1, bufOffset, segment2, bufSize / 10 - bufOffset);
            long offset = buffer.Length / 20;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySequence<byte> sliced = buffer;
                        long sliceLen = sliced.Length;
                        while (sliceLen > 0)
                        {
                            sliceLen -= offset;
                            SequencePosition end = sliced.GetPosition(sliceLen);
                            SequencePosition start = sliced.GetPosition(offset);
                            sliceLen -= offset;
                            sliced = sliced.Slice(start, end);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        #endregion

        #region String

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void String_Long(int bufSize, int bufOffset)
        {
            ReadOnlyMemory<char> memory = new string('a', bufSize).AsMemory();
            memory = memory.Slice(bufOffset, bufSize - 2 * bufOffset);
            var buffer = new ReadOnlySequence<char>(memory);
            long offset = buffer.Length / 10;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySequence<char> sliced = buffer;
                        long sliceLen = sliced.Length;
                        while (sliceLen > 0)
                        {
                            sliceLen -= offset;
                            sliced = sliced.Slice(offset);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void String_LongLong(int bufSize, int bufOffset)
        {
            ReadOnlyMemory<char> memory = new string('a', bufSize).AsMemory();
            memory = memory.Slice(bufOffset, bufSize - 2 * bufOffset);
            var buffer = new ReadOnlySequence<char>(memory);
            long offset = buffer.Length / 20;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySequence<char> sliced = buffer;
                        long sliceLen = sliced.Length;
                        while (sliceLen > 0)
                        {
                            sliceLen -= 2 * offset;
                            sliced = sliced.Slice(offset, sliceLen);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void String_LongPos(int bufSize, int bufOffset)
        {
            ReadOnlyMemory<char> memory = new string('a', bufSize).AsMemory();
            memory = memory.Slice(bufOffset, bufSize - 2 * bufOffset);
            var buffer = new ReadOnlySequence<char>(memory);
            long offset = buffer.Length / 20;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySequence<char> sliced = buffer;
                        long sliceLen = sliced.Length;
                        SequencePosition end = sliced.GetPosition(0, sliced.End);
                        while (sliceLen > 0)
                        {
                            sliceLen -= 2 * offset;
                            end =  new SequencePosition(end.GetObject(), end.GetInteger() - (int)offset);
                            sliced = sliced.Slice(offset, end);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void String_Pos(int bufSize, int bufOffset)
        {
            ReadOnlyMemory<char> memory = new string('a', bufSize).AsMemory();
            memory = memory.Slice(bufOffset, bufSize - 2 * bufOffset);
            var buffer = new ReadOnlySequence<char>(memory);
            long offset = buffer.Length / 10;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySequence<char> sliced = buffer;
                        long sliceLen = sliced.Length;
                        SequencePosition start = sliced.GetPosition(0);
                        while (sliceLen > 0)
                        {
                            sliceLen -= offset;
                            start = new SequencePosition(start.GetObject(), start.GetInteger() + (int)offset);
                            sliced = sliced.Slice(start);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void String_PosLong(int bufSize, int bufOffset)
        {
            ReadOnlyMemory<char> memory = new string('a', bufSize).AsMemory();
            memory = memory.Slice(bufOffset, bufSize - 2 * bufOffset);
            var buffer = new ReadOnlySequence<char>(memory);
            long offset = buffer.Length / 20;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySequence<char> sliced = buffer;
                        long sliceLen = sliced.Length;
                        SequencePosition start = sliced.GetPosition(0);
                        while (sliceLen > 0)
                        {
                            sliceLen -= 2 * offset;
                            start = new SequencePosition(start.GetObject(), start.GetInteger() + (int)offset);
                            sliced = sliced.Slice(start, sliceLen);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 100)]
        private static void String_PosPos(int bufSize, int bufOffset)
        {
            ReadOnlyMemory<char> memory = new string('a', bufSize).AsMemory();
            memory = memory.Slice(bufOffset, bufSize - 2 * bufOffset);
            var buffer = new ReadOnlySequence<char>(memory);
            long offset = buffer.Length / 20;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySequence<char> sliced = buffer;
                        long sliceLen = sliced.Length;
                        SequencePosition start = sliced.GetPosition(0);
                        SequencePosition end = sliced.GetPosition(0, sliced.End);
                        while (sliceLen > 0)
                        {
                            sliceLen -= 2 * offset;
                            start = new SequencePosition(start.GetObject(), start.GetInteger() + (int)offset);
                            end = new SequencePosition(end.GetObject(), end.GetInteger() - (int)offset);
                            sliced = sliced.Slice(start, end);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        #endregion

    }
}
