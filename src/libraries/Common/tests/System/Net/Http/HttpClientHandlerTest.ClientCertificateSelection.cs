// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Net.Test.Common;
using System.Security.Cryptography.X509Certificates;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

#if WINHTTPHANDLER_TEST
    using HttpClientHandler = System.Net.Http.WinHttpClientHandler;
#endif

    /// <summary>
    /// Tests to validate client certificate selection logic, specifically Extended Key Usage (EKU) validation.
    /// These tests ensure that certificate selection respects EKU restrictions for client authentication.
    /// </summary>
    public abstract class HttpClientHandler_ClientCertificateSelection_Test : HttpClientHandlerTestBase
    {
        public HttpClientHandler_ClientCertificateSelection_Test(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void GetEligibleClientCertificate_WithClientAuthEKU_ReturnsFirstCert()
        {
            // Certificate with Client Authentication EKU should be selected
            using X509Certificate2 clientCert = Configuration.Certificates.GetClientCertificate();
            
            var certs = new X509Certificate2Collection { clientCert };
            X509Certificate2? selected = CertificateHelper.GetEligibleClientCertificate(certs);
            
            Assert.NotNull(selected);
            Assert.Equal(clientCert.Thumbprint, selected.Thumbprint);
        }

        [Fact]
        public void GetEligibleClientCertificate_WithNoEKU_ReturnsFirstCert()
        {
            // Certificate with no EKU extension (all usages permitted) should be selected
            using X509Certificate2 noEkuCert = Configuration.Certificates.GetNoEKUCertificate();
            
            var certs = new X509Certificate2Collection { noEkuCert };
            X509Certificate2? selected = CertificateHelper.GetEligibleClientCertificate(certs);
            
            Assert.NotNull(selected);
            Assert.Equal(noEkuCert.Thumbprint, selected.Thumbprint);
        }

        [Fact]
        public void GetEligibleClientCertificate_WithOnlyServerAuthEKU_ReturnsNull()
        {
            // Certificate with only Server Authentication EKU should NOT be selected
            using X509Certificate2 serverCert = Configuration.Certificates.GetServerCertificate();
            
            var certs = new X509Certificate2Collection { serverCert };
            X509Certificate2? selected = CertificateHelper.GetEligibleClientCertificate(certs);
            
            Assert.Null(selected);
        }

        [Fact]
        public void GetEligibleClientCertificate_MultipleC​erts_SkipsInvalidAndReturnsValid()
        {
            // When multiple certificates are available, invalid ones (server EKU only)
            // should be skipped and the first valid one should be returned
            using X509Certificate2 serverCert = Configuration.Certificates.GetServerCertificate();
            using X509Certificate2 clientCert = Configuration.Certificates.GetClientCertificate();
            using X509Certificate2 noEkuCert = Configuration.Certificates.GetNoEKUCertificate();
            
            var certs = new X509Certificate2Collection { serverCert, clientCert, noEkuCert };
            X509Certificate2? selected = CertificateHelper.GetEligibleClientCertificate(certs);
            
            Assert.NotNull(selected);
            // Should return clientCert, not serverCert (which is first but invalid)
            Assert.Equal(clientCert.Thumbprint, selected.Thumbprint);
        }

        [Fact]
        public void GetEligibleClientCertificate_OnlyInvalidCerts_ReturnsNull()
        {
            // When only invalid certificates (server EKU only) are available, should return null
            using X509Certificate2 serverCert1 = Configuration.Certificates.GetServerCertificate();
            using X509Certificate2 serverCert2 = Configuration.Certificates.GetServerCertificate();
            
            var certs = new X509Certificate2Collection { serverCert1, serverCert2 };
            X509Certificate2? selected = CertificateHelper.GetEligibleClientCertificate(certs);
            
            Assert.Null(selected);
        }

        [Fact]
        public void GetEligibleClientCertificate_EmptyCollection_ReturnsNull()
        {
            var certs = new X509Certificate2Collection();
            X509Certificate2? selected = CertificateHelper.GetEligibleClientCertificate(certs);
            
            Assert.Null(selected);
        }

        [Fact]
        public void GetEligibleClientCertificate_NullCollection_ReturnsNull()
        {
            X509Certificate2? selected = CertificateHelper.GetEligibleClientCertificate((X509Certificate2Collection?)null);
            
            Assert.Null(selected);
        }
    }
}
