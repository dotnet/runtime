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

        private readonly byte[] buffer;
        private readonly QuicClientConnectionOptions _clientOpts;
        private readonly QuicListenerOptions _serverOpts;
        private readonly ManagedQuicConnection _client;
        private readonly ManagedQuicConnection _server;
        
        private static readonly IPEndPoint IpEndPoint = new IPEndPoint(IPAddress.Any, 1010);

        public HandshakeTests(ITestOutputHelper output)
        {
            buffer = new byte[16 * 1024];
            this.output = output;
            // this is safe as tests within the same class will not run parallel to each other.
            Console.SetOut(new XUnitTextWriter(output));
            _clientOpts = new QuicClientConnectionOptions();
            _serverOpts = new QuicListenerOptions()
            {
                CertificateFilePath = "Certs/cert.crt",
                PrivateKeyFilePath = "Certs/cert.key"
            };
            _client = new ManagedQuicConnection(_clientOpts);
            _server = new ManagedQuicConnection(_serverOpts);
        }

        private PacketFlight SendFlight(ManagedQuicConnection from, ManagedQuicConnection to)
        {
            // make a copy of the buffer, because decryption happens in-place
            var written = from.SendData(buffer, out _);
            var copy = buffer.AsSpan(0, written).ToArray();

            output.WriteLine(from == _client ? "\nClient:" : "\nServer:");
            var packets = PacketBase.ParseMany(copy, written, new TestHarness(from));
            foreach (var packet in packets)
            {
                output.WriteLine(packet.ToString());
            }

            to.ReceiveData(buffer, written, IpEndPoint);

            return new PacketFlight(packets, written);
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
            //                                        <- Handshake[1]: ACK[0]
            
            // client:
            // Initial[0]: CRYPTO[CH] ->
            {
                var flight = SendFlight(_client, _server);
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
                var flight = SendFlight(_server, _client);
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
                Assert.Equal(4, handshakePacket.Frames.Count);
                Assert.True(handshakePacket.Frames.All(f => f.FrameType == FrameType.Crypto));
            }
            
            // client:
            // Initial[1]: ACK[0]
            // Handshake[0]: CRYPTO[FIN], ACK[0] ->
            {
                var flight = SendFlight(_client, _server);
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
                
                // after this, the TLS handshake should be complete
                Assert.True(_server.Connected);
                Assert.True(_client.Connected);
            }

            // TODO-RZ HANDSHAKE_DONE frame?
            // server:
            //                                        <- Handshake[1]: ACK[0]
            {
                var flight = SendFlight(_server, _client);
                var handshakePacket = Assert.IsType<HandShakePacket>(Assert.Single(flight.Packets));
                
                Assert.Equal(1u, handshakePacket.PacketNumber);
                var ackFrame = Assert.IsType<AckFrame>(Assert.Single(handshakePacket.Frames));
                Assert.Equal(0u, ackFrame.LargestAcknowledged);
                Assert.Equal(0u, ackFrame.FirstAckRange);
                Assert.Empty(ackFrame.AckRanges);
            }
        }
    }
}