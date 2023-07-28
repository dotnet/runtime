// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// QueryOperator.cs
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
    /// This is the abstract base class for all query operators in the system. It
    /// implements the ParallelQuery{T} type so that it can be bound as the source
    /// of parallel queries and so that it can be returned as the result of parallel query
    /// operations. Not much is in here, although it does serve as the "entry point" for
    /// opening all query operators: it will lazily analyze and cache a plan the first
    /// time the tree is opened, and will open the tree upon calls to GetEnumerator.
    ///
    /// Notes:
    ///     This class implements ParallelQuery so that any parallel query operator
    ///     can bind to the parallel query provider overloads. This allows us to string
    ///     together operators w/out the user always specifying AsParallel, e.g.
    ///     Select(Where(..., ...), ...), and so forth.
    /// </summary>
    /// <typeparam name="TOutput"></typeparam>
    internal abstract class QueryOperator<TOutput> : ParallelQuery<TOutput>
    {
        protected bool _outputOrdered;

        internal QueryOperator(QuerySettings settings)
            : this(false, settings)
        {
        }

        internal QueryOperator(bool isOrdered, QuerySettings settings)
            : base(settings)
        {
            _outputOrdered = isOrdered;
        }

        //---------------------------------------------------------------------------------------
        // Opening the query operator will do whatever is necessary to begin enumerating its
        // results. This includes in some cases actually introducing parallelism, enumerating
        // other query operators, and so on. This is abstract and left to the specific concrete
        // operator classes to implement.
        //
        // Arguments:
        //     settings - various flags and settings to control query execution
        //     preferStriping - flag representing whether the caller prefers striped partitioning
        //                      over range partitioning
        //
        // Return Values:
        //     Either a single enumerator, or a partition (for partition parallelism).
        //

        internal abstract QueryResults<TOutput> Open(QuerySettings settings, bool preferStriping);

        //---------------------------------------------------------------------------------------
        // The GetEnumerator method is the standard IEnumerable mechanism for walking the
        // contents of a query. Note that GetEnumerator is only ever called on the root node:
        // we then proceed by calling Open on all of the subsequent query nodes.
        //
        // Arguments:
        //     usePipelining     - whether the returned enumerator will pipeline (i.e. return
        //                         control to the caller when the query is spawned) or not
        //                         (i.e. use the calling thread to execute the query).  Note
        //                         that there are some conditions during which this hint will
        //                         be ignored -- currently, that happens only if a sort is
        //                         found anywhere in the query graph.
        //     suppressOrderPreservation - whether to shut order preservation off, regardless
        //                                 of the contents of the query
        //
        // Return Value:
        //     An enumerator that retrieves elements from the query output.
        //
        // Notes:
        //     The default mode of execution is to pipeline the query execution with respect
        //     to the GetEnumerator caller (aka the consumer). An overload is available
        //     that can be used to override the default with an explicit choice.
        //

        public override IEnumerator<TOutput> GetEnumerator()
        {
            // Buffering is unspecified and  order preservation is not suppressed.
            return GetEnumerator(null, false);
        }

        public IEnumerator<TOutput> GetEnumerator(ParallelMergeOptions? mergeOptions)
        {
            // Pass through the value supplied for pipelining, and do not suppress
            // order preservation by default.
            return GetEnumerator(mergeOptions, false);
        }

        //---------------------------------------------------------------------------------------
        // Is the output of this operator ordered?
        //

        internal bool OutputOrdered
        {
            get { return _outputOrdered; }
        }

        internal virtual IEnumerator<TOutput> GetEnumerator(ParallelMergeOptions? mergeOptions, bool suppressOrderPreservation)
        {
            // Return a dummy enumerator that will call back GetOpenedEnumerator() on 'this' QueryOperator
            // the first time the user calls MoveNext(). We do this to prevent executing the query if user
            // never calls MoveNext().
            return new QueryOpeningEnumerator<TOutput>(this, mergeOptions, suppressOrderPreservation);
        }

        //---------------------------------------------------------------------------------------
        // The GetOpenedEnumerator method return an enumerator that walks the contents of a query.
        // The enumerator will be "opened", which means that PLINQ will start executing the query
        // immediately, even before the user calls MoveNext() for the first time.
        //
        internal IEnumerator<TOutput>? GetOpenedEnumerator(ParallelMergeOptions? mergeOptions, bool suppressOrder, bool forEffect,
            QuerySettings querySettings)
        {
            Debug.Assert(querySettings.ExecutionMode != null);
            // If the top-level enumerator forces a premature merge, run the query sequentially.
            if (querySettings.ExecutionMode.Value == ParallelExecutionMode.Default && LimitsParallelism)
            {
                IEnumerable<TOutput> opSequential = AsSequentialQuery(querySettings.CancellationState.ExternalCancellationToken);
                return ExceptionAggregator.WrapEnumerable(opSequential, querySettings.CancellationState).GetEnumerator();
            }

            QueryResults<TOutput> queryResults = GetQueryResults(querySettings);

            mergeOptions ??= querySettings.MergeOptions;
            Debug.Assert(mergeOptions != null);

            // Top-level preemptive cancellation test.
            // This handles situations where cancellation has occurred before execution commences
            // The handling for in-execution occurs in QueryTaskGroupState.QueryEnd()

            if (querySettings.CancellationState.MergedCancellationToken.IsCancellationRequested)
            {
                querySettings.CancellationState.ExternalCancellationToken.ThrowIfCancellationRequested();
                throw new OperationCanceledException();
            }

            bool orderedMerge = OutputOrdered && !suppressOrder;

            Debug.Assert(querySettings.TaskScheduler != null);
            PartitionedStreamMerger<TOutput> merger = new PartitionedStreamMerger<TOutput>(forEffect, mergeOptions.GetValueOrDefault(),
                                                                                           querySettings.TaskScheduler,
                                                                                           orderedMerge,
                                                                                           querySettings.CancellationState,
                                                                                           querySettings.QueryId);

            queryResults.GivePartitionedStream(merger); // hook up the data flow between the operator-executors, starting from the merger.

            if (forEffect)
            {
                return null;
            }

            Debug.Assert(merger.MergeExecutor != null);
            return merger.MergeExecutor.GetEnumerator();
        }


        // This method is called only once on the 'head operator' which is the last specified operator in the query
        // This method then recursively uses Open() to prepare itself and the other enumerators.
        private QueryResults<TOutput> GetQueryResults(QuerySettings querySettings)
        {
            TraceHelpers.TraceInfo("[timing]: {0}: starting execution - QueryOperator<>::GetQueryResults", DateTime.Now.Ticks);

            // All mandatory query settings must be specified
            Debug.Assert(querySettings.TaskScheduler != null);
            Debug.Assert(querySettings.DegreeOfParallelism.HasValue);
            Debug.Assert(querySettings.ExecutionMode.HasValue);

            // Now just open the query tree's root operator, supplying a specific DOP
            return Open(querySettings, false);
        }

        //---------------------------------------------------------------------------------------
        // Executes the query and returns the results in an array.
        //

        internal TOutput[] ExecuteAndGetResultsAsArray()
        {
            QuerySettings querySettings =
                SpecifiedQuerySettings
                .WithPerExecutionSettings()
                .WithDefaults();

            QueryLifecycle.LogicalQueryExecutionBegin(querySettings.QueryId);
            try
            {
                Debug.Assert(querySettings.ExecutionMode != null);
                if (querySettings.ExecutionMode.Value == ParallelExecutionMode.Default && LimitsParallelism)
                {
                    IEnumerable<TOutput> opSequential = AsSequentialQuery(querySettings.CancellationState.ExternalCancellationToken);
                    IEnumerable<TOutput> opSequentialWithCancelChecks = CancellableEnumerable.Wrap(opSequential, querySettings.CancellationState.ExternalCancellationToken);
                    return ExceptionAggregator.WrapEnumerable(opSequentialWithCancelChecks, querySettings.CancellationState).ToArray();
                }

                QueryResults<TOutput> results = GetQueryResults(querySettings);

                // Top-level preemptive cancellation test.
                // This handles situations where cancellation has occurred before execution commences
                // The handling for in-execution occurs in QueryTaskGroupState.QueryEnd()

                if (querySettings.CancellationState.MergedCancellationToken.IsCancellationRequested)
                {
                    querySettings.CancellationState.ExternalCancellationToken.ThrowIfCancellationRequested();
                    throw new OperationCanceledException();
                }

                if (results.IsIndexible && OutputOrdered)
                {
                    // The special array-based merge performs better if the output is ordered, because
                    // it does not have to pay for ordering. In the unordered case, we it appears that
                    // the stop-and-go merge performs a little better.
                    ArrayMergeHelper<TOutput> merger = new ArrayMergeHelper<TOutput>(SpecifiedQuerySettings, results);
                    merger.Execute();
                    TOutput[] output = merger.GetResultsAsArray();
                    querySettings.CleanStateAtQueryEnd();
                    return output;
                }
                else
                {
                    Debug.Assert(querySettings.TaskScheduler != null);
                    PartitionedStreamMerger<TOutput> merger =
                        new PartitionedStreamMerger<TOutput>(false, ParallelMergeOptions.FullyBuffered, querySettings.TaskScheduler,
                            OutputOrdered, querySettings.CancellationState, querySettings.QueryId);
                    results.GivePartitionedStream(merger);
                    Debug.Assert(merger.MergeExecutor != null);
                    TOutput[]? output = merger.MergeExecutor.GetResultsAsArray();
                    querySettings.CleanStateAtQueryEnd();
                    Debug.Assert(output != null);
                    return output;
                }
            }
            finally
            {
                QueryLifecycle.LogicalQueryExecutionEnd(querySettings.QueryId);
            }
        }

        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //
        // Note that iterating the returned enumerable will not wrap exceptions AggregateException.
        // Before this enumerable is returned to the user, we must wrap it with an
        // ExceptionAggregator.
        //

        internal abstract IEnumerable<TOutput> AsSequentialQuery(CancellationToken token);


        //---------------------------------------------------------------------------------------
        // Whether this operator performs a premature merge.
        //

        internal abstract bool LimitsParallelism { get; }

        //---------------------------------------------------------------------------------------
        // The state of the order index of the results returned by this operator.
        //

        internal abstract OrdinalIndexState OrdinalIndexState { get; }

        //---------------------------------------------------------------------------------------
        // A helper method that executes the query rooted at the openedChild operator, and returns
        // the results as ListQueryResults<TSource>.
        //

        internal static ListQueryResults<TOutput> ExecuteAndCollectResults<TKey>(
            PartitionedStream<TOutput, TKey> openedChild,
            int partitionCount,
            bool outputOrdered,
            bool useStriping,
            QuerySettings settings)
        {
            TaskScheduler? taskScheduler = settings.TaskScheduler;
            Debug.Assert(taskScheduler != null);

            MergeExecutor<TOutput> executor = MergeExecutor<TOutput>.Execute<TKey>(
                openedChild, false, ParallelMergeOptions.FullyBuffered, taskScheduler, outputOrdered,
                settings.CancellationState, settings.QueryId);
            return new ListQueryResults<TOutput>(executor.GetResultsAsArray()!, partitionCount, useStriping);
        }


        //---------------------------------------------------------------------------------------
        // Returns a QueryOperator<T> for any IEnumerable<T> data source. This will just do a
        // cast and return a reference to the same data source if the source is another query
        // operator, but will lazily allocate a scan operation and return that otherwise.
        //
        // Arguments:
        //    source  - any enumerable data source to be wrapped
        //
        // Return Value:
        //    A query operator.
        //

        internal static QueryOperator<TOutput> AsQueryOperator(IEnumerable<TOutput> source)
        {
            Debug.Assert(source != null);

            // Just try casting the data source to a query operator, in the case that
            // our child is just another query operator.
            QueryOperator<TOutput>? sourceAsOperator = source as QueryOperator<TOutput>;

            if (sourceAsOperator == null)
            {
                if (source is OrderedParallelQuery<TOutput>  orderedQuery)
                {
                    // We have to handle OrderedParallelQuery<T> specially. In all other cases,
                    // ParallelQuery *is* the QueryOperator<T>. But, OrderedParallelQuery<T>
                    // is not QueryOperator<T>, it only has a reference to one. Ideally, we
                    // would want SortQueryOperator<T> to inherit from OrderedParallelQuery<T>,
                    // but that conflicts with other constraints on our class hierarchy.
                    sourceAsOperator = (QueryOperator<TOutput>)orderedQuery.SortOperator;
                }
                else
                {
                    // If the cast failed, then the data source is a real piece of data. We
                    // just construct a new scan operator on top of it.
                    sourceAsOperator = new ScanQueryOperator<TOutput>(source);
                }
            }

            Debug.Assert(sourceAsOperator != null);

            return sourceAsOperator;
        }
    }
}
