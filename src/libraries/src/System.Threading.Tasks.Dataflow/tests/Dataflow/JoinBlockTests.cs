// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Xunit;

namespace System.Threading.Tasks.Dataflow.Tests
{
    public partial class DataflowBlockTests : DataflowBlockTestBase
    {
        [Fact]        
        public void RunJoinBlockTests()
        {
            Assert.True(IDataflowBlockTestHelper.TestToString(nameFormat => nameFormat != null ? new JoinBlock<int, int>(new GroupingDataflowBlockOptions() { NameFormat = nameFormat }) : new JoinBlock<int, int>()));
            Assert.True(IDataflowBlockTestHelper.TestToString(nameFormat => nameFormat != null ? new JoinBlock<int, int, int>(new GroupingDataflowBlockOptions() { NameFormat = nameFormat }) : new JoinBlock<int, int, int>()));
            Assert.True(ISourceBlockTestHelper.TestLinkTo<Tuple<int, int>>(ConstructJoinNewWithNMessages(2), 1));
            Assert.True(ISourceBlockTestHelper.TestReserveMessageAndReleaseReservation<Tuple<int, int>>(ConstructJoinNewWithNMessages(1)));
            //Assert.True(ISourceBlockTestHelper.TestReleaseReservation<Tuple<int, int>>(ConstructJoinNewWithNMessages(1)));
            Assert.True(ISourceBlockTestHelper.TestConsumeMessage<Tuple<int, int>>(ConstructJoinNewWithNMessages(1)));
            Assert.True(ISourceBlockTestHelper.TestTryReceiveWithFilter<Tuple<int, int>>(ConstructJoinNewWithNMessages(1), 1));
            Assert.True(ISourceBlockTestHelper.TestTryReceiveAll<Tuple<int, int>>(ConstructJoinNewWithNMessages(1), 1));

            Assert.True(TestJoinMaxNumberOfGroups(greedy: true, sync: true));
            Assert.True(TestJoinMaxNumberOfGroups(greedy: true, sync: false));
            // {non-greedy sync} is intentioanlly skipped because it is not supported by the block
            Assert.True(TestJoinMaxNumberOfGroups(greedy: false, sync: false));
        }

