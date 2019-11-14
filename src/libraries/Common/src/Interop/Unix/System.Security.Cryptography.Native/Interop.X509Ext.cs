// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509ExtensionCreateByObj")]
        internal static extern SafeX509ExtensionHandle X509ExtensionCreateByObj(
            SafeAsn1ObjectHandle oid,
            [MarshalAs(UnmanagedType.Bool)] bool isCritical,
            SafeAsn1OctetStringHandle data);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509ExtensionDestroy")]
        internal static extern int X509ExtensionDestroy(IntPtr x);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509V3ExtPrint")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool X509V3ExtPrint(SafeBioHandle buf, SafeX509ExtensionHandle ext);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_DecodeX509BasicConstraints2Extension")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DecodeX509BasicConstraints2Extension(
            byte[] encoded,
            int encodedLength,
            [MarshalAs(UnmanagedType.Bool)] out bool certificateAuthority,
            [MarshalAs(UnmanagedType.Bool)] out bool hasPathLengthConstraint,
            out int pathLengthConstraint);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_DecodeExtendedKeyUsage")]
        internal static extern SafeEkuExtensionHandle DecodeExtendedKeyUsage(byte[] buf, int len);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_ExtendedKeyUsageDestory")]
        internal static extern void ExtendedKeyUsageDestory(IntPtr a);
    }
}
