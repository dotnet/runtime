// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.MemoryTests
{
    public static partial class MemoryTests
    {
        [Fact]
        public static void SameObjectsHaveSameHashCodes()
        {
            int[] a = { 91, 92, 93, 94, 95 };
            var left = new Memory<int>(a, 2, 3);
            var right = new Memory<int>(a, 2, 3);

            int[] b = { 1, 2, 3, 4, 5 };
            var different = new Memory<int>(b, 2, 3);

            Assert.Equal(left.GetHashCode(), right.GetHashCode());
            Assert.NotEqual(left.GetHashCode(), different.GetHashCode());
        }

        [Fact]
        public static void HashCodeIncludesLength()
        {
            int[] a = { 91, 92, 93, 94, 95 };
            var left = new Memory<int>(a, 2, 1);
            var right = new Memory<int>(a, 2, 3);

            Assert.NotEqual(left.GetHashCode(), right.GetHashCode());
        }

        [Fact]
        public static void HashCodeIncludesBase()
        {
            int[] a = { 91, 92, 93, 94, 95 };
            var left = new Memory<int>(a, 1, 3);
            var right = new Memory<int>(a, 2, 3);

            Assert.NotEqual(left.GetHashCode(), right.GetHashCode());
        }

        [Fact]
        public static void HashCodesDifferentForSameContent()
        {
            var left = new Memory<int>(new int[] { 0, 1, 2 }, 1, 1);
            var right = new Memory<int>(new int[] { 0, 1, 2 }, 1, 1);

            Assert.NotEqual(left.GetHashCode(), right.GetHashCode());
        }

        [Fact]
        public static void EmptyMemoryHashCodeNotUnified()
        {
            var left = new Memory<int>(new int[0]);
            var right = new Memory<int>(new int[0]);

            Memory<int> memoryFromNonEmptyArrayButWithZeroLength = new Memory<int>(new int[1] { 123 }).Slice(0, 0);

            Assert.NotEqual(left.GetHashCode(), right.GetHashCode());
            Assert.NotEqual(left.GetHashCode(), memoryFromNonEmptyArrayButWithZeroLength.GetHashCode());
            Assert.NotEqual(right.GetHashCode(), memoryFromNonEmptyArrayButWithZeroLength.GetHashCode());

            // Empty property hashcode is equal
            left = Memory<int>.Empty;
            right = Memory<int>.Empty;

            Assert.Equal(left.GetHashCode(), right.GetHashCode());
        }

        [Fact]
        public static void HashCodesForImplicitCastsAreEqual()
        {
            byte[] bytes = new byte[10];
            var memory = new Memory<byte>(bytes);
            var readOnlyMemory = new ReadOnlyMemory<byte>(bytes);
            ReadOnlyMemory<byte> implicitReadOnlyMemory = memory;
            Memory<byte> implicitMemoryArray = bytes;
            ReadOnlyMemory<byte> implicitReadOnlyMemoryArray = bytes;

            Assert.Equal(readOnlyMemory.GetHashCode(), memory.GetHashCode());
            Assert.Equal(implicitReadOnlyMemory.GetHashCode(), memory.GetHashCode());
            Assert.Equal(implicitMemoryArray.GetHashCode(), memory.GetHashCode());
            Assert.Equal(implicitReadOnlyMemoryArray.GetHashCode(), memory.GetHashCode());
        }

        [Fact]
        public static void DefaultMemoryHashCode()
        {
            Memory<int> memory = default;
            Assert.Equal(0, memory.GetHashCode());
            Memory<int> memory2 = default;
            Assert.Equal(memory2.GetHashCode(), memory.GetHashCode());
        }
    }
}
