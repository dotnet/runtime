// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    public static partial class MemoryExtensions
    {
        public static SpanSplitEnumerator<char> Split(this ReadOnlySpan<char> span)
            => new SpanSplitEnumerator<char>(span, ' ');

        public static SpanSplitEnumerator<char> Split(this ReadOnlySpan<char> span, char separator)
            => new SpanSplitEnumerator<char>(span, separator);

        public static SpanSplitEnumerator<char> Split(this ReadOnlySpan<char> span, string separator)
            => new SpanSplitEnumerator<char>(span, separator);
    }

    public ref struct SpanSplitEnumerator<T> where T : IEquatable<T>
    {
        private readonly ReadOnlySpan<T> _sequence;
        private readonly ReadOnlySpan<T> _separators;
        private readonly T _separator;
        private readonly bool _isSequence;
        private readonly int _separatorLength;
        private int _offset;
        private int _index;

        public SpanSplitEnumerator<T> GetEnumerator() => this;
        public readonly Range Current => new Range(_offset, _offset + _index - _separatorLength);

        internal SpanSplitEnumerator(ReadOnlySpan<T> span, ReadOnlySpan<T> separators)
        {
            _sequence = span;
            _separators = separators;
            _separator = default;
            _isSequence = true;
            (_index, _offset) = (0, 0);
            _separatorLength = _isSequence ? _separators.Length : 1;
        }

        internal SpanSplitEnumerator(ReadOnlySpan<T> span, T separator)
        {
            _sequence = span;
            _separator = separator;
            _separators = default;
            _isSequence = false;
            (_index, _offset) = (0, 0);
            _separatorLength = _isSequence ? _separators.Length : 1;
        }

        public bool MoveNext()
        {
            if ((_offset += _index) > _sequence.Length) { return false; }
            var slice = _sequence.Slice(_offset);

            var nextIdx = _isSequence ? slice.IndexOf(_separators) : slice.IndexOf(_separator);
            _index = (nextIdx != -1 ? nextIdx : slice.Length) + _separatorLength;
            return true;
        }
    }
}
