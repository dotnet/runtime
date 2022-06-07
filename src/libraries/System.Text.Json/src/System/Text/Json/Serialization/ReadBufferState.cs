// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization
{
    internal struct ReadBufferState : IDisposable
    {
        private byte[] _buffer;
        private byte _offset; // Read bytes offset typically used when skipping the UTF-8 BOM.
        private int _count; // Number of read bytes yet to be consumed by the serializer.
        private int _maxCount; // Number of bytes we need to clear before returning the buffer.
        private bool _isFirstBlock;
        private bool _isFinalBlock;

        public ReadBufferState(int initialBufferSize)
        {
            _buffer = ArrayPool<byte>.Shared.Rent(Math.Max(initialBufferSize, JsonConstants.Utf8Bom.Length));
            _maxCount = _count = _offset = 0;
            _isFirstBlock = true;
            _isFinalBlock = false;
        }

        public bool IsFinalBlock => _isFinalBlock;

        public ReadOnlySpan<byte> Bytes => _buffer.AsSpan(_offset, _count);

        /// <summary>
        /// Read from the stream until either our buffer is filled or we hit EOF.
        /// Calling ReadCore is relatively expensive, so we minimize the number of times
        /// we need to call it.
        /// </summary>
        public readonly async ValueTask<ReadBufferState> ReadFromStreamAsync(
            Stream utf8Json,
            CancellationToken cancellationToken,
            bool fillBuffer = true)
        {
            // Since mutable structs don't work well with async state machines,
            // make all updates on a copy which is returned once complete.
            ReadBufferState bufferState = this;

            do
            {
                int bytesRead = await utf8Json.ReadAsync(
#if BUILDING_INBOX_LIBRARY
                    bufferState._buffer.AsMemory(bufferState._count),
#else
                    bufferState._buffer, bufferState._count, bufferState._buffer.Length - bufferState._count,
#endif
                    cancellationToken).ConfigureAwait(false);

                if (bytesRead == 0)
                {
                    bufferState._isFinalBlock = true;
                    break;
                }

                bufferState._count += bytesRead;
            }
            while (fillBuffer && bufferState._count < bufferState._buffer.Length);

            bufferState.ProcessReadBytes();
            return bufferState;
        }

        /// <summary>
        /// Read from the stream until either our buffer is filled or we hit EOF.
        /// Calling ReadCore is relatively expensive, so we minimize the number of times
        /// we need to call it.
        /// </summary>
        public void ReadFromStream(Stream utf8Json)
        {
            do
            {
                int bytesRead = utf8Json.Read(
#if BUILDING_INBOX_LIBRARY
                    _buffer.AsSpan(_count));
#else
                    _buffer, _count, _buffer.Length - _count);
#endif

                if (bytesRead == 0)
                {
                    _isFinalBlock = true;
                    break;
                }

                _count += bytesRead;
            }
            while (_count < _buffer.Length);

            ProcessReadBytes();
        }

        /// <summary>
        /// Advances the buffer in anticipation of a subsequent read operation.
        /// </summary>
        public void AdvanceBuffer(int bytesConsumed)
        {
            Debug.Assert(bytesConsumed <= _count);
            Debug.Assert(!_isFinalBlock || _count == bytesConsumed, "The reader should have thrown if we have remaining bytes.");

            _count -= bytesConsumed;

            if (!_isFinalBlock)
            {
                // Check if we need to shift or expand the buffer because there wasn't enough data to complete deserialization.
                if ((uint)_count > ((uint)_buffer.Length / 2))
                {
                    // We have less than half the buffer available, double the buffer size.
                    byte[] oldBuffer = _buffer;
                    int oldMaxCount = _maxCount;
                    byte[] newBuffer = ArrayPool<byte>.Shared.Rent((_buffer.Length < (int.MaxValue / 2)) ? _buffer.Length * 2 : int.MaxValue);

                    // Copy the unprocessed data to the new buffer while shifting the processed bytes.
                    Buffer.BlockCopy(oldBuffer, _offset + bytesConsumed, newBuffer, 0, _count);
                    _buffer = newBuffer;
                    _offset = 0;
                    _maxCount = _count;

                    // Clear and return the old buffer
                    new Span<byte>(oldBuffer, 0, oldMaxCount).Clear();
                    ArrayPool<byte>.Shared.Return(oldBuffer);
                }
                else if (_count != 0)
                {
                    // Shift the processed bytes to the beginning of buffer to make more room.
                    Buffer.BlockCopy(_buffer, _offset + bytesConsumed, _buffer, 0, _count);
                    _offset = 0;
                }
            }
        }

        private void ProcessReadBytes()
        {
            if (_count > _maxCount)
            {
                _maxCount = _count;
            }

            if (_isFirstBlock)
            {
                _isFirstBlock = false;

                // Handle the UTF-8 BOM if present
                Debug.Assert(_buffer.Length >= JsonConstants.Utf8Bom.Length);
                if (_buffer.AsSpan(0, _count).StartsWith(JsonConstants.Utf8Bom))
                {
                    _offset = (byte)JsonConstants.Utf8Bom.Length;
                    _count -= JsonConstants.Utf8Bom.Length;
                }
            }
        }

        public void Dispose()
        {
            // Clear only what we used and return the buffer to the pool
            new Span<byte>(_buffer, 0, _maxCount).Clear();

            byte[] toReturn = _buffer;
            _buffer = null!;

            ArrayPool<byte>.Shared.Return(toReturn);
        }
    }
}
