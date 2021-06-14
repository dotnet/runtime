// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace System.Net.WebSockets
{
    public class WebSocketStream : Stream
    {
        // Used by the class to hold the underlying socket the stream uses.
        private readonly WebSocket _streamSocket;

        // Whether the stream should dispose of the socket when the stream is disposed
        private readonly bool _ownsSocket;

        // Used by the class to indicate that the stream is m_Readable.
        private bool _readable;

        // Used by the class to indicate that the stream is writable.
        private bool _writeable;

        // Whether Dispose has been called. 0 == false, 1 == true
        private int _disposed;

        public WebSocketStream(WebSocket socket)
            : this(socket, FileAccess.ReadWrite, ownsSocket: false)
        {
        }

        public WebSocketStream(WebSocket socket, bool ownsSocket)
            : this(socket, FileAccess.ReadWrite, ownsSocket)
        {
        }

        public WebSocketStream(WebSocket socket, FileAccess access)
            : this(socket, access, ownsSocket: false)
        {
        }

        public WebSocketStream(WebSocket socket, FileAccess access, bool ownsSocket)
        {
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }
            if (socket.State != WebSocketState.Open)
            {
                throw new IOException("SR.net_notconnected");
            }

            _streamSocket = socket;
            _ownsSocket = ownsSocket;

            switch (access)
            {
                case FileAccess.Read:
                    _readable = true;
                    break;
                case FileAccess.Write:
                    _writeable = true;
                    break;
                case FileAccess.ReadWrite:
                default: // assume FileAccess.ReadWrite
                    _readable = true;
                    _writeable = true;
                    break;
            }
        }

        public WebSocket Socket => _streamSocket;

        protected bool Readable
        {
            get { return _readable; }
            set { _readable = value; }
        }

        protected bool Writeable
        {
            get { return _writeable; }
            set { _writeable = value; }
        }

        public override bool CanRead => _readable;


        public override bool CanSeek => false;


        public override bool CanWrite => _writeable;


        public override bool CanTimeout => true;


        public override long Length
        {
            get
            {
                throw new NotSupportedException("SR.net_noseek");
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException("SR.net_noseek");
            }

            set
            {
                throw new NotSupportedException("SR.net_noseek");
            }
        }


        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("SR.net_noseek");
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new IOException("SR.net_sockets_blocking");
        }

        public override int Read(Span<byte> buffer)
        {
            throw new IOException("SR.net_sockets_blocking");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new IOException("SR.net_sockets_blocking");
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            throw new IOException("SR.net_sockets_blocking");
        }

        private int _closeTimeout = -1;

        public void Close(int timeout)
        {
            if (timeout < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }
            _closeTimeout = timeout;
            Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            if (disposing)
            {
                _readable = false;
                _writeable = false;
                if (_ownsSocket)
                {
                    if (_streamSocket != null && (_streamSocket.State == WebSocketState.Open || _streamSocket.State == WebSocketState.Connecting || _streamSocket.State == WebSocketState.None))
                    {
                        try
                        {
                            var task = _streamSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing remoteLoop", CancellationToken.None);
                            Task.WaitAll(task);
                        }
                        catch (Exception)
                        {
                        }
                        finally
                        {
                            _streamSocket.Dispose();
                        }
                    }
                }
            }

            base.Dispose(disposing);
        }

        ~WebSocketStream() => Dispose(false);

        public async override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            ThrowIfDisposed();
            if (!CanRead)
            {
                throw new InvalidOperationException("SR.net_writeonlystream");
            }

            try
            {
                var res = await _streamSocket.ReceiveAsync(new Memory<byte>(buffer, offset, count), cancellationToken);
                return res.Count;
            }
            catch (Exception exception) when (!(exception is OutOfMemoryException))
            {
                throw WrapException("SR.net_io_readfailure", exception);
            }
        }

        public async override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            bool canRead = CanRead; // Prevent race with Dispose.
            ThrowIfDisposed();
            if (!canRead)
            {
                throw new InvalidOperationException("SR.net_writeonlystream");
            }

            try
            {
                var res = await _streamSocket.ReceiveAsync(buffer,
                    cancellationToken);
                return res.Count;
            }
            catch (Exception exception) when (!(exception is OutOfMemoryException))
            {
                throw WrapException("SR.net_io_readfailure", exception);
            }
        }

        public async override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            ThrowIfDisposed();
            if (!CanWrite)
            {
                throw new InvalidOperationException("SR.net_readonlystream");
            }

            try
            {
                await _streamSocket.SendAsync(new ReadOnlyMemory<byte>(buffer, offset, count), WebSocketMessageType.Binary, true, cancellationToken);
            }
            catch (Exception exception) when (!(exception is OutOfMemoryException))
            {
                throw WrapException("SR.net_io_writefailure", exception);
            }
        }

        public async override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            bool canWrite = CanWrite; // Prevent race with Dispose.
            ThrowIfDisposed();
            if (!canWrite)
            {
                throw new InvalidOperationException("SR.net_readonlystream");
            }

            try
            {
                await _streamSocket.SendAsync(buffer, WebSocketMessageType.Binary, true, cancellationToken);
            }
            catch (Exception exception) when (!(exception is OutOfMemoryException))
            {
                throw WrapException("SR.net_io_writefailure", exception);
            }
        }

        public override void Flush()
        {
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("SR.net_noseek");
        }

        private void ThrowIfDisposed()
        {
            if (_disposed != 0)
            {
                ThrowObjectDisposedException();
            }

            void ThrowObjectDisposedException() => throw new ObjectDisposedException(GetType().FullName);
        }

        private static IOException WrapException(string resourceFormatString, Exception innerException)
        {
            return new IOException(SR.Format(resourceFormatString, innerException.Message), innerException);
        }
    }
}
