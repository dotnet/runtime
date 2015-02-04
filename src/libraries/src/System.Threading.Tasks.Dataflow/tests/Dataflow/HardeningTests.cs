// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

// NOTE: This file of tests needs to be reviewed, scrubbed, and augmented.

namespace System.Threading.Tasks.Dataflow.Tests
{
    public class HardeningTests
    {
        private const int BufferBoundedCapacity = 2;
        private static readonly int s_randomSeed = Environment.TickCount;

        [Fact]
        [OuterLoop]
        [ActiveIssue(614)]
        public void RunHardeningTests1()
        {
            // OfferMessage: All single-target source blocks
            Assert.True(TestHardeningSource((s, m) => ((ITargetBlock<ThrowOn>)s).Post(m), new BufferBlock<ThrowOn>(), HardeningScenario.Sync));
            Assert.True(TestHardeningSource((s, m) => ((ITargetBlock<ThrowOn>)s).Post(m), new BufferBlock<ThrowOn>(), HardeningScenario.Async));

            // ConsumeMessage (greedy): Batch, BatchedJoin, Join, WriteOnce, Buffer, Broadcast, TargetCore sync (~Action), TargetCore async (~Action)
            Assert.True(TestHardeningTargetJoin2(new BatchedJoinBlock<ThrowOn, ThrowOn>(2), ThrowOn.ConsumeMessage, greedy: true));
            Assert.True(TestHardeningTarget(new WriteOnceBlock<ThrowOn>(x => x), ThrowOn.ConsumeMessage, greedy: true));

            var nonGreedyGroupingOptions = new GroupingDataflowBlockOptions { Greedy = false }; // Sequential, non-greedy
            var nonGreedyExecutionOptions = new ExecutionDataflowBlockOptions { BoundedCapacity = 1 }; // Sequential, non-greedy

            // ConsumeMessage (non-greedy): Batch, Join, TargetCore sync (~Action), TargetCore async (~Action), bounded Buffer
            Assert.True(TestHardeningTargetJoin2(new JoinBlock<ThrowOn, ThrowOn>(nonGreedyGroupingOptions), ThrowOn.ConsumeMessage, greedy: false));
            Assert.True(TestHardeningTarget(new BatchBlock<ThrowOn>(2, nonGreedyGroupingOptions), ThrowOn.ConsumeMessage, greedy: false));

            // ReserveMessage: Batch (non-greedy), Join (non-greedy)
            Assert.True(TestHardeningTarget(new BatchBlock<ThrowOn>(2, nonGreedyGroupingOptions), ThrowOn.ReserveMessage, greedy: false));

            // ReleaseReservation: Batch (non-greedy), Join (non-greedy)
            Assert.True(TestHardeningTarget(new BatchBlock<ThrowOn>(2, nonGreedyGroupingOptions), ThrowOn.ReleaseReservation, greedy: false));

            // User callbacks: Action, Action(Async), Transform, Transform(Async), 
            //                 TransformMany(IEnumerable), TransformMany(Async), WriteOnce, Broadcast
            Assert.True(TestHardeningCallback(new ActionBlock<int>(x => { throw new InvalidOperationException("Callback"); }), ""));
        }

