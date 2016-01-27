// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Threading.Tasks.Dataflow.Tests
{
    public class BatchBlockTests
    {
        [Fact]
        public void TestCtor()
        {
            var blocks = new[]
            {
                new BatchBlock<int>(1),
                new BatchBlock<int>(2, new GroupingDataflowBlockOptions { MaxNumberOfGroups = 1 }),
                new BatchBlock<int>(3, new GroupingDataflowBlockOptions { MaxMessagesPerTask = 1 }),
                new BatchBlock<int>(4, new GroupingDataflowBlockOptions { MaxMessagesPerTask = 1, CancellationToken = new CancellationToken(true), MaxNumberOfGroups = 1 })
            };
            for (int i = 0; i < blocks.Length; i++)
            {
                Assert.Equal(expected: i + 1, actual: blocks[i].BatchSize);
                Assert.Equal(expected: 0, actual: blocks[i].OutputCount);
                Assert.NotNull(blocks[i].Completion);
            }
        }

        [Fact]
        public void TestArgumentExceptions()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BatchBlock<int>(-1));
            Assert.Throws<ArgumentNullException>(() => new BatchBlock<int>(2, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BatchBlock<int>(2, new GroupingDataflowBlockOptions { BoundedCapacity = 1 }));
            DataflowTestHelpers.TestArgumentsExceptions(new BatchBlock<int>(1));
        }

        [Fact]
        public void TestToString()
        {
            DataflowTestHelpers.TestToString(nameFormat =>
                nameFormat != null ?
                    new BatchBlock<int>(2, new GroupingDataflowBlockOptions() { NameFormat = nameFormat }) :
                    new BatchBlock<int>(2));
        }

        [Fact]
        public async Task TestOfferMessage()
        {
            var generators = new Func<BatchBlock<int>>[]
            {
                () => new BatchBlock<int>(2),
                () => new BatchBlock<int>(2, new GroupingDataflowBlockOptions { MaxMessagesPerTask = 1 })
            };
            foreach (var generator in generators)
            {
                DataflowTestHelpers.TestOfferMessage_ArgumentValidation(generator());

                var target = generator();
                DataflowTestHelpers.TestOfferMessage_AcceptsDataDirectly(target, messages: target.BatchSize * 2);
                IList<int[]> items;
                Assert.True(target.TryReceiveAll(out items));
                Assert.Equal(expected: 2, actual: items.Count);
                DataflowTestHelpers.TestOfferMessage_CompleteAndOffer(target);
                await target.Completion;

                target = generator();
                await DataflowTestHelpers.TestOfferMessage_AcceptsViaLinking(target, messages: target.BatchSize * 2);
                Assert.True(target.TryReceiveAll(out items));
                Assert.Equal(expected: 2, actual: items.Count);
                DataflowTestHelpers.TestOfferMessage_CompleteAndOffer(target);
                await target.Completion;
            }
        }

        [Fact]
        public void TestPost()
        {
            foreach (int boundedCapacity in new[] { DataflowBlockOptions.Unbounded, 1 })
            {
                var bb = new BufferBlock<int>(new DataflowBlockOptions { BoundedCapacity = boundedCapacity });
                Assert.True(bb.Post(0));
                bb.Complete();
                Assert.False(bb.Post(0));
            }
        }

        [Fact]
        public Task TestCompletionTask()
        {
            return DataflowTestHelpers.TestCompletionTask(() => new BufferBlock<int>());
        }

        [Fact]
        public async Task TestLinkToOptions()
        {
            const int Messages = 2;
            foreach (bool append in DataflowTestHelpers.BooleanValues)
            {
                var bb = new BatchBlock<int>(1);
                var values = new int[Messages][];
                var targets = new ActionBlock<int[]>[Messages];
                for (int i = 0; i < Messages; i++)
                {
                    int slot = i;
                    targets[i] = new ActionBlock<int[]>(item => values[slot] = item);
                    bb.LinkTo(targets[i], new DataflowLinkOptions { MaxMessages = 1, Append = append });
                }
                bb.PostRange(0, Messages);
                bb.Complete();
                await bb.Completion;

                for (int i = 0; i < Messages; i++)
                {
                    targets[i].Complete();
                    await targets[i].Completion;
                    Assert.Equal(
                        expected: append ? i : Messages - i - 1,
                        actual: values[i][0]);
                }
            }
        }

        [Fact]
        public void TestReceives()
        {
            for (int test = 0; test < 3; test++)
            {
                var bb = new BatchBlock<int>(1);
                bb.PostRange(0, 5);

                int[] item;
                switch (test)
                {
                    case 0:
                        IList<int[]> items;
                        Assert.True(bb.TryReceiveAll(out items));
                        Assert.Equal(expected: 5, actual: items.Count);
                        for (int i = 0; i < items.Count; i++)
                        {
                            Assert.Equal(expected: i, actual: items[i][0]);
                        }
                        Assert.False(bb.TryReceiveAll(out items));
                        break;

                    case 1:
                        for (int i = 0; i < 5; i++)
                        {
                            Assert.True(bb.TryReceive(f => true, out item));
                            Assert.Equal(expected: i, actual: item[0]);
                        }
                        Assert.False(bb.TryReceive(f => true, out item));
                        break;

                    case 2:
                        for (int i = 0; i < 5; i++)
                        {
                            Assert.False(bb.TryReceive(f => f[0] == i + 1, out item));
                            Assert.True(bb.TryReceive(f => f[0] == i, out item));
                            Assert.Equal(expected: i, actual: item[0]);
                        }
                        Assert.False(bb.TryReceive(f => true, out item));
                        break;
                }
            }
        }

        [Fact]
        public async Task TestBoundedReceives()
        {
            for (int test = 0; test < 4; test++)
            {
                var bb = new BatchBlock<int>(1, new GroupingDataflowBlockOptions { BoundedCapacity = 1 });
                Assert.True(bb.Post(0));

                int[] item;
                const int sends = 5;
                for (int i = 1; i <= sends; i++)
                {
                    Task<bool> send = bb.SendAsync(i);
                    Assert.True(await bb.OutputAvailableAsync()); // wait for previously posted/sent item

                    switch (test)
                    {
                        case 0:
                            IList<int[]> items;
                            Assert.True(bb.TryReceiveAll(out items));
                            Assert.Equal(expected: 1, actual: items.Count);
                            Assert.Equal(expected: 1, actual: items[0].Length);
                            Assert.Equal(expected: i - 1, actual: items[0][0]);
                            break;

                        case 1:
                            Assert.True(bb.TryReceive(f => true, out item));
                            Assert.Equal(expected: 1, actual: item.Length);
                            Assert.Equal(expected: i - 1, actual: item[0]);
                            break;

                        case 2:
                            Assert.False(bb.TryReceive(f => f.Length == 1 && f[0] == i, out item));
                            Assert.True(bb.TryReceive(f => f.Length == 1 && f[0] == i - 1, out item));
                            Assert.Equal(expected: 1, actual: item.Length);
                            Assert.Equal(expected: i - 1, actual: item[0]);
                            break;

                        case 3:
                            item = await bb.ReceiveAsync();
                            Assert.Equal(expected: 1, actual: item.Length);
                            Assert.Equal(expected: i - 1, actual: item[0]);
                            break;
                    }
                }

                // Receive remaining item
                item = await bb.ReceiveAsync();
                Assert.Equal(expected: 1, actual: item.Length);
                Assert.Equal(expected: sends, actual: item[0]);

                bb.Complete();
                await bb.Completion;
            }
        }

        [Fact]
        public async Task TestProducerConsumer()
        {
            foreach (TaskScheduler scheduler in new[] { TaskScheduler.Default, new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler })
            foreach (int maxMessagesPerTask in new[] { DataflowBlockOptions.Unbounded, 1, 2 })
            foreach (int boundedCapacity in new[] { DataflowBlockOptions.Unbounded, 2, 3 })
            foreach (int batchSize in new[] { 1, 2 })
            {
                const int Messages = 50;
                var bb = new BatchBlock<int>(batchSize, new GroupingDataflowBlockOptions
                {
                    BoundedCapacity = boundedCapacity,
                    MaxMessagesPerTask = maxMessagesPerTask,
                    TaskScheduler = scheduler
                });
                await Task.WhenAll(
                    Task.Run(async delegate { // consumer
                        int i = 0;
                        while (await bb.OutputAvailableAsync())
                        {
                            int[] items = await bb.ReceiveAsync();
                            Assert.Equal(expected: batchSize, actual: items.Length);
                            for (int j = 0; j < items.Length; j++)
                            {
                                Assert.Equal(expected: i + j, actual: items[j]);
                            }
                            i += batchSize;
                        }
                    }),
                    Task.Run(async delegate { // producer
                        for (int i = 0; i < Messages; i++)
                        {
                            await bb.SendAsync(i);
                        }
                        bb.Complete();
                    }));
            }
        }

        [Fact]
        public async Task TestReserveReleaseConsume()
        {
            var bb = new BatchBlock<int>(2);
            bb.PostItems(1, 2);
            await DataflowTestHelpers.TestReserveAndRelease(bb);

            bb = new BatchBlock<int>(2);
            bb.PostItems(1, 2);
            await DataflowTestHelpers.TestReserveAndConsume(bb);
        }

        [Fact]
        public async Task TestNonGreedy()
        {
            var batch = new BatchBlock<int>(2, new GroupingDataflowBlockOptions { Greedy = false });

            for (int trial = 0; trial < 2; trial++)
            {
                Task<bool> firstSend = batch.SendAsync(1 + trial);
                Assert.False(firstSend.IsCompleted); // should always pass, but due to race might not test what we really want it to
                Assert.Equal(expected: 0, actual: batch.OutputCount); // ditto

                Task<bool> secondSend = batch.SendAsync(3 + trial);

                Assert.Equal(expected: true, actual: await firstSend);
                Assert.Equal(expected: true, actual: await secondSend);

                Assert.Equal(expected: true, actual: await batch.OutputAvailableAsync());
                Assert.Equal(expected: 1, actual: batch.OutputCount);
                int[] result = await batch.ReceiveAsync();
                Assert.NotNull(result);
                Assert.Equal(expected: 2, actual: result.Length);
                Assert.Equal(expected: 1 + trial, actual: result[0]);
                Assert.Equal(expected: 3 + trial, actual: result[1]);
            }

            batch.Complete();
            await batch.Completion;
        }

        [Fact]
        public async Task TestGreedyFromPosts()
        {
            const int Batches = 3;
            foreach (int batchSize in new[] { 1, 2, 5 })
            {
                var batch = new BatchBlock<int>(batchSize);
                for (int i = 0; i < Batches * batchSize; i++)
                {
                    Assert.True(batch.Post(i));
                    Assert.Equal(expected: (i+1) / batchSize, actual: batch.OutputCount);
                }

                for (int i = 0; i < Batches; i++)
                {
                    int[] result = await batch.ReceiveAsync();
                    Assert.Equal(expected: batchSize, actual: result.Length);
                    Assert.Equal(expected: Batches - (i + 1), actual: batch.OutputCount);
                    for (int j = 0; j < result.Length - 1; j++)
                    {
                        Assert.Equal(result[j] + 1, result[j + 1]);
                    }
                }
            }
        }

        [Fact]
        public async Task TestMultipleNonGreedyFromSources()
        {
            const int Batches = 10;
            foreach (int batchSize in new[] { 1, 2, 5 })
            {
                var batch = new BatchBlock<int>(batchSize, new GroupingDataflowBlockOptions { Greedy = false });
                var buffers = Enumerable.Range(0, batchSize).Select(_ => new BufferBlock<int>()).ToList();
                foreach (var buffer in buffers)
                {
                    buffer.LinkTo(batch);
                }

                int prevSum = -1;
                for (int i = 0; i < Batches; i++)
                {
                    for (int j = 0; j < batchSize; j++)
                    {
                        buffers[j].Post(i);
                    }
                    int sum = (await batch.ReceiveAsync()).Sum();
                    Assert.True(sum > prevSum);
                    prevSum = sum;
                }
            }
        }

        [Fact]
        public async Task TestBoundedCapacityFromSends()
        {
            foreach (int boundedCapacity in new[] { DataflowBlockOptions.Unbounded, 3 })
            foreach (bool greedy in DataflowTestHelpers.BooleanValues)
            {
                var bb = new BatchBlock<int>(2, new GroupingDataflowBlockOptions { BoundedCapacity = boundedCapacity, Greedy = greedy });
                Task<bool>[] sends = Enumerable.Range(0, 100).Select(i => bb.SendAsync(i)).ToArray();
                ((IDataflowBlock)bb).Fault(new InvalidCastException());
                await Assert.ThrowsAsync<InvalidCastException>(() => bb.Completion);
                await Task.WhenAll(sends);
            }
        }

        [Fact]
        public async Task TestNonGreedyFailedConsume()
        {
            foreach (bool exceptionalConsume in DataflowTestHelpers.BooleanValues)
            foreach (bool linkGoodFirst in DataflowTestHelpers.BooleanValues)
            {
                const int BatchSize = 2;
                var bb = new BatchBlock<int>(BatchSize, new GroupingDataflowBlockOptions { Greedy = false });

                var goodSource = new BufferBlock<int>();
                Assert.True(goodSource.Post(1));

                if (linkGoodFirst)
                {
                    goodSource.LinkTo(bb);
                }

                var badSource = new DelegatePropagator<int, int>
                {
                    ReserveMessageDelegate = delegate { return true; },
                    ConsumeMessageDelegate = delegate(DataflowMessageHeader header, ITargetBlock<int> target, out bool messageConsumed) {
                        if (exceptionalConsume)
                        {
                            throw new FormatException(); // throw when attempting to consume reserved message
                        }
                        else
                        {
                            messageConsumed = false; // fail when attempting to consume reserved message
                            return 0;
                        }
                    }
                };
                Assert.Equal(
                    expected: DataflowMessageStatus.Postponed,
                    actual: ((ITargetBlock<int>)bb).OfferMessage(new DataflowMessageHeader(2), 2, badSource, consumeToAccept: true));

                if (!linkGoodFirst)
                {
                    goodSource.LinkTo(bb);
                }

                await (exceptionalConsume ?
                    (Task)Assert.ThrowsAsync<FormatException>(() => bb.Completion) :
                    (Task)Assert.ThrowsAsync<InvalidOperationException>(() => bb.Completion));
            }
        }

        [Fact]
        public async Task TestBatchingFromSubsetOfSources()
        {
            const int Batches = 5;
            foreach (int batchSize in new[] { 1, 2, 5 })
            foreach (bool greedy in DataflowTestHelpers.BooleanValues)
            {
                var batch = new BatchBlock<int>(batchSize, new GroupingDataflowBlockOptions { Greedy = greedy });
                var buffers = Enumerable.Range(0, batchSize * Batches).Select(_ => new BufferBlock<int>()).ToList();

                foreach (var buffer in buffers)
                {
                    buffer.LinkTo(batch);
                    buffer.Post(1);
                }

                for (int i = 0; i < Batches; i++)
                {
                    Assert.Equal(expected: batchSize, actual: (await batch.ReceiveAsync()).Sum());
                }
            }
        }

        [Fact]
        public async Task TestNonGreedyLostMessages()
        {
            foreach (int batchSize in new[] { 2, 5 })
            {
                var batch = new BatchBlock<int>(batchSize, new GroupingDataflowBlockOptions { Greedy = false });
                var buffers = Enumerable.Range(0, batchSize - 1).Select(_ => new BufferBlock<int>()).ToList();

                var tcs = new TaskCompletionSource<bool>();
                int remaining = buffers.Count;

                // Offer the batch almost all the messages it needs, but have them consumed by someone else
                foreach (var buffer in buffers)
                {
                    buffer.LinkTo(batch);
                    buffer.LinkTo(new ActionBlock<int>(i => {
                        if (Interlocked.Decrement(ref remaining) == 0)
                        {
                            tcs.SetResult(true);
                        }
                    }));
                    buffer.Post(42);
                }
                await tcs.Task;

                // Now offer from another set of sources that won't lose them
                buffers = Enumerable.Range(0, batchSize).Select(_ => new BufferBlock<int>()).ToList();
                foreach (var buffer in buffers)
                {
                    buffer.LinkTo(batch);
                    buffer.Post(43);
                    buffer.Complete();
                }

                // Wait until all the messages are consumed
                await Task.WhenAll(from buffer in buffers select buffer.Completion);

                int[] results = await batch.ReceiveAsync();
                Assert.Equal(expected: 0, actual: batch.OutputCount);
                batch.Complete();
                await batch.Completion;
            }
        }

        [Fact]
        public async Task TestPrecancellation()
        {
            var b = new BatchBlock<int>(42, new GroupingDataflowBlockOptions { 
                CancellationToken = new CancellationToken(canceled: true), MaxNumberOfGroups = 1 
            });

            Assert.Equal(expected: 42, actual: b.BatchSize);
            Assert.NotNull(b.LinkTo(DataflowBlock.NullTarget<int[]>()));
            Assert.False(b.Post(42));
            Task<bool> t = b.SendAsync(42);
            Assert.True(t.IsCompleted);
            Assert.False(t.Result);
            int[] ignoredValue;
            IList<int[]> ignoredValues;
            Assert.False(b.TryReceive(out ignoredValue));
            Assert.False(b.TryReceiveAll(out ignoredValues));
            Assert.Equal(expected: 0, actual: b.OutputCount);
            Assert.NotNull(b.Completion);
            b.Complete(); // verify doesn't throw

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => b.Completion);
        }

        [Fact]
        public async Task TestFaultingAndCancellation()
        {
            foreach (bool fault in DataflowTestHelpers.BooleanValues)
            {
                var cts = new CancellationTokenSource();
                var bb = new BatchBlock<int>(1, new GroupingDataflowBlockOptions { CancellationToken = cts.Token });
                bb.PostRange(0, 4);
                Assert.Equal(expected: 0, actual: (await bb.ReceiveAsync())[0]);
                Assert.Equal(expected: 1, actual: (await bb.ReceiveAsync())[0]);

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

                Assert.Equal(expected: 0, actual: bb.OutputCount);
            }
        }

        [Fact]
        public async Task TestCompletionWithBufferedItems()
        {
            var b = new BatchBlock<int>(5);
            b.PostRange(0, 3);
            b.Complete();

            await b.OutputAvailableAsync();
            Assert.Equal(expected: 1, actual: b.OutputCount);
            int[] items = await b.ReceiveAsync();
            Assert.Equal(expected: 3, actual: items.Length);
            await b.Completion;
        }

        [Fact]
        public async Task TestMaxNumberOfGroups()
        {
            foreach (bool greedy in DataflowTestHelpers.BooleanValues)
            for (int maxNumberOfGroups = 1; maxNumberOfGroups <= 21; maxNumberOfGroups += 20)
            {
                for (int itemsPerBatch = 1; itemsPerBatch <= 3; itemsPerBatch += 2)
                {
                    var batch = new BatchBlock<int>(itemsPerBatch, 
                        new GroupingDataflowBlockOptions { MaxNumberOfGroups = maxNumberOfGroups, Greedy = greedy });

                    // Feed all N batches; all should succeed
                    for (int batchNum = 0; batchNum < maxNumberOfGroups; batchNum++)
                    {
                        var sendAsyncs = new Task<bool>[itemsPerBatch];
                        for (int itemNum = 0; itemNum < itemsPerBatch; itemNum++)
                        {
                            sendAsyncs[itemNum] = batch.SendAsync(itemNum);
                            if (greedy)
                            {
                                Assert.True(sendAsyncs[itemNum].IsCompleted);
                                Assert.True(sendAsyncs[itemNum].Result);
                            }
                            else if (itemNum < itemsPerBatch - 1)
                            {
                                Assert.False(sendAsyncs[itemNum].IsCompleted);
                            }
                        }
                        await Task.WhenAll(sendAsyncs);
                    }

                    // Next message should fail in greedy mode
                    if (greedy)
                    {
                        var t = batch.SendAsync(1);
                        Assert.Equal(expected: TaskStatus.RanToCompletion, actual: t.Status);
                        Assert.False(t.Result);
                    }

                    // Make sure all batches were produced
                    for (int i = 0; i < maxNumberOfGroups; i++)
                    {
                        int[] result = await batch.ReceiveAsync();
                        Assert.Equal(expected: itemsPerBatch, actual: result.Length);
                    }

                    // Next message should fail, even after groups have been produced
                    if (!greedy)
                    {
                        var t = batch.SendAsync(1);
                        Assert.Equal(expected: TaskStatus.RanToCompletion, actual: t.Status);
                        Assert.False(t.Result);
                    }
                }
            }
        }

        [Fact]
        public async Task TestReleaseOnReserveException()
        {
            foreach (bool linkBadFirst in DataflowTestHelpers.BooleanValues)
            {
                var goodSource = new BufferBlock<int>();
                goodSource.Post(1);

                DelegatePropagator<int, int> badSource = null;
                badSource = new DelegatePropagator<int, int>
                {
                    LinkToDelegate = (target, options) => {
                        target.OfferMessage(new DataflowMessageHeader(1), 2, badSource, consumeToAccept: true);
                        return new DelegateDisposable();
                    },
                    ReserveMessageDelegate = delegate { throw new InvalidCastException(); }
                };

                var batch = new BatchBlock<int>(2, new GroupingDataflowBlockOptions { Greedy = false });

                if (linkBadFirst) // Each linking will offer a message
                {
                    badSource.LinkTo(batch);
                    goodSource.LinkTo(batch);
                }
                else
                {
                    goodSource.LinkTo(batch);
                    badSource.LinkTo(batch);
                }

                await Assert.ThrowsAnyAsync<InvalidCastException>(() => batch.Completion);

                int item;
                Assert.True(goodSource.TryReceive(out item)); // The good message must not be Reserved
            }
        }

        [Fact]
        public void TestTriggerBatch_Nop()
        {
            const int Iters = 2;
            var b = new BatchBlock<int>(1);
            for (int i = 0; i < Iters; i++)
            {
                b.Post(i);
                int outputCount = b.OutputCount;
                Assert.Equal(expected: i + 1, actual: outputCount);
                b.TriggerBatch();
                Assert.Equal(expected: outputCount, actual: b.OutputCount);
            }

            b = new BatchBlock<int>(1);
            Assert.Equal(expected: 0, actual: b.OutputCount);
            for (int i = 0; i < 2; i++)
            {
                b.TriggerBatch();
            }
            for (int i = 0; i < 2; i++)
            {
                b.Complete();
                b.TriggerBatch();
            }
            Assert.Equal(expected: 0, actual: b.OutputCount);
        }

        [Fact]
        public void TestTriggerBatch_VaryingBatchSizes()
        {
            foreach (var batchSize in new[] { 2, 5 })
            foreach (var queuedBeforeTrigger in new[] { 1, batchSize - 1 })
            {
                var b = new BatchBlock<int>(batchSize);
                b.PostRange(1, queuedBeforeTrigger + 1);

                Assert.Equal(expected: 0, actual: b.OutputCount);
                b.TriggerBatch();
                Assert.Equal(expected: 1, actual: b.OutputCount);
                
                int[] results;
                Assert.True(b.TryReceive(out results));
                Assert.Equal(expected: queuedBeforeTrigger, actual: results.Length);

                for (int j = 0; j < batchSize; j++)
                {
                    Assert.Equal(expected: 0, actual: b.OutputCount);
                    b.Post(j);
                }
                Assert.Equal(expected: 1, actual: b.OutputCount);
            }
        }

        [Fact]
        public void TestTriggerBatch_Cancellation()
        {
            foreach (bool post in DataflowTestHelpers.BooleanValues)
            {
                var cts = new CancellationTokenSource();
                var b = new BatchBlock<int>(2, new GroupingDataflowBlockOptions { CancellationToken = cts.Token });
                Assert.Equal(expected: 0, actual: b.OutputCount);
                if (post)
                {
                    b.Post(42);
                    Assert.Equal(expected: 0, actual: b.OutputCount);
                }
                cts.Cancel();
                Assert.Equal(expected: 0, actual: b.OutputCount);
                b.TriggerBatch();
                Assert.Equal(expected: 0, actual: b.OutputCount);
            }
        }

        [Fact]
        public void TestTriggerBatch_MaxNumberOfGroups()
        {
            foreach (int maxGroups in new[] { 1, 3 })
            {
                var b = new BatchBlock<int>(2, new GroupingDataflowBlockOptions { MaxNumberOfGroups = maxGroups });
                for (int i = 0; i < maxGroups; i++)
                {
                    b.Post(42);
                    Assert.Equal(expected: i, actual: b.OutputCount);
                    b.TriggerBatch();
                    Assert.Equal(expected: i + 1, actual: b.OutputCount);
                }
                Assert.False(b.Post(43));
                b.TriggerBatch();
                Assert.Equal(expected: maxGroups, actual: b.OutputCount);
            }
        }

        [Fact]
        public void TestTriggerBatch_NonGreedyEmpty()
        {
            var dbo = new GroupingDataflowBlockOptions { Greedy = false };
            var b = new BatchBlock<int>(3, dbo);
            Assert.Equal(expected: 0, actual: b.OutputCount);
            b.TriggerBatch();
            Assert.Equal(expected: 0, actual: b.OutputCount);
        }

        [Fact]
        public async Task TestTriggerBatch_NonGreedy()
        {
            var dbo = new GroupingDataflowBlockOptions { Greedy = false };

            const int BatchSize = 10;
            for (int numPostponedMessages = 1; numPostponedMessages < BatchSize; numPostponedMessages++)
            {
                var b = new BatchBlock<int>(BatchSize, dbo);
                Assert.Equal(expected: 0, actual: b.OutputCount);
                for (int i = 0; i < numPostponedMessages; i++)
                {
                    Assert.False(b.SendAsync(i).IsCompleted);
                }
                b.TriggerBatch();
                int[] results = await b.ReceiveAsync();
                Assert.Equal(expected: numPostponedMessages, actual: results.Length);
                for (int i = 0; i < results.Length; i++)
                {
                    Assert.Equal(expected: i, actual: results[i]);
                }
                Assert.Equal(expected: 0, actual: b.OutputCount);
                b.TriggerBatch();
                Assert.Equal(expected: 0, actual: b.OutputCount);
            }
        }

        [Fact]
        public async Task TestFaultyScheduler()
        {
            var bb = new BatchBlock<int>(2, new GroupingDataflowBlockOptions
            {
                Greedy = false,
                TaskScheduler = new DelegateTaskScheduler
                {
                    QueueTaskDelegate = delegate { throw new FormatException(); }
                }
            });
            Task<bool> t1 = bb.SendAsync(1);
            Task<bool> t2 = bb.SendAsync(2);
            await Assert.ThrowsAsync<TaskSchedulerException>(() => bb.Completion);
            Assert.False(await t1);
            Assert.False(await t2);
        }
    }
}
