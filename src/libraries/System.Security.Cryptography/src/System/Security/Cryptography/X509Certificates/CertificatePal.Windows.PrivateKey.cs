// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
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

        public RSA? GetRSAPrivateKey()
        {
            return GetPrivateKey<RSA>(
                delegate (CspParameters csp)
                {
                    return new RSACryptoServiceProvider(csp);
                },
                delegate (CngKey cngKey)
                {
                    return new RSACng(cngKey, transferOwnership: true);
                }
            );
        }

        public DSA? GetDSAPrivateKey()
        {
            return GetPrivateKey<DSA>(
                delegate (CspParameters csp)
                {
                    return new DSACryptoServiceProvider(csp);
                },
                delegate (CngKey cngKey)
                {
                    return new DSACng(cngKey, transferOwnership: true);
                }
            );
        }

        public ECDsa? GetECDsaPrivateKey()
        {
            return GetPrivateKey<ECDsa>(
                delegate (CspParameters csp)
                {
                    throw new NotSupportedException(SR.NotSupported_ECDsa_Csp);
                },
                delegate (CngKey cngKey)
                {
                    return new ECDsaCng(cngKey, transferOwnership: true);
                }
            );
        }

        public ECDiffieHellman? GetECDiffieHellmanPrivateKey()
        {
            static ECDiffieHellmanCng? FromCngKey(CngKey cngKey)
            {
                if (cngKey.AlgorithmGroup == CngAlgorithmGroup.ECDiffieHellman)
                {
                    return new ECDiffieHellmanCng(cngKey, transferOwnership: true);
                }

                // We might be getting an ECDSA key here. CNG allows ECDH to be either ECDH or ECDSA, however if
                // the AlgorithmGroup is ECDSA, then it cannot be used for ECDH, even though both of them are ECC keys.
                return null;
            }

            return GetPrivateKey<ECDiffieHellman>(
                csp => throw new NotSupportedException(SR.NotSupported_ECDiffieHellman_Csp),
                FromCngKey
            );
        }

        public MLDsa? GetMLDsaPrivateKey()
        {
            return GetPrivateKey<MLDsa>(
                _ =>
                {
                    Debug.Fail("CryptoApi does not support ML-DSA.");
                    throw new PlatformNotSupportedException();
                },
                cngKey => new MLDsaCng(cngKey, transferOwnership: true)
            );
        }

        public MLKem? GetMLKemPrivateKey()
        {
            // MLKem is not supported on Windows.
            return null;
        }

        public SlhDsa? GetSlhDsaPrivateKey()
        {
            // SlhDsa is not supported on Windows.
            return null;
        }

        public ICertificatePal CopyWithPrivateKey(DSA dsa)
        {
            DSACng? dsaCng = dsa as DSACng;
            ICertificatePal? clone;

            if (dsaCng != null)
            {
                clone = CopyWithPersistedCngKey(dsaCng.Key);

                if (clone != null)
                {
                    return clone;
                }
            }

            DSACryptoServiceProvider? dsaCsp = dsa as DSACryptoServiceProvider;

            if (dsaCsp != null)
            {
                clone = CopyWithPersistedCapiKey(dsaCsp.CspKeyContainerInfo);

                if (clone != null)
                {
                    return clone;
                }
            }

            DSAParameters privateParameters = dsa.ExportParameters(true);

            using (PinAndClear.Track(privateParameters.X!))
            using (DSACng clonedKey = new DSACng())
            {
                clonedKey.ImportParameters(privateParameters);

                return CopyWithEphemeralKey(clonedKey.Key);
            }
        }

        public ICertificatePal CopyWithPrivateKey(ECDsa ecdsa)
        {
            ECDsaCng? ecdsaCng = ecdsa as ECDsaCng;

            if (ecdsaCng != null)
            {
                ICertificatePal? clone = CopyWithPersistedCngKey(ecdsaCng.Key);

                if (clone != null)
                {
                    return clone;
                }
            }

            ECParameters privateParameters = ecdsa.ExportParameters(true);

            using (PinAndClear.Track(privateParameters.D!))
            using (ECDsaCng clonedKey = new ECDsaCng())
            {
                clonedKey.ImportParameters(privateParameters);

                return CopyWithEphemeralKey(clonedKey.Key);
            }
        }

        public ICertificatePal CopyWithPrivateKey(ECDiffieHellman ecdh)
        {
            ECDiffieHellmanCng? ecdhCng = ecdh as ECDiffieHellmanCng;

            if (ecdhCng != null)
            {
                ICertificatePal? clone = CopyWithPersistedCngKey(ecdhCng.Key);

                if (clone != null)
                {
                    return clone;
                }
            }

            ECParameters privateParameters = ecdh.ExportParameters(true);

            using (PinAndClear.Track(privateParameters.D!))
            using (ECDiffieHellmanCng clonedKey = new ECDiffieHellmanCng())
            {
                clonedKey.ImportParameters(privateParameters);

                return CopyWithEphemeralKey(clonedKey.Key);
            }
        }

        public ICertificatePal CopyWithPrivateKey(MLDsa privateKey) => CertificateHelpers.CopyWithPrivateKey(this, privateKey);

        public ICertificatePal CopyWithPrivateKey(MLKem privateKey)
        {
            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(MLKem)));
        }

        public ICertificatePal CopyWithPrivateKey(SlhDsa privateKey)
        {
            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(SlhDsa)));
        }

        public ICertificatePal CopyWithPrivateKey(RSA rsa)
        {
            RSACng? rsaCng = rsa as RSACng;
            ICertificatePal? clone;

            if (rsaCng != null)
            {
                clone = CopyWithPersistedCngKey(rsaCng.Key);

                if (clone != null)
                {
                    return clone;
                }
            }

            RSACryptoServiceProvider? rsaCsp = rsa as RSACryptoServiceProvider;

            if (rsaCsp != null)
            {
                clone = CopyWithPersistedCapiKey(rsaCsp.CspKeyContainerInfo);

                if (clone != null)
                {
                    return clone;
                }
            }

            RSAParameters privateParameters = rsa.ExportParameters(true);

            using (PinAndClear.Track(privateParameters.D!))
            using (PinAndClear.Track(privateParameters.P!))
            using (PinAndClear.Track(privateParameters.Q!))
            using (PinAndClear.Track(privateParameters.DP!))
            using (PinAndClear.Track(privateParameters.DQ!))
            using (PinAndClear.Track(privateParameters.InverseQ!))
            using (RSACng clonedKey = new RSACng())
            {
                clonedKey.ImportParameters(privateParameters);

                return CopyWithEphemeralKey(clonedKey.Key);
            }
        }

        private unsafe CertificatePal? CopyWithPersistedCapiKey(CspKeyContainerInfo keyContainerInfo)
        {
            if (string.IsNullOrEmpty(keyContainerInfo.KeyContainerName))
            {
                return null;
            }

            // Make a new pal from bytes.
            CertificatePal pal = (CertificatePal)FromBlob(RawData, SafePasswordHandle.InvalidHandle, X509KeyStorageFlags.PersistKeySet);
            Interop.Crypt32.CRYPT_KEY_PROV_INFO keyProvInfo = default;

            fixed (char* keyName = keyContainerInfo.KeyContainerName)
            fixed (char* provName = keyContainerInfo.ProviderName)
            {
                keyProvInfo.pwszContainerName = keyName;
                keyProvInfo.pwszProvName = provName;
                keyProvInfo.dwFlags = keyContainerInfo.MachineKeyStore ? Interop.Crypt32.CryptAcquireContextFlags.CRYPT_MACHINE_KEYSET : 0;
                keyProvInfo.dwProvType = keyContainerInfo.ProviderType;
                keyProvInfo.dwKeySpec = (int)keyContainerInfo.KeyNumber;

                if (!Interop.Crypt32.CertSetCertificateContextProperty(
                    pal._certContext,
                    Interop.Crypt32.CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID,
                    Interop.Crypt32.CertSetPropertyFlags.None,
                    &keyProvInfo))
                {
                    Exception e = Marshal.GetLastPInvokeError().ToCryptographicException();
                    pal.Dispose();
                    throw e;
                }
            }

            return pal;
        }

        private T? GetPrivateKey<T>(Func<CspParameters, T> createCsp, Func<CngKey, T?> createCng)
            where T : class, IDisposable
        {
            return CertificateHelpers.GetPrivateKey<T>(this, createCsp, createCng);
        }

        private CertificatePal? CopyWithPersistedCngKey(CngKey cngKey) => CertificateHelpers.CopyWithPersistedCngKey(this, cngKey);

        private CertificatePal CopyWithEphemeralKey(CngKey cngKey) => CertificateHelpers.CopyWithEphemeralKey(this, cngKey);
    }
}
