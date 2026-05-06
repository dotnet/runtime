// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.InteropServices;
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

            MemoryManager<int> manager = new CustomMemoryForTest<int>(a);
            manager.Memory.Span.Validate(91, 92, -93, 94);
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

            MemoryManager<long> manager = new CustomMemoryForTest<long>(a);
            manager.Memory.Span.Validate(91, -92, 93, 94, -95);
        }

        [Fact]
        public static void SpanFromCtorArrayChar()
        {
            char[] a = { '1', '2', '3', '4', '-' };
            Memory<char> memory;

            memory = new Memory<char>(a);
            memory.Span.Validate('1', '2', '3', '4', '-');

            memory = new Memory<char>(a, 0, a.Length);
            memory.Span.Validate('1', '2', '3', '4', '-');

            MemoryManager<char> manager = new CustomMemoryForTest<char>(a);
            manager.Memory.Span.Validate('1', '2', '3', '4', '-');
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

            MemoryManager<object> manager = new CustomMemoryForTest<object>(a);
            manager.Memory.Span.ValidateReferenceType(o1, o2);
        }

        [Fact]
        public static void SpanFromStringAsMemory()
        {
            string a = "1234-";
            ReadOnlyMemory<char> memory;

            memory = a.AsMemory();
            MemoryMarshal.AsMemory(memory).Span.Validate('1', '2', '3', '4', '-');

            memory = a.AsMemory(0, a.Length);
            MemoryMarshal.AsMemory(memory).Span.Validate('1', '2', '3', '4', '-');
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

            MemoryManager<int> manager = new CustomMemoryForTest<int>(empty);
            manager.Memory.Span.Validate();
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

            MemoryManager<int> manager = new CustomMemoryForTest<int>(aAsIntArray);
            manager.Memory.Span.Validate(42, -1);
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

        [Fact]
        public static void TornMemory_Array_SpanThrowsIfOutOfBounds()
        {
            Memory<int> memory;

            memory = TestHelpers.DangerousCreateMemory<int>(new int[4], 0, 5);
            Assert.Throws<ArgumentOutOfRangeException>(() => memory.Span.DontBox());

            memory = TestHelpers.DangerousCreateMemory<int>(new int[4], 3, 2);
            Assert.Throws<ArgumentOutOfRangeException>(() => memory.Span.DontBox());
        }

        [Fact]
        public static void TornMemory_String_SpanThrowsIfOutOfBounds()
        {
            Memory<char> memory;

            memory = TestHelpers.DangerousCreateMemory<char>("1234", 0, 5);
            Assert.Throws<ArgumentOutOfRangeException>(() => memory.Span.DontBox());

            memory = TestHelpers.DangerousCreateMemory<char>("1234", 3, 2);
            Assert.Throws<ArgumentOutOfRangeException>(() => memory.Span.DontBox());
        }
    }
}
