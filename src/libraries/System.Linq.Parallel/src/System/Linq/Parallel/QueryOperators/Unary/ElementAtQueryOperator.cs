// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ElementAtQueryOperator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// ElementAt just retrieves an element at a specific index.  There is some cross-partition
    /// coordination to force partitions to stop looking once a partition has found the
    /// sought-after element.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    internal sealed class ElementAtQueryOperator<TSource> : UnaryQueryOperator<TSource, TSource>
    {
        private readonly int _index; // The index that we're looking for.
        private readonly bool _prematureMerge = false; // Whether to prematurely merge the input of this operator.
        private readonly bool _limitsParallelism = false; // Whether this operator limits parallelism

        //---------------------------------------------------------------------------------------
        // Constructs a new instance of the contains search operator.
        //
        // Arguments:
        //     child       - the child tree to enumerate.
        //     index       - index we are searching for.
        //

        internal ElementAtQueryOperator(IEnumerable<TSource> child, int index)
            : base(child)
        {
            Debug.Assert(child != null, "child data source cannot be null");
            Debug.Assert(index >= 0, "index can't be less than 0");
            _index = index;

            OrdinalIndexState childIndexState = Child.OrdinalIndexState;
            if (ExchangeUtilities.IsWorseThan(childIndexState, OrdinalIndexState.Correct))
            {
                _prematureMerge = true;
                _limitsParallelism = childIndexState != OrdinalIndexState.Shuffled;
            }
        }

        //---------------------------------------------------------------------------------------
        // Just opens the current operator, including opening the child and wrapping it with
        // partitions as needed.
        //

        internal override QueryResults<TSource> Open(
            QuerySettings settings, bool preferStriping)
        {
            // We just open the child operator.
            QueryResults<TSource> childQueryResults = Child.Open(settings, false);
            return new UnaryQueryOperatorResults(childQueryResults, this, settings, preferStriping);
        }

        internal override void WrapPartitionedStream<TKey>(
            PartitionedStream<TSource, TKey> inputStream, IPartitionedStreamRecipient<TSource> recipient, bool preferStriping, QuerySettings settings)
        {
            // If the child OOP index is not correct, reindex.
            int partitionCount = inputStream.PartitionCount;

            PartitionedStream<TSource, int> intKeyStream;
            if (_prematureMerge)
            {
                intKeyStream = ExecuteAndCollectResults(inputStream, partitionCount, Child.OutputOrdered, preferStriping, settings).GetPartitionedStream();
                Debug.Assert(intKeyStream.OrdinalIndexState == OrdinalIndexState.Indexable);
            }
            else
            {
                intKeyStream = (PartitionedStream<TSource, int>)(object)inputStream;
            }

            // Create a shared cancellation variable and then return a possibly wrapped new enumerator.
            Shared<bool> resultFoundFlag = new Shared<bool>(false);

            PartitionedStream<TSource, int> outputStream = new PartitionedStream<TSource, int>(
                partitionCount, Util.GetDefaultComparer<int>(), OrdinalIndexState.Correct);

            for (int i = 0; i < partitionCount; i++)
            {
                outputStream[i] = new ElementAtQueryOperatorEnumerator(intKeyStream[i], _index, resultFoundFlag, settings.CancellationState.MergedCancellationToken);
            }

            recipient.Receive(outputStream);
        }

        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //

        [ExcludeFromCodeCoverage]
        internal override IEnumerable<TSource> AsSequentialQuery(CancellationToken token)
        {
            Debug.Fail("This method should never be called as fallback to sequential is handled in Aggregate().");
            throw new NotSupportedException();
        }

        //---------------------------------------------------------------------------------------
        // Whether this operator performs a premature merge that would not be performed in
        // a similar sequential operation (i.e., in LINQ to Objects).
        //

        internal override bool LimitsParallelism
        {
            get { return _limitsParallelism; }
        }


        /// <summary>
        /// Executes the query, either sequentially or in parallel, depending on the query execution mode and
        /// whether a premature merge was inserted by this ElementAt operator.
        /// </summary>
        /// <param name="result">result</param>
        /// <param name="withDefaultValue">withDefaultValue</param>
        /// <returns>whether an element with this index exists</returns>
        internal bool Aggregate(out TSource result, bool withDefaultValue)
        {
            // If we were to insert a premature merge before this ElementAt, and we are executing in conservative mode, run the whole query
            // sequentially.
            if (LimitsParallelism && SpecifiedQuerySettings.WithDefaults().ExecutionMode.Value != ParallelExecutionMode.ForceParallelism)
            {
                CancellationState cancelState = SpecifiedQuerySettings.CancellationState;
                if (withDefaultValue)
                {
                    IEnumerable<TSource> childAsSequential = Child.AsSequentialQuery(cancelState.ExternalCancellationToken);
                    IEnumerable<TSource> childWithCancelChecks = CancellableEnumerable.Wrap(childAsSequential, cancelState.ExternalCancellationToken);
                    result = ExceptionAggregator.WrapEnumerable(childWithCancelChecks, cancelState).ElementAtOrDefault(_index);
                }
                else
                {
                    IEnumerable<TSource> childAsSequential = Child.AsSequentialQuery(cancelState.ExternalCancellationToken);
                    IEnumerable<TSource> childWithCancelChecks = CancellableEnumerable.Wrap(childAsSequential, cancelState.ExternalCancellationToken);
                    result = ExceptionAggregator.WrapEnumerable(childWithCancelChecks, cancelState).ElementAt(_index);
                }
                return true;
            }

            using (IEnumerator<TSource> e = GetEnumerator(ParallelMergeOptions.FullyBuffered))
            {
                if (e.MoveNext())
                {
                    TSource current = e.Current;
                    Debug.Assert(!e.MoveNext(), "expected enumerator to be empty");
                    result = current;
                    return true;
                }
            }

            result = default(TSource);
            return false;
        }


        //---------------------------------------------------------------------------------------
        // This enumerator performs the search for the element at the specified index.
        //

        class ElementAtQueryOperatorEnumerator : QueryOperatorEnumerator<TSource, int>
        {
            private QueryOperatorEnumerator<TSource, int> _source; // The source data.
            private int _index; // The index of the element to seek.
            private Shared<bool> _resultFoundFlag; // Whether to cancel the operation.
            private CancellationToken _cancellationToken;

            //---------------------------------------------------------------------------------------
            // Instantiates a new any/all search operator.
            //

            internal ElementAtQueryOperatorEnumerator(QueryOperatorEnumerator<TSource, int> source,
                                                      int index, Shared<bool> resultFoundFlag,
                CancellationToken cancellationToken)
            {
                Debug.Assert(source != null);
                Debug.Assert(index >= 0);
                Debug.Assert(resultFoundFlag != null);

                _source = source;
                _index = index;
                _resultFoundFlag = resultFoundFlag;
                _cancellationToken = cancellationToken;
            }

            //---------------------------------------------------------------------------------------
            // Enumerates the entire input until the element with the specified is found or another
            // partition has signaled that it found the element.
            //

            internal override bool MoveNext(ref TSource currentElement, ref int currentKey)
            {
                // Just walk the enumerator until we've found the element.
                int i = 0;
                while (_source.MoveNext(ref currentElement, ref currentKey))
                {
                    if ((i++ & CancellationState.POLL_INTERVAL) == 0)
                        CancellationState.ThrowIfCanceled(_cancellationToken);

                    if (_resultFoundFlag.Value)
                    {
                        // Another partition found the element.
                        break;
                    }

                    if (currentKey == _index)
                    {
                        // We have found the element. Cancel other searches and return true.
                        _resultFoundFlag.Value = true;
                        return true;
                    }
                }

                return false;
            }

            protected override void Dispose(bool disposing)
            {
                Debug.Assert(_source != null);
                _source.Dispose();
            }
        }
    }
}
