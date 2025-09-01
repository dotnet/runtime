// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;

namespace System.Security.Cryptography
{
    internal static partial class EccKeyFormatHelper
    {
        internal static void GetECPointFromUncompressedPublicKey(ReadOnlySpan<byte> publicKey, int fieldWidthInBytes, out byte[] x, out byte[] y)
        {
            if (publicKey.Length == 0)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            // Implementation limitation
            // 04 (Uncompressed ECPoint) is almost always used.
            if (publicKey[0] != 0x04)
            {
                throw new CryptographicException(SR.Cryptography_NotValidPublicOrPrivateKey);
            }

            // https://www.secg.org/sec1-v2.pdf, 2.3.4, #3 (M has length 2 * CEIL(log2(q)/8) + 1)
            if (publicKey.Length != 2 * fieldWidthInBytes + 1)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            x = publicKey.Slice(1, fieldWidthInBytes).ToArray();
            y = publicKey.Slice(1 + fieldWidthInBytes).ToArray();
        }

        internal static void WriteUncompressedPublicKey(byte[] x, byte[] y, AsnWriter writer)
        {
            int publicKeyLength = x.Length * 2 + 1;

            // A NIST P-521 Q will encode to 133 bytes: (521 + 7)/8 * 2 + 1.
            // 256 should be plenty for all but very atypical uses.
            const int MaxStackAllocSize = 256;
            Span<byte> publicKeyBytes = stackalloc byte[MaxStackAllocSize];
            byte[]? rented = null;

            if (publicKeyLength > MaxStackAllocSize)
            {
                publicKeyBytes = rented = CryptoPool.Rent(publicKeyLength);
            }

            publicKeyBytes[0] = 0x04;
            x.CopyTo(publicKeyBytes.Slice(1));
            y.CopyTo(publicKeyBytes.Slice(1 + x.Length));

            writer.WriteBitString(publicKeyBytes.Slice(0, publicKeyLength));

            if (rented is not null)
            {
                // Q contains public EC parameters that are not sensitive.
                CryptoPool.Return(rented, clearSize: 0);
            }
        }
    }
}
