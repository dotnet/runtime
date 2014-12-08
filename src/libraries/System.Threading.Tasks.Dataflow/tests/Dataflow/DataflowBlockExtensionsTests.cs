// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Xunit;

namespace System.Threading.Tasks.Dataflow.Tests
{
    public partial class DataflowBlockTests : DataflowBlockTestBase
    {
        [Fact]
        public void TestNullTarget()
        {
            const int SHORT_WAIT = 1;

            // Test OfferMessage arguments
            {
                bool localPassed = true;
                var nullTarget = DataflowBlock.NullTarget<int>();

                localPassed = ITargetBlockTestHelper.TestArgumentsExceptions(nullTarget);

                Assert.True(localPassed, string.Format("{0}: OfferMessage arguments", localPassed ? "Success" : "Failure"));
            }

            // Test OfferMessage(consumeToAccept: false)
            {
                bool localPassed = true;
                var nullTarget = DataflowBlock.NullTarget<int>();
                StringBuilder diagnostics = new StringBuilder();

                for (int i = 0; i < 2; i++)
                {
                    var status = nullTarget.OfferMessage(new DataflowMessageHeader(1), i, source: null, consumeToAccept: false);
                    localPassed &= status == DataflowMessageStatus.Accepted;
                    if (status != DataflowMessageStatus.Accepted) diagnostics.AppendFormat("[{0}]<-{1} ", i, status);
                }

                var notCompleted = !nullTarget.Completion.Wait(SHORT_WAIT);
                if (!notCompleted) diagnostics.Append("Completed");
                localPassed &= notCompleted;

                Assert.True(localPassed, string.Format("{0}: OfferMessage(consumeToAccept: false) {1}", localPassed ? "Success" : "Failure", diagnostics.ToString()));
            }

            // Test OfferMessage(consumeToAccept: true)
            {
                bool localPassed = true;
                var nullTarget = DataflowBlock.NullTarget<ThrowOn>();
                StringBuilder diagnostics = new StringBuilder();

                for (int i = 0; i < 2; i++)
                {
                    var source = new ThrowerBlock();
                    source.LinkTo(nullTarget);
                    source.Post(ThrowOn.Uninitialized);
                    localPassed &= source.LastOperation == ThrowOn.ConsumeMessage;
                    if (source.LastOperation != ThrowOn.ConsumeMessage) diagnostics.AppendFormat("[{0}]<-{1} ", i, source.LastOperation);
                }

                var notCompleted = !nullTarget.Completion.Wait(SHORT_WAIT);
                if (!notCompleted) diagnostics.Append("Completed");
                localPassed &= notCompleted;

                Assert.True(localPassed, string.Format("{0}: OfferMessage(consumeToAccept: true) {1}", localPassed ? "Success" : "Failure", diagnostics.ToString()));
            }

            // Test bad source
            {
                bool localPassed = true;
                var nullTarget = DataflowBlock.NullTarget<ThrowOn>();
                StringBuilder diagnostics = new StringBuilder();

                for (int i = 0; i < 2; i++)
                {
                    var source = new ThrowerBlock();
                    source.LinkTo(nullTarget);
                    Assert.Throws<InvalidOperationException>(() => source.Post(ThrowOn.ConsumeMessage));
                }

                var notCompleted = !nullTarget.Completion.Wait(SHORT_WAIT);
                if (!notCompleted) diagnostics.Append("Completed");
                localPassed &= notCompleted;

                Assert.True(localPassed, string.Format("{0}: bad source {1}", localPassed ? "Success" : "Failure", diagnostics.ToString()));
            }

            // Test Complete and Completion
            {
                bool localPassed = true;
                var nullTarget = DataflowBlock.NullTarget<int>();

                for (int i = 0; i < 2; i++)
                {
                    nullTarget.Complete(); // Should be a no-op
                }
                localPassed = !nullTarget.Completion.Wait(SHORT_WAIT);

                Assert.True(localPassed, string.Format("{0}: Complete", localPassed ? "Success" : "Failure"));
            }

            // Test Fault
            {
                bool localPassed = true;
                var nullTarget = DataflowBlock.NullTarget<int>();

                for (int i = 0; i < 2; i++)
                {
                    nullTarget.Fault(new FormatException("PfxDevUnitTests")); // Should be a no-op
                }
                localPassed = !nullTarget.Completion.Wait(SHORT_WAIT);

                Assert.True(localPassed, string.Format("{0}: Fault", localPassed ? "Success" : "Failure"));
            }
        }

        //[Fact(Skip = "Outerloop")]
        public void TestAsObservableAndAsObserver()
        {
            // Test that preset data flows correctly
            {
                bool localPassed = true;
                var bb = new BufferBlock<int>();
                for (int i = 0; i < 2; i++) bb.Post(i);
                bb.Complete();

                int nextValueExpected = 0;
                var ab = new ActionBlock<int>(i =>
                {
                    Assert.True(i == nextValueExpected, string.Format("Expected next value to be {0} but got {1}", nextValueExpected, i));
                    nextValueExpected++;
                });

                bb.AsObservable().Subscribe(ab.AsObserver());

                Assert.True(ab.Completion.Wait(4000), "Expected preset data to complete the target observer");

                Assert.True(localPassed, string.Format("{0}: Preset data flows correctly", localPassed ? "Success" : "Failure"));
            }

            // Test that new data flows correctly
            {
                bool localPassed = true;

                int nextValueExpected = -2;
                var ab = new ActionBlock<int>(i =>
                {
                    Assert.True(i == nextValueExpected, string.Format("Expected next value to be {0} but got {1}", nextValueExpected, i));
                    nextValueExpected++;
                });

                var bb = new BufferBlock<int>();
                bb.AsObservable().Subscribe(ab.AsObserver());

                for (int i = -2; i < 0; i++) bb.Post(i);
                bb.Complete();

                Assert.True(ab.Completion.Wait(4000), "Expected new data to complete the target observer");

                Assert.True(localPassed, string.Format("{0}: New data flows correctly", localPassed ? "Success" : "Failure"));
            }

            // Test that unsubscribing stops flow of data and stops completion
            {
                bool localPassed = true;

                int nextValueExpected = 0;
                var ab1 = new ActionBlock<int>(i =>
                {
                    Assert.True(i == nextValueExpected, string.Format("Expected next value to be {0} but got {1}", nextValueExpected, i));
                    nextValueExpected++;
                });

                var bb = new BufferBlock<int>();
                var unsubscribe = bb.AsObservable().Subscribe(ab1.AsObserver());

                for (int i = 0; i < 2; i++) bb.Post(i);
                Assert.True(SpinWait.SpinUntil(() => nextValueExpected == 2, 4000), "Expected action block to receive initial 2 elements in a timely fashion");
                unsubscribe.Dispose();

                var wob = new WriteOnceBlock<int>(i => i);
                bb.LinkTo(wob);
                bb.Post(42);

                Assert.True(wob.Completion.Wait(4000), "Expected buffered data to skip unsubscribed target");
                bb.Complete();

                Task.Delay(10).Wait();
                Assert.False(ab1.Completion.IsCompleted, "Expected unsubscribed target to still be alive");

                Assert.True(localPassed, string.Format("{0}: Unsubscribing stops flow of data and completion", localPassed ? "Success" : "Failure"));
            }

            // Test that exceptional data flows when exception occurs before subscription
            {
                bool localPassed = true;

                var tb = new TransformBlock<int, int>(i =>
                {
                    if (i == 42) throw new InvalidOperationException("uh oh");
                    return i;
                });
                tb.Post(42);
                ((IAsyncResult)tb.Completion).AsyncWaitHandle.WaitOne(4000);

                var wob = new WriteOnceBlock<Exception>(i => i);
                var observer = new DelegateObserver<int>(
                    null, e => wob.Post(e), null);
                tb.AsObservable().Subscribe(observer);
                Assert.True(wob.Completion.Wait(4000), "Expected pre-exception to be received in a timely fashion");

                Assert.True(localPassed, string.Format("{0}: Exceptional data before subscription", localPassed ? "Success" : "Failure"));
            }

            // Test that exceptional data flows when exception occurs after subscription
            {
                bool localPassed = true;

                var tb = new TransformBlock<int, int>(i =>
                {
                    if (i == 42) throw new InvalidOperationException("uh oh");
                    return i;
                });

                var wob = new WriteOnceBlock<Exception>(i => i);
                var observer = new DelegateObserver<int>(
                    null, e => wob.Post(e), null);
                tb.AsObservable().Subscribe(observer);

                tb.Post(42);

                Assert.True(wob.Completion.Wait(4000), "Expected post-exception to be received in a timely fashion");

                Assert.True(localPassed, string.Format("{0}: Exceptional data after subscription", localPassed ? "Success" : "Failure"));
            }

            // Test that a faulted block causes observers to fault
            {
                bool localPassed = true;

                for (int iter = 0; iter < 2; iter++)
                {
                    bool faultBeforeObservation = iter == 0;
                    var bb = new BufferBlock<int>();

                    Action faultBb = () =>
                    {
                        ((IDataflowBlock)bb).Fault(new InvalidOperationException("uh oh"));
                        ((IAsyncResult)bb.Completion).AsyncWaitHandle.WaitOne();
                        Assert.True(bb.Completion.IsFaulted, "Expected the buffer block to be faulted");
                    };

                    if (faultBeforeObservation) faultBb();

                    IObservable<int>[] observables = new IObservable<int>[3];
                    for (int i = 0; i < observables.Length; i++) observables[i] = bb.AsObservable();
                    const int OBSERVERS_PER_OBSERVABLE = 2;
                    int nexts = 0, completions = 0;
                    var ce = new CountdownEvent(OBSERVERS_PER_OBSERVABLE * observables.Length);
                    var exceptions = new ConcurrentQueue<Exception>();
                    foreach (var observable in observables)
                    {
                        for (int i = 0; i < OBSERVERS_PER_OBSERVABLE; i++)
                        {
                            observable.Subscribe(new DelegateObserver<int>(
                                n => Interlocked.Increment(ref nexts),
                                e => { exceptions.Enqueue(e); ce.Signal(); },
                                () => { Interlocked.Increment(ref completions); ce.Signal(); }));
                        }
                    }

                    if (!faultBeforeObservation) faultBb();

                    Assert.True(ce.Wait(4000), string.Format("Timed out waiting for completion; current count {0} out of {1}", ce.CurrentCount, ce.InitialCount));
                    Assert.True(nexts == 0, string.Format("Excepted 0 OnNext calls but got {0}", nexts));
                    Assert.True(exceptions.Count == OBSERVERS_PER_OBSERVABLE * observables.Length,
                        string.Format("Expected {0} exceptions but got {1}", OBSERVERS_PER_OBSERVABLE * observables.Length, exceptions.Count));
                    Assert.True(completions == 0, string.Format("Excepted 0 OnComplete calls but got {0}", completions));
                }

                Assert.True(localPassed, string.Format("{0}: Faulted block faults observer", localPassed ? "Success" : "Failure"));
            }

            // Test that AsObservable returns the right instance
            {
                bool localPassed = true;

                ISourceBlock<string> b1 = new BufferBlock<string>();
                ISourceBlock<string> b2 = new BufferBlock<string>();
                ISourceBlock<string> b3 = new BufferBlock<string>();
                ISourceBlock<int> b4 = new BufferBlock<int>();
                ISourceBlock<int> b5 = new BufferBlock<int>();

                Assert.True(b1.AsObservable() == b1.AsObservable(), "AsObservable on the same instance should return the same object (ref)");
                Assert.True(
                    b1.AsObservable() != b2.AsObservable() && b2.AsObservable() != b3.AsObservable(),
                    "AsObservable on different instances of the same type should return different object (ref)");
                Assert.True(b4.AsObservable() == b4.AsObservable(), "AsObservable on the same instance should return the same object (value)");
                Assert.True(b4.AsObservable() != b5.AsObservable(), "AsObservable on different instances of the same type should return different object (value)");

                Assert.True(localPassed, string.Format("{0}: AsObservable returns the right instance", localPassed ? "Success" : "Failure"));
            }

            // Test that AsObserver doesn't remove data
            {
                bool localPassed = true;

                BufferBlock<int> b = new BufferBlock<int>();
                for (int i = 0; i < 2; i++) b.Post(i);
                Assert.True(b.Count == 2, "Expected initialization messages to be in the buffer");

                var observable = b.AsObservable();
                Assert.True(observable != null, "Expected non-null observable from AsObservable");
                Task.Delay(10).Wait();
                Assert.True(b.Count == 2, "Expected count to remain the same after calling AsObservable");

                Assert.True(localPassed, string.Format("{0}: AsObserver doesn't remove data", localPassed ? "Success" : "Failure"));
            }

            // Test that all observers get copies of the data
            {
                bool localPassed = true;

                BufferBlock<int> b = new BufferBlock<int>();
                var observable = b.AsObservable();

                BufferBlock<int> t1 = new BufferBlock<int>();
                BufferBlock<int> t2 = new BufferBlock<int>();
                BufferBlock<int> t3 = new BufferBlock<int>();
                var unsub1 = observable.Subscribe(t1.AsObserver());
                var unsub2 = observable.Subscribe(t2.AsObserver());
                var unsub3 = observable.Subscribe(t3.AsObserver());

                for (int i = 0; i < 10; i++) b.Post(i);
                bool allDataReceived = SpinWait.SpinUntil(() => t1.Count == 10 && t2.Count == 10 && t3.Count == 10, 4000);
                Assert.True(allDataReceived, "Expected all targets to receive all data");

                for (int i = 0; i < 10; i++)
                {
                    Assert.True(t1.Receive() == i, "Expected first observer to have a copy of " + i);
                    Assert.True(t2.Receive() == i, "Expected second observer to have a copy of " + i);
                    Assert.True(t3.Receive() == i, "Expected third observer to have a copy of " + i);
                }
                Assert.True(t1.Count == 0 && t2.Count == 0 && t3.Count == 0, "Expected all targets to now be empty");

                unsub1.Dispose();
                unsub2.Dispose();
                unsub3.Dispose();

                for (int iter = 0; iter < 5; iter++)
                {
                    for (int i = 0; i < 10; i++) b.Post(i);
                    Task.Delay(100).Wait();
                    Assert.True(b.Count == 10, "With no more observers, all data should remain in buffer");

                    Assert.True(observable == b.AsObservable(), "New observable is the same as the old");

                    using (observable.Subscribe(t1.AsObserver()))
                    {
                        Assert.True(
                            SpinWait.SpinUntil(() => t1.Count == 10, 4000),
                            "New observer should get all of the data");
                        for (int i = 0; i < 10; i++)
                        {
                            Assert.True(t1.Receive() == i, "Expected only observer to have " + i);
                        }
                    }
                }

                Assert.True(localPassed, string.Format("{0}: All observers get copies of the data", localPassed ? "Success" : "Failure"));
            }

            // Test that creating observables does not leak
            {
                bool localPassed = true;

                long startMem = GC.GetTotalMemory(true);
                for (int i = 0; i < 2; i++)
                {
                    var bb = new BufferBlock<int>();
                    var observable = bb.AsObservable();
                    bb = null;
                    observable = null;
                    GC.Collect(0);
                }
                long endMem = GC.GetTotalMemory(true);
                long diff = endMem - startMem;
                Assert.True(diff < 40000, // arbitrary limit... can be dialed back if the test starts failing
                    string.Format("Expected 0 byte growth but found {0} bytes leaked", diff));

                Assert.True(localPassed, string.Format("{0}: AsObservable doesn't leak", localPassed ? "Success" : "Failure"));
            }

            // Test broadcasting with back pressure
            {
                const int WAIT_LONG = 5000;
                const int WAIT_SHORT = 1000;
                bool localPassed = true;

                // Create plain blocks
                var source = new BufferBlock<int>();
                var target1 = new BufferBlock<int>();
                var target2 = new BufferBlock<int>(new DataflowBlockOptions() { BoundedCapacity = 1 }); // This target will apply back pressure
                var target3 = new BufferBlock<int>();

                // Link the observable to the observers
                var observable = source.AsObservable();
                observable.Subscribe(target1.AsObserver());
                observable.Subscribe(target2.AsObserver());
                observable.Subscribe(target3.AsObserver());

                // Post first message.
                // Since there is no pressure yet, all targets should accept it.
                source.Post(1);
                var tasks = new Task[] { target1.ReceiveAsync(), target2.OutputAvailableAsync(), target3.ReceiveAsync() }; // Clear target1 and target3
                localPassed &= Task.WaitAll(tasks, WAIT_LONG);
                Assert.True(localPassed, "Failure: Not all targets accepted first message");

                // Post second message.
                // target2 will postpone, but target1 and target3 should accept it.
                source.Post(2);
                tasks = new Task[] { target1.ReceiveAsync(), target3.ReceiveAsync() };
                localPassed &= Task.WaitAll(tasks, WAIT_LONG);
                Assert.True(localPassed, "Failure: Not all unbounded targets accepted second message");

                // Post third message.
                // No target should be offered the message, because the source should be waiting for target2 to accept second message.
                source.Post(3);
                tasks = new Task[] { target1.OutputAvailableAsync(), target3.OutputAvailableAsync() };
                localPassed &= localPassed &= Task.WaitAny(tasks, WAIT_SHORT) == -1;
                Assert.True(localPassed, "Failure: The source propagated third message while it should have waited");

                // Unblock target2 to let third message propagate.
                // Then all targets should end up with a message.
                target2.ReceiveAsync();
                tasks = new Task[] { target1.ReceiveAsync(), target2.ReceiveAsync(), target3.ReceiveAsync() }; // Clear all targets
                localPassed &= Task.WaitAll(tasks, WAIT_LONG);
                Assert.True(localPassed, "Failure: Not all targets received third message after releasing back pressure");
                target2.ReceiveAsync(); // Clear the message left in target2

                // Complete the source which should complete all the targets
                source.Complete();
                tasks = new Task[] { source.Completion, target1.Completion, target2.Completion, target3.Completion };
                localPassed &= Task.WaitAll(tasks, WAIT_LONG);
                Assert.True(localPassed, "Failure: Not all blocks completed");
                localPassed &= source.Completion.Status == TaskStatus.RanToCompletion &&
                               target1.Completion.Status == TaskStatus.RanToCompletion &&
                               target2.Completion.Status == TaskStatus.RanToCompletion &&
                               target3.Completion.Status == TaskStatus.RanToCompletion;
                Assert.True(localPassed, "Failure: Not all blocks completed in RanToCompletion status");

                Assert.True(localPassed, string.Format("{0}: Broadcasting with back pressure", localPassed ? "Success" : "Failure"));
            }

            // Test broadcasting to a faulty target
            {
                bool localPassed = true;

                // Create plain blocks
                var source = new BufferBlock<int>();
                var target1 = new BufferBlock<int>();
                var target2 = new ThrowerTarget<int>();
                var target3 = new BufferBlock<int>();

                // Link the observable to the observers
                var observable = source.AsObservable();
                observable.Subscribe(target1.AsObserver());
                observable.Subscribe(target2.AsObserver());
                observable.Subscribe(target3.AsObserver());

                // Post a message.
                // target1 and target3 should accept it but there is a race with the source faulting them.
                // So the outcome is non-deterministic. We won't check it.
                source.Post(1);

                // target1 and target3 should be completed in a Faulted state
                Assert.Throws<AggregateException>(() => target1.Completion.Wait());
                Assert.Throws<AggregateException>(() => target3.Completion.Wait());
                Assert.True(localPassed, "Failure: Not all unbounded targets completed in Faulted state");

                Assert.True(localPassed, string.Format("{0}: Broadcasting to a faulty target", localPassed ? "Success" : "Failure"));
            }
        }

