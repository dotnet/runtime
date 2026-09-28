// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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
