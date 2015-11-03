// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
