// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// MergeExecutor.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace System.Linq.Parallel
{
    /// <summary>
    /// Drives execution of an actual merge operation, including creating channel data
    /// structures and scheduling parallel work as appropriate. The algorithms used
    /// internally are parameterized based on the type of data in the partitions; e.g.
    /// if an order preserved stream is found, the merge will automatically use an
    /// order preserving merge, and so forth.
    /// </summary>
    /// <typeparam name="TInputOutput"></typeparam>
    internal sealed class MergeExecutor<TInputOutput> : IEnumerable<TInputOutput>
    {
        // Many internal algorithms are parameterized based on the data. The IMergeHelper
        // is the pluggable interface whose implementations perform those algorithms.
        private IMergeHelper<TInputOutput>? _mergeHelper;

        // Private constructor. MergeExecutor should only be constructed via the
        // MergeExecutor.Execute static method.
        private MergeExecutor()
        {
        }

        //-----------------------------------------------------------------------------------
        // Creates and executes a new merge executor object.
        //
        // Arguments:
        //     partitions   - the partitions whose data will be merged into one stream
        //     ignoreOutput - if true, we are enumerating "for effect", and we won't actually
        //                    generate data in the output stream
        //     pipeline     - whether to use a pipelined merge or not.
        //     isOrdered    - whether to perform an ordering merge.
        //

        internal static MergeExecutor<TInputOutput> Execute<TKey>(
            PartitionedStream<TInputOutput, TKey> partitions, bool ignoreOutput, ParallelMergeOptions options, TaskScheduler taskScheduler, bool isOrdered,
            CancellationState cancellationState, int queryId)
        {
            Debug.Assert(partitions != null);
            Debug.Assert(partitions.PartitionCount > 0);
            Debug.Assert(!ignoreOutput || options == ParallelMergeOptions.FullyBuffered, "Pipelining with no output is not supported.");

            MergeExecutor<TInputOutput> mergeExecutor = new MergeExecutor<TInputOutput>();
            if (isOrdered && !ignoreOutput)
            {
                if (options != ParallelMergeOptions.FullyBuffered && !partitions.OrdinalIndexState.IsWorseThan(OrdinalIndexState.Increasing))
                {
                    Debug.Assert(options == ParallelMergeOptions.NotBuffered || options == ParallelMergeOptions.AutoBuffered);
                    bool autoBuffered = (options == ParallelMergeOptions.AutoBuffered);

                    if (partitions.PartitionCount > 1)
                    {
                        // We use a pipelining ordered merge
                        mergeExecutor._mergeHelper = new OrderPreservingPipeliningMergeHelper<TInputOutput, TKey>(
                            partitions, taskScheduler, cancellationState, autoBuffered, queryId, partitions.KeyComparer);
                    }
                    else
                    {
                        // When DOP=1, the default merge simply returns the single producer enumerator to the consumer. This way, ordering
                        // does not add any extra overhead, and no producer task needs to be scheduled.
                        mergeExecutor._mergeHelper = new DefaultMergeHelper<TInputOutput, TKey>(
                            partitions, false, options, taskScheduler, cancellationState, queryId);
                    }
                }
                else
                {
                    // We use a stop-and-go ordered merge helper
                    mergeExecutor._mergeHelper = new OrderPreservingMergeHelper<TInputOutput, TKey>(partitions, taskScheduler, cancellationState, queryId);
                }
            }
            else
            {
                // We use a default - unordered - merge helper.
                mergeExecutor._mergeHelper = new DefaultMergeHelper<TInputOutput, TKey>(partitions, ignoreOutput, options, taskScheduler, cancellationState, queryId);
            }

            mergeExecutor.Execute();
            return mergeExecutor;
        }

        //-----------------------------------------------------------------------------------
        // Initiates execution of the merge.
        //

        private void Execute()
        {
            Debug.Assert(_mergeHelper != null);
            _mergeHelper.Execute();
        }

        //-----------------------------------------------------------------------------------
        // Returns an enumerator that will yield elements from the resulting merged data
        // stream.
        //

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<TInputOutput> GetEnumerator()
        {
            Debug.Assert(_mergeHelper != null);
            return _mergeHelper.GetEnumerator();
        }

        //-----------------------------------------------------------------------------------
        // Returns the merged results as an array.
        //

        internal TInputOutput[]? GetResultsAsArray()
        {
            Debug.Assert(_mergeHelper != null);
            return _mergeHelper.GetResultsAsArray();
        }

        //-----------------------------------------------------------------------------------
        // This internal helper method is used to generate a set of asynchronous channels.
        // The algorithm used by each channel contains the necessary synchronizations to
        // ensure it is suitable for pipelined consumption.
        //
        // Arguments:
        //     partitionsCount - the number of partitions for which to create new channels.
        //
        // Return Value:
        //     An array of asynchronous channels, one for each partition.
        //

        internal static AsynchronousChannel<TInputOutput>[] MakeAsynchronousChannels(int partitionCount, ParallelMergeOptions options, IntValueEvent? consumerEvent, CancellationToken cancellationToken)
        {
            AsynchronousChannel<TInputOutput>[] channels = new AsynchronousChannel<TInputOutput>[partitionCount];

            Debug.Assert(options == ParallelMergeOptions.NotBuffered || options == ParallelMergeOptions.AutoBuffered);
            TraceHelpers.TraceInfo("MergeExecutor::MakeChannels: setting up {0} async channels in prep for pipeline", partitionCount);

            // If we are pipelining, we need a channel that contains the necessary synchronization
            // in it. We choose a bounded/blocking channel data structure: bounded so that we can
            // limit the amount of memory overhead used by the query by putting a cap on the
            // buffer size into which producers place data, and blocking so that the consumer can
            // wait for additional data to arrive in the case that it's found to be empty.

            int chunkSize = 0; // 0 means automatic chunk size
            if (options == ParallelMergeOptions.NotBuffered)
            {
                chunkSize = 1;
            }

            for (int i = 0; i < channels.Length; i++)
            {
                channels[i] = new AsynchronousChannel<TInputOutput>(i, chunkSize, cancellationToken, consumerEvent);
            }

            return channels;
        }

        //-----------------------------------------------------------------------------------
        // This internal helper method is used to generate a set of synchronous channels.
        // The channel data structure used has been optimized for sequential execution and
        // does not support pipelining.
        //
        // Arguments:
        //     partitionsCount - the number of partitions for which to create new channels.
        //
        // Return Value:
        //     An array of synchronous channels, one for each partition.
        //

        internal static SynchronousChannel<TInputOutput>[] MakeSynchronousChannels(int partitionCount)
        {
            SynchronousChannel<TInputOutput>[] channels = new SynchronousChannel<TInputOutput>[partitionCount];

            TraceHelpers.TraceInfo("MergeExecutor::MakeChannels: setting up {0} channels in prep for stop-and-go", partitionCount);

            // We just build up the results in memory using simple, dynamically growable FIFO queues.
            for (int i = 0; i < channels.Length; i++)
            {
                channels[i] = new SynchronousChannel<TInputOutput>();
            }

            return channels;
        }
    }
}
