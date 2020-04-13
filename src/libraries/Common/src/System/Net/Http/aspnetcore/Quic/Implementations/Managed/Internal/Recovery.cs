using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Quic.Implementations.Managed.Internal.Frames;
using System.Reflection;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    /// <summary>
    ///     Class encapsulating logic on packet loss recovery.
    /// </summary>
    internal class Recovery
    {
        /// <summary>
        ///     Helper structure for holding packet number space related data together.
        /// </summary>
        private class PacketNumberSpace
        {
            /// <summary>
            ///     The largest packet number acknowledged in the packet number space so far.
            /// </summary>
            internal long LargestAckedPacketNumber { get; set; }

            /// <summary>
            ///     The time the most recent ack-eliciting packet was sent.
            /// </summary>
            internal DateTime TimeOfLastAckElicitingPacketSent { get; set; }

            /// <summary>
            ///     The time at which the next packet in the packet number space will be considered lost based on exceeding
            ///     the reordering window in time.
            /// </summary>
            internal DateTime NextLossTime { get; set; }

            /// <summary>
            ///     All sent packets, for which we are still awaiting acknowledgement.
            /// </summary>
            internal SortedList<long, SentPacket> SentPackets { get; } = new SortedList<long, SentPacket>();

            /// <summary>
            ///     List of packets deemed lost.
            /// </summary>
            internal List<SentPacket> LostPackets { get; } = new List<SentPacket>();

            /// <summary>
            ///     List of packets newly acked by the peer.
            /// </summary>
            internal List<SentPacket> AckedPackets { get; } = new List<SentPacket>();
        }

        private readonly PacketNumberSpace[] _pnSpaces = new PacketNumberSpace[]
        {
            new PacketNumberSpace(), new PacketNumberSpace(), new PacketNumberSpace()
        };

        private PacketNumberSpace GetPacketNumberSpace(PacketSpace space) => _pnSpaces[(int)space];

        /// <summary>
        ///     The most recent RTT measurement made when receiving ack for a previously unacked packet.
        /// </summary>
        private TimeSpan LatestRtt { get; set; }

        /// <summary>
        ///     The smoothed RTT of the connection. Computed as exponentially weighted average as described in RFC6298.
        /// </summary>
        private TimeSpan SmoothedRtt { get; set; }

        /// <summary>
        ///     The RTT variation, computed as described in RFC6298.
        /// </summary>
        private TimeSpan RttVariation { get; set; }

        /// <summary>
        ///     The minimum RTT seen in the connection, ignoring the ack delay.
        /// </summary>
        private TimeSpan MinimumRtt { get; set; }

        /// <summary>
        ///     The number of times a probe time out (PTO) has been sent without receiving an ack.
        /// </summary>
        private int PtoCount { get; set; }

        /// <summary>
        ///     Time when the next loss recovery tick needs to be made.
        /// </summary>
        internal DateTime LossRecoveryTimer { get; private set; } = DateTime.MaxValue;

        /// <summary>
        ///     Maximum delay by which the endpoint will delay sending acknowledgments. This value SHOULD include
        ///     the receiver's expected delays in alarms firing.
        /// </summary>
        internal TimeSpan MaxAckDelay { get; set; } = TimeSpan.FromMilliseconds(25);

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
        internal const int MaxDatagramSize = 1452;

        /// <summary>
        ///     Default limit on the initial amount of data in flight, in bytes. The RECOMMENDED value is the minimum
        ///     of 10 * MaxDatagramSize and max(2 * MaxDatagramSize, 14720).
        /// </summary>
        internal static readonly int InitialWindowSize = Math.Min(10 * MaxDatagramSize, Math.Max(2 * MaxDatagramSize, 14720));

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
        ///     Resets the recovery controller to the initial state.
        /// </summary>
        void Reset()
        {
            PtoCount = 0;
            LatestRtt = TimeSpan.Zero;
            SmoothedRtt = TimeSpan.Zero;
            MinimumRtt = TimeSpan.Zero;
        }

        internal void OnPacketSent(long packetNumber, PacketSpace space, SentPacket packet, bool handshakeComplete)
        {
            var pnSpace = GetPacketNumberSpace(space);
            pnSpace.SentPackets.Add(packetNumber, packet);

            if (packet.InFlight)
            {
                if (packet.AckEliciting)
                {
                    pnSpace.TimeOfLastAckElicitingPacketSent = packet.TimeSent;
                }

                OnPacketSentCC(packet.BytesSent);
                SetLossDetectionTimer(handshakeComplete);
            }
        }

        internal void OnAckReceived(PacketSpace space, ReadOnlySpan<PacketNumberRange> ranges, TimeSpan ackDelay,
            in AckFrame frame, DateTime now, bool isHandshakeComplete)
        {
            Debug.Assert(!ranges.IsEmpty);

            long largestAcked = ranges[^1].End;
            var pnSpace = GetPacketNumberSpace(space);

            pnSpace.LargestAckedPacketNumber = Math.Max(pnSpace.LargestAckedPacketNumber, largestAcked);

            bool isLargestAcknowledgedNewlyAcked = pnSpace.SentPackets.TryGetValue(largestAcked, out var largestAckedPacket);
            bool newlyAckedIncludeAckEliciting = ProcessNewlyAckedPackets(ranges, pnSpace);

            if (isLargestAcknowledgedNewlyAcked &&
                newlyAckedIncludeAckEliciting)
            {
                LatestRtt = now - largestAckedPacket!.TimeSent;
                if (space != PacketSpace.Application)
                {
                    ackDelay = TimeSpan.Zero;
                }

                UpdateRtt(ackDelay);
            }

            if (frame.HasEcnCounts)
            {
                // TODO-RZ: Process ECN information
            }

            DetectLostPackets(space, now);
            PtoCount = 0;
            SetLossDetectionTimer(isHandshakeComplete);
        }

        private bool ProcessNewlyAckedPackets(ReadOnlySpan<PacketNumberRange> ranges, PacketNumberSpace pnSpace)
        {
            int rangeIndex = 0;
            bool newlyAckedIncludeAckEliciting = false;
            // TODO-RZ: make this more efficient
            var pnsInFlight = pnSpace.SentPackets.Keys.ToArray();
            for (int i = 0; i < pnsInFlight.Length; i++)
            {
                long pn = pnsInFlight[i];
                while (ranges[rangeIndex].End < pn)
                {
                    rangeIndex++;
                    if (rangeIndex == ranges.Length)
                    {
                        // all ranges processed
                        return newlyAckedIncludeAckEliciting;
                    }
                }

                Debug.Assert(pn <= ranges[rangeIndex].End);
                if (pn < ranges[rangeIndex].Start)
                {
                    continue;
                }

                var packet = pnSpace.SentPackets[pn];
                newlyAckedIncludeAckEliciting |= packet.AckEliciting;

                OnPacketAcked(pn, packet, pnSpace);
                pnSpace.SentPackets.Remove(pn);
            }

            return newlyAckedIncludeAckEliciting;
        }

        private void OnPacketAcked(long packetNumber, SentPacket packet, PacketNumberSpace pnSpace)
        {
            if (packet.InFlight)
            {
                OnPacketAckedCC(packet);
            }

            pnSpace.SentPackets.Remove(packetNumber);
            pnSpace.AckedPackets.Add(packet);
        }

        internal void UpdateRtt(TimeSpan ackDelay)
        {
            // First RTT sample
            if (SmoothedRtt == TimeSpan.Zero)
            {
                MinimumRtt = LatestRtt;
                SmoothedRtt = LatestRtt;
                RttVariation = LatestRtt / 2;
                return;
            }

            // do the calculation in ticks to simplify math
            MinimumRtt = TimeSpan.FromTicks(Math.Min(MinimumRtt.Ticks, LatestRtt.Ticks));
            long ackDelayTicks = Math.Min(ackDelay.Ticks, MaxAckDelay.Ticks);

            // adjust for ack delay if plausible
            long adjustedRttTicks = LatestRtt.Ticks;
            if (adjustedRttTicks > MinimumRtt.Ticks + ackDelayTicks)
                adjustedRttTicks = LatestRtt.Ticks - ackDelayTicks;

            RttVariation =
                TimeSpan.FromTicks((long) (3.0 / 4 * RttVariation.Ticks +
                                           1.0 / 4 * Math.Abs(SmoothedRtt.Ticks - adjustedRttTicks)));
        }

        private (DateTime, PacketSpace) GetEarliestLossTime(bool handshakeComplete)
        {
            var time = GetPacketNumberSpace(PacketSpace.Initial).NextLossTime;
            var space = PacketSpace.Initial;

            if (GetPacketNumberSpace(PacketSpace.Handshake).NextLossTime < time)
            {
                time = GetPacketNumberSpace(PacketSpace.Handshake).NextLossTime;
                space = PacketSpace.Handshake;
            }

            // skip application epoch until handshake completes
            if (handshakeComplete &&
                GetPacketNumberSpace(PacketSpace.Application).NextLossTime < time)
            {
                time = GetPacketNumberSpace(PacketSpace.Application).NextLossTime;
                space = PacketSpace.Application;
            }

            return (time, space);
        }

        private (DateTime, PacketSpace) GetEarliestLastAckElicitingPacketSent(bool handshakeComplete)
        {
            var time = GetPacketNumberSpace(PacketSpace.Initial).TimeOfLastAckElicitingPacketSent;
            var space = PacketSpace.Initial;

            if (GetPacketNumberSpace(PacketSpace.Handshake).TimeOfLastAckElicitingPacketSent < time)
            {
                time = GetPacketNumberSpace(PacketSpace.Handshake).TimeOfLastAckElicitingPacketSent;
                space = PacketSpace.Handshake;
            }

            // skip application epoch until handshake completes
            if (handshakeComplete &&
                GetPacketNumberSpace(PacketSpace.Application).TimeOfLastAckElicitingPacketSent < time)
            {
                time = GetPacketNumberSpace(PacketSpace.Application).TimeOfLastAckElicitingPacketSent;
                space = PacketSpace.Application;
            }

            return (time, space);
        }

        private bool PeerNotAwaitingAddressValidation()
        {
            // Assume clients validate the server's address implicitly.
            // if (_isServer)
                // return true;

            // servers complete address validation when a protected packet is received
            // TODO-RZ: return (has received Handshake ACK || has received 1-RTT ACK)
            return true;
        }

        private void SetLossDetectionTimer(bool isHandshakeComplete)
        {
            var (earliestLossTime, _) = GetEarliestLossTime(isHandshakeComplete);

            if (earliestLossTime != default)
            {
                // Time threshold loss detection.
                LossRecoveryTimer = earliestLossTime;
                return;
            }

            if ( // TODO-RZ: no ack-eliciting packets in flight &&
                PeerNotAwaitingAddressValidation())
            {
                // cancel the timer
                LossRecoveryTimer = DateTime.MaxValue;
                return;
            }

            TimeSpan timeout;
            if (SmoothedRtt == TimeSpan.Zero)
            {
                // use a default timeout if there are no RTT measurements
                timeout = 2 * InitialRtt;
            }
            else
            {
                timeout = SmoothedRtt +
                          TimeSpan.FromTicks(Math.Max(4 * RttVariation.Ticks, TimerGranularity.Ticks)) +
                          MaxAckDelay;
            }

            timeout *= 1 << PtoCount;

            var (sentTime, _) = GetEarliestLastAckElicitingPacketSent(isHandshakeComplete);
            LossRecoveryTimer = sentTime + timeout;
        }

        private void OnLossDetectionTimeout(bool isServer, bool isHandshakeComplete, DateTime now)
        {
            var (earliestLossTime, space) = GetEarliestLossTime(isHandshakeComplete);

            if (earliestLossTime != default)
            {
                // Time threshold loss detection
                DetectLostPackets(space, now);
                SetLossDetectionTimer(isHandshakeComplete);
                return;
            }

            // if (!isServer && GetPacketNumberSpace(EncryptionLevel.Application).RecvCryptoSeal == null)
            {
                // TODO-RZ: Client needs to send an anti-deadlock packet:
                throw new NotImplementedException();
            }
            // else
            {
                // TODO-RZ: PTO. Send new data if available, else retransmit old data.
                // If neither is available, send single PING frame.
                throw new NotImplementedException();
            }

            // PtoCount++;
            // SetLossDetectionTimer();
        }

        private void DetectLostPackets(PacketSpace space, DateTime now)
        {
            var pnSpace = GetPacketNumberSpace(space);

            pnSpace.NextLossTime = DateTime.MaxValue;

            var lostPackets = new List<(long pn, SentPacket packet)>();
            var lossDelayTicks = (long)(TimeReorderingThreshold *
                                        Math.Max(LatestRtt.Ticks, SmoothedRtt.Ticks));

            // minimum time based on timer granularity before packets are deemded lost.
            lossDelayTicks = Math.Max(lossDelayTicks, TimerGranularity.Ticks);

            var lostSendTime = now - TimeSpan.FromTicks(lossDelayTicks);

            long largestAcked = pnSpace.LargestAckedPacketNumber;

            // make a copy before iterating because we are going to remove items from the collection
            foreach (var (pn, packet) in pnSpace.SentPackets.ToArray())
            {
                if (pn > largestAcked)
                    continue;

                if (packet.TimeSent <= lostSendTime ||
                    largestAcked >= pn + PacketReorderingThreshold)
                {
                    // Mark packet as lost
                    pnSpace.SentPackets.Remove(pn);

                    // TODO-RZ: why only in-flight are to be processed this way?
                    if (packet.InFlight)
                    {
                        lostPackets.Add((pn, packet));
                    }
                }
                else
                {
                    // set time when the packet should be marked lost
                    if (packet.TimeSent.AddTicks(lossDelayTicks) < pnSpace.NextLossTime )
                    {
                        pnSpace.NextLossTime = packet.TimeSent.AddTicks(lossDelayTicks);
                    }
                }
            }

            // Inform the congestion controller of lost packets and let it decide whether to retransmit immediately.
            OnPacketsLost(lostPackets, now);
        }


        // congestion control implementation:

        /// <summary>
        ///     The sum of the size in bytes of all sent packets that contain at least one ack-eliciting or PADDING
        ///     frame, and have not been acked or declared lost. The size does not include IP or UDP overhead, but does
        ///     Include the QUIC header and AEAD overhead. Packets only containing ACK frames do not count towards this
        ///     count to ensure congestion control does not impede congestion feedback.
        /// </summary>
        private long BytesInFlight { get; set; }

        /// <summary>
        ///     Maximum number of bytes in flight that may be sent.
        /// </summary>
        private long CongestionWindow { get; set; }

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

        private void InitCongestionController()
        {
            CongestionWindow = InitialWindowSize;
            BytesInFlight = 0;
            CongestionRecoveryStartTime = DateTime.MaxValue;
            SlowStartThreshold = Int64.MaxValue;

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

        private void OnPacketSentCC(int bytes)
        {
            BytesInFlight += bytes;
        }

        private void OnPacketAckedCC(SentPacket packet)
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

        internal List<SentPacket> GetAckedFrames(PacketSpace space)
        {
            return GetPacketNumberSpace(space).AckedPackets;
        }

        internal void OnCongestionEvent(DateTime sentTime, DateTime now)
        {
            // start a new congestion event if packet was sent after the start of the previous congestion recovery period
            if (InCongestionRecovery(sentTime))
                return;

            CongestionRecoveryStartTime = now;
            CongestionWindow = Math.Max(MinimumWindowSize,
                (long) (CongestionWindow * LossReductionFactor));
            SlowStartThreshold = CongestionWindow;
        }

        internal void OnPacketsLost(List<(long, SentPacket)> lostPackets, DateTime now)
        {
            foreach ((_, SentPacket packet) in lostPackets)
            {
                BytesInFlight -= packet.BytesSent;
            }

            if (lostPackets.Count > 0)
            {
                var (_, lastPacket) = lostPackets[^1];
                OnCongestionEvent(lastPacket.TimeSent, now);

                if (InPersistentCongestion(lastPacket))
                {
                    CongestionWindow = MinimumWindowSize;
                }
            }
        }

    }
}
