// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Streams;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Quic.Tests
{
    public class SendStreamTest
    {
        private SendStream stream = new SendStream(0);

        private void EnqueueBytes(int count)
        {
            Span<byte> tmp = stackalloc byte[count];

            // generate ascending integers so that we can test for data correctness
            for (int i = 0; i < tmp.Length; i++)
            {
                tmp[i] = (byte)(stream.WrittenBytes + i);
            }

            stream.Enqueue(tmp);
            stream.ForceFlushPartialChunk();
        }

        [Fact]
        public void ReturnsCorrectPendingRange()
        {
            EnqueueBytes(10);
            Assert.False(stream.IsFlushable); // MaxData is still 0

            stream.UpdateMaxData(10);
            Assert.True(stream.IsFlushable);

            var (start, count) = stream.GetNextSendableRange();
            Assert.Equal(0u, start);
            Assert.Equal(10u, count);

            EnqueueBytes(10);
            (start, count) = stream.GetNextSendableRange();
            Assert.Equal(0u, start);
            Assert.Equal(10u, count); // still MaxData is 10

            stream.UpdateMaxData(20);
            (_, count) = stream.GetNextSendableRange();
            Assert.Equal(20u, count);
        }

        [Fact]
        public void ChecksOutPartialChunk()
        {
            stream.UpdateMaxData(50);
            EnqueueBytes(10);

            byte[] destination = new byte[5];
            stream.CheckOut(destination);

            Assert.Equal(new byte[]{0,1,2,3,4}, destination);

            var (start, count) = stream.GetNextSendableRange();

            Assert.Equal(5u, start);
            Assert.Equal(5u, count);
        }

        [Fact]
        public void ChecksOutAcrossChunks()
        {
            stream.UpdateMaxData(50);
            EnqueueBytes(10);
            EnqueueBytes(10);

            byte[] destination = new byte[10];
            // discard first 5 bytes
            stream.CheckOut(destination.AsSpan(0, 5));

            stream.CheckOut(destination);
            var (start, count) = stream.GetNextSendableRange();

            Assert.Equal(new byte[]{5,6,7,8,9,10,11,12,13,14}, destination);

            Assert.Equal(15u, start);
            Assert.Equal(5u, count);
        }

        [Fact]
        public void RequeuesLostBytes()
        {
            stream.UpdateMaxData(50);
            EnqueueBytes(10);
            EnqueueBytes(10);

            byte[] destination = new byte[10];
            // discard first 10 bytes
            stream.CheckOut(destination.AsSpan(0, 5));
            stream.CheckOut(destination.AsSpan(0, 5));
            // first 5 bytes got lost
            stream.OnLost(0, 5);

            // the first bytes should be again pending
            var (start, count) = stream.GetNextSendableRange();
            Assert.Equal(0u, start);
            Assert.Equal(5u, count);

            // if second 5 bytes are lost too, the entire stream should be pending
            stream.OnLost(5, 5);
            (start, count) = stream.GetNextSendableRange();
            Assert.Equal(0u, start);
            Assert.Equal(20u, count);
        }

        [Fact]
        public void ProcessAllTheData()
        {
            stream.UpdateMaxData(50);
            EnqueueBytes(10);

            Assert.Equal(SendStreamState.Ready, stream.StreamState);
            Assert.False(stream.SizeKnown);
            stream.MarkEndOfData();

            Assert.True(stream.SizeKnown);
            // still no transition
            Assert.Equal(SendStreamState.Ready, stream.StreamState);

            byte[] destination = new byte[5];
            stream.CheckOut(destination);
            stream.OnAck(0, 5);
            Assert.Equal(SendStreamState.Send, stream.StreamState);

            stream.CheckOut(destination);
            Assert.Equal(SendStreamState.DataSent, stream.StreamState);
            stream.OnAck(5, 5, true);

            Assert.Equal(SendStreamState.DataReceived, stream.StreamState);
        }

        [Fact]
        public async Task RequestingAbortAbortsWriters()
        {
            var destination = new byte[100];

            var exnTask = Assert.ThrowsAsync<QuicStreamAbortedException>(
                async () =>
                {
                    while (true)
                    {
                        await stream.EnqueueAsync(destination);
                        await stream.FlushChunkAsync();
                    }
                });

            stream.RequestAbort(10000);

            var exn = await exnTask.TimeoutAfter(5_000);
            Assert.Equal(10000, exn.ErrorCode);
        }
    }
}
