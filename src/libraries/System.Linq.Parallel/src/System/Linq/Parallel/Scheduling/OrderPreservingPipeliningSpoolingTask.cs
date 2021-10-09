// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// OrderPreservingPipeliningSpoolingTask.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Parallel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace System.Linq.Parallel
{
    internal sealed class OrderPreservingPipeliningSpoolingTask<TOutput, TKey> : SpoolingTaskBase
    {
        private readonly QueryTaskGroupState _taskGroupState; // State shared among tasks.
        private readonly QueryOperatorEnumerator<TOutput, TKey> _partition; // The source partition.
        private readonly bool[] _consumerWaiting; // Whether a consumer is waiting on a particular producer
        private readonly bool[] _producerWaiting; // Whether a particular producer is waiting on the consumer
        private readonly bool[] _producerDone; // Whether each producer is done
        private readonly int _partitionIndex; // Index of the partition owned by this task.

        private readonly Queue<Pair<TKey, TOutput>>[] _buffers; // The buffer for the results
        private readonly object _bufferLock; // A lock for the buffer

        /// <summary>
        /// Whether the producer is allowed to buffer up elements before handing a chunk to the consumer.
        /// If false, the producer will make each result available to the consumer immediately after it is
        /// produced.
        /// </summary>
        private readonly bool _autoBuffered;

        /// <summary>
        /// The number of elements to accumulate on the producer before copying the elements to the
        /// producer-consumer buffer. This constant is only used in the AutoBuffered mode.
        ///
        /// Experimentally, 16 appears to be sufficient buffer size to compensate for the synchronization
        /// cost.
        /// </summary>
        private const int PRODUCER_BUFFER_AUTO_SIZE = 16;

        /// <summary>
        /// Constructor
        /// </summary>
        internal OrderPreservingPipeliningSpoolingTask(
            QueryOperatorEnumerator<TOutput, TKey> partition,
            QueryTaskGroupState taskGroupState,
            bool[] consumerWaiting,
            bool[] producerWaiting,
            bool[] producerDone,
            int partitionIndex,
            Queue<Pair<TKey, TOutput>>[] buffers,
            object bufferLock,
            bool autoBuffered)
            : base(partitionIndex, taskGroupState)
        {
            Debug.Assert(partition != null);
            Debug.Assert(taskGroupState != null);
            Debug.Assert(consumerWaiting != null);
            Debug.Assert(producerWaiting != null && producerWaiting.Length == consumerWaiting.Length);
            Debug.Assert(producerDone != null && producerDone.Length == consumerWaiting.Length);
            Debug.Assert(buffers != null && buffers.Length == consumerWaiting.Length);
            Debug.Assert(partitionIndex >= 0 && partitionIndex < consumerWaiting.Length);

            _partition = partition;
            _taskGroupState = taskGroupState;
            _producerDone = producerDone;
            _consumerWaiting = consumerWaiting;
            _producerWaiting = producerWaiting;
            _partitionIndex = partitionIndex;
            _buffers = buffers;
            _bufferLock = bufferLock;
            _autoBuffered = autoBuffered;
        }

        /// <summary>
        /// This method is responsible for enumerating results and enqueuing them to
        /// the output buffer as appropriate.  Each base class implements its own.
        /// </summary>
        protected override void SpoolingWork()
        {
            TOutput element = default(TOutput)!;
            TKey key = default(TKey)!;

            int chunkSize = _autoBuffered ? PRODUCER_BUFFER_AUTO_SIZE : 1;
            Pair<TKey, TOutput>[] chunk = new Pair<TKey, TOutput>[chunkSize];
            var partition = _partition;
            CancellationToken cancelToken = _taskGroupState.CancellationState.MergedCancellationToken;

            int lastChunkSize;
            do
            {
                lastChunkSize = 0;
                while (lastChunkSize < chunkSize && partition.MoveNext(ref element!, ref key))
                {
                    chunk[lastChunkSize] = new Pair<TKey, TOutput>(key, element);
                    lastChunkSize++;
                }

                if (lastChunkSize == 0) break;

                lock (_bufferLock)
                {
                    // Check if the query has been cancelled.
                    if (cancelToken.IsCancellationRequested)
                    {
                        break;
                    }

                    for (int i = 0; i < lastChunkSize; i++)
                    {
                        _buffers[_partitionIndex].Enqueue(chunk[i]);
                    }

                    if (_consumerWaiting[_partitionIndex])
                    {
                        Monitor.Pulse(_bufferLock);
                        _consumerWaiting[_partitionIndex] = false;
                    }

                    // If the producer buffer is too large, wait.
                    // Note: we already checked for cancellation after acquiring the lock on this producer.
                    // That guarantees that the consumer will eventually wake up the producer.
                    if (_buffers[_partitionIndex].Count >= OrderPreservingPipeliningMergeHelper<TOutput, TKey>.MAX_BUFFER_SIZE)
                    {
                        _producerWaiting[_partitionIndex] = true;
                        Monitor.Wait(_bufferLock);
                    }
                }
            } while (lastChunkSize == chunkSize);
        }


        /// <summary>
        /// Creates and begins execution of a new set of spooling tasks.
        /// </summary>
        public static void Spool(
            QueryTaskGroupState groupState, PartitionedStream<TOutput, TKey> partitions,
            bool[] consumerWaiting, bool[] producerWaiting, bool[] producerDone,
            Queue<Pair<TKey, TOutput>>[] buffers, object[] bufferLocks,
            TaskScheduler taskScheduler, bool autoBuffered)
        {
            Debug.Assert(groupState != null);
            Debug.Assert(partitions != null);
            Debug.Assert(producerDone != null && producerDone.Length == partitions.PartitionCount);
            Debug.Assert(buffers != null && buffers.Length == partitions.PartitionCount);
            Debug.Assert(bufferLocks != null);

            int degreeOfParallelism = partitions.PartitionCount;

            // Initialize the buffers and buffer locks.
            for (int i = 0; i < degreeOfParallelism; i++)
            {
                buffers[i] = new Queue<Pair<TKey, TOutput>>(OrderPreservingPipeliningMergeHelper<TOutput, TKey>.INITIAL_BUFFER_SIZE);
                bufferLocks[i] = new object();
            }

            // Ensure all tasks in this query are parented under a common root. Because this
            // is a pipelined query, we detach it from the parent (to avoid blocking the calling
            // thread), and run the query on a separate thread.
            Task rootTask = new Task(
                () =>
                {
                    for (int i = 0; i < degreeOfParallelism; i++)
                    {
                        QueryTask asyncTask = new OrderPreservingPipeliningSpoolingTask<TOutput, TKey>(
                            partitions[i], groupState, consumerWaiting, producerWaiting,
                            producerDone, i, buffers, bufferLocks[i], autoBuffered);
                        asyncTask.RunAsynchronously(taskScheduler);
                    }
                });

            // Begin the query on the calling thread.
            groupState.QueryBegin(rootTask);

            // And schedule it for execution.  This is done after beginning to ensure no thread tries to
            // end the query before its root task has been recorded properly.
            rootTask.Start(taskScheduler);
            // We don't call QueryEnd here; when we return, the query is still executing, and the
            // last enumerator to be disposed of will call QueryEnd for us.
        }

        /// <summary>
        /// Dispose the underlying enumerator and wake up the consumer if necessary.
        /// </summary>
        protected override void SpoolingFinally()
        {
            // Let the consumer know that this producer is done.
            lock (_bufferLock)
            {
                _producerDone[_partitionIndex] = true;
                if (_consumerWaiting[_partitionIndex])
                {
                    Monitor.Pulse(_bufferLock);
                    _consumerWaiting[_partitionIndex] = false;
                }
            }

            // Call the base implementation.
            base.SpoolingFinally();

            // Dispose of the source enumerator *after* signaling that the task is done.
            // We call Dispose() last to ensure that if it throws an exception, we will not cause a deadlock.
            _partition.Dispose();
        }
    }
}
