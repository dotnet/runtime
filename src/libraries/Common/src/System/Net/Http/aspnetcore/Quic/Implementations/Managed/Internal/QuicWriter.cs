using System.Buffers.Binary;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    /// <summary>
    ///     Helper class for writing QUIC primitives to a byte buffer.
    /// </summary>
    internal class QuicWriter
    {
        // underlying buffer to which data are being written.
        private ArraySegment<byte> _buffer;
        // number of bytes already written into the buffer.
        private int _written;

        public QuicWriter(ArraySegment<byte> buffer)
        {
            _buffer = buffer;
        }

        internal int BytesWritten => _written;

        internal int BytesAvailable => _buffer.Count - BytesWritten;

        internal ArraySegment<byte> Buffer => _buffer;

        internal void Reset(ArraySegment<byte> buffer, int offset = 0)
        {
            _buffer = buffer;
            _written = offset;
        }

        internal void Reset(byte[] buffer)
        {
            Reset(new ArraySegment<byte>(buffer, 0, buffer.Length));
        }

        internal void WriteUInt8(byte value)
        {
            CheckSizeAvailable(sizeof(byte));
            _buffer[_written] = value;
            Advance(sizeof(byte));
        }

        internal void WriteUInt16(ushort value)
        {
            BinaryPrimitives.WriteUInt16BigEndian(GetWritableSpan(sizeof(ushort)), value);
        }

        internal void WriteUInt24(uint value)
        {
            // TODO-RZ: implement this platform endianness aware way
            throw new NotImplementedException("24bit int not implemented");
        }

        internal void WriteUInt32(uint value)
        {
            BinaryPrimitives.WriteUInt32BigEndian(GetWritableSpan(sizeof(uint)), value);
        }

        internal void WriteUInt64(ulong value)
        {
            BinaryPrimitives.WriteUInt64BigEndian(GetWritableSpan(sizeof(ulong)), value);
        }

        internal void WriteVarInt(ulong value)
        {
            QuicPrimitives.WriteVarInt(GetWritableSpan(1 << QuicPrimitives.GetVarIntLengthLogarithm(value)), value);
        }

        internal void WriteSpan(ReadOnlySpan<byte> data)
        {
            data.CopyTo(GetWritableSpan(data.Length));
        }

        internal Span<byte> GetWritableSpan(int length)
        {
            var span = GetSpan(length);
            Advance(length);
            return span;
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
