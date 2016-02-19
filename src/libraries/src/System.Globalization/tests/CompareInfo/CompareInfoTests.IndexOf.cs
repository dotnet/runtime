// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

using Xunit;

namespace System.Globalization.Tests
{
    public class CompareInfoIndexOfTests
    {
        private static CompareInfo s_invariantCompare = CultureInfo.InvariantCulture.CompareInfo;
        private static CompareInfo s_currentCompare = CultureInfo.CurrentCulture.CompareInfo;
        private static CompareInfo s_hungarianCompare = new CultureInfo("hu-HU").CompareInfo;
        private static CompareInfo s_turkishCompare = new CultureInfo("tr-TR").CompareInfo;

        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();
        
        public static IEnumerable<object[]> IndexOf_TestData()
        {
            // Empty string
            yield return new object[] { s_invariantCompare, "foo", "", 0, 3, CompareOptions.None, 0 };
            yield return new object[] { s_invariantCompare, "", "", 0, 0, CompareOptions.None, 0 };

            // Long strings
            yield return new object[] { s_invariantCompare, new string('b', 100) + new string('a', 5555), "aaaaaaaaaaaaaaa", 0, 5655, CompareOptions.None, 100 };
            yield return new object[] { s_invariantCompare, new string('b', 101) + new string('a', 5555), new string('a', 5000), 0, 5656, CompareOptions.None, 101 };
            yield return new object[] { s_invariantCompare, new string('a', 5555), new string('a', 5000) + "b", 0, 5555, CompareOptions.None, -1 };

            // Hungarian
            yield return new object[] { s_hungarianCompare, "foobardzsdzs", "rddzs", 0, 12, CompareOptions.Ordinal, -1 };
            yield return new object[] { s_invariantCompare, "foobardzsdzs", "rddzs", 0, 12, CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "foobardzsdzs", "rddzs", 0, 12, CompareOptions.Ordinal, -1 };

            // Turkish
            yield return new object[] { s_turkishCompare, "Hi", "I", 0, 2, CompareOptions.None, -1 };
            yield return new object[] { s_turkishCompare, "Hi", "I", 0, 2, CompareOptions.IgnoreCase, -1 };
            yield return new object[] { s_turkishCompare, "Hi", "\u0130", 0, 2, CompareOptions.None, -1 };
            yield return new object[] { s_turkishCompare, "Hi", "\u0130", 0, 2, CompareOptions.IgnoreCase, 1 };
            yield return new object[] { s_invariantCompare, "Hi", "I", 0, 2, CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "Hi", "I", 0, 2, CompareOptions.IgnoreCase, 1 };
            yield return new object[] { s_invariantCompare, "Hi", "\u0130", 0, 2, CompareOptions.IgnoreCase, -1 };

            // Unicode
            yield return new object[] { s_invariantCompare, "Hi", "\u0130", 0, 2, CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "Exhibit \u00C0", "A\u0300", 0, 9, CompareOptions.None, 8 };
            yield return new object[] { s_invariantCompare, "Exhibit \u00C0", "A\u0300", 0, 9, CompareOptions.Ordinal, -1 };
            yield return new object[] { s_invariantCompare, "Exhibit \u00C0", "a\u0300", 0, 9, CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "Exhibit \u00C0", "a\u0300", 0, 9, CompareOptions.Ordinal, -1 };
            yield return new object[] { s_invariantCompare, "Exhibit \u00C0", "a\u0300", 0, 9, CompareOptions.IgnoreCase, 8 };
            yield return new object[] { s_invariantCompare, "Exhibit \u00C0", "a\u0300", 0, 9, CompareOptions.OrdinalIgnoreCase, -1 };
            yield return new object[] { s_invariantCompare, "FooBar", "Foo\u0400Bar", 0, 6, CompareOptions.Ordinal, -1 };
            yield return new object[] { s_invariantCompare, "TestFooBA\u0300R", "FooB\u00C0R", 0, 11, CompareOptions.IgnoreNonSpace, 4 };

            // Ignore symbols
            yield return new object[] { s_invariantCompare, "More Test's", "Tests", 0, 11, CompareOptions.IgnoreSymbols, 5 };
            yield return new object[] { s_invariantCompare, "More Test's", "Tests", 0, 11, CompareOptions.None, -1 };
            yield return new object[] { s_invariantCompare, "cbabababdbaba", "ab", 0, 13, CompareOptions.None, 2 };
        }