        [Fact]
        public void TestLinkTo1()
        {
            // Test LinkTo(target, predicate) with two-phase commit
            {
                bool localPassed = true;
                var source1 = new BufferBlock<int>();
                var source2 = new BufferBlock<int>();
                var jb = new JoinBlock<int, int>(new GroupingDataflowBlockOptions { Greedy = false, MaxNumberOfGroups = 1 });
                source1.Completion.ContinueWith(_ => jb.Target1.Complete());
                source2.Completion.ContinueWith(_ => jb.Target2.Complete());
                source1.LinkTo(jb.Target1, i => true);
                source2.LinkTo(jb.Target2, i => true);
                source1.Post(42);
                source2.Post(43);
                source1.Complete();
                source2.Complete();
                var tuple = jb.Receive();
                localPassed &= tuple.Item1 == 42;
                localPassed &= tuple.Item2 == 43;
                Assert.True(localPassed, string.Format("{0}: LinkTo(target, predicate) to non-greedy join", localPassed ? "Success" : "Failure"));
            }

            // Test LinkTo with double-linking
            {
                bool localPassed = true;
                var source1 = new BufferBlock<int>();
                var source2 = new BufferBlock<int>();
                var jb = new JoinBlock<int, int>(new GroupingDataflowBlockOptions { MaxNumberOfGroups = 1, Greedy = false });
                source1.Completion.ContinueWith(_ => jb.Target1.Complete());
                source2.Completion.ContinueWith(_ => jb.Target2.Complete());

                using (source1.LinkTo(jb.Target1))
                {
                    source1.LinkTo(jb.Target1); // force NopLinkPropagator creation
                }
                using (source2.LinkTo(jb.Target2))
                {
                    source2.LinkTo(jb.Target2); // force NopLinkPropagator creation
                }

                source1.Post(42);
                source2.Post(43);
                source1.Complete();
                source2.Complete();

                try
                {
                    var tuple = jb.Receive();
                    localPassed &= tuple.Item1 == 42;
                    localPassed &= tuple.Item2 == 43;
                }
                catch (Exception exc)
                {
                    Assert.True(localPassed, string.Format("Exception {0}", exc));
                    localPassed &= false;
                }

                Assert.True(localPassed, string.Format("{0}: LinkTo(target) with double linking to join", localPassed ? "Success" : "Failure"));
            }
        }

        [Fact]
        public void TestLinkTo2()
        {
            // Test parameter validation
            {
                var source = new BufferBlock<int>();
                var target = new ActionBlock<int>(i => { });
                Assert.Throws<ArgumentNullException>(() => source.LinkTo(null));
                Assert.Throws<ArgumentNullException>(() => ((IPropagatorBlock<int, int>)null).LinkTo(target));
                Assert.Throws<ArgumentNullException>(() => source.LinkTo(null, i => true));
                Assert.Throws<ArgumentNullException>(() => source.LinkTo(null, new DataflowLinkOptions(), i => true));
                Assert.Throws<ArgumentNullException>(() => source.LinkTo(target, null));
                Assert.Throws<ArgumentNullException>(() => source.LinkTo(target, new DataflowLinkOptions(), null));
                Assert.Throws<ArgumentNullException>(() => ((IPropagatorBlock<int, int>)null).LinkTo(null, new DataflowLinkOptions(), null));
                Assert.Throws<ArgumentNullException>(() => source.LinkTo(target, null, i => true));
            }

            // Test LinkTo(target)
            {
                bool localPassed = true;
                int counter = 0;
                var source = new BufferBlock<int>();
                var target1 = new ActionBlock<int>(i => counter++);
                using (source.LinkTo(target1))
                {
                    for (int i = 0; i < 2; i++) source.Post(i);
                    source.Complete();
                    source.Completion.ContinueWith(delegate { target1.Complete(); });
                    target1.Completion.Wait();
                }
                localPassed &= counter == 2;
                Assert.True(localPassed, string.Format("{0}: LinkTo(target)", localPassed ? "Success" : "Failure"));
            }

            // Test LinkTo(target, predicate) /w NullTarget
            {
                bool localPassed = true;
                int counter = 0;
                var source = new BufferBlock<int>();
                var target1 = new ActionBlock<int>(i => counter++);
                using (source.LinkTo(target1, i => i % 2 == 0))
                {
                    source.LinkTo(DataflowBlock.NullTarget<int>());
                    for (int i = 0; i < 2; i++) source.Post(i);
                    source.Complete();
                    source.Completion.ContinueWith(delegate { target1.Complete(); });
                    target1.Completion.Wait();
                }
                localPassed &= counter == 1;
                Assert.True(localPassed, string.Format("{0}: LinkTo(target, predicate) /w NullTarget", localPassed ? "Success" : "Failure"));
            }

            // Test LinkTo(target, predicate) w/o NullTarget
            {
                bool localPassed = true;
                int counter = 0;
                int dumpedCounter = 0;
                var source = new BufferBlock<int>();
                var target1 = new ActionBlock<int>(i => counter++);
                var target2 = new ActionBlock<int>(i => dumpedCounter++);
                using (source.LinkTo(target1, i => i % 2 == 0))
                using (source.LinkTo(target2))
                {
                    for (int i = 0; i < 2; i++) source.Post(i);
                    source.Complete();
                    source.Completion.ContinueWith(delegate
                    {
                        target1.Complete();
                        target2.Complete();
                    });
                    target1.Completion.Wait();
                    target2.Completion.Wait();
                    localPassed &= counter == 1;
                    localPassed &= dumpedCounter == 1;
                }
                Assert.True(localPassed, string.Format("{0}: LinkTo(target, predicate) w/o NullTarget", localPassed ? "Success" : "Failure"));
            }

            

            // Test FilteredLinkPropagator from a different target
            {
                bool localPassed = true;
                var source = new BufferBlock<int>();
                var target = new DelayedTargetForSendAsync<int>(DelayedTargetForSendAsyncMode.MultipleReserveReleaseBeforeConsume, new BufferBlock<int>());

                source.LinkTo(target, x => true); // FilteredLinkPropagator injected here

                source.Post(42);
                target.Completion.Wait();

                Assert.True(localPassed, string.Format("{0}: FilteredLinkPropagator from a different target", localPassed ? "Success" : "Failure"));
            }

            // Test FilteredLinkPropagator.Completion
            {
                bool localPassed = true;
                var source = new BufferBlock<int>();
                var target = new DelayedTargetForSendAsync<int>(DelayedTargetForSendAsyncMode.DeclineForMultipleLinking);
                var sweep = new ActionBlock<int>(x => { });

                source.LinkTo(target, x => true); // FilteredLinkPropagator injected here
                source.LinkTo(sweep);

                source.Post(42);
                source.Complete();
                source.Completion.Wait();

                Assert.True(localPassed, string.Format("{0}: FilteredLinkPropagator.Completion", localPassed ? "Success" : "Failure"));
            }

            // Test NopLinkPropagator from a different target
            {
                bool localPassed = true;
                var source = new BufferBlock<int>();
                var target = new DelayedTargetForSendAsync<int>(DelayedTargetForSendAsyncMode.MultipleReserveReleaseBeforeConsume, new BufferBlock<int>());

                var unlinker = source.LinkTo(target);
                source.LinkTo(target); // NopLinkPropagator injected here
                unlinker.Dispose(); // Unlink the original

                source.Post(42);
                target.Completion.Wait();

                Assert.True(localPassed, string.Format("{0}: NopLinkPropagator from a different target", localPassed ? "Success" : "Failure"));
            }

            // Test NopLinkPropagator.Completion
            {
                bool localPassed = true;
                var source = new BufferBlock<int>();
                var target = new DelayedTargetForSendAsync<int>(DelayedTargetForSendAsyncMode.DeclineForMultipleLinking);
                var sweep = new ActionBlock<int>(x => { });

                source.LinkTo(target);
                source.LinkTo(target); // NopLinkPropagator injected here
                source.LinkTo(sweep);

                source.Post(42);
                source.Complete();
                source.Completion.Wait();

                Assert.True(localPassed, string.Format("{0}: NopLinkPropagator.Completion", localPassed ? "Success" : "Failure"));
            }
        }

