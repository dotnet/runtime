// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xunit;

namespace System.Threading.Tasks.Dataflow.Tests
{
    public class TransformManyBlockTests
    {
        [Fact]
        public async Task TestCtor()
        {
            var blocks = new[] {
                new TransformManyBlock<int, int>(DataflowTestHelpers.ToEnumerable),
                new TransformManyBlock<int, int>(DataflowTestHelpers.ToEnumerable, new ExecutionDataflowBlockOptions { MaxMessagesPerTask = 1 }),
                new TransformManyBlock<int, int>(i => Task.Run(() => DataflowTestHelpers.ToEnumerable(i)), new ExecutionDataflowBlockOptions { MaxMessagesPerTask = 1 })
            };
            foreach (var block in blocks)
            {
                Assert.Equal(expected: 0, actual: block.InputCount);
                Assert.Equal(expected: 0, actual: block.OutputCount);
                Assert.False(block.Completion.IsCompleted);
            }

            blocks = new[] {
                new TransformManyBlock<int, int>(DataflowTestHelpers.ToEnumerable, 
                    new ExecutionDataflowBlockOptions { CancellationToken = new CancellationToken(true) }),
                new TransformManyBlock<int, int>(i => Task.Run(() => DataflowTestHelpers.ToEnumerable(i)), 
                    new ExecutionDataflowBlockOptions { CancellationToken = new CancellationToken(true) })
            };
            foreach (var block in blocks)
            {
                Assert.Equal(expected: 0, actual: block.InputCount);
                Assert.Equal(expected: 0, actual: block.OutputCount);
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => block.Completion);
            }
        }

        [Fact]
        public void TestArgumentExceptions()
        {
            Assert.Throws<ArgumentNullException>(() => new TransformManyBlock<int, int>((Func<int, IEnumerable<int>>)null));
            Assert.Throws<ArgumentNullException>(() => new TransformManyBlock<int, int>((Func<int, Task<IEnumerable<int>>>)null));
            Assert.Throws<ArgumentNullException>(() => new TransformManyBlock<int, int>(DataflowTestHelpers.ToEnumerable, null));
            Assert.Throws<ArgumentNullException>(() => new TransformManyBlock<int, int>(i => Task.Run(() => DataflowTestHelpers.ToEnumerable(i)), null));

            DataflowTestHelpers.TestArgumentsExceptions(new TransformManyBlock<int, int>(DataflowTestHelpers.ToEnumerable));
        }

        [Fact]
        public void TestToString()
        {
            DataflowTestHelpers.TestToString(nameFormat =>
                nameFormat != null ?
                    new TransformManyBlock<int, int>(DataflowTestHelpers.ToEnumerable, new ExecutionDataflowBlockOptions() { NameFormat = nameFormat }) :
                    new TransformManyBlock<int, int>(DataflowTestHelpers.ToEnumerable));
        }

