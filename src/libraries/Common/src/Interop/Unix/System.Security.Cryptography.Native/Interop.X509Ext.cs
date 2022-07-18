// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509ExtensionCreateByObj")]
        internal static partial SafeX509ExtensionHandle X509ExtensionCreateByObj(
            SafeAsn1ObjectHandle oid,
            [MarshalAs(UnmanagedType.Bool)] bool isCritical,
            SafeAsn1OctetStringHandle data);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509ExtensionDestroy")]
        internal static partial int X509ExtensionDestroy(IntPtr x);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509V3ExtPrint")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool X509V3ExtPrint(SafeBioHandle buf, SafeX509ExtensionHandle ext);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_DecodeX509BasicConstraints2Extension")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool DecodeX509BasicConstraints2Extension(
            byte[] encoded,
            int encodedLength,
            [MarshalAs(UnmanagedType.Bool)] out bool certificateAuthority,
            [MarshalAs(UnmanagedType.Bool)] out bool hasPathLengthConstraint,
            out int pathLengthConstraint);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_DecodeExtendedKeyUsage")]
        internal static partial SafeEkuExtensionHandle DecodeExtendedKeyUsage(byte[] buf, int len);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_ExtendedKeyUsageDestroy")]
        internal static partial void ExtendedKeyUsageDestroy(IntPtr a);
    }
}
