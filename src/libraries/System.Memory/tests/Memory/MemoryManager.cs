// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using Xunit;

namespace System.MemoryTests
{
    //
    // Tests for Memory<T>.ctor(MemoryManager<T>, int , int)
    //
    public static partial class MemoryTests
    {
        [Fact]
        public static void MemoryFromMemoryManagerInt()
        {
            int[] a = { 91, 92, -93, 94 };
            MemoryManager<int> manager = new CustomMemoryForTest<int>(a);
            Memory<int> memory = manager.Memory;
            memory.Validate(91, 92, -93, 94);
            memory.Slice(0, 4).Validate(91, 92, -93, 94);
            memory.Slice(1, 0).Validate();
            memory.Slice(1, 1).Validate(92);
            memory.Slice(1, 2).Validate(92, -93);
            memory.Slice(2, 2).Validate(-93, 94);
            memory.Slice(4, 0).Validate();
        }

        [Fact]
        public static void MemoryManagerMemoryCtorInvalid()
        {
            int[] a = { 91, 92, -93, 94 };
            CustomMemoryForTest<int> manager = new CustomMemoryForTest<int>(a);

            Assert.Throws<ArgumentOutOfRangeException>(() => manager.Memory.Slice(a.Length + 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => manager.Memory.Slice(0, a.Length + 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => manager.Memory.Slice(a.Length + 1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => manager.Memory.Slice(1, a.Length));

            Assert.Throws<ArgumentOutOfRangeException>(() => manager.CreateMemoryForTest(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => manager.CreateMemoryForTest(0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => manager.CreateMemoryForTest(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => manager.CreateMemoryForTest(-1, -1));
        }

        [Fact]
        public static void ReadOnlyMemoryFromMemoryFromMemoryManagerInt()
        {
            int[] a = { 91, 92, -93, 94 };
            MemoryManager<int> manager = new CustomMemoryForTest<int>(a);
            ReadOnlyMemory<int> readOnlyMemory = manager.Memory;
            readOnlyMemory.Validate(91, 92, -93, 94);
            readOnlyMemory.Slice(0, 4).Validate(91, 92, -93, 94);
            readOnlyMemory.Slice(1, 0).Validate();
            readOnlyMemory.Slice(1, 1).Validate(92);
            readOnlyMemory.Slice(1, 2).Validate(92, -93);
            readOnlyMemory.Slice(2, 2).Validate(-93, 94);
            readOnlyMemory.Slice(4, 0).Validate();
        }

        [Fact]
        public static void MemoryFromMemoryManagerLong()
        {
            long[] a = { 91, -92, 93, 94, -95 };
            MemoryManager<long> manager = new CustomMemoryForTest<long>(a);
            Memory<long> memory = manager.Memory;
            memory.Validate(91, -92, 93, 94, -95);
            memory.Slice(0, 5).Validate(91, -92, 93, 94, -95);
            memory.Slice(1, 0).Validate();
            memory.Slice(1, 1).Validate(-92);
            memory.Slice(1, 2).Validate(-92, 93);
            memory.Slice(2, 3).Validate(93, 94, -95);
            memory.Slice(5, 0).Validate();
        }

        [Fact]
        public static void MemoryFromMemoryManagerObject()
        {
            object o1 = new object();
            object o2 = new object();
            object[] a = { o1, o2 };
            MemoryManager<object> manager = new CustomMemoryForTest<object>(a);
            Memory<object> memory = manager.Memory;
            memory.ValidateReferenceType(o1, o2);
        }

        [Fact]
        public static void ImplicitReadOnlyMemoryFromMemoryManager()
        {
            long[] a = { 91, -92, 93, 94, -95 };
            MemoryManager<long> manager = new CustomMemoryForTest<long>(a);
            Memory<long> memory = manager.Memory;
            CastReadOnly<long>(memory, 91, -92, 93, 94, -95);
        }

        [Fact]
        public static void MemoryManagerDispose()
        {
            int[] a = { 91, 92, -93, 94 };
            CustomMemoryForTest<int> manager;
            using (manager = new CustomMemoryForTest<int>(a))
            {
                Assert.False(manager.IsDisposed);
            }
            Assert.True(manager.IsDisposed);
        }

        [Fact]
        public static void MemoryManagerPinEmptyArray()
        {
            int[] a = { };
            MemoryManager<int> manager = new CustomMemoryForTest<int>(a);
            MemoryHandle handle = manager.Pin();
            unsafe
            {
                Assert.True(handle.Pointer != null);
            }
        }

        [Fact]
        public static void MemoryManagerPinArray()
        {
            int[] array = { 1, 2, 3, 4, 5 };
            MemoryManager<int> manager = new CustomMemoryForTest<int>(array);
            MemoryHandle handle = manager.Pin();
            unsafe
            {
                int* pointer = (int*)handle.Pointer;
                Assert.True(pointer != null);

                GC.Collect();

                for (int i = 0; i < manager.Memory.Length; i++)
                {
                    Assert.Equal(array[i], pointer[i]);
                }
            }
            handle.Dispose();
        }

        [Fact]
        [OuterLoop]
        public static void MemoryManagerPinLargeArray()
        {
            // Early-out: we can only run this test on 64-bit platforms.
            if (IntPtr.Size == 4)
            {
                return;
            }

            int[] array = new int[0x2000_0000]; // will produce array with total byte length > 2 GB
            MemoryManager<int> manager = new CustomMemoryForTest<int>(array);
            Assert.Throws<ArgumentOutOfRangeException>(() => manager.Pin(int.MinValue));
        }

        [Fact]
        public static void SpanFromMemoryManagerAfterDispose()
        {
            int[] a = { 91, 92, -93, 94 };
            MemoryManager<int> manager;
            using (manager = new CustomMemoryForTest<int>(a))
            {

            }
            Assert.Throws<ObjectDisposedException>(() => manager.GetSpan());
        }
    }
}

