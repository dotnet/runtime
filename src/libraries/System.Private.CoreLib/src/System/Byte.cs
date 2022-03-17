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
    public readonly struct Byte
        : IComparable,
          IConvertible,
          ISpanFormattable,
          IComparable<byte>,
          IEquatable<byte>,
          IBinaryInteger<byte>,
          IMinMaxValue<byte>,
          IUnsignedNumber<byte>
    {
        private readonly byte m_value; // Do not rename (binary serialization)

        // The maximum value that a Byte may represent: 255.
        public const byte MaxValue = (byte)0xFF;

        // The minimum value that a Byte may represent: 0.
        public const byte MinValue = 0;

        // Compares this object to another object, returning an integer that
        // indicates the relationship.
        // Returns a value less than zero if this  object
        // null is considered to be less than any instance.
        // If object is not of type byte, this method throws an ArgumentException.
        //
        public int CompareTo(object? value)
        {
            if (value == null)
            {
                return 1;
            }
            if (!(value is byte))
            {
                throw new ArgumentException(SR.Arg_MustBeByte);
            }

            return m_value - (((byte)value).m_value);
        }

        public int CompareTo(byte value)
        {
            return m_value - value;
        }

        // Determines whether two Byte objects are equal.
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (!(obj is byte))
            {
                return false;
            }
            return m_value == ((byte)obj).m_value;
        }

        [NonVersionable]
        public bool Equals(byte obj)
        {
            return m_value == obj;
        }

        // Gets a hash code for this instance.
        public override int GetHashCode()
        {
            return m_value;
        }

        public static byte Parse(string s)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Parse((ReadOnlySpan<char>)s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);
        }

        public static byte Parse(string s, NumberStyles style)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Parse((ReadOnlySpan<char>)s, style, NumberFormatInfo.CurrentInfo);
        }

        public static byte Parse(string s, IFormatProvider? provider)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Parse((ReadOnlySpan<char>)s, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
        }

        // Parses an unsigned byte from a String in the given style.  If
        // a NumberFormatInfo isn't specified, the current culture's
        // NumberFormatInfo is assumed.
        public static byte Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Parse((ReadOnlySpan<char>)s, style, NumberFormatInfo.GetInstance(provider));
        }

        public static byte Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Parse(s, style, NumberFormatInfo.GetInstance(provider));
        }

        private static byte Parse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info)
        {
            Number.ParsingStatus status = Number.TryParseUInt32(s, style, info, out uint i);
            if (status != Number.ParsingStatus.OK)
            {
                Number.ThrowOverflowOrFormatException(status, TypeCode.Byte);
            }

            if (i > MaxValue) Number.ThrowOverflowException(TypeCode.Byte);
            return (byte)i;
        }

        public static bool TryParse([NotNullWhen(true)] string? s, out byte result)
        {
            if (s == null)
            {
                result = 0;
                return false;
            }

            return TryParse((ReadOnlySpan<char>)s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse(ReadOnlySpan<char> s, out byte result)
        {
            return TryParse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out byte result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);

            if (s == null)
            {
                result = 0;
                return false;
            }

            return TryParse((ReadOnlySpan<char>)s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out byte result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return TryParse(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        private static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info, out byte result)
        {
            if (Number.TryParseUInt32(s, style, info, out uint i) != Number.ParsingStatus.OK
                || i > MaxValue)
            {
                result = 0;
                return false;
            }
            result = (byte)i;
            return true;
        }

        public override string ToString()
        {
            return Number.UInt32ToDecStr(m_value);
        }

        public string ToString(string? format)
        {
            return Number.FormatUInt32(m_value, format, null);
        }

        public string ToString(IFormatProvider? provider)
        {
            return Number.UInt32ToDecStr(m_value);
        }

        public string ToString(string? format, IFormatProvider? provider)
        {
            return Number.FormatUInt32(m_value, format, provider);
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatUInt32(m_value, format, provider, destination, out charsWritten);
        }

        //
        // IConvertible
        //

        public TypeCode GetTypeCode()
        {
            return TypeCode.Byte;
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
            return m_value;
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
            return Convert.ToDouble(m_value);
        }

        decimal IConvertible.ToDecimal(IFormatProvider? provider)
        {
            return Convert.ToDecimal(m_value);
        }

        DateTime IConvertible.ToDateTime(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Byte", "DateTime"));
        }

        object IConvertible.ToType(Type type, IFormatProvider? provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }

        //
        // IAdditionOperators
        //

        static byte IAdditionOperators<byte, byte, byte>.operator +(byte left, byte right)
            => (byte)(left + right);

        // static checked byte IAdditionOperators<byte, byte, byte>.operator +(byte left, byte right)
        //     => checked((byte)(left + right));

        //
        // IAdditiveIdentity
        //

        static byte IAdditiveIdentity<byte, byte>.AdditiveIdentity => 0;

        //
        // IBinaryInteger
        //

        static byte IBinaryInteger<byte>.LeadingZeroCount(byte value)
            => (byte)(BitOperations.LeadingZeroCount(value) - 24);

        static byte IBinaryInteger<byte>.PopCount(byte value)
            => (byte)BitOperations.PopCount(value);

        static byte IBinaryInteger<byte>.RotateLeft(byte value, int rotateAmount)
            => (byte)((value << (rotateAmount & 7)) | (value >> ((8 - rotateAmount) & 7)));

        static byte IBinaryInteger<byte>.RotateRight(byte value, int rotateAmount)
            => (byte)((value >> (rotateAmount & 7)) | (value << ((8 - rotateAmount) & 7)));

        static byte IBinaryInteger<byte>.TrailingZeroCount(byte value)
            => (byte)(BitOperations.TrailingZeroCount(value << 24) - 24);

        //
        // IBinaryNumber
        //

        static bool IBinaryNumber<byte>.IsPow2(byte value)
            => BitOperations.IsPow2((uint)value);

        static byte IBinaryNumber<byte>.Log2(byte value)
            => (byte)BitOperations.Log2(value);

        //
        // IBitwiseOperators
        //

        static byte IBitwiseOperators<byte, byte, byte>.operator &(byte left, byte right)
            => (byte)(left & right);

        static byte IBitwiseOperators<byte, byte, byte>.operator |(byte left, byte right)
            => (byte)(left | right);

        static byte IBitwiseOperators<byte, byte, byte>.operator ^(byte left, byte right)
            => (byte)(left ^ right);

        static byte IBitwiseOperators<byte, byte, byte>.operator ~(byte value)
            => (byte)(~value);

        //
        // IComparisonOperators
        //

        static bool IComparisonOperators<byte, byte>.operator <(byte left, byte right)
            => left < right;

        static bool IComparisonOperators<byte, byte>.operator <=(byte left, byte right)
            => left <= right;

        static bool IComparisonOperators<byte, byte>.operator >(byte left, byte right)
            => left > right;

        static bool IComparisonOperators<byte, byte>.operator >=(byte left, byte right)
            => left >= right;

        //
        // IDecrementOperators
        //

        static byte IDecrementOperators<byte>.operator --(byte value)
            => --value;

        // static checked byte IDecrementOperators<byte>.operator --(byte value)
        //     => checked(--value);

        //
        // IDivisionOperators
        //

        static byte IDivisionOperators<byte, byte, byte>.operator /(byte left, byte right)
            => (byte)(left / right);

        // static checked byte IDivisionOperators<byte, byte, byte>.operator /(byte left, byte right)
        //     => checked((byte)(left / right));

        //
        // IEqualityOperators
        //

        static bool IEqualityOperators<byte, byte>.operator ==(byte left, byte right)
            => left == right;

        static bool IEqualityOperators<byte, byte>.operator !=(byte left, byte right)
            => left != right;

        //
        // IIncrementOperators
        //

        static byte IIncrementOperators<byte>.operator ++(byte value)
            => ++value;

        // static checked byte IIncrementOperators<byte>.operator ++(byte value)
        //     => checked(++value);

        //
        // IMinMaxValue
        //

        static byte IMinMaxValue<byte>.MinValue => MinValue;

        static byte IMinMaxValue<byte>.MaxValue => MaxValue;

        //
        // IModulusOperators
        //

        static byte IModulusOperators<byte, byte, byte>.operator %(byte left, byte right)
            => (byte)(left % right);

        // static checked byte IModulusOperators<byte, byte, byte>.operator %(byte left, byte right)
        //     => checked((byte)(left % right));

        //
        // IMultiplicativeIdentity
        //

        static byte IMultiplicativeIdentity<byte, byte>.MultiplicativeIdentity => 1;

        //
        // IMultiplyOperators
        //

        static byte IMultiplyOperators<byte, byte, byte>.operator *(byte left, byte right)
            => (byte)(left * right);

        // static checked byte IMultiplyOperators<byte, byte, byte>.operator *(byte left, byte right)
        //     => checked((byte)(left * right));

        //
        // INumber
        //

        static byte INumber<byte>.One => 1;

        static byte INumber<byte>.Zero => 0;

        static byte INumber<byte>.Abs(byte value)
            => value;

        static byte INumber<byte>.Clamp(byte value, byte min, byte max)
            => Math.Clamp(value, min, max);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte INumber<byte>.Create<TOther>(TOther value)
        {
            if (typeof(TOther) == typeof(byte))
            {
                return (byte)(object)value;
            }
            else if (typeof(TOther) == typeof(char))
            {
                return checked((byte)(char)(object)value);
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                return checked((byte)(decimal)(object)value);
            }
            else if (typeof(TOther) == typeof(double))
            {
                return checked((byte)(double)(object)value);
            }
            else if (typeof(TOther) == typeof(short))
            {
                return checked((byte)(short)(object)value);
            }
            else if (typeof(TOther) == typeof(int))
            {
                return checked((byte)(int)(object)value);
            }
            else if (typeof(TOther) == typeof(long))
            {
                return checked((byte)(long)(object)value);
            }
            else if (typeof(TOther) == typeof(nint))
            {
                return checked((byte)(nint)(object)value);
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                return checked((byte)(sbyte)(object)value);
            }
            else if (typeof(TOther) == typeof(float))
            {
                return checked((byte)(float)(object)value);
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                return checked((byte)(ushort)(object)value);
            }
            else if (typeof(TOther) == typeof(uint))
            {
                return checked((byte)(uint)(object)value);
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                return checked((byte)(ulong)(object)value);
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                return checked((byte)(nuint)(object)value);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte INumber<byte>.CreateSaturating<TOther>(TOther value)
        {
            if (typeof(TOther) == typeof(byte))
            {
                return (byte)(object)value;
            }
            else if (typeof(TOther) == typeof(char))
            {
                var actualValue = (char)(object)value;
                return (actualValue > MaxValue) ? MaxValue : (byte)actualValue;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                var actualValue = (decimal)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < 0) ? MinValue : (byte)actualValue;
            }
            else if (typeof(TOther) == typeof(double))
            {
                var actualValue = (double)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < 0) ? MinValue : (byte)actualValue;
            }
            else if (typeof(TOther) == typeof(short))
            {
                var actualValue = (short)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < 0) ? MinValue : (byte)actualValue;
            }
            else if (typeof(TOther) == typeof(int))
            {
                var actualValue = (int)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < 0) ? MinValue : (byte)actualValue;
            }
            else if (typeof(TOther) == typeof(long))
            {
                var actualValue = (long)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < 0) ? MinValue : (byte)actualValue;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                var actualValue = (nint)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < 0) ? MinValue : (byte)actualValue;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                var actualValue = (sbyte)(object)value;
                return (actualValue < 0) ? MinValue : (byte)actualValue;
            }
            else if (typeof(TOther) == typeof(float))
            {
                var actualValue = (float)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < 0) ? MinValue : (byte)actualValue;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                var actualValue = (ushort)(object)value;
                return (actualValue > MaxValue) ? MaxValue : (byte)actualValue;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                var actualValue = (uint)(object)value;
                return (actualValue > MaxValue) ? MaxValue : (byte)actualValue;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                var actualValue = (ulong)(object)value;
                return (actualValue > MaxValue) ? MaxValue : (byte)actualValue;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                var actualValue = (nuint)(object)value;
                return (actualValue > MaxValue) ? MaxValue : (byte)actualValue;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte INumber<byte>.CreateTruncating<TOther>(TOther value)
        {
            if (typeof(TOther) == typeof(byte))
            {
                return (byte)(object)value;
            }
            else if (typeof(TOther) == typeof(char))
            {
                return (byte)(char)(object)value;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                return (byte)(decimal)(object)value;
            }
            else if (typeof(TOther) == typeof(double))
            {
                return (byte)(double)(object)value;
            }
            else if (typeof(TOther) == typeof(short))
            {
                return (byte)(short)(object)value;
            }
            else if (typeof(TOther) == typeof(int))
            {
                return (byte)(int)(object)value;
            }
            else if (typeof(TOther) == typeof(long))
            {
                return (byte)(long)(object)value;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                return (byte)(nint)(object)value;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                return (byte)(sbyte)(object)value;
            }
            else if (typeof(TOther) == typeof(float))
            {
                return (byte)(float)(object)value;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                return (byte)(ushort)(object)value;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                return (byte)(uint)(object)value;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                return (byte)(ulong)(object)value;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                return (byte)(nuint)(object)value;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        static (byte Quotient, byte Remainder) INumber<byte>.DivRem(byte left, byte right)
            => Math.DivRem(left, right);

        static byte INumber<byte>.Max(byte x, byte y)
            => Math.Max(x, y);

        static byte INumber<byte>.Min(byte x, byte y)
            => Math.Min(x, y);

        static byte INumber<byte>.Parse(string s, NumberStyles style, IFormatProvider? provider)
            => Parse(s, style, provider);

        static byte INumber<byte>.Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider)
            => Parse(s, style, provider);

        static byte INumber<byte>.Sign(byte value)
            => (byte)((value == 0) ? 0 : 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumber<byte>.TryCreate<TOther>(TOther value, out byte result)
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

                result = (byte)actualValue;
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

                result = (byte)actualValue;
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

                result = (byte)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                var actualValue = (short)(object)value;

                if ((actualValue < 0) || (actualValue > MaxValue))
                {
                    result = default;
                    return false;
                }

                result = (byte)actualValue;
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

                result = (byte)actualValue;
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

                result = (byte)actualValue;
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

                result = (byte)actualValue;
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

                result = (byte)actualValue;
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

                result = (byte)actualValue;
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

                result = (byte)actualValue;
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

                result = (byte)actualValue;
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

                result = (byte)actualValue;
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

                result = (byte)actualValue;
                return true;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                result = default;
                return false;
            }
        }

        static bool INumber<byte>.TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out byte result)
            => TryParse(s, style, provider, out result);

        static bool INumber<byte>.TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out byte result)
            => TryParse(s, style, provider, out result);

        //
        // IParseable
        //

        static byte IParseable<byte>.Parse(string s, IFormatProvider? provider)
            => Parse(s, provider);

        static bool IParseable<byte>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out byte result)
            => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // IShiftOperators
        //

        static byte IShiftOperators<byte, byte>.operator <<(byte value, int shiftAmount)
            => (byte)(value << shiftAmount);

        static byte IShiftOperators<byte, byte>.operator >>(byte value, int shiftAmount)
            => (byte)(value >> shiftAmount);

        // static byte IShiftOperators<byte, byte, byte>.operator >>>(byte value, int shiftAmount)
        //     => (byte)(value >> shiftAmount);

        //
        // ISpanParseable
        //

        static byte ISpanParseable<byte>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
            => Parse(s, NumberStyles.Integer, provider);

        static bool ISpanParseable<byte>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out byte result)
            => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // ISubtractionOperators
        //

        static byte ISubtractionOperators<byte, byte, byte>.operator -(byte left, byte right)
            => (byte)(left - right);

        // static checked byte ISubtractionOperators<byte, byte, byte>.operator -(byte left, byte right)
        //     => checked((byte)(left - right));

        //
        // IUnaryNegationOperators
        //

        static byte IUnaryNegationOperators<byte, byte>.operator -(byte value)
            => (byte)(-value);

        // static checked byte IUnaryNegationOperators<byte, byte>.operator -(byte value)
        //     => checked((byte)(-value));

        //
        // IUnaryPlusOperators
        //

        static byte IUnaryPlusOperators<byte, byte>.operator +(byte value)
            => (byte)(+value);

        // static checked byte IUnaryPlusOperators<byte, byte>.operator +(byte value)
        //     => checked((byte)(+value));
    }
}
