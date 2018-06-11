// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// JoinQueryOperator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// A join operator takes a left query tree and a right query tree, and then yields the
    /// matching pairs between the two. LINQ supports equi-key-based joins. Hence, a key-
    /// selection function for the left and right data types will yield keys of the same
    /// type for both. We then merely have to match elements from the left with elements from
    /// the right that have the same exact key. Note that this is an inner join. In other
    /// words, outer elements with no matching inner elements do not appear in the output.
    ///
    /// Hash-joins work in two phases:
    ///
    ///    (1) Building - we build a hash-table from one of the data sources. In the case
    ///            of this specific operator, the table is built from the hash-codes of
    ///            keys selected via the key selector function. Because elements may share
    ///            the same key, the table must support one-key-to-many-values.
    ///    (2) Probing - for each element in the data source not used for building, we
    ///            use its key to look into the hash-table. If we find elements under this
    ///            key, we just enumerate all of them, yielding them as join matches.
    ///
    /// Because hash-tables exhibit on average O(1) lookup, we turn what would have been
    /// an O(n*m) algorithm -- in the case of nested loops joins -- into an O(n) algorithm.
    /// We of course require some additional storage to do so, but in general this pays.
    /// </summary>
    /// <typeparam name="TLeftInput"></typeparam>
    /// <typeparam name="TRightInput"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TOutput"></typeparam>
    internal sealed class JoinQueryOperator<TLeftInput, TRightInput, TKey, TOutput> : BinaryQueryOperator<TLeftInput, TRightInput, TOutput>
    {
        private readonly Func<TLeftInput, TKey> _leftKeySelector; // The key selection routine for the outer (left) data source.
        private readonly Func<TRightInput, TKey> _rightKeySelector; // The key selection routine for the inner (right) data source.
        private readonly Func<TLeftInput, TRightInput, TOutput> _resultSelector; // The result selection routine.
        private readonly IEqualityComparer<TKey> _keyComparer; // An optional key comparison object.

        //---------------------------------------------------------------------------------------
        // Constructs a new join operator.
        //

        internal JoinQueryOperator(ParallelQuery<TLeftInput> left, ParallelQuery<TRightInput> right,
                                   Func<TLeftInput, TKey> leftKeySelector,
                                   Func<TRightInput, TKey> rightKeySelector,
                                   Func<TLeftInput, TRightInput, TOutput> resultSelector,
                                   IEqualityComparer<TKey> keyComparer)
            : base(left, right)
        {
            Debug.Assert(left != null && right != null, "child data sources cannot be null");
            Debug.Assert(leftKeySelector != null, "left key selector must not be null");
            Debug.Assert(rightKeySelector != null, "right key selector must not be null");
            Debug.Assert(resultSelector != null, "need a result selector function");

            _leftKeySelector = leftKeySelector;
            _rightKeySelector = rightKeySelector;
            _resultSelector = resultSelector;
            _keyComparer = keyComparer;
            _outputOrdered = LeftChild.OutputOrdered;

            SetOrdinalIndex(OrdinalIndexState.Shuffled);
        }

        public override void WrapPartitionedStream<TLeftKey, TRightKey>(
            PartitionedStream<TLeftInput, TLeftKey> leftStream, PartitionedStream<TRightInput, TRightKey> rightStream,
            IPartitionedStreamRecipient<TOutput> outputRecipient, bool preferStriping, QuerySettings settings)
        {
            Debug.Assert(rightStream.PartitionCount == leftStream.PartitionCount);

            if (LeftChild.OutputOrdered)
            {
                if(ExchangeUtilities.IsWorseThan(LeftChild.OrdinalIndexState, OrdinalIndexState.Increasing))
                {
                    PartitionedStream<TLeftInput, int> leftStreamInt =
                        QueryOperator<TLeftInput>.ExecuteAndCollectResults(leftStream, leftStream.PartitionCount, OutputOrdered, preferStriping, settings)
                        .GetPartitionedStream();
                    WrapPartitionedStreamHelper<int, TRightKey>(
                        ExchangeUtilities.HashRepartitionOrdered(leftStreamInt, _leftKeySelector, _keyComparer, null, settings.CancellationState.MergedCancellationToken),
                        rightStream, outputRecipient, settings.CancellationState.MergedCancellationToken);
                }
                else
                {
                    WrapPartitionedStreamHelper<TLeftKey, TRightKey>(
                        ExchangeUtilities.HashRepartitionOrdered(leftStream, _leftKeySelector, _keyComparer, null, settings.CancellationState.MergedCancellationToken),
                        rightStream, outputRecipient, settings.CancellationState.MergedCancellationToken);
                }
            }
            else
            {
                WrapPartitionedStreamHelper<int, TRightKey>(
                    ExchangeUtilities.HashRepartition(leftStream, _leftKeySelector, _keyComparer, null, settings.CancellationState.MergedCancellationToken),
                    rightStream, outputRecipient, settings.CancellationState.MergedCancellationToken);
            }
        }

        //---------------------------------------------------------------------------------------
        // This is a helper method. WrapPartitionedStream decides what type TLeftKey is going
        // to be, and then call this method with that key as a generic parameter.
        //

        private void WrapPartitionedStreamHelper<TLeftKey, TRightKey>(
            PartitionedStream<Pair<TLeftInput, TKey>, TLeftKey> leftHashStream, PartitionedStream<TRightInput, TRightKey> rightPartitionedStream,
            IPartitionedStreamRecipient<TOutput> outputRecipient, CancellationToken cancellationToken)
        {
            if (RightChild.OutputOrdered && LeftChild.OutputOrdered)
            {
                PairOutputKeyBuilder<TLeftKey, TRightKey> outputKeyBuilder = new PairOutputKeyBuilder<TLeftKey, TRightKey>();
                IComparer<Pair<TLeftKey, TRightKey>> outputKeyComparer =
                    new PairComparer<TLeftKey, TRightKey>(leftHashStream.KeyComparer, rightPartitionedStream.KeyComparer);

                WrapPartitionedStreamHelper<TLeftKey, TRightKey, Pair<TLeftKey, TRightKey>>(leftHashStream,
                    ExchangeUtilities.HashRepartitionOrdered(rightPartitionedStream, _rightKeySelector, _keyComparer, null, cancellationToken),
                    outputKeyBuilder, outputKeyComparer, outputRecipient, cancellationToken);
            }
            else
            {
                LeftKeyOutputKeyBuilder<TLeftKey, int> outputKeyBuilder = new LeftKeyOutputKeyBuilder<TLeftKey, int>();

                WrapPartitionedStreamHelper<TLeftKey, int, TLeftKey>(leftHashStream,
                    ExchangeUtilities.HashRepartition(rightPartitionedStream, _rightKeySelector, _keyComparer, null, cancellationToken),
                    outputKeyBuilder, leftHashStream.KeyComparer, outputRecipient, cancellationToken);
            }
        }

        private void WrapPartitionedStreamHelper<TLeftKey, TRightKey, TOutputKey>(
            PartitionedStream<Pair<TLeftInput, TKey>, TLeftKey> leftHashStream, PartitionedStream<Pair<TRightInput, TKey>, TRightKey> rightHashStream,
            HashJoinOutputKeyBuilder<TLeftKey, TRightKey, TOutputKey> outputKeyBuilder, IComparer<TOutputKey> outputKeyComparer,
            IPartitionedStreamRecipient<TOutput> outputRecipient, CancellationToken cancellationToken)
        {
            int partitionCount = leftHashStream.PartitionCount;

            PartitionedStream<TOutput, TOutputKey> outputStream =
                new PartitionedStream<TOutput, TOutputKey>(partitionCount, outputKeyComparer, OrdinalIndexState);

            for (int i = 0; i < partitionCount; i++)
            {
                JoinHashLookupBuilder<TRightInput, TRightKey, TKey> rightLookupBuilder =
                    new JoinHashLookupBuilder<TRightInput, TRightKey, TKey>(rightHashStream[i], _keyComparer);
                outputStream[i] = new HashJoinQueryOperatorEnumerator<TLeftInput, TLeftKey, TRightInput, TRightKey, TKey, TOutput, TOutputKey>(
                    leftHashStream[i], rightLookupBuilder, _resultSelector, outputKeyBuilder, cancellationToken);
            }

            outputRecipient.Receive(outputStream);
        }

        internal override QueryResults<TOutput> Open(QuerySettings settings, bool preferStriping)
        {
            QueryResults<TLeftInput> leftResults = LeftChild.Open(settings, false);
            QueryResults<TRightInput> rightResults = RightChild.Open(settings, false);

            return new BinaryQueryOperatorResults(leftResults, rightResults, this, settings, false);
        }

        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //

        internal override IEnumerable<TOutput> AsSequentialQuery(CancellationToken token)
        {
            IEnumerable<TLeftInput> wrappedLeftChild = CancellableEnumerable.Wrap(LeftChild.AsSequentialQuery(token), token);
            IEnumerable<TRightInput> wrappedRightChild = CancellableEnumerable.Wrap(RightChild.AsSequentialQuery(token), token);

            return wrappedLeftChild.Join(
                wrappedRightChild, _leftKeySelector, _rightKeySelector, _resultSelector, _keyComparer);
        }

        //---------------------------------------------------------------------------------------
        // Whether this operator performs a premature merge that would not be performed in
        // a similar sequential operation (i.e., in LINQ to Objects).
        //

        internal override bool LimitsParallelism
        {
            get { return false; }
        }
    }

    /// <summary>
    /// Class to build a HashJoinHashLookup of right elements for use in Join operations.
    /// </summary>
    /// <typeparam name="TElement"></typeparam>
    /// <typeparam name="TOrderKey"></typeparam>
    /// <typeparam name="THashKey"></typeparam>
    internal class JoinHashLookupBuilder<TElement, TOrderKey, THashKey> : HashLookupBuilder<TElement, TOrderKey, THashKey>
    {
        private readonly QueryOperatorEnumerator<Pair<TElement, THashKey>, TOrderKey> _dataSource; // data source. For building.
        private readonly IEqualityComparer<THashKey> _keyComparer; // An optional key comparison object.

        internal JoinHashLookupBuilder(QueryOperatorEnumerator<Pair<TElement, THashKey>, TOrderKey> dataSource, IEqualityComparer<THashKey> keyComparer)
        {
            Debug.Assert(dataSource != null);

            _dataSource = dataSource;
            _keyComparer = keyComparer;
        }

        public override HashJoinHashLookup<THashKey, TElement, TOrderKey> BuildHashLookup(CancellationToken cancellationToken)
        {
            HashLookup<THashKey, HashLookupValueList<TElement, TOrderKey>> lookup =
                new HashLookup<THashKey, HashLookupValueList<TElement, TOrderKey>>(_keyComparer);
            JoinBaseHashBuilder baseHashBuilder = new JoinBaseHashBuilder(lookup);

            BuildBaseHashLookup(_dataSource, baseHashBuilder, cancellationToken);

            return new JoinHashLookup(lookup);
        }

        protected override void Dispose(bool disposing)
        {
            Debug.Assert(_dataSource != null);

            _dataSource.Dispose();
        }

        /// <summary>
        /// Adds TElement,TOrderKey values to a HashLookup of HashLookupValueLists.
        /// </summary>
        private struct JoinBaseHashBuilder : IBaseHashBuilder<TElement, TOrderKey>
        {
            private readonly HashLookup<THashKey, HashLookupValueList<TElement, TOrderKey>> _base;

            public JoinBaseHashBuilder(HashLookup<THashKey, HashLookupValueList<TElement, TOrderKey>> baseLookup)
            {
                Debug.Assert(baseLookup != null);

                _base = baseLookup;
            }

            public bool Add(THashKey hashKey, TElement element, TOrderKey orderKey)
            {
                HashLookupValueList<TElement, TOrderKey> currentValue = default(HashLookupValueList<TElement, TOrderKey>);
                if (!_base.TryGetValue(hashKey, ref currentValue))
                {
                    currentValue = new HashLookupValueList<TElement, TOrderKey>(element, orderKey);
                    _base.Add(hashKey, currentValue);
                    return false;
                }
                else
                {
                    if (currentValue.Add(element, orderKey))
                    {
                        // We need to re-store this element because the pair is a value type.
                        _base[hashKey] = currentValue;
                    }
                    return true;
                }
            }
        }

        /// <summary>
        /// A wrapper for the HashLookup returned by JoinHashLookupBuilder.
        /// 
        /// Since Join operations do not require a default, this just passes the call on to the base lookup.
        /// </summary>
        private class JoinHashLookup : HashJoinHashLookup<THashKey, TElement, TOrderKey>
        {
            private readonly HashLookup<THashKey, HashLookupValueList<TElement, TOrderKey>> _base;

            internal JoinHashLookup(HashLookup<THashKey, HashLookupValueList<TElement, TOrderKey>> baseLookup)
            {
                Debug.Assert(baseLookup != null);

                _base = baseLookup;
            }

            public override bool TryGetValue(THashKey key, ref HashLookupValueList<TElement, TOrderKey> value)
            {
                return _base.TryGetValue(key, ref value);
            }
        }
    }
}
