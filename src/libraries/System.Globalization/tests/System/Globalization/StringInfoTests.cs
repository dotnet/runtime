// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class StringInfoMiscTests
    {
        [Fact]
        public void Ctor_Default()
        {
            StringInfo stringInfo = new StringInfo();
            Assert.Equal(string.Empty, stringInfo.String);
            Assert.Equal(0, stringInfo.LengthInTextElements);
        }

        public static IEnumerable<object[]> Ctor_String_TestData()
        {
            yield return new object[] { new string('a', 256), 256 };
            yield return new object[] { "\u4f00\u302a\ud800\udc00\u4f01", 3 };
            yield return new object[] { "abcdefgh", 8 };
            yield return new object[] { "zj\uDBFF\uDFFFlk", 5 };
            yield return new object[] { "!@#$%^&", 7 };
            yield return new object[] { "!\u20D1bo\uFE22\u20D1\u20EB|", 4 };
            yield return new object[] { "1\uDBFF\uDFFF@\uFE22\u20D1\u20EB9", 4 };
            yield return new object[] { "a\u0300", 1 };
            yield return new object[] { "   ", 3 };
            yield return new object[] { "", 0 };
        }

        [Theory]
        [MemberData(nameof(Ctor_String_TestData))]
        public void Ctor_String(string value, int lengthInTextElements)
        {
            var stringInfo = new StringInfo(value);
            Assert.Same(value, stringInfo.String);
            Assert.Equal(lengthInTextElements, stringInfo.LengthInTextElements);
        }

        [Fact]
        public void Ctor_NullValue_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", "String", () => new StringInfo(null));
        }

        [Theory]
        [InlineData("")]
        [InlineData("abc")]
        [InlineData("\uD800\uDC00")]
        public void String_Set_GetReturnsExpected(string value)
        {
            StringInfo stringInfo = new StringInfo();
            stringInfo.String = value;
            Assert.Same(value, stringInfo.String);
        }

        [Fact]
        public void String_SetNull_ThrowsArgumentNullException()
        {
            var stringInfo = new StringInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", "String", () => stringInfo.String = null);
        }

        public static IEnumerable<object[]> StringInfo_TestData()
        {
            yield return new object[] { "Simple Text", 7, "Text", 4, "Text" };
            yield return new object[] { "Simple Text", 0, "Simple Text", 6, "Simple" };
            yield return new object[] { "\uD800\uDC00\uD801\uDC01Left", 2, "Left", 2, "Le" };
            yield return new object[] { "\uD800\uDC00\uD801\uDC01Left", 1, "\uD801\uDC01Left", 2, "\uD801\uDC01L" };
            yield return new object[] { "Start\uD800\uDC00\uD801\uDC01Left", 5, "\uD800\uDC00\uD801\uDC01Left", 1, "\uD800\uDC00" };
        }

        [Theory]
        [MemberData(nameof(StringInfo_TestData))]
        public void SubstringTest(string source, int index, string expected, int length, string expectedWithLength)
        {
            StringInfo si = new StringInfo(source);
            Assert.Equal(expected, si.SubstringByTextElements(index));
            Assert.Equal(expectedWithLength, si.SubstringByTextElements(index, length));
        }

        [Fact]
        public void NegativeTest()
        {
            string s = "Some String";
            StringInfo si = new StringInfo(s);
            StringInfo siEmpty = new StringInfo("");

            Assert.Throws<ArgumentOutOfRangeException>(() => si.SubstringByTextElements(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => si.SubstringByTextElements(s.Length + 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => siEmpty.SubstringByTextElements(0));

            Assert.Throws<ArgumentOutOfRangeException>(() => si.SubstringByTextElements(-1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => si.SubstringByTextElements(s.Length + 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => siEmpty.SubstringByTextElements(0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => si.SubstringByTextElements(0, s.Length + 1));
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new StringInfo(), new StringInfo(), true };
            yield return new object[] { new StringInfo("stringinfo1"), new StringInfo("stringinfo1"), true };
            yield return new object[] { new StringInfo("stringinfo1"), new StringInfo("stringinfo2"), false };
            yield return new object[] { new StringInfo("stringinfo1"), "stringinfo1", false };
            yield return new object[] { new StringInfo("stringinfo1"), 123, false };
            yield return new object[] { new StringInfo("stringinfo1"), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void EqualsTest(StringInfo stringInfo, object value, bool expected)
        {
            Assert.Equal(expected, stringInfo.Equals(value));
            if (value is StringInfo)
            {
                Assert.Equal(expected, stringInfo.GetHashCode().Equals(value.GetHashCode()));
            }
        }

        public static IEnumerable<object[]> GetNextTextElement_TestData()
        {
            yield return new object[] { "", 0, "" }; // Empty string
            yield return new object[] { "Hello", 5, "" }; // Index = string.Length

            // Surrogate pair
            yield return new object[] { "\uDBFF\uDFFFabcde", 0, "\uDBFF\uDFFF" };
            yield return new object[] { "ef45-;\uDBFF\uDFFFabcde", 6, "\uDBFF\uDFFF" };

            yield return new object[] { "a\u20D1abcde", 0, "a\u20D1" }; // Combining character or non spacing mark

            // Base character with several combining characters
            yield return new object[] { "z\uFE22\u20D1\u20EBabcde", 0, "z\uFE22\u20D1\u20EB" };
            yield return new object[] { "az\uFE22\u20D1\u20EBabcde", 1, "z\uFE22\u20D1\u20EB" };

            yield return new object[] { "13229^a\u20D1abcde", 6, "a\u20D1" }; // Combining characters

            // Single base and combining character
            yield return new object[] { "a\u0300", 0, "a\u0300" };
            yield return new object[] { "a\u0300", 1, "\u0300" };

            // Lone combining character
            yield return new object[] { "\u0300\u0300", 0, "\u0300\u0300" }; // second U+0300 extends first U+0300
            yield return new object[] { "\u0300\u0300", 1, "\u0300" };
        }

        [Theory]
        [MemberData(nameof(GetNextTextElement_TestData))]
        public void GetNextTextElement(string str, int index, string expected)
        {
            if (index == 0)
            {
                Assert.Equal(expected, StringInfo.GetNextTextElement(str));
            }
            Assert.Equal(expected, StringInfo.GetNextTextElement(str, index));
        }

        [Fact]
        public void GetNextTextElement_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("str", () => StringInfo.GetNextTextElement(null)); // Str is null
            AssertExtensions.Throws<ArgumentNullException>("str", () => StringInfo.GetNextTextElement(null, 0)); // Str is null

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => StringInfo.GetNextTextElement("abc", -1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => StringInfo.GetNextTextElement("abc", 4)); // Index > str.Length
        }

        [Theory]
        [MemberData(nameof(GetNextTextElement_TestData))]
        public void GetNextTextElementLength(string str, int index, string expected)
        {
            if (index == 0)
            {
                Assert.Equal(expected.Length, StringInfo.GetNextTextElementLength(str));
            }
            Assert.Equal(expected.Length, StringInfo.GetNextTextElementLength(str, index));
            Assert.Equal(expected.Length, StringInfo.GetNextTextElementLength(str.AsSpan(index)));
        }

        [Fact]
        public void GetNextTextElementLength_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("str", () => StringInfo.GetNextTextElementLength(null)); // Str is null
            AssertExtensions.Throws<ArgumentNullException>("str", () => StringInfo.GetNextTextElementLength(null, 0)); // Str is null

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => StringInfo.GetNextTextElementLength("abc", -1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => StringInfo.GetNextTextElementLength("abc", 4)); // Index > str.Length
        }

        public static IEnumerable<object[]> GetTextElementEnumerator_TestData()
        {
            yield return new object[] { "", 0, new string[0] }; // Empty string
            yield return new object[] { "Hello", 5, new string[0] }; // Index = string.Length

            // Surrogate pair
            yield return new object[] { "s\uDBFF\uDFFF$", 0, new string[] { "s", "\uDBFF\uDFFF", "$" } };
            yield return new object[] { "s\uDBFF\uDFFF$", 1, new string[] { "\uDBFF\uDFFF", "$" } };

            // Combining characters
            yield return new object[] { "13229^a\u20D1a", 6, new string[] { "a\u20D1", "a" } };
            yield return new object[] { "13229^a\u20D1a", 0, new string[] { "1", "3", "2", "2", "9", "^", "a\u20D1", "a" } };

            // Single base and combining character
            yield return new object[] { "a\u0300", 0, new string[] { "a\u0300" } };
            yield return new object[] { "a\u0300", 1, new string[] { "\u0300" } };

            // Extending chars can extend other extending chars
            yield return new object[] { "\u0300\u0300", 0, new string[] { "\u0300\u0300" } };
        }

        [Theory]
        [MemberData(nameof(GetTextElementEnumerator_TestData))]
        public void GetTextElementEnumerator(string str, int index, string[] expected)
        {
            if (index == 0)
            {
                TextElementEnumerator basicEnumerator = StringInfo.GetTextElementEnumerator(str);
                int basicCounter = 0;
                while (basicEnumerator.MoveNext())
                {
                    Assert.Equal(expected[basicCounter], basicEnumerator.Current.ToString());
                    basicCounter++;
                }
                Assert.Equal(expected.Length, basicCounter);
            }
            TextElementEnumerator indexedEnumerator = StringInfo.GetTextElementEnumerator(str, index);
            int indexedCounter = 0;
            while (indexedEnumerator.MoveNext())
            {
                Assert.Equal(expected[indexedCounter], indexedEnumerator.Current.ToString());
                indexedCounter++;
            }
            Assert.Equal(expected.Length, indexedCounter);
        }

        [Fact]
        public void GetTextElementEnumerator_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("str", () => StringInfo.GetTextElementEnumerator(null)); // Str is null
            AssertExtensions.Throws<ArgumentNullException>("str", () => StringInfo.GetTextElementEnumerator(null, 0)); // Str is null

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => StringInfo.GetTextElementEnumerator("abc", -1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => StringInfo.GetTextElementEnumerator("abc", 4)); // Index > str.Length
        }

        public static IEnumerable<object[]> ParseCombiningCharacters_TestData()
        {
            // When "rules" are mentioned in this function, they refer to:
            // https://www.unicode.org/reports/tr29/#Grapheme_Cluster_Boundary_Rules

            //                           ,-- Other (U+4F00 CJK UNIFIED IDEOGRAPH-4F00)
            //                           |     ,-- Extend (U+302A IDEOGRAPHIC LEVEL TONE MARK)
            //                           |     |     ,-- Other (U+10000 LINEAR B SYLLABLE B008 A)
            //                           |     |     |           ,-- Other (U+4F01 CJK UNIFIED IDEOGRAPH-4F01)
            yield return new object[] { "\u4f00\u302a\ud800\udc00\u4f01", new int[] { 0, 2, 4 } };

            yield return new object[] { "abcdefgh", new int[] { 0, 1, 2, 3, 4, 5, 6, 7 } };
            yield return new object[] { "!@#$%^&", new int[] { 0, 1, 2, 3, 4, 5, 6 } };

            //                            ,-- Extend (U+20D1 COMBINING RIGHT HARPOON ABOVE)
            //                            |       ,-- Extend (U+FE22 COMBINING DOUBLE TILDE LEFT HALF)
            //                            |       |     ,-- Extend (U+20D1 COMBINING RIGHT HARPOON ABOVE)
            //                            |       |     |     ,-- Extend (U+20EB COMBINING LONG DOUBLE SOLIDUS OVERLAY)
            yield return new object[] { "!\u20D1bo\uFE22\u20D1\u20EB|", new int[] { 0, 2, 3, 7 } };

            //                            ,-- Other (U+10FFFF <Unassigned>)
            yield return new object[] { "1\uDBFF\uDFFF@\uFE22\u20D1\u20EB9", new int[] { 0, 1, 3, 7 } };

            //                            ,-- Extend (U+0300 COMBINING GRAVE ACCENT)
            yield return new object[] { "a\u0300", new int[] { 0 } };
            yield return new object[] { "\u0300\u0300", new int[] { 0 } }; // second U+0300 extends first U+0300

            yield return new object[] { "   ", new int[] { 0, 1, 2 } };
            yield return new object[] { "", new int[0] };

            // Control characters (such as U+0000) should never be followable by another character (rule GB4).
            // (CR and LF are treated specially and will be handled later.)

            yield return new object[] { "\u0000\u0000\u0000", new int[] { 0, 1, 2 } };
            yield return new object[] { "\u0000\u0300\u0300", new int[] { 0, 1 } }; // first U+0300 does not extend the U+0000, but second U+0300 extends first U+0300

            // Prepend characters (like U+0600 ARABIC NUMBER SIGN) may combine with non-control characters,
            // but never with control characters (rule GB5).

            yield return new object[] { "\u0600a", new int[] { 0 } };
            yield return new object[] { "\u0600\r", new int[] { 0, 1 } };
            yield return new object[] { "\u0600\u0000", new int[] { 0, 1 } };

            // Nothing can come after CR (except LF), and nothing can come after LF (rules GB3, GB4).

            yield return new object[] { "\t\r\n", new int[] { 0, 1 } };
            yield return new object[] { "\t\r\r\n", new int[] { 0, 1, 2 } };
            yield return new object[] { "\r\u0300\r\n\u0300", new int[] { 0, 1, 2, 4 } };

            // Now test complex emoji and symbols

            // (woman dancing; medium skin tone)
            //                           ,-- Extended_Pictographic (U+1F483 DANCER)
            //                           |         ,-- Extend (U+1F3FD EMOJI MODIFIER FITZPATRICK TYPE-4)
            yield return new object[] { "\U0001F483\U0001F3FD", new int[] { 0 } };

            // (flag: Antarctica)
            //                           ,-- Regional_Indicator (U+1F1E6 REGIONAL INDICATOR SYMBOL LETTER A)
            //                           |         ,-- Regional_Indicator (U+1F1F6 REGIONAL INDICATOR SYMBOL LETTER Q)
            yield return new object[] { "\U0001F1E6\U0001F1F6", new int[] { 0 } };

            // (man golfing)
            //                           ,-- Extended_Pictographic (U+1F3CC GOLFER)
            //                           |         ,-- Extend (U+FE0F VARIATION SELECTOR-16)
            //                           |         |     ,-- ZWJ (U+200D ZERO WIDTH JOINER)
            //                           |         |     |     ,-- Extended_Pictographic (U+2642 MALE SIGN)
            yield return new object[] { "\U0001F3CC\uFE0F\u200D\u2642\uFE0F", new int[] { 0 } };
        }

        [Theory]
        [MemberData(nameof(ParseCombiningCharacters_TestData))]
        public void ParseCombiningCharacters(string str, int[] expected)
        {
            Assert.Equal(expected, StringInfo.ParseCombiningCharacters(str));
        }

        [Fact]
        public void ParseCombiningChars_UnpairedSurrogates()
        {
            // Unpaired surrogates should be treated as U+FFFD REPLACEMENT CHAR ("Other"),
            // so prepend and extend can affect them. We can't use [Theory] because the
            // unit test runner may corrupt malformed UTF-16 constant data.

            ParseCombiningCharacters("\u0600\uD800", new int[] { 0 }); // prepend + unpaired surrogate
            ParseCombiningCharacters("\udfff\ud800\u0300", new int[] { 0, 1 }); // unpaired | unpaired + extender
            ParseCombiningCharacters("\udfff\ud800", new int[] { 0, 1 }); // unpaired | unpaired
        }

        [Fact]
        public void ParseCombiningChars_LargeStrings()
        {
            string s = "\u0600x" /* prepend + 'x' */ + new string('\u0300' /* extend */, 9_999_998);
            s = string.Concat(s, s, s, s); // 40 million characters

            ParseCombiningCharacters(s, new int[] { 0, 10_000_000, 20_000_000, 30_000_000 });
        }

        [Fact]
        public void ParseCombiningCharacters_Null_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("str", () => StringInfo.ParseCombiningCharacters(null)); // Str is null
        }
    }
}
