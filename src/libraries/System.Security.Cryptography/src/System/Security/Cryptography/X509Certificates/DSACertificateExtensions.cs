// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    /// <summary>
    /// Provides extension methods for retrieving <see cref="DSA" /> implementations for the
    /// public and private keys of a <see cref="X509Certificate2" />.
    /// </summary>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    public static class DSACertificateExtensions
    {
        /// <summary>
        /// Gets the <see cref="DSA" /> public key from the certificate or null if the certificate does not have a DSA public key.
        /// </summary>
        public static DSA? GetDSAPublicKey(this X509Certificate2 certificate)
        {
            return certificate.GetPublicKey<DSA>();
        }

        /// <summary>
        /// Gets the <see cref="DSA" /> private key from the certificate or null if the certificate does not have a DSA private key.
        /// </summary>
        public static DSA? GetDSAPrivateKey(this X509Certificate2 certificate)
        {
            return certificate.GetPrivateKey<DSA>();
        }

        public static X509Certificate2 CopyWithPrivateKey(this X509Certificate2 certificate, DSA privateKey)
        {
            ArgumentNullException.ThrowIfNull(certificate);
            ArgumentNullException.ThrowIfNull(privateKey);

            if (certificate.HasPrivateKey)
                throw new InvalidOperationException(SR.Cryptography_Cert_AlreadyHasPrivateKey);

            using (DSA? publicKey = GetDSAPublicKey(certificate))
            {
                if (publicKey == null)
                    throw new ArgumentException(SR.Cryptography_PrivateKey_WrongAlgorithm);

                DSAParameters currentParameters = publicKey.ExportParameters(false);
                DSAParameters newParameters = privateKey.ExportParameters(false);

                if (!currentParameters.G.ContentsEqual(newParameters.G) ||
                    !currentParameters.P.ContentsEqual(newParameters.P) ||
                    !currentParameters.Q.ContentsEqual(newParameters.Q) ||
                    !currentParameters.Y.ContentsEqual(newParameters.Y))
                {
                    throw new ArgumentException(SR.Cryptography_PrivateKey_DoesNotMatch, nameof(privateKey));
                }
            }

            ICertificatePal pal = certificate.Pal.CopyWithPrivateKey(privateKey);
            return new X509Certificate2(pal);
        }
    }
}
