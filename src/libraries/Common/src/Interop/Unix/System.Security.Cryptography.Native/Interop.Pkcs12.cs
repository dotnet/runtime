// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_DecodePkcs12")]
        internal static extern unsafe SafePkcs12Handle DecodePkcs12(byte[] buf, int len);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_DecodePkcs12FromBio")]
        internal static extern SafePkcs12Handle DecodePkcs12FromBio(SafeBioHandle bio);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_Pkcs12Destroy")]
        internal static extern void Pkcs12Destroy(IntPtr p12);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_Pkcs12Create", CharSet = CharSet.Ansi)]
        internal static extern SafePkcs12Handle Pkcs12Create(
            SafePasswordHandle pass,
            SafeEvpPKeyHandle pkey,
            SafeX509Handle cert,
            SafeX509StackHandle ca);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetPkcs12DerSize")]
        internal static extern int GetPkcs12DerSize(SafePkcs12Handle p12);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EncodePkcs12")]
        internal static extern int EncodePkcs12(SafePkcs12Handle p12, byte[] buf);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_Pkcs12Parse")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool Pkcs12Parse(
            SafePkcs12Handle p12,
            SafePasswordHandle pass,
            out SafeEvpPKeyHandle pkey,
            out SafeX509Handle cert,
            out SafeX509StackHandle ca);
    }
}