        private static JoinBlock<int, int> ConstructJoinNewWithNMessages(int messagesCount)
        {
            var block = new JoinBlock<int, int>();
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

        //[Fact(Skip = "Outerloop")]
        public void TestJoinBlockConstructor()
        {
            // without decline without option
            var block = new JoinBlock<int, int>();
            Assert.False(block.OutputCount != 0, "Constructor failed! OutputCount returned a non zero value for a brand new JoinBlock.");
            // decline without option
            block = new JoinBlock<int, int>(new GroupingDataflowBlockOptions { MaxNumberOfGroups = 1 });
            Assert.False(block.OutputCount != 0, "Constructor failed! OutputCount returned a non zero value for a brand new JoinBlock.");
            // decline with not cancelled token and default scheduler
            block = new JoinBlock<int, int>(new GroupingDataflowBlockOptions { MaxMessagesPerTask = 1 });
            Assert.False(block.OutputCount != 0, "Constructor failed! OutputCount returned a non zero value for a brand new JoinBlock.");
            // decline with a cancelled token and default scheduler
            var token = new CancellationToken(true);
            block = new JoinBlock<int, int>(new GroupingDataflowBlockOptions { MaxMessagesPerTask = 1, CancellationToken = token, MaxNumberOfGroups = 1 });
            Assert.False(block.OutputCount != 0, "Constructor failed! OutputCount returned a non zero value for a brand new JoinBlock.");
        }

        //[Fact(Skip = "Outerloop")]
        public void TestJoinInvalidArgumentValidation()
        {
            Assert.Throws<ArgumentNullException>(() => new JoinBlock<int, int>(null));
            Assert.Throws<ArgumentNullException>(() => new JoinBlock<int, int, int>(null));
            Assert.Throws<NotSupportedException>(() => { var ignored = new JoinBlock<int, int>().Target1.Completion; });
            Assert.Throws<NotSupportedException>(() => { var ignored = new JoinBlock<int, int, int>().Target3.Completion; });

            Assert.True(ISourceBlockTestHelper.TestArgumentsExceptions<Tuple<int, int>>(new JoinBlock<int, int>()));
            Assert.True(ISourceBlockTestHelper.TestArgumentsExceptions<Tuple<int, int, int>>(new JoinBlock<int, int, int>()));
        }

        //[Fact(Skip = "Outerloop")]
        public void RunJoinBlockConformanceTests()
        {
            // Test Post/Receive single block
            {
                int iter = 2;

                var block2 = new JoinBlock<int, int>();
                for (int i = 0; i < iter; i++)
                {
                    block2.Target1.Post(i);
                    block2.Target2.Post(i);
                    var msg = block2.Receive();

                    Assert.False(msg.Item1 != i || msg.Item2 != i, string.Format("JoinBlock Post/Receive failed expected {0},{1} and actual {2},{3}", i, i, msg.Item1, msg.Item2));
                }

                var block3 = new JoinBlock<int, int, int>();
                for (int i = 0; i < iter; i++)
                {
                    block3.Target1.Post(i);
                    block3.Target2.Post(i);
                    block3.Target3.Post(i);
                    var msg = block3.Receive();
                    Assert.False(msg.Item1 != i || msg.Item2 != i || msg.Item3 != i, string.Format("JoinBlock Post/Receive failed expected {0},{1},{2} and actual {3},{4},{5}", i, i, i, msg.Item1, msg.Item2, msg.Item3));
                }
            }

            // Test PostAll then Receive single block
            {
                int iter = 2;

                var block2 = new JoinBlock<int, int>();
                for (int i = 0; i < iter; i++)
                {
                    block2.Target1.Post(i);
                    block2.Target2.Post(i);
                }
                for (int i = 0; i < iter; i++)
                {
                    var msg = block2.Receive();
                    Assert.False(msg.Item1 != msg.Item2, "JoinBlock PostAll then Receive failed expected, incorrect msg pair");
                }

                var block3 = new JoinBlock<int, int, int>();
                for (int i = 0; i < iter; i++)
                {
                    block3.Target1.Post(i);
                    block3.Target2.Post(i);
                    block3.Target3.Post(i);
                }
                for (int i = 0; i < iter; i++)
                {
                    var msg = block3.Receive();
                    Assert.False(msg.Item1 != msg.Item2 || msg.Item2 != msg.Item3, "JoinBlock PostAll then Receive failed expected, incorrect msg pair");
                }
            }

            //Test one target Post with TryReceive
            {
                var block2 = new JoinBlock<int, int>();
                block2.Target1.Post(0);
                Tuple<int, int> result2;
                Assert.False(block2.TryReceive(out result2), "JoinBlock.TryReceive failed, returned true and only one target posted a message");
                Assert.False(block2.OutputCount > 0, "JoinBlock.OutputCount failed, returned count > 0 and only one target posted a message");
                var block3 = new JoinBlock<int, int, int>();
                block3.Target1.Post(0);
                Tuple<int, int, int> result3;
                Assert.False(block3.TryReceive(out result3), "JoinBlock.TryReceive failed, returned true and only one target posted a message");
                Assert.False(block3.OutputCount > 0, "JoinBlock.OutputCount failed, returned count > 0 and only one target posted a message");
            }

            // Test JoinBlock`2 using a precanceled token
            {
                var localPassed = true;
                try
                {
                    var cts = new CancellationTokenSource();
                    cts.Cancel();
                    var dbo = new GroupingDataflowBlockOptions { CancellationToken = cts.Token, MaxNumberOfGroups = 1 };
                    var jb = new JoinBlock<int, int>(dbo);

                    Tuple<int, int> ignoredValue;
                    IList<Tuple<int, int>> ignoredValues;
                    localPassed &= jb.LinkTo(new ActionBlock<Tuple<int, int>>(delegate { })) != null;
                    localPassed &= jb.Target1.Post(42) == false;
                    localPassed &= jb.Target2.Post(42) == false;
                    localPassed &= jb.Target1.SendAsync(42).Result == false;
                    localPassed &= jb.Target2.SendAsync(42).Result == false;
                    localPassed &= jb.TryReceiveAll(out ignoredValues) == false;
                    localPassed &= jb.TryReceive(out ignoredValue) == false;
                    localPassed &= jb.OutputCount == 0;
                    localPassed &= jb.Completion != null;
                    jb.Target1.Complete();
                    jb.Target2.Complete();
                }
                catch (Exception)
                {
                    localPassed = false;
                }

                Assert.True(localPassed, string.Format("Precanceled tokens on JB`2 - {0}", localPassed ? "Passed" : "FAILED"));
            }

            // Test JoinBlock`3 using a precanceled token
            {
                var localPassed = true;
                try
                {
                    var cts = new CancellationTokenSource();
                    cts.Cancel();
                    var dbo = new GroupingDataflowBlockOptions { CancellationToken = cts.Token, MaxNumberOfGroups = 1 };
                    var jb = new JoinBlock<int, int, int>(dbo);

                    Tuple<int, int, int> ignoredValue;
                    IList<Tuple<int, int, int>> ignoredValues;
                    localPassed &= jb.LinkTo(new ActionBlock<Tuple<int, int, int>>(delegate { })) != null;
                    localPassed &= jb.Target1.Post(42) == false;
                    localPassed &= jb.Target2.Post(42) == false;
                    localPassed &= jb.Target3.Post(42) == false;
                    localPassed &= jb.Target1.SendAsync(42).Result == false;
                    localPassed &= jb.Target2.SendAsync(42).Result == false;
                    localPassed &= jb.Target3.SendAsync(42).Result == false;
                    localPassed &= jb.TryReceiveAll(out ignoredValues) == false;
                    localPassed &= jb.TryReceive(out ignoredValue) == false;
                    localPassed &= jb.OutputCount == 0;
                    localPassed &= jb.Completion != null;
                    jb.Target1.Complete();
                    jb.Target2.Complete();
                    jb.Target3.Complete();
                }
                catch (Exception)
                {
                    localPassed = false;
                }

                Assert.True(localPassed, string.Format("Precanceled tokens on JB`3 - {0}", localPassed ? "Passed" : "FAILED"));
            }

            // Test JoinBlock`2 completion through all targets
            {
                var localPassed = true;
                var join = new JoinBlock<int, int>();
                join.Target1.Post(1);
                join.Target1.Complete();
                join.Target2.Complete();
                localPassed = join.Completion.Wait(2000);

                Assert.True(localPassed, string.Format("JoinBlock`2 completed through targets - {0}", localPassed ? "Passed" : "FAILED"));
            }

            // Test JoinBlock`3 completion through all targets
            {
                var localPassed = true;
                var join = new JoinBlock<int, int, int>();
                join.Target1.Post(1);
                join.Target1.Complete();
                join.Target2.Complete();
                join.Target3.Complete();
                localPassed = join.Completion.Wait(2000);

                Assert.True(localPassed, string.Format("JoinBlock`3 completed through targets - {0}", localPassed ? "Passed" : "FAILED"));
            }

            // Test JoinBlock`2 completion through block
            {
                var localPassed = true;
                var join = new JoinBlock<int, int>();
                join.Target1.Post(1);
                join.Complete();
                localPassed = join.Completion.Wait(2000);

                Assert.True(localPassed, string.Format("JoinBlock`2 completed through block - {0}", localPassed ? "Passed" : "FAILED"));
            }

            // Test JoinBlock`3 completion through block
            {
                var localPassed = true;
                var join = new JoinBlock<int, int, int>();
                join.Target1.Post(1);
                join.Complete();
                localPassed = join.Completion.Wait(2000);

                Assert.True(localPassed, string.Format("JoinBlock`3 completed through block - {0}", localPassed ? "Passed" : "FAILED"));
            }
        }

        //[Fact(Skip = "Outerloop")]
        public void TestNonGreedyFailToConsumeReservedMessage()
        {
            NullOnConsumeSource<int> source1 = new NullOnConsumeSource<int>();
            NullOnConsumeSource<int> source2 = new NullOnConsumeSource<int>();
            var options = new GroupingDataflowBlockOptions { Greedy = false };
            JoinBlock<int, int> join = new JoinBlock<int, int>(options);

            source1.LinkTo(join.Target1);
            source2.LinkTo(join.Target2);

            try
            {
                join.Completion.Wait();
                Assert.True(false, "Failed to throw an exception");
            }
            catch (AggregateException ae)
            {
                ae.Handle(e => true);
            }

            if (source2.ConsumeMessageCalled)
            {
                Assert.True(false, "Failed to skip consuming the second message");
            }
        }

        //[Fact(Skip = "outerloop")]
        public void TestJoinNonGreedyCombo()
        {
            JoinBlock<int, int> join = new JoinBlock<int, int>(new GroupingDataflowBlockOptions { Greedy = false });

            // Offer 1 message - this should be postponed
            // To avoid implementing a source, we use SendAsync and we verify the task is not completed
            var send1 = join.Target1.SendAsync(1);
            bool localPassed = !send1.Wait(500);
            Assert.True(localPassed, string.Format("SendAsync not completed - {0}", localPassed ? "Passed" : "FAILED"));

            // Output count must be 0
            localPassed = join.OutputCount == 0;
            Assert.True(localPassed, string.Format("OutputCount == 0 - {0} ({1})", localPassed ? "Passed" : "FAILED", join.OutputCount));

            // Post 1 message - the result is non-deterministic but at least no exception should be thrown
            join.Target2.Post(2);
            Assert.True(localPassed, string.Format("Post did not throw - Passed"));

            // Offer 1 message - this should be postponed
            var send2 = join.Target2.SendAsync(2);

            // The SendAsync tasks must be completed
            localPassed = send1.Wait(500);
            Assert.True(localPassed, string.Format("SendAsync1 completed - {0}", localPassed ? "Passed" : "FAILED"));
            localPassed = send2.Wait(500);
            Assert.True(localPassed, string.Format("SendAsync2 completed - {0}", localPassed ? "Passed" : "FAILED"));

            // The SendAsync tasks must have a result of true
            if (localPassed)
            {
                localPassed = send1.Result;
                Assert.True(localPassed, string.Format("SendAsync1 true - {0}", localPassed ? "Passed" : "FAILED"));

                localPassed = send2.Result;
                Assert.True(localPassed, string.Format("SendAsync2 true - {0}", localPassed ? "Passed" : "FAILED"));
            }
            Task.Delay(500).Wait();

            // Output count must be 1
            localPassed = join.OutputCount == 1;
            Assert.True(localPassed, string.Format("OutputCount == 1 - {0} ({1})", localPassed ? "Passed" : "FAILED", join.OutputCount));

            // TryReceive should return 1 result
            Tuple<int, int> result;
            localPassed = join.TryReceive(out result);
            Assert.True(localPassed, string.Format("TryReceive - {0}", localPassed ? "Passed" : "FAILED"));
        }

        //[Fact(Skip = "outerloop")]
        public void TestJoinCancelationWaits()
        {
            bool passed = true;
            bool localPassed;

            var cts = new CancellationTokenSource();
            var nonGreedyOptionsWithCancellation = new GroupingDataflowBlockOptions { CancellationToken = cts.Token, Greedy = false };

            var badSource = new BlockOnConsumeSource<int>(1, true);
            var goodSource = new BlockOnConsumeSource<int>(2, false);

            var join = new JoinBlock<int, int>(nonGreedyOptionsWithCancellation);

            // Linking a target behind the Join and feeding messages into the targets 
            // is important to trigger the functionality implemented by SourceCore. 
            var terminator = new ActionBlock<Tuple<int, int>>(x => { Console.WriteLine("terminator: ({0},{1})", x.Item1, x.Item2); });
            join.LinkTo(terminator);
            var send1 = join.Target1.SendAsync(98);
            var send2 = join.Target2.SendAsync(99);

            // Wait for the sent messages to be consumed
            send1.Wait();
            send2.Wait();

            // Each linking will offer a message
            badSource.LinkTo(join.Target1);
            goodSource.LinkTo(join.Target2);

            // Both messages must be reserved
            badSource.ReservedEvent.WaitOne();
            goodSource.ReservedEvent.WaitOne();

            // The message from badSource must be consumed
            badSource.ConsumedEvent.WaitOne();

            // Cancel the Join
            cts.Cancel();

            // The Join must not complete, because the targets are still working
            try
            {
                localPassed = !join.Completion.Wait(1000);
            }
            catch (AggregateException ae)
            {
                ae.Handle(e => e is TaskCanceledException);
                localPassed = false;
            }
            passed &= localPassed;
            Assert.True(localPassed, string.Format("Join is not complete ({0}) - {1}", join.Completion.Status, localPassed ? "Passed" : "FAILED"));

            // Unblock the blocked operation
            badSource.BlockingEvent.Set();

            // The Join must become Canceled now
            try
            {
                join.Completion.Wait(1000);
                localPassed = false;
            }
            catch (AggregateException ae)
            {
                ae.Handle(e => e is TaskCanceledException);
                localPassed = join.Completion.Status == TaskStatus.Canceled;
            }
            passed &= localPassed;
            Assert.True(localPassed, string.Format("Join is canceled ({0}) - {1}", join.Completion.Status, localPassed ? "Passed" : "FAILED"));
        }

        //[Fact(Skip = "Outerloop")]
        public void TestJoinWithFaultedTarget()
        {
            bool passed = true;

            var nonGreedyOptions = new GroupingDataflowBlockOptions { Greedy = false };
            var goodSource = new ThrowerBlock();
            var badSource = new ThrowerBlock();
            var join = new JoinBlock<ThrowOn, ThrowOn>(nonGreedyOptions);
            var terminator = new ActionBlock<Tuple<ThrowOn, ThrowOn>>(xy => { Console.WriteLine("Terminator: We shouldn't be here - FAILED"); passed = false; });

            // Pre-load the sources
            goodSource.Post(ThrowOn.TryReceive); // Will not throw in this test
            badSource.Post(ThrowOn.ConsumeMessage);

            // Link
            join.LinkTo(terminator);
            goodSource.LinkTo(join.Target1);
            badSource.LinkTo(join.Target2);
            Task.Delay(500).Wait();

            // The Join must be faulted now
            passed &= TaskHasFaulted(join.Completion, "ConsumeMessage");

            Assert.True(passed, string.Format("{0}", passed ? "Passed" : "FAILED"));
        }

        private static bool TestJoinMaxNumberOfGroups(bool greedy, bool sync)
        {
            Contract.Assert(greedy || !sync, "Non-greedy sync doesn't make sense.");

            Console.WriteLine("* TestJoinMaxNumberOfGroups (greedy={0}, sync={1})", greedy, sync);
            bool passed = true;

            for (int maxNumberOfGroups = 1; maxNumberOfGroups <= 3; maxNumberOfGroups += 2)
            {
                var options = new GroupingDataflowBlockOptions { Greedy = greedy, MaxNumberOfGroups = maxNumberOfGroups };
                var join = new JoinBlock<int, int>(options);

                for (int joinNum = 0; joinNum < maxNumberOfGroups; joinNum++)
                {
                    // Feed each target to make sure one join is produced
                    if (sync)
                    {
                        passed &= join.Target1.Post(1);
                        Assert.True(passed, string.Format("FAILED join.Target1.Post(1) should be accepted in group num {0}", joinNum));
                        if (joinNum == maxNumberOfGroups - 1)
                        {
                            passed &= !join.Target1.Post(2);
                            Assert.True(passed, string.Format("FAILED join.Target1.Post(2) should be declined in group num {0}", joinNum));
                        }

                        passed &= join.Target2.Post(3);
                        Assert.True(passed, string.Format("FAILED join.Target2.Post(3) should be accepted in group num {0}", joinNum));
                    }
                    else
                    {
                        var feed1 = join.Target1.SendAsync(1);
                        if (greedy && joinNum == maxNumberOfGroups - 1)
                        {
                            var feed2 = join.Target1.SendAsync(2);
                            passed &= feed2.Status == TaskStatus.RanToCompletion && feed2.Result == false;
                            Assert.True(passed,
                                string.Format("FAILED join.Target1.SendAsync(2) should be declined in group num {0}", joinNum));
                        }
                        var feed3 = join.Target2.SendAsync(3);

                        passed &= feed1.Result;
                        Assert.True(passed, string.Format("FAILED join.Target1.SendAsync(1) should be accepted in group num {0}", joinNum));
                        passed &= feed3.Result;
                        Assert.True(passed, string.Format("FAILED join.Target2.SendAsync(3) should be accepted in group num {0}", joinNum));
                    }
                }

                // Wait until the first join is produced
                passed &= SpinWait.SpinUntil(() => join.OutputCount == maxNumberOfGroups, 4000);
                Assert.True(passed, string.Format("FAILED Not all joins completed in a timely fashion - completed {0} out of {1}", join.OutputCount, maxNumberOfGroups));

                // This attempt to feed should fail on each target
                if (sync)
                {
                    passed &= !join.Target1.Post(4);
                    Assert.True(passed, string.Format("FAILED Target1 incorrectly accepted a post after all {0} groups completed", maxNumberOfGroups));
                    passed &= !join.Target2.Post(5);
                    Assert.True(passed, string.Format("FAILED Target2 incorrectly accepted a post after all {0} groups completed", maxNumberOfGroups));
                }
                else
                {
                    passed &= !join.Target1.SendAsync(4).Result;
                    Assert.True(passed, string.Format("FAILED Target1 incorrectly accepted a SendAsync after all {0} groups completed", maxNumberOfGroups));
                    passed &= !join.Target2.SendAsync(5).Result;
                    Assert.True(passed, string.Format("FAILED Target2 incorrectly accepted a SendAsync after all {0} groups completed", maxNumberOfGroups));
                }
            }

            return passed;
        }
    }

