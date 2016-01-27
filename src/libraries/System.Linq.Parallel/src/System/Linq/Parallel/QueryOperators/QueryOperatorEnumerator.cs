// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// QueryOperatorEnumerator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq.Parallel
{
    /// <summary>
    /// A common enumerator type that unifies all query operator enumerators. 
    /// </summary>
    /// <typeparam name="TElement"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    internal abstract class QueryOperatorEnumerator<TElement, TKey>
    {
        // Moves the position of the enumerator forward by one, and simultaneously returns
        // the (new) current element and key. If empty, false is returned.
        internal abstract bool MoveNext(ref TElement currentElement, ref TKey currentKey);

        // Standard implementation of the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            // This is a no-op by default.  Subclasses can override.
        }

        internal virtual void Reset()
        {
            // This is a no-op by default.  Subclasses can override.
        }

        //-----------------------------------------------------------------------------------
        // A simple way to turn a query operator enumerator into a "classic" one.
        //

        internal IEnumerator<TElement> AsClassicEnumerator()
        {
            return new QueryOperatorClassicEnumerator(this);
        }

        class QueryOperatorClassicEnumerator : IEnumerator<TElement>
        {
            private QueryOperatorEnumerator<TElement, TKey> _operatorEnumerator;
            private TElement _current;

            internal QueryOperatorClassicEnumerator(QueryOperatorEnumerator<TElement, TKey> operatorEnumerator)
            {
                Debug.Assert(operatorEnumerator != null);
                _operatorEnumerator = operatorEnumerator;
            }

            public bool MoveNext()
            {
                TKey keyUnused = default(TKey);
                return _operatorEnumerator.MoveNext(ref _current, ref keyUnused);
            }

            public TElement Current
            {
                get { return _current; }
            }

            object IEnumerator.Current
            {
                get { return _current; }
            }

            public void Dispose()
            {
                _operatorEnumerator.Dispose();
                _operatorEnumerator = null;
            }

            public void Reset()
            {
                _operatorEnumerator.Reset();
            }
        }
    }
}
