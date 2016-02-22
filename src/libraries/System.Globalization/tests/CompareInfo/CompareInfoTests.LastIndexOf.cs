// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

using Xunit;

namespace System.Globalization.Tests
{
    public class CompareInfoLastIndexOfTests
    {
        private static CompareInfo s_invariantCompare = CultureInfo.InvariantCulture.CompareInfo;
        private static CompareInfo s_hungarianCompare = new CultureInfo("hu-HU").CompareInfo;
        private static CompareInfo s_turkishCompare = new CultureInfo("tr-TR").CompareInfo;
        
        public static IEnumerable<object[]> LastIndexOf_TestData()
        {
            // Empty strings
            yield return new object[] { s_invariantCompare, "foo", "", 2, 3, CompareOptions.None, 2 };
            yield return new object[] { s_invariantCompare, "", "", 0, 0, CompareOptions.None, 0 };

            // Long strings
            yield return new object[] { s_invariantCompare, new string('a', 5555) + new string('b', 100), "aaaaaaaaaaaaaaa", 5654, 5655, CompareOptions.None, 5540 };
            yield return new object[] { s_invariantCompare, new string('b', 101) + new string('a', 5555), new string('a', 5000), 5655, 5656, CompareOptions.None, 656 };
            yield return new object[] { s_invariantCompare, new string('a', 5555), new string('a', 5000) + "b", 5554, 5555, CompareOptions.None, -1 };

            // Hungarian
            yield return new object[] { s_hungarianCompare, "foobardzsdzs", "rddzs", 11, 12, CompareOptions.Ordinal, -1 };
            yield return new object[] { s_invariantCompare, "foobardzsdzs", "rddzs", 11, 12, CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "foobardzsdzs", "rddzs", 11, 12, CompareOptions.Ordinal, -1 };

            // Turkish
            yield return new object[] { s_turkishCompare, "Hi", "I", 1, 2, CompareOptions.None, -1 };
            yield return new object[] { s_turkishCompare, "Hi", "I", 1, 2, CompareOptions.IgnoreCase, -1 };
            yield return new object[] { s_turkishCompare, "Hi", "\u0130", 1, 2, CompareOptions.None, -1 };
            yield return new object[] { s_turkishCompare, "Hi", "\u0130", 1, 2, CompareOptions.IgnoreCase, 1 };

            yield return new object[] { s_invariantCompare, "Hi", "I", 1, 2, CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "Hi", "I", 1, 2, CompareOptions.IgnoreCase, 1 };
            yield return new object[] { s_invariantCompare, "Hi", "\u0130", 1, 2, CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "Hi", "\u0130", 1, 2, CompareOptions.IgnoreCase, -1 };

            // Unicode
            yield return new object[] { s_invariantCompare, "Exhibit \u00C0", "A\u0300", 8, 9, CompareOptions.None, 8 };
            yield return new object[] { s_invariantCompare, "Exhibit \u00C0", "A\u0300", 8, 9, CompareOptions.Ordinal, -1 };
            yield return new object[] { s_invariantCompare, "Exhibit \u00C0", "a\u0300", 8, 9, CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "Exhibit \u00C0", "a\u0300", 8, 9, CompareOptions.IgnoreCase, 8 };
            yield return new object[] { s_invariantCompare, "Exhibit \u00C0", "a\u0300", 8, 9, CompareOptions.OrdinalIgnoreCase, -1 };
            yield return new object[] { s_invariantCompare, "Exhibit \u00C0", "a\u0300", 8, 9, CompareOptions.Ordinal, -1 };
            yield return new object[] { s_invariantCompare, "FooBar", "Foo\u0400Bar", 5, 6, CompareOptions.Ordinal, -1 };
            yield return new object[] { s_invariantCompare, "TestFooBA\u0300R", "FooB\u00C0R", 10, 11, CompareOptions.IgnoreNonSpace, 4 };

            // Ignore symbols
            yield return new object[] { s_invariantCompare, "More Test's", "Tests", 10, 11, CompareOptions.IgnoreSymbols, 5 };
            yield return new object[] { s_invariantCompare, "More Test's", "Tests", 10, 11, CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "cbabababdbaba", "ab", 12, 13, CompareOptions.None, 10 };
        }

