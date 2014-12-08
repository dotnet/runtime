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
        public void RunActionBlockTests()
        {
            Assert.True(IDataflowBlockTestHelper.TestToString(nameFormat => nameFormat != null ? new ActionBlock<int>(x => { }, new ExecutionDataflowBlockOptions() { NameFormat = nameFormat }) : new ActionBlock<int>(x => { })));
            Assert.True(IDataflowBlockTestHelper.TestToString(nameFormat =>
                nameFormat != null ?
                new ActionBlock<int>(x => { }, new ExecutionDataflowBlockOptions() { NameFormat = nameFormat, SingleProducerConstrained = true }) :
                new ActionBlock<int>(x => { }, new ExecutionDataflowBlockOptions() { SingleProducerConstrained = true })));

            Assert.True(ITargetBlockTestHelper.TestArgumentsExceptions<int>(new ActionBlock<int>(i => { })));
            Assert.True(ITargetBlockTestHelper.TestOfferMessage<int>(new ActionBlock<int>(i => { }, new ExecutionDataflowBlockOptions { SingleProducerConstrained = true })));

            Assert.True(ITargetBlockTestHelper.TestPost<int>(new ActionBlock<int>(i => { }, new ExecutionDataflowBlockOptions { SingleProducerConstrained = true })));
            Assert.True(ITargetBlockTestHelper.TestComplete<int>(new ActionBlock<int>(i => { }, new ExecutionDataflowBlockOptions { SingleProducerConstrained = true })));
            Assert.True(ITargetBlockTestHelper.TestCompletionTask<int>(new ActionBlock<int>(i => { }, new ExecutionDataflowBlockOptions { SingleProducerConstrained = true })));

            Assert.True(ITargetBlockTestHelper.TestNonGreedyPost(new ActionBlock<int>(x => { Task.Delay(1); }, new ExecutionDataflowBlockOptions() { BoundedCapacity = 1 })));
        }

        [Fact]
        public void TestActionBlockConstructor()
        {
            // SYNC
            // without option
            var block = new ActionBlock<int>(i => { });
            Assert.False(block.InputCount != 0, "Constructor failed! InputCount returned a non zero value for a brand new ActionBlock." + block.InputCount);

            //with not cancelled token and default scheduler
            block = new ActionBlock<int>(i => { }, new ExecutionDataflowBlockOptions { MaxMessagesPerTask = 1 });
            Assert.False(block.InputCount != 0, "Constructor failed! InputCount returned a non zero value for a brand new ActionBlock.");

            //with a cancelled token and default scheduler
            var token = new CancellationToken(true);
            block = new ActionBlock<int>(i => { }, new ExecutionDataflowBlockOptions { MaxMessagesPerTask = 1, CancellationToken = token });
            Assert.False(block.InputCount != 0, "Constructor failed! InputCount returned a non zero value for a brand new ActionBlock.");

            // ASYNC (a copy of the sync but with constructors returning Task instead of void
            var dummyTask = new Task(() => { });

            // without option
            block = new ActionBlock<int>(i => dummyTask);
            Assert.False(block.InputCount != 0, "Constructor failed! InputCount returned a non zero value for a brand new ActionBlock.");

            //with not cancelled token and default scheduler
            block = new ActionBlock<int>(i => dummyTask, new ExecutionDataflowBlockOptions { MaxMessagesPerTask = 1 });
            Assert.False(block.InputCount != 0, "Constructor failed! InputCount returned a non zero value for a brand new ActionBlock.");

            //with a cancelled token and default scheduler
            token = new CancellationToken(true);
            block = new ActionBlock<int>(i => dummyTask, new ExecutionDataflowBlockOptions { MaxMessagesPerTask = 1, CancellationToken = token });
            Assert.False(block.InputCount != 0, "Constructor failed! InputCount returned a non zero value for a brand new ActionBlock.");
        }

        [Fact]
        public void TestActionBlockInvalidArgumentValidation()
        {
            Assert.Throws<ArgumentNullException>(() => new ActionBlock<int>((Func<int, Task>)null));
            Assert.Throws<ArgumentNullException>(() => new ActionBlock<int>((Func<int, Task>)null));
            Assert.Throws<ArgumentNullException>(() => new ActionBlock<int>(i => { }, null));
            Assert.Throws<ArgumentNullException>(() => new ActionBlock<int>(i => Task.Factory.StartNew(() => { }), null));
        }

        //[Fact(Skip = "Outerloop")]
        public void RunActionBlockConformanceTests()
        {
            // SYNC
            // Do everything twice - once through OfferMessage and Once through Post
            for (FeedMethod feedMethod = FeedMethod._First; feedMethod < FeedMethod._Count; feedMethod++)
            {
                Func<DataflowBlockOptions, TargetProperties<int>> actionBlockFactory =
                    options =>
                    {
                        ITargetBlock<int> target = new ActionBlock<int>(i => TrackCaptures(i), (ExecutionDataflowBlockOptions)options);
                        return new TargetProperties<int> { Target = target, Capturer = target, ErrorVerifyable = true };
                    };

                CancellationTokenSource cancellationSource = new CancellationTokenSource();
                var defaultOptions = new ExecutionDataflowBlockOptions();
                var dopOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
                var mptOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, MaxMessagesPerTask = 1 };
                var cancellationOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, MaxMessagesPerTask = 1, CancellationToken = cancellationSource.Token };
                var spscOptions = new ExecutionDataflowBlockOptions { SingleProducerConstrained = true };
                var spscMptOptions = new ExecutionDataflowBlockOptions { SingleProducerConstrained = true, MaxMessagesPerTask = 10 };

                Assert.True(FeedTarget(actionBlockFactory, defaultOptions, 1, Intervention.None, null, feedMethod, true));
                Assert.True(FeedTarget(actionBlockFactory, dopOptions, 1, Intervention.None, null, feedMethod, true));
                Assert.True(FeedTarget(actionBlockFactory, mptOptions, 1, Intervention.None, null, feedMethod, true));
                Assert.True(FeedTarget(actionBlockFactory, mptOptions, 1, Intervention.Complete, null, feedMethod, true));
                Assert.True(FeedTarget(actionBlockFactory, cancellationOptions, 1, Intervention.Cancel, cancellationSource, feedMethod, true));

                Assert.True(FeedTarget(actionBlockFactory, spscOptions, 1, Intervention.None, null, feedMethod, true));
                Assert.True(FeedTarget(actionBlockFactory, spscOptions, 1, Intervention.Complete, null, feedMethod, true));
                Assert.True(FeedTarget(actionBlockFactory, spscMptOptions, 1, Intervention.None, null, feedMethod, true));
                Assert.True(FeedTarget(actionBlockFactory, spscMptOptions, 1, Intervention.Complete, null, feedMethod, true));
            }

            // Test scheduler usage
            {
                bool localPassed = true;
                for (int trial = 0; trial < 2; trial++)
                {
                    var sts = new SimpleTaskScheduler();

                    var options = new ExecutionDataflowBlockOptions { TaskScheduler = sts, MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded, MaxMessagesPerTask = 1 };
                    if (trial == 0) options.SingleProducerConstrained = true;

                    var ab = new ActionBlock<int>(i => localPassed &= TaskScheduler.Current.Id == sts.Id, options);
                    for (int i = 0; i < 2; i++) ab.Post(i);
                    ab.Complete();
                    ab.Completion.Wait();
                }

                Assert.True(localPassed, string.Format("{0}: Correct scheduler usage", localPassed ? "Success" : "Failure"));
            }

            // Test count
            {
                bool localPassed = true;
                for (int trial = 0; trial < 2; trial++)
                {
                    var barrier1 = new Barrier(2);
                    var barrier2 = new Barrier(2);
                    var ab = new ActionBlock<int>(i =>
                    {
                        barrier1.SignalAndWait();
                        barrier2.SignalAndWait();
                    }, new ExecutionDataflowBlockOptions { SingleProducerConstrained = (trial == 0) });
                    for (int iter = 0; iter < 2; iter++)
                    {
                        for (int i = 1; i <= 2; i++) ab.Post(i);
                        for (int i = 1; i >= 0; i--)
                        {
                            barrier1.SignalAndWait();
                            localPassed &= i == ab.InputCount;
                            barrier2.SignalAndWait();
                        }
                    }
                }

                Assert.True(localPassed, string.Format("{0}: InputCount", localPassed ? "Success" : "Failure"));
            }

            // Test ordering
            {
                bool localPassed = true;
                for (int trial = 0; trial < 2; trial++)
                {
                    int prev = -1;
                    var ab = new ActionBlock<int>(i =>
                    {
                        if (prev + 1 != i) localPassed &= false;
                        prev = i;
                    }, new ExecutionDataflowBlockOptions { SingleProducerConstrained = (trial == 0) });
                    for (int i = 0; i < 2; i++) ab.Post(i);
                    ab.Complete();
                    ab.Completion.Wait();
                }

                Assert.True(localPassed, string.Format("{0}: Correct ordering", localPassed ? "Success" : "Failure"));
            }

            // Test non-greedy
            {
                bool localPassed = true;
                var barrier = new Barrier(2);
                var ab = new ActionBlock<int>(i =>
                {
                    barrier.SignalAndWait();
                }, new ExecutionDataflowBlockOptions { BoundedCapacity = 1 });
                ab.SendAsync(1);
                Task.Delay(200).Wait();
                var sa2 = ab.SendAsync(2);
                localPassed &= !sa2.IsCompleted;
                barrier.SignalAndWait(); // for SendAsync(1)
                barrier.SignalAndWait(); // for SendAsync(2)
                localPassed &= sa2.Wait(100);
                int total = 0;
                ab = new ActionBlock<int>(i =>
                {
                    Interlocked.Add(ref total, i);
                    Task.Delay(1).Wait();
                }, new ExecutionDataflowBlockOptions { BoundedCapacity = 1 });
                for (int i = 1; i <= 100; i++) ab.SendAsync(i);
                SpinWait.SpinUntil(() => total == ((100 * 101) / 2), 30000);
                localPassed &= total == ((100 * 101) / 2);
                Assert.True(localPassed, string.Format("total={0} (must be {1})", total, (100 * 101) / 2));
                Assert.True(localPassed, string.Format("{0}: Non-greedy support", localPassed ? "Success" : "Failure"));
            }

            // Test that OperationCanceledExceptions are ignored
            {
                bool localPassed = true;
                for (int trial = 0; trial < 2; trial++)
                {
                    int sumOfOdds = 0;
                    var ab = new ActionBlock<int>(i =>
                    {
                        if ((i % 2) == 0) throw new OperationCanceledException();
                        sumOfOdds += i;
                    }, new ExecutionDataflowBlockOptions { SingleProducerConstrained = (trial == 0) });
                    for (int i = 0; i < 4; i++) ab.Post(i);
                    ab.Complete();
                    ab.Completion.Wait();
                    localPassed = sumOfOdds == (1 + 3);
                }

                Assert.True(localPassed, string.Format("{0}: OperationCanceledExceptions are ignored", localPassed ? "Success" : "Failure"));
            }

            // Test using a precanceled token
            {
                bool localPassed = true;
                try
                {
                    var cts = new CancellationTokenSource();
                    cts.Cancel();
                    var dbo = new ExecutionDataflowBlockOptions { CancellationToken = cts.Token };
                    var ab = new ActionBlock<int>(i => { }, dbo);

                    localPassed &= ab.Post(42) == false;
                    localPassed &= ab.InputCount == 0;
                    localPassed &= ab.Completion != null;
                    ab.Complete();
                }
                catch (Exception)
                {
                    localPassed = false;
                }

                Assert.True(localPassed, string.Format("{0}: Precanceled tokens work correctly", localPassed ? "Success" : "Failure"));
            }

            // Test faulting
            {
                bool localPassed = true;
                for (int trial = 0; trial < 2; trial++)
                {
                    var ab = new ActionBlock<int>(i => { throw new InvalidOperationException(); },
                        new ExecutionDataflowBlockOptions { SingleProducerConstrained = (trial == 0) });
                    ab.Post(42);
                    ab.Post(1);
                    ab.Post(2);
                    ab.Post(3);
                    try { localPassed &= ab.Completion.Wait(5000); }
                    catch { }
                    localPassed &= ab.Completion.IsFaulted;
                    localPassed &= SpinWait.SpinUntil(() => ab.InputCount == 0, 500);
                    localPassed &= ab.Post(4) == false;
                }

                Assert.True(localPassed, string.Format("{0}: Faulted handled correctly", localPassed ? "Success" : "Failure"));
            }

            // ASYNC (a copy of the sync but with constructors returning Task instead of void

            // Do everything twice - once through OfferMessage and Once through Post
            for (FeedMethod feedMethod = FeedMethod._First; feedMethod < FeedMethod._Count; feedMethod++)
            {
                Func<DataflowBlockOptions, TargetProperties<int>> actionBlockFactory =
                    options =>
                    {
                        ITargetBlock<int> target = new ActionBlock<int>(i => TrackCapturesAsync(i), (ExecutionDataflowBlockOptions)options);
                        return new TargetProperties<int> { Target = target, Capturer = target, ErrorVerifyable = true };
                    };
                CancellationTokenSource cancellationSource = new CancellationTokenSource();
                var defaultOptions = new ExecutionDataflowBlockOptions();
                var dopOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
                var mptOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, MaxMessagesPerTask = 10 };
                var cancellationOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, MaxMessagesPerTask = 100, CancellationToken = cancellationSource.Token };

                Assert.True(FeedTarget(actionBlockFactory, defaultOptions, 1, Intervention.None, null, feedMethod, true));
                Assert.True(FeedTarget(actionBlockFactory, defaultOptions, 10, Intervention.None, null, feedMethod, true));
                Assert.True(FeedTarget(actionBlockFactory, dopOptions, 1000, Intervention.None, null, feedMethod, true));
                Assert.True(FeedTarget(actionBlockFactory, mptOptions, 10000, Intervention.None, null, feedMethod, true));
                Assert.True(FeedTarget(actionBlockFactory, mptOptions, 10000, Intervention.Complete, null, feedMethod, true));
                Assert.True(FeedTarget(actionBlockFactory, cancellationOptions, 10000, Intervention.Cancel, cancellationSource, feedMethod, true));
            }

            // Test scheduler usage
            {
                bool localPassed = true;
                var sts = new SimpleTaskScheduler();
                var ab = new ActionBlock<int>(i =>
                    {
                        localPassed &= TaskScheduler.Current.Id == sts.Id;
                        return Task.Factory.StartNew(() => { });
                    }, new ExecutionDataflowBlockOptions { TaskScheduler = sts, MaxDegreeOfParallelism = -1, MaxMessagesPerTask = 10 });
                for (int i = 0; i < 2; i++) ab.Post(i);
                ab.Complete();
                ab.Completion.Wait();
                Assert.True(localPassed, string.Format("{0}: Correct scheduler usage", localPassed ? "Success" : "Failure"));
            }

            // Test count
            {
                bool localPassed = true;
                var barrier1 = new Barrier(2);
                var barrier2 = new Barrier(2);
                var ab = new ActionBlock<int>(i => Task.Factory.StartNew(() =>
                {
                    barrier1.SignalAndWait();
                    barrier2.SignalAndWait();
                }));
                for (int iter = 0; iter < 2; iter++)
                {
                    for (int i = 1; i <= 2; i++) ab.Post(i);
                    for (int i = 1; i >= 0; i--)
                    {
                        barrier1.SignalAndWait();
                        localPassed &= i == ab.InputCount;
                        barrier2.SignalAndWait();
                    }
                }
                Assert.True(localPassed, string.Format("{0}: InputCount", localPassed ? "Success" : "Failure"));
            }

            // Test ordering
            {
                bool localPassed = true;
                int prev = -1;
                var ab = new ActionBlock<int>(i =>
                {
                    return Task.Factory.StartNew(() =>
                    {
                        if (prev + 1 != i) localPassed &= false;
                        prev = i;
                    });
                });
                for (int i = 0; i < 2; i++) ab.Post(i);
                ab.Complete();
                ab.Completion.Wait();
                Assert.True(localPassed, string.Format("{0}: Correct ordering", localPassed ? "Success" : "Failure"));
            }

            // Test non-greedy
            {
                bool localPassed = true;
                var barrier = new Barrier(2);
                var ab = new ActionBlock<int>(i =>
                {
                    return Task.Factory.StartNew(() =>
                    {
                        barrier.SignalAndWait();
                    });
                }, new ExecutionDataflowBlockOptions { BoundedCapacity = 1 });
                ab.SendAsync(1);
                Task.Delay(200).Wait();
                var sa2 = ab.SendAsync(2);
                localPassed &= !sa2.IsCompleted;
                barrier.SignalAndWait(); // for SendAsync(1)
                barrier.SignalAndWait(); // for SendAsync(2)
                localPassed &= sa2.Wait(100);
                int total = 0;
                ab = new ActionBlock<int>(i =>
                {
                    return Task.Factory.StartNew(() =>
                    {
                        Interlocked.Add(ref total, i);
                        Task.Delay(1).Wait();
                    });
                }, new ExecutionDataflowBlockOptions { BoundedCapacity = 1 });
                for (int i = 1; i <= 100; i++) ab.SendAsync(i);
                SpinWait.SpinUntil(() => total == ((100 * 101) / 2), 30000);
                localPassed &= total == ((100 * 101) / 2);
                Assert.True(localPassed, string.Format("total={0} (must be {1})", total, (100 * 101) / 2));
                Assert.True(localPassed, string.Format("{0}: Non-greedy support", localPassed ? "Success" : "Failure"));
            }

            // Test that OperationCanceledExceptions are ignored
            {
                bool localPassed = true;
                int sumOfOdds = 0;
                var ab = new ActionBlock<int>(i =>
                {
                    if ((i % 2) == 0) throw new OperationCanceledException();
                    return Task.Factory.StartNew(() => { sumOfOdds += i; });
                });
                for (int i = 0; i < 4; i++) ab.Post(i);
                ab.Complete();
                ab.Completion.Wait();
                localPassed = sumOfOdds == (1 + 3);
                Assert.True(localPassed, string.Format("{0}: OperationCanceledExceptions are ignored", localPassed ? "Success" : "Failure"));
            }

            // Test that null task is ignored
            {
                bool localPassed = true;
                int sumOfOdds = 0;
                var ab = new ActionBlock<int>(i =>
                {
                    if ((i % 2) == 0) return null;
                    return Task.Factory.StartNew(() => { sumOfOdds += i; });
                });
                for (int i = 0; i < 4; i++) ab.Post(i);
                ab.Complete();
                ab.Completion.Wait();
                localPassed = sumOfOdds == (1 + 3);
                Assert.True(localPassed, string.Format("{0}: null tasks are ignored", localPassed ? "Success" : "Failure"));
            }

            // Test faulting from the delegate
            {
                bool localPassed = true;
                var ab = new ActionBlock<int>(new Func<int, Task>(i => { throw new InvalidOperationException(); }));
                ab.Post(42);
                ab.Post(1);
                ab.Post(2);
                ab.Post(3);
                try { localPassed &= ab.Completion.Wait(100); }
                catch { }
                localPassed &= ab.Completion.IsFaulted;
                localPassed &= SpinWait.SpinUntil(() => ab.InputCount == 0, 500);
                localPassed &= ab.Post(4) == false;
                Assert.True(localPassed, string.Format("{0}: Faulted from delegate handled correctly", localPassed ? "Success" : "Failure"));
            }

            // Test faulting from the task
            {
                bool localPassed = true;
                var ab = new ActionBlock<int>(i => Task.Factory.StartNew(() => { throw new InvalidOperationException(); }));
                ab.Post(42);
                ab.Post(1);
                ab.Post(2);
                ab.Post(3);
                try { localPassed &= ab.Completion.Wait(100); }
                catch { }
                localPassed &= ab.Completion.IsFaulted;
                localPassed &= SpinWait.SpinUntil(() => ab.InputCount == 0, 500);
                localPassed &= ab.Post(4) == false;
                Assert.True(localPassed, string.Format("{0}: Faulted from task handled correctly", localPassed ? "Success" : "Failure"));
            }
        }

        //[Fact(Skip = "Outerloop")]
        public void TestDynamicParallelism()
        {
            bool passed = false, executingFirst = false;
            const int firstItem = 1;
            const int secondItem = 2;

            int maxDOP = Parallelism.ActualDegreeOfParallelism > 1 ? Parallelism.ActualDegreeOfParallelism : 2; // Must be >= 2
            int maxMPT = Int32.MaxValue;
            var options = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = maxDOP, MaxMessagesPerTask = maxMPT };

            ActionBlock<int> action = new ActionBlock<int>((item) =>
            {
                if (item == firstItem)
                {
                    executingFirst = true;
                    Task.Delay(100).Wait();
                    executingFirst = false;
                }

                if (item == secondItem)
                {
                    passed = executingFirst;
                }
            }, options);

            BufferBlock<int> buffer = new BufferBlock<int>();
            buffer.LinkTo(action);

            buffer.Post(firstItem);
            Task.Delay(1).Wait(); // Make sure item 2 propagates after item 1 has started executing
            buffer.Post(secondItem);

            Task.Delay(1).Wait(); // Let item 2 get propagated to the ActionBlock
            action.Complete();
            action.Completion.Wait();

            Assert.True(passed, "Test failed: executingFirst is false.");
        }

        //[Fact(Skip = "Outerloop")]
        public void TestReleasingOfPostponedMessages()
        {
            const int excess = 5;
            for (int dop = 1; dop <= Parallelism.ActualDegreeOfParallelism; dop++)
            {
                var localPassed = true;
                var nextOfferEvent = new AutoResetEvent(true);
                var releaseProcessingEvent = new ManualResetEventSlim();
                var options = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = dop, BoundedCapacity = dop };
                var action = new ActionBlock<int>(x => { nextOfferEvent.Set(); releaseProcessingEvent.Wait(); }, options);
                var sendAsyncDop = new Task<bool>[dop];
                var sendAsyncExcess = new Task<bool>[excess];

                // Send DOP messages
                for (int i = 0; i < dop; i++)
                {
                    // Throttle sending to make sure we saturate DOP exactly
                    nextOfferEvent.WaitOne();
                    sendAsyncDop[i] = action.SendAsync(i);
                }

                // Send EXCESS more messages. All of these will surely be postponed
                for (int i = 0; i < excess; i++)
                    sendAsyncExcess[i] = action.SendAsync(dop + i);

                // Wait until the tasks for the first DOP messages get completed
                Task.WaitAll(sendAsyncDop, 5000);

                // Complete the block. This will cause the EXCESS messages to be declined.
                action.Complete();
                releaseProcessingEvent.Set();

                // Verify all DOP messages have been accepted
                for (int i = 0; i < dop; i++) localPassed &= sendAsyncDop[i].Result;
                Assert.True(localPassed, string.Format("DOP={0} : Consumed up to DOP - {1}", dop, localPassed ? "Passed" : "FAILED"));


                // Verify all EXCESS messages have been declined
                localPassed = true;
                for (int i = 0; i < excess; i++) localPassed &= !sendAsyncExcess[i].Result;
                Assert.True(localPassed, string.Format("DOP={0} : Declined excess - {1}", dop, localPassed ? "Passed" : "FAILED"));
            }
        }
    }
}
