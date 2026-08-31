// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// IntersectQueryOperator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// Operator that yields the intersection of two data sources.
    /// </summary>
    /// <typeparam name="TInputOutput"></typeparam>
    internal sealed class IntersectQueryOperator<TInputOutput> :
        BinaryQueryOperator<TInputOutput, TInputOutput, TInputOutput>
    {
        private readonly IEqualityComparer<TInputOutput>? _comparer; // An equality comparer.

        //---------------------------------------------------------------------------------------
        // Constructs a new intersection operator.
        //

        internal IntersectQueryOperator(ParallelQuery<TInputOutput> left, ParallelQuery<TInputOutput> right, IEqualityComparer<TInputOutput>? comparer)
            : base(left, right)
        {
            Debug.Assert(left != null && right != null, "child data sources cannot be null");

            _comparer = comparer;
            _outputOrdered = LeftChild.OutputOrdered;

            SetOrdinalIndex(OrdinalIndexState.Shuffled);
        }


        internal override QueryResults<TInputOutput> Open(
            QuerySettings settings, bool preferStriping)
        {
            // We just open our child operators, left and then right.  Do not propagate the preferStriping value, but
            // instead explicitly set it to false. Regardless of whether the parent prefers striping or range
            // partitioning, the output will be hash-partitioned.
            QueryResults<TInputOutput> leftChildResults = LeftChild.Open(settings, false);
            QueryResults<TInputOutput> rightChildResults = RightChild.Open(settings, false);

            return new BinaryQueryOperatorResults(leftChildResults, rightChildResults, this, settings, false);
        }

        public override void WrapPartitionedStream<TLeftKey, TRightKey>(
            PartitionedStream<TInputOutput, TLeftKey> leftPartitionedStream, PartitionedStream<TInputOutput, TRightKey> rightPartitionedStream,
            IPartitionedStreamRecipient<TInputOutput> outputRecipient, bool preferStriping, QuerySettings settings)
        {
            Debug.Assert(leftPartitionedStream.PartitionCount == rightPartitionedStream.PartitionCount);

            if (OutputOrdered)
            {
                WrapPartitionedStreamHelper<TLeftKey, TRightKey>(
                    ExchangeUtilities.HashRepartitionOrdered<TInputOutput, NoKeyMemoizationRequired, TLeftKey>(
                        leftPartitionedStream, null, null, _comparer, settings.CancellationState.MergedCancellationToken),
                    rightPartitionedStream, outputRecipient, settings.CancellationState.MergedCancellationToken);
            }
            else
            {
                WrapPartitionedStreamHelper<int, TRightKey>(
                    ExchangeUtilities.HashRepartition<TInputOutput, NoKeyMemoizationRequired, TLeftKey>(
                        leftPartitionedStream, null, null, _comparer, settings.CancellationState.MergedCancellationToken),
                    rightPartitionedStream, outputRecipient, settings.CancellationState.MergedCancellationToken);
            }
        }

        //---------------------------------------------------------------------------------------
        // This is a helper method. WrapPartitionedStream decides what type TLeftKey is going
        // to be, and then call this method with that key as a generic parameter.
        //

        private void WrapPartitionedStreamHelper<TLeftKey, TRightKey>(
            PartitionedStream<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> leftHashStream, PartitionedStream<TInputOutput, TRightKey> rightPartitionedStream,
            IPartitionedStreamRecipient<TInputOutput> outputRecipient, CancellationToken cancellationToken)
        {
            int partitionCount = leftHashStream.PartitionCount;

            PartitionedStream<Pair<TInputOutput, NoKeyMemoizationRequired>, int> rightHashStream =
                ExchangeUtilities.HashRepartition<TInputOutput, NoKeyMemoizationRequired, TRightKey>(
                    rightPartitionedStream, null, null, _comparer, cancellationToken);

            PartitionedStream<TInputOutput, TLeftKey> outputStream =
                new PartitionedStream<TInputOutput, TLeftKey>(partitionCount, leftHashStream.KeyComparer, OrdinalIndexState.Shuffled);
            for (int i = 0; i < partitionCount; i++)
            {
                if (OutputOrdered)
                {
                    outputStream[i] = new OrderedIntersectQueryOperatorEnumerator<TLeftKey>(
                        leftHashStream[i], rightHashStream[i], _comparer, leftHashStream.KeyComparer, cancellationToken);
                }
                else
                {
                    outputStream[i] = (QueryOperatorEnumerator<TInputOutput, TLeftKey>)(object)
                            new IntersectQueryOperatorEnumerator<TLeftKey>(leftHashStream[i], rightHashStream[i], _comparer, cancellationToken);
                }
            }

            outputRecipient.Receive(outputStream);
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
        // This enumerator performs the intersection operation incrementally. It does this by
        // maintaining a history -- in the form of a set -- of all data already seen. It then
        // only returns elements that are seen twice (returning each one only once).
        //

        private sealed class IntersectQueryOperatorEnumerator<TLeftKey> : QueryOperatorEnumerator<TInputOutput, int>
        {
            private readonly QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> _leftSource; // Left data source.
            private readonly QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, int> _rightSource; // Right data source.
            private readonly IEqualityComparer<TInputOutput>? _comparer; // Comparer to use for equality/hash-coding.
            private HashSet<TInputOutput>? _hashLookup; // The hash lookup, used to produce the intersection.
            private readonly CancellationToken _cancellationToken;
            private Shared<int>? _outputLoopCount;

            //---------------------------------------------------------------------------------------
            // Instantiates a new intersection operator.
            //

            internal IntersectQueryOperatorEnumerator(
                QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> leftSource,
                QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, int> rightSource,
                IEqualityComparer<TInputOutput>? comparer, CancellationToken cancellationToken)
            {
                Debug.Assert(leftSource != null);
                Debug.Assert(rightSource != null);

                _leftSource = leftSource;
                _rightSource = rightSource;
                _comparer = comparer;
                _cancellationToken = cancellationToken;
            }

            //---------------------------------------------------------------------------------------
            // Walks the two data sources, left and then right, to produce the intersection.
            //

            internal override bool MoveNext([MaybeNullWhen(false), AllowNull] ref TInputOutput currentElement, ref int currentKey)
            {
                Debug.Assert(_leftSource != null);
                Debug.Assert(_rightSource != null);

                // Build the set out of the right data source, if we haven't already.

                if (_hashLookup == null)
                {
                    _outputLoopCount = new Shared<int>(0);
                    _hashLookup = new HashSet<TInputOutput>(_comparer);

                    Pair<TInputOutput, NoKeyMemoizationRequired> rightElement = default(Pair<TInputOutput, NoKeyMemoizationRequired>);
                    int rightKeyUnused = default(int);

                    int i = 0;
                    while (_rightSource.MoveNext(ref rightElement, ref rightKeyUnused))
                    {
                        if ((i++ & CancellationState.POLL_INTERVAL) == 0)
                            _cancellationToken.ThrowIfCancellationRequested();

                        _hashLookup.Add(rightElement.First);
                    }
                }

                // Now iterate over the left data source, looking for matches.
                Pair<TInputOutput, NoKeyMemoizationRequired> leftElement = default(Pair<TInputOutput, NoKeyMemoizationRequired>);
                TLeftKey keyUnused = default(TLeftKey)!;

                while (_leftSource.MoveNext(ref leftElement, ref keyUnused))
                {
                    Debug.Assert(_outputLoopCount != null);
                    if ((_outputLoopCount.Value++ & CancellationState.POLL_INTERVAL) == 0)
                        _cancellationToken.ThrowIfCancellationRequested();

                    // If we found the element in our set, and if we haven't returned it yet,
                    // we can yield it to the caller. We also mark it so we know we've returned
                    // it once already and never will again.
                    if (_hashLookup.Remove(leftElement.First))
                    {
                        currentElement = leftElement.First;
#if DEBUG
                        currentKey = unchecked((int)0xdeadbeef);
#endif
                        return true;
                    }
                }

                return false;
            }

            protected override void Dispose(bool disposing)
            {
                Debug.Assert(_leftSource != null && _rightSource != null);
                _leftSource.Dispose();
                _rightSource.Dispose();
            }
        }

        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //

        internal override IEnumerable<TInputOutput> AsSequentialQuery(CancellationToken token)
        {
            IEnumerable<TInputOutput> wrappedLeftChild = CancellableEnumerable.Wrap(LeftChild.AsSequentialQuery(token), token);
            IEnumerable<TInputOutput> wrappedRightChild = CancellableEnumerable.Wrap(RightChild.AsSequentialQuery(token), token);
            return wrappedLeftChild.Intersect(wrappedRightChild, _comparer);
        }


        private sealed class OrderedIntersectQueryOperatorEnumerator<TLeftKey> : QueryOperatorEnumerator<TInputOutput, TLeftKey>
        {
            private readonly QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> _leftSource; // Left data source.
            private readonly QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, int> _rightSource; // Right data source.
            private readonly IEqualityComparer<Wrapper<TInputOutput>> _comparer; // Comparer to use for equality/hash-coding.
            private readonly IComparer<TLeftKey> _leftKeyComparer; // Comparer to use to determine ordering of order keys.
            private Dictionary<Wrapper<TInputOutput>, Pair<TInputOutput, TLeftKey>>? _hashLookup; // The hash lookup, used to produce the intersection.
            private readonly CancellationToken _cancellationToken;

            //---------------------------------------------------------------------------------------
            // Instantiates a new intersection operator.
            //

            internal OrderedIntersectQueryOperatorEnumerator(
                QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> leftSource,
                QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, int> rightSource,
                IEqualityComparer<TInputOutput>? comparer, IComparer<TLeftKey> leftKeyComparer,
                CancellationToken cancellationToken)
            {
                Debug.Assert(leftSource != null);
                Debug.Assert(rightSource != null);

                _leftSource = leftSource;
                _rightSource = rightSource;
                _comparer = new WrapperEqualityComparer<TInputOutput>(comparer);
                _leftKeyComparer = leftKeyComparer;
                _cancellationToken = cancellationToken;
            }

            //---------------------------------------------------------------------------------------
            // Walks the two data sources, left and then right, to produce the intersection.
            //

            internal override bool MoveNext([MaybeNullWhen(false), AllowNull] ref TInputOutput currentElement, [AllowNull] ref TLeftKey currentKey)
            {
                Debug.Assert(_leftSource != null);
                Debug.Assert(_rightSource != null);

                // Build the set out of the left data source, if we haven't already.
                int i = 0;
                if (_hashLookup == null)
                {
                    _hashLookup = new Dictionary<Wrapper<TInputOutput>, Pair<TInputOutput, TLeftKey>>(_comparer);

                    Pair<TInputOutput, NoKeyMemoizationRequired> leftElement = default(Pair<TInputOutput, NoKeyMemoizationRequired>);
                    TLeftKey leftKey = default(TLeftKey)!;
                    while (_leftSource.MoveNext(ref leftElement, ref leftKey))
                    {
                        if ((i++ & CancellationState.POLL_INTERVAL) == 0)
                            _cancellationToken.ThrowIfCancellationRequested();

                        // For each element, we track the smallest order key for that element that we saw so far
                        Pair<TInputOutput, TLeftKey> oldEntry;
                        Wrapper<TInputOutput> wrappedLeftElem = new Wrapper<TInputOutput>(leftElement.First);

                        // If this is the first occurrence of this element, or the order key is lower than all keys we saw previously,
                        // update the order key for this element.
                        if (!_hashLookup.TryGetValue(wrappedLeftElem, out oldEntry) || _leftKeyComparer.Compare(leftKey, oldEntry.Second) < 0)
                        {
                            // For each "elem" value, we store the smallest key, and the element value that had that key.
                            // Note that even though two element values are "equal" according to the EqualityComparer,
                            // we still cannot choose arbitrarily which of the two to yield.
                            _hashLookup[wrappedLeftElem] = new Pair<TInputOutput, TLeftKey>(leftElement.First, leftKey);
                        }
                    }
                }

                // Now iterate over the right data source, looking for matches.
                Pair<TInputOutput, NoKeyMemoizationRequired> rightElement = default(Pair<TInputOutput, NoKeyMemoizationRequired>);
                int rightKeyUnused = default(int);
                while (_rightSource.MoveNext(ref rightElement, ref rightKeyUnused))
                {
                    if ((i++ & CancellationState.POLL_INTERVAL) == 0)
                        _cancellationToken.ThrowIfCancellationRequested();

                    // If we found the element in our set, and if we haven't returned it yet,
                    // we can yield it to the caller. We also mark it so we know we've returned
                    // it once already and never will again.

                    Pair<TInputOutput, TLeftKey> entry;
                    Wrapper<TInputOutput> wrappedRightElem = new Wrapper<TInputOutput>(rightElement.First);

                    if (_hashLookup.TryGetValue(wrappedRightElem, out entry))
                    {
                        currentElement = entry.First;
                        currentKey = entry.Second;

                        _hashLookup.Remove(new Wrapper<TInputOutput>(entry.First));
                        return true;
                    }
                }

                return false;
            }

            protected override void Dispose(bool disposing)
            {
                Debug.Assert(_leftSource != null && _rightSource != null);
                _leftSource.Dispose();
                _rightSource.Dispose();
            }
        }
    }
}
