// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
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
    public readonly struct UInt64
        : IComparable,
          IConvertible,
          ISpanFormattable,
          IComparable<ulong>,
          IEquatable<ulong>,
          IBinaryInteger<ulong>,
          IMinMaxValue<ulong>,
          IUnsignedNumber<ulong>
    {
        private readonly ulong m_value; // Do not rename (binary serialization)

        public const ulong MaxValue = (ulong)0xffffffffffffffffL;
        public const ulong MinValue = 0x0;

        /// <summary>Represents the additive identity (0).</summary>
        private const ulong AdditiveIdentity = 0;

        /// <summary>Represents the multiplicative identity (1).</summary>
        private const ulong MultiplicativeIdentity = 1;

        /// <summary>Represents the number one (1).</summary>
        private const ulong One = 1;

        /// <summary>Represents the number zero (0).</summary>
        private const ulong Zero = 0;

        // Compares this object to another object, returning an integer that
        // indicates the relationship.
        // Returns a value less than zero if this  object
        // null is considered to be less than any instance.
        // If object is not of type UInt64, this method throws an ArgumentException.
        //
        public int CompareTo(object? value)
        {
            if (value == null)
            {
                return 1;
            }

            // Need to use compare because subtraction will wrap
            // to positive for very large neg numbers, etc.
            if (value is ulong i)
            {
                if (m_value < i) return -1;
                if (m_value > i) return 1;
                return 0;
            }

            throw new ArgumentException(SR.Arg_MustBeUInt64);
        }

        public int CompareTo(ulong value)
        {
            // Need to use compare because subtraction will wrap
            // to positive for very large neg numbers, etc.
            if (m_value < value) return -1;
            if (m_value > value) return 1;
            return 0;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (!(obj is ulong))
            {
                return false;
            }
            return m_value == ((ulong)obj).m_value;
        }

        [NonVersionable]
        public bool Equals(ulong obj)
        {
            return m_value == obj;
        }

        // The value of the lower 32 bits XORed with the uppper 32 bits.
        public override int GetHashCode()
        {
            return ((int)m_value) ^ (int)(m_value >> 32);
        }

        public override string ToString()
        {
            return Number.UInt64ToDecStr(m_value, -1);
        }

        public string ToString(IFormatProvider? provider)
        {
            return Number.UInt64ToDecStr(m_value, -1);
        }

        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
        {
            return Number.FormatUInt64(m_value, format, null);
        }

        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider)
        {
            return Number.FormatUInt64(m_value, format, provider);
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatUInt64(m_value, format, provider, destination, out charsWritten);
        }

        public static ulong Parse(string s)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseUInt64(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);
        }

        public static ulong Parse(string s, NumberStyles style)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseUInt64(s, style, NumberFormatInfo.CurrentInfo);
        }

        public static ulong Parse(string s, IFormatProvider? provider)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseUInt64(s, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
        }

        public static ulong Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseUInt64(s, style, NumberFormatInfo.GetInstance(provider));
        }

        public static ulong Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.ParseUInt64(s, style, NumberFormatInfo.GetInstance(provider));
        }

        public static bool TryParse([NotNullWhen(true)] string? s, out ulong result)
        {
            if (s == null)
            {
                result = 0;
                return false;
            }

            return Number.TryParseUInt64IntegerStyle(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result) == Number.ParsingStatus.OK;
        }

        public static bool TryParse(ReadOnlySpan<char> s, out ulong result)
        {
            return Number.TryParseUInt64IntegerStyle(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result) == Number.ParsingStatus.OK;
        }

        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out ulong result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);

            if (s == null)
            {
                result = 0;
                return false;
            }

            return Number.TryParseUInt64(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
        }

        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out ulong result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.TryParseUInt64(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
        }

        //
        // IConvertible implementation
        //

        public TypeCode GetTypeCode()
        {
            return TypeCode.UInt64;
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
            return Convert.ToInt64(m_value);
        }

        ulong IConvertible.ToUInt64(IFormatProvider? provider)
        {
            return m_value;
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
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "UInt64", "DateTime"));
        }

        object IConvertible.ToType(Type type, IFormatProvider? provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }

        //
        // IAdditionOperators
        //

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        static ulong IAdditionOperators<ulong, ulong, ulong>.operator +(ulong left, ulong right) => left + right;

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        static ulong IAdditionOperators<ulong, ulong, ulong>.operator checked +(ulong left, ulong right) => checked(left + right);

        //
        // IAdditiveIdentity
        //

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static ulong IAdditiveIdentity<ulong, ulong>.AdditiveIdentity => AdditiveIdentity;

        //
        // IBinaryInteger
        //

        /// <inheritdoc cref="IBinaryInteger{TSelf}.DivRem(TSelf, TSelf)" />
        public static (ulong Quotient, ulong Remainder) DivRem(ulong left, ulong right) => Math.DivRem(left, right);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.LeadingZeroCount(TSelf)" />
        public static ulong LeadingZeroCount(ulong value) => (ulong)BitOperations.LeadingZeroCount(value);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.PopCount(TSelf)" />
        public static ulong PopCount(ulong value) => (ulong)BitOperations.PopCount(value);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.RotateLeft(TSelf, int)" />
        public static ulong RotateLeft(ulong value, int rotateAmount) => BitOperations.RotateLeft(value, rotateAmount);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.RotateRight(TSelf, int)" />
        public static ulong RotateRight(ulong value, int rotateAmount) => BitOperations.RotateRight(value, rotateAmount);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TrailingZeroCount(TSelf)" />
        public static ulong TrailingZeroCount(ulong value) => (ulong)BitOperations.TrailingZeroCount(value);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.GetShortestBitLength()" />
        long IBinaryInteger<ulong>.GetShortestBitLength() => (sizeof(ulong) * 8) - (long)LeadingZeroCount(m_value);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.GetByteCount()" />
        int IBinaryInteger<ulong>.GetByteCount() => sizeof(ulong);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryWriteLittleEndian(Span{byte}, out int)" />
        bool IBinaryInteger<ulong>.TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(ulong))
            {
                ulong value = BitConverter.IsLittleEndian ? m_value : BinaryPrimitives.ReverseEndianness(m_value);
                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);

                bytesWritten = sizeof(ulong);
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        //
        // IBinaryNumber
        //

        /// <inheritdoc cref="IBinaryNumber{TSelf}.IsPow2(TSelf)" />
        public static bool IsPow2(ulong value) => BitOperations.IsPow2(value);

        /// <inheritdoc cref="IBinaryNumber{TSelf}.Log2(TSelf)" />
        public static ulong Log2(ulong value) => (ulong)BitOperations.Log2(value);

        //
        // IBitwiseOperators
        //

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseAnd(TSelf, TOther)" />
        static ulong IBitwiseOperators<ulong, ulong, ulong>.operator &(ulong left, ulong right) => left & right;

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseOr(TSelf, TOther)" />
        static ulong IBitwiseOperators<ulong, ulong, ulong>.operator |(ulong left, ulong right) => left | right;

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_ExclusiveOr(TSelf, TOther)" />
        static ulong IBitwiseOperators<ulong, ulong, ulong>.operator ^(ulong left, ulong right) => left ^ right;

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_OnesComplement(TSelf)" />
        static ulong IBitwiseOperators<ulong, ulong, ulong>.operator ~(ulong value) => ~value;

        //
        // IComparisonOperators
        //

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_LessThan(TSelf, TOther)" />
        static bool IComparisonOperators<ulong, ulong>.operator <(ulong left, ulong right) => left < right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_LessThanOrEqual(TSelf, TOther)" />
        static bool IComparisonOperators<ulong, ulong>.operator <=(ulong left, ulong right) => left <= right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_GreaterThan(TSelf, TOther)" />
        static bool IComparisonOperators<ulong, ulong>.operator >(ulong left, ulong right) => left > right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_GreaterThanOrEqual(TSelf, TOther)" />
        static bool IComparisonOperators<ulong, ulong>.operator >=(ulong left, ulong right) => left >= right;

        //
        // IDecrementOperators
        //

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        static ulong IDecrementOperators<ulong>.operator --(ulong value) => --value;

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        static ulong IDecrementOperators<ulong>.operator checked --(ulong value) => checked(--value);

        //
        // IDivisionOperators
        //

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_Division(TSelf, TOther)" />
        static ulong IDivisionOperators<ulong, ulong, ulong>.operator /(ulong left, ulong right) => left / right;

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_CheckedDivision(TSelf, TOther)" />
        static ulong IDivisionOperators<ulong, ulong, ulong>.operator checked /(ulong left, ulong right) => left / right;

        //
        // IEqualityOperators
        //

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther}.op_Equality(TSelf, TOther)" />
        static bool IEqualityOperators<ulong, ulong>.operator ==(ulong left, ulong right) => left == right;

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther}.op_Inequality(TSelf, TOther)" />
        static bool IEqualityOperators<ulong, ulong>.operator !=(ulong left, ulong right) => left != right;

        //
        // IIncrementOperators
        //

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_Increment(TSelf)" />
        static ulong IIncrementOperators<ulong>.operator ++(ulong value) => ++value;

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_CheckedIncrement(TSelf)" />
        static ulong IIncrementOperators<ulong>.operator checked ++(ulong value) => checked(++value);

        //
        // IMinMaxValue
        //

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MinValue" />
        static ulong IMinMaxValue<ulong>.MinValue => MinValue;

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MaxValue" />
        static ulong IMinMaxValue<ulong>.MaxValue => MaxValue;

        //
        // IModulusOperators
        //

        /// <inheritdoc cref="IModulusOperators{TSelf, TOther, TResult}.op_Modulus(TSelf, TOther)" />
        static ulong IModulusOperators<ulong, ulong, ulong>.operator %(ulong left, ulong right) => left % right;

        //
        // IMultiplicativeIdentity
        //

        /// <inheritdoc cref="IMultiplicativeIdentity{TSelf, TResult}.MultiplicativeIdentity" />
        static ulong IMultiplicativeIdentity<ulong, ulong>.MultiplicativeIdentity => MultiplicativeIdentity;

        //
        // IMultiplyOperators
        //

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_Multiply(TSelf, TOther)" />
        static ulong IMultiplyOperators<ulong, ulong, ulong>.operator *(ulong left, ulong right) => left * right;

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_CheckedMultiply(TSelf, TOther)" />
        static ulong IMultiplyOperators<ulong, ulong, ulong>.operator checked *(ulong left, ulong right) => checked(left * right);

        //
        // INumber
        //

        /// <inheritdoc cref="INumber{TSelf}.Abs(TSelf)" />
        static ulong INumber<ulong>.Abs(ulong value) => value;

        /// <inheritdoc cref="INumber{TSelf}.Clamp(TSelf, TSelf, TSelf)" />
        public static ulong Clamp(ulong value, ulong min, ulong max) => Math.Clamp(value, min, max);

        /// <inheritdoc cref="INumber{TSelf}.CopySign(TSelf, TSelf)" />
        static ulong INumber<ulong>.CopySign(ulong value, ulong sign) => value;

        /// <inheritdoc cref="INumber{TSelf}.CreateChecked{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong CreateChecked<TOther>(TOther value)
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
                return checked((ulong)(decimal)(object)value);
            }
            else if (typeof(TOther) == typeof(double))
            {
                return checked((ulong)(double)(object)value);
            }
            else if (typeof(TOther) == typeof(short))
            {
                return checked((ulong)(short)(object)value);
            }
            else if (typeof(TOther) == typeof(int))
            {
                return checked((ulong)(int)(object)value);
            }
            else if (typeof(TOther) == typeof(long))
            {
                return checked((ulong)(long)(object)value);
            }
            else if (typeof(TOther) == typeof(nint))
            {
                return checked((ulong)(nint)(object)value);
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                return checked((ulong)(sbyte)(object)value);
            }
            else if (typeof(TOther) == typeof(float))
            {
                return checked((ulong)(float)(object)value);
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
        public static ulong CreateSaturating<TOther>(TOther value)
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
                       (actualValue < 0) ? MinValue : (ulong)actualValue;
            }
            else if (typeof(TOther) == typeof(double))
            {
                var actualValue = (double)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < 0) ? MinValue : (ulong)actualValue;
            }
            else if (typeof(TOther) == typeof(short))
            {
                var actualValue = (short)(object)value;
                return (actualValue < 0) ? MinValue : (ulong)actualValue;
            }
            else if (typeof(TOther) == typeof(int))
            {
                var actualValue = (int)(object)value;
                return (actualValue < 0) ? MinValue : (ulong)actualValue;
            }
            else if (typeof(TOther) == typeof(long))
            {
                var actualValue = (long)(object)value;
                return (actualValue < 0) ? MinValue : (ulong)actualValue;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                var actualValue = (nint)(object)value;
                return (actualValue < 0) ? MinValue : (ulong)actualValue;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                var actualValue = (sbyte)(object)value;
                return (actualValue < 0) ? MinValue : (ulong)actualValue;
            }
            else if (typeof(TOther) == typeof(float))
            {
                var actualValue = (float)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < 0) ? MinValue : (ulong)actualValue;
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
        public static ulong CreateTruncating<TOther>(TOther value)
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
                return (ulong)(decimal)(object)value;
            }
            else if (typeof(TOther) == typeof(double))
            {
                return (ulong)(double)(object)value;
            }
            else if (typeof(TOther) == typeof(short))
            {
                return (ulong)(short)(object)value;
            }
            else if (typeof(TOther) == typeof(int))
            {
                return (ulong)(int)(object)value;
            }
            else if (typeof(TOther) == typeof(long))
            {
                return (ulong)(long)(object)value;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                return (ulong)(nint)(object)value;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                return (ulong)(sbyte)(object)value;
            }
            else if (typeof(TOther) == typeof(float))
            {
                return (ulong)(float)(object)value;
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

        /// <inheritdoc cref="INumber{TSelf}.IsNegative(TSelf)" />
        static bool INumber<ulong>.IsNegative(ulong value) => false;

        /// <inheritdoc cref="INumber{TSelf}.Max(TSelf, TSelf)" />
        public static ulong Max(ulong x, ulong y) => Math.Max(x, y);

        /// <inheritdoc cref="INumber{TSelf}.MaxMagnitude(TSelf, TSelf)" />
        static ulong INumber<ulong>.MaxMagnitude(ulong x, ulong y) => Max(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Min(TSelf, TSelf)" />
        public static ulong Min(ulong x, ulong y) => Math.Min(x, y);

        /// <inheritdoc cref="INumber{TSelf}.MinMagnitude(TSelf, TSelf)" />
        static ulong INumber<ulong>.MinMagnitude(ulong x, ulong y) => Min(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Sign(TSelf)" />
        public static int Sign(ulong value) => (value == 0) ? 0 : 1;

        /// <inheritdoc cref="INumber{TSelf}.TryCreate{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryCreate<TOther>(TOther value, out ulong result)
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

                if ((actualValue < MinValue) || (actualValue > MaxValue))
                {
                    result = default;
                    return false;
                }

                result = (ulong)actualValue;
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

                result = (ulong)actualValue;
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

                result = (ulong)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                var actualValue = (int)(object)value;

                if (actualValue < 0)
                {
                    result = default;
                    return false;
                }

                result = (ulong)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                var actualValue = (long)(object)value;

                if (actualValue < 0)
                {
                    result = default;
                    return false;
                }

                result = (ulong)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                var actualValue = (nint)(object)value;

                if (actualValue < 0)
                {
                    result = default;
                    return false;
                }

                result = (ulong)actualValue;
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

                result = (ulong)actualValue;
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

                result = (ulong)actualValue;
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
        static ulong INumberBase<ulong>.One => One;

        /// <inheritdoc cref="INumberBase{TSelf}.Zero" />
        static ulong INumberBase<ulong>.Zero => Zero;

        //
        // IParsable
        //

        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out ulong result) => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // IShiftOperators
        //

        /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_LeftShift(TSelf, int)" />
        static ulong IShiftOperators<ulong, ulong>.operator <<(ulong value, int shiftAmount) => value << (int)shiftAmount;

        /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_RightShift(TSelf, int)" />
        static ulong IShiftOperators<ulong, ulong>.operator >>(ulong value, int shiftAmount) => value >> (int)shiftAmount;

        // /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_UnsignedRightShift(TSelf, int)" />
        // static ulong IShiftOperators<ulong, ulong>.operator >>>(ulong value, int shiftAmount) => value >> (int)shiftAmount;

        //
        // ISpanParsable
        //

        /// <inheritdoc cref="ISpanParsable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static ulong Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Integer, provider);

        /// <inheritdoc cref="ISpanParsable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out ulong result) => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // ISubtractionOperators
        //

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_Subtraction(TSelf, TOther)" />
        static ulong ISubtractionOperators<ulong, ulong, ulong>.operator -(ulong left, ulong right) => left - right;

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_CheckedSubtraction(TSelf, TOther)" />
        static ulong ISubtractionOperators<ulong, ulong, ulong>.operator checked -(ulong left, ulong right) => checked(left - right);

        //
        // IUnaryNegationOperators
        //

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_UnaryNegation(TSelf)" />
        static ulong IUnaryNegationOperators<ulong, ulong>.operator -(ulong value) => 0UL - value;

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_CheckedUnaryNegation(TSelf)" />
        static ulong IUnaryNegationOperators<ulong, ulong>.operator checked -(ulong value) => checked(0UL - value);

        //
        // IUnaryPlusOperators
        //

        /// <inheritdoc cref="IUnaryPlusOperators{TSelf, TResult}.op_UnaryPlus(TSelf)" />
        static ulong IUnaryPlusOperators<ulong, ulong>.operator +(ulong value) => +value;
    }
}
