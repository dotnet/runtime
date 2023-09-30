// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace ILCompiler.ObjectWriter
{
    /// <summary>
    /// Optimized append-only stream for writing sections.
    /// </summary>
    /// <remarks>
    /// Implements a stream of chained buffers. It supports appending existing
    /// read-only buffers without copying.
    /// </remarks>
    public sealed class ObjectWriterStream : Stream
    {
        private ArrayBufferWriter<byte> _appendBuffer = new();
        private List<ReadOnlyMemory<byte>> _buffers = new();
        private long _length;
        private int _bufferIndex;
        private int _bufferPosition;
        private long _position;
        private byte[] _padding = new byte[16];

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => _length + _appendBuffer.WrittenCount;

        public override long Position
        {
            get => _position;
            set
            {
                // Flush any non-appended data
                FlushAppendBuffer();

                // Seek to the correct buffer
                _position = 0;
                _bufferIndex = 0;
                _bufferPosition = 0;
                while (_position < value && _bufferIndex < _buffers.Count)
                {
                    if (_buffers[_bufferIndex].Length < value - _position)
                    {
                        _bufferIndex++;
                        _position += _buffers[_bufferIndex].Length;
                    }
                    else
                    {
                        _bufferPosition = (int)(value - _position);
                        _position = value;
                        break;
                    }
                }
            }
        }

        public ObjectWriterStream(byte paddingByte)
        {
            _padding.AsSpan().Fill(paddingByte);
        }

        public override void Flush()
        {
            FlushAppendBuffer();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Read(buffer.AsSpan(offset, count));
        }

        public override int Read(Span<byte> buffer)
        {
            int bytesRead = 0;

            while (_bufferIndex < _buffers.Count)
            {
                var currentBuffer = _buffers[_bufferIndex].Span.Slice(_bufferPosition);

                if (currentBuffer.Length >= buffer.Length)
                {
                    currentBuffer.Slice(0, buffer.Length).CopyTo(buffer);
                    bytesRead += buffer.Length;
                    _position += buffer.Length;
                    _bufferPosition += buffer.Length;
                    return bytesRead;
                }

                currentBuffer.CopyTo(buffer);
                buffer = buffer.Slice(currentBuffer.Length);
                bytesRead += currentBuffer.Length;
                _position += currentBuffer.Length;
                _bufferIndex++;
                _bufferPosition = 0;
            }

            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            Position = origin switch
            {
                SeekOrigin.End => Length + offset,
                SeekOrigin.Current => _position + offset,
                SeekOrigin.Begin => offset,
                _ => throw new ArgumentOutOfRangeException(nameof(origin))
            };
            return _position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Write(buffer.AsSpan(offset, count));
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            // We only support appending to the end of the stream
            if (_position != Length)
            {
                throw new NotSupportedException("ObjectWriterStream only supports appending to the end");
            }

            buffer.CopyTo(_appendBuffer.GetSpan(buffer.Length));
            _appendBuffer.Advance(buffer.Length);
            _position += buffer.Length;
            _bufferPosition += buffer.Length;
        }

        public void AppendData(ReadOnlyMemory<byte> data)
        {
            // We only support appending to the end of the stream
            if (_position != Length)
            {
                throw new NotSupportedException("ObjectWriterStream only supports appending to the end");
            }

            FlushAppendBuffer();
            _buffers.Add(data);
            _length += data.Length;
            _position += data.Length;
            _bufferIndex++;
            _bufferPosition = 0;
        }

        public void AppendPadding(int paddingLength)
        {
            if (paddingLength > 0)
            {
                if (_appendBuffer.WrittenCount > 0 || paddingLength > _padding.Length)
                {
                    _appendBuffer.GetSpan(paddingLength).Slice(0, paddingLength).Fill(_padding[0]);
                    _appendBuffer.Advance(paddingLength);
                    _position += paddingLength;
                    _bufferPosition += paddingLength;
                }
                else
                {
                    AppendData(_padding.AsMemory(0, paddingLength));
                }
            }
        }

        private void FlushAppendBuffer()
        {
            if (_appendBuffer.WrittenCount > 0)
            {
                _buffers.Add(_appendBuffer.WrittenSpan.ToArray());
                _length += _appendBuffer.WrittenCount;
                _bufferIndex++;
                _bufferPosition = 0;
                _appendBuffer.Clear();
            }
        }
    }
}
