// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

#if !NET10_0_OR_GREATER
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
#endif

#if !NET10_0_OR_GREATER && !NETSTANDARD
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Reflection;
#endif

namespace System.Security.Cryptography.X509Certificates
{
    /// <summary>
    /// Helper methods to access keys on <see cref="X509Certificate2"/>.
    /// </summary>
    public static class X509CertificateKeyAccessors
    {
        /// <summary>
        ///   Gets the <see cref="MLKem"/> public key from this certificate.
        /// </summary>
        /// <param name="certificate">
        ///   The X.509 certificate that contains the public key.
        /// </param>
        /// <returns>
        ///   The public key, or <see langword="null"/> if this certificate does not have an ML-KEM public key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="certificate"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The certificate has an ML-KEM public key, but the platform does not support ML-KEM.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The public key was invalid, or otherwise could not be imported.
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static MLKem? GetMLKemPublicKey(this X509Certificate2 certificate)
        {
#if NET10_0_OR_GREATER
            return certificate.GetMLKemPublicKey();
#else
            ArgumentNullException.ThrowIfNull(certificate);

            if (MLKemAlgorithm.FromOid(certificate.GetKeyAlgorithm()) is null)
            {
                return null;
            }

            ArraySegment<byte> encoded = GetCertificateSubjectPublicKeyInfo(certificate);

            try
            {
                return MLKem.ImportSubjectPublicKeyInfo(encoded);
            }
            finally
            {
                // SubjectPublicKeyInfo does not need to clear since it's public
                CryptoPool.Return(encoded, clearSize: 0);
            }
#endif
        }

        /// <summary>
        ///   Gets the <see cref="MLKem"/> private key from this certificate.
        /// </summary>
        /// <param name="certificate">
        ///   The X.509 certificate that contains the private key.
        /// </param>
        /// <returns>
        ///   The private key, or <see langword="null"/> if this certificate does not have an ML-KEM private key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   An error occurred accessing the private key.
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static MLKem? GetMLKemPrivateKey(this X509Certificate2 certificate) =>
#if NET10_0_OR_GREATER
            certificate.GetMLKemPrivateKey();
#else
            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(MLKem)));
#endif

        /// <summary>
        ///   Combines a private key with a certificate containing the associated public key into a
        ///   new instance that can access the private key.
        /// </summary>
        /// <param name="certificate">
        ///   The X.509 certificate that contains the public key.
        /// </param>
        /// <param name="privateKey">
        ///   The ML-KEM private key that corresponds to the ML-KEM public key in this certificate.
        /// </param>
        /// <returns>
        ///   A new certificate with the <see cref="X509Certificate2.HasPrivateKey" /> property set to <see langword="true"/>.
        ///   The current certificate isn't modified.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="certificate"/> or <paramref name="privateKey"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   The specified private key doesn't match the public key for this certificate.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   The certificate already has an associated private key.
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static X509Certificate2 CopyWithPrivateKey(this X509Certificate2 certificate, MLKem privateKey) =>
#if NET10_0_OR_GREATER
            certificate.CopyWithPrivateKey(privateKey);
#else
            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(MLKem)));
#endif

        /// <summary>
        ///   Gets the <see cref="MLDsa"/> public key from this certificate.
        /// </summary>
        /// <param name="certificate">
        ///   The X.509 certificate that contains the public key.
        /// </param>
        /// <returns>
        ///   The public key, or <see langword="null"/> if this certificate does not have an ML-DSA public key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="certificate"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The certificate has an ML-DSA public key, but the platform does not support ML-DSA.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The public key was invalid, or otherwise could not be imported.
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static MLDsa? GetMLDsaPublicKey(this X509Certificate2 certificate)
        {
#if NET10_0_OR_GREATER
            return certificate.GetMLDsaPublicKey();
#else
            ArgumentNullException.ThrowIfNull(certificate);

            if (MLDsaAlgorithm.GetMLDsaAlgorithmFromOid(certificate.GetKeyAlgorithm()) is null)
            {
                return null;
            }

            ArraySegment<byte> encoded = GetCertificateSubjectPublicKeyInfo(certificate);

            try
            {
                return MLDsa.ImportSubjectPublicKeyInfo(encoded);
            }
            finally
            {
                // SubjectPublicKeyInfo does not need to clear since it's public
                CryptoPool.Return(encoded, clearSize: 0);
            }
#endif
        }

        /// <summary>
        ///   Gets the <see cref="MLDsa"/> private key from this certificate.
        /// </summary>
        /// <param name="certificate">
        ///   The X.509 certificate that contains the private key.
        /// </param>
        /// <returns>
        ///   The private key, or <see langword="null"/> if this certificate does not have an ML-DSA private key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   An error occurred accessing the private key.
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]

        // TODO This is a public API update:
        [SupportedOSPlatform("windows")]
        public static MLDsa? GetMLDsaPrivateKey(this X509Certificate2 certificate)
