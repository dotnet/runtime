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
        internal static extern SafeX509ExtensionHandle X509_EXTENSION_create_by_OBJ(
            IntPtr zero,
            SafeAsn1ObjectHandle oid,
            [MarshalAs(UnmanagedType.Bool)] bool critical,
            SafeAsn1OctetStringHandle data);

        [DllImport(Libraries.LibCrypto)]
        internal static extern int X509_EXTENSION_free(IntPtr x);

        [DllImport(Libraries.LibCrypto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool X509V3_EXT_print(SafeBioHandle buf, SafeX509ExtensionHandle ext, X509V3ExtPrintFlags flags, int indent);

        [DllImport(Libraries.LibCrypto)]
        internal static unsafe extern SafeBasicConstraintsHandle d2i_BASIC_CONSTRAINTS(IntPtr zero, byte** ppin, int len);

        [DllImport(Libraries.LibCrypto)]
        internal static extern void BASIC_CONSTRAINTS_free(IntPtr a);

        [DllImport(Libraries.LibCrypto)]
        internal static unsafe extern SafeEkuExtensionHandle d2i_EXTENDED_KEY_USAGE(IntPtr zero, byte** ppin, int len);

        [DllImport(Libraries.LibCrypto)]
        internal static extern void EXTENDED_KEY_USAGE_free(IntPtr a);

        [StructLayout(LayoutKind.Sequential)]
        internal struct BASIC_CONSTRAINTS
        {
            // This is a boolean, but [MarshalAs(UnmanagedType.Bool)] produced a "true != true" answer.
            internal int CA;
            internal IntPtr pathlen;
        }

        // As of 2015-04-11 there is no documentation on openssl.org or within the header files
        // which describes possible values of "flags" to X509V3_EXT_print.
        //
        // The flag which would be the most likely candidate is X509V3_EXT_MULTILINE, but that is a
        // flag on the extension definition (and a test call providing that value had no discernable
        // effect on OpenSSL 1.0.1f).
        [Flags]
        internal enum X509V3ExtPrintFlags
        {
            None = 0,
        }
    }
}