        [Fact]
        [OuterLoop]
        [ActiveIssue(614)]
        public void RunHardeningTests2()
        {
            // OfferMessage: All single-target source blocks
            Assert.True(TestHardeningSource((s, m) => ((ITargetBlock<ThrowOn>)s).Post(m), new BroadcastBlock<ThrowOn>(x => x), HardeningScenario.Sync));
            Assert.True(TestHardeningSource((s, m) => ((ITargetBlock<ThrowOn>)s).Post(m), new BroadcastBlock<ThrowOn>(x => x), HardeningScenario.Async));
            Assert.True(TestHardeningSource((s, m) => ((ITargetBlock<ThrowOn>)s).Post(m), new WriteOnceBlock<ThrowOn>(x => x), HardeningScenario.Sync));
            Assert.True(TestHardeningSource((s, m) => ((ITargetBlock<ThrowOn>)s).Post(m), new WriteOnceBlock<ThrowOn>(x => x), HardeningScenario.Async));
            Assert.True(TestHardeningSource((s, m) => ((ITargetBlock<ThrowOn>)s).Post(m), new TransformBlock<ThrowOn, ThrowOn>(x => x), HardeningScenario.Sync));
            Assert.True(TestHardeningSource((s, m) => ((ITargetBlock<ThrowOn>)s).Post(m), new TransformManyBlock<ThrowOn, ThrowOn>(x => new[] { x }), HardeningScenario.Async));
            Assert.True(TestHardeningSource((s, m) => ((ITargetBlock<ThrowOn>)s).Post(m), new BatchBlock<ThrowOn>(1), HardeningScenario.Sync));
            Assert.True(TestHardeningSource((s, m) => ((ITargetBlock<ThrowOn>)s).Post(m), new BatchBlock<ThrowOn>(1), HardeningScenario.Async));

            // ConsumeMessage (greedy): Batch, BatchedJoin, Join, WriteOnce, Buffer, Broadcast, TargetCore sync (~Action), TargetCore async (~Action)
            Assert.True(TestHardeningTargetJoin2(new JoinBlock<ThrowOn, ThrowOn>(), ThrowOn.ConsumeMessage, greedy: true));
            Assert.True(TestHardeningTarget(new BatchBlock<ThrowOn>(2), ThrowOn.ConsumeMessage, greedy: true));
            Assert.True(TestHardeningTarget(new BufferBlock<ThrowOn>(), ThrowOn.ConsumeMessage, greedy: true));
            Assert.True(TestHardeningTarget(new BroadcastBlock<ThrowOn>(x => x), ThrowOn.ConsumeMessage, greedy: true));
            Assert.True(TestHardeningTarget(new ActionBlock<ThrowOn>(x => { }), ThrowOn.ConsumeMessage, greedy: true));
            Assert.True(TestHardeningTarget(new ActionBlock<ThrowOn>(x => { return Task.Run(() => { }); }), ThrowOn.ConsumeMessage, greedy: true));

            // ConsumeMessage (non-greedy): Batch, Join, TargetCore sync (~Action), TargetCore async (~Action), bounded Buffer
            var nonGreedyGroupingOptions = new GroupingDataflowBlockOptions { Greedy = false }; // Sequential, non-greedy
            var nonGreedyExecutionOptions = new ExecutionDataflowBlockOptions { BoundedCapacity = 1 }; // Sequential, non-greedy
            Assert.True(TestHardeningTarget(new ActionBlock<ThrowOn>(x => { return Task.Run(() => { Task.Delay(1000).Wait(); }); }, nonGreedyExecutionOptions), ThrowOn.ConsumeMessage, greedy: false));
            Assert.True(TestHardeningTarget(new ActionBlock<ThrowOn>(x => { Task.Delay(1000).Wait(); }, nonGreedyExecutionOptions), ThrowOn.ConsumeMessage, greedy: false));
            Assert.True(TestHardeningTarget(new BufferBlock<ThrowOn>(new DataflowBlockOptions { BoundedCapacity = BufferBoundedCapacity }), ThrowOn.ConsumeMessage, greedy: false));

            Assert.True(TestHardeningTargetJoin2(new JoinBlock<ThrowOn, ThrowOn>(nonGreedyGroupingOptions), ThrowOn.ReleaseReservation, greedy: false));
            Assert.True(TestHardeningTargetJoin2(new JoinBlock<ThrowOn, ThrowOn>(nonGreedyGroupingOptions), ThrowOn.ReserveMessage, greedy: false));

            // User callbacks: Action, Action(Async), Transform, Transform(Async), 
            //                 TransformMany(IEnumerable), TransformMany(Async), WriteOnce, Broadcast
            Assert.True(TestHardeningCallback(new ActionBlock<int>(x => { if (x == 1) throw new InvalidOperationException("Callback"); else return null as Task; }), "Async"));
            Assert.True(TestHardeningCallback(new TransformBlock<int, int>(x => { if (x == 1) throw new InvalidOperationException("Callback"); else return x; }), ""));
            Assert.True(TestHardeningCallback(new TransformBlock<int, int>(x => { if (x == 1) throw new InvalidOperationException("Callback"); else return null as Task<int>; }), "Async"));
            Assert.True(TestHardeningCallback(new TransformManyBlock<int, int>(x => { if (x == 1) throw new InvalidOperationException("Callback"); else return null as IEnumerable<int>; }), ""));
            Assert.True(TestHardeningCallback(new TransformManyBlock<int, int>(x => { if (x == 1) throw new InvalidOperationException("Callback"); else return null as Task<IEnumerable<int>>; }), ""));
            Assert.True(TestHardeningCallback(new WriteOnceBlock<int>(x => { if (x == 1) throw new InvalidOperationException("Callback"); else return x; }), ""));
            Assert.True(TestHardeningCallback(new BroadcastBlock<int>(x => { if (x == 1) throw new InvalidOperationException("Callback"); else return x; }), ""));

            // TransformMany: IEnumerable traversal
            Assert.True(TestHardeningTransformMany(new TransformManyBlock<int, int>(x => new ThrowerEnumerable(ThrowOn.GetEnumerator)), "GetEnumerator"));
            Assert.True(TestHardeningTransformMany(new TransformManyBlock<int, int>(x => new ThrowerEnumerable(ThrowOn.MoveNext)), "MoveNext"));
            Assert.True(TestHardeningTransformMany(new TransformManyBlock<int, int>(x => new ThrowerEnumerable(ThrowOn.Current)), "Current"));
            Assert.True(TestHardeningTransformMany(new TransformManyBlock<int, int>(x => Task.Run(() => (IEnumerable<int>)new ThrowerEnumerable(ThrowOn.GetEnumerator))), "GetEnumerator"));
            Assert.True(TestHardeningTransformMany(new TransformManyBlock<int, int>(x => Task.Run(() => (IEnumerable<int>)new ThrowerEnumerable(ThrowOn.MoveNext))), "MoveNext"));
            Assert.True(TestHardeningTransformMany(new TransformManyBlock<int, int>(x => Task.Run(() => (IEnumerable<int>)new ThrowerEnumerable(ThrowOn.Current))), "Current"));

            // Extensions: SendAsync, Receive (Sync), Receive (Async), ReceiveAsync (Sync), ReceiveAsync (Async)
            Assert.True(TestHardeningExtensionsReceive(HardeningScenario.Async));
            Assert.True(TestHardeningExtensionsReceiveAsync(HardeningScenario.Sync));
            Assert.True(TestHardeningExtensionsReceive(HardeningScenario.Sync));
            Assert.True(TestHardeningExtensionsReceiveAsync(HardeningScenario.Async));
        }

