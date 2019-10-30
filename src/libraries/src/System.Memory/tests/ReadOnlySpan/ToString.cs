// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void ToStringInt()
        {
            int[] a = { 91, 92, 93 };
            var span = new ReadOnlySpan<int>(a);
            Assert.Equal("System.ReadOnlySpan<Int32>[3]", span.ToString());
        }

        [Fact]
        public static void ToStringInt_Empty()
        {
            var span = new ReadOnlySpan<int>();
            Assert.Equal("System.ReadOnlySpan<Int32>[0]", span.ToString());
        }

        [Fact]
        public static void ToStringChar()
        {
            char[] a = { 'a', 'b', 'c' };
            var span = new ReadOnlySpan<char>(a);
            Assert.Equal("abc", span.ToString());

            string testString = "abcdefg";
            ReadOnlySpan<char> readOnlySpan = testString.AsSpan();
            Assert.Equal(testString, readOnlySpan.ToString());
        }

        [Fact]
        public static void ToStringChar_Empty()
        {
            var span = new ReadOnlySpan<char>();
            Assert.Equal("", span.ToString());
        }

        [Fact]
        public static void ToStringForSpanOfString()
        {
            string[] a = { "a", "b", "c" };
            var span = new ReadOnlySpan<string>(a);
            Assert.Equal("System.ReadOnlySpan<String>[3]", span.ToString());
        }

        [Fact]
        public static void ToStringFromString()
        {
            string orig = "hello world";
            Assert.Equal(orig, orig.AsSpan().ToString());
            Assert.Equal(orig.Substring(0, 5), orig.AsSpan(0, 5).ToString());
            Assert.Equal(orig.Substring(5), orig.AsSpan(5).ToString());
            Assert.Equal(orig.Substring(1, 3), orig.AsSpan(1, 3).ToString());
        }

        [Fact]
        public static void ToStringSpanOverSubstringDoesNotReturnOriginal()
        {
            string original = TestHelpers.BuildString(10, 42);
            ReadOnlySpan<char> span = original.AsSpan();

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

            Assert.NotSame(original, subString1);
            Assert.NotSame(original, subString2);
            Assert.NotSame(original, subString3);

            Assert.NotSame(subString1, subString2);
            Assert.NotSame(subString1, subString3);
            Assert.NotSame(subString2, subString3);
        }
    }
}
