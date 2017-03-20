// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Tests
{
    public static partial class StringTests
    {
        [Theory]
        [InlineData("Hello", 'o', true)]
        [InlineData("Hello", 'O', false)]
        [InlineData("o", 'o', true)]
        [InlineData("o", 'O', false)]
        [InlineData("Hello", 'e', false)]
        [InlineData("Hello", '\0', false)]
        [InlineData("", '\0', false)]
        [InlineData("\0", '\0', true)]
        [InlineData("", 'a', false)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", 'z', true)]
        public static void EndsWith(string s, char value, bool expected)
        {
            Assert.Equal(expected, s.EndsWith(value));
        }

        [Theory]
        [InlineData("Hello", 'H', true)]
        [InlineData("Hello", 'h', false)]
        [InlineData("H", 'H', true)]
        [InlineData("H", 'h', false)]
        [InlineData("Hello", 'e', false)]
        [InlineData("Hello", '\0', false)]
        [InlineData("", '\0', false)]
        [InlineData("\0", '\0', true)]
        [InlineData("", 'a', false)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", 'a', true)]
        public static void StartsWith(string s, char value, bool expected)
        {
            Assert.Equal(expected, s.StartsWith(value));
        }

        public static IEnumerable<object[]> Join_Char_StringArray_TestData()
        {
            yield return new object[] { '|', new string[0], 0, 0, "" };
            yield return new object[] { '|', new string[] { "a" }, 0, 1, "a" };
            yield return new object[] { '|', new string[] { "a", "b", "c" }, 0, 3, "a|b|c" };
            yield return new object[] { '|', new string[] { "a", "b", "c" }, 0, 2, "a|b" };
            yield return new object[] { '|', new string[] { "a", "b", "c" }, 1, 1, "b" };
            yield return new object[] { '|', new string[] { "a", "b", "c" }, 1, 2, "b|c" };
            yield return new object[] { '|', new string[] { "a", "b", "c" }, 3, 0, "" };
            yield return new object[] { '|', new string[] { "a", "b", "c" }, 0, 0, "" };
            yield return new object[] { '|', new string[] { "", "", "" }, 0, 3, "||" };
            yield return new object[] { '|', new string[] { null, null, null }, 0, 3, "||" };
        }

        [Theory]
        [MemberData(nameof(Join_Char_StringArray_TestData))]
        public static void Join_Char_StringArray(char separator, string[] values, int startIndex, int count, string expected)
        {
            if (startIndex == 0 && count == values.Length)
            {
                Assert.Equal(expected, string.Join(separator, values));
                Assert.Equal(expected, string.Join(separator, (IEnumerable<string>)values));
                Assert.Equal(expected, string.Join(separator, (object[])values));
                Assert.Equal(expected, string.Join(separator, (IEnumerable<object>)values));
            }

            Assert.Equal(expected, string.Join(separator, values, startIndex, count));
            Assert.Equal(expected, string.Join(separator.ToString(), values, startIndex, count));
        }

        public static IEnumerable<object[]> Join_Char_ObjectArray_TestData()
        {
            yield return new object[] { '|', new object[0], "" };
            yield return new object[] { '|', new object[] { 1 }, "1" };
            yield return new object[] { '|', new object[] { 1, 2, 3 }, "1|2|3" };
            yield return new object[] { '|', new object[] { new ObjectWithNullToString(), 2, new ObjectWithNullToString() }, "|2|" };
            yield return new object[] { '|', new object[] { "1", null, "3" }, "1||3" };
            yield return new object[] { '|', new object[] { "", "", "" }, "||" };
            yield return new object[] { '|', new object[] { "", null, "" }, "||" };
            yield return new object[] { '|', new object[] { null, null, null }, "||" };
        }

        [Theory]
        [MemberData(nameof(Join_Char_ObjectArray_TestData))]
        public static void Join_Char_ObjectArray(char separator, object[] values, string expected)
        {
            Assert.Equal(expected, string.Join(separator, values));
            Assert.Equal(expected, string.Join(separator, (IEnumerable<object>)values));
        }

        [Fact]
        public static void Join_Char_NullValues_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("value", () => string.Join('|', (string[])null));
            Assert.Throws<ArgumentNullException>("value", () => string.Join('|', (string[])null, 0, 0));
            Assert.Throws<ArgumentNullException>("values", () => string.Join('|', (object[])null));
            Assert.Throws<ArgumentNullException>("values", () => string.Join('|', (IEnumerable<object>)null));
        }

        [Fact]
        public static void Join_Char_NegativeStartIndex_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => string.Join('|', new string[] { "Foo" }, -1, 0));
        }

        [Fact]
        public static void Join_Char_NegativeCount_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>("count", () => string.Join('|', new string[] { "Foo" }, 0, -1));
        }

        [Theory]
        [InlineData(2, 1)]
        [InlineData(2, 0)]
        [InlineData(1, 2)]
        [InlineData(1, 1)]
        [InlineData(0, 2)]
        [InlineData(-1, 0)]
        public static void Join_Char_InvalidStartIndexCount_ThrowsArgumentOutOfRangeException(int startIndex, int count)
        {
            Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => string.Join('|', new string[] { "Foo" }, startIndex, count));
        }
    }
}