        // Tests IDataflowBlock.Fault
        [Fact]
        [OuterLoop]
        public void TestFault()
        {
            var nonGreedyExecutionOptions = new ExecutionDataflowBlockOptions() { BoundedCapacity = 1 };
            var nonGreedyGroupingOptions = new GroupingDataflowBlockOptions() { Greedy = false };
            var boundedBufferOptions = new DataflowBlockOptions() { BoundedCapacity = 4 };

            Assert.True(TestFaultCore<int>(new ActionBlock<int>(x => { }), withMessages: false, greedy: true));
            Assert.True(TestFaultCore<int>(new ActionBlock<int>(x => { }), withMessages: true, greedy: true));
            Assert.True(TestFaultCore<int>(new ActionBlock<int>(x => { }, nonGreedyExecutionOptions), withMessages: true, greedy: false));

            Assert.True(TestFaultCore<int>(new TransformBlock<int, int>(x => x), withMessages: false, greedy: true));
            Assert.True(TestFaultCore<int>(new TransformBlock<int, int>(x => x), withMessages: true, greedy: true));
            Assert.True(TestFaultCore<int>(new TransformBlock<int, int>(x => x, nonGreedyExecutionOptions), withMessages: true, greedy: false));

            Assert.True(TestFaultCore<int>(new TransformManyBlock<int, int>(x => new int[] { x }), withMessages: false, greedy: true));
            Assert.True(TestFaultCore<int>(new TransformManyBlock<int, int>(x => new int[] { x }), withMessages: true, greedy: true));
            Assert.True(TestFaultCore<int>(new TransformManyBlock<int, int>(x => new int[] { x }, nonGreedyExecutionOptions), withMessages: true, greedy: false));

            Assert.True(TestFaultCore<int>(new BufferBlock<int>(boundedBufferOptions), withMessages: false, greedy: true));
            Assert.True(TestFaultCore<int>(new BufferBlock<int>(boundedBufferOptions), withMessages: true, greedy: true));

            Assert.True(TestFaultCore<int>(new BroadcastBlock<int>(x => x), withMessages: false, greedy: true));
            Assert.True(TestFaultCore<int>(new BroadcastBlock<int>(x => x), withMessages: true, greedy: true));

            Assert.True(TestFaultCore<int>(new BatchBlock<int>(4), withMessages: false, greedy: true));
            Assert.True(TestFaultCore<int>(new BatchBlock<int>(4), withMessages: true, greedy: true));
            Assert.True(TestFaultCore<int>(new BatchBlock<int>(4, nonGreedyGroupingOptions), withMessages: true, greedy: false));

            Assert.True(TestFaultCore<int>(new WriteOnceBlock<int>(x => x), withMessages: false, greedy: true));

            Assert.True(TestFaultCore<int>(new JoinBlock<int, int>(), withMessages: false, greedy: true));
            Assert.True(TestFaultCore<int>(new JoinBlock<int, int>(), withMessages: true, greedy: true));
            Assert.True(TestFaultCore<int>(new JoinBlock<int, int>(nonGreedyGroupingOptions), withMessages: true, greedy: false));

            Assert.True(TestFaultCore<int>(new JoinBlock<int, int, int>(), withMessages: false, greedy: true));
            Assert.True(TestFaultCore<int>(new JoinBlock<int, int, int>(), withMessages: true, greedy: true));
            Assert.True(TestFaultCore<int>(new JoinBlock<int, int, int>(nonGreedyGroupingOptions), withMessages: true, greedy: false));

            Assert.True(TestFaultCore<int>(new BatchedJoinBlock<int, int>(4), withMessages: false, greedy: true));
            Assert.True(TestFaultCore<int>(new BatchedJoinBlock<int, int>(4), withMessages: true, greedy: true));

            Assert.True(TestFaultCore<int>(new BatchedJoinBlock<int, int, int>(4), withMessages: false, greedy: true));
            Assert.True(TestFaultCore<int>(new BatchedJoinBlock<int, int, int>(4), withMessages: true, greedy: true));
        }

        private static bool TestFaultCore<T>(IDataflowBlock block, bool withMessages, bool greedy)
        {
            Console.WriteLine("* TestFaultCore {0} (withMessages={1}, greedy={2})", block.GetType().Name, withMessages, greedy);

            const int MessageCount = 100;
            bool passed = true;
            var rnd = new Random(s_randomSeed);
            var errorMessage = rnd.Next().ToString();
            var excIn = new InvalidOperationException(errorMessage);
            Task<bool>[] sendTasks = null;

            // If messages are needed, send some asynchronously.
            // We don't need to wait for the items to be consumed now, but we want to hold on to the tasks
            // so that we can wait on them later.
            if (withMessages)
            {
                for (int i = 0; i < MessageCount; i++)
                {
                    if (block is ITargetBlock<T>)
                    {
                        if (sendTasks == null) sendTasks = new Task<bool>[MessageCount];

                        sendTasks[i] = ((ITargetBlock<T>)block).SendAsync(default(T));
                    }
                    else if (block is JoinBlock<T, T>)
                    {
                        if (sendTasks == null) sendTasks = new Task<bool>[2 * MessageCount];

                        sendTasks[i] = ((JoinBlock<T, T>)block).Target1.SendAsync(default(T));
                        sendTasks[i + MessageCount] = ((JoinBlock<T, T>)block).Target2.SendAsync(default(T));
                    }
                    else if (block is JoinBlock<T, T, T>)
                    {
                        if (sendTasks == null) sendTasks = new Task<bool>[3 * MessageCount];

                        sendTasks[i] = ((JoinBlock<T, T, T>)block).Target1.SendAsync(default(T));
                        sendTasks[i + MessageCount] = ((JoinBlock<T, T, T>)block).Target2.SendAsync(default(T));
                        sendTasks[i + 2 * MessageCount] = ((JoinBlock<T, T, T>)block).Target3.SendAsync(default(T));
                    }
                    else if (block is BatchedJoinBlock<T, T>)
                    {
                        if (sendTasks == null) sendTasks = new Task<bool>[2 * MessageCount];

                        sendTasks[i] = ((BatchedJoinBlock<T, T>)block).Target1.SendAsync(default(T));
                        sendTasks[i + MessageCount] = ((BatchedJoinBlock<T, T>)block).Target2.SendAsync(default(T));
                    }
                    else if (block is BatchedJoinBlock<T, T, T>)
                    {
                        if (sendTasks == null) sendTasks = new Task<bool>[3 * MessageCount];

                        sendTasks[i] = ((BatchedJoinBlock<T, T, T>)block).Target1.SendAsync(default(T));
                        sendTasks[i + MessageCount] = ((BatchedJoinBlock<T, T, T>)block).Target2.SendAsync(default(T));
                        sendTasks[i + 2 * MessageCount] = ((BatchedJoinBlock<T, T, T>)block).Target3.SendAsync(default(T));
                    }
                    else throw new InvalidOperationException(string.Format("Unexpected block type: {0}", block.GetType().Name));
                }
            }

            // Fault the block now
            block.Fault(excIn);

            // Now let's wait for all send tasks to complete in order to keep things clean.
            // Ignore cancellation exceptions.
            try { if (withMessages) Task.WaitAll(sendTasks); }
            catch (AggregateException ae) { ae.Handle(e => e is OperationCanceledException); }

            // The block should be faulted
            Assert.True(TaskHasFaulted(block.Completion, errorMessage));

            return passed;
        }

