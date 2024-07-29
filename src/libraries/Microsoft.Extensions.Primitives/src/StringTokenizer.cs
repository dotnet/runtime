// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Extensions.Primitives
{
    /// <summary>
    /// Tokenizes a <see cref="string"/> into <see cref="StringSegment"/>s.
    /// </summary>
    public readonly struct StringTokenizer : IEnumerable<StringSegment>
    {
        private readonly StringSegment _value;
        private readonly char[] _separators;

        /// <summary>
        /// Initializes a new instance of <see cref="StringTokenizer"/>.
        /// </summary>
        /// <param name="value">The <see cref="string"/> to tokenize.</param>
        /// <param name="separators">The characters to tokenize by.</param>
        public StringTokenizer(string value, char[] separators)
        {
            if (value == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            if (separators == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.separators);
            }

            _value = value;
            _separators = separators;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="StringTokenizer"/>.
        /// </summary>
        /// <param name="value">The <see cref="StringSegment"/> to tokenize.</param>
        /// <param name="separators">The characters to tokenize by.</param>
        public StringTokenizer(StringSegment value, char[] separators)
        {
            if (!value.HasValue)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            if (separators == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.separators);
            }

            _value = value;
            _separators = separators;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Enumerator"/>.
        /// </summary>
        /// <returns>An <see cref="Enumerator"/> based on the <see cref="StringTokenizer"/>'s value and separators.</returns>
        public Enumerator GetEnumerator() => new Enumerator(in _value, _separators);

        IEnumerator<StringSegment> IEnumerable<StringSegment>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Enumerates the <see cref="string"/> tokens represented by <see cref="StringSegment"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<StringSegment>
        {
            private readonly StringSegment _value;
            private readonly char[] _separators;
            private int _index;

            internal Enumerator(in StringSegment value, char[] separators)
            {
                _value = value;
                _separators = separators;
                Current = default;
                _index = 0;
            }

            /// <summary>
            /// Initializes an <see cref="Enumerator"/> using a <see cref="StringTokenizer"/>.
            /// </summary>
            /// <param name="tokenizer"><see cref="StringTokenizer"/> containing value and separators for enumeration.</param>
            public Enumerator(ref StringTokenizer tokenizer)
            {
                _value = tokenizer._value;
                _separators = tokenizer._separators;
                Current = default(StringSegment);
                _index = 0;
            }

            /// <summary>
            /// Gets the current <see cref="StringSegment"/> from the <see cref="StringTokenizer"/>.
            /// </summary>
            public StringSegment Current { get; private set; }

            object IEnumerator.Current => Current;

            /// <summary>
            /// Releases all resources used by the <see cref="Enumerator"/>.
            /// </summary>
            public void Dispose()
            {
            }

            /// <summary>
            /// Advances the enumerator to the next token in the <see cref="StringTokenizer"/>.
            /// </summary>
            /// <returns><see langword="true"/> if the enumerator was successfully advanced to the next token; <see langword="false"/> if the enumerator has passed the end of the <see cref="StringTokenizer"/>.</returns>
            public bool MoveNext()
            {
                if (!_value.HasValue || _index > _value.Length)
                {
                    Current = default(StringSegment);
                    return false;
                }

                int next = _value.IndexOfAny(_separators, _index);
                if (next == -1)
                {
                    // No separator found. Consume the remainder of the string.
                    next = _value.Length;
                }

                Current = _value.Subsegment(_index, next - _index);
                _index = next + 1;

                return true;
            }

            /// <summary>
            /// Resets the <see cref="Enumerator"/> to its initial state.
            /// </summary>
            public void Reset()
            {
                Current = default(StringSegment);
                _index = 0;
            }
        }
    }
}
