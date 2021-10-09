// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// QueryOpeningEnumerator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace System.Linq.Parallel
{
    /// <summary>
    /// A wrapper enumerator that just opens the query operator when MoveNext() is called for the
    /// first time. We use QueryOpeningEnumerator to call QueryOperator.GetOpenedEnumerator()
    /// lazily because once GetOpenedEnumerator() is called, PLINQ starts precomputing the
    /// results of the query.
    /// </summary>
    internal sealed class QueryOpeningEnumerator<TOutput> : IEnumerator<TOutput>
    {
        private readonly QueryOperator<TOutput> _queryOperator;
        private IEnumerator<TOutput>? _openedQueryEnumerator;
        private QuerySettings _querySettings;
        private readonly ParallelMergeOptions? _mergeOptions;
        private readonly bool _suppressOrderPreservation;
        private int _moveNextIteration;
        private bool _hasQueryOpeningFailed;

        // -- Cancellation and Dispose fields--
        // Disposal of the queryOpeningEnumerator can trigger internal cancellation and so it is important
        // that the internal cancellation signal is available both at this level, and deep in query execution
        // Also, it is useful to track the cause of cancellation so that appropriate exceptions etc can be
        // throw from the execution managers.
        // => Both the topLevelDisposeFlag and the topLevelCancellationSignal are defined here, and will be shared
        //    down to QuerySettings and to the QueryTaskGroupStates that are associated with actual task-execution.
        // => whilst these are the definitions, it is best to consider QuerySettings as the true owner of these.
        private readonly Shared<bool> _topLevelDisposedFlag = new Shared<bool>(false);  //a shared<bool> so that it can be referenced by others.

        // a top-level cancellation signal is required so that QueryOpeningEnumerator.Dispose() can tear things down.
        // This cancellationSignal will be used as the actual internal signal in QueryTaskGroupState.
        private readonly CancellationTokenSource _topLevelCancellationTokenSource = new CancellationTokenSource();


        internal QueryOpeningEnumerator(QueryOperator<TOutput> queryOperator, ParallelMergeOptions? mergeOptions, bool suppressOrderPreservation)
        {
            Debug.Assert(queryOperator != null);

            _queryOperator = queryOperator;
            _mergeOptions = mergeOptions;
            _suppressOrderPreservation = suppressOrderPreservation;
        }

        public TOutput Current
        {
            get
            {
                if (_openedQueryEnumerator == null)
                {
                    throw new InvalidOperationException(SR.PLINQ_CommonEnumerator_Current_NotStarted);
                }

                return _openedQueryEnumerator.Current;
            }
        }

        public void Dispose()
        {
            _topLevelDisposedFlag.Value = true;
            _topLevelCancellationTokenSource.Cancel(); // initiate internal cancellation.
            if (_openedQueryEnumerator != null)
            {
                _openedQueryEnumerator.Dispose();
                _querySettings.CleanStateAtQueryEnd();
            }

            QueryLifecycle.LogicalQueryExecutionEnd(_querySettings.QueryId);
        }

        object? IEnumerator.Current
        {
            get { return ((IEnumerator<TOutput>)this).Current; }
        }

        public bool MoveNext()
        {
            if (_topLevelDisposedFlag.Value)
            {
                throw new ObjectDisposedException("enumerator", SR.PLINQ_DisposeRequested);
            }


            //Note: if Dispose has been called on a different thread to the thread that is enumerating,
            //then there is a race condition where _openedQueryEnumerator is instantiated but not disposed.
            //Best practice is that Dispose() should only be called by the owning thread, hence this cannot occur in correct usage scenarios.

            // Open the query operator if called for the first time.

            if (_openedQueryEnumerator == null)
            {
                // To keep the MoveNext method body small, the code that executes first time only is in a separate method.
                // It appears that if the method becomes too large, we observe a performance regression. This may have
                // to do with method inlining.
                OpenQuery();
            }

            bool innerMoveNextResult = _openedQueryEnumerator!.MoveNext();

            // This provides cancellation-testing for the consumer-side of the buffers that appears in each scenario:
            //   Non-order-preserving (defaultMergeHelper)
            //       - asynchronous channel (pipelining)
            //       - synchronous channel  (stop-and-go)
            //   Order-preserving (orderPreservingMergeHelper)
            //       - internal results buffer.
            // This moveNext is consuming data out of buffers, hence the inner moveNext is expected to be very fast.
            // => thus we only test for cancellation per-N-iterations.
            // NOTE: the cancellation check occurs after performing moveNext in case the cancellation caused no data
            //       to be produced.. We need to ensure that users sees an OCE rather than simply getting no data. (see Bug702254)
            if ((_moveNextIteration & CancellationState.POLL_INTERVAL) == 0)
            {
                CancellationState.ThrowWithStandardMessageIfCanceled(
                    _querySettings.CancellationState.ExternalCancellationToken);
            }

            _moveNextIteration++;
            return innerMoveNextResult;
        }

        /// <summary>
        /// Opens the query and initializes _openedQueryEnumerator and _querySettings.
        /// Called from the first MoveNext call.
        /// </summary>
        private void OpenQuery()
        {
            // Avoid opening (and failing) twice.. not only would it be bad to re-enumerate some elements, but
            // the cancellation/disposed flags are most likely stale.
            if (_hasQueryOpeningFailed)
                throw new InvalidOperationException(SR.PLINQ_EnumerationPreviouslyFailed);

            try
            {
                // stuff in appropriate defaults for unspecified options.
                _querySettings = _queryOperator.SpecifiedQuerySettings
                    .WithPerExecutionSettings(_topLevelCancellationTokenSource, _topLevelDisposedFlag)
                    .WithDefaults();

                QueryLifecycle.LogicalQueryExecutionBegin(_querySettings.QueryId);

                _openedQueryEnumerator = _queryOperator.GetOpenedEnumerator(
                    _mergeOptions, _suppressOrderPreservation, false, _querySettings);


                // Now that we have opened the query, and got our hands on a supplied cancellation token
                // we can perform an early cancellation check so that we will not do any major work if the token is already canceled.
                CancellationState.ThrowWithStandardMessageIfCanceled(_querySettings.CancellationState.ExternalCancellationToken);
            }
            catch
            {
                _hasQueryOpeningFailed = true;
                throw;
            }
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }
    }
}
