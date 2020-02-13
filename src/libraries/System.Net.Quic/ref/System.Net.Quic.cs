// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

using System.Buffers;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic
{
    public sealed partial class QuicConnection : System.IDisposable
    {
        public QuicConnection(IPEndPoint remoteEndPoint, System.Net.Security.SslClientAuthenticationOptions sslClientAuthenticationOptions, IPEndPoint localEndPoint = null) { }
        public System.Threading.Tasks.ValueTask ConnectAsync(System.Threading.CancellationToken cancellationToken = default) { throw null; }
        public bool Connected => throw null;
        public IPEndPoint LocalEndPoint => throw null;
        public IPEndPoint RemoteEndPoint => throw null;
        public QuicStream OpenUnidirectionalStream() => throw null;
        public QuicStream OpenBidirectionalStream() => throw null;
        public long GetRemoteAvailableUnidirectionalStreamCount() => throw null;
        public long GetRemoteAvailableBidirectionalStreamCount() => throw null;
        public System.Threading.Tasks.ValueTask<QuicStream> AcceptStreamAsync(System.Threading.CancellationToken cancellationToken = default) => throw null;
        public System.Net.Security.SslApplicationProtocol NegotiatedApplicationProtocol => throw null;
        public ValueTask CloseAsync(long errorCode, System.Threading.CancellationToken cancellationToken = default) => throw null;
        public void Dispose() => throw null;
        public static bool IsQuicSupported => throw null;
    }
    public sealed partial class QuicListener : IDisposable
    {
        public QuicListener(IPEndPoint listenEndPoint, System.Net.Security.SslServerAuthenticationOptions sslServerAuthenticationOptions) { }
        public IPEndPoint ListenEndPoint => throw null;
        public System.Threading.Tasks.ValueTask<QuicConnection> AcceptConnectionAsync(System.Threading.CancellationToken cancellationToken = default) => throw null;
        public void Start() => throw null;
        public void Close() => throw null;
        public void Dispose() => throw null;
    }
    public sealed class QuicStream : System.IO.Stream
    {
        internal QuicStream() { }
        public override bool CanSeek => throw null;
        public override long Length => throw null;
        public override long Seek(long offset, System.IO.SeekOrigin origin) => throw null;
        public override void SetLength(long value) => throw null;
        public override long Position { get => throw null; set => throw null; }
        public override bool CanRead => throw null;
        public override bool CanWrite => throw null;
        public override void Flush() => throw null;
        public override int Read(byte[] buffer, int offset, int count) => throw null;
        public override void Write(byte[] buffer, int offset, int count) => throw null;
        public long StreamId => throw null;
        public void AbortRead(long errorCode) => throw null;
        public void AbortWrite(long errorCode) => throw null;
        public ValueTask WriteAsync(ReadOnlyMemory<byte> data, bool endStream, System.Threading.CancellationToken cancellationToken = default) => throw null;
        public ValueTask WriteAsync(ReadOnlySequence<byte> data, System.Threading.CancellationToken cancellationToken = default) => throw null;
        public ValueTask WriteAsync(ReadOnlySequence<byte> data, bool endStream, System.Threading.CancellationToken cancellationToken = default) => throw null;
        public ValueTask WriteAsync(ReadOnlyMemory<ReadOnlyMemory<byte>> data, System.Threading.CancellationToken cancellationToken = default) => throw null;
        public ValueTask WriteAsync(ReadOnlyMemory<ReadOnlyMemory<byte>> data, bool endStream, System.Threading.CancellationToken cancellationToken = default) => throw null;
        public ValueTask ShutdownWriteCompleted(System.Threading.CancellationToken cancellationToken = default) => throw null;
        public void Shutdown() => throw null;
    }
    public class QuicClientConnectionOptions
    {
        public SslClientAuthenticationOptions ClientAuthenticationOptions { get => throw null; set => throw null; }
        public IPEndPoint LocalEndPoint { get => throw null; set => throw null; }
        public IPEndPoint RemoteEndPoint { get => throw null; set => throw null; }
        public long MaxBidirectionalStreams { get => throw null; set => throw null; }
        public long MaxUnidirectionalStreams { get => throw null; set => throw null; }
        public TimeSpan IdleTimeout { get => throw null; set => throw null; }
    }
    public class QuicListenerOptions
    {
        public SslServerAuthenticationOptions ServerAuthenticationOptions { get => throw null; set => throw null; }
        public IPEndPoint ListenEndPoint { get => throw null; set => throw null; }
        public int ListenBacklog { get => throw null; set => throw null; }
        public long MaxBidirectionalStreams { get => throw null; set => throw null; }
        public long MaxUnidirectionalStreams { get => throw null; set => throw null; }
        public TimeSpan IdleTimeout { get => throw null; set => throw null; }
    }
    public class QuicException : Exception
    {
        public QuicException(string message) : base(message) { }
    }
    public class QuicConnectionAbortedException : QuicException
    {
        public QuicConnectionAbortedException(string message, long errorCode) : base(message) { }
        public long ErrorCode { get; }

    }
    public class QuicStreamAbortedException : QuicException
    {
        public QuicStreamAbortedException(string message, long errorCode) : base(message) { }
        public long ErrorCode { get; }
    }
    public class QuicOperationAbortedException : QuicException
    {
        public QuicOperationAbortedException(string message) : base(message) { }
    }
}
