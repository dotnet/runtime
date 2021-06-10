// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Text.Encodings.Web.Tests
{
    public class TextEncoderTests
    {
        [Fact]
        public void EncodeIntoBuffer_SurrogatePairs()
        {
            // Arange
            TextEncoder encoder = new ConfigurableScalarTextEncoder(_ => false);

            const string X = "\U00000058"; // LATIN CAPITAL LETTER X (ascii)
            const string Pair = "\U0001033A"; // GOTHIC LETTER KUSMA (surrogate pair)

            const string eX = "[0058]";
            const string ePair = "[1033A]";

            // Act & assert
            Assert.Equal("", encoder.Encode(""));

            Assert.Equal(eX, encoder.Encode(X)); // no iteration, block
            Assert.Equal(eX + eX, encoder.Encode(X + X)); // two iterations, no block
            Assert.Equal(eX + eX + eX, encoder.Encode(X + X + X)); // two iterations, block

            Assert.Equal(ePair, encoder.Encode(Pair)); // one iteration, no block
            Assert.Equal(ePair + ePair, encoder.Encode(Pair + Pair)); // two iterations, no block

            Assert.Equal(eX + ePair, encoder.Encode(X + Pair)); // two iterations, no block
            Assert.Equal(ePair + eX, encoder.Encode(Pair + X)); // one iteration, block

            Assert.Equal(eX + ePair + eX, encoder.Encode(X + Pair + X)); // two iterations, block, even length
            Assert.Equal(ePair + eX + ePair, encoder.Encode(Pair + X + Pair)); // three iterations, no block, odd length
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(2, 2)]
        [InlineData(3, 3)]
        [InlineData(4, 3)]
        [InlineData(5, 3)]
        [InlineData(6, 6)]
        [InlineData(7, 6)]
        [InlineData(8, 6)]
        [InlineData(9, 6)]
        [InlineData(10, 10)]
        [InlineData(11, 11)]
        [InlineData(12, 11)]
        public void EncodeUtf8_WellFormedInput_DoesNotRequireEncoding_CopiedToDestinationCorrectly(int destinationSize, int expectedBytesCopied)
        {
            // This test considers input which is well-formed and doesn't need to be encoded.
            // If the destination buffer is large enough, the data should be copied in its entirety.
            // If the destination buffer is too small, only complete UTF-8 subsequences should be copied.
            // We should never copy a partial subsequence, as it would cause a future call to EncodeUtf8
            // to misinterpret the data as ill-formed.

            // Arrange

            byte[] fullUtf8Input = new byte[] {
                0xC2, 0x82,
                0x40,
                0xE2, 0x90, 0x91,
                0xF3, 0xA0, 0xA1, 0xA2,
                0x50 }; // UTF-8 subsequences of varying length

            var encoder = new ConfigurableScalarTextEncoder(_ => true /* allow everything */);

            // Act & assert

            OperationStatus expectedOpStatus = (expectedBytesCopied == fullUtf8Input.Length) ? OperationStatus.Done : OperationStatus.DestinationTooSmall;

            byte[] destination = new byte[destinationSize];
            Assert.Equal(expectedOpStatus, encoder.EncodeUtf8(fullUtf8Input, destination, out int bytesConsumed, out int bytesWritten, isFinalBlock: true));
            Assert.Equal(expectedBytesCopied, bytesConsumed);
            Assert.Equal(expectedBytesCopied, bytesWritten); // bytes written should match bytes consumed if no encoding needs to take place
            Assert.Equal(fullUtf8Input.AsSpan(0, bytesConsumed).ToArray(), destination.AsSpan(0, bytesWritten).ToArray()); // ensure byte-for-byte copy

            destination = new byte[destinationSize];
            Assert.Equal(expectedOpStatus, encoder.EncodeUtf8(fullUtf8Input, destination, out bytesConsumed, out bytesWritten, isFinalBlock: false));
            Assert.Equal(expectedBytesCopied, bytesConsumed);
            Assert.Equal(expectedBytesCopied, bytesWritten); // bytes written should match bytes consumed if no encoding needs to take place
            Assert.Equal(fullUtf8Input.AsSpan(0, bytesConsumed).ToArray(), destination.AsSpan(0, bytesWritten).ToArray()); // ensure byte-for-byte copy
        }

        [Fact]
        public void EncodeUtf8_MixedInputWhichRequiresEncodingOrReplacement()
        {
            // Arrange

            var fullInput = new[]
            {
                new { utf8Bytes = new byte[] { 0x40 }, output = "@" },
                new { utf8Bytes = new byte[] { 0xC3, 0x85 }, output = "[00C5]" }, // U+00C5 LATIN CAPITAL LETTER A WITH RING ABOVE (encoded since odd scalar value)
                new { utf8Bytes = new byte[] { 0xC3, 0x86 }, output = "\u00C6" }, // U+00C6 LATIN CAPITAL LETTER AE (on allow list)
                new { utf8Bytes = new byte[] { 0xFF }, output = "[FFFD]" }, // (invalid UTF-8, replaced with encoded form of U+FFFD)
                new { utf8Bytes = new byte[] { 0xEF, 0xBF, 0xBD }, output = "[FFFD]" }, // U+FFFD REPLACEMENT CHARACTER (encoded since not on allow list)
                new { utf8Bytes = new byte[] { 0xF0, 0x90, 0x82, 0x82 }, output = "\U00010082" }, // U+10082 LINEAR B IDEOGRAM B104 DEER (not encoded since on allow list)
                new { utf8Bytes = new byte[] { 0xF0, 0x90, 0x82, 0x83 }, output = "[10083]" }, // U+10083 LINEAR B IDEOGRAM B105 EQUID (encoded since not on allow list)
            };

            var encoder = new ConfigurableScalarTextEncoder(scalarValue => (scalarValue % 2) == 0 /* allow only even-valued scalars to be represented unescaped */);

            // Act & assert

            List<byte> aggregateInputBytesSoFar = new List<byte>();
            List<byte> expectedOutputBytesSoFar = new List<byte>();

            foreach (var entry in fullInput)
            {
                int aggregateInputByteCountAtStartOfLoop = aggregateInputBytesSoFar.Count;

                byte[] destination;
                int bytesConsumed, bytesWritten;

                for (int i = 0; i < entry.utf8Bytes.Length - 1; i++)
                {
                    aggregateInputBytesSoFar.Add(entry.utf8Bytes[i]);

                    // If not final block, partial encoding should say "needs more data".
                    // We'll try with various destination lengths just to make sure it doesn't affect result.

                    foreach (int destinationLength in new[] { expectedOutputBytesSoFar.Count, expectedOutputBytesSoFar.Count + 1024 })
                    {
                        destination = new byte[destinationLength];

                        Assert.Equal(OperationStatus.NeedMoreData, encoder.EncodeUtf8(aggregateInputBytesSoFar.ToArray(), destination, out bytesConsumed, out bytesWritten, isFinalBlock: false));
                        Assert.Equal(aggregateInputByteCountAtStartOfLoop, bytesConsumed);
                        Assert.Equal(expectedOutputBytesSoFar.Count, bytesWritten);
                        Assert.Equal(expectedOutputBytesSoFar.ToArray(), new Span<byte>(destination, 0, expectedOutputBytesSoFar.Count).ToArray());
                    }

                    // Now try it with "isFinalBlock = true" to force the U+FFFD conversion

                    destination = new byte[expectedOutputBytesSoFar.Count]; // first with not enough output space to write "[FFFD]"

                    Assert.Equal(OperationStatus.DestinationTooSmall, encoder.EncodeUtf8(aggregateInputBytesSoFar.ToArray(), destination, out bytesConsumed, out bytesWritten, isFinalBlock: true));
                    Assert.Equal(aggregateInputByteCountAtStartOfLoop, bytesConsumed);
                    Assert.Equal(expectedOutputBytesSoFar.Count, bytesWritten);
                    Assert.Equal(expectedOutputBytesSoFar.ToArray(), new Span<byte>(destination, 0, expectedOutputBytesSoFar.Count).ToArray());

                    destination = new byte[expectedOutputBytesSoFar.Count + 1024]; // then with enough output space to write "[FFFD]"

                    Assert.Equal(OperationStatus.Done, encoder.EncodeUtf8(aggregateInputBytesSoFar.ToArray(), destination, out bytesConsumed, out bytesWritten, isFinalBlock: true));
                    Assert.Equal(aggregateInputBytesSoFar.Count, bytesConsumed);
                    Assert.Equal(expectedOutputBytesSoFar.Count + "[FFFD]".Length, bytesWritten);
                    Assert.Equal(expectedOutputBytesSoFar.Concat(Encoding.UTF8.GetBytes("[FFFD]")).ToArray(), new Span<byte>(destination, 0, expectedOutputBytesSoFar.Count + "[FFFD]".Length).ToArray());
                }

                // Consume the remainder of this entry and make sure it escaped properly (if needed).

                aggregateInputBytesSoFar.Add(entry.utf8Bytes.Last());

                // First with not enough space in the destination buffer.

                destination = new byte[expectedOutputBytesSoFar.Count + Encoding.UTF8.GetByteCount(entry.output) - 1];

                Assert.Equal(OperationStatus.DestinationTooSmall, encoder.EncodeUtf8(aggregateInputBytesSoFar.ToArray(), destination, out bytesConsumed, out bytesWritten, isFinalBlock: true));
                Assert.Equal(aggregateInputByteCountAtStartOfLoop, bytesConsumed);
                Assert.Equal(expectedOutputBytesSoFar.Count, bytesWritten);
                Assert.Equal(expectedOutputBytesSoFar.ToArray(), new Span<byte>(destination, 0, expectedOutputBytesSoFar.Count).ToArray());

                // Then with exactly enough space in the destination buffer,
                // and again with more than enough space in the destination buffer.

                expectedOutputBytesSoFar.AddRange(Encoding.UTF8.GetBytes(entry.output));

                foreach (int destinationLength in new[] { expectedOutputBytesSoFar.Count, expectedOutputBytesSoFar.Count + 1024 })
                {
                    destination = new byte[destinationLength];

                    Assert.Equal(OperationStatus.Done, encoder.EncodeUtf8(aggregateInputBytesSoFar.ToArray(), destination, out bytesConsumed, out bytesWritten, isFinalBlock: false));
                    Assert.Equal(aggregateInputBytesSoFar.Count, bytesConsumed);
                    Assert.Equal(expectedOutputBytesSoFar.Count, bytesWritten);
                    Assert.Equal(expectedOutputBytesSoFar.ToArray(), new Span<byte>(destination, 0, expectedOutputBytesSoFar.Count).ToArray());
                }
            }
        }

        [Fact]
        public void EncodeUtf8_EmptyInput_AlwaysSucceeds()
        {
            // Arrange

            var encoder = new ConfigurableScalarTextEncoder(_ => false /* disallow everything */);

            // Act & assert

            Assert.Equal(OperationStatus.Done, encoder.EncodeUtf8(ReadOnlySpan<byte>.Empty, Span<byte>.Empty, out int bytesConsumed, out int bytesWritten, isFinalBlock: true));
            Assert.Equal(0, bytesConsumed);
            Assert.Equal(0, bytesWritten);

            Assert.Equal(OperationStatus.Done, encoder.EncodeUtf8(ReadOnlySpan<byte>.Empty, Span<byte>.Empty, out bytesConsumed, out bytesWritten, isFinalBlock: false));
            Assert.Equal(0, bytesConsumed);
            Assert.Equal(0, bytesWritten);
        }

        [Fact]
        public void FindFirstCharToEncodeUtf8_EmptyInput_ReturnsNegOne()
        {
            // Arrange

            var encoder = new ConfigurableScalarTextEncoder(_ => false /* disallow everything */);

            // Act

            int idxOfFirstByteToEncode = encoder.FindFirstCharacterToEncodeUtf8(ReadOnlySpan<byte>.Empty);

            // Assert

            Assert.Equal(-1, idxOfFirstByteToEncode);
        }

        [Fact]
        public void FindFirstCharToEncodeUtf8_WellFormedData_AllCharsAllowed()
        {
            // Arrange

            byte[] inputBytes = Encoding.UTF8.GetBytes("\U00000040\U00000400\U00004000\U00040000"); // code units of different lengths
            var encoder = new ConfigurableScalarTextEncoder(_ => true /* allow everything */);

            // Act

            int idxOfFirstByteToEncode = encoder.FindFirstCharacterToEncodeUtf8(inputBytes);

            // Assert

            Assert.Equal(-1, idxOfFirstByteToEncode);
        }

        [Fact]
        public void FindFirstCharToEncodeUtf8_WellFormedData_SomeCharsDisallowed()
        {
            // Arrange

            byte[] inputBytes = Encoding.UTF8.GetBytes("\U00000040\U00000400\U00004000\U00040000"); // code units of different lengths
            var encoder = new ConfigurableScalarTextEncoder(codePoint => codePoint != 0x4000 /* disallow U+4000, allow all else */);

            // Act

            int idxOfFirstByteToEncode = encoder.FindFirstCharacterToEncodeUtf8(inputBytes);

            // Assert

            Assert.Equal(3, idxOfFirstByteToEncode);
        }

        [Theory]
        [InlineData(new byte[] { 0x00, 0xC0, 0x80, 0x80 }, 1)]
        [InlineData(new byte[] { 0x00, 0xC2, 0x80, 0x80 }, 3)]
        [InlineData(new byte[] { 0xF1, 0x80, 0x80 }, 0)]
        [InlineData(new byte[] { 0xF1, 0x80, 0x80, 0x80, 0xFF }, 4)]
        [InlineData(new byte[] { 0xFF, 0x80, 0x80, 0x80, 0xFF }, 0)]
        public void FindFirstCharToEncodeUtf8_IllFormedData_ReturnsIndexOfIllFormedSubsequence(byte[] utf8Data, int expectedIndex)
        {
            // Arrange

            var encoder = new ConfigurableScalarTextEncoder(_ => true /* allow everything */);

            // Act

            int actualIndex = encoder.FindFirstCharacterToEncodeUtf8(utf8Data);

            // Assert

            Assert.Equal(expectedIndex, actualIndex);
        }

        [Theory]
        [InlineData("", 0, "", 0, OperationStatus.Done)]
        [InlineData("", 20, "", 0, OperationStatus.Done)]
        [InlineData("ABC", 0, "", 0, OperationStatus.DestinationTooSmall)]
        [InlineData("ABC", 2, "AB", 2, OperationStatus.DestinationTooSmall)]
        [InlineData("ABC", 3, "ABC", 3, OperationStatus.Done)]
        [InlineData("ABC", 30, "ABC", 3, OperationStatus.Done)]
        [InlineData("ABC+DEF", 3, "ABC", 3, OperationStatus.DestinationTooSmall)]
        [InlineData("ABC+DEF", 8, "ABC", 3, OperationStatus.DestinationTooSmall)]
        [InlineData("ABC+DEF", 9, "ABC[002B]", 4, OperationStatus.DestinationTooSmall)]
        [InlineData("ABC+DEF", 12, "ABC[002B]DEF", 7, OperationStatus.Done)]
        public void EncodeUtf16_OperationStatus_AlphaNumericOnly(string input, int destBufferSize, string expectedOutput, int expectedCharsConsumed, OperationStatus expectedResult)
        {
            // Arrange

            var encoder = new ConfigurableScalarTextEncoder(scalar => UnicodeUtility.IsInRangeInclusive((uint)scalar | 0x20, 'a', 'z')); // allow only [A-Za-z] unescaped
            using BoundedMemory<char> boundedInput = BoundedMemory.AllocateFromExistingData<char>(input.AsSpan());
            using BoundedMemory<char> boundedOutput = BoundedMemory.Allocate<char>(destBufferSize);

            // Act

            OperationStatus actualResult = encoder.Encode(boundedInput.Span, boundedOutput.Span, out int actualCharsConsumed, out int actualCharsWritten);

            // Assert

            Assert.Equal(expectedResult, actualResult);
            Assert.Equal(expectedCharsConsumed, actualCharsConsumed);
            Assert.Equal(expectedOutput, boundedOutput.Span.Slice(0, actualCharsWritten).ToString());
        }

        [Theory]
        [InlineData("ABC\U0001F600", 4, "ABC", 3, OperationStatus.DestinationTooSmall)] // don't allow breaking across a surrogate
        [InlineData("ABC\U0001F600", 5, "ABC\U0001F600", 5, OperationStatus.Done)]
        public void EncodeUtf16_OperationStatus_AllowEverything(string input, int destBufferSize, string expectedOutput, int expectedCharsConsumed, OperationStatus expectedResult)
        {
            // Arrange

            var encoder = new ConfigurableScalarTextEncoder(_ => true); // allow all well-formed scalars
            using BoundedMemory<char> boundedInput = BoundedMemory.AllocateFromExistingData<char>(input.AsSpan());
            using BoundedMemory<char> boundedOutput = BoundedMemory.Allocate<char>(destBufferSize);

            // Act

            OperationStatus actualResult = encoder.Encode(boundedInput.Span, boundedOutput.Span, out int actualCharsConsumed, out int actualCharsWritten);

            // Assert

            Assert.Equal(expectedResult, actualResult);
            Assert.Equal(expectedCharsConsumed, actualCharsConsumed);
            Assert.Equal(expectedOutput, boundedOutput.Span.Slice(0, actualCharsWritten).ToString());
        }

        [Theory]
        [InlineData(new[] { 'A', 'B', '\ud83d' }, 2, true, "AB", 2, OperationStatus.DestinationTooSmall)]
        [InlineData(new[] { 'A', 'B', '\ud83d' }, 2, false, "AB", 2, OperationStatus.NeedMoreData)]
        [InlineData(new[] { 'A', 'B', '\ud83d' }, 3, true, "AB", 2, OperationStatus.DestinationTooSmall)]
        [InlineData(new[] { 'A', 'B', '\ud83d' }, 3, false, "AB", 2, OperationStatus.NeedMoreData)]
        [InlineData(new[] { 'A', 'B', '\ud83d' }, 10, true, "AB[FFFD]", 3, OperationStatus.Done)]
        [InlineData(new[] { 'A', 'B', '\ud83d' }, 10, false, "AB", 2, OperationStatus.NeedMoreData)]
        [InlineData(new[] { 'A', 'B', '\ud83d', '\ude00' }, 2, true, "AB", 2, OperationStatus.DestinationTooSmall)]
        [InlineData(new[] { 'A', 'B', '\ud83d', '\ude00' }, 2, false, "AB", 2, OperationStatus.DestinationTooSmall)]
        [InlineData(new[] { 'A', 'B', '\ud83d', '\ude00' }, 4, true, "AB\U0001F600", 4, OperationStatus.Done)]
        [InlineData(new[] { 'A', 'B', '\ud83d', '\ude00' }, 4, false, "AB\U0001F600", 4, OperationStatus.Done)]
        public void EncodeUtf16_OperationStatus_SurrogateHandlingEdgeCases(char[] input, int destBufferSize, bool isFinalBlock, string expectedOutput, int expectedCharsConsumed, OperationStatus expectedResult)
        {
            // Arrange

            var encoder = new ConfigurableScalarTextEncoder(_ => true); // allow all well-formed scalars
            using BoundedMemory<char> boundedInput = BoundedMemory.AllocateFromExistingData(input);
            using BoundedMemory<char> boundedOutput = BoundedMemory.Allocate<char>(destBufferSize);

            // Act

            OperationStatus actualResult = encoder.Encode(boundedInput.Span, boundedOutput.Span, out int actualCharsConsumed, out int actualCharsWritten, isFinalBlock);

            // Assert

            Assert.Equal(expectedResult, actualResult);
            Assert.Equal(expectedCharsConsumed, actualCharsConsumed);
            Assert.Equal(expectedOutput, boundedOutput.Span.Slice(0, actualCharsWritten).ToString());
        }
    }
}
