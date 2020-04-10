using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal enum PacketEpoch
    {
        Initial,
        Handshake,
        Application
    }

    internal static class QuicPrimitives
    {
        internal const ulong MaxVarintValue = (1ul << 62) - 1;

        private static bool TryWriteVarInt(Span<byte> destination, ulong value, int length)
        {
            Debug.Assert(GetVarIntLength(value) <= (uint) length && length > 0);

            int log = BitOperations.Log2((uint) length);

            value |= (ulong)log << (length * 8 - 2);

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
                    BinaryPrimitives.WriteUInt64BigEndian(destination, value);
                    break;
                default:
                    throw new InvalidOperationException("Unreachable");
            }

            return true;
        }

        internal static void WriteVarInt(Span<byte> destination, ulong value, int length)
        {
            if (!TryWriteVarInt(destination, value, length))
            {
                throw new InvalidOperationException("Buffer too small");
            }
        }

        internal static int WriteVarInt(Span<byte> destination, ulong value)
        {
            int length = GetVarIntLength(value);

            if (TryWriteVarInt(destination, value, length))
            {
                return length;
            }

            return 0;
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

        internal static int GetVarIntLengthLogarithm(ulong value)
        {
            if (value < 1ul <<  6) return 0;
            if (value < 1ul << 14) return 1;
            if (value < 1ul << 30) return 2;
            if (value < 1ul << 62) return 3;

            throw new ArgumentOutOfRangeException(nameof(value));
        }

        internal static int GetVarIntLength(ulong value)
        {
            return 1 << GetVarIntLengthLogarithm(value);
        }

        private static int GetMinimumEncodingLength(ulong value)
        {
            if (value < 1ul << 8) return 1;
            if (value < 1ul << 16) return 2;
            if (value < 1ul << 24) return 3;
            if (value < 1ul << 32) return 4;

            throw new ArgumentOutOfRangeException("Invalid packet number");
        }

        /// <summary>
        /// Returns number of least significant bytes of the packet number needed to be sent in order for peer to be able to correctly decode the packet number.
        /// </summary>
        /// <param name="largestAckedPn">Largest packet number acknowledged by the peer.</param>
        /// <param name="currentPn">Packet number to be encoded.</param>
        /// <returns></returns>
        internal static int GetPacketNumberByteCount(ulong largestAckedPn, ulong currentPn)
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
        internal static ulong DecodePacketNumber(ulong largestAckedPn, ulong truncatedPn, int pnLength)
        {
            var pnNbits = 8 * pnLength;

            // following code has been copied and adapted from the RFC.
            var expectedPn = largestAckedPn + 1;
            var pnWin = 1ul << pnNbits;
            var pnHwin = pnWin / 2;
            var pnMask = pnWin - 1;

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

            var candidatePn = (expectedPn & ~pnMask) | truncatedPn;

            if (candidatePn + pnHwin <= expectedPn &&
                candidatePn < (1ul << 62) - pnWin)
                return candidatePn + pnWin;

            if (candidatePn > expectedPn + pnHwin &&
                candidatePn >= pnWin)
                return candidatePn - pnWin;

            return candidatePn;
        }
    }
}
