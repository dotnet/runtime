// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using Xunit;

namespace System.MemoryTests
{
    public static partial class ReadOnlyMemoryTests
    {
        [Fact]
        public static void MemoryDangerousTryGetArray()
        {
            int[] array = new int[10];
            ReadOnlyMemory<int> memory = array;
            Assert.True(memory.DangerousTryGetArray(out ArraySegment<int> segment));
            Assert.Equal(array.Length, segment.Count);

            for (int i = segment.Offset; i < segment.Count + segment.Offset; i++)
            {
                Assert.Equal(array[i], segment.Array[i]);
            }
        }

        [Fact]
        public static void OwnedMemoryTryGetArray()
        {
            int[] array = new int[10];
            OwnedMemory<int> owner = new CustomMemoryForTest<int>(array);
            ReadOnlyMemory<int> memory = owner.Memory;
            Assert.True(memory.DangerousTryGetArray(out ArraySegment<int> segment));
            Assert.Equal(array.Length, segment.Count);

            for (int i = segment.Offset; i < segment.Count + segment.Offset; i++)
            {
                Assert.Equal(array[i], segment.Array[i]);
            }
        }
    }
}
