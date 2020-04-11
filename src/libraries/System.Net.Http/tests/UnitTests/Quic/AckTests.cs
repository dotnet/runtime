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
            _client = new ManagedQuicConnection(_clientOpts);
            _server = new ManagedQuicConnection(_serverOpts, TestHarness.DummySocketContet, TestHarness.IpAnyEndpoint);

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

    }
}
