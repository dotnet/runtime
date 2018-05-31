// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// GroupJoinQueryOperator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// A group join operator takes a left query tree and a right query tree, and then yields
    /// the matching elements between the two. This can be used for outer joins, i.e. those
    /// where an outer element has no matching inner elements -- the result is just an empty
    /// list. As with the join algorithm above, we currently use a hash join algorithm.
    /// </summary>
    /// <typeparam name="TLeftInput"></typeparam>
    /// <typeparam name="TRightInput"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TOutput"></typeparam>
    internal sealed class GroupJoinQueryOperator<TLeftInput, TRightInput, TKey, TOutput> : BinaryQueryOperator<TLeftInput, TRightInput, TOutput>
    {
        private readonly Func<TLeftInput, TKey> _leftKeySelector; // The key selection routine for the outer (left) data source.
        private readonly Func<TRightInput, TKey> _rightKeySelector; // The key selection routine for the inner (right) data source.
        private readonly Func<TLeftInput, IEnumerable<TRightInput>, TOutput> _resultSelector; // The result selection routine.
        private readonly IEqualityComparer<TKey> _keyComparer; // An optional key comparison object.

        //---------------------------------------------------------------------------------------
        // Constructs a new join operator.
        //

        internal GroupJoinQueryOperator(ParallelQuery<TLeftInput> left, ParallelQuery<TRightInput> right,
                                        Func<TLeftInput, TKey> leftKeySelector,
                                        Func<TRightInput, TKey> rightKeySelector,
                                        Func<TLeftInput, IEnumerable<TRightInput>, TOutput> resultSelector,
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

        //---------------------------------------------------------------------------------------
        // Just opens the current operator, including opening the child and wrapping it with
        // partitions as needed.
        //

        internal override QueryResults<TOutput> Open(QuerySettings settings, bool preferStriping)
        {
            QueryResults<TLeftInput> leftResults = LeftChild.Open(settings, false);
            QueryResults<TRightInput> rightResults = RightChild.Open(settings, false);

            return new BinaryQueryOperatorResults(leftResults, rightResults, this, settings, false);
        }

        public override void WrapPartitionedStream<TLeftKey, TRightKey>(
            PartitionedStream<TLeftInput, TLeftKey> leftStream, PartitionedStream<TRightInput, TRightKey> rightStream,
            IPartitionedStreamRecipient<TOutput> outputRecipient, bool preferStriping, QuerySettings settings)
        {
            Debug.Assert(rightStream.PartitionCount == leftStream.PartitionCount);
            int partitionCount = leftStream.PartitionCount;

            if (LeftChild.OutputOrdered)
            {
                WrapPartitionedStreamHelper<TLeftKey, TRightKey>(
                    ExchangeUtilities.HashRepartitionOrdered(leftStream, _leftKeySelector, _keyComparer, null, settings.CancellationState.MergedCancellationToken),
                    rightStream, outputRecipient, partitionCount, settings.CancellationState.MergedCancellationToken);
            }
            else
            {
                WrapPartitionedStreamHelper<int, TRightKey>(
                    ExchangeUtilities.HashRepartition(leftStream, _leftKeySelector, _keyComparer, null, settings.CancellationState.MergedCancellationToken),
                    rightStream, outputRecipient, partitionCount, settings.CancellationState.MergedCancellationToken);
            }
        }

        //---------------------------------------------------------------------------------------
        // This is a helper method. WrapPartitionedStream decides what type TLeftKey is going
        // to be, and then call this method with that key as a generic parameter.
        //

        private void WrapPartitionedStreamHelper<TLeftKey, TRightKey>(
            PartitionedStream<Pair<TLeftInput,TKey>, TLeftKey> leftHashStream, PartitionedStream<TRightInput, TRightKey> rightPartitionedStream,
            IPartitionedStreamRecipient<TOutput> outputRecipient, int partitionCount, CancellationToken cancellationToken)
        {
            if (RightChild.OutputOrdered)
            {
                PartitionedStream<Pair<TRightInput, TKey>, TRightKey> rePartitionedRightStream = ExchangeUtilities.HashRepartitionOrdered(
                    rightPartitionedStream, _rightKeySelector, _keyComparer, null, cancellationToken);

                HashLookupBuilder<IEnumerable<TRightInput>, Pair<bool, TRightKey>, TKey>[] rightLookupBuilders =
                    new HashLookupBuilder<IEnumerable<TRightInput>, Pair<bool, TRightKey>, TKey>[partitionCount];
                for (int i = 0; i < partitionCount; i++)
                {
                    rightLookupBuilders[i] = new OrderedGroupJoinHashLookupBuilder<TRightInput, TRightKey, TKey>(
                        rePartitionedRightStream[i], _keyComparer, rePartitionedRightStream.KeyComparer);
                }

                WrapPartitionedStreamHelper<TLeftKey, Pair<bool, TRightKey>>(leftHashStream, rightLookupBuilders,
                    CreateComparer(rightPartitionedStream.KeyComparer), outputRecipient, partitionCount, cancellationToken);
            }
            else
            {
                PartitionedStream<Pair<TRightInput, TKey>, int> rePartitionedRightStream = ExchangeUtilities.HashRepartition(
                    rightPartitionedStream, _rightKeySelector, _keyComparer, null, cancellationToken);

                HashLookupBuilder<IEnumerable<TRightInput>, int, TKey>[] rightLookupBuilders =
                    new HashLookupBuilder<IEnumerable<TRightInput>, int, TKey>[partitionCount];
                for (int i = 0; i < partitionCount; i++)
                {
                    rightLookupBuilders[i] = new GroupJoinHashLookupBuilder<TRightInput, int, TKey>(
                        rePartitionedRightStream[i], _keyComparer);
                }

                WrapPartitionedStreamHelper<TLeftKey, int>(leftHashStream, rightLookupBuilders,
                    null, outputRecipient, partitionCount, cancellationToken);
            }
        }

        private void WrapPartitionedStreamHelper<TLeftKey, TRightKey>(
            PartitionedStream<Pair<TLeftInput, TKey>, TLeftKey> leftHashStream,
            HashLookupBuilder<IEnumerable<TRightInput>, TRightKey, TKey>[] rightLookupBuilders,
            IComparer<TRightKey> rightKeyComparer, IPartitionedStreamRecipient<TOutput> outputRecipient,
            int partitionCount, CancellationToken cancellationToken)
        {
            if (RightChild.OutputOrdered && LeftChild.OutputOrdered)
            {
                PairOutputKeyBuilder<TLeftKey, TRightKey> outputKeyBuilder = new PairOutputKeyBuilder<TLeftKey, TRightKey>();
                IComparer<Pair<TLeftKey, TRightKey>> outputKeyComparer = new PairComparer<TLeftKey, TRightKey>(leftHashStream.KeyComparer, rightKeyComparer);

                WrapPartitionedStreamHelper<TLeftKey, TRightKey, Pair<TLeftKey, TRightKey>>(leftHashStream, rightLookupBuilders,
                    outputKeyBuilder, outputKeyComparer, outputRecipient, partitionCount, cancellationToken);
            }
            else
            {
                LeftKeyOutputKeyBuilder<TLeftKey, TRightKey> outputKeyBuilder = new LeftKeyOutputKeyBuilder<TLeftKey, TRightKey>();

                WrapPartitionedStreamHelper<TLeftKey, TRightKey, TLeftKey>(leftHashStream, rightLookupBuilders,
                    outputKeyBuilder, leftHashStream.KeyComparer, outputRecipient, partitionCount, cancellationToken);
            }
        }

        private IComparer<Pair<bool, TRightKey>> CreateComparer<TRightKey>(IComparer<TRightKey> comparer)
        {
            return CreateComparer(Comparer<bool>.Default, comparer);
        }

        private IComparer<Pair<TLeftKey, TRightKey>> CreateComparer<TLeftKey, TRightKey>(IComparer<TLeftKey> leftKeyComparer, IComparer<TRightKey> rightKeyComparer)
        {
            return new PairComparer<TLeftKey, TRightKey>(leftKeyComparer, rightKeyComparer);
        }

        private void WrapPartitionedStreamHelper<TLeftKey, TRightKey, TOutputKey>(
            PartitionedStream<Pair<TLeftInput, TKey>, TLeftKey> leftHashStream,
            HashLookupBuilder<IEnumerable<TRightInput>, TRightKey, TKey>[] rightLookupBuilders,
            HashJoinOutputKeyBuilder<TLeftKey, TRightKey, TOutputKey> outputKeyBuilder, IComparer<TOutputKey> outputKeyComparer,
            IPartitionedStreamRecipient<TOutput> outputRecipient, int partitionCount, CancellationToken cancellationToken)
        {
            PartitionedStream<TOutput, TOutputKey> outputStream = new PartitionedStream<TOutput, TOutputKey>(
                partitionCount, outputKeyComparer, OrdinalIndexState);

            for (int i = 0; i < partitionCount; i++)
            {
                outputStream[i] = new HashJoinQueryOperatorEnumerator<TLeftInput, TLeftKey, IEnumerable<TRightInput>, TRightKey, TKey, TOutput, TOutputKey>(
                    leftHashStream[i], rightLookupBuilders[i], _resultSelector, outputKeyBuilder, cancellationToken);
            }

            outputRecipient.Receive(outputStream);
        }

        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //

        internal override IEnumerable<TOutput> AsSequentialQuery(CancellationToken token)
        {
            IEnumerable<TLeftInput> wrappedLeftChild = CancellableEnumerable.Wrap(LeftChild.AsSequentialQuery(token), token);
            IEnumerable<TRightInput> wrappedRightChild = CancellableEnumerable.Wrap(RightChild.AsSequentialQuery(token), token);

            return wrappedLeftChild
                .GroupJoin(
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
    /// Class to build a HashJoinHashLookup of right elements for use in GroupJoin operations.
    /// </summary>
    /// <typeparam name="TElement"></typeparam>
    /// <typeparam name="TOrderKey"></typeparam>
    /// <typeparam name="THashKey"></typeparam>
    internal class GroupJoinHashLookupBuilder<TElement, TOrderKey, THashKey> : HashLookupBuilder<IEnumerable<TElement>, int, THashKey>
    {
        private readonly QueryOperatorEnumerator<Pair<TElement, THashKey>, TOrderKey> _dataSource; // data source. For building.
        private readonly IEqualityComparer<THashKey> _keyComparer; // An optional key comparison object.

        internal GroupJoinHashLookupBuilder(QueryOperatorEnumerator<Pair<TElement, THashKey>, TOrderKey> dataSource, IEqualityComparer<THashKey> keyComparer)
        {
            Debug.Assert(dataSource != null);

            _dataSource = dataSource;
            _keyComparer = keyComparer;
        }

        public override HashJoinHashLookup<THashKey, IEnumerable<TElement>, int> BuildHashLookup(CancellationToken cancellationToken)
        {
            HashLookup<THashKey, ListChunk<TElement>> lookup = new HashLookup<THashKey, ListChunk<TElement>>(_keyComparer);
            GroupJoinBaseHashBuilder baseHashBuilder = new GroupJoinBaseHashBuilder(lookup);

            BuildBaseHashLookup(_dataSource, baseHashBuilder, cancellationToken);

            return new GroupJoinHashLookup(lookup);

        }

        /// <summary>
        /// Adds TElement values to a HashLookup of ListChunks. TOrderKey is ignored.
        /// </summary>
        private struct GroupJoinBaseHashBuilder : IBaseHashBuilder<TElement, TOrderKey>
        {
            private readonly HashLookup<THashKey, ListChunk<TElement>> _base;

            public GroupJoinBaseHashBuilder(HashLookup<THashKey, ListChunk<TElement>> baseLookup)
            {
                Debug.Assert(baseLookup != null);

                _base = baseLookup;
            }

            public bool Add(THashKey hashKey, TElement element, TOrderKey orderKey)
            {
                bool hasCollision = true;

                ListChunk<TElement> currentValue = default(ListChunk<TElement>);
                if (!_base.TryGetValue(hashKey, ref currentValue))
                {
                    const int INITIAL_CHUNK_SIZE = 2;
                    currentValue = new ListChunk<TElement>(INITIAL_CHUNK_SIZE);
                    _base.Add(hashKey, currentValue);
                    hasCollision = false;
                }

                currentValue.Add(element);

                return hasCollision;
            }
        }

        /// <summary>
        /// A wrapper for the HashLookup returned by GroupJoinHashLookupBuilder.
        /// 
        /// The order key is a dummy value since we are unordered.
        /// </summary>
        private class GroupJoinHashLookup : GroupJoinHashLookup<THashKey, TElement, ListChunk<TElement>, int>
        {
            const int OrderKey = unchecked((int)0xdeadbeef);

            internal GroupJoinHashLookup(HashLookup<THashKey, ListChunk<TElement>> lookup)
                : base(lookup)
            {
            }

            protected override int EmptyValueKey => OrderKey;

            protected override Pair<IEnumerable<TElement>, int> CreateValuePair(ListChunk<TElement> baseValue)
            {
                return new Pair<IEnumerable<TElement>, int>(baseValue, OrderKey);
            }
        }
    }

    /// <summary>
    /// Class to build a HashJoinHashLookup of ordered right elements for use in GroupJoin operations.
    /// </summary>
    /// <typeparam name="TElement"></typeparam>
    /// <typeparam name="TOrderKey"></typeparam>
    /// <typeparam name="THashKey"></typeparam>
    internal sealed class OrderedGroupJoinHashLookupBuilder<TElement, TOrderKey, THashKey> : HashLookupBuilder<IEnumerable<TElement>, Pair<bool, TOrderKey>, THashKey>
    {
        private readonly QueryOperatorEnumerator<Pair<TElement, THashKey>, TOrderKey> _dataSource; // data source. For building.
        private readonly IEqualityComparer<THashKey> _keyComparer; // An optional key comparison object.
        private readonly IComparer<TOrderKey> _orderKeyComparer;

        internal OrderedGroupJoinHashLookupBuilder(
            QueryOperatorEnumerator<Pair<TElement, THashKey>, TOrderKey> dataSource,
            IEqualityComparer<THashKey> keyComparer,
            IComparer<TOrderKey> orderKeyComparer)
        {
            Debug.Assert(dataSource != null);

            _dataSource = dataSource;
            _keyComparer = keyComparer;
            _orderKeyComparer = orderKeyComparer;
        }

        public override HashJoinHashLookup<THashKey, IEnumerable<TElement>, Pair<bool, TOrderKey>> BuildHashLookup(CancellationToken cancellationToken)
        {
            HashLookup<THashKey, GroupKeyData> lookup = new HashLookup<THashKey, GroupKeyData>(_keyComparer);
            OrderedGroupJoinBaseHashBuilder baseHashBuilder = new OrderedGroupJoinBaseHashBuilder(lookup, _orderKeyComparer);

            BuildBaseHashLookup(_dataSource, baseHashBuilder, cancellationToken);

            for (int i = 0; i < lookup.Count; i++)
            {
                lookup[i].Value._grouping.DoneAdding();
            }

            return new OrderedGroupJoinHashLookup(lookup);

        }

        /// <summary>
        /// Adds TElement values to a HashLookup of GroupKeyData. 
        /// TOrderKey is used for both ordering the elements that have the same hashKey
        /// and also for providing an order key for the resulting list.
        /// </summary>
        /// <remarks>
        /// The least order key in the list is chosen to represent the list
        /// </remarks>
        private struct OrderedGroupJoinBaseHashBuilder : IBaseHashBuilder<TElement, TOrderKey>
        {
            private readonly HashLookup<THashKey, GroupKeyData> _base;
            private readonly IComparer<TOrderKey> _orderKeyComparer;

            public OrderedGroupJoinBaseHashBuilder(
                HashLookup<THashKey, GroupKeyData> baseLookup,
                IComparer<TOrderKey> orderKeyComparer)
            {
                Debug.Assert(baseLookup != null);

                _base = baseLookup;
                _orderKeyComparer = orderKeyComparer;
            }

            public bool Add(THashKey hashKey, TElement element, TOrderKey orderKey)
            {
                bool hasCollision = true;

                GroupKeyData currentValue = default(GroupKeyData);
                if (!_base.TryGetValue(hashKey, ref currentValue))
                {
                    currentValue = new GroupKeyData(orderKey, hashKey, _orderKeyComparer);
                    _base.Add(hashKey, currentValue);
                    hasCollision = false;
                }

                currentValue._grouping.Add(element, orderKey);
                if (_orderKeyComparer.Compare(orderKey, currentValue._orderKey) < 0)
                {
                    currentValue._orderKey = orderKey;
                }

                return hasCollision;
            }
        }

        /// <summary>
        /// A wrapper for the HashLookup returned by OrderedGroupJoinHashLookupBuilder.
        /// 
        /// The order key is wrapped so that empty lists can be treated as less than all non-empty lists.
        /// </summary>
        private class OrderedGroupJoinHashLookup : GroupJoinHashLookup<THashKey, TElement, GroupKeyData, Pair<bool, TOrderKey>>
        {
            internal OrderedGroupJoinHashLookup(HashLookup<THashKey, GroupKeyData> lookup)
                : base(lookup)
            {
            }

            protected override Pair<bool, TOrderKey> EmptyValueKey => default(Pair<bool, TOrderKey>);

            protected override Pair<IEnumerable<TElement>, Pair<bool, TOrderKey>> CreateValuePair(GroupKeyData baseValue)
            {
                return new Pair<IEnumerable<TElement>, Pair<bool, TOrderKey>>(baseValue._grouping, Wrap(baseValue._orderKey));
            }

            private Pair<bool, TOrderKey> Wrap(TOrderKey orderKey)
            {
                return new Pair<bool, TOrderKey>(true, orderKey);
            }
        }

        /// <summary>
        /// A structure to hold both the elements that match a hash key and an order key for the grouping.
        /// </summary>
        private class GroupKeyData
        {
            internal TOrderKey _orderKey;
            internal OrderedGroupByGrouping<THashKey, TOrderKey, TElement> _grouping;

            internal GroupKeyData(TOrderKey orderKey, THashKey hashKey, IComparer<TOrderKey> orderComparer)
            {
                _orderKey = orderKey;
                _grouping = new OrderedGroupByGrouping<THashKey, TOrderKey, TElement>(hashKey, orderComparer);
            }
        }
    }

    /// <summary>
    /// A base wrapper for the HashLookup returned by GroupJoinHashLookupBuilder and OrderedGroupJoinHashLookupBuilder.
    /// 
    /// Since GroupJoin operations always match, if no matching elements exist, an empty enumerable is returned.
    /// </summary>
    internal abstract class GroupJoinHashLookup<THashKey, TElement, TBaseElement, TOrderKey> : HashJoinHashLookup<THashKey, IEnumerable<TElement>, TOrderKey>
    {
        private readonly HashLookup<THashKey, TBaseElement> _base;

        internal GroupJoinHashLookup(HashLookup<THashKey, TBaseElement> baseLookup)
        {
            Debug.Assert(baseLookup != null);

            _base = baseLookup;
        }

        public override bool TryGetValue(THashKey key, ref HashLookupValueList<IEnumerable<TElement>, TOrderKey> value)
        {
            Pair<IEnumerable<TElement>, TOrderKey> valueList = GetValueList(key);
            value = new HashLookupValueList<IEnumerable<TElement>, TOrderKey>(valueList.First, valueList.Second);
            return true;
        }

        private Pair<IEnumerable<TElement>, TOrderKey> GetValueList(THashKey key)
        {
            TBaseElement baseValue = default(TBaseElement);
            if (_base.TryGetValue(key, ref baseValue))
            {
                return CreateValuePair(baseValue);
            }
            else
            {
                return new Pair<IEnumerable<TElement>, TOrderKey>(ParallelEnumerable.Empty<TElement>(), EmptyValueKey);
            }
        }

        protected abstract Pair<IEnumerable<TElement>, TOrderKey> CreateValuePair(TBaseElement baseValue);
        protected abstract TOrderKey EmptyValueKey { get; }
    }
}
