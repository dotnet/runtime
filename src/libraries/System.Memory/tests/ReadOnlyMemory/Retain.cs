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
        public static void MemoryRetainWithPinningAndSlice()
        {
            int[] array = { 1, 2, 3, 4, 5 };
            ReadOnlyMemory<int> memory = array;
            memory = memory.Slice(1);
            MemoryHandle handle = memory.Retain(pin: true);
            ReadOnlySpan<int> span = memory.Span;
            unsafe
            {
                int* pointer = (int*)handle.PinnedPointer;
                Assert.True(pointer != null);

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

        [ActiveIssue(24384, TargetFrameworkMonikers.UapAot)]
        [Fact]
        public static void OwnedMemoryRetainWithPinningAndSlice()
        {
            int[] array = { 1, 2, 3, 4, 5 };
            OwnedMemory<int> owner = new CustomMemoryForTest<int>(array);
            ReadOnlyMemory<int> memory = owner.Memory.Slice(1);
            MemoryHandle handle = memory.Retain(pin: true);
            ReadOnlySpan<int> span = memory.Span;
            unsafe
            {
                int* pointer = (int*)handle.PinnedPointer;
                Assert.True(pointer != null);

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