        private class ThrowFromToString
        {
            public override string ToString() { throw new InvalidOperationException(); }
        }

        private class ThrowFromDataException : Exception
        {
            public override IDictionary Data { get { throw new InvalidOperationException(); } }
        }

        [Fact]
        [OuterLoop]
        public void TestFaultyTaskScheduler()
        {
            bool passed = true;
            var faultyScheduler = new ControllableTaskScheduler();
            faultyScheduler.FailQueueing = true;

            var testedBlocks = new IDataflowBlock[]
                {
                    new ActionBlock<int>(x => {}, new ExecutionDataflowBlockOptions(){ TaskScheduler = faultyScheduler }),
                    new TransformBlock<int, int>(x => x, new ExecutionDataflowBlockOptions(){ TaskScheduler = faultyScheduler }),
                    new TransformManyBlock<int, int>(x => new int[] { x }, new ExecutionDataflowBlockOptions(){ TaskScheduler = faultyScheduler }),
                    new BatchBlock<int>(1, new GroupingDataflowBlockOptions(){ TaskScheduler = faultyScheduler }),
                    new JoinBlock<int, int>(new GroupingDataflowBlockOptions(){ TaskScheduler = faultyScheduler, Greedy = false }),
                    new BufferBlock<int>(new DataflowBlockOptions(){ TaskScheduler = faultyScheduler, BoundedCapacity = 1 }),
                    new BufferBlock<int>(new DataflowBlockOptions(){ TaskScheduler = faultyScheduler }), // Hits SourceCore (without BufferBlock)
                    new BroadcastBlock<int>(null, new DataflowBlockOptions(){ TaskScheduler = faultyScheduler, BoundedCapacity = 1 }), // Hits both target and source side
                    new BroadcastBlock<int>(null, new DataflowBlockOptions(){ TaskScheduler = faultyScheduler }), // Hits only source side
                    new WriteOnceBlock<int>(null, new DataflowBlockOptions(){ TaskScheduler = faultyScheduler }),
                    // BatchedJoinBlock does not have async processing
                };

            // Post to the tested block to trigger processing which should fault the block
            foreach (var testedBlock in testedBlocks)
            {
                var testedBlockType = testedBlock.GetType();

                // The JoinBlock is non-greedy in order to trigger async processing/consumption
                if (testedBlockType == typeof(JoinBlock<int, int>))
                {
                    var testedJoinBlock = (testedBlock as JoinBlock<int, int>);
                    testedJoinBlock.Target1.SendAsync(41);
                    testedJoinBlock.Target2.SendAsync(42);
                }
                else
                {
                    var testedTarget = (testedBlock as ITargetBlock<int>);
                    testedTarget.SendAsync(41); // This is accepted synchronously
                    testedTarget.SendAsync(42); // This is postponed by bounded blocks

                    // For BufferBlock to trigger async consumption, we use bounded capacity. 
                    // We offer extra messages that will get postponed, and we receive to free up space. 
                    if (testedBlockType == typeof(BufferBlock<int>))
                    {
                        var testedReceivableSource = (testedBlock as IReceivableSourceBlock<int>);
                        int dummy;
                        testedReceivableSource.TryReceive(out dummy);
                    }
                }

                var localPassed = TaskHasFaulted(testedBlock.Completion, null);
                passed &= localPassed;
            }

            Assert.True(passed, string.Format("{0}", passed ? "Passed" : "FAILED"));
        }

        private class ControllableTaskScheduler : TaskScheduler
        {
            protected override IEnumerable<Task> GetScheduledTasks() { return null; }

            protected override void QueueTask(Task task)
            {
                if (FailQueueing) throw new InvalidOperationException("FailQueueing == true");
                else ThreadPool.QueueUserWorkItem(delegate { TryExecuteTask(task); });
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                if (FailQueueing) throw new InvalidOperationException("FailQueueing == true");
                else return TryExecuteTask(task);
            }

            public bool FailQueueing { get; set; }
        }

