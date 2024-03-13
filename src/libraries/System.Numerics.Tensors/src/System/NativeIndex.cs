// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System
{
    /// <summary>Represent a type can be used to NativeIndex a collection either from the start or the end.</summary>
    /// <remarks>
    /// NativeIndex is used by the C# compiler to support the new NativeIndex syntax
    /// <code>
    /// int[] someArray = new int[5] { 1, 2, 3, 4, 5 } ;
    /// int lastElement = someArray[^1]; // lastElement = 5
    /// </code>
    /// </remarks>

    public readonly struct NativeIndex : IEquatable<NativeIndex>
    {
        private readonly nint _value;

        /// <summary>Construct an NativeIndex using a value and indicating if the NativeIndex is from the start or from the end.</summary>
        /// <param name="value">The NativeIndex value. it has to be zero or positive number.</param>
        /// <param name="fromEnd">Indicating if the NativeIndex is from the start or from the end.</param>
        /// <remarks>
        /// If the NativeIndex constructed from the end, NativeIndex value 1 means pointing at the last element and NativeIndex value 0 means pointing at beyond last element.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeIndex(nint value, bool fromEnd = false)
        {
            if (value < 0)
            {
                ThrowValueArgumentOutOfRange_NeedNonNegNumException();
            }

            if (fromEnd)
                _value = ~value;
            else
                _value = value;
        }

        // The following private constructors mainly created for perf reason to avoid the checks
        private NativeIndex(nint value)
        {
            _value = value;
        }

        /// <summary>Create an NativeIndex pointing at first element.</summary>
        public static NativeIndex Start => new NativeIndex(0);

        /// <summary>Create an NativeIndex pointing at beyond last element.</summary>
        public static NativeIndex End => new NativeIndex(~0);

        /// <summary>Create an NativeIndex from the start at the position indicated by the value.</summary>
        /// <param name="value">The NativeIndex value from the start.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeIndex FromStart(nint value)
        {
            if (value < 0)
            {
                ThrowValueArgumentOutOfRange_NeedNonNegNumException();
            }

            return new NativeIndex(value);
        }

        /// <summary>Create an NativeIndex from the end at the position indicated by the value.</summary>
        /// <param name="value">The NativeIndex value from the end.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeIndex FromEnd(nint value)
        {
            if (value < 0)
            {
                ThrowValueArgumentOutOfRange_NeedNonNegNumException();
            }

            return new NativeIndex(~value);
        }

        /// <summary>Returns the NativeIndex value.</summary>
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

        /// <summary>Indicates whether the NativeIndex is from the start or the end.</summary>
        public bool IsFromEnd => _value < 0;

        /// <summary>Calculate the offset from the start using the giving collection length.</summary>
        /// <param name="length">The length of the collection that the NativeIndex will be used with. length has to be a positive value</param>
        /// <remarks>
        /// For performance reason, we don't validate the input length parameter and the returned offset value against negative values.
        /// we don't validate either the returned offset is greater than the input length.
        /// It is expected NativeIndex will be used with collections which always have non negative length/count. If the returned offset is negative and
        /// then used to NativeIndex a collection will get out of range exception which will be same affect as the validation.
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

        /// <summary>Indicates whether the current NativeIndex object is equal to another object of the same type.</summary>
        /// <param name="value">An object to compare with this object</param>
        public override bool Equals([NotNullWhen(true)] object? value) => value is NativeIndex && _value == ((NativeIndex)value)._value;

        /// <summary>Indicates whether the current NativeIndex object is equal to another NativeIndex object.</summary>
        /// <param name="other">An object to compare with this object</param>
        public bool Equals(NativeIndex other) => _value == other._value;

        // BUGBUG: FIX THIS. THE CONVERSION IS JUST TO FIX ERROR FOR NOW.
        /// <summary>Returns the hash code for this instance.</summary>
        public override int GetHashCode() => (int)_value;

        /// <summary>Converts integer number to a NativeIndex.</summary>
        public static implicit operator NativeIndex(int value) => FromStart(value);

        /// <summary>Converts native integer number to a NativeIndex.</summary>
        public static implicit operator NativeIndex(nint value) => FromStart(value);

        /// <summary>Converts the value of the current NativeIndex object to its equivalent string representation.</summary>
        public override string ToString()
        {
            if (IsFromEnd)
                return ToStringFromEnd();

            return Value.ToString();
        }

        private static void ThrowValueArgumentOutOfRange_NeedNonNegNumException()
        {
            throw new ArgumentOutOfRangeException("value", "value must be non-negative");
        }

        private string ToStringFromEnd()
        {
            Span<char> span = stackalloc char[11]; // 1 for ^ and 10 for longest possible uint value
            bool formatted = ((uint)Value).TryFormat(span.Slice(1), out int charsWritten);
            Debug.Assert(formatted);
            span[0] = '^';
            return new string(span.Slice(0, charsWritten + 1));
        }

        /// <summary>
        /// Converts Index to a NativeIndex.
        /// </summary>
        /// <param name="index">The Index to convert.</param>
        public static implicit operator NativeIndex(Index index)
        {
            return new NativeIndex(index.Value, index.IsFromEnd);
        }

        public static implicit operator int(NativeIndex index)
        {
            if (index.Value > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "can't implicitly convert NativeIndex to int when NativeIndex.Value is larger than int.MaxValue");
            }
            return (int)index.Value;
        }

        public static implicit operator nint(NativeIndex index)
        {
            return index.Value;
        }
    }
}
