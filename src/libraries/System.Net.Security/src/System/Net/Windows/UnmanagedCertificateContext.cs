// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace System.Net
{
    internal unsafe static class UnmanagedCertificateContext
    {
        internal static X509Certificate2Collection GetRemoteCertificatesFromStoreContext(SafeFreeCertContext certContext)
        {
            X509Certificate2Collection result = new X509Certificate2Collection();

            if (certContext.IsInvalid)
            {
                return result;
            }

            Interop.Crypt32.CERT_CONTEXT context = Marshal.PtrToStructure<Interop.Crypt32.CERT_CONTEXT>(certContext.DangerousGetHandle());

            if (context.hCertStore != IntPtr.Zero)
            {
                X509Store store = null;
                try
                {
                    store = X509StoreExtensions.CreateFromNativeHandle(context.hCertStore);
                    result = store.Certificates;
                }
                finally
                {
                    if (store != null)
                    {
                        store.Dispose();
                    }
                }
            }
            return result;
        }
    }
}