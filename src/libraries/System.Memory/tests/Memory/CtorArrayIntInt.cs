// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.MemoryTests
{
    //
    // Tests for MemoryTests<T>.ctor(T[], int, int). If the test is not specific to this overload, consider putting it in CtorArray.cs instread.
    //
    public static partial class MemoryTests
    {
        [Fact]
        public static void CtorArrayWithStartAndLengthInt()
        {
            int[] a = { 90, 91, 92, 93, 94, 95, 96, 97, 98 };
            var memory = new Memory<int>(a, 3, 2);
            memory.Validate(93, 94);
        }

        [Fact]
        public static void CtorArrayWithStartAndLengthLong()
        {
            long[] a = { 90, 91, 92, 93, 94, 95, 96, 97, 98 };
            var memory = new Memory<long>(a, 4, 3);
            memory.Validate(94, 95, 96);
        }

        [Fact]
        public static void CtorArrayWithStartAndLengthRangeExtendsToEndOfArray()
        {
            long[] a = { 90, 91, 92, 93, 94, 95, 96, 97, 98 };
            var memory = new Memory<long>(a, 4, 5);
            memory.Validate(94, 95, 96, 97, 98);
        }

        [Fact]
        public static void CtorArrayWithNegativeStartAndLength()
        {
            int[] a = new int[3];
            Assert.Throws<ArgumentOutOfRangeException>(() => new Memory<int>(a, -1, 0));
        }

        [Fact]
        public static void CtorArrayWithStartTooLargeAndLength()
        {
            int[] a = new int[3];
            Assert.Throws<ArgumentOutOfRangeException>(() => new Memory<int>(a, 4, 0));
        }

        [Fact]
        public static void CtorArrayWithStartAndNegativeLength()
        {
            int[] a = new int[3];
            Assert.Throws<ArgumentOutOfRangeException>(() => new Memory<int>(a, 0, -1));
        }

        [Fact]
        public static void CtorArrayWithStartAndLengthTooLarge()
        {
            int[] a = new int[3];
            Assert.Throws<ArgumentOutOfRangeException>(() => new Memory<int>(a, 3, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Memory<int>(a, 2, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Memory<int>(a, 1, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Memory<int>(a, 0, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Memory<int>(a, int.MaxValue, int.MaxValue));
        }

        [Fact]
        public static void CtorArrayWithStartAndLengthBothEqual()
        {
            // Valid for start to equal the array length. This returns an empty memory that starts "just past the array."
            int[] a = { 91, 92, 93 };
            var memory = new Memory<int>(a, 3, 0);
            memory.Validate();
        }
    }
}

