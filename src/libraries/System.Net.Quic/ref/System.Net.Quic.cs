// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Net.Quic
{
    public sealed partial class QuicClientConnectionOptions : System.Net.Quic.QuicConnectionOptions
    {
        public QuicClientConnectionOptions() { }
        public required System.Net.Security.SslClientAuthenticationOptions ClientAuthenticationOptions { get { throw null; } set { } }
        public System.Net.IPEndPoint? LocalEndPoint { get { throw null; } set { } }
        public required System.Net.EndPoint RemoteEndPoint { get { throw null; } set { } }
    }
    public sealed partial class QuicConnection : System.IAsyncDisposable
    {
        internal QuicConnection() { }
        public static bool IsSupported { get { throw null; } }
        public System.Net.IPEndPoint LocalEndPoint { get { throw null; } }
        public System.Net.Security.SslApplicationProtocol NegotiatedApplicationProtocol { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.X509Certificate? RemoteCertificate { get { throw null; } }
        public System.Net.IPEndPoint RemoteEndPoint { get { throw null; } }
        public System.Threading.Tasks.ValueTask<System.Net.Quic.QuicStream> AcceptInboundStreamAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.Threading.Tasks.ValueTask CloseAsync(long errorCode, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.ValueTask<System.Net.Quic.QuicConnection> ConnectAsync(System.Net.Quic.QuicClientConnectionOptions options, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        public System.Threading.Tasks.ValueTask<System.Net.Quic.QuicStream> OpenOutboundStreamAsync(System.Net.Quic.QuicStreamType type, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class QuicConnectionAbortedException : System.Net.Quic.QuicException
    {
        public QuicConnectionAbortedException(string message, long errorCode) : base (default(string)) { }
        public long ErrorCode { get { throw null; } }
    }
    public abstract partial class QuicConnectionOptions
    {
        internal QuicConnectionOptions() { }
        public required long DefaultStreamErrorCode { get { throw null; } set { } }
        public System.TimeSpan IdleTimeout { get { throw null; } set { } }
        public int MaxInboundBidirectionalStreams { get { throw null; } set { } }
        public int MaxInboundUnidirectionalStreams { get { throw null; } set { } }
    }
    public partial class QuicException : System.Exception
    {
        public QuicException(string? message) { }
        public QuicException(string? message, System.Exception? innerException) { }
        public QuicException(string? message, System.Exception? innerException, int result) { }
    }
    public sealed partial class QuicListener : System.IAsyncDisposable
    {
        internal QuicListener() { }
        public static bool IsSupported { get { throw null; } }
        public System.Net.IPEndPoint LocalEndPoint { get { throw null; } }
        public System.Threading.Tasks.ValueTask<System.Net.Quic.QuicConnection> AcceptConnectionAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        public static System.Threading.Tasks.ValueTask<System.Net.Quic.QuicListener> ListenAsync(System.Net.Quic.QuicListenerOptions options, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override string ToString() { throw null; }
    }
    public sealed partial class QuicListenerOptions
    {
        public QuicListenerOptions() { }
        public required System.Collections.Generic.List<System.Net.Security.SslApplicationProtocol> ApplicationProtocols { get { throw null; } set { } }
        public required System.Func<System.Net.Quic.QuicConnection, System.Net.Security.SslClientHelloInfo, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Net.Quic.QuicServerConnectionOptions>> ConnectionOptionsCallback { get { throw null; } set { } }
        public int ListenBacklog { get { throw null; } set { } }
        public required System.Net.IPEndPoint ListenEndPoint { get { throw null; } set { } }
    }
    public partial class QuicOperationAbortedException : System.Net.Quic.QuicException
    {
        public QuicOperationAbortedException(string message) : base (default(string)) { }
    }
    public sealed partial class QuicServerConnectionOptions : System.Net.Quic.QuicConnectionOptions
    {
        public QuicServerConnectionOptions() { }
        public required System.Net.Security.SslServerAuthenticationOptions ServerAuthenticationOptions { get { throw null; } set { } }
    }
    public sealed partial class QuicStream : System.IO.Stream
    {
        internal QuicStream() { }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanTimeout { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public override long Length { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        public bool ReadsCompleted { get { throw null; } }
        public override int ReadTimeout { get { throw null; } set { } }
        public long StreamId { get { throw null; } }
        public override int WriteTimeout { get { throw null; } set { } }
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
        public override int ReadByte() { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public void Shutdown() { }
        public System.Threading.Tasks.ValueTask ShutdownCompleted(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.Threading.Tasks.ValueTask WaitForWriteCompletionAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override void Write(System.ReadOnlySpan<byte> buffer) { }
        public System.Threading.Tasks.ValueTask WriteAsync(System.Buffers.ReadOnlySequence<byte> buffers, bool endStream, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.Threading.Tasks.ValueTask WriteAsync(System.Buffers.ReadOnlySequence<byte> buffers, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.ValueTask WriteAsync(System.ReadOnlyMemory<byte> buffer, bool endStream, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override System.Threading.Tasks.ValueTask WriteAsync(System.ReadOnlyMemory<byte> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override void WriteByte(byte value) { }
    }
    public partial class QuicStreamAbortedException : System.Net.Quic.QuicException
    {
        public QuicStreamAbortedException(string message, long errorCode) : base (default(string)) { }
        public long ErrorCode { get { throw null; } }
    }
    public enum QuicStreamType
    {
        Unidirectional = 0,
        Bidirectional = 1,
    }
}
