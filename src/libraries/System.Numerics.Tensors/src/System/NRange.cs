// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    /// <summary>Represent a range that has start and end indices.</summary>
    /// <remarks>
    /// <code>
    /// int[] someArray = new int[5] { 1, 2, 3, 4, 5 };
    /// int[] subArray1 = someArray[0..2]; // { 1, 2 }
    /// int[] subArray2 = someArray[1..^0]; // { 2, 3, 4, 5 }
    /// </code>
    /// </remarks>
    [Experimental("SNTEXP0001")]
    public readonly struct NRange : IEquatable<NRange>
    {
        /// <summary>Represent the inclusive start NIndex of the NRange.</summary>
        public NIndex Start { get; }

        /// <summary>Represent the exclusive end NIndex of the NRange.</summary>
        public NIndex End { get; }

        /// <summary>Construct an NRange object using the start and end NIndexes.</summary>
        /// <param name="start">Represent the inclusive start NIndex of the NRange.</param>
        /// <param name="end">Represent the exclusive end NIndex of the NRange.</param>
        public NRange(NIndex start, NIndex end)
        {
            Start = start;
            End = end;
        }

        /// <summary>
        /// Construct a <see cref="NRange"/> object using a <see cref="Range"/>.
        /// </summary>
        /// <param name="range">The <see cref="Range"/> to use.</param>
        public NRange(Range range)
        {
            Start = range.Start;
            End = range.End;
        }

        /// <summary>Indicates whether the current NRange object is equal to another object of the same type.</summary>
        /// <param name="value">An object to compare with this object</param>
        public override bool Equals([NotNullWhen(true)] object? value) =>
            value is NRange r &&
            r.Start.Equals(Start) &&
            r.End.Equals(End);

        /// <summary>Indicates whether the current NRange object is equal to another NRange object.</summary>
        /// <param name="other">An object to compare with this object</param>
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

        /// <summary>Create an NRange object starting from start NIndex to the end of the collection.</summary>
        public static NRange StartAt(NIndex start) => new NRange(start, NIndex.End);

        /// <summary>Create an NRange object starting from first element in the collection to the end NIndex.</summary>
        public static NRange EndAt(NIndex end) => new NRange(NIndex.Start, end);

        /// <summary>Create an NRange object starting from first element to the end.</summary>
        public static NRange All => new NRange(NIndex.Start, NIndex.End);

        /// <summary>Calculate the start offset and length of NRange object using a collection length.</summary>
        /// <param name="length">The length of the collection that the NRange will be used with. length has to be a positive value.</param>
        /// <remarks>
        /// For performance reason, we don't validate the input length parameter against negative values.
        /// It is expected NRange will be used with collections which always have non negative length/count.
        /// We validate the NRange is inside the length scope though.
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

        public static implicit operator NRange(Range range) => new NRange(range.Start, range.End);

        public static explicit operator Range(NRange value) => new Range((Index)value.Start, (Index)value.End);
        public static explicit operator checked Range(NRange value) => new Range(checked((Index)value.Start), checked((Index)value.End));

        public Range ToRange() => new Range(checked((Index)Start), checked((Index)End));
        public Range ToRangeUnchecked() => new Range((Index)Start, (Index)End);
    }
}
