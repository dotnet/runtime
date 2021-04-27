// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Threading.Tasks.Dataflow.Tests
{
    public partial class DataflowBlockExtensionsTests
    {
        [Fact]
        public void TestDataflowMessageHeader()
        {
            AssertExtensions.Throws<ArgumentException>("id", () => new DataflowMessageHeader(0));

            Assert.False(new DataflowMessageHeader().IsValid);
            Assert.True(new DataflowMessageHeader(1).IsValid);
            Assert.True(new DataflowMessageHeader(-1).IsValid);

            Assert.Equal(expected: 42, actual: new DataflowMessageHeader(42).Id);

            Assert.True(new DataflowMessageHeader(1).Equals(new DataflowMessageHeader(1)));
            Assert.False(new DataflowMessageHeader(1).Equals(new DataflowMessageHeader(2)));

            Assert.True(new DataflowMessageHeader(1).Equals((object)new DataflowMessageHeader(1)));
            Assert.False(new DataflowMessageHeader(1).Equals((object)new DataflowMessageHeader(2)));
            Assert.False(new DataflowMessageHeader(1).Equals("string"));

            Assert.True(new DataflowMessageHeader(100) == new DataflowMessageHeader(100));
            Assert.False(new DataflowMessageHeader(100) == new DataflowMessageHeader(101));
            Assert.True(new DataflowMessageHeader(100) != new DataflowMessageHeader(101));
            Assert.False(new DataflowMessageHeader(100) != new DataflowMessageHeader(100));

            Assert.True(new DataflowMessageHeader(42).GetHashCode() == 42);
        }

        [Fact]
        public void TestNullTarget_NonNull()
        {
            Assert.NotNull(DataflowBlock.NullTarget<int>());
            Assert.NotNull(DataflowBlock.NullTarget<object>());
            Assert.False(DataflowBlock.NullTarget<int>().Completion.IsCompleted);
        }

        [Fact]
        public async Task TestNullTarget_OfferMessage()
        {
            DataflowTestHelpers.TestOfferMessage_ArgumentValidation(DataflowBlock.NullTarget<int>());
            DataflowTestHelpers.TestOfferMessage_AcceptsDataDirectly(DataflowBlock.NullTarget<string>());
            await DataflowTestHelpers.TestOfferMessage_AcceptsViaLinking(DataflowBlock.NullTarget<double>());

            // Test OfferMessage(consumeToAccept: false)
            Assert.Equal(
                expected: DataflowMessageStatus.Accepted,
                actual: DataflowBlock.NullTarget<double>().OfferMessage(new DataflowMessageHeader(1), 3.14, null, consumeToAccept: false));

            // Test OfferMessage(consumeToAccept: true)
            long consumedId = -1;
            DataflowBlock.NullTarget<int>().OfferMessage(new DataflowMessageHeader(42), 84, new DelegatePropagator<int, int>()
            {
                ConsumeMessageDelegate = delegate(DataflowMessageHeader messageHeader, ITargetBlock<int> target, out bool messageConsumed) {
                    consumedId = messageHeader.Id;
                    messageConsumed = true;
                    return 0;
                }
            }, consumeToAccept: true);
            Assert.Equal(expected: 42, actual: consumedId);

            // Test bad source
            Assert.Throws<InvalidOperationException>(() => {
                var target = DataflowBlock.NullTarget<int>();
                DataflowBlock.NullTarget<int>().OfferMessage(new DataflowMessageHeader(42), 84, new DelegatePropagator<int, int>()
                {
                    ConsumeMessageDelegate = delegate(DataflowMessageHeader _, ITargetBlock<int> __, out bool ___) {
                        throw new InvalidOperationException();
                    }
                }, consumeToAccept: true);
                Assert.True(target.Post(42));
            });

            // Test message no longer available
            var stingySource = new DelegatePropagator<int, int>
            {
                ConsumeMessageDelegate = (DataflowMessageHeader messageHeader, ITargetBlock<int> target, out bool messageConsumed) => {
                    messageConsumed = false;
                    return 0;
                }
            };
            Assert.Equal(
                expected: DataflowMessageStatus.NotAvailable,
                actual: DataflowBlock.NullTarget<int>().OfferMessage(
                    new DataflowMessageHeader(1), 1, stingySource, consumeToAccept: true));
        }

        [Fact]
        public void TestNullTarget_Completion()
        {
            var target = DataflowBlock.NullTarget<object>();

            Assert.NotNull(target.Completion);
            Assert.Equal(target.Completion, target.Completion);

            target.Complete();
            target.Fault(new Exception());
            Assert.False(target.Completion.IsCompleted);

            Assert.NotSame(
                DataflowBlock.NullTarget<object>().Completion,
                DataflowBlock.NullTarget<object>().Completion);
            Assert.NotSame(
                DataflowBlock.NullTarget<int>().Completion,
                DataflowBlock.NullTarget<int>().Completion);
        }

        [Fact]
        [OuterLoop] // finalizer/GC interactions
        [ActiveIssue("https://github.com/dotnet/runtime/issues/46566", TestPlatforms.Browser)]
        public void TestNullTarget_CompletionNoCaching()
        {
            // Make sure that the Completion task returned by a NullTarget
            // is not cached across all NullTargets.  Since it'll never complete,
            // that would be a potentially huge memory leak.
            var wro = CreateWeakReferenceToObjectReferencedByNullTargetContinuation();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            object state;
            Assert.False(wro.TryGetTarget(out state));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference<object> CreateWeakReferenceToObjectReferencedByNullTargetContinuation()
        {
            var state = new object();
            var wro = new WeakReference<object>(state);
            DataflowBlock.NullTarget<int>().Completion.ContinueWith(delegate { }, state);
            return wro;
        }

        [Fact]
        public void TestPost_ArgumentValidation()
        {
            Assert.Throws<ArgumentNullException>(() => ((ITargetBlock<int>)null).Post(42));
        }

        [Fact]
        public void TestAsObservableAndAsObserver_ArgumentValidation()
        {
            Assert.Throws<ArgumentNullException>(() => ((ISourceBlock<int>)null).AsObservable());
            Assert.Throws<ArgumentNullException>(() => ((ITargetBlock<int>)null).AsObserver());
            Assert.Throws<ArgumentNullException>(() => new BufferBlock<int>().AsObservable().Subscribe(null));
        }

        [Fact]
        public async Task TestAsObservableAndAsObserver_MixedTargets()
        {
            var tcs = new TaskCompletionSource<int>();

            var source = new BufferBlock<int>();
            source.AsObservable().Subscribe(new DelegateObserver<int>
            {
                OnNextDelegate = i => tcs.TrySetResult(i)
            });

            source.Post(42);
            Assert.Equal(expected: 42, actual: await tcs.Task);
        }

        [Fact]
        public async Task TestAsObservableAndAsObserver_DataPropagation()
        {
            // Test that preset data flows correctly
            {
                var bb = new BufferBlock<int>();
                bb.PostRange(0, 2);
                bb.Complete();

                int nextValueExpected = 0;
                var ab = new ActionBlock<int>(i => {
                    Assert.True(i == nextValueExpected, string.Format("Expected next value to be {0} but got {1}", nextValueExpected, i));
                    nextValueExpected++;
                });

                bb.AsObservable().Subscribe(ab.AsObserver());
                await ab.Completion;
            }

            // Test that new data flows correctly
            {
                int nextValueExpected = -2;
                var ab = new ActionBlock<int>(i => {
                    Assert.True(i == nextValueExpected, string.Format("Expected next value to be {0} but got {1}", nextValueExpected, i));
                    nextValueExpected++;
                });

                var bb = new BufferBlock<int>();
                bb.AsObservable().Subscribe(ab.AsObserver());

                bb.PostRange(-2, 0);
                bb.Complete();

                await ab.Completion;
            }

            // Test that unsubscribing stops flow of data and stops completion
            {
                var target = new BufferBlock<int>();
                var source = new BufferBlock<int>();

                using (source.AsObservable().Subscribe(target.AsObserver()))
                {
                    source.PostItems(1, 2);
                    Assert.Equal(expected: 1, actual: await target.ReceiveAsync());
                    Assert.Equal(expected: 2, actual: await target.ReceiveAsync());
                }

                source.Post(3);
                var wb = new WriteOnceBlock<int>(i => i);
                source.LinkTo(wb);
                await wb.Completion;

                source.Complete();
                await source.Completion;

                Assert.False(target.Completion.IsCompleted);
            }
        }

        [Fact]
        public async Task TestAsObservableAndAsObserver_ErrorPropagation()
        {
            // Test that exceptional data flows when exception occurs before and after subscription
            foreach (bool beforeSubscription in DataflowTestHelpers.BooleanValues)
            {
                var tb = new TransformBlock<int, int>(i => {
                    if (i == 42) throw new InvalidOperationException("uh oh");
                    return i;
                });

                if (beforeSubscription)
                {
                    tb.Post(42);
                    await Assert.ThrowsAsync<InvalidOperationException>(async () => await tb.Completion);
                }

                ITargetBlock<int>[] targets = Enumerable.Range(0, 3).Select(_ => new WriteOnceBlock<int>(i => i)).ToArray();
                foreach (var target in targets)
                {
                    tb.AsObservable().Subscribe(target.AsObserver());
                }

                if (!beforeSubscription)
                {
                    tb.Post(42);
                    await Assert.ThrowsAsync<InvalidOperationException>(() => tb.Completion);
                }

                foreach (var target in targets)
                {
                    await Assert.ThrowsAsync<AggregateException>(() => target.Completion);
                }
            }
        }

        [Fact]
        public void TestAsObservableAndAsObserver_ObservableIdempotency()
        {
            ISourceBlock<string> b1 = new BufferBlock<string>();
            ISourceBlock<string> b2 = new BufferBlock<string>();
            Assert.NotNull(b1.AsObservable());
            Assert.True(b1.AsObservable() == b1.AsObservable());
            Assert.True(b1.AsObservable() != b2.AsObservable());

            ISourceBlock<int> b4 = new BufferBlock<int>();
            ISourceBlock<int> b5 = new BufferBlock<int>();
            Assert.NotNull(b4.AsObservable());
            Assert.True(b4.AsObservable() == b4.AsObservable());
            Assert.True(b4.AsObservable() != b5.AsObservable());
        }

        [Fact]
        public void TestAsObservableAndAsObserver_AsObservableDoesntConsume()
        {
            var b = new BufferBlock<int>();
            b.PostRange(0, 2);

            Assert.Equal(expected: 2, actual: b.Count);
            Assert.NotNull(b.AsObservable());
            Assert.Equal(expected: 2, actual: b.Count);
        }

        [Fact]
        public void TestAsObservableAndAsObserver_LinkAndUnlink()
        {
            var b = new BufferBlock<int>();
            var o = b.AsObservable();
            for (int i = 0; i < 2; i++)
            {
                IDisposable[] unlinkers = Enumerable.Range(0, 5).Select(_ => o.Subscribe(new DelegateObserver<int>())).ToArray();
                foreach (var unlinker in unlinkers) unlinker.Dispose();
                foreach (var unlinker in unlinkers) unlinker.Dispose(); // make sure it's ok to dispose twice
            }

            // Validate sane behavior with a bad LinkTo
            new DelegatePropagator<int, int>
            {
                LinkToDelegate = (_, __) => null
            }.AsObservable().Subscribe(DataflowBlock.NullTarget<int>().AsObserver()).Dispose();
        }

        [Fact]
        public async Task TestAsObservableAndAsObserver_AllObserversGetData()
        {
            int total = 0;
            var options = new ExecutionDataflowBlockOptions { TaskScheduler = new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler };
            ITargetBlock<int>[] targets = Enumerable.Range(0, 3).Select(_ => new ActionBlock<int>(i => total += i, options)).ToArray();

            var source = new BufferBlock<int>();
            var sourceObservable = source.AsObservable();
            foreach (var target in targets)
            {
                sourceObservable.Subscribe(target.AsObserver());
            }

            int expectedTotal = 0;
            for (int i = 1; i <= 10; i++)
            {
                expectedTotal += i * targets.Length;
                source.Post(i);
            }
            source.Complete();

            await source.Completion;
            foreach (var target in targets)
            {
                await target.Completion;
            }
            Assert.Equal(expected: expectedTotal, actual: total);
        }

        [Fact]
        [OuterLoop] // stress test
        [ActiveIssue("https://github.com/dotnet/runtime/issues/46566", TestPlatforms.Browser)]
        public void TestAsObservableAndAsObserver_AsObservableDoesntLeak()
        {
            const int Count = 1000;

            var blockReferences = new WeakReference<BufferBlock<int>>[Count];

            for (int i = 0; i < Count; i++)
            {
                var b = new BufferBlock<int>();
                var o = b.AsObservable();
                blockReferences[i] = new WeakReference<BufferBlock<int>>(b);
                b = null;
                o = null;
            }

            for (int i = 0; i < 1; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            int remaining = blockReferences.Count(wr => {
                BufferBlock<int> b;
                return wr.TryGetTarget(out b);
            });
            Assert.True(remaining <= 1);
        }

        [Fact]
        public async Task TestAsObservableAndAsObserver_BroadcastBackPressure()
        {
            var source = new BufferBlock<int>();
            var targets = new[]
            {
                new BufferBlock<int>(),
                new BufferBlock<int>(new DataflowBlockOptions() { BoundedCapacity = 1 }), // This target will apply back pressure
                new BufferBlock<int>()
            };

            // Link the observable to the observers
            var observable = source.AsObservable();
            foreach (var target in targets)
                observable.Subscribe(target.AsObserver());

            // Post first message. Since there is no pressure yet, all targets should accept it.
            source.Post(1);
            Task<int> first = targets[0].ReceiveAsync();
            Task<bool> second = targets[1].OutputAvailableAsync();
            Task<int> third = targets[2].ReceiveAsync();
            await Task.WhenAll(first, second, third);
            Assert.Equal(expected: 1, actual: first.Result);
            Assert.True(second.Result);
            Assert.Equal(expected: 1, actual: third.Result);

            // Post second message. Target2 will postpone, but target1 and target3 should accept it.
            source.Post(2);
            await Task.WhenAll(targets[0].ReceiveAsync(), targets[2].ReceiveAsync());

            // Post third message. No target should be offered the message, because the source should be waiting for target2 to accept second message.
            source.Post(3);
            Assert.False(targets[0].OutputAvailableAsync().IsCompleted); // there's a race here such that this test might be a nop, but
            Assert.False(targets[2].OutputAvailableAsync().IsCompleted); // there's no good way to test the absence of a push

            // Unblock target2 to let third message propagate. Then all targets should end up with a message.
            await Task.WhenAll(targets[1].ReceiveAsync(), targets[1].ReceiveAsync()); // clear two items from second target
            await Task.WhenAll(from target in targets select target.ReceiveAsync()); // clear third item from all targets

            // Complete the source which should complete all the targets
            source.Complete();
            await source.Completion;
            await Task.WhenAll(from target in targets select target.Completion);
        }

        [Fact]
        public async Task TestAsObservableAndAsObserver_BroadcastFaultyTarget()
        {
            var targets = new ITargetBlock<int>[]
            {
                new BufferBlock<int>(),
                new DelegatePropagator<int, int>() { OfferMessageDelegate = delegate { throw new InvalidOperationException(); } },
                new BufferBlock<int>()
            };

            var source = new BufferBlock<int>();
            foreach (var target in targets) source.AsObservable().Subscribe(target.AsObserver());

            source.Post(1);

            await Task.WhenAll(
                Assert.ThrowsAsync<AggregateException>(() => targets[0].Completion),
                Assert.ThrowsAsync<AggregateException>(() => targets[2].Completion));
        }

        [Fact]
        public void TestLinkTo_ArgumentValidation()
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void TestLinkTo_TwoPhaseCommit()
        {
            var source1 = new BufferBlock<int>();
            var source2 = new BufferBlock<int>();
            var jb = new JoinBlock<int, int>(new GroupingDataflowBlockOptions { Greedy = false, MaxNumberOfGroups = 1 });

            source1.Completion.ContinueWith(_ => jb.Target1.Complete(), TaskScheduler.Default);
            source2.Completion.ContinueWith(_ => jb.Target2.Complete(), TaskScheduler.Default);

            source1.LinkTo(jb.Target1);
            source2.LinkTo(jb.Target2);

            source1.Post(42);
            source2.Post(43);

            source1.Complete();
            source2.Complete();

            var tuple = jb.Receive();
            Assert.Equal(expected: 42, actual: tuple.Item1);
            Assert.Equal(expected: 43, actual: tuple.Item2);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task TestLinkTo_DoubleLinking()
        {
            foreach (bool greedy in DataflowTestHelpers.BooleanValues)
            foreach (bool append in DataflowTestHelpers.BooleanValues)
            {
                var source1 = new BufferBlock<int>();
                var source2 = new BufferBlock<int>();
                var jb = new JoinBlock<int, int>(new GroupingDataflowBlockOptions { MaxNumberOfGroups = 1, Greedy = greedy });

                var ignored = source1.Completion.ContinueWith(_ => jb.Target1.Complete(), TaskScheduler.Default);
                ignored = source2.Completion.ContinueWith(_ => jb.Target2.Complete(), TaskScheduler.Default);

                using (source1.LinkTo(jb.Target1))
                {
                    source1.LinkTo(jb.Target1, new DataflowLinkOptions { Append = append }); // force NopLinkPropagator creation
                }
                using (source2.LinkTo(jb.Target2))
                {
                    source2.LinkTo(jb.Target2, new DataflowLinkOptions { Append = append }); // force NopLinkPropagator creation
                }

                source1.Post(42);
                source2.Post(43);

                source1.Complete();
                source2.Complete();

                var tuple = jb.Receive();
                Assert.Equal(expected: 42, actual: tuple.Item1);
                Assert.Equal(expected: 43, actual: tuple.Item2);
            }

            ITargetBlock<int> target = new ActionBlock<int>(i => { });
            ISourceBlock<int> source = new BufferBlock<int>();
            using (source.LinkTo(target))
            {
                source.LinkTo(target, new DataflowLinkOptions { PropagateCompletion = true });
                source.LinkTo(target, new DataflowLinkOptions { PropagateCompletion = true }, f => false);
            }
            source.Fault(new FormatException());
            await Assert.ThrowsAsync<AggregateException>(() => target.Completion);
        }

        [Fact]
        public async Task TestLinkTo_DoubleLinking_ValidPropagator()
        {
            bool tested = false;
            var tcs = new TaskCompletionSource<bool>();

            // Link a source to a target
            var source = new BufferBlock<int>();
            DelegatePropagator<int, int> target = null;
            target = new DelegatePropagator<int, int>
            {
                OfferMessageDelegate = (header, value, nopPropagator, consumeToAccept) =>
                {
                    if (!tested) // just run once; the release below could trigger an addition offering
                    {
                        // Make sure the nop propagator's Completion object is the same as that of the source
                        Assert.Same(expected: source.Completion, actual: nopPropagator.Completion);

                        // Make sure we can reserve and release through the propagator
                        Assert.True(nopPropagator.ReserveMessage(header, target));
                        nopPropagator.ReleaseReservation(header, target);

                        // Make sure its LinkTo doesn't work; that wouldn't make sense
                        Assert.Throws<NotSupportedException>(() => nopPropagator.LinkTo(DataflowBlock.NullTarget<int>(), new DataflowLinkOptions()));
                    }
                    return DataflowMessageStatus.Accepted;
                },
                CompleteDelegate = () => tcs.SetResult(true)
            };

            // Link from the source to the target, ensuring that we do so via a nop propagator
            using (source.LinkTo(target))
            {
                source.LinkTo(target, new DataflowLinkOptions { PropagateCompletion = true });
            }

            // Now put data in the source that it can propagator through the nop link
            source.Post(42);
            source.Complete();

            // Wait for everything to shut down.
            await source.Completion;
            await tcs.Task;
        }

        [Fact]
        public async Task TestLinkTo_BasicLinking()
        {
            foreach (bool propagateCompletion in DataflowTestHelpers.BooleanValues)
            {
                int counter = 0;
                var source = new BufferBlock<int>();
                var target = new ActionBlock<int>(i => counter++);

                using (source.LinkTo(target, new DataflowLinkOptions { PropagateCompletion = propagateCompletion }))
                {
                    source.PostRange(0, 2);
                    source.Complete();
                    await source.Completion;

                    if (propagateCompletion)
                    {
                        await target.Completion;
                        Assert.Equal(expected: 2, actual: counter);
                    }
                    else
                    {
                        Assert.False(target.Completion.IsCompleted);
                    }
                }
            }

            var completedSource = new BufferBlock<int>();
            completedSource.Complete();
            await completedSource.Completion;
            using (completedSource.LinkTo(DataflowBlock.NullTarget<int>()))
            using (completedSource.LinkTo(DataflowBlock.NullTarget<int>()))
            {
                // just make sure we can link while completed
            }
        }

        [Fact]
        public async Task TestLinkTo_Predicate()
        {
            int counter = 0;
            var source = new BufferBlock<int>();
            var target = new ActionBlock<int>(i => counter++);
            using (source.LinkTo(target, i => i % 2 == 0))
            using (source.LinkTo(DataflowBlock.NullTarget<int>()))
            {
                source.PostRange(0, 6);
                source.Complete();
                await source.Completion.ContinueWith(delegate { target.Complete(); }, TaskScheduler.Default);
                await target.Completion;
            }
            Assert.Equal(expected: 3, actual: counter);
        }

        [Fact]
        public async Task TestLinkTo_MaxMessages()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DataflowLinkOptions { MaxMessages = -2 });
            Assert.Throws<ArgumentOutOfRangeException>(() => new DataflowLinkOptions { MaxMessages = 0 });

            const int MaxMessages = 3, ExtraMessages = 2;

            for (int mode = 0; mode < 3; mode++)
            {
                int consumedMessages = 0, remainingMessages = 0;
                var options = new DataflowLinkOptions() { MaxMessages = MaxMessages };
                var source = new BufferBlock<int>();
                var target = new ActionBlock<int>(x => consumedMessages++);
                var otherTarget = new ActionBlock<int>(x => remainingMessages++);

                switch (mode)
                {
                    case 0:
                        source.LinkTo(target, options);
                        break;
                    case 1:
                        source.LinkTo(target, options, x => true); // Injects FilteredLinkPropagator
                        break;
                    case 2:
                        using (source.LinkTo(target)) source.LinkTo(target, options); // Injects NopLinkPropagator
                        break;
                }
                source.LinkTo(otherTarget);

                source.PostRange(0, MaxMessages + ExtraMessages);
                source.Complete();
                await source.Completion;

                target.Complete();
                otherTarget.Complete();
                await Task.WhenAll(target.Completion, otherTarget.Completion);

                Assert.Equal(expected: MaxMessages, actual: consumedMessages);
                Assert.Equal(expected: ExtraMessages, actual: remainingMessages);
            }
        }

        [Fact]
        public async Task TestLinkTo_Append()
        {
            var append = new DataflowLinkOptions() { Append = true, PropagateCompletion = true };
            var prepend = new DataflowLinkOptions() { Append = false, PropagateCompletion = true };

            var source = new BufferBlock<int>();
            var targets = new ActionBlock<int>[6];

            int[] consumedMessages = new int[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                int localI = i;
                targets[localI] = new ActionBlock<int>(x => consumedMessages[localI]++);
            }

            int lostMessages = 0;
            var extraTarget = new ActionBlock<int>(x => lostMessages++);

            // Link in a different order but use prepend/append to get them into expected/right order
            source.LinkTo(targets[2], prepend, x => x <= 2);
            source.LinkTo(targets[3], append, x => x <= 3);
            using (source.LinkTo(extraTarget, prepend))
            {
                source.LinkTo(targets[4], append, x => x <= 4);
                source.LinkTo(targets[1], prepend, x => x <= 1);
                using (source.LinkTo(extraTarget, append))
                {
                    source.LinkTo(targets[0], prepend, x => x <= 0);
                    source.LinkTo(targets[5], append, x => x <= 5);
                    using (source.LinkTo(extraTarget, prepend)) { }
                }
            }

            source.PostRange(0, targets.Length); // one message for each source
            source.Complete();
            await source.Completion;
            await Task.WhenAll(from target in targets select target.Completion);
            Assert.All(consumedMessages, i => Assert.Equal(expected: 1, actual: i));
            Assert.Equal(expected: 0, actual: lostMessages);
        }

        [Fact]
        public async Task TestLinkTo_PropagateCompletion()
        {
            IPropagatorBlock<int, int> source;
            ITargetBlock<int> target;

            source = new BufferBlock<int>();
            target = new ActionBlock<int>(i => { });
            source.LinkTo(target, new DataflowLinkOptions { PropagateCompletion = true });
            source.Complete();
            await target.Completion;

            source = new BufferBlock<int>();
            target = new ActionBlock<int>(i => { });
            source.LinkTo(target, new DataflowLinkOptions { PropagateCompletion = true });
            source.Fault(new InvalidOperationException());
            await Assert.ThrowsAsync<AggregateException>(() => target.Completion);
        }

        [Fact]
        public void TestSendAsync_ArgumentValidation()
        {
            Assert.Throws<ArgumentNullException>(() => { ((ITargetBlock<int>)null).SendAsync(42); });
        }

        [Fact]
        public void TestSendAsync_Immediate()
        {
            Task<bool> t;

            t = new BufferBlock<int>().SendAsync(42);
            Assert.True(t.IsCompleted);
            Assert.True(t.Result);

            var bb = new BufferBlock<int>();
            bb.Complete();
            t = bb.SendAsync(42);
            Assert.True(t.IsCompleted);
            Assert.False(t.Result);
        }

        [Fact]
        public async Task TestSendAsync_DelayedConsume()
        {
            var bb = new BufferBlock<int>(new DataflowBlockOptions { BoundedCapacity = 1 });
            Task<bool> t = bb.SendAsync(1);
            Assert.True(t.IsCompleted);
            Assert.True(t.Result);

            t = bb.SendAsync(2);
            Assert.False(t.IsCompleted);

            Assert.Equal(expected: 1, actual: await bb.ReceiveAsync());
            Assert.True(await t);

            t = bb.SendAsync(3);
            bb.Complete();
            Assert.Equal(expected: 2, actual: await bb.ReceiveAsync());
            Assert.False(await t);
        }

        [Fact]
        public async Task TestSendAsync_Canceled()
        {
            CancellationTokenSource cts;
            var bb = new BufferBlock<int>(new DataflowBlockOptions { BoundedCapacity = 1 });

            Task<bool> t = bb.SendAsync(1, new CancellationToken(canceled: true));
            Assert.True(t.IsCanceled);

            t = bb.SendAsync(2, new CancellationToken(canceled: false));
            Assert.True(t.IsCompleted);
            Assert.True(t.Result);

            cts = new CancellationTokenSource();
            t = bb.SendAsync(3, cts.Token);
            Assert.False(t.IsCompleted);
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);

            Assert.Equal(expected: 2, actual: await bb.ReceiveAsync());
            bb.Complete();
            await bb.Completion;

            foreach (bool withCancellation in DataflowTestHelpers.BooleanValues)
            {
                var target = new DelegatePropagator<int, int>();
                target.OfferMessageDelegate = (messageHeader, messageValue, source, consumeToAccept) => {
                    if (source == null)
                        return DataflowMessageStatus.Declined;

                    Assert.Equal(expected: withCancellation, actual: consumeToAccept);
                    if (consumeToAccept)
                    {
                        Assert.NotNull(source);
                        bool consumed;
                        source.ConsumeMessage(messageHeader, target, out consumed);
                        Assert.True(consumed);
                    }
                    return DataflowMessageStatus.Accepted;
                };

                t = withCancellation ?
                    target.SendAsync(1, new CancellationTokenSource().Token) :
                    target.SendAsync(2);
                Assert.True(await t);
            }
        }

        [Fact]
        public async Task TestSendAsync_ReserveRelease()
        {
            bool alreadyReservedReleased = false;

            foreach (bool withCancellation in DataflowTestHelpers.BooleanValues)
            {
                var cts = new CancellationTokenSource();

                DelegatePropagator<int, int> target = new DelegatePropagator<int, int>();
                target.OfferMessageDelegate = (messageHeader, messageValue, source, consumeToAccept) => {
                    Assert.True(messageHeader.IsValid);
                    Assert.Equal(expected: 42, actual: messageValue);

                    if (source == null)
                    {
                        return DataflowMessageStatus.Declined;
                    }

                    Assert.Equal(expected: withCancellation, actual: consumeToAccept);

                    if (!alreadyReservedReleased)
                    {
                        alreadyReservedReleased = true;
                        Task.Run(() => {
                            Assert.True(source.ReserveMessage(messageHeader, target));
                            if (withCancellation)
                                cts.Cancel();
                            source.ReleaseReservation(messageHeader, target);
                        });
                        return DataflowMessageStatus.Postponed;
                    }
                    else
                    {
                        return DataflowMessageStatus.Accepted;
                    }
                };

                if (withCancellation)
                {
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => target.SendAsync(42, cts.Token));
                }
                else
                {
                    Assert.True(await target.SendAsync(42));
                }
            }
        }

        [Fact]
        public async Task TestSendAsync_FaultyTarget()
        {
            var target = new DelegatePropagator<int, int>
            {
                OfferMessageDelegate = (header, value, source, consumeToAccept) => {
                    if (source == null)
                        return DataflowMessageStatus.Declined;
                    throw new FormatException();
                }
            };
            await Assert.ThrowsAsync<FormatException>(() => target.SendAsync(1));
        }

        [Fact]
        public void TestSendAsync_BehavesAsGoodSource()
        {
            ISourceBlock<int> sendSource = null;
            ITargetBlock<int> capturingTarget = new DelegatePropagator<int, int>
            {
                OfferMessageDelegate = delegate(DataflowMessageHeader header, int value, ISourceBlock<int> source, bool consumeToAccept) {
                    if (source == null)
                    {
                        return DataflowMessageStatus.Declined;
                    }
                    sendSource = source;
                    return DataflowMessageStatus.Postponed;
                }
            };

            Task<bool> sendTask = capturingTarget.SendAsync(42);
            Assert.False(sendTask.IsCompleted);
            Assert.NotNull(sendSource);

            DataflowTestHelpers.TestConsumeReserveReleaseArgumentsExceptions(sendSource);
            Assert.Throws<NotSupportedException>(() => sendSource.LinkTo(DataflowBlock.NullTarget<int>()));
            Assert.Throws<NotSupportedException>(() => sendSource.Fault(new Exception()));
            Assert.Throws<NotSupportedException>(() => sendSource.Complete());
        }

        [Fact]
        public async Task TestSendAsync_ConsumeCanceled()
        {
            var cts = new CancellationTokenSource();
            DelegatePropagator<int, int> target = null;
            target = new DelegatePropagator<int, int>
            {
                OfferMessageDelegate = delegate(DataflowMessageHeader header, int value, ISourceBlock<int> source, bool consumeToAccept) {
                    if (source == null)
                    {
                        return DataflowMessageStatus.Declined;
                    }

                    Assert.True(consumeToAccept);
                    cts.Cancel();

                    bool consumed;
                    int consumedMessage = source.ConsumeMessage(header, target, out consumed);
                    Assert.False(consumed);
                    Assert.Equal(expected: 0, actual: consumedMessage);
                    return DataflowMessageStatus.Postponed; // should really be NotAvailable, but doing so causes an (expected) assert in the product code
                }
            };
            Task<bool> send = target.SendAsync(42, cts.Token);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => send);
        }

        [Fact]
        public void TestReceive_ArgumentValidation()
        {
            var buffer = new BufferBlock<int>();
            int item;
            Assert.Throws<ArgumentNullException>(() => ((IReceivableSourceBlock<int>)null).TryReceive(out item));
            Assert.Throws<ArgumentNullException>(() => ((IReceivableSourceBlock<int>)null).Receive());
            Assert.Throws<ArgumentNullException>(() => ((IReceivableSourceBlock<int>)null).Receive(new CancellationToken(true)));
            Assert.Throws<ArgumentNullException>(() => { ((IReceivableSourceBlock<int>)null).ReceiveAsync(); });
            Assert.Throws<ArgumentNullException>(() => { ((IReceivableSourceBlock<int>)null).ReceiveAsync(new CancellationToken(true)); });
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Receive(TimeSpan.FromSeconds(-2)));
            Assert.Throws<ArgumentOutOfRangeException>(() => { buffer.ReceiveAsync(TimeSpan.FromSeconds(-2)); });
        }

        [Fact]
        public async Task TestReceive_AlreadyAvailable()
        {
            var buffer = new BufferBlock<int>();

            buffer.PostItems(1, 2, 3, 4);
            Assert.Equal(expected: 1, actual: buffer.Receive());
            Assert.Equal(expected: 3, actual: buffer.Count);
            Assert.Equal(expected: 2, actual: buffer.Receive(new CancellationTokenSource().Token));
            Assert.Equal(expected: 2, actual: buffer.Count);
            Assert.Equal(expected: 3, actual: buffer.Receive(TimeSpan.FromDays(1)));
            Assert.Equal(expected: 1, actual: buffer.Count);
            Assert.Equal(expected: 4, actual: buffer.Receive(TimeSpan.FromDays(1), new CancellationTokenSource().Token));
            Assert.Equal(expected: 0, actual: buffer.Count);

            buffer.PostItems(1, 2, 3, 4);
            Assert.Equal(expected: 1, actual: await buffer.ReceiveAsync());
            Assert.Equal(expected: 3, actual: buffer.Count);
            Assert.Equal(expected: 2, actual: await buffer.ReceiveAsync(new CancellationTokenSource().Token));
            Assert.Equal(expected: 2, actual: buffer.Count);
            Assert.Equal(expected: 3, actual: await buffer.ReceiveAsync(TimeSpan.FromDays(1)));
            Assert.Equal(expected: 1, actual: buffer.Count);
            Assert.Equal(expected: 4, actual: await buffer.ReceiveAsync(TimeSpan.FromDays(1), new CancellationTokenSource().Token));
            Assert.Equal(expected: 0, actual: buffer.Count);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task TestReceive_NotYetAvailable()
        {
            var buffer = new BufferBlock<int>();

            // The following test is racy, but just in terms
            // of what's being tested.  The test should always succeed
            // regardless, but we might actually be testing receiving
            // an already-available value rather than one not yet available.
            var ignored = Task.Run(() => buffer.Post(1));
            Assert.Equal(expected: 1, actual: buffer.Receive());
            ignored = Task.Run(() => buffer.Post(2));
            Assert.Equal(expected: 2, actual: buffer.Receive(new CancellationTokenSource().Token));
            ignored = Task.Run(() => buffer.Post(3));
            Assert.Equal(expected: 3, actual: buffer.Receive(TimeSpan.FromDays(1)));
            ignored = Task.Run(() => buffer.Post(4));
            Assert.Equal(expected: 4, actual: buffer.Receive(TimeSpan.FromDays(1), new CancellationTokenSource().Token));

            // The following are non-racy.
            var t1 = buffer.ReceiveAsync();
            var t2 = buffer.ReceiveAsync(new CancellationTokenSource().Token);
            var t3 = buffer.ReceiveAsync(TimeSpan.FromDays(1));
            var t4 = buffer.ReceiveAsync(TimeSpan.FromDays(1), new CancellationTokenSource().Token);
            Assert.False(t1.IsCompleted);
            Assert.False(t2.IsCompleted);
            Assert.False(t3.IsCompleted);
            Assert.False(t4.IsCompleted);
            buffer.PostItems(3, 4, 5, 6);
            Assert.Equal(expected: 3, actual: await t1);
            Assert.Equal(expected: 4, actual: await t2);
            Assert.Equal(expected: 5, actual: await t3);
            Assert.Equal(expected: 6, actual: await t4);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop] // timeout involved
        public async Task TestReceive_Timeout()
        {
            var bb = new BufferBlock<int>();

            Assert.Throws<TimeoutException>(() => bb.Receive(TimeSpan.FromMilliseconds(1)));
            await Assert.ThrowsAsync<TimeoutException>(() => bb.ReceiveAsync(TimeSpan.FromMilliseconds(1)));

            var cts = new CancellationTokenSource();
            Assert.Throws<TimeoutException>(() => bb.Receive(TimeSpan.FromMilliseconds(1), cts.Token));
            await Assert.ThrowsAsync<TimeoutException>(() => bb.ReceiveAsync(TimeSpan.FromMilliseconds(1), cts.Token));
        }

        [Fact]
        public async Task TestReceive_TimeoutZero()
        {
            var bb = new BufferBlock<int>();
            Assert.Throws<TimeoutException>(() => bb.Receive(TimeSpan.FromMilliseconds(0)));
            await Assert.ThrowsAsync<TimeoutException>(() => bb.ReceiveAsync(TimeSpan.FromMilliseconds(0)));
        }


        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task TestReceive_Cancellation()
        {
            var bb = new BufferBlock<int>();

            // Cancel before Receive/ReceiveAsync
            Assert.ThrowsAny<OperationCanceledException>(() => bb.Receive(new CancellationToken(canceled: true)));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => bb.ReceiveAsync(new CancellationToken(canceled: true)));

            // Cancel after Receive/ReceiveAsync but before data
            {
                var cts = new CancellationTokenSource();
                var ignored = Task.Run(() => cts.Cancel()); // as elsewhere, this test should always succeed, but is racy as to what's being tested
                Assert.ThrowsAny<OperationCanceledException>(() => bb.Receive(cts.Token));
            }
            {
                var cts = new CancellationTokenSource();
                var t = bb.ReceiveAsync(cts.Token);
                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => bb.ReceiveAsync(cts.Token));
            }

            // Cancel after data received
            {
                var cts = new CancellationTokenSource();
                var ignored = Task.Run(() => bb.Post(1)); // as elsewhere, this test should always succeed, but is racy as to what's being tested
                Assert.Equal(expected: 1, actual: bb.Receive(cts.Token));
                cts.Cancel(); // just checking to make sure there's no exception
            }
            {
                var cts = new CancellationTokenSource();
                var t = bb.ReceiveAsync(cts.Token);
                bb.Post(2);
                Assert.Equal(expected: 2, actual: await t);
                cts.Cancel(); // just checking to make sure there's no exception            }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task TestReceive_CanceledSource()
        {
            foreach (bool beforeReceive in DataflowTestHelpers.BooleanValues)
            {
                var cts = new CancellationTokenSource();
                var bb = new BufferBlock<int>(new DataflowBlockOptions { CancellationToken = cts.Token });

                if (beforeReceive)
                {
                    cts.Cancel();
                }
                else
                {
                    var ignored = Task.Run(() => cts.Cancel()); // as elsewhere, this test should always succeed, but is racy as to what's being tested
                }
                Assert.Throws<InvalidOperationException>(() => bb.Receive());
            }

            foreach (bool beforeReceive in DataflowTestHelpers.BooleanValues)
            {
                var cts = new CancellationTokenSource();
                var bb = new BufferBlock<int>(new DataflowBlockOptions { CancellationToken = cts.Token });

                if (beforeReceive)
                {
                    cts.Cancel();
                }

                var t = bb.ReceiveAsync();
                if (!beforeReceive)
                {
                    cts.Cancel();
                }
                await Assert.ThrowsAsync<InvalidOperationException>(() => t);
            }
        }

        [Fact]
        public async Task TestReceiveAsync_ManyInOrder()
        {
            var bb = new BufferBlock<int>();

            Task<int>[] tasks = Enumerable.Range(0, 100).Select(_ => bb.ReceiveAsync()).ToArray();
            Assert.All(tasks, t => Assert.False(t.IsCompleted));

            bb.PostRange(0, tasks.Length);

            for (int i = 0; i < tasks.Length; i++)
            {
                Assert.Equal(expected: i, actual: await tasks[i]);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/38817", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoInterpreter))]
        [SkipOnPlatform(TestPlatforms.Browser, "uses a lot of stack")]
        public async Task TestReceiveAsync_LongChain()
        {
            const int Length = 10000;

            var bb = new BufferBlock<int>();

            Task t = bb.ReceiveAsync();
            for (int i = 1; i < Length; i++)
            {
                t = t.ContinueWith(_ => bb.ReceiveAsync(),
                    CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
            }

            bb.PostRange(0, Length);

            await t;
        }

        [Fact]
        public void TestReceive_WellBehavingTarget()
        {
            ITargetBlock<int> receiveTarget = null;
            var source = new DelegateReceivablePropagator<int, int>
            {
                LinkToDelegate = (target, options) => {
                    receiveTarget = target;
                    return new DelegateDisposable();
                }
            };
            Task<int> receiveTask = source.ReceiveAsync();
            Assert.NotNull(receiveTarget);
            DataflowTestHelpers.TestOfferMessage_ArgumentValidation(receiveTarget);
            Assert.Throws<NotSupportedException>(() => { var ignored = receiveTarget.Completion; });
            receiveTarget.Fault(new Exception()); // shouldn't throw
        }

        [Fact]
        public async Task TestReceive_FaultySourceConsume()
        {
            ITargetBlock<int> receiveTarget = null;
            var source = new DelegateReceivablePropagator<int, int>
            {
                LinkToDelegate = (target, options) => {
                    receiveTarget = target;
                    return new DelegateDisposable();
                },
                ConsumeMessageDelegate = delegate(DataflowMessageHeader messageHeader, ITargetBlock<int> target, out bool messageConsumed) {
                    throw new FormatException();
                }
            };
            Task<int> receiveTask = source.ReceiveAsync();
            Assert.NotNull(receiveTarget);
            receiveTarget.OfferMessage(new DataflowMessageHeader(1), 1, source, consumeToAccept: true);
            await Assert.ThrowsAsync<FormatException>(() => receiveTask);
        }

        [Fact]
        public async Task TestReceive_FaultySourceTryReceive()
        {
            var source = new DelegateReceivablePropagator<int, int>
            {
                TryReceiveDelegate = delegate(Predicate<int> filter, out int item) {
                    throw new InvalidProgramException();
                }
            };
            Task<int> receiveTask = source.ReceiveAsync();
            await Assert.ThrowsAsync<InvalidProgramException>(() => receiveTask);
        }

        [Fact]
        public void TestChoose_ArgumentValidation()
        {
            Assert.Throws<ArgumentNullException>(
                () => { DataflowBlock.Choose<int, int>(null, i => { }, new BufferBlock<int>(), i => { }); });
            Assert.Throws<ArgumentNullException>(
                () => { DataflowBlock.Choose<int, int>(new BufferBlock<int>(), i => { }, null, i => { }); });
            Assert.Throws<ArgumentNullException>(
                () => { DataflowBlock.Choose<int, int>(new BufferBlock<int>(), null, new BufferBlock<int>(), i => { }); });
            Assert.Throws<ArgumentNullException>(
                () => { DataflowBlock.Choose<int, int>(new BufferBlock<int>(), i => { }, new BufferBlock<int>(), null); });
            Assert.Throws<ArgumentNullException>(
                () => { DataflowBlock.Choose<int, int>(new BufferBlock<int>(), i => { }, new BufferBlock<int>(), i => { }, null); });

            Assert.Throws<ArgumentNullException>(
                () => { DataflowBlock.Choose<int, int, int>(null, i => { }, new BufferBlock<int>(), i => { }, new BufferBlock<int>(), i => { }); });
            Assert.Throws<ArgumentNullException>(
                () => { DataflowBlock.Choose<int, int, int>(new BufferBlock<int>(), i => { }, null, i => { }, new BufferBlock<int>(), i => { }); });
            Assert.Throws<ArgumentNullException>(
                () => { DataflowBlock.Choose<int, int, int>(new BufferBlock<int>(), i => { }, new BufferBlock<int>(), i => { }, null, i => { }); });
            Assert.Throws<ArgumentNullException>(
                () => { DataflowBlock.Choose<int, int, int>(new BufferBlock<int>(), null, new BufferBlock<int>(), i => { }, new BufferBlock<int>(), i => { }); });
            Assert.Throws<ArgumentNullException>(
                () => { DataflowBlock.Choose<int, int, int>(new BufferBlock<int>(), i => { }, new BufferBlock<int>(), null, new BufferBlock<int>(), i => { }); });
            Assert.Throws<ArgumentNullException>(
                () => { DataflowBlock.Choose<int, int, int>(new BufferBlock<int>(), i => { }, new BufferBlock<int>(), i => { }, new BufferBlock<int>(), null); });
            Assert.Throws<ArgumentNullException>(
                () => { DataflowBlock.Choose<int, int, int>(new BufferBlock<int>(), i => { }, new BufferBlock<int>(), i => { }, new BufferBlock<int>(), i => { }, null); });
        }

        [Fact]
        public async Task TestChoose2_BasicFunctionality()
        {
            for (int chooseTestCase = 0; chooseTestCase < 5; chooseTestCase++)
            {
                var cesp = new ConcurrentExclusiveSchedulerPair(); // ensure BufferBlocks are serialized
                var source1 = new BufferBlock<int>(new DataflowBlockOptions { TaskScheduler = cesp.ExclusiveScheduler });
                var source2 = new BufferBlock<string>(new DataflowBlockOptions { TaskScheduler = cesp.ExclusiveScheduler });

                int intValue = 0;
                string stringValue = null;
                TaskScheduler usedScheduler = null, requestedScheduler = new ConcurrentExclusiveSchedulerPair().ConcurrentScheduler;
                var cts = new CancellationTokenSource();

                var t = chooseTestCase < 3 ?
                    DataflowBlock.Choose(
                        source1, i => intValue = i,
                        source2, s => stringValue = s) :
                    DataflowBlock.Choose(
                        source1, i => usedScheduler = TaskScheduler.Current,
                        source2, s => usedScheduler = TaskScheduler.Current,
                        new DataflowBlockOptions { TaskScheduler = requestedScheduler, MaxMessagesPerTask = 1, CancellationToken = cts.Token });

                switch (chooseTestCase)
                {
                    case 0: // Test data on the first source
                        source1.PostItems(42, 43);
                        Assert.Equal(expected: 0, actual: await t);
                        Assert.Equal(expected: 42, actual: intValue);
                        Assert.Null(stringValue);
                        Assert.Equal(expected: 1, actual: source1.Count);
                        break;

                    case 1: // Test data on the second source
                        source2.PostItems("44", "45");
                        Assert.Equal(expected: 1, actual: await t);
                        Assert.Equal(expected: 0, actual: intValue);
                        Assert.Equal(expected: "44", actual: stringValue);
                        Assert.Equal(expected: 1, actual: source2.Count);
                        break;

                    case 2: // Test no data on either source
                        source1.Complete();
                        source2.Complete();
                        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);
                        Assert.Equal(expected: 0, actual: intValue);
                        Assert.Null(stringValue);
                        break;

                    // >= 3 TEST USING DATAFLOW BLOCK OPTIONS

                    case 3: // Test correct TaskScheduler is used
                        source1.Post(42);
                        await t;
                        Assert.Equal(expected: requestedScheduler, actual: usedScheduler);
                        break;

                    case 4: // Test cancellation takes effect
                        cts.Cancel();
                        source1.Post(42);
                        source2.Post("43");
                        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);
                        Assert.Equal(expected: 1, actual: source1.Count);
                        Assert.Equal(expected: 1, actual: source2.Count);
                        break;
                }
            }
        }

        [Fact]
        public async Task TestChoose2_DataAlreadyAvailable()
        {
            for (int i = 0; i < 3; i++)
            {
                var sources = new[] { new BufferBlock<int>(), new BufferBlock<int>() };
                switch (i)
                {
                    case 0:
                        sources[0].Post(1);
                        break;
                    case 1:
                        sources[1].Post(2);
                        break;
                    case 2:
                        sources[0].Post(3);
                        sources[1].Post(4);
                        break;
                }

                int item = 0;
                int branch = await DataflowBlock.Choose(
                    sources[0], v => item = v,
                    sources[1], v => item = v);

                switch (i)
                {
                    case 0:
                        Assert.Equal(expected: 0, actual: branch);
                        Assert.Equal(expected: 0, actual: sources[0].Count);
                        Assert.Equal(expected: 1, actual: item);
                        break;
                    case 1:
                        Assert.Equal(expected: 1, actual: branch);
                        Assert.Equal(expected: 0, actual: sources[1].Count);
                        Assert.Equal(expected: 2, actual: item);
                        break;
                    case 2:
                        Assert.Equal(expected: 0, actual: branch);
                        Assert.Equal(expected: 0, actual: sources[0].Count);
                        Assert.Equal(expected: 1, actual: sources[1].Count);
                        Assert.Equal(expected: 3, actual: item);
                        break;
                }
            }
        }

        [Fact]
        public async Task TestChoose3_DataAlreadyAvailable()
        {
            for (int i = 0; i < 4; i++)
            {
                var sources = new[] { new BufferBlock<int>(), new BufferBlock<int>(), new BufferBlock<int>() };
                switch (i)
                {
                    case 0:
                        sources[0].Post(1);
                        break;
                    case 1:
                        sources[1].Post(2);
                        break;
                    case 2:
                        sources[2].Post(3);
                        break;
                    case 3:
                        sources[0].Post(4);
                        sources[1].Post(5);
                        sources[2].Post(6);
                        break;
                }

                int item = 0;
                int branch = await DataflowBlock.Choose(
                    sources[0], v => item = v,
                    sources[1], v => item = v,
                    sources[2], v => item = v);

                switch (i)
                {
                    case 0:
                        Assert.Equal(expected: 0, actual: branch);
                        Assert.Equal(expected: 0, actual: sources[0].Count);
                        Assert.Equal(expected: 1, actual: item);
                        break;
                    case 1:
                        Assert.Equal(expected: 1, actual: branch);
                        Assert.Equal(expected: 0, actual: sources[1].Count);
                        Assert.Equal(expected: 2, actual: item);
                        break;
                    case 2:
                        Assert.Equal(expected: 2, actual: branch);
                        Assert.Equal(expected: 0, actual: sources[2].Count);
                        Assert.Equal(expected: 3, actual: item);
                        break;
                    case 3:
                        Assert.Equal(expected: 0, actual: branch);
                        Assert.Equal(expected: 0, actual: sources[0].Count);
                        Assert.Equal(expected: 1, actual: sources[1].Count);
                        Assert.Equal(expected: 1, actual: sources[2].Count);
                        Assert.Equal(expected: 4, actual: item);
                        break;
                }
            }
        }

        [Fact]
        public async Task TestChoose2_Exceptions()
        {
            for (int test = 0; test < 2; test++)
            {
                var cesp = new ConcurrentExclusiveSchedulerPair(); // ensure BufferBlocks are serialized
                var source1 = new BufferBlock<int>(new DataflowBlockOptions { TaskScheduler = cesp.ExclusiveScheduler });
                var source2 = new BufferBlock<string>(new DataflowBlockOptions { TaskScheduler = cesp.ExclusiveScheduler });
                var t = DataflowBlock.Choose(
                    source1, i => { throw new InvalidOperationException(); },
                    source2, s => { throw new InvalidCastException(); });
                Assert.False(t.IsCompleted);

                switch (test)
                {
                    case 1:
                        source1.Post(42);
                        source2.Post("43");
                        await Assert.ThrowsAsync<InvalidOperationException>(() => t);
                        Assert.Equal(expected: 0, actual: source1.Count);
                        Assert.Equal(expected: 1, actual: source2.Count);
                        break;

                    case 2:
                        source2.Post("43");
                        source1.Post(42);
                        await Assert.ThrowsAsync<InvalidCastException>(() => t);
                        Assert.Equal(expected: 1, actual: source1.Count);
                        Assert.Equal(expected: 0, actual: source2.Count);
                        break;
                }
            }
        }

        [Fact]
        public async Task TestChoose3_BasicFunctionality()
        {
            for (int chooseTestCase = 0; chooseTestCase < 5; chooseTestCase++)
            {
                var cesp = new ConcurrentExclusiveSchedulerPair(); // ensure BufferBlocks are serialized
                var source1 = new BufferBlock<int>(new DataflowBlockOptions { TaskScheduler = cesp.ExclusiveScheduler });
                var source2 = new BufferBlock<string>(new DataflowBlockOptions { TaskScheduler = cesp.ExclusiveScheduler });
                var source3 = new BufferBlock<double>(new DataflowBlockOptions { TaskScheduler = cesp.ExclusiveScheduler });
                int intValue = 0;
                string stringValue = null;
                double doubleValue = 0.0;

                TaskScheduler usedScheduler = null, requestedScheduler = new ConcurrentExclusiveSchedulerPair().ConcurrentScheduler;
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
                    case 0: // Test data on the first source
                        source1.PostItems(42, 43);
                        Assert.Equal(expected: 0, actual: await t);
                        Assert.Equal(expected: 42, actual: intValue);
                        Assert.Null(stringValue);
                        Assert.Equal(expected: 0, actual: doubleValue);
                        Assert.Equal(expected: 1, actual: source1.Count);
                        break;

                    case 1: // Test data on the second source
                        source2.PostItems("42", "43");
                        Assert.Equal(expected: 1, actual: await t);
                        Assert.Equal(expected: "42", actual: stringValue);
                        Assert.Equal(expected: 0, actual: intValue);
                        Assert.Equal(expected: 0, actual: doubleValue);
                        Assert.Equal(expected: 1, actual: source2.Count);
                        break;

                    case 2: // Test data on the third source
                        source3.PostItems(42.0, 43.0);
                        Assert.Equal(expected: 2, actual: await t);
                        Assert.Equal(expected: 42.0, actual: doubleValue);
                        Assert.Equal(expected: 0, actual: intValue);
                        Assert.Null(stringValue);
                        Assert.Equal(expected: 1, actual: source3.Count);
                        break;

                    case 3: // Test first source complete and data on second
                        source1.Complete();
                        source2.Post("42");
                        Assert.Equal(expected: 1, actual: await t);
                        Assert.Equal(expected: "42", actual: stringValue);
                        Assert.Equal(expected: 0, actual: intValue);
                        Assert.Equal(expected: 0, actual: doubleValue);
                        Assert.Equal(expected: 0, actual: source2.Count);
                        break;

                    case 4: // Test second source complete and data on third
                        source2.Complete();
                        source3.Post(42.0);
                        Assert.Equal(expected: 2, actual: await t);
                        Assert.Equal(expected: 42.0, actual: doubleValue);
                        Assert.Equal(expected: 0, actual: intValue);
                        Assert.Null(stringValue);
                        Assert.Equal(expected: 0, actual: source3.Count);
                        break;

                    case 5: // Test third source complete and data on first
                        source3.Complete();
                        source1.Post(42);
                        Assert.Equal(expected: 0, actual: await t);
                        Assert.Equal(expected: 42, actual: intValue);
                        Assert.Null(stringValue);
                        Assert.Equal(expected: 0, actual: doubleValue);
                        Assert.Equal(expected: 0, actual: source1.Count);

                        break;

                    case 6: // Test all sources complete
                        source1.Complete();
                        source2.Complete();
                        source3.Complete();
                        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);
                        Assert.Equal(expected: 0, actual: intValue);
                        Assert.Null(stringValue);
                        Assert.Equal(expected: 0, actual: doubleValue);
                        break;

                    // >= 7 TEST USING DATAFLOW BLOCK OPTIONS

                    case 7: // Test correct TaskScheduler is used
                        source3.Post(42);
                        await t;
                        Assert.Equal(expected: requestedScheduler, actual: usedScheduler);
                        break;

                    case 8: // Test cancellation before data takes effect
                        cts.Cancel();
                        source1.Post(42);
                        source2.Post("43");
                        source3.Post(44.0);
                        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);
                        Assert.Equal(expected: 1, actual: source1.Count);
                        Assert.Equal(expected: 1, actual: source2.Count);
                        Assert.Equal(expected: 1, actual: source3.Count);
                        break;
                }
            }
        }

        [Fact]
        public async Task TestChoose3_Exceptions()
        {
            for (int test = 1; test <= 3; test++)
            {
                var cesp = new ConcurrentExclusiveSchedulerPair(); // ensure BufferBlocks are serialized
                var source1 = new BufferBlock<int>(new DataflowBlockOptions { TaskScheduler = cesp.ExclusiveScheduler });
                var source2 = new BufferBlock<string>(new DataflowBlockOptions { TaskScheduler = cesp.ExclusiveScheduler });
                var source3 = new BufferBlock<double>(new DataflowBlockOptions { TaskScheduler = cesp.ExclusiveScheduler });
                var t = DataflowBlock.Choose(
                    source1, i => { throw new InvalidOperationException(); },
                    source2, s => { throw new InvalidCastException(); },
                    source3, d => { throw new FormatException(); });
                Assert.False(t.IsCompleted);

                switch (test)
                {
                    case 1:
                        source1.Post(42);
                        source2.Post("43");
                        source3.Post(44.0);
                        await Assert.ThrowsAsync<InvalidOperationException>(() => t);
                        Assert.Equal(expected: 0, actual: source1.Count);
                        Assert.Equal(expected: 1, actual: source2.Count);
                        Assert.Equal(expected: 1, actual: source3.Count);
                        break;

                    case 2:
                        source2.Post("43");
                        source1.Post(42);
                        source3.Post(44.0);
                        await Assert.ThrowsAsync<InvalidCastException>(() => t);
                        Assert.Equal(expected: 1, actual: source1.Count);
                        Assert.Equal(expected: 0, actual: source2.Count);
                        Assert.Equal(expected: 1, actual: source3.Count);
                        break;

                    case 3:
                        source3.Post(44.0);
                        source1.Post(42);
                        source2.Post("43");
                        await Assert.ThrowsAsync<FormatException>(() => t);
                        Assert.Equal(expected: 1, actual: source1.Count);
                        Assert.Equal(expected: 1, actual: source2.Count);
                        Assert.Equal(expected: 0, actual: source3.Count);
                        break;
                }
            }
        }

        [Fact]
        public async Task TestChoose_LinkTracking()
        {
            for (int n = 2; n <= 3; n++)
            {
                foreach (bool cancelBeforeChoose in DataflowTestHelpers.BooleanValues)
                {
                    int[] linkCounts = new int[n], unlinkCounts = new int[n];
                    ISourceBlock<int>[] sources = Enumerable.Range(0, n).Select(i => new DelegatePropagator<int, int>
                    {
                        LinkToDelegate = (target, linkOptions) => {
                            Interlocked.Increment(ref linkCounts[i]);
                            return new DelegateDisposable { DisposeDelegate = () => Interlocked.Increment(ref unlinkCounts[i]) };
                        }
                    }).ToArray();

                    var cts = new CancellationTokenSource();
                    if (cancelBeforeChoose)
                        cts.Cancel();

                    var options = new DataflowBlockOptions { MaxMessagesPerTask = 1, CancellationToken = cts.Token };
                    Action<int> nop = x => { };
                    Task<int> choose = n == 2 ?
                        DataflowBlock.Choose(sources[0], nop, sources[1], nop, options) :
                        DataflowBlock.Choose(sources[0], nop, sources[1], nop, sources[2], nop, options);

                    if (!cancelBeforeChoose)
                        cts.Cancel();

                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => choose);

                    int expectedLinkCount = cancelBeforeChoose ? 0 : 1;
                    Assert.All(linkCounts, i => Assert.Equal(expected: expectedLinkCount, actual: i));
                    Assert.All(unlinkCounts, i => Assert.Equal(expected: expectedLinkCount, actual: i));
                }
            }
        }

        [Fact]
        public async Task TestChoose_ManyConcurrent()
        {
            const int iterations = 1000;

            BufferBlock<int> source1 = new BufferBlock<int>(), source2 = new BufferBlock<int>();
            int branch1 = 0, branch2 = 0;
            Task<int>[] tasks = Enumerable.Range(0, iterations).Select(_ =>
                DataflowBlock.Choose(source1, x => Interlocked.Increment(ref branch1), source2, x => Interlocked.Increment(ref branch2))).ToArray();

            foreach (Task task in tasks)
            {
                source1.Post(0);
                source2.Post(0);
            }

            await Task.WhenAll(tasks);
            Assert.Equal(expected: tasks.Length, actual: branch1 + branch2);
            Assert.Equal(expected: tasks.Length, actual: source1.Count + source2.Count);
        }

        [Fact]
        public void TestChoose_WellBehavingTarget()
        {
            ITargetBlock<int> chooseTarget = null;
            var source = new DelegateReceivablePropagator<int, int>
            {
                LinkToDelegate = (target, options) => {
                    chooseTarget = target;
                    return new DelegateDisposable();
                }
            };
            Task<int> chooseTask = DataflowBlock.Choose(source, i => { }, source, i => { });
            Assert.NotNull(chooseTarget);
            DataflowTestHelpers.TestOfferMessage_ArgumentValidation(chooseTarget);
            Assert.Throws<NotSupportedException>(() => { var ignored = chooseTarget.Completion; });
            chooseTarget.Fault(new Exception()); // shouldn't throw
        }

        [Fact]
        public void TestChoose_FaultySource()
        {
            var source = new DelegateReceivablePropagator<int, int> {
                TryReceiveDelegate = delegate(Predicate<int> filter, out int item) {
                    throw new FormatException();
                }
            };
            Task<int> t = DataflowBlock.Choose(source, i => { }, source, i => { });
            Assert.Throws<FormatException>(() => t.GetAwaiter().GetResult());
        }

        [Fact]
        public async Task TestChoose_ConsumeToAccept()
        {
            var bb = new BroadcastBlock<int>(i => i * 2);
            Task<int> t = DataflowBlock.Choose(bb, i => { }, bb, i => { });
            Assert.False(t.IsCompleted);
            bb.Post(42);
            await t;
        }

        [Fact]
        public void TestEncapsulate_ArgumentValidation()
        {
            Assert.Throws<ArgumentNullException>(
                () => DataflowBlock.Encapsulate<int, int>(null, new BufferBlock<int>()));
            Assert.Throws<ArgumentNullException>(
                () => DataflowBlock.Encapsulate<int, int>(new BufferBlock<int>(), null));
            Assert.Throws<ArgumentNullException>(
                () => DataflowBlock.Encapsulate<int, int>(new BufferBlock<int>(), new BufferBlock<int>()).Fault(null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void TestEncapsulate_LinkingAndUnlinking()
        {
            var buffer = new BufferBlock<int>();
            var action = new ActionBlock<int>(i => buffer.Post(i));
            action.Completion.ContinueWith(delegate { buffer.Complete(); }, TaskScheduler.Default);
            IPropagatorBlock<int, int> encapsulated = DataflowBlock.Encapsulate(action, buffer);

            var endTarget = new BufferBlock<int>();

            IDisposable unlink = encapsulated.LinkTo(endTarget);
            Assert.NotNull(unlink);

            IDisposable unlink2 = encapsulated.LinkTo(endTarget);
            Assert.NotNull(unlink);

            action.Post(1);
            Assert.Equal(expected: 1, actual: endTarget.Receive());

            unlink.Dispose();
            action.Post(2);
            Assert.Equal(expected: 2, actual: endTarget.Receive());

            unlink2.Dispose();
        }

        [Fact]
        public async Task TestEncapsulate_EncapsulateBoundedTarget()
        {
            // source->||BoundedTransform->buffer||->sink

            int messagesReceived = 0;
            var transform = new TransformBlock<int, int>(x => {
                messagesReceived++;
                return x;
            }, new ExecutionDataflowBlockOptions() { BoundedCapacity = 1 });

            var buffer = new BufferBlock<int>();
            transform.LinkTo(buffer);
            var ignored = transform.Completion.ContinueWith(completion => buffer.Complete(), TaskScheduler.Default);

            IPropagatorBlock<int, int> encapsulated = DataflowBlock.Encapsulate(transform, buffer);
            encapsulated.LinkTo(new ActionBlock<int>(x => { }));

            var source = new BufferBlock<int>();
            source.LinkTo(encapsulated);
            ignored = source.Completion.ContinueWith(completion => encapsulated.Complete(), TaskScheduler.Default);

            // Feed
            const int messagesSent = 10;
            source.PostRange(0, messagesSent);
            source.Complete();

            await encapsulated.Completion;
            Assert.Equal(messagesReceived, messagesSent);
        }

        [Fact]
        public async Task TestEncapsulate_ReserveAndRelease()
        {
            var buffer1 = new BufferBlock<int>();
            var action1 = new ActionBlock<int>(i => buffer1.Post(i));
            var ignored = action1.Completion.ContinueWith(delegate { buffer1.Complete(); }, TaskScheduler.Default);
            IPropagatorBlock<int, int> encapsulated1 = DataflowBlock.Encapsulate(action1, buffer1);

            var buffer2 = new BufferBlock<string>();
            var action2 = new ActionBlock<string>(i => buffer2.Post(i));
            ignored = action2.Completion.ContinueWith(delegate { buffer2.Complete(); }, TaskScheduler.Default);
            IPropagatorBlock<string, string> encapsulated2 = DataflowBlock.Encapsulate(action2, buffer2);

            var join = new JoinBlock<int, string>(new GroupingDataflowBlockOptions { Greedy = false });
            encapsulated1.LinkTo(join.Target1, new DataflowLinkOptions { PropagateCompletion = true });
            encapsulated2.LinkTo(join.Target2, new DataflowLinkOptions { PropagateCompletion = true });

            for (int i = 0; i < 2; i++)
            {
                encapsulated1.Post(1);
                encapsulated2.Post("2");
                Tuple<int, string> result = await join.ReceiveAsync();
            }

            encapsulated1.Complete();
            encapsulated2.Complete();
            await join.Completion;
        }

        [Fact]
        public async Task TestEncapsulate_Receives()
        {
            var buffer = new BufferBlock<int>();
            var action1 = new ActionBlock<int>(i => buffer.Post(i));
            var action2 = new ActionBlock<int>(i => buffer.Post(i));
            IPropagatorBlock<int, int> encapsulated1 = DataflowBlock.Encapsulate(action1, buffer);

            encapsulated1.PostItems(1, 2, 3);
            encapsulated1.Complete();
            await action1.Completion;

            IList<int> items;
            Assert.True(((IReceivableSourceBlock<int>)encapsulated1).TryReceiveAll(out items));
            Assert.Equal(expected: 3, actual: items.Count);
            Assert.Equal(expected: 1, actual: items[0]);
            Assert.Equal(expected: 2, actual: items[1]);
            Assert.Equal(expected: 3, actual: items[2]);

            IPropagatorBlock<int, int> encapsulated2 = DataflowBlock.Encapsulate(action2, buffer);
            encapsulated2.PostItems(4, 5, 6);
            encapsulated2.Complete();
            await action2.Completion;

            int item;
            Assert.False(((IReceivableSourceBlock<int>)encapsulated2).TryReceive(f => false, out item));
            Assert.True(((IReceivableSourceBlock<int>)encapsulated2).TryReceive(out item));
            Assert.Equal(expected: 4, actual: item);
            Assert.Equal(expected: 5, actual: ((IReceivableSourceBlock<int>)encapsulated2).Receive());
            Assert.Equal(expected: 6, actual: await ((IReceivableSourceBlock<int>)encapsulated2).ReceiveAsync());
        }

        [Fact]
        public async Task TestEncapsulate_CompleteAndFaultPassthrough()
        {
            var source = new BufferBlock<int>();

            var target = new ActionBlock<int>(i => { });
            var encapsulated = DataflowBlock.Encapsulate(target, source);
            encapsulated.Complete();
            await target.Completion;

            target = new ActionBlock<int>(i => { });
            encapsulated = DataflowBlock.Encapsulate(target, source);
            encapsulated.Fault(new FormatException());
            await Assert.ThrowsAnyAsync<FormatException>(() => target.Completion);
        }

        [Fact]
        public void TestOutputAvailableAsync_ArgumentValidation()
        {
            Assert.Throws<ArgumentNullException>(() => { DataflowBlock.OutputAvailableAsync<int>(null); });
            Assert.Throws<ArgumentNullException>(() => { DataflowBlock.OutputAvailableAsync<int>(null, CancellationToken.None); });
        }

        [Fact]
        public async Task TestOutputAvailableAsync_AvailabilityBeforeAfterData()
        {
            var buffer = new BufferBlock<int>();

            foreach (bool withUncanceledToken in DataflowTestHelpers.BooleanValues)
                foreach (bool beforeData in DataflowTestHelpers.BooleanValues)
                {
                    if (beforeData)
                    {
                        var t = withUncanceledToken ?
                            buffer.OutputAvailableAsync(new CancellationTokenSource().Token) :
                            buffer.OutputAvailableAsync();
                        Assert.False(t.IsCompleted);
                        buffer.Post(42);
                        Assert.True(await t);
                    }
                    else
                    {
                        buffer.Post(42);
                        var t = withUncanceledToken ?
                            buffer.OutputAvailableAsync(new CancellationTokenSource().Token) :
                            buffer.OutputAvailableAsync();
                        Assert.True(t.IsCompleted);
                        Assert.True(t.Result);
                    }
                    Assert.Equal(expected: 1, actual: buffer.Count);
                    Assert.Equal(expected: 42, actual: await buffer.ReceiveAsync());
                }
        }

        [Fact]
        public async Task TestOutputAvailableAsync_NoDataAfterCompletion()
        {
            foreach (bool withUncanceledToken in DataflowTestHelpers.BooleanValues)
                foreach (bool completeBefore in DataflowTestHelpers.BooleanValues)
                {
                    var buffer = new BufferBlock<int>();
                    Task<bool> t;

                    if (completeBefore) buffer.Complete();

                    t = withUncanceledToken ?
                        buffer.OutputAvailableAsync(new CancellationTokenSource().Token) :
                        buffer.OutputAvailableAsync();

                    if (!completeBefore) buffer.Complete();

                    Assert.False(await t);
                }
        }

        [Fact]
        public async Task TestOutputAvailableAsync_DataAfterCompletion()
        {
            foreach (bool withUncanceledToken in DataflowTestHelpers.BooleanValues)
                foreach (bool withData in DataflowTestHelpers.BooleanValues)
                {
                    var wob = new WriteOnceBlock<int>(_ => _);
                    if (withData)
                        wob.Post(42);
                    else
                        wob.Complete();
                    await wob.Completion;

                    Task<bool> t = withUncanceledToken ?
                        wob.OutputAvailableAsync(new CancellationTokenSource().Token) :
                        wob.OutputAvailableAsync();

                    Assert.Equal(expected: withData, actual: await t);
                }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/38817", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoInterpreter))]
        [SkipOnPlatform(TestPlatforms.Browser, "uses a lot of stack")]
        public async Task TestOutputAvailableAsync_LongSequence()
        {
            const int iterations = 10000; // enough to stack overflow if there's a problem

            var source = new BufferBlock<int>();
            Task t = source.OutputAvailableAsync();
            for (int i = 1; i < iterations; i++)
            {
                t = t.ContinueWith(_ => source.OutputAvailableAsync(),
                    CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
            }

            source.Post(42);
            await t;
        }

        [Fact]
        public async Task TestOutputAvailableAsync_Cancellation()
        {
            var buffer = new BufferBlock<int>();

            Assert.True(buffer.OutputAvailableAsync(new CancellationToken(true)).IsCanceled);

            var cts = new CancellationTokenSource();
            var t = buffer.OutputAvailableAsync(cts.Token);
            Assert.False(t.IsCompleted);
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);

            cts = new CancellationTokenSource();
            t = buffer.OutputAvailableAsync(cts.Token);
            buffer.Post(42);
            await t;
            cts.Cancel(); // make sure no exception happens
        }

        [Fact]
        public async Task TestOutputAvailableAsync_FaultySource()
        {
            var source = new DelegatePropagator<int, int>
            {
                LinkToDelegate = delegate { throw new InvalidCastException(); }
            };
            var t = source.OutputAvailableAsync();
            await Assert.ThrowsAsync<InvalidCastException>(() => t);
        }
    }
}
