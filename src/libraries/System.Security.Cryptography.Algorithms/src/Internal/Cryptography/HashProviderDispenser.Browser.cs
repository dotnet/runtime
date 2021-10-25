// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography
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
                    return new SHAHashProvider(hashAlgorithmId);
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
                throw new PlatformNotSupportedException(SR.SystemSecurityCryptographyAlgorithms_PlatformNotSupported);
            }

            public static int HashData(string hashAlgorithmId, ReadOnlySpan<byte> source, Span<byte> destination)
            {
                HashProvider provider = HashProviderDispenser.CreateHashProvider(hashAlgorithmId);
                provider.AppendHashData(source);
                return provider.FinalizeHashAndReset(destination);
            }
        }

        public static unsafe HashProvider CreateMacProvider(string hashAlgorithmId, ReadOnlySpan<byte> key)
        {
            throw new PlatformNotSupportedException(SR.SystemSecurityCryptographyAlgorithms_PlatformNotSupported);
        }
    }
}
