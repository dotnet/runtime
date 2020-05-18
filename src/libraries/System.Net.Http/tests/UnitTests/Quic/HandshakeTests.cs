using System.Collections.Generic;
using System.Linq;
using System.Net.Quic.Implementations.Managed;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Tests.Harness;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Quic.Tests
{
    public class HandshakeTests : ManualTransmissionQuicTestBase
    {
        public HandshakeTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact]
        public void SendsConnectionCloseOnSmallClientInitial()
        {
            InterceptFlight(Client, Server, flight =>
            {
                // remove all padding, leading to a very short client initial packet.
                var initial = Assert.IsType<InitialPacket>(flight.Packets[0]);
                initial.Frames.RemoveAll(f => f.FrameType == FrameType.Padding);
            });

            // response should contain only initial packet
            InterceptInitial(Server, Client, initial =>
            {
                Assert.Single(initial.Frames);

                initial.ShouldHaveConnectionClose(
                    TransportErrorCode.ProtocolViolation,
                    QuicError.InitialPacketTooShort);
            });
        }

        [Fact]
        public void SendsProbeTimeoutIfInitialLost()
        {
            // first packet lost
            GetInitialToSend(Client);
            Assert.NotEqual(long.MaxValue, Client.GetNextTimerTimestamp());

            CurrentTimestamp = Client.GetNextTimerTimestamp();
            Client.OnTimeout(CurrentTimestamp);
            var flight = GetFlightToSend(Client);
            var packet = Assert.IsType<InitialPacket>(Assert.Single(flight.Packets));

            // it is still initial packet, so minimum size applies
            Assert.Equal(QuicConstants.MinimumClientInitialDatagramSize, flight.UdpDatagramSize);
            packet.ShouldHaveFrame<PingFrame>();
        }

        [Fact]
        public void SimpleSuccessfulConnectionTest()
        {
            // Expected handshake pattern
            // Client                                                  Server
            //
            // Initial[0]: CRYPTO[CH] ->
            //
            //                                  Initial[0]: CRYPTO[SH] ACK[0]
            //                     <- Handshake[0]: CRYPTO[EE, CERT, CV, FIN]
            //
            // Initial[1]: ACK[0]
            // Handshake[0]: CRYPTO[FIN], ACK[0] ->
            //
            //                                           Handshake[1]: ACK[0]
            //                                    <- OneRtt[0]: HandshakeDone

            // client:
            // Initial[0]: CRYPTO[CH] ->
            InterceptFlight(Client, Server, flight =>
            {
                Assert.Equal(QuicConstants.MinimumClientInitialDatagramSize, flight.UdpDatagramSize);

                // client: single initial packet
                Assert.Single(flight.Packets);
                var initialPacket = Assert.IsType<InitialPacket>(flight.Packets[0]);
                Assert.Equal(0u, initialPacket.PacketNumber);
                initialPacket.ShouldHaveFrame<CryptoFrame>();

                // crypto frame is first, the only other frames in the first client initial are padding
                Assert.Empty(initialPacket.Frames.Skip(1).Where(f => f.FrameType != FrameType.Padding));
            });

            // server:
            //                                  Initial[0]: CRYPTO[SH] ACK[0]
            //                     <- Handshake[0]: CRYPTO[EE, CERT, CV, FIN]
            InterceptFlight(Server, Client, flight =>
            {
                Assert.Equal(2, flight.Packets.Count);

                var initialPacket = Assert.IsType<InitialPacket>(flight.Packets[0]);
                Assert.Equal(0u, initialPacket.PacketNumber);
                Assert.Equal(2, initialPacket.Frames.Count);

                initialPacket.ShouldHaveFrame<CryptoFrame>();

                var ackFrame = initialPacket.ShouldHaveFrame<AckFrame>();
                Assert.Equal(0u, ackFrame.LargestAcknowledged);
                Assert.Equal(0u, ackFrame.FirstAckRange);
                Assert.Empty(ackFrame.AckRanges);

                var handshakePacket = Assert.IsType<HandShakePacket>(flight.Packets[1]);
                Assert.Equal(0u, handshakePacket.PacketNumber);
                Assert.True(handshakePacket.Frames.All(f => f.FrameType == FrameType.Crypto));
            });

            // client:
            // Initial[1]: ACK[0]
            // Handshake[0]: CRYPTO[FIN], ACK[0] ->
            InterceptFlight(Client, Server, flight =>
            {
                Assert.Equal(2, flight.Packets.Count);

                Assert.True(QuicConstants.MinimumClientInitialDatagramSize <= flight.UdpDatagramSize);

                var initialPacket = Assert.IsType<InitialPacket>(flight.Packets[0]);
                Assert.Equal(1u, initialPacket.PacketNumber);

                // no padding frames in this packet
                var ackFrame = initialPacket.ShouldHaveFrame<AckFrame>();
                Assert.Equal(0u, ackFrame.LargestAcknowledged);
                Assert.Equal(0u, ackFrame.FirstAckRange);
                Assert.Empty(ackFrame.AckRanges);

                var handshakePacket = Assert.IsType<HandShakePacket>(flight.Packets[1]);
                Assert.Equal(0u, handshakePacket.PacketNumber);

                handshakePacket.ShouldHaveFrame<CryptoFrame>();

                ackFrame = handshakePacket.ShouldHaveFrame<AckFrame>();
                Assert.Equal(0u, ackFrame.LargestAcknowledged);
                Assert.Equal(0u, ackFrame.FirstAckRange);
                Assert.Empty(ackFrame.AckRanges);
            });

            // after this, the TLS handshake should be complete, but not yet confirmed on the client
            Assert.True(Server.Connected);
            Assert.False(Client.Connected);

            // server:
            //                                           Handshake[1]: ACK[0]
            //                                    <- OneRtt[0]: HandshakeDone
            InterceptFlight(Server, Client, flight =>
            {
                var handshakePacket = Assert.IsType<HandShakePacket>(flight.Packets[0]);

                Assert.Equal(1u, handshakePacket.PacketNumber);
                var ackFrame =
                    Assert.IsType<AckFrame>(
                        Assert.Single(handshakePacket.Frames.Where(f => f.FrameType != FrameType.Padding)));
                Assert.Equal(0u, ackFrame.LargestAcknowledged);
                Assert.Equal(0u, ackFrame.FirstAckRange);
                Assert.Empty(ackFrame.AckRanges);

                var oneRtt = Assert.IsType<OneRttPacket>(flight.Packets[1]);
                Assert.Contains(oneRtt.Frames, f => f.FrameType == FrameType.HandshakeDone);
            });

            // handshake is now confirmed on the client
            Assert.True(Client.Connected);
        }

        [Fact]
        public void SendsConnectionCloseWhenServerSendsToken()
        {
            SendFlight(Client, Server);
            InterceptFlight(Server, Client, flight =>
            {
                Assert.IsType<InitialPacket>(flight.Packets[0]).Token = new byte[] {1, 2, 3, 4};
            });

            var packet = (CommonPacket)SendFlight(Client, Server).Packets[0];
            packet.ShouldHaveConnectionClose(
                TransportErrorCode.ProtocolViolation,
                QuicError.UnexpectedToken);
        }
    }
}
