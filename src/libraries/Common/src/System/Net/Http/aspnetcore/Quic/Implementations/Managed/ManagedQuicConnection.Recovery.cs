using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Frames;

namespace System.Net.Quic.Implementations.Managed
{
    internal sealed partial class ManagedQuicConnection
    {
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
        ///     Timestamp when loss detection timer should fire.
        /// </summary>
        private DateTime NextTimerAlarm { get; set; }

        private TimeSpan MaxAckDelay => TimeSpan.FromMilliseconds(_peerTransportParameters.MaxAckDelay);

        /// <summary>
        ///     The largest packet number acknowledged in the packet number space so far.
        /// </summary>
        private long[] LargestAckedPacket { get; } = new long[3];

        /// <summary>
        ///     The time the most recent ack-eliciting packet was sent.
        /// </summary>
        private DateTime[] TimeOfLastSentAckElicitingPacket { get; } = new DateTime[3];

        /// <summary>
        ///     The time at which the next packet in the packet number space will be considered lost based on exceeding
        ///     the reordering window in time.
        /// </summary>
        private DateTime[] LossTime { get; } = new DateTime[3];

        private IDictionary<long, SentPacket> SentPackets(int epoch) => _pnSpaces[epoch].PacketsInFlight;

        private void InitLossDetection()
        {
            NextTimerAlarm = DateTime.MaxValue;
            PtoCount = 0;
            LatestRtt = TimeSpan.Zero;
            SmoothedRtt = TimeSpan.Zero;
            MinimumRtt = TimeSpan.Zero;
        }

        private void OnPacketSent(long packetNumber, PacketSpace pnSpace, SentPacket packet, DateTime now)
        {
            if (packet.InFlight)
            {
                if (packet.AckEliciting)
                {
                    TimeOfLastSentAckElicitingPacket[(int)pnSpace] = now;
                }

                OnPacketSentCC(packet.BytesSent);
                SetLossDetectionTimer();
            }
        }

        private void OnAckReceived(PacketSpace space, ReadOnlySpan<PacketNumberRange> ranges, TimeSpan ackDelay,
            in AckFrame frame, DateTime now)
        {
            Debug.Assert(!ranges.IsEmpty);

            long largestAcked = ranges[^1].End;
            var pnSpace = _pnSpaces[(int)space];

            LargestAckedPacket[(int)space] = Math.Max(LargestAckedPacket[(int)space], largestAcked);

            bool isLargestAcknowledgedNewlyAcked = pnSpace.PacketsInFlight.TryGetValue(largestAcked, out var largestAckedPacket);
            bool newlyAckedIncludeAckEliciting = false;

            int rangeIndex = 0;
            // TODO-RZ: make this more efficient
            var pnsInFlight = pnSpace.PacketsInFlight.Keys.ToArray();
            for (int i = 0; i < pnsInFlight.Length; i++)
            {
                long pn = pnsInFlight[i];
                while (ranges[rangeIndex].End < pn)
                {
                    rangeIndex++;
                    if (rangeIndex == ranges.Length)
                    {
                        // all ranges processed
                        return;
                    }
                }

                Debug.Assert(pn <= ranges[rangeIndex].End);
                if (pn < ranges[rangeIndex].Start)
                {
                    continue;
                }

                var packet = pnSpace.PacketsInFlight[pn];
                newlyAckedIncludeAckEliciting |= packet.AckEliciting;

                OnPacketAcked(pnSpace, packet);
                pnSpace.PacketsInFlight.Remove(pn);
            }

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
            SetLossDetectionTimer();
        }

        private void OnPacketAcked(PacketNumberSpace pnSpace, SentPacket packet)
        {
            // mark all sent data as acked
            foreach (var r in packet.CryptoRanges)
            {
                pnSpace.CryptoOutboundStream.OnAck(
                    r.Start, r.Length);
            }

            foreach ((long streamId, RangeSet ranges) in packet.SentStreamData)
            {
                var buffer = _streams[streamId].OutboundBuffer!;
                foreach (var r in ranges)
                {
                    buffer.OnAck(r.Start, r.Length);
                }
            }

            // Since we know the acks arrived, we don't want to send acks sent by this packet anymore.
            pnSpace.UnackedPacketNumbers.Remove(packet.AckedRanges);

            if (packet.InFlight)
            {
                OnPacketAckedCC(packet);
            }
        }

        private void UpdateRtt(TimeSpan ackDelay)
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
            // TODO-RZ: to ticks
            long ackDelayTicks = Math.Min(ackDelay.Ticks, _peerTransportParameters.MaxAckDelay);

            // adjust for ack delay if plausible
            var adjustedRttTicks = LatestRtt.Ticks;
            if (adjustedRttTicks > MinimumRtt.Ticks + ackDelayTicks)
                adjustedRttTicks = LatestRtt.Ticks - ackDelayTicks;

            RttVariation =
                TimeSpan.FromTicks((long) (3.0 / 4 * RttVariation.Ticks +
                                           1.0 / 4 * Math.Abs(SmoothedRtt.Ticks - adjustedRttTicks)));
        }

