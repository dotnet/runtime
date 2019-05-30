// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Buffers;

namespace System.SpanTests
{
    public static partial class MemoryMarshalTests
    {
        [Fact]
        public static void CreateFromPinnedArrayInt()
        {
            int[] a = { 90, 91, 92, 93, 94, 95, 96, 97, 98 };
            Memory<int> pinnedMemory = MemoryMarshal.CreateFromPinnedArray(a, 3, 2);
            pinnedMemory.Validate(93, 94);
        }

        [Fact]
        public static void CreateFromPinnedArrayLong()
        {
            long[] a = { 90, 91, 92, 93, 94, 95, 96, 97, 98 };
            Memory<long> pinnedMemory = MemoryMarshal.CreateFromPinnedArray(a, 4, 3);
            pinnedMemory.Validate(94, 95, 96);
        }

        [Fact]
        public static void CreateFromPinnedArrayString()
        {
            string[] a = { "90", "91", "92", "93", "94", "95", "96", "97", "98" };
            Memory<string> pinnedMemory = MemoryMarshal.CreateFromPinnedArray(a, 4, 3);
            pinnedMemory.Validate("94", "95", "96");
        }
        
        [Fact]
        public static void CreateFromPinnedArrayIntSliceRemainsPinned()
        {
            int[] a = { 90, 91, 92, 93, 94, 95, 96, 97, 98 };
            Memory<int> pinnedMemory = MemoryMarshal.CreateFromPinnedArray(a, 3, 5);
            pinnedMemory.Validate(93, 94, 95, 96, 97);

            Memory<int> slice = pinnedMemory.Slice(0);
            TestMemory<int> testSlice = Unsafe.As<Memory<int>, TestMemory<int>>(ref slice);
            Assert.Equal(3 | (1 << 31), testSlice._index);
            Assert.Equal(5, testSlice._length);

            slice = pinnedMemory.Slice(0, pinnedMemory.Length);
            testSlice = Unsafe.As<Memory<int>, TestMemory<int>>(ref slice);
            Assert.Equal(3 | (1 << 31), testSlice._index);
            Assert.Equal(5, testSlice._length);

            slice = pinnedMemory.Slice(1);
            testSlice = Unsafe.As<Memory<int>, TestMemory<int>>(ref slice);
            Assert.Equal(4 | (1 << 31), testSlice._index);
            Assert.Equal(4, testSlice._length);

            slice = pinnedMemory.Slice(1, 2);
            testSlice = Unsafe.As<Memory<int>, TestMemory<int>>(ref slice);
            Assert.Equal(4 | (1 << 31), testSlice._index);
            Assert.Equal(2, testSlice._length);
        }
        
        [Fact]
        public static void CreateFromPinnedArrayIntReadOnlyMemorySliceRemainsPinned()
        {
            int[] a = { 90, 91, 92, 93, 94, 95, 96, 97, 98 };
            ReadOnlyMemory<int> pinnedMemory = MemoryMarshal.CreateFromPinnedArray(a, 3, 5);
            pinnedMemory.Validate(93, 94, 95, 96, 97);

            ReadOnlyMemory<int> slice = pinnedMemory.Slice(0);
            TestMemory<int> testSlice = Unsafe.As<ReadOnlyMemory<int>, TestMemory<int>>(ref slice);
            Assert.Equal(3 | (1 << 31), testSlice._index);
            Assert.Equal(5, testSlice._length);

            slice = pinnedMemory.Slice(0, pinnedMemory.Length);
            testSlice = Unsafe.As<ReadOnlyMemory<int>, TestMemory<int>>(ref slice);
            Assert.Equal(3 | (1 << 31), testSlice._index);
            Assert.Equal(5, testSlice._length);

            slice = pinnedMemory.Slice(1);
            testSlice = Unsafe.As<ReadOnlyMemory<int>, TestMemory<int>>(ref slice);
            Assert.Equal(4 | (1 << 31), testSlice._index);
            Assert.Equal(4, testSlice._length);

            slice = pinnedMemory.Slice(1, 2);
            testSlice = Unsafe.As<ReadOnlyMemory<int>, TestMemory<int>>(ref slice);
            Assert.Equal(4 | (1 << 31), testSlice._index);
            Assert.Equal(2, testSlice._length);
        }

        [Fact]
        public static void CreateFromPinnedArrayWithStartAndLengthRangeExtendsToEndOfArray()
        {
            long[] a = { 90, 91, 92, 93, 94, 95, 96, 97, 98 };
            Memory<long> pinnedMemory = MemoryMarshal.CreateFromPinnedArray(a, 4, 5);
            pinnedMemory.Validate(94, 95, 96, 97, 98);
        }

