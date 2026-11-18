// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [LibraryImport(Libraries.CryptoNative)]
        private static partial SafeEvpPKeyHandle CryptoNative_CreateEvpPkeyFromEcKey(IntPtr ecKey, out int keySize);

        /// <summary>
        /// Creates a new EVP_PKEY from a raw EC_KEY pointer (IntPtr).
        /// The EC_KEY is duplicated (up-ref'd) so the caller retains ownership.
        /// Also returns the EC key size. Returns NULL (invalid handle) on failure.
        /// </summary>
        internal static SafeEvpPKeyHandle CreateEvpPkeyFromEcKey(IntPtr ecKeyHandle, out int keySize)
        {
            SafeEvpPKeyHandle pkey = CryptoNative_CreateEvpPkeyFromEcKey(ecKeyHandle, out keySize);

            if (pkey.IsInvalid)
            {
                pkey.Dispose();
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }

            return pkey;
        }
    }
}
