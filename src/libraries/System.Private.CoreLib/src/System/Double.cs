// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
**
** Purpose: A representation of an IEEE double precision
**          floating point number.
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
    public readonly struct Double : IComparable, IConvertible, ISpanFormattable, IComparable<double>, IEquatable<double>
#if FEATURE_GENERIC_MATH
#pragma warning disable SA1001
        , IBinaryFloatingPoint<double>,
          IMinMaxValue<double>
#pragma warning restore SA1001
#endif // FEATURE_GENERIC_MATH
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

        // We use this explicit definition to avoid the confusion between 0.0 and -0.0.
        internal const double NegativeZero = -0.0;

        //
        // Constants for manipulating the private bit-representation
        //

        internal const ulong SignMask = 0x8000_0000_0000_0000;
        internal const int SignShift = 63;

        internal const ulong ExponentMask = 0x7FF0_0000_0000_0000;
        internal const int ExponentShift = 52;
        internal const uint ShiftedExponentMask = (uint)(ExponentMask >> ExponentShift);

        internal const ulong SignificandMask = 0x000F_FFFF_FFFF_FFFF;

        internal const byte MinSign = 0;
        internal const byte MaxSign = 1;

        internal const ushort MinExponent = 0x0000;
        internal const ushort MaxExponent = 0x07FF;

        internal const ulong MinSignificand = 0x0000_0000_0000_0000;
        internal const ulong MaxSignificand = 0x000F_FFFF_FFFF_FFFF;

        /// <summary>Determines whether the specified value is finite (zero, subnormal, or normal).</summary>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool IsFinite(double d)
        {
            long bits = BitConverter.DoubleToInt64Bits(d);
            return (bits & 0x7FFFFFFFFFFFFFFF) < 0x7FF0000000000000;
        }

        /// <summary>Determines whether the specified value is infinite.</summary>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool IsInfinity(double d)
        {
            long bits = BitConverter.DoubleToInt64Bits(d);
            return (bits & 0x7FFFFFFFFFFFFFFF) == 0x7FF0000000000000;
        }

        /// <summary>Determines whether the specified value is NaN.</summary>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool IsNaN(double d)
        {
            // A NaN will never equal itself so this is an
            // easy and efficient way to check for NaN.

            #pragma warning disable CS1718
            return d != d;
            #pragma warning restore CS1718
        }

        /// <summary>Determines whether the specified value is negative.</summary>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool IsNegative(double d)
        {
            return BitConverter.DoubleToInt64Bits(d) < 0;
        }

        /// <summary>Determines whether the specified value is negative infinity.</summary>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNegativeInfinity(double d)
        {
            return d == double.NegativeInfinity;
        }

        /// <summary>Determines whether the specified value is normal.</summary>
        [NonVersionable]
        // This is probably not worth inlining, it has branches and should be rarely called
        public static unsafe bool IsNormal(double d)
        {
            long bits = BitConverter.DoubleToInt64Bits(d);
            bits &= 0x7FFFFFFFFFFFFFFF;
            return (bits < 0x7FF0000000000000) && (bits != 0) && ((bits & 0x7FF0000000000000) != 0);
        }

        /// <summary>Determines whether the specified value is positive infinity.</summary>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPositiveInfinity(double d)
        {
            return d == double.PositiveInfinity;
        }

        /// <summary>Determines whether the specified value is subnormal.</summary>
        [NonVersionable]
        // This is probably not worth inlining, it has branches and should be rarely called
        public static unsafe bool IsSubnormal(double d)
        {
            long bits = BitConverter.DoubleToInt64Bits(d);
            bits &= 0x7FFFFFFFFFFFFFFF;
            return (bits < 0x7FF0000000000000) && (bits != 0) && ((bits & 0x7FF0000000000000) == 0);
        }

        internal static int ExtractExponentFromBits(ulong bits)
        {
            return (int)(bits >> ExponentShift) & (int)ShiftedExponentMask;
        }

        internal static ulong ExtractSignificandFromBits(ulong bits)
        {
            return bits & SignificandMask;
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
            if (value == null)
            {
                return 1;
            }

            if (value is double d)
            {
                if (m_value < d) return -1;
                if (m_value > d) return 1;
                if (m_value == d) return 0;

                // At least one of the values is NaN.
                if (IsNaN(m_value))
                    return IsNaN(d) ? 0 : -1;
                else
                    return 1;
            }

            throw new ArgumentException(SR.Arg_MustBeDouble);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(double value)
        {
            if (m_value < value) return -1;
            if (m_value > value) return 1;
            if (m_value == value) return 0;

            // At least one of the values is NaN.
            if (IsNaN(m_value))
                return IsNaN(value) ? 0 : -1;
            else
                return 1;
        }

        // True if obj is another Double with the same value as the current instance.  This is
        // a method of object equality, that only returns true if obj is also a double.
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (!(obj is double))
            {
                return false;
            }
            double temp = ((double)obj).m_value;
            // This code below is written this way for performance reasons i.e the != and == check is intentional.
            if (temp == m_value)
            {
                return true;
            }
            return IsNaN(temp) && IsNaN(m_value);
        }

        [NonVersionable]
        public static bool operator ==(double left, double right) => left == right;

        [NonVersionable]
        public static bool operator !=(double left, double right) => left != right;

        [NonVersionable]
        public static bool operator <(double left, double right) => left < right;

        [NonVersionable]
        public static bool operator >(double left, double right) => left > right;

        [NonVersionable]
        public static bool operator <=(double left, double right) => left <= right;

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
            long bits = Unsafe.As<double, long>(ref Unsafe.AsRef(in m_value));

            // Optimized check for IsNan() || IsZero()
            if (((bits - 1) & 0x7FFFFFFFFFFFFFFF) >= 0x7FF0000000000000)
            {
                // Ensure that all NaNs and both zeros have the same hash code
                bits &= 0x7FF0000000000000;
            }

            return unchecked((int)bits) ^ ((int)(bits >> 32));
        }

        public override string ToString()
        {
            return Number.FormatDouble(m_value, null, NumberFormatInfo.CurrentInfo);
        }

        public string ToString(string? format)
        {
            return Number.FormatDouble(m_value, format, NumberFormatInfo.CurrentInfo);
        }

        public string ToString(IFormatProvider? provider)
        {
            return Number.FormatDouble(m_value, null, NumberFormatInfo.GetInstance(provider));
        }

        public string ToString(string? format, IFormatProvider? provider)
        {
            return Number.FormatDouble(m_value, format, NumberFormatInfo.GetInstance(provider));
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatDouble(m_value, format, NumberFormatInfo.GetInstance(provider), destination, out charsWritten);
        }

        public static double Parse(string s)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseDouble(s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.CurrentInfo);
        }

        public static double Parse(string s, NumberStyles style)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseDouble(s, style, NumberFormatInfo.CurrentInfo);
        }

        public static double Parse(string s, IFormatProvider? provider)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseDouble(s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.GetInstance(provider));
        }

        public static double Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseDouble(s, style, NumberFormatInfo.GetInstance(provider));
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
            return Number.ParseDouble(s, style, NumberFormatInfo.GetInstance(provider));
        }

        public static bool TryParse([NotNullWhen(true)] string? s, out double result)
        {
            if (s == null)
            {
                result = 0;
                return false;
            }

            return TryParse((ReadOnlySpan<char>)s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse(ReadOnlySpan<char> s, out double result)
        {
            return TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out double result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);

            if (s == null)
            {
                result = 0;
                return false;
            }

            return TryParse((ReadOnlySpan<char>)s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out double result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return TryParse(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        private static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info, out double result)
        {
            return Number.TryParseDouble(s, style, info, out result);
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

#if FEATURE_GENERIC_MATH
        //
        // IAdditionOperators
        //

        [RequiresPreviewFeatures]
        static double IAdditionOperators<double, double, double>.operator +(double left, double right)
            => left + right;

        // [RequiresPreviewFeatures]
        // static checked double IAdditionOperators<double, double, double>.operator +(double left, double right)
        //     => checked(left + right);

        //
        // IAdditiveIdentity
        //

        [RequiresPreviewFeatures]
        static double IAdditiveIdentity<double, double>.AdditiveIdentity => 0.0;

        //
        // IBinaryNumber
        //

        [RequiresPreviewFeatures]
        static bool IBinaryNumber<double>.IsPow2(double value)
        {
            ulong bits = BitConverter.DoubleToUInt64Bits(value);

            uint exponent = (uint)(bits >> ExponentShift) & ShiftedExponentMask;
            ulong significand = bits & SignificandMask;

            return (value > 0)
                && (exponent != MinExponent) && (exponent != MaxExponent)
                && (significand == MinSignificand);
        }

        [RequiresPreviewFeatures]
        static double IBinaryNumber<double>.Log2(double value)
            => Math.Log2(value);

        //
        // IBitwiseOperators
        //

        [RequiresPreviewFeatures]
        static double IBitwiseOperators<double, double, double>.operator &(double left, double right)
        {
            ulong bits = BitConverter.DoubleToUInt64Bits(left) & BitConverter.DoubleToUInt64Bits(right);
            return BitConverter.UInt64BitsToDouble(bits);
        }

        [RequiresPreviewFeatures]
        static double IBitwiseOperators<double, double, double>.operator |(double left, double right)
        {
            ulong bits = BitConverter.DoubleToUInt64Bits(left) | BitConverter.DoubleToUInt64Bits(right);
            return BitConverter.UInt64BitsToDouble(bits);
        }

        [RequiresPreviewFeatures]
        static double IBitwiseOperators<double, double, double>.operator ^(double left, double right)
        {
            ulong bits = BitConverter.DoubleToUInt64Bits(left) ^ BitConverter.DoubleToUInt64Bits(right);
            return BitConverter.UInt64BitsToDouble(bits);
        }

        [RequiresPreviewFeatures]
        static double IBitwiseOperators<double, double, double>.operator ~(double value)
        {
            ulong bits = ~BitConverter.DoubleToUInt64Bits(value);
            return BitConverter.UInt64BitsToDouble(bits);
        }

        //
        // IComparisonOperators
        //

        [RequiresPreviewFeatures]
        static bool IComparisonOperators<double, double>.operator <(double left, double right)
            => left < right;

        [RequiresPreviewFeatures]
        static bool IComparisonOperators<double, double>.operator <=(double left, double right)
            => left <= right;

        [RequiresPreviewFeatures]
        static bool IComparisonOperators<double, double>.operator >(double left, double right)
            => left > right;

        [RequiresPreviewFeatures]
        static bool IComparisonOperators<double, double>.operator >=(double left, double right)
            => left >= right;

        //
        // IDecrementOperators
        //

        [RequiresPreviewFeatures]
        static double IDecrementOperators<double>.operator --(double value)
            => --value;

        // [RequiresPreviewFeatures]
        // static checked double IDecrementOperators<double>.operator --(double value)
        //     => checked(--value);

        //
        // IDivisionOperators
        //

        [RequiresPreviewFeatures]
        static double IDivisionOperators<double, double, double>.operator /(double left, double right)
            => left / right;

        // [RequiresPreviewFeatures]
        // static checked double IDivisionOperators<double, double, double>.operator /(double left, double right)
        //     => checked(left / right);

        //
        // IEqualityOperators
        //

        [RequiresPreviewFeatures]
        static bool IEqualityOperators<double, double>.operator ==(double left, double right)
            => left == right;

        [RequiresPreviewFeatures]
        static bool IEqualityOperators<double, double>.operator !=(double left, double right)
            => left != right;

        //
        // IFloatingPoint
        //

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.E => Math.E;

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.Epsilon => Epsilon;

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.NaN => NaN;

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.NegativeInfinity => NegativeInfinity;

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.NegativeZero => -0.0;

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.Pi => Math.PI;

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.PositiveInfinity => PositiveInfinity;

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.Tau => Math.Tau;

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.Acos(double x)
            => Math.Acos(x);

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.Acosh(double x)
            => Math.Acosh(x);

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.Asin(double x)
            => Math.Asin(x);

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.Asinh(double x)
            => Math.Asinh(x);

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.Atan(double x)
            => Math.Atan(x);

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.Atan2(double y, double x)
            => Math.Atan2(y, x);

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.Atanh(double x)
            => Math.Atanh(x);

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.BitIncrement(double x)
            => Math.BitIncrement(x);

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.BitDecrement(double x)
            => Math.BitDecrement(x);

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.Cbrt(double x)
            => Math.Cbrt(x);

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.Ceiling(double x)
            => Math.Ceiling(x);

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.CopySign(double x, double y)
            => Math.CopySign(x, y);

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.Cos(double x)
            => Math.Cos(x);

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.Cosh(double x)
            => Math.Cosh(x);

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.Exp(double x)
            => Math.Exp(x);

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.Floor(double x)
            => Math.Floor(x);

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.FusedMultiplyAdd(double left, double right, double addend)
            => Math.FusedMultiplyAdd(left, right, addend);

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.IEEERemainder(double left, double right)
            => Math.IEEERemainder(left, right);

        [RequiresPreviewFeatures]
        static TInteger IFloatingPoint<double>.ILogB<TInteger>(double x)
            => TInteger.Create(Math.ILogB(x));

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.Log(double x)
            => Math.Log(x);

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.Log(double x, double newBase)
            => Math.Log(x, newBase);

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.Log2(double x)
            => Math.Log2(x);

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.Log10(double x)
            => Math.Log10(x);

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.MaxMagnitude(double x, double y)
            => Math.MaxMagnitude(x, y);

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.MinMagnitude(double x, double y)
            => Math.MinMagnitude(x, y);

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.Pow(double x, double y)
            => Math.Pow(x, y);

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.Round(double x)
            => Math.Round(x);

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.Round<TInteger>(double x, TInteger digits)
            => Math.Round(x, int.Create(digits));

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.Round(double x, MidpointRounding mode)
            => Math.Round(x, mode);

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.Round<TInteger>(double x, TInteger digits, MidpointRounding mode)
            => Math.Round(x, int.Create(digits), mode);

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.ScaleB<TInteger>(double x, TInteger n)
            => Math.ScaleB(x, int.Create(n));

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.Sin(double x)
            => Math.Sin(x);

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.Sinh(double x)
            => Math.Sinh(x);

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.Sqrt(double x)
            => Math.Sqrt(x);

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.Tan(double x)
            => Math.Tan(x);

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.Tanh(double x)
            => Math.Tanh(x);

        [RequiresPreviewFeatures]
        static double IFloatingPoint<double>.Truncate(double x)
            => Math.Truncate(x);

        [RequiresPreviewFeatures]
        static bool IFloatingPoint<double>.IsFinite(double d) => IsFinite(d);

        [RequiresPreviewFeatures]
        static bool IFloatingPoint<double>.IsInfinity(double d) => IsInfinity(d);

        [RequiresPreviewFeatures]
        static bool IFloatingPoint<double>.IsNaN(double d) => IsNaN(d);

        [RequiresPreviewFeatures]
        static bool IFloatingPoint<double>.IsNegative(double d) => IsNegative(d);

        [RequiresPreviewFeatures]
        static bool IFloatingPoint<double>.IsNegativeInfinity(double d) => IsNegativeInfinity(d);

        [RequiresPreviewFeatures]
        static bool IFloatingPoint<double>.IsNormal(double d) => IsNormal(d);

        [RequiresPreviewFeatures]
        static bool IFloatingPoint<double>.IsPositiveInfinity(double d) => IsPositiveInfinity(d);

        [RequiresPreviewFeatures]
        static bool IFloatingPoint<double>.IsSubnormal(double d) => IsSubnormal(d);

        // static double IFloatingPoint<double>.AcosPi(double x)
        //     => Math.AcosPi(x);
        //
        // static double IFloatingPoint<double>.AsinPi(double x)
        //     => Math.AsinPi(x);
        //
        // static double IFloatingPoint<double>.AtanPi(double x)
        //     => Math.AtanPi(x);
        //
        // static double IFloatingPoint<double>.Atan2Pi(double y, double x)
        //     => Math.Atan2Pi(y, x);
        //
        // static double IFloatingPoint<double>.Compound(double x, double n)
        //     => Math.Compound(x, n);
        //
        // static double IFloatingPoint<double>.CosPi(double x)
        //     => Math.CosPi(x);
        //
        // static double IFloatingPoint<double>.ExpM1(double x)
        //     => Math.ExpM1(x);
        //
        // static double IFloatingPoint<double>.Exp2(double x)
        //     => Math.Exp2(x);
        //
        // static double IFloatingPoint<double>.Exp2M1(double x)
        //     => Math.Exp2M1(x);
        //
        // static double IFloatingPoint<double>.Exp10(double x)
        //     => Math.Exp10(x);
        //
        // static double IFloatingPoint<double>.Exp10M1(double x)
        //     => Math.Exp10M1(x);
        //
        // static double IFloatingPoint<double>.Hypot(double x, double y)
        //     => Math.Hypot(x, y);
        //
        // static double IFloatingPoint<double>.LogP1(double x)
        //     => Math.LogP1(x);
        //
        // static double IFloatingPoint<double>.Log2P1(double x)
        //     => Math.Log2P1(x);
        //
        // static double IFloatingPoint<double>.Log10P1(double x)
        //     => Math.Log10P1(x);
        //
        // static double IFloatingPoint<double>.MaxMagnitudeNumber(double x, double y)
        //     => Math.MaxMagnitudeNumber(x, y);
        //
        // static double IFloatingPoint<double>.MaxNumber(double x, double y)
        //     => Math.MaxNumber(x, y);
        //
        // static double IFloatingPoint<double>.MinMagnitudeNumber(double x, double y)
        //     => Math.MinMagnitudeNumber(x, y);
        //
        // static double IFloatingPoint<double>.MinNumber(double x, double y)
        //     => Math.MinNumber(x, y);
        //
        // static double IFloatingPoint<double>.Root(double x, double n)
        //     => Math.Root(x, n);
        //
        // static double IFloatingPoint<double>.SinPi(double x)
        //     => Math.SinPi(x, y);
        //
        // static double TanPi(double x)
        //     => Math.TanPi(x, y);

        //
        // IIncrementOperators
        //

        [RequiresPreviewFeatures]
        static double IIncrementOperators<double>.operator ++(double value)
            => ++value;

        // [RequiresPreviewFeatures]
        // static checked double IIncrementOperators<double>.operator ++(double value)
        //     => checked(++value);

        //
        // IMinMaxValue
        //

        [RequiresPreviewFeatures]
        static double IMinMaxValue<double>.MinValue => MinValue;

        [RequiresPreviewFeatures]
        static double IMinMaxValue<double>.MaxValue => MaxValue;

        //
        // IModulusOperators
        //

        [RequiresPreviewFeatures]
        static double IModulusOperators<double, double, double>.operator %(double left, double right)
            => left % right;

        // [RequiresPreviewFeatures]
        // static checked double IModulusOperators<double, double, double>.operator %(double left, double right)
        //     => checked(left % right);

        //
        // IMultiplicativeIdentity
        //

        [RequiresPreviewFeatures]
        static double IMultiplicativeIdentity<double, double>.MultiplicativeIdentity => 1.0;

        //
        // IMultiplyOperators
        //

        [RequiresPreviewFeatures]
        static double IMultiplyOperators<double, double, double>.operator *(double left, double right)
            => (double)(left * right);

        // [RequiresPreviewFeatures]
        // static checked double IMultiplyOperators<double, double, double>.operator *(double left, double right)
        //     => checked((double)(left * right));

        //
        // INumber
        //

        [RequiresPreviewFeatures]
        static double INumber<double>.One => 1.0;

        [RequiresPreviewFeatures]
        static double INumber<double>.Zero => 0.0;

        [RequiresPreviewFeatures]
        static double INumber<double>.Abs(double value)
            => Math.Abs(value);

        [RequiresPreviewFeatures]
        static double INumber<double>.Clamp(double value, double min, double max)
            => Math.Clamp(value, min, max);

        [RequiresPreviewFeatures]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static double INumber<double>.Create<TOther>(TOther value)
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
                return (double)(decimal)(object)value;
            }
            else if (typeof(TOther) == typeof(double))
            {
                return (double)(object)value;
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
        static double INumber<double>.CreateSaturating<TOther>(TOther value)
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
                return (double)(decimal)(object)value;
            }
            else if (typeof(TOther) == typeof(double))
            {
                return (double)(object)value;
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
        static double INumber<double>.CreateTruncating<TOther>(TOther value)
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
                return (double)(decimal)(object)value;
            }
            else if (typeof(TOther) == typeof(double))
            {
                return (double)(object)value;
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
        static (double Quotient, double Remainder) INumber<double>.DivRem(double left, double right)
            => (left / right, left % right);

        [RequiresPreviewFeatures]
        static double INumber<double>.Max(double x, double y)
            => Math.Max(x, y);

        [RequiresPreviewFeatures]
        static double INumber<double>.Min(double x, double y)
            => Math.Min(x, y);

        [RequiresPreviewFeatures]
        static double INumber<double>.Parse(string s, NumberStyles style, IFormatProvider? provider)
            => Parse(s, style, provider);

        [RequiresPreviewFeatures]
        static double INumber<double>.Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider)
            => Parse(s, style, provider);

        [RequiresPreviewFeatures]
        static double INumber<double>.Sign(double value)
            => Math.Sign(value);

        [RequiresPreviewFeatures]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumber<double>.TryCreate<TOther>(TOther value, out double result)
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
                result = (double)(decimal)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                result = (double)(object)value;
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
        static bool INumber<double>.TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out double result)
            => TryParse(s, style, provider, out result);

        [RequiresPreviewFeatures]
        static bool INumber<double>.TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out double result)
            => TryParse(s, style, provider, out result);

        //
        // IParseable
        //

        [RequiresPreviewFeatures]
        static double IParseable<double>.Parse(string s, IFormatProvider? provider)
            => Parse(s, provider);

        [RequiresPreviewFeatures]
        static bool IParseable<double>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out double result)
            => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // ISignedNumber
        //

        [RequiresPreviewFeatures]
        static double ISignedNumber<double>.NegativeOne => -1;

        //
        // ISpanParseable
        //

        [RequiresPreviewFeatures]
        static double ISpanParseable<double>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
            => Parse(s, NumberStyles.Integer, provider);

        [RequiresPreviewFeatures]
        static bool ISpanParseable<double>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out double result)
            => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // ISubtractionOperators
        //

        [RequiresPreviewFeatures]
        static double ISubtractionOperators<double, double, double>.operator -(double left, double right)
            => (double)(left - right);

        // [RequiresPreviewFeatures]
        // static checked double ISubtractionOperators<double, double, double>.operator -(double left, double right)
        //     => checked((double)(left - right));

        //
        // IUnaryNegationOperators
        //

        [RequiresPreviewFeatures]
        static double IUnaryNegationOperators<double, double>.operator -(double value)
            => (double)(-value);

        // [RequiresPreviewFeatures]
        // static checked double IUnaryNegationOperators<double, double>.operator -(double value)
        //     => checked((double)(-value));

        //
        // IUnaryNegationOperators
        //

        [RequiresPreviewFeatures]
        static double IUnaryPlusOperators<double, double>.operator +(double value)
            => (double)(+value);

        // [RequiresPreviewFeatures]
        // static checked double IUnaryPlusOperators<double, double>.operator +(double value)
        //     => checked((double)(+value));
#endif // FEATURE_GENERIC_MATH
    }
}
