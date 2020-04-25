using System.Collections.Generic;
using System.Linq;
using System.Net.Quic.Implementations.Managed;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Tests.Harness;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Quic.Tests
{
    public class HandshakeTests
    {
        private readonly ITestOutputHelper output;

        private readonly QuicClientConnectionOptions _clientOpts;
        private readonly QuicListenerOptions _serverOpts;
        private readonly ManagedQuicConnection _client;
        private readonly ManagedQuicConnection _server;

        private readonly TestHarness _harness;

        private static readonly IPEndPoint IpEndPoint = new IPEndPoint(IPAddress.Any, 1010);

        public HandshakeTests(ITestOutputHelper output)
        {
            this.output = output;
            // this is safe as tests within the same class will not run parallel to each other.
            Console.SetOut(new XUnitTextWriter(output));
            _clientOpts = new QuicClientConnectionOptions();
            _serverOpts = new QuicListenerOptions
            {
                CertificateFilePath = TestHarness.CertificateFilePath,
                PrivateKeyFilePath = TestHarness.PrivateKeyFilePath
            };
            _client = TestHarness.CreateClient(_clientOpts);
            _server = TestHarness.CreateServer(_serverOpts);

            _harness = new TestHarness(output, _client);
        }

        [Fact]
        public void SendsConnectionCloseOnSmallClientInitial()
        {
            var flight = _harness.GetFlightToSend(_client);

            // remove all padding, leading to a very short client initial packet.
            var initial = Assert.IsType<InitialPacket>(flight.Packets[0]);
            initial.Frames.RemoveAll(f => f.FrameType == FrameType.Padding);
            _harness.SendFlight(_client, _server, flight.Packets);

            var response = _harness.SendFlight(_server, _client);
            initial = Assert.IsType<InitialPacket>(response.Packets[0]);

            // there should be a single frame only
            var closeFrame = Assert.IsType<ConnectionCloseFrame>(Assert.Single(initial.Frames));

            Assert.Equal(TransportErrorCode.ProtocolViolation, closeFrame.ErrorCode);
            Assert.True(closeFrame.IsQuicError);
            Assert.Equal(FrameType.Padding, closeFrame.ErrorFrameType); // 0x00
            Assert.Equal(QuicError.InitialPacketTooShort, closeFrame.ReasonPhrase);
        }

        [Fact]
        public void SendsProbeTimeoutIfInitialLost()
        {
            // first packet lost
            _harness.GetInitialToSend(_client);
            Assert.NotEqual(long.MaxValue, _client.GetNextTimerTimestamp());

            _harness.Timestamp = _client.GetNextTimerTimestamp();
            var flight = _harness.GetFlightToSend(_client);
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
            {
                var flight = _harness.SendFlight(_client, _server);
                Assert.Equal(QuicConstants.MinimumClientInitialDatagramSize, flight.UdpDatagramSize);

                // client: single initial packet
                Assert.Single(flight.Packets);
                var initialPacket = Assert.IsType<InitialPacket>(flight.Packets[0]);
                Assert.Equal(0u, initialPacket.PacketNumber);
                Assert.IsType<CryptoFrame>(initialPacket.Frames[0]);
                // only other frames allowed in first client initial are padding
                Assert.Empty(initialPacket.Frames.Skip(1).Where(f => f.FrameType != FrameType.Padding));
            }

            // server:
            //                                  Initial[0]: CRYPTO[SH] ACK[0]
            //                     <- Handshake[0]: CRYPTO[EE, CERT, CV, FIN]
            {
                var flight = _harness.SendFlight(_server, _client);
                Assert.Equal(2, flight.Packets.Count);

                var initialPacket = Assert.IsType<InitialPacket>(flight.Packets[0]);
                Assert.Equal(0u, initialPacket.PacketNumber);
                Assert.Equal(2, initialPacket.Frames.Count);
                Assert.Single(initialPacket.Frames.OfType<CryptoFrame>());
                var ackFrame = Assert.Single(initialPacket.Frames.OfType<AckFrame>());
                Assert.Equal(0u, ackFrame.LargestAcknowledged);
                Assert.Equal(0u, ackFrame.FirstAckRange);
                Assert.Empty(ackFrame.AckRanges);

                var handshakePacket = Assert.IsType<HandShakePacket>(flight.Packets[1]);
                Assert.Equal(0u, handshakePacket.PacketNumber);
                Assert.True(handshakePacket.Frames.All(f => f.FrameType == FrameType.Crypto));
            }

            // client:
            // Initial[1]: ACK[0]
            // Handshake[0]: CRYPTO[FIN], ACK[0] ->
            {
                var flight = _harness.SendFlight(_client, _server);
                Assert.Equal(2, flight.Packets.Count);

                Assert.True(QuicConstants.MinimumClientInitialDatagramSize <= flight.UdpDatagramSize);

                var initialPacket = Assert.IsType<InitialPacket>(flight.Packets[0]);
                Assert.Equal(1u, initialPacket.PacketNumber);

                // no padding frames in this packet
                var ackFrame = Assert.Single(initialPacket.Frames.OfType<AckFrame>());
                Assert.Equal(0u, ackFrame.LargestAcknowledged);
                Assert.Equal(0u, ackFrame.FirstAckRange);
                Assert.Empty(ackFrame.AckRanges);

                var handshakePacket = Assert.IsType<HandShakePacket>(flight.Packets[1]);
                Assert.Equal(0u, handshakePacket.PacketNumber);
                Assert.Single(handshakePacket.Frames.OfType<CryptoFrame>());
                ackFrame = Assert.Single(handshakePacket.Frames.OfType<AckFrame>());
                Assert.Equal(0u, ackFrame.LargestAcknowledged);
                Assert.Equal(0u, ackFrame.FirstAckRange);
                Assert.Empty(ackFrame.AckRanges);

                // after this, the TLS handshake should be complete, but not yet confirmed on the client
                Assert.True(_server.Connected);
                Assert.False(_client.Connected);
            }

            // server:
            //                                           Handshake[1]: ACK[0]
            //                                    <- OneRtt[0]: HandshakeDone
            {
                var flight = _harness.SendFlight(_server, _client);
                var handshakePacket = Assert.IsType<HandShakePacket>(flight.Packets[0]);

                Assert.Equal(1u, handshakePacket.PacketNumber);
                var ackFrame = Assert.IsType<AckFrame>(Assert.Single(handshakePacket.Frames.Where(f => f.FrameType != FrameType.Padding)));
                Assert.Equal(0u, ackFrame.LargestAcknowledged);
                Assert.Equal(0u, ackFrame.FirstAckRange);
                Assert.Empty(ackFrame.AckRanges);

                var oneRtt = Assert.IsType<OneRttPacket>(flight.Packets[1]);
                Assert.Contains(oneRtt.Frames, f => f.FrameType == FrameType.HandshakeDone);

                // handshake confirmed on the client
                Assert.True(_client.Connected);
            }
        }
    }
}
