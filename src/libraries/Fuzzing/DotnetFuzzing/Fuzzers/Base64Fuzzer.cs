// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;

namespace DotnetFuzzing.Fuzzers
{
    internal sealed class Base64Fuzzer : IFuzzer
    {
        private const int Base64LineBreakPosition = 76; // Needs to be in sync with Convert.Base64LineBreakPosition

        public string[] TargetAssemblies => [];

        public string[] TargetCoreLibPrefixes => ["System.Buffers.Text.Base64", "System.Convert"];

        public void FuzzTarget(ReadOnlySpan<byte> bytes)
        {
            using PooledBoundedMemory<byte> inputPoisonBefore = PooledBoundedMemory<byte>.Rent(bytes, PoisonPagePlacement.Before);
            using PooledBoundedMemory<byte> inputPoisonAfter = PooledBoundedMemory<byte>.Rent(bytes, PoisonPagePlacement.After);

            TestCases(inputPoisonBefore.Span, PoisonPagePlacement.Before);
            TestCases(inputPoisonAfter.Span, PoisonPagePlacement.After);
        }

        private void TestCases(Span<byte> input, PoisonPagePlacement poison)
        {
            TestBase64(input, poison);
            TestBase64Chars(input, poison);
            TestToStringToCharArray(input, Base64FormattingOptions.None);
            TestToStringToCharArray(input, Base64FormattingOptions.InsertLineBreaks);
        }

