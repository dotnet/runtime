using System.Collections.Generic;
using System.Diagnostics;

namespace System.Net.Quic.Implementations.Managed.Internal.Recovery
{
    /// <summary>
    ///     Implementation of the reference congestion controll algorithm for QUIC based on the [RECOVERY] draft.
    /// </summary>
    internal class NewRenoCongestionController : ICongestionController
    {
        internal static readonly NewRenoCongestionController Instance = new NewRenoCongestionController();

        private NewRenoCongestionController()
        {
        }

        /// <summary>
        ///     Reduction in congestion window when a new loss event is detected. The RECOMMENDED value is 0.5.
        /// </summary>
        internal const double LossReductionFactor = 0.5;

        /// <summary>
        ///     Period of time for persistent congestion to be established, specified as the PTO multiplier.
        /// </summary>
        internal const int PersistentCongestionThreshold = 3;

        public void OnPacketSent(RecoveryController recovery, SentPacket packet)
        {
            Debug.Assert(packet.InFlight);
            recovery.BytesInFlight += packet.BytesSent;
        }

        public void OnPacketAcked(RecoveryController recovery, SentPacket packet, long now)
        {
            Debug.Assert(packet.InFlight);
            recovery.BytesInFlight -= packet.BytesSent;

            if (InCongestionRecovery(recovery, packet.TimeSent))
            {
                // do not increase congestion window in recovery period.
                return;
            }

            // TODO-RZ: Do not increase congestion window if limited by flow control or application has not supplied
            // enough data to saturate the connection
            // if (Is app or flow control limited) return;
            if (recovery.CongestionWindow < recovery.SlowStartThreshold)
            {
                // slow start
                recovery.CongestionWindow += packet.BytesSent;
            }
            else
            {
                // congestion avoidance
                recovery.CongestionWindow += RecoveryController.MaxDatagramSize * packet.BytesSent / recovery.CongestionWindow;
            }
        }

        public void OnPacketsLost(RecoveryController recovery, List<SentPacket> lostPackets, long now)
        {
            SentPacket? lastPacket = null;
            foreach (var packet in lostPackets)
            {
                Debug.Assert(packet.InFlight);
                recovery.BytesInFlight -= packet.BytesSent;
                lastPacket = packet;
            }

            if (lastPacket != null)
            {
                OnCongestionEvent(recovery, lastPacket.TimeSent, now);

                if (InPersistentCongestion(lastPacket))
                {
                    recovery.CongestionWindow = RecoveryController.MinimumWindowSize;
                }
            }
        }

        private bool InCongestionRecovery(RecoveryController recovery, long sentTimestamp)
        {
            return sentTimestamp < recovery.CongestionRecoveryStartTime;
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

        internal void OnCongestionEvent(RecoveryController recovery, long sentTimestamp, long now)
        {
            // start a new congestion event if packet was sent after the start of the previous congestion recovery period
            if (InCongestionRecovery(recovery, sentTimestamp))
                return;

            recovery.CongestionRecoveryStartTime = now;
            recovery.CongestionWindow = Math.Max(RecoveryController.MinimumWindowSize, (int)(recovery.CongestionWindow * LossReductionFactor));
            recovery.SlowStartThreshold = recovery.CongestionWindow;
        }
    }
}
