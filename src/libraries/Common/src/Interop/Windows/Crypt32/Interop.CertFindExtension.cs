// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static unsafe partial CERT_EXTENSION* CertFindExtension([MarshalAs(UnmanagedType.LPStr)] string pszObjId, int cExtensions, IntPtr rgExtensions);
    }
}
