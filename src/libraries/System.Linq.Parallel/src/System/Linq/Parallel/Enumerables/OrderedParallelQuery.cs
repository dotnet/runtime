// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// OrderedParallelQuery.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq.Parallel;
using System.Diagnostics;

namespace System.Linq
{
    /// <summary>
    /// Represents a sorted, parallel sequence.
    /// </summary>
    public class OrderedParallelQuery<TSource> : ParallelQuery<TSource>
    {
        private QueryOperator<TSource> _sortOp;

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
