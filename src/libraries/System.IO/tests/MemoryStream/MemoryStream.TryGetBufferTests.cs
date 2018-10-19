// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.IO;
using System.Collections.Generic;

namespace System.IO.Tests
{
    public class MemoryStream_TryGetBufferTests
    {
        [Fact]
        public static void TryGetBuffer_Constructor_AlwaysReturnsTrue()
        {
            var stream = new MemoryStream();
            Assert.Equal(0, stream.Capacity);

            ArraySegment<byte> segment;
            Assert.True(stream.TryGetBuffer(out segment));

            Assert.NotNull(segment.Array);
            Assert.Equal(0, segment.Offset);
            Assert.Equal(0, segment.Count);
        }

        [Fact]
        public static void TryGetBuffer_Constructor_Int32_AlwaysReturnsTrue()
        {
            var stream = new MemoryStream(512);
            Assert.Equal(512, stream.Capacity);

            ArraySegment<byte> segment;
            Assert.True(stream.TryGetBuffer(out segment));

            Assert.Equal(512, segment.Array.Length);
            Assert.Equal(0, segment.Offset);
            Assert.Equal(0, segment.Count);
        }

        [Fact]
        public static void TryGetBuffer_Constructor_ByteArray_AlwaysReturnsFalse()
        {
            var stream = new MemoryStream(new byte[512]);
            Assert.Equal(512, stream.Capacity);

            ArraySegment<byte> segment;
            Assert.False(stream.TryGetBuffer(out segment));
        }

        [Fact]
        public static void TryGetBuffer_Constructor_ByteArray_Bool_AlwaysReturnsFalse()
        {
            var stream = new MemoryStream(new byte[512], writable: true);
            Assert.Equal(512, stream.Capacity);

            ArraySegment<byte> segment;
            Assert.False(stream.TryGetBuffer(out segment));
        }

        [Fact]
        public static void TryGetBuffer_Constructor_ByteArray_Int32_Int32_AlwaysReturnsFalse()
        {
            var stream = new MemoryStream(new byte[512], index: 0, count: 512);
            Assert.Equal(512, stream.Capacity);

            ArraySegment<byte> segment;
            Assert.False(stream.TryGetBuffer(out segment));
        }

        [Fact]
        public static void TryGetBuffer_Constructor_ByteArray_Int32_Int32_Bool_AlwaysReturnsFalse()
        {
            var stream = new MemoryStream(new byte[512], index: 0, count: 512, writable: true);
            Assert.Equal(512, stream.Capacity);

            ArraySegment<byte> segment;
            Assert.False(stream.TryGetBuffer(out segment));
        }

        [Fact]
        public static void TryGetBuffer_Constructor_ByteArray_Int32_Int32_Bool_Bool_FalseAsPubliclyVisible_ReturnsFalse()
        {
            var stream = new MemoryStream(new byte[512], index: 0, count: 512, writable: true, publiclyVisible: false);
            Assert.Equal(512, stream.Capacity);

            ArraySegment<byte> segment;
            Assert.False(stream.TryGetBuffer(out segment));
        }

        [Fact]
        public static void TryGetBuffer_Constructor_ByteArray_Int32_Int32_Bool_Bool_TrueAsPubliclyVisible_ReturnsTrue()
        {
            var stream = new MemoryStream(new byte[512], index: 0, count: 512, writable: true, publiclyVisible: true);
            Assert.Equal(512, stream.Capacity);

            ArraySegment<byte> segment;
            Assert.True(stream.TryGetBuffer(out segment));

            Assert.NotNull(segment.Array);
            Assert.Equal(512, segment.Array.Length);
            Assert.Equal(0, segment.Offset);
            Assert.Equal(512, segment.Count);
        }

        [Theory]
        [MemberData(nameof(GetArraysVariedBySize))]
        public static void TryGetBuffer_Constructor_ByteArray_AlwaysReturnsEmptyArraySegment(byte[] array)
        {
            var stream = new MemoryStream(array);

            ArraySegment<byte> result;
            Assert.False(stream.TryGetBuffer(out result));

            // publiclyVisible = false;
            Assert.True(default(ArraySegment<byte>).Equals(result));
        }

