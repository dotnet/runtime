// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Collections.ObjectModel;
using Xunit;

namespace System.Threading.Tasks.Dataflow.Tests
{
    public partial class DataflowBlockTests : DataflowBlockTestBase
    {
        [Fact]        
        public void RunTransformManyBlockTests()
        {
            Assert.True(IDataflowBlockTestHelper.TestToString(nameFormat => nameFormat != null ? new TransformManyBlock<int, int>(x => new int[] { x }, new ExecutionDataflowBlockOptions() { NameFormat = nameFormat }) : new TransformManyBlock<int, int>(x => new int[] { x })));
            Assert.True(ISourceBlockTestHelper.TestLinkTo<int>(ConstructTransformManyWithNMessages(2), 1));
            Assert.True(ISourceBlockTestHelper.TestReserveMessageAndReleaseReservation<int>(ConstructTransformManyWithNMessages(1)));
            Assert.True(ISourceBlockTestHelper.TestConsumeMessage<int>(ConstructTransformManyWithNMessages(1)));
            Assert.True(ISourceBlockTestHelper.TestTryReceiveWithFilter<int>(ConstructTransformManyWithNMessages(1), 1));
            Assert.True(ISourceBlockTestHelper.TestTryReceiveAll<int>(ConstructTransformManyWithNMessages(1), 1));
            Assert.True(ITargetBlockTestHelper.TestOfferMessage<int>(new TransformManyBlock<int, int>(i => new int[] { i })));
            Assert.True(ITargetBlockTestHelper.TestPost<int>(new TransformManyBlock<int, int>(i => new int[] { i })));
            Assert.True(ITargetBlockTestHelper.TestComplete<int>(new TransformManyBlock<int, int>(i => new int[] { i })));
            Assert.True(ITargetBlockTestHelper.TestCompletionTask<int>(new TransformManyBlock<int, int>(i => new int[] { i })));
            Assert.True(ITargetBlockTestHelper.TestNonGreedyPost(new TransformManyBlock<int, int>(x => { Task.Delay(500).Wait(); return new int[] { x }; }, new ExecutionDataflowBlockOptions { BoundedCapacity = 1 })));
        }

