// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Theory]
        [InlineData("Hello", 'l', 0, 5, 2)]
        [InlineData("Hello", 'x', 0, 5, -1)]
        [InlineData("Hello", 'l', 1, 4, 2)]
        [InlineData("Hello", 'l', 3, 2, 3)]
        [InlineData("Hello", 'l', 4, 1, -1)]
        [InlineData("Hello", 'x', 1, 4, -1)]
        [InlineData("Hello", 'l', 3, 0, -1)]
        [InlineData("Hello", 'l', 0, 2, -1)]
        [InlineData("Hello", 'l', 0, 3, 2)]
        [InlineData("Hello", 'l', 4, 1, -1)]
        [InlineData("Hello", 'x', 1, 4, -1)]
        [InlineData("Hello", 'o', 5, 0, -1)]
        [InlineData("H" + SoftHyphen + "ello", 'e', 0, 3, 2)]
        [InlineData("\ud800\udfff", '\ud800', 0, 1, 0)] // Surrogate characters
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 'A', 0, 26, 0)]
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 'B', 1, 25, 1)]
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 'C', 2, 24, 2)]
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 'D', 3, 23, 3)]
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 'G', 2, 24, 6)]
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 'K', 2, 24, 10)]
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 'O', 2, 24, 14)]
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 'P', 2, 24, 15)]
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 'Q', 2, 24, 16)]
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 'R', 2, 24, 17)]
        [InlineData("________\u8080\u8080\u8080________", '\u0080', 0, 19, -1)]
        [InlineData("________\u8000\u8000\u8000________", '\u0080', 0, 19, -1)]
        [InlineData("__\u8080\u8000\u0080______________", '\u0080', 0, 19, 4)]
        [InlineData("__\u8080\u8000__\u0080____________", '\u0080', 0, 19, 6)]
        [InlineData("__________________________________", '\ufffd', 0, 34, -1)]
        [InlineData("____________________________\ufffd", '\ufffd', 0, 29, 28)]
        [InlineData("ABCDEFGHIJKLM", 'M', 0, 13, 12)]
        [InlineData("ABCDEFGHIJKLMN", 'N', 0, 14, 13)]
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", '@', 0, 26, -1)]
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXY", '@', 0, 25, -1)]
        [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ#", '@', 0, 27, -1)]
        [InlineData("_____________\u807f", '\u007f', 0, 14, -1)]
        [InlineData("_____________\u807f__", '\u007f', 0, 16, -1)]
        [InlineData("_____________\u807f\u007f_", '\u007f', 0, 16, 14)]
        [InlineData("__\u807f_______________", '\u007f', 0, 18, -1)]
        [InlineData("__\u807f___\u007f___________", '\u007f', 0, 18, 6)]
        [InlineData("ABCDEFGHIJKLMN", 'N', 2, 11, -1)]
        [InlineData("!@#$%^&", '%', 0, 7, 4)]
        [InlineData("!@#$", '!', 0, 4, 0)]
        [InlineData("!@#$", '@', 0, 4, 1)]
        [InlineData("!@#$", '#', 0, 4, 2)]
        [InlineData("!@#$", '$', 0, 4, 3)]
        [InlineData("!@#$%^&*", '%', 0, 8, 4)]
        public static void IndexOf_SingleLetter(string s, char target, int startIndex, int count, int expected)
        {
            bool safeForCurrentCulture =
                IsSafeForCurrentCultureComparisons(s)
                && IsSafeForCurrentCultureComparisons(target.ToString());

            ReadOnlySpan<char> span = s.AsSpan();
            var charArray = new char[1];
            charArray[0] = target;
            ReadOnlySpan<char> targetSpan = charArray;

            int expectedFromSpan = expected == -1 ? expected : expected - startIndex;

            if (count + startIndex == s.Length)
            {
                if (startIndex == 0)
                {
                    Assert.Equal(expectedFromSpan, span.IndexOf(targetSpan, StringComparison.Ordinal));
                    Assert.Equal(expectedFromSpan, span.IndexOf(targetSpan, StringComparison.OrdinalIgnoreCase));

                    // To be safe we only want to run CurrentCulture comparisons if
                    // we know the results will not vary depending on location
                    if (safeForCurrentCulture)
                    {
                        Assert.Equal(expectedFromSpan, span.IndexOf(targetSpan, StringComparison.CurrentCulture));
                    }
                }

                Assert.Equal(expectedFromSpan, span.Slice(startIndex).IndexOf(targetSpan, StringComparison.Ordinal));
                Assert.Equal(expectedFromSpan, span.Slice(startIndex).IndexOf(targetSpan, StringComparison.OrdinalIgnoreCase));

                if (safeForCurrentCulture)
                {
                    Assert.Equal(expectedFromSpan, span.Slice(startIndex).IndexOf(targetSpan, StringComparison.CurrentCulture));
                }
            }
            Assert.Equal(expectedFromSpan, span.Slice(startIndex, count).IndexOf(targetSpan, StringComparison.Ordinal));
            Assert.Equal(expectedFromSpan, span.Slice(startIndex, count).IndexOf(targetSpan, StringComparison.OrdinalIgnoreCase));

            if (safeForCurrentCulture)
            {
                Assert.Equal(expectedFromSpan, span.Slice(startIndex, count).IndexOf(targetSpan, StringComparison.CurrentCulture));
            }
        }

        private static bool IsSafeForCurrentCultureComparisons(string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                // We only want ASCII chars that you can see
                // No controls, no delete, nothing >= 0x80
                if (c < 0x20 || c == 0x7f || c >= 0x80)
                {
                    return false;
                }
            }
            return true;
        }


        // NOTE: This is by design. Unix ignores the null characters (i.e. null characters have no weights for the string comparison).
        // For desired behavior, use ordinal comparison instead of linguistic comparison.
        // This is a known difference between Windows and Unix (https://github.com/dotnet/coreclr/issues/2051).
        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData("He\0lo", "He\0lo", 0)]
        [InlineData("He\0lo", "He\0", 0)]
        [InlineData("He\0lo", "\0", 2)]
        [InlineData("He\0lo", "\0lo", 2)]
        [InlineData("He\0lo", "lo", 3)]
        [InlineData("Hello", "lo\0", -1)]
        [InlineData("Hello", "\0lo", -1)]
        [InlineData("Hello", "l\0o", -1)]
        public static void IndexOf_NullInStrings(string s, string value, int expected)
        {
            Assert.Equal(expected, s.AsSpan().IndexOf(value.AsSpan(), StringComparison.Ordinal));
        }

        [Theory]
        [MemberData(nameof(AllSubstringsAndComparisons), new object[] { "abcde" })]
        public static void IndexOf_AllSubstrings(string s, string value, int startIndex, StringComparison comparison)
        {
            bool ignoringCase = comparison == StringComparison.OrdinalIgnoreCase || comparison == StringComparison.CurrentCultureIgnoreCase;

            // First find the substring.  We should be able to with all comparison types.
            Assert.Equal(startIndex, s.AsSpan().IndexOf(value.AsSpan(), comparison)); // in the whole string
            Assert.Equal(0, s.AsSpan(startIndex).IndexOf(value.AsSpan(), comparison)); // starting at substring
            if (startIndex > 0)
            {
                Assert.Equal(1, s.AsSpan(startIndex - 1).IndexOf(value.AsSpan(), comparison)); // starting just before substring
            }
            Assert.Equal(-1, s.AsSpan(startIndex + 1).IndexOf(value.AsSpan(), comparison)); // starting just after start of substring

            // Shouldn't be able to find the substring if the count is less than substring's length
            Assert.Equal(-1, s.AsSpan(0, value.Length - 1).IndexOf(value.AsSpan(), comparison));

            // Now double the source.  Make sure we find the first copy of the substring.
            int halfLen = s.Length;
            s += s;
            Assert.Equal(startIndex, s.AsSpan().IndexOf(value.AsSpan(), comparison));

            // Now change the case of a letter.
            s = s.ToUpperInvariant();
            Assert.Equal(ignoringCase ? startIndex : -1, s.AsSpan().IndexOf(value.AsSpan(), comparison));
        }

        public static IEnumerable<object[]> AllSubstringsAndComparisons(string source)
        {
            var comparisons = new StringComparison[]
            {
            StringComparison.CurrentCulture,
            StringComparison.CurrentCultureIgnoreCase,
            StringComparison.Ordinal,
            StringComparison.OrdinalIgnoreCase
            };

            foreach (StringComparison comparison in comparisons)
            {
                for (int i = 0; i <= source.Length; i++)
                {
                    for (int subLen = source.Length - i; subLen > 0; subLen--)
                    {
                        yield return new object[] { source, source.Substring(i, subLen), i, comparison };
                    }
                }
            }
        }

        [Fact]
        public static void IndexOf_TurkishI_TurkishCulture()
        {
            CultureInfo backupCulture = CultureInfo.CurrentCulture;

            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");

            string str = "Turkish I \u0131s TROUBL\u0130NG!";
            string valueString = "\u0130";
            ReadOnlySpan<char> s = str.AsSpan();
            ReadOnlySpan<char> value = valueString.AsSpan();
            Assert.Equal(19, s.IndexOf(value, StringComparison.CurrentCulture));
            Assert.Equal(4, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
            Assert.Equal(19, s.IndexOf(value, StringComparison.Ordinal));
            Assert.Equal(19, s.IndexOf(value, StringComparison.OrdinalIgnoreCase));

            valueString = "\u0131";
            value = valueString.AsSpan();
            Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCulture));
            Assert.Equal(8, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
            Assert.Equal(10, s.IndexOf(value, StringComparison.Ordinal));
            Assert.Equal(10, s.IndexOf(value, StringComparison.OrdinalIgnoreCase));

            Thread.CurrentThread.CurrentCulture = backupCulture;
        }

        [Fact]
        public static void IndexOf_TurkishI_InvariantCulture()
        {
            CultureInfo backupCulture = CultureInfo.CurrentCulture;

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            string str = "Turkish I \u0131s TROUBL\u0130NG!";
            string valueString = "\u0130";
            ReadOnlySpan<char> s = str.AsSpan();
            ReadOnlySpan<char> value = valueString.AsSpan();

            Assert.Equal(19, s.IndexOf(value, StringComparison.CurrentCulture));
            Assert.Equal(19, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

            valueString = "\u0131";
            value = valueString.AsSpan();
            Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCulture));
            Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

            Thread.CurrentThread.CurrentCulture = backupCulture;
        }

        [Fact]
        public static void IndexOf_TurkishI_EnglishUSCulture()
        {
            CultureInfo backupCulture = CultureInfo.CurrentCulture;

            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            string str = "Turkish I \u0131s TROUBL\u0130NG!";
            string valueString = "\u0130";
            ReadOnlySpan<char> s = str.AsSpan();
            ReadOnlySpan<char> value = valueString.AsSpan();

            Assert.Equal(19, s.IndexOf(value, StringComparison.CurrentCulture));
            Assert.Equal(19, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

            valueString = "\u0131";
            value = valueString.AsSpan();
            Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCulture));
            Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

            Thread.CurrentThread.CurrentCulture = backupCulture;
        }

        [Fact]
        public static void IndexOf_HungarianDoubleCompression_HungarianCulture()
        {
            string str = "dzsdzs";
            string valueString = "ddzs";

            ReadOnlySpan<char> s = str.AsSpan();
            ReadOnlySpan<char> value = valueString.AsSpan();

            CultureInfo backupCulture = CultureInfo.CurrentCulture;

            Thread.CurrentThread.CurrentCulture = new CultureInfo("hu-HU");
            /*
             There are differences between Windows and ICU regarding contractions.
             Windows has equal contraction collation weights, including case (target="Ddzs" same behavior as "ddzs").
             ICU has different contraction collation weights, depending on locale collation rules.
             If CurrentCultureIgnoreCase is specified, ICU will use 'secondary' collation rules
             which ignore the contraction collation weights (defined as 'tertiary' rules)
            */
            Assert.Equal(PlatformDetection.IsWindows ? 0 : -1, s.IndexOf(value, StringComparison.CurrentCulture));

            Assert.Equal(0, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
            Assert.Equal(-1, s.IndexOf(value, StringComparison.Ordinal));
            Assert.Equal(-1, s.IndexOf(value, StringComparison.OrdinalIgnoreCase));

            Thread.CurrentThread.CurrentCulture = backupCulture;
        }

        [Fact]
        public static void IndexOf_HungarianDoubleCompression_InvariantCulture()
        {
            string str = "dzsdzs";
            string valueString = "ddzs";

            ReadOnlySpan<char> s = str.AsSpan();
            ReadOnlySpan<char> value = valueString.AsSpan();

            CultureInfo backupCulture = CultureInfo.CurrentCulture;

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Assert.Equal(-1, s.IndexOf(value, StringComparison.CurrentCulture));
            Assert.Equal(-1, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

            Thread.CurrentThread.CurrentCulture = backupCulture;
        }

        [Fact]
        public static void IndexOf_EquivalentDiacritics_EnglishUSCulture()
        {
            string str = "Exhibit a\u0300\u00C0";
            string valueString = "\u00C0";

            ReadOnlySpan<char> s = str.AsSpan();
            ReadOnlySpan<char> value = valueString.AsSpan();

            CultureInfo backupCulture = CultureInfo.CurrentCulture;

            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCulture));
            Assert.Equal(8, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
            Assert.Equal(10, s.IndexOf(value, StringComparison.Ordinal));
            Assert.Equal(10, s.IndexOf(value, StringComparison.OrdinalIgnoreCase));

            valueString = "a\u0300"; // this diacritic combines with preceding character
            value = valueString.AsSpan();
            Assert.Equal(8, s.IndexOf(value, StringComparison.CurrentCulture));
            Assert.Equal(8, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
            Assert.Equal(8, s.IndexOf(value, StringComparison.Ordinal));
            Assert.Equal(8, s.IndexOf(value, StringComparison.OrdinalIgnoreCase));

            Thread.CurrentThread.CurrentCulture = backupCulture;
        }

        [Fact]
        public static void IndexOf_EquivalentDiacritics_InvariantCulture()
        {
            string str = "Exhibit a\u0300\u00C0";
            string valueString = "\u00C0";

            ReadOnlySpan<char> s = str.AsSpan();
            ReadOnlySpan<char> value = valueString.AsSpan();

            CultureInfo backupCulture = CultureInfo.CurrentCulture;

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Assert.Equal(10, s.IndexOf(value, StringComparison.CurrentCulture));
            Assert.Equal(8, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

            valueString = "a\u0300"; // this diacritic combines with preceding character
            value = valueString.AsSpan();
            Assert.Equal(8, s.IndexOf(value, StringComparison.CurrentCulture));
            Assert.Equal(8, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

            Thread.CurrentThread.CurrentCulture = backupCulture;
        }

        [Fact]
        public static void IndexOf_CyrillicE_EnglishUSCulture()
        {
            string str = "Foo\u0400Bar";
            string valueString = "\u0400";

            ReadOnlySpan<char> s = str.AsSpan();
            ReadOnlySpan<char> value = valueString.AsSpan();

            CultureInfo backupCulture = CultureInfo.CurrentCulture;

            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            Assert.Equal(3, s.IndexOf(value, StringComparison.CurrentCulture));
            Assert.Equal(3, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
            Assert.Equal(3, s.IndexOf(value, StringComparison.Ordinal));
            Assert.Equal(3, s.IndexOf(value, StringComparison.OrdinalIgnoreCase));

            valueString = "bar";
            value = valueString.AsSpan();
            Assert.Equal(-1, s.IndexOf(value, StringComparison.CurrentCulture));
            Assert.Equal(4, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));
            Assert.Equal(-1, s.IndexOf(value, StringComparison.Ordinal));
            Assert.Equal(4, s.IndexOf(value, StringComparison.OrdinalIgnoreCase));

            Thread.CurrentThread.CurrentCulture = backupCulture;
        }

        [Fact]
        public static void IndexOf_CyrillicE_InvariantCulture()
        {
            string str = "Foo\u0400Bar";
            string valueString = "\u0400";

            ReadOnlySpan<char> s = str.AsSpan();
            ReadOnlySpan<char> value = valueString.AsSpan();

            CultureInfo backupCulture = CultureInfo.CurrentCulture;

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Assert.Equal(3, s.IndexOf(value, StringComparison.CurrentCulture));
            Assert.Equal(3, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

            valueString = "bar";
            value = valueString.AsSpan();
            Assert.Equal(-1, s.IndexOf(value, StringComparison.CurrentCulture));
            Assert.Equal(4, s.IndexOf(value, StringComparison.CurrentCultureIgnoreCase));

            Thread.CurrentThread.CurrentCulture = backupCulture;
        }
    }
}
