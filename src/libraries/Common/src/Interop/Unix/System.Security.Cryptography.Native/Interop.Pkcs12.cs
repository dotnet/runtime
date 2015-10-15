// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative)]
        internal static unsafe extern SafePkcs12Handle DecodePkcs12(byte[] buf, int len);

        [DllImport(Libraries.CryptoNative)]
        internal static extern SafePkcs12Handle DecodePkcs12FromBio(SafeBioHandle bio);

        [DllImport(Libraries.CryptoNative)]
        internal static extern void Pkcs12Destroy(IntPtr p12);

        [DllImport(Libraries.CryptoNative, CharSet = CharSet.Ansi)]
        internal static extern SafePkcs12Handle Pkcs12Create(
            string pass,
            SafeEvpPKeyHandle pkey,
            SafeX509Handle cert,
            SafeX509StackHandle ca);

        [DllImport(Libraries.CryptoNative)]
        internal static extern int GetPkcs12DerSize(SafePkcs12Handle p12);

        [DllImport(Libraries.CryptoNative)]
        internal static extern int EncodePkcs12(SafePkcs12Handle p12, byte[] buf);

        [DllImport(Libraries.CryptoNative, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool Pkcs12Parse(
            SafePkcs12Handle p12,
            string pass,
            out SafeEvpPKeyHandle pkey,
            out SafeX509Handle cert,
            out SafeX509StackHandle ca);
    }
}
