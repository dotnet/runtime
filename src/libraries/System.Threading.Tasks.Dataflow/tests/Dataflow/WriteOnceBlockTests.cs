// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Threading.Tasks.Dataflow.Tests
{
    public class WriteOnceBlockTests
    {
        [Fact]
        public void TestCtor()
        {
            var blocks = new[] {
                new WriteOnceBlock<int>(null), // valid
                new WriteOnceBlock<int>(i => i),
                new WriteOnceBlock<int>(null, new DataflowBlockOptions()),
                new WriteOnceBlock<int>(i => i, new DataflowBlockOptions { BoundedCapacity = 2 }),
                new WriteOnceBlock<int>(null, new DataflowBlockOptions { CancellationToken = new CancellationTokenSource().Token  }),
                new WriteOnceBlock<int>(null, new DataflowBlockOptions { MaxMessagesPerTask = 1  }),
                new WriteOnceBlock<int>(null, new DataflowBlockOptions { NameFormat = ""  }),
                new WriteOnceBlock<int>(null, new DataflowBlockOptions { TaskScheduler = new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler  }),
            };
            foreach (var block in blocks)
            {
                Assert.NotNull(block.Completion);
                Assert.False(block.Completion.IsCompleted);
                int item;
                Assert.False(block.TryReceive(out item));
            }
        }

        [Fact]
        public void TestArgumentExceptions()
        {
            Assert.Throws<ArgumentNullException>(() => new WriteOnceBlock<int>(i => i, null));
            DataflowTestHelpers.TestArgumentsExceptions(new WriteOnceBlock<int>(i => i));
            Assert.Throws<ArgumentNullException>(() => ((ITargetBlock<int>)new WriteOnceBlock<int>(null)).Fault(null));
        }

        [Fact]
        public void TestToString()
        {
            // Test ToString() with the only custom configuration being NameFormat
            DataflowTestHelpers.TestToString(
                nameFormat => nameFormat != null ?
                    new WriteOnceBlock<int>(i => i, new DataflowBlockOptions() { NameFormat = nameFormat }) :
                    new WriteOnceBlock<int>(i => i));
        }

        [Fact]
        public async Task TestLinkToOptions()
        {
            foreach (bool consumeToAccept in DataflowTestHelpers.BooleanValues)
            foreach (bool propagateCompletion in DataflowTestHelpers.BooleanValues)
            foreach (bool append in DataflowTestHelpers.BooleanValues)
            foreach (int maxMessages in new[] { DataflowBlockOptions.Unbounded, 1, 2 })
            {
                var wob = consumeToAccept ?
                    new WriteOnceBlock<int>(i => i) :
                    new WriteOnceBlock<int>(null);

                int result = 0;
                const int Count = 10;
                Assert.True(Count % 2 == 0);
                var targets = Enumerable.Range(0, Count).Select(i => new ActionBlock<int>(_ => Interlocked.Increment(ref result))).ToArray();
                var options = new DataflowLinkOptions
                {
                    MaxMessages = maxMessages,
                    Append = append,
                    PropagateCompletion = propagateCompletion
                };

                for (int i = 0; i < Count / 2; i++)
                {
                    wob.LinkTo(targets[i], options, f => false);
                    wob.LinkTo(targets[i], options);
                }
                wob.Post(1);
                for (int i = Count / 2; i < Count; i++)
                {
                    wob.LinkTo(targets[i], options);
                }

                await wob.Completion;
                if (propagateCompletion)
                {
                    await Task.WhenAll(from target in targets select target.Completion);
                    Assert.Equal(expected: Count, actual: result);
                }
                else
                {
                    // This should never fail, but there is a race such that it won't always
                    // be testing what we want it to test.  Doing so would mean waiting
                    // for an arbitrary period of time to ensure something hasn't happened.
                    Assert.All(targets, t => Assert.False(t.Completion.IsCompleted));
                }
            }
        }

        [Fact]
        public async Task TestOfferMessage()
        {
            var generators = new Func<WriteOnceBlock<int>>[]
            {
                () => new WriteOnceBlock<int>(i => i),
                () => new WriteOnceBlock<int>(i => i, new DataflowBlockOptions { BoundedCapacity = 10 }),
                () => new WriteOnceBlock<int>(i => i, new DataflowBlockOptions { BoundedCapacity = 10, MaxMessagesPerTask = 1 })
            };
            foreach (var generator in generators)
            {
                DataflowTestHelpers.TestOfferMessage_ArgumentValidation(generator());

                var target = generator();
                DataflowTestHelpers.TestOfferMessage_AcceptsDataDirectly(target, messages: 1);
                DataflowTestHelpers.TestOfferMessage_CompleteAndOffer(target);
                await target.Completion;

                target = generator();
                await DataflowTestHelpers.TestOfferMessage_AcceptsViaLinking(target, messages: 1);
                DataflowTestHelpers.TestOfferMessage_CompleteAndOffer(target);
                await target.Completion;

                target = generator();
                Assert.Equal(
                    expected: DataflowMessageStatus.Accepted,
                    actual: ((ITargetBlock<int>)target).OfferMessage(new DataflowMessageHeader(1), 1, null, false));
                Assert.Equal(
                    expected: DataflowMessageStatus.DecliningPermanently,
                    actual: ((ITargetBlock<int>)target).OfferMessage(new DataflowMessageHeader(1), 1, null, false));
                await target.Completion;
            }
        }

        [Fact]
        public async Task TestCompletionTask()
        {
            await DataflowTestHelpers.TestCompletionTask(() => new WriteOnceBlock<int>(i => i));
        }

        [Fact]
        public async Task TestPost()
        {
            foreach (int boundedCapacity in new[] { DataflowBlockOptions.Unbounded, 1, 2 })
            {
                var wob = new WriteOnceBlock<int>(i => i, new DataflowBlockOptions { BoundedCapacity = boundedCapacity }); // options shouldn't affect anything
                Assert.True(wob.Post(1));
                Assert.False(wob.Post(2));
                await wob.Completion;
            }
        }

        [Fact]
        public async Task TestPostThenReceive()
        {
            var wob = new WriteOnceBlock<int>(i => i);
            for (int i = 10; i < 15; i++)
            {
                bool posted = wob.Post(i);
                Assert.Equal(expected: i == 10, actual: posted);
            }
            int item;
            Assert.True(wob.TryReceive(out item));
            Assert.Equal(expected: 10, actual: item);
            await wob.Completion;

            wob = new WriteOnceBlock<int>(null);
            wob.Post(42);
            Task<int> t = wob.ReceiveAsync();
            Assert.True(t.IsCompleted);
            Assert.Equal(expected: 42, actual: t.Result);
            await wob.Completion;
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedAndBlockingWait))]
        public async Task TestReceiveThenPost()
        {
            var wob = new WriteOnceBlock<int>(null);
            var ignored = Task.Run(() => wob.Post(42));
            Assert.Equal(expected: 42, actual: wob.Receive()); // this should always pass, but due to race we may not test what we're hoping to
            await wob.Completion;

            wob = new WriteOnceBlock<int>(null);
            Task<int> t = wob.ReceiveAsync();
            Assert.False(t.IsCompleted);
            wob.Post(16);
            Assert.Equal(expected: 16, actual: await t);
        }

        [Fact]
        public async Task TestTryReceiveWithFilter()
        {
            var wob = new WriteOnceBlock<int>(null);
            wob.Post(1);

            int item;
            Assert.True(wob.TryReceive(out item));
            Assert.Equal(expected: 1, actual: item);

            Assert.True(wob.TryReceive(i => i == 1, out item));
            Assert.Equal(expected: 1, actual: item);

            Assert.False(wob.TryReceive(i => i == 0, out item));

            await wob.Completion;
        }

        [Fact]
        public async Task TestBroadcasting()
        {
            var wob = new WriteOnceBlock<int>(i => i + 1);
            var targets = Enumerable.Range(0, 3).Select(_ => new TransformBlock<int, int>(i => i)).ToArray();
            foreach (var target in targets)
            {
                wob.LinkTo(target);
            }
            wob.Post(42);
            foreach (var target in targets)
            {
                Assert.Equal(expected: 43, actual: await target.ReceiveAsync());
            }
        }

        [Fact]
        public async Task TestCancellationBeforeAndAfterCtor()
        {
            foreach (bool before in DataflowTestHelpers.BooleanValues)
            {
                var cts = new CancellationTokenSource();
                if (before)
                {
                    cts.Cancel();
                }
                var wob = new WriteOnceBlock<int>(null, new DataflowBlockOptions { CancellationToken = cts.Token });
                if (!before)
                {
                    cts.Cancel();
                }

                int ignoredValue;
                IList<int> ignoredValues;

                Assert.NotNull(wob.LinkTo(DataflowBlock.NullTarget<int>()));
                Assert.False(wob.Post(42));
                Task<bool> sendTask = wob.SendAsync(43);
                Assert.True(sendTask.IsCompleted);
                Assert.False(sendTask.Result);
                Assert.False(wob.TryReceive(out ignoredValue));
                Assert.False(((IReceivableSourceBlock<int>)wob).TryReceiveAll(out ignoredValues));
                Assert.NotNull(wob.Completion);

                await AssertExtensions.CanceledAsync(cts.Token, wob.Completion);
            }
        }

        [Fact]
        public async Task TestCloning()
        {
            // Test cloning when a clone function is provided
            {
                int data = 42;
                var wob = new WriteOnceBlock<int>(x => -x);
                Assert.True(wob.Post(data));

                Assert.False(wob.Post(data + 1));

                for (int i = 0; i < 3; i++)
                {
                    int item;
                    Assert.True(wob.TryReceive(out item));
                    Assert.Equal(expected: -data, actual: item);

                    Assert.Equal(expected: -data, actual: wob.Receive());

                    Assert.Equal(expected: -data, actual: await wob.ReceiveAsync());

                    IList<int> items;
                    Assert.True(((IReceivableSourceBlock<int>)wob).TryReceiveAll(out items));
                    Assert.Equal(expected: 1, actual: items.Count);
                    Assert.Equal(expected: -data, actual: items[0]);
                }

                int result = 0;
                var target = new ActionBlock<int>(i => {
                    Assert.Equal(expected: 0, actual: result);
                    result = i;
                    Assert.Equal(expected: -data, actual: i);
                });
                wob.LinkTo(target, new DataflowLinkOptions { PropagateCompletion = true });
                await target.Completion;
            }

            // Test successful processing when no clone function exists
            {
                var data = new object();
                var wob = new WriteOnceBlock<object>(null);
                Assert.True(wob.Post(data));

                Assert.False(wob.Post(new object()));

                object result;
                for (int i = 0; i < 3; i++)
                {
                    Assert.True(wob.TryReceive(out result));
                    Assert.Equal(expected: data, actual: result);

                    Assert.Equal(expected: data, actual: wob.Receive());
                    Assert.Equal(expected: data, actual: await wob.ReceiveAsync());

                    IList<object> items;
                    Assert.True(((IReceivableSourceBlock<object>)wob).TryReceiveAll(out items));
                    Assert.Equal(expected: 1, actual: items.Count);
                    Assert.Equal(expected: data, actual: items[0]);
                }

                result = null;
                var target = new ActionBlock<object>(o => {
                    Assert.Null(result);
                    result = o;
                    Assert.Equal(expected: data, actual: o);
                });
                wob.LinkTo(target, new DataflowLinkOptions { PropagateCompletion = true });
                await target.Completion;
            }
        }

        [Fact]
        public async Task TestReserveReleaseConsume()
        {
            var wb = new WriteOnceBlock<int>(i => i * 2);
            wb.Post(1);
            await DataflowTestHelpers.TestReserveAndRelease(wb, reservationIsTargetSpecific: false);

            wb = new WriteOnceBlock<int>(i => i * 2);
            wb.Post(2);
            await DataflowTestHelpers.TestReserveAndConsume(wb, reservationIsTargetSpecific: false);
        }

        [Fact]
        public async Task TestFaultyTarget()
        {
            var wob = new WriteOnceBlock<int>(null);
            wob.LinkTo(new DelegatePropagator<int, int> {
                OfferMessageDelegate = delegate {
                    throw new FormatException();
                }
            });
            wob.Post(42);
            await Assert.ThrowsAsync<FormatException>(() => wob.Completion);
        }

        [Fact]
        public async Task TestFaultyScheduler()
        {
            var wob = new WriteOnceBlock<int>(null, new DataflowBlockOptions {
                TaskScheduler = new DelegateTaskScheduler {
                    QueueTaskDelegate = delegate {
                        throw new InvalidCastException();
                    }
                }
            });
            wob.LinkTo(DataflowBlock.NullTarget<int>());
            wob.Post(42);
            await Assert.ThrowsAsync<TaskSchedulerException>(() => wob.Completion);
        }

    }
}
