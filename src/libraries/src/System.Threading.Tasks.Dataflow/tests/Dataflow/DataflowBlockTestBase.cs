// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using Xunit;

namespace System.Threading.Tasks.Dataflow.Tests
{
    public class DataflowBlockTestBase
    {
        protected enum Intervention
        {
            None,
            Complete,
            Cancel,
        }

        protected enum FeedMethod
        {
            _First = 0,
            Post = _First,
            OfferMessage,
            _Count
        }

        protected struct TargetProperties<T>
        {
            internal ITargetBlock<T> Target;
            internal ITargetBlock<T> Capturer;
            internal bool ErrorVerifyable;
        }

        protected static IPropagatorBlock<U, U> Chain<T, U>(int numBlocks, Func<T> generate) where T : IPropagatorBlock<U, U>
        {
            var transforms = Enumerable.Range(0, numBlocks).Select(_ => generate()).ToArray();
            for (int i = 0; i < transforms.Length - 1; i++)
            {
                transforms[i].LinkTo(transforms[i + 1]);
                transforms[i].Completion.ContinueWith(delegate { transforms[i].Complete(); });
            }
            return DataflowBlock.Encapsulate(transforms[0], transforms[transforms.Length - 1]);
        }

        protected static bool FeedTarget(Func<DataflowBlockOptions, TargetProperties<int>> targetFactory, DataflowBlockOptions options,
                                        int messageCount, Intervention intervention, CancellationTokenSource cancellationSource,
                                        FeedMethod feedMethod, bool isVerifiable)
        {
            bool passed = true;
            int expectedCaptureCount = messageCount;
            DataflowMessageStatus expectedStatus = DataflowMessageStatus.Accepted;
            bool expectedBoolStatus = true;

            // Aestetics
            var maxDop = options is ExecutionDataflowBlockOptions ? ((ExecutionDataflowBlockOptions)options).MaxDegreeOfParallelism : 1;
            string testHeader = string.Format("    + MessageCount={0}, MaxDOP={1}, MaxMPT={2}, Intervention={3}, FeedMethod={4}, Verifyable={5}",
                messageCount,
                maxDop,
                options.MaxMessagesPerTask, intervention, feedMethod, isVerifiable);

            // Initialize capturing structs
            s_captures = new int[messageCount];
            for (int j = 0; j < messageCount; j++)
            {
                s_captures[j] = -1;
            }
            s_captureCount = 0;
            s_currentDOP = 0;
            s_maxDOP = maxDop;


            // Construct target
            TargetProperties<int> targetProperties = targetFactory(options);
            ITargetBlock<int> target = targetProperties.Target;


            // Measure the message processing time
            DateTime startTime = DateTime.Now;

            // Feeding loop
            for (int i = 0; i < messageCount; i++)
            {
                if (feedMethod == FeedMethod.OfferMessage)
                {
                    DataflowMessageStatus status = target.OfferMessage(new DataflowMessageHeader(1 + i), i, null, false); // Message ID doesn't metter because consumeToAccept:false
                    if (status != expectedStatus &&
                        (cancellationSource == null || !cancellationSource.IsCancellationRequested)) // Ignore cancellation - it makes the expected result unpredictable
                    {
                        passed = false;
                        break;
                    }
                }
                else
                {
                    bool boolStatus = target.Post(i);
                    if (boolStatus != expectedBoolStatus &&
                        (cancellationSource == null || !cancellationSource.IsCancellationRequested)) // Ignore cancellation - it makes the expected result unpredictable
                    {
                        passed = false;
                        break;
                    }
                }

                // Intervene half way through.
                // While the intervention code is similar, the product behavior is different -
                // when DeclinePermanenetly is triggered, pending messages must still be processed, i.e.
                // the expectedCaptureCount is exactly i + 1;
                // when Cancel is triggered, pending messages may be discarded, i.e.
                // the expectedCaptureCount may be any number up to i + 1.
                if (i == messageCount / 2)
                {
                    switch (intervention)
                    {
                        case Intervention.Complete:
                            target.Complete();
                            expectedCaptureCount = i + 1;
                            expectedStatus = DataflowMessageStatus.DecliningPermanently;
                            expectedBoolStatus = false;
                            break;
                        case Intervention.Cancel:
                            cancellationSource.Cancel();
                            expectedCaptureCount = i + 1;
                            expectedStatus = DataflowMessageStatus.DecliningPermanently;
                            expectedBoolStatus = false;
                            break;
                    }
                }
            }


            // Tell the target we are done
            target.Complete();

            // Wait for the block to declare it is complete before starting verification
            try
            {
                target.Completion.Wait();
            }
            catch (AggregateException ae)
            {
                // Swallow OperationCancelledException.
                ae.Handle(e => e is OperationCanceledException);
            }
            DateTime finishTime = DateTime.Now;


            // Tell the capturer we are done
            Task.Delay(1000).Wait();
            targetProperties.Capturer.Complete();

            // Wait for the block to declare it is complete before starting verification
            try
            {
                targetProperties.Capturer.Completion.Wait();
            }
            catch (AggregateException ae)
            {
                // Swallow OperationCancelledException.
                ae.Handle(e => e is OperationCanceledException);
            }


            // Verify total capture count.
            // s_captureCount is the actual number of captures. 
            // expectedCaptureCount is the number of messages fed into the block before any intervention.
            // If there hasn't been intervention, that is the total number of messages.
            // When the intervention has been Complete, the actual capture count must match the input message count. 
            // When the intervention has been Cancel, the actual capture count can be any number up to the input message count.
            if (isVerifiable && passed &&
                ((expectedCaptureCount != s_captureCount && intervention != Intervention.Cancel) ||
                    (expectedCaptureCount < s_captureCount && intervention == Intervention.Cancel)))
            {
                passed = false;
            }

            // When there's been cancellation, there might be gaps in the last maxDop captures.
            // So eliminate them from verification.
            s_captureCount -= maxDop;

            // Verify gaps in captures.
            // Each element of the capture array contains the ID of the task that reported the message with that payload.
            // Each element has been initialized to -1. If an element is still -1 after the run, then a message has not
            // been captured.
            if (isVerifiable && passed)
            {
                for (int i = 0; i < s_captureCount; i++)
                {
                    if (s_captures[i] < 0)
                    {
                        passed = false;
                        break;
                    }
                }
            }

            // Verify no task has exceeded MaxMessagesPerTask
            // Aggregate the elements of the capture array and count the occurences.
            // No count may exceed MMPT.
            if (isVerifiable && passed && options.MaxMessagesPerTask != -1)
            {
                var exceededMaxMPT = s_captures
                                    .Where(x => x >= 0)
                                    .GroupBy(x => x)
                                    .Where(g => g.Count() > options.MaxMessagesPerTask);

                foreach (var exceed in exceededMaxMPT)
                {
                    passed = false;
                }
            }

            return passed;
        }


