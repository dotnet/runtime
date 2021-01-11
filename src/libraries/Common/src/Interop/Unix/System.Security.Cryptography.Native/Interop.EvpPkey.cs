// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPkeyCreate")]
        internal static extern SafeEvpPKeyHandle EvpPkeyCreate();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPkeyDestroy")]
        internal static extern void EvpPkeyDestroy(IntPtr pkey);

        /// <summary>
        /// Gets the size, in bits, of the asymmetric key object.
        /// </summary>
        internal static int EvpPKeyKeySize(SafeEvpPKeyHandle pkey)
        {
            int size = CryptoNative_EvpPKeyKeySize(pkey);

            // A null key value was passed in, or something went really wrong.
            // There's no guarantee an error code was set, so throw vague.
            // This should be unreachable, but stops nonsense behaviors if it isn't.
            if (size <= 0)
            {
                throw new CryptographicException();
            }

            return size;
        }

        [DllImport(Libraries.CryptoNative)]
        private static extern int CryptoNative_EvpPKeyKeySize(SafeEvpPKeyHandle pkey);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_UpRefEvpPkey")]
        internal static extern int UpRefEvpPkey(SafeEvpPKeyHandle handle);

        internal static SafeEvpPKeyHandle EvpPkeyDuplicate(SafeEvpPKeyHandle pkey)
        {
            int ret = CryptoNative_EvpPkeyDuplicate(pkey, out SafeEvpPKeyHandle pkeyNew);

            if (ret != 1)
            {
                Debug.Assert(ret == 0);
                pkeyNew.Dispose();
                throw CreateOpenSslCryptographicException();
            }

            return pkeyNew;
        }

        [DllImport(Libraries.CryptoNative)]
        private static extern int CryptoNative_EvpPkeyDuplicate(
            SafeEvpPKeyHandle pkeyIn,
            out SafeEvpPKeyHandle pkeyOut);
    }
}
