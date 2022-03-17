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
    public readonly struct SByte
        : IComparable,
          IConvertible,
          ISpanFormattable,
          IComparable<sbyte>,
          IEquatable<sbyte>,
          IBinaryInteger<sbyte>,
          IMinMaxValue<sbyte>,
          ISignedNumber<sbyte>
    {
        private readonly sbyte m_value; // Do not rename (binary serialization)

        // The maximum value that a Byte may represent: 127.
        public const sbyte MaxValue = (sbyte)0x7F;

        // The minimum value that a Byte may represent: -128.
        public const sbyte MinValue = unchecked((sbyte)0x80);


        // Compares this object to another object, returning an integer that
        // indicates the relationship.
        // Returns a value less than zero if this  object
        // null is considered to be less than any instance.
        // If object is not of type SByte, this method throws an ArgumentException.
        //
        public int CompareTo(object? obj)
        {
            if (obj == null)
            {
                return 1;
            }
            if (!(obj is sbyte))
            {
                throw new ArgumentException(SR.Arg_MustBeSByte);
            }
            return m_value - ((sbyte)obj).m_value;
        }

        public int CompareTo(sbyte value)
        {
            return m_value - value;
        }

        // Determines whether two Byte objects are equal.
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (!(obj is sbyte))
            {
                return false;
            }
            return m_value == ((sbyte)obj).m_value;
        }

        [NonVersionable]
        public bool Equals(sbyte obj)
        {
            return m_value == obj;
        }

        // Gets a hash code for this instance.
        public override int GetHashCode()
        {
            return m_value;
        }


        // Provides a string representation of a byte.
        public override string ToString()
        {
            return Number.Int32ToDecStr(m_value);
        }

        public string ToString(string? format)
        {
            return ToString(format, null);
        }

        public string ToString(IFormatProvider? provider)
        {
            return Number.FormatInt32(m_value, 0, null, provider);
        }

        public string ToString(string? format, IFormatProvider? provider)
        {
            return Number.FormatInt32(m_value, 0x000000FF, format, provider);
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatInt32(m_value, 0x000000FF, format, provider, destination, out charsWritten);
        }

        public static sbyte Parse(string s)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Parse((ReadOnlySpan<char>)s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);
        }

        public static sbyte Parse(string s, NumberStyles style)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Parse((ReadOnlySpan<char>)s, style, NumberFormatInfo.CurrentInfo);
        }

        public static sbyte Parse(string s, IFormatProvider? provider)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Parse((ReadOnlySpan<char>)s, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
        }

        // Parses a signed byte from a String in the given style.  If
        // a NumberFormatInfo isn't specified, the current culture's
        // NumberFormatInfo is assumed.
        //
        public static sbyte Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Parse((ReadOnlySpan<char>)s, style, NumberFormatInfo.GetInstance(provider));
        }

        public static sbyte Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Parse(s, style, NumberFormatInfo.GetInstance(provider));
        }

        private static sbyte Parse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info)
        {
            Number.ParsingStatus status = Number.TryParseInt32(s, style, info, out int i);
            if (status != Number.ParsingStatus.OK)
            {
                Number.ThrowOverflowOrFormatException(status, TypeCode.SByte);
            }

            // For hex number styles AllowHexSpecifier >> 2 == 0x80 and cancels out MinValue so the check is effectively: (uint)i > byte.MaxValue
            // For integer styles it's zero and the effective check is (uint)(i - MinValue) > byte.MaxValue
            if ((uint)(i - MinValue - ((int)(style & NumberStyles.AllowHexSpecifier) >> 2)) > byte.MaxValue)
            {
                Number.ThrowOverflowException(TypeCode.SByte);
            }
            return (sbyte)i;
        }

        public static bool TryParse([NotNullWhen(true)] string? s, out sbyte result)
        {
            if (s == null)
            {
                result = 0;
                return false;
            }

            return TryParse((ReadOnlySpan<char>)s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse(ReadOnlySpan<char> s, out sbyte result)
        {
            return TryParse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out sbyte result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);

            if (s == null)
            {
                result = 0;
                return false;
            }

            return TryParse((ReadOnlySpan<char>)s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out sbyte result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return TryParse(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        private static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info, out sbyte result)
        {
            // For hex number styles AllowHexSpecifier >> 2 == 0x80 and cancels out MinValue so the check is effectively: (uint)i > byte.MaxValue
            // For integer styles it's zero and the effective check is (uint)(i - MinValue) > byte.MaxValue
            if (Number.TryParseInt32(s, style, info, out int i) != Number.ParsingStatus.OK
                || (uint)(i - MinValue - ((int)(style & NumberStyles.AllowHexSpecifier) >> 2)) > byte.MaxValue)
            {
                result = 0;
                return false;
            }
            result = (sbyte)i;
            return true;
        }

        //
        // IConvertible implementation
        //

        public TypeCode GetTypeCode()
        {
            return TypeCode.SByte;
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
            return m_value;
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
            return m_value;
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
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "SByte", "DateTime"));
        }

        object IConvertible.ToType(Type type, IFormatProvider? provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }

        //
        // IAdditionOperators
        //

        static sbyte IAdditionOperators<sbyte, sbyte, sbyte>.operator +(sbyte left, sbyte right)
            => (sbyte)(left + right);

        // static checked sbyte IAdditionOperators<sbyte, sbyte, sbyte>.operator +(sbyte left, sbyte right)
        //     => checked((sbyte)(left + right));

        //
        // IAdditiveIdentity
        //

        static sbyte IAdditiveIdentity<sbyte, sbyte>.AdditiveIdentity => 0;

        //
        // IBinaryInteger
        //

        static sbyte IBinaryInteger<sbyte>.LeadingZeroCount(sbyte value)
            => (sbyte)(BitOperations.LeadingZeroCount((byte)value) - 24);

        static sbyte IBinaryInteger<sbyte>.PopCount(sbyte value)
            => (sbyte)BitOperations.PopCount((byte)value);

        static sbyte IBinaryInteger<sbyte>.RotateLeft(sbyte value, int rotateAmount)
            => (sbyte)((value << (rotateAmount & 7)) | ((byte)value >> ((8 - rotateAmount) & 7)));

        static sbyte IBinaryInteger<sbyte>.RotateRight(sbyte value, int rotateAmount)
            => (sbyte)(((byte)value >> (rotateAmount & 7)) | (value << ((8 - rotateAmount) & 7)));

        static sbyte IBinaryInteger<sbyte>.TrailingZeroCount(sbyte value)
            => (sbyte)(BitOperations.TrailingZeroCount(value << 24) - 24);

        //
        // IBinaryNumber
        //

        static bool IBinaryNumber<sbyte>.IsPow2(sbyte value)
            => BitOperations.IsPow2(value);

        static sbyte IBinaryNumber<sbyte>.Log2(sbyte value)
        {
            if (value < 0)
            {
                ThrowHelper.ThrowValueArgumentOutOfRange_NeedNonNegNumException();
            }
            return (sbyte)BitOperations.Log2((byte)value);
        }

        //
        // IBitwiseOperators
        //

        static sbyte IBitwiseOperators<sbyte, sbyte, sbyte>.operator &(sbyte left, sbyte right)
            => (sbyte)(left & right);

        static sbyte IBitwiseOperators<sbyte, sbyte, sbyte>.operator |(sbyte left, sbyte right)
            => (sbyte)(left | right);

        static sbyte IBitwiseOperators<sbyte, sbyte, sbyte>.operator ^(sbyte left, sbyte right)
            => (sbyte)(left ^ right);

        static sbyte IBitwiseOperators<sbyte, sbyte, sbyte>.operator ~(sbyte value)
            => (sbyte)(~value);

        //
        // IComparisonOperators
        //

        static bool IComparisonOperators<sbyte, sbyte>.operator <(sbyte left, sbyte right)
            => left < right;

        static bool IComparisonOperators<sbyte, sbyte>.operator <=(sbyte left, sbyte right)
            => left <= right;

        static bool IComparisonOperators<sbyte, sbyte>.operator >(sbyte left, sbyte right)
            => left > right;

        static bool IComparisonOperators<sbyte, sbyte>.operator >=(sbyte left, sbyte right)
            => left >= right;

        //
        // IDecrementOperators
        //

        static sbyte IDecrementOperators<sbyte>.operator --(sbyte value)
            => --value;

        // static checked sbyte IDecrementOperators<sbyte>.operator --(sbyte value)
        //     => checked(--value);

        //
        // IDivisionOperators
        //

        static sbyte IDivisionOperators<sbyte, sbyte, sbyte>.operator /(sbyte left, sbyte right)
            => (sbyte)(left / right);

        // static checked sbyte IDivisionOperators<sbyte, sbyte, sbyte>.operator /(sbyte left, sbyte right)
        //     => checked((sbyte)(left / right));

        //
        // IEqualityOperators
        //

        static bool IEqualityOperators<sbyte, sbyte>.operator ==(sbyte left, sbyte right)
            => left == right;

        static bool IEqualityOperators<sbyte, sbyte>.operator !=(sbyte left, sbyte right)
            => left != right;

        //
        // IIncrementOperators
        //

        static sbyte IIncrementOperators<sbyte>.operator ++(sbyte value)
            => ++value;

        // static checked sbyte IIncrementOperators<sbyte>.operator ++(sbyte value)
        //     => checked(++value);

        //
        // IMinMaxValue
        //

        static sbyte IMinMaxValue<sbyte>.MinValue => MinValue;

        static sbyte IMinMaxValue<sbyte>.MaxValue => MaxValue;

        //
        // IModulusOperators
        //

        static sbyte IModulusOperators<sbyte, sbyte, sbyte>.operator %(sbyte left, sbyte right)
            => (sbyte)(left % right);

        // static checked sbyte IModulusOperators<sbyte, sbyte, sbyte>.operator %(sbyte left, sbyte right)
        //     => checked((sbyte)(left % right));

        //
        // IMultiplicativeIdentity
        //

        static sbyte IMultiplicativeIdentity<sbyte, sbyte>.MultiplicativeIdentity => 1;

        //
        // IMultiplyOperators
        //

        static sbyte IMultiplyOperators<sbyte, sbyte, sbyte>.operator *(sbyte left, sbyte right)
            => (sbyte)(left * right);

        // static checked sbyte IMultiplyOperators<sbyte, sbyte, sbyte>.operator *(sbyte left, sbyte right)
        //     => checked((sbyte)(left * right));

        //
        // INumber
        //

        static sbyte INumber<sbyte>.One => 1;

        static sbyte INumber<sbyte>.Zero => 0;

        static sbyte INumber<sbyte>.Abs(sbyte value)
            => Math.Abs(value);

        static sbyte INumber<sbyte>.Clamp(sbyte value, sbyte min, sbyte max)
            => Math.Clamp(value, min, max);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static sbyte INumber<sbyte>.Create<TOther>(TOther value)
        {
            if (typeof(TOther) == typeof(byte))
            {
                return checked((sbyte)(byte)(object)value);
            }
            else if (typeof(TOther) == typeof(char))
            {
                return checked((sbyte)(char)(object)value);
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                return checked((sbyte)(decimal)(object)value);
            }
            else if (typeof(TOther) == typeof(double))
            {
                return checked((sbyte)(double)(object)value);
            }
            else if (typeof(TOther) == typeof(short))
            {
                return checked((sbyte)(short)(object)value);
            }
            else if (typeof(TOther) == typeof(int))
            {
                return checked((sbyte)(int)(object)value);
            }
            else if (typeof(TOther) == typeof(long))
            {
                return checked((sbyte)(long)(object)value);
            }
            else if (typeof(TOther) == typeof(nint))
            {
                return checked((sbyte)(nint)(object)value);
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                return (sbyte)(object)value;
            }
            else if (typeof(TOther) == typeof(float))
            {
                return checked((sbyte)(float)(object)value);
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                return checked((sbyte)(ushort)(object)value);
            }
            else if (typeof(TOther) == typeof(uint))
            {
                return checked((sbyte)(uint)(object)value);
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                return checked((sbyte)(ulong)(object)value);
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                return checked((sbyte)(nuint)(object)value);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static sbyte INumber<sbyte>.CreateSaturating<TOther>(TOther value)
        {
            if (typeof(TOther) == typeof(byte))
            {
                var actualValue = (byte)(object)value;
                return (actualValue > MaxValue) ? MaxValue : (sbyte)actualValue;
            }
            else if (typeof(TOther) == typeof(char))
            {
                var actualValue = (char)(object)value;
                return (actualValue > MaxValue) ? MaxValue : (sbyte)actualValue;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                var actualValue = (decimal)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < MinValue) ? MinValue : (sbyte)actualValue;
            }
            else if (typeof(TOther) == typeof(double))
            {
                var actualValue = (double)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < MinValue) ? MinValue : (sbyte)actualValue;
            }
            else if (typeof(TOther) == typeof(short))
            {
                var actualValue = (short)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < MinValue) ? MinValue : (sbyte)actualValue;
            }
            else if (typeof(TOther) == typeof(int))
            {
                var actualValue = (int)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < MinValue) ? MinValue : (sbyte)actualValue;
            }
            else if (typeof(TOther) == typeof(long))
            {
                var actualValue = (long)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < MinValue) ? MinValue : (sbyte)actualValue;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                var actualValue = (nint)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < MinValue) ? MinValue : (sbyte)actualValue;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                return (sbyte)(object)value;
            }
            else if (typeof(TOther) == typeof(float))
            {
                var actualValue = (float)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < MinValue) ? MinValue : (sbyte)actualValue;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                var actualValue = (ushort)(object)value;
                return (actualValue > MaxValue) ? MaxValue : (sbyte)actualValue;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                var actualValue = (uint)(object)value;
                return (actualValue > MaxValue) ? MaxValue : (sbyte)actualValue;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                var actualValue = (ulong)(object)value;
                return (actualValue > (uint)MaxValue) ? MaxValue : (sbyte)actualValue;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                var actualValue = (nuint)(object)value;
                return (actualValue > (uint)MaxValue) ? MaxValue : (sbyte)actualValue;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static sbyte INumber<sbyte>.CreateTruncating<TOther>(TOther value)
        {
            if (typeof(TOther) == typeof(byte))
            {
                return (sbyte)(byte)(object)value;
            }
            else if (typeof(TOther) == typeof(char))
            {
                return (sbyte)(char)(object)value;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                return (sbyte)(decimal)(object)value;
            }
            else if (typeof(TOther) == typeof(double))
            {
                return (sbyte)(double)(object)value;
            }
            else if (typeof(TOther) == typeof(short))
            {
                return (sbyte)(short)(object)value;
            }
            else if (typeof(TOther) == typeof(int))
            {
                return (sbyte)(int)(object)value;
            }
            else if (typeof(TOther) == typeof(long))
            {
                return (sbyte)(long)(object)value;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                return (sbyte)(nint)(object)value;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                return (sbyte)(object)value;
            }
            else if (typeof(TOther) == typeof(float))
            {
                return (sbyte)(float)(object)value;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                return (sbyte)(ushort)(object)value;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                return (sbyte)(uint)(object)value;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                return (sbyte)(ulong)(object)value;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                return (sbyte)(nuint)(object)value;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        static (sbyte Quotient, sbyte Remainder) INumber<sbyte>.DivRem(sbyte left, sbyte right)
            => Math.DivRem(left, right);

        static sbyte INumber<sbyte>.Max(sbyte x, sbyte y)
            => Math.Max(x, y);

        static sbyte INumber<sbyte>.Min(sbyte x, sbyte y)
            => Math.Min(x, y);

        static sbyte INumber<sbyte>.Parse(string s, NumberStyles style, IFormatProvider? provider)
            => Parse(s, style, provider);

        static sbyte INumber<sbyte>.Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider)
            => Parse(s, style, provider);

        static sbyte INumber<sbyte>.Sign(sbyte value)
            => (sbyte)Math.Sign(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumber<sbyte>.TryCreate<TOther>(TOther value, out sbyte result)
        {
            if (typeof(TOther) == typeof(byte))
            {
                var actualValue = (byte)(object)value;

                if (actualValue > MaxValue)
                {
                    result = default;
                    return false;
                }

                result = (sbyte)actualValue;
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

                result = (sbyte)actualValue;
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

                result = (sbyte)actualValue;
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

                result = (sbyte)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                var actualValue = (short)(object)value;

                if ((actualValue < MinValue) || (actualValue > MaxValue))
                {
                    result = default;
                    return false;
                }

                result = (sbyte)actualValue;
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

                result = (sbyte)actualValue;
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

                result = (sbyte)actualValue;
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

                result = (sbyte)actualValue;
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

                result = (sbyte)actualValue;
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

                result = (sbyte)actualValue;
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

                result = (sbyte)actualValue;
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

                result = (sbyte)actualValue;
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

                result = (sbyte)actualValue;
                return true;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                result = default;
                return false;
            }
        }

        static bool INumber<sbyte>.TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out sbyte result)
            => TryParse(s, style, provider, out result);

        static bool INumber<sbyte>.TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out sbyte result)
            => TryParse(s, style, provider, out result);

        //
        // IParseable
        //

        static sbyte IParseable<sbyte>.Parse(string s, IFormatProvider? provider)
            => Parse(s, provider);

        static bool IParseable<sbyte>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out sbyte result)
            => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // IShiftOperators
        //

        static sbyte IShiftOperators<sbyte, sbyte>.operator <<(sbyte value, int shiftAmount)
            => (sbyte)(value << shiftAmount);

        static sbyte IShiftOperators<sbyte, sbyte>.operator >>(sbyte value, int shiftAmount)
            => (sbyte)(value >> shiftAmount);

        // static sbyte IShiftOperators<sbyte, sbyte>.operator >>>(sbyte value, int shiftAmount)
        //     => (sbyte)((byte)value >> shiftAmount);

        //
        // ISignedNumber
        //

        static sbyte ISignedNumber<sbyte>.NegativeOne => -1;

        //
        // ISpanParseable
        //

        static sbyte ISpanParseable<sbyte>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
            => Parse(s, NumberStyles.Integer, provider);

        static bool ISpanParseable<sbyte>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out sbyte result)
            => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // ISubtractionOperators
        //

        static sbyte ISubtractionOperators<sbyte, sbyte, sbyte>.operator -(sbyte left, sbyte right)
            => (sbyte)(left - right);

        // static checked sbyte ISubtractionOperators<sbyte, sbyte, sbyte>.operator -(sbyte left, sbyte right)
        //     => checked((sbyte)(left - right));

        //
        // IUnaryNegationOperators
        //

        static sbyte IUnaryNegationOperators<sbyte, sbyte>.operator -(sbyte value)
            => (sbyte)(-value);

        // static checked sbyte IUnaryNegationOperators<sbyte, sbyte>.operator -(sbyte value)
        //     => checked((sbyte)(-value));

        //
        // IUnaryPlusOperators
        //

        static sbyte IUnaryPlusOperators<sbyte, sbyte>.operator +(sbyte value)
            => (sbyte)(+value);

        // static checked sbyte IUnaryPlusOperators<sbyte, sbyte>.operator +(sbyte value)
        //     => checked((sbyte)(+value));
    }
}
