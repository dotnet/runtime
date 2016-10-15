// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

using Internal.Cryptography.Pal.Native;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class CertificatePal : IDisposable, ICertificatePal
    {
        public bool HasPrivateKey
        {
            get
            {
                return _certContext.ContainsPrivateKey;
            }
        }

        public RSA GetRSAPrivateKey()
        {
            return GetPrivateKey<RSA>(
                delegate (CspParameters csp)
                {
#if NETNATIVE
                    // In .NET Native (UWP) we don't have access to CAPI, so it's CNG-or-nothing.
                    // But we don't expect to get here, so it shouldn't be a problem.
    
                    Debug.Fail("A CAPI provider type code was specified");
                    return null;
#else
                    return new RSACryptoServiceProvider(csp);
#endif
                },
                delegate (CngKey cngKey)
                {
                    return new RSACng(cngKey);
                }
            );
        }

        public ECDsa GetECDsaPrivateKey()
        {
            return GetPrivateKey<ECDsa>(
                delegate (CspParameters csp)
                {
                    throw new NotSupportedException(SR.NotSupported_ECDsa_Csp);
                },
                delegate (CngKey cngKey)
                {
                    return new ECDsaCng(cngKey);
                }
            );
        }

        private T GetPrivateKey<T>(Func<CspParameters, T> createCsp, Func<CngKey, T> createCng) where T : AsymmetricAlgorithm
        {
            CngKeyHandleOpenOptions cngHandleOptions;
            SafeNCryptKeyHandle ncryptKey = TryAcquireCngPrivateKey(CertContext, out cngHandleOptions);
            if (ncryptKey != null)
            {
                CngKey cngKey = CngKey.Open(ncryptKey, cngHandleOptions);
                return createCng(cngKey);
            }
 
            CspParameters cspParameters = GetPrivateKey();
            if (cspParameters == null)
                return null;

            if (cspParameters.ProviderType == 0)
            {
                // ProviderType being 0 signifies that this is actually a CNG key, not a CAPI key. Crypt32.dll stuffs the CNG Key Storage Provider
                // name into CRYPT_KEY_PROV_INFO->ProviderName, and the CNG key name into CRYPT_KEY_PROV_INFO->KeyContainerName.

                string keyStorageProvider = cspParameters.ProviderName;
                string keyName = cspParameters.KeyContainerName;
                CngKey cngKey = CngKey.Open(keyName, new CngProvider(keyStorageProvider));
                return createCng(cngKey);
            }
            else
            {
                // ProviderType being non-zero signifies that this is a CAPI key.
#if NETNATIVE
                // In .NET Native (UWP) we don't have access to CAPI, so it's CNG-or-nothing.
                // But we don't expect to get here, so it shouldn't be a problem.
    
                Debug.Fail("A CAPI provider type code was specified");
                return null;
#else
                // We never want to stomp over certificate private keys.
                cspParameters.Flags |= CspProviderFlags.UseExistingKey;
                return createCsp(cspParameters);
#endif
            }
        }

        private static SafeNCryptKeyHandle TryAcquireCngPrivateKey(
            SafeCertContextHandle certificateContext,
            out CngKeyHandleOpenOptions handleOptions)
        {
            Debug.Assert(certificateContext != null, "certificateContext != null");
            Debug.Assert(!certificateContext.IsClosed && !certificateContext.IsInvalid,
                         "!certificateContext.IsClosed && !certificateContext.IsInvalid");

            IntPtr privateKeyPtr;

            // If the certificate has a key handle instead of a key prov info, return the
            // ephemeral key
            {
                int cbData = IntPtr.Size;

                if (Interop.crypt32.CertGetCertificateContextProperty(
                    certificateContext,
                    CertContextPropId.CERT_NCRYPT_KEY_HANDLE_PROP_ID,
                    out privateKeyPtr,
                    ref cbData))
                {
                    handleOptions = CngKeyHandleOpenOptions.EphemeralKey;
                    return new SafeNCryptKeyHandle(privateKeyPtr, certificateContext);
                }
            }

            bool freeKey = true;
            SafeNCryptKeyHandle privateKey = null;
            handleOptions = CngKeyHandleOpenOptions.None;
            try
            {
                int keySpec = 0;
                if (!Interop.crypt32.CryptAcquireCertificatePrivateKey(
                    certificateContext,
                    CryptAcquireFlags.CRYPT_ACQUIRE_ONLY_NCRYPT_KEY_FLAG,
                    IntPtr.Zero,
                    out privateKey,
                    out keySpec,
                    out freeKey))
                {
                    int dwErrorCode = Marshal.GetLastWin32Error();

                    // The documentation for CryptAcquireCertificatePrivateKey says that freeKey
                    // should already be false if "key acquisition fails", and it can be presumed
                    // that privateKey was set to 0.  But, just in case:
                    freeKey = false;
                    privateKey?.SetHandleAsInvalid();
                    return null;
                }

                // It is very unlikely that Windows will tell us !freeKey other than when reporting failure,
                // because we set neither CRYPT_ACQUIRE_CACHE_FLAG nor CRYPT_ACQUIRE_USE_PROV_INFO_FLAG, which are
                // currently the only two success situations documented. However, any !freeKey response means the
                // key's lifetime is tied to that of the certificate, so re-register the handle as a child handle
                // of the certificate.
                if (!freeKey && privateKey != null && !privateKey.IsInvalid)
                {
                    var newKeyHandle = new SafeNCryptKeyHandle(privateKey.DangerousGetHandle(), certificateContext);
                    privateKey.SetHandleAsInvalid();
                    privateKey = newKeyHandle;
                    freeKey = true;
                }

                return privateKey;
            }
            catch
            {
                // If we aren't supposed to free the key, and we're not returning it,
                // just tell the SafeHandle to not free itself.
                if (privateKey != null && !freeKey)
                {
                    privateKey.SetHandleAsInvalid();
                }

                throw;
            }
        }

        //
        // Returns the private key referenced by a store certificate. Note that despite the return type being declared "CspParameters",
        // the key can actually be a CNG key. To distinguish, examine the ProviderType property. If it is 0, this key is a CNG key with
        // the various properties of CspParameters being "repurposed" into storing CNG info. 
        // 
        // This is a behavior this method inherits directly from the Crypt32 CRYPT_KEY_PROV_INFO semantics.
        //
        // It would have been nice not to let this ugliness escape out of this helper method. But X509Certificate2.ToString() calls this 
        // method too so we cannot just change it without breaking its output.
        // 
        private CspParameters GetPrivateKey()
        {
            int cbData = 0;
            if (!Interop.crypt32.CertGetCertificateContextProperty(_certContext, CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID, null, ref cbData))
            {
                int dwErrorCode = Marshal.GetLastWin32Error();
                if (dwErrorCode == ErrorCode.CRYPT_E_NOT_FOUND)
                    return null;
                throw dwErrorCode.ToCryptographicException();
            }

            unsafe
            {
                byte[] privateKey = new byte[cbData];
                fixed (byte* pPrivateKey = privateKey)
                {
                    if (!Interop.crypt32.CertGetCertificateContextProperty(_certContext, CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID, privateKey, ref cbData))
                        throw Marshal.GetLastWin32Error().ToCryptographicException();
                    CRYPT_KEY_PROV_INFO* pKeyProvInfo = (CRYPT_KEY_PROV_INFO*)pPrivateKey;

                    CspParameters cspParameters = new CspParameters();
                    cspParameters.ProviderName = Marshal.PtrToStringUni((IntPtr)(pKeyProvInfo->pwszProvName));
                    cspParameters.KeyContainerName = Marshal.PtrToStringUni((IntPtr)(pKeyProvInfo->pwszContainerName));
                    cspParameters.ProviderType = pKeyProvInfo->dwProvType;
                    cspParameters.KeyNumber = pKeyProvInfo->dwKeySpec;
                    cspParameters.Flags = (CspProviderFlags)((pKeyProvInfo->dwFlags & CryptAcquireContextFlags.CRYPT_MACHINE_KEYSET) == CryptAcquireContextFlags.CRYPT_MACHINE_KEYSET ? CspProviderFlags.UseMachineKeyStore : 0);
                    return cspParameters;
                }
            }
        }
    }
}
