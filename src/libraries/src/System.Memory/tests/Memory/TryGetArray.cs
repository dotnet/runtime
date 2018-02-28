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
            Assert.True(memory.TryGetArray(out ArraySegment<int> segment));
            Assert.Equal(0, segment.Array.Length);
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
        
        [Fact]
        public static void TryGetArrayFromEmptyMemory()
        {
            int[] array = new int[0];
            Memory<int> memory = array;

            Assert.True(memory.TryGetArray(out ArraySegment<int> segment));
            Assert.Same(array, segment.Array);
            Assert.Equal(0, segment.Array.Length);

            Assert.True(Memory<byte>.Empty.TryGetArray(out ArraySegment<byte> byteSegment));
            Assert.Equal(0, byteSegment.Array.Length);
        }

        [Fact]
        public static void TryGetArrayFromEmptyOwnedMemory()
        {
            int[] array = new int[0];
            OwnedMemory<int> owner = new CustomMemoryForTest<int>(array);

            Assert.True(owner.Memory.TryGetArray(out ArraySegment<int> segment));
            Assert.Same(array, segment.Array);
            Assert.Equal(0, segment.Array.Length);
        }

        [Fact]
        public static void TryGetArrayFromEmptyNonRetrievableOwnedMemory()
        {
            using (var owner = new NativeOwnedMemory(0))
            {
                Assert.True(owner.Memory.TryGetArray(out ArraySegment<byte> segment));
                Assert.Equal(0, segment.Array.Length);
            }
        }
    }
}