        private void TestBase64(Span<byte> input, PoisonPagePlacement poison)
        { 
            int maxEncodedLength = Base64.GetMaxEncodedToUtf8Length(input.Length);
            using PooledBoundedMemory<byte> destPoisoned = PooledBoundedMemory<byte>.Rent(maxEncodedLength, poison);
            Span<byte> encoderDest = destPoisoned.Span;
            using PooledBoundedMemory<byte> decoderDestPoisoned = PooledBoundedMemory<byte>.Rent(Base64.GetMaxDecodedFromUtf8Length(maxEncodedLength), poison);
            Span<byte> decoderDest = decoderDestPoisoned.Span;
            { // IsFinalBlock = true
                OperationStatus status = Base64.EncodeToUtf8(input, encoderDest, out int bytesConsumed, out int bytesEncoded);
                Assert.Equal(OperationStatus.Done, status);
                Assert.Equal(input.Length, bytesConsumed);
                Assert.Equal(true, maxEncodedLength >= bytesEncoded && maxEncodedLength - 2 <= bytesEncoded);

                status = Base64.DecodeFromUtf8(encoderDest.Slice(0, bytesEncoded), decoderDest, out int bytesRead, out int bytesDecoded);

                Assert.Equal(OperationStatus.Done, status);
                Assert.Equal(input.Length, bytesDecoded);
                Assert.Equal(bytesEncoded, bytesRead);
                Assert.SequenceEqual(input, decoderDest.Slice(0, bytesDecoded));
            }

            { // IsFinalBlock = false
                encoderDest.Clear();
                decoderDest.Clear();
                OperationStatus status = Base64.EncodeToUtf8(input, encoderDest, out int bytesConsumed, out int bytesEncoded, isFinalBlock: false);
                Span<byte> decodeInput = encoderDest.Slice(0, bytesEncoded);

                if (input.Length % 3 == 0)
                {
                    Assert.Equal(OperationStatus.Done, status);
                    Assert.Equal(input.Length, bytesConsumed);
                    Assert.Equal(true, maxEncodedLength == bytesEncoded);

                    status = Base64.DecodeFromUtf8(decodeInput, decoderDest, out int bytesRead, out int bytesDecoded, isFinalBlock: false);

                    Assert.Equal(OperationStatus.Done, status);
                    Assert.Equal(input.Length, bytesDecoded);
                    Assert.Equal(bytesEncoded, bytesRead);
                    Assert.SequenceEqual(input, decoderDest.Slice(0, bytesDecoded));
                }
                else
                {
                    Assert.Equal(OperationStatus.NeedMoreData, status);
                    Assert.Equal(true, input.Length / 3 * 4 == bytesEncoded);

                    status = Base64.DecodeFromUtf8(decodeInput, decoderDest, out int bytesRead, out int bytesDecoded, isFinalBlock: false);

                    if (decodeInput.Length % 4 == 0)
                    {
                        Assert.Equal(OperationStatus.Done, status);
                        Assert.Equal(bytesConsumed, bytesDecoded);
                        Assert.Equal(bytesEncoded, bytesRead);
                    }
                    else
                    {
                        Assert.Equal(OperationStatus.NeedMoreData, status);
                    }

                    Assert.SequenceEqual(input.Slice(0, bytesDecoded), decoderDest.Slice(0, bytesDecoded));
                }
            }

            { // Encode / decode in place
                encoderDest.Clear();
                input.CopyTo(encoderDest);
                OperationStatus status = Base64.EncodeToUtf8InPlace(encoderDest, input.Length, out int bytesEncoded);

                Assert.Equal(OperationStatus.Done, status);
                Assert.Equal(true, maxEncodedLength >= bytesEncoded && maxEncodedLength - 2 <= bytesEncoded);

                status = Base64.DecodeFromUtf8InPlace(encoderDest.Slice(0, bytesEncoded), out int bytesDecoded);

                Assert.Equal(OperationStatus.Done, status);
                Assert.Equal(input.Length, bytesDecoded);
                Assert.SequenceEqual(input, encoderDest.Slice(0, bytesDecoded));
            }

            { // Decode the random input directly, Assert IsValid result matches with decoded result
                decoderDest.Clear();
                if (Base64.IsValid(input, out int decodedLength))
                {
                    int maxDecodedLength = Base64.GetMaxDecodedFromUtf8Length(input.Length);
                    OperationStatus status = Base64.DecodeFromUtf8(input, decoderDest, out int bytesRead, out int bytesDecoded);
                    Assert.Equal(OperationStatus.Done, status);
                    Assert.Equal(input.Length, bytesRead);
                    Assert.Equal(decodedLength, bytesDecoded);
                    Assert.Equal(true, maxDecodedLength >= bytesDecoded);

                    status = Base64.DecodeFromUtf8InPlace(input, out int inPlaceDecoded);
                    Assert.Equal(OperationStatus.Done, status);
                    Assert.Equal(bytesDecoded, inPlaceDecoded);
                    Assert.SequenceEqual<byte>(input.Slice(0, inPlaceDecoded), decoderDest.Slice(0, bytesDecoded));
                }
                else
                {
                    Assert.Equal(OperationStatus.InvalidData, Base64.DecodeFromUtf8(input, decoderDest, out int _, out int bytesDecoded));
                    Assert.Equal(OperationStatus.InvalidData, Base64.DecodeFromUtf8InPlace(input, out int inPlaceDecoded));
                }
            }

            { // Test new simplified UTF-8 APIs
                // Test EncodeToUtf8 returning byte[]
                byte[] encodedArray = Base64.EncodeToUtf8(input);
                Assert.Equal(true, maxEncodedLength >= encodedArray.Length && maxEncodedLength - 2 <= encodedArray.Length);

                // Test EncodeToUtf8 returning int
                encoderDest.Clear();
                int charsWritten = Base64.EncodeToUtf8(input, encoderDest);
                Assert.SequenceEqual(encodedArray.AsSpan(), encoderDest.Slice(0, charsWritten));

                // Test TryEncodeToUtf8
                encoderDest.Clear();
                Assert.Equal(true, Base64.TryEncodeToUtf8(input, encoderDest, out int tryCharsWritten));
                Assert.Equal(charsWritten, tryCharsWritten);
                Assert.SequenceEqual(encodedArray.AsSpan(), encoderDest.Slice(0, tryCharsWritten));

                // Test DecodeFromUtf8 returning byte[]
                byte[] decodedArray = Base64.DecodeFromUtf8(encodedArray);
                Assert.SequenceEqual(input, decodedArray.AsSpan());

                // Test DecodeFromUtf8 returning int
                decoderDest.Clear();
                int bytesWritten = Base64.DecodeFromUtf8(encodedArray, decoderDest);
                Assert.Equal(input.Length, bytesWritten);
                Assert.SequenceEqual(input, decoderDest.Slice(0, bytesWritten));

                // Test TryDecodeFromUtf8
                decoderDest.Clear();
                Assert.Equal(true, Base64.TryDecodeFromUtf8(encodedArray, decoderDest, out int tryBytesWritten));
                Assert.Equal(input.Length, tryBytesWritten);
                Assert.SequenceEqual(input, decoderDest.Slice(0, tryBytesWritten));

                // Test TryEncodeToUtf8InPlace
                using PooledBoundedMemory<byte> inPlaceBuffer = PooledBoundedMemory<byte>.Rent(maxEncodedLength, poison);
                Span<byte> inPlaceDest = inPlaceBuffer.Span;
                input.CopyTo(inPlaceDest);
                Assert.Equal(true, Base64.TryEncodeToUtf8InPlace(inPlaceDest, input.Length, out int inPlaceWritten));
                Assert.SequenceEqual(encodedArray.AsSpan(), inPlaceDest.Slice(0, inPlaceWritten));

                // Test GetEncodedLength matches GetMaxEncodedToUtf8Length
                Assert.Equal(Base64.GetMaxEncodedToUtf8Length(input.Length), Base64.GetEncodedLength(input.Length));

                // Test GetMaxDecodedLength matches GetMaxDecodedFromUtf8Length
                Assert.Equal(Base64.GetMaxDecodedFromUtf8Length(maxEncodedLength), Base64.GetMaxDecodedLength(maxEncodedLength));
            }
        }

