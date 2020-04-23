using System.Collections.Generic;
using System.Linq;
using System.Net.Quic.Implementations.Managed;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Frames;
using Xunit;

namespace System.Net.Quic.Tests
{
    public class RecoveryTests
    {
        private readonly RecoveryController Recovery = new RecoveryController();

        private long Now = Timestamp.Now;

        private int sentPackets = 0;
        private bool handshakeComplete = false;

        private SentPacket NewPacket(bool ackEliciting, bool inFlight, int bytesSent, long sent)
        {
            return new SentPacket
            {
                AckEliciting = ackEliciting,
                BytesSent = bytesSent,
                InFlight = inFlight,
                TimeSent = sent
            };
        }

        private void SendPacket(long now, PacketSpace space, bool ackEliciting, bool inFlight,
            int bytesSent)
        {
            var packet = NewPacket(ackEliciting, inFlight, bytesSent, now);
            Recovery.OnPacketSent(++sentPackets, space, packet, handshakeComplete);
        }

        private void ReceiveAck(long time, PacketSpace packetSpace, long ackDelay, params Range[] ranges) =>
            Recovery.OnAckReceived(packetSpace, ranges.Select(r => new PacketNumberRange(r.Start.Value, r.End.Value)).ToArray(), ackDelay,
                new AckFrame(), time, false);


        [Fact]
        public void SetsTimeoutAfterSendingFirstInitialPacket()
        {
            Assert.Equal(long.MaxValue, Recovery.LossRecoveryTimer); // no timer yet
            SendPacket(Now, PacketSpace.Initial, true, true, 1200);

            Assert.NotEqual(long.MaxValue, Recovery.LossRecoveryTimer);
            Assert.True(Recovery.LossRecoveryTimer - Now >= RecoveryController.TimerGranularity);
        }

        [Fact]
        public void MarksAckedPacketAsAcked()
        {
            SendPacket(Now, PacketSpace.Initial, true, true, 1200);

            // simulate receiving ack frame some time later
            ReceiveAck(Now + 2 * RecoveryController.InitialRtt, PacketSpace.Initial, Recovery.MaxAckDelay, 1..1);

            Assert.Single(Recovery.GetPacketNumberSpace(PacketSpace.Initial).AckedPackets);
        }

        [Fact]
        public void ClearTimerAfterReceivingAllPackets()
        {
            SendPacket(Now, PacketSpace.Initial, true, true, 1200);

            ReceiveAck(Now + 2 * RecoveryController.InitialRtt, PacketSpace.Initial, Recovery.MaxAckDelay, 1..1);

            Assert.Equal(long.MaxValue, Recovery.LossRecoveryTimer);
        }

        [Fact]
        public void PromptsProbePacketsAfterFirstPacketTimeout()
        {
            SendPacket(Now, PacketSpace.Initial, true, true, 1200);

            Recovery.OnLossDetectionTimeout(handshakeComplete, Recovery.LossRecoveryTimer);

            // packet should not be declared lost yet.
            var pnSpace = Recovery.GetPacketNumberSpace(PacketSpace.Initial);
            Assert.Empty(pnSpace.LostPackets);
            Assert.NotEqual(0, pnSpace.RemainingLossProbes);
        }

        [Fact]
        public void DeclaresPacketLostAfterDelay()
        {
            SendPacket(Now, PacketSpace.Initial, true, true, 1200);
            // timeout, send a probe
            Now = Recovery.LossRecoveryTimer;
            Recovery.OnLossDetectionTimeout(handshakeComplete, Now);
            SendPacket(Now, PacketSpace.Initial, true, true, 1200);

            // the second packet gets acked, since enough time has passed since sending the first one,
            // it should be considered lost
            Now += 2 * RecoveryController.InitialRtt;
            ReceiveAck(Now, PacketSpace.Initial, Recovery.MaxAckDelay, 2..2);

            // no more packets in-flight => no timer
            // Assert.Equal(long.MaxValue, Recovery.LossRecoveryTimer);
            Assert.Single(Recovery.GetPacketNumberSpace(PacketSpace.Initial).LostPackets);
            Assert.Single(Recovery.GetPacketNumberSpace(PacketSpace.Initial).AckedPackets);
        }

        [Fact]
        public void DeclaresPacketLostAfterReorderingWindow()
        {
            // send many packets in a burst
            int count = RecoveryController.PacketReorderingThreshold + 1;
            for (int i = 0; i < count; i++)
            {
                SendPacket(Now, PacketSpace.Initial, true, true, 1200);
            }

            // Receive ack only for the last
            Now += +2 * RecoveryController.InitialRtt;
            ReceiveAck(Now, PacketSpace.Initial, Recovery.MaxAckDelay, count..count);

            // only the first packet should be deemed lost
            Assert.Single(Recovery.GetPacketNumberSpace(PacketSpace.Initial).LostPackets);
            // but timer should be armed for the other packets.
            Assert.NotEqual(long.MaxValue, Recovery.LossRecoveryTimer);
        }

        [Fact]
        public void DeclaresPacketLostAfterTimeout()
        {
            // Send two packets, ack the latter, and timout the first
            SendPacket(Now, PacketSpace.Initial, true, true, 1200);
            SendPacket(Now, PacketSpace.Initial, true, true, 1200);

            Now += +2 * RecoveryController.InitialRtt;
            ReceiveAck(Now, PacketSpace.Initial, Recovery.MaxAckDelay, 2..2);

            // alarm should be armed for the timout on first packet
            Assert.NotEqual(long.MaxValue, Recovery.LossRecoveryTimer);
            Now = Recovery.LossRecoveryTimer;
            Recovery.OnLossDetectionTimeout(handshakeComplete, Now);

            // now the first should be lost
            Assert.Single(Recovery.GetPacketNumberSpace(PacketSpace.Initial).LostPackets);
        }
    }
}
