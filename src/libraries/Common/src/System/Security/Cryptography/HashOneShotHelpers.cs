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
            return hashAlgorithm.Name switch
            {
                HashAlgorithmNames.SHA256 => SHA256.HashData(source),
                HashAlgorithmNames.SHA1 => SHA1.HashData(source),
                HashAlgorithmNames.SHA512 => SHA512.HashData(source),
                HashAlgorithmNames.SHA384 => SHA384.HashData(source),
                HashAlgorithmNames.SHA3_256 => SHA3_256.HashData(source),
                HashAlgorithmNames.SHA3_384 => SHA3_384.HashData(source),
                HashAlgorithmNames.SHA3_512 => SHA3_512.HashData(source),
                HashAlgorithmNames.MD5 when Helpers.HasMD5 => MD5.HashData(source),
                _ => throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name)),
            };
        }

        internal static bool TryHashData(
            HashAlgorithmName hashAlgorithm,
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            out int bytesWritten)
        {
            return hashAlgorithm.Name switch
            {
                HashAlgorithmNames.SHA256 => SHA256.TryHashData(source, destination, out bytesWritten),
                HashAlgorithmNames.SHA1 => SHA1.TryHashData(source, destination, out bytesWritten),
                HashAlgorithmNames.SHA512 => SHA512.TryHashData(source, destination, out bytesWritten),
                HashAlgorithmNames.SHA384 => SHA384.TryHashData(source, destination, out bytesWritten),
                HashAlgorithmNames.SHA3_256 => SHA3_256.TryHashData(source, destination, out bytesWritten),
                HashAlgorithmNames.SHA3_384 => SHA3_384.TryHashData(source, destination, out bytesWritten),
                HashAlgorithmNames.SHA3_512 => SHA3_512.TryHashData(source, destination, out bytesWritten),
                HashAlgorithmNames.MD5 when Helpers.HasMD5 => MD5.TryHashData(source, destination, out bytesWritten),
                _ => throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name)),
            };
        }

        internal static byte[] HashData(HashAlgorithmName hashAlgorithm, Stream source)
        {
            return hashAlgorithm.Name switch
            {
                HashAlgorithmNames.SHA256 => SHA256.HashData(source),
                HashAlgorithmNames.SHA1 => SHA1.HashData(source),
                HashAlgorithmNames.SHA512 => SHA512.HashData(source),
                HashAlgorithmNames.SHA384 => SHA384.HashData(source),
                HashAlgorithmNames.SHA3_256 => SHA3_256.HashData(source),
                HashAlgorithmNames.SHA3_384 => SHA3_384.HashData(source),
                HashAlgorithmNames.SHA3_512 => SHA3_512.HashData(source),
                HashAlgorithmNames.MD5 when Helpers.HasMD5 => MD5.HashData(source),
                _ => throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name)),
            };
        }

        internal static int MacData(
            HashAlgorithmName hashAlgorithm,
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> source,
            Span<byte> destination)
        {
            return hashAlgorithm.Name switch
            {
                HashAlgorithmNames.SHA256 => HMACSHA256.HashData(key, source, destination),
                HashAlgorithmNames.SHA1 => HMACSHA1.HashData(key, source, destination),
                HashAlgorithmNames.SHA512 => HMACSHA512.HashData(key, source, destination),
                HashAlgorithmNames.SHA384 => HMACSHA384.HashData(key, source, destination),
                HashAlgorithmNames.SHA3_256 => HMACSHA3_256.HashData(key, source, destination),
                HashAlgorithmNames.SHA3_384 => HMACSHA3_384.HashData(key, source, destination),
                HashAlgorithmNames.SHA3_512 => HMACSHA3_512.HashData(key, source, destination),
                HashAlgorithmNames.MD5 when Helpers.HasMD5 => HMACMD5.HashData(key, source, destination),
                _ => throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name)),
            };
        }
    }
}