        private static void TestBase64Chars(Span<byte> input, PoisonPagePlacement poison)
        {
            int encodedLength = Base64.GetEncodedLength(input.Length);
            int maxDecodedLength = Base64.GetMaxDecodedLength(encodedLength);

            using PooledBoundedMemory<char> destPoisoned = PooledBoundedMemory<char>.Rent(encodedLength, poison);
            using PooledBoundedMemory<byte> decoderDestPoisoned = PooledBoundedMemory<byte>.Rent(maxDecodedLength, poison);

            Span<char> encoderDest = destPoisoned.Span;
            Span<byte> decoderDest = decoderDestPoisoned.Span;

            { // IsFinalBlock = true
                OperationStatus status = Base64.EncodeToChars(input, encoderDest, out int bytesConsumed, out int charsEncoded);

                Assert.Equal(OperationStatus.Done, status);
                Assert.Equal(input.Length, bytesConsumed);
                Assert.Equal(encodedLength, charsEncoded);

                string encodedString = Base64.EncodeToString(input);
                Assert.Equal(encodedString, new string(encoderDest.Slice(0, charsEncoded)));

                status = Base64.DecodeFromChars(encoderDest.Slice(0, charsEncoded), decoderDest, out int charsRead, out int bytesDecoded);

                Assert.Equal(OperationStatus.Done, status);
                Assert.Equal(input.Length, bytesDecoded);
                Assert.Equal(charsEncoded, charsRead);
                Assert.SequenceEqual(input, decoderDest.Slice(0, bytesDecoded));
            }

            { // IsFinalBlock = false
                encoderDest.Clear();
                decoderDest.Clear();
                OperationStatus status = Base64.EncodeToChars(input, encoderDest, out int bytesConsumed, out int charsEncoded, isFinalBlock: false);
                Span<char> decodeInput = encoderDest.Slice(0, charsEncoded);

                if (input.Length % 3 == 0)
                {
                    Assert.Equal(OperationStatus.Done, status);
                    Assert.Equal(input.Length, bytesConsumed);
                    Assert.Equal(encodedLength, charsEncoded);

                    status = Base64.DecodeFromChars(decodeInput, decoderDest, out int charsRead, out int bytesDecoded, isFinalBlock: false);

                    Assert.Equal(OperationStatus.Done, status);
                    Assert.Equal(input.Length, bytesDecoded);
                    Assert.Equal(charsEncoded, charsRead);
                    Assert.SequenceEqual(input, decoderDest.Slice(0, bytesDecoded));
                }
                else
                {
                    Assert.Equal(OperationStatus.NeedMoreData, status);
                    Assert.Equal(true, input.Length / 3 * 4 == charsEncoded);

                    status = Base64.DecodeFromChars(decodeInput, decoderDest, out int charsRead, out int bytesDecoded, isFinalBlock: false);

                    if (decodeInput.Length % 4 == 0)
                    {
                        Assert.Equal(OperationStatus.Done, status);
                        Assert.Equal(bytesConsumed, bytesDecoded);
                        Assert.Equal(charsEncoded, charsRead);
                    }
                    else
                    {
                        Assert.Equal(OperationStatus.NeedMoreData, status);
                    }

                    Assert.SequenceEqual(input.Slice(0, bytesDecoded), decoderDest.Slice(0, bytesDecoded));
                }
            }

            { // Test array-returning and int-returning overloads
                char[] encodedChars = Base64.EncodeToChars(input);
                Assert.Equal(encodedLength, encodedChars.Length);

                encoderDest.Clear();
                int charsWritten = Base64.EncodeToChars(input, encoderDest);
                Assert.Equal(encodedLength, charsWritten);
                Assert.SequenceEqual(encodedChars.AsSpan(), encoderDest.Slice(0, charsWritten));

                byte[] decodedBytes = Base64.DecodeFromChars(encodedChars);
                Assert.SequenceEqual(input, decodedBytes.AsSpan());

                decoderDest.Clear();
                int bytesWritten = Base64.DecodeFromChars(encodedChars, decoderDest);
                Assert.Equal(input.Length, bytesWritten);
                Assert.SequenceEqual(input, decoderDest.Slice(0, bytesWritten));
            }

            { // Test Try* variants
                encoderDest.Clear();
                Assert.Equal(true, Base64.TryEncodeToChars(input, encoderDest, out int charsWritten));
                Assert.Equal(encodedLength, charsWritten);

                decoderDest.Clear();
                Assert.Equal(true, Base64.TryDecodeFromChars(encoderDest.Slice(0, charsWritten), decoderDest, out int bytesWritten));
                Assert.Equal(input.Length, bytesWritten);
                Assert.SequenceEqual(input, decoderDest.Slice(0, bytesWritten));
            }

            { // Decode the random chars directly (as chars, from the input bytes interpreted as UTF-16)
                // Create a char span from the input bytes for testing decode with random data
                if (input.Length >= 2)
                {
                    ReadOnlySpan<char> inputChars = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, char>(input);
                    decoderDest.Clear();

                    // Try decoding - may succeed or fail depending on if input is valid base64
                    OperationStatus status = Base64.DecodeFromChars(inputChars, decoderDest, out int charsConsumed, out int bytesDecoded);
                    // Just verify we don't crash - the result depends on input validity
                    Assert.Equal(true, status == OperationStatus.Done || status == OperationStatus.InvalidData ||
                                       status == OperationStatus.NeedMoreData || status == OperationStatus.DestinationTooSmall);
                }
            }
        }

