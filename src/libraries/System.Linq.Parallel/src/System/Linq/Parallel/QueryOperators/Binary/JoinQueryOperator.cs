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
                WrapPartitionedStreamHelper<TLeftKey, TRightKey>(
                    ExchangeUtilities.HashRepartitionOrdered(leftStream, _leftKeySelector, _keyComparer, null, settings.CancellationState.MergedCancellationToken),
                    rightStream, outputRecipient, settings.CancellationState.MergedCancellationToken);
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
            int partitionCount = leftHashStream.PartitionCount;
            PartitionedStream<Pair<TRightInput, TKey>, int> rightHashStream = ExchangeUtilities.HashRepartition(
                rightPartitionedStream, _rightKeySelector, _keyComparer, null, cancellationToken);

            PartitionedStream<TOutput, TLeftKey> outputStream = new PartitionedStream<TOutput, TLeftKey>(
                partitionCount, leftHashStream.KeyComparer, OrdinalIndexState);

            for (int i = 0; i < partitionCount; i++)
            {
                outputStream[i] = new HashJoinQueryOperatorEnumerator<TLeftInput, TLeftKey, TRightInput, TKey, TOutput>(
                    leftHashStream[i], rightHashStream[i], _resultSelector, null, _keyComparer, cancellationToken);
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
}
