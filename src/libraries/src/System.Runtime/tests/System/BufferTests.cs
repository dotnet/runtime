// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Tests
{
    public static class BufferTests
    {
        [Fact]
        public static void BlockCopy()
        {
            byte[] src = new byte[] { 0x1a, 0x2b, 0x3c, 0x4d };
            byte[] dst = new byte[] { 0x6f, 0x6f, 0x6f, 0x6f, 0x6f, 0x6f };
            Buffer.BlockCopy(src, 0, dst, 1, 4);
            Assert.Equal(new byte[] { 0x6f, 0x1a, 0x2b, 0x3c, 0x4d, 0x6f }, dst);
        }

        [Fact]
        public static void BlockCopy_SameDestinationAndSourceArray()
        {
            byte[] dst = new byte[] { 0x1a, 0x2b, 0x3c, 0x4d, 0x5e };
            Buffer.BlockCopy(dst, 1, dst, 2, 2);
            Assert.Equal(new byte[] { 0x1a, 0x2b, 0x2b, 0x3c, 0x5e }, dst);
        }

        [Fact]
        public static void BlockCopy_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("src", () => Buffer.BlockCopy(null, 0, new int[3], 0, 0)); // Src is null
            AssertExtensions.Throws<ArgumentNullException>("dst", () => Buffer.BlockCopy(new string[3], 0, null, 0, 0)); // Dst is null

            AssertExtensions.Throws<ArgumentOutOfRangeException>("srcOffset", () => Buffer.BlockCopy(new byte[3], -1, new byte[3], 0, 0)); // SrcOffset < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("dstOffset", () => Buffer.BlockCopy(new byte[3], 0, new byte[3], -1, 0)); // DstOffset < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => Buffer.BlockCopy(new byte[3], 0, new byte[3], 0, -1)); // Count < 0

            AssertExtensions.Throws<ArgumentException>("src", () => Buffer.BlockCopy(new string[3], 0, new byte[3], 0, 0)); // Src is not a byte array
            AssertExtensions.Throws<ArgumentException>("dst", "dest", () => Buffer.BlockCopy(new byte[3], 0, new string[3], 0, 0)); // Dst is not a byte array

            AssertExtensions.Throws<ArgumentException>("", () => Buffer.BlockCopy(new byte[3], 3, new byte[3], 0, 1)); // SrcOffset + count >= src.length
            AssertExtensions.Throws<ArgumentException>("", () => Buffer.BlockCopy(new byte[3], 4, new byte[3], 0, 0)); // SrcOffset >= src.Length

            AssertExtensions.Throws<ArgumentException>("", () => Buffer.BlockCopy(new byte[3], 0, new byte[3], 3, 1)); // DstOffset + count >= dst.Length
            AssertExtensions.Throws<ArgumentException>("", () => Buffer.BlockCopy(new byte[3], 0, new byte[3], 4, 0)); // DstOffset >= dst.Length
        }

        public static unsafe IEnumerable<object[]> ByteLength_TestData()
        {
            return new object[][]
            {
            new object[] { typeof(byte), sizeof(byte) },
            new object[] { typeof(sbyte), sizeof(sbyte) },
            new object[] { typeof(short), sizeof(short) },
            new object[] { typeof(ushort), sizeof(ushort) },
            new object[] { typeof(int), sizeof(int) },
            new object[] { typeof(uint), sizeof(uint) },
            new object[] { typeof(long), sizeof(long) },
            new object[] { typeof(ulong), sizeof(ulong) },
            new object[] { typeof(IntPtr), sizeof(IntPtr) },
            new object[] { typeof(UIntPtr), sizeof(UIntPtr) },
            new object[] { typeof(double), sizeof(double) },
            new object[] { typeof(float), sizeof(float) },
            new object[] { typeof(bool), sizeof(bool) },
            new object[] { typeof(char), sizeof(char) }
            };
        }

        [Theory]
        [MemberData(nameof(ByteLength_TestData))]
        public static void ByteLength(Type type, int size)
        {
            const int Length = 25;
            Array array = Array.CreateInstance(type, Length);
            Assert.Equal(Length * size, Buffer.ByteLength(array));
        }

        [Fact]
        public static void ByteLength_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("array", () => Buffer.ByteLength(null)); // Array is null

            AssertExtensions.Throws<ArgumentException>("array", () => Buffer.ByteLength(Array.CreateInstance(typeof(DateTime), 25))); // Array is not a primitive
            AssertExtensions.Throws<ArgumentException>("array", () => Buffer.ByteLength(Array.CreateInstance(typeof(decimal), 25))); // Array is not a primitive
            AssertExtensions.Throws<ArgumentException>("array", () => Buffer.ByteLength(Array.CreateInstance(typeof(string), 25))); // Array is not a primitive
        }

        [Theory]
        [InlineData(new uint[] { 0x01234567, 0x89abcdef }, 0, 0x67)]
        [InlineData(new uint[] { 0x01234567, 0x89abcdef }, 7, 0x89)]
        public static void GetByte(Array array, int index, int expected)
        {
            Assert.Equal(expected, Buffer.GetByte(array, index));
        }

        [Fact]
        public static void GetByte_Invalid()
        {
            var array = new uint[] { 0x01234567, 0x89abcdef };

            AssertExtensions.Throws<ArgumentNullException>("array", () => Buffer.GetByte(null, 0)); // Array is null
            AssertExtensions.Throws<ArgumentException>("array", () => Buffer.GetByte(new object[10], 0)); // Array is not a primitive array

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => Buffer.GetByte(array, -1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => Buffer.GetByte(array, 8)); // Index >= array.Length
        }

        [Theory]
        [InlineData(25000, 0, 30000, 0, 25000)]
        [InlineData(25000, 0, 30000, 5000, 25000)]
        [InlineData(25000, 5000, 30000, 6000, 20000)]
        [InlineData(25000, 5000, 30000, 6000, 4000)]
        [InlineData(100, 0, 100, 0, 0)]
        [InlineData(100, 0, 100, 0, 1)]
        [InlineData(100, 0, 100, 0, 2)]
        [InlineData(100, 0, 100, 0, 3)]
        [InlineData(100, 0, 100, 0, 4)]
        [InlineData(100, 0, 100, 0, 5)]
        [InlineData(100, 0, 100, 0, 6)]
        [InlineData(100, 0, 100, 0, 7)]
        [InlineData(100, 0, 100, 0, 8)]
        [InlineData(100, 0, 100, 0, 9)]
        [InlineData(100, 0, 100, 0, 10)]
        [InlineData(100, 0, 100, 0, 11)]
        [InlineData(100, 0, 100, 0, 12)]
        [InlineData(100, 0, 100, 0, 13)]
        [InlineData(100, 0, 100, 0, 14)]
        [InlineData(100, 0, 100, 0, 15)]
        [InlineData(100, 0, 100, 0, 16)]
        [InlineData(100, 0, 100, 0, 17)]
        public static unsafe void MemoryCopy(int sourceLength, int sourceIndexOffset, int destinationLength, int destinationIndexOffset, long sourceBytesToCopy)
        {
            var sourceArray = new byte[sourceLength];
            for (int i = 0; i < sourceArray.Length; i++)
            {
                sourceArray[i] = unchecked((byte)i);
            }

            var destinationArray = new byte[destinationLength];
            for (int i = 0; i < destinationArray.Length; i++)
            {
                destinationArray[i] = unchecked((byte)(i * 2));
            }
            fixed (byte* sourceBase = sourceArray, destinationBase = destinationArray)
            {
                Buffer.MemoryCopy(sourceBase + sourceIndexOffset, destinationBase + destinationIndexOffset, destinationLength, sourceBytesToCopy);
            }

            for (int i = 0; i < destinationIndexOffset; i++)
            {
                Assert.Equal(unchecked((byte)(i * 2)), destinationArray[i]);
            }
            for (int i = 0; i < sourceBytesToCopy; i++)
            {
                Assert.Equal(sourceArray[i + sourceIndexOffset], destinationArray[i + destinationIndexOffset]);
            }
            for (long i = destinationIndexOffset + sourceBytesToCopy; i < destinationArray.Length; i++)
            {
                Assert.Equal(unchecked((byte)(i * 2)), destinationArray[i]);
            }
        }

        [Theory]
        [InlineData(200, 50, 100)]
        [InlineData(200, 5, 15)]
        public static unsafe void MemoryCopy_OverlappingBuffers(int sourceLength, int destinationIndexOffset, int sourceBytesToCopy)
        {
            var array = new int[sourceLength];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = i;
            }

            fixed (int* arrayBase = array)
            {
                Buffer.MemoryCopy(arrayBase, arrayBase + destinationIndexOffset, sourceLength * 4, sourceBytesToCopy * 4);
            }

            for (int i = 0; i < sourceBytesToCopy; i++)
            {
                Assert.Equal(i, array[i + destinationIndexOffset]);
            }
        }

        [Fact]
        public static unsafe void MemoryCopy_Invalid()
        {
            var sourceArray = new int[5000];
            var destinationArray = new int[1000];

            AssertExtensions.Throws<ArgumentOutOfRangeException>("sourceBytesToCopy", () =>
            {
                fixed (int* sourceBase = sourceArray, destinationBase = destinationArray)
                {
                    Buffer.MemoryCopy(sourceBase, destinationBase, 5000 * 4, 20000 * 4); // Source bytes to copy > destination size in bytes
            }
            });

            AssertExtensions.Throws<ArgumentOutOfRangeException>("sourceBytesToCopy", () =>
            {
                fixed (int* sourceBase = sourceArray, destinationBase = destinationArray)
                {
                    Buffer.MemoryCopy(sourceBase, destinationBase, (ulong)5000 * 4, (ulong)20000 * 4); // Source bytes to copy > destination size in bytes
            }
            });
        }

        [Theory]
        [InlineData(new uint[] { 0x01234567, 0x89abcdef }, 0, 0x42, new uint[] { 0x01234542, 0x89abcdef })]
        [InlineData(new uint[] { 0x01234542, 0x89abcdef }, 7, 0xa2, new uint[] { 0x01234542, 0xa2abcdef })]
        public static void SetByte(Array array, int index, byte value, Array expected)
        {
            Buffer.SetByte(array, index, value);
            Assert.Equal(expected, array);
        }

        [Fact]
        public static void SetByte_Invalid()
        {
            var array = new uint[] { 0x01234567, 0x89abcdef };

            AssertExtensions.Throws<ArgumentNullException>("array", () => Buffer.SetByte(null, 0, 0xff)); // Array is null
            AssertExtensions.Throws<ArgumentException>("array", () => Buffer.SetByte(new object[10], 0, 0xff)); // Array is not a primitive array

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => Buffer.SetByte(array, -1, 0xff)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => Buffer.SetByte(array, 8, 0xff)); // Index > array.Length
        }
    }
}
