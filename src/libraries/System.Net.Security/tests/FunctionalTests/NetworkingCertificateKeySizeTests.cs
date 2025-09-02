// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.X509Certificates.Tests.Common;
using Xunit;

namespace System.Net.Security.Tests
{
    public static class NetworkingCertificateKeySizeTests
    {
        [Fact]
        public static void NetworkingTests_UseAppropriateCertificateKeySize()
        {
            // Test that networking tests use larger key sizes for compatibility
            CertificateAuthority.BuildPrivatePki(
                PkiOptions.EndEntityRevocationViaOcsp,
                out RevocationResponder responder,
                out CertificateAuthority rootAuthority,
                out CertificateAuthority[] intermediateAuthorities,
                out X509Certificate2 endEntityCert,
                intermediateAuthorityCount: 1);

            try
            {
                // Test the end entity certificate
                using (RSA? eeRsa = endEntityCert.GetRSAPrivateKey())
                {
                    if (eeRsa != null)
                    {
                        // Networking tests should use larger key size for security/compatibility
                        Assert.Equal(2048, eeRsa.KeySize);
                    }
                }

                // Test the root authority certificate
                using (X509Certificate2 rootCert = rootAuthority.CloneIssuerCert())
                using (RSA? rootRsa = rootCert.GetRSAPrivateKey())
                {
                    if (rootRsa != null)
                    {
                        // Root certificate should also use larger key size
                        Assert.Equal(2048, rootRsa.KeySize);
                    }
                }
            }
            finally
            {
                // Clean up
                endEntityCert?.Dispose();
                responder?.Dispose();
                rootAuthority?.Dispose();
                foreach (var auth in intermediateAuthorities)
                {
                    auth?.Dispose();
                }
            }
        }
    }
}