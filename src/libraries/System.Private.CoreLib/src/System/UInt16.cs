// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System
{
    [Serializable]
    [CLSCompliant(false)]
    [StructLayout(LayoutKind.Sequential)]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public readonly struct UInt16
        : IComparable,
          IConvertible,
          ISpanFormattable,
          IComparable<ushort>,
          IEquatable<ushort>,
          IBinaryInteger<ushort>,
          IMinMaxValue<ushort>,
          IUnsignedNumber<ushort>
    {
        private readonly ushort m_value; // Do not rename (binary serialization)

        public const ushort MaxValue = (ushort)0xFFFF;
        public const ushort MinValue = 0;

        /// <summary>Represents the additive identity (0).</summary>
        public const ushort AdditiveIdentity = 0;

        /// <summary>Represents the multiplicative identity (1).</summary>
        public const ushort MultiplicativeIdentity = 1;

        /// <summary>Represents the number one (1).</summary>
        public const ushort One = 1;

        /// <summary>Represents the number zero (0).</summary>
        public const ushort Zero = 0;

        // Compares this object to another object, returning an integer that
        // indicates the relationship.
        // Returns a value less than zero if this  object
        // null is considered to be less than any instance.
        // If object is not of type UInt16, this method throws an ArgumentException.
        //
        public int CompareTo(object? value)
        {
            if (value == null)
            {
                return 1;
            }
            if (value is ushort)
            {
                return (int)m_value - (int)(((ushort)value).m_value);
            }
            throw new ArgumentException(SR.Arg_MustBeUInt16);
        }

        public int CompareTo(ushort value)
        {
            return (int)m_value - (int)value;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (!(obj is ushort))
            {
                return false;
            }
            return m_value == ((ushort)obj).m_value;
        }

        [NonVersionable]
        public bool Equals(ushort obj)
        {
            return m_value == obj;
        }

        // Returns a HashCode for the UInt16
        public override int GetHashCode()
        {
            return (int)m_value;
        }

        // Converts the current value to a String in base-10 with no extra padding.
        public override string ToString()
        {
            return Number.UInt32ToDecStr(m_value);
        }

        public string ToString(IFormatProvider? provider)
        {
            return Number.UInt32ToDecStr(m_value);
        }

        public string ToString(string? format)
        {
            return Number.FormatUInt32(m_value, format, null);
        }

        public string ToString(string? format, IFormatProvider? provider)
        {
            return Number.FormatUInt32(m_value, format, provider);
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatUInt32(m_value, format, provider, destination, out charsWritten);
        }

        public static ushort Parse(string s)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Parse((ReadOnlySpan<char>)s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);
        }

        public static ushort Parse(string s, NumberStyles style)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Parse((ReadOnlySpan<char>)s, style, NumberFormatInfo.CurrentInfo);
        }

        public static ushort Parse(string s, IFormatProvider? provider)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Parse((ReadOnlySpan<char>)s, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
        }

        public static ushort Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Parse((ReadOnlySpan<char>)s, style, NumberFormatInfo.GetInstance(provider));
        }

        public static ushort Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Parse(s, style, NumberFormatInfo.GetInstance(provider));
        }

        private static ushort Parse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info)
        {
            Number.ParsingStatus status = Number.TryParseUInt32(s, style, info, out uint i);
            if (status != Number.ParsingStatus.OK)
            {
                Number.ThrowOverflowOrFormatException(status, TypeCode.UInt16);
            }

            if (i > MaxValue) Number.ThrowOverflowException(TypeCode.UInt16);
            return (ushort)i;
        }

        public static bool TryParse([NotNullWhen(true)] string? s, out ushort result)
        {
            if (s == null)
            {
                result = 0;
                return false;
            }

            return TryParse((ReadOnlySpan<char>)s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse(ReadOnlySpan<char> s, out ushort result)
        {
            return TryParse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out ushort result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);

            if (s == null)
            {
                result = 0;
                return false;
            }

            return TryParse((ReadOnlySpan<char>)s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out ushort result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return TryParse(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        private static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info, out ushort result)
        {
            if (Number.TryParseUInt32(s, style, info, out uint i) != Number.ParsingStatus.OK
                || i > MaxValue)
            {
                result = 0;
                return false;
            }
            result = (ushort)i;
            return true;
        }

        //
        // IConvertible implementation
        //

        public TypeCode GetTypeCode()
        {
            return TypeCode.UInt16;
        }

        bool IConvertible.ToBoolean(IFormatProvider? provider)
        {
            return Convert.ToBoolean(m_value);
        }

        char IConvertible.ToChar(IFormatProvider? provider)
        {
            return Convert.ToChar(m_value);
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
            return m_value;
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
            return Convert.ToDouble(m_value);
        }

        decimal IConvertible.ToDecimal(IFormatProvider? provider)
        {
            return Convert.ToDecimal(m_value);
        }

        DateTime IConvertible.ToDateTime(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "UInt16", "DateTime"));
        }

        object IConvertible.ToType(Type type, IFormatProvider? provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }

        //
        // IAdditionOperators
        //

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        static ushort IAdditionOperators<ushort, ushort, ushort>.operator +(ushort left, ushort right) => (ushort)(left + right);

        // /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        // static ushort IAdditionOperators<ushort, ushort, ushort>.operator checked +(ushort left, ushort right) => checked((ushort)(left + right));

        //
        // IAdditiveIdentity
        //

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static ushort IAdditiveIdentity<ushort, ushort>.AdditiveIdentity => AdditiveIdentity;

        //
        // IBinaryInteger
        //

        /// <inheritdoc cref="IBinaryInteger{TSelf}.LeadingZeroCount(TSelf)" />
        public static ushort LeadingZeroCount(ushort value) => (ushort)(BitOperations.LeadingZeroCount(value) - 16);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.PopCount(TSelf)" />
        public static ushort PopCount(ushort value) => (ushort)BitOperations.PopCount(value);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.RotateLeft(TSelf, int)" />
        public static ushort RotateLeft(ushort value, int rotateAmount) => (ushort)((value << (rotateAmount & 15)) | (value >> ((16 - rotateAmount) & 15)));

        /// <inheritdoc cref="IBinaryInteger{TSelf}.RotateRight(TSelf, int)" />
        public static ushort RotateRight(ushort value, int rotateAmount) => (ushort)((value >> (rotateAmount & 15)) | (value << ((16 - rotateAmount) & 15)));

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TrailingZeroCount(TSelf)" />
        public static ushort TrailingZeroCount(ushort value) => (ushort)(BitOperations.TrailingZeroCount(value << 16) - 16);

        //
        // IBinaryNumber
        //

        /// <inheritdoc cref="IBinaryNumber{TSelf}.IsPow2(TSelf)" />
        public static bool IsPow2(ushort value) => BitOperations.IsPow2((uint)value);

        /// <inheritdoc cref="IBinaryNumber{TSelf}.Log2(TSelf)" />
        public static ushort Log2(ushort value) => (ushort)BitOperations.Log2(value);

        //
        // IBitwiseOperators
        //

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseAnd(TSelf, TOther)" />
        static ushort IBitwiseOperators<ushort, ushort, ushort>.operator &(ushort left, ushort right) => (ushort)(left & right);

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseOr(TSelf, TOther)" />
        static ushort IBitwiseOperators<ushort, ushort, ushort>.operator |(ushort left, ushort right) => (ushort)(left | right);

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_ExclusiveOr(TSelf, TOther)" />
        static ushort IBitwiseOperators<ushort, ushort, ushort>.operator ^(ushort left, ushort right) => (ushort)(left ^ right);

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_OnesComplement(TSelf)" />
        static ushort IBitwiseOperators<ushort, ushort, ushort>.operator ~(ushort value) => (ushort)(~value);

        //
        // IComparisonOperators
        //

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_LessThan(TSelf, TOther)" />
        static bool IComparisonOperators<ushort, ushort>.operator <(ushort left, ushort right) => left < right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_LessThanOrEqual(TSelf, TOther)" />
        static bool IComparisonOperators<ushort, ushort>.operator <=(ushort left, ushort right) => left <= right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_GreaterThan(TSelf, TOther)" />
        static bool IComparisonOperators<ushort, ushort>.operator >(ushort left, ushort right) => left > right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_GreaterThanOrEqual(TSelf, TOther)" />
        static bool IComparisonOperators<ushort, ushort>.operator >=(ushort left, ushort right) => left >= right;

        //
        // IDecrementOperators
        //

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        static ushort IDecrementOperators<ushort>.operator --(ushort value) => --value;

        // /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        // static ushort IDecrementOperators<ushort>.operator checked --(ushort value) => checked(--value);

        //
        // IDivisionOperators
        //

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_Division(TSelf, TOther)" />
        static ushort IDivisionOperators<ushort, ushort, ushort>.operator /(ushort left, ushort right) => (ushort)(left / right);

        // /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_CheckedDivision(TSelf, TOther)" />
        // static ushort IDivisionOperators<ushort, ushort, ushort>.operator checked /(ushort left, ushort right) => checked((ushort)(left / right));

        //
        // IEqualityOperators
        //

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther}.op_Equality(TSelf, TOther)" />
        static bool IEqualityOperators<ushort, ushort>.operator ==(ushort left, ushort right) => left == right;

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther}.op_Inequality(TSelf, TOther)" />
        static bool IEqualityOperators<ushort, ushort>.operator !=(ushort left, ushort right) => left != right;

        //
        // IIncrementOperators
        //

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_Increment(TSelf)" />
        static ushort IIncrementOperators<ushort>.operator ++(ushort value) => ++value;

        // /// <inheritdoc cref="IIncrementOperators{TSelf}.op_CheckedIncrement(TSelf)" />
        // static ushort IIncrementOperators<ushort>.operator checked ++(ushort value) => checked(++value);

        //
        // IMinMaxValue
        //

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MinValue" />
        static ushort IMinMaxValue<ushort>.MinValue => MinValue;

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MaxValue" />
        static ushort IMinMaxValue<ushort>.MaxValue => MaxValue;

        //
        // IModulusOperators
        //

        /// <inheritdoc cref="IModulusOperators{TSelf, TOther, TResult}.op_Modulus(TSelf, TOther)" />
        static ushort IModulusOperators<ushort, ushort, ushort>.operator %(ushort left, ushort right) => (ushort)(left % right);

        //
        // IMultiplicativeIdentity
        //

        /// <inheritdoc cref="IMultiplicativeIdentity{TSelf, TResult}.MultiplicativeIdentity" />
        static ushort IMultiplicativeIdentity<ushort, ushort>.MultiplicativeIdentity => MultiplicativeIdentity;

        //
        // IMultiplyOperators
        //

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_Multiply(TSelf, TOther)" />
        static ushort IMultiplyOperators<ushort, ushort, ushort>.operator *(ushort left, ushort right) => (ushort)(left * right);

        // /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_CheckedMultiply(TSelf, TOther)" />
        // static ushort IMultiplyOperators<ushort, ushort, ushort>.operator checked *(ushort left, ushort right) => checked((ushort)(left * right));

        //
        // INumber
        //

        /// <inheritdoc cref="INumber{TSelf}.One" />
        static ushort INumber<ushort>.One => One;

        /// <inheritdoc cref="INumber{TSelf}.Zero" />
        static ushort INumber<ushort>.Zero => Zero;

        /// <inheritdoc cref="INumber{TSelf}.Abs(TSelf)" />
        public static ushort Abs(ushort value) => value;

        /// <inheritdoc cref="INumber{TSelf}.Clamp(TSelf, TSelf, TSelf)" />
        public static ushort Clamp(ushort value, ushort min, ushort max) => Math.Clamp(value, min, max);

        /// <inheritdoc cref="INumber{TSelf}.Create{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Create<TOther>(TOther value)
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
                return checked((ushort)(decimal)(object)value);
            }
            else if (typeof(TOther) == typeof(double))
            {
                return checked((ushort)(double)(object)value);
            }
            else if (typeof(TOther) == typeof(short))
            {
                return checked((ushort)(short)(object)value);
            }
            else if (typeof(TOther) == typeof(int))
            {
                return checked((ushort)(int)(object)value);
            }
            else if (typeof(TOther) == typeof(long))
            {
                return checked((ushort)(long)(object)value);
            }
            else if (typeof(TOther) == typeof(nint))
            {
                return checked((ushort)(nint)(object)value);
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                return checked((ushort)(sbyte)(object)value);
            }
            else if (typeof(TOther) == typeof(float))
            {
                return checked((ushort)(float)(object)value);
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                return (ushort)(object)value;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                return checked((ushort)(uint)(object)value);
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                return checked((ushort)(ulong)(object)value);
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                return checked((ushort)(nuint)(object)value);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        /// <inheritdoc cref="INumber{TSelf}.CreateSaturating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort CreateSaturating<TOther>(TOther value)
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
                var actualValue = (decimal)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < 0) ? MinValue : (ushort)actualValue;
            }
            else if (typeof(TOther) == typeof(double))
            {
                var actualValue = (double)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < 0) ? MinValue : (ushort)actualValue;
            }
            else if (typeof(TOther) == typeof(short))
            {
                var actualValue = (short)(object)value;
                return (actualValue < 0) ? MinValue : (ushort)actualValue;
            }
            else if (typeof(TOther) == typeof(int))
            {
                var actualValue = (int)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < 0) ? MinValue : (ushort)actualValue;
            }
            else if (typeof(TOther) == typeof(long))
            {
                var actualValue = (long)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < 0) ? MinValue : (ushort)actualValue;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                var actualValue = (nint)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < 0) ? MinValue : (ushort)actualValue;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                var actualValue = (sbyte)(object)value;
                return (actualValue < 0) ? MinValue : (ushort)actualValue;
            }
            else if (typeof(TOther) == typeof(float))
            {
                var actualValue = (float)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < 0) ? MinValue : (ushort)actualValue;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                return (ushort)(object)value;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                var actualValue = (uint)(object)value;
                return (actualValue > MaxValue) ? MaxValue : (ushort)actualValue;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                var actualValue = (ulong)(object)value;
                return (actualValue > MaxValue) ? MaxValue : (ushort)actualValue;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                var actualValue = (nuint)(object)value;
                return (actualValue > MaxValue) ? MaxValue : (ushort)actualValue;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        /// <inheritdoc cref="INumber{TSelf}.CreateTruncating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort CreateTruncating<TOther>(TOther value)
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
                return (ushort)(decimal)(object)value;
            }
            else if (typeof(TOther) == typeof(double))
            {
                return (ushort)(double)(object)value;
            }
            else if (typeof(TOther) == typeof(short))
            {
                return (ushort)(short)(object)value;
            }
            else if (typeof(TOther) == typeof(int))
            {
                return (ushort)(int)(object)value;
            }
            else if (typeof(TOther) == typeof(long))
            {
                return (ushort)(long)(object)value;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                return (ushort)(nint)(object)value;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                return (ushort)(sbyte)(object)value;
            }
            else if (typeof(TOther) == typeof(float))
            {
                return (ushort)(float)(object)value;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                return (ushort)(object)value;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                return (ushort)(uint)(object)value;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                return (ushort)(ulong)(object)value;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                return (ushort)(nuint)(object)value;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        /// <inheritdoc cref="INumber{TSelf}.DivRem(TSelf, TSelf)" />
        public static (ushort Quotient, ushort Remainder) DivRem(ushort left, ushort right) => Math.DivRem(left, right);

        /// <inheritdoc cref="INumber{TSelf}.Max(TSelf, TSelf)" />
        public static ushort Max(ushort x, ushort y) => Math.Max(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Min(TSelf, TSelf)" />
        public static ushort Min(ushort x, ushort y) => Math.Min(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Sign(TSelf)" />
        public static ushort Sign(ushort value) => (ushort)((value == 0) ? 0 : 1);

        /// <inheritdoc cref="INumber{TSelf}.TryCreate{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryCreate<TOther>(TOther value, out ushort result)
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
                var actualValue = (decimal)(object)value;

                if ((actualValue < 0) || (actualValue > MaxValue))
                {
                    result = default;
                    return false;
                }

                result = (ushort)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                var actualValue = (double)(object)value;

                if ((actualValue < 0) || (actualValue > MaxValue))
                {
                    result = default;
                    return false;
                }

                result = (ushort)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                var actualValue = (short)(object)value;

                if (actualValue < 0)
                {
                    result = default;
                    return false;
                }

                result = (ushort)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                var actualValue = (int)(object)value;

                if ((actualValue < 0) || (actualValue > MaxValue))
                {
                    result = default;
                    return false;
                }

                result = (ushort)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                var actualValue = (long)(object)value;

                if ((actualValue < 0) || (actualValue > MaxValue))
                {
                    result = default;
                    return false;
                }

                result = (ushort)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                var actualValue = (nint)(object)value;

                if ((actualValue < 0) || (actualValue > MaxValue))
                {
                    result = default;
                    return false;
                }

                result = (ushort)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                var actualValue = (sbyte)(object)value;

                if (actualValue < 0)
                {
                    result = default;
                    return false;
                }

                result = (ushort)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                var actualValue = (float)(object)value;

                if ((actualValue < 0) || (actualValue > MaxValue))
                {
                    result = default;
                    return false;
                }

                result = (ushort)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                result = (ushort)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                var actualValue = (uint)(object)value;

                if (actualValue > MaxValue)
                {
                    result = default;
                    return false;
                }

                result = (ushort)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                var actualValue = (ulong)(object)value;

                if (actualValue > MaxValue)
                {
                    result = default;
                    return false;
                }

                result = (ushort)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                var actualValue = (nuint)(object)value;

                if (actualValue > MaxValue)
                {
                    result = default;
                    return false;
                }

                result = (ushort)actualValue;
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

        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out ushort result) => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // IShiftOperators
        //

        /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_LeftShift(TSelf, int)" />
        static ushort IShiftOperators<ushort, ushort>.operator <<(ushort value, int shiftAmount) => (ushort)(value << shiftAmount);

        /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_RightShift(TSelf, int)" />
        static ushort IShiftOperators<ushort, ushort>.operator >>(ushort value, int shiftAmount) => (ushort)(value >> shiftAmount);

        // /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_UnsignedRightShift(TSelf, int)" />
        // static ushort IShiftOperators<ushort, ushort>.operator >>>(ushort value, int shiftAmount) => (ushort)(value >> shiftAmount);

        //
        // ISpanParseable
        //

        /// <inheritdoc cref="ISpanParseable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static ushort Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Integer, provider);

        /// <inheritdoc cref="ISpanParseable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out ushort result) => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // ISubtractionOperators
        //

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_Subtraction(TSelf, TOther)" />
        static ushort ISubtractionOperators<ushort, ushort, ushort>.operator -(ushort left, ushort right) => (ushort)(left - right);

        // /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_CheckedSubtraction(TSelf, TOther)" />
        // static ushort ISubtractionOperators<ushort, ushort, ushort>.operator checked -(ushort left, ushort right) => checked((ushort)(left - right));

        //
        // IUnaryNegationOperators
        //

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_UnaryNegation(TSelf)" />
        static ushort IUnaryNegationOperators<ushort, ushort>.operator -(ushort value) => (ushort)(-value);

        // /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_CheckedUnaryNegation(TSelf)" />
        // static ushort IUnaryNegationOperators<ushort, ushort>.operator checked -(ushort value) => checked((ushort)(-value));

        //
        // IUnaryPlusOperators
        //

        /// <inheritdoc cref="IUnaryPlusOperators{TSelf, TResult}.op_UnaryPlus(TSelf)" />
        static ushort IUnaryPlusOperators<ushort, ushort>.operator +(ushort value) => (ushort)(+value);

        // /// <inheritdoc cref="IUnaryPlusOperators{TSelf, TResult}.op_CheckedUnaryPlus(TSelf)" />
        // static ushort IUnaryPlusOperators<ushort, ushort>.operator checked +(ushort value) => checked((ushort)(+value));
    }
}
