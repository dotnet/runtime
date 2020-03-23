namespace System.Net.Quic.Implementations.Managed.Internal.Crypto
{
    internal class QuicEncoding
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