        //[Fact(Skip = "Outerloop")]
        public void TestLinkTo3()
        {
            // Test DataflowLinkOptions.MaxMessages
            {
                const int LONG_WAIT = 5000;

                const int MAX_MESSAGES = 2;
                const int EXTRA_MESSAGES = 1;

                const string SCENARIO_DIRECT = "Direct";
                const string SCENARIO_FILTER = "Filter";
                const string SCENARIO_DOUBLE = "Double";
                var scenarios = new string[] { SCENARIO_DIRECT, SCENARIO_FILTER, SCENARIO_DOUBLE };

                foreach (var scenario in scenarios)
                {
                    int consumedMessages = 0;
                    int extraMessages = 0;

                    var linkOpt = new DataflowLinkOptions() { MaxMessages = MAX_MESSAGES };
                    var source = new BufferBlock<int>();
                    var target = new ActionBlock<int>(x => Interlocked.Increment(ref consumedMessages));
                    var otherTarget = new ActionBlock<int>(x => Interlocked.Increment(ref extraMessages));

                    // Implement the link scenario
                    switch (scenario)
                    {
                        case SCENARIO_DIRECT:
                            source.LinkTo(target, linkOpt);
                            source.LinkTo(otherTarget);
                            break;

                        case SCENARIO_FILTER:
                            source.LinkTo(target, linkOpt, x => true); // Injects FilteredLinkPropagator
                            source.LinkTo(otherTarget);
                            break;

                        case SCENARIO_DOUBLE:
                            using (source.LinkTo(target))
                            {
                                source.LinkTo(target, linkOpt); // Injects NopLinkPropagator
                            }
                            source.LinkTo(otherTarget);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException("scenario", scenario);
                    }

                    // Post the exact number of messages
                    for (int i = 0; i < MAX_MESSAGES + EXTRA_MESSAGES; i++)
                    {
                        source.Post(i);
                    }

                    // Let the messages settle into the targets
                    source.Complete();
                    var sourceCompleted = source.Completion.Wait(LONG_WAIT);
                    target.Complete();
                    var targetCompleted = target.Completion.Wait(LONG_WAIT);
                    otherTarget.Complete();
                    var otherTargetCompleted = otherTarget.Completion.Wait(LONG_WAIT);

                    // Verify result
                    var localPassed = sourceCompleted && targetCompleted && otherTargetCompleted
                                        && consumedMessages == MAX_MESSAGES && extraMessages == EXTRA_MESSAGES;
                    var diagnostics = localPassed ? String.Empty :
                                            string.Format("(sourceCompleted={0}, targetCompleted={1} ({2}), otherTargetCompleted={3} ({4})",
                                                           sourceCompleted, targetCompleted, consumedMessages, otherTargetCompleted, extraMessages);

                    // Log result
                    Assert.True(localPassed, string.Format("{0}: MaxMessages {1} {2}",
                        localPassed ? "Success" : "Failure", scenario, diagnostics));
                }
            }

            // Test DataflowLinkOptions.Append
            {
                const int LONG_WAIT = 5000;

                const int TARGETS = 6;
                int[] consumedMessages = new int[TARGETS];
                int lostMessages = 0;

                var append = new DataflowLinkOptions() { Append = true };
                var prepend = new DataflowLinkOptions() { Append = false };
                var source = new BufferBlock<int>();
                var targets = new ActionBlock<int>[TARGETS];

                targets[0] = new ActionBlock<int>(x => Interlocked.Increment(ref consumedMessages[0]));
                targets[1] = new ActionBlock<int>(x => Interlocked.Increment(ref consumedMessages[1]));
                targets[2] = new ActionBlock<int>(x => Interlocked.Increment(ref consumedMessages[2]));
                targets[3] = new ActionBlock<int>(x => Interlocked.Increment(ref consumedMessages[3]));
                targets[4] = new ActionBlock<int>(x => Interlocked.Increment(ref consumedMessages[4]));
                targets[5] = new ActionBlock<int>(x => Interlocked.Increment(ref consumedMessages[5]));
                var extraTarget = new ActionBlock<int>(x => Interlocked.Increment(ref lostMessages));

                // Link targets in a semi-random order but make sure the final order is as expected
                source.LinkTo(targets[2], prepend, x => x <= 2);
                source.LinkTo(targets[3], append, x => x <= 3);
                var unlink1 = source.LinkTo(extraTarget, prepend);
                source.LinkTo(targets[4], append, x => x <= 4);
                source.LinkTo(targets[1], prepend, x => x <= 1);
                var unlink2 = source.LinkTo(extraTarget, append);
                source.LinkTo(targets[0], prepend, x => x <= 0);
                source.LinkTo(targets[5], append, x => x <= 5);
                var unlink3 = source.LinkTo(extraTarget, prepend);

                // Unlink the extras
                unlink1.Dispose();
                unlink2.Dispose();
                unlink3.Dispose();

                // Post the exact number of messages
                for (int i = 0; i < TARGETS; i++)
                {
                    source.Post(i);
                }

                // Complete the source
                source.Complete();
                var sourceCompleted = source.Completion.Wait(LONG_WAIT);
                var diagnostics = new StringBuilder();
                if (!sourceCompleted) diagnostics.Append("(sourceCompleted=False, ");

                // Complete the targets and verify result
                var localPassed = sourceCompleted;
                for (int i = 0; i < TARGETS; i++)
                {
                    targets[i].Complete();
                    var targetCompleted = targets[i].Completion.Wait(LONG_WAIT);
                    if (!targetCompleted) diagnostics.AppendFormat("[{0}].Completed=False, ", i);
                    if (consumedMessages[i] != 1) diagnostics.AppendFormat("[{0}].Consumed={1}, ", i, consumedMessages[i]);
                    localPassed &= targetCompleted && consumedMessages[i] == 1;
                }

                // Log result
                Assert.True(localPassed, string.Format("{0}: Append {1}", localPassed ? "Success" : "Failure", diagnostics));
            }

            // Test DataflowLinkOptions.PropagateCompletion
            {
                const int LONG_WAIT = 5000;
                const int SHORT_WAIT = 1000;

                const string METHOD_COMPLETE = "Complete";
                const string METHOD_CANCEL = "Cancel";
                const string METHOD_FAULT = "Fault";
                var completionMethods = new string[] { METHOD_COMPLETE, METHOD_CANCEL, METHOD_FAULT };

                Func<Task, int, bool> waitFor = (task, timeout) =>
                                                {
                                                    bool completed = false;

                                                    try
                                                    {
                                                        completed = task.Wait(timeout);
                                                    }
                                                    catch (AggregateException aggregate)
                                                    {
                                                        aggregate.Handle(inner =>
                                                                        {
                                                                            completed = inner is TaskCanceledException ||
                                                                                        inner is FormatException ||
                                                                                        inner is AggregateException;
                                                                            return completed;
                                                                        });
                                                    }

                                                    return completed;
                                                };

                foreach (var completionMethod in completionMethods)
                {
                    var cts1 = new CancellationTokenSource();
                    var linkOpt = new DataflowLinkOptions() { PropagateCompletion = true };

                    // Sources: SourceCore (e.g. BufferBlock), BroadcastBlock, WriteOnceBlock
                    var source = new BufferBlock<int>(new DataflowBlockOptions { CancellationToken = cts1.Token }) as IPropagatorBlock<int, int>;

                    bool sourceCompleted = false;
                    Action triggerSourceCompletion = () =>
                        {
                            switch (completionMethod)
                            {
                                case METHOD_COMPLETE:
                                    source.Complete();
                                    break;

                                case METHOD_CANCEL:
                                    if (!cts1.IsCancellationRequested) cts1.Cancel();
                                    break;

                                case METHOD_FAULT:
                                    source.Fault(new FormatException("Faulted"));
                                    break;

                                default:
                                    throw new ArgumentOutOfRangeException("completionMethod", completionMethod);
                            }
                        };

                    triggerSourceCompletion();
                    sourceCompleted = waitFor(source.Completion, LONG_WAIT);

                    var target = new BufferBlock<int>();
                    var otherTarget = new BufferBlock<int>();

                    source.LinkTo(otherTarget);
                    source.LinkTo(target, linkOpt);

                    // Verify targets' states
                    var targetCompleted = waitFor(target.Completion, LONG_WAIT);
                    var otherTargetDidNotComplete = !waitFor(otherTarget.Completion, SHORT_WAIT);

                    var localPassed = sourceCompleted && targetCompleted && otherTargetDidNotComplete;
                    var diagnostics = localPassed ? String.Empty : string.Format("(sourceCompleted={0}, targetCompleted={1}, otherTargetDidNotComplete={2}",
                                                                                    sourceCompleted, targetCompleted, otherTargetDidNotComplete);

                    // Log result
                    Assert.True(localPassed, string.Format("{0}: PropagateCompletion {1} {2}",
                        localPassed ? "Success" : "Failure", completionMethod, diagnostics));
                }
            }
        }

