// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    public ref struct SpanSplitEnumerator<T> where T : IEquatable<T>
    {
        private readonly ReadOnlySpan<T> _buffer;
        private readonly ReadOnlySpan<T> _separatorSequence;
        private readonly T _separator;
        private readonly bool _isSequence;
        private readonly int _separatorLength;
        private int _offset;
        private int _index;

        public SpanSplitEnumerator<T> GetEnumerator() => this;
        public readonly Range Current => new Range(_offset, _offset + _index - _separatorLength);

        internal SpanSplitEnumerator(ReadOnlySpan<T> buffer, ReadOnlySpan<T> separator)
        {
            _buffer = buffer;
            _separatorSequence = separator;
            _separator = default!;
            _isSequence = true;
            (_index, _offset) = (0, 0);
            _separatorLength = _separatorSequence.Length;
        }

        internal SpanSplitEnumerator(ReadOnlySpan<T> buffer, T separator)
        {
            _buffer = buffer;
            _separator = separator;
            _separatorSequence = default;
            _isSequence = false;
            (_index, _offset) = (0, 0);
            _separatorLength = 1;
        }

        public bool MoveNext()
        {
            _offset += _index;
            if (_offset > _buffer.Length) { return false; }
            var slice = _buffer.Slice(_offset);

            var nextIdx = _isSequence ? slice.IndexOf(_separatorSequence) : slice.IndexOf(_separator);
            _index = (nextIdx != -1 ? nextIdx : slice.Length) + _separatorLength;
            return true;
        }
    }
}