#if NET10_0_OR_GREATER
            => certificate.GetMLDsaPrivateKey();
#elif NETSTANDARD
            => throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(MLDsa)));
#else
        {
            return GetPrivateKey<MLDsa>(certificate, cngKey => new MLDsaCng(cngKey, transferOwnership: true));
        }
#endif

        /// <summary>
        ///   Combines a private key with a certificate containing the associated public key into a
        ///   new instance that can access the private key.
        /// </summary>
        /// <param name="certificate">
        ///   The X.509 certificate that contains the public key.
        /// </param>
        /// <param name="privateKey">
        ///   The ML-DSA private key that corresponds to the ML-DSA public key in this certificate.
        /// </param>
        /// <returns>
        ///   A new certificate with the <see cref="X509Certificate2.HasPrivateKey" /> property set to <see langword="true"/>.
        ///   The current certificate isn't modified.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="certificate"/> or <paramref name="privateKey"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   The specified private key doesn't match the public key for this certificate.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   The certificate already has an associated private key.
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]

        // TODO This is a public API update:
        [SupportedOSPlatform("windows")]
        public static X509Certificate2 CopyWithPrivateKey(this X509Certificate2 certificate, MLDsa privateKey)
#if NET10_0_OR_GREATER
             => certificate.CopyWithPrivateKey(privateKey);
#elif NETSTANDARD
            => throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(MLDsa)));
#else
        {
            ArgumentNullException.ThrowIfNull(certificate);
            ArgumentNullException.ThrowIfNull(privateKey);

            if (certificate.HasPrivateKey)
                throw new InvalidOperationException(SR.Cryptography_Cert_AlreadyHasPrivateKey);

            using (MLDsa? publicKey = GetMLDsaPublicKey(certificate))
            {
                if (publicKey is null)
                {
                    throw new ArgumentException(SR.Cryptography_PrivateKey_WrongAlgorithm);
                }

                if (publicKey.Algorithm != privateKey.Algorithm)
                {
                    throw new ArgumentException(SR.Cryptography_PrivateKey_DoesNotMatch, nameof(privateKey));
                }

                byte[] pk1 = publicKey.ExportMLDsaPublicKey();
                byte[] pk2 = privateKey.ExportMLDsaPublicKey();

                if (pk1.Length != pk2.Length || !pk1.AsSpan().SequenceEqual(pk2))
                {
                    throw new ArgumentException(SR.Cryptography_PrivateKey_DoesNotMatch, nameof(privateKey));
                }
            }

            using (SafeCertContextHandle hCertContext = CopyWithPrivateKeyImpl(certificate, privateKey))
            {
                return new X509Certificate2(hCertContext.DangerousGetHandle());
            }
        }
#endif

        /// <summary>
        ///   Gets the <see cref="SlhDsa"/> public key from this certificate.
        /// </summary>
        /// <param name="certificate">
        ///   The X509 certificate that contains the public key.
        /// </param>
        /// <returns>
        ///   The public key, or <see langword="null"/> if this certificate does not have an SLH-DSA public key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="certificate"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The certificate has an SLH-DSA public key, but the platform does not support SLH-DSA.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The public key was invalid, or otherwise could not be imported.
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static SlhDsa? GetSlhDsaPublicKey(this X509Certificate2 certificate) =>
#if NET10_0_OR_GREATER
            certificate.GetSlhDsaPublicKey();
#else
            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(SlhDsa)));
#endif

        /// <summary>
        ///   Gets the <see cref="SlhDsa"/> private key from this certificate.
        /// </summary>
        /// <param name="certificate">
        ///   The X509 certificate that contains the private key.
        /// </param>
        /// <returns>
        ///   The private key, or <see langword="null"/> if this certificate does not have an SLH-DSA private key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   An error occurred accessing the private key.
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static SlhDsa? GetSlhDsaPrivateKey(this X509Certificate2 certificate) =>
#if NET10_0_OR_GREATER
            certificate.GetSlhDsaPrivateKey();
#else
            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(SlhDsa)));
