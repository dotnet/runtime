// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

#if SYSTEM_SECURITY_CRYPTOGRAPHY
using TCertificate = System.Security.Cryptography.X509Certificates.CertificatePal;
#else
using TCertificate = System.Security.Cryptography.X509Certificates.X509Certificate2;
#endif

namespace System.Security.Cryptography.X509Certificates
{
    internal static partial class CertificateHelpers
    {
        private static partial CryptographicException GetExceptionForLastError();

        private static partial SafeNCryptKeyHandle CreateSafeNCryptKeyHandle(IntPtr handle, SafeHandle parentHandle);

        private static partial TCertificate CopyFromRawBytes(TCertificate certificate);

        private static partial int GuessKeySpec(CngProvider provider, string keyName, bool machineKey, CngAlgorithmGroup? algorithmGroup);

#if !SYSTEM_SECURITY_CRYPTOGRAPHY
        [SupportedOSPlatform("windows")]
#endif
        internal static TCertificate CopyWithPrivateKey(TCertificate certificate, MLDsa privateKey)
        {
            if (privateKey is MLDsaCng mldsaCng)
            {
                CngKey key = mldsaCng.KeyNoDuplicate;

                TCertificate? clone = CopyWithPersistedCngKey(certificate, key);

                if (clone is not null)
                {
                    return clone;
                }
            }

            if (privateKey is MLDsaImplementation mldsaImplementation)
            {
                using (CngKey clonedKey = mldsaImplementation.CreateEphemeralCng())
                {
                    return CopyWithEphemeralKey(certificate, clonedKey);
                }
            }

            // MLDsaCng and third-party implementations can be copied by exporting the PKCS#8 and importing it into
            // a new MLDsaCng. An alternative to PKCS#8 would be to try the private seed and fall back to private key,
            // but that potentially requires two calls and wouldn't allow implementations to do anything smarter internally.
            // Blobs may also be an option for MLDsaCng, but for now we will stick with PKCS#8.
            byte[] exportedPkcs8 = privateKey.ExportPkcs8PrivateKey();

            using (PinAndClear.Track(exportedPkcs8))
            using (MLDsaCng clonedKey = MLDsaCng.ImportPkcs8PrivateKey(exportedPkcs8, out _))
            {
                CngKey clonedCngKey = clonedKey.KeyNoDuplicate;

                if (clonedCngKey.AlgorithmGroup != CngAlgorithmGroup.MLDsa)
                {
                    Debug.Fail($"{nameof(MLDsaCng)} should only give ML-DSA keys.");
                    throw new CryptographicException();
                }

                return CopyWithEphemeralKey(certificate, clonedCngKey);
            }
        }

