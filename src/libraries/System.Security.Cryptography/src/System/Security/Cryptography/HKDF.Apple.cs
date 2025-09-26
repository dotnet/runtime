// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    public static partial class HKDF
    {
        private static readonly bool s_hasCryptoKitImplementation =
            OperatingSystem.IsMacOS() ||
            OperatingSystem.IsMacCatalyst() ||
            OperatingSystem.IsIOSVersionAtLeast(14) ||
            OperatingSystem.IsTvOSVersionAtLeast(14);

        private static void Extract(
            HashAlgorithmName hashAlgorithmName,
            int hashLength,
            ReadOnlySpan<byte> ikm,
            ReadOnlySpan<byte> salt,
            Span<byte> prk)
        {
            ThrowForUnsupportedHashAlgorithm(hashAlgorithmName);

            if (s_hasCryptoKitImplementation)
            {
                Interop.AppleCrypto.HKDFExtract(hashAlgorithmName, ikm, salt, prk);
            }
            else
            {
                HKDFManagedImplementation.Extract(hashAlgorithmName, hashLength, ikm, salt, prk);
            }
        }

        private static void Expand(
            HashAlgorithmName hashAlgorithmName,
            int hashLength,
            ReadOnlySpan<byte> prk,
            Span<byte> output,
            ReadOnlySpan<byte> info)
        {
            ThrowForUnsupportedHashAlgorithm(hashAlgorithmName);

            if (s_hasCryptoKitImplementation)
            {
                Interop.AppleCrypto.HkdfExpand(hashAlgorithmName, prk, info, output);
            }
            else
            {
                HKDFManagedImplementation.Expand(hashAlgorithmName, hashLength, prk, output, info);
            }
        }

        private static void DeriveKeyCore(
            HashAlgorithmName hashAlgorithmName,
            int hashLength,
            ReadOnlySpan<byte> ikm,
            Span<byte> output,
            ReadOnlySpan<byte> salt,
            ReadOnlySpan<byte> info)
        {
            ThrowForUnsupportedHashAlgorithm(hashAlgorithmName);

            if (s_hasCryptoKitImplementation)
            {
                Interop.AppleCrypto.HKDFDeriveKey(hashAlgorithmName, ikm, salt, info, output);
            }
            else
            {
                HKDFManagedImplementation.DeriveKey(hashAlgorithmName, hashLength, ikm, output, salt, info);
            }
        }

        private static void ThrowForUnsupportedHashAlgorithm(HashAlgorithmName hashAlgorithmName)
        {
            if (hashAlgorithmName == HashAlgorithmName.SHA3_256 || hashAlgorithmName == HashAlgorithmName.SHA3_384 ||
                hashAlgorithmName == HashAlgorithmName.SHA3_512)
            {
                throw new PlatformNotSupportedException();
            }

            // Unknown algorithms are handled outside of this as a CryptographicException. SHA-3 is known, it's just
            // not supported.
        }
    }
}