        [Theory]
        [MemberData(nameof(GetArraysVariedBySize))]
        public static void TryGetBuffer_Constructor_ByteArray_Bool_AlwaysReturnsEmptyArraySegment(byte[] array)
        {
            var stream = new MemoryStream(array, writable: true);

            ArraySegment<byte> result;
            Assert.False(stream.TryGetBuffer(out result));

            // publiclyVisible = false;
            Assert.True(default(ArraySegment<byte>).Equals(result));
        }

        [Theory]
        [MemberData(nameof(GetArraysVariedByOffsetAndLength))]
        public static void TryGetBuffer_Constructor_ByteArray_Int32_Int32_AlwaysReturnsEmptyArraySegment(ArraySegment<byte> array)
        {
            var stream = new MemoryStream(array.Array, index: array.Offset, count: array.Count);

            ArraySegment<byte> result;
            Assert.False(stream.TryGetBuffer(out result));

            // publiclyVisible = false;
            Assert.True(default(ArraySegment<byte>).Equals(result));
        }

        [Theory]
        [MemberData(nameof(GetArraysVariedByOffsetAndLength))]
        public static void TryGetBuffer_Constructor_ByteArray_Int32_Int32_Bool_AlwaysReturnsEmptyArraySegment(ArraySegment<byte> array)
        {
            var stream = new MemoryStream(array.Array, index: array.Offset, count: array.Count, writable: true);

            ArraySegment<byte> result;
            Assert.False(stream.TryGetBuffer(out result));

            // publiclyVisible = false;
            Assert.True(default(ArraySegment<byte>).Equals(result));
        }

        [Theory]
        [MemberData(nameof(GetArraysVariedByOffsetAndLength))]
        public static void TryGetBuffer_Constructor_ByteArray_Int32_Int32_Bool_Bool_FalseAsPubliclyVisible_ReturnsEmptyArraySegment(ArraySegment<byte> array)
        {
            var stream = new MemoryStream(array.Array, index: array.Offset, count: array.Count, writable: true, publiclyVisible: false);

            ArraySegment<byte> result;
            Assert.False(stream.TryGetBuffer(out result));

            // publiclyVisible = false;
            Assert.True(default(ArraySegment<byte>).Equals(result));
        }

        [Fact]
        public static void TryGetBuffer_Constructor_AlwaysReturnsOffsetSetToZero()
        {
            var stream = new MemoryStream();

            ArraySegment<byte> result;
            Assert.True(stream.TryGetBuffer(out result));

            Assert.Equal(0, result.Offset);

        }

        [Fact]
        public static void TryGetBuffer_Constructor_Int32_AlwaysReturnsOffsetSetToZero()
        {
            var stream = new MemoryStream(512);

            ArraySegment<byte> result;
            Assert.True(stream.TryGetBuffer(out result));

            Assert.Equal(0, result.Offset);
        }

        [Theory]
        [MemberData(nameof(GetArraysVariedByOffsetAndLength))]
        public static void TryGetBuffer_Constructor_ByteArray_Int32_Int32_Bool_Bool_ValueAsIndexAndTrueAsPubliclyVisible_AlwaysReturnsOffsetSetToIndex(ArraySegment<byte> array)
        {
            var stream = new MemoryStream(array.Array, index: array.Offset, count: array.Count, writable: true, publiclyVisible: true);

            ArraySegment<byte> result;
            Assert.True(stream.TryGetBuffer(out result));

            Assert.Equal(array.Offset, result.Offset);
        }

        [Fact]
        public static void TryGetBuffer_Constructor_ByDefaultReturnsCountSetToZero()
        {
            var stream = new MemoryStream();

            ArraySegment<byte> result;
            Assert.True(stream.TryGetBuffer(out result));

            Assert.Equal(0, result.Count);
        }

