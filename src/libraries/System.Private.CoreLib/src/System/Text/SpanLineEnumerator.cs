// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text
{
    /// <summary>
    /// Enumerates the lines of a <see cref="ReadOnlySpan{Char}"/>.
    /// </summary>
    /// <remarks>
    /// To get an instance of this type, use <see cref="MemoryExtensions.EnumerateLines(ReadOnlySpan{char})"/>.
    /// </remarks>
    public ref struct SpanLineEnumerator
    {
        private ReadOnlySpan<char> _remaining;
        private ReadOnlySpan<char> _current;
        private bool _isEnumeratorActive;

        internal SpanLineEnumerator(ReadOnlySpan<char> buffer)
        {
            _remaining = buffer;
            _current = default;
            _isEnumeratorActive = true;
        }

        /// <summary>
        /// Gets the line at the current position of the enumerator.
        /// </summary>
        public ReadOnlySpan<char> Current => _current;

        /// <summary>
        /// Returns this instance as an enumerator.
        /// </summary>
        public SpanLineEnumerator GetEnumerator() => this;

        /// <summary>
        /// Advances the enumerator to the next line of the span.
        /// </summary>
        /// <returns>
        /// True if the enumerator successfully advanced to the next line; false if
        /// the enumerator has advanced past the end of the span.
        /// </returns>
        public bool MoveNext()
        {
            if (!_isEnumeratorActive)
            {
                return false; // EOF previously reached or enumerator was never initialized
            }

            int idx = string.IndexOfNewlineChar(_remaining, out int stride);
            if (idx >= 0)
            {
                _current = _remaining.Slice(0, idx);
                _remaining = _remaining.Slice(idx + stride);
            }
            else
            {
                // We've reached EOF, but we still need to return 'true' for this final
                // iteration so that the caller can query the Current property once more.

                _current = _remaining;
                _remaining = default;
                _isEnumeratorActive = false;
            }

            return true;
        }
    }
}
