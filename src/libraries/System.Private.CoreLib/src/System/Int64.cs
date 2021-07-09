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
    public readonly struct Int64 : IComparable, IConvertible, ISpanFormattable, IComparable<long>, IEquatable<long>
#if FEATURE_GENERIC_MATH
#pragma warning disable SA1001
        , IBinaryInteger<long>,
          IMinMaxValue<long>,
          ISignedNumber<long>
#pragma warning restore SA1001
#endif // FEATURE_GENERIC_MATH
    {
        private readonly long m_value; // Do not rename (binary serialization)

        public const long MaxValue = 0x7fffffffffffffffL;
        public const long MinValue = unchecked((long)0x8000000000000000L);

        // Compares this object to another object, returning an integer that
        // indicates the relationship.
        // Returns a value less than zero if this  object
        // null is considered to be less than any instance.
        // If object is not of type Int64, this method throws an ArgumentException.
        //
        public int CompareTo(object? value)
        {
            if (value == null)
            {
                return 1;
            }

            // Need to use compare because subtraction will wrap
            // to positive for very large neg numbers, etc.
            if (value is long i)
            {
                if (m_value < i) return -1;
                if (m_value > i) return 1;
                return 0;
            }

            throw new ArgumentException(SR.Arg_MustBeInt64);
        }

        public int CompareTo(long value)
        {
            // Need to use compare because subtraction will wrap
            // to positive for very large neg numbers, etc.
            if (m_value < value) return -1;
            if (m_value > value) return 1;
            return 0;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (!(obj is long))
            {
                return false;
            }
            return m_value == ((long)obj).m_value;
        }

        [NonVersionable]
        public bool Equals(long obj)
        {
            return m_value == obj;
        }

        // The value of the lower 32 bits XORed with the uppper 32 bits.
        public override int GetHashCode()
        {
            return unchecked((int)((long)m_value)) ^ (int)(m_value >> 32);
        }

        public override string ToString()
        {
            return Number.Int64ToDecStr(m_value);
        }

        public string ToString(IFormatProvider? provider)
        {
            return Number.FormatInt64(m_value, null, provider);
        }

        public string ToString(string? format)
        {
            return Number.FormatInt64(m_value, format, null);
        }

        public string ToString(string? format, IFormatProvider? provider)
        {
            return Number.FormatInt64(m_value, format, provider);
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatInt64(m_value, format, provider, destination, out charsWritten);
        }

        public static long Parse(string s)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseInt64(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);
        }

        public static long Parse(string s, NumberStyles style)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseInt64(s, style, NumberFormatInfo.CurrentInfo);
        }

        public static long Parse(string s, IFormatProvider? provider)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseInt64(s, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
        }

        // Parses a long from a String in the given style.  If
        // a NumberFormatInfo isn't specified, the current culture's
        // NumberFormatInfo is assumed.
        //
        public static long Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseInt64(s, style, NumberFormatInfo.GetInstance(provider));
        }

        public static long Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.ParseInt64(s, style, NumberFormatInfo.GetInstance(provider));
        }

        public static bool TryParse([NotNullWhen(true)] string? s, out long result)
        {
            if (s == null)
            {
                result = 0;
                return false;
            }

            return Number.TryParseInt64IntegerStyle(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result) == Number.ParsingStatus.OK;
        }

        public static bool TryParse(ReadOnlySpan<char> s, out long result)
        {
            return Number.TryParseInt64IntegerStyle(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result) == Number.ParsingStatus.OK;
        }

        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out long result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);

            if (s == null)
            {
                result = 0;
                return false;
            }

            return Number.TryParseInt64(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
        }

        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out long result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.TryParseInt64(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
        }

        //
        // IConvertible implementation
        //

        public TypeCode GetTypeCode()
        {
            return TypeCode.Int64;
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
            return m_value;
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
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Int64", "DateTime"));
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
        static long IAdditionOperators<long, long, long>.operator +(long left, long right)
            => left + right;

        // [RequiresPreviewFeatures]
        // static checked long IAdditionOperators<long, long, long>.operator +(long left, long right)
        //     => checked(left + right);

        //
        // IAdditiveIdentity
        //

        [RequiresPreviewFeatures]
        static long IAdditiveIdentity<long, long>.AdditiveIdentity => 0;

        //
        // IBinaryInteger
        //

        [RequiresPreviewFeatures]
        static long IBinaryInteger<long>.LeadingZeroCount(long value)
            => BitOperations.LeadingZeroCount((ulong)value);

        [RequiresPreviewFeatures]
        static long IBinaryInteger<long>.PopCount(long value)
            => BitOperations.PopCount((ulong)value);

        [RequiresPreviewFeatures]
        static long IBinaryInteger<long>.RotateLeft(long value, long rotateAmount)
            => (long)BitOperations.RotateLeft((ulong)value, (int)rotateAmount);

        [RequiresPreviewFeatures]
        static long IBinaryInteger<long>.RotateRight(long value, long rotateAmount)
            => (long)BitOperations.RotateRight((ulong)value, (int)rotateAmount);

        [RequiresPreviewFeatures]
        static long IBinaryInteger<long>.TrailingZeroCount(long value)
            => BitOperations.TrailingZeroCount(value);

        //
        // IBinaryNumber
        //

        [RequiresPreviewFeatures]
        static bool IBinaryNumber<long>.IsPow2(long value)
            => BitOperations.IsPow2(value);

        [RequiresPreviewFeatures]
        static long IBinaryNumber<long>.Log2(long value)
        {
            if (value < 0)
            {
                ThrowHelper.ThrowValueArgumentOutOfRange_NeedNonNegNumException();
            }
            return BitOperations.Log2((ulong)value);
        }

        //
        // IBitwiseOperators
        //

        [RequiresPreviewFeatures]
        static long IBitwiseOperators<long, long, long>.operator &(long left, long right)
            => left & right;

        [RequiresPreviewFeatures]
        static long IBitwiseOperators<long, long, long>.operator |(long left, long right)
            => left | right;

        [RequiresPreviewFeatures]
        static long IBitwiseOperators<long, long, long>.operator ^(long left, long right)
            => left ^ right;

        [RequiresPreviewFeatures]
        static long IBitwiseOperators<long, long, long>.operator ~(long value)
            => ~value;

        //
        // IComparisonOperators
        //

        [RequiresPreviewFeatures]
        static bool IComparisonOperators<long, long>.operator <(long left, long right)
            => left < right;

        [RequiresPreviewFeatures]
        static bool IComparisonOperators<long, long>.operator <=(long left, long right)
            => left <= right;

        [RequiresPreviewFeatures]
        static bool IComparisonOperators<long, long>.operator >(long left, long right)
            => left > right;

        [RequiresPreviewFeatures]
        static bool IComparisonOperators<long, long>.operator >=(long left, long right)
            => left >= right;

        //
        // IDecrementOperators
        //

        [RequiresPreviewFeatures]
        static long IDecrementOperators<long>.operator --(long value)
            => value--;

        // [RequiresPreviewFeatures]
        // static checked long IDecrementOperators<long>.operator --(long value)
        //     => checked(value--);

        //
        // IDivisionOperators
        //

        [RequiresPreviewFeatures]
        static long IDivisionOperators<long, long, long>.operator /(long left, long right)
            => left / right;

        // [RequiresPreviewFeatures]
        // static checked long IDivisionOperators<long, long, long>.operator /(long left, long right)
        //     => checked(left / right);

        //
        // IEqualityOperators
        //

        [RequiresPreviewFeatures]
        static bool IEqualityOperators<long, long>.operator ==(long left, long right)
            => left == right;

        [RequiresPreviewFeatures]
        static bool IEqualityOperators<long, long>.operator !=(long left, long right)
            => left != right;

        //
        // IIncrementOperators
        //

        [RequiresPreviewFeatures]
        static long IIncrementOperators<long>.operator ++(long value)
            => value++;

        // [RequiresPreviewFeatures]
        // static checked long IIncrementOperators<long>.operator ++(long value)
        //     => checked(value++);

        //
        // IMinMaxValue
        //

        [RequiresPreviewFeatures]
        static long IMinMaxValue<long>.MinValue => MinValue;

        [RequiresPreviewFeatures]
        static long IMinMaxValue<long>.MaxValue => MaxValue;

        //
        // IModulusOperators
        //

        [RequiresPreviewFeatures]
        static long IModulusOperators<long, long, long>.operator %(long left, long right)
            => left % right;

        // [RequiresPreviewFeatures]
        // static checked long IModulusOperators<long, long, long>.operator %(long left, long right)
        //     => checked(left % right);

        //
        // IMultiplicativeIdentity
        //

        [RequiresPreviewFeatures]
        static long IMultiplicativeIdentity<long, long>.MultiplicativeIdentity => 1;

        //
        // IMultiplyOperators
        //

        [RequiresPreviewFeatures]
        static long IMultiplyOperators<long, long, long>.operator *(long left, long right)
            => left * right;

        // [RequiresPreviewFeatures]
        // static checked long IMultiplyOperators<long, long, long>.operator *(long left, long right)
        //     => checked(left * right);

        //
        // INumber
        //

        [RequiresPreviewFeatures]
        static long INumber<long>.One => 1;

        [RequiresPreviewFeatures]
        static long INumber<long>.Zero => 0;

        [RequiresPreviewFeatures]
        static long INumber<long>.Abs(long value)
            => Math.Abs(value);

        [RequiresPreviewFeatures]
        static long INumber<long>.Clamp(long value, long min, long max)
            => Math.Clamp(value, min, max);

        [RequiresPreviewFeatures]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static long INumber<long>.Create<TOther>(TOther value)
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
                return checked((long)(decimal)(object)value);
            }
            else if (typeof(TOther) == typeof(double))
            {
                return checked((long)(double)(object)value);
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
                return checked((long)(float)(object)value);
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
                return checked((long)(ulong)(object)value);
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                return checked((long)(nuint)(object)value);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        [RequiresPreviewFeatures]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static long INumber<long>.CreateSaturating<TOther>(TOther value)
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
                       (actualValue < MinValue) ? MinValue : (long)actualValue;
            }
            else if (typeof(TOther) == typeof(double))
            {
                var actualValue = (double)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < MinValue) ? MinValue : (long)actualValue;
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
                var actualValue = (float)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < MinValue) ? MinValue : (long)actualValue;
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
                var actualValue = (ulong)(object)value;
                return (actualValue > MaxValue) ? MaxValue : (long)actualValue;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                var actualValue = (nuint)(object)value;
                return (actualValue > MaxValue) ? MaxValue : (long)actualValue;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        [RequiresPreviewFeatures]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static long INumber<long>.CreateTruncating<TOther>(TOther value)
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
                return (long)(decimal)(object)value;
            }
            else if (typeof(TOther) == typeof(double))
            {
                return (long)(double)(object)value;
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
                return (long)(float)(object)value;
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
                return (long)(ulong)(object)value;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                return (long)(nuint)(object)value;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        [RequiresPreviewFeatures]
        static (long Quotient, long Remainder) INumber<long>.DivRem(long left, long right)
            => Math.DivRem(left, right);

        [RequiresPreviewFeatures]
        static long INumber<long>.Max(long x, long y)
            => Math.Max(x, y);

        [RequiresPreviewFeatures]
        static long INumber<long>.Min(long x, long y)
            => Math.Min(x, y);

        [RequiresPreviewFeatures]
        static long INumber<long>.Parse(string s, NumberStyles style, IFormatProvider? provider)
            => Parse(s, style, provider);

        [RequiresPreviewFeatures]
        static long INumber<long>.Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider)
            => Parse(s, style, provider);

        [RequiresPreviewFeatures]
        static long INumber<long>.Sign(long value)
            => Math.Sign(value);

        [RequiresPreviewFeatures]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumber<long>.TryCreate<TOther>(TOther value, out long result)
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

                if ((actualValue < MinValue) || (actualValue > MaxValue))
                {
                    result = default;
                    return false;
                }

                result = (long)actualValue;
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

                result = (long)actualValue;
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
                var actualValue = (float)(object)value;

                if ((actualValue < MinValue) || (actualValue > MaxValue))
                {
                    result = default;
                    return false;
                }

                result = (long)actualValue;
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
                var actualValue = (ulong)(object)value;

                if (actualValue > MaxValue)
                {
                    result = default;
                    return false;
                }

                result = (long)actualValue;
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

                result = (long)actualValue;
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
        static bool INumber<long>.TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out long result)
            => TryParse(s, style, provider, out result);

        [RequiresPreviewFeatures]
        static bool INumber<long>.TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out long result)
            => TryParse(s, style, provider, out result);

        //
        // IParseable
        //

        [RequiresPreviewFeatures]
        static long IParseable<long>.Parse(string s, IFormatProvider? provider)
            => Parse(s, provider);

        [RequiresPreviewFeatures]
        static bool IParseable<long>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out long result)
            => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // IShiftOperators
        //

        [RequiresPreviewFeatures]
        static long IShiftOperators<long, long>.operator <<(long value, int shiftAmount)
            => value << (int)shiftAmount;

        [RequiresPreviewFeatures]
        static long IShiftOperators<long, long>.operator >>(long value, int shiftAmount)
            => value >> (int)shiftAmount;

        // [RequiresPreviewFeatures]
        // static long IShiftOperators<long, long>.operator >>>(long value, int shiftAmount)
        //     => (long)((ulong)value >> (int)shiftAmount);

        //
        // ISignedNumber
        //

        [RequiresPreviewFeatures]
        static long ISignedNumber<long>.NegativeOne => -1;

        //
        // ISpanParseable
        //

        [RequiresPreviewFeatures]
        static long ISpanParseable<long>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
            => Parse(s, NumberStyles.Integer, provider);

        [RequiresPreviewFeatures]
        static bool ISpanParseable<long>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out long result)
            => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // ISubtractionOperators
        //

        [RequiresPreviewFeatures]
        static long ISubtractionOperators<long, long, long>.operator -(long left, long right)
            => left - right;

        // [RequiresPreviewFeatures]
        // static checked long ISubtractionOperators<long, long, long>.operator -(long left, long right)
        //     => checked(left - right);

        //
        // IUnaryNegationOperators
        //

        [RequiresPreviewFeatures]
        static long IUnaryNegationOperators<long, long>.operator -(long value)
            => -value;

        // [RequiresPreviewFeatures]
        // static checked long IUnaryNegationOperators<long, long>.operator -(long value)
        //     => checked(-value);

        //
        // IUnaryPlusOperators
        //

        [RequiresPreviewFeatures]
        static long IUnaryPlusOperators<long, long>.operator +(long value)
            => +value;

        // [RequiresPreviewFeatures]
        // static checked long IUnaryPlusOperators<long, long>.operator +(long value)
        //     => checked(+value);
#endif // FEATURE_GENERIC_MATH
    }
}
