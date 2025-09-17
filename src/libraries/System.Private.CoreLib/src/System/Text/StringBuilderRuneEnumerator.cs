// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Text
{
    // An enumerator for retrieving System.Text.Rune instances from a System.Text.StringBuilder.
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

        public readonly Rune Current => _current;

        public readonly StringBuilderRuneEnumerator GetEnumerator() => this;

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

        readonly object? IEnumerator.Current => _current;

        readonly void IDisposable.Dispose()
        {
            // no-op
        }

        readonly IEnumerator IEnumerable.GetEnumerator() => this;

        readonly IEnumerator<Rune> IEnumerable<Rune>.GetEnumerator() => this;

        void IEnumerator.Reset()
        {
            _current = default;
            _nextIndex = 0;
        }
    }
}
