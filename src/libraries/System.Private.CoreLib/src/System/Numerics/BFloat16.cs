// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

        internal byte BiasedExponent
        {
            get
            {
                ushort bits = _value;
                return ExtractBiasedExponentFromBits(bits);
            }
        }

        internal sbyte Exponent
        {
            get
            {
                return (sbyte)(BiasedExponent - ExponentBias);
            }
        }

        internal ushort Significand
        {
            get
            {
                return (ushort)(TrailingSignificand | ((BiasedExponent != 0) ? (1U << BiasedExponentShift) : 0U));
            }
        }

        internal ushort TrailingSignificand
        {
            get
            {
                ushort bits = _value;
                return ExtractTrailingSignificandFromBits(bits);
            }
        }

        internal static byte ExtractBiasedExponentFromBits(ushort bits)
        {
            return (byte)((bits >> BiasedExponentShift) & ShiftedBiasedExponentMask);
        }

        internal static ushort ExtractTrailingSignificandFromBits(ushort bits)
        {
            return (ushort)(bits & TrailingSignificandMask);
        }

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

        //
        // IAdditionOperators
        //

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        public static BFloat16 operator +(BFloat16 left, BFloat16 right) => (BFloat16)((float)left + (float)right);

        //
        // IAdditiveIdentity
        //

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static BFloat16 IAdditiveIdentity<BFloat16, BFloat16>.AdditiveIdentity => new BFloat16(PositiveZeroBits);

        //
        // IBinaryNumber
        //

        /// <inheritdoc cref="IBinaryNumber{TSelf}.AllBitsSet" />
        static BFloat16 IBinaryNumber<BFloat16>.AllBitsSet => new BFloat16(0xFFFF);

        /// <inheritdoc cref="IBinaryNumber{TSelf}.IsPow2(TSelf)" />
        public static bool IsPow2(BFloat16 value)
        {
            ushort bits = value._value;

            if ((short)bits <= 0)
            {
                // Zero and negative values cannot be powers of 2
                return false;
            }

            byte biasedExponent = ExtractBiasedExponentFromBits(bits);
            ushort trailingSignificand = ExtractTrailingSignificandFromBits(bits);

            if (biasedExponent == MinBiasedExponent)
            {
                // Subnormal values have 1 bit set when they're powers of 2
                return ushort.PopCount(trailingSignificand) == 1;
            }
            else if (biasedExponent == MaxBiasedExponent)
            {
                // NaN and Infinite values cannot be powers of 2
                return false;
            }

            // Normal values have 0 bits set when they're powers of 2
            return trailingSignificand == MinTrailingSignificand;
        }

        /// <inheritdoc cref="IBinaryNumber{TSelf}.Log2(TSelf)" />
        public static BFloat16 Log2(BFloat16 value) => (BFloat16)MathF.Log2((float)value);

        //
        // IBitwiseOperators
        //

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseAnd(TSelf, TOther)" />
        static BFloat16 IBitwiseOperators<BFloat16, BFloat16, BFloat16>.operator &(BFloat16 left, BFloat16 right)
        {
            return new BFloat16((ushort)(left._value & right._value));
        }

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseOr(TSelf, TOther)" />
        static BFloat16 IBitwiseOperators<BFloat16, BFloat16, BFloat16>.operator |(BFloat16 left, BFloat16 right)
        {
            return new BFloat16((ushort)(left._value | right._value));
        }

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_ExclusiveOr(TSelf, TOther)" />
        static BFloat16 IBitwiseOperators<BFloat16, BFloat16, BFloat16>.operator ^(BFloat16 left, BFloat16 right)
        {
            return new BFloat16((ushort)(left._value ^ right._value));
        }

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_OnesComplement(TSelf)" />
        static BFloat16 IBitwiseOperators<BFloat16, BFloat16, BFloat16>.operator ~(BFloat16 value)
        {
            return new BFloat16((ushort)(~value._value));
        }

        //
        // IDecrementOperators
        //

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        public static BFloat16 operator --(BFloat16 value)
        {
            var tmp = (float)value;
            --tmp;
            return (BFloat16)tmp;
        }

        //
        // IDivisionOperators
        //

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_Division(TSelf, TOther)" />
        public static BFloat16 operator /(BFloat16 left, BFloat16 right) => (BFloat16)((float)left / (float)right);

        //
        // IExponentialFunctions
        //

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp" />
        public static BFloat16 Exp(BFloat16 x) => (BFloat16)MathF.Exp((float)x);

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.ExpM1(TSelf)" />
        public static BFloat16 ExpM1(BFloat16 x) => (BFloat16)float.ExpM1((float)x);

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp2(TSelf)" />
        public static BFloat16 Exp2(BFloat16 x) => (BFloat16)float.Exp2((float)x);

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp2M1(TSelf)" />
        public static BFloat16 Exp2M1(BFloat16 x) => (BFloat16)float.Exp2M1((float)x);

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp10(TSelf)" />
        public static BFloat16 Exp10(BFloat16 x) => (BFloat16)float.Exp10((float)x);

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp10M1(TSelf)" />
        public static BFloat16 Exp10M1(BFloat16 x) => (BFloat16)float.Exp10M1((float)x);

        //
        // IFloatingPoint
        //

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Ceiling(TSelf)" />
        public static BFloat16 Ceiling(BFloat16 x) => (BFloat16)MathF.Ceiling((float)x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Floor(TSelf)" />
        public static BFloat16 Floor(BFloat16 x) => (BFloat16)MathF.Floor((float)x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf)" />
        public static BFloat16 Round(BFloat16 x) => (BFloat16)MathF.Round((float)x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, int)" />
        public static BFloat16 Round(BFloat16 x, int digits) => (BFloat16)MathF.Round((float)x, digits);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, MidpointRounding)" />
        public static BFloat16 Round(BFloat16 x, MidpointRounding mode) => (BFloat16)MathF.Round((float)x, mode);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, int, MidpointRounding)" />
        public static BFloat16 Round(BFloat16 x, int digits, MidpointRounding mode) => (BFloat16)MathF.Round((float)x, digits, mode);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Truncate(TSelf)" />
        public static BFloat16 Truncate(BFloat16 x) => (BFloat16)MathF.Truncate((float)x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetExponentByteCount()" />
        int IFloatingPoint<BFloat16>.GetExponentByteCount() => sizeof(sbyte);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetExponentShortestBitLength()" />
        int IFloatingPoint<BFloat16>.GetExponentShortestBitLength()
        {
            sbyte exponent = Exponent;

            if (exponent >= 0)
            {
                return (sizeof(sbyte) * 8) - sbyte.LeadingZeroCount(exponent);
            }
            else
            {
                return (sizeof(sbyte) * 8) + 1 - sbyte.LeadingZeroCount((sbyte)(~exponent));
            }
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetSignificandByteCount()" />
        int IFloatingPoint<BFloat16>.GetSignificandByteCount() => sizeof(ushort);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetSignificandBitLength()" />
        int IFloatingPoint<BFloat16>.GetSignificandBitLength() => SignificandLength;

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteExponentBigEndian(Span{byte}, out int)" />
        bool IFloatingPoint<BFloat16>.TryWriteExponentBigEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(sbyte))
            {
                sbyte exponent = Exponent;
                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), exponent);

                bytesWritten = sizeof(sbyte);
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteExponentLittleEndian(Span{byte}, out int)" />
        bool IFloatingPoint<BFloat16>.TryWriteExponentLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(sbyte))
            {
                sbyte exponent = Exponent;
                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), exponent);

                bytesWritten = sizeof(sbyte);
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteSignificandBigEndian(Span{byte}, out int)" />
        bool IFloatingPoint<BFloat16>.TryWriteSignificandBigEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(ushort))
            {
                ushort significand = Significand;

                if (BitConverter.IsLittleEndian)
                {
                    significand = BinaryPrimitives.ReverseEndianness(significand);
                }

                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), significand);

                bytesWritten = sizeof(ushort);
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteSignificandLittleEndian(Span{byte}, out int)" />
        bool IFloatingPoint<BFloat16>.TryWriteSignificandLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(ushort))
            {
                ushort significand = Significand;

                if (!BitConverter.IsLittleEndian)
                {
                    significand = BinaryPrimitives.ReverseEndianness(significand);
                }

                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), significand);

                bytesWritten = sizeof(ushort);
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        //
        // IFloatingPointConstants
        //

        /// <inheritdoc cref="IFloatingPointConstants{TSelf}.E" />
        public static BFloat16 E => new BFloat16(EBits);

        /// <inheritdoc cref="IFloatingPointConstants{TSelf}.Pi" />
        public static BFloat16 Pi => new BFloat16(PiBits);

        /// <inheritdoc cref="IFloatingPointConstants{TSelf}.Tau" />
        public static BFloat16 Tau => new BFloat16(TauBits);

        //
        // IFloatingPointIeee754
        //

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.NegativeZero" />
        public static BFloat16 NegativeZero => new BFloat16(NegativeZeroBits);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Atan2(TSelf, TSelf)" />
        public static BFloat16 Atan2(BFloat16 y, BFloat16 x) => (BFloat16)MathF.Atan2((float)y, (float)x);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Atan2Pi(TSelf, TSelf)" />
        public static BFloat16 Atan2Pi(BFloat16 y, BFloat16 x) => (BFloat16)float.Atan2Pi((float)y, (float)x);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.BitDecrement(TSelf)" />
        public static BFloat16 BitDecrement(BFloat16 x)
        {
            uint bits = x._value;

            if (!IsFinite(x))
            {
                // NaN returns NaN
                // -Infinity returns -Infinity
                // +Infinity returns MaxValue
                return (bits == PositiveInfinityBits) ? MaxValue : x;
            }

            if (bits == PositiveZeroBits)
            {
                // +0.0 returns -Epsilon
                return -Epsilon;
            }

            // Negative values need to be incremented
            // Positive values need to be decremented

            if (IsNegative(x))
            {
                bits += 1;
            }
            else
            {
                bits -= 1;
            }
            return new BFloat16((ushort)bits);
        }

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.BitIncrement(TSelf)" />
        public static BFloat16 BitIncrement(BFloat16 x)
        {
            uint bits = x._value;

            if (!IsFinite(x))
            {
                // NaN returns NaN
                // -Infinity returns MinValue
                // +Infinity returns +Infinity
                return (bits == NegativeInfinityBits) ? MinValue : x;
            }

            if (bits == NegativeZeroBits)
            {
                // -0.0 returns Epsilon
                return Epsilon;
            }

            // Negative values need to be decremented
            // Positive values need to be incremented

            if (IsNegative(x))
            {
                bits -= 1;
            }
            else
            {
                bits += 1;
            }
            return new BFloat16((ushort)bits);
        }

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.FusedMultiplyAdd(TSelf, TSelf, TSelf)" />
        public static BFloat16 FusedMultiplyAdd(BFloat16 left, BFloat16 right, BFloat16 addend) => (BFloat16)MathF.FusedMultiplyAdd((float)left, (float)right, (float)addend);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Ieee754Remainder(TSelf, TSelf)" />
        public static BFloat16 Ieee754Remainder(BFloat16 left, BFloat16 right) => (BFloat16)MathF.IEEERemainder((float)left, (float)right);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ILogB(TSelf)" />
        public static int ILogB(BFloat16 x)
        {
            // This code is based on `ilogbf` from amd/aocl-libm-ose
            // Copyright (C) 2008-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            if (!IsNormal(x)) // x is zero, subnormal, infinity, or NaN
            {
                if (IsZero(x))
                {
                    return int.MinValue;
                }

                if (!IsFinite(x)) // infinity or NaN
                {
                    return int.MaxValue;
                }

                Debug.Assert(IsSubnormal(x));
                return MinExponent - (BitOperations.TrailingZeroCount(x.TrailingSignificand) - BiasedExponentLength);
            }

            return x.Exponent;
        }

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Lerp(TSelf, TSelf, TSelf)" />
        public static BFloat16 Lerp(BFloat16 value1, BFloat16 value2, BFloat16 amount) => (BFloat16)float.Lerp((float)value1, (float)value2, (float)amount);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ReciprocalEstimate(TSelf)" />
        public static BFloat16 ReciprocalEstimate(BFloat16 x) => (BFloat16)MathF.ReciprocalEstimate((float)x);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ReciprocalSqrtEstimate(TSelf)" />
        public static BFloat16 ReciprocalSqrtEstimate(BFloat16 x) => (BFloat16)MathF.ReciprocalSqrtEstimate((float)x);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ScaleB(TSelf, int)" />
        public static BFloat16 ScaleB(BFloat16 x, int n) => (BFloat16)MathF.ScaleB((float)x, n);

        // /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Compound(TSelf, TSelf)" />
        // public static BFloat16 Compound(BFloat16 x, BFloat16 n) => (BFloat16)MathF.Compound((float)x, (float)n);

        //
        // IHyperbolicFunctions
        //

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Acosh(TSelf)" />
        public static BFloat16 Acosh(BFloat16 x) => (BFloat16)MathF.Acosh((float)x);

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Asinh(TSelf)" />
        public static BFloat16 Asinh(BFloat16 x) => (BFloat16)MathF.Asinh((float)x);

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Atanh(TSelf)" />
        public static BFloat16 Atanh(BFloat16 x) => (BFloat16)MathF.Atanh((float)x);

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Cosh(TSelf)" />
        public static BFloat16 Cosh(BFloat16 x) => (BFloat16)MathF.Cosh((float)x);

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Sinh(TSelf)" />
        public static BFloat16 Sinh(BFloat16 x) => (BFloat16)MathF.Sinh((float)x);

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Tanh(TSelf)" />
        public static BFloat16 Tanh(BFloat16 x) => (BFloat16)MathF.Tanh((float)x);

        //
        // IIncrementOperators
        //

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_Increment(TSelf)" />
        public static BFloat16 operator ++(BFloat16 value)
        {
            var tmp = (float)value;
            ++tmp;
            return (BFloat16)tmp;
        }

        //
        // ILogarithmicFunctions
        //

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log(TSelf)" />
        public static BFloat16 Log(BFloat16 x) => (BFloat16)MathF.Log((float)x);

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log(TSelf, TSelf)" />
        public static BFloat16 Log(BFloat16 x, BFloat16 newBase) => (BFloat16)MathF.Log((float)x, (float)newBase);

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log10(TSelf)" />
        public static BFloat16 Log10(BFloat16 x) => (BFloat16)MathF.Log10((float)x);

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.LogP1(TSelf)" />
        public static BFloat16 LogP1(BFloat16 x) => (BFloat16)float.LogP1((float)x);

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log2P1(TSelf)" />
        public static BFloat16 Log2P1(BFloat16 x) => (BFloat16)float.Log2P1((float)x);

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log10P1(TSelf)" />
        public static BFloat16 Log10P1(BFloat16 x) => (BFloat16)float.Log10P1((float)x);

        //
        // IModulusOperators
        //

        /// <inheritdoc cref="IModulusOperators{TSelf, TOther, TResult}.op_Modulus(TSelf, TOther)" />
        public static BFloat16 operator %(BFloat16 left, BFloat16 right) => (BFloat16)((float)left % (float)right);

        //
        // IMultiplicativeIdentity
        //

        /// <inheritdoc cref="IMultiplicativeIdentity{TSelf, TResult}.MultiplicativeIdentity" />
        public static BFloat16 MultiplicativeIdentity => new BFloat16(PositiveOneBits);

        //
        // IMultiplyOperators
        //

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_Multiply(TSelf, TOther)" />
        public static BFloat16 operator *(BFloat16 left, BFloat16 right) => (BFloat16)((float)left * (float)right);

        //
        // INumber
        //

        /// <inheritdoc cref="INumber{TSelf}.Clamp(TSelf, TSelf, TSelf)" />
        public static BFloat16 Clamp(BFloat16 value, BFloat16 min, BFloat16 max) => (BFloat16)Math.Clamp((float)value, (float)min, (float)max);

        /// <inheritdoc cref="INumber{TSelf}.CopySign(TSelf, TSelf)" />
        public static BFloat16 CopySign(BFloat16 value, BFloat16 sign)
        {
            // This method is required to work for all inputs,
            // including NaN, so we operate on the raw bits.
            uint xbits = value._value;
            uint ybits = sign._value;

            // Remove the sign from x, and remove everything but the sign from y
            // Then, simply OR them to get the correct sign
            return new BFloat16((ushort)((xbits & ~SignMask) | (ybits & SignMask)));
        }

        /// <inheritdoc cref="INumber{TSelf}.Max(TSelf, TSelf)" />
        public static BFloat16 Max(BFloat16 x, BFloat16 y) => (BFloat16)MathF.Max((float)x, (float)y);

        /// <inheritdoc cref="INumber{TSelf}.MaxNumber(TSelf, TSelf)" />
        public static BFloat16 MaxNumber(BFloat16 x, BFloat16 y)
        {
            // This matches the IEEE 754:2019 `maximumNumber` function
            //
            // It does not propagate NaN inputs back to the caller and
            // otherwise returns the larger of the inputs. It
            // treats +0 as larger than -0 as per the specification.

            if (x != y)
            {
                if (!IsNaN(y))
                {
                    return y < x ? x : y;
                }

                return x;
            }

            return IsNegative(y) ? x : y;
        }

        /// <inheritdoc cref="INumber{TSelf}.Min(TSelf, TSelf)" />
        public static BFloat16 Min(BFloat16 x, BFloat16 y) => (BFloat16)MathF.Min((float)x, (float)y);

        /// <inheritdoc cref="INumber{TSelf}.MinNumber(TSelf, TSelf)" />
        public static BFloat16 MinNumber(BFloat16 x, BFloat16 y)
        {
            // This matches the IEEE 754:2019 `minimumNumber` function
            //
            // It does not propagate NaN inputs back to the caller and
            // otherwise returns the larger of the inputs. It
            // treats +0 as larger than -0 as per the specification.

            if (x != y)
            {
                if (!IsNaN(y))
                {
                    return x < y ? x : y;
                }

                return x;
            }

            return IsNegative(x) ? x : y;
        }

        /// <inheritdoc cref="INumber{TSelf}.Sign(TSelf)" />
        public static int Sign(BFloat16 value)
        {
            if (IsNaN(value))
            {
                throw new ArithmeticException(SR.Arithmetic_NaN);
            }

            if (IsZero(value))
            {
                return 0;
            }
            else if (IsNegative(value))
            {
                return -1;
            }

            return +1;
        }

        //
        // INumberBase
        //

        /// <inheritdoc cref="INumberBase{TSelf}.One" />
        public static BFloat16 One => new BFloat16(PositiveOneBits);

        /// <inheritdoc cref="INumberBase{TSelf}.Radix" />
        static int INumberBase<BFloat16>.Radix => 2;

        /// <inheritdoc cref="INumberBase{TSelf}.Zero" />
        public static BFloat16 Zero => new BFloat16(PositiveZeroBits);

        /// <inheritdoc cref="INumberBase{TSelf}.Abs(TSelf)" />
        public static BFloat16 Abs(BFloat16 value) => new BFloat16((ushort)(value._value & ~SignMask));

        /// <inheritdoc cref="INumberBase{TSelf}.CreateChecked{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BFloat16 CreateChecked<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            BFloat16 result;

            if (typeof(TOther) == typeof(BFloat16))
            {
                result = (BFloat16)(object)value;
            }
            else if (!TryConvertFrom(value, out result) && !TOther.TryConvertToChecked(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateSaturating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BFloat16 CreateSaturating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            BFloat16 result;

            if (typeof(TOther) == typeof(BFloat16))
            {
                result = (BFloat16)(object)value;
            }
            else if (!TryConvertFrom(value, out result) && !TOther.TryConvertToSaturating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateTruncating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BFloat16 CreateTruncating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            BFloat16 result;

            if (typeof(TOther) == typeof(BFloat16))
            {
                result = (BFloat16)(object)value;
            }
            else if (!TryConvertFrom(value, out result) && !TOther.TryConvertToTruncating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsCanonical(TSelf)" />
        static bool INumberBase<BFloat16>.IsCanonical(BFloat16 value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsComplexNumber(TSelf)" />
        static bool INumberBase<BFloat16>.IsComplexNumber(BFloat16 value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsEvenInteger(TSelf)" />
        public static bool IsEvenInteger(BFloat16 value) => float.IsEvenInteger((float)value);

        /// <inheritdoc cref="INumberBase{TSelf}.IsImaginaryNumber(TSelf)" />
        static bool INumberBase<BFloat16>.IsImaginaryNumber(BFloat16 value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsInteger(TSelf)" />
        public static bool IsInteger(BFloat16 value) => float.IsInteger((float)value);

        /// <inheritdoc cref="INumberBase{TSelf}.IsOddInteger(TSelf)" />
        public static bool IsOddInteger(BFloat16 value) => float.IsOddInteger((float)value);

        /// <inheritdoc cref="INumberBase{TSelf}.IsPositive(TSelf)" />
        public static bool IsPositive(BFloat16 value) => (short)(value._value) >= 0;

        /// <inheritdoc cref="INumberBase{TSelf}.IsRealNumber(TSelf)" />
        public static bool IsRealNumber(BFloat16 value)
        {
            // A NaN will never equal itself so this is an
            // easy and efficient way to check for a real number.

#pragma warning disable CS1718
            return value == value;
#pragma warning restore CS1718
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsZero(TSelf)" />
        static bool INumberBase<BFloat16>.IsZero(BFloat16 value) => IsZero(value);

        /// <inheritdoc cref="INumberBase{TSelf}.MaxMagnitude(TSelf, TSelf)" />
        public static BFloat16 MaxMagnitude(BFloat16 x, BFloat16 y) => (BFloat16)MathF.MaxMagnitude((float)x, (float)y);

        /// <inheritdoc cref="INumberBase{TSelf}.MaxMagnitudeNumber(TSelf, TSelf)" />
        public static BFloat16 MaxMagnitudeNumber(BFloat16 x, BFloat16 y)
        {
            // This matches the IEEE 754:2019 `maximumMagnitudeNumber` function
            //
            // It does not propagate NaN inputs back to the caller and
            // otherwise returns the input with a larger magnitude.
            // It treats +0 as larger than -0 as per the specification.

            BFloat16 ax = Abs(x);
            BFloat16 ay = Abs(y);

            if ((ax > ay) || IsNaN(ay))
            {
                return x;
            }

            if (ax == ay)
            {
                return IsNegative(x) ? y : x;
            }

            return y;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.MinMagnitude(TSelf, TSelf)" />
        public static BFloat16 MinMagnitude(BFloat16 x, BFloat16 y) => (BFloat16)MathF.MinMagnitude((float)x, (float)y);

        /// <inheritdoc cref="INumberBase{TSelf}.MinMagnitudeNumber(TSelf, TSelf)" />
        public static BFloat16 MinMagnitudeNumber(BFloat16 x, BFloat16 y)
        {
            // This matches the IEEE 754:2019 `minimumMagnitudeNumber` function
            //
            // It does not propagate NaN inputs back to the caller and
            // otherwise returns the input with a larger magnitude.
            // It treats +0 as larger than -0 as per the specification.

            BFloat16 ax = Abs(x);
            BFloat16 ay = Abs(y);

            if ((ax < ay) || IsNaN(ay))
            {
                return x;
            }

            if (ax == ay)
            {
                return IsNegative(x) ? x : y;
            }

            return y;
        }

    }
}
