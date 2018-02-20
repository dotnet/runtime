// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Test.Common;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using System.Threading.Tasks;
using Xunit;
using System.Linq;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class SslClientAuthenticationOptionsTest
    {
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
                SslProtocols serverSslProtocols = SslProtocols.Tls11 | SslProtocols.Tls12;
                EncryptionPolicy serverEncryption = EncryptionPolicy.AllowNoEncryption;
                RemoteCertificateValidationCallback serverRemoteCallback = new RemoteCertificateValidationCallback(delegate { return true; });

                var network = new VirtualNetwork();
                using (var client = new SslStream(new VirtualNetworkStream(network, isServer: false)))
                using (var server = new SslStream(new VirtualNetworkStream(network, isServer: true)))
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
                        TargetHost = clientHost
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
                        ServerCertificate = serverCert
                    };

                    // Authenticate
                    Task clientTask = client.AuthenticateAsClientAsync(clientOptions, default);
                    Task serverTask = server.AuthenticateAsServerAsync(serverOptions, default);
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
                }
            }
        }
    }
}
