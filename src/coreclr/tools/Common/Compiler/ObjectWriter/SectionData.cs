// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Text;

namespace ILCompiler.ObjectWriter
{
    /// <summary>
    /// Optimized append-only structure for writing sections.
    /// </summary>
    /// <remarks>
    /// The section data are kept in memory as a sequence of buffers, with the
    /// first buffer stored directly and a list allocated only when needed. It
    /// supports appending existing read-only buffers without copying (such as
    /// buffer from ObjectNode.ObjectData).
    /// </remarks>
    internal sealed class SectionData
    {
        private const int PaddingBufferSize = 16;
        private const byte NopPaddingByte = 0x90;

        private static readonly byte[] s_zeroPadding = new byte[PaddingBufferSize];
        private static readonly byte[] s_nopPadding = CreatePadding(NopPaddingByte);

        private ArrayBufferWriter<byte> _appendBuffer;
        private List<ReadOnlyMemory<byte>> _buffers;
        private ReadOnlyMemory<byte> _firstBuffer;
        private int _bufferCount;
        private long _length;
        private readonly byte[] _padding;

        public SectionData(byte paddingByte = 0)
        {
            _padding = paddingByte switch
            {
                0 => s_zeroPadding,
                NopPaddingByte => s_nopPadding,
                _ => CreatePadding(paddingByte),
            };
        }

        private static byte[] CreatePadding(byte value)
        {
            byte[] result = new byte[PaddingBufferSize];
            result.AsSpan().Fill(value);
            return result;
        }

        private void FlushAppendBuffer()
        {
            if (_appendBuffer is { WrittenCount: > 0 } appendBuffer)
            {
                AddBuffer(appendBuffer.WrittenSpan.ToArray());
                _length += appendBuffer.WrittenCount;
                appendBuffer.Clear();
            }
        }

        public void AppendData(ReadOnlyMemory<byte> data)
        {
            FlushAppendBuffer();
            AddBuffer(data);
            _length += data.Length;
        }

        public void AppendPadding(int paddingLength)
        {
            if (paddingLength > 0)
            {
                if ((_appendBuffer?.WrittenCount ?? 0) > 0 || paddingLength > _padding.Length)
                {
                    ArrayBufferWriter<byte> appendBuffer = GetAppendBuffer();
                    appendBuffer.GetSpan(paddingLength).Slice(0, paddingLength).Fill(_padding[0]);
                    appendBuffer.Advance(paddingLength);
                }
                else
                {
                    AppendData(_padding.AsMemory(0, paddingLength));
                }
            }
        }

        public IBufferWriter<byte> BufferWriter => GetAppendBuffer();

        public long Length => _length + (_appendBuffer?.WrittenCount ?? 0);

        /// <summary>
        /// Gets a read-only stream accessing the section data.
        /// </summary>
        public Stream GetReadStream() => new ReadStream(this);

        private ArrayBufferWriter<byte> GetAppendBuffer()
        {
            return _appendBuffer ??= new ArrayBufferWriter<byte>();
        }

        private void AddBuffer(ReadOnlyMemory<byte> data)
        {
            if (_bufferCount == 0)
            {
                _firstBuffer = data;
            }
            else
            {
                if (_buffers is null)
                {
                    _buffers = new List<ReadOnlyMemory<byte>>();
                    _buffers.Add(_firstBuffer);
                }

                _buffers.Add(data);
            }

            _bufferCount++;
        }

        private int BufferCount => _bufferCount;

        private ReadOnlyMemory<byte> GetBuffer(int index)
        {
            Debug.Assert((uint)index < (uint)_bufferCount);
            return _buffers is null ? _firstBuffer : _buffers[index];
        }

        private sealed class ReadStream : Stream
        {
            private readonly SectionData _sectionData;
            private int _bufferIndex;
            private int _bufferPosition;
            private long _position;

            public override bool CanRead => true;
            public override bool CanSeek => true;
            public override bool CanWrite => false;
            public override long Length => _sectionData.Length;

            public override long Position
            {
                get => _position;
                set
                {
                    // Flush any non-appended data
                    _sectionData.FlushAppendBuffer();

                    // Seek to the correct buffer
                    _position = 0;
                    _bufferIndex = 0;
                    _bufferPosition = 0;
                    while (_position < value && _bufferIndex < _sectionData.BufferCount)
                    {
                        ReadOnlyMemory<byte> currentBuffer = _sectionData.GetBuffer(_bufferIndex);
                        if (currentBuffer.Length < value - _position)
                        {
                            _position += currentBuffer.Length;
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

            public ReadStream(SectionData sectionData)
            {
                _sectionData = sectionData;
            }

            public override void Flush() => throw new NotSupportedException();

            public override int Read(byte[] buffer, int offset, int count)
            {
                return Read(buffer.AsSpan(offset, count));
            }

            public override int Read(Span<byte> buffer)
            {
                int bytesRead = 0;

                // Flush any non-appended data
                _sectionData.FlushAppendBuffer();

                // _bufferIndex and _bufferPosition is only valid after seeking when
                // _position < _length
                while (_position < _sectionData._length && _bufferIndex < _sectionData.BufferCount)
                {
                    ReadOnlySpan<byte> currentBuffer = _sectionData.GetBuffer(_bufferIndex).Span.Slice(_bufferPosition);

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

            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override void Write(ReadOnlySpan<byte> buffer) => throw new NotSupportedException();
        }
    }
}
