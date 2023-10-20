// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

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
        private readonly ArrayBufferWriter<byte> _appendBuffer = new();
        private readonly List<ReadOnlyMemory<byte>> _buffers = new();
        private long _length;
        private int _bufferIndex;
        private int _bufferPosition;
        private long _position;
        private readonly byte[] _padding = new byte[16];

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => _length + _appendBuffer.WrittenCount;

        public override long Position
        {
            get => _position + _appendBuffer.WrittenCount;
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
                        _position += _buffers[_bufferIndex].Length;
                        _bufferIndex++;
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

        public ObjectWriterStream(byte paddingByte = 0)
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

            // _bufferIndex and _bufferPosition is only valid after seeking when
            // _position < _length
            while (_position < _length && _bufferIndex < _buffers.Count)
            {
                ReadOnlySpan<byte> currentBuffer = _buffers[_bufferIndex].Span.Slice(_bufferPosition);

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
                SeekOrigin.Current => Position + offset,
                SeekOrigin.Begin => offset,
                _ => throw new ArgumentOutOfRangeException(nameof(origin))
            };
            return Position;
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
            Debug.Assert(_position == _length, "ObjectWriterStream only supports appending to the end");
            buffer.CopyTo(_appendBuffer.GetSpan(buffer.Length));
            _appendBuffer.Advance(buffer.Length);
        }

        public void AppendData(ReadOnlyMemory<byte> data)
        {
            Debug.Assert(_position == _length, "ObjectWriterStream only supports appending to the end");
            FlushAppendBuffer();
            _buffers.Add(data);
            _length += data.Length;
            _position += data.Length;
        }

        public void AppendPadding(int paddingLength)
        {
            if (paddingLength > 0)
            {
                if (_appendBuffer.WrittenCount > 0 || paddingLength > _padding.Length)
                {
                    _appendBuffer.GetSpan(paddingLength).Slice(0, paddingLength).Fill(_padding[0]);
                    _appendBuffer.Advance(paddingLength);
                }
                else
                {
                    AppendData(_padding.AsMemory(0, paddingLength));
                }
            }
        }

        public void WriteULEB128(ulong value)
        {
            Debug.Assert(_position == _length, "ObjectWriterStream only supports appending to the end");
            DwarfHelper.WriteULEB128(_appendBuffer, value);
        }

        public void WriteSLEB128(long value)
        {
            Debug.Assert(_position == _length, "ObjectWriterStream only supports appending to the end");
            DwarfHelper.WriteSLEB128(_appendBuffer, value);
        }

        public void WriteUInt8(byte value)
        {
            Debug.Assert(_position == _length, "ObjectWriterStream only supports appending to the end");
            Span<byte> buffer = _appendBuffer.GetSpan(1);
            buffer[0] = value;
            _appendBuffer.Advance(1);
        }

        public void WriteUInt16(ushort value)
        {
            Debug.Assert(_position == _length, "ObjectWriterStream only supports appending to the end");
            Span<byte> buffer = _appendBuffer.GetSpan(sizeof(ushort));
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
            _appendBuffer.Advance(sizeof(ushort));
        }

        public void WriteUInt32(uint value)
        {
            Debug.Assert(_position == _length, "ObjectWriterStream only supports appending to the end");
            Span<byte> buffer = _appendBuffer.GetSpan(sizeof(uint));
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
            _appendBuffer.Advance(sizeof(uint));
        }

        public void WriteUInt64(ulong value)
        {
            Debug.Assert(_position == _length, "ObjectWriterStream only supports appending to the end");
            Span<byte> buffer = _appendBuffer.GetSpan(sizeof(ulong));
            BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
            _appendBuffer.Advance(sizeof(ulong));
        }

        public void WriteUtf8String(string value)
        {
            Debug.Assert(_position == _length, "ObjectWriterStream only supports appending to the end");
            int size = Encoding.UTF8.GetByteCount(value) + 1;
            Span<byte> buffer = _appendBuffer.GetSpan(size);
            Encoding.UTF8.GetBytes(value, buffer);
            buffer[size - 1] = 0;
            _appendBuffer.Advance(size);
        }

        private void FlushAppendBuffer()
        {
            if (_appendBuffer.WrittenCount > 0)
            {
                _buffers.Add(_appendBuffer.WrittenSpan.ToArray());
                _position += _appendBuffer.WrittenCount;
                _length += _appendBuffer.WrittenCount;
                _appendBuffer.Clear();
            }
        }
    }
}
