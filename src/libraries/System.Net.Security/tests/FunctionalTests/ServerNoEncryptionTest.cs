// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Security.Tests
{
    public class ServerNoEncryptionTest
    {
        private readonly ITestOutputHelper _log;

        public ServerNoEncryptionTest()
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
        public void ServerNoEncryption_ClientRequireEncryption_NoConnect()
        {
            using (var serverNoEncryption = new DummyTcpServer(
                new IPEndPoint(IPAddress.Loopback, 0), EncryptionPolicy.NoEncryption))
            using (var client = new TcpClient())
            {
                client.Connect(serverNoEncryption.RemoteEndPoint);

                using (var sslStream = new SslStream(client.GetStream(), false, AllowAnyServerCertificate, null, EncryptionPolicy.RequireEncryption))
                {
                    Assert.Throws<IOException>(() =>
                    {
                        sslStream.AuthenticateAsClient("localhost", null, TestConfiguration.DefaultSslProtocols, false);
                    });
                }
            }
        }

        [Fact]
        public void ServerNoEncryption_ClientAllowNoEncryption_ConnectWithNoEncryption()
        {
            using (var serverNoEncryption = new DummyTcpServer(
                new IPEndPoint(IPAddress.Loopback, 0), EncryptionPolicy.NoEncryption))
            using (var client = new TcpClient())
            {
                client.Connect(serverNoEncryption.RemoteEndPoint);

                using (var sslStream = new SslStream(client.GetStream(), false, AllowAnyServerCertificate, null, EncryptionPolicy.AllowNoEncryption))
                {
                    sslStream.AuthenticateAsClient("localhost", null, TestConfiguration.DefaultSslProtocols, false);

                    _log.WriteLine("Client({0}) authenticated to server({1}) with encryption cipher: {2} {3}-bit strength",
                        client.Client.LocalEndPoint, client.Client.RemoteEndPoint,
                        sslStream.CipherAlgorithm, sslStream.CipherStrength);

                    CipherAlgorithmType expected = CipherAlgorithmType.Null;
                    Assert.Equal(expected, sslStream.CipherAlgorithm);
                    Assert.Equal(0, sslStream.CipherStrength);
                }
            }
        }

        [Fact]
        public void ServerNoEncryption_ClientNoEncryption_ConnectWithNoEncryption()
        {
            using (var serverNoEncryption = new DummyTcpServer(
                new IPEndPoint(IPAddress.Loopback, 0), EncryptionPolicy.NoEncryption))
            using (var client = new TcpClient())
            {
                client.Connect(serverNoEncryption.RemoteEndPoint);

                using (var sslStream = new SslStream(client.GetStream(), false, AllowAnyServerCertificate, null, EncryptionPolicy.NoEncryption))
                {
                    sslStream.AuthenticateAsClient("localhost", null, TestConfiguration.DefaultSslProtocols, false);
                    _log.WriteLine("Client({0}) authenticated to server({1}) with encryption cipher: {2} {3}-bit strength",
                        client.Client.LocalEndPoint, client.Client.RemoteEndPoint,
                        sslStream.CipherAlgorithm, sslStream.CipherStrength);

                    CipherAlgorithmType expected = CipherAlgorithmType.Null;
                    Assert.Equal(expected, sslStream.CipherAlgorithm);
                    Assert.Equal(0, sslStream.CipherStrength);
                }
            }
        }
    }
}

