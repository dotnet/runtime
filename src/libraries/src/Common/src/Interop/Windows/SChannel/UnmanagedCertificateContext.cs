// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace System.Net
{
    internal static unsafe partial class UnmanagedCertificateContext
    {
        internal static X509Certificate2Collection GetRemoteCertificatesFromStoreContext(SafeFreeCertContext certContext)
        {
            if (certContext.IsInvalid)
            {
                return new X509Certificate2Collection();
            }

            return GetRemoteCertificatesFromStoreContext(certContext.DangerousGetHandle());
        }
    }
}
