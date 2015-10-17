// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative)]
        internal static extern SafeRsaHandle EvpPkeyGetRsa(SafeEvpPKeyHandle pkey);

        [DllImport(Libraries.CryptoNative)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvpPkeySetRsa(SafeEvpPKeyHandle pkey, SafeRsaHandle rsa);
    }
}