        private static TransformManyBlock<int, int> ConstructTransformManyWithNMessages(int messagesCount)
        {
            var block = new TransformManyBlock<int, int>(i => new int[] { i });
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
        public void TestTransformManyBlockConstructor()
        {
            // IEnumerable without option
            var block = new TransformManyBlock<int, string>(i => new string[10]);
            Assert.False(block.InputCount != 0, "Constructor failed! InputCount returned a non zero value for a brand new TransformManyBlock.");

            // Task without option
            block = new TransformManyBlock<int, string>(i => Task.Factory.StartNew(() => (IEnumerable<string>)new string[10]));
            Assert.False(block.InputCount != 0, "Constructor failed! InputCount returned a non zero value for a brand new TransformManyBlock.");

            // IEnumerable with not cancelled token and default scheduler
            block = new TransformManyBlock<int, string>(i => new string[10], new ExecutionDataflowBlockOptions { MaxMessagesPerTask = 1 });
            Assert.False(block.InputCount != 0, "Constructor failed! InputCount returned a non zero value for a brand new TransformManyBlock.");

            // Task with not cancelled token and default scheduler
            block = new TransformManyBlock<int, string>(i => Task.Factory.StartNew(() => (IEnumerable<string>)new string[10]), new ExecutionDataflowBlockOptions { MaxMessagesPerTask = 1 });
            Assert.False(block.InputCount != 0, "Constructor failed! InputCount returned a non zero value for a brand new TransformManyBlock.");

            // IEnumerable with a cancelled token and default scheduler
            var token = new CancellationToken(true);
            block = new TransformManyBlock<int, string>(i => new string[10], new ExecutionDataflowBlockOptions { MaxMessagesPerTask = 1, CancellationToken = token });
            Assert.False(block.InputCount != 0, "Constructor failed! InputCount returned a non zero value for a brand new TransformManyBlock.");

            // Task with a cancelled token and default scheduler
            token = new CancellationToken(true);
            block = new TransformManyBlock<int, string>(i => Task.Factory.StartNew(() => (IEnumerable<string>)new string[10]), new ExecutionDataflowBlockOptions { MaxMessagesPerTask = 1, CancellationToken = token });
            Assert.False(block.InputCount != 0, "Constructor failed! InputCount returned a non zero value for a brand new TransformManyBlock.");
        }

        [Fact]
        public void TestTransformManyBlockInvalidArgumentValidation()
        {
            bool passed = true;

            Assert.Throws<ArgumentNullException>(() => new TransformManyBlock<int, string>((Func<int, IEnumerable<string>>)null));
            Assert.Throws<ArgumentNullException>(() => new TransformManyBlock<int, string>((Func<int, Task<IEnumerable<string>>>)null));
            Assert.Throws<ArgumentNullException>(() => new TransformManyBlock<int, string>(i => new[] { i.ToString() }, null));
            Assert.Throws<ArgumentNullException>(() => new TransformManyBlock<int, string>(i => Task.Factory.StartNew(() => (IEnumerable<string>)new[] { i.ToString() }), null));

            passed &= ITargetBlockTestHelper.TestArgumentsExceptions<int>(new TransformManyBlock<int, int>(i => new int[] { i }));
            passed &= ISourceBlockTestHelper.TestArgumentsExceptions<int>(new TransformManyBlock<int, int>(i => new int[] { i }));

            Assert.True(passed, "Test failed.");
        }

        //[Fact(Skip = "Outerloop")]
        public void RunTransformManyBlockConformanceTests()
        {
            bool passed = true;

            #region Sync
            {
                // Do everything twice - once through OfferMessage and Once through Post
                for (FeedMethod feedMethod = FeedMethod._First; passed & feedMethod < FeedMethod._Count; feedMethod++)
                {
                    Func<DataflowBlockOptions, TargetProperties<int>> transformManyBlockFactory =
                        options =>
                        {
                            TransformManyBlock<int, int> transformManyBlock = new TransformManyBlock<int, int>(i => new[] { i }, (ExecutionDataflowBlockOptions)options);
                            ActionBlock<int> actionBlock = new ActionBlock<int>(i => TrackCaptures(i), (ExecutionDataflowBlockOptions)options);

                            transformManyBlock.LinkTo(actionBlock);

                            return new TargetProperties<int> { Target = transformManyBlock, Capturer = actionBlock, ErrorVerifyable = false };
                        };
                    CancellationTokenSource cancellationSource = new CancellationTokenSource();
                    var defaultOptions = new ExecutionDataflowBlockOptions();
                    var dopOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
                    var mptOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, MaxMessagesPerTask = 10 };
                    var cancellationOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, MaxMessagesPerTask = 100, CancellationToken = cancellationSource.Token };

                    passed &= FeedTarget(transformManyBlockFactory, defaultOptions, 1, Intervention.None, null, feedMethod, true);
                    passed &= FeedTarget(transformManyBlockFactory, defaultOptions, 10, Intervention.None, null, feedMethod, true);
                    passed &= FeedTarget(transformManyBlockFactory, dopOptions, 1000, Intervention.None, null, feedMethod, true);
                    passed &= FeedTarget(transformManyBlockFactory, mptOptions, 10000, Intervention.None, null, feedMethod, true);
                    passed &= FeedTarget(transformManyBlockFactory, mptOptions, 10000, Intervention.Complete, null, feedMethod, true);
                    passed &= FeedTarget(transformManyBlockFactory, cancellationOptions, 10000, Intervention.Cancel, cancellationSource, feedMethod, true);
                }

                // Test chained Post/Receive
                {
                    bool localPassed = true;
                    const int ITERS = 2;
                    var network = Chain<TransformManyBlock<int, int>, int>(4, () => new TransformManyBlock<int, int>(i => new[] { i * 2 }));
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
                    var network = Chain<TransformManyBlock<int, int>, int>(4, () => new TransformManyBlock<int, int>(i => new[] { i * 2 }));
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
                    var network = Chain<TransformManyBlock<int, int>, int>(4, () => new TransformManyBlock<int, int>(i => new[] { i * 2 }));
                    for (int i = 0; i < ITERS; i++) localPassed &= network.Post(i) == true;
                    for (int i = 0; i < ITERS; i++) localPassed &= ((IReceivableSourceBlock<int>)network).Receive() == i * 16;
                    Console.WriteLine("{0}: Chained Post all then Receive", localPassed ? "Success" : "Failure");
                    passed &= localPassed;
                }

                // Test chained SendAsync all then Receive
                {
                    bool localPassed = true;
                    const int ITERS = 2;
                    var network = Chain<TransformManyBlock<int, int>, int>(4, () => new TransformManyBlock<int, int>(i => new[] { i * 2 }));
                    var tasks = new Task[ITERS];
                    for (int i = 1; i <= ITERS; i++) tasks[i - 1] = network.SendAsync(i);
                    Task.WaitAll(tasks);
                    int total = 0;
                    for (int i = 1; i <= ITERS; i++) total += ((IReceivableSourceBlock<int>)network).Receive();
                    localPassed &= (total == ((ITERS * (ITERS + 1)) / 2 * 16));
                    Console.WriteLine("{0}: Chained SendAsync all then Receive", localPassed ? "Success" : "Failure");
                    passed &= localPassed;
                }

                // Test multiple yielded results
                {
                    bool localPassed = true;

                    var t = new TransformManyBlock<int, int>(i =>
                    {
                        return Enumerable.Range(0, 10);
                    });
                    t.Post(42);
                    t.Complete();
                    for (int i = 0; i < 10; i++)
                    {
                        localPassed &= t.Receive() == i;
                    }
                    t.Completion.Wait();
                    Console.WriteLine("{0}: Test multiple yielded results", localPassed ? "Success" : "Failure");
                    passed &= localPassed;
                }

                // Test that OperationCanceledExceptions are ignored
                {
                    bool localPassed = true;

                    var t = new TransformManyBlock<int, int>(i =>
                    {
                        if ((i % 2) == 0) throw new OperationCanceledException();
                        return new[] { i };
                    });
                    for (int i = 0; i < 10; i++) t.Post(i);
                    t.Complete();
                    for (int i = 0; i < 10; i++)
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
                        var t = new TransformManyBlock<int, int>(i => new[] { i }, dbo);

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
                    Console.WriteLine("      > {0}: Precanceled tokens work correctly", localPassed ? "Success" : "Failure");
                    passed &= localPassed;
                }

                // Test faulting
                {
                    bool localPassed = true;
                    var t = new TransformManyBlock<int, int>(new Func<int, IEnumerable<int>>(i => { throw new InvalidOperationException(); }));
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
                    Console.WriteLine("      > {0}: Faulted handled correctly", localPassed ? "Success" : "Failure");
                    passed &= localPassed;
                }

                // Test reuse of a list and array
                {
                    bool localPassed = true;
                    foreach (bool bounded in new[] { false, true })
                    {
                        for (int dop = 1; dop < Environment.ProcessorCount; dop++)
                        {
                            var dbo = bounded ?
                                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = dop, BoundedCapacity = 2 } :
                                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = dop };
                            foreach (IList<int> list in new IList<int>[]
                            {
                                new int[1],
                                new List<int>() { 0 },
                                new Collection<int>() { 0 }
                            })
                            {
                                int nextExpectedValue = 1;
                                TransformManyBlock<int, int> tmb1 = null;
                                tmb1 = new TransformManyBlock<int, int>(i =>
                                {
                                    if (i == 1000)
                                    {
                                        tmb1.Complete();
                                        return (IEnumerable<int>)null;
                                    }
                                    else if (dop == 1)
                                    {
                                        list[0] = i + 1;
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
                                    else return new Collection<int>() { i + 1 };
                                }, dbo);
                                TransformBlock<int, int> tmb2 = new TransformBlock<int, int>(i =>
                                {
                                    if (i != nextExpectedValue)
                                    {
                                        localPassed = false;
                                        tmb1.Complete();
                                    }
                                    nextExpectedValue++;
                                    return i;
                                });
                                tmb1.LinkTo(tmb2);
                                tmb2.LinkTo(tmb1);
                                tmb1.SendAsync(0).Wait();
                                tmb1.Completion.Wait();
                            }
                        }
                    }
                    Console.WriteLine("      > {0}: Reuse of a list and array", localPassed ? "Success" : "Failure");
                    passed &= localPassed;
                }

