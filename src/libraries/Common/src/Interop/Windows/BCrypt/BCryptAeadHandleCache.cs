// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;
using System.Threading;
using Internal.NativeCrypto;

namespace Internal.Cryptography
{
    internal static class BCryptAeadHandleCache
    {
        private static SafeAlgorithmHandle? s_aesCcm;
        private static SafeAlgorithmHandle? s_aesGcm;
        private static SafeAlgorithmHandle? s_chaCha20Poly1305;

        internal static SafeAlgorithmHandle AesCcm => GetCachedAlgorithmHandle(ref s_aesCcm, Cng.BCRYPT_AES_ALGORITHM, Cng.BCRYPT_CHAIN_MODE_CCM);
        internal static SafeAlgorithmHandle AesGcm => GetCachedAlgorithmHandle(ref s_aesGcm, Cng.BCRYPT_AES_ALGORITHM, Cng.BCRYPT_CHAIN_MODE_GCM);

        internal static bool IsChaCha20Poly1305Supported { get; } = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 20142);
        internal static SafeAlgorithmHandle ChaCha20Poly1305 => GetCachedAlgorithmHandle(ref s_chaCha20Poly1305, Cng.BCRYPT_CHACHA20_POLY1305_ALGORITHM);

        private static SafeAlgorithmHandle GetCachedAlgorithmHandle(ref SafeAlgorithmHandle? handle, string algId, string? chainingMode = null)
        {
            // Do we already have a handle to this algorithm?
            SafeAlgorithmHandle? existingHandle = Volatile.Read(ref handle);
            if (existingHandle != null)
            {
                return existingHandle;
            }

            // No cached handle exists; create a new handle. It's ok if multiple threads call
            // this concurrently. Only one handle will "win" and the rest will be destroyed.
            SafeAlgorithmHandle newHandle = Cng.BCryptOpenAlgorithmProvider(algId, null, Cng.OpenAlgorithmProviderFlags.NONE);
            if (chainingMode != null)
            {
                newHandle.SetCipherMode(chainingMode);
            }

            existingHandle = Interlocked.CompareExchange(ref handle, newHandle, null);
            if (existingHandle != null)
            {
                newHandle.Dispose();
                return existingHandle;
            }
            else
            {
                return newHandle;
            }
        }
    }
}