#endif

        /// <summary>
        ///   Combines a private key with a certificate containing the associated public key into a
        ///   new instance that can access the private key.
        /// </summary>
        /// <param name="certificate">
        ///   The X509 certificate that contains the public key.
        /// </param>
        /// <param name="privateKey">
        ///   The SLH-DSA private key that corresponds to the SLH-DSA public key in this certificate.
        /// </param>
        /// <returns>
        ///   A new certificate with the <see cref="X509Certificate2.HasPrivateKey" /> property set to <see langword="true"/>.
        ///   The current certificate isn't modified.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="certificate"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="privateKey"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   The specified private key doesn't match the public key for this certificate.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   The certificate already has an associated private key.
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static X509Certificate2 CopyWithPrivateKey(this X509Certificate2 certificate, SlhDsa privateKey) =>
#if NET10_0_OR_GREATER
            certificate.CopyWithPrivateKey(privateKey);
#else
            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(SlhDsa)));
#endif

#if !NET10_0_OR_GREATER
        private static ArraySegment<byte> GetCertificateSubjectPublicKeyInfo(X509Certificate2 certificate)
        {
            // We construct the SubjectPublicKeyInfo from the certificate as-is, parameters and all. Consumers
            // decide if the parameters are good or not.
            SubjectPublicKeyInfoAsn spki = new SubjectPublicKeyInfoAsn
            {
                Algorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = certificate.GetKeyAlgorithm(),

                    // .NET Framework uses "empty" to indicate no value, not null, so normalize empty to null since
                    // the Asn types expect the parameters to be an ASN.1 ANY.
                    Parameters = certificate.GetKeyAlgorithmParameters() switch
                    {
                        null or { Length: 0 } => default(ReadOnlyMemory<byte>?),
                        byte[] array => array,
                    },
                },
                SubjectPublicKey = certificate.GetPublicKey(),
            };

            AsnWriter writer = new(AsnEncodingRules.DER);
            spki.Encode(writer);

            byte[] rented = CryptoPool.Rent(writer.GetEncodedLength());
            int written = writer.Encode(rented);
            return new ArraySegment<byte>(rented, offset: 0, count: written);
        }
#endif

