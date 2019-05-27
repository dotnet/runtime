// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Security.Tests
{
    public class ClientAsyncAuthenticateTest
    {
        private readonly ITestOutputHelper _log;

        public ClientAsyncAuthenticateTest()
        {
            _log = TestLogging.GetInstance();
        }

        [Fact]
        public async Task ClientAsyncAuthenticate_SslStreamClientServerNone_UseStrongCryptoSet()
        {
            SslProtocols protocol = SslProtocols.None;
            await ClientAsyncSslHelper(protocol, protocol);

            // Additional manual verification.
            // Step into the code and verify that the 'SCH_USE_STRONG_CRYPTO' flag is being set.
        }

        [Fact]
        public async Task ClientAsyncAuthenticate_ServerRequireEncryption_ConnectWithEncryption()
        {
            await ClientAsyncSslHelper(EncryptionPolicy.RequireEncryption);
        }

        [Fact]
        public async Task ClientAsyncAuthenticate_ServerNoEncryption_NoConnect()
        {
            await Assert.ThrowsAsync<IOException>(() => ClientAsyncSslHelper(EncryptionPolicy.NoEncryption));
        }

        [Theory]
        [ClassData(typeof(SslProtocolSupport.SupportedSslProtocolsTestData))]
        public async Task ClientAsyncAuthenticate_EachSupportedProtocol_Success(SslProtocols protocol)
        {
            await ClientAsyncSslHelper(protocol, protocol);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task ClientAsyncAuthenticate_Ssl2WithSelf_Success()
        {
            // Test Ssl2 against itself.  This is a standalone test as even on versions where Windows supports Ssl2,
            // it appears to have rules around not using it when other protocols are mentioned.
            if (!PlatformDetection.IsWindows10Version1607OrGreater)
            {
#pragma warning disable 0618
                await ClientAsyncSslHelper(SslProtocols.Ssl2, SslProtocols.Ssl2);
#pragma warning restore 0618
            }
        }

        [Theory]
        [MemberData(nameof(ProtocolMismatchData))]
        public async Task ClientAsyncAuthenticate_MismatchProtocols_Fails(
            SslProtocols serverProtocol,
            SslProtocols clientProtocol,
            Type expectedException)
        {
            Exception e = await Record.ExceptionAsync(() => ClientAsyncSslHelper(serverProtocol, clientProtocol));
            Assert.NotNull(e);
            Assert.IsAssignableFrom(expectedException, e);
        }

        [Fact]
        public async Task ClientAsyncAuthenticate_AllServerAllClient_Success()
        {
            await ClientAsyncSslHelper(
                SslProtocolSupport.SupportedSslProtocols,
                SslProtocolSupport.SupportedSslProtocols);
        }

        [Theory]
        [ClassData(typeof(SslProtocolSupport.SupportedSslProtocolsTestData))]
        public async Task ClientAsyncAuthenticate_AllServerVsIndividualClientSupportedProtocols_Success(
            SslProtocols clientProtocol)
        {
            await ClientAsyncSslHelper(clientProtocol, SslProtocolSupport.SupportedSslProtocols);
        }

        [Theory]
        [ClassData(typeof(SslProtocolSupport.SupportedSslProtocolsTestData))]
        public async Task ClientAsyncAuthenticate_IndividualServerVsAllClientSupportedProtocols_Success(
            SslProtocols serverProtocol)
        {
            await ClientAsyncSslHelper(SslProtocolSupport.SupportedSslProtocols, serverProtocol);
            // Cached Tls creds fail when used against Tls servers of higher versions.
            // Servers are not expected to dynamically change versions.
        }

        public static IEnumerable<object[]> ProtocolMismatchData()
        {
#pragma warning disable 0618
            yield return new object[] { SslProtocols.Ssl2, SslProtocols.Ssl3, typeof(Exception) };
            yield return new object[] { SslProtocols.Ssl2, SslProtocols.Tls12, typeof(Exception) };
            yield return new object[] { SslProtocols.Ssl3, SslProtocols.Tls12, typeof(Exception) };
#pragma warning restore 0618
            yield return new object[] { SslProtocols.Tls, SslProtocols.Tls11, typeof(IOException) };
            yield return new object[] { SslProtocols.Tls, SslProtocols.Tls12, typeof(IOException) };
            yield return new object[] { SslProtocols.Tls11, SslProtocols.Tls, typeof(AuthenticationException) };
            yield return new object[] { SslProtocols.Tls12, SslProtocols.Tls, typeof(AuthenticationException) };
            yield return new object[] { SslProtocols.Tls12, SslProtocols.Tls11, typeof(AuthenticationException) };
            yield return new object[] { SslProtocols.Tls11, SslProtocols.Tls12, typeof(IOException) };
        }

        #region Helpers

        private Task ClientAsyncSslHelper(EncryptionPolicy encryptionPolicy)
        {
            return ClientAsyncSslHelper(encryptionPolicy, SslProtocolSupport.DefaultSslProtocols, SslProtocolSupport.DefaultSslProtocols);
        }

        private Task ClientAsyncSslHelper(SslProtocols clientSslProtocols, SslProtocols serverSslProtocols)
        {
            return ClientAsyncSslHelper(EncryptionPolicy.RequireEncryption, clientSslProtocols, serverSslProtocols);
        }

        private async Task ClientAsyncSslHelper(
            EncryptionPolicy encryptionPolicy,
            SslProtocols clientSslProtocols,
            SslProtocols serverSslProtocols)
        {
            _log.WriteLine("Server: " + serverSslProtocols + "; Client: " + clientSslProtocols);

            IPEndPoint endPoint = new IPEndPoint(IPAddress.IPv6Loopback, 0);

            using (var server = new DummyTcpServer(endPoint, encryptionPolicy))
            using (var client = new TcpClient(AddressFamily.InterNetworkV6))
            {
                server.SslProtocols = serverSslProtocols;
                await client.ConnectAsync(server.RemoteEndPoint.Address, server.RemoteEndPoint.Port);
                using (SslStream sslStream = new SslStream(client.GetStream(), false, AllowAnyServerCertificate, null))
                {
                    Task clientAuthTask = sslStream.AuthenticateAsClientAsync("localhost", null, clientSslProtocols, false);
                    await clientAuthTask.TimeoutAfter(TestConfiguration.PassingTestTimeoutMilliseconds);

                    _log.WriteLine("Client authenticated to server({0}) with encryption cipher: {1} {2}-bit strength",
                        server.RemoteEndPoint, sslStream.CipherAlgorithm, sslStream.CipherStrength);
                    Assert.True(sslStream.CipherAlgorithm != CipherAlgorithmType.Null, "Cipher algorithm should not be NULL");
                    Assert.True(sslStream.CipherStrength > 0, "Cipher strength should be greater than 0");
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
            return true;  // allow everything
        }

        #endregion Helpers
    }
}
