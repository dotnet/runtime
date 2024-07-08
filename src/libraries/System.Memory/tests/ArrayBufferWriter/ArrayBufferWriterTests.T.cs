// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Buffers.Tests
{
    public abstract class ArrayBufferWriterTests<T> where T : IEquatable<T>
    {
        [Fact]
        public void ArrayBufferWriter_Ctor()
        {
            {
                var output = new ArrayBufferWriter<T>();
                Assert.Equal(0, output.FreeCapacity);
                Assert.Equal(0, output.Capacity);
                Assert.Equal(0, output.WrittenCount);
                Assert.True(ReadOnlySpan<T>.Empty.SequenceEqual(output.WrittenSpan));
                Assert.True(ReadOnlyMemory<T>.Empty.Span.SequenceEqual(output.WrittenMemory.Span));
            }

            {
                var output = new ArrayBufferWriter<T>(200);
                Assert.True(output.FreeCapacity >= 200);
                Assert.True(output.Capacity >= 200);
                Assert.Equal(0, output.WrittenCount);
                Assert.True(ReadOnlySpan<T>.Empty.SequenceEqual(output.WrittenSpan));
                Assert.True(ReadOnlyMemory<T>.Empty.Span.SequenceEqual(output.WrittenMemory.Span));
            }

            {
                ArrayBufferWriter<T> output = default;
                Assert.Null(output);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/15002", TestRuntimes.Mono)]
        public void Invalid_Ctor()
        {
            Assert.Throws<ArgumentException>(() => new ArrayBufferWriter<T>(0));
            Assert.Throws<ArgumentException>(() => new ArrayBufferWriter<T>(-1));
            Assert.Throws<OutOfMemoryException>(() => new ArrayBufferWriter<T>(int.MaxValue));
        }

        [Fact]
        public void Clear()
        {
            var output = new ArrayBufferWriter<T>(256);
            int previousAvailable = output.FreeCapacity;
            WriteData(output, 2);
            Assert.True(output.FreeCapacity < previousAvailable);
            Assert.True(output.WrittenCount > 0);
            Assert.False(ReadOnlySpan<T>.Empty.SequenceEqual(output.WrittenSpan));
            Assert.False(ReadOnlyMemory<T>.Empty.Span.SequenceEqual(output.WrittenMemory.Span));
            Assert.True(output.WrittenSpan.SequenceEqual(output.WrittenMemory.Span));

            ReadOnlyMemory<T> transientMemory = output.WrittenMemory;
            ReadOnlySpan<T> transientSpan = output.WrittenSpan;
            T t0 = transientMemory.Span[0];
            T t1 = transientSpan[1];
            Assert.NotEqual(default, t0);
            Assert.NotEqual(default, t1);
            output.Clear();
            Assert.Equal(default, transientMemory.Span[0]);
            Assert.Equal(default, transientSpan[1]);

            Assert.Equal(0, output.WrittenCount);
            Assert.True(ReadOnlySpan<T>.Empty.SequenceEqual(output.WrittenSpan));
            Assert.True(ReadOnlyMemory<T>.Empty.Span.SequenceEqual(output.WrittenMemory.Span));
            Assert.Equal(previousAvailable, output.FreeCapacity);
        }

        [Fact]
        public void ResetWrittenCount()
        {
            var output = new ArrayBufferWriter<T>(256);
            int previousAvailable = output.FreeCapacity;
            WriteData(output, 2);
            Assert.True(output.FreeCapacity < previousAvailable);
            Assert.True(output.WrittenCount > 0);
            Assert.False(ReadOnlySpan<T>.Empty.SequenceEqual(output.WrittenSpan));
            Assert.False(ReadOnlyMemory<T>.Empty.Span.SequenceEqual(output.WrittenMemory.Span));
            Assert.True(output.WrittenSpan.SequenceEqual(output.WrittenMemory.Span));

            ReadOnlyMemory<T> transientMemory = output.WrittenMemory;
            ReadOnlySpan<T> transientSpan = output.WrittenSpan;
            T t0 = transientMemory.Span[0];
            T t1 = transientSpan[1];
            Assert.NotEqual(default, t0);
            Assert.NotEqual(default, t1);
            output.ResetWrittenCount();
            Assert.Equal(t0, transientMemory.Span[0]);
            Assert.Equal(t1, transientSpan[1]);

            Assert.Equal(0, output.WrittenCount);
            Assert.True(ReadOnlySpan<T>.Empty.SequenceEqual(output.WrittenSpan));
            Assert.True(ReadOnlyMemory<T>.Empty.Span.SequenceEqual(output.WrittenMemory.Span));
            Assert.Equal(previousAvailable, output.FreeCapacity);
        }

        [Fact]
        public void Advance()
        {
            {
                var output = new ArrayBufferWriter<T>();
                int capacity = output.Capacity;
                Assert.Equal(capacity, output.FreeCapacity);
                output.Advance(output.FreeCapacity);
                Assert.Equal(capacity, output.WrittenCount);
                Assert.Equal(0, output.FreeCapacity);
            }

            {
                var output = new ArrayBufferWriter<T>();
                output.Advance(output.Capacity);
                Assert.Equal(output.Capacity, output.WrittenCount);
                Assert.Equal(0, output.FreeCapacity);
                int previousCapacity = output.Capacity;
                Span<T> _ = output.GetSpan();
                Assert.True(output.Capacity > previousCapacity);
            }

            {
                var output = new ArrayBufferWriter<T>(256);
                WriteData(output, 2);
                ReadOnlyMemory<T> previousMemory = output.WrittenMemory;
                ReadOnlySpan<T> previousSpan = output.WrittenSpan;
                Assert.True(previousSpan.SequenceEqual(previousMemory.Span));
                output.Advance(10);
                Assert.False(previousMemory.Span.SequenceEqual(output.WrittenMemory.Span));
                Assert.False(previousSpan.SequenceEqual(output.WrittenSpan));
                Assert.True(output.WrittenSpan.SequenceEqual(output.WrittenMemory.Span));
            }

            {
                var output = new ArrayBufferWriter<T>();
                _ = output.GetSpan(20);
                WriteData(output, 10);
                ReadOnlyMemory<T> previousMemory = output.WrittenMemory;
                ReadOnlySpan<T> previousSpan = output.WrittenSpan;
                Assert.True(previousSpan.SequenceEqual(previousMemory.Span));
                Assert.Throws<InvalidOperationException>(() => output.Advance(247));
                output.Advance(10);
                Assert.False(previousMemory.Span.SequenceEqual(output.WrittenMemory.Span));
                Assert.False(previousSpan.SequenceEqual(output.WrittenSpan));
                Assert.True(output.WrittenSpan.SequenceEqual(output.WrittenMemory.Span));
            }
        }

        [Fact]
        public void AdvanceZero()
        {
            var output = new ArrayBufferWriter<T>();
            WriteData(output, 2);
            Assert.Equal(2, output.WrittenCount);
            ReadOnlyMemory<T> previousMemory = output.WrittenMemory;
            ReadOnlySpan<T> previousSpan = output.WrittenSpan;
            Assert.True(previousSpan.SequenceEqual(previousMemory.Span));
            output.Advance(0);
            Assert.Equal(2, output.WrittenCount);
            Assert.True(previousMemory.Span.SequenceEqual(output.WrittenMemory.Span));
            Assert.True(previousSpan.SequenceEqual(output.WrittenSpan));
            Assert.True(output.WrittenSpan.SequenceEqual(output.WrittenMemory.Span));
        }

        [Fact]
        public void InvalidAdvance()
        {
            {
                var output = new ArrayBufferWriter<T>();
                Assert.Throws<ArgumentException>(() => output.Advance(-1));
                Assert.Throws<InvalidOperationException>(() => output.Advance(output.Capacity + 1));
            }

            {
                var output = new ArrayBufferWriter<T>();
                WriteData(output, 100);
                Assert.Throws<InvalidOperationException>(() => output.Advance(output.FreeCapacity + 1));
            }
        }

        [Fact]
        public void GetSpan_DefaultCtor()
        {
            var output = new ArrayBufferWriter<T>();
            Span<T> span = output.GetSpan();
            Assert.Equal(256, span.Length);
        }

        [Theory]
        [MemberData(nameof(SizeHints))]
        public void GetSpan_DefaultCtor_WithSizeHint(int sizeHint)
        {
            var output = new ArrayBufferWriter<T>();
            Span<T> span = output.GetSpan(sizeHint);
            Assert.Equal(sizeHint <= 256 ? 256 : sizeHint, span.Length);
        }

        [Fact]
        public void GetSpan_InitSizeCtor()
        {
            var output = new ArrayBufferWriter<T>(100);
            Span<T> span = output.GetSpan();
            Assert.Equal(100, span.Length);
        }

        [Theory]
        [MemberData(nameof(SizeHints))]
        public void GetSpan_InitSizeCtor_WithSizeHint(int sizeHint)
        {
            {
                var output = new ArrayBufferWriter<T>(256);
                Span<T> span = output.GetSpan(sizeHint);
                Assert.Equal(sizeHint <= 256 ? 256 : sizeHint + 256, span.Length);
            }

            {
                var output = new ArrayBufferWriter<T>(1000);
                Span<T> span = output.GetSpan(sizeHint);
                Assert.Equal(sizeHint <= 1000 ? 1000 : sizeHint + 1000, span.Length);
            }
        }

        [Fact]
        public void GetMemory_DefaultCtor()
        {
            var output = new ArrayBufferWriter<T>();
            Memory<T> memory = output.GetMemory();
            Assert.Equal(256, memory.Length);
        }

        [Theory]
        [MemberData(nameof(SizeHints))]
        public void GetMemory_DefaultCtor_WithSizeHint(int sizeHint)
        {
            var output = new ArrayBufferWriter<T>();
            Memory<T> memory = output.GetMemory(sizeHint);
            Assert.Equal(sizeHint <= 256 ? 256 : sizeHint, memory.Length);
        }

        [Fact]
        public void GetMemory_ExceedMaximumBufferSize_WithSmallStartingSize()
        {
            var output = new ArrayBufferWriter<T>(256);
            Assert.Throws<OutOfMemoryException>(() => output.GetMemory(int.MaxValue));
        }

        [Fact]
        public void GetMemory_InitSizeCtor()
        {
            var output = new ArrayBufferWriter<T>(100);
            Memory<T> memory = output.GetMemory();
            Assert.Equal(100, memory.Length);
        }

        [Theory]
        [MemberData(nameof(SizeHints))]
        public void GetMemory_InitSizeCtor_WithSizeHint(int sizeHint)
        {
            {
                var output = new ArrayBufferWriter<T>(256);
                Memory<T> memory = output.GetMemory(sizeHint);
                Assert.Equal(sizeHint <= 256 ? 256 : sizeHint + 256, memory.Length);
            }

            {
                var output = new ArrayBufferWriter<T>(1000);
                Memory<T> memory = output.GetMemory(sizeHint);
                Assert.Equal(sizeHint <= 1000 ? 1000 : sizeHint + 1000, memory.Length);
            }
        }

        // NOTE: InvalidAdvance_Large test is constrained to run on Windows and MacOSX because it causes
        //       problems on Linux due to the way deferred memory allocation works. On Linux, the allocation can
        //       succeed even if there is not enough memory but then the test may get killed by the OOM killer at the
        //       time the memory is accessed which triggers the full memory allocation.
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.OSX)]
        [ConditionalFact(typeof(Environment), nameof(Environment.Is64BitProcess))]
        [OuterLoop]
        public void InvalidAdvance_Large()
        {
            try
            {
                {
                    var output = new ArrayBufferWriter<T>(2_000_000_000);
                    WriteData(output, 1_000);
                    Assert.Throws<InvalidOperationException>(() => output.Advance(int.MaxValue));
                    Assert.Throws<InvalidOperationException>(() => output.Advance(2_000_000_000 - 1_000 + 1));
                }
            }
            catch (OutOfMemoryException) { }
        }

        [Fact]
        public void GetMemoryAndSpan()
        {
            {
                var output = new ArrayBufferWriter<T>();
                WriteData(output, 2);
                Span<T> span = output.GetSpan();
                Memory<T> memory = output.GetMemory();
                Span<T> memorySpan = memory.Span;
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
                var output = new ArrayBufferWriter<T>();
                WriteData(output, 2);
                ReadOnlyMemory<T> writtenSoFarMemory = output.WrittenMemory;
                ReadOnlySpan<T> writtenSoFar = output.WrittenSpan;
                Assert.True(writtenSoFarMemory.Span.SequenceEqual(writtenSoFar));
                int previousAvailable = output.FreeCapacity;
                Span<T> span = output.GetSpan(500);
                Assert.True(span.Length >= 500);
                Assert.True(output.FreeCapacity >= 500);
                Assert.True(output.FreeCapacity > previousAvailable);

                Assert.Equal(writtenSoFar.Length, output.WrittenCount);
                Assert.False(writtenSoFar.SequenceEqual(span.Slice(0, output.WrittenCount)));

                Memory<T> memory = output.GetMemory();
                Span<T> memorySpan = memory.Span;
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
        public void GetSpanShouldAtleastDoubleWhenGrowing()
        {
            var output = new ArrayBufferWriter<T>(256);
            WriteData(output, 100);
            int previousAvailable = output.FreeCapacity;

            _ = output.GetSpan(previousAvailable);
            Assert.Equal(previousAvailable, output.FreeCapacity);

            _ = output.GetSpan(previousAvailable + 1);
            Assert.True(output.FreeCapacity >= previousAvailable * 2);
        }

        [Fact]
        public void GetSpanOnlyGrowsAboveThreshold()
        {
            {
                var output = new ArrayBufferWriter<T>();
                _ = output.GetSpan();
                int previousAvailable = output.FreeCapacity;

                for (int i = 0; i < 10; i++)
                {
                    _ = output.GetSpan();
                    Assert.Equal(previousAvailable, output.FreeCapacity);
                }
            }

            {
                var output = new ArrayBufferWriter<T>();
                _ = output.GetSpan(10);
                int previousAvailable = output.FreeCapacity;

                for (int i = 0; i < 10; i++)
                {
                    _ = output.GetSpan(previousAvailable);
                    Assert.Equal(previousAvailable, output.FreeCapacity);
                }
            }
        }

        [Fact]
        public void InvalidGetMemoryAndSpan()
        {
            var output = new ArrayBufferWriter<T>();
            WriteData(output, 2);
            Assert.Throws<ArgumentException>(() => output.GetSpan(-1));
            Assert.Throws<ArgumentException>(() => output.GetMemory(-1));
        }

        [Fact]
        public void MultipleCallsToGetSpan()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                return;
            }

            var output = new ArrayBufferWriter<T>(300);
            Assert.True(MemoryMarshal.TryGetArray(output.GetMemory(), out ArraySegment<T> array));
            GCHandle pinnedArray = GCHandle.Alloc(array.Array, GCHandleType.Pinned);
            try
            {
                int previousAvailable = output.FreeCapacity;
                Assert.True(previousAvailable >= 300);
                Assert.True(output.Capacity >= 300);
                Assert.Equal(previousAvailable, output.Capacity);
                Span<T> span = output.GetSpan();
                Assert.True(span.Length >= previousAvailable);
                Assert.True(span.Length >= 256);
                Span<T> newSpan = output.GetSpan();
                Assert.Equal(span.Length, newSpan.Length);
                Assert.Equal(0, Unsafe.ByteOffset(ref MemoryMarshal.GetReference(span), ref MemoryMarshal.GetReference(newSpan)));
                Assert.Equal(span.Length, output.GetSpan().Length);
            }
            finally
            {
                pinnedArray.Free();
            }
        }

        protected abstract void WriteData(IBufferWriter<T> bufferWriter, int numBytes);

        public static IEnumerable<object[]> SizeHints
        {
            get
            {
                return new List<object[]>
                {
                    new object[] { 0 },
                    new object[] { 1 },
                    new object[] { 2 },
                    new object[] { 3 },
                    new object[] { 99 },
                    new object[] { 100 },
                    new object[] { 101 },
                    new object[] { 255 },
                    new object[] { 256 },
                    new object[] { 257 },
                    new object[] { 1000 },
                    new object[] { 2000 },
                };
            }
        }
    }
}
