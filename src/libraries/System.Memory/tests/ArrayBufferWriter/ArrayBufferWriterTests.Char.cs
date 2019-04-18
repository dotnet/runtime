// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Buffers.Tests
{
    public static partial class ArrayBufferWriterTests_Char
    {
        [Fact]
        public static void ArrayBufferWriter_Ctor()
        {
            {
                var output = new ArrayBufferWriter<char>();
                Assert.True(output.FreeCapacity > 0);
                Assert.True(output.Capacity > 0);
                Assert.Equal(0, output.WrittenCount);
                Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(output.WrittenSpan));
                Assert.True(ReadOnlyMemory<char>.Empty.Span.SequenceEqual(output.WrittenMemory.Span));
            }

            {
                var output = new ArrayBufferWriter<char>(200);
                Assert.True(output.FreeCapacity >= 200);
                Assert.True(output.Capacity >= 200);
                Assert.Equal(0, output.WrittenCount);
                Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(output.WrittenSpan));
                Assert.True(ReadOnlyMemory<char>.Empty.Span.SequenceEqual(output.WrittenMemory.Span));
            }

            {
                ArrayBufferWriter<char> output = default;
                Assert.Equal(null, output);
            }
        }

        [Fact]
        public static void Invalid_Ctor()
        {
            Assert.Throws<ArgumentException>(() => new ArrayBufferWriter<char>(0));
            Assert.Throws<ArgumentException>(() => new ArrayBufferWriter<char>(-1));
            Assert.Throws<OutOfMemoryException>(() => new ArrayBufferWriter<char>(int.MaxValue));
        }

        [Fact]
        public static void Clear()
        {
            var output = new ArrayBufferWriter<char>();
            int previousAvailable = output.FreeCapacity;
            WriteData(output, 2);
            Assert.True(output.FreeCapacity < previousAvailable);
            Assert.True(output.WrittenCount > 0);
            Assert.False(ReadOnlySpan<char>.Empty.SequenceEqual(output.WrittenSpan));
            Assert.False(ReadOnlyMemory<char>.Empty.Span.SequenceEqual(output.WrittenMemory.Span));
            Assert.True(output.WrittenSpan.SequenceEqual(output.WrittenMemory.Span));
            output.Clear();
            Assert.Equal(0, output.WrittenCount);
            Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(output.WrittenSpan));
            Assert.True(ReadOnlyMemory<char>.Empty.Span.SequenceEqual(output.WrittenMemory.Span));
            Assert.Equal(previousAvailable, output.FreeCapacity);
        }

        [Fact]
        public static void Advance()
        {
            {
                var output = new ArrayBufferWriter<char>();
                int capacity = output.Capacity;
                Assert.Equal(capacity, output.FreeCapacity);
                output.Advance(output.FreeCapacity);
                Assert.Equal(capacity, output.WrittenCount);
                Assert.Equal(0, output.FreeCapacity);
            }

            {
                var output = new ArrayBufferWriter<char>();
                output.Advance(output.Capacity);
                Assert.Equal(output.Capacity, output.WrittenCount);
                Assert.Equal(0, output.FreeCapacity);
                int previousCapacity = output.Capacity;
                _ = output.GetSpan();
                Assert.True(output.Capacity > previousCapacity);
            }

            {
                var output = new ArrayBufferWriter<char>();
                WriteData(output, 2);
                ReadOnlyMemory<char> previousMemory = output.WrittenMemory;
                ReadOnlySpan<char> previousSpan = output.WrittenSpan;
                Assert.True(previousSpan.SequenceEqual(previousMemory.Span));
                output.Advance(10);
                Assert.False(previousMemory.Span.SequenceEqual(output.WrittenMemory.Span));
                Assert.False(previousSpan.SequenceEqual(output.WrittenSpan));
                Assert.True(output.WrittenSpan.SequenceEqual(output.WrittenMemory.Span));
            }
        }

        [Fact]
        public static void AdvanceZero()
        {
            var output = new ArrayBufferWriter<char>();
            WriteData(output, 2);
            Assert.Equal(2, output.WrittenCount);
            ReadOnlyMemory<char> previousMemory = output.WrittenMemory;
            ReadOnlySpan<char> previousSpan = output.WrittenSpan;
            Assert.True(previousSpan.SequenceEqual(previousMemory.Span));
            output.Advance(0);
            Assert.Equal(2, output.WrittenCount);
            Assert.True(previousMemory.Span.SequenceEqual(output.WrittenMemory.Span));
            Assert.True(previousSpan.SequenceEqual(output.WrittenSpan));
            Assert.True(output.WrittenSpan.SequenceEqual(output.WrittenMemory.Span));
        }

        [Fact]
        public static void InvalidAdvance()
        {
            {
                var output = new ArrayBufferWriter<char>();
                Assert.Throws<ArgumentException>(() => output.Advance(-1));
                Assert.Throws<InvalidOperationException>(() => output.Advance(output.Capacity + 1));
            }

            {
                var output = new ArrayBufferWriter<char>();
                WriteData(output, 100);
                Assert.Throws<InvalidOperationException>(() => output.Advance(output.FreeCapacity + 1));
            }
        }

        public static bool IsX64 { get; } = IntPtr.Size >= 8;

        [ConditionalFact(nameof(IsX64))]
        [OuterLoop]
        public static void InvalidAdvance_Large()
        {
            try
            {
                {
                    var output = new ArrayBufferWriter<char>(2_000_000_000);
                    WriteData(output, 1_000);
                    Assert.Throws<InvalidOperationException>(() => output.Advance(int.MaxValue));
                    Assert.Throws<InvalidOperationException>(() => output.Advance(2_000_000_000 - 1_000 + 1));
                }
            }
            catch (OutOfMemoryException) { }
        }

        [Fact]
        public static void GetMemoryAndSpan()
        {
            {
                var output = new ArrayBufferWriter<char>();
                WriteData(output, 2);
                Span<char> span = output.GetSpan();
                Memory<char> memory = output.GetMemory();
                Span<char> memorySpan = memory.Span;
                Assert.True(span.Length > 0);
                Assert.True(memorySpan.Length > 0);
                Assert.Equal(span.Length, memorySpan.Length);
                for (int i = 0; i < span.Length; i++)
                {
                    Assert.Equal(default, span[i]);
                    Assert.Equal(default, memorySpan[i]);
                }
            }

            {
                var output = new ArrayBufferWriter<char>();
                WriteData(output, 2);
                ReadOnlyMemory<char> writtenSoFarMemory = output.WrittenMemory;
                ReadOnlySpan<char> writtenSoFar = output.WrittenSpan;
                Assert.True(writtenSoFarMemory.Span.SequenceEqual(writtenSoFar));
                int previousAvailable = output.FreeCapacity;
                Span<char> span = output.GetSpan(500);
                Assert.True(span.Length >= 500);
                Assert.True(output.FreeCapacity >= 500);
                Assert.True(output.FreeCapacity > previousAvailable);

                Assert.Equal(writtenSoFar.Length, output.WrittenCount);
                Assert.False(writtenSoFar.SequenceEqual(span.Slice(0, output.WrittenCount)));

                Memory<char> memory = output.GetMemory();
                Span<char> memorySpan = memory.Span;
                Assert.True(span.Length >= 500);
                Assert.True(memorySpan.Length >= 500);
                Assert.Equal(span.Length, memorySpan.Length);
                for (int i = 0; i < span.Length; i++)
                {
                    Assert.Equal(default, span[i]);
                    Assert.Equal(default, memorySpan[i]);
                }

                memory = output.GetMemory(500);
                memorySpan = memory.Span;
                Assert.True(memorySpan.Length >= 500);
                Assert.Equal(span.Length, memorySpan.Length);
                for (int i = 0; i < memorySpan.Length; i++)
                {
                    Assert.Equal(default, memorySpan[i]);
                }
            }
        }

        [Fact]
        public static void GetSpanShouldAtleastDoubleWhenGrowing()
        {
            var output = new ArrayBufferWriter<char>();
            WriteData(output, 100);
            int previousAvailable = output.FreeCapacity;

            _ = output.GetSpan(previousAvailable);
            Assert.Equal(previousAvailable, output.FreeCapacity);

            _ = output.GetSpan(previousAvailable + 1);
            Assert.True(output.FreeCapacity >= previousAvailable * 2);
        }

        [Fact]
        public static void GetSpanOnlyGrowsAboveThreshold()
        {
            {
                var output = new ArrayBufferWriter<char>();
                int previousAvailable = output.FreeCapacity;

                for (int i = 0; i < 10; i++)
                {
                    _ = output.GetSpan();
                    Assert.Equal(previousAvailable, output.FreeCapacity);
                }
            }

            {
                var output = new ArrayBufferWriter<char>();
                int previousAvailable = output.FreeCapacity;

                _ = output.GetSpan(previousAvailable);
                Assert.Equal(previousAvailable, output.FreeCapacity);

                previousAvailable = output.FreeCapacity;
                for (int i = 0; i < 10; i++)
                {
                    _ = output.GetSpan();
                    Assert.Equal(previousAvailable, output.FreeCapacity);
                }
            }
        }

        [Fact]
        public static void InvalidGetMemoryAndSpan()
        {
            var output = new ArrayBufferWriter<char>();
            WriteData(output, 2);
            Assert.Throws<ArgumentException>(() => output.GetSpan(-1));
            Assert.Throws<ArgumentException>(() => output.GetMemory(-1));
        }

        [Fact]
        public static void MultipleCallsToGetSpan()
        {
            var output = new ArrayBufferWriter<char>(300);
            int previousAvailable = output.FreeCapacity;
            Assert.True(previousAvailable >= 300);
            Assert.True(output.Capacity >= 300);
            Assert.Equal(previousAvailable, output.Capacity);
            Span<char> span = output.GetSpan();
            Assert.True(span.Length >= previousAvailable);
            Assert.True(span.Length >= 256);
            Span<char> newSpan = output.GetSpan();
            Assert.Equal(span.Length, newSpan.Length);

            unsafe
            {
                void* pSpan = Unsafe.AsPointer(ref MemoryMarshal.GetReference(span));
                void* pNewSpan = Unsafe.AsPointer(ref MemoryMarshal.GetReference(newSpan));
                Assert.Equal((IntPtr)pSpan, (IntPtr)pNewSpan);
            }

            Assert.Equal(span.Length, output.GetSpan().Length);
        }

        private static void WriteData(IBufferWriter<char> bufferWriter, int numChars)
        {
            Span<char> outputSpan = bufferWriter.GetSpan(numChars);
            Debug.Assert(outputSpan.Length >= numChars);
            var random = new Random(42);

            var data = new char[numChars];

            for (int i = 0; i < numChars; i++)
            {
                data[i] = (char)random.Next(0, char.MaxValue);
            }

            data.CopyTo(outputSpan);

            bufferWriter.Advance(numChars);
        }
    }
}
