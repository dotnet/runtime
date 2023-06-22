// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Buffers;

namespace System.Text;

/// <summary>
/// The position in unicode data that allows deeper data inspection.
/// </summary>
/// <param name="Rune">
/// Unicode scalar value <see cref="System.Text.Rune"/> of the current char in unicode data.
/// Invalid encoded unicode char will be represented by <see cref="Rune.ReplacementChar"/> value.
/// </param>
/// <param name="StartIndex">The index of current char in unicode data.</param>
/// <param name="Length">The length of current char in unicode data.</param>
/// <param name="WasReplaced">
/// true if current char is invalid encoded and <see cref="Rune"/> was replaced by <see cref="System.Text.Rune.ReplacementChar"/>.
/// false it current char is correct encoded and <see cref="Rune"/> contain its scalar value.
/// </param>
/// <remarks>
/// Invalid unicode char will be represented in <see cref="Rune"/> by <see cref="Rune.ReplacementChar"/>.
/// </remarks>
public readonly record struct RunePosition(Rune Rune, int StartIndex, int Length, bool WasReplaced)
    : IEquatable<RunePosition>
{
    /// <summary>
    /// Returns an enumeration of <see cref="RunePosition"/> from the provided span that allows deeper data inspection.
    /// </summary>
    /// <param name="span">The <see cref="ReadOnlySpan{T}"/> with unicode data.</param>
    /// <returns><see cref="Utf16Enumerator"/> to enumerate <see cref="RunePosition"/> from the provided span with unicode data.</returns>
    /// <remarks>
    /// Invalid unicode chars will be represented the enumeration by <see cref="Rune.ReplacementChar"/>.
    /// </remarks>
    public static Utf16Enumerator EnumerateUtf16(ReadOnlySpan<char> span)
        => new Utf16Enumerator(span);

    /// <summary>
    /// Returns an enumeration of <see cref="RunePosition"/> from the provided span that allows deeper data inspection.
    /// </summary>
    /// <param name="span">The <see cref="ReadOnlySpan{T}"/> with unicode data.</param>
    /// <returns><see cref="Utf8Enumerator"/> to enumerate <see cref="RunePosition"/> from the provided span with unicode data.</returns>
    /// <remarks>
    /// Invalid unicode chars will be represented the enumeration by <see cref="Rune.ReplacementChar"/>.
    /// </remarks>
    public static Utf8Enumerator EnumerateUtf8(ReadOnlySpan<byte> span)
        => new Utf8Enumerator(span);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Deconstruct(out Rune rune, out int startIndex)
    {
        rune = Rune;
        startIndex = StartIndex;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Deconstruct(out Rune rune, out int startIndex, out int length)
    {
        rune = Rune;
        startIndex = StartIndex;
        length = Length;
    }

    /// <summary>
    /// An enumerator for retrieving <see cref="RunePosition"/> instances from unicode data.
    /// </summary>
    /// <remarks>
    /// Methods are pattern-matched by compiler to allow using foreach pattern.
    /// </remarks>
    public ref struct Utf16Enumerator
    {
        private ReadOnlySpan<char> _remaining;
        private RunePosition _current;

        internal Utf16Enumerator(ReadOnlySpan<char> buffer)
        {
            _remaining = buffer;
            _current = default;
        }

        public RunePosition Current => _current;

        public Utf16Enumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            if (_remaining.IsEmpty)
            {
                // reached the end of the buffer
                _current = default;
                return false;
            }

            // In UTF-16 specifically, invalid sequences always have length 1, which is the same
            // length as the replacement character U+FFFD. This means that we can always bump the
            // next index by the current scalar's UTF-16 sequence length. This optimization is not
            // generally applicable; for example, enumerating scalars from UTF-8 cannot utilize
            // this same trick.

            int scalarValue = Rune.ReadFirstRuneFromUtf16Buffer(_remaining);
            if (scalarValue >= 0)
            {
                Rune rune = Rune.UnsafeCreate((uint)scalarValue);
                int length = rune.Utf16SequenceLength;
                _current = new RunePosition(rune, _current.StartIndex + _current.Length, length, false);
                _remaining = _remaining.Slice(length);
            }
            else
            {
                _current = new RunePosition(Rune.ReplacementChar, _current.StartIndex + _current.Length, 1, true);
                _remaining = _remaining.Slice(1);
            }
            return true;
        }
    }

    /// <summary>
    /// An enumerator for retrieving <see cref="RunePosition"/> instances from unicode data.
    /// </summary>
    /// <remarks>
    /// Methods are pattern-matched by compiler to allow using foreach pattern.
    /// </remarks>
    public ref struct Utf8Enumerator
    {
        private ReadOnlySpan<byte> _remaining;
        private RunePosition _current;

        internal Utf8Enumerator(ReadOnlySpan<byte> buffer)
        {
            _remaining = buffer;
            _current = default;
        }

        public RunePosition Current => _current;

        public Utf8Enumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            if (_remaining.IsEmpty)
            {
                // reached the end of the buffer
                _current = default;
                return false;
            }

            bool wasReplaced = Rune.DecodeFromUtf8(_remaining, out Rune rune, out int charsConsumed) != OperationStatus.Done;
            _current = new RunePosition(rune, _current.StartIndex + _current.Length, charsConsumed, wasReplaced);
            _remaining = _remaining.Slice(charsConsumed);
            return true;
        }
    }
}
