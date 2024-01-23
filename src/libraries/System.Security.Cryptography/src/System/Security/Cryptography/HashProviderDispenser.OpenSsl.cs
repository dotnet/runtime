// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    internal static partial class HashProviderDispenser
    {
        internal static bool KmacSupported(string algorithmId)
        {
            return algorithmId switch
            {
                HashAlgorithmNames.KMAC128 => Interop.Crypto.EvpMacAlgs.Kmac128 is not null,
                HashAlgorithmNames.KMAC256 => Interop.Crypto.EvpMacAlgs.Kmac256 is not null,
                _ => false,
            };
        }

        internal static partial class OneShotHashProvider
        {
            public static void KmacData(
                string algorithmId,
                ReadOnlySpan<byte> key,
                ReadOnlySpan<byte> source,
                Span<byte> destination,
                ReadOnlySpan<byte> customizationString,
                bool xof)
            {
                SafeEvpMacHandle? macHandle = algorithmId switch
                {
                    HashAlgorithmNames.KMAC128 => Interop.Crypto.EvpMacAlgs.Kmac128,
                    HashAlgorithmNames.KMAC256 => Interop.Crypto.EvpMacAlgs.Kmac256,
                    _ => throw new CryptographicException(),
                };

                Debug.Assert(macHandle is not null);
                Interop.Crypto.EvpMacOneShot(macHandle, key, customizationString, source, destination, xof);
            }

            public static unsafe void HashDataXof(string hashAlgorithmId, ReadOnlySpan<byte> source, Span<byte> destination)
            {
                IntPtr evpType = Interop.Crypto.HashAlgorithmToEvp(hashAlgorithmId);
                Debug.Assert(evpType != IntPtr.Zero);

                const int Success = 1;
                int ret = Interop.Crypto.EvpDigestXOFOneShot(evpType, source, destination);

                if (ret != Success)
                {
                    Debug.Assert(ret == 0);
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }
            }
        }
    }
}
