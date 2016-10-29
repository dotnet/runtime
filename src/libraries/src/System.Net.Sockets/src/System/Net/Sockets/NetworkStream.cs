// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    // Provides the underlying stream of data for network access.
    public class NetworkStream : Stream
    {
        // Used by the class to hold the underlying socket the stream uses.
        private Socket _streamSocket;

        // Used by the class to indicate that the stream is m_Readable.
        private bool _readable;

        // Used by the class to indicate that the stream is writable.
        private bool _writeable;

        private bool _ownsSocket;

        // Creates a new instance of the System.Net.Sockets.NetworkStream without initialization.
        internal NetworkStream()
        {
            _ownsSocket = true;
        }

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
#if DEBUG
            using (DebugThreadTracking.SetThreadKind(ThreadKinds.User))
            {
#endif
                if (socket == null)
                {
                    throw new ArgumentNullException(nameof(socket));
                }
                if (!socket.Blocking)
                {
                    throw new IOException(SR.net_sockets_blocking);
                }
                if (!socket.Connected)
                {
                    throw new IOException(SR.net_notconnected);
                }
                if (socket.SocketType != SocketType.Stream)
                {
                    throw new IOException(SR.net_notstream);
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
#if DEBUG
            }
#endif
        }

        // Socket - provides access to socket for stream closing
        protected Socket Socket
        {
            get
            {
                return _streamSocket;
            }
        }

        internal Socket InternalSocket
        {
            get
            {
                Socket chkSocket = _streamSocket;
                if (_cleanedUp || chkSocket == null)
                {
                    throw new ObjectDisposedException(this.GetType().FullName);
                }

                return chkSocket;
            }
        }

        internal void InternalAbortSocket()
        {
            if (!_ownsSocket)
            {
                throw new InvalidOperationException();
            }

            Socket chkSocket = _streamSocket;
            if (_cleanedUp || chkSocket == null)
            {
                return;
            }

            try
            {
                chkSocket.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        internal void ConvertToNotSocketOwner()
        {
            _ownsSocket = false;
            // Suppress for finialization still allow proceed the requests
            GC.SuppressFinalize(this);
        }

        // Used by the class to indicate that the stream is m_Readable.
        protected bool Readable
        {
            get
            {
                return _readable;
            }
            set
            {
                _readable = value;
            }
        }

        // Used by the class to indicate that the stream is writable.
        protected bool Writeable
        {
            get
            {
                return _writeable;
            }
            set
            {
                _writeable = value;
            }
        }

        // Indicates that data can be read from the stream.
        // We return the readability of this stream. This is a read only property.
        public override bool CanRead
        {
            get
            {
                return _readable;
            }
        }

        // Indicates that the stream can seek a specific location
        // in the stream. This property always returns false.
        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        // Indicates that data can be written to the stream.
        public override bool CanWrite
        {
            get
            {
                return _writeable;
            }
        }

        // Indicates whether we can timeout
        public override bool CanTimeout
        {
            get
            {
                return true; // should we check for Connected state?
            }
        }

        // Set/Get ReadTimeout, note of a strange behavior, 0 timeout == infinite for sockets,
        // so we map this to -1, and if you set 0, we cannot support it
        public override int ReadTimeout
        {
            get
            {
#if DEBUG
                using (DebugThreadTracking.SetThreadKind(ThreadKinds.User | ThreadKinds.Async))
                {
#endif
                    int timeout = (int)_streamSocket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout);
                    if (timeout == 0)
                    {
                        return -1;
                    }
                    return timeout;
#if DEBUG
                }
#endif
            }
            set
            {
#if DEBUG
                using (DebugThreadTracking.SetThreadKind(ThreadKinds.User | ThreadKinds.Async))
                {
#endif
                    if (value <= 0 && value != System.Threading.Timeout.Infinite)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), SR.net_io_timeout_use_gt_zero);
                    }
                    SetSocketTimeoutOption(SocketShutdown.Receive, value, false);
#if DEBUG
                }
#endif
            }
        }

        // Set/Get WriteTimeout, note of a strange behavior, 0 timeout == infinite for sockets,
        // so we map this to -1, and if you set 0, we cannot support it
        public override int WriteTimeout
        {
            get
            {
#if DEBUG
                using (DebugThreadTracking.SetThreadKind(ThreadKinds.User | ThreadKinds.Async))
                {
#endif
                    int timeout = (int)_streamSocket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout);
                    if (timeout == 0)
                    {
                        return -1;
                    }
                    return timeout;
#if DEBUG
                }
#endif
            }
            set
            {
#if DEBUG
                using (DebugThreadTracking.SetThreadKind(ThreadKinds.User | ThreadKinds.Async))
                {
#endif
                    if (value <= 0 && value != System.Threading.Timeout.Infinite)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), SR.net_io_timeout_use_gt_zero);
                    }
                    SetSocketTimeoutOption(SocketShutdown.Send, value, false);
