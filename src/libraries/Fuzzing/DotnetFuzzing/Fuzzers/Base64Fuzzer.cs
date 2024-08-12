// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Buffers;
using System.Buffers.Text;

namespace DotnetFuzzing.Fuzzers
{
    internal class Base64Fuzzer : IFuzzer
    {
        public string[] TargetAssemblies => [];

        public string[] TargetCoreLibPrefixes => ["System.Buffers.Text"];

        public void FuzzTarget(ReadOnlySpan<byte> bytes)
        {
            using PooledBoundedMemory<byte> inputPoisoned = PooledBoundedMemory<byte>.Rent(bytes, PoisonPagePlacement.After);
            Span<byte> input = inputPoisoned.Span;
            int maxEncodedLength = Base64.GetMaxEncodedToUtf8Length(bytes.Length);
            using PooledBoundedMemory<byte> destPoisoned = PooledBoundedMemory<byte>.Rent(maxEncodedLength, PoisonPagePlacement.After);
            Span<byte> encoderDest = destPoisoned.Span;
            using PooledBoundedMemory<byte> decoderDestPoisoned = PooledBoundedMemory<byte>.Rent(Base64.GetMaxDecodedFromUtf8Length(maxEncodedLength), PoisonPagePlacement.After);
            Span<byte> decoderDest = decoderDestPoisoned.Span;
            { // IsFinalBlock = true
                OperationStatus status = Base64.EncodeToUtf8(input, encoderDest, out int bytesConsumed, out int bytesEncoded);

                Assert.Equal(OperationStatus.Done, status);
                Assert.Equal(bytes.Length, bytesConsumed);
                Assert.Equal(true, maxEncodedLength >= bytesEncoded && maxEncodedLength - 2 <= bytesEncoded);       
                
                status = Base64.DecodeFromUtf8(encoderDest.Slice(0, bytesEncoded), decoderDest, out int bytesRead, out int bytesDecoded);

                Assert.Equal(OperationStatus.Done, status);
                Assert.Equal(bytes.Length, bytesDecoded);
                Assert.Equal(bytesEncoded, bytesRead);
                Assert.SequenceEqual(bytes, decoderDest.Slice(0, bytesDecoded));
            }

            { // IsFinalBlock = false
                encoderDest.Clear();
                decoderDest.Clear();
                OperationStatus status = Base64.EncodeToUtf8(input, encoderDest, out int bytesConsumed, out int bytesEncoded, isFinalBlock: false);
                Span<byte> decodeInput = encoderDest.Slice(0, bytesEncoded);

                if (bytes.Length % 3 == 0)
                {
                    Assert.Equal(OperationStatus.Done, status);
                    Assert.Equal(bytes.Length, bytesConsumed);
                    Assert.Equal(true, maxEncodedLength == bytesEncoded);

                    status = Base64.DecodeFromUtf8(decodeInput, decoderDest, out int bytesRead, out int bytesDecoded, isFinalBlock: false);

                    Assert.Equal(OperationStatus.Done, status);
                    Assert.Equal(bytes.Length, bytesDecoded);
                    Assert.Equal(bytesEncoded, bytesRead);
                    Assert.SequenceEqual(bytes, decoderDest.Slice(0, bytesDecoded));
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

                    Assert.SequenceEqual(bytes.Slice(0, bytesDecoded), decoderDest.Slice(0, bytesDecoded));
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
                Assert.Equal(bytes.Length, bytesDecoded);
                Assert.SequenceEqual(bytes, encoderDest.Slice(0, bytesDecoded));
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
    }
}