        public static IEnumerable<object[]> LastIndexOf_Aesc_Ligature_TestData()
        {
            // Searches for the ligature Æ;
            string source = "Is AE or ae the same as \u00C6 or \u00E6?";
            yield return new object[] { s_invariantCompare, source, "AE", 25, 18, CompareOptions.None, 24 };
            yield return new object[] { s_invariantCompare, source, "ae", 25, 18, CompareOptions.None, 9 };
            yield return new object[] { s_invariantCompare, source, '\u00C6', 25, 18, CompareOptions.None, 24 };
            yield return new object[] { s_invariantCompare, source, '\u00E6', 25, 18, CompareOptions.None, 9 };
            yield return new object[] { s_invariantCompare, source, "AE", 25, 18, CompareOptions.Ordinal, -1 };
            yield return new object[] { s_invariantCompare, source, "ae", 25, 18, CompareOptions.Ordinal, 9 };
            yield return new object[] { s_invariantCompare, source, '\u00C6', 25, 18, CompareOptions.Ordinal, 24 };
            yield return new object[] { s_invariantCompare, source, '\u00E6', 25, 18, CompareOptions.Ordinal, -1 };
            yield return new object[] { s_invariantCompare, source, "AE", 25, 18, CompareOptions.IgnoreCase, 24 };
            yield return new object[] { s_invariantCompare, source, "ae", 25, 18, CompareOptions.IgnoreCase, 24 };
            yield return new object[] { s_invariantCompare, source, '\u00C6', 25, 18, CompareOptions.IgnoreCase, 24 };
            yield return new object[] { s_invariantCompare, source, '\u00E6', 25, 18, CompareOptions.IgnoreCase, 24 };
        }

        public static IEnumerable<object[]> LastIndexOf_U_WithDiaeresis_TestData()
        {
            // Searches for the combining character sequence Latin capital letter U with diaeresis or Latin small letter u with diaeresis.
            string source = "Is \u0055\u0308 or \u0075\u0308 the same as \u00DC or \u00FC?";
            yield return new object[] { s_invariantCompare, source, "U\u0308", 25, 18, CompareOptions.None, 24 };
            yield return new object[] { s_invariantCompare, source, "u\u0308", 25, 18, CompareOptions.None, 9 };
            yield return new object[] { s_invariantCompare, source, '\u00DC', 25, 18, CompareOptions.None, 24 };
            yield return new object[] { s_invariantCompare, source, '\u00FC', 25, 18, CompareOptions.None, 9 };
            yield return new object[] { s_invariantCompare, source, "U\u0308", 25, 18, CompareOptions.Ordinal, -1 };
            yield return new object[] { s_invariantCompare, source, "u\u0308", 25, 18, CompareOptions.Ordinal, 9 };
            yield return new object[] { s_invariantCompare, source, '\u00DC', 25, 18, CompareOptions.Ordinal, 24 };
            yield return new object[] { s_invariantCompare, source, '\u00FC', 25, 18, CompareOptions.Ordinal, -1 };
            yield return new object[] { s_invariantCompare, source, "U\u0308", 25, 18, CompareOptions.IgnoreCase, 24 };
            yield return new object[] { s_invariantCompare, source, "u\u0308", 25, 18, CompareOptions.IgnoreCase, 24 };
            yield return new object[] { s_invariantCompare, source, '\u00DC', 25, 18, CompareOptions.IgnoreCase, 24 };
            yield return new object[] { s_invariantCompare, source, '\u00FC', 25, 18, CompareOptions.IgnoreCase, 24 };
        }

