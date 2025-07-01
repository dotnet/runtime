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
