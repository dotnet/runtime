// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Runtime.InteropServices;
using Xunit;

namespace System.MemoryTests
{
    public static partial class MemoryTests
    {
        [Fact]
        public static void ToStringInt()
        {
            int[] a = { 91, 92, 93 };
            var memory = new Memory<int>(a);
            Assert.Equal("System.Memory<Int32>[3]", memory.ToString());
            Assert.Equal("System.Memory<Int32>[1]", memory.Slice(1, 1).ToString());
            Assert.Equal("System.Span<Int32>[3]", memory.Span.ToString());
        }

        [Fact]
        public static void ToStringInt_Empty()
        {
            var memory = new Memory<int>();
            Assert.Equal("System.Memory<Int32>[0]", memory.ToString());
            Assert.Equal("System.Memory<Int32>[0]", Memory<int>.Empty.ToString());
            Assert.Equal("System.Span<Int32>[0]", memory.Span.ToString());
        }

        [Fact]
        public static void ToStringChar()
        {
            char[] a = { 'a', 'b', 'c' };
            var memory = new Memory<char>(a);
            Assert.Equal("abc", memory.ToString());
            Assert.Equal("b", memory.Slice(1, 1).ToString());
            Assert.Equal("abc", memory.Span.ToString());
        }

        [Fact]
        public static void ToStringChar_Empty()
        {
            var memory = new Memory<char>();
            Assert.Equal("", memory.ToString());
            Assert.Equal("", Memory<char>.Empty.ToString());
            Assert.Equal("", memory.Span.ToString());
        }

        [Fact]
        public static void ToStringMemoryFromReadOnlyMemory()
        {
            string testString = "abcdefg";
            Memory<char> memory = MemoryMarshal.AsMemory(testString.AsMemory());
            Assert.Equal(testString, memory.ToString());
            Assert.Equal(testString.Substring(1, 1), memory.Slice(1, 1).ToString());
            Assert.Equal(testString, memory.Span.ToString());
        }

        [Fact]
        public static void ToStringForMemoryOfString()
        {
            string[] a = { "a", "b", "c" };
            var memory = new Memory<string>(a);
            Assert.Equal("System.Memory<String>[3]", memory.ToString());
            Assert.Equal("System.Memory<String>[1]", memory.Slice(1, 1).ToString());
            Assert.Equal("System.Span<String>[3]", memory.Span.ToString());
        }

        [Fact]
        public static void ToStringFromMemoryFromOwnedMemory()
        {
            int[] a = { 91, 92, -93, 94 };
            OwnedMemory<int> intOwner = new CustomMemoryForTest<int>(a);
            Assert.Equal("System.Memory<Int32>[4]", intOwner.Memory.ToString());

            intOwner = new CustomMemoryForTest<int>(Array.Empty<int>());
            Assert.Equal("System.Memory<Int32>[0]", intOwner.Memory.ToString());

            char[] charArray = { '1', '2', '-', '4' };
            OwnedMemory<char> charOwner = new CustomMemoryForTest<char>(charArray);
            Assert.Equal("12-4", charOwner.Memory.ToString());

            charOwner = new CustomMemoryForTest<char>(Array.Empty<char>());
            Assert.Equal("", charOwner.Memory.ToString());

            string[] strArray = { "91", "92", "-93", "94" };
            OwnedMemory<string> strOwner = new CustomMemoryForTest<string>(strArray);
            Assert.Equal("System.Memory<String>[4]", strOwner.Memory.ToString());

            strOwner = new CustomMemoryForTest<string>(Array.Empty<string>());
            Assert.Equal("System.Memory<String>[0]", strOwner.Memory.ToString());
        }

        [Fact]
        public static void ToStringMemoryOverFullStringReturnsOriginal()
        {
            string original = TestHelpers.BuildString(10, 42);

            ReadOnlyMemory<char> readOnlyMemory = original.AsMemory();
            Memory<char> memory = MemoryMarshal.AsMemory(readOnlyMemory);

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
