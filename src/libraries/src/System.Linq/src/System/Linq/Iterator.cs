// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        internal abstract class Iterator<TSource> : IEnumerable<TSource>, IEnumerator<TSource>
        {
            private readonly int _threadId;
            internal int _state;
            internal TSource _current;

            protected Iterator()
            {
                _threadId = Environment.CurrentManagedThreadId;
            }

            public TSource Current
            {
                get { return _current; }
            }

            public abstract Iterator<TSource> Clone();

            public virtual void Dispose()
            {
                _current = default(TSource);
                _state = -1;
            }

            public IEnumerator<TSource> GetEnumerator()
            {
                Iterator<TSource> enumerator = _state == 0 && _threadId == Environment.CurrentManagedThreadId ? this : Clone();
                enumerator._state = 1;
                return enumerator;
            }

            public abstract bool MoveNext();

            public virtual IEnumerable<TResult> Select<TResult>(Func<TSource, TResult> selector)
            {
                return new SelectEnumerableIterator<TSource, TResult>(this, selector);
            }

            public virtual IEnumerable<TSource> Where(Func<TSource, bool> predicate)
            {
                return new WhereEnumerableIterator<TSource>(this, predicate);
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            void IEnumerator.Reset()
            {
                throw Error.NotSupported();
            }
        }
    }
}