    /// <summary>
    /// A source block that always returns messageConsumed==false on ConsumeMessage even for reserved messages.
    /// It is testing JoinBlock only.
    /// </summary>
    internal class NullOnConsumeSource<TOutput> : ISourceBlock<TOutput>
    {
        internal bool ConsumeMessageCalled = false;

        TOutput ISourceBlock<TOutput>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target, out Boolean messageConsumed)
        {
            ConsumeMessageCalled = true;
            messageConsumed = false; // This is the purpose of the block
            return default(TOutput);
        }

        IDisposable ISourceBlock<TOutput>.LinkTo(ITargetBlock<TOutput> target, DataflowLinkOptions linkOptions)
        {
            target.OfferMessage(new DataflowMessageHeader(1), default(TOutput), this, true /* call back ConsumeMassage */);
            return new NoopOnUnlinked();
        }

        void ISourceBlock<TOutput>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
        {
        }

        bool ISourceBlock<TOutput>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
        {
            return true;
        }

        Task IDataflowBlock.Completion
        {
            get { throw new NotImplementedException(); }
        }

        public void Complete()
        {
            throw new NotImplementedException();
        }

        void IDataflowBlock.Fault(Exception exception)
        {
            throw new NotImplementedException();
        }
    }

    internal class NoopOnUnlinked : IDisposable
    {
        void IDisposable.Dispose()
        {
        }
    }


