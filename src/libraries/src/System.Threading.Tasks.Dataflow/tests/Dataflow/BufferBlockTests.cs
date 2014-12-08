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
        public void RunBufferBlockTests()
        {
            Assert.True(IDataflowBlockTestHelper.TestToString(nameFormat => nameFormat != null ? new BufferBlock<int>(new DataflowBlockOptions() { NameFormat = nameFormat }) : new BufferBlock<int>()));
            Assert.True(ISourceBlockTestHelper.TestLinkTo<int>(ConstructBufferNewWithNMessages(2), 1));
            Assert.True(ISourceBlockTestHelper.TestReserveMessageAndReleaseReservation<int>(ConstructBufferNewWithNMessages(1)));
            Assert.True(ISourceBlockTestHelper.TestConsumeMessage<int>(ConstructBufferNewWithNMessages(1)));
            Assert.True(ISourceBlockTestHelper.TestTryReceiveWithFilter<int>(ConstructBufferNewWithNMessages(1), 1));
            Assert.True(ISourceBlockTestHelper.TestTryReceiveAll<int>(ConstructBufferNewWithNMessages(1), 1));
            Assert.True(ITargetBlockTestHelper.TestOfferMessage<int>(new BufferBlock<int>()));
            Assert.True(ITargetBlockTestHelper.TestPost<int>(new BufferBlock<int>()));
            Assert.True(ITargetBlockTestHelper.TestComplete<int>(new BufferBlock<int>()));
            Assert.True(ITargetBlockTestHelper.TestCompletionTask<int>(new BufferBlock<int>()));
        }

        private static BufferBlock<int> ConstructBufferNewWithNMessages(int messagesCount)
        {
            var block = new BufferBlock<int>();
            for (int i = 0; i < messagesCount; i++)
            {
                block.Post(i);
            }

            // Spin until the messages have been properly buffered up. 
            // Otherwise TryReceive fails.
            SpinWait.SpinUntil(() => block.Count == messagesCount);

            return block;
        }

        //[Fact(Skip = "outerloop")]
        public void TestBufferBlockBounding()
        {
            const int WAIT_TIMEOUT = 4000; // wait at most 4 seconds for a particularly race-condition

            // Test buffer doesn't exceed limit
            {
                bool localPassed = true;

                for (int boundedCapacity = 1; boundedCapacity <= 3 && localPassed; boundedCapacity += 2)
                {
                    var b = new BufferBlock<int>(new DataflowBlockOptions { BoundedCapacity = boundedCapacity });
                    for (int i = 0; i < boundedCapacity; i++)
                    {
                        var send = b.SendAsync(i);
                        Assert.True(send.Wait(0) && send.Result, "Send should succeed as capacity not yet reached");
                    }

                    for (int i = boundedCapacity; i < boundedCapacity + 2 && localPassed; i++)
                    {
                        Assert.True(b.Count == boundedCapacity, "Count should equal bounded capacity after all posts completed");

                        var t = b.SendAsync(i);
                        Assert.True(!t.IsCompleted, "Send should not have completed on a full buffer");
                        b.Receive();
                        Assert.True(t.Wait(WAIT_TIMEOUT), "The send should have completed before the timeout");
                        Assert.True(t.IsCompleted && t.Result, "The send should have completed successfully");
                        Assert.True(SpinWait.SpinUntil(() => b.Count == boundedCapacity, WAIT_TIMEOUT), "The count should be back at the bounded capacity after successful send");
                    }

                    int remainingCount = b.Count;
                    while (b.Count > 0)
                    {
                        remainingCount--;
                        int ignored;
                        Assert.True(b.TryReceive(out ignored), "Should have been able to successfully remove each item");
                    }

                    Assert.True(remainingCount == 0, "Should be no items left after removals");
                }

                Assert.True(localPassed, string.Format("{0}: Doesn't exceed limits", localPassed ? "Success" : "Failure"));
            }

            // Test correct ordering
            {
                bool localPassed = true;

                for (int boundedCapacity = 1; boundedCapacity <= 3 && localPassed; boundedCapacity += 2)
                {
                    int iters = boundedCapacity + 2;
                    var tasks = new Task<bool>[iters];

                    var b = new BufferBlock<int>(new DataflowBlockOptions { BoundedCapacity = boundedCapacity });
                    for (int i = 0; i < boundedCapacity; i++)
                    {
                        var t = b.SendAsync(i);
                        Assert.True(t.IsCompleted && t.Result, "Sends until capacity reached should complete immediately and successfully");
                        tasks[i] = t;
                    }

                    for (int i = boundedCapacity; i < iters; i++)
                    {
                        var t = b.SendAsync(i);
                        Assert.True(!t.IsCompleted, "Sends after capacity reached should not be completed");
                        tasks[i] = t;
                    }

                    for (int i = 0; i < iters && localPassed; i++)
                    {
                        if (i >= boundedCapacity & i < iters - boundedCapacity)
                        {
                            Assert.True(!tasks[i + boundedCapacity].IsCompleted, "Remaining sends should not yet be completed");
                        }
                        Assert.True(b.Receive() == i, "Received value should match sent value in correct order");
                        Assert.True(tasks[i].Wait(WAIT_TIMEOUT) && tasks[i].Result, "Next sender task should have completed");
                    }
                }

                Assert.True(localPassed, string.Format("{0}: Correct ordering", localPassed ? "Success" : "Failure"));
            }

            // Test declining
            {
                bool localPassed = true;

                for (int boundedCapacity = 1; boundedCapacity <= 3 && localPassed; boundedCapacity += 2)
                {
                    var b = new BufferBlock<string>(new DataflowBlockOptions { BoundedCapacity = boundedCapacity });

                    int total = boundedCapacity + 2;
                    var tasks = new Task<bool>[total];

                    for (int i = 0; i < total; i++)
                    {
                        tasks[i] = b.SendAsync(i.ToString());
                    }

                    for (int i = 0; i < total; i++)
                    {
                        Assert.True((i < boundedCapacity) == tasks[i].IsCompleted, "All sends below the capacity should have completed");
                    }

                    b.Complete();

                    Assert.True(Task.WaitAll(tasks, WAIT_TIMEOUT), "All postponed sends should complete once declined");

                    for (int i = 0; i < total; i++)
                    {
                        Assert.True(
                            tasks[i].IsCompleted && tasks[i].Result == (i < boundedCapacity),
                            "All postponed/declined sends should have returned false");
                    }
                }
            }

            // Test circular linking
            {
                bool localPassed = true;

                for (int boundedCapacity = 1; boundedCapacity <= 3 && localPassed; boundedCapacity += 2)
                {
                    var b = new BufferBlock<int>(new DataflowBlockOptions { BoundedCapacity = boundedCapacity });
                    b.LinkTo(b);

                    for (int i = 0; i < boundedCapacity - 1; i++)
                    {
                        var send = b.SendAsync(i);
                        localPassed &= send.Wait(0) && send.Result;
                    }

                    Task.Delay(250).Wait(); // allow some arbitrary time for the block to propagate to itself
                    Assert.True(SpinWait.SpinUntil(() => b.Count == boundedCapacity - 1, WAIT_TIMEOUT),
                        "Count should eventually be equal to number posted, even with circular linking");
                    var sendLast = b.SendAsync(boundedCapacity - 1);
                    Assert.True(sendLast.Wait(WAIT_TIMEOUT) && sendLast.Result,
                        "Posting the last item to make the capacity should succeed");
                    Assert.True(SpinWait.SpinUntil(() => b.Count == boundedCapacity, WAIT_TIMEOUT),
                        "Count should now equal the bounded capacity");
                    var sendExcess = b.SendAsync(boundedCapacity);
                    Assert.True(!sendExcess.Wait(500),
                        "Additional posts should fail");

                    if (localPassed)
                    {
                        var items = new HashSet<int>(Enumerable.Range(0, boundedCapacity + 1)); //Include the excess item
                        for (int i = 0; i < boundedCapacity + 1 && localPassed; i++)
                        {
                            int value = -1;
                            Assert.True(SpinWait.SpinUntil(() => b.TryReceive(out value), WAIT_TIMEOUT),
                                "Should be able to receive all previously posted items");
                            if (localPassed)
                            {
                                Assert.True(
                                    items.Remove(value),
                                    "All previously posted values should be found");
                            }
                        }

                        Assert.True(items.Count == 0, "All items should have been removed");
                        Assert.True(b.Count == 0, "Nothing should be left in the buffer");
                    }
                }
            }

            // Test producer-consumer
            {
                bool localPassed = true;
                const int ITERS = 2;

                for (int boundedCapacity = 1; boundedCapacity <= 3 && localPassed; boundedCapacity += 2)
                {
                    var b = new BufferBlock<int>(new DataflowBlockOptions { BoundedCapacity = boundedCapacity });

                    var p = Task.Factory.StartNew(() =>
                    {
                        Assert.True(b.Count == 0, "Nothing should be in the buffer yet");
                        for (int i = 0; i < ITERS; i++)
                        {
                            if (!b.SendAsync(i).Wait(WAIT_TIMEOUT))
                            {
                                localPassed = false;
                                Assert.True(localPassed, "Send should have completed within timeout");
                            }
                        }
                    });

                    var c = Task.Factory.StartNew(() =>
                    {
                        for (int i = 0; i < ITERS; i++)
                        {
                            var t = b.ReceiveAsync();
                            if (!t.Wait(WAIT_TIMEOUT))
                            {
                                localPassed = false;
                                Assert.True(localPassed, "Receive should have completed within timeout");
                            }
                            if (t.Status != TaskStatus.RanToCompletion || t.Result != i)
                            {
                                localPassed = false;
                                Assert.True(localPassed, "Receive should have completed with correct value");
                            }
                        }
                        Assert.True(b.Count == 0, "The buffer should be empty after all items received");
                    });

                    if (!Task.WaitAll(new[] { p, c }, WAIT_TIMEOUT))
                    {
                        localPassed = false;
                        Assert.True(localPassed, "Both producer and consumer should have completed in allotted time");
                    }
                }
            }

            // Test multi-item removal
            {
                bool localPassed = true;

                for (int boundedCapacity = 1; boundedCapacity <= 3 && localPassed; boundedCapacity += 2)
                {
                    var b = new BufferBlock<int>(new DataflowBlockOptions { BoundedCapacity = boundedCapacity });

                    for (int iter = 0; iter < 2; iter++)
                    {
                        for (int i = 0; i < boundedCapacity; i++)
                        {
                            var send = b.SendAsync(i);
                            localPassed &= send.Wait(0) && send.Result;
                        }

                        IList<int> output;
                        Assert.True(b.TryReceiveAll(out output), "Data should have been available");
                        Assert.True(output.Count == boundedCapacity, "Should have removed all posted items");
                    }
                }
            }

            // Test releasing of postponed messages
            {
                const int excess = 10;
                bool localPassed = true;

                for (int boundedCapacity = 1; boundedCapacity <= 3 && localPassed; boundedCapacity += 2)
                {
                    for (int iter = 0; iter < 2; iter++)
                    {
                        var b = new BufferBlock<int>(new DataflowBlockOptions { BoundedCapacity = boundedCapacity });

                        var sendAsync = new Task<bool>[boundedCapacity + excess];
                        for (int i = 0; i < boundedCapacity + excess; i++) sendAsync[i] = b.SendAsync(i);

                        b.Complete();

                        for (int i = 0; i < boundedCapacity; i++)
                        {
                            Assert.True(sendAsync[i].Result, string.Format("bc={0} iter={1} send failed but should have succeeded", boundedCapacity, iter));
                        }

                        for (int i = 0; i < excess; i++)
                        {
                            Assert.True(!sendAsync[boundedCapacity + i].Result, string.Format("bc={0} iter={1} send succeeded but should have failed", boundedCapacity, iter));
                        }
                    }
                }
            }
        }

        [Fact]
        public void TestBufferBlockConstructor()
        {
            // without option
            var block = new BufferBlock<int>();
            Assert.False(block.Count != 0, "Constructor failed! Count returned a non zero value for a brand new BufferBlock.");

            //with not cancelled token and default scheduler
            block = new BufferBlock<int>(new DataflowBlockOptions { MaxMessagesPerTask = 1 });
            Assert.False(block.Count != 0, "Constructor failed! Count returned a non zero value for a brand new BufferBlock.");

            //with a cancelled token and default scheduler
            var token = new CancellationToken(true);
            block = new BufferBlock<int>(new DataflowBlockOptions { MaxMessagesPerTask = 1, CancellationToken = token });
            Assert.False(block.Count != 0, "Constructor failed! Count returned a non zero value for a brand new BufferBlock.");
        }

        [Fact]
        public void TestSourceCoreSpecificsThroughBufferBlock()
        {
            var messageHeader = new DataflowMessageHeader(1);
            bool consumed;
            var block = new BufferBlock<int>();
            ((ITargetBlock<int>)block).OfferMessage(messageHeader, 42, null, false);

            var nonlinkedTarget = new ActionBlock<int>(i => { });
            bool reserved = ((ISourceBlock<int>)block).ReserveMessage(messageHeader, nonlinkedTarget);
            Assert.False(!reserved, "Failure: SourceCore did not allow a non-linked target to reserve");

            ((ISourceBlock<int>)block).ReleaseReservation(messageHeader, nonlinkedTarget);
            ((ISourceBlock<int>)block).ConsumeMessage(messageHeader, new ActionBlock<int>(i => { }), out consumed);
            Assert.False(!consumed || block.Count != 0, "Failure: SourceCore did not allow a non-linked target to consume");
        }

        [Fact]
        public async Task TestBufferBlockOutputAvailableAsyncAfterTryReceiveAll()
        {
            var multipleConcurrentTestsTask =
                Task.WhenAll(
                    Enumerable.Repeat(-1, 1000)
                        .Select(_ => GetOutputAvailableAsyncTaskAfterTryReceiveAllOnNonEmptyBufferBlock()));
            var timeoutTask = Task.Delay(100);
            var completedTask = await Task.WhenAny(multipleConcurrentTestsTask, timeoutTask);

            Assert.True(completedTask != timeoutTask);
        }

        private Task GetOutputAvailableAsyncTaskAfterTryReceiveAllOnNonEmptyBufferBlock()
        {
            var buffer = new BufferBlock<object>();

            buffer.Post(null);

            IList<object> items;
            buffer.TryReceiveAll(out items);

            var outputAvailableAsync = buffer.OutputAvailableAsync();

            buffer.Post(null);

            return outputAvailableAsync;
        }

        [Fact]
        public void TestBufferBlockInvalidArgumentValidation()
        {
            Assert.Throws<ArgumentNullException>(() => new BufferBlock<int>(null));
            Assert.True(ITargetBlockTestHelper.TestArgumentsExceptions<int>(new BufferBlock<int>()));
            Assert.True(ISourceBlockTestHelper.TestArgumentsExceptions<int>(new BufferBlock<int>()));
        }

        //[Fact(Skip = "Outerloop")]
        public void TestBufferBlockCompletionOrder()
        {
            const int ITERATIONS = 1000;
            for (int iter = 0; iter < ITERATIONS; iter++)
            {
                var cts = new CancellationTokenSource();
                var options = new DataflowBlockOptions() { CancellationToken = cts.Token };
                var buffer = new BufferBlock<int>(options);

                buffer.Post(1);
                cts.Cancel();
                try { buffer.Completion.Wait(); }
                catch { }
                    
                Assert.False(buffer.Count != 0, string.Format("Iteration {0}: Completed before clearing messages.", iter));
            }
        }

        [Fact]
        public void TestBufferBlockCount()
        {
            BufferBlock<int> bufferBlock = new BufferBlock<int>();
            Assert.False(bufferBlock.Count != 0, "BufferBlock.Count failed! an initialized block has a non zero count");

            for (int i = 0; i < 10; i++)
            {
                ((ITargetBlock<int>)bufferBlock).OfferMessage(new DataflowMessageHeader(1 + i), i, null, false); // Message ID doesn't matter because consumeTosAccept:false
            }

            Assert.False(bufferBlock.Count != 10, string.Format("BufferBlock.Count failed! expected {0}, actual {1}", 10, bufferBlock.Count));

            IList<int> items;
            bool result = bufferBlock.TryReceiveAll(out items);

            Assert.False(bufferBlock.Count != 0, string.Format("BufferBlock.Count failed! expected {0}, actual {1}", 0, bufferBlock.Count));
        }

        //[Fact(Skip = "Outerloop")]
        public void RunBufferBlockConformanceTests()
        {
            bool localPassed;
            // Do everything twice - once through OfferMessage and Once through Post
            for (FeedMethod feedMethod = FeedMethod._First; feedMethod < FeedMethod._Count; feedMethod++)
            {
                Func<DataflowBlockOptions, TargetProperties<int>> bufferBlockFactory =
                    options =>
                    {
                        BufferBlock<int> bufferBlock = new BufferBlock<int>(options);
                        ActionBlock<int> actionBlock = new ActionBlock<int>(i => TrackCaptures(i), (ExecutionDataflowBlockOptions)options);

                        bufferBlock.LinkTo(actionBlock);

                        return new TargetProperties<int> { Target = bufferBlock, Capturer = actionBlock, ErrorVerifyable = false };
                    };
                CancellationTokenSource cancellationSource = new CancellationTokenSource();
                var defaultOptions = new ExecutionDataflowBlockOptions();
                var dopOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
                var mptOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, MaxMessagesPerTask = 10 };
                var cancellationOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, MaxMessagesPerTask = 100, CancellationToken = cancellationSource.Token };

                Assert.True(FeedTarget(bufferBlockFactory, defaultOptions, 1, Intervention.None, null, feedMethod, true));
                Assert.True(FeedTarget(bufferBlockFactory, defaultOptions, 10, Intervention.None, null, feedMethod, true));
                Assert.True(FeedTarget(bufferBlockFactory, dopOptions, 1000, Intervention.None, null, feedMethod, true));
                Assert.True(FeedTarget(bufferBlockFactory, mptOptions, 10000, Intervention.None, null, feedMethod, true));
                Assert.True(FeedTarget(bufferBlockFactory, mptOptions, 10000, Intervention.Complete, null, feedMethod, true));
                Assert.True(FeedTarget(bufferBlockFactory, cancellationOptions, 10000, Intervention.Cancel, cancellationSource, feedMethod, true));
            }

            // Test chained Post/Receive
            {
                localPassed = true;
                const int ITERS = 2;
                var network = Chain<BufferBlock<int>, int>(4, () => new BufferBlock<int>());
                for (int i = 0; i < ITERS; i++)
                {
                    network.Post(i);
                    localPassed &= (((IReceivableSourceBlock<int>)network).Receive() == i);
                }
                Assert.True(localPassed, string.Format("{0}: Chained Post/Receive", localPassed ? "Success" : "Failure"));
            }

            // Test chained SendAsync/Receive
            {
                localPassed = true;
                const int ITERS = 2;
                var network = Chain<BufferBlock<int>, int>(4, () => new BufferBlock<int>());
                for (int i = 0; i < ITERS; i++)
                {
                    network.SendAsync(i);
                    localPassed &= (((IReceivableSourceBlock<int>)network).Receive() == i);
                }
                Assert.True(localPassed, string.Format("{0}: Chained SendAsync/Receive", localPassed ? "Success" : "Failure"));
            }

            // Test chained Post all then Receive
            {
                localPassed = true;
                const int ITERS = 2;
                var network = Chain<BufferBlock<int>, int>(4, () => new BufferBlock<int>());
                for (int i = 0; i < ITERS; i++) localPassed &= network.Post(i) == true;
                for (int i = 0; i < ITERS; i++) localPassed &= ((IReceivableSourceBlock<int>)network).Receive() == i;
                Assert.True(localPassed, string.Format("{0}: Chained Post all then Receive", localPassed ? "Success" : "Failure"));
            }

            // Test chained SendAsync all then Receive
            {
                localPassed = true;
                const int ITERS = 2;
                var network = Chain<BufferBlock<int>, int>(4, () => new BufferBlock<int>());
                var tasks = new Task[ITERS];
                for (int i = 1; i <= ITERS; i++) tasks[i - 1] = network.SendAsync(i);
                Task.WaitAll(tasks);
                int total = 0;
                for (int i = 1; i <= ITERS; i++) total += ((IReceivableSourceBlock<int>)network).Receive();
                localPassed &= (total == ((ITERS * (ITERS + 1)) / 2));
                Assert.True(localPassed, string.Format("{0}: Chained SendAsync all then Receive", localPassed ? "Success" : "Failure"));
            }

            // Test using a precanceled token
            {
                localPassed = true;
                try
                {
                    var cts = new CancellationTokenSource();
                    cts.Cancel();
                    var dbo = new DataflowBlockOptions { CancellationToken = cts.Token };
                    var bb = new BufferBlock<int>(dbo);

                    int ignoredValue;
                    IList<int> ignoredValues;
                    localPassed &= bb.LinkTo(new ActionBlock<int>(delegate { })) != null;
                    localPassed &= bb.SendAsync(42).Result == false;
                    localPassed &= bb.TryReceiveAll(out ignoredValues) == false;
                    localPassed &= bb.Post(42) == false;
                    localPassed &= bb.Count == 0;
                    localPassed &= bb.TryReceive(out ignoredValue) == false;
                    localPassed &= bb.Completion != null;
                    bb.Complete();
                }
                catch (Exception)
                {
                    localPassed = false;
                }
                Assert.True(localPassed, string.Format("    {0}: Precanceled tokens work correctly", localPassed ? "Success" : "Failure"));
            }
        }
    }
}
