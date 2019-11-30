// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic
{
    public sealed partial class QuicConnection : System.IDisposable, System.IAsyncDisposable
    {
        public QuicConnection(IPEndPoint remoteEndPoint, System.Net.Security.SslClientAuthenticationOptions sslClientAuthenticationOptions, IPEndPoint localEndPoint = null) { }
        public System.Threading.Tasks.ValueTask ConnectAsync(System.Threading.CancellationToken cancellationToken = default) { throw null; }
        public bool Connected => throw null;
        public IPEndPoint LocalEndPoint => throw null;
        public IPEndPoint RemoteEndPoint => throw null;
        public QuicStream OpenUnidirectionalStream() => throw null;
        public QuicStream OpenBidirectionalStream() => throw null;
        public System.Threading.Tasks.ValueTask<QuicStream> AcceptStreamAsync(System.Threading.CancellationToken cancellationToken = default) => throw null;
        public System.Net.Security.SslApplicationProtocol NegotiatedApplicationProtocol => throw null;
        public void Close() => throw null;
        public void Dispose() => throw null;
        public ValueTask DisposeAsync() => throw null;
    }
    public sealed partial class QuicListener : IDisposable, System.IAsyncDisposable
    {
        public QuicListener(IPEndPoint listenEndPoint, System.Net.Security.SslServerAuthenticationOptions sslServerAuthenticationOptions) { }
        public IPEndPoint ListenEndPoint => throw null;
        public System.Threading.Tasks.ValueTask<QuicConnection> AcceptConnectionAsync(System.Threading.CancellationToken cancellationToken = default) => throw null;
        public void Close() => throw null;
        public void Dispose() => throw null;
        public ValueTask DisposeAsync() => throw null;
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
        public void ShutdownRead() => throw null;
        public void ShutdownWrite() => throw null;
    }
}
