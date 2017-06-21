// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Windows.Storage.Streams;
using Xunit;

namespace System.Runtime.InteropServices.WindowsRuntime.Tests
{
    public class WindowsRuntimeBufferTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void Create_Capacity_ReturnsExpected(int capacity)
        {
            IBuffer buffer = WindowsRuntimeBuffer.Create(capacity);
            Assert.Equal(capacity, (int)buffer.Capacity);
            Assert.Equal(0, (int)buffer.Length);
        }

        public static IEnumerable<object[]> Create_TestData()
        {
            yield return new object[] { new byte[0], 0, 0, 0 };
            yield return new object[] { new byte[] { 1, 2, 3 }, 0, 3, 3 };
            yield return new object[] { new byte[] { 1, 2, 3, 4, 5, 6 }, 2, 1, 4 };
            yield return new object[] { new byte[] { 1, 2, 3 }, 1, 2, 2 };
        }

        [Theory]
        [MemberData(nameof(Create_TestData))]
        public void Create_Buffer_ReturnsExpected(byte[] source, int offset, int length, int capacity)
        {
            IBuffer buffer = WindowsRuntimeBuffer.Create(source, offset, length, capacity);
            Assert.Equal(capacity, (int)buffer.Capacity);
            Assert.Equal(length, (int)buffer.Length);

            for (uint i = 0; i < length; i++)
            {
                Assert.Equal(source[i + offset], buffer.GetByte(i));
            }

            // The source byte array should be copied.
            if (source.Length > 0)
            {
                source[0] = 45;
                Assert.NotEqual(45, buffer.GetByte(0));
            }
        }

        [Fact]
        public void Create_NegativeCapacity_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => WindowsRuntimeBuffer.Create(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => WindowsRuntimeBuffer.Create(new byte[0], 0, 0, -1));
        }

        [Fact]
        public void Create_NullData_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("data", () => WindowsRuntimeBuffer.Create(null, 0, 0, 0));
        }

        [Fact]
        public void Create_NegativeOffset_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => WindowsRuntimeBuffer.Create(new byte[0], -1, 0, 0));
        }

        [Fact]
        public void Create_NegativeLength_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => WindowsRuntimeBuffer.Create(new byte[0], 0, -1, 0));
        }

        [Theory]
        [InlineData(new byte[0], 0, 1, 0)]
        [InlineData(new byte[0], 1, 0, 0)]
        [InlineData(new byte[] { 0, 0 }, 1, 2, 0)]
        [InlineData(new byte[] { 0, 0 }, int.MaxValue, 0, 0)]
        [InlineData(new byte[] { 0, 0 }, 0, 0, 3)]
        [InlineData(new byte[] { 0, 0 }, 0, 0, int.MaxValue)]
        [InlineData(new byte[] { 0, 0 }, 0, 2, 1)]
        public void Create_InvalidOffsetLengthCapacity_ThrowsArgumentException(byte[] data, int offset, int length, int capacity)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => WindowsRuntimeBuffer.Create(data, offset, length, capacity));
        }

        [Fact]
        public void Length_SetGreaterThanCapacity_ThrowsArgumentException()
        {
            IBuffer buffer = WindowsRuntimeBuffer.Create(2);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => buffer.Length = 3);
        }
    }
}
