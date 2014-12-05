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
    internal class ITargetBlockTestHelper
    {
        internal const int BOUNDED_CAPACITY = 2;
        internal const int COMPLETE_TIMEOUT = 100;
        internal const int NOT_COMPLETED_TIMEOUT = 10;
        private static ManualResetEventSlim _m_pause;
        internal static void BoundingAction<T>(T x) { _m_pause.Wait(); }

        internal static bool TestBoundingTarget<TInput, TOutput>(ITargetBlock<TInput> target, bool greedy)
        {
            Console.WriteLine("* TestBoundingTarget {0} ({1})", target.GetType().Name, greedy ? "greedy" : "non-greedy");

            bool passed = true;
            bool localPassed = true;
            int i;
            var send1 = new Task<bool>[BOUNDED_CAPACITY];
            var send2 = new Task<bool>[BOUNDED_CAPACITY];

            if (target is ActionBlock<TInput>) _m_pause = new ManualResetEventSlim();

            // Send messages up to the bounding capacity
            for (i = 0; localPassed && i < BOUNDED_CAPACITY; i++)
            {
                // All should be accepted immediately in greedy mode
                send1[i] = target.SendAsync(default(TInput));
                if (greedy) localPassed = send1[i].Wait(0) && send1[i].Result;
            }

            // In non-greedy mode, all should be accepted afterwards
            if (!greedy) localPassed = Task.WaitAll(send1, COMPLETE_TIMEOUT);
            passed &= localPassed;
            Assert.True(localPassed, string.Format("FAILED to send up to capacity ({0}) at iteration {1}", BOUNDED_CAPACITY, i));

            // Send one more message - it should be postponed (not consumed)
            send2[0] = target.SendAsync(default(TInput));
            localPassed = !send2[0].Wait(NOT_COMPLETED_TIMEOUT) || target is BroadcastBlock<TInput>; // TODO: How to make BroadcastBlock reliably postpone messages?
            passed &= localPassed;
            Assert.True(localPassed, "FAILED to postpone the excess message");

            // Unblock at least one message
            if (target is ActionBlock<TInput>) _m_pause.Set();
            else
            {
                TOutput item;
                ((IReceivableSourceBlock<TOutput>)target).TryReceive(out item);
            }

            // Now the excess item should be consumed in greedy mode and postponed in non-greedy mode
            localPassed = send2[0].Wait(greedy ? COMPLETE_TIMEOUT : NOT_COMPLETED_TIMEOUT);
            if (!greedy) localPassed = !localPassed;
            passed &= localPassed;
            Assert.True(localPassed, "FAILED to consume the excess message after unblocking");

            if (target is BatchBlock<TInput>)
            {
                // Let the task that consumed the excess message go away
                if (greedy) Task.Delay(COMPLETE_TIMEOUT).Wait();

                // Send messages up to the bounding capacity
                for (i = 1; localPassed && i < BOUNDED_CAPACITY; i++)
                {
                    // All should be accepted immediately in greedy mode
                    send2[i] = target.SendAsync(default(TInput));
                    if (greedy) localPassed = send2[i].Wait(0) && send2[i].Result;
                }

                // In non-greedy mode, all should be accepted afterwards
                if (!greedy) localPassed = Task.WaitAll(send2, COMPLETE_TIMEOUT);
                passed &= localPassed;
                Assert.True(localPassed, string.Format("FAILED to send up to capacity ({0}) at iteration {1} AFTER unblocking", BOUNDED_CAPACITY, i));
            }

            return passed;
        }

        internal static bool TestBoundingJoin2<TInput>(JoinBlock<TInput, TInput> join, bool greedy)
        {
            Console.WriteLine("* TestBoundingJoin2 {0} ({1})", join.GetType().Name, greedy ? "greedy" : "non-greedy");

            bool passed = true;
            bool localPassed = true;
            int i;
            var send1 = new Task<bool>[2 * BOUNDED_CAPACITY];
            var send2 = new Task<bool>[2];

            // Send messages up to the bounding capacity
            for (i = 0; localPassed && i < BOUNDED_CAPACITY; i++)
            {
                // All should be accepted immediately in greedy mode
                send1[i] = join.Target1.SendAsync(default(TInput));
                send1[BOUNDED_CAPACITY + i] = join.Target2.SendAsync(default(TInput));
                if (greedy)
                    localPassed = send1[i].Wait(0) && send1[i].Result &&
                                  send1[BOUNDED_CAPACITY + i].Wait(0) && send1[BOUNDED_CAPACITY + i].Result;
            }

            // In non-greedy mode, all should be accepted afterwards
            if (!greedy) localPassed = Task.WaitAll(send1, COMPLETE_TIMEOUT);
            passed &= localPassed;
            Assert.True(localPassed, string.Format("FAILED to send up to capacity ({0}) at iteration {1}", BOUNDED_CAPACITY, i));

            // Send one more message to each target - it should be postponed (not consumed)
            send2[0] = join.Target1.SendAsync(default(TInput));
            send2[1] = join.Target2.SendAsync(default(TInput));
            localPassed = !Task.WaitAll(send2, NOT_COMPLETED_TIMEOUT);
            passed &= localPassed;
            Assert.True(localPassed, "FAILED to postpone the excess message");

            // Unblock at least one message
            Tuple<TInput, TInput> item;
            join.TryReceive(out item);

            // Now the excess items should be consumed
            localPassed = send2[0].Wait(COMPLETE_TIMEOUT);
            passed &= localPassed;
            Assert.True(localPassed, "FAILED to consume the excess message after unblocking");

            return passed;
        }

        internal static bool TestBoundingJoin3<TInput>(JoinBlock<TInput, TInput, TInput> join, bool greedy)
        {
            Console.WriteLine("* TestBoundingJoin3 {0} ({1})", join.GetType().Name, greedy ? "greedy" : "non-greedy");

            bool passed = true;
            bool localPassed = true;
            int i;
            var send1 = new Task<bool>[3 * BOUNDED_CAPACITY];
            var send2 = new Task<bool>[3];

            // Send messages up to the bounding capacity
            for (i = 0; localPassed && i < BOUNDED_CAPACITY; i++)
            {
                // All should be accepted immediately in greedy mode
                send1[i] = join.Target1.SendAsync(default(TInput));
                send1[BOUNDED_CAPACITY + i] = join.Target2.SendAsync(default(TInput));
                send1[2 * BOUNDED_CAPACITY + i] = join.Target3.SendAsync(default(TInput));
                if (greedy)
                    localPassed = send1[i].Wait(0) && send1[i].Result &&
                                  send1[BOUNDED_CAPACITY + i].Wait(0) && send1[BOUNDED_CAPACITY + i].Result &&
                                  send1[2 * BOUNDED_CAPACITY + i].Wait(0) && send1[2 * BOUNDED_CAPACITY + i].Result;
            }

            // In non-greedy mode, all should be accepted afterwards
            if (!greedy) localPassed = Task.WaitAll(send1, COMPLETE_TIMEOUT);
            passed &= localPassed;
            Assert.True(localPassed, string.Format("FAILED to send up to capacity ({0}) at iteration {1}", BOUNDED_CAPACITY, i));

            // Send one more message to each target - it should be postponed (not consumed)
            send2[0] = join.Target1.SendAsync(default(TInput));
            send2[1] = join.Target2.SendAsync(default(TInput));
            send2[2] = join.Target3.SendAsync(default(TInput));
            localPassed = !Task.WaitAll(send2, NOT_COMPLETED_TIMEOUT);
            passed &= localPassed;
            Assert.True(localPassed, "FAILED to postpone the excess message");

            // Unblock at least one message
            Tuple<TInput, TInput, TInput> item;
            join.TryReceive(out item);

            // Now the excess items should be consumed
            localPassed = send2[0].Wait(COMPLETE_TIMEOUT);
            passed &= localPassed;
            Assert.True(localPassed, "FAILED to consume the excess message after unblocking");

            return passed;
        }

        internal static bool TestBoundingGreedyJoinTarget2<TInput>(JoinBlock<TInput, TInput> join, int testedTargetIndex)
        {
            Console.WriteLine("* TestBoundingGreedyJoinTarget2 (Target{0})", testedTargetIndex);

            bool passed = true;
            bool localPassed = true;
            int i;
            var send1 = new Task<bool>[BOUNDED_CAPACITY];
            var send2 = new Task<bool>[2]; //number of targets
            ITargetBlock<TInput> target1, target2;

            // Point the target1 and target2 refs
            switch (testedTargetIndex)
            {
                case 1:
                    target1 = join.Target1;
                    target2 = join.Target2;
                    break;

                case 2:
                    target1 = join.Target2;
                    target2 = join.Target1;
                    break;

                default:
                    throw new InvalidOperationException("Invalid testTargetIndex");
            }

            // Send messages up to the bounding capacity
            for (i = 0; localPassed && i < BOUNDED_CAPACITY; i++)
            {
                // All should be accepted immediately in greedy mode
                send1[i] = target1.SendAsync(default(TInput));
                localPassed = send1[i].Wait(0) && send1[i].Result;
            }
            passed &= localPassed;
            Assert.True(localPassed, string.Format("FAILED to send up to capacity ({0}) at iteration {1} (target1)", BOUNDED_CAPACITY, i));

            // Send one more message to target1 - it should be postponed (not consumed)
            send2[0] = target1.SendAsync(default(TInput));
            localPassed = !send2[0].Wait(NOT_COMPLETED_TIMEOUT);
            passed &= localPassed;
            Assert.True(localPassed, "FAILED to postpone the excess message (target1)");

            // Send one message to target2 - it should be accepted immediately
            send2[1] = target2.SendAsync(default(TInput));
            localPassed = send2[1].Wait(0) && send2[1].Result;
            passed &= localPassed;
            Assert.True(localPassed, "FAILED to accept the first message (target2)");

            // Unblock at least one message
            Tuple<TInput, TInput> item;
            join.TryReceive(out item);

            // Now the excess item on Target1 should be consumed
            localPassed = send2[0].Wait(COMPLETE_TIMEOUT);
            passed &= localPassed;
            Assert.True(localPassed, "FAILED to consume the excess message after unblocking");

            return passed;
        }

        internal static bool TestBoundingGreedyJoinTarget3<TInput>(JoinBlock<TInput, TInput, TInput> join, int testedTargetIndex)
        {
            Console.WriteLine("* TestBoundingGreedyJoinTarget3 (Target{0})", testedTargetIndex);

            bool passed = true;
            bool localPassed = true;
            int i;
            var send1 = new Task<bool>[BOUNDED_CAPACITY];
            var send2 = new Task<bool>[3]; // number of targets
            ITargetBlock<TInput> target1, target2, target3;

            // Point the target1 and target2 refs
            switch (testedTargetIndex)
            {
                case 1:
                    target1 = join.Target1;
                    target2 = join.Target2;
                    target3 = join.Target3;
                    break;

                case 2:
                    target1 = join.Target2;
                    target2 = join.Target3;
                    target3 = join.Target1;
                    break;

                case 3:
                    target1 = join.Target3;
                    target2 = join.Target1;
                    target3 = join.Target2;
                    break;

                default:
                    throw new InvalidOperationException("Invalid testTargetIndex");
            }

            // Send messages up to the bounding capacity
            for (i = 0; localPassed && i < BOUNDED_CAPACITY; i++)
            {
                // All should be accepted immediately in greedy mode
                send1[i] = target1.SendAsync(default(TInput));
                localPassed = send1[i].Wait(0) && send1[i].Result;
            }
            passed &= localPassed;
            Assert.True(localPassed, string.Format("FAILED to send up to capacity ({0}) at iteration {1} (target1)", BOUNDED_CAPACITY, i));

            // Send one more message to target1 - it should be postponed (not consumed)
            send2[0] = target1.SendAsync(default(TInput));
            localPassed = !send2[0].Wait(NOT_COMPLETED_TIMEOUT);
            passed &= localPassed;
            Assert.True(localPassed, "FAILED to postpone the excess message (target1)");

            // Send one message to target2 - it should be accepted immediately
            send2[1] = target2.SendAsync(default(TInput));
            localPassed = send2[1].Wait(0) && send2[1].Result;
            passed &= localPassed;
            Assert.True(localPassed, "FAILED to accept the first message (target2)");

            // Send one message to target3 - it should be accepted immediately
            send2[2] = target3.SendAsync(default(TInput));
            localPassed = send2[2].Wait(0) && send2[1].Result;
            passed &= localPassed;
            Assert.True(localPassed, "FAILED to accept the first message (target3)");

            // Unblock at least one message
            Tuple<TInput, TInput, TInput> item;
            join.TryReceive(out item);

            // Now the excess item on Target1 should be consumed
            localPassed = send2[0].Wait(COMPLETE_TIMEOUT);
            passed &= localPassed;
            Assert.True(localPassed, "FAILED to consume the excess message after unblocking");

            return passed;
        }

        internal static bool TestOfferMessage<T>(ITargetBlock<T> target)
        {
            Console.WriteLine("* TestOfferMessage");

            var status = target.OfferMessage(new DataflowMessageHeader(1), default(T), null, false);
            if (status != DataflowMessageStatus.Accepted)
            {
                Console.WriteLine("> OfferMessage failed! expected return value to be Accepted and actual {0}", status);
                return false;
            }

            var src = new BufferBlock<T>();
            src.Post(default(T));
            src.LinkTo(target);
            var expectedStatus = DataflowMessageStatus.NotAvailable;

            if (target is WriteOnceBlock<T>) // write once will always DecliningPermanently after the first msg
                expectedStatus = DataflowMessageStatus.DecliningPermanently;
            status = target.OfferMessage(new DataflowMessageHeader(2), default(T), src, true);
            if (status != expectedStatus)
            {
                Console.WriteLine("> OfferMessage failed! expected return value to be {0} and actual {1}", expectedStatus, status);
                return false;
            }

            target.Complete();
            status = target.OfferMessage(new DataflowMessageHeader(3), default(T), null, false);
            if (status != DataflowMessageStatus.DecliningPermanently)
            {
                Console.WriteLine("> OfferMessage failed! expected return value to be DecliningPermanently and actual {0}", status);
                return false;
            }

            return true;
        }


        internal static bool TestPost<T>(ITargetBlock<T> target)
        {
            Console.WriteLine("* TestPost");
            var result = target.Post(default(T));
            if (!result)
            {
                Console.WriteLine(">Post returned false and expected to return true");
                return false;
            }

            target.Complete();
            result = target.Post(default(T));
            if (result)
            {
                Console.WriteLine(">Post returned true and expected to return false");
                return false;
            }

            return true;
        }


        internal static bool TestNonGreedyPost<T>(ITargetBlock<T> target)
        {
            bool passed = true;

            // The result of this call is non-deterministic.
            // We are testing that no exception is thrown.
            target.Post(default(T));

            return passed;
        }


        internal static bool TestComplete<T>(ITargetBlock<T> target)
        {
            Console.WriteLine("* TestComplete");
            //test multiple call doesn't throw exceptions
            target.Complete();
            target.Complete();
            return true;
        }

        internal static bool TestCompletionTask<T>(ITargetBlock<T> target)
        {
            Console.WriteLine("* TestCompletionTask");

            if (target.Completion == null) // should never be null
            {
                Console.WriteLine("> Completion failed! returned null.");
                return false;
            }
            return true;
        }

        internal static bool TestArgumentsExceptions<T>(ITargetBlock<T> target)
        {
            bool passed = true;
            var validMessageHeader = new DataflowMessageHeader(1);
            var invalidMessageHeader = default(DataflowMessageHeader);
            var validSource = new BufferBlock<T>();
            ISourceBlock<T> invalidSource = null;
            Assert.Throws<ArgumentException>(() => target.OfferMessage(invalidMessageHeader, default(T), validSource, false));
            Assert.Throws<ArgumentException>(() => target.OfferMessage(invalidMessageHeader, default(T), invalidSource, false));
            Assert.Throws<ArgumentException>(() => target.OfferMessage(validMessageHeader, default(T), invalidSource, true));
            return passed;
        }
    }
}
