using System.Buffers.Binary;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal class QuicReader
    {
        // Underlying buffer from which data are read.
        private byte[] _buffer;

        // number of bytes read from the buffer.
        private int _consumed;

        internal QuicReader(byte[] buffer)
        {
            _buffer = buffer;
        }

        public int BytesRead => _consumed;

        public int BytesLeft => _buffer.Length - _consumed;

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
            if (BytesLeft == 0)
            {
                result = 0;
                return false;
            }

            // first two bits give logarithm of size
            int logBytes = _buffer[_consumed] >> 6;
            int bytes = 1 << logBytes;

            // mask the log length prefix (uppermost 2 bits)
            bool success;
            switch (bytes)
            {
                case 1:
                {
                    success = TryReadUInt8(out byte res);
                    result = (ulong) (res & 0x3f);
                    break;
                }
                case 2:
                {
                    success = TryReadUInt16(out ushort res);
                    result = (ulong) (res & 0x3fff);
                    break;
                }
                case 4:
                {
                    success = TryReadUInt32(out uint res);
                    result = (ulong) (res & 0x3fff_ffff);
                    break;
                }
                case 8:
                {
                    success = TryReadUInt64(out ulong res);
                    result = res & 0x3fff_ffff_ffff_ffff;
                    break;
                }
                default:
                    throw new InvalidOperationException("Unreachable");
            }

            return success;
        }

        internal ulong ReadVarInt()
        {
            if (!TryReadVarInt(out ulong result)) throw new InvalidOperationException("Buffer too short");
            return result;
        }

        private void Advance(int bytes)
        {
            _consumed += bytes;
        }

        internal void Reset(byte[] buffer)
        {
            _buffer = buffer;
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
