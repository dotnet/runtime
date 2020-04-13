using System.Reflection;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal class Recovery
    {
        /// <summary>
        ///     Maximum reordering in packets before packet threshold loss detection considers a packet lost.
        ///     The RECOMMENDED value is 3.
        /// </summary>
        internal const int PacketReorderingThreshold = 3;

        /// <summary>
        ///     Maximum reordering in time before time threshold loss detection considers a packet lost. Specified
        ///     As RTT multiplier. The RECOMMENDED value is 9/8.
        /// </summary>
        internal const double TimeReorderingThreshold = 9.0 / 8;

        /// <summary>
        ///     Timer granularity. The value is system-dependent, but SHOULD be at least 1ms.
        /// </summary>
        internal static readonly TimeSpan TimerGranularity = TimeSpan.FromMilliseconds(10);

        /// <summary>
        ///     The RTT used before an RTT sample is taken. the RECOMMENDED value is 500ms.
        /// </summary>
        internal static readonly TimeSpan InitialRtt = TimeSpan.FromMilliseconds(500);


        // constants for congestion control
        /// <summary>
        ///     Default limit on the initial amount of data in flight, in bytes. The RECOMMENDED value is the minimum
        ///     of 10 * MaxDatagramSize and max(2 * MaxDatagramSize, 14720).
        /// </summary>
        // TODO-RZ: these should be computed
        internal const int InitialWindowSize = 10 * 1200;

        /// <summary>
        ///     Minimum congestion window in bytes. The RECOMMENDED value is 2 * MaxDatagramSize.
        /// </summary>
        internal const int MinimumWindowSize = 2 * 1200;

        /// <summary>
        ///     Reduction in congestion window when a new loss event is detected. The RECOMMENDED value is 0.5.
        /// </summary>
        internal const double LossReductionFactor = 0.5;

        /// <summary>
        ///     Period of time for persistent congestion to be established, specified as the PTO multiplier.
        /// </summary>
        internal const int PersistentCongestionThreshold = 3;
    }
}
