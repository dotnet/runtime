// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        /// <summary>
        /// A base class for enumerables that are loaded on-demand.
        /// </summary>
        /// <typeparam name="TSource">The type of each item to yield.</typeparam>
        /// <remarks>
        /// - The value of an iterator is immutable; when re-enumerated, it should always yield
        ///   the same items with respect to its inputs.
        /// - However, an iterator also serves as its own enumerator, so the state of an iterator
        ///   may change as it is being enumerated.
        /// - Hence, state that is relevant to an iterator's value should be kept in readonly fields.
        ///   State that is relevant to an iterator's enumeration (such as the currently yielded item)
        ///   should be kept in non-readonly fields.
        /// </remarks>
        internal abstract class Iterator<TSource> : IEnumerable<TSource>, IEnumerator<TSource>
        {
            private readonly int _threadId;
            internal int _state;
            internal TSource _current;

            /// <summary>
            /// Initializes a new instance of the <see cref="Iterator{TSource}"/> class.
            /// </summary>
            protected Iterator()
            {
                _threadId = Environment.CurrentManagedThreadId;
            }

            /// <summary>
            /// The item currently yielded by this iterator.
            /// </summary>
            public TSource Current => _current;

            /// <summary>
            /// Makes a shallow copy of this iterator.
            /// </summary>
            /// <remarks>
            /// This method is called if <see cref="GetEnumerator"/> is called more than once.
            /// </remarks>
            public abstract Iterator<TSource> Clone();

            /// <summary>
            /// Puts this iterator in a state whereby no further enumeration will take place.
            /// </summary>
            public virtual void Dispose()
            {
                _current = default(TSource);
                _state = -1;
            }

            /// <summary>
            /// Gets the enumerator used to yield values from this iterator.
            /// </summary>
            public IEnumerator<TSource> GetEnumerator()
            {
                Iterator<TSource> enumerator = _state == 0 && _threadId == Environment.CurrentManagedThreadId ? this : Clone();
                enumerator._state = 1;
                return enumerator;
            }

            /// <summary>
            /// Retrieves the next item in this iterator and yields it via <see cref="Current"/>.
            /// </summary>
            /// <returns><c>true</c> if there was another value to be yielded; otherwise, <c>false</c>.</returns>
            public abstract bool MoveNext();

            /// <summary>
            /// Returns an enumerable that maps each item in this iterator based on a selector.
            /// </summary>
            /// <typeparam name="TResult">The type of the mapped items.</typeparam>
            /// <param name="selector">The selector used to map each item.</param>
            public virtual IEnumerable<TResult> Select<TResult>(Func<TSource, TResult> selector)
            {
                return new SelectEnumerableIterator<TSource, TResult>(this, selector);
            }

            /// <summary>
            /// Returns an enumerable that filters each item in this iterator based on a predicate.
            /// </summary>
            /// <param name="predicate">The predicate used to filter each item.</param>
            public virtual IEnumerable<TSource> Where(Func<TSource, bool> predicate)
            {
                return new WhereEnumerableIterator<TSource>(this, predicate);
            }

            object IEnumerator.Current => Current;

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void IEnumerator.Reset()
            {
                throw Error.NotSupported();
            }
        }
    }
}
