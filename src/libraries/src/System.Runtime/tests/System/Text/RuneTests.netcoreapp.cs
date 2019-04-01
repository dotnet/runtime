// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Globalization;
using Xunit;

namespace System.Text.Tests
{
    public static partial class RuneTests
    {
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows8xOrLater))] // the localization tables used by our test data only exist on Win8+
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData('0', '0', '0', "en-US")]
        [InlineData('a', 'A', 'a', "en-US")]
        [InlineData('i', 'I', 'i', "en-US")]
        [InlineData('i', '\u0130', 'i', "tr-TR")]
        [InlineData('z', 'Z', 'z', "en-US")]
        [InlineData('A', 'A', 'a', "en-US")]
        [InlineData('I', 'I', 'i', "en-US")]
        [InlineData('I', 'I', '\u0131', "tr-TR")]
        [InlineData('Z', 'Z', 'z', "en-US")]
        [InlineData('\u00DF', '\u00DF', '\u00DF', "de-DE")] // U+00DF LATIN SMALL LETTER SHARP S -- n.b. ToUpper doesn't create the majuscule form
        [InlineData('\u0130', '\u0130', 'i', "tr-TR")] // U+0130 LATIN CAPITAL LETTER I WITH DOT ABOVE
        [InlineData('\u0131', 'I', '\u0131', "tr-TR")] // U+0131 LATIN SMALL LETTER DOTLESS I
        [InlineData('\u1E9E', '\u1E9E', '\u00DF', "de-DE")] // U+1E9E LATIN CAPITAL LETTER SHARP S
        [InlineData(0x10400, 0x10400, 0x10428, "en-US")] // U+10400 DESERET CAPITAL LETTER LONG I
        [InlineData(0x10428, 0x10400, 0x10428, "en-US")] // U+10428 DESERET SMALL LETTER LONG I
        public static void Casing_CultureAware(int original, int upper, int lower, string culture)
        {
            var rune = new Rune(original);
            var cultureInfo = CultureInfo.GetCultureInfo(culture);
            Assert.Equal(new Rune(upper), Rune.ToUpper(rune, cultureInfo));
            Assert.Equal(new Rune(lower), Rune.ToLower(rune, cultureInfo));
        }

        // Invariant ToUpper / ToLower doesn't modify Turkish I or majuscule Eszett
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows8xOrLater))] // the localization tables used by our test data only exist on Win8+
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData('0', '0', '0')]
        [InlineData('a', 'A', 'a')]
        [InlineData('i', 'I', 'i')]
        [InlineData('z', 'Z', 'z')]
        [InlineData('A', 'A', 'a')]
        [InlineData('I', 'I', 'i')]
        [InlineData('Z', 'Z', 'z')]
        [InlineData('\u00DF', '\u00DF', '\u00DF')] // U+00DF LATIN SMALL LETTER SHARP S
        [InlineData('\u0130', '\u0130', '\u0130')] // U+0130 LATIN CAPITAL LETTER I WITH DOT ABOVE
        [InlineData('\u0131', '\u0131', '\u0131')] // U+0131 LATIN SMALL LETTER DOTLESS I
        [InlineData('\u1E9E', '\u1E9E', '\u1E9E')] // U+1E9E LATIN CAPITAL LETTER SHARP S
        [InlineData(0x10400, 0x10400, 0x10428)] // U+10400 DESERET CAPITAL LETTER LONG I
        [InlineData(0x10428, 0x10400, 0x10428)] // U+10428 DESERET SMALL LETTER LONG I
        public static void Casing_Invariant(int original, int upper, int lower)
        {
            var rune = new Rune(original);
            Assert.Equal(new Rune(upper), Rune.ToUpperInvariant(rune));
            Assert.Equal(new Rune(lower), Rune.ToLowerInvariant(rune));
        }

        [Theory]
        [MemberData(nameof(GeneralTestData_BmpCodePoints_NoSurrogates))]
        public static void Ctor_Cast_Char_Valid(GeneralTestData testData)
        {
            Rune rune = new Rune(checked((char)testData.ScalarValue));
            Rune runeFromCast = (Rune)(char)testData.ScalarValue;

            Assert.Equal(rune, runeFromCast);
            Assert.Equal(testData.ScalarValue, rune.Value);
            Assert.Equal(testData.IsAscii, rune.IsAscii);
            Assert.Equal(testData.IsBmp, rune.IsBmp);
            Assert.Equal(testData.Plane, rune.Plane);
        }

        [Theory]
        [MemberData(nameof(BmpCodePoints_SurrogatesOnly))]
        public static void Ctor_Cast_Char_Invalid_Throws(char ch)
        {
            Assert.Throws<ArgumentOutOfRangeException>(nameof(ch), () => new Rune(ch));
            Assert.Throws<ArgumentOutOfRangeException>(nameof(ch), () => (Rune)ch);
        }

        [Theory]
        [MemberData(nameof(GeneralTestData_BmpCodePoints_NoSurrogates))]
        [MemberData(nameof(GeneralTestData_SupplementaryCodePoints_ValidOnly))]
        public static void Ctor_Cast_Int32_Valid(GeneralTestData testData)
        {
            Rune rune = new Rune((int)testData.ScalarValue);
            Rune runeFromCast = (Rune)(int)testData.ScalarValue;

            Assert.Equal(rune, runeFromCast);
            Assert.Equal(testData.ScalarValue, rune.Value);
            Assert.Equal(testData.IsAscii, rune.IsAscii);
            Assert.Equal(testData.IsBmp, rune.IsBmp);
            Assert.Equal(testData.Plane, rune.Plane);
        }

        [Theory]
        [MemberData(nameof(BmpCodePoints_SurrogatesOnly))]
        [MemberData(nameof(SupplementaryCodePoints_InvalidOnly))]
        public static void Ctor_Cast_Int32_Invalid_Throws(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(nameof(value), () => new Rune(value));
            Assert.Throws<ArgumentOutOfRangeException>(nameof(value), () => (Rune)value);
        }

        [Theory]
        [MemberData(nameof(GeneralTestData_BmpCodePoints_NoSurrogates))]
        [MemberData(nameof(GeneralTestData_SupplementaryCodePoints_ValidOnly))]
        public static void Ctor_Cast_UInt32_Valid(GeneralTestData testData)
        {
            Rune rune = new Rune((uint)testData.ScalarValue);
            Rune runeFromCast = (Rune)(uint)testData.ScalarValue;

            Assert.Equal(rune, runeFromCast);
            Assert.Equal(testData.ScalarValue, rune.Value);
            Assert.Equal(testData.IsAscii, rune.IsAscii);
            Assert.Equal(testData.IsBmp, rune.IsBmp);
            Assert.Equal(testData.Plane, rune.Plane);
        }

        [Theory]
        [MemberData(nameof(BmpCodePoints_SurrogatesOnly))]
        [MemberData(nameof(SupplementaryCodePoints_InvalidOnly))]
        public static void Ctor_Cast_UInt32_Invalid_Throws(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(nameof(value), () => new Rune((uint)value));
            Assert.Throws<ArgumentOutOfRangeException>(nameof(value), () => (Rune)(uint)value);
        }

        [Theory]
        [MemberData(nameof(SurrogatePairTestData_ValidOnly))]
        public static void Ctor_SurrogatePair_Valid(char highSurrogate, char lowSurrogate, int expectedValue)
        {
            Assert.Equal(expectedValue, new Rune(highSurrogate, lowSurrogate).Value);
        }

        [Theory]
        [MemberData(nameof(SurrogatePairTestData_InvalidOnly))]
        public static void Ctor_SurrogatePair_Valid(char highSurrogate, char lowSurrogate)
        {
            string expectedParamName = !char.IsHighSurrogate(highSurrogate) ? nameof(highSurrogate) : nameof(lowSurrogate);
            Assert.Throws<ArgumentOutOfRangeException>(expectedParamName, () => new Rune(highSurrogate, lowSurrogate));
        }

        [Theory]
        [InlineData('A', 'a', -1)]
        [InlineData('A', 'A', 0)]
        [InlineData('a', 'A', 1)]
        [InlineData(0x10000, 0x10000, 0)]
        [InlineData('\uFFFD', 0x10000, -1)]
        [InlineData(0x10FFFF, 0x10000, 1)]
        public static void CompareTo_And_ComparisonOperators(int first, int other, int expectedSign)
        {
            Rune a = new Rune(first);
            Rune b = new Rune(other);

            Assert.Equal(expectedSign, Math.Sign(a.CompareTo(b)));
            Assert.Equal(expectedSign < 0, a < b);
            Assert.Equal(expectedSign <= 0, a <= b);
            Assert.Equal(expectedSign > 0, a > b);
            Assert.Equal(expectedSign >= 0, a >= b);
        }

        [Theory]
        [InlineData(new char[0], OperationStatus.NeedMoreData, 0xFFFD, 0)] // empty buffer
        [InlineData(new char[] { '\u1234' }, OperationStatus.Done, 0x1234, 1)] // BMP char
        [InlineData(new char[] { '\u1234', '\ud800' }, OperationStatus.Done, 0x1234, 1)] // BMP char
        [InlineData(new char[] { '\ud83d', '\ude32' }, OperationStatus.Done, 0x1F632, 2)] // supplementary value (U+1F632 ASTONISHED FACE)
        [InlineData(new char[] { '\udc00' }, OperationStatus.InvalidData, 0xFFFD, 1)] // standalone low surrogate
        [InlineData(new char[] { '\udc00', '\udc00' }, OperationStatus.InvalidData, 0xFFFD, 1)] // standalone low surrogate
        [InlineData(new char[] { '\udc00', '\udc00' }, OperationStatus.InvalidData, 0xFFFD, 1)] // standalone low surrogate
        [InlineData(new char[] { '\ud800' }, OperationStatus.NeedMoreData, 0xFFFD, 1)] // high surrogate at end of buffer
        [InlineData(new char[] { '\ud800', '\ud800' }, OperationStatus.InvalidData, 0xFFFD, 1)] // standalone high surrogate
        [InlineData(new char[] { '\ud800', '\u1234' }, OperationStatus.InvalidData, 0xFFFD, 1)] // standalone high surrogate
        public static void DecodeFromUtf16(char[] data, OperationStatus expectedOperationStatus, int expectedRuneValue, int expectedCharsConsumed)
        {
            Assert.Equal(expectedOperationStatus, Rune.DecodeFromUtf16(data, out Rune actualRune, out int actualCharsConsumed));
            Assert.Equal(expectedRuneValue, actualRune.Value);
            Assert.Equal(expectedCharsConsumed, actualCharsConsumed);
        }

        [Theory]
        [InlineData(new char[0], OperationStatus.NeedMoreData, 0xFFFD, 0)] // empty buffer
        [InlineData(new char[] { '\u1234', '\u5678' }, OperationStatus.Done, 0x5678, 1)] // BMP char
        [InlineData(new char[] { '\udc00', '\ud800' }, OperationStatus.NeedMoreData, 0xFFFD, 1)] // high surrogate at end of buffer
        [InlineData(new char[] { '\ud83d', '\ude32' }, OperationStatus.Done, 0x1F632, 2)] // supplementary value (U+1F632 ASTONISHED FACE)
        [InlineData(new char[] { '\u1234', '\udc00' }, OperationStatus.InvalidData, 0xFFFD, 1)] // standalone low surrogate
        [InlineData(new char[] { '\udc00' }, OperationStatus.InvalidData, 0xFFFD, 1)] // standalone low surrogate
        public static void DecodeLastFromUtf16(char[] data, OperationStatus expectedOperationStatus, int expectedRuneValue, int expectedCharsConsumed)
        {
            Assert.Equal(expectedOperationStatus, Rune.DecodeLastFromUtf16(data, out Rune actualRune, out int actualCharsConsumed));
            Assert.Equal(expectedRuneValue, actualRune.Value);
            Assert.Equal(expectedCharsConsumed, actualCharsConsumed);
        }

        [Theory]
        [InlineData(new byte[0], OperationStatus.NeedMoreData, 0xFFFD, 0)] // empty buffer
        [InlineData(new byte[] { 0x30 }, OperationStatus.Done, 0x0030, 1)] // ASCII byte
        [InlineData(new byte[] { 0x30, 0x40, 0x50 }, OperationStatus.Done, 0x0030, 1)] // ASCII byte
        [InlineData(new byte[] { 0x80 }, OperationStatus.InvalidData, 0xFFFD, 1)] // standalone continuation byte
        [InlineData(new byte[] { 0x80, 0x80, 0x80 }, OperationStatus.InvalidData, 0xFFFD, 1)] // standalone continuation byte
        [InlineData(new byte[] { 0xC1 }, OperationStatus.InvalidData, 0xFFFD, 1)] // C1 is never a valid UTF-8 byte
        [InlineData(new byte[] { 0xF5 }, OperationStatus.InvalidData, 0xFFFD, 1)] // F5 is never a valid UTF-8 byte
        [InlineData(new byte[] { 0xC2 }, OperationStatus.NeedMoreData, 0xFFFD, 1)] // C2 is a valid byte; expecting it to be followed by a continuation byte
        [InlineData(new byte[] { 0xED }, OperationStatus.NeedMoreData, 0xFFFD, 1)] // ED is a valid byte; expecting it to be followed by a continuation byte
        [InlineData(new byte[] { 0xF4 }, OperationStatus.NeedMoreData, 0xFFFD, 1)] // F4 is a valid byte; expecting it to be followed by a continuation byte
        [InlineData(new byte[] { 0xC2, 0xC2 }, OperationStatus.InvalidData, 0xFFFD, 1)] // C2 not followed by continuation byte
        [InlineData(new byte[] { 0xC3, 0x90 }, OperationStatus.Done, 0x00D0, 2)] // [ C3 90 ] is U+00D0 LATIN CAPITAL LETTER ETH
        [InlineData(new byte[] { 0xC1, 0xBF }, OperationStatus.InvalidData, 0xFFFD, 1)] // [ C1 BF ] is overlong 2-byte sequence, all overlong sequences have maximal invalid subsequence length 1
        [InlineData(new byte[] { 0xE0, 0x9F }, OperationStatus.InvalidData, 0xFFFD, 1)] // [ E0 9F ] is overlong 3-byte sequence, all overlong sequences have maximal invalid subsequence length 1
        [InlineData(new byte[] { 0xE0, 0xA0 }, OperationStatus.NeedMoreData, 0xFFFD, 2)] // [ E0 A0 ] is valid 2-byte start of 3-byte sequence
        [InlineData(new byte[] { 0xED, 0x9F }, OperationStatus.NeedMoreData, 0xFFFD, 2)] // [ ED 9F ] is valid 2-byte start of 3-byte sequence
        [InlineData(new byte[] { 0xED, 0xBF }, OperationStatus.InvalidData, 0xFFFD, 1)] // [ ED BF ] would place us in UTF-16 surrogate range, all surrogate sequences have maximal invalid subsequence length 1
        [InlineData(new byte[] { 0xEE, 0x80 }, OperationStatus.NeedMoreData, 0xFFFD, 2)] // [ EE 80 ] is valid 2-byte start of 3-byte sequence
        [InlineData(new byte[] { 0xF0, 0x8F }, OperationStatus.InvalidData, 0xFFFD, 1)] // [ F0 8F ] is overlong 4-byte sequence, all overlong sequences have maximal invalid subsequence length 1
        [InlineData(new byte[] { 0xF0, 0x90 }, OperationStatus.NeedMoreData, 0xFFFD, 2)] // [ F0 90 ] is valid 2-byte start of 4-byte sequence
        [InlineData(new byte[] { 0xF4, 0x90 }, OperationStatus.InvalidData, 0xFFFD, 1)] // [ F4 90 ] would place us beyond U+10FFFF, all such sequences have maximal invalid subsequence length 1
        [InlineData(new byte[] { 0xE2, 0x88, 0xB4 }, OperationStatus.Done, 0x2234, 3)] // [ E2 88 B4 ] is U+2234 THEREFORE
        [InlineData(new byte[] { 0xE2, 0x88, 0xC0 }, OperationStatus.InvalidData, 0xFFFD, 2)] // [ E2 88 ] followed by non-continuation byte, maximal invalid subsequence length 2
        [InlineData(new byte[] { 0xF0, 0x9F, 0x98 }, OperationStatus.NeedMoreData, 0xFFFD, 3)] // [ F0 9F 98 ] is valid 3-byte start of 4-byte sequence
        [InlineData(new byte[] { 0xF0, 0x9F, 0x98, 0x20 }, OperationStatus.InvalidData, 0xFFFD, 3)] // [ F0 9F 98 ] followed by non-continuation byte, maximal invalid subsequence length 3
        [InlineData(new byte[] { 0xF0, 0x9F, 0x98, 0xB2 }, OperationStatus.Done, 0x1F632, 4)] // [ F0 9F 98 B2 ] is U+1F632 ASTONISHED FACE
        public static void DecodeFromUtf8(byte[] data, OperationStatus expectedOperationStatus, int expectedRuneValue, int expectedBytesConsumed)
        {
            Assert.Equal(expectedOperationStatus, Rune.DecodeFromUtf8(data, out Rune actualRune, out int actualBytesConsumed));
            Assert.Equal(expectedRuneValue, actualRune.Value);
            Assert.Equal(expectedBytesConsumed, actualBytesConsumed);
        }

        [Theory]
        [InlineData(new byte[0], OperationStatus.NeedMoreData, 0xFFFD, 0)] // empty buffer
        [InlineData(new byte[] { 0x30 }, OperationStatus.Done, 0x0030, 1)] // ASCII byte
        [InlineData(new byte[] { 0x30, 0x40, 0x50 }, OperationStatus.Done, 0x0050, 1)] // ASCII byte
        [InlineData(new byte[] { 0x80 }, OperationStatus.InvalidData, 0xFFFD, 1)] // standalone continuation byte
        [InlineData(new byte[] { 0x80, 0x80, 0x80 }, OperationStatus.InvalidData, 0xFFFD, 1)] // standalone continuation byte
        [InlineData(new byte[] { 0x80, 0x80, 0x80, 0x80 }, OperationStatus.InvalidData, 0xFFFD, 1)] // standalone continuation byte
        [InlineData(new byte[] { 0x80, 0x80, 0x80, 0xC2 }, OperationStatus.NeedMoreData, 0xFFFD, 1)] // [ C2 ] at end of buffer, valid 1-byte start of 2-byte sequence
        [InlineData(new byte[] { 0xC1 }, OperationStatus.InvalidData, 0xFFFD, 1)] // [ C1 ] is never a valid byte
        [InlineData(new byte[] { 0x80, 0xE2, 0x88, 0xB4 }, OperationStatus.Done, 0x2234, 3)] // [ E2 88 B4 ] is U+2234 THEREFORE
        [InlineData(new byte[] { 0xF0, 0x9F, 0x98, 0xB2 }, OperationStatus.Done, 0x1F632, 4)] // [ F0 9F 98 B2 ] is U+1F632 ASTONISHED FACE
        [InlineData(new byte[] { 0xE2, 0x88, 0xB4, 0xB2 }, OperationStatus.InvalidData, 0xFFFD, 1)] // [ B2 ] is standalone continuation byte
        [InlineData(new byte[] { 0x80, 0x62, 0x80, 0x80 }, OperationStatus.InvalidData, 0xFFFD, 1)] // [ 80 ] is standalone continuation byte
        [InlineData(new byte[] { 0xF0, 0x9F, 0x98, }, OperationStatus.NeedMoreData, 0xFFFD, 3)] // [ F0 9F 98 ] is valid 3-byte start of 4-byte sequence
        public static void DecodeLastFromUtf8(byte[] data, OperationStatus expectedOperationStatus, int expectedRuneValue, int expectedBytesConsumed)
        {
            Assert.Equal(expectedOperationStatus, Rune.DecodeLastFromUtf8(data, out Rune actualRune, out int actualBytesConsumed));
            Assert.Equal(expectedRuneValue, actualRune.Value);
            Assert.Equal(expectedBytesConsumed, actualBytesConsumed);
        }

        [Theory]
        [InlineData(0, 0, true)]
        [InlineData(0x10FFFF, 0x10FFFF, true)]
        [InlineData(0xFFFD, 0xFFFD, true)]
        [InlineData(0xFFFD, 0xFFFF, false)]
        [InlineData('a', 'a', true)]
        [InlineData('a', 'A', false)]
        [InlineData('a', 'b', false)]
        public static void Equals_OperatorEqual_OperatorNotEqual(int first, int other, bool expected)
        {
            Rune a = new Rune(first);
            Rune b = new Rune(other);

            Assert.Equal(expected, Object.Equals(a, b));
            Assert.Equal(expected, a.Equals(b));
            Assert.Equal(expected, a.Equals((object)b));
            Assert.Equal(expected, a == b);
            Assert.NotEqual(expected, a != b);
        }

        [Theory]
        [InlineData(0)]
        [InlineData('a')]
        [InlineData('\uFFFD')]
        [InlineData(0x10FFFF)]
        public static void GetHashCodeTests(int scalarValue)
        {
            Assert.Equal(scalarValue, new Rune(scalarValue).GetHashCode());
        }

        [Theory]
        [InlineData("a", 0, (int)'a')]
        [InlineData("ab", 1, (int)'b')]
        [InlineData("x\U0001F46Ey", 3, (int)'y')]
        [InlineData("x\U0001F46Ey", 1, 0x1F46E)] // U+1F46E POLICE OFFICER
        public static void GetRuneAt_TryGetRuneAt_Utf16_Success(string inputString, int index, int expectedScalarValue)
        {
            // GetRuneAt
            Assert.Equal(expectedScalarValue, Rune.GetRuneAt(inputString, index).Value);

            // TryGetRuneAt
            Assert.True(Rune.TryGetRuneAt(inputString, index, out Rune rune));
            Assert.Equal(expectedScalarValue, rune.Value);
        }

        // Our unit test runner doesn't deal well with malformed literal strings, so
        // we smuggle it as a char[] and turn it into a string within the test itself.
        [Theory]
        [InlineData(new char[] { 'x', '\uD83D', '\uDC6E', 'y' }, 2)] // attempt to index into the middle of a UTF-16 surrogate pair
        [InlineData(new char[] { 'x', '\uD800', 'y' }, 1)] // high surrogate not followed by low surrogate
        [InlineData(new char[] { 'x', '\uDFFF', '\uDFFF' }, 1)] // attempt to start at a low surrogate
        [InlineData(new char[] { 'x', '\uD800' }, 1)] // end of string reached before could complete surrogate pair
        public static void GetRuneAt_TryGetRuneAt_Utf16_InvalidData(char[] inputCharArray, int index)
        {
            string inputString = new string(inputCharArray);

            // GetRuneAt
            Assert.Throws<ArgumentException>("index", () => Rune.GetRuneAt(inputString, index));

            // TryGetRuneAt
            Assert.False(Rune.TryGetRuneAt(inputString, index, out Rune rune));
            Assert.Equal(0, rune.Value);
        }

        [Fact]
        public static void GetRuneAt_TryGetRuneAt_Utf16_BadArgs()
        {
            // null input
            Assert.Throws<ArgumentNullException>("input", () => Rune.GetRuneAt(null, 0));

            // negative index specified
            Assert.Throws<ArgumentOutOfRangeException>("index", () => Rune.GetRuneAt("hello", -1));

            // index goes past end of string
            Assert.Throws<ArgumentOutOfRangeException>("index", () => Rune.GetRuneAt(string.Empty, 0));
        }

        [Theory]
        [MemberData(nameof(UnicodeInfoTestData_Latin1AndSelectOthers))]
        public static void GetNumericValue(UnicodeInfoTestData testData)
        {
            Assert.Equal(testData.NumericValue, Rune.GetNumericValue(testData.ScalarValue));
        }

        [Theory]
        [MemberData(nameof(UnicodeInfoTestData_Latin1AndSelectOthers))]
        public static void GetUnicodeCategory(UnicodeInfoTestData testData)
        {
            Assert.Equal(testData.UnicodeCategory, Rune.GetUnicodeCategory(testData.ScalarValue));
        }

        [Theory]
        [MemberData(nameof(UnicodeInfoTestData_Latin1AndSelectOthers))]
        public static void IsControl(UnicodeInfoTestData testData)
        {
            Assert.Equal(testData.IsControl, Rune.IsControl(testData.ScalarValue));
        }

        [Theory]
        [MemberData(nameof(UnicodeInfoTestData_Latin1AndSelectOthers))]
        public static void IsDigit(UnicodeInfoTestData testData)
        {
            Assert.Equal(testData.IsDigit, Rune.IsDigit(testData.ScalarValue));
        }

        [Theory]
        [MemberData(nameof(UnicodeInfoTestData_Latin1AndSelectOthers))]
        public static void IsLetter(UnicodeInfoTestData testData)
        {
            Assert.Equal(testData.IsLetter, Rune.IsLetter(testData.ScalarValue));
        }

        [Theory]
        [MemberData(nameof(UnicodeInfoTestData_Latin1AndSelectOthers))]
        public static void IsLetterOrDigit(UnicodeInfoTestData testData)
        {
            Assert.Equal(testData.IsLetterOrDigit, Rune.IsLetterOrDigit(testData.ScalarValue));
        }

        [Theory]
        [MemberData(nameof(UnicodeInfoTestData_Latin1AndSelectOthers))]
        public static void IsLower(UnicodeInfoTestData testData)
        {
            Assert.Equal(testData.IsLower, Rune.IsLower(testData.ScalarValue));
        }

        [Theory]
        [MemberData(nameof(UnicodeInfoTestData_Latin1AndSelectOthers))]
        public static void IsNumber(UnicodeInfoTestData testData)
        {
            Assert.Equal(testData.IsNumber, Rune.IsNumber(testData.ScalarValue));
        }

        [Theory]
        [MemberData(nameof(UnicodeInfoTestData_Latin1AndSelectOthers))]
        public static void IsPunctuation(UnicodeInfoTestData testData)
        {
            Assert.Equal(testData.IsPunctuation, Rune.IsPunctuation(testData.ScalarValue));
        }

        [Theory]
        [MemberData(nameof(UnicodeInfoTestData_Latin1AndSelectOthers))]
        public static void IsSeparator(UnicodeInfoTestData testData)
        {
            Assert.Equal(testData.IsSeparator, Rune.IsSeparator(testData.ScalarValue));
        }

        [Theory]
        [MemberData(nameof(UnicodeInfoTestData_Latin1AndSelectOthers))]
        public static void IsSymbol(UnicodeInfoTestData testData)
        {
            Assert.Equal(testData.IsSymbol, Rune.IsSymbol(testData.ScalarValue));
        }

        [Theory]
        [MemberData(nameof(UnicodeInfoTestData_Latin1AndSelectOthers))]
        public static void IsUpper(UnicodeInfoTestData testData)
        {
            Assert.Equal(testData.IsUpper, Rune.IsUpper(testData.ScalarValue));
        }

        [Theory]
        [MemberData(nameof(IsValidTestData))]
        public static void IsValid(int scalarValue, bool expectedIsValid)
        {
            Assert.Equal(expectedIsValid, Rune.IsValid(scalarValue));
            Assert.Equal(expectedIsValid, Rune.IsValid((uint)scalarValue));
        }

        [Theory]
        [MemberData(nameof(UnicodeInfoTestData_Latin1AndSelectOthers))]
        public static void IsWhiteSpace(UnicodeInfoTestData testData)
        {
            Assert.Equal(testData.IsWhiteSpace, Rune.IsWhiteSpace(testData.ScalarValue));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0x80, 0x80)]
        [InlineData(0x80, 0x100)]
        [InlineData(0x100, 0x80)]
        public static void Operators_And_CompareTo(uint scalarValueLeft, uint scalarValueRight)
        {
            Rune left = new Rune(scalarValueLeft);
            Rune right = new Rune(scalarValueRight);

            Assert.Equal(scalarValueLeft == scalarValueRight, left == right);
            Assert.Equal(scalarValueLeft != scalarValueRight, left != right);
            Assert.Equal(scalarValueLeft < scalarValueRight, left < right);
            Assert.Equal(scalarValueLeft <= scalarValueRight, left <= right);
            Assert.Equal(scalarValueLeft > scalarValueRight, left > right);
            Assert.Equal(scalarValueLeft >= scalarValueRight, left >= right);
            Assert.Equal(scalarValueLeft.CompareTo(scalarValueRight), left.CompareTo(right));
        }

        [Fact]
        public static void ReplacementChar()
        {
            Assert.Equal(0xFFFD, Rune.ReplacementChar.Value);
        }

        [Theory]
        [MemberData(nameof(GeneralTestData_BmpCodePoints_NoSurrogates))]
        public static void TryCreate_Char_Valid(GeneralTestData testData)
        {
            Assert.True(Rune.TryCreate((char)testData.ScalarValue, out Rune result));
            Assert.Equal(testData.ScalarValue, result.Value);
        }

        [Theory]
        [MemberData(nameof(BmpCodePoints_SurrogatesOnly))]
        public static void TryCreate_Char_Invalid(int scalarValue)
        {
            Assert.False(Rune.TryCreate((char)scalarValue, out Rune result));
            Assert.Equal(0, result.Value);
        }

        [Theory]
        [MemberData(nameof(SurrogatePairTestData_InvalidOnly))]
        public static void TryCreate_SurrogateChars_Invalid(char highSurrogate, char lowSurrogate)
        {
            Assert.False(Rune.TryCreate(highSurrogate, lowSurrogate, out Rune result));
            Assert.Equal(0, result.Value);
        }

        [Theory]
        [MemberData(nameof(SurrogatePairTestData_ValidOnly))]
        public static void TryCreate_SurrogateChars_Valid(char highSurrogate, char lowSurrogate, int expectedValue)
        {
            Assert.True(Rune.TryCreate(highSurrogate, lowSurrogate, out Rune result));
            Assert.Equal(expectedValue, result.Value);
        }

        [Theory]
        [MemberData(nameof(GeneralTestData_BmpCodePoints_NoSurrogates))]
        [MemberData(nameof(GeneralTestData_SupplementaryCodePoints_ValidOnly))]
        public static void TryCreate_Int32_Valid(GeneralTestData testData)
        {
            Assert.True(Rune.TryCreate((int)testData.ScalarValue, out Rune result));
            Assert.Equal(testData.ScalarValue, result.Value);
        }

        [Theory]
        [MemberData(nameof(BmpCodePoints_SurrogatesOnly))]
        [MemberData(nameof(SupplementaryCodePoints_InvalidOnly))]
        public static void TryCreate_Int32_Invalid(int scalarValue)
        {
            Assert.False(Rune.TryCreate((int)scalarValue, out Rune result));
            Assert.Equal(0, result.Value);
        }

        [Theory]
        [MemberData(nameof(GeneralTestData_BmpCodePoints_NoSurrogates))]
        [MemberData(nameof(GeneralTestData_SupplementaryCodePoints_ValidOnly))]
        public static void TryCreate_UInt32_Valid(GeneralTestData testData)
        {
            Assert.True(Rune.TryCreate((uint)testData.ScalarValue, out Rune result));
            Assert.Equal(testData.ScalarValue, result.Value);
        }

        [Theory]
        [MemberData(nameof(BmpCodePoints_SurrogatesOnly))]
        [MemberData(nameof(SupplementaryCodePoints_InvalidOnly))]
        public static void TryCreate_UInt32_Invalid(int scalarValue)
        {
            Assert.False(Rune.TryCreate((uint)scalarValue, out Rune result));
            Assert.Equal(0, result.Value);
        }

        [Theory]
        [MemberData(nameof(GeneralTestData_BmpCodePoints_NoSurrogates))]
        [MemberData(nameof(GeneralTestData_SupplementaryCodePoints_ValidOnly))]
        public static void ToStringTests(GeneralTestData testData)
        {
            Assert.Equal(new string(testData.Utf16Sequence), new Rune(testData.ScalarValue).ToString());
        }

        [Theory]
        [MemberData(nameof(GeneralTestData_BmpCodePoints_NoSurrogates))]
        [MemberData(nameof(GeneralTestData_SupplementaryCodePoints_ValidOnly))]
        public static void TryEncodeToUtf16(GeneralTestData testData)
        {
            Rune rune = new Rune(testData.ScalarValue);
            Assert.Equal(testData.Utf16Sequence.Length, rune.Utf16SequenceLength);

            // First, try with a buffer that's too short

            Span<char> utf16Buffer = stackalloc char[rune.Utf16SequenceLength - 1];
            bool success = rune.TryEncodeToUtf16(utf16Buffer, out int charsWritten);
            Assert.False(success);
            Assert.Equal(0, charsWritten);

            Assert.Throws<ArgumentException>("destination", () => rune.EncodeToUtf16(new char[rune.Utf16SequenceLength - 1]));

            // Then, try with a buffer that's appropriately sized

            utf16Buffer = stackalloc char[rune.Utf16SequenceLength];
            success = rune.TryEncodeToUtf16(utf16Buffer, out charsWritten);
            Assert.True(success);
            Assert.Equal(testData.Utf16Sequence.Length, charsWritten);
            Assert.True(utf16Buffer.SequenceEqual(testData.Utf16Sequence));

            utf16Buffer.Clear();
            Assert.Equal(testData.Utf16Sequence.Length, rune.EncodeToUtf16(utf16Buffer));
            Assert.True(utf16Buffer.SequenceEqual(testData.Utf16Sequence));

            // Finally, try with a buffer that's too long (should succeed)

            utf16Buffer = stackalloc char[rune.Utf16SequenceLength + 1];
            success = rune.TryEncodeToUtf16(utf16Buffer, out charsWritten);
            Assert.True(success);
            Assert.Equal(testData.Utf16Sequence.Length, charsWritten);
            Assert.True(utf16Buffer.Slice(0, testData.Utf16Sequence.Length).SequenceEqual(testData.Utf16Sequence));

            utf16Buffer.Clear();
            Assert.Equal(testData.Utf16Sequence.Length, rune.EncodeToUtf16(utf16Buffer));
            Assert.True(utf16Buffer.Slice(0, testData.Utf16Sequence.Length).SequenceEqual(testData.Utf16Sequence));
        }

        [Theory]
        [MemberData(nameof(GeneralTestData_BmpCodePoints_NoSurrogates))]
        [MemberData(nameof(GeneralTestData_SupplementaryCodePoints_ValidOnly))]
        public static void TryEncodeToUtf8(GeneralTestData testData)
        {
            Rune rune = new Rune(testData.ScalarValue);
            Assert.Equal(testData.Utf8Sequence.Length, actual: rune.Utf8SequenceLength);

            // First, try with a buffer that's too short

            Span<byte> utf8Buffer = stackalloc byte[rune.Utf8SequenceLength - 1];
            bool success = rune.TryEncodeToUtf8(utf8Buffer, out int bytesWritten);
            Assert.False(success);
            Assert.Equal(0, bytesWritten);

            Assert.Throws<ArgumentException>("destination", () => rune.EncodeToUtf8(new byte[rune.Utf8SequenceLength - 1]));

            // Then, try with a buffer that's appropriately sized

            utf8Buffer = stackalloc byte[rune.Utf8SequenceLength];
            success = rune.TryEncodeToUtf8(utf8Buffer, out bytesWritten);
            Assert.True(success);
            Assert.Equal(testData.Utf8Sequence.Length, bytesWritten);
            Assert.True(utf8Buffer.SequenceEqual(testData.Utf8Sequence));

            utf8Buffer.Clear();
            Assert.Equal(testData.Utf8Sequence.Length, rune.EncodeToUtf8(utf8Buffer));
            Assert.True(utf8Buffer.SequenceEqual(testData.Utf8Sequence));

            // Finally, try with a buffer that's too long (should succeed)

            utf8Buffer = stackalloc byte[rune.Utf8SequenceLength + 1];
            success = rune.TryEncodeToUtf8(utf8Buffer, out bytesWritten);
            Assert.True(success);
            Assert.Equal(testData.Utf8Sequence.Length, bytesWritten);
            Assert.True(utf8Buffer.Slice(0, testData.Utf8Sequence.Length).SequenceEqual(testData.Utf8Sequence));

            utf8Buffer.Clear();
            Assert.Equal(testData.Utf8Sequence.Length, rune.EncodeToUtf8(utf8Buffer));
            Assert.True(utf8Buffer.Slice(0, testData.Utf8Sequence.Length).SequenceEqual(testData.Utf8Sequence));
        }
    }
}
