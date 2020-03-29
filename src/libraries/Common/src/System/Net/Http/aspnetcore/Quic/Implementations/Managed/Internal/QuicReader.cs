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
        private ArraySegment<byte> _buffer;

        // number of bytes read from the buffer.
        private int _consumed;

        internal QuicReader(ArraySegment<byte> buffer)
        {
            _buffer = buffer;
        }

        public int BytesRead => _consumed;

        public int BytesLeft => _buffer.Count - _consumed;

        public ArraySegment<byte> Buffer => _buffer;

        internal bool TryReadUInt8(out byte result)
        {
            if (BytesLeft < sizeof(byte))
            {
                result = 0;
                return false;
            }

            result = _buffer[_consumed];
            Advance(sizeof(byte));
            return true;
        }

        internal byte ReadUInt8()
        {
            CheckSizeAvailable(sizeof(byte));
            byte value = _buffer[_consumed];
            Advance(sizeof(byte));
            return value;
        }

        internal bool TryReadUInt16(out ushort result)
        {
            return BinaryPrimitives.TryReadUInt16BigEndian(ReadSpan(sizeof(ushort)), out result);
        }

        internal ushort ReadUInt16()
        {
            return BinaryPrimitives.ReadUInt16BigEndian(ReadSpan(sizeof(ushort)));
        }

        internal bool TryReadUInt24(out uint result)
        {
            // TODO-RZ: implement this platform endianness aware way
            throw new NotImplementedException("24bit int not implemented");
        }

        internal uint ReadUInt24()
        {
            // TODO-RZ: implement this platform endianness aware way
            throw new NotImplementedException("24bit int not implemented");
        }

        internal bool TryReadUInt32(out uint result)
        {
            return BinaryPrimitives.TryReadUInt32BigEndian(ReadSpan(sizeof(uint)), out result);
        }

        internal uint ReadUInt32()
        {
            return BinaryPrimitives.ReadUInt32BigEndian(ReadSpan(sizeof(uint)));
        }

        internal bool TryReadUInt64(out ulong result)
        {
            return BinaryPrimitives.TryReadUInt64BigEndian(ReadSpan(sizeof(ulong)), out result);
        }

        internal ulong ReadUInt64()
        {
            return BinaryPrimitives.ReadUInt64BigEndian(ReadSpan(sizeof(ulong)));
        }

        internal bool TryReadVarInt(out ulong result)
        {
            int bytes =  QuicPrimitives.ReadVarInt(PeekSpan(BytesLeft), out result);
            Advance(bytes);
            return bytes > 0;
        }

        internal ulong PeekVarInt()
        {
            QuicPrimitives.ReadVarInt(PeekSpan(BytesLeft), out ulong result);
            return result;
        }

        internal ulong ReadVarInt()
        {
            if (!TryReadVarInt(out ulong result)) throw new InvalidOperationException("Buffer too short");
            return result;
        }

        internal void Advance(int bytes)
        {
            _consumed += bytes;
        }

        internal void Reset(ArraySegment<byte> buffer, int consumed = 0)
        {
            _buffer = buffer;
            _consumed = consumed;
        }

        internal void Reset(byte[] buffer, int start, int count)
        {
            Reset(new ArraySegment<byte>(buffer, start, count));
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

        internal ReadOnlySpan<byte> PeekSpan(int length)
        {
            CheckSizeAvailable(length);
            return _buffer.AsSpan(_consumed, length);
        }

        internal byte PeekUInt8()
        {
            return _buffer[_consumed];
        }

        private void CheckSizeAvailable(int size)
        {
            if (BytesLeft < size) throw new InvalidOperationException("Buffer too small");
        }
    }
}
