// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.IO.Tests;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Security.Tests
{
    public abstract class SslStreamConformanceTests : WrappingConnectedStreamConformanceTests
    {
        protected override bool UsableAfterCanceledReads => false;
        protected override bool BlocksOnZeroByteReads => true;
        protected override Type UnsupportedConcurrentExceptionType => typeof(NotSupportedException);

        protected virtual SslProtocols GetSslProtocols() => SslProtocols.None;
        internal NetworkStream? networkStream;

        protected override async Task<StreamPair> CreateWrappedConnectedStreamsAsync(StreamPair wrapped, bool leaveOpen = false)
        {
            X509Certificate2? cert = Test.Common.Configuration.Certificates.GetServerCertificate();
            var ssl1 = new SslStream(wrapped.Stream1, leaveOpen, delegate { return true; });
            var ssl2 = new SslStream(wrapped.Stream2, leaveOpen, delegate { return true; });

            await new[]
            {
                ssl1.AuthenticateAsClientAsync(cert.GetNameInfo(X509NameType.SimpleName, false), null, GetSslProtocols(), false),
                ssl2.AuthenticateAsServerAsync(cert, false, GetSslProtocols(), false)
            }.WhenAllOrAnyFailed().ConfigureAwait(false);

            if (wrapped.Stream2 is NetworkStream)
            {
                networkStream = wrapped.Stream2 as NetworkStream;
            }

            return new StreamPair(ssl1, ssl2);
        }
    }

    public sealed class SslStreamMemoryConformanceTests : SslStreamConformanceTests
    {
        protected override Task<StreamPair> CreateConnectedStreamsAsync() =>
            CreateWrappedConnectedStreamsAsync(ConnectedStreams.CreateBidirectional());
    }

    public abstract class SslStreamDefaultNetworkConformanceTests : SslStreamConformanceTests
    {
        protected override bool CanTimeout => true;

        protected override Task<StreamPair> CreateConnectedStreamsAsync() =>
            CreateWrappedConnectedStreamsAsync(TestHelper.GetConnectedTcpStreams());
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.SupportsTls11))]
    public sealed class SslStreamTls11NetworkConformanceTests : SslStreamDefaultNetworkConformanceTests
    {
        protected override SslProtocols GetSslProtocols() => SslProtocols.Tls11;
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.SupportsTls12))]
    public sealed class SslStreamTls12NetworkConformanceTests : SslStreamDefaultNetworkConformanceTests
    {
        protected override SslProtocols GetSslProtocols() => SslProtocols.Tls12;
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.SupportsTls13))]
    public sealed class SslStreamTls13NetworkConformanceTests : SslStreamDefaultNetworkConformanceTests
    {
        protected override SslProtocols GetSslProtocols() => SslProtocols.Tls13;

        // Override the default method as we need to process extra TLS 1.3 messages to avoid
        // connection reset when we have unidirectional transfer.
        public override async Task CopyToAsync_AllDataCopied(int byteCount, bool useAsync)
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();
            (Stream writeable, Stream readable) = GetReadWritePair(streams);

            var results = new MemoryStream();
            byte[] dataToCopy = RandomNumberGenerator.GetBytes(byteCount);

            if (networkStream != null)
            {
                // If we are network stream, signal peer we will not write.
                networkStream.Socket.Shutdown(SocketShutdown.Send);
            }

            Task copyTask;
            if (useAsync)
            {
                copyTask = readable.CopyToAsync(results);
                await writeable.WriteAsync(dataToCopy);
            }
            else
            {
                copyTask = Task.Run(() => readable.CopyTo(results));
                writeable.Write(new ReadOnlySpan<byte>(dataToCopy));
            }

            // Read any pending protocol messages to avoid reset on close.
            // There should not be any application data.
            byte[] readBuffer = new byte[10];
            int readLength = await writeable.ReadAsync(readBuffer);
            Assert.Equal(0, readLength);

            writeable.Dispose();
            await copyTask;

            Assert.Equal(dataToCopy, results.ToArray());
        }
    }
}
