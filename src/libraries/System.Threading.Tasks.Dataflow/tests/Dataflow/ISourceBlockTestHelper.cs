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
    internal class ISourceBlockTestHelper
    {
        internal static bool TestLinkTo<T>(ISourceBlock<T> source, int msgsCount)
        {
            // test LinkTo with unlink after once = true;
            var mres = new ManualResetEventSlim(false);
            int setCount = 1;
            int currentCount = 0;
            var target = new TransparentBlock<T>((msg) =>
                {
                    if (Interlocked.Increment(ref currentCount) == setCount)
                    {
                        mres.Set();
                    }
                });
            var disposable = source.LinkTo(target, new DataflowLinkOptions() { MaxMessages = 1 });
            if (disposable == null)
            {
                Console.WriteLine("LinkTo failed! returned null");
                return false;
            }
            if (target.Count != 1)
            {
                Console.WriteLine("LinkTo failed, The target didn't receive one msg, received {0}", target.Count);
                return false;
            }

            // test LinkTo with unlink after once = false;
            mres.Reset();
            // If the source is WriteOnceBlock or BroadcastBlock, only one msg will be propagated
            if (source is WriteOnceBlock<T> || source is BroadcastBlock<T>)
            {
                setCount = 1;
                msgsCount = 2;
            }
            else
                setCount = msgsCount - 1;
            currentCount = 0;
            disposable = source.LinkTo(target);

            if (disposable == null)
            {
                Console.WriteLine("LinkTo failed! returned null");
                return false;
            }
            if (target.Count != msgsCount)
            {
                Console.WriteLine("LinkTo failed, The target didn't receive all messages, it received {0}", target.Count);
            }

            return true;
        }


        internal static bool TestConsumeMessage<T>(ISourceBlock<T> source)
        {
            bool consumed;
            source.ConsumeMessage(new DataflowMessageHeader(-99), new ActionBlock<T>(i => { }), out consumed);
            if (consumed)
            {
                Console.WriteLine("ConsumeMessage failed, didn't return messageConsumed==false for a unresrved msg");
                return false;
            }

            // test consume the correct message with different target
            var mres = new ManualResetEventSlim(false);
            var offeredMessage = default(DataflowMessageHeader);
            var target1 = new TransparentBlock<T>((msg) =>
                {
                    offeredMessage = msg;
                    mres.Set();
                }, false);
            source.LinkTo(target1);
            mres.Wait();

            if (!source.ReserveMessage(offeredMessage, target1))
            {
                Console.WriteLine("ReserveMessage failed, returned false");
                return false;
            }

            //exclude writeOnce and broadCast because they don't respect reservation to the target
            if (!(source is WriteOnceBlock<T> || source is BroadcastBlock<T>))
            {
                //define another target
                var target2 = new TransparentBlock<T>(null);
                source.ConsumeMessage(offeredMessage, target2, out consumed);
                if (consumed)
                {
                    Console.WriteLine("ConsumeMessage failed, a different target succeeded to consume the message that it doesn't own");
                    return false;
                }
            }

            //same target different msg
            source.ConsumeMessage(new DataflowMessageHeader(-99), target1, out consumed);
            if (consumed)
            {
                Console.WriteLine("ConsumeMessage failed, the target succeeded to consume a differnt msg");
                return false;
            }

            //same target, same message
            source.ConsumeMessage(offeredMessage, target1, out consumed);
            if (!consumed)
            {
                Console.WriteLine("ConsumeMessage failed, the target failed to consume the reserved msg");
                return false;
            }

            return true;
        }


        internal static bool TestReserveMessageAndReleaseReservation<T>(IReceivableSourceBlock<T> source)
        {
            // test consume the correct message with different target
            var mres = new ManualResetEventSlim(false);
            var offeredMessage = default(DataflowMessageHeader);
            var target1 = new TransparentBlock<T>((msg) =>
            {
                offeredMessage = msg;
                mres.Set();
            }, false);
            source.LinkTo(target1);
            mres.Wait();

            if (!source.ReserveMessage(offeredMessage, target1))
            {
                Console.WriteLine("ReserveMessage failed, returned false");
                return false;
            }

            // try to reserve another msg
            if (source.ReserveMessage(new DataflowMessageHeader(-99), target1))
            {
                Console.WriteLine("ReserveMessage failed, succeeded to reserve a message while there is another reservation");
                return false;
            }

            // try TryReceive while reservation
            //exclude writeOnce and broadCast because they don't respect reservation to the target
            if (!(source is WriteOnceBlock<T> || source is BroadcastBlock<T>))
            {
                T result;
                if (source.TryReceive(out result))
                {
                    Console.WriteLine("ReserveMessage failed, TryReceive succeeded while there is another reservation");
                    return false;
                }
            }

            //try different target
            if (!(source is WriteOnceBlock<T>)) // exclude WriteOnce because RelaseReservation doesn't respect the target and will succeed 
            {
                var target2 = new TransparentBlock<T>(null);
                bool passed = true;
                Assert.Throws<InvalidOperationException>(() => source.ReleaseReservation(offeredMessage, target2));
                if (!passed)
                {
                    Console.WriteLine("ReleaseMessage FAILED, another target succeeded to release the reservation");
                    return false;
                }
            }

            // Now try releasing a different message. Even WriteOnce should throw.
            {
                bool passed = true;
                Assert.Throws<InvalidOperationException>(() => source.ReleaseReservation(new DataflowMessageHeader(-42), target1));
                if (!passed)
                {
                    Console.WriteLine("ReleaseMessage FAILED, the target succeeded to release the reservation of a different message");
                    return false;
                }
            }

            // try TryReceive after release
            source.ReleaseReservation(offeredMessage, target1);

            T result1;
            if (!source.TryReceive(out result1))
            {
                Console.WriteLine("ReleaseMessage FAILED, TryReceive failed after the release");
                return false;
            }

            return true;
        }

        internal static bool TestTryReceive<T>(IReceivableSourceBlock<T> source, int msgsCount)
        {
            T item;
            for (int i = 0; i < msgsCount; i++)
            {
                if (!source.TryReceive(out item))
                {
                    Console.WriteLine(" TryReceive failed! Returned false and expected to return true (i={0})", i);
                    return false;
                }
            }

            bool expectedResult = false;
            if (source is WriteOnceBlock<T> || source is BroadcastBlock<T>) // because WriteOnce and Broadcast always pass a copy and keep the msg, so TryReceive should succeed
                expectedResult = true;

            if (source.TryReceive(out item) != expectedResult)
            {
                Console.WriteLine(" TryReceive failed! Returned {0} and expected to return {1}", expectedResult, !expectedResult);
                return false;
            }

            return true;
        }

        internal static bool TestTryReceiveWithFilter<T>(IReceivableSourceBlock<T> source, int msgsCount)
        {
            T item;

            for (int i = 0; i < msgsCount; i++)
            {
                if (source.TryReceive(input => false, out item))
                {
                    Console.WriteLine(" TryReceive failed! Returned true and expected to return false (i={0})", i);
                    return false;
                }
            }

            for (int i = 0; i < msgsCount; i++)
            {
                if (!source.TryReceive(input => true, out item))
                {
                    Console.WriteLine(" TryReceive failed! Returned false and expected to return true (i={0})", i);
                    return false;
                }
            }

            bool expectedResult = false;
            if (source is WriteOnceBlock<T> || source is BroadcastBlock<T>) // because WriteOnce and Broadcast always pass a copy and keep the msg, so TryReceive should succeed
                expectedResult = true;

            if (source.TryReceive(out item) != expectedResult)
            {
                Console.WriteLine(" TryReceive failed! Returned {0} and expected to return {1}", expectedResult, !expectedResult);
                return false;
            }

            return true;
        }


        internal static bool TestTryReceiveAll<T>(IReceivableSourceBlock<T> source, int msgsCount)
        {
            IList<T> items;

            if (!source.TryReceiveAll(out items))
            {
                return false;
            }

            if (source is WriteOnceBlock<T> || source is BroadcastBlock<T>) // They only propagate one msg at a time
                msgsCount = 1;

            if (items.Count != msgsCount)
            {
                Console.WriteLine("* TryReceiveAll failed! Returned items count {0} and expected {1}", items.Count, msgsCount);
                return false;
            }

            bool expectedResult = false;
            if (source is WriteOnceBlock<T> || source is BroadcastBlock<T>) // because WriteOnce and Broadcast always pass a copy and keep the msg, so TryReceive should succeed
                expectedResult = true;

            if (source.TryReceiveAll(out items) != expectedResult)
            {
                Console.WriteLine(" TryReceiveAll failed! Returned {0} and expected to return {1}", expectedResult, !expectedResult);
                return false;
            }

            return true;
        }

        internal static bool TestCompletionTask<T>(ISourceBlock<T> source)
        {
            Console.WriteLine(" TestCompletionTask");

            if (source.Completion == null) // should never be null
            {
                Console.WriteLine("Completion failed! returned null.");
                return false;
            }
            return true;
        }

        internal static bool TestArgumentsExceptions<T>(ISourceBlock<T> source)
        {
            bool passed = true;

            var validMessageHeader = new DataflowMessageHeader(1);
            var invalidMessageHeader = default(DataflowMessageHeader);
            ITargetBlock<T> validTarget = new BufferBlock<T>();
            ITargetBlock<T> invalidTarget = null;
            DataflowLinkOptions invalidLinkOptions = null;
            DataflowLinkOptions validLinkOptions = new DataflowLinkOptions();
            bool consumed;

            Assert.Throws<ArgumentNullException>(() => source.ConsumeMessage(validMessageHeader, invalidTarget, out consumed));
            Assert.Throws<ArgumentException>(() => source.ConsumeMessage(invalidMessageHeader, validTarget, out consumed));
            Assert.Throws<ArgumentException>(() => source.ConsumeMessage(invalidMessageHeader, invalidTarget, out consumed));
            Assert.Throws<ArgumentNullException>(() => source.ReserveMessage(validMessageHeader, invalidTarget));
            Assert.Throws<ArgumentException>(() => source.ReserveMessage(invalidMessageHeader, validTarget));
            Assert.Throws<ArgumentException>(() => source.ReserveMessage(invalidMessageHeader, invalidTarget));
            Assert.Throws<ArgumentNullException>(() => source.ReleaseReservation(validMessageHeader, invalidTarget));
            Assert.Throws<ArgumentException>(() => source.ReleaseReservation(invalidMessageHeader, validTarget));
            Assert.Throws<ArgumentException>(() => source.ReleaseReservation(invalidMessageHeader, invalidTarget));
            Assert.Throws<ArgumentNullException>(() => source.LinkTo(invalidTarget));
            Assert.Throws<ArgumentNullException>(() => source.LinkTo(invalidTarget, validLinkOptions));
            Assert.Throws<ArgumentNullException>(() => source.LinkTo(validTarget, invalidLinkOptions));
            Assert.Throws<ArgumentNullException>(() => source.LinkTo(invalidTarget, invalidLinkOptions));

            return passed;
        }

        internal static bool TestCancelWhileReserve<TOutput>(Func<CancellationToken, ISourceBlock<TOutput>> sourceFactory,
                                                            Action<ISourceBlock<TOutput>> postMethod,
                                                            Func<ISourceBlock<TOutput>, int> countMethod)
        {
            Console.WriteLine(" TestCancelWhileReserve");

            CancellationTokenSource cts = new CancellationTokenSource();
            ISourceBlock<TOutput> source = sourceFactory(cts.Token);

            bool mayCompleteNow = false;
            ReserveTarget<TOutput> target = new ReserveTarget<TOutput>();
            Task<bool> completionVerificationTask = source.Completion.ContinueWith<bool>(_ =>
                {
                    if (mayCompleteNow)
                    {
                        // Wait for target's releasing task to finish before we examine the state of the source
                        target.ReleasingTask.Wait();

                        int messageCount0 = -1;

                        // We must wait for the block to finish processing the messages,
                        // but we don't want to sleep for a long period of time unnecessarily.
                        // So wake up every 200 ms for a period of 10 sec to check.
                        for (int i = 0; i < 2 && messageCount0 != 0; i++)
                        {
                            messageCount0 = countMethod(source);
                            Task.Delay(1).Wait();
                        }

                        if (messageCount0 != 0)
                        {
                            Console.WriteLine("Count failed! returned {0} and expected 0.", messageCount0);
                            return false;
                        }
                        Console.WriteLine("Passed");
                        return true;
                    }

                    Console.WriteLine("Completion failed! May not complete now.");
                    return false;
                });

            postMethod(source);
            source.LinkTo(target);

            // Target's tasks are creating at the first OfferMessage.
            // Therefore spin until the tasks are created before we can them them. 
            SpinWait.SpinUntil(() => target.ReservingTask != null);
            target.ReservingTask.Wait();
            if (!target.ReservingTask.Result)
            {
                Console.WriteLine("Reservation failed! returned false.");
                return false;
            }

            postMethod(source);
            Task.Delay(5).Wait();
            int messageCount2 = countMethod(source);
            if (messageCount2 != 2)
            {
                Console.WriteLine("Count failed! returned {0} and expected 2.", messageCount2);
                return false;
            }

            mayCompleteNow = true;
            cts.Cancel();
            Task.Delay(1).Wait();
            target.ReleasingTask.Start();

            completionVerificationTask.Wait();
            return completionVerificationTask.Result;
        }

        internal static bool TestTargetOrder<T>(IPropagatorBlock<T, T> source, int iterations)
        {
            Console.WriteLine(" TestTargetOrder");
            bool passed = true;
            int value = 0;

            // The targets will be linked in this order:
            // 1. target1
            // 2. target2
            // 3. target1 (again)
            // 4. terminator
            // Locking around value access is not necessary, because messages will be posted one at a time
            // and each offerring loop is sequential with default options.
            var target1 = new CustomizableTarget<T>((msgHeader, msgValue, src, c2a) =>
                                                    {
                                                        if (value == 0 || value == 2) value++;
                                                        else passed = false;
                                                        return DataflowMessageStatus.Declined;
                                                    });
            var target2 = new CustomizableTarget<T>((msgHeader, msgValue, src, c2a) =>
                                                    {
                                                        if (value == 1) value++;
                                                        else passed = false;
                                                        return DataflowMessageStatus.Declined;
                                                    });
            var terminator = new CustomizableTarget<T>((msgHeader, msgValue, src, c2a) =>
                                                    {
                                                        if (value == 3) value = 0;
                                                        else passed = false;
                                                        return DataflowMessageStatus.Accepted;
                                                    });

            source.LinkTo(target1);
            source.LinkTo(target2);
            source.LinkTo(target1);
            source.LinkTo(terminator);

            for (int i = 0; passed && i < iterations; i++)
            {
                source.Post(default(T));
                SpinWait.SpinUntil(() => value == 0);
            }

            Console.WriteLine("> {0}", passed ? "Passed" : "FAILED");
            return passed;
        }
    }

    /// <summary>
    /// A target block whose behavior on interface methods could be customized.
    /// At this moment only OfferMessage customizations are necessary.
    /// </summary>
    internal class CustomizableTarget<T> : ITargetBlock<T>
    {
        private readonly Func<DataflowMessageHeader, T, ISourceBlock<T>, bool, DataflowMessageStatus> _m_offerMessageAction;

        internal CustomizableTarget(Func<DataflowMessageHeader, T, ISourceBlock<T>, bool, DataflowMessageStatus> offerMessageAction)
        {
            _m_offerMessageAction = offerMessageAction;
        }

        Task IDataflowBlock.Completion
        {
            get { throw new NotSupportedException(); }
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
            return _m_offerMessageAction(messageHeader, messageValue, source, consumeToAccept);
        }

        public bool Post(T item)
        {
            throw new NotSupportedException();
        }
    }
}
