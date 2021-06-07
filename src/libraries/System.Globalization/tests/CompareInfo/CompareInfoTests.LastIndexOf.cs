// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace System.Globalization.Tests
{
    public class CompareInfoLastIndexOfTests
    {
        private static CompareInfo s_invariantCompare = CultureInfo.InvariantCulture.CompareInfo;
        private static CompareInfo s_germanCompare = new CultureInfo("de-DE").CompareInfo;
        private static CompareInfo s_hungarianCompare = new CultureInfo("hu-HU").CompareInfo;
        private static CompareInfo s_turkishCompare = new CultureInfo("tr-TR").CompareInfo;
        private static CompareInfo s_slovakCompare = new CultureInfo("sk-SK").CompareInfo;

        public static IEnumerable<object[]> LastIndexOf_TestData()
        {
            bool useNls = PlatformDetection.IsNlsGlobalization;

            // Empty strings
            yield return new object[] { s_invariantCompare, "foo", "", 2, 3, CompareOptions.None, 3, 0 };
            yield return new object[] { s_invariantCompare, "", "", 0, 0, CompareOptions.None, 0, 0 };
            yield return new object[] { s_invariantCompare, "", "a", 0, 0, CompareOptions.None, -1, 0 };
            yield return new object[] { s_invariantCompare, "", "", -1, 0, CompareOptions.None, 0, 0 };
            yield return new object[] { s_invariantCompare, "", "a", -1, 0, CompareOptions.None, -1, 0 };
            yield return new object[] { s_invariantCompare, "", "", 0, -1, CompareOptions.None, 0, 0 };
            yield return new object[] { s_invariantCompare, "", "a", 0, -1, CompareOptions.None, -1, 0 };

            // Start index = source.Length
            yield return new object[] { s_invariantCompare, "Hello", "l", 5, 5, CompareOptions.None, 3, 1 };
            yield return new object[] { s_invariantCompare, "Hello", "b", 5, 5, CompareOptions.None, -1, 0 };
            yield return new object[] { s_invariantCompare, "Hello", "l", 5, 0, CompareOptions.None, -1, 0 };

            yield return new object[] { s_invariantCompare, "Hello", "", 5, 5, CompareOptions.None, 5, 0 };
            yield return new object[] { s_invariantCompare, "Hello", "", 5, 0, CompareOptions.None, 5, 0 };

            // OrdinalIgnoreCase
            yield return new object[] { s_invariantCompare, "Hello", "l", 4, 5, CompareOptions.OrdinalIgnoreCase, 3, 1 };
            yield return new object[] { s_invariantCompare, "Hello", "L", 4, 5, CompareOptions.OrdinalIgnoreCase, 3, 1 };
            yield return new object[] { s_invariantCompare, "Hello", "h", 4, 5, CompareOptions.OrdinalIgnoreCase, 0, 1 };

            // Long strings
            yield return new object[] { s_invariantCompare, new string('a', 5555) + new string('b', 100), "aaaaaaaaaaaaaaa", 5654, 5655, CompareOptions.None, 5540, 15 };
            yield return new object[] { s_invariantCompare, new string('b', 101) + new string('a', 5555), new string('a', 5000), 5655, 5656, CompareOptions.None, 656, 5000 };
            yield return new object[] { s_invariantCompare, new string('a', 5555), new string('a', 5000) + "b", 5554, 5555, CompareOptions.None, -1, 0 };

            // Hungarian
            yield return new object[] { s_hungarianCompare, "foobardzsdzs", "rddzs", 11, 12, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_invariantCompare, "foobardzsdzs", "rddzs", 11, 12, CompareOptions.None, -1, 0 };
            yield return new object[] { s_invariantCompare, "foobardzsdzs", "rddzs", 11, 12, CompareOptions.Ordinal, -1, 0 };

            // Slovak
            yield return new object[] { s_slovakCompare, "ch", "h", 0, 1, CompareOptions.None, -1, 0 };
            yield return new object[] { s_slovakCompare, "hore chodit", "HO", 11, 12, CompareOptions.IgnoreCase, 0, 2 };
            yield return new object[] { s_slovakCompare, "chh", "h", 2, 2, CompareOptions.None, 2, 1 };

            // Turkish
            yield return new object[] { s_turkishCompare, "Hi", "I", 1, 2, CompareOptions.None, -1, 0 };
            yield return new object[] { s_turkishCompare, "Hi", "I", 1, 2, CompareOptions.IgnoreCase, -1, 0 };
            yield return new object[] { s_turkishCompare, "Hi", "\u0130", 1, 2, CompareOptions.None, -1, 0 };
            yield return new object[] { s_turkishCompare, "Hi", "\u0130", 1, 2, CompareOptions.IgnoreCase, 1, 1 };

            yield return new object[] { s_invariantCompare, "Hi", "I", 1, 2, CompareOptions.None, -1, 0 };
            yield return new object[] { s_invariantCompare, "Hi", "I", 1, 2, CompareOptions.IgnoreCase, 1, 1 };
            yield return new object[] { s_invariantCompare, "Hi", "\u0130", 1, 2, CompareOptions.None, -1, 0 };
            yield return new object[] { s_invariantCompare, "Hi", "\u0130", 1, 2, CompareOptions.IgnoreCase, -1, 0 };

            // Unicode
            yield return new object[] { s_invariantCompare, "Exhibit \u00C0", "A\u0300", 8, 9, CompareOptions.None, 8, 1 };
            yield return new object[] { s_invariantCompare, "Exhibit \u00C0", "A\u0300", 8, 9, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_invariantCompare, "Exhibit \u00C0", "a\u0300", 8, 9, CompareOptions.None, -1, 0 };
            yield return new object[] { s_invariantCompare, "Exhibit \u00C0", "a\u0300", 8, 9, CompareOptions.IgnoreCase, 8, 1 };
            yield return new object[] { s_invariantCompare, "Exhibit \u00C0", "a\u0300", 8, 9, CompareOptions.OrdinalIgnoreCase, -1, 0 };
            yield return new object[] { s_invariantCompare, "Exhibit \u00C0", "a\u0300", 8, 9, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_invariantCompare, "FooBar", "Foo\u0400Bar", 5, 6, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_invariantCompare, "TestFooBA\u0300R", "FooB\u00C0R", 10, 11, CompareOptions.IgnoreNonSpace, 4, 7 };
            yield return new object[] { s_invariantCompare, "o\u0308", "o", 1, 2, CompareOptions.None, -1, 0 };

            // Weightless characters
            // NLS matches weightless characters at the end of the string
            // ICU matches weightless characters at 1 index prior to the end of the string
            yield return new object[] { s_invariantCompare, "", "\u200d", 0, 0, CompareOptions.None, 0, 0 };
            yield return new object[] { s_invariantCompare, "", "\u200d", -1, 0, CompareOptions.None, 0, 0 };
            yield return new object[] { s_invariantCompare, "hello", "\u200d", 4, 5, CompareOptions.IgnoreCase, useNls ? 5 : 4 , 0};

            // Ignore symbols
            yield return new object[] { s_invariantCompare, "More Test's", "Tests", 10, 11, CompareOptions.IgnoreSymbols, 5, 6 };
            yield return new object[] { s_invariantCompare, "More Test's", "Tests", 10, 11, CompareOptions.None, -1, 0 };
            yield return new object[] { s_invariantCompare, "cbabababdbaba", "ab", 12, 13, CompareOptions.None, 10, 2 };

            // Platform differences
            if (PlatformDetection.IsNlsGlobalization)
            {
                yield return new object[] { s_hungarianCompare, "foobardzsdzs", "rddzs", 11, 12, CompareOptions.None, 5, 7 };
            }
            else
            {
                yield return new object[] { s_hungarianCompare, "foobardzsdzs", "rddzs", 11, 12, CompareOptions.None, -1, 0 };
            }

            // Inputs where matched length does not equal value string length
            yield return new object[] { s_invariantCompare, "abcdzxyz", "\u01F3", 7, 8, CompareOptions.IgnoreNonSpace, 3, 2 };
            yield return new object[] { s_invariantCompare, "abc\u01F3xyz", "dz", 6, 7, CompareOptions.IgnoreNonSpace, 3, 1 };
            yield return new object[] { s_germanCompare, "abc Strasse Strasse xyz", "stra\u00DFe", 22, 23, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace, 12, 7 };
            yield return new object[] { s_germanCompare, "abc Strasse Strasse xyz", "xtra\u00DFe", 22, 23, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace, -1, 0 };
            yield return new object[] { s_germanCompare, "abc stra\u00DFe stra\u00DFe xyz", "Strasse", 20, 21, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace, 11, 6 };
            yield return new object[] { s_germanCompare, "abc stra\u00DFe stra\u00DFe xyz", "Xtrasse", 20, 21, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace, -1, 0 };
        }

        public static IEnumerable<object[]> LastIndexOf_Aesc_Ligature_TestData()
        {
            bool useNls = PlatformDetection.IsNlsGlobalization;

            // Searches for the ligature \u00C6
            string source = "Is AE or ae the same as \u00C6 or \u00E6?";
            yield return new object[] { s_invariantCompare, source, "AE", 25, 18, CompareOptions.None, useNls ? 24 : -1, useNls ? 1 : 0 };
            yield return new object[] { s_invariantCompare, source, "ae", 25, 18, CompareOptions.None, 9, 2 };
            yield return new object[] { s_invariantCompare, source, '\u00C6', 25, 18, CompareOptions.None, 24, 1 };
            yield return new object[] { s_invariantCompare, source, '\u00E6', 25, 18, CompareOptions.None, useNls ? 9 : -1, useNls ? 2 : 0 };
            yield return new object[] { s_invariantCompare, source, "AE", 25, 18, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_invariantCompare, source, "ae", 25, 18, CompareOptions.Ordinal, 9, 2 };
            yield return new object[] { s_invariantCompare, source, '\u00C6', 25, 18, CompareOptions.Ordinal, 24, 1 };
            yield return new object[] { s_invariantCompare, source, '\u00E6', 25, 18, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_invariantCompare, source, "AE", 25, 18, CompareOptions.IgnoreCase, useNls ? 24 : 9, useNls ? 1 : 2 };
            yield return new object[] { s_invariantCompare, source, "ae", 25, 18, CompareOptions.IgnoreCase, useNls ? 24 : 9, useNls ? 1 : 2 };
            yield return new object[] { s_invariantCompare, source, '\u00C6', 25, 18, CompareOptions.IgnoreCase, 24, 1 };
            yield return new object[] { s_invariantCompare, source, '\u00E6', 25, 18, CompareOptions.IgnoreCase, 24, 1 };
        }

        public static IEnumerable<object[]> LastIndexOf_U_WithDiaeresis_TestData()
        {
            // Searches for the combining character sequence Latin capital letter U with diaeresis or Latin small letter u with diaeresis.
            string source = "Is \u0055\u0308 or \u0075\u0308 the same as \u00DC or \u00FC?";
            yield return new object[] { s_invariantCompare, source, "U\u0308", 25, 18, CompareOptions.None, 24, 1 };
            yield return new object[] { s_invariantCompare, source, "u\u0308", 25, 18, CompareOptions.None, 9, 2 };
            yield return new object[] { s_invariantCompare, source, '\u00DC', 25, 18, CompareOptions.None, 24, 1 };
            yield return new object[] { s_invariantCompare, source, '\u00FC', 25, 18, CompareOptions.None, 9, 2 };
            yield return new object[] { s_invariantCompare, source, "U\u0308", 25, 18, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_invariantCompare, source, "u\u0308", 25, 18, CompareOptions.Ordinal, 9, 2 };
            yield return new object[] { s_invariantCompare, source, '\u00DC', 25, 18, CompareOptions.Ordinal, 24, 1 };
            yield return new object[] { s_invariantCompare, source, '\u00FC', 25, 18, CompareOptions.Ordinal, -1, 0 };
            yield return new object[] { s_invariantCompare, source, "U\u0308", 25, 18, CompareOptions.IgnoreCase, 24, 1 };
            yield return new object[] { s_invariantCompare, source, "u\u0308", 25, 18, CompareOptions.IgnoreCase, 24, 1 };
            yield return new object[] { s_invariantCompare, source, '\u00DC', 25, 18, CompareOptions.IgnoreCase, 24, 1 };
            yield return new object[] { s_invariantCompare, source, '\u00FC', 25, 18, CompareOptions.IgnoreCase, 24, 1 };
        }

        [Theory]
        [MemberData(nameof(LastIndexOf_TestData))]
        [MemberData(nameof(LastIndexOf_U_WithDiaeresis_TestData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36672", TestPlatforms.Android)]
        public void LastIndexOf_String(CompareInfo compareInfo, string source, string value, int startIndex, int count, CompareOptions options, int expected, int expectedMatchLength)
        {
            if (value.Length == 1)
            {
                LastIndexOf_Char(compareInfo, source, value[0], startIndex, count, options, expected);
            }
            if (options == CompareOptions.None)
            {
                // Use LastIndexOf(string, string, int, int) or LastIndexOf(string, string)
                if (startIndex + 1 == source.Length && count == source.Length)
                {
                    // Use LastIndexOf(string, string)
                    Assert.Equal(expected, compareInfo.LastIndexOf(source, value));
                }
                // Use LastIndexOf(string, string, int, int)
                Assert.Equal(expected, compareInfo.LastIndexOf(source, value, startIndex, count));
            }
            if (count - startIndex - 1 == 0)
            {
                // Use LastIndexOf(string, string, int, CompareOptions) or LastIndexOf(string, string, CompareOptions)
                if (startIndex == source.Length)
                {
                    // Use LastIndexOf(string, string, CompareOptions)
                    Assert.Equal(expected, compareInfo.LastIndexOf(source, value, options));
                }
                // Use LastIndexOf(string, string, int, CompareOptions)
                Assert.Equal(expected, compareInfo.LastIndexOf(source, value, startIndex, options));
            }
            // Use LastIndexOf(string, string, int, int, CompareOptions)
            Assert.Equal(expected, compareInfo.LastIndexOf(source, value, startIndex, count, options));

            // Fixup offsets so that we can call the span-based APIs.

            ReadOnlySpan<char> sourceSpan;
            int adjustmentFactor; // number of chars to add to retured index from span-based APIs

            if (startIndex == source.Length - 1 && count == source.Length)
            {
                // This idiom means "read the whole span"
                sourceSpan = source;
                adjustmentFactor = 0;
            }
            else if (startIndex == source.Length)
            {
                // Account for possible off-by-one at the call site
                sourceSpan = source.AsSpan()[^(Math.Max(0, count - 1))..];
                adjustmentFactor = source.Length - sourceSpan.Length;
            }
            else
            {
                // Bump 'startIndex' by 1, then go back 'count' chars
                sourceSpan = source.AsSpan()[..(startIndex + 1)][^count..];
                adjustmentFactor = startIndex + 1 - count;
            }

            if (expected < 0) { adjustmentFactor = 0; } // don't modify "not found" (-1) return values

            if ((compareInfo == s_invariantCompare) && ((options == CompareOptions.None) || (options == CompareOptions.IgnoreCase)))
            {
                StringComparison stringComparison = (options == CompareOptions.IgnoreCase) ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;

                // Use int string.LastIndexOf(string, StringComparison)
                Assert.Equal(expected, source.LastIndexOf(value, startIndex, count, stringComparison));

                // Use int MemoryExtensions.LastIndexOf(this ReadOnlySpan<char>, ReadOnlySpan<char>, StringComparison)
                Assert.Equal(expected - adjustmentFactor, sourceSpan.LastIndexOf(value.AsSpan(), stringComparison));
            }

            // Now test the span-based versions - use BoundedMemory to detect buffer overruns

            RunSpanLastIndexOfTest(compareInfo, sourceSpan, value, options, expected - adjustmentFactor, expectedMatchLength);

            static void RunSpanLastIndexOfTest(CompareInfo compareInfo, ReadOnlySpan<char> source, ReadOnlySpan<char> value, CompareOptions options, int expected, int expectedMatchLength)
            {
                using BoundedMemory<char> sourceBoundedMemory = BoundedMemory.AllocateFromExistingData(source);
                sourceBoundedMemory.MakeReadonly();

                using BoundedMemory<char> valueBoundedMemory = BoundedMemory.AllocateFromExistingData(value);
                valueBoundedMemory.MakeReadonly();

                Assert.Equal(expected, compareInfo.LastIndexOf(sourceBoundedMemory.Span, valueBoundedMemory.Span, options));
                Assert.Equal(expected, compareInfo.LastIndexOf(sourceBoundedMemory.Span, valueBoundedMemory.Span, options, out int actualMatchLength));
                Assert.Equal(expectedMatchLength, actualMatchLength);

                if (TryCreateRuneFrom(value, out Rune rune))
                {
                    Assert.Equal(expected, compareInfo.LastIndexOf(sourceBoundedMemory.Span, rune, options)); // try the Rune-based version
                }
            }
        }

        private static void LastIndexOf_Char(CompareInfo compareInfo, string source, char value, int startIndex, int count, CompareOptions options, int expected)
        {
            if (options == CompareOptions.None)
            {
                // Use LastIndexOf(string, char, int, int) or LastIndexOf(string, char)
                if (startIndex + 1 == source.Length && count == source.Length)
                {
                    // Use LastIndexOf(string, char)
                    Assert.Equal(expected, compareInfo.LastIndexOf(source, value));
                }
                // Use LastIndexOf(string, char, int, int)
                Assert.Equal(expected, compareInfo.LastIndexOf(source, value, startIndex, count));
            }
            if (count - startIndex - 1 == 0)
            {
                // Use LastIndexOf(string, char, int, CompareOptions) or LastIndexOf(string, char, CompareOptions)
                if (startIndex == source.Length)
                {
                    // Use LastIndexOf(string, char, CompareOptions)
                    Assert.Equal(expected, compareInfo.LastIndexOf(source, value, options));
                }
                // Use LastIndexOf(string, char, int, CompareOptions)
                Assert.Equal(expected, compareInfo.LastIndexOf(source, value, startIndex, options));
            }
            // Use LastIndexOf(string, char, int, int, CompareOptions)
            Assert.Equal(expected, compareInfo.LastIndexOf(source, value, startIndex, count, options));
        }

        [Theory]
        [MemberData(nameof(LastIndexOf_Aesc_Ligature_TestData))]
        public void LastIndexOf_Aesc_Ligature(CompareInfo compareInfo, string source, string value, int startIndex, int count, CompareOptions options, int expected, int expectedMatchLength)
        {
            LastIndexOf_String(compareInfo, source, value, startIndex, count, options, expected, expectedMatchLength);
        }

        [Fact]
        public void LastIndexOf_UnassignedUnicode()
        {
            bool useNls = PlatformDetection.IsNlsGlobalization;
            int expectedMatchLength = (useNls) ? 6 : 0;
            LastIndexOf_String(s_invariantCompare, "FooBar", "Foo\uFFFFBar", 5, 6, CompareOptions.None, useNls ? 0 : -1, expectedMatchLength);
            LastIndexOf_String(s_invariantCompare, "~FooBar", "Foo\uFFFFBar", 6, 7, CompareOptions.IgnoreNonSpace, useNls ? 1 : -1, expectedMatchLength);
        }

        [Fact]
        public void LastIndexOf_Invalid()
        {
            // Source is null
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.LastIndexOf(null, "a"));
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.LastIndexOf(null, "a", CompareOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.LastIndexOf(null, "a", 0, 0));
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.LastIndexOf(null, "a", 0, CompareOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.LastIndexOf(null, "a", 0, 0, CompareOptions.None));

            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.LastIndexOf(null, 'a'));
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.LastIndexOf(null, 'a', CompareOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.LastIndexOf(null, 'a', 0, 0));
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.LastIndexOf(null, 'a', 0, CompareOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.LastIndexOf(null, 'a', 0, 0, CompareOptions.None));

            // Value is null
            AssertExtensions.Throws<ArgumentNullException>("value", () => s_invariantCompare.LastIndexOf("", null));
            AssertExtensions.Throws<ArgumentNullException>("value", () => s_invariantCompare.LastIndexOf("", null, CompareOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("value", () => s_invariantCompare.LastIndexOf("", null, 0, 0));
            AssertExtensions.Throws<ArgumentNullException>("value", () => s_invariantCompare.LastIndexOf("", null, 0, CompareOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("value", () => s_invariantCompare.LastIndexOf("", null, 0, 0, CompareOptions.None));

            // Source and value are null
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.LastIndexOf(null, null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.LastIndexOf(null, null, CompareOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.LastIndexOf(null, null, 0, 0));
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.LastIndexOf(null, null, 0, CompareOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("source", () => s_invariantCompare.LastIndexOf(null, null, 0, 0, CompareOptions.None));

            // Options are invalid
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", "Tests", CompareOptions.StringSort));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", "Tests", 0, CompareOptions.StringSort));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", "Tests", 0, 1, CompareOptions.StringSort));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", 'a', CompareOptions.StringSort));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", 'a', 0, CompareOptions.StringSort));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", 'a', 0, 1, CompareOptions.StringSort));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", "a".AsSpan(), CompareOptions.StringSort));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", "a".AsSpan(), CompareOptions.StringSort, out int matchLength));

            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", "Tests", CompareOptions.Ordinal | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", "Tests", 0, CompareOptions.Ordinal | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", "Tests", 0, 1, CompareOptions.Ordinal | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", 'a', CompareOptions.Ordinal | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", 'a', 0, CompareOptions.Ordinal | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", 'a', 0, 1, CompareOptions.Ordinal | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", "a".AsSpan(), CompareOptions.Ordinal | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", "a".AsSpan(), CompareOptions.Ordinal | CompareOptions.IgnoreWidth, out int matchLength));

            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", "Tests", CompareOptions.OrdinalIgnoreCase | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", "Tests", 0, CompareOptions.OrdinalIgnoreCase | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", "Tests", 0, 1, CompareOptions.OrdinalIgnoreCase | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", 'a', CompareOptions.OrdinalIgnoreCase | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", 'a', 0, CompareOptions.OrdinalIgnoreCase | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", 'a', 0, 1, CompareOptions.OrdinalIgnoreCase | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", "a".AsSpan(), CompareOptions.OrdinalIgnoreCase | CompareOptions.IgnoreWidth));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", "a".AsSpan(), CompareOptions.OrdinalIgnoreCase | CompareOptions.IgnoreWidth, out int matchLength));

            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", "Tests", (CompareOptions)(-1)));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", "Tests", 0, (CompareOptions)(-1)));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", "Tests", 0, 1, (CompareOptions)(-1)));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", "Tests", (CompareOptions)(-1)));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", 'a', 0, (CompareOptions)(-1)));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", 'a', 0, 1, (CompareOptions)(-1)));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", "a".AsSpan(), (CompareOptions)(-1)));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", "a".AsSpan(), (CompareOptions)(-1), out int matchLength));

            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", "Tests", (CompareOptions)0x11111111));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", "Tests", 0, (CompareOptions)0x11111111));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", "Tests", 0, 1, (CompareOptions)0x11111111));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", 'a', (CompareOptions)0x11111111));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", 'a', 0, (CompareOptions)0x11111111));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", 'a', 0, 1, (CompareOptions)0x11111111));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", "a".AsSpan(), (CompareOptions)0x11111111));
            AssertExtensions.Throws<ArgumentException>("options", () => s_invariantCompare.LastIndexOf("Test's", "a".AsSpan(), (CompareOptions)0x11111111, out int matchLength));

            // StartIndex < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s_invariantCompare.LastIndexOf("Test", "Test", -1, CompareOptions.None));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s_invariantCompare.LastIndexOf("Test", "Test", -1, 2));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s_invariantCompare.LastIndexOf("Test", "Test", -1, 2, CompareOptions.None));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s_invariantCompare.LastIndexOf("Test", 'a', -1, CompareOptions.None));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s_invariantCompare.LastIndexOf("Test", 'a', -1, 2));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s_invariantCompare.LastIndexOf("Test", 'a', -1, 2, CompareOptions.None));

            // StartIndex >= source.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s_invariantCompare.LastIndexOf("Test", "Test", 5, CompareOptions.None));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s_invariantCompare.LastIndexOf("Test", "Test", 5, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s_invariantCompare.LastIndexOf("Test", "Test", 5, 0, CompareOptions.None));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s_invariantCompare.LastIndexOf("Test", 'a', 5, CompareOptions.None));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s_invariantCompare.LastIndexOf("Test", 'a', 5, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => s_invariantCompare.LastIndexOf("Test", 'a', 5, 0, CompareOptions.None));

            // Count < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.LastIndexOf("Test", "Test", 0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.LastIndexOf("Test", "Test", 0, -1, CompareOptions.None));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.LastIndexOf("Test", 'a', 0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.LastIndexOf("Test", 'a', 0, -1, CompareOptions.None));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.LastIndexOf("Test", "Test", 4, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.LastIndexOf("Test", "Test", 4, -1, CompareOptions.None));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.LastIndexOf("Test", 'a', 4, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.LastIndexOf("Test", 'a', 4, -1, CompareOptions.None));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.LastIndexOf("Test", "", 4, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.LastIndexOf("Test", "", 4, -1, CompareOptions.None));

            // Count > source.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.LastIndexOf("Test", "Test", 0, 5));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.LastIndexOf("Test", "Test", 0, 5, CompareOptions.None));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.LastIndexOf("Test", 'a', 0, 5));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.LastIndexOf("Test", 'a', 0, 5, CompareOptions.None));

            // StartIndex + count > source.Length + 1
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.LastIndexOf("Test", "Test", 3, 5));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.LastIndexOf("Test", "Test", 3, 5, CompareOptions.None));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.LastIndexOf("Test", 'a', 3, 5));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.LastIndexOf("Test", 'a', 3, 5, CompareOptions.None));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.LastIndexOf("Test", "s", 4, 6));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.LastIndexOf("Test", "s", 4, 7, CompareOptions.None));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.LastIndexOf("Test", 's', 4, 6));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.LastIndexOf("Test", 's', 4, 7, CompareOptions.None));

            // Count > StartIndex + 1
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.LastIndexOf("Test", "e", 1, 3));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.LastIndexOf("Test", "e", 1, 3, CompareOptions.None));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.LastIndexOf("Test", 'e', 1, 3));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => s_invariantCompare.LastIndexOf("Test", 'e', 1, 3, CompareOptions.None));
        }

        // Attempts to create a Rune from the entirety of a given text buffer.
        private static bool TryCreateRuneFrom(ReadOnlySpan<char> text, out Rune value)
        {
            return Rune.DecodeFromUtf16(text, out value, out int charsConsumed) == OperationStatus.Done
                && charsConsumed == text.Length;
        }
    }
}