                // Test throwing an OCE
                {
                    bool localPassed = true;
                    foreach (bool bounded in new[] { true, false })
                    {
                        for (int dop = 1; dop < Environment.ProcessorCount; dop++)
                        {
                            var dbo = bounded ?
                                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = dop, BoundedCapacity = 2 } :
                                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = dop };

                            foreach (int mode in new[] { 0, 1, 2 })
                            {
                                const int ITERS = 50;
                                var mres = new ManualResetEventSlim();
                                var tmb = new TransformManyBlock<int, int>(i =>
                                {
                                    if (i < ITERS - 1) throw new OperationCanceledException();
                                    if (mode == 0) return new int[] { i };
                                    else if (mode == 1) return new List<int>() { i };
                                    else return Enumerable.Repeat(i, 1);
                                }, dbo);
                                var ab = new ActionBlock<int>(i =>
                                {
                                    if (i != ITERS - 1) localPassed = false;
                                    mres.Set();
                                });
                                tmb.LinkTo(ab);
                                for (int i = 0; i < ITERS; i++) tmb.SendAsync(i).Wait();
                                mres.Wait();
                            }
                        }
                    }
                    Console.WriteLine("{0}: Canceled invocation", localPassed ? "Success" : "Failure");
                    passed &= localPassed;
                }
            }
            #endregion

            #region Async
            {
                // Do everything twice - once through OfferMessage and Once through Post
                for (FeedMethod feedMethod = FeedMethod._First; passed & feedMethod < FeedMethod._Count; feedMethod++)
                {
                    Func<DataflowBlockOptions, TargetProperties<int>> transformManyBlockFactory =
                        options =>
                        {
                            TransformManyBlock<int, int> transformManyBlock = new TransformManyBlock<int, int>(i => Task.Factory.StartNew(() => (IEnumerable<int>)new[] { i }), (ExecutionDataflowBlockOptions)options);
                            ActionBlock<int> actionBlock = new ActionBlock<int>(i => TrackCaptures(i), (ExecutionDataflowBlockOptions)options);

                            transformManyBlock.LinkTo(actionBlock);

                            return new TargetProperties<int> { Target = transformManyBlock, Capturer = actionBlock, ErrorVerifyable = false };
                        };
                    CancellationTokenSource cancellationSource = new CancellationTokenSource();
                    var defaultOptions = new ExecutionDataflowBlockOptions();
                    var dopOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
                    var mptOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, MaxMessagesPerTask = 10 };
                    var cancellationOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, MaxMessagesPerTask = 100, CancellationToken = cancellationSource.Token };

                    passed &= FeedTarget(transformManyBlockFactory, defaultOptions, 1, Intervention.None, null, feedMethod, true);
                    passed &= FeedTarget(transformManyBlockFactory, defaultOptions, 10, Intervention.None, null, feedMethod, true);
                    passed &= FeedTarget(transformManyBlockFactory, dopOptions, 1000, Intervention.None, null, feedMethod, true);
                    passed &= FeedTarget(transformManyBlockFactory, mptOptions, 10000, Intervention.None, null, feedMethod, true);
                    passed &= FeedTarget(transformManyBlockFactory, mptOptions, 10000, Intervention.Complete, null, feedMethod, true);
                    passed &= FeedTarget(transformManyBlockFactory, cancellationOptions, 10000, Intervention.Cancel, cancellationSource, feedMethod, true);
                }

                // Test chained Post/Receive
                {
                    bool localPassed = true;
                    const int ITERS = 2;
                    var network = Chain<TransformManyBlock<int, int>, int>(4, () => new TransformManyBlock<int, int>(i => Task.Factory.StartNew(() => (IEnumerable<int>)new[] { i * 2 })));
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
                    var network = Chain<TransformManyBlock<int, int>, int>(4, () => new TransformManyBlock<int, int>(i => Task.Factory.StartNew(() => (IEnumerable<int>)new[] { i * 2 })));
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
                    var network = Chain<TransformManyBlock<int, int>, int>(4, () => new TransformManyBlock<int, int>(i => Task.Factory.StartNew(() => (IEnumerable<int>)new[] { i * 2 })));
                    for (int i = 0; i < ITERS; i++) localPassed &= network.Post(i) == true;
                    for (int i = 0; i < ITERS; i++) localPassed &= ((IReceivableSourceBlock<int>)network).Receive() == i * 16;
                    Console.WriteLine("{0}: Chained Post all then Receive", localPassed ? "Success" : "Failure");
                    passed &= localPassed;
                }

                // Test chained SendAsync all then Receive
                {
                    bool localPassed = true;
                    const int ITERS = 2;
                    var network = Chain<TransformManyBlock<int, int>, int>(4, () => new TransformManyBlock<int, int>(i => Task.Factory.StartNew(() => (IEnumerable<int>)new[] { i * 2 })));
                    var tasks = new Task[ITERS];
                    for (int i = 1; i <= ITERS; i++) tasks[i - 1] = network.SendAsync(i);
                    Task.WaitAll(tasks);
                    int total = 0;
                    for (int i = 1; i <= ITERS; i++) total += ((IReceivableSourceBlock<int>)network).Receive();
                    localPassed &= (total == ((ITERS * (ITERS + 1)) / 2 * 16));
                    Console.WriteLine("{0}: Chained SendAsync all then Receive", localPassed ? "Success" : "Failure");
                    passed &= localPassed;
                }

                // Test multiple yielded results
                {
                    bool localPassed = true;

                    var t = new TransformManyBlock<int, int>(i => Task.Factory.StartNew(() => (IEnumerable<int>)Enumerable.Range(0, 10).ToArray()));
                    t.Post(42);
                    t.Complete();
                    for (int i = 0; i < 10; i++)
                    {
                        localPassed &= t.Receive() == i;
                    }
                    t.Completion.Wait();
                    Console.WriteLine("{0}: Test multiple yielded results", localPassed ? "Success" : "Failure");
                    passed &= localPassed;
                }

                // Test that OperationCanceledExceptions are ignored
                {
                    bool localPassed = true;

                    var t = new TransformManyBlock<int, int>(i =>
                    {
                        if ((i % 2) == 0) throw new OperationCanceledException();
                        return new[] { i };
                    });
                    for (int i = 0; i < 10; i++) t.Post(i);
                    t.Complete();
                    for (int i = 0; i < 10; i++)
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

                    var t = new TransformManyBlock<int, int>(i =>
                    {
                        if ((i % 2) == 0) return null;
                        return Task.Factory.StartNew(() => (IEnumerable<int>)new[] { i });
                    });
                    for (int i = 0; i < 10; i++) t.Post(i);
                    t.Complete();
                    for (int i = 0; i < 10; i++)
                    {
                        if ((i % 2) != 0) localPassed &= t.Receive() == i;
                    }
                    t.Completion.Wait();
                    Console.WriteLine("{0}: OperationCanceledExceptions are ignored", localPassed ? "Success" : "Failure");
                    passed &= localPassed;
                }

                // Test that null tasks are ignored when a reordering buffer is in place
                {
                    bool localPassed = true;

                    var t = new TransformManyBlock<int, int>(new Func<int, Task<IEnumerable<int>>>(i =>
                    {
                        if (i == 0)
                        {
                            Task.Delay(1000).Wait();
                            return null;
                        }
                        return Task.Factory.StartNew(() => (IEnumerable<int>)new[] { i });
                    }), new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 2 });
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
                    var t = new TransformManyBlock<int, int>(new Func<int, Task<IEnumerable<int>>>(i => { throw new InvalidOperationException(); }));
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
                    Console.WriteLine("      > {0}: Faulted from delegate handled correctly", localPassed ? "Success" : "Failure");
                    passed &= localPassed;
                }

                // Test faulting from the task
                {
                    bool localPassed = true;
                    var t = new TransformManyBlock<int, int>(new Func<int, Task<IEnumerable<int>>>(i => Task<IEnumerable<int>>.Factory.StartNew(() => { throw new InvalidOperationException(); })));
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
                    Console.WriteLine("      > {0}: Faulted from task handled correctly", localPassed ? "Success" : "Failure");
                    passed &= localPassed;
                }

                // Test reuse of a list and array
                {
                    bool localPassed = true;
                    foreach (bool bounded in new[] { false, true })
                    {
                        for (int dop = 1; dop < Environment.ProcessorCount; dop++)
                        {
                            var dbo = bounded ?
                                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = dop, BoundedCapacity = 2 } :
                                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = dop };
                            foreach (IList<int> list in new IList<int>[]
                            {
                                new int[1],
                                new List<int>() { 0 },
                                new Collection<int>() { 0 }
                            })
                            {
                                int nextExpectedValue = 1;
                                TransformManyBlock<int, int> tmb1 = null;
                                tmb1 = new TransformManyBlock<int, int>(i =>
                                {
                                    return Task.Factory.StartNew(() =>
                                    {
                                        if (i == 1000)
                                        {
                                            tmb1.Complete();
                                            return (IEnumerable<int>)null;
                                        }
                                        else if (dop == 1)
                                        {
                                            list[0] = i + 1;
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
                                        else return new Collection<int>() { i + 1 };
                                    });
                                }, dbo);
                                TransformBlock<int, int> tmb2 = new TransformBlock<int, int>(i =>
                                {
                                    if (i != nextExpectedValue)
                                    {
                                        localPassed = false;
                                        tmb1.Complete();
                                    }
                                    nextExpectedValue++;
                                    return i;
                                });
                                tmb1.LinkTo(tmb2);
                                tmb2.LinkTo(tmb1);
                                tmb1.SendAsync(0).Wait();
                                tmb1.Completion.Wait();
                            }
                        }
                    }
                    Console.WriteLine("      > {0}: Reuse of a list and array", localPassed ? "Success" : "Failure");
                    passed &= localPassed;
                }

                // Test throwing an OCE
                {
                    bool localPassed = true;
                    foreach (bool bounded in new[] { true, false })
                    {
                        for (int dop = 1; dop < Environment.ProcessorCount; dop++)
                        {
                            var dbo = bounded ?
                                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = dop, BoundedCapacity = 2 } :
                                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = dop };

                            foreach (int mode in new[] { 0, 1, 2 })
                            {
                                const int ITERS = 50;
                                var mres = new ManualResetEventSlim();
                                var tmb = new TransformManyBlock<int, int>(i =>
                                {
                                    var cts = new CancellationTokenSource();
                                    return Task.Factory.StartNew(() =>
                                    {
                                        if (i < ITERS - 1)
                                        {
                                            cts.Cancel();
                                            cts.Token.ThrowIfCancellationRequested();
                                        }
                                        if (mode == 0) return new int[] { i };
                                        else if (mode == 1) return new List<int>() { i };
                                        else return Enumerable.Repeat(i, 1);
                                    }, cts.Token);
                                }, dbo);
                                var ab = new ActionBlock<int>(i =>
                                {
                                    if (i != ITERS - 1) localPassed = false;
                                    mres.Set();
                                });
                                tmb.LinkTo(ab);
                                for (int i = 0; i < ITERS; i++) tmb.SendAsync(i).Wait();
                                mres.Wait();
                            }
                        }
                    }
                    Console.WriteLine("      > {0}: Canceled invocation", localPassed ? "Success" : "Failure");
                    passed &= localPassed;
                }
            }
            #endregion

            Assert.True(passed, "Test failed.");
        }
    }
}