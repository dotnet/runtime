// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal static partial class HashProviderDispenser
    {
        internal static bool HashSupported(string hashAlgorithmId)
        {
            switch (hashAlgorithmId)
            {
                case HashAlgorithmNames.SHA1:
                case HashAlgorithmNames.SHA256:
                case HashAlgorithmNames.SHA384:
                case HashAlgorithmNames.SHA512:
                    return true;
                default:
                    return false;
            }
        }

        internal static bool MacSupported(string hashAlgorithmId) => HashSupported(hashAlgorithmId);

        internal static bool KmacSupported(string algorithmId)
        {
            _ = algorithmId;
            return false;
        }

        public static HashProvider CreateHashProvider(string hashAlgorithmId)
        {
            switch (hashAlgorithmId)
            {
                case HashAlgorithmNames.SHA1:
                case HashAlgorithmNames.SHA256:
                case HashAlgorithmNames.SHA384:
                case HashAlgorithmNames.SHA512:
                    return new SHAManagedHashProvider(hashAlgorithmId);
            }
            throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmId));
        }

#pragma warning disable IDE0060

        public static class OneShotHashProvider
        {
            public static void KmacData(
                string algorithmId,
                ReadOnlySpan<byte> key,
                ReadOnlySpan<byte> source,
                Span<byte> destination,
                ReadOnlySpan<byte> customizationString,
                bool xof)
            {
                _ = algorithmId;
                _ = key;
                _ = customizationString;
                _ = source;
                _ = destination;
                _ = xof;
                Debug.Fail("Platform should have checked if KMAC was available first.");
                throw new UnreachableException();
            }

            public static unsafe int MacData(
                string hashAlgorithmId,
                ReadOnlySpan<byte> key,
                ReadOnlySpan<byte> source,
                Span<byte> destination)
            {
                using HashProvider provider = CreateMacProvider(hashAlgorithmId, key);
                provider.AppendHashData(source);
                return provider.FinalizeHashAndReset(destination);
            }

            public static int HashData(string hashAlgorithmId, ReadOnlySpan<byte> source, Span<byte> destination)
            {
                HashProvider provider = CreateHashProvider(hashAlgorithmId);
                provider.AppendHashData(source);
                return provider.FinalizeHashAndReset(destination);
            }

            public static void HashDataXof(string hashAlgorithmId, ReadOnlySpan<byte> source, Span<byte> destination)
            {
                _ = hashAlgorithmId;
                _ = source;
                _ = destination;
                Debug.Fail("Caller should have checked if platform supported XOFs.");
                throw new UnreachableException();
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
                    return new HMACManagedHashProvider(hashAlgorithmId, key);
            }
            throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmId));
        }

#pragma warning restore IDE0060

    }
}
