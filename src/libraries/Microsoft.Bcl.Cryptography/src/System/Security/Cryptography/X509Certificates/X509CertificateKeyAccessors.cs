// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace System.Security.Cryptography.X509Certificates
{
    /// <summary>
    /// Helper methods to access keys on <see cref="X509Certificate2"/>.
    /// </summary>
    public static class X509CertificateKeyAccessors
    {
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
    }
}
