// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "Weak algorithms are used as instructed by the caller")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5351", Justification = "Weak algorithms are used as instructed by the caller")]
    internal static class HashOneShotHelpers
    {
        public static int MacData(
            HashAlgorithmName hashAlgorithm,
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> source,
            Span<byte> destination)
        {
            if (hashAlgorithm == HashAlgorithmName.SHA256)
            {
                return HMACSHA256.HashData(key, source, destination);
            }
            else if (hashAlgorithm == HashAlgorithmName.SHA1)
            {
                return HMACSHA1.HashData(key, source, destination);
            }
            else if (hashAlgorithm == HashAlgorithmName.SHA512)
            {
                return HMACSHA512.HashData(key, source, destination);
            }
            else if (hashAlgorithm == HashAlgorithmName.SHA384)
            {
                return HMACSHA384.HashData(key, source, destination);
            }
            else if (hashAlgorithm == HashAlgorithmName.MD5)
            {
                return HMACMD5.HashData(key, source, destination);
            }

            throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name));
        }
    }
}
