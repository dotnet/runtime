// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Text
{
    // An enumerator for retrieving System.Text.Rune instances from a ROS<char>.
    // Methods are pattern-matched by compiler to allow using foreach pattern.
    public ref struct SpanRuneEnumerator : IEnumerator<Rune>
    {
        private ReadOnlySpan<char> _remaining;
        private Rune _current;

        internal SpanRuneEnumerator(ReadOnlySpan<char> buffer)
        {
            _remaining = buffer;
            _current = default;
        }

        /// <summary>
        /// Gets the rune at the current position of the enumerator.
        /// </summary>
        public Rune Current => _current;

        /// <summary>
        /// Returns this instance as an enumerator.
        /// </summary>
        public SpanRuneEnumerator GetEnumerator() => this;

        /// <summary>
        /// Advances the enumerator to the next rune of the span.
        /// </summary>
        /// <returns>
        /// True if the enumerator successfully advanced to the next rune; false if
        /// the enumerator has advanced past the end of the span.
        /// </returns>
        public bool MoveNext()
        {
            if (_remaining.IsEmpty)
            {
                // reached the end of the buffer
                _current = default;
                return false;
            }

            int scalarValue = Rune.ReadFirstRuneFromUtf16Buffer(_remaining);
            if (scalarValue < 0)
            {
                // replace invalid sequences with U+FFFD
                scalarValue = Rune.ReplacementChar.Value;
            }

            // In UTF-16 specifically, invalid sequences always have length 1, which is the same
            // length as the replacement character U+FFFD. This means that we can always bump the
            // next index by the current scalar's UTF-16 sequence length. This optimization is not
            // generally applicable; for example, enumerating scalars from UTF-8 cannot utilize
            // this same trick.

            _current = Rune.UnsafeCreate((uint)scalarValue);
            _remaining = _remaining.Slice(_current.Utf16SequenceLength);
            return true;
        }

        /// <summary>
        /// Gets the rune at the current position of the enumerator.
        /// </summary>
        object IEnumerator.Current => Current;

        void IEnumerator.Reset() => throw new NotSupportedException();
        void IDisposable.Dispose() { }
    }
}
