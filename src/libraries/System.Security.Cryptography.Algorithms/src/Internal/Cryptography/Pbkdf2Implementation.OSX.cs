// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using PAL_HashAlgorithm = Interop.AppleCrypto.PAL_HashAlgorithm;

namespace Internal.Cryptography
{
    internal partial class Pbkdf2Implementation
    {
        public static unsafe void Fill(
            ReadOnlySpan<byte> password,
            ReadOnlySpan<byte> salt,
            int iterations,
            HashAlgorithmName hashAlgorithmName,
            Span<byte> destination)
        {
            Debug.Assert(!salt.IsEmpty);
            Debug.Assert(!destination.IsEmpty);

            PAL_HashAlgorithm prfAlgorithm = hashAlgorithmName.Name switch {
                "SHA1" => PAL_HashAlgorithm.Sha1,
                "SHA256" => PAL_HashAlgorithm.Sha256,
                "SHA384" => PAL_HashAlgorithm.Sha384,
                "SHA512" => PAL_HashAlgorithm.Sha512,
                _ => throw new CryptographicException() // Should have been validated before getting here.
            };

            Interop.AppleCrypto.KeyDerivationPBKDF(prfAlgorithm, password, salt, iterations, destination);
        }
    }
}
