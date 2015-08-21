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
        internal static extern SafePkcs7Handle PEM_read_bio_PKCS7(SafeBioHandle bp, IntPtr xZero, IntPtr cbZero, IntPtr uZero);

        [DllImport(Libraries.LibCrypto)]
        internal static unsafe extern SafePkcs7Handle d2i_PKCS7(IntPtr zero, byte** ppin, int len);

        [DllImport(Libraries.LibCrypto)]
        internal static extern SafePkcs7Handle d2i_PKCS7_bio(SafeBioHandle bp, IntPtr pp7Zero);

        [DllImport(Libraries.LibCrypto)]
        internal static extern void PKCS7_free(IntPtr p12);
    }
}
