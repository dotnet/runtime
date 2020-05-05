using System.Net.Quic.Implementations.Managed;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Tests.Harness;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Quic.Tests
{
    public class NewConnectionIdFrameTests
    {
        private readonly ManagedQuicConnection _client;
        private readonly ManagedQuicConnection _server;

        private readonly TestHarness _harness;

        public NewConnectionIdFrameTests(ITestOutputHelper output)
        {
            (_client, _server, _harness) = TestHarness.InitConnection(output);
            _harness.EstablishConnection(_client, _server);
        }

        private void SendNewConnectionIdFrame(ManagedQuicConnection source, ManagedQuicConnection destination,
            byte[] cid, long sequenceNumber, StatelessResetToken token)
        {
            source.Ping(); // ensure a frame is indeed sent
            _harness.Intercept1Rtt(source, destination, packet =>
            {
                packet.Frames.Add(new NewConnectionIdFrame()
                {
                    ConnectionId = cid,
                    SequenceNumber = sequenceNumber,
                    RetirePriorTo = 0,
                    StatelessResetToken = token
                });
            });
        }

        [Fact]
        public void ClosesConnectionWhenChangingSequenceNumber()
        {
            byte[] cid = {1, 2, 3, 4, 5, 6, 6, 8};
            StatelessResetToken token = StatelessResetToken.Random();
            SendNewConnectionIdFrame(_server, _client, cid, 1, token);

            // everything okay still
            _harness.Send1Rtt(_client, _server).ShouldNotHaveFrame<ConnectionCloseFrame>();

            // send same connection id with different sequence number
            SendNewConnectionIdFrame(_server, _client, cid, 2, token);

            // now we are in trouble
            _harness.Send1Rtt(_client, _server).ShouldContainConnectionClose(
                TransportErrorCode.ProtocolViolation,
                QuicError.InconsistentNewConnectionIdFrame,
                FrameType.NewConnectionId);
        }

        [Fact]
        public void ClosesConnectionWhenChangingStatelessResetToken()
        {
            byte[] cid = {1, 2, 3, 4, 5, 6, 6, 8};
            StatelessResetToken token = StatelessResetToken.Random();
            SendNewConnectionIdFrame(_server, _client, cid, 1, token);

            // everything okay still
            _harness.Send1Rtt(_client, _server).ShouldNotHaveFrame<ConnectionCloseFrame>();

            // send same connection id with different stateless reset token
            SendNewConnectionIdFrame(_server, _client, cid, 1, StatelessResetToken.Random());

            // now we are in trouble
            _harness.Send1Rtt(_client, _server).ShouldContainConnectionClose(
                TransportErrorCode.ProtocolViolation,
                QuicError.InconsistentNewConnectionIdFrame,
                FrameType.NewConnectionId);
        }
    }
}