        [Fact]
        public static void CreateFromPinnedArrayZeroLength()
        {
            int[] empty = Array.Empty<int>();
            Memory<int> memory = MemoryMarshal.CreateFromPinnedArray(empty, 0, empty.Length);
            memory.Validate();
        }

        [Fact]
        public static void CreateFromPinnedArrayNullArray()
        {
            Memory<int> memory = MemoryMarshal.CreateFromPinnedArray((int[])null, 0, 0);
            memory.Validate();
            Assert.Equal(default, memory);
        }


        [Fact]
        public static void CreateFromPinnedArrayNullArrayNonZeroStartAndLength()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => MemoryMarshal.CreateFromPinnedArray((int[])null, 1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => MemoryMarshal.CreateFromPinnedArray((int[])null, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => MemoryMarshal.CreateFromPinnedArray((int[])null, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => MemoryMarshal.CreateFromPinnedArray((int[])null, -1, -1));
        }

        [Fact]
        public static void CreateFromPinnedArrayWrongArrayType()
        {
            // Cannot pass variant array, if array type is not a valuetype.
            string[] a = { "Hello" };
            Assert.Throws<ArrayTypeMismatchException>(() => MemoryMarshal.CreateFromPinnedArray<object>(a, 0, a.Length));
        }

        [Fact]
        public static void CreateFromPinnedArrayWrongValueType()
        {
            // Can pass variant array, if array type is a valuetype.
            uint[] a = { 42u, 0xffffffffu };
            int[] aAsIntArray = (int[])(object)a;

            Memory<int> memory = MemoryMarshal.CreateFromPinnedArray(aAsIntArray, 0, aAsIntArray.Length);
            memory.Validate(42, -1);
        }

        [Fact]
        public static void CreateFromPinnedArrayWithNegativeStartAndLength()
        {
            int[] a = new int[3];
            Assert.Throws<ArgumentOutOfRangeException>(() => MemoryMarshal.CreateFromPinnedArray(a, -1, 0));
        }

        [Fact]
        public static void CreateFromPinnedArrayWithStartTooLargeAndLength()
        {
            int[] a = new int[3];
            Assert.Throws<ArgumentOutOfRangeException>(() => MemoryMarshal.CreateFromPinnedArray(a, 4, 0));
        }

        [Fact]
        public static void CreateFromPinnedArrayWithStartAndNegativeLength()
        {
            int[] a = new int[3];
            Assert.Throws<ArgumentOutOfRangeException>(() => MemoryMarshal.CreateFromPinnedArray(a, 0, -1));
        }

        [Fact]
        public static void CreateFromPinnedArrayWithStartAndLengthTooLarge()
        {
            int[] a = new int[3];
            Assert.Throws<ArgumentOutOfRangeException>(() => MemoryMarshal.CreateFromPinnedArray(a, 3, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => MemoryMarshal.CreateFromPinnedArray(a, 2, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => MemoryMarshal.CreateFromPinnedArray(a, 1, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => MemoryMarshal.CreateFromPinnedArray(a, 0, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => MemoryMarshal.CreateFromPinnedArray(a, int.MaxValue, int.MaxValue));
        }

        [Fact]
        public static void CreateFromPinnedArrayWithStartAndLengthBothEqual()
        {
            // Valid for start to equal the array length. This returns an empty memory that starts "just past the array."
            int[] a = { 91, 92, 93 };
            Memory<int> pinnedMemory = MemoryMarshal.CreateFromPinnedArray(a, 3, 0);
            pinnedMemory.Validate();
        }

        [Fact]
        public static unsafe void CreateFromPinnedArrayVerifyPinning()
        {
            int[] pinnedArray = { 90, 91, 92, 93, 94, 95, 96, 97, 98 };
            GCHandle pinnedGCHandle = GCHandle.Alloc(pinnedArray, GCHandleType.Pinned);

            Memory<int> pinnedMemory = MemoryMarshal.CreateFromPinnedArray(pinnedArray, 0, 2);
            void* pinnedPtr = Unsafe.AsPointer(ref MemoryMarshal.GetReference(pinnedMemory.Span));
            void* memoryHandlePinnedPtr = pinnedMemory.Pin().Pointer;

            GC.Collect();
            GC.Collect(2);

            Assert.Equal((int)pinnedPtr, (int)Unsafe.AsPointer(ref MemoryMarshal.GetReference(pinnedMemory.Span)));
            Assert.Equal((int)memoryHandlePinnedPtr, (int)pinnedGCHandle.AddrOfPinnedObject().ToPointer());

            pinnedGCHandle.Free();
        }
    }
}
