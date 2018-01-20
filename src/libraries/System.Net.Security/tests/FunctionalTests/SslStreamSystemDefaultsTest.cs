// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http;
using System.Net.Test.Common;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public abstract class SslStreamSystemDefaultTest
    {
        protected readonly SslStream _clientStream;
        protected readonly SslStream _serverStream;

        public SslStreamSystemDefaultTest()
        {
            var network = new VirtualNetwork();
            var clientNet = new VirtualNetworkStream(network, isServer:false);
            var serverNet = new VirtualNetworkStream(network, isServer: true);

            _clientStream = new SslStream(clientNet, false, ClientCertCallback);
            _serverStream = new SslStream(serverNet, false, ServerCertCallback);
        }

        public static bool IsNotWindows7 => !PlatformDetection.IsWindows7;

        protected abstract Task AuthenticateClientAsync(string targetHost, X509CertificateCollection clientCertificates, bool checkCertificateRevocation, SslProtocols? protocols = null);
        protected abstract Task AuthenticateServerAsync(X509Certificate serverCertificate, bool clientCertificateRequired, bool checkCertificateRevocation, SslProtocols? protocols = null);

        [ConditionalTheory(nameof(IsNotWindows7))]
        [InlineData(null, null)]
        [InlineData(SslProtocols.None, null)]
        [InlineData(null, SslProtocols.None)]
        [InlineData(SslProtocols.None, SslProtocols.None)]
        [InlineData(null, SslProtocols.Tls11)]
        [InlineData(SslProtocols.Tls11, null)]
        [InlineData(null, SslProtocols.Tls12)]
        [InlineData(SslProtocols.Tls12, null)]
        [InlineData(SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12, null)]
        [InlineData(null, SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12)]
        public async Task ClientAndServer_OneOrBothUseDefault_Ok(SslProtocols? clientProtocols, SslProtocols? serverProtocols)
        {
            const int SEC_E_BUFFER_TOO_SMALL = unchecked((int)0x80090321);

            X509Certificate2 serverCertificate = Configuration.Certificates.GetServerCertificate();
            string serverHost = serverCertificate.GetNameInfo(X509NameType.SimpleName, false);
            var clientCertificates = new X509CertificateCollection();
            clientCertificates.Add(Configuration.Certificates.GetClientCertificate());

            var tasks = new Task[2];
            tasks[0] = AuthenticateClientAsync(serverHost, clientCertificates, checkCertificateRevocation: false, protocols: clientProtocols);
            tasks[1] = AuthenticateServerAsync(serverCertificate, clientCertificateRequired: true, checkCertificateRevocation: false, protocols: serverProtocols);

            try
            {
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(tasks);
                if (PlatformDetection.IsWindows && PlatformDetection.WindowsVersion >= 10)
                {
                    Assert.True(
                        (_clientStream.SslProtocol == SslProtocols.Tls11 && _clientStream.HashAlgorithm == HashAlgorithmType.Sha1) ||
                        _clientStream.HashAlgorithm == HashAlgorithmType.Sha256 ||
                        _clientStream.HashAlgorithm == HashAlgorithmType.Sha384 ||
                        _clientStream.HashAlgorithm == HashAlgorithmType.Sha512);
                }
            }
            catch (HttpRequestException e) when (e.InnerException?.GetType().Name == "WinHttpException" &&
                e.InnerException.HResult == SEC_E_BUFFER_TOO_SMALL &&
                !PlatformDetection.IsWindows10Version1607OrGreater)
            {
                // Testing on old Windows versions can hit https://github.com/dotnet/corefx/issues/7812
                // Ignore SEC_E_BUFFER_TOO_SMALL error on such cases.
            }
        }

        private bool ClientCertCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
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

        private bool ServerCertCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            switch (sslPolicyErrors)
            {
                case SslPolicyErrors.None:
                case SslPolicyErrors.RemoteCertificateChainErrors:
                case SslPolicyErrors.RemoteCertificateNameMismatch:
                    return true;
                case SslPolicyErrors.RemoteCertificateNotAvailable:
                    // https://technet.microsoft.com/en-us/library/hh831771.aspx#BKMK_Changes2012R2
                    // Starting with Windows 8, the "Management of trusted issuers for client authentication" has changed:
                    // The behavior to send the Trusted Issuers List by default is off.
                    //
                    // In Windows 7 the Trusted Issuers List is sent within the Server Hello TLS record. This list is built
                    // by the server using certificates from the Trusted Root Authorities certificate store.
                    // The client side will use the Trusted Issuers List, if not empty, to filter proposed certificates.
                    return PlatformDetection.IsWindows7 && !Capability.IsTrustedRootCertificateInstalled();
                default:
                    return false;
            }
        }

        public void Dispose()
        {
            _clientStream?.Dispose();
            _serverStream?.Dispose();
        }
    }

    public sealed class SyncSslStreamSystemDefaultTest : SslStreamSystemDefaultTest
    {
        protected override Task AuthenticateClientAsync(string targetHost, X509CertificateCollection clientCertificates, bool checkCertificateRevocation, SslProtocols? protocols) =>
            Task.Run(() =>
            {
                if (protocols.HasValue)
                {
                    _clientStream.AuthenticateAsClient(targetHost, clientCertificates, protocols.Value, checkCertificateRevocation);
                }
                else
                {
                    _clientStream.AuthenticateAsClient(targetHost, clientCertificates, checkCertificateRevocation);
                }
            });

        protected override Task AuthenticateServerAsync(X509Certificate serverCertificate, bool clientCertificateRequired, bool checkCertificateRevocation, SslProtocols? protocols) =>
            Task.Run(() =>
            {
                if (protocols.HasValue)
                {
                    _serverStream.AuthenticateAsServer(serverCertificate, clientCertificateRequired, protocols.Value, checkCertificateRevocation);
                }
                else
                {
                    _serverStream.AuthenticateAsServer(serverCertificate, clientCertificateRequired, checkCertificateRevocation);
                }
            });
    }

    public sealed class ApmSslStreamSystemDefaultTest : SslStreamSystemDefaultTest
    {
        protected override Task AuthenticateClientAsync(string targetHost, X509CertificateCollection clientCertificates, bool checkCertificateRevocation, SslProtocols? protocols) =>
            Task.Factory.FromAsync(
                (callback, state) => protocols.HasValue ?
                    _clientStream.BeginAuthenticateAsClient(targetHost, clientCertificates, protocols.Value, checkCertificateRevocation, callback, state) :
                    _clientStream.BeginAuthenticateAsClient(targetHost, clientCertificates, checkCertificateRevocation, callback, state),
                _clientStream.EndAuthenticateAsClient,
                state: null);

        protected override Task AuthenticateServerAsync(X509Certificate serverCertificate, bool clientCertificateRequired, bool checkCertificateRevocation, SslProtocols? protocols) =>
            Task.Factory.FromAsync(
                (callback, state) => protocols.HasValue ?
                    _serverStream.BeginAuthenticateAsServer(serverCertificate, clientCertificateRequired, protocols.Value, checkCertificateRevocation, callback, state) :
                    _serverStream.BeginAuthenticateAsServer(serverCertificate, clientCertificateRequired, checkCertificateRevocation, callback, state),
                _serverStream.EndAuthenticateAsServer,
                state: null);
    }

    public sealed class AsyncSslStreamSystemDefaultTest : SslStreamSystemDefaultTest
    {
        protected override Task AuthenticateClientAsync(string targetHost, X509CertificateCollection clientCertificates, bool checkCertificateRevocation, SslProtocols? protocols) =>
            protocols.HasValue ?
            _clientStream.AuthenticateAsClientAsync(targetHost, clientCertificates, protocols.Value, checkCertificateRevocation) :
            _clientStream.AuthenticateAsClientAsync(targetHost, clientCertificates, checkCertificateRevocation);

        protected override Task AuthenticateServerAsync(X509Certificate serverCertificate, bool clientCertificateRequired, bool checkCertificateRevocation, SslProtocols? protocols) =>
            protocols.HasValue ?
            _serverStream.AuthenticateAsServerAsync(serverCertificate, clientCertificateRequired, protocols.Value, checkCertificateRevocation) :
            _serverStream.AuthenticateAsServerAsync(serverCertificate, clientCertificateRequired, checkCertificateRevocation);
    }
}
