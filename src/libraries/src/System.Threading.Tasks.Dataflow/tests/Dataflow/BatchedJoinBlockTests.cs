// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Xunit;

namespace System.Threading.Tasks.Dataflow.Tests
{
    public partial class DataflowBlockTests : DataflowBlockTestBase
    {
        [Fact]
        public void RunBatchedJoinBlockTests()
        {
            // BatchedJoinBlock`2
            Assert.True(IDataflowBlockTestHelper.TestToString(nameFormat => nameFormat != null ? new BatchedJoinBlock<int, int>(1, new GroupingDataflowBlockOptions() { NameFormat = nameFormat }) : new BatchedJoinBlock<int, int>(1)));
            Assert.True(ISourceBlockTestHelper.TestLinkTo<Tuple<IList<int>, IList<int>>>(ConstructBatchedJoin2NewWithNMessages(1), 1));
            Assert.True(ISourceBlockTestHelper.TestReserveMessageAndReleaseReservation<Tuple<IList<int>, IList<int>>>(ConstructBatchedJoin2NewWithNMessages(1)));
            //Assert.True(ISourceBlockTestHelper.TestReleaseReservation<Tuple<IList<int>, IList<int>>>(ConstructBatchedJoin2NewWithNMessages(1)));

            // BatchedJoinBlock`3
            Assert.True(ISourceBlockTestHelper.TestConsumeMessage<Tuple<IList<int>, IList<int>, IList<int>>>(ConstructBatchedJoin3NewWithNMessages(1)));
            Assert.True(ISourceBlockTestHelper.TestTryReceiveWithFilter<Tuple<IList<int>, IList<int>, IList<int>>>(ConstructBatchedJoin3NewWithNMessages(1), 1));
            Assert.True(ISourceBlockTestHelper.TestTryReceiveAll<Tuple<IList<int>, IList<int>, IList<int>>>(ConstructBatchedJoin3NewWithNMessages(1), 1));
        }

        private static BatchedJoinBlock<int, int> ConstructBatchedJoin2NewWithNMessages(int messagesCount)
        {
            var block = new BatchedJoinBlock<int, int>(2);
            for (int i = 0; i < messagesCount; i++)
            {
                block.Target1.Post(i);
                block.Target2.Post(i);
            }

            // Spin until the messages have been properly buffered up. 
            // Otherwise TryReceive fails.
            SpinWait.SpinUntil(() => block.OutputCount == messagesCount);

            return block;
        }

        private static BatchedJoinBlock<int, int, int> ConstructBatchedJoin3NewWithNMessages(int messagesCount)
        {
            var block = new BatchedJoinBlock<int, int, int>(3);
            for (int i = 0; i < messagesCount; i++)
            {
                block.Target1.Post(i);
                block.Target2.Post(i);
                block.Target3.Post(i);
            }

            // Spin until the messages have been properly buffered up. 
            // Otherwise TryReceive fails.
            SpinWait.SpinUntil(() => block.OutputCount == messagesCount);

            return block;
        }

