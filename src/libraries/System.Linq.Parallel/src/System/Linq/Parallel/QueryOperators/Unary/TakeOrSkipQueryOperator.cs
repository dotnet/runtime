// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// TakeOrSkipQueryOperator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace System.Linq.Parallel
{
    /// <summary>
    /// Take and Skip either take or skip a specified number of elements, captured in the
    /// count argument.  These will work a little bit like TakeWhile and SkipWhile: there
    /// are two phases, (1) Search and (2) Yield.  In the search phase, our goal is to
    /// find the 'count'th index from the input.  We do this in parallel by sharing a count-
    /// sized array.  Each thread races to populate the array with indices in ascending
    /// order.  This requires synchronization for inserts.  We use a simple heap, for decent
    /// worst case performance.  After a thread has scanned ‘count’ elements, or its current
    /// index is greater than or equal to the maximum index in the array (and the array is
    /// fully populated), the thread can stop searching.  All threads issue a barrier before
    /// moving to the Yield phase.  When the Yield phase is entered, the count-1th element
    /// of the array contains: in the case of Take, the maximum index (exclusive) to be
    /// returned; or in the case of Skip, the minimum index (inclusive) to be returned.  The
    /// Yield phase simply consists of yielding these elements as output.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    internal sealed class TakeOrSkipQueryOperator<TResult> : UnaryQueryOperator<TResult, TResult>
    {
        private readonly int _count; // The number of elements to take or skip.
        private readonly bool _take; // Whether to take (true) or skip (false).
        private bool _prematureMerge = false; // Whether to prematurely merge the input of this operator.

        //---------------------------------------------------------------------------------------
        // Initializes a new take-while operator.
        //
        // Arguments:
        //     child  - the child data source to enumerate
        //     count  - the number of elements to take or skip
        //     take   - whether this is a Take (true) or Skip (false)
        //

        internal TakeOrSkipQueryOperator(IEnumerable<TResult> child, int count, bool take)
            : base(child)
        {
            Debug.Assert(child != null, "child data source cannot be null");

            _count = count;
            _take = take;

            SetOrdinalIndexState(OutputOrdinalIndexState());
        }

        /// <summary>
        /// Determines the order index state for the output operator
        /// </summary>
        private OrdinalIndexState OutputOrdinalIndexState()
        {
            OrdinalIndexState indexState = Child.OrdinalIndexState;

            if (indexState == OrdinalIndexState.Indexable)
            {
                return OrdinalIndexState.Indexable;
            }

            if (indexState.IsWorseThan(OrdinalIndexState.Increasing))
            {
                _prematureMerge = true;
                indexState = OrdinalIndexState.Correct;
            }

            // If the operator is skip and the index was correct, now it is only increasing.
            if (!_take && indexState == OrdinalIndexState.Correct)
            {
                indexState = OrdinalIndexState.Increasing;
            }

            return indexState;
        }

        internal override void WrapPartitionedStream<TKey>(
            PartitionedStream<TResult, TKey> inputStream, IPartitionedStreamRecipient<TResult> recipient, bool preferStriping, QuerySettings settings)
        {
            Debug.Assert(Child.OrdinalIndexState != OrdinalIndexState.Indexable, "Don't take this code path if the child is indexable.");

            // If the index is not at least increasing, we need to reindex.
            if (_prematureMerge)
            {
                ListQueryResults<TResult> results = ExecuteAndCollectResults(
                    inputStream, inputStream.PartitionCount, Child.OutputOrdered, preferStriping, settings);
                PartitionedStream<TResult, int> inputIntStream = results.GetPartitionedStream();
                WrapHelper<int>(inputIntStream, recipient, settings);
            }
            else
            {
                WrapHelper<TKey>(inputStream, recipient, settings);
            }
        }

        private void WrapHelper<TKey>(PartitionedStream<TResult, TKey> inputStream, IPartitionedStreamRecipient<TResult> recipient, QuerySettings settings)
        {
            int partitionCount = inputStream.PartitionCount;
            FixedMaxHeap<TKey> sharedIndices = new FixedMaxHeap<TKey>(_count, inputStream.KeyComparer); // an array used to track the sequence of indices leading up to the Nth index
            CountdownEvent sharedBarrier = new CountdownEvent(partitionCount); // a barrier to synchronize before yielding

            PartitionedStream<TResult, TKey> outputStream =
                new PartitionedStream<TResult, TKey>(partitionCount, inputStream.KeyComparer, OrdinalIndexState);
            for (int i = 0; i < partitionCount; i++)
            {
                outputStream[i] = new TakeOrSkipQueryOperatorEnumerator<TKey>(
                    inputStream[i], _take, sharedIndices, sharedBarrier,
                    settings.CancellationState.MergedCancellationToken, inputStream.KeyComparer);
            }

            recipient.Receive(outputStream);
        }

        //---------------------------------------------------------------------------------------
        // Just opens the current operator, including opening the child and wrapping it with
        // partitions as needed.
        //

        internal override QueryResults<TResult> Open(QuerySettings settings, bool preferStriping)
        {
            QueryResults<TResult> childQueryResults = Child.Open(settings, true);
            return TakeOrSkipQueryOperatorResults.NewResults(childQueryResults, this, settings, preferStriping);
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
        // The enumerator type responsible for executing the Take or Skip.
        //

        class TakeOrSkipQueryOperatorEnumerator<TKey> : QueryOperatorEnumerator<TResult, TKey>
        {
            private readonly QueryOperatorEnumerator<TResult, TKey> _source; // The data source to enumerate.
            private readonly int _count; // The number of elements to take or skip.
            private readonly bool _take; // Whether to execute a Take (true) or Skip (false).
            private readonly IComparer<TKey> _keyComparer; // Comparer for the order keys.

            // These fields are all shared among partitions.
            private readonly FixedMaxHeap<TKey> _sharedIndices; // The indices shared among partitions.
            private readonly CountdownEvent _sharedBarrier; // To separate the search/yield phases.
            private readonly CancellationToken _cancellationToken; // Indicates that cancellation has occurred.

            private List<Pair<TResult, TKey>> _buffer; // Our buffer.
            private Shared<int> _bufferIndex; // Our current index within the buffer. [allocate in moveNext to avoid false-sharing]

            //---------------------------------------------------------------------------------------
            // Instantiates a new select enumerator.
            //

            internal TakeOrSkipQueryOperatorEnumerator(
                QueryOperatorEnumerator<TResult, TKey> source, bool take,
                FixedMaxHeap<TKey> sharedIndices, CountdownEvent sharedBarrier, CancellationToken cancellationToken,
                IComparer<TKey> keyComparer)
            {
                Debug.Assert(source != null);
                Debug.Assert(sharedIndices != null);
                Debug.Assert(sharedBarrier != null);
                Debug.Assert(keyComparer != null);

                _source = source;
                _count = sharedIndices.Size;
                _take = take;
                _sharedIndices = sharedIndices;
                _sharedBarrier = sharedBarrier;
                _cancellationToken = cancellationToken;
                _keyComparer = keyComparer;
            }

            //---------------------------------------------------------------------------------------
            // Straightforward IEnumerator<T> methods.
            //

            internal override bool MoveNext(ref TResult currentElement, ref TKey currentKey)
            {
                Debug.Assert(_sharedIndices != null);

                // If the buffer has not been created, we will populate it lazily on demand.
                if (_buffer == null && _count > 0)
                {
                    // Create a buffer, but don't publish it yet (in case of exception).
                    List<Pair<TResult, TKey>> buffer = new List<Pair<TResult, TKey>>();

                    // Enter the search phase. In this phase, all partitions race to populate
                    // the shared indices with their first 'count' contiguous elements.
                    TResult current = default(TResult);
                    TKey index = default(TKey);
                    int i = 0; //counter to help with cancellation
                    while (buffer.Count < _count && _source.MoveNext(ref current, ref index))
                    {
                        if ((i++ & CancellationState.POLL_INTERVAL) == 0)
                            CancellationState.ThrowIfCanceled(_cancellationToken);

                        // Add the current element to our buffer.
                        buffer.Add(new Pair<TResult, TKey>(current, index));

                        // Now we will try to insert our index into the shared indices list, quitting if
                        // our index is greater than all of the indices already inside it.
                        lock (_sharedIndices)
                        {
                            if (!_sharedIndices.Insert(index))
                            {
                                // We have read past the maximum index. We can move to the barrier now.
                                break;
                            }
                        }
                    }

                    // Before exiting the search phase, we will synchronize with others. This is a barrier.
                    _sharedBarrier.Signal();
                    _sharedBarrier.Wait(_cancellationToken);

                    // Publish the buffer and set the index to just before the 1st element.
                    _buffer = buffer;
                    _bufferIndex = new Shared<int>(-1);
                }

                // Now either enter (or continue) the yielding phase. As soon as we reach this, we know the
                // index of the 'count'-th input element.
                if (_take)
                {
                    // In the case of a Take, we will yield each element from our buffer for which
                    // the element is lesser than the 'count'-th index found.
                    if (_count == 0 || _bufferIndex.Value >= _buffer.Count - 1)
                    {
                        return false;
                    }

                    // Increment the index, and remember the values.
                    ++_bufferIndex.Value;
                    currentElement = _buffer[_bufferIndex.Value].First;
                    currentKey = _buffer[_bufferIndex.Value].Second;

                    // Only yield the element if its index is less than or equal to the max index.
                    return _sharedIndices.Count == 0
                        || _keyComparer.Compare(_buffer[_bufferIndex.Value].Second, _sharedIndices.MaxValue) <= 0;
                }
                else
                {
                    TKey minKey = default(TKey);

                    // If the count to skip was greater than 0, look at the buffer.
                    if (_count > 0)
                    {
                        // If there wasn't enough input to skip, return right away.
                        if (_sharedIndices.Count < _count)
                        {
                            return false;
                        }

                        minKey = _sharedIndices.MaxValue;

                        // In the case of a skip, we must skip over elements whose index is lesser than the
                        // 'count'-th index found. Once we've exhausted the buffer, we must go back and continue
                        // enumerating the data source until it is empty.
                        if (_bufferIndex.Value < _buffer.Count - 1)
                        {
                            for (_bufferIndex.Value++; _bufferIndex.Value < _buffer.Count; _bufferIndex.Value++)
                            {
                                // If the current buffered element's index is greater than the 'count'-th index,
                                // we will yield it as a result.
                                if (_keyComparer.Compare(_buffer[_bufferIndex.Value].Second, minKey) > 0)
                                {
                                    currentElement = _buffer[_bufferIndex.Value].First;
                                    currentKey = _buffer[_bufferIndex.Value].Second;
                                    return true;
                                }
                            }
                        }
                    }

                    // Lastly, so long as our input still has elements, they will be yieldable.
                    if (_source.MoveNext(ref currentElement, ref currentKey))
                    {
                        Debug.Assert(_count <= 0 || _keyComparer.Compare(currentKey, minKey) > 0,
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

        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //

        internal override IEnumerable<TResult> AsSequentialQuery(CancellationToken token)
        {
            if (_take)
            {
                return Child.AsSequentialQuery(token).Take(_count);
            }

            IEnumerable<TResult> wrappedChild = CancellableEnumerable.Wrap(Child.AsSequentialQuery(token), token);
            return wrappedChild.Skip(_count);
        }

        //-----------------------------------------------------------------------------------
        // Query results for a Take or a Skip operator. The results are indexable if the child
        // results were indexable.
        //

        class TakeOrSkipQueryOperatorResults : UnaryQueryOperatorResults
        {
            private TakeOrSkipQueryOperator<TResult> _takeOrSkipOp; // The operator that generated the results
            private int _childCount; // The number of elements in child results

            public static QueryResults<TResult> NewResults(
                QueryResults<TResult> childQueryResults, TakeOrSkipQueryOperator<TResult> op,
                QuerySettings settings, bool preferStriping)
            {
                if (childQueryResults.IsIndexible)
                {
                    return new TakeOrSkipQueryOperatorResults(
                        childQueryResults, op, settings, preferStriping);
                }
                else
                {
                    return new UnaryQueryOperatorResults(
                        childQueryResults, op, settings, preferStriping);
                }
            }

            private TakeOrSkipQueryOperatorResults(
                QueryResults<TResult> childQueryResults, TakeOrSkipQueryOperator<TResult> takeOrSkipOp,
                QuerySettings settings, bool preferStriping)
                : base(childQueryResults, takeOrSkipOp, settings, preferStriping)
            {
                _takeOrSkipOp = takeOrSkipOp;
                Debug.Assert(_childQueryResults.IsIndexible);

                _childCount = _childQueryResults.ElementsCount;
            }

            internal override bool IsIndexible
            {
                get { return _childCount >= 0; }
            }

            internal override int ElementsCount
            {
                get
                {
                    Debug.Assert(_childCount >= 0);
                    if (_takeOrSkipOp._take)
                    {
                        return Math.Min(_childCount, _takeOrSkipOp._count);
                    }
                    else
                    {
                        return Math.Max(_childCount - _takeOrSkipOp._count, 0);
                    }
                }
            }

            internal override TResult GetElement(int index)
            {
                Debug.Assert(_childCount >= 0);
                Debug.Assert(index >= 0);
                Debug.Assert(index < ElementsCount);

                if (_takeOrSkipOp._take)
                {
                    return _childQueryResults.GetElement(index);
                }
                else
                {
                    return _childQueryResults.GetElement(_takeOrSkipOp._count + index);
                }
            }
        }
    }
}
