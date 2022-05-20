// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Security.Tests
{
    public class ServerRequireEncryptionTest
    {
        private readonly ITestOutputHelper _log;

        public ServerRequireEncryptionTest(ITestOutputHelper output)
        {
            _log = output;
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/68206", TestPlatforms.Android)]
        public async Task ServerRequireEncryption_ClientRequireEncryption_ConnectWithEncryption()
        {
            (NetworkStream clientStream, NetworkStream serverStream) = TestHelper.GetConnectedTcpStreams();
            using (clientStream)
            using (serverStream)
            {
                using (var client = new SslStream(clientStream, false, TestHelper.AllowAnyServerCertificate, null, EncryptionPolicy.RequireEncryption))
                using (var server = new SslStream(serverStream))
                {
                    await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                        client.AuthenticateAsClientAsync("localhost", null, SslProtocolSupport.DefaultSslProtocols, false),
                        server.AuthenticateAsServerAsync(TestConfiguration.ServerCertificate));

                    _log.WriteLine("Client authenticated to server({0}) with encryption cipher: {1} {2}-bit strength",
                        clientStream.Socket.RemoteEndPoint, client.CipherAlgorithm, client.CipherStrength);
                    Assert.True(client.CipherAlgorithm != CipherAlgorithmType.Null, "Cipher algorithm should not be NULL");
                    Assert.True(client.CipherStrength > 0, "Cipher strength should be greater than 0");
                }
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/68206", TestPlatforms.Android)]
        public async Task ServerRequireEncryption_ClientAllowNoEncryption_ConnectWithEncryption()
        {
            (NetworkStream clientStream, NetworkStream serverStream) = TestHelper.GetConnectedTcpStreams();
            using (clientStream)
            using (serverStream)
            {
#pragma warning disable SYSLIB0040 // NoEncryption and AllowNoEncryption are obsolete
                using (var client = new SslStream(clientStream, false, TestHelper.AllowAnyServerCertificate, null, EncryptionPolicy.AllowNoEncryption))
#pragma warning restore SYSLIB0040
                using (var server = new SslStream(serverStream))
                {
                    await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                        client.AuthenticateAsClientAsync("localhost", null, SslProtocolSupport.DefaultSslProtocols, false),
                        server.AuthenticateAsServerAsync(TestConfiguration.ServerCertificate));

                    _log.WriteLine("Client authenticated to server({0}) with encryption cipher: {1} {2}-bit strength",
                        clientStream.Socket.RemoteEndPoint, client.CipherAlgorithm, client.CipherStrength);
                    Assert.True(client.CipherAlgorithm != CipherAlgorithmType.Null, "Cipher algorithm should not be NULL");
                    Assert.True(client.CipherStrength > 0, "Cipher strength should be greater than 0");
                }
            }
        }

        [ConditionalFact(typeof(TestConfiguration), nameof(TestConfiguration.SupportsNullEncryption))]
        public async Task ServerRequireEncryption_ClientNoEncryption_NoConnect()
        {
            (NetworkStream clientStream, NetworkStream serverStream) = TestHelper.GetConnectedTcpStreams();
            using (clientStream)
            using (serverStream)
            {
#pragma warning disable SYSLIB0040 // NoEncryption and AllowNoEncryption are obsolete
                using (var client = new SslStream(clientStream, false, TestHelper.AllowAnyServerCertificate, null, EncryptionPolicy.NoEncryption))
#pragma warning restore SYSLIB0040
                using (var server = new SslStream(serverStream))
                {
                    Task serverTask = server.AuthenticateAsServerAsync(TestConfiguration.ServerCertificate);
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete                    
                    await Assert.ThrowsAsync(TestConfiguration.SupportsHandshakeAlerts ? typeof(AuthenticationException) : typeof(IOException), () =>
                            client.AuthenticateAsClientAsync("localhost", null, SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12, false));
#pragma warning restore SYSLIB0039                            
                    try
                    {
                        await serverTask.WaitAsync(TestConfiguration.PassingTestTimeout);
                    }
                    catch (Exception ex)
                    {
                        // This will fail. Log server error in case test fails.
                        _log.WriteLine(ex.ToString());
                    }
                }
            }
        }
    }
}
