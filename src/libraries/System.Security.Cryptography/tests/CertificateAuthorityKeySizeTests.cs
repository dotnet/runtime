// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.X509Certificates.Tests.Common;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static class CertificateAuthorityKeySizeTests
    {
        [Fact]
        public static void CertificateAuthority_UsesAppropriateKeySize()
        {
            // Test that CertificateAuthority uses the expected key size based on preprocessor directive
            CertificateAuthority.BuildPrivatePki(
                PkiOptions.EndEntityRevocationViaOcsp,
                out RevocationResponder responder,
                out CertificateAuthority rootAuthority,
                out CertificateAuthority[] intermediateAuthorities,
                out X509Certificate2 endEntityCert,
                intermediateAuthorityCount: 1);

            try
            {
                // Test the root authority certificate
                using (X509Certificate2 rootCert = rootAuthority.CloneIssuerCert())
                using (RSA? rootRsa = rootCert.GetRSAPrivateKey())
                {
                    if (rootRsa != null)
                    {
#if CRYPTO_TESTS
                        // In crypto tests, expect smaller key size for speed
                        Assert.Equal(1024, rootRsa.KeySize);
#else
                        // In networking tests, expect larger key size for security
                        Assert.Equal(2048, rootRsa.KeySize);
#endif
                    }
                }

                // Test the end entity certificate
                using (RSA? eeRsa = endEntityCert.GetRSAPrivateKey())
                {
                    if (eeRsa != null)
                    {
#if CRYPTO_TESTS
                        // In crypto tests, expect smaller key size for speed
                        Assert.Equal(1024, eeRsa.KeySize);
#else
                        // In networking tests, expect larger key size for security
                        Assert.Equal(2048, eeRsa.KeySize);
#endif
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

        [Fact]
        public static void CertificateAuthority_WithRSAKeyFactory_UsesSpecifiedKeySize()
        {
            // Test that when an explicit RSA key factory is provided, it uses that size regardless of preprocessor directive
            var customKeyFactory = CertificateAuthority.KeyFactory.RSASize(2048);
            
            CertificateAuthority.BuildPrivatePki(
                PkiOptions.EndEntityRevocationViaOcsp,
                out RevocationResponder responder,
                out CertificateAuthority rootAuthority,
                out CertificateAuthority[] intermediateAuthorities,
                out X509Certificate2 endEntityCert,
                intermediateAuthorityCount: 1,
                keyFactory: customKeyFactory);

            try
            {
                // Regardless of preprocessor directive, explicit key factory should be used
                using (RSA? eeRsa = endEntityCert.GetRSAPrivateKey())
                {
                    if (eeRsa != null)
                    {
                        Assert.Equal(2048, eeRsa.KeySize);
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