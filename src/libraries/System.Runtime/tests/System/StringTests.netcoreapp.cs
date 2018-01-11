// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Tests
{
    public partial class StringTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(3, 1)]
        public static void Ctor_CharSpan_EmptyString(int length, int offset)
        {
            Assert.Same(string.Empty, new string(new ReadOnlySpan<char>(new char[length], offset, 0)));
        }

        [Theory]
        [InlineData(new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', '\0' }, 0, 8, "abcdefgh")]
        [InlineData(new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', '\0', 'i', 'j', 'k' }, 0, 12, "abcdefgh\0ijk")]
        [InlineData(new char[] { 'a', 'b', 'c' }, 0, 0, "")]
        [InlineData(new char[] { 'a', 'b', 'c' }, 0, 1, "a")]
        [InlineData(new char[] { 'a', 'b', 'c' }, 2, 1, "c")]
        [InlineData(new char[] { '\u8001', '\u8002', '\ufffd', '\u1234', '\ud800', '\udfff' }, 0, 6, "\u8001\u8002\ufffd\u1234\ud800\udfff")]
        public static void Ctor_CharSpan(char[] valueArray, int startIndex, int length, string expected)
        {
            var span = new ReadOnlySpan<char>(valueArray, startIndex, length);
            Assert.Equal(expected, new string(span));
        }

        [Fact]
        public static void Create_InvalidArguments_Throw()
        {
            AssertExtensions.Throws<ArgumentNullException>("action", () => string.Create(-1, 0, null));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => string.Create(-1, 0, (span, state) => { }));
        }

        [Fact]
        public static void Create_Length0_ReturnsEmptyString()
        {
            bool actionInvoked = false;
            Assert.Same(string.Empty, string.Create(0, 0, (span, state) => actionInvoked = true));
            Assert.False(actionInvoked);
        }

        [Fact]
        public static void Create_NullState_Allowed()
        {
            string result = string.Create(1, (object)null, (span, state) =>
            {
                span[0] = 'a';
                Assert.Null(state);
            });
            Assert.Equal("a", result);
        }

        [Fact]
        public static void Create_ClearsMemory()
        {
            const int Length = 10;
            string result = string.Create(Length, (object)null, (span, state) =>
            {
                for (int i = 0; i < span.Length; i++)
                {
                    Assert.Equal('\0', span[i]);
                }
            });
            Assert.Equal(new string('\0', Length), result);
        }

        [Theory]
        [InlineData("a")]
        [InlineData("this is a test")]
        [InlineData("\0\u8001\u8002\ufffd\u1234\ud800\udfff")]
        public static void Create_ReturnsExpectedString(string expected)
        {
            char[] input = expected.ToCharArray();
            string result = string.Create(input.Length, input, (span, state) =>
            {
                Assert.Same(input, state);
                for (int i = 0; i < state.Length; i++)
                {
                    span[i] = state[i];
                }
            });
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("Hello", 'H', true)]
        [InlineData("Hello", 'Z', false)]
        [InlineData("Hello", 'e', true)]
        [InlineData("Hello", 'E', false)]
        [InlineData("", 'H', false)]
        public static void Contains(string s, char value, bool expected)
        {
            Assert.Equal(expected, s.Contains(value));
        }

        [Theory]
        // CurrentCulture
        [InlineData("Hello", 'H', StringComparison.CurrentCulture, true)]
        [InlineData("Hello", 'Z', StringComparison.CurrentCulture, false)]
        [InlineData("Hello", 'e', StringComparison.CurrentCulture, true)]
        [InlineData("Hello", 'E', StringComparison.CurrentCulture, false)]
        [InlineData("", 'H', StringComparison.CurrentCulture, false)]
        // CurrentCultureIgnoreCase
        [InlineData("Hello", 'H', StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("Hello", 'Z', StringComparison.CurrentCultureIgnoreCase, false)]
        [InlineData("Hello", 'e', StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("Hello", 'E', StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("", 'H', StringComparison.CurrentCultureIgnoreCase, false)]
        // InvariantCulture
        [InlineData("Hello", 'H', StringComparison.InvariantCulture, true)]
        [InlineData("Hello", 'Z', StringComparison.InvariantCulture, false)]
        [InlineData("Hello", 'e', StringComparison.InvariantCulture, true)]
        [InlineData("Hello", 'E', StringComparison.InvariantCulture, false)]
        [InlineData("", 'H', StringComparison.InvariantCulture, false)]
        // InvariantCultureIgnoreCase
        [InlineData("Hello", 'H', StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Hello", 'Z', StringComparison.InvariantCultureIgnoreCase, false)]
        [InlineData("Hello", 'e', StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Hello", 'E', StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("", 'H', StringComparison.InvariantCultureIgnoreCase, false)]
        // Ordinal
        [InlineData("Hello", 'H', StringComparison.Ordinal, true)]
        [InlineData("Hello", 'Z', StringComparison.Ordinal, false)]
        [InlineData("Hello", 'e', StringComparison.Ordinal, true)]
        [InlineData("Hello", 'E', StringComparison.Ordinal, false)]
        [InlineData("", 'H', StringComparison.Ordinal, false)]
        // OrdinalIgnoreCase
        [InlineData("Hello", 'H', StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("Hello", 'Z', StringComparison.OrdinalIgnoreCase, false)]
        [InlineData("Hello", 'e', StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("Hello", 'E', StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("", 'H', StringComparison.OrdinalIgnoreCase, false)]
        public static void Contains(string s, char value, StringComparison comparisionType, bool expected)
        {
            Assert.Equal(expected, s.Contains(value, comparisionType));
        }

        [Theory]
        // CurrentCulture
        [InlineData("Hello", "ello", StringComparison.CurrentCulture, true)]
        [InlineData("Hello", "ELL", StringComparison.CurrentCulture, false)]
        [InlineData("Hello", "ElLo", StringComparison.CurrentCulture, false)]
        [InlineData("Hello", "Larger Hello", StringComparison.CurrentCulture, false)]
        [InlineData("Hello", "Goodbye", StringComparison.CurrentCulture, false)]
        [InlineData("", "", StringComparison.CurrentCulture, true)]
        [InlineData("", "hello", StringComparison.CurrentCulture, false)]
        [InlineData("Hello", "", StringComparison.CurrentCulture, true)]
        [InlineData("Hello", "ell" + SoftHyphen, StringComparison.CurrentCulture, true)]
        [InlineData("Hello", "Ell" + SoftHyphen, StringComparison.CurrentCulture, false)]
        // CurrentCultureIgnoreCase
        [InlineData("Hello", "ello", StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("Hello", "ELL", StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("Hello", "ElLo", StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("Hello", "Larger Hello", StringComparison.CurrentCultureIgnoreCase, false)]
        [InlineData("Hello", "Goodbye", StringComparison.CurrentCultureIgnoreCase, false)]
        [InlineData("", "", StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("", "hello", StringComparison.CurrentCultureIgnoreCase, false)]
        [InlineData("Hello", "", StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("Hello", "ell" + SoftHyphen, StringComparison.CurrentCultureIgnoreCase, true)]
        [InlineData("Hello", "Ell" + SoftHyphen, StringComparison.CurrentCultureIgnoreCase, true)]
        // InvariantCulture
        [InlineData("Hello", "ello", StringComparison.InvariantCulture, true)]
        [InlineData("Hello", "ELL", StringComparison.InvariantCulture, false)]
        [InlineData("Hello", "ElLo", StringComparison.InvariantCulture, false)]
        [InlineData("Hello", "Larger Hello", StringComparison.InvariantCulture, false)]
        [InlineData("Hello", "Goodbye", StringComparison.InvariantCulture, false)]
        [InlineData("", "", StringComparison.InvariantCulture, true)]
        [InlineData("", "hello", StringComparison.InvariantCulture, false)]
        [InlineData("Hello", "", StringComparison.InvariantCulture, true)]
        [InlineData("Hello", "ell" + SoftHyphen, StringComparison.InvariantCulture, true)]
        [InlineData("Hello", "Ell" + SoftHyphen, StringComparison.InvariantCulture, false)]
        // InvariantCultureIgnoreCase
        [InlineData("Hello", "ello", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Hello", "ELL", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Hello", "ElLo", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Hello", "Larger Hello", StringComparison.InvariantCultureIgnoreCase, false)]
        [InlineData("Hello", "Goodbye", StringComparison.InvariantCultureIgnoreCase, false)]
        [InlineData("", "", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("", "hello", StringComparison.InvariantCultureIgnoreCase, false)]
        [InlineData("Hello", "", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Hello", "ell" + SoftHyphen, StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Hello", "Ell" + SoftHyphen, StringComparison.InvariantCultureIgnoreCase, true)]
        // Ordinal
        [InlineData("Hello", "ello", StringComparison.Ordinal, true)]
        [InlineData("Hello", "ELL", StringComparison.Ordinal, false)]
        [InlineData("Hello", "ElLo", StringComparison.Ordinal, false)]
        [InlineData("Hello", "Larger Hello", StringComparison.Ordinal, false)]
        [InlineData("Hello", "Goodbye", StringComparison.Ordinal, false)]
        [InlineData("", "", StringComparison.Ordinal, true)]
        [InlineData("", "hello", StringComparison.Ordinal, false)]
        [InlineData("Hello", "", StringComparison.Ordinal, true)]
        [InlineData("Hello", "ell" + SoftHyphen, StringComparison.Ordinal, false)]
        [InlineData("Hello", "Ell" + SoftHyphen, StringComparison.Ordinal, false)]
        // OrdinalIgnoreCase
        [InlineData("Hello", "ello", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("Hello", "ELL", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("Hello", "ElLo", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("Hello", "Larger Hello", StringComparison.OrdinalIgnoreCase, false)]
        [InlineData("Hello", "Goodbye", StringComparison.OrdinalIgnoreCase, false)]
        [InlineData("", "", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("", "hello", StringComparison.OrdinalIgnoreCase, false)]
        [InlineData("Hello", "", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("Hello", "ell" + SoftHyphen, StringComparison.OrdinalIgnoreCase, false)]
        [InlineData("Hello", "Ell" + SoftHyphen, StringComparison.OrdinalIgnoreCase, false)]
        public static void Contains(string s, string value, StringComparison comparisonType, bool expected)
        {
            Assert.Equal(expected, s.Contains(value, comparisonType));
        }

        [Fact]
        public static void Contains_StringComparison_TurkishI()
        {
            string str = "\u0069\u0130";
            RemoteInvoke((source) =>
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

                Assert.True(source.Contains("\u0069\u0069", StringComparison.CurrentCultureIgnoreCase));

                return SuccessExitCode;
            }, str).Dispose();

            RemoteInvoke((source) =>
            {
                CultureInfo.CurrentCulture = new CultureInfo("en-US");

                Assert.False(source.Contains("\u0069\u0069", StringComparison.CurrentCultureIgnoreCase));

                return SuccessExitCode;
            }, str).Dispose();
        }

        [Theory]
        [InlineData(StringComparison.CurrentCulture)]
        [InlineData(StringComparison.CurrentCultureIgnoreCase)]
        [InlineData(StringComparison.InvariantCulture)]
        [InlineData(StringComparison.InvariantCultureIgnoreCase)]
        [InlineData(StringComparison.Ordinal)]
        [InlineData(StringComparison.OrdinalIgnoreCase)]
        public static void Contains_NullValue_ThrowsArgumentNullException(StringComparison comparisonType)
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => "foo".Contains(null, comparisonType));
        }

        [Theory]
        [InlineData(StringComparison.CurrentCulture - 1)]
        [InlineData(StringComparison.OrdinalIgnoreCase + 1)]
        public static void Contains_InvalidComparisonType_ThrowsArgumentOutOfRangeException(StringComparison comparisonType)
        {
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "ab".Contains("a", comparisonType));
        }

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
            AssertExtensions.Throws<ArgumentNullException>("value", () => string.Join('|', (string[])null));
            AssertExtensions.Throws<ArgumentNullException>("value", () => string.Join('|', (string[])null, 0, 0));
            AssertExtensions.Throws<ArgumentNullException>("values", () => string.Join('|', (object[])null));
            AssertExtensions.Throws<ArgumentNullException>("values", () => string.Join('|', (IEnumerable<object>)null));
        }

        [Fact]
        public static void Join_Char_NegativeStartIndex_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => string.Join('|', new string[] { "Foo" }, -1, 0));
        }

        [Fact]
        public static void Join_Char_NegativeCount_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => string.Join('|', new string[] { "Foo" }, 0, -1));
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
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => string.Join('|', new string[] { "Foo" }, startIndex, count));
        }

        public static IEnumerable<object[]> Replace_StringComparison_TestData()
        {
            yield return new object[] { "abc", "abc", "def", StringComparison.CurrentCulture, "def" };
            yield return new object[] { "abc", "ABC", "def", StringComparison.CurrentCulture, "abc" };
            yield return new object[] { "abc", "abc", "", StringComparison.CurrentCulture, "" };
            yield return new object[] { "abc", "b", "LONG", StringComparison.CurrentCulture, "aLONGc" };
            yield return new object[] { "abc", "b", "d", StringComparison.CurrentCulture, "adc" };
            yield return new object[] { "abc", "b", null, StringComparison.CurrentCulture, "ac" };
            yield return new object[] { "abc", "abc" + SoftHyphen, "def", StringComparison.CurrentCulture, "def" };

            yield return new object[] { "abc", "abc", "def", StringComparison.CurrentCultureIgnoreCase, "def" };
            yield return new object[] { "abc", "ABC", "def", StringComparison.CurrentCultureIgnoreCase, "def" };
            yield return new object[] { "abc", "abc", "", StringComparison.CurrentCultureIgnoreCase, "" };
            yield return new object[] { "abc", "b", "LONG", StringComparison.CurrentCultureIgnoreCase, "aLONGc" };
            yield return new object[] { "abc", "b", "d", StringComparison.CurrentCultureIgnoreCase, "adc" };
            yield return new object[] { "abc", "b", null, StringComparison.CurrentCultureIgnoreCase, "ac" };
            yield return new object[] { "abc", "abc" + SoftHyphen, "def", StringComparison.CurrentCultureIgnoreCase, "def" };

            yield return new object[] { "abc", "abc", "def", StringComparison.Ordinal, "def" };
            yield return new object[] { "abc", "ABC", "def", StringComparison.Ordinal, "abc" };
            yield return new object[] { "abc", "abc", "", StringComparison.Ordinal, "" };
            yield return new object[] { "abc", "b", "LONG", StringComparison.Ordinal, "aLONGc" };
            yield return new object[] { "abc", "b", "d", StringComparison.Ordinal, "adc" };
            yield return new object[] { "abc", "b", null, StringComparison.Ordinal, "ac" };
            yield return new object[] { "abc", "abc" + SoftHyphen, "def", StringComparison.Ordinal, "abc" };

            yield return new object[] { "abc", "abc", "def", StringComparison.OrdinalIgnoreCase, "def" };
            yield return new object[] { "abc", "ABC", "def", StringComparison.OrdinalIgnoreCase, "def" };
            yield return new object[] { "abc", "abc", "", StringComparison.OrdinalIgnoreCase, "" };
            yield return new object[] { "abc", "b", "LONG", StringComparison.OrdinalIgnoreCase, "aLONGc" };
            yield return new object[] { "abc", "b", "d", StringComparison.OrdinalIgnoreCase, "adc" };
            yield return new object[] { "abc", "b", null, StringComparison.OrdinalIgnoreCase, "ac" };
            yield return new object[] { "abc", "abc" + SoftHyphen, "def", StringComparison.OrdinalIgnoreCase, "abc" };

            yield return new object[] { "abc", "abc", "def", StringComparison.InvariantCulture, "def" };
            yield return new object[] { "abc", "ABC", "def", StringComparison.InvariantCulture, "abc" };
            yield return new object[] { "abc", "abc", "", StringComparison.InvariantCulture, "" };
            yield return new object[] { "abc", "b", "LONG", StringComparison.InvariantCulture, "aLONGc" };
            yield return new object[] { "abc", "b", "d", StringComparison.InvariantCulture, "adc" };
            yield return new object[] { "abc", "b", null, StringComparison.InvariantCulture, "ac" };
            yield return new object[] { "abc", "abc" + SoftHyphen, "def", StringComparison.InvariantCulture, "def" };

            yield return new object[] { "abc", "abc", "def", StringComparison.InvariantCultureIgnoreCase, "def" };
            yield return new object[] { "abc", "ABC", "def", StringComparison.InvariantCultureIgnoreCase, "def" };
            yield return new object[] { "abc", "abc", "", StringComparison.InvariantCultureIgnoreCase, "" };
            yield return new object[] { "abc", "b", "LONG", StringComparison.InvariantCultureIgnoreCase, "aLONGc" };
            yield return new object[] { "abc", "b", "d", StringComparison.InvariantCultureIgnoreCase, "adc" };
            yield return new object[] { "abc", "b", null, StringComparison.InvariantCultureIgnoreCase, "ac" };
            yield return new object[] { "abc", "abc" + SoftHyphen, "def", StringComparison.InvariantCultureIgnoreCase, "def" };

            string turkishSource = "\u0069\u0130";

            yield return new object[] { turkishSource, "\u0069", "a", StringComparison.Ordinal, "a\u0130" };
            yield return new object[] { turkishSource, "\u0069", "a", StringComparison.OrdinalIgnoreCase, "a\u0130" };
            yield return new object[] { turkishSource, "\u0130", "a", StringComparison.Ordinal, "\u0069a" };
            yield return new object[] { turkishSource, "\u0130", "a", StringComparison.OrdinalIgnoreCase, "\u0069a" };

            yield return new object[] { turkishSource, "\u0069", "a", StringComparison.InvariantCulture, "a\u0130" };
            yield return new object[] { turkishSource, "\u0069", "a", StringComparison.InvariantCultureIgnoreCase, "a\u0130" };
            yield return new object[] { turkishSource, "\u0130", "a", StringComparison.InvariantCulture, "\u0069a" };
            yield return new object[] { turkishSource, "\u0130", "a", StringComparison.InvariantCultureIgnoreCase, "\u0069a" };
        }

        [Theory]
        [MemberData(nameof(Replace_StringComparison_TestData))]
        public void Replace_StringComparison_ReturnsExpected(string original, string oldValue, string newValue, StringComparison comparisonType, string expected)
        {
            Assert.Equal(expected, original.Replace(oldValue, newValue, comparisonType));
        }

        [Fact]
        public void Replace_StringComparison_TurkishI()
        {
            string src = "\u0069\u0130";

            RemoteInvoke((source) =>
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

                Assert.True("\u0069".Equals("\u0130", StringComparison.CurrentCultureIgnoreCase));

                Assert.Equal("a\u0130", source.Replace("\u0069", "a", StringComparison.CurrentCulture));
                Assert.Equal("aa", source.Replace("\u0069", "a", StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal("\u0069a", source.Replace("\u0130", "a", StringComparison.CurrentCulture));
                Assert.Equal("aa", source.Replace("\u0130", "a", StringComparison.CurrentCultureIgnoreCase));

                CultureInfo.CurrentCulture = new CultureInfo("en-US");

                Assert.False("\u0069".Equals("\u0130", StringComparison.CurrentCultureIgnoreCase));

                Assert.Equal("a\u0130", source.Replace("\u0069", "a", StringComparison.CurrentCulture));
                Assert.Equal("a\u0130", source.Replace("\u0069", "a", StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal("\u0069a", source.Replace("\u0130", "a", StringComparison.CurrentCulture));
                Assert.Equal("\u0069a", source.Replace("\u0130", "a", StringComparison.CurrentCultureIgnoreCase));

                return SuccessExitCode;
            }, src).Dispose();
        }

        public static IEnumerable<object[]> Replace_StringComparisonCulture_TestData()
        {
            yield return new object[] { "abc", "abc", "def", false, null, "def" };
            yield return new object[] { "abc", "ABC", "def", false, null, "abc" };
            yield return new object[] { "abc", "abc", "def", false, CultureInfo.InvariantCulture, "def" };
            yield return new object[] { "abc", "ABC", "def", false, CultureInfo.InvariantCulture, "abc" };

            yield return new object[] { "abc", "abc", "def", true, null, "def" };
            yield return new object[] { "abc", "ABC", "def", true, null, "def" };
            yield return new object[] { "abc", "abc", "def", true, CultureInfo.InvariantCulture, "def" };
            yield return new object[] { "abc", "ABC", "def", true, CultureInfo.InvariantCulture, "def" };

            yield return new object[] { "abc", "abc" + SoftHyphen, "def", false, null, "def" };
            yield return new object[] { "abc", "abc" + SoftHyphen, "def", true, null, "def" };
            yield return new object[] { "abc", "abc" + SoftHyphen, "def", false, CultureInfo.InvariantCulture, "def" };
            yield return new object[] { "abc", "abc" + SoftHyphen, "def", true, CultureInfo.InvariantCulture, "def" };

            yield return new object[] { "\u0069\u0130", "\u0069", "a", false, new CultureInfo("tr-TR"), "a\u0130" };
            yield return new object[] { "\u0069\u0130", "\u0069", "a", true, new CultureInfo("tr-TR"), "aa" };
            yield return new object[] { "\u0069\u0130", "\u0069", "a", false, CultureInfo.InvariantCulture, "a\u0130" };
            yield return new object[] { "\u0069\u0130", "\u0069", "a", true, CultureInfo.InvariantCulture, "a\u0130" };
        }

        [Theory]
        [MemberData(nameof(Replace_StringComparisonCulture_TestData))]
        public void Replace_StringComparisonCulture_ReturnsExpected(string original, string oldValue, string newValue, bool ignoreCase, CultureInfo culture, string expected)
        {
            Assert.Equal(expected, original.Replace(oldValue, newValue, ignoreCase, culture));
            if (culture == null)
            {
                Assert.Equal(expected, original.Replace(oldValue, newValue, ignoreCase, CultureInfo.CurrentCulture));
            }
        }

        [Fact]
        public void Replace_StringComparison_NullOldValue_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentNullException>("oldValue", () => "abc".Replace(null, "def", StringComparison.CurrentCulture));
            AssertExtensions.Throws<ArgumentNullException>("oldValue", () => "abc".Replace(null, "def", true, CultureInfo.CurrentCulture));
        }

        [Fact]
        public void Replace_StringComparison_EmptyOldValue_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("oldValue", () => "abc".Replace("", "def", StringComparison.CurrentCulture));
            AssertExtensions.Throws<ArgumentException>("oldValue", () => "abc".Replace("", "def", true, CultureInfo.CurrentCulture));
        }

        [Theory]
        [InlineData(StringComparison.CurrentCulture - 1)]
        [InlineData(StringComparison.OrdinalIgnoreCase + 1)]
        public void Replace_NoSuchStringComparison_ThrowsArgumentException(StringComparison comparisonType)
        {
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "abc".Replace("abc", "def", comparisonType));
        }


        private static readonly StringComparison[] StringComparisons = (StringComparison[])Enum.GetValues(typeof(StringComparison));


        public static IEnumerable<object[]> GetHashCode_StringComparison_Data => StringComparisons.Select(value => new object[] { value });

        [Theory]
        [MemberData(nameof(GetHashCode_StringComparison_Data))]
        public static void GetHashCode_StringComparison(StringComparison comparisonType)
        {
            Assert.Equal(StringComparer.FromComparison(comparisonType).GetHashCode("abc"), "abc".GetHashCode(comparisonType));
        }


        public static IEnumerable<object[]> GetHashCode_NoSuchStringComparison_ThrowsArgumentException_Data => new[]
        {
            new object[] { StringComparisons.Min() - 1 },
            new object[] { StringComparisons.Max() + 1 },
        };

        [Theory]
        [MemberData(nameof(GetHashCode_NoSuchStringComparison_ThrowsArgumentException_Data))]
        public static void GetHashCode_NoSuchStringComparison_ThrowsArgumentException(StringComparison comparisonType)
        {
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "abc".GetHashCode(comparisonType));
        }

        [Theory]
        [InlineData("")]
        [InlineData("a")]
        [InlineData("\0")]
        [InlineData("abc")]
        public static unsafe void ImplicitCast_ResultingSpanMatches(string s)
        {
            ReadOnlySpan<char> span = s;
            Assert.Equal(s.Length, span.Length);
            fixed (char* stringPtr = s)
            fixed (char* spanPtr = &MemoryMarshal.GetReference(span))
            {
                Assert.Equal((IntPtr)stringPtr, (IntPtr)spanPtr);
            }
        }

        [Fact]
        public static void ImplicitCast_NullString_ReturnsDefaultSpan()
        {
            ReadOnlySpan<char> span = (string)null;
            Assert.True(span == default);
        }

        [Theory]
        [InlineData("Hello", 'l', StringComparison.Ordinal, 2)]
        [InlineData("Hello", 'x', StringComparison.Ordinal, -1)]
        [InlineData("Hello", 'h', StringComparison.Ordinal, -1)]
        [InlineData("Hello", 'o', StringComparison.Ordinal, 4)]
        [InlineData("Hello", 'h', StringComparison.OrdinalIgnoreCase, 0)]
        [InlineData("HelLo", 'L', StringComparison.OrdinalIgnoreCase, 2)]
        [InlineData("HelLo", 'L', StringComparison.Ordinal, 3)]
        [InlineData("HelLo", '\0', StringComparison.Ordinal, -1)]
        [InlineData("!@#$%", '%', StringComparison.Ordinal, 4)]
        [InlineData("!@#$", '!', StringComparison.Ordinal, 0)]
        [InlineData("!@#$", '@', StringComparison.Ordinal, 1)]
        [InlineData("!@#$%", '%', StringComparison.OrdinalIgnoreCase, 4)]
        [InlineData("!@#$", '!', StringComparison.OrdinalIgnoreCase, 0)]
        [InlineData("!@#$", '@', StringComparison.OrdinalIgnoreCase, 1)]
        [InlineData("_____________\u807f", '\u007f', StringComparison.Ordinal, -1)]
        [InlineData("_____________\u807f__", '\u007f', StringComparison.Ordinal, -1)]
        [InlineData("_____________\u807f\u007f_", '\u007f', StringComparison.Ordinal, 14)]
        [InlineData("__\u807f_______________", '\u007f', StringComparison.Ordinal, -1)]
        [InlineData("__\u807f___\u007f___________", '\u007f', StringComparison.Ordinal, 6)]
        [InlineData("_____________\u807f", '\u007f', StringComparison.OrdinalIgnoreCase, -1)]
        [InlineData("_____________\u807f__", '\u007f', StringComparison.OrdinalIgnoreCase, -1)]
        [InlineData("_____________\u807f\u007f_", '\u007f', StringComparison.OrdinalIgnoreCase, 14)]
        [InlineData("__\u807f_______________", '\u007f', StringComparison.OrdinalIgnoreCase, -1)]
        [InlineData("__\u807f___\u007f___________", '\u007f', StringComparison.OrdinalIgnoreCase, 6)]
        public static void IndexOf_SingleLetter(string s, char target, StringComparison stringComparison, int expected)
        {
            Assert.Equal(expected, s.IndexOf(target, stringComparison));
        }

        [Fact]
        public static void IndexOf_TurkishI_TurkishCulture_Char()
        {
            RemoteInvoke(() =>
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

                string s = "Turkish I \u0131s TROUBL\u0130NG!";
                char value = '\u0130';
                Assert.Equal(19, s.IndexOf(value));
                Assert.Equal(19, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(4, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(19, s.IndexOf(value, StringComparison.Ordinal));
                Assert.Equal(19, s.IndexOf(value, StringComparison.OrdinalIgnoreCase));

                value = '\u0131';
                Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(8, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(10, s.IndexOf(value, StringComparison.Ordinal));
                Assert.Equal(10, s.IndexOf(value, StringComparison.OrdinalIgnoreCase));

                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public static void IndexOf_TurkishI_InvariantCulture_Char()
        {
            RemoteInvoke(() =>
            {
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

                string s = "Turkish I \u0131s TROUBL\u0130NG!";
                char value = '\u0130';

                Assert.Equal(19, s.IndexOf(value));
                Assert.Equal(19, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(19, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

                value = '\u0131';
                Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public static void IndexOf_TurkishI_EnglishUSCulture_Char()
        {
            RemoteInvoke(() =>
            {
                CultureInfo.CurrentCulture = new CultureInfo("en-US");

                string s = "Turkish I \u0131s TROUBL\u0130NG!";
                char value = '\u0130';

                Assert.Equal(19, s.IndexOf(value));
                Assert.Equal(19, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(19, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

                value = '\u0131';
                Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public static void IndexOf_EquivalentDiacritics_EnglishUSCulture_Char()
        {
            RemoteInvoke(() =>
            {
                string s = "Exhibit a\u0300\u00C0";
                char value = '\u00C0';

                CultureInfo.CurrentCulture = new CultureInfo("en-US");
                Assert.Equal(10, s.IndexOf(value));
                Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(8, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(10, s.IndexOf(value, StringComparison.Ordinal));
                Assert.Equal(10, s.IndexOf(value, StringComparison.OrdinalIgnoreCase));

                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public static void IndexOf_EquivalentDiacritics_InvariantCulture_Char()
        {
            RemoteInvoke(() =>
            {
                string s = "Exhibit a\u0300\u00C0";
                char value = '\u00C0';

                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                Assert.Equal(10, s.IndexOf(value));
                Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(8, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public static void IndexOf_CyrillicE_EnglishUSCulture_Char()
        {
            RemoteInvoke(() =>
            {
                string s = "Foo\u0400Bar";
                char value = '\u0400';

                CultureInfo.CurrentCulture = new CultureInfo("en-US");
                Assert.Equal(3, s.IndexOf(value));
                Assert.Equal(3, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(3, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(3, s.IndexOf(value, StringComparison.Ordinal));
                Assert.Equal(3, s.IndexOf(value, StringComparison.OrdinalIgnoreCase));

                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public static void IndexOf_CyrillicE_InvariantCulture_Char()
        {
            RemoteInvoke(() =>
            {
                string s = "Foo\u0400Bar";
                char value = '\u0400';

                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                Assert.Equal(3, s.IndexOf(value));
                Assert.Equal(3, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(3, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public static void IndexOf_Invalid_Char()
        {
            // Invalid comparison type
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "foo".IndexOf('o', StringComparison.CurrentCulture - 1));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => "foo".IndexOf('o', StringComparison.OrdinalIgnoreCase + 1));
        }
    }
}
