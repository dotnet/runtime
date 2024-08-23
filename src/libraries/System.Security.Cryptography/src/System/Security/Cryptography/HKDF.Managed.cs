// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Versioning;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public static partial class HKDF
    {
        private static void Extract(HashAlgorithmName hashAlgorithmName, int hashLength, ReadOnlySpan<byte> ikm, ReadOnlySpan<byte> salt, Span<byte> prk)
        {
            Debug.Assert(HashLength(hashAlgorithmName) == hashLength);
            int written = CryptographicOperations.HmacData(hashAlgorithmName, salt, ikm, prk);
            Debug.Assert(written == prk.Length, $"Bytes written is {written} bytes which does not match output length ({prk.Length} bytes)");
        }

        private static void Expand(HashAlgorithmName hashAlgorithmName, int hashLength, ReadOnlySpan<byte> prk, Span<byte> output, ReadOnlySpan<byte> info)
        {
            Debug.Assert(HashLength(hashAlgorithmName) == hashLength);

            byte counter = 0;
            var counterSpan = new Span<byte>(ref counter);
            Span<byte> t = Span<byte>.Empty;
            Span<byte> remainingOutput = output;

            const int MaxStackInfoBuffer = 64;
            Span<byte> tempInfoBuffer = stackalloc byte[MaxStackInfoBuffer];
            scoped ReadOnlySpan<byte> infoBuffer;
            byte[]? rentedTempInfoBuffer = null;

            if (output.Overlaps(info))
            {
                if (info.Length > MaxStackInfoBuffer)
                {
                    rentedTempInfoBuffer = CryptoPool.Rent(info.Length);
                    tempInfoBuffer = rentedTempInfoBuffer;
                }

                tempInfoBuffer = tempInfoBuffer.Slice(0, info.Length);
                info.CopyTo(tempInfoBuffer);
                infoBuffer = tempInfoBuffer;
            }
            else
            {
                infoBuffer = info;
            }

            using (IncrementalHash hmac = IncrementalHash.CreateHMAC(hashAlgorithmName, prk))
            {
                for (int i = 1; ; i++)
                {
                    hmac.AppendData(t);
                    hmac.AppendData(infoBuffer);
                    counter = (byte)i;
                    hmac.AppendData(counterSpan);

                    if (remainingOutput.Length >= hashLength)
                    {
                        t = remainingOutput.Slice(0, hashLength);
                        remainingOutput = remainingOutput.Slice(hashLength);
                        GetHashAndReset(hmac, t);
                    }
                    else
                    {
                        if (remainingOutput.Length > 0)
                        {
                            Debug.Assert(hashLength <= 512 / 8, "hashLength is larger than expected, consider increasing this value or using regular allocation");
                            Span<byte> lastChunk = stackalloc byte[hashLength];
                            GetHashAndReset(hmac, lastChunk);
                            lastChunk.Slice(0, remainingOutput.Length).CopyTo(remainingOutput);
                        }

                        break;
                    }
                }
            }

            if (rentedTempInfoBuffer is not null)
            {
                CryptoPool.Return(rentedTempInfoBuffer, clearSize: info.Length);
            }
        }

        private static void DeriveKeyCore(HashAlgorithmName hashAlgorithmName, int hashLength, ReadOnlySpan<byte> ikm, Span<byte> output, ReadOnlySpan<byte> salt, ReadOnlySpan<byte> info)
        {
            Span<byte> prk = stackalloc byte[hashLength];

            Extract(hashAlgorithmName, hashLength, ikm, salt, prk);
            Expand(hashAlgorithmName, hashLength, prk, output, info);
        }

        private static void GetHashAndReset(IncrementalHash hmac, Span<byte> output)
        {
            if (!hmac.TryGetHashAndReset(output, out int bytesWritten))
            {
                Debug.Fail("HMAC operation failed unexpectedly");
                throw new CryptographicException(SR.Arg_CryptographyException);
            }

            Debug.Assert(bytesWritten == output.Length, $"Bytes written is {bytesWritten} bytes which does not match output length ({output.Length} bytes)");
        }
    }
}
