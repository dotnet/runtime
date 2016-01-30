// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// OrderPreservingMergeHelper.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;

namespace System.Linq.Parallel
{
    /// <summary>
    /// The order preserving merge helper guarantees the output stream is in a specific order. This is done
    /// by comparing keys from a set of already-sorted input partitions, and coalescing output data using
    /// incremental key comparisons.
    /// </summary>
    /// <typeparam name="TInputOutput"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    internal class OrderPreservingMergeHelper<TInputOutput, TKey> : IMergeHelper<TInputOutput>
    {
        private QueryTaskGroupState _taskGroupState; // State shared among tasks.
        private PartitionedStream<TInputOutput, TKey> _partitions; // Source partitions.
        private Shared<TInputOutput[]> _results; // The array where results are stored.
        private TaskScheduler _taskScheduler; // The task manager to execute the query.

        //-----------------------------------------------------------------------------------
        // Instantiates a new merge helper.
        //
        // Arguments:
        //     partitions   - the source partitions from which to consume data.
        //     ignoreOutput - whether we're enumerating "for effect" or for output.
        //

        internal OrderPreservingMergeHelper(PartitionedStream<TInputOutput, TKey> partitions, TaskScheduler taskScheduler,
            CancellationState cancellationState, int queryId)
        {
            Debug.Assert(partitions != null);

            TraceHelpers.TraceInfo("KeyOrderPreservingMergeHelper::.ctor(..): creating an order preserving merge helper");

            _taskGroupState = new QueryTaskGroupState(cancellationState, queryId);
            _partitions = partitions;
            _results = new Shared<TInputOutput[]>(null);
            _taskScheduler = taskScheduler;
        }

        //-----------------------------------------------------------------------------------
        // Schedules execution of the merge itself.
        //
        // Arguments:
        //    ordinalIndexState - the state of the ordinal index of the merged partitions
        //

        void IMergeHelper<TInputOutput>.Execute()
        {
            OrderPreservingSpoolingTask<TInputOutput, TKey>.Spool(_taskGroupState, _partitions, _results, _taskScheduler);
        }

        //-----------------------------------------------------------------------------------
        // Gets the enumerator from which to enumerate output results.
        //

        IEnumerator<TInputOutput> IMergeHelper<TInputOutput>.GetEnumerator()
        {
            Debug.Assert(_results.Value != null);
            return ((IEnumerable<TInputOutput>)_results.Value).GetEnumerator();
        }


        //-----------------------------------------------------------------------------------
        // Returns the results as an array.
        //

        public TInputOutput[] GetResultsAsArray()
        {
            return _results.Value;
        }
    }
}
