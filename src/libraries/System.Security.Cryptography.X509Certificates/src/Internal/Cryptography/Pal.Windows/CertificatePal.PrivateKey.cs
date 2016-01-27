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
            SafeNCryptKeyHandle ncryptKey = TryAcquireCngPrivateKey(CertContext);
            if (ncryptKey != null)
            {
                CngKey cngKey = CngKey.Open(ncryptKey, CngKeyHandleOpenOptions.None);
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

        private static SafeNCryptKeyHandle TryAcquireCngPrivateKey(SafeCertContextHandle certificateContext)
        {
            Debug.Assert(certificateContext != null, "certificateContext != null");
            Debug.Assert(!certificateContext.IsClosed && !certificateContext.IsInvalid,
                         "!certificateContext.IsClosed && !certificateContext.IsInvalid");

            bool freeKey = true;
            SafeNCryptKeyHandle privateKey = null;
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
                    return null;
                }

                return privateKey;
            }
            finally
            {
                // If we're not supposed to release the key handle, then we need to bump the reference count
                // on the safe handle to correspond to the reference that Windows is holding on to.  This will
                // prevent the CLR from freeing the object handle.
                // 
                // This is certainly not the ideal way to solve this problem - it would be better for
                // SafeNCryptKeyHandle to maintain an internal bool field that we could toggle here and
                // have that suppress the release when the CLR calls the ReleaseHandle override.  However, that
                // field does not currently exist, so we'll use this hack instead.
                if (privateKey != null && !freeKey)
                {
                    bool addedRef = false;
                    privateKey.DangerousAddRef(ref addedRef);
                }
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
