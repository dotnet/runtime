// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    internal static partial class HashProviderDispenser
    {
        public static HashProvider CreateHashProvider(string hashAlgorithmId)
        {
            switch (hashAlgorithmId)
            {
                case HashAlgorithmNames.SHA1:
                case HashAlgorithmNames.SHA256:
                case HashAlgorithmNames.SHA384:
                case HashAlgorithmNames.SHA512:
                    return Interop.BrowserCrypto.CanUseSubtleCrypto
                        ? new SHANativeHashProvider(hashAlgorithmId)
                        : new SHAManagedHashProvider(hashAlgorithmId);
            }
            throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmId));
        }

        public static class OneShotHashProvider
        {
            public static unsafe int MacData(
                string hashAlgorithmId,
                ReadOnlySpan<byte> key,
                ReadOnlySpan<byte> source,
                Span<byte> destination)
            {
                if (Interop.BrowserCrypto.CanUseSubtleCrypto)
                {
                    return HMACNativeHashProvider.MacDataOneShot(hashAlgorithmId, key, source, destination);
                }
                else
                {
                    using HashProvider provider = CreateMacProvider(hashAlgorithmId, key);
                    provider.AppendHashData(source);
                    return provider.FinalizeHashAndReset(destination);
                }
            }

            public static int HashData(string hashAlgorithmId, ReadOnlySpan<byte> source, Span<byte> destination)
            {
                if (Interop.BrowserCrypto.CanUseSubtleCrypto)
                {
                    return SHANativeHashProvider.HashOneShot(hashAlgorithmId, source, destination);
                }
                else
                {
                    HashProvider provider = CreateHashProvider(hashAlgorithmId);
                    provider.AppendHashData(source);
                    return provider.FinalizeHashAndReset(destination);
                }
            }
        }

        public static unsafe HashProvider CreateMacProvider(string hashAlgorithmId, ReadOnlySpan<byte> key)
        {
            switch (hashAlgorithmId)
            {
                case HashAlgorithmNames.SHA1:
                case HashAlgorithmNames.SHA256:
                case HashAlgorithmNames.SHA384:
                case HashAlgorithmNames.SHA512:
                    return Interop.BrowserCrypto.CanUseSubtleCrypto
                        ? new HMACNativeHashProvider(hashAlgorithmId, key)
                        : new HMACManagedHashProvider(hashAlgorithmId, key);
            }
            throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmId));
        }
    }
}
