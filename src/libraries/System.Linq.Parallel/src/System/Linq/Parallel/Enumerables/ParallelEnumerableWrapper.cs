// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ParallelEnumerableWrapper.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq.Parallel
{
    /// <summary>
    /// A simple implementation of the ParallelQuery{object} interface which wraps an
    /// underlying IEnumerable, such that it can be used in parallel queries.
    /// </summary>
    internal class ParallelEnumerableWrapper : ParallelQuery<object>
    {
        private readonly IEnumerable _source; // The wrapped enumerable object.

        //-----------------------------------------------------------------------------------
        // Instantiates a new wrapper object.
        //

        internal ParallelEnumerableWrapper(Collections.IEnumerable source)
            : base(QuerySettings.Empty)
        {
            Debug.Assert(source != null);
            _source = source;
        }

        internal override IEnumerator GetEnumeratorUntyped()
        {
            return _source.GetEnumerator();
        }

        public override IEnumerator<object> GetEnumerator()
        {
            return new EnumerableWrapperWeakToStrong(_source).GetEnumerator();
        }
    }

    /// <summary>
    /// A simple implementation of the ParallelQuery{T} interface which wraps an
    /// underlying IEnumerable{T}, such that it can be used in parallel queries.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class ParallelEnumerableWrapper<T> : ParallelQuery<T>
    {
        private readonly IEnumerable<T> _wrappedEnumerable; // The wrapped enumerable object.

        //-----------------------------------------------------------------------------------
        // Instantiates a new wrapper object.
        //
        // Arguments:
        //     wrappedEnumerable   - the underlying enumerable object being wrapped
        //
        // Notes:
        //     The analysisOptions and degreeOfParallelism settings are optional.  Passing null
        //     indicates that the system defaults should be used instead.
        //

        internal ParallelEnumerableWrapper(IEnumerable<T> wrappedEnumerable)
            : base(QuerySettings.Empty)
        {
            Debug.Assert(wrappedEnumerable != null, "wrappedEnumerable must not be null.");

            _wrappedEnumerable = wrappedEnumerable;
        }

        //-----------------------------------------------------------------------------------
        // Retrieves the wrapped enumerable object.
        //

        internal IEnumerable<T> WrappedEnumerable
        {
            get { return _wrappedEnumerable; }
        }

        //-----------------------------------------------------------------------------------
        // Implementations of GetEnumerator that just delegate to the wrapped enumerable.
        //

        public override IEnumerator<T> GetEnumerator()
        {
            Debug.Assert(_wrappedEnumerable != null);
            return _wrappedEnumerable.GetEnumerator();
        }
    }
}
