// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;

namespace System.Text
{
    public readonly ref partial struct Utf8Span
    {
        public CharEnumerable Chars => new CharEnumerable(this);
        public RuneEnumerable Runes => new RuneEnumerable(this);

        public readonly ref struct CharEnumerable
        {
            private readonly Utf8Span _span;

            internal CharEnumerable(Utf8Span span)
            {
                _span = span;
            }

            public Enumerator GetEnumerator() => new Enumerator(_span);

            public ref struct Enumerator
            {
                private uint _currentCharPair;
                private ReadOnlySpan<byte> _remainingUtf8Bytes;

                internal Enumerator(Utf8Span span)
                {
                    _currentCharPair = default;
                    _remainingUtf8Bytes = span.Bytes;
                }

                public char Current => (char)_currentCharPair;

                public bool MoveNext()
                {
                    // We don't need to worry about tearing since this enumerator is a ref struct.

                    if (_currentCharPair > char.MaxValue)
                    {
                        // There was a surrogate pair smuggled in here from a previous operation.
                        // Shift out the high surrogate value and return immediately.

                        _currentCharPair >>= 16;
                        return true;
                    }

                    if (_remainingUtf8Bytes.IsEmpty)
                    {
                        return false;
                    }

                    // TODO_UTF8STRING: Since we assume Utf8String instances are well-formed, we may instead
                    // call an optimized version of the "decode" routine below which skips well-formedness checks.

                    OperationStatus status = Rune.DecodeFromUtf8(_remainingUtf8Bytes, out Rune currentRune, out int bytesConsumed);
                    Debug.Assert(status == OperationStatus.Done, "Somebody fed us invalid data?");

                    if (currentRune.IsBmp)
                    {
                        // Common case - BMP scalar value.

                        _currentCharPair = (uint)currentRune.Value;
                    }
                    else
                    {
                        // Uncommon case - supplementary plane (astral) scalar value.
                        // We'll smuggle the two UTF-16 code units into a single 32-bit value,
                        // with the leading surrogate packed into the low 16 bits of the value,
                        // and the trailing surrogate packed into the high 16 bits of the value.

                        UnicodeUtility.GetUtf16SurrogatesFromSupplementaryPlaneScalar((uint)currentRune.Value, out char leadingCodeUnit, out char trailingCodeUnit);
                        _currentCharPair = (uint)leadingCodeUnit + ((uint)trailingCodeUnit << 16);
                    }

                    // TODO_UTF8STRING: We can consider unsafe slicing below if we wish since we know we're
                    // not going to overrun the end of the span.

                    _remainingUtf8Bytes = _remainingUtf8Bytes.Slice(bytesConsumed);
                    return true;
                }
            }
        }

        public readonly ref struct RuneEnumerable
        {
            private readonly Utf8Span _span;

            internal RuneEnumerable(Utf8Span span)
            {
                _span = span;
            }

            public Enumerator GetEnumerator() => new Enumerator(_span);

            public ref struct Enumerator
            {
                private Rune _currentRune;
                private ReadOnlySpan<byte> _remainingUtf8Bytes;

                internal Enumerator(Utf8Span span)
                {
                    _currentRune = default;
                    _remainingUtf8Bytes = span.Bytes;
                }

                public Rune Current => _currentRune;

                public bool MoveNext()
                {
                    // We don't need to worry about tearing since this enumerator is a ref struct.

                    if (_remainingUtf8Bytes.IsEmpty)
                    {
                        return false;
                    }

                    // TODO_UTF8STRING: Since we assume Utf8Span instances are well-formed, we may instead
                    // call an optimized version of the "decode" routine below which skips well-formedness checks.

                    OperationStatus status = Rune.DecodeFromUtf8(_remainingUtf8Bytes, out _currentRune, out int bytesConsumed);
                    Debug.Assert(status == OperationStatus.Done, "Somebody fed us invalid data?");

                    // TODO_UTF8STRING: We can consider unsafe slicing below if we wish since we know we're
                    // not going to overrun the end of the span.

                    _remainingUtf8Bytes = _remainingUtf8Bytes.Slice(bytesConsumed);
                    return true;
                }
            }
        }
    }
}
