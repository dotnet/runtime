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
        public static void MemoryRetainWithoutPinning()
        {
            int[] array = { 1, 2, 3, 4, 5 };
            ReadOnlyMemory<int> memory = array;
            MemoryHandle handle = memory.Retain();
            unsafe
            {
                int* pointer = (int*)handle.PinnedPointer;
                Assert.True(pointer == null);
            }
            handle.Dispose();
        }

        [Fact]
        public static void MemoryRetainWithPinning()
        {
            int[] array = { 1, 2, 3, 4, 5 };
            ReadOnlyMemory<int> memory = array;
            MemoryHandle handle = memory.Retain(pin: true);
            unsafe
            {
                int* pointer = (int*)handle.PinnedPointer;
                Assert.True(pointer != null);

                GC.Collect();

                for (int i = 0; i < memory.Length; i++)
                {
                    Assert.Equal(array[i], pointer[i]);
                }
            }
            handle.Dispose();
        }

        [Fact]
        public static void OwnedMemoryRetainWithoutPinning()
        {
            int[] array = { 1, 2, 3, 4, 5 };
            OwnedMemory<int> owner = new CustomMemoryForTest<int>(array);
            ReadOnlyMemory<int> memory = owner.Memory;
            MemoryHandle handle = memory.Retain();
            unsafe
            {
                int* pointer = (int*)handle.PinnedPointer;
                Assert.True(pointer == null);
            }
            handle.Dispose();
        }

        [Fact]
        public static void OwnedMemoryRetainWithPinning()
        {
            int[] array = { 1, 2, 3, 4, 5 };
            OwnedMemory<int> owner = new CustomMemoryForTest<int>(array);
            ReadOnlyMemory<int> memory = owner.Memory;
            MemoryHandle handle = memory.Retain(pin: true);
            unsafe
            {
                int* pointer = (int*)handle.PinnedPointer;
                Assert.True(pointer != null);

                GC.Collect();

                for (int i = 0; i < memory.Length; i++)
                {
                    Assert.Equal(array[i], pointer[i]);
                }
            }
            handle.Dispose();
        }
    }
}
