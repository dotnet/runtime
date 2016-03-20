// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// IndexedSelectQueryOperator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// A variant of the Select operator that supplies element index while performing the
    /// projection operation. This requires cooperation with partitioning and merging to
    /// guarantee ordering is preserved.
    ///
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    /// <typeparam name="TOutput"></typeparam>
    internal sealed class IndexedSelectQueryOperator<TInput, TOutput> : UnaryQueryOperator<TInput, TOutput>
    {
        // Selector function. Used to project elements to a transformed view during execution.
        private readonly Func<TInput, int, TOutput> _selector;
        private bool _prematureMerge = false; // Whether to prematurely merge the input of this operator.
        private bool _limitsParallelism = false; // Whether this operator limits parallelism

        //---------------------------------------------------------------------------------------
        // Initializes a new select operator.
        //
        // Arguments:
        //    child         - the child operator or data source from which to pull data
        //    selector      - a delegate representing the selector function
        //
        // Assumptions:
        //    selector must be non null.
        //

        internal IndexedSelectQueryOperator(IEnumerable<TInput> child,
                                            Func<TInput, int, TOutput> selector)
            : base(child)
        {
            Debug.Assert(child != null, "child data source cannot be null");
            Debug.Assert(selector != null, "need a selector function");

            _selector = selector;

            // In an indexed Select, elements must be returned in the order in which
            // indices were assigned.
            _outputOrdered = true;

            InitOrdinalIndexState();
        }

        private void InitOrdinalIndexState()
        {
            OrdinalIndexState childIndexState = Child.OrdinalIndexState;
            OrdinalIndexState indexState = childIndexState;

            if (ExchangeUtilities.IsWorseThan(childIndexState, OrdinalIndexState.Correct))
            {
                _prematureMerge = true;
                _limitsParallelism = childIndexState != OrdinalIndexState.Shuffled;
                indexState = OrdinalIndexState.Correct;
            }

            Debug.Assert(!ExchangeUtilities.IsWorseThan(indexState, OrdinalIndexState.Correct));

            SetOrdinalIndexState(indexState);
        }

        //---------------------------------------------------------------------------------------
        // Just opens the current operator, including opening the child and wrapping it with
        // partitions as needed.
        //

        internal override QueryResults<TOutput> Open(
            QuerySettings settings, bool preferStriping)
        {
            QueryResults<TInput> childQueryResults = Child.Open(settings, preferStriping);
            return IndexedSelectQueryOperatorResults.NewResults(childQueryResults, this, settings, preferStriping);
        }

        internal override void WrapPartitionedStream<TKey>(
            PartitionedStream<TInput, TKey> inputStream, IPartitionedStreamRecipient<TOutput> recipient, bool preferStriping, QuerySettings settings)
        {
            int partitionCount = inputStream.PartitionCount;

            // If the index is not correct, we need to reindex.
            PartitionedStream<TInput, int> inputStreamInt;
            if (_prematureMerge)
            {
                ListQueryResults<TInput> listResults = QueryOperator<TInput>.ExecuteAndCollectResults(
                    inputStream, partitionCount, Child.OutputOrdered, preferStriping, settings);
                inputStreamInt = listResults.GetPartitionedStream();
            }
            else
            {
                Debug.Assert(typeof(TKey) == typeof(int));
                inputStreamInt = (PartitionedStream<TInput, int>)(object)inputStream;
            }

            // Since the index is correct, the type of the index must be int
            PartitionedStream<TOutput, int> outputStream =
                new PartitionedStream<TOutput, int>(partitionCount, Util.GetDefaultComparer<int>(), OrdinalIndexState);

            for (int i = 0; i < partitionCount; i++)
            {
                outputStream[i] = new IndexedSelectQueryOperatorEnumerator(inputStreamInt[i], _selector);
            }

            recipient.Receive(outputStream);
        }


        //---------------------------------------------------------------------------------------
        // Whether this operator performs a premature merge that would not be performed in
        // a similar sequential operation (i.e., in LINQ to Objects).
        //

        internal override bool LimitsParallelism
        {
            get { return _limitsParallelism; }
        }

        //---------------------------------------------------------------------------------------
        // The enumerator type responsible for projecting elements as it is walked.
        //

        class IndexedSelectQueryOperatorEnumerator : QueryOperatorEnumerator<TOutput, int>
        {
            private readonly QueryOperatorEnumerator<TInput, int> _source; // The data source to enumerate.
            private readonly Func<TInput, int, TOutput> _selector;  // The actual select function.

            //---------------------------------------------------------------------------------------
            // Instantiates a new select enumerator.
            //

            internal IndexedSelectQueryOperatorEnumerator(QueryOperatorEnumerator<TInput, int> source, Func<TInput, int, TOutput> selector)
            {
                Debug.Assert(source != null);
                Debug.Assert(selector != null);
                _source = source;
                _selector = selector;
            }

            //---------------------------------------------------------------------------------------
            // Straightforward IEnumerator<T> methods.
            //

            internal override bool MoveNext(ref TOutput currentElement, ref int currentKey)
            {
                // So long as the source has a next element, we have an element.
                TInput element = default(TInput);
                if (_source.MoveNext(ref element, ref currentKey))
                {
                    Debug.Assert(_selector != null, "expected a compiled selection function");
                    currentElement = _selector(element, currentKey);
                    return true;
                }

                return false;
            }

            protected override void Dispose(bool disposing)
            {
                _source.Dispose();
            }
        }


        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //

        internal override IEnumerable<TOutput> AsSequentialQuery(CancellationToken token)
        {
            return Child.AsSequentialQuery(token).Select(_selector);
        }

        //-----------------------------------------------------------------------------------
        // Query results for an indexed Select operator. The results are indexable if the child
        // results were indexable.
        //

        class IndexedSelectQueryOperatorResults : UnaryQueryOperatorResults
        {
            private IndexedSelectQueryOperator<TInput, TOutput> _selectOp;  // Operator that generated the results
            private int _childCount; // The number of elements in child results

            public static QueryResults<TOutput> NewResults(
                QueryResults<TInput> childQueryResults, IndexedSelectQueryOperator<TInput, TOutput> op,
                QuerySettings settings, bool preferStriping)
            {
                if (childQueryResults.IsIndexible)
                {
                    return new IndexedSelectQueryOperatorResults(
                        childQueryResults, op, settings, preferStriping);
                }
                else
                {
                    return new UnaryQueryOperatorResults(
                        childQueryResults, op, settings, preferStriping);
                }
            }

            private IndexedSelectQueryOperatorResults(
                QueryResults<TInput> childQueryResults, IndexedSelectQueryOperator<TInput, TOutput> op,
                QuerySettings settings, bool preferStriping)
                : base(childQueryResults, op, settings, preferStriping)
            {
                _selectOp = op;
                Debug.Assert(_childQueryResults.IsIndexible);

                _childCount = _childQueryResults.ElementsCount;
            }

            internal override int ElementsCount
            {
                get
                {
                    Debug.Assert(_childCount >= 0);
                    return _childQueryResults.ElementsCount;
                }
            }

            internal override bool IsIndexible
            {
                get { return true; }
            }

            internal override TOutput GetElement(int index)
            {
                return _selectOp._selector(_childQueryResults.GetElement(index), index);
            }
        }
    }
}