        public static IEnumerable<object[]> IndexOf_Random_TestData()
        {
            string[] interestingStrings = new string[] { "", "a", "1", "-", "A", "!", "abc", "aBc", "a\u0400Bc", "I", "i", "\u0130", "\u0131", "A", "\uFF21", "\uFE57" };
            foreach (string string1 in interestingStrings)
            {
                foreach (string string2 in interestingStrings)
                {
                    yield return new object[] { s_currentCompare, string1, string2, 0, string1.Length, CompareOptions.Ordinal, PredictIndexOfOrdinalResult(string1, string2) };
                }
            }

            // Random
            for (int i = 0; i < 1000; i++)
            {
                string string1 = s_randomDataGenerator.GetString(-55, false, 5, 20);
                string string2 = s_randomDataGenerator.GetString(-55, false, 5, 20);
                string string3 = string1 + string2;
                yield return new object[] { s_currentCompare, string1, string1, 0, string1.Length, CompareOptions.Ordinal, 0 };
                yield return new object[] { s_currentCompare, string2, string2, 0, string2.Length, CompareOptions.Ordinal, 0 };
                yield return new object[] { s_currentCompare, string1, string2, 0, string1.Length, CompareOptions.Ordinal, PredictIndexOfOrdinalResult(string1, string2) };
                yield return new object[] { s_currentCompare, string3, string2, 0, string3.Length, CompareOptions.Ordinal, PredictIndexOfOrdinalResult(string3, string2) };
            }
        }

        public static IEnumerable<object[]> IndexOf_Aesc_Ligature_TestData()
        {
            // Searches for the ligature Æ;
            string source1 = "Is AE or ae the same as \u00C6 or \u00E6?";
            yield return new object[] { s_invariantCompare, source1, "AE", 8, 18, CompareOptions.None, 24 };
            yield return new object[] { s_invariantCompare, source1, "ae", 8, 18, CompareOptions.None, 9 };
            yield return new object[] { s_invariantCompare, source1, "\u00C6", 8, 18, CompareOptions.None, 24 };
            yield return new object[] { s_invariantCompare, source1, "\u00E6", 8, 18, CompareOptions.None, 9 };
            yield return new object[] { s_invariantCompare, source1, "AE", 8, 18, CompareOptions.Ordinal, -1 };
            yield return new object[] { s_invariantCompare, source1, "ae", 8, 18, CompareOptions.Ordinal, 9 };
            yield return new object[] { s_invariantCompare, source1, "\u00C6", 8, 18, CompareOptions.Ordinal, 24 };
            yield return new object[] { s_invariantCompare, source1, "\u00E6", 8, 18, CompareOptions.Ordinal, -1 };
            yield return new object[] { s_invariantCompare, source1, "AE", 8, 18, CompareOptions.IgnoreCase, 9 };
            yield return new object[] { s_invariantCompare, source1, "ae", 8, 18, CompareOptions.IgnoreCase, 9 };
            yield return new object[] { s_invariantCompare, source1, "\u00C6", 8, 18, CompareOptions.IgnoreCase, 9 };
            yield return new object[] { s_invariantCompare, source1, "\u00E6", 8, 18, CompareOptions.IgnoreCase, 9 };
        }

        public static IEnumerable<object[]> IndexOf_U_WithDiaeresis_TestData()
        {
            // Searches for the combining character sequence Latin capital letter U with diaeresis or Latin small letter u with diaeresis.
            string source = "Is \u0055\u0308 or \u0075\u0308 the same as \u00DC or \u00FC?";
            yield return new object[] { s_invariantCompare, source, "U\u0308", 8, 18, CompareOptions.None, 24 };
            yield return new object[] { s_invariantCompare, source, "u\u0308", 8, 18, CompareOptions.None, 9 };
            yield return new object[] { s_invariantCompare, source, "\u00DC", 8, 18, CompareOptions.None, 24 };
            yield return new object[] { s_invariantCompare, source, "\u00FC", 8, 18, CompareOptions.None, 9 };
            yield return new object[] { s_invariantCompare, source, "U\u0308", 8, 18, CompareOptions.Ordinal, -1 };
            yield return new object[] { s_invariantCompare, source, "u\u0308", 8, 18, CompareOptions.Ordinal, 9 };
            yield return new object[] { s_invariantCompare, source, "\u00DC", 8, 18, CompareOptions.Ordinal, 24 };
            yield return new object[] { s_invariantCompare, source, "\u00FC", 8, 18, CompareOptions.Ordinal, -1 };
            yield return new object[] { s_invariantCompare, source, "U\u0308", 8, 18, CompareOptions.IgnoreCase, 9 };
            yield return new object[] { s_invariantCompare, source, "u\u0308", 8, 18, CompareOptions.IgnoreCase, 9 };
            yield return new object[] { s_invariantCompare, source, "\u00DC", 8, 18, CompareOptions.IgnoreCase, 9 };
            yield return new object[] { s_invariantCompare, source, "\u00FC", 8, 18, CompareOptions.IgnoreCase, 9 };
        }

