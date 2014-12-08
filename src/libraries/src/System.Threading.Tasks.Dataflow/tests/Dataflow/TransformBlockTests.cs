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
        public void RunTransformBlockTests()
        {
            Assert.True(IDataflowBlockTestHelper.TestToString(nameFormat => nameFormat != null ? new TransformBlock<int, int>(x => x, new ExecutionDataflowBlockOptions() { NameFormat = nameFormat }) : new TransformBlock<int, int>(x => x)));
            Assert.True(ISourceBlockTestHelper.TestLinkTo<string>(ConstructTransformWithNMessages(2), 1));
            Assert.True(ISourceBlockTestHelper.TestReserveMessageAndReleaseReservation<string>(ConstructTransformWithNMessages(1)));
            Assert.True(ISourceBlockTestHelper.TestConsumeMessage<string>(ConstructTransformWithNMessages(1)));
            Assert.True(ISourceBlockTestHelper.TestTryReceiveWithFilter<string>(ConstructTransformWithNMessages(1), 1));
            Assert.True(ISourceBlockTestHelper.TestTryReceiveAll<string>(ConstructTransformWithNMessages(1), 1));
            Assert.True(ISourceBlockTestHelper.TestTargetOrder<int>(new TransformBlock<int, int>(x => x), 2));
            Assert.True(ITargetBlockTestHelper.TestOfferMessage<int>(new TransformBlock<int, string>(i => i.ToString())));
            Assert.True(ITargetBlockTestHelper.TestPost<int>(new TransformBlock<int, string>(i => i.ToString())));
            Assert.True(ITargetBlockTestHelper.TestComplete<int>(new TransformBlock<int, string>(i => i.ToString())));
            Assert.True(ITargetBlockTestHelper.TestCompletionTask<int>(new TransformBlock<int, string>(i => i.ToString())));
            Assert.True(ITargetBlockTestHelper.TestNonGreedyPost(new TransformBlock<int, int>(x => { Task.Delay(5).Wait(); return x; }, new ExecutionDataflowBlockOptions { BoundedCapacity = 1 })));

            Assert.True(TestQuickStop(testThrow: false));
            Assert.True(TestQuickStop(testThrow: true));
        }

        private static TransformBlock<int, string> ConstructTransformWithNMessages(int messagesCount)
        {
            var block = new TransformBlock<int, string>(i => i.ToString());
            for (int i = 0; i < messagesCount; i++)
            {
                block.Post(i);
            }

            // Spin until the messages have been properly buffered up. 
            // Otherwise TryReceive fails.
            SpinWait.SpinUntil(() => block.OutputCount == messagesCount);

            return block;
        }

        [Fact]
        public void TestTransformBlockConstructor()
        {
            // SYNC
            {
                // without option
                var block = new TransformBlock<int, string>(i => i.ToString());
                Assert.False(block.InputCount != 0, "Constructor failed! InputCount returned a non zero value for a brand new TransformBlock.");
                //with not cancelled token and default scheduler
                block = new TransformBlock<int, string>(i => i.ToString(), new ExecutionDataflowBlockOptions { MaxMessagesPerTask = 1 });
                Assert.False(block.InputCount != 0, "Constructor failed! InputCount returned a non zero value for a brand new TransformBlock.");
                //with a cancelled token and default scheduler
                var token = new CancellationToken(true);
                block = new TransformBlock<int, string>(i => i.ToString(), new ExecutionDataflowBlockOptions { MaxMessagesPerTask = 1, CancellationToken = token });
                Assert.False(block.InputCount != 0, "Constructor failed! InputCount returned a non zero value for a brand new TransformBlock.");
            }

            // ASYNC (a copy of the sync but with constructors returning Task<T> instead of T
            {
                // without option
                var block = new TransformBlock<int, string>(i => Task.Factory.StartNew(() => i.ToString()));
                Assert.False(block.InputCount != 0, "Constructor failed! InputCount returned a non zero value for a brand new TransformBlock.");
                //with not cancelled token and default scheduler
                block = new TransformBlock<int, string>(i => Task.Factory.StartNew(() => i.ToString()), new ExecutionDataflowBlockOptions { MaxMessagesPerTask = 1 });
                Assert.False(block.InputCount != 0, "Constructor failed! InputCount returned a non zero value for a brand new TransformBlock.");
                //with a cancelled token and default scheduler
                var token = new CancellationToken(true);
                block = new TransformBlock<int, string>(i => Task.Factory.StartNew(() => i.ToString()), new ExecutionDataflowBlockOptions { MaxMessagesPerTask = 1, CancellationToken = token });
                Assert.False(block.InputCount != 0, "Constructor failed! InputCount returned a non zero value for a brand new TransformBlock.");
            }
        }

        [Fact]
        public void TestTransformBlockInvalidArgumentValidation()
        {
            Assert.Throws<ArgumentNullException>(() => new TransformBlock<int, string>((Func<int, string>)null));
            Assert.Throws<ArgumentNullException>(() => new TransformBlock<int, string>((Func<int, Task<string>>)null));
            Assert.Throws<ArgumentNullException>(() => new TransformBlock<int, string>(i => i.ToString(), null));
            Assert.Throws<ArgumentNullException>(() => new TransformBlock<int, string>(i => Task.Factory.StartNew(() => i.ToString()), null));
            Assert.True(ITargetBlockTestHelper.TestArgumentsExceptions<int>(new TransformBlock<int, string>(i => i.ToString())));
            Assert.True(ISourceBlockTestHelper.TestArgumentsExceptions<string>(new TransformBlock<int, string>(i => i.ToString())));
        }

        private static bool TestQuickStop(bool testThrow)
        {
            bool passed = true;

            CancellationTokenSource cts = new CancellationTokenSource();
            var options = new ExecutionDataflowBlockOptions { CancellationToken = cts.Token };

            var propagator = new TransformBlock<ThrowOn, ThrowOn>(x => { Task.Delay(200).Wait(); return x; }, options);
            var thrower = new ThrowerBlock();

            // Post enough messages to require long processing
            for (int i = 0; i < 2; i++)
                propagator.Post(testThrow && i == 1 ? ThrowOn.OfferMessage : ThrowOn.TryReceive); // Throw on the second message

            // Link the thrower
            propagator.LinkTo(thrower);

            // Once a message has been processed, cancel the propagator (if we are testing cancellation)
            SpinWait.SpinUntil(() => thrower.LastOperation == ThrowOn.OfferMessage);
            if (!testThrow) cts.Cancel();

            // Wait for the propagator to complete
            try
            {
                var ranToCompletion = propagator.Completion.Wait(10000);
                passed = false;
                Console.WriteLine("Task is faulted or canceled (finished: {0}) - FAILED", ranToCompletion ? "ran to copmpletion" : "still running");
            }
            catch (AggregateException ae)
            {
                passed = testThrow ? ae.InnerException is InvalidOperationException : ae.InnerException is TaskCanceledException;
                ae.Handle(e => true);
                Console.WriteLine("Task is faulted or canceled (exception) - {0}", passed ? "Passed" : "FAILED");
            }

            return passed;
        }

        //
        // The test that verifies that internally, TransformBlock does not get confused
        // between Func<T, Task<object>> and Func<T, object>.
        //[Fact(Skip = "Outerloop")]
        public void RunTransformCtorOverloadTest()
        {
            bool passed = true;
            {
                Func<object, Task<object>> f = x => Task<object>.FromResult(x);
                TransformBlock<object, object> tf = new TransformBlock<object, object>(f);
                var mre = new ManualResetEvent(false);

                ActionBlock<object> a = new ActionBlock<object>(x =>
                {
                    bool localPassed = !(x is Task<object>);
                    passed &= localPassed;

                    mre.Set();
                });

                tf.LinkTo(a);
                tf.Post(new object());

                mre.WaitOne();
            }
            {
                Func<object, Task<object>> f = x => Task<object>.FromResult(x);
                TransformBlock<object, object> tf = new TransformBlock<object, object>((Func<object, object>)f);
                var mre = new ManualResetEvent(false);

                ActionBlock<object> a = new ActionBlock<object>(x =>
                {
                    bool localPassed = x is Task<object>;
                    passed &= localPassed;

                    mre.Set();
                });

                tf.LinkTo(a);
                tf.Post(new object());

                mre.WaitOne();
            }

            Assert.True(passed, "Test failed.");
        }

        //[Fact(Skip = "Outerloop")]
        public void RunTransformBlockConformanceTests()
        {
            bool passed = true;

            // SYNC
            #region Sync
            {
                // Do everything twice - once through OfferMessage and Once through Post
                for (FeedMethod feedMethod = FeedMethod._First; passed & feedMethod < FeedMethod._Count; feedMethod++)
                {
                    Func<DataflowBlockOptions, TargetProperties<int>> transformBlockFactory =
                        options =>
                        {
                            TransformBlock<int, int> transformBlock = new TransformBlock<int, int>(i => i, (ExecutionDataflowBlockOptions)options);
                            ActionBlock<int> actionBlock = new ActionBlock<int>(i => TrackCaptures(i), (ExecutionDataflowBlockOptions)options);

                            transformBlock.LinkTo(actionBlock);

                            return new TargetProperties<int> { Target = transformBlock, Capturer = actionBlock, ErrorVerifyable = false };
                        };
                    CancellationTokenSource cancellationSource = new CancellationTokenSource();
                    var defaultOptions = new ExecutionDataflowBlockOptions();
                    var dopOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
                    var mptOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, MaxMessagesPerTask = 2 };
                    var cancellationOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, MaxMessagesPerTask = 2, CancellationToken = cancellationSource.Token };

                    passed &= FeedTarget(transformBlockFactory, defaultOptions, 1, Intervention.None, null, feedMethod, true);
                    passed &= FeedTarget(transformBlockFactory, defaultOptions, 1, Intervention.None, null, feedMethod, true);
                    passed &= FeedTarget(transformBlockFactory, dopOptions, 1, Intervention.None, null, feedMethod, true);
                    passed &= FeedTarget(transformBlockFactory, mptOptions, 1, Intervention.None, null, feedMethod, true);
                    passed &= FeedTarget(transformBlockFactory, mptOptions, 1, Intervention.Complete, null, feedMethod, true);
                    passed &= FeedTarget(transformBlockFactory, cancellationOptions, 1, Intervention.Cancel, cancellationSource, feedMethod, true);
                }

                // Test chained Post/Receive
                {
                    bool localPassed = true;
                    const int ITERS = 2;
                    var network = Chain<TransformBlock<int, int>, int>(4, () => new TransformBlock<int, int>(i => i * 2));
                    for (int i = 0; i < ITERS; i++)
                    {
                        network.Post(i);
                        localPassed &= (((IReceivableSourceBlock<int>)network).Receive() == i * 16);
                    }
                    Console.WriteLine("{0}: Chained Post/Receive", localPassed ? "Success" : "Failure");
                    passed &= localPassed;
                }

                // Test chained SendAsync/Receive
                {
                    bool localPassed = true;
                    const int ITERS = 2;
                    var network = Chain<TransformBlock<int, int>, int>(4, () => new TransformBlock<int, int>(i => i * 2));
                    for (int i = 0; i < ITERS; i++)
                    {
                        network.SendAsync(i);
                        localPassed &= (((IReceivableSourceBlock<int>)network).Receive() == i * 16);
                    }
                    Console.WriteLine("{0}: Chained SendAsync/Receive", localPassed ? "Success" : "Failure");
                    passed &= localPassed;
                }

                // Test chained Post all then Receive
                {
                    bool localPassed = true;
                    const int ITERS = 2;
                    var network = Chain<TransformBlock<int, int>, int>(4, () => new TransformBlock<int, int>(i => i * 2));
                    for (int i = 0; i < ITERS; i++) localPassed &= network.Post(i) == true;
                    for (int i = 0; i < ITERS; i++) localPassed &= ((IReceivableSourceBlock<int>)network).Receive() == i * 16;
                    Console.WriteLine("{0}: Chained Post all then Receive", localPassed ? "Success" : "Failure");
                    passed &= localPassed;
                }

                // Test chained SendAsync all then Receive
                {
                    bool localPassed = true;
                    const int ITERS = 2;
                    var network = Chain<TransformBlock<int, int>, int>(4, () => new TransformBlock<int, int>(i => i * 2));
                    var tasks = new Task[ITERS];
                    for (int i = 1; i <= ITERS; i++) tasks[i - 1] = network.SendAsync(i);
                    Task.WaitAll(tasks);
                    int total = 0;
                    for (int i = 1; i <= ITERS; i++) total += ((IReceivableSourceBlock<int>)network).Receive();
                    localPassed &= (total == ((ITERS * (ITERS + 1)) / 2 * 16));
                    Console.WriteLine("{0}: Chained SendAsync all then Receive", localPassed ? "Success" : "Failure");
                    passed &= localPassed;
                }

                // Test that OperationCanceledExceptions are ignored
                {
                    bool localPassed = true;

                    var t = new TransformBlock<int, int>(i =>
                    {
                        if ((i % 2) == 0) throw new OperationCanceledException();
                        return i;
                    });
                    for (int i = 0; i < 2; i++) t.Post(i);
                    t.Complete();
                    for (int i = 0; i < 2; i++)
                    {
                        if ((i % 2) != 0) localPassed &= t.Receive() == i;
                    }
                    t.Completion.Wait();
                    Console.WriteLine("{0}: OperationCanceledExceptions are ignored", localPassed ? "Success" : "Failure");
                    passed &= localPassed;
                }

                // Test using a precanceled token
                {
                    bool localPassed = true;
                    try
                    {
                        var cts = new CancellationTokenSource();
                        cts.Cancel();
                        var dbo = new ExecutionDataflowBlockOptions { CancellationToken = cts.Token };
                        var t = new TransformBlock<int, int>(i => i, dbo);

                        int ignoredValue;
                        IList<int> ignoredValues;
                        localPassed &= t.LinkTo(new ActionBlock<int>(delegate { })) != null;
                        localPassed &= t.SendAsync(42).Result == false;
                        localPassed &= t.TryReceiveAll(out ignoredValues) == false;
                        localPassed &= t.Post(42) == false;
                        localPassed &= t.OutputCount == 0;
                        localPassed &= t.TryReceive(out ignoredValue) == false;
                        localPassed &= t.Completion != null;
                        t.Complete();
                    }
                    catch (Exception)
                    {
                        localPassed = false;
                    }
                    Console.WriteLine("    {0}: Precanceled tokens work correctly", localPassed ? "Success" : "Failure");
                    passed &= localPassed;
                }

                // Test faulting
                {
                    bool localPassed = true;
                    var t = new TransformBlock<int, int>(new Func<int, int>(i => { throw new InvalidOperationException(); }));
                    t.Post(42);
                    t.Post(1);
                    t.Post(2);
                    t.Post(3);
                    try { t.Completion.Wait(); }
                    catch { }
                    localPassed &= t.Completion.IsFaulted;
                    localPassed &= SpinWait.SpinUntil(() => t.InputCount == 0, 500);
                    localPassed &= SpinWait.SpinUntil(() => t.OutputCount == 0, 500);
                    localPassed &= t.Post(4) == false;
                    Console.WriteLine("    {0}: Faulted handled correctly", localPassed ? "Success" : "Failure");
                    passed &= localPassed;
                }
            }
            #endregion

            #region Async
            // ASYNC (a copy of the sync but with constructors returning Task<T> instead of T
            {
                // Do everything twice - once through OfferMessage and Once through Post
                for (FeedMethod feedMethod = FeedMethod._First; passed & feedMethod < FeedMethod._Count; feedMethod++)
                {
                    Func<DataflowBlockOptions, TargetProperties<int>> transformBlockFactory =
                        options =>
                        {
                            TransformBlock<int, int> transformBlock = new TransformBlock<int, int>(i => Task.Factory.StartNew(() => i), (ExecutionDataflowBlockOptions)options);
                            ActionBlock<int> actionBlock = new ActionBlock<int>(i => TrackCaptures(i), (ExecutionDataflowBlockOptions)options);

                            transformBlock.LinkTo(actionBlock);

                            return new TargetProperties<int> { Target = transformBlock, Capturer = actionBlock, ErrorVerifyable = false };
                        };
                    CancellationTokenSource cancellationSource = new CancellationTokenSource();
                    var defaultOptions = new ExecutionDataflowBlockOptions();
                    var dopOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
                    var mptOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, MaxMessagesPerTask = 2 };
                    var cancellationOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, MaxMessagesPerTask = 2, CancellationToken = cancellationSource.Token };

                    passed &= FeedTarget(transformBlockFactory, defaultOptions, 1, Intervention.None, null, feedMethod, true);
                    passed &= FeedTarget(transformBlockFactory, dopOptions, 10, Intervention.None, null, feedMethod, true);
                    passed &= FeedTarget(transformBlockFactory, mptOptions, 10000, Intervention.None, null, feedMethod, true);
                    passed &= FeedTarget(transformBlockFactory, mptOptions, 10000, Intervention.Complete, null, feedMethod, true);
                    passed &= FeedTarget(transformBlockFactory, cancellationOptions, 10000, Intervention.Cancel, cancellationSource, feedMethod, true);
                }

                // Test chained Post/Receive
                {
                    bool localPassed = true;
                    const int ITERS = 2;
                    var network = Chain<TransformBlock<int, int>, int>(4, () => new TransformBlock<int, int>(i => Task.Factory.StartNew(() => i * 2)));
                    for (int i = 0; i < ITERS; i++)
                    {
                        network.Post(i);
                        localPassed &= (((IReceivableSourceBlock<int>)network).Receive() == i * 16);
                    }
                    Console.WriteLine("{0}: Chained Post/Receive", localPassed ? "Success" : "Failure");
                    passed &= localPassed;
                }

                // Test chained SendAsync/Receive
                {
                    bool localPassed = true;
                    const int ITERS = 2;
                    var network = Chain<TransformBlock<int, int>, int>(4, () => new TransformBlock<int, int>(i => Task.Factory.StartNew(() => i * 2)));
                    for (int i = 0; i < ITERS; i++)
                    {
                        network.SendAsync(i);
                        localPassed &= (((IReceivableSourceBlock<int>)network).Receive() == i * 16);
                    }
                    Console.WriteLine("{0}: Chained SendAsync/Receive", localPassed ? "Success" : "Failure");
                    passed &= localPassed;
                }

                // Test chained Post all then Receive
                {
                    bool localPassed = true;
                    const int ITERS = 2;
                    var network = Chain<TransformBlock<int, int>, int>(4, () => new TransformBlock<int, int>(i => Task.Factory.StartNew(() => i * 2)));
                    for (int i = 0; i < ITERS; i++) localPassed &= network.Post(i) == true;
                    for (int i = 0; i < ITERS; i++) localPassed &= ((IReceivableSourceBlock<int>)network).Receive() == i * 16;
                    Console.WriteLine("{0}: Chained Post all then Receive", localPassed ? "Success" : "Failure");
                    passed &= localPassed;
                }

                // Test chained SendAsync all then Receive
                {
                    bool localPassed = true;
                    const int ITERS = 2;
                    var network = Chain<TransformBlock<int, int>, int>(4, () => new TransformBlock<int, int>(i => Task.Factory.StartNew(() => i * 2)));
                    var tasks = new Task[ITERS];
                    for (int i = 1; i <= ITERS; i++) tasks[i - 1] = network.SendAsync(i);
                    Task.WaitAll(tasks);
                    int total = 0;
                    for (int i = 1; i <= ITERS; i++) total += ((IReceivableSourceBlock<int>)network).Receive();
                    localPassed &= (total == ((ITERS * (ITERS + 1)) / 2 * 16));
                    Console.WriteLine("{0}: Chained SendAsync all then Receive", localPassed ? "Success" : "Failure");
                    passed &= localPassed;
                }

                // Test that OperationCanceledExceptions are ignored
                {
                    bool localPassed = true;

                    var t = new TransformBlock<int, int>(i =>
                    {
                        if ((i % 2) == 0) throw new OperationCanceledException();
                        return Task.Factory.StartNew(() => i);
                    });
                    for (int i = 0; i < 2; i++) t.Post(i);
                    t.Complete();
                    for (int i = 0; i < 2; i++)
                    {
                        if ((i % 2) != 0) localPassed &= t.Receive() == i;
                    }
                    t.Completion.Wait();
                    Console.WriteLine("{0}: OperationCanceledExceptions are ignored", localPassed ? "Success" : "Failure");
                    passed &= localPassed;
                }

                // Test that null tasks are ignored
                {
                    bool localPassed = true;

                    var t = new TransformBlock<int, int>(i =>
                    {
                        if ((i % 2) == 0) return null;
                        return Task.Factory.StartNew(() => i);
                    });
                    for (int i = 0; i < 2; i++) t.Post(i);
                    t.Complete();
                    for (int i = 0; i < 2; i++)
                    {
                        if ((i % 2) != 0) localPassed &= t.Receive() == i;
                    }
                    t.Completion.Wait();
                    Console.WriteLine("{0}: null tasks are ignored", localPassed ? "Success" : "Failure");
                    passed &= localPassed;
                }

                // Test that null tasks are ignored when a reordering buffer is in place
                {
                    bool localPassed = true;

                    var t = new TransformBlock<int, int>(i =>
                    {
                        if (i == 0)
                        {
                            Task.Delay(10).Wait();
                            return null;
                        }
                        return Task.Factory.StartNew(() => i);
                    }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 2 });
                    t.Post(0);
                    t.Post(1);
                    try
                    {
                        localPassed &= t.Receive(TimeSpan.FromSeconds(4)) == 1;
                    }
                    catch
                    {
                        localPassed = false;
                    }
                    Console.WriteLine("{0}: null tasks are ignored with reordering buffer", localPassed ? "Success" : "Failure");
                    passed &= localPassed;
                }

                // Test faulting from the delegate
                {
                    bool localPassed = true;
                    var t = new TransformBlock<int, int>(new Func<int, Task<int>>(i => { throw new InvalidOperationException(); }));
                    t.Post(42);
                    t.Post(1);
                    t.Post(2);
                    t.Post(3);
                    try { t.Completion.Wait(); }
                    catch { }
                    localPassed &= t.Completion.IsFaulted;
                    localPassed &= SpinWait.SpinUntil(() => t.InputCount == 0, 500);
                    localPassed &= SpinWait.SpinUntil(() => t.OutputCount == 0, 500);
                    localPassed &= t.Post(4) == false;
                    Console.WriteLine("    {0}: Faulted from delegate handled correctly", localPassed ? "Success" : "Failure");
                    passed &= localPassed;
                }

                // Test faulting from the task
                {
                    bool localPassed = true;
                    var t = new TransformBlock<int, int>(new Func<int, Task<int>>(i => Task<int>.Factory.StartNew(() => { throw new InvalidOperationException(); })));
                    t.Post(42);
                    t.Post(1);
                    t.Post(2);
                    t.Post(3);
                    try { t.Completion.Wait(); }
                    catch { }
                    localPassed &= t.Completion.IsFaulted;
                    localPassed &= SpinWait.SpinUntil(() => t.InputCount == 0, 500);
                    localPassed &= SpinWait.SpinUntil(() => t.OutputCount == 0, 500);
                    localPassed &= t.Post(4) == false;
                    Console.WriteLine("    {0}: Faulted from task handled correctly", localPassed ? "Success" : "Failure");
                    passed &= localPassed;
                }
            }
            #endregion

            Assert.True(passed, "Test failed.");
        }
    }
}