        [SupportedOSPlatform("windows")]
        internal static T? GetPrivateKey<T>(TCertificate certificate, Func<CspParameters, T> createCsp, Func<CngKey, T?> createCng)
            where T : class, IDisposable
        {
            using (SafeCertContextHandle certContext = Interop.Crypt32.CertDuplicateCertificateContext(certificate.Handle))
            {
                SafeNCryptKeyHandle? ncryptKey = TryAcquireCngPrivateKey(certContext, out CngKeyHandleOpenOptions cngHandleOptions);
                if (ncryptKey != null)
                {
#if SYSTEM_SECURITY_CRYPTOGRAPHY
                    CngKey cngKey = CngKey.OpenNoDuplicate(ncryptKey, cngHandleOptions);
#else
                    CngKey cngKey = CngKey.Open(ncryptKey, cngHandleOptions);
#endif
                    T? result = createCng(cngKey);

                    // Dispose of cngKey if its ownership did not transfer to the underlying algorithm.
                    if (result is null)
                    {
                        cngKey.Dispose();
                    }

                    return result;
                }

                CspParameters? cspParameters = GetPrivateKeyCsp(certContext);
                if (cspParameters == null)
                    return null;

                if (cspParameters.ProviderType == 0)
                {
                    // ProviderType being 0 signifies that this is actually a CNG key, not a CAPI key. Crypt32.dll stuffs the CNG Key Storage Provider
                    // name into CRYPT_KEY_PROV_INFO->ProviderName, and the CNG key name into CRYPT_KEY_PROV_INFO->KeyContainerName.

                    string keyStorageProvider = cspParameters.ProviderName!;
                    string keyName = cspParameters.KeyContainerName!;
                    CngKey cngKey = CngKey.Open(keyName, new CngProvider(keyStorageProvider));
                    return createCng(cngKey);
                }
                else
                {
                    // ProviderType being non-zero signifies that this is a CAPI key.
                    // We never want to stomp over certificate private keys.
                    cspParameters.Flags |= CspProviderFlags.UseExistingKey;
                    return createCsp(cspParameters);
                }
            }
        }

#if !SYSTEM_SECURITY_CRYPTOGRAPHY
        [SupportedOSPlatform("windows")]
#endif
        private static SafeNCryptKeyHandle? TryAcquireCngPrivateKey(
            SafeCertContextHandle certificateContext,
            out CngKeyHandleOpenOptions handleOptions)
        {
            Debug.Assert(certificateContext != null);
            Debug.Assert(!certificateContext.IsClosed && !certificateContext.IsInvalid);

            // If the certificate has a key handle without a key prov info, return the
            // ephemeral key
            if (!certificateContext.HasPersistedPrivateKey)
            {
                int cbData = IntPtr.Size;

                if (Interop.Crypt32.CertGetCertificateContextProperty(
                    certificateContext,
                    Interop.Crypt32.CertContextPropId.CERT_NCRYPT_KEY_HANDLE_PROP_ID,
                    out IntPtr privateKeyPtr,
                    ref cbData))
                {
                    handleOptions = CngKeyHandleOpenOptions.EphemeralKey;
                    return CreateSafeNCryptKeyHandle(privateKeyPtr, certificateContext);
                }
            }

            bool freeKey = true;
            SafeNCryptKeyHandle? privateKey = null;
            handleOptions = CngKeyHandleOpenOptions.None;
            try
            {
                if (!Interop.Crypt32.CryptAcquireCertificatePrivateKey(
                    certificateContext,
                    Interop.Crypt32.CryptAcquireCertificatePrivateKeyFlags.CRYPT_ACQUIRE_ONLY_NCRYPT_KEY_FLAG,
                    IntPtr.Zero,
                    out privateKey,
                    out Interop.Crypt32.CryptKeySpec _,
                    out freeKey))
                {

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
                    SafeNCryptKeyHandle newKeyHandle = CreateSafeNCryptKeyHandle(privateKey.DangerousGetHandle(), certificateContext);
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
#if !SYSTEM_SECURITY_CRYPTOGRAPHY
        [SupportedOSPlatform("windows")]
#endif
        internal static CspParameters? GetPrivateKeyCsp(SafeCertContextHandle hCertContext)
        {
            int cbData = 0;
            if (!Interop.Crypt32.CertGetCertificateContextProperty(hCertContext, Interop.Crypt32.CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID, null, ref cbData))
            {
#if NETFRAMEWORK
                int dwErrorCode = Marshal.GetHRForLastWin32Error();
#else
                int dwErrorCode = Marshal.GetLastPInvokeError();
#endif
                if (dwErrorCode == ErrorCode.CRYPT_E_NOT_FOUND)
                    return null;
                throw dwErrorCode.ToCryptographicException();
            }

            unsafe
            {
                byte[] privateKey = new byte[cbData];
                fixed (byte* pPrivateKey = privateKey)
                {
                    if (!Interop.Crypt32.CertGetCertificateContextProperty(hCertContext, Interop.Crypt32.CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID, privateKey, ref cbData))
                        throw GetExceptionForLastError();
                    Interop.Crypt32.CRYPT_KEY_PROV_INFO* pKeyProvInfo = (Interop.Crypt32.CRYPT_KEY_PROV_INFO*)pPrivateKey;

                    return new CspParameters
                    {
                        ProviderName = Marshal.PtrToStringUni((IntPtr)(pKeyProvInfo->pwszProvName)),
                        KeyContainerName = Marshal.PtrToStringUni((IntPtr)(pKeyProvInfo->pwszContainerName)),
                        ProviderType = pKeyProvInfo->dwProvType,
                        KeyNumber = pKeyProvInfo->dwKeySpec,
                        Flags = (pKeyProvInfo->dwFlags & Interop.Crypt32.CryptAcquireContextFlags.CRYPT_MACHINE_KEYSET) == Interop.Crypt32.CryptAcquireContextFlags.CRYPT_MACHINE_KEYSET ? CspProviderFlags.UseMachineKeyStore : 0,
                    };
                }
            }
        }

#if !SYSTEM_SECURITY_CRYPTOGRAPHY
        [UnsupportedOSPlatform("browser")]
#endif
        internal static unsafe TCertificate? CopyWithPersistedCngKey(TCertificate certificate, CngKey cngKey)
        {
            if (string.IsNullOrEmpty(cngKey.KeyName))
            {
                return null;
            }

            // Make a new pal from bytes.
            TCertificate newCert = CopyFromRawBytes(certificate);

            CngProvider provider = cngKey.Provider!;
            string keyName = cngKey.KeyName;
            bool machineKey = cngKey.IsMachineKey;

            // CAPI RSA_SIGN keys won't open correctly under CNG without the key number being specified, so
            // check to see if we can figure out what key number it needs to re-open.
            int keySpec = GuessKeySpec(provider, keyName, machineKey, cngKey.AlgorithmGroup);

            Interop.Crypt32.CRYPT_KEY_PROV_INFO keyProvInfo = default;

            fixed (char* keyNamePtr = cngKey.KeyName)
            fixed (char* provNamePtr = cngKey.Provider!.Provider)
            {
                keyProvInfo.pwszContainerName = keyNamePtr;
                keyProvInfo.pwszProvName = provNamePtr;
                keyProvInfo.dwFlags = machineKey ? Interop.Crypt32.CryptAcquireContextFlags.CRYPT_MACHINE_KEYSET : 0;
                keyProvInfo.dwKeySpec = keySpec;

                if (!Interop.Crypt32.CertSetCertificateContextProperty(
                    newCert.Handle,
                    Interop.Crypt32.CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID,
                    Interop.Crypt32.CertSetPropertyFlags.None,
                    &keyProvInfo))
                {
                    Exception e = GetExceptionForLastError();
                    newCert.Dispose();
                    throw e;
                }
            }

            return newCert;
        }

#if !SYSTEM_SECURITY_CRYPTOGRAPHY
        [UnsupportedOSPlatform("browser")]
#endif
        internal static TCertificate CopyWithEphemeralKey(TCertificate certificate, CngKey cngKey)
        {
            Debug.Assert(string.IsNullOrEmpty(cngKey.KeyName));

            // Handle makes a copy of the handle.  This is required given that CertSetCertificateContextProperty accepts a SafeHandle
            // and transfers ownership of the handle to the certificate.  We can't transfer that ownership out of the cngKey, as it's
            // owned by the caller, so we make a copy.
            using (SafeNCryptKeyHandle handle = cngKey.Handle)
            {
                // Make a new certificate from bytes.
                TCertificate newCert = CopyFromRawBytes(certificate);
                try
                {
                    if (!Interop.Crypt32.CertSetCertificateContextProperty(
                        newCert.Handle,
                        Interop.Crypt32.CertContextPropId.CERT_NCRYPT_KEY_HANDLE_PROP_ID,
                        Interop.Crypt32.CertSetPropertyFlags.CERT_SET_PROPERTY_INHIBIT_PERSIST_FLAG,
                        handle))
                    {
                        throw GetExceptionForLastError();
                    }

                    // The value was transferred to the certificate.
                    handle.SetHandleAsInvalid();

                    return newCert;
                }
                catch
                {
                    newCert.Dispose();
                    throw;
                }
            }
        }
    }
}