        //[Fact(Skip = "Outerloop")]
        public void TestSendAsync()
        {
            // Test argument validation
            {
                bool localPassed = true;
                Assert.Throws<ArgumentNullException>(() => ((ITargetBlock<int>)null).SendAsync(42));
                Assert.True(localPassed, string.Format("{0}: Argument validation", localPassed ? "Success" : "Failure"));
            }

            // ImmediateAcceptance
            {
                bool localPassed = true;
                var dtfsa = new DelayedTargetForSendAsync<int>(DelayedTargetForSendAsyncMode.ImmediateAcceptance);
                var t = dtfsa.SendAsync(42);
                localPassed &= t.Result == true;
                Assert.True(localPassed, string.Format("{0}: ImmediateAcceptance", localPassed ? "Success" : "Failure"));
            }

            // ImmediateDecline
            {
                bool localPassed = true;
                var dtfsa = new DelayedTargetForSendAsync<int>(DelayedTargetForSendAsyncMode.ImmediateDecline);
                var t = dtfsa.SendAsync(42);
                localPassed &= t.Result == false;
                Assert.True(localPassed, string.Format("{0}: ImmediateDecline", localPassed ? "Success" : "Failure"));
            }

            // DelayedConsume
            {
                bool localPassed = true;
                var dtfsa = new DelayedTargetForSendAsync<int>(DelayedTargetForSendAsyncMode.DelayedConsume);
                var t = dtfsa.SendAsync(42);
                localPassed &= !t.IsCompleted;
                localPassed &= t.Result == true;
                Assert.True(localPassed, string.Format("{0}: DelayedConsume", localPassed ? "Success" : "Failure"));
            }

            // MultipleReserveReleaseBeforeConsume as a different target
            {
                bool localPassed = true;
                var dtfsa = new DelayedTargetForSendAsync<int>(DelayedTargetForSendAsyncMode.MultipleReserveReleaseBeforeConsume, new BufferBlock<int>());
                var t = dtfsa.SendAsync(42);
                localPassed &= !t.IsCompleted;
                localPassed &= t.Result == true;
                Assert.True(localPassed, string.Format("{0}: MultipleReserveReleaseBeforeConsume as a different target", localPassed ? "Success" : "Failure"));
            }

            // Drop
            {
                bool localPassed = true;
                var dtfsa = new DelayedTargetForSendAsync<int>(DelayedTargetForSendAsyncMode.Drop);
                var t = dtfsa.SendAsync(42);
                while (!t.IsCompleted)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    Task.Delay(1).Wait();
                }
                localPassed &= t.Result == false;
                Assert.True(localPassed, string.Format("{0}: Drop", localPassed ? "Success" : "Failure"));
            }


            // Pre-canceled
            {
                bool localPassed = true;
                var dtfsa = new DelayedTargetForSendAsync<int>(DelayedTargetForSendAsyncMode.ImmediateAcceptance);
                var ct = new CancellationToken(true);
                var t = dtfsa.SendAsync(42, ct);
                localPassed &= t.IsCanceled;
                localPassed &= !dtfsa.Completion.IsCompleted;
                Assert.True(localPassed, string.Format("{0}: Pre-canceled", localPassed ? "Success" : "Failure"));
            }

            // Canceled after SendAsync call but before consumption
            {
                bool localPassed = true;
                var dtfsa = new DelayedTargetForSendAsync<int>(DelayedTargetForSendAsyncMode.AcceptOnTriggerCall);
                var cts = new CancellationTokenSource();
                var t = dtfsa.SendAsync(42, cts.Token);
                localPassed &= !t.IsCompleted;
                localPassed &= !dtfsa.Completion.IsCompleted;
                cts.Cancel();
                dtfsa.TriggerAccept();
                ((IAsyncResult)t).AsyncWaitHandle.WaitOne();
                localPassed &= t.Status == TaskStatus.Canceled;
                ((IAsyncResult)dtfsa.Completion).AsyncWaitHandle.WaitOne();
                localPassed &= dtfsa.Completion.Status == TaskStatus.Canceled;
                Assert.True(localPassed, string.Format("{0}: Canceled after SendAsync", localPassed ? "Success" : "Failure"));
            }

            // Canceled during multiple reserve/release before acceptance
            {
                bool localPassed = true;
                var dtfsa = new DelayedTargetForSendAsync<int>(DelayedTargetForSendAsyncMode.MultipleReserveReleaseBeforeAcceptOnTriggerCall);
                var cts = new CancellationTokenSource();
                var t = dtfsa.SendAsync(42, cts.Token);
                localPassed &= !t.IsCompleted;
                localPassed &= !dtfsa.Completion.IsCompleted;
                Task.Delay(1).Wait(); // must be much less than timeout used in DelayedTargetForSendAsyncMode
                cts.Cancel();
                dtfsa.TriggerAccept();
                ((IAsyncResult)t).AsyncWaitHandle.WaitOne();
                localPassed &= t.Status == TaskStatus.Canceled;
                ((IAsyncResult)dtfsa.Completion).AsyncWaitHandle.WaitOne();
                localPassed &= dtfsa.Completion.Status == TaskStatus.Canceled;
                Assert.True(localPassed, string.Format("{0}: Canceled during reserve/release cycle", localPassed ? "Success" : "Failure"));
            }
        }

        //[Fact(Skip = "Outerloop")]
        public void TestReceive()
        {
            // Test argument validation
            {
                bool localPassed = true;
                var buffer = new BufferBlock<int>();
                Assert.Throws<ArgumentNullException>(() => ((IReceivableSourceBlock<int>)null).Receive());
                Assert.True(localPassed, string.Format("{0}: Argument validation", localPassed ? "Success" : "Failure"));
            }

            // Test to make sure Receive only removes one available item and the correct one
            {
                bool localPassed = true;
                var buffer = new BufferBlock<int>();
                buffer.Post(1);
                buffer.Post(2);
                localPassed &= buffer.Receive() == 1;
                Assert.True(localPassed, string.Format("{0}: Receive returns correct item", localPassed ? "Success" : "Failure"));
                localPassed &= buffer.Count == 1;
                Assert.True(localPassed, string.Format("{0}: Receive removes a single, correct item", localPassed ? "Success" : "Failure"));
            }

            // Test to make sure Receive only removes one not-yet-available item and the correct one
            {
                bool localPassed = true;
                var buffer = new BufferBlock<int>();
                var t = Task.Factory.StartNew(() =>
                {
                    Task.Delay(1).Wait();
                    buffer.Post(1);
                    buffer.Post(2);
                });
                localPassed &= buffer.Receive() == 1;
                Assert.True(localPassed, string.Format("{0}: Receive returns correct item", localPassed ? "Success" : "Failure"));
                t.Wait();
                localPassed &= buffer.Count == 1;
                Assert.True(localPassed, string.Format("{0}: Receive removes a single, not-yet-available and correct item (Count={1})", localPassed ? "Success" : "Failure", buffer.Count));
            }

            // Test Receive with timeout
            {
                bool localPassed = true;
                var buffer = new BufferBlock<int>();
                Assert.Throws<TimeoutException>(() => buffer.Receive(TimeSpan.FromMilliseconds(0)));
                buffer.Post(42);
                localPassed &= buffer.Receive(TimeSpan.FromMilliseconds(-1)) == 42;
                Task.Factory.StartNew(() => { Task.Delay(50).Wait(); buffer.Post(44); });
                Assert.Throws<TimeoutException>(() => buffer.Receive(TimeSpan.FromMilliseconds(1)));
                Assert.True(localPassed, string.Format("{0}: Receive with timeout", localPassed ? "Success" : "Failure"));
            }

            // Test Receive with cancellation
            {
                bool localPassed = true;
                var precanceledToken = new CancellationToken(true);

                // Canceling the source with an already canceled token
                {
                    var buffer = new BufferBlock<int>(new DataflowBlockOptions { CancellationToken = precanceledToken });
                    Assert.Throws<InvalidOperationException>(() => buffer.Receive());
                }

                // Canceling the source
                {
                    var cts = new CancellationTokenSource();
                    var buffer = new BufferBlock<int>(new DataflowBlockOptions { CancellationToken = cts.Token });
                    Task.Factory.StartNew(() => { Task.Delay(1).Wait(); cts.Cancel(); });
                    Assert.Throws<InvalidOperationException>(() => buffer.Receive());
                }

                // Canceling the receive with an already canceled token
                {
                    var buffer = new BufferBlock<int>();
                    Assert.Throws<OperationCanceledException>(() => buffer.Receive(precanceledToken));
                }

                // Canceling the source after the receive completes
                {
                    var buffer = new BufferBlock<int>();
                    var cts = new CancellationTokenSource();
                    buffer = new BufferBlock<int>(new DataflowBlockOptions { CancellationToken = cts.Token });
                    buffer.Post(42);
                    localPassed &= buffer.Receive(cts.Token) == 42;
                    cts.Cancel();
                    localPassed &= buffer.Post(42) == false;
                    Assert.Throws<InvalidOperationException>(() => buffer.Receive());
                }

                // Canceling the receive before it gets data
                {
                    var buffer = new BufferBlock<int>();
                    var cts = new CancellationTokenSource();
                    Task.Factory.StartNew(() => { Task.Delay(1).Wait(); cts.Cancel(); });
                    Assert.Throws<OperationCanceledException>(() => buffer.Receive(cts.Token));
                }

                Assert.True(localPassed, string.Format("{0}: Receive with cancellation", localPassed ? "Success" : "Failure"));
            }
        }

        private static Type GetInnerException(Action action)
        {
            try
            {
                action();
            }
            catch (AggregateException ex)
            {
                return ex.InnerException.GetType();
            }

            return null;
        }

