// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel;
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

        private const SslProtocols AllSslProtocols =
            SslProtocols.Ssl2
            | SslProtocols.Ssl3
            | SslProtocols.Tls
            | SslProtocols.Tls11
            | SslProtocols.Tls12;

        private static readonly SslProtocols[] s_eachSslProtocol = new SslProtocols[]
        {
            SslProtocols.Ssl3,
            SslProtocols.Tls,
            SslProtocols.Tls11,
            SslProtocols.Tls12,
        };

        private static IEnumerable<object[]> ProtocolMismatchData()
        {
            yield return new object[] { SslProtocols.Ssl3, SslProtocols.Ssl2, typeof(IOException) };
            yield return new object[] { SslProtocols.Ssl3, SslProtocols.Tls, typeof(IOException) };
            yield return new object[] { SslProtocols.Ssl3, SslProtocols.Tls11, typeof(IOException) };
            yield return new object[] { SslProtocols.Ssl3, SslProtocols.Tls12, typeof(IOException) };

            yield return new object[] { SslProtocols.Tls, SslProtocols.Ssl2, typeof(IOException) };
            yield return new object[] { SslProtocols.Tls, SslProtocols.Ssl3, typeof(AuthenticationException) };
            yield return new object[] { SslProtocols.Tls, SslProtocols.Tls11, typeof(IOException) };
            yield return new object[] { SslProtocols.Tls, SslProtocols.Tls12, typeof(IOException) };

            yield return new object[] { SslProtocols.Tls11, SslProtocols.Ssl2, typeof(IOException) };
            yield return new object[] { SslProtocols.Tls11, SslProtocols.Ssl3, typeof(AuthenticationException) };
            yield return new object[] { SslProtocols.Tls11, SslProtocols.Tls, typeof(AuthenticationException) };
            yield return new object[] { SslProtocols.Tls11, SslProtocols.Tls12, typeof(IOException) };

            yield return new object[] { SslProtocols.Tls12, SslProtocols.Ssl2, typeof(IOException) };
            yield return new object[] { SslProtocols.Tls12, SslProtocols.Ssl3, typeof(AuthenticationException) };
            yield return new object[] { SslProtocols.Tls12, SslProtocols.Tls, typeof(AuthenticationException) };
            yield return new object[] { SslProtocols.Tls12, SslProtocols.Tls11, typeof(AuthenticationException) };
        }

        private static IEnumerable<object[]> ProtocolMismatchDataSsl2SpecificWindows()
        {
            yield return new object[] {SslProtocols.Ssl2, SslProtocols.Ssl3, typeof (IOException)};
            yield return new object[] {SslProtocols.Ssl2, SslProtocols.Tls, typeof (IOException)};
            yield return new object[] {SslProtocols.Ssl2, SslProtocols.Tls11, typeof (IOException)};
            yield return new object[] {SslProtocols.Ssl2, SslProtocols.Tls12, typeof (IOException)};
        }

        private static IEnumerable<object[]> ProtocolMismatchDataSsl2SpecificLinux()
        {
            yield return new object[] { SslProtocols.Ssl2, SslProtocols.Ssl3, typeof(AuthenticationException) };
            yield return new object[] { SslProtocols.Ssl2, SslProtocols.Tls, typeof(AuthenticationException) };
            yield return new object[] { SslProtocols.Ssl2, SslProtocols.Tls11, typeof(AuthenticationException) };
            yield return new object[] { SslProtocols.Ssl2, SslProtocols.Tls12, typeof(AuthenticationException) };
        }

        public ClientAsyncAuthenticateTest()
        {
            _log = TestLogging.GetInstance();
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

        [Fact]
        public async Task ClientAsyncAuthenticate_EachProtocol_Success()
        {
            foreach (SslProtocols protocol in s_eachSslProtocol)
            {
                await ClientAsyncSslHelper(protocol, protocol);
            }
        }

        [Theory]
        [MemberData("ProtocolMismatchData")]
        public async Task ClientAsyncAuthenticate_MismatchProtocols_Fails(SslProtocols server, SslProtocols client, Type expected)
        {
            await Assert.ThrowsAsync(expected, () => ClientAsyncSslHelper(server, client));
        }

        [Theory]
        [MemberData("ProtocolMismatchDataSsl2SpecificWindows")]
        [PlatformSpecific(PlatformID.Windows)]
        public async Task ClientAsyncAuthenticate_MismatchProtocols_Ssl2_Fails_Windows(SslProtocols server, SslProtocols client, Type expected)
        {
            await Assert.ThrowsAsync(expected, () => ClientAsyncSslHelper(server, client));
        }

        [Theory]
        [MemberData("ProtocolMismatchDataSsl2SpecificLinux")]
        [PlatformSpecific(PlatformID.Linux)]
        public async Task ClientAsyncAuthenticate_MismatchProtocols_Ssl2_Fails_Linux(SslProtocols server, SslProtocols client, Type expected)
        {
            await Assert.ThrowsAsync(expected, () => ClientAsyncSslHelper(server, client));
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public async Task ClientAsyncAuthenticate_EachProtocol_Ssl2_Success()
        {
            await ClientAsyncSslHelper(SslProtocols.Ssl2, SslProtocols.Ssl2);
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public async Task ClientAsyncAuthenticate_Ssl2Tls12ServerSsl2Client_Fails()
        {
            // Ssl2 and Tls 1.2 are mutually exclusive.
            await Assert.ThrowsAsync<Win32Exception>(() => ClientAsyncSslHelper(SslProtocols.Ssl2 | SslProtocols.Tls12, SslProtocols.Ssl2));
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public async Task ClientAsyncAuthenticate_Ssl2Tls12ServerTls12Client_Fails()
        {
            // Ssl2 and Tls 1.2 are mutually exclusive.
            await Assert.ThrowsAsync<Win32Exception>(() => ClientAsyncSslHelper(SslProtocols.Ssl2 | SslProtocols.Tls12, SslProtocols.Tls12));
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public async Task ClientAsyncAuthenticate_Ssl2ServerSsl2Tls12Client_Success()
        {
            await ClientAsyncSslHelper(SslProtocols.Ssl2, SslProtocols.Ssl2 | SslProtocols.Tls12);
        }

        [Fact]
        public async Task ClientAsyncAuthenticate_Tls12ServerSsl2Tls12Client_Success()
        {
            await ClientAsyncSslHelper(SslProtocols.Tls12, SslProtocols.Ssl2 | SslProtocols.Tls12);
        }

        [Fact]
        public async Task ClientAsyncAuthenticate_AllServerAllClient_Success()
        {
            // Drop Ssl2, it's incompatible with Tls 1.2
            SslProtocols sslProtocols = AllSslProtocols & ~SslProtocols.Ssl2;
            await ClientAsyncSslHelper(sslProtocols, sslProtocols);
        }

        [Fact]
        public async Task ClientAsyncAuthenticate_AllServerVsIndividualClientProtocols_Success()
        {
            foreach (SslProtocols clientProtocol in s_eachSslProtocol)
            {
                if (clientProtocol != SslProtocols.Ssl2) // Incompatible with Tls 1.2
                {
                    await ClientAsyncSslHelper(clientProtocol, AllSslProtocols);
                }
            }
        }

        [Fact]
        public async Task ClientAsyncAuthenticate_IndividualServerVsAllClientProtocols_Success()
        {
            SslProtocols clientProtocols = AllSslProtocols & ~SslProtocols.Ssl2; // Incompatible with Tls 1.2
            foreach (SslProtocols serverProtocol in s_eachSslProtocol)
            {
                if (serverProtocol != SslProtocols.Ssl2) // Incompatible with Tls 1.2
                {
                    await ClientAsyncSslHelper(clientProtocols, serverProtocol);
                    // Cached Tls creds fail when used against Tls servers of higher versions.
                    // Servers are not expected to dynamically change versions.
                }
            }
        }

        #region Helpers

        private Task ClientAsyncSslHelper(EncryptionPolicy encryptionPolicy)
        {
            return ClientAsyncSslHelper(encryptionPolicy, TestConfiguration.DefaultSslProtocols, TestConfiguration.DefaultSslProtocols);
        }

        private Task ClientAsyncSslHelper(SslProtocols clientSslProtocols, SslProtocols serverSslProtocols)
        {
            return ClientAsyncSslHelper(EncryptionPolicy.RequireEncryption, clientSslProtocols, serverSslProtocols);
        }

        private async Task ClientAsyncSslHelper(EncryptionPolicy encryptionPolicy, SslProtocols clientSslProtocols,
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
                    Task async = sslStream.AuthenticateAsClientAsync("localhost", null, clientSslProtocols, false);
                    Assert.True(((IAsyncResult)async).AsyncWaitHandle.WaitOne(TestConfiguration.TestTimeoutSeconds * 1000), "Timed Out");
                    async.GetAwaiter().GetResult();

                    _log.WriteLine("Client({0}) authenticated to server({1}) with encryption cipher: {2} {3}-bit strength",
                        client.Client.LocalEndPoint, client.Client.RemoteEndPoint,
                        sslStream.CipherAlgorithm, sslStream.CipherStrength);
                    Assert.True(sslStream.CipherAlgorithm != CipherAlgorithmType.Null, "Cipher algorithm should not be NULL");
                    Assert.True(sslStream.CipherStrength > 0, "Cipher strength should be greater than 0");
                }
            }
        }

        // The following method is invoked by the RemoteCertificateValidationDelegate.
        public bool AllowAnyServerCertificate(
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