        [Fact]
        public async Task TestOfferMessage()
        {
            var generators = new Func<TransformManyBlock<int, int>>[]
            {
                () => new TransformManyBlock<int, int>(DataflowTestHelpers.ToEnumerable),
                () => new TransformManyBlock<int, int>(DataflowTestHelpers.ToEnumerable, new ExecutionDataflowBlockOptions { BoundedCapacity = 10 }),
                () => new TransformManyBlock<int, int>(i => Task.Run(() => DataflowTestHelpers.ToEnumerable(i)), new ExecutionDataflowBlockOptions { BoundedCapacity = 10, MaxMessagesPerTask = 1 })
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
        public void TestPost()
        {
            foreach (bool bounded in DataflowTestHelpers.BooleanValues)
                foreach (var tb in new[] { 
                new TransformManyBlock<int, int>(DataflowTestHelpers.ToEnumerable, new ExecutionDataflowBlockOptions { BoundedCapacity = bounded ? 1 : -1 }),
                new TransformManyBlock<int, int>(i => Task.Run(() => DataflowTestHelpers.ToEnumerable(i)), new ExecutionDataflowBlockOptions { BoundedCapacity = bounded ? 1 : -1 })})
                {
                    Assert.True(tb.Post(0));
                    tb.Complete();
                    Assert.False(tb.Post(0));
                }
        }

        [Fact]
        public Task TestCompletionTask()
        {
            return DataflowTestHelpers.TestCompletionTask(() => new TransformManyBlock<int, int>(DataflowTestHelpers.ToEnumerable));
        }

        [Fact]
        public async Task TestLinkToOptions()
        {
            const int Messages = 1;
            foreach (bool append in DataflowTestHelpers.BooleanValues)
            foreach (var tb in new[] {
                new TransformManyBlock<int, int>(DataflowTestHelpers.ToEnumerable), 
                new TransformManyBlock<int, int>(i => Task.Run(() => DataflowTestHelpers.ToEnumerable(i))) })
            {
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
        public async Task TestReceives()
        {
            for (int test = 0; test < 2; test++)
            {
                foreach (var tb in new[] { 
                    new TransformManyBlock<int, int>(i => Enumerable.Repeat(i * 2, 1)),
                    new TransformManyBlock<int, int>(i => Task.Run(() => Enumerable.Repeat(i * 2, 1))) })
                {
                    tb.PostRange(0, 5);

                    for (int i = 0; i < 5; i++)
                    {
                        Assert.Equal(expected: i * 2, actual: await tb.ReceiveAsync());
                    }

                    int item;
                    IList<int> items;
                    Assert.False(tb.TryReceive(out item));
                    Assert.False(tb.TryReceiveAll(out items));
                }
            }
        }

        [Fact]
        public async Task TestCircularLinking()
        {
            const int Iters = 200;

            foreach (bool sync in DataflowTestHelpers.BooleanValues)
            {
                var tcs = new TaskCompletionSource<bool>();
                Func<int, IEnumerable<int>> body = i => {
                    if (i >= Iters) tcs.SetResult(true);
                    return Enumerable.Repeat(i + 1, 1);
                };

                TransformManyBlock<int, int> tb = sync ?
                    new TransformManyBlock<int, int>(body) :
                    new TransformManyBlock<int, int>(i => Task.Run(() => body(i)));

                using (tb.LinkTo(tb))
                {
                    tb.Post(0);
                    await tcs.Task;
                    tb.Complete();
                }
            }
        }

        [Fact]
        public async Task TestProducerConsumer()
        {
            foreach (TaskScheduler scheduler in new[] { TaskScheduler.Default, new ConcurrentExclusiveSchedulerPair().ConcurrentScheduler })
            foreach (int maxMessagesPerTask in new[] { DataflowBlockOptions.Unbounded, 1, 2 })
            foreach (int boundedCapacity in new[] { DataflowBlockOptions.Unbounded, 1, 2 })
            foreach (int dop in new[] { 1, 2 })
            foreach (int elementsPerItem in new[] { 1, 3, 5 })
            foreach (bool sync in DataflowTestHelpers.BooleanValues)
            {
                const int Messages = 50;
                var options = new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = boundedCapacity,
                    MaxDegreeOfParallelism = dop,
                    MaxMessagesPerTask = maxMessagesPerTask,
                    TaskScheduler = scheduler
                };
                TransformManyBlock<int, int> tb = sync ?
                    new TransformManyBlock<int, int>(i => Enumerable.Repeat(i, elementsPerItem), options) :
                    new TransformManyBlock<int, int>(i => Task.Run(() => Enumerable.Repeat(i, elementsPerItem)), options);

                await Task.WhenAll(
                    Task.Run(async delegate { // consumer
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
                    }),
                    Task.Run(async delegate { // producer
                        for (int i = 0; i < Messages; i++)
                        {
                            await tb.SendAsync(i);
                        }
                        tb.Complete();
                    }));
            }
        }

        [Fact]
        public async Task TestMessagePostponement()
        {
            const int Excess = 10;
            foreach (int boundedCapacity in new[] { 1, 3 })
            {
                var options = new ExecutionDataflowBlockOptions { BoundedCapacity = boundedCapacity };
                foreach (var tb in new[] { 
                    new TransformManyBlock<int, int>(DataflowTestHelpers.ToEnumerable, options), 
                    new TransformManyBlock<int, int>(i => Task.Run(() => DataflowTestHelpers.ToEnumerable(i)), options) })
                {
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
        }

        [Fact]
        public async Task TestMultipleYields()
        {
            const int Messages = 10;

            var t = new TransformManyBlock<int, int>(i => Enumerable.Range(0, Messages));
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
        public async Task TestReserveReleaseConsume()
        {
            var tb = new TransformManyBlock<int, int>(DataflowTestHelpers.ToEnumerable);
            tb.Post(1);
            await DataflowTestHelpers.TestReserveAndRelease(tb);

            tb = new TransformManyBlock<int, int>(DataflowTestHelpers.ToEnumerable);
            tb.Post(2);
            await DataflowTestHelpers.TestReserveAndConsume(tb);
        }

        [Fact]
        public async Task TestCountZeroAtCompletion()
        {
            var cts = new CancellationTokenSource();
            var tb = new TransformManyBlock<int, int>(DataflowTestHelpers.ToEnumerable, new ExecutionDataflowBlockOptions() { CancellationToken = cts.Token });
            tb.Post(1);
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => tb.Completion);
            Assert.Equal(expected: 0, actual: tb.InputCount);
            Assert.Equal(expected: 0, actual: tb.OutputCount);

            cts = new CancellationTokenSource();
            tb = new TransformManyBlock<int, int>(DataflowTestHelpers.ToEnumerable);
            tb.Post(1);
            ((IDataflowBlock)tb).Fault(new InvalidOperationException());
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => tb.Completion);
            Assert.Equal(expected: 0, actual: tb.InputCount);
            Assert.Equal(expected: 0, actual: tb.OutputCount);
        }

        [Fact]
        public void TestInputCount()
        {
            foreach (bool sync in DataflowTestHelpers.BooleanValues)
            {
                Barrier barrier1 = new Barrier(2), barrier2 = new Barrier(2);
                Func<int, IEnumerable<int>> body = item => {
                    barrier1.SignalAndWait();
                    // will test InputCount here
                    barrier2.SignalAndWait();
                    return new[] { item };
                };

                TransformManyBlock<int, int> tb = sync ?
                    new TransformManyBlock<int, int>(body) :
                    new TransformManyBlock<int, int>(i => Task.Run(() => body(i)));

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
        }

        [Fact]
        [OuterLoop] // spins waiting for a condition to be true, though it should happen very quickly
        public async Task TestCount()
        {
            var tb = new TransformManyBlock<int, int>(DataflowTestHelpers.ToEnumerable);
            Assert.Equal(expected: 0, actual: tb.InputCount);
            Assert.Equal(expected: 0, actual: tb.OutputCount);

            tb.PostRange(1, 11);
            await Task.Run(() => SpinWait.SpinUntil(() => tb.OutputCount == 10));
            for (int i = 10; i > 0; i--)
            {
                int item;
                Assert.True(tb.TryReceive(out item));
                Assert.Equal(expected: 11 - i, actual: item);
                Assert.Equal(expected: i - 1, actual: tb.OutputCount);
            }
        }

        [Fact]
        public async Task TestChainedSendReceive()
        {
            foreach (bool post in DataflowTestHelpers.BooleanValues)
            foreach (bool sync in DataflowTestHelpers.BooleanValues)
            {
                Func<TransformManyBlock<int, int>> func = sync ?
                    (Func<TransformManyBlock<int, int>>)(() => new TransformManyBlock<int, int>(i => new[] { i * 2 })) :
                    (Func<TransformManyBlock<int, int>>)(() => new TransformManyBlock<int, int>(i => Task.Run(() => Enumerable.Repeat(i * 2, 1))));
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
        public async Task TestSendAllThenReceive()
        {
            foreach (bool post in DataflowTestHelpers.BooleanValues)
            foreach (bool sync in DataflowTestHelpers.BooleanValues)
            {
                Func<TransformManyBlock<int, int>> func = sync ?
                    (Func<TransformManyBlock<int, int>>)(() => new TransformManyBlock<int, int>(i => new[] { i * 2 })) :
                    (Func<TransformManyBlock<int, int>>)(() => new TransformManyBlock<int, int>(i => Task.Run(() => Enumerable.Repeat(i * 2, 1))));
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
        public async Task TestPrecanceled()
        {
            var bb = new TransformManyBlock<int, int>(DataflowTestHelpers.ToEnumerable,
                new ExecutionDataflowBlockOptions { CancellationToken = new CancellationToken(canceled: true) });

            int ignoredValue;
            IList<int> ignoredValues;

            IDisposable link = bb.LinkTo(DataflowBlock.NullTarget<int>());
            Assert.NotNull(link);
            link.Dispose();

            Assert.False(bb.Post(42));
            var t = bb.SendAsync(42);
            Assert.True(t.IsCompleted);
            Assert.False(t.Result);

            Assert.False(bb.TryReceiveAll(out ignoredValues));
            Assert.False(bb.TryReceive(out ignoredValue));

            Assert.NotNull(bb.Completion);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => bb.Completion);
            bb.Complete(); // just make sure it doesn't throw
        }

        [Fact]
        public async Task TestExceptions()
        {
            var tb1 = new TransformManyBlock<int, int>((Func<int, IEnumerable<int>>)(i => { throw new InvalidCastException(); }));
            var tb2 = new TransformManyBlock<int, int>((Func<int, Task<IEnumerable<int>>>)(i => { throw new InvalidProgramException(); }));
            var tb3 = new TransformManyBlock<int, int>((Func<int, Task<IEnumerable<int>>>)(i => Task.Run((Func<IEnumerable<int>>)(() => { throw new InvalidTimeZoneException(); }))));
            var tb4 = new TransformManyBlock<int, int>(i => ExceptionAfter(3));
            var tb5 = new TransformManyBlock<int, int>(i => Task.Run(() => ExceptionAfter(3)));

            for (int i = 0; i < 3; i++)
            {
                tb1.Post(i);
                tb2.Post(i);
                tb3.Post(i);
                tb4.Post(i);
                tb5.Post(i);
            }

            await Assert.ThrowsAsync<InvalidCastException>(() => tb1.Completion);
            await Assert.ThrowsAsync<InvalidProgramException>(() => tb2.Completion);
            await Assert.ThrowsAsync<InvalidTimeZoneException>(() => tb3.Completion);
            await Assert.ThrowsAsync<FormatException>(() => tb4.Completion);
            await Assert.ThrowsAsync<FormatException>(() => tb5.Completion);

            Assert.All(new[] { tb1, tb2, tb3 }, tb => Assert.True(tb.InputCount == 0 && tb.OutputCount == 0));
        }

        private IEnumerable<int> ExceptionAfter(int iterations)
        {
            for (int i = 0; i < iterations; i++)
            {
                yield return i;
            }
            throw new FormatException();
        }

        [Fact]
        public async Task TestFaultingAndCancellation()
        {
            foreach (bool fault in DataflowTestHelpers.BooleanValues)
            {
                var cts = new CancellationTokenSource();
                var tb = new TransformManyBlock<int, int>(DataflowTestHelpers.ToEnumerable, new ExecutionDataflowBlockOptions { CancellationToken = cts.Token });
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
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => tb.Completion);
                }

                Assert.Equal(expected: 0, actual: tb.InputCount);
                Assert.Equal(expected: 0, actual: tb.OutputCount);
            }
        }

        [Fact]
        public async Task TestCancellationExceptionsIgnored()
        {
            foreach (bool sync in DataflowTestHelpers.BooleanValues)
            {
                Func<int, IEnumerable<int>> body = i => {
                    if ((i % 2) == 0) throw new OperationCanceledException();
                    return new[] { i };
                };

                TransformManyBlock<int, int> t = sync ?
                    new TransformManyBlock<int, int>(body) :
                    new TransformManyBlock<int, int>(async i => await Task.Run(() => body(i)));

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
        }

        [Fact]
        public async Task TestNullTasksIgnored()
        {
            foreach (int dop in new[] { DataflowBlockOptions.Unbounded, 1, 2 })
            {
                var tb = new TransformManyBlock<int, int>(i => {
                    if ((i % 2) == 0) return null;
                    return Task.Run(() => (IEnumerable<int>)new[] { i });
                }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = dop });
 
                const int Iters = 100;
                tb.PostRange(0, Iters);
                tb.Complete();

                for (int i = 0; i < Iters; i++)
                {
                    if ((i % 2) != 0)
                    {
                        Assert.Equal(expected: i, actual: await tb.ReceiveAsync());
                    }
                }
                await tb.Completion;
            }
        }

        [Fact]
        public async Task TestYieldingNoResults()
        {
            foreach (int dop in new[] { 1, Environment.ProcessorCount })
            foreach (int boundedCapacity in new[] { DataflowBlockOptions.Unbounded, 1, 2 })
            {
                const int Modes = 3, Iters = 100;
                var tb = new TransformManyBlock<int, int>(i => {
                    switch (i % Modes)
                    {
                        default:
                        case 0:
                            return new List<int> { i };
                        case 1:
                            return new int[0];
                        case 2:
                            return new Collection<int> { i, i + 1 };
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
        public async Task TestArrayListReusePossibleForDop1()
        {
            foreach (int boundedCapacity in new[] { DataflowBlockOptions.Unbounded, 2 })
            foreach (bool sync in DataflowTestHelpers.BooleanValues)
            {
                foreach (int dop in new[] { 1, Environment.ProcessorCount })
                {
                    var dbo = new ExecutionDataflowBlockOptions { BoundedCapacity = boundedCapacity, MaxDegreeOfParallelism = dop };
                    foreach (IList<int> list in new IList<int>[] { new int[1], new List<int> { 0 }, new Collection<int> { 0 } })
                    {
                        int nextExpectedValue = 1;

                        TransformManyBlock<int, int> transform = null;
                        Func<int, IEnumerable<int>> body = i => {
                            if (i == 100) // we're done iterating
                            {
                                transform.Complete();
                                return (IEnumerable<int>)null;
                            }
                            else if (dop == 1)
                            {
                                list[0] = i + 1; // reuse the list over and over, but only at dop == 1
                                return (IEnumerable<int>)list;
                            }
                            else if (list is int[])
                            {
                                return new int[1] { i + 1 };
                            }
                            else if (list is List<int>)
                            {
                                return new List<int>() { i + 1 };
                            }
                            else
                            {
                                return new Collection<int>() { i + 1 };
                            }
                        };

                        transform = sync ?
                            new TransformManyBlock<int, int>(body, dbo) :
                            new TransformManyBlock<int, int>(i => Task.Run(() => body(i)), dbo);

                        TransformBlock<int, int> verifier = new TransformBlock<int, int>(i => {
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
        public async Task TestOrdering_Sync_OrderedEnabled(int mmpt, int dop, bool? EnsureOrdered)
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

            var tb = new TransformManyBlock<int, int>(i => new[] { i }, options);
            tb.PostRange(0, iters);
            for (int i = 0; i < iters; i++)
            {
                Assert.Equal(expected: i, actual: await tb.ReceiveAsync());
            }
            tb.Complete();
            await tb.Completion;
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
        public async Task TestOrdering_Async_OrderedEnabled(int mmpt, int dop, bool? EnsureOrdered)
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

            var tb = new TransformManyBlock<int, int>(i => Task.FromResult(Enumerable.Repeat(i, 1)), options);
            tb.PostRange(0, iters);
            for (int i = 0; i < iters; i++)
            {
                Assert.Equal(expected: i, actual: await tb.ReceiveAsync());
            }
            tb.Complete();
            await tb.Completion;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestOrdering_Async_OrderedDisabled(bool trustedEnumeration)
        {
            // If ordering were enabled, this test would hang.

            var options = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded, EnsureOrdered = false };

            var tasks = new TaskCompletionSource<IEnumerable<int>>[10];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = new TaskCompletionSource<IEnumerable<int>>();
            }

            var tb = new TransformManyBlock<int, int>(i => tasks[i].Task, options);
            tb.PostRange(0, tasks.Length);

            for (int i = tasks.Length - 1; i >= 0; i--)
            {
                tasks[i].SetResult(trustedEnumeration ?
                    new[] { i } :
                    Enumerable.Repeat(i, 1));
                Assert.Equal(expected: i, actual: await tb.ReceiveAsync());
            }

            tb.Complete();
            await tb.Completion;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestOrdering_Sync_OrderedDisabled(bool trustedEnumeration)
        {
            // If ordering were enabled, this test would hang.

            var options = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 2, EnsureOrdered = false };

            var mres = new ManualResetEventSlim();
            var tb = new TransformManyBlock<int, int>(i =>
            {
                if (i == 0) mres.Wait();
                return trustedEnumeration ?
                    new[] { i } :
                    Enumerable.Repeat(i, 1);
            }, options);
            tb.Post(0);
            tb.Post(1);

            Assert.Equal(1, await tb.ReceiveAsync());
            mres.Set();
            Assert.Equal(0, await tb.ReceiveAsync());

            tb.Complete();
            await tb.Completion;
        }
    }
}
