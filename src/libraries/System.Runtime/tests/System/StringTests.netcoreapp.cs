// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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

        [Fact]
        public static unsafe void Ctor_CharSpan_Empty()
        {
            Assert.Same(string.Empty, new string((ReadOnlySpan<char>)null));
            Assert.Same(string.Empty, new string(ReadOnlySpan<char>.Empty));
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

            ReadOnlySpan<char> span = s.AsSpan();
            Assert.Equal(expected, span.Contains(value));
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
            Assert.Equal(expected, s.AsSpan().Contains(value, comparisonType));
        }

        [Fact]
        public static void Contains_StringComparison_TurkishI()
        {
            string str = "\u0069\u0130";
            RemoteInvoke((source) =>
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

                Assert.True(source.Contains("\u0069\u0069", StringComparison.CurrentCultureIgnoreCase));
                Assert.True(source.AsSpan().Contains("\u0069\u0069", StringComparison.CurrentCultureIgnoreCase));

                return SuccessExitCode;
            }, str).Dispose();

            RemoteInvoke((source) =>
            {
                CultureInfo.CurrentCulture = new CultureInfo("en-US");

                Assert.False(source.Contains("\u0069\u0069", StringComparison.CurrentCultureIgnoreCase));
                Assert.False(source.AsSpan().Contains("\u0069\u0069", StringComparison.CurrentCultureIgnoreCase));

                return SuccessExitCode;
            }, str).Dispose();
        }

        [Fact]
        public static void Contains_Match_Char()
        {
            Assert.False("".Contains('a'));
            Assert.False("".AsSpan().Contains('a'));

            // Use a long-enough string to incur vectorization code
            const int max = 250;

            for (var length = 1; length < max; length++)
            {
                char[] ca = new char[length];
                for (int i = 0; i < length; i++)
                {
                    ca[i] = (char)(i + 1);
                }

                var span = new Span<char>(ca);
                var ros = new ReadOnlySpan<char>(ca);
                var str = new string(ca);

                for (var targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    char target = ca[targetIndex];

                    // Span
                    bool found = span.Contains(target);
                    Assert.True(found);

                    // ReadOnlySpan
                    found = ros.Contains(target);
                    Assert.True(found);

                    // String
                    found = str.Contains(target);
                    Assert.True(found);
                }
            }
        }

        [Fact]
        public static void Contains_ZeroLength_Char()
        {
            // Span
            var span = new Span<char>(Array.Empty<char>());
            bool found = span.Contains((char)0);
            Assert.False(found);

            span = Span<char>.Empty;
            found = span.Contains((char)0);
            Assert.False(found);

            // ReadOnlySpan
            var ros = new ReadOnlySpan<char>(Array.Empty<char>());
            found = ros.Contains((char)0);
            Assert.False(found);

            ros = ReadOnlySpan<char>.Empty;
            found = ros.Contains((char)0);
            Assert.False(found);

            // String
            found = string.Empty.Contains((char)0);
            Assert.False(found);
        }

        [Fact]
        public static void Contains_MultipleMatches_Char()
        {
            for (int length = 2; length < 32; length++)
            {
                var ca = new char[length];
                for (int i = 0; i < length; i++)
                {
                    ca[i] = (char)(i + 1);
                }

                ca[length - 1] = (char)200;
                ca[length - 2] = (char)200;

                // Span
                var span = new Span<char>(ca);
                bool found = span.Contains((char)200);
                Assert.True(found);

                // ReadOnlySpan
                var ros = new ReadOnlySpan<char>(ca);
                found = ros.Contains((char)200);
                Assert.True(found);

                // String
                var str = new string(ca);
                found = str.Contains((char)200);
                Assert.True(found);
            }
        }

        [Fact]
        public static void Contains_EnsureNoChecksGoOutOfRange_Char()
        {
            for (int length = 0; length < 100; length++)
            {
                var ca = new char[length + 2];
                ca[0] = '9';
                ca[length + 1] = '9';

                // Span
                var span = new Span<char>(ca, 1, length);
                bool found = span.Contains('9');
                Assert.False(found);

                // ReadOnlySpan
                var ros = new ReadOnlySpan<char>(ca, 1, length);
                found = ros.Contains('9');
                Assert.False(found);

                // String
                var str = new string(ca, 1, length);
                found = str.Contains('9');
                Assert.False(found);
            }
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
        [InlineData(new char[0], new int[0])] // empty
        [InlineData(new char[] { 'x', 'y', 'z' }, new int[] { 'x', 'y', 'z' })]
        [InlineData(new char[] { 'x', '\uD86D', '\uDF54', 'y' }, new int[] { 'x', 0x2B754, 'y' })] // valid surrogate pair
        [InlineData(new char[] { 'x', '\uD86D', 'y' }, new int[] { 'x', 0xFFFD, 'y' })] // standalone high surrogate
        [InlineData(new char[] { 'x', '\uDF54', 'y' }, new int[] { 'x', 0xFFFD, 'y' })] // standalone low surrogate
        [InlineData(new char[] { 'x', '\uD86D' }, new int[] { 'x', 0xFFFD })] // standalone high surrogate at end of string
        [InlineData(new char[] { 'x', '\uDF54' }, new int[] { 'x', 0xFFFD })] // standalone low surrogate at end of string
        [InlineData(new char[] { 'x', '\uD86D', '\uD86D', 'y' }, new int[] { 'x', 0xFFFD, 0xFFFD, 'y' })] // two high surrogates should be two replacement chars
        [InlineData(new char[] { 'x', '\uFFFD', 'y' }, new int[] { 'x', 0xFFFD, 'y' })] // literal U+FFFD
        public static void EnumerateRunes(char[] chars, int[] expected)
        {
            // Test data is smuggled as char[] instead of straight-up string since the test framework
            // doesn't like invalid UTF-16 literals.

            string asString = new string(chars);

            // First, use a straight-up foreach keyword to ensure pattern matching works as expected

            List<int> enumeratedScalarValues = new List<int>();
            foreach (Rune rune in asString.EnumerateRunes())
            {
                enumeratedScalarValues.Add(rune.Value);
            }
            Assert.Equal(expected, enumeratedScalarValues.ToArray());

            // Then use LINQ to ensure IEnumerator<...> works as expected

            int[] enumeratedValues = new string(chars).EnumerateRunes().Select(r => r.Value).ToArray();
            Assert.Equal(expected, enumeratedValues);
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

                return SuccessExitCode;
            }, src).Dispose();

            RemoteInvoke((source) =>
            {
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

        [Fact]
        public static void GetHashCode_OfSpan_EmbeddedNull_ReturnsDifferentHashCodes()
        {
            Assert.NotEqual(string.GetHashCode("\0AAAAAAAAA".AsSpan()), string.GetHashCode("\0BBBBBBBBBBBB".AsSpan()));
        }

        [Fact]
        public static void GetHashCode_OfSpan_MatchesOfString()
        {
            // parameterless should be ordinal only
            Assert.Equal("abc".GetHashCode(), string.GetHashCode("abc".AsSpan()));
            Assert.NotEqual("abc".GetHashCode(), string.GetHashCode("ABC".AsSpan())); // case differences
        }

        [Fact]
        public static void GetHashCode_CompareInfo()
        {
            // ordinal
            Assert.Equal("abc".GetHashCode(), CultureInfo.InvariantCulture.CompareInfo.GetHashCode("abc", CompareOptions.Ordinal));
            Assert.NotEqual("abc".GetHashCode(), CultureInfo.InvariantCulture.CompareInfo.GetHashCode("ABC", CompareOptions.Ordinal));

            // ordinal ignore case
            Assert.Equal("abc".GetHashCode(StringComparison.OrdinalIgnoreCase), CultureInfo.InvariantCulture.CompareInfo.GetHashCode("abc", CompareOptions.OrdinalIgnoreCase));
            Assert.Equal("abc".GetHashCode(StringComparison.OrdinalIgnoreCase), CultureInfo.InvariantCulture.CompareInfo.GetHashCode("ABC", CompareOptions.OrdinalIgnoreCase));

            // culture-aware
            Assert.Equal("aeiXXabc".GetHashCode(StringComparison.CurrentCulture), CultureInfo.CurrentCulture.CompareInfo.GetHashCode("aeiXXabc", CompareOptions.None));
            Assert.Equal("aeiXXabc".GetHashCode(StringComparison.CurrentCultureIgnoreCase), CultureInfo.CurrentCulture.CompareInfo.GetHashCode("aeiXXabc", CompareOptions.IgnoreCase));

            // invariant culture
            Assert.Equal("aeiXXabc".GetHashCode(StringComparison.InvariantCulture), CultureInfo.InvariantCulture.CompareInfo.GetHashCode("aeiXXabc", CompareOptions.None));
            Assert.Equal("aeiXXabc".GetHashCode(StringComparison.InvariantCultureIgnoreCase), CultureInfo.InvariantCulture.CompareInfo.GetHashCode("aeiXXabc", CompareOptions.IgnoreCase));
        }

        [Fact]
        public static void GetHashCode_CompareInfo_OfSpan()
        {
            // ordinal
            Assert.Equal("abc".GetHashCode(), CultureInfo.InvariantCulture.CompareInfo.GetHashCode("abc".AsSpan(), CompareOptions.Ordinal));
            Assert.NotEqual("abc".GetHashCode(), CultureInfo.InvariantCulture.CompareInfo.GetHashCode("ABC".AsSpan(), CompareOptions.Ordinal));

            // ordinal ignore case
            Assert.Equal("abc".GetHashCode(StringComparison.OrdinalIgnoreCase), CultureInfo.InvariantCulture.CompareInfo.GetHashCode("abc".AsSpan(), CompareOptions.OrdinalIgnoreCase));
            Assert.Equal("abc".GetHashCode(StringComparison.OrdinalIgnoreCase), CultureInfo.InvariantCulture.CompareInfo.GetHashCode("ABC".AsSpan(), CompareOptions.OrdinalIgnoreCase));

            // culture-aware
            Assert.Equal("aeiXXabc".GetHashCode(StringComparison.CurrentCulture), CultureInfo.CurrentCulture.CompareInfo.GetHashCode("aeiXXabc".AsSpan(), CompareOptions.None));
            Assert.Equal("aeiXXabc".GetHashCode(StringComparison.CurrentCultureIgnoreCase), CultureInfo.CurrentCulture.CompareInfo.GetHashCode("aeiXXabc".AsSpan(), CompareOptions.IgnoreCase));

            // invariant culture
            Assert.Equal("aeiXXabc".GetHashCode(StringComparison.InvariantCulture), CultureInfo.InvariantCulture.CompareInfo.GetHashCode("aeiXXabc".AsSpan(), CompareOptions.None));
            Assert.Equal("aeiXXabc".GetHashCode(StringComparison.InvariantCultureIgnoreCase), CultureInfo.InvariantCulture.CompareInfo.GetHashCode("aeiXXabc".AsSpan(), CompareOptions.IgnoreCase));
        }

        public static IEnumerable<object[]> GetHashCode_StringComparison_Data => StringComparisons.Select(value => new object[] { value });

        [Theory]
        [MemberData(nameof(GetHashCode_StringComparison_Data))]
        public static void GetHashCode_StringComparison(StringComparison comparisonType)
        {
            int hashCodeFromStringComparer = StringComparer.FromComparison(comparisonType).GetHashCode("abc");
            int hashCodeFromStringGetHashCode = "abc".GetHashCode(comparisonType);
            int hashCodeFromStringGetHashCodeOfSpan = string.GetHashCode("abc".AsSpan(), comparisonType);

            Assert.Equal(hashCodeFromStringComparer, hashCodeFromStringGetHashCode);
            Assert.Equal(hashCodeFromStringComparer, hashCodeFromStringGetHashCodeOfSpan);
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
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => string.GetHashCode("abc".AsSpan(), comparisonType));
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
            var charArray = new char[1];
            charArray[0] = target;
            Assert.Equal(expected, s.AsSpan().IndexOf(charArray, stringComparison));
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

                ReadOnlySpan<char> span = s.AsSpan();
                Assert.Equal(19, span.IndexOf(new char[] { value }, StringComparison.CurrentCulture));
                Assert.Equal(4, span.IndexOf(new char[] { value }, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(19, span.IndexOf(new char[] { value }, StringComparison.Ordinal));
                Assert.Equal(19, span.IndexOf(new char[] { value }, StringComparison.OrdinalIgnoreCase));

                value = '\u0131';
                Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCulture));
                Assert.Equal(8, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(10, s.IndexOf(value, StringComparison.Ordinal));
                Assert.Equal(10, s.IndexOf(value, StringComparison.OrdinalIgnoreCase));

                Assert.Equal(10, span.IndexOf(new char[] { value }, StringComparison.CurrentCulture));
                Assert.Equal(8, span.IndexOf(new char[] { value }, StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal(10, span.IndexOf(new char[] { value }, StringComparison.Ordinal));
                Assert.Equal(10, span.IndexOf(new char[] { value }, StringComparison.OrdinalIgnoreCase));

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
