// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace System.Globalization.Tests
{
    public class CompareInfoIndexOfTests
    {
        private static CompareInfo s_invariantCompare = CultureInfo.InvariantCulture.CompareInfo;
        private static CompareInfo s_currentCompare = CultureInfo.CurrentCulture.CompareInfo;
        private static CompareInfo s_germanCompare = new CultureInfo("de-DE").CompareInfo;
        private static CompareInfo s_hungarianCompare = new CultureInfo("hu-HU").CompareInfo;
        private static CompareInfo s_turkishCompare = new CultureInfo("tr-TR").CompareInfo;
        private static CompareInfo s_slovakCompare = new CultureInfo("sk-SK").CompareInfo;

        public static IEnumerable<object[]> IndexOf_TestData()
        {
            // Empty string
            yield return new object[] { s_invariantCompare, "foo", "", 0, 3, CompareOptions.None, 0, 0 };
            yield return new object[] { s_invariantCompare, "foo", "", 2, 1, CompareOptions.None, 2, 0 };
            yield return new object[] { s_invariantCompare, "", "", 0, 0, CompareOptions.None, 0, 0 };

            // OrdinalIgnoreCase
            yield return new object[] { s_invariantCompare, "Hello", "l", 0, 5, CompareOptions.OrdinalIgnoreCase, 2, 1 };
            yield return new object[] { s_invariantCompare, "Hello", "L", 0, 5, CompareOptions.OrdinalIgnoreCase, 2, 1 };
            yield return new object[] { s_invariantCompare, "Hello", "h", 0, 5, CompareOptions.OrdinalIgnoreCase, 0, 1 };

            // Long strings
            yield return new object[] { s_invariantCompare, new string('b', 100) + new string('a', 5555), "aaaaaaaaaaaaaaa", 0, 5655, CompareOptions.None, 100, 15 };
            yield return new object[] { s_invariantCompare, new string('b', 101) + new string('a', 5555), new string('a', 5000), 0, 5656, CompareOptions.None, 101, 5000 };
            yield return new object[] { s_invariantCompare, new string('a', 5555), new string('a', 5000) + "b", 0, 5555, CompareOptions.None, -1, 0 };

            // Hungarian
            yield return new object[] { s_hungarianCompare, "foobardzsdzs", "rddzs", 0, 12, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_invariantCompare, "foobardzsdzs", "rddzs", 0, 12, CompareOptions.None, -1, 0 };
            yield return new object[] { s_invariantCompare, "foobardzsdzs", "rddzs", 0, 12, CompareOptions.Ordinal, -1, 0 };

            // Slovak
            yield return new object[] { s_slovakCompare, "ch", "h", 0, 2, CompareOptions.None, -1, 0 };
            yield return new object[] { s_slovakCompare, "chodit hore", "HO", 0, 11, CompareOptions.IgnoreCase, 7, 2 };
            yield return new object[] { s_slovakCompare, "chh", "h", 0, 3, CompareOptions.None, 2, 1 };

            // Turkish
            yield return new object[] { s_turkishCompare, "Hi", "I", 0, 2, CompareOptions.None, -1, 0 };
            yield return new object[] { s_turkishCompare, "Hi", "I", 0, 2, CompareOptions.IgnoreCase, -1, 0 };
            yield return new object[] { s_turkishCompare, "Hi", "\u0130", 0, 2, CompareOptions.None, -1, 0 };
            yield return new object[] { s_turkishCompare, "Hi", "\u0130", 0, 2, CompareOptions.IgnoreCase, 1, 1 };
            yield return new object[] { s_invariantCompare, "Hi", "I", 0, 2, CompareOptions.None, -1, 0 };
            yield return new object[] { s_invariantCompare, "Hi", "I", 0, 2, CompareOptions.IgnoreCase, 1, 1 };
            yield return new object[] { s_invariantCompare, "Hi", "\u0130", 0, 2, CompareOptions.IgnoreCase, -1, 0 };

            // Unicode
            yield return new object[] { s_invariantCompare, "Hi", "\u0130", 0, 2, CompareOptions.None, -1, 0 };
            yield return new object[] { s_invariantCompare, "Exhibit \u00C0", "A\u0300", 0, 9, CompareOptions.None, 8, 1 };
            yield return new object[] { s_invariantCompare, "Exhibit \u00C0", "A\u0300", 0, 9, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_invariantCompare, "Exhibit \u00C0", "a\u0300", 0, 9, CompareOptions.None, -1, 0 };
            yield return new object[] { s_invariantCompare, "Exhibit \u00C0", "a\u0300", 0, 9, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_invariantCompare, "Exhibit \u00C0", "a\u0300", 0, 9, CompareOptions.IgnoreCase, 8, 1 };
            yield return new object[] { s_invariantCompare, "Exhibit \u00C0", "a\u0300", 0, 9, CompareOptions.OrdinalIgnoreCase, -1, 0 };
            yield return new object[] { s_invariantCompare, "FooBar", "Foo\u0400Bar", 0, 6, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_invariantCompare, "TestFooBA\u0300R", "FooB\u00C0R", 0, 11, CompareOptions.IgnoreNonSpace, 4, 7 };
            yield return new object[] { s_invariantCompare, "o\u0308", "o", 0, 2, CompareOptions.None, -1, 0 };

            // Weightless characters
            yield return new object[] { s_invariantCompare, "", "\u200d", 0, 0, CompareOptions.None, 0, 0 };
            yield return new object[] { s_invariantCompare, "hello", "\u200d", 1, 3, CompareOptions.IgnoreCase, 1, 0 };

            // Ignore symbols
            yield return new object[] { s_invariantCompare, "More Test's", "Tests", 0, 11, CompareOptions.IgnoreSymbols, 5, 6 };
            yield return new object[] { s_invariantCompare, "More Test's", "Tests", 0, 11, CompareOptions.None, -1, 0 };
            yield return new object[] { s_invariantCompare, "cbabababdbaba", "ab", 0, 13, CompareOptions.None, 2, 2 };

            // Ordinal should be case-sensitive
            yield return new object[] { s_currentCompare, "a", "a", 0, 1, CompareOptions.Ordinal, 0, 1 };
            yield return new object[] { s_currentCompare, "a", "A", 0, 1, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_currentCompare, "abc", "aBc", 0, 3, CompareOptions.Ordinal, -1, 0 };

            // Ordinal with numbers and symbols
            yield return new object[] { s_currentCompare, "a", "1", 0, 1, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_currentCompare, "1", "1", 0, 1, CompareOptions.Ordinal, 0, 1 };
            yield return new object[] { s_currentCompare, "1", "!", 0, 1, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_currentCompare, "a", "-", 0, 1, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_currentCompare, "-", "-", 0, 1, CompareOptions.Ordinal, 0, 1 };
            yield return new object[] { s_currentCompare, "-", "!", 0, 1, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_currentCompare, "!", "!", 0, 1, CompareOptions.Ordinal, 0, 1 };

            // Ordinal with unicode
            yield return new object[] { s_currentCompare, "\uFF21", "\uFE57", 0, 1, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_currentCompare, "\uFE57", "\uFF21", 0, 1, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_currentCompare, "\uFF21", "a\u0400Bc", 0, 1, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_currentCompare, "\uFE57", "a\u0400Bc", 0, 1, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_currentCompare, "a", "a\u0400Bc", 0, 1, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_currentCompare, "a\u0400Bc", "a", 0, 4, CompareOptions.Ordinal, 0, 1 };

            // Ordinal with I or i (American and Turkish)
            yield return new object[] { s_currentCompare, "I", "i", 0, 1, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_currentCompare, "I", "I", 0, 1, CompareOptions.Ordinal, 0, 1 };
            yield return new object[] { s_currentCompare, "i", "I", 0, 1, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_currentCompare, "i", "i", 0, 1, CompareOptions.Ordinal, 0, 1 };
            yield return new object[] { s_currentCompare, "I", "\u0130", 0, 1, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_currentCompare, "\u0130", "I", 0, 1, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_currentCompare, "i", "\u0130", 0, 1, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_currentCompare, "\u0130", "i", 0, 1, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_currentCompare, "I", "\u0131", 0, 1, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_currentCompare, "\0131", "I", 0, 1, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_currentCompare, "i", "\u0131", 0, 1, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_currentCompare, "\u0131", "i", 0, 1, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_currentCompare, "\u0130", "\u0130", 0, 1, CompareOptions.Ordinal, 0, 1 };
            yield return new object[] { s_currentCompare, "\u0131", "\u0131", 0, 1, CompareOptions.Ordinal, 0, 1 };
            yield return new object[] { s_currentCompare, "\u0130", "\u0131", 0, 1, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_currentCompare, "\u0131", "\u0130", 0, 1, CompareOptions.Ordinal, -1, 0 };

            // Platform differences
            if (PlatformDetection.IsNlsGlobalization)
            {
                yield return new object[] { s_hungarianCompare, "foobardzsdzs", "rddzs", 0, 12, CompareOptions.None, 5, 7 };
            }
            else
            {
                yield return new object[] { s_hungarianCompare, "foobardzsdzs", "rddzs", 0, 12, CompareOptions.None, -1, 0 };
            }

            // Inputs where matched length does not equal value string length
            yield return new object[] { s_invariantCompare, "abcdzxyz", "\u01F3", 0, 8, CompareOptions.IgnoreNonSpace, 3, 2 };
            yield return new object[] { s_invariantCompare, "abc\u01F3xyz", "dz", 0, 7, CompareOptions.IgnoreNonSpace, 3, 1 };
            yield return new object[] { s_germanCompare, "abc Strasse Strasse xyz", "stra\u00DFe", 0, 23, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace, 4, 7 };
            yield return new object[] { s_germanCompare, "abc Strasse Strasse xyz", "xtra\u00DFe", 0, 23, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace, -1, 0 };
            yield return new object[] { s_germanCompare, "abc stra\u00DFe stra\u00DFe xyz", "Strasse", 0, 21, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace, 4, 6 };
            yield return new object[] { s_germanCompare, "abc stra\u00DFe stra\u00DFe xyz", "Xtrasse", 0, 21, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace, -1, 0 };
        }

        public static IEnumerable<object[]> IndexOf_Aesc_Ligature_TestData()
        {
            bool useNls = PlatformDetection.IsNlsGlobalization;
            // Searches for the ligature \u00C6
            string source1 = "Is AE or ae the same as \u00C6 or \u00E6?";
            yield return new object[] { s_invariantCompare, source1, "AE", 8, 18, CompareOptions.None, useNls ? 24 : -1, useNls ? 1 : 0};
            yield return new object[] { s_invariantCompare, source1, "ae", 8, 18, CompareOptions.None, 9 , 2};
            yield return new object[] { s_invariantCompare, source1, "\u00C6", 8, 18, CompareOptions.None, 24, 1 };
            yield return new object[] { s_invariantCompare, source1, "\u00E6", 8, 18, CompareOptions.None, useNls ? 9 : -1, useNls ? 2 : 0};
            yield return new object[] { s_invariantCompare, source1, "AE", 8, 18, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_invariantCompare, source1, "ae", 8, 18, CompareOptions.Ordinal, 9, 2 };
            yield return new object[] { s_invariantCompare, source1, "\u00C6", 8, 18, CompareOptions.Ordinal, 24, 1 };
            yield return new object[] { s_invariantCompare, source1, "\u00E6", 8, 18, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_invariantCompare, source1, "AE", 8, 18, CompareOptions.IgnoreCase, 9, 2 };
            yield return new object[] { s_invariantCompare, source1, "ae", 8, 18, CompareOptions.IgnoreCase, 9, 2 };
            yield return new object[] { s_invariantCompare, source1, "\u00C6", 8, 18, CompareOptions.IgnoreCase, useNls ? 9 : 24, useNls ? 2 : 1 };
            yield return new object[] { s_invariantCompare, source1, "\u00E6", 8, 18, CompareOptions.IgnoreCase, useNls ? 9 : 24, useNls ? 2 : 1 };
        }

        public static IEnumerable<object[]> IndexOf_U_WithDiaeresis_TestData()
        {
            // Searches for the combining character sequence Latin capital letter U with diaeresis or Latin small letter u with diaeresis.
            string source = "Is \u0055\u0308 or \u0075\u0308 the same as \u00DC or \u00FC?";
            yield return new object[] { s_invariantCompare, source, "U\u0308", 8, 18, CompareOptions.None, 24, 1 };
            yield return new object[] { s_invariantCompare, source, "u\u0308", 8, 18, CompareOptions.None, 9, 2 };
            yield return new object[] { s_invariantCompare, source, "\u00DC", 8, 18, CompareOptions.None, 24, 1 };
            yield return new object[] { s_invariantCompare, source, "\u00FC", 8, 18, CompareOptions.None, 9, 2 };
            yield return new object[] { s_invariantCompare, source, "U\u0308", 8, 18, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_invariantCompare, source, "u\u0308", 8, 18, CompareOptions.Ordinal, 9, 2 };
            yield return new object[] { s_invariantCompare, source, "\u00DC", 8, 18, CompareOptions.Ordinal, 24, 1 };
            yield return new object[] { s_invariantCompare, source, "\u00FC", 8, 18, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_invariantCompare, source, "U\u0308", 8, 18, CompareOptions.IgnoreCase, 9, 2 };
            yield return new object[] { s_invariantCompare, source, "u\u0308", 8, 18, CompareOptions.IgnoreCase, 9, 2 };
            yield return new object[] { s_invariantCompare, source, "\u00DC", 8, 18, CompareOptions.IgnoreCase, 9, 2 };
            yield return new object[] { s_invariantCompare, source, "\u00FC", 8, 18, CompareOptions.IgnoreCase, 9, 2 };
        }

        [Theory]
        [MemberData(nameof(IndexOf_TestData))]
        [MemberData(nameof(IndexOf_Aesc_Ligature_TestData))]
        [MemberData(nameof(IndexOf_U_WithDiaeresis_TestData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36672", TestPlatforms.Android)]
        public void IndexOf_String(CompareInfo compareInfo, string source, string value, int startIndex, int count, CompareOptions options, int expected, int expectedMatchLength)
        {
            if (value.Length == 1)
            {
                IndexOf_Char(compareInfo, source, value[0], startIndex, count, options, expected);
            }
            if (options == CompareOptions.None)
            {
                // Use IndexOf(string, string, int, int) or IndexOf(string, string)
                if (startIndex == 0 && count == source.Length)
                {
                    // Use IndexOf(string, string)
                    Assert.Equal(expected, compareInfo.IndexOf(source, value));
                }
                // Use IndexOf(string, string, int, int)
                Assert.Equal(expected, compareInfo.IndexOf(source, value, startIndex, count));
            }
            if (startIndex + count == source.Length)
            {
                // Use IndexOf(string, string, int, CompareOptions) or IndexOf(string, string, CompareOptions)
                if (startIndex == 0)
                {
                    // Use IndexOf(string, string, CompareOptions)
                    Assert.Equal(expected, compareInfo.IndexOf(source, value, options));
                }
                // Use IndexOf(string, string, int, CompareOptions)
                Assert.Equal(expected, compareInfo.IndexOf(source, value, startIndex, options));
            }
            // Use IndexOf(string, string, int, int, CompareOptions)
            Assert.Equal(expected, compareInfo.IndexOf(source, value, startIndex, count, options));

            if ((compareInfo == s_invariantCompare) && ((options == CompareOptions.None) || (options == CompareOptions.IgnoreCase)))
            {
                StringComparison stringComparison = (options == CompareOptions.IgnoreCase) ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
                // Use int string.IndexOf(string, StringComparison)
                Assert.Equal(expected, source.IndexOf(value, startIndex, count, stringComparison));
                // Use int MemoryExtensions.IndexOf(this ReadOnlySpan<char>, ReadOnlySpan<char>, StringComparison)
                Assert.Equal((expected == -1) ? -1 : (expected - startIndex), source.AsSpan(startIndex, count).IndexOf(value.AsSpan(), stringComparison));
            }

            // Now test the span-based versions - use BoundedMemory to detect buffer overruns

            RunSpanIndexOfTest(compareInfo, source.AsSpan(startIndex, count), value, options, (expected < 0) ? expected : expected - startIndex, expectedMatchLength);

            static void RunSpanIndexOfTest(CompareInfo compareInfo, ReadOnlySpan<char> source, ReadOnlySpan<char> value, CompareOptions options, int expected, int expectedMatchLength)
            {
                using BoundedMemory<char> sourceBoundedMemory = BoundedMemory.AllocateFromExistingData(source);
                sourceBoundedMemory.MakeReadonly();

                using BoundedMemory<char> valueBoundedMemory = BoundedMemory.AllocateFromExistingData(value);
                valueBoundedMemory.MakeReadonly();

                Assert.Equal(expected, compareInfo.IndexOf(sourceBoundedMemory.Span, valueBoundedMemory.Span, options));
                Assert.Equal(expected, compareInfo.IndexOf(sourceBoundedMemory.Span, valueBoundedMemory.Span, options, out int actualMatchLength));
                Assert.Equal(expectedMatchLength, actualMatchLength);

                if (TryCreateRuneFrom(value, out Rune rune))
                {
                    Assert.Equal(expected, compareInfo.IndexOf(sourceBoundedMemory.Span, rune, options)); // try the Rune-based version
                }
            }
        }

        private static void IndexOf_Char(CompareInfo compareInfo, string source, char value, int startIndex, int count, CompareOptions options, int expected)
        {
            if (options == CompareOptions.None)
            {
                // Use IndexOf(string, char, int, int) or IndexOf(string, char)
                if (startIndex == 0 && count == source.Length)
                {
                    // Use IndexOf(string, char)
                    Assert.Equal(expected, compareInfo.IndexOf(source, value));
                }
                // Use IndexOf(string, char, int, int)
                Assert.Equal(expected, compareInfo.IndexOf(source, value, startIndex, count));
            }
            if (startIndex + count == source.Length)
            {
                // Use IndexOf(string, char, int, CompareOptions) or IndexOf(string, char, CompareOptions)
                if (startIndex == 0)
                {
                    // Use IndexOf(string, char, CompareOptions)
                    Assert.Equal(expected, compareInfo.IndexOf(source, value, options));
                }
                // Use IndexOf(string, char, int, CompareOptions)
                Assert.Equal(expected, compareInfo.IndexOf(source, value, startIndex, options));
            }
            // Use IndexOf(string, char, int, int, CompareOptions)
            Assert.Equal(expected, compareInfo.IndexOf(source, value, startIndex, count, options));
        }

        [Fact]
        public void IndexOf_UnassignedUnicode()
        {
            bool useNls = PlatformDetection.IsNlsGlobalization;
            int expectedMatchLength = (useNls) ? 6 : 0;
            IndexOf_String(s_invariantCompare, "FooBar", "Foo\uFFFFBar", 0, 6, CompareOptions.None, useNls ? 0 : -1, expectedMatchLength);
            IndexOf_String(s_invariantCompare, "~FooBar", "Foo\uFFFFBar", 0, 7, CompareOptions.IgnoreNonSpace, useNls ? 1 : -1, expectedMatchLength);
        }

        [Fact]
        public void IndexOf_Invalid()
        {
            // Source is null
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.IndexOf(null, "a"));
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.IndexOf(null, "a", CompareOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.IndexOf(null, "a", 0, 0));
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.IndexOf(null, "a", 0, CompareOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.IndexOf(null, "a", 0, 0, CompareOptions.None));

            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.IndexOf(null, 'a'));
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.IndexOf(null, 'a', CompareOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.IndexOf(null, 'a', 0, 0));
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.IndexOf(null, 'a', 0, CompareOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.IndexOf(null, 'a', 0, 0, CompareOptions.None));

            // Value is null
            AssertExtensions.Throws<ArgumentNullException>("value", () => s_invariantCompare.IndexOf("", null));
            AssertExtensions.Throws<ArgumentNullException>("value", () => s_invariantCompare.IndexOf("", null, CompareOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("value", () => s_invariantCompare.IndexOf("", null, 0, 0));
            AssertExtensions.Throws<ArgumentNullException>("value", () => s_invariantCompare.IndexOf("", null, 0, CompareOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("value", () => s_invariantCompare.IndexOf("", null, 0, 0, CompareOptions.None));

            // Source and value are null
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.IndexOf(null, null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.IndexOf(null, null, CompareOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.IndexOf(null, null, 0, 0));
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.IndexOf(null, null, 0, CompareOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.IndexOf(null, null, 0, 0, CompareOptions.None));

            // Options are invalid
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's", "Tests", CompareOptions.StringSort));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's", "Tests", 0, CompareOptions.StringSort));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's", "Tests", 0, 2, CompareOptions.StringSort));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's", 'a', CompareOptions.StringSort));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's", 'b', 0, CompareOptions.StringSort));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's", 'c', 0, 2, CompareOptions.StringSort));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's".AsSpan(), "b".AsSpan(), CompareOptions.StringSort));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's".AsSpan(), "b".AsSpan(), CompareOptions.StringSort, out _));

            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's", "Tests", CompareOptions.Ordinal | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's", "Tests", 0, CompareOptions.Ordinal | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's", "Tests", 0, 2, CompareOptions.Ordinal | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's", 'a', CompareOptions.Ordinal | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's", 'b', 0, CompareOptions.Ordinal | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's", 'c', 0, 2, CompareOptions.Ordinal | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's".AsSpan(), "b".AsSpan(), CompareOptions.Ordinal | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's".AsSpan(), "b".AsSpan(), CompareOptions.Ordinal | CompareOptions.IgnoreWidth, out _));

            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's", "Tests", CompareOptions.OrdinalIgnoreCase | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's", "Tests", 0, CompareOptions.OrdinalIgnoreCase | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's", "Tests", 0, 2, CompareOptions.OrdinalIgnoreCase | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's", 'a', CompareOptions.OrdinalIgnoreCase | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's", 'b', 0, CompareOptions.OrdinalIgnoreCase | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's", 'c', 0, 2, CompareOptions.OrdinalIgnoreCase | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's".AsSpan(), "b".AsSpan(), CompareOptions.OrdinalIgnoreCase | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's".AsSpan(), "b".AsSpan(), CompareOptions.OrdinalIgnoreCase | CompareOptions.IgnoreWidth, out _));

            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's", "Tests", (CompareOptions)(-1)));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's", "Tests", 0, (CompareOptions)(-1)));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's", "Tests", 0, 2, (CompareOptions)(-1)));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's", 'a', (CompareOptions)(-1)));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's", 'a', 0, (CompareOptions)(-1)));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's", 'a', 0, 2, (CompareOptions)(-1)));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's".AsSpan(), "b".AsSpan(), (CompareOptions)(-1)));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's".AsSpan(), "b".AsSpan(), (CompareOptions)(-1), out _));

            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's", "Tests", (CompareOptions)0x11111111));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's", "Tests", 0, (CompareOptions)0x11111111));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's", "Tests", 0, 2, (CompareOptions)0x11111111));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's", 'a', (CompareOptions)0x11111111));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's", 'a', 0, (CompareOptions)0x11111111));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's", 'a', 0, 2, (CompareOptions)0x11111111));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's".AsSpan(), "b".AsSpan(), (CompareOptions)0x11111111));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.IndexOf("Test's".AsSpan(), "b".AsSpan(), (CompareOptions)0x11111111, out _));

            // StartIndex < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s_invariantCompare.IndexOf("Test", "Test", -1, CompareOptions.None));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s_invariantCompare.IndexOf("Test", "Test", -1, 4));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s_invariantCompare.IndexOf("Test", "Test", -1, 4, CompareOptions.None));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s_invariantCompare.IndexOf("Test", 'a', -1, CompareOptions.None));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s_invariantCompare.IndexOf("Test", 'a', -1, 4));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s_invariantCompare.IndexOf("Test", 'a', -1, 4, CompareOptions.None));

            // StartIndex > source.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s_invariantCompare.IndexOf("Test", "Test", 5, CompareOptions.None));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s_invariantCompare.IndexOf("Test", "Test", 5, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s_invariantCompare.IndexOf("Test", "Test", 5, 0, CompareOptions.None));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s_invariantCompare.IndexOf("Test", 'a', 5, CompareOptions.None));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s_invariantCompare.IndexOf("Test", 'a', 5, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s_invariantCompare.IndexOf("Test", 'a', 5, 0, CompareOptions.None));

            // Count < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.IndexOf("Test", "Test", 0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.IndexOf("Test", "Test", 0, -1, CompareOptions.None));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.IndexOf("Test", 'a', 0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.IndexOf("Test", 'a', 0, -1, CompareOptions.None));

            // Count > source.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.IndexOf("Test", "Test", 0, 5));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.IndexOf("Test", "Test", 0, 5, CompareOptions.None));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.IndexOf("Test", 'a', 0, 5));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.IndexOf("Test", 'a', 0, 5, CompareOptions.None));

            // StartIndex + count > source.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.IndexOf("Test", "Test", 2, 4));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.IndexOf("Test", "Test", 2, 4, CompareOptions.None));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.IndexOf("Test", 'a', 2, 4));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.IndexOf("Test", 'a', 2, 4, CompareOptions.None));
        }

        // Attempts to create a Rune from the entirety of a given text buffer.
        private static bool TryCreateRuneFrom(ReadOnlySpan<char> text, out Rune value)
        {
            return Rune.DecodeFromUtf16(text, out value, out int charsConsumed) == OperationStatus.Done
                && charsConsumed == text.Length;
        }
    }
}
