// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    /// <summary>Represents a range that has start and end indices.</summary>
    /// <remarks>
    /// <code>
    /// int[] someArray = new int[5] { 1, 2, 3, 4, 5 };
    /// int[] subArray1 = someArray[0..2]; // { 1, 2 }
    /// int[] subArray2 = someArray[1..^0]; // { 2, 3, 4, 5 }
    /// </code>
    /// </remarks>
    public readonly struct NRange : IEquatable<NRange>
    {
        /// <summary>Gets the inclusive start NIndex of the NRange.</summary>
        public NIndex Start { get; }

        /// <summary>Gets the exclusive end NIndex of the NRange.</summary>
        public NIndex End { get; }

        /// <summary>Constructs an <see cref="NRange"/> object using the start and end <see cref="NIndex"/>.</summary>
        /// <param name="start">The inclusive start <see cref="NIndex"/> of the <see cref="NRange"/>.</param>
        /// <param name="end">The exclusive end <see cref="NIndex"/> of the <see cref="NRange"/>.</param>
        public NRange(NIndex start, NIndex end)
        {
            Start = start;
            End = end;
        }

        /// <summary>
        /// Constructs an <see cref="NRange"/> object using a <see cref="Range"/>.
        /// </summary>
        /// <param name="range">The <see cref="Range"/> to use.</param>
        public NRange(Range range)
        {
            Start = range.Start;
            End = range.End;
        }

        /// <summary>Compares the current <see cref="NRange"/> object to another object of the same type for equality.</summary>
        /// <param name="value">An object to compare with this object.</param>
        public override bool Equals([NotNullWhen(true)] object? value) =>
            value is NRange r &&
            r.Start.Equals(Start) &&
            r.End.Equals(End);

        /// <summary>Compares the current <see cref="NRange"/> object to another <see cref="NRange"/> object for equality.</summary>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(NRange other) => other.Start.Equals(Start) && other.End.Equals(End);

        /// <summary>Returns the hash code for this instance.</summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(Start.GetHashCode(), End.GetHashCode());
        }

        /// <summary>Converts the value of the current NRange object to its equivalent string representation.</summary>
        public override string ToString()
        {
            Span<char> span = stackalloc char[2 + 2 * 21]; // 2 for "..", then for each NIndex 1 for '^' and 20 for longest possible nuint
            int pos = 0;

            if (Start.IsFromEnd)
            {
                span[0] = '^';
                pos = 1;
            }
            bool formatted = ((uint)Start.Value).TryFormat(span.Slice(pos), out int charsWritten);
            Debug.Assert(formatted);
            pos += charsWritten;

            span[pos++] = '.';
            span[pos++] = '.';

            if (End.IsFromEnd)
            {
                span[pos++] = '^';
            }
            formatted = ((uint)End.Value).TryFormat(span.Slice(pos), out charsWritten);
            Debug.Assert(formatted);
            pos += charsWritten;

            return new string(span.Slice(0, pos));
        }

        /// <summary>Creates an <see cref="NRange"/> object starting from start <see cref="NIndex"/> to the end of the collection.</summary>
        public static NRange StartAt(NIndex start) => new NRange(start, NIndex.End);

        /// <summary>Creates an <see cref="NRange"/> object starting from first element in the collection to the end <see cref="NIndex"/>.</summary>
        public static NRange EndAt(NIndex end) => new NRange(NIndex.Start, end);

        /// <summary>Creates an NRange object starting from first element to the end.</summary>
        public static NRange All => new NRange(NIndex.Start, NIndex.End);

        /// <summary>Calculates the start offset and length of the <see cref="NRange"/> object using a collection length.</summary>
        /// <param name="length">The length of the collection that the <see cref="NRange"/> will be used with. Must be a positive value.</param>
        /// <remarks>
        /// For performance reasons, the input length parameter isn't validated against negative values.
        /// It's expected NRange will be used with collections that always have a non-negative length/count.
        /// The <see cref="NRange"/> is validated to be inside the length scope, however.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (nint Offset, nint Length) GetOffsetAndLength(nint length)
        {
            nint start = Start.GetOffset(length);
            nint end = End.GetOffset(length);

            if ((uint)end > (uint)length || (uint)start > (uint)end)
            {
                ThrowArgumentOutOfRangeException();
            }

            return (start, end - start);
        }

        private static void ThrowArgumentOutOfRangeException() => throw new ArgumentOutOfRangeException("length");

        /// <summary>
        /// Implicitly converts a <see cref="Range"/> to an <see cref="NRange"/>.
        /// </summary>
        /// <param name="range"></param>
        public static implicit operator NRange(Range range) => new NRange(range.Start, range.End);

        /// <summary>
        /// Explicitly converts an <see cref="NRange"/> to a <see cref="Range"/> without doing bounds checks.
        /// </summary>
        /// <param name="value"><see cref="NRange"/> to convert.</param>
        public static explicit operator Range(NRange value) => new Range((Index)value.Start, (Index)value.End);

        /// <summary>
        /// Explicitly converts an <see cref="NRange"/> to a <see cref="Range"/>.
        /// </summary>
        /// <param name="value"><see cref="NRange"/> to convert.</param>
        public static explicit operator checked Range(NRange value) => new Range(checked((Index)value.Start), checked((Index)value.End));

        /// <summary>
        /// Converts a <see cref="NRange"/> to a <see cref="Range"/>.
        /// </summary>
        /// <returns>The converted Range.</returns>
        public Range ToRange() => new Range(checked((Index)Start), checked((Index)End));

        /// <summary>
        /// Converts a <see cref="NRange"/> to a <see cref="Range"/> without doing bounds checks.
        /// </summary>
        /// <returns>The converted Range.</returns>
        public Range ToRangeUnchecked() => new Range((Index)Start, (Index)End);
    }
}
