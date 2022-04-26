// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Security.Tests
{
    public class ClientDefaultEncryptionTest
    {
        private readonly ITestOutputHelper _log;

        public ClientDefaultEncryptionTest(ITestOutputHelper output)
        {
            _log = output;
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/68206", TestPlatforms.Android)]
        public async Task ClientDefaultEncryption_ServerRequireEncryption_ConnectWithEncryption()
        {
            (NetworkStream clientStream, NetworkStream serverStream) = TestHelper.GetConnectedTcpStreams();
            using (clientStream)
            using (serverStream)
            {
                using (var client = new SslStream(clientStream, false, TestHelper.AllowAnyServerCertificate, null))
                using (var server = new SslStream(serverStream))
                {
                    await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                        client.AuthenticateAsClientAsync("localhost", null, SslProtocolSupport.DefaultSslProtocols, false),
                        server.AuthenticateAsServerAsync(TestConfiguration.ServerCertificate));

                    _log.WriteLine("Client authenticated to server({0}) with encryption cipher: {1} {2}-bit strength",
                        clientStream.Socket.RemoteEndPoint, client.CipherAlgorithm, client.CipherStrength) ;
                    Assert.True(client.CipherAlgorithm != CipherAlgorithmType.Null, "Cipher algorithm should not be NULL");
                    Assert.True(client.CipherStrength > 0, "Cipher strength should be greater than 0");
                }
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/68206", TestPlatforms.Android)]
        public async Task ClientDefaultEncryption_ServerAllowNoEncryption_ConnectWithEncryption()
        {
            (NetworkStream clientStream, NetworkStream serverStream) = TestHelper.GetConnectedTcpStreams();
            using (clientStream)
            using (serverStream)
            {
                using (var client = new SslStream(clientStream, false, TestHelper.AllowAnyServerCertificate, null))
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
        public async Task ClientDefaultEncryption_ServerNoEncryption_NoConnect()
        {
            (NetworkStream clientStream, NetworkStream serverStream) = TestHelper.GetConnectedTcpStreams();
            using (clientStream)
            using (serverStream)
            {
                using (var client = new SslStream(clientStream, false, TestHelper.AllowAnyServerCertificate, null))
#pragma warning disable SYSLIB0040 // NoEncryption and AllowNoEncryption are obsolete
                using (var server = new SslStream(serverStream, false, TestHelper.AllowAnyServerCertificate, null, EncryptionPolicy.NoEncryption))
#pragma warning restore SYSLIB0040
                {
                    Task serverTask = server.AuthenticateAsServerAsync(TestConfiguration.ServerCertificate);
                    await Assert.ThrowsAsync<AuthenticationException>(() =>
                        client.AuthenticateAsClientAsync("localhost", null, SslProtocolSupport.DefaultSslProtocols, false));
                    try
                    {
                        await serverTask.WaitAsync(TestConfiguration.PassingTestTimeout);
                    }
                    catch (Exception ex)
                    {
                        // serverTask will fail.
                        // We generally don't care but can log exception to help diagnose test failures
                        _log.WriteLine(ex.ToString());
                    }
                }
            }
        }
    }
}
