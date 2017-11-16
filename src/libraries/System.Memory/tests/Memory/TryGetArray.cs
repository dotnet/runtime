// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using Xunit;

namespace System.MemoryTests
{
    public static partial class MemoryTests
    {
        [Fact]
        public static void MemoryTryGetArray()
        {
            int[] array = new int[10];
            Memory<int> memory = array;
            Assert.True(memory.TryGetArray(out ArraySegment<int> segment));
            Assert.Equal(array.Length, segment.Count);

            for (int i = segment.Offset; i < segment.Count + segment.Offset; i++)
            {
                Assert.Equal(array[i], segment.Array[i]);
            }
        }

        [Fact]
        public static void TryGetArrayFromDefaultMemory()
        {
            Memory<int> memory = default;
            Assert.False(memory.TryGetArray(out ArraySegment<int> segment));
            Assert.True(segment.Equals(default));
        }

        [Fact]
        public static void OwnedMemoryTryGetArray()
        {
            int[] array = new int[10];
            OwnedMemory<int> owner = new CustomMemoryForTest<int>(array);
            Memory<int> memory = owner.Memory;
            Assert.True(memory.TryGetArray(out ArraySegment<int> segment));
            Assert.Equal(array.Length, segment.Count);

            for (int i = segment.Offset; i < segment.Count + segment.Offset; i++)
            {
                Assert.Equal(array[i], segment.Array[i]);
            }
        }
    }
}
