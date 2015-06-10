// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using Microsoft.Win32.SafeHandles;

using NativeULong=System.UIntPtr;

internal static partial class Interop
{
    internal static partial class libcrypto
    {
        [DllImport(Libraries.LibCrypto)]
        internal static extern void X509_free(IntPtr a);

        [DllImport(Libraries.LibCrypto)]
        internal static unsafe extern SafeX509Handle d2i_X509(IntPtr zero, byte** ppin, int len);

        [DllImport(Libraries.LibCrypto)]
        internal static extern unsafe int i2d_X509(SafeX509Handle x, byte** @out);

        [DllImport(Libraries.LibCrypto)]
        internal static extern IntPtr X509_get_serialNumber(SafeX509Handle x);

        [DllImport(Libraries.LibCrypto)]
        internal static unsafe extern SafeX509NameHandle d2i_X509_NAME(IntPtr zero, byte** ppin, int len);

        [DllImport(Libraries.LibCrypto)]
        internal static extern int X509_NAME_print_ex(SafeBioHandle @out, SafeX509NameHandle nm, int indent, NativeULong flags);

        [DllImport(Libraries.LibCrypto)]
        internal static extern void X509_NAME_free(IntPtr a);

        [DllImport(Libraries.LibCrypto)]
        internal static extern IntPtr X509_get_issuer_name(SafeX509Handle a);

        [DllImport(Libraries.LibCrypto)]
        internal static extern IntPtr X509_get_subject_name(SafeX509Handle a);

        [DllImport(Libraries.LibCrypto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool X509_check_purpose(SafeX509Handle x, int id, int ca);

        [DllImport(Libraries.LibCrypto)]
        internal static extern int X509_get_ext_count(SafeX509Handle x);

        // Returns a pointer already being tracked by the SafeX509Handle, shouldn't be SafeHandle tracked/freed.
        // Bounds checking is in place for "loc", IntPtr.Zero is returned on violations.
        [DllImport(Libraries.LibCrypto)]
        internal static extern IntPtr X509_get_ext(SafeX509Handle x, int loc);

        // Returns a pointer already being tracked by a SafeX509Handle, shouldn't be SafeHandle tracked/freed.
        [DllImport(Libraries.LibCrypto)]
        internal static extern IntPtr X509_EXTENSION_get_object(IntPtr ex);

        // Returns a pointer already being tracked by a SafeX509Handle, shouldn't be SafeHandle tracked/freed.
        [DllImport(Libraries.LibCrypto)]
        internal static extern IntPtr X509_EXTENSION_get_data(IntPtr ex);

        [DllImport(Libraries.LibCrypto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool X509_EXTENSION_get_critical(IntPtr ex);
    }
}
