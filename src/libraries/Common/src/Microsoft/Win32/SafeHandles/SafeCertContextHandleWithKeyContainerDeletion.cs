// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Microsoft.Win32.SafeHandles
{
    /// <summary>
    /// SafeHandle for the CERT_CONTEXT structure defined by crypt32. Unlike SafeCertContextHandle, disposition already deletes any associated key containers.
    /// </summary>
    internal sealed class SafeCertContextHandleWithKeyContainerDeletion : SafeCertContextHandle
    {
        protected sealed override bool ReleaseHandle()
        {
            using (SafeCertContextHandle certContext = Interop.Crypt32.CertDuplicateCertificateContext(handle))
            {
                DeleteKeyContainer(certContext);
            }
            base.ReleaseHandle();
            return true;
        }

        internal static void DeleteKeyContainer(SafeCertContextHandle pCertContext)
        {
            if (pCertContext.IsInvalid)
                return;

            int cb = 0;
            bool containsPrivateKey = Interop.Crypt32.CertGetCertificateContextProperty(pCertContext, Interop.Crypt32.CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID, null, ref cb);
            if (!containsPrivateKey)
                return;

            byte[] provInfoAsBytes = new byte[cb];
            if (!Interop.Crypt32.CertGetCertificateContextProperty(pCertContext, Interop.Crypt32.CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID, provInfoAsBytes, ref cb))
                return;

            unsafe
            {
                fixed (byte* pProvInfoAsBytes = provInfoAsBytes)
                {
                    Interop.Crypt32.CRYPT_KEY_PROV_INFO* pProvInfo = (Interop.Crypt32.CRYPT_KEY_PROV_INFO*)pProvInfoAsBytes;

                    if (pProvInfo->dwProvType == 0)
                    {
                        // dwProvType being 0 indicates that the key is stored in CNG.
                        // dwProvType being non-zero indicates that the key is stored in CAPI.

                        string providerName = Marshal.PtrToStringUni((IntPtr)(pProvInfo->pwszProvName))!;
                        string keyContainerName = Marshal.PtrToStringUni((IntPtr)(pProvInfo->pwszContainerName))!;

                        try
                        {
                            using (CngKey cngKey = CngKey.Open(keyContainerName, new CngProvider(providerName)))
                            {
                                cngKey.Delete();
                            }
                        }
                        catch (CryptographicException)
                        {
                            // While leaving the file on disk is undesirable, an inability to perform this cleanup
                            // should not manifest itself to a user.
                        }
                    }
                    else
                    {
                        Interop.Crypt32.CryptAcquireContextFlags flags = (pProvInfo->dwFlags & Interop.Crypt32.CryptAcquireContextFlags.CRYPT_MACHINE_KEYSET) | Interop.Crypt32.CryptAcquireContextFlags.CRYPT_DELETEKEYSET;
                        IntPtr hProv;
                        _ = Interop.cryptoapi.CryptAcquireContext(out hProv, pProvInfo->pwszContainerName, pProvInfo->pwszProvName, pProvInfo->dwProvType, flags);

                        // Called CryptAcquireContext solely for the side effect of deleting the key containers. When called with these flags, no actual
                        // hProv is returned (so there's nothing to clean up.)
                        Debug.Assert(hProv == IntPtr.Zero);
                    }
                }
            }
        }
    }
}