        [Theory]
        [MemberData(nameof(GetArraysVariedBySize))]
        public static void TryGetBuffer_Constructor_ReturnsCountSetToWrittenLength(byte[] array)
        {
            var stream = new MemoryStream();
            stream.Write(array, 0, array.Length);

            ArraySegment<byte> result;
            Assert.True(stream.TryGetBuffer(out result));

            Assert.Equal(array.Length, result.Count);
        }

        [Fact]
        public static void TryGetBuffer_Constructor_Int32_ByDefaultReturnsCountSetToZero()
        {
            var stream = new MemoryStream(512);

            ArraySegment<byte> result;
            Assert.True(stream.TryGetBuffer(out result));

            Assert.Equal(0, result.Offset);
        }

        [Theory]
        [MemberData(nameof(GetArraysVariedBySize))]
        public static void TryGetBuffer_Constructor_Int32_ReturnsCountSetToWrittenLength(byte[] array)
        {
            var stream = new MemoryStream(512);
            stream.Write(array, 0, array.Length);

            ArraySegment<byte> result;
            Assert.True(stream.TryGetBuffer(out result));

            Assert.Equal(array.Length, result.Count);
        }

        [Theory]
        [MemberData(nameof(GetArraysVariedByOffsetAndLength))]
        public static void TryGetBuffer_Constructor_ByteArray_Int32_Int32_Bool_Bool_ValueAsCountAndTrueAsPubliclyVisible_AlwaysReturnsCountSetToCount(ArraySegment<byte> array)
        {
            var stream = new MemoryStream(array.Array, index: array.Offset, count: array.Count, writable: true, publiclyVisible: true);

            ArraySegment<byte> result;
            Assert.True(stream.TryGetBuffer(out result));

            Assert.Equal(array.Count, result.Count);
        }

        [Fact]
        public static void TryGetBuffer_Constructor_ReturnsArray()
        {
            var stream = new MemoryStream();

            ArraySegment<byte> result;
            Assert.True(stream.TryGetBuffer(out result));

            Assert.NotNull(result.Array);
        }

        [Fact]
        public static void TryGetBuffer_Constructor_MultipleCallsReturnsSameArray()
        {
            var stream = new MemoryStream();

            ArraySegment<byte> result1;
            ArraySegment<byte> result2;
            Assert.True(stream.TryGetBuffer(out result1));
            Assert.True(stream.TryGetBuffer(out result2));

            Assert.Same(result1.Array, result2.Array);
        }

        [Fact]
        public static void TryGetBuffer_Constructor_Int32_MultipleCallsReturnSameArray()
        {
            var stream = new MemoryStream(512);

            ArraySegment<byte> result1;
            ArraySegment<byte> result2;
            Assert.True(stream.TryGetBuffer(out result1));
            Assert.True(stream.TryGetBuffer(out result2));

            Assert.Same(result1.Array, result2.Array);
        }

        [Fact]
        public static void TryGetBuffer_Constructor_Int32_WhenWritingPastCapacity_ReturnsDifferentArrays()
        {
            var stream = new MemoryStream(512);

            ArraySegment<byte> result1;
            Assert.True(stream.TryGetBuffer(out result1));

            // Force the stream to resize the underlying array
            stream.Write(new byte[1024], 0, 1024);

            ArraySegment<byte> result2;
            Assert.True(stream.TryGetBuffer(out result2));

            Assert.NotSame(result1.Array, result2.Array);
        }

        [Theory]
        [MemberData(nameof(GetArraysVariedByOffsetAndLength))]
        public static void TryGetBuffer_Constructor_ByteArray_Int32_Int32_Bool_Bool_ValueAsBufferAndTrueAsPubliclyVisible_AlwaysReturnsArraySetToBuffer(ArraySegment<byte> array)
        {
            var stream = new MemoryStream(array.Array, index: array.Offset, count: array.Count, writable: true, publiclyVisible: true);

            ArraySegment<byte> result;
            Assert.True(stream.TryGetBuffer(out result));

            Assert.Same(array.Array, result.Array);
        }

