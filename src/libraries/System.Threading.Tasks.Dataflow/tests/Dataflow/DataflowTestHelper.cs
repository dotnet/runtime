// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Threading.Tasks.Dataflow.Tests
{
    internal static class DataflowTestHelpers
    {
        internal static bool[] BooleanValues = { true, false };
        internal static Func<int, IEnumerable<int>> ToEnumerable = item => Enumerable.Repeat(item, 1);

        internal static ITargetBlock<int> PostRange(this ITargetBlock<int> target, int lowerBoundInclusive, int upperBoundExclusive)
        {
            return PostRange(target, lowerBoundInclusive, upperBoundExclusive, i => i);
        }

        internal static ITargetBlock<T> PostRange<T>(this ITargetBlock<T> target, int lowerBoundInclusive, int upperBoundExclusive, Func<int, T> selector)
        {
            Assert.NotNull(target);
            for (int i = lowerBoundInclusive; i < upperBoundExclusive; i++)
            {
                Assert.True(target.Post(selector(i)));
            }
            return target;
        }

        internal static ITargetBlock<T> PostItems<T>(this ITargetBlock<T> target, params T[] items)
        {
            return PostAll(target, items);
        }

        internal static ITargetBlock<T> PostAll<T>(this ITargetBlock<T> target, IEnumerable<T> items)
        {
            Assert.NotNull(target);
            Assert.NotNull(items);
            foreach (var item in items)
            {
                Assert.True(target.Post(item));
            }
            return target;
        }

        internal static void TestArgumentsExceptions<T>(ISourceBlock<T> source)
        {
            TestConsumeReserveReleaseArgumentsExceptions(source);

            ITargetBlock<T> validTarget = new BufferBlock<T>(), invalidTarget = null;
            DataflowLinkOptions validLinkOptions = new DataflowLinkOptions(), invalidLinkOptions = null;

            Assert.Throws<ArgumentNullException>(() => source.LinkTo(invalidTarget, validLinkOptions));
            Assert.Throws<ArgumentNullException>(() => source.LinkTo(validTarget, invalidLinkOptions));
            Assert.Throws<ArgumentNullException>(() => source.LinkTo(invalidTarget, invalidLinkOptions));
        }

        internal static void TestConsumeReserveReleaseArgumentsExceptions<T>(ISourceBlock<T> source)
        {
            DataflowMessageHeader validMessageHeader = new DataflowMessageHeader(1), invalidMessageHeader = default(DataflowMessageHeader);
            ITargetBlock<T> validTarget = new BufferBlock<T>(), invalidTarget = null;
            bool consumed;

            Assert.Throws<ArgumentNullException>(() => source.ConsumeMessage(validMessageHeader, invalidTarget, out consumed));
            AssertExtensions.Throws<ArgumentException>("messageHeader", () => source.ConsumeMessage(invalidMessageHeader, validTarget, out consumed));
            AssertExtensions.Throws<ArgumentException>("messageHeader", () => source.ConsumeMessage(invalidMessageHeader, invalidTarget, out consumed));
            Assert.Throws<ArgumentNullException>(() => source.ReserveMessage(validMessageHeader, invalidTarget));
            AssertExtensions.Throws<ArgumentException>("messageHeader", () => source.ReserveMessage(invalidMessageHeader, validTarget));
            AssertExtensions.Throws<ArgumentException>("messageHeader", () => source.ReserveMessage(invalidMessageHeader, invalidTarget));
            Assert.Throws<ArgumentNullException>(() => source.ReleaseReservation(validMessageHeader, invalidTarget));
            AssertExtensions.Throws<ArgumentException>("messageHeader", () => source.ReleaseReservation(invalidMessageHeader, validTarget));
            AssertExtensions.Throws<ArgumentException>("messageHeader", () => source.ReleaseReservation(invalidMessageHeader, invalidTarget));
        }


        internal static void TestOfferMessage_ArgumentValidation<T>(ITargetBlock<T> target)
        {
            AssertExtensions.Throws<ArgumentException>("messageHeader", () => target.OfferMessage(default(DataflowMessageHeader), default(T), new BufferBlock<T>(), false));
            AssertExtensions.Throws<ArgumentException>("messageHeader", () => target.OfferMessage(default(DataflowMessageHeader), default(T), null, false));
            AssertExtensions.Throws<ArgumentException>("consumeToAccept", () => target.OfferMessage(new DataflowMessageHeader(1), default(T), null, true));
        }

        internal static void TestOfferMessage_AcceptsDataDirectly<T>(ITargetBlock<T> target, int messages = 3)
        {
            for (int i = 1; i <= messages; i++)
            {
                Assert.Equal(
                    expected: DataflowMessageStatus.Accepted,
                    actual: target.OfferMessage(new DataflowMessageHeader(i), default(T), null, false));
            }
        }

        internal static void TestOfferMessage_CompleteAndOffer<T>(ITargetBlock<T> target, int messages = 3)
        {
            target.Complete();
            for (int i = 1; i <= messages; i++)
            {
                Assert.Equal(
                expected: DataflowMessageStatus.DecliningPermanently,
                actual: target.OfferMessage(new DataflowMessageHeader(4), default(T), null, false));
            }
        }

        internal static async Task TestOfferMessage_AcceptsViaLinking<T>(ITargetBlock<T> target, int messages = 3)
        {
            var src = new BufferBlock<T>();

            var stingySource = new DelegatePropagator<T, T>
            {
                ConsumeMessageDelegate = (DataflowMessageHeader _, ITargetBlock<T> __, out bool messageConsumed) => {
                    messageConsumed = false;
                    return default(T);
                }
            };
            Assert.Equal(
                expected: DataflowMessageStatus.NotAvailable,
                actual: target.OfferMessage(new DataflowMessageHeader(1), default(T), stingySource, consumeToAccept: true));

            src.PostRange(1, messages + 1, i => default(T));
            Assert.Equal(expected: messages, actual: src.Count);
            src.LinkTo(target);
            src.Complete();
            await src.Completion;
        }

        internal static async Task TestCompletionTask(Func<IDataflowBlock> generator)
        {
            foreach (bool before in DataflowTestHelpers.BooleanValues)
            {
                IDataflowBlock block = generator();
                if (before) // verify all is well regardless of whether the task is completed before or after we access Completion
                {
                    Assert.NotNull(block.Completion); // Completion should never be null
                    Assert.Equal(block.Completion, block.Completion); // Completion should be idempotent
                    Assert.False(block.Completion.IsCompleted);
                }
                block.Complete();
                Assert.NotNull(block.Completion); // ditto
                Assert.Equal(block.Completion, block.Completion); // ditto
                await block.Completion;
            }
        }

        internal static void TestToString(Func<string, IDataflowBlock> blockFactory)
        {
            IDataflowBlock block;

            block = blockFactory(null); // default
            Assert.Equal(
                expected: string.Format("{0} Id={1}", block.GetType().Name, block.Completion.Id),
                actual: block.ToString());

            block = blockFactory("none"); // no args
            Assert.Equal(
                expected: "none",
                actual: block.ToString());

            block = blockFactory("foo {0}"); // one arg
            Assert.Equal(
                expected: string.Format("foo {0}", block.GetType().Name),
                actual: block.ToString());

            block = blockFactory("foo {0} bar {1}"); // two args
            Assert.Equal(
                expected: string.Format("foo {0} bar {1}", block.GetType().Name, block.Completion.Id),
                actual: block.ToString());
        }

        internal static async Task TestReserveAndRelease<T>(
            IReceivableSourceBlock<T> block, bool reservationIsTargetSpecific = true)
        {
            var tcs = new TaskCompletionSource<bool>();

            // Offer the message to a target and wait until it's postponed
            var offeredMessage = default(DataflowMessageHeader);
            var target = new DelegatePropagator<T, T>
            {
                OfferMessageDelegate = (messageHeader, value, source, consumeToAccept) => {
                    offeredMessage = messageHeader;
                    tcs.TrySetResult(true);
                    return DataflowMessageStatus.Postponed;
                }
            };
            block.LinkTo(target);
            await tcs.Task;

            Assert.False(block.ReserveMessage(new DataflowMessageHeader(-99), target)); // reserving a different message should fail
            Assert.True(block.ReserveMessage(offeredMessage, target)); // reserve the message
            Assert.False(block.ReserveMessage(new DataflowMessageHeader(-99), target)); // reserving a different message should still fail

            if (reservationIsTargetSpecific)
            {
                Assert.False(block.ReserveMessage(offeredMessage, DataflowBlock.NullTarget<T>())); // another block tries to reserve the message
                Assert.Throws<InvalidOperationException>(() => block.ReleaseReservation(offeredMessage, DataflowBlock.NullTarget<T>())); // another block tries to release the message
            }

            T item;
            Assert.Equal(expected: !reservationIsTargetSpecific, actual: block.TryReceive(out item)); // anyone tries to receive

            Assert.Throws<InvalidOperationException>(() => block.ReleaseReservation(new DataflowMessageHeader(-42), target)); // anyone tries to release a reservation on a different message

            block.ReleaseReservation(offeredMessage, target); // release the reservation

            Assert.True(block.TryReceive(out item)); // now receiving should work
        }

        internal static async Task TestReserveAndConsume<T>(
            ISourceBlock<T> block, bool reservationIsTargetSpecific = true)
        {
            bool consumed;
            block.ConsumeMessage(new DataflowMessageHeader(-99), new ActionBlock<T>(i => { }), out consumed);
            Assert.False(consumed);

            var tcs = new TaskCompletionSource<bool>();
            var offeredMessage = default(DataflowMessageHeader);
            var target = new DelegatePropagator<T, T>
            {
                OfferMessageDelegate = (messageHeader, value, source, consumeToAccept) => {
                    offeredMessage = messageHeader;
                    tcs.TrySetResult(true);
                    return DataflowMessageStatus.Postponed;
                }
            };
            block.LinkTo(target);
            await tcs.Task;

            Assert.True(block.ReserveMessage(offeredMessage, target)); // reserve the message

            if (reservationIsTargetSpecific)
            {
                block.ConsumeMessage(offeredMessage, new ActionBlock<T>(delegate { }), out consumed); // different target tries to consume
                Assert.False(consumed);
            }

            block.ConsumeMessage(new DataflowMessageHeader(-99), target, out consumed); // right target, wrong message
            Assert.False(consumed);

            block.ConsumeMessage(offeredMessage, target, out consumed); // right target, right message
            Assert.True(consumed);
        }

        internal static IPropagatorBlock<U, U> Chain<T, U>(int numBlocks, Func<T> generate) where T : IPropagatorBlock<U, U>
        {
            var transforms = Enumerable.Range(0, numBlocks).Select(_ => generate()).ToArray();
            for (int i = 0; i < transforms.Length - 1; i++)
            {
                transforms[i].LinkTo(transforms[i + 1]);
                transforms[i].Completion.ContinueWith(delegate { transforms[i].Complete(); }, TaskScheduler.Default);
            }
            return DataflowBlock.Encapsulate(transforms[0], transforms[transforms.Length - 1]);
        }

    }
}
