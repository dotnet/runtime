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

        /// <summary>Represents the additive identity (0).</summary>
        public const byte AdditiveIdentity = 0;

        /// <summary>Represents the multiplicative identity (1).</summary>
        public const byte MultiplicativeIdentity = 1;

        /// <summary>Represents the number one (1).</summary>
        public const byte One = 1;

        /// <summary>Represents the number zero (0).</summary>
        public const byte Zero = 0;

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

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        static byte IAdditionOperators<byte, byte, byte>.operator +(byte left, byte right) => (byte)(left + right);

        // /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_CheckedAddition(TSelf, TOther)" />
        // static byte IAdditionOperators<byte, byte, byte>.operator checked +(byte left, byte right) => checked((byte)(left + right));

        //
        // IAdditiveIdentity
        //

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static byte IAdditiveIdentity<byte, byte>.AdditiveIdentity => AdditiveIdentity;

        //
        // IBinaryInteger
        //

        /// <inheritdoc cref="IBinaryInteger{TSelf}.LeadingZeroCount(TSelf)" />
        public static byte LeadingZeroCount(byte value) => (byte)(BitOperations.LeadingZeroCount(value) - 24);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.PopCount(TSelf)" />
        public static byte PopCount(byte value) => (byte)BitOperations.PopCount(value);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.RotateLeft(TSelf, int)" />
        public static byte RotateLeft(byte value, int rotateAmount) => (byte)((value << (rotateAmount & 7)) | (value >> ((8 - rotateAmount) & 7)));

        /// <inheritdoc cref="IBinaryInteger{TSelf}.RotateRight(TSelf, int)" />
        public static byte RotateRight(byte value, int rotateAmount) => (byte)((value >> (rotateAmount & 7)) | (value << ((8 - rotateAmount) & 7)));

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TrailingZeroCount(TSelf)" />
        public static byte TrailingZeroCount(byte value) => (byte)(BitOperations.TrailingZeroCount(value << 24) - 24);

        //
        // IBinaryNumber
        //

        /// <inheritdoc cref="IBinaryNumber{TSelf}.IsPow2(TSelf)" />
        public static bool IsPow2(byte value) => BitOperations.IsPow2((uint)value);

        /// <inheritdoc cref="IBinaryNumber{TSelf}.Log2(TSelf)" />
        public static byte Log2(byte value) => (byte)BitOperations.Log2(value);

        //
        // IBitwiseOperators
        //

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseAnd(TSelf, TOther)" />
        static byte IBitwiseOperators<byte, byte, byte>.operator &(byte left, byte right) => (byte)(left & right);

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseOr(TSelf, TOther)" />
        static byte IBitwiseOperators<byte, byte, byte>.operator |(byte left, byte right) => (byte)(left | right);

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_ExclusiveOr(TSelf, TOther)" />
        static byte IBitwiseOperators<byte, byte, byte>.operator ^(byte left, byte right) => (byte)(left ^ right);

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_OnesComplement(TSelf)" />
        static byte IBitwiseOperators<byte, byte, byte>.operator ~(byte value) => (byte)(~value);

        //
        // IComparisonOperators
        //

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_LessThan(TSelf, TOther)" />
        static bool IComparisonOperators<byte, byte>.operator <(byte left, byte right) => left < right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_LessThanOrEqual(TSelf, TOther)" />
        static bool IComparisonOperators<byte, byte>.operator <=(byte left, byte right) => left <= right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_GreaterThan(TSelf, TOther)" />
        static bool IComparisonOperators<byte, byte>.operator >(byte left, byte right) => left > right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_GreaterThanOrEqual(TSelf, TOther)" />
        static bool IComparisonOperators<byte, byte>.operator >=(byte left, byte right) => left >= right;

        //
        // IDecrementOperators
        //

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        static byte IDecrementOperators<byte>.operator --(byte value) => --value;

        // /// <inheritdoc cref="IDecrementOperators{TSelf}.op_CheckedDecrement(TSelf)" />
        // static byte IDecrementOperators<byte>.operator checked --(byte value) => checked(--value);

        //
        // IDivisionOperators
        //

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_Division(TSelf, TOther)" />
        static byte IDivisionOperators<byte, byte, byte>.operator /(byte left, byte right) => (byte)(left / right);

        // /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_CheckedDivision(TSelf, TOther)" />
        // static byte IDivisionOperators<byte, byte, byte>.operator checked /(byte left, byte right) => checked((byte)(left / right));

        //
        // IEqualityOperators
        //

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther}.op_Equality(TSelf, TOther)" />
        static bool IEqualityOperators<byte, byte>.operator ==(byte left, byte right) => left == right;

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther}.op_Inequality(TSelf, TOther)" />
        static bool IEqualityOperators<byte, byte>.operator !=(byte left, byte right) => left != right;

        //
        // IIncrementOperators
        //

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_Increment(TSelf)" />
        static byte IIncrementOperators<byte>.operator ++(byte value) => ++value;

        // /// <inheritdoc cref="IIncrementOperators{TSelf}.op_CheckedIncrement(TSelf)" />
        // static byte IIncrementOperators<byte>.operator checked ++(byte value) => checked(++value);

        //
        // IMinMaxValue
        //

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MinValue" />
        static byte IMinMaxValue<byte>.MinValue => MinValue;

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MaxValue" />
        static byte IMinMaxValue<byte>.MaxValue => MaxValue;

        //
        // IModulusOperators
        //

        /// <inheritdoc cref="IModulusOperators{TSelf, TOther, TResult}.op_Modulus(TSelf, TOther)" />
        static byte IModulusOperators<byte, byte, byte>.operator %(byte left, byte right) => (byte)(left % right);

        //
        // IMultiplicativeIdentity
        //

        /// <inheritdoc cref="IMultiplicativeIdentity{TSelf, TResult}.MultiplicativeIdentity" />
        static byte IMultiplicativeIdentity<byte, byte>.MultiplicativeIdentity => MultiplicativeIdentity;

        //
        // IMultiplyOperators
        //

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_Multiply(TSelf, TOther)" />
        static byte IMultiplyOperators<byte, byte, byte>.operator *(byte left, byte right) => (byte)(left * right);

        // /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_CheckedMultiply(TSelf, TOther)" />
        // static byte IMultiplyOperators<byte, byte, byte>.operator checked *(byte left, byte right) => checked((byte)(left * right));

        //
        // INumber
        //

        /// <inheritdoc cref="INumber{TSelf}.One" />
        static byte INumber<byte>.One => One;

        /// <inheritdoc cref="INumber{TSelf}.Zero" />
        static byte INumber<byte>.Zero => Zero;

        /// <inheritdoc cref="INumber{TSelf}.Abs(TSelf)" />
        public static byte Abs(byte value) => value;

        /// <inheritdoc cref="INumber{TSelf}.Clamp(TSelf, TSelf, TSelf)" />
        public static byte Clamp(byte value, byte min, byte max) => Math.Clamp(value, min, max);

        /// <inheritdoc cref="INumber{TSelf}.Create{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Create<TOther>(TOther value)
            where TOther : INumber<TOther>
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

        /// <inheritdoc cref="INumber{TSelf}.CreateSaturating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte CreateSaturating<TOther>(TOther value)
            where TOther : INumber<TOther>
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

        /// <inheritdoc cref="INumber{TSelf}.CreateTruncating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte CreateTruncating<TOther>(TOther value)
            where TOther : INumber<TOther>
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

        /// <inheritdoc cref="INumber{TSelf}.DivRem(TSelf, TSelf)" />
        public static (byte Quotient, byte Remainder) DivRem(byte left, byte right) => Math.DivRem(left, right);

        /// <inheritdoc cref="INumber{TSelf}.Max(TSelf, TSelf)" />
        public static byte Max(byte x, byte y) => Math.Max(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Min(TSelf, TSelf)" />
        public static byte Min(byte x, byte y) => Math.Min(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Sign(TSelf)" />
        public static byte Sign(byte value) => (byte)((value == 0) ? 0 : 1);

        /// <inheritdoc cref="INumber{TSelf}.TryCreate{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryCreate<TOther>(TOther value, out byte result)
            where TOther : INumber<TOther>
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

        //
        // IParseable
        //

        /// <inheritdoc cref="IParseable{TSelf}.TryParse(string?, IFormatProvider?, out TSelf)" />
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out byte result) => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // IShiftOperators
        //

        /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_LeftShift(TSelf, int)" />
        static byte IShiftOperators<byte, byte>.operator <<(byte value, int shiftAmount) => (byte)(value << shiftAmount);

        /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_RightShift(TSelf, int)" />
        static byte IShiftOperators<byte, byte>.operator >>(byte value, int shiftAmount) => (byte)(value >> shiftAmount);

        // /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_UnsignedRightShift(TSelf, int)" />
        // static byte IShiftOperators<byte, byte>.operator >>>(byte value, int shiftAmount) => (byte)(value >> shiftAmount);

        //
        // ISpanParseable
        //

        /// <inheritdoc cref="ISpanParseable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static byte Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Integer, provider);

        /// <inheritdoc cref="ISpanParseable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out byte result) => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // ISubtractionOperators
        //

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_Subtraction(TSelf, TOther)" />
        static byte ISubtractionOperators<byte, byte, byte>.operator -(byte left, byte right) => (byte)(left - right);

        // /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_CheckedSubtraction(TSelf, TOther)" />
        // static byte ISubtractionOperators<byte, byte, byte>.operator checked -(byte left, byte right) => checked((byte)(left - right));

        //
        // IUnaryNegationOperators
        //

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_UnaryNegation(TSelf)" />
        static byte IUnaryNegationOperators<byte, byte>.operator -(byte value) => (byte)(-value);

        // /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_CheckedUnaryNegation(TSelf)" />
        // static byte IUnaryNegationOperators<byte, byte>.operator checked -(byte value) => checked((byte)(-value));

        //
        // IUnaryPlusOperators
        //

        /// <inheritdoc cref="IUnaryPlusOperators{TSelf, TResult}.op_UnaryPlus(TSelf)" />
        static byte IUnaryPlusOperators<byte, byte>.operator +(byte value) => (byte)(+value);

        // /// <inheritdoc cref="IUnaryPlusOperators{TSelf, TResult}.op_CheckedUnaryPlus(TSelf)" />
        // static byte IUnaryPlusOperators<byte, byte>.operator checked +(byte value) => checked((byte)(+value));
    }
}
