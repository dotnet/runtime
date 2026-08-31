// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// PartitionedStreamMerger.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Linq.Parallel
{
    /// <summary>
    /// Partitioned stream recipient that will merge the results.
    /// </summary>
    internal sealed class PartitionedStreamMerger<TOutput> : IPartitionedStreamRecipient<TOutput>
    {
        private readonly bool _forEffectMerge;
        private readonly ParallelMergeOptions _mergeOptions;
        private readonly bool _isOrdered;
        private MergeExecutor<TOutput>? _mergeExecutor;
        private readonly TaskScheduler _taskScheduler;
        private readonly int _queryId; // ID of the current query execution

        private readonly CancellationState _cancellationState;

#if DEBUG
        private bool _received;
#endif
        // Returns the merge executor which merges the received partitioned stream.
        internal MergeExecutor<TOutput>? MergeExecutor
        {
            get
            {
#if DEBUG
                Debug.Assert(_received, "Cannot return the merge executor because Receive() has not been called yet.");
#endif
                return _mergeExecutor;
            }
        }

        internal PartitionedStreamMerger(bool forEffectMerge, ParallelMergeOptions mergeOptions, TaskScheduler taskScheduler, bool outputOrdered,
            CancellationState cancellationState, int queryId)
        {
            _forEffectMerge = forEffectMerge;
            _mergeOptions = mergeOptions;
            _isOrdered = outputOrdered;
            _taskScheduler = taskScheduler;
            _cancellationState = cancellationState;
            _queryId = queryId;
        }

        public void Receive<TKey>(PartitionedStream<TOutput, TKey> partitionedStream)
        {
#if DEBUG
            _received = true;
#endif
            _mergeExecutor = MergeExecutor<TOutput>.Execute<TKey>(
                partitionedStream, _forEffectMerge, _mergeOptions, _taskScheduler, _isOrdered, _cancellationState, _queryId);

            TraceHelpers.TraceInfo("[timing]: {0}: finished opening - QueryOperator<>::GetEnumerator", DateTime.Now.Ticks);
        }
    }
}
