// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace System.Threading.Tasks.Dataflow.Tests
{
    public partial class TransformManyBlockTests
    {
        [Fact]
        public async Task TestCtorAsyncEnumerable()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var blocks = new[] {
                new TransformManyBlock<int, int>(DataflowTestHelpers.ToAsyncEnumerable),
                new TransformManyBlock<int, int>(DataflowTestHelpers.ToAsyncEnumerable, new ExecutionDataflowBlockOptions { MaxMessagesPerTask = 1 })
            };
            foreach (var block in blocks)
            {
                Assert.Equal(expected: 0, actual: block.InputCount);
                Assert.Equal(expected: 0, actual: block.OutputCount);
                Assert.False(block.Completion.IsCompleted);
            }

            var canceledBlock = new TransformManyBlock<int, int>(
                DataflowTestHelpers.ToAsyncEnumerable,
                new ExecutionDataflowBlockOptions { CancellationToken = cts.Token });

            Assert.Equal(expected: 0, actual: canceledBlock.InputCount);
            Assert.Equal(expected: 0, actual: canceledBlock.OutputCount);
            await AssertExtensions.CanceledAsync(cts.Token, canceledBlock.Completion);
        }

        [Fact]
        public void TestArgumentExceptionsAsyncEnumerable()
        {
            Assert.Throws<ArgumentNullException>(() => new TransformManyBlock<int, int>((Func<int, IAsyncEnumerable<int>>)null));
            Assert.Throws<ArgumentNullException>(() => new TransformManyBlock<int, int>(DataflowTestHelpers.ToAsyncEnumerable, null));

            DataflowTestHelpers.TestArgumentsExceptions(new TransformManyBlock<int, int>(DataflowTestHelpers.ToAsyncEnumerable));
        }

        [Fact]
        public void TestToStringAsyncEnumerable()
        {
            DataflowTestHelpers.TestToString(nameFormat =>
                nameFormat != null ?
                    new TransformManyBlock<int, int>(DataflowTestHelpers.ToAsyncEnumerable, new ExecutionDataflowBlockOptions() { NameFormat = nameFormat }) :
                    new TransformManyBlock<int, int>(DataflowTestHelpers.ToAsyncEnumerable));
        }

        [Fact]
        public async Task TestOfferMessageAsyncEnumerable()
        {
            var generators = new Func<TransformManyBlock<int, int>>[]
            {
                () => new TransformManyBlock<int, int>(DataflowTestHelpers.ToAsyncEnumerable),
                () => new TransformManyBlock<int, int>(DataflowTestHelpers.ToAsyncEnumerable, new ExecutionDataflowBlockOptions { BoundedCapacity = 10 }),
                () => new TransformManyBlock<int, int>(DataflowTestHelpers.ToAsyncEnumerable, new ExecutionDataflowBlockOptions { BoundedCapacity = 10, MaxMessagesPerTask = 1 }),
            };
            foreach (var generator in generators)
            {
                DataflowTestHelpers.TestOfferMessage_ArgumentValidation(generator());

                var target = generator();
                DataflowTestHelpers.TestOfferMessage_AcceptsDataDirectly(target);
                DataflowTestHelpers.TestOfferMessage_CompleteAndOffer(target);

                target = generator();
                await DataflowTestHelpers.TestOfferMessage_AcceptsViaLinking(target);
                DataflowTestHelpers.TestOfferMessage_CompleteAndOffer(target);
            }
        }

        [Fact]
        public void TestPostAsyncEnumerable()
        {
            foreach (bool bounded in DataflowTestHelpers.BooleanValues)
            {
                var tb = new TransformManyBlock<int, int>(DataflowTestHelpers.ToAsyncEnumerable, new ExecutionDataflowBlockOptions { BoundedCapacity = bounded ? 1 : -1 });
                Assert.True(tb.Post(0));
                tb.Complete();
                Assert.False(tb.Post(0));
            }
        }

        [Fact]
        public Task TestCompletionTaskAsyncEnumerable()
        {
            return DataflowTestHelpers.TestCompletionTask(() => new TransformManyBlock<int, int>(DataflowTestHelpers.ToAsyncEnumerable));
        }

        [Fact]
        public async Task TestLinkToOptionsAsyncEnumerable()
        {
            const int Messages = 1;
            foreach (bool append in DataflowTestHelpers.BooleanValues)
            {
                var tb = new TransformManyBlock<int, int>(DataflowTestHelpers.ToAsyncEnumerable);
                var values = new int[Messages];
                var targets = new ActionBlock<int>[Messages];
                for (int i = 0; i < Messages; i++)
                {
                    int slot = i;
                    targets[i] = new ActionBlock<int>(item => values[slot] = item);
                    tb.LinkTo(targets[i], new DataflowLinkOptions { MaxMessages = 1, Append = append });
                }

                tb.PostRange(0, Messages);
                tb.Complete();
                await tb.Completion;

                for (int i = 0; i < Messages; i++)
                {
                    Assert.Equal(
                        expected: append ? i : Messages - i - 1,
                        actual: values[i]);
                }
            }
        }

        [Fact]
        public async Task TestReceivesAsyncEnumerable()
        {
            for (int test = 0; test < 2; test++)
            {
                var tb = new TransformManyBlock<int, int>(i => AsyncEnumerable.Repeat(i * 2, 1));
                tb.PostRange(0, 5);

                for (int i = 0; i < 5; i++)
                {
                    Assert.Equal(expected: i * 2, actual: await tb.ReceiveAsync());
                }

                Assert.False(tb.TryReceive(out _));
                Assert.False(tb.TryReceiveAll(out _));
            }
        }

        [Fact]
        public async Task TestCircularLinkingAsyncEnumerable()
        {
            const int Iters = 200;

            var tcs = new TaskCompletionSource<bool>();
            IAsyncEnumerable<int> body(int i)
            {
                if (i >= Iters) tcs.SetResult(true);
                return AsyncEnumerable.Repeat(i + 1, 1);
            }

            TransformManyBlock<int, int> tb = new TransformManyBlock<int, int>(body);

            using (tb.LinkTo(tb))
            {
                tb.Post(0);
                await tcs.Task;
                tb.Complete();
            }
        }

        [Fact]
        public async Task TestProducerConsumerAsyncEnumerable()
        {
            foreach (TaskScheduler scheduler in new[] { TaskScheduler.Default, new ConcurrentExclusiveSchedulerPair().ConcurrentScheduler })
            foreach (int maxMessagesPerTask in new[] { DataflowBlockOptions.Unbounded, 1, 2 })
            foreach (int boundedCapacity in new[] { DataflowBlockOptions.Unbounded, 1, 2 })
            foreach (int dop in new[] { 1, 2, 8 })
            foreach (int elementsPerItem in new[] { 1, 5, 100 })
            foreach (bool ensureOrdered in DataflowTestHelpers.BooleanValues)
            {
                const int Messages = 100;
                var options = new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = boundedCapacity,
                    MaxDegreeOfParallelism = dop,
                    MaxMessagesPerTask = maxMessagesPerTask,
                    TaskScheduler = scheduler,
                    EnsureOrdered = ensureOrdered,
                };
                TransformManyBlock<int, int> tb = new TransformManyBlock<int, int>(i => AsyncEnumerable.Repeat(i, elementsPerItem), options);

                await Task.WhenAll(
                    Task.Run(async delegate // consumer
                    {
                        if (ensureOrdered)
                        {
                            int i = 0;
                            int processed = 0;
                            while (await tb.OutputAvailableAsync())
                            {
                                Assert.Equal(expected: i, actual: await tb.ReceiveAsync());
                                processed++;
                                if (processed % elementsPerItem == 0)
                                {
                                    i++;
                                }
                            }
                        }
                        else
                        {
                            var results = new List<int>();
                            await foreach (int result in tb.ReceiveAllAsync())
                            {
                                results.Add(result);
                            }

                            IEnumerable<IGrouping<int, int>> messages = results.GroupBy(i => i);
                            Assert.Equal(Messages, messages.Count());
                            Assert.All(messages, m => Assert.Equal(elementsPerItem, m.Count()));
                        }
                    }),
                    Task.Run(async delegate // producer
                    {
                        for (int i = 0; i < Messages; i++)
                        {
                            await tb.SendAsync(i);
                        }
                        tb.Complete();
                    }));
            }
        }

        [Fact]
        public async Task TestMessagePostponementAsyncEnumerable()
        {
            const int Excess = 10;
            foreach (int boundedCapacity in new[] { 1, 3 })
            {
                var options = new ExecutionDataflowBlockOptions { BoundedCapacity = boundedCapacity };
                var tb = new TransformManyBlock<int, int>(DataflowTestHelpers.ToAsyncEnumerable, options);

                var sendAsync = new Task<bool>[boundedCapacity + Excess];
                for (int i = 0; i < boundedCapacity + Excess; i++)
                {
                    sendAsync[i] = tb.SendAsync(i);
                }
                tb.Complete();

                for (int i = 0; i < boundedCapacity; i++)
                {
                    Assert.True(sendAsync[i].IsCompleted);
                    Assert.True(sendAsync[i].Result);
                }

                for (int i = 0; i < Excess; i++)
                {
                    Assert.False(await sendAsync[boundedCapacity + i]);
                }
            }
        }

        [Fact]
        public async Task TestMultipleYieldsAsyncEnumerable()
        {
            const int Messages = 10;

            var t = new TransformManyBlock<int, int>(i => AsyncEnumerable.Range(0, Messages));
            t.Post(42);
            t.Complete();

            for (int i = 0; i < Messages; i++)
            {
                Assert.False(t.Completion.IsCompleted);
                Assert.Equal(expected: i, actual: await t.ReceiveAsync());
            }
            await t.Completion;
        }

        [Fact]
        public async Task TestReserveReleaseConsumeAsyncEnumerable()
        {
            var tb = new TransformManyBlock<int, int>(DataflowTestHelpers.ToAsyncEnumerable);
            tb.Post(1);
            await DataflowTestHelpers.TestReserveAndRelease(tb);

            tb = new TransformManyBlock<int, int>(DataflowTestHelpers.ToAsyncEnumerable);
            tb.Post(2);
            await DataflowTestHelpers.TestReserveAndConsume(tb);
        }

        [Fact]
        public async Task TestCountZeroAtCompletionAsyncEnumerable()
        {
            var cts = new CancellationTokenSource();
            var tb = new TransformManyBlock<int, int>(DataflowTestHelpers.ToAsyncEnumerable, new ExecutionDataflowBlockOptions() { CancellationToken = cts.Token });
            tb.Post(1);
            cts.Cancel();
            await AssertExtensions.CanceledAsync(cts.Token, tb.Completion);
            Assert.Equal(expected: 0, actual: tb.InputCount);
            Assert.Equal(expected: 0, actual: tb.OutputCount);

            tb = new TransformManyBlock<int, int>(DataflowTestHelpers.ToAsyncEnumerable);
            tb.Post(1);
            ((IDataflowBlock)tb).Fault(new InvalidOperationException());
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => tb.Completion);
            Assert.Equal(expected: 0, actual: tb.InputCount);
            Assert.Equal(expected: 0, actual: tb.OutputCount);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedAndBlockingWait))]
        public void TestInputCountAsyncEnumerable()
        {
            using Barrier barrier1 = new Barrier(2), barrier2 = new Barrier(2);
            IAsyncEnumerable<int> body(int item)
            {
                barrier1.SignalAndWait();
                // will test InputCount here
                barrier2.SignalAndWait();
                return DataflowTestHelpers.ToAsyncEnumerable(item);
            }

            TransformManyBlock<int, int> tb = new TransformManyBlock<int, int>(body);

            for (int iter = 0; iter < 2; iter++)
            {
                tb.PostItems(1, 2);
                for (int i = 1; i >= 0; i--)
                {
                    barrier1.SignalAndWait();
                    Assert.Equal(expected: i, actual: tb.InputCount);
                    barrier2.SignalAndWait();
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedAndBlockingWait))]
        [OuterLoop] // spins waiting for a condition to be true, though it should happen very quickly
        public async Task TestCountAsyncEnumerable()
        {
            var tb = new TransformManyBlock<int, int>(DataflowTestHelpers.ToAsyncEnumerable);
            Assert.Equal(expected: 0, actual: tb.InputCount);
            Assert.Equal(expected: 0, actual: tb.OutputCount);

            tb.PostRange(1, 11);
            await Task.Run(() => SpinWait.SpinUntil(() => tb.OutputCount == 10));
            for (int i = 10; i > 0; i--)
            {
                Assert.True(tb.TryReceive(out int item));
                Assert.Equal(expected: 11 - i, actual: item);
                Assert.Equal(expected: i - 1, actual: tb.OutputCount);
            }
        }

        [Fact]
        public async Task TestChainedSendReceiveAsyncEnumerable()
        {
            foreach (bool post in DataflowTestHelpers.BooleanValues)
            {
                static TransformManyBlock<int, int> func() => new TransformManyBlock<int, int>(i => AsyncEnumerable.Repeat(i * 2, 1));
                var network = DataflowTestHelpers.Chain<TransformManyBlock<int, int>, int>(4, func);

                const int Iters = 10;
                for (int i = 0; i < Iters; i++)
                {
                    if (post)
                    {
                        network.Post(i);
                    }
                    else
                    {
                        await network.SendAsync(i);
                    }
                    Assert.Equal(expected: i * 16, actual: await network.ReceiveAsync());
                }
            }
        }

        [Fact]
        public async Task TestSendAllThenReceiveAsyncEnumerable()
        {
            foreach (bool post in DataflowTestHelpers.BooleanValues)
            {
                static TransformManyBlock<int, int> func() => new TransformManyBlock<int, int>(i => AsyncEnumerable.Repeat(i * 2, 1));
                var network = DataflowTestHelpers.Chain<TransformManyBlock<int, int>, int>(4, func);

                const int Iters = 10;
                if (post)
                {
                    network.PostRange(0, Iters);
                }
                else
                {
                    await Task.WhenAll(from i in Enumerable.Range(0, Iters) select network.SendAsync(i));
                }

                for (int i = 0; i < Iters; i++)
                {
                    Assert.Equal(expected: i * 16, actual: await network.ReceiveAsync());
                }
            }
        }

        [Fact]
        public async Task TestPrecanceledAsyncEnumerable()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var bb = new TransformManyBlock<int, int>(DataflowTestHelpers.ToAsyncEnumerable,
                new ExecutionDataflowBlockOptions { CancellationToken = cts.Token });

            IDisposable link = bb.LinkTo(DataflowBlock.NullTarget<int>());
            Assert.NotNull(link);
            link.Dispose();

            Assert.False(bb.Post(42));
            var t = bb.SendAsync(42);
            Assert.True(t.IsCompleted);
            Assert.False(t.Result);

            Assert.False(bb.TryReceiveAll(out _));
            Assert.False(bb.TryReceive(out _));

            Assert.NotNull(bb.Completion);
            await AssertExtensions.CanceledAsync(cts.Token, bb.Completion);
            bb.Complete(); // just make sure it doesn't throw
        }

        [Fact]
        public async Task TestExceptionsAsyncEnumerable()
        {
            var tb1 = new TransformManyBlock<int, int>((Func<int, IAsyncEnumerable<int>>)(i => { throw new InvalidCastException(); }));
            var tb2 = new TransformManyBlock<int, int>(i => ExceptionAfterAsync(3));

            for (int i = 0; i < 3; i++)
            {
                tb1.Post(i);
                tb2.Post(i);
            }

            await Assert.ThrowsAsync<InvalidCastException>(() => tb1.Completion);
            await Assert.ThrowsAsync<FormatException>(() => tb2.Completion);

            Assert.True(tb1.InputCount == 0 && tb1.OutputCount == 0);
        }

        private async IAsyncEnumerable<int> ExceptionAfterAsync(int iterations)
        {
            for (int i = 0; i < iterations; i++)
            {
                await Task.Yield();
                yield return i;
            }
            throw new FormatException();
        }

        [Fact]
        public async Task TestFaultingAndCancellationAsyncEnumerable()
        {
            foreach (bool fault in DataflowTestHelpers.BooleanValues)
            {
                var cts = new CancellationTokenSource();
                var tb = new TransformManyBlock<int, int>(DataflowTestHelpers.ToAsyncEnumerable, new ExecutionDataflowBlockOptions { CancellationToken = cts.Token });
                tb.PostRange(0, 4);
                Assert.Equal(expected: 0, actual: await tb.ReceiveAsync());
                Assert.Equal(expected: 1, actual: await tb.ReceiveAsync());

                if (fault)
                {
                    Assert.Throws<ArgumentNullException>(() => ((IDataflowBlock)tb).Fault(null));
                    ((IDataflowBlock)tb).Fault(new InvalidCastException());
                    await Assert.ThrowsAsync<InvalidCastException>(() => tb.Completion);
                }
                else
                {
                    cts.Cancel();
                    await AssertExtensions.CanceledAsync(cts.Token, tb.Completion);
                }

                Assert.Equal(expected: 0, actual: tb.InputCount);
                Assert.Equal(expected: 0, actual: tb.OutputCount);
            }
        }

        [Fact]
        public async Task TestCancellationExceptionsIgnoredAsyncEnumerable()
        {
            static IAsyncEnumerable<int> body(int i)
            {
                if ((i % 2) == 0) throw new OperationCanceledException();
                return DataflowTestHelpers.ToAsyncEnumerable(i);
            }

            TransformManyBlock<int, int> t = new TransformManyBlock<int, int>(body);

            t.PostRange(0, 2);
            t.Complete();
            for (int i = 0; i < 2; i++)
            {
                if ((i % 2) != 0)
                {
                    Assert.Equal(expected: i, actual: await t.ReceiveAsync());
                }
            }

            await t.Completion;
        }

        [Fact]
        public async Task TestYieldingNoResultsAsyncEnumerable()
        {
            foreach (int dop in new[] { 1, Environment.ProcessorCount })
                foreach (int boundedCapacity in new[] { DataflowBlockOptions.Unbounded, 1, 2 })
                {
                    const int Modes = 3, Iters = 100;
                    var tb = new TransformManyBlock<int, int>(i =>
                    {
                        switch (i % Modes)
                        {
                            default:
                            case 0:
                                return AsyncEnumerable.Range(i, 1);
                            case 1:
                                return AsyncEnumerable.Range(i, 0);
                            case 2:
                                return AsyncEnumerable.Range(i, 2);
                        }
                    }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = dop, BoundedCapacity = boundedCapacity });

                    var source = new BufferBlock<int>();
                    source.PostRange(0, Modes * Iters);
                    source.Complete();
                    source.LinkTo(tb, new DataflowLinkOptions { PropagateCompletion = true });

                    int received = 0;
                    while (await tb.OutputAvailableAsync())
                    {
                        await tb.ReceiveAsync();
                        received++;
                    }
                    Assert.Equal(expected: Modes * Iters, actual: received);
                }
        }

        [Fact]
        public async Task TestArrayListReusePossibleForDop1AsyncEnumerable()
        {
            foreach (int boundedCapacity in new[] { DataflowBlockOptions.Unbounded, 2 })
            {
                foreach (int dop in new[] { 1, Environment.ProcessorCount })
                {
                    var dbo = new ExecutionDataflowBlockOptions { BoundedCapacity = boundedCapacity, MaxDegreeOfParallelism = dop };
                    foreach (IList<int> list in new IList<int>[] { new int[1], new List<int> { 0 }, new Collection<int> { 0 } })
                    {
                        int nextExpectedValue = 1;

                        TransformManyBlock<int, int> transform = null;
                        IAsyncEnumerable<int> body(int i)
                        {
                            if (i == 100) // we're done iterating
                            {
                                transform.Complete();
                                return null;
                            }
                            else if (dop == 1)
                            {
                                list[0] = i + 1; // reuse the list over and over, but only at dop == 1
                                return list.ToAsyncEnumerable();
                            }
                            else if (list is int[])
                            {
                                return new int[1] { i + 1 }.ToAsyncEnumerable();
                            }
                            else if (list is List<int>)
                            {
                                return new List<int>() { i + 1 }.ToAsyncEnumerable();
                            }
                            else
                            {
                                return new Collection<int>() { i + 1 }.ToAsyncEnumerable();
                            }
                        }

                        transform = new TransformManyBlock<int, int>(body, dbo);

                        TransformBlock<int, int> verifier = new TransformBlock<int, int>(i =>
                        {
                            Assert.Equal(expected: nextExpectedValue, actual: i);
                            nextExpectedValue++;
                            return i;
                        });

                        transform.LinkTo(verifier);
                        verifier.LinkTo(transform);

                        await transform.SendAsync(0);
                        await transform.Completion;
                    }
                }
            }
        }

        [Theory]
        [InlineData(DataflowBlockOptions.Unbounded, 1, null)]
        [InlineData(DataflowBlockOptions.Unbounded, 2, null)]
        [InlineData(DataflowBlockOptions.Unbounded, DataflowBlockOptions.Unbounded, null)]
        [InlineData(1, 1, null)]
        [InlineData(1, 2, null)]
        [InlineData(1, DataflowBlockOptions.Unbounded, null)]
        [InlineData(2, 2, true)]
        [InlineData(2, 1, false)] // no force ordered, but dop == 1, so it doesn't matter
        public async Task TestOrdering_Sync_OrderedEnabledAsyncEnumerable(int mmpt, int dop, bool? EnsureOrdered)
        {
            const int iters = 1000;

            var options = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = dop, MaxMessagesPerTask = mmpt };
            if (EnsureOrdered == null)
            {
                Assert.True(options.EnsureOrdered);
            }
            else
            {
                options.EnsureOrdered = EnsureOrdered.Value;
            }

            var tb = new TransformManyBlock<int, int>(i => DataflowTestHelpers.ToAsyncEnumerable(i), options);
            tb.PostRange(0, iters);
            for (int i = 0; i < iters; i++)
            {
                Assert.Equal(expected: i, actual: await tb.ReceiveAsync());
            }
            tb.Complete();
            await tb.Completion;
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedAndBlockingWait))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestOrdering_Sync_OrderedDisabledAsyncEnumerable(bool trustedEnumeration)
        {
            // If ordering were enabled, this test would hang.

            var options = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 2, EnsureOrdered = false };

            using var mres = new ManualResetEventSlim();
            var tb = new TransformManyBlock<int, int>(i =>
            {
                if (i == 0) mres.Wait();
                return trustedEnumeration ?
                    DataflowTestHelpers.ToAsyncEnumerable(i) :
                    AsyncEnumerable.Repeat(i, 1);
            }, options);
            tb.Post(0);
            tb.Post(1);

            Assert.Equal(1, await tb.ReceiveAsync());
            mres.Set();
            Assert.Equal(0, await tb.ReceiveAsync());

            tb.Complete();
            await tb.Completion;
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedAndBlockingWait))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TestOrdering_Sync_BlockingEnumeration_NoDeadlockAsyncEnumerable(bool ensureOrdered)
        {
            // If iteration of the yielded enumerables happened while holding a lock, this would deadlock.
            var options = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 2, EnsureOrdered = ensureOrdered };

            using ManualResetEventSlim mres1 = new ManualResetEventSlim(), mres2 = new ManualResetEventSlim();
            var tb = new TransformManyBlock<int, int>(i => i == 0 ? BlockableIterator(mres1, mres2) : BlockableIterator(mres2, mres1), options);
            tb.Post(0);
            tb.Post(1);
            Assert.Equal(42, await tb.ReceiveAsync());
            Assert.Equal(42, await tb.ReceiveAsync());

            tb.Complete();
            await tb.Completion;

            static IAsyncEnumerable<int> BlockableIterator(ManualResetEventSlim wait, ManualResetEventSlim release)
            {
                release.Set();
                wait.Wait();
                return DataflowTestHelpers.ToAsyncEnumerable(42);
            }
        }

        [Fact]
        public async Task TestScheduling_MoveNextAsync_RunsOnTargetScheduler()
        {
            TaskScheduler scheduler = new ConcurrentExclusiveSchedulerPair().ConcurrentScheduler;
            Assert.NotEqual(scheduler, TaskScheduler.Current);

            async IAsyncEnumerable<int> Body(int value)
            {
                Assert.Equal(scheduler, TaskScheduler.Current);
                await Task.Yield();
                Assert.Equal(scheduler, TaskScheduler.Current);
                yield return value;
                Assert.Equal(scheduler, TaskScheduler.Current);
            }

            TransformManyBlock<int, int> t = new TransformManyBlock<int, int>(Body, new ExecutionDataflowBlockOptions { TaskScheduler = scheduler });

            t.PostRange(0, 2);
            t.Complete();
            for (int i = 0; i < 2; i++)
            {
                Assert.Equal(expected: i, actual: await t.ReceiveAsync());
            }

            await t.Completion;
        }
    }
}
