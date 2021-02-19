using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets.Tests
{
    /// <summary>
    /// A helper stream class that can be used simulate
    /// sending / receiving (duplex) data in a websocket.
    /// </summary>
    public class WebSocketStream : Stream
    {
        private readonly SemaphoreSlim _inputLock = new(initialCount: 0);
        private readonly Queue<Block> _inputQueue = new();
        private readonly CancellationTokenSource _disposed = new();

        public WebSocketStream()
        {
            GC.SuppressFinalize(this);
            Remote = new WebSocketStream(this);
        }

        private WebSocketStream(WebSocketStream remote)
        {
            GC.SuppressFinalize(this);
            Remote = remote;
        }

        public WebSocketStream Remote { get; }

        /// <summary>
        /// Returns the number of unread bytes.
        /// </summary>
        public int Available
        {
            get
            {
                var available = 0;

                lock (_inputQueue)
                {
                    foreach (var x in _inputQueue)
                    {
                        available += x.AvailableLength;
                    }
                }

                return available;
            }
        }

        public Span<byte> NextAvailableBytes
        {
            get
            {
                lock (_inputQueue)
                {
                    var block = _inputQueue.Peek();

                    if (block is null)
                        return default;

                    return block.Available;
                }
            }
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => -1;

        public override long Position { get => -1; set => throw new NotSupportedException(); }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed.IsCancellationRequested)
            {
                _disposed.Cancel();

                lock (Remote._inputQueue)
                {
                    Remote._inputLock.Release();
                    Remote._inputQueue.Enqueue(Block.ConnectionClosed);
                }
            }
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            using (var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposed.Token))
            {
                try
                {
                    await _inputLock.WaitAsync(cancellation.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (_disposed.IsCancellationRequested)
                {
                    return 0;
                }
            }

            lock (_inputQueue)
            {
                var block = _inputQueue.Peek();
                if (block == Block.ConnectionClosed)
                    return 0;

                var count = Math.Min(block.AvailableLength, buffer.Length);

                block.Available.Slice(0, count).CopyTo(buffer.Span);
                block.Advance(count);

                if (block.AvailableLength == 0)
                {
                    _inputQueue.Dequeue();
                }
                else
                {
                    // Because we haven't fully consumed the buffer
                    // we should release once the input lock so we can acquire
                    // it again on consequent receive.
                    _inputLock.Release();
                }

                return count;
            }
        }

        /// <summary>
        /// Receives the data and enqueues it for processing.
        /// </summary>
        public void Enqueue(params byte[] data)
        {
            lock (_inputQueue)
            {
                _inputLock.Release();
                _inputQueue.Enqueue(new Block(data));
            }
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            lock (Remote._inputQueue)
            {
                Remote._inputLock.Release();
                Remote._inputQueue.Enqueue(new Block(buffer.ToArray()));
            }
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Write(buffer.Span);
            return ValueTask.CompletedTask;
        }

        public override void Flush() => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        private sealed class Block
        {
            public static readonly Block ConnectionClosed = new(Array.Empty<byte>());

            private readonly byte[] _data;
            private int _position;

            public Block(byte[] data)
            {
                _data = data;
            }

            public Span<byte> Available => _data.AsSpan(_position);

            public int AvailableLength => _data.Length - _position;

            public void Advance(int count) => _position += count;
        }
    }
}
