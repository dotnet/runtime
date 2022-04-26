// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    public class ServerNoEncryptionTest
    {
        private readonly ITestOutputHelper _log;

        public ServerNoEncryptionTest(ITestOutputHelper output)
        {
            _log = output;
        }

        [ConditionalFact(typeof(TestConfiguration), nameof(TestConfiguration.SupportsNullEncryption))]
        public async Task ServerNoEncryption_ClientRequireEncryption_NoConnect()
        {
            (NetworkStream clientStream, NetworkStream serverStream) = TestHelper.GetConnectedTcpStreams();
            using (clientStream)
            using (serverStream)
            {
                using (var client = new SslStream(clientStream, false, TestHelper.AllowAnyServerCertificate, null, EncryptionPolicy.RequireEncryption))
#pragma warning disable SYSLIB0040 // NoEncryption and AllowNoEncryption are obsolete
                using (var server = new SslStream(serverStream, false, TestHelper.AllowAnyServerCertificate, null, EncryptionPolicy.NoEncryption))
#pragma warning restore SYSLIB0040
                {
                    Task serverTask = server.AuthenticateAsServerAsync(TestConfiguration.ServerCertificate);
                    await Assert.ThrowsAsync<AuthenticationException>(() =>
                        client.AuthenticateAsClientAsync("localhost", null, SslProtocolSupport.DefaultSslProtocols, false));
                    try
                    {
                        await serverTask;
                    }
                    catch (Exception ex)
                    {
                        // serverTask will fail. Log server error in case the test fails.
                        _log.WriteLine(ex.ToString());
                    }
                }
            }
        }

        [ConditionalTheory(typeof(TestConfiguration), nameof(TestConfiguration.SupportsNullEncryption))]
#pragma warning disable SYSLIB0040 // NoEncryption and AllowNoEncryption are obsolete
        [InlineData(EncryptionPolicy.AllowNoEncryption)]
        [InlineData(EncryptionPolicy.NoEncryption)]
#pragma warning restore SYSLIB0040
        public async Task ServerNoEncryption_ClientPermitsNoEncryption_ConnectWithNoEncryption(EncryptionPolicy policy)
        {
            (NetworkStream clientStream, NetworkStream serverStream) = TestHelper.GetConnectedTcpStreams();
            using (clientStream)
            using (serverStream)
            {
                using (var client = new SslStream(clientStream, false, TestHelper.AllowAnyServerCertificate, null, policy))
#pragma warning disable SYSLIB0040 // NoEncryption and AllowNoEncryption are obsolete
                using (var server = new SslStream(serverStream, false, TestHelper.AllowAnyServerCertificate, null, EncryptionPolicy.NoEncryption))
#pragma warning restore SYSLIB0040
                {
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
                    await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                        // null encryption is not permitted with Tls13
                        client.AuthenticateAsClientAsync("localhost", null, SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12, false),
                        server.AuthenticateAsServerAsync(TestConfiguration.ServerCertificate));
#pragma warning restore SYSLIB0039                        

                    _log.WriteLine("Client authenticated to server({0}) with encryption cipher: {1} {2}-bit strength",
                        serverStream.Socket.RemoteEndPoint, client.CipherAlgorithm, client.CipherStrength);

                    Assert.Equal(CipherAlgorithmType.Null, client.CipherAlgorithm);
                    Assert.Equal(0, client.CipherStrength);
                }
            }
        }
    }
}
