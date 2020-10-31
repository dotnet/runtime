// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace System.Text.Unicode.Tests
{
    public class Utf8Tests
    {
        private const string X_UTF8 = "58"; // U+0058 LATIN CAPITAL LETTER X, 1 byte
        private const string X_UTF16 = "X";

        private const string Y_UTF8 = "59"; // U+0058 LATIN CAPITAL LETTER Y, 1 byte
        private const string Y_UTF16 = "Y";

        private const string Z_UTF8 = "5A"; // U+0058 LATIN CAPITAL LETTER Z, 1 byte
        private const string Z_UTF16 = "Z";

        private const string E_ACUTE_UTF8 = "C3A9"; // U+00E9 LATIN SMALL LETTER E WITH ACUTE, 2 bytes
        private const string E_ACUTE_UTF16 = "\u00E9";

        private const string EURO_SYMBOL_UTF8 = "E282AC"; // U+20AC EURO SIGN, 3 bytes
        private const string EURO_SYMBOL_UTF16 = "\u20AC";

        private const string REPLACEMENT_CHAR_UTF8 = "EFBFBD"; // U+FFFD REPLACEMENT CHAR, 3 bytes
        private const string REPLACEMENT_CHAR_UTF16 = "\uFFFD";

        private const string GRINNING_FACE_UTF8 = "F09F9880"; // U+1F600 GRINNING FACE, 4 bytes
        private const string GRINNING_FACE_UTF16 = "\U0001F600";

        private const string WOMAN_CARTWHEELING_MEDSKIN_UTF16 = "\U0001F938\U0001F3FD\u200D\u2640\uFE0F"; // U+1F938 U+1F3FD U+200D U+2640 U+FE0F WOMAN CARTWHEELING: MEDIUM SKIN TONE

        // All valid scalars [ U+0000 .. U+D7FF ] and [ U+E000 .. U+10FFFF ].
        private static readonly IEnumerable<Rune> s_allValidScalars = Enumerable.Range(0x0000, 0xD800).Concat(Enumerable.Range(0xE000, 0x110000 - 0xE000)).Select(value => new Rune(value));

        private static readonly ReadOnlyMemory<char> s_allScalarsAsUtf16;
        private static readonly ReadOnlyMemory<byte> s_allScalarsAsUtf8;

        static Utf8Tests()
        {
            List<char> allScalarsAsUtf16 = new List<char>();
            List<byte> allScalarsAsUtf8 = new List<byte>();

            foreach (Rune rune in s_allValidScalars)
            {
                allScalarsAsUtf16.AddRange(ToUtf16(rune));
                allScalarsAsUtf8.AddRange(ToUtf8(rune));
            }

            s_allScalarsAsUtf16 = allScalarsAsUtf16.ToArray().AsMemory();
            s_allScalarsAsUtf8 = allScalarsAsUtf8.ToArray().AsMemory();
        }

        /*
         * COMMON UTILITIES FOR UNIT TESTS
         */

        public static byte[] DecodeHex(ReadOnlySpan<char> inputHex)
        {
            Assert.True(Regex.IsMatch(inputHex.ToString(), "^([0-9a-fA-F]{2})*$"), "Input must be an even number of hex characters.");

            return Convert.FromHexString(inputHex);
        }

        // !! IMPORTANT !!
        // Don't delete this implementation, as we use it as a reference to make sure the framework's
        // transcoding logic is correct.
        public static byte[] ToUtf8(Rune rune)
        {
            Assert.True(Rune.IsValid(rune.Value), $"Rune with value U+{(uint)rune.Value:X4} is not well-formed.");

            if (rune.Value < 0x80)
            {
                return new[]
                {
                    (byte)rune.Value
                };
            }
            else if (rune.Value < 0x0800)
            {
                return new[]
                {
                    (byte)((rune.Value >> 6) | 0xC0),
                    (byte)((rune.Value & 0x3F) | 0x80)
                };
            }
            else if (rune.Value < 0x10000)
            {
                return new[]
                {
                    (byte)((rune.Value >> 12) | 0xE0),
                    (byte)(((rune.Value >> 6) & 0x3F) | 0x80),
                    (byte)((rune.Value & 0x3F) | 0x80)
                };
            }
            else
            {
                return new[]
                {
                    (byte)((rune.Value >> 18) | 0xF0),
                    (byte)(((rune.Value >> 12) & 0x3F) | 0x80),
                    (byte)(((rune.Value >> 6) & 0x3F) | 0x80),
                    (byte)((rune.Value & 0x3F) | 0x80)
                };
            }
        }

        // !! IMPORTANT !!
        // Don't delete this implementation, as we use it as a reference to make sure the framework's
        // transcoding logic is correct.
        private static char[] ToUtf16(Rune rune)
        {
            Assert.True(Rune.IsValid(rune.Value), $"Rune with value U+{(uint)rune.Value:X4} is not well-formed.");

            if (rune.IsBmp)
            {
                return new[]
                {
                    (char)rune.Value
                };
            }
            else
            {
                return new[]
                {
                    (char)((rune.Value >> 10) + 0xD800 - 0x40),
                    (char)((rune.Value & 0x03FF) + 0xDC00)
                };
            }
        }

        [Theory]
        [InlineData("", "")] // empty string is OK
        [InlineData(X_UTF16, X_UTF8)]
        [InlineData(E_ACUTE_UTF16, E_ACUTE_UTF8)]
        [InlineData(EURO_SYMBOL_UTF16, EURO_SYMBOL_UTF8)]
        public void ToBytes_WithSmallValidBuffers(string utf16Input, string expectedUtf8TranscodingHex)
        {
            // These test cases are for the "slow processing" code path at the end of TranscodeToUtf8,
            // so inputs should be less than 2 chars.

            Assert.InRange(utf16Input.Length, 0, 1);

            ToBytes_Test_Core(
                utf16Input: utf16Input,
                destinationSize: expectedUtf8TranscodingHex.Length / 2,
                replaceInvalidSequences: false,
                isFinalChunk: false,
                expectedOperationStatus: OperationStatus.Done,
                expectedNumCharsRead: utf16Input.Length,
                expectedUtf8Transcoding: DecodeHex(expectedUtf8TranscodingHex));
        }

        [Theory]
        [InlineData("AB")] // 2 ASCII chars, hits fast inner loop
        [InlineData("ABCD")] // 4 ASCII chars, hits fast inner loop
        [InlineData("ABCDEF")] // 6 ASCII chars, hits fast inner loop
        [InlineData("ABCDEFGH")] // 8 ASCII chars, hits fast inner loop
        [InlineData("ABCDEFGHIJ")] // 10 ASCII chars, hits fast inner loop
        [InlineData("ABCDEF" + E_ACUTE_UTF16 + "HIJ")] // interrupts inner loop due to non-ASCII char in first char of first DWORD
        [InlineData("ABCDEFG" + EURO_SYMBOL_UTF16 + "IJ")] // interrupts inner loop due to non-ASCII char in second char of first DWORD
        [InlineData("ABCDEFGH" + E_ACUTE_UTF16 + "J")] // interrupts inner loop due to non-ASCII char in first char of second DWORD
        [InlineData("ABCDEFGHI" + EURO_SYMBOL_UTF16)] // interrupts inner loop due to non-ASCII char in second char of second DWORD
        [InlineData(X_UTF16 + E_ACUTE_UTF16)] // drains first ASCII char then falls down to slow path
        [InlineData(X_UTF16 + E_ACUTE_UTF16 + E_ACUTE_UTF16)] // drains first ASCII char then consumes 2x 2-byte sequences at once
        [InlineData(E_ACUTE_UTF16 + X_UTF16)] // no first ASCII char to drain, consumes 2-byte seq followed by ASCII char
        [InlineData(E_ACUTE_UTF16 + E_ACUTE_UTF16 + E_ACUTE_UTF16 + E_ACUTE_UTF16)] // stay within 2x 2-byte sequence processing loop
        [InlineData(E_ACUTE_UTF16 + E_ACUTE_UTF16 + E_ACUTE_UTF16 + X_UTF16)] // break out of 2x 2-byte seq loop due to ASCII data in second char of DWORD
        [InlineData(E_ACUTE_UTF16 + E_ACUTE_UTF16 + X_UTF16 + X_UTF16)] // break out of 2x 2-byte seq loop due to ASCII data in first char of DWORD
        [InlineData(E_ACUTE_UTF16 + E_ACUTE_UTF16 + E_ACUTE_UTF16 + EURO_SYMBOL_UTF16)] // break out of 2x 2-byte seq loop due to 3-byte data
        [InlineData(E_ACUTE_UTF16 + EURO_SYMBOL_UTF16)] // 2-byte logic sees next char isn't ASCII, cannot read full DWORD from remaining input buffer, falls down to slow drain loop
        [InlineData(EURO_SYMBOL_UTF16 + EURO_SYMBOL_UTF16 + X_UTF16)] // 2x 3-byte logic can't read a full DWORD from next part of buffer, falls down to slow drain loop
        [InlineData(EURO_SYMBOL_UTF16 + X_UTF16)] // 3-byte processing loop consumes trailing ASCII char, but can't read next DWORD, falls down to slow drain loop
        [InlineData(EURO_SYMBOL_UTF16 + X_UTF16 + X_UTF16)] // 3-byte processing loop consumes trailing ASCII char, but can't read next DWORD, falls down to slow drain loop
        [InlineData(EURO_SYMBOL_UTF16 + E_ACUTE_UTF16)] // 3-byte processing loop can't consume next ASCII char, can't read DWORD, falls down to slow drain loop
        [InlineData(EURO_SYMBOL_UTF16 + EURO_SYMBOL_UTF16 + EURO_SYMBOL_UTF16 + EURO_SYMBOL_UTF16)] // stay within 2x 3-byte sequence processing loop
        [InlineData(EURO_SYMBOL_UTF16 + EURO_SYMBOL_UTF16 + X_UTF16 + EURO_SYMBOL_UTF16 + EURO_SYMBOL_UTF16 + EURO_SYMBOL_UTF16)] // consume stray ASCII char at beginning of DWORD after 2x 3-byte sequence
        [InlineData(EURO_SYMBOL_UTF16 + EURO_SYMBOL_UTF16 + EURO_SYMBOL_UTF16 + X_UTF16 + EURO_SYMBOL_UTF16 + EURO_SYMBOL_UTF16)] // consume stray ASCII char at end of DWORD after 2x 3-byte sequence
        [InlineData(EURO_SYMBOL_UTF16 + E_ACUTE_UTF16 + X_UTF16)] // consume 2-byte sequence as second char in DWORD which begins with 3-byte encoded char
        [InlineData(EURO_SYMBOL_UTF16 + GRINNING_FACE_UTF16)] // 3-byte sequence followed by 4-byte sequence
        [InlineData(EURO_SYMBOL_UTF16 + EURO_SYMBOL_UTF16 + GRINNING_FACE_UTF16)] // 2x 3-byte sequence followed by 4-byte sequence
        [InlineData(GRINNING_FACE_UTF16)] // single 4-byte surrogate char pair
        [InlineData(GRINNING_FACE_UTF16 + EURO_SYMBOL_UTF16)] // 4-byte surrogate char pair, cannot read next DWORD, falls down to slow drain loop
        public void ToBytes_WithLargeValidBuffers(string utf16Input)
        {
            // These test cases are for the "fast processing" code which is the main loop of TranscodeToUtf8,
            // so inputs should be at least 2 chars.

            Assert.True(utf16Input.Length >= 2);

            // We're going to run the tests with destination buffer lengths ranging from 0 all the way
            // to buffers large enough to hold the full output. This allows us to test logic that
            // detects whether we're about to overrun our destination buffer and instead returns DestinationTooSmall.

            Rune[] enumeratedScalars = utf16Input.EnumerateRunes().ToArray();

            // 0-length buffer test
            ToBytes_Test_Core(
                utf16Input: utf16Input,
                destinationSize: 0,
                replaceInvalidSequences: false,
                isFinalChunk: false,
                expectedOperationStatus: OperationStatus.DestinationTooSmall,
                expectedNumCharsRead: 0,
                expectedUtf8Transcoding: ReadOnlySpan<byte>.Empty);

            int expectedNumCharsConsumed = 0;
            byte[] concatenatedUtf8 = Array.Empty<byte>();

            for (int i = 0; i < enumeratedScalars.Length; i++)
            {
                Rune thisScalar = enumeratedScalars[i];

                // provide partial destination buffers all the way up to (but not including) enough to hold the next full scalar encoding
                for (int j = 1; j < thisScalar.Utf8SequenceLength; j++)
                {
                    ToBytes_Test_Core(
                        utf16Input: utf16Input,
                        destinationSize: concatenatedUtf8.Length + j,
                        replaceInvalidSequences: false,
                        isFinalChunk: false,
                        expectedOperationStatus: OperationStatus.DestinationTooSmall,
                        expectedNumCharsRead: expectedNumCharsConsumed,
                        expectedUtf8Transcoding: concatenatedUtf8);
                }

                // now provide a destination buffer large enough to hold the next full scalar encoding

                expectedNumCharsConsumed += thisScalar.Utf16SequenceLength;
                concatenatedUtf8 = concatenatedUtf8.Concat(ToUtf8(thisScalar)).ToArray();

                ToBytes_Test_Core(
                   utf16Input: utf16Input,
                   destinationSize: concatenatedUtf8.Length,
                   replaceInvalidSequences: false,
                   isFinalChunk: false,
                   expectedOperationStatus: (i == enumeratedScalars.Length - 1) ? OperationStatus.Done : OperationStatus.DestinationTooSmall,
                   expectedNumCharsRead: expectedNumCharsConsumed,
                   expectedUtf8Transcoding: concatenatedUtf8);
            }

            // now throw lots of ASCII data at the beginning so that we exercise the vectorized code paths

            utf16Input = new string('x', 64) + utf16Input;
            concatenatedUtf8 = utf16Input.EnumerateRunes().SelectMany(ToUtf8).ToArray();

            ToBytes_Test_Core(
                utf16Input: utf16Input,
                destinationSize: concatenatedUtf8.Length,
                replaceInvalidSequences: false,
                isFinalChunk: true,
                expectedOperationStatus: OperationStatus.Done,
                expectedNumCharsRead: utf16Input.Length,
                expectedUtf8Transcoding: concatenatedUtf8);

            // now throw some non-ASCII data at the beginning so that we *don't* exercise the vectorized code paths

            utf16Input = WOMAN_CARTWHEELING_MEDSKIN_UTF16 + utf16Input[64..];
            concatenatedUtf8 = utf16Input.EnumerateRunes().SelectMany(ToUtf8).ToArray();

            ToBytes_Test_Core(
                utf16Input: utf16Input,
                destinationSize: concatenatedUtf8.Length,
                replaceInvalidSequences: false,
                isFinalChunk: true,
                expectedOperationStatus: OperationStatus.Done,
                expectedNumCharsRead: utf16Input.Length,
                expectedUtf8Transcoding: concatenatedUtf8);
        }

        [Theory]
        [InlineData('\uD800', OperationStatus.NeedMoreData)] // standalone high surrogate
        [InlineData('\uDFFF', OperationStatus.InvalidData)] // standalone low surrogate
        public void ToBytes_WithOnlyStandaloneSurrogates(char charValue, OperationStatus expectedOperationStatus)
        {
            ToBytes_Test_Core(
                utf16Input: new[] { charValue },
                destinationSize: 0,
                replaceInvalidSequences: false,
                isFinalChunk: false,
                expectedOperationStatus: expectedOperationStatus,
                expectedNumCharsRead: 0,
                expectedUtf8Transcoding: Span<byte>.Empty);
        }

        [Theory]
        [InlineData("<LOW><HIGH>", 0, "")] // swapped surrogate pair characters
        [InlineData("A<LOW><HIGH>", 1, "41")] // consume standalone ASCII char, then swapped surrogate pair characters
        [InlineData("A<HIGH>B", 1, "41")] // consume standalone ASCII char, then standalone high surrogate char
        [InlineData("A<LOW>B", 1, "41")] // consume standalone ASCII char, then standalone low surrogate char
        [InlineData("AB<HIGH><HIGH>", 2, "4142")] // consume two ASCII chars, then standalone high surrogate char
        [InlineData("AB<LOW><LOW>", 2, "4142")] // consume two ASCII chars, then standalone low surrogate char
        public void ToBytes_WithInvalidSurrogates(string utf16Input, int expectedNumCharsConsumed, string expectedUtf8TranscodingHex)
        {
            // xUnit can't handle ill-formed strings in [InlineData], so we replace here.

            utf16Input = utf16Input.Replace("<HIGH>", "\uD800").Replace("<LOW>", "\uDFFF");

            // These test cases are for the "fast processing" code which is the main loop of TranscodeToUtf8,
            // so inputs should be at least 2 chars.

            Assert.True(utf16Input.Length >= 2);

            ToBytes_Test_Core(
                utf16Input: utf16Input,
                destinationSize: expectedUtf8TranscodingHex.Length / 2,
                replaceInvalidSequences: false,
                isFinalChunk: false,
                expectedOperationStatus: OperationStatus.InvalidData,
                expectedNumCharsRead: expectedNumCharsConsumed,
                expectedUtf8Transcoding: DecodeHex(expectedUtf8TranscodingHex));

            // Now try the tests again with a larger buffer.
            // This ensures that running out of destination space wasn't the reason we failed.

            ToBytes_Test_Core(
                utf16Input: utf16Input,
                destinationSize: (expectedUtf8TranscodingHex.Length) / 2 + 16,
                replaceInvalidSequences: false,
                isFinalChunk: false,
                expectedOperationStatus: OperationStatus.InvalidData,
                expectedNumCharsRead: expectedNumCharsConsumed,
                expectedUtf8Transcoding: DecodeHex(expectedUtf8TranscodingHex));
        }

        [Theory]
        [InlineData("<LOW><HIGH>", REPLACEMENT_CHAR_UTF8)] // standalone low surr. and incomplete high surr.
        [InlineData("<HIGH><HIGH>", REPLACEMENT_CHAR_UTF8)] // standalone high surr. and incomplete high surr.
        [InlineData("<LOW><LOW>", REPLACEMENT_CHAR_UTF8 + REPLACEMENT_CHAR_UTF8)] // standalone low surr. and incomplete low surr.
        [InlineData("A<LOW>B<LOW>C<HIGH>D", "41" + REPLACEMENT_CHAR_UTF8 + "42" + REPLACEMENT_CHAR_UTF8 + "43" + REPLACEMENT_CHAR_UTF8 + "44")] // standalone low, low, high surrounded by other data
        public void ToBytes_WithReplacements(string utf16Input, string expectedUtf8TranscodingHex)
        {
            // xUnit can't handle ill-formed strings in [InlineData], so we replace here.

            utf16Input = utf16Input.Replace("<HIGH>", "\uD800").Replace("<LOW>", "\uDFFF");

            bool isFinalCharHighSurrogate = char.IsHighSurrogate(utf16Input.Last());

            ToBytes_Test_Core(
                utf16Input: utf16Input,
                destinationSize: expectedUtf8TranscodingHex.Length / 2,
                replaceInvalidSequences: true,
                isFinalChunk: false,
                expectedOperationStatus: (isFinalCharHighSurrogate) ? OperationStatus.NeedMoreData : OperationStatus.Done,
                expectedNumCharsRead: (isFinalCharHighSurrogate) ? (utf16Input.Length - 1) : utf16Input.Length,
                expectedUtf8Transcoding: DecodeHex(expectedUtf8TranscodingHex));

            if (isFinalCharHighSurrogate)
            {
                // Also test with isFinalChunk = true
                ToBytes_Test_Core(
                    utf16Input: utf16Input,
                    destinationSize: expectedUtf8TranscodingHex.Length / 2 + Rune.ReplacementChar.Utf8SequenceLength /* for replacement char */,
                    replaceInvalidSequences: true,
                    isFinalChunk: true,
                    expectedOperationStatus: OperationStatus.Done,
                    expectedNumCharsRead: utf16Input.Length,
                    expectedUtf8Transcoding: DecodeHex(expectedUtf8TranscodingHex + REPLACEMENT_CHAR_UTF8));
            }
        }

        [Theory]
        [InlineData(E_ACUTE_UTF16 + "<LOW>", true, 1, OperationStatus.DestinationTooSmall, E_ACUTE_UTF8)] // not enough output buffer to hold U+FFFD
        [InlineData(E_ACUTE_UTF16 + "<LOW>", true, 2, OperationStatus.Done, E_ACUTE_UTF8 + REPLACEMENT_CHAR_UTF8)] // replace standalone low surr. at end
        [InlineData(E_ACUTE_UTF16 + "<HIGH>", true, 1, OperationStatus.DestinationTooSmall, E_ACUTE_UTF8)] // not enough output buffer to hold U+FFFD
        [InlineData(E_ACUTE_UTF16 + "<HIGH>", true, 2, OperationStatus.Done, E_ACUTE_UTF8 + REPLACEMENT_CHAR_UTF8)] // replace standalone high surr. at end
        [InlineData(E_ACUTE_UTF16 + "<HIGH>", false, 1, OperationStatus.NeedMoreData, E_ACUTE_UTF8)] // don't replace standalone high surr. at end
        [InlineData(E_ACUTE_UTF16 + "<HIGH>" + X_UTF16, true, 2, OperationStatus.DestinationTooSmall, E_ACUTE_UTF8 + REPLACEMENT_CHAR_UTF8)] // not enough output buffer to hold 'X'
        [InlineData(E_ACUTE_UTF16 + "<HIGH>" + X_UTF16, false, 2, OperationStatus.DestinationTooSmall, E_ACUTE_UTF8 + REPLACEMENT_CHAR_UTF8)] // not enough output buffer to hold 'X'
        [InlineData(E_ACUTE_UTF16 + "<HIGH>" + X_UTF16, true, 3, OperationStatus.Done, E_ACUTE_UTF8 + REPLACEMENT_CHAR_UTF8 + X_UTF8)] // replacement followed by 'X'
        [InlineData(E_ACUTE_UTF16 + "<HIGH>" + X_UTF16, false, 3, OperationStatus.Done, E_ACUTE_UTF8 + REPLACEMENT_CHAR_UTF8 + X_UTF8)] // replacement followed by 'X'
        public void ToBytes_WithReplacements_AndCustomBufferSizes(string utf16Input, bool isFinalChunk, int expectedNumCharsConsumed, OperationStatus expectedOperationStatus, string expectedUtf8TranscodingHex)
        {
            // xUnit can't handle ill-formed strings in [InlineData], so we replace here.

            utf16Input = utf16Input.Replace("<HIGH>", "\uD800").Replace("<LOW>", "\uDFFF");

            ToBytes_Test_Core(
                utf16Input: utf16Input,
                destinationSize: expectedUtf8TranscodingHex.Length / 2,
                replaceInvalidSequences: true,
                isFinalChunk: isFinalChunk,
                expectedOperationStatus: expectedOperationStatus,
                expectedNumCharsRead: expectedNumCharsConsumed,
                expectedUtf8Transcoding: DecodeHex(expectedUtf8TranscodingHex));
        }

        [Fact]
        public void ToBytes_AllPossibleScalarValues()
        {
            ToBytes_Test_Core(
                utf16Input: s_allScalarsAsUtf16.Span,
                destinationSize: s_allScalarsAsUtf8.Length,
                replaceInvalidSequences: false,
                isFinalChunk: false,
                expectedOperationStatus: OperationStatus.Done,
                expectedNumCharsRead: s_allScalarsAsUtf16.Length,
                expectedUtf8Transcoding: s_allScalarsAsUtf8.Span);
        }

        private static void ToBytes_Test_Core(ReadOnlySpan<char> utf16Input, int destinationSize, bool replaceInvalidSequences, bool isFinalChunk, OperationStatus expectedOperationStatus, int expectedNumCharsRead, ReadOnlySpan<byte> expectedUtf8Transcoding)
        {
            // Arrange

            using (BoundedMemory<char> boundedSource = BoundedMemory.AllocateFromExistingData(utf16Input))
            using (BoundedMemory<byte> boundedDestination = BoundedMemory.Allocate<byte>(destinationSize))
            {
                boundedSource.MakeReadonly();

                // Act

                OperationStatus actualOperationStatus = Utf8.FromUtf16(boundedSource.Span, boundedDestination.Span, out int actualNumCharsRead, out int actualNumBytesWritten, replaceInvalidSequences, isFinalChunk);

                // Assert

                Assert.Equal(expectedOperationStatus, actualOperationStatus);
                Assert.Equal(expectedNumCharsRead, actualNumCharsRead);
                Assert.Equal(expectedUtf8Transcoding.Length, actualNumBytesWritten);
                Assert.Equal(expectedUtf8Transcoding.ToArray(), boundedDestination.Span.Slice(0, actualNumBytesWritten).ToArray());
            }
        }

        [Theory]
        [InlineData("80", 0, "")] // sequence cannot begin with continuation character
        [InlineData("8182", 0, "")] // sequence cannot begin with continuation character
        [InlineData("838485", 0, "")] // sequence cannot begin with continuation character
        [InlineData(X_UTF8 + "80", 1, X_UTF16)] // sequence cannot begin with continuation character
        [InlineData(X_UTF8 + "8182", 1, X_UTF16)] // sequence cannot begin with continuation character
        [InlineData("C0", 0, "")] // [ C0 ] is always invalid
        [InlineData("C080", 0, "")] // [ C0 ] is always invalid
        [InlineData("C08081", 0, "")] // [ C0 ] is always invalid
        [InlineData(X_UTF8 + "C1", 1, X_UTF16)] // [ C1 ] is always invalid
        [InlineData(X_UTF8 + "C180", 1, X_UTF16)] // [ C1 ] is always invalid
        [InlineData(X_UTF8 + "C27F", 1, X_UTF16)] // [ C2 ] is improperly terminated
        [InlineData("E2827F", 0, "")] // [ E2 82 ] is improperly terminated
        [InlineData("E09F80", 0, "")] // [ E0 9F ... ] is overlong
        [InlineData("E0C080", 0, "")] // [ E0 ] is improperly terminated
        [InlineData("ED7F80", 0, "")] // [ ED ] is improperly terminated
        [InlineData("EDA080", 0, "")] // [ ED A0 ... ] is surrogate
        public void ToChars_WithSmallInvalidBuffers(string utf8HexInput, int expectedNumBytesConsumed, string expectedUtf16Transcoding)
        {
            // These test cases are for the "slow processing" code path at the end of TranscodeToUtf16,
            // so inputs should be less than 4 bytes.

            Assert.InRange(utf8HexInput.Length, 0, 6);

            ToChars_Test_Core(
              utf8Input: DecodeHex(utf8HexInput),
              destinationSize: expectedUtf16Transcoding.Length,
              replaceInvalidSequences: false,
              isFinalChunk: false,
              expectedOperationStatus: OperationStatus.InvalidData,
              expectedNumBytesRead: expectedNumBytesConsumed,
              expectedUtf16Transcoding: expectedUtf16Transcoding);

            // Now try the tests again with a larger buffer.
            // This ensures that running out of destination space wasn't the reason we failed.

            ToChars_Test_Core(
              utf8Input: DecodeHex(utf8HexInput),
              destinationSize: expectedUtf16Transcoding.Length + 16,
              replaceInvalidSequences: false,
              isFinalChunk: false,
              expectedOperationStatus: OperationStatus.InvalidData,
              expectedNumBytesRead: expectedNumBytesConsumed,
              expectedUtf16Transcoding: expectedUtf16Transcoding);
        }

        [Theory]
        [InlineData("C2", 0, "")] // [ C2 ] is an incomplete sequence
        [InlineData("E282", 0, "")] // [ E2 82 ] is an incomplete sequence
        [InlineData(X_UTF8 + "C2", 1, X_UTF16)] // [ C2 ] is an incomplete sequence
        [InlineData(X_UTF8 + "E0", 1, X_UTF16)] // [ E0 ] is an incomplete sequence
        [InlineData(X_UTF8 + "E0BF", 1, X_UTF16)] // [ E0 BF ] is an incomplete sequence
        [InlineData(X_UTF8 + "F0", 1, X_UTF16)] // [ F0 ] is an incomplete sequence
        [InlineData(X_UTF8 + "F0BF", 1, X_UTF16)] // [ F0 BF ] is an incomplete sequence
        [InlineData(X_UTF8 + "F0BFA0", 1, X_UTF16)] // [ F0 BF A0 ] is an incomplete sequence
        [InlineData(E_ACUTE_UTF8 + "C2", 2, E_ACUTE_UTF16)] // [ C2 ] is an incomplete sequence
        [InlineData(E_ACUTE_UTF8 + "E0", 2, E_ACUTE_UTF16)] // [ E0 ] is an incomplete sequence
        [InlineData(E_ACUTE_UTF8 + "F0", 2, E_ACUTE_UTF16)] // [ F0 ] is an incomplete sequence
        [InlineData(E_ACUTE_UTF8 + "E0BF", 2, E_ACUTE_UTF16)] // [ E0 BF ] is an incomplete sequence
        [InlineData(E_ACUTE_UTF8 + "F0BF", 2, E_ACUTE_UTF16)] // [ F0 BF ] is an incomplete sequence
        [InlineData(EURO_SYMBOL_UTF8 + "C2", 3, EURO_SYMBOL_UTF16)] // [ C2 ] is an incomplete sequence
        [InlineData(EURO_SYMBOL_UTF8 + "E0", 3, EURO_SYMBOL_UTF16)] // [ E0 ] is an incomplete sequence
        [InlineData(EURO_SYMBOL_UTF8 + "F0", 3, EURO_SYMBOL_UTF16)] // [ F0 ] is an incomplete sequence
        public void ToChars_WithVariousIncompleteBuffers(string utf8HexInput, int expectedNumBytesConsumed, string expectedUtf16Transcoding)
        {
            // These test cases are for the "slow processing" code path at the end of TranscodeToUtf16,
            // so inputs should be less than 4 bytes.

            ToChars_Test_Core(
              utf8Input: DecodeHex(utf8HexInput),
              destinationSize: expectedUtf16Transcoding.Length,
              replaceInvalidSequences: false,
              isFinalChunk: false,
              expectedOperationStatus: OperationStatus.NeedMoreData,
              expectedNumBytesRead: expectedNumBytesConsumed,
              expectedUtf16Transcoding: expectedUtf16Transcoding);

            // Now try the tests again with a larger buffer.
            // This ensures that running out of destination space wasn't the reason we failed.

            ToChars_Test_Core(
             utf8Input: DecodeHex(utf8HexInput),
             destinationSize: expectedUtf16Transcoding.Length + 16,
             replaceInvalidSequences: false,
             isFinalChunk: false,
             expectedOperationStatus: OperationStatus.NeedMoreData,
             expectedNumBytesRead: expectedNumBytesConsumed,
             expectedUtf16Transcoding: expectedUtf16Transcoding);
        }

        [Theory]
        /* SMALL VALID BUFFERS - tests drain loop at end of method */
        [InlineData("")] // empty string is OK
        [InlineData("X")]
        [InlineData("XY")]
        [InlineData("XYZ")]
        [InlineData(E_ACUTE_UTF16)]
        [InlineData(X_UTF16 + E_ACUTE_UTF16)]
        [InlineData(E_ACUTE_UTF16 + X_UTF16)]
        [InlineData(EURO_SYMBOL_UTF16)]
        /* LARGE VALID BUFFERS - test main loop at beginning of method */
        [InlineData(E_ACUTE_UTF16 + "ABCD" + "0123456789:;<=>?")] // Loop unrolling at end of buffer
        [InlineData(E_ACUTE_UTF16 + "ABCD" + "0123456789:;<=>?" + "01234567" + E_ACUTE_UTF16 + "89:;<=>?")] // Loop unrolling interrupted by non-ASCII
        [InlineData("ABC" + E_ACUTE_UTF16 + "0123")] // 3 ASCII bytes followed by non-ASCII
        [InlineData("AB" + E_ACUTE_UTF16 + "0123")] // 2 ASCII bytes followed by non-ASCII
        [InlineData("A" + E_ACUTE_UTF16 + "0123")] // 1 ASCII byte followed by non-ASCII
        [InlineData(E_ACUTE_UTF16 + E_ACUTE_UTF16 + E_ACUTE_UTF16 + E_ACUTE_UTF16)] // 4x 2-byte sequences, exercises optimization code path in 2-byte sequence processing
        [InlineData(E_ACUTE_UTF16 + E_ACUTE_UTF16 + E_ACUTE_UTF16 + "PQ")] // 3x 2-byte sequences + 2 ASCII bytes, exercises optimization code path in 2-byte sequence processing
        [InlineData(E_ACUTE_UTF16 + "PQ")] // single 2-byte sequence + 2 trailing ASCII bytes, exercises draining logic in 2-byte sequence processing
        [InlineData(E_ACUTE_UTF16 + "P" + E_ACUTE_UTF16 + "0@P")] // single 2-byte sequences + 1 trailing ASCII byte + 2-byte sequence, exercises draining logic in 2-byte sequence processing
        [InlineData(EURO_SYMBOL_UTF16 + "@")] // single 3-byte sequence + 1 trailing ASCII byte, exercises draining logic in 3-byte sequence processing
        [InlineData(EURO_SYMBOL_UTF16 + "@P`")] // single 3-byte sequence + 3 trailing ASCII byte, exercises draining logic and "running out of data" logic in 3-byte sequence processing
        [InlineData(EURO_SYMBOL_UTF16 + EURO_SYMBOL_UTF16 + EURO_SYMBOL_UTF16)] // 3x 3-byte sequences, exercises "stay within 3-byte loop" logic in 3-byte sequence processing
        [InlineData(EURO_SYMBOL_UTF16 + EURO_SYMBOL_UTF16 + EURO_SYMBOL_UTF16 + EURO_SYMBOL_UTF16)] // 4x 3-byte sequences, exercises "consume multiple bytes at a time" logic in 3-byte sequence processing
        [InlineData(EURO_SYMBOL_UTF16 + EURO_SYMBOL_UTF16 + EURO_SYMBOL_UTF16 + E_ACUTE_UTF16)] // 3x 3-byte sequences + single 2-byte sequence, exercises "consume multiple bytes at a time" logic in 3-byte sequence processing
        [InlineData(EURO_SYMBOL_UTF16 + EURO_SYMBOL_UTF16 + E_ACUTE_UTF16 + E_ACUTE_UTF16 + E_ACUTE_UTF16 + E_ACUTE_UTF16)] // 2x 3-byte sequences + 4x 2-byte sequences, exercises "consume multiple bytes at a time" logic in 3-byte sequence processing
        [InlineData(GRINNING_FACE_UTF16 + GRINNING_FACE_UTF16)] // 2x 4-byte sequences, exercises 4-byte sequence processing
        [InlineData(GRINNING_FACE_UTF16 + "@AB")] // single 4-byte sequence + 3 ASCII bytes, exercises 4-byte sequence processing and draining logic
        [InlineData(WOMAN_CARTWHEELING_MEDSKIN_UTF16)] // exercises switching between multiple sequence lengths
        public void ToChars_ValidBuffers(string utf16Input)
        {
            // We're going to run the tests with destination buffer lengths ranging from 0 all the way
            // to buffers large enough to hold the full output. This allows us to test logic that
            // detects whether we're about to overrun our destination buffer and instead returns DestinationTooSmall.

            Rune[] enumeratedScalars = utf16Input.EnumerateRunes().ToArray();

            // Convert entire input to UTF-8 using our unit test reference logic.

            byte[] utf8Input = enumeratedScalars.SelectMany(ToUtf8).ToArray();

            // 0-length buffer test
            ToChars_Test_Core(
                utf8Input: utf8Input,
                destinationSize: 0,
                replaceInvalidSequences: false,
                isFinalChunk: false,
                expectedOperationStatus: (utf8Input.Length == 0) ? OperationStatus.Done : OperationStatus.DestinationTooSmall,
                expectedNumBytesRead: 0,
                expectedUtf16Transcoding: ReadOnlySpan<char>.Empty);

            int expectedNumBytesConsumed = 0;
            char[] concatenatedUtf16 = Array.Empty<char>();

            for (int i = 0; i < enumeratedScalars.Length; i++)
            {
                Rune thisScalar = enumeratedScalars[i];

                // if this is an astral scalar value, quickly test a buffer that's not large enough to contain the entire UTF-16 encoding

                if (!thisScalar.IsBmp)
                {
                    ToChars_Test_Core(
                        utf8Input: utf8Input,
                        destinationSize: concatenatedUtf16.Length + 1,
                        replaceInvalidSequences: false,
                        isFinalChunk: false,
                        expectedOperationStatus: OperationStatus.DestinationTooSmall,
                        expectedNumBytesRead: expectedNumBytesConsumed,
                        expectedUtf16Transcoding: concatenatedUtf16);
                }

                // now provide a destination buffer large enough to hold the next full scalar encoding

                expectedNumBytesConsumed += thisScalar.Utf8SequenceLength;
                concatenatedUtf16 = concatenatedUtf16.Concat(ToUtf16(thisScalar)).ToArray();

                ToChars_Test_Core(
                    utf8Input: utf8Input,
                    destinationSize: concatenatedUtf16.Length,
                    replaceInvalidSequences: false,
                    isFinalChunk: false,
                    expectedOperationStatus: (i == enumeratedScalars.Length - 1) ? OperationStatus.Done : OperationStatus.DestinationTooSmall,
                    expectedNumBytesRead: expectedNumBytesConsumed,
                    expectedUtf16Transcoding: concatenatedUtf16);
            }

            // now throw lots of ASCII data at the beginning so that we exercise the vectorized code paths

            utf16Input = new string('x', 64) + utf16Input;
            utf8Input = utf16Input.EnumerateRunes().SelectMany(ToUtf8).ToArray();

            ToChars_Test_Core(
                utf8Input: utf8Input,
                destinationSize: utf16Input.Length,
                replaceInvalidSequences: false,
                isFinalChunk: true,
                expectedOperationStatus: OperationStatus.Done,
                expectedNumBytesRead: utf8Input.Length,
                expectedUtf16Transcoding: utf16Input);

            // now throw some non-ASCII data at the beginning so that we *don't* exercise the vectorized code paths

            utf16Input = WOMAN_CARTWHEELING_MEDSKIN_UTF16 + utf16Input[64..];
            utf8Input = utf16Input.EnumerateRunes().SelectMany(ToUtf8).ToArray();

            ToChars_Test_Core(
                utf8Input: utf8Input,
                destinationSize: utf16Input.Length,
                replaceInvalidSequences: false,
                isFinalChunk: true,
                expectedOperationStatus: OperationStatus.Done,
                expectedNumBytesRead: utf8Input.Length,
                expectedUtf16Transcoding: utf16Input);
        }

        [Theory]
        [InlineData("3031" + "80" + "202122232425", 2, "01")] // Continuation character at start of sequence should match no bitmask
        [InlineData("3031" + "C080" + "2021222324", 2, "01")] // Overlong 2-byte sequence at start of DWORD
        [InlineData("3031" + "C180" + "2021222324", 2, "01")] // Overlong 2-byte sequence at start of DWORD
        [InlineData("C280" + "C180", 2, "\u0080")] // Overlong 2-byte sequence at end of DWORD
        [InlineData("C27F" + "C280", 0, "")] // Improperly terminated 2-byte sequence at start of DWORD
        [InlineData("C2C0" + "C280", 0, "")] // Improperly terminated 2-byte sequence at start of DWORD
        [InlineData("C280" + "C27F", 2, "\u0080")] // Improperly terminated 2-byte sequence at end of DWORD
        [InlineData("C280" + "C2C0", 2, "\u0080")] // Improperly terminated 2-byte sequence at end of DWORD
        [InlineData("C280" + "C280" + "80203040", 4, "\u0080\u0080")] // Continuation character at start of sequence, within "stay in 2-byte processing" optimization
        [InlineData("C280" + "C280" + "C180" + "C280", 4, "\u0080\u0080")] // Overlong 2-byte sequence at start of DWORD, within "stay in 2-byte processing" optimization
        [InlineData("C280" + "C280" + "C280" + "C180", 6, "\u0080\u0080\u0080")] // Overlong 2-byte sequence at end of DWORD, within "stay in 2-byte processing" optimization
        [InlineData("3031" + "E09F80" + EURO_SYMBOL_UTF8 + EURO_SYMBOL_UTF8, 2, "01")] // Overlong 3-byte sequence at start of DWORD
        [InlineData("3031" + "E07F80" + EURO_SYMBOL_UTF8 + EURO_SYMBOL_UTF8, 2, "01")] // Improperly terminated 3-byte sequence at start of DWORD
        [InlineData("3031" + "E0C080" + EURO_SYMBOL_UTF8 + EURO_SYMBOL_UTF8, 2, "01")] // Improperly terminated 3-byte sequence at start of DWORD
        [InlineData("3031" + "E17F80" + EURO_SYMBOL_UTF8 + EURO_SYMBOL_UTF8, 2, "01")] // Improperly terminated 3-byte sequence at start of DWORD
        [InlineData("3031" + "E1C080" + EURO_SYMBOL_UTF8 + EURO_SYMBOL_UTF8, 2, "01")] // Improperly terminated 3-byte sequence at start of DWORD
        [InlineData("3031" + "EDA080" + EURO_SYMBOL_UTF8 + EURO_SYMBOL_UTF8, 2, "01")] // Surrogate 3-byte sequence at start of DWORD
        [InlineData("3031" + "E69C88" + "E59B" + "E69C88", 5, "01\u6708")] // Incomplete 3-byte sequence surrounded by valid 3-byte sequences
        [InlineData("3031" + "F5808080", 2, "01")] // [ F5 ] is always invalid
        [InlineData("3031" + "F6808080", 2, "01")] // [ F6 ] is always invalid
        [InlineData("3031" + "F7808080", 2, "01")] // [ F7 ] is always invalid
        [InlineData("3031" + "F8808080", 2, "01")] // [ F8 ] is always invalid
        [InlineData("3031" + "F9808080", 2, "01")] // [ F9 ] is always invalid
        [InlineData("3031" + "FA808080", 2, "01")] // [ FA ] is always invalid
        [InlineData("3031" + "FB808080", 2, "01")] // [ FB ] is always invalid
        [InlineData("3031" + "FC808080", 2, "01")] // [ FC ] is always invalid
        [InlineData("3031" + "FD808080", 2, "01")] // [ FD ] is always invalid
        [InlineData("3031" + "FE808080", 2, "01")] // [ FE ] is always invalid
        [InlineData("3031" + "FF808080", 2, "01")] // [ FF ] is always invalid
        public void ToChars_WithLargeInvalidBuffers(string utf8HexInput, int expectedNumBytesConsumed, string expectedUtf16Transcoding)
        {
            // These test cases are for the "fast processing" code which is the main loop of TranscodeToUtf16,
            // so inputs should be less >= 4 bytes.

            Assert.True(utf8HexInput.Length >= 8);

            ToChars_Test_Core(
                utf8Input: DecodeHex(utf8HexInput),
                destinationSize: expectedUtf16Transcoding.Length,
                replaceInvalidSequences: false,
                isFinalChunk: false,
                expectedOperationStatus: OperationStatus.InvalidData,
                expectedNumBytesRead: expectedNumBytesConsumed,
                expectedUtf16Transcoding: expectedUtf16Transcoding);

            // Now try the tests again with a larger buffer.
            // This ensures that running out of destination space wasn't the reason we failed.

            ToChars_Test_Core(
                utf8Input: DecodeHex(utf8HexInput),
                destinationSize: expectedUtf16Transcoding.Length + 16,
                replaceInvalidSequences: false,
                isFinalChunk: false,
                expectedOperationStatus: OperationStatus.InvalidData,
                expectedNumBytesRead: expectedNumBytesConsumed,
                expectedUtf16Transcoding: expectedUtf16Transcoding);
        }

        [Theory]
        [InlineData(X_UTF8 + "80" + X_UTF8, X_UTF16 + REPLACEMENT_CHAR_UTF16 + X_UTF16)] // stray continuation byte [ 80 ]
        [InlineData(X_UTF8 + "FF" + X_UTF8, X_UTF16 + REPLACEMENT_CHAR_UTF16 + X_UTF16)] // invalid UTF-8 byte [ FF ]
        [InlineData(X_UTF8 + "C2" + X_UTF8, X_UTF16 + REPLACEMENT_CHAR_UTF16 + X_UTF16)] // 2-byte sequence starter [ C2 ] not followed by continuation byte
        [InlineData(X_UTF8 + "C1C180" + X_UTF8, X_UTF16 + REPLACEMENT_CHAR_UTF16 + REPLACEMENT_CHAR_UTF16 + REPLACEMENT_CHAR_UTF16 + X_UTF16)] // [ C1 80 ] is overlong but consists of two maximal invalid subsequences, each of length 1 byte
        [InlineData(X_UTF8 + E_ACUTE_UTF8 + "E08080", X_UTF16 + E_ACUTE_UTF16 + REPLACEMENT_CHAR_UTF16 + REPLACEMENT_CHAR_UTF16 + REPLACEMENT_CHAR_UTF16)] // [ E0 80 ] is overlong 2-byte sequence (1 byte maximal invalid subsequence), and following [ 80 ] is stray continuation byte
        [InlineData(GRINNING_FACE_UTF8 + "F08F8080" + GRINNING_FACE_UTF8, GRINNING_FACE_UTF16 + REPLACEMENT_CHAR_UTF16 + REPLACEMENT_CHAR_UTF16 + REPLACEMENT_CHAR_UTF16 + REPLACEMENT_CHAR_UTF16 + GRINNING_FACE_UTF16)] // [ F0 8F ] is overlong 4-byte sequence (1 byte maximal invalid subsequence), and following [ 80 ] instances are stray continuation bytes
        [InlineData(GRINNING_FACE_UTF8 + "F4908080" + GRINNING_FACE_UTF8, GRINNING_FACE_UTF16 + REPLACEMENT_CHAR_UTF16 + REPLACEMENT_CHAR_UTF16 + REPLACEMENT_CHAR_UTF16 + REPLACEMENT_CHAR_UTF16 + GRINNING_FACE_UTF16)] // [ F4 90 ] is out-of-range 4-byte sequence (1 byte maximal invalid subsequence), and following [ 80 ] instances are stray continuation bytes
        [InlineData(E_ACUTE_UTF8 + "EDA0" + X_UTF8, E_ACUTE_UTF16 + REPLACEMENT_CHAR_UTF16 + REPLACEMENT_CHAR_UTF16 + X_UTF16)] // [ ED A0 ] is encoding of UTF-16 surrogate code point, so consists of two maximal invalid subsequences, each of length 1 byte
        [InlineData(E_ACUTE_UTF8 + "ED80" + X_UTF8, E_ACUTE_UTF16 + REPLACEMENT_CHAR_UTF16 + X_UTF16)] // [ ED 80 ] is incomplete 3-byte sequence, so is 2-byte maximal invalid subsequence
        [InlineData(E_ACUTE_UTF8 + "F380" + X_UTF8, E_ACUTE_UTF16 + REPLACEMENT_CHAR_UTF16 + X_UTF16)] // [ F3 80 ] is incomplete 4-byte sequence, so is 2-byte maximal invalid subsequence
        [InlineData(E_ACUTE_UTF8 + "F38080" + X_UTF8, E_ACUTE_UTF16 + REPLACEMENT_CHAR_UTF16 + X_UTF16)] // [ F3 80 80 ] is incomplete 4-byte sequence, so is 3-byte maximal invalid subsequence
        public void ToChars_WithReplacement(string utf8HexInput, string expectedUtf16Transcoding)
        {
            // First run the test with isFinalBlock = false,
            // both with and without some bytes of incomplete trailing data.

            ToChars_Test_Core(
                utf8Input: DecodeHex(utf8HexInput),
                destinationSize: expectedUtf16Transcoding.Length,
                replaceInvalidSequences: true,
                isFinalChunk: false,
                expectedOperationStatus: OperationStatus.Done,
                expectedNumBytesRead: utf8HexInput.Length / 2,
                expectedUtf16Transcoding: expectedUtf16Transcoding);

            ToChars_Test_Core(
                utf8Input: DecodeHex(utf8HexInput + "E0BF" /* trailing data */),
                destinationSize: expectedUtf16Transcoding.Length,
                replaceInvalidSequences: true,
                isFinalChunk: false,
                expectedOperationStatus: OperationStatus.NeedMoreData,
                expectedNumBytesRead: utf8HexInput.Length / 2,
                expectedUtf16Transcoding: expectedUtf16Transcoding);

            // Then run the test with isFinalBlock = true, with incomplete trailing data.

            ToChars_Test_Core(
                utf8Input: DecodeHex(utf8HexInput + "E0BF" /* trailing data */),
                destinationSize: expectedUtf16Transcoding.Length,
                replaceInvalidSequences: true,
                isFinalChunk: true,
                expectedOperationStatus: OperationStatus.DestinationTooSmall,
                expectedNumBytesRead: utf8HexInput.Length / 2,
                expectedUtf16Transcoding: expectedUtf16Transcoding);

            ToChars_Test_Core(
                 utf8Input: DecodeHex(utf8HexInput + "E0BF" /* trailing data */),
                 destinationSize: expectedUtf16Transcoding.Length + 1, // allow room for U+FFFD
                 replaceInvalidSequences: true,
                 isFinalChunk: true,
                 expectedOperationStatus: OperationStatus.Done,
                 expectedNumBytesRead: utf8HexInput.Length / 2 + 2,
                 expectedUtf16Transcoding: expectedUtf16Transcoding + REPLACEMENT_CHAR_UTF16);
        }

        [Fact]
        public void ToChars_AllPossibleScalarValues()
        {
            ToChars_Test_Core(
                utf8Input: s_allScalarsAsUtf8.Span,
                destinationSize: s_allScalarsAsUtf16.Length,
                replaceInvalidSequences: false,
                isFinalChunk: false,
                expectedOperationStatus: OperationStatus.Done,
                expectedNumBytesRead: s_allScalarsAsUtf8.Length,
                expectedUtf16Transcoding: s_allScalarsAsUtf16.Span);
        }

        private static void ToChars_Test_Core(ReadOnlySpan<byte> utf8Input, int destinationSize, bool replaceInvalidSequences, bool isFinalChunk, OperationStatus expectedOperationStatus, int expectedNumBytesRead, ReadOnlySpan<char> expectedUtf16Transcoding)
        {
            // Arrange

            using (BoundedMemory<byte> boundedSource = BoundedMemory.AllocateFromExistingData(utf8Input))
            using (BoundedMemory<char> boundedDestination = BoundedMemory.Allocate<char>(destinationSize))
            {
                boundedSource.MakeReadonly();

                // Act

                OperationStatus actualOperationStatus = Utf8.ToUtf16(boundedSource.Span, boundedDestination.Span, out int actualNumBytesRead, out int actualNumCharsWritten, replaceInvalidSequences, isFinalChunk);

                // Assert

                Assert.Equal(expectedOperationStatus, actualOperationStatus);
                Assert.Equal(expectedNumBytesRead, actualNumBytesRead);
                Assert.Equal(expectedUtf16Transcoding.Length, actualNumCharsWritten);
                Assert.Equal(expectedUtf16Transcoding.ToString(), boundedDestination.Span.Slice(0, actualNumCharsWritten).ToString());
            }
        }
    }
}
