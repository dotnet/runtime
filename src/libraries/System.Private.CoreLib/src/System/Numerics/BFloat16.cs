// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Reflection.Emit.TypeNameBuilder;

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
        private const NumberStyles DefaultParseStyle = NumberStyles.Float | NumberStyles.AllowThousands;

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

        // private const ushort PositiveQNaNBits = 0x7FC0;
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

        /// <summary>
        /// Parses a <see cref="BFloat16"/> from a <see cref="string"/> in the default parse style.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <returns>The equivalent <see cref="BFloat16"/> value representing the input string. If the input exceeds BFloat16's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. </returns>
        public static BFloat16 Parse(string s) => Parse(s, DefaultParseStyle, provider: null);

        /// <summary>
        /// Parses a <see cref="BFloat16"/> from a <see cref="string"/> in the given <see cref="NumberStyles"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <returns>The equivalent <see cref="BFloat16"/> value representing the input string. If the input exceeds BFloat16's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. </returns>
        public static BFloat16 Parse(string s, NumberStyles style) => Parse(s, style, provider: null);

        /// <summary>
        /// Parses a <see cref="BFloat16"/> from a <see cref="string"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="provider">A format provider.</param>
        /// <returns>The equivalent <see cref="BFloat16"/> value representing the input string. If the input exceeds BFloat16's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. </returns>
        public static BFloat16 Parse(string s, IFormatProvider? provider) => Parse(s, DefaultParseStyle, provider);

        /// <summary>
        /// Parses a <see cref="BFloat16"/> from a <see cref="string"/> with the given <see cref="NumberStyles"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider.</param>
        /// <returns>The equivalent <see cref="BFloat16"/> value representing the input string. If the input exceeds BFloat16's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. </returns>
        public static BFloat16 Parse(string s, NumberStyles style = DefaultParseStyle, IFormatProvider? provider = null)
        {
            if (s is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            return Parse(s.AsSpan(), style, provider);
        }

        /// <summary>
        /// Parses a <see cref="BFloat16"/> from a <see cref="ReadOnlySpan{Char}"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider. </param>
        /// <returns>The equivalent <see cref="BFloat16"/> value representing the input string. If the input exceeds BFloat16's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. </returns>
        public static BFloat16 Parse(ReadOnlySpan<char> s, NumberStyles style = DefaultParseStyle, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Number.ParseFloat<char, BFloat16>(s, style, NumberFormatInfo.GetInstance(provider));
        }

        /// <summary>
        /// Tries to parse a <see cref="BFloat16"/> from a <see cref="string"/> in the default parse style.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="result">The equivalent <see cref="BFloat16"/> value representing the input string if the parse was successful. If the input exceeds BFloat16's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. If the parse was unsuccessful, a default <see cref="BFloat16"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, out BFloat16 result) => TryParse(s, DefaultParseStyle, provider: null, out result);

        /// <summary>
        /// Tries to parse a <see cref="BFloat16"/> from a <see cref="ReadOnlySpan{Char}"/> in the default parse style.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="result">The equivalent <see cref="BFloat16"/> value representing the input string if the parse was successful. If the input exceeds BFloat16's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. If the parse was unsuccessful, a default <see cref="BFloat16"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, out BFloat16 result) => TryParse(s, DefaultParseStyle, provider: null, out result);

        /// <summary>Tries to convert a UTF-8 character span containing the string representation of a number to its <see cref="BFloat16"/> number equivalent.</summary>
        /// <param name="utf8Text">A read-only UTF-8 character span that contains the number to convert.</param>
        /// <param name="result">When this method returns, contains a <see cref="BFloat16"/> number equivalent of the numeric value or symbol contained in <paramref name="utf8Text" /> if the conversion succeeded or zero if the conversion failed. The conversion fails if the <paramref name="utf8Text" /> is <see cref="ReadOnlySpan{T}.Empty" /> or is not in a valid format. This parameter is passed uninitialized; any value originally supplied in result will be overwritten.</param>
        /// <returns><c>true</c> if <paramref name="utf8Text" /> was converted successfully; otherwise, false.</returns>
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, out BFloat16 result) => TryParse(utf8Text, DefaultParseStyle, provider: null, out result);

        /// <summary>
        /// Tries to parse a <see cref="BFloat16"/> from a <see cref="string"/> with the given <see cref="NumberStyles"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider. </param>
        /// <param name="result">The equivalent <see cref="BFloat16"/> value representing the input string if the parse was successful. If the input exceeds BFloat16's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. If the parse was unsuccessful, a default <see cref="BFloat16"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out BFloat16 result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);

            if (s == null)
            {
                result = Zero;
                return false;
            }
            return Number.TryParseFloat(s.AsSpan(), style, NumberFormatInfo.GetInstance(provider), out result);
        }

        /// <summary>
        /// Tries to parse a <see cref="BFloat16"/> from a <see cref="ReadOnlySpan{Char}"/> with the given <see cref="NumberStyles"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider. </param>
        /// <param name="result">The equivalent <see cref="BFloat16"/> value representing the input string if the parse was successful. If the input exceeds BFloat16's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. If the parse was unsuccessful, a default <see cref="BFloat16"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out BFloat16 result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Number.TryParseFloat(s, style, NumberFormatInfo.GetInstance(provider), out result);
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
        public override string ToString() => Number.FormatFloat(this, null, NumberFormatInfo.CurrentInfo);

        /// <summary>
        /// Returns a string representation of the current value using the specified <paramref name="format"/>.
        /// </summary>
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
        {
            return Number.FormatFloat(this, format, NumberFormatInfo.CurrentInfo);
        }

        /// <summary>
        /// Returns a string representation of the current value with the specified <paramref name="provider"/>.
        /// </summary>
        public string ToString(IFormatProvider? provider)
        {
            return Number.FormatFloat(this, null, NumberFormatInfo.GetInstance(provider));
        }

        /// <summary>
        /// Returns a string representation of the current value using the specified <paramref name="format"/> and <paramref name="provider"/>.
        /// </summary>
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider)
        {
            return Number.FormatFloat(this, format, NumberFormatInfo.GetInstance(provider));
        }

        /// <summary>
        /// Tries to format the value of the current BFloat16 instance into the provided span of characters.
        /// </summary>
        /// <param name="destination">When this method returns, this instance's value formatted as a span of characters.</param>
        /// <param name="charsWritten">When this method returns, the number of characters that were written in <paramref name="destination"/>.</param>
        /// <param name="format">A span containing the characters that represent a standard or custom format string that defines the acceptable format for <paramref name="destination"/>.</param>
        /// <param name="provider">An optional object that supplies culture-specific formatting information for <paramref name="destination"/>.</param>
        /// <returns></returns>
        public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatFloat(this, format, NumberFormatInfo.GetInstance(provider), destination, out charsWritten);
        }

        /// <inheritdoc cref="IUtf8SpanFormattable.TryFormat" />
        public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatFloat(this, format, NumberFormatInfo.GetInstance(provider), utf8Destination, out bytesWritten);
        }

        //
        // Explicit Convert To BFloat16
        //

        /// <summary>Explicitly converts a <see cref="char" /> value to its nearest representable <see cref="BFloat16"/> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="BFloat16"/> value.</returns>
        public static explicit operator BFloat16(char value) => (BFloat16)(float)value;

        /// <summary>Explicitly converts a <see cref="decimal" /> value to its nearest representable <see cref="BFloat16"/> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="BFloat16"/> value.</returns>
        public static explicit operator BFloat16(decimal value) => (BFloat16)(float)value;

        /// <summary>Explicitly converts a <see cref="double" /> value to its nearest representable <see cref="BFloat16"/> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="BFloat16"/> value.</returns>
        public static explicit operator BFloat16(double value)
        {
            // See explaination of the algorithm at Half.operator Half(float)

            // Minimum exponent for rounding
            const ulong MinExp = 0x3810_0000_0000_0000u;
            // Exponent displacement #1
            const ulong Exponent942 = 0x3ae0_0000_0000_0000u;
            // Exponent mask
            const ulong SingleBiasedExponentMask = double.BiasedExponentMask;
            // Exponent displacement #2
            const ulong Exponent45 = 0x02D0_0000_0000_0000u;
            // The maximum infinitely precise value that will round down to MaxValue
            const double MaxBFloat16ValueBelowInfinity = 3.39617752923046E+38;
            // Mask for exponent bits in BFloat16
            const ulong ExponentMask = BiasedExponentMask;
            ulong bitValue = BitConverter.DoubleToUInt64Bits(value);
            // Extract sign bit
            ulong sign = (bitValue & double.SignMask) >> 48;
            // Detecting NaN (~0u if a is not NaN)
            ulong realMask = double.IsNaN(value) ? 0uL : ~0uL;
            // Clear sign bit
            value = double.Abs(value);
            // Rectify values that are Infinity in BFloat16. (float.Min now emits vminps instruction if one of two arguments is a constant)
            value = double.Min(MaxBFloat16ValueBelowInfinity, value);
            // Rectify lower exponent
            ulong exponentOffset0 = BitConverter.DoubleToUInt64Bits(double.Max(value, BitConverter.UInt64BitsToDouble(MinExp)));
            // Extract exponent
            exponentOffset0 &= SingleBiasedExponentMask;
            // Add exponent by 45
            exponentOffset0 += Exponent45;
            // Round Single into BFloat16's precision (NaN also gets modified here, just setting the MSB of fraction)
            value += BitConverter.UInt64BitsToDouble(exponentOffset0);
            bitValue = BitConverter.DoubleToUInt64Bits(value);
            // Only exponent bits will be modified if NaN
            ulong maskedBFloat16ExponentForNaN = ~realMask & ExponentMask;
            // Subtract exponent by 942
            bitValue -= Exponent942;
            // Shift bitValue right by 45 bits to match the boundary of exponent part and fraction part.
            ulong newExponent = bitValue >> 45;
            // Clear the fraction parts if the value was NaN.
            bitValue &= realMask;
            // Merge the exponent part with fraction part, and add the exponent part and fraction part's overflow.
            bitValue += newExponent;
            // Clear exponents if value is NaN
            bitValue &= ~maskedBFloat16ExponentForNaN;
            // Merge sign bit with possible NaN exponent
            ulong signAndMaskedExponent = maskedBFloat16ExponentForNaN | sign;
            // Merge sign bit and possible NaN exponent
            bitValue |= signAndMaskedExponent;
            // The final result
            return new BFloat16((ushort)bitValue);
        }

        /// <summary>
        /// Rounds a number to shorter length with the midpoint-to-even rule.
        /// </summary>
        /// <typeparam name="TInteger">The integer type to operate with.</typeparam>
        /// <param name="value">The payload number. Can be either actual unsigned integer, or (non-NaN) IEEE754 binary fp type.</param>
        /// <param name="trailingLength">The length of trailing bits to round up. Should be constant.</param>
        /// <returns>Rounded payload bits, right shifted by <paramref name="trailingLength"/> and aligns to LSB.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TInteger RoundMidpointToEven<TInteger>(TInteger value, int trailingLength)
            where TInteger : unmanaged, IBinaryInteger<TInteger>
        {
            TInteger lower = value & ((TInteger.One << trailingLength) - TInteger.One);
            TInteger upper = value >>> trailingLength;

            // Determine the increment for rounding
            // When upper is even, midpoint will tie to no increment, which is effectively a decrement of lower
            TInteger lowerShift = (~upper) & (lower >>> (trailingLength - 1)) & TInteger.One; // Upper is even & lower>=midpoint (not 0)
            lower -= lowerShift;
            TInteger increment = lower >>> (trailingLength - 1);
            // Do the increment, MaxValue will be correctly increased to Infinity
            upper += increment;

            return upper;
        }

        private static unsafe BFloat16 RoundFromSigned<TInteger>(TInteger value)
            where TInteger : unmanaged, IBinaryInteger<TInteger>, ISignedNumber<TInteger>
        {
            bool sign = TInteger.IsNegative(value);
            TInteger abs = TInteger.IsNegative(value) ? -value : value;

            int scale = int.CreateTruncating(TInteger.LeadingZeroCount(abs));
            TInteger alignedValue = abs << scale;
            TInteger significandBits = RoundMidpointToEven(alignedValue, sizeof(TInteger) * 8 - SignificandLength);

            // Leverage FPU to calculate the value significandBits * 2^(size-SignificandLength-scale), for proper handling of 0 and carrying
            // Use int->float conversion which usually has better FPU support
            float significand = (float)int.CreateTruncating(significandBits);
            // Craft the value 2^(size-SignificandLength-scale)
            float scaleFactor = float.CreateSingle(sign, (byte)(sizeof(TInteger) * 8 - SignificandLength - scale + float.ExponentBias), 0);
            float roundedValue = significand * scaleFactor;

            uint roundedValueBits = BitConverter.SingleToUInt32Bits(roundedValue);
            Debug.Assert((ushort)roundedValueBits == 0); // The value should be properly rounded
            return new BFloat16((ushort)(roundedValueBits >> 16));
        }

        /// <summary>Explicitly converts a <see cref="short" /> value to its nearest representable <see cref="BFloat16"/> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="BFloat16"/> value.</returns>
        public static explicit operator BFloat16(short value) => (BFloat16)(float)value;

        /// <summary>Explicitly converts a <see cref="Half" /> value to its nearest representable <see cref="BFloat16"/> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="BFloat16"/> value.</returns>
        public static explicit operator BFloat16(Half value) => (BFloat16)(float)value;

        /// <summary>Explicitly converts a <see cref="int" /> value to its nearest representable <see cref="BFloat16"/> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="BFloat16"/> value.</returns>
        public static explicit operator BFloat16(int value) => RoundFromSigned(value);

        /// <summary>Explicitly converts a <see cref="long" /> value to its nearest representable <see cref="BFloat16"/> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="BFloat16"/> value.</returns>
        public static explicit operator BFloat16(long value) => RoundFromSigned(value);

        /// <summary>Explicitly converts a <see cref="Int128" /> value to its nearest representable <see cref="BFloat16"/> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="BFloat16"/> value.</returns>
        public static explicit operator BFloat16(Int128 value) => RoundFromSigned(value);

        /// <summary>Explicitly converts a <see cref="nint" /> value to its nearest representable <see cref="BFloat16"/> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="BFloat16"/> value.</returns>
        public static explicit operator BFloat16(nint value) => RoundFromSigned(value);

        /// <summary>Explicitly converts a <see cref="float" /> value to its nearest representable <see cref="BFloat16"/> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="BFloat16"/> value.</returns>
        public static explicit operator BFloat16(float value)
        {
            uint bits = BitConverter.SingleToUInt32Bits(value);
            uint roundedBits = RoundMidpointToEven(bits, 16);

            // Only do rounding for non-NaN
            return new BFloat16((ushort)(!float.IsNaN(value) ? roundedBits : (bits >> 16)));
        }

        private static unsafe BFloat16 RoundFromUnsigned<TInteger>(TInteger value)
            where TInteger : unmanaged, IBinaryInteger<TInteger>, IUnsignedNumber<TInteger>
        {
            int scale = int.CreateTruncating(TInteger.LeadingZeroCount(value));
            TInteger alignedValue = value << scale;
            TInteger significandBits = RoundMidpointToEven(alignedValue, sizeof(TInteger) * 8 - SignificandLength);

            // Leverage FPU to calculate the value significandBits * 2^(size-SignificandLength-scale), for proper handling of 0 and carrying
            // Use int->float conversion which usually has better FPU support
            float significand = (float)int.CreateTruncating(significandBits);
            // Craft the value 2^(size-SignificandLength-scale)
            float scaleFactor = float.CreateSingle(false, (byte)(sizeof(TInteger) * 8 - SignificandLength - scale + float.ExponentBias), 0);
            float roundedValue = significand * scaleFactor;

            uint roundedValueBits = BitConverter.SingleToUInt32Bits(roundedValue);
            Debug.Assert((ushort)roundedValueBits == 0); // The value should be properly rounded
            return new BFloat16((ushort)(roundedValueBits >> 16));
        }

        /// <summary>Explicitly converts a <see cref="ushort" /> value to its nearest representable <see cref="BFloat16"/> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="BFloat16"/> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator BFloat16(ushort value) => (BFloat16)(float)value;

        /// <summary>Explicitly converts a <see cref="uint" /> value to its nearest representable <see cref="BFloat16"/> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="BFloat16"/> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator BFloat16(uint value) => RoundFromUnsigned(value);

        /// <summary>Explicitly converts a <see cref="ulong" /> value to its nearest representable <see cref="BFloat16"/> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="BFloat16"/> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator BFloat16(ulong value) => RoundFromUnsigned(value);

        /// <summary>Explicitly converts a <see cref="nuint" /> value to its nearest representable <see cref="BFloat16"/> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="BFloat16"/> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator BFloat16(nuint value) => RoundFromUnsigned(value);

        /// <summary>Explicitly converts a <see cref="UInt128" /> value to its nearest representable <see cref="BFloat16"/> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="BFloat16"/> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator BFloat16(UInt128 value) => RoundFromUnsigned(value);

        //
        // Explicit Convert From BFloat16
        //

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to its nearest representable <see cref="byte" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="byte" /> value.</returns>
        public static explicit operator byte(BFloat16 value) => (byte)(float)value;

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to its nearest representable <see cref="byte" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="byte" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="byte" />.</exception>
        public static explicit operator checked byte(BFloat16 value) => checked((byte)(float)value);

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to its nearest representable <see cref="char" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="char" /> value.</returns>
        public static explicit operator char(BFloat16 value) => (char)(float)value;

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to its nearest representable <see cref="char" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="char" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="char" />.</exception>
        public static explicit operator checked char(BFloat16 value) => checked((char)(float)value);

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to its nearest representable <see cref="decimal" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="decimal" /> value.</returns>
        public static explicit operator decimal(BFloat16 value) => (decimal)(float)value;

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to its nearest representable <see cref="short" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="short" /> value.</returns>
        public static explicit operator short(BFloat16 value) => (short)(float)value;

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to its nearest representable <see cref="short" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="short" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="short" />.</exception>
        public static explicit operator checked short(BFloat16 value) => checked((short)(float)value);

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to its nearest representable <see cref="int" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="int" /> value.</returns>
        public static explicit operator int(BFloat16 value) => (int)(float)value;

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to its nearest representable <see cref="int" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="int" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="int" />.</exception>
        public static explicit operator checked int(BFloat16 value) => checked((int)(float)value);

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to its nearest representable <see cref="long" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="long" /> value.</returns>
        public static explicit operator long(BFloat16 value) => (long)(float)value;

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to its nearest representable <see cref="long" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="long" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="long" />.</exception>
        public static explicit operator checked long(BFloat16 value) => checked((long)(float)value);

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to its nearest representable <see cref="Int128"/>.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit signed integer.</returns>
        public static explicit operator Int128(BFloat16 value) => (Int128)(double)(value);

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to its nearest representable <see cref="Int128"/>, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit signed integer.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="Int128" />.</exception>
        public static explicit operator checked Int128(BFloat16 value) => checked((Int128)(double)(value));

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to its nearest representable <see cref="IntPtr" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="IntPtr" /> value.</returns>
        public static explicit operator nint(BFloat16 value) => (nint)(float)value;

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to its nearest representable <see cref="IntPtr" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="IntPtr" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="IntPtr" />.</exception>
        public static explicit operator checked nint(BFloat16 value) => checked((nint)(float)value);

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to its nearest representable <see cref="sbyte" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="sbyte" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator sbyte(BFloat16 value) => (sbyte)(float)value;

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to its nearest representable <see cref="sbyte" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="sbyte" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="sbyte" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked sbyte(BFloat16 value) => checked((sbyte)(float)value);

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to its nearest representable <see cref="ushort" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="ushort" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator ushort(BFloat16 value) => (ushort)(float)value;

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to its nearest representable <see cref="ushort" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="ushort" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="ushort" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked ushort(BFloat16 value) => checked((ushort)(float)value);

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to its nearest representable <see cref="uint" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="uint" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator uint(BFloat16 value) => (uint)(float)value;

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to its nearest representable <see cref="uint" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="uint" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="uint" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked uint(BFloat16 value) => checked((uint)(float)value);

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to its nearest representable <see cref="ulong" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="ulong" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator ulong(BFloat16 value) => (ulong)(float)value;

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to its nearest representable <see cref="ulong" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="ulong" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="ulong" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked ulong(BFloat16 value) => checked((ulong)(float)value);

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to its nearest representable <see cref="UInt128"/>.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit unsigned integer.</returns>
        [CLSCompliant(false)]
        public static explicit operator UInt128(BFloat16 value) => (UInt128)(double)(value);

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to its nearest representable <see cref="UInt128"/>, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit unsigned integer.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="UInt128" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked UInt128(BFloat16 value) => checked((UInt128)(double)(value));

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to its nearest representable <see cref="UIntPtr" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="UIntPtr" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator nuint(BFloat16 value) => (nuint)(float)value;

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to its nearest representable <see cref="UIntPtr" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="UIntPtr" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="UIntPtr" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked nuint(BFloat16 value) => checked((nuint)(float)value);

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to its nearest representable <see cref="Half" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="Half" /> value.</returns>
        public static explicit operator Half(BFloat16 value) => (Half)(float)value;

        //
        // Implicit Convert To BFloat16
        //

        /// <summary>Implicitly converts a <see cref="byte" /> value to its nearest representable <see cref="BFloat16" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="BFloat16" /> value.</returns>
        public static implicit operator BFloat16(byte value) => (BFloat16)(float)value;

        /// <summary>Implicitly converts a <see cref="sbyte" /> value to its nearest representable <see cref="BFloat16" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="BFloat16" /> value.</returns>
        [CLSCompliant(false)]
        public static implicit operator BFloat16(sbyte value) => (BFloat16)(float)value;

        //
        // Implicit Convert From BFloat16 (actually explicit)
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
        public static BFloat16 Log2(BFloat16 value) => (BFloat16)float.Log2((float)value);

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
        public static BFloat16 Exp(BFloat16 x) => (BFloat16)float.Exp((float)x);

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
        public static BFloat16 Ceiling(BFloat16 x) => (BFloat16)float.Ceiling((float)x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Floor(TSelf)" />
        public static BFloat16 Floor(BFloat16 x) => (BFloat16)float.Floor((float)x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf)" />
        public static BFloat16 Round(BFloat16 x) => (BFloat16)float.Round((float)x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, int)" />
        public static BFloat16 Round(BFloat16 x, int digits) => (BFloat16)float.Round((float)x, digits);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, MidpointRounding)" />
        public static BFloat16 Round(BFloat16 x, MidpointRounding mode) => (BFloat16)float.Round((float)x, mode);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, int, MidpointRounding)" />
        public static BFloat16 Round(BFloat16 x, int digits, MidpointRounding mode) => (BFloat16)float.Round((float)x, digits, mode);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Truncate(TSelf)" />
        public static BFloat16 Truncate(BFloat16 x) => (BFloat16)float.Truncate((float)x);

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
                destination[0] = (byte)Exponent;
                bytesWritten = sizeof(sbyte);
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteExponentLittleEndian(Span{byte}, out int)" />
        bool IFloatingPoint<BFloat16>.TryWriteExponentLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(sbyte))
            {
                destination[0] = (byte)Exponent;
                bytesWritten = sizeof(sbyte);
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteSignificandBigEndian(Span{byte}, out int)" />
        bool IFloatingPoint<BFloat16>.TryWriteSignificandBigEndian(Span<byte> destination, out int bytesWritten)
        {
            if (BinaryPrimitives.TryWriteUInt16BigEndian(destination, Significand))
            {
                bytesWritten = sizeof(uint);
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteSignificandLittleEndian(Span{byte}, out int)" />
        bool IFloatingPoint<BFloat16>.TryWriteSignificandLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (BinaryPrimitives.TryWriteUInt16LittleEndian(destination, Significand))
            {
                bytesWritten = sizeof(uint);
                return true;
            }

            bytesWritten = 0;
            return false;
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
        public static BFloat16 Atan2(BFloat16 y, BFloat16 x) => (BFloat16)float.Atan2((float)y, (float)x);

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
        public static BFloat16 FusedMultiplyAdd(BFloat16 left, BFloat16 right, BFloat16 addend) => (BFloat16)float.FusedMultiplyAdd((float)left, (float)right, (float)addend);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Ieee754Remainder(TSelf, TSelf)" />
        public static BFloat16 Ieee754Remainder(BFloat16 left, BFloat16 right) => (BFloat16)float.Ieee754Remainder((float)left, (float)right);

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
                return MinExponent - (BitOperations.LeadingZeroCount(x.TrailingSignificand) - BiasedExponentLength);
            }

            return x.Exponent;
        }

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Lerp(TSelf, TSelf, TSelf)" />
        public static BFloat16 Lerp(BFloat16 value1, BFloat16 value2, BFloat16 amount) => (BFloat16)float.Lerp((float)value1, (float)value2, (float)amount);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ReciprocalEstimate(TSelf)" />
        public static BFloat16 ReciprocalEstimate(BFloat16 x) => (BFloat16)float.ReciprocalEstimate((float)x);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ReciprocalSqrtEstimate(TSelf)" />
        public static BFloat16 ReciprocalSqrtEstimate(BFloat16 x) => (BFloat16)float.ReciprocalSqrtEstimate((float)x);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ScaleB(TSelf, int)" />
        public static BFloat16 ScaleB(BFloat16 x, int n) => (BFloat16)float.ScaleB((float)x, n);

        // /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Compound(TSelf, TSelf)" />
        // public static BFloat16 Compound(BFloat16 x, BFloat16 n) => (BFloat16)float.Compound((float)x, (float)n);

        //
        // IHyperbolicFunctions
        //

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Acosh(TSelf)" />
        public static BFloat16 Acosh(BFloat16 x) => (BFloat16)float.Acosh((float)x);

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Asinh(TSelf)" />
        public static BFloat16 Asinh(BFloat16 x) => (BFloat16)float.Asinh((float)x);

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Atanh(TSelf)" />
        public static BFloat16 Atanh(BFloat16 x) => (BFloat16)float.Atanh((float)x);

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Cosh(TSelf)" />
        public static BFloat16 Cosh(BFloat16 x) => (BFloat16)float.Cosh((float)x);

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Sinh(TSelf)" />
        public static BFloat16 Sinh(BFloat16 x) => (BFloat16)float.Sinh((float)x);

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Tanh(TSelf)" />
        public static BFloat16 Tanh(BFloat16 x) => (BFloat16)float.Tanh((float)x);

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
        public static BFloat16 Log(BFloat16 x) => (BFloat16)float.Log((float)x);

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log(TSelf, TSelf)" />
        public static BFloat16 Log(BFloat16 x, BFloat16 newBase) => (BFloat16)float.Log((float)x, (float)newBase);

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log10(TSelf)" />
        public static BFloat16 Log10(BFloat16 x) => (BFloat16)float.Log10((float)x);

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
        public static BFloat16 Max(BFloat16 x, BFloat16 y) => (BFloat16)float.Max((float)x, (float)y);

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
        public static BFloat16 Min(BFloat16 x, BFloat16 y) => (BFloat16)float.Min((float)x, (float)y);

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
        public static BFloat16 MaxMagnitude(BFloat16 x, BFloat16 y) => (BFloat16)float.MaxMagnitude((float)x, (float)y);

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
        public static BFloat16 MinMagnitude(BFloat16 x, BFloat16 y) => (BFloat16)float.MinMagnitude((float)x, (float)y);

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

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromChecked{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<BFloat16>.TryConvertFromChecked<TOther>(TOther value, out BFloat16 result)
        {
            return TryConvertFrom(value, out result);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromSaturating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<BFloat16>.TryConvertFromSaturating<TOther>(TOther value, out BFloat16 result)
        {
            return TryConvertFrom(value, out result);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromTruncating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<BFloat16>.TryConvertFromTruncating<TOther>(TOther value, out BFloat16 result)
        {
            return TryConvertFrom(value, out result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvertFrom<TOther>(TOther value, out BFloat16 result)
            where TOther : INumberBase<TOther>
        {
            // `BFloat16` is non-first class type in System.Numerics namespace.
            // It should handle all conversions from/to types under System namespace.

            if (typeof(TOther) == typeof(double))
            {
                double actualValue = (double)(object)value;
                result = (BFloat16)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualValue = (float)(object)value;
                result = (BFloat16)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualValue = (Half)(object)value;
                result = (BFloat16)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualValue = (sbyte)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualValue = (short)(object)value;
                result = (BFloat16)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualValue = (int)(object)value;
                result = (BFloat16)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualValue = (long)(object)value;
                result = (BFloat16)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualValue = (Int128)(object)value;
                result = (BFloat16)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualValue = (nint)(object)value;
                result = (BFloat16)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(byte))
            {
                byte actualValue = (byte)(object)value;
                result = (BFloat16)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualValue = (char)(object)value;
                result = (BFloat16)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                ushort actualValue = (ushort)(object)value;
                result = (BFloat16)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualValue = (uint)(object)value;
                result = (BFloat16)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualValue = (ulong)(object)value;
                result = (BFloat16)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualValue = (UInt128)(object)value;
                result = (BFloat16)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                nuint actualValue = (nuint)(object)value;
                result = (BFloat16)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualValue = (decimal)(object)value;
                result = (BFloat16)actualValue;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToChecked{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<BFloat16>.TryConvertToChecked<TOther>(BFloat16 value, [MaybeNullWhen(false)] out TOther result)
        {
            // `BFloat16` is non-first class type in System.Numerics namespace.
            // It should handle all conversions from/to types under System namespace.

            if (typeof(TOther) == typeof(double))
            {
                double actualResult = (double)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualResult = (float)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualResult = (Half)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualResult = checked((sbyte)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualResult = checked((short)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualResult = checked((int)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualResult = checked((long)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualResult = checked((Int128)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualResult = checked((nint)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            if (typeof(TOther) == typeof(byte))
            {
                byte actualResult = checked((byte)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualResult = checked((char)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                ushort actualResult = checked((ushort)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualResult = checked((uint)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualResult = checked((ulong)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualResult = checked((UInt128)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                nuint actualResult = checked((nuint)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualResult = checked((decimal)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToSaturating{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<BFloat16>.TryConvertToSaturating<TOther>(BFloat16 value, [MaybeNullWhen(false)] out TOther result)
        {
            return TryConvertTo(value, out result);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToTruncating{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<BFloat16>.TryConvertToTruncating<TOther>(BFloat16 value, [MaybeNullWhen(false)] out TOther result)
        {
            return TryConvertTo(value, out result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvertTo<TOther>(BFloat16 value, [MaybeNullWhen(false)] out TOther result)
            where TOther : INumberBase<TOther>
        {
            // `BFloat16` is non-first class type in System.Numerics namespace.
            // It should handle all conversions from/to types under System namespace.

            if (typeof(TOther) == typeof(double))
            {
                double actualResult = (double)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualResult = (float)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualResult = (Half)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualResult = (value >= sbyte.MaxValue) ? sbyte.MaxValue :
                                     (value <= sbyte.MinValue) ? sbyte.MinValue : (sbyte)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualResult = ((float)value >= short.MaxValue) ? short.MaxValue :
                                     ((float)value <= short.MinValue) ? short.MinValue : (short)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualResult = ((float)value >= int.MaxValue) ? int.MaxValue :
                                   ((float)value <= int.MinValue) ? int.MinValue : (int)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualResult = ((float)value >= long.MaxValue) ? long.MaxValue :
                                    ((float)value <= long.MinValue) ? long.MinValue : (long)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualResult = ((float)value >= +170141183460469231731687303715884105727.0f) ? Int128.MaxValue :
                                      ((float)value <= -170141183460469231731687303715884105728.0f) ? Int128.MinValue : (Int128)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualResult = ((float)value >= nint.MaxValue) ? nint.MaxValue :
                                    ((float)value <= nint.MinValue) ? nint.MinValue : (nint)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(byte))
            {
                byte actualResult = ((float)value >= byte.MaxValue) ? byte.MaxValue :
                                   ((float)value <= byte.MinValue) ? byte.MinValue : (byte)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualResult = ((float)value >= char.MaxValue) ? char.MaxValue :
                                    (value <= Zero) ? char.MinValue : (char)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualResult = ((float)value >= +79228162514264337593543950336.0f) ? decimal.MaxValue :
                                       ((float)value <= -79228162514264337593543950336.0f) ? decimal.MinValue :
                                       IsNaN(value) ? 0.0m : (decimal)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                ushort actualResult = ((float)value >= ushort.MaxValue) ? ushort.MaxValue :
                                      (value <= Zero) ? ushort.MinValue : (ushort)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualResult = ((float)value >= uint.MaxValue) ? uint.MaxValue :
                                    (value <= Zero) ? uint.MinValue : (uint)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualResult = ((float)value >= ulong.MaxValue) ? ulong.MaxValue :
                                     (value <= Zero) ? ulong.MinValue : (ulong)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualResult = (value == PositiveInfinity) ? UInt128.MaxValue :
                                       (value <= Zero) ? UInt128.MinValue : (UInt128)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                nuint actualResult = ((float)value >= nuint.MaxValue) ? nuint.MaxValue :
                                     (value <= Zero) ? nuint.MinValue : (nuint)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        //
        // IParsable
        //

        /// <inheritdoc cref="IParsable{TSelf}.TryParse(string?, IFormatProvider?, out TSelf)" />
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out BFloat16 result) => TryParse(s, DefaultParseStyle, provider, out result);

        //
        // IPowerFunctions
        //

        /// <inheritdoc cref="IPowerFunctions{TSelf}.Pow(TSelf, TSelf)" />
        public static BFloat16 Pow(BFloat16 x, BFloat16 y) => (BFloat16)float.Pow((float)x, (float)y);

        //
        // IRootFunctions
        //

        /// <inheritdoc cref="IRootFunctions{TSelf}.Cbrt(TSelf)" />
        public static BFloat16 Cbrt(BFloat16 x) => (BFloat16)float.Cbrt((float)x);

        /// <inheritdoc cref="IRootFunctions{TSelf}.Hypot(TSelf, TSelf)" />
        public static BFloat16 Hypot(BFloat16 x, BFloat16 y) => (BFloat16)float.Hypot((float)x, (float)y);

        /// <inheritdoc cref="IRootFunctions{TSelf}.RootN(TSelf, int)" />
        public static BFloat16 RootN(BFloat16 x, int n) => (BFloat16)float.RootN((float)x, n);

        /// <inheritdoc cref="IRootFunctions{TSelf}.Sqrt(TSelf)" />
        public static BFloat16 Sqrt(BFloat16 x) => (BFloat16)float.Sqrt((float)x);

        //
        // ISignedNumber
        //

        /// <inheritdoc cref="ISignedNumber{TSelf}.NegativeOne" />
        public static BFloat16 NegativeOne => new BFloat16(NegativeOneBits);

        //
        // ISpanParsable
        //

        /// <inheritdoc cref="ISpanParsable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static BFloat16 Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, DefaultParseStyle, provider);

        /// <inheritdoc cref="ISpanParsable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out BFloat16 result) => TryParse(s, DefaultParseStyle, provider, out result);

        //
        // ISubtractionOperators
        //

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_Subtraction(TSelf, TOther)" />
        public static BFloat16 operator -(BFloat16 left, BFloat16 right) => (BFloat16)((float)left - (float)right);

        //
        // ITrigonometricFunctions
        //

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Acos(TSelf)" />
        public static BFloat16 Acos(BFloat16 x) => (BFloat16)float.Acos((float)x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AcosPi(TSelf)" />
        public static BFloat16 AcosPi(BFloat16 x) => (BFloat16)float.AcosPi((float)x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Asin(TSelf)" />
        public static BFloat16 Asin(BFloat16 x) => (BFloat16)float.Asin((float)x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AsinPi(TSelf)" />
        public static BFloat16 AsinPi(BFloat16 x) => (BFloat16)float.AsinPi((float)x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Atan(TSelf)" />
        public static BFloat16 Atan(BFloat16 x) => (BFloat16)float.Atan((float)x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AtanPi(TSelf)" />
        public static BFloat16 AtanPi(BFloat16 x) => (BFloat16)float.AtanPi((float)x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Cos(TSelf)" />
        public static BFloat16 Cos(BFloat16 x) => (BFloat16)float.Cos((float)x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.CosPi(TSelf)" />
        public static BFloat16 CosPi(BFloat16 x) => (BFloat16)float.CosPi((float)x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.DegreesToRadians(TSelf)" />
        public static BFloat16 DegreesToRadians(BFloat16 degrees)
        {
            // NOTE: Don't change the algorithm without consulting the DIM
            // which elaborates on why this implementation was chosen

            return (BFloat16)float.DegreesToRadians((float)degrees);
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.RadiansToDegrees(TSelf)" />
        public static BFloat16 RadiansToDegrees(BFloat16 radians)
        {
            // NOTE: Don't change the algorithm without consulting the DIM
            // which elaborates on why this implementation was chosen

            return (BFloat16)float.RadiansToDegrees((float)radians);
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Sin(TSelf)" />
        public static BFloat16 Sin(BFloat16 x) => (BFloat16)float.Sin((float)x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinCos(TSelf)" />
        public static (BFloat16 Sin, BFloat16 Cos) SinCos(BFloat16 x)
        {
            var (sin, cos) = float.SinCos((float)x);
            return ((BFloat16)sin, (BFloat16)cos);
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinCosPi(TSelf)" />
        public static (BFloat16 SinPi, BFloat16 CosPi) SinCosPi(BFloat16 x)
        {
            var (sinPi, cosPi) = float.SinCosPi((float)x);
            return ((BFloat16)sinPi, (BFloat16)cosPi);
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinPi(TSelf)" />
        public static BFloat16 SinPi(BFloat16 x) => (BFloat16)float.SinPi((float)x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Tan(TSelf)" />
        public static BFloat16 Tan(BFloat16 x) => (BFloat16)float.Tan((float)x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.TanPi(TSelf)" />
        public static BFloat16 TanPi(BFloat16 x) => (BFloat16)float.TanPi((float)x);

        //
        // IUnaryNegationOperators
        //

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_UnaryNegation(TSelf)" />
        public static BFloat16 operator -(BFloat16 value) => (BFloat16)(-(float)value);

        //
        // IUnaryPlusOperators
        //

        /// <inheritdoc cref="IUnaryPlusOperators{TSelf, TResult}.op_UnaryPlus(TSelf)" />
        public static BFloat16 operator +(BFloat16 value) => value;

        //
        // IUtf8SpanParsable
        //

        /// <inheritdoc cref="INumberBase{TSelf}.Parse(ReadOnlySpan{byte}, NumberStyles, IFormatProvider?)" />
        public static BFloat16 Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style = DefaultParseStyle, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.ParseFloat<byte, BFloat16>(utf8Text, style, NumberFormatInfo.GetInstance(provider));
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryParse(ReadOnlySpan{byte}, NumberStyles, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out BFloat16 result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.TryParseFloat(utf8Text, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        /// <inheritdoc cref="IUtf8SpanParsable{TSelf}.Parse(ReadOnlySpan{byte}, IFormatProvider?)" />
        public static BFloat16 Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) => Parse(utf8Text, DefaultParseStyle, provider);

        /// <inheritdoc cref="IUtf8SpanParsable{TSelf}.TryParse(ReadOnlySpan{byte}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out BFloat16 result) => TryParse(utf8Text, DefaultParseStyle, provider, out result);

        //
        // IBinaryFloatParseAndFormatInfo
        //

        static int IBinaryFloatParseAndFormatInfo<BFloat16>.NumberBufferLength => 96 + 1 + 1; // 96 for the longest input + 1 for rounding (+1 for the null terminator)

        static ulong IBinaryFloatParseAndFormatInfo<BFloat16>.ZeroBits => 0;
        static ulong IBinaryFloatParseAndFormatInfo<BFloat16>.InfinityBits => PositiveInfinityBits;

        static ulong IBinaryFloatParseAndFormatInfo<BFloat16>.NormalMantissaMask => (1UL << SignificandLength) - 1;
        static ulong IBinaryFloatParseAndFormatInfo<BFloat16>.DenormalMantissaMask => TrailingSignificandMask;

        static int IBinaryFloatParseAndFormatInfo<BFloat16>.MinBinaryExponent => 1 - MaxExponent;
        static int IBinaryFloatParseAndFormatInfo<BFloat16>.MaxBinaryExponent => MaxExponent;

        static int IBinaryFloatParseAndFormatInfo<BFloat16>.MinDecimalExponent => -41;
        static int IBinaryFloatParseAndFormatInfo<BFloat16>.MaxDecimalExponent => 39;

        static int IBinaryFloatParseAndFormatInfo<BFloat16>.ExponentBias => ExponentBias;
        static ushort IBinaryFloatParseAndFormatInfo<BFloat16>.ExponentBits => BiasedExponentLength;

        static int IBinaryFloatParseAndFormatInfo<BFloat16>.OverflowDecimalExponent => (MaxExponent + (2 * SignificandLength)) / 3;
        static int IBinaryFloatParseAndFormatInfo<BFloat16>.InfinityExponent => MaxBiasedExponent;

        static ushort IBinaryFloatParseAndFormatInfo<BFloat16>.NormalMantissaBits => SignificandLength;
        static ushort IBinaryFloatParseAndFormatInfo<BFloat16>.DenormalMantissaBits => TrailingSignificandLength;

        static int IBinaryFloatParseAndFormatInfo<BFloat16>.MinFastFloatDecimalExponent => -59;
        static int IBinaryFloatParseAndFormatInfo<BFloat16>.MaxFastFloatDecimalExponent => 38;

        static int IBinaryFloatParseAndFormatInfo<BFloat16>.MinExponentRoundToEven => -24;
        static int IBinaryFloatParseAndFormatInfo<BFloat16>.MaxExponentRoundToEven => 3;

        static int IBinaryFloatParseAndFormatInfo<BFloat16>.MaxExponentFastPath => 3;
        static ulong IBinaryFloatParseAndFormatInfo<BFloat16>.MaxMantissaFastPath => 2UL << TrailingSignificandLength;

        static int IBinaryFloatParseAndFormatInfo<BFloat16>.MaxRoundTripDigits => 4;

        static int IBinaryFloatParseAndFormatInfo<BFloat16>.MaxPrecisionCustomFormat => 4;

        static BFloat16 IBinaryFloatParseAndFormatInfo<BFloat16>.BitsToFloat(ulong bits) => new BFloat16((ushort)(bits));

        static ulong IBinaryFloatParseAndFormatInfo<BFloat16>.FloatToBits(BFloat16 value) => value._value;
    }
}
