// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    public class ClientDefaultEncryptionTest
    {
        private readonly ITestOutputHelper _log;

        public ClientDefaultEncryptionTest()
        {
            _log = TestLogging.GetInstance();
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

        [Fact]
        public async Task ClientDefaultEncryption_ServerRequireEncryption_ConnectWithEncryption()
        {
            using (var serverRequireEncryption = new DummyTcpServer(
                new IPEndPoint(IPAddress.Loopback, 0), EncryptionPolicy.RequireEncryption))
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(serverRequireEncryption.RemoteEndPoint.Address, serverRequireEncryption.RemoteEndPoint.Port);

                using (var sslStream = new SslStream(client.GetStream(), false, AllowAnyServerCertificate, null))
                {
                    await sslStream.AuthenticateAsClientAsync("localhost", null, SslProtocolSupport.DefaultSslProtocols, false);
                    _log.WriteLine("Client authenticated to server({0}) with encryption cipher: {1} {2}-bit strength",
                        serverRequireEncryption.RemoteEndPoint, sslStream.CipherAlgorithm, sslStream.CipherStrength);
                    Assert.True(sslStream.CipherAlgorithm != CipherAlgorithmType.Null, "Cipher algorithm should not be NULL");
                    Assert.True(sslStream.CipherStrength > 0, "Cipher strength should be greater than 0");
                }
            }
        }

        [Fact]
        [ActiveIssue(16534, TestPlatforms.Windows)]
        public async Task ClientDefaultEncryption_ServerAllowNoEncryption_ConnectWithEncryption()
        {
            using (var serverAllowNoEncryption = new DummyTcpServer(
                new IPEndPoint(IPAddress.Loopback, 0), EncryptionPolicy.AllowNoEncryption))
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(serverAllowNoEncryption.RemoteEndPoint.Address, serverAllowNoEncryption.RemoteEndPoint.Port);

                using (var sslStream = new SslStream(client.GetStream(), false, AllowAnyServerCertificate, null))
                {
                    await sslStream.AuthenticateAsClientAsync("localhost", null, SslProtocolSupport.DefaultSslProtocols, false);
                    _log.WriteLine("Client authenticated to server({0}) with encryption cipher: {1} {2}-bit strength",
                        serverAllowNoEncryption.RemoteEndPoint, sslStream.CipherAlgorithm, sslStream.CipherStrength);
                    Assert.True(sslStream.CipherAlgorithm != CipherAlgorithmType.Null, "Cipher algorithm should not be NULL");
                    Assert.True(sslStream.CipherStrength > 0, "Cipher strength should be greater than 0");
                }
            }
        }

        [Fact]
        public async Task ClientDefaultEncryption_ServerNoEncryption_NoConnect()
        {
            using (var serverNoEncryption = new DummyTcpServer(
                new IPEndPoint(IPAddress.Loopback, 0), EncryptionPolicy.NoEncryption))
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(serverNoEncryption.RemoteEndPoint.Address, serverNoEncryption.RemoteEndPoint.Port);

                using (var sslStream = new SslStream(client.GetStream(), false, AllowAnyServerCertificate, null))
                {
                    await Assert.ThrowsAsync<IOException>(() =>
                        sslStream.AuthenticateAsClientAsync("localhost", null, SslProtocolSupport.DefaultSslProtocols, false));
                }
            }
        }
    }
}
