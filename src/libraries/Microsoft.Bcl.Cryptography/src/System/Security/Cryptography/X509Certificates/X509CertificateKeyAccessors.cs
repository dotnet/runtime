﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

#if !NET10_0_OR_GREATER
using System.Security.Cryptography.Asn1;
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
        [ExperimentalAttribute(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static MLKem? GetMLKemPublicKey(this X509Certificate2 certificate)
        {
#if NET10_0_OR_GREATER
            return certificate.GetMLKemPublicKey();
#else
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
        [ExperimentalAttribute(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
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
        [ExperimentalAttribute(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static X509Certificate2 CopyWithPrivateKey(this X509Certificate2 certificate, MLKem privateKey) =>
#if NET10_0_OR_GREATER
            certificate.CopyWithPrivateKey(privateKey);
#else
            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(MLKem)));
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
        [ExperimentalAttribute(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
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
        [ExperimentalAttribute(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
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
        [ExperimentalAttribute(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
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
    }
}
