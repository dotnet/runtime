// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ConcatQueryOperator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// Concatenates one data source with another.  Order preservation is used to ensure
    /// the output is actually a concatenation -- i.e. one after the other.  The only
    /// special synchronization required is to find the largest index N in the first data
    /// source so that the indices of elements in the second data source can be offset
    /// by adding N+1.  This makes it appear to the order preservation infrastructure as
    /// though all elements in the second came after all elements in the first, which is
    /// precisely what we want.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    internal sealed class ConcatQueryOperator<TSource> : BinaryQueryOperator<TSource, TSource, TSource>
    {
        private readonly bool _prematureMergeLeft; // Whether to prematurely merge the left data source
        private readonly bool _prematureMergeRight; // Whether to prematurely merge the right data source

        //---------------------------------------------------------------------------------------
        // Initializes a new concatenation operator.
        //
        // Arguments:
        //     child                - the child whose data we will reverse
        //

        internal ConcatQueryOperator(ParallelQuery<TSource> firstChild, ParallelQuery<TSource> secondChild)
            : base(firstChild, secondChild)
        {
            Debug.Assert(firstChild != null, "first child data source cannot be null");
            Debug.Assert(secondChild != null, "second child data source cannot be null");
            _outputOrdered = LeftChild.OutputOrdered || RightChild.OutputOrdered;

            _prematureMergeLeft = LeftChild.OrdinalIndexState.IsWorseThan(OrdinalIndexState.Increasing);
            _prematureMergeRight = RightChild.OrdinalIndexState.IsWorseThan(OrdinalIndexState.Increasing);

            if ((LeftChild.OrdinalIndexState == OrdinalIndexState.Indexable)
                && (RightChild.OrdinalIndexState == OrdinalIndexState.Indexable))
            {
                SetOrdinalIndex(OrdinalIndexState.Indexable);
            }
            else
            {
                SetOrdinalIndex(
                    ExchangeUtilities.Worse(OrdinalIndexState.Increasing,
                        ExchangeUtilities.Worse(LeftChild.OrdinalIndexState, RightChild.OrdinalIndexState)));
            }
        }

        //---------------------------------------------------------------------------------------
        // Just opens the current operator, including opening the child and wrapping it with
        // partitions as needed.
        //

        internal override QueryResults<TSource> Open(QuerySettings settings, bool preferStriping)
        {
            // We just open the children operators.
            QueryResults<TSource> leftChildResults = LeftChild.Open(settings, preferStriping);
            QueryResults<TSource> rightChildResults = RightChild.Open(settings, preferStriping);

            return ConcatQueryOperatorResults.NewResults(leftChildResults, rightChildResults, this, settings, preferStriping);
        }

        public override void WrapPartitionedStream<TLeftKey, TRightKey>(
            PartitionedStream<TSource, TLeftKey> leftStream, PartitionedStream<TSource, TRightKey> rightStream,
            IPartitionedStreamRecipient<TSource> outputRecipient, bool preferStriping, QuerySettings settings)
        {
            // Prematurely merge the left results, if necessary
            if (_prematureMergeLeft)
            {
                ListQueryResults<TSource> leftStreamResults =
                    ExecuteAndCollectResults(leftStream, leftStream.PartitionCount, LeftChild.OutputOrdered, preferStriping, settings);
                PartitionedStream<TSource, int> leftStreamInc = leftStreamResults.GetPartitionedStream();
                WrapHelper<int, TRightKey>(leftStreamInc, rightStream, outputRecipient, settings, preferStriping);
            }
            else
            {
                Debug.Assert(!ExchangeUtilities.IsWorseThan(leftStream.OrdinalIndexState, OrdinalIndexState.Increasing));
                WrapHelper<TLeftKey, TRightKey>(leftStream, rightStream, outputRecipient, settings, preferStriping);
            }
        }

        private void WrapHelper<TLeftKey, TRightKey>(
            PartitionedStream<TSource, TLeftKey> leftStreamInc, PartitionedStream<TSource, TRightKey> rightStream,
            IPartitionedStreamRecipient<TSource> outputRecipient, QuerySettings settings, bool preferStriping)
        {
            // Prematurely merge the right results, if necessary
            if (_prematureMergeRight)
            {
                ListQueryResults<TSource> rightStreamResults =
                    ExecuteAndCollectResults(rightStream, leftStreamInc.PartitionCount, LeftChild.OutputOrdered, preferStriping, settings);
                PartitionedStream<TSource, int> rightStreamInc = rightStreamResults.GetPartitionedStream();
                WrapHelper2<TLeftKey, int>(leftStreamInc, rightStreamInc, outputRecipient);
            }
            else
            {
                Debug.Assert(!ExchangeUtilities.IsWorseThan(rightStream.OrdinalIndexState, OrdinalIndexState.Increasing));
                WrapHelper2<TLeftKey, TRightKey>(leftStreamInc, rightStream, outputRecipient);
            }
        }

        private void WrapHelper2<TLeftKey, TRightKey>(
            PartitionedStream<TSource, TLeftKey> leftStreamInc, PartitionedStream<TSource, TRightKey> rightStreamInc,
            IPartitionedStreamRecipient<TSource> outputRecipient)
        {
            int partitionCount = leftStreamInc.PartitionCount;

            // Generate the shared data.
            IComparer<ConcatKey<TLeftKey, TRightKey>> comparer = ConcatKey<TLeftKey, TRightKey>.MakeComparer(
                leftStreamInc.KeyComparer, rightStreamInc.KeyComparer);
            var outputStream = new PartitionedStream<TSource, ConcatKey<TLeftKey, TRightKey>>(partitionCount, comparer, OrdinalIndexState);

            for (int i = 0; i < partitionCount; i++)
            {
                outputStream[i] = new ConcatQueryOperatorEnumerator<TLeftKey, TRightKey>(leftStreamInc[i], rightStreamInc[i]);
            }

            outputRecipient.Receive(outputStream);
        }


        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //

        internal override IEnumerable<TSource> AsSequentialQuery(CancellationToken token)
        {
            return LeftChild.AsSequentialQuery(token).Concat(RightChild.AsSequentialQuery(token));
        }


        //---------------------------------------------------------------------------------------
        // Whether this operator performs a premature merge that would not be performed in
        // a similar sequential operation (i.e., in LINQ to Objects).
        //

        internal override bool LimitsParallelism
        {
            get { return false; }
        }

        //---------------------------------------------------------------------------------------
        // The enumerator type responsible for concatenating two data sources.
        //

        private sealed class ConcatQueryOperatorEnumerator<TLeftKey, TRightKey> : QueryOperatorEnumerator<TSource, ConcatKey<TLeftKey, TRightKey>>
        {
            private readonly QueryOperatorEnumerator<TSource, TLeftKey> _firstSource; // The first data source to enumerate.
            private readonly QueryOperatorEnumerator<TSource, TRightKey> _secondSource; // The second data source to enumerate.
            private bool _begunSecond; // Whether this partition has begun enumerating the second source yet.

            //---------------------------------------------------------------------------------------
            // Instantiates a new select enumerator.
            //

            internal ConcatQueryOperatorEnumerator(
                QueryOperatorEnumerator<TSource, TLeftKey> firstSource,
                QueryOperatorEnumerator<TSource, TRightKey> secondSource)
            {
                Debug.Assert(firstSource != null);
                Debug.Assert(secondSource != null);

                _firstSource = firstSource;
                _secondSource = secondSource;
            }

            //---------------------------------------------------------------------------------------
            // MoveNext advances to the next element in the output.  While the first data source has
            // elements, this consists of just advancing through it.  After this, all partitions must
            // synchronize at a barrier and publish the maximum index N.  Finally, all partitions can
            // move on to the second data source, adding N+1 to indices in order to get the correct
            // index offset.
            //

            internal override bool MoveNext([MaybeNullWhen(false), AllowNull] ref TSource currentElement, ref ConcatKey<TLeftKey, TRightKey> currentKey)
            {
                Debug.Assert(_firstSource != null);
                Debug.Assert(_secondSource != null);

                // If we are still enumerating the first source, fetch the next item.
                if (!_begunSecond)
                {
                    // If elements remain, just return true and continue enumerating the left.
                    TLeftKey leftKey = default(TLeftKey)!;
                    if (_firstSource.MoveNext(ref currentElement!, ref leftKey))
                    {
                        currentKey = ConcatKey<TLeftKey, TRightKey>.MakeLeft(leftKey);
                        return true;
                    }
                    _begunSecond = true;
                }

                // Now either move on to, or continue, enumerating the right data source.
                TRightKey rightKey = default(TRightKey)!;
                if (_secondSource.MoveNext(ref currentElement!, ref rightKey))
                {
                    currentKey = ConcatKey<TLeftKey, TRightKey>.MakeRight(rightKey);
                    return true;
                }

                return false;
            }

            protected override void Dispose(bool disposing)
            {
                _firstSource.Dispose();
                _secondSource.Dispose();
            }
        }


        //-----------------------------------------------------------------------------------
        // Query results for a Concat operator. The results are indexable if the child
        // results were indexable.
        //

        private sealed class ConcatQueryOperatorResults : BinaryQueryOperatorResults
        {
            private readonly int _leftChildCount; // The number of elements in the left child result set
            private readonly int _rightChildCount; // The number of elements in the right child result set

            public static QueryResults<TSource> NewResults(
                QueryResults<TSource> leftChildQueryResults, QueryResults<TSource> rightChildQueryResults,
                ConcatQueryOperator<TSource> op, QuerySettings settings,
                bool preferStriping)
            {
                if (leftChildQueryResults.IsIndexible && rightChildQueryResults.IsIndexible)
                {
                    return new ConcatQueryOperatorResults(
                        leftChildQueryResults, rightChildQueryResults, op, settings, preferStriping);
                }
                else
                {
                    return new BinaryQueryOperatorResults(
                        leftChildQueryResults, rightChildQueryResults, op, settings, preferStriping);
                }
            }

            private ConcatQueryOperatorResults(
                QueryResults<TSource> leftChildQueryResults, QueryResults<TSource> rightChildQueryResults,
                ConcatQueryOperator<TSource> concatOp, QuerySettings settings,
                bool preferStriping)
                : base(leftChildQueryResults, rightChildQueryResults, concatOp, settings, preferStriping)
            {
                Debug.Assert(leftChildQueryResults.IsIndexible && rightChildQueryResults.IsIndexible);

                _leftChildCount = leftChildQueryResults.ElementsCount;
                _rightChildCount = rightChildQueryResults.ElementsCount;
            }

            internal override bool IsIndexible
            {
                get { return true; }
            }

            internal override int ElementsCount
            {
                get
                {
                    Debug.Assert(_leftChildCount >= 0 && _rightChildCount >= 0);
                    return _leftChildCount + _rightChildCount;
                }
            }

            internal override TSource GetElement(int index)
            {
                if (index < _leftChildCount)
                {
                    return _leftChildQueryResults.GetElement(index);
                }
                else
                {
                    return _rightChildQueryResults.GetElement(index - _leftChildCount);
                }
            }
        }
    }

    //---------------------------------------------------------------------------------------
    // ConcatKey represents an ordering key for the Concat operator. It knows whether the
    // element it is associated with is from the left source or the right source, and also
    // the elements ordering key.
    //

    internal struct ConcatKey<TLeftKey, TRightKey>
    {
        private readonly TLeftKey? _leftKey;
        private readonly TRightKey? _rightKey;
        private readonly bool _isLeft;

        private ConcatKey([AllowNull] TLeftKey leftKey, [AllowNull] TRightKey rightKey, bool isLeft)
        {
            _leftKey = leftKey;
            _rightKey = rightKey;
            _isLeft = isLeft;
        }

        internal static ConcatKey<TLeftKey, TRightKey> MakeLeft([AllowNull] TLeftKey leftKey)
        {
            return new ConcatKey<TLeftKey, TRightKey>(leftKey, default, isLeft: true);
        }

        internal static ConcatKey<TLeftKey, TRightKey> MakeRight([AllowNull] TRightKey rightKey)
        {
            return new ConcatKey<TLeftKey, TRightKey>(default, rightKey, isLeft: false);
        }

        internal static IComparer<ConcatKey<TLeftKey, TRightKey>> MakeComparer(
            IComparer<TLeftKey> leftComparer, IComparer<TRightKey> rightComparer)
        {
            return new ConcatKeyComparer(leftComparer, rightComparer);
        }

        //---------------------------------------------------------------------------------------
        // ConcatKeyComparer compares ConcatKeys, so that elements from the left source come
        // before elements from the right source, and elements within each source are ordered
        // according to the corresponding order key.
        //

        private sealed class ConcatKeyComparer : IComparer<ConcatKey<TLeftKey, TRightKey>>
        {
            private readonly IComparer<TLeftKey> _leftComparer;
            private readonly IComparer<TRightKey> _rightComparer;

            internal ConcatKeyComparer(IComparer<TLeftKey> leftComparer, IComparer<TRightKey> rightComparer)
            {
                _leftComparer = leftComparer;
                _rightComparer = rightComparer;
            }

            public int Compare(ConcatKey<TLeftKey, TRightKey> x, ConcatKey<TLeftKey, TRightKey> y)
            {
                // If one element is from the left source and the other not, the element from the left source
                // comes earlier.
                if (x._isLeft != y._isLeft)
                {
                    return x._isLeft ? -1 : 1;
                }

                // Elements are from the same source (left or right). Compare the corresponding keys.
                if (x._isLeft)
                {
                    return _leftComparer.Compare(x._leftKey, y._leftKey);
                }
                return _rightComparer.Compare(x._rightKey, y._rightKey);
            }
        }
    }
}
