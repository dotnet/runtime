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
    public readonly struct UInt32
        : IComparable,
          IConvertible,
          ISpanFormattable,
          IComparable<uint>,
          IEquatable<uint>,
          IBinaryInteger<uint>,
          IMinMaxValue<uint>,
          IUnsignedNumber<uint>
    {
        private readonly uint m_value; // Do not rename (binary serialization)

        public const uint MaxValue = (uint)0xffffffff;
        public const uint MinValue = 0U;

        /// <summary>Represents the additive identity (0).</summary>
        private const uint AdditiveIdentity = 0;

        /// <summary>Represents the multiplicative identity (1).</summary>
        private const uint MultiplicativeIdentity = 1;

        /// <summary>Represents the number one (1).</summary>
        private const uint One = 1;

        /// <summary>Represents the number zero (0).</summary>
        private const uint Zero = 0;

        // Compares this object to another object, returning an integer that
        // indicates the relationship.
        // Returns a value less than zero if this  object
        // null is considered to be less than any instance.
        // If object is not of type UInt32, this method throws an ArgumentException.
        //
        public int CompareTo(object? value)
        {
            if (value == null)
            {
                return 1;
            }

            // Need to use compare because subtraction will wrap
            // to positive for very large neg numbers, etc.
            if (value is uint i)
            {
                if (m_value < i) return -1;
                if (m_value > i) return 1;
                return 0;
            }

            throw new ArgumentException(SR.Arg_MustBeUInt32);
        }

        public int CompareTo(uint value)
        {
            // Need to use compare because subtraction will wrap
            // to positive for very large neg numbers, etc.
            if (m_value < value) return -1;
            if (m_value > value) return 1;
            return 0;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (!(obj is uint))
            {
                return false;
            }
            return m_value == ((uint)obj).m_value;
        }

        [NonVersionable]
        public bool Equals(uint obj)
        {
            return m_value == obj;
        }

        // The absolute value of the int contained.
        public override int GetHashCode()
        {
            return (int)m_value;
        }

        // The base 10 representation of the number with no extra padding.
        public override string ToString()
        {
            return Number.UInt32ToDecStr(m_value);
        }

        public string ToString(IFormatProvider? provider)
        {
            return Number.UInt32ToDecStr(m_value);
        }

        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
        {
            return Number.FormatUInt32(m_value, format, null);
        }

        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider)
        {
            return Number.FormatUInt32(m_value, format, provider);
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatUInt32(m_value, format, provider, destination, out charsWritten);
        }

        public static uint Parse(string s)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseUInt32(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);
        }

        public static uint Parse(string s, NumberStyles style)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseUInt32(s, style, NumberFormatInfo.CurrentInfo);
        }

        public static uint Parse(string s, IFormatProvider? provider)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseUInt32(s, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
        }

        public static uint Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseUInt32(s, style, NumberFormatInfo.GetInstance(provider));
        }

        public static uint Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.ParseUInt32(s, style, NumberFormatInfo.GetInstance(provider));
        }

        public static bool TryParse([NotNullWhen(true)] string? s, out uint result)
        {
            if (s == null)
            {
                result = 0;
                return false;
            }

            return Number.TryParseUInt32IntegerStyle(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result) == Number.ParsingStatus.OK;
        }

        public static bool TryParse(ReadOnlySpan<char> s, out uint result)
        {
            return Number.TryParseUInt32IntegerStyle(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result) == Number.ParsingStatus.OK;
        }

        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out uint result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);

            if (s == null)
            {
                result = 0;
                return false;
            }

            return Number.TryParseUInt32(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
        }

        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out uint result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.TryParseUInt32(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
        }

        //
        // IConvertible implementation
        //

        public TypeCode GetTypeCode()
        {
            return TypeCode.UInt32;
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
            return m_value;
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
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "UInt32", "DateTime"));
        }

        object IConvertible.ToType(Type type, IFormatProvider? provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }

        //
        // IAdditionOperators
        //

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        static uint IAdditionOperators<uint, uint, uint>.operator +(uint left, uint right) => left + right;

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        static uint IAdditionOperators<uint, uint, uint>.operator checked +(uint left, uint right) => checked(left + right);

        //
        // IAdditiveIdentity
        //

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static uint IAdditiveIdentity<uint, uint>.AdditiveIdentity => AdditiveIdentity;

        //
        // IBinaryInteger
        //

        /// <inheritdoc cref="IBinaryInteger{TSelf}.DivRem(TSelf, TSelf)" />
        public static (uint Quotient, uint Remainder) DivRem(uint left, uint right) => Math.DivRem(left, right);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.LeadingZeroCount(TSelf)" />
        public static uint LeadingZeroCount(uint value) => (uint)BitOperations.LeadingZeroCount(value);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.PopCount(TSelf)" />
        public static uint PopCount(uint value) => (uint)BitOperations.PopCount(value);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.RotateLeft(TSelf, int)" />
        public static uint RotateLeft(uint value, int rotateAmount) => BitOperations.RotateLeft(value, rotateAmount);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.RotateRight(TSelf, int)" />
        public static uint RotateRight(uint value, int rotateAmount) => BitOperations.RotateRight(value, rotateAmount);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TrailingZeroCount(TSelf)" />
        public static uint TrailingZeroCount(uint value) => (uint)BitOperations.TrailingZeroCount(value);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.GetShortestBitLength()" />
        long IBinaryInteger<uint>.GetShortestBitLength() => (sizeof(uint) * 8) - LeadingZeroCount(m_value);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.GetByteCount()" />
        int IBinaryInteger<uint>.GetByteCount() => sizeof(uint);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryWriteLittleEndian(Span{byte}, out int)" />
        bool IBinaryInteger<uint>.TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(uint))
            {
                uint value = BitConverter.IsLittleEndian ? m_value : BinaryPrimitives.ReverseEndianness(m_value);
                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);

                bytesWritten = sizeof(uint);
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
        public static bool IsPow2(uint value) => BitOperations.IsPow2(value);

        /// <inheritdoc cref="IBinaryNumber{TSelf}.Log2(TSelf)" />
        public static uint Log2(uint value) => (uint)BitOperations.Log2(value);

        //
        // IBitwiseOperators
        //

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseAnd(TSelf, TOther)" />
        static uint IBitwiseOperators<uint, uint, uint>.operator &(uint left, uint right) => left & right;

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseOr(TSelf, TOther)" />
        static uint IBitwiseOperators<uint, uint, uint>.operator |(uint left, uint right) => left | right;

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_ExclusiveOr(TSelf, TOther)" />
        static uint IBitwiseOperators<uint, uint, uint>.operator ^(uint left, uint right) => left ^ right;

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_OnesComplement(TSelf)" />
        static uint IBitwiseOperators<uint, uint, uint>.operator ~(uint value) => ~value;

        //
        // IComparisonOperators
        //

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_LessThan(TSelf, TOther)" />
        static bool IComparisonOperators<uint, uint>.operator <(uint left, uint right) => left < right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_LessThanOrEqual(TSelf, TOther)" />
        static bool IComparisonOperators<uint, uint>.operator <=(uint left, uint right) => left <= right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_GreaterThan(TSelf, TOther)" />
        static bool IComparisonOperators<uint, uint>.operator >(uint left, uint right) => left > right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_GreaterThanOrEqual(TSelf, TOther)" />
        static bool IComparisonOperators<uint, uint>.operator >=(uint left, uint right) => left >= right;

        //
        // IDecrementOperators
        //

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        static uint IDecrementOperators<uint>.operator --(uint value) => --value;

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        static uint IDecrementOperators<uint>.operator checked --(uint value) => checked(--value);

        //
        // IDivisionOperators
        //

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_Division(TSelf, TOther)" />
        static uint IDivisionOperators<uint, uint, uint>.operator /(uint left, uint right) => left / right;

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_CheckedDivision(TSelf, TOther)" />
        static uint IDivisionOperators<uint, uint, uint>.operator checked /(uint left, uint right) => left / right;

        //
        // IEqualityOperators
        //

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther}.op_Equality(TSelf, TOther)" />
        static bool IEqualityOperators<uint, uint>.operator ==(uint left, uint right) => left == right;

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther}.op_Inequality(TSelf, TOther)" />
        static bool IEqualityOperators<uint, uint>.operator !=(uint left, uint right) => left != right;

        //
        // IIncrementOperators
        //

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_Increment(TSelf)" />
        static uint IIncrementOperators<uint>.operator ++(uint value) => ++value;

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_CheckedIncrement(TSelf)" />
        static uint IIncrementOperators<uint>.operator checked ++(uint value) => checked(++value);

        //
        // IMinMaxValue
        //

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MinValue" />
        static uint IMinMaxValue<uint>.MinValue => MinValue;

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MaxValue" />
        static uint IMinMaxValue<uint>.MaxValue => MaxValue;

        //
        // IModulusOperators
        //

        /// <inheritdoc cref="IModulusOperators{TSelf, TOther, TResult}.op_Modulus(TSelf, TOther)" />
        static uint IModulusOperators<uint, uint, uint>.operator %(uint left, uint right) => left % right;

        //
        // IMultiplicativeIdentity
        //

        /// <inheritdoc cref="IMultiplicativeIdentity{TSelf, TResult}.MultiplicativeIdentity" />
        static uint IMultiplicativeIdentity<uint, uint>.MultiplicativeIdentity => MultiplicativeIdentity;

        //
        // IMultiplyOperators
        //

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_Multiply(TSelf, TOther)" />
        static uint IMultiplyOperators<uint, uint, uint>.operator *(uint left, uint right) => left * right;

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_CheckedMultiply(TSelf, TOther)" />
        static uint IMultiplyOperators<uint, uint, uint>.operator checked *(uint left, uint right) => checked(left * right);

        //
        // INumber
        //

        /// <inheritdoc cref="INumber{TSelf}.Abs(TSelf)" />
        static uint INumber<uint>.Abs(uint value) => value;

        /// <inheritdoc cref="INumber{TSelf}.Clamp(TSelf, TSelf, TSelf)" />
        public static uint Clamp(uint value, uint min, uint max) => Math.Clamp(value, min, max);

        /// <inheritdoc cref="INumber{TSelf}.CopySign(TSelf, TSelf)" />
        static uint INumber<uint>.CopySign(uint value, uint sign) => value;

        /// <inheritdoc cref="INumber{TSelf}.CreateChecked{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint CreateChecked<TOther>(TOther value)
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
                return checked((uint)(decimal)(object)value);
            }
            else if (typeof(TOther) == typeof(double))
            {
                return checked((uint)(double)(object)value);
            }
            else if (typeof(TOther) == typeof(short))
            {
                return checked((uint)(short)(object)value);
            }
            else if (typeof(TOther) == typeof(int))
            {
                return checked((uint)(int)(object)value);
            }
            else if (typeof(TOther) == typeof(long))
            {
                return checked((uint)(long)(object)value);
            }
            else if (typeof(TOther) == typeof(nint))
            {
                return checked((uint)(nint)(object)value);
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                return checked((uint)(sbyte)(object)value);
            }
            else if (typeof(TOther) == typeof(float))
            {
                return checked((uint)(float)(object)value);
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
                return checked((uint)(ulong)(object)value);
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                return checked((uint)(nuint)(object)value);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        /// <inheritdoc cref="INumber{TSelf}.CreateSaturating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint CreateSaturating<TOther>(TOther value)
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
                       (actualValue < 0) ? MinValue : (uint)actualValue;
            }
            else if (typeof(TOther) == typeof(double))
            {
                var actualValue = (double)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < 0) ? MinValue : (uint)actualValue;
            }
            else if (typeof(TOther) == typeof(short))
            {
                var actualValue = (short)(object)value;
                return (actualValue < 0) ? MinValue : (uint)actualValue;
            }
            else if (typeof(TOther) == typeof(int))
            {
                var actualValue = (int)(object)value;
                return (actualValue < 0) ? MinValue : (uint)actualValue;
            }
            else if (typeof(TOther) == typeof(long))
            {
                var actualValue = (long)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < 0) ? MinValue : (uint)actualValue;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                var actualValue = (nint)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < 0) ? MinValue : (uint)actualValue;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                var actualValue = (sbyte)(object)value;
                return (actualValue < 0) ? MinValue : (uint)actualValue;
            }
            else if (typeof(TOther) == typeof(float))
            {
                var actualValue = (float)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < 0) ? MinValue : (uint)actualValue;
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
                return (actualValue > MaxValue) ? MaxValue : (uint)actualValue;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                var actualValue = (nuint)(object)value;
                return (actualValue > MaxValue) ? MaxValue : (uint)actualValue;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        /// <inheritdoc cref="INumber{TSelf}.CreateTruncating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint CreateTruncating<TOther>(TOther value)
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
                return (uint)(decimal)(object)value;
            }
            else if (typeof(TOther) == typeof(double))
            {
                return (uint)(double)(object)value;
            }
            else if (typeof(TOther) == typeof(short))
            {
                return (uint)(short)(object)value;
            }
            else if (typeof(TOther) == typeof(int))
            {
                return (uint)(int)(object)value;
            }
            else if (typeof(TOther) == typeof(long))
            {
                return (uint)(long)(object)value;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                return (uint)(nint)(object)value;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                return (uint)(sbyte)(object)value;
            }
            else if (typeof(TOther) == typeof(float))
            {
                return (uint)(float)(object)value;
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
                return (uint)(ulong)(object)value;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                return (uint)(nuint)(object)value;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        /// <inheritdoc cref="INumber{TSelf}.IsNegative(TSelf)" />
        static bool INumber<uint>.IsNegative(uint value) => false;

        /// <inheritdoc cref="INumber{TSelf}.Max(TSelf, TSelf)" />
        public static uint Max(uint x, uint y) => Math.Max(x, y);

        /// <inheritdoc cref="INumber{TSelf}.MaxMagnitude(TSelf, TSelf)" />
        static uint INumber<uint>.MaxMagnitude(uint x, uint y) => Max(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Min(TSelf, TSelf)" />
        public static uint Min(uint x, uint y) => Math.Min(x, y);

        /// <inheritdoc cref="INumber{TSelf}.MinMagnitude(TSelf, TSelf)" />
        static uint INumber<uint>.MinMagnitude(uint x, uint y) => Min(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Sign(TSelf)" />
        public static int Sign(uint value) => (value == 0) ? 0 : 1;

        /// <inheritdoc cref="INumber{TSelf}.TryCreate{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryCreate<TOther>(TOther value, out uint result)
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

                result = (uint)actualValue;
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

                result = (uint)actualValue;
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

                result = (uint)actualValue;
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

                result = (uint)actualValue;
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

                result = (uint)actualValue;
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

                result = (uint)actualValue;
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

                result = (uint)actualValue;
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

                result = (uint)actualValue;
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

                result = (uint)actualValue;
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

                result = (uint)actualValue;
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
        static uint INumberBase<uint>.One => One;

        /// <inheritdoc cref="INumberBase{TSelf}.Zero" />
        static uint INumberBase<uint>.Zero => Zero;

        //
        // IParsable
        //

        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out uint result) => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // IShiftOperators
        //

        /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_LeftShift(TSelf, int)" />
        static uint IShiftOperators<uint, uint>.operator <<(uint value, int shiftAmount) => value << (int)shiftAmount;

        /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_RightShift(TSelf, int)" />
        static uint IShiftOperators<uint, uint>.operator >>(uint value, int shiftAmount) => value >> (int)shiftAmount;

        // /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_UnsignedRightShift(TSelf, int)" />
        // static uint IShiftOperators<uint, uint>.operator >>>(uint value, int shiftAmount) => value >> (int)shiftAmount;

        //
        // ISpanParsable
        //

        /// <inheritdoc cref="ISpanParsable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static uint Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Integer, provider);

        /// <inheritdoc cref="ISpanParsable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out uint result) => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // ISubtractionOperators
        //

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_Subtraction(TSelf, TOther)" />
        static uint ISubtractionOperators<uint, uint, uint>.operator -(uint left, uint right) => left - right;

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_CheckedSubtraction(TSelf, TOther)" />
        static uint ISubtractionOperators<uint, uint, uint>.operator checked -(uint left, uint right) => checked(left - right);

        //
        // IUnaryNegationOperators
        //

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_UnaryNegation(TSelf)" />
        static uint IUnaryNegationOperators<uint, uint>.operator -(uint value) => 0u - value;

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_CheckedUnaryNegation(TSelf)" />
        static uint IUnaryNegationOperators<uint, uint>.operator checked -(uint value) => checked(0u - value);

        //
        // IUnaryPlusOperators
        //

        /// <inheritdoc cref="IUnaryPlusOperators{TSelf, TResult}.op_UnaryPlus(TSelf)" />
        static uint IUnaryPlusOperators<uint, uint>.operator +(uint value) => +value;
    }
}
