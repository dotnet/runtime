// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace Tests.System.Net
{
    public sealed class ArrayBufferTests
    {
        private static void AssertInvariants(ArrayBuffer buffer)
        {
            byte[]? bytes = buffer.DangerousGetUnderlyingBuffer();

            if (bytes is null)
            {
                Assert.Equal(0, buffer.ActiveStartOffset);
                Assert.Equal(0, buffer.ActiveLength);
                Assert.Throws<NullReferenceException>(() => buffer.Capacity);
            }
            else
            {
                Assert.Equal(bytes.Length, buffer.Capacity);

                Assert.True(buffer.ActiveLength >= 0);
                Assert.True(buffer.AvailableLength >= 0);
                Assert.True(buffer.AvailableLength <= bytes.Length);

                int availableOffset = bytes.Length - buffer.AvailableLength;
                int activeOffset = buffer.ActiveStartOffset;

                Assert.True(availableOffset >= 0);
                Assert.True(activeOffset >= 0);
                Assert.True(availableOffset >= activeOffset);
                Assert.Equal(availableOffset - activeOffset, buffer.ActiveLength);

                Assert.Equal(bytes.Length - availableOffset, buffer.AvailableLength);
                Assert.Equal(buffer.AvailableLength, buffer.AvailableSpan.Length);
                Assert.Equal(buffer.AvailableLength, buffer.AvailableMemory.Length);

                Assert.Equal(buffer.ActiveLength, buffer.ActiveSpan.Length);
                Assert.Equal(buffer.ActiveLength, buffer.ActiveMemory.Length);

                ref byte expectedAvailableRef = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(bytes), availableOffset);
                Assert.True(Unsafe.AreSame(ref expectedAvailableRef, ref MemoryMarshal.GetReference(buffer.AvailableSpan)));
                Assert.True(Unsafe.AreSame(ref expectedAvailableRef, ref MemoryMarshal.GetReference(buffer.AvailableMemory.Span)));

                ref byte expectedActiveRef = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(bytes), activeOffset);
                Assert.True(Unsafe.AreSame(ref expectedActiveRef, ref MemoryMarshal.GetReference(buffer.ActiveSpan)));
                Assert.True(Unsafe.AreSame(ref expectedActiveRef, ref MemoryMarshal.GetReference(buffer.ActiveMemory.Span)));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void BasicTest(bool usePool)
        {
            AssertInvariants(default);

            var buffer = new ArrayBuffer(32, usePool);
            AssertInvariants(buffer);

            Assert.Equal(32, buffer.Capacity);
            buffer.AvailableSpan[0] = 42;
            buffer.Commit(1);
            Assert.Equal(new byte[] { 42 }, buffer.ActiveSpan);
            AssertInvariants(buffer);

            buffer.EnsureAvailableSpace(32);
            Assert.Equal(64, buffer.Capacity);
            Assert.Equal(new byte[] { 42 }, buffer.ActiveSpan);
            AssertInvariants(buffer);

            buffer.AvailableSpan.Fill(43);
            buffer.AvailableSpan[^2] = 44;
            buffer.Commit(buffer.AvailableLength - 1);
            AssertInvariants(buffer);

            buffer.Discard(1);
            Assert.Equal(62, buffer.ActiveLength);
            Assert.Equal(1, buffer.ActiveStartOffset);
            Assert.Equal(44, buffer.ActiveSpan[^1]);
            Assert.Equal(61, buffer.ActiveSpan.IndexOfAnyExcept((byte)43));
            AssertInvariants(buffer);

            // We shift the contents instead of resizing the buffer
            Assert.Equal(1, buffer.AvailableLength);
            buffer.EnsureAvailableSpace(2);
            Assert.Equal(2, buffer.AvailableLength);
            Assert.Equal(64, buffer.Capacity);
            Assert.Equal(62, buffer.ActiveLength);
            Assert.Equal(0, buffer.ActiveStartOffset);
            AssertInvariants(buffer);

            buffer.Discard(1);
            Assert.Equal(1, buffer.ActiveStartOffset);
            AssertInvariants(buffer);

            buffer.EnsureAvailableSpace(4);
            Assert.Equal(128, buffer.Capacity);
            Assert.Equal(67, buffer.AvailableLength);
            Assert.Equal(0, buffer.ActiveStartOffset);
            AssertInvariants(buffer);

            buffer.EnsureAvailableSpace(500);
            Assert.Equal(usePool ? 1024 : 500 + buffer.ActiveLength, buffer.Capacity);
            AssertInvariants(buffer);

            Assert.Equal(61, buffer.ActiveLength);
            Assert.Equal(44, buffer.ActiveSpan[^1]);
            Assert.Equal(60, buffer.ActiveSpan.IndexOfAnyExcept((byte)43));

            buffer.Commit(buffer.AvailableLength);
            buffer.EnsureAvailableSpace(0);
            Assert.Equal(0, buffer.AvailableLength);
            AssertInvariants(buffer);

            if (usePool)
            {
                buffer.ClearAndReturnBuffer();
                AssertInvariants(buffer);

                buffer.EnsureAvailableSpace(42);
                Assert.Equal(64, buffer.Capacity);
                AssertInvariants(buffer);
            }

            buffer.Dispose();
            AssertInvariants(buffer);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AddByteByByteAndConsumeByteByByte_Success(bool usePool)
        {
            const int Size = 64 * 1024 + 1;

            using ArrayBuffer buffer = new(42, usePool);

            for (int i = 0; i < Size; i++)
            {
                buffer.EnsureAvailableSpace(1);
                buffer.AvailableSpan[0] = (byte)i;
                buffer.Commit(1);
                AssertInvariants(buffer);
            }

            for (int i = 0; i < Size; i++)
            {
                Assert.Equal((byte)i, buffer.ActiveSpan[0]);
                buffer.Discard(1);
                AssertInvariants(buffer);
            }

            Assert.Equal(0, buffer.ActiveLength);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AddSeveralBytesRepeatedlyAndConsumeSeveralBytesRepeatedly_Success(bool usePool)
        {
            const int ByteCount = 7;
            const int RepeatCount = 8 * 1024;

            using ArrayBuffer buffer = new(42, usePool);

            for (int i = 0; i < RepeatCount; i++)
            {
                buffer.EnsureAvailableSpace(ByteCount);
                for (int j = 0; j < ByteCount; j++)
                {
                    buffer.AvailableSpan[j] = (byte)(j + 1);
                }
                buffer.Commit(ByteCount);
                AssertInvariants(buffer);
            }

            for (int i = 0; i < RepeatCount; i++)
            {
                for (int j = 0; j < ByteCount; j++)
                {
                    Assert.Equal(j + 1, buffer.ActiveSpan[j]);
                }
                buffer.Discard(ByteCount);
                AssertInvariants(buffer);
            }

            Assert.Equal(0, buffer.ActiveLength);
        }

        [OuterLoop]
        [ConditionalTheory(typeof(Environment), nameof(Environment.Is64BitProcess))]
        [InlineData(true)]
        [InlineData(false)]
        public void CanResizeToMaxArraySize(bool usePool)
        {
            using var buffer = new ArrayBuffer(42, usePool);

            while (buffer.Capacity < Array.MaxLength)
            {
                buffer.EnsureAvailableSpace(1);
                buffer.Commit(buffer.AvailableLength);
                AssertInvariants(buffer);
            }

            Assert.Equal(Array.MaxLength, buffer.Capacity);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ExceedMaximumBufferSize_Throws(bool usePool)
        {
            using var buffer = new ArrayBuffer(42, usePool);
            Assert.Throws<OutOfMemoryException>(() => buffer.EnsureAvailableSpace(int.MaxValue));

            buffer.Commit(1);
            Assert.Throws<OutOfMemoryException>(() => buffer.EnsureAvailableSpace(Array.MaxLength));
        }
    }
}
