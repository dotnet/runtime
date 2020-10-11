// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal.Frames;
using System.Net.Quic.Implementations.Managed.Internal.Recovery;
using System.Net.Quic.Implementations.Managed.Internal.Tracing;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    /// <summary>
    ///     Class encapsulating logic on packet loss recovery.
    /// </summary>
    internal class RecoveryController
    {
        internal const int MaxDatagramSize = 1452;


        public RecoveryController(QuicTrace? trace = null)
        {
            Trace = trace;
            CongestionController = NewRenoCongestionController.Instance;
            Reset();
        }

        /// <summary>
        ///     Object for logging traces for the connection events.
        /// </summary>
        public QuicTrace? Trace { get; }

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
        ///     Timer granularity in ticks. The value is system-dependent, but SHOULD be at least 1ms.
        /// </summary>
        internal static readonly long TimerGranularity = Timestamp.FromMilliseconds(5);

        /// <summary>
        ///     The RTT used before an RTT sample is taken in ticks. the RECOMMENDED value is 500ms.
        /// </summary>
        internal static readonly long InitialRtt = Timestamp.FromMilliseconds(500);

        /// <summary>
        ///     Minimum congestion window in bytes. The RECOMMENDED value is 2 * MaxDatagramSize.
        /// </summary>
        internal const int MinimumWindowSize = 2 * MaxDatagramSize;

        /// <summary>
        ///     Default limit on the initial amount of data in flight, in bytes. The RECOMMENDED value is the minimum
        ///     of 10 * MaxDatagramSize and max(2 * MaxDatagramSize, 14720).
        /// </summary>
        internal static readonly int InitialWindowSize =
            Math.Min(10 * MaxDatagramSize, Math.Max(2 * MaxDatagramSize, 14720));


        /// <summary>
        ///     Helper structure for holding packet number space related data together.
        /// </summary>
        internal class PacketNumberSpace
        {
            internal static readonly Comparer<PacketNumberSpace> LossTimeComparer = Comparer<PacketNumberSpace>.Create(
                (l, r) => l.NextLossTime.CompareTo(r.NextLossTime));

            internal static readonly Comparer<PacketNumberSpace> TimeOfLastAckElicitingPacketSentComparer = Comparer<PacketNumberSpace>.Create((l, r) =>
                l.TimeOfLastAckElicitingPacketSent.CompareTo(r.TimeOfLastAckElicitingPacketSent));

            public PacketNumberSpace(PacketType packetType)
            {
                PacketType = packetType;
            }

            /// <summary>
            ///     Packet type sent in this packet number space.
            /// </summary>
            internal PacketType PacketType { get; }

            /// <summary>
            ///     The largest packet number sent by this endpoint which was acknowledged in the packet number space so far.
            /// </summary>
            internal long LargestAckedPacketNumber { get; set; }

            /// <summary>
            ///     The largest packet number known to be received by the peer.
            /// </summary>
            internal long LargestTransportedPacketNumber { get; set; }

            /// <summary>
            ///     The time the most recent ack-eliciting packet was sent. MaxValue if no packet was sent yet.
            /// </summary>
            internal long TimeOfLastAckElicitingPacketSent { get; set; }

            /// <summary>
            ///     The time at which the next packet in the packet number space will be considered lost based on exceeding
            ///     the reordering window in time. MaxValue if no packet in flight.
            /// </summary>
            internal long NextLossTime { get; set; }

            /// <summary>
            ///     All sent packets, for which we are still awaiting acknowledgement. Ordered by packet number.
            /// </summary>
            internal List<SentPacket> SentPackets { get; } = new List<SentPacket>();

            /// <summary>
            ///     Queue of packets deemed lost.
            /// </summary>
            internal Queue<(SentPacket packet, PacketLossTrigger trigger)> LostPackets { get; } = new Queue<(SentPacket packet, PacketLossTrigger trigger)>();

            /// <summary>
            ///     Queue of packets newly acked by the peer.
            /// </summary>
            internal Queue<SentPacket> AckedPackets { get; } = new Queue<SentPacket>();

            /// <summary>
            ///     If PTO timer expired, contains number of remaining probe packets this endpoint should send as a
            ///     reaction to the timeout. Otherwise 0.
            /// </summary>
            internal int RemainingLossProbes { get; set; }

            /// <summary>
            ///     Resets the state of the packet space to the initial state.
            /// </summary>
            internal void Reset()
            {
                RemainingLossProbes = 0;
                AckedPackets.Clear();
                LostPackets.Clear();
                SentPackets.Clear();
                NextLossTime = long.MaxValue;
                LargestAckedPacketNumber = 0;
                TimeOfLastAckElicitingPacketSent = long.MaxValue;
            }
        }

        private readonly PacketNumberSpace[] _pnSpaces =
        {
            new PacketNumberSpace(PacketType.Initial),
            new PacketNumberSpace(PacketType.Handshake),
            new PacketNumberSpace(PacketType.OneRtt)
        };

        internal PacketNumberSpace GetPacketNumberSpace(PacketSpace space) => _pnSpaces[(int)space];

        /// <summary>
        ///     The most recent RTT measurement in ticks made when receiving ack for a previously unacked packet.
        /// </summary>
        internal long LatestRtt { get; private set; }

        /// <summary>
        ///     The smoothed RTT of the connection in ticks. Computed as exponentially weighted average as described in RFC6298.
        /// </summary>
        internal long SmoothedRtt { get; private set; }

        /// <summary>
        ///     The RTT variation in ticks, computed as described in RFC6298.
        /// </summary>
        internal long RttVariation { get; private set; }

        /// <summary>
        ///     The minimum RTT seen in the connection in ticks, ignoring the ack delay.
        /// </summary>
        internal long MinimumRtt { get; private set; }

        /// <summary>
        ///     The number of times a probe time out (PTO) has been sent without receiving an ack.
        /// </summary>
        internal int PtoCount { get; private set; }

        /// <summary>
        ///     Time when the next loss recovery tick needs to be made.
        /// </summary>
        internal long LossRecoveryTimer { get; private set; }

        /// <summary>
        ///     Maximum delay by which the endpoint will delay sending acknowledgments. This value SHOULD include
        ///     the receiver's expected delays in alarms firing. The value is in ticks.
        /// </summary>
        internal long MaxAckDelay { get; set; }

        /// <summary>
        ///     Congestion controller algorithm used.
        /// </summary>
        internal ICongestionController CongestionController { get; }

        /// <summary>
        ///     Returns bytes available with respect to the current congestion window
        /// </summary>
        /// <returns></returns>
        internal int GetAvailableCongestionWindowBytes()
        {
             return Math.Max(0, CongestionWindow - BytesInFlight);
        }

        /// <summary>
        ///     The sum of the size in bytes of all sent packets that contain at least one ack-eliciting or PADDING
        ///     frame, and have not been acked or declared lost. The size does not include IP or UDP overhead, but does
        ///     Include the QUIC header and AEAD overhead. Packets only containing ACK frames do not count towards this
        ///     count to ensure congestion control does not impede congestion feedback.
        /// </summary>
        internal int BytesInFlight { get; set; }

        /// <summary>
        ///     Current width of the congestion window in bytes.
        /// </summary>
        public int CongestionWindow { get; set; }

        /// <summary>
        ///     The timestamp when QUIC first detects congestion due to loss of ECN, causing it to enter congestion
        ///     recovery. When a packet sent after this time is acknowledged, QUIC exits congestion recovery.
        /// </summary>
        internal long CongestionRecoveryStartTime { get; set; }

        /// <summary>
        ///     Slow start threshold in bytes. When the congestion window is below this value, the mode is slow start
        ///     and the window grows by the number of bytes acknowledged.
        /// </summary>
        internal int SlowStartThreshold { get; set; }

        /// <summary>
        ///     Current state of the congestion control algorithm. This is largely only for tracing purposes.
        /// </summary>
        public CongestionState CongestionState { get; set; }

        /// <summary>
        ///     Resets the recovery controller to the initial state.
        /// </summary>
        internal void Reset()
        {
            PtoCount = 0;
            LatestRtt = 0;
            SmoothedRtt = 0;
            MinimumRtt = 0;
            CongestionWindow = InitialWindowSize;
            SlowStartThreshold = int.MaxValue;
            LossRecoveryTimer = long.MaxValue;
            MaxAckDelay = Timestamp.FromMilliseconds(25);
            BytesInFlight = 0;

            foreach (PacketNumberSpace space in _pnSpaces)
            {
                space.Reset();
            }

            Trace?.OnRecoveryParametersSet(this);
        }

        /// <summary>
        ///     Informs the recovery controller that a packet has been sent to the peer.
        /// </summary>
        /// <param name="space">Packet space in which the packet was sent.</param>
        /// <param name="packet">The sent packet.</param>
        /// <param name="isHandshakeComplete">True if the TLS handshake has been completed.</param>
        internal void OnPacketSent(PacketSpace space, SentPacket packet, bool isHandshakeComplete)
        {
            var pnSpace = GetPacketNumberSpace(space);
            pnSpace.SentPackets.Add(packet);

            if (packet.InFlight)
            {
                if (packet.AckEliciting)
                {
                    pnSpace.TimeOfLastAckElicitingPacketSent = packet.TimeSent;
                }

                CongestionController.OnPacketSent(this, packet);

                // Note that we do not set NextLossTime because we need to receive an ack for later packet in order
                // to deem this packet lost. The NextLossTime has to be set only when receiving ack or during the
                // loss timer processing.
                SetLossDetectionTimer(isHandshakeComplete);

                Trace?.OnRecoveryMetricsUpdated(this);
                Trace?.OnCongestionStateUpdated(CongestionState);
            }
        }

        /// <summary>
        ///     Instance of <see cref="SentPacket"/> to be used in binary search to avoid one per each call.
        /// </summary>
        internal readonly SentPacket _binarySearchPacket = new SentPacket();

        /// <summary>
        ///     Informs the recovery controller that ACK frames have been received.
        /// </summary>
        /// <param name="space">Packet space for which the frame was received.</param>
        /// <param name="ranges">Acknowledged ranges of packet numbers.</param>
        /// <param name="ackDelay">Ack delay reported in the frame.</param>
        /// <param name="frame">The received frame.</param>
        /// <param name="now">Timestamp of the current moment.</param>
        /// <param name="isHandshakeComplete">True if the TLS handshake has been completed.</param>
        internal void OnAckReceived(PacketSpace space, Span<RangeSet.Range> ranges, long ackDelay,
            in AckFrame frame, long now, bool isHandshakeComplete)
        {
            Debug.Assert(ranges.Length > 0);

            long largestAcked = ranges[^1].End;
            var pnSpace = GetPacketNumberSpace(space);

            pnSpace.LargestAckedPacketNumber = Math.Max(pnSpace.LargestAckedPacketNumber, largestAcked);

            SentPacket? largestAckedPacket = null;
            {
                _binarySearchPacket.PacketNumber = largestAcked;
                int index = pnSpace.SentPackets.BinarySearch(_binarySearchPacket, SentPacket.PacketNumberComparer);
                if ((uint) index < pnSpace.SentPackets.Count)
                {
                    largestAckedPacket = pnSpace.SentPackets[index];
                }
            }

            bool isLargestAcknowledgedNewlyAcked = largestAckedPacket != null;
            bool newlyAckedIncludeAckEliciting = ProcessNewlyAckedPackets(ranges, pnSpace, now);

            if (isLargestAcknowledgedNewlyAcked &&
                newlyAckedIncludeAckEliciting)
            {
                LatestRtt = now - largestAckedPacket!.TimeSent;
                if (space != PacketSpace.Application)
                {
                    ackDelay = 0;
                }

                UpdateRtt(ackDelay);
            }

            if (frame.HasEcnCounts)
            {
                // TODO-RZ: Process ECN information
            }

            DetectLostPackets(pnSpace, now);
            PtoCount = 0;
            SetLossDetectionTimer(isHandshakeComplete);
        }

        private bool ProcessNewlyAckedPackets(Span<RangeSet.Range> ranges, PacketNumberSpace pnSpace,
            long now)
        {
            int rangeIndex = 0;
            bool newlyAckedIncludeAckEliciting = false;

            int toRemove = 0;
            int removeStart = 0;
            var sentPackets = pnSpace.SentPackets;

            // remove the acked packet from the sentPacket list, compacting it during the process
            while (toRemove + removeStart < sentPackets.Count)
            {
                var packet = sentPackets[toRemove + removeStart];
                long pn = packet.PacketNumber;

                while (rangeIndex < ranges.Length && ranges[rangeIndex].End < pn)
                {
                    rangeIndex++;
                }

                if (rangeIndex == ranges.Length)
                {
                    // all ranges processed
                    break;
                }

                Debug.Assert(pn <= ranges[rangeIndex].End);
                if (pn < ranges[rangeIndex].Start)
                {
                    // this packet was not acked, move it before the acked (deleted) ones
                    sentPackets[removeStart] = packet;
                    removeStart++;
                    continue;
                }

                // packet is acked, remove it from the list
                newlyAckedIncludeAckEliciting |= packet.AckEliciting;

                if (packet.InFlight)
                {
                    CongestionController.OnPacketAcked(this, packet, now);
                }

                pnSpace.LargestTransportedPacketNumber = Math.Max(pn, pnSpace.LargestTransportedPacketNumber);
                pnSpace.AckedPackets.Enqueue(packet);
                toRemove++;
            }

            // remove the range left by acked packets to shift packets that we did not get to.
            sentPackets.RemoveRange(removeStart, toRemove);

            return newlyAckedIncludeAckEliciting;
        }

        private void UpdateRtt(long ackDelay)
        {
            // First RTT sample
            if (SmoothedRtt == 0)
            {
                MinimumRtt = LatestRtt;
                SmoothedRtt = LatestRtt;
                RttVariation = LatestRtt / 2;
                return;
            }

            MinimumRtt = Math.Min(MinimumRtt, LatestRtt);
            ackDelay = Math.Min(ackDelay, MaxAckDelay);

            // adjust for ack delay if plausible
            long adjustedRtt = LatestRtt;
            if (adjustedRtt > MinimumRtt + ackDelay)
                adjustedRtt = LatestRtt - ackDelay;

            SmoothedRtt = (7 * SmoothedRtt + adjustedRtt) / 8;
            RttVariation = 3 * RttVariation / 4 + 1 * Math.Abs(SmoothedRtt - adjustedRtt) / 4;
        }

        private PacketNumberSpace GetEarliestSpace(bool handshakeComplete, Comparer<PacketNumberSpace> comparer)
        {
            var epoch = _pnSpaces[0];

            // skip the last (application) packet number space until handshake completes
            for (int i = 1; i < (handshakeComplete ? 3 : 2); i++)
            {
                if (comparer.Compare(_pnSpaces[i], epoch) < 0)
                {
                    epoch = _pnSpaces[i];
                }
            }

            return epoch;
        }

        private bool PeerNotAwaitingAddressValidation()
        {
            // Assume clients validate the server's address implicitly.
            // if (_isServer)
                // return true;

            // servers complete address validation when a protected packet is received
            // TODO-RZ: Implement peer address validation.
            return true;
        }

        private void SetLossDetectionTimer(bool isHandshakeComplete)
        {
            // cancel previous timer
            LossRecoveryTimer = long.MaxValue;

            long earliestLossTime = GetEarliestSpace(isHandshakeComplete, PacketNumberSpace.LossTimeComparer).NextLossTime;
            if (earliestLossTime != long.MaxValue)
            {
                // Time threshold loss detection.
                LossRecoveryTimer = earliestLossTime;
                return;
            }

            if (BytesInFlight == 0 && // no ack-eliciting packets in flight
                PeerNotAwaitingAddressValidation())
            {
                // no timer set
                return;
            }

            // probe timeout
            long lastAckElicitingSent = GetEarliestSpace(isHandshakeComplete, PacketNumberSpace.TimeOfLastAckElicitingPacketSentComparer).TimeOfLastAckElicitingPacketSent;
            if (lastAckElicitingSent == long.MaxValue)
            {
                return;
            }

            LossRecoveryTimer = lastAckElicitingSent + GetProbeTimeoutInterval();
        }

        /// <summary>
        ///     Gets currently used PTO interval.
        /// </summary>
        internal long GetProbeTimeoutInterval()
        {
            long timeout;

            if (SmoothedRtt == 0)
            {
                // use a default timeout if there are no RTT measurements
                timeout = 2 * InitialRtt;
            }
            else
            {
                timeout = SmoothedRtt +
                          Math.Max(4 * RttVariation, TimerGranularity) +
                          MaxAckDelay;
            }

            return timeout * (1 << PtoCount);
        }

        /// <summary>
        ///     Called to process a timeout event.
        /// </summary>
        /// <param name="isHandshakeComplete">True if TLS handshake has been completed.</param>
        /// <param name="now">Timestamp of the current moment.</param>
        internal void OnLossDetectionTimeout(bool isHandshakeComplete, long now)
        {
            var pnSpace = GetEarliestSpace(isHandshakeComplete, PacketNumberSpace.LossTimeComparer);
            long earliestLossTime = pnSpace.NextLossTime;

            if (earliestLossTime != long.MaxValue)
            {
                // Time threshold loss detection
                DetectLostPackets(pnSpace, now);
                SetLossDetectionTimer(isHandshakeComplete);
                return;
            }

            // if no ack-based loss detection expected, set timeout for probing if last sent packet
            // was lost
            pnSpace = GetEarliestSpace(isHandshakeComplete,
                PacketNumberSpace.TimeOfLastAckElicitingPacketSentComparer);

            Debug.Assert(pnSpace.TimeOfLastAckElicitingPacketSent != long.MaxValue);

            pnSpace.RemainingLossProbes = 2;
            PtoCount++;

            SetLossDetectionTimer(isHandshakeComplete);
        }

        private void DetectLostPackets(PacketNumberSpace pnSpace, long now)
        {
            // will be set again later, if necessary
            pnSpace.NextLossTime = long.MaxValue;

            // lost packets to be passed to congestion controller (with InFlight = true)
            int newlyLostCount = 0;
            SentPacket[] newlyLost = ArrayPool<SentPacket>.Shared.Rent(pnSpace.SentPackets.Count);

            long lossDelay = (long)(TimeReorderingThreshold * Math.Max(LatestRtt, SmoothedRtt));

            // minimum time based on timer granularity before packets are deemded lost.
            lossDelay = Math.Max(lossDelay, TimerGranularity);

            long lostSendTime = now - lossDelay;
            long largestAcked = pnSpace.LargestAckedPacketNumber;

            int removed = 0;
            for (; removed < pnSpace.SentPackets.Count; removed++)
            {
                var packet = pnSpace.SentPackets[removed];

                if (packet.PacketNumber > largestAcked)
                {
                    // this and all following packets are not deemed lost yet
                    break;
                }

                PacketLossTrigger? trigger = null;
                if (packet.TimeSent <= lostSendTime)
                {
                    trigger = PacketLossTrigger.TimeThreshold;
                }
                else if (largestAcked >= packet.PacketNumber + PacketReorderingThreshold)
                {
                    trigger = PacketLossTrigger.ReorderingThreshold;
                }

                if (trigger != null)
                {
                    // Mark packet as lost
                    pnSpace.LostPackets.Enqueue((packet, trigger.Value));

                    if (packet.InFlight)
                    {
                        newlyLost[newlyLostCount++] = packet;
                    }
                }
                else
                {
                    // set time when the packet should be marked lost
                    pnSpace.NextLossTime = packet.TimeSent + lossDelay;

                    // we can stop now, since all following packets were sent afterwards.
                    break;
                }
            }
            pnSpace.SentPackets.RemoveRange(0, removed);

            // Inform the congestion controller of lost packets.
            var newlyLostAsSpan = newlyLost.AsSpan(0, newlyLostCount);
            CongestionController.OnPacketsLost(this, newlyLostAsSpan, now);

            // clear to avoid having references to packets from pooled array
            newlyLostAsSpan.Clear();
            ArrayPool<SentPacket>.Shared.Return(newlyLost);
        }

        /// <summary>
        ///     Drops all unacked data from given packet space.
        /// </summary>
        /// <param name="space">Packet space to drop.</param>
        /// <param name="isHandshakeComplete">True if handshake is complete</param>
        /// <param name="sentPacketPool">Object pool to which instances of <see cref="SentPacket"/> should be returned.</param>
        internal void DropUnackedData(PacketSpace space, bool isHandshakeComplete,
            ObjectPool<SentPacket> sentPacketPool)
        {
            var pnSpace = GetPacketNumberSpace(space);

            // remove sent bytes from congestion window
            for (int i = 0; i < pnSpace.SentPackets.Count; i++)
            {
                if (pnSpace.SentPackets[i].InFlight)
                {
                    BytesInFlight -= pnSpace.SentPackets[i].BytesSent;
                }

                sentPacketPool.Return(pnSpace.SentPackets[i]);
            }

            while (pnSpace.AckedPackets.TryDequeue(out var packet))
            {
                sentPacketPool.Return(packet);
            }

            while (pnSpace.LostPackets.TryDequeue(out var i))
            {
                sentPacketPool.Return(i.packet);
            }

            pnSpace.Reset();
            SetLossDetectionTimer(isHandshakeComplete);
        }
    }
}
