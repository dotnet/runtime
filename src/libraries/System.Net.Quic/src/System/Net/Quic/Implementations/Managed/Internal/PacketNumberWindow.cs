using System.Numerics;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    /// <summary>
    ///     Struct for efficient representation of received packet numbers in a window of 64.
    /// </summary>
    internal struct PacketNumberWindow
    {
        /// <summary>
        ///     Packet number of the least significant bit in the window. All lesser numbers are assumed to be present.
        /// </summary>
        private long _lower;

        /// <summary>
        ///     Bit mask of present packets.
        /// </summary>
        private ulong _window;

        private const int WindowWidth = 8 * sizeof(ulong);

        /// <summary>
        ///     Adds number to the window.
        /// </summary>
        /// <param name="value">Value to be added.</param>
        internal void Add(long value)
        {
            if (value < _lower)
                return; // already present

            if (value >= _lower + WindowWidth)
            {
                int shift = BitOperations.TrailingZeroCount(~_window);

                if (shift == 0)
                {
                    // this can happen only if a packet has been lost and a packet with a number larger than 64 has been
                    // received. We will assume that the packets are truly lost and thus possible false positives
                    // Contains() tests caused by moving the window are unlikely to happen.
                    //
                    // Either way, discarding the packet due to said false positive would prompt the peer to resend the data in another packet.

                    // shift so that the newly set bit is the most significant bit of the window
                    shift = (int) (value - _lower - WindowWidth + 1);
                }

                // bit shift does nothing if arg is 64 or greater
                if (shift >= 64)
                {
                    _window = 0;
                }
                else
                {
                    _window >>= shift;
                }

                _lower += shift;
            }

            _window |= (1ul << (int) (value - _lower));
        }

        /// <summary>
        ///     Checks if given value is present.
        /// </summary>
        /// <param name="value">Value to be checked.</param>
        internal bool Contains(long value)
        {
            if (value < _lower)
                return true;

            if (value >= _lower + WindowWidth)
                return false;

            // if overflows, then mask is 0 and gives correct result
            ulong mask = 1ul << (int)(value - _lower);
            return (_window & mask) != 0;
        }
    }
}
