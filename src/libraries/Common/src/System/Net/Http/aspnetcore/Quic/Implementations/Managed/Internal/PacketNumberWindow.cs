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
        /// <exception cref="InvalidOperationException">If added value is outside representable window.</exception>
        internal void Add(long value)
        {
            if (value < _lower)
                return; // already present

            if (value > _lower + WindowWidth)
            {
                int shift = BitOperations.TrailingZeroCount(~_window);

                // TODO-RZ: what to do?
                if (shift == 0) throw new InvalidOperationException("Window width exceeded.");

                _lower += shift;
                _window <<= shift;
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

            // if overflows, then mask is 0 and gives correct result
            ulong mask = 1ul << (int)(value - _lower);
            return (_window & mask) != 0;
        }
    }
}