        // Tests how a propagator block handles misbehaving targets.
        private static bool TestHardeningSource<TOutput>(Action<ISourceBlock<TOutput>, ThrowOn> postMessage, ISourceBlock<TOutput> source, HardeningScenario scenario)
        {
            bool passed = true;

            // The message is always ThrowOn.OfferMessage
            ThrowOn message = ThrowOn.OfferMessage;

            // The target is always ThrowerBlock
            ITargetBlock<TOutput> target = new ThrowerTarget<TOutput>();

            Console.WriteLine(string.Format("* TestHardeningSource: OfferMessage ({0}) {1}", scenario, source.GetType().Name));

            switch (scenario)
            {
                case HardeningScenario.Sync:
                    // If we are in the Sync scenario, 
                    // we must post a message to the source BEFORE linking the target.
                    // Then we should expect the exception to be surfaced to the user.
                    postMessage(source, message);
                    Task.Delay(15).Wait();
                    try
                    {
                        source.LinkTo(target);
                        Console.WriteLine("Exception propagated - FAILED");
                        passed = false;
                    }
                    catch (InvalidOperationException e)
                    {
                        Console.WriteLine("Exception propagated - Passed");
                        bool exceptionMatched = e.Message.Equals(message.ToString());
                        passed &= exceptionMatched;
                        Assert.True(exceptionMatched, string.Format("Exception matched ({0}) - {1}", e.Message, exceptionMatched ? "Passed" : "FAILED"));
                    }
                    break;

                case HardeningScenario.Async:
                    // If we are in the Async scenario, 
                    // we must post a message to the source AFTER linking the target.
                    // Then we should verify the exception has been collected and the task has been faulted.
                    source.LinkTo(target);
                    try
                    {
                        postMessage(source, message);
                        Task.Delay(5).Wait();
                        Console.WriteLine("Exception not propagated - Passed");
                    }
                    catch (InvalidOperationException)
                    {
                        Console.WriteLine("Exception not propagated - FAILED");
                        passed = false;
                    }

                    // When the target block throws, the exception must be collected in the source,
                    // and the source's Completion must be faulted.
                    Assert.True(TaskHasFaulted(source.Completion, message.ToString()));
                    break;

                default:
                    throw new InvalidOperationException(string.Format("Unhandled scenario: {0}", scenario));
            }

            Assert.True(passed, string.Format("{0}", passed ? "Passed" : "FAILED"));
            return passed;
        }


        // Tests how a target block handles misbehaving sources.
        private static bool TestHardeningTarget(ITargetBlock<ThrowOn> target, ThrowOn message, bool greedy)
        {
            Console.WriteLine(string.Format("* TestHardeningTarget: {0} ({1}) {2}", message, greedy ? "greedy" : "non-greedy", target.GetType().Name));
            bool passed = true;

            // The source is always ThrowerBlock
            IPropagatorBlock<ThrowOn, ThrowOn> source = new ThrowerBlock();
            source.Post(message);

            try
            {
                // Setup BEFORE the bad source is linked
                if (!greedy)
                {
                    // Certain blocks need additional messages
                    if (target is ActionBlock<ThrowOn>)
                    {
                        // ActionBlock (TargetCore) needs a single message to trigger the "slow" callback
                        target.SendAsync(ThrowOn.Uninitialized);
                    }
                    else if (target is BufferBlock<ThrowOn>)
                    {
                        // Bounded Buffer needs to fill up its capacity in order to start postponing
                        var buffer = (BufferBlock<ThrowOn>)target;
                        for (int i = 0; i < BufferBoundedCapacity; i++) buffer.SendAsync(ThrowOn.Uninitialized);
                    }
                }


                // *** LINKING THE BAD SOURCE ***
                source.LinkTo(target);


                // Setup AFTER the bad source is linked
                if (!greedy)
                {
                    if (target is BufferBlock<ThrowOn>)
                    {
                        // Receive one message to cause the postponed message from the bad source to be consumed
                        var buffer = (BufferBlock<ThrowOn>)target;
                        var ignored = buffer.Receive();
                    }
                    else if (target is BatchBlock<ThrowOn>)
                    {
                        // Non-greedy Batch needs a whole batch
                        var batch = (BatchBlock<ThrowOn>)target;
                        for (int i = 0; i < batch.BatchSize - 1; i++)
                        {
                            if (message == ThrowOn.ReleaseReservation)
                            {
                                // Reservation is always tested through the bad source.
                                // This source doesn't support reservation in order to trigger ReleaseReservation (on the first source.)
                                var source2 = new ThrowerBlock(false);
                                source2.Post(message);
                                source2.LinkTo(target);
                            }
                            else
                            {
                                batch.SendAsync(ThrowOn.Uninitialized);
                            }
                        }
                    }
                }

                // In non-greedy mode, the exception is collected in the block's Completion.
                // In greedy mode, the exception is propagated here.
                if (!greedy)
                {
                    Console.WriteLine("Exception not propagated - Passed");
                }
                else
                {
                    Console.WriteLine("Exception propagated - FAILED");
                    passed = false;
                }
            }
            catch (InvalidOperationException e)
            {
                if (!greedy)
                {
                    Console.WriteLine("Exception not propagated - FAILED");
                    passed = false;
                }
                else
                {
                    Console.WriteLine("Exception propagated - Passed");
                    bool exceptionMatched = e.Message.Equals(message.ToString());
                    passed &= exceptionMatched;
                    Assert.True(exceptionMatched, string.Format("Exception matched ({0}) - {1}", e.Message, exceptionMatched ? "Passed" : "FAILED"));
                }
            }

            // In non-greedy mode, the target's task must be faulted
            if (!greedy)
            {
                Assert.True(TaskHasFaulted(target.Completion, message.ToString()));
            }

            Assert.True(passed, string.Format("{0}", passed ? "Passed" : "FAILED"));
            return passed;
        }