        private (DateTime, PacketSpace) GetEarliestTimeAndSpace(DateTime[] times)
        {
            var time = times[0];
            var space = PacketSpace.Initial;

            if (times[1] != default &&
                (time == default || times[1] < time))
            {
                time = times[1];
                space = PacketSpace.Handshake;
            }

            if (times[2] != default &&
                (time == default || times[2] < time) &&
                // skip application epoch until handshake completes
                _tls.IsHandshakeComplete) // TODO-RZ: complete or confirmed?
            {

                time = times[2];
                space = PacketSpace.Application;
            }

            return (time, space);
        }

        private bool PeerNotAwaitingAddressValidation()
        {
            // Assume clients validate the server's address implicitly.
            if (_isServer)
                return true;

            // servers complete address validation when a protected packet is received
            // TODO-RZ: return (has received Handshake ACK || has received 1-RTT ACK)
            return true;
        }

        private void SetLossDetectionTimer()
        {
            var (earliestLossTime, _) = GetEarliestTimeAndSpace(LossTime);

            if (earliestLossTime != default)
            {
                // Time threshold loss detection.
                NextTimerAlarm = earliestLossTime;
                return;
            }

            if ( // TODO-RZ: no ack-eliciting packets in flight &&
                PeerNotAwaitingAddressValidation())
            {
                // cancel the timer
                NextTimerAlarm = DateTime.MaxValue;
                return;
            }

            TimeSpan timeout;
            if (SmoothedRtt == TimeSpan.Zero)
            {
                // use a default timeout if there are no RTT measurements
                timeout = 2 * Recovery.InitialRtt;
            }
            else
            {
                timeout = SmoothedRtt +
                          TimeSpan.FromTicks(Math.Max(4 * RttVariation.Ticks, Recovery.TimerGranularity.Ticks)) +
                          TimeSpan.FromMilliseconds(_peerTransportParameters.MaxAckDelay);
            }

            timeout *= 1 << PtoCount;

            var (sentTime, _) = GetEarliestTimeAndSpace(TimeOfLastSentAckElicitingPacket);
            NextTimerAlarm = sentTime + timeout;
        }

        private void OnLossDetectionTimeout(DateTime now)
        {
            var (earliestLossTime, space) = GetEarliestTimeAndSpace(LossTime);

            if (earliestLossTime != default)
            {
                // Time threshold loss detection
                DetectLostPackets(space, now);
                SetLossDetectionTimer();
                return;
            }

            if (!_isServer && GetPacketNumberSpace(EncryptionLevel.Application).RecvCryptoSeal == null)
            {
                // TODO-RZ: Client needs to send an anti-deadlock packet:
                throw new NotImplementedException();
            }
            else
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
            LossTime[(int)space] = default;

            var lostPackets = new List<(long pn, SentPacket packet)>();
            var lossDelayTicks = (long)(Recovery.TimeReorderingThreshold *
                                        Math.Max(LatestRtt.Ticks, SmoothedRtt.Ticks));

            // minimum time based on timer granularity before packets are deemded lost.
            lossDelayTicks = Math.Max(lossDelayTicks, Recovery.TimerGranularity.Ticks);

            var lostSendTime = now - TimeSpan.FromTicks(lossDelayTicks);

            long largestAcked = LargestAckedPacket[(int)space];

            var sentPackets = SentPackets((int)space);

            // make a copy before iterating because we are going to remove items from the collection
            foreach (var (pn, packet) in sentPackets.ToArray())
            {
                if (pn > largestAcked)
                    continue;

                if (packet.TimeSent <= lostSendTime ||
                    largestAcked >= pn + Recovery.PacketReorderingThreshold)
                {
                    // Mark packet as lost
                    sentPackets.Remove(pn);

                    // TODO-RZ: why only in-flight are to be processed this way?
                    if (packet.InFlight)
                    {
                        lostPackets.Add((pn, packet));
                    }
                }
                else
                {
                    // set time when the packet should be marked lost
                    if (LossTime[(int)space] == default ||
                        packet.TimeSent.AddTicks(lossDelayTicks) < LossTime[(int) space])
                    {
                        LossTime[(int)space] = packet.TimeSent.AddTicks(lossDelayTicks);
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
            CongestionWindow = Recovery.InitialWindowSize;
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
                CongestionWindow += _peerTransportParameters.MaxPacketSize * packet.BytesSent / CongestionWindow;
            }
        }

        internal void OnCongestionEvent(DateTime sentTime, DateTime now)
        {
            // start a new congestion event if packet was sent after the start of the previous congestion recovery period
            if (InCongestionRecovery(sentTime))
                return;

            CongestionRecoveryStartTime = now;
            CongestionWindow = Math.Max(Recovery.MinimumWindowSize,
                (long) (CongestionWindow * Recovery.LossReductionFactor));
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
                    CongestionWindow = Recovery.MinimumWindowSize;
                }
            }
        }
    }
}
