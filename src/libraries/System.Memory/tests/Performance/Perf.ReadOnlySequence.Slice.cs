// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Memory.Tests;
using System.MemoryTests;
using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Buffers.Tests
{
    public abstract class Perf_ReadOnlySequence_Slice_Byte: Perf_ReadOnlySequence_Slice<byte>
    {
        public Perf_ReadOnlySequence_Slice_Byte(ReadOnlySequenceFactory<byte> factory): base(factory) { }

        public class Array: Perf_ReadOnlySequence_Slice_Byte
        {
            public Array(): base(ReadOnlySequenceFactory<byte>.ArrayFactory) { }
        }

        public class Memory : Perf_ReadOnlySequence_Slice_Byte
        {
            public Memory() : base(ReadOnlySequenceFactory<byte>.MemoryFactory) { }
        }

        // 001 in name for correcter order in log
        public class Segments001 : Perf_ReadOnlySequence_Slice_Byte
        {
            public Segments001() : base(ReadOnlySequenceFactory<byte>.SingleSegmentFactory) { }
        }

        // 010 in name for correcter order in log
        public class Segments010 : Perf_ReadOnlySequence_Slice_Byte
        {
            public Segments010() : base(new ReadOnlySequenceFactory<byte>.SegmentsTestSequenceFactory(10)) { }
        }

        public class Segments100 : Perf_ReadOnlySequence_Slice_Byte
        {
            public Segments100() : base(new ReadOnlySequenceFactory<byte>.SegmentsTestSequenceFactory(100)) { }
        }
    }

    public abstract class Perf_ReadOnlySequence_Slice_Char: Perf_ReadOnlySequence_Slice<char>
    {
        public Perf_ReadOnlySequence_Slice_Char(ReadOnlySequenceFactory<char> factory): base(factory) { }

        public class Array: Perf_ReadOnlySequence_Slice_Char
        {
            public Array(): base(ReadOnlySequenceFactory<char>.ArrayFactory) { }
        }

        public class Memory : Perf_ReadOnlySequence_Slice_Char
        {
            public Memory() : base(ReadOnlySequenceFactory<char>.MemoryFactory) { }
        }

        public class String : Perf_ReadOnlySequence_Slice_Char
        {
            public String() : base(ReadOnlySequenceFactoryChar.StringFactory) { }
        }

        // 001 in name for correcter order in log
        public class Segments001 : Perf_ReadOnlySequence_Slice_Char
        {
            public Segments001() : base(ReadOnlySequenceFactory<char>.SingleSegmentFactory) { }
        }

        // 010 in name for correcter order in log
        public class Segments010 : Perf_ReadOnlySequence_Slice_Char
        {
            public Segments010() : base(new ReadOnlySequenceFactory<char>.SegmentsTestSequenceFactory(10)) { }
        }

        public class Segments100 : Perf_ReadOnlySequence_Slice_Char
        {
            public Segments100() : base(new ReadOnlySequenceFactory<char>.SegmentsTestSequenceFactory(100)) { }
        }
    }

    public abstract class Perf_ReadOnlySequence_Slice<T>
    {
        private const int InnerCount = 10_000;
        volatile static int _volatileInt = 0;

        internal ReadOnlySequenceFactory<T> Factory { get; }

        public Perf_ReadOnlySequence_Slice(ReadOnlySequenceFactory<T> factory)
        {
            Factory = factory;
        }
        
        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 10)]
        public void Offset(int bufSize, int posCount)
        {
            ReadOnlySequence<T> buffer = Factory.CreateOfSize(bufSize);

            long[] positions = PreparePositionsOffset(buffer, posCount);
            int indexLast = positions.Length - 1;
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        for (int j = indexLast; j >= 0; j--)
                        {
                            ReadOnlySequence<T> sliced = buffer.Slice(positions[j]);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }


        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 10)]
        public void OffsetLength(int bufSize, int posCount)
        {
            ReadOnlySequence<T> buffer = Factory.CreateOfSize(bufSize);

            (long, long)[] positions = PreparePositionsOffsetLength(buffer, posCount);
            int indexLast = positions.Length - 1;
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        for (int j = indexLast; j >= 0; j--)
                        {
                            (long start, long length) startLength = positions[j];
                            ReadOnlySequence<T> sliced = buffer.Slice(startLength.start, startLength.length);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 10)]
        public void OffsetEnd(int bufSize, int posCount)
        {
            ReadOnlySequence<T> buffer = Factory.CreateOfSize(bufSize);

            (long, SequencePosition)[] positions = PreparePositionsOffsetEnd(buffer, posCount);
            int indexLast = positions.Length - 1;
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        for (int j = indexLast; j >= 0; j--)
                        {
                            (long start, SequencePosition end) startEnd = positions[j];
                            ReadOnlySequence<T> sliced = buffer.Slice(startEnd.start, startEnd.end);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 10)]
        public void Start(int bufSize, int posCount)
        {
            ReadOnlySequence<T> buffer = Factory.CreateOfSize(bufSize);

            SequencePosition[] positions = PreparePositionsStart(buffer, posCount);
            int indexLast = positions.Length - 1;
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        for (int j = indexLast; j >= 0; j--)
                        {
                            ReadOnlySequence<T> sliced = buffer.Slice(positions[j]);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 10)]
        public void StartLength(int bufSize, int posCount)
        {
            ReadOnlySequence<T> buffer = Factory.CreateOfSize(bufSize);

            (SequencePosition, long)[] positions = PreparePositionsStartLength(buffer, posCount);
            int indexLast = positions.Length - 1;
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        for (int j = indexLast; j >= 0; j--)
                        {
                            (SequencePosition start, long length) startLength = positions[j];
                            ReadOnlySequence<T> sliced = buffer.Slice(startLength.start, startLength.length);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 10)]
        public void StartEnd(int bufSize, int posCount)
        {
            ReadOnlySequence<T> buffer = Factory.CreateOfSize(bufSize);

            (SequencePosition, SequencePosition)[] positions = PreparePositionsStartEnd(buffer, posCount);
            int indexLast = positions.Length - 1;
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        for (int j = indexLast; j >= 0; j--)
                        {
                            (SequencePosition start, SequencePosition end) startEnd = positions[j];
                            ReadOnlySequence<T> sliced = buffer.Slice(startEnd.start, startEnd.end);
                            localInt ^= sliced.Start.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10_000, 10)]
        public void EmptyLoop(int bufSize, int posCount)
        {
            ReadOnlySequence<T> buffer = Factory.CreateOfSize(bufSize);

            (SequencePosition, SequencePosition)[] positions = PreparePositionsStartEnd(buffer, posCount);
            int indexLast = positions.Length - 1;
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        for (int j = indexLast; j >= 0; j--)
                        {
                            (SequencePosition start, SequencePosition end) startEnd = positions[j];
                            localInt ^= startEnd.start.GetInteger();
                            localInt ^= startEnd.end.GetInteger();
                        }
                    }
                }
                _volatileInt = localInt;
            }
        }

        #region  PreparePositions

        internal static long[] PreparePositionsOffset(in ReadOnlySequence<T> buffer, int count)
        {
            var result = new long[count];

            long length = buffer.Length;
            for (int i = 0; i < count; i++)
                result[i] = length * (i + 1) / (count + 1);

            return result;
        }

        internal static SequencePosition[] PreparePositionsStart(in ReadOnlySequence<T> buffer, int count)
        {
            long[] positions = PreparePositionsOffset(buffer, count);

            var result = new SequencePosition[positions.Length];
            for (int i = 0; i < positions.Length; i++)
                result[i] = buffer.GetPosition(positions[i]);

            return result;
        }

        internal static (long, long)[] PreparePositionsOffsetLength(in ReadOnlySequence<T> buffer, int count)
        {
            var result = new (long offset, long length)[count];

            long length = buffer.Length;
            long endStart = length / 2;
            long endLength = length - endStart;
            for (int i = 0; i < count; i++)
            {
                result[i].offset = length * (i + 1) / (count + 1);
                long endOffset = endLength * (i + 1) / count + endStart;
                result[i].length = endOffset - result[i].offset;
            }

            return result;
        }

        internal static (long, SequencePosition)[] PreparePositionsOffsetEnd(in ReadOnlySequence<T> buffer, int count)
        {
            (long offset, long length)[] positions = PreparePositionsOffsetLength(buffer, count);

            var result = new (long offset, SequencePosition end)[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                result[i].offset = positions[i].offset;
                result[i].end = buffer.GetPosition(positions[i].offset + positions[i].length);
            }

            return result;
        }

        internal static (SequencePosition, long)[] PreparePositionsStartLength(in ReadOnlySequence<T> buffer, int count)
        {
            (long offset, long length)[] positions = PreparePositionsOffsetLength(buffer, count);

            var result = new (SequencePosition start, long length)[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                result[i].start = buffer.GetPosition(positions[i].offset);
                result[i].length = positions[i].length;
            }

            return result;
        }

        internal static (SequencePosition, SequencePosition)[] PreparePositionsStartEnd(in ReadOnlySequence<T> buffer, int count)
        {
            (long offset, long length)[] positions = PreparePositionsOffsetLength(buffer, count);

            var result = new (SequencePosition start, SequencePosition end)[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                result[i].start = buffer.GetPosition(positions[i].offset);
                result[i].end = buffer.GetPosition(positions[i].offset + positions[i].length);
            }

            return result;
        }

        #endregion
    }
}
