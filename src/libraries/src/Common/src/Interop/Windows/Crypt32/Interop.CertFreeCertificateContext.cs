// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        // Note: This api always return TRUE, regardless of success.
        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool CertFreeCertificateContext(IntPtr pCertContext);
    }
}