        [Theory]
        [MemberData("LastIndexOf_TestData")]
        [MemberData("LastIndexOf_U_WithDiaeresis_TestData")]
        public void LastIndexOf_String(CompareInfo compareInfo, string source, string value, int startIndex, int count, CompareOptions options, int expected)
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
        }

        public void LastIndexOf_Char(CompareInfo compareInfo, string source, char value, int startIndex, int count, CompareOptions options, int expected)
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
        [MemberData("LastIndexOf_Aesc_Ligature_TestData")]
        [ActiveIssue(5463, PlatformID.AnyUnix)]
        public void LastIndexOf_Aesc_Ligature(CompareInfo compareInfo, string source, string value, int startIndex, int count, CompareOptions options, int expected)
        {
            // TODO: Remove this function, and combine into LastIndexOf_String once 5463 is fixed
            LastIndexOf_String(compareInfo, source, value, startIndex, count, options, expected);
        }

        [Fact]
        [ActiveIssue(5463, PlatformID.AnyUnix)]
        public void LastIndexOf_UnassignedUnicode()
        {
            LastIndexOf_String(s_invariantCompare, "FooBar", "Foo" + UnassignedUnicodeCharacter() + "Bar", 5, 6, CompareOptions.None, 0);
            LastIndexOf_String(s_invariantCompare, "~FooBar", "Foo" + UnassignedUnicodeCharacter() + "Bar", 6, 7, CompareOptions.IgnoreNonSpace, 1);
        }

        [Fact]
        [ActiveIssue(5463, PlatformID.AnyUnix)]
        public void LastIndexOf_Hungarian()
        {
            // TODO: Remove this function, and combine into LastIndexOf_TestData once 5463 is fixed
            LastIndexOf_String(s_hungarianCompare, "foobardzsdzs", "rddzs", 11, 12, CompareOptions.None, 5);
        }

        [Fact]
        public void LastIndexOf_Invalid()
        {
            // Source is null
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.LastIndexOf(null, "a"));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.LastIndexOf(null, "a", CompareOptions.None));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.LastIndexOf(null, "a", 0, 0));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.LastIndexOf(null, "a", 0, CompareOptions.None));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.LastIndexOf(null, "a", 0, 0, CompareOptions.None));

            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.LastIndexOf(null, 'a'));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.LastIndexOf(null, 'a', CompareOptions.None));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.LastIndexOf(null, 'a', 0, 0));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.LastIndexOf(null, 'a', 0, CompareOptions.None));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.LastIndexOf(null, 'a', 0, 0, CompareOptions.None));

            // Value is null
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.LastIndexOf("", null));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.LastIndexOf("", null, CompareOptions.None));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.LastIndexOf("", null, 0, 0));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.LastIndexOf("", null, 0, CompareOptions.None));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.LastIndexOf("", null, 0, 0, CompareOptions.None));

            // Source and value are null
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.LastIndexOf(null, null));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.LastIndexOf(null, null, CompareOptions.None));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.LastIndexOf(null, null, 0, 0));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.LastIndexOf(null, null, 0, CompareOptions.None));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.LastIndexOf(null, null, 0, 0, CompareOptions.None));

            // Options are invalid
            Assert.Throws<ArgumentException>(() => s_invariantCompare.LastIndexOf("Test's", "Tests", CompareOptions.StringSort));
            Assert.Throws<ArgumentException>(() => s_invariantCompare.LastIndexOf("Test's", "Tests", (CompareOptions)(-1)));
            Assert.Throws<ArgumentException>(() => s_invariantCompare.LastIndexOf("Test's", "Tests", (CompareOptions)0x11111111));
        }

        private static char UnassignedUnicodeCharacter()
        {
            for (char ch = '\uFFFF'; ch > '\u0000'; ch++)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.OtherNotAssigned)
                {
                    return ch;
                }
            }
            return char.MinValue; // There are no unassigned unicode characters from \u0000 - \uFFFF
        }
    }
}
