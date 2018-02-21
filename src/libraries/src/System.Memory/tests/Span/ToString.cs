// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public static unsafe void ToStringForSpanOfString()
        {
            string[] a = { "a", "b", "c" };
            var span = new Span<string>(a);
            Assert.Equal("System.Span<String>[3]", span.ToString());
        }
    }
}
