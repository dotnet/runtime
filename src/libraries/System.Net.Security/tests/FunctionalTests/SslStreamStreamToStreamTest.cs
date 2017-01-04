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

        protected abstract bool DoHandshake(SslStream clientSslStream, SslStream serverSslStream);

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SslStream_StreamToStream_Authentication_Success()
        {
            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new SslStream(clientStream, false, AllowAnyServerCertificate))
            using (var server = new SslStream(serverStream))
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                Assert.True(DoHandshake(client, server), "Handshake completed in the allotted time");
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SslStream_StreamToStream_Authentication_IncorrectServerName_Fail()
        {
            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new SslStream(clientStream))
            using (var server = new SslStream(serverStream))
            using (var certificate = Configuration.Certificates.GetServerCertificate())
            {
                Task[] auth = new Task[2];
                auth[0] = client.AuthenticateAsClientAsync("incorrectServer");
                auth[1] = server.AuthenticateAsServerAsync(certificate);

                Assert.Throws<AuthenticationException>(() =>
                {
                    auth[0].GetAwaiter().GetResult();
                });

                auth[1].GetAwaiter().GetResult();
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SslStream_StreamToStream_Successive_ClientWrite_Sync_Success()
        {
            byte[] recvBuf = new byte[_sampleMsg.Length];
            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var clientSslStream = new SslStream(clientStream, false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(serverStream))
            {
                bool result = DoHandshake(clientSslStream, serverSslStream);

                Assert.True(result, "Handshake completed.");

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

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SslStream_StreamToStream_Successive_ClientWrite_WithZeroBytes_Success()
        {
            byte[] recvBuf = new byte[_sampleMsg.Length];
            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var clientSslStream = new SslStream(clientStream, false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(serverStream))
            {
                bool result = DoHandshake(clientSslStream, serverSslStream);

                Assert.True(result, "Handshake completed.");

                clientSslStream.Write(Array.Empty<byte>());
                clientSslStream.WriteAsync(Array.Empty<byte>(), 0, 0).Wait();
                clientSslStream.Write(_sampleMsg);

                int bytesRead = 0;
                while (bytesRead < _sampleMsg.Length)
                {
                    bytesRead += serverSslStream.Read(recvBuf, bytesRead, _sampleMsg.Length - bytesRead);
                }

                Assert.True(VerifyOutput(recvBuf, _sampleMsg), "verify first read data is as expected.");

                clientSslStream.Write(_sampleMsg);
                clientSslStream.WriteAsync(Array.Empty<byte>(), 0, 0).Wait();
                clientSslStream.Write(Array.Empty<byte>());

                bytesRead = 0;
                while (bytesRead < _sampleMsg.Length)
                {
                    bytesRead += serverSslStream.Read(recvBuf, bytesRead, _sampleMsg.Length - bytesRead);
                }
                Assert.True(VerifyOutput(recvBuf, _sampleMsg), "verify second read data is as expected.");
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SslStream_StreamToStream_LargeWrites_Sync_Success(bool randomizedData)
        {
            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer:false))
            using (var serverStream = new VirtualNetworkStream(network, isServer:true))
            using (var clientSslStream = new SslStream(clientStream, false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(serverStream))
            {
                Assert.True(DoHandshake(clientSslStream, serverSslStream), "Handshake complete");

                byte[] largeMsg = new byte[4096 * 5]; // length longer than max read chunk size (16K + headers)
                if (randomizedData)
                {
                    new Random().NextBytes(largeMsg); // not very compressible
                }
                else
                {
                    for (int i = 0; i < largeMsg.Length; i++)
                    {
                        largeMsg[i] = (byte)i; // very compressible
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

        [OuterLoop] // TODO: Issue #11345
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
                bool result = DoHandshake(clientSslStream, serverSslStream);

                Assert.True(result, "Handshake completed.");

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

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SslStream_StreamToStream_Write_ReadByte_Success()
        {
            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer:false))
            using (var serverStream = new VirtualNetworkStream(network, isServer:true))
            using (var clientSslStream = new SslStream(clientStream, false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(serverStream))
            {
                bool result = DoHandshake(clientSslStream, serverSslStream);
                Assert.True(result, "Handshake completed.");

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

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task SslStream_StreamToStream_WriteAsync_ReadByte_Success()
        {
            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var clientSslStream = new SslStream(clientStream, false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(serverStream))
            {
                bool result = DoHandshake(clientSslStream, serverSslStream);
                Assert.True(result, "Handshake completed.");

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

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task SslStream_StreamToStream_WriteAsync_ReadAsync_Pending_Success()
        {
            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new NotifyReadVirtualNetworkStream(network, isServer: true))
            using (var clientSslStream = new SslStream(clientStream, false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(serverStream))
            {
                bool result = DoHandshake(clientSslStream, serverSslStream);
                Assert.True(result, "Handshake completed.");

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

        [OuterLoop] // TODO: Issue #11345
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

        [OuterLoop] // TODO: Issue #11345
        [Fact]
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

        private bool AllowAnyServerCertificate(
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
        protected override bool DoHandshake(SslStream clientSslStream, SslStream serverSslStream)
        {
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                Task t1 = clientSslStream.AuthenticateAsClientAsync(certificate.GetNameInfo(X509NameType.SimpleName, false));
                Task t2 = serverSslStream.AuthenticateAsServerAsync(certificate);
                return Task.WaitAll(new[] { t1, t2 }, TestConfiguration.PassingTestTimeoutMilliseconds);
            }
        }
    }

    public sealed class SslStreamStreamToStreamTest_BeginEnd : SslStreamStreamToStreamTest
    {
        protected override bool DoHandshake(SslStream clientSslStream, SslStream serverSslStream)
        {
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                IAsyncResult a1 = clientSslStream.BeginAuthenticateAsClient(certificate.GetNameInfo(X509NameType.SimpleName, false), null, null);
                IAsyncResult a2 = serverSslStream.BeginAuthenticateAsServer(certificate, null, null);
                if (WaitHandle.WaitAll(new[] { a1.AsyncWaitHandle, a2.AsyncWaitHandle }, TestConfiguration.PassingTestTimeoutMilliseconds))
                {
                    clientSslStream.EndAuthenticateAsClient(a1);
                    serverSslStream.EndAuthenticateAsServer(a2);
                    return true;
                }
                return false;
            }
        }
    }

    public sealed class SslStreamStreamToStreamTest_Sync : SslStreamStreamToStreamTest
    {
        protected override bool DoHandshake(SslStream clientSslStream, SslStream serverSslStream)
        {
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                Task t1 = Task.Run(() => clientSslStream.AuthenticateAsClient(certificate.GetNameInfo(X509NameType.SimpleName, false)));
                Task t2 = Task.Run(() => serverSslStream.AuthenticateAsServer(certificate));
                return Task.WaitAll(new[] { t1, t2 }, TestConfiguration.PassingTestTimeoutMilliseconds);
            }
        }
    }
}