        // Tests how Join and BatchedJoin handle misbehaving sources.
        private static bool TestHardeningTargetJoin2<TJoin>(TJoin target, ThrowOn message, bool greedy)
        {
            bool passed = true;

            Console.WriteLine(string.Format("* TestHardeningTargetJoin2: {0} ({1}) {2}", message, greedy ? "greedy" : "non-greedy", target.GetType().Name));

            ThrowerBlock source1 = new ThrowerBlock();
            ThrowerBlock source2 = new ThrowerBlock(supportReservation: message != ThrowOn.ReleaseReservation); // The second source must not support reservation in order to test ReleaseReservation

            // The scenario is always Sync
            source1.Post(message);
            source2.Post(message);
            try
            {
                if (target is JoinBlock<ThrowOn, ThrowOn>)
                {
                    source1.LinkTo((target as JoinBlock<ThrowOn, ThrowOn>).Target1);
                    source2.LinkTo((target as JoinBlock<ThrowOn, ThrowOn>).Target2);
                }
                else
                {
                    source1.LinkTo((target as BatchedJoinBlock<ThrowOn, ThrowOn>).Target1);
                    source2.LinkTo((target as BatchedJoinBlock<ThrowOn, ThrowOn>).Target2);
                }

                // In non-greedy mode, the exception must be collected in the block's Completion.
                // In greedy mode, the exception must be propagated.
                if (!greedy)
                {
                    Console.WriteLine("Exception not propagated - Passed");
                }
                else
                {
                    Console.WriteLine("Exception propagated - FAILED");
                    passed = false;
                }
            }
            catch (InvalidOperationException e)
            {
                // In the case of ReserveMessage and ReleaseReservation, the exception must not be propagated
                if (!greedy)
                {
                    Console.WriteLine("Exception not propagated - FAILED");
                    passed = false;
                }
                else
                {
                    Console.WriteLine("Exception propagated - Passed");
                    bool exceptionMatched = e.Message.Equals(message.ToString());
                    passed &= exceptionMatched;
                    Assert.True(exceptionMatched, string.Format("Exception matched ({0}) - {1}", e.Message, exceptionMatched ? "Passed" : "FAILED"));
                }
            }

            // In the case of ReserveMessage and ReleaseReservation, the target's task must be faulted
            if (!greedy)
            {
                Assert.True(TaskHasFaulted((target as IDataflowBlock).Completion, message.ToString()));
            }

            return passed;
        }

        private static bool TestHardeningCallback(ITargetBlock<int> target, string modifier)
        {
            bool passed = true;

            Console.WriteLine("* TestHardeningCallback: {0} {1}", target.GetType().Name, modifier);

            // Link a dummy target to propagator blocks to make sure the callback is triggered
            ActionBlock<int> dummy = new ActionBlock<int>(x => { });
            if (target is ISourceBlock<int>)
            {
                (target as ISourceBlock<int>).LinkTo(dummy);
                Console.WriteLine("Linked to dummy target");
            }

            target.Post(1);
            Task.Delay(5).Wait();

            // Verify the target's task has completed and is in faulted state
            Assert.True(TaskHasFaulted(target.Completion, "Callback"));

            return passed;
        }

        private static bool TestHardeningTransformMany(TransformManyBlock<int, int> source, string modifier)
        {
            bool passed = true;

            Console.WriteLine(string.Format("* TestHardeningTransformMany: ({0})", modifier));

            // Link a dummy target to make sure this source churns on its input
            ActionBlock<int> target = new ActionBlock<int>(x => { });
            source.LinkTo(target);

            try
            {
                source.Post(1);
                Task.Delay(5).Wait();
                Console.WriteLine("Exception not propagated - Passed");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("Exception not propagated - FAILED");
                passed = false;
            }

            // Verify the target's task has completed and is in faulted state
            Assert.True(TaskHasFaulted(source.Completion, modifier));

            return passed;
        }

        [Fact]
        [OuterLoop]
        public void TestHardeningExtensionsSendAsync()
        {
            bool passed = true;

            Console.WriteLine("* TestHardeningExtensions: SendAsync");
            Task<bool> completionTask = null;

            try
            {
                completionTask = new ThrowerBlock().SendAsync(ThrowOn.OfferMessage);
                Task.Delay(500).Wait();
                Console.WriteLine("Exception not propagated - Passed");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("Exception not propagated - FAILED");
                passed = false;
            }

            // Verify the task has completed and is in faulted state
            Assert.True(TaskHasFaulted(completionTask, "OfferMessage"));

            Assert.True(passed, string.Format("{0}", passed ? "Passed" : "FAILED"));
        }

        private static bool TestHardeningExtensionsReceive(HardeningScenario scenario)
        {
            bool passed = true;

            Console.WriteLine(string.Format("* TestHardeningExtensions: Receive ({0})", scenario));
            ThrowerBlock source = new ThrowerBlock();
            ThrowOn message = ThrowOn.Uninitialized;

            switch (scenario)
            {
                case HardeningScenario.Sync:
                    // In this scenario the source must have a message ready for immediate consuption.
                    message = ThrowOn.TryReceive;
                    source.Post(message);
                    break;

                case HardeningScenario.Async:
                    // In this scenario the source must not have messages prior to the receive attempt.
                    // Therefore delay the post.
                    message = ThrowOn.ConsumeMessage;
                    Task.Run(() => { Task.Delay(5).Wait(); source.Post(message); });
                    break;
            }

            try
            {
                ThrowOn item = source.Receive(new TimeSpan(0, 0, 2));
                Console.WriteLine("Exception propagated - FAILED");
                passed = false;
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine("Exception propagated - Passed");
                bool exceptionMatched = e.Message.Equals(message.ToString());
                passed &= exceptionMatched;
                Assert.True(exceptionMatched, string.Format("Exception matched ({0}) - {1}", e.Message, exceptionMatched ? "Passed" : "FAILED"));
            }
            catch (TimeoutException)
            {
                Console.WriteLine("Not hung - FAILED");
                passed = false;
            }

            source.Cleanup();
            return passed;
        }

