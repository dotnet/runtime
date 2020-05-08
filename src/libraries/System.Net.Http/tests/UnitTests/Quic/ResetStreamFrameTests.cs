using System.Net.Quic.Implementations.Managed;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Tests.Harness;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Quic.Tests
{
    public class ResetStreamFrameTests : ManualTransmissionQuicTestBase
    {
        public ResetStreamFrameTests(ITestOutputHelper output) : base(output)
        {
            EstablishConnection();
        }

        [Fact]
        public void CausesReadStreamOperationsToThrow()
        {
            var stream = Client.OpenStream(false);
            long errorCode = 15;

            Server.Ping();
            Intercept1Rtt(Server, Client, packet =>
            {
                packet.Frames.Add(new ResetStreamFrame()
                {
                    FinalSize = 0,
                    StreamId = stream.StreamId,
                    ApplicationErrorCode = errorCode
                });
            });

            var exception = Assert.Throws<QuicStreamAbortedException>(() => stream.Read(Span<byte>.Empty));
            Assert.Equal(errorCode, exception.ErrorCode);
        }

        private void CloseConnectionCommon(ResetStreamFrame frame, TransportErrorCode errorCode, string reason)
        {
            Server.Ping();
            Intercept1Rtt(Server, Client, packet => { packet.Frames.Add(frame); });

            Send1Rtt(Client, Server).ShouldHaveConnectionClose(
                errorCode,
                reason,
                FrameType.ResetStream);
        }

        [Fact]
        public void ClosesConnection_WhenReceivedForNonReadableStream()
        {
            CloseConnectionCommon(new ResetStreamFrame()
                {
                    StreamId = StreamHelpers.ComposeStreamId(StreamType.ClientInitiatedUnidirectional, 0),
                    ApplicationErrorCode = 14
                },
                TransportErrorCode.StreamStateError, QuicError.StreamNotReadable);
        }

        [Fact]
        public void ClosesConnection_WhenViolatingStreamLimit()
        {
            CloseConnectionCommon(new ResetStreamFrame()
                {
                    StreamId = StreamHelpers.ComposeStreamId(StreamType.ServerInitiatedUnidirectional, ListenerOptions.MaxBidirectionalStreams + 1),
                    ApplicationErrorCode = 14
                },
                TransportErrorCode.StreamLimitError, QuicError.StreamsLimitViolated);
        }
    }
}