        //[Fact(Skip = "Outerloop")]
        public void TestReceiveAsync()
        {
            // Test argument validation
            {
                bool localPassed = true;
                var buffer = new BufferBlock<int>();
                Assert.Throws<ArgumentNullException>(() => ((IReceivableSourceBlock<int>)null).ReceiveAsync());
                Assert.True(localPassed, string.Format("{0}: Argument validation", localPassed ? "Success" : "Failure"));
            }

            // Test ReceiveAsync with data already available
            {
                bool localPassed = true;
                int counter = 0;
                var buffer = new BufferBlock<int>();
                for (int i = 0; i < 1; i++) { counter += i; buffer.Post(i); }
                buffer.Complete();
                Task.Delay(1).Wait();
                for (int i = 0; i < 1; i++) { counter -= buffer.ReceiveAsync().Result; }
                buffer.Completion.Wait();
                localPassed &= counter == 0;
                Assert.True(localPassed, string.Format("{0}: ReceiveAsync with data already available", localPassed ? "Success" : "Failure"));
            }

            // Test to make sure ReceiveAsync only removes one available item and the correct one
            {
                bool localPassed = true;
                var buffer = new BufferBlock<int>();
                buffer.Post(1);
                buffer.Post(2);
                localPassed &= buffer.ReceiveAsync().Result == 1;
                localPassed &= buffer.Count == 1;
                Assert.True(localPassed, string.Format("{0}: ReceiveAsync removes a single, correct item", localPassed ? "Success" : "Failure"));
            }

            // Test to make sure ReceiveAsync only removes one not-yet-available item and the correct one
            {
                bool localPassed = true;
                var buffer = new BufferBlock<int>();
                var posts = Task.Factory.StartNew(() =>
                {
                    Task.Delay(1).Wait();
                    buffer.Post(1);
                    buffer.Post(2);
                });
                var t = buffer.ReceiveAsync();
                posts.Wait();
                Task.Delay(1).Wait();
                localPassed &= t.Result == 1;
                localPassed &= buffer.Count == 1;
                Assert.True(localPassed, string.Format("{0}: ReceiveAsync a single, not-yet-available and correct item", localPassed ? "Success" : "Failure"));
            }

            // Test ReceiveAsync with no data available
            {
                bool localPassed = true;
                var buffer = new BufferBlock<int>();
                var t = buffer.ReceiveAsync();
                Task.Delay(1).Wait();
                localPassed &= !t.IsCompleted;
                Assert.True(localPassed, string.Format("{0}: ReceiveAsync with no data available", localPassed ? "Success" : "Failure"));
            }

            // Test ReceiveAsync before data available
            {
                bool localPassed = true;
                int counter = 0;
                var buffer = new BufferBlock<int>();
                for (int i = 0; i < 1; i++)
                {
                    counter += i;
                    Task.Factory.StartNew(state => { Task.Delay(1).Wait(); buffer.Post((int)state); }, i);
                    counter -= buffer.ReceiveAsync().Result;
                }
                localPassed &= counter == 0;
                Assert.True(localPassed, string.Format("{0}: ReceiveAsync before data available", localPassed ? "Success" : "Failure"));
            }

            // Test ReceiveAsync with timeout
            {
                bool localPassed = true;
                var buffer = new BufferBlock<int>();
                try
                {
                    buffer.ReceiveAsync(TimeSpan.FromMilliseconds(0)).Wait();
                }
                catch (AggregateException ex)
                {
                    if (!ex.InnerException.GetType().Equals(typeof(TimeoutException)))
                    {
                        Assert.False(true, "Did not throw Timeout Exception.");
                    }
                }
                buffer.Post(42);
                localPassed &= buffer.ReceiveAsync(TimeSpan.FromMilliseconds(-1)).Result == 42;
                Task.Factory.StartNew(() => { Task.Delay(2).Wait(); buffer.Post(45); });
                try
                {
                    buffer.ReceiveAsync(TimeSpan.FromMilliseconds(1)).Wait();
                }
                catch (AggregateException ex)
                {
                    if (!ex.InnerException.GetType().Equals(typeof(TimeoutException)))
                    {
                        Assert.False(true, "Did not throw TimeoutException.");
                    }
                }

                Assert.True(localPassed, string.Format("{0}: ReceiveAsync with timeout", localPassed ? "Success" : "Failure"));
            }

            // Test ReceiveAsync with cancellation
            {
                bool localPassed = true;
                var precanceledToken = new CancellationToken(true);

                // Canceling the source with an already canceled token
                {
                    var buffer = new BufferBlock<int>(new DataflowBlockOptions { CancellationToken = precanceledToken });
                    try
                    {
                        buffer.ReceiveAsync().Wait();
                    }
                    catch (AggregateException ex)
                    {
                        if (!ex.InnerException.GetType().Equals(typeof(InvalidOperationException)))
                        {
                            Assert.False(true, "Did not throw InvalidOperationException.");
                        }
                    }
                }

                // Canceling the source
                {
                    var cts = new CancellationTokenSource();
                    var buffer = new BufferBlock<int>(new DataflowBlockOptions { CancellationToken = cts.Token });
                    var t = buffer.ReceiveAsync();
                    cts.Cancel();
                    try
                    {
                        t.Wait();
                    }
                    catch (AggregateException ex)
                    {
                        if (!ex.InnerException.GetType().Equals(typeof(InvalidOperationException)))
                        {
                            Assert.False(true, "Did not throw InvalidOperationException.");
                        }
                    }
                }

                // Canceling the receive with an already canceled token
                {
                    var buffer = new BufferBlock<int>();
                    try
                    {
                        buffer.ReceiveAsync(precanceledToken).Wait();
                    }
                    catch (AggregateException ex)
                    {
                        if (!ex.InnerException.GetType().Equals(typeof(TaskCanceledException)))
                        {
                            Assert.False(true, "Did not throw TaskCanceledException.");
                        }
                    }
                }

                // Canceling the source after the receive completes
                {
                    var buffer = new BufferBlock<int>();
                    var cts = new CancellationTokenSource();
                    buffer = new BufferBlock<int>(new DataflowBlockOptions { CancellationToken = cts.Token });
                    buffer.Post(42);
                    localPassed &= buffer.ReceiveAsync(cts.Token).Result == 42;
                    cts.Cancel();
                    localPassed &= buffer.Post(42) == false;
                    try
                    {
                        buffer.ReceiveAsync().Wait();
                    }
                    catch (AggregateException ex)
                    {
                        if (!ex.InnerException.GetType().Equals(typeof(InvalidOperationException)))
                        {
                            Assert.False(true, "Did not throw InvalidOperationException.");
                        }
                    }
                }

                // Canceling the receive before it gets data
                {
                    var buffer = new BufferBlock<int>();
                    var cts = new CancellationTokenSource();
                    var t = buffer.ReceiveAsync(cts.Token);
                    cts.Cancel();
                    try
                    {
                        t.Wait();
                    }
                    catch (AggregateException ex)
                    {
                        if (!ex.InnerException.GetType().Equals(typeof(TaskCanceledException)))
                        {
                            Assert.False(true, "Did not throw TaskCanceledException.");
                        }
                    }
                }

                Assert.True(localPassed, string.Format("{0}: ReceiveAsync with cancellation", localPassed ? "Success" : "Failure"));
            }

            // Test ReceiveAsync in a sequence
            {
                bool localPassed = true;
                for (int i = 0; i < 2; i++)
                {
                    const int ITEMS_TO_POST = 2;
                    bool executeSynchronously = i == 0;
                    var bb = new BufferBlock<int>();
                    for (int v = 0; v < ITEMS_TO_POST; v++) bb.Post(v);
                    using (var mres = new ManualResetEventSlim())
                    {
                        int iter = 0;
                        Action body = null;
                        body = () =>
                        {
                            bb.ReceiveAsync().ContinueWith(receiveTask =>
                            {
                                if (receiveTask.Result != iter)
                                {
                                    localPassed = false;
                                    mres.Set();
                                }
                                else
                                {
                                    iter++;
                                    if (iter < ITEMS_TO_POST) body();
                                    else mres.Set();
                                }
                            }, executeSynchronously ? TaskContinuationOptions.ExecuteSynchronously : TaskContinuationOptions.None);
                        };
                        body();
                        mres.Wait();
                    }
                }

                Assert.True(localPassed, string.Format("{0}: ReceiveAsync in continuation sequence", localPassed ? "Success" : "Failure"));
            }
        }

        //[Fact(Skip = "Outerloop")]
        public void TestChoose2()
        {
            // Argument validation
            {
                Assert.Throws<ArgumentNullException>(
                    () => DataflowBlock.Choose<int, int>(null, i => { }, new BufferBlock<int>(), i => { }));
                Assert.Throws<ArgumentNullException>(
                    () => DataflowBlock.Choose<int, int>(new BufferBlock<int>(), i => { }, null, i => { }));
                Assert.Throws<ArgumentNullException>(
                    () => DataflowBlock.Choose<int, int>(new BufferBlock<int>(), null, new BufferBlock<int>(), i => { }));
                Assert.Throws<ArgumentNullException>(
                    () => DataflowBlock.Choose<int, int>(new BufferBlock<int>(), i => { }, new BufferBlock<int>(), null));
                Assert.Throws<ArgumentNullException>(
                    () => DataflowBlock.Choose<int, int>(new BufferBlock<int>(), i => { }, new BufferBlock<int>(), i => { }, null));
            }

            // Multiple test cases
            {
                for (int chooseTestCase = 0; chooseTestCase < 7; chooseTestCase++)
                {
                    bool localPassed = true;
                    var cesp = new ConcurrentExclusiveSchedulerPair(); // ensure BufferBlocks are serialized
                    var source1 = new BufferBlock<int>(new DataflowBlockOptions { TaskScheduler = cesp.ExclusiveScheduler });
                    var source2 = new BufferBlock<string>(new DataflowBlockOptions { TaskScheduler = cesp.ExclusiveScheduler });
                    int intValue = 0;
                    string stringValue = null;
                    TaskScheduler usedScheduler = null, requestedScheduler = new SimpleTaskScheduler();
                    var cts = new CancellationTokenSource();
                    var t = chooseTestCase < 5 ?
                        DataflowBlock.Choose(
                            source1, i => intValue = i,
                            source2, s => stringValue = s) :
                        DataflowBlock.Choose(
                            source1, i => usedScheduler = TaskScheduler.Current,
                            source2, s => usedScheduler = TaskScheduler.Current,
                            new DataflowBlockOptions { TaskScheduler = requestedScheduler, MaxMessagesPerTask = 1, CancellationToken = cts.Token });

                    switch (chooseTestCase)
                    {
                        case 0: // Test data on the first source before data on the second
                            source1.Post(42);
                            source1.Post(43);
                            Task.Delay(100).Wait();
                            source2.Post("44");
                            source2.Post("45");
                            localPassed &= t.Result == 0;
                            localPassed &= intValue == 42;
                            localPassed &= stringValue == null;
                            localPassed &= source1.Count == 1;
                            localPassed &= source2.Count == 2;
                            Assert.True(localPassed, string.Format("{0}: Data on the first source before data on the second", localPassed ? "Success" : "Failure"));

                            break;

                        case 1: // Test data on the second source before data on the first
                            source2.Post("44");
                            source2.Post("45");
                            Task.Delay(100).Wait();
                            source1.Post(42);
                            source1.Post(43);
                            localPassed &= t.Result == 1;
                            localPassed &= intValue == 0;
                            localPassed &= stringValue == "44";
                            localPassed &= source1.Count == 2;
                            localPassed &= source2.Count == 1;
                            Assert.True(localPassed, string.Format("{0}: Data on the second source before data on the first", localPassed ? "Success" : "Failure"));

                            break;

                        case 2: // Test no data on first source but data on second
                            source1.Complete();
                            source2.Post("42");
                            localPassed &= t.Result == 1;
                            localPassed &= intValue == 0;
                            localPassed &= stringValue == "42";
                            localPassed &= source2.Count == 0;
                            Assert.True(localPassed, string.Format("{0}: No data on first source but data on second", localPassed ? "Success" : "Failure"));

                            break;

                        case 3: // Test no data on second source but data on first
                            source2.Complete();
                            source1.Post(42);
                            localPassed &= t.Result == 0;
                            localPassed &= intValue == 42;
                            localPassed &= stringValue == null;
                            localPassed &= source1.Count == 0;
                            Assert.True(localPassed, string.Format("{0}: No data on second source but data on first", localPassed ? "Success" : "Failure"));

                            break;

                        case 4: // Test no data on either source
                            source1.Complete();
                            source2.Complete();
                            ((IAsyncResult)t).AsyncWaitHandle.WaitOne();
                            localPassed &= t.IsCanceled;
                            localPassed &= intValue == 0;
                            localPassed &= stringValue == null;
                            Assert.True(localPassed, string.Format("{0}: No data on either source", localPassed ? "Success" : "Failure"));

                            break;

                        // >= 5 TEST USING DATAFLOW BLOCK OPTIONS

                        case 5: // Test correct TaskScheduler is used
                            source1.Post(42);
                            t.Wait();
                            localPassed &= usedScheduler == requestedScheduler;
                            Assert.True(localPassed, string.Format("{0}: Correct TaskScheduler is used", localPassed ? "Success" : "Failure"));

                            break;

                        case 6: // Test cancellation takes effect
                            cts.Cancel();
                            source1.Post(42);
                            source2.Post("43");
                            ((IAsyncResult)t).AsyncWaitHandle.WaitOne();
                            localPassed &= t.IsCanceled;
                            localPassed &= source1.Count == 1;
                            localPassed &= source2.Count == 1;
                            Assert.True(localPassed, string.Format("{0}: Cancellation takes effect", localPassed ? "Success" : "Failure"));

                            break;
                    }
                }
            }

            // Exception handling
            {
                bool localPassed = true;

                {
                    var cesp = new ConcurrentExclusiveSchedulerPair(); // ensure BufferBlocks are serialized
                    var source1 = new BufferBlock<int>(new DataflowBlockOptions { TaskScheduler = cesp.ExclusiveScheduler });
                    var source2 = new BufferBlock<string>(new DataflowBlockOptions { TaskScheduler = cesp.ExclusiveScheduler });
                    var t = DataflowBlock.Choose(
                        source1, i => { throw new InvalidOperationException(); },
                        source2, s => { throw new InvalidCastException(); });
                    Assert.True(!t.IsCompleted, "Choose should not yet be completed");
                    source1.Post(42);
                    source2.Post("43");
                    ((IAsyncResult)t).AsyncWaitHandle.WaitOne();
                    Assert.True(t.IsFaulted, "Choose should have faulted");
                    Assert.True(t.Exception.InnerException is InvalidOperationException, "Choose should have faulted with right exception");
                    Assert.True(source1.Count == 0, "First source should not contain its data");
                    Assert.True(source2.Count == 1, "Second source should still contain its data");
                }

                {
                    var cesp = new ConcurrentExclusiveSchedulerPair(); // ensure BufferBlocks are serialized
                    var source1 = new BufferBlock<int>(new DataflowBlockOptions { TaskScheduler = cesp.ExclusiveScheduler });
                    var source2 = new BufferBlock<string>(new DataflowBlockOptions { TaskScheduler = cesp.ExclusiveScheduler });
                    var t = DataflowBlock.Choose(
                        source1, i => { throw new InvalidOperationException(); },
                        source2, s => { throw new InvalidCastException(); });
                    Assert.True(!t.IsCompleted, "Choose should not yet be completed");
                    source2.Post("43");
                    source1.Post(42);
                    ((IAsyncResult)t).AsyncWaitHandle.WaitOne();
                    Assert.True(t.IsFaulted, "Choose should have faulted");
                    Assert.True(t.Exception.InnerException is InvalidCastException, "Choose should have faulted with right exception");
                    Assert.True(source1.Count == 1, "First source should still contain its data");
                    Assert.True(source2.Count == 0, "Second source should not contain its data");
                }

                Assert.True(localPassed, string.Format("{0}: Exception handling", localPassed ? "Success" : "Failure"));
            }
        }

