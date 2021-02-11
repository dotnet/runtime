// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

            MemoryManager<int> manager = new CustomMemoryForTest<int>(a);
            ((ReadOnlyMemory<int>)manager.Memory).Span.Validate(91, 92, -93, 94);
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

            MemoryManager<long> manager = new CustomMemoryForTest<long>(a);
            ((ReadOnlyMemory<long>)manager.Memory).Span.Validate(91, -92, 93, 94, -95);
        }

        [Fact]
        public static void SpanFromCtorArrayChar()
        {
            char[] a = { '1', '2', '3', '4', '-' };
            ReadOnlyMemory<char> memory;

            memory = new ReadOnlyMemory<char>(a);
            memory.Span.Validate('1', '2', '3', '4', '-');

            memory = new ReadOnlyMemory<char>(a, 0, a.Length);
            memory.Span.Validate('1', '2', '3', '4', '-');

            MemoryManager<char> manager = new CustomMemoryForTest<char>(a);
            ((ReadOnlyMemory<char>)manager.Memory).Span.Validate('1', '2', '3', '4', '-');
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

            MemoryManager<object> manager = new CustomMemoryForTest<object>(a);
            ((ReadOnlyMemory<object>)manager.Memory).Span.ValidateReferenceType(o1, o2);
        }

        [Fact]
        public static void SpanFromStringAsMemory()
        {
            string a = "1234-";
            ReadOnlyMemory<char> memory;

            memory = a.AsMemory();
            memory.Span.Validate('1', '2', '3', '4', '-');

            memory = a.AsMemory(0, a.Length);
            memory.Span.Validate('1', '2', '3', '4', '-');
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

            MemoryManager<int> manager = new CustomMemoryForTest<int>(empty);
            ((ReadOnlyMemory<int>)manager.Memory).Span.Validate();
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

            MemoryManager<int> manager = new CustomMemoryForTest<int>(aAsIntArray);
            ((ReadOnlyMemory<int>)manager.Memory).Span.Validate(42, -1);
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

        [Fact]
        public static void TornMemory_Array_SpanThrowsIfOutOfBounds()
        {
            ReadOnlyMemory<int> memory;

            memory = TestHelpers.DangerousCreateReadOnlyMemory<int>(new int[4], 0, 5);
            Assert.Throws<ArgumentOutOfRangeException>(() => memory.Span.DontBox());

            memory = TestHelpers.DangerousCreateReadOnlyMemory<int>(new int[4], 3, 2);
            Assert.Throws<ArgumentOutOfRangeException>(() => memory.Span.DontBox());
        }

        [Fact]
        public static void TornMemory_String_SpanThrowsIfOutOfBounds()
        {
            ReadOnlyMemory<char> memory;

            memory = TestHelpers.DangerousCreateReadOnlyMemory<char>("1234", 0, 5);
            Assert.Throws<ArgumentOutOfRangeException>(() => memory.Span.DontBox());

            memory = TestHelpers.DangerousCreateReadOnlyMemory<char>("1234", 3, 2);
            Assert.Throws<ArgumentOutOfRangeException>(() => memory.Span.DontBox());
        }
    }
}
