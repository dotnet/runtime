// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        [Fact]
        public static void ToStringInt()
        {
            int[] a = { 91, 92, 93 };
            var span = new Span<int>(a);
            Assert.Equal("System.Span<Int32>[3]", span.ToString());
        }

        [Fact]
        public static void ToStringInt_Empty()
        {
            var span = new Span<int>();
            Assert.Equal("System.Span<Int32>[0]", span.ToString());
        }

        [Fact]
        public static unsafe void ToStringChar()
        {
            char[] a = { 'a', 'b', 'c' };
            var span = new Span<char>(a);
            Assert.Equal("abc", span.ToString());

            string testString = "abcdefg";
            ReadOnlySpan<char> readOnlySpan = testString.AsSpan();

            fixed (void* ptr = &MemoryMarshal.GetReference(readOnlySpan))
            {
                var temp = new Span<char>(ptr, readOnlySpan.Length);
                Assert.Equal(testString, temp.ToString());
            }
        }

        [Fact]
        public static void ToStringChar_Empty()
        {
            var span = new Span<char>();
            Assert.Equal("", span.ToString());
        }

        [Fact]
        public static void ToStringForSpanOfString()
        {
            string[] a = { "a", "b", "c" };
            var span = new Span<string>(a);
            Assert.Equal("System.Span<String>[3]", span.ToString());
        }

        [Fact]
        public static void ToStringSpanOverFullStringDoesNotReturnOriginal()
        {
            string original = TestHelpers.BuildString(10, 42);

            ReadOnlyMemory<char> readOnlyMemory = original.AsMemory();
            Memory<char> memory = MemoryMarshal.AsMemory(readOnlyMemory);

            Span<char> span = memory.Span;

            string returnedString = span.ToString();
            string returnedStringUsingSlice = span.Slice(0, original.Length).ToString();

            string subString1 = span.Slice(1).ToString();
            string subString2 = span.Slice(0, 2).ToString();
            string subString3 = span.Slice(1, 2).ToString();

            Assert.Equal(original, returnedString);
            Assert.Equal(original, returnedStringUsingSlice);

            Assert.Equal(original.Substring(1), subString1);
            Assert.Equal(original.Substring(0, 2), subString2);
            Assert.Equal(original.Substring(1, 2), subString3);

            Assert.NotSame(original, returnedString);
            Assert.NotSame(original, returnedStringUsingSlice);

            Assert.NotSame(original, subString1);
            Assert.NotSame(original, subString2);
            Assert.NotSame(original, subString3);

            Assert.NotSame(subString1, subString2);
            Assert.NotSame(subString1, subString3);
            Assert.NotSame(subString2, subString3);
        }
    }
}
