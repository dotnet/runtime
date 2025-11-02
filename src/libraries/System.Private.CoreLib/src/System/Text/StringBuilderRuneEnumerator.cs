// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Text
{
    /// <summary>
    /// An enumerator for retrieving <see cref="Rune"/> instances from a <see cref="StringBuilder"/>.
    /// </summary>
    public struct StringBuilderRuneEnumerator : IEnumerable<Rune>, IEnumerator<Rune>
    {
        private readonly StringBuilder _stringBuilder;
        private Rune _current;
        private int _nextIndex;

        internal StringBuilderRuneEnumerator(StringBuilder value)
        {
            _stringBuilder = value;
            _current = default;
            _nextIndex = 0;
        }

        /// <summary>
        /// Gets the <see cref="Rune"/> at the current position of the enumerator.
        /// </summary>
        public readonly Rune Current => _current;

        /// <summary>
        /// Returns the current enumerator instance.
        /// </summary>
        /// <returns>The current enumerator instance.</returns>
        public readonly StringBuilderRuneEnumerator GetEnumerator() => this;

        /// <summary>
        /// Advances the enumerator to the next <see cref="Rune"/> of the builder.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the enumerator successfully advanced to the next item;
        /// <see langword="false"/> if the end of the builder has been reached.
        /// </returns>
        public bool MoveNext()
        {
            if ((uint)_nextIndex >= _stringBuilder.Length)
            {
                // reached the end of the string
                _current = default;
                return false;
            }

            if (!_stringBuilder.TryGetRuneAt(_nextIndex, out _current))
            {
                // replace invalid sequences with U+FFFD
                _current = Rune.ReplacementChar;
            }

            // In UTF-16 specifically, invalid sequences always have length 1, which is the same
            // length as the replacement character U+FFFD. This means that we can always bump the
            // next index by the current scalar's UTF-16 sequence length. This optimization is not
            // generally applicable; for example, enumerating scalars from UTF-8 cannot utilize
            // this same trick.

            _nextIndex += _current.Utf16SequenceLength;
            return true;
        }

        /// <summary>
        /// Gets the <see cref="Rune"/> at the current position of the enumerator.
        /// </summary>
        readonly object? IEnumerator.Current => _current;

        /// <summary>
        /// Releases all resources used by the current <see cref="StringBuilderRuneEnumerator"/> instance.
        /// </summary>
        /// <remarks>
        /// This method performs no operation and produces no side effects.
        /// </remarks>
        readonly void IDisposable.Dispose()
        {
            // no-op
        }

        /// <summary>
        /// Returns the current enumerator instance.
        /// </summary>
        /// <returns>The current enumerator instance.</returns>
        readonly IEnumerator IEnumerable.GetEnumerator() => this;

        /// <summary>
        /// Returns the current enumerator instance.
        /// </summary>
        /// <returns>The current enumerator instance.</returns>
        readonly IEnumerator<Rune> IEnumerable<Rune>.GetEnumerator() => this;

        /// <summary>
        /// Resets the current <see cref="StringBuilderRuneEnumerator"/> instance to the beginning of the builder.
        /// </summary>
        void IEnumerator.Reset()
        {
            _current = default;
            _nextIndex = 0;
        }
    }
}
