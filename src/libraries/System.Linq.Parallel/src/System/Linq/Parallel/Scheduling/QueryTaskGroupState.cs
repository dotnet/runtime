// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// QueryTaskGroupState.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace System.Linq.Parallel
{
    /// <summary>
    /// A collection of tasks used by a single query instance. This type also offers some
    /// convenient methods for tracing significant ETW events, waiting on tasks, propagating
    /// exceptions, and performing cancellation activities.
    /// </summary>
    internal sealed class QueryTaskGroupState
    {
        private Task? _rootTask; // The task under which all query tasks root.
        private int _alreadyEnded; // Whether the tasks have been waited on already.
        private readonly CancellationState _cancellationState; // The cancellation state.
        private readonly int _queryId; // Id of this query execution.


        //-----------------------------------------------------------------------------------
        // Creates a new shared bit of state among tasks.
        //

        internal QueryTaskGroupState(CancellationState cancellationState, int queryId)
        {
            _cancellationState = cancellationState;
            _queryId = queryId;
        }

        //-----------------------------------------------------------------------------------
        // Whether this query has ended or not.
        //

        internal bool IsAlreadyEnded
        {
            get { return _alreadyEnded == 1; }
        }

        //-----------------------------------------------------------------------------------
        // Cancellation state, used to tear down tasks cooperatively when necessary.
        //

        internal CancellationState CancellationState
        {
            get { return _cancellationState; }
        }

        //-----------------------------------------------------------------------------------
        // Id of this query execution.
        //

        internal int QueryId
        {
            get { return _queryId; }
        }

        //-----------------------------------------------------------------------------------
        // Marks the beginning of a query's execution.
        //

        internal void QueryBegin(Task rootTask)
        {
            Debug.Assert(rootTask != null, "Expected a non-null task");
            Debug.Assert(_rootTask == null, "Cannot begin a query more than once");
            _rootTask = rootTask;
        }

        //-----------------------------------------------------------------------------------
        // Marks the end of a query's execution, waiting for all tasks to finish and
        // propagating any relevant exceptions.  Note that the full set of tasks must have
        // been initialized (with SetTask) before calling this.
        //

        internal void QueryEnd(bool userInitiatedDispose)
        {
            Debug.Assert(_rootTask != null);
            //Debug.Assert(Task.Current == null || (Task.Current != _rootTask && Task.Current.Parent != _rootTask));

            if (Interlocked.Exchange(ref _alreadyEnded, 1) == 0)
            {
                // There are four cases:
                // Case #1: Wait produced an exception that is not OCE(ct), or an AggregateException which is not full of OCE(ct) ==>  We rethrow.
                // Case #2: External cancellation has been requested ==> we'll manually throw OCE(externalToken).
                // Case #3a: We are servicing a call to Dispose() (and possibly also external cancellation has been requested).. simply return.
                // Case #3b: The enumerator has already been disposed (and possibly also external cancellation was requested).  Throw an ODE.
                // Case #4: No exceptions or explicit call to Dispose() by this caller ==> we just return.

                // See also "InlinedAggregationOperator" which duplicates some of this logic for the aggregators.
                // See also "QueryOpeningEnumerator" which duplicates some of this logic.
                // See also "ExceptionAggregator" which duplicates some of this logic.

                try
                {
                    // Wait for all the tasks to complete
                    // If any of the tasks ended in the Faulted stated, an AggregateException will be thrown.
                    _rootTask.Wait();
                }
                catch (AggregateException ae)
                {
                    AggregateException flattenedAE = ae.Flatten();
                    bool allOCEsOnTrackedExternalCancellationToken = true;
                    for (int i = 0; i < flattenedAE.InnerExceptions.Count; i++)
                    {
                        OperationCanceledException? oce = flattenedAE.InnerExceptions[i] as OperationCanceledException;

                        // we only let it pass through iff:
                        // it is not null, not default, and matches the exact token we were given as being the external token
                        // and the external Token is actually canceled (i.e. not a spoof OCE(extCT) for a non-canceled extCT)
                        if (oce == null ||
                            !oce.CancellationToken.IsCancellationRequested ||
                            oce.CancellationToken != _cancellationState.ExternalCancellationToken)
                        {
                            allOCEsOnTrackedExternalCancellationToken = false;
                            break;
                        }
                    }

                    // if all the exceptions were OCE(externalToken), then we will propagate only a single OCE(externalToken) below
                    // otherwise, we flatten the aggregate (because the WaitAll above already aggregated) and rethrow.
                    if (!allOCEsOnTrackedExternalCancellationToken || flattenedAE.InnerExceptions.Count == 0)
                        throw flattenedAE;  // Case #1
                }
                finally
                {
                    //_rootTask don't support Dispose on some platforms
                    (_rootTask as IDisposable)?.Dispose();
                }

                if (_cancellationState.MergedCancellationToken.IsCancellationRequested)
                {
                    // cancellation has occurred but no user-delegate exceptions were detected

                    // NOTE: it is important that we see other state variables correctly here, and that
                    // read-reordering hasn't played havoc.
                    // This is OK because
                    //   1. all the state writes (e,g. in the Initiate* methods) are volatile writes (standard .NET MM)
                    //   2. tokenCancellationRequested is backed by a volatile field, hence the reads below
                    //   won't get reordered about the read of token.IsCancellationRequested.

                    // If the query has already been disposed, we don't want to throw an OCE
                    if (!_cancellationState.TopLevelDisposedFlag.Value)
                    {
                        CancellationState.ThrowWithStandardMessageIfCanceled(_cancellationState.ExternalCancellationToken); // Case #2
                    }

                    //otherwise, given that there were no user-delegate exceptions (they would have been rethrown above),
                    //the only remaining situation is user-initiated dispose.
                    Debug.Assert(_cancellationState.TopLevelDisposedFlag.Value);

                    // If we aren't actively disposing, that means somebody else previously disposed
                    // of the enumerator. We must throw an ObjectDisposedException.
                    if (!userInitiatedDispose)
                    {
                        throw new ObjectDisposedException("enumerator", SR.PLINQ_DisposeRequested); // Case #3
                    }
                }
                // Case #4. nothing to do.
            }
        }
    }
}