        [Theory]
        [MemberData(nameof(GetArraysVariedByOffsetAndLength))]
        public static void TryGetBuffer_WhenDisposed_ReturnsTrue(ArraySegment<byte> array)
        {
            var stream = new MemoryStream(array.Array, index: array.Offset, count: array.Count, writable: true, publiclyVisible: true);
            stream.Dispose();

            ArraySegment<byte> segment;
            Assert.True(stream.TryGetBuffer(out segment));

            Assert.Same(array.Array, segment.Array);
            Assert.Equal(array.Offset, segment.Offset);
            Assert.Equal(array.Count, segment.Count);
        }

        [Theory]
        [MemberData(nameof(GetArraysVariedByOffsetAndLength))]
        public static void TryGetBuffer_WhenDisposed_ReturnsOffsetSetToIndex(ArraySegment<byte> array)
        {
            var stream = new MemoryStream(array.Array, index: array.Offset, count: array.Count, writable: true, publiclyVisible: true);
            stream.Dispose();

            ArraySegment<byte> result;
            Assert.True(stream.TryGetBuffer(out result));

            Assert.Equal(array.Offset, result.Offset);
        }

        [Theory]
        [MemberData(nameof(GetArraysVariedByOffsetAndLength))]
        public static void TryGetBuffer_WhenDisposed_ReturnsCountSetToCount(ArraySegment<byte> array)
        {
            var stream = new MemoryStream(array.Array, index: array.Offset, count: array.Count, writable: true, publiclyVisible: true);
            stream.Dispose();

            ArraySegment<byte> result;
            Assert.True(stream.TryGetBuffer(out result));

            Assert.Equal(array.Count, result.Count);
        }

        [Theory]
        [MemberData(nameof(GetArraysVariedByOffsetAndLength))]
        public static void TryGetBuffer_WhenDisposed_ReturnsArraySetToBuffer(ArraySegment<byte> array)
        {
            var stream = new MemoryStream(array.Array, index: array.Offset, count: array.Count, writable: true, publiclyVisible: true);
            stream.Dispose();

            ArraySegment<byte> result;
            Assert.True(stream.TryGetBuffer(out result));

            Assert.Same(array.Array, result.Array);
        }

        public static IEnumerable<object[]> GetArraysVariedByOffsetAndLength()
        {
            yield return new object[] { new ArraySegment<byte>(new byte[512], 0, 512) };
            yield return new object[] { new ArraySegment<byte>(new byte[512], 1, 511) };
            yield return new object[] { new ArraySegment<byte>(new byte[512], 2, 510) };
            yield return new object[] { new ArraySegment<byte>(new byte[512], 256, 256) };
            yield return new object[] { new ArraySegment<byte>(new byte[512], 512, 0) };
            yield return new object[] { new ArraySegment<byte>(new byte[512], 511, 1) };
            yield return new object[] { new ArraySegment<byte>(new byte[512], 510, 2) };
        }

        public static IEnumerable<object[]> GetArraysVariedBySize()
        {
            yield return new object[] { FillWithData(new byte[0]) };
            yield return new object[] { FillWithData(new byte[1]) };
            yield return new object[] { FillWithData(new byte[2]) };
            yield return new object[] { FillWithData(new byte[254]) };
            yield return new object[] { FillWithData(new byte[255]) };
            yield return new object[] { FillWithData(new byte[256]) };
            yield return new object[] { FillWithData(new byte[511]) };
            yield return new object[] { FillWithData(new byte[512]) };
            yield return new object[] { FillWithData(new byte[513]) };
            yield return new object[] { FillWithData(new byte[1023]) };
            yield return new object[] { FillWithData(new byte[1024]) };
            yield return new object[] { FillWithData(new byte[1025]) };
            yield return new object[] { FillWithData(new byte[2047]) };
            yield return new object[] { FillWithData(new byte[2048]) };
            yield return new object[] { FillWithData(new byte[2049]) };
        }

        private static byte[] FillWithData(byte[] buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = unchecked((byte)i);
            }

            return buffer;
        }
    }
}
