using System.Collections.Generic;

namespace System.Net.Quic.Implementations.Managed.Internal.Recovery
{
    /// <summary>
    ///     Implementation of the reference congestion controll algorithm for QUIC based on the [RECOVERY] draft.
    /// </summary>
    internal class NewRenoCongestionController : ICongestionController
    {
        internal const int MaxDatagramSize = 1452;

        /// <summary>
        ///     Minimum congestion window in bytes. The RECOMMENDED value is 2 * MaxDatagramSize.
        /// </summary>
        internal const int MinimumWindowSize = 2 * MaxDatagramSize;

        /// <summary>
        ///     Reduction in congestion window when a new loss event is detected. The RECOMMENDED value is 0.5.
        /// </summary>
        internal const double LossReductionFactor = 0.5;

        /// <summary>
        ///     Period of time for persistent congestion to be established, specified as the PTO multiplier.
        /// </summary>
        internal const int PersistentCongestionThreshold = 3;

        /// <summary>
        ///     Default limit on the initial amount of data in flight, in bytes. The RECOMMENDED value is the minimum
        ///     of 10 * MaxDatagramSize and max(2 * MaxDatagramSize, 14720).
        /// </summary>
        internal static readonly int InitialWindowSize =
            Math.Min(10 * MaxDatagramSize, Math.Max(2 * MaxDatagramSize, 14720));

        /// <summary>
        ///     The time when QUIC first detects congestion due to loss of ECN, causing it to enter congestion recovery.
        ///     When a packet sent after this time is acknowledged, QUIC exits congestion recovery.
        /// </summary>
        private DateTime CongestionRecoveryStartTime { get; set; }

        /// <summary>
        ///     Slow start threshold in bytes. When the congestion window is below this value, the mode is slow start
        ///     and the window grows by the number of bytes acknowledged.
        /// </summary>
        private long SlowStartThreshold { get; set; }

        public int CongestionWindow { get; private set; }

        public int BytesInFlight { get; private set; }

        public void Reset()
        {
            CongestionWindow = InitialWindowSize;
            BytesInFlight = 0;
            CongestionRecoveryStartTime = DateTime.MaxValue;
            SlowStartThreshold = long.MaxValue;
            CongestionRecoveryStartTime = DateTime.MaxValue;
            CongestionWindow = InitialWindowSize;
        }

        public void OnPacketSent(SentPacket packet)
        {
            BytesInFlight += packet.BytesSent;
        }

        public void OnPacketAcked(SentPacket packet)
        {
            BytesInFlight -= packet.BytesSent;
            if (InCongestionRecovery(packet.TimeSent))
            {
                // do not increase congestion window in recovery period.
                return;
            }

            // TODO-RZ: Do not increase congestion window if limited by flow control or application has not supplied
            // enough data to saturate the connection
            // if (Is app or flow control limited) return;
            if (CongestionWindow < SlowStartThreshold)
            {
                // slow start
                CongestionWindow += packet.BytesSent;
            }
            else
            {
                // congestion avoidance
                CongestionWindow += MaxDatagramSize * packet.BytesSent / CongestionWindow;
            }
        }

        public void OnPacketsLost(List<SentPacket> lostPackets, DateTime now)
        {
            foreach (var packet in lostPackets)
            {
                BytesInFlight -= packet.BytesSent;
            }

            if (lostPackets.Count > 0)
            {
                var lastPacket = lostPackets[^1];
                OnCongestionEvent(lastPacket.TimeSent, now);

                if (InPersistentCongestion(lastPacket))
                {
                    CongestionWindow = MinimumWindowSize;
                }
            }
        }

        private bool InCongestionRecovery(DateTime sentTime)
        {
            return sentTime < CongestionRecoveryStartTime;
        }

        private bool InPersistentCongestion(SentPacket largestLostPacket)
        {
            // var pto = SmoothedRtt.Ticks + Math.Max(4 * RttVariation.Ticks, Recovery.TimerGranularity.Ticks) +
            // MaxAckDelay.Ticks;
            // var congestionPeriod = pto * Recovery.PersistentCongestionThreshold;
            // TODO-RZ: determine if all packets in the time period before the newest lost packet, including the edges
            // are marked lost
            return false;
        }

        internal void OnCongestionEvent(DateTime sentTime, DateTime now)
        {
            // start a new congestion event if packet was sent after the start of the previous congestion recovery period
            if (InCongestionRecovery(sentTime))
                return;

            CongestionRecoveryStartTime = now;
            CongestionWindow = Math.Max(MinimumWindowSize, (int)(CongestionWindow * LossReductionFactor));
            SlowStartThreshold = CongestionWindow;
        }
    }
}
