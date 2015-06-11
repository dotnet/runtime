// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

using Internal.NativeCrypto;
using Internal.Cryptography;
using Internal.Cryptography.Pal.Native;

using FILETIME = Internal.Cryptography.Pal.Native.FILETIME;

using CryptographicUnexpectedOperationException = System.Security.Cryptography.CryptographicException;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

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

        public AsymmetricAlgorithm PrivateKey
        {
            get
            {
                CspParameters cspParameters = GetPrivateKey();
                if (cspParameters == null)
                    return null;

                // We never want to stomp over certificate private keys.
                cspParameters.Flags |= CspProviderFlags.UseExistingKey;

                int algId = OidInfo.FindOidInfo(CryptOidInfoKeyType.CRYPT_OID_INFO_OID_KEY, KeyAlgorithm, OidGroup.PublicKeyAlgorithm, fallBackToAllGroups: true).AlgId;
                switch (algId)
                {
                    case AlgId.CALG_RSA_KEYX:
                    case AlgId.CALG_RSA_SIGN:
                        return new RSACryptoServiceProvider(cspParameters);

                    case AlgId.CALG_DSS_SIGN:
                        return new DSACryptoServiceProvider(cspParameters);

                    default:
                        throw new NotSupportedException(SR.NotSupported_KeyAlgorithm);
                }
            }
        }

        /// <summary>
        /// Set the private key object. The additional "publicKey" argument is used to validate that the private key corresponds to the existing publicKey.
        /// </summary>
        public void SetPrivateKey(AsymmetricAlgorithm privateKey, AsymmetricAlgorithm publicKey)
        {
            if (privateKey == null)
            {
                unsafe
                {
                    if (!Interop.crypt32.CertSetCertificateContextProperty(_certContext, CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID, CertSetPropertyFlags.None, (CRYPT_KEY_PROV_INFO*)null))
                        throw new CryptographicException(Marshal.GetLastWin32Error());
                }
            }
            else
            {
                // we do not support keys in non-CAPI storage for now.
                ICspAsymmetricAlgorithm asymmetricAlgorithm = privateKey as ICspAsymmetricAlgorithm;
                if (asymmetricAlgorithm == null)
                    throw new NotSupportedException(SR.NotSupported_InvalidKeyImpl);
                if (asymmetricAlgorithm.CspKeyContainerInfo == null)
                    throw new ArgumentException("CspKeyContainerInfo");

                // check that the public key in the certificate corresponds to the private key passed in.
                // 
                // note that it should be legal to set a key which matches in every aspect but the usage
                // i.e. to use a CALG_RSA_KEYX private key to match a CALG_RSA_SIGN public key. A
                // PUBLICKEYBLOB is defined as:
                //
                //  BLOBHEADER publickeystruc
                //  RSAPUBKEY rsapubkey
                //  BYTE modulus[rsapubkey.bitlen/8]
                //  
                // To allow keys which differ by key usage only, we skip over the BLOBHEADER of the key,
                // and start comparing bytes at the RSAPUBKEY structure.
                unsafe
                {
                    // This cast is safe because via our contract with our caller, "publicKey" is the Key property of a PublicKey object that this Pal class manufactured in the first place.
                    // Since we manufactured the PublicKey, we know the real types of the object.
                    ICspAsymmetricAlgorithm cspPublicKey = (ICspAsymmetricAlgorithm)publicKey;
                    byte[] array1 = cspPublicKey.ExportCspBlob(false);
                    byte[] array2 = asymmetricAlgorithm.ExportCspBlob(false);
                    if (array1 == null || array2 == null || array1.Length != array2.Length || array1.Length <= sizeof(BLOBHEADER))
                        throw new CryptographicUnexpectedOperationException(SR.Cryptography_X509_KeyMismatch);
                    for (int index = sizeof(BLOBHEADER); index < array1.Length; index++)
                    {
                        if (array1[index] != array2[index])
                            throw new CryptographicUnexpectedOperationException(SR.Cryptography_X509_KeyMismatch);
                    }
                }

                unsafe
                {
                    CspKeyContainerInfo keyContainerInfo = asymmetricAlgorithm.CspKeyContainerInfo;
                    fixed (char* pKeyContainerName = keyContainerInfo.KeyContainerName, pProviderName = keyContainerInfo.ProviderName)
                    {
                        CRYPT_KEY_PROV_INFO keyProvInfo = new CRYPT_KEY_PROV_INFO()
                        {
                            pwszContainerName = pKeyContainerName,
                            pwszProvName = pProviderName,
                            dwProvType = keyContainerInfo.ProviderType,
                            dwFlags = keyContainerInfo.MachineKeyStore ? CryptAcquireContextFlags.CRYPT_MACHINE_KEYSET : CryptAcquireContextFlags.None,
                            cProvParam = 0,
                            rgProvParam = IntPtr.Zero,
                            dwKeySpec = (int)(keyContainerInfo.KeyNumber),
                        };

                        if (!Interop.crypt32.CertSetCertificateContextProperty(_certContext, CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID, CertSetPropertyFlags.None, &keyProvInfo))
                            throw new CryptographicException(Marshal.GetLastWin32Error());
                    }
                }
            }
        }

        private CspParameters GetPrivateKey()
        {
            int cbData = 0;
            if (!Interop.crypt32.CertGetCertificateContextProperty(_certContext, CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID, null, ref cbData))
            {
                int dwErrorCode = Marshal.GetLastWin32Error();
                if (dwErrorCode == ErrorCode.CRYPT_E_NOT_FOUND)
                    return null;
                throw new CryptographicException(dwErrorCode);
            }

            unsafe
            {
                byte[] privateKey = new byte[cbData];
                fixed (byte* pPrivateKey = privateKey)
                {
                    if (!Interop.crypt32.CertGetCertificateContextProperty(_certContext, CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID, privateKey, ref cbData))
                        throw new CryptographicException(Marshal.GetLastWin32Error());
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
