// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public abstract class SslStreamStreamToStreamTest
    {
        private readonly byte[] _sampleMsg = Encoding.UTF8.GetBytes("Sample Test Message");

        protected static async Task WithServerCertificate(X509Certificate serverCertificate, Func<X509Certificate, string, Task> func)
        {
            X509Certificate certificate = serverCertificate ?? Configuration.Certificates.GetServerCertificate();
            try
            {
                string name;
                if (certificate is X509Certificate2 cert2)
                {
                    name = cert2.GetNameInfo(X509NameType.SimpleName, forIssuer: false);
                }
                else
                {
                    using (cert2 = new X509Certificate2(certificate))
                    {
                        name = cert2.GetNameInfo(X509NameType.SimpleName, forIssuer: false);
                    }
                }

                await func(certificate, name).ConfigureAwait(false);
            }
            finally
            {
                if (certificate != serverCertificate)
                {
                    certificate.Dispose();
                }
            }
        }

        protected abstract Task DoHandshake(SslStream clientSslStream, SslStream serverSslStream, X509Certificate serverCertificate = null, X509Certificate clientCertificate = null);

        protected abstract Task<int> ReadAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);

        protected abstract Task WriteAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);

        public static IEnumerable<object[]> SslStream_StreamToStream_Authentication_Success_MemberData()
        {
            using (X509Certificate2 serverCert = Configuration.Certificates.GetServerCertificate())
            using (X509Certificate2 clientCert = Configuration.Certificates.GetClientCertificate())
            {
                yield return new object[] { new X509Certificate2(serverCert), new X509Certificate2(clientCert) };
                yield return new object[] { new X509Certificate(serverCert.Export(X509ContentType.Pfx)), new X509Certificate(clientCert.Export(X509ContentType.Pfx)) };
            }
        }

        [ConditionalTheory]
        [MemberData(nameof(SslStream_StreamToStream_Authentication_Success_MemberData))]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "X509 certificate store is not supported on iOS or tvOS.")]
        public async Task SslStream_StreamToStream_Authentication_Success(X509Certificate serverCert = null, X509Certificate clientCert = null)
        {
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (var client = new SslStream(stream1, false, AllowAnyServerCertificate))
            using (var server = new SslStream(stream2, false, delegate { return true; }))
            {
                await DoHandshake(client, server, serverCert, clientCert);
                Assert.True(client.IsAuthenticated);
                Assert.True(server.IsAuthenticated);
            }

            clientCert?.Dispose();
            serverCert?.Dispose();
        }

        [Fact]
        public async Task SslStream_StreamToStream_Authentication_IncorrectServerName_Fail()
        {
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (var client = new SslStream(stream1))
            using (var server = new SslStream(stream2))
            using (var certificate = Configuration.Certificates.GetServerCertificate())
            {
                Task t1 = client.AuthenticateAsClientAsync("incorrectServer");
                Task t2 = server.AuthenticateAsServerAsync(certificate);

                await Assert.ThrowsAsync<AuthenticationException>(() => t1);
                try
                {
                    await t2;
                }
                catch
                {
                    // Ignore outcome of t2. It can succeed or fail depending on timing.
                }
            }
        }

        [Fact]
        public async Task SslStream_ServerLocalCertificateSelectionCallbackReturnsNull_Throw()
        {
            var selectionCallback = new LocalCertificateSelectionCallback((object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] issuers) =>
            {
                return null;
            });

            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (var client = new SslStream(stream1, false, AllowAnyServerCertificate))
            using (var server = new SslStream(stream2, false, null, selectionCallback))
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                await Assert.ThrowsAsync<NotSupportedException>(async () =>
                    await TestConfiguration.WhenAllOrAnyFailedWithTimeout(client.AuthenticateAsClientAsync(certificate.GetNameInfo(X509NameType.SimpleName, false)), server.AuthenticateAsServerAsync(certificate))
                );
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "X509 certificate store is not supported on iOS or tvOS.")]
        public async Task Read_CorrectlyUnlocksAfterFailure()
        {
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            var clientStream = new ThrowingDelegatingStream(stream1);
            using (var clientSslStream = new SslStream(clientStream, false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(stream2))
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
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "X509 certificate store is not supported on iOS or tvOS.")]
        public async Task Write_CorrectlyUnlocksAfterFailure()
        {
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            var clientStream = new ThrowingDelegatingStream(stream1);
            using (var clientSslStream = new SslStream(clientStream, false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(stream2))
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
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            var clientStream = new PreReadWriteActionDelegatingStream(stream1);
            using (var clientSslStream = new SslStream(clientStream, false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(stream2))
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
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "X509 certificate store is not supported on iOS or tvOS.")]
        public async Task Write_InvokedSynchronously()
        {
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            var clientStream = new PreReadWriteActionDelegatingStream(stream1);
            using (var clientSslStream = new SslStream(clientStream, false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(stream2))
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
        public async Task SslStream_StreamToStream_Dispose_Throws()
        {
            if (this is SslStreamStreamToStreamTest_SyncBase)
            {
                // This test assumes operations complete asynchronously.
                return;
            }

            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (var clientSslStream = new SslStream(DelegateDelegatingStream.NopDispose(stream1), false, AllowAnyServerCertificate))
            {
                var serverSslStream = new SslStream(DelegateDelegatingStream.NopDispose(stream2));
                await DoHandshake(clientSslStream, serverSslStream);

                var serverBuffer = new byte[1];
                Task serverReadTask = ReadAsync(serverSslStream, serverBuffer, 0, serverBuffer.Length);
                await WriteAsync(serverSslStream, new byte[] { 1 }, 0, 1)
                    .WaitAsync(TestConfiguration.PassingTestTimeout);

                // Shouldn't throw, the context is disposed now.
                // Since the server read task is in progress, the read buffer is not returned to ArrayPool.
                serverSslStream.Dispose();

                // Read in client
                var clientBuffer = new byte[1];
                await ReadAsync(clientSslStream, clientBuffer, 0, clientBuffer.Length);
                Assert.Equal(1, clientBuffer[0]);

                await WriteAsync(clientSslStream, new byte[] { 2 }, 0, 1);

                // We're inconsistent as to whether the ObjectDisposedException is thrown directly
                // or wrapped in an IOException.  For Begin/End, it's always wrapped; for Async,
                // it's only wrapped on .NET Framework.
                if (this is SslStreamStreamToStreamTest_BeginEnd || PlatformDetection.IsNetFramework)
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
        public async Task SslStream_StreamToStream_EOFDuringFrameRead_ThrowsIOException()
        {
            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            {
                int readMode = 0;
                var serverWrappedNetworkStream = new DelegateStream(
                    canWriteFunc: () => true,
                    canReadFunc: () => true,
                    writeFunc: (buffer, offset, count) => serverStream.Write(buffer, offset, count),
                    writeAsyncFunc: (buffer, offset, count, token) => serverStream.WriteAsync(buffer, offset, count, token),
                    readFunc: (buffer, offset, count) =>
                    {
                        // Do normal reads as requested until the read mode is set
                        // to 1.  Then do a single read of only 10 bytes to read only
                        // part of the message, and subsequently return EOF.
                        if (readMode == 0)
                        {
                            return serverStream.Read(buffer, offset, count);
                        }
                        else if (readMode == 1)
                        {
                            readMode = 2;
                            return serverStream.Read(buffer, offset, 10); // read at least header but less than full frame
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
                            return serverStream.ReadAsync(buffer, offset, count);
                        }
                        else if (readMode == 1)
                        {
                            readMode = 2;
                            return serverStream.ReadAsync(buffer, offset, 10); // read at least header but less than full frame
                        }
                        else
                        {
                            return Task.FromResult(0);
                        }
                    });

                using (var clientSslStream = new SslStream(clientStream, false, AllowAnyServerCertificate))
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

    public sealed class SslStreamStreamToStreamTest_Async : SslStreamStreamToStreamTest
    {
        protected override async Task DoHandshake(SslStream clientSslStream, SslStream serverSslStream, X509Certificate serverCertificate = null, X509Certificate clientCertificate = null)
        {
            X509CertificateCollection clientCerts = clientCertificate != null ? new X509CertificateCollection() { clientCertificate } : null;
            await WithServerCertificate(serverCertificate, async(certificate, name) =>
            {
                Task t1 = clientSslStream.AuthenticateAsClientAsync(name, clientCerts, SslProtocols.None, checkCertificateRevocation: false);
                Task t2 = serverSslStream.AuthenticateAsServerAsync(certificate, clientCertificateRequired: clientCertificate != null, checkCertificateRevocation: false);
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);
            });
        }

        protected override Task<int> ReadAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            stream.ReadAsync(buffer, offset, count, cancellationToken);

        protected override Task WriteAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            stream.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public sealed class SslStreamStreamToStreamTest_BeginEnd : SslStreamStreamToStreamTest
    {
        protected override async Task DoHandshake(SslStream clientSslStream, SslStream serverSslStream, X509Certificate serverCertificate = null, X509Certificate clientCertificate = null)
        {
            X509CertificateCollection clientCerts = clientCertificate != null ? new X509CertificateCollection() { clientCertificate } : null;
            await WithServerCertificate(serverCertificate, async (certificate, name) =>
            {
                Task t1 = Task.Factory.FromAsync(clientSslStream.BeginAuthenticateAsClient(name, clientCerts, SslProtocols.None, checkCertificateRevocation: false, null, null), clientSslStream.EndAuthenticateAsClient);
                Task t2 = Task.Factory.FromAsync(serverSslStream.BeginAuthenticateAsServer(certificate, clientCertificateRequired: clientCertificate != null, checkCertificateRevocation: false, null, null), serverSslStream.EndAuthenticateAsServer);
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);
            });
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

    public abstract class SslStreamStreamToStreamTest_SyncBase : SslStreamStreamToStreamTest
    {
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

        [Fact]
        public async Task SslStream_StreamToStream_Handshake_DisposeClient_Throws()
        {
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (var clientSslStream = new SslStream(stream1, false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(stream2))
            {
                stream1.Dispose();

                await Assert.ThrowsAsync<AggregateException>(() => DoHandshake(clientSslStream, serverSslStream));
            }
        }

        [Fact]
        public async Task SslStream_StreamToStream_Handshake_DisposeServer_Throws()
        {
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (var clientSslStream = new SslStream(stream1, false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(stream2))
            {
                stream2.Dispose();

                await Assert.ThrowsAsync<AggregateException>(() => DoHandshake(clientSslStream, serverSslStream));
            }
        }

        [Fact]
        public async Task SslStream_StreamToStream_Handshake_DisposeClientSsl_Throws()
        {
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (var serverSslStream = new SslStream(DelegateDelegatingStream.NopDispose(stream1)))
            {
                var clientSslStream = new SslStream(DelegateDelegatingStream.NopDispose(stream2), false, AllowAnyServerCertificate);
                clientSslStream.Dispose();

                await Assert.ThrowsAsync<ObjectDisposedException>(() => DoHandshake(clientSslStream, serverSslStream));
            }
        }

        [Fact]
        public async Task SslStream_StreamToStream_Handshake_DisposeServerSsl_Throws()
        {
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (var clientSslStream = new SslStream(DelegateDelegatingStream.NopDispose(stream1), false, AllowAnyServerCertificate))
            {
                var serverSslStream = new SslStream(DelegateDelegatingStream.NopDispose(stream2));
                serverSslStream.Dispose();

                await Assert.ThrowsAsync<ObjectDisposedException>(() => DoHandshake(clientSslStream, serverSslStream));
            }
        }
    }

    public sealed class SslStreamStreamToStreamTest_SyncParameters : SslStreamStreamToStreamTest_SyncBase
    {
        protected override async Task DoHandshake(SslStream clientSslStream, SslStream serverSslStream, X509Certificate serverCertificate = null, X509Certificate clientCertificate = null)
        {
            X509CertificateCollection clientCerts = clientCertificate != null ? new X509CertificateCollection() { clientCertificate } : null;
            await WithServerCertificate(serverCertificate, async (certificate, name) =>
            {
                Task t1 = Task.Run(() => clientSslStream.AuthenticateAsClient(name, clientCerts, SslProtocols.None, checkCertificateRevocation: false));
                Task t2 = Task.Run(() => serverSslStream.AuthenticateAsServer(certificate, clientCertificateRequired: clientCertificate != null, checkCertificateRevocation: false));
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);
            });
        }
    }

    public sealed class SslStreamStreamToStreamTest_SyncSslOptions : SslStreamStreamToStreamTest_SyncBase
    {
        protected override async Task DoHandshake(SslStream clientSslStream, SslStream serverSslStream, X509Certificate serverCertificate = null, X509Certificate clientCertificate = null)
        {
            X509CertificateCollection clientCerts = clientCertificate != null ? new X509CertificateCollection() { clientCertificate } : null;
            await WithServerCertificate(serverCertificate, async (certificate, name) =>
            {
                SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = name,
                    ClientCertificates = clientCerts,
                    EnabledSslProtocols = SslProtocols.None,
                };
                SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions()
                {
                    ServerCertificate = certificate, ClientCertificateRequired = clientCertificate != null,
                };
                Task t1 = Task.Run(() => clientSslStream.AuthenticateAsClient(clientOptions));
                Task t2 = Task.Run(() => serverSslStream.AuthenticateAsServer(serverOptions));
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);
            });
        }
    }

    public sealed class SslStreamStreamToStreamTest_MemoryAsync : SslStreamStreamToStreamTest
    {
        protected override async Task DoHandshake(SslStream clientSslStream, SslStream serverSslStream, X509Certificate serverCertificate = null, X509Certificate clientCertificate = null)
        {
            X509CertificateCollection clientCerts = clientCertificate != null ? new X509CertificateCollection() { clientCertificate } : null;
            await WithServerCertificate(serverCertificate, async(certificate, name) =>
            {
                Task t1 = clientSslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions() { TargetHost = name, ClientCertificates = clientCerts }, CancellationToken.None);
                Task t2 = serverSslStream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions() { ServerCertificate = certificate, ClientCertificateRequired = clientCertificate != null }, CancellationToken.None);
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);
            });
        }

        protected override Task<int> ReadAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            stream.ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();

        protected override Task WriteAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            stream.WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();

        [Fact]
        public async Task Authenticate_Precanceled_ThrowsOperationCanceledException()
        {
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (var clientSslStream = new SslStream(stream1, false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(stream2))
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => clientSslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions() { TargetHost = certificate.GetNameInfo(X509NameType.SimpleName, false) }, new CancellationToken(true)));
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => serverSslStream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions() { ServerCertificate = certificate }, new CancellationToken(true)));
            }
        }

        [Fact]
        public async Task AuthenticateAsClientAsync_MemoryBuffer_CanceledAfterStart_ThrowsOperationCanceledException()
        {
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (var clientSslStream = new SslStream(stream1, false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(stream2))
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                var cts = new CancellationTokenSource();
                Task t = clientSslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions() { TargetHost = certificate.GetNameInfo(X509NameType.SimpleName, false) }, cts.Token);
                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);
            }
        }

        [Fact]
        public async Task AuthenticateAsClientAsync_Sockets_CanceledAfterStart_ThrowsOperationCanceledException()
        {
            (Stream client, Stream server) = TestHelper.GetConnectedTcpStreams();

            using (client)
            using (server)
            using (var clientSslStream = new SslStream(client, false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(server))
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                var cts = new CancellationTokenSource();
                Task t = clientSslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions() { TargetHost = certificate.GetNameInfo(X509NameType.SimpleName, false) }, cts.Token);
                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);
            }
        }

        [Fact]
        public async Task AuthenticateAsServerAsync_VirtualNetwork_CanceledAfterStart_ThrowsOperationCanceledException()
        {
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            using (var clientSslStream = new SslStream(stream1, false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(stream2))
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                var cts = new CancellationTokenSource();
                Task t = serverSslStream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions() { ServerCertificate = certificate }, cts.Token);
                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);
            }
        }

        [Fact]
        public async Task AuthenticateAsServerAsync_Sockets_CanceledAfterStart_ThrowsOperationCanceledException()
        {
            (Stream client, Stream server) = TestHelper.GetConnectedTcpStreams();

            using (client)
            using (server)
            using (var clientSslStream = new SslStream(client, false, AllowAnyServerCertificate))
            using (var serverSslStream = new SslStream(server))
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                var cts = new CancellationTokenSource();
                Task t = serverSslStream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions() { ServerCertificate = certificate }, cts.Token);
                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);
            }
        }
    }
}
