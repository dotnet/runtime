// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// SpoolingTask.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq.Parallel
{
    /// <summary>
    /// A factory class to execute spooling logic.
    /// </summary>
    internal static class SpoolingTask
    {
        //-----------------------------------------------------------------------------------
        // Creates and begins execution of a new spooling task. Executes synchronously,
        // and by the time this API has returned all of the results have been produced.
        //
        // Arguments:
        //     groupState      - values for inter-task communication
        //     partitions      - the producer enumerators
        //     channels        - the producer-consumer channels
        //     taskScheduler   - the task manager on which to execute
        //

        internal static void SpoolStopAndGo<TInputOutput, TIgnoreKey>(
            QueryTaskGroupState groupState, PartitionedStream<TInputOutput, TIgnoreKey> partitions,
            SynchronousChannel<TInputOutput>[] channels, TaskScheduler taskScheduler)
        {
            Debug.Assert(partitions.PartitionCount == channels.Length);
            Debug.Assert(groupState != null);

            // Ensure all tasks in this query are parented under a common root.
            Task rootTask = new Task(
                () =>
                {
                    int maxToRunInParallel = partitions.PartitionCount - 1;

                    // A stop-and-go merge uses the current thread for one task and then blocks before
                    // returning to the caller, until all results have been accumulated. We do this by
                    // running the last partition on the calling thread.
                    for (int i = 0; i < maxToRunInParallel; i++)
                    {
                        TraceHelpers.TraceInfo("SpoolingTask::Spool: Running partition[{0}] asynchronously", i);

                        QueryTask asyncTask = new StopAndGoSpoolingTask<TInputOutput, TIgnoreKey>(i, groupState, partitions[i], channels[i]);
                        asyncTask.RunAsynchronously(taskScheduler);
                    }

                    TraceHelpers.TraceInfo("SpoolingTask::Spool: Running partition[{0}] synchronously", maxToRunInParallel);

                    // Run one task synchronously on the current thread.
                    QueryTask syncTask = new StopAndGoSpoolingTask<TInputOutput, TIgnoreKey>(
                        maxToRunInParallel, groupState, partitions[maxToRunInParallel], channels[maxToRunInParallel]);
                    syncTask.RunSynchronously(taskScheduler);
                });

            // Begin the query on the calling thread.
            groupState.QueryBegin(rootTask);

            // We don't want to return until the task is finished.  Run it on the calling thread.
            rootTask.RunSynchronously(taskScheduler);

            // Wait for the query to complete, propagate exceptions, and so on.
            // For pipelined queries, this step happens in the async enumerator.
            groupState.QueryEnd(false);
        }

        //-----------------------------------------------------------------------------------
        // Creates and begins execution of a new spooling task. Runs asynchronously.
        //
        // Arguments:
        //     groupState      - values for inter-task communication
        //     partitions      - the producer enumerators
        //     channels        - the producer-consumer channels
        //     taskScheduler   - the task manager on which to execute
        //

#if !FEATURE_WASM_THREADS
        [System.Runtime.Versioning.UnsupportedOSPlatform("browser")]
#endif
        internal static void SpoolPipeline<TInputOutput, TIgnoreKey>(
            QueryTaskGroupState groupState, PartitionedStream<TInputOutput, TIgnoreKey> partitions,
            AsynchronousChannel<TInputOutput>[] channels, TaskScheduler taskScheduler)
        {
            Debug.Assert(partitions.PartitionCount == channels.Length);
            Debug.Assert(groupState != null);

            // Ensure all tasks in this query are parented under a common root. Because this
            // is a pipelined query, we detach it from the parent (to avoid blocking the calling
            // thread), and run the query on a separate thread.
            Task rootTask = new Task(
                () =>
                {
                    // Create tasks that will enumerate the partitions in parallel. Because we're pipelining,
                    // we will begin running these tasks in parallel and then return.
                    for (int i = 0; i < partitions.PartitionCount; i++)
                    {
                        TraceHelpers.TraceInfo("SpoolingTask::Spool: Running partition[{0}] asynchronously", i);

                        QueryTask asyncTask = new PipelineSpoolingTask<TInputOutput, TIgnoreKey>(i, groupState, partitions[i], channels[i]);
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

        //-----------------------------------------------------------------------------------
        // Creates and begins execution of a new spooling task. This is a for-all style
        // execution, meaning that the query will be run fully (for effect) before returning
        // and that there are no channels into which data will be queued.
        //
        // Arguments:
        //     groupState      - values for inter-task communication
        //     partitions      - the producer enumerators
        //     taskScheduler   - the task manager on which to execute
        //

        internal static void SpoolForAll<TInputOutput, TIgnoreKey>(
            QueryTaskGroupState groupState, PartitionedStream<TInputOutput, TIgnoreKey> partitions, TaskScheduler taskScheduler)
        {
            Debug.Assert(groupState != null);

            // Ensure all tasks in this query are parented under a common root.
            Task rootTask = new Task(
                () =>
                {
                    int maxToRunInParallel = partitions.PartitionCount - 1;

                    // Create tasks that will enumerate the partitions in parallel "for effect"; in other words,
                    // no data will be placed into any kind of producer-consumer channel.
                    for (int i = 0; i < maxToRunInParallel; i++)
                    {
                        TraceHelpers.TraceInfo("SpoolingTask::Spool: Running partition[{0}] asynchronously", i);

                        QueryTask asyncTask = new ForAllSpoolingTask<TInputOutput, TIgnoreKey>(i, groupState, partitions[i]);
                        asyncTask.RunAsynchronously(taskScheduler);
                    }

                    TraceHelpers.TraceInfo("SpoolingTask::Spool: Running partition[{0}] synchronously", maxToRunInParallel);

                    // Run one task synchronously on the current thread.
                    QueryTask syncTask = new ForAllSpoolingTask<TInputOutput, TIgnoreKey>(maxToRunInParallel, groupState, partitions[maxToRunInParallel]);
                    syncTask.RunSynchronously(taskScheduler);
                });

            // Begin the query on the calling thread.
            groupState.QueryBegin(rootTask);

            // We don't want to return until the task is finished.  Run it on the calling thread.
            rootTask.RunSynchronously(taskScheduler);

            // Wait for the query to complete, propagate exceptions, and so on.
            // For pipelined queries, this step happens in the async enumerator.
            groupState.QueryEnd(false);
        }
    }

    /// <summary>
    /// A spooling task handles marshaling data from a producer to a consumer. It's given
    /// a single enumerator object that contains all of the production algorithms, a single
    /// destination channel from which consumers draw results, and (optionally) a
    /// synchronization primitive using which to notify asynchronous consumers.
    /// </summary>
    /// <typeparam name="TInputOutput"></typeparam>
    /// <typeparam name="TIgnoreKey"></typeparam>
    internal sealed class StopAndGoSpoolingTask<TInputOutput, TIgnoreKey> : SpoolingTaskBase
    {
        // The data source from which to pull data.
        private readonly QueryOperatorEnumerator<TInputOutput, TIgnoreKey> _source;

        // The destination channel into which data is placed. This can be null if we are
        // enumerating "for effect", e.g. forall loop.
        private readonly SynchronousChannel<TInputOutput> _destination;

        //-----------------------------------------------------------------------------------
        // Creates, but does not execute, a new spooling task.
        //
        // Arguments:
        //     taskIndex   - the unique index of this task
        //     source      - the producer enumerator
        //     destination - the destination channel into which to spool elements
        //
        // Assumptions:
        //     Source cannot be null, although the other arguments may be.
        //

        internal StopAndGoSpoolingTask(
            int taskIndex, QueryTaskGroupState groupState,
            QueryOperatorEnumerator<TInputOutput, TIgnoreKey> source, SynchronousChannel<TInputOutput> destination)
            : base(taskIndex, groupState)
        {
            Debug.Assert(source != null);
            _source = source;
            _destination = destination;
        }

        //-----------------------------------------------------------------------------------
        // This method is responsible for enumerating results and enqueueing them to
        // the output channel(s) as appropriate.  Each base class implements its own.
        //

        protected override void SpoolingWork()
        {
            // We just enumerate over the entire source data stream, placing each element
            // into the destination channel.
            TInputOutput current = default(TInputOutput)!;
            TIgnoreKey keyUnused = default(TIgnoreKey)!;

            QueryOperatorEnumerator<TInputOutput, TIgnoreKey> source = _source;
            SynchronousChannel<TInputOutput> destination = _destination;
            CancellationToken cancelToken = _groupState.CancellationState.MergedCancellationToken;

            destination.Init();
            while (source.MoveNext(ref current!, ref keyUnused))
            {
                // If an abort has been requested, stop this worker immediately.
                if (cancelToken.IsCancellationRequested)
                {
                    break;
                }

                destination.Enqueue(current);
            }
        }

        //-----------------------------------------------------------------------------------
        // Ensure we signal that the channel is complete.
        //

        protected override void SpoolingFinally()
        {
            // Call the base implementation.
            base.SpoolingFinally();

            // Signal that we are done, in the case of asynchronous consumption.
            _destination?.SetDone();

            // Dispose of the source enumerator *after* signaling that the task is done.
            // We call Dispose() last to ensure that if it throws an exception, we will not cause a deadlock.
            _source.Dispose();
        }
    }

    /// <summary>
    /// A spooling task handles marshaling data from a producer to a consumer. It's given
    /// a single enumerator object that contains all of the production algorithms, a single
    /// destination channel from which consumers draw results, and (optionally) a
    /// synchronization primitive using which to notify asynchronous consumers.
    /// </summary>
    /// <typeparam name="TInputOutput"></typeparam>
    /// <typeparam name="TIgnoreKey"></typeparam>
#if !FEATURE_WASM_THREADS
    [System.Runtime.Versioning.UnsupportedOSPlatform("browser")]
#endif
    internal sealed class PipelineSpoolingTask<TInputOutput, TIgnoreKey> : SpoolingTaskBase
    {
        // The data source from which to pull data.
        private readonly QueryOperatorEnumerator<TInputOutput, TIgnoreKey> _source;

        // The destination channel into which data is placed. This can be null if we are
        // enumerating "for effect", e.g. forall loop.
        private readonly AsynchronousChannel<TInputOutput> _destination;

        //-----------------------------------------------------------------------------------
        // Creates, but does not execute, a new spooling task.
        //
        // Arguments:
        //     taskIndex   - the unique index of this task
        //     source      - the producer enumerator
        //     destination - the destination channel into which to spool elements
        //
        // Assumptions:
        //     Source cannot be null, although the other arguments may be.
        //

        internal PipelineSpoolingTask(
            int taskIndex, QueryTaskGroupState groupState,
            QueryOperatorEnumerator<TInputOutput, TIgnoreKey> source, AsynchronousChannel<TInputOutput> destination)
            : base(taskIndex, groupState)
        {
            Debug.Assert(source != null);
            _source = source;
            _destination = destination;
        }

        //-----------------------------------------------------------------------------------
        // This method is responsible for enumerating results and enqueueing them to
        // the output channel(s) as appropriate.  Each base class implements its own.
        //

        protected override void SpoolingWork()
        {
            // We just enumerate over the entire source data stream, placing each element
            // into the destination channel.
            TInputOutput current = default(TInputOutput)!;
            TIgnoreKey keyUnused = default(TIgnoreKey)!;

            QueryOperatorEnumerator<TInputOutput, TIgnoreKey> source = _source;
            AsynchronousChannel<TInputOutput> destination = _destination;
            CancellationToken cancelToken = _groupState.CancellationState.MergedCancellationToken;

            while (source.MoveNext(ref current!, ref keyUnused))
            {
                // If an abort has been requested, stop this worker immediately.
                if (cancelToken.IsCancellationRequested)
                {
                    break;
                }

                destination.Enqueue(current);
            }

            // Flush remaining data to the query consumer in preparation for channel shutdown.
            destination.FlushBuffers();
        }

        //-----------------------------------------------------------------------------------
        // Ensure we signal that the channel is complete.
        //

        protected override void SpoolingFinally()
        {
            // Call the base implementation.
            base.SpoolingFinally();

            // Signal that we are done, in the case of asynchronous consumption.
            _destination?.SetDone();

            // Dispose of the source enumerator *after* signaling that the task is done.
            // We call Dispose() last to ensure that if it throws an exception, we will not cause a deadlock.
            _source.Dispose();
        }
    }

    /// <summary>
    /// A spooling task handles marshaling data from a producer to a consumer. It's given
    /// a single enumerator object that contains all of the production algorithms, a single
    /// destination channel from which consumers draw results, and (optionally) a
    /// synchronization primitive using which to notify asynchronous consumers.
    /// </summary>
    /// <typeparam name="TInputOutput"></typeparam>
    /// <typeparam name="TIgnoreKey"></typeparam>
    internal sealed class ForAllSpoolingTask<TInputOutput, TIgnoreKey> : SpoolingTaskBase
    {
        // The data source from which to pull data.
        private readonly QueryOperatorEnumerator<TInputOutput, TIgnoreKey> _source;

        //-----------------------------------------------------------------------------------
        // Creates, but does not execute, a new spooling task.
        //
        // Arguments:
        //     taskIndex   - the unique index of this task
        //     source      - the producer enumerator
        //     destination - the destination channel into which to spool elements
        //
        // Assumptions:
        //     Source cannot be null, although the other arguments may be.
        //

        internal ForAllSpoolingTask(
            int taskIndex, QueryTaskGroupState groupState,
            QueryOperatorEnumerator<TInputOutput, TIgnoreKey> source)
            : base(taskIndex, groupState)
        {
            Debug.Assert(source != null);
            _source = source;
        }

        //-----------------------------------------------------------------------------------
        // This method is responsible for enumerating results and enqueueing them to
        // the output channel(s) as appropriate.  Each base class implements its own.
        //

        protected override void SpoolingWork()
        {
            // We just enumerate over the entire source data stream for effect.
            TInputOutput currentUnused = default(TInputOutput)!;
            TIgnoreKey keyUnused = default(TIgnoreKey)!;

            //Note: this only ever runs with a ForAll operator, and ForAllEnumerator performs cancellation checks
            while (_source.MoveNext(ref currentUnused!, ref keyUnused))
                ;
        }

        //-----------------------------------------------------------------------------------
        // Ensure we signal that the channel is complete.
        //

        protected override void SpoolingFinally()
        {
            // Call the base implementation.
            base.SpoolingFinally();

            // Dispose of the source enumerator
            _source.Dispose();
        }
    }
}
