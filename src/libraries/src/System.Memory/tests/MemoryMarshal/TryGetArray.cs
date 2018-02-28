// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Runtime.InteropServices;
using Xunit;

namespace System.MemoryTests
{
    public static partial class MemoryMarshalTests
    {
        [Fact]
        public static void ReadOnlyMemoryTryGetArray()
        {
            int[] array = new int[10];
            ReadOnlyMemory<int> memory = array;
            Assert.True(MemoryMarshal.TryGetArray(memory, out ArraySegment<int> segment));
            Assert.Equal(array.Length, segment.Count);

            for (int i = segment.Offset; i < segment.Count + segment.Offset; i++)
            {
                Assert.Equal(array[i], segment.Array[i]);
            }
        }

        [Fact]
        public static void TryGetArrayFromDefaultMemory()
        {
            ReadOnlyMemory<int> memory = default;
            Assert.True(MemoryMarshal.TryGetArray(memory, out ArraySegment<int> segment));
            Assert.Equal(0, segment.Array.Length);
        }

        [Fact]
        public static void OwnedMemoryTryGetArray()
        {
            int[] array = new int[10];
            OwnedMemory<int> owner = new CustomMemoryForTest<int>(array);
            ReadOnlyMemory<int> memory = owner.Memory;
            Assert.True(MemoryMarshal.TryGetArray(memory, out ArraySegment<int> segment));
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
            ReadOnlyMemory<int> memory = array;

            Assert.True(MemoryMarshal.TryGetArray(memory, out ArraySegment<int> segment));
            Assert.Same(array, segment.Array);
            Assert.Equal(0, segment.Array.Length);

            Assert.True(MemoryMarshal.TryGetArray(ReadOnlyMemory<byte>.Empty, out ArraySegment<byte> byteSegment));
            Assert.Equal(0, byteSegment.Array.Length);
        }

        [Fact]
        public static void TryGetArrayFromEmptyOwnedMemory()
        {
            int[] array = new int[0];
            OwnedMemory<int> owner = new CustomMemoryForTest<int>(array);

            Assert.True(MemoryMarshal.TryGetArray(owner.Memory, out ArraySegment<int> segment));
            Assert.Same(array, segment.Array);
            Assert.Equal(0, segment.Array.Length);
        }

        [Fact]
        public static void TryGetArrayFromEmptyNonRetrievableOwnedMemory()
        {
            using (var owner = new NativeOwnedMemory(0))
            {
                Assert.True(MemoryMarshal.TryGetArray(owner.Memory, out ArraySegment<byte> segment));
                Assert.Equal(0, segment.Array.Length);
            }
        }
    }
}
