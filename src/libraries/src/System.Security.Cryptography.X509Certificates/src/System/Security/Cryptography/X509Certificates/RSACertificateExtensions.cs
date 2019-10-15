// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Cryptography;
using Internal.Cryptography.Pal;

namespace System.Security.Cryptography.X509Certificates
{
    /// <summary>
    /// Provides extension methods for retrieving <see cref="RSA" /> implementations for the
    /// public and private keys of a <see cref="X509Certificate2" />.
    /// </summary>
    public static class RSACertificateExtensions
    {
        /// <summary>
        /// Gets the <see cref="RSA" /> public key from the certificate or null if the certificate does not have an RSA public key.
        /// </summary>
        public static RSA GetRSAPublicKey(this X509Certificate2 certificate)
        {
            return certificate.GetPublicKey<RSA>();
        }

        /// <summary>
        /// Gets the <see cref="RSA" /> private key from the certificate or null if the certificate does not have an RSA private key.
        /// </summary>
        public static RSA GetRSAPrivateKey(this X509Certificate2 certificate)
        {
            return certificate.GetPrivateKey<RSA>();
        }

        public static X509Certificate2 CopyWithPrivateKey(this X509Certificate2 certificate, RSA privateKey)
        {
            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate));
            if (privateKey == null)
                throw new ArgumentNullException(nameof(privateKey));

            if (certificate.HasPrivateKey)
                throw new InvalidOperationException(SR.Cryptography_Cert_AlreadyHasPrivateKey);

            using (RSA publicKey = GetRSAPublicKey(certificate))
            {
                if (publicKey == null)
                    throw new ArgumentException(SR.Cryptography_PrivateKey_WrongAlgorithm);

                RSAParameters currentParameters = publicKey.ExportParameters(false);
                RSAParameters newParameters = privateKey.ExportParameters(false);

                if (!currentParameters.Modulus.ContentsEqual(newParameters.Modulus) ||
                    !currentParameters.Exponent.ContentsEqual(newParameters.Exponent))
                {
                    throw new ArgumentException(SR.Cryptography_PrivateKey_DoesNotMatch, nameof(privateKey));
                }
            }

            ICertificatePal pal = certificate.Pal.CopyWithPrivateKey(privateKey);
            return new X509Certificate2(pal);
        }
    }
}
