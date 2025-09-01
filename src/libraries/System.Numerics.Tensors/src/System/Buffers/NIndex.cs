// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    /// <summary>Represents a type that can be used to index a collection either from the start or the end.</summary>
    /// <remarks>
    /// <code>
    /// int[] someArray = new int[5] { 1, 2, 3, 4, 5 } ;
    /// int lastElement = someArray[^1]; // lastElement = 5
    /// </code>
    /// </remarks>
    public readonly struct NIndex : IEquatable<NIndex>
    {
        private readonly nint _value;

        /// <summary>Constructs an <see cref="NIndex"/> using an index value and a Boolean that indicates if the <see cref="NIndex"/> is from the start or from the end.</summary>
        /// <param name="value">The index value. It must be greater than or equal to zero.</param>
        /// <param name="fromEnd"><see langword="true"/> if the index is from the start; <see langword="false"/> if it's from the end.</param>
        /// <remarks>
        /// If the NIndex constructed from the end, an index value of 1 points at the last element and an index value of 0 points beyond the last element.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NIndex(nint value, bool fromEnd = false)
        {
            if (value < 0)
            {
                ThrowHelper.ThrowValueArgumentOutOfRange_NeedNonNegNumException();
            }

            if (fromEnd)
                _value = ~value;
            else
                _value = value;
        }

        /// <summary>Constructs an <see cref="NIndex"/> from an <see cref="Index"/>.</summary>
        /// <param name="index">The <see cref="Index"/> to create the <see cref="NIndex"/> from.</param>
        /// <remarks>
        /// If the NIndex constructed from the end, an index value of 1 points at the last element and an index value of 0 points beyond the last element.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NIndex(Index index)
        {
            if (index.IsFromEnd)
                _value = ~index.Value;
            else
                _value = index.Value;
        }

        // The following private constructor exists to skip the checks in the public ctor
        private NIndex(nint value)
        {
            _value = value;
        }

        /// <summary>Creates an <see cref="NIndex"/> that points at the first element.</summary>
        public static NIndex Start => new NIndex((nint)0);

        /// <summary>Creates an <see cref="NIndex"/> that points beyond the last element.</summary>
        public static NIndex End => new NIndex((nint)~0);

        /// <summary>Creates an <see cref="NIndex"/> from the start at the specified position.</summary>
        /// <param name="value">The index value from the start.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NIndex FromStart(nint value)
        {
            if (value < 0)
            {
                ThrowHelper.ThrowValueArgumentOutOfRange_NeedNonNegNumException();
            }

            return new NIndex(value);
        }

        /// <summary>Creates an NIndex from the end at the specified position.</summary>
        /// <param name="value">The index value from the end.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NIndex FromEnd(nint value)
        {
            if (value < 0)
            {
                ThrowHelper.ThrowValueArgumentOutOfRange_NeedNonNegNumException();
            }

            return new NIndex(~value);
        }

        /// <summary>
        /// Converts the <see cref="NIndex"/> to an <see cref="Index"/>.
        /// </summary>
        /// <returns>The converted Index.</returns>
        public Index ToIndex() => checked((Index)this);

        /// <summary>
        /// Converts the <see cref="NIndex"/> to an <see cref="Index"/> without doing bounds checks.
        /// </summary>
        /// <returns>The converted Index.</returns>
        public Index ToIndexUnchecked() => (Index)this;

        /// <summary>Gets the <see cref="NIndex"/> value.</summary>
        public nint Value
        {
            get
            {
                if (_value < 0)
                    return ~_value;
                else
                    return _value;
            }
        }

        /// <summary>Gets a value that indicates whether the <see cref="NIndex"/> is from the start or the end.</summary>
        public bool IsFromEnd => _value < 0;

        /// <summary>Calculates the offset from the start using the given collection length.</summary>
        /// <param name="length">The length of the collection that the NIndex will be used with. Must be a positive value.</param>
        /// <remarks>
        /// For performance reasons, the input length argument and the returned offset value aren't validated against negative values.
        /// Also, the returned offset might be greater than the input length.
        /// It is expected <see cref="NIndex"/> will be used with collections that always have a non-negative length/count. If the returned offset is negative and
        /// then used to <see cref="NIndex"/> a collection, an <see cref="ArgumentOutOfRangeException" /> is thrown, which has the same effect as the validation.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public nint GetOffset(nint length)
        {
            nint offset = _value;
            if (IsFromEnd)
            {
                // offset = length - (~value)
                // offset = length + (~(~value) + 1)
                // offset = length + value + 1

                offset += length + 1;
            }
            return offset;
        }

        /// <summary>Compares the current NIndex object to another object of the same type for equality.</summary>
        /// <param name="value">An object to compare with this object.</param>
        public override bool Equals([NotNullWhen(true)] object? value) => value is NIndex other && _value == other._value;

        /// <summary>Compares the current <see cref="NIndex"/> object to another <see cref="NIndex"/> object for equality.</summary>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(NIndex other) => _value == other._value;

        /// <summary>Returns the hash code for this instance.</summary>
        public override int GetHashCode() => _value.GetHashCode();

        /// <summary>Converts an integer number to an NIndex.</summary>
        public static implicit operator NIndex(nint value) => FromStart(value);

        /// <summary>Converts a native integer number to an NIndex.</summary>
        public static implicit operator NIndex(Index value) => new NIndex(value);

        /// <summary>Converts an <see cref="NIndex"/> to an <see cref="Index"/>.</summary>
        public static explicit operator Index(NIndex value) => new Index((int)value.Value, value.IsFromEnd);

        /// <summary>Converts an <see cref="NIndex"/> to an <see cref="Index"/>.</summary>
        public static explicit operator checked Index(NIndex value) => new Index(checked((int)value.Value), value.IsFromEnd);

        /// <summary>Converts the value of the current NIndex object to its equivalent string representation.</summary>
        public override string ToString()
        {
            if (IsFromEnd)
                return ToStringFromEnd();

            return Value.ToString();
        }

        private string ToStringFromEnd()
        {
            Span<char> span = stackalloc char[21]; // 1 for ^ and 20 for longest possible nuint value
            bool formatted = ((uint)Value).TryFormat(span.Slice(1), out int charsWritten);
            Debug.Assert(formatted);
            span[0] = '^';
            return new string(span.Slice(0, charsWritten + 1));
        }
    }
}
