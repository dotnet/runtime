// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Cryptography;
using System.IO;

namespace System.Security.Cryptography
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "Weak algorithms are used as instructed by the caller")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5351", Justification = "Weak algorithms are used as instructed by the caller")]
    internal static class HashOneShotHelpers
    {
        internal static byte[] HashData(HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> source)
        {
            if (hashAlgorithm == HashAlgorithmName.SHA256)
            {
                return SHA256.HashData(source);
            }
            else if (hashAlgorithm == HashAlgorithmName.SHA1)
            {
                return SHA1.HashData(source);
            }
            else if (hashAlgorithm == HashAlgorithmName.SHA512)
            {
                return SHA512.HashData(source);
            }
            else if (hashAlgorithm == HashAlgorithmName.SHA384)
            {
                return SHA384.HashData(source);
            }
            else if (Helpers.HasMD5 && hashAlgorithm == HashAlgorithmName.MD5)
            {
                return MD5.HashData(source);
            }

            throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name));
        }

        internal static bool TryHashData(
            HashAlgorithmName hashAlgorithm,
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            out int bytesWritten)
        {
            if (hashAlgorithm == HashAlgorithmName.SHA256)
            {
                return SHA256.TryHashData(source, destination, out bytesWritten);
            }
            else if (hashAlgorithm == HashAlgorithmName.SHA1)
            {
                return SHA1.TryHashData(source, destination, out bytesWritten);
            }
            else if (hashAlgorithm == HashAlgorithmName.SHA512)
            {
                return SHA512.TryHashData(source, destination, out bytesWritten);
            }
            else if (hashAlgorithm == HashAlgorithmName.SHA384)
            {
                return SHA384.TryHashData(source, destination, out bytesWritten);
            }
            else if (Helpers.HasMD5 && hashAlgorithm == HashAlgorithmName.MD5)
            {
                return MD5.TryHashData(source, destination, out bytesWritten);
            }

            throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name));
        }

        internal static byte[] HashData(HashAlgorithmName hashAlgorithm, Stream source)
        {
            if (hashAlgorithm == HashAlgorithmName.SHA256)
            {
                return SHA256.HashData(source);
            }
            else if (hashAlgorithm == HashAlgorithmName.SHA1)
            {
                return SHA1.HashData(source);
            }
            else if (hashAlgorithm == HashAlgorithmName.SHA512)
            {
                return SHA512.HashData(source);
            }
            else if (hashAlgorithm == HashAlgorithmName.SHA384)
            {
                return SHA384.HashData(source);
            }
            else if (Helpers.HasMD5 && hashAlgorithm == HashAlgorithmName.MD5)
            {
                return MD5.HashData(source);
            }

            throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name));
        }

        internal static int MacData(
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
            else if (Helpers.HasMD5 && hashAlgorithm == HashAlgorithmName.MD5)
            {
                return HMACMD5.HashData(key, source, destination);
            }

            throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name));
        }
    }
}