    /// <summary>
    /// Dummy Source that will offer one message and will block on consume (if required).
    /// This block should be used by the Non greedy joins.
    /// </summary>
    public class BlockOnConsumeSource<TOutput> : ISourceBlock<TOutput>
    {
        public ManualResetEvent ReservedEvent = new ManualResetEvent(false);
        public ManualResetEvent ConsumedEvent = new ManualResetEvent(false);
        public ManualResetEvent ReleasedEvent = new ManualResetEvent(false);
        public ManualResetEvent BlockingEvent = new ManualResetEvent(false);

        private TOutput _m_value;
        private bool _m_blockOnConsume;

        public BlockOnConsumeSource(TOutput value, bool blockOnConsume)
        {
            _m_value = value;
            _m_blockOnConsume = blockOnConsume;

            if (!blockOnConsume) BlockingEvent.Set();
        }

        TOutput ISourceBlock<TOutput>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target, out Boolean messageConsumed)
        {
            ConsumedEvent.Set();
            this.BlockingEvent.WaitOne();
            messageConsumed = true;
            return _m_value;
        }

        IDisposable ISourceBlock<TOutput>.LinkTo(ITargetBlock<TOutput> target, DataflowLinkOptions linkOptions)
        {
            target.OfferMessage(new DataflowMessageHeader(1), _m_value, this, consumeToAccept: true);
            return new NoopOnUnlinked();
        }

        void ISourceBlock<TOutput>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
        {
            ReleasedEvent.Set();
        }

        bool ISourceBlock<TOutput>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
        {
            ReservedEvent.Set();
            return true;
        }

        Task IDataflowBlock.Completion
        {
            get { throw new NotImplementedException(); }
        }

        public void Complete()
        {
            throw new NotImplementedException();
        }

        void IDataflowBlock.Fault(Exception exception)
        {
            throw new NotImplementedException();
        }
    }
}
