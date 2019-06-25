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
        public static void ToStringInt()
        {
            int[] a = { 91, 92, 93 };
            var memory = new ReadOnlyMemory<int>(a);
            Assert.Equal("System.ReadOnlyMemory<Int32>[3]", memory.ToString());
            Assert.Equal("System.ReadOnlyMemory<Int32>[1]", memory.Slice(1, 1).ToString());
            Assert.Equal("System.ReadOnlySpan<Int32>[3]", memory.Span.ToString());
        }

        [Fact]
        public static void ToStringInt_Empty()
        {
            var memory = new ReadOnlyMemory<int>();
            Assert.Equal("System.ReadOnlyMemory<Int32>[0]", memory.ToString());
            Assert.Equal("System.ReadOnlyMemory<Int32>[0]", ReadOnlyMemory<int>.Empty.ToString());
            Assert.Equal("System.ReadOnlySpan<Int32>[0]", memory.Span.ToString());
        }

        [Fact]
        public static void ToStringChar()
        {
            char[] a = { 'a', 'b', 'c' };
            var memory = new ReadOnlyMemory<char>(a);
            Assert.Equal("abc", memory.ToString());
            Assert.Equal("b", memory.Slice(1, 1).ToString());
            Assert.Equal("abc", memory.Span.ToString());
        }

        [Fact]
        public static void ToStringChar_Empty()
        {
            var memory = new ReadOnlyMemory<char>();
            Assert.Equal("", memory.ToString());
            Assert.Equal("", ReadOnlyMemory<char>.Empty.ToString());
            Assert.Equal("", memory.Span.ToString());
        }

        [Fact]
        public static void ToStringMemoryFromReadOnlyMemory()
        {
            string testString = "abcdefg";
            ReadOnlyMemory<char> memory = testString.AsMemory();
            Assert.Equal(testString, memory.ToString());
            Assert.Equal(testString.Substring(1, 1), memory.Slice(1, 1).ToString());
            Assert.Equal(testString, memory.Span.ToString());
        }

        [Fact]
        public static void ToStringForMemoryOfString()
        {
            string[] a = { "a", "b", "c" };
            var memory = new ReadOnlyMemory<string>(a);
            Assert.Equal("System.ReadOnlyMemory<String>[3]", memory.ToString());
            Assert.Equal("System.ReadOnlyMemory<String>[1]", memory.Slice(1, 1).ToString());
            Assert.Equal("System.ReadOnlySpan<String>[3]", memory.Span.ToString());
        }

        [Fact]
        public static void ToStringFromMemoryFromMemoryManager()
        {
            int[] a = { 91, 92, -93, 94 };
            MemoryManager<int> intManager = new CustomMemoryForTest<int>(a);
            Assert.Equal("System.ReadOnlyMemory<Int32>[4]", ((ReadOnlyMemory<int>)intManager.Memory).ToString());

            intManager = new CustomMemoryForTest<int>(Array.Empty<int>());
            Assert.Equal("System.ReadOnlyMemory<Int32>[0]", ((ReadOnlyMemory<int>)intManager.Memory).ToString());

            char[] charArray = { '1', '2', '-', '4' };
            MemoryManager<char> charManager = new CustomMemoryForTest<char>(charArray);
            Assert.Equal("12-4", ((ReadOnlyMemory<char>)charManager.Memory).ToString());

            charManager = new CustomMemoryForTest<char>(Array.Empty<char>());
            Assert.Equal("", ((ReadOnlyMemory<char>)charManager.Memory).ToString());

            string[] strArray = { "91", "92", "-93", "94" };
            MemoryManager<string> strManager = new CustomMemoryForTest<string>(strArray);
            Assert.Equal("System.ReadOnlyMemory<String>[4]", ((ReadOnlyMemory<string>)strManager.Memory).ToString());

            strManager = new CustomMemoryForTest<string>(Array.Empty<string>());
            Assert.Equal("System.ReadOnlyMemory<String>[0]", ((ReadOnlyMemory<string>)strManager.Memory).ToString());
        }

        [Fact]
        public static void ToStringMemoryOverFullStringReturnsOriginal()
        {
            string original = TestHelpers.BuildString(10, 42);

            ReadOnlyMemory<char> memory = original.AsMemory();

            string returnedString = memory.ToString();
            string returnedStringUsingSlice = memory.Slice(0, original.Length).ToString();

            string subString1 = memory.Slice(1).ToString();
            string subString2 = memory.Slice(0, 2).ToString();
            string subString3 = memory.Slice(1, 2).ToString();

            Assert.Equal(original, returnedString);
            Assert.Equal(original, returnedStringUsingSlice);

            Assert.Equal(original.Substring(1), subString1);
            Assert.Equal(original.Substring(0, 2), subString2);
            Assert.Equal(original.Substring(1, 2), subString3);

            Assert.Same(original, returnedString);
            Assert.Same(original, returnedStringUsingSlice);

            Assert.NotSame(original, subString1);
            Assert.NotSame(original, subString2);
            Assert.NotSame(original, subString3);

            Assert.NotSame(subString1, subString2);
            Assert.NotSame(subString1, subString3);
            Assert.NotSame(subString2, subString3);
        }
    }
}
