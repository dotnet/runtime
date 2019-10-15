// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Linq;
using Xunit;

namespace System.Text.Unicode.Tests
{
    public partial class Utf8Tests
    {
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
    }
}
