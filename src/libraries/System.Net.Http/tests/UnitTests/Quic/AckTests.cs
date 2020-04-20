#nullable enable

using System.Linq;
using System.Net.Quic.Implementations.Managed;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Tests.Harness;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Quic.Tests
{
    public class AckTests
    {
        private readonly QuicClientConnectionOptions _clientOpts;
        private readonly QuicListenerOptions _serverOpts;
        private readonly ManagedQuicConnection _client;
        private readonly ManagedQuicConnection _server;

        private readonly TestHarness _harness;

        public AckTests(ITestOutputHelper output)
        {
            _clientOpts = new QuicClientConnectionOptions();
            _serverOpts = new QuicListenerOptions
            {
                CertificateFilePath = TestHarness.CertificateFilePath,
                PrivateKeyFilePath = TestHarness.PrivateKeyFilePath
            };
            _client = TestHarness.CreateClient(_clientOpts);
            _server = TestHarness.CreateServer(_serverOpts);

            _harness = new TestHarness(output, _client);

            _harness.EstablishConnection(_client, _server);
        }

        [Fact]
        public void ConnectionCloseWhenAckingFuturePacket()
        {
            _client.Ping();
            var packet = _harness.Get1RttToSend(_client);
            var ack = packet.ShouldHaveFrame<AckFrame>();

            // ack one more than intended
            ack.LargestAcknowledged++;
            ack.FirstAckRange++;
            _harness.SendPacket(_client, _server, packet);

            _harness.Send1Rtt(_server, _client)
                .ShouldContainConnectionClose(TransportErrorCode.ProtocolViolation,
                    QuicError.InvalidAckRange,
                    FrameType.Ack);
        }

        [Fact]
        public void ConnectionCloseWhenAckingNegativePacket()
        {
            _client.Ping();
            var packet = _harness.Get1RttToSend(_client);
            var ack = packet.ShouldHaveFrame<AckFrame>();

            // ack one more than intended
            ack.FirstAckRange++;
            _harness.SendPacket(_client, _server, packet);

            _harness.Send1Rtt(_server, _client)
                .ShouldContainConnectionClose(TransportErrorCode.ProtocolViolation,
                    QuicError.InvalidAckRange,
                    FrameType.Ack);
        }

        [Fact]
        public void TestNotAckingPastFrames()
        {
            // TODO-RZ: use natural timeout instead of forced ping
            // since PING frames are ack-eliciting, the endpoint should always send an ack frame, leading to each endpoint always acking only the last received packet.
            var sender = _client;
            var receiver = _server;
            for (int i = 0; i < 3; i++)
            {
                sender.Ping();
                var flight = _harness.SendFlight(sender, receiver);
                var packet = Assert.IsType<OneRttPacket>(flight.Packets[0]);
                var ack = Assert.Single(packet.Frames.OfType<AckFrame>());

                Assert.Equal(0u, ack.FirstAckRange);
                Assert.Empty(ack.AckRanges);

                var tmp = sender;
                sender = receiver;
                receiver = tmp;
            }
        }

        [Fact]
        public void TestAckNonContiguousRanges()
        {
            // make sure the end has enough consecutive PNs to guarantee that earlier packets are determined lost
            var received = new [] {2, 3, 4, 7, 8, 10, 13, 14, 15, 16};
            var lost = Enumerable.Range(0, 17).Except(received).ToArray();

            long last = received[^1];

            // we enforce sending packets by writing one byte, coincidentally containing the value of expected packet
            // number for better testing
            var clientStream = _client.OpenUnidirectionalStream();

            for (int i = 0; i <= last; i++)
            {
                // enforce sending a packet
                clientStream.Write(new[]{(byte) i});
                PacketBase packet = _harness.Get1RttToSend(_client);

                // make sure our testing strategy works
                Assert.Equal(i, packet.PacketNumber);

                if (received.Contains((int) packet.PacketNumber))
                {
                    // let the packet be received
                    _harness.SendPacket(_client, _server, packet);
                }
                // else drop the packet
            }

            // send ack back to server
            var frame = _harness.Send1Rtt(_server, _client).ShouldHaveFrame<AckFrame>();

            Assert.Equal(last, frame.LargestAcknowledged);
            Assert.Equal(3, frame.FirstAckRange);

            Assert.Equal(new[]
            {
                // remember that all numbers are encoded as 1 lesser, and encoding starts from the largest
                new AckFrame.AckRange { Acked = 0, Gap = 1 }, // 10
                new AckFrame.AckRange { Acked = 1, Gap = 0 }, // 7, 8
                new AckFrame.AckRange { Acked = 2, Gap = 1 }, // 2, 3, 4
            }, frame.AckRanges);

            // the data should be resent by now
            var resent = _harness.Get1RttToSend(_client).Frames
                .OfType<StreamFrame>().SelectMany(f => f.StreamData.Select(i => (int) i));

            Assert.Equal(lost, resent);
        }
    }
}
