// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System
{
    /// <summary>
    /// Represents a double-precision floating-point number.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public readonly struct Double
        : IComparable,
          IConvertible,
          ISpanFormattable,
          IComparable<double>,
          IEquatable<double>,
          IBinaryFloatingPointIeee754<double>,
          IMinMaxValue<double>,
          IUtf8SpanFormattable,
          IBinaryFloatParseAndFormatInfo<double>
    {
        private readonly double m_value; // Do not rename (binary serialization)

        //
        // Public Constants
        //
        public const double MinValue = -1.7976931348623157E+308;
        public const double MaxValue = 1.7976931348623157E+308;

        // Note Epsilon should be a double whose hex representation is 0x1
        // on little endian machines.
        public const double Epsilon = 4.9406564584124654E-324;
        public const double NegativeInfinity = (double)-1.0 / (double)(0.0);
        public const double PositiveInfinity = (double)1.0 / (double)(0.0);
        public const double NaN = (double)0.0 / (double)0.0;

        /// <summary>Represents the additive identity (0).</summary>
        internal const double AdditiveIdentity = 0.0;

        /// <summary>Represents the multiplicative identity (1).</summary>
        internal const double MultiplicativeIdentity = 1.0;

        /// <summary>Represents the number one (1).</summary>
        internal const double One = 1.0;

        /// <summary>Represents the number zero (0).</summary>
        internal const double Zero = 0.0;

        /// <summary>Represents the number negative one (-1).</summary>
        internal const double NegativeOne = -1.0;

        /// <summary>Represents the number negative zero (-0).</summary>
        public const double NegativeZero = -0.0;

        /// <summary>Represents the natural logarithmic base, specified by the constant, e.</summary>
        /// <remarks>Euler's number is approximately 2.7182818284590452354.</remarks>
        public const double E = Math.E;

        /// <summary>Represents the ratio of the circumference of a circle to its diameter, specified by the constant, PI.</summary>
        /// <remarks>Pi is approximately 3.1415926535897932385.</remarks>
        public const double Pi = Math.PI;

        /// <summary>Represents the number of radians in one turn, specified by the constant, Tau.</summary>
        /// <remarks>Tau is approximately 6.2831853071795864769.</remarks>
        public const double Tau = Math.Tau;

        //
        // Constants for manipulating the private bit-representation
        //

        internal const ulong SignMask = 0x8000_0000_0000_0000;
        internal const int SignShift = 63;
        internal const byte ShiftedSignMask = (byte)(SignMask >> SignShift);

        internal const ulong BiasedExponentMask = 0x7FF0_0000_0000_0000;
        internal const int BiasedExponentShift = 52;
        internal const int BiasedExponentLength = 11;
        internal const ushort ShiftedExponentMask = (ushort)(BiasedExponentMask >> BiasedExponentShift);

        internal const ulong TrailingSignificandMask = 0x000F_FFFF_FFFF_FFFF;

        internal const byte MinSign = 0;
        internal const byte MaxSign = 1;

        internal const ushort MinBiasedExponent = 0x0000;
        internal const ushort MaxBiasedExponent = 0x07FF;

        internal const ushort ExponentBias = 1023;

        internal const short MinExponent = -1022;
        internal const short MaxExponent = +1023;

        internal const ulong MinTrailingSignificand = 0x0000_0000_0000_0000;
        internal const ulong MaxTrailingSignificand = 0x000F_FFFF_FFFF_FFFF;

        internal const int TrailingSignificandLength = 52;
        internal const int SignificandLength = TrailingSignificandLength + 1;

        // Constants representing the private bit-representation for various default values

        internal const ulong PositiveZeroBits = 0x0000_0000_0000_0000;
        internal const ulong NegativeZeroBits = 0x8000_0000_0000_0000;

        internal const ulong EpsilonBits = 0x0000_0000_0000_0001;

        internal const ulong PositiveInfinityBits = 0x7FF0_0000_0000_0000;
        internal const ulong NegativeInfinityBits = 0xFFF0_0000_0000_0000;

        internal const ulong SmallestNormalBits = 0x0010_0000_0000_0000;

        internal ushort BiasedExponent
        {
            get
            {
                ulong bits = BitConverter.DoubleToUInt64Bits(m_value);
                return ExtractBiasedExponentFromBits(bits);
            }
        }

        internal short Exponent
        {
            get
            {
                return (short)(BiasedExponent - ExponentBias);
            }
        }

        internal ulong Significand
        {
            get
            {
                return TrailingSignificand | ((BiasedExponent != 0) ? (1UL << BiasedExponentShift) : 0UL);
            }
        }

        internal ulong TrailingSignificand
        {
            get
            {
                ulong bits = BitConverter.DoubleToUInt64Bits(m_value);
                return ExtractTrailingSignificandFromBits(bits);
            }
        }

        internal static ushort ExtractBiasedExponentFromBits(ulong bits)
        {
            return (ushort)((bits >> BiasedExponentShift) & ShiftedExponentMask);
        }

        internal static ulong ExtractTrailingSignificandFromBits(ulong bits)
        {
            return bits & TrailingSignificandMask;
        }

        /// <summary>Determines whether the specified value is finite (zero, subnormal, or normal).</summary>
        /// <remarks>This effectively checks the value is not NaN and not infinite.</remarks>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFinite(double d)
        {
            ulong bits = BitConverter.DoubleToUInt64Bits(d);
            return (~bits & PositiveInfinityBits) != 0;
        }

        /// <summary>Determines whether the specified value is infinite.</summary>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInfinity(double d)
        {
            ulong bits = BitConverter.DoubleToUInt64Bits(d);
            return (bits & ~SignMask) == PositiveInfinityBits;
        }

        /// <summary>Determines whether the specified value is NaN.</summary>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNaN(double d)
        {
            // A NaN will never equal itself so this is an
            // easy and efficient way to check for NaN.

            #pragma warning disable CS1718
            return d != d;
            #pragma warning restore CS1718
        }

        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsNaNOrZero(double d)
        {
            ulong bits = BitConverter.DoubleToUInt64Bits(d);
            return ((bits - 1) & ~SignMask) >= PositiveInfinityBits;
        }

        /// <summary>Determines whether the specified value is negative.</summary>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNegative(double d)
        {
            return BitConverter.DoubleToInt64Bits(d) < 0;
        }

        /// <summary>Determines whether the specified value is negative infinity.</summary>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNegativeInfinity(double d)
        {
            return d == NegativeInfinity;
        }

        /// <summary>Determines whether the specified value is normal (finite, but not zero or subnormal).</summary>
        /// <remarks>This effectively checks the value is not NaN, not infinite, not subnormal, and not zero.</remarks>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNormal(double d)
        {
            ulong bits = BitConverter.DoubleToUInt64Bits(d);
            return ((bits & ~SignMask) - SmallestNormalBits) < (PositiveInfinityBits - SmallestNormalBits);
        }

        /// <summary>Determines whether the specified value is positive infinity.</summary>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPositiveInfinity(double d)
        {
            return d == PositiveInfinity;
        }

        /// <summary>Determines whether the specified value is subnormal (finite, but not zero or normal).</summary>
        /// <remarks>This effectively checks the value is not NaN, not infinite, not normal, and not zero.</remarks>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSubnormal(double d)
        {
            ulong bits = BitConverter.DoubleToUInt64Bits(d);
            return ((bits & ~SignMask) - 1) < MaxTrailingSignificand;
        }

        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsZero(double d)
        {
            return d == 0;
        }

        // Compares this object to another object, returning an instance of System.Relation.
        // Null is considered less than any instance.
        //
        // If object is not of type Double, this method throws an ArgumentException.
        //
        // Returns a value less than zero if this  object
        //
        public int CompareTo(object? value)
        {
            if (value is not double other)
            {
                return (value is null) ? 1 : throw new ArgumentException(SR.Arg_MustBeDouble);
            }
            return CompareTo(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(double value)
        {
            if (m_value < value)
            {
                return -1;
            }

            if (m_value > value)
            {
                return 1;
            }

            if (m_value == value)
            {
                return 0;
            }

            if (IsNaN(m_value))
            {
                return IsNaN(value) ? 0 : -1;
            }

            Debug.Assert(IsNaN(value));
            return 1;
        }

        // True if obj is another Double with the same value as the current instance.  This is
        // a method of object equality, that only returns true if obj is also a double.
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return (obj is double other) && Equals(other);
        }

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther, TResult}.op_Equality(TSelf, TOther)" />
        [NonVersionable]
        public static bool operator ==(double left, double right) => left == right;

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther, TResult}.op_Inequality(TSelf, TOther)" />
        [NonVersionable]
        public static bool operator !=(double left, double right) => left != right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_LessThan(TSelf, TOther)" />
        [NonVersionable]
        public static bool operator <(double left, double right) => left < right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_GreaterThan(TSelf, TOther)" />
        [NonVersionable]
        public static bool operator >(double left, double right) => left > right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_LessThanOrEqual(TSelf, TOther)" />
        [NonVersionable]
        public static bool operator <=(double left, double right) => left <= right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_GreaterThanOrEqual(TSelf, TOther)" />
        [NonVersionable]
        public static bool operator >=(double left, double right) => left >= right;

        public bool Equals(double obj)
        {
            if (obj == m_value)
            {
                return true;
            }
            return IsNaN(obj) && IsNaN(m_value);
        }

        // The hashcode for a double is the absolute value of the integer representation
        // of that double.
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // 64-bit constants make the IL unusually large that makes the inliner to reject the method
        public override int GetHashCode()
        {
            ulong bits = BitConverter.DoubleToUInt64Bits(m_value);

            if (IsNaNOrZero(m_value))
            {
                // Ensure that all NaNs and both zeros have the same hash code
                bits &= PositiveInfinityBits;
            }

            return unchecked((int)bits) ^ ((int)(bits >> 32));
        }

        public override string ToString()
        {
            return Number.FormatDouble(m_value, null, NumberFormatInfo.CurrentInfo);
        }

        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
        {
            return Number.FormatDouble(m_value, format, NumberFormatInfo.CurrentInfo);
        }

        public string ToString(IFormatProvider? provider)
        {
            return Number.FormatDouble(m_value, null, NumberFormatInfo.GetInstance(provider));
        }

        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider)
        {
            return Number.FormatDouble(m_value, format, NumberFormatInfo.GetInstance(provider));
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatDouble(m_value, format, NumberFormatInfo.GetInstance(provider), destination, out charsWritten);
        }

        /// <inheritdoc cref="IUtf8SpanFormattable.TryFormat" />
        public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatDouble(m_value, format, NumberFormatInfo.GetInstance(provider), utf8Destination, out bytesWritten);
        }

        public static double Parse(string s) => Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider: null);

        public static double Parse(string s, NumberStyles style) => Parse(s, style, provider: null);

        public static double Parse(string s, IFormatProvider? provider) => Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider);

        public static double Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            if (s is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            return Parse(s.AsSpan(), style, provider);
        }

        // Parses a double from a String in the given style.  If
        // a NumberFormatInfo isn't specified, the current culture's
        // NumberFormatInfo is assumed.
        //
        // This method will not throw an OverflowException, but will return
        // PositiveInfinity or NegativeInfinity for a number that is too
        // large or too small.

        public static double Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Number.ParseFloat<char, double>(s, style, NumberFormatInfo.GetInstance(provider));
        }

        public static bool TryParse([NotNullWhen(true)] string? s, out double result) => TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider: null, out result);

        public static bool TryParse(ReadOnlySpan<char> s, out double result) => TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider: null, out result);

        /// <summary>Tries to convert a UTF-8 character span containing the string representation of a number to its double-precision floating-point number equivalent.</summary>
        /// <param name="utf8Text">A read-only UTF-8 character span that contains the number to convert.</param>
        /// <param name="result">When this method returns, contains a double-precision floating-point number equivalent of the numeric value or symbol contained in <paramref name="utf8Text" /> if the conversion succeeded or zero if the conversion failed. The conversion fails if the <paramref name="utf8Text" /> is <see cref="ReadOnlySpan{T}.Empty" /> or is not in a valid format. This parameter is passed uninitialized; any value originally supplied in result will be overwritten.</param>
        /// <returns><c>true</c> if <paramref name="utf8Text" /> was converted successfully; otherwise, false.</returns>
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, out double result) => TryParse(utf8Text, NumberStyles.Float | NumberStyles.AllowThousands, provider: null, out result);

        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out double result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);

            if (s == null)
            {
                result = 0;
                return false;
            }
            return Number.TryParseFloat(s.AsSpan(), style, NumberFormatInfo.GetInstance(provider), out result);
        }

        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out double result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Number.TryParseFloat(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        //
        // IConvertible implementation
        //

        public TypeCode GetTypeCode()
        {
            return TypeCode.Double;
        }

        bool IConvertible.ToBoolean(IFormatProvider? provider)
        {
            return Convert.ToBoolean(m_value);
        }

        char IConvertible.ToChar(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Double", "Char"));
        }

        sbyte IConvertible.ToSByte(IFormatProvider? provider)
        {
            return Convert.ToSByte(m_value);
        }

        byte IConvertible.ToByte(IFormatProvider? provider)
        {
            return Convert.ToByte(m_value);
        }

        short IConvertible.ToInt16(IFormatProvider? provider)
        {
            return Convert.ToInt16(m_value);
        }

        ushort IConvertible.ToUInt16(IFormatProvider? provider)
        {
            return Convert.ToUInt16(m_value);
        }

        int IConvertible.ToInt32(IFormatProvider? provider)
        {
            return Convert.ToInt32(m_value);
        }

        uint IConvertible.ToUInt32(IFormatProvider? provider)
        {
            return Convert.ToUInt32(m_value);
        }

        long IConvertible.ToInt64(IFormatProvider? provider)
        {
            return Convert.ToInt64(m_value);
        }

        ulong IConvertible.ToUInt64(IFormatProvider? provider)
        {
            return Convert.ToUInt64(m_value);
        }

        float IConvertible.ToSingle(IFormatProvider? provider)
        {
            return Convert.ToSingle(m_value);
        }

        double IConvertible.ToDouble(IFormatProvider? provider)
        {
            return m_value;
        }

        decimal IConvertible.ToDecimal(IFormatProvider? provider)
        {
            return Convert.ToDecimal(m_value);
        }

        DateTime IConvertible.ToDateTime(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Double", "DateTime"));
        }

        object IConvertible.ToType(Type type, IFormatProvider? provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }

        //
        // IAdditionOperators
        //

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        static double IAdditionOperators<double, double, double>.operator +(double left, double right) => left + right;

        //
        // IAdditiveIdentity
        //

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static double IAdditiveIdentity<double, double>.AdditiveIdentity => AdditiveIdentity;

        //
        // IBinaryNumber
        //

        /// <inheritdoc cref="IBinaryNumber{TSelf}.AllBitsSet" />
        static double IBinaryNumber<double>.AllBitsSet => BitConverter.UInt64BitsToDouble(0xFFFF_FFFF_FFFF_FFFF);

        /// <inheritdoc cref="IBinaryNumber{TSelf}.IsPow2(TSelf)" />
        public static bool IsPow2(double value)
        {
            ulong bits = BitConverter.DoubleToUInt64Bits(value);

            if ((long)bits <= 0)
            {
                // Zero and negative values cannot be powers of 2
                return false;
            }

            ushort biasedExponent = ExtractBiasedExponentFromBits(bits);
            ulong trailingSignificand = ExtractTrailingSignificandFromBits(bits);

            if (biasedExponent == MinBiasedExponent)
            {
                // Subnormal values have 1 bit set when they're powers of 2
                return ulong.PopCount(trailingSignificand) == 1;
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
        [Intrinsic]
        public static double Log2(double value) => Math.Log2(value);

        //
        // IBitwiseOperators
        //

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseAnd(TSelf, TOther)" />
        static double IBitwiseOperators<double, double, double>.operator &(double left, double right)
        {
            ulong bits = BitConverter.DoubleToUInt64Bits(left) & BitConverter.DoubleToUInt64Bits(right);
            return BitConverter.UInt64BitsToDouble(bits);
        }

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseOr(TSelf, TOther)" />
        static double IBitwiseOperators<double, double, double>.operator |(double left, double right)
        {
            ulong bits = BitConverter.DoubleToUInt64Bits(left) | BitConverter.DoubleToUInt64Bits(right);
            return BitConverter.UInt64BitsToDouble(bits);
        }

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_ExclusiveOr(TSelf, TOther)" />
        static double IBitwiseOperators<double, double, double>.operator ^(double left, double right)
        {
            ulong bits = BitConverter.DoubleToUInt64Bits(left) ^ BitConverter.DoubleToUInt64Bits(right);
            return BitConverter.UInt64BitsToDouble(bits);
        }

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_OnesComplement(TSelf)" />
        static double IBitwiseOperators<double, double, double>.operator ~(double value)
        {
            ulong bits = ~BitConverter.DoubleToUInt64Bits(value);
            return BitConverter.UInt64BitsToDouble(bits);
        }

        //
        // IDecrementOperators
        //

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        static double IDecrementOperators<double>.operator --(double value) => --value;

        //
        // IDivisionOperators
        //

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_Division(TSelf, TOther)" />
        static double IDivisionOperators<double, double, double>.operator /(double left, double right) => left / right;

        //
        // IExponentialFunctions
        //

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp" />
        [Intrinsic]
        public static double Exp(double x) => Math.Exp(x);

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.ExpM1(TSelf)" />
        public static double ExpM1(double x) => Math.Exp(x) - 1;

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp2(TSelf)" />
        public static double Exp2(double x) => Math.Pow(2, x);

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp2M1(TSelf)" />
        public static double Exp2M1(double x) => Math.Pow(2, x) - 1;

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp10(TSelf)" />
        public static double Exp10(double x) => Math.Pow(10, x);

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp10M1(TSelf)" />
        public static double Exp10M1(double x) => Math.Pow(10, x) - 1;

        //
        // IFloatingPoint
        //

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Ceiling(TSelf)" />
        [Intrinsic]
        public static double Ceiling(double x) => Math.Ceiling(x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Floor(TSelf)" />
        [Intrinsic]
        public static double Floor(double x) => Math.Floor(x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf)" />
        [Intrinsic]
        public static double Round(double x) => Math.Round(x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, int)" />
        public static double Round(double x, int digits) => Math.Round(x, digits);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, MidpointRounding)" />
        public static double Round(double x, MidpointRounding mode) => Math.Round(x, mode);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, int, MidpointRounding)" />
        public static double Round(double x, int digits, MidpointRounding mode) => Math.Round(x, digits, mode);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Truncate(TSelf)" />
        [Intrinsic]
        public static double Truncate(double x) => Math.Truncate(x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetExponentByteCount()" />
        int IFloatingPoint<double>.GetExponentByteCount() => sizeof(short);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetExponentShortestBitLength()" />
        int IFloatingPoint<double>.GetExponentShortestBitLength()
        {
            short exponent = Exponent;

            if (exponent >= 0)
            {
                return (sizeof(short) * 8) - short.LeadingZeroCount(exponent);
            }
            else
            {
                return (sizeof(short) * 8) + 1 - short.LeadingZeroCount((short)(~exponent));
            }
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetSignificandByteCount()" />
        int IFloatingPoint<double>.GetSignificandByteCount() => sizeof(ulong);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetSignificandBitLength()" />
        int IFloatingPoint<double>.GetSignificandBitLength() => 53;

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteExponentBigEndian(Span{byte}, out int)" />
        bool IFloatingPoint<double>.TryWriteExponentBigEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(short))
            {
                short exponent = Exponent;

                if (BitConverter.IsLittleEndian)
                {
                    exponent = BinaryPrimitives.ReverseEndianness(exponent);
                }

                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), exponent);

                bytesWritten = sizeof(short);
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteExponentLittleEndian(Span{byte}, out int)" />
        bool IFloatingPoint<double>.TryWriteExponentLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(short))
            {
                short exponent = Exponent;

                if (!BitConverter.IsLittleEndian)
                {
                    exponent = BinaryPrimitives.ReverseEndianness(exponent);
                }

                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), exponent);

                bytesWritten = sizeof(short);
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteSignificandBigEndian(Span{byte}, out int)" />
        bool IFloatingPoint<double>.TryWriteSignificandBigEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(ulong))
            {
                ulong significand = Significand;

                if (BitConverter.IsLittleEndian)
                {
                    significand = BinaryPrimitives.ReverseEndianness(significand);
                }

                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), significand);

                bytesWritten = sizeof(ulong);
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteSignificandLittleEndian(Span{byte}, out int)" />
        bool IFloatingPoint<double>.TryWriteSignificandLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(ulong))
            {
                ulong significand = Significand;

                if (!BitConverter.IsLittleEndian)
                {
                    significand = BinaryPrimitives.ReverseEndianness(significand);
                }

                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), significand);

                bytesWritten = sizeof(ulong);
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
        static double IFloatingPointConstants<double>.E => Math.E;

        /// <inheritdoc cref="IFloatingPointConstants{TSelf}.Pi" />
        static double IFloatingPointConstants<double>.Pi => Pi;

        /// <inheritdoc cref="IFloatingPointConstants{TSelf}.Tau" />
        static double IFloatingPointConstants<double>.Tau => Tau;

        //
        // IFloatingPointIeee754
        //

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Epsilon" />
        static double IFloatingPointIeee754<double>.Epsilon => Epsilon;

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.NaN" />
        static double IFloatingPointIeee754<double>.NaN => NaN;

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.NegativeInfinity" />
        static double IFloatingPointIeee754<double>.NegativeInfinity => NegativeInfinity;

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.NegativeZero" />
        static double IFloatingPointIeee754<double>.NegativeZero => NegativeZero;

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.PositiveInfinity" />
        static double IFloatingPointIeee754<double>.PositiveInfinity => PositiveInfinity;

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Atan2(TSelf, TSelf)" />
        [Intrinsic]
        public static double Atan2(double y, double x) => Math.Atan2(y, x);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Atan2Pi(TSelf, TSelf)" />
        public static double Atan2Pi(double y, double x) => Atan2(y, x) / Pi;

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.BitDecrement(TSelf)" />
        public static double BitDecrement(double x) => Math.BitDecrement(x);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.BitIncrement(TSelf)" />
        public static double BitIncrement(double x) => Math.BitIncrement(x);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.FusedMultiplyAdd(TSelf, TSelf, TSelf)" />
        [Intrinsic]
        public static double FusedMultiplyAdd(double left, double right, double addend) => Math.FusedMultiplyAdd(left, right, addend);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Ieee754Remainder(TSelf, TSelf)" />
        public static double Ieee754Remainder(double left, double right) => Math.IEEERemainder(left, right);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ILogB(TSelf)" />
        public static int ILogB(double x) => Math.ILogB(x);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Lerp(TSelf, TSelf, TSelf)" />
        public static double Lerp(double value1, double value2, double amount) => (value1 * (1.0 - amount)) + (value2 * amount);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ReciprocalEstimate(TSelf)" />
        public static double ReciprocalEstimate(double x) => Math.ReciprocalEstimate(x);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ReciprocalSqrtEstimate(TSelf)" />
        public static double ReciprocalSqrtEstimate(double x) => Math.ReciprocalSqrtEstimate(x);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ScaleB(TSelf, int)" />
        public static double ScaleB(double x, int n) => Math.ScaleB(x, n);

        // /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Compound(TSelf, TSelf)" />
        // public static double Compound(double x, double n) => Math.Compound(x, n);

        //
        // IHyperbolicFunctions
        //

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Acosh(TSelf)" />
        [Intrinsic]
        public static double Acosh(double x) => Math.Acosh(x);

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Asinh(TSelf)" />
        [Intrinsic]
        public static double Asinh(double x) => Math.Asinh(x);

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Atanh(TSelf)" />
        [Intrinsic]
        public static double Atanh(double x) => Math.Atanh(x);

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Cosh(TSelf)" />
        [Intrinsic]
        public static double Cosh(double x) => Math.Cosh(x);

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Sinh(TSelf)" />
        [Intrinsic]
        public static double Sinh(double x) => Math.Sinh(x);

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Tanh(TSelf)" />
        [Intrinsic]
        public static double Tanh(double x) => Math.Tanh(x);

        //
        // IIncrementOperators
        //

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_Increment(TSelf)" />
        static double IIncrementOperators<double>.operator ++(double value) => ++value;

        //
        // ILogarithmicFunctions
        //

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log(TSelf)" />
        [Intrinsic]
        public static double Log(double x) => Math.Log(x);

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log(TSelf, TSelf)" />
        public static double Log(double x, double newBase) => Math.Log(x, newBase);

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.LogP1(TSelf)" />
        public static double LogP1(double x) => Math.Log(x + 1);

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log2P1(TSelf)" />
        public static double Log2P1(double x) => Math.Log2(x + 1);

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log10(TSelf)" />
        [Intrinsic]
        public static double Log10(double x) => Math.Log10(x);

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log10P1(TSelf)" />
        public static double Log10P1(double x) => Math.Log10(x + 1);

        //
        // IMinMaxValue
        //

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MinValue" />
        static double IMinMaxValue<double>.MinValue => MinValue;

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MaxValue" />
        static double IMinMaxValue<double>.MaxValue => MaxValue;

        //
        // IModulusOperators
        //

        /// <inheritdoc cref="IModulusOperators{TSelf, TOther, TResult}.op_Modulus(TSelf, TOther)" />
        static double IModulusOperators<double, double, double>.operator %(double left, double right) => left % right;

        //
        // IMultiplicativeIdentity
        //

        /// <inheritdoc cref="IMultiplicativeIdentity{TSelf, TResult}.MultiplicativeIdentity" />
        static double IMultiplicativeIdentity<double, double>.MultiplicativeIdentity => MultiplicativeIdentity;

        //
        // IMultiplyOperators
        //

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_Multiply(TSelf, TOther)" />
        static double IMultiplyOperators<double, double, double>.operator *(double left, double right) => left * right;

        //
        // INumber
        //

        /// <inheritdoc cref="INumber{TSelf}.Clamp(TSelf, TSelf, TSelf)" />
        public static double Clamp(double value, double min, double max) => Math.Clamp(value, min, max);

        /// <inheritdoc cref="INumber{TSelf}.CopySign(TSelf, TSelf)" />
        public static double CopySign(double value, double sign) => Math.CopySign(value, sign);

        /// <inheritdoc cref="INumber{TSelf}.Max(TSelf, TSelf)" />
        [Intrinsic]
        public static double Max(double x, double y) => Math.Max(x, y);

        /// <inheritdoc cref="INumber{TSelf}.MaxNumber(TSelf, TSelf)" />
        [Intrinsic]
        public static double MaxNumber(double x, double y)
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
        [Intrinsic]
        public static double Min(double x, double y) => Math.Min(x, y);

        /// <inheritdoc cref="INumber{TSelf}.MinNumber(TSelf, TSelf)" />
        [Intrinsic]
        public static double MinNumber(double x, double y)
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
        public static int Sign(double value) => Math.Sign(value);

        //
        // INumberBase
        //

        /// <inheritdoc cref="INumberBase{TSelf}.One" />
        static double INumberBase<double>.One => One;

        /// <inheritdoc cref="INumberBase{TSelf}.Radix" />
        static int INumberBase<double>.Radix => 2;

        /// <inheritdoc cref="INumberBase{TSelf}.Zero" />
        static double INumberBase<double>.Zero => Zero;

        /// <inheritdoc cref="INumberBase{TSelf}.Abs(TSelf)" />
        [Intrinsic]
        public static double Abs(double value) => Math.Abs(value);

        /// <inheritdoc cref="INumberBase{TSelf}.CreateChecked{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double CreateChecked<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            double result;

            if (typeof(TOther) == typeof(double))
            {
                result = (double)(object)value;
            }
            else if (!TryConvertFrom(value, out result) && !TOther.TryConvertToChecked(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateSaturating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double CreateSaturating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            double result;

            if (typeof(TOther) == typeof(double))
            {
                result = (double)(object)value;
            }
            else if (!TryConvertFrom(value, out result) && !TOther.TryConvertToSaturating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateTruncating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double CreateTruncating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            double result;

            if (typeof(TOther) == typeof(double))
            {
                result = (double)(object)value;
            }
            else if (!TryConvertFrom(value, out result) && !TOther.TryConvertToTruncating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsCanonical(TSelf)" />
        static bool INumberBase<double>.IsCanonical(double value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsComplexNumber(TSelf)" />
        static bool INumberBase<double>.IsComplexNumber(double value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsEvenInteger(TSelf)" />
        public static bool IsEvenInteger(double value) => IsInteger(value) && (Abs(value % 2) == 0);

        /// <inheritdoc cref="INumberBase{TSelf}.IsImaginaryNumber(TSelf)" />
        static bool INumberBase<double>.IsImaginaryNumber(double value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsInteger(TSelf)" />
        public static bool IsInteger(double value) => IsFinite(value) && (value == Truncate(value));

        /// <inheritdoc cref="INumberBase{TSelf}.IsOddInteger(TSelf)" />
        public static bool IsOddInteger(double value) => IsInteger(value) && (Abs(value % 2) == 1);

        /// <inheritdoc cref="INumberBase{TSelf}.IsPositive(TSelf)" />
        public static bool IsPositive(double value) => BitConverter.DoubleToInt64Bits(value) >= 0;

        /// <inheritdoc cref="INumberBase{TSelf}.IsRealNumber(TSelf)" />
        public static bool IsRealNumber(double value)
        {
            // A NaN will never equal itself so this is an
            // easy and efficient way to check for a real number.

#pragma warning disable CS1718
            return value == value;
#pragma warning restore CS1718
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsZero(TSelf)" />
        static bool INumberBase<double>.IsZero(double value) => IsZero(value);

        /// <inheritdoc cref="INumberBase{TSelf}.MaxMagnitude(TSelf, TSelf)" />
        [Intrinsic]
        public static double MaxMagnitude(double x, double y) => Math.MaxMagnitude(x, y);

        /// <inheritdoc cref="INumberBase{TSelf}.MaxMagnitudeNumber(TSelf, TSelf)" />
        [Intrinsic]
        public static double MaxMagnitudeNumber(double x, double y)
        {
            // This matches the IEEE 754:2019 `maximumMagnitudeNumber` function
            //
            // It does not propagate NaN inputs back to the caller and
            // otherwise returns the input with a larger magnitude.
            // It treats +0 as larger than -0 as per the specification.

            double ax = Abs(x);
            double ay = Abs(y);

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
        [Intrinsic]
        public static double MinMagnitude(double x, double y) => Math.MinMagnitude(x, y);

        /// <inheritdoc cref="INumberBase{TSelf}.MinMagnitudeNumber(TSelf, TSelf)" />
        [Intrinsic]
        public static double MinMagnitudeNumber(double x, double y)
        {
            // This matches the IEEE 754:2019 `minimumMagnitudeNumber` function
            //
            // It does not propagate NaN inputs back to the caller and
            // otherwise returns the input with a larger magnitude.
            // It treats +0 as larger than -0 as per the specification.

            double ax = Abs(x);
            double ay = Abs(y);

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
        static bool INumberBase<double>.TryConvertFromChecked<TOther>(TOther value, out double result)
        {
            return TryConvertFrom(value, out result);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromSaturating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<double>.TryConvertFromSaturating<TOther>(TOther value, out double result)
        {
            return TryConvertFrom(value, out result);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromTruncating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<double>.TryConvertFromTruncating<TOther>(TOther value, out double result)
        {
            return TryConvertFrom(value, out result);
        }

        private static bool TryConvertFrom<TOther>(TOther value, out double result)
            where TOther : INumberBase<TOther>
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `double` will handle the other signed types and
            // `ConvertTo` will handle the unsigned types

            if (typeof(TOther) == typeof(Half))
            {
                Half actualValue = (Half)(object)value;
                result = (double)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualValue = (short)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualValue = (int)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualValue = (long)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualValue = (Int128)(object)value;
                result = (double)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualValue = (nint)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualValue = (sbyte)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualValue = (float)(object)value;
                result = actualValue;
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
        static bool INumberBase<double>.TryConvertToChecked<TOther>(double value, [MaybeNullWhen(false)] out TOther result)
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `double` will handle the other signed types and
            // `ConvertTo` will handle the unsigned types

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
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualResult = checked((decimal)value);
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
            else
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToSaturating{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<double>.TryConvertToSaturating<TOther>(double value, [MaybeNullWhen(false)] out TOther result)
        {
            return TryConvertTo(value, out result);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToTruncating{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<double>.TryConvertToTruncating<TOther>(double value, [MaybeNullWhen(false)] out TOther result)
        {
            return TryConvertTo(value, out result);
        }

        private static bool TryConvertTo<TOther>(double value, [MaybeNullWhen(false)] out TOther result)
            where TOther : INumberBase<TOther>
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `double` will handle the other signed types and
            // `ConvertTo` will handle the unsigned types

            if (typeof(TOther) == typeof(byte))
            {
                byte actualResult = (value >= byte.MaxValue) ? byte.MaxValue :
                                    (value <= byte.MinValue) ? byte.MinValue : (byte)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualResult = (value >= char.MaxValue) ? char.MaxValue :
                                    (value <= char.MinValue) ? char.MinValue : (char)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualResult = (value >= +79228162514264337593543950336.0) ? decimal.MaxValue :
                                       (value <= -79228162514264337593543950336.0) ? decimal.MinValue :
                                       IsNaN(value) ? 0.0m : (decimal)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                ushort actualResult = (value >= ushort.MaxValue) ? ushort.MaxValue :
                                      (value <= ushort.MinValue) ? ushort.MinValue : (ushort)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualResult = (value >= uint.MaxValue) ? uint.MaxValue :
                                    (value <= uint.MinValue) ? uint.MinValue : (uint)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualResult = (value >= ulong.MaxValue) ? ulong.MaxValue :
                                     (value <= ulong.MinValue) ? ulong.MinValue :
                                     IsNaN(value) ? 0 : (ulong)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualResult = (value >= 340282366920938463463374607431768211455.0) ? UInt128.MaxValue :
                                       (value <= 0.0) ? UInt128.MinValue : (UInt128)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
#if TARGET_64BIT
                nuint actualResult = (value >= ulong.MaxValue) ? unchecked((nuint)ulong.MaxValue) :
                                     (value <= ulong.MinValue) ? unchecked((nuint)ulong.MinValue) : (nuint)value;
                result = (TOther)(object)actualResult;
                return true;
#else
                nuint actualResult = (value >= uint.MaxValue) ? uint.MaxValue :
                                     (value <= uint.MinValue) ? uint.MinValue : (nuint)value;
                result = (TOther)(object)actualResult;
                return true;
#endif
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
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out double result) => TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);

        //
        // IPowerFunctions
        //

        /// <inheritdoc cref="IPowerFunctions{TSelf}.Pow(TSelf, TSelf)" />
        [Intrinsic]
        public static double Pow(double x, double y) => Math.Pow(x, y);

        //
        // IRootFunctions
        //

        /// <inheritdoc cref="IRootFunctions{TSelf}.Cbrt(TSelf)" />
        [Intrinsic]
        public static double Cbrt(double x) => Math.Cbrt(x);

        /// <inheritdoc cref="IRootFunctions{TSelf}.Hypot(TSelf, TSelf)" />
        public static double Hypot(double x, double y)
        {
            // This code is based on `hypot` from amd/aocl-libm-ose
            // Copyright (C) 2008-2020 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            double result;

            if (IsFinite(x) && IsFinite(y))
            {
                double ax = Abs(x);
                double ay = Abs(y);

                if (ax == 0.0f)
                {
                    result = ay;
                }
                else if (ay == 0.0f)
                {
                    result = ax;
                }
                else
                {
                    ulong xBits = BitConverter.DoubleToUInt64Bits(ax);
                    ulong yBits = BitConverter.DoubleToUInt64Bits(ay);

                    uint xExp = (uint)((xBits >> BiasedExponentShift) & ShiftedExponentMask);
                    uint yExp = (uint)((yBits >> BiasedExponentShift) & ShiftedExponentMask);

                    int expDiff = (int)(xExp - yExp);
                    double expFix = 1.0;

                    if ((expDiff <= (SignificandLength + 1)) && (expDiff >= (-SignificandLength - 1)))
                    {
                        if ((xExp > (ExponentBias + 500)) || (yExp > (ExponentBias + 500)))
                        {
                            // To prevent overflow, scale down by 2^+600
                            expFix = 4.149515568880993E+180;

                            xBits -= 0x2580000000000000;
                            yBits -= 0x2580000000000000;
                        }
                        else if ((xExp < (ExponentBias - 500)) || (yExp < (ExponentBias - 500)))
                        {
                            // To prevent underflow, scale up by 2^-600
                            expFix = 2.409919865102884E-181;

                            xBits += 0x2580000000000000;
                            yBits += 0x2580000000000000;

                            // For subnormal values, do an additional fixing up changing the
                            // adjustment to scale up by 2^601 instead and then subtract a
                            // correction of 2^601 to account for the implicit bit.

                            if (xExp == 0) // x is subnormal
                            {
                                xBits += 0x0010000000000000;

                                ax = BitConverter.UInt64BitsToDouble(xBits);
                                ax -= 9.232978617785736E-128;

                                xBits = BitConverter.DoubleToUInt64Bits(ax);
                            }

                            if (yExp == 0) // y is subnormal
                            {
                                yBits += 0x0010000000000000;

                                ay = BitConverter.UInt64BitsToDouble(yBits);
                                ay -= 9.232978617785736E-128;

                                yBits = BitConverter.DoubleToUInt64Bits(ay);
                            }
                        }

                        ax = BitConverter.UInt64BitsToDouble(xBits);
                        ay = BitConverter.UInt64BitsToDouble(yBits);

                        if (ax < ay)
                        {
                            // Sort so ax is greater than ay
                            double tmp = ax;

                            ax = ay;
                            ay = tmp;

                            ulong tmpBits = xBits;

                            xBits = yBits;
                            yBits = tmpBits;
                        }

                        Debug.Assert(ax >= ay);

                        // Split ax and ay into a head and tail portion

                        double xHead = BitConverter.UInt64BitsToDouble(xBits & 0xFFFF_FFFF_F800_0000);
                        double yHead = BitConverter.UInt64BitsToDouble(yBits & 0xFFFF_FFFF_F800_0000);

                        double xTail = ax - xHead;
                        double yTail = ay - yHead;

                        // Compute (x * x) + (y * y) with extra precision
                        //
                        // This includes taking into account expFix which may
                        // cause an underflow or overflow, but if it does that
                        // will still be the correct result.

                        double xx = ax * ax;
                        double yy = ay * ay;

                        double rHead = xx + yy;
                        double rTail = (xx - rHead) + yy;

                        rTail += (xHead * xHead) - xx;
                        rTail += 2 * xHead * xTail;
                        rTail += xTail * xTail;

                        if (expDiff == 0)
                        {
                            // We only need to do extra accounting when ax and ay have equal exponents

                            rTail += (yHead * yHead) - yy;
                            rTail += 2 * yHead * yTail;
                            rTail += yTail * yTail;
                        }

                        result = Sqrt(rHead + rTail) * expFix;
                    }
                    else
                    {
                        // x or y is insignificant compared to the other
                        result = ax + ay;
                    }
                }
            }
            else if (IsInfinity(x) || IsInfinity(y))
            {
                // IEEE 754 requires that we return +Infinity
                // even if one of the inputs is NaN

                result = PositiveInfinity;
            }
            else
            {
                // IEEE 754 requires that we return NaN
                // if either input is NaN and neither is Infinity

                result = NaN;
            }

            return result;
        }

        /// <inheritdoc cref="IRootFunctions{TSelf}.RootN(TSelf, int)" />
        public static double RootN(double x, int n)
        {
            double result;

            if (n > 0)
            {
                if (n == 2)
                {
                    result = (x != 0.0) ? Sqrt(x) : 0.0;
                }
                else if (n == 3)
                {
                    result = Cbrt(x);
                }
                else
                {
                    result = PositiveN(x, n);
                }
            }
            else if (n < 0)
            {
                result = NegativeN(x, n);
            }
            else
            {
                Debug.Assert(n == 0);
                result = NaN;
            }

            return result;

            static double PositiveN(double x, int n)
            {
                double result;

                if (IsFinite(x))
                {
                    if (x != 0)
                    {
                        if ((x > 0) || IsOddInteger(n))
                        {
                            result = Pow(Abs(x), 1.0 / n);
                            result = CopySign(result, x);
                        }
                        else
                        {
                            result = NaN;
                        }
                    }
                    else if (IsEvenInteger(n))
                    {
                        result = 0.0;
                    }
                    else
                    {
                        result = CopySign(0.0, x);
                    }
                }
                else if (IsNaN(x))
                {
                    result = NaN;
                }
                else if (x > 0)
                {
                    Debug.Assert(IsPositiveInfinity(x));
                    result = PositiveInfinity;
                }
                else
                {
                    Debug.Assert(IsNegativeInfinity(x));
                    result = int.IsOddInteger(n) ? NegativeInfinity : NaN;
                }

                return result;
            }

            static double NegativeN(double x, int n)
            {
                double result;

                if (IsFinite(x))
                {
                    if (x != 0)
                    {
                        if ((x > 0) || IsOddInteger(n))
                        {
                            result = Pow(Abs(x), 1.0 / n);
                            result = CopySign(result, x);
                        }
                        else
                        {
                            result = NaN;
                        }
                    }
                    else if (IsEvenInteger(n))
                    {
                        result = PositiveInfinity;
                    }
                    else
                    {
                        result = CopySign(PositiveInfinity, x);
                    }
                }
                else if (IsNaN(x))
                {
                    result = NaN;
                }
                else if (x > 0)
                {
                    Debug.Assert(IsPositiveInfinity(x));
                    result = 0.0;
                }
                else
                {
                    Debug.Assert(IsNegativeInfinity(x));
                    result = int.IsOddInteger(n) ? -0.0 : NaN;
                }

                return result;
            }
        }

        /// <inheritdoc cref="IRootFunctions{TSelf}.Sqrt(TSelf)" />
        [Intrinsic]
        public static double Sqrt(double x) => Math.Sqrt(x);

        //
        // ISignedNumber
        //

        /// <inheritdoc cref="ISignedNumber{TSelf}.NegativeOne" />
        static double ISignedNumber<double>.NegativeOne => NegativeOne;

        //
        // ISpanParsable
        //

        /// <inheritdoc cref="ISpanParsable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static double Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider);

        /// <inheritdoc cref="ISpanParsable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out double result) => TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);

        //
        // ISubtractionOperators
        //

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_Subtraction(TSelf, TOther)" />
        static double ISubtractionOperators<double, double, double>.operator -(double left, double right) => left - right;

        //
        // ITrigonometricFunctions
        //

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Acos(TSelf)" />
        [Intrinsic]
        public static double Acos(double x) => Math.Acos(x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AcosPi(TSelf)" />
        public static double AcosPi(double x)
        {
            return Acos(x) / Pi;
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Asin(TSelf)" />
        [Intrinsic]
        public static double Asin(double x) => Math.Asin(x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AsinPi(TSelf)" />
        public static double AsinPi(double x)
        {
            return Asin(x) / Pi;
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Atan(TSelf)" />
        [Intrinsic]
        public static double Atan(double x) => Math.Atan(x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AtanPi(TSelf)" />
        public static double AtanPi(double x)
        {
            return Atan(x) / Pi;
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Cos(TSelf)" />
        [Intrinsic]
        public static double Cos(double x) => Math.Cos(x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.CosPi(TSelf)" />
        public static double CosPi(double x)
        {
            // This code is based on `cospi` from amd/aocl-libm-ose
            // Copyright (C) 2008-2020 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            double result;

            if (IsFinite(x))
            {
                double ax = Abs(x);

                if (ax < 4_503_599_627_370_496.0)           // |x| < 2^52
                {
                    if (ax > 0.25)
                    {
                        long integral = (long)ax;

                        double fractional = ax - integral;
                        double sign = long.IsOddInteger(integral) ? -1.0 : +1.0;

                        if (fractional <= 0.25)
                        {
                            if (fractional != 0.00)
                            {
                                result = sign * CosForIntervalPiBy4(fractional * Pi, 0.0);
                            }
                            else
                            {
                                result = sign;
                            }
                        }
                        else if (fractional <= 0.50)
                        {
                            if (fractional != 0.50)
                            {
                                result = sign * SinForIntervalPiBy4((0.5 - fractional) * Pi, 0.0);
                            }
                            else
                            {
                                result = 0.0;
                            }
                        }
                        else if (fractional <= 0.75)
                        {
                            result = -sign * SinForIntervalPiBy4((fractional - 0.5) * Pi, 0.0);
                        }
                        else
                        {
                            result = -sign * CosForIntervalPiBy4((1.0 - fractional) * Pi, 0.0);
                        }
                    }
                    else if (ax >= 6.103515625E-05)         // |x| >= 2^-14
                    {
                        result = CosForIntervalPiBy4(x * Pi, 0.0);
                    }
                    else if (ax >= 7.450580596923828E-09)   // |x| >= 2^-27
                    {
                        double value = x * Pi;
                        result = 1.0 - (value * value * 0.5);
                    }
                    else
                    {
                        result = 1.0;
                    }
                }
                else if (ax < 9_007_199_254_740_992.0)      // |x| < 2^53
                {
                    // x is an integer
                    long bits = BitConverter.DoubleToInt64Bits(ax);
                    result = long.IsOddInteger(bits) ? -1.0 : +1.0;
                }
                else
                {
                    // x is an even integer
                    result = 1.0;
                }
            }
            else
            {
                result = NaN;
            }

            return result;
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.DegreesToRadians(TSelf)" />
        public static double DegreesToRadians(double degrees)
        {
            // NOTE: Don't change the algorithm without consulting the DIM
            // which elaborates on why this implementation was chosen

            return (degrees * Pi) / 180.0;
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.RadiansToDegrees(TSelf)" />
        public static double RadiansToDegrees(double radians)
        {
            // NOTE: Don't change the algorithm without consulting the DIM
            // which elaborates on why this implementation was chosen

            return (radians * 180.0) / Pi;
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Sin(TSelf)" />
        [Intrinsic]
        public static double Sin(double x) => Math.Sin(x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinCos(TSelf)" />
        public static (double Sin, double Cos) SinCos(double x) => Math.SinCos(x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinCos(TSelf)" />
        public static (double SinPi, double CosPi) SinCosPi(double x)
        {
            // This code is based on `cospi` and `sinpi` from amd/aocl-libm-ose
            // Copyright (C) 2008-2020 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            double sinPi;
            double cosPi;

            if (IsFinite(x))
            {
                double ax = Abs(x);

                if (ax < 4_503_599_627_370_496.0)           // |x| < 2^52
                {
                    if (ax > 0.25)
                    {
                        long integral = (long)ax;

                        double fractional = ax - integral;
                        double sign = long.IsOddInteger(integral) ? -1.0 : +1.0;

                        double sinSign = ((x > 0.0) ? +1.0 : -1.0) * sign;
                        double cosSign = sign;

                        if (fractional <= 0.25)
                        {
                            if (fractional != 0.00)
                            {
                                double value = fractional * Pi;

                                sinPi = sinSign * SinForIntervalPiBy4(value, 0.0);
                                cosPi = cosSign * CosForIntervalPiBy4(value, 0.0);
                            }
                            else
                            {
                                sinPi = x * 0.0;
                                cosPi = cosSign;
                            }
                        }
                        else if (fractional <= 0.50)
                        {
                            if (fractional != 0.50)
                            {
                                double value = (0.5 - fractional) * Pi;

                                sinPi = sinSign * CosForIntervalPiBy4(value, 0.0);
                                cosPi = cosSign * SinForIntervalPiBy4(value, 0.0);
                            }
                            else
                            {
                                sinPi = sinSign;
                                cosPi = 0.0;
                            }
                        }
                        else if (fractional <= 0.75)
                        {
                            double value = (fractional - 0.5) * Pi;

                            sinPi = +sinSign * CosForIntervalPiBy4(value, 0.0);
                            cosPi = -cosSign * SinForIntervalPiBy4(value, 0.0);
                        }
                        else
                        {
                            double value = (1.0 - fractional) * Pi;

                            sinPi = +sinSign * SinForIntervalPiBy4(value, 0.0);
                            cosPi = -cosSign * CosForIntervalPiBy4(value, 0.0);
                        }
                    }
                    else if (ax >= 1.220703125E-4)          // |x| >= 2^-13
                    {
                        double value = x * Pi;

                        sinPi = SinForIntervalPiBy4(value, 0.0);
                        cosPi = CosForIntervalPiBy4(value, 0.0);
                    }
                    else if (ax >= 7.450580596923828E-09)   // |x| >= 2^-27
                    {
                        double value = x * Pi;
                        double valueSq = value * value;

                        sinPi = value - (valueSq * value * (1.0 / 6.0));
                        cosPi = 1.0 - (valueSq * 0.5);
                    }
                    else
                    {
                        sinPi = x * Pi;
                        cosPi = 1.0;
                    }
                }
                else if (ax < 9_007_199_254_740_992.0)      // |x| < 2^53
                {
                    // x is an integer
                    sinPi = x * 0.0;

                    long bits = BitConverter.DoubleToInt64Bits(ax);
                    cosPi = long.IsOddInteger(bits) ? -1.0 : +1.0;
                }
                else
                {
                    // x is an even integer
                    sinPi = x * 0.0;
                    cosPi = 1.0;
                }
            }
            else
            {
                sinPi = NaN;
                cosPi = NaN;
            }

            return (sinPi, cosPi);
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinPi(TSelf)" />
        public static double SinPi(double x)
        {
            // This code is based on `sinpi` from amd/aocl-libm-ose
            // Copyright (C) 2008-2020 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            double result;

            if (IsFinite(x))
            {
                double ax = Abs(x);

                if (ax < 4_503_599_627_370_496.0)           // |x| < 2^52
                {
                    if (ax > 0.25)
                    {
                        long integral = (long)ax;

                        double fractional = ax - integral;
                        double sign = ((x > 0.0) ? +1.0 : -1.0) * (long.IsOddInteger(integral) ? -1.0 : +1.0);

                        if (fractional <= 0.25)
                        {
                            if (fractional != 0.00)
                            {
                                result = sign * SinForIntervalPiBy4(fractional * Pi, 0.0);
                            }
                            else
                            {
                                result = x * 0.0;
                            }
                        }
                        else if (fractional <= 0.50)
                        {
                            if (fractional != 0.50)
                            {
                                result = sign * CosForIntervalPiBy4((0.5 - fractional) * Pi, 0.0);
                            }
                            else
                            {
                                result = sign;
                            }
                        }
                        else if (fractional <= 0.75)
                        {
                            result = sign * CosForIntervalPiBy4((fractional - 0.5) * Pi, 0.0);
                        }
                        else
                        {
                            result = sign * SinForIntervalPiBy4((1.0 - fractional) * Pi, 0.0);
                        }
                    }
                    else if (ax >= 1.220703125E-4)          // |x| >= 2^-13
                    {
                        result = SinForIntervalPiBy4(x * Pi, 0.0);
                    }
                    else if (ax >= 7.450580596923828E-09)   // |x| >= 2^-27
                    {
                        double value = x * Pi;
                        result = value - (value * value * value * (1.0 / 6.0));
                    }
                    else
                    {
                        result = x * Pi;
                    }
                }
                else
                {
                    // x is an integer
                    result = x * 0.0;
                }
            }
            else
            {
                result = NaN;
            }

            return result;
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Tan(TSelf)" />
        [Intrinsic]
        public static double Tan(double x) => Math.Tan(x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.TanPi(TSelf)" />
        public static double TanPi(double x)
        {
            // This code is based on `tanpi` from amd/aocl-libm-ose
            // Copyright (C) 2008-2020 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            double result;

            if (IsFinite(x))
            {
                double ax = Abs(x);
                double sign = (x > 0.0) ? +1.0 : -1.0;

                if (ax < 4_503_599_627_370_496.0)           // |x| < 2^52
                {
                    if (ax > 0.25)
                    {
                        long integral = (long)ax;
                        double fractional = ax - integral;

                        if (fractional <= 0.25)
                        {
                            if (fractional != 0.00)
                            {
                                result = sign * TanForIntervalPiBy4(fractional * Pi, 0.0, isReciprocal: false);
                            }
                            else
                            {
                                result = sign * (long.IsOddInteger(integral) ? -0.0 : +0.0);
                            }
                        }
                        else if (fractional <= 0.50)
                        {
                            if (fractional != 0.50)
                            {
                                result = -sign * TanForIntervalPiBy4((0.5 - fractional) * Pi, 0.0, isReciprocal: true);
                            }
                            else
                            {
                                result = +sign * (long.IsOddInteger(integral) ? NegativeInfinity : PositiveInfinity);
                            }
                        }
                        else if (fractional <= 0.75)
                        {
                            result = +sign * TanForIntervalPiBy4((fractional - 0.5) * Pi, 0.0, isReciprocal: true);
                        }
                        else
                        {
                            result = -sign * TanForIntervalPiBy4((1.0 - fractional) * Pi, 0.0, isReciprocal: false);
                        }
                    }
                    else if (ax >= 6.103515625E-05)         // |x| >= 2^-14
                    {
                        result = TanForIntervalPiBy4(x * Pi, 0.0, isReciprocal: false);
                    }
                    else if (ax >= 7.450580596923828E-09)   // |x| >= 2^-27
                    {
                        double value = x * Pi;
                        result = value + (value * value * value * (1.0 / 3.0));
                    }
                    else
                    {
                        result = x * Pi;
                    }
                }
                else if (ax < 9_007_199_254_740_992.0)      // |x| < 2^53
                {
                    // x is an integer
                    long bits = BitConverter.DoubleToInt64Bits(ax);
                    result = sign * (long.IsOddInteger(bits) ? -0.0 : +0.0);
                }
                else
                {
                    // x is an even integer
                    result = sign * 0.0;
                }
            }
            else
            {
                result = NaN;
            }

            return result;
        }

        //
        // IUnaryNegationOperators
        //

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_UnaryNegation(TSelf)" />
        static double IUnaryNegationOperators<double, double>.operator -(double value) => -value;

        //
        // IUnaryPlusOperators
        //

        /// <inheritdoc cref="IUnaryPlusOperators{TSelf, TResult}.op_UnaryPlus(TSelf)" />
        static double IUnaryPlusOperators<double, double>.operator +(double value) => (double)(+value);

        //
        // IUtf8SpanParsable
        //

        /// <inheritdoc cref="INumberBase{TSelf}.Parse(ReadOnlySpan{byte}, NumberStyles, IFormatProvider?)" />
        public static double Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.ParseFloat<byte, double>(utf8Text, style, NumberFormatInfo.GetInstance(provider));
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryParse(ReadOnlySpan{byte}, NumberStyles, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out double result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.TryParseFloat(utf8Text, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        /// <inheritdoc cref="IUtf8SpanParsable{TSelf}.Parse(ReadOnlySpan{byte}, IFormatProvider?)" />
        public static double Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) => Parse(utf8Text, NumberStyles.Float | NumberStyles.AllowThousands, provider);

        /// <inheritdoc cref="IUtf8SpanParsable{TSelf}.TryParse(ReadOnlySpan{byte}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out double result) => TryParse(utf8Text, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);

        //
        // IBinaryFloatParseAndFormatInfo
        //

        static int IBinaryFloatParseAndFormatInfo<double>.NumberBufferLength => Number.DoubleNumberBufferLength;

        static ulong IBinaryFloatParseAndFormatInfo<double>.ZeroBits => 0;
        static ulong IBinaryFloatParseAndFormatInfo<double>.InfinityBits => 0x7FF00000_00000000;

        static ulong IBinaryFloatParseAndFormatInfo<double>.NormalMantissaMask => (1UL << SignificandLength) - 1;
        static ulong IBinaryFloatParseAndFormatInfo<double>.DenormalMantissaMask => TrailingSignificandMask;

        static int IBinaryFloatParseAndFormatInfo<double>.MinBinaryExponent => 1 - MaxExponent;
        static int IBinaryFloatParseAndFormatInfo<double>.MaxBinaryExponent => MaxExponent;

        static int IBinaryFloatParseAndFormatInfo<double>.MinDecimalExponent => -324;
        static int IBinaryFloatParseAndFormatInfo<double>.MaxDecimalExponent => 309;

        static int IBinaryFloatParseAndFormatInfo<double>.ExponentBias => ExponentBias;
        static ushort IBinaryFloatParseAndFormatInfo<double>.ExponentBits => 11;

        static int IBinaryFloatParseAndFormatInfo<double>.OverflowDecimalExponent => (MaxExponent + (2 * SignificandLength)) / 3;
        static int IBinaryFloatParseAndFormatInfo<double>.InfinityExponent => 0x7FF;

        static ushort IBinaryFloatParseAndFormatInfo<double>.NormalMantissaBits => SignificandLength;
        static ushort IBinaryFloatParseAndFormatInfo<double>.DenormalMantissaBits => TrailingSignificandLength;

        static int IBinaryFloatParseAndFormatInfo<double>.MinFastFloatDecimalExponent => -324;
        static int IBinaryFloatParseAndFormatInfo<double>.MaxFastFloatDecimalExponent => 308;

        static int IBinaryFloatParseAndFormatInfo<double>.MinExponentRoundToEven => -4;
        static int IBinaryFloatParseAndFormatInfo<double>.MaxExponentRoundToEven => 23;

        static int IBinaryFloatParseAndFormatInfo<double>.MaxExponentFastPath => 22;
        static ulong IBinaryFloatParseAndFormatInfo<double>.MaxMantissaFastPath => 2UL << TrailingSignificandLength;

        static double IBinaryFloatParseAndFormatInfo<double>.BitsToFloat(ulong bits) => BitConverter.UInt64BitsToDouble(bits);

        static ulong IBinaryFloatParseAndFormatInfo<double>.FloatToBits(double value) => BitConverter.DoubleToUInt64Bits(value);

        //
        // Helpers
        //

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double CosForIntervalPiBy4(double x, double xTail)
        {
            // This code is based on `cos_piby4` from amd/aocl-libm-ose
            // Copyright (C) 2008-2020 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Taylor series for cos(x) is: 1 - (x^2 / 2!) + (x^4 / 4!) - (x^6 / 6!) ...
            //
            // Then define f(xx) where xx = (x * x)
            // and f(xx) = 1 - (xx / 2!) + (xx^2 / 4!) - (xx^3 / 6!) ...
            //
            // We use a minimax approximation of (f(xx) - 1 + (xx / 2)) / (xx * xx)
            // because this produces an expansion in even powers of x.
            //
            // If xTail is non-zero, we subtract a correction term g(x, xTail) = (x * xTail)
            // to the result, where g(x, xTail) is an approximation to sin(x) * sin(xTail)
            //
            // This is valid because xTail is tiny relative to x.

            const double C1 = +0.41666666666666665390037E-1;        // approx: +1 / 4!
            const double C2 = -0.13888888888887398280412E-2;        // approx: -1 / 6!
            const double C3 = +0.248015872987670414957399E-4;       // approx: +1 / 8!
            const double C4 = -0.275573172723441909470836E-6;       // approx: -1 / 10!
            const double C5 = +0.208761463822329611076335E-8;       // approx: +1 / 12!
            const double C6 = -0.113826398067944859590880E-10;      // approx: -1 / 14!

            double xx = x * x;

            double tmp1 = 0.5 * xx;
            double tmp2 = 1.0 - tmp1;

            double result = C6;

            result = (result * xx) + C5;
            result = (result * xx) + C4;
            result = (result * xx) + C3;
            result = (result * xx) + C2;
            result = (result * xx) + C1;

            result *= (xx * xx);
            result += 1.0 - tmp2 - tmp1 - (x * xTail);
            result += tmp2;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double SinForIntervalPiBy4(double x, double xTail)
        {
            // This code is based on `sin_piby4` from amd/aocl-libm-ose
            // Copyright (C) 2008-2020 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Taylor series for sin(x) is x - (x^3 / 3!) + (x^5 / 5!) - (x^7 / 7!) ...
            // Which can be expressed as x * (1 - (x^2 / 3!) + (x^4 /5!) - (x^6 /7!) ...)
            //
            // Then define f(xx) where xx = (x * x)
            // and f(xx) = 1 - (xx / 3!) + (xx^2 / 5!) - (xx^3 / 7!) ...
            //
            // We use a minimax approximation of (f(xx) - 1) / xx
            // because this produces an expansion in even powers of x.
            //
            // If xTail is non-zero, we add a correction term g(x, xTail) = (1 - xx / 2) * xTail
            // to the result, where g(x, xTail) is an approximation to cos(x) * sin(xTail)
            //
            // This is valid because xTail is tiny relative to x.

            const double C1 = -0.166666666666666646259241729;       // approx: -1 / 3!
            const double C2 = +0.833333333333095043065222816E-2;    // approx: +1 / 5!
            const double C3 = -0.19841269836761125688538679E-3;     // approx: -1 / 7!
            const double C4 = +0.275573161037288022676895908448E-5; // approx: +1 / 9!
            const double C5 = -0.25051132068021699772257377197E-7;  // approx: -1 / 11!
            const double C6 = +0.159181443044859136852668200E-9;    // approx: +1 / 13!

            double xx = x * x;
            double xxx = xx * x;

            double result = C6;

            result = (result * xx) + C5;
            result = (result * xx) + C4;
            result = (result * xx) + C3;
            result = (result * xx) + C2;

            if (xTail == 0.0)
            {
                result = (xx * result) + C1;
                result = (xxx * result) + x;
            }
            else
            {
                result = x - ((xx * ((0.5 * xTail) - (xxx * result))) - xTail - (xxx * C1));
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double TanForIntervalPiBy4(double x, double xTail, bool isReciprocal)
        {
            // This code is based on `tan_piby4` from amd/aocl-libm-ose
            // Copyright (C) 2008-2020 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // In order to maintain relative precision transform using the identity:
            //  tan((pi / 4) - x) = (1 - tan(x)) / (1 + tan(x)) for arguments close to (pi / 4).
            //
            // Similarly use tan(x - (pi / 4)) = (tan(x) - 1) / (tan(x) + 1) close to (-pi / 4).

            const double PiBy4Head = 7.85398163397448278999E-01;
            const double PiBy4Tail = 3.06161699786838240164E-17;

            int transform = 0;

            if (x > +0.68)
            {
                transform = 1;
                x = (PiBy4Head - x) + (PiBy4Tail - xTail);
                xTail = 0.0;
            }
            else if (x < -0.68)
            {
                transform = -1;
                x = (PiBy4Head + x) + (PiBy4Tail + xTail);
                xTail = 0.0;
            }

            // Core Remez [2, 3] approximation to tan(x + xTail) on the interval [0, 0.68].

            double tmp1 = (x * x) + (2.0 * x * xTail);

            double denominator = -0.232371494088563558304549252913E-3;
            denominator = +0.260656620398645407524064091208E-1 + (denominator * tmp1);
            denominator = -0.515658515729031149329237816945E+0 + (denominator * tmp1);
            denominator = +0.111713747927937668539901657944E+1 + (denominator * tmp1);

            double numerator = +0.224044448537022097264602535574E-3;
            numerator = -0.229345080057565662883358588111E-1 + (numerator * tmp1);
            numerator = +0.372379159759792203640806338901E+0 + (numerator * tmp1);

            double tmp2 = x * tmp1;
            tmp2 *= numerator / denominator;
            tmp2 += xTail;

            // Reconstruct tan(x) in the transformed case

            double result = x + tmp2;

            if (transform != 0)
            {
                if (isReciprocal)
                {
                    result = (transform * (2 * result / (result - 1))) - 1.0;
                }
                else
                {
                    result = transform * (1.0 - (2 * result / (1 + result)));
                }
            }
            else if (isReciprocal)
            {
                // Compute -1.0 / (x + tmp2) accurately

                ulong bits = BitConverter.DoubleToUInt64Bits(result);
                bits &= 0xFFFFFFFF00000000;

                double z1 = BitConverter.UInt64BitsToDouble(bits);
                double z2 = tmp2 - (z1 - x);

                double reciprocal = -1.0 / result;

                bits = BitConverter.DoubleToUInt64Bits(reciprocal);
                bits &= 0xFFFFFFFF00000000;

                double reciprocalHead = BitConverter.UInt64BitsToDouble(bits);
                result = reciprocalHead + (reciprocal * (1.0 + (reciprocalHead * z1) + (reciprocalHead * z2)));
            }

            return result;
        }
    }
}