        private static bool TestHardeningExtensionsReceiveAsync(HardeningScenario scenario)
        {
            bool passed = true;
            ThrowOn message = ThrowOn.Uninitialized;

            Console.WriteLine(string.Format("* TestHardeningExtensions: ReceiveAsync ({0})", scenario));
            ThrowerBlock source = new ThrowerBlock();

            switch (scenario)
            {
                case HardeningScenario.Sync:
                    // In this scenario the source must have a message ready for immediate consuption.
                    message = ThrowOn.TryReceive;
                    source.Post(message);
                    break;

                case HardeningScenario.Async:
                    // In this scenario the source must not have messages prior to the receive attempt.
                    // Therefore delay the post.
                    message = ThrowOn.ConsumeMessage;
                    Task.Run(() => { Task.Delay(5).Wait(); source.Post(message); });
                    break;
            }

            Task<ThrowOn> completionTask = null;
            try
            {
                completionTask = source.ReceiveAsync();
                Console.WriteLine("Exception not propagated - Passed");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("Exception not propagated - FAILED");
                passed = false;
            }

            // Verify the task has completed and is in faulted state
            if (completionTask != null)
            {
                Assert.True(TaskHasFaulted(completionTask, message.ToString()));
            }
            else
            {
                Console.WriteLine("Completion task returned - FAILED");
                passed = false;
            }

            source.Cleanup();
            return passed;
        }

        [Fact]
        [OuterLoop]
        public void TestHardeningExtensionsWithDisposedCancellationTokenSource()
        {
            var cts = new CancellationTokenSource();
            var ct = cts.Token;
            cts.Dispose();

            // We'll need the block to postpone for SendAsync
            var source = new BufferBlock<int>(new DataflowBlockOptions() { BoundedCapacity = 1 });

            // OutputAvailableAsync
            {
                Console.WriteLine("* TestHardeningExtensionsWithDisposedCancellationTokenSource - OutputAvailableAsync");

                var localPassed = true;
                Task<bool> completionTask = null;
                try
                {
                    completionTask = source.OutputAvailableAsync(ct);
                    Console.WriteLine("Exception not propagated - Passed");
                    localPassed &= TaskHasFaulted(completionTask, null);
                }
                catch (Exception exc)
                {
                    Console.WriteLine("Exception not propagated - FAILED");
                    Console.WriteLine(string.Format(exc.ToString()));
                    localPassed = false;
                }

                Assert.True(localPassed, string.Format("{0}", localPassed ? "Passed" : "FAILED"));
            }

            // ReceiveAsync
            {
                var localPassed = true;
                Task<int> completionTask = null;
                try
                {
                    completionTask = source.ReceiveAsync(ct);
                    Console.WriteLine("Exception not propagated - Passed");
                    localPassed &= TaskHasFaulted(completionTask, null);
                }
                catch (Exception exc)
                {
                    Console.WriteLine("Exception not propagated - FAILED");
                    Console.WriteLine(string.Format(exc.ToString()));
                    localPassed = false;
                }

                Assert.True(localPassed, string.Format("{0}", localPassed ? "Passed" : "FAILED"));
            }

            // SendAsync
            {
                var localPassed = true;
                Task<bool> completionTask = null;

                // Post a message to the source to fill its bounded capacity so it starts postponing
                source.Post(1);

                try
                {
                    completionTask = source.SendAsync(2, ct);
                    Console.WriteLine("Exception not propagated - Passed");
                    localPassed &= TaskHasFaulted(completionTask, null);
                }
                catch (Exception exc)
                {
                    Console.WriteLine("Exception not propagated - FAILED");
                    Console.WriteLine(exc.ToString());
                    localPassed = false;
                }

                Assert.True(localPassed, string.Format("{0}", localPassed ? "Passed" : "FAILED"));
            }
        }

        private static bool TaskHasFaulted(Task completionTask, string expectedErrorMessage)
        {
            bool passed = true;

            // Waiting on a faulted task throws
            try
            {
                bool completed = completionTask.Wait(10000);
                Console.WriteLine(string.Format("Task completed - {0}", completed ? "Passed" : "FAILED"));
                Console.WriteLine("Task is in faulted state - FAILED");
                passed = false;
            }
            catch (AggregateException ae)
            {
                string errorMessage = "";
                ae.Handle(e => {
                    errorMessage = e.Message;
                    return (expectedErrorMessage == null) || e is InvalidOperationException;
                }); // Observe the expected exception 

                Console.WriteLine("Task completed - Passed");
                Console.WriteLine(string.Format("Task is in faulted state - {0}", completionTask.IsFaulted ? "Passed" : "FAILED"));
                passed &= completionTask.IsFaulted;
                if (expectedErrorMessage != null)
                {
                    Console.WriteLine(string.Format("Exception matched ({0}) - {1}", errorMessage, errorMessage.Equals(expectedErrorMessage) ? "Passed" : "FAILED"));
                    passed &= errorMessage.Equals(expectedErrorMessage);
                }
            }

            return passed;
        }

        private static bool CheckAssemblyConfiguration(System.Reflection.Assembly assemblyToCheck)
        {
            foreach (var attribute in assemblyToCheck.GetCustomAttributes())
            {
                var debuggableAttribute = attribute as System.Diagnostics.DebuggableAttribute;
                //if (debuggableAttribute != null) { return debuggableAttribute.IsJITTrackingEnabled; }
            }

            return false;
        }
    }


    /// <summary>
    /// Message values that drive the thrower blocks.
    /// </summary>
    internal enum ThrowOn
    {
        Uninitialized,

        // Interface methods
        OfferMessage,
        ConsumeMessage,
        ReserveMessage,
        ReleaseReservation,

        // Extension methods
        TryReceive,

        // Enumeration
        GetEnumerator,
        Current,
        MoveNext
    }


