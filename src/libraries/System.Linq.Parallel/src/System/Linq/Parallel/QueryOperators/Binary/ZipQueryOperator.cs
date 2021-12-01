// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ZipQueryOperator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// A Zip operator combines two input data sources into a single output stream,
    /// using a pairwise element matching algorithm. For example, the result of zipping
    /// two vectors a = {0, 1, 2, 3} and b = {9, 8, 7, 6} is the vector of pairs,
    /// c = {(0,9), (1,8), (2,7), (3,6)}. Because the expectation is that each element
    /// is matched with the element in the other data source at the same ordinal
    /// position, the zip operator requires order preservation.
    /// </summary>
    /// <typeparam name="TLeftInput"></typeparam>
    /// <typeparam name="TRightInput"></typeparam>
    /// <typeparam name="TOutput"></typeparam>
    internal sealed class ZipQueryOperator<TLeftInput, TRightInput, TOutput>
        : QueryOperator<TOutput>
    {
        private readonly Func<TLeftInput, TRightInput, TOutput> _resultSelector; // To select result elements.
        private readonly QueryOperator<TLeftInput> _leftChild;
        private readonly QueryOperator<TRightInput> _rightChild;
        private readonly bool _prematureMergeLeft; // Whether to prematurely merge the left data source
        private readonly bool _prematureMergeRight; // Whether to prematurely merge the right data source
        private readonly bool _limitsParallelism; // Whether this operator limits parallelism

        //---------------------------------------------------------------------------------------
        // Initializes a new zip operator.
        //
        // Arguments:
        //    leftChild     - the left data source from which to pull data.
        //    rightChild    - the right data source from which to pull data.
        //

        internal ZipQueryOperator(
            ParallelQuery<TLeftInput> leftChildSource, ParallelQuery<TRightInput> rightChildSource,
            Func<TLeftInput, TRightInput, TOutput> resultSelector)
            : this(
                QueryOperator<TLeftInput>.AsQueryOperator(leftChildSource),
                QueryOperator<TRightInput>.AsQueryOperator(rightChildSource),
                resultSelector)
        {
        }

        private ZipQueryOperator(
            QueryOperator<TLeftInput> left, QueryOperator<TRightInput> right,
            Func<TLeftInput, TRightInput, TOutput> resultSelector)
            : base(left.SpecifiedQuerySettings.Merge(right.SpecifiedQuerySettings))
        {
            Debug.Assert(resultSelector != null, "operator cannot be null");

            _leftChild = left;
            _rightChild = right;
            _resultSelector = resultSelector;
            _outputOrdered = _leftChild.OutputOrdered || _rightChild.OutputOrdered;

            OrdinalIndexState leftIndexState = _leftChild.OrdinalIndexState;
            OrdinalIndexState rightIndexState = _rightChild.OrdinalIndexState;

            _prematureMergeLeft = leftIndexState != OrdinalIndexState.Indexable;
            _prematureMergeRight = rightIndexState != OrdinalIndexState.Indexable;
            _limitsParallelism =
                (_prematureMergeLeft && leftIndexState != OrdinalIndexState.Shuffled)
                || (_prematureMergeRight && rightIndexState != OrdinalIndexState.Shuffled);
        }

        //---------------------------------------------------------------------------------------
        // Just opens the current operator, including opening the children and wrapping them with
        // partitions as needed.
        //

        internal override QueryResults<TOutput> Open(QuerySettings settings, bool preferStriping)
        {
            // We just open our child operators, left and then right.
            QueryResults<TLeftInput> leftChildResults = _leftChild.Open(settings, preferStriping);
            QueryResults<TRightInput> rightChildResults = _rightChild.Open(settings, preferStriping);

            Debug.Assert(settings.DegreeOfParallelism != null);
            int partitionCount = settings.DegreeOfParallelism.Value;
            Debug.Assert(settings.TaskScheduler != null);
            if (_prematureMergeLeft)
            {
                PartitionedStreamMerger<TLeftInput> merger = new PartitionedStreamMerger<TLeftInput>(
                    false, ParallelMergeOptions.FullyBuffered, settings.TaskScheduler, _leftChild.OutputOrdered,
                    settings.CancellationState, settings.QueryId);
                leftChildResults.GivePartitionedStream(merger);
                Debug.Assert(merger.MergeExecutor != null);
                leftChildResults = new ListQueryResults<TLeftInput>(
                    merger.MergeExecutor.GetResultsAsArray()!, partitionCount, preferStriping);
            }

            if (_prematureMergeRight)
            {
                PartitionedStreamMerger<TRightInput> merger = new PartitionedStreamMerger<TRightInput>(
                    false, ParallelMergeOptions.FullyBuffered, settings.TaskScheduler, _rightChild.OutputOrdered,
                    settings.CancellationState, settings.QueryId);
                rightChildResults.GivePartitionedStream(merger);
                Debug.Assert(merger.MergeExecutor != null);
                rightChildResults = new ListQueryResults<TRightInput>(
                    merger.MergeExecutor.GetResultsAsArray()!, partitionCount, preferStriping);
            }

            return new ZipQueryOperatorResults(leftChildResults, rightChildResults, _resultSelector, partitionCount, preferStriping);
        }

        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //

        internal override IEnumerable<TOutput> AsSequentialQuery(CancellationToken token)
        {
            using (IEnumerator<TLeftInput> leftEnumerator = _leftChild.AsSequentialQuery(token).GetEnumerator())
            using (IEnumerator<TRightInput> rightEnumerator = _rightChild.AsSequentialQuery(token).GetEnumerator())
            {
                while (leftEnumerator.MoveNext() && rightEnumerator.MoveNext())
                {
                    yield return _resultSelector(leftEnumerator.Current, rightEnumerator.Current);
                }
            }
        }

        //---------------------------------------------------------------------------------------
        // The state of the order index of the results returned by this operator.
        //

        internal override OrdinalIndexState OrdinalIndexState
        {
            get
            {
                return OrdinalIndexState.Indexable;
            }
        }

        //---------------------------------------------------------------------------------------
        // Whether this operator performs a premature merge that would not be performed in
        // a similar sequential operation (i.e., in LINQ to Objects).
        //

        internal override bool LimitsParallelism
        {
            get
            {
                return _limitsParallelism;
            }
        }

        //---------------------------------------------------------------------------------------
        // A special QueryResults class for the Zip operator. It requires that both of the child
        // QueryResults are indexable.
        //

        internal sealed class ZipQueryOperatorResults : QueryResults<TOutput>
        {
            private readonly QueryResults<TLeftInput> _leftChildResults;
            private readonly QueryResults<TRightInput> _rightChildResults;
            private readonly Func<TLeftInput, TRightInput, TOutput> _resultSelector; // To select result elements.
            private readonly int _count;
            private readonly int _partitionCount;
            private readonly bool _preferStriping;

            internal ZipQueryOperatorResults(
                QueryResults<TLeftInput> leftChildResults, QueryResults<TRightInput> rightChildResults,
                Func<TLeftInput, TRightInput, TOutput> resultSelector, int partitionCount, bool preferStriping)
            {
                _leftChildResults = leftChildResults;
                _rightChildResults = rightChildResults;
                _resultSelector = resultSelector;
                _partitionCount = partitionCount;
                _preferStriping = preferStriping;

                Debug.Assert(_leftChildResults.IsIndexible);
                Debug.Assert(_rightChildResults.IsIndexible);

                _count = Math.Min(_leftChildResults.Count, _rightChildResults.Count);
            }

            internal override int ElementsCount
            {
                get { return _count; }
            }

            internal override bool IsIndexible
            {
                get { return true; }
            }

            internal override TOutput GetElement(int index)
            {
                return _resultSelector(_leftChildResults.GetElement(index), _rightChildResults.GetElement(index));
            }

            internal override void GivePartitionedStream(IPartitionedStreamRecipient<TOutput> recipient)
            {
                PartitionedStream<TOutput, int> partitionedStream = ExchangeUtilities.PartitionDataSource(this, _partitionCount, _preferStriping);
                recipient.Receive(partitionedStream);
            }
        }
    }
}
