// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using Xunit;

namespace Tests.System.Net
{
    public sealed class MultiArrayBufferTests
    {
        [Fact]
        public void BasicTest()
        {
            MultiArrayBuffer buffer = new MultiArrayBuffer(0);

            Assert.Equal(0, buffer.ActiveMemory.Length);
            Assert.Equal(0, buffer.ActiveMemory.BlockCount);

            buffer.TryEnsureAvailableSpaceUpToLimit(3, int.MaxValue);

            int available = buffer.AvailableMemory.Length;
            Assert.True(available >= 3);

            buffer.AvailableMemory[0] = 10;
            buffer.Commit(1);

            Assert.Equal(1, buffer.ActiveMemory.Length);
            Assert.Equal(10, buffer.ActiveMemory[0]);
            Assert.Equal(available - 1, buffer.AvailableMemory.Length);
            Assert.Equal(1, buffer.ActiveMemory.BlockCount);
            Assert.Equal(10, buffer.ActiveMemory.GetBlock(0).Span[0]);

            buffer.AvailableMemory[0] = 20;
            buffer.Commit(1);

            Assert.Equal(2, buffer.ActiveMemory.Length);
            Assert.Equal(20, buffer.ActiveMemory[1]);
            Assert.Equal(available - 2, buffer.AvailableMemory.Length);
            Assert.InRange(buffer.ActiveMemory.BlockCount, 1, 2);

            buffer.AvailableMemory[0] = 30;
            buffer.Commit(1);

            Assert.Equal(3, buffer.ActiveMemory.Length);
            Assert.Equal(30, buffer.ActiveMemory[2]);
            Assert.Equal(available - 3, buffer.AvailableMemory.Length);
            Assert.InRange(buffer.ActiveMemory.BlockCount, 1, 2);

            buffer.Discard(1);
            Assert.Equal(2, buffer.ActiveMemory.Length);
            Assert.Equal(20, buffer.ActiveMemory[0]);
            Assert.InRange(buffer.ActiveMemory.BlockCount, 1, 2);
            Assert.Equal(20, buffer.ActiveMemory.GetBlock(0).Span[0]);

            buffer.Discard(1);
            Assert.Equal(1, buffer.ActiveMemory.Length);
            Assert.Equal(30, buffer.ActiveMemory[0]);
            Assert.Equal(1, buffer.ActiveMemory.BlockCount);
            Assert.Equal(30, buffer.ActiveMemory.GetBlock(0).Span[0]);

            buffer.Discard(1);
            Assert.Equal(0, buffer.ActiveMemory.Length);
            Assert.Equal(0, buffer.ActiveMemory.BlockCount);
        }

        [Fact]
        public void AddByteByByteAndConsumeByteByByte_Success()
        {
            const int Size = 64 * 1024 + 1;

            MultiArrayBuffer buffer = new MultiArrayBuffer(0);

            for (int i = 0; i < Size; i++)
            {
                buffer.TryEnsureAvailableSpaceUpToLimit(1, int.MaxValue);
                buffer.AvailableMemory[0] = (byte)i;
                buffer.Commit(1);
            }

            for (int i = 0; i < Size; i++)
            {
                Assert.Equal((byte)i, buffer.ActiveMemory[0]);
                buffer.Discard(1);
            }

            Assert.Equal(0, buffer.ActiveMemory.Length);
        }

        [Fact]
        public void AddSeveralBytesRepeatedlyAndConsumeSeveralBytesRepeatedly_Success()
        {
            const int ByteCount = 7;
            const int RepeatCount = 8 * 1024;       // enough to ensure we cross several block boundaries

            MultiArrayBuffer buffer = new MultiArrayBuffer(0);

            for (int i = 0; i < RepeatCount; i++)
            {
                buffer.TryEnsureAvailableSpaceUpToLimit(ByteCount, int.MaxValue);
                for (int j = 0; j < ByteCount; j++)
                {
                    buffer.AvailableMemory[j] = (byte)(j + 1);
                }
                buffer.Commit(ByteCount);
            }

            for (int i = 0; i < RepeatCount; i++)
            {
                for (int j = 0; j < ByteCount; j++)
                {
                    Assert.Equal(j + 1, buffer.ActiveMemory[j]);
                }
                buffer.Discard(ByteCount);
            }

            Assert.Equal(0, buffer.ActiveMemory.Length);
        }

