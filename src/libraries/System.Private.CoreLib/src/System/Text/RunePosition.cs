// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Buffers;

namespace System.Text;

/// <summary>
/// The position in unicode data that allows deeper data inspection.
/// </summary>
/// <remarks>
/// Invalid unicode char will be represented in <see cref="RunePosition.Rune"/> by <see cref="Rune.ReplacementChar"/> value.
/// </remarks>
public readonly struct RunePosition : IEquatable<RunePosition>
{
    /// <summary>
    /// Returns an enumeration of <see cref="RunePosition"/> from the provided span that allows deeper data inspection.
    /// </summary>
    /// <param name="span">The <see cref="ReadOnlySpan{T}"/> with unicode data.</param>
    /// <returns><see cref="Utf16Enumerator"/> to enumerate <see cref="RunePosition"/> from the provided span with unicode data.</returns>
    /// <remarks>
    /// Invalid unicode chars will be represented the enumeration by <see cref="Rune.ReplacementChar"/> value.
    /// </remarks>
    public static Utf16Enumerator EnumerateUtf16(ReadOnlySpan<char> span) => new Utf16Enumerator(span);

    /// <summary>
    /// Returns an enumeration of <see cref="RunePosition"/> from the provided span that allows deeper data inspection.
    /// </summary>
    /// <param name="span">The <see cref="ReadOnlySpan{T}"/> with unicode data.</param>
    /// <returns><see cref="Utf8Enumerator"/> to enumerate <see cref="RunePosition"/> from the provided span with unicode data.</returns>
    /// <remarks>
    /// Invalid unicode chars will be represented the enumeration by <see cref="Rune.ReplacementChar"/> value.
    /// </remarks>
    public static Utf8Enumerator EnumerateUtf8(ReadOnlySpan<byte> span) => new Utf8Enumerator(span);

    /// <summary>
    /// Unicode scalar value <see cref="System.Text.Rune"/> of the current char in unicode data.
    /// Invalid unicode char will be represented by <see cref="Rune.ReplacementChar"/> value.
    /// </summary>
    public Rune Rune { get; }

    /// <summary>
    /// The index of current char in unicode data.
    /// </summary>
    public int StartIndex { get; }

    /// <summary>
    /// The length of current char in unicode data.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// false it current char is correct encoded and <see cref="RunePosition.Rune"/> contain its scalar value.
    /// true if current char is invalid encoded and <see cref="RunePosition.Rune"/> was replaced by <see cref="System.Text.Rune.ReplacementChar"/> value.
    /// </summary>
    public bool WasReplaced { get; }

    public RunePosition(Rune rune, int startIndex, int length, bool wasReplaced) =>
        (Rune, StartIndex, Length, WasReplaced) = (rune, startIndex, length, wasReplaced);

    public bool Equals(RunePosition other) =>
        Rune == other.Rune && StartIndex == other.StartIndex && Length == other.Length && WasReplaced == other.WasReplaced;

    public override bool Equals(object? obj) =>
        obj is RunePosition runePosition ? Equals(runePosition) : false;

    public override int GetHashCode() =>
        ((Rune.GetHashCode() * -1521134295 + StartIndex.GetHashCode()) * -1521134295 + Length.GetHashCode()) * -1521134295 + WasReplaced.GetHashCode();

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Deconstruct(out Rune rune, out int startIndex) =>
        (rune, startIndex) = (Rune, StartIndex);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Deconstruct(out Rune rune, out int startIndex, out int length) =>
        (rune, startIndex, length) = (Rune, StartIndex, Length);

    public static bool operator ==(RunePosition left, RunePosition right) => left.Equals(right);
    public static bool operator !=(RunePosition left, RunePosition right) => !(left == right);

    /// <summary>
    /// An enumerator for retrieving <see cref="RunePosition"/> instances from unicode data.
    /// </summary>
    /// <remarks>
    /// Methods are pattern-matched by compiler to allow using foreach pattern.
    /// </remarks>
    public ref struct Utf16Enumerator
    {
        private ReadOnlySpan<char> _remaining;

        public RunePosition Current { get; private set; }

        public Utf16Enumerator GetEnumerator() => this;

        internal Utf16Enumerator(ReadOnlySpan<char> buffer)
        {
            _remaining = buffer;
            Current = default;
        }

        public bool MoveNext()
        {
            if (_remaining.IsEmpty)
            {
                // reached the end of the buffer
                Current = default;
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
                Current = new RunePosition(rune, Current.StartIndex + Current.Length, length, false);
                _remaining = _remaining.Slice(length);
            }
            else
            {
                Current = new RunePosition(Rune.ReplacementChar, Current.StartIndex + Current.Length, 1, true);
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

        public RunePosition Current { get; private set; }

        public Utf8Enumerator GetEnumerator() => this;

        internal Utf8Enumerator(ReadOnlySpan<byte> buffer)
        {
            _remaining = buffer;
            Current = default;
        }

        public bool MoveNext()
        {
            if (_remaining.IsEmpty)
            {
                // reached the end of the buffer
                Current = default;
                return false;
            }

            bool wasReplaced = Rune.DecodeFromUtf8(_remaining, out Rune rune, out int charsConsumed) != OperationStatus.Done;
            Current = new RunePosition(rune, Current.StartIndex + Current.Length, charsConsumed, wasReplaced);
            _remaining = _remaining.Slice(charsConsumed);
            return true;
        }
    }
}
