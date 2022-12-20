// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using PAL_HashAlgorithm = Interop.AppleCrypto.PAL_HashAlgorithm;

namespace System.Security.Cryptography
{
    internal static partial class HashAlgorithmNames
    {
        internal static PAL_HashAlgorithm HashAlgorithmToPal(string hashAlgorithmId) => hashAlgorithmId switch {
            HashAlgorithmNames.MD5 => PAL_HashAlgorithm.Md5,
            HashAlgorithmNames.SHA1 => PAL_HashAlgorithm.Sha1,
            HashAlgorithmNames.SHA256 => PAL_HashAlgorithm.Sha256,
            HashAlgorithmNames.SHA384 => PAL_HashAlgorithm.Sha384,
            HashAlgorithmNames.SHA512 => PAL_HashAlgorithm.Sha512,
            _ => throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmId))
        };
    }
}
