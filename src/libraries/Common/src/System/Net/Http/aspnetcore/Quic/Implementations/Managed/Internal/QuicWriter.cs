using System.Buffers.Binary;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal class QuicWriter
    {
        // underlying buffer to which data are being written.
        private byte[] _buffer;
        // number of bytes already written into the buffer.
        private int _written;

        public QuicWriter(byte[] buffer)
        {
            _buffer = buffer;
        }

        internal int BytesWritten => _written;

        internal int BytesAvailable => _buffer.Length - BytesWritten;

        internal void Reset(byte[] buffer)
        {
            _buffer = buffer;
            _written = 0;
        }

        internal void WriteFrameType(FrameType type)
        {
            WriteVarInt((ulong) type);
        }

        private static int GetVarIntLogLength(ulong value)
        {
            if (value <= 63) return 0;
            if (value <= 16_383) return 1;
            if (value <= 1_073_741_823) return 2;
            if (value <= 4_611_686_018_427_387_903) return 3;

            throw new ArgumentOutOfRangeException(nameof(value));
        }

        internal void WriteUInt8(byte value)
        {
            CheckSizeAvailable(sizeof(byte));
            _buffer[_written] = value;
            Advance(sizeof(byte));
        }

        internal void WriteUInt16(ushort value)
        {
            BinaryPrimitives.WriteUInt16BigEndian(GetSpan(sizeof(ushort)), value);
            Advance(sizeof(ushort));
        }

        internal void WriteUInt24(uint value)
        {
            // TODO-RZ: implement this platform endianness aware way
            throw new NotImplementedException("24bit int not implemented");
        }

        internal void WriteUInt32(uint value)
        {
            BinaryPrimitives.WriteUInt32BigEndian(GetSpan(sizeof(uint)), value);
            Advance(sizeof(uint));
        }

        internal void WriteUInt64(ulong value)
        {
            BinaryPrimitives.WriteUInt64BigEndian(GetSpan(sizeof(ulong)), value);
            Advance(sizeof(ulong));
        }

        internal void WriteVarInt(ulong value)
        {
            int log = GetVarIntLogLength(value);
            int bytes = 1 << log;

            // prefix with log length
            value |= (ulong) log << (bytes * 8 - 2);

            switch (bytes)
            {
                case 1:
                    WriteUInt8((byte) value);
                    break;
                case 2:
                    WriteUInt16((ushort) value);
                    break;
                case 4:
                    WriteUInt32((uint) value);
                    break;
                case 8:
                    WriteUInt64(value);
                    break;
                default:
                    throw new InvalidOperationException("Unreachable");
            }
        }

        internal void WriteSpan(ReadOnlySpan<byte> data)
        {
            data.CopyTo(GetSpan(data.Length));
            Advance(data.Length);
        }

        private void Advance(int bytes)
        {
            _written += bytes;
        }

        private Span<byte> GetSpan(int length)
        {
            CheckSizeAvailable(length);
            return _buffer.AsSpan(BytesWritten, length);
        }

        private void CheckSizeAvailable(int size)
        {
            if (BytesAvailable < size) throw new ArgumentException("Buffer too short");
        }
    }
}
