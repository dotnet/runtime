// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.MemoryTests
{
    public static partial class MemoryTests
    {
        [Fact]
        public static void ToArray1()
        {
            int[] a = { 91, 92, 93 };
            var memory = new Memory<int>(a);
            int[] copy = memory.ToArray();
            Assert.Equal<int>(a, copy);
            Assert.NotSame(a, copy);
        }

        [Fact]
        public static void ToArrayEmpty()
        {
            Memory<int> memory = Memory<int>.Empty;
            int[] copy = memory.ToArray();
            Assert.Equal(0, copy.Length);
        }
    }
}
