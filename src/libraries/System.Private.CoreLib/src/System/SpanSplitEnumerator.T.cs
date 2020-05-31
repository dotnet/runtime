// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    /// <summary>
    /// <see cref="System.SpanSplitEnumerator{T}"/> allows for enumeration of each element within a <see cref="System.ReadOnlySpan{T}"/>
    /// that has been split using a provided separator.
    /// </summary>
    public ref struct SpanSplitEnumerator<T> where T : IEquatable<T>
    {
        private readonly ReadOnlySpan<T> _buffer;
        private readonly ReadOnlySpan<T> _separatorSequence;
        private readonly T _separator;
        private readonly bool _isSequence;
        private readonly int _separatorLength;
        private int _offset;
        private int _index;

        /// <summary>
        /// Returns an enumerator that allows for iteration over the split span.
        /// </summary>
        /// <returns>Returns a <see cref="System.SpanSplitEnumerator{T}"/> that can be used to iterate over the split span.</returns>
        public SpanSplitEnumerator<T> GetEnumerator() => this;

        /// <summary>
        /// Returns the current element of the enumeration.
        /// </summary>
        /// <returns>Returns a <see cref="System.Range"/> struct that defines the bounds of the current element withing the source span.</returns>
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

        /// <summary>
        /// Advances the enumerator to the next element of the split span.
        /// </summary>
        /// <returns>Returns a bool indicating whether an element is available.</returns>
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
