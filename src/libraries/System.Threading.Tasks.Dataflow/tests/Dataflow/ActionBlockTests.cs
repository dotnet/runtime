// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.Threading.Tasks.Dataflow.Tests
{
    public class ActionBlockTests
    {
        [Fact]
        public void TestToString()
        {
            // Test ToString() with the only custom configuration being NameFormat
            DataflowTestHelpers.TestToString(
                nameFormat => nameFormat != null ?
                    new ActionBlock<int>(i => { }, new ExecutionDataflowBlockOptions() { NameFormat = nameFormat }) :
                    new ActionBlock<int>(i => { }));

            // Test ToString() with other configuration
            DataflowTestHelpers.TestToString(
                nameFormat => nameFormat != null ?
                    new ActionBlock<int>(i => { }, new ExecutionDataflowBlockOptions() { NameFormat = nameFormat, SingleProducerConstrained = true }) :
                    new ActionBlock<int>(i => { }, new ExecutionDataflowBlockOptions() { SingleProducerConstrained = true }));
        }

        [Fact]
        public async Task TestOfferMessage()
        {
            var generators = new Func<ActionBlock<int>>[]
            {
                () => new ActionBlock<int>(i => { }),
                () => new ActionBlock<int>(i => { }, new ExecutionDataflowBlockOptions { BoundedCapacity = 10 }),
                () => new ActionBlock<int>(i => { }, new ExecutionDataflowBlockOptions { BoundedCapacity = 10, MaxMessagesPerTask = 1, MaxDegreeOfParallelism = 4 })
            };
            foreach (var generator in generators)
            {
                DataflowTestHelpers.TestOfferMessage_ArgumentValidation(generator());

                var target = generator();
                DataflowTestHelpers.TestOfferMessage_AcceptsDataDirectly(target);
                DataflowTestHelpers.TestOfferMessage_CompleteAndOffer(target);
                await target.Completion;

                target = generator();
                await DataflowTestHelpers.TestOfferMessage_AcceptsViaLinking(target);
                DataflowTestHelpers.TestOfferMessage_CompleteAndOffer(target);
                await target.Completion;
            }
        }

        [Fact]
        public void TestPost()
        {
            foreach (bool bounded in DataflowTestHelpers.BooleanValues)
            {
                ActionBlock<int> ab = new ActionBlock<int>(i => { },
                    new ExecutionDataflowBlockOptions { BoundedCapacity = bounded ? 1 : -1 }); // test greedy and then non-greedy
                Assert.True(ab.Post(0), "Expected non-completed ActionBlock to accept Post'd message");
                ab.Complete();
                Assert.False(ab.Post(0), "Expected Complete'd ActionBlock to decline messages");
            }
        }

        [Fact]
        public async Task TestCompletionTask()
        {
            await DataflowTestHelpers.TestCompletionTask(() => new ActionBlock<int>(i => { }));
        }

        [Fact]
        public void TestCtor()
        {
            // Invalid arguments
            Assert.Throws<ArgumentNullException>(() => new ActionBlock<int>((Func<int, Task>)null));
            Assert.Throws<ArgumentNullException>(() => new ActionBlock<int>((Func<int, Task>)null));
            Assert.Throws<ArgumentNullException>(() => new ActionBlock<int>(i => { }, null));
            Assert.Throws<ArgumentNullException>(() => new ActionBlock<int>(i => default(Task), null));

            // Valid arguments; make sure they don't throw, and validate some properties afterwards
            var blocks = new[]
            {
                new ActionBlock<int>(i => { }),

                new ActionBlock<int>(i => { }, new ExecutionDataflowBlockOptions { MaxMessagesPerTask = 1 }),
                new ActionBlock<int>(i => { }, new ExecutionDataflowBlockOptions { MaxMessagesPerTask = 1, CancellationToken = new CancellationToken(true) }),

                new ActionBlock<int>(i => default(Task)),
                new ActionBlock<int>(i => default(Task), new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 2 }),

                new ActionBlock<int>(i => default(Task), new ExecutionDataflowBlockOptions { MaxMessagesPerTask = 1 }),
                new ActionBlock<int>(i => default(Task), new ExecutionDataflowBlockOptions { MaxMessagesPerTask = 1, CancellationToken = new CancellationToken(true) })
            };
            foreach (var block in blocks)
            {
                Assert.Equal(0, block.InputCount);
                Assert.NotNull(block.Completion);
            }
        }

        [Fact]
        public async Task TestBasicMessageProcessing()
        {
            var options = new[]
            {
                // Actual values used here aren't important; just want to make sure the block works
                // with these properties set to non-default values
                new ExecutionDataflowBlockOptions { },
                new ExecutionDataflowBlockOptions { BoundedCapacity = 1 },
                new ExecutionDataflowBlockOptions { MaxMessagesPerTask = 2 },
                new ExecutionDataflowBlockOptions { SingleProducerConstrained = true },
                new ExecutionDataflowBlockOptions { TaskScheduler = new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler },
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = 2,
                    MaxMessagesPerTask = 4,
                    SingleProducerConstrained = true,
                    TaskScheduler = new ConcurrentExclusiveSchedulerPair().ConcurrentScheduler,
                    CancellationToken = new CancellationTokenSource().Token,
                    NameFormat = ""
                }
            };

            // Make sure all data makes it through the block
            for (int propMechanism = 0; propMechanism < 2; propMechanism++)
            {
                foreach (var option in options)
                {
                    string result = null;
                    foreach (var target in new[]
                        {
                            new ActionBlock<char>(c => result += c, option), // sync
                            new ActionBlock<char>(c => Task.Run(() => result += c), option) // async
                        })
                    {
                        result = "";

                        switch (propMechanism)
                        {
                            case 0:
                                for (int i = 0; i < 26; i++)
                                {
                                    char c = (char)('a' + i);
                                    if (option.BoundedCapacity == DataflowBlockOptions.Unbounded)
                                        target.Post(c);
                                    else
                                        await target.SendAsync(c); // will work even if Unbounded, but we can test Post if it's Unbounded
                                }
                                target.Complete();
                                break;

                            case 1:
                                var source = new BufferBlock<char>();
                                source.PostAll(Enumerable.Range(0, 26).Select(i => (char)('a' + i)));
                                source.Complete();
                                source.LinkTo(target, new DataflowLinkOptions { PropagateCompletion = true });
                                break;
                        }

                        await target.Completion;
                        Assert.Equal(expected: "abcdefghijklmnopqrstuvwxyz", actual: result);
                    }
                }
            }

        }

        [Fact]
        public async Task TestSchedulerUsage()
        {
            foreach (bool singleProducerConstrained in DataflowTestHelpers.BooleanValues)
            {
                var scheduler = new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler;

                var actionBlockSync = new ActionBlock<int>(_ => Assert.Equal(scheduler.Id, TaskScheduler.Current.Id),
                    new ExecutionDataflowBlockOptions
                    {
                        TaskScheduler = scheduler,
                        SingleProducerConstrained = singleProducerConstrained
                    });
                actionBlockSync.PostRange(0, 10);
                actionBlockSync.Complete();
                await actionBlockSync.Completion;

                var actionBlockAsync = new ActionBlock<int>(_ => {
                    Assert.Equal(scheduler.Id, TaskScheduler.Current.Id);
                    return Task.FromResult(0);
                }, new ExecutionDataflowBlockOptions
                    {
                        TaskScheduler = scheduler,
                        SingleProducerConstrained = singleProducerConstrained
                    });
                actionBlockAsync.PostRange(0, 10);
                actionBlockAsync.Complete();
                await actionBlockAsync.Completion;
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedAndBlockingWait))]
        public async Task TestInputCount()
        {
            foreach (bool sync in DataflowTestHelpers.BooleanValues)
            foreach (bool singleProducerConstrained in DataflowTestHelpers.BooleanValues)
            {
                Barrier barrier1 = new Barrier(2), barrier2 = new Barrier(2);
                var options = new ExecutionDataflowBlockOptions { SingleProducerConstrained = singleProducerConstrained };
                Action<int> body = _ => {
                    barrier1.SignalAndWait();
                    // will test InputCount here
                    barrier2.SignalAndWait();
                };

                ActionBlock<int> ab = sync ?
                    new ActionBlock<int>(body, options) :
                    new ActionBlock<int>(i => Task.Run(() => body(i)), options);

                for (int iter = 0; iter < 2; iter++)
                {
                    ab.PostItems(1, 2);
                    for (int i = 1; i >= 0; i--)
                    {
                        barrier1.SignalAndWait();
                        Assert.Equal(expected: i, actual: ab.InputCount);
                        barrier2.SignalAndWait();
                    }
                }

                ab.Complete();
                await ab.Completion;
            }
        }

        [Fact]
        public async Task TestOrderMaintained()
        {
            foreach (bool sync in DataflowTestHelpers.BooleanValues)
            foreach (bool singleProducerConstrained in DataflowTestHelpers.BooleanValues)
            {
                var options = new ExecutionDataflowBlockOptions { SingleProducerConstrained = singleProducerConstrained };
                int prev = -1;
                Action<int> body = i =>
                {
                    Assert.Equal(expected: prev + 1, actual: i);
                    prev = i;
                };

                ActionBlock<int> ab = sync ?
                    new ActionBlock<int>(body, options) :
                    new ActionBlock<int>(i => Task.Run(() => body(i)), options);
                ab.PostRange(0, 100);
                ab.Complete();
                await ab.Completion;
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedAndBlockingWait))]
        public async Task TestNonGreedy()
        {
            foreach (bool sync in DataflowTestHelpers.BooleanValues)
            {
                var barrier1 = new Barrier(2);
                Action<int> body = _ => barrier1.SignalAndWait();
                var options = new ExecutionDataflowBlockOptions { BoundedCapacity = 1 };

                ActionBlock<int> ab = sync ?
                    new ActionBlock<int>(body, options) :
                    new ActionBlock<int>(i => Task.Run(() => body(i)), options);

                Task<bool>[] sends = Enumerable.Range(0, 10).Select(i => ab.SendAsync(i)).ToArray();
                for (int i = 0; i < sends.Length; i++)
                {
                    Assert.True(sends[i].Result); // Next send should have completed and with the value successfully accepted
                    for (int j = i + 1; j < sends.Length; j++) // No further sends should have completed yet
                    {
                        Assert.False(sends[j].IsCompleted);
                    }
                    barrier1.SignalAndWait();
                }

                ab.Complete();
                await ab.Completion;
            }
        }

        [Fact]
        public async Task TestConsumeToAccept()
        {
            foreach (int maxMessagesPerTask in new[] { DataflowBlockOptions.Unbounded, 1 })
            foreach (bool singleProducer in DataflowTestHelpers.BooleanValues)
            {
                int sum = 0;
                var bb = new BroadcastBlock<int>(i => i * 2, new DataflowBlockOptions { MaxMessagesPerTask = maxMessagesPerTask });
                var ab = new ActionBlock<int>(i => sum += i, new ExecutionDataflowBlockOptions { SingleProducerConstrained = singleProducer });
                bb.LinkTo(ab, new DataflowLinkOptions { PropagateCompletion = true });

                const int Messages = 100;
                bb.PostRange(1, Messages + 1);
                bb.Complete();

                await ab.Completion;
                Assert.Equal(expected: 100 * 101, actual: sum);
            }
        }

        [Fact]
        public async Task TestOperationCanceledExceptionsIgnored()
        {
            foreach (bool sync in DataflowTestHelpers.BooleanValues)
            foreach (bool singleProducerConstrained in DataflowTestHelpers.BooleanValues)
            {
                var options = new ExecutionDataflowBlockOptions { SingleProducerConstrained = singleProducerConstrained };
                int sumOfOdds = 0;
                Action<int> body = i => {
                    if ((i % 2) == 0) throw new OperationCanceledException();
                    sumOfOdds += i;
                };

                ActionBlock<int> ab = sync ?
                    new ActionBlock<int>(body, options) :
                    new ActionBlock<int>(async i => { await Task.Yield(); body(i); }, options);

                const int MaxValue = 10;
                ab.PostRange(0, MaxValue);
                ab.Complete();
                await ab.Completion;
                Assert.Equal(
                    expected: Enumerable.Range(0, MaxValue).Where(i => i % 2 != 0).Sum(),
                    actual: sumOfOdds);
            }
        }

        [Fact]
        public async Task TestPrecanceledToken()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var options = new ExecutionDataflowBlockOptions { CancellationToken = cts.Token };
            var blocks = new []
            {
                new ActionBlock<int>(i => { }, options),
                new ActionBlock<int>(i => Task.FromResult(0), options)
            };

            foreach (ActionBlock<int> ab in blocks)
            {
                Assert.False(ab.Post(42));
                Assert.Equal(expected: 0, actual: ab.InputCount);
                Assert.NotNull(ab.Completion);

                ab.Complete();
                ((IDataflowBlock)ab).Fault(new Exception());

                await AssertExtensions.CanceledAsync(cts.Token, ab.Completion);
            }
        }

        [Fact]
        public async Task TestFault()
        {
            foreach (bool singleProducerConstrained in DataflowTestHelpers.BooleanValues)
            {
                var ab = new ActionBlock<int>(i => { },
                    new ExecutionDataflowBlockOptions { SingleProducerConstrained = true });
                Assert.Throws<ArgumentNullException>(() => ((IDataflowBlock)ab).Fault(null));
                ((IDataflowBlock)ab).Fault(new InvalidCastException());
                await Assert.ThrowsAsync<InvalidCastException>(() => ab.Completion);
            }
        }

        [Fact]
        public async Task TestFaulting()
        {
            for (int trial = 0; trial < 3; trial++)
            foreach (bool singleProducerConstrained in DataflowTestHelpers.BooleanValues)
            {
                var options = new ExecutionDataflowBlockOptions { SingleProducerConstrained = singleProducerConstrained };
                Action thrower = () => { throw new InvalidOperationException(); };

                ActionBlock<int> ab = null;
                switch (trial)
                {
                    case 0: ab = new ActionBlock<int>(i => thrower(), options); break;
                    case 1: ab = new ActionBlock<int>(i => { thrower(); return Task.FromResult(0); }, options); break;
                    case 2: ab = new ActionBlock<int>(i => Task.Run(thrower), options); break;
                }
                for (int i = 0; i < 4; i++)
                {
                    ab.Post(i); // Post may return false, depending on race with ActionBlock faulting
                }

                await Assert.ThrowsAsync<InvalidOperationException>(async () => await ab.Completion);

                if (!singleProducerConstrained)
                {
                    Assert.Equal(expected: 0, actual: ab.InputCount); // not 100% guaranteed in the SPSC case
                }
                Assert.False(ab.Post(5));
            }
        }

        [Fact]
        public async Task TestNullReturnedTasks()
        {
            int sumOfOdds = 0;

            var ab = new ActionBlock<int>(i => {
                if ((i % 2) == 0) return null;
                return Task.Run(() => { sumOfOdds += i; });
            });

            const int MaxValue = 10;
            ab.PostRange(0, MaxValue);
            ab.Complete();
            await ab.Completion;

            Assert.Equal(
                expected: Enumerable.Range(0, MaxValue).Where(i => i % 2 != 0).Sum(),
                actual: sumOfOdds);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedAndBlockingWait))]
        public async Task TestParallelExecution()
        {
            int dop = 2;
            foreach (bool sync in DataflowTestHelpers.BooleanValues)
            foreach (bool singleProducerConstrained in DataflowTestHelpers.BooleanValues)
            {
                Barrier barrier = new Barrier(dop);
                var options = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = dop, SingleProducerConstrained = singleProducerConstrained };
                ActionBlock<int> ab = sync ?
                    new ActionBlock<int>(_ => barrier.SignalAndWait(), options) :
                    new ActionBlock<int>(_ => Task.Run(() => barrier.SignalAndWait()), options);

                int iters = dop * 4;
                ab.PostRange(0, iters);
                ab.Complete();
                await ab.Completion;
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedAndBlockingWait))]
        public async Task TestReleasingOfPostponedMessages()
        {
            foreach (bool sync in DataflowTestHelpers.BooleanValues)
            {
                Barrier barrier1 = new Barrier(2), barrier2 = new Barrier(2);
                Action<int> body = i => { barrier1.SignalAndWait(); barrier2.SignalAndWait(); };
                var options = new ExecutionDataflowBlockOptions { BoundedCapacity = 1 };
                ActionBlock<int> ab = sync ?
                    new ActionBlock<int>(body, options) :
                    new ActionBlock<int>(i => Task.Run(() => body(i)), options);

                ab.Post(0);
                barrier1.SignalAndWait();

                Task<bool>[] sends = Enumerable.Range(0, 10).Select(i => ab.SendAsync(i)).ToArray();
                Assert.All(sends, s => Assert.False(s.IsCompleted));

                ab.Complete();
                barrier2.SignalAndWait();

                await ab.Completion;

                Assert.All(sends, s => Assert.False(s.Result));
            }
        }

        [Fact]
        public async Task TestExceptionDataStorage()
        {
            const string DataKey = "DataflowMessageValue"; // must match key used in dataflow source

            // Validate that a message which causes the ActionBlock to fault
            // ends up being stored (ToString) in the resulting exception's Data
            var ab1 = new ActionBlock<int>((Action<int>)(i => { throw new FormatException(); }));
            ab1.Post(42);
            await Assert.ThrowsAsync<FormatException>(() => ab1.Completion);
            AggregateException e = ab1.Completion.Exception;
            Assert.Equal(expected: 1, actual: e.InnerExceptions.Count);
            Assert.Equal(expected: "42", actual: (string)e.InnerException.Data[DataKey]);

            // Test case where message's ToString throws
            var ab2 = new ActionBlock<ObjectWithFaultyToString>((Action<ObjectWithFaultyToString>)(i => { throw new FormatException(); }));
            ab2.Post(new ObjectWithFaultyToString());
            Exception ex = await Assert.ThrowsAsync<FormatException>(() => ab2.Completion);
            Assert.False(ex.Data.Contains(DataKey));
        }

        private class ObjectWithFaultyToString
        {
            public override string ToString() { throw new InvalidTimeZoneException(); }
        }

        [Fact]
        public async Task TestFaultyScheduler()
        {
            var ab = new ActionBlock<int>(i => { },
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = new DelegateTaskScheduler
                    {
                        QueueTaskDelegate = delegate { throw new FormatException(); }
                    }
                });
            ab.Post(42);
            await Assert.ThrowsAsync<TaskSchedulerException>(() => ab.Completion);
        }

    }
}
