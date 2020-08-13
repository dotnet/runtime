// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    // Provides the underlying stream of data for network access.
    public class NetworkStream : Stream
    {
        // Used by the class to hold the underlying socket the stream uses.
        private readonly Socket _streamSocket;

        // Whether the stream should dispose of the socket when the stream is disposed
        private readonly bool _ownsSocket;

        // Used by the class to indicate that the stream is m_Readable.
        private bool _readable;

        // Used by the class to indicate that the stream is writable.
        private bool _writeable;

        // Creates a new instance of the System.Net.Sockets.NetworkStream class for the specified System.Net.Sockets.Socket.
        public NetworkStream(Socket socket)
            : this(socket, FileAccess.ReadWrite, ownsSocket: false)
        {
        }

        public NetworkStream(Socket socket, bool ownsSocket)
            : this(socket, FileAccess.ReadWrite, ownsSocket)
        {
        }

        public NetworkStream(Socket socket, FileAccess access)
            : this(socket, access, ownsSocket: false)
        {
        }

        public NetworkStream(Socket socket, FileAccess access, bool ownsSocket)
        {
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }
            if (!socket.Blocking)
            {
                // Stream.Read*/Write* are incompatible with the semantics of non-blocking sockets, and
                // allowing non-blocking sockets could result in non-deterministic failures from those
                // operations. A developer that requires using NetworkStream with a non-blocking socket can
                // temporarily flip Socket.Blocking as a workaround.
                throw GetCustomNetworkException(SR.net_sockets_blocking);
            }
            if (!socket.Connected)
            {
                throw GetCustomNetworkException(SR.net_notconnected);
            }
            if (socket.SocketType != SocketType.Stream)
            {
                throw GetCustomNetworkException(SR.net_notstream);
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

        public Socket Socket => _streamSocket;

        // Used by the class to indicate that the stream is m_Readable.
        protected bool Readable
        {
            get { return _readable; }
            set { _readable = value; }
        }

        // Used by the class to indicate that the stream is writable.
        protected bool Writeable
        {
            get { return _writeable; }
            set { _writeable = value; }
        }

        // Indicates that data can be read from the stream.
        // We return the readability of this stream. This is a read only property.
        public override bool CanRead => _readable;

        // Indicates that the stream can seek a specific location
        // in the stream. This property always returns false.
        public override bool CanSeek => false;

        // Indicates that data can be written to the stream.
        public override bool CanWrite => _writeable;

        // Indicates whether we can timeout
        public override bool CanTimeout => true;

        // Set/Get ReadTimeout, note of a strange behavior, 0 timeout == infinite for sockets,
        // so we map this to -1, and if you set 0, we cannot support it
        public override int ReadTimeout
        {
            get
            {
                int timeout = (int)_streamSocket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout)!;
                if (timeout == 0)
                {
                    return -1;
                }
                return timeout;
            }
            set
            {
                if (value <= 0 && value != System.Threading.Timeout.Infinite)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.net_io_timeout_use_gt_zero);
                }
                SetSocketTimeoutOption(SocketShutdown.Receive, value, false);
            }
        }

        // Set/Get WriteTimeout, note of a strange behavior, 0 timeout == infinite for sockets,
        // so we map this to -1, and if you set 0, we cannot support it
        public override int WriteTimeout
        {
            get
            {
                int timeout = (int)_streamSocket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout)!;
                if (timeout == 0)
                {
                    return -1;
                }
                return timeout;
            }
            set
            {
                if (value <= 0 && value != System.Threading.Timeout.Infinite)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.net_io_timeout_use_gt_zero);
                }
                SetSocketTimeoutOption(SocketShutdown.Send, value, false);
            }
        }

        // Indicates data is available on the stream to be read.
        // This property checks to see if at least one byte of data is currently available
        public virtual bool DataAvailable
        {
            get
            {
                ThrowIfDisposed();

                // Ask the socket how many bytes are available. If it's
                // not zero, return true.
                return _streamSocket.Available != 0;
            }
        }

        // The length of data available on the stream. Always throws NotSupportedException.
        public override long Length
        {
            get
            {
                throw new NotSupportedException(SR.net_noseek);
            }
        }

        // Gets or sets the position in the stream. Always throws NotSupportedException.
        public override long Position
        {
            get
            {
                throw new NotSupportedException(SR.net_noseek);
            }

            set
            {
                throw new NotSupportedException(SR.net_noseek);
            }
        }

        // Seeks a specific position in the stream. This method is not supported by the
        // NetworkStream class.
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException(SR.net_noseek);
        }

        // Read - provide core Read functionality.
        //
        // Provide core read functionality. All we do is call through to the
        // socket Receive functionality.
        //
        // Input:
        //
        //     Buffer  - Buffer to read into.
        //     Offset  - Offset into the buffer where we're to read.
        //     Count   - Number of bytes to read.
        //
        // Returns:
        //
        //     Number of bytes we read, or 0 if the socket is closed.
        public override int Read(byte[] buffer, int offset, int size)
        {
            bool canRead = CanRead;  // Prevent race with Dispose.
            ThrowIfDisposed();
            if (!canRead)
            {
                throw new InvalidOperationException(SR.net_writeonlystream);
            }

            // Validate input parameters.
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if ((uint)offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if ((uint)size > buffer.Length - offset)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            try
            {
                return _streamSocket.Receive(buffer, offset, size, 0);
            }
            catch (SocketException socketException)
            {
                throw NetworkErrorHelper.MapSocketException(socketException);
            }
            catch (Exception exception) when (!(exception is OutOfMemoryException))
            {
                throw GetCustomNetworkException(SR.Format(SR.net_io_readfailure, exception.Message), exception);
            }
        }

        public override int Read(Span<byte> buffer)
        {
            if (GetType() != typeof(NetworkStream))
            {
                // NetworkStream is not sealed, and a derived type may have overridden Read(byte[], int, int) prior
                // to this Read(Span<byte>) overload being introduced.  In that case, this Read(Span<byte>) overload
                // should use the behavior of Read(byte[],int,int) overload.
                return base.Read(buffer);
            }

            ThrowIfDisposed();
            if (!CanRead) throw new InvalidOperationException(SR.net_writeonlystream);

            int bytesRead = _streamSocket.Receive(buffer, SocketFlags.None, out SocketError errorCode);
            if (errorCode != SocketError.Success)
            {
                var exception = new SocketException((int)errorCode);
                throw NetworkErrorHelper.MapSocketException(exception);
            }
            return bytesRead;
        }

        public override unsafe int ReadByte()
        {
            byte b;
            return Read(new Span<byte>(&b, 1)) == 0 ? -1 : b;
        }

        // Write - provide core Write functionality.
        //
        // Provide core write functionality. All we do is call through to the
        // socket Send method..
        //
        // Input:
        //
        //     Buffer  - Buffer to write from.
        //     Offset  - Offset into the buffer from where we'll start writing.
        //     Count   - Number of bytes to write.
        //
        // Returns:
        //
        //     Number of bytes written. We'll throw an exception if we
        //     can't write everything. It's brutal, but there's no other
        //     way to indicate an error.
        public override void Write(byte[] buffer, int offset, int size)
        {
            bool canWrite = CanWrite; // Prevent race with Dispose.
            ThrowIfDisposed();
            if (!canWrite)
            {
                throw new InvalidOperationException(SR.net_readonlystream);
            }

            // Validate input parameters.
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if ((uint)offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if ((uint)size > buffer.Length - offset)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            try
            {
                // Since the socket is in blocking mode this will always complete
                // after ALL the requested number of bytes was transferred.
                _streamSocket.Send(buffer, offset, size, SocketFlags.None);
            }
            catch (SocketException socketException)
            {
                throw NetworkErrorHelper.MapSocketException(socketException);
            }
            catch (Exception exception) when (!(exception is OutOfMemoryException))
            {
                throw GetCustomNetworkException(SR.Format(SR.net_io_writefailure, exception.Message), exception);
            }
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (GetType() != typeof(NetworkStream))
            {
                // NetworkStream is not sealed, and a derived type may have overridden Write(byte[], int, int) prior
                // to this Write(ReadOnlySpan<byte>) overload being introduced.  In that case, this Write(ReadOnlySpan<byte>)
                // overload should use the behavior of Write(byte[],int,int) overload.
                base.Write(buffer);
                return;
            }

            ThrowIfDisposed();
            if (!CanWrite) throw new InvalidOperationException(SR.net_readonlystream);

            _streamSocket.Send(buffer, SocketFlags.None, out SocketError errorCode);
            if (errorCode != SocketError.Success)
            {
                var exception = new SocketException((int)errorCode);
                throw NetworkErrorHelper.MapSocketException(exception);
            }
        }

        public override unsafe void WriteByte(byte value) =>
            Write(new ReadOnlySpan<byte>(&value, 1));

        private int _closeTimeout = Socket.DefaultCloseTimeout; // -1 = respect linger options

        public void Close(int timeout)
        {
            if (timeout < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }
            _closeTimeout = timeout;
            Dispose();
        }
        private volatile bool _disposed;
        protected override void Dispose(bool disposing)
        {
            // Mark this as disposed before changing anything else.
            bool disposed = _disposed;
            _disposed = true;
            if (!disposed && disposing)
            {
                // The only resource we need to free is the network stream, since this
                // is based on the client socket, closing the stream will cause us
                // to flush the data to the network, close the stream and (in the
                // NetoworkStream code) close the socket as well.
                _readable = false;
                _writeable = false;
                if (_ownsSocket)
                {
                    // If we own the Socket (false by default), close it
                    // ignoring possible exceptions (eg: the user told us
                    // that we own the Socket but it closed at some point of time,
                    // here we would get an ObjectDisposedException)
                    _streamSocket.InternalShutdown(SocketShutdown.Both);
                    _streamSocket.Close(_closeTimeout);
                }
            }

            base.Dispose(disposing);
        }

        ~NetworkStream() => Dispose(false);

        // BeginRead - provide async read functionality.
        //
        // This method provides async read functionality. All we do is
        // call through to the underlying socket async read.
        //
        // Input:
        //
        //     buffer  - Buffer to read into.
        //     offset  - Offset into the buffer where we're to read.
        //     size   - Number of bytes to read.
        //
        // Returns:
        //
        //     An IASyncResult, representing the read.
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int size, AsyncCallback? callback, object? state)
        {
            bool canRead = CanRead; // Prevent race with Dispose.
            ThrowIfDisposed();
            if (!canRead)
            {
                throw new InvalidOperationException(SR.net_writeonlystream);
            }

            // Validate input parameters.
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if ((uint)offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if ((uint)size > buffer.Length - offset)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            try
            {
                return _streamSocket.BeginReceive(
                        buffer,
                        offset,
                        size,
                        SocketFlags.None,
                        callback,
                        state);
            }
            catch (SocketException socketException)
            {
                throw NetworkErrorHelper.MapSocketException(socketException);
            }
            catch (Exception exception) when (!(exception is OutOfMemoryException))
            {
                throw GetCustomNetworkException(SR.Format(SR.net_io_readfailure, exception.Message), exception);
            }
        }

        // EndRead - handle the end of an async read.
        //
        // This method is called when an async read is completed. All we
        // do is call through to the core socket EndReceive functionality.
        //
        // Returns:
        //
        //     The number of bytes read. May throw an exception.
        public override int EndRead(IAsyncResult asyncResult)
        {
            ThrowIfDisposed();

            // Validate input parameters.
            if (asyncResult == null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }

            try
            {
                return _streamSocket.EndReceive(asyncResult);
            }
            catch (SocketException socketException)
            {
                throw NetworkErrorHelper.MapSocketException(socketException);
            }
            catch (Exception exception) when (!(exception is OutOfMemoryException))
            {
                throw GetCustomNetworkException(SR.Format(SR.net_io_readfailure, exception.Message), exception);
            }
        }

        // BeginWrite - provide async write functionality.
        //
        // This method provides async write functionality. All we do is
        // call through to the underlying socket async send.
        //
        // Input:
        //
        //     buffer  - Buffer to write into.
        //     offset  - Offset into the buffer where we're to write.
        //     size   - Number of bytes to written.
        //
        // Returns:
        //
        //     An IASyncResult, representing the write.
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int size, AsyncCallback? callback, object? state)
        {
            bool canWrite = CanWrite; // Prevent race with Dispose.
            ThrowIfDisposed();
            if (!canWrite)
            {
                throw new InvalidOperationException(SR.net_readonlystream);
            }

            // Validate input parameters.
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if ((uint)offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if ((uint)size > buffer.Length - offset)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            try
            {
                // Call BeginSend on the Socket.
                return _streamSocket.BeginSend(
                        buffer,
                        offset,
                        size,
                        SocketFlags.None,
                        callback,
                        state);
            }
            catch (SocketException socketException)
            {
                throw NetworkErrorHelper.MapSocketException(socketException);
            }
            catch (Exception exception) when (!(exception is OutOfMemoryException))
            {
                throw GetCustomNetworkException(SR.Format(SR.net_io_writefailure, exception.Message), exception);
            }
        }

        // Handle the end of an asynchronous write.
        // This method is called when an async write is completed. All we
        // do is call through to the core socket EndSend functionality.
        // Returns:  The number of bytes read. May throw an exception.
        public override void EndWrite(IAsyncResult asyncResult)
        {
            ThrowIfDisposed();

            // Validate input parameters.
            if (asyncResult == null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }

            try
            {
                _streamSocket.EndSend(asyncResult);
            }
            catch (SocketException socketException)
            {
                throw NetworkErrorHelper.MapSocketException(socketException);
            }
            catch (Exception exception) when (!(exception is OutOfMemoryException))
            {
                throw GetCustomNetworkException(SR.Format(SR.net_io_writefailure, exception.Message), exception);
            }
        }

        // ReadAsync - provide async read functionality.
        //
        // This method provides async read functionality. All we do is
        // call through to the Begin/EndRead methods.
        //
        // Input:
        //
        //     buffer            - Buffer to read into.
        //     offset            - Offset into the buffer where we're to read.
        //     size              - Number of bytes to read.
        //     cancellationToken - Token used to request cancellation of the operation
        //
        // Returns:
        //
        //     A Task<int> representing the read.
        public override Task<int> ReadAsync(byte[] buffer, int offset, int size, CancellationToken cancellationToken)
        {
            bool canRead = CanRead; // Prevent race with Dispose.
            ThrowIfDisposed();
            if (!canRead)
            {
                throw new InvalidOperationException(SR.net_writeonlystream);
            }

            // Validate input parameters.
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if ((uint)offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if ((uint)size > buffer.Length - offset)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            try
            {
                return _streamSocket.ReceiveAsync(
                    new Memory<byte>(buffer, offset, size),
                    SocketFlags.None,
                    fromNetworkStream: true,
                    cancellationToken).AsTask();
            }
            catch (SocketException socketException)
            {
                throw NetworkErrorHelper.MapSocketException(socketException);
            }
            catch (Exception exception) when (!(exception is OutOfMemoryException))
            {
                throw GetCustomNetworkException(SR.Format(SR.net_io_readfailure, exception.Message), exception);
            }
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            bool canRead = CanRead; // Prevent race with Dispose.
            ThrowIfDisposed();
            if (!canRead)
            {
                throw new InvalidOperationException(SR.net_writeonlystream);
            }

            try
            {
                return _streamSocket.ReceiveAsync(
                    buffer,
                    SocketFlags.None,
                    fromNetworkStream: true,
                    cancellationToken: cancellationToken);
            }
            catch (SocketException socketException)
            {
                throw NetworkErrorHelper.MapSocketException(socketException);
            }
            catch (Exception exception) when (!(exception is OutOfMemoryException))
            {
                throw GetCustomNetworkException(SR.Format(SR.net_io_readfailure, exception.Message), exception);
            }
        }

        // WriteAsync - provide async write functionality.
        //
        // This method provides async write functionality. All we do is
        // call through to the Begin/EndWrite methods.
        //
        // Input:
        //
        //     buffer  - Buffer to write into.
        //     offset  - Offset into the buffer where we're to write.
        //     size    - Number of bytes to write.
        //     cancellationToken - Token used to request cancellation of the operation
        //
        // Returns:
        //
        //     A Task representing the write.
        public override Task WriteAsync(byte[] buffer, int offset, int size, CancellationToken cancellationToken)
        {
            bool canWrite = CanWrite; // Prevent race with Dispose.
            ThrowIfDisposed();
            if (!canWrite)
            {
                throw new InvalidOperationException(SR.net_readonlystream);
            }

            // Validate input parameters.
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if ((uint)offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if ((uint)size > buffer.Length - offset)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            try
            {
                return _streamSocket.SendAsyncForNetworkStream(
                    new ReadOnlyMemory<byte>(buffer, offset, size),
                    SocketFlags.None,
                    cancellationToken).AsTask();
            }
            catch (SocketException socketException)
            {
                throw NetworkErrorHelper.MapSocketException(socketException);
            }
            catch (Exception exception) when (!(exception is OutOfMemoryException))
            {
                throw GetCustomNetworkException(SR.Format(SR.net_io_writefailure, exception.Message), exception);
            }
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            bool canWrite = CanWrite; // Prevent race with Dispose.
            ThrowIfDisposed();
            if (!canWrite)
            {
                throw new InvalidOperationException(SR.net_readonlystream);
            }

            try
            {
                return _streamSocket.SendAsyncForNetworkStream(
                    buffer,
                    SocketFlags.None,
                    cancellationToken);
            }
            catch (SocketException socketException)
            {
                throw NetworkErrorHelper.MapSocketException(socketException);
            }
            catch (Exception exception) when (!(exception is OutOfMemoryException))
            {
                throw GetCustomNetworkException(SR.Format(SR.net_io_writefailure, exception.Message), exception);
            }
        }

        // Flushes data from the stream.  This is meaningless for us, so it does nothing.
        public override void Flush()
        {
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        // Sets the length of the stream. Always throws NotSupportedException
        public override void SetLength(long value)
        {
            throw new NotSupportedException(SR.net_noseek);
        }

        private int _currentReadTimeout = -1;
        private int _currentWriteTimeout = -1;
        internal void SetSocketTimeoutOption(SocketShutdown mode, int timeout, bool silent)
        {
            if (timeout < 0)
            {
                timeout = 0; // -1 becomes 0 for the winsock stack
            }

            if (mode == SocketShutdown.Send || mode == SocketShutdown.Both)
            {
                if (timeout != _currentWriteTimeout)
                {
                    _streamSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, timeout, silent);
                    _currentWriteTimeout = timeout;
                }
            }

            if (mode == SocketShutdown.Receive || mode == SocketShutdown.Both)
            {
                if (timeout != _currentReadTimeout)
                {
                    _streamSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, timeout, silent);
                    _currentReadTimeout = timeout;
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                ThrowObjectDisposedException();
            }

            void ThrowObjectDisposedException() => throw new ObjectDisposedException(GetType().FullName);
        }

        private static NetworkException GetCustomNetworkException(string message, Exception? innerException = null)
        {
            return new NetworkException(message, NetworkError.Unknown, innerException);
        }
    }
}