        [Theory]
        [MemberData("IndexOf_TestData")]
        [MemberData("IndexOf_Random_TestData")]
        [MemberData("IndexOf_U_WithDiaeresis_TestData")]
        public void IndexOf_String(CompareInfo compareInfo, string source, string value, int startIndex, int count, CompareOptions options, int expected)
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
        }

        public void IndexOf_Char(CompareInfo compareInfo, string source, char value, int startIndex, int count, CompareOptions options, int expected)
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

        [Theory]
        [MemberData("IndexOf_Aesc_Ligature_TestData")]
        [ActiveIssue(5463, PlatformID.AnyUnix)]
        public void IndexOf_Aesc_Ligature(CompareInfo compareInfo, string source, string value, int startIndex, int count, CompareOptions options, int expected)
        {
            // TODO: Remove this function, and combine into IndexOf_String once 5463 is fixed
            IndexOf_String(compareInfo, source, value, startIndex, count, options, expected);
        }

        [Fact]
        [ActiveIssue(5463, PlatformID.AnyUnix)]
        public void IndexOf_UnassignedUnicode()
        {
            IndexOf_String(s_invariantCompare, "FooBar", "Foo" + UnassignedUnicodeCharacter() + "Bar", 0, 6, CompareOptions.None, 0);
            IndexOf_String(s_invariantCompare, "~FooBar", "Foo" + UnassignedUnicodeCharacter() + "Bar", 0, 7, CompareOptions.IgnoreNonSpace, 1);
        }

        [Fact]
        [ActiveIssue(5463, PlatformID.AnyUnix)]
        public void IndexOf_Hungarian()
        {
            // TODO: Remove this function, and combine into IndexOf_TestData once 5463 is fixed
            IndexOf_String(s_hungarianCompare, "foobardzsdzs", "rddzs", 0, 12, CompareOptions.None, 5);
        }

        [Fact]
        public void IndexOf_Invalid()
        {
            // Source is null
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.IndexOf(null, "a"));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.IndexOf(null, "a", CompareOptions.None));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.IndexOf(null, "a", 0, 0));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.IndexOf(null, "a", 0, CompareOptions.None));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.IndexOf(null, "a", 0, 0, CompareOptions.None));

            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.IndexOf(null, 'a'));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.IndexOf(null, 'a', CompareOptions.None));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.IndexOf(null, 'a', 0, 0));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.IndexOf(null, 'a', 0, CompareOptions.None));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.IndexOf(null, 'a', 0, 0, CompareOptions.None));

            // Value is null
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.IndexOf("", null));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.IndexOf("", null, CompareOptions.None));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.IndexOf("", null, 0, 0));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.IndexOf("", null, 0, CompareOptions.None));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.IndexOf("", null, 0, 0, CompareOptions.None));

            // Source and value are null
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.IndexOf(null, null));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.IndexOf(null, null, CompareOptions.None));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.IndexOf(null, null, 0, 0));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.IndexOf(null, null, 0, CompareOptions.None));
            Assert.Throws<ArgumentNullException>(() => s_invariantCompare.IndexOf(null, null, 0, 0, CompareOptions.None));

            // Options are invalid
            Assert.Throws<ArgumentException>(() => s_invariantCompare.IndexOf("Test's", "Tests", CompareOptions.StringSort));
            Assert.Throws<ArgumentException>(() => s_invariantCompare.IndexOf("Test's", "Tests", (CompareOptions)(-1)));
            Assert.Throws<ArgumentException>(() => s_invariantCompare.IndexOf("Test's", "Tests", (CompareOptions)0x11111111));
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

        private static int PredictIndexOfOrdinalResult(string string1, string string2)
        {
            if (string1 == null)
            {
                if (string2 == null) return 0;
                else return -1;
            }
            if (string2 == null) return -1;
 
            if (string2.Length > string1.Length) return -1;
 
            for (int i = 0; i <= string1.Length - string2.Length; i++)
            {
                bool match = true;
                for (int j = 0; j<string2.Length; j++)
                {
                    if (string1[i + j] != string2[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return i;
            }
            return -1;
        }

        private static int NormalizeCompare(int result)
        {
            return Math.Sign(result);
        }
    }
}
