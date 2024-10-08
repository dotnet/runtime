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
                ServerCertificateContext = SslStreamCertificateContext.Create(_certificates.serverCert, _certificates.serverChain),
                RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true,
                ClientCertificateRequired = clientCertScenario == ClientCertScenario.InHandshake,
            };

            SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions
            {
                TargetHost = Guid.NewGuid().ToString("N"),
                EnabledSslProtocols = sslProtocol,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                ClientCertificates = clientCertScenario != ClientCertScenario.None
                    ? new X509CertificateCollection { _certificates.serverCert }
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
