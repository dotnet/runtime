// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Numerics
{
    /// <summary>
    /// Represents a shortened (16-bit) version of 32 bit floating-point value (<see cref="float"/>).
    /// </summary>
    public readonly struct BFloat16
        : IComparable,
          ISpanFormattable,
          IComparable<BFloat16>,
          IEquatable<BFloat16>,
          IBinaryFloatingPointIeee754<BFloat16>,
          IMinMaxValue<BFloat16>,
          IUtf8SpanFormattable,
          IBinaryFloatParseAndFormatInfo<BFloat16>
    {
        // Constants for manipulating the private bit-representation

        internal const ushort SignMask = 0x8000;
        internal const int SignShift = 15;
        internal const byte ShiftedSignMask = SignMask >> SignShift;

        internal const ushort BiasedExponentMask = 0x7F80;
        internal const int BiasedExponentShift = 7;
        internal const int BiasedExponentLength = 8;
        internal const byte ShiftedBiasedExponentMask = BiasedExponentMask >> BiasedExponentShift;

        internal const ushort TrailingSignificandMask = 0x007F;

        internal const byte MinSign = 0;
        internal const byte MaxSign = 1;

        internal const byte MinBiasedExponent = 0x00;
        internal const byte MaxBiasedExponent = 0xFF;

        internal const byte ExponentBias = 127;

        internal const sbyte MinExponent = -126;
        internal const sbyte MaxExponent = +127;

        internal const ushort MinTrailingSignificand = 0x0000;
        internal const ushort MaxTrailingSignificand = 0x007F;

        internal const int TrailingSignificandLength = 7;
        internal const int SignificandLength = TrailingSignificandLength + 1;

        // Constants representing the private bit-representation for various default values

        private const ushort PositiveZeroBits = 0x0000;
        private const ushort NegativeZeroBits = 0x8000;

        private const ushort EpsilonBits = 0x0001;

        private const ushort PositiveInfinityBits = 0x7F80;
        private const ushort NegativeInfinityBits = 0xFF80;

        private const ushort PositiveQNaNBits = 0x7FC0;
        private const ushort NegativeQNaNBits = 0xFFC0;

        private const ushort MinValueBits = 0xFF7F;
        private const ushort MaxValueBits = 0x7F7F;

        private const ushort PositiveOneBits = 0x3F80;
        private const ushort NegativeOneBits = 0xBF80;

        private const ushort SmallestNormalBits = 0x0080;

        private const ushort EBits = 0x402E;
        private const ushort PiBits = 0x4049;
        private const ushort TauBits = 0x40C9;

        // Well-defined and commonly used values

        public static BFloat16 Epsilon => new BFloat16(EpsilonBits);

        public static BFloat16 PositiveInfinity => new BFloat16(PositiveInfinityBits);

        public static BFloat16 NegativeInfinity => new BFloat16(NegativeInfinityBits);

        public static BFloat16 NaN => new BFloat16(NegativeQNaNBits);

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MinValue" />
        public static BFloat16 MinValue => new BFloat16(MinValueBits);

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MaxValue" />
        public static BFloat16 MaxValue => new BFloat16(MaxValueBits);

        internal readonly ushort _value;

        internal BFloat16(ushort value) => _value = value;

        // INumberBase

        /// <summary>Determines whether the specified value is finite (zero, subnormal, or normal).</summary>
        /// <remarks>This effectively checks the value is not NaN and not infinite.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFinite(BFloat16 value)
        {
            uint bits = value._value;
            return (~bits & PositiveInfinityBits) != 0;
        }

        /// <summary>Determines whether the specified value is infinite.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInfinity(BFloat16 value)
        {
            uint bits = value._value;
            return (bits & ~SignMask) == PositiveInfinityBits;
        }

        /// <summary>Determines whether the specified value is NaN.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNaN(BFloat16 value)
        {
            uint bits = value._value;
            return (bits & ~SignMask) > PositiveInfinityBits;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsNaNOrZero(BFloat16 value)
        {
            uint bits = value._value;
            return ((bits - 1) & ~SignMask) >= PositiveInfinityBits;
        }

        /// <summary>Determines whether the specified value is negative.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNegative(BFloat16 value)
        {
            return (short)(value._value) < 0;
        }

        /// <summary>Determines whether the specified value is negative infinity.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNegativeInfinity(BFloat16 value)
        {
            return value._value == NegativeInfinityBits;
        }

        /// <summary>Determines whether the specified value is normal (finite, but not zero or subnormal).</summary>
        /// <remarks>This effectively checks the value is not NaN, not infinite, not subnormal, and not zero.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNormal(BFloat16 value)
        {
            uint bits = value._value;
            return (ushort)((bits & ~SignMask) - SmallestNormalBits) < (PositiveInfinityBits - SmallestNormalBits);
        }

        /// <summary>Determines whether the specified value is positive infinity.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPositiveInfinity(BFloat16 value)
        {
            return value._value == PositiveInfinityBits;
        }

        /// <summary>Determines whether the specified value is subnormal (finite, but not zero or normal).</summary>
        /// <remarks>This effectively checks the value is not NaN, not infinite, not normal, and not zero.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSubnormal(BFloat16 value)
        {
            uint bits = value._value;
            return (ushort)((bits & ~SignMask) - 1) < MaxTrailingSignificand;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsZero(BFloat16 value)
        {
            uint bits = value._value;
            return (bits & ~SignMask) == 0;
        }

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

        //
        // Explicit Convert To BFloat16
        //

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
                // Determine the increment for rounding
                // When upper is even, midpoint (0x8000) will tie to no increment, which is effectively a decrement of lower
                uint lowerShift = (~upper) & (lower >> 15) & 1; // Upper is even & lower>=0x8000 (not 0)
                lower -= lowerShift;
                uint increment = lower >> 15;
                // Do the increment, MaxValue will be correctly increased to Infinity
                upper += increment;
            }
            return new BFloat16((ushort)upper);
        }

        /// <summary>Explicitly converts a <see cref="double" /> value to its nearest representable <see cref="BFloat16"/> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="BFloat16"/> value.</returns>
        public static explicit operator BFloat16(double value) => (BFloat16)(float)value;

        //
        // Explicit Convert From BFloat16
        //

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to its nearest representable <see cref="float"/> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="float"/> value.</returns>

        public static explicit operator float(BFloat16 value) => BitConverter.Int32BitsToSingle(value._value << 16);

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to its nearest representable <see cref="double"/> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="double"/> value.</returns>
        public static explicit operator double(BFloat16 value) => (double)(float)value;
    }
}
