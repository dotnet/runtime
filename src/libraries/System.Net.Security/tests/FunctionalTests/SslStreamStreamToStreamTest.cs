// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public abstract class SslStreamStreamToStreamTest
    {
        private readonly byte[] _sampleMsg = Encoding.UTF8.GetBytes("Sample Test Message");

        protected abstract Task DoHandshake(SslStream clientSslStream, SslStream serverSslStream);

        [Fact]
        public async Task SslStream_StreamToStream_Authentication_Success()
        {
            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new SslStream(clientStream, false, AllowAnyServerCertificate))
            using (var server = new SslStream(serverStream))
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                await DoHandshake(client, server);
                Assert.True(client.IsAuthenticated);
                Assert.True(server.IsAuthenticated);
            }
        }

        [Fact]
        public async Task SslStream_StreamToStream_Authentication_IncorrectServerName_Fail()
        {
            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new SslStream(clientStream))
            using (var server = new SslStream(serverStream))
            using (var certificate = Configuration.Certificates.GetServerCertificate())
            {
                Task t1 = client.AuthenticateAsClientAsync("incorrectServer");
                Task t2 = server.AuthenticateAsServerAsync(certificate);

                await Assert.ThrowsAsync<AuthenticationException>(() => t1);
                await t2;
            }
        }

        [Fact]
        public async Task SslStream_StreamToStream_Successive_ClientWrite_Sync_Success()
        {
            byte[] recvBuf = new byte[_sampleMsg.Length];
            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var clientSslStream = new SslStream(clientStream, false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(serverStream))
            {
                await DoHandshake(clientSslStream, serverSslStream);
                
                clientSslStream.Write(_sampleMsg);

                int bytesRead = 0;
                while (bytesRead < _sampleMsg.Length)
                {
                    bytesRead += serverSslStream.Read(recvBuf, bytesRead, _sampleMsg.Length - bytesRead);
                }

                Assert.True(VerifyOutput(recvBuf, _sampleMsg), "verify first read data is as expected.");

                clientSslStream.Write(_sampleMsg);

                bytesRead = 0;
                while (bytesRead < _sampleMsg.Length)
                {
                    bytesRead += serverSslStream.Read(recvBuf, bytesRead, _sampleMsg.Length - bytesRead);
                }

                Assert.True(VerifyOutput(recvBuf, _sampleMsg), "verify second read data is as expected.");
            }
        }

        [Fact]
        public async Task SslStream_StreamToStream_Successive_ClientWrite_WithZeroBytes_Success()
        {
            byte[] recvBuf = new byte[_sampleMsg.Length];
            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var clientSslStream = new SslStream(clientStream, false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(serverStream))
            {
                await DoHandshake(clientSslStream, serverSslStream);
                
                clientSslStream.Write(Array.Empty<byte>());
                await clientSslStream.WriteAsync(Array.Empty<byte>(), 0, 0);
                clientSslStream.Write(_sampleMsg);

                int bytesRead = 0;
                while (bytesRead < _sampleMsg.Length)
                {
                    bytesRead += serverSslStream.Read(recvBuf, bytesRead, _sampleMsg.Length - bytesRead);
                }

                Assert.True(VerifyOutput(recvBuf, _sampleMsg), "verify first read data is as expected.");

                clientSslStream.Write(_sampleMsg);
                await clientSslStream.WriteAsync(Array.Empty<byte>(), 0, 0);
                clientSslStream.Write(Array.Empty<byte>());

                bytesRead = 0;
                while (bytesRead < _sampleMsg.Length)
                {
                    bytesRead += serverSslStream.Read(recvBuf, bytesRead, _sampleMsg.Length - bytesRead);
                }
                Assert.True(VerifyOutput(recvBuf, _sampleMsg), "verify second read data is as expected.");
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SslStream_StreamToStream_LargeWrites_Sync_Success(bool randomizedData)
        {
            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var clientSslStream = new SslStream(clientStream, false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(serverStream))
            {
                await DoHandshake(clientSslStream, serverSslStream);

                byte[] largeMsg = new byte[4096 * 5]; // length longer than max read chunk size (16K + headers)
                if (randomizedData)
                {
                    new Random().NextBytes(largeMsg); // not very compressible
                }
                else
                {
                    for (int i = 0; i < largeMsg.Length; i++)
                    {
                        largeMsg[i] = unchecked((byte)i); // very compressible
                    }
                }
                byte[] receivedLargeMsg = new byte[largeMsg.Length];

                // First do a large write and read blocks at a time
                clientSslStream.Write(largeMsg);
                int bytesRead = 0, totalRead = 0;
                while (totalRead < largeMsg.Length &&
                    (bytesRead = serverSslStream.Read(receivedLargeMsg, totalRead, receivedLargeMsg.Length - totalRead)) != 0)
                {
                    totalRead += bytesRead;
                }
                Assert.Equal(receivedLargeMsg.Length, totalRead);
                Assert.Equal(largeMsg, receivedLargeMsg);

                // Then write again and read bytes at a time
                clientSslStream.Write(largeMsg);
                foreach (byte b in largeMsg)
                {
                    Assert.Equal(b, serverSslStream.ReadByte());
                }
            }
        }

        [Fact]
        public async Task SslStream_StreamToStream_Successive_ClientWrite_Async_Success()
        {
            byte[] recvBuf = new byte[_sampleMsg.Length];
            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var clientSslStream = new SslStream(clientStream, false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(serverStream))
            {
                await DoHandshake(clientSslStream, serverSslStream);
                                
                await clientSslStream.WriteAsync(_sampleMsg, 0, _sampleMsg.Length)
                    .TimeoutAfter(TestConfiguration.PassingTestTimeoutMilliseconds);

                int bytesRead = 0;
                while (bytesRead < _sampleMsg.Length)
                {
                    bytesRead += await serverSslStream.ReadAsync(recvBuf, bytesRead, _sampleMsg.Length - bytesRead)
                        .TimeoutAfter(TestConfiguration.PassingTestTimeoutMilliseconds);
                }

                Assert.True(VerifyOutput(recvBuf, _sampleMsg), "verify first read data is as expected.");

                await clientSslStream.WriteAsync(_sampleMsg, 0, _sampleMsg.Length)
                    .TimeoutAfter(TestConfiguration.PassingTestTimeoutMilliseconds);

                bytesRead = 0;
                while (bytesRead < _sampleMsg.Length)
                {
                    bytesRead += await serverSslStream.ReadAsync(recvBuf, bytesRead, _sampleMsg.Length - bytesRead)
                        .TimeoutAfter(TestConfiguration.PassingTestTimeoutMilliseconds);
                }

                Assert.True(VerifyOutput(recvBuf, _sampleMsg), "verify second read data is as expected.");
            }
        }

        [Fact]
        public async Task SslStream_StreamToStream_Write_ReadByte_Success()
        {
            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var clientSslStream = new SslStream(clientStream, false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(serverStream))
            {
                await DoHandshake(clientSslStream, serverSslStream);
                
                for (int i = 0; i < 3; i++)
                {
                    clientSslStream.Write(_sampleMsg);
                    foreach (byte b in _sampleMsg)
                    {
                        Assert.Equal(b, serverSslStream.ReadByte());
                    }
                }
            }
        }

        [Fact]
        public async Task SslStream_StreamToStream_WriteAsync_ReadByte_Success()
        {
            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var clientSslStream = new SslStream(clientStream, false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(serverStream))
            {
                await DoHandshake(clientSslStream, serverSslStream);
                
                for (int i = 0; i < 3; i++)
                {
                    await clientSslStream.WriteAsync(_sampleMsg, 0, _sampleMsg.Length).ConfigureAwait(false);
                    foreach (byte b in _sampleMsg)
                    {
                        Assert.Equal(b, serverSslStream.ReadByte());
                    }
                }
            }
        }

        [Fact]
        public async Task SslStream_StreamToStream_WriteAsync_ReadAsync_Pending_Success()
        {
            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new NotifyReadVirtualNetworkStream(network, isServer: true))
            using (var clientSslStream = new SslStream(clientStream, false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(serverStream))
            {
                await DoHandshake(clientSslStream, serverSslStream);
                
                var serverBuffer = new byte[1];
                var tcs = new TaskCompletionSource<object>();
                serverStream.OnRead += (buffer, offset, count) =>
                {
                    tcs.TrySetResult(null);
                };
                Task readTask = serverSslStream.ReadAsync(serverBuffer, 0, serverBuffer.Length);

                // Since the sequence of calls that ends in serverStream.Read() is sync, by now
                // the read task will have acquired the semaphore shared by Stream.BeginReadInternal()
                // and Stream.BeginWriteInternal().
                // But to be sure, we wait until we know we're inside Read().
                await tcs.Task.TimeoutAfter(TestConfiguration.PassingTestTimeoutMilliseconds);

                // Should not hang
                await serverSslStream.WriteAsync(new byte[] { 1 }, 0, 1)
                    .TimeoutAfter(TestConfiguration.PassingTestTimeoutMilliseconds);

                // Read in client
                var clientBuffer = new byte[1];
                await clientSslStream.ReadAsync(clientBuffer, 0, clientBuffer.Length);
                Assert.Equal(1, clientBuffer[0]);

                // Complete server read task
                await clientSslStream.WriteAsync(new byte[] { 2 }, 0, 1);
                await readTask;
                Assert.Equal(2, serverBuffer[0]);
            }
        }

        [Fact]
        public void SslStream_StreamToStream_Flush_Propagated()
        {
            VirtualNetwork network = new VirtualNetwork();

            using (var stream = new VirtualNetworkStream(network, isServer: false))
            using (var sslStream = new SslStream(stream, false, AllowAnyServerCertificate))
            {
                Assert.False(stream.HasBeenSyncFlushed);
                sslStream.Flush();
                Assert.True(stream.HasBeenSyncFlushed);
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Relies on FlushAsync override not available in desktop")]
        public void SslStream_StreamToStream_FlushAsync_Propagated()
        {
            VirtualNetwork network = new VirtualNetwork();

            using (var stream = new VirtualNetworkStream(network, isServer: false))
            using (var sslStream = new SslStream(stream, false, AllowAnyServerCertificate))
            {
                Task task = sslStream.FlushAsync();

                Assert.False(task.IsCompleted);
                stream.CompleteAsyncFlush();
                Assert.True(task.IsCompleted);
            }
        }

        private bool VerifyOutput(byte[] actualBuffer, byte[] expectedBuffer)
        {
            return expectedBuffer.SequenceEqual(actualBuffer);
        }

        protected bool AllowAnyServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            SslPolicyErrors expectedSslPolicyErrors = SslPolicyErrors.None;

            if (!Capability.IsTrustedRootCertificateInstalled())
            {
                expectedSslPolicyErrors = SslPolicyErrors.RemoteCertificateChainErrors;
            }

            Assert.Equal(expectedSslPolicyErrors, sslPolicyErrors);

            if (sslPolicyErrors == expectedSslPolicyErrors)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public sealed class SslStreamStreamToStreamTest_Async : SslStreamStreamToStreamTest
    {
        protected override async Task DoHandshake(SslStream clientSslStream, SslStream serverSslStream)
        {
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                Task t1 = clientSslStream.AuthenticateAsClientAsync(certificate.GetNameInfo(X509NameType.SimpleName, false));
                Task t2 = serverSslStream.AuthenticateAsServerAsync(certificate);
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);
            }
        }
    }

    public sealed class SslStreamStreamToStreamTest_BeginEnd : SslStreamStreamToStreamTest
    {
        protected override async Task DoHandshake(SslStream clientSslStream, SslStream serverSslStream)
        {
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                Task t1 = Task.Factory.FromAsync(clientSslStream.BeginAuthenticateAsClient(certificate.GetNameInfo(X509NameType.SimpleName, false), null, null), clientSslStream.EndAuthenticateAsClient);
                Task t2 = Task.Factory.FromAsync(serverSslStream.BeginAuthenticateAsServer(certificate, null, null), serverSslStream.EndAuthenticateAsServer);
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);
            }
        }
    }

    public sealed class SslStreamStreamToStreamTest_Sync : SslStreamStreamToStreamTest
    {
        protected override async Task DoHandshake(SslStream clientSslStream, SslStream serverSslStream)
        {
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                Task t1 = Task.Run(() => clientSslStream.AuthenticateAsClient(certificate.GetNameInfo(X509NameType.SimpleName, false)));
                Task t2 = Task.Run(() => serverSslStream.AuthenticateAsServer(certificate));
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);
            }
        }
    }
}
