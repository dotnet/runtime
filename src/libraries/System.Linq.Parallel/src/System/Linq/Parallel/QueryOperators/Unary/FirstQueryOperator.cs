// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// FirstQueryOperator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// First tries to discover the first element in the source, optionally matching a
    /// predicate.  All partitions search in parallel, publish the lowest index for a
    /// candidate match, and reach a barrier.  Only the partition that "wins" the race,
    /// i.e. who found the candidate with the smallest index, will yield an element.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    internal sealed class FirstQueryOperator<TSource> : UnaryQueryOperator<TSource, TSource>
    {
        private readonly Func<TSource, bool>? _predicate; // The optional predicate used during the search.
        private readonly bool _prematureMergeNeeded; // Whether to prematurely merge the input of this operator.

        //---------------------------------------------------------------------------------------
        // Initializes a new first operator.
        //
        // Arguments:
        //     child                - the child whose data we will reverse
        //

        internal FirstQueryOperator(IEnumerable<TSource> child, Func<TSource, bool>? predicate)
            : base(child)
        {
            Debug.Assert(child != null, "child data source cannot be null");
            _predicate = predicate;
            _prematureMergeNeeded = Child.OrdinalIndexState.IsWorseThan(OrdinalIndexState.Increasing);
        }

        //---------------------------------------------------------------------------------------
        // Just opens the current operator, including opening the child and wrapping it with
        // partitions as needed.
        //

        internal override QueryResults<TSource> Open(QuerySettings settings, bool preferStriping)
        {
            // We just open the child operator.
            QueryResults<TSource> childQueryResults = Child.Open(settings, false);
            return new UnaryQueryOperatorResults(childQueryResults, this, settings, preferStriping);
        }

        internal override void WrapPartitionedStream<TKey>(
            PartitionedStream<TSource, TKey> inputStream, IPartitionedStreamRecipient<TSource> recipient, bool preferStriping, QuerySettings settings)
        {
            // If the index is not at least increasing, we need to reindex.
            if (_prematureMergeNeeded)
            {
                ListQueryResults<TSource> listResults = ExecuteAndCollectResults(inputStream, inputStream.PartitionCount, Child.OutputOrdered, preferStriping, settings);
                WrapHelper<int>(listResults.GetPartitionedStream(), recipient, settings);
            }
            else
            {
                WrapHelper<TKey>(inputStream, recipient, settings);
            }
        }

        private void WrapHelper<TKey>(
            PartitionedStream<TSource, TKey> inputStream, IPartitionedStreamRecipient<TSource> recipient, QuerySettings settings)
        {
            int partitionCount = inputStream.PartitionCount;

            // Generate the shared data.
            FirstQueryOperatorState<TKey> operatorState = new FirstQueryOperatorState<TKey>();
            CountdownEvent sharedBarrier = new CountdownEvent(partitionCount);

            PartitionedStream<TSource, int> outputStream = new PartitionedStream<TSource, int>(
                partitionCount, Util.GetDefaultComparer<int>(), OrdinalIndexState.Shuffled);

            for (int i = 0; i < partitionCount; i++)
            {
                outputStream[i] = new FirstQueryOperatorEnumerator<TKey>(
                    inputStream[i], _predicate, operatorState, sharedBarrier,
                    settings.CancellationState.MergedCancellationToken, inputStream.KeyComparer, i);
            }

            recipient.Receive(outputStream);
        }


        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //

        [ExcludeFromCodeCoverage(Justification = "This method should never be called as fallback to sequential is handled in ParallelEnumerable.First()")]
        internal override IEnumerable<TSource> AsSequentialQuery(CancellationToken token)
        {
            Debug.Fail("This method should never be called as fallback to sequential is handled in ParallelEnumerable.First().");
            throw new NotSupportedException();
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
        // The enumerator type responsible for executing the first operation.
        //

        private sealed class FirstQueryOperatorEnumerator<TKey> : QueryOperatorEnumerator<TSource, int>
        {
            private readonly QueryOperatorEnumerator<TSource, TKey> _source; // The data source to enumerate.
            private readonly Func<TSource, bool>? _predicate; // The optional predicate used during the search.
            private bool _alreadySearched; // Set once the enumerator has performed the search.
            private readonly int _partitionId; // ID of this partition

            // Data shared among partitions.
            private readonly FirstQueryOperatorState<TKey> _operatorState; // The current first candidate and its partition index.
            private readonly CountdownEvent _sharedBarrier; // Shared barrier, signaled when partitions find their 1st element.
            private readonly CancellationToken _cancellationToken; // Token used to cancel this operator.
            private readonly IComparer<TKey> _keyComparer; // Comparer for the order keys

            //---------------------------------------------------------------------------------------
            // Instantiates a new enumerator.
            //

            internal FirstQueryOperatorEnumerator(
                QueryOperatorEnumerator<TSource, TKey> source, Func<TSource, bool>? predicate,
                FirstQueryOperatorState<TKey> operatorState, CountdownEvent sharedBarrier, CancellationToken cancellationToken,
                IComparer<TKey> keyComparer, int partitionId)
            {
                Debug.Assert(source != null);
                Debug.Assert(operatorState != null);
                Debug.Assert(sharedBarrier != null);
                Debug.Assert(keyComparer != null);

                _source = source;
                _predicate = predicate;
                _operatorState = operatorState;
                _sharedBarrier = sharedBarrier;
                _cancellationToken = cancellationToken;
                _keyComparer = keyComparer;
                _partitionId = partitionId;
            }

            //---------------------------------------------------------------------------------------
            // Straightforward IEnumerator<T> methods.
            //

            internal override bool MoveNext([MaybeNullWhen(false), AllowNull] ref TSource currentElement, ref int currentKey)
            {
                Debug.Assert(_source != null);

                if (_alreadySearched)
                {
                    return false;
                }

                // Look for the lowest element.
                TSource candidate = default(TSource)!;
                TKey candidateKey = default(TKey)!;
                try
                {
                    TSource value = default(TSource)!;
                    TKey key = default(TKey)!;
                    int i = 0;
                    while (_source.MoveNext(ref value!, ref key))
                    {
                        if ((i++ & CancellationState.POLL_INTERVAL) == 0)
                            _cancellationToken.ThrowIfCancellationRequested();

                        // If the predicate is null or the current element satisfies it, we have found the
                        // current partition's "candidate" for the first element.  Note it.
                        if (_predicate == null || _predicate(value))
                        {
                            candidate = value;
                            candidateKey = key;

                            lock (_operatorState)
                            {
                                if (_operatorState._partitionId == -1 || _keyComparer.Compare(candidateKey, _operatorState._key) < 0)
                                {
                                    _operatorState._key = candidateKey;
                                    _operatorState._partitionId = _partitionId;
                                }
                            }

                            break;
                        }
                    }
                }
                finally
                {
                    // No matter whether we exit due to an exception or normal completion, we must ensure
                    // that we signal other partitions that we have completed.  Otherwise, we can cause deadlocks.
                    _sharedBarrier.Signal();
                }

                _alreadySearched = true;

                // Wait only if we may have the result
                if (_partitionId == _operatorState._partitionId)
                {
                    _sharedBarrier.Wait(_cancellationToken);

                    // Now re-read the shared index. If it's the same as ours, we won and return true.
                    if (_partitionId == _operatorState._partitionId)
                    {
                        currentElement = candidate;
                        currentKey = 0; // 1st (and only) element, so we hardcode the output index to 0.
                        return true;
                    }
                }

                // If we got here, we didn't win. Return false.
                return false;
            }

            protected override void Dispose(bool disposing)
            {
                _source.Dispose();
            }
        }

        private sealed class FirstQueryOperatorState<TKey>
        {
            internal TKey _key = default!;
            internal int _partitionId = -1;
        }
    }
}
