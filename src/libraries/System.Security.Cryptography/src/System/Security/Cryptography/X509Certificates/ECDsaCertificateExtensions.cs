// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    /// <summary>
    /// Provides extension methods for retrieving <see cref="ECDsa" /> implementations for the
    /// public and private keys of a <see cref="X509Certificate2" />.
    /// </summary>
    public static class ECDsaCertificateExtensions
    {
        /// <summary>
        /// Gets the <see cref="ECDsa" /> public key from the certificate or null if the certificate does not have an ECDsa public key.
        /// </summary>
        public static ECDsa? GetECDsaPublicKey(this X509Certificate2 certificate)
        {
            return certificate.GetPublicKey<ECDsa>(cert => HasECDsaKeyUsage(cert));
        }

        /// <summary>
        /// Gets the <see cref="ECDsa" /> private key from the certificate or null if the certificate does not have an ECDsa private key.
        /// </summary>
        public static ECDsa? GetECDsaPrivateKey(this X509Certificate2 certificate)
        {
            return certificate.GetPrivateKey<ECDsa>(cert => HasECDsaKeyUsage(cert));
        }

        public static X509Certificate2 CopyWithPrivateKey(this X509Certificate2 certificate, ECDsa privateKey)
        {
            ArgumentNullException.ThrowIfNull(certificate);
            ArgumentNullException.ThrowIfNull(privateKey);

            if (certificate.HasPrivateKey)
                throw new InvalidOperationException(SR.Cryptography_Cert_AlreadyHasPrivateKey);

            using (ECDsa? publicKey = GetECDsaPublicKey(certificate))
            {
                if (publicKey == null)
                    throw new ArgumentException(SR.Cryptography_PrivateKey_WrongAlgorithm);

                if (!Helpers.AreSamePublicECParameters(publicKey.ExportParameters(false), privateKey.ExportParameters(false)))
                {
                    throw new ArgumentException(SR.Cryptography_PrivateKey_DoesNotMatch, nameof(privateKey));
                }
            }

            ICertificatePal pal = certificate.Pal.CopyWithPrivateKey(privateKey);
            return new X509Certificate2(pal);
        }

        private static bool HasECDsaKeyUsage(X509Certificate2 certificate)
        {
            foreach (X509Extension extension in certificate.Extensions)
            {
                if (extension.Oid!.Value == Oids.KeyUsage)
                {
                    X509KeyUsageExtension ext = (X509KeyUsageExtension)extension;

                    if ((ext.KeyUsages & X509KeyUsageFlags.KeyAgreement) == 0)
                    {
                        // If this does not have KeyAgreement flag present, it cannot be ECDH
                        // or ECMQV key as KeyAgreement is mandatory flag for ECDH or ECMQV (RFC 5480 Section 3).
                        //
                        // In that case, at this point, it is safe to assume it is ECDSA
                        return true;
                    }

                    // Even if KeyAgreement was specified, if any of the signature uses was
                    // specified then ECDSA is a valid usage.
                    const X509KeyUsageFlags ecdsaFlags =
                        X509KeyUsageFlags.DigitalSignature |
                        X509KeyUsageFlags.NonRepudiation |
                        X509KeyUsageFlags.KeyCertSign |
                        X509KeyUsageFlags.CrlSign;

                    return ((ext.KeyUsages & ecdsaFlags) != 0);
                }
            }

            // If the key usage extension is not present in the certificate it is
            // considered valid for all usages, so we can use it for ECDSA.
            return true;
        }
    }
}
