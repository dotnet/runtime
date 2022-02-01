// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class ServerAsyncAuthenticateTest : IDisposable
    {
        private readonly ITestOutputHelper _log;
        private readonly ITestOutputHelper _logVerbose;
        private readonly X509Certificate2 _serverCertificate;

        public ServerAsyncAuthenticateTest(ITestOutputHelper output)
        {
            _log = output;
            _logVerbose = VerboseTestLogging.GetInstance();
            _serverCertificate = Configuration.Certificates.GetServerCertificate();
        }

        public void Dispose()
        {
            _serverCertificate.Dispose();
        }

        [Theory]
        [ClassData(typeof(SslProtocolSupport.SupportedSslProtocolsTestData))]
        public async Task ServerAsyncAuthenticate_EachSupportedProtocol_Success(SslProtocols protocol)
        {
            await ServerAsyncSslHelper(protocol, protocol);
        }

        [Theory]
        [MemberData(nameof(ProtocolMismatchData))]
        public async Task ServerAsyncAuthenticate_MismatchProtocols_Fails(
            SslProtocols serverProtocol,
            SslProtocols clientProtocol,
            Type expectedException)
        {
            Exception e = await Record.ExceptionAsync(
                () =>
                {
                    return ServerAsyncSslHelper(
                        clientProtocol,
                        serverProtocol,
                        expectedToFail: true);
                });

            Assert.NotNull(e);
            Assert.IsAssignableFrom(expectedException, e);
        }

        [Theory]
        [ClassData(typeof(SslProtocolSupport.SupportedSslProtocolsTestData))]
        public async Task ServerAsyncAuthenticate_AllClientVsIndividualServerSupportedProtocols_Success(
            SslProtocols serverProtocol)
        {
            await ServerAsyncSslHelper(SslProtocolSupport.SupportedSslProtocols, serverProtocol);
        }

        [Fact]
        public async Task ServerAsyncAuthenticate_SimpleSniOptions_Success()
        {
            var state = new object();
            var serverOptions = new SslServerAuthenticationOptions() { ServerCertificate = _serverCertificate };
            var clientOptions = new SslClientAuthenticationOptions() { TargetHost = _serverCertificate.GetNameInfo(X509NameType.SimpleName, false) };
            clientOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            using (client)
            using (server)
            {
                Task t1 = client.AuthenticateAsClientAsync(clientOptions, CancellationToken.None);
                Task t2 = server.AuthenticateAsServerAsync(
                    (stream, clientHelloInfo, userState, cancellationToken) =>
                    {
                        Assert.Equal(server, stream);
                        Assert.Equal(clientOptions.TargetHost, clientHelloInfo.ServerName);
                        Assert.True(object.ReferenceEquals(state, userState));
                        return new ValueTask<SslServerAuthenticationOptions>(serverOptions);
                    },
                    state, CancellationToken.None);

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);
            }
        }

        [Theory]
        [MemberData(nameof(SupportedProtocolData))]
        public async Task ServerAsyncAuthenticate_SniSetVersion_Success(SslProtocols version)
        {
            var serverOptions = new SslServerAuthenticationOptions() { ServerCertificate = _serverCertificate, EnabledSslProtocols = version };
            var clientOptions = new SslClientAuthenticationOptions() { TargetHost = _serverCertificate.GetNameInfo(X509NameType.SimpleName, forIssuer: false), EnabledSslProtocols = SslProtocols.Tls11 | SslProtocols.Tls12 };
            clientOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            using (client)
            using (server)
            {
                Task t1 = client.AuthenticateAsClientAsync(clientOptions, CancellationToken.None);
                Task t2 = server.AuthenticateAsServerAsync(
                    (stream, clientHelloInfo, userState, cancellationToken) =>
                    {
                        Assert.Equal(server, stream);
                        Assert.Equal(clientOptions.TargetHost, clientHelloInfo.ServerName);
                        return new ValueTask<SslServerAuthenticationOptions>(serverOptions);
                    },
                    null, CancellationToken.None);

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);
                // Verify that the SNI callback can impact version.
                Assert.Equal(version, client.SslProtocol);
            }
        }

        private async Task<SslServerAuthenticationOptions> FailedTask()
        {
            await Task.Yield();
            throw new InvalidOperationException("foo");
        }

        private async Task<SslServerAuthenticationOptions> OptionsTask(SslServerAuthenticationOptions value)
        {
            await Task.Yield();
            return value;
        }

        [Fact]
        public async Task ServerAsyncAuthenticate_AsyncOptions_Success()
        {
            var state = new object();
            var serverOptions = new SslServerAuthenticationOptions() { ServerCertificate = _serverCertificate };
            var clientOptions = new SslClientAuthenticationOptions() { TargetHost = _serverCertificate.GetNameInfo(X509NameType.SimpleName, false) };
            clientOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            using (client)
            using (server)
            {
                Task t1 = client.AuthenticateAsClientAsync(clientOptions, CancellationToken.None);
                Task t2 = server.AuthenticateAsServerAsync(
                    (stream, clientHelloInfo, userState, cancellationToken) =>
                    {
                        Assert.Equal(server, stream);
                        Assert.Equal(clientOptions.TargetHost, clientHelloInfo.ServerName);
                        Assert.True(object.ReferenceEquals(state, userState));
                        return new ValueTask<SslServerAuthenticationOptions>(OptionsTask(serverOptions));
                    },
                    state, CancellationToken.None);

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ServerAsyncAuthenticate_FailingOptionCallback_Throws(bool useAsync)
        {
            var serverOptions = new SslServerAuthenticationOptions() { ServerCertificate = _serverCertificate };
            var clientOptions = new SslClientAuthenticationOptions() { TargetHost = _serverCertificate.GetNameInfo(X509NameType.SimpleName, false) };
            clientOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            using (client)
            using (server)
            {
                Task t1 = client.AuthenticateAsClientAsync(clientOptions, CancellationToken.None);
                Task t2 = server.AuthenticateAsServerAsync(
                    (stream, clientHelloInfo, userState, cancellationToken) =>
                    {
                        if (useAsync)
                        {
                            return new ValueTask<SslServerAuthenticationOptions>(FailedTask());
                        }

                        throw new InvalidOperationException("foo");
                    },
                    null, CancellationToken.None);
                await Assert.ThrowsAsync<InvalidOperationException>(() => t2);
            }
        }

        [Fact]
        public async Task ServerAsyncAuthenticate_VerificationDelegate_Success()
        {
            bool validationCallbackCalled = false;
            var serverOptions = new SslServerAuthenticationOptions() { ServerCertificate = _serverCertificate, ClientCertificateRequired = true, };
            var clientOptions = new SslClientAuthenticationOptions() { TargetHost = _serverCertificate.GetNameInfo(X509NameType.SimpleName, false) };
            clientOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            serverOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
            {
                validationCallbackCalled = true;
                return true;
            };

            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            using (client)
            using (server)
            {
                Task t1 = client.AuthenticateAsClientAsync(clientOptions, CancellationToken.None);
                Task t2 = server.AuthenticateAsServerAsync(
                    (stream, clientHelloInfo, userState, cancellationToken) =>
                    {
                        Assert.Equal(server, stream);
                        Assert.Equal(clientOptions.TargetHost, clientHelloInfo.ServerName);
                        return new ValueTask<SslServerAuthenticationOptions>(OptionsTask(serverOptions));
                    },
                    null, CancellationToken.None);

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);
                Assert.True(validationCallbackCalled);
            }
        }

        [Fact]
        public async Task ServerAsyncAuthenticate_ConstructorVerificationDelegate_Success()
        {
            bool validationCallbackCalled = false;
            var serverOptions = new SslServerAuthenticationOptions() { ServerCertificate = _serverCertificate, ClientCertificateRequired = true, };
            var clientOptions = new SslClientAuthenticationOptions() { TargetHost = _serverCertificate.GetNameInfo(X509NameType.SimpleName, false) };
            clientOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            var client = new SslStream(clientStream);
            var server = new SslStream(serverStream, false, (sender, certificate, chain, sslPolicyErrors) => { validationCallbackCalled = true; return true;});

            using (client)
            using (server)
            {
                Task t1 = client.AuthenticateAsClientAsync(clientOptions, CancellationToken.None);
                Task t2 = server.AuthenticateAsServerAsync(
                    (stream, clientHelloInfo, userState, cancellationToken) =>
                    {
                        Assert.Equal(server, stream);
                        Assert.Equal(clientOptions.TargetHost, clientHelloInfo.ServerName);
                        return new ValueTask<SslServerAuthenticationOptions>(OptionsTask(serverOptions));
                    },
                    null, CancellationToken.None);

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);
                Assert.True(validationCallbackCalled);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ServerAsyncAuthenticate_NoCertificate_Throws(bool useAsync)
        {
            var serverOptions = new SslServerAuthenticationOptions();
            var clientOptions = new SslClientAuthenticationOptions() { TargetHost = _serverCertificate.GetNameInfo(X509NameType.SimpleName, false) };
            clientOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            using (client)
            using (server)
            {
                Task t1 = client.AuthenticateAsClientAsync(clientOptions, CancellationToken.None);
                Task t2 = server.AuthenticateAsServerAsync(
                    (stream, clientHelloInfo, userState, cancellationToken) =>
                    {
                        if (useAsync)
                        {
                            return new ValueTask<SslServerAuthenticationOptions>(serverOptions);
                        }

                        return new ValueTask<SslServerAuthenticationOptions>(OptionsTask(serverOptions));
                    },
                    null, CancellationToken.None);
                await Assert.ThrowsAsync<System.NotSupportedException>(() => t2);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ServerAsyncAuthenticate_InvalidHello_Throws(bool close)
        {
            (NetworkStream client, NetworkStream server) = TestHelper.GetConnectedTcpStreams();
            using (client)
            using (SslStream ssl = new SslStream(server))
            {
                byte[] buffer = new byte[182];
                buffer[0] = 178;
                buffer[1] = 0;
                buffer[2] = 0;
                buffer[3] = 1;
                buffer[4] = 133;
                buffer[5] = 166;

                Task t1 = ssl.AuthenticateAsServerAsync(_serverCertificate, false, false);
                Task t2 = client.WriteAsync(buffer).AsTask();
                if (close)
                {
                    await t2.WaitAsync(TestConfiguration.PassingTestTimeout);
                    client.Socket.Shutdown(SocketShutdown.Send);
                }
                else
                {
                    // Write enough data to full frame size
                    buffer = new byte[13000];
                    t2 = client.WriteAsync(buffer).AsTask();
                    await t2.WaitAsync(TestConfiguration.PassingTestTimeout);
                }

                if (close)
                {
                    await Assert.ThrowsAsync<IOException>(() => t1);
                }
                else
                {
                    await Assert.ThrowsAsync<AuthenticationException>(() => t1);
                }
            }
        }

        public static IEnumerable<object[]> ProtocolMismatchData()
        {
            if (PlatformDetection.SupportsSsl3)
            {
#pragma warning disable 0618
                yield return new object[] { SslProtocols.Ssl3, SslProtocols.Tls12, typeof(Exception) };
                if (PlatformDetection.SupportsSsl2)
                {
                    yield return new object[] { SslProtocols.Ssl2, SslProtocols.Ssl3, typeof(Exception) };
                    yield return new object[] { SslProtocols.Ssl2, SslProtocols.Tls12, typeof(Exception) };
                }
#pragma warning restore 0618
            }

            // It is OK if server does not support given protocol. It should still fail.
            // But if client does not support it, it will simply fail without sending out any data.

            if (PlatformDetection.SupportsTls10)
            {
                yield return new object[] { SslProtocols.Tls11, SslProtocols.Tls, typeof(AuthenticationException) };
                yield return new object[] { SslProtocols.Tls12, SslProtocols.Tls, typeof(AuthenticationException) };
            }

            if (PlatformDetection.SupportsTls11)
            {
                yield return new object[] { SslProtocols.Tls, SslProtocols.Tls11, typeof(AuthenticationException) };
                yield return new object[] { SslProtocols.Tls12, SslProtocols.Tls11, typeof(AuthenticationException) };
            }

            if (PlatformDetection.SupportsTls12)
            {
                yield return new object[] { SslProtocols.Tls, SslProtocols.Tls12, typeof(AuthenticationException) };
                yield return new object[] { SslProtocols.Tls11, SslProtocols.Tls12, typeof(AuthenticationException) };
            }
        }

        public static IEnumerable<Object[]> SupportedProtocolData()
        {
            if (PlatformDetection.SupportsTls11)
            {
                yield return new object[] { SslProtocols.Tls11 };
            }

            if (PlatformDetection.SupportsTls12)
            {
                yield return new object[] { SslProtocols.Tls12 };
            }
        }
        #region Helpers

        private async Task ServerAsyncSslHelper(
            SslProtocols clientSslProtocols,
            SslProtocols serverSslProtocols,
            bool expectedToFail = false)
        {
            _log.WriteLine(
                "Server: " + serverSslProtocols + "; Client: " + clientSslProtocols +
                " expectedToFail: " + expectedToFail);

            (NetworkStream clientStream, NetworkStream serverStream) = TestHelper.GetConnectedTcpStreams();

            using (SslStream sslServerStream = new SslStream(
                serverStream,
                false,
                AllowEmptyClientCertificate))
            using (SslStream sslClientStream = new SslStream(
                clientStream,
                false,
                delegate {
                    // Allow any certificate from the server.
                    // Note that simply ignoring exceptions from AuthenticateAsClientAsync() is not enough
                    // because in Mono, certificate validation is performed during the handshake and a failure
                    // would result in the connection being terminated before the handshake completed, thus
                    // making the server-side AuthenticateAsServerAsync() fail as well.
                    return true;
                }))
            {
                // Use a different SNI for each connection to prevent TLS 1.3 renegotiation issue: https://github.com/dotnet/runtime/issues/47378
                string serverName = TestHelper.GetTestSNIName(nameof(ServerAsyncSslHelper), clientSslProtocols, serverSslProtocols);

                _log.WriteLine("Connected on {0} {1} ({2} {3})", clientStream.Socket.LocalEndPoint, clientStream.Socket.RemoteEndPoint, clientStream.Socket.Handle, serverStream.Socket.Handle);
                _log.WriteLine("client SslStream#{0} server SslStream#{1}", sslClientStream.GetHashCode(),  sslServerStream.GetHashCode());

                _logVerbose.WriteLine("ServerAsyncAuthenticateTest.AuthenticateAsClientAsync start.");
                Task clientAuthentication = sslClientStream.AuthenticateAsClientAsync(
                    serverName,
                    null,
                    clientSslProtocols,
                    false);

                _logVerbose.WriteLine("ServerAsyncAuthenticateTest.AuthenticateAsServerAsync start.");
                Task serverAuthentication = sslServerStream.AuthenticateAsServerAsync(
                    _serverCertificate,
                    true,
                    serverSslProtocols,
                    false);

                try
                {
                    await clientAuthentication.WaitAsync(TestConfiguration.PassingTestTimeout);
                    _logVerbose.WriteLine("ServerAsyncAuthenticateTest.clientAuthentication complete.");
                }
                catch (Exception ex)
                {
                    // Ignore client-side errors: we're only interested in server-side behavior.
                    _log.WriteLine("Client exception : " + ex);
                    clientStream.Socket.Shutdown(SocketShutdown.Send);
                }

                await serverAuthentication.WaitAsync(TestConfiguration.PassingTestTimeout);
                _logVerbose.WriteLine("ServerAsyncAuthenticateTest.serverAuthentication complete.");

                _log.WriteLine(
                    "Server({0}) authenticated with encryption cipher: {1} {2}-bit strength",
                    serverStream.Socket.LocalEndPoint,
                    sslServerStream.CipherAlgorithm,
                    sslServerStream.CipherStrength);

                Assert.True(
                    sslServerStream.CipherAlgorithm != CipherAlgorithmType.Null,
                    "Cipher algorithm should not be NULL");

                Assert.True(sslServerStream.CipherStrength > 0, "Cipher strength should be greater than 0");
            }
        }

        // The following method is invoked by the RemoteCertificateValidationDelegate.
        private bool AllowEmptyClientCertificate(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
        {
            Assert.True(
                (sslPolicyErrors & SslPolicyErrors.RemoteCertificateNotAvailable) == SslPolicyErrors.RemoteCertificateNotAvailable,
                "Client didn't supply a cert, the server required one, yet sslPolicyErrors is " + sslPolicyErrors);
            return true;  // allow everything
        }

        #endregion Helpers
    }
}
