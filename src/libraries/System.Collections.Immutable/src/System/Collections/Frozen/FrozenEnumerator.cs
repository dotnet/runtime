// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections.Immutable
{
    /// <summary>
    /// Enumerates the entries of a frozen collection.
    /// </summary>
    /// <typeparam name="T">The types of the collection's entries.</typeparam>
    public struct FrozenEnumerator<T> : IEnumerator<T>
    {
        private readonly T[] _entries;
        private int _index;

        internal FrozenEnumerator(T[] entries)
        {
            _entries = entries;
            _index = -1;
        }

        /// <summary>
        /// Gets the current value held by the enumerator.
        /// </summary>
        public readonly T Current
        {
            get
            {
                if (_index >= 0)
                {
                    return _entries[_index];
                }

                return Throw();
            }
        }

        /// <summary>
        /// Dispose this object.
        /// </summary>
        void IDisposable.Dispose()
        {
            // nothing to do
        }

        /// <summary>
        /// Advances the enumerator to the next item in the collection.
        /// </summary>
        /// <returns><see langword="true" /> if the enumerator was successfully advanced to the next item; <see langword="false" /> if the enumerator has passed the end of the collection.</returns>
        public bool MoveNext()
        {
            if (_index < _entries.Length - 1)
            {
                _index++;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Resets the enumerator to its initial state.
        /// </summary>
        void IEnumerator.Reset() => _index = -1;

        /// <summary>
        /// Gets the current value held by the enumerator.
        /// </summary>
        object IEnumerator.Current => Current!;

        // keep this separate to allow inlining of the Current property
        private static T Throw()
        {
            throw new InvalidOperationException("Call MoveNext() before reading the Current property.");
        }
    }
}
