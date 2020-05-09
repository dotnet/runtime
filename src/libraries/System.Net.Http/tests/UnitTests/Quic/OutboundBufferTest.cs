using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Buffers;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Quic.Tests
{
    public class OutboundBufferTest
    {
        private OutboundBuffer buffer = new OutboundBuffer(0);

        private void EnqueueBytes(int count)
        {
            Span<byte> tmp = stackalloc byte[count];

            // generate ascending integers so that we can test for data correctness
            for (int i = 0; i < tmp.Length; i++)
            {
                tmp[i] = (byte)(buffer.WrittenBytes + i);
            }

            buffer.Enqueue(tmp);
            buffer.ForceFlushPartialChunk();
        }

        [Fact]
        public void ReturnsCorrectPendingRange()
        {
            EnqueueBytes(10);
            Assert.False(buffer.IsFlushable); // MaxData is 0 yet

            buffer.UpdateMaxData(10);
            Assert.True(buffer.IsFlushable);

            var (start, count) = buffer.GetNextSendableRange();
            Assert.Equal(0u, start);
            Assert.Equal(10u, count);

            EnqueueBytes(10);
            (start, count) = buffer.GetNextSendableRange();
            Assert.Equal(0u, start);
            Assert.Equal(10u, count); // still MaxData is 10

            buffer.UpdateMaxData(20);
            (_, count) = buffer.GetNextSendableRange();
            Assert.Equal(20u, count);
        }

        [Fact]
        public void ChecksOutPartialChunk()
        {
            buffer.UpdateMaxData(50);
            EnqueueBytes(10);

            byte[] destination = new byte[5];
            buffer.CheckOut(destination);

            Assert.Equal(new byte[]{0,1,2,3,4}, destination);

            var (start, count) = buffer.GetNextSendableRange();

            Assert.Equal(5u, start);
            Assert.Equal(5u, count);
        }

        [Fact]
        public void ChecksOutAcrossChunks()
        {
            buffer.UpdateMaxData(50);
            EnqueueBytes(10);
            EnqueueBytes(10);

            byte[] destination = new byte[10];
            // discard first 5 bytes
            buffer.CheckOut(destination.AsSpan(0, 5));

            buffer.CheckOut(destination);
            var (start, count) = buffer.GetNextSendableRange();

            Assert.Equal(new byte[]{5,6,7,8,9,10,11,12,13,14}, destination);

            Assert.Equal(15u, start);
            Assert.Equal(5u, count);
        }

        [Fact]
        public void RequeuesLostBytes()
        {
            buffer.UpdateMaxData(50);
            EnqueueBytes(10);
            EnqueueBytes(10);

            byte[] destination = new byte[10];
            // discard first 10 bytes
            buffer.CheckOut(destination.AsSpan(0, 5));
            buffer.CheckOut(destination.AsSpan(0, 5));
            // first 5 bytes got lost
            buffer.OnLost(0, 5);

            // the first bytes should be again pending
            var (start, count) = buffer.GetNextSendableRange();
            Assert.Equal(0u, start);
            Assert.Equal(5u, count);

            // if second 5 bytes are lost too, the entire buffer should be pending
            buffer.OnLost(5, 5);
            (start, count) = buffer.GetNextSendableRange();
            Assert.Equal(0u, start);
            Assert.Equal(20u, count);
        }

        [Fact]
        public void ProcessAllTheData()
        {
            buffer.UpdateMaxData(50);
            EnqueueBytes(10);

            Assert.Equal(SendStreamState.Ready, buffer.StreamState);
            Assert.False(buffer.SizeKnown);
            buffer.MarkEndOfData();

            Assert.True(buffer.SizeKnown);
            // still no transition
            Assert.Equal(SendStreamState.Ready, buffer.StreamState);

            byte[] destination = new byte[5];
            buffer.CheckOut(destination);
            buffer.OnAck(0, 5);
            Assert.Equal(SendStreamState.Send, buffer.StreamState);

            buffer.CheckOut(destination);
            Assert.Equal(SendStreamState.DataSent, buffer.StreamState);
            buffer.OnAck(5, 5, true);

            Assert.Equal(SendStreamState.DataReceived, buffer.StreamState);
        }
    }
}