        [Fact]
        public void TestBatchedJoinBlockConstructor()
        {
            // *** 2-way BatchedJoinBlock ***
            // batch size without decline without option
            var block = new BatchedJoinBlock<int, int>(42);
            Assert.False(block.OutputCount != 0, "Constructor failed! OutputCount returned a non zero value for a brand new BatchedJoinBlock.");
            Assert.False(block.BatchSize != 42, "Constructor failed! BatchSize does not match for a brand new BatchedJoinBlock.");
            // batch size with decline without option
            block = new BatchedJoinBlock<int, int>(43, new GroupingDataflowBlockOptions { MaxNumberOfGroups = 1 });
            Assert.False(block.OutputCount != 0, "Constructor failed! OutputCount returned a non zero value for a brand new BatchedJoinBlock.");
            Assert.False(block.BatchSize != 43, "Constructor failed! BatchSize does not match for a brand new BatchedJoinBlock.");
            // batch size with decline with not cancelled token and default scheduler
            block = new BatchedJoinBlock<int, int>(44, new GroupingDataflowBlockOptions { MaxMessagesPerTask = 1 });
            Assert.False(block.OutputCount != 0, "Constructor failed! OutputCount returned a non zero value for a brand new BatchedJoinBlock.");
            Assert.False(block.BatchSize != 44, "Constructor failed! BatchSize does not match for a brand new BatchedJoinBlock.");
            // decline with a cancelled token and default scheduler
            var token = new CancellationToken(true);
            block = new BatchedJoinBlock<int, int>(45, new GroupingDataflowBlockOptions { MaxMessagesPerTask = 1, CancellationToken = token, MaxNumberOfGroups = 1 });
            Assert.False(block.OutputCount != 0, "Constructor failed! OutputCount returned a non zero value for a brand new BatchedJoinBlock.");
            Assert.False(block.BatchSize != 45, "Constructor failed! BatchSize does not match for a brand new BatchedJoinBlock.");

            // *** 3-way BatchedJoinBlock ***
            // batch size without decline without option
            var block3 = new BatchedJoinBlock<int, int, int>(42);
            Assert.False(block3.OutputCount != 0, "Constructor failed! OutputCount returned a non zero value for a brand new BatchedJoinBlock.");
            Assert.False(block3.BatchSize != 42, "Constructor failed! BatchSize does not match for a brand new BatchedJoinBlock.");
            // batch size with decline without option
            block3 = new BatchedJoinBlock<int, int, int>(43, new GroupingDataflowBlockOptions { MaxNumberOfGroups = 1 });
            Assert.False(block3.OutputCount != 0, "Constructor failed! OutputCount returned a non zero value for a brand new BatchedJoinBlock.");
            Assert.False(block3.BatchSize != 43, "Constructor failed! BatchSize does not match for a brand new BatchedJoinBlock.");
            // batch size with decline with not cancelled token and default scheduler
            block3 = new BatchedJoinBlock<int, int, int>(44, new GroupingDataflowBlockOptions { MaxNumberOfGroups = 1 });
            Assert.False(block3.OutputCount != 0, "Constructor failed! OutputCount returned a non zero value for a brand new BatchedJoinBlock.");
            Assert.False(block3.BatchSize != 44, "Constructor failed! BatchSize does not match for a brand new BatchedJoinBlock.");
            // decline with a cancelled token and default scheduler
            token = new CancellationToken(true);
            block3 = new BatchedJoinBlock<int, int, int>(45, new GroupingDataflowBlockOptions { MaxMessagesPerTask = 1, CancellationToken = token, MaxNumberOfGroups = 1 });
            Assert.False(block3.OutputCount != 0, "Constructor failed! OutputCount returned a non zero value for a brand new BatchedJoinBlock.");
            Assert.False(block3.BatchSize != 45, "Constructor failed! BatchSize does not match for a brand new BatchedJoinBlock.");
        }

        [Fact]
        public void TestBatchedJoinInvalidArgumentValidation()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BatchedJoinBlock<int, int>(0));
            Assert.Throws<ArgumentNullException>(() => new BatchedJoinBlock<int, int>(1, null));
            Assert.Throws<ArgumentException>(() => new BatchedJoinBlock<int, int>(1, new GroupingDataflowBlockOptions { Greedy = false }));
            Assert.Throws<NotSupportedException>(() => { var ignored = new BatchedJoinBlock<int, int>(2).Target1.Completion; });
            Assert.True(ISourceBlockTestHelper.TestArgumentsExceptions<Tuple<IList<int>, IList<int>>>(new BatchedJoinBlock<int, int>(2)));

