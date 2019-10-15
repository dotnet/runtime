// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Crypt32
    {
        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern unsafe CERT_CONTEXT* CertEnumCertificatesInStore(IntPtr hCertStore, CERT_CONTEXT* pPrevCertContext);
    }
}
