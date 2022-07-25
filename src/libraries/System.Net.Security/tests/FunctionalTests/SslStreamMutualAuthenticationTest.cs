// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using System.Net.Test.Common;
using System.Security.Cryptography.X509Certificates;

using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class SslStreamMutualAuthenticationTest : IDisposable
    {
        private readonly X509Certificate2 _clientCertificate;
        private readonly X509Certificate2 _serverCertificate;

        public SslStreamMutualAuthenticationTest()
        {
            _serverCertificate = Configuration.Certificates.GetServerCertificate();
            _clientCertificate = Configuration.Certificates.GetClientCertificate();
        }

        public void Dispose()
        {
            _serverCertificate.Dispose();
            _clientCertificate.Dispose();
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task SslStream_RequireClientCert_IsMutuallyAuthenticated_ReturnsTrue(bool clientCertificateRequired, bool useClientSelectionCallback)
        {
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (var client = new SslStream(stream1, false, AllowAnyCertificate))
            using (var server = new SslStream(stream2, false, AllowAnyCertificate))
            {
                Task t2 = client.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    ClientCertificates = useClientSelectionCallback ? null : new X509CertificateCollection() { _clientCertificate },
                    LocalCertificateSelectionCallback = useClientSelectionCallback ? ClientCertSelectionCallback : null,
                    TargetHost = Guid.NewGuid().ToString("N")
                });
                Task t1 = server.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                {
                    ServerCertificate = _serverCertificate,
                    ClientCertificateRequired = clientCertificateRequired
                });

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);

                if (Capability.IsTrustedRootCertificateInstalled())
                {
                    // https://technet.microsoft.com/en-us/library/hh831771.aspx#BKMK_Changes2012R2
                    // Starting with Windows 8, the "Management of trusted issuers for client authentication" has changed:
                    // The behavior to send the Trusted Issuers List by default is off.
                    //
                    // In Windows 7 the Trusted Issuers List is sent within the Server Hello TLS record. This list is built
                    // by the server using certificates from the Trusted Root Authorities certificate store.
                    // The client side will use the Trusted Issuers List, if not empty, to filter proposed certificates.

                    if (clientCertificateRequired)
                    {
                        Assert.True(client.IsMutuallyAuthenticated, "client.IsMutuallyAuthenticated");
                        Assert.True(server.IsMutuallyAuthenticated, "server.IsMutuallyAuthenticated");
                    }
                    else
                    {
                        // Even though the certificate was provided, it was not requested by the server and thus the client
                        // was not authenticated.
                        Assert.False(client.IsMutuallyAuthenticated, "client.IsMutuallyAuthenticated");
                        Assert.False(server.IsMutuallyAuthenticated, "server.IsMutuallyAuthenticated");
                    }
                }
            }
        }

        private static bool AllowAnyCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
        private X509Certificate ClientCertSelectionCallback(
            object sender,
            string targetHost,
            X509CertificateCollection localCertificates,
            X509Certificate remoteCertificate,
            string[] acceptableIssuers)
        {
            return _clientCertificate;
        }
    }
}
