// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using System.Threading.Tasks;
using Xunit;
using System.Threading;
using System.Linq;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public sealed class SslStreamAppDataTests
    {
        [Fact]
        public async Task UtilizeFullSizeOfTlsFrames()
        {
            (Stream client, Stream server) = TestHelper.GetConnectedTcpStreams();
            using (client)
            using (server)
            {
                using var clientInterceptingStream = new TlsFrameInterceptingStream(client);
                using var serverInterceptingStream = new TlsFrameInterceptingStream(server);

                using var clientStream = new SslStream(clientInterceptingStream, leaveInnerStreamOpen: true, (sender, cert, chain, errors) => true);
                using var serverStream = new SslStream(serverInterceptingStream, leaveInnerStreamOpen: true);

                using var serverCertificate = Configuration.Certificates.GetServerCertificate();
                var hostName = serverCertificate.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

                Task t1 = clientStream.AuthenticateAsClientAsync(hostName, [], SslProtocols.None, checkCertificateRevocation: false);
                Task t2 = serverStream.AuthenticateAsServerAsync(serverCertificate, clientCertificateRequired: false, checkCertificateRevocation: false);
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);

                // Clear the intercepted frames of the handshake
                clientInterceptingStream.InterceptedTlsFrameHeaders.Clear();
                serverInterceptingStream.InterceptedTlsFrameHeaders.Clear();

                var cts = new CancellationTokenSource(TestConfiguration.PassingTestTimeoutMilliseconds);
                var bytesToSend = 123_456;
                var sentData = new byte[bytesToSend];
                Random.Shared.NextBytes(sentData);
                var receivedData = new byte[bytesToSend];

                await clientStream.WriteAsync(sentData, cts.Token);

                int receivedBytes = 0;
                while (receivedBytes < bytesToSend)
                {
                    receivedBytes += await serverStream.ReadAsync(receivedData.AsMemory(receivedBytes), cts.Token);
                }

                Assert.Equal(sentData, receivedData);

                Assert.Equal(8, clientInterceptingStream.InterceptedTlsFrameHeaders.Count);
                Assert.All(clientInterceptingStream.InterceptedTlsFrameHeaders, static frameHeader => Assert.Equal(TlsContentType.AppData, frameHeader.Type));

                for (int i = 0; i < 7; i++)
                {
                    // The first 7 frames should contain 16384 bytes of data + TLS frame overhead
                    Assert.True(clientInterceptingStream.InterceptedTlsFrameHeaders[i].Length > 16384
                        && clientInterceptingStream.InterceptedTlsFrameHeaders[i].Length <= 16709);
                }

                // The last frame should contain less data than the previous ones
                Assert.True(clientInterceptingStream.InterceptedTlsFrameHeaders[7].Length < 16384);
            }
        }

        private sealed class TlsFrameInterceptingStream(Stream innerStream) : Stream
        {
            public List<TlsFrameHeader> InterceptedTlsFrameHeaders { get; } = new();
            public List<byte[]> InterceptedTlsFramePayloads { get; } = new();

            private readonly Stream _innerStream = innerStream;
            private readonly List<byte> _tlsFrameBuffer = new();

            public override bool CanRead => true;
            public override bool CanWrite => true;
            public override bool CanSeek => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

            public override void Flush() => _innerStream.Flush();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) { }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return _innerStream.Read(buffer, offset, count);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _tlsFrameBuffer.AddRange(buffer[offset..(offset+count)]);
                _innerStream.Write(buffer, offset, count);

                TlsFrameHeader header = default;
                while (TlsFrameHelper.TryGetFrameHeader(_tlsFrameBuffer.ToArray(), ref header))
                {
                    if (header.Length <= _tlsFrameBuffer.Count)
                    {
                        InterceptedTlsFrameHeaders.Add(header);
                        InterceptedTlsFramePayloads.Add(_tlsFrameBuffer.Take(header.Length).ToArray());
                        _tlsFrameBuffer.RemoveRange(0, header.Length);
                    }
                }
            }
        }
    }
}
