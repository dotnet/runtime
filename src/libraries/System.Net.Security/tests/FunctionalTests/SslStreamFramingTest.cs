// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    // These tests target framing detection in SslStream by manipulating chunking of the data sent between client and server.
    public class SslStreamFramingTests : IClassFixture<CertificateSetup>
    {
        private static bool SupportsRenegotiation => TestConfiguration.SupportsRenegotiation;

        readonly ITestOutputHelper _output;
        readonly CertificateSetup _certificates;

        public SslStreamFramingTests(ITestOutputHelper output, CertificateSetup setup)
        {
            _output = output;
            _certificates = setup;
        }

        public enum FramingType
        {
            // 1 byte reads
            ByteByByte,

            // Receive data at chunks, not necessarily respecting frame boundaries
            Chunked,

            // Coalesce reads to biggest chunks possible
            Coalescing
        }

        public enum ClientCertScenario
        {
            None,
            InHandshake,
            PostHandshake
        }

        public static TheoryData<FramingType, SslProtocols, ClientCertScenario> HandshakeScenarioData()
        {
            var data = new TheoryData<FramingType, SslProtocols, ClientCertScenario>();

            foreach (FramingType framingType in Enum.GetValues(typeof(FramingType)))
            {
                foreach (SslProtocols sslProtocol in SslProtocolSupport.EnumerateSupportedProtocols(SslProtocols.Tls12 | SslProtocols.Tls13, true))
                {
                    foreach (ClientCertScenario clientCertScenario in Enum.GetValues(typeof(ClientCertScenario)))
                    {
                        if (clientCertScenario == ClientCertScenario.PostHandshake && !TestConfiguration.SupportsRenegotiation)
                        {
                            continue;
                        }

                        data.Add(framingType, sslProtocol, clientCertScenario);
                    }
                }
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(HandshakeScenarioData))]
        public async Task Handshake_Success(FramingType framingType, SslProtocols sslProtocol, ClientCertScenario clientCertScenario)
        {
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();

            ConfigurableReadStream clientStream = new(stream1, framingType);
            ConfigurableReadStream serverStream = new(stream2, framingType);
            using SslStream client = new SslStream(clientStream);
            using SslStream server = new SslStream(serverStream);

            SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions
            {
                EnabledSslProtocols = sslProtocol,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                ServerCertificateContext = _certificates.CreateSslStreamCertificateContext(),
                RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true,
                ClientCertificateRequired = clientCertScenario == ClientCertScenario.InHandshake,
            };

            SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions
            {
                TargetHost = Guid.NewGuid().ToString("N"),
                EnabledSslProtocols = sslProtocol,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                ClientCertificates = clientCertScenario != ClientCertScenario.None
                    ? new X509CertificateCollection { _certificates.ServerCert }
                    : new X509CertificateCollection(),
                RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true,
            };

            Task clientTask = Task.Run(async () =>
                {
                    await client.AuthenticateAsClientAsync(clientOptions);

                    // reading triggers potential post-handshake authentication
                    await client.ReadExactlyAsync(new byte[13]);
                });
            Task serverTask = Task.Run(async () =>
                {
                    await server.AuthenticateAsServerAsync(serverOptions);
                    if (clientCertScenario == ClientCertScenario.PostHandshake)
                    {
                        await server.NegotiateClientCertificateAsync();
                    }

                    await server.WriteAsync(Encoding.UTF8.GetBytes("Hello, world!"));
                });

            await TestConfiguration.WhenAllOrAnyFailedWithTimeout(clientTask, serverTask);

            // verify that we used the mocked read method
            Assert.True(clientStream.ReadCalled, "Mocked read method was not used");
            Assert.True(serverStream.ReadCalled, "Mocked read method was not used");

            await TestHelper.PingPong(client, server);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.SupportsTls13))]
        public async Task Read_ExactlyFiveByteTlsRecord_DetectedAsCompleteFrame()
        {
            // Regression test: a TLS record that is exactly the 5-byte header with a
            // zero-length payload (e.g. 17 03 03 00 00) must be recognized as a complete
            // frame. Previously EnsureFullTlsFrameAsync only recomputed the frame size once
            // more than HeaderSize bytes were buffered, so such a record left SslStream
            // waiting forever for a sixth byte that never arrives.
            //
            // The scenario is pinned to TLS 1.3: a zero-length application_data record cannot
            // be decrypted (there is no room for the AEAD tag), so once framing recognizes the
            // complete 5-byte frame the read fails fast. Under TLS 1.2 a zero-length record is
            // a legal empty fragment that decrypts to zero bytes and is skipped, which would
            // make even the fixed code read again and mask the framing behavior under test.
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();

            ZeroLengthRecordInjectingStream clientStream = new(stream1);
            using SslStream client = new SslStream(clientStream);
            using SslStream server = new SslStream(stream2);

            SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions
            {
                EnabledSslProtocols = SslProtocols.Tls13,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                ServerCertificateContext = _certificates.CreateSslStreamCertificateContext(),
                RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true,
            };

            SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions
            {
                TargetHost = Guid.NewGuid().ToString("N"),
                EnabledSslProtocols = SslProtocols.Tls13,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true,
            };

            await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                client.AuthenticateAsClientAsync(clientOptions),
                server.AuthenticateAsServerAsync(serverOptions));

            // From now on, the client's next non-empty read returns a raw, exactly-5-byte
            // TLS record with a zero-length payload, and any subsequent read blocks.
            clientStream.StartInjecting();

            using CancellationTokenSource cts = new CancellationTokenSource(TestConfiguration.PassingTestTimeout);

            // With the fix, the 5-byte record is recognized as a complete frame and the read
            // completes promptly (decrypting the bogus empty record fails). Without the fix,
            // SslStream keeps waiting for more data and this read hangs until cts fires.
            await Assert.ThrowsAnyAsync<Exception>(() => client.ReadAsync(new byte[16], cts.Token).AsTask());

            Assert.False(cts.IsCancellationRequested, "SslStream hung waiting for more data instead of detecting the complete 5-byte TLS frame.");
        }

        // Wraps a stream and, once StartInjecting is called, serves a single raw 5-byte TLS
        // record (application data, zero-length payload) on the next non-empty read. Any read
        // after that blocks until cancellation, so a caller that fails to treat the 5-byte
        // record as a complete frame observes a hang.
        private sealed class ZeroLengthRecordInjectingStream : Stream
        {
            // application_data (0x17), TLS 1.2 record version (0x0303), length 0x0000
            private static readonly byte[] s_zeroLengthRecord = { 0x17, 0x03, 0x03, 0x00, 0x00 };

            private readonly Stream _inner;
            private bool _injecting;
            private bool _injected;

            public ZeroLengthRecordInjectingStream(Stream inner) => _inner = inner;

            public void StartInjecting() => _injecting = true;

            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                if (_injecting && buffer.Length >= s_zeroLengthRecord.Length)
                {
                    if (!_injected)
                    {
                        _injected = true;
                        s_zeroLengthRecord.CopyTo(buffer.Span);
                        return s_zeroLengthRecord.Length;
                    }

                    // The complete frame was already delivered above. A correct implementation
                    // does not ask for more data; if it does, block so the caller hangs.
                    await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
                }

                return await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            }

            public override int Read(byte[] buffer, int offset, int count)
                => ReadAsync(new Memory<byte>(buffer, offset, count)).AsTask().GetAwaiter().GetResult();

            public override bool CanRead => _inner.CanRead;
            public override bool CanSeek => _inner.CanSeek;
            public override bool CanWrite => _inner.CanWrite;
            public override long Length => _inner.Length;
            public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override void Flush() => _inner.Flush();
            public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
            public override void SetLength(long value) => throw new NotImplementedException();
            public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _inner.Dispose();
                }

                base.Dispose(disposing);
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.OSX)]
        public async Task Handshake_TransportReframedAfterHandshake_Success()
        {
            // A transport that changes its own framing once the handshake completes, the way
            // SqlClient's SslOverTdsStream stops encapsulating TLS records in TDS packets once
            // AuthenticateAsClient returns. Nothing may be read from the transport between the
            // final handshake record and the first application operation, or the bytes that
            // follow are consumed with the handshake framing still applied.
            var psi = new ProcessStartInfo();
            psi.Environment.Add("DOTNET_SYSTEM_NET_SECURITY_USENETWORKFRAMEWORK", "1");

            await RemoteExecutor.Invoke(static async () =>
            {
                (Stream clientTransport, Stream serverTransport) = TestHelper.GetConnectedTcpStreams();
                using var clientFraming = new HandshakeFramingStream(clientTransport);
                using var serverFraming = new HandshakeFramingStream(serverTransport);
                using var client = new SslStream(clientFraming);
                using var server = new SslStream(serverFraming);
                using X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate();

                var clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = certificate.GetNameInfo(X509NameType.SimpleName, false),
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                    RemoteCertificateValidationCallback = TestHelper.AllowAnyServerCertificate,
                };
                var serverOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificate = certificate,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                };

                // The transport leaves handshake framing only after authentication returns, which
                // is exactly the window a read issued past the last handshake record lands in.
                Task clientTask = Task.Run(() =>
                {
                    client.AuthenticateAsClient(clientOptions);
                    clientFraming.FinishHandshake();
                });
                Task serverTask = Task.Run(() =>
                {
                    server.AuthenticateAsServer(serverOptions);
                    serverFraming.FinishHandshake();
                });

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(clientTask, serverTask);

                await TestHelper.PingPong(client, server);

                Assert.Equal(0, clientFraming.MisframedReads);
                Assert.Equal(0, serverFraming.MisframedReads);
            }, new RemoteInvokeOptions { StartInfo = psi }).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.OSX)]
        public async Task Dispose_RacingApplicationWrite_CompletesWithoutCrash()
        {
            // Disposing while a send is in flight must not release the native callback context
            // early: the nw_connection_send completion and the framer output write both resolve
            // it, and resolving a freed handle corrupts an unrelated connection or terminates the
            // process. Dispose must also not wait on a write it is itself responsible for
            // unblocking, which would hang instead.
            //
            // This is a smoke test for that shutdown ordering, not a reproduction of the
            // use-after-free: on loopback the send completion has already run by the time Dispose
            // is reached, so the test passes with or without the wait in Dispose. Reproducing it
            // reliably needs a stalled send completion, which the PAL offers no hook for.
            var psi = new ProcessStartInfo();
            psi.Environment.Add("DOTNET_SYSTEM_NET_SECURITY_USENETWORKFRAMEWORK", "1");

            await RemoteExecutor.Invoke(static async () =>
            {
                byte[] payload = new byte[256 * 1024];
                using X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate();

                for (int i = 0; i < 25; i++)
                {
                    (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
                    using (server)
                    {
                        var clientOptions = new SslClientAuthenticationOptions
                        {
                            TargetHost = certificate.GetNameInfo(X509NameType.SimpleName, false),
                            CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                            RemoteCertificateValidationCallback = TestHelper.AllowAnyServerCertificate,
                        };
                        var serverOptions = new SslServerAuthenticationOptions
                        {
                            ServerCertificate = certificate,
                            CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                        };

                        await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                            client.AuthenticateAsClientAsync(clientOptions),
                            server.AuthenticateAsServerAsync(serverOptions));

                        // Large enough that the send is still outstanding when Dispose runs, since
                        // nothing is draining the peer.
                        var writeIssued = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                        Task writeTask = Task.Run(async () =>
                        {
                            try
                            {
                                // Signal only once the send has actually been handed to the PAL, so
                                // Dispose races an in-flight native callback rather than a no-op.
                                ValueTask write = client.WriteAsync(payload);
                                writeIssued.TrySetResult();
                                await write;
                            }
                            catch (Exception ex) when (ex is ObjectDisposedException or IOException or OperationCanceledException or InvalidOperationException)
                            {
                                // Expected: the stream may be disposed before or during the write.
                            }
                            finally
                            {
                                writeIssued.TrySetResult();
                            }
                        });

                        await writeIssued.Task.WaitAsync(TestConfiguration.PassingTestTimeout);
                        client.Dispose();

                        // A hang here means Dispose and the write are waiting on each other.
                        await writeTask.WaitAsync(TestConfiguration.PassingTestTimeout);
                    }
                }
            }, new RemoteInvokeOptions { StartInfo = psi }).DisposeAsync();
        }

        // Wraps a stream in a length prefixed envelope while the handshake runs and switches to
        // pass-through afterwards, modelling a transport that multiplexes the TLS handshake inside
        // its own protocol framing.
        private sealed class HandshakeFramingStream : Stream
        {
            private const int HeaderLength = 8;
            private const int PayloadLength = 4088;
            private const byte PacketType = 0x12;

            private readonly Stream _inner;
            private int _packetBytes;
            private volatile bool _encapsulate = true;

            public HandshakeFramingStream(Stream inner) => _inner = inner;

            // Number of times a read consumed a header that this stream never wrote, which means
            // the peer's payload was parsed with the handshake framing still applied.
            public int MisframedReads { get; private set; }

            public void FinishHandshake() => _encapsulate = false;

            public override int Read(byte[] buffer, int offset, int count) =>
                Read(buffer.AsSpan(offset, count));

            public override int Read(Span<byte> buffer)
            {
                if (!_encapsulate)
                {
                    return _inner.Read(buffer);
                }

                if (_packetBytes == 0 && !ReadHeader())
                {
                    return 0;
                }

                int read = _inner.Read(buffer.Slice(0, Math.Min(buffer.Length, _packetBytes)));
                _packetBytes -= read;
                return read;
            }

            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                if (!_encapsulate)
                {
                    return await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                }

                if (_packetBytes == 0)
                {
                    byte[] header = new byte[HeaderLength];
                    int headerRead = await _inner.ReadAtLeastAsync(header.AsMemory(0, HeaderLength), HeaderLength, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(false);
                    if (headerRead < HeaderLength)
                    {
                        return 0;
                    }

                    ProcessHeader(header);
                }

                int read = await _inner.ReadAsync(buffer.Slice(0, Math.Min(buffer.Length, _packetBytes)), cancellationToken).ConfigureAwait(false);
                _packetBytes -= read;
                return read;
            }

            public override void Write(byte[] buffer, int offset, int count) =>
                Write(buffer.AsSpan(offset, count));

            public override void Write(ReadOnlySpan<byte> buffer)
            {
                if (!_encapsulate)
                {
                    _inner.Write(buffer);
                    _inner.Flush();
                    return;
                }

                Span<byte> header = stackalloc byte[HeaderLength];
                while (!buffer.IsEmpty)
                {
                    int length = Math.Min(buffer.Length, PayloadLength);
                    header.Clear();
                    WriteHeader(header, length);
                    _inner.Write(header);
                    _inner.Write(buffer.Slice(0, length));
                    _inner.Flush();
                    buffer = buffer.Slice(length);
                }
            }

            public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                if (!_encapsulate)
                {
                    await _inner.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
                    await _inner.FlushAsync(cancellationToken).ConfigureAwait(false);
                    return;
                }

                while (!buffer.IsEmpty)
                {
                    int length = Math.Min(buffer.Length, PayloadLength);
                    byte[] header = new byte[HeaderLength];
                    WriteHeader(header, length);
                    await _inner.WriteAsync(header, cancellationToken).ConfigureAwait(false);
                    await _inner.WriteAsync(buffer.Slice(0, length), cancellationToken).ConfigureAwait(false);
                    await _inner.FlushAsync(cancellationToken).ConfigureAwait(false);
                    buffer = buffer.Slice(length);
                }
            }

            private bool ReadHeader()
            {
                Span<byte> header = stackalloc byte[HeaderLength];
                int read = 0;
                while (read < HeaderLength)
                {
                    int bytes = _inner.Read(header.Slice(read));
                    if (bytes == 0)
                    {
                        return false;
                    }

                    read += bytes;
                }

                ProcessHeader(header);
                return true;
            }

            private void ProcessHeader(ReadOnlySpan<byte> header)
            {
                if (header[0] != PacketType)
                {
                    MisframedReads++;
                }

                _packetBytes = ((header[2] << 8) | header[3]) - HeaderLength;
            }

            private static void WriteHeader(Span<byte> header, int payloadLength)
            {
                int packetLength = HeaderLength + payloadLength;
                header[0] = PacketType;
                header[1] = 1;
                header[2] = (byte)(packetLength >> 8);
                header[3] = (byte)packetLength;
            }

            public override bool CanRead => _inner.CanRead;
            public override bool CanSeek => false;
            public override bool CanWrite => _inner.CanWrite;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() => _inner.Flush();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _inner.Dispose();
                }

                base.Dispose(disposing);
            }
        }

        internal class ConfigurableReadStream : Stream
        {
            private readonly Stream _stream;
            private readonly FramingType _framingType;

            public bool ReadCalled { get; private set; }

            public ConfigurableReadStream(Stream stream, FramingType framingType)
            {
                _stream = stream;
                _framingType = framingType;
            }

            public override bool CanRead => _stream.CanRead;

            public override bool CanSeek => _stream.CanSeek;

            public override bool CanWrite => _stream.CanWrite;

            public override long Length => _stream.Length;

            public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public override void Flush()
            {
                _stream.Flush();
            }

            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                ReadCalled = true;

                switch (_framingType)
                {
                    case FramingType.ByteByByte:
                        return await _stream.ReadAsync(buffer.Length > 0 ? buffer.Slice(0, 1) : buffer, cancellationToken);

                    case FramingType.Coalescing:
                        {
                            if (buffer.Length > 0)
                            {
                                // wait 10ms, this should be enough for the other side to write as much data
                                // as it will ever write before receiving something back.
                                await Task.Delay(10);
                            }
                            return await _stream.ReadAsync(buffer, cancellationToken);
                        }
                    case FramingType.Chunked:
                        {
                            if (buffer.Length > 0)
                            {
                                // wait 10ms, this should be enough for the other side to write as much data
                                // as it will ever write before receiving something back.
                                await Task.Delay(10);

                                const int maxRead = 1519; // arbitrarily chosen chunk size

                                if (buffer.Length > maxRead)
                                {
                                    buffer = buffer.Slice(0, maxRead);
                                }
                            }
                            return await _stream.ReadAsync(buffer, cancellationToken);
                        }

                    default:
                        throw new NotImplementedException();
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return ReadAsync(new Memory<byte>(buffer, offset, count)).AsTask().GetAwaiter().GetResult();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return _stream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _stream.Write(buffer, offset, count);
            }
        }
    }
}
