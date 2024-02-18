// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics
{
    /// <summary>
    /// Represents a shortened (16-bit) version of 32 bit floating-point value (<see cref="float"/>).
    /// </summary>
    public readonly struct BFloat16
        : IComparable,
          IComparable<BFloat16>,
          IEquatable<BFloat16>
    {
        private const ushort EpsilonBits = 0x0001;

        private const ushort MinValueBits = 0xFF7F;
        private const ushort MaxValueBits = 0x7F7F;

        /// <summary>
        /// Represents the smallest positive <see cref="BFloat16"/> value that is greater than zero.
        /// </summary>
        public static BFloat16 Epsilon => new BFloat16(EpsilonBits);

        /// <summary>
        /// Represents the smallest possible value of <see cref="BFloat16"/>.
        /// </summary>
        public static BFloat16 MinValue => new BFloat16(MinValueBits);

        /// <summary>
        /// Represents the largest possible value of <see cref="BFloat16"/>.
        /// </summary>
        public static BFloat16 MaxValue => new BFloat16(MaxValueBits);

        internal readonly ushort _value;

        internal BFloat16(ushort value) => _value = value;

        // Casting

        /// <summary>Explicitly converts a <see cref="float" /> value to its nearest representable <see cref="BFloat16"/> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="BFloat16"/> value.</returns>
        public static explicit operator BFloat16(float value)
        {
            uint bits = BitConverter.SingleToUInt32Bits(value);
            uint upper = bits >> 16;
            // Only do rounding for finite numbers
            if (float.IsFinite(value))
            {
                uint lower = bits & 0xFFFF;
                uint sign = upper & 0x8000;
                // Strip sign from upper
                upper &= 0x7FFF;
                // Determine the increment for rounding
                // When upper is even, midpoint (0x8000) will tie to no increment, which is effectively a decrement of lower
                uint lowerShift = (~upper) & (lower >> 15) & 1; // Upper is even & lower>=0x8000 (not 0)
                lower -= lowerShift;
                uint increment = lower >> 15;
                // Do the increment, MaxValue will be correctly increased to Infinity
                upper += increment;
                // Put back sign with upper bits and done
                upper |= sign;
            }
            return new BFloat16((ushort)upper);
        }

        /// <summary>Explicitly converts a <see cref="double" /> value to its nearest representable <see cref="BFloat16"/> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="BFloat16"/> value.</returns>
        public static explicit operator BFloat16(double value) => (BFloat16)(float)value;

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to its nearest representable <see cref="float"/> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="float"/> value.</returns>

        public static explicit operator float(BFloat16 value) => BitConverter.Int32BitsToSingle(value._value << 16);

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to its nearest representable <see cref="double"/> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="double"/> value.</returns>
        public static explicit operator double(BFloat16 value) => (double)(float)value;

        // BFloat is effectively a truncation of Single, with lower 16 bits of mantissa truncated.
        // Delegating all operations to Single should be correct and effective.

        // Comparison

        /// <summary>
        /// Compares this object to another object, returning an integer that indicates the relationship.
        /// </summary>
        /// <returns>A value less than zero if this is less than <paramref name="obj"/>, zero if this is equal to <paramref name="obj"/>, or a value greater than zero if this is greater than <paramref name="obj"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="obj"/> is not of type <see cref="BFloat16"/>.</exception>
        public int CompareTo(object? obj)
        {
            if (obj is not BFloat16 other)
            {
                return (obj is null) ? 1 : throw new ArgumentException(SR.Arg_MustBeBFloat16);
            }
            return CompareTo(other);
        }

        /// <summary>
        /// Compares this object to another object, returning an integer that indicates the relationship.
        /// </summary>
        /// <returns>A value less than zero if this is less than <paramref name="other"/>, zero if this is equal to <paramref name="other"/>, or a value greater than zero if this is greater than <paramref name="other"/>.</returns>
        public int CompareTo(BFloat16 other) => ((float)this).CompareTo((float)other);

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther, TResult}.op_Equality(TSelf, TOther)" />
        public static bool operator ==(BFloat16 left, BFloat16 right) => (float)left == (float)right;

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther, TResult}.op_Inequality(TSelf, TOther)" />
        public static bool operator !=(BFloat16 left, BFloat16 right) => (float)left != (float)right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_LessThan(TSelf, TOther)" />
        public static bool operator <(BFloat16 left, BFloat16 right) => (float)left < (float)right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_GreaterThan(TSelf, TOther)" />
        public static bool operator >(BFloat16 left, BFloat16 right) => (float)left > (float)right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_LessThanOrEqual(TSelf, TOther)" />
        public static bool operator <=(BFloat16 left, BFloat16 right) => (float)left <= (float)right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_GreaterThanOrEqual(TSelf, TOther)" />
        public static bool operator >=(BFloat16 left, BFloat16 right) => (float)left >= (float)right;

        // Equality

        /// <summary>
        /// Returns a value that indicates whether this instance is equal to a specified <paramref name="other"/> value.
        /// </summary>
        public bool Equals(BFloat16 other) => ((float)this).Equals((float)other);

        /// <summary>
        /// Returns a value that indicates whether this instance is equal to a specified <paramref name="obj"/>.
        /// </summary>
        public override bool Equals(object? obj) => obj is BFloat16 other && Equals(other);

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        public override int GetHashCode() => ((float)this).GetHashCode();

        /// <summary>
        /// Returns a string representation of the current value.
        /// </summary>
        public override string ToString() => ((float)this).ToString();
    }
}
