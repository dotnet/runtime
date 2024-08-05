// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// OrderPreservingSpoolingTask.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq.Parallel
{
    /// <summary>
    /// A spooling task handles marshaling data from a producer to a consumer. It's given
    /// a single enumerator object that contains all of the production algorithms, a single
    /// destination channel from which consumers draw results, and (optionally) a
    /// synchronization primitive using which to notify asynchronous consumers. This
    /// particular task variant preserves sort order in the final data.
    /// </summary>
    /// <typeparam name="TInputOutput"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    internal sealed class OrderPreservingSpoolingTask<TInputOutput, TKey> : SpoolingTaskBase
    {
        private readonly Shared<TInputOutput[]?> _results; // The destination array cell into which data is placed.
        private readonly SortHelper<TInputOutput> _sortHelper; // A helper that performs the sorting.

        //-----------------------------------------------------------------------------------
        // Creates, but does not execute, a new spooling task.
        //
        // Arguments:
        //     taskIndex   - the unique index of this task
        //     ordinalIndexState - the state of ordinal indices
        //     source      - the producer enumerator
        //     destination - the destination channel into which to spool elements
        //
        // Assumptions:
        //     Source cannot be null, although the other arguments may be.
        //

        private OrderPreservingSpoolingTask(
            int taskIndex, QueryTaskGroupState groupState,
            Shared<TInputOutput[]?> results, SortHelper<TInputOutput> sortHelper) :
            base(taskIndex, groupState)
        {
            Debug.Assert(groupState != null);
            Debug.Assert(results != null);
            Debug.Assert(sortHelper != null);

            _results = results;
            _sortHelper = sortHelper;
        }

        //-----------------------------------------------------------------------------------
        // Creates and begins execution of a new spooling task. If pipelineMerges is specified,
        // we will execute the task asynchronously; otherwise, this is done synchronously,
        // and by the time this API has returned all of the results have been produced.
        //
        // Arguments:
        //     source      - the producer enumerator
        //     destination - the destination channel into which to spool elements
        //     ordinalIndexState - state of the index of the input to the merge
        //
        // Assumptions:
        //     Source cannot be null, although the other arguments may be.
        //

        internal static void Spool(
            QueryTaskGroupState groupState, PartitionedStream<TInputOutput, TKey> partitions,
            Shared<TInputOutput[]?> results, TaskScheduler taskScheduler)
        {
            Debug.Assert(groupState != null);
            Debug.Assert(partitions != null);
            Debug.Assert(results != null);
            Debug.Assert(results.Value == null);

            // Determine how many async tasks to create.
            int maxToRunInParallel = partitions.PartitionCount - 1;

            // Generate a set of sort helpers.
            SortHelper<TInputOutput, TKey>[] sortHelpers =
                SortHelper<TInputOutput, TKey>.GenerateSortHelpers(partitions, groupState);

            // Ensure all tasks in this query are parented under a common root.
            Task rootTask = new Task(
                () =>
                {
                    // Create tasks that will enumerate the partitions in parallel.  We'll use the current
                    // thread for one task and then block before returning to the caller, until all results
                    // have been accumulated. Pipelining is not supported by sort merges.
                    for (int i = 0; i < maxToRunInParallel; i++)
                    {
                        TraceHelpers.TraceInfo("OrderPreservingSpoolingTask::Spool: Running partition[{0}] asynchronously", i);
                        QueryTask asyncTask = new OrderPreservingSpoolingTask<TInputOutput, TKey>(
                            i, groupState, results, sortHelpers[i]);
                        asyncTask.RunAsynchronously(taskScheduler);
                    }

                    // Run one task synchronously on the current thread.
                    TraceHelpers.TraceInfo("OrderPreservingSpoolingTask::Spool: Running partition[{0}] synchronously", maxToRunInParallel);
                    QueryTask syncTask = new OrderPreservingSpoolingTask<TInputOutput, TKey>(
                        maxToRunInParallel, groupState, results, sortHelpers[maxToRunInParallel]);
                    syncTask.RunSynchronously(taskScheduler);
                });

            // Begin the query on the calling thread.
            groupState.QueryBegin(rootTask);

            // We don't want to return until the task is finished.  Run it on the calling thread.
            rootTask.RunSynchronously(taskScheduler);

            // Destroy the state associated with our sort helpers.
            for (int i = 0; i < sortHelpers.Length; i++)
            {
                sortHelpers[i].Dispose();
            }

            // End the query, which has the effect of propagating any unhandled exceptions.
            groupState.QueryEnd(false);
        }

        //-----------------------------------------------------------------------------------
        // This method is responsible for enumerating results and enqueueing them to
        // the output channel(s) as appropriate.  Each base class implements its own.
        //

        protected override void SpoolingWork()
        {
            Debug.Assert(_sortHelper != null);

            // This task must perform a sort just prior to handing data to the merge.
            // We just defer to a sort helper object for this task.
            TInputOutput[] sortedOutput = _sortHelper.Sort();

            if (!_groupState.CancellationState.MergedCancellationToken.IsCancellationRequested)
            {
                // The 0th task is responsible for communicating the results to the merging infrastructure.
                // By this point, the results have been sorted, so we just publish a reference to the array.
                if (_taskIndex == 0)
                {
                    Debug.Assert(sortedOutput != null);
                    _results.Value = sortedOutput;
                }
            }
        }
    }
}
