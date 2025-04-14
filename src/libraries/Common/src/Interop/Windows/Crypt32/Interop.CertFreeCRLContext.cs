// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        // https://learn.microsoft.com/windows/win32/api/wincrypt/nf-wincrypt-certfreecrlcontext
        [LibraryImport(Libraries.Crypt32)]
        public static partial int CertFreeCRLContext(IntPtr certContext);
    }
}
