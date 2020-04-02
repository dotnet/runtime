using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Frames;
using Xunit;

namespace System.Net.Quic.Tests
{
    public class FrameSerializationTests
    {
        public FrameSerializationTests()
        {
            buffer = new byte[1024];
            reader = new QuicReader(buffer);
            writer = new QuicWriter(buffer);
        }

        private readonly QuicReader reader;
        private readonly QuicWriter writer;

        private readonly byte[] buffer;

        [Fact]
        public void SerializeAckFrame()
        {
            ulong[] ranges = {14, 134, 1313, 123123};

            Span<byte> rangesRaw = stackalloc byte[1024];
            int written = 0;
            foreach (ulong range in ranges)
            {
                written += QuicPrimitives.WriteVarInt(rangesRaw.Slice(written), range);
            }

            var expected = new AckFrame(1, 2, (ulong)(ranges.Length / 2), 3, rangesRaw.Slice(0, written), true, 1, 2,
                3);
            AckFrame.Write(writer, expected);
            Assert.True(AckFrame.Read(reader, out var actual));

            Assert.Equal(expected.LargestAcknowledged, actual.LargestAcknowledged);
            Assert.Equal(expected.AckDelay, actual.AckDelay);
            Assert.Equal(expected.AckRangeCount, actual.AckRangeCount);
            Assert.Equal(expected.FirstAckRange, actual.FirstAckRange);
            Assert.True(expected.AckRangesRaw.SequenceEqual(actual.AckRangesRaw));
            Assert.Equal(expected.HasEcnCounts, actual.HasEcnCounts);
            Assert.Equal(expected.Ect0Count, actual.Ect0Count);
            Assert.Equal(expected.Ect1Count, actual.Ect1Count);
            Assert.Equal(expected.CeCount, actual.CeCount);

            Assert.Equal(writer.BytesWritten, expected.GetSerializedLength());
        }

        [Fact]
        public void SerializeConnectionCloseFrame()
        {
            var expected = new ConnectionCloseFrame(1, true, FrameType.Ack, "hello");
            ConnectionCloseFrame.Write(writer, expected);
            Assert.True(ConnectionCloseFrame.Read(reader, out var actual));

            Assert.Equal(expected.ErrorCode, actual.ErrorCode);
            Assert.Equal(expected.IsQuicError, actual.IsQuicError);
            Assert.Equal(expected.FrameType, actual.FrameType);
            Assert.Equal(expected.ReasonPhrase, actual.ReasonPhrase);

            Assert.Equal(writer.BytesWritten, expected.GetSerializedLength());
        }

        [Fact]
        public void SerializeCryptoFrame()
        {
            var cryptoData = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 0};
            var expected = new CryptoFrame(1, cryptoData);
            CryptoFrame.Write(writer, expected);
            Assert.True(CryptoFrame.Read(reader, out var actual));

            Assert.Equal(expected.Offset, actual.Offset);
            Assert.True(expected.CryptoData.SequenceEqual(actual.CryptoData));

            Assert.Equal(writer.BytesWritten, expected.GetSerializedLength());
        }

        [Fact]
        public void SerializeDataBlockedFrame()
        {
            var expected = new DataBlockedFrame(10);
            DataBlockedFrame.Write(writer, expected);
            Assert.True(DataBlockedFrame.Read(reader, out var actual));

            Assert.Equal(expected.DataLimit, actual.DataLimit);

            Assert.Equal(writer.BytesWritten, expected.GetSerializedLength());
        }

        [Fact]
        public void SerializeMaxDataFrame()
        {
            var expected = new MaxDataFrame(300);
            MaxDataFrame.Write(writer, expected);
            Assert.True(MaxDataFrame.Read(reader, out var actual));

            Assert.Equal(expected.MaximumData, actual.MaximumData);

            Assert.Equal(writer.BytesWritten, expected.GetSerializedLength());
        }

        [Fact]
        public void SerializeMaxStreamDataFrame()
        {
            var expected = new MaxStreamDataFrame(49, 29);
            MaxStreamDataFrame.Write(writer, expected);
            Assert.True(MaxStreamDataFrame.Read(reader, out var actual));

            Assert.Equal(expected.StreamId, actual.StreamId);
            Assert.Equal(expected.MaximumStreamData, actual.MaximumStreamData);

            Assert.Equal(writer.BytesWritten, expected.GetSerializedLength());
        }

        [Fact]
        public void SerializeMaxStreamsFrame()
        {
            var expected = new MaxStreamsFrame(22332, true);
            MaxStreamsFrame.Write(writer, expected);
            Assert.True(MaxStreamsFrame.Read(reader, out var actual));

            Assert.Equal(expected.MaximumStreams, actual.MaximumStreams);
            Assert.Equal(expected.Bidirectional, actual.Bidirectional);

            Assert.Equal(writer.BytesWritten, expected.GetSerializedLength());
        }

        [Fact]
        public void SerializeNewConnectionIdFrame()
        {
            var connectionId = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 0};
            var resetToken = new byte[] {1, 2, 31, 4, 2, 1, 43, 4, 2, 1, 34, 3, 2, 24, 0, 3};

