// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Test.Common;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public abstract class SslClientAuthenticationOptionsTestBase
    {
        protected abstract bool TestAuthenticateAsync { get; }

        [Fact]
        public async Task ClientOptions_ServerOptions_NotMutatedDuringAuthentication()
        {
            using (X509Certificate2 clientCert = Configuration.Certificates.GetClientCertificate())
            using (X509Certificate2 serverCert = Configuration.Certificates.GetServerCertificate())
            {
                // Values used to populate client options
                bool clientAllowRenegotiation = false;
                List<SslApplicationProtocol> clientAppProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http11 };
                X509RevocationMode clientRevocation = X509RevocationMode.NoCheck;
                X509CertificateCollection clientCertificates = new X509CertificateCollection() { clientCert };
                SslProtocols clientSslProtocols = SslProtocols.Tls12;
                EncryptionPolicy clientEncryption = EncryptionPolicy.RequireEncryption;
                LocalCertificateSelectionCallback clientLocalCallback = new LocalCertificateSelectionCallback(delegate { return null; });
                RemoteCertificateValidationCallback clientRemoteCallback = new RemoteCertificateValidationCallback(delegate { return true; });
                string clientHost = serverCert.GetNameInfo(X509NameType.SimpleName, false);

                // Values used to populate server options
                bool serverAllowRenegotiation = true;
                List<SslApplicationProtocol> serverAppProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http11, SslApplicationProtocol.Http2 };
                X509RevocationMode serverRevocation = X509RevocationMode.NoCheck;
                bool serverCertRequired = false;
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
                SslProtocols serverSslProtocols = SslProtocols.Tls11 | SslProtocols.Tls12;
#pragma warning restore SYSLIB0039
#pragma warning disable SYSLIB0040 // NoEncryption and AllowNoEncryption are obsolete
                EncryptionPolicy serverEncryption = EncryptionPolicy.AllowNoEncryption;
#pragma warning restore SYSLIB0040
                RemoteCertificateValidationCallback serverRemoteCallback = new RemoteCertificateValidationCallback(delegate { return true; });
                SslStreamCertificateContext certificateContext = SslStreamCertificateContext.Create(serverCert, null, false);
                X509ChainPolicy policy = new X509ChainPolicy();

                (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
                using (var client = new SslStream(stream1))
                using (var server = new SslStream(stream2))
                {
                    // Create client options
                    var clientOptions = new SslClientAuthenticationOptions
                    {
                        AllowRenegotiation = clientAllowRenegotiation,
                        ApplicationProtocols = clientAppProtocols,
                        CertificateRevocationCheckMode = clientRevocation,
                        ClientCertificates = clientCertificates,
                        EnabledSslProtocols = clientSslProtocols,
                        EncryptionPolicy = clientEncryption,
                        LocalCertificateSelectionCallback = clientLocalCallback,
                        RemoteCertificateValidationCallback = clientRemoteCallback,
                        TargetHost = clientHost,
                        CertificateChainPolicy = policy,
                    };

                    // Create server options
                    var serverOptions = new SslServerAuthenticationOptions
                    {
                        AllowRenegotiation = serverAllowRenegotiation,
                        ApplicationProtocols = serverAppProtocols,
                        CertificateRevocationCheckMode = serverRevocation,
                        ClientCertificateRequired = serverCertRequired,
                        EnabledSslProtocols = serverSslProtocols,
                        EncryptionPolicy = serverEncryption,
                        RemoteCertificateValidationCallback = serverRemoteCallback,
                        ServerCertificate = serverCert,
                        ServerCertificateContext = certificateContext,
                        CertificateChainPolicy = policy,
                    };

                    // Authenticate
                    Task clientTask = client.AuthenticateAsClientAsync(TestAuthenticateAsync, clientOptions);
                    Task serverTask = server.AuthenticateAsServerAsync(TestAuthenticateAsync, serverOptions);
                    await new[] { clientTask, serverTask }.WhenAllOrAnyFailed();

                    // Validate that client options are unchanged
                    Assert.Equal(clientAllowRenegotiation, clientOptions.AllowRenegotiation);
                    Assert.Same(clientAppProtocols, clientOptions.ApplicationProtocols);
                    Assert.Equal(1, clientOptions.ApplicationProtocols.Count);
                    Assert.Equal(clientRevocation, clientOptions.CertificateRevocationCheckMode);
                    Assert.Same(clientCertificates, clientOptions.ClientCertificates);
                    Assert.Contains(clientCert, clientOptions.ClientCertificates.Cast<X509Certificate2>());
                    Assert.Equal(clientSslProtocols, clientOptions.EnabledSslProtocols);
                    Assert.Equal(clientEncryption, clientOptions.EncryptionPolicy);
                    Assert.Same(clientLocalCallback, clientOptions.LocalCertificateSelectionCallback);
                    Assert.Same(clientRemoteCallback, clientOptions.RemoteCertificateValidationCallback);
                    Assert.Same(clientHost, clientOptions.TargetHost);
                    Assert.Same(policy, clientOptions.CertificateChainPolicy);

                    // Validate that server options are unchanged
                    Assert.Equal(serverAllowRenegotiation, serverOptions.AllowRenegotiation);
                    Assert.Same(serverAppProtocols, serverOptions.ApplicationProtocols);
                    Assert.Equal(2, serverOptions.ApplicationProtocols.Count);
                    Assert.Equal(clientRevocation, serverOptions.CertificateRevocationCheckMode);
                    Assert.Equal(serverCertRequired, serverOptions.ClientCertificateRequired);
                    Assert.Equal(serverSslProtocols, serverOptions.EnabledSslProtocols);
                    Assert.Equal(serverEncryption, serverOptions.EncryptionPolicy);
                    Assert.Same(serverRemoteCallback, serverOptions.RemoteCertificateValidationCallback);
                    Assert.Same(serverCert, serverOptions.ServerCertificate);
                    Assert.Same(certificateContext, serverOptions.ServerCertificateContext);
                    Assert.Same(policy, serverOptions.CertificateChainPolicy);
                }
            }
        }

        [Fact]
        public async Task ClientOptions_TargetHostNull_OK()
        {
            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            using (client)
            using (server)
            {
                var serverOptions = new SslServerAuthenticationOptions() { ServerCertificate = Configuration.Certificates.GetServerCertificate() };
                var clientOptions = new SslClientAuthenticationOptions() { RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true };

                Assert.Null(clientOptions.TargetHost);

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                        client.AuthenticateAsClientAsync(clientOptions),
                        server.AuthenticateAsServerAsync(serverOptions));
               Assert.Equal(string.Empty, client.TargetHostName);
               Assert.Equal(string.Empty, server.TargetHostName);
            }
        }
    }

    public sealed class SslClientAuthenticationOptionsTestBase_Sync : SslClientAuthenticationOptionsTestBase
    {
        protected override bool TestAuthenticateAsync => false;
    }

    public sealed class SslClientAuthenticationOptionsTestBase_Async : SslClientAuthenticationOptionsTestBase
    {
        protected override bool TestAuthenticateAsync => true;
    }
}