        private static void TestToStringToCharArray(Span<byte> input, Base64FormattingOptions options)
        {
            int encodedLength = ToBase64_CalculateOutputLength(input.Length, options == Base64FormattingOptions.InsertLineBreaks);
            char[] dest = new char[encodedLength];

            string toStringResult = Convert.ToBase64String(input, options);
            byte[] decoded = Convert.FromBase64String(toStringResult);

            Assert.SequenceEqual(input, decoded);

            int written = Convert.ToBase64CharArray(input.ToArray(), 0, input.Length, dest, 0, options);
            decoded = Convert.FromBase64CharArray(dest, 0, written);

            Assert.SequenceEqual(input, decoded);
            Assert.SequenceEqual(toStringResult.AsSpan(), dest.AsSpan(0, written));
        }

        private static int ToBase64_CalculateOutputLength(int inputLength, bool insertLineBreaks)
        {
            uint outlen = ((uint)inputLength + 2) / 3 * 4;

            if (outlen == 0)
                return 0;

            if (insertLineBreaks)
            {
                (uint newLines, uint remainder) = Math.DivRem(outlen, Base64LineBreakPosition);
                if (remainder == 0)
                {
                    --newLines;
                }
                outlen += newLines * 2; // 2 line break chars added: "\r\n"
            }

            return (int)outlen;
        }
    }
}
