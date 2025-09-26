// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        internal enum PAL_HashAlgorithm
        {
            Unknown = 0,
            Md5,
            Sha1,
            Sha256,
            Sha384,
            Sha512,
        }

        private static PAL_HashAlgorithm PalAlgorithmFromAlgorithmName(HashAlgorithmName hashAlgorithmName) =>
            hashAlgorithmName == HashAlgorithmName.MD5 ? PAL_HashAlgorithm.Md5 :
            hashAlgorithmName == HashAlgorithmName.SHA1 ? PAL_HashAlgorithm.Sha1 :
            hashAlgorithmName == HashAlgorithmName.SHA256 ? PAL_HashAlgorithm.Sha256 :
            hashAlgorithmName == HashAlgorithmName.SHA384 ? PAL_HashAlgorithm.Sha384 :
            hashAlgorithmName == HashAlgorithmName.SHA512 ? PAL_HashAlgorithm.Sha512 :
            throw new CryptographicException(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmName.Name);
    }
}