        //[Fact(Skip = "Outerloop")]
        public void TestChoose3()
        {
            // Argument validation
            {
                Assert.Throws<ArgumentNullException>(
                    () => DataflowBlock.Choose<int, int, int>(null, i => { }, new BufferBlock<int>(), i => { }, new BufferBlock<int>(), i => { }));
                Assert.Throws<ArgumentNullException>(
                    () => DataflowBlock.Choose<int, int, int>(new BufferBlock<int>(), i => { }, null, i => { }, new BufferBlock<int>(), i => { }));
                Assert.Throws<ArgumentNullException>(
                    () => DataflowBlock.Choose<int, int, int>(new BufferBlock<int>(), i => { }, new BufferBlock<int>(), i => { }, null, i => { }));
                Assert.Throws<ArgumentNullException>(
                    () => DataflowBlock.Choose<int, int, int>(new BufferBlock<int>(), null, new BufferBlock<int>(), i => { }, new BufferBlock<int>(), i => { }));
                Assert.Throws<ArgumentNullException>(
                    () => DataflowBlock.Choose<int, int, int>(new BufferBlock<int>(), i => { }, new BufferBlock<int>(), null, new BufferBlock<int>(), i => { }));
                Assert.Throws<ArgumentNullException>(
                    () => DataflowBlock.Choose<int, int, int>(new BufferBlock<int>(), i => { }, new BufferBlock<int>(), i => { }, new BufferBlock<int>(), null));
                Assert.Throws<ArgumentNullException>(
                    () => DataflowBlock.Choose<int, int, int>(new BufferBlock<int>(), i => { }, new BufferBlock<int>(), i => { }, new BufferBlock<int>(), i => { }, null));
            }

            // Multiple test cases
            {
                for (int chooseTestCase = 0; chooseTestCase < 9; chooseTestCase++)
                {
                    bool localPassed = true;
                    var cesp = new ConcurrentExclusiveSchedulerPair(); // ensure BufferBlocks are serialized
                    var source1 = new BufferBlock<int>(new DataflowBlockOptions { TaskScheduler = cesp.ExclusiveScheduler });
                    var source2 = new BufferBlock<string>(new DataflowBlockOptions { TaskScheduler = cesp.ExclusiveScheduler });
                    var source3 = new BufferBlock<double>(new DataflowBlockOptions { TaskScheduler = cesp.ExclusiveScheduler });
                    int intValue = 0;
                    string stringValue = null;
                    double doubleValue = 0.0;
                    TaskScheduler usedScheduler = null, requestedScheduler = new SimpleTaskScheduler();
                    var cts = new CancellationTokenSource();
                    var t = chooseTestCase < 7 ?
                        DataflowBlock.Choose(
                            source1, i => intValue = i,
                            source2, s => stringValue = s,
                            source3, d => doubleValue = d) :
                        DataflowBlock.Choose(
                            source1, i => usedScheduler = TaskScheduler.Current,
                            source2, s => usedScheduler = TaskScheduler.Current,
                            source3, d => usedScheduler = TaskScheduler.Current,
                            new DataflowBlockOptions { TaskScheduler = requestedScheduler, MaxMessagesPerTask = 1, CancellationToken = cts.Token });

                    switch (chooseTestCase)
                    {
                        case 0: // Test data on the first source before data on the second or third
                            source1.Post(42);
                            source1.Post(43);
                            Task.Delay(100).Wait();
                            source2.Post("44");
                            source2.Post("45");
                            source3.Post(46.0);
                            source3.Post(47.0);
                            localPassed &= t.Result == 0;
                            localPassed &= intValue == 42;
                            localPassed &= stringValue == null;
                            localPassed &= doubleValue == 0.0;
                            localPassed &= source1.Count == 1;
                            localPassed &= source2.Count == 2;
                            localPassed &= source3.Count == 2;
                            Assert.True(localPassed, string.Format("{0}: Data on the first source before data on the second or third", localPassed ? "Success" : "Failure"));

                            break;

                        case 1: // Test data on the second source before data on the first or third
                            source2.Post("42");
                            source2.Post("43");
                            Task.Delay(100).Wait();
                            source1.Post(44);
                            source1.Post(45);
                            source3.Post(46.0);
                            source3.Post(47.0);
                            localPassed &= t.Result == 1;
                            localPassed &= stringValue == "42";
                            localPassed &= intValue == 0;
                            localPassed &= doubleValue == 0.0;
                            localPassed &= source1.Count == 2;
                            localPassed &= source2.Count == 1;
                            localPassed &= source3.Count == 2;
                            Assert.True(localPassed, string.Format("{0}: Data on the second source before data on the first or third", localPassed ? "Success" : "Failure"));

                            break;

                        case 2: // Test data on the third source before data on the first or second
                            source3.Post(42.0);
                            source3.Post(43.0);
                            Task.Delay(100).Wait();
                            source1.Post(44);
                            source1.Post(45);
                            source2.Post("46");
                            source2.Post("47");
                            localPassed &= t.Result == 2;
                            localPassed &= doubleValue == 42.0;
                            localPassed &= stringValue == null;
                            localPassed &= intValue == 0;
                            localPassed &= source1.Count == 2;
                            localPassed &= source2.Count == 2;
                            localPassed &= source3.Count == 1;
                            Assert.True(localPassed, string.Format("{0}: Data on the third source before data on the first or second", localPassed ? "Success" : "Failure"));

                            break;

                        case 3: // Test no data on first source but data on second
                            source1.Complete();
                            source2.Post("42");
                            localPassed &= t.Result == 1;
                            localPassed &= intValue == 0;
                            localPassed &= stringValue == "42";
                            localPassed &= source2.Count == 0;
                            Assert.True(localPassed, string.Format("{0}: No data on first source but data on second", localPassed ? "Success" : "Failure"));

                            break;

                        case 4: // Test no data on second source but data on third
                            source2.Complete();
                            source3.Post(42.0);
                            localPassed &= t.Result == 2;
                            localPassed &= doubleValue == 42.0;
                            localPassed &= intValue == 0;
                            localPassed &= stringValue == null;
                            localPassed &= source3.Count == 0;
                            Assert.True(localPassed, string.Format("{0}: No data on second source but data on third", localPassed ? "Success" : "Failure"));

                            break;

                        case 5: // Test no data on third source but data on first
                            source3.Complete();
                            source1.Post(42);
                            localPassed &= t.Result == 0;
                            localPassed &= intValue == 42;
                            localPassed &= doubleValue == 0;
                            localPassed &= stringValue == null;
                            localPassed &= source1.Count == 0;
                            Assert.True(localPassed, string.Format("{0}: No data on third source but data on first", localPassed ? "Success" : "Failure"));

                            break;

                        case 6: // Test no data on any source
                            source1.Complete();
                            source2.Complete();
                            source3.Complete();
                            ((IAsyncResult)t).AsyncWaitHandle.WaitOne();
                            localPassed &= t.IsCanceled;
                            localPassed &= intValue == 0;
                            localPassed &= stringValue == null;
                            localPassed &= doubleValue == 0.0;
                            Assert.True(localPassed, string.Format("{0}: No data on either source", localPassed ? "Success" : "Failure"));

                            break;

                        // >= 7 TEST USING DATAFLOW BLOCK OPTIONS

                        case 7: // Test correct TaskScheduler is used
                            source3.Post(42);
                            t.Wait();
                            localPassed &= usedScheduler == requestedScheduler;
                            Assert.True(localPassed, string.Format("{0}: Correct TaskScheduler is used", localPassed ? "Success" : "Failure"));

                            break;

                        case 8: // Test cancellation takes effect
                            cts.Cancel();
                            source1.Post(42);
                            source2.Post("43");
                            source3.Post(44.0);
                            ((IAsyncResult)t).AsyncWaitHandle.WaitOne();
                            localPassed &= t.IsCanceled;
                            localPassed &= source1.Count == 1;
                            localPassed &= source2.Count == 1;
                            localPassed &= source3.Count == 1;
                            Assert.True(localPassed, string.Format("{0}: Cancellation takes effect", localPassed ? "Success" : "Failure"));

                            break;
                    }
                }
            }

            // Exception handling
            {
                bool localPassed = true;

                {
                    var cesp = new ConcurrentExclusiveSchedulerPair(); // ensure BufferBlocks are serialized
                    var source1 = new BufferBlock<int>(new DataflowBlockOptions { TaskScheduler = cesp.ExclusiveScheduler });
                    var source2 = new BufferBlock<string>(new DataflowBlockOptions { TaskScheduler = cesp.ExclusiveScheduler });
                    var source3 = new BufferBlock<double>(new DataflowBlockOptions { TaskScheduler = cesp.ExclusiveScheduler });
                    var t = DataflowBlock.Choose(
                        source1, i => { throw new InvalidOperationException(); },
                        source2, s => { throw new InvalidCastException(); },
                        source3, d => { throw new FormatException(); });
                    Assert.True(!t.IsCompleted, "Choose should not yet be completed");
                    source1.Post(42);
                    source2.Post("43");
                    source3.Post(44.0);
                    ((IAsyncResult)t).AsyncWaitHandle.WaitOne();
                    Assert.True(t.IsFaulted, "Choose should have faulted");
                    Assert.True(t.Exception.InnerException is InvalidOperationException, "Choose should have faulted with right exception");
                    Assert.True(source1.Count == 0, "First source should not have its data");
                    Assert.True(source2.Count == 1, "Second source should still contain its data");
                    Assert.True(source3.Count == 1, "Third source should still contain its data");
                }

                {
                    var cesp = new ConcurrentExclusiveSchedulerPair(); // ensure BufferBlocks are serialized
                    var source1 = new BufferBlock<int>(new DataflowBlockOptions { TaskScheduler = cesp.ExclusiveScheduler });
                    var source2 = new BufferBlock<string>(new DataflowBlockOptions { TaskScheduler = cesp.ExclusiveScheduler });
                    var source3 = new BufferBlock<double>(new DataflowBlockOptions { TaskScheduler = cesp.ExclusiveScheduler });
                    var t = DataflowBlock.Choose(
                        source1, i => { throw new InvalidOperationException(); },
                        source2, s => { throw new InvalidCastException(); },
                        source3, d => { throw new FormatException(); });
                    Assert.True(!t.IsCompleted, "Choose should not yet be completed");
                    source2.Post("43");
                    source1.Post(42);
                    source3.Post(44.0);
                    ((IAsyncResult)t).AsyncWaitHandle.WaitOne();
                    Assert.True(t.IsFaulted, "Choose should have faulted");
                    Assert.True(t.Exception.InnerException is InvalidCastException, "Choose should have faulted with right exception");
                    Assert.True(source1.Count == 1, "First source should still contain its data");
                    Assert.True(source2.Count == 0, "Second source should not have its data");
                    Assert.True(source3.Count == 1, "Third source should still contain its data");
                }

                {
                    var cesp = new ConcurrentExclusiveSchedulerPair(); // ensure BufferBlocks are serialized
                    var source1 = new BufferBlock<int>(new DataflowBlockOptions { TaskScheduler = cesp.ExclusiveScheduler });
                    var source2 = new BufferBlock<string>(new DataflowBlockOptions { TaskScheduler = cesp.ExclusiveScheduler });
                    var source3 = new BufferBlock<double>(new DataflowBlockOptions { TaskScheduler = cesp.ExclusiveScheduler });
                    var t = DataflowBlock.Choose(
                        source1, i => { throw new InvalidOperationException(); },
                        source2, s => { throw new InvalidCastException(); },
                        source3, d => { throw new FormatException(); });
                    Assert.True(!t.IsCompleted, "Choose should not yet be completed");
                    source3.Post(44.0);
                    source2.Post("43");
                    source1.Post(42);
                    ((IAsyncResult)t).AsyncWaitHandle.WaitOne();
                    Assert.True(t.IsFaulted, "Choose should have faulted");
                    Assert.True(t.Exception.InnerException is FormatException, "Choose should have faulted with right exception");
                    Assert.True(source1.Count == 1, "First source should still contain its data");
                    Assert.True(source2.Count == 1, "Second source should still contain its data");
                    Assert.True(source3.Count == 0, "Third source should not have its data");
                }

                Assert.True(localPassed, string.Format("{0}: Exception handling", localPassed ? "Success" : "Failure"));
            }
        }

        //[Fact(Skip = "Outerloop")]
        public void TestChooseWithLinkTracking()
        {
            Assert.True(TestChooseWithLinkTrackingOverload(n: 2, cancelBeforeChoose: true));
            Assert.True(TestChooseWithLinkTrackingOverload(n: 2, cancelBeforeChoose: false));
            Assert.True(TestChooseWithLinkTrackingOverload(n: 3, cancelBeforeChoose: true));
            Assert.True(TestChooseWithLinkTrackingOverload(n: 3, cancelBeforeChoose: false));
        }

