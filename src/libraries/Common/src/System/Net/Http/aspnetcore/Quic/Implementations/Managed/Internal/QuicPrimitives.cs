using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal enum PacketSpace
    {
        Initial,
        Handshake,
        Application
    }

    internal static class QuicPrimitives
    {
        internal const long MaxVarintValue = (1L << 62) - 1;

        private static bool TryWriteVarInt(Span<byte> destination, long value, int length)
        {
            Debug.Assert(GetVarIntLength(value) <= (uint) length && length > 0);

            int log = BitOperations.Log2((uint) length);

            value |= (long)log << (length * 8 - 2);

            if (destination.Length < length)
            {
                return false;
            }

            switch (length)
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
                    BinaryPrimitives.WriteInt64BigEndian(destination, value);
                    break;
                default:
                    throw new InvalidOperationException("Unreachable");
            }

            return true;
        }

        internal static void WriteVarInt(Span<byte> destination, long value, int length)
        {
            if (!TryWriteVarInt(destination, value, length))
            {
                throw new InvalidOperationException("Buffer too small");
            }
        }

        internal static int WriteVarInt(Span<byte> destination, long value)
        {
            int length = GetVarIntLength(value);

            if (TryWriteVarInt(destination, value, length))
            {
                return length;
            }

            return 0;
        }

        internal static int ReadVarInt(ReadOnlySpan<byte> source, out long result)
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
                    result = source[0] & 0x3f;
                    break;
                }
                case 2:
                {
                    success = BinaryPrimitives.TryReadUInt16BigEndian(source, out ushort res);
                    result = res & 0x3fff;
                    break;
                }
                case 4:
                {
                    success = BinaryPrimitives.TryReadUInt32BigEndian(source, out uint res);
                    result = res & 0x3fff_ffff;
                    break;
                }
                case 8:
                {
                    success = BinaryPrimitives.TryReadUInt64BigEndian(source, out ulong res);
                    result = (long) res & 0x3fff_ffff_ffff_ffff;
                    break;
                }
                default:
                    throw new InvalidOperationException("Unreachable");
            }

            return success ? bytes : 0;
        }

        internal static int GetVarIntLengthLogarithm(long value)
        {
            if (value < 1L <<  6) return 0;
            if (value < 1L << 14) return 1;
            if (value < 1L << 30) return 2;
            if (value < 1L << 62) return 3;

            throw new ArgumentOutOfRangeException(nameof(value));
        }

        internal static int GetVarIntLength(long value)
        {
            return 1 << GetVarIntLengthLogarithm(value);
        }

        private static int GetMinimumEncodingLength(long value)
        {
            if (value < 1L << 8) return 1;
            if (value < 1L << 16) return 2;
            if (value < 1L << 24) return 3;
            if (value < 1L << 32) return 4;

            throw new ArgumentOutOfRangeException("Invalid packet number");
        }

        /// <summary>
        /// Returns number of least significant bytes of the packet number needed to be sent in order for peer to be able to correctly decode the packet number.
        /// </summary>
        /// <param name="largestAckedPn">Largest packet number acknowledged by the peer.</param>
        /// <param name="currentPn">Packet number to be encoded.</param>
        /// <returns></returns>
        internal static int GetPacketNumberByteCount(long largestAckedPn, long currentPn)
        {
            // The sender MUST use a packet number size able to represent more than
            // twice as large a range than the difference between the largest
            // acknowledged packet and packet number being sent.

            var range = 2 * (currentPn - largestAckedPn);
            return GetMinimumEncodingLength(range);
        }

        /// <summary>
        /// Decodes packet number using the algorithm defined in Appendix A of QUIC-TRANSPORT RFC.
        /// </summary>
        /// <param name="largestAckedPn">Largest packet number acknowledged by the peer.</param>
        /// <param name="truncatedPn">Packet number to be decoded.</param>
        /// <param name="pnLength">Length of the <paramref name="truncatedPn"/> in bytes.</param>
        /// <returns></returns>
        internal static long DecodePacketNumber(long largestAckedPn, long truncatedPn, int pnLength)
        {
            int pnNbits = 8 * pnLength;

            // following code has been copied and adapted from the RFC.
            long expectedPn = largestAckedPn + 1;
            long pnWin = 1L << pnNbits;
            long pnHwin = pnWin / 2;
            long pnMask = pnWin - 1;

            // The incoming packet number should be greater than
            // expectedPn - pnHwin and less than or equal to
            // expectedPn + pnHwin
            //
            // This means we can't just strip the trailing bits from
            // expectedPn and add the truncatedPn because that might
            // yield a value outside the window.
            //
            // The following code calculates a candidate value and
            // makes sure it's within the packet number window.
            // Note the extra checks to prevent overflow and underflow.

            long candidatePn = (expectedPn & ~pnMask) | truncatedPn;

            if (candidatePn + pnHwin <= expectedPn &&
                candidatePn < (1L << 62) - pnWin)
                return candidatePn + pnWin;

            if (candidatePn > expectedPn + pnHwin &&
                candidatePn >= pnWin)
                return candidatePn - pnWin;

            return candidatePn;
        }
    }
}
