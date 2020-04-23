using System.Linq;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Quic.Tests
{
    public class InboundBufferTest
    {
        private InboundBuffer buffer = new InboundBuffer(long.MaxValue);

        private void ReceiveBytes(long offset, int count, bool end = false)
        {
            Span<byte> tmp = stackalloc byte[count];

            // generate ascending integers so that we can test for data correctness
            for (int i = 0; i < tmp.Length; i++)
            {
                tmp[i] = (byte)(offset + i);
            }

            buffer.Receive(offset, tmp, end);
        }

        [Fact]
        public void ReceivesInOrderData()
        {
            ReceiveBytes(0, 10);
            var destination = new byte[10];
            Assert.Equal(10u, buffer.BytesAvailable);
            buffer.Deliver(destination);

            Assert.Equal(new byte[]{0,1,2,3,4,5,6,7,8,9}, destination);
            Assert.Equal(10u, buffer.BytesRead);
            Assert.Equal(0u, buffer.BytesAvailable);
        }

        [Fact]
        public void ReceivesOutOfOrderData()
        {
            ReceiveBytes(5, 5);
            Assert.Equal(0u, buffer.BytesAvailable);
            ReceiveBytes(0, 5);
            Assert.Equal(10u, buffer.BytesAvailable);

            var destination = new byte[10];
            buffer.Deliver(destination);

            Assert.Equal(new byte[]{0,1,2,3,4,5,6,7,8,9}, destination);
        }

        [Fact]
        public void ReceiveDuplicateData()
        {
            ReceiveBytes(0, 5);
            ReceiveBytes(10, 5);
            ReceiveBytes(20, 5);

            ReceiveBytes(0, 25);

            Assert.Equal(25u, buffer.BytesAvailable);
            var destination = new byte[25];

            buffer.Deliver(destination);
            Assert.Equal(Enumerable.Range(0, 25).Select(i => (byte) i), destination);
        }

        [Fact]
        public async Task BlocksWhenNoDataAvailable()
        {
            var destination = new byte[10];
            var task = buffer.DeliverAsync(destination, CancellationToken.None);
            Assert.False(task.IsCompleted);

            ReceiveBytes(0, 10);
            int written = await task.AsTask().TimeoutAfter(5_000);
            Assert.Equal(10, written);
        }

        [Fact]
        public async Task DoesNotBlockIfDataAvailable()
        {
            ReceiveBytes(0, 10);
            var destination = new byte[10];
            var task = buffer.DeliverAsync(destination, CancellationToken.None);
            Assert.True(task.IsCompleted);

            int written = await task;
            Assert.Equal(10, written);
        }

        [Fact]
        public async Task DoesNotBlockIfAllDataRead()
        {
            ReceiveBytes(0, 10, true);
            var destination = new byte[10];
            buffer.Deliver(destination);

            // all data has been read, and stream is finished
            Assert.Equal(0, buffer.BytesAvailable);
            var task = buffer.DeliverAsync(destination, CancellationToken.None);
            Assert.True(task.IsCompleted);

            Assert.Equal(0, await task);
        }

        [Fact]
        public async Task EmptyFrameWithFinUnblocksReader()
        {
            var destination = new byte[10];
            var task = buffer.DeliverAsync(destination, CancellationToken.None);
            Assert.False(task.IsCompleted);

            ReceiveBytes(0, 0, true);

            int written = await task.AsTask().TimeoutAfter(100);
            Assert.Equal(0, written);
        }

        [Fact]
        public async Task OutOfOrderFinBit()
        {
            var destination = new byte[10];

            ReceiveBytes(5, 5, true);
            ReceiveBytes(0, 5);

            int read = buffer.Deliver(destination);
            Assert.Equal(10, read);
            var task = buffer.DeliverAsync(destination, CancellationToken.None);

            // All data have been read, no blocking is expected.
            Assert.True(task.IsCompleted);
            Assert.Equal(0, await task);
        }
    }
}
