// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
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

        protected abstract Task<int> ReadAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);

        protected abstract Task WriteAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);

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
        public async Task SslStream_ServerLocalCertificateSelectionCallbackReturnsNull_Throw()
        {
            VirtualNetwork network = new VirtualNetwork();

            var selectionCallback = new LocalCertificateSelectionCallback((object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] issuers) =>
            {
                return null;
            });

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new SslStream(clientStream, false, AllowAnyServerCertificate))
            using (var server = new SslStream(serverStream, false, null, selectionCallback))
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                await Assert.ThrowsAsync<NotSupportedException>(async () =>
                    await TestConfiguration.WhenAllOrAnyFailedWithTimeout(client.AuthenticateAsClientAsync(certificate.GetNameInfo(X509NameType.SimpleName, false)), server.AuthenticateAsServerAsync(certificate))
                );
            }
        }

        [Fact]
        public async Task Read_CorrectlyUnlocksAfterFailure()
        {
            var network = new VirtualNetwork();
            var clientStream = new ThrowingDelegatingStream(new VirtualNetworkStream(network, isServer: false));
            using (var clientSslStream = new SslStream(clientStream, false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(new VirtualNetworkStream(network, isServer: true)))
            {
                await DoHandshake(clientSslStream, serverSslStream);

                // Throw an exception from the wrapped stream's read operation
                clientStream.ExceptionToThrow = new FormatException();
                IOException thrown = await Assert.ThrowsAsync<IOException>(() => ReadAsync(clientSslStream, new byte[1], 0, 1));
                Assert.Same(clientStream.ExceptionToThrow, thrown.InnerException);
                clientStream.ExceptionToThrow = null;

                // Validate that the SslStream continues to be usable
                for (byte b = 42; b < 52; b++) // arbitrary test values
                {
                    await WriteAsync(serverSslStream, new byte[1] { b }, 0, 1);
                    byte[] buffer = new byte[1];
                    Assert.Equal(1, await ReadAsync(clientSslStream, buffer, 0, 1));
                    Assert.Equal(b, buffer[0]);
                }
            }
        }

        [Fact]
        public async Task Write_CorrectlyUnlocksAfterFailure()
        {
            var network = new VirtualNetwork();
            var clientStream = new ThrowingDelegatingStream(new VirtualNetworkStream(network, isServer: false));
            using (var clientSslStream = new SslStream(clientStream, false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(new VirtualNetworkStream(network, isServer: true)))
            {
                await DoHandshake(clientSslStream, serverSslStream);

                // Throw an exception from the wrapped stream's write operation
                clientStream.ExceptionToThrow = new FormatException();
                IOException thrown = await Assert.ThrowsAsync<IOException>(() => WriteAsync(clientSslStream, new byte[1], 0, 1));
                Assert.Same(clientStream.ExceptionToThrow, thrown.InnerException);
                clientStream.ExceptionToThrow = null;

                // Validate that the SslStream continues to be writable. However, the stream is still largely
                // unusable: because the previously encrypted data won't have been written to the underlying
                // stream and thus not received by the reader, if we tried to validate this data being received
                // by the reader, it would likely fail with a decryption error.
                await WriteAsync(clientSslStream, new byte[1] { 42 }, 0, 1);
            }
        }

        [Fact]
        public async Task Read_InvokedSynchronously()
        {
            var network = new VirtualNetwork();
            var clientStream = new PreReadWriteActionDelegatingStream(new VirtualNetworkStream(network, isServer: false));
            using (var clientSslStream = new SslStream(clientStream, false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(new VirtualNetworkStream(network, isServer: true)))
            {
                await DoHandshake(clientSslStream, serverSslStream);

                // Validate that the read call to the underlying stream is made as part of the
                // synchronous call to the read method on SslStream, even if the method is async.
                using (var tl = new ThreadLocal<int>())
                {
                    await WriteAsync(serverSslStream, new byte[1], 0, 1);
                    tl.Value = 42;
                    clientStream.PreReadWriteAction = () => Assert.Equal(42, tl.Value);
                    Task t = ReadAsync(clientSslStream, new byte[1], 0, 1);
                    tl.Value = 0;
                    await t;
                }
            }
        }

        [Fact]
        public async Task Write_InvokedSynchronously()
        {
            var network = new VirtualNetwork();
            var clientStream = new PreReadWriteActionDelegatingStream(new VirtualNetworkStream(network, isServer: false));
            using (var clientSslStream = new SslStream(clientStream, false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(new VirtualNetworkStream(network, isServer: true)))
            {
                await DoHandshake(clientSslStream, serverSslStream);

                // Validate that the write call to the underlying stream is made as part of the
                // synchronous call to the write method on SslStream, even if the method is async.
                using (var tl = new ThreadLocal<int>())
                {
                    tl.Value = 42;
                    clientStream.PreReadWriteAction = () => Assert.Equal(42, tl.Value);
                    Task t = WriteAsync(clientSslStream, new byte[1], 0, 1);
                    tl.Value = 0;
                    await t;
                }
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

                await WriteAsync(clientSslStream, Array.Empty<byte>(), 0, 0);
                await WriteAsync(clientSslStream, _sampleMsg, 0, _sampleMsg.Length);

                int bytesRead = 0;
                while (bytesRead < _sampleMsg.Length)
                {
                    bytesRead += await ReadAsync(serverSslStream, recvBuf, bytesRead, _sampleMsg.Length - bytesRead);
                }

                Assert.True(VerifyOutput(recvBuf, _sampleMsg), "verify first read data is as expected.");

                await WriteAsync(clientSslStream, _sampleMsg, 0, _sampleMsg.Length);
                await WriteAsync(clientSslStream, Array.Empty<byte>(), 0, 0);

                bytesRead = 0;
                while (bytesRead < _sampleMsg.Length)
                {
                    bytesRead += await ReadAsync(serverSslStream, recvBuf, bytesRead, _sampleMsg.Length - bytesRead);
                }
                Assert.True(VerifyOutput(recvBuf, _sampleMsg), "verify second read data is as expected.");
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SslStream_StreamToStream_LargeWrites_Success(bool randomizedData)
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
                await WriteAsync(clientSslStream, largeMsg, 0, largeMsg.Length);
                int bytesRead = 0, totalRead = 0;
                while (totalRead < largeMsg.Length &&
                    (bytesRead = await ReadAsync(serverSslStream, receivedLargeMsg, totalRead, receivedLargeMsg.Length - totalRead)) != 0)
                {
                    totalRead += bytesRead;
                }
                Assert.Equal(receivedLargeMsg.Length, totalRead);
                Assert.Equal(largeMsg, receivedLargeMsg);

                // Then write again and read bytes at a time
                await WriteAsync(clientSslStream, largeMsg, 0, largeMsg.Length);
                foreach (byte b in largeMsg)
                {
                    Assert.Equal(b, serverSslStream.ReadByte());
                }
            }
        }

        [Fact]
        public async Task SslStream_StreamToStream_Successive_ClientWrite_Success()
        {
            byte[] recvBuf = new byte[_sampleMsg.Length];
            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var clientSslStream = new SslStream(clientStream, false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(serverStream))
            {
                await DoHandshake(clientSslStream, serverSslStream);

                await WriteAsync(clientSslStream, _sampleMsg, 0, _sampleMsg.Length)
                    .TimeoutAfter(TestConfiguration.PassingTestTimeoutMilliseconds);

                int bytesRead = 0;
                while (bytesRead < _sampleMsg.Length)
                {
                    bytesRead += await ReadAsync(serverSslStream, recvBuf, bytesRead, _sampleMsg.Length - bytesRead)
                        .TimeoutAfter(TestConfiguration.PassingTestTimeoutMilliseconds);
                }

                Assert.True(VerifyOutput(recvBuf, _sampleMsg), "verify first read data is as expected.");

                await WriteAsync(clientSslStream, _sampleMsg, 0, _sampleMsg.Length)
                    .TimeoutAfter(TestConfiguration.PassingTestTimeoutMilliseconds);

                bytesRead = 0;
                while (bytesRead < _sampleMsg.Length)
                {
                    bytesRead += await ReadAsync(serverSslStream, recvBuf, bytesRead, _sampleMsg.Length - bytesRead)
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
                    await WriteAsync(clientSslStream, _sampleMsg, 0, _sampleMsg.Length);
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
                    await WriteAsync(clientSslStream, _sampleMsg, 0, _sampleMsg.Length).ConfigureAwait(false);
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
            if (this is SslStreamStreamToStreamTest_Sync)
            {
                // This test assumes operations complete asynchronously.
                return;
            }

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
                Task readTask = ReadAsync(serverSslStream, serverBuffer, 0, serverBuffer.Length);

                // Since the sequence of calls that ends in serverStream.Read() is sync, by now
                // the read task will have acquired the semaphore shared by Stream.BeginReadInternal()
                // and Stream.BeginWriteInternal().
                // But to be sure, we wait until we know we're inside Read().
                await tcs.Task.TimeoutAfter(TestConfiguration.PassingTestTimeoutMilliseconds);

                // Should not hang
                await WriteAsync(serverSslStream, new byte[] { 1 }, 0, 1)
                    .TimeoutAfter(TestConfiguration.PassingTestTimeoutMilliseconds);

                // Read in client
                var clientBuffer = new byte[1];
                await ReadAsync(clientSslStream, clientBuffer, 0, clientBuffer.Length);
                Assert.Equal(1, clientBuffer[0]);

                // Complete server read task
                await WriteAsync(clientSslStream, new byte[] { 2 }, 0, 1);
                await readTask;
                Assert.Equal(2, serverBuffer[0]);
            }
        }

        [Fact]
        public async Task SslStream_ConcurrentBidirectionalReadsWrites_Success()
        {
            VirtualNetwork network = new VirtualNetwork();
            using (var clientSslStream = new SslStream(new VirtualNetworkStream(network, isServer: false), false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(new NotifyReadVirtualNetworkStream(network, isServer: true)))
            {
                await DoHandshake(clientSslStream, serverSslStream);

                const int BytesPerSend = 100;
                DateTime endTime = DateTime.UtcNow + TimeSpan.FromSeconds(3);
                await new Task[]
                {
                    Task.Run(async delegate
                    {
                        var buffer = new byte[BytesPerSend];
                        while (DateTime.UtcNow < endTime)
                        {
                            await WriteAsync(clientSslStream, buffer, 0, buffer.Length);
                            int received = 0, bytesRead = 0;
                            while (received < BytesPerSend && (bytesRead = await ReadAsync(serverSslStream, buffer, 0, buffer.Length)) != 0)
                            {
                                received += bytesRead;
                            }
                            Assert.NotEqual(0, bytesRead);
                        }
                    }),
                    Task.Run(async delegate
                    {
                        var buffer = new byte[BytesPerSend];
                        while (DateTime.UtcNow < endTime)
                        {
                            await WriteAsync(serverSslStream, buffer, 0, buffer.Length);
                            int received = 0, bytesRead = 0;
                            while (received < BytesPerSend && (bytesRead = await ReadAsync(clientSslStream, buffer, 0, buffer.Length)) != 0)
                            {
                                received += bytesRead;
                            }
                            Assert.NotEqual(0, bytesRead);
                        }
                    })
                }.WhenAllOrAnyFailed();
            }
        }

        [Fact]
        public async Task SslStream_StreamToStream_Dispose_Throws()
        {
            if (this is SslStreamStreamToStreamTest_Sync)
            {
                // This test assumes operations complete asynchronously.
                return;
            }

            VirtualNetwork network = new VirtualNetwork()
            {
                DisableConnectionBreaking = true
            };

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var clientSslStream = new SslStream(clientStream, false, AllowAnyServerCertificate))
            {
                var serverSslStream = new SslStream(serverStream);
                await DoHandshake(clientSslStream, serverSslStream);

                var serverBuffer = new byte[1];
                Task serverReadTask = ReadAsync(serverSslStream, serverBuffer, 0, serverBuffer.Length);
                await WriteAsync(serverSslStream, new byte[] { 1 }, 0, 1)
                    .TimeoutAfter(TestConfiguration.PassingTestTimeoutMilliseconds);

                // Shouldn't throw, the context is diposed now.
                // Since the server read task is in progress, the read buffer is not returned to ArrayPool.
                serverSslStream.Dispose();

                // Read in client
                var clientBuffer = new byte[1];
                await ReadAsync(clientSslStream, clientBuffer, 0, clientBuffer.Length);
                Assert.Equal(1, clientBuffer[0]);

                await WriteAsync(clientSslStream, new byte[] { 2 }, 0, 1);

                // We're inconsistent as to whether the ObjectDisposedException is thrown directly
                // or wrapped in an IOException.  For Begin/End, it's always wrapped; for Async,
                // it's only wrapped on netfx.
                if (this is SslStreamStreamToStreamTest_BeginEnd || PlatformDetection.IsFullFramework)
                {
                    await Assert.ThrowsAsync<ObjectDisposedException>(() => serverReadTask);
                }
                else
                {
                    IOException serverException = await Assert.ThrowsAsync<IOException>(() => serverReadTask);
                    Assert.IsType<ObjectDisposedException>(serverException.InnerException);
                }

                await Assert.ThrowsAsync<ObjectDisposedException>(() => ReadAsync(serverSslStream, serverBuffer, 0, serverBuffer.Length));

                // Now, there is no pending read, so the internal buffer will be returned to ArrayPool.
                serverSslStream.Dispose();
                await Assert.ThrowsAsync<ObjectDisposedException>(() => ReadAsync(serverSslStream, serverBuffer, 0, serverBuffer.Length));
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

        [Fact]
        public async Task SslStream_StreamToStream_EOFDuringFrameRead_ThrowsIOException()
        {
            var network = new VirtualNetwork();
            using (var clientNetworkStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverNetworkStream = new VirtualNetworkStream(network, isServer: true))
            {
                int readMode = 0;
                var serverWrappedNetworkStream = new DelegateStream(
                    canWriteFunc: () => true,
                    canReadFunc: () => true,
                    writeFunc: (buffer, offset, count) => serverNetworkStream.Write(buffer, offset, count),
                    writeAsyncFunc: (buffer, offset, count, token) => serverNetworkStream.WriteAsync(buffer, offset, count, token),
                    readFunc: (buffer, offset, count) =>
                    {
                        // Do normal reads as requested until the read mode is set
                        // to 1.  Then do a single read of only 10 bytes to read only
                        // part of the message, and subsequently return EOF.
                        if (readMode == 0)
                        {
                            return serverNetworkStream.Read(buffer, offset, count);
                        }
                        else if (readMode == 1)
                        {
                            readMode = 2;
                            return serverNetworkStream.Read(buffer, offset, 10); // read at least header but less than full frame
                        }
                        else
                        {
                            return 0;
                        }
                    },
                    readAsyncFunc: (buffer, offset, count, token) =>
                    {
                        // Do normal reads as requested until the read mode is set
                        // to 1.  Then do a single read of only 10 bytes to read only
                        // part of the message, and subsequently return EOF.
                        if (readMode == 0)
                        {
                            return serverNetworkStream.ReadAsync(buffer, offset, count);
                        }
                        else if (readMode == 1)
                        {
                            readMode = 2;
                            return serverNetworkStream.ReadAsync(buffer, offset, 10); // read at least header but less than full frame
                        }
                        else
                        {
                            return Task.FromResult(0);
                        }
                    });

                using (var clientSslStream = new SslStream(clientNetworkStream, false, AllowAnyServerCertificate))
                using (var serverSslStream = new SslStream(serverWrappedNetworkStream))
                {
                    await DoHandshake(clientSslStream, serverSslStream);
                    await WriteAsync(clientSslStream, new byte[20], 0, 20);
                    readMode = 1;
                    await Assert.ThrowsAsync<IOException>(() => ReadAsync(serverSslStream, new byte[1], 0, 1));
                }
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

        private class PreReadWriteActionDelegatingStream : Stream
        {
            private readonly Stream _stream;

            public PreReadWriteActionDelegatingStream(Stream stream) => _stream = stream;

            public Action PreReadWriteAction { get; set; }

            public override bool CanRead => _stream.CanRead;
            public override bool CanWrite => _stream.CanWrite;
            public override bool CanSeek => _stream.CanSeek;
            protected override void Dispose(bool disposing) => _stream.Dispose();
            public override long Length => _stream.Length;
            public override long Position { get => _stream.Position; set => _stream.Position = value; }
            public override void Flush() => _stream.Flush();
            public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);
            public override void SetLength(long value) => _stream.SetLength(value);

            public override int Read(byte[] buffer, int offset, int count)
            {
                PreReadWriteAction?.Invoke();
                return _stream.Read(buffer, offset, count);
            }

            public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            {
                PreReadWriteAction?.Invoke();
                return _stream.BeginRead(buffer, offset, count, callback, state);
            }

            public override int EndRead(IAsyncResult asyncResult) => _stream.EndRead(asyncResult);

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                PreReadWriteAction?.Invoke();
                return _stream.ReadAsync(buffer, offset, count, cancellationToken);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                PreReadWriteAction?.Invoke();
                _stream.Write(buffer, offset, count);
            }

            public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            {
                PreReadWriteAction?.Invoke();
                return _stream.BeginWrite(buffer, offset, count, callback, state);
            }

            public override void EndWrite(IAsyncResult asyncResult) => _stream.EndWrite(asyncResult);

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                PreReadWriteAction?.Invoke();
                return _stream.WriteAsync(buffer, offset, count, cancellationToken);
            }
        }

        private sealed class ThrowingDelegatingStream : PreReadWriteActionDelegatingStream
        {
            public ThrowingDelegatingStream(Stream stream) : base(stream)
            {
                PreReadWriteAction = () =>
                {
                    if (ExceptionToThrow != null)
                    {
                        throw ExceptionToThrow;
                    }
                };
            }

            public Exception ExceptionToThrow { get; set; }
        }
    }

    public abstract class SslStreamStreamToStreamTest_CancelableReadWriteAsync : SslStreamStreamToStreamTest
    {
        [Fact]
        public async Task ReadAsync_WriteAsync_Precanceled_ThrowsOperationCanceledException()
        {
            var network = new VirtualNetwork();
            using (var clientSslStream = new SslStream(new VirtualNetworkStream(network, isServer: false), false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(new VirtualNetworkStream(network, isServer: true)))
            {
                await DoHandshake(clientSslStream, serverSslStream);
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ReadAsync(clientSslStream, new byte[1], 0, 1, new CancellationToken(true)));
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => WriteAsync(serverSslStream, new byte[1], 0, 1, new CancellationToken(true)));
            }
        }

        [Fact]
        public async Task ReadAsync_CanceledAfterStart_ThrowsOperationCanceledException()
        {
            var network = new VirtualNetwork();
            using (var clientSslStream = new SslStream(new VirtualNetworkStream(network, isServer: false), false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(new VirtualNetworkStream(network, isServer: true)))
            {
                await DoHandshake(clientSslStream, serverSslStream);

                var cts = new CancellationTokenSource();

                Task t = ReadAsync(clientSslStream, new byte[1], 0, 1, cts.Token);
                Assert.False(t.IsCompleted);

                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);
            }
        }
    }

    public sealed class SslStreamStreamToStreamTest_Async : SslStreamStreamToStreamTest_CancelableReadWriteAsync
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

        protected override Task<int> ReadAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            stream.ReadAsync(buffer, offset, count, cancellationToken);

        protected override Task WriteAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            stream.WriteAsync(buffer, offset, count, cancellationToken);
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

        protected override Task<int> ReadAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            cancellationToken.IsCancellationRequested ?
                Task.FromCanceled<int>(cancellationToken) :
                Task.Factory.FromAsync(stream.BeginRead, stream.EndRead, buffer, offset, count, null);

        protected override Task WriteAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            cancellationToken.IsCancellationRequested ?
                Task.FromCanceled<int>(cancellationToken) :
                Task.Factory.FromAsync(stream.BeginWrite, stream.EndWrite, buffer, offset, count, null);
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

        protected override Task<int> ReadAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }

            try
            {
                return Task.FromResult<int>(stream.Read(buffer, offset, count));
            }
            catch (Exception e)
            {
                return Task.FromException<int>(e);
            }
        }

        protected override Task WriteAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }

            try
            {
                stream.Write(buffer, offset, count);
                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                return Task.FromException<int>(e);
            }
        }
    }
}
