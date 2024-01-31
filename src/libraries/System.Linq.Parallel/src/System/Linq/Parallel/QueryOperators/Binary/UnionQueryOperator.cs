// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// UnionQueryOperator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// Operator that yields the union of two data sources.
    /// </summary>
    /// <typeparam name="TInputOutput"></typeparam>
    internal sealed class UnionQueryOperator<TInputOutput> :
        BinaryQueryOperator<TInputOutput, TInputOutput, TInputOutput>
    {
        private readonly IEqualityComparer<TInputOutput>? _comparer; // An equality comparer.

        //---------------------------------------------------------------------------------------
        // Constructs a new union operator.
        //

        internal UnionQueryOperator(ParallelQuery<TInputOutput> left, ParallelQuery<TInputOutput> right, IEqualityComparer<TInputOutput>? comparer)
            : base(left, right)
        {
            Debug.Assert(left != null && right != null, "child data sources cannot be null");

            _comparer = comparer;
            _outputOrdered = LeftChild.OutputOrdered || RightChild.OutputOrdered;
        }

        //---------------------------------------------------------------------------------------
        // Just opens the current operator, including opening the child and wrapping it with
        // partitions as needed.
        //

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
            int partitionCount = leftStream.PartitionCount;

            // Wrap both child streams with hash repartition

            if (LeftChild.OutputOrdered)
            {
                PartitionedStream<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> leftHashStream =
                    ExchangeUtilities.HashRepartitionOrdered<TInputOutput, NoKeyMemoizationRequired, TLeftKey>(
                        leftStream, null, null, _comparer, settings.CancellationState.MergedCancellationToken);

                WrapPartitionedStreamFixedLeftType<TLeftKey, TRightKey>(
                    leftHashStream, rightStream, outputRecipient, partitionCount, settings.CancellationState.MergedCancellationToken);
            }
            else
            {
                PartitionedStream<Pair<TInputOutput, NoKeyMemoizationRequired>, int> leftHashStream =
                    ExchangeUtilities.HashRepartition<TInputOutput, NoKeyMemoizationRequired, TLeftKey>(
                        leftStream, null, null, _comparer, settings.CancellationState.MergedCancellationToken);

                WrapPartitionedStreamFixedLeftType<int, TRightKey>(
                    leftHashStream, rightStream, outputRecipient, partitionCount, settings.CancellationState.MergedCancellationToken);
            }
        }

        //---------------------------------------------------------------------------------------
        // A helper method that allows WrapPartitionedStream to fix the TLeftKey type parameter.
        //

        private void WrapPartitionedStreamFixedLeftType<TLeftKey, TRightKey>(
            PartitionedStream<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> leftHashStream, PartitionedStream<TInputOutput, TRightKey> rightStream,
            IPartitionedStreamRecipient<TInputOutput> outputRecipient, int partitionCount, CancellationToken cancellationToken)
        {
            if (RightChild.OutputOrdered)
            {
                PartitionedStream<Pair<TInputOutput, NoKeyMemoizationRequired>, TRightKey> rightHashStream =
                    ExchangeUtilities.HashRepartitionOrdered<TInputOutput, NoKeyMemoizationRequired, TRightKey>(
                        rightStream, null, null, _comparer, cancellationToken);

                WrapPartitionedStreamFixedBothTypes<TLeftKey, TRightKey>(
                    leftHashStream, rightHashStream, outputRecipient, partitionCount, cancellationToken);
            }
            else
            {
                PartitionedStream<Pair<TInputOutput, NoKeyMemoizationRequired>, int> rightHashStream =
                    ExchangeUtilities.HashRepartition<TInputOutput, NoKeyMemoizationRequired, TRightKey>(
                        rightStream, null, null, _comparer, cancellationToken);

                WrapPartitionedStreamFixedBothTypes<TLeftKey, int>(
                    leftHashStream, rightHashStream, outputRecipient, partitionCount, cancellationToken);
            }
        }

        //---------------------------------------------------------------------------------------
        // A helper method that allows WrapPartitionedStreamHelper to fix the TRightKey type parameter.
        //

        private void WrapPartitionedStreamFixedBothTypes<TLeftKey, TRightKey>(
            PartitionedStream<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> leftHashStream,
            PartitionedStream<Pair<TInputOutput, NoKeyMemoizationRequired>, TRightKey> rightHashStream,
            IPartitionedStreamRecipient<TInputOutput> outputRecipient, int partitionCount,
            CancellationToken cancellationToken)
        {
            if (LeftChild.OutputOrdered || RightChild.OutputOrdered)
            {
                IComparer<ConcatKey<TLeftKey, TRightKey>> compoundKeyComparer =
                    ConcatKey<TLeftKey, TRightKey>.MakeComparer(leftHashStream.KeyComparer, rightHashStream.KeyComparer);

                PartitionedStream<TInputOutput, ConcatKey<TLeftKey, TRightKey>> outputStream =
                    new PartitionedStream<TInputOutput, ConcatKey<TLeftKey, TRightKey>>(partitionCount, compoundKeyComparer, OrdinalIndexState.Shuffled);

                for (int i = 0; i < partitionCount; i++)
                {
                    outputStream[i] = new OrderedUnionQueryOperatorEnumerator<TLeftKey, TRightKey>(
                        leftHashStream[i], rightHashStream[i], LeftChild.OutputOrdered, RightChild.OutputOrdered,
                        _comparer, compoundKeyComparer, cancellationToken);
                }

                outputRecipient.Receive(outputStream);
            }
            else
            {
                PartitionedStream<TInputOutput, int> outputStream =
                    new PartitionedStream<TInputOutput, int>(partitionCount, Util.GetDefaultComparer<int>(), OrdinalIndexState.Shuffled);

                for (int i = 0; i < partitionCount; i++)
                {
                    outputStream[i] = new UnionQueryOperatorEnumerator<TLeftKey, TRightKey>(
                        leftHashStream[i], rightHashStream[i], _comparer, cancellationToken);
                }

                outputRecipient.Receive(outputStream);
            }
        }


        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //

        internal override IEnumerable<TInputOutput> AsSequentialQuery(CancellationToken token)
        {
            IEnumerable<TInputOutput> wrappedLeftChild = CancellableEnumerable.Wrap(LeftChild.AsSequentialQuery(token), token);
            IEnumerable<TInputOutput> wrappedRightChild = CancellableEnumerable.Wrap(RightChild.AsSequentialQuery(token), token);
            return wrappedLeftChild.Union(wrappedRightChild, _comparer);
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
        // This enumerator performs the union operation incrementally. It does this by maintaining
        // a history -- in the form of a set -- of all data already seen. It is careful not to
        // return any duplicates.
        //

        private sealed class UnionQueryOperatorEnumerator<TLeftKey, TRightKey> : QueryOperatorEnumerator<TInputOutput, int>
        {
            private QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey>? _leftSource; // Left data source.
            private QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TRightKey>? _rightSource; // Right data source.
            private HashSet<TInputOutput>? _hashLookup; // The hash lookup, used to produce the union.
            private readonly CancellationToken _cancellationToken;
            private Shared<int>? _outputLoopCount;
            private readonly IEqualityComparer<TInputOutput>? _comparer;

            //---------------------------------------------------------------------------------------
            // Instantiates a new union operator.
            //

            internal UnionQueryOperatorEnumerator(
                QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> leftSource,
                QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TRightKey> rightSource,
                IEqualityComparer<TInputOutput>? comparer,
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
            // Walks the two data sources, left and then right, to produce the union.
            //

            internal override bool MoveNext([MaybeNullWhen(false), AllowNull] ref TInputOutput currentElement, ref int currentKey)
            {
                if (_hashLookup == null)
                {
                    _hashLookup = new HashSet<TInputOutput>(_comparer);
                    _outputLoopCount = new Shared<int>(0);
                }

                Debug.Assert(_hashLookup != null);

                // Enumerate the left and then right data source. When each is done, we set the
                // field to null so we will skip it upon subsequent calls to MoveNext.
                if (_leftSource != null)
                {
                    // Iterate over this set's elements until we find a unique element.
                    TLeftKey keyUnused = default(TLeftKey)!;
                    Pair<TInputOutput, NoKeyMemoizationRequired> currentLeftElement = default(Pair<TInputOutput, NoKeyMemoizationRequired>);

                    int i = 0;
                    while (_leftSource.MoveNext(ref currentLeftElement, ref keyUnused))
                    {
                        if ((i++ & CancellationState.POLL_INTERVAL) == 0)
                            _cancellationToken.ThrowIfCancellationRequested();

                        // We ensure we never return duplicates by tracking them in our set.
                        if (_hashLookup.Add(currentLeftElement.First))
                        {
#if DEBUG
                            currentKey = unchecked((int)0xdeadbeef);
#endif
                            currentElement = currentLeftElement.First;
                            return true;
                        }
                    }

                    _leftSource.Dispose();
                    _leftSource = null;
                }


                if (_rightSource != null)
                {
                    // Iterate over this set's elements until we find a unique element.
                    TRightKey keyUnused = default(TRightKey)!;
                    Pair<TInputOutput, NoKeyMemoizationRequired> currentRightElement = default(Pair<TInputOutput, NoKeyMemoizationRequired>);

                    while (_rightSource.MoveNext(ref currentRightElement, ref keyUnused))
                    {
                        Debug.Assert(_outputLoopCount != null);
                        if ((_outputLoopCount.Value++ & CancellationState.POLL_INTERVAL) == 0)
                            _cancellationToken.ThrowIfCancellationRequested();

                        // We ensure we never return duplicates by tracking them in our set.
                        if (_hashLookup.Add(currentRightElement.First))
                        {
#if DEBUG
                            currentKey = unchecked((int)0xdeadbeef);
#endif
                            currentElement = currentRightElement.First;
                            return true;
                        }
                    }

                    _rightSource.Dispose();
                    _rightSource = null;
                }

                return false;
            }

            protected override void Dispose(bool disposing)
            {
                _leftSource?.Dispose();
                _rightSource?.Dispose();
            }
        }

        private sealed class OrderedUnionQueryOperatorEnumerator<TLeftKey, TRightKey> : QueryOperatorEnumerator<TInputOutput, ConcatKey<TLeftKey, TRightKey>>
        {
            private readonly QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> _leftSource; // Left data source.
            private readonly QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TRightKey> _rightSource; // Right data source.
            private readonly IComparer<ConcatKey<TLeftKey, TRightKey>> _keyComparer; // Comparer for compound order keys.
#pragma warning disable CA1859
            private IEnumerator<KeyValuePair<Wrapper<TInputOutput>, Pair<TInputOutput, ConcatKey<TLeftKey, TRightKey>>>>? _outputEnumerator; // Enumerator over the output of the union.
#pragma warning restore
            private readonly bool _leftOrdered; // Whether the left data source is ordered.
            private readonly bool _rightOrdered; // Whether the right data source is ordered.
            private readonly IEqualityComparer<TInputOutput>? _comparer; // Comparer for the elements.
            private readonly CancellationToken _cancellationToken;

            //---------------------------------------------------------------------------------------
            // Instantiates a new union operator.
            //

            internal OrderedUnionQueryOperatorEnumerator(
                QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> leftSource,
                QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TRightKey> rightSource,
                bool leftOrdered, bool rightOrdered, IEqualityComparer<TInputOutput>? comparer, IComparer<ConcatKey<TLeftKey, TRightKey>> keyComparer,
                CancellationToken cancellationToken)
            {
                Debug.Assert(leftSource != null);
                Debug.Assert(rightSource != null);

                _leftSource = leftSource;
                _rightSource = rightSource;
                _keyComparer = keyComparer;

                _leftOrdered = leftOrdered;
                _rightOrdered = rightOrdered;
                _comparer = comparer ?? EqualityComparer<TInputOutput>.Default;

                _cancellationToken = cancellationToken;
            }

            //---------------------------------------------------------------------------------------
            // Walks the two data sources, left and then right, to produce the union.
            //

            internal override bool MoveNext([MaybeNullWhen(false), AllowNull] ref TInputOutput currentElement, ref ConcatKey<TLeftKey, TRightKey> currentKey)
            {
                Debug.Assert(_leftSource != null);
                Debug.Assert(_rightSource != null);

                if (_outputEnumerator == null)
                {
                    IEqualityComparer<Wrapper<TInputOutput>> wrapperComparer = new WrapperEqualityComparer<TInputOutput>(_comparer);
                    Dictionary<Wrapper<TInputOutput>, Pair<TInputOutput, ConcatKey<TLeftKey, TRightKey>>> union =
                        new Dictionary<Wrapper<TInputOutput>, Pair<TInputOutput, ConcatKey<TLeftKey, TRightKey>>>(wrapperComparer);

                    Pair<TInputOutput, NoKeyMemoizationRequired> elem = default(Pair<TInputOutput, NoKeyMemoizationRequired>);
                    TLeftKey leftKey = default(TLeftKey)!;

                    int i = 0;
                    while (_leftSource.MoveNext(ref elem, ref leftKey))
                    {
                        if ((i++ & CancellationState.POLL_INTERVAL) == 0)
                            _cancellationToken.ThrowIfCancellationRequested();

                        ConcatKey<TLeftKey, TRightKey> key =
                            ConcatKey<TLeftKey, TRightKey>.MakeLeft(_leftOrdered ? leftKey : default);
                        Pair<TInputOutput, ConcatKey<TLeftKey, TRightKey>> oldEntry;
                        Wrapper<TInputOutput> wrappedElem = new Wrapper<TInputOutput>(elem.First);

                        if (!union.TryGetValue(wrappedElem, out oldEntry) || _keyComparer.Compare(key, oldEntry.Second) < 0)
                        {
                            union[wrappedElem] = new Pair<TInputOutput, ConcatKey<TLeftKey, TRightKey>>(elem.First, key);
                        }
                    }

                    TRightKey rightKey = default(TRightKey)!;
                    while (_rightSource.MoveNext(ref elem, ref rightKey))
                    {
                        if ((i++ & CancellationState.POLL_INTERVAL) == 0)
                            _cancellationToken.ThrowIfCancellationRequested();

                        ConcatKey<TLeftKey, TRightKey> key =
                            ConcatKey<TLeftKey, TRightKey>.MakeRight(_rightOrdered ? rightKey : default);
                        Pair<TInputOutput, ConcatKey<TLeftKey, TRightKey>> oldEntry;
                        Wrapper<TInputOutput> wrappedElem = new Wrapper<TInputOutput>(elem.First);

                        if (!union.TryGetValue(wrappedElem, out oldEntry) || _keyComparer.Compare(key, oldEntry.Second) < 0)
                        {
                            union[wrappedElem] = new Pair<TInputOutput, ConcatKey<TLeftKey, TRightKey>>(elem.First, key);
                        }
                    }

                    _outputEnumerator = union.GetEnumerator();
                }

                if (_outputEnumerator.MoveNext())
                {
                    Pair<TInputOutput, ConcatKey<TLeftKey, TRightKey>> current = _outputEnumerator.Current.Value;
                    currentElement = current.First;
                    currentKey = current.Second;
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