        private static bool TestChooseWithLinkTrackingOverload(int n, bool cancelBeforeChoose)
        {
            // We never post any messages to the sources, because
            // a canceled Choose should complete even without messages.
            var cts = new CancellationTokenSource();
            var options = new DataflowBlockOptions { MaxMessagesPerTask = 1, CancellationToken = cts.Token };
            var source1 = new LinkTrackingSource<int>();
            var source2 = new LinkTrackingSource<int>();
            var source3 = new LinkTrackingSource<int>();
            Action<int> noop = x => { };
            int expectedLinkCount = cancelBeforeChoose ? 0 : 1;
            Task<int> choose;
            bool localPassed;

            // Cancel the token before Choose if requested
            if (cancelBeforeChoose) { cts.Cancel(); Task.Delay(500).Wait(); }

            // Launch Choose
            if (n == 2) choose = DataflowBlock.Choose(source1, noop, source2, noop, options);
            else choose = DataflowBlock.Choose(source1, noop, source2, noop, source3, noop, options);

            // Cancel the token after Choose if requested
            if (!cancelBeforeChoose) { Task.Delay(500).Wait(); cts.Cancel(); }

            // The Choose should complete even if there have been no messages
            localPassed = ((IAsyncResult)choose).AsyncWaitHandle.WaitOne(1000) && choose.IsCanceled;

            Assert.True(localPassed, string.Format("Choice is canceled - {0}", localPassed ? "Passed" : "FAILED"));

            // The Choose should unlink from each source
            localPassed = source1.UnlinkCount == expectedLinkCount && source1.LinkCount == expectedLinkCount;

            Assert.True(localPassed, string.Format("source1.LinkCount={0}, source1.UnlinkCount={1} - {2}", source1.LinkCount, source1.UnlinkCount, localPassed ? "Passed" : "FAILED"));

            localPassed = source2.UnlinkCount == expectedLinkCount && source2.LinkCount == expectedLinkCount;

            Assert.True(localPassed, string.Format("source2.LinkCount={0}, source2.UnlinkCount={1} - {2}", source2.LinkCount, source2.UnlinkCount, localPassed ? "Passed" : "FAILED"));

            if (n > 2)
            {
                localPassed = source3.UnlinkCount == expectedLinkCount && source3.LinkCount == expectedLinkCount;

                Assert.True(localPassed, string.Format("source3.LinkCount={0}, source3.UnlinkCount={1} - {2}", source3.LinkCount, source3.UnlinkCount, localPassed ? "Passed" : "FAILED"));
            }

            return localPassed;
        }

        //[Fact(Skip = "Outerloop")]
        public void TestNoDeadlockOnChoose()
        {
            const int ITERATIONS = 1000;
            var tasks = new Task<int>[ITERATIONS];
            int branch1 = 0;
            int branch2 = 0;
            // Construct sources
            var source1 = new BufferBlock<int>();
            var source2 = new BufferBlock<int>();
            source1.Completion.ContinueWith(t => { Console.WriteLine("source1"); });
            source2.Completion.ContinueWith(t => { Console.WriteLine("source2"); });

            // Post the rest of the messages and choose
            for (int i = 0; i < ITERATIONS; i++)
            {
                source1.Post(i);
                source2.Post(i);

                tasks[i] = DataflowBlock.Choose(source1, x => { Interlocked.Increment(ref branch1); }, source2, x => { Interlocked.Increment(ref branch2); });
            }

            Console.WriteLine("Start waiting...");
            Task.WaitAll(tasks);
            Console.WriteLine(string.Format("branch1={0}, branch2={1}", branch1, branch2));
            Console.WriteLine("Test passed.");
        }

        //[Fact(Skip = "Outerloop")]
        public void TestEncapsulate()
        {
            var buffer = new BufferBlock<int>();
            var action = new ActionBlock<int>(i => buffer.Post(i));
            action.Completion.ContinueWith(delegate { buffer.Complete(); });
            var encapsulated = DataflowBlock.Encapsulate(action, buffer);

            // Test argument validation
            {
                Assert.Throws<ArgumentNullException>(
                    () => DataflowBlock.Encapsulate<int, int>(null, new BufferBlock<int>()));
                Assert.Throws<ArgumentNullException>(
                    () => DataflowBlock.Encapsulate<int, int>(new BufferBlock<int>(), null));
            }

            // Test linking and unlinking
            {
                bool localPassed = true;
                var endTarget = new BufferBlock<int>();
                var unlink = encapsulated.LinkTo(endTarget);
                localPassed &= unlink != null;
                var unlink2 = encapsulated.LinkTo(endTarget);
                localPassed &= unlink2 != null;
                action.Post(1);
                localPassed &= endTarget.Receive() == 1;
                unlink.Dispose();
                action.Post(2);
                localPassed &= endTarget.Receive() == 2;
                unlink2.Dispose();
                Assert.True(localPassed, string.Format("{0}: Linking and unlinking", localPassed ? "Success" : "Failure"));
            }

            // Test encapsulation of a bounded target
            {
                bool localPassed = true;
                int messageSent = 0;
                int messagesReceived = 0;

                // Encapsulation (with a sink)
                var boundedTargetPart = new TransformBlock<int, int>(x => { messagesReceived++; Task.Delay(1).Wait(); return x; },
                                                                    new ExecutionDataflowBlockOptions() { BoundedCapacity = 1 });
                var sourcePart = new BufferBlock<int>();
                boundedTargetPart.LinkTo(sourcePart);
                boundedTargetPart.Completion.ContinueWith(completion => sourcePart.Complete());
                var encapsulatedTarget = DataflowBlock.Encapsulate(boundedTargetPart, sourcePart);
                var sink = new ActionBlock<int>(x => { });
                encapsulatedTarget.LinkTo(sink);

                // Mini-network
                var source = new BufferBlock<int>();
                source.LinkTo(encapsulatedTarget);
                source.Completion.ContinueWith(completion => encapsulatedTarget.Complete());

                // Feed
                for (messageSent = 0; messageSent < 2; messageSent++) source.Post(messageSent);
                source.Complete();

                // Verify
                encapsulatedTarget.Completion.Wait(5000);
                localPassed &= messagesReceived == messageSent;
                Assert.True(localPassed, string.Format("{0}: Bounded (sent={1}, received={2})", localPassed ? "Success" : "Failure", messageSent, messagesReceived));
            }
        }

        //[Fact(Skip = "Outerloop")]
        public void TestOutputAvailableAsync()
        {
            for (int c = 0; c < 2; c++)
            {
                CancellationToken cancellationToken = c == 0 ?
                    CancellationToken.None :
                    new CancellationTokenSource().Token;

                // Test argument validation
                {
                    Assert.Throws<ArgumentNullException>(
                        () => DataflowBlock.OutputAvailableAsync<int>(null));
                    Assert.Throws<ArgumentNullException>(
                        () => DataflowBlock.OutputAvailableAsync<int>(null, cancellationToken));
                }

                // Test availability before there is any data, without a token
                {
                    var localPassed = true;
                    var buffer = new BufferBlock<int>();
                    var t = buffer.OutputAvailableAsync();
                    localPassed &= !t.IsCompleted;
                    buffer.Post(42);
                    localPassed &= t.Result;
                    localPassed &= buffer.Count == 1;
                    localPassed &= buffer.Receive() == 42;
                    Assert.True(localPassed, string.Format("{0}: No cancellation token, before data available", localPassed ? "Success" : "Failure"));
                }

                // Test availability before there is any data
                {
                    var localPassed = true;
                    var buffer = new BufferBlock<int>();
                    var t = buffer.OutputAvailableAsync(cancellationToken);
                    localPassed &= !t.IsCompleted;
                    buffer.Post(42);
                    localPassed &= t.Result;
                    localPassed &= buffer.Count == 1;
                    localPassed &= buffer.Receive() == 42;
                    Assert.True(localPassed, string.Format("{0}: Before data available", localPassed ? "Success" : "Failure"));
                }

                // Test availability after there is data, without a token
                {
                    var localPassed = true;
                    var buffer = new BufferBlock<int>();
                    buffer.Post(42);
                    var t = buffer.OutputAvailableAsync();
                    localPassed &= t.IsCompleted;
                    localPassed &= t.Result;
                    localPassed &= buffer.Count == 1;
                    localPassed &= buffer.Receive() == 42;
                    Assert.True(localPassed, string.Format("{0}: No cancellation token, after data available", localPassed ? "Success" : "Failure"));
                }

                // Test availability after there is data
                {
                    var localPassed = true;
                    var buffer = new BufferBlock<int>();
                    buffer.Post(42);
                    var t = buffer.OutputAvailableAsync(cancellationToken);
                    localPassed &= t.IsCompleted;
                    localPassed &= t.Result;
                    localPassed &= buffer.Count == 1;
                    localPassed &= buffer.Receive() == 42;
                    Assert.True(localPassed, string.Format("{0}: After data available", localPassed ? "Success" : "Failure"));
                }

                // Test availability for blocks that do not offer data after they're completed
                {
                    var localPassed = true;
                    var buffer = new BufferBlock<int>();
                    buffer.Post(42);
                    buffer.Complete();
                    buffer.Receive();
                    buffer.Completion.Wait();
                    var t = buffer.OutputAvailableAsync(cancellationToken);
                    localPassed &= !t.Result;
                    localPassed &= buffer.Count == 0;
                    Assert.True(localPassed, string.Format("{0}: After BufferBlock completion with no data", localPassed ? "Success" : "Failure"));
                }

                // Test availability for blocks that offer data after they're completed
                {
                    var localPassed = true;
                    var wob = new WriteOnceBlock<int>(_ => _);
                    wob.Post(42);
                    wob.Completion.Wait();
                    var t = wob.OutputAvailableAsync(cancellationToken);
                    localPassed &= t.IsCompleted;
                    localPassed &= t.Result;
                    localPassed &= wob.Receive() == 42;
                    Assert.True(localPassed, string.Format("{0}: After WriteOnceBlock completion with data", localPassed ? "Success" : "Failure"));
                }
                {
                    var localPassed = true;
                    var bb = new BroadcastBlock<int>(_ => _);
                    bb.Post(42);
                    bb.Complete();
                    bb.Completion.Wait();
                    var t = bb.OutputAvailableAsync(cancellationToken);
                    localPassed &= t.IsCompleted;
                    localPassed &= t.Result;
                    localPassed &= bb.Receive() == 42;
                    Assert.True(localPassed, string.Format("{0}: After BroadcastBlock completion with data", localPassed ? "Success" : "Failure"));
                }
                {
                    var localPassed = true;
                    var wob = new WriteOnceBlock<int>(_ => _);
                    wob.Complete();
                    wob.Completion.Wait();
                    var t = wob.OutputAvailableAsync(cancellationToken);
                    localPassed &= !t.Result;
                    Assert.True(localPassed, string.Format("{0}: After WriteOnceBlock completion with no data", localPassed ? "Success" : "Failure"));
                }
                {
                    var localPassed = true;
                    var bb = new BroadcastBlock<int>(_ => _);
                    bb.Complete();
                    bb.Completion.Wait();
                    var t = bb.OutputAvailableAsync(cancellationToken);
                    localPassed &= !t.Result;
                    Assert.True(localPassed, string.Format("{0}: After BroadcastBlock completion with no data", localPassed ? "Success" : "Failure"));
                }

                // Test OutputAvailableAsync in a sequence
                {
                    var localPassed = true;
                    for (int i = 0; i < 2; i++)
                    {
                        const int ITEMS_TO_POST = 1000;
                        bool executeSynchronously = i == 0;
                        var bb = new BufferBlock<int>();
                        for (int v = 0; v < ITEMS_TO_POST; v++) bb.Post(v);
                        using (var mres = new ManualResetEventSlim())
                        {
                            int iter = 0;
                            Action body = null;
                            body = () =>
                            {
                                bb.OutputAvailableAsync(cancellationToken).ContinueWith(receiveTask =>
                                {
                                    if (!receiveTask.Result || iter != bb.Receive())
                                    {
                                        localPassed = false;
                                        mres.Set();
                                    }
                                    else
                                    {
                                        iter++;
                                        if (iter < ITEMS_TO_POST) body();
                                        else mres.Set();
                                    }
                                }, executeSynchronously ? TaskContinuationOptions.ExecuteSynchronously : TaskContinuationOptions.None);
                            };
                            body();
                            mres.Wait();
                        }
                    }
                    Assert.True(localPassed, string.Format("{0}: OutputAvailableAsync in continuation sequence", localPassed ? "Success" : "Failure"));
                }
            }

            // Pre-canceled
            {
                var localPassed = true;
                var buffer = new BufferBlock<int>();
                var ct = new CancellationToken(true);
                var t = buffer.OutputAvailableAsync(ct);
                localPassed &= t.IsCanceled;
                Assert.True(localPassed, string.Format("{0}: Pre-canceled", localPassed ? "Success" : "Failure"));
            }

            // Cancel after call but before data available
            {
                var localPassed = true;
                var buffer = new BufferBlock<int>();
                var cts = new CancellationTokenSource();
                var t = buffer.OutputAvailableAsync(cts.Token);
                localPassed &= !t.IsCompleted;
                cts.Cancel();
                ((IAsyncResult)t).AsyncWaitHandle.WaitOne();
                buffer.Post(42);
                localPassed &= t.IsCanceled;
                Assert.True(localPassed, string.Format("{0}: Cancel after call but before data available", localPassed ? "Success" : "Failure"));
            }

            // Cancel after completion
            {
                var localPassed = true;
                var buffer = new BufferBlock<int>();
                var cts = new CancellationTokenSource();
                var t = buffer.OutputAvailableAsync(cts.Token);
                localPassed &= !t.IsCompleted;
                buffer.Post(42);
                t.Wait();
                cts.Cancel();
                localPassed &= t.Result;
                Assert.True(localPassed, string.Format("{0}: Cancel after data available", localPassed ? "Success" : "Failure"));
            }
        }

