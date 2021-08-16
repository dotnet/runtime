// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// DefaultIfEmptyQueryOperator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// This operator just exposes elements directly from the underlying data source, if
    /// it's not empty, or yields a single default element if the data source is empty.
    /// There is a minimal amount of synchronization at the beginning, until all partitions
    /// have registered whether their stream is empty or not. Once the 0th partition knows
    /// that at least one other partition is non-empty, it may proceed. Otherwise, it is
    /// the 0th partition which yields the default value.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    internal sealed class DefaultIfEmptyQueryOperator<TSource> : UnaryQueryOperator<TSource, TSource>
    {
        private readonly TSource _defaultValue; // The default value to use (if empty).

        //---------------------------------------------------------------------------------------
        // Initializes a new reverse operator.
        //
        // Arguments:
        //     child                - the child whose data we will reverse
        //

        internal DefaultIfEmptyQueryOperator(IEnumerable<TSource> child, TSource defaultValue)
            : base(child)
        {
            Debug.Assert(child != null, "child data source cannot be null");
            _defaultValue = defaultValue;
            SetOrdinalIndexState(ExchangeUtilities.Worse(Child.OrdinalIndexState, OrdinalIndexState.Correct));
        }

        //---------------------------------------------------------------------------------------
        // Just opens the current operator, including opening the child and wrapping it with
        // partitions as needed.
        //

        internal override QueryResults<TSource> Open(QuerySettings settings, bool preferStriping)
        {
            // We just open the child operator.
            QueryResults<TSource> childQueryResults = Child.Open(settings, preferStriping);
            return new UnaryQueryOperatorResults(childQueryResults, this, settings, preferStriping);
        }

        internal override void WrapPartitionedStream<TKey>(
            PartitionedStream<TSource, TKey> inputStream, IPartitionedStreamRecipient<TSource> recipient, bool preferStriping, QuerySettings settings)
        {
            int partitionCount = inputStream.PartitionCount;

            // Generate the shared data.
            Shared<int> sharedEmptyCount = new Shared<int>(0);
            CountdownEvent sharedLatch = new CountdownEvent(partitionCount - 1);

            PartitionedStream<TSource, TKey> outputStream =
                new PartitionedStream<TSource, TKey>(partitionCount, inputStream.KeyComparer, OrdinalIndexState);

            for (int i = 0; i < partitionCount; i++)
            {
                outputStream[i] = new DefaultIfEmptyQueryOperatorEnumerator<TKey>(
                    inputStream[i], _defaultValue, i, partitionCount, sharedEmptyCount, sharedLatch, settings.CancellationState.MergedCancellationToken);
            }

            recipient.Receive(outputStream);
        }

        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //

        internal override IEnumerable<TSource> AsSequentialQuery(CancellationToken token)
        {
            return Child.AsSequentialQuery(token).DefaultIfEmpty(_defaultValue);
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
        // The enumerator type responsible for executing the default-if-empty operation.
        //

        private sealed class DefaultIfEmptyQueryOperatorEnumerator<TKey> : QueryOperatorEnumerator<TSource, TKey>
        {
            private readonly QueryOperatorEnumerator<TSource, TKey> _source; // The data source to enumerate.
            private bool _lookedForEmpty; // Whether this partition has looked for empty yet.
            private readonly int _partitionIndex; // This enumerator's partition index.
            private readonly int _partitionCount; // The number of partitions.
            private readonly TSource _defaultValue; // The default value if the 0th partition is empty.

            // Data shared among partitions.
            private readonly Shared<int> _sharedEmptyCount; // The number of empty partitions.

            private readonly CountdownEvent _sharedLatch; // Shared latch, signaled when partitions process the 1st item.
            private readonly CancellationToken _cancelToken; // Token used to cancel this operator.

            //---------------------------------------------------------------------------------------
            // Instantiates a new select enumerator.
            //

            internal DefaultIfEmptyQueryOperatorEnumerator(
                QueryOperatorEnumerator<TSource, TKey> source, TSource defaultValue, int partitionIndex, int partitionCount,
                Shared<int> sharedEmptyCount, CountdownEvent sharedLatch, CancellationToken cancelToken)
            {
                Debug.Assert(source != null);
                Debug.Assert(0 <= partitionIndex && partitionIndex < partitionCount);
                Debug.Assert(partitionCount > 0);
                Debug.Assert(sharedEmptyCount != null);
                Debug.Assert(sharedLatch != null);

                _source = source;
                _defaultValue = defaultValue;
                _partitionIndex = partitionIndex;
                _partitionCount = partitionCount;
                _sharedEmptyCount = sharedEmptyCount;
                _sharedLatch = sharedLatch;
                _cancelToken = cancelToken;
            }

            //---------------------------------------------------------------------------------------
            // Straightforward IEnumerator<T> methods.
            //

            internal override bool MoveNext([MaybeNullWhen(false), AllowNull] ref TSource currentElement, [AllowNull] ref TKey currentKey)
            {
                Debug.Assert(_source != null);

                bool moveNextResult = _source.MoveNext(ref currentElement!, ref currentKey);

                // There is special logic the first time this function is called.
                if (!_lookedForEmpty)
                {
                    // Ensure we don't enter this loop again.
                    _lookedForEmpty = true;

                    if (!moveNextResult)
                    {
                        if (_partitionIndex == 0)
                        {
                            // If this is the 0th partition, we must wait for all others.  Note: we could
                            // actually do a wait-any here: if at least one other partition finds an element,
                            // there is strictly no need to wait.  But this would require extra coordination
                            // which may or may not be worth the trouble.
                            _sharedLatch.Wait(_cancelToken);
                            _sharedLatch.Dispose();

                            // Now see if there were any other partitions with data.
                            if (_sharedEmptyCount.Value == _partitionCount - 1)
                            {
                                // No data, we will yield the default value.
                                currentElement = _defaultValue;
                                currentKey = default(TKey)!;
                                return true;
                            }
                            else
                            {
                                // Another partition has data, we are done.
                                return false;
                            }
                        }
                        else
                        {
                            // Not the 0th partition, we will increment the shared empty counter.
                            Interlocked.Increment(ref _sharedEmptyCount.Value);
                        }
                    }

                    // Every partition (but the 0th) will signal the latch the first time.
                    if (_partitionIndex != 0)
                    {
                        _sharedLatch.Signal();
                    }
                }

                return moveNextResult;
            }

            protected override void Dispose(bool disposing)
            {
                _source.Dispose();
            }
        }
    }
}
