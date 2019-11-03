// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic
{
    public sealed class QuicStream : Stream
    {
        private bool _disposed = false;
        private readonly long _streamId;
        private bool _canRead;
        private bool _canWrite;
        private QuicConnection _connection;

        // !!! TEMPORARY FOR QUIC MOCK SUPPORT
        private readonly bool _mock = false;
        private Socket _socket = null;

        // Constructor for outbound streams
        // !!! TEMPORARY FOR QUIC MOCK SUPPORT
        internal QuicStream(QuicConnection connection, long streamId, bool bidirectional)
        {
            _mock = true;
            _connection = connection;
            _streamId = streamId;
            _canRead = bidirectional;
            _canWrite = true;
        }

        // Constructor for inbound streams
        // !!! TEMPORARY FOR QUIC MOCK SUPPORT
        internal QuicStream(Socket socket, long streamId, bool bidirectional)
        {
            _mock = true;
            _socket = socket;
            _streamId = streamId;
            _canRead = true;
            _canWrite = bidirectional;
        }

        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) =>
            TaskToApm.Begin(ReadAsync(buffer, offset, count, default), callback, state);

        public override int EndRead(IAsyncResult asyncResult) =>
            TaskToApm.End<int>(asyncResult);

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) =>
            TaskToApm.Begin(WriteAsync(buffer, offset, count, default), callback, state);

        public override void EndWrite(IAsyncResult asyncResult) =>
            TaskToApm.End(asyncResult);

        private static void ValidateBufferArgs(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if ((uint)offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if ((uint)count > buffer.Length - offset)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArgs(buffer, offset, count);
            return Read(buffer.AsSpan(offset, count));
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArgs(buffer, offset, count);
            return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateBufferArgs(buffer, offset, count);
            Write(buffer.AsSpan(offset, count));
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArgs(buffer, offset, count);
            return WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        private async ValueTask ConnectAsync(CancellationToken cancellationToken = default)
        {
            Debug.Assert(_mock);
            Debug.Assert(_connection != null, "Stream not connected but no connection???");

            _socket = await _connection.CreateOutboundMockStreamAsync(_streamId).ConfigureAwait(false);

            // Don't need to hold on to the connection any longer.
            _connection = null;
        }

        /// <summary>
        /// QUIC stream ID.
        /// </summary>
        public long StreamId
        {
            get
            {
                CheckDisposed();
                return _streamId;
            }
        }

        public override bool CanRead => _canRead;

        public override int Read(Span<byte> buffer)
        {
            CheckDisposed();

            if (!_canRead)
            {
                throw new NotSupportedException();
            }

            if (_mock)
            {
                return _socket.Receive(buffer);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            if (!_canRead)
            {
                throw new NotSupportedException();
            }

            if (_mock)
            {
                if (_socket == null)
                {
                    await ConnectAsync(cancellationToken).ConfigureAwait(false);
                }

                return await _socket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override bool CanWrite => _canWrite;

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            CheckDisposed();

            if (!_canWrite)
            {
                throw new NotSupportedException();
            }

            if (_mock)
            {
                _socket.Send(buffer);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            if (!_canWrite)
            {
                throw new NotSupportedException();
            }

            if (_mock)
            {
                if (_socket == null)
                {
                    await ConnectAsync(cancellationToken).ConfigureAwait(false);
                }

                await _socket.SendAsync(buffer, SocketFlags.None, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override void Flush()
        {
            CheckDisposed();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            CheckDisposed();

            return Task.CompletedTask;
        }

        public void ShutdownRead()
        {
            throw new NotImplementedException();
        }

        public void ShutdownWrite()
        {
            CheckDisposed();

            if (_mock)
            {
                _socket.Shutdown(SocketShutdown.Send);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(QuicStream));
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;

                if (_mock)
                {
                    _socket?.Dispose();
                    _socket = null;
                }
            }

            base.Dispose(disposing);
        }
    }
}
