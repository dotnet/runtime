using System.Diagnostics;
using System.Net.Quic.Implementations.Managed;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Tests.Harness;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Quic.Tests
{
    public class StreamTests
    {
        private readonly QuicClientConnectionOptions _clientOpts;
        private readonly QuicListenerOptions _serverOpts;
        private readonly ManagedQuicConnection _client;
        private readonly ManagedQuicConnection _server;

        private readonly TestHarness _harness;

        public StreamTests(ITestOutputHelper output)
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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SimpleStreamOpen(bool unidirectional)
        {
            byte[] data = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0};
            var clientStream = _client.OpenStream(unidirectional);
            Assert.True(clientStream.CanWrite);
            Assert.Equal(!unidirectional, clientStream.CanRead);
            clientStream.Write(data);
            clientStream.Flush();

            var frame = _harness.Send1Rtt(_client, _server)
                .ShouldHaveFrame<StreamFrame>();

            Assert.Equal(clientStream.StreamId, frame.StreamId);
            Assert.Equal(0u, frame.Offset);
            Assert.Equal(data, frame.StreamData);
            Assert.False(frame.Fin);

            var serverStream = _server.AcceptStream();
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
            var clientStream = _client.OpenStream(true);
            clientStream.Write(data);
            clientStream.Flush();
            clientStream.Shutdown();

            var frame = _harness.Send1RttWithFrame<StreamFrame>(_client, _server);
            Assert.True(frame.Fin);
        }


        [Fact]
        public void SendsEmptyStreamFrameWithFin()
        {
            byte[] data = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0};
            var clientStream = _client.OpenStream(true);

            // send data before marking end of stream
            clientStream.Write(data);
            clientStream.Flush();
            var frame = _harness.Send1RttWithFrame<StreamFrame>(_client, _server);
            Assert.False(frame.Fin);

            // no more data to send, just the fin bit
            clientStream.Shutdown();
            frame = _harness.Send1RttWithFrame<StreamFrame>(_client, _server);

            Assert.True(frame.Fin);
        }

        [Fact]
        public void ClosesConnectionWhenStreamLimitIsExceeded()
        {
            byte[] data = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0};
            var clientStream = _client.OpenStream(true);
            clientStream.Write(data);
            clientStream.Flush();
            _harness.Intercept1RttFrame<StreamFrame>(_client, _server, frame =>
            {
                // make sure the stream id is above bounds
                frame.StreamId += _serverOpts.MaxUnidirectionalStreams << 2 + 4;
            });

            _harness.Send1Rtt(_server, _client).ShouldContainConnectionClose(
                TransportErrorCode.StreamLimitError,
                QuicError.StreamsLimitViolated,
                FrameType.Stream);
        }

        [Fact]
        public void ClosesConnectionWhenSendingPastMaxRepresentableOffset()
        {
            byte[] data = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0};
            var clientStream = _client.OpenStream(true);
            clientStream.Write(data);
            clientStream.Flush();
            _harness.Intercept1RttFrame<StreamFrame>(_client, _server,
                frame => { frame.Offset = StreamHelpers.MaxStreamOffset; });

            _harness.Send1Rtt(_server, _client).ShouldContainConnectionClose(
                TransportErrorCode.FrameEncodingError,
                QuicError.UnableToDeserialize);
        }

        [Fact]
        public void ClosesConnectionWhenSendingPastFin()
        {
            byte[] data = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0};
            var clientStream = _client.OpenStream(true);
            clientStream.Write(data);
            clientStream.Flush();
            _harness.Intercept1RttFrame<StreamFrame>(_client, _server,
                frame => { frame.Offset = StreamHelpers.MaxStreamOffset; });

            _harness.Send1Rtt(_server, _client).ShouldContainConnectionClose(
                TransportErrorCode.FrameEncodingError,
                QuicError.UnableToDeserialize);
        }

        [Fact]
        public void ClosesConnectionWhenSendingInNonReadableStream()
        {
            byte[] data = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0};
            var clientStream = _client.OpenStream(true);
            clientStream.Write(data);
            clientStream.Flush();
            _harness.Intercept1RttFrame<StreamFrame>(_client, _server, frame =>
            {
                // use the only type of stream into which client cannot send
                frame.StreamId = StreamHelpers.ComposeStreamId(StreamType.ServerInitiatedUnidirectional, 0);
            });

            _harness.Send1Rtt(_server, _client).ShouldContainConnectionClose(
                TransportErrorCode.StreamStateError,
                QuicError.StreamNotWritable);
        }

        [Fact]
        public void ClosesConnectionWhenSendingPastStreamMaxData()
        {
            byte[] data = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0};
            var clientStream = _client.OpenStream(true);
            clientStream.Write(data);
            clientStream.Flush();
            _harness.Intercept1RttFrame<StreamFrame>(_client, _server, frame =>
            {
                frame.Offset = TransportParameters.DefaultMaxStreamData - 1;
            });

            _harness.Send1Rtt(_server, _client).ShouldContainConnectionClose(
                TransportErrorCode.FlowControlError,
                QuicError.StreamMaxDataViolated);
        }

        [Fact]
        public void ResendsDataAfterLoss()
        {
            byte[] data = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0};
            var clientStream = _client.OpenStream(true);
            clientStream.Write(data);
            clientStream.Flush();

            // lose the first packet
            _harness.Get1RttToSend(_client).ShouldHaveFrame<StreamFrame>();

            clientStream.Write(data);
            clientStream.Flush();
            _harness.Timestamp += RecoveryController.InitialRtt * 1;
            // deliver second packet
            _harness.Send1Rtt(_client, _server).ShouldHaveFrame<StreamFrame>();

            // send ack back, leading the client to believe that first packet was lost
            _harness.Timestamp += RecoveryController.InitialRtt * 1;
            _harness.Send1Rtt(_server, _client).ShouldHaveFrame<AckFrame>();

            // resend original data
            var frame = _harness.Send1Rtt(_client, _server).ShouldHaveFrame<StreamFrame>();
            Assert.Equal(0, frame.Offset);
            Assert.Equal(data, frame.StreamData);
        }

        [Fact]
        public void ReceiverSendsMaxDataAfterReadingFromStream()
        {
            byte[] data = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0};
            byte[] recvBuf = new byte[data.Length];

            var senderStream = _client.OpenStream(true);
            senderStream.Write(data);
            senderStream.Flush();

            _harness.Send1Rtt(_client, _server);

            // read data
            var receiverStream = _server.AcceptStream();
            Assert.NotNull(receiverStream);
            int read = receiverStream.Read(recvBuf);
            Assert.Equal(recvBuf.Length, read);

            // next time, the receiver should send max data update
            var frame = _harness.Get1RttToSend(_server).ShouldHaveFrame<MaxStreamDataFrame>();
            Assert.Equal(senderStream.StreamId, frame.StreamId);
            Assert.Equal(senderStream.OutboundBuffer!.MaxData + data.Length, frame.MaximumStreamData);
        }

        [Fact]
        public void ClosesConnectionOnInvalidStreamId_StreamMaxDataFrame()
        {
            byte[] data = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0};
            byte[] recvBuf = new byte[data.Length];

            var senderStream = _client.OpenStream(true);
            senderStream.Write(data);
            senderStream.Flush();
            _harness.Send1Rtt(_client, _server);

            // read data
            var receiverStream = _server.AcceptStream();
            Assert.NotNull(receiverStream);
            receiverStream.Read(recvBuf);

            _harness.Intercept1RttFrame<MaxStreamDataFrame>(_server, _client, frame =>
            {
                // make sure the id is too large
                frame.StreamId = _clientOpts.MaxUnidirectionalStreams * 4 + 1;
            });

            _harness.Send1Rtt(_client, _server)
                .ShouldContainConnectionClose(TransportErrorCode.StreamLimitError,
                QuicError.StreamsLimitViolated, FrameType.MaxStreamData);
        }
    }
}
