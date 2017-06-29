// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using Windows.Storage.Streams;
using Xunit;

namespace System.Runtime.InteropServices.WindowsRuntime.Tests
{
    public class WindowsRuntimeBufferExtensionsTests
    {
        public static IEnumerable<object[]> AsBuffer_TestData()
        {
            yield return new object[] { new byte[0], 0, 0, 0 };
            yield return new object[] { new byte[] { 1, 2, 3 }, 2, 0, 0 };
            yield return new object[] { new byte[] { 1, 2, 3 }, 2, 0, 1 };
            yield return new object[] { new byte[] { 1, 2, 3 }, 0, 3, 3 };
            yield return new object[] { new byte[] { 1, 2, 3 }, 1, 2, 2 };
        }

        [Theory]
        [MemberData(nameof(AsBuffer_TestData))]
        public void AsBuffer_Buffer_ReturnsExpected(byte[] source, int offset, int length, int capacity)
        {
            if (capacity == length)
            {
                if (offset == 0 && length == source.Length)
                {
                    Verify(WindowsRuntimeBufferExtensions.AsBuffer(source), source, offset, length, capacity);
                }

                Verify(WindowsRuntimeBufferExtensions.AsBuffer(source, offset, length), source, offset, length, capacity);
            }

            Verify(WindowsRuntimeBufferExtensions.AsBuffer(source, offset, length, capacity), source, offset, length, capacity);
        }

        [Fact]
        public void AsBuffer_NullBuffer_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => WindowsRuntimeBufferExtensions.AsBuffer(null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => WindowsRuntimeBufferExtensions.AsBuffer(null, 0, 0));
            AssertExtensions.Throws<ArgumentNullException>("source", () => WindowsRuntimeBufferExtensions.AsBuffer(null, 0, 0, 0));
        }

