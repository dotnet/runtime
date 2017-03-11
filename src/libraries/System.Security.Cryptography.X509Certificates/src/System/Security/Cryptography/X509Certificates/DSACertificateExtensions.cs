// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Cryptography.Pal;

namespace System.Security.Cryptography.X509Certificates
{
    /// <summary>
    /// Provides extension methods for retrieving <see cref="DSA" /> implementations for the
    /// public and private keys of a <see cref="X509Certificate2" />.
    /// </summary>
    public static class DSACertificateExtensions
    {
        /// <summary>
        /// Gets the <see cref="DSA" /> public key from the certificate or null if the certificate does not have a DSA public key.
        /// </summary>
        public static DSA GetDSAPublicKey(this X509Certificate2 certificate)
        {
            return certificate.GetPublicKey<DSA>();
        }

        /// <summary>
        /// Gets the <see cref="DSA" /> private key from the certificate or null if the certificate does not have a DSA private key.
        /// </summary>
        public static DSA GetDSAPrivateKey(this X509Certificate2 certificate)
        {
            return certificate.GetPrivateKey<DSA>();
        }
    }
}
