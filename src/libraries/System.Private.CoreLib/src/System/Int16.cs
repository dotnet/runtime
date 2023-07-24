// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
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
          ISignedNumber<short>,
          IUtf8SpanFormattable,
          IBinaryIntegerParseAndFormatInfo<short>
    {
        private readonly short m_value; // Do not rename (binary serialization)

        public const short MaxValue = (short)0x7FFF;
        public const short MinValue = unchecked((short)0x8000);

        /// <summary>Represents the additive identity (0).</summary>
        private const short AdditiveIdentity = 0;

        /// <summary>Represents the multiplicative identity (1).</summary>
        private const short MultiplicativeIdentity = 1;

        /// <summary>Represents the number one (1).</summary>
        private const short One = 1;

        /// <summary>Represents the number zero (0).</summary>
        private const short Zero = 0;

        /// <summary>Represents the number negative one (-1).</summary>
        private const short NegativeOne = -1;

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

        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
        {
            return ToString(format, null);
        }

        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider)
        {
            return Number.FormatInt32(m_value, 0x0000FFFF, format, provider);
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatInt32(m_value, 0x0000FFFF, format, provider, destination, out charsWritten);
        }

        /// <inheritdoc cref="IUtf8SpanFormattable.TryFormat" />
        public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatInt32(m_value, 0x0000FFFF, format, provider, utf8Destination, out bytesWritten);
        }

        public static short Parse(string s) => Parse(s, NumberStyles.Integer, provider: null);

        public static short Parse(string s, NumberStyles style) => Parse(s, style, provider: null);

        public static short Parse(string s, IFormatProvider? provider) => Parse(s, NumberStyles.Integer, provider);

        public static short Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            if (s is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s); }
            return Parse(s.AsSpan(), style, provider);
        }

        public static short Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.ParseBinaryInteger<char, short>(s, style, NumberFormatInfo.GetInstance(provider));
        }

        public static bool TryParse([NotNullWhen(true)] string? s, out short result) => TryParse(s, NumberStyles.Integer, provider: null, out result);

        public static bool TryParse(ReadOnlySpan<char> s, out short result) => TryParse(s, NumberStyles.Integer, provider: null, out result);

        /// <summary>Tries to convert a UTF-8 character span containing the string representation of a number to its 16-bit signed integer equivalent.</summary>
        /// <param name="utf8Text">A span containing the UTF-8 characters representing the number to convert.</param>
        /// <param name="result">When this method returns, contains the 16-bit signed integer value equivalent to the number contained in <paramref name="utf8Text" /> if the conversion succeeded, or zero if the conversion failed. This parameter is passed uninitialized; any value originally supplied in result will be overwritten.</param>
        /// <returns><c>true</c> if <paramref name="utf8Text" /> was converted successfully; otherwise, false.</returns>
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, out short result) => TryParse(utf8Text, NumberStyles.Integer, provider: null, out result);

        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out short result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);

            if (s is null)
            {
                result = 0;
                return false;
            }
            return Number.TryParseBinaryInteger(s.AsSpan(), style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
        }

        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out short result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.TryParseBinaryInteger(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
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

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        static short IAdditionOperators<short, short, short>.operator +(short left, short right) => (short)(left + right);

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        static short IAdditionOperators<short, short, short>.operator checked +(short left, short right) => checked((short)(left + right));

        //
        // IAdditiveIdentity
        //

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static short IAdditiveIdentity<short, short>.AdditiveIdentity => AdditiveIdentity;

        //
        // IBinaryInteger
        //

        /// <inheritdoc cref="IBinaryInteger{TSelf}.DivRem(TSelf, TSelf)" />
        public static (short Quotient, short Remainder) DivRem(short left, short right) => Math.DivRem(left, right);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.LeadingZeroCount(TSelf)" />
        public static short LeadingZeroCount(short value) => (short)(BitOperations.LeadingZeroCount((ushort)value) - 16);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.PopCount(TSelf)" />
        public static short PopCount(short value) => (short)BitOperations.PopCount((ushort)value);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.RotateLeft(TSelf, int)" />
        public static short RotateLeft(short value, int rotateAmount) => (short)((value << (rotateAmount & 15)) | ((ushort)value >> ((16 - rotateAmount) & 15)));

        /// <inheritdoc cref="IBinaryInteger{TSelf}.RotateRight(TSelf, int)" />
        public static short RotateRight(short value, int rotateAmount) => (short)(((ushort)value >> (rotateAmount & 15)) | (value << ((16 - rotateAmount) & 15)));

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TrailingZeroCount(TSelf)" />
        public static short TrailingZeroCount(short value) => (byte)(BitOperations.TrailingZeroCount(value << 16) - 16);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryReadBigEndian(ReadOnlySpan{byte}, bool, out TSelf)" />
        static bool IBinaryInteger<short>.TryReadBigEndian(ReadOnlySpan<byte> source, bool isUnsigned, out short value)
        {
            short result = default;

            if (source.Length != 0)
            {
                // Propagate the most significant bit so we have `0` or `-1`
                sbyte sign = (sbyte)(source[0]);
                sign >>= 31;
                Debug.Assert((sign == 0) || (sign == -1));

                // We need to also track if the input data is unsigned
                isUnsigned |= (sign == 0);

                if (isUnsigned && sbyte.IsNegative(sign) && (source.Length >= sizeof(short)))
                {
                    // When we are unsigned and the most significant bit is set, we are a large positive
                    // and therefore definitely out of range

                    value = result;
                    return false;
                }

                if (source.Length > sizeof(short))
                {
                    if (source[..^sizeof(short)].ContainsAnyExcept((byte)sign))
                    {
                        // When we are unsigned and have any non-zero leading data or signed with any non-set leading
                        // data, we are a large positive/negative, respectively, and therefore definitely out of range

                        value = result;
                        return false;
                    }

                    if (isUnsigned == sbyte.IsNegative((sbyte)source[^sizeof(short)]))
                    {
                        // When the most significant bit of the value being set/clear matches whether we are unsigned
                        // or signed then we are a large positive/negative and therefore definitely out of range

                        value = result;
                        return false;
                    }
                }

                ref byte sourceRef = ref MemoryMarshal.GetReference(source);

                if (source.Length >= sizeof(short))
                {
                    sourceRef = ref Unsafe.Add(ref sourceRef, source.Length - sizeof(short));

                    // We have at least 2 bytes, so just read the ones we need directly
                    result = Unsafe.ReadUnaligned<short>(ref sourceRef);

                    if (BitConverter.IsLittleEndian)
                    {
                        result = BinaryPrimitives.ReverseEndianness(result);
                    }
                }
                else if (isUnsigned)
                {
                    // We only have 1-byte so read it directly
                    result = sourceRef;
                }
                else
                {
                    // We only have 1-byte so read it directly with sign extension
                    result = (sbyte)sourceRef;
                }
            }

            value = result;
            return true;
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryReadLittleEndian(ReadOnlySpan{byte}, bool, out TSelf)" />
        static bool IBinaryInteger<short>.TryReadLittleEndian(ReadOnlySpan<byte> source, bool isUnsigned, out short value)
        {
            short result = default;

            if (source.Length != 0)
            {
                // Propagate the most significant bit so we have `0` or `-1`
                sbyte sign = (sbyte)(source[^1]);
                sign >>= 31;
                Debug.Assert((sign == 0) || (sign == -1));

                // We need to also track if the input data is unsigned
                isUnsigned |= (sign == 0);

                if (isUnsigned && sbyte.IsNegative(sign) && (source.Length >= sizeof(short)))
                {
                    // When we are unsigned and the most significant bit is set, we are a large positive
                    // and therefore definitely out of range

                    value = result;
                    return false;
                }

                if (source.Length > sizeof(short))
                {
                    if (source[sizeof(short)..].ContainsAnyExcept((byte)sign))
                    {
                        // When we are unsigned and have any non-zero leading data or signed with any non-set leading
                        // data, we are a large positive/negative, respectively, and therefore definitely out of range

                        value = result;
                        return false;
                    }

                    if (isUnsigned == sbyte.IsNegative((sbyte)source[sizeof(short) - 1]))
                    {
                        // When the most significant bit of the value being set/clear matches whether we are unsigned
                        // or signed then we are a large positive/negative and therefore definitely out of range

                        value = result;
                        return false;
                    }
                }

                ref byte sourceRef = ref MemoryMarshal.GetReference(source);

                if (source.Length >= sizeof(short))
                {
                    // We have at least 2 bytes, so just read the ones we need directly
                    result = Unsafe.ReadUnaligned<short>(ref sourceRef);

                    if (!BitConverter.IsLittleEndian)
                    {
                        result = BinaryPrimitives.ReverseEndianness(result);
                    }
                }
                else if (isUnsigned)
                {
                    // We only have 1-byte so read it directly
                    result = sourceRef;
                }
                else
                {
                    // We only have 1-byte so read it directly with sign extension
                    result = (sbyte)sourceRef;
                }
            }

            value = result;
            return true;
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.GetShortestBitLength()" />
        int IBinaryInteger<short>.GetShortestBitLength()
        {
            short value = m_value;

            if (value >= 0)
            {
                return (sizeof(short) * 8) - LeadingZeroCount(value);
            }
            else
            {
                return (sizeof(short) * 8) + 1 - LeadingZeroCount((short)(~value));
            }
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.GetByteCount()" />
        int IBinaryInteger<short>.GetByteCount() => sizeof(short);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryWriteBigEndian(Span{byte}, out int)" />
        bool IBinaryInteger<short>.TryWriteBigEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(short))
            {
                short value = BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(m_value) : m_value;
                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);

                bytesWritten = sizeof(short);
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryWriteLittleEndian(Span{byte}, out int)" />
        bool IBinaryInteger<short>.TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(short))
            {
                short value = BitConverter.IsLittleEndian ? m_value : BinaryPrimitives.ReverseEndianness(m_value);
                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);

                bytesWritten = sizeof(short);
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
        static short IBinaryNumber<short>.AllBitsSet => NegativeOne;

        /// <inheritdoc cref="IBinaryNumber{TSelf}.IsPow2(TSelf)" />
        public static bool IsPow2(short value) => BitOperations.IsPow2(value);

        /// <inheritdoc cref="IBinaryNumber{TSelf}.Log2(TSelf)" />
        public static short Log2(short value)
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

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseAnd(TSelf, TOther)" />
        static short IBitwiseOperators<short, short, short>.operator &(short left, short right) => (short)(left & right);

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseOr(TSelf, TOther)" />
        static short IBitwiseOperators<short, short, short>.operator |(short left, short right) => (short)(left | right);

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_ExclusiveOr(TSelf, TOther)" />
        static short IBitwiseOperators<short, short, short>.operator ^(short left, short right) => (short)(left ^ right);

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_OnesComplement(TSelf)" />
        static short IBitwiseOperators<short, short, short>.operator ~(short value) => (short)(~value);

        //
        // IComparisonOperators
        //

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_LessThan(TSelf, TOther)" />
        static bool IComparisonOperators<short, short, bool>.operator <(short left, short right) => left < right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_LessThanOrEqual(TSelf, TOther)" />
        static bool IComparisonOperators<short, short, bool>.operator <=(short left, short right) => left <= right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_GreaterThan(TSelf, TOther)" />
        static bool IComparisonOperators<short, short, bool>.operator >(short left, short right) => left > right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_GreaterThanOrEqual(TSelf, TOther)" />
        static bool IComparisonOperators<short, short, bool>.operator >=(short left, short right) => left >= right;

        //
        // IDecrementOperators
        //

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        static short IDecrementOperators<short>.operator --(short value) => --value;

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        static short IDecrementOperators<short>.operator checked --(short value) => checked(--value);

        //
        // IDivisionOperators
        //

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_Division(TSelf, TOther)" />
        static short IDivisionOperators<short, short, short>.operator /(short left, short right) => (short)(left / right);

        //
        // IEqualityOperators
        //

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther, TResult}.op_Equality(TSelf, TOther)" />
        static bool IEqualityOperators<short, short, bool>.operator ==(short left, short right) => left == right;

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther, TResult}.op_Inequality(TSelf, TOther)" />
        static bool IEqualityOperators<short, short, bool>.operator !=(short left, short right) => left != right;

        //
        // IIncrementOperators
        //

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_Increment(TSelf)" />
        static short IIncrementOperators<short>.operator ++(short value) => ++value;

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_CheckedIncrement(TSelf)" />
        static short IIncrementOperators<short>.operator checked ++(short value) => checked(++value);

        //
        // IMinMaxValue
        //

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MinValue" />
        static short IMinMaxValue<short>.MinValue => MinValue;

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MaxValue" />
        static short IMinMaxValue<short>.MaxValue => MaxValue;

        //
        // IModulusOperators
        //

        /// <inheritdoc cref="IModulusOperators{TSelf, TOther, TResult}.op_Modulus(TSelf, TOther)" />
        static short IModulusOperators<short, short, short>.operator %(short left, short right) => (short)(left % right);

        //
        // IMultiplicativeIdentity
        //

        /// <inheritdoc cref="IMultiplicativeIdentity{TSelf, TResult}.MultiplicativeIdentity" />
        static short IMultiplicativeIdentity<short, short>.MultiplicativeIdentity => MultiplicativeIdentity;

        //
        // IMultiplyOperators
        //

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_Multiply(TSelf, TOther)" />
        static short IMultiplyOperators<short, short, short>.operator *(short left, short right) => (short)(left * right);

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_CheckedMultiply(TSelf, TOther)" />
        static short IMultiplyOperators<short, short, short>.operator checked *(short left, short right) => checked((short)(left * right));

        //
        // INumber
        //

        /// <inheritdoc cref="INumber{TSelf}.Clamp(TSelf, TSelf, TSelf)" />
        public static short Clamp(short value, short min, short max) => Math.Clamp(value, min, max);

        /// <inheritdoc cref="INumber{TSelf}.CopySign(TSelf, TSelf)" />
        public static short CopySign(short value, short sign)
        {
            short absValue = value;

            if (absValue < 0)
            {
                absValue = (short)(-absValue);
            }

            if (sign >= 0)
            {
                if (absValue < 0)
                {
                    Math.ThrowNegateTwosCompOverflow();
                }

                return absValue;
            }

            return (short)(-absValue);
        }

        /// <inheritdoc cref="INumber{TSelf}.Max(TSelf, TSelf)" />
        public static short Max(short x, short y) => Math.Max(x, y);

        /// <inheritdoc cref="INumber{TSelf}.MaxNumber(TSelf, TSelf)" />
        static short INumber<short>.MaxNumber(short x, short y) => Max(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Min(TSelf, TSelf)" />
        public static short Min(short x, short y) => Math.Min(x, y);

        /// <inheritdoc cref="INumber{TSelf}.MinNumber(TSelf, TSelf)" />
        static short INumber<short>.MinNumber(short x, short y) => Min(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Sign(TSelf)" />
        public static int Sign(short value) => Math.Sign(value);

        //
        // INumberBase
        //

        /// <inheritdoc cref="INumberBase{TSelf}.One" />
        static short INumberBase<short>.One => One;

        /// <inheritdoc cref="INumberBase{TSelf}.Radix" />
        static int INumberBase<short>.Radix => 2;

        /// <inheritdoc cref="INumberBase{TSelf}.Zero" />
        static short INumberBase<short>.Zero => Zero;

        /// <inheritdoc cref="INumberBase{TSelf}.Abs(TSelf)" />
        public static short Abs(short value) => Math.Abs(value);

        /// <inheritdoc cref="INumberBase{TSelf}.CreateChecked{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short CreateChecked<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            short result;

            if (typeof(TOther) == typeof(short))
            {
                result = (short)(object)value;
            }
            else if (!TryConvertFromChecked(value, out result) && !TOther.TryConvertToChecked(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateSaturating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short CreateSaturating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            short result;

            if (typeof(TOther) == typeof(short))
            {
                result = (short)(object)value;
            }
            else if (!TryConvertFromSaturating(value, out result) && !TOther.TryConvertToSaturating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateTruncating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short CreateTruncating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            short result;

            if (typeof(TOther) == typeof(short))
            {
                result = (short)(object)value;
            }
            else if (!TryConvertFromTruncating(value, out result) && !TOther.TryConvertToTruncating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsCanonical(TSelf)" />
        static bool INumberBase<short>.IsCanonical(short value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsComplexNumber(TSelf)" />
        static bool INumberBase<short>.IsComplexNumber(short value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsEvenInteger(TSelf)" />
        public static bool IsEvenInteger(short value) => (value & 1) == 0;

        /// <inheritdoc cref="INumberBase{TSelf}.IsFinite(TSelf)" />
        static bool INumberBase<short>.IsFinite(short value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsImaginaryNumber(TSelf)" />
        static bool INumberBase<short>.IsImaginaryNumber(short value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsInfinity(TSelf)" />
        static bool INumberBase<short>.IsInfinity(short value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsInteger(TSelf)" />
        static bool INumberBase<short>.IsInteger(short value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsNaN(TSelf)" />
        static bool INumberBase<short>.IsNaN(short value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsNegative(TSelf)" />
        public static bool IsNegative(short value) => value < 0;

        /// <inheritdoc cref="INumberBase{TSelf}.IsNegativeInfinity(TSelf)" />
        static bool INumberBase<short>.IsNegativeInfinity(short value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsNormal(TSelf)" />
        static bool INumberBase<short>.IsNormal(short value) => value != 0;

        /// <inheritdoc cref="INumberBase{TSelf}.IsOddInteger(TSelf)" />
        public static bool IsOddInteger(short value) => (value & 1) != 0;

        /// <inheritdoc cref="INumberBase{TSelf}.IsPositive(TSelf)" />
        public static bool IsPositive(short value) => value >= 0;

        /// <inheritdoc cref="INumberBase{TSelf}.IsPositiveInfinity(TSelf)" />
        static bool INumberBase<short>.IsPositiveInfinity(short value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsRealNumber(TSelf)" />
        static bool INumberBase<short>.IsRealNumber(short value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsSubnormal(TSelf)" />
        static bool INumberBase<short>.IsSubnormal(short value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsZero(TSelf)" />
        static bool INumberBase<short>.IsZero(short value) => (value == 0);

        /// <inheritdoc cref="INumberBase{TSelf}.MaxMagnitude(TSelf, TSelf)" />
        public static short MaxMagnitude(short x, short y)
        {
            short absX = x;

            if (absX < 0)
            {
                absX = (short)(-absX);

                if (absX < 0)
                {
                    return x;
                }
            }

            short absY = y;

            if (absY < 0)
            {
                absY = (short)(-absY);

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
        static short INumberBase<short>.MaxMagnitudeNumber(short x, short y) => MaxMagnitude(x, y);

        /// <inheritdoc cref="INumberBase{TSelf}.MinMagnitude(TSelf, TSelf)" />
        public static short MinMagnitude(short x, short y)
        {
            short absX = x;

            if (absX < 0)
            {
                absX = (short)(-absX);

                if (absX < 0)
                {
                    return y;
                }
            }

            short absY = y;

            if (absY < 0)
            {
                absY = (short)(-absY);

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
        static short INumberBase<short>.MinMagnitudeNumber(short x, short y) => MinMagnitude(x, y);

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromChecked{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<short>.TryConvertFromChecked<TOther>(TOther value, out short result) => TryConvertFromChecked(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvertFromChecked<TOther>(TOther value, out short result)
            where TOther : INumberBase<TOther>
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `short` will handle the other signed types and
            // `ConvertTo` will handle the unsigned types

            if (typeof(TOther) == typeof(double))
            {
                double actualValue = (double)(object)value;
                result = checked((short)actualValue);
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualValue = (Half)(object)value;
                result = checked((short)actualValue);
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualValue = (int)(object)value;
                result = checked((short)actualValue);
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualValue = (long)(object)value;
                result = checked((short)actualValue);
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualValue = (Int128)(object)value;
                result = checked((short)actualValue);
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualValue = (nint)(object)value;
                result = checked((short)actualValue);
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualValue = (sbyte)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualValue = (float)(object)value;
                result = checked((short)actualValue);
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
        static bool INumberBase<short>.TryConvertFromSaturating<TOther>(TOther value, out short result) => TryConvertFromSaturating(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvertFromSaturating<TOther>(TOther value, out short result)
            where TOther : INumberBase<TOther>
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `short` will handle the other signed types and
            // `ConvertTo` will handle the unsigned types

            if (typeof(TOther) == typeof(double))
            {
                double actualValue = (double)(object)value;
                result = (actualValue >= MaxValue) ? MaxValue :
                         (actualValue <= MinValue) ? MinValue : (short)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualValue = (Half)(object)value;
                result = (actualValue >= BitConverter.UInt16BitsToHalf(0x7800)) ? MaxValue :
                         (actualValue <= BitConverter.UInt16BitsToHalf(0xF800)) ? MinValue : (short)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualValue = (int)(object)value;
                result = (actualValue >= MaxValue) ? MaxValue :
                         (actualValue <= MinValue) ? MinValue : (short)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualValue = (long)(object)value;
                result = (actualValue >= MaxValue) ? MaxValue :
                         (actualValue <= MinValue) ? MinValue : (short)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualValue = (Int128)(object)value;
                result = (actualValue >= MaxValue) ? MaxValue :
                         (actualValue <= MinValue) ? MinValue : (short)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualValue = (nint)(object)value;
                result = (actualValue >= MaxValue) ? MaxValue :
                         (actualValue <= MinValue) ? MinValue : (short)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualValue = (sbyte)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualValue = (float)(object)value;
                result = (actualValue >= MaxValue) ? MaxValue :
                         (actualValue <= MinValue) ? MinValue : (short)actualValue;
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
        static bool INumberBase<short>.TryConvertFromTruncating<TOther>(TOther value, out short result) => TryConvertFromTruncating(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvertFromTruncating<TOther>(TOther value, out short result)
            where TOther : INumberBase<TOther>
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `short` will handle the other signed types and
            // `ConvertTo` will handle the unsigned types

            if (typeof(TOther) == typeof(double))
            {
                double actualValue = (double)(object)value;
                result = (actualValue >= MaxValue) ? MaxValue :
                         (actualValue <= MinValue) ? MinValue : (short)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualValue = (Half)(object)value;
                result = (actualValue >= BitConverter.UInt16BitsToHalf(0x7800)) ? MaxValue :
                         (actualValue <= BitConverter.UInt16BitsToHalf(0xF800)) ? MinValue : (short)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualValue = (int)(object)value;
                result = (short)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualValue = (long)(object)value;
                result = (short)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualValue = (Int128)(object)value;
                result = (short)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualValue = (nint)(object)value;
                result = (short)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualValue = (sbyte)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualValue = (float)(object)value;
                result = (actualValue >= MaxValue) ? MaxValue :
                         (actualValue <= MinValue) ? MinValue : (short)actualValue;
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
        static bool INumberBase<short>.TryConvertToChecked<TOther>(short value, [MaybeNullWhen(false)] out TOther result)
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `short` will handle the other signed types and
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
        static bool INumberBase<short>.TryConvertToSaturating<TOther>(short value, [MaybeNullWhen(false)] out TOther result)
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `short` will handle the other signed types and
            // `ConvertTo` will handle the unsigned types

            if (typeof(TOther) == typeof(byte))
            {
                byte actualResult = (value >= byte.MaxValue) ? byte.MaxValue :
                                    (value <= byte.MinValue) ? byte.MinValue : (byte)value;
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
        static bool INumberBase<short>.TryConvertToTruncating<TOther>(short value, [MaybeNullWhen(false)] out TOther result)
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `short` will handle the other signed types and
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
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out short result) => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // IShiftOperators
        //

        /// <inheritdoc cref="IShiftOperators{TSelf, TOther, TResult}.op_LeftShift(TSelf, TOther)" />
        static short IShiftOperators<short, int, short>.operator <<(short value, int shiftAmount) => (short)(value << shiftAmount);

        /// <inheritdoc cref="IShiftOperators{TSelf, TOther, TResult}.op_RightShift(TSelf, TOther)" />
        static short IShiftOperators<short, int, short>.operator >>(short value, int shiftAmount) => (short)(value >> shiftAmount);

        /// <inheritdoc cref="IShiftOperators{TSelf, TOther, TResult}.op_UnsignedRightShift(TSelf, TOther)" />
        static short IShiftOperators<short, int, short>.operator >>>(short value, int shiftAmount) => (short)((ushort)value >>> shiftAmount);

        //
        // ISignedNumber
        //

        /// <inheritdoc cref="ISignedNumber{TSelf}.NegativeOne" />
        static short ISignedNumber<short>.NegativeOne => NegativeOne;

        //
        // ISpanParsable
        //

        /// <inheritdoc cref="ISpanParsable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static short Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Integer, provider);

        /// <inheritdoc cref="ISpanParsable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out short result) => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // ISubtractionOperators
        //

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_Subtraction(TSelf, TOther)" />
        static short ISubtractionOperators<short, short, short>.operator -(short left, short right) => (short)(left - right);

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_CheckedSubtraction(TSelf, TOther)" />
        static short ISubtractionOperators<short, short, short>.operator checked -(short left, short right) => checked((short)(left - right));

        //
        // IUnaryNegationOperators
        //

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_UnaryNegation(TSelf)" />
        static short IUnaryNegationOperators<short, short>.operator -(short value) => (short)(-value);

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_CheckedUnaryNegation(TSelf)" />
        static short IUnaryNegationOperators<short, short>.operator checked -(short value) => checked((short)(-value));

        //
        // IUnaryPlusOperators
        //

        /// <inheritdoc cref="IUnaryPlusOperators{TSelf, TResult}.op_UnaryPlus(TSelf)" />
        static short IUnaryPlusOperators<short, short>.operator +(short value) => (short)(+value);

        //
        // IUtf8SpanParsable
        //

        /// <inheritdoc cref="INumberBase{TSelf}.Parse(ReadOnlySpan{byte}, NumberStyles, IFormatProvider?)" />
        public static short Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.ParseBinaryInteger<byte, short>(utf8Text, style, NumberFormatInfo.GetInstance(provider));
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryParse(ReadOnlySpan{byte}, NumberStyles, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out short result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.TryParseBinaryInteger(utf8Text, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
        }

        /// <inheritdoc cref="IUtf8SpanParsable{TSelf}.Parse(ReadOnlySpan{byte}, IFormatProvider?)" />
        public static short Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) => Parse(utf8Text, NumberStyles.Integer, provider);

        /// <inheritdoc cref="IUtf8SpanParsable{TSelf}.TryParse(ReadOnlySpan{byte}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out short result) => TryParse(utf8Text, NumberStyles.Integer, provider, out result);

        //
        // IBinaryIntegerParseAndFormatInfo
        //

        static bool IBinaryIntegerParseAndFormatInfo<short>.IsSigned => true;

        static int IBinaryIntegerParseAndFormatInfo<short>.MaxDigitCount => 5; // 32_767

        static int IBinaryIntegerParseAndFormatInfo<short>.MaxHexDigitCount => 4; // 0x7FFF

        static short IBinaryIntegerParseAndFormatInfo<short>.MaxValueDiv10 => MaxValue / 10;

        static string IBinaryIntegerParseAndFormatInfo<short>.OverflowMessage => SR.Overflow_Int16;

        static bool IBinaryIntegerParseAndFormatInfo<short>.IsGreaterThanAsUnsigned(short left, short right) => (ushort)(left) > (ushort)(right);

        static short IBinaryIntegerParseAndFormatInfo<short>.MultiplyBy10(short value) => (short)(value * 10);

        static short IBinaryIntegerParseAndFormatInfo<short>.MultiplyBy16(short value) => (short)(value * 16);
    }
}
