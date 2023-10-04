// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Cryptography;
using System.Diagnostics;
using System.IO;

namespace System.Security.Cryptography
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "Weak algorithms are used as instructed by the caller")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5351", Justification = "Weak algorithms are used as instructed by the caller")]
    internal static class HashOneShotHelpers
    {
        internal static byte[] HashData(HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> source)
        {
            int hashSizeInBytes = HashSize(hashAlgorithm);
            byte[] result = new byte[hashSizeInBytes];
            int written = HashProviderDispenser.OneShotHashProvider.HashData(hashAlgorithm.Name!, source, result);
            Debug.Assert(written == hashSizeInBytes);
            return result;
        }

        internal static bool TryHashData(
            HashAlgorithmName hashAlgorithm,
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            out int bytesWritten)
        {
            int hashSizeInBytes = HashSize(hashAlgorithm);

            if (destination.Length < hashSizeInBytes)
            {
                bytesWritten = 0;
                return false;
            }

            bytesWritten = HashProviderDispenser.OneShotHashProvider.HashData(hashAlgorithm.Name!, source, destination);
            Debug.Assert(bytesWritten == hashSizeInBytes);
            return true;
        }

        internal static byte[] HashData(HashAlgorithmName hashAlgorithm, Stream source)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (!source.CanRead)
            {
                throw new ArgumentException(SR.Argument_StreamNotReadable, nameof(source));
            }

            int hashSizeInBytes = HashSize(hashAlgorithm);
            return LiteHashProvider.HashStream(hashAlgorithm.Name!, hashSizeInBytes, source);
        }

        internal static int MacData(
            HashAlgorithmName hashAlgorithm,
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> source,
            Span<byte> destination)
        {
            int macSizeInBytes = MacSize(hashAlgorithm);

            if (destination.Length < macSizeInBytes)
            {
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
            }

            int written = HashProviderDispenser.OneShotHashProvider.MacData(hashAlgorithm.Name!, key, source, destination);
            Debug.Assert(written == macSizeInBytes);
            return written;
        }

        private static int HashSize(HashAlgorithmName hashAlgorithm)
        {
            switch (hashAlgorithm.Name)
            {
                case HashAlgorithmNames.SHA256:
                    return SHA256.HashSizeInBytes;
                case HashAlgorithmNames.SHA1:
                    return SHA1.HashSizeInBytes;
                case HashAlgorithmNames.SHA512:
                    return SHA512.HashSizeInBytes;
                case HashAlgorithmNames.SHA384:
                    return SHA384.HashSizeInBytes;
                case HashAlgorithmNames.SHA3_256:
                    if (!HashProviderDispenser.HashSupported(HashAlgorithmNames.SHA3_256))
                    {
                        throw new PlatformNotSupportedException();
                    }

                    return SHA3_256.HashSizeInBytes;
                case HashAlgorithmNames.SHA3_384:
                    if (!HashProviderDispenser.HashSupported(HashAlgorithmNames.SHA3_384))
                    {
                        throw new PlatformNotSupportedException();
                    }

                    return SHA3_384.HashSizeInBytes;
                case HashAlgorithmNames.SHA3_512:
                    if (!HashProviderDispenser.HashSupported(HashAlgorithmNames.SHA3_512))
                    {
                        throw new PlatformNotSupportedException();
                    }

                    return SHA3_512.HashSizeInBytes;
                case HashAlgorithmNames.MD5 when Helpers.HasMD5:
                    return MD5.HashSizeInBytes;
                default:
                    throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name));
            }
        }

        private static int MacSize(HashAlgorithmName hashAlgorithm)
        {
            switch (hashAlgorithm.Name)
            {
                case HashAlgorithmNames.SHA256:
                    return HMACSHA256.HashSizeInBytes;
                case HashAlgorithmNames.SHA1:
                    return HMACSHA1.HashSizeInBytes;
                case HashAlgorithmNames.SHA512:
                    return HMACSHA512.HashSizeInBytes;
                case HashAlgorithmNames.SHA384:
                    return HMACSHA384.HashSizeInBytes;
                case HashAlgorithmNames.SHA3_256:
                    if (!HashProviderDispenser.MacSupported(HashAlgorithmNames.SHA3_256))
                    {
                        throw new PlatformNotSupportedException();
                    }

                    return HMACSHA3_256.HashSizeInBytes;
                case HashAlgorithmNames.SHA3_384:
                    if (!HashProviderDispenser.MacSupported(HashAlgorithmNames.SHA3_384))
                    {
                        throw new PlatformNotSupportedException();
                    }

                    return HMACSHA3_384.HashSizeInBytes;
                case HashAlgorithmNames.SHA3_512:
                    if (!HashProviderDispenser.MacSupported(HashAlgorithmNames.SHA3_512))
                    {
                        throw new PlatformNotSupportedException();
                    }

                    return HMACSHA3_512.HashSizeInBytes;
                case HashAlgorithmNames.MD5 when Helpers.HasMD5:
                    return HMACMD5.HashSizeInBytes;
                default:
                    throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name));
            }
        }
    }
}