#if !NET10_0_OR_GREATER && !NETSTANDARD
        [SupportedOSPlatform("windows")]
        private static T? GetPrivateKey<T>(X509Certificate2 certificate, Func<CngKey, T?> createCng)
            where T : class, IDisposable
        {
            // TODO Could there be a race if certificate.Handle is freed from another thread while we are in
            // the following CertDuplicateCertificateContext call?

            using (SafeCertContextHandle certContext = Interop.Crypt32.CertDuplicateCertificateContext(certificate.Handle))
            {
                SafeNCryptKeyHandle? ncryptKey = TryAcquireCngPrivateKey(certContext, out CngKeyHandleOpenOptions cngHandleOptions);
                if (ncryptKey != null)
                {
                    CngKey cngKey = CngKey.Open(ncryptKey, cngHandleOptions);
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

                    // TODO CAPI key is not supported
                    // TODO resx
                    throw new CryptographicException();
                }
            }
        }

        [SupportedOSPlatform("windows")]
        private static SafeNCryptKeyHandle CreateSafeNCryptKeyHandle(IntPtr handle, SafeHandle parentHandle)
        {
#if NETFRAMEWORK
            ConstructorInfo ctor = typeof(SafeNCryptKeyHandle).GetConstructor([typeof(IntPtr), typeof(SafeHandle)]);
            if (ctor == null)
            {
                // TODO resx
                throw new PlatformNotSupportedException();
            }

            return (SafeNCryptKeyHandle)ctor.Invoke(new object?[] { handle, parentHandle });
#else
            return new SafeNCryptKeyHandle(handle, parentHandle);
#endif
        }

        [SupportedOSPlatform("windows")]
        private static SafeNCryptKeyHandle? TryAcquireCngPrivateKey(
            SafeCertContextHandle certificateContext,
            out CngKeyHandleOpenOptions handleOptions)
        {
            Debug.Assert(certificateContext != null);
            Debug.Assert(!certificateContext.IsClosed && !certificateContext.IsInvalid);

            IntPtr privateKeyPtr;

            // If the certificate has a key handle without a key prov info, return the
            // ephemeral key
            if (!certificateContext.HasPersistedPrivateKey)
            {
                int cbData = IntPtr.Size;

                if (Interop.Crypt32.CertGetCertificateContextProperty(
                    certificateContext,
                    Interop.Crypt32.CertContextPropId.CERT_NCRYPT_KEY_HANDLE_PROP_ID,
                    out privateKeyPtr,
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
                Interop.Crypt32.CryptKeySpec keySpec = 0;
                if (!Interop.Crypt32.CryptAcquireCertificatePrivateKey(
                    certificateContext,
                    Interop.Crypt32.CryptAcquireCertificatePrivateKeyFlags.CRYPT_ACQUIRE_ONLY_NCRYPT_KEY_FLAG,
                    IntPtr.Zero,
                    out privateKey,
                    out keySpec,
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
                    var newKeyHandle = CreateSafeNCryptKeyHandle(privateKey.DangerousGetHandle(), certificateContext);
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
        [SupportedOSPlatform("windows")]
        private static CspParameters? GetPrivateKeyCsp(SafeCertContextHandle hCertContext)
        {
            int cbData = 0;
            if (!Interop.Crypt32.CertGetCertificateContextProperty(hCertContext, Interop.Crypt32.CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID, null, ref cbData))
            {
#if NETFRAMEWORK
                int dwErrorCode = Marshal.GetLastWin32Error();
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
#if NETFRAMEWORK
                        throw Marshal.GetLastWin32Error().ToCryptographicException();
#else
                        throw Marshal.GetLastPInvokeError().ToCryptographicException();
#endif
                    Interop.Crypt32.CRYPT_KEY_PROV_INFO* pKeyProvInfo = (Interop.Crypt32.CRYPT_KEY_PROV_INFO*)pPrivateKey;

                    CspParameters cspParameters = new CspParameters();
                    cspParameters.ProviderName = Marshal.PtrToStringUni((IntPtr)(pKeyProvInfo->pwszProvName));
                    cspParameters.KeyContainerName = Marshal.PtrToStringUni((IntPtr)(pKeyProvInfo->pwszContainerName));
                    cspParameters.ProviderType = pKeyProvInfo->dwProvType;
                    cspParameters.KeyNumber = pKeyProvInfo->dwKeySpec;
                    cspParameters.Flags = (CspProviderFlags)((pKeyProvInfo->dwFlags & Interop.Crypt32.CryptAcquireContextFlags.CRYPT_MACHINE_KEYSET) == Interop.Crypt32.CryptAcquireContextFlags.CRYPT_MACHINE_KEYSET ? CspProviderFlags.UseMachineKeyStore : 0);
                    return cspParameters;
                }
            }
        }

        [SupportedOSPlatform("windows")]
        internal static SafeCertContextHandle CopyWithPrivateKeyImpl(X509Certificate2 certificate, MLDsa privateKey)
        {
            if (privateKey is MLDsaCng mldsaCng)
            {
                CngKey key = mldsaCng.KeyNoDuplicate;

                SafeCertContextHandle? clone = CopyWithPersistedCngKey(certificate, key);

                if (clone != null)
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
            // a new MLDsaCng. An alternative to PKCS#8 would be to try the private seed and fall back to secret key,
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

        private static SafeCertContextHandle CopyWithEphemeralKey(X509Certificate2 certificate, CngKey cngKey)
        {
            Debug.Assert(string.IsNullOrEmpty(cngKey.KeyName));

            // Handle makes a copy of the handle.  This is required given that CertSetCertificateContextProperty accepts a SafeHandle
            // and transfers ownership of the handle to the certificate.  We can't transfer that ownership out of the cngKey, as it's
            // owned by the caller, so we make a copy (using Handle rather than HandleNoDuplicate).
            using (SafeNCryptKeyHandle handle = cngKey.Handle)
            {
                // Make a new pal from bytes.
                SafeCertContextHandle hCertContext = FromBlob(certificate.RawData, X509KeyStorageFlags.PersistKeySet);
                try
                {
                    if (!Interop.Crypt32.CertSetCertificateContextProperty(
                        hCertContext,
                        Interop.Crypt32.CertContextPropId.CERT_NCRYPT_KEY_HANDLE_PROP_ID,
                        Interop.Crypt32.CertSetPropertyFlags.CERT_SET_PROPERTY_INHIBIT_PERSIST_FLAG,
                        handle))
                    {
#if NETFRAMEWORK
                        // TODO CertSetCertificateContextProperty has SetLastError=true so I think
                        // this should be equivalent to Marshal.GetLastPInvokeError?
                        throw Marshal.GetLastWin32Error().ToCryptographicException();
#else
                        throw Marshal.GetLastPInvokeError().ToCryptographicException();
#endif
                    }

                    // The value was transferred to the certificate.
                    handle.SetHandleAsInvalid();

                    return hCertContext;
                }
                catch
                {
                    hCertContext.Dispose();
                    throw;
                }
            }
        }

        private static unsafe SafeCertContextHandle? CopyWithPersistedCngKey(X509Certificate2 certificate, CngKey cngKey)
        {
            if (string.IsNullOrEmpty(cngKey.KeyName))
            {
                return null;
            }

            // Make a new pal from bytes.
            SafeCertContextHandle hCertContext = FromBlob(certificate.RawData, X509KeyStorageFlags.PersistKeySet);

            CngProvider provider = cngKey.Provider!;
            string keyName = cngKey.KeyName;
            bool machineKey = cngKey.IsMachineKey;

            // TODO I think GuessKeySpec is only relevant for RSA/DSA?
            int keySpec = 0;

            Interop.Crypt32.CRYPT_KEY_PROV_INFO keyProvInfo = default;

            fixed (char* keyNamePtr = cngKey.KeyName)
            fixed (char* provNamePtr = cngKey.Provider!.Provider)
            {
                keyProvInfo.pwszContainerName = keyNamePtr;
                keyProvInfo.pwszProvName = provNamePtr;
                keyProvInfo.dwFlags = machineKey ? Interop.Crypt32.CryptAcquireContextFlags.CRYPT_MACHINE_KEYSET : 0;
                keyProvInfo.dwKeySpec = keySpec;

                if (!Interop.Crypt32.CertSetCertificateContextProperty(
                    hCertContext,
                    Interop.Crypt32.CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID,
                    Interop.Crypt32.CertSetPropertyFlags.None,
                    &keyProvInfo))
                {
#if NETFRAMEWORK
                    // TODO CertSetCertificateContextProperty has SetLastError=true so I think
                    // this should be equivalent to Marshal.GetLastPInvokeError?
                    Exception e = Marshal.GetLastWin32Error().ToCryptographicException();
#else
                    Exception e = Marshal.GetLastPInvokeError().ToCryptographicException();
#endif
                    hCertContext.Dispose();
                    throw e;
                }
            }

            return hCertContext;
        }

        // TODO if this is only needed in MBC, then move it there instead of common.

        // TODO consider using SafeCertContextHandle instead nint
        private static SafeCertContextHandle FromBlob(ReadOnlySpan<byte> rawData, X509KeyStorageFlags keyStorageFlags)
        {
            Debug.Assert(!rawData.IsEmpty);

            Interop.Crypt32.ContentType contentType;
            SafeCertStoreHandle? hCertStore = null;
            SafeCryptMsgHandle? hCryptMsg = null;
            SafeCertContextHandle? hCertContext = null;

            try
            {
                unsafe
                {
                    fixed (byte* pRawData = rawData)
                    {
                        Interop.Crypt32.DATA_BLOB certBlob = new Interop.Crypt32.DATA_BLOB(new IntPtr(pRawData), (uint)rawData.Length);

                        Interop.Crypt32.CertQueryObjectType objectType = Interop.Crypt32.CertQueryObjectType.CERT_QUERY_OBJECT_BLOB;
                        void* pvObject = &certBlob;

                        bool success = Interop.Crypt32.CryptQueryObject(
                            objectType,
                            pvObject,
                            X509ExpectedContentTypeFlags,
                            X509ExpectedFormatTypeFlags,
                            0,
                            out _,
                            out contentType,
                            out _,
                            out hCertStore,
                            out hCryptMsg,
                            out hCertContext);

                        if (!success)
                        {
                            int hr = Marshal.GetHRForLastWin32Error();
                            throw hr.ToCryptographicException();
                        }
                    }

                    if (contentType is Interop.Crypt32.ContentType.CERT_QUERY_CONTENT_PKCS7_SIGNED or
                                       Interop.Crypt32.ContentType.CERT_QUERY_CONTENT_PKCS7_SIGNED_EMBED or
                                       Interop.Crypt32.ContentType.CERT_QUERY_CONTENT_PFX)
                    {
                        // TODO these are supported in SSC but not MBC. Are they valid return types for BLOBs
                        // and if so, do we want to support them?
                        // It requires adding more pfx loading support and some pkcs7 support (latter doesn't seem hard to add).

                        // TODO free the cert context if it has been set

                        // TODO resx
                        throw new CryptographicException();
                    }

                    return hCertContext;
                }
            }
            finally
            {
                hCertStore?.Dispose();
                hCryptMsg?.Dispose();
            }
        }

        private const Interop.Crypt32.ExpectedContentTypeFlags X509ExpectedContentTypeFlags =
            Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_CERT |
            Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_SERIALIZED_CERT |
            Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED |
            Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED_EMBED |
            Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_PFX;

        private const Interop.Crypt32.ExpectedFormatTypeFlags X509ExpectedFormatTypeFlags = Interop.Crypt32.ExpectedFormatTypeFlags.CERT_QUERY_FORMAT_FLAG_ALL;
#endif
            }
        }
