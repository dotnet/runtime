// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Channels.Tests
{
    public class UnbufferedChannelTests : ChannelTestBase
    {
        protected override Channel<int> CreateChannel() => Channel.CreateUnbuffered<int>();
        protected override Channel<int> CreateFullChannel() => CreateChannel();
        protected override bool BuffersItems => false;

        [Fact]
        public async Task Complete_BeforeEmpty_WaitingWriters_TriggersCompletion()
        {
            Channel<int> c = CreateChannel();
            Task write1 = c.Writer.WriteAsync(42);
            Task write2 = c.Writer.WriteAsync(43);
            c.Writer.Complete();
            await c.Reader.Completion;
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => write1);
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => write2);
        }

        [Fact]
        public void TryReadWrite_NoPartner_Fail()
        {
            Channel<int> c = CreateChannel();
            Assert.False(c.Writer.TryWrite(42));
            Assert.False(c.Reader.TryRead(out int result));
            Assert.Equal(result, 0);
        }

        [Fact]
        public void TryRead_WriteAsync_Success()
        {
            Channel<int> c = CreateChannel();
            Task w = c.Writer.WriteAsync(42);
            Assert.False(w.IsCompleted);
            Assert.True(c.Reader.TryRead(out int result));
            Assert.Equal(42, result);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Read_MultipleUnpartneredWrites_CancelSome_ReadSucceeds(bool useReadAsync)
        {
            Channel<int> c = CreateChannel();
            var cts = new CancellationTokenSource();

            Task[] cancelableWrites = (from i in Enumerable.Range(0, 10) select c.Writer.WriteAsync(42, cts.Token)).ToArray();
            Assert.All(cancelableWrites, cw => Assert.Equal(TaskStatus.WaitingForActivation, cw.Status));

            Task w = c.Writer.WriteAsync(84);

            cts.Cancel();
            foreach (Task t in cancelableWrites)
            {
                await AssertCanceled(t, cts.Token);
            }

            if (useReadAsync)
            {
                Assert.True(c.Reader.TryRead(out int result));
                Assert.Equal(84, result);
            }
            else
            {
                Assert.Equal(84, await c.Reader.ReadAsync());
            }
        }

        [Fact]
        public async Task Cancel_PartneredWrite_Success()
        {
            Channel<int> c = CreateChannel();
            var cts = new CancellationTokenSource();

            Task w = c.Writer.WriteAsync(42, cts.Token);
            Assert.False(w.IsCompleted);

            ValueTask<int> r = c.Reader.ReadAsync();
            Assert.True(r.IsCompletedSuccessfully);

            cts.Cancel();
            await w; // no throw
        }

    }
}