    internal enum HardeningScenario
    {
        Sync,  // OfferMessage synchronously
        Async  // OfferMessage asynchronously
    }


    /// <summary>
    /// A block that throws based on the message value being propagated.
    /// </summary>
    internal class ThrowerBlock : IPropagatorBlock<ThrowOn, ThrowOn>, IReceivableSourceBlock<ThrowOn>
    {
        private ITargetBlock<ThrowOn> _m_target;
        private ThrowOn _m_value = ThrowOn.Uninitialized;
        private TaskCompletionSource<ThrowOn> _m_tcs = new TaskCompletionSource<ThrowOn>();
        private bool _m_supportReservation;
        private ThrowOn _m_lastOperation = ThrowOn.Uninitialized;

        internal ThrowerBlock(bool supportReservation = true)
        {
            _m_supportReservation = supportReservation;
        }

        ThrowOn ISourceBlock<ThrowOn>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<ThrowOn> target, out Boolean messageConsumed)
        {
            _m_lastOperation = ThrowOn.ConsumeMessage;
            if (_m_value == ThrowOn.ConsumeMessage) throw new InvalidOperationException("ConsumeMessage");
            messageConsumed = true;
            return _m_value;
        }

        void ISourceBlock<ThrowOn>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<ThrowOn> target)
        {
            _m_lastOperation = ThrowOn.ReleaseReservation;
            if (_m_value == ThrowOn.ReleaseReservation) throw new InvalidOperationException("ReleaseReservation");
        }

        bool ISourceBlock<ThrowOn>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<ThrowOn> target)
        {
            _m_lastOperation = ThrowOn.ReserveMessage;
            if (_m_value == ThrowOn.ReserveMessage) throw new InvalidOperationException("ReserveMessage");
            return _m_supportReservation;
        }

        IDisposable ISourceBlock<ThrowOn>.LinkTo(ITargetBlock<ThrowOn> target, DataflowLinkOptions linkOptions)
        {
            _m_target = target;

            // If an item has been buffered, offer it to the target
            if (_m_value != ThrowOn.Uninitialized) _m_target.OfferMessage(new DataflowMessageHeader(1), _m_value, this, true); // We always want the target to come back and consume it

            return new DelegateDisposable();
        }

        bool IReceivableSourceBlock<ThrowOn>.TryReceive(Predicate<ThrowOn> filter, out ThrowOn item)
        {
            _m_lastOperation = ThrowOn.TryReceive;
            item = _m_value;
            if (_m_value == ThrowOn.TryReceive) throw new InvalidOperationException("TryReceive");
            return false;
        }

        bool IReceivableSourceBlock<ThrowOn>.TryReceiveAll(out IList<ThrowOn> items)
        {
            throw new NotSupportedException();
        }

        Task IDataflowBlock.Completion
        {
            get { return _m_tcs.Task; }
        }

        public void Complete()
        {
            throw new NotSupportedException();
        }

        void IDataflowBlock.Fault(Exception exception)
        {
            throw new NotSupportedException();
        }

        public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, ThrowOn messageValue, ISourceBlock<ThrowOn> source, bool consumeToAccept)
        {
            _m_lastOperation = ThrowOn.OfferMessage;
            if (messageValue == ThrowOn.OfferMessage) throw new InvalidOperationException("OfferMessage");
            _m_value = messageValue;
            return DataflowMessageStatus.Accepted;
        }

        public bool Post(ThrowOn item)
        {
            // Store the value first
            _m_value = item;

            // If the target has been linked, offer the message.
            if (_m_target != null) _m_target.OfferMessage(new DataflowMessageHeader(1), _m_value, this, true); // We always want the target to come back and consume it
            return true;
        }

        // The purpose of this method is to execute any cleanup continuation on this block.
        // In particular, this is used to cleanup Extensions.Receive/ReceiveAsync.
        internal void Cleanup()
        {
            _m_tcs.TrySetCanceled();
        }

        internal ThrowOn LastOperation { get { return _m_lastOperation; } }
    }

    internal class ThrowerTarget<T> : ITargetBlock<T>
    {
        public void Complete()
        {
            throw new InvalidOperationException("Complete");
        }

        public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T> source, bool consumeToAccept)
        {
            throw new InvalidOperationException("OfferMessage");
        }

        public Task Completion
        {
            get { throw new NotSupportedException(); }
        }

        void IDataflowBlock.Fault(Exception exception)
        {
            // No-Op
        }
    }

    /// <summary>
    /// An enumerable/enumerator that simulates bad bahavior during traversal.
    /// </summary>
    internal class ThrowerEnumerable : IEnumerable<int>, IEnumerator<int>
    {
        private ThrowOn _m_throwOn;

        internal ThrowerEnumerable(ThrowOn throwOn)
        {
            _m_throwOn = throwOn;
        }

        // IEnumerable implemntation
        public IEnumerator GetEnumerator()
        {
            throw new NotSupportedException();
        }

        IEnumerator<int> IEnumerable<int>.GetEnumerator()
        {
            if (_m_throwOn == ThrowOn.GetEnumerator) throw new InvalidOperationException("GetEnumerator");
            return this;
        }

        // IEnumerator implementation
        object IEnumerator.Current
        {
            get { throw new NotSupportedException(); }
        }

        int IEnumerator<int>.Current
        {
            get
            {
                if (_m_throwOn == ThrowOn.Current) throw new InvalidOperationException("Current");
                else return 1;
            }
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        public bool MoveNext()
        {
            if (_m_throwOn == ThrowOn.MoveNext) throw new InvalidOperationException("MoveNext");
            else return true;
        }

        // IDisposable implementation
        void IDisposable.Dispose()
        {
        }
    }
}
