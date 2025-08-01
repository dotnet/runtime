// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    }
}
