// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Buffers;
using System.Collections.Generic;
using System.Collections;

namespace System.Text;

/// <summary>
/// Represents a position in Unicode data, allowing for deeper data inspection.
/// </summary>
/// <remarks>
/// Invalid Unicode symbols will be represented by the <see cref="System.Text.Rune.ReplacementChar"/> value.
/// </remarks>
public readonly struct RunePosition : IEquatable<RunePosition>
{
    /// <summary>
    /// Returns an enumeration of <see cref="RunePosition"/> from the provided span that allows deeper data inspection.
    /// </summary>
    /// <param name="span">The <see cref="ReadOnlySpan{T}"/> with Unicode data.</param>
    /// <returns>
    /// <see cref="Utf16Enumerator"/> to enumerate <see cref="RunePosition"/> from the provided span with UTF-16
    /// Unicode data.
    /// </returns>
    /// <remarks>
    /// Invalid Unicode symbols will be represented by <see cref="System.Text.Rune.ReplacementChar"/>
    /// value.
    /// </remarks>
    public static Utf16Enumerator EnumerateUtf16(ReadOnlySpan<char> span) => new(span);

    /// <summary>
    /// Returns an enumeration of <see cref="RunePosition"/> from the provided span that allows deeper data inspection.
    /// </summary>
    /// <param name="span">The <see cref="ReadOnlySpan{T}"/> with Unicode data.</param>
    /// <returns>
    /// <see cref="Utf8Enumerator"/> to enumerate <see cref="RunePosition"/> from the provided span with UTF-8 Unicode
    /// data.
    /// </returns>
    /// <remarks>
    /// Invalid Unicode symbols will be represented by <see cref="Rune.ReplacementChar"/> value.
    /// </remarks>
    public static Utf8Enumerator EnumerateUtf8(ReadOnlySpan<byte> span) => new(span);

    /// <summary>
    /// Unicode scalar value <see cref="System.Text.Rune"/> of the current symbol in Unicode data.
    /// Invalid Unicode symbols will be represented by <see cref="System.Text.Rune.ReplacementChar"/> value.
    /// </summary>
    public Rune Rune { get; }

    /// <summary>
    /// The index of current symbol in Unicode data.
    /// </summary>
    public int StartIndex { get; }

    /// <summary>
    /// The length of current symbol in Unicode data.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// <see langword="false"/> it current Unicode symbol is correct encoded and <see cref="RunePosition.Rune"/>
    /// contain its scalar value.
    /// <br />
    /// <see langword="true"/> if current Unicode symbol is invalid encoded and <see cref="RunePosition.Rune"/> was
    /// replaced by <see cref="System.Text.Rune.ReplacementChar"/> value.
    /// </summary>
    public bool WasReplaced { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RunePosition"/> struct.
    /// </summary>
    /// <param name="rune">The Unicode scalar value.</param>
    /// <param name="startIndex">The index of the current symbol in Unicode data.</param>
    /// <param name="length">The length of the current symbol in Unicode data.</param>
    /// <param name="wasReplaced">Indicates if the current Unicode symbol was replaced.</param>
    public RunePosition(Rune rune, int startIndex, int length, bool wasReplaced)
    {
        if (startIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_NeedNonNegNum);
        }

        if ((uint)length > Rune.MaxUtf8BytesPerRune)
        {
            throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_NeedNonNegNum);
        }

        StartIndex = startIndex;
        Length = length;
        Rune = rune;
        WasReplaced = wasReplaced;
    }

    /// <summary>
    /// Determines whether the specified <see cref="RunePosition"/> is equal to the current <see cref="RunePosition"/>.
    /// </summary>
    /// <param name="other">The other <see cref="RunePosition"/> to compare with.</param>
    /// <returns>
    /// <see langword="true"/> if the specified <see cref="RunePosition"/> is equal to the current
    /// <see cref="RunePosition"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public bool Equals(RunePosition other) =>
        Rune == other.Rune && StartIndex == other.StartIndex && Length == other.Length && WasReplaced == other.WasReplaced;

    /// <summary>
    /// Determines whether the specified <see cref="object"/> is equal to the current <see cref="RunePosition"/>.
    /// </summary>
    /// <param name="obj">The object to compare with the current <see cref="RunePosition"/>.</param>
    /// <returns>
    /// <see langword="true"/> if the specified <see cref="object"/> is equal to the current
    /// <see cref="RunePosition"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public override bool Equals(object? obj) =>
        obj is RunePosition runePosition && Equals(runePosition);

    /// <summary>
    /// Returns the hash code for the current <see cref="RunePosition"/>.
    /// </summary>
    /// <returns>The hash code for the current <see cref="RunePosition"/>.</returns>
    public override int GetHashCode() =>
        HashCode.Combine(Rune, StartIndex, Length, WasReplaced);

    /// <summary>
    /// Deconstructs the <see cref="RunePosition"/> into its components.
    /// </summary>
    /// <param name="rune">The Unicode scalar value.</param>
    /// <param name="startIndex">The index of the current symbol in Unicode data.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Deconstruct(out Rune rune, out int startIndex)
    {
        rune = Rune;
        startIndex = StartIndex;
    }

    /// <summary>
    /// Deconstructs the <see cref="RunePosition"/> into its components.
    /// </summary>
    /// <param name="rune">The Unicode scalar value.</param>
    /// <param name="startIndex">The index of the current symbol in Unicode data.</param>
    /// <param name="length">The length of the current symbol in Unicode data.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Deconstruct(out Rune rune, out int startIndex, out int length)
    {
        rune = Rune;
        startIndex = StartIndex;
        length = Length;
    }

    /// <summary>
    /// Determines whether two specified <see cref="RunePosition"/> instances are equal.
    /// </summary>
    /// <param name="left">The first <see cref="RunePosition"/> to compare.</param>
    /// <param name="right">The second <see cref="RunePosition"/> to compare.</param>
    /// <returns>
    /// <see langword="true"/> if the two <see cref="RunePosition"/> instances are equal; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public static bool operator ==(RunePosition left, RunePosition right) => left.Equals(right);

    /// <summary>
    /// Determines whether two specified <see cref="RunePosition"/> instances are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="RunePosition"/> to compare.</param>
    /// <param name="right">The second <see cref="RunePosition"/> to compare.</param>
    /// <returns>
    /// <see langword="true"/> if the two <see cref="RunePosition"/> instances are not equal; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public static bool operator !=(RunePosition left, RunePosition right) => !(left == right);

    /// <summary>
    /// An enumerator for retrieving <see cref="RunePosition"/> instances from Unicode data.
    /// </summary>
    /// <remarks>
    /// Methods are pattern-matched by compiler to allow using foreach pattern.
    /// </remarks>
    public ref struct Utf16Enumerator: IEnumerator<RunePosition>
    {
        private ReadOnlySpan<char> _remaining;

        /// <summary>
        /// The current <see cref="RunePosition"/> in the Unicode data.
        /// </summary>
        public RunePosition Current { get; private set; }

        /// <summary>
        /// Returns the current enumerator instance.
        /// </summary>
        /// <returns>The current enumerator instance.</returns>
        public Utf16Enumerator GetEnumerator() => this;

        internal Utf16Enumerator(ReadOnlySpan<char> buffer)
        {
            _remaining = buffer;
            Current = default;
        }

        /// <summary>
        /// Moves to the next <see cref="RunePosition"/> in the Unicode data.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the enumerator was successfully advanced to the next <see cref="RunePosition"/>;
        /// <br />
        /// <see langword="false"/> if the enumerator has passed the end of the span.</returns>
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

        object IEnumerator.Current => Current;
        void IEnumerator.Reset() => throw new NotSupportedException();
        void IDisposable.Dispose() { }
    }

    /// <summary>
    /// An enumerator for retrieving <see cref="RunePosition"/> instances from Unicode data.
    /// </summary>
    /// <remarks>
    /// Methods are pattern-matched by compiler to allow using foreach pattern.
    /// </remarks>
    public ref struct Utf8Enumerator : IEnumerator<RunePosition>
    {
        private ReadOnlySpan<byte> _remaining;

        /// <summary>
        /// The current <see cref="RunePosition"/> in the Unicode data.
        /// </summary>
        public RunePosition Current { get; private set; }

        /// <summary>
        /// Returns the current enumerator instance.
        /// </summary>
        /// <returns>The current enumerator instance.</returns>
        public Utf8Enumerator GetEnumerator() => this;

        /// <summary>
        /// Initializes a new instance of the <see cref="Utf8Enumerator"/> struct.
        /// </summary>
        /// <param name="buffer">The buffer containing the Unicode data.</param>
        internal Utf8Enumerator(ReadOnlySpan<byte> buffer)
        {
            _remaining = buffer;
            Current = default;
        }

        /// <summary>
        /// Moves to the next <see cref="RunePosition"/> in the Unicode data.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the enumerator was successfully advanced to the next <see cref="RunePosition"/>;
        /// <br />
        /// <see langword="false"/> if the enumerator has passed the end of the span.
        /// </returns>
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

        object IEnumerator.Current => Current;
        void IEnumerator.Reset() => throw new NotSupportedException();
        void IDisposable.Dispose() { }
    }
}
