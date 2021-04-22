// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Net.Quic
{
    public partial class QuicClientConnectionOptions : System.Net.Quic.QuicOptions
    {
        public QuicClientConnectionOptions() { }
        public System.Net.Security.SslClientAuthenticationOptions? ClientAuthenticationOptions { get { throw null; } set { } }
        public System.Net.IPEndPoint? LocalEndPoint { get { throw null; } set { } }
        public System.Net.EndPoint? RemoteEndPoint { get { throw null; } set { } }
    }
    public sealed partial class QuicConnection : System.IDisposable
    {
        public QuicConnection(System.Net.EndPoint remoteEndPoint, System.Net.Security.SslClientAuthenticationOptions? sslClientAuthenticationOptions, System.Net.IPEndPoint? localEndPoint = null) { }
        public QuicConnection(System.Net.Quic.Implementations.QuicImplementationProvider implementationProvider, System.Net.EndPoint remoteEndPoint, System.Net.Security.SslClientAuthenticationOptions? sslClientAuthenticationOptions, System.Net.IPEndPoint? localEndPoint = null) { }
        public QuicConnection(System.Net.Quic.Implementations.QuicImplementationProvider implementationProvider, System.Net.Quic.QuicClientConnectionOptions options) { }
        public QuicConnection(System.Net.Quic.QuicClientConnectionOptions options) { }
        public bool Connected { get { throw null; } }
        public System.Net.IPEndPoint? LocalEndPoint { get { throw null; } }
        public System.Net.Security.SslApplicationProtocol NegotiatedApplicationProtocol { get { throw null; } }
        public System.Net.EndPoint RemoteEndPoint { get { throw null; } }
        public System.Threading.Tasks.ValueTask<System.Net.Quic.QuicStream> AcceptStreamAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.Threading.Tasks.ValueTask CloseAsync(long errorCode, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.Threading.Tasks.ValueTask ConnectAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public void Dispose() { }
        public long GetRemoteAvailableBidirectionalStreamCount() { throw null; }
        public long GetRemoteAvailableUnidirectionalStreamCount() { throw null; }
        public System.Net.Quic.QuicStream OpenBidirectionalStream() { throw null; }
        public System.Net.Quic.QuicStream OpenUnidirectionalStream() { throw null; }
    }
    public partial class QuicConnectionAbortedException : System.Net.Quic.QuicException
    {
        public QuicConnectionAbortedException(string message, long errorCode) : base (default(string)) { }
        public long ErrorCode { get { throw null; } }
    }
    public partial class QuicException : System.Exception
    {
        public QuicException(string? message) { }
        public QuicException(string? message, System.Exception? innerException) { }
    }
    public static partial class QuicImplementationProviders
    {
        public static System.Net.Quic.Implementations.QuicImplementationProvider Default { get { throw null; } }
        public static System.Net.Quic.Implementations.QuicImplementationProvider Mock { get { throw null; } }
        public static System.Net.Quic.Implementations.QuicImplementationProvider MsQuic { get { throw null; } }
    }
    public sealed partial class QuicListener : System.IDisposable
    {
        public QuicListener(System.Net.IPEndPoint listenEndPoint, System.Net.Security.SslServerAuthenticationOptions sslServerAuthenticationOptions) { }
        public QuicListener(System.Net.Quic.Implementations.QuicImplementationProvider implementationProvider, System.Net.IPEndPoint listenEndPoint, System.Net.Security.SslServerAuthenticationOptions sslServerAuthenticationOptions) { }
        public QuicListener(System.Net.Quic.Implementations.QuicImplementationProvider implementationProvider, System.Net.Quic.QuicListenerOptions options) { }
        public QuicListener(System.Net.Quic.QuicListenerOptions options) { }
        public System.Net.IPEndPoint ListenEndPoint { get { throw null; } }
        public System.Threading.Tasks.ValueTask<System.Net.Quic.QuicConnection> AcceptConnectionAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public void Dispose() { }
    }
    public partial class QuicListenerOptions : System.Net.Quic.QuicOptions
    {
        public QuicListenerOptions() { }
        public int ListenBacklog { get { throw null; } set { } }
        public System.Net.IPEndPoint? ListenEndPoint { get { throw null; } set { } }
        public System.Net.Security.SslServerAuthenticationOptions? ServerAuthenticationOptions { get { throw null; } set { } }
    }
    public partial class QuicOperationAbortedException : System.Net.Quic.QuicException
    {
        public QuicOperationAbortedException(string message) : base (default(string)) { }
    }
    public partial class QuicOptions
    {
        public QuicOptions() { }
        public System.TimeSpan IdleTimeout { get { throw null; } set { } }
        public long MaxBidirectionalStreams { get { throw null; } set { } }
        public long MaxUnidirectionalStreams { get { throw null; } set { } }
    }
    public sealed partial class QuicStream : System.IO.Stream
    {
        internal QuicStream() { }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public override long Length { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        public long StreamId { get { throw null; } }
        public void AbortRead(long errorCode) { }
        public void AbortWrite(long errorCode) { }
        public override System.IAsyncResult BeginRead(byte[] buffer, int offset, int count, System.AsyncCallback? callback, object? state) { throw null; }
        public override System.IAsyncResult BeginWrite(byte[] buffer, int offset, int count, System.AsyncCallback? callback, object? state) { throw null; }
        protected override void Dispose(bool disposing) { }
        public override int EndRead(System.IAsyncResult asyncResult) { throw null; }
        public override void EndWrite(System.IAsyncResult asyncResult) { }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public override int Read(byte[] buffer, int offset, int count) { throw null; }
        public override int Read(System.Span<byte> buffer) { throw null; }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override System.Threading.Tasks.ValueTask<int> ReadAsync(System.Memory<byte> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public void Shutdown() { }
        public System.Threading.Tasks.ValueTask ShutdownWriteCompleted(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.Threading.Tasks.ValueTask ShutdownCompleted(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override void Write(System.ReadOnlySpan<byte> buffer) { }
        public System.Threading.Tasks.ValueTask WriteAsync(System.Buffers.ReadOnlySequence<byte> buffers, bool endStream, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.Threading.Tasks.ValueTask WriteAsync(System.Buffers.ReadOnlySequence<byte> buffers, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.ValueTask WriteAsync(System.ReadOnlyMemory<byte> buffer, bool endStream, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override System.Threading.Tasks.ValueTask WriteAsync(System.ReadOnlyMemory<byte> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.Threading.Tasks.ValueTask WriteAsync(System.ReadOnlyMemory<System.ReadOnlyMemory<byte>> buffers, bool endStream, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.Threading.Tasks.ValueTask WriteAsync(System.ReadOnlyMemory<System.ReadOnlyMemory<byte>> buffers, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    public partial class QuicStreamAbortedException : System.Net.Quic.QuicException
    {
        public QuicStreamAbortedException(string message, long errorCode) : base (default(string)) { }
        public long ErrorCode { get { throw null; } }
    }
}
namespace System.Net.Quic.Implementations
{
    public abstract partial class QuicImplementationProvider
    {
        internal QuicImplementationProvider() { }
        public abstract bool IsSupported { get; }
    }
}