        [Fact]
        public void AsBuffer_NegativeOffset_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => WindowsRuntimeBufferExtensions.AsBuffer(new byte[0], -1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => WindowsRuntimeBufferExtensions.AsBuffer(new byte[0], -1, 0, 0));
        }

        [Fact]
        public void AsBuffer_NegativeLength_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => WindowsRuntimeBufferExtensions.AsBuffer(new byte[0], 0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => WindowsRuntimeBufferExtensions.AsBuffer(new byte[0], 0, -1, 0));
        }

        [Theory]
        [InlineData(new byte[0], 0, 1, 0)]
        [InlineData(new byte[0], 1, 0, 0)]
        [InlineData(new byte[] { 0, 0 }, 1, 2, 0)]
        [InlineData(new byte[] { 0, 0 }, int.MaxValue, 0, 0)]
        [InlineData(new byte[] { 0, 0 }, 0, 0, 3)]
        [InlineData(new byte[] { 0, 0 }, 0, 0, int.MaxValue)]
        [InlineData(new byte[] { 0, 0 }, 0, 2, 1)]
        public void AsBuffer_InvalidOffsetLengthCapacity_ThrowsArgumentException(byte[] data, int offset, int length, int capacity)
        {
            if (capacity == 0)
            {
                AssertExtensions.Throws<ArgumentException>(null, () => WindowsRuntimeBufferExtensions.AsBuffer(data, offset, length));
            }

            AssertExtensions.Throws<ArgumentException>(null, () => WindowsRuntimeBufferExtensions.AsBuffer(data, offset, length, capacity));
        }

        [Fact]
        public void AsBuffer_NegativeCapacity_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => WindowsRuntimeBufferExtensions.AsBuffer(new byte[0], 0, 0, -1));
        }

        public static IEnumerable<object[]> AsStream_TestData()
        {
            yield return new object[] { new byte[0].AsBuffer(), new byte[0] };
            yield return new object[] { new byte[] { 1, 2, 3 }.AsBuffer(), new byte[] { 1, 2, 3 } };
            yield return new object[] { new byte[] { 1, 2, 3 }.AsBuffer(1, 2), new byte[] { 2, 3 } };
        }

        [Theory]
        [MemberData(nameof(AsStream_TestData))]
        public void AsStream_Buffer_Success(IBuffer buffer, byte[] expected)
        {
            using (MemoryStream stream = (MemoryStream)buffer.AsStream())
            {
                Assert.Equal(expected.Length, stream.Length);
                Assert.True(stream.CanWrite);

                Assert.Equal(expected, stream.ToArray());
            }
        }

        [Fact]
        public void AsStream_NullBuffer_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => WindowsRuntimeBufferExtensions.AsStream(null));
        }

        [Fact]
        public void AsStream_CustomBuffer_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => WindowsRuntimeBufferExtensions.AsStream(new CustomBuffer()));
        }

        public static IEnumerable<object[]> CopyTo_TestData()
        {
            yield return new object[] { new byte[] { 1, 2, 3 }, 0, new byte[] { 2, 3, 4 }, 0, 3, new byte[] { 1, 2, 3 } };
            yield return new object[] { new byte[] { 1, 2, 3, 4, 5 }, 3, new byte[] { 255, 254, 253, 252 }, 2, 1, new byte[] { 255, 254, 4, 252 } };
            yield return new object[] { new byte[] { 1, 2, 3, 4, 5 }, 0, new byte[] { 255, 254, 253, 252 }, 0, 0, new byte[] { 255, 254, 253, 252 } };
        }

        [Theory]
        [MemberData(nameof(CopyTo_TestData))]
        public void CopyTo_Buffer_Success(byte[] source, int sourceIndex, byte[] destination, int destinationIndex, int count, byte[] expected)
        {
            byte[] Clone(byte[] array) => (byte[])array.Clone();
            IBuffer Buffer(byte[] array) => Clone(array).AsBuffer();

            if (sourceIndex == 0 && destinationIndex == 0 && count == source.Length)
            {
                // CopyTo(byte[], IBuffer)
                byte[] source1 = Clone(source);
                IBuffer destination1 = Buffer(destination);
                WindowsRuntimeBufferExtensions.CopyTo(source1, destination1);
                Assert.Equal(expected, destination1.ToArray());

                // CopyTo(IBuffer, byte[])
                IBuffer source2 = Buffer(source);
                byte[] destination2 = Clone(destination);
                WindowsRuntimeBufferExtensions.CopyTo(source2, destination2);
                Assert.Equal(expected, destination2);

                // CopyTo(IBuffer, IBuffer)
                IBuffer source3 = Buffer(source);
                IBuffer destination3 = Buffer(destination);
                WindowsRuntimeBufferExtensions.CopyTo(source3, destination3);
                Assert.Equal(expected, destination3.ToArray());
            }

            // CopyTo(byte[], int, IBuffer, int, int)
            byte[] source4 = Clone(source);
            IBuffer destination4 = Buffer(destination);
            WindowsRuntimeBufferExtensions.CopyTo(source4, sourceIndex, destination4, (uint)destinationIndex, count);
            Assert.Equal(expected, destination4.ToArray());

            // CopyTo(IBuffer, int, byte[], int, int)
            IBuffer source5 = Buffer(source);
            byte[] destination5 = Clone(destination);
            WindowsRuntimeBufferExtensions.CopyTo(source5, (uint)sourceIndex, destination5, destinationIndex, count);
            Assert.Equal(expected, destination5);

            // CopyTo(IBuffer, int, IBuffer, int, int)
            IBuffer source6 = Buffer(source);
            IBuffer destination6 = Buffer(destination);
            WindowsRuntimeBufferExtensions.CopyTo(source6, (uint)sourceIndex, destination6, (uint)destinationIndex, (uint)count);
            Assert.Equal(expected, destination6.ToArray());
        }

        [Fact]
        public void CopyTo_NullSource_ThrowsArgumentNullException()
        {
            IBuffer buffer = WindowsRuntimeBufferExtensions.AsBuffer(new byte[0]);
            AssertExtensions.Throws<ArgumentNullException>("source", () => WindowsRuntimeBufferExtensions.CopyTo((byte[])null, buffer));
            AssertExtensions.Throws<ArgumentNullException>("source", () => WindowsRuntimeBufferExtensions.CopyTo(null, new byte[0]));
            AssertExtensions.Throws<ArgumentNullException>("source", () => WindowsRuntimeBufferExtensions.CopyTo((IBuffer)null, buffer));
            AssertExtensions.Throws<ArgumentNullException>("source", () => WindowsRuntimeBufferExtensions.CopyTo(null, 0, buffer, 0, 0));
            AssertExtensions.Throws<ArgumentNullException>("source", () => WindowsRuntimeBufferExtensions.CopyTo(null, 0, new byte[0], 0, 0));
            AssertExtensions.Throws<ArgumentNullException>("source", () => WindowsRuntimeBufferExtensions.CopyTo((IBuffer)null, 0, buffer, 0, 0));
        }

        [Fact]
        public void CopyTo_NullDestination_ThrowsArgumentNullException()
        {
            IBuffer buffer = WindowsRuntimeBufferExtensions.AsBuffer(new byte[0]);
            AssertExtensions.Throws<ArgumentNullException>("destination", () => WindowsRuntimeBufferExtensions.CopyTo(new byte[0], null));
            AssertExtensions.Throws<ArgumentNullException>("destination", () => WindowsRuntimeBufferExtensions.CopyTo(buffer, (IBuffer)null));
            AssertExtensions.Throws<ArgumentNullException>("destination", () => WindowsRuntimeBufferExtensions.CopyTo(buffer, (byte[])null));
            AssertExtensions.Throws<ArgumentNullException>("destination", () => WindowsRuntimeBufferExtensions.CopyTo(new byte[0], 0, null, 0, 0));
            AssertExtensions.Throws<ArgumentNullException>("destination", () => WindowsRuntimeBufferExtensions.CopyTo(buffer, 0, null, 0, 0));
            AssertExtensions.Throws<ArgumentNullException>("destination", () => WindowsRuntimeBufferExtensions.CopyTo(buffer, 0, (IBuffer)null, 0, 0));
        }

        [Fact]
        public void CopyTo_NegativeSourceIndex_ThrowsArgumentOutOfRangeException()
        {
            IBuffer buffer = WindowsRuntimeBufferExtensions.AsBuffer(new byte[0]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("sourceIndex", () => WindowsRuntimeBufferExtensions.CopyTo(new byte[0], -1, buffer, 0, 0));
        }

        [Fact]
        public void CopyTo_NegativeDestinationIndex_ThrowsArgumentOutOfRangeException()
        {
            IBuffer buffer = WindowsRuntimeBufferExtensions.AsBuffer(new byte[0]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("destinationIndex", () => WindowsRuntimeBufferExtensions.CopyTo(buffer, 0, new byte[0], -1, 0));
        }

        [Fact]
        public void CopyTo_LargeSourceIndex_ThrowsArgumentException()
        {
            IBuffer buffer = WindowsRuntimeBufferExtensions.AsBuffer(new byte[0]);
            AssertExtensions.Throws<ArgumentException>("sourceIndex", () => WindowsRuntimeBufferExtensions.CopyTo(new byte[0], 1, buffer, 0, 0));
            AssertExtensions.Throws<ArgumentException>(null, () => WindowsRuntimeBufferExtensions.CopyTo(buffer, 1, buffer, 0, 0));
            AssertExtensions.Throws<ArgumentException>(null, () => WindowsRuntimeBufferExtensions.CopyTo(buffer, 1, new byte[0], 0, 0));
        }

        [Fact]
        public void CopyTo_NegativeCount_ThrowsArgumentOutOfRangeException()
        {
            IBuffer buffer = WindowsRuntimeBufferExtensions.AsBuffer(new byte[0]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => WindowsRuntimeBufferExtensions.CopyTo(new byte[0], 0, buffer, 0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => WindowsRuntimeBufferExtensions.CopyTo(buffer, 0, new byte[0], 0, -1));
        }

        [Theory]
        [InlineData(new byte[0], 0, 0)]
        [InlineData(new byte[0], 0, 1)]
        [InlineData(new byte[] { 0, 0 }, 2, 0)]
        [InlineData(new byte[] { 0, 0 }, 1, 2)]
        public void CopyTo_InvalidSourceIndexCount_ThrowsArgumentException(byte[] bytes, uint sourceIndex, uint count)
        {
            IBuffer buffer = WindowsRuntimeBufferExtensions.AsBuffer(bytes);
            AssertExtensions.Throws<ArgumentException>(sourceIndex >= bytes.Length ? "sourceIndex" : null, () => WindowsRuntimeBufferExtensions.CopyTo(bytes, (int)sourceIndex, buffer, 0, (int)count));
            AssertExtensions.Throws<ArgumentException>(null, () => WindowsRuntimeBufferExtensions.CopyTo(buffer, sourceIndex, buffer, 0, count));
            AssertExtensions.Throws<ArgumentException>(null, () => WindowsRuntimeBufferExtensions.CopyTo(buffer, sourceIndex, new byte[0], 0, (int)count));
        }

        [Theory]
        [InlineData(new byte[] { 0, 0 }, 2, 1)]
        [InlineData(new byte[] { 0, 0 }, 1, 2)]
        public void CopyTo_InvalidDestinationIndexCount_ThrowsArgumentException(byte[] bytes, uint destinationIndex, uint count)
        {
            IBuffer buffer = WindowsRuntimeBufferExtensions.AsBuffer(bytes);
            AssertExtensions.Throws<ArgumentException>(null, () => WindowsRuntimeBufferExtensions.CopyTo(new byte[10], 0, buffer, destinationIndex, (int)count));
            AssertExtensions.Throws<ArgumentException>(null, () => WindowsRuntimeBufferExtensions.CopyTo(new byte[10].AsBuffer(), 0, bytes, (int)destinationIndex, (int)count));
            AssertExtensions.Throws<ArgumentException>(null, () => WindowsRuntimeBufferExtensions.CopyTo(new byte[10].AsBuffer(), 0, buffer, destinationIndex, count));
        }

        [Fact]
        public void CopyTo_CustomBuffer_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => WindowsRuntimeBufferExtensions.CopyTo(new byte[10], new CustomBuffer()));
            Assert.Throws<InvalidCastException>(() => WindowsRuntimeBufferExtensions.CopyTo(new CustomBuffer(), new byte[10]));
            Assert.Throws<InvalidCastException>(() => WindowsRuntimeBufferExtensions.CopyTo(new CustomBuffer(), new CustomBuffer()));
            Assert.Throws<InvalidCastException>(() => WindowsRuntimeBufferExtensions.CopyTo(new byte[10], 0, new CustomBuffer(), 0, 0));
            Assert.Throws<InvalidCastException>(() => WindowsRuntimeBufferExtensions.CopyTo(new CustomBuffer(), 0, new byte[10], 0, 0));

            Assert.Throws<InvalidCastException>(() => WindowsRuntimeBufferExtensions.CopyTo(new CustomBuffer(), 0, new CustomBuffer(), 0, 0));
            Assert.Throws<InvalidCastException>(() => WindowsRuntimeBufferExtensions.CopyTo(new CustomBuffer(), 0, new byte[10].AsBuffer(), 0, 0));
            Assert.Throws<InvalidCastException>(() => WindowsRuntimeBufferExtensions.CopyTo(new byte[10].AsBuffer(), 0, new CustomBuffer(), 0, 0));
        }

        public static IEnumerable<object[]> IsSameData_TestData()
        {
            byte[] data = new byte[] { 1, 2, 3 };
            IBuffer buffer = data.AsBuffer();
            yield return new object[] { buffer, buffer, true };
            yield return new object[] { buffer, data.AsBuffer(), true };
            yield return new object[] { buffer, new byte[] { 1, 3, 3 }.AsBuffer(), false };

            yield return new object[] { buffer, new CustomBuffer(), false };
            yield return new object[] { buffer, null, false };
        }

        [Theory]
        [MemberData(nameof(IsSameData_TestData))]
        public void IsSameData_Buffer_ReturnsExpected(IBuffer buffer, IBuffer other, bool expected)
        {
            Assert.Equal(expected, WindowsRuntimeBufferExtensions.IsSameData(buffer, other));
        }

        [Fact]
        public void IsSameData_CustomBuffer_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => WindowsRuntimeBufferExtensions.IsSameData(new CustomBuffer(), new CustomBuffer()));
        }

        [Fact]
        public void IsSameData_NullBuffer_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("buffer", () => WindowsRuntimeBufferExtensions.IsSameData(null, new byte[0].AsBuffer()));
        }

        [Fact]
        public void GetByte_NullBuffer_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => WindowsRuntimeBufferExtensions.GetByte(null, 0));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void GetByte_InvalidOffset_ThrowsArgumentException(uint byteOffset)
        {
            IBuffer buffer = new byte[1].AsBuffer();
            AssertExtensions.Throws<ArgumentException>("byteOffset", () => WindowsRuntimeBufferExtensions.GetByte(buffer, byteOffset));
        }

        [Fact]
        public void GetByte_CustomBuffer_ThrowsInvalidCastExceptionException()
        {
            Assert.Throws<InvalidCastException>(() => WindowsRuntimeBufferExtensions.GetByte(new CustomBuffer(), 2));
        }

        public static IEnumerable<object[]> GetWindowsRuntimeBuffer_TestData()
        {
            yield return new object[] { new byte[] { 1, 2, 3 }, 0, 3 };
            yield return new object[] { new byte[] { 1, 2, 3 }, 1, 2 };
            yield return new object[] { new byte[] { 1, 2, 3 }, 3, 0 };
        }

        [Theory]
        [MemberData(nameof(GetWindowsRuntimeBuffer_TestData))]
        public void GetWindowsRuntimeBuffer_Stream_Success(byte[] bytes, int positionInStream, int length)
        {
            using (var stream = new MemoryStream())
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Position = 0;

                if (positionInStream == 0 && length == bytes.Length)
                {
                    Verify(WindowsRuntimeBufferExtensions.GetWindowsRuntimeBuffer(stream), bytes, positionInStream, length, stream.Capacity);
                }

                stream.Position = 0;
                Verify(WindowsRuntimeBufferExtensions.GetWindowsRuntimeBuffer(stream, positionInStream, length), bytes, positionInStream, length, length);
            }
        }

        [Fact]
        public void GetWindowsRuntimeBuffer_NullStream_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("underlyingStream", () => WindowsRuntimeBufferExtensions.GetWindowsRuntimeBuffer(null));
            AssertExtensions.Throws<ArgumentNullException>("underlyingStream", () => WindowsRuntimeBufferExtensions.GetWindowsRuntimeBuffer(null, 0, 0));
        }

        [Fact]
        public void GetWindowsRuntimeBuffer_NonWritableStream_ThrowsUnauthorizedAccessException()
        {
            var memoryStream = new MemoryStream(new byte[10], false);
            Assert.Throws<UnauthorizedAccessException>(() => WindowsRuntimeBufferExtensions.GetWindowsRuntimeBuffer(memoryStream));
            Assert.Throws<UnauthorizedAccessException>(() => WindowsRuntimeBufferExtensions.GetWindowsRuntimeBuffer(memoryStream, 0, 0));
        }

        [Fact]
        public void GetWindowsRuntimeBuffer_NegativePositionInStream_ThrowsArgumentOufOfRangeException()
        {
            var memoryStream = new MemoryStream();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("positionInStream", () => WindowsRuntimeBufferExtensions.GetWindowsRuntimeBuffer(memoryStream, -1, 0));
        }

        [Fact]
        public void GetWindowsRuntimeBuffer_NegativeLength_ThrowsArgumentOufOfRangeException()
        {
            var memoryStream = new MemoryStream();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => WindowsRuntimeBufferExtensions.GetWindowsRuntimeBuffer(memoryStream, 0, -1));
        }

        [Fact]
        public void GetWindowsRuntimeBuffer_EmptyStream_ThrowsArgumentException()
        {
            using (var stream = new MemoryStream())
            {
                Assert.Throws<ArgumentException>(null, () => WindowsRuntimeBufferExtensions.GetWindowsRuntimeBuffer(stream, 0, 0));
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void GetWindowsRuntimeBuffer_PositonInStreamGreaterOrEqualToCapacity_ThrowsArgumentException(int positionInStream)
        {
            var memoryStream = new MemoryStream(1);
            AssertExtensions.Throws<ArgumentException>(null, () => WindowsRuntimeBufferExtensions.GetWindowsRuntimeBuffer(memoryStream, positionInStream, 0));
        }

        [Fact]
        public void GetWindowsRuntimeBuffer_BufferWithLengthGreaterThanIntMax_Throws()
        {
            var memoryStream = new SubMemoryStream(10);
            WindowsRuntimeBufferExtensions.GetWindowsRuntimeBuffer(memoryStream, 0, 0);
        }

        public static IEnumerable<object[]> ToArray_TestData()
        {
            yield return new object[] { new byte[] { 1, 2, 3 }, 0, 3, new byte[] { 1, 2, 3 } };
            yield return new object[] { new byte[] { 1, 2, 3 }, 2, 0, new byte[0] };
            yield return new object[] { new byte[] { 1, 2, 3 }, 1, 2, new byte[] { 2, 3 } };
        }

        [Theory]
        [MemberData(nameof(ToArray_TestData))]
        public void ToArray_Buffer_ReturnsExpected(byte[] buffer, uint index, int count, byte[] expected)
        {
            if (index == 0 && count == buffer.Length)
            {
                Assert.Equal(expected, buffer.AsBuffer().ToArray());
            }

            Assert.Equal(expected, buffer.AsBuffer().ToArray(index, count));
        }

        [Fact]
        public void ToArray_NullBuffer_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => WindowsRuntimeBufferExtensions.ToArray(null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => WindowsRuntimeBufferExtensions.ToArray(null, 0, 0));
        }

        [Fact]
        public void ToArray_NegativeCount_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => WindowsRuntimeBufferExtensions.ToArray(new byte[0].AsBuffer(), 0, -1));
        }

        [Theory]
        [InlineData(new byte[0], 0, 0)]
        [InlineData(new byte[] { 0 }, 1, 0)]
        [InlineData(new byte[] { 0, 0, 0 }, 0, 4)]
        [InlineData(new byte[] { 0, 0, 0 }, 1, 3)]
        public void ToArray_InvalidIndexCount_ThrowsArgumentException(byte[] buffer, uint index, int count)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => WindowsRuntimeBufferExtensions.ToArray(buffer.AsBuffer(), index, count));
        }

        private static void Verify(IBuffer buffer, byte[] source, int offset, int length, int capacity)
        {
            Assert.Equal(length, (int)buffer.Length);
            Assert.Equal(capacity, (int)buffer.Capacity);

            for (uint i = 0; i < length; i++)
            {
                Assert.Equal(source[i + offset], buffer.GetByte(i));
            }
        }

        private class SubMemoryStream : MemoryStream
        {
            public SubMemoryStream(int capacity) : base(capacity) { }

            public override long Length => long.MaxValue;
        }

        private class CustomBuffer : IBuffer
        {
            public uint Capacity => 10;
            public uint Length { get; set; } = 10;
        }
    }
}
