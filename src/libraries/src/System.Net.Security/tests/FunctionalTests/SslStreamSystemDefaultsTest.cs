// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Test.Common;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class SslStreamSystemDefaultTest
    {
        [Fact]
        public async Task SslStream_DefaultTlsConfigurationSync_Ok()
        {
            using (var test = new SyncTest())
            {
                await test.RunTest();
            }
        }

        [Fact]
        public async Task SslStream_DefaultTlsConfigurationApm_Ok()
        {
            using (var test = new ApmTest())
            {
                await test.RunTest();
            }
        }

        [Fact]
        public async Task SslStream_DefaultTlsConfigurationAsync_Ok()
        {
            using (var test = new AsyncTest())
            {
                await test.RunTest();
            }
        }

        public abstract class TestBase : IDisposable
        {
            protected SslStream _clientStream;
            protected SslStream _serverStream;

            public TestBase()
            {
                var network = new VirtualNetwork();
                var clientNet = new VirtualNetworkStream(network, false);
                var serverNet = new VirtualNetworkStream(network, true);

                _clientStream = new SslStream(clientNet, false, AllowAnyServerCertificate);
                _serverStream = new SslStream(serverNet, false, AllowAnyServerCertificate);
            }

            public async Task RunTest()
            {
                X509Certificate2 serverCertificate = Configuration.Certificates.GetServerCertificate();
                string serverHost = serverCertificate.GetNameInfo(X509NameType.SimpleName, false);
                X509CertificateCollection clientCertificates = new X509CertificateCollection();
                clientCertificates.Add(Configuration.Certificates.GetClientCertificate());

                var tasks = new Task[2];
                tasks[0] = AuthenticateClient(serverHost, clientCertificates, checkCertificateRevocation: false);
                tasks[1] = AuthenticateServer(serverCertificate, clientCertificateRequired:true, checkCertificateRevocation:false);
                await Task.WhenAll(tasks);
                
                if (PlatformDetection.IsWindows && PlatformDetection.WindowsVersion >= 10)
                {
                    Assert.True(_clientStream.HashAlgorithm == HashAlgorithmType.Sha256 ||
                                _clientStream.HashAlgorithm == HashAlgorithmType.Sha384 ||
                                _clientStream.HashAlgorithm == HashAlgorithmType.Sha512);
                }
            }

            private bool AllowAnyServerCertificate(
                object sender,
                X509Certificate certificate,
                X509Chain chain,
                SslPolicyErrors sslPolicyErrors)
            {
                switch (sslPolicyErrors)
                {
                    case SslPolicyErrors.None:
                    case SslPolicyErrors.RemoteCertificateChainErrors:
                    case SslPolicyErrors.RemoteCertificateNameMismatch:
                        return true;
                    case SslPolicyErrors.RemoteCertificateNotAvailable:
                    default:
                        return false;
                }
            }

            protected abstract Task AuthenticateClient(string targetHost, X509CertificateCollection clientCertificates, bool checkCertificateRevocation);

            protected abstract Task AuthenticateServer(X509Certificate serverCertificate, bool clientCertificateRequired, bool checkCertificateRevocation);

            public void Dispose()
            {
                if (_clientStream != null)
                {
                    _clientStream.Dispose();
                }

                if (_serverStream != null)
                {
                    _serverStream.Dispose();
                }
            }
        }
        
        public class SyncTest : TestBase
        {
            protected override Task AuthenticateClient(string targetHost, X509CertificateCollection clientCertificates, bool checkCertificateRevocation)
            {
                return Task.Run( () => { _clientStream.AuthenticateAsClient(targetHost, clientCertificates, checkCertificateRevocation); });
            }

            protected override Task AuthenticateServer(X509Certificate serverCertificate, bool clientCertificateRequired, bool checkCertificateRevocation)
            {
                return Task.Run( () => { _serverStream.AuthenticateAsServer(serverCertificate, clientCertificateRequired, checkCertificateRevocation); });
            }
        }

        public class ApmTest : TestBase
        {
            protected override Task AuthenticateClient(string targetHost, X509CertificateCollection clientCertificates, bool checkCertificateRevocation)
            {
                return Task.Factory.FromAsync(
                    (callback, state) => _clientStream.BeginAuthenticateAsClient(targetHost, clientCertificates, checkCertificateRevocation, callback, state), 
                    _clientStream.EndAuthenticateAsClient, 
                    state:null);
            }

            protected override Task AuthenticateServer(X509Certificate serverCertificate, bool clientCertificateRequired, bool checkCertificateRevocation)
            {
                return Task.Factory.FromAsync(
                    (callback, state) => _serverStream.BeginAuthenticateAsServer(serverCertificate, clientCertificateRequired, checkCertificateRevocation, callback, state),
                    _serverStream.EndAuthenticateAsServer,
                    state:null);
            }
        }

        public class AsyncTest : TestBase
        {
            protected override Task AuthenticateClient(string targetHost, X509CertificateCollection clientCertificates, bool checkCertificateRevocation)
            {
                return _clientStream.AuthenticateAsClientAsync(targetHost, clientCertificates, checkCertificateRevocation);
            }

            protected override Task AuthenticateServer(X509Certificate serverCertificate, bool clientCertificateRequired, bool checkCertificateRevocation)
            {
                return _serverStream.AuthenticateAsServerAsync(serverCertificate, clientCertificateRequired, checkCertificateRevocation);
            }
        }
    }
}