        // Stores the Task ID on which value i was passed to the side effect callback 
        protected static int[] s_captures;
        protected static int s_captureCount;
        protected static int s_currentDOP;
        protected static int s_maxDOP;


        protected static void NoOp(int i)
        {
        }


        protected static void TrackCaptures(int i)
        {
            int currentDOP = Interlocked.Increment(ref s_currentDOP);

            s_captures[i] = Task.CurrentId != null ? (int)Task.CurrentId : -1;
            Interlocked.Increment(ref s_captureCount);


            Interlocked.Decrement(ref s_currentDOP);
            // Put the throw here so we can correlate the number of errors to the number of captures
            //throw new InvalidProgramException(i.ToString());
        }

        protected static Task TrackCapturesAsync(int i)
        {
            return Task.Factory.StartNew(() => TrackCaptures(i));
        }

        /// <summary>
        /// Validate the IsourceBlock ConsumeMessage,Reserve and ReleaseReservation methods parameters
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        private static bool TestArgumentsExceptions<T>(ISourceBlock<T> source)
        {
            bool passed = true;

            var validMessageHeader = new DataflowMessageHeader(1);
            var invalidMessageHeader = default(DataflowMessageHeader);
            ITargetBlock<T> validTarget = new BufferBlock<T>();
            ITargetBlock<T> invalidTarget = null;
            bool consumed;

            Assert.Throws<ArgumentNullException>(() => source.ConsumeMessage(validMessageHeader, invalidTarget, out consumed));
            Assert.Throws<ArgumentNullException>(() => source.ConsumeMessage(invalidMessageHeader, validTarget, out consumed));
            Assert.Throws<ArgumentNullException>(() => source.ReserveMessage(validMessageHeader, invalidTarget));
            Assert.Throws<ArgumentNullException>(() => source.ReserveMessage(invalidMessageHeader, validTarget));
            Assert.Throws<ArgumentNullException>(() => source.ReleaseReservation(validMessageHeader, invalidTarget));
            Assert.Throws<ArgumentNullException>(() => source.ReleaseReservation(invalidMessageHeader, validTarget));

            return passed;
        }

        // Since all Dataflow block tests are methods of this class, they can all see this member
        protected static readonly int s_iterationCount = 2;

