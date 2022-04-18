// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    // Portions of the code implemented below are based on the 'Berkeley SoftFloat Release 3e' algorithms.

    /// <summary>
    /// An IEEE 754 compliant float16 type.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Half
        : IComparable,
          ISpanFormattable,
          IComparable<Half>,
          IEquatable<Half>,
          IBinaryFloatingPointIeee754<Half>,
          IMinMaxValue<Half>
    {
        private const NumberStyles DefaultParseStyle = NumberStyles.Float | NumberStyles.AllowThousands;

        // Constants for manipulating the private bit-representation

        internal const ushort SignMask = 0x8000;
        internal const int SignShift = 15;
        internal const byte ShiftedSignMask = SignMask >> SignShift;

        internal const ushort BiasedExponentMask = 0x7C00;
        internal const int BiasedExponentShift = 10;
        internal const byte ShiftedBiasedExponentMask = BiasedExponentMask >> BiasedExponentShift;

        internal const ushort TrailingSignificandMask = 0x03FF;

        internal const byte MinSign = 0;
        internal const byte MaxSign = 1;

        internal const byte MinBiasedExponent = 0x00;
        internal const byte MaxBiasedExponent = 0x1F;

        internal const byte ExponentBias = 15;

        internal const sbyte MinExponent = -14;
        internal const sbyte MaxExponent = +15;

        internal const ushort MinTrailingSignificand = 0x0000;
        internal const ushort MaxTrailingSignificand = 0x03FF;

        // Constants representing the private bit-representation for various default values

        private const ushort PositiveZeroBits = 0x0000;
        private const ushort NegativeZeroBits = 0x8000;

        private const ushort EpsilonBits = 0x0001;

        private const ushort PositiveInfinityBits = 0x7C00;
        private const ushort NegativeInfinityBits = 0xFC00;

        private const ushort PositiveQNaNBits = 0x7E00;
        private const ushort NegativeQNaNBits = 0xFE00;

        private const ushort MinValueBits = 0xFBFF;
        private const ushort MaxValueBits = 0x7BFF;

        private const ushort PositiveOneBits = 0x3C00;
        private const ushort NegativeOneBits = 0xBC00;

        private const ushort EBits = 0x4170;
        private const ushort PiBits = 0x4248;
        private const ushort TauBits = 0x4648;

        // Well-defined and commonly used values

        public static Half Epsilon => new Half(EpsilonBits);                        //  5.9604645E-08

        public static Half PositiveInfinity => new Half(PositiveInfinityBits);      //  1.0 / 0.0;

        public static Half NegativeInfinity => new Half(NegativeInfinityBits);      // -1.0 / 0.0

        public static Half NaN => new Half(NegativeQNaNBits);                       //  0.0 / 0.0

        /// <inheritdoc cref="INumber{TSelf}.Min(TSelf, TSelf)" />
        public static Half MinValue => new Half(MinValueBits);                      // -65504

        /// <inheritdoc cref="INumber{TSelf}.Max(TSelf, TSelf)" />
        public static Half MaxValue => new Half(MaxValueBits);                      //  65504

        private readonly ushort _value;

        internal Half(ushort value)
        {
            _value = value;
        }

        private Half(bool sign, ushort exp, ushort sig) => _value = (ushort)(((sign ? 1 : 0) << SignShift) + (exp << BiasedExponentShift) + sig);

        private byte BiasedExponent
        {
            get
            {
                ushort bits = _value;
                return ExtractBiasedExponentFromBits(bits);
            }
        }

        private sbyte Exponent
        {
            get
            {
                return (sbyte)(BiasedExponent - ExponentBias);
            }
        }

        private ushort Significand
        {
            get
            {
                return (ushort)(TrailingSignificand | ((BiasedExponent != 0) ? (1U << BiasedExponentShift) : 0U));
            }
        }

        private ushort TrailingSignificand
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

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_LessThan(TSelf, TOther)" />
        public static bool operator <(Half left, Half right)
        {
            if (IsNaN(left) || IsNaN(right))
            {
                // IEEE defines that NaN is unordered with respect to everything, including itself.
                return false;
            }

            bool leftIsNegative = IsNegative(left);

            if (leftIsNegative != IsNegative(right))
            {
                // When the signs of left and right differ, we know that left is less than right if it is
                // the negative value. The exception to this is if both values are zero, in which case IEEE
                // says they should be equal, even if the signs differ.
                return leftIsNegative && !AreZero(left, right);
            }

            return (left._value != right._value) && ((left._value < right._value) ^ leftIsNegative);
        }

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_GreaterThan(TSelf, TOther)" />
        public static bool operator >(Half left, Half right)
        {
            return right < left;
        }

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_LessThanOrEqual(TSelf, TOther)" />
        public static bool operator <=(Half left, Half right)
        {
            if (IsNaN(left) || IsNaN(right))
            {
                // IEEE defines that NaN is unordered with respect to everything, including itself.
                return false;
            }

            bool leftIsNegative = IsNegative(left);

            if (leftIsNegative != IsNegative(right))
            {
                // When the signs of left and right differ, we know that left is less than right if it is
                // the negative value. The exception to this is if both values are zero, in which case IEEE
                // says they should be equal, even if the signs differ.
                return leftIsNegative || AreZero(left, right);
            }

            return (left._value == right._value) || ((left._value < right._value) ^ leftIsNegative);
        }

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_GreaterThanOrEqual(TSelf, TOther)" />
        public static bool operator >=(Half left, Half right)
        {
            return right <= left;
        }

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther}.op_Equality(TSelf, TOther)" />
        public static bool operator ==(Half left, Half right)
        {
            if (IsNaN(left) || IsNaN(right))
            {
                // IEEE defines that NaN is not equal to anything, including itself.
                return false;
            }

            // IEEE defines that positive and negative zero are equivalent.
            return (left._value == right._value) || AreZero(left, right);
        }

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther}.op_Inequality(TSelf, TOther)" />
        public static bool operator !=(Half left, Half right)
        {
            return !(left == right);
        }

        /// <summary>Determines whether the specified value is finite (zero, subnormal, or normal).</summary>
        public static bool IsFinite(Half value)
        {
            return StripSign(value) < PositiveInfinityBits;
        }

        /// <summary>Determines whether the specified value is infinite.</summary>
        public static bool IsInfinity(Half value)
        {
            return StripSign(value) == PositiveInfinityBits;
        }

        /// <summary>Determines whether the specified value is NaN.</summary>
        public static bool IsNaN(Half value)
        {
            return StripSign(value) > PositiveInfinityBits;
        }

        /// <summary>Determines whether the specified value is negative.</summary>
        public static bool IsNegative(Half value)
        {
            return (short)(value._value) < 0;
        }

        /// <summary>Determines whether the specified value is negative infinity.</summary>
        public static bool IsNegativeInfinity(Half value)
        {
            return value._value == NegativeInfinityBits;
        }

        /// <summary>Determines whether the specified value is normal.</summary>
        // This is probably not worth inlining, it has branches and should be rarely called
        public static bool IsNormal(Half value)
        {
            uint absValue = StripSign(value);
            return (absValue < PositiveInfinityBits)    // is finite
                && (absValue != 0)                      // is not zero
                && ((absValue & BiasedExponentMask) != 0);    // is not subnormal (has a non-zero exponent)
        }

        /// <summary>Determines whether the specified value is positive infinity.</summary>
        public static bool IsPositiveInfinity(Half value)
        {
            return value._value == PositiveInfinityBits;
        }

        /// <summary>Determines whether the specified value is subnormal.</summary>
        // This is probably not worth inlining, it has branches and should be rarely called
        public static bool IsSubnormal(Half value)
        {
            uint absValue = StripSign(value);
            return (absValue < PositiveInfinityBits)    // is finite
                && (absValue != 0)                      // is not zero
                && ((absValue & BiasedExponentMask) == 0);    // is subnormal (has a zero exponent)
        }

        /// <summary>
        /// Parses a <see cref="Half"/> from a <see cref="string"/> in the default parse style.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <returns>The equivalent <see cref="Half"/> value representing the input string. If the input exceeds Half's range, a <see cref="Half.PositiveInfinity"/> or <see cref="Half.NegativeInfinity"/> is returned. </returns>
        public static Half Parse(string s)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseHalf(s, DefaultParseStyle, NumberFormatInfo.CurrentInfo);
        }

        /// <summary>
        /// Parses a <see cref="Half"/> from a <see cref="string"/> in the given <see cref="NumberStyles"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <returns>The equivalent <see cref="Half"/> value representing the input string. If the input exceeds Half's range, a <see cref="Half.PositiveInfinity"/> or <see cref="Half.NegativeInfinity"/> is returned. </returns>
        public static Half Parse(string s, NumberStyles style)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseHalf(s, style, NumberFormatInfo.CurrentInfo);
        }

        /// <summary>
        /// Parses a <see cref="Half"/> from a <see cref="string"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="provider">A format provider.</param>
        /// <returns>The equivalent <see cref="Half"/> value representing the input string. If the input exceeds Half's range, a <see cref="Half.PositiveInfinity"/> or <see cref="Half.NegativeInfinity"/> is returned. </returns>
        public static Half Parse(string s, IFormatProvider? provider)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseHalf(s, DefaultParseStyle, NumberFormatInfo.GetInstance(provider));
        }

        /// <summary>
        /// Parses a <see cref="Half"/> from a <see cref="string"/> with the given <see cref="NumberStyles"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider.</param>
        /// <returns>The equivalent <see cref="Half"/> value representing the input string. If the input exceeds Half's range, a <see cref="Half.PositiveInfinity"/> or <see cref="Half.NegativeInfinity"/> is returned. </returns>
        public static Half Parse(string s, NumberStyles style = DefaultParseStyle, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseHalf(s, style, NumberFormatInfo.GetInstance(provider));
        }

        /// <summary>
        /// Parses a <see cref="Half"/> from a <see cref="ReadOnlySpan{Char}"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider. </param>
        /// <returns>The equivalent <see cref="Half"/> value representing the input string. If the input exceeds Half's range, a <see cref="Half.PositiveInfinity"/> or <see cref="Half.NegativeInfinity"/> is returned. </returns>
        public static Half Parse(ReadOnlySpan<char> s, NumberStyles style = DefaultParseStyle, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Number.ParseHalf(s, style, NumberFormatInfo.GetInstance(provider));
        }

        /// <summary>
        /// Tries to parses a <see cref="Half"/> from a <see cref="string"/> in the default parse style.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="result">The equivalent <see cref="Half"/> value representing the input string if the parse was successful. If the input exceeds Half's range, a <see cref="Half.PositiveInfinity"/> or <see cref="Half.NegativeInfinity"/> is returned. If the parse was unsuccessful, a default <see cref="Half"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, out Half result)
        {
            if (s == null)
            {
                result = default;
                return false;
            }
            return TryParse(s, DefaultParseStyle, provider: null, out result);
        }

        /// <summary>
        /// Tries to parses a <see cref="Half"/> from a <see cref="ReadOnlySpan{Char}"/> in the default parse style.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="result">The equivalent <see cref="Half"/> value representing the input string if the parse was successful. If the input exceeds Half's range, a <see cref="Half.PositiveInfinity"/> or <see cref="Half.NegativeInfinity"/> is returned. If the parse was unsuccessful, a default <see cref="Half"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, out Half result)
        {
            return TryParse(s, DefaultParseStyle, provider: null, out result);
        }

        /// <summary>
        /// Tries to parse a <see cref="Half"/> from a <see cref="string"/> with the given <see cref="NumberStyles"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider. </param>
        /// <param name="result">The equivalent <see cref="Half"/> value representing the input string if the parse was successful. If the input exceeds Half's range, a <see cref="Half.PositiveInfinity"/> or <see cref="Half.NegativeInfinity"/> is returned. If the parse was unsuccessful, a default <see cref="Half"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out Half result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);

            if (s == null)
            {
                result = default;
                return false;
            }

            return TryParse(s.AsSpan(), style, provider, out result);
        }

        /// <summary>
        /// Tries to parse a <see cref="Half"/> from a <see cref="ReadOnlySpan{Char}"/> with the given <see cref="NumberStyles"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider. </param>
        /// <param name="result">The equivalent <see cref="Half"/> value representing the input string if the parse was successful. If the input exceeds Half's range, a <see cref="Half.PositiveInfinity"/> or <see cref="Half.NegativeInfinity"/> is returned. If the parse was unsuccessful, a default <see cref="Half"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out Half result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Number.TryParseHalf(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        private static bool AreZero(Half left, Half right)
        {
            // IEEE defines that positive and negative zero are equal, this gives us a quick equality check
            // for two values by or'ing the private bits together and stripping the sign. They are both zero,
            // and therefore equivalent, if the resulting value is still zero.
            return (ushort)((left._value | right._value) & ~SignMask) == 0;
        }

        private static bool IsNaNOrZero(Half value)
        {
            return ((value._value - 1) & ~SignMask) >= PositiveInfinityBits;
        }

        private static uint StripSign(Half value)
        {
            return (ushort)(value._value & ~SignMask);
        }

        /// <summary>
        /// Compares this object to another object, returning an integer that indicates the relationship.
        /// </summary>
        /// <returns>A value less than zero if this is less than <paramref name="obj"/>, zero if this is equal to <paramref name="obj"/>, or a value greater than zero if this is greater than <paramref name="obj"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="obj"/> is not of type <see cref="Half"/>.</exception>
        public int CompareTo(object? obj)
        {
            if (!(obj is Half))
            {
                return (obj is null) ? 1 : throw new ArgumentException(SR.Arg_MustBeHalf);
            }
            return CompareTo((Half)(obj));
        }

        /// <summary>
        /// Compares this object to another object, returning an integer that indicates the relationship.
        /// </summary>
        /// <returns>A value less than zero if this is less than <paramref name="other"/>, zero if this is equal to <paramref name="other"/>, or a value greater than zero if this is greater than <paramref name="other"/>.</returns>
        public int CompareTo(Half other)
        {
            if (this < other)
            {
                return -1;
            }

            if (this > other)
            {
                return 1;
            }

            if (this == other)
            {
                return 0;
            }

            if (IsNaN(this))
            {
                return IsNaN(other) ? 0 : -1;
            }

            Debug.Assert(IsNaN(other));
            return 1;
        }

        /// <summary>
        /// Returns a value that indicates whether this instance is equal to a specified <paramref name="obj"/>.
        /// </summary>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return (obj is Half other) && Equals(other);
        }

        /// <summary>
        /// Returns a value that indicates whether this instance is equal to a specified <paramref name="other"/> value.
        /// </summary>
        public bool Equals(Half other)
        {
            return _value == other._value
                || AreZero(this, other)
                || (IsNaN(this) && IsNaN(other));
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        public override int GetHashCode()
        {
            if (IsNaNOrZero(this))
            {
                // All NaNs should have the same hash code, as should both Zeros.
                return _value & PositiveInfinityBits;
            }
            return _value;
        }

        /// <summary>
        /// Returns a string representation of the current value.
        /// </summary>
        public override string ToString()
        {
            return Number.FormatHalf(this, null, NumberFormatInfo.CurrentInfo);
        }

        /// <summary>
        /// Returns a string representation of the current value using the specified <paramref name="format"/>.
        /// </summary>
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
        {
            return Number.FormatHalf(this, format, NumberFormatInfo.CurrentInfo);
        }

        /// <summary>
        /// Returns a string representation of the current value with the specified <paramref name="provider"/>.
        /// </summary>
        public string ToString(IFormatProvider? provider)
        {
            return Number.FormatHalf(this, null, NumberFormatInfo.GetInstance(provider));
        }

        /// <summary>
        /// Returns a string representation of the current value using the specified <paramref name="format"/> and <paramref name="provider"/>.
        /// </summary>
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider)
        {
            return Number.FormatHalf(this, format, NumberFormatInfo.GetInstance(provider));
        }

        /// <summary>
        /// Tries to format the value of the current Half instance into the provided span of characters.
        /// </summary>
        /// <param name="destination">When this method returns, this instance's value formatted as a span of characters.</param>
        /// <param name="charsWritten">When this method returns, the number of characters that were written in <paramref name="destination"/>.</param>
        /// <param name="format">A span containing the characters that represent a standard or custom format string that defines the acceptable format for <paramref name="destination"/>.</param>
        /// <param name="provider">An optional object that supplies culture-specific formatting information for <paramref name="destination"/>.</param>
        /// <returns></returns>
        public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatHalf(this, format, NumberFormatInfo.GetInstance(provider), destination, out charsWritten);
        }

        // -----------------------Start of to-half conversions-------------------------

        public static explicit operator Half(float value)
        {
            const int SingleMaxExponent = 0xFF;

            uint floatInt = BitConverter.SingleToUInt32Bits(value);
            bool sign = (floatInt & float.SignMask) >> float.SignShift != 0;
            int exp = (int)(floatInt & float.BiasedExponentMask) >> float.BiasedExponentShift;
            uint sig = floatInt & float.TrailingSignificandMask;

            if (exp == SingleMaxExponent)
            {
                if (sig != 0) // NaN
                {
                    return CreateHalfNaN(sign, (ulong)sig << 41); // Shift the significand bits to the left end
                }
                return sign ? NegativeInfinity : PositiveInfinity;
            }

            uint sigHalf = sig >> 9 | ((sig & 0x1FFU) != 0 ? 1U : 0U); // RightShiftJam

            if ((exp | (int)sigHalf) == 0)
            {
                return new Half(sign, 0, 0);
            }

            return new Half(RoundPackToHalf(sign, (short)(exp - 0x71), (ushort)(sigHalf | 0x4000)));
        }

        public static explicit operator Half(double value)
        {
            const int DoubleMaxExponent = 0x7FF;

            ulong doubleInt = BitConverter.DoubleToUInt64Bits(value);
            bool sign = (doubleInt & double.SignMask) >> double.SignShift != 0;
            int exp = (int)((doubleInt & double.BiasedExponentMask) >> double.BiasedExponentShift);
            ulong sig = doubleInt & double.TrailingSignificandMask;

            if (exp == DoubleMaxExponent)
            {
                if (sig != 0) // NaN
                {
                    return CreateHalfNaN(sign, sig << 12); // Shift the significand bits to the left end
                }
                return sign ? NegativeInfinity : PositiveInfinity;
            }

            uint sigHalf = (uint)ShiftRightJam(sig, 38);
            if ((exp | (int)sigHalf) == 0)
            {
                return new Half(sign, 0, 0);
            }
            return new Half(RoundPackToHalf(sign, (short)(exp - 0x3F1), (ushort)(sigHalf | 0x4000)));
        }

        // -----------------------Start of from-half conversions-------------------------
        public static explicit operator float(Half value)
        {
            bool sign = IsNegative(value);
            int exp = value.BiasedExponent;
            uint sig = value.TrailingSignificand;

            if (exp == MaxBiasedExponent)
            {
                if (sig != 0)
                {
                    return CreateSingleNaN(sign, (ulong)sig << 54);
                }
                return sign ? float.NegativeInfinity : float.PositiveInfinity;
            }

            if (exp == 0)
            {
                if (sig == 0)
                {
                    return BitConverter.UInt32BitsToSingle(sign ? float.SignMask : 0); // Positive / Negative zero
                }
                (exp, sig) = NormSubnormalF16Sig(sig);
                exp -= 1;
            }

            return CreateSingle(sign, (byte)(exp + 0x70), sig << 13);
        }

        public static explicit operator double(Half value)
        {
            bool sign = IsNegative(value);
            int exp = value.BiasedExponent;
            uint sig = value.TrailingSignificand;

            if (exp == MaxBiasedExponent)
            {
                if (sig != 0)
                {
                    return CreateDoubleNaN(sign, (ulong)sig << 54);
                }
                return sign ? double.NegativeInfinity : double.PositiveInfinity;
            }

            if (exp == 0)
            {
                if (sig == 0)
                {
                    return BitConverter.UInt64BitsToDouble(sign ? double.SignMask : 0); // Positive / Negative zero
                }
                (exp, sig) = NormSubnormalF16Sig(sig);
                exp -= 1;
            }

            return CreateDouble(sign, (ushort)(exp + 0x3F0), (ulong)sig << 42);
        }

        // IEEE 754 specifies NaNs to be propagated
        internal static Half Negate(Half value)
        {
            return IsNaN(value) ? value : new Half((ushort)(value._value ^ SignMask));
        }

        private static (int Exp, uint Sig) NormSubnormalF16Sig(uint sig)
        {
            int shiftDist = BitOperations.LeadingZeroCount(sig) - 16 - 5;
            return (1 - shiftDist, sig << shiftDist);
        }

        #region Utilities

        // Significand bits should be shifted towards to the left end before calling these methods
        // Creates Quiet NaN if significand == 0
        private static Half CreateHalfNaN(bool sign, ulong significand)
        {
            const uint NaNBits = BiasedExponentMask | 0x200; // Most significant significand bit

            uint signInt = (sign ? 1U : 0U) << SignShift;
            uint sigInt = (uint)(significand >> 54);

            return BitConverter.UInt16BitsToHalf((ushort)(signInt | NaNBits | sigInt));
        }

        private static ushort RoundPackToHalf(bool sign, short exp, ushort sig)
        {
            const int RoundIncrement = 0x8; // Depends on rounding mode but it's always towards closest / ties to even
            int roundBits = sig & 0xF;

            if ((uint)exp >= 0x1D)
            {
                if (exp < 0)
                {
                    sig = (ushort)ShiftRightJam(sig, -exp);
                    exp = 0;
                    roundBits = sig & 0xF;
                }
                else if (exp > 0x1D || sig + RoundIncrement >= 0x8000) // Overflow
                {
                    return sign ? NegativeInfinityBits : PositiveInfinityBits;
                }
            }

            sig = (ushort)((sig + RoundIncrement) >> 4);
            sig &= (ushort)~(((roundBits ^ 8) != 0 ? 0 : 1) & 1);

            if (sig == 0)
            {
                exp = 0;
            }

            return new Half(sign, (ushort)exp, sig)._value;
        }

        // If any bits are lost by shifting, "jam" them into the LSB.
        // if dist > bit count, Will be 1 or 0 depending on i
        // (unlike bitwise operators that masks the lower 5 bits)
        private static uint ShiftRightJam(uint i, int dist) => dist < 31 ? (i >> dist) | (i << (-dist & 31) != 0 ? 1U : 0U) : (i != 0 ? 1U : 0U);

        private static ulong ShiftRightJam(ulong l, int dist) => dist < 63 ? (l >> dist) | (l << (-dist & 63) != 0 ? 1UL : 0UL) : (l != 0 ? 1UL : 0UL);

        private static float CreateSingleNaN(bool sign, ulong significand)
        {
            const uint NaNBits = float.BiasedExponentMask | 0x400000; // Most significant significand bit

            uint signInt = (sign ? 1U : 0U) << float.SignShift;
            uint sigInt = (uint)(significand >> 41);

            return BitConverter.UInt32BitsToSingle(signInt | NaNBits | sigInt);
        }

        private static double CreateDoubleNaN(bool sign, ulong significand)
        {
            const ulong NaNBits = double.BiasedExponentMask | 0x80000_00000000; // Most significant significand bit

            ulong signInt = (sign ? 1UL : 0UL) << double.SignShift;
            ulong sigInt = significand >> 12;

            return BitConverter.UInt64BitsToDouble(signInt | NaNBits | sigInt);
        }

        private static float CreateSingle(bool sign, byte exp, uint sig) => BitConverter.UInt32BitsToSingle(((sign ? 1U : 0U) << float.SignShift) + ((uint)exp << float.BiasedExponentShift) + sig);

        private static double CreateDouble(bool sign, ushort exp, ulong sig) => BitConverter.UInt64BitsToDouble(((sign ? 1UL : 0UL) << double.SignShift) + ((ulong)exp << double.BiasedExponentShift) + sig);

        #endregion

        //
        // IAdditionOperators
        //

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        public static Half operator +(Half left, Half right) => (Half)((float)left + (float)right);

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        static Half IAdditionOperators<Half, Half, Half>.operator checked +(Half left, Half right) => left + right;

        //
        // IAdditiveIdentity
        //

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static Half IAdditiveIdentity<Half, Half>.AdditiveIdentity => new Half(PositiveZeroBits);

        //
        // IBinaryNumber
        //

        /// <inheritdoc cref="IBinaryNumber{TSelf}.IsPow2(TSelf)" />
        public static bool IsPow2(Half value)
        {
            ushort bits = BitConverter.HalfToUInt16Bits(value);

            byte biasedExponent = ExtractBiasedExponentFromBits(bits);
            ushort trailingSignificand = ExtractTrailingSignificandFromBits(bits);

            return (value > default(Half))
                && (biasedExponent != MinBiasedExponent) && (biasedExponent != MaxBiasedExponent)
                && (trailingSignificand == MinTrailingSignificand);
        }

        /// <inheritdoc cref="IBinaryNumber{TSelf}.Log2(TSelf)" />
        public static Half Log2(Half value) => (Half)MathF.Log2((float)value);

        //
        // IBitwiseOperators
        //

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseAnd(TSelf, TOther)" />
        static Half IBitwiseOperators<Half, Half, Half>.operator &(Half left, Half right)
        {
            ushort bits = (ushort)(BitConverter.HalfToUInt16Bits(left) & BitConverter.HalfToUInt16Bits(right));
            return BitConverter.UInt16BitsToHalf(bits);
        }

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseOr(TSelf, TOther)" />
        static Half IBitwiseOperators<Half, Half, Half>.operator |(Half left, Half right)
        {
            ushort bits = (ushort)(BitConverter.HalfToUInt16Bits(left) | BitConverter.HalfToUInt16Bits(right));
            return BitConverter.UInt16BitsToHalf(bits);
        }

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_ExclusiveOr(TSelf, TOther)" />
        static Half IBitwiseOperators<Half, Half, Half>.operator ^(Half left, Half right)
        {
            ushort bits = (ushort)(BitConverter.HalfToUInt16Bits(left) ^ BitConverter.HalfToUInt16Bits(right));
            return BitConverter.UInt16BitsToHalf(bits);
        }

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_OnesComplement(TSelf)" />
        static Half IBitwiseOperators<Half, Half, Half>.operator ~(Half value)
        {
            ushort bits = (ushort)(~BitConverter.HalfToUInt16Bits(value));
            return BitConverter.UInt16BitsToHalf(bits);
        }

        //
        // IDecrementOperators
        //

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        public static Half operator --(Half value)
        {
            var tmp = (float)value;
            --tmp;
            return (Half)tmp;
        }

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_CheckedDecrement(TSelf)" />
        static Half IDecrementOperators<Half>.operator checked --(Half value) => --value;

        //
        // IDivisionOperators
        //

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_Division(TSelf, TOther)" />
        public static Half operator /(Half left, Half right) => (Half)((float)left / (float)right);

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_CheckedDivision(TSelf, TOther)" />
        static Half IDivisionOperators<Half, Half, Half>.operator checked /(Half left, Half right) => left / right;

        //
        // IExponentialFunctions
        //

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp" />
        public static Half Exp(Half x) => (Half)MathF.Exp((float)x);

        // /// <inheritdoc cref="IExponentialFunctions{TSelf}.ExpM1(TSelf)" />
        // public static Half ExpM1(Half x) => (Half)MathF.ExpM1((float)x);

        // /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp2(TSelf)" />
        // public static Half Exp2(Half x) => (Half)MathF.Exp2((float)x);

        // /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp2M1(TSelf)" />
        // public static Half Exp2M1(Half x) => (Half)MathF.Exp2M1((float)x);

        // /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp10(TSelf)" />
        // public static Half Exp10(Half x) => (Half)MathF.Exp10((float)x);

        // /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp10M1(TSelf)" />
        // public static Half Exp10M1(Half x) => (Half)MathF.Exp10M1((float)x);

        //
        // IFloatingPoint
        //

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Ceiling(TSelf)" />
        public static Half Ceiling(Half x) => (Half)MathF.Ceiling((float)x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Floor(TSelf)" />
        public static Half Floor(Half x) => (Half)MathF.Floor((float)x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf)" />
        public static Half Round(Half x) => (Half)MathF.Round((float)x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, int)" />
        public static Half Round(Half x, int digits) => (Half)MathF.Round((float)x, digits);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, MidpointRounding)" />
        public static Half Round(Half x, MidpointRounding mode) => (Half)MathF.Round((float)x, mode);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, int, MidpointRounding)" />
        public static Half Round(Half x, int digits, MidpointRounding mode) => (Half)MathF.Round((float)x, digits, mode);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Truncate(TSelf)" />
        public static Half Truncate(Half x) => (Half)MathF.Truncate((float)x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetExponentShortestBitLength()" />
        long IFloatingPoint<Half>.GetExponentShortestBitLength()
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

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetExponentByteCount()" />
        int IFloatingPoint<Half>.GetExponentByteCount() => sizeof(sbyte);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteExponentLittleEndian(Span{byte}, out int)" />
        bool IFloatingPoint<Half>.TryWriteExponentLittleEndian(Span<byte> destination, out int bytesWritten)
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

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetSignificandBitLength()" />
        long IFloatingPoint<Half>.GetSignificandBitLength() => 11;

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetSignificandByteCount()" />
        int IFloatingPoint<Half>.GetSignificandByteCount() => sizeof(ushort);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteSignificandLittleEndian(Span{byte}, out int)" />
        bool IFloatingPoint<Half>.TryWriteSignificandLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(ushort))
            {
                ushort significand = Significand;
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
        // IFloatingPointIeee754
        //

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.E" />
        public static Half E => new Half(EBits);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.NegativeZero" />
        public static Half NegativeZero => new Half(NegativeZeroBits);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Pi" />
        public static Half Pi => new Half(PiBits);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Tau" />
        public static Half Tau => new Half(TauBits);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.BitDecrement(TSelf)" />
        public static Half BitDecrement(Half x) => (Half)MathF.BitDecrement((float)x);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.BitIncrement(TSelf)" />
        public static Half BitIncrement(Half x) => (Half)MathF.BitIncrement((float)x);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.FusedMultiplyAdd(TSelf, TSelf, TSelf)" />
        public static Half FusedMultiplyAdd(Half left, Half right, Half addend) => (Half)MathF.FusedMultiplyAdd((float)left, (float)right, (float)addend);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Ieee754Remainder(TSelf, TSelf)" />
        public static Half Ieee754Remainder(Half left, Half right) => (Half)MathF.IEEERemainder((float)left, (float)right);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ILogB(TSelf)" />
        public static int ILogB(Half x) => MathF.ILogB((float)x);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ReciprocalEstimate(TSelf)" />
        public static Half ReciprocalEstimate(Half x) => (Half)MathF.ReciprocalEstimate((float)x);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ReciprocalSqrtEstimate(TSelf)" />
        public static Half ReciprocalSqrtEstimate(Half x) => (Half)MathF.ReciprocalSqrtEstimate((float)x);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ScaleB(TSelf, int)" />
        public static Half ScaleB(Half x, int n) => (Half)MathF.ScaleB((float)x, n);

        // /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Compound(TSelf, TSelf)" />
        // public static Half Compound(Half x, Half n) => (Half)MathF.Compound((float)x, (float)n);

        // /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.MaxMagnitudeNumber(TSelf, TSelf)" />
        // public static Half MaxMagnitudeNumber(Half x, Half y) => (Half)MathF.MaxMagnitudeNumber((float)x, (float)y);

        // /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.MaxNumber(TSelf, TSelf)" />
        // public static Half MaxNumber(Half x, Half y) => (Half)MathF.MaxNumber((float)x, (float)y);

        // /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.MinMagnitudeNumber(TSelf, TSelf)" />
        // public static Half MinMagnitudeNumber(Half x, Half y) => (Half)MathF.MinMagnitudeNumber((float)x, (float)y);

        // /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.MinNumber(TSelf, TSelf)" />
        // public static Half MinNumber(Half x, Half y) => (Half)MathF.MinNumber((float)x, (float)y);

        //
        // IHyperbolicFunctions
        //

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Acosh(TSelf)" />
        public static Half Acosh(Half x) => (Half)MathF.Acosh((float)x);

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Asinh(TSelf)" />
        public static Half Asinh(Half x) => (Half)MathF.Asinh((float)x);

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Atanh(TSelf)" />
        public static Half Atanh(Half x) => (Half)MathF.Atanh((float)x);

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Cosh(TSelf)" />
        public static Half Cosh(Half x) => (Half)MathF.Cosh((float)x);

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Sinh(TSelf)" />
        public static Half Sinh(Half x) => (Half)MathF.Sinh((float)x);

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Tanh(TSelf)" />
        public static Half Tanh(Half x) => (Half)MathF.Tanh((float)x);

        //
        // IIncrementOperators
        //

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_Increment(TSelf)" />
        public static Half operator ++(Half value)
        {
            var tmp = (float)value;
            ++tmp;
            return (Half)tmp;
        }

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_CheckedIncrement(TSelf)" />
        static Half IIncrementOperators<Half>.operator checked ++(Half value) => ++value;

        //
        // ILogarithmicFunctions
        //

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log(TSelf)" />
        public static Half Log(Half x) => (Half)MathF.Log((float)x);

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log(TSelf, TSelf)" />
        public static Half Log(Half x, Half newBase) => (Half)MathF.Log((float)x, (float)newBase);

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log10(TSelf)" />
        public static Half Log10(Half x) => (Half)MathF.Log10((float)x);

        // /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.LogP1(TSelf)" />
        // public static Half LogP1(Half x) => (Half)MathF.LogP1((float)x);

        // /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log2P1(TSelf)" />
        // public static Half Log2P1(Half x) => (Half)MathF.Log2P1((float)x);

        // /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log10P1(TSelf)" />
        // public static Half Log10P1(Half x) => (Half)MathF.Log10P1((float)x);

        //
        // IModulusOperators
        //

        /// <inheritdoc cref="IModulusOperators{TSelf, TOther, TResult}.op_Modulus(TSelf, TOther)" />
        public static Half operator %(Half left, Half right) => (Half)((float)left % (float)right);

        //
        // IMultiplicativeIdentity
        //

        /// <inheritdoc cref="IMultiplicativeIdentity{TSelf, TResult}.MultiplicativeIdentity" />
        public static Half MultiplicativeIdentity => new Half(PositiveOneBits);

        //
        // IMultiplyOperators
        //

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_Multiply(TSelf, TOther)" />
        public static Half operator *(Half left, Half right) => (Half)((float)left * (float)right);

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_CheckedMultiply(TSelf, TOther)" />
        static Half IMultiplyOperators<Half, Half, Half>.operator checked *(Half left, Half right) => left * right;

        //
        // INumber
        //

        /// <inheritdoc cref="INumber{TSelf}.Abs(TSelf)" />
        public static Half Abs(Half value) => (Half)MathF.Abs((float)value);

        /// <inheritdoc cref="INumber{TSelf}.Clamp(TSelf, TSelf, TSelf)" />
        public static Half Clamp(Half value, Half min, Half max) => (Half)Math.Clamp((float)value, (float)min, (float)max);

        /// <inheritdoc cref="INumber{TSelf}.CopySign(TSelf, TSelf)" />
        public static Half CopySign(Half x, Half y) => (Half)MathF.CopySign((float)x, (float)y);

        /// <inheritdoc cref="INumber{TSelf}.CreateChecked{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Half CreateChecked<TOther>(TOther value)
            where TOther : INumber<TOther>
        {
            if (typeof(TOther) == typeof(byte))
            {
                return (Half)(byte)(object)value;
            }
            else if (typeof(TOther) == typeof(char))
            {
                return (Half)(char)(object)value;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                return (Half)(float)(decimal)(object)value;
            }
            else if (typeof(TOther) == typeof(double))
            {
                return (Half)(double)(object)value;
            }
            else if (typeof(TOther) == typeof(short))
            {
                return (Half)(short)(object)value;
            }
            else if (typeof(TOther) == typeof(int))
            {
                return (Half)(int)(object)value;
            }
            else if (typeof(TOther) == typeof(long))
            {
                return (Half)(long)(object)value;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                return (Half)(long)(nint)(object)value;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                return (Half)(sbyte)(object)value;
            }
            else if (typeof(TOther) == typeof(float))
            {
                return (Half)(float)(object)value;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                return (Half)(ushort)(object)value;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                return (Half)(uint)(object)value;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                return (Half)(ulong)(object)value;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                return (Half)(ulong)(nuint)(object)value;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        /// <inheritdoc cref="INumber{TSelf}.CreateSaturating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Half CreateSaturating<TOther>(TOther value)
            where TOther : INumber<TOther>
        {
            if (typeof(TOther) == typeof(byte))
            {
                return (Half)(byte)(object)value;
            }
            else if (typeof(TOther) == typeof(char))
            {
                return (Half)(char)(object)value;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                return (Half)(float)(decimal)(object)value;
            }
            else if (typeof(TOther) == typeof(double))
            {
                return (Half)(double)(object)value;
            }
            else if (typeof(TOther) == typeof(short))
            {
                return (Half)(short)(object)value;
            }
            else if (typeof(TOther) == typeof(int))
            {
                return (Half)(int)(object)value;
            }
            else if (typeof(TOther) == typeof(long))
            {
                return (Half)(long)(object)value;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                return (Half)(long)(nint)(object)value;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                return (Half)(sbyte)(object)value;
            }
            else if (typeof(TOther) == typeof(float))
            {
                return (Half)(float)(object)value;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                return (Half)(ushort)(object)value;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                return (Half)(uint)(object)value;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                return (Half)(ulong)(object)value;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                return (Half)(ulong)(nuint)(object)value;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        /// <inheritdoc cref="INumber{TSelf}.CreateTruncating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Half CreateTruncating<TOther>(TOther value)
            where TOther : INumber<TOther>
        {
            if (typeof(TOther) == typeof(byte))
            {
                return (Half)(byte)(object)value;
            }
            else if (typeof(TOther) == typeof(char))
            {
                return (Half)(char)(object)value;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                return (Half)(float)(decimal)(object)value;
            }
            else if (typeof(TOther) == typeof(double))
            {
                return (Half)(double)(object)value;
            }
            else if (typeof(TOther) == typeof(short))
            {
                return (Half)(short)(object)value;
            }
            else if (typeof(TOther) == typeof(int))
            {
                return (Half)(int)(object)value;
            }
            else if (typeof(TOther) == typeof(long))
            {
                return (Half)(long)(object)value;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                return (Half)(long)(nint)(object)value;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                return (Half)(sbyte)(object)value;
            }
            else if (typeof(TOther) == typeof(float))
            {
                return (Half)(float)(object)value;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                return (Half)(ushort)(object)value;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                return (Half)(uint)(object)value;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                return (Half)(ulong)(object)value;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                return (Half)(ulong)(nuint)(object)value;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        /// <inheritdoc cref="INumber{TSelf}.Max(TSelf, TSelf)" />
        public static Half Max(Half x, Half y) => (Half)MathF.Max((float)x, (float)y);

        /// <inheritdoc cref="INumber{TSelf}.MaxMagnitude(TSelf, TSelf)" />
        public static Half MaxMagnitude(Half x, Half y) => (Half)MathF.MaxMagnitude((float)x, (float)y);

        /// <inheritdoc cref="INumber{TSelf}.Min(TSelf, TSelf)" />
        public static Half Min(Half x, Half y) => (Half)MathF.Min((float)x, (float)y);

        /// <inheritdoc cref="INumber{TSelf}.MinMagnitude(TSelf, TSelf)" />
        public static Half MinMagnitude(Half x, Half y) => (Half)MathF.MinMagnitude((float)x, (float)y);

        /// <inheritdoc cref="INumber{TSelf}.Sign(TSelf)" />
        public static int Sign(Half value) => MathF.Sign((float)value);

        /// <inheritdoc cref="INumber{TSelf}.TryCreate{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryCreate<TOther>(TOther value, out Half result)
            where TOther : INumber<TOther>
        {
            if (typeof(TOther) == typeof(byte))
            {
                result = (Half)(byte)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                result = (Half)(char)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                result = (Half)(float)(decimal)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                result = (Half)(double)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                result = (Half)(short)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                result = (Half)(int)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                result = (Half)(long)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                result = (Half)(long)(nint)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                result = (Half)(sbyte)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                result = (Half)(float)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                result = (Half)(ushort)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                result = (Half)(uint)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                result = (Half)(ulong)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                result = (Half)(ulong)(nuint)(object)value;
                return true;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                result = default;
                return false;
            }
        }

        //
        // INumberBase
        //

        /// <inheritdoc cref="INumberBase{TSelf}.One" />
        static Half INumberBase<Half>.One => new Half(PositiveOneBits);

        /// <inheritdoc cref="INumberBase{TSelf}.Zero" />
        static Half INumberBase<Half>.Zero => new Half(PositiveZeroBits);

        //
        // IParsable
        //

        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Half result) => TryParse(s, DefaultParseStyle, provider, out result);

        //
        // IPowerFunctions
        //

        /// <inheritdoc cref="IPowerFunctions{TSelf}.Pow(TSelf, TSelf)" />
        public static Half Pow(Half x, Half y) => (Half)MathF.Pow((float)x, (float)y);

        //
        // IRootFunctions
        //

        /// <inheritdoc cref="IRootFunctions{TSelf}.Cbrt(TSelf)" />
        public static Half Cbrt(Half x) => (Half)MathF.Cbrt((float)x);

        // /// <inheritdoc cref="IRootFunctions{TSelf}.Hypot(TSelf, TSelf)" />
        // public static Half Hypot(Half x, Half y) => (Half)MathF.Hypot((float)x, (float)y);

        /// <inheritdoc cref="IRootFunctions{TSelf}.Sqrt(TSelf)" />
        public static Half Sqrt(Half x) => (Half)MathF.Sqrt((float)x);

        // /// <inheritdoc cref="IRootFunctions{TSelf}.Root(TSelf, TSelf)" />
        // public static Half Root(Half x, Half n) => (Half)MathF.Root((float)x, (float)n);

        //
        // ISignedNumber
        //

        /// <inheritdoc cref="ISignedNumber{TSelf}.NegativeOne" />
        static Half ISignedNumber<Half>.NegativeOne => new Half(NegativeOneBits);

        //
        // ISpanParsable
        //

        /// <inheritdoc cref="ISpanParsable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static Half Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, DefaultParseStyle, provider);

        /// <inheritdoc cref="ISpanParsable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Half result) => TryParse(s, DefaultParseStyle, provider, out result);

        //
        // ISubtractionOperators
        //

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_Subtraction(TSelf, TOther)" />
        public static Half operator -(Half left, Half right) => (Half)((float)left - (float)right);

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_CheckedSubtraction(TSelf, TOther)" />
        static Half ISubtractionOperators<Half, Half, Half>.operator checked -(Half left, Half right) => left - right;

        //
        // ITrigonometricFunctions
        //

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Acos(TSelf)" />
        public static Half Acos(Half x) => (Half)MathF.Acos((float)x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Asin(TSelf)" />
        public static Half Asin(Half x) => (Half)MathF.Asin((float)x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Atan(TSelf)" />
        public static Half Atan(Half x) => (Half)MathF.Atan((float)x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Atan2(TSelf, TSelf)" />
        public static Half Atan2(Half y, Half x) => (Half)MathF.Atan2((float)y, (float)x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Cos(TSelf)" />
        public static Half Cos(Half x) => (Half)MathF.Cos((float)x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Sin(TSelf)" />
        public static Half Sin(Half x) => (Half)MathF.Sin((float)x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinCos(TSelf)" />
        public static (Half Sin, Half Cos) SinCos(Half x)
        {
            var (sin, cos) = MathF.SinCos((float)x);
            return ((Half)sin, (Half)cos);
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Tan(TSelf)" />
        public static Half Tan(Half x) => (Half)MathF.Tan((float)x);

        // /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AcosPi(TSelf)" />
        // public static Half AcosPi(Half x) => (Half)MathF.AcosPi((float)x);

        // /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AsinPi(TSelf)" />
        // public static Half AsinPi(Half x) => (Half)MathF.AsinPi((float)x);

        // /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AtanPi(TSelf)" />
        // public static Half AtanPi(Half x) => (Half)MathF.AtanPi((float)x);

        // /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Atan2Pi(TSelf)" />
        // public static Half Atan2Pi(Half y, Half x) => (Half)MathF.Atan2Pi((float)y, (float)x);

        // /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.CosPi(TSelf)" />
        // public static Half CosPi(Half x) => (Half)MathF.CosPi((float)x);

        // /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinPi(TSelf)" />
        // public static Half SinPi(Half x) => (Half)MathF.SinPi((float)x, (float)y);

        // /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.TanPi(TSelf)" />
        // public static Half TanPi(Half x) => (Half)MathF.TanPi((float)x, (float)y);

        //
        // IUnaryNegationOperators
        //

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_UnaryNegation(TSelf)" />
        public static Half operator -(Half value) => (Half)(-(float)value);

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_CheckedUnaryNegation(TSelf)" />
        static Half IUnaryNegationOperators<Half, Half>.operator checked -(Half value) => -value;

        //
        // IUnaryPlusOperators
        //

        /// <inheritdoc cref="IUnaryPlusOperators{TSelf, TResult}.op_UnaryPlus(TSelf)" />
        public static Half operator +(Half value) => value;
    }
}
