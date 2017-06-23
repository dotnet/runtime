// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Threading.Tasks.Dataflow.Tests
{
    public class BatchedJoinBlockTests
    {
        [Fact]
        public void TestCtor()
        {
            var blocks2 = new[]
            {
                new BatchedJoinBlock<int, string>(1),
                new BatchedJoinBlock<int, string>(2, new GroupingDataflowBlockOptions { 
                    MaxNumberOfGroups = 1 }),
                new BatchedJoinBlock<int, string>(3, new GroupingDataflowBlockOptions { 
                    MaxMessagesPerTask = 1 }),
                new BatchedJoinBlock<int, string>(4, new GroupingDataflowBlockOptions { 
                    MaxMessagesPerTask = 1, CancellationToken = new CancellationToken(true), MaxNumberOfGroups = 1 })
            };
            for (int i = 0; i < blocks2.Length; i++)
            {
                Assert.Equal(expected: i + 1, actual: blocks2[i].BatchSize);
                Assert.Equal(expected: 0, actual: blocks2[i].OutputCount);
                Assert.NotNull(blocks2[i].Completion);
            }

            var blocks3 = new[]
            {
                new BatchedJoinBlock<int, string, double>(1),
                new BatchedJoinBlock<int, string, double>(2, new GroupingDataflowBlockOptions { 
                    MaxNumberOfGroups = 1 }),
                new BatchedJoinBlock<int, string, double>(3, new GroupingDataflowBlockOptions { 
                    MaxMessagesPerTask = 1 }),
                new BatchedJoinBlock<int, string, double>(4, new GroupingDataflowBlockOptions { 
                    MaxMessagesPerTask = 1, CancellationToken = new CancellationToken(true), MaxNumberOfGroups = 1 })
            };
            for (int i = 0; i < blocks3.Length; i++)
            {
                Assert.Equal(expected: i + 1, actual: blocks2[i].BatchSize);
                Assert.Equal(expected: 0, actual: blocks2[i].OutputCount);
                Assert.NotNull(blocks2[i].Completion);
            }
        }

        [Fact]
        public void TestArgumentExceptions()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BatchedJoinBlock<int, string>(-1));
            Assert.Throws<ArgumentNullException>(() => new BatchedJoinBlock<int, string>(2, null));
            AssertExtensions.Throws<ArgumentException>("dataflowBlockOptions", () => new BatchedJoinBlock<int, string>(2, new GroupingDataflowBlockOptions { Greedy = false }));
            AssertExtensions.Throws<ArgumentException>("dataflowBlockOptions", () => new BatchedJoinBlock<int, string>(2, new GroupingDataflowBlockOptions { BoundedCapacity = 2 }));
            Assert.Throws<ArgumentNullException>(() => ((IDataflowBlock)new BatchedJoinBlock<int, string>(2)).Fault(null));
            Assert.Throws<ArgumentNullException>(() => new BatchedJoinBlock<int, string>(2).Target1.Fault(null));

            Assert.Throws<ArgumentOutOfRangeException>(() => new BatchedJoinBlock<int, string, double>(-1));
            Assert.Throws<ArgumentNullException>(() => new BatchedJoinBlock<int, string, double>(2, null));
            AssertExtensions.Throws<ArgumentException>("dataflowBlockOptions", () => new BatchedJoinBlock<int, string, double>(2, new GroupingDataflowBlockOptions { Greedy = false }));
            AssertExtensions.Throws<ArgumentException>("dataflowBlockOptions", () => new BatchedJoinBlock<int, string, double>(2, new GroupingDataflowBlockOptions { BoundedCapacity = 2 }));
            Assert.Throws<ArgumentNullException>(() => ((IDataflowBlock)new BatchedJoinBlock<int, string, double>(2)).Fault(null));

            DataflowTestHelpers.TestArgumentsExceptions(new BatchedJoinBlock<int, string>(1));
            DataflowTestHelpers.TestArgumentsExceptions(new BatchedJoinBlock<int, string, double>(1));
        }

        [Fact]
        public void TestToString()
        {
            DataflowTestHelpers.TestToString(nameFormat =>
                nameFormat != null ?
                    new BatchedJoinBlock<int, string>(2, new GroupingDataflowBlockOptions() { NameFormat = nameFormat }) :
                    new BatchedJoinBlock<int, string>(2));

            DataflowTestHelpers.TestToString(nameFormat =>
                nameFormat != null ?
                    new BatchedJoinBlock<int, string, double>(3, new GroupingDataflowBlockOptions() { NameFormat = nameFormat }) :
                    new BatchedJoinBlock<int, string, double>(3));
        }

        [Fact]
        public async Task TestCompletionTask()
        {
            await DataflowTestHelpers.TestCompletionTask(() => new BatchedJoinBlock<int, string>(2));
            await DataflowTestHelpers.TestCompletionTask(() => new BatchedJoinBlock<int, string, double>(2));

            await Assert.ThrowsAsync<NotSupportedException>(() => new BatchedJoinBlock<int, string>(2).Target1.Completion);
            await Assert.ThrowsAsync<NotSupportedException>(() => new BatchedJoinBlock<int, string, double>(2).Target1.Completion);
        }

        [Fact]
        public void TestPostThenReceive2()
        {
            const int Iters = 10;
            var block = new BatchedJoinBlock<int, string>(2);
            for (int i = 0; i < Iters; i++)
            {
                int prevCount = block.OutputCount;
                block.Target1.Post(i);
                Assert.Equal(expected: prevCount, actual: block.OutputCount);
                block.Target2.Post(i.ToString());

                if (i % block.BatchSize == 0)
                {
                    Assert.Equal(expected: prevCount + 1, actual: block.OutputCount);

                    Tuple<IList<int>, IList<string>> msg;
                    Assert.False(block.TryReceive(f => false, out msg));
                    Assert.True(block.TryReceive(out msg));

                    Assert.Equal(expected: 1, actual: msg.Item1.Count);
                    Assert.Equal(expected: 1, actual: msg.Item2.Count);

                    for (int j = 0; j < msg.Item1.Count; j++)
                    {
                        Assert.Equal(msg.Item1[j].ToString(), msg.Item2[j]);
                    }
                }
            }
        }

        [Fact]
        public void TestPostThenReceive3()
        {
            const int Iters = 10;
            var block = new BatchedJoinBlock<int, string, int>(3);
            for (int i = 0; i < Iters; i++)
            {
                Tuple<IList<int>, IList<string>, IList<int>> item;
                Assert.Equal(expected: 0, actual: block.OutputCount);

                block.Target1.Post(i);
                Assert.Equal(expected: 0, actual: block.OutputCount);
                Assert.False(block.TryReceive(out item));

                block.Target2.Post(i.ToString());
                Assert.Equal(expected: 0, actual: block.OutputCount);
                Assert.False(block.TryReceive(out item));

                block.Target3.Post(i);
                Assert.Equal(expected: 1, actual: block.OutputCount);

                Tuple<IList<int>, IList<string>, IList<int>> msg;
                Assert.True(block.TryReceive(out msg));
                Assert.Equal(expected: 1, actual: msg.Item1.Count);
                Assert.Equal(expected: 1, actual: msg.Item2.Count);
                Assert.Equal(expected: 1, actual: msg.Item3.Count);
                for (int j = 0; j < msg.Item1.Count; j++)
                {
                    Assert.Equal(msg.Item1[j].ToString(), msg.Item2[j]);
                }
            }
        }

        [Fact]
        public void TestPostAllThenReceive()
        {
            const int Iters = 10;

            var block = new BatchedJoinBlock<int, int>(2);
            for (int i = 0; i < Iters; i++)
            {
                block.Target1.Post(i);
                block.Target2.Post(i);
            }
            Assert.Equal(expected: Iters, actual: block.OutputCount);

            for (int i = 0; i < block.OutputCount; i++)
            {
                Tuple<IList<int>, IList<int>> msg;
                Assert.True(block.TryReceive(out msg));

                Assert.Equal(expected: 1, actual: msg.Item1.Count);
                Assert.Equal(expected: 1, actual: msg.Item2.Count);

                for (int j = 0; j < msg.Item1.Count; j++)
                {
                    Assert.Equal(msg.Item1[j], msg.Item2[j]);
                }
            }
        }

        [Fact]
        public void TestUnbalanced2()
        {
            const int Iters = 10, NumBatches = 2;
            int batchSize = Iters / NumBatches;

            var block = new BatchedJoinBlock<string, int>(batchSize);
            for (int i = 0; i < Iters; i++)
            {
                block.Target2.Post(i);
                Assert.Equal(expected: (i + 1) / batchSize, actual: block.OutputCount);
            }

            IList<Tuple<IList<string>, IList<int>>> items;
            Assert.True(block.TryReceiveAll(out items));
            Assert.Equal(expected: NumBatches, actual: items.Count);

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                Assert.NotNull(item.Item1);
                Assert.NotNull(item.Item2);
                Assert.Equal(expected: batchSize, actual: item.Item2.Count);
                for (int j = 0; j < batchSize; j++)
                {
                    Assert.Equal(expected: (i * batchSize) + j, actual: item.Item2[j]);
                }
            }
            Assert.False(block.TryReceiveAll(out items));
        }

        [Fact]
        public void TestUnbalanced3()
        {
            const int Iters = 10, NumBatches = 2;
            int batchSize = Iters / NumBatches;
            Tuple<IList<int>, IList<string>, IList<double>> item;

            var block = new BatchedJoinBlock<int, string, double>(batchSize);
            for (int i = 0; i < Iters; i++)
            {
                block.Target1.Post(i);
                Assert.Equal(expected: (i + 1) / batchSize, actual: block.OutputCount);
            }

            for (int i = 0; i < NumBatches; i++)
            {
                Assert.True(block.TryReceive(out item));
                Assert.NotNull(item.Item1);
                Assert.NotNull(item.Item2);
                Assert.NotNull(item.Item3);
                Assert.Equal(expected: batchSize, actual: item.Item1.Count);
                for (int j = 0; j < batchSize; j++)
                {
                    Assert.Equal(expected: (i * batchSize) + j, actual: item.Item1[j]);
                }
            }
            Assert.False(block.TryReceive(out item));
        }

        [Fact]
        public void TestCompletion()
        {
            const int Iters = 10;

            var block = new BatchedJoinBlock<int, int>(2);
            for (int i = 0; i < Iters; i++)
            {
                block.Target1.Post(i);
                block.Target2.Post(i);
            }
            Assert.Equal(expected: Iters, actual: block.OutputCount);

            block.Target1.Post(10);
            block.Target1.Complete();
            block.Target2.Complete();
            Assert.Equal(expected: Iters + 1, actual: block.OutputCount);

            Tuple<IList<int>, IList<int>> item;
            for (int i = 0; i < Iters; i++)
            {
                Assert.True(block.TryReceive(out item));
                Assert.Equal(expected: 1, actual: item.Item1.Count);
                Assert.Equal(expected: 1, actual: item.Item2.Count);
            }

            Assert.True(block.TryReceive(out item));
            Assert.Equal(expected: 1, actual: item.Item1.Count);
            Assert.Equal(expected: 0, actual: item.Item2.Count);
        }

        [Fact]
        public async Task TestPrecanceled2()
        {
            var b = new BatchedJoinBlock<int, int>(42, 
                new GroupingDataflowBlockOptions { CancellationToken = new CancellationToken(canceled: true), MaxNumberOfGroups = 1 });

            Tuple<IList<int>, IList<int>> ignoredValue;
            IList<Tuple<IList<int>, IList<int>>> ignoredValues;

            Assert.NotNull(b.LinkTo(new ActionBlock<Tuple<IList<int>, IList<int>>>(delegate { })));
            Assert.False(b.Target1.Post(42));
            Assert.False(b.Target2.Post(42));
            
            foreach (var target in new[] { b.Target1, b.Target2 })
            {
                var t = target.SendAsync(42);
                Assert.True(t.IsCompleted);
                Assert.False(t.Result);
            }
            
            Assert.False(b.TryReceiveAll(out ignoredValues));
            Assert.False(b.TryReceive(out ignoredValue));
            Assert.Equal(expected: 0, actual: b.OutputCount);
            Assert.NotNull(b.Completion);
            b.Target1.Complete();
            b.Target2.Complete();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => b.Completion);
        }

        [Fact]
        public async Task TestPrecanceled3()
        {
            var b = new BatchedJoinBlock<int, int, int>(42,
                new GroupingDataflowBlockOptions { CancellationToken = new CancellationToken(canceled: true), MaxNumberOfGroups = 1 });

            Tuple<IList<int>, IList<int>, IList<int>> ignoredValue;
            IList<Tuple<IList<int>, IList<int>, IList<int>>> ignoredValues;

            Assert.NotNull(b.LinkTo(new ActionBlock<Tuple<IList<int>, IList<int>, IList<int>>>(delegate { })));
            Assert.False(b.Target1.Post(42));
            Assert.False(b.Target2.Post(42));

            foreach (var target in new[] { b.Target1, b.Target2 })
            {
                var t = target.SendAsync(42);
                Assert.True(t.IsCompleted);
                Assert.False(t.Result);
            }

            Assert.False(b.TryReceiveAll(out ignoredValues));
            Assert.False(b.TryReceive(out ignoredValue));
            Assert.Equal(expected: 0, actual: b.OutputCount);
            Assert.NotNull(b.Completion);
            b.Target1.Complete();
            b.Target2.Complete();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => b.Completion);
        }

        [Fact]
        public async Task TestCompletesThroughTargets()
        {
            var b2 = new BatchedJoinBlock<int, int>(99);
            b2.Target1.Post(1);
            b2.Target1.Complete();
            b2.Target2.Complete();
            Tuple<IList<int>, IList<int>> item2 = await b2.ReceiveAsync();
            Assert.Equal(expected: 1, actual: item2.Item1.Count);
            Assert.Equal(expected: 0, actual: item2.Item2.Count);
            await b2.Completion;

            var b3 = new BatchedJoinBlock<int, int, int>(99);
            b3.Target2.Post(1);
            b3.Target3.Complete();
            b3.Target2.Complete();
            b3.Target1.Complete();
            Tuple<IList<int>, IList<int>, IList<int>> item3 = await b3.ReceiveAsync();
            Assert.Equal(expected: 0, actual: item3.Item1.Count);
            Assert.Equal(expected: 1, actual: item3.Item2.Count);
            Assert.Equal(expected: 0, actual: item3.Item3.Count);
            await b3.Completion;
        }

        [Fact]
        public async Task TestFaultsThroughTargets()
        {
            var b2 = new BatchedJoinBlock<int, int>(99);
            b2.Target1.Post(1);
            ((IDataflowBlock)b2.Target1).Fault(new FormatException());
            await Assert.ThrowsAsync<FormatException>(() => b2.Completion);

            var b3 = new BatchedJoinBlock<int, int, int>(99);
            b3.Target3.Post(1);
            ((IDataflowBlock)b3.Target2).Fault(new FormatException());
            await Assert.ThrowsAsync<FormatException>(() => b3.Completion);
        }

        [Fact]
        public async Task TestCompletesThroughBlock()
        {
            var b2 = new BatchedJoinBlock<int, int>(99);
            b2.Target1.Post(1);
            b2.Complete();
            Tuple<IList<int>, IList<int>> item2 = await b2.ReceiveAsync();
            Assert.Equal(expected: 1, actual: item2.Item1.Count);
            Assert.Equal(expected: 0, actual: item2.Item2.Count);
            await b2.Completion;

            var b3 = new BatchedJoinBlock<int, int, int>(99);
            b3.Target3.Post(1);
            b3.Complete();
            Tuple<IList<int>, IList<int>, IList<int>> item3 = await b3.ReceiveAsync();
            Assert.Equal(expected: 0, actual: item3.Item1.Count);
            Assert.Equal(expected: 0, actual: item3.Item2.Count);
            Assert.Equal(expected: 1, actual: item3.Item3.Count);
            await b3.Completion;
        }

        [Fact]
        public async Task TestReserveReleaseConsume()
        {
            var b2 = new BatchedJoinBlock<int, int>(2);
            b2.Target1.Post(1);
            b2.Target2.Post(2);
            await DataflowTestHelpers.TestReserveAndRelease(b2);

            b2 = new BatchedJoinBlock<int, int>(2);
            b2.Target1.Post(1);
            b2.Target2.Post(2);
            await DataflowTestHelpers.TestReserveAndConsume(b2);

            var b3 = new BatchedJoinBlock<int, int, int>(1);
            b3.Target2.Post(3);
            await DataflowTestHelpers.TestReserveAndRelease(b3);

            b3 = new BatchedJoinBlock<int, int, int>(4);
            b3.Target3.Post(1);
            b3.Target3.Post(2);
            b3.Target3.Post(3);
            b3.Target2.Post(3);
            await DataflowTestHelpers.TestReserveAndConsume(b3);
        }

        [Fact]
        public async Task TestConsumeToAccept()
        {
            var wob = new WriteOnceBlock<int>(i => i * 2);
            wob.Post(1);
            await wob.Completion;

            var b2 = new BatchedJoinBlock<int, int>(1);
            wob.LinkTo(b2.Target2, new DataflowLinkOptions { PropagateCompletion = true });
            Tuple<IList<int>, IList<int>> item2 = await b2.ReceiveAsync();
            Assert.Equal(expected: 0, actual: item2.Item1.Count);
            Assert.Equal(expected: 1, actual: item2.Item2.Count);
            b2.Target1.Complete();

            var b3 = new BatchedJoinBlock<int, int, int>(1);
            wob.LinkTo(b3.Target3, new DataflowLinkOptions { PropagateCompletion = true });
            Tuple<IList<int>, IList<int>, IList<int>> item3 = await b3.ReceiveAsync();
            Assert.Equal(expected: 0, actual: item3.Item1.Count);
            Assert.Equal(expected: 0, actual: item3.Item2.Count);
            Assert.Equal(expected: 1, actual: item3.Item3.Count);
            b3.Target1.Complete();
            b3.Target2.Complete();

            await Task.WhenAll(b2.Completion, b3.Completion);
        }

        [Fact]
        public async Task TestOfferMessage2()
        {
            Func<ITargetBlock<int>> generator = () => {
                var b = new BatchedJoinBlock<int, int>(1);
                return b.Target1;
            };
            DataflowTestHelpers.TestOfferMessage_ArgumentValidation(generator());
            DataflowTestHelpers.TestOfferMessage_AcceptsDataDirectly(generator());
            await DataflowTestHelpers.TestOfferMessage_AcceptsViaLinking(generator());
        }

        [Fact]
        public async Task TestOfferMessage3()
        {
            Func<ITargetBlock<int>> generator = () => {
                var b = new BatchedJoinBlock<int, int, int>(1);
                return b.Target1;
            };
            DataflowTestHelpers.TestOfferMessage_ArgumentValidation(generator());
            DataflowTestHelpers.TestOfferMessage_AcceptsDataDirectly(generator());
            await DataflowTestHelpers.TestOfferMessage_AcceptsViaLinking(generator());
        }

        [Fact]
        public async Task TestMaxNumberOfGroups()
        {
            const int MaxGroups = 2;

            var b2 = new BatchedJoinBlock<int, int>(1, new GroupingDataflowBlockOptions { MaxNumberOfGroups = MaxGroups });
            b2.Target1.PostRange(0, MaxGroups);
            Assert.False(b2.Target1.Post(42));
            Assert.False(b2.Target2.Post(42));
            IList<Tuple<IList<int>, IList<int>>> items2;
            Assert.True(b2.TryReceiveAll(out items2));
            Assert.Equal(expected: MaxGroups, actual: items2.Count);
            await b2.Completion;

            var b3 = new BatchedJoinBlock<int, int, int>(1, new GroupingDataflowBlockOptions { MaxNumberOfGroups = MaxGroups });
            b3.Target1.PostRange(0, MaxGroups);
            Assert.False(b3.Target1.Post(42));
            Assert.False(b3.Target2.Post(42));
            Assert.False(b3.Target3.Post(42));
            IList<Tuple<IList<int>, IList<int>, IList<int>>> items3;
            Assert.True(b3.TryReceiveAll(out items3));
            Assert.Equal(expected: MaxGroups, actual: items3.Count);
            await b3.Completion;
        }

    }
}