#if DEBUG
                }
#endif
            }
        }

        // Indicates data is available on the stream to be read.
        // This property checks to see if at least one byte of data is currently available            
        public virtual bool DataAvailable
        {
            get
            {
#if DEBUG
                using (DebugThreadTracking.SetThreadKind(ThreadKinds.User | ThreadKinds.Async))
                {
#endif
                    if (_cleanedUp)
                    {
                        throw new ObjectDisposedException(this.GetType().FullName);
                    }

                    Socket chkStreamSocket = _streamSocket;
                    if (chkStreamSocket == null)
                    {
                        throw new IOException(SR.Format(SR.net_io_readfailure, SR.net_io_connectionclosed));
                    }

                    // Ask the socket how many bytes are available. If it's
                    // not zero, return true.
                    return chkStreamSocket.Available != 0;
#if DEBUG
                }
#endif
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

        internal bool PollRead()
        {
            if (_cleanedUp)
            {
                return false;
            }

            Socket chkStreamSocket = _streamSocket;
            if (chkStreamSocket == null)
            {
                return false;
            }
            return chkStreamSocket.Poll(0, SelectMode.SelectRead);
        }

        internal bool Poll(int microSeconds, SelectMode mode)
        {
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

            Socket chkStreamSocket = _streamSocket;
            if (chkStreamSocket == null)
            {
                throw new IOException(SR.Format(SR.net_io_readfailure, SR.net_io_connectionclosed));
            }

            return chkStreamSocket.Poll(microSeconds, mode);
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
#if DEBUG
            using (DebugThreadTracking.SetThreadKind(ThreadKinds.User | ThreadKinds.Sync))
            {
#endif
                bool canRead = CanRead;  // Prevent race with Dispose.
                if (_cleanedUp)
                {
                    throw new ObjectDisposedException(this.GetType().FullName);
                }
                if (!canRead)
                {
                    throw new InvalidOperationException(SR.net_writeonlystream);
                }

                // Validate input parameters.
                if (buffer == null)
                {
                    throw new ArgumentNullException(nameof(buffer));
                }
                if (offset < 0 || offset > buffer.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(offset));
                }
                if (size < 0 || size > buffer.Length - offset)
                {
                    throw new ArgumentOutOfRangeException(nameof(size));
                }

                Socket chkStreamSocket = _streamSocket;
                if (chkStreamSocket == null)
                {
                    throw new IOException(SR.Format(SR.net_io_readfailure, SR.net_io_connectionclosed));
                }

                try
                {
                    int bytesTransferred = chkStreamSocket.Receive(buffer, offset, size, 0);
                    return bytesTransferred;
                }
                catch (Exception exception)
                {
                    if (exception is OutOfMemoryException)
                    {
                        throw;
                    }

                    // Some sort of error occurred on the socket call,
                    // set the SocketException as InnerException and throw.
                    throw new IOException(SR.Format(SR.net_io_readfailure, exception.Message), exception);
                }
#if DEBUG
            }
#endif
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
#if DEBUG
            using (DebugThreadTracking.SetThreadKind(ThreadKinds.User | ThreadKinds.Sync))
            {
#endif
                bool canWrite = CanWrite; // Prevent race with Dispose.
                if (_cleanedUp)
                {
                    throw new ObjectDisposedException(this.GetType().FullName);
                }
                if (!canWrite)
                {
                    throw new InvalidOperationException(SR.net_readonlystream);
                }

                // Validate input parameters.
                if (buffer == null)
                {
                    throw new ArgumentNullException(nameof(buffer));
                }
                if (offset < 0 || offset > buffer.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(offset));
                }
                if (size < 0 || size > buffer.Length - offset)
                {
                    throw new ArgumentOutOfRangeException(nameof(size));
                }

                Socket chkStreamSocket = _streamSocket;
                if (chkStreamSocket == null)
                {
                    throw new IOException(SR.Format(SR.net_io_writefailure, SR.net_io_connectionclosed));
                }

                try
                {
                    // Since the socket is in blocking mode this will always complete
                    // after ALL the requested number of bytes was transferred.
                    chkStreamSocket.Send(buffer, offset, size, SocketFlags.None);
                }
                catch (Exception exception)
                {
                    if (exception is OutOfMemoryException)
                    {
                        throw;
                    }

                    // Some sort of error occurred on the socket call,
                    // set the SocketException as InnerException and throw.
                    throw new IOException(SR.Format(SR.net_io_writefailure, exception.Message), exception);
                }
#if DEBUG
            }
#endif
        }

        private int _closeTimeout = Socket.DefaultCloseTimeout; // -1 = respect linger options

        public void Close(int timeout)
        {
#if DEBUG
            using (DebugThreadTracking.SetThreadKind(ThreadKinds.User | ThreadKinds.Sync))
            {
#endif
                if (timeout < -1)
                {
                    throw new ArgumentOutOfRangeException(nameof(timeout));
                }
                _closeTimeout = timeout;
                Dispose();
#if DEBUG
            }
#endif
        }
        private volatile bool _cleanedUp = false;
        protected override void Dispose(bool disposing)
        {
#if DEBUG
            using (DebugThreadTracking.SetThreadKind(ThreadKinds.User))
            {
#endif
                // Mark this as disposed before changing anything else.
                bool cleanedUp = _cleanedUp;
                _cleanedUp = true;
                if (!cleanedUp && disposing)
                {
                    // The only resource we need to free is the network stream, since this
                    // is based on the client socket, closing the stream will cause us
                    // to flush the data to the network, close the stream and (in the
                    // NetoworkStream code) close the socket as well.
                    if (_streamSocket != null)
                    {
                        _readable = false;
                        _writeable = false;
                        if (_ownsSocket)
                        {
                            // If we own the Socket (false by default), close it
                            // ignoring possible exceptions (eg: the user told us
                            // that we own the Socket but it closed at some point of time,
                            // here we would get an ObjectDisposedException)
                            Socket chkStreamSocket = _streamSocket;
                            if (chkStreamSocket != null)
                            {
                                chkStreamSocket.InternalShutdown(SocketShutdown.Both);
                                chkStreamSocket.Close(_closeTimeout);
                            }
                        }
                    }
                }
#if DEBUG
            }
#endif
            base.Dispose(disposing);
        }

        ~NetworkStream()
        {
#if DEBUG
            DebugThreadTracking.SetThreadSource(ThreadKinds.Finalization);
#endif
            Dispose(false);
        }

        // Indicates whether the stream is still connected
        internal bool Connected
        {
            get
            {
                Socket socket = _streamSocket;
                if (!_cleanedUp && socket != null && socket.Connected)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

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
#if !netcore50
        override
#endif
        public IAsyncResult BeginRead(byte[] buffer, int offset, int size, AsyncCallback callback, Object state)
        {
#if DEBUG
            using (DebugThreadTracking.SetThreadKind(ThreadKinds.User | ThreadKinds.Async))
            {
#endif
                bool canRead = CanRead; // Prevent race with Dispose.
                if (_cleanedUp)
                {
                    throw new ObjectDisposedException(this.GetType().FullName);
                }
                if (!canRead)
                {
                    throw new InvalidOperationException(SR.net_writeonlystream);
                }

                // Validate input parameters.
                if (buffer == null)
                {
                    throw new ArgumentNullException(nameof(buffer));
                }
                if (offset < 0 || offset > buffer.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(offset));
                }
                if (size < 0 || size > buffer.Length - offset)
                {
                    throw new ArgumentOutOfRangeException(nameof(size));
                }

                Socket chkStreamSocket = _streamSocket;
                if (chkStreamSocket == null)
                {
                    throw new IOException(SR.Format(SR.net_io_readfailure, SR.net_io_connectionclosed));
                }

                try
                {
                    IAsyncResult asyncResult =
                        chkStreamSocket.BeginReceive(
                            buffer,
                            offset,
                            size,
                            SocketFlags.None,
                            callback,
                            state);

                    return asyncResult;
                }
                catch (Exception exception)
                {
                    if (exception is OutOfMemoryException)
                    {
                        throw;
                    }

                    // Some sort of error occurred on the socket call,
                    // set the SocketException as InnerException and throw.
                    throw new IOException(SR.Format(SR.net_io_readfailure, exception.Message), exception);
                }
#if DEBUG
            }
#endif
        }

        internal virtual IAsyncResult UnsafeBeginRead(byte[] buffer, int offset, int size, AsyncCallback callback, Object state)
        {
            bool canRead = CanRead; // Prevent race with Dispose.
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (!canRead)
            {
                throw new InvalidOperationException(SR.net_writeonlystream);
            }

            Socket chkStreamSocket = _streamSocket;
            if (chkStreamSocket == null)
            {
                throw new IOException(SR.Format(SR.net_io_readfailure, SR.net_io_connectionclosed));
            }

            try
            {
                IAsyncResult asyncResult = chkStreamSocket.UnsafeBeginReceive(
                    buffer,
                    offset,
                    size,
                    SocketFlags.None,
                    callback,
                    state);

                return asyncResult;
            }
            catch (Exception exception)
            {
                if (ExceptionCheck.IsFatal(exception)) throw;

                // Some sort of error occurred on the socket call,
                // set the SocketException as InnerException and throw.
                throw new IOException(SR.Format(SR.net_io_readfailure, exception.Message), exception);
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
#if !netcore50
        override
#endif
        public int EndRead(IAsyncResult asyncResult)
        {
#if DEBUG
            using (DebugThreadTracking.SetThreadKind(ThreadKinds.User))
            {
#endif
                if (_cleanedUp)
                {
                    throw new ObjectDisposedException(this.GetType().FullName);
                }

                // Validate input parameters.
                if (asyncResult == null)
                {
                    throw new ArgumentNullException(nameof(asyncResult));
                }

                Socket chkStreamSocket = _streamSocket;
                if (chkStreamSocket == null)
                {
                    throw new IOException(SR.Format(SR.net_io_readfailure, SR.net_io_connectionclosed));
                }

                try
                {
                    int bytesTransferred = chkStreamSocket.EndReceive(asyncResult);
                    return bytesTransferred;
                }
                catch (Exception exception)
                {
                    if (exception is OutOfMemoryException)
                    {
                        throw;
                    }

                    // Some sort of error occurred on the socket call,
                    // set the SocketException as InnerException and throw.
                    throw new IOException(SR.Format(SR.net_io_readfailure, exception.Message), exception);
                }
#if DEBUG
            }
#endif
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
#if !netcore50
        override
#endif
        public IAsyncResult BeginWrite(byte[] buffer, int offset, int size, AsyncCallback callback, Object state)
        {
#if DEBUG
            using (DebugThreadTracking.SetThreadKind(ThreadKinds.User | ThreadKinds.Async))
            {
#endif
                bool canWrite = CanWrite; // Prevent race with Dispose.
                if (_cleanedUp)
                {
                    throw new ObjectDisposedException(this.GetType().FullName);
                }
                if (!canWrite)
                {
                    throw new InvalidOperationException(SR.net_readonlystream);
                }

                // Validate input parameters.
                if (buffer == null)
                {
                    throw new ArgumentNullException(nameof(buffer));
                }
                if (offset < 0 || offset > buffer.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(offset));
                }
                if (size < 0 || size > buffer.Length - offset)
                {
                    throw new ArgumentOutOfRangeException(nameof(size));
                }

                Socket chkStreamSocket = _streamSocket;
                if (chkStreamSocket == null)
                {
                    throw new IOException(SR.Format(SR.net_io_writefailure, SR.net_io_connectionclosed));
                }

                try
                {
                    // Call BeginSend on the Socket.
                    IAsyncResult asyncResult =
                        chkStreamSocket.BeginSend(
                            buffer,
                            offset,
                            size,
                            SocketFlags.None,
                            callback,
                            state);

                    return asyncResult;
                }
                catch (Exception exception)
                {
                    if (exception is OutOfMemoryException)
                    {
                        throw;
                    }

                    // Some sort of error occurred on the socket call,
                    // set the SocketException as InnerException and throw.
                    throw new IOException(SR.Format(SR.net_io_writefailure, exception.Message), exception);
                }
#if DEBUG
            }
#endif
        }

        internal virtual IAsyncResult UnsafeBeginWrite(byte[] buffer, int offset, int size, AsyncCallback callback, Object state)
        {
#if DEBUG
            using (DebugThreadTracking.SetThreadKind(ThreadKinds.User | ThreadKinds.Async))
            {
#endif
                bool canWrite = CanWrite; // Prevent race with Dispose.
                if (_cleanedUp)
                {
                    throw new ObjectDisposedException(this.GetType().FullName);
                }

                if (!canWrite)
                {
                    throw new InvalidOperationException(SR.net_readonlystream);
                }

                Socket chkStreamSocket = _streamSocket;
                if (chkStreamSocket == null)
                {
                    throw new IOException(SR.Format(SR.net_io_writefailure, SR.net_io_connectionclosed));
                }

                try
                {
                    // Call BeginSend on the Socket.
                    IAsyncResult asyncResult =
                        chkStreamSocket.UnsafeBeginSend(
                            buffer,
                            offset,
                            size,
                            SocketFlags.None,
                            callback,
                            state);

                    return asyncResult;
                }
                catch (Exception exception)
                {
                    if (exception is OutOfMemoryException)
                    {
                        throw;
                    }

                    // Some sort of error occurred on the socket call,
                    // set the SocketException as InnerException and throw.
                    throw new IOException(SR.Format(SR.net_io_writefailure, exception.Message), exception);
                }
#if DEBUG
            }
#endif
        }

        // Handle the end of an asynchronous write.
        // This method is called when an async write is completed. All we
        // do is call through to the core socket EndSend functionality.
        // Returns:  The number of bytes read. May throw an exception.
#if !netcore50
        override
#endif
        public void EndWrite(IAsyncResult asyncResult)
        {
#if DEBUG
            using (DebugThreadTracking.SetThreadKind(ThreadKinds.User))
            {
#endif
                if (_cleanedUp)
                {
                    throw new ObjectDisposedException(this.GetType().FullName);
                }

                // Validate input parameters.
                if (asyncResult == null)
                {
                    throw new ArgumentNullException(nameof(asyncResult));
                }

                Socket chkStreamSocket = _streamSocket;
                if (chkStreamSocket == null)
                {
                    throw new IOException(SR.Format(SR.net_io_writefailure, SR.net_io_connectionclosed));
                }

                try
                {
                    chkStreamSocket.EndSend(asyncResult);
                }
                catch (Exception exception)
                {
                    if (exception is OutOfMemoryException)
                    {
                        throw;
                    }

                    // Some sort of error occurred on the socket call,
                    // set the SocketException as InnerException and throw.
                    throw new IOException(SR.Format(SR.net_io_writefailure, exception.Message), exception);
                }
#if DEBUG
            }
#endif
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
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }

            return Task.Factory.FromAsync(
                (bufferArg, offsetArg, sizeArg, callback, state) => ((NetworkStream)state).BeginRead(bufferArg, offsetArg, sizeArg, callback, state),
                iar => ((NetworkStream)iar.AsyncState).EndRead(iar),
                buffer, 
                offset, 
                size, 
                this);
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
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }

            return Task.Factory.FromAsync(
                (bufferArg, offsetArg, sizeArg, callback, state) => ((NetworkStream)state).BeginWrite(bufferArg, offsetArg, sizeArg, callback, state),
                iar => ((NetworkStream)iar.AsyncState).EndWrite(iar),
                buffer, 
                offset, 
                size, 
                this);
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            // Validate arguments as would the base CopyToAsync
            StreamHelpers.ValidateCopyToArgs(this, destination, bufferSize);

            // And bail early if cancellation has already been requested
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            // Then do additional checks as ReadAsync would.

            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

            Socket streamSocket = _streamSocket;
            if (streamSocket == null)
            {
                throw new IOException(SR.Format(SR.net_io_readfailure, SR.net_io_connectionclosed));
            }

            // Do the copy.  We get a copy buffer from the shared pool, and we pass both it and the
            // socket into the copy as part of the event args so as to avoid additional fields in
            // the async method's state machine.
            return CopyToAsyncCore(
                destination,
                new AwaitableSocketAsyncEventArgs(streamSocket, ArrayPool<byte>.Shared.Rent(bufferSize)),
                cancellationToken);
        }

        private static async Task CopyToAsyncCore(Stream destination, AwaitableSocketAsyncEventArgs ea, CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    int bytesRead = await ea.ReceiveAsync();
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    await destination.WriteAsync(ea.Buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(ea.Buffer, clearArray: true);
                ea.Dispose();
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
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this, mode, timeout, silent);
            DebugThreadTracking.ThreadContract(ThreadKinds.Unknown, $"NetworkStream#{NetEventSource.IdOf(this)}");

            if (timeout < 0)
            {
                timeout = 0; // -1 becomes 0 for the winsock stack
            }

            Socket chkStreamSocket = _streamSocket;
            if (chkStreamSocket == null)
            {
                return;
            }
            if (mode == SocketShutdown.Send || mode == SocketShutdown.Both)
            {
                if (timeout != _currentWriteTimeout)
                {
                    chkStreamSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, timeout, silent);
                    _currentWriteTimeout = timeout;
                }
            }
            if (mode == SocketShutdown.Receive || mode == SocketShutdown.Both)
            {
                if (timeout != _currentReadTimeout)
                {
                    chkStreamSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, timeout, silent);
                    _currentReadTimeout = timeout;
                }
            }
        }

        [System.Diagnostics.Conditional("TRACE_VERBOSE")]
        internal void DebugMembers()
        {
            if (_streamSocket != null)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, _streamSocket);
                _streamSocket.DebugMembers();
            }
        }

        /// <summary>A SocketAsyncEventArgs that can be awaited to get the result of an operation.</summary>
        internal sealed class AwaitableSocketAsyncEventArgs : SocketAsyncEventArgs, ICriticalNotifyCompletion
        {
            /// <summary>Sentinal object used to indicate that the operation has completed prior to OnCompleted being called.</summary>
            private static readonly Action s_completedSentinel = () => { };
            /// <summary>
            /// null if the operation has not completed, <see cref="s_completedSentinel"/> if it has, and another object
            /// if OnCompleted was called before the operation could complete, in which case it's the delegate to invoke
            /// when the operation does complete.
            /// </summary>
            private Action _continuation;

            /// <summary>Initializes the event args.</summary>
            /// <param name="socket">The associated socket.</param>
            /// <param name="buffer">The buffer to use for all operations.</param>
            public AwaitableSocketAsyncEventArgs(Socket socket, byte[] buffer)
            {
                Debug.Assert(socket != null);
                Debug.Assert(buffer != null && buffer.Length > 0);

                // Store the socket into the base's UserToken.  This avoids the need for an extra field, at the expense
                // of an object=>Socket cast when we need to access it, which is only once per operation.
                UserToken = socket;

                // Store the buffer for use by all operations with this instance.
                SetBuffer(buffer, 0, buffer.Length);

                // Hook up the completed event.
                Completed += delegate
                {
                    // When the operation completes, see if OnCompleted was already called to hook up a continuation.
                    // If it was, invoke the continuation.
                    Action c = _continuation;
                    if (c != null)
                    {
                        c();
                    }
                    else
                    {
                        // We may be racing with OnCompleted, so check with synchronization, trying to swap in our
                        // completion sentinel.  If we lose the race and OnCompleted did hook up a continuation,
                        // invoke it.  Otherwise, there's nothing more to be done.
                        Interlocked.CompareExchange(ref _continuation, s_completedSentinel, null)?.Invoke();
                    }
                };
            }

            /// <summary>Initiates a receive operation on the associated socket.</summary>
            /// <returns>This instance.</returns>
            public AwaitableSocketAsyncEventArgs ReceiveAsync()
            {
                if (!Socket.ReceiveAsync(this))
                {
                    _continuation = s_completedSentinel;
                }
                return this;
            }

            /// <summary>Gets this instance.</summary>
            public AwaitableSocketAsyncEventArgs GetAwaiter() => this;

            /// <summary>Gets whether the operation has already completed.</summary>
            /// <remarks>
            /// This is not a generically usable IsCompleted operation that suggests the whole operation has completed.
            /// Rather, it's specifically used as part of the await pattern, and is only usable to determine whether the
            /// operation has completed by the time the instance is awaited.
            /// </remarks>
            public bool IsCompleted => _continuation != null;

            /// <summary>Same as <see cref="OnCompleted(Action)"/> </summary>
            public void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);

            /// <summary>Queues the provided continuation to be executed once the operation has completed.</summary>
            public void OnCompleted(Action continuation)
            {
                if (_continuation == s_completedSentinel || Interlocked.CompareExchange(ref _continuation, continuation, null) == s_completedSentinel)
                {
                    Task.Run(continuation);
                }
            }

            /// <summary>Gets the result of the completion operation.</summary>
            /// <returns>Number of bytes transferred.</returns>
            /// <remarks>
            /// Unlike Task's awaiter's GetResult, this does not block until the operation completes: it must only
            /// be used once the operation has completed.  This is handled implicitly by await.
            /// </remarks>
            public int GetResult()
            {
                _continuation = null;
                if (SocketError != SocketError.Success)
                {
                    ThrowIOSocketException();
                }
                return BytesTransferred;
            }

            /// <summary>Gets the associated socket.</summary>
            internal Socket Socket => (Socket)UserToken; // stored in the base's UserToken to avoid an extra field in the object

            /// <summary>Throws an IOException wrapping a SocketException using the current <see cref="SocketError"/>.</summary>
            [MethodImpl(MethodImplOptions.NoInlining)]
            private void ThrowIOSocketException()
            {
                var se = new SocketException((int)SocketError);
                throw new IOException(SR.Format(SR.net_io_readfailure, se.Message), se);
            }
        }
    }
}
