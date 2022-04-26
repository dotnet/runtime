// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

namespace System.Security.Cryptography.Cose
{
    // https://www.iana.org/assignments/cose/cose.xhtml#algorithms
    internal static class KnownCoseAlgorithms
    {
        // ECDsa w/SHA
        internal const int ES256 = -7;
        internal const int ES384 = -35;
        internal const int ES512 = -36;
        // RSASSA-PSS w/SHA
        internal const int PS256 = -37;
        internal const int PS384 = -38;
        internal const int PS512 = -39;

        internal static void ThrowIfNotSupported(long alg)
        {
            if (alg != ES256 && alg > ES384 && alg < PS512)
            {
                throw new CryptographicException(SR.Format(SR.Sign1UnknownCoseAlgorithm, alg));
            }
        }

        internal static void ThrowUnsignedIntegerNotSupported(ulong alg) // All algorithm valid values are negatives.
            => throw new CryptographicException(SR.Format(SR.Sign1UnknownCoseAlgorithm, alg));

        internal static void ThrowCborNegativeIntegerNotSupported(ulong alg) // Cbor Negative Integer Representation is too big.
            => throw new CryptographicException(SR.Format(SR.Sign1UnknownCoseAlgorithm, BigInteger.MinusOne - new BigInteger(alg)));

        internal static int FromString(string algString)
        {
            return algString switch
            {
                nameof(ES256) => ES256,
                nameof(ES384) => ES384,
                nameof(ES512) => ES512,
                nameof(PS256) => PS256,
                nameof(PS384) => PS384,
                nameof(PS512) => PS512,
                _ => throw new CryptographicException(SR.Format(SR.Sign1UnknownCoseAlgorithm, algString))
            };
        }
    }
}
