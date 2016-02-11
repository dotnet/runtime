// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ExceptQueryOperator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// Operator that yields the elements from the first data source that aren't in the second.
    /// This is known as the set relative complement, i.e. left - right. 
    /// </summary>
    /// <typeparam name="TInputOutput"></typeparam>
    internal sealed class ExceptQueryOperator<TInputOutput> :
        BinaryQueryOperator<TInputOutput, TInputOutput, TInputOutput>
    {
        private readonly IEqualityComparer<TInputOutput> _comparer; // An equality comparer.

        //---------------------------------------------------------------------------------------
        // Constructs a new set except operator.
        //

        internal ExceptQueryOperator(ParallelQuery<TInputOutput> left, ParallelQuery<TInputOutput> right, IEqualityComparer<TInputOutput> comparer)
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
            PartitionedStream<TInputOutput, TLeftKey> leftStream, PartitionedStream<TInputOutput, TRightKey> rightStream,
            IPartitionedStreamRecipient<TInputOutput> outputRecipient, bool preferStriping, QuerySettings settings)
        {
            Debug.Assert(leftStream.PartitionCount == rightStream.PartitionCount);

            if (OutputOrdered)
            {
                WrapPartitionedStreamHelper<TLeftKey, TRightKey>(
                    ExchangeUtilities.HashRepartitionOrdered<TInputOutput, NoKeyMemoizationRequired, TLeftKey>(
                        leftStream, null, null, _comparer, settings.CancellationState.MergedCancellationToken),
                    rightStream, outputRecipient, settings.CancellationState.MergedCancellationToken);
            }
            else
            {
                WrapPartitionedStreamHelper<int, TRightKey>(
                    ExchangeUtilities.HashRepartition<TInputOutput, NoKeyMemoizationRequired, TLeftKey>(
                        leftStream, null, null, _comparer, settings.CancellationState.MergedCancellationToken),
                    rightStream, outputRecipient, settings.CancellationState.MergedCancellationToken);
            }
        }

        //---------------------------------------------------------------------------------------
        // This is a helper method. WrapPartitionedStream decides what type TLeftKey is going
        // to be, and then call this method with that key as a generic parameter.
        //

        private void WrapPartitionedStreamHelper<TLeftKey, TRightKey>(
            PartitionedStream<Pair<TInputOutput,NoKeyMemoizationRequired>, TLeftKey> leftHashStream, PartitionedStream<TInputOutput, TRightKey> rightPartitionedStream,
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
                    outputStream[i] = new OrderedExceptQueryOperatorEnumerator<TLeftKey>(
                        leftHashStream[i], rightHashStream[i], _comparer, leftHashStream.KeyComparer, cancellationToken);
                }
                else
                {
                    outputStream[i] = (QueryOperatorEnumerator<TInputOutput, TLeftKey>)(object)
                        new ExceptQueryOperatorEnumerator<TLeftKey>(leftHashStream[i], rightHashStream[i], _comparer, cancellationToken);
                }
            }

            outputRecipient.Receive(outputStream);
        }


        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //

        internal override IEnumerable<TInputOutput> AsSequentialQuery(CancellationToken token)
        {
            IEnumerable<TInputOutput> wrappedLeftChild = CancellableEnumerable.Wrap(LeftChild.AsSequentialQuery(token), token);
            IEnumerable<TInputOutput> wrappedRightChild = CancellableEnumerable.Wrap(RightChild.AsSequentialQuery(token), token);
            return wrappedLeftChild.Except(wrappedRightChild, _comparer);
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
        // This enumerator calculates the distinct set incrementally. It does this by maintaining
        // a history -- in the form of a set -- of all data already seen. It then only returns
        // elements that have not yet been seen.
        //

        class ExceptQueryOperatorEnumerator<TLeftKey> : QueryOperatorEnumerator<TInputOutput, int>
        {
            private QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> _leftSource; // Left data source.
            private QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, int> _rightSource; // Right data source.
            private IEqualityComparer<TInputOutput> _comparer; // A comparer used for equality checks/hash-coding.
            private Set<TInputOutput> _hashLookup; // The hash lookup, used to produce the distinct set.
            private CancellationToken _cancellationToken;
            private Shared<int> _outputLoopCount;

            //---------------------------------------------------------------------------------------
            // Instantiates a new except query operator enumerator.
            //

            internal ExceptQueryOperatorEnumerator(
                QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> leftSource,
                QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, int> rightSource,
                IEqualityComparer<TInputOutput> comparer,
                CancellationToken cancellationToken)
            {
                Debug.Assert(leftSource != null);
                Debug.Assert(rightSource != null);

                _leftSource = leftSource;
                _rightSource = rightSource;
                _comparer = comparer;
                _cancellationToken = cancellationToken;
            }

            //---------------------------------------------------------------------------------------
            // Walks the two data sources, left and then right, to produce the distinct set
            //

            internal override bool MoveNext(ref TInputOutput currentElement, ref int currentKey)
            {
                Debug.Assert(_leftSource != null);
                Debug.Assert(_rightSource != null);

                // Build the set out of the left data source, if we haven't already.

                if (_hashLookup == null)
                {
                    _outputLoopCount = new Shared<int>(0);

                    _hashLookup = new Set<TInputOutput>(_comparer);

                    Pair<TInputOutput, NoKeyMemoizationRequired> rightElement = default(Pair<TInputOutput, NoKeyMemoizationRequired>);
                    int rightKeyUnused = default(int);

                    int i = 0;
                    while (_rightSource.MoveNext(ref rightElement, ref rightKeyUnused))
                    {
                        if ((i++ & CancellationState.POLL_INTERVAL) == 0)
                            CancellationState.ThrowIfCanceled(_cancellationToken);

                        _hashLookup.Add(rightElement.First);
                    }
                }

                // Now iterate over the right data source, looking for matches.
                Pair<TInputOutput, NoKeyMemoizationRequired> leftElement = default(Pair<TInputOutput, NoKeyMemoizationRequired>);
                TLeftKey leftKeyUnused = default(TLeftKey);

                while (_leftSource.MoveNext(ref leftElement, ref leftKeyUnused))
                {
                    if ((_outputLoopCount.Value++ & CancellationState.POLL_INTERVAL) == 0)
                        CancellationState.ThrowIfCanceled(_cancellationToken);

                    if (_hashLookup.Add(leftElement.First))
                    {
                        // This element has never been seen. Return it.
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

        class OrderedExceptQueryOperatorEnumerator<TLeftKey> : QueryOperatorEnumerator<TInputOutput, TLeftKey>
        {
            private QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> _leftSource; // Left data source.
            private QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, int> _rightSource; // Right data source.
            private IEqualityComparer<TInputOutput> _comparer; // A comparer used for equality checks/hash-coding.
            private IComparer<TLeftKey> _leftKeyComparer; // A comparer for order keys.
            private IEnumerator<KeyValuePair<Wrapper<TInputOutput>, Pair<TInputOutput, TLeftKey>>> _outputEnumerator; // The enumerator output elements + order keys.
            private CancellationToken _cancellationToken;

            //---------------------------------------------------------------------------------------
            // Instantiates a new except query operator enumerator.
            //

            internal OrderedExceptQueryOperatorEnumerator(
                QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> leftSource,
                QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, int> rightSource,
                IEqualityComparer<TInputOutput> comparer, IComparer<TLeftKey> leftKeyComparer,
                CancellationToken cancellationToken)
            {
                Debug.Assert(leftSource != null);
                Debug.Assert(rightSource != null);

                _leftSource = leftSource;
                _rightSource = rightSource;
                _comparer = comparer;
                _leftKeyComparer = leftKeyComparer;
                _cancellationToken = cancellationToken;
            }

            //---------------------------------------------------------------------------------------
            // Walks the two data sources, left and then right, to produce the distinct set
            //

            internal override bool MoveNext(ref TInputOutput currentElement, ref TLeftKey currentKey)
            {
                Debug.Assert(_leftSource != null);
                Debug.Assert(_rightSource != null);

                // Build the set out of the left data source, if we haven't already.
                if (_outputEnumerator == null)
                {
                    Set<TInputOutput> rightLookup = new Set<TInputOutput>(_comparer);

                    Pair<TInputOutput, NoKeyMemoizationRequired> rightElement = default(Pair<TInputOutput, NoKeyMemoizationRequired>);
                    int rightKeyUnused = default(int);
                    int i = 0;
                    while (_rightSource.MoveNext(ref rightElement, ref rightKeyUnused))
                    {
                        if ((i++ & CancellationState.POLL_INTERVAL) == 0)
                            CancellationState.ThrowIfCanceled(_cancellationToken);

                        rightLookup.Add(rightElement.First);
                    }

                    var leftLookup =
                        new Dictionary<Wrapper<TInputOutput>, Pair<TInputOutput, TLeftKey>>(
                            new WrapperEqualityComparer<TInputOutput>(_comparer));

                    Pair<TInputOutput, NoKeyMemoizationRequired> leftElement = default(Pair<TInputOutput, NoKeyMemoizationRequired>);
                    TLeftKey leftKey = default(TLeftKey);
                    while (_leftSource.MoveNext(ref leftElement, ref leftKey))
                    {
                        if ((i++ & CancellationState.POLL_INTERVAL) == 0)
                            CancellationState.ThrowIfCanceled(_cancellationToken);

                        if (rightLookup.Contains(leftElement.First))
                        {
                            continue;
                        }

                        Pair<TInputOutput, TLeftKey> oldEntry;
                        Wrapper<TInputOutput> wrappedLeftElement = new Wrapper<TInputOutput>(leftElement.First);
                        if (!leftLookup.TryGetValue(wrappedLeftElement, out oldEntry) || _leftKeyComparer.Compare(leftKey, oldEntry.Second) < 0)
                        {
                            // For each "elem" value, we store the smallest key, and the element value that had that key.
                            // Note that even though two element values are "equal" according to the EqualityComparer,
                            // we still cannot choose arbitrarily which of the two to yield.
                            leftLookup[wrappedLeftElement] = new Pair<TInputOutput, TLeftKey>(leftElement.First, leftKey);
                        }
                    }

                    _outputEnumerator = leftLookup.GetEnumerator();
                }

                if (_outputEnumerator.MoveNext())
                {
                    Pair<TInputOutput,TLeftKey> currentPair = _outputEnumerator.Current.Value;
                    currentElement = currentPair.First;
                    currentKey = currentPair.Second;
                    return true;
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
