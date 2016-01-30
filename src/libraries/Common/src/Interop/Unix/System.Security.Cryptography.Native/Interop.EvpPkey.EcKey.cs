// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPkeyGetEcKey")]
        internal static extern SafeEcKeyHandle EvpPkeyGetEcKey(SafeEvpPKeyHandle pkey);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPkeySetEcKey")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvpPkeySetEcKey(SafeEvpPKeyHandle pkey, SafeEcKeyHandle key);
    }
}
