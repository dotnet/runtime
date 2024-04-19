// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// OrderedParallelQuery.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Parallel;
using System.Text;

namespace System.Linq
{
    /// <summary>
    /// Represents a sorted, parallel sequence.
    /// </summary>
    public class OrderedParallelQuery<TSource> : ParallelQuery<TSource>
    {
        private readonly QueryOperator<TSource> _sortOp;

        internal OrderedParallelQuery(QueryOperator<TSource> sortOp)
            : base(sortOp.SpecifiedQuerySettings)
        {
            _sortOp = sortOp;
            Debug.Assert(sortOp is IOrderedEnumerable<TSource>);
        }

        internal QueryOperator<TSource> SortOperator
        {
            get { return _sortOp; }
        }

        internal IOrderedEnumerable<TSource> OrderedEnumerable
        {
            get { return (IOrderedEnumerable<TSource>)_sortOp; }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the sequence.
        /// </summary>
        /// <returns>An enumerator that iterates through the sequence.</returns>
        public override IEnumerator<TSource> GetEnumerator()
        {
            return _sortOp.GetEnumerator();
        }
    }
}
