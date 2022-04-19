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
          IMinMaxValue<float>
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

        /// <summary>Represents the ratio of the circumference of a circle to its diameter, specified by the constant, π.</summary>
        /// <remarks>Pi is approximately 3.1415926535897932385.</remarks>
        public const float Pi = MathF.PI;

        /// <summary>Represents the number of radians in one turn, specified by the constant, τ.</summary>
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

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther}.op_Equality(TSelf, TOther)" />
        [NonVersionable]
        public static bool operator ==(float left, float right) => left == right;

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther}.op_Inequality(TSelf, TOther)" />
        [NonVersionable]
        public static bool operator !=(float left, float right) => left != right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_LessThan(TSelf, TOther)" />
        [NonVersionable]
        public static bool operator <(float left, float right) => left < right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_GreaterThan(TSelf, TOther)" />
        [NonVersionable]
        public static bool operator >(float left, float right) => left > right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_LessThanOrEqual(TSelf, TOther)" />
        [NonVersionable]
        public static bool operator <=(float left, float right) => left <= right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_GreaterThanOrEqual(TSelf, TOther)" />
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

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        static float IAdditionOperators<float, float, float>.operator checked +(float left, float right) => left + right;

        //
        // IAdditiveIdentity
        //

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static float IAdditiveIdentity<float, float>.AdditiveIdentity => AdditiveIdentity;

        //
        // IBinaryNumber
        //

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

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        static float IDecrementOperators<float>.operator checked --(float value) => --value;

        //
        // IDivisionOperators
        //

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_Division(TSelf, TOther)" />
        static float IDivisionOperators<float, float, float>.operator /(float left, float right) => left / right;

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_CheckedDivision(TSelf, TOther)" />
        static float IDivisionOperators<float, float, float>.operator checked /(float left, float right) => left / right;

        //
        // IExponentialFunctions
        //

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp" />
        public static float Exp(float x) => MathF.Exp(x);

        // /// <inheritdoc cref="IExponentialFunctions{TSelf}.ExpM1(TSelf)" />
        // public static float ExpM1(float x) => MathF.ExpM1(x);

        // /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp2(TSelf)" />
        // public static float Exp2(float x) => MathF.Exp2(x);

        // /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp2M1(TSelf)" />
        // public static float Exp2M1(float x) => MathF.Exp2M1(x);

        // /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp10(TSelf)" />
        // public static float Exp10(float x) => MathF.Exp10(x);

        // /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp10M1(TSelf)" />
        // public static float Exp10M1(float x) => MathF.Exp10M1(x);

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

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetExponentShortestBitLength()" />
        long IFloatingPoint<float>.GetExponentShortestBitLength()
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
        int IFloatingPoint<float>.GetExponentByteCount() => sizeof(sbyte);

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

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetSignificandBitLength()" />
        long IFloatingPoint<float>.GetSignificandBitLength() => 24;

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetSignificandByteCount()" />
        int IFloatingPoint<float>.GetSignificandByteCount() => sizeof(uint);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteSignificandLittleEndian(Span{byte}, out int)" />
        bool IFloatingPoint<float>.TryWriteSignificandLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(uint))
            {
                uint significand = Significand;
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
        // IFloatingPointIeee754
        //

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.E" />
        static float IFloatingPointIeee754<float>.E => E;

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Epsilon" />
        static float IFloatingPointIeee754<float>.Epsilon => Epsilon;

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.NaN" />
        static float IFloatingPointIeee754<float>.NaN => NaN;

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.NegativeInfinity" />
        static float IFloatingPointIeee754<float>.NegativeInfinity => NegativeInfinity;

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.NegativeZero" />
        static float IFloatingPointIeee754<float>.NegativeZero => NegativeZero;

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Pi" />
        static float IFloatingPointIeee754<float>.Pi => Pi;

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.PositiveInfinity" />
        static float IFloatingPointIeee754<float>.PositiveInfinity => PositiveInfinity;

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Tau" />
        static float IFloatingPointIeee754<float>.Tau => Tau;

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

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ReciprocalEstimate(TSelf)" />
        public static float ReciprocalEstimate(float x) => MathF.ReciprocalEstimate(x);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ReciprocalSqrtEstimate(TSelf)" />
        public static float ReciprocalSqrtEstimate(float x) => MathF.ReciprocalSqrtEstimate(x);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ScaleB(TSelf, int)" />
        public static float ScaleB(float x, int n) => MathF.ScaleB(x, n);

        // /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Compound(TSelf, TSelf)" />
        // public static float Compound(float x, float n) => MathF.Compound(x, n);

        // /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.MaxMagnitudeNumber(TSelf, TSelf)" />
        // public static float MaxMagnitudeNumber(float x, float y) => MathF.MaxMagnitudeNumber(x, y);

        // /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.MaxNumber(TSelf, TSelf)" />
        // public static float MaxNumber(float x, float y) => MathF.MaxNumber(x, y);

        // /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.MinMagnitudeNumber(TSelf, TSelf)" />
        // public static float MinMagnitudeNumber(float x, float y) => MathF.MinMagnitudeNumber(x, y);

        // /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.MinNumber(TSelf, TSelf)" />
        // public static float MinNumber(float x, float y) => MathF.MinNumber(x, y);

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

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_CheckedIncrement(TSelf)" />
        static float IIncrementOperators<float>.operator checked ++(float value) => ++value;

        //
        // ILogarithmicFunctions
        //

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log(TSelf)" />
        public static float Log(float x) => MathF.Log(x);

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log(TSelf, TSelf)" />
        public static float Log(float x, float newBase) => MathF.Log(x, newBase);

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log10(TSelf)" />
        public static float Log10(float x) => MathF.Log10(x);

        // /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.LogP1(TSelf)" />
        // public static float LogP1(float x) => MathF.LogP1(x);

        // /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log2P1(TSelf)" />
        // public static float Log2P1(float x) => MathF.Log2P1(x);

        // /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log10P1(TSelf)" />
        // public static float Log10P1(float x) => MathF.Log10P1(x);

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

        // static checked float IModulusOperators<float, float, float>.operator %(float left, float right) => checked(left % right);

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

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_CheckedMultiply(TSelf, TOther)" />
        static float IMultiplyOperators<float, float, float>.operator checked *(float left, float right) => left * right;

        //
        // INumber
        //

        /// <inheritdoc cref="INumber{TSelf}.Abs(TSelf)" />
        public static float Abs(float value) => MathF.Abs(value);

        /// <inheritdoc cref="INumber{TSelf}.Clamp(TSelf, TSelf, TSelf)" />
        public static float Clamp(float value, float min, float max) => Math.Clamp(value, min, max);

        /// <inheritdoc cref="INumber{TSelf}.CopySign(TSelf, TSelf)" />
        public static float CopySign(float x, float y) => MathF.CopySign(x, y);

        /// <inheritdoc cref="INumber{TSelf}.CreateChecked{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CreateChecked<TOther>(TOther value)
            where TOther : INumber<TOther>
        {
            if (typeof(TOther) == typeof(byte))
            {
                return (byte)(object)value;
            }
            else if (typeof(TOther) == typeof(char))
            {
                return (char)(object)value;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                return (float)(decimal)(object)value;
            }
            else if (typeof(TOther) == typeof(double))
            {
                return (float)(double)(object)value;
            }
            else if (typeof(TOther) == typeof(short))
            {
                return (short)(object)value;
            }
            else if (typeof(TOther) == typeof(int))
            {
                return (int)(object)value;
            }
            else if (typeof(TOther) == typeof(long))
            {
                return (long)(object)value;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                return (nint)(object)value;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                return (sbyte)(object)value;
            }
            else if (typeof(TOther) == typeof(float))
            {
                return (float)(object)value;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                return (ushort)(object)value;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                return (uint)(object)value;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                return (ulong)(object)value;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                return (nuint)(object)value;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        /// <inheritdoc cref="INumber{TSelf}.CreateSaturating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CreateSaturating<TOther>(TOther value)
            where TOther : INumber<TOther>
        {
            if (typeof(TOther) == typeof(byte))
            {
                return (byte)(object)value;
            }
            else if (typeof(TOther) == typeof(char))
            {
                return (char)(object)value;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                return (float)(decimal)(object)value;
            }
            else if (typeof(TOther) == typeof(double))
            {
                return (float)(double)(object)value;
            }
            else if (typeof(TOther) == typeof(short))
            {
                return (short)(object)value;
            }
            else if (typeof(TOther) == typeof(int))
            {
                return (int)(object)value;
            }
            else if (typeof(TOther) == typeof(long))
            {
                return (long)(object)value;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                return (nint)(object)value;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                return (sbyte)(object)value;
            }
            else if (typeof(TOther) == typeof(float))
            {
                return (float)(object)value;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                return (ushort)(object)value;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                return (uint)(object)value;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                return (ulong)(object)value;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                return (nuint)(object)value;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        /// <inheritdoc cref="INumber{TSelf}.CreateTruncating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CreateTruncating<TOther>(TOther value)
            where TOther : INumber<TOther>
        {
            if (typeof(TOther) == typeof(byte))
            {
                return (byte)(object)value;
            }
            else if (typeof(TOther) == typeof(char))
            {
                return (char)(object)value;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                return (float)(decimal)(object)value;
            }
            else if (typeof(TOther) == typeof(double))
            {
                return (float)(double)(object)value;
            }
            else if (typeof(TOther) == typeof(short))
            {
                return (short)(object)value;
            }
            else if (typeof(TOther) == typeof(int))
            {
                return (int)(object)value;
            }
            else if (typeof(TOther) == typeof(long))
            {
                return (long)(object)value;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                return (nint)(object)value;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                return (sbyte)(object)value;
            }
            else if (typeof(TOther) == typeof(float))
            {
                return (float)(object)value;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                return (ushort)(object)value;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                return (uint)(object)value;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                return (ulong)(object)value;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                return (nuint)(object)value;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        /// <inheritdoc cref="INumber{TSelf}.Max(TSelf, TSelf)" />
        public static float Max(float x, float y) => MathF.Max(x, y);

        /// <inheritdoc cref="INumber{TSelf}.MaxMagnitude(TSelf, TSelf)" />
        public static float MaxMagnitude(float x, float y) => MathF.MaxMagnitude(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Min(TSelf, TSelf)" />
        public static float Min(float x, float y) => MathF.Min(x, y);

        /// <inheritdoc cref="INumber{TSelf}.MinMagnitude(TSelf, TSelf)" />
        public static float MinMagnitude(float x, float y) => MathF.MinMagnitude(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Sign(TSelf)" />
        public static int Sign(float value) => MathF.Sign(value);

        /// <inheritdoc cref="INumber{TSelf}.TryCreate{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryCreate<TOther>(TOther value, out float result)
            where TOther : INumber<TOther>
        {
            if (typeof(TOther) == typeof(byte))
            {
                result = (byte)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                result = (char)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                result = (float)(decimal)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                result = (float)(double)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                result = (short)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                result = (int)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                result = (long)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                result = (nint)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                result = (sbyte)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                result = (float)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                result = (ushort)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                result = (uint)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                result = (ulong)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                result = (nuint)(object)value;
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
        static float INumberBase<float>.One => One;

        /// <inheritdoc cref="INumberBase{TSelf}.Zero" />
        static float INumberBase<float>.Zero => Zero;

        //
        // IParsable
        //

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

        // /// <inheritdoc cref="IRootFunctions{TSelf}.Hypot(TSelf, TSelf)" />
        // public static float Hypot(float x, float y) => MathF.Hypot(x, y);

        /// <inheritdoc cref="IRootFunctions{TSelf}.Sqrt(TSelf)" />
        public static float Sqrt(float x) => MathF.Sqrt(x);

        // /// <inheritdoc cref="IRootFunctions{TSelf}.Root(TSelf, TSelf)" />
        // public static float Root(float x, float n) => MathF.Root(x, n);

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

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_CheckedSubtraction(TSelf, TOther)" />
        static float ISubtractionOperators<float, float, float>.operator checked -(float left, float right) => left - right;

        //
        // ITrigonometricFunctions
        //

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Acos(TSelf)" />
        public static float Acos(float x) => MathF.Acos(x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Asin(TSelf)" />
        public static float Asin(float x) => MathF.Asin(x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Atan(TSelf)" />
        public static float Atan(float x) => MathF.Atan(x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Atan2(TSelf, TSelf)" />
        public static float Atan2(float y, float x) => MathF.Atan2(y, x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Cos(TSelf)" />
        public static float Cos(float x) => MathF.Cos(x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Sin(TSelf)" />
        public static float Sin(float x) => MathF.Sin(x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinCos(TSelf)" />
        public static (float Sin, float Cos) SinCos(float x) => MathF.SinCos(x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Tan(TSelf)" />
        public static float Tan(float x) => MathF.Tan(x);

        // /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AcosPi(TSelf)" />
        // public static float AcosPi(float x) => MathF.AcosPi(x);

        // /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AsinPi(TSelf)" />
        // public static float AsinPi(float x) => MathF.AsinPi(x);

        // /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AtanPi(TSelf)" />
        // public static float AtanPi(float x) => MathF.AtanPi(x);

        // /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Atan2Pi(TSelf)" />
        // public static float Atan2Pi(float y, float x) => MathF.Atan2Pi(y, x);

        // /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.CosPi(TSelf)" />
        // public static float CosPi(float x) => MathF.CosPi(x);

        // /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinPi(TSelf)" />
        // public static float SinPi(float x) => MathF.SinPi(x, y);

        // /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.TanPi(TSelf)" />
        // public static float TanPi(float x) => MathF.TanPi(x, y);

        //
        // IUnaryNegationOperators
        //

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_UnaryNegation(TSelf)" />
        static float IUnaryNegationOperators<float, float>.operator -(float value) => -value;

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_CheckedUnaryNegation(TSelf)" />
        static float IUnaryNegationOperators<float, float>.operator checked -(float value) => -value;

        //
        // IUnaryPlusOperators
        //

        /// <inheritdoc cref="IUnaryPlusOperators{TSelf, TResult}.op_UnaryPlus(TSelf)" />
        static float IUnaryPlusOperators<float, float>.operator +(float value) => (float)(+value);
    }
}
