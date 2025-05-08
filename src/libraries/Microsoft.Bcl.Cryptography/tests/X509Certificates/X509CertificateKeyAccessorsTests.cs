// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.SLHDsa.Tests;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static class X509CertificateKeyAccessorsTests
    {
        [ConditionalFact(typeof(SlhDsa), nameof(SlhDsa.IsSupported))]
        public static void GetPublicKey()
        {
#if NET10_0_OR_GREATER
            string certPem = PemEncoding.WriteString("CERTIFICATE", SlhDsaTestData.IetfSlhDsaSha2_128sCertificate);
            using (X509Certificate2 cert = X509Certificate2.CreateFromPem(certPem))
            using (SlhDsa? certKey = X509CertificateKeyAccessors.GetSlhDsaPublicKey(cert)) // Use extension method
            {
                Assert.NotNull(certKey);
                AssertExtensions.SequenceEqual(SlhDsaTestData.IetfSlhDsaSha2_128sPublicKeyValue, certKey.ExportSlhDsaPublicKey());
            }
#else
            // Unix: OpenSsl accessors not supported downlevel
            // Windows: SlhDsa currently not supported so test won't execute
            Assert.Throws<PlatformNotSupportedException>(() => X509CertificateKeyAccessors.GetSlhDsaPrivateKey(null));
#endif
        }

        [ConditionalFact(typeof(SlhDsa), nameof(SlhDsa.IsSupported))]
        public static void GetPrivateKey()
        {
#if NET10_0_OR_GREATER
            string certPem = PemEncoding.WriteString("CERTIFICATE", SlhDsaTestData.IetfSlhDsaSha2_128sCertificate);
            string keyPem = PemEncoding.WriteString("PRIVATE KEY", SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyPkcs8);
            using (X509Certificate2 cert = X509Certificate2.CreateFromPem(certPem, keyPem))
            using (SlhDsa? certKey = X509CertificateKeyAccessors.GetSlhDsaPrivateKey(cert)) // Use extension method
            {
                Assert.NotNull(certKey);
                AssertExtensions.SequenceEqual(SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyValue, certKey.ExportSlhDsaSecretKey());
            }
#else
            // Unix: OpenSsl accessors not supported downlevel
            // Windows: SlhDsa currently not supported so test won't execute
            Assert.Throws<PlatformNotSupportedException>(() => X509CertificateKeyAccessors.GetSlhDsaPrivateKey(null));
#endif
        }

        [ConditionalFact(typeof(SlhDsa), nameof(SlhDsa.IsSupported))]
        public static void CopyWithPrivateKey()
        {
#if NET10_0_OR_GREATER
            string certPem = PemEncoding.WriteString("CERTIFICATE", SlhDsaTestData.IetfSlhDsaSha2_128sCertificate);
            string keyPem = PemEncoding.WriteString("PRIVATE KEY", SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyPkcs8);
            using (X509Certificate2 cert = X509Certificate2.CreateFromPem(certPem))
            using (SlhDsa? privateKey = SlhDsa.ImportFromPem(keyPem))
            {
                AssertExtensions.FalseExpression(cert.HasPrivateKey);

                // Use extension method
                X509Certificate2 copied = X509CertificateKeyAccessors.CopyWithPrivateKey(cert, privateKey);
                AssertExtensions.TrueExpression(copied.HasPrivateKey);

                SlhDsa? copiedKey = copied.GetSlhDsaPrivateKey();
                Assert.NotNull(copiedKey);
                AssertExtensions.SequenceEqual(SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyValue, copiedKey.ExportSlhDsaSecretKey());
            }
#else
            // Unix: OpenSsl accessors not supported downlevel
            // Windows: SlhDsa currently not supported so test won't execute
            Assert.Throws<PlatformNotSupportedException>(() => X509CertificateKeyAccessors.GetSlhDsaPrivateKey(null));
#endif
        }
    }
}
