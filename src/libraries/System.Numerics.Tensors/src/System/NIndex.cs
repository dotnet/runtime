// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    /// <summary>Represent a type can be used to index a collection either from the start or the end.</summary>
    /// <remarks>
    /// <code>
    /// int[] someArray = new int[5] { 1, 2, 3, 4, 5 } ;
    /// int lastElement = someArray[^1]; // lastElement = 5
    /// </code>
    /// </remarks>
    public readonly struct NIndex : IEquatable<NIndex>
    {
        private readonly nint _value;

        /// <summary>Construct an NIndex using a value and indicating if the NIndex is from the start or from the end.</summary>
        /// <param name="value">The index value. it has to be zero or positive number.</param>
        /// <param name="fromEnd">Indicating if the index is from the start or from the end.</param>
        /// <remarks>
        /// If the NIndex constructed from the end, index value 1 means pointing at the last element and index value 0 means pointing at beyond last element.
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

        /// <summary>Construct a <see cref="NIndex"/> from a <see cref="Index"/></summary>
        /// <param name="index">The <see cref="Index"/> to create the <see cref="NIndex"/> from.</param>
        /// <remarks>
        /// If the NIndex constructed from the end, index value 1 means pointing at the last element and index value 0 means pointing at beyond last element.
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

        /// <summary>Create an NIndex pointing at first element.</summary>
        public static NIndex Start => new NIndex((nint)0);

        /// <summary>Create an NIndex pointing at beyond last element.</summary>
        public static NIndex End => new NIndex((nint)~0);

        /// <summary>Create an NIndex from the start at the position indicated by the value.</summary>
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

        /// <summary>Create an NIndex from the end at the position indicated by the value.</summary>
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

        public Index ToIndex() => checked((Index)this);
        public Index ToIndexUnchecked() => (Index)this;

        /// <summary>Returns the NIndex value.</summary>
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

        /// <summary>Indicates whether the NIndex is from the start or the end.</summary>
        public bool IsFromEnd => _value < 0;

        /// <summary>Calculate the offset from the start using the giving collection length.</summary>
        /// <param name="length">The length of the collection that the NIndex will be used with. length has to be a positive value</param>
        /// <remarks>
        /// For performance reason, we don't validate the input length parameter and the returned offset value against negative values.
        /// we don't validate either the returned offset is greater than the input length.
        /// It is expected NIndex will be used with collections which always have non negative length/count. If the returned offset is negative and
        /// then used to NIndex a collection will get out of range exception which will be same affect as the validation.
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

        /// <summary>Indicates whether the current NIndex object is equal to another object of the same type.</summary>
        /// <param name="value">An object to compare with this object</param>
        public override bool Equals([NotNullWhen(true)] object? value) => value is NIndex other && _value == other._value;

        /// <summary>Indicates whether the current NIndex object is equal to another NIndex object.</summary>
        /// <param name="other">An object to compare with this object</param>
        public bool Equals(NIndex other) => _value == other._value;

        /// <summary>Returns the hash code for this instance.</summary>
        public override int GetHashCode() => _value.GetHashCode();

        /// <summary>Converts integer number to an NIndex.</summary>
        public static implicit operator NIndex(nint value) => FromStart(value);

        /// <summary>Converts native integer number to an NIndex.</summary>
        public static implicit operator NIndex(Index value) => new NIndex(value);

        /// <summary>Converts a <see cref="NIndex"/> to an <see cref="Index"/>."/></summary>
        public static explicit operator Index(NIndex value) => new Index((int)value.Value, value.IsFromEnd);

        /// <summary>Converts a <see cref="NIndex"/> to an <see cref="Index"/>."/></summary>
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
