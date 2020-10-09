// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    /// <summary>
    ///     Helper class for reading QUIC primitives from a byte array.
    /// </summary>
    internal class QuicReader
    {
        // Underlying buffer from which data are read.
        private Memory<byte> _buffer;

        // number of bytes read from the buffer.
        private int _consumed;

        internal QuicReader(Memory<byte> buffer)
        {
            _buffer = buffer;
        }

        public int BytesRead => _consumed;

        public int BytesLeft => _buffer.Length - _consumed;

        public Memory<byte> Buffer => _buffer;

        internal bool TryReadUInt8(out byte result)
        {
            if (BytesLeft < sizeof(byte))
            {
                result = 0;
                return false;
            }

            result = _buffer.Span[_consumed];
            Advance(sizeof(byte));
            return true;
        }

        internal byte ReadUInt8()
        {
            CheckSizeAvailable(sizeof(byte));
            byte value = _buffer.Span[_consumed];
            Advance(sizeof(byte));
            return value;
        }

        internal bool TryReadUInt16(out ushort result)
        {
            return BinaryPrimitives.TryReadUInt16BigEndian(ReadSpan(sizeof(short)), out result);
        }

        internal short ReadInt16()
        {
            return BinaryPrimitives.ReadInt16BigEndian(ReadSpan(sizeof(short)));
        }

        internal bool TryReadInt24(out int result)
        {
            if (BytesLeft < 3)
            {
                result = 0;
                return false;
            }

            result = ReadInt24();
            return true;
        }

        internal int ReadInt24()
        {
            var source = ReadSpan(3);
            // Source data is in big endian, and bit shift opreation is endianness-agnostic,
            // so this works on all platforms.
            return (source[0] << 16) |
                   (source[1] << 8) |
                   source[2];
        }

        internal bool TryReadUInt32(out uint result)
        {
            return BinaryPrimitives.TryReadUInt32BigEndian(ReadSpan(sizeof(uint)), out result);
        }

        internal int ReadInt32()
        {
            return BinaryPrimitives.ReadInt32BigEndian(ReadSpan(sizeof(uint)));
        }

        internal bool TryReadUInt64(out long result)
        {
            return BinaryPrimitives.TryReadInt64BigEndian(ReadSpan(sizeof(long)), out result);
        }

        internal long ReadInt64()
        {
            return BinaryPrimitives.ReadInt64BigEndian(ReadSpan(sizeof(long)));
        }

        internal bool TryReadVarInt(out long result)
        {
            int bytes =  QuicPrimitives.TryReadVarInt(PeekRestOfBuffer(), out result);
            Advance(bytes);
            return bytes > 0;
        }

        internal long PeekVarInt()
        {
            QuicPrimitives.TryReadVarInt(PeekRestOfBuffer(), out long result);
            return result;
        }

        internal long ReadVarInt()
        {
            if (!TryReadVarInt(out long result)) throw new InvalidOperationException("Buffer too short");
            return result;
        }

        internal void Advance(int bytes)
        {
            _consumed += bytes;
        }

        internal void Reset(Memory<byte> buffer, int consumed = 0)
        {
            _buffer = buffer;
            Seek(consumed);
        }

        internal void Seek(int pos)
        {
            _consumed = pos;
        }

        internal bool TryReadSpan(int length, out ReadOnlySpan<byte> result)
        {
            if (length < 0 || BytesLeft < length)
            {
                result = default;
                return false;
            }

            result = ReadSpan(length);
            return true;
        }

        internal ReadOnlySpan<byte> ReadSpan(int length)
        {
            var span = PeekSpan(length);
            Advance(length);
            return span;
        }

        private ReadOnlySpan<byte> PeekRestOfBuffer()
        {
            return _buffer.Span.Slice(_consumed);
        }

        internal ReadOnlySpan<byte> PeekSpan(int length)
        {
            CheckSizeAvailable(length);
            return _buffer.Span.Slice(_consumed, length);
        }

        internal byte Peek()
        {
            return _buffer.Span[_consumed];
        }

        [Conditional("DEBUG")]
        private void CheckSizeAvailable(int size)
        {
            Debug.Assert(BytesLeft >= size);
        }
    }
}
