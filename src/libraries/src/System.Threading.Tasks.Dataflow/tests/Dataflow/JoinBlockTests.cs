// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Threading.Tasks.Dataflow.Tests
{
    public class JoinBlockTests
    {
        [Fact]
        public void TestCtor2()
        {
            var blocks = new[]
            {
                new JoinBlock<int, string>(),
                new JoinBlock<int, string>(new GroupingDataflowBlockOptions { MaxNumberOfGroups = 1 }),
                new JoinBlock<int, string>(new GroupingDataflowBlockOptions { MaxMessagesPerTask = 1 }),
                new JoinBlock<int, string>(new GroupingDataflowBlockOptions { MaxMessagesPerTask = 1, CancellationToken = new CancellationToken(true), MaxNumberOfGroups = 1 })
            };
            foreach (var block in blocks)
            {
                Assert.Equal(expected: 0, actual: block.OutputCount);
                Assert.NotNull(block.Completion);
            }
        }

        [Fact]
        public void TestCtor3()
        {
            var blocks = new[]
            {
                new JoinBlock<int, string, double>(),
                new JoinBlock<int, string, double>(new GroupingDataflowBlockOptions { MaxNumberOfGroups = 1 }),
                new JoinBlock<int, string, double>(new GroupingDataflowBlockOptions { MaxMessagesPerTask = 1 }),
                new JoinBlock<int, string, double>(new GroupingDataflowBlockOptions { MaxMessagesPerTask = 1, CancellationToken = new CancellationToken(true), MaxNumberOfGroups = 1 })
            };
            foreach (var block in blocks)
            {
                Assert.Equal(expected: 0, actual: block.OutputCount);
                Assert.NotNull(block.Completion);
            }
        }

        [Fact]
        public void TestToString()
        {
            DataflowTestHelpers.TestToString(
                nameFormat => nameFormat != null ?
                    new JoinBlock<int, string>(new GroupingDataflowBlockOptions() { NameFormat = nameFormat }) :
                    new JoinBlock<int, string>());
            DataflowTestHelpers.TestToString(
                nameFormat => nameFormat != null ?
                    new JoinBlock<int, string, double>(new GroupingDataflowBlockOptions() { NameFormat = nameFormat }) :
                    new JoinBlock<int, string, double>());
        }

        [Fact]
        public void TestArgumentExceptions()
        {
            Assert.Throws<ArgumentNullException>(() => new JoinBlock<int, int>(null));
            Assert.Throws<ArgumentNullException>(() => new JoinBlock<int, int, int>(null));
            Assert.Throws<NotSupportedException>(() => { var ignored = new JoinBlock<int, int>().Target1.Completion; });
            Assert.Throws<NotSupportedException>(() => { var ignored = new JoinBlock<int, int, int>().Target3.Completion; });
            Assert.Throws<ArgumentNullException>(() => new JoinBlock<int, int>().Target1.Fault(null));
            AssertExtensions.Throws<ArgumentException>("messageHeader", () => new JoinBlock<int, int>().Target1.OfferMessage(default(DataflowMessageHeader), 1, null, false));
            AssertExtensions.Throws<ArgumentException>("consumeToAccept", () => new JoinBlock<int, int>().Target1.OfferMessage(new DataflowMessageHeader(1), 1, null, true));

            DataflowTestHelpers.TestArgumentsExceptions<Tuple<int, int>>(new JoinBlock<int, int>());
            DataflowTestHelpers.TestArgumentsExceptions<Tuple<int, int, int>>(new JoinBlock<int, int, int>());
        }

        [Fact]
        public async Task TestPostThenReceive()
        {
            const int Iters = 3;

            var block2 = new JoinBlock<int, int>();
            for (int i = 0; i < Iters; i++)
            {
                block2.Target1.Post(i);
                block2.Target2.Post(i + 1);

                Tuple<int, int> msg = await block2.ReceiveAsync();
                Assert.Equal(expected: i, actual: msg.Item1);
                Assert.Equal(expected: i + 1, actual: msg.Item2);
            }

            var block3 = new JoinBlock<int, int, int>();
            for (int i = 0; i < Iters; i++)
            {
                block3.Target1.Post(i);
                block3.Target2.Post(i + 1);
                block3.Target3.Post(i + 2);

                Tuple<int, int, int> msg = await block3.ReceiveAsync();
                Assert.Equal(expected: i, actual: msg.Item1);
                Assert.Equal(expected: i + 1, actual: msg.Item2);
                Assert.Equal(expected: i + 2, actual: msg.Item3);
            }
        }

        [Fact]
        public async Task TestPostAllThenReceive()
        {
            int iter = 2;

            var block2 = new JoinBlock<int, int>();
            for (int i = 0; i < iter; i++)
            {
                block2.Target1.Post(i);
                block2.Target2.Post(i + 1);
            }
            for (int i = 0; i < iter; i++)
            {
                Tuple<int, int> msg = await block2.ReceiveAsync(); 
                Assert.Equal(expected: i, actual: msg.Item1);
                Assert.Equal(expected: i + 1, actual: msg.Item2);
            }

            var block3 = new JoinBlock<int, int, int>();
            for (int i = 0; i < iter; i++)
            {
                block3.Target1.Post(i);
                block3.Target2.Post(i + 1);
                block3.Target3.Post(i + 2);
            }
            for (int i = 0; i < iter; i++)
            {
                Tuple<int, int, int> msg = await block3.ReceiveAsync();
                Assert.Equal(expected: i, actual: msg.Item1);
                Assert.Equal(expected: i + 1, actual: msg.Item2);
                Assert.Equal(expected: i + 2, actual: msg.Item3);
            }
        }

        [Fact]
        public async Task TestSendAllThenReceive()
        {
            int iter = 2;
            Task<bool> t1, t2, t3;

            var block2 = new JoinBlock<int, int>(new GroupingDataflowBlockOptions { Greedy = false });
            for (int i = 0; i < iter; i++)
            {
                t1 = block2.Target1.SendAsync(i);
                Assert.False(t1.IsCompleted);
                t2 = block2.Target2.SendAsync(i + 1);
                await Task.WhenAll(t1, t2);
            }
            for (int i = 0; i < iter; i++)
            {
                Tuple<int, int> msg = await block2.ReceiveAsync();
                Assert.Equal(expected: i, actual: msg.Item1);
                Assert.Equal(expected: i + 1, actual: msg.Item2);
            }

            var block3 = new JoinBlock<int, int, int>(new GroupingDataflowBlockOptions { Greedy = false });
            for (int i = 0; i < iter; i++)
            {
                t1 = block3.Target1.SendAsync(i);
                Assert.False(t1.IsCompleted);
                t2 = block3.Target2.SendAsync(i + 1);
                Assert.False(t2.IsCompleted);
                t3 = block3.Target3.SendAsync(i + 2);
                await Task.WhenAll(t1, t2, t3);
            }
            for (int i = 0; i < iter; i++)
            {
                Tuple<int, int, int> msg = await block3.ReceiveAsync();
                Assert.Equal(expected: i, actual: msg.Item1);
                Assert.Equal(expected: i + 1, actual: msg.Item2);
                Assert.Equal(expected: i + 2, actual: msg.Item3);
            }
        }

        [Fact]
        public void TestOneTargetInsufficient()
        {
            var block2 = new JoinBlock<int, int>();
            block2.Target1.Post(0);

            Tuple<int, int> result2;
            Assert.False(block2.TryReceive(out result2));
            Assert.Equal(expected: 0, actual: block2.OutputCount);

            var block3 = new JoinBlock<int, int, int>();
            block3.Target1.Post(0);

            Tuple<int, int, int> result3;
            Assert.False(block3.TryReceive(out result3));
            Assert.Equal(expected: 0, actual: block3.OutputCount);
        }

        [Fact]
        public async Task TestPrecancellation2()
        {
            var b = new JoinBlock<int, int>(new GroupingDataflowBlockOptions { 
                CancellationToken = new CancellationToken(canceled: true), MaxNumberOfGroups = 1 
            });

            Assert.NotNull(b.LinkTo(DataflowBlock.NullTarget<Tuple<int, int>>()));
            Assert.False(b.Target1.Post(42));
            Assert.False(b.Target2.Post(43));
            
            Task<bool> t1 = b.Target1.SendAsync(42);
            Task<bool> t2 = b.Target2.SendAsync(43);
            Assert.True(t1.IsCompleted);
            Assert.False(t1.Result);
            Assert.True(t2.IsCompleted);
            Assert.False(t2.Result);

            Tuple<int, int> ignoredValue;
            IList<Tuple<int, int>>  ignoredValues;
            Assert.False(b.TryReceive(out ignoredValue));
            Assert.False(b.TryReceiveAll(out ignoredValues));
            Assert.Equal(expected: 0, actual: b.OutputCount);
            Assert.NotNull(b.Completion);
            b.Complete();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => b.Completion);
        }

        [Fact]
        public async Task TestPrecancellation3()
        {
            var b = new JoinBlock<int, int, int>(new GroupingDataflowBlockOptions
            {
                CancellationToken = new CancellationToken(canceled: true),
                MaxNumberOfGroups = 1
            });

            Assert.NotNull(b.LinkTo(DataflowBlock.NullTarget<Tuple<int, int, int>>()));
            Assert.False(b.Target1.Post(42));
            Assert.False(b.Target2.Post(43));
            Assert.False(b.Target2.Post(44));

            Task<bool> t1 = b.Target1.SendAsync(42);
            Task<bool> t2 = b.Target2.SendAsync(43);
            Task<bool> t3 = b.Target2.SendAsync(44);
            Assert.True(t1.IsCompleted);
            Assert.False(t1.Result);
            Assert.True(t2.IsCompleted);
            Assert.False(t2.Result);
            Assert.True(t3.IsCompleted);
            Assert.False(t3.Result);

            Tuple<int, int, int> ignoredValue;
            IList<Tuple<int, int, int>> ignoredValues;
            Assert.False(b.TryReceive(out ignoredValue));
            Assert.False(b.TryReceiveAll(out ignoredValues));
            Assert.Equal(expected: 0, actual: b.OutputCount);
            Assert.NotNull(b.Completion);
            b.Complete();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => b.Completion);
        }

        [Fact]
        public async Task TestCompletionThroughTargets()
        {
            for (int test = 1; test <= 2; test++)
            {
                var join = new JoinBlock<int, int>();
                switch (test)
                {
                    case 1:
                        join.Target1.Post(1);
                        join.Target1.Complete();
                        join.Target2.Complete();
                        break;
                    case 2:
                        join.Target1.Complete();
                        join.Target2.Post(1);
                        join.Target2.Complete();
                        break;
                }
                await join.Completion;
            }

            for (int test = 1; test <= 3; test++)
            {
                var join = new JoinBlock<int, int, int>();
                switch (test)
                {
                    case 1:
                        join.Target1.Post(1);
                        join.Target1.Complete();
                        join.Target2.Complete();
                        join.Target3.Complete();
                        break;
                    case 2:
                        join.Target1.Complete();
                        join.Target2.Post(1);
                        join.Target2.Complete();
                        join.Target3.Complete();
                        break;
                    case 3:
                        join.Target1.Complete();
                        join.Target2.Complete();
                        join.Target3.Post(1);
                        join.Target3.Complete();
                        break;
                }
                await join.Completion;
            }
        }

        [Fact]
        public async Task TestFaultThroughTargets()
        {
            var join2 = new JoinBlock<int, string>();
            join2.Target2.Fault(new FormatException());
            await Assert.ThrowsAsync<FormatException>(() => join2.Completion);

            var join3 = new JoinBlock<int, string, double>();
            join3.Target3.Fault(new FormatException());
            await Assert.ThrowsAsync<FormatException>(() => join3.Completion);
        }

        [Fact]
        public async Task TestCompletionTask()
        {
            await DataflowTestHelpers.TestCompletionTask(() => new JoinBlock<int, string>());
            await DataflowTestHelpers.TestCompletionTask(() => new JoinBlock<int, string, double>());

            await Assert.ThrowsAsync<NotSupportedException>(() => new JoinBlock<string, string>().Target1.Completion);
            await Assert.ThrowsAsync<NotSupportedException>(() => new JoinBlock<string, string, double>().Target1.Completion);
        }

        [Fact]
        public async Task TestCompletionThroughBlock()
        {
            var join2 = new JoinBlock<int, int>();
            join2.Target1.Post(1);
            join2.Complete();
            await join2.Completion;

            var join3 = new JoinBlock<int, int, int>();
            join3.Target1.Post(2);
            join3.Complete();
            await join3.Completion;
        }

        [Fact]
        public async Task TestNonGreedyFailToConsumeReservedMessage()
        {
            var sources = Enumerable.Range(0, 2).Select(i => new DelegatePropagator<int, int>
            {
                ReserveMessageDelegate = delegate { return true; },
                ConsumeMessageDelegate = delegate(DataflowMessageHeader messageHeader, ITargetBlock<int> target, out bool messageConsumed) {
                    messageConsumed = false; // fail consumption of a message already reserved
                    Assert.Equal(expected: 0, actual: i); // shouldn't get to second source
                    return 0;
                }
            }).ToArray();
                
            var options = new GroupingDataflowBlockOptions { Greedy = false };
            JoinBlock<int, int> join = new JoinBlock<int, int>(options);

            join.Target1.OfferMessage(new DataflowMessageHeader(1), 0, sources[0], consumeToAccept: true); // call back ConsumeMassage
            join.Target2.OfferMessage(new DataflowMessageHeader(1), 0, sources[1], consumeToAccept: true); // call back ConsumeMassage

            await Assert.ThrowsAsync<InvalidOperationException>(() => join.Completion);
        }

        [Fact]
        public async Task TestNonGreedyDropPostponedOnCompletion()
        {
            var joinBlock = new JoinBlock<int, int>(new GroupingDataflowBlockOptions { Greedy = false });
            var source = new BufferBlock<int>();
            source.Post(1);
            source.LinkTo(joinBlock.Target1);
            joinBlock.Complete();
            await joinBlock.Completion;
        }

        [Fact]
        public async Task TestNonGreedyReleasingFailsAtCompletion()
        {
            var joinBlock = new JoinBlock<int, int>(new GroupingDataflowBlockOptions { Greedy = false });
            var source = new DelegatePropagator<int, int>
            {
                ReserveMessageDelegate = (header, target) => true,
                ReleaseMessageDelegate = delegate { throw new FormatException(); }
            };

            joinBlock.Target1.OfferMessage(new DataflowMessageHeader(1), 1, source, consumeToAccept: true);
            joinBlock.Complete();

            await Assert.ThrowsAsync<FormatException>(() => joinBlock.Completion);
        }

        [Fact]
        public async Task TestNonGreedyConsumingFailsWhileJoining()
        {
            var joinBlock = new JoinBlock<int, int>(new GroupingDataflowBlockOptions { Greedy = false });
            var source1 = new DelegatePropagator<int, int>
            {
                ReserveMessageDelegate = (header, target) => true,
                ConsumeMessageDelegate = delegate(DataflowMessageHeader messageHeader, ITargetBlock<int> target, out bool messageConsumed) {
                    throw new FormatException();
                }
            };

            joinBlock.Target1.OfferMessage(new DataflowMessageHeader(1), 1, source1, consumeToAccept: true);

            var source2 = new BufferBlock<int>();
            source2.Post(2);
            source2.LinkTo(joinBlock.Target2);

            await Assert.ThrowsAsync<FormatException>(() => joinBlock.Completion);
        }

        [Fact]
        public async Task TestNonGreedyPostponedMessagesNotAvailable()
        {
            var joinBlock = new JoinBlock<int, int>(new GroupingDataflowBlockOptions { Greedy = false });

            var cts = new CancellationTokenSource();
            Task<bool>[] sends = Enumerable.Range(0, 3).Select(i => joinBlock.Target1.SendAsync(i, cts.Token)).ToArray();

            cts.Cancel();
            foreach (Task<bool> send in sends)
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => send);
            }

            joinBlock.Target2.Post(1);
            joinBlock.Target2.Complete();
            joinBlock.Target1.Complete();

            await joinBlock.Completion;
        }

        [Fact]
        public async Task TestMaxNumberOfGroups()
        {
            foreach (int boundedCapacity in new[] { DataflowBlockOptions.Unbounded, 2, 3 })
            foreach (bool greedy in DataflowTestHelpers.BooleanValues)
            {
                var join = new JoinBlock<int, int>(new GroupingDataflowBlockOptions { MaxNumberOfGroups = 2, BoundedCapacity = 2 });
                Task<bool>[] sends1 = Enumerable.Range(0, 10).Select(i => join.Target1.SendAsync(i)).ToArray();
                Task<bool>[] sends2 = Enumerable.Range(0, 10).Select(i => join.Target2.SendAsync(i)).ToArray();
                var ab = new ActionBlock<Tuple<int, int>>(i => { }, new ExecutionDataflowBlockOptions { BoundedCapacity = 1 });
                join.LinkTo(ab, new DataflowLinkOptions { PropagateCompletion = true });
                await join.Completion;
                await Task.WhenAll(sends1);
                await Task.WhenAll(sends2);
            }
        }

        [Fact]
        public async Task TestTree()
        {
            foreach (bool greedy in DataflowTestHelpers.BooleanValues)
            foreach (int boundedCapacity in new[] { DataflowBlockOptions.Unbounded, 1 })
            foreach (int maxMessagesPerTask in new[] { DataflowBlockOptions.Unbounded, 1 })
            {
                var gdbo = new GroupingDataflowBlockOptions
                {
                    Greedy = greedy,
                    BoundedCapacity = boundedCapacity,
                    MaxMessagesPerTask = maxMessagesPerTask
                };
                var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

                var join1 = new JoinBlock<int, string>(gdbo);
                var join2 = new JoinBlock<double, short>(gdbo);
                var join5 = new JoinBlock<Tuple<int, string>, Tuple<double, short>>(gdbo);

                var join3 = new JoinBlock<string, object>(gdbo);
                var join4 = new JoinBlock<float, IntPtr>(gdbo);
                var join6 = new JoinBlock<Tuple<string, object>, Tuple<float, IntPtr>>(gdbo);

                var join7 = new JoinBlock<
                    Tuple<Tuple<int, string>, Tuple<double, short>>,
                    Tuple<Tuple<string, object>, Tuple<float, IntPtr>>>(gdbo);

                int count = 0;
                var sink = new ActionBlock<Tuple<Tuple<Tuple<int, string>, Tuple<double, short>>,
                    Tuple<Tuple<string, object>, Tuple<float, IntPtr>>>>(i => count++);

                join1.LinkTo(new ActionBlock<Tuple<int,string>>(item => { }), t => false); // ensure don't propagate across false filtered link
                join1.LinkTo(join5.Target1, linkOptions, t => true); // ensure joins work through filters
                join2.LinkTo(join5.Target2, linkOptions);
                join3.LinkTo(join6.Target1, linkOptions, t => true);
                join4.LinkTo(join6.Target2, linkOptions);
                join5.LinkTo(join7.Target1, linkOptions, t => true);
                join6.LinkTo(join7.Target2, linkOptions);
                join7.LinkTo(sink, linkOptions);

                const int Messages = 5;
                CreateFillLink<int>(Messages, join1.Target1);
                CreateFillLink<string>(Messages, join1.Target2);
                CreateFillLink<double>(Messages, join2.Target1);
                CreateFillLink<short>(Messages, join2.Target2);
                CreateFillLink<string>(Messages, join3.Target1);
                CreateFillLink<object>(Messages, join3.Target2);
                CreateFillLink<float>(Messages, join4.Target1);
                CreateFillLink<IntPtr>(Messages, join4.Target2);

                await sink.Completion;
                Assert.Equal(expected: Messages, actual: count);
            }
        }

        private static void CreateFillLink<T>(int messages, ITargetBlock<T> target)
        {
            var b = new BufferBlock<T>();
            b.PostRange(0, messages, i => default(T));
            b.Complete();
            b.LinkTo(target, new DataflowLinkOptions { PropagateCompletion = true });
        }

    }
}
