// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Internal.TypeSystem
{
    /// <summary>
    /// A integer type used for layout calculations. Supports addition, max, min, comparison and alignup operations
    /// A custom type is used to allow the concept of an indeterminate value. (Some types representable in the
    /// type system do not have a known size. This type is used to make such sizes viral through the type layout
    /// computations)
    /// </summary>
#pragma warning disable CA1066 // IEquatable<T> implementation wouldn't be used
    public struct LayoutInt
#pragma warning restore CA1066
    {
        private int _value;

        public static LayoutInt Indeterminate = CreateIndeterminateLayoutInt();
        public static LayoutInt Zero = new LayoutInt(0);
        public static LayoutInt One = new LayoutInt(1);

        public LayoutInt(int input)
        {
            if (input < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(input));
            }
            else
            {
                _value = input;
            }
        }

        private static LayoutInt CreateIndeterminateLayoutInt()
        {
            LayoutInt output = default(LayoutInt);
            output._value = -1;
            Debug.Assert(output.IsIndeterminate);
            return output;
        }

        public bool IsIndeterminate => _value == -1;
        public int AsInt
        {
            get
            {
                if (IsIndeterminate)
                    throw new InvalidOperationException();
                return _value;
            }
        }

        public override string ToString()
        {
            return ToStringInvariant();
        }

        public string ToStringInvariant()
        {
            if (IsIndeterminate)
                return "Indeterminate";
            else
                return _value.ToStringInvariant();
        }

        public static bool operator ==(LayoutInt left, LayoutInt right)
        {
            return left._value == right._value;
        }

        public static bool operator !=(LayoutInt left, LayoutInt right)
        {
            return left._value != right._value;
        }

        public static LayoutInt operator +(LayoutInt left, LayoutInt right)
        {
            if (left.IsIndeterminate || right.IsIndeterminate)
                return Indeterminate;

            return new LayoutInt(checked(left._value + right._value));
        }

        public static LayoutInt operator -(LayoutInt left, LayoutInt right)
        {
            if (left.IsIndeterminate || right.IsIndeterminate)
                return Indeterminate;

            return new LayoutInt(checked(left._value - right._value));
        }

        public override bool Equals(object obj)
        {
            if (obj is LayoutInt)
            {
                return this == (LayoutInt)obj;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public static LayoutInt Max(LayoutInt left, LayoutInt right)
        {
            if (left.IsIndeterminate || right.IsIndeterminate)
                return Indeterminate;

            return new LayoutInt(Math.Max(left._value, right._value));
        }

        public static LayoutInt Min(LayoutInt left, LayoutInt right)
        {
            if (left.IsIndeterminate || right.IsIndeterminate)
                return Indeterminate;

            return new LayoutInt(Math.Min(left._value, right._value));
        }

        public static LayoutInt AlignUp(LayoutInt value, LayoutInt alignment, TargetDetails target)
        {
            if (value.IsIndeterminate || alignment.IsIndeterminate)
            {
                // If value is already aligned to maximum possible alignment, then whatever
                // alignment is can't change value
                if (!value.IsIndeterminate)
                {
                    if (value.AsInt.AlignUp(target.MaximumAlignment) == value.AsInt)
                        return value;
                }
                return Indeterminate;
            }

            Debug.Assert(alignment._value <= target.MaximumAlignment); // Assert that the alignment handling for indeterminate types is safe
            Debug.Assert(alignment._value >= 1 || ((value._value == 0) && (alignment._value == 0))); // Alignment to less than one doesn't make sense, except for 0 to 0 alignment

            return new LayoutInt(AlignmentHelper.AlignUp(value._value, alignment._value));
        }
    }
}
