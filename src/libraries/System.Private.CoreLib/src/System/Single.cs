// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
**
** Purpose: A wrapper class for the primitive type float.
**
**
===========================================================*/

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
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public readonly struct Single
        : IComparable,
          IConvertible,
          ISpanFormattable,
          IComparable<float>,
          IEquatable<float>,
          IBinaryFloatingPointIeee754<float>,
          IMinMaxValue<float>,
          IUtf8SpanFormattable
    {
        private readonly float m_value; // Do not rename (binary serialization)

        //
        // Public constants
        //
        public const float MinValue = (float)-3.40282346638528859e+38;
        public const float MaxValue = (float)3.40282346638528859e+38;

        // Note Epsilon should be a float whose hex representation is 0x1
        // on little endian machines.
        public const float Epsilon = (float)1.4e-45;
        public const float NegativeInfinity = (float)-1.0 / (float)0.0;
        public const float PositiveInfinity = (float)1.0 / (float)0.0;
        public const float NaN = (float)0.0 / (float)0.0;

        /// <summary>Represents the additive identity (0).</summary>
        internal const float AdditiveIdentity = 0.0f;

        /// <summary>Represents the multiplicative identity (1).</summary>
        internal const float MultiplicativeIdentity = 1.0f;

        /// <summary>Represents the number one (1).</summary>
        internal const float One = 1.0f;

        /// <summary>Represents the number zero (0).</summary>
        internal const float Zero = 0.0f;

        /// <summary>Represents the number negative one (-1).</summary>
        internal const float NegativeOne = -1.0f;

        /// <summary>Represents the number negative zero (-0).</summary>
        public const float NegativeZero = -0.0f;

        /// <summary>Represents the natural logarithmic base, specified by the constant, e.</summary>
        /// <remarks>This is known as Euler's number and is approximately 2.7182818284590452354.</remarks>
        public const float E = MathF.E;

        /// <summary>Represents the ratio of the circumference of a circle to its diameter, specified by the constant, PI.</summary>
        /// <remarks>Pi is approximately 3.1415926535897932385.</remarks>
        public const float Pi = MathF.PI;

        /// <summary>Represents the number of radians in one turn, specified by the constant, Tau.</summary>
        /// <remarks>Tau is approximately 6.2831853071795864769.</remarks>
        public const float Tau = MathF.Tau;

        //
        // Constants for manipulating the private bit-representation
        //

        internal const uint SignMask = 0x8000_0000;
        internal const int SignShift = 31;
        internal const byte ShiftedSignMask = (byte)(SignMask >> SignShift);

        internal const uint BiasedExponentMask = 0x7F80_0000;
        internal const int BiasedExponentShift = 23;
        internal const byte ShiftedBiasedExponentMask = (byte)(BiasedExponentMask >> BiasedExponentShift);

        internal const uint TrailingSignificandMask = 0x007F_FFFF;

        internal const byte MinSign = 0;
        internal const byte MaxSign = 1;

        internal const byte MinBiasedExponent = 0x00;
        internal const byte MaxBiasedExponent = 0xFF;

        internal const byte ExponentBias = 127;

        internal const sbyte MinExponent = -126;
        internal const sbyte MaxExponent = +127;

        internal const uint MinTrailingSignificand = 0x0000_0000;
        internal const uint MaxTrailingSignificand = 0x007F_FFFF;

        internal byte BiasedExponent
        {
            get
            {
                uint bits = BitConverter.SingleToUInt32Bits(m_value);
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

        internal uint Significand
        {
            get
            {
                return TrailingSignificand | ((BiasedExponent != 0) ? (1U << BiasedExponentShift) : 0U);
            }
        }

        internal uint TrailingSignificand
        {
            get
            {
                uint bits = BitConverter.SingleToUInt32Bits(m_value);
                return ExtractTrailingSignificandFromBits(bits);
            }
        }

        internal static byte ExtractBiasedExponentFromBits(uint bits)
        {
            return (byte)((bits >> BiasedExponentShift) & ShiftedBiasedExponentMask);
        }

        internal static uint ExtractTrailingSignificandFromBits(uint bits)
        {
            return bits & TrailingSignificandMask;
        }

        /// <summary>Determines whether the specified value is finite (zero, subnormal, or normal).</summary>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFinite(float f)
        {
            int bits = BitConverter.SingleToInt32Bits(f);
            return (bits & 0x7FFFFFFF) < 0x7F800000;
        }

        /// <summary>Determines whether the specified value is infinite.</summary>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool IsInfinity(float f)
        {
            int bits = BitConverter.SingleToInt32Bits(f);
            return (bits & 0x7FFFFFFF) == 0x7F800000;
        }

        /// <summary>Determines whether the specified value is NaN.</summary>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool IsNaN(float f)
        {
            // A NaN will never equal itself so this is an
            // easy and efficient way to check for NaN.

#pragma warning disable CS1718
            return f != f;
#pragma warning restore CS1718
        }

        /// <summary>Determines whether the specified value is negative.</summary>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool IsNegative(float f)
        {
            return BitConverter.SingleToInt32Bits(f) < 0;
        }

        /// <summary>Determines whether the specified value is negative infinity.</summary>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool IsNegativeInfinity(float f)
        {
            return f == float.NegativeInfinity;
        }

        /// <summary>Determines whether the specified value is normal.</summary>
        [NonVersionable]
        // This is probably not worth inlining, it has branches and should be rarely called
        public static unsafe bool IsNormal(float f)
        {
            int bits = BitConverter.SingleToInt32Bits(f);
            bits &= 0x7FFFFFFF;
            return (bits < 0x7F800000) && (bits != 0) && ((bits & 0x7F800000) != 0);
        }

        /// <summary>Determines whether the specified value is positive infinity.</summary>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool IsPositiveInfinity(float f)
        {
            return f == float.PositiveInfinity;
        }

        /// <summary>Determines whether the specified value is subnormal.</summary>
        [NonVersionable]
        // This is probably not worth inlining, it has branches and should be rarely called
        public static unsafe bool IsSubnormal(float f)
        {
            int bits = BitConverter.SingleToInt32Bits(f);
            bits &= 0x7FFFFFFF;
            return (bits < 0x7F800000) && (bits != 0) && ((bits & 0x7F800000) == 0);
        }

        // Compares this object to another object, returning an integer that
        // indicates the relationship.
        // Returns a value less than zero if this  object
        // null is considered to be less than any instance.
        // If object is not of type Single, this method throws an ArgumentException.
        //
        public int CompareTo(object? value)
        {
            if (value == null)
            {
                return 1;
            }

            if (value is float f)
            {
                if (m_value < f) return -1;
                if (m_value > f) return 1;
                if (m_value == f) return 0;

                // At least one of the values is NaN.
                if (IsNaN(m_value))
                    return IsNaN(f) ? 0 : -1;
                else // f is NaN.
                    return 1;
            }

            throw new ArgumentException(SR.Arg_MustBeSingle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(float value)
        {
            if (m_value < value) return -1;
            if (m_value > value) return 1;
            if (m_value == value) return 0;

            // At least one of the values is NaN.
            if (IsNaN(m_value))
                return IsNaN(value) ? 0 : -1;
            else // f is NaN.
                return 1;
        }

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther, TResult}.op_Equality(TSelf, TOther)" />
        [NonVersionable]
        public static bool operator ==(float left, float right) => left == right;

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther, TResult}.op_Inequality(TSelf, TOther)" />
        [NonVersionable]
        public static bool operator !=(float left, float right) => left != right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_LessThan(TSelf, TOther)" />
        [NonVersionable]
        public static bool operator <(float left, float right) => left < right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_GreaterThan(TSelf, TOther)" />
        [NonVersionable]
        public static bool operator >(float left, float right) => left > right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_LessThanOrEqual(TSelf, TOther)" />
        [NonVersionable]
        public static bool operator <=(float left, float right) => left <= right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_GreaterThanOrEqual(TSelf, TOther)" />
        [NonVersionable]
        public static bool operator >=(float left, float right) => left >= right;

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (!(obj is float))
            {
                return false;
            }
            float temp = ((float)obj).m_value;
            if (temp == m_value)
            {
                return true;
            }

            return IsNaN(temp) && IsNaN(m_value);
        }

        public bool Equals(float obj)
        {
            if (obj == m_value)
            {
                return true;
            }

            return IsNaN(obj) && IsNaN(m_value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            int bits = Unsafe.As<float, int>(ref Unsafe.AsRef(in m_value));

            // Optimized check for IsNan() || IsZero()
            if (((bits - 1) & 0x7FFFFFFF) >= 0x7F800000)
            {
                // Ensure that all NaNs and both zeros have the same hash code
                bits &= 0x7F800000;
            }

            return bits;
        }

        public override string ToString()
        {
            return Number.FormatSingle(m_value, null, NumberFormatInfo.CurrentInfo);
        }

        public string ToString(IFormatProvider? provider)
        {
            return Number.FormatSingle(m_value, null, NumberFormatInfo.GetInstance(provider));
        }

        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
        {
            return Number.FormatSingle(m_value, format, NumberFormatInfo.CurrentInfo);
        }

        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider)
        {
            return Number.FormatSingle(m_value, format, NumberFormatInfo.GetInstance(provider));
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatSingle(m_value, format, NumberFormatInfo.GetInstance(provider), destination, out charsWritten);
        }

        /// <inheritdoc cref="IUtf8SpanFormattable.TryFormat" />
        public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatSingle(m_value, format, NumberFormatInfo.GetInstance(provider), utf8Destination, out bytesWritten);
        }

        // Parses a float from a String in the given style.  If
        // a NumberFormatInfo isn't specified, the current culture's
        // NumberFormatInfo is assumed.
        //
        // This method will not throw an OverflowException, but will return
        // PositiveInfinity or NegativeInfinity for a number that is too
        // large or too small.
        //
        public static float Parse(string s)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseSingle(s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.CurrentInfo);
        }

        public static float Parse(string s, NumberStyles style)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseSingle(s, style, NumberFormatInfo.CurrentInfo);
        }

        public static float Parse(string s, IFormatProvider? provider)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseSingle(s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.GetInstance(provider));
        }

        public static float Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseSingle(s, style, NumberFormatInfo.GetInstance(provider));
        }

        public static float Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Number.ParseSingle(s, style, NumberFormatInfo.GetInstance(provider));
        }

        public static bool TryParse([NotNullWhen(true)] string? s, out float result)
        {
            if (s == null)
            {
                result = 0;
                return false;
            }

            return TryParse((ReadOnlySpan<char>)s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse(ReadOnlySpan<char> s, out float result)
        {
            return TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out float result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);

            if (s == null)
            {
                result = 0;
                return false;
            }

            return TryParse((ReadOnlySpan<char>)s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out float result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return TryParse(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        private static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info, out float result)
        {
            return Number.TryParseSingle(s, style, info, out result);
        }

        //
        // IConvertible implementation
        //

        public TypeCode GetTypeCode()
        {
            return TypeCode.Single;
        }

        bool IConvertible.ToBoolean(IFormatProvider? provider)
        {
            return Convert.ToBoolean(m_value);
        }

        char IConvertible.ToChar(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Single", "Char"));
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
            return m_value;
        }

        double IConvertible.ToDouble(IFormatProvider? provider)
        {
            return Convert.ToDouble(m_value);
        }

        decimal IConvertible.ToDecimal(IFormatProvider? provider)
        {
            return Convert.ToDecimal(m_value);
        }

        DateTime IConvertible.ToDateTime(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Single", "DateTime"));
        }

        object IConvertible.ToType(Type type, IFormatProvider? provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }

        //
        // IAdditionOperators
        //

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        static float IAdditionOperators<float, float, float>.operator +(float left, float right) => left + right;

        //
        // IAdditiveIdentity
        //

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static float IAdditiveIdentity<float, float>.AdditiveIdentity => AdditiveIdentity;

        //
        // IBinaryNumber
        //

        /// <inheritdoc cref="IBinaryNumber{TSelf}.AllBitsSet" />
        static float IBinaryNumber<float>.AllBitsSet => BitConverter.UInt32BitsToSingle(0xFFFF_FFFF);

        /// <inheritdoc cref="IBinaryNumber{TSelf}.IsPow2(TSelf)" />
        public static bool IsPow2(float value)
        {
            uint bits = BitConverter.SingleToUInt32Bits(value);

            byte biasedExponent = ExtractBiasedExponentFromBits(bits);
            uint trailingSignificand = ExtractTrailingSignificandFromBits(bits);

            return (value > 0)
                && (biasedExponent != MinBiasedExponent) && (biasedExponent != MaxBiasedExponent)
                && (trailingSignificand == MinTrailingSignificand);
        }

        /// <inheritdoc cref="IBinaryNumber{TSelf}.Log2(TSelf)" />
        public static float Log2(float value) => MathF.Log2(value);

        //
        // IBitwiseOperators
        //

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseAnd(TSelf, TOther)" />
        static float IBitwiseOperators<float, float, float>.operator &(float left, float right)
        {
            uint bits = BitConverter.SingleToUInt32Bits(left) & BitConverter.SingleToUInt32Bits(right);
            return BitConverter.UInt32BitsToSingle(bits);
        }

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseOr(TSelf, TOther)" />
        static float IBitwiseOperators<float, float, float>.operator |(float left, float right)
        {
            uint bits = BitConverter.SingleToUInt32Bits(left) | BitConverter.SingleToUInt32Bits(right);
            return BitConverter.UInt32BitsToSingle(bits);
        }

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_ExclusiveOr(TSelf, TOther)" />
        static float IBitwiseOperators<float, float, float>.operator ^(float left, float right)
        {
            uint bits = BitConverter.SingleToUInt32Bits(left) ^ BitConverter.SingleToUInt32Bits(right);
            return BitConverter.UInt32BitsToSingle(bits);
        }

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_OnesComplement(TSelf)" />
        static float IBitwiseOperators<float, float, float>.operator ~(float value)
        {
            uint bits = ~BitConverter.SingleToUInt32Bits(value);
            return BitConverter.UInt32BitsToSingle(bits);
        }

        //
        // IDecrementOperators
        //

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        static float IDecrementOperators<float>.operator --(float value) => --value;

        //
        // IDivisionOperators
        //

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_Division(TSelf, TOther)" />
        static float IDivisionOperators<float, float, float>.operator /(float left, float right) => left / right;

        //
        // IExponentialFunctions
        //

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp" />
        public static float Exp(float x) => MathF.Exp(x);

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.ExpM1(TSelf)" />
        public static float ExpM1(float x) => MathF.Exp(x) - 1;

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp2(TSelf)" />
        public static float Exp2(float x) => MathF.Pow(2, x);

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp2M1(TSelf)" />
        public static float Exp2M1(float x) => MathF.Pow(2, x) - 1;

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp10(TSelf)" />
        public static float Exp10(float x) => MathF.Pow(10, x);

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp10M1(TSelf)" />
        public static float Exp10M1(float x) => MathF.Pow(10, x) - 1;

        //
        // IFloatingPoint
        //

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Ceiling(TSelf)" />
        public static float Ceiling(float x) => MathF.Ceiling(x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Floor(TSelf)" />
        public static float Floor(float x) => MathF.Floor(x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf)" />
        public static float Round(float x) => MathF.Round(x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, int)" />
        public static float Round(float x, int digits) => MathF.Round(x, digits);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, MidpointRounding)" />
        public static float Round(float x, MidpointRounding mode) => MathF.Round(x, mode);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, int, MidpointRounding)" />
        public static float Round(float x, int digits, MidpointRounding mode) => MathF.Round(x, digits, mode);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Truncate(TSelf)" />
        public static float Truncate(float x) => MathF.Truncate(x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetExponentByteCount()" />
        int IFloatingPoint<float>.GetExponentByteCount() => sizeof(sbyte);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetExponentShortestBitLength()" />
        int IFloatingPoint<float>.GetExponentShortestBitLength()
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
        int IFloatingPoint<float>.GetSignificandByteCount() => sizeof(uint);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetSignificandBitLength()" />
        int IFloatingPoint<float>.GetSignificandBitLength() => 24;

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteExponentBigEndian(Span{byte}, out int)" />
        bool IFloatingPoint<float>.TryWriteExponentBigEndian(Span<byte> destination, out int bytesWritten)
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
        bool IFloatingPoint<float>.TryWriteExponentLittleEndian(Span<byte> destination, out int bytesWritten)
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
        bool IFloatingPoint<float>.TryWriteSignificandBigEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(uint))
            {
                uint significand = Significand;

                if (BitConverter.IsLittleEndian)
                {
                    significand = BinaryPrimitives.ReverseEndianness(significand);
                }

                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), significand);

                bytesWritten = sizeof(uint);
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteSignificandLittleEndian(Span{byte}, out int)" />
        bool IFloatingPoint<float>.TryWriteSignificandLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(uint))
            {
                uint significand = Significand;

                if (!BitConverter.IsLittleEndian)
                {
                    significand = BinaryPrimitives.ReverseEndianness(significand);
                }

                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), significand);

                bytesWritten = sizeof(uint);
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
        static float IFloatingPointConstants<float>.E => E;

        /// <inheritdoc cref="IFloatingPointConstants{TSelf}.Pi" />
        static float IFloatingPointConstants<float>.Pi => Pi;

        /// <inheritdoc cref="IFloatingPointConstants{TSelf}.Tau" />
        static float IFloatingPointConstants<float>.Tau => Tau;

        //
        // IFloatingPointIeee754
        //

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Epsilon" />
        static float IFloatingPointIeee754<float>.Epsilon => Epsilon;

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.NaN" />
        static float IFloatingPointIeee754<float>.NaN => NaN;

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.NegativeInfinity" />
        static float IFloatingPointIeee754<float>.NegativeInfinity => NegativeInfinity;

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.NegativeZero" />
        static float IFloatingPointIeee754<float>.NegativeZero => NegativeZero;

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.PositiveInfinity" />
        static float IFloatingPointIeee754<float>.PositiveInfinity => PositiveInfinity;

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Atan2(TSelf, TSelf)" />
        public static float Atan2(float y, float x) => MathF.Atan2(y, x);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Atan2Pi(TSelf, TSelf)" />
        public static float Atan2Pi(float y, float x) => Atan2(y, x) / Pi;

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.BitDecrement(TSelf)" />
        public static float BitDecrement(float x) => MathF.BitDecrement(x);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.BitIncrement(TSelf)" />
        public static float BitIncrement(float x) => MathF.BitIncrement(x);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.FusedMultiplyAdd(TSelf, TSelf, TSelf)" />
        public static float FusedMultiplyAdd(float left, float right, float addend) => MathF.FusedMultiplyAdd(left, right, addend);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Ieee754Remainder(TSelf, TSelf)" />
        public static float Ieee754Remainder(float left, float right) => MathF.IEEERemainder(left, right);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ILogB(TSelf)" />
        public static int ILogB(float x) => MathF.ILogB(x);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Lerp(TSelf, TSelf, TSelf)" />
        public static float Lerp(float value1, float value2, float amount) => (value1 * (1.0f - amount)) + (value2 * amount);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ReciprocalEstimate(TSelf)" />
        public static float ReciprocalEstimate(float x) => MathF.ReciprocalEstimate(x);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ReciprocalSqrtEstimate(TSelf)" />
        public static float ReciprocalSqrtEstimate(float x) => MathF.ReciprocalSqrtEstimate(x);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ScaleB(TSelf, int)" />
        public static float ScaleB(float x, int n) => MathF.ScaleB(x, n);

        // /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Compound(TSelf, TSelf)" />
        // public static float Compound(float x, float n) => MathF.Compound(x, n);

        //
        // IHyperbolicFunctions
        //

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Acosh(TSelf)" />
        public static float Acosh(float x) => MathF.Acosh(x);

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Asinh(TSelf)" />
        public static float Asinh(float x) => MathF.Asinh(x);

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Atanh(TSelf)" />
        public static float Atanh(float x) => MathF.Atanh(x);

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Cosh(TSelf)" />
        public static float Cosh(float x) => MathF.Cosh(x);

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Sinh(TSelf)" />
        public static float Sinh(float x) => MathF.Sinh(x);

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Tanh(TSelf)" />
        public static float Tanh(float x) => MathF.Tanh(x);

        //
        // IIncrementOperators
        //

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_Increment(TSelf)" />
        static float IIncrementOperators<float>.operator ++(float value) => ++value;

        //
        // ILogarithmicFunctions
        //

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log(TSelf)" />
        public static float Log(float x) => MathF.Log(x);

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log(TSelf, TSelf)" />
        public static float Log(float x, float newBase) => MathF.Log(x, newBase);

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.LogP1(TSelf)" />
        public static float LogP1(float x) => MathF.Log(x + 1);

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log10(TSelf)" />
        public static float Log10(float x) => MathF.Log10(x);

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log2P1(TSelf)" />
        public static float Log2P1(float x) => MathF.Log2(x + 1);

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log10P1(TSelf)" />
        public static float Log10P1(float x) => MathF.Log10(x + 1);

        //
        // IMinMaxValue
        //

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MinValue" />
        static float IMinMaxValue<float>.MinValue => MinValue;

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MaxValue" />
        static float IMinMaxValue<float>.MaxValue => MaxValue;

        //
        // IModulusOperators
        //

        /// <inheritdoc cref="IModulusOperators{TSelf, TOther, TResult}.op_Modulus(TSelf, TOther)" />
        static float IModulusOperators<float, float, float>.operator %(float left, float right) => left % right;

        //
        // IMultiplicativeIdentity
        //

        /// <inheritdoc cref="IMultiplicativeIdentity{TSelf, TResult}.MultiplicativeIdentity" />
        static float IMultiplicativeIdentity<float, float>.MultiplicativeIdentity => MultiplicativeIdentity;

        //
        // IMultiplyOperators
        //

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_Multiply(TSelf, TOther)" />
        static float IMultiplyOperators<float, float, float>.operator *(float left, float right) => left * right;

        //
        // INumber
        //

        /// <inheritdoc cref="INumber{TSelf}.Clamp(TSelf, TSelf, TSelf)" />
        public static float Clamp(float value, float min, float max) => Math.Clamp(value, min, max);

        /// <inheritdoc cref="INumber{TSelf}.CopySign(TSelf, TSelf)" />
        public static float CopySign(float value, float sign) => MathF.CopySign(value, sign);

        /// <inheritdoc cref="INumber{TSelf}.Max(TSelf, TSelf)" />
        public static float Max(float x, float y) => MathF.Max(x, y);

        /// <inheritdoc cref="INumber{TSelf}.MaxNumber(TSelf, TSelf)" />
        public static float MaxNumber(float x, float y)
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
        public static float Min(float x, float y) => MathF.Min(x, y);

        /// <inheritdoc cref="INumber{TSelf}.MinNumber(TSelf, TSelf)" />
        public static float MinNumber(float x, float y)
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
        public static int Sign(float value) => MathF.Sign(value);

        //
        // INumberBase
        //

        /// <inheritdoc cref="INumberBase{TSelf}.One" />
        static float INumberBase<float>.One => One;

        /// <inheritdoc cref="INumberBase{TSelf}.Radix" />
        static int INumberBase<float>.Radix => 2;

        /// <inheritdoc cref="INumberBase{TSelf}.Zero" />
        static float INumberBase<float>.Zero => Zero;

        /// <inheritdoc cref="INumberBase{TSelf}.Abs(TSelf)" />
        public static float Abs(float value) => MathF.Abs(value);

        /// <inheritdoc cref="INumberBase{TSelf}.CreateChecked{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CreateChecked<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            float result;

            if (typeof(TOther) == typeof(float))
            {
                result = (float)(object)value;
            }
            else if (!TryConvertFrom(value, out result) && !TOther.TryConvertToChecked(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateSaturating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CreateSaturating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            float result;

            if (typeof(TOther) == typeof(float))
            {
                result = (float)(object)value;
            }
            else if (!TryConvertFrom(value, out result) && !TOther.TryConvertToSaturating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateTruncating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CreateTruncating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            float result;

            if (typeof(TOther) == typeof(float))
            {
                result = (float)(object)value;
            }
            else if (!TryConvertFrom(value, out result) && !TOther.TryConvertToTruncating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsCanonical(TSelf)" />
        static bool INumberBase<float>.IsCanonical(float value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsComplexNumber(TSelf)" />
        static bool INumberBase<float>.IsComplexNumber(float value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsEvenInteger(TSelf)" />
        public static bool IsEvenInteger(float value) => IsInteger(value) && (Abs(value % 2) == 0);

        /// <inheritdoc cref="INumberBase{TSelf}.IsImaginaryNumber(TSelf)" />
        static bool INumberBase<float>.IsImaginaryNumber(float value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsInteger(TSelf)" />
        public static bool IsInteger(float value) => IsFinite(value) && (value == Truncate(value));

        /// <inheritdoc cref="INumberBase{TSelf}.IsOddInteger(TSelf)" />
        public static bool IsOddInteger(float value) => IsInteger(value) && (Abs((value) % 2) == 1);

        /// <inheritdoc cref="INumberBase{TSelf}.IsPositive(TSelf)" />
        public static bool IsPositive(float value) => BitConverter.SingleToInt32Bits(value) >= 0;

        /// <inheritdoc cref="INumberBase{TSelf}.IsRealNumber(TSelf)" />
        public static bool IsRealNumber(float value)
        {
            // A NaN will never equal itself so this is an
            // easy and efficient way to check for a real number.

#pragma warning disable CS1718
            return value == value;
#pragma warning restore CS1718
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsZero(TSelf)" />
        static bool INumberBase<float>.IsZero(float value) => (value == 0);

        /// <inheritdoc cref="INumberBase{TSelf}.MaxMagnitude(TSelf, TSelf)" />
        public static float MaxMagnitude(float x, float y) => MathF.MaxMagnitude(x, y);

        /// <inheritdoc cref="INumberBase{TSelf}.MaxMagnitudeNumber(TSelf, TSelf)" />
        public static float MaxMagnitudeNumber(float x, float y)
        {
            // This matches the IEEE 754:2019 `maximumMagnitudeNumber` function
            //
            // It does not propagate NaN inputs back to the caller and
            // otherwise returns the input with a larger magnitude.
            // It treats +0 as larger than -0 as per the specification.

            float ax = Abs(x);
            float ay = Abs(y);

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
        public static float MinMagnitude(float x, float y) => MathF.MinMagnitude(x, y);

        /// <inheritdoc cref="INumberBase{TSelf}.MinMagnitudeNumber(TSelf, TSelf)" />
        public static float MinMagnitudeNumber(float x, float y)
        {
            // This matches the IEEE 754:2019 `minimumMagnitudeNumber` function
            //
            // It does not propagate NaN inputs back to the caller and
            // otherwise returns the input with a larger magnitude.
            // It treats +0 as larger than -0 as per the specification.

            float ax = Abs(x);
            float ay = Abs(y);

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
        static bool INumberBase<float>.TryConvertFromChecked<TOther>(TOther value, out float result)
        {
            return TryConvertFrom<TOther>(value, out result);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromSaturating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<float>.TryConvertFromSaturating<TOther>(TOther value, out float result)
        {
            return TryConvertFrom<TOther>(value, out result);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromTruncating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<float>.TryConvertFromTruncating<TOther>(TOther value, out float result)
        {
            return TryConvertFrom<TOther>(value, out result);
        }

        private static bool TryConvertFrom<TOther>(TOther value, out float result)
            where TOther : INumberBase<TOther>
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `float` will handle the other signed types and
            // `ConvertTo` will handle the unsigned types

            if (typeof(TOther) == typeof(double))
            {
                double actualValue = (double)(object)value;
                result = (float)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualValue = (Half)(object)value;
                result = (float)actualValue;
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
                result = (float)actualValue;
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
            else
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToChecked{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<float>.TryConvertToChecked<TOther>(float value, [MaybeNullWhen(false)] out TOther result)
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `float` will handle the other signed types and
            // `ConvertTo` will handle the unsigned types.

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
        static bool INumberBase<float>.TryConvertToSaturating<TOther>(float value, [MaybeNullWhen(false)] out TOther result)
        {
            return TryConvertTo<TOther>(value, out result);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToTruncating{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<float>.TryConvertToTruncating<TOther>(float value, [MaybeNullWhen(false)] out TOther result)
        {
            return TryConvertTo<TOther>(value, out result);
        }

        private static bool TryConvertTo<TOther>(float value, [MaybeNullWhen(false)] out TOther result)
            where TOther : INumberBase<TOther>
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `float` will handle the other signed types and
            // `ConvertTo` will handle the unsigned types.

            if (typeof(TOther) == typeof(byte))
            {
                var actualResult = (value >= byte.MaxValue) ? byte.MaxValue :
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
                decimal actualResult = (value >= +79228162514264337593543950336.0f) ? decimal.MaxValue :
                                       (value <= -79228162514264337593543950336.0f) ? decimal.MinValue :
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
                UInt128 actualResult = (value == PositiveInfinity) ? UInt128.MaxValue :
                                       (value <= 0.0f) ? UInt128.MinValue : (UInt128)value;
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
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out float result) => TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);

        //
        // IPowerFunctions
        //

        /// <inheritdoc cref="IPowerFunctions{TSelf}.Pow(TSelf, TSelf)" />
        public static float Pow(float x, float y) => MathF.Pow(x, y);

        //
        // IRootFunctions
        //

        /// <inheritdoc cref="IRootFunctions{TSelf}.Cbrt(TSelf)" />
        public static float Cbrt(float x) => MathF.Cbrt(x);

        /// <inheritdoc cref="IRootFunctions{TSelf}.Hypot(TSelf, TSelf)" />
        public static float Hypot(float x, float y)
        {
            // This code is based on `hypotf` from amd/aocl-libm-ose
            // Copyright (C) 2008-2020 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            float result;

            if (IsFinite(x) && IsFinite(y))
            {
                float ax = Abs(x);
                float ay = Abs(y);

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
                    double xx = ax;
                    xx *= xx;

                    double yy = ay;
                    yy *= yy;

                    result = (float)double.Sqrt(xx + yy);
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

                Debug.Assert(IsNaN(x) || IsNaN(y));
                result = NaN;
            }

            return result;
        }

        /// <inheritdoc cref="IRootFunctions{TSelf}.RootN(TSelf, int)" />
        public static float RootN(float x, int n)
        {
            float result;

            if (n > 0)
            {
                if (n == 2)
                {
                    result = (x != 0.0f) ? Sqrt(x) : 0.0f;
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

            static float PositiveN(float x, int n)
            {
                float result;

                if (IsFinite(x))
                {
                    if (x != 0)
                    {
                        if ((x > 0) || IsOddInteger(n))
                        {
                            result = (float)double.Pow(Abs(x), 1.0 / n);
                            result = CopySign(result, x);
                        }
                        else
                        {
                            result = NaN;
                        }
                    }
                    else if (IsEvenInteger(n))
                    {
                        result = 0.0f;
                    }
                    else
                    {
                        result = CopySign(0.0f, x);
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

            static float NegativeN(float x, int n)
            {
                float result;

                if (IsFinite(x))
                {
                    if (x != 0)
                    {
                        if ((x > 0) || IsOddInteger(n))
                        {
                            result = (float)double.Pow(Abs(x), 1.0 / n);
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
                    result = 0.0f;
                }
                else
                {
                    Debug.Assert(IsNegativeInfinity(x));
                    result = int.IsOddInteger(n) ? -0.0f : NaN;
                }

                return result;
            }
        }

        /// <inheritdoc cref="IRootFunctions{TSelf}.Sqrt(TSelf)" />
        public static float Sqrt(float x) => MathF.Sqrt(x);

        //
        // ISignedNumber
        //

        /// <inheritdoc cref="ISignedNumber{TSelf}.NegativeOne" />
        static float ISignedNumber<float>.NegativeOne => NegativeOne;

        //
        // ISpanParsable
        //

        /// <inheritdoc cref="ISpanParsable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static float Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider);

        /// <inheritdoc cref="ISpanParsable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out float result) => TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);

        //
        // ISubtractionOperators
        //

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_Subtraction(TSelf, TOther)" />
        static float ISubtractionOperators<float, float, float>.operator -(float left, float right) => left - right;

        //
        // ITrigonometricFunctions
        //

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Acos(TSelf)" />
        public static float Acos(float x) => MathF.Acos(x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AcosPi(TSelf)" />
        public static float AcosPi(float x)
        {
            return Acos(x) / Pi;
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Asin(TSelf)" />
        public static float Asin(float x) => MathF.Asin(x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AsinPi(TSelf)" />
        public static float AsinPi(float x)
        {
            return Asin(x) / Pi;
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Atan(TSelf)" />
        public static float Atan(float x) => MathF.Atan(x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AtanPi(TSelf)" />
        public static float AtanPi(float x)
        {
            return Atan(x) / Pi;
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Cos(TSelf)" />
        public static float Cos(float x) => MathF.Cos(x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.CosPi(TSelf)" />
        public static float CosPi(float x)
        {
            // This code is based on `cospif` from amd/aocl-libm-ose
            // Copyright (C) 2008-2020 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            float result;

            if (IsFinite(x))
            {
                float ax = Abs(x);

                if (ax < 8_388_608.0f)              // |x| < 2^23
                {
                    if (ax > 0.25f)
                    {
                        int integral = (int)ax;

                        float fractional = ax - integral;
                        float sign = int.IsOddInteger(integral) ? -1.0f : +1.0f;

                        if (fractional <= 0.25f)
                        {
                            if (fractional != 0.00f)
                            {
                                result = sign * CosForIntervalPiBy4(fractional * Pi);
                            }
                            else
                            {
                                result = sign;
                            }
                        }
                        else if (fractional <= 0.50f)
                        {
                            if (fractional != 0.50f)
                            {
                                result = sign * SinForIntervalPiBy4((0.5f - fractional) * Pi);
                            }
                            else
                            {
                                result = 0.0f;
                            }
                        }
                        else if (fractional <= 0.75)
                        {
                            result = -sign * SinForIntervalPiBy4((fractional - 0.5f) * Pi);
                        }
                        else
                        {
                            result = -sign * CosForIntervalPiBy4((1.0f - fractional) * Pi);
                        }
                    }
                    else if (ax >= 7.8125E-3f)      // |x| >= 2^-7
                    {
                        result = CosForIntervalPiBy4(x * Pi);
                    }
                    else if (ax >= 1.22070313E-4f)  // |x| >= 2^-13
                    {
                        float value = x * Pi;
                        result = 1.0f - (value * value * 0.5f);
                    }
                    else
                    {
                        result = 1.0f;
                    }
                }
                else if (ax < 16_777_216.0f)        // |x| < 2^24
                {
                    // x is an integer
                    int bits = BitConverter.SingleToInt32Bits(ax);
                    result = int.IsOddInteger(bits) ? -1.0f : +1.0f;
                }
                else
                {
                    // x is an even integer
                    result = 1.0f;
                }
            }
            else
            {
                result = NaN;
            }

            return result;
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Sin(TSelf)" />
        public static float Sin(float x) => MathF.Sin(x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinCos(TSelf)" />
        public static (float Sin, float Cos) SinCos(float x) => MathF.SinCos(x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinCos(TSelf)" />
        public static (float SinPi, float CosPi) SinCosPi(float x)
        {
            // This code is based on `cospif` and `sinpif` from amd/aocl-libm-ose
            // Copyright (C) 2008-2020 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            float sinPi;
            float cosPi;

            if (IsFinite(x))
            {
                float ax = Abs(x);

                if (ax < 8_388_608.0f)              // |x| < 2^23
                {
                    if (ax > 0.25f)
                    {
                        int integral = (int)ax;

                        float fractional = ax - integral;
                        float sign = int.IsOddInteger(integral) ? -1.0f : +1.0f;

                        float sinSign = ((x > 0.0f) ? +1.0f : -1.0f) * sign;
                        float cosSign = sign;

                        if (fractional <= 0.25f)
                        {
                            if (fractional != 0.00f)
                            {
                                float value = fractional * Pi;

                                sinPi = sinSign * SinForIntervalPiBy4(value);
                                cosPi = cosSign * CosForIntervalPiBy4(value);
                            }
                            else
                            {
                                sinPi = x * 0.0f;
                                cosPi = cosSign;
                            }
                        }
                        else if (fractional <= 0.50f)
                        {
                            if (fractional != 0.50f)
                            {
                                float value = (0.5f - fractional) * Pi;

                                sinPi = sinSign * CosForIntervalPiBy4(value);
                                cosPi = cosSign * SinForIntervalPiBy4(value);
                            }
                            else
                            {
                                sinPi = sinSign;
                                cosPi = 0.0f;
                            }
                        }
                        else if (fractional <= 0.75f)
                        {
                            float value = (fractional - 0.5f) * Pi;

                            sinPi = +sinSign * CosForIntervalPiBy4(value);
                            cosPi = -cosSign * SinForIntervalPiBy4(value);
                        }
                        else
                        {
                            float value = (1.0f - fractional) * Pi;

                            sinPi = +sinSign * SinForIntervalPiBy4(value);
                            cosPi = -cosSign * CosForIntervalPiBy4(value);
                        }
                    }
                    else if (ax >= 7.8125E-3f)      // |x| >= 2^-7
                    {
                        float value = x * Pi;

                        sinPi = SinForIntervalPiBy4(value);
                        cosPi = CosForIntervalPiBy4(value);
                    }
                    else if (ax >= 1.22070313E-4f)  // |x| >= 2^-13
                    {
                        float value = x * Pi;
                        float valueSq = value * value;

                        sinPi = value - (valueSq * value * (1.0f / 6.0f));
                        cosPi = 1.0f - (valueSq * 0.5f);
                    }
                    else
                    {
                        sinPi = x * Pi;
                        cosPi = 1.0f;
                    }
                }
                else if (ax < 16_777_216.0f)        // |x| < 2^24
                {
                    // x is an integer
                    sinPi = x * 0.0f;

                    int bits = BitConverter.SingleToInt32Bits(ax);
                    cosPi = int.IsOddInteger(bits) ? -1.0f : +1.0f;
                }
                else
                {
                    // x is an even integer
                    sinPi = x * 0.0f;
                    cosPi = 1.0f;
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
        public static float SinPi(float x)
        {
            // This code is based on `sinpif` from amd/aocl-libm-ose
            // Copyright (C) 2008-2020 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            float result;

            if (IsFinite(x))
            {
                float ax = Abs(x);

                if (ax < 8_388_608.0f)              // |x| < 2^23
                {
                    if (ax > 0.25f)
                    {
                        int integral = (int)ax;

                        float fractional = ax - integral;
                        float sign = ((x > 0.0f) ? +1.0f : -1.0f) * (int.IsOddInteger(integral) ? -1.0f : +1.0f);

                        if (fractional <= 0.25f)
                        {
                            if (fractional != 0.00f)
                            {
                                result = sign * SinForIntervalPiBy4(fractional * Pi);
                            }
                            else
                            {
                                result = x * 0.0f;
                            }
                        }
                        else if (fractional <= 0.50f)
                        {
                            if (fractional != 0.50f)
                            {
                                result = sign * CosForIntervalPiBy4((0.5f - fractional) * Pi);
                            }
                            else
                            {
                                result = sign;
                            }
                        }
                        else if (fractional <= 0.75f)
                        {
                            result = sign * CosForIntervalPiBy4((fractional - 0.5f) * Pi);
                        }
                        else
                        {
                            result = sign * SinForIntervalPiBy4((1.0f - fractional) * Pi);
                        }
                    }
                    else if (ax >= 7.8125E-3f)      // |x| >= 2^-7
                    {
                        result = SinForIntervalPiBy4(x * Pi);
                    }
                    else if (ax >= 1.22070313E-4f)  // |x| >= 2^-13
                    {
                        float value = x * Pi;
                        result = value - (value * value * value * (1.0f / 6.0f));
                    }
                    else
                    {
                        result = x * Pi;
                    }
                }
                else
                {
                    // x is an integer
                    result = x * 0.0f;
                }
            }
            else
            {
                result = NaN;
            }

            return result;
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Tan(TSelf)" />
        public static float Tan(float x) => MathF.Tan(x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.TanPi(TSelf)" />
        public static float TanPi(float x)
        {
            // This code is based on `tanpif` from amd/aocl-libm-ose
            // Copyright (C) 2008-2020 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            float result;

            if (IsFinite(x))
            {
                float ax = Abs(x);
                float sign = (x > 0.0f) ? +1.0f : -1.0f;

                if (ax < 8_388_608.0f)              // |x| < 2^23
                {
                    if (ax > 0.25f)
                    {
                        int integral = (int)ax;
                        float fractional = ax - integral;

                        if (fractional <= 0.25f)
                        {
                            if (fractional != 0.00f)
                            {
                                result = sign * TanForIntervalPiBy4(fractional * Pi, isReciprocal: false);
                            }
                            else
                            {
                                result = sign * (int.IsOddInteger(integral) ? -0.0f : +0.0f);
                            }
                        }
                        else if (fractional <= 0.50f)
                        {
                            if (fractional != 0.50f)
                            {
                                result = -sign * TanForIntervalPiBy4((0.5f - fractional) * Pi, isReciprocal: true);
                            }
                            else
                            {
                                result = +sign * (int.IsOddInteger(integral) ? NegativeInfinity : PositiveInfinity);
                            }
                        }
                        else if (fractional <= 0.75f)
                        {
                            result = +sign * TanForIntervalPiBy4((fractional - 0.5f) * Pi, isReciprocal: true);
                        }
                        else
                        {
                            result = -sign * TanForIntervalPiBy4((1.0f - fractional) * Pi, isReciprocal: false);
                        }
                    }
                    else if (ax >= 7.8125E-3f)      // |x| >= 2^-7
                    {
                        result = TanForIntervalPiBy4(x * Pi, isReciprocal: false);
                    }
                    else if (ax >= 1.22070313E-4f)  // |x| >= 2^-13
                    {
                        float value = x * Pi;
                        result = value + (value * value * value * (1.0f / 3.0f));
                    }
                    else
                    {
                        result = x * Pi;
                    }
                }
                else if (ax < 16_777_216)           // |x| < 2^24
                {
                    // x is an integer
                    int bits = BitConverter.SingleToInt32Bits(ax);
                    result = sign * (int.IsOddInteger(bits) ? -0.0f : +0.0f);
                }
                else
                {
                    // x is an even integer
                    result = sign * 0.0f;
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
        static float IUnaryNegationOperators<float, float>.operator -(float value) => -value;

        //
        // IUnaryPlusOperators
        //

        /// <inheritdoc cref="IUnaryPlusOperators{TSelf, TResult}.op_UnaryPlus(TSelf)" />
        static float IUnaryPlusOperators<float, float>.operator +(float value) => (float)(+value);

        //
        // Helpers
        //

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float CosForIntervalPiBy4(float x)
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

            const double C1 = +0.41666666666666665390037E-1;        // approx: +1 / 4!
            const double C2 = -0.13888888888887398280412E-2;        // approx: -1 / 6!
            const double C3 = +0.248015872987670414957399E-4;       // approx: +1 / 8!
            const double C4 = -0.275573172723441909470836E-6;       // approx: -1 / 10!

            double xx = x * x;
            double result = C4;

            result = (result * xx) + C3;
            result = (result * xx) + C2;
            result = (result * xx) + C1;

            result *= xx * xx;
            result += 1.0 - (0.5 * xx);

            return (float)result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float SinForIntervalPiBy4(float x)
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

            const double C1 = -0.166666666666666646259241729;       // approx: -1 / 3!
            const double C2 = +0.833333333333095043065222816E-2;    // approx: +1 / 5!
            const double C3 = -0.19841269836761125688538679E-3;     // approx: -1 / 7!
            const double C4 = +0.275573161037288022676895908448E-5; // approx: +1 / 9!

            double xx = x * x;
            double result = C4;

            result = (result * xx) + C3;
            result = (result * xx) + C2;
            result = (result * xx) + C1;

            result *= x * xx;
            result += x;

            return (float)result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float TanForIntervalPiBy4(float  x, bool isReciprocal)
        {
            // This code is based on `tan_piby4` from amd/aocl-libm-ose
            // Copyright (C) 2008-2020 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Core Remez [1, 2] approximation to tan(x) on the interval [0, pi / 4].

            double xx = x * x;

            double denominator = +0.1844239256901656082986661E-1;
            denominator = -0.51396505478854532132342E+0 + (denominator * xx);
            denominator = +0.115588821434688393452299E+1 + (denominator * xx);

            double numerator = -0.172032480471481694693109E-1;
            numerator = 0.385296071263995406715129E+0 + (numerator * xx);

            double result = x * xx;
            result *= numerator / denominator;
            result += x;

            if (isReciprocal)
            {
                result = -1.0 / result;
            }

            return (float)result;
        }
    }
}
