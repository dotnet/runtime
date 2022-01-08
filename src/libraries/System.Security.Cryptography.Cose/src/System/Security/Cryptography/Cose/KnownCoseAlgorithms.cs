// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Cose
{
    // https://www.iana.org/assignments/cose/cose.xhtml#algorithms
    internal static class KnownCoseAlgorithms
    {
        // ECDsa w/SHA
        public const int ES256 = -7;
        public const int ES384 = -35;
        public const int ES512 = -36;
        // RSASSA-PSS w/SHA
        public const int PS256 = -37;
        public const int PS384 = -38;
        public const int PS512 = -39;

        public static void ThrowIfNotSupported(int alg)
        {
            if (alg != ES256 && alg > ES384 && alg < PS512)
            {
                throw new CryptographicException(SR.Format(SR.Sign1UnknownCoseAlgorithm, alg));
            }
        }

        public static int FromString(string algString)
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
