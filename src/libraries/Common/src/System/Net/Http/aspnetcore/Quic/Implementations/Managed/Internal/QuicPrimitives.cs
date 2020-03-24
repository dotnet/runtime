using System.Buffers.Binary;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal static class QuicPrimitives
    {
        internal static int WriteVarInt(Span<byte> destination, ulong value)
        {
            int log = GetVarIntLogLength(value);
            int bytes = 1 << log;

            // prefix with log length
            value |= (ulong) log << (bytes * 8 - 2);

            switch (bytes)
            {
                case 1:
                    destination[0] = (byte)value;
                    break;
                case 2:
                    BinaryPrimitives.WriteUInt16BigEndian(destination, (ushort) value);
                    break;
                case 4:
                    BinaryPrimitives.WriteUInt32BigEndian(destination, (uint) value);
                    break;
                case 8:
                    BinaryPrimitives.WriteUInt64BigEndian(destination, value);
                    break;
                default:
                    throw new InvalidOperationException("Unreachable");
            }

            return bytes;
        }

        internal static int ReadVarInt(ReadOnlySpan<byte> source, out ulong result)
        {
            if (source.Length == 0)
            {
                result = 0;
                return 0;
            }

            // first two bits give logarithm of size
            int logBytes = source[0] >> 6;
            int bytes = 1 << logBytes;

            // mask the log length prefix (uppermost 2 bits)
            bool success;
            switch (bytes)
            {
                case 1:
                {
                    success = true;
                    result = (ulong) (source[0] & 0x3f);
                    break;
                }
                case 2:
                {
                    success = BinaryPrimitives.TryReadUInt16BigEndian(source, out ushort res);
                    result = (ulong) (res & 0x3fff);
                    break;
                }
                case 4:
                {
                    success = BinaryPrimitives.TryReadUInt32BigEndian(source, out uint res);
                    result = (ulong) (res & 0x3fff_ffff);
                    break;
                }
                case 8:
                {
                    success = BinaryPrimitives.TryReadUInt64BigEndian(source, out ulong res);
                    result = res & 0x3fff_ffff_ffff_ffff;
                    break;
                }
                default:
                    throw new InvalidOperationException("Unreachable");
            }

            return success ? bytes : 0;
        }

        internal static int GetVarIntLogLength(ulong value)
        {
            if (value <= 63) return 0;
            if (value <= 16_383) return 1;
            if (value <= 1_073_741_823) return 2;
            if (value <= 4_611_686_018_427_387_903) return 3;

            throw new ArgumentOutOfRangeException(nameof(value));
        }
    }
}
