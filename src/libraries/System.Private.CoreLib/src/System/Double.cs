﻿// Licensed to the .NET Foundation under one or more agreements.
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

namespace System
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public readonly struct Double
        : IComparable,
          IConvertible,
          ISpanFormattable,
          IComparable<double>,
          IEquatable<double>,
          IBinaryFloatingPoint<double>,
          IMinMaxValue<double>
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
        public const double AdditiveIdentity = 0.0;

        /// <summary>Represents the multiplicative identity (1).</summary>
        public const double MultiplicativeIdentity = 1.0;

        /// <summary>Represents the number one (1).</summary>
        public const double One = 1.0;

        /// <summary>Represents the number zero (0).</summary>
        public const double Zero = 0.0;

        /// <summary>Represents the number negative one (-1).</summary>
        public const double NegativeOne = -1.0;

        /// <summary>Represents the number negative zero (-0).</summary>
        public const double NegativeZero = -0.0;

        /// <summary>Represents the natural logarithmic base, specified by the constant, e.</summary>
        /// <remarks>Euler's number is approximately 2.7182818284590452354.</remarks>
        public const double E = Math.E;

        /// <summary>Represents the ratio of the circumference of a circle to its diameter, specified by the constant, π.</summary>
        /// <remarks>Pi is approximately 3.1415926535897932385.</remarks>
        public const double Pi = Math.PI;

        /// <summary>Represents the number of radians in one turn, specified by the constant, τ.</summary>
        /// <remarks>Tau is approximately 6.2831853071795864769.</remarks>
        public const double Tau = Math.Tau;

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

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther}.op_Equality(TSelf, TOther)" />
        [NonVersionable]
        public static bool operator ==(double left, double right) => left == right;

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther}.op_Inequality(TSelf, TOther)" />
        [NonVersionable]
        public static bool operator !=(double left, double right) => left != right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_LessThan(TSelf, TOther)" />
        [NonVersionable]
        public static bool operator <(double left, double right) => left < right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_GreaterThan(TSelf, TOther)" />
        [NonVersionable]
        public static bool operator >(double left, double right) => left > right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_LessThanOrEqual(TSelf, TOther)" />
        [NonVersionable]
        public static bool operator <=(double left, double right) => left <= right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_GreaterThanOrEqual(TSelf, TOther)" />
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

        //
        // IAdditionOperators
        //

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        static double IAdditionOperators<double, double, double>.operator +(double left, double right) => left + right;

        // /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        // static double IAdditionOperators<double, double, double>.operator checked +(double left, double right) => checked(left + right);

        //
        // IAdditiveIdentity
        //

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static double IAdditiveIdentity<double, double>.AdditiveIdentity => AdditiveIdentity;

        //
        // IBinaryNumber
        //

        /// <inheritdoc cref="IBinaryNumber{TSelf}.IsPow2(TSelf)" />
        public static bool IsPow2(double value)
        {
            ulong bits = BitConverter.DoubleToUInt64Bits(value);

            uint exponent = (uint)(bits >> ExponentShift) & ShiftedExponentMask;
            ulong significand = bits & SignificandMask;

            return (value > 0)
                && (exponent != MinExponent) && (exponent != MaxExponent)
                && (significand == MinSignificand);
        }

        /// <inheritdoc cref="IBinaryNumber{TSelf}.Log2(TSelf)" />
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

        // /// <inheritdoc cref="IDecrementOperators{TSelf}.op_CheckedDecrement(TSelf)" />
        // static double IDecrementOperators<double>.operator checked --(double value) => checked(--value);

        //
        // IDivisionOperators
        //

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_Division(TSelf, TOther)" />
        static double IDivisionOperators<double, double, double>.operator /(double left, double right) => left / right;

        // /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_CheckedDivision(TSelf, TOther)" />
        // static double IDivisionOperators<double, double, double>.operator checked /(double left, double right) => checked(left / right);

        //
        // IFloatingPoint
        //

        /// <inheritdoc cref="IFloatingPoint{TSelf}.E" />
        static double IFloatingPoint<double>.E => Math.E;

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Epsilon" />
        static double IFloatingPoint<double>.Epsilon => Epsilon;

        /// <inheritdoc cref="IFloatingPoint{TSelf}.NaN" />
        static double IFloatingPoint<double>.NaN => NaN;

        /// <inheritdoc cref="IFloatingPoint{TSelf}.NegativeInfinity" />
        static double IFloatingPoint<double>.NegativeInfinity => NegativeInfinity;

        /// <inheritdoc cref="IFloatingPoint{TSelf}.NegativeZero" />
        static double IFloatingPoint<double>.NegativeZero => NegativeZero;

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Pi" />
        static double IFloatingPoint<double>.Pi => Pi;

        /// <inheritdoc cref="IFloatingPoint{TSelf}.PositiveInfinity" />
        static double IFloatingPoint<double>.PositiveInfinity => PositiveInfinity;

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Tau" />
        static double IFloatingPoint<double>.Tau => Tau;

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Acos(TSelf)" />
        public static double Acos(double x) => Math.Acos(x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Acosh(TSelf)" />
        public static double Acosh(double x) => Math.Acosh(x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Asin(TSelf)" />
        public static double Asin(double x) => Math.Asin(x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Asinh(TSelf)" />
        public static double Asinh(double x) => Math.Asinh(x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Atan(TSelf)" />
        public static double Atan(double x) => Math.Atan(x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Atan2(TSelf, TSelf)" />
        public static double Atan2(double y, double x) => Math.Atan2(y, x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Atanh(TSelf)" />
        public static double Atanh(double x) => Math.Atanh(x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.BitIncrement(TSelf)" />
        public static double BitIncrement(double x) => Math.BitIncrement(x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.BitDecrement(TSelf)" />
        public static double BitDecrement(double x) => Math.BitDecrement(x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Cbrt(TSelf)" />
        public static double Cbrt(double x) => Math.Cbrt(x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Ceiling(TSelf)" />
        public static double Ceiling(double x) => Math.Ceiling(x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.CopySign(TSelf, TSelf)" />
        public static double CopySign(double x, double y) => Math.CopySign(x, y);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Cos(TSelf)" />
        public static double Cos(double x) => Math.Cos(x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Cosh(TSelf)" />
        public static double Cosh(double x) => Math.Cosh(x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Exp" />
        public static double Exp(double x) => Math.Exp(x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Floor(TSelf)" />
        public static double Floor(double x) => Math.Floor(x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.FusedMultiplyAdd(TSelf, TSelf, TSelf)" />
        public static double FusedMultiplyAdd(double left, double right, double addend) => Math.FusedMultiplyAdd(left, right, addend);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.IEEERemainder(TSelf, TSelf)" />
        public static double IEEERemainder(double left, double right) => Math.IEEERemainder(left, right);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.ILogB{TInteger}(TSelf)" />
        public static TInteger ILogB<TInteger>(double x)
            where TInteger : IBinaryInteger<TInteger> => TInteger.Create(Math.ILogB(x));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Log(TSelf)" />
        public static double Log(double x) => Math.Log(x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Log(TSelf, TSelf)" />
        public static double Log(double x, double newBase) => Math.Log(x, newBase);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Log10(TSelf)" />
        public static double Log10(double x) => Math.Log10(x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.MaxMagnitude(TSelf, TSelf)" />
        public static double MaxMagnitude(double x, double y) => Math.MaxMagnitude(x, y);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.MinMagnitude(TSelf, TSelf)" />
        public static double MinMagnitude(double x, double y) => Math.MinMagnitude(x, y);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Pow(TSelf, TSelf)" />
        public static double Pow(double x, double y) => Math.Pow(x, y);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf)" />
        public static double Round(double x) => Math.Round(x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round{TInteger}(TSelf, TInteger)" />
        public static double Round<TInteger>(double x, TInteger digits)
            where TInteger : IBinaryInteger<TInteger> => Math.Round(x, int.Create(digits));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, MidpointRounding)" />
        public static double Round(double x, MidpointRounding mode) => Math.Round(x, mode);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round{TInteger}(TSelf, TInteger, MidpointRounding)" />
        public static double Round<TInteger>(double x, TInteger digits, MidpointRounding mode)
            where TInteger : IBinaryInteger<TInteger> => Math.Round(x, int.Create(digits), mode);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.ScaleB{TInteger}(TSelf, TInteger)" />
        public static double ScaleB<TInteger>(double x, TInteger n)
            where TInteger : IBinaryInteger<TInteger> => Math.ScaleB(x, int.Create(n));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Sin(TSelf)" />
        public static double Sin(double x) => Math.Sin(x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Sinh(TSelf)" />
        public static double Sinh(double x) => Math.Sinh(x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Sqrt(TSelf)" />
        public static double Sqrt(double x) => Math.Sqrt(x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Tan(TSelf)" />
        public static double Tan(double x) => Math.Tan(x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Tanh(TSelf)" />
        public static double Tanh(double x) => Math.Tanh(x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Truncate(TSelf)" />
        public static double Truncate(double x) => Math.Truncate(x);

        // /// <inheritdoc cref="IFloatingPoint{TSelf}.AcosPi(TSelf)" />
        // public static double AcosPi(double x) => Math.AcosPi(x);

        // /// <inheritdoc cref="IFloatingPoint{TSelf}.AsinPi(TSelf)" />
        // public static double AsinPi(double x) => Math.AsinPi(x);

        // /// <inheritdoc cref="IFloatingPoint{TSelf}.AtanPi(TSelf)" />
        // public static double AtanPi(double x) => Math.AtanPi(x);

        // /// <inheritdoc cref="IFloatingPoint{TSelf}.Atan2Pi(TSelf)" />
        // public static double Atan2Pi(double y, double x) => Math.Atan2Pi(y, x);

        // /// <inheritdoc cref="IFloatingPoint{TSelf}.Compound(TSelf, TSelf)" />
        // public static double Compound(double x, double n) => Math.Compound(x, n);

        // /// <inheritdoc cref="IFloatingPoint{TSelf}.CosPi(TSelf)" />
        // public static double CosPi(double x) => Math.CosPi(x);

        // /// <inheritdoc cref="IFloatingPoint{TSelf}.ExpM1(TSelf)" />
        // public static double ExpM1(double x) => Math.ExpM1(x);

        // /// <inheritdoc cref="IFloatingPoint{TSelf}.Exp2(TSelf)" />
        // public static double Exp2(double x) => Math.Exp2(x);

        // /// <inheritdoc cref="IFloatingPoint{TSelf}.Exp2M1(TSelf)" />
        // public static double Exp2M1(double x) => Math.Exp2M1(x);

        // /// <inheritdoc cref="IFloatingPoint{TSelf}.Exp10(TSelf)" />
        // public static double Exp10(double x) => Math.Exp10(x);

        // /// <inheritdoc cref="IFloatingPoint{TSelf}.Exp10M1(TSelf)" />
        // public static double Exp10M1(double x) => Math.Exp10M1(x);

        // /// <inheritdoc cref="IFloatingPoint{TSelf}.Hypot(TSelf, TSelf)" />
        // public static double Hypot(double x, double y) => Math.Hypot(x, y);

        // /// <inheritdoc cref="IFloatingPoint{TSelf}.LogP1(TSelf)" />
        // public static double LogP1(double x) => Math.LogP1(x);

        // /// <inheritdoc cref="IFloatingPoint{TSelf}.Log2P1(TSelf)" />
        // public static double Log2P1(double x) => Math.Log2P1(x);

        // /// <inheritdoc cref="IFloatingPoint{TSelf}.Log10P1(TSelf)" />
        // public static double Log10P1(double x) => Math.Log10P1(x);

        // /// <inheritdoc cref="IFloatingPoint{TSelf}.MaxMagnitudeNumber(TSelf, TSelf)" />
        // public static double MaxMagnitudeNumber(double x, double y) => Math.MaxMagnitudeNumber(x, y);

        // /// <inheritdoc cref="IFloatingPoint{TSelf}.MaxNumber(TSelf, TSelf)" />
        // public static double MaxNumber(double x, double y) => Math.MaxNumber(x, y);

        // /// <inheritdoc cref="IFloatingPoint{TSelf}.MinMagnitudeNumber(TSelf, TSelf)" />
        // public static double MinMagnitudeNumber(double x, double y) => Math.MinMagnitudeNumber(x, y);

        // /// <inheritdoc cref="IFloatingPoint{TSelf}.MinNumber(TSelf, TSelf)" />
        // public static double MinNumber(double x, double y) => Math.MinNumber(x, y);

        // /// <inheritdoc cref="IFloatingPoint{TSelf}.Root(TSelf, TSelf)" />
        // public static double Root(double x, double n) => Math.Root(x, n);

        // /// <inheritdoc cref="IFloatingPoint{TSelf}.SinPi(TSelf)" />
        // public static double SinPi(double x) => Math.SinPi(x, y);

        // /// <inheritdoc cref="IFloatingPoint{TSelf}.TanPi(TSelf)" />
        // public static double TanPi(double x) => Math.TanPi(x, y);

        //
        // IIncrementOperators
        //

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_Increment(TSelf)" />
        static double IIncrementOperators<double>.operator ++(double value) => ++value;

        // /// <inheritdoc cref="IIncrementOperators{TSelf}.op_CheckedIncrement(TSelf)" />
        // static double IIncrementOperators<double>.operator checked ++(double value) => checked(++value);

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

        // static checked double IModulusOperators<double, double, double>.operator %(double left, double right) => checked(left % right);

        //
        // IMultiplicativeIdentity
        //

        /// <inheritdoc cref="IMultiplicativeIdentity{TSelf, TResult}.MultiplicativeIdentity" />
        static double IMultiplicativeIdentity<double, double>.MultiplicativeIdentity => MultiplicativeIdentity;

        //
        // IMultiplyOperators
        //

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_Multiply(TSelf, TOther)" />
        static double IMultiplyOperators<double, double, double>.operator *(double left, double right) => (double)(left * right);

        // /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_CheckedMultiply(TSelf, TOther)" />
        // static double IMultiplyOperators<double, double, double>.operator checked *(double left, double right) => checked((double)(left * right));

        //
        // INumber
        //

        /// <inheritdoc cref="INumber{TSelf}.One" />
        static double INumber<double>.One => One;

        /// <inheritdoc cref="INumber{TSelf}.Zero" />
        static double INumber<double>.Zero => Zero;

        /// <inheritdoc cref="INumber{TSelf}.Abs(TSelf)" />
        public static double Abs(double value) => Math.Abs(value);

        /// <inheritdoc cref="INumber{TSelf}.Clamp(TSelf, TSelf, TSelf)" />
        public static double Clamp(double value, double min, double max) => Math.Clamp(value, min, max);

        /// <inheritdoc cref="INumber{TSelf}.Create{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Create<TOther>(TOther value)
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

        /// <inheritdoc cref="INumber{TSelf}.CreateSaturating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double CreateSaturating<TOther>(TOther value)
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

        /// <inheritdoc cref="INumber{TSelf}.CreateTruncating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double CreateTruncating<TOther>(TOther value)
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

        /// <inheritdoc cref="INumber{TSelf}.DivRem(TSelf, TSelf)" />
        public static (double Quotient, double Remainder) DivRem(double left, double right) => (left / right, left % right);

        /// <inheritdoc cref="INumber{TSelf}.Max(TSelf, TSelf)" />
        public static double Max(double x, double y) => Math.Max(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Min(TSelf, TSelf)" />
        public static double Min(double x, double y) => Math.Min(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Sign(TSelf)" />
        public static double Sign(double value) => Math.Sign(value);

        /// <inheritdoc cref="INumber{TSelf}.TryCreate{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryCreate<TOther>(TOther value, out double result)
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

        //
        // IParseable
        //

        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out double result) => TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);

        //
        // ISignedNumber
        //

        /// <inheritdoc cref="ISignedNumber{TSelf}.NegativeOne" />
        static double ISignedNumber<double>.NegativeOne => NegativeOne;

        //
        // ISpanParseable
        //

        /// <inheritdoc cref="ISpanParseable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static double Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider);

        /// <inheritdoc cref="ISpanParseable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out double result) => TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);

        //
        // ISubtractionOperators
        //

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_Subtraction(TSelf, TOther)" />
        static double ISubtractionOperators<double, double, double>.operator -(double left, double right) => (double)(left - right);

        // /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_CheckedSubtraction(TSelf, TOther)" />
        // static double ISubtractionOperators<double, double, double>.operator checked -(double left, double right) => checked((double)(left - right));

        //
        // IUnaryNegationOperators
        //

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_UnaryNegation(TSelf)" />
        static double IUnaryNegationOperators<double, double>.operator -(double value) => (double)(-value);

        // /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_CheckedUnaryNegation(TSelf)" />
        // static double IUnaryNegationOperators<double, double>.operator checked -(double value) => checked((double)(-value));

        //
        // IUnaryPlusOperators
        //

        /// <inheritdoc cref="IUnaryPlusOperators{TSelf, TResult}.op_UnaryPlus(TSelf)" />
        static double IUnaryPlusOperators<double, double>.operator +(double value) => (double)(+value);

        // /// <inheritdoc cref="IUnaryPlusOperators{TSelf, TResult}.op_CheckedUnaryPlus(TSelf)" />
        // static double IUnaryPlusOperators<double, double>.operator checked +(double value) => checked((double)(+value));
    }
}
