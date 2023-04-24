// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
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
          ISignedNumber<sbyte>,
          IUtf8SpanFormattable,
          IBinaryIntegerParseAndFormatInfo<sbyte>
    {
        private readonly sbyte m_value; // Do not rename (binary serialization)

        // The maximum value that a Byte may represent: 127.
        public const sbyte MaxValue = (sbyte)0x7F;

        // The minimum value that a Byte may represent: -128.
        public const sbyte MinValue = unchecked((sbyte)0x80);

        /// <summary>Represents the additive identity (0).</summary>
        private const sbyte AdditiveIdentity = 0;

        /// <summary>Represents the multiplicative identity (1).</summary>
        private const sbyte MultiplicativeIdentity = 1;

        /// <summary>Represents the number one (1).</summary>
        private const sbyte One = 1;

        /// <summary>Represents the number zero (0).</summary>
        private const sbyte Zero = 0;

        /// <summary>Represents the number negative one (-1).</summary>
        private const sbyte NegativeOne = -1;

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

        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
        {
            return ToString(format, null);
        }

        public string ToString(IFormatProvider? provider)
        {
            return Number.FormatInt32(m_value, 0, null, provider);
        }

        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider)
        {
            return Number.FormatInt32(m_value, 0x000000FF, format, provider);
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatInt32(m_value, 0x000000FF, format, provider, destination, out charsWritten);
        }

        /// <inheritdoc cref="IUtf8SpanFormattable.TryFormat" />
        public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatInt32(m_value, 0x000000FF, format, provider, utf8Destination, out bytesWritten);
        }

        public static sbyte Parse(string s) => Parse(s, NumberStyles.Integer, provider: null);

        public static sbyte Parse(string s, NumberStyles style) => Parse(s, style, provider: null);

        public static sbyte Parse(string s, IFormatProvider? provider) => Parse(s, NumberStyles.Integer, provider);

        public static sbyte Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            if (s is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s); }
            return Parse(s.AsSpan(), style, provider);
        }

        public static sbyte Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.ParseBinaryInteger<sbyte>(s, style, NumberFormatInfo.GetInstance(provider));
        }

        public static bool TryParse([NotNullWhen(true)] string? s, out sbyte result) => TryParse(s, NumberStyles.Integer, provider: null, out result);

        public static bool TryParse(ReadOnlySpan<char> s, out sbyte result) => TryParse(s, NumberStyles.Integer, provider: null, out result);

        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out sbyte result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);

            if (s is null)
            {
                result = 0;
                return false;
            }
            return Number.TryParseBinaryInteger(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
        }

        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out sbyte result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.TryParseBinaryInteger(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
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

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        static sbyte IAdditionOperators<sbyte, sbyte, sbyte>.operator +(sbyte left, sbyte right) => (sbyte)(left + right);

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        static sbyte IAdditionOperators<sbyte, sbyte, sbyte>.operator checked +(sbyte left, sbyte right) => checked((sbyte)(left + right));

        //
        // IAdditiveIdentity
        //

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static sbyte IAdditiveIdentity<sbyte, sbyte>.AdditiveIdentity => AdditiveIdentity;

        //
        // IBinaryInteger
        //

        /// <inheritdoc cref="IBinaryInteger{TSelf}.DivRem(TSelf, TSelf)" />
        public static (sbyte Quotient, sbyte Remainder) DivRem(sbyte left, sbyte right) => Math.DivRem(left, right);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.LeadingZeroCount(TSelf)" />
        public static sbyte LeadingZeroCount(sbyte value) => (sbyte)(BitOperations.LeadingZeroCount((byte)value) - 24);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.PopCount(TSelf)" />
        public static sbyte PopCount(sbyte value) => (sbyte)BitOperations.PopCount((byte)value);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.RotateLeft(TSelf, int)" />
        public static sbyte RotateLeft(sbyte value, int rotateAmount) => (sbyte)((value << (rotateAmount & 7)) | ((byte)value >> ((8 - rotateAmount) & 7)));

        /// <inheritdoc cref="IBinaryInteger{TSelf}.RotateRight(TSelf, int)" />
        public static sbyte RotateRight(sbyte value, int rotateAmount) => (sbyte)(((byte)value >> (rotateAmount & 7)) | (value << ((8 - rotateAmount) & 7)));

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TrailingZeroCount(TSelf)" />
        public static sbyte TrailingZeroCount(sbyte value) => (sbyte)(BitOperations.TrailingZeroCount(value << 24) - 24);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryReadBigEndian(ReadOnlySpan{byte}, bool, out TSelf)" />
        static bool IBinaryInteger<sbyte>.TryReadBigEndian(ReadOnlySpan<byte> source, bool isUnsigned, out sbyte value)
        {
            sbyte result = default;

            if (source.Length != 0)
            {
                // Propagate the most significant bit so we have `0` or `-1`
                sbyte sign = (sbyte)(source[0]);
                sign >>= 31;
                Debug.Assert((sign == 0) || (sign == -1));

                // We need to also track if the input data is unsigned
                isUnsigned |= (sign == 0);

                if (isUnsigned && sbyte.IsNegative(sign))
                {
                    // When we are unsigned and the most significant bit is set, we are a large positive
                    // and therefore definitely out of range

                    value = result;
                    return false;
                }

                if (source.Length > sizeof(sbyte))
                {
                    if (source[..^sizeof(sbyte)].IndexOfAnyExcept((byte)sign) >= 0)
                    {
                        // When we are unsigned and have any non-zero leading data or signed with any non-set leading
                        // data, we are a large positive/negative, respectively, and therefore definitely out of range

                        value = result;
                        return false;
                    }

                    if (isUnsigned == sbyte.IsNegative((sbyte)source[^sizeof(sbyte)]))
                    {
                        // When the most significant bit of the value being set/clear matches whether we are unsigned
                        // or signed then we are a large positive/negative and therefore definitely out of range

                        value = result;
                        return false;
                    }
                }

                // We only have 1-byte so read it directly
                result = (sbyte)Unsafe.Add(ref MemoryMarshal.GetReference(source), source.Length - sizeof(sbyte));
            }

            value = result;
            return true;
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryReadLittleEndian(ReadOnlySpan{byte}, bool, out TSelf)" />
        static bool IBinaryInteger<sbyte>.TryReadLittleEndian(ReadOnlySpan<byte> source, bool isUnsigned, out sbyte value)
        {
            sbyte result = default;

            if (source.Length != 0)
            {
                // Propagate the most significant bit so we have `0` or `-1`
                sbyte sign = (sbyte)(source[^1]);
                sign >>= 31;
                Debug.Assert((sign == 0) || (sign == -1));

                // We need to also track if the input data is unsigned
                isUnsigned |= (sign == 0);

                if (isUnsigned && sbyte.IsNegative(sign))
                {
                    // When we are unsigned and the most significant bit is set, we are a large positive
                    // and therefore definitely out of range

                    value = result;
                    return false;
                }

                if (source.Length > sizeof(sbyte))
                {
                    if (source[sizeof(sbyte)..].IndexOfAnyExcept((byte)sign) >= 0)
                    {
                        // When we are unsigned and have any non-zero leading data or signed with any non-set leading
                        // data, we are a large positive/negative, respectively, and therefore definitely out of range

                        value = result;
                        return false;
                    }

                    if (isUnsigned == sbyte.IsNegative((sbyte)source[sizeof(sbyte) - 1]))
                    {
                        // When the most significant bit of the value being set/clear matches whether we are unsigned
                        // or signed then we are a large positive/negative and therefore definitely out of range

                        value = result;
                        return false;
                    }
                }

                // We only have 1-byte so read it directly
                result = (sbyte)MemoryMarshal.GetReference(source);
            }

            value = result;
            return true;
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.GetShortestBitLength()" />
        int IBinaryInteger<sbyte>.GetShortestBitLength()
        {
            sbyte value = m_value;

            if (value >= 0)
            {
                return (sizeof(sbyte) * 8) - LeadingZeroCount(value);
            }
            else
            {
                return (sizeof(sbyte) * 8) + 1 - LeadingZeroCount((sbyte)(~value));
            }
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.GetByteCount()" />
        int IBinaryInteger<sbyte>.GetByteCount() => sizeof(sbyte);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryWriteBigEndian(Span{byte}, out int)" />
        bool IBinaryInteger<sbyte>.TryWriteBigEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(sbyte))
            {
                sbyte value = m_value;
                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);

                bytesWritten = sizeof(sbyte);
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryWriteLittleEndian(Span{byte}, out int)" />
        bool IBinaryInteger<sbyte>.TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(sbyte))
            {
                sbyte value = m_value;
                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);

                bytesWritten = sizeof(sbyte);
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

        /// <inheritdoc cref="IBinaryNumber{TSelf}.AllBitsSet" />
        static sbyte IBinaryNumber<sbyte>.AllBitsSet => NegativeOne;

        /// <inheritdoc cref="IBinaryNumber{TSelf}.IsPow2(TSelf)" />
        public static bool IsPow2(sbyte value) => BitOperations.IsPow2(value);

        /// <inheritdoc cref="IBinaryNumber{TSelf}.Log2(TSelf)" />
        public static sbyte Log2(sbyte value)
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

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseAnd(TSelf, TOther)" />
        static sbyte IBitwiseOperators<sbyte, sbyte, sbyte>.operator &(sbyte left, sbyte right) => (sbyte)(left & right);

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseOr(TSelf, TOther)" />
        static sbyte IBitwiseOperators<sbyte, sbyte, sbyte>.operator |(sbyte left, sbyte right) => (sbyte)(left | right);

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_ExclusiveOr(TSelf, TOther)" />
        static sbyte IBitwiseOperators<sbyte, sbyte, sbyte>.operator ^(sbyte left, sbyte right) => (sbyte)(left ^ right);

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_OnesComplement(TSelf)" />
        static sbyte IBitwiseOperators<sbyte, sbyte, sbyte>.operator ~(sbyte value) => (sbyte)(~value);

        //
        // IComparisonOperators
        //

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_LessThan(TSelf, TOther)" />
        static bool IComparisonOperators<sbyte, sbyte, bool>.operator <(sbyte left, sbyte right) => left < right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_LessThanOrEqual(TSelf, TOther)" />
        static bool IComparisonOperators<sbyte, sbyte, bool>.operator <=(sbyte left, sbyte right) => left <= right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_GreaterThan(TSelf, TOther)" />
        static bool IComparisonOperators<sbyte, sbyte, bool>.operator >(sbyte left, sbyte right) => left > right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_GreaterThanOrEqual(TSelf, TOther)" />
        static bool IComparisonOperators<sbyte, sbyte, bool>.operator >=(sbyte left, sbyte right) => left >= right;

        //
        // IDecrementOperators
        //

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        static sbyte IDecrementOperators<sbyte>.operator --(sbyte value) => --value;

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        static sbyte IDecrementOperators<sbyte>.operator checked --(sbyte value) => checked(--value);

        //
        // IDivisionOperators
        //

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_Division(TSelf, TOther)" />
        static sbyte IDivisionOperators<sbyte, sbyte, sbyte>.operator /(sbyte left, sbyte right) => (sbyte)(left / right);

        //
        // IEqualityOperators
        //

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther, TResult}.op_Equality(TSelf, TOther)" />
        static bool IEqualityOperators<sbyte, sbyte, bool>.operator ==(sbyte left, sbyte right) => left == right;

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther, TResult}.op_Inequality(TSelf, TOther)" />
        static bool IEqualityOperators<sbyte, sbyte, bool>.operator !=(sbyte left, sbyte right) => left != right;

        //
        // IIncrementOperators
        //

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_Increment(TSelf)" />
        static sbyte IIncrementOperators<sbyte>.operator ++(sbyte value) => ++value;

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_CheckedIncrement(TSelf)" />
        static sbyte IIncrementOperators<sbyte>.operator checked ++(sbyte value) => checked(++value);

        //
        // IMinMaxValue
        //

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MinValue" />
        static sbyte IMinMaxValue<sbyte>.MinValue => MinValue;

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MaxValue" />
        static sbyte IMinMaxValue<sbyte>.MaxValue => MaxValue;

        //
        // IModulusOperators
        //

        /// <inheritdoc cref="IModulusOperators{TSelf, TOther, TResult}.op_Modulus(TSelf, TOther)" />
        static sbyte IModulusOperators<sbyte, sbyte, sbyte>.operator %(sbyte left, sbyte right) => (sbyte)(left % right);

        //
        // IMultiplicativeIdentity
        //

        /// <inheritdoc cref="IMultiplicativeIdentity{TSelf, TResult}.MultiplicativeIdentity" />
        static sbyte IMultiplicativeIdentity<sbyte, sbyte>.MultiplicativeIdentity => MultiplicativeIdentity;

        //
        // IMultiplyOperators
        //

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_Multiply(TSelf, TOther)" />
        static sbyte IMultiplyOperators<sbyte, sbyte, sbyte>.operator *(sbyte left, sbyte right) => (sbyte)(left * right);

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_CheckedMultiply(TSelf, TOther)" />
        static sbyte IMultiplyOperators<sbyte, sbyte, sbyte>.operator checked *(sbyte left, sbyte right) => checked((sbyte)(left * right));

        //
        // INumber
        //

        /// <inheritdoc cref="INumber{TSelf}.Clamp(TSelf, TSelf, TSelf)" />
        public static sbyte Clamp(sbyte value, sbyte min, sbyte max) => Math.Clamp(value, min, max);

        /// <inheritdoc cref="INumber{TSelf}.CopySign(TSelf, TSelf)" />
        public static sbyte CopySign(sbyte value, sbyte sign)
        {
            sbyte absValue = value;

            if (absValue < 0)
            {
                absValue = (sbyte)(-absValue);
            }

            if (sign >= 0)
            {
                if (absValue < 0)
                {
                    Math.ThrowNegateTwosCompOverflow();
                }

                return absValue;
            }

            return (sbyte)(-absValue);
        }

        /// <inheritdoc cref="INumber{TSelf}.Max(TSelf, TSelf)" />
        public static sbyte Max(sbyte x, sbyte y) => Math.Max(x, y);

        /// <inheritdoc cref="INumber{TSelf}.MaxNumber(TSelf, TSelf)" />
        static sbyte INumber<sbyte>.MaxNumber(sbyte x, sbyte y) => Max(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Min(TSelf, TSelf)" />
        public static sbyte Min(sbyte x, sbyte y) => Math.Min(x, y);

        /// <inheritdoc cref="INumber{TSelf}.MinNumber(TSelf, TSelf)" />
        static sbyte INumber<sbyte>.MinNumber(sbyte x, sbyte y) => Min(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Sign(TSelf)" />
        public static int Sign(sbyte value) => Math.Sign(value);

        //
        // INumberBase
        //

        /// <inheritdoc cref="INumberBase{TSelf}.One" />
        static sbyte INumberBase<sbyte>.One => One;

        /// <inheritdoc cref="INumberBase{TSelf}.Radix" />
        static int INumberBase<sbyte>.Radix => 2;

        /// <inheritdoc cref="INumberBase{TSelf}.Zero" />
        static sbyte INumberBase<sbyte>.Zero => Zero;

        /// <inheritdoc cref="INumberBase{TSelf}.Abs(TSelf)" />
        public static sbyte Abs(sbyte value) => Math.Abs(value);

        /// <inheritdoc cref="INumberBase{TSelf}.CreateChecked{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte CreateChecked<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            sbyte result;

            if (typeof(TOther) == typeof(sbyte))
            {
                result = (sbyte)(object)value;
            }
            else if (!TryConvertFromChecked(value, out result) && !TOther.TryConvertToChecked(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateSaturating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte CreateSaturating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            sbyte result;

            if (typeof(TOther) == typeof(sbyte))
            {
                result = (sbyte)(object)value;
            }
            else if (!TryConvertFromSaturating(value, out result) && !TOther.TryConvertToSaturating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateTruncating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte CreateTruncating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            sbyte result;

            if (typeof(TOther) == typeof(sbyte))
            {
                result = (sbyte)(object)value;
            }
            else if (!TryConvertFromTruncating(value, out result) && !TOther.TryConvertToTruncating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsCanonical(TSelf)" />
        static bool INumberBase<sbyte>.IsCanonical(sbyte value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsComplexNumber(TSelf)" />
        static bool INumberBase<sbyte>.IsComplexNumber(sbyte value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsEvenInteger(TSelf)" />
        public static bool IsEvenInteger(sbyte value) => (value & 1) == 0;

        /// <inheritdoc cref="INumberBase{TSelf}.IsFinite(TSelf)" />
        static bool INumberBase<sbyte>.IsFinite(sbyte value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsImaginaryNumber(TSelf)" />
        static bool INumberBase<sbyte>.IsImaginaryNumber(sbyte value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsInfinity(TSelf)" />
        static bool INumberBase<sbyte>.IsInfinity(sbyte value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsNaN(TSelf)" />
        static bool INumberBase<sbyte>.IsNaN(sbyte value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsInteger(TSelf)" />
        static bool INumberBase<sbyte>.IsInteger(sbyte value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsNegative(TSelf)" />
        public static bool IsNegative(sbyte value) => value < 0;

        /// <inheritdoc cref="INumberBase{TSelf}.IsNegativeInfinity(TSelf)" />
        static bool INumberBase<sbyte>.IsNegativeInfinity(sbyte value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsNormal(TSelf)" />
        static bool INumberBase<sbyte>.IsNormal(sbyte value) => value != 0;

        /// <inheritdoc cref="INumberBase{TSelf}.IsOddInteger(TSelf)" />
        public static bool IsOddInteger(sbyte value) => (value & 1) != 0;

        /// <inheritdoc cref="INumberBase{TSelf}.IsPositive(TSelf)" />
        public static bool IsPositive(sbyte value) => value >= 0;

        /// <inheritdoc cref="INumberBase{TSelf}.IsPositiveInfinity(TSelf)" />
        static bool INumberBase<sbyte>.IsPositiveInfinity(sbyte value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsRealNumber(TSelf)" />
        static bool INumberBase<sbyte>.IsRealNumber(sbyte value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsSubnormal(TSelf)" />
        static bool INumberBase<sbyte>.IsSubnormal(sbyte value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsZero(TSelf)" />
        static bool INumberBase<sbyte>.IsZero(sbyte value) => (value == 0);

        /// <inheritdoc cref="INumberBase{TSelf}.MaxMagnitude(TSelf, TSelf)" />
        public static sbyte MaxMagnitude(sbyte x, sbyte y)
        {
            sbyte absX = x;

            if (absX < 0)
            {
                absX = (sbyte)(-absX);

                if (absX < 0)
                {
                    return x;
                }
            }

            sbyte absY = y;

            if (absY < 0)
            {
                absY = (sbyte)(-absY);

                if (absY < 0)
                {
                    return y;
                }
            }

            if (absX > absY)
            {
                return x;
            }

            if (absX == absY)
            {
                return IsNegative(x) ? y : x;
            }

            return y;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.MaxMagnitudeNumber(TSelf, TSelf)" />
        static sbyte INumberBase<sbyte>.MaxMagnitudeNumber(sbyte x, sbyte y) => MaxMagnitude(x, y);

        /// <inheritdoc cref="INumberBase{TSelf}.MinMagnitude(TSelf, TSelf)" />
        public static sbyte MinMagnitude(sbyte x, sbyte y)
        {
            sbyte absX = x;

            if (absX < 0)
            {
                absX = (sbyte)(-absX);

                if (absX < 0)
                {
                    return y;
                }
            }

            sbyte absY = y;

            if (absY < 0)
            {
                absY = (sbyte)(-absY);

                if (absY < 0)
                {
                    return x;
                }
            }

            if (absX < absY)
            {
                return x;
            }

            if (absX == absY)
            {
                return IsNegative(x) ? x : y;
            }

            return y;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.MinMagnitudeNumber(TSelf, TSelf)" />
        static sbyte INumberBase<sbyte>.MinMagnitudeNumber(sbyte x, sbyte y) => MinMagnitude(x, y);

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromChecked{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<sbyte>.TryConvertFromChecked<TOther>(TOther value, out sbyte result) => TryConvertFromChecked(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvertFromChecked<TOther>(TOther value, out sbyte result)
            where TOther : INumberBase<TOther>
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `sbyte` will handle the other signed types and
            // `ConvertTo` will handle the unsigned types

            if (typeof(TOther) == typeof(double))
            {
                double actualValue = (double)(object)value;
                result = checked((sbyte)actualValue);
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualValue = (Half)(object)value;
                result = checked((sbyte)actualValue);
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualValue = (short)(object)value;
                result = checked((sbyte)actualValue);
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualValue = (int)(object)value;
                result = checked((sbyte)actualValue);
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualValue = (long)(object)value;
                result = checked((sbyte)actualValue);
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualValue = (Int128)(object)value;
                result = checked((sbyte)actualValue);
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualValue = (nint)(object)value;
                result = checked((sbyte)actualValue);
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualValue = (float)(object)value;
                result = checked((sbyte)actualValue);
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromSaturating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<sbyte>.TryConvertFromSaturating<TOther>(TOther value, out sbyte result) => TryConvertFromSaturating(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvertFromSaturating<TOther>(TOther value, out sbyte result)
            where TOther : INumberBase<TOther>
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `sbyte` will handle the other signed types and
            // `ConvertTo` will handle the unsigned types

            if (typeof(TOther) == typeof(double))
            {
                double actualValue = (double)(object)value;
                result = (actualValue >= MaxValue) ? MaxValue :
                         (actualValue <= MinValue) ? MinValue : (sbyte)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualValue = (Half)(object)value;
                result = (actualValue >= MaxValue) ? MaxValue :
                         (actualValue <= MinValue) ? MinValue : (sbyte)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualValue = (short)(object)value;
                result = (actualValue >= MaxValue) ? MaxValue :
                         (actualValue <= MinValue) ? MinValue : (sbyte)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualValue = (int)(object)value;
                result = (actualValue >= MaxValue) ? MaxValue :
                         (actualValue <= MinValue) ? MinValue : (sbyte)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualValue = (long)(object)value;
                result = (actualValue >= MaxValue) ? MaxValue :
                         (actualValue <= MinValue) ? MinValue : (sbyte)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualValue = (Int128)(object)value;
                result = (actualValue >= MaxValue) ? MaxValue :
                         (actualValue <= MinValue) ? MinValue : (sbyte)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualValue = (nint)(object)value;
                result = (actualValue >= MaxValue) ? MaxValue :
                         (actualValue <= MinValue) ? MinValue : (sbyte)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualValue = (float)(object)value;
                result = (actualValue >= MaxValue) ? MaxValue :
                         (actualValue <= MinValue) ? MinValue : (sbyte)actualValue;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromTruncating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<sbyte>.TryConvertFromTruncating<TOther>(TOther value, out sbyte result) => TryConvertFromTruncating(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvertFromTruncating<TOther>(TOther value, out sbyte result)
            where TOther : INumberBase<TOther>
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `sbyte` will handle the other signed types and
            // `ConvertTo` will handle the unsigned types

            if (typeof(TOther) == typeof(double))
            {
                double actualValue = (double)(object)value;
                result = (actualValue >= MaxValue) ? MaxValue :
                         (actualValue <= MinValue) ? MinValue : (sbyte)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualValue = (Half)(object)value;
                result = (actualValue >= MaxValue) ? MaxValue :
                         (actualValue <= MinValue) ? MinValue : (sbyte)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualValue = (short)(object)value;
                result = (sbyte)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualValue = (int)(object)value;
                result = (sbyte)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualValue = (long)(object)value;
                result = (sbyte)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualValue = (Int128)(object)value;
                result = (sbyte)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualValue = (nint)(object)value;
                result = (sbyte)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualValue = (float)(object)value;
                result = (actualValue >= MaxValue) ? MaxValue :
                         (actualValue <= MinValue) ? MinValue : (sbyte)actualValue;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToChecked{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<sbyte>.TryConvertToChecked<TOther>(sbyte value, [MaybeNullWhen(false)] out TOther result)
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `sbyte` will handle the other signed types and
            // `ConvertTo` will handle the unsigned types

            if (typeof(TOther) == typeof(byte))
            {
                byte actualResult = checked((byte)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualResult = checked((char)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualResult = value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                ushort actualResult = checked((ushort)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualResult = checked((uint)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualResult = checked((ulong)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualResult = checked((UInt128)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                nuint actualResult = checked((nuint)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToSaturating{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<sbyte>.TryConvertToSaturating<TOther>(sbyte value, [MaybeNullWhen(false)] out TOther result)
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `sbyte` will handle the other signed types and
            // `ConvertTo` will handle the unsigned types

            if (typeof(TOther) == typeof(byte))
            {
                byte actualResult = (value <= 0) ? byte.MinValue : (byte)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualResult = (value <= 0) ? char.MinValue : (char)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualResult = value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                ushort actualResult = (value <= 0) ? ushort.MinValue : (ushort)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualResult = (value <= 0) ? uint.MinValue : (uint)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualResult = (value <= 0) ? ulong.MinValue : (ulong)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualResult = (value <= 0) ? UInt128.MinValue : (UInt128)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                nuint actualResult = (value <= 0) ? 0 : (nuint)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToTruncating{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<sbyte>.TryConvertToTruncating<TOther>(sbyte value, [MaybeNullWhen(false)] out TOther result)
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `sbyte` will handle the other signed types and
            // `ConvertTo` will handle the unsigned types

            if (typeof(TOther) == typeof(byte))
            {
                byte actualResult = (byte)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualResult = (char)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualResult = value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                ushort actualResult = (ushort)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualResult = (uint)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualResult = (ulong)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualResult = (UInt128)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                nuint actualResult = (nuint)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        //
        // IParsable
        //

        /// <inheritdoc cref="IParsable{TSelf}.TryParse(string?, IFormatProvider?, out TSelf)" />
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out sbyte result) => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // IShiftOperators
        //

        /// <inheritdoc cref="IShiftOperators{TSelf, TOther, TResult}.op_LeftShift(TSelf, TOther)" />
        static sbyte IShiftOperators<sbyte, int, sbyte>.operator <<(sbyte value, int shiftAmount) => (sbyte)(value << shiftAmount);

        /// <inheritdoc cref="IShiftOperators{TSelf, TOther, TResult}.op_RightShift(TSelf, TOther)" />
        static sbyte IShiftOperators<sbyte, int, sbyte>.operator >>(sbyte value, int shiftAmount) => (sbyte)(value >> shiftAmount);

        /// <inheritdoc cref="IShiftOperators{TSelf, TOther, TResult}.op_UnsignedRightShift(TSelf, TOther)" />
        static sbyte IShiftOperators<sbyte, int, sbyte>.operator >>>(sbyte value, int shiftAmount) => (sbyte)((byte)value >>> shiftAmount);

        //
        // ISignedNumber
        //

        /// <inheritdoc cref="ISignedNumber{TSelf}.NegativeOne" />
        static sbyte ISignedNumber<sbyte>.NegativeOne => NegativeOne;

        //
        // ISpanParsable
        //

        /// <inheritdoc cref="ISpanParsable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static sbyte Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Integer, provider);

        /// <inheritdoc cref="ISpanParsable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out sbyte result) => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // ISubtractionOperators
        //

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_Subtraction(TSelf, TOther)" />
        static sbyte ISubtractionOperators<sbyte, sbyte, sbyte>.operator -(sbyte left, sbyte right) => (sbyte)(left - right);

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_CheckedSubtraction(TSelf, TOther)" />
        static sbyte ISubtractionOperators<sbyte, sbyte, sbyte>.operator checked -(sbyte left, sbyte right) => checked((sbyte)(left - right));

        //
        // IUnaryNegationOperators
        //

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_UnaryNegation(TSelf)" />
        static sbyte IUnaryNegationOperators<sbyte, sbyte>.operator -(sbyte value) => (sbyte)(-value);

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_CheckedUnaryNegation(TSelf)" />
        static sbyte IUnaryNegationOperators<sbyte, sbyte>.operator checked -(sbyte value) => checked((sbyte)(-value));

        //
        // IUnaryPlusOperators
        //

        /// <inheritdoc cref="IUnaryPlusOperators{TSelf, TResult}.op_UnaryPlus(TSelf)" />
        static sbyte IUnaryPlusOperators<sbyte, sbyte>.operator +(sbyte value) => (sbyte)(+value);

        //
        // IBinaryIntegerParseAndFormatInfo
        //

        static bool IBinaryIntegerParseAndFormatInfo<sbyte>.IsSigned => true;

        static int IBinaryIntegerParseAndFormatInfo<sbyte>.MaxDigitCount => 3; // 127

        static int IBinaryIntegerParseAndFormatInfo<sbyte>.MaxHexDigitCount => 2; // 0x7F

        static sbyte IBinaryIntegerParseAndFormatInfo<sbyte>.MaxValueDiv10 => MaxValue / 10;

        static string IBinaryIntegerParseAndFormatInfo<sbyte>.OverflowMessage => SR.Overflow_SByte;

        static bool IBinaryIntegerParseAndFormatInfo<sbyte>.IsGreaterThanAsUnsigned(sbyte left, sbyte right) => (byte)(left) > (byte)(right);

        static sbyte IBinaryIntegerParseAndFormatInfo<sbyte>.MultiplyBy10(sbyte value) => (sbyte)(value * 10);

        static sbyte IBinaryIntegerParseAndFormatInfo<sbyte>.MultiplyBy16(sbyte value) => (sbyte)(value * 16);
    }
}
