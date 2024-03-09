// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System
{
    /// <summary>Represent a NativeRange has start and end NativeIndexes.</summary>
    /// <remarks>
    /// NativeRange is used by the C# compiler to support the NativeRange syntax.
    /// <code>
    /// int[] someArray = new int[5] { 1, 2, 3, 4, 5 };
    /// int[] subArray1 = someArray[0..2]; // { 1, 2 }
    /// int[] subArray2 = someArray[1..^0]; // { 2, 3, 4, 5 }
    /// </code>
    /// </remarks>
    public readonly struct NativeRange : IEquatable<NativeRange>
    {
        /// <summary>Represent the inclusive start NativeIndex of the NativeRange.</summary>
        public NativeIndex Start { get; }

        /// <summary>Represent the exclusive end NativeIndex of the NativeRange.</summary>
        public NativeIndex End { get; }

        /// <summary>Construct a NativeRange object using the start and end NativeIndexes.</summary>
        /// <param name="start">Represent the inclusive start NativeIndex of the NativeRange.</param>
        /// <param name="end">Represent the exclusive end NativeIndex of the NativeRange.</param>
        public NativeRange(NativeIndex start, NativeIndex end)
        {
            Start = start;
            End = end;
        }

        /// <summary>Indicates whether the current NativeRange object is equal to another object of the same type.</summary>
        /// <param name="value">An object to compare with this object</param>
        public override bool Equals([NotNullWhen(true)] object? value) =>
            value is NativeRange r &&
            r.Start.Equals(Start) &&
            r.End.Equals(End);

        /// <summary>Indicates whether the current NativeRange object is equal to another NativeRange object.</summary>
        /// <param name="other">An object to compare with this object</param>
        public bool Equals(NativeRange other) => other.Start.Equals(Start) && other.End.Equals(End);

        /// <summary>Returns the hash code for this instance.</summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(Start.GetHashCode(), End.GetHashCode());
        }

        /// <summary>Converts the value of the current NativeRange object to its equivalent string representation.</summary>
        public override string ToString()
        {
            Span<char> span = stackalloc char[2 + (2 * 11)]; // 2 for "..", then for each NativeIndex 1 for '^' and 10 for longest possible uint
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

        /// <summary>Create a NativeRange object starting from start NativeIndex to the end of the collection.</summary>
        public static NativeRange StartAt(NativeIndex start) => new NativeRange(start, NativeIndex.End);

        /// <summary>Create a NativeRange object starting from first element in the collection to the end NativeIndex.</summary>
        public static NativeRange EndAt(NativeIndex end) => new NativeRange(NativeIndex.Start, end);

        /// <summary>Create a NativeRange object starting from first element to the end.</summary>
        public static NativeRange All => new NativeRange(NativeIndex.Start, NativeIndex.End);

        /// <summary>Calculate the start offset and length of NativeRange object using a collection length.</summary>
        /// <param name="length">The length of the collection that the NativeRange will be used with. length has to be a positive value.</param>
        /// <remarks>
        /// For performance reason, we don't validate the input length parameter against negative values.
        /// It is expected NativeRange will be used with collections which always have non negative length/count.
        /// We validate the NativeRange is inside the length scope though.
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

        private static void ThrowArgumentOutOfRangeException()
        {
            throw new ArgumentOutOfRangeException("length");
        }

        public static implicit operator NativeRange(Range range)
        {
            return new NativeRange(range.Start, range.End);
        }
    }
}
