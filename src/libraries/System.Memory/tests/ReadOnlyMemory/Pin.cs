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
        public static void MemoryPin()
        {
            int[] array = { 1, 2, 3, 4, 5 };
            ReadOnlyMemory<int> memory = array;
            MemoryHandle handle = memory.Pin();
            Assert.True(handle.HasPointer);
            unsafe
            {
                int* pointer = (int*)handle.Pointer;

                GC.Collect();

                for (int i = 0; i < memory.Length; i++)
                {
                    Assert.Equal(array[i], pointer[i]);
                }
            }
            handle.Dispose();
        }

        [Fact]
        public static void MemoryFromEmptyArrayPin()
        {
            ReadOnlyMemory<int> memory = new int[0];
            MemoryHandle handle = memory.Pin();
            Assert.True(handle.HasPointer);
            handle.Dispose();
        }

        [Fact]
        public static void DefaultMemoryPin()
        {
            ReadOnlyMemory<int> memory = default;
            MemoryHandle handle = memory.Pin();
            Assert.False(handle.HasPointer);
            unsafe
            {
                Assert.True(handle.Pointer == null);
            }
            handle.Dispose();
        }

        [Fact]
        public static void MemoryPinAndSlice()
        {
            int[] array = { 1, 2, 3, 4, 5 };
            ReadOnlyMemory<int> memory = array;
            memory = memory.Slice(1);
            MemoryHandle handle = memory.Pin();
            ReadOnlySpan<int> span = memory.Span;
            Assert.True(handle.HasPointer);
            unsafe
            {
                int* pointer = (int*)handle.Pointer;

                GC.Collect();

                for (int i = 0; i < memory.Length; i++)
                {
                    Assert.Equal(array[i + 1], pointer[i]);
                }

                for (int i = 0; i < memory.Length; i++)
                {
                    Assert.Equal(array[i + 1], span[i]);
                }
            }
            handle.Dispose();
        }

        [Fact]
        public static void OwnedMemoryPin()
        {
            int[] array = { 1, 2, 3, 4, 5 };
            OwnedMemory<int> owner = new CustomMemoryForTest<int>(array);
            ReadOnlyMemory<int> memory = owner.Memory;
            MemoryHandle handle = memory.Pin();
            Assert.True(handle.HasPointer);
            unsafe
            {
                int* pointer = (int*)handle.Pointer;

                GC.Collect();

                for (int i = 0; i < memory.Length; i++)
                {
                    Assert.Equal(array[i], pointer[i]);
                }
            }
            handle.Dispose();
        }

        [Fact]
        public static void OwnedMemoryPinAndSlice()
        {
            int[] array = { 1, 2, 3, 4, 5 };
            OwnedMemory<int> owner = new CustomMemoryForTest<int>(array);
            ReadOnlyMemory<int> memory = owner.Memory.Slice(1);
            MemoryHandle handle = memory.Pin();
            ReadOnlySpan<int> span = memory.Span;
            Assert.True(handle.HasPointer);
            unsafe
            {
                int* pointer = (int*)handle.Pointer;

                GC.Collect();

                for (int i = 0; i < memory.Length; i++)
                {
                    Assert.Equal(array[i + 1], pointer[i]);
                }

                for (int i = 0; i < memory.Length; i++)
                {
                    Assert.Equal(array[i + 1], span[i]);
                }
            }
            handle.Dispose();
        }
    }
}
