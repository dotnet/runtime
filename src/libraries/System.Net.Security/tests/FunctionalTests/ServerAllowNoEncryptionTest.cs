// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Security.Tests
{
    public class ServerAllowNoEncryptionTest
    {
        private readonly ITestOutputHelper _log;

        public ServerAllowNoEncryptionTest(ITestOutputHelper output)
        {
            _log = output;
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/68206", TestPlatforms.Android)]
        public async Task ServerAllowNoEncryption_ClientRequireEncryption_ConnectWithEncryption()
        {
            (NetworkStream clientStream, NetworkStream serverStream) = TestHelper.GetConnectedTcpStreams();
            using (clientStream)
            using (serverStream)
            {
                using (var client = new SslStream(clientStream, false, TestHelper.AllowAnyServerCertificate, null, EncryptionPolicy.RequireEncryption))
#pragma warning disable SYSLIB0040 // NoEncryption and AllowNoEncryption are obsolete
                using (var server = new SslStream(serverStream, false, TestHelper.AllowAnyServerCertificate, null, EncryptionPolicy.AllowNoEncryption))
#pragma warning restore SYSLIB0040
                {
                    await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                        client.AuthenticateAsClientAsync("localhost", null, SslProtocols.None, false),
                        server.AuthenticateAsServerAsync(TestConfiguration.ServerCertificate));

                    _log.WriteLine("Client authenticated to server({0}) with encryption cipher: {1} {2}-bit strength",
                        clientStream.Socket.RemoteEndPoint, client.CipherAlgorithm, client.CipherStrength);
                    Assert.NotEqual(CipherAlgorithmType.Null, client.CipherAlgorithm);
                    Assert.True(client.CipherStrength > 0);
                }
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/68206", TestPlatforms.Android)]
        public async Task ServerAllowNoEncryption_ClientAllowNoEncryption_ConnectWithEncryption()
        {
            (NetworkStream clientStream, NetworkStream serverStream) = TestHelper.GetConnectedTcpStreams();
            using (clientStream)
            using (serverStream)
            {
#pragma warning disable SYSLIB0040 // NoEncryption and AllowNoEncryption are obsolete
                using (var client = new SslStream(clientStream, false, TestHelper.AllowAnyServerCertificate, null, EncryptionPolicy.AllowNoEncryption))
                using (var server = new SslStream(serverStream, false, TestHelper.AllowAnyServerCertificate, null, EncryptionPolicy.AllowNoEncryption))
#pragma warning restore SYSLIB0040
                {
                    await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                        client.AuthenticateAsClientAsync("localhost", null, SslProtocols.None, false),
                        server.AuthenticateAsServerAsync(TestConfiguration.ServerCertificate));

                    _log.WriteLine("Client authenticated to server({0}) with encryption cipher: {1} {2}-bit strength",
                        clientStream.Socket.RemoteEndPoint, client.CipherAlgorithm, client.CipherStrength);
                    Assert.NotEqual(CipherAlgorithmType.Null, client.CipherAlgorithm);
                    Assert.True(client.CipherStrength > 0, "Cipher strength should be greater than 0");
                }
            }
        }

        [ConditionalFact(typeof(TestConfiguration), nameof(TestConfiguration.SupportsNullEncryption))]
        public async Task ServerAllowNoEncryption_ClientNoEncryption_ConnectWithNoEncryption()
        {
            (NetworkStream clientStream, NetworkStream serverStream) = TestHelper.GetConnectedTcpStreams();
            using (clientStream)
            using (serverStream)
            {
#pragma warning disable SYSLIB0040 // NoEncryption and AllowNoEncryption are obsolete
                using (var client = new SslStream(clientStream, false, TestHelper.AllowAnyServerCertificate, null, EncryptionPolicy.NoEncryption))
                using (var server = new SslStream(serverStream, false, TestHelper.AllowAnyServerCertificate, null, EncryptionPolicy.AllowNoEncryption))
#pragma warning restore SYSLIB0040
                {
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
                    await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                        // null encryption is not permitted with Tls13
                        client.AuthenticateAsClientAsync("localhost", null, SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12, false),
                        server.AuthenticateAsServerAsync(TestConfiguration.ServerCertificate));
#pragma warning restore SYSLIB0039
                    _log.WriteLine("Client authenticated to server({0}) with encryption cipher: {1} {2}-bit strength",
                        clientStream.Socket.RemoteEndPoint, client.CipherAlgorithm, client.CipherStrength);

                    CipherAlgorithmType expected = CipherAlgorithmType.Null;
                    Assert.Equal(expected, client.CipherAlgorithm);
                    Assert.Equal(0, client.CipherStrength);
                }
            }
        }
    }
}
