// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Globalization.Tests
{
    public class OrdinalCasingTests
    {
        private const int FirstSurrogate = 0xD800;
        private const int LastSurrogate = 0xDFFF;

        // Pairs that are equal under StringComparison.OrdinalIgnoreCase including the well known
        // characters that fold to the same uppercase letter even though they are not the ASCII forms.
        public static IEnumerable<object[]> OrdinalIgnoreCaseEqualPairs()
        {
            yield return new object[] { "", "" };
            yield return new object[] { "abc", "ABC" };
            yield return new object[] { "Hello", "hELLO" };
            yield return new object[] { "\u017F", "\u017F" };  // LATIN SMALL LETTER LONG S stays itself (no OIC fold to 'S')
            yield return new object[] { "\u0131", "\u0131" };  // LATIN SMALL LETTER DOTLESS I stays itself (no OIC fold to 'I')
            yield return new object[] { "\u03C3", "\u03A3" };  // GREEK SMALL SIGMA vs GREEK CAPITAL SIGMA
            yield return new object[] { "\u0561\u0562", "\u0531\u0532" }; // Armenian small vs capital

            // MICRO SIGN and GREEK SMALL FINAL SIGMA fold to their Greek capitals only under ICU and invariant
            // casing. NLS does not treat these as OrdinalIgnoreCase-equal, so only include them when not on NLS.
            if (!PlatformDetection.IsNlsGlobalization)
            {
                yield return new object[] { "\u00B5", "\u039C" };  // MICRO SIGN folds to GREEK CAPITAL MU
                yield return new object[] { "\u03C2", "\u03A3" };  // GREEK SMALL FINAL SIGMA vs GREEK CAPITAL SIGMA
            }
        }

        public static IEnumerable<object[]> OrdinalIgnoreCaseNotEqualPairs()
        {
            yield return new object[] { "abc", "abd" };
            yield return new object[] { "abc", "ab" };
            yield return new object[] { "\u017F", "S" };       // LONG S is not OIC equal to 'S'
            yield return new object[] { "\u017F", "s" };
            yield return new object[] { "\u0131", "I" };       // DOTLESS I is not OIC equal to 'I'
            yield return new object[] { "\u0131", "i" };
            yield return new object[] { "\u212A", "K" };       // KELVIN SIGN is not OIC equal to 'K'
            yield return new object[] { "\u212A", "k" };
            yield return new object[] { "K", "L" };
        }

        [Theory]
        [MemberData(nameof(OrdinalIgnoreCaseEqualPairs))]
        public void ToUpperOrdinal_MatchesOrdinalIgnoreCase_ForEqualStrings(string left, string right)
        {
            bool oic = string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
            Assert.True(oic);
            Assert.Equal(left.ToUpperOrdinal(), right.ToUpperOrdinal());
        }

        [Theory]
        [MemberData(nameof(OrdinalIgnoreCaseNotEqualPairs))]
        public void ToUpperOrdinal_MatchesOrdinalIgnoreCase_ForNotEqualStrings(string left, string right)
        {
            bool oic = string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
            Assert.False(oic);
            bool foldEqual = string.Equals(left.ToUpperOrdinal(), right.ToUpperOrdinal(), StringComparison.Ordinal);
            Assert.False(foldEqual);
        }

        [Fact]
        public void ToUpperOrdinal_EveryBmpChar_IsOrdinalIgnoreCaseEqualToItsFold()
        {
            for (int c = 0; c <= 0xFFFF; c++)
            {
                if (c >= FirstSurrogate && c <= LastSurrogate)
                {
                    continue;
                }

                string original = ((char)c).ToString();
                string upper = original.ToUpperOrdinal();

                Assert.Equal(1, upper.Length);
                Assert.True(string.Equals(original, upper, StringComparison.OrdinalIgnoreCase),
                    $"U+{c:X4} is not OrdinalIgnoreCase-equal to its ToUpperOrdinal fold U+{(int)upper[0]:X4}");

                // The String, Char and span overloads must agree.
                Assert.Equal(upper[0], char.ToUpperOrdinal((char)c));

                Span<char> destination = ['\0'];
                Assert.Equal(1, original.AsSpan().ToUpperOrdinal(destination));
                Assert.Equal(upper[0], destination[0]);
            }
        }

        [Fact]
        public void ToLowerOrdinal_EveryBmpChar_AgreesAcrossOverloads()
        {
            for (int c = 0; c <= 0xFFFF; c++)
            {
                if (c >= FirstSurrogate && c <= LastSurrogate)
                {
                    continue;
                }

                string original = ((char)c).ToString();
                string lower = original.ToLowerOrdinal();

                Assert.Equal(1, lower.Length);
                Assert.Equal(lower[0], char.ToLowerOrdinal((char)c));

                Span<char> destination = ['\0'];
                Assert.Equal(1, original.AsSpan().ToLowerOrdinal(destination));
                Assert.Equal(lower[0], destination[0]);
            }
        }

        // ToLowerOrdinal must stay consistent with OrdinalIgnoreCase: a character is always
        // OrdinalIgnoreCase-equal to its ordinal lower-cased form. This guards the special handling that keeps
        // characters unchanged when their simple lower mapping would move them out of their ordinal upper-casing
        // equivalence class (for example the Kelvin, Ohm, and Angstrom signs) instead of folding them into a different class.
        [Fact]
        public void ToLowerOrdinal_EveryBmpChar_IsOrdinalIgnoreCaseEqualToItsFold()
        {
            for (int c = 0; c <= 0xFFFF; c++)
            {
                if (c >= FirstSurrogate && c <= LastSurrogate)
                {
                    continue;
                }

                string original = ((char)c).ToString();
                string lower = original.ToLowerOrdinal();
                bool equal = string.Equals(original, lower, StringComparison.OrdinalIgnoreCase);
                Assert.True(equal);
            }
        }

        // Validates the ordinal lower casing value for the whole BMP, including the pages that are pre-seeded as
        // no-casing, against char.ToLowerInvariant. Invariant lowering matches the simple lower mapping everywhere,
        // so it is used as an independent oracle: where it would move a character out of its ordinal upper-casing
        // class the ordinal value keeps the original character, otherwise the two must agree. A page wrongly marked
        // as no-casing would return the original character while the oracle returns the lowercased form.
        [Fact]
        public void ToLowerOrdinal_EveryBmpChar_MatchesInvariantLoweringWithinClass()
        {
            for (int c = 0; c <= 0xFFFF; c++)
            {
                if (c >= FirstSurrogate && c <= LastSurrogate)
                {
                    continue;
                }

                char ordinalLower = char.ToLowerOrdinal((char)c);
                char invariantLower = char.ToLowerInvariant((char)c);

                if (char.ToUpperOrdinal(invariantLower) != char.ToUpperOrdinal((char)c))
                {
                    Assert.Equal((char)c, ordinalLower);
                }
                else
                {
                    Assert.Equal(invariantLower, ordinalLower);
                }
            }
        }

        [Fact]
        public void Casing_Ascii_IsCorrect()
        {
            for (int c = 0; c < 0x80; c++)
            {
                char expectedUpper = (c >= 'a' && c <= 'z') ? (char)(c - 0x20) : (char)c;
                char expectedLower = (c >= 'A' && c <= 'Z') ? (char)(c + 0x20) : (char)c;

                Assert.Equal(expectedUpper, char.ToUpperOrdinal((char)c));
                Assert.Equal(expectedLower, char.ToLowerOrdinal((char)c));
            }
        }

        public static IEnumerable<object[]> CharUpperData()
        {
            yield return new object[] { 'a', 'A' };
            yield return new object[] { 'z', 'Z' };
            yield return new object[] { 'A', 'A' };
            yield return new object[] { '5', '5' };
            yield return new object[] { '\u00E0', '\u00C0' }; // a-grave -> A-grave
            yield return new object[] { '\u03B1', '\u0391' }; // greek small alpha -> capital alpha
            yield return new object[] { '\u0450', '\u0400' }; // cyrillic small ie -> capital
            yield return new object[] { '\u212A', '\u212A' };  // KELVIN SIGN is not folded by ordinal upper
            yield return new object[] { '\u0131', '\u0131' };  // DOTLESS I unchanged
            yield return new object[] { '\u017F', '\u017F' };  // LONG S unchanged

            // MICRO SIGN folds to GREEK CAPITAL MU only under ICU and invariant casing. NLS leaves it unchanged.
            if (!PlatformDetection.IsNlsGlobalization)
            {
                yield return new object[] { '\u00B5', '\u039C' }; // MICRO SIGN -> GREEK CAPITAL MU
            }
        }

        public static IEnumerable<object[]> CharLowerData()
        {
            yield return new object[] { 'A', 'a' };
            yield return new object[] { 'Z', 'z' };
            yield return new object[] { 'a', 'a' };
            yield return new object[] { '5', '5' };
            yield return new object[] { '\u00C0', '\u00E0' }; // A-grave -> a-grave
            yield return new object[] { '\u0391', '\u03B1' }; // greek capital alpha -> small alpha
            yield return new object[] { '\u0400', '\u0450' }; // cyrillic capital -> small ie
            yield return new object[] { '\u212A', '\u212A' };  // KELVIN SIGN stays itself (not OIC equal to 'k')
            yield return new object[] { '\u212B', '\u212B' };  // ANGSTROM SIGN stays itself (not OIC equal to small a-ring)
            yield return new object[] { '\u2126', '\u2126' };  // OHM SIGN stays itself (not OIC equal to small omega)
            yield return new object[] { '\u1E9E', '\u1E9E' };  // CAPITAL SHARP S stays itself (not OIC equal to small sharp s)
            yield return new object[] { '\u03F4', '\u03F4' };  // GREEK CAPITAL THETA SYMBOL stays itself (not OIC equal to small theta)
            yield return new object[] { '\u0130', '\u0130' };  // CAPITAL I WITH DOT ABOVE stays itself (simple 'i' would leave its class)
            yield return new object[] { 'I', 'i' };            // ordinal lowering ignores Turkish rules
        }

        [Theory]
        [MemberData(nameof(CharUpperData))]
        public void Char_ToUpperOrdinal(char input, char expected)
        {
            Assert.Equal(expected, char.ToUpperOrdinal(input));
            Assert.Equal(expected.ToString(), input.ToString().ToUpperOrdinal());
        }

        [Theory]
        [MemberData(nameof(CharLowerData))]
        public void Char_ToLowerOrdinal(char input, char expected)
        {
            Assert.Equal(expected, char.ToLowerOrdinal(input));
            Assert.Equal(expected.ToString(), input.ToString().ToLowerOrdinal());
        }

        [Fact]
        public void String_Empty_ReturnsEmpty()
        {
            Assert.Same(string.Empty, "".ToUpperOrdinal());
            Assert.Same(string.Empty, "".ToLowerOrdinal());
        }

        [Fact]
        public void String_PreservesLength()
        {
            string value = "Stra\u00DFe \u212A \u0131\u017F Test 123";
            Assert.Equal(value.Length, value.ToUpperOrdinal().Length);
            Assert.Equal(value.Length, value.ToLowerOrdinal().Length);
        }

        [Theory]
        [InlineData("A")]
        [InlineData("AB")]
        [InlineData("ABC")]
        [InlineData("HELLO WORLD")]
        [InlineData("HELLO WORLD 123!")]
        [InlineData("123 !@#$ ")]
        public void ToUpperOrdinal_AlreadyUpperAscii_ReturnsSameInstance(string value)
        {
            Assert.Same(value, value.ToUpperOrdinal());
        }

        [Theory]
        [InlineData("a")]
        [InlineData("ab")]
        [InlineData("abc")]
        [InlineData("hello world")]
        [InlineData("hello world 123!")]
        [InlineData("123 !@#$ ")]
        public void ToLowerOrdinal_AlreadyLowerAscii_ReturnsSameInstance(string value)
        {
            Assert.Same(value, value.ToLowerOrdinal());
        }

        [Theory]
        [InlineData("abc", "ABC")]         // change at the start
        [InlineData("aBcD", "ABCD")]       // interleaved
        [InlineData("ABCd", "ABCD")]       // change at the end (odd length)
        [InlineData("aBCDEFGH", "ABCDEFGH")]
        [InlineData("ABCDEFGa", "ABCDEFGA")]
        [InlineData("ABC\u00E9", "ABC\u00C9")]     // ascii prefix already upper, non-ascii tail changes
        [InlineData("aBc\u00E9", "ABC\u00C9")]     // change begins in the ascii prefix
        public void ToUpperOrdinal_NeedsChange_ProducesCorrectNewInstance(string value, string expected)
        {
            string result = value.ToUpperOrdinal();
            Assert.Equal(expected, result);
            Assert.NotSame(value, result);
        }

        [Theory]
        [InlineData("ABC", "abc")]
        [InlineData("aBcD", "abcd")]
        [InlineData("abcD", "abcd")]
        [InlineData("Abcdefgh", "abcdefgh")]
        [InlineData("abcdefgH", "abcdefgh")]
        [InlineData("abc\u00C9", "abc\u00E9")]
        [InlineData("aBc\u00C9", "abc\u00E9")]
        public void ToLowerOrdinal_NeedsChange_ProducesCorrectNewInstance(string value, string expected)
        {
            string result = value.ToLowerOrdinal();
            Assert.Equal(expected, result);
            Assert.NotSame(value, result);
        }

        [Fact]
        public void Span_DestinationTooSmall_ReturnsNegativeOne()
        {
            ReadOnlySpan<char> source = "abc";
            Span<char> destination = stackalloc char[2];
            Assert.Equal(-1, source.ToUpperOrdinal(destination));
            Assert.Equal(-1, source.ToLowerOrdinal(destination));
        }

        [Fact]
        public void Span_ExactSize_WritesAllCharacters()
        {
            ReadOnlySpan<char> source = "aBcD";
            Span<char> upper = stackalloc char[4];
            Span<char> lower = stackalloc char[4];

            Assert.Equal(4, source.ToUpperOrdinal(upper));
            Assert.Equal(4, source.ToLowerOrdinal(lower));
            Assert.Equal("ABCD", upper.ToString());
            Assert.Equal("abcd", lower.ToString());
        }

        [Fact]
        public void Span_OverlappingBuffers_Throws()
        {
            char[] buffer = { 'a', 'b', 'c', 'd' };
            Assert.Throws<InvalidOperationException>(() => ((ReadOnlySpan<char>)buffer).ToUpperOrdinal(buffer));
            Assert.Throws<InvalidOperationException>(() => ((ReadOnlySpan<char>)buffer).ToLowerOrdinal(buffer));
        }

        public static IEnumerable<object[]> RuneUpperData()
        {
            yield return new object[] { new Rune('a'), new Rune('A') };
            yield return new object[] { new Rune('5'), new Rune('5') };
            yield return new object[] { new Rune('\u03B1'), new Rune('\u0391') }; // greek
            yield return new object[] { new Rune('\u212A'), new Rune('\u212A') }; // KELVIN SIGN is not folded by ordinal upper
            yield return new object[] { new Rune(0x10428), new Rune(0x10400) };    // DESERET small ee -> capital long i
        }

        public static IEnumerable<object[]> RuneLowerData()
        {
            yield return new object[] { new Rune('A'), new Rune('a') };
            yield return new object[] { new Rune('5'), new Rune('5') };
            yield return new object[] { new Rune('\u0391'), new Rune('\u03B1') }; // greek
            yield return new object[] { new Rune('\u212A'), new Rune('\u212A') }; // KELVIN SIGN stays itself (not OIC equal to 'k')
            yield return new object[] { new Rune(0x10400), new Rune(0x10428) };    // DESERET capital -> small
        }

        [Theory]
        [MemberData(nameof(RuneUpperData))]
        public void Rune_ToUpperOrdinal(Rune input, Rune expected)
        {
            Assert.Equal(expected, Rune.ToUpperOrdinal(input));

            if (input.IsBmp)
            {
                Assert.Equal((char)expected.Value, char.ToUpperOrdinal((char)input.Value));
            }
        }

        [Theory]
        [MemberData(nameof(RuneLowerData))]
        public void Rune_ToLowerOrdinal(Rune input, Rune expected)
        {
            Assert.Equal(expected, Rune.ToLowerOrdinal(input));

            if (input.IsBmp)
            {
                Assert.Equal((char)expected.Value, char.ToLowerOrdinal((char)input.Value));
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void OrdinalIgnoreCase_Equivalence_HoldsInInvariantMode()
        {
            var options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions.Add("System.Globalization.Invariant", true);

            RemoteExecutor.Invoke(static () =>
            {
                foreach (object[] pair in OrdinalIgnoreCaseEqualPairs())
                {
                    string left = (string)pair[0];
                    string right = (string)pair[1];

                    bool oic = string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
                    bool upperEqual = string.Equals(left.ToUpperOrdinal(), right.ToUpperOrdinal(), StringComparison.Ordinal);
                    Assert.Equal(oic, upperEqual);
                }

                // Verify the ToUpperOrdinal-equals-OrdinalIgnoreCase contract over the whole BMP in invariant mode.
                for (int c = 0; c <= 0xFFFF; c++)
                {
                    if (c >= FirstSurrogate && c <= LastSurrogate)
                    {
                        continue;
                    }

                    string original = ((char)c).ToString();
                    string upper = original.ToUpperOrdinal();
                    bool selfOic = string.Equals(original, upper, StringComparison.OrdinalIgnoreCase);
                    Assert.True(selfOic);

                    // ToLowerOrdinal has its own invariant-mode path (InvariantModeCasing.ToLower) that must keep
                    // the same OrdinalIgnoreCase consistency, including the characters (Kelvin, Ohm, Angstrom, ...)
                    // whose simple lower mapping would otherwise move them out of their ordinal upper-casing class.
                    string lower = original.ToLowerOrdinal();
                    Assert.Equal(1, lower.Length);
                    Assert.True(string.Equals(original, lower, StringComparison.OrdinalIgnoreCase),
                        $"U+{c:X4} is not OrdinalIgnoreCase-equal to its ToLowerOrdinal fold U+{(int)lower[0]:X4} in invariant mode");
                    Assert.Equal(lower[0], char.ToLowerOrdinal((char)c));
                }
            }, options).Dispose();
        }
    }
}
