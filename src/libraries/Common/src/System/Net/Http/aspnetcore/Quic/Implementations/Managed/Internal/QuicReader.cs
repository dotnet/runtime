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
        private byte[] _buffer;

        // Number of bytes from _buffer to use.
        private int _maxOffset;

        // number of bytes read from the buffer.
        private int _consumed;

        internal QuicReader(byte[] buffer)
            : this (buffer, buffer.Length)
        {
        }

        internal QuicReader(byte[] buffer, int count)
        {
            _buffer = buffer; //to get rid of the warning.
            Reset(buffer, count);
        }

        public int BytesRead => _consumed;

        public int BytesLeft => _maxOffset - _consumed;

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

        internal ulong ReadVarInt()
        {
            if (!TryReadVarInt(out ulong result)) throw new InvalidOperationException("Buffer too short");
            return result;
        }

        internal void Advance(int bytes)
        {
            _consumed += bytes;
        }

        internal void Reset(byte[] buffer, int count)
        {
            Debug.Assert(count >= buffer.Length);

            _buffer = buffer;
            _maxOffset = count;
            _consumed = 0;
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

        private void CheckSizeAvailable(int size)
        {
            if (BytesLeft < size) throw new InvalidOperationException("Buffer too small");
        }
    }
}
