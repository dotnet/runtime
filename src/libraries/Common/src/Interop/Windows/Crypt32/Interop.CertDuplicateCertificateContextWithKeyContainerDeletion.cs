// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [LibraryImport(Libraries.Crypt32, EntryPoint = "CertDuplicateCertificateContext", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial SafeCertContextHandleWithKeyContainerDeletion CertDuplicateCertificateContextWithKeyContainerDeletion(IntPtr pCertContext);
    }
}
