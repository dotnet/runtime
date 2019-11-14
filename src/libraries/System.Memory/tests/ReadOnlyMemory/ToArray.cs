// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.MemoryTests
{
    public static partial class ReadOnlyMemoryTests
    {
        [Fact]
        public static void ToArray1()
        {
            int[] a = { 91, 92, 93 };
            var memory = new ReadOnlyMemory<int>(a);
            int[] copy = memory.ToArray();
            Assert.Equal<int>(a, copy);
            Assert.NotSame(a, copy);
        }

        [Fact]
        public static void ToArrayWithIndex()
        {
            int[] a = { 91, 92, 93, 94, 95 };
            var memory = new Memory<int>(a);
            int[] copy = memory.Slice(2).ToArray();

            Assert.Equal<int>(new int[] { 93, 94, 95 }, copy);
        }

        [Fact]
        public static void ToArrayWithIndexAndLength()
        {
            int[] a = { 91, 92, 93 };
            var memory = new Memory<int>(a, 1, 1);
            int[] copy = memory.ToArray();
            Assert.Equal<int>(new int[] { 92 }, copy);
        }

        [Fact]
        public static void ToArrayEmpty()
        {
            ReadOnlyMemory<int> memory = ReadOnlyMemory<int>.Empty;
            int[] copy = memory.ToArray();
            Assert.Equal(0, copy.Length);
        }

        [Fact]
        public static void ToArrayDefault()
        {
            ReadOnlyMemory<int> memory = default;
            int[] copy = memory.ToArray();
            Assert.Equal(0, copy.Length);
        }
    }
}
