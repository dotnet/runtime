// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// OrderPreservingPipeliningMergeHelper.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq.Parallel
{
    /// <summary>
    /// A merge helper that yields results in a streaming fashion, while still ensuring correct output
    /// ordering. This merge only works if each producer task generates outputs in the correct order,
    /// i.e. with an Increasing (or Correct) order index.
    ///
    /// The merge creates DOP producer tasks, each of which will be  writing results into a separate
    /// buffer.
    ///
    /// The consumer always waits until each producer buffer contains at least one element. If we don't
    /// have one element from each producer, we cannot yield the next element. (If the order index is
    /// Correct, or in some special cases with the Increasing order, we could yield sooner. The
    /// current algorithm does not take advantage of this.)
    ///
    /// The consumer maintains a producer heap, and uses it to decide which producer should yield the next output
    /// result. After yielding an element from a particular producer, the consumer will take another element
    /// from the same producer. However, if the producer buffer exceeded a particular threshold, the consumer
    /// will take the entire buffer, and give the producer an empty buffer to fill.
    ///
    /// Finally, if the producer notices that its buffer has exceeded an even greater threshold, it will
    /// go to sleep and wait until the consumer takes the entire buffer.
    /// </summary>
#if !FEATURE_WASM_MANAGED_THREADS
    [System.Runtime.Versioning.UnsupportedOSPlatform("browser")]
#endif
    internal sealed class OrderPreservingPipeliningMergeHelper<TOutput, TKey> : IMergeHelper<TOutput>
    {
        private readonly QueryTaskGroupState _taskGroupState; // State shared among tasks.
        private readonly PartitionedStream<TOutput, TKey> _partitions; // Source partitions.
        private readonly TaskScheduler _taskScheduler; // The task manager to execute the query.

        /// <summary>
        /// Whether the producer is allowed to buffer up elements before handing a chunk to the consumer.
        /// If false, the producer will make each result available to the consumer immediately after it is
        /// produced.
        /// </summary>
        private readonly bool _autoBuffered;

        /// <summary>
        /// Buffers for the results. Each buffer has elements added by one producer, and removed
        /// by the consumer.
        /// </summary>
        private readonly Queue<Pair<TKey, TOutput>>[] _buffers;

        /// <summary>
        /// Whether each producer is done producing. Set to true by individual producers, read by consumer.
        /// </summary>
        private readonly bool[] _producerDone;

        /// <summary>
        /// Whether a particular producer is waiting on the consumer. Read by the consumer, set to true
        /// by producers, set to false by the consumer.
        /// </summary>
        private readonly bool[] _producerWaiting;

        /// <summary>
        ///  Whether the consumer is waiting on a particular producer. Read by producers, set to true
        ///  by consumer, set to false by producer.
        /// </summary>
        private readonly bool[] _consumerWaiting;

        /// <summary>
        /// Each object is a lock protecting the corresponding elements in _buffers, _producerDone,
        /// _producerWaiting and _consumerWaiting.
        /// </summary>
        private readonly object[] _bufferLocks;

        /// <summary>
        /// A comparer used by the producer heap.
        /// </summary>
        private readonly IComparer<Producer<TKey>> _producerComparer;

        /// <summary>
        /// The initial capacity of the buffer queue. The value was chosen experimentally.
        /// </summary>
        internal const int INITIAL_BUFFER_SIZE = 128;

        /// <summary>
        /// If the consumer notices that the queue reached this limit, it will take the entire buffer from
        /// the producer, instead of just popping off one result. The value was chosen experimentally.
        /// </summary>
        internal const int STEAL_BUFFER_SIZE = 1024;

        /// <summary>
        /// If the producer notices that the queue reached this limit, it will go to sleep until woken up
        /// by the consumer. Chosen experimentally.
        /// </summary>
        internal const int MAX_BUFFER_SIZE = 8192;

        //-----------------------------------------------------------------------------------
        // Instantiates a new merge helper.
        //
        // Arguments:
        //     partitions   - the source partitions from which to consume data.
        //     ignoreOutput - whether we're enumerating "for effect" or for output.
        //

        internal OrderPreservingPipeliningMergeHelper(
            PartitionedStream<TOutput, TKey> partitions,
            TaskScheduler taskScheduler,
            CancellationState cancellationState,
            bool autoBuffered,
            int queryId,
            IComparer<TKey> keyComparer)
        {
            Debug.Assert(partitions != null);

            TraceHelpers.TraceInfo("KeyOrderPreservingMergeHelper::.ctor(..): creating an order preserving merge helper");

            _taskGroupState = new QueryTaskGroupState(cancellationState, queryId);
            _partitions = partitions;
            _taskScheduler = taskScheduler;
            _autoBuffered = autoBuffered;

            int partitionCount = _partitions.PartitionCount;
            _buffers = new Queue<Pair<TKey, TOutput>>[partitionCount];
            _producerDone = new bool[partitionCount];
            _consumerWaiting = new bool[partitionCount];
            _producerWaiting = new bool[partitionCount];
            _bufferLocks = new object[partitionCount];
            if (keyComparer == Util.GetDefaultComparer<int>())
            {
                Debug.Assert(typeof(TKey) == typeof(int));
                _producerComparer = (IComparer<Producer<TKey>>)(object)ProducerComparerInt.Instance;
            }
            else
            {
                _producerComparer = new ProducerComparer(keyComparer);
            }
        }

        //-----------------------------------------------------------------------------------
        // Schedules execution of the merge itself.
        //

        void IMergeHelper<TOutput>.Execute()
        {
            OrderPreservingPipeliningSpoolingTask<TOutput, TKey>.Spool(
                _taskGroupState, _partitions, _consumerWaiting, _producerWaiting, _producerDone,
                _buffers, _bufferLocks, _taskScheduler, _autoBuffered);
        }

        //-----------------------------------------------------------------------------------
        // Gets the enumerator from which to enumerate output results.
        //

        IEnumerator<TOutput> IMergeHelper<TOutput>.GetEnumerator()
        {
            return new OrderedPipeliningMergeEnumerator(this, _producerComparer);
        }

        //-----------------------------------------------------------------------------------
        // Returns the results as an array.
        //

        [ExcludeFromCodeCoverage(Justification = "An ordered pipelining merge is not intended to be used this way")]
        public TOutput[] GetResultsAsArray()
        {
            Debug.Fail("An ordered pipelining merge is not intended to be used this way.");
            throw new InvalidOperationException();
        }

        /// <summary>
        /// A comparer used by FixedMaxHeap(Of Producer)
        ///
        /// This comparer will be used by max-heap. We want the producer with the smallest MaxKey to
        /// end up in the root of the heap.
        ///
        ///     x.MaxKey GREATER_THAN y.MaxKey  =>  x LESS_THAN y     => return -
        ///     x.MaxKey EQUALS y.MaxKey        =>  x EQUALS y        => return 0
        ///     x.MaxKey LESS_THAN y.MaxKey     =>  x GREATER_THAN y  => return +
        /// </summary>
        private sealed class ProducerComparer : IComparer<Producer<TKey>>
        {
            private readonly IComparer<TKey> _keyComparer;

            internal ProducerComparer(IComparer<TKey> keyComparer)
            {
                _keyComparer = keyComparer;
            }

            public int Compare(Producer<TKey> x, Producer<TKey> y)
            {
                return _keyComparer.Compare(y.MaxKey, x.MaxKey);
            }
        }

        /// <summary>
        /// Enumerator over the results of an order-preserving pipelining merge.
        /// </summary>
        private sealed class OrderedPipeliningMergeEnumerator : MergeEnumerator<TOutput>

        {
            /// <summary>
            /// Merge helper associated with this enumerator
            /// </summary>
            private readonly OrderPreservingPipeliningMergeHelper<TOutput, TKey> _mergeHelper;

            /// <summary>
            /// Heap used to efficiently locate the producer whose result should be consumed next.
            /// For each producer, stores the order index for the next element to be yielded.
            ///
            /// Read and written by the consumer only.
            /// </summary>
            private readonly FixedMaxHeap<Producer<TKey>> _producerHeap;

            /// <summary>
            /// Stores the next element to be yielded from each producer. We use a separate array
            /// rather than storing this information in the producer heap to keep the Producer struct
            /// small.
            ///
            /// Read and written by the consumer only.
            /// </summary>
            private readonly TOutput[] _producerNextElement;

            /// <summary>
            /// A private buffer for the consumer. When the size of a producer buffer exceeds a threshold
            /// (STEAL_BUFFER_SIZE), the consumer will take ownership of the entire buffer, and give the
            /// producer a new empty buffer to place results into.
            ///
            /// Read and written by the consumer only.
            /// </summary>
            private readonly Queue<Pair<TKey, TOutput>>?[] _privateBuffer;

            /// <summary>
            /// Tracks whether MoveNext() has already been called previously.
            /// </summary>
            private bool _initialized;

            /// <summary>
            /// Constructor
            /// </summary>
            internal OrderedPipeliningMergeEnumerator(OrderPreservingPipeliningMergeHelper<TOutput, TKey> mergeHelper, IComparer<Producer<TKey>> producerComparer)
                : base(mergeHelper._taskGroupState)
            {
                int partitionCount = mergeHelper._partitions.PartitionCount;

                _mergeHelper = mergeHelper;
                _producerHeap = new FixedMaxHeap<Producer<TKey>>(partitionCount, producerComparer);
                _privateBuffer = new Queue<Pair<TKey, TOutput>>[partitionCount];
                _producerNextElement = new TOutput[partitionCount];
            }

            /// <summary>
            /// Returns the current result
            /// </summary>
            public override TOutput Current
            {
                get
                {
                    int producerToYield = _producerHeap.MaxValue.ProducerIndex;
                    return _producerNextElement[producerToYield];
                }
            }

            /// <summary>
            /// Moves the enumerator to the next result, or returns false if there are no more results to yield.
            /// </summary>
            public override bool MoveNext()
            {
                if (!_initialized)
                {
                    //
                    // Initialization: wait until each producer has produced at least one element. Since the order indices
                    // are increasing, we cannot start yielding until we have at least one element from each producer.
                    //

                    _initialized = true;

                    for (int producer = 0; producer < _mergeHelper._partitions.PartitionCount; producer++)
                    {
                        Pair<TKey, TOutput> element = default(Pair<TKey, TOutput>);

                        // Get the first element from this producer
                        if (TryWaitForElement(producer, ref element))
                        {
                            // Update the producer heap and its helper array with the received element
                            _producerHeap.Insert(new Producer<TKey>(element.First, producer));
                            _producerNextElement[producer] = element.Second;
                        }
                        else
                        {
                            // If this producer didn't produce any results because it encountered an exception,
                            // cancellation would have been initiated by now. If cancellation has started, we will
                            // propagate the exception now.
                            ThrowIfInTearDown();
                        }
                    }
                }
                else
                {
                    // If the producer heap is empty, we are done. In fact, we know that a previous MoveNext() call
                    // already returned false.
                    if (_producerHeap.Count == 0)
                    {
                        return false;
                    }

                    //
                    // Get the next element from the producer that yielded a value last. Update the producer heap.
                    // The next producer to yield will be in the front of the producer heap.
                    //

                    // The last producer whose result the merge yielded
                    int lastProducer = _producerHeap.MaxValue.ProducerIndex;

                    // Get the next element from the same producer
                    Pair<TKey, TOutput> element = default(Pair<TKey, TOutput>);
                    if (TryGetPrivateElement(lastProducer, ref element)
                        || TryWaitForElement(lastProducer, ref element))
                    {
                        // Update the producer heap and its helper array with the received element
                        _producerHeap.ReplaceMax(new Producer<TKey>(element.First, lastProducer));
                        _producerNextElement[lastProducer] = element.Second;
                    }
                    else
                    {
                        // If this producer is done because it encountered an exception, cancellation
                        // would have been initiated by now. If cancellation has started, we will propagate
                        // the exception now.
                        ThrowIfInTearDown();

                        // This producer is done. Remove it from the producer heap.
                        _producerHeap.RemoveMax();
                    }
                }

                return _producerHeap.Count > 0;
            }

            /// <summary>
            /// If the cancellation of the query has been initiated (because one or more producers
            /// encountered exceptions, or because external cancellation token has been set), the method
            /// will tear down the query and rethrow the exception.
            /// </summary>
            private void ThrowIfInTearDown()
            {
                if (_mergeHelper._taskGroupState.CancellationState.MergedCancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // Wake up all producers. Since the cancellation token has already been
                        // set, the producers will eventually stop after waking up.
                        if (!ParallelEnumerable.SinglePartitionMode) {
                            object[] locks = _mergeHelper._bufferLocks;
                            for (int i = 0; i < locks.Length; i++)
                            {
                                lock (locks[i])
                                {
                                    Monitor.Pulse(locks[i]);
                                }
                            }
                        }

                        // Now, we wait for all producers to wake up, notice the cancellation and stop executing.
                        // QueryEnd will wait on all tasks to complete and then propagate all exceptions.
                        _taskGroupState.QueryEnd(false);

                        Debug.Fail("QueryEnd() should have thrown an exception.");
                    }
                    finally
                    {
                        // Clear the producer heap so that future calls to MoveNext simply return false.
                        _producerHeap.Clear();
                    }
                }
            }


            /// <summary>
            /// Wait until a producer's buffer is non-empty, or until that producer is done.
            /// </summary>
            /// <returns>false if there is no element to yield because the producer is done, true otherwise</returns>
            private bool TryWaitForElement(int producer, ref Pair<TKey, TOutput> element)
            {
                Queue<Pair<TKey, TOutput>> buffer = _mergeHelper._buffers[producer];
                object bufferLock = _mergeHelper._bufferLocks[producer];
                lock (bufferLock)
                {
                    // If the buffer is empty, we need to wait on the producer
                    if (buffer.Count == 0)
                    {
                        // If the producer is already done, return false
                        if (_mergeHelper._producerDone[producer])
                        {
                            element = default(Pair<TKey, TOutput>);
                            return false;
                        }

                        if (ParallelEnumerable.SinglePartitionMode)
                            return false;

                        _mergeHelper._consumerWaiting[producer] = true;
                        Monitor.Wait(bufferLock);

                        // If the buffer is still empty, the producer is done
                        if (buffer.Count == 0)
                        {
                            Debug.Assert(_mergeHelper._producerDone[producer]);
                            element = default(Pair<TKey, TOutput>);
                            return false;
                        }
                    }

                    Debug.Assert(buffer.Count > 0, "Producer's buffer should not be empty here.");


                    // If the producer is waiting, wake it up
                    if (_mergeHelper._producerWaiting[producer])
                    {
                        Debug.Assert(!ParallelEnumerable.SinglePartitionMode);
                        Monitor.Pulse(bufferLock);
                        _mergeHelper._producerWaiting[producer] = false;
                    }

                    if (buffer.Count < STEAL_BUFFER_SIZE)
                    {
                        element = buffer.Dequeue();

                        return true;
                    }
                    else
                    {
                        // Privatize the entire buffer
                        _privateBuffer[producer] = _mergeHelper._buffers[producer];

                        // Give an empty buffer to the producer
                        _mergeHelper._buffers[producer] = new Queue<Pair<TKey, TOutput>>(INITIAL_BUFFER_SIZE);
                        // No return statement.
                        // This is the only branch that continues below of the lock region.
                    }
                }

                // Get an element out of the private buffer.
                bool gotElement = TryGetPrivateElement(producer, ref element);
                Debug.Assert(gotElement);

                return true;
            }

            /// <summary>
            /// Looks for an element from a particular producer in the consumer's private buffer.
            /// </summary>
            private bool TryGetPrivateElement(int producer, ref Pair<TKey, TOutput> element)
            {
                var privateChunk = _privateBuffer[producer];
                if (privateChunk != null)
                {
                    if (privateChunk.Count > 0)
                    {
                        element = privateChunk.Dequeue();
                        return true;
                    }

                    Debug.Assert(_privateBuffer[producer]!.Count == 0);
                    _privateBuffer[producer] = null;
                }

                return false;
            }

            public override void Dispose()
            {
                // Wake up any waiting producers
                if (!ParallelEnumerable.SinglePartitionMode) {
                    int partitionCount = _mergeHelper._buffers.Length;
                    for (int producer = 0; producer < partitionCount; producer++)
                    {
                        object bufferLock = _mergeHelper._bufferLocks[producer];
                        lock (bufferLock)
                        {
                            if (_mergeHelper._producerWaiting[producer])
                            {
                                Monitor.Pulse(bufferLock);
                            }
                        }
                    }
                }

                base.Dispose();
            }
        }
    }

    /// <summary>
    /// A structure to represent a producer in the producer heap.
    /// </summary>
    internal readonly struct Producer<TKey>
    {
        internal readonly TKey MaxKey; // Order index of the next element from this producer
        internal readonly int ProducerIndex; // Index of the producer, [0..DOP)

        internal Producer(TKey maxKey, int producerIndex)
        {
            MaxKey = maxKey;
            ProducerIndex = producerIndex;
        }
    }


    /// <summary>
    /// A comparer used by FixedMaxHeap(Of Producer)
    ///
    /// This comparer will be used by max-heap. We want the producer with the smallest MaxKey to
    /// end up in the root of the heap.
    ///
    ///     x.MaxKey GREATER_THAN y.MaxKey  =>  x LESS_THAN y     => return -
    ///     x.MaxKey EQUALS y.MaxKey        =>  x EQUALS y        => return 0
    ///     x.MaxKey LESS_THAN y.MaxKey     =>  x GREATER_THAN y  => return +
    /// </summary>
    internal sealed class ProducerComparerInt : IComparer<Producer<int>>
    {
        public static readonly ProducerComparerInt Instance = new ProducerComparerInt();

        private ProducerComparerInt() { }

        public int Compare(Producer<int> x, Producer<int> y)
        {
            Debug.Assert(x.MaxKey >= 0 && y.MaxKey >= 0); // Guarantees no overflow on next line

            return y.MaxKey - x.MaxKey;
        }
    }
}
