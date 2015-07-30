// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class libcrypto
    {
        [DllImport(Libraries.LibCrypto)]
        internal static unsafe extern SafePkcs12Handle d2i_PKCS12(IntPtr zero, byte** ppin, int len);

        [DllImport(Libraries.LibCrypto)]
        internal static extern SafePkcs12Handle d2i_PKCS12_bio(SafeBioHandle bio, IntPtr zero);

        [DllImport(Libraries.LibCrypto)]
        internal static extern void PKCS12_free(IntPtr p12);
        
        [DllImport(Libraries.CryptoInterop, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool PKCS12_parse(SafePkcs12Handle p12, string pass, out SafeEvpPkeyHandle pkey, out SafeX509Handle cert, out SafeX509StackHandle ca);
    }
}
