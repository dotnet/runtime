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
    public sealed partial class QuicConnection : System.IDisposable
    {
        internal QuicConnection() { }
        public bool Connected { get { throw null; } }
        public static bool IsSupported { get { throw null; } }
        public System.Net.IPEndPoint? LocalEndPoint { get { throw null; } }
        public System.Net.Security.SslApplicationProtocol NegotiatedApplicationProtocol { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.X509Certificate? RemoteCertificate { get { throw null; } }
        public System.Net.EndPoint RemoteEndPoint { get { throw null; } }
        public System.Threading.Tasks.ValueTask<System.Net.Quic.QuicStream> AcceptStreamAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.Threading.Tasks.ValueTask CloseAsync(long errorCode, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.ValueTask<System.Net.Quic.QuicConnection> ConnectAsync(System.Net.Quic.QuicClientConnectionOptions options, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.Threading.Tasks.ValueTask ConnectAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public void Dispose() { }
        public int GetRemoteAvailableBidirectionalStreamCount() { throw null; }
        public int GetRemoteAvailableUnidirectionalStreamCount() { throw null; }
        public System.Threading.Tasks.ValueTask<System.Net.Quic.QuicStream> OpenBidirectionalStreamAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.Threading.Tasks.ValueTask<System.Net.Quic.QuicStream> OpenUnidirectionalStreamAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    public enum QuicError
    {
        Success = 0,
        InternalError = 1,
        ConnectionAborted = 2,
        StreamAborted = 3,
        AddressInUse = 4,
        InvalidAddress = 5,
        ConnectionTimeout = 6,
        HostUnreachable = 7,
        ConnectionRefused = 8,
        VersionNegotiationError = 9,
        ConnectionIdle = 10,
        ProtocolError = 11,
        OperationAborted = 12,
    }
    public abstract partial class QuicConnectionOptions
    {
        internal QuicConnectionOptions() { }
        public System.TimeSpan IdleTimeout { get { throw null; } set { } }
        public int MaxBidirectionalStreams { get { throw null; } set { } }
        public int MaxUnidirectionalStreams { get { throw null; } set { } }
    }
    public partial class QuicException : System.Exception
    {
        public QuicException(System.Net.Quic.QuicError error, long? applicationErrorCode, string message, System.Exception? innerException) { }
        public long? ApplicationErrorCode { get { throw null; } }
        public System.Net.Quic.QuicError QuicError { get { throw null; } }
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
}
