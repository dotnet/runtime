// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static unsafe partial bool CertSetCertificateContextProperty(SafeCertContextHandle pCertContext, CertContextPropId dwPropId, CertSetPropertyFlags dwFlags, SafeNCryptKeyHandle keyHandle);
    }
}
