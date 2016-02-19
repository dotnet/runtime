// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Security.Tests
{
    public class ServerAsyncAuthenticateTest
    {
        private readonly ITestOutputHelper _log;
        private readonly X509Certificate2 _serverCertificate;

        public ServerAsyncAuthenticateTest()
        {
            _log = TestLogging.GetInstance();
            _serverCertificate = TestConfiguration.GetServerCertificate();
        }

        [Theory]
        [ClassData(typeof(SslProtocolSupport.SupportedSslProtocolsTestData))]
        public async Task ServerAsyncAuthenticate_EachSupportedProtocol_Success(SslProtocols protocol)
        {
            await ServerAsyncSslHelper(protocol, protocol);
        }

        [Theory]
        [ClassData(typeof(SslProtocolSupport.UnsupportedSslProtocolsTestData))]
        public async Task ServerAsyncAuthenticate_EachServerUnsupportedProtocol_Fail(SslProtocols protocol)
        {
            await Assert.ThrowsAsync<NotSupportedException>(() =>
            {
                return ServerAsyncSslHelper(
                    SslProtocolSupport.SupportedSslProtocols,
                    protocol,
                    expectedToFail: true);
            });
        }

        [Theory]
        [MemberData(nameof(ProtocolMismatchData))]
        public async Task ServerAsyncAuthenticate_MismatchProtocols_Fails(
            SslProtocols serverProtocol,
            SslProtocols clientProtocol,
            Type expectedException)
        {
            await Assert.ThrowsAsync(
                expectedException,
                () =>
                {
                    return ServerAsyncSslHelper(
                        serverProtocol,
                        clientProtocol,
                        expectedToFail: true);
                });
        }

        [Fact]
        public async Task ServerAsyncAuthenticate_UnsuportedAllServer_Fail()
        {
            await Assert.ThrowsAsync<NotSupportedException>(() =>
            {
                return ServerAsyncSslHelper(
                    SslProtocolSupport.SupportedSslProtocols,
                    SslProtocolSupport.UnsupportedSslProtocols,
                    expectedToFail: true);
            });
        }

        [Theory]
        [ClassData(typeof(SslProtocolSupport.SupportedSslProtocolsTestData))]
        public async Task ServerAsyncAuthenticate_AllClientVsIndividualServerSupportedProtocols_Success(
            SslProtocols serverProtocol)
        {
            await ServerAsyncSslHelper(SslProtocolSupport.SupportedSslProtocols, serverProtocol);
        }

        private static IEnumerable<object[]> ProtocolMismatchData()
        {
            yield return new object[] { SslProtocols.Tls, SslProtocols.Tls11, typeof(AuthenticationException) };
            yield return new object[] { SslProtocols.Tls, SslProtocols.Tls12, typeof(AuthenticationException) };
            yield return new object[] { SslProtocols.Tls11, SslProtocols.Tls, typeof(TimeoutException) };
            yield return new object[] { SslProtocols.Tls11, SslProtocols.Tls12, typeof(AuthenticationException) };
            yield return new object[] { SslProtocols.Tls12, SslProtocols.Tls, typeof(TimeoutException) };
            yield return new object[] { SslProtocols.Tls12, SslProtocols.Tls11, typeof(TimeoutException) };
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

            int timeOut = expectedToFail ? TestConfiguration.FailingTestTimeoutMiliseconds
                : TestConfiguration.PassingTestTimeoutMilliseconds;

            IPEndPoint endPoint = new IPEndPoint(IPAddress.IPv6Loopback, 0);
            var server = new TcpListener(endPoint);
            server.Start();

            using (var clientConnection = new TcpClient(AddressFamily.InterNetworkV6))
            {
                IPEndPoint serverEndPoint = (IPEndPoint)server.LocalEndpoint;

                Task clientConnect = clientConnection.ConnectAsync(serverEndPoint.Address, serverEndPoint.Port);
                Task<TcpClient> serverAccept = server.AcceptTcpClientAsync();

                // We expect that the network-level connect will always complete.
                Task.WaitAll(
                    new Task[] { clientConnect, serverAccept },
                    TestConfiguration.PassingTestTimeoutMilliseconds);

                using (TcpClient serverConnection = await serverAccept)
                using (SslStream sslClientStream = new SslStream(clientConnection.GetStream()))
                using (SslStream sslServerStream = new SslStream(
                    serverConnection.GetStream(),
                    false,
                    AllowAnyServerCertificate))
                {
                    string serverName = _serverCertificate.GetNameInfo(X509NameType.SimpleName, false);

                    Task clientAuthentication = sslClientStream.AuthenticateAsClientAsync(
                        serverName,
                        null,
                        clientSslProtocols,
                        false);

                    Task serverAuthentication = sslServerStream.AuthenticateAsServerAsync(
                        _serverCertificate,
                        true,
                        serverSslProtocols,
                        false);

                    try
                    {
                        clientAuthentication.Wait(timeOut);
                    }
                    catch (AggregateException ex)
                    {
                        // Ignore client-side errors: we're only interested in server-side behavior.
                        _log.WriteLine("Client exception: " + ex.InnerException);
                    }

                    bool serverAuthenticationCompleted = false;

                    try
                    {
                        serverAuthenticationCompleted = serverAuthentication.Wait(timeOut);
                    }
                    catch (AggregateException ex)
                    {
                        throw ex.InnerException;
                    }

                    if (!serverAuthenticationCompleted)
                    {
                        throw new TimeoutException();
                    }

                    _log.WriteLine(
                        "Server({0}) authenticated with encryption cipher: {1} {2}-bit strength",
                        serverEndPoint,
                        sslServerStream.CipherAlgorithm,
                        sslServerStream.CipherStrength);

                    Assert.True(
                        sslServerStream.CipherAlgorithm != CipherAlgorithmType.Null,
                        "Cipher algorithm should not be NULL");

                    Assert.True(sslServerStream.CipherStrength > 0, "Cipher strength should be greater than 0");
                }
            }
        }

        // The following method is invoked by the RemoteCertificateValidationDelegate.
        private bool AllowAnyServerCertificate(
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
