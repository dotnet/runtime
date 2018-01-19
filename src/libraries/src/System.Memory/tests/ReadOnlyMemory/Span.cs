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
        public static void SpanFromCtorArrayInt()
        {
            int[] a = { 91, 92, -93, 94 };
            ReadOnlyMemory<int> memory;

            memory = new ReadOnlyMemory<int>(a);
            memory.Span.Validate(91, 92, -93, 94);

            memory = new ReadOnlyMemory<int>(a, 0, a.Length);
            memory.Span.Validate(91, 92, -93, 94);

            OwnedMemory<int> owner = new CustomMemoryForTest<int>(a);
            ((ReadOnlyMemory<int>)owner.Memory).Span.Validate(91, 92, -93, 94);
        }

        [Fact]
        public static void SpanFromCtorArrayLong()
        {
            long[] a = { 91, -92, 93, 94, -95 };
            ReadOnlyMemory<long> memory;

            memory = new ReadOnlyMemory<long>(a);
            memory.Span.Validate(91, -92, 93, 94, -95);

            memory = new ReadOnlyMemory<long>(a, 0, a.Length);
            memory.Span.Validate(91, -92, 93, 94, -95);

            OwnedMemory<long> owner = new CustomMemoryForTest<long>(a);
            ((ReadOnlyMemory<long>)owner.Memory).Span.Validate(91, -92, 93, 94, -95);
        }

        [Fact]
        public static void SpanFromCtorArrayObject()
        {
            object o1 = new object();
            object o2 = new object();
            object[] a = { o1, o2 };
            ReadOnlyMemory<object> memory;

            memory = new ReadOnlyMemory<object>(a);
            memory.Span.ValidateReferenceType(o1, o2);

            memory = new ReadOnlyMemory<object>(a, 0, a.Length);
            memory.Span.ValidateReferenceType(o1, o2);

            OwnedMemory<object> owner = new CustomMemoryForTest<object>(a);
            ((ReadOnlyMemory<object>)owner.Memory).Span.ValidateReferenceType(o1, o2);
        }

        [Fact]
        public static void SpanFromCtorArrayZeroLength()
        {
            int[] empty = Array.Empty<int>();
            ReadOnlyMemory<int> memory;

            memory = new ReadOnlyMemory<int>(empty);
            memory.Span.ValidateNonNullEmpty();

            memory = new ReadOnlyMemory<int>(empty, 0, empty.Length);
            memory.Span.ValidateNonNullEmpty();

            OwnedMemory<int> owner = new CustomMemoryForTest<int>(empty);
            ((ReadOnlyMemory<int>)owner.Memory).Span.Validate();
        }

        [Fact]
        public static void SpanFromCtorArrayWrongValueType()
        {
            // Can pass variant array, if array type is a valuetype.

            uint[] a = { 42u, 0xffffffffu };
            int[] aAsIntArray = (int[])(object)a;
            ReadOnlyMemory<int> memory;

            memory = new ReadOnlyMemory<int>(aAsIntArray);
            memory.Span.Validate(42, -1);

            memory = new ReadOnlyMemory<int>(aAsIntArray, 0, aAsIntArray.Length);
            memory.Span.Validate(42, -1);

            OwnedMemory<int> owner = new CustomMemoryForTest<int>(aAsIntArray);
            ((ReadOnlyMemory<int>)owner.Memory).Span.Validate(42, -1);
        }

        [Fact]
        public static void SpanFromDefaultMemory()
        {
            ReadOnlyMemory<int> memory = default;
            ReadOnlySpan<int> span = memory.Span;
            Assert.True(span.SequenceEqual(default));

            ReadOnlyMemory<string> memoryObject = default;
            ReadOnlySpan<string> spanObject = memoryObject.Span;
            Assert.True(spanObject.SequenceEqual(default));
        }

    }
}
