// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.MemoryTests
{
    public static partial class MemoryTests
    {
        [Fact]
        public static void Empty()
        {
            Memory<int> empty = Memory<int>.Empty;
            Assert.True(empty.IsEmpty);
            Assert.Equal(0, empty.Length);
        }

        [Fact]
        public static void IsEmpty()
        {
            Memory<int> empty = new int[0];
            Assert.True(empty.IsEmpty);
            Assert.Equal(0, empty.Length);
        }

        [Fact]
        public static void EmptyEqualsDefault()
        {
            Memory<int> empty = Memory<int>.Empty;
            Memory<int> defaultMemory = default;
            Assert.True(defaultMemory.Equals(empty));
            Assert.True(defaultMemory.IsEmpty);
            Assert.Equal(0, defaultMemory.Length);
        }
    }
}
