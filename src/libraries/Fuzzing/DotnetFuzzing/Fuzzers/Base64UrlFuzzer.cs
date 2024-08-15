// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Formats.Asn1;
using System.Reflection;

namespace DotnetFuzzing.Fuzzers
{
    internal class Base64UrlFuzzer : IFuzzer
    {
        public string[] TargetAssemblies => [];

        public string[] TargetCoreLibPrefixes => ["System.Buffers.Text"];

        public void FuzzTarget(ReadOnlySpan<byte> bytes)
        {
            using PooledBoundedMemory<byte> inputPoisoned = PooledBoundedMemory<byte>.Rent(bytes, PoisonPagePlacement.After);
            Span<byte> input = inputPoisoned.Span;
            int encodedLength = Base64Url.GetEncodedLength(bytes.Length);
            using PooledBoundedMemory<char> destPoisoned = PooledBoundedMemory<char>.Rent(encodedLength, PoisonPagePlacement.After);
            Span<char> encoderDest = destPoisoned.Span;
            int maxDecodedLength = Base64Url.GetMaxDecodedLength(encodedLength);
            using PooledBoundedMemory<byte> decoderDestPoisoned = PooledBoundedMemory<byte>.Rent(maxDecodedLength, PoisonPagePlacement.After);
            Span<byte> decoderDest = decoderDestPoisoned.Span;
            { // IsFinalBlock = true
                OperationStatus status = Base64Url.EncodeToChars(input, encoderDest, out int bytesConsumed, out int bytesEncoded); 

                Assert.Equal(OperationStatus.Done, status);
                Assert.Equal(bytes.Length, bytesConsumed);
                Assert.Equal(true, encodedLength == bytesEncoded);

                string encodedString = Base64Url.EncodeToString(input);
                Assert.Equal(encodedString, new string(encoderDest));

                status = Base64Url.DecodeFromChars(encoderDest, decoderDest, out int bytesRead, out int bytesDecoded);

                Assert.Equal(OperationStatus.Done, status);
                Assert.Equal(bytes.Length, bytesDecoded);
                Assert.Equal(bytesEncoded, bytesRead);
                Assert.SequenceEqual(bytes, decoderDest.Slice(0, bytesDecoded));
            }

            { // IsFinalBlock = false
                encoderDest.Clear();
                decoderDest.Clear();
                OperationStatus status = Base64Url.EncodeToChars(input, encoderDest, out int bytesConsumed, out int bytesEncoded, isFinalBlock: false);
                Span<char> decodeInput = encoderDest.Slice(0, bytesEncoded);

                if (bytes.Length % 3 == 0)
                {
                    Assert.Equal(OperationStatus.Done, status);
                    Assert.Equal(bytes.Length, bytesConsumed);
                    Assert.Equal(true, encodedLength == bytesEncoded);

                    status = Base64Url.DecodeFromChars(decodeInput, decoderDest, out int bytesRead, out int bytesDecoded, isFinalBlock: false);

                    Assert.Equal(OperationStatus.Done, status);
                    Assert.Equal(bytes.Length, bytesDecoded);
                    Assert.Equal(bytesEncoded, bytesRead);
                    Assert.SequenceEqual(bytes, decoderDest.Slice(0, bytesDecoded));
                }
                else
                {
                    Assert.Equal(OperationStatus.NeedMoreData, status);
                    Assert.Equal(true, input.Length / 3 * 4 == bytesEncoded);

                    status = Base64Url.DecodeFromChars(decodeInput, decoderDest, out int bytesRead, out int bytesDecoded, isFinalBlock: false);

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

                    Assert.SequenceEqual(bytes.Slice(0, bytesDecoded), decoderDest.Slice(0, bytesDecoded));
                }
            }

            { // Encode / decode in place
                using PooledBoundedMemory<byte> destPoisoned2 = PooledBoundedMemory<byte>.Rent(encodedLength, PoisonPagePlacement.After);
                Span<byte> tryEncode = destPoisoned2.Span;
                input.CopyTo(tryEncode);

                Assert.Equal(true, Base64Url.TryEncodeToUtf8InPlace(tryEncode, input.Length, out int bytesEncoded));
                Assert.Equal(encodedLength, bytesEncoded);

                int bytesDecoded = Base64Url.DecodeFromUtf8InPlace(tryEncode);

                Assert.Equal(bytes.Length, bytesDecoded);
                Assert.SequenceEqual(bytes, tryEncode.Slice(0, bytesDecoded));
            }

            { // Decode the random input directly, Assert IsValid result matches with decoded result
                decoderDest.Clear();
                if (Base64Url.IsValid(input, out int decodedLength))
                {
                    maxDecodedLength = Base64Url.GetMaxDecodedLength(input.Length);
                    OperationStatus status = Base64Url.DecodeFromUtf8(input, decoderDest, out int bytesRead, out int bytesDecoded);
                    Assert.Equal(OperationStatus.Done, status);
                    Assert.Equal(input.Length, bytesRead);
                    Assert.Equal(decodedLength, bytesDecoded);
                    Assert.Equal(true, maxDecodedLength >= bytesDecoded);

                    Span<byte> tryDecode = new byte[maxDecodedLength];
                    Assert.Equal(true, Base64Url.TryDecodeFromUtf8(input, tryDecode, out bytesDecoded));
                    Assert.Equal(decodedLength, bytesDecoded);
                    Assert.SequenceEqual<byte>(tryDecode.Slice(0, bytesDecoded), decoderDest.Slice(0, bytesDecoded));

                    int decoded = Base64Url.DecodeFromUtf8InPlace(input);
                    Assert.Equal(OperationStatus.Done, status);
                    Assert.Equal(bytesDecoded, decoded);
                    Assert.SequenceEqual<byte>(input.Slice(0, decoded), decoderDest.Slice(0, bytesDecoded));
                }
                else
                {
                    Assert.Equal(OperationStatus.InvalidData, Base64Url.DecodeFromUtf8(input, decoderDest, out int _, out int bytesDecoded));
                    try
                    {
                        Assert.Equal(0, Base64Url.DecodeFromUtf8InPlace(input));
                    }
                    catch (FormatException) { /* DecodeFromUtf8InPlace would throw FormatException for InvalidData*/ }
                }
            }
        }
    }
}
