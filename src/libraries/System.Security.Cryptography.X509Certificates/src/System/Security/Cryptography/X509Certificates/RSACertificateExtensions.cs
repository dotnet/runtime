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
        [SecuritySafeCritical]
        public static RSA GetRSAPublicKey(this X509Certificate2 certificate)
        {
            return certificate.GetPublicKey<RSA>();
        }

        /// <summary>
        /// Gets the <see cref="RSA" /> private key from the certificate or null if the certificate does not have an RSA private key.
        /// </summary>
        [SecuritySafeCritical]
        public static RSA GetRSAPrivateKey(this X509Certificate2 certificate)
        {
            return certificate.GetPrivateKey<RSA>();
        }
    }
}
