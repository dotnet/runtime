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
    [StructLayout(LayoutKind.Sequential)]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public readonly struct Int16
        : IComparable,
          IConvertible,
          ISpanFormattable,
          IComparable<short>,
          IEquatable<short>,
          IBinaryInteger<short>,
          IMinMaxValue<short>,
          ISignedNumber<short>
    {
        private readonly short m_value; // Do not rename (binary serialization)

        public const short MaxValue = (short)0x7FFF;
        public const short MinValue = unchecked((short)0x8000);

        // Compares this object to another object, returning an integer that
        // indicates the relationship.
        // Returns a value less than zero if this  object
        // null is considered to be less than any instance.
        // If object is not of type Int16, this method throws an ArgumentException.
        //
        public int CompareTo(object? value)
        {
            if (value == null)
            {
                return 1;
            }

            if (value is short)
            {
                return m_value - ((short)value).m_value;
            }

            throw new ArgumentException(SR.Arg_MustBeInt16);
        }

        public int CompareTo(short value)
        {
            return m_value - value;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (!(obj is short))
            {
                return false;
            }
            return m_value == ((short)obj).m_value;
        }

        [NonVersionable]
        public bool Equals(short obj)
        {
            return m_value == obj;
        }

        // Returns a HashCode for the Int16
        public override int GetHashCode()
        {
            return m_value;
        }


        public override string ToString()
        {
            return Number.Int32ToDecStr(m_value);
        }

        public string ToString(IFormatProvider? provider)
        {
            return Number.FormatInt32(m_value, 0, null, provider);
        }

        public string ToString(string? format)
        {
            return ToString(format, null);
        }

        public string ToString(string? format, IFormatProvider? provider)
        {
            return Number.FormatInt32(m_value, 0x0000FFFF, format, provider);
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatInt32(m_value, 0x0000FFFF, format, provider, destination, out charsWritten);
        }

        public static short Parse(string s)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Parse((ReadOnlySpan<char>)s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);
        }

        public static short Parse(string s, NumberStyles style)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Parse((ReadOnlySpan<char>)s, style, NumberFormatInfo.CurrentInfo);
        }

        public static short Parse(string s, IFormatProvider? provider)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Parse((ReadOnlySpan<char>)s, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
        }

        public static short Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Parse((ReadOnlySpan<char>)s, style, NumberFormatInfo.GetInstance(provider));
        }

        public static short Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Parse(s, style, NumberFormatInfo.GetInstance(provider));
        }

        private static short Parse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info)
        {
            Number.ParsingStatus status = Number.TryParseInt32(s, style, info, out int i);
            if (status != Number.ParsingStatus.OK)
            {
                Number.ThrowOverflowOrFormatException(status, TypeCode.Int16);
            }

            // For hex number styles AllowHexSpecifier << 6 == 0x8000 and cancels out MinValue so the check is effectively: (uint)i > ushort.MaxValue
            // For integer styles it's zero and the effective check is (uint)(i - MinValue) > ushort.MaxValue
            if ((uint)(i - MinValue - ((int)(style & NumberStyles.AllowHexSpecifier) << 6)) > ushort.MaxValue)
            {
                Number.ThrowOverflowException(TypeCode.Int16);
            }
            return (short)i;
        }

        public static bool TryParse([NotNullWhen(true)] string? s, out short result)
        {
            if (s == null)
            {
                result = 0;
                return false;
            }

            return TryParse((ReadOnlySpan<char>)s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse(ReadOnlySpan<char> s, out short result)
        {
            return TryParse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out short result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);

            if (s == null)
            {
                result = 0;
                return false;
            }

            return TryParse((ReadOnlySpan<char>)s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out short result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return TryParse(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        private static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info, out short result)
        {
            // For hex number styles AllowHexSpecifier << 6 == 0x8000 and cancels out MinValue so the check is effectively: (uint)i > ushort.MaxValue
            // For integer styles it's zero and the effective check is (uint)(i - MinValue) > ushort.MaxValue
            if (Number.TryParseInt32(s, style, info, out int i) != Number.ParsingStatus.OK
                || (uint)(i - MinValue - ((int)(style & NumberStyles.AllowHexSpecifier) << 6)) > ushort.MaxValue)
            {
                result = 0;
                return false;
            }
            result = (short)i;
            return true;
        }

        //
        // IConvertible implementation
        //

        public TypeCode GetTypeCode()
        {
            return TypeCode.Int16;
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
            return m_value;
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
            return Convert.ToDouble(m_value);
        }

        decimal IConvertible.ToDecimal(IFormatProvider? provider)
        {
            return Convert.ToDecimal(m_value);
        }

        DateTime IConvertible.ToDateTime(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Int16", "DateTime"));
        }

        object IConvertible.ToType(Type type, IFormatProvider? provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }

        //
        // IAdditionOperators
        //

        static short IAdditionOperators<short, short, short>.operator +(short left, short right)
            => (short)(left + right);

        // static checked short IAdditionOperators<short, short, short>.operator +(short left, short right)
        //     => checked((short)(left + right));

        //
        // IAdditiveIdentity
        //

        static short IAdditiveIdentity<short, short>.AdditiveIdentity => 0;

        //
        // IBinaryInteger
        //

        static short IBinaryInteger<short>.LeadingZeroCount(short value)
            => (short)(BitOperations.LeadingZeroCount((ushort)value) - 16);

        static short IBinaryInteger<short>.PopCount(short value)
            => (short)BitOperations.PopCount((ushort)value);

        static short IBinaryInteger<short>.RotateLeft(short value, int rotateAmount)
            => (short)((value << (rotateAmount & 15)) | ((ushort)value >> ((16 - rotateAmount) & 15)));

        static short IBinaryInteger<short>.RotateRight(short value, int rotateAmount)
            => (short)(((ushort)value >> (rotateAmount & 15)) | (value << ((16 - rotateAmount) & 15)));

        static short IBinaryInteger<short>.TrailingZeroCount(short value)
            => (byte)(BitOperations.TrailingZeroCount(value << 16) - 16);

        //
        // IBinaryNumber
        //

        static bool IBinaryNumber<short>.IsPow2(short value)
            => BitOperations.IsPow2(value);

        static short IBinaryNumber<short>.Log2(short value)
        {
            if (value < 0)
            {
                ThrowHelper.ThrowValueArgumentOutOfRange_NeedNonNegNumException();
            }
            return (short)BitOperations.Log2((ushort)value);
        }

        //
        // IBitwiseOperators
        //

        static short IBitwiseOperators<short, short, short>.operator &(short left, short right)
            => (short)(left & right);

        static short IBitwiseOperators<short, short, short>.operator |(short left, short right)
            => (short)(left | right);

        static short IBitwiseOperators<short, short, short>.operator ^(short left, short right)
            => (short)(left ^ right);

        static short IBitwiseOperators<short, short, short>.operator ~(short value)
            => (short)(~value);

        //
        // IComparisonOperators
        //

        static bool IComparisonOperators<short, short>.operator <(short left, short right)
            => left < right;

        static bool IComparisonOperators<short, short>.operator <=(short left, short right)
            => left <= right;

        static bool IComparisonOperators<short, short>.operator >(short left, short right)
            => left > right;

        static bool IComparisonOperators<short, short>.operator >=(short left, short right)
            => left >= right;

        //
        // IDecrementOperators
        //

        static short IDecrementOperators<short>.operator --(short value)
            => --value;

        // static checked short IDecrementOperators<short>.operator --(short value)
        //     => checked(--value);

        //
        // IDivisionOperators
        //

        static short IDivisionOperators<short, short, short>.operator /(short left, short right)
            => (short)(left / right);

        // static checked short IDivisionOperators<short, short, short>.operator /(short left, short right)
        //     => checked((short)(left / right));

        //
        // IEqualityOperators
        //

        static bool IEqualityOperators<short, short>.operator ==(short left, short right)
            => left == right;

        static bool IEqualityOperators<short, short>.operator !=(short left, short right)
            => left != right;

        //
        // IIncrementOperators
        //

        static short IIncrementOperators<short>.operator ++(short value)
            => ++value;

        // static checked short IIncrementOperators<short>.operator ++(short value)
        //     => checked(++value);

        //
        // IMinMaxValue
        //

        static short IMinMaxValue<short>.MinValue => MinValue;

        static short IMinMaxValue<short>.MaxValue => MaxValue;

        //
        // IModulusOperators
        //

        static short IModulusOperators<short, short, short>.operator %(short left, short right)
            => (short)(left % right);

        // static checked short IModulusOperators<short, short, short>.operator %(short left, short right)
        //     => checked((short)(left % right));

        //
        // IMultiplicativeIdentity
        //

        static short IMultiplicativeIdentity<short, short>.MultiplicativeIdentity => 1;

        //
        // IMultiplyOperators
        //

        static short IMultiplyOperators<short, short, short>.operator *(short left, short right)
            => (short)(left * right);

        // static checked short IMultiplyOperators<short, short, short>.operator *(short left, short right)
        //     => checked((short)(left * right));

        //
        // INumber
        //

        static short INumber<short>.One => 1;

        static short INumber<short>.Zero => 0;

        static short INumber<short>.Abs(short value)
            => Math.Abs(value);

        static short INumber<short>.Clamp(short value, short min, short max)
            => Math.Clamp(value, min, max);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static short INumber<short>.Create<TOther>(TOther value)
        {
            if (typeof(TOther) == typeof(byte))
            {
                return (byte)(object)value;
            }
            else if (typeof(TOther) == typeof(char))
            {
                return checked((short)(char)(object)value);
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                return checked((short)(decimal)(object)value);
            }
            else if (typeof(TOther) == typeof(double))
            {
                return checked((short)(double)(object)value);
            }
            else if (typeof(TOther) == typeof(short))
            {
                return (short)(object)value;
            }
            else if (typeof(TOther) == typeof(int))
            {
                return checked((short)(int)(object)value);
            }
            else if (typeof(TOther) == typeof(long))
            {
                return checked((short)(long)(object)value);
            }
            else if (typeof(TOther) == typeof(nint))
            {
                return checked((short)(nint)(object)value);
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                return (sbyte)(object)value;
            }
            else if (typeof(TOther) == typeof(float))
            {
                return checked((short)(float)(object)value);
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                return checked((short)(ushort)(object)value);
            }
            else if (typeof(TOther) == typeof(uint))
            {
                return checked((short)(uint)(object)value);
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                return checked((short)(ulong)(object)value);
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                return checked((short)(nuint)(object)value);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static short INumber<short>.CreateSaturating<TOther>(TOther value)
        {
            if (typeof(TOther) == typeof(byte))
            {
                return (byte)(object)value;
            }
            else if (typeof(TOther) == typeof(char))
            {
                var actualValue = (char)(object)value;
                return (actualValue > MaxValue) ? MaxValue : (short)actualValue;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                var actualValue = (decimal)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < MinValue) ? MinValue : (short)actualValue;
            }
            else if (typeof(TOther) == typeof(double))
            {
                var actualValue = (double)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < MinValue) ? MinValue : (short)actualValue;
            }
            else if (typeof(TOther) == typeof(short))
            {
                return (short)(object)value;
            }
            else if (typeof(TOther) == typeof(int))
            {
                var actualValue = (int)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < MinValue) ? MinValue : (short)actualValue;
            }
            else if (typeof(TOther) == typeof(long))
            {
                var actualValue = (long)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < MinValue) ? MinValue : (short)actualValue;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                var actualValue = (nint)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < MinValue) ? MinValue : (short)actualValue;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                return (sbyte)(object)value;
            }
            else if (typeof(TOther) == typeof(float))
            {
                var actualValue = (float)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < MinValue) ? MinValue : (short)actualValue;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                var actualValue = (ushort)(object)value;
                return (actualValue > MaxValue) ? MaxValue : (short)actualValue;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                var actualValue = (uint)(object)value;
                return (actualValue > MaxValue) ? MaxValue : (short)actualValue;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                var actualValue = (ulong)(object)value;
                return (actualValue > (uint)MaxValue) ? MaxValue : (short)actualValue;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                var actualValue = (nuint)(object)value;
                return (actualValue > (uint)MaxValue) ? MaxValue : (short)actualValue;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static short INumber<short>.CreateTruncating<TOther>(TOther value)
        {
            if (typeof(TOther) == typeof(byte))
            {
                return (byte)(object)value;
            }
            else if (typeof(TOther) == typeof(char))
            {
                return (short)(char)(object)value;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                return (short)(decimal)(object)value;
            }
            else if (typeof(TOther) == typeof(double))
            {
                return (short)(double)(object)value;
            }
            else if (typeof(TOther) == typeof(short))
            {
                return (short)(object)value;
            }
            else if (typeof(TOther) == typeof(int))
            {
                return (short)(int)(object)value;
            }
            else if (typeof(TOther) == typeof(long))
            {
                return (short)(long)(object)value;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                return (short)(nint)(object)value;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                return (sbyte)(object)value;
            }
            else if (typeof(TOther) == typeof(float))
            {
                return (short)(float)(object)value;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                return (short)(ushort)(object)value;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                return (short)(uint)(object)value;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                return (short)(ulong)(object)value;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                return (short)(nuint)(object)value;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        static (short Quotient, short Remainder) INumber<short>.DivRem(short left, short right)
            => Math.DivRem(left, right);

        static short INumber<short>.Max(short x, short y)
            => Math.Max(x, y);

        static short INumber<short>.Min(short x, short y)
            => Math.Min(x, y);

        static short INumber<short>.Parse(string s, NumberStyles style, IFormatProvider? provider)
            => Parse(s, style, provider);

        static short INumber<short>.Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider)
            => Parse(s, style, provider);

        static short INumber<short>.Sign(short value)
            => (short)Math.Sign(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumber<short>.TryCreate<TOther>(TOther value, out short result)
        {
            if (typeof(TOther) == typeof(byte))
            {
                result = (byte)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                var actualValue = (char)(object)value;

                if (actualValue > MaxValue)
                {
                    result = default;
                    return false;
                }

                result = (short)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                var actualValue = (decimal)(object)value;

                if ((actualValue < MinValue) || (actualValue > MaxValue))
                {
                    result = default;
                    return false;
                }

                result = (short)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                var actualValue = (double)(object)value;

                if ((actualValue < MinValue) || (actualValue > MaxValue))
                {
                    result = default;
                    return false;
                }

                result = (short)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                result = (short)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                var actualValue = (int)(object)value;

                if ((actualValue < MinValue) || (actualValue > MaxValue))
                {
                    result = default;
                    return false;
                }

                result = (short)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                var actualValue = (long)(object)value;

                if ((actualValue < MinValue) || (actualValue > MaxValue))
                {
                    result = default;
                    return false;
                }

                result = (short)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                var actualValue = (nint)(object)value;

                if ((actualValue < MinValue) || (actualValue > MaxValue))
                {
                    result = default;
                    return false;
                }

                result = (short)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                result = (sbyte)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                var actualValue = (float)(object)value;

                if ((actualValue < MinValue) || (actualValue > MaxValue))
                {
                    result = default;
                    return false;
                }

                result = (short)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                var actualValue = (ushort)(object)value;

                if (actualValue > MaxValue)
                {
                    result = default;
                    return false;
                }

                result = (short)actualValue;
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

                result = (short)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                var actualValue = (ulong)(object)value;

                if (actualValue > (uint)MaxValue)
                {
                    result = default;
                    return false;
                }

                result = (short)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                var actualValue = (nuint)(object)value;

                if (actualValue > (uint)MaxValue)
                {
                    result = default;
                    return false;
                }

                result = (short)actualValue;
                return true;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                result = default;
                return false;
            }
        }

        static bool INumber<short>.TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out short result)
            => TryParse(s, style, provider, out result);

        static bool INumber<short>.TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out short result)
            => TryParse(s, style, provider, out result);

        //
        // IParseable
        //

        static short IParseable<short>.Parse(string s, IFormatProvider? provider)
            => Parse(s, provider);

        static bool IParseable<short>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out short result)
            => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // IShiftOperators
        //

        static short IShiftOperators<short, short>.operator <<(short value, int shiftAmount)
            => (short)(value << shiftAmount);

        static short IShiftOperators<short, short>.operator >>(short value, int shiftAmount)
            => (short)(value >> shiftAmount);

        // static short IShiftOperators<short, short>.operator >>>(short value, int shiftAmount)
        //     => (short)((ushort)value >> shiftAmount);

        //
        // ISignedNumber
        //

        static short ISignedNumber<short>.NegativeOne => -1;

        //
        // ISpanParseable
        //

        static short ISpanParseable<short>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
            => Parse(s, NumberStyles.Integer, provider);

        static bool ISpanParseable<short>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out short result)
            => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // ISubtractionOperators
        //

        static short ISubtractionOperators<short, short, short>.operator -(short left, short right)
            => (short)(left - right);

        // static checked short ISubtractionOperators<short, short, short>.operator -(short left, short right)
        //     => checked((short)(left - right));

        //
        // IUnaryNegationOperators
        //

        static short IUnaryNegationOperators<short, short>.operator -(short value)
            => (short)(-value);

        // static checked short IUnaryNegationOperators<short, short>.operator -(short value)
        //     => checked((short)(-value));

        //
        // IUnaryPlusOperators
        //

        static short IUnaryPlusOperators<short, short>.operator +(short value)
            => (short)(+value);

        // static checked short IUnaryPlusOperators<short, short>.operator +(short value)
        //     => checked((short)(+value));
    }
}
