// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        /// <summary>
        /// A base class for async enumerables that are loaded on-demand.
        /// </summary>
        /// <typeparam name="TSource">The type of each item to yield.</typeparam>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>
        /// The value of an async iterator is immutable; the operation it represents cannot be changed.
        /// </description></item>
        /// <item><description>
        /// However, an async iterator also serves as its own enumerator, so the state of an async iterator
        /// may change as it is being enumerated.
        /// </description></item>
        /// <item><description>
        /// Hence, state that is relevant to an async iterator's value should be kept in readonly fields.
        /// State that is relevant to an async iterator's enumeration (such as the currently yielded item)
        /// should be kept in non-readonly fields.
        /// </description></item>
        /// </list>
        /// </remarks>
        private abstract class AsyncIterator<TSource> : IAsyncEnumerable<TSource>, IAsyncEnumerator<TSource>
        {
            private readonly int _threadId = Environment.CurrentManagedThreadId;

            private protected int _state;
            private protected TSource _current = default!;
            private protected CancellationToken _cancellationToken;

            /// <summary>
            /// The item currently yielded by this iterator.
            /// </summary>
            public TSource Current => _current;

            /// <summary>
            /// Makes a shallow copy of this iterator.
            /// </summary>
            /// <remarks>
            /// This method is called if <see cref="GetAsyncEnumerator"/> is called more than once.
            /// </remarks>
            private protected abstract AsyncIterator<TSource> Clone();

            /// <summary>
            /// Puts this iterator in a state whereby no further enumeration will take place.
            /// </summary>
            /// <remarks>
            /// Derived classes should override this method if necessary to clean up any
            /// mutable state they hold onto (for example, calling DisposeAsync on other enumerators).
            /// </remarks>
            public virtual ValueTask DisposeAsync()
            {
                _current = default!;
                _state = -1;
                return default;
            }

            /// <summary>
            /// Gets the enumerator used to yield values from this iterator.
            /// </summary>
            /// <remarks>
            /// If <see cref="GetAsyncEnumerator"/> is called for the first time on the same thread
            /// that created this iterator, the result will be this iterator. Otherwise, the result
            /// will be a shallow copy of this iterator.
            /// </remarks>
            public IAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                AsyncIterator<TSource> enumerator = _state == 0 && _threadId == Environment.CurrentManagedThreadId ? this : Clone();
                enumerator._state = 1;
                enumerator._cancellationToken = cancellationToken;
                return enumerator;
            }

            /// <summary>
            /// Retrieves the next item in this iterator and yields it via <see cref="Current"/>.
            /// </summary>
            /// <returns><c>true</c> if there was another value to be yielded; otherwise, <c>false</c>.</returns>
            public abstract ValueTask<bool> MoveNextAsync();
        }
    }
}
