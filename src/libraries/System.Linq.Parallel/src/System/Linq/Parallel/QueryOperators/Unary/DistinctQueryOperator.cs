// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// DistinctQueryOperator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// This operator yields all of the distinct elements in a single data set. It works quite
    /// like the above set operations, with the obvious difference being that it only accepts
    /// a single data source as input.
    /// </summary>
    /// <typeparam name="TInputOutput"></typeparam>
    internal sealed class DistinctQueryOperator<TInputOutput> : UnaryQueryOperator<TInputOutput, TInputOutput>
    {
        private readonly IEqualityComparer<TInputOutput>? _comparer; // An (optional) equality comparer.

        //---------------------------------------------------------------------------------------
        // Constructs a new distinction operator.
        //

        internal DistinctQueryOperator(IEnumerable<TInputOutput> source, IEqualityComparer<TInputOutput>? comparer)
            : base(source)
        {
            Debug.Assert(source != null, "child data source cannot be null");
            _comparer = comparer;
            SetOrdinalIndexState(OrdinalIndexState.Shuffled);
        }

        //---------------------------------------------------------------------------------------
        // Just opens the current operator, including opening the child and wrapping it with
        // partitions as needed.
        //

        internal override QueryResults<TInputOutput> Open(QuerySettings settings, bool preferStriping)
        {
            // We just open our child operator.  Do not propagate the preferStriping value, but
            // instead explicitly set it to false. Regardless of whether the parent prefers striping or range
            // partitioning, the output will be hash-partitioned.
            QueryResults<TInputOutput> childResults = Child.Open(settings, false);
            return new UnaryQueryOperatorResults(childResults, this, settings, false);
        }

        internal override void WrapPartitionedStream<TKey>(
            PartitionedStream<TInputOutput, TKey> inputStream, IPartitionedStreamRecipient<TInputOutput> recipient, bool preferStriping, QuerySettings settings)
        {
            // Hash-repartition the source stream
            if (OutputOrdered)
            {
                WrapPartitionedStreamHelper<TKey>(
                    ExchangeUtilities.HashRepartitionOrdered<TInputOutput, NoKeyMemoizationRequired, TKey>(
                        inputStream, null, null, _comparer, settings.CancellationState.MergedCancellationToken),
                    recipient, settings.CancellationState.MergedCancellationToken);
            }
            else
            {
                WrapPartitionedStreamHelper<int>(
                    ExchangeUtilities.HashRepartition<TInputOutput, NoKeyMemoizationRequired, TKey>(
                        inputStream, null, null, _comparer, settings.CancellationState.MergedCancellationToken),
                    recipient, settings.CancellationState.MergedCancellationToken);
            }
        }

        //---------------------------------------------------------------------------------------
        // This is a helper method. WrapPartitionedStream decides what type TKey is going
        // to be, and then call this method with that key as a generic parameter.
        //

        private void WrapPartitionedStreamHelper<TKey>(
            PartitionedStream<Pair<TInputOutput, NoKeyMemoizationRequired>, TKey> hashStream,
            IPartitionedStreamRecipient<TInputOutput> recipient, CancellationToken cancellationToken)
        {
            int partitionCount = hashStream.PartitionCount;
            PartitionedStream<TInputOutput, TKey> outputStream =
                new PartitionedStream<TInputOutput, TKey>(partitionCount, hashStream.KeyComparer, OrdinalIndexState.Shuffled);

            for (int i = 0; i < partitionCount; i++)
            {
                if (OutputOrdered)
                {
                    outputStream[i] =
                        new OrderedDistinctQueryOperatorEnumerator<TKey>(hashStream[i], _comparer, hashStream.KeyComparer, cancellationToken);
                }
                else
                {
                    outputStream[i] = (QueryOperatorEnumerator<TInputOutput, TKey>)(object)
                                                                                   new DistinctQueryOperatorEnumerator<TKey>(hashStream[i], _comparer, cancellationToken);
                }
            }

            recipient.Receive(outputStream);
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
        // This enumerator performs the distinct operation incrementally. It does this by
        // maintaining a history -- in the form of a set -- of all data already seen. It simply
        // then doesn't return elements it has already seen before.
        //

        private sealed class DistinctQueryOperatorEnumerator<TKey> : QueryOperatorEnumerator<TInputOutput, int>
        {
            private readonly QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TKey> _source; // The data source.
            private readonly HashSet<TInputOutput> _hashLookup; // The hash lookup, used to produce the distinct set.
            private readonly CancellationToken _cancellationToken;
            private Shared<int>? _outputLoopCount; // Allocated in MoveNext to avoid false sharing.

            //---------------------------------------------------------------------------------------
            // Instantiates a new distinction operator.
            //

            internal DistinctQueryOperatorEnumerator(
                QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TKey> source, IEqualityComparer<TInputOutput>? comparer,
                CancellationToken cancellationToken)
            {
                Debug.Assert(source != null);
                _source = source;
                _hashLookup = new HashSet<TInputOutput>(comparer);
                _cancellationToken = cancellationToken;
            }

            //---------------------------------------------------------------------------------------
            // Walks the single data source, skipping elements it has already seen.
            //

            internal override bool MoveNext([MaybeNullWhen(false), AllowNull] ref TInputOutput currentElement, ref int currentKey)
            {
                Debug.Assert(_source != null);
                Debug.Assert(_hashLookup != null);

                // Iterate over this set's elements until we find a unique element.
                TKey keyUnused = default!;
                Pair<TInputOutput, NoKeyMemoizationRequired> current = default(Pair<TInputOutput, NoKeyMemoizationRequired>);

                if (_outputLoopCount == null)
                    _outputLoopCount = new Shared<int>(0);

                while (_source.MoveNext(ref current, ref keyUnused))
                {
                    if ((_outputLoopCount.Value++ & CancellationState.POLL_INTERVAL) == 0)
                        _cancellationToken.ThrowIfCancellationRequested();

                    // We ensure we never return duplicates by tracking them in our set.
                    if (_hashLookup.Add(current.First))
                    {
#if DEBUG
                        currentKey = unchecked((int)0xdeadbeef);
#endif
                        currentElement = current.First;
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

        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //

        internal override IEnumerable<TInputOutput> AsSequentialQuery(CancellationToken token)
        {
            IEnumerable<TInputOutput> wrappedChild = CancellableEnumerable.Wrap(Child.AsSequentialQuery(token), token);
            return wrappedChild.Distinct(_comparer);
        }

        private sealed class OrderedDistinctQueryOperatorEnumerator<TKey> : QueryOperatorEnumerator<TInputOutput, TKey>
        {
            private readonly QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TKey> _source; // The data source.
            private readonly Dictionary<Wrapper<TInputOutput>, TKey> _hashLookup; // The hash lookup, used to produce the distinct set.
            private readonly IComparer<TKey> _keyComparer; // Comparer to decide the key order.
            private IEnumerator<KeyValuePair<Wrapper<TInputOutput>, TKey>>? _hashLookupEnumerator; // Enumerates over _hashLookup.
            private readonly CancellationToken _cancellationToken;

            //---------------------------------------------------------------------------------------
            // Instantiates a new distinction operator.
            //

            internal OrderedDistinctQueryOperatorEnumerator(
                QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TKey> source,
                IEqualityComparer<TInputOutput>? comparer, IComparer<TKey> keyComparer,
                CancellationToken cancellationToken)
            {
                Debug.Assert(source != null);
                _source = source;
                _keyComparer = keyComparer;

                _hashLookup = new Dictionary<Wrapper<TInputOutput>, TKey>(
                    new WrapperEqualityComparer<TInputOutput>(comparer));

                _cancellationToken = cancellationToken;
            }

            //---------------------------------------------------------------------------------------
            // Walks the single data source, skipping elements it has already seen.
            //

            internal override bool MoveNext([MaybeNullWhen(false), AllowNull] ref TInputOutput currentElement, [AllowNull] ref TKey currentKey)
            {
                Debug.Assert(_source != null);
                Debug.Assert(_hashLookup != null);

                if (_hashLookupEnumerator == null)
                {
                    Pair<TInputOutput, NoKeyMemoizationRequired> elem = default(Pair<TInputOutput, NoKeyMemoizationRequired>);
                    TKey orderKey = default!;

                    int i = 0;
                    while (_source.MoveNext(ref elem, ref orderKey))
                    {
                        if ((i++ & CancellationState.POLL_INTERVAL) == 0)
                            _cancellationToken.ThrowIfCancellationRequested();

                        // For each element, we track the smallest order key for that element that we saw so far
                        TKey oldEntry;

                        Wrapper<TInputOutput> wrappedElem = new Wrapper<TInputOutput>(elem.First);

                        // If this is the first occurrence of this element, or the order key is lower than all keys we saw previously,
                        // update the order key for this element.
                        if (!_hashLookup.TryGetValue(wrappedElem, out oldEntry!) || _keyComparer.Compare(orderKey, oldEntry) < 0)
                        {
                            // For each "elem" value, we store the smallest key, and the element value that had that key.
                            // Note that even though two element values are "equal" according to the EqualityComparer,
                            // we still cannot choose arbitrarily which of the two to yield.
                            _hashLookup[wrappedElem] = orderKey;
                        }
                    }

                    _hashLookupEnumerator = _hashLookup.GetEnumerator();
                }

                if (_hashLookupEnumerator.MoveNext())
                {
                    KeyValuePair<Wrapper<TInputOutput>, TKey> currentPair = _hashLookupEnumerator.Current;
                    currentElement = currentPair.Key.Value;
                    currentKey = currentPair.Value;
                    return true;
                }

                return false;
            }

            protected override void Dispose(bool disposing)
            {
                Debug.Assert(_source != null);
                _source.Dispose();

                if (_hashLookupEnumerator != null)
                {
                    _hashLookupEnumerator.Dispose();
                }
            }
        }
    }
}
