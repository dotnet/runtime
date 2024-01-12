// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// TakeOrSkipWhileQueryOperator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// Take- and SkipWhile work similarly. Execution is broken into two phases: Search
    /// and Yield.
    ///
    /// During the Search phase, many partitions at once search for the first occurrence
    /// of a false element.  As they search, any time a partition finds a false element
    /// whose index is lesser than the current lowest-known false element, the new index
    /// will be published, so other partitions can stop the search.  The search stops
    /// as soon as (1) a partition exhausts its input, (2) the predicate yields false for
    /// one of the partition's elements, or (3) its input index passes the current lowest-
    /// known index (sufficient since a given partition's indices are always strictly
    /// incrementing -- asserted below).  Elements are buffered during this process.
    ///
    /// Partitions use a barrier after Search and before moving on to Yield.  Once all
    /// have passed the barrier, Yielding begins.  At this point, the lowest-known false
    /// index will be accurate for the entire set, since all partitions have finished
    /// scanning.  This is where TakeWhile and SkipWhile differ.  TakeWhile will start at
    /// the beginning of its buffer and yield all elements whose indices are less than
    /// the lowest-known false index.  SkipWhile, on the other hand, will skip any such
    /// elements in the buffer, yielding those whose index is greater than or equal to
    /// the lowest-known false index, and then finish yielding any remaining elements in
    /// its data source (since it may have stopped prematurely due to (3) above).
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    internal sealed class TakeOrSkipWhileQueryOperator<TResult> : UnaryQueryOperator<TResult, TResult>
    {
        // Predicate function used to decide when to stop yielding elements. One pair is used for
        // index-based evaluation (i.e. it is passed the index as well as the element's value).
        private readonly Func<TResult, bool>? _predicate;
        private readonly Func<TResult, int, bool>? _indexedPredicate;

        private readonly bool _take; // Whether to take (true) or skip (false).
        private bool _prematureMerge; // Whether to prematurely merge the input of this operator.
        private bool _limitsParallelism; // The precomputed value of LimitsParallelism

        //---------------------------------------------------------------------------------------
        // Initializes a new take-while operator.
        //
        // Arguments:
        //     child                - the child data source to enumerate
        //     predicate            - the predicate function (if expression tree isn't provided)
        //     indexedPredicate     - the index-based predicate function (if expression tree isn't provided)
        //     take                 - whether this is a TakeWhile (true) or SkipWhile (false)
        //
        // Notes:
        //     Only one kind of predicate can be specified, an index-based one or not.  If an
        //     expression tree is provided, the delegate cannot also be provided.
        //

        internal TakeOrSkipWhileQueryOperator(IEnumerable<TResult> child,
                                              Func<TResult, bool>? predicate,
                                              Func<TResult, int, bool>? indexedPredicate, bool take)
            : base(child)
        {
            Debug.Assert(child != null, "child data source cannot be null");
            Debug.Assert(predicate != null || indexedPredicate != null, "need a predicate function");

            _predicate = predicate;
            _indexedPredicate = indexedPredicate;
            _take = take;

            InitOrderIndexState();
        }

        /// <summary>
        /// Determines the order index state for the output operator
        /// </summary>
        private void InitOrderIndexState()
        {
            // SkipWhile/TakeWhile needs an increasing index. However, if the predicate expression depends on the index,
            // the index needs to be correct, not just increasing.

            OrdinalIndexState requiredIndexState = OrdinalIndexState.Increasing;
            OrdinalIndexState childIndexState = Child.OrdinalIndexState;
            if (_indexedPredicate != null)
            {
                requiredIndexState = OrdinalIndexState.Correct;
                _limitsParallelism = childIndexState == OrdinalIndexState.Increasing;
            }

            OrdinalIndexState indexState = ExchangeUtilities.Worse(childIndexState, OrdinalIndexState.Correct);
            if (indexState.IsWorseThan(requiredIndexState))
            {
                _prematureMerge = true;
            }

            if (!_take)
            {
                // If the index was correct, now it is only increasing.
                indexState = indexState.Worse(OrdinalIndexState.Increasing);
            }

            SetOrdinalIndexState(indexState);
        }

        internal override void WrapPartitionedStream<TKey>(
            PartitionedStream<TResult, TKey> inputStream, IPartitionedStreamRecipient<TResult> recipient, bool preferStriping, QuerySettings settings)
        {
            if (_prematureMerge)
            {
                ListQueryResults<TResult> results = ExecuteAndCollectResults(inputStream, inputStream.PartitionCount, Child.OutputOrdered, preferStriping, settings);
                PartitionedStream<TResult, int> listInputStream = results.GetPartitionedStream();
                WrapHelper<int>(listInputStream, recipient, settings);
            }
            else
            {
                WrapHelper<TKey>(inputStream, recipient, settings);
            }
        }

        private void WrapHelper<TKey>(PartitionedStream<TResult, TKey> inputStream, IPartitionedStreamRecipient<TResult> recipient, QuerySettings settings)
        {
            int partitionCount = inputStream.PartitionCount;
            if (ParallelEnumerable.SinglePartitionMode)
                Debug.Assert(partitionCount == 1);

            // Create shared data.
            OperatorState<TKey> operatorState = new OperatorState<TKey>();
            CountdownEvent sharedBarrier = new CountdownEvent(partitionCount);

            Debug.Assert(_indexedPredicate == null || typeof(TKey) == typeof(int));
            Func<TResult, TKey, bool>? convertedIndexedPredicate = (Func<TResult, TKey, bool>?)(object?)_indexedPredicate;

            PartitionedStream<TResult, TKey> partitionedStream =
                new PartitionedStream<TResult, TKey>(partitionCount, inputStream.KeyComparer, OrdinalIndexState);
            for (int i = 0; i < partitionCount; i++)
            {
                partitionedStream[i] = new TakeOrSkipWhileQueryOperatorEnumerator<TKey>(
                    inputStream[i], _predicate, convertedIndexedPredicate, _take, operatorState, sharedBarrier,
                    settings.CancellationState.MergedCancellationToken, inputStream.KeyComparer);
            }

            recipient.Receive(partitionedStream);
        }

        //---------------------------------------------------------------------------------------
        // Just opens the current operator, including opening the child and wrapping it with
        // partitions as needed.
        //

        internal override QueryResults<TResult> Open(QuerySettings settings, bool preferStriping)
        {
            QueryResults<TResult> childQueryResults = Child.Open(settings, true);
            return new UnaryQueryOperatorResults(childQueryResults, this, settings, preferStriping);
        }

        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //

        internal override IEnumerable<TResult> AsSequentialQuery(CancellationToken token)
        {
            if (_take)
            {
                if (_indexedPredicate != null)
                {
                    return Child.AsSequentialQuery(token).TakeWhile(_indexedPredicate);
                }

                Debug.Assert(_predicate != null);
                return Child.AsSequentialQuery(token).TakeWhile(_predicate);
            }

            if (_indexedPredicate != null)
            {
                IEnumerable<TResult> wrappedIndexedChild = CancellableEnumerable.Wrap(Child.AsSequentialQuery(token), token);
                return wrappedIndexedChild.SkipWhile(_indexedPredicate);
            }

            Debug.Assert(_predicate != null);
            IEnumerable<TResult> wrappedChild = CancellableEnumerable.Wrap(Child.AsSequentialQuery(token), token);
            return wrappedChild.SkipWhile(_predicate);
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
        // The enumerator type responsible for executing the take- or skip-while.
        //

        private sealed class TakeOrSkipWhileQueryOperatorEnumerator<TKey> : QueryOperatorEnumerator<TResult, TKey>
        {
            private readonly QueryOperatorEnumerator<TResult, TKey> _source; // The data source to enumerate.
            private readonly Func<TResult, bool>? _predicate;  // The actual predicate function.
            private readonly Func<TResult, TKey, bool>? _indexedPredicate;  // The actual index-based predicate function.
            private readonly bool _take; // Whether to execute a take- (true) or skip-while (false).
            private readonly IComparer<TKey> _keyComparer; // Comparer for the order keys.

            // These fields are all shared among partitions.
            private readonly OperatorState<TKey> _operatorState; // The lowest false found by any partition.
            private readonly CountdownEvent _sharedBarrier; // To separate the search/yield phases.
            private readonly CancellationToken _cancellationToken; // Token used to cancel this operator.

            private List<Pair<TResult, TKey>>? _buffer; // Our buffer.
            private Shared<int>? _bufferIndex; // Our current index within the buffer.  [allocate in moveNext to avoid false-sharing]
            private int _updatesSeen; // How many updates has this enumerator observed? (Each other enumerator will contribute one update.)
            private TKey _currentLowKey = default!; // The lowest key rejected by one of the other enumerators.


            //---------------------------------------------------------------------------------------
            // Instantiates a new select enumerator.
            //

            internal TakeOrSkipWhileQueryOperatorEnumerator(
                QueryOperatorEnumerator<TResult, TKey> source, Func<TResult, bool>? predicate, Func<TResult, TKey, bool>? indexedPredicate, bool take,
                OperatorState<TKey> operatorState, CountdownEvent sharedBarrier, CancellationToken cancelToken, IComparer<TKey> keyComparer)
            {
                Debug.Assert(source != null);
                Debug.Assert(predicate != null || indexedPredicate != null);
                Debug.Assert(operatorState != null);
                Debug.Assert(sharedBarrier != null);
                Debug.Assert(keyComparer != null);

                _source = source;
                _predicate = predicate;
                _indexedPredicate = indexedPredicate;
                _take = take;
                _operatorState = operatorState;
                _sharedBarrier = sharedBarrier;
                _cancellationToken = cancelToken;
                _keyComparer = keyComparer;
            }

            //---------------------------------------------------------------------------------------
            // Straightforward IEnumerator<T> methods.
            //

            internal override bool MoveNext([MaybeNullWhen(false), AllowNull] ref TResult currentElement, [AllowNull] ref TKey currentKey)
            {
                // If the buffer has not been created, we will generate it lazily on demand.
                if (_buffer == null)
                {
                    // Create a buffer, but don't publish it yet (in case of exception).
                    List<Pair<TResult, TKey>> buffer = new List<Pair<TResult, TKey>>();

                    // Enter the search phase.  In this phase, we scan the input until one of three
                    // things happens:  (1) all input has been exhausted, (2) the predicate yields
                    // false for one of our elements, or (3) we move past the current lowest index
                    // found by other partitions for a false element.  As we go, we have to remember
                    // the elements by placing them into the buffer.

                    try
                    {
                        TResult current = default(TResult)!;
                        TKey key = default(TKey)!;
                        int i = 0; //counter to help with cancellation
                        while (_source.MoveNext(ref current!, ref key))
                        {
                            if ((i++ & CancellationState.POLL_INTERVAL) == 0)
                                _cancellationToken.ThrowIfCancellationRequested();

                            // Add the current element to our buffer.
                            buffer.Add(new Pair<TResult, TKey>(current, key));

                            // See if another partition has found a false value before this element. If so,
                            // we should stop scanning the input now and reach the barrier ASAP.
                            if (_updatesSeen != _operatorState._updatesDone)
                            {
                                lock (_operatorState)
                                {
                                    _currentLowKey = _operatorState._currentLowKey;
                                    _updatesSeen = _operatorState._updatesDone;
                                }
                            }

                            if (_updatesSeen > 0 && _keyComparer.Compare(key, _currentLowKey) > 0)
                            {
                                break;
                            }

                            // Evaluate the predicate, either indexed or not based on info passed to the ctor.
                            bool predicateResult;
                            if (_predicate != null)
                            {
                                predicateResult = _predicate(current);
                            }
                            else
                            {
                                Debug.Assert(_indexedPredicate != null);
                                predicateResult = _indexedPredicate(current, key);
                            }

                            if (!predicateResult)
                            {
                                // Signal that we've found a false element, racing with other partitions to
                                // set the shared index value.
                                lock (_operatorState)
                                {
                                    if (_operatorState._updatesDone == 0 || _keyComparer.Compare(_operatorState._currentLowKey, key) > 0)
                                    {
                                        _currentLowKey = _operatorState._currentLowKey = key;
                                        _updatesSeen = ++_operatorState._updatesDone;
                                    }
                                }

                                break;
                            }
                        }
                    }
                    finally
                    {
                        if (!ParallelEnumerable.SinglePartitionMode) {
                            // No matter whether we exit due to an exception or normal completion, we must ensure
                            // that we signal other partitions that we have completed.  Otherwise, we can cause deadlocks.
                            _sharedBarrier.Signal();
                        }
                    }

                    // Before exiting the search phase, we will synchronize with others. This is a barrier.
                    if (!ParallelEnumerable.SinglePartitionMode)
                        _sharedBarrier.Wait(_cancellationToken);

                    // Publish the buffer and set the index to just before the 1st element.
                    _buffer = buffer;
                    _bufferIndex = new Shared<int>(-1);
                }

                Debug.Assert(_bufferIndex != null);
                // Now either enter (or continue) the yielding phase. As soon as we reach this, we know the
                // current shared "low false" value is the absolute lowest with a false.
                if (_take)
                {
                    // In the case of a take-while, we will yield each element from our buffer for which
                    // the element is lesser than the lowest false index found.
                    if (_bufferIndex.Value >= _buffer.Count - 1)
                    {
                        return false;
                    }

                    // Increment the index, and remember the values.
                    ++_bufferIndex.Value;
                    currentElement = _buffer[_bufferIndex.Value].First;
                    currentKey = _buffer[_bufferIndex.Value].Second;

                    return _operatorState._updatesDone == 0 || _keyComparer.Compare(_operatorState._currentLowKey, currentKey) > 0;
                }
                else
                {
                    // If no false was found, the output is empty.
                    if (_operatorState._updatesDone == 0)
                    {
                        return false;
                    }

                    // In the case of a skip-while, we must skip over elements whose index is lesser than the
                    // lowest index found. Once we've exhausted the buffer, we must go back and continue
                    // enumerating the data source until it is empty.
                    if (_bufferIndex.Value < _buffer.Count - 1)
                    {
                        for (_bufferIndex.Value++; _bufferIndex.Value < _buffer.Count; _bufferIndex.Value++)
                        {
                            // If the current buffered element's index is greater than or equal to the smallest
                            // false index found, we will yield it as a result.
                            if (_keyComparer.Compare(_buffer[_bufferIndex.Value].Second, _operatorState._currentLowKey) >= 0)
                            {
                                currentElement = _buffer[_bufferIndex.Value].First;
                                currentKey = _buffer[_bufferIndex.Value].Second;
                                return true;
                            }
                        }
                    }

                    // Lastly, so long as our input still has elements, they will be yieldable.
                    if (_source.MoveNext(ref currentElement!, ref currentKey))
                    {
                        Debug.Assert(_keyComparer.Compare(currentKey, _operatorState._currentLowKey) > 0,
                                        "expected remaining element indices to be greater than smallest");
                        return true;
                    }
                }

                return false;
            }

            protected override void Dispose(bool disposing)
            {
                _source.Dispose();
            }
        }

        private sealed class OperatorState<TKey>
        {
            internal volatile int _updatesDone;
            internal TKey _currentLowKey = default!;
        }
    }
}