            var expected = new NewConnectionIdFrame(4, 2, connectionId, StatelessResetToken.FromSpan(resetToken));
            NewConnectionIdFrame.Write(writer, expected);
            Assert.True(NewConnectionIdFrame.Read(reader, out var actual));

            Assert.Equal(expected.SequenceNumber, actual.SequenceNumber);
            Assert.Equal(expected.RetirePriorTo, actual.RetirePriorTo);
            Assert.True(expected.ConnectionId.SequenceEqual(actual.ConnectionId));
            Assert.Equal(expected.StatelessResetToken, actual.StatelessResetToken);

            Assert.Equal(writer.BytesWritten, expected.GetSerializedLength());
        }

        [Fact]
        public void SerializeNewTokenFrame()
        {
            var token = new byte[] {1, 2, 31, 4, 2, 1, 43, 4, 2, 1, 34, 3, 2, 24, 0};

            var expected = new NewTokenFrame(token);
            NewTokenFrame.Write(writer, expected);
            Assert.True(NewTokenFrame.Read(reader, out var actual));

            Assert.True(expected.Token.SequenceEqual(actual.Token));

            Assert.Equal(writer.BytesWritten, expected.GetSerializedLength());
        }

        [Fact]
        public void SerializePathChallengeFrame()
        {
            var expected = new PathChallengeFrame(34234134113423255, true);
            PathChallengeFrame.Write(writer, expected);
            Assert.True(PathChallengeFrame.Read(reader, out var actual));

            Assert.Equal(expected.Data, actual.Data);
            Assert.Equal(expected.IsChallenge, actual.IsChallenge);

            Assert.Equal(writer.BytesWritten, expected.GetSerializedLength());
        }

        [Fact]
        public void SerializeResetStreamFrame()
        {
            var expected = new ResetStreamFrame(12, 344, 10334);
            ResetStreamFrame.Write(writer, expected);
            Assert.True(ResetStreamFrame.Read(reader, out var actual));

            Assert.Equal(expected.StreamId, actual.StreamId);
            Assert.Equal(expected.ApplicationErrorCode, actual.ApplicationErrorCode);
            Assert.Equal(expected.FinalSize, actual.FinalSize);

            Assert.Equal(writer.BytesWritten, expected.GetSerializedLength());
        }

        [Fact]
        public void SerializeRetireConnectionIdFrame()
        {
            var expected = new RetireConnectionIdFrame(21);
            RetireConnectionIdFrame.Write(writer, expected);
            Assert.True(RetireConnectionIdFrame.Read(reader, out var actual));

            Assert.Equal(expected.SequenceNumber, actual.SequenceNumber);

            Assert.Equal(writer.BytesWritten, expected.GetSerializedLength());
        }

        [Fact]
        public void SerializeStopSendingFrame()
        {
            var expected = new StopSendingFrame(22, 223);
            StopSendingFrame.Write(writer, expected);
            Assert.True(StopSendingFrame.Read(reader, out var actual));

            Assert.Equal(expected.StreamId, actual.StreamId);
            Assert.Equal(expected.ApplicationErrorCode, actual.ApplicationErrorCode);

            Assert.Equal(writer.BytesWritten, expected.GetSerializedLength());
        }

        [Fact]
        public void SerializeStreamDataBlockedFrame()
        {
            var expected = new StreamDataBlockedFrame(23, 4444);
            StreamDataBlockedFrame.Write(writer, expected);
            Assert.True(StreamDataBlockedFrame.Read(reader, out var actual));

            Assert.Equal(expected.StreamId, actual.StreamId);
            Assert.Equal(expected.StreamDataLimit, actual.StreamDataLimit);

            Assert.Equal(writer.BytesWritten, expected.GetSerializedLength());
        }

        [Fact]
        public void SerializeStreamFrame()
        {
            var data = new byte[] {13, 2, 3, 5, 7, 9, 98, 7, 87, 89, 79, 7, 89};

            var expected = new StreamFrame(44, 4234, false, data);
            StreamFrame.Write(writer, expected);
            Assert.True(StreamFrame.Read(reader, out var actual));

            Assert.Equal(expected.StreamId, actual.StreamId);
            Assert.Equal(expected.Offset, actual.Offset);
            Assert.Equal(expected.Fin, actual.Fin);
            Assert.True(expected.StreamData.SequenceEqual(actual.StreamData));

            Assert.Equal(writer.BytesWritten, expected.GetSerializedLength());
        }

        [Fact]
        public void SerializeStreamsBlockedFrame()
        {
            var expected = new StreamsBlockedFrame(34, true);
            StreamsBlockedFrame.Write(writer, expected);
            Assert.True(StreamsBlockedFrame.Read(reader, out var actual));

            Assert.Equal(expected.StreamLimit, actual.StreamLimit);
            Assert.Equal(expected.Bidirectional, actual.Bidirectional);

            Assert.Equal(writer.BytesWritten, expected.GetSerializedLength());
        }
    }
}
