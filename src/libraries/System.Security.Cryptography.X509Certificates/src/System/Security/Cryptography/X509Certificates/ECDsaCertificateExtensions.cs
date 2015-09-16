// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Internal.Cryptography;
using Internal.Cryptography.Pal;

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
        [SecuritySafeCritical]
        public static ECDsa GetECDsaPublicKey(this X509Certificate2 certificate)
        {
            return certificate.GetPublicKey<ECDsa>();
        }

        /// <summary>
        /// Gets the <see cref="ECDsa" /> private key from the certificate or null if the certificate does not have an ECDsa private key.
        /// </summary>
        [SecuritySafeCritical]
        public static ECDsa GetECDsaPrivateKey(this X509Certificate2 certificate)
        {
            return certificate.GetPrivateKey<ECDsa>();
        }
    }
}
