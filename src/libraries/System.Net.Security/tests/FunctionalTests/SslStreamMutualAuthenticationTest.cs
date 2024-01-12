// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

using Xunit;
using System.Runtime.InteropServices;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class SslStreamMutualAuthenticationTest : IDisposable
    {
        private readonly X509Certificate2 _clientCertificate;
        private readonly X509Certificate2 _serverCertificate;
        private readonly X509Certificate2 _selfSignedCertificate;

        public SslStreamMutualAuthenticationTest()
        {
            _serverCertificate = Configuration.Certificates.GetServerCertificate();
            _clientCertificate = Configuration.Certificates.GetClientCertificate();
            _selfSignedCertificate = Configuration.Certificates.GetSelfSignedServerCertificate();
        }

        public void Dispose()
        {
            _serverCertificate.Dispose();
            _clientCertificate.Dispose();
        }

        public enum ClientCertSource
        {
            ClientCertificate,
            SelectionCallback,
        }

        public static TheoryData<ClientCertSource> CertSourceData()
        {
            TheoryData<ClientCertSource> data = new();

            foreach (var source in Enum.GetValues<ClientCertSource>())
            {
                data.Add(source);
            }

            return data;
        }


        public static TheoryData<bool, ClientCertSource> BoolAndCertSourceData()
        {
            TheoryData<bool, ClientCertSource> data = new();

            foreach (var source in Enum.GetValues<ClientCertSource>())
            {
                data.Add(true, source);
                data.Add(false, source);
            }

            return data;
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

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        [ClassData(typeof(SslProtocolSupport.SupportedSslProtocolsTestData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/65563", TestPlatforms.Android)]
        public async Task SslStream_CachedCredentials_IsMutuallyAuthenticatedCorrect(
           SslProtocols protocol)
        {
            var clientOptions = new SslClientAuthenticationOptions
            {
                ClientCertificates = new X509CertificateCollection() { _clientCertificate },
                EnabledSslProtocols = protocol,
                RemoteCertificateValidationCallback = delegate { return true; },
                TargetHost = Guid.NewGuid().ToString("N")
            };

            for (int i = 0; i < 5; i++)
            {
                (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
                using (client)
                using (server)
                {
                    bool expectMutualAuthentication = (i % 2) == 0;

                    var serverOptions = new SslServerAuthenticationOptions
                    {
                        ClientCertificateRequired = expectMutualAuthentication,
                        ServerCertificate = expectMutualAuthentication ? _serverCertificate : _selfSignedCertificate,
                        RemoteCertificateValidationCallback = delegate { return true; },
                        EnabledSslProtocols = protocol
                    };

                    await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                        client.AuthenticateAsClientAsync(clientOptions),
                        server.AuthenticateAsServerAsync(serverOptions));

                    // mutual authentication should only be set if server required client cert
                    Assert.Equal(expectMutualAuthentication, server.IsMutuallyAuthenticated);
                    Assert.Equal(expectMutualAuthentication, client.IsMutuallyAuthenticated);
                };
            }
        }

        [ConditionalTheory(typeof(TestConfiguration), nameof(TestConfiguration.SupportsRenegotiation))]
        [MemberData(nameof(CertSourceData))]
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.Linux)]
        public async Task SslStream_NegotiateClientCertificate_IsMutuallyAuthenticatedCorrect(ClientCertSource certSource)
        {
            SslStreamCertificateContext context = SslStreamCertificateContext.Create(_serverCertificate, null);
            var clientOptions = new SslClientAuthenticationOptions
            {
                TargetHost = Guid.NewGuid().ToString("N")
            };

            for (int round = 0; round < 3; round++)
            {
                (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
                using (var client = new SslStream(stream1, false, AllowAnyCertificate))
                using (var server = new SslStream(stream2, false, AllowAnyCertificate))
                {

                    switch (certSource)
                    {
                        case ClientCertSource.ClientCertificate:
                            clientOptions.ClientCertificates = new X509CertificateCollection() { _clientCertificate };
                            break;
                        case ClientCertSource.SelectionCallback:
                            clientOptions.LocalCertificateSelectionCallback = ClientCertSelectionCallback;
                            break;
                    }

                    Task t2 = client.AuthenticateAsClientAsync(clientOptions);
                    Task t1 = server.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                    {
                        ServerCertificateContext = context,
                        ClientCertificateRequired = false,
                        EnabledSslProtocols = SslProtocols.Tls12,

                    });

                    await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);

                    if (round >= 0 && server.RemoteCertificate != null)
                    {
                        // TLS resumed
                        Assert.True(client.IsMutuallyAuthenticated, "client.IsMutuallyAuthenticated");
                        Assert.True(server.IsMutuallyAuthenticated, "server.IsMutuallyAuthenticated");
                        continue;
                    }

                    Assert.False(client.IsMutuallyAuthenticated, "client.IsMutuallyAuthenticated");
                    Assert.False(server.IsMutuallyAuthenticated, "server.IsMutuallyAuthenticated");

                    var t = client.ReadAsync(new byte[1]);
                    await server.NegotiateClientCertificateAsync();
                    Assert.NotNull(server.RemoteCertificate);
                    await server.WriteAsync(new byte[1]);
                    await t;

                    Assert.NotNull(server.RemoteCertificate);
                    Assert.True(client.IsMutuallyAuthenticated, "client.IsMutuallyAuthenticated");
                    Assert.True(server.IsMutuallyAuthenticated, "server.IsMutuallyAuthenticated");
                }
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        [ClassData(typeof(SslProtocolSupport.SupportedSslProtocolsTestData))]
        public async Task SslStream_ResumedSessionsClientCollection_IsMutuallyAuthenticatedCorrect(
           SslProtocols protocol)
        {
            var clientOptions = new SslClientAuthenticationOptions
            {
                EnabledSslProtocols = protocol,
                RemoteCertificateValidationCallback = delegate { return true; },
                TargetHost = Guid.NewGuid().ToString("N")
            };

            // Create options with certificate context so TLS resume is possible on Linux
            var serverOptions = new SslServerAuthenticationOptions
            {
                ClientCertificateRequired = true,
                ServerCertificateContext = SslStreamCertificateContext.Create(_serverCertificate, null),
                RemoteCertificateValidationCallback = delegate { return true; },
                EnabledSslProtocols = protocol
            };

            for (int i = 0; i < 5; i++)
            {
                (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
                using (client)
                using (server)
                {
                    bool expectMutualAuthentication = (i % 2) == 0;

                    clientOptions.ClientCertificates = expectMutualAuthentication ? new X509CertificateCollection() { _clientCertificate } : null;
                    await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                        client.AuthenticateAsClientAsync(clientOptions),
                        server.AuthenticateAsServerAsync(serverOptions));

                    // mutual authentication should only be set if client set certificate
                    Assert.Equal(expectMutualAuthentication, server.IsMutuallyAuthenticated);
                    Assert.Equal(expectMutualAuthentication, client.IsMutuallyAuthenticated);

                    if (expectMutualAuthentication)
                    {
                        Assert.NotNull(server.RemoteCertificate);
                    }
                    else
                    {
                        Assert.Null(server.RemoteCertificate);
                    }
                };
            }
        }

        [ClassData(typeof(SslProtocolSupport.SupportedSslProtocolsTestData))]
        [PlatformSpecific(TestPlatforms.Linux)] // https://github.com/dotnet/runtime/issues/65563
        [Theory]
        public async Task SslStream_ResumedSessionsCallbackSet_IsMutuallyAuthenticatedCorrect(
           SslProtocols protocol)
        {
            var clientOptions = new SslClientAuthenticationOptions
            {
                EnabledSslProtocols = protocol,
                RemoteCertificateValidationCallback = delegate { return true; },
                TargetHost = Guid.NewGuid().ToString("N")
            };

            // Create options with certificate context so TLS resume is possible on Linux
            var serverOptions = new SslServerAuthenticationOptions
            {
                ClientCertificateRequired = true,
                ServerCertificateContext = SslStreamCertificateContext.Create(_serverCertificate, null),
                RemoteCertificateValidationCallback = delegate { return true; },
                EnabledSslProtocols = protocol
            };

            for (int i = 0; i < 5; i++)
            {
                (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
                using (client)
                using (server)
                {
                    bool expectMutualAuthentication = (i % 2) == 0;

                    clientOptions.LocalCertificateSelectionCallback = (s, t, l, r, a) =>
                    {
                        return expectMutualAuthentication ? _clientCertificate : null;
                    };

                    await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                        client.AuthenticateAsClientAsync(clientOptions),
                        server.AuthenticateAsServerAsync(serverOptions));

                    // mutual authentication should only be set if client set certificate
                    Assert.Equal(expectMutualAuthentication, server.IsMutuallyAuthenticated);
                    Assert.Equal(expectMutualAuthentication, client.IsMutuallyAuthenticated);

                    if (expectMutualAuthentication)
                    {
                        Assert.NotNull(server.RemoteCertificate);
                    }
                    else
                    {
                        Assert.Null(server.RemoteCertificate);
                    }
                };
            }
        }

        [ClassData(typeof(SslProtocolSupport.SupportedSslProtocolsTestData))]
        [PlatformSpecific(TestPlatforms.Linux)] // https://github.com/dotnet/runtime/issues/65563
        [Theory]
        public async Task SslStream_ResumedSessionsCallbackMaybeSet_IsMutuallyAuthenticatedCorrect(
           SslProtocols protocol)
        {
            var clientOptions = new SslClientAuthenticationOptions
            {
                EnabledSslProtocols = protocol,
                RemoteCertificateValidationCallback = delegate { return true; },
                TargetHost = Guid.NewGuid().ToString("N")
            };

            // Create options with certificate context so TLS resume is possible on Linux
            var serverOptions = new SslServerAuthenticationOptions
            {
                ClientCertificateRequired = true,
                ServerCertificateContext = SslStreamCertificateContext.Create(_serverCertificate, null),
                RemoteCertificateValidationCallback = delegate { return true; },
                EnabledSslProtocols = protocol
            };

            for (int i = 0; i < 5; i++)
            {
                (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
                using (client)
                using (server)
                {
                    bool expectMutualAuthentication = (i % 2) == 0;

                    if (expectMutualAuthentication)
                    {
                        clientOptions.LocalCertificateSelectionCallback = (s, t, l, r, a) => _clientCertificate;
                    }
                    else
                    {
                        clientOptions.LocalCertificateSelectionCallback = null;
                    }

                    await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                        client.AuthenticateAsClientAsync(clientOptions),
                        server.AuthenticateAsServerAsync(serverOptions));

                    // mutual authentication should only be set if client set certificate
                    Assert.Equal(expectMutualAuthentication, server.IsMutuallyAuthenticated);
                    Assert.Equal(expectMutualAuthentication, client.IsMutuallyAuthenticated);

                    if (expectMutualAuthentication)
                    {
                        Assert.NotNull(server.RemoteCertificate);
                    }
                    else
                    {
                        Assert.Null(server.RemoteCertificate);
                    }
                };
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
