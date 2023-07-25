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
    public readonly struct UInt16
        : IComparable,
          IConvertible,
          ISpanFormattable,
          IComparable<ushort>,
          IEquatable<ushort>,
          IBinaryInteger<ushort>,
          IMinMaxValue<ushort>,
          IUnsignedNumber<ushort>,
          IUtf8SpanFormattable,
          IBinaryIntegerParseAndFormatInfo<ushort>
    {
        private readonly ushort m_value; // Do not rename (binary serialization)

        public const ushort MaxValue = (ushort)0xFFFF;
        public const ushort MinValue = 0;

        /// <summary>Represents the additive identity (0).</summary>
        private const ushort AdditiveIdentity = 0;

        /// <summary>Represents the multiplicative identity (1).</summary>
        private const ushort MultiplicativeIdentity = 1;

        /// <summary>Represents the number one (1).</summary>
        private const ushort One = 1;

        /// <summary>Represents the number zero (0).</summary>
        private const ushort Zero = 0;

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

        /// <inheritdoc cref="IUtf8SpanFormattable.TryFormat" />
        public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatUInt32(m_value, format, provider, utf8Destination, out bytesWritten);
        }

        public static ushort Parse(string s) => Parse(s, NumberStyles.Integer, provider: null);

        public static ushort Parse(string s, NumberStyles style) => Parse(s, style, provider: null);

        public static ushort Parse(string s, IFormatProvider? provider) => Parse(s, NumberStyles.Integer, provider);

        public static ushort Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            if (s is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s); }
            return Parse(s.AsSpan(), style, provider);
        }

        public static ushort Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.ParseBinaryInteger<char, ushort>(s, style, NumberFormatInfo.GetInstance(provider));
        }

        public static bool TryParse([NotNullWhen(true)] string? s, out ushort result) => TryParse(s, NumberStyles.Integer, provider: null, out result);

        public static bool TryParse(ReadOnlySpan<char> s, out ushort result) => TryParse(s, NumberStyles.Integer, provider: null, out result);

        /// <summary>Tries to convert a UTF-8 character span containing the string representation of a number to its 16-bit unsigned integer equivalent.</summary>
        /// <param name="utf8Text">A span containing the UTF-8 characters representing the number to convert.</param>
        /// <param name="result">When this method returns, contains the 16-bit unsigned integer value equivalent to the number contained in <paramref name="utf8Text" /> if the conversion succeeded, or zero if the conversion failed. This parameter is passed uninitialized; any value originally supplied in result will be overwritten.</param>
        /// <returns><c>true</c> if <paramref name="utf8Text" /> was converted successfully; otherwise, false.</returns>
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, out ushort result) => TryParse(utf8Text, NumberStyles.Integer, provider: null, out result);

        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out ushort result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);

            if (s is null)
            {
                result = 0;
                return false;
            }
            return Number.TryParseBinaryInteger(s.AsSpan(), style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
        }

        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out ushort result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.TryParseBinaryInteger(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
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

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        static ushort IAdditionOperators<ushort, ushort, ushort>.operator checked +(ushort left, ushort right) => checked((ushort)(left + right));

        //
        // IAdditiveIdentity
        //

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static ushort IAdditiveIdentity<ushort, ushort>.AdditiveIdentity => AdditiveIdentity;

        //
        // IBinaryInteger
        //

        /// <inheritdoc cref="IBinaryInteger{TSelf}.DivRem(TSelf, TSelf)" />
        public static (ushort Quotient, ushort Remainder) DivRem(ushort left, ushort right) => Math.DivRem(left, right);

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

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryReadBigEndian(ReadOnlySpan{byte}, bool, out TSelf)" />
        static bool IBinaryInteger<ushort>.TryReadBigEndian(ReadOnlySpan<byte> source, bool isUnsigned, out ushort value)
        {
            ushort result = default;

            if (source.Length != 0)
            {
                if (!isUnsigned && sbyte.IsNegative((sbyte)source[0]))
                {
                    // When we are signed and the sign bit is set, we are negative and therefore
                    // definitely out of range

                    value = result;
                    return false;
                }

                if ((source.Length > sizeof(ushort)) && (source[..^sizeof(ushort)].ContainsAnyExcept((byte)0x00)))
                {
                    // When we have any non-zero leading data, we are a large positive and therefore
                    // definitely out of range

                    value = result;
                    return false;
                }

                ref byte sourceRef = ref MemoryMarshal.GetReference(source);

                if (source.Length >= sizeof(ushort))
                {
                    sourceRef = ref Unsafe.Add(ref sourceRef, source.Length - sizeof(ushort));

                    // We have at least 2 bytes, so just read the ones we need directly
                    result = Unsafe.ReadUnaligned<ushort>(ref sourceRef);

                    if (BitConverter.IsLittleEndian)
                    {
                        result = BinaryPrimitives.ReverseEndianness(result);
                    }
                }
                else
                {
                    // We only have 1-byte so read it directly
                    result = sourceRef;
                }
            }

            value = result;
            return true;
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryReadLittleEndian(ReadOnlySpan{byte}, bool, out TSelf)" />
        static bool IBinaryInteger<ushort>.TryReadLittleEndian(ReadOnlySpan<byte> source, bool isUnsigned, out ushort value)
        {
            ushort result = default;

            if (source.Length != 0)
            {
                if (!isUnsigned && sbyte.IsNegative((sbyte)source[^1]))
                {
                    // When we are signed and the sign bit is set, we are negative and therefore
                    // definitely out of range

                    value = result;
                    return false;
                }

                if ((source.Length > sizeof(ushort)) && (source[sizeof(ushort)..].ContainsAnyExcept((byte)0x00)))
                {
                    // When we have any non-zero leading data, we are a large positive and therefore
                    // definitely out of range

                    value = result;
                    return false;
                }

                ref byte sourceRef = ref MemoryMarshal.GetReference(source);

                if (source.Length >= sizeof(ushort))
                {
                    // We have at least 2 bytes, so just read the ones we need directly
                    result = Unsafe.ReadUnaligned<ushort>(ref sourceRef);

                    if (!BitConverter.IsLittleEndian)
                    {
                        result = BinaryPrimitives.ReverseEndianness(result);
                    }
                }
                else
                {
                    // We only have 1-byte so read it directly
                    result = sourceRef;
                }
            }

            value = result;
            return true;
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.GetShortestBitLength()" />
        int IBinaryInteger<ushort>.GetShortestBitLength() => (sizeof(ushort) * 8) - LeadingZeroCount(m_value);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.GetByteCount()" />
        int IBinaryInteger<ushort>.GetByteCount() => sizeof(ushort);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryWriteBigEndian(Span{byte}, out int)" />
        bool IBinaryInteger<ushort>.TryWriteBigEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(ushort))
            {
                ushort value = BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(m_value) : m_value;
                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);

                bytesWritten = sizeof(ushort);
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryWriteLittleEndian(Span{byte}, out int)" />
        bool IBinaryInteger<ushort>.TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(ushort))
            {
                ushort value = BitConverter.IsLittleEndian ? m_value : BinaryPrimitives.ReverseEndianness(m_value);
                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);

                bytesWritten = sizeof(ushort);
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
        static ushort IBinaryNumber<ushort>.AllBitsSet => MaxValue;

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

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_LessThan(TSelf, TOther)" />
        static bool IComparisonOperators<ushort, ushort, bool>.operator <(ushort left, ushort right) => left < right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_LessThanOrEqual(TSelf, TOther)" />
        static bool IComparisonOperators<ushort, ushort, bool>.operator <=(ushort left, ushort right) => left <= right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_GreaterThan(TSelf, TOther)" />
        static bool IComparisonOperators<ushort, ushort, bool>.operator >(ushort left, ushort right) => left > right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_GreaterThanOrEqual(TSelf, TOther)" />
        static bool IComparisonOperators<ushort, ushort, bool>.operator >=(ushort left, ushort right) => left >= right;

        //
        // IDecrementOperators
        //

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        static ushort IDecrementOperators<ushort>.operator --(ushort value) => --value;

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        static ushort IDecrementOperators<ushort>.operator checked --(ushort value) => checked(--value);

        //
        // IDivisionOperators
        //

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_Division(TSelf, TOther)" />
        static ushort IDivisionOperators<ushort, ushort, ushort>.operator /(ushort left, ushort right) => (ushort)(left / right);

        //
        // IEqualityOperators
        //

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther, TResult}.op_Equality(TSelf, TOther)" />
        static bool IEqualityOperators<ushort, ushort, bool>.operator ==(ushort left, ushort right) => left == right;

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther, TResult}.op_Inequality(TSelf, TOther)" />
        static bool IEqualityOperators<ushort, ushort, bool>.operator !=(ushort left, ushort right) => left != right;

        //
        // IIncrementOperators
        //

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_Increment(TSelf)" />
        static ushort IIncrementOperators<ushort>.operator ++(ushort value) => ++value;

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_CheckedIncrement(TSelf)" />
        static ushort IIncrementOperators<ushort>.operator checked ++(ushort value) => checked(++value);

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

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_CheckedMultiply(TSelf, TOther)" />
        static ushort IMultiplyOperators<ushort, ushort, ushort>.operator checked *(ushort left, ushort right) => checked((ushort)(left * right));

        //
        // INumber
        //

        /// <inheritdoc cref="INumber{TSelf}.Clamp(TSelf, TSelf, TSelf)" />
        public static ushort Clamp(ushort value, ushort min, ushort max) => Math.Clamp(value, min, max);

        /// <inheritdoc cref="INumber{TSelf}.CopySign(TSelf, TSelf)" />
        static ushort INumber<ushort>.CopySign(ushort value, ushort sign) => value;

        /// <inheritdoc cref="INumber{TSelf}.Max(TSelf, TSelf)" />
        public static ushort Max(ushort x, ushort y) => Math.Max(x, y);

        /// <inheritdoc cref="INumber{TSelf}.MaxNumber(TSelf, TSelf)" />
        static ushort INumber<ushort>.MaxNumber(ushort x, ushort y) => Max(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Min(TSelf, TSelf)" />
        public static ushort Min(ushort x, ushort y) => Math.Min(x, y);

        /// <inheritdoc cref="INumber{TSelf}.MinNumber(TSelf, TSelf)" />
        static ushort INumber<ushort>.MinNumber(ushort x, ushort y) => Min(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Sign(TSelf)" />
        public static int Sign(ushort value) => (value == 0) ? 0 : 1;

        //
        // INumberBase
        //

        /// <inheritdoc cref="INumberBase{TSelf}.One" />
        static ushort INumberBase<ushort>.One => One;

        /// <inheritdoc cref="INumberBase{TSelf}.Radix" />
        static int INumberBase<ushort>.Radix => 2;

        /// <inheritdoc cref="INumberBase{TSelf}.Zero" />
        static ushort INumberBase<ushort>.Zero => Zero;

        /// <inheritdoc cref="INumberBase{TSelf}.Abs(TSelf)" />
        static ushort INumberBase<ushort>.Abs(ushort value) => value;

        /// <inheritdoc cref="INumberBase{TSelf}.CreateChecked{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort CreateChecked<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            ushort result;

            if (typeof(TOther) == typeof(ushort))
            {
                result = (ushort)(object)value;
            }
            else if (!TryConvertFromChecked(value, out result) && !TOther.TryConvertToChecked(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateSaturating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort CreateSaturating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            ushort result;

            if (typeof(TOther) == typeof(ushort))
            {
                result = (ushort)(object)value;
            }
            else if (!TryConvertFromSaturating(value, out result) && !TOther.TryConvertToSaturating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateTruncating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort CreateTruncating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            ushort result;

            if (typeof(TOther) == typeof(ushort))
            {
                result = (ushort)(object)value;
            }
            else if (!TryConvertFromTruncating(value, out result) && !TOther.TryConvertToTruncating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsCanonical(TSelf)" />
        static bool INumberBase<ushort>.IsCanonical(ushort value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsComplexNumber(TSelf)" />
        static bool INumberBase<ushort>.IsComplexNumber(ushort value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsEvenInteger(TSelf)" />
        public static bool IsEvenInteger(ushort value) => (value & 1) == 0;

        /// <inheritdoc cref="INumberBase{TSelf}.IsFinite(TSelf)" />
        static bool INumberBase<ushort>.IsFinite(ushort value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsImaginaryNumber(TSelf)" />
        static bool INumberBase<ushort>.IsImaginaryNumber(ushort value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsInfinity(TSelf)" />
        static bool INumberBase<ushort>.IsInfinity(ushort value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsInteger(TSelf)" />
        static bool INumberBase<ushort>.IsInteger(ushort value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsNaN(TSelf)" />
        static bool INumberBase<ushort>.IsNaN(ushort value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsNegative(TSelf)" />
        static bool INumberBase<ushort>.IsNegative(ushort value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsNegativeInfinity(TSelf)" />
        static bool INumberBase<ushort>.IsNegativeInfinity(ushort value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsNormal(TSelf)" />
        static bool INumberBase<ushort>.IsNormal(ushort value) => value != 0;

        /// <inheritdoc cref="INumberBase{TSelf}.IsOddInteger(TSelf)" />
        public static bool IsOddInteger(ushort value) => (value & 1) != 0;

        /// <inheritdoc cref="INumberBase{TSelf}.IsPositive(TSelf)" />
        static bool INumberBase<ushort>.IsPositive(ushort value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsPositiveInfinity(TSelf)" />
        static bool INumberBase<ushort>.IsPositiveInfinity(ushort value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsRealNumber(TSelf)" />
        static bool INumberBase<ushort>.IsRealNumber(ushort value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsSubnormal(TSelf)" />
        static bool INumberBase<ushort>.IsSubnormal(ushort value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsZero(TSelf)" />
        static bool INumberBase<ushort>.IsZero(ushort value) => (value == 0);

        /// <inheritdoc cref="INumberBase{TSelf}.MaxMagnitude(TSelf, TSelf)" />
        static ushort INumberBase<ushort>.MaxMagnitude(ushort x, ushort y) => Max(x, y);

        /// <inheritdoc cref="INumberBase{TSelf}.MaxMagnitudeNumber(TSelf, TSelf)" />
        static ushort INumberBase<ushort>.MaxMagnitudeNumber(ushort x, ushort y) => Max(x, y);

        /// <inheritdoc cref="INumberBase{TSelf}.MinMagnitude(TSelf, TSelf)" />
        static ushort INumberBase<ushort>.MinMagnitude(ushort x, ushort y) => Min(x, y);

        /// <inheritdoc cref="INumberBase{TSelf}.MinMagnitudeNumber(TSelf, TSelf)" />
        static ushort INumberBase<ushort>.MinMagnitudeNumber(ushort x, ushort y) => Min(x, y);

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromChecked{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<ushort>.TryConvertFromChecked<TOther>(TOther value, out ushort result) => TryConvertFromChecked(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvertFromChecked<TOther>(TOther value, out ushort result)
            where TOther : INumberBase<TOther>
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `ushort` will handle the other unsigned types and
            // `ConvertTo` will handle the signed types

            if (typeof(TOther) == typeof(byte))
            {
                byte actualValue = (byte)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualValue = (char)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualValue = (decimal)(object)value;
                result = checked((ushort)actualValue);
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualValue = (uint)(object)value;
                result = checked((ushort)actualValue);
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualValue = (ulong)(object)value;
                result = checked((ushort)actualValue);
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualValue = (UInt128)(object)value;
                result = checked((ushort)actualValue);
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                nuint actualValue = (nuint)(object)value;
                result = checked((ushort)actualValue);
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
        static bool INumberBase<ushort>.TryConvertFromSaturating<TOther>(TOther value, out ushort result) => TryConvertFromSaturating(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvertFromSaturating<TOther>(TOther value, out ushort result)
            where TOther : INumberBase<TOther>
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `ushort` will handle the other unsigned types and
            // `ConvertTo` will handle the signed types

            if (typeof(TOther) == typeof(byte))
            {
                byte actualValue = (byte)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualValue = (char)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualValue = (decimal)(object)value;
                result = (actualValue >= MaxValue) ? MaxValue :
                         (actualValue <= MinValue) ? MinValue : (ushort)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualValue = (uint)(object)value;
                result = (actualValue >= MaxValue) ? MaxValue : (ushort)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualValue = (ulong)(object)value;
                result = (actualValue >= MaxValue) ? MaxValue : (ushort)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualValue = (UInt128)(object)value;
                result = (actualValue >= MaxValue) ? MaxValue : (ushort)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                nuint actualValue = (nuint)(object)value;
                result = (actualValue >= MaxValue) ? MaxValue : (ushort)actualValue;
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
        static bool INumberBase<ushort>.TryConvertFromTruncating<TOther>(TOther value, out ushort result) => TryConvertFromTruncating(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvertFromTruncating<TOther>(TOther value, out ushort result)
            where TOther : INumberBase<TOther>
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `ushort` will handle the other unsigned types and
            // `ConvertTo` will handle the signed types

            if (typeof(TOther) == typeof(byte))
            {
                byte actualValue = (byte)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualValue = (char)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualValue = (decimal)(object)value;
                result = (actualValue >= MaxValue) ? MaxValue :
                         (actualValue <= MinValue) ? MinValue : (ushort)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualValue = (uint)(object)value;
                result = (ushort)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualValue = (ulong)(object)value;
                result = (ushort)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualValue = (UInt128)(object)value;
                result = (ushort)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                nuint actualValue = (nuint)(object)value;
                result = (ushort)actualValue;
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
        static bool INumberBase<ushort>.TryConvertToChecked<TOther>(ushort value, [MaybeNullWhen(false)] out TOther result)
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `ushort` will handle the other unsigned types and
            // `ConvertTo` will handle the signed types

            if (typeof(TOther) == typeof(double))
            {
                double actualResult = value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualResult = (Half)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualResult = checked((short)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualResult = value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualResult = value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualResult = value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualResult = value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualResult = checked((sbyte)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualResult = value;
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
        static bool INumberBase<ushort>.TryConvertToSaturating<TOther>(ushort value, [MaybeNullWhen(false)] out TOther result)
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `ushort` will handle the other unsigned types and
            // `ConvertTo` will handle the signed types

            if (typeof(TOther) == typeof(double))
            {
                double actualResult = value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualResult = (Half)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualResult = (value >= short.MaxValue) ? short.MaxValue : (short)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualResult = value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualResult = value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualResult = value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualResult = value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualResult = (value >= sbyte.MaxValue) ? sbyte.MaxValue : (sbyte)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualResult = value;
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
        static bool INumberBase<ushort>.TryConvertToTruncating<TOther>(ushort value, [MaybeNullWhen(false)] out TOther result)
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `ushort` will handle the other unsigned types and
            // `ConvertTo` will handle the signed types

            if (typeof(TOther) == typeof(double))
            {
                double actualResult = value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualResult = (Half)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualResult = (short)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualResult = value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualResult = value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualResult = value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualResult = value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualResult = (sbyte)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualResult = value;
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
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out ushort result) => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // IShiftOperators
        //

        /// <inheritdoc cref="IShiftOperators{TSelf, TOther, TResult}.op_LeftShift(TSelf, TOther)" />
        static ushort IShiftOperators<ushort, int, ushort>.operator <<(ushort value, int shiftAmount) => (ushort)(value << shiftAmount);

        /// <inheritdoc cref="IShiftOperators{TSelf, TOther, TResult}.op_RightShift(TSelf, TOther)" />
        static ushort IShiftOperators<ushort, int, ushort>.operator >>(ushort value, int shiftAmount) => (ushort)(value >> shiftAmount);

        /// <inheritdoc cref="IShiftOperators{TSelf, TOther, TResult}.op_UnsignedRightShift(TSelf, TOther)" />
        static ushort IShiftOperators<ushort, int, ushort>.operator >>>(ushort value, int shiftAmount) => (ushort)(value >>> shiftAmount);

        //
        // ISpanParsable
        //

        /// <inheritdoc cref="ISpanParsable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static ushort Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Integer, provider);

        /// <inheritdoc cref="ISpanParsable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out ushort result) => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // ISubtractionOperators
        //

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_Subtraction(TSelf, TOther)" />
        static ushort ISubtractionOperators<ushort, ushort, ushort>.operator -(ushort left, ushort right) => (ushort)(left - right);

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_CheckedSubtraction(TSelf, TOther)" />
        static ushort ISubtractionOperators<ushort, ushort, ushort>.operator checked -(ushort left, ushort right) => checked((ushort)(left - right));

        //
        // IUnaryNegationOperators
        //

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_UnaryNegation(TSelf)" />
        static ushort IUnaryNegationOperators<ushort, ushort>.operator -(ushort value) => (ushort)(-value);

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_CheckedUnaryNegation(TSelf)" />
        static ushort IUnaryNegationOperators<ushort, ushort>.operator checked -(ushort value) => checked((ushort)(-value));

        //
        // IUnaryPlusOperators
        //

        /// <inheritdoc cref="IUnaryPlusOperators{TSelf, TResult}.op_UnaryPlus(TSelf)" />
        static ushort IUnaryPlusOperators<ushort, ushort>.operator +(ushort value) => (ushort)(+value);

        //
        // IUtf8SpanParsable
        //

        /// <inheritdoc cref="INumberBase{TSelf}.Parse(ReadOnlySpan{byte}, NumberStyles, IFormatProvider?)" />
        public static ushort Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.ParseBinaryInteger<byte, ushort>(utf8Text, style, NumberFormatInfo.GetInstance(provider));
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryParse(ReadOnlySpan{byte}, NumberStyles, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out ushort result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.TryParseBinaryInteger(utf8Text, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
        }

        /// <inheritdoc cref="IUtf8SpanParsable{TSelf}.Parse(ReadOnlySpan{byte}, IFormatProvider?)" />
        public static ushort Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) => Parse(utf8Text, NumberStyles.Integer, provider);

        /// <inheritdoc cref="IUtf8SpanParsable{TSelf}.TryParse(ReadOnlySpan{byte}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out ushort result) => TryParse(utf8Text, NumberStyles.Integer, provider, out result);

        //
        // IBinaryIntegerParseAndFormatInfo
        //

        static bool IBinaryIntegerParseAndFormatInfo<ushort>.IsSigned => false;

        static int IBinaryIntegerParseAndFormatInfo<ushort>.MaxDigitCount => 5; // 65_535

        static int IBinaryIntegerParseAndFormatInfo<ushort>.MaxHexDigitCount => 4; // 0xFFFF

        static ushort IBinaryIntegerParseAndFormatInfo<ushort>.MaxValueDiv10 => MaxValue / 10;

        static string IBinaryIntegerParseAndFormatInfo<ushort>.OverflowMessage => SR.Overflow_UInt16;

        static bool IBinaryIntegerParseAndFormatInfo<ushort>.IsGreaterThanAsUnsigned(ushort left, ushort right) => left > right;

        static ushort IBinaryIntegerParseAndFormatInfo<ushort>.MultiplyBy10(ushort value) => (ushort)(value * 10);

        static ushort IBinaryIntegerParseAndFormatInfo<ushort>.MultiplyBy16(ushort value) => (ushort)(value * 16);
    }
}
