// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace System.Net
{
    internal static partial class UnmanagedCertificateContext
    {
        internal static void GetRemoteCertificatesFromStoreContext(SafeFreeCertContext certContext, X509Certificate2Collection collection)
        {
            if (certContext.IsInvalid)
            {
                return;
            }

            GetRemoteCertificatesFromStoreContext(certContext.DangerousGetHandle(), collection);
        }
    }
}
