// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [LibraryImport(Libraries.Crypt32, EntryPoint = "CertGetNameStringW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static unsafe partial int CertGetNameString(SafeCertContextHandle pCertContext, CertNameType dwType, CertNameFlags dwFlags, in CertNameStringType pvTypePara, char* pszNameString, int cchNameString);
    }
}
