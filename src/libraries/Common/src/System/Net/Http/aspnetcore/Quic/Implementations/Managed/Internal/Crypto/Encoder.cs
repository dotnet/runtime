namespace System.Net.Quic.Implementations.Managed.Internal.Crypto
{
    // TODO-RZ: get a better name for this class
    internal class Encoder
    {
        /// <summary>
        /// Gets minimum number of bytes needed to encode given value.
        /// </summary>
        /// <param name="value">Value to be represented.</param>
        /// <returns></returns>
        private static int GetBytesLen(ulong value)
        {
            var count = 1;
            while (value > byte.MaxValue)
            {
                count++;
                value >>= 8;
            }

            return count;
        }

        private static int GetVarIntLogLength(ulong value)
        {
            if (value <= 63) return 0;
            if (value <= 16_383) return 1;
            if (value <= 1_073_741_823) return 2;
            if (value <= 4_611_686_018_427_387_903) return 3;

            throw new ArgumentOutOfRangeException(nameof(value));
        }

        private static int ReadVarIntLength(byte firstByte)
        {
            switch (firstByte >> 6)
            {
                case 00: return 1;
                case 01: return 2;
                case 10: return 4;
                case 11: return 8;
                default: // Unreachable
                    throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Encodes a variable length integer, returns number of bytes written.
        /// </summary>
        /// <param name="value">Integer value to be encoded.</param>
        /// <param name="memory">Target memory to be encoded into.</param>
        /// <returns></returns>
        internal static int EncodeVarInt(ulong value, Span<byte> memory)
        {
            var log = GetVarIntLogLength(value);
            var bytes = 1 << log;

            if (memory.Length < bytes) throw new ArgumentException("Buffer too short");

            // prefix with log length
            value |= (ulong) log << (bytes * 8 - 2);

            for (int i = 0; i < bytes; i++)
            {
                memory[bytes - i - 1] = (byte) value;
                value >>= 8;
            }

            return bytes;
        }

        /// <summary>
        /// Decodes a variable length encoding from the given memory, returns number of bytes read.
        /// </summary>
        /// <param name="memory"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static int DecodeVarInt(ReadOnlySpan<byte> memory, out ulong value)
        {
            // first two bits give logarithm of size
            var logBytes = memory[0] >> 6;
            var bytes = 1 << logBytes;

            if (memory.Length < bytes) throw new ArgumentException("Buffer too short");

            ulong v = (ulong) (memory[0] & 0b0011_1111);

            for (int i = 1; i < bytes; i++)
            {
                v = (v << 8) | memory[i];
            }

            value = v;
            return bytes;
        }

        private static int GetPacketNumberLength(ulong packetNumber)
        {
            if (packetNumber < byte.MaxValue) return 1;
            if (packetNumber < ushort.MaxValue) return 2;
            if (packetNumber < uint.MaxValue) return 4;

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
            return GetBytesLen(range);
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

            if (candidatePn <= expectedPn - pnHwin &&
                candidatePn < (1ul << 62) - pnWin)
                return candidatePn + pnWin;

            if (candidatePn > expectedPn + pnHwin &&
                candidatePn >= pnWin)
                return candidatePn - pnWin;

            return candidatePn;
        }
    }
}
