// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace System.Buffers.Tests
{
    public static partial class ArrayBufferWriterTests_Byte
    {
        [Fact]
        public static void ArrayBufferWriter_Ctor()
        {
            {
                var output = new ArrayBufferWriter<byte>();
                Assert.True(output.FreeCapacity > 0);
                Assert.True(output.Capacity > 0);
                Assert.Equal(0, output.WrittenCount);
                Assert.True(ReadOnlySpan<byte>.Empty.SequenceEqual(output.WrittenSpan));
                Assert.True(ReadOnlyMemory<byte>.Empty.Span.SequenceEqual(output.WrittenMemory.Span));
            }

            {
                var output = new ArrayBufferWriter<byte>(200);
                Assert.True(output.FreeCapacity >= 200);
                Assert.True(output.Capacity >= 200);
                Assert.Equal(0, output.WrittenCount);
                Assert.True(ReadOnlySpan<byte>.Empty.SequenceEqual(output.WrittenSpan));
                Assert.True(ReadOnlyMemory<byte>.Empty.Span.SequenceEqual(output.WrittenMemory.Span));
            }

            {
                ArrayBufferWriter<byte> output = default;
                Assert.Equal(null, output);
            }
        }

        [Fact]
        public static void Invalid_Ctor()
        {
            Assert.Throws<ArgumentException>(() => new ArrayBufferWriter<byte>(0));
            Assert.Throws<ArgumentException>(() => new ArrayBufferWriter<byte>(-1));
            Assert.Throws<OutOfMemoryException>(() => new ArrayBufferWriter<byte>(int.MaxValue));
        }

        [Fact]
        public static void Clear()
        {
            var output = new ArrayBufferWriter<byte>();
            int previousAvailable = output.FreeCapacity;
            WriteData(output, 2);
            Assert.True(output.FreeCapacity < previousAvailable);
            Assert.True(output.WrittenCount > 0);
            Assert.False(ReadOnlySpan<byte>.Empty.SequenceEqual(output.WrittenSpan));
            Assert.False(ReadOnlyMemory<byte>.Empty.Span.SequenceEqual(output.WrittenMemory.Span));
            Assert.True(output.WrittenSpan.SequenceEqual(output.WrittenMemory.Span));
            output.Clear();
            Assert.Equal(0, output.WrittenCount);
            Assert.True(ReadOnlySpan<byte>.Empty.SequenceEqual(output.WrittenSpan));
            Assert.True(ReadOnlyMemory<byte>.Empty.Span.SequenceEqual(output.WrittenMemory.Span));
            Assert.Equal(previousAvailable, output.FreeCapacity);
        }

        [Fact]
        public static void Advance()
        {
            {
                var output = new ArrayBufferWriter<byte>();
                int capacity = output.Capacity;
                Assert.Equal(capacity, output.FreeCapacity);
                output.Advance(output.FreeCapacity);
                Assert.Equal(capacity, output.WrittenCount);
                Assert.Equal(0, output.FreeCapacity);
            }

            {
                var output = new ArrayBufferWriter<byte>();
                output.Advance(output.Capacity);
                Assert.Equal(output.Capacity, output.WrittenCount);
                Assert.Equal(0, output.FreeCapacity);
                int previousCapacity = output.Capacity;
                Span<byte> _ = output.GetSpan();
                Assert.True(output.Capacity > previousCapacity);
            }

            {
                var output = new ArrayBufferWriter<byte>();
                WriteData(output, 2);
                ReadOnlyMemory<byte> previousMemory = output.WrittenMemory;
                ReadOnlySpan<byte> previousSpan = output.WrittenSpan;
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
            var output = new ArrayBufferWriter<byte>();
            WriteData(output, 2);
            Assert.Equal(2, output.WrittenCount);
            ReadOnlyMemory<byte> previousMemory = output.WrittenMemory;
            ReadOnlySpan<byte> previousSpan = output.WrittenSpan;
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
                var output = new ArrayBufferWriter<byte>();
                Assert.Throws<ArgumentException>(() => output.Advance(-1));
                Assert.Throws<InvalidOperationException>(() => output.Advance(output.Capacity + 1));
            }

            {
                var output = new ArrayBufferWriter<byte>();
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
                    var output = new ArrayBufferWriter<byte>(2_000_000_000);
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
                var output = new ArrayBufferWriter<byte>();
                WriteData(output, 2);
                Span<byte> span = output.GetSpan();
                Memory<byte> memory = output.GetMemory();
                Span<byte> memorySpan = memory.Span;
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
                var output = new ArrayBufferWriter<byte>();
                WriteData(output, 2);
                ReadOnlyMemory<byte> writtenSoFarMemory = output.WrittenMemory;
                ReadOnlySpan<byte> writtenSoFar = output.WrittenSpan;
                Assert.True(writtenSoFarMemory.Span.SequenceEqual(writtenSoFar));
                int previousAvailable = output.FreeCapacity;
                Span<byte> span = output.GetSpan(500);
                Assert.True(span.Length >= 500);
                Assert.True(output.FreeCapacity >= 500);
                Assert.True(output.FreeCapacity > previousAvailable);

                Assert.Equal(writtenSoFar.Length, output.WrittenCount);
                Assert.False(writtenSoFar.SequenceEqual(span.Slice(0, output.WrittenCount)));

                Memory<byte> memory = output.GetMemory();
                Span<byte> memorySpan = memory.Span;
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
            var output = new ArrayBufferWriter<byte>();
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
                var output = new ArrayBufferWriter<byte>();
                int previousAvailable = output.FreeCapacity;

                for (int i = 0; i < 10; i++)
                {
                    _ = output.GetSpan();
                    Assert.Equal(previousAvailable, output.FreeCapacity);
                }
            }

            {
                var output = new ArrayBufferWriter<byte>();
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
            var output = new ArrayBufferWriter<byte>();
            WriteData(output, 2);
            Assert.Throws<ArgumentException>(() => output.GetSpan(-1));
            Assert.Throws<ArgumentException>(() => output.GetMemory(-1));
        }

        [Fact]
        public static void MultipleCallsToGetSpan()
        {
            var output = new ArrayBufferWriter<byte>(300);
            int previousAvailable = output.FreeCapacity;
            Assert.True(previousAvailable >= 300);
            Assert.True(output.Capacity >= 300);
            Assert.Equal(previousAvailable, output.Capacity);
            Span<byte> span = output.GetSpan();
            Assert.True(span.Length >= previousAvailable);
            Assert.True(span.Length >= 256);
            Span<byte> newSpan = output.GetSpan();
            Assert.Equal(span.Length, newSpan.Length);

            unsafe
            {
                void* pSpan = Unsafe.AsPointer(ref MemoryMarshal.GetReference(span));
                void* pNewSpan = Unsafe.AsPointer(ref MemoryMarshal.GetReference(newSpan));
                Assert.Equal((IntPtr)pSpan, (IntPtr)pNewSpan);
            }

            Assert.Equal(span.Length, output.GetSpan().Length);
        }

        [Fact]
        public static void WriteAndCopyToStream()
        {
            var output = new ArrayBufferWriter<byte>();
            WriteData(output, 100);
            using (var memStream = new MemoryStream(100))
            {
                Assert.Equal(100, output.WrittenCount);

                ReadOnlySpan<byte> outputSpan = output.WrittenMemory.ToArray();

                ReadOnlyMemory<byte> transientMemory = output.WrittenMemory;
                ReadOnlySpan<byte> transientSpan = output.WrittenSpan;

                Assert.True(transientSpan.SequenceEqual(transientMemory.Span));

                Assert.True(transientSpan[0] != 0);

                memStream.Write(transientSpan.ToArray(), 0, transientSpan.Length);
                output.Clear();

                Assert.True(transientSpan[0] == 0);
                Assert.True(transientMemory.Span[0] == 0);

                Assert.Equal(0, output.WrittenCount);
                byte[] streamOutput = memStream.ToArray();

                Assert.True(ReadOnlyMemory<byte>.Empty.Span.SequenceEqual(output.WrittenMemory.Span));
                Assert.True(ReadOnlySpan<byte>.Empty.SequenceEqual(output.WrittenMemory.Span));
                Assert.True(output.WrittenSpan.SequenceEqual(output.WrittenMemory.Span));

                Assert.Equal(outputSpan.Length, streamOutput.Length);
                Assert.True(outputSpan.SequenceEqual(streamOutput));
            }
        }

        [Fact]
        public static async Task WriteAndCopyToStreamAsync()
        {
            var output = new ArrayBufferWriter<byte>();
            WriteData(output, 100);
            using (var memStream = new MemoryStream(100))
            {
                Assert.Equal(100, output.WrittenCount);

                ReadOnlyMemory<byte> outputMemory = output.WrittenMemory.ToArray();

                ReadOnlyMemory<byte> transient = output.WrittenMemory;

                Assert.True(transient.Span[0] != 0);

                await memStream.WriteAsync(transient.ToArray(), 0, transient.Length);
                output.Clear();

                Assert.True(transient.Span[0] == 0);

                Assert.Equal(0, output.WrittenCount);
                byte[] streamOutput = memStream.ToArray();

                Assert.True(ReadOnlyMemory<byte>.Empty.Span.SequenceEqual(output.WrittenMemory.Span));
                Assert.True(ReadOnlySpan<byte>.Empty.SequenceEqual(output.WrittenMemory.Span));

                Assert.Equal(outputMemory.Length, streamOutput.Length);
                Assert.True(outputMemory.Span.SequenceEqual(streamOutput));
            }
        }

        private static void WriteData(IBufferWriter<byte> bufferWriter, int numBytes)
        {
            Span<byte> outputSpan = bufferWriter.GetSpan(numBytes);
            Debug.Assert(outputSpan.Length >= numBytes);
            var random = new Random(42);

            var data = new byte[numBytes];
            random.NextBytes(data);
            data.CopyTo(outputSpan);

            bufferWriter.Advance(numBytes);
        }
    }
}
