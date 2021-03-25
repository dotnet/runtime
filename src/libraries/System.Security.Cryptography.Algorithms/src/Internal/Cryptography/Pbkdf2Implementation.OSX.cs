// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using PAL_HashAlgorithm = Interop.AppleCrypto.PAL_HashAlgorithm;

namespace Internal.Cryptography
{
    internal static partial class Pbkdf2Implementation
    {
        public static unsafe void Fill(
            ReadOnlySpan<byte> password,
            ReadOnlySpan<byte> salt,
            int iterations,
            HashAlgorithmName hashAlgorithmName,
            Span<byte> destination)
        {
            Debug.Assert(!destination.IsEmpty);

            PAL_HashAlgorithm prfAlgorithm;

            switch (hashAlgorithmName.Name)
            {
                case HashAlgorithmNames.SHA1:
                    prfAlgorithm = PAL_HashAlgorithm.Sha1;
                    break;
                case HashAlgorithmNames.SHA256:
                    prfAlgorithm = PAL_HashAlgorithm.Sha256;
                    break;
                case HashAlgorithmNames.SHA384:
                    prfAlgorithm = PAL_HashAlgorithm.Sha384;
                    break;
                case HashAlgorithmNames.SHA512:
                    prfAlgorithm = PAL_HashAlgorithm.Sha512;
                    break;
                default:
                    Debug.Fail($"Unexpected hash algorithm '{hashAlgorithmName.Name}'");
                    throw new CryptographicException();
            };

            Interop.AppleCrypto.Pbkdf2(prfAlgorithm, password, salt, iterations, destination);
        }
    }
}
