// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
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
            (Stream clientNet, Stream serverNet) = TestHelper.GetConnectedTcpStreams();
            _clientStream = new SslStream(clientNet, false, ClientCertCallback);
            _serverStream = new SslStream(serverNet, false, ServerCertCallback);
        }

        public static bool IsNotWindows7 => !PlatformDetection.IsWindows7;

        protected abstract Task AuthenticateClientAsync(string targetHost, X509CertificateCollection clientCertificates, bool checkCertificateRevocation, SslProtocols? protocols = null);
        protected abstract Task AuthenticateServerAsync(X509Certificate serverCertificate, bool clientCertificateRequired, bool checkCertificateRevocation, SslProtocols? protocols = null);

        public static IEnumerable<object[]> OneOrBothUseDefaulData()
        {
            yield return new object[] { null, null };
            yield return new object[] { SslProtocols.None, null };
            yield return new object[] { null, SslProtocols.None };
            yield return new object[] { SslProtocols.None, SslProtocols.None };
#pragma warning disable 0618
            if (PlatformDetection.SupportsTls10)
            {
                // Default only has Ssl3 and Tls1.0 for legacy reasons.
                yield return new object[] { SslProtocols.Default, SslProtocolSupport.NonTls13Protocols };
                yield return new object[] { SslProtocolSupport.NonTls13Protocols, SslProtocols.Default };
            }

#pragma warning restore 0618
            if (PlatformDetection.SupportsTls11)
            {
                yield return new object[] { SslProtocolSupport.NonTls13Protocols, SslProtocols.Tls11 };
                yield return new object[] { SslProtocols.Tls11, SslProtocolSupport.NonTls13Protocols };
            }

            if (PlatformDetection.SupportsTls12)
            {
                yield return new object[] { null, SslProtocols.Tls12 };
                yield return new object[] { SslProtocols.Tls12, null };
            }

            if (PlatformDetection.SupportsTls13)
            {
                yield return new object[] { null, SslProtocols.Tls13 };
                yield return new object[] { SslProtocols.Tls13, null };
            }

            if ((SslProtocolSupport.SupportedSslProtocols & SslProtocolSupport.NonTls13Protocols) != 0)
            {
                yield return new object[] { SslProtocolSupport.NonTls13Protocols, null };
                yield return new object[] { null, SslProtocolSupport.NonTls13Protocols };
            }
        }

        [ConditionalTheory]
        [MemberData(nameof(OneOrBothUseDefaulData))]
        public async Task ClientAndServer_OneOrBothUseDefault_Ok(SslProtocols? clientProtocols, SslProtocols? serverProtocols)
        {
            using (X509Certificate2 serverCertificate = Configuration.Certificates.GetServerCertificate())
            using (X509Certificate2 clientCertificate = Configuration.Certificates.GetClientCertificate())
            {
                // Use a different SNI for each connection to prevent TLS 1.3 renegotiation issue: https://github.com/dotnet/runtime/issues/47378
                string serverHost = TestHelper.GetTestSNIName(nameof(ClientAndServer_OneOrBothUseDefault_Ok), clientProtocols, serverProtocols);
                var clientCertificates = new X509CertificateCollection() { clientCertificate };

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    AuthenticateClientAsync(serverHost, clientCertificates, checkCertificateRevocation: false, protocols: clientProtocols),
                    AuthenticateServerAsync(serverCertificate, clientCertificateRequired: true, checkCertificateRevocation: false, protocols: serverProtocols));
                if (PlatformDetection.IsWindows && PlatformDetection.WindowsVersion >= 10 &&
#pragma warning disable 0618
                    clientProtocols.GetValueOrDefault() != SslProtocols.Default &&
                    serverProtocols.GetValueOrDefault() != SslProtocols.Default)
#pragma warning restore 0618
                {
                    Assert.True(
                        (_clientStream.SslProtocol == SslProtocols.Tls11 && _clientStream.HashAlgorithm == HashAlgorithmType.Sha1) ||
                        _clientStream.HashAlgorithm == HashAlgorithmType.Sha256 ||
                        _clientStream.HashAlgorithm == HashAlgorithmType.Sha384 ||
                        _clientStream.HashAlgorithm == HashAlgorithmType.Sha512,
                        _clientStream.SslProtocol + " " + _clientStream.HashAlgorithm);
                }
            }
        }

        [ConditionalTheory(nameof(IsNotWindows7))]
#pragma warning disable 0618
        [InlineData(null, SslProtocols.Ssl2)]
        [InlineData(SslProtocols.None, SslProtocols.Ssl2)]
        [InlineData(SslProtocols.Ssl2, null)]
        [InlineData(SslProtocols.Ssl2, SslProtocols.None)]
#pragma warning restore 0618
        public async Task ClientAndServer_OneUsesDefault_OtherUsesLowerProtocol_Fails(
            SslProtocols? clientProtocols, SslProtocols? serverProtocols)
        {
            using (X509Certificate2 serverCertificate = Configuration.Certificates.GetServerCertificate())
            using (X509Certificate2 clientCertificate = Configuration.Certificates.GetClientCertificate())
            {
                string serverHost = serverCertificate.GetNameInfo(X509NameType.SimpleName, false);
                var clientCertificates = new X509CertificateCollection() { clientCertificate };

                await Assert.ThrowsAnyAsync<Exception>(() => TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    AuthenticateClientAsync(serverHost, clientCertificates, checkCertificateRevocation: false, protocols: clientProtocols),
                    AuthenticateServerAsync(serverCertificate, clientCertificateRequired: true, checkCertificateRevocation: false, protocols: serverProtocols)));
            }
        }

        private bool ClientCertCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            switch (sslPolicyErrors)
            {
                case SslPolicyErrors.None:
                case SslPolicyErrors.RemoteCertificateChainErrors:
                case SslPolicyErrors.RemoteCertificateNameMismatch:
                case SslPolicyErrors.RemoteCertificateChainErrors | SslPolicyErrors.RemoteCertificateNameMismatch:
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