        [Fact]
        public void AddSeveralBytesRepeatedlyAndConsumeSeveralBytesRepeatedly_UsingSlice_Success()
        {
            const int ByteCount = 7;
            const int RepeatCount = 8 * 1024;       // enough to ensure we cross several block boundaries

            MultiArrayBuffer buffer = new MultiArrayBuffer(0);

            for (int i = 0; i < RepeatCount; i++)
            {
                buffer.TryEnsureAvailableSpaceUpToLimit(ByteCount, int.MaxValue);
                for (int j = 0; j < ByteCount; j++)
                {
                    buffer.AvailableMemory.Slice(j)[0] = (byte)(j + 1);
                }
                buffer.Commit(ByteCount);
            }

            for (int i = 0; i < RepeatCount; i++)
            {
                for (int j = 0; j < ByteCount; j++)
                {
                    Assert.Equal(j + 1, buffer.ActiveMemory.Slice(j)[0]);
                }
                buffer.Discard(ByteCount);
            }

            Assert.Equal(0, buffer.ActiveMemory.Length);
        }

        [Fact]
        public void AddSeveralBytesRepeatedlyAndConsumeSeveralBytesRepeatedly_UsingSliceWithLength_Success()
        {
            const int ByteCount = 7;
            const int RepeatCount = 8 * 1024;       // enough to ensure we cross several block boundaries

            MultiArrayBuffer buffer = new MultiArrayBuffer(0);

            for (int i = 0; i < RepeatCount; i++)
            {
                buffer.TryEnsureAvailableSpaceUpToLimit(ByteCount, int.MaxValue);
                for (int j = 0; j < ByteCount; j++)
                {
                    buffer.AvailableMemory.Slice(j, ByteCount - j)[0] = (byte)(j + 1);
                }
                buffer.Commit(ByteCount);
            }

            for (int i = 0; i < RepeatCount; i++)
            {
                for (int j = 0; j < ByteCount; j++)
                {
                    Assert.Equal(j + 1, buffer.ActiveMemory.Slice(j, ByteCount - j)[0]);
                }
                buffer.Discard(ByteCount);
            }

            Assert.Equal(0, buffer.ActiveMemory.Length);
        }

        [Fact]
        public void CopyFromRepeatedlyAndCopyToRepeatedly_Success()
        {
            ReadOnlySpan<byte> source = new byte[] { 1, 2, 3, 4, 5, 6, 7 }.AsSpan();

            const int RepeatCount = 8 * 1024;       // enough to ensure we cross several block boundaries

            MultiArrayBuffer buffer = new MultiArrayBuffer(0);

            for (int i = 0; i < RepeatCount; i++)
            {
                buffer.TryEnsureAvailableSpaceUpToLimit(source.Length, int.MaxValue);
                buffer.AvailableMemory.CopyFrom(source);
                buffer.Commit(source.Length);
            }

            Span<byte> destination = new byte[source.Length].AsSpan();
            for (int i = 0; i < RepeatCount; i++)
            {
                buffer.ActiveMemory.Slice(0, source.Length).CopyTo(destination);
                Assert.True(source.SequenceEqual(destination));
                buffer.Discard(source.Length);
            }

            Assert.Equal(0, buffer.ActiveMemory.Length);
        }

        [Fact]
        public void CopyFromRepeatedlyAndCopyToRepeatedly_LargeCopies_Success()
        {
            ReadOnlySpan<byte> source = Enumerable.Range(0, 64 * 1024 - 1).Select(x => (byte)x).ToArray().AsSpan();

            const int RepeatCount = 13;

            MultiArrayBuffer buffer = new MultiArrayBuffer(0);

            for (int i = 0; i < RepeatCount; i++)
            {
                buffer.TryEnsureAvailableSpaceUpToLimit(source.Length, int.MaxValue);
                buffer.AvailableMemory.CopyFrom(source);
                buffer.Commit(source.Length);
            }

            Span<byte> destination = new byte[source.Length].AsSpan();
            for (int i = 0; i < RepeatCount; i++)
            {
                buffer.ActiveMemory.Slice(0, source.Length).CopyTo(destination);
                Assert.True(source.SequenceEqual(destination));
                buffer.Discard(source.Length);
            }

            Assert.Equal(0, buffer.ActiveMemory.Length);
        }

        [Fact]
        public void EmptyMultiMemoryTest()
        {
            MultiMemory mm = MultiMemory.Empty;

            Assert.Equal(0, mm.Length);
            Assert.Equal(0, mm.BlockCount);
            Assert.Equal(0, mm.Slice(0).Length);
            Assert.Equal(0, mm.Slice(0, 0).Length);

            // These should not throw
            mm.CopyTo(new byte[0]);
            mm.CopyFrom(new byte[0]);
        }
    }
}
