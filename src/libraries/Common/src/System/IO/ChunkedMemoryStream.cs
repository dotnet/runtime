// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace System.IO
{
    /// <summary>Provides an in-memory stream composed of non-contiguous chunks.</summary>
    internal sealed class ChunkedMemoryStream : Stream
    {
        private MemoryChunk? _headChunk;
        private MemoryChunk? _currentChunk;

        private const int InitialChunkDefaultSize = 1024;
        private const int MaxChunkSize = 1024 * InitialChunkDefaultSize;
        private int _totalLength;

        internal ChunkedMemoryStream() { }

        public byte[] ToArray()
        {
            byte[] result = new byte[_totalLength];
            int offset = 0;
            for (MemoryChunk? chunk = _headChunk; chunk != null; chunk = chunk._next)
            {
                Debug.Assert(chunk._next == null || chunk._freeOffset == chunk._buffer.Length);
                Buffer.BlockCopy(chunk._buffer, 0, result, offset, chunk._freeOffset);
                offset += chunk._freeOffset;
            }
            return result;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Write(new ReadOnlySpan<byte>(buffer, offset, count));
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            while (!buffer.IsEmpty)
            {
                if (_currentChunk != null)
                {
                    int remaining = _currentChunk._buffer.Length - _currentChunk._freeOffset;
                    if (remaining > 0)
                    {
                        int toCopy = Math.Min(remaining, buffer.Length);
                        buffer.Slice(0, toCopy).CopyTo(new Span<byte>(_currentChunk._buffer, _currentChunk._freeOffset, toCopy));
                        buffer = buffer.Slice(toCopy);
                        _totalLength += toCopy;
                        _currentChunk._freeOffset += toCopy;
                        continue;
                    }
                }

                AppendChunk(buffer.Length);
            }
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            Write(buffer, offset, count);
            return Task.CompletedTask;
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled(cancellationToken);
            }

            Write(buffer.Span);
            return ValueTask.CompletedTask;
        }

        private void AppendChunk(long count)
        {
            int nextChunkLength = _currentChunk != null ? _currentChunk._buffer.Length * 2 : InitialChunkDefaultSize;
            if (count > nextChunkLength)
            {
                nextChunkLength = (int)Math.Min(count, MaxChunkSize);
            }

            MemoryChunk newChunk = new MemoryChunk(nextChunkLength);

            if (_currentChunk == null)
            {
                Debug.Assert(_headChunk == null);
                _headChunk = _currentChunk = newChunk;
            }
            else
            {
                Debug.Assert(_headChunk != null);
                _currentChunk._next = newChunk;
                _currentChunk = newChunk;
            }
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _totalLength;
        public override void Flush() { }
        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
        public override int Read(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        public override void SetLength(long value)
        {
            if (_currentChunk != null) throw new NotSupportedException();
            AppendChunk(value);
        }

        private sealed class MemoryChunk
        {
            internal readonly byte[] _buffer;
            internal int _freeOffset;
            internal MemoryChunk? _next;

            internal MemoryChunk(int bufferSize) { _buffer = new byte[bufferSize]; }
        }
    }

}
