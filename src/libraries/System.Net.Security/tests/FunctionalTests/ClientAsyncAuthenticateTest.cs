// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.IO;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

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
            SslProtocols.Ssl2,
            SslProtocols.Ssl3,
            SslProtocols.Tls,
            SslProtocols.Tls11,
            SslProtocols.Tls12,
        };

        public ClientAsyncAuthenticateTest()
        {
            _log = TestLogging.GetInstance();
        }

        [Fact]
        public void ClientAsyncAuthenticate_ServerRequireEncryption_ConnectWithEncryption()
        {
            ClientAsyncSslHelper(EncryptionPolicy.RequireEncryption);
        }

        [Fact]
        public void ClientAsyncAuthenticate_ServerNoEncryption_NoConnect()
        {
            Assert.Throws<IOException>(() =>
            {
                ClientAsyncSslHelper(EncryptionPolicy.NoEncryption);
            });
        }

        [Fact]
        public void ClientAsyncAuthenticate_EachProtocol_Success()
        {
            foreach (SslProtocols protocol in s_eachSslProtocol)
            {
                ClientAsyncSslHelper(protocol, protocol);
            }
        }

        [Fact]
        public void ClientAsyncAuthenticate_MismatchProtocols_Fails()
        {
            foreach (SslProtocols serverProtocol in s_eachSslProtocol)
            {
                foreach (SslProtocols clientProtocol in s_eachSslProtocol)
                {
                    if (serverProtocol != clientProtocol)
                    {
                        try
                        {
                            ClientAsyncSslHelper(serverProtocol, clientProtocol);
                            Assert.True(false, serverProtocol + "; " + clientProtocol);
                        }
                        catch (AuthenticationException) { }
                        catch (IOException) { }
                    }
                }
            }
        }

        [Fact]
        public void ClientAsyncAuthenticate_Ssl2Tls12ServerSsl2Client_Fails()
        {
            Assert.Throws<Win32Exception>(() =>
            {
                // Ssl2 and Tls 1.2 are mutually exclusive.
                ClientAsyncSslHelper(SslProtocols.Ssl2 | SslProtocols.Tls12, SslProtocols.Ssl2);
            });
        }

        [Fact]
        public void ClientAsyncAuthenticate_Ssl2Tls12ServerTls12Client_Fails()
        {
            Assert.Throws<Win32Exception>(() =>
            {
                // Ssl2 and Tls 1.2 are mutually exclusive.
                ClientAsyncSslHelper(SslProtocols.Ssl2 | SslProtocols.Tls12, SslProtocols.Tls12);
            });
        }

        [Fact]
        public void ClientAsyncAuthenticate_Ssl2ServerSsl2Tls12Client_Success()
        {
            ClientAsyncSslHelper(SslProtocols.Ssl2, SslProtocols.Ssl2 | SslProtocols.Tls12);
        }

        [Fact]
        public void ClientAsyncAuthenticate_Tls12ServerSsl2Tls12Client_Success()
        {
            ClientAsyncSslHelper(SslProtocols.Tls12, SslProtocols.Ssl2 | SslProtocols.Tls12);
        }

        [Fact]
        public void ClientAsyncAuthenticate_AllServerAllClient_Success()
        {
            // Drop Ssl2, it's incompatible with Tls 1.2
            SslProtocols sslProtocols = AllSslProtocols & ~SslProtocols.Ssl2;
            ClientAsyncSslHelper(sslProtocols, sslProtocols);
        }

        [Fact]
        public void ClientAsyncAuthenticate_AllServerVsIndividualClientProtocols_Success()
        {
            foreach (SslProtocols clientProtocol in s_eachSslProtocol)
            {
                if (clientProtocol != SslProtocols.Ssl2) // Incompatible with Tls 1.2
                {
                    ClientAsyncSslHelper(clientProtocol, AllSslProtocols);
                }
            }
        }

        [Fact]
        public void ClientAsyncAuthenticate_IndividualServerVsAllClientProtocols_Success()
        {
            SslProtocols clientProtocols = AllSslProtocols & ~SslProtocols.Ssl2; // Incompatible with Tls 1.2
            foreach (SslProtocols serverProtocol in s_eachSslProtocol)
            {
                if (serverProtocol != SslProtocols.Ssl2) // Incompatible with Tls 1.2
                {
                    ClientAsyncSslHelper(clientProtocols, serverProtocol);
                    // Cached Tls creds fail when used against Tls servers of higher versions.
                    // Servers are not expected to dynamically change versions.
                }
            }
        }

        #region Helpers

        private void ClientAsyncSslHelper(EncryptionPolicy encryptionPolicy)
        {
            ClientAsyncSslHelper(encryptionPolicy, TestConfiguration.DefaultSslProtocols, TestConfiguration.DefaultSslProtocols);
        }

        private void ClientAsyncSslHelper(SslProtocols clientSslProtocols, SslProtocols serverSslProtocols)
        {
            ClientAsyncSslHelper(EncryptionPolicy.RequireEncryption, clientSslProtocols, serverSslProtocols);
        }

        private void ClientAsyncSslHelper(EncryptionPolicy encryptionPolicy, SslProtocols clientSslProtocols,
            SslProtocols serverSslProtocols)
        {
            _log.WriteLine("Server: " + serverSslProtocols + "; Client: " + clientSslProtocols);

            IPEndPoint endPoint = new IPEndPoint(IPAddress.IPv6Loopback, 0);

            using (var server = new DummyTcpServer(endPoint, encryptionPolicy))
            using (var client = new TcpClient(AddressFamily.InterNetworkV6))
            {
                server.SslProtocols = serverSslProtocols;
                client.Connect(server.RemoteEndPoint);
                using (SslStream sslStream = new SslStream(client.GetStream(), false, AllowAnyServerCertificate, null))
                {
                    IAsyncResult async = sslStream.BeginAuthenticateAsClient("localhost", null, clientSslProtocols, false, null, null);
                    Assert.True(async.AsyncWaitHandle.WaitOne(10000), "Timed Out");
                    sslStream.EndAuthenticateAsClient(async);

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

