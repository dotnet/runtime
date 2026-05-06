// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        public static void MemoryManagerTryGetArray()
        {
            int[] array = new int[10];
            MemoryManager<int> manager = new CustomMemoryForTest<int>(array);
            ReadOnlyMemory<int> memory = manager.Memory;
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
        public static void TryGetArrayFromEmptyMemoryManager()
        {
            int[] array = new int[0];
            MemoryManager<int> manager = new CustomMemoryForTest<int>(array);

            Assert.True(MemoryMarshal.TryGetArray(manager.Memory, out ArraySegment<int> segment));
            Assert.Same(array, segment.Array);
            Assert.Equal(0, segment.Array.Length);
        }

        [Fact]
        public static void TryGetArrayFromEmptyNonRetrievableMemoryManager()
        {
            using (var manager = new NativeMemoryManager(0))
            {
                Assert.True(MemoryMarshal.TryGetArray(manager.Memory, out ArraySegment<byte> segment));
                Assert.Equal(0, segment.Array.Length);
            }
        }
    }
}
