// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.MemoryTests
{
    public static partial class ReadOnlyMemoryTests
    {
        [Fact]
        public static void Empty()
        {
            ReadOnlyMemory<int> empty = ReadOnlyMemory<int>.Empty;
            Assert.True(empty.IsEmpty);
            Assert.Equal(0, empty.Length);
        }

        [Fact]
        public static void IsEmpty()
        {
            ReadOnlyMemory<int> empty = new int[0];
            Assert.True(empty.IsEmpty);
            Assert.Equal(0, empty.Length);
        }

        [Fact]
        public static void EmptyEqualsDefault()
        {
            ReadOnlyMemory<int> empty = ReadOnlyMemory<int>.Empty;
            ReadOnlyMemory<int> defaultMemory = default;
            Assert.True(defaultMemory.Equals(empty));
            Assert.True(defaultMemory.IsEmpty);
            Assert.Equal(0, defaultMemory.Length);
        }
    }
}
