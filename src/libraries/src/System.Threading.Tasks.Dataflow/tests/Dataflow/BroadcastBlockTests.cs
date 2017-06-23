// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Threading.Tasks.Dataflow.Tests
{
    public class BroadcastBlockTests
    {
        [Fact]
        public void TestCtor()
        {
            var blocks = new[] {
                new BroadcastBlock<int>(i => i),
                new BroadcastBlock<int>(null),
                new BroadcastBlock<int>(i => i, new DataflowBlockOptions { MaxMessagesPerTask = 1 }),
                new BroadcastBlock<int>(null, new DataflowBlockOptions { MaxMessagesPerTask = 1 }),
                new BroadcastBlock<int>(i => i, new DataflowBlockOptions { MaxMessagesPerTask = 1, CancellationToken = new CancellationToken(true) }),
                new BroadcastBlock<int>(null, new DataflowBlockOptions { MaxMessagesPerTask = 1, CancellationToken = new CancellationToken(true) })
            };
            foreach (var block in blocks)
            {
                Assert.NotNull(block.Completion);
                int item;
                Assert.False(block.TryReceive(out item));
            }
        }

        [Fact]
        public void TestArgumentExceptions()
        {
            Assert.Throws<ArgumentNullException>(() => new BroadcastBlock<int>(i => i, null));
            AssertExtensions.Throws<ArgumentException>("messageHeader", () =>
                ((ITargetBlock<int>)new BroadcastBlock<int>(null)).OfferMessage(default(DataflowMessageHeader), 0, null, consumeToAccept: false));
            AssertExtensions.Throws<ArgumentException>("consumeToAccept", () =>
                ((ITargetBlock<int>)new BroadcastBlock<int>(null)).OfferMessage(new DataflowMessageHeader(1), 0, null, consumeToAccept: true));
            DataflowTestHelpers.TestArgumentsExceptions(new BroadcastBlock<int>(i => i));
        }

        [Fact]
        public void TestToString()
        {
            DataflowTestHelpers.TestToString(
                nameFormat => nameFormat != null ?
                    new BroadcastBlock<int>(i => i, new DataflowBlockOptions() { NameFormat = nameFormat }) :
                    new BroadcastBlock<int>(i => i));
        }

        [Fact]
        public async Task TestPost()
        {
            var bb = new BroadcastBlock<int>(i => i);
            Assert.True(bb.Post(1));
            bb.Complete();
            Assert.False(bb.Post(2));
            await bb.Completion;
        }

        [Fact]
        public async Task TestCompletionTask()
        {
            await DataflowTestHelpers.TestCompletionTask(() => new WriteOnceBlock<int>(i => i));
        }

        [Fact]
        public async Task TestReceiveThenPost()
        {
            var bb = new BroadcastBlock<int>(null);
            var ignored = Task.Run(() => bb.Post(42));
            Assert.Equal(expected: 42, actual: await bb.ReceiveAsync()); // this should always pass, but due to race we may not test what we're hoping to

            bb = new BroadcastBlock<int>(null);
            Task<int> t = bb.ReceiveAsync();
            Assert.False(t.IsCompleted);
            bb.Post(16);
            Assert.Equal(expected: 16, actual: await t);
        }

        [Fact]
        public async Task TestBroadcasting()
        {
            var bb = new BroadcastBlock<int>(i => i + 1);
            var targets = Enumerable.Range(0, 3).Select(_ => new TransformBlock<int, int>(i => i)).ToArray();
            foreach (var target in targets)
            {
                bb.LinkTo(target);
            }

            const int Messages = 3;
            bb.PostRange(0, Messages);
            for (int i = 0; i < Messages; i++)
            {
                foreach (var target in targets)
                {
                    Assert.Equal(expected: i + 1, actual: await target.ReceiveAsync());
                }
            }
        }

        [Fact]
        public async Task TestRepeatedReceives()
        {
            var bb = new BroadcastBlock<int>(null);
            bb.Post(42);
            Assert.Equal(expected: 42, actual: await bb.ReceiveAsync());
            for (int i = 0; i < 3; i++)
            {
                int item;
                Assert.True(bb.TryReceive(out item));
                Assert.Equal(expected: 42, actual: item);

                Assert.False(bb.TryReceive(f => f == 41, out item));
                Assert.True(bb.TryReceive(f => f == 42, out item));
                Assert.Equal(expected: 42, actual: item);
            }
        }

        [Fact]
        public async Task TestLinkingAfterCompletion()
        {
            var b = new BroadcastBlock<int>(i => i * 2);
            b.Post(1);
            b.Complete();
            await b.Completion;
            using (b.LinkTo(new ActionBlock<int>(i => { })))
            using (b.LinkTo(new ActionBlock<int>(i => { })))
            {
                Assert.False(b.Post(2));
            }
        }

        [Fact]
        public async Task TestLinkingToCompleted()
        {
            var b = new BroadcastBlock<int>(i => i * 2);
            var ab = new ActionBlock<int>(i => { });
            b.LinkTo(ab);
            ab.Complete();
            Assert.True(b.Post(1));
            b.Complete();
            await b.Completion;
        }

        [Fact]
        public async Task TestCloning()
        {
            // Test cloning when a clone function is provided
            {
                int data = 42;
                var bb = new BroadcastBlock<int>(x => -x);
                Assert.True(bb.Post(data));

                for (int i = 0; i < 3; i++)
                {
                    Assert.Equal(expected: -data, actual: await bb.ReceiveAsync());
                    Assert.Equal(expected: -data, actual: bb.Receive());
                    Assert.Equal(expected: -data, actual: await bb.ReceiveAsync());

                    IList<int> items;
                    Assert.True(((IReceivableSourceBlock<int>)bb).TryReceiveAll(out items));
                    Assert.Equal(expected: items.Count, actual: 1);
                    Assert.Equal(expected: -data, actual: items[0]);
                }

                int result = 0;
                var target = new ActionBlock<int>(i => {
                    Assert.Equal(expected: 0, actual: result);
                    result = i;
                    Assert.Equal(expected: -data, actual: i);
                });
                bb.LinkTo(target, new DataflowLinkOptions { PropagateCompletion = true });
                bb.Complete();
                await target.Completion;
            }

            // Test successful processing when no clone function exists
            {
                var data = new object();
                var bb = new BroadcastBlock<object>(null);
                Assert.True(bb.Post(data));

                object result;
                for (int i = 0; i < 3; i++)
                {
                    Assert.Equal(expected: data, actual: await bb.ReceiveAsync());
                    Assert.Equal(expected: data, actual: bb.Receive());
                    Assert.Equal(expected: data, actual: await bb.ReceiveAsync());

                    IList<object> items;
                    Assert.True(((IReceivableSourceBlock<object>)bb).TryReceiveAll(out items));
                    Assert.Equal(expected: 1, actual: items.Count);
                    Assert.Equal(expected: data, actual: items[0]);
                }

                result = null;
                var target = new ActionBlock<object>(o => {
                    Assert.Null(result);
                    result = o;
                    Assert.Equal(expected: data, actual: o);
                });
                bb.LinkTo(target, new DataflowLinkOptions { PropagateCompletion = true });
                bb.Complete();
                await target.Completion;
            }
        }

        [Fact]
        public async Task TestPrecancellation()
        {
            var b = new BroadcastBlock<int>(null, new DataflowBlockOptions { CancellationToken = new CancellationToken(canceled: true) });

            Assert.NotNull(b.LinkTo(DataflowBlock.NullTarget<int>()));
            Assert.False(b.Post(42));
            Task<bool> t = b.SendAsync(42);
            Assert.True(t.IsCompleted);
            Assert.False(t.Result);
            int ignoredValue;
            IList<int> ignoredValues;
            Assert.False(b.TryReceive(out ignoredValue));
            Assert.False(((IReceivableSourceBlock<int>)b).TryReceiveAll(out ignoredValues));
            Assert.NotNull(b.Completion);
            b.Complete(); // verify doesn't throw

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => b.Completion);
        }

        [Fact]
        public async Task TestReserveReleaseConsume()
        {
            var bb = new BroadcastBlock<int>(i => i * 2);
            bb.Post(1);
            await DataflowTestHelpers.TestReserveAndRelease(bb, reservationIsTargetSpecific: false);

            bb = new BroadcastBlock<int>(i => i * 2);
            bb.Post(2);
            await DataflowTestHelpers.TestReserveAndConsume(bb, reservationIsTargetSpecific: false);
        }

        [Fact]
        public async Task TestBounding()
        {
            var bb = new BroadcastBlock<int>(null, new DataflowBlockOptions { BoundedCapacity = 1 });
            var ab = new ActionBlock<int>(i => { });
            bb.LinkTo(ab, new DataflowLinkOptions { PropagateCompletion = true });

            Task<bool>[] sends = Enumerable.Range(0, 40).Select(i => bb.SendAsync(i)).ToArray();
            bb.Complete();

            await Task.WhenAll(sends);
            await ab.Completion;
        }

        [Fact]
        public async Task TestFaultingAndCancellation()
        {
            foreach (bool fault in DataflowTestHelpers.BooleanValues)
            {
                var cts = new CancellationTokenSource();
                var bb = new BroadcastBlock<int>(null, new GroupingDataflowBlockOptions { CancellationToken = cts.Token, BoundedCapacity = 2 });
                Task<bool>[] sends = Enumerable.Range(0, 4).Select(i => bb.SendAsync(i)).ToArray();

                if (fault)
                {
                    Assert.Throws<ArgumentNullException>(() => ((IDataflowBlock)bb).Fault(null));
                    ((IDataflowBlock)bb).Fault(new InvalidCastException());
                    await Assert.ThrowsAsync<InvalidCastException>(() => bb.Completion);
                }
                else
                {
                    cts.Cancel();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => bb.Completion);
                }

                await Task.WhenAll(sends);
            }
        }

        [Fact]
        public async Task TestFaultyScheduler()
        {
            var bb = new BroadcastBlock<int>(null, new DataflowBlockOptions {
                BoundedCapacity = 1,
                TaskScheduler = new DelegateTaskScheduler
                {
                    QueueTaskDelegate = delegate { throw new FormatException(); }
                }
            });
            Task<bool> t1 = bb.SendAsync(1);
            Task<bool> t2 = bb.SendAsync(2);
            bb.LinkTo(DataflowBlock.NullTarget<int>());
            await Assert.ThrowsAsync<TaskSchedulerException>(() => bb.Completion);
            Assert.True(await t1);
            Assert.False(await t2);
        }

    }
}
