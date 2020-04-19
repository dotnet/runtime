using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Quic.Implementations.Managed.Internal.Frames;
using System.Net.Quic.Implementations.Managed.Internal.Recovery;
using System.Reflection;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    /// <summary>
    ///     Class encapsulating logic on packet loss recovery.
    /// </summary>
    internal class RecoveryController
    {
        public RecoveryController()
        {
            CongestionController = new NewRenoCongestionController();
            Reset();
        }

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
        internal static readonly long TimerGranularity = Timestamp.FromMilliseconds(15);

        /// <summary>
        ///     The RTT used before an RTT sample is taken in ticks. the RECOMMENDED value is 500ms.
        /// </summary>
        internal static readonly long InitialRtt = Timestamp.FromMilliseconds(500);

        /// <summary>
        ///     Helper structure for holding packet number space related data together.
        /// </summary>
        internal class PacketNumberSpace
        {
            internal static readonly Comparer<PacketNumberSpace> LossTimeComparer = Comparer<PacketNumberSpace>.Create(
                (l, r) => l.NextLossTime.CompareTo(r.NextLossTime));

            internal static readonly Comparer<PacketNumberSpace> TimeOfLastAckElicitingPacketSentComparer = Comparer<PacketNumberSpace>.Create((l, r) =>
                l.TimeOfLastAckElicitingPacketSent.CompareTo(r.TimeOfLastAckElicitingPacketSent));

            /// <summary>
            ///     The largest packet number sent by this endpoint which was acknowledged in the packet number space so far.
            /// </summary>
            internal long LargestAckedPacketNumber { get; set; }

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

            /// <summary>
            ///     If PTO timer expired, contains number of remaining probe packets this endpoint should send as a
            ///     reaction to the timeout. Otherwise 0.
            /// </summary>
            internal int RemainingLossProbes { get; set; }
        }

        private readonly PacketNumberSpace[] _pnSpaces = new PacketNumberSpace[]
        {
            new PacketNumberSpace(), new PacketNumberSpace(), new PacketNumberSpace()
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
            // for (int i = 0; i < _pnSpaces.Length; i++)
            // {
                // if (_pnSpaces[i].RemainingLossProbes > 0)
                    // return int.MaxValue;
            // }

            return Math.Max(0, CongestionController.CongestionWindow - AckElicitingBytesInFlight);
        }

        /// <summary>
        ///     The sum of the size in bytes of all sent packets that contain at least one ack-eliciting or PADDING
        ///     frame, and have not been acked or declared lost. The size does not include IP or UDP overhead, but does
        ///     Include the QUIC header and AEAD overhead. Packets only containing ACK frames do not count towards this
        ///     count to ensure congestion control does not impede congestion feedback.
        /// </summary>
        private int AckElicitingBytesInFlight => CongestionController.BytesInFlight;


        /// <summary>
        ///     Resets the recovery controller to the initial state.
        /// </summary>
        void Reset()
        {
            CongestionController.Reset();

            PtoCount = 0;
            LatestRtt = 0;
            SmoothedRtt = 0;
            MinimumRtt = 0;
            LossRecoveryTimer = long.MaxValue;
            MaxAckDelay = Timestamp.FromMilliseconds(25);

            foreach (PacketNumberSpace space in _pnSpaces)
            {
                space.RemainingLossProbes = 0;
                space.AckedPackets.Clear();
                space.LostPackets.Clear();
                space.SentPackets.Clear();
                space.NextLossTime = long.MaxValue;
                space.LargestAckedPacketNumber = 0;
                space.TimeOfLastAckElicitingPacketSent = long.MaxValue;
            }
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

                CongestionController.OnPacketSent(packet);

                // Note that we do not set NextLossTime because we need to receive an ack for later packet in order
                // to deem this packet lost. The NextLossTime has to be set only when receiving ack or during the
                // loss timer processing.
                SetLossDetectionTimer(handshakeComplete);
            }
        }

        internal void OnAckReceived(PacketSpace space, ReadOnlySpan<PacketNumberRange> ranges, long ackDelay,
            in AckFrame frame, long now, bool isHandshakeComplete)
        {
            Debug.Assert(!ranges.IsEmpty);

            long largestAcked = ranges[^1].End;
            var pnSpace = GetPacketNumberSpace(space);

            pnSpace.LargestAckedPacketNumber = Math.Max(pnSpace.LargestAckedPacketNumber, largestAcked);

            bool isLargestAcknowledgedNewlyAcked = pnSpace.SentPackets.TryGetValue(largestAcked, out var largestAckedPacket);
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

        private bool ProcessNewlyAckedPackets(ReadOnlySpan<PacketNumberRange> ranges, PacketNumberSpace pnSpace,
            long now)
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

                OnPacketAcked(pn, packet, pnSpace, now);
                pnSpace.SentPackets.Remove(pn);
            }

            return newlyAckedIncludeAckEliciting;
        }

        private void OnPacketAcked(long packetNumber, SentPacket packet, PacketNumberSpace pnSpace, long now)
        {
            if (packet.InFlight)
            {
                CongestionController.OnPacketAcked(packet, now);
            }

            pnSpace.SentPackets.Remove(packetNumber);
            pnSpace.AckedPackets.Add(packet);
        }

        internal void UpdateRtt(long ackDelay)
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
            long adjustedRttTicks = LatestRtt;
            if (adjustedRttTicks > MinimumRtt + ackDelay)
                adjustedRttTicks = LatestRtt - ackDelay;

            RttVariation = (long)(3.0 / 4 * RttVariation +
                                  1.0 / 4 * Math.Abs(SmoothedRtt - adjustedRttTicks));
        }

        private PacketNumberSpace GetSpace(bool handshakeComplete, Comparer<PacketNumberSpace> comparer)
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
            // TODO-RZ: return (has received Handshake ACK || has received 1-RTT ACK)
            return true;
        }

        private void SetLossDetectionTimer(bool isHandshakeComplete)
        {
            var earliestLossTime = GetSpace(isHandshakeComplete, PacketNumberSpace.LossTimeComparer).NextLossTime;

            if (earliestLossTime != long.MaxValue)
            {
                // Time threshold loss detection.
                LossRecoveryTimer = earliestLossTime;
                return;
            }

            if (AckElicitingBytesInFlight == 0 && // no ack-eliciting packets in flight
                PeerNotAwaitingAddressValidation())
            {
                // cancel the timer
                LossRecoveryTimer = long.MaxValue;
                return;
            }

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

            timeout *= 1 << PtoCount;

            var pnSpace = GetSpace(isHandshakeComplete, PacketNumberSpace.TimeOfLastAckElicitingPacketSentComparer);
            LossRecoveryTimer = pnSpace.TimeOfLastAckElicitingPacketSent + timeout;
        }

        internal void OnLossDetectionTimeout(bool isHandshakeComplete, long now)
        {
            var pnSpace = GetSpace(isHandshakeComplete, PacketNumberSpace.LossTimeComparer);
            long earliestLossTime = pnSpace.NextLossTime;

            if (earliestLossTime != long.MaxValue)
            {
                // Time threshold loss detection
                DetectLostPackets(pnSpace, now);
                SetLossDetectionTimer(isHandshakeComplete);
                return;
            }

            pnSpace.RemainingLossProbes = 2;
            // TODO-RZ: Move the code handling these cases to ManagedQuicConnection
            // if (!isServer && GetPacketNumberSpace(EncryptionLevel.Application).RecvCryptoSeal == null)
            // {
                // TODO-RZ: Client needs to send an anti-deadlock packet:
                // throw new NotImplementedException();
            // }
            // else
            // {
                // TODO-RZ: PTO. Send new data if available, else retransmit old data.
                // If neither is available, send single PING frame.
                // throw new NotImplementedException();
            // }

            PtoCount++;
            SetLossDetectionTimer(isHandshakeComplete);
        }

        private void DetectLostPackets(PacketNumberSpace pnSpace, long now)
        {
            // will be set again later, if necessary
            pnSpace.NextLossTime = long.MaxValue;

            // lost packets to be passed to congestion controller (with InFlight = true)
            var lostPacketsForCc = new List<SentPacket>();

            long lossDelay = (long)(TimeReorderingThreshold * Math.Max(LatestRtt, SmoothedRtt));

            // minimum time based on timer granularity before packets are deemded lost.
            lossDelay = Math.Max(lossDelay, TimerGranularity);

            long lostSendTime = now - lossDelay;
            long largestAcked = pnSpace.LargestAckedPacketNumber;

            // make a copy before iterating because we are going to remove items from the collection
            foreach ((long pn, var packet) in pnSpace.SentPackets.ToArray())
            {
                if (pn > largestAcked)
                    continue;

                if (packet.TimeSent <= lostSendTime ||
                    largestAcked >= pn + PacketReorderingThreshold)
                {
                    // Mark packet as lost
                    pnSpace.SentPackets.Remove(pn);
                    pnSpace.LostPackets.Add(packet);

                    if (packet.InFlight)
                    {
                        lostPacketsForCc.Add(packet);
                    }
                }
                else
                {
                    // set time when the packet should be marked lost
                    pnSpace.NextLossTime = Math.Min(pnSpace.NextLossTime, packet.TimeSent + lossDelay);
                }
            }

            // Inform the congestion controller of lost packets.
            CongestionController.OnPacketsLost(lostPacketsForCc, now);
        }

        internal List<SentPacket> GetAckedPackets(PacketSpace space)
        {
            return GetPacketNumberSpace(space).AckedPackets;
        }

        internal List<SentPacket> GetLostPackets(PacketSpace space)
        {
            return GetPacketNumberSpace(space).LostPackets;
        }
    }
}
