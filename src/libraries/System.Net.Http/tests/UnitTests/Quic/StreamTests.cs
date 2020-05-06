using System.Diagnostics;
using System.Net.Quic.Implementations.Managed;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Tests.Harness;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Quic.Tests
{
    public class StreamTests : ManualTransmissionQuicTestBase
    {
        public StreamTests(ITestOutputHelper output)
            : base(output)
        {
            // all tests start after connection has been established
            EstablishConnection();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SimpleStreamOpen(bool unidirectional)
        {
            byte[] data = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0};
            var clientStream = Client.OpenStream(unidirectional);
            Assert.True(clientStream.CanWrite);
            Assert.Equal(!unidirectional, clientStream.CanRead);
            clientStream.Write(data);
            clientStream.Flush();

            Intercept1RttFrame<StreamFrame>(Client, Server, frame =>
            {
                Assert.Equal(clientStream.StreamId, frame.StreamId);
                Assert.Equal(0u, frame.Offset);
                Assert.Equal(data, frame.StreamData);
                Assert.False(frame.Fin);
            });

            var serverStream = Server.AcceptStream();
            Assert.NotNull(serverStream);
            Assert.Equal(clientStream.StreamId, serverStream.StreamId);
            Assert.True(serverStream.CanRead);
            Assert.Equal(!unidirectional, serverStream.CanWrite);

            var read = new byte[data.Length];
            int bytesRead = serverStream.Read(read);
            Assert.Equal(data.Length, bytesRead);
            Assert.Equal(data, read);
        }

        [Fact]
        public void SendsFinWithLastFrame()
        {
            byte[] data = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0};
            var clientStream = Client.OpenStream(true);
            clientStream.Write(data);
            clientStream.Flush();
            clientStream.Shutdown();

            Intercept1RttFrame<StreamFrame>(Client, Server, frame =>
            {
                Assert.True(frame.Fin);
            });
        }


        [Fact]
        public void SendsEmptyStreamFrameWithFin()
        {
            byte[] data = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0};
            var clientStream = Client.OpenStream(true);

            // send data before marking end of stream
            clientStream.Write(data);
            clientStream.Flush();
            Intercept1RttFrame<StreamFrame>(Client, Server, frame =>
            {
                Assert.False(frame.Fin);
            });

            // no more data to send, just the fin bit
            clientStream.Shutdown();
            Intercept1RttFrame<StreamFrame>(Client, Server, frame =>
            {
                Assert.Empty(frame.StreamData);
                Assert.True(frame.Fin);
            });
        }

        [Fact]
        public void ClosesConnectionWhenStreamLimitIsExceeded()
        {
            byte[] data = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0};
            var clientStream = Client.OpenStream(true);
            clientStream.Write(data);
            clientStream.Flush();
            Intercept1RttFrame<StreamFrame>(Client, Server, frame =>
            {
                // make sure the stream id is above bounds
                frame.StreamId += ListenerOptions.MaxUnidirectionalStreams << 2 + 4;
            });

            Send1Rtt(Server, Client).ShouldHaveConnectionClose(
                TransportErrorCode.StreamLimitError,
                QuicError.StreamsLimitViolated,
                FrameType.Stream | FrameType.StreamLenBit);
        }

        [Fact]
        public void ClosesConnectionWhenSendingPastMaxRepresentableOffset()
        {
            byte[] data = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0};
            var clientStream = Client.OpenStream(true);
            clientStream.Write(data);
            clientStream.Flush();
            Intercept1RttFrame<StreamFrame>(Client, Server,
                frame => { frame.Offset = StreamHelpers.MaxStreamOffset; });

            Send1Rtt(Server, Client).ShouldHaveConnectionClose(
                TransportErrorCode.FrameEncodingError,
                QuicError.UnableToDeserialize,
                FrameType.Stream | FrameType.StreamLenBit | FrameType.StreamOffBit);
        }

        [Fact]
        public void ClosesConnectionWhenSendingPastFin()
        {
            byte[] data = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0};
            var clientStream = Client.OpenStream(true);
            clientStream.Write(data);
            clientStream.Flush();
            Intercept1RttFrame<StreamFrame>(Client, Server,
                frame => { frame.Offset = StreamHelpers.MaxStreamOffset; });

            Send1Rtt(Server, Client).ShouldHaveConnectionClose(
                TransportErrorCode.FrameEncodingError,
                QuicError.UnableToDeserialize,
                 FrameType.Stream | FrameType.StreamLenBit | FrameType.StreamOffBit);
        }

        [Fact]
        public void ClosesConnectionWhenSendingInNonReadableStream()
        {
            byte[] data = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0};
            var clientStream = Client.OpenStream(true);
            clientStream.Write(data);
            clientStream.Flush();
            Intercept1RttFrame<StreamFrame>(Client, Server, frame =>
            {
                // use the only type of stream into which client cannot send
                frame.StreamId = StreamHelpers.ComposeStreamId(StreamType.ServerInitiatedUnidirectional, 0);
            });

            Send1Rtt(Server, Client).ShouldHaveConnectionClose(
                TransportErrorCode.StreamStateError,
                QuicError.StreamNotWritable,
                FrameType.Stream | FrameType.StreamLenBit);
        }

        [Fact]
        public void ClosesConnectionWhenSendingPastStreamMaxData()
        {
            byte[] data = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0};
            var clientStream = Client.OpenStream(true);
            clientStream.Write(data);
            clientStream.Flush();
            Intercept1RttFrame<StreamFrame>(Client, Server,
                frame => { frame.Offset = TransportParameters.DefaultMaxStreamData - 1; });

            Send1Rtt(Server, Client).ShouldHaveConnectionClose(
                TransportErrorCode.FlowControlError,
                QuicError.StreamMaxDataViolated,
                FrameType.Stream | FrameType.StreamLenBit | FrameType.StreamOffBit);
        }

        [Fact]
        public void ClosesConnectionWhenSendingPastConnectionMaxData()
        {
            byte[] data = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0};
            var clientStream = Client.OpenStream(true);
            clientStream.Write(data);
            clientStream.Flush();
            Intercept1RttFrame<StreamFrame>(Client, Server,
                frame => { frame.Offset = TransportParameters.DefaultMaxData - 1; });

            Send1Rtt(Server, Client).ShouldHaveConnectionClose(
                TransportErrorCode.FlowControlError,
                QuicError.MaxDataViolated,
                 FrameType.Stream | FrameType.StreamLenBit | FrameType.StreamOffBit);
        }

        [Fact]
        public void ResendsDataAfterLoss()
        {
            byte[] data = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0};
            var clientStream = Client.OpenStream(true);
            clientStream.Write(data);
            clientStream.Flush();

            // lose the first packet with stream data
            Get1RttToSend(Client).ShouldHaveFrame<StreamFrame>();

            clientStream.Write(data);
            clientStream.Flush();
            CurrentTimestamp += RecoveryController.InitialRtt * 1;
            // deliver second packet with more data
            Send1RttWithFrame<StreamFrame>(Client, Server);

            // send ack back, leading the client to believe that first packet was lost
            CurrentTimestamp += RecoveryController.InitialRtt * 1;
            Send1Rtt(Server, Client).ShouldHaveFrame<AckFrame>();

            // resend original data
            var frame = Send1Rtt(Client, Server).ShouldHaveFrame<StreamFrame>();
            Assert.Equal(0, frame.Offset);
            Assert.Equal(data, frame.StreamData);
        }

        [Fact]
        public void ReceiverSendsMaxDataAfterReadingFromStream()
        {
            byte[] data = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0};
            byte[] recvBuf = new byte[data.Length];

            var senderStream = Client.OpenStream(true);
            senderStream.Write(data);
            senderStream.Flush();

            Send1Rtt(Client, Server);

            // read data
            var receiverStream = Server.AcceptStream();
            Assert.NotNull(receiverStream);
            int read = receiverStream.Read(recvBuf);
            Assert.Equal(recvBuf.Length, read);

            // next time, the receiver should send max data update
            var frame = Get1RttToSend(Server).ShouldHaveFrame<MaxStreamDataFrame>();
            Assert.Equal(senderStream.StreamId, frame.StreamId);
            Assert.Equal(senderStream.OutboundBuffer!.MaxData + data.Length, frame.MaximumStreamData);
        }

        [Fact]
        public void ClosesConnectionOnInvalidStreamId_StreamMaxDataFrame()
        {
            byte[] data = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0};
            byte[] recvBuf = new byte[data.Length];

            var senderStream = Client.OpenStream(true);
            senderStream.Write(data);
            senderStream.Flush();
            Send1Rtt(Client, Server);

            // read data
            var receiverStream = Server.AcceptStream();
            Assert.NotNull(receiverStream);
            receiverStream.Read(recvBuf);

            Intercept1RttFrame<MaxStreamDataFrame>(Server, Client, frame =>
            {
                // make sure the id above the client-specified limit
                frame.StreamId = ClientOptions.MaxUnidirectionalStreams * 4 + 1;
            });

            Send1Rtt(Client, Server)
                .ShouldHaveConnectionClose(TransportErrorCode.StreamLimitError,
                    QuicError.StreamsLimitViolated, FrameType.MaxStreamData);
        }
    }
}
