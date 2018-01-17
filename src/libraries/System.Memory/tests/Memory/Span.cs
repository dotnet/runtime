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
        public static void SpanFromCtorArrayInt()
        {
            int[] a = { 91, 92, -93, 94 };
            Memory<int> memory;

            memory = new Memory<int>(a);
            memory.Span.Validate(91, 92, -93, 94);

            memory = new Memory<int>(a, 0, a.Length);
            memory.Span.Validate(91, 92, -93, 94);

            OwnedMemory<int> owner = new CustomMemoryForTest<int>(a);
            owner.Memory.Span.Validate(91, 92, -93, 94);
        }

        [Fact]
        public static void SpanFromCtorArrayLong()
        {
            long[] a = { 91, -92, 93, 94, -95 };
            Memory<long> memory;

            memory = new Memory<long>(a);
            memory.Span.Validate(91, -92, 93, 94, -95);

            memory = new Memory<long>(a, 0, a.Length);
            memory.Span.Validate(91, -92, 93, 94, -95);

            OwnedMemory<long> owner = new CustomMemoryForTest<long>(a);
            owner.Memory.Span.Validate(91, -92, 93, 94, -95);
        }

        [Fact]
        public static void SpanFromCtorArrayObject()
        {
            object o1 = new object();
            object o2 = new object();
            object[] a = { o1, o2 };
            Memory<object> memory;

            memory = new Memory<object>(a);
            memory.Span.ValidateReferenceType(o1, o2);

            memory = new Memory<object>(a, 0, a.Length);
            memory.Span.ValidateReferenceType(o1, o2);

            OwnedMemory<object> owner = new CustomMemoryForTest<object>(a);
            owner.Memory.Span.ValidateReferenceType(o1, o2);
        }

        [Fact]
        public static void SpanFromCtorArrayZeroLength()
        {
            int[] empty = Array.Empty<int>();
            Memory<int> memory;

            memory = new Memory<int>(empty);
            memory.Span.ValidateNonNullEmpty();

            memory = new Memory<int>(empty, 0, empty.Length);
            memory.Span.ValidateNonNullEmpty();

            OwnedMemory<int> owner = new CustomMemoryForTest<int>(empty);
            owner.Memory.Span.Validate();
        }

        [Fact]
        public static void SpanFromCtorArrayWrongValueType()
        {
            // Can pass variant array, if array type is a valuetype.

            uint[] a = { 42u, 0xffffffffu };
            int[] aAsIntArray = (int[])(object)a;
            Memory<int> memory;

            memory = new Memory<int>(aAsIntArray);
            memory.Span.Validate(42, -1);

            memory = new Memory<int>(aAsIntArray, 0, aAsIntArray.Length);
            memory.Span.Validate(42, -1);

            OwnedMemory<int> owner = new CustomMemoryForTest<int>(aAsIntArray);
            owner.Memory.Span.Validate(42, -1);
        }

        [Fact]
        public static void SpanFromDefaultMemory()
        {
            Memory<int> memory = default;
            Span<int> span = memory.Span;
            Assert.True(span.SequenceEqual(default));

            Memory<string> memoryObject = default;
            Span<string> spanObject = memoryObject.Span;
            Assert.True(spanObject.SequenceEqual(default));
        }

    }
}