            Assert.Throws<ArgumentOutOfRangeException>(() => new BatchedJoinBlock<int, int, int>(0));
            Assert.Throws<ArgumentNullException>(() => new BatchedJoinBlock<int, int, int>(1, null));
            Assert.Throws<ArgumentException>(() => new BatchedJoinBlock<int, int, int>(1, new GroupingDataflowBlockOptions { Greedy = false }));
            Assert.Throws<NotSupportedException>(() => { var ignored = new BatchedJoinBlock<int, int, int>(2).Target3.Completion; });
            Assert.True(ISourceBlockTestHelper.TestArgumentsExceptions<Tuple<IList<int>, IList<int>, IList<int>>>(new BatchedJoinBlock<int, int, int>(2)));
        }

        //[Fact(Skip = "Outerloop")]
        public void RunBatchedJoinBlockConformanceTests()
        {
            // Test Post/Receive single block
            var block = new BatchedJoinBlock<int, int>(2);
            int iter = 10;
            for (int i = 0; i < iter; i++)
            {
                block.Target1.Post(i);
                block.Target2.Post(i);
                if (i % block.BatchSize == 0)
                {
                    var msg = block.Receive();
                    Assert.False(msg.Item1.Count != msg.Item2.Count, "BatchedJoinBlock Post/Receive failed, returned arrays of differnet length");
                    for (int j = 0; j < msg.Item1.Count; j++)
                    {
                        if (msg.Item1[j] != msg.Item2[j])
                        {
                            Assert.False(true, "BatchedJoinBlock Post/Receive failed, returned arrys items are different");
                        }
                    }
                }
            }

            // Test PostAll then Receive single block
            block = new BatchedJoinBlock<int, int>(2);
            for (int i = 0; i < iter; i++)
            {
                block.Target1.Post(i);
                block.Target2.Post(i);
            }

            Assert.False(block.OutputCount != iter, string.Format("BatchedJoinBlock Post failed, expected, incorrect OutputCount. Expected {0} actual {1}", iter, block.OutputCount));

            for (int i = 0; i < block.OutputCount; i++)
            {
                var msg = block.Receive();
                if (msg.Item1.Count != msg.Item2.Count)
                {
                    Assert.False(true, "BatchedJoinBlock PostAll then Receive failed, returned arrays of differnet length");
                }
                for (int j = 0; j < msg.Item1.Count; j++)
                {
                    if (msg.Item1[j] != msg.Item2[j])
                    {
                        Assert.False(true, "BatchedJoinBlock PostAll then Receive failed, returned arrys items are different");
                    }
                }
            }

            //Test one target Post < patchSize msg with TryReceive
            block = new BatchedJoinBlock<int, int>(2);
            block.Target1.Post(0);
            Tuple<IList<int>, IList<int>> result;
            if (block.TryReceive(out result))
            {
                Assert.False(true, "BatchedJoinBlock.TryReceive failed, returned true and the number of messages is less than the batch size");
            }
            if (block.OutputCount > 0)
            {
                Assert.False(true, "BatchedJoinBlock.OutputCount failed, returned count > 0 and only one target posted a message");
            }

            // Test handling of stragglers at end of block's life
            block = new BatchedJoinBlock<int, int>(2);
            for (int i = 0; i < 10; i++)
            {
                block.Target1.Post(i);
                block.Target2.Post(i);
            }
            block.Target1.Post(10);
            block.Target1.Complete();
            block.Target2.Complete();
            if (block.OutputCount != 11)
            {
                Assert.False(true, "BatchedJoinBlock last batch not generated correctly");
            }

            for (int i = 0; i < 10; i++) block.Receive();
            var lastResult = block.Receive();
            if (lastResult.Item1.Count != 1 || lastResult.Item2.Count != 0)
            {
                Assert.False(true, "BatchedJoinBlock last batch contains incorrect data");
            }

            // Test BatchedJoinBlock`2 using a precanceled token
            {
                var localPassed = true;
                try
                {
                    var cts = new CancellationTokenSource();
                    cts.Cancel();
                    var dbo = new GroupingDataflowBlockOptions { CancellationToken = cts.Token, MaxNumberOfGroups = 1 };
                    var bjb = new BatchedJoinBlock<int, int>(42, dbo);

                    Tuple<IList<int>, IList<int>> ignoredValue;
                    IList<Tuple<IList<int>, IList<int>>> ignoredValues;
                    localPassed &= bjb.LinkTo(new ActionBlock<Tuple<IList<int>, IList<int>>>(delegate { })) != null;
                    localPassed &= bjb.Target1.Post(42) == false;
                    localPassed &= bjb.Target2.Post(42) == false;
                    localPassed &= bjb.Target1.SendAsync(42).Result == false;
                    localPassed &= bjb.Target2.SendAsync(42).Result == false;
                    localPassed &= bjb.TryReceiveAll(out ignoredValues) == false;
                    localPassed &= bjb.TryReceive(out ignoredValue) == false;
                    localPassed &= bjb.OutputCount == 0;
                    localPassed &= bjb.Completion != null;
                    bjb.Target1.Complete();
                    bjb.Target2.Complete();
                }
                catch (Exception)
                {
                    localPassed = false;
                }

                Assert.True(localPassed, "Precanceled tokens don't work correctly on BJB`2");
            }

            // Test BatchedJoinBlock`3 using a precanceled token
            {
                var localPassed = true;
                try
                {
                    var cts = new CancellationTokenSource();
                    cts.Cancel();
                    var dbo = new GroupingDataflowBlockOptions { CancellationToken = cts.Token, MaxNumberOfGroups = 1 };
                    var bjb = new BatchedJoinBlock<int, int, int>(42, dbo);

                    Tuple<IList<int>, IList<int>, IList<int>> ignoredValue;
                    IList<Tuple<IList<int>, IList<int>, IList<int>>> ignoredValues;
                    localPassed &= bjb.LinkTo(new ActionBlock<Tuple<IList<int>, IList<int>, IList<int>>>(delegate { })) != null;
                    localPassed &= bjb.Target1.Post(42) == false;
                    localPassed &= bjb.Target2.Post(42) == false;
                    localPassed &= bjb.Target3.Post(42) == false;
                    localPassed &= bjb.Target1.SendAsync(42).Result == false;
                    localPassed &= bjb.Target2.SendAsync(42).Result == false;
                    localPassed &= bjb.Target3.SendAsync(42).Result == false;
                    localPassed &= bjb.TryReceiveAll(out ignoredValues) == false;
                    localPassed &= bjb.TryReceive(out ignoredValue) == false;
                    localPassed &= bjb.OutputCount == 0;
                    localPassed &= bjb.Completion != null;
                    bjb.Target1.Complete();
                    bjb.Target2.Complete();
                    bjb.Target3.Complete();
                }
                catch (Exception)
                {
                    localPassed = false;
                }

                Assert.True(localPassed, "Precanceled tokens don't work correctly on BJB`3");
            }

            // Test BatchedJoinBlock`2 completion through all targets
            {
                var localPassed = true;
                var batchedJoin = new BatchedJoinBlock<int, int>(99);
                var terminator = new ActionBlock<Tuple<IList<int>, IList<int>>>(x => { });
                batchedJoin.LinkTo(terminator);
                batchedJoin.Target1.Post(1);
                batchedJoin.Target1.Complete();
                batchedJoin.Target2.Complete();
                localPassed = batchedJoin.Completion.Wait(2000);

                Assert.True(localPassed, string.Format("BatchedJoinBlock`2 completed through targets - {0}", localPassed ? "Passed" : "FAILED"));
            }

            // Test BatchedJoinBlock`3 completion through all targets
            {
                var localPassed = true;
                var batchedJoin = new BatchedJoinBlock<int, int, int>(99);
                var terminator = new ActionBlock<Tuple<IList<int>, IList<int>, IList<int>>>(x => { });
                batchedJoin.LinkTo(terminator);
                batchedJoin.Target1.Post(1);
                batchedJoin.Target1.Complete();
                batchedJoin.Target2.Complete();
                batchedJoin.Target3.Complete();
                localPassed = batchedJoin.Completion.Wait(2000);

                Assert.True(localPassed, string.Format("BatchedJoinBlock`3 completed through targets - {0}", localPassed ? "Passed" : "FAILED"));
            }

            // Test BatchedJoinBlock`2 completion through block
            {
                var localPassed = true;
                var batchedJoin = new BatchedJoinBlock<int, int>(99);
                var terminator = new ActionBlock<Tuple<IList<int>, IList<int>>>(x => { });
                batchedJoin.LinkTo(terminator);
                batchedJoin.Target1.Post(1);
                batchedJoin.Complete();
                localPassed = batchedJoin.Completion.Wait(2000);

                Assert.True(localPassed, string.Format("BatchedJoinBlock`2 completed through block - {0}", localPassed ? "Passed" : "FAILED"));
            }

            // Test BatchedJoinBlock`3 completion through block
            {
                var localPassed = true;
                var batchedJoin = new BatchedJoinBlock<int, int, int>(99);
                var terminator = new ActionBlock<Tuple<IList<int>, IList<int>, IList<int>>>(x => { });
                batchedJoin.LinkTo(terminator);
                batchedJoin.Target1.Post(1);
                batchedJoin.Complete();
                localPassed = batchedJoin.Completion.Wait(2000);

                Assert.True(localPassed, string.Format("BatchedJoinBlock`3 completed through block - {0}", localPassed ? "Passed" : "FAILED"));
            }
        }
    }
}
