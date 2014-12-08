// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
        public void RunBatchBlockTests()
        {
            Assert.True(IDataflowBlockTestHelper.TestToString(nameFormat => nameFormat != null ? new BatchBlock<int>(1, new GroupingDataflowBlockOptions() { NameFormat = nameFormat }) : new BatchBlock<int>(1)));
            Assert.True(ISourceBlockTestHelper.TestLinkTo<int[]>(ConstructBatchNewWithNMessages(1), 1));
            Assert.True(ISourceBlockTestHelper.TestReserveMessageAndReleaseReservation<int[]>(ConstructBatchNewWithNMessages(1)));
            Assert.True(ISourceBlockTestHelper.TestConsumeMessage<int[]>(ConstructBatchNewWithNMessages(1)));
            Assert.True(ISourceBlockTestHelper.TestTryReceiveWithFilter<int[]>(ConstructBatchNewWithNMessages(1), 1));
            Assert.True(ISourceBlockTestHelper.TestTryReceiveAll<int[]>(ConstructBatchNewWithNMessages(1), 1));
            Assert.True(ISourceBlockTestHelper.TestCancelWhileReserve(ct => new BatchBlock<int>(1, new GroupingDataflowBlockOptions { CancellationToken = ct }), source => (source as BatchBlock<int>).Post(0), source => (source as BatchBlock<int>).OutputCount));
            Assert.True(ITargetBlockTestHelper.TestOfferMessage<int>(new BatchBlock<int>(1)));
            Assert.True(ITargetBlockTestHelper.TestPost<int>(new BatchBlock<int>(1)));
            Assert.True(ITargetBlockTestHelper.TestComplete<int>(new BatchBlock<int>(1)));
            Assert.True(ITargetBlockTestHelper.TestCompletionTask<int>(new BatchBlock<int>(1)));
            Assert.True(ITargetBlockTestHelper.TestNonGreedyPost(new BatchBlock<int>(1, new GroupingDataflowBlockOptions { Greedy = false })));

            Assert.True(TestReleaseOnReserveException(linkBadFirst: true));
            Assert.True(TestReleaseOnReserveException(linkBadFirst: false));
            Assert.True(TestMaxNumberOfGroups(greedy: true, sync: true));
            Assert.True(TestMaxNumberOfGroups(greedy: true, sync: false));
            // {non-greedy sync} is intentioanlly skipped because it is not supported by the block
            Assert.True(TestMaxNumberOfGroups(greedy: false, sync: false));
            Assert.True(TestTriggerBatch(boundedCapacity: DataflowBlockOptions.Unbounded));
            Assert.True(TestTriggerBatchRacingWithSendAsync(greedy: true));
            Assert.True(TestTriggerBatchRacingWithSendAsync(greedy: false));
            Assert.True(TestTriggerBatchRacingWithComplete(greedy: true));
            Assert.True(TestTriggerBatchRacingWithComplete(greedy: false));
        }

        [Fact]
        public void TestBatchBlockConstructor()
        {
            // size without decline without option
            var block = new BatchBlock<int>(42);
            Assert.False(block.BatchSize != 42, "Constructor failed! BatchSize doesn't match for a brand new BatchBlock.");

            // size with decline without option
            block = new BatchBlock<int>(43, new GroupingDataflowBlockOptions { MaxNumberOfGroups = 1 });
            Assert.False(block.BatchSize != 43, "Constructor failed! BatchSize doesn't match for a brand new BatchBlock.");

            // size with decline with not cancelled token and default scheduler
            block = new BatchBlock<int>(44, new GroupingDataflowBlockOptions { MaxMessagesPerTask = 1 });
            Assert.False(block.BatchSize != 44, "Constructor failed! BatchSize doesn't match for a brand new BatchBlock.");

            //with a cancelled token and default scheduler
            var token = new CancellationToken(true);
            block = new BatchBlock<int>(45, new GroupingDataflowBlockOptions { MaxMessagesPerTask = 1, CancellationToken = token, MaxNumberOfGroups = 1 });
            Assert.False(block.BatchSize != 45, "Constructor failed! BatchSize doesn't match for a brand new BatchBlock.");
        }

        [Fact]
        public void TestBatchInvalidArgumentValidation()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BatchBlock<int>(-1));
            Assert.Throws<ArgumentNullException>(() => new BatchBlock<int>(2, null));
            Assert.True(ITargetBlockTestHelper.TestArgumentsExceptions<int>(new BatchBlock<int>(1)));
            Assert.True(ISourceBlockTestHelper.TestArgumentsExceptions<int[]>(new BatchBlock<int>(1)));
        }

        //[Fact(Skip = "Outerloop")]
        public void TestBatchNonGreedyCombo()
        {
            bool passed = true;
            const int value1 = 1;
            const int value2 = 2;

            BatchBlock<int> batch = new BatchBlock<int>(2, new GroupingDataflowBlockOptions { Greedy = false });

            // Offer 1 message - this should be postponed
            // To avoid implementing a source, we use SendAsync and we verify the task is not completed
            Task<bool> postTask1 = batch.SendAsync(value1);
            bool localPassed = !postTask1.Wait(500);

            Assert.True(localPassed, string.Format("SendAsync1 not completed - {0}", localPassed ? "Passed" : "FAILED"));

            // Output count must be 0
            localPassed = batch.OutputCount == 0;
            Assert.True(localPassed, string.Format("OutputCount == 0 - {0} ({1})", localPassed ? "Passed" : "FAILED", batch.OutputCount));

            // Offer another message - this should be postponed first and then consumed
            Task<bool> postTask2 = batch.SendAsync(value2);

            // Both SendAsync tasks must be completed
            localPassed = postTask1.Wait(5000);

            Assert.True(localPassed, string.Format("SendAsync1 completed - {0}", localPassed ? "Passed" : "FAILED"));
            localPassed = postTask2.Wait(500);

            Assert.True(localPassed, string.Format("SendAsync2 completed - {0}", localPassed ? "Passed" : "FAILED"));

            // Both SendAsync tasks must have a result of true
            if (passed)
            {
                localPassed = postTask1.Result;

                Assert.True(localPassed, string.Format("SendAsync1 true - {0}", localPassed ? "Passed" : "FAILED"));
                localPassed = postTask2.Result;

                Assert.True(localPassed, string.Format("SendAsync2 true - {0}", localPassed ? "Passed" : "FAILED"));
            }

            // Output count must be 1
            Task.Delay(500).Wait();
            localPassed = batch.OutputCount == 1;

            Assert.True(localPassed, string.Format("OutputCount == 1 - {0} ({1})", localPassed ? "Passed" : "FAILED", batch.OutputCount));

            // TryReceive should return 1 result
            int[] result;
            localPassed = batch.TryReceive(out result);

            Assert.True(localPassed, string.Format("TryReceive - {0}", localPassed ? "Passed" : "FAILED"));

            // Verify order
            localPassed = (result[0] == value1) && (result[1] == value2);

            Assert.True(localPassed, string.Format("Order - {0} (Received[{1}, {2}] Expected[{3}, {4}])", localPassed ? "Passed" : "FAILED",
                                                                                             result[0], result[1], value1, value2));

            // Post 1 message - the result is non-deterministic but at least no exception should be thrown
            batch.Post(value2);
            Assert.True(localPassed, string.Format("Post did not throw - Passed"));

            // Output count must be 0
            localPassed = batch.OutputCount == 0;

            Assert.True(localPassed, string.Format("OutputCount == 0 - {0} ({1})", localPassed ? "Passed" : "FAILED", batch.OutputCount));

            Assert.True(localPassed, string.Format("{0}", passed ? "Passed" : "FAILED"));
        }

        //[Fact(Skip = "Outerloop")]
        public void RunBatchBlockConformanceTests()
        {
            bool localPassed;
            // Greedy batching
            {
                localPassed = true;
                const int NUM_MESSAGES = 1;
                const int BATCH_SIZE = 1;

                var batch = new BatchBlock<int>(BATCH_SIZE);
                for (int i = 0; i < NUM_MESSAGES * BATCH_SIZE; i++) batch.Post(i);
                for (int i = 0; i < NUM_MESSAGES; i++)
                {
                    int[] result = batch.Receive();
                    localPassed &= result.Length == BATCH_SIZE;
                    for (int j = 0; j < result.Length - 1; j++)
                    {
                        localPassed &= (result[j] + 1 == result[j + 1]);
                    }
                }

                Assert.True(localPassed, string.Format("{0}: Greedy batching", localPassed ? "Success" : "Failure"));
            }

            // Non-greedy batching with BATCH_SIZE sources used repeatedly
            {
                localPassed = true;
                const int NUM_MESSAGES = 1;
                const int BATCH_SIZE = 1;

                var batch = new BatchBlock<int>(BATCH_SIZE, new GroupingDataflowBlockOptions { Greedy = false });
                var buffers = Enumerable.Range(0, BATCH_SIZE).Select(_ => new BufferBlock<int>()).ToList();
                foreach (var buffer in buffers) buffer.LinkTo(batch);

                int prevSum = -1;
                for (int i = 0; i < NUM_MESSAGES; i++)
                {
                    for (int j = 0; j < BATCH_SIZE; j++) buffers[j].Post(i);
                    int sum = batch.Receive().Sum();
                    localPassed &= (sum > prevSum);
                    prevSum = sum;
                }

                Assert.True(localPassed, string.Format("{0}: Non-greedy batching with BATCH_SIZE sources used repeatedly", localPassed ? "Success" : "Failure"));
            }

            // Non-greedy batching with BATCH_SIZE * NUM_MESSAGES sources
            {
                localPassed = true;
                const int NUM_MESSAGES = 1;
                const int BATCH_SIZE = 2;

                var batch = new BatchBlock<int>(BATCH_SIZE, new GroupingDataflowBlockOptions { Greedy = false });
                var buffers = Enumerable.Range(0, BATCH_SIZE * NUM_MESSAGES).Select(_ => new BufferBlock<int>()).ToList();
                foreach (var buffer in buffers)
                {
                    buffer.LinkTo(batch);
                    buffer.Post(1);
                }

                for (int i = 0; i < NUM_MESSAGES; i++)
                {
                    localPassed &= batch.Receive().Sum() == BATCH_SIZE;
                }

                Assert.True(localPassed, string.Format("{0}: Non-greedy batching with N*M sources", localPassed ? "Success" : "Failure"));
            }

            // Non-greedy batching with missed messages
            {
                localPassed = true;
                const int BATCH_SIZE = 2;

                var batch = new BatchBlock<int>(BATCH_SIZE, new GroupingDataflowBlockOptions { Greedy = false });
                var buffers = Enumerable.Range(0, BATCH_SIZE - 1).Select(_ => new BufferBlock<int>()).ToList();
                using (var ce = new CountdownEvent(BATCH_SIZE - 1))
                {
                    foreach (var buffer in buffers)
                    {
                        buffer.LinkTo(batch);
                        buffer.LinkTo(new ActionBlock<int>(i => ce.Signal()));
                        buffer.Post(42);
                    }
                    ce.Wait();
                }

                buffers = Enumerable.Range(0, BATCH_SIZE).Select(_ => new BufferBlock<int>()).ToList();
                foreach (var buffer in buffers)
                {
                    buffer.LinkTo(batch);
                    buffer.Post(42);
                    buffer.Complete();
                }

                localPassed &= Task.WaitAll(buffers.Select(b => b.Completion).ToArray(), 2000);

                Assert.True(localPassed, string.Format("{0}: Non-greedy batching with missed messages", localPassed ? "Success" : "Failure"));
            }

            // Test using a precanceled token
            {
                localPassed = true;
                try
                {
                    var cts = new CancellationTokenSource();
                    cts.Cancel();
                    var dbo = new GroupingDataflowBlockOptions { CancellationToken = cts.Token, MaxNumberOfGroups = 1 };
                    var b = new BatchBlock<int>(42, dbo);

                    int[] ignoredValue;
                    IList<int[]> ignoredValues;
                    localPassed &= b.BatchSize == 42;
                    localPassed &= b.LinkTo(new ActionBlock<int[]>(delegate { })) != null;
                    localPassed &= b.SendAsync(42).Result == false;
                    localPassed &= b.TryReceiveAll(out ignoredValues) == false;
                    localPassed &= b.Post(42) == false;
                    localPassed &= b.OutputCount == 0;
                    localPassed &= b.TryReceive(out ignoredValue) == false;
                    localPassed &= b.Completion != null;
                    b.Complete();
                }
                catch (Exception)
                {
                    localPassed = false;
                }

                Assert.True(localPassed, string.Format("{0}: Precanceled tokens work correctly", localPassed ? "Success" : "Failure"));
            }

            // Test completing block while still items buffered
            {
                localPassed = true;
                var b = new BatchBlock<int>(5);
                b.Post(1);
                b.Post(2);
                b.Post(3);
                b.Complete();
                localPassed &= b.Receive().Length == 3;
                Assert.True(localPassed, string.Format("{0}: Makes batches of remaining items", localPassed ? "Success" : "Failure"));
            }
        }

        private static BatchBlock<int> ConstructBatchNewWithNMessages(int messagesCount)
        {
            var block = new BatchBlock<int>(1);
            for (int i = 0; i < messagesCount; i++)
            {
                block.Post(i);
            }

            // Spin until the messages have been properly buffered up. 
            // Otherwise TryReceive fails.
            SpinWait.SpinUntil(() => block.OutputCount == messagesCount);

            return block;
        }

        private static bool TestMaxNumberOfGroups(bool greedy, bool sync)
        {
            Contract.Assert(greedy || !sync, "Non-greedy sync doesn't make sense.");
            bool passed = true;

            for (int maxNumberOfGroups = 1; maxNumberOfGroups <= 21; maxNumberOfGroups += 20)
            {
                for (int itemsPerBatch = 1; itemsPerBatch <= 1; itemsPerBatch++)
                {
                    var options = new GroupingDataflowBlockOptions { MaxNumberOfGroups = maxNumberOfGroups, Greedy = greedy };
                    var batch = new BatchBlock<int>(itemsPerBatch, options);

                    // Feed all N batches; all should succeed
                    for (int batchNum = 0; batchNum < maxNumberOfGroups; batchNum++)
                    {
                        var sendAsyncs = new Task<bool>[itemsPerBatch];
                        for (int itemNum = 0; itemNum < itemsPerBatch; itemNum++)
                        {
                            if (sync)
                            {
                                Assert.True(batch.Post(itemNum), string.Format("FAILED batch.Post({0}) on MaxNOG {1}", itemNum, batchNum));
                            }
                            else
                            {
                                sendAsyncs[itemNum] = batch.SendAsync(itemNum);
                            }
                        }
                        if (!sync)
                        {
                            Assert.True(Task.WaitAll(sendAsyncs, 4000),
                                string.Format("FAILED batch.SendAsyncs should have been completed in batch num {0}", batchNum));
                            if (passed)
                            {
                                Assert.True(sendAsyncs.All(t => t.Status == TaskStatus.RanToCompletion && t.Result),
                                    string.Format("FAILED batch.SendAsyncs should have been completed in batch num {0}", batchNum));
                            }
                        }
                    }

                    // Next message should fail in greedy mode
                    if (greedy)
                    {
                        if (sync)
                        {
                            Assert.False(batch.Post(1), "FAILED batch.Post(1) after completed groups should be declind");
                        }
                        else
                        {
                            var t = batch.SendAsync(1);
                            Assert.True(t != null && t.Status == TaskStatus.RanToCompletion && t.Result == false, "FAILED batch.SendAsync(1) after completed groups should be declined");
                        }
                    }

                    // Wait until the all batches are produced
                    Assert.True(SpinWait.SpinUntil(() => batch.OutputCount == maxNumberOfGroups, 4000), "FAILED All batches should have been produced");

                    // Next message should fail, even after groups have been produced
                    if (sync)
                    {
                        Assert.False(batch.Post(1), "FAILED batch.Post(1) after completed groups are output should be declind");
                    }
                    else
                    {
                        var t = batch.SendAsync(1);
                        Assert.True(t != null && t.Status == TaskStatus.RanToCompletion && t.Result == false, "FAILED batch.SendAsync(1) after completed groups are output should be declined");
                    }
                }
            }

            Assert.True(passed, string.Format("{0}", passed ? "Passed" : "FAILED"));
            return passed;
        }

        private static bool TestReleaseOnReserveException(bool linkBadFirst)
        {
            bool passed = true;

            // Bad source throws on ReserveMessage
            var badSource = new ThrowerBlock();
            badSource.Post(ThrowOn.ReserveMessage);

            // Good source never throws
            var goodSource = new ThrowerBlock();
            goodSource.Post(ThrowOn.TryReceive); // Any value unrelated to the offer/consume protocol

            var batch = new BatchBlock<ThrowOn>(2, new GroupingDataflowBlockOptions { Greedy = false });

            // Each linking will offer a message
            if (linkBadFirst)
            {
                badSource.LinkTo(batch);
                goodSource.LinkTo(batch);
            }
            else
            {
                goodSource.LinkTo(batch);
                badSource.LinkTo(batch);
            }

            // The batch must be faulted
            Task.Delay(1).Wait();
            Assert.True(TaskHasFaulted(batch.Completion, "ReserveMessage"));

            // The good message must not be Reserved
            Task.Delay(1).Wait();
            Assert.True(goodSource.LastOperation != ThrowOn.ReserveMessage);
            Assert.True(passed, string.Format("Good message not reserved ({0}) - {1}", goodSource.LastOperation, passed ? "Passed" : "FAILED"));

            Assert.True(passed, string.Format("{0}", passed ? "Passed" : "FAILED"));
            return passed;
        }

        private static bool TestTriggerBatch(int boundedCapacity)
        {
            bool passed = true;

            // Test greedy with batch size of 1 (force should always be a nop)
            {
                bool localPassed = true;
                const int ITERS = 2;
                var b = new BatchBlock<int>(1, new GroupingDataflowBlockOptions() { BoundedCapacity = boundedCapacity });
                for (int i = 0; i < ITERS; i++)
                {
                    b.Post(i);
                    int outputCount = b.OutputCount;
                    b.TriggerBatch();
                    localPassed &= outputCount == b.OutputCount;
                }
                localPassed &= b.OutputCount == ITERS;
                for (int i = 0; i < ITERS; i++)
                {
                    var arr = b.Receive();
                    localPassed &= arr.Length == 1 && arr[0] == i;
                }

                Assert.True(localPassed, string.Format("greedy with batch size of 1 - {0}", localPassed ? "Passed" : "FAILED"));
            }

            // Test greedy with varying batch sizes and smaller queued numbers
            {
                bool localPassed = true;
                foreach (var batchSize in new[] { 3 })
                {
                    foreach (var queuedBeforeTrigger in new[] { 1, batchSize - 1 })
                    {
                        var b = new BatchBlock<int>(batchSize, new GroupingDataflowBlockOptions() { BoundedCapacity = boundedCapacity });
                        for (int p = 1; p <= queuedBeforeTrigger; p++) b.Post(p);
                        localPassed &= b.OutputCount == 0;
                        b.TriggerBatch();
                        b.OutputAvailableAsync().Wait(); // The previous batch is triggered asynchronously when non-Unbounded BoundedCapacity is provided
                        localPassed &= b.OutputCount == 1 && b.Receive().Length == queuedBeforeTrigger;
                        for (int j = 0; j < batchSize; j++)
                        {
                            localPassed &= b.OutputCount == 0;
                            b.Post(j);
                        }
                        localPassed &= b.OutputCount == 1;
                    }
                }

                Assert.True(localPassed, string.Format("greedy with varying batch sizes - {0}", localPassed ? "Passed" : "FAILED"));
                passed &= localPassed;
            }

            // Test greedy with empty queue
            {
                bool localPassed = true;
                foreach (var batchSize in new[] { 1 })
                {
                    var b = new BatchBlock<int>(batchSize, new GroupingDataflowBlockOptions() { BoundedCapacity = boundedCapacity });
                    for (int i = 0; i < 2; i++) b.TriggerBatch();
                    localPassed &= b.OutputCount == 0;
                }

                Assert.True(localPassed, string.Format("greedy with empty queue - {0}", localPassed ? "Passed" : "FAILED"));
                passed &= localPassed;
            }

            // Test greedy after decline
            {
                bool localPassed = true;
                {
                    var b = new BatchBlock<int>(2, new GroupingDataflowBlockOptions() { BoundedCapacity = boundedCapacity });
                    localPassed &= b.OutputCount == 0;
                    b.Complete();
                    localPassed &= b.OutputCount == 0;
                    b.TriggerBatch();
                    localPassed &= b.OutputCount == 0;
                }

                {
                    var b = new BatchBlock<int>(2, new GroupingDataflowBlockOptions() { BoundedCapacity = boundedCapacity });
                    localPassed &= b.OutputCount == 0;
                    b.Post(1);
                    localPassed &= b.OutputCount == 0;
                    b.Complete();
                    localPassed &= b.OutputCount == 1;
                    b.TriggerBatch();
                    localPassed &= b.OutputCount == 1;
                }

                Assert.True(localPassed, string.Format("greedy after decline - {0}", localPassed ? "Passed" : "FAILED"));
                passed &= localPassed;
            }

            // Test greedy after canceled
            {
                bool localPassed = true;
                {
                    var cts = new CancellationTokenSource();
                    var dbo = new GroupingDataflowBlockOptions { CancellationToken = cts.Token, BoundedCapacity = boundedCapacity };
                    var b = new BatchBlock<int>(2, dbo);
                    localPassed &= b.OutputCount == 0;
                    cts.Cancel();
                    localPassed &= b.OutputCount == 0;
                    b.TriggerBatch();
                    localPassed &= b.OutputCount == 0;
                }

                {
                    var cts = new CancellationTokenSource();
                    var dbo = new GroupingDataflowBlockOptions { CancellationToken = cts.Token, BoundedCapacity = boundedCapacity };
                    var b = new BatchBlock<int>(2, dbo);
                    localPassed &= b.OutputCount == 0;
                    b.Post(1);
                    localPassed &= b.OutputCount == 0;
                    cts.Cancel();
                    localPassed &= b.OutputCount == 0;
                    b.TriggerBatch();
                    localPassed &= b.OutputCount == 0;
                }

                Assert.True(localPassed, string.Format("greedy after canceled - {0}", localPassed ? "Passed" : "FAILED"));
                passed &= localPassed;
            }

            // Test greedy with MaxNumberOfGroups == 1
            {
                bool localPassed = true;

                var b = new BatchBlock<int>(2, new GroupingDataflowBlockOptions { MaxNumberOfGroups = 1, BoundedCapacity = boundedCapacity });
                b.Post(1);
                localPassed &= b.OutputCount == 0;
                b.TriggerBatch();
                b.OutputAvailableAsync().Wait(); // The previous batch is triggered asynchronously when non-Unbounded BoundedCapacity is provided
                localPassed &= b.OutputCount == 1;
                localPassed &= b.Post(2) == false;
                b.TriggerBatch();
                localPassed &= b.OutputCount == 1;

                Assert.True(localPassed, string.Format("greedy with MaxNumberOfGroups == 1 - {0}", localPassed ? "Passed" : "FAILED"));
                passed &= localPassed;
            }

            // Test non-greedy with no queued or postponed messages
            {
                bool localPassed = true;

                var dbo = new GroupingDataflowBlockOptions { Greedy = false, BoundedCapacity = boundedCapacity };
                var b = new BatchBlock<int>(3, dbo);
                localPassed &= b.OutputCount == 0;
                b.TriggerBatch();
                localPassed &= b.OutputCount == 0;

                Assert.True(localPassed, string.Format("non-greedy with no mesages - {0}", localPassed ? "Passed" : "FAILED"));
                passed &= localPassed;
            }

            // Test non-greedy with no queued but postponed messages
            {
                bool localPassed = true;
                var dbo = new GroupingDataflowBlockOptions { Greedy = false, BoundedCapacity = boundedCapacity };

                const int BATCH_SIZE = 10;
                for (int numPostponedMessages = 1; numPostponedMessages < BATCH_SIZE; numPostponedMessages++)
                {
                    var b = new BatchBlock<int>(BATCH_SIZE, dbo);
                    localPassed &= b.OutputCount == 0;
                    for (int i = 0; i < numPostponedMessages; i++) localPassed &= !b.SendAsync(i).IsCompleted;
                    b.TriggerBatch();
                    var output = b.Receive();
                    localPassed &= output.Length == numPostponedMessages;
                    for (int i = 0; i < output.Length; i++) localPassed &= output[i] == i;
                    localPassed &= b.OutputCount == 0;
                    b.TriggerBatch();
                    localPassed &= b.OutputCount == 0;
                }

                Assert.True(localPassed, string.Format("non-greedy with postponed, no queued - {0}", localPassed ? "Passed" : "FAILED"));
                passed &= localPassed;
            }

            return passed;
        }

        private static bool TestTriggerBatchRacingWithSendAsync(bool greedy)
        {
            bool passed = true;
            const int batchSize = 2;
            const int iterations = 1;
            const int waitTimeout = 100;
            var dbo = new GroupingDataflowBlockOptions { Greedy = greedy };

            for (int iter = 0; iter < iterations; iter++)
            {
                bool localPassed = true;
                var sendAsyncTasks = new Task<bool>[batchSize - 1];
                Task<bool> lastSendAsyncTask = null;
                var racerReady = new ManualResetEventSlim();
                var racerDone = new ManualResetEventSlim();
                int[] output1 = null;
                int[] output2 = null;

                // Blocks
                var batch = new BatchBlock<int>(batchSize, dbo);
                var terminator = new ActionBlock<int[]>(x => { if (output1 == null) output1 = x; else output2 = x; });
                batch.LinkTo(terminator);

                // Queue up batchSize-1 input items
                for (int i = 0; i < batchSize - 1; i++) sendAsyncTasks[i] = batch.SendAsync(i);
                var racer = Task.Factory.StartNew(() =>
                                    {
                                        racerReady.Set();
                                        lastSendAsyncTask = batch.SendAsync(batchSize - 1);
                                        racerDone.Set();
                                    });

                // Wait for the racer to get ready and trigger
                localPassed &= (racerReady.Wait(waitTimeout));
                batch.TriggerBatch();
                Assert.True(localPassed, "The racer task FAILED to start.");

                // Wait for the SendAsync tasks to complete
                localPassed &= Task.WaitAll(sendAsyncTasks, waitTimeout);
                Assert.True(localPassed, "SendAsync tasks FAILED to complete");

                // Wait for a batch to be produced
                if (localPassed)
                {
                    localPassed &= SpinWait.SpinUntil(() => output1 != null, waitTimeout);
                    Assert.True(localPassed, "FAILED to produce a batch");
                }

                if (localPassed && output1.Length < batchSize)
                {
                    // If the produced batch is not full, we'll trigger one more and count the items.
                    // However, we need to make sure the last message has been offered. Otherwise this 
                    // trigger will have no effect.
                    racerDone.Wait(waitTimeout);
                    batch.TriggerBatch();

                    if (localPassed)
                    {
                        // Wait for the last SendAsync task to complete
                        localPassed &= SpinWait.SpinUntil(() => lastSendAsyncTask != null, waitTimeout);
                        localPassed &= lastSendAsyncTask.Wait(waitTimeout);
                        Assert.True(localPassed, "The last SendAsync task FAILED to complete");
                    }

                    // Wait for a second batch to be produced
                    if (localPassed)
                    {
                        localPassed &= SpinWait.SpinUntil(() => output2 != null, waitTimeout);
                        Assert.True(localPassed, "FAILED to produce a second batch");
                    }

                    //Verify the total number of input items propagated
                    if (localPassed)
                    {
                        localPassed &= output1.Length + output2.Length == batchSize;
                        Assert.True(localPassed, string.Format("FAILED to propagate {0} input items. count1={1}, count2={2}",
                                                                            batchSize, output1.Length, output2.Length));
                    }
                }

                passed &= localPassed;
                if (!localPassed)
                {
                    Assert.True(localPassed, string.Format("Iteration={0}", iter));
                    Assert.True(localPassed, string.Format("Count1={0}", output1 == null ? "null" : output1.Length.ToString()));
                    Assert.True(localPassed, string.Format("Count2={0}", output2 == null ? "null" : output2.Length.ToString()));
                    break;
                }
            }

            return passed;
        }

        private static bool TestTriggerBatchRacingWithComplete(bool greedy)
        {
            bool passed = true;
            const int batchSize = 2;
            const int iterations = 1;
            const int waitTimeout = 100;
            var dbo = new GroupingDataflowBlockOptions { Greedy = greedy };

            for (int iter = 0; iter < iterations; iter++)
            {
                bool localPassed = true;
                var sendAsyncTasks = new Task<bool>[batchSize - 1];
                var racerReady = new ManualResetEventSlim();
                int[] output1 = null;
                int[] output2 = null;

                // Blocks
                var batch = new BatchBlock<int>(batchSize, dbo);
                var terminator = new ActionBlock<int[]>(x => { if (output1 == null) output1 = x; else output2 = x; });
                batch.LinkTo(terminator);

                // Queue up batchSize-1 input items
                for (int i = 0; i < batchSize - 1; i++) sendAsyncTasks[i] = batch.SendAsync(i);
                var racer = Task.Factory.StartNew(() =>
                {
                    racerReady.Set();
                    batch.Complete();
                });

                // Wait for the racer to get ready and trigger
                localPassed &= racerReady.Wait(waitTimeout);
                batch.TriggerBatch();
                Assert.True(localPassed, "The racer task FAILED to start.");

                if (localPassed)
                {
                    // Wait for the SendAsync tasks to complete
                    localPassed &= Task.WaitAll(sendAsyncTasks, waitTimeout);
                    Assert.True(localPassed, "SendAsync tasks FAILED to complete");
                }

                // Do this verification only in greedy mode, because non-greedy is non-deterministic
                if (greedy)
                {
                    // Wait for a batch to be produced
                    if (localPassed)
                    {
                        localPassed &= SpinWait.SpinUntil(() => output1 != null, waitTimeout);
                        Assert.True(localPassed, "FAILED to produce a batch");
                    }

                    if (localPassed)
                    {
                        //Verify the number of input items propagated
                        localPassed &= output1.Length == batchSize - 1;
                        Assert.True(localPassed, string.Format("FAILED to propagate {0} input items. count1={1}",
                                                                            batchSize, output1.Length));
                    }
                }

                // Wait for the block to complete
                if (localPassed)
                {
                    localPassed &= batch.Completion.Wait(waitTimeout);
                    Assert.True(localPassed, "The block FAILED to complete");
                }

                // There should never be a second batch produced
                if (localPassed)
                {
                    localPassed &= output2 == null;
                    Assert.True(localPassed, "FAILED not to produce a second batch");
                }

                passed &= localPassed;
                if (!localPassed)
                {
                    Assert.True(localPassed, string.Format("Iteration={0}", iter));
                    Assert.True(localPassed, string.Format("Count1={0}", output1 == null ? "null" : output1.Length.ToString()));
                    break;
                }
            }

            return passed;
        }
    }
}