        // Creates dop tasks that invoke method(arg) concurrently.
        protected static bool TestConcurrently<T1, TR>(string blockName, string methodName, Func<T1, TR> method, T1 arg, TR expected)
        {
            bool passed = true;
            Task<bool>[] tasks = new Task<bool>[Parallelism.ActualDegreeOfParallelism];

            // Use this event to block tasks until all are ready for invocation 
            CountdownEvent ce = new CountdownEvent(Parallelism.ActualDegreeOfParallelism);

            // Launch tasks
            for (int i = 0; i < Parallelism.ActualDegreeOfParallelism; i++)
            {
                tasks[i] = Task<bool>.Factory.StartNew(() =>
                                            {
                                                ce.Signal();
                                                ce.Wait();

                                                for (int iteration = 0; iteration < s_iterationCount; iteration++)
                                                {
                                                    TR r = method(arg);
                                                    if (!r.Equals(expected))
                                                    {
                                                        return false;
                                                    }
                                                }

                                                return true;
                                            });
            }

            // Wait for the tasks to finish and examine results
            Task.WaitAll(tasks);
            for (int i = 0; passed && i < Parallelism.ActualDegreeOfParallelism; i++)
            {
                passed &= tasks[i].Result;
            }

            return passed;
        }
    }

    internal static class Extensions
    {
        internal static void LinkWithCompletion<T>(this ISourceBlock<T> source, ITargetBlock<T> target)
        {
            source.LinkTo(target);
            source.Completion.ContinueWith(t => target.Complete());
        }
    }

    /// <summary>
    /// Helper target block that gives access to the offered msg objet via user provided action
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    internal class TransparentBlock<TInput> : ITargetBlock<TInput>
    {
        private ConcurrentQueue<TInput> _m_messages;
        private Action<DataflowMessageHeader> _m_action;
        private volatile bool _m_decliningPermanently;
        private bool _m_alwaysAccept;
        private object _m_incomingLock;

        public TransparentBlock(Action<DataflowMessageHeader> notificationAction, bool alwaysAccept = true)
        {
            _m_action = notificationAction;
            _m_alwaysAccept = alwaysAccept;
            _m_messages = new ConcurrentQueue<TInput>();
            _m_incomingLock = new object();
        }

        public Boolean Post(TInput item)
        {
            lock (_m_incomingLock)
            {
                if (_m_decliningPermanently) return false;

                _m_messages.Enqueue(item);
                return true;
            }
        }


        public void Complete()
        {
            lock (_m_incomingLock)
            {
                _m_decliningPermanently = true;
            }
        }


        public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, TInput messageValue, ISourceBlock<TInput> source, Boolean consumeToAccept)
        {
            DataflowMessageStatus returnValue;
            lock (_m_incomingLock)
            {
                if (_m_alwaysAccept)
                {
                    bool consumed = !consumeToAccept;
                    var acceptedMessage = (source != null && consumeToAccept) ? source.ConsumeMessage(messageHeader, this, out consumed) : messageValue;
                    _m_messages.Enqueue(messageValue);
                    returnValue = DataflowMessageStatus.Accepted;
                }
                else
                    returnValue = DataflowMessageStatus.Declined;
            }
            if (_m_action != null)
                _m_action(messageHeader);

            return returnValue;
        }

        public Task Completion { get { return null; } }

        void IDataflowBlock.Fault(Exception exception)
        {
            throw new NotImplementedException();
        }

        public TInput[] Messages
        {
            get { return _m_messages.ToArray(); }
        }

        public int Count
        {
            get { return _m_messages.Count; }
        }
    }


    /// <summary>
    /// Helper target block that reserves the offered message unless it is already holding a reservation
    /// </summary>
    internal class ReserveTarget<T> : ITargetBlock<T>
    {
        public Task<bool> ReservingTask;
        public Task ReleasingTask;

        public void Complete()
        {
            throw new NotImplementedException();
        }

        public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T> source, bool consumeToAccept)
        {
            // Either both tasks are set or both tasks are unset
            Contract.Assert(!(ReservingTask != null ^ ReleasingTask != null));

            // Reserve the message only if there is no other reservation in place
            if (ReservingTask == null)
            {
                ReservingTask = Task<bool>.Factory.StartNew(() => { return source.ReserveMessage(messageHeader, this); });
                ReleasingTask = new Task(() => { source.ReleaseReservation(messageHeader, this); });
            }

            return DataflowMessageStatus.Postponed;
        }

        public bool Post(T item)
        {
            throw new NotImplementedException();
        }

        public Task Completion
        {
            get { throw new NotImplementedException(); }
        }

        void IDataflowBlock.Fault(Exception exception)
        {
            throw new NotImplementedException();
        }
    }
}
