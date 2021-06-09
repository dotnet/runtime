// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// QueryExecutionOption.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// Represents operators that set various query execution options.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    internal sealed class QueryExecutionOption<TSource> : QueryOperator<TSource>
    {
        private readonly QueryOperator<TSource> _child;
        private readonly OrdinalIndexState _indexState;

        internal QueryExecutionOption(QueryOperator<TSource> source, QuerySettings settings)
            : base(source.OutputOrdered, settings.Merge(source.SpecifiedQuerySettings))
        {
            _child = source;
            _indexState = _child.OrdinalIndexState;
        }

        internal override QueryResults<TSource> Open(QuerySettings settings, bool preferStriping)
        {
            return _child.Open(settings, preferStriping);
        }

        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //

        internal override IEnumerable<TSource> AsSequentialQuery(CancellationToken token)
        {
            return _child.AsSequentialQuery(token);
        }

        internal override OrdinalIndexState OrdinalIndexState
        {
            get { return _indexState; }
        }


        //---------------------------------------------------------------------------------------
        // Whether this operator performs a premature merge that would not be performed in
        // a similar sequential operation (i.e., in LINQ to Objects).
        //

        internal override bool LimitsParallelism
        {
            get { return _child.LimitsParallelism; }
        }
    }
}