        #region DataflowBlockExtension Test Helpers
        private sealed class SimpleTaskScheduler : TaskScheduler
        {
            protected override IEnumerable<Task> GetScheduledTasks() { return null; }

            protected override void QueueTask(Task task)
            {
                ThreadPool.QueueUserWorkItem(delegate { TryExecuteTask(task); });
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                return TryExecuteTask(task);
            }
        }

        internal enum DelayedTargetForSendAsyncMode
        {
            ImmediateAcceptance,
            ImmediateDecline,
            DelayedConsume,
            MultipleReserveReleaseBeforeAcceptance,
            MultipleReserveReleaseBeforeConsume,
            Drop,
            DeclineForMultipleLinking,
            AcceptOnTriggerCall,
            MultipleReserveReleaseBeforeAcceptOnTriggerCall,
        }

        private sealed class DelayedTargetForSendAsync<T> : ITargetBlock<T>
        {
            private readonly TaskCompletionSource<object> _m_task = new TaskCompletionSource<object>();
            private readonly DelayedTargetForSendAsyncMode _m_mode;
            private readonly ITargetBlock<T> _m_this;
            private const int MILLISECONDS_DELAY = 10;
            private const int MULTIPLE_MODE_MAX_ITERATIONS = 2;
            private int _m_iteration = 0;
            private ManualResetEventSlim _m_triggerAcceptEvent = new ManualResetEventSlim();

            public DelayedTargetForSendAsync(DelayedTargetForSendAsyncMode mode)
            {
                _m_mode = mode;
                _m_this = this;
            }

            public DelayedTargetForSendAsync(DelayedTargetForSendAsyncMode mode, ITargetBlock<T> thisOverride)
            {
                _m_mode = mode;
                _m_this = thisOverride;
            }

            public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T> source, bool consumeToAccept)
            {
                if (!messageHeader.IsValid) throw new ArgumentException("The value does not represent a valid message.", "messageHeader");

                // Decline SendAsync's fast path so that it retries with the slow path
                if (source == null) return DataflowMessageStatus.Declined;

                // Make sure the source has a valid Completion property
                var completion = source.Completion;

                switch (_m_mode)
                {
                    case DelayedTargetForSendAsyncMode.ImmediateAcceptance:
                        if (consumeToAccept)
                        {
                            bool messageConsumed;
                            T consumedValue = source.ConsumeMessage(messageHeader, this, out messageConsumed);
                            if (!messageConsumed) return DataflowMessageStatus.NotAvailable;
                        }
                        _m_task.SetResult(null);
                        return DataflowMessageStatus.Accepted;

                    case DelayedTargetForSendAsyncMode.AcceptOnTriggerCall:
                        ThreadPool.QueueUserWorkItem(delegate
                        {
                            _m_triggerAcceptEvent.Wait();
                            bool consumed = false;
                            messageValue = source.ConsumeMessage(messageHeader, _m_this, out consumed);
                            if (!consumed) _m_task.SetCanceled();
                            else _m_task.SetResult(null);
                        });
                        return DataflowMessageStatus.Postponed;

                    case DelayedTargetForSendAsyncMode.ImmediateDecline:
                        _m_task.SetResult(null);
                        return DataflowMessageStatus.Declined;

                    case DelayedTargetForSendAsyncMode.DelayedConsume:
                        new Timer(self =>
                        {
                            ((Timer)self).Dispose();
                            bool consumed;
                            messageValue = source.ConsumeMessage(messageHeader, _m_this, out consumed);
                            if (!consumed) _m_task.SetException(new InvalidOperationException("Didn't get message back from SendAsync"));
                            else _m_task.SetResult(null);
                        }, null, MILLISECONDS_DELAY, Timeout.Infinite);
                        return DataflowMessageStatus.Postponed;

                    case DelayedTargetForSendAsyncMode.MultipleReserveReleaseBeforeAcceptance:
                        if (++_m_iteration >= MULTIPLE_MODE_MAX_ITERATIONS)
                        {
                            if (consumeToAccept)
                            {
                                bool messageConsumed;
                                T consumedValue = source.ConsumeMessage(messageHeader, this, out messageConsumed);
                                if (!messageConsumed) return DataflowMessageStatus.NotAvailable;
                            }
                            _m_task.SetResult(null);
                            return DataflowMessageStatus.Accepted;
                        }
                        else
                        {
                            new Timer(self =>
                            {
                                ((Timer)self).Dispose();
                                bool reserved = source.ReserveMessage(messageHeader, _m_this);
                                if (!reserved) _m_task.SetException(new InvalidOperationException("Could not reserve message."));
                                else
                                {
                                    try
                                    {
                                        source.ReleaseReservation(messageHeader, _m_this);
                                    }
                                    catch (Exception exception) { _m_task.SetException(exception); }
                                }
                            }, null, MILLISECONDS_DELAY, Timeout.Infinite);
                            return DataflowMessageStatus.Postponed;
                        }

                    case DelayedTargetForSendAsyncMode.MultipleReserveReleaseBeforeAcceptOnTriggerCall:
                        if (++_m_iteration >= MULTIPLE_MODE_MAX_ITERATIONS)
                        {
                            _m_triggerAcceptEvent.Wait();
                            if (consumeToAccept)
                            {
                                bool messageConsumed;
                                T consumedValue = source.ConsumeMessage(messageHeader, this, out messageConsumed);
                                if (!messageConsumed) return DataflowMessageStatus.NotAvailable;
                            }
                            _m_task.SetResult(null);
                            return DataflowMessageStatus.Accepted;
                        }
                        else
                        {
                            new Timer(self =>
                            {
                                ((Timer)self).Dispose();
                                bool reserved = source.ReserveMessage(messageHeader, _m_this);
                                if (!reserved) _m_task.SetCanceled();
                                else
                                {
                                    try
                                    {
                                        source.ReleaseReservation(messageHeader, _m_this);
                                    }
                                    catch (Exception exception) { _m_task.SetException(exception); }
                                }
                            }, null, MILLISECONDS_DELAY, Timeout.Infinite);
                            return DataflowMessageStatus.Postponed;
                        }

                    case DelayedTargetForSendAsyncMode.MultipleReserveReleaseBeforeConsume:
                        if (++_m_iteration >= MULTIPLE_MODE_MAX_ITERATIONS)
                        {
                            new Timer(self =>
                            {
                                ((Timer)self).Dispose();
                                bool consumed;
                                messageValue = source.ConsumeMessage(messageHeader, _m_this, out consumed);
                                if (!consumed) _m_task.SetException(new InvalidOperationException("Didn't get message back from SendAsync"));
                                else _m_task.SetResult(null);
                            }, null, MILLISECONDS_DELAY, Timeout.Infinite);
                        }
                        else
                        {
                            new Timer(self =>
                            {
                                ((Timer)self).Dispose();
                                bool reserved = source.ReserveMessage(messageHeader, _m_this);
                                if (!reserved) _m_task.SetException(new InvalidOperationException("Could not reserve message."));
                                else
                                {
                                    try
                                    {
                                        source.ReleaseReservation(messageHeader, _m_this);
                                    }
                                    catch (Exception exception) { _m_task.SetException(exception); }
                                }
                            }, null, MILLISECONDS_DELAY, Timeout.Infinite);
                        }
                        return DataflowMessageStatus.Postponed;

                    case DelayedTargetForSendAsyncMode.Drop:
                        _m_task.SetResult(null);
                        return DataflowMessageStatus.Postponed;

                    case DelayedTargetForSendAsyncMode.DeclineForMultipleLinking:
                        _m_task.TrySetResult(null);
                        return DataflowMessageStatus.Declined;


                    default:
                        throw new InvalidOperationException("Invalid mode");
                }
            }

            public void TriggerAccept()
            {
                _m_triggerAcceptEvent.Set();
            }

            public System.Threading.Tasks.Task Completion { get { return _m_task.Task; } }

            public bool Post(T item) { throw new NotImplementedException(); }
            public void Complete() { throw new NotImplementedException(); }
            void IDataflowBlock.Fault(Exception exception) { throw new NotImplementedException(); }
        }

        private sealed class DelegateObserver<T> : IObserver<T>
        {
            private Action<T> _m_onNext = null;
            private Action<Exception> _m_onError = null;
            private Action _m_onCompleted = null;

            public DelegateObserver(Action<T> onNext = null, Action<Exception> onError = null, Action onCompleted = null)
            {
                _m_onNext = onNext;
                _m_onError = onError;
                _m_onCompleted = onCompleted;
            }

            void IObserver<T>.OnNext(T value) { if (_m_onNext != null) _m_onNext(value); }
            void IObserver<T>.OnError(Exception error) { if (_m_onError != null) _m_onError(error); }
            void IObserver<T>.OnCompleted() { if (_m_onCompleted != null) _m_onCompleted(); }
        }

        private sealed class NonSynchronizedHotObservingObservable<T> : IObservable<T>
        {
            private Dictionary<IDisposable, IObserver<T>> _m_observers = new Dictionary<IDisposable, IObserver<T>>();

            public IDisposable Subscribe(IObserver<T> observer)
            {
                ActionOnDispose disposer = null;
                disposer = new ActionOnDispose { m_dispose = () => _m_observers.Remove(disposer) };
                _m_observers.Add(disposer, observer);
                return disposer;
            }

            public void OnNext(T item) { foreach (var observer in _m_observers.Values) observer.OnNext(item); }
            public void OnError(Exception error) { foreach (var observer in _m_observers.Values) observer.OnError(error); }
            public void OnCompleted() { foreach (var observer in _m_observers.Values) observer.OnCompleted(); }
        }



        private sealed class ActionOnDispose : IDisposable
        {
            internal Action m_dispose;
            public void Dispose() { if (m_dispose != null) m_dispose(); }
        }
        #endregion
    }

    internal class LinkTrackingSource<T> : IPropagatorBlock<T, T>
    {
        internal int LinkCount;
        internal int UnlinkCount;
        private TaskCompletionSource<object> _m_completionTask = new TaskCompletionSource<object>();

        T ISourceBlock<T>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<T> target, out Boolean messageConsumed)
        {
            throw new NotSupportedException();
        }

        void ISourceBlock<T>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<T> target)
        {
            throw new NotSupportedException();
        }

        bool ISourceBlock<T>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<T> target)
        {
            throw new NotSupportedException();
        }

        IDisposable ISourceBlock<T>.LinkTo(ITargetBlock<T> target, DataflowLinkOptions linkOptions)
        {
            Interlocked.Increment(ref LinkCount);
            return new TrackUnlink(this);
        }

        Task IDataflowBlock.Completion
        {
            get { return _m_completionTask.Task; }
        }

        public void Complete()
        {
            throw new NotSupportedException();
        }

        void IDataflowBlock.Fault(Exception exception)
        {
            throw new NotSupportedException();
        }

        public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T> source, bool consumeToAccept)
        {
            throw new NotSupportedException();
        }

        public bool Post(T item)
        {
            throw new NotSupportedException();
        }

        class TrackUnlink : IDisposable
        {
            private LinkTrackingSource<T> _m_source;

            internal TrackUnlink(LinkTrackingSource<T> source)
            {
                _m_source = source;
            }

            void IDisposable.Dispose()
            {
                Interlocked.Increment(ref _m_source.UnlinkCount);
            }
        }
    }
}
