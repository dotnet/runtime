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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using Internal.Runtime.CompilerServices;

namespace System
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public readonly struct Single : IComparable, IConvertible, ISpanFormattable, IComparable<float>, IEquatable<float>
#if FEATURE_GENERIC_MATH
#pragma warning disable SA1001
        , IBinaryFloatingPoint<float>,
          IMinMaxValue<float>
#pragma warning restore SA1001
#endif // FEATURE_GENERIC_MATH
    {
        private readonly float m_value; // Do not rename (binary serialization)

        //
        // Public constants
        //
        public const float MinValue = (float)-3.40282346638528859e+38;
        public const float Epsilon = (float)1.4e-45;
        public const float MaxValue = (float)3.40282346638528859e+38;
        public const float PositiveInfinity = (float)1.0 / (float)0.0;
        public const float NegativeInfinity = (float)-1.0 / (float)0.0;
        public const float NaN = (float)0.0 / (float)0.0;

        // We use this explicit definition to avoid the confusion between 0.0 and -0.0.
        internal const float NegativeZero = (float)-0.0;

        //
        // Constants for manipulating the private bit-representation
        //

        internal const uint SignMask = 0x8000_0000;
        internal const int SignShift = 31;

        internal const uint ExponentMask = 0x7F80_0000;
        internal const int ExponentShift = 23;
        internal const uint ShiftedExponentMask = ExponentMask >> ExponentShift;

        internal const uint SignificandMask = 0x007F_FFFF;

        internal const byte MinSign = 0;
        internal const byte MaxSign = 1;

        internal const byte MinExponent = 0x00;
        internal const byte MaxExponent = 0xFF;

        internal const uint MinSignificand = 0x0000_0000;
        internal const uint MaxSignificand = 0x007F_FFFF;

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

        internal static int ExtractExponentFromBits(uint bits)
        {
            return (int)(bits >> ExponentShift) & (int)ShiftedExponentMask;
        }

        internal static uint ExtractSignificandFromBits(uint bits)
        {
            return bits & SignificandMask;
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

        [NonVersionable]
        public static bool operator ==(float left, float right) => left == right;

        [NonVersionable]
        public static bool operator !=(float left, float right) => left != right;

        [NonVersionable]
        public static bool operator <(float left, float right) => left < right;

        [NonVersionable]
        public static bool operator >(float left, float right) => left > right;

        [NonVersionable]
        public static bool operator <=(float left, float right) => left <= right;

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

        public string ToString(string? format)
        {
            return Number.FormatSingle(m_value, format, NumberFormatInfo.CurrentInfo);
        }

        public string ToString(string? format, IFormatProvider? provider)
        {
            return Number.FormatSingle(m_value, format, NumberFormatInfo.GetInstance(provider));
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
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

#if FEATURE_GENERIC_MATH
        //
        // IAdditionOperators
        //

        [RequiresPreviewFeatures]
        static float IAdditionOperators<float, float, float>.operator +(float left, float right)
            => left + right;

        // [RequiresPreviewFeatures]
        // static checked float IAdditionOperators<float, float, float>.operator +(float left, float right)
        //     => checked(left + right);

        //
        // IAdditiveIdentity
        //

        [RequiresPreviewFeatures]
        static float IAdditiveIdentity<float, float>.AdditiveIdentity => 0.0f;

        //
        // IBinaryNumber
        //

        [RequiresPreviewFeatures]
        static bool IBinaryNumber<float>.IsPow2(float value)
        {
            uint bits = BitConverter.SingleToUInt32Bits(value);

            uint exponent = (bits >> ExponentShift) & ShiftedExponentMask;
            uint significand = bits & SignificandMask;

            return (value > 0)
                && (exponent != MinExponent) && (exponent != MaxExponent)
                && (significand == MinSignificand);
        }

        [RequiresPreviewFeatures]
        static float IBinaryNumber<float>.Log2(float value)
            => MathF.Log2(value);

        //
        // IBitwiseOperators
        //

        [RequiresPreviewFeatures]
        static float IBitwiseOperators<float, float, float>.operator &(float left, float right)
        {
            uint bits = BitConverter.SingleToUInt32Bits(left) & BitConverter.SingleToUInt32Bits(right);
            return BitConverter.UInt32BitsToSingle(bits);
        }

        [RequiresPreviewFeatures]
        static float IBitwiseOperators<float, float, float>.operator |(float left, float right)
        {
            uint bits = BitConverter.SingleToUInt32Bits(left) | BitConverter.SingleToUInt32Bits(right);
            return BitConverter.UInt32BitsToSingle(bits);
        }

        [RequiresPreviewFeatures]
        static float IBitwiseOperators<float, float, float>.operator ^(float left, float right)
        {
            uint bits = BitConverter.SingleToUInt32Bits(left) ^ BitConverter.SingleToUInt32Bits(right);
            return BitConverter.UInt32BitsToSingle(bits);
        }

        [RequiresPreviewFeatures]
        static float IBitwiseOperators<float, float, float>.operator ~(float value)
        {
            uint bits = ~BitConverter.SingleToUInt32Bits(value);
            return BitConverter.UInt32BitsToSingle(bits);
        }

        //
        // IComparisonOperators
        //

        [RequiresPreviewFeatures]
        static bool IComparisonOperators<float, float>.operator <(float left, float right)
            => left < right;

        [RequiresPreviewFeatures]
        static bool IComparisonOperators<float, float>.operator <=(float left, float right)
            => left <= right;

        [RequiresPreviewFeatures]
        static bool IComparisonOperators<float, float>.operator >(float left, float right)
            => left > right;

        [RequiresPreviewFeatures]
        static bool IComparisonOperators<float, float>.operator >=(float left, float right)
            => left >= right;

        //
        // IDecrementOperators
        //

        [RequiresPreviewFeatures]
        static float IDecrementOperators<float>.operator --(float value)
            => --value;

        // [RequiresPreviewFeatures]
        // static checked float IDecrementOperators<float>.operator --(float value)
        //     => checked(--value);

        //
        // IDivisionOperators
        //

        [RequiresPreviewFeatures]
        static float IDivisionOperators<float, float, float>.operator /(float left, float right)
            => left / right;

        // [RequiresPreviewFeatures]
        // static checked float IDivisionOperators<float, float, float>.operator /(float left, float right)
        //     => checked(left / right);

        //
        // IEqualityOperators
        //

        [RequiresPreviewFeatures]
        static bool IEqualityOperators<float, float>.operator ==(float left, float right)
            => left == right;

        [RequiresPreviewFeatures]
        static bool IEqualityOperators<float, float>.operator !=(float left, float right)
            => left != right;

        //
        // IFloatingPoint
        //

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.E => MathF.E;

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.Epsilon => Epsilon;

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.NaN => NaN;

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.NegativeInfinity => NegativeInfinity;

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.NegativeZero => -0.0f;

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.Pi => MathF.PI;

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.PositiveInfinity => PositiveInfinity;

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.Tau => MathF.Tau;

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.Acos(float x)
            => MathF.Acos(x);

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.Acosh(float x)
            => MathF.Acosh(x);

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.Asin(float x)
            => MathF.Asin(x);

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.Asinh(float x)
            => MathF.Asinh(x);

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.Atan(float x)
            => MathF.Atan(x);

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.Atan2(float y, float x)
            => MathF.Atan2(y, x);

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.Atanh(float x)
            => MathF.Atanh(x);

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.BitIncrement(float x)
            => MathF.BitIncrement(x);

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.BitDecrement(float x)
            => MathF.BitDecrement(x);

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.Cbrt(float x)
            => MathF.Cbrt(x);

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.Ceiling(float x)
            => MathF.Ceiling(x);

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.CopySign(float x, float y)
            => MathF.CopySign(x, y);

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.Cos(float x)
            => MathF.Cos(x);

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.Cosh(float x)
            => MathF.Cosh(x);

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.Exp(float x)
            => MathF.Exp(x);

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.Floor(float x)
            => MathF.Floor(x);

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.FusedMultiplyAdd(float left, float right, float addend)
            => MathF.FusedMultiplyAdd(left, right, addend);

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.IEEERemainder(float left, float right)
            => MathF.IEEERemainder(left, right);

        [RequiresPreviewFeatures]
        static TInteger IFloatingPoint<float>.ILogB<TInteger>(float x)
            => TInteger.Create(MathF.ILogB(x));

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.Log(float x)
            => MathF.Log(x);

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.Log(float x, float newBase)
            => MathF.Log(x, newBase);

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.Log2(float x)
            => MathF.Log2(x);

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.Log10(float x)
            => MathF.Log10(x);

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.MaxMagnitude(float x, float y)
            => MathF.MaxMagnitude(x, y);

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.MinMagnitude(float x, float y)
            => MathF.MinMagnitude(x, y);

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.Pow(float x, float y)
            => MathF.Pow(x, y);

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.Round(float x)
            => MathF.Round(x);

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.Round<TInteger>(float x, TInteger digits)
            => MathF.Round(x, int.Create(digits));

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.Round(float x, MidpointRounding mode)
            => MathF.Round(x, mode);

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.Round<TInteger>(float x, TInteger digits, MidpointRounding mode)
            => MathF.Round(x, int.Create(digits), mode);

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.ScaleB<TInteger>(float x, TInteger n)
            => MathF.ScaleB(x, int.Create(n));

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.Sin(float x)
            => MathF.Sin(x);

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.Sinh(float x)
            => MathF.Sinh(x);

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.Sqrt(float x)
            => MathF.Sqrt(x);

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.Tan(float x)
            => MathF.Tan(x);

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.Tanh(float x)
            => MathF.Tanh(x);

        [RequiresPreviewFeatures]
        static float IFloatingPoint<float>.Truncate(float x)
            => MathF.Truncate(x);

        [RequiresPreviewFeatures]
        static bool IFloatingPoint<float>.IsFinite(float x) => IsFinite(x);

        [RequiresPreviewFeatures]
        static bool IFloatingPoint<float>.IsInfinity(float x) => IsInfinity(x);

        [RequiresPreviewFeatures]
        static bool IFloatingPoint<float>.IsNaN(float x) => IsNaN(x);

        [RequiresPreviewFeatures]
        static bool IFloatingPoint<float>.IsNegative(float x) => IsNegative(x);

        [RequiresPreviewFeatures]
        static bool IFloatingPoint<float>.IsNegativeInfinity(float x) => IsNegativeInfinity(x);

        [RequiresPreviewFeatures]
        static bool IFloatingPoint<float>.IsNormal(float x) => IsNormal(x);

        [RequiresPreviewFeatures]
        static bool IFloatingPoint<float>.IsPositiveInfinity(float x) => IsPositiveInfinity(x);

        [RequiresPreviewFeatures]
        static bool IFloatingPoint<float>.IsSubnormal(float x) => IsSubnormal(x);

        // static float IFloatingPoint<float>.AcosPi(float x)
        //     => MathF.AcosPi(x);
        //
        // static float IFloatingPoint<float>.AsinPi(float x)
        //     => MathF.AsinPi(x);
        //
        // static float IFloatingPoint<float>.AtanPi(float x)
        //     => MathF.AtanPi(x);
        //
        // static float IFloatingPoint<float>.Atan2Pi(float y, float x)
        //     => MathF.Atan2Pi(y, x);
        //
        // static float IFloatingPoint<float>.Compound(float x, float n)
        //     => MathF.Compound(x, n);
        //
        // static float IFloatingPoint<float>.CosPi(float x)
        //     => MathF.CosPi(x);
        //
        // static float IFloatingPoint<float>.ExpM1(float x)
        //     => MathF.ExpM1(x);
        //
        // static float IFloatingPoint<float>.Exp2(float x)
        //     => MathF.Exp2(x);
        //
        // static float IFloatingPoint<float>.Exp2M1(float x)
        //     => MathF.Exp2M1(x);
        //
        // static float IFloatingPoint<float>.Exp10(float x)
        //     => MathF.Exp10(x);
        //
        // static float IFloatingPoint<float>.Exp10M1(float x)
        //     => MathF.Exp10M1(x);
        //
        // static float IFloatingPoint<float>.Hypot(float x, float y)
        //     => MathF.Hypot(x, y);
        //
        // static float IFloatingPoint<float>.LogP1(float x)
        //     => MathF.LogP1(x);
        //
        // static float IFloatingPoint<float>.Log2P1(float x)
        //     => MathF.Log2P1(x);
        //
        // static float IFloatingPoint<float>.Log10P1(float x)
        //     => MathF.Log10P1(x);
        //
        // static float IFloatingPoint<float>.MaxMagnitudeNumber(float x, float y)
        //     => MathF.MaxMagnitudeNumber(x, y);
        //
        // static float IFloatingPoint<float>.MaxNumber(float x, float y)
        //     => MathF.MaxNumber(x, y);
        //
        // static float IFloatingPoint<float>.MinMagnitudeNumber(float x, float y)
        //     => MathF.MinMagnitudeNumber(x, y);
        //
        // static float IFloatingPoint<float>.MinNumber(float x, float y)
        //     => MathF.MinNumber(x, y);
        //
        // static float IFloatingPoint<float>.Root(float x, float n)
        //     => MathF.Root(x, n);
        //
        // static float IFloatingPoint<float>.SinPi(float x)
        //     => MathF.SinPi(x, y);
        //
        // static float TanPi(float x)
        //     => MathF.TanPi(x, y);

        //
        // IIncrementOperators
        //

        [RequiresPreviewFeatures]
        static float IIncrementOperators<float>.operator ++(float value)
            => ++value;

        // [RequiresPreviewFeatures]
        // static checked float IIncrementOperators<float>.operator ++(float value)
        //     => checked(++value);

        //
        // IMinMaxValue
        //

        [RequiresPreviewFeatures]
        static float IMinMaxValue<float>.MinValue => MinValue;

        [RequiresPreviewFeatures]
        static float IMinMaxValue<float>.MaxValue => MaxValue;

        //
        // IModulusOperators
        //

        [RequiresPreviewFeatures]
        static float IModulusOperators<float, float, float>.operator %(float left, float right)
            => left % right;

        // [RequiresPreviewFeatures]
        // static checked float IModulusOperators<float, float, float>.operator %(float left, float right)
        //     => checked(left % right);

        //
        // IMultiplicativeIdentity
        //

        [RequiresPreviewFeatures]
        static float IMultiplicativeIdentity<float, float>.MultiplicativeIdentity => 1.0f;

        //
        // IMultiplyOperators
        //

        [RequiresPreviewFeatures]
        static float IMultiplyOperators<float, float, float>.operator *(float left, float right)
            => (float)(left * right);

        // [RequiresPreviewFeatures]
        // static checked float IMultiplyOperators<float, float, float>.operator *(float left, float right)
        //     => checked((float)(left * right));

        //
        // INumber
        //

        [RequiresPreviewFeatures]
        static float INumber<float>.One => 1.0f;

        [RequiresPreviewFeatures]
        static float INumber<float>.Zero => 0.0f;

        [RequiresPreviewFeatures]
        static float INumber<float>.Abs(float value)
            => MathF.Abs(value);

        [RequiresPreviewFeatures]
        static float INumber<float>.Clamp(float value, float min, float max)
            => Math.Clamp(value, min, max);

        [RequiresPreviewFeatures]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float INumber<float>.Create<TOther>(TOther value)
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

        [RequiresPreviewFeatures]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float INumber<float>.CreateSaturating<TOther>(TOther value)
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

        [RequiresPreviewFeatures]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float INumber<float>.CreateTruncating<TOther>(TOther value)
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

        [RequiresPreviewFeatures]
        static (float Quotient, float Remainder) INumber<float>.DivRem(float left, float right)
            => (left / right, left % right);

        [RequiresPreviewFeatures]
        static float INumber<float>.Max(float x, float y)
            => MathF.Max(x, y);

        [RequiresPreviewFeatures]
        static float INumber<float>.Min(float x, float y)
            => MathF.Min(x, y);

        [RequiresPreviewFeatures]
        static float INumber<float>.Parse(string s, NumberStyles style, IFormatProvider? provider)
            => Parse(s, style, provider);

        [RequiresPreviewFeatures]
        static float INumber<float>.Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider)
            => Parse(s, style, provider);

        [RequiresPreviewFeatures]
        static float INumber<float>.Sign(float value)
            => MathF.Sign(value);

        [RequiresPreviewFeatures]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumber<float>.TryCreate<TOther>(TOther value, out float result)
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

        [RequiresPreviewFeatures]
        static bool INumber<float>.TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out float result)
            => TryParse(s, style, provider, out result);

        [RequiresPreviewFeatures]
        static bool INumber<float>.TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out float result)
            => TryParse(s, style, provider, out result);

        //
        // IParseable
        //

        [RequiresPreviewFeatures]
        static float IParseable<float>.Parse(string s, IFormatProvider? provider)
            => Parse(s, provider);

        [RequiresPreviewFeatures]
        static bool IParseable<float>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out float result)
            => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // ISignedNumber
        //

        [RequiresPreviewFeatures]
        static float ISignedNumber<float>.NegativeOne => -1;

        //
        // ISpanParseable
        //

        [RequiresPreviewFeatures]
        static float ISpanParseable<float>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
            => Parse(s, NumberStyles.Integer, provider);

        [RequiresPreviewFeatures]
        static bool ISpanParseable<float>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out float result)
            => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // ISubtractionOperators
        //

        [RequiresPreviewFeatures]
        static float ISubtractionOperators<float, float, float>.operator -(float left, float right)
            => (float)(left - right);

        // [RequiresPreviewFeatures]
        // static checked float ISubtractionOperators<float, float, float>.operator -(float left, float right)
        //     => checked((float)(left - right));

        //
        // IUnaryNegationOperators
        //

        [RequiresPreviewFeatures]
        static float IUnaryNegationOperators<float, float>.operator -(float value) => (float)(-value);

        // [RequiresPreviewFeatures]
        // static checked float IUnaryNegationOperators<float, float>.operator -(float value) => checked((float)(-value));

        //
        // IUnaryNegationOperators
        //

        [RequiresPreviewFeatures]
        static float IUnaryPlusOperators<float, float>.operator +(float value) => (float)(+value);

        // [RequiresPreviewFeatures]
        // static checked float IUnaryPlusOperators<float, float>.operator +(float value) => checked((float)(+value));
#endif // FEATURE_GENERIC_MATH
    }
}
