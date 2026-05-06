// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPkeyGetEcKey")]
        internal static partial SafeEcKeyHandle EvpPkeyGetEcKey(SafeEvpPKeyHandle pkey);

        [LibraryImport(Libraries.CryptoNative)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool CryptoNative_EvpPkeySetEcKey(SafeEvpPKeyHandle pkey, SafeEcKeyHandle key);

        // Calls EVP_PKEY_set1_EC_KEY therefore the key will be duplicated
        internal static SafeEvpPKeyHandle CreateEvpPkeyFromEcKey(SafeEcKeyHandle key)
        {
            SafeEvpPKeyHandle pkey = Interop.Crypto.EvpPkeyCreate();
            if (!CryptoNative_EvpPkeySetEcKey(pkey, key))
            {
                pkey.Dispose();
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }

            return pkey;
        }
    }
}
