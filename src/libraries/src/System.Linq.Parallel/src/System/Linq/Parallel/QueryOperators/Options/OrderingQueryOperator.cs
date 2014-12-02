// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// OrderingQueryOperator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// Represents operators AsOrdered and AsUnordered. In the current implementation, it
    /// simply turns on preservation globally in the query. 
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    internal sealed class OrderingQueryOperator<TSource> : QueryOperator<TSource>
    {
        // Turns on order (AsOrdered) or turns off order (AsUnordered)
        private bool _orderOn;
        private QueryOperator<TSource> _child;
        private OrdinalIndexState _ordinalIndexState;

        public OrderingQueryOperator(QueryOperator<TSource> child, bool orderOn)
            : base(orderOn, child.SpecifiedQuerySettings)
        {
            _child = child;
            _ordinalIndexState = _child.OrdinalIndexState;
            _orderOn = orderOn;
        }

        internal override QueryResults<TSource> Open(QuerySettings settings, bool preferStriping)
        {
            return _child.Open(settings, preferStriping);
        }

        internal override IEnumerator<TSource> GetEnumerator(ParallelMergeOptions? mergeOptions, bool suppressOrderPreservation)
        {
            ScanQueryOperator<TSource> childAsScan = _child as ScanQueryOperator<TSource>;
            if (childAsScan != null)
            {
                return childAsScan.Data.GetEnumerator();
            }
            return base.GetEnumerator(mergeOptions, suppressOrderPreservation);
        }

        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //

        internal override IEnumerable<TSource> AsSequentialQuery(CancellationToken token)
        {
            return _child.AsSequentialQuery(token);
        }


        //---------------------------------------------------------------------------------------
        // Whether this operator performs a premature merge that would not be performed in
        // a similar sequential operation (i.e., in LINQ to Objects).
        //

        internal override bool LimitsParallelism
        {
            get { return _child.LimitsParallelism; }
        }

        internal override OrdinalIndexState OrdinalIndexState
        {
            get { return _ordinalIndexState; }
        }
    }
}