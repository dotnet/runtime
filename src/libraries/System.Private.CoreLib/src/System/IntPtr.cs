// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;

#pragma warning disable SA1121 // explicitly using type aliases instead of built-in types

#if TARGET_64BIT
using nint_t = System.Int64;
#else
using nint_t = System.Int32;
#endif

namespace System
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public readonly struct IntPtr
        : IEquatable<nint>,
          IComparable,
          IComparable<nint>,
          ISpanFormattable,
          ISerializable,
          IBinaryInteger<nint>,
          IMinMaxValue<nint>,
          ISignedNumber<nint>
    {
        private readonly nint _value;

        [Intrinsic]
        public static readonly nint Zero;

        [NonVersionable]
        public IntPtr(int value)
        {
            _value = value;
        }

        [NonVersionable]
        public IntPtr(long value)
        {
#if TARGET_64BIT
            _value = (nint)value;
#else
            _value = checked((nint)value);
#endif
        }

        [CLSCompliant(false)]
        [NonVersionable]
        public unsafe IntPtr(void* value)
        {
            _value = (nint)value;
        }

        private IntPtr(SerializationInfo info, StreamingContext context)
        {
            long value = info.GetInt64("value");

#if TARGET_32BIT
            if ((value > int.MaxValue) || (value < int.MinValue))
            {
                throw new ArgumentException(SR.Serialization_InvalidPtrValue);
            }
#endif

            _value = (nint)value;
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            ArgumentNullException.ThrowIfNull(info);

            long value = _value;
            info.AddValue("value", value);
        }

        public override bool Equals([NotNullWhen(true)] object? obj) => (obj is nint other) && Equals(other);

        public override int GetHashCode()
        {
#if TARGET_64BIT
            long value = _value;
            return value.GetHashCode();
#else
            return (int)_value;
#endif
        }

        [NonVersionable]
        public int ToInt32()
        {
#if TARGET_64BIT
            return checked((int)_value);
#else
            return (int)_value;
#endif
        }

        [NonVersionable]
        public long ToInt64() => _value;

        [NonVersionable]
        public static explicit operator nint(int value) => value;

        [NonVersionable]
        public static explicit operator nint(long value) => checked((nint)value);

        [CLSCompliant(false)]
        [NonVersionable]
        public static unsafe explicit operator nint(void* value) => (nint)value;

        [CLSCompliant(false)]
        [NonVersionable]
        public static unsafe explicit operator void*(nint value) => (void*)value;

        [NonVersionable]
        public static explicit operator int(nint value)
        {
#if TARGET_64BIT
            return checked((int)value);
#else
            return (int)value;
#endif
        }

        [NonVersionable]
        public static explicit operator long(nint value) => value;

        [NonVersionable]
        public static bool operator ==(nint value1, nint value2) => value1 == value2;

        [NonVersionable]
        public static bool operator !=(nint value1, nint value2) => value1 != value2;

        [NonVersionable]
        public static nint Add(nint pointer, int offset) => pointer + offset;

        [NonVersionable]
        public static nint operator +(nint pointer, int offset) => pointer + offset;

        [NonVersionable]
        public static nint Subtract(nint pointer, int offset) => pointer - offset;

        [NonVersionable]
        public static nint operator -(nint pointer, int offset) => pointer - offset;

        public static int Size
        {
            [NonVersionable]
            get => sizeof(nint_t);
        }

        [CLSCompliant(false)]
        [NonVersionable]
        public unsafe void* ToPointer() => (void*)_value;

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MaxValue" />
        public static nint MaxValue
        {
            [NonVersionable]
            get => unchecked((nint)nint_t.MaxValue);
        }

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MinValue" />
        public static nint MinValue
        {
            [NonVersionable]
            get => unchecked((nint)nint_t.MinValue);
        }

        public int CompareTo(object? value)
        {
            if (value is nint other)
            {
                return CompareTo(other);
            }
            else if (value is null)
            {
                return 1;
            }

            throw new ArgumentException(SR.Arg_MustBeIntPtr);
        }

        public int CompareTo(nint value)
        {
            if (_value < value) return -1;
            if (_value > value) return 1;
            return 0;
        }

        [NonVersionable]
        public bool Equals(nint other) => _value == other;

        public override string ToString() => ((nint_t)_value).ToString();
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format) => ((nint_t)_value).ToString(format);
        public string ToString(IFormatProvider? provider) => ((nint_t)_value).ToString(provider);
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider) => ((nint_t)_value).ToString(format, provider);

        public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null) =>
            ((nint_t)_value).TryFormat(destination, out charsWritten, format, provider);

        public static nint Parse(string s) => (nint)nint_t.Parse(s);
        public static nint Parse(string s, NumberStyles style) => (nint)nint_t.Parse(s, style);
        public static nint Parse(string s, IFormatProvider? provider) => (nint)nint_t.Parse(s, provider);
        public static nint Parse(string s, NumberStyles style, IFormatProvider? provider) => (nint)nint_t.Parse(s, style, provider);
        public static nint Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => (nint)nint_t.Parse(s, provider);
        public static nint Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null) => (nint)nint_t.Parse(s, style, provider);

        public static bool TryParse([NotNullWhen(true)] string? s, out nint result)
        {
            Unsafe.SkipInit(out result);
            return nint_t.TryParse(s, out Unsafe.As<nint, nint_t>(ref result));
        }

        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out nint result)
        {
            Unsafe.SkipInit(out result);
            return nint_t.TryParse(s, provider, out Unsafe.As<nint, nint_t>(ref result));
        }

        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out nint result)
        {
            Unsafe.SkipInit(out result);
            return nint_t.TryParse(s, style, provider, out Unsafe.As<nint, nint_t>(ref result));
        }

        public static bool TryParse(ReadOnlySpan<char> s, out nint result)
        {
            Unsafe.SkipInit(out result);
            return nint_t.TryParse(s, out Unsafe.As<nint, nint_t>(ref result));
        }

        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out nint result)
        {
            Unsafe.SkipInit(out result);
            return nint_t.TryParse(s, provider, out Unsafe.As<nint, nint_t>(ref result));
        }

        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out nint result)
        {
            Unsafe.SkipInit(out result);
            return nint_t.TryParse(s, style, provider, out Unsafe.As<nint, nint_t>(ref result));
        }

        //
        // IAdditionOperators
        //

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        static nint IAdditionOperators<nint, nint, nint>.operator +(nint left, nint right) => left + right;

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        static nint IAdditionOperators<nint, nint, nint>.operator checked +(nint left, nint right) => checked(left + right);

        //
        // IAdditiveIdentity
        //

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static nint IAdditiveIdentity<nint, nint>.AdditiveIdentity => 0;

        //
        // IBinaryInteger
        //

        /// <inheritdoc cref="IBinaryInteger{TSelf}.DivRem(TSelf, TSelf)" />
        public static (nint Quotient, nint Remainder) DivRem(nint left, nint right) => Math.DivRem(left, right);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.LeadingZeroCount(TSelf)" />
        public static nint LeadingZeroCount(nint value) => BitOperations.LeadingZeroCount((nuint)value);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.PopCount(TSelf)" />
        public static nint PopCount(nint value) => BitOperations.PopCount((nuint)value);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.RotateLeft(TSelf, int)" />
        public static nint RotateLeft(nint value, int rotateAmount) => (nint)BitOperations.RotateLeft((nuint)value, rotateAmount);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.RotateRight(TSelf, int)" />
        public static nint RotateRight(nint value, int rotateAmount) => (nint)BitOperations.RotateRight((nuint)value, rotateAmount);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TrailingZeroCount(TSelf)" />
        public static nint TrailingZeroCount(nint value) => BitOperations.TrailingZeroCount(value);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryReadBigEndian(ReadOnlySpan{byte}, bool, out TSelf)" />
        static bool IBinaryInteger<nint>.TryReadBigEndian(ReadOnlySpan<byte> source, bool isUnsigned, out nint value)
        {
            nint result = default;

            if (source.Length != 0)
            {
                // Propagate the most significant bit so we have `0` or `-1`
                sbyte sign = (sbyte)(source[0]);
                sign >>= 31;
                Debug.Assert((sign == 0) || (sign == -1));

                // We need to also track if the input data is unsigned
                isUnsigned |= (sign == 0);

                if (isUnsigned && sbyte.IsNegative(sign) && (source.Length >= sizeof(nint_t)))
                {
                    // When we are unsigned and the most significant bit is set, we are a large positive
                    // and therefore definitely out of range

                    value = result;
                    return false;
                }

                if (source.Length > sizeof(nint_t))
                {
                    if (source[..^sizeof(nint_t)].IndexOfAnyExcept((byte)sign) >= 0)
                    {
                        // When we are unsigned and have any non-zero leading data or signed with any non-set leading
                        // data, we are a large positive/negative, respectively, and therefore definitely out of range

                        value = result;
                        return false;
                    }

                    if (isUnsigned == sbyte.IsNegative((sbyte)source[^sizeof(nint_t)]))
                    {
                        // When the most significant bit of the value being set/clear matches whether we are unsigned
                        // or signed then we are a large positive/negative and therefore definitely out of range

                        value = result;
                        return false;
                    }
                }

                ref byte sourceRef = ref MemoryMarshal.GetReference(source);

                if (source.Length >= sizeof(nint_t))
                {
                    sourceRef = ref Unsafe.Add(ref sourceRef, source.Length - sizeof(nint_t));

                    // We have at least 4/8 bytes, so just read the ones we need directly
                    result = Unsafe.ReadUnaligned<nint>(ref sourceRef);

                    if (BitConverter.IsLittleEndian)
                    {
                        result = BinaryPrimitives.ReverseEndianness(result);
                    }
                }
                else
                {
                    // We have between 1 and 3/7 bytes, so construct the relevant value directly
                    // since the data is in Big Endian format, we can just read the bytes and
                    // shift left by 8-bits for each subsequent part

                    for (int i = 0; i < source.Length; i++)
                    {
                        result <<= 8;
                        result |= Unsafe.Add(ref sourceRef, i);
                    }

                    if (!isUnsigned)
                    {
                        result |= (((nint)1 << ((sizeof(nint_t) * 8) - 1)) >> (((sizeof(nint_t) - source.Length) * 8) - 1));
                    }
                }
            }

            value = result;
            return true;
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryReadLittleEndian(ReadOnlySpan{byte}, bool, out TSelf)" />
        static bool IBinaryInteger<nint>.TryReadLittleEndian(ReadOnlySpan<byte> source, bool isUnsigned, out nint value)
        {
            nint result = default;

            if (source.Length != 0)
            {
                // Propagate the most significant bit so we have `0` or `-1`
                sbyte sign = (sbyte)(source[^1]);
                sign >>= 31;
                Debug.Assert((sign == 0) || (sign == -1));

                // We need to also track if the input data is unsigned
                isUnsigned |= (sign == 0);

                if (isUnsigned && sbyte.IsNegative(sign) && (source.Length >= sizeof(nint_t)))
                {
                    // When we are unsigned and the most significant bit is set, we are a large positive
                    // and therefore definitely out of range

                    value = result;
                    return false;
                }

                if (source.Length > sizeof(nint_t))
                {
                    if (source[sizeof(nint_t)..].IndexOfAnyExcept((byte)sign) >= 0)
                    {
                        // When we are unsigned and have any non-zero leading data or signed with any non-set leading
                        // data, we are a large positive/negative, respectively, and therefore definitely out of range

                        value = result;
                        return false;
                    }

                    if (isUnsigned == sbyte.IsNegative((sbyte)source[sizeof(nint_t) - 1]))
                    {
                        // When the most significant bit of the value being set/clear matches whether we are unsigned
                        // or signed then we are a large positive/negative and therefore definitely out of range

                        value = result;
                        return false;
                    }
                }

                ref byte sourceRef = ref MemoryMarshal.GetReference(source);

                if (source.Length >= sizeof(nint_t))
                {
                    // We have at least 4/8 bytes, so just read the ones we need directly
                    result = Unsafe.ReadUnaligned<nint>(ref sourceRef);

                    if (!BitConverter.IsLittleEndian)
                    {
                        result = BinaryPrimitives.ReverseEndianness(result);
                    }
                }
                else
                {
                    // We have between 1 and 3/7 bytes, so construct the relevant value directly
                    // since the data is in Little Endian format, we can just read the bytes and
                    // shift left by 8-bits for each subsequent part, then reverse endianness to
                    // ensure the order is correct. This is more efficient than iterating in reverse
                    // due to current JIT limitations

                    for (int i = 0; i < source.Length; i++)
                    {
                        result <<= 8;
                        result |= Unsafe.Add(ref sourceRef, i);
                    }

                    result <<= ((sizeof(nint_t) - source.Length) * 8);
                    result = BinaryPrimitives.ReverseEndianness(result);

                    if (!isUnsigned)
                    {
                        result |= (((nint)1 << ((sizeof(nint_t) * 8) - 1)) >> (((sizeof(nint_t) - source.Length) * 8) - 1));
                    }
                }
            }

            value = result;
            return true;
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.GetShortestBitLength()" />
        int IBinaryInteger<nint>.GetShortestBitLength()
        {
            nint value = _value;

            if (value >= 0)
            {
                return (sizeof(nint_t) * 8) - BitOperations.LeadingZeroCount((nuint)value);
            }
            else
            {
                return (sizeof(nint_t) * 8) + 1 - BitOperations.LeadingZeroCount((nuint)(~value));
            }
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.GetByteCount()" />
        int IBinaryInteger<nint>.GetByteCount() => sizeof(nint_t);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryWriteBigEndian(Span{byte}, out int)" />
        bool IBinaryInteger<nint>.TryWriteBigEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(nint_t))
            {
                nint_t value = (nint_t)_value;

                if (BitConverter.IsLittleEndian)
                {
                    value = BinaryPrimitives.ReverseEndianness(value);
                }
                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);

                bytesWritten = sizeof(nint_t);
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryWriteLittleEndian(Span{byte}, out int)" />
        bool IBinaryInteger<nint>.TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(nint_t))
            {
                nint_t value = (nint_t)_value;

                if (!BitConverter.IsLittleEndian)
                {
                    value = BinaryPrimitives.ReverseEndianness(value);
                }
                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);

                bytesWritten = sizeof(nint_t);
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
        static nint IBinaryNumber<nint>.AllBitsSet => -1;

        /// <inheritdoc cref="IBinaryNumber{TSelf}.IsPow2(TSelf)" />
        public static bool IsPow2(nint value) => BitOperations.IsPow2(value);

        /// <inheritdoc cref="IBinaryNumber{TSelf}.Log2(TSelf)" />
        public static nint Log2(nint value)
        {
            if (value < 0)
            {
                ThrowHelper.ThrowValueArgumentOutOfRange_NeedNonNegNumException();
            }
            return BitOperations.Log2((nuint)value);
        }

        //
        // IBitwiseOperators
        //

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseAnd(TSelf, TOther)" />
        static nint IBitwiseOperators<nint, nint, nint>.operator &(nint left, nint right) => left & right;

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseOr(TSelf, TOther)" />
        static nint IBitwiseOperators<nint, nint, nint>.operator |(nint left, nint right) => left | right;

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_ExclusiveOr(TSelf, TOther)" />
        static nint IBitwiseOperators<nint, nint, nint>.operator ^(nint left, nint right) => left ^ right;

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_OnesComplement(TSelf)" />
        static nint IBitwiseOperators<nint, nint, nint>.operator ~(nint value) => ~value;

        //
        // IComparisonOperators
        //

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_LessThan(TSelf, TOther)" />
        static bool IComparisonOperators<nint, nint, bool>.operator <(nint left, nint right) => left < right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_LessThanOrEqual(TSelf, TOther)" />
        static bool IComparisonOperators<nint, nint, bool>.operator <=(nint left, nint right) => left <= right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_GreaterThan(TSelf, TOther)" />
        static bool IComparisonOperators<nint, nint, bool>.operator >(nint left, nint right) => left > right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_GreaterThanOrEqual(TSelf, TOther)" />
        static bool IComparisonOperators<nint, nint, bool>.operator >=(nint left, nint right) => left >= right;

        //
        // IDecrementOperators
        //

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        static nint IDecrementOperators<nint>.operator --(nint value) => --value;

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        static nint IDecrementOperators<nint>.operator checked --(nint value) => checked(--value);

        //
        // IDivisionOperators
        //

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_Division(TSelf, TOther)" />
        static nint IDivisionOperators<nint, nint, nint>.operator /(nint left, nint right) => left / right;

        //
        // IIncrementOperators
        //

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_Increment(TSelf)" />
        static nint IIncrementOperators<nint>.operator ++(nint value) => ++value;

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_CheckedIncrement(TSelf)" />
        static nint IIncrementOperators<nint>.operator checked ++(nint value) => checked(++value);

        //
        // IMinMaxValue
        //

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MinValue" />
        static nint IMinMaxValue<nint>.MinValue => MinValue;

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MaxValue" />
        static nint IMinMaxValue<nint>.MaxValue => MaxValue;

        //
        // IModulusOperators
        //

        /// <inheritdoc cref="IModulusOperators{TSelf, TOther, TResult}.op_Modulus(TSelf, TOther)" />
        static nint IModulusOperators<nint, nint, nint>.operator %(nint left, nint right) => left % right;

        //
        // IMultiplicativeIdentity
        //

        /// <inheritdoc cref="IMultiplicativeIdentity{TSelf, TResult}.MultiplicativeIdentity" />
        static nint IMultiplicativeIdentity<nint, nint>.MultiplicativeIdentity => 1;

        //
        // IMultiplyOperators
        //

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_Multiply(TSelf, TOther)" />
        static nint IMultiplyOperators<nint, nint, nint>.operator *(nint left, nint right) => left * right;

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_CheckedMultiply(TSelf, TOther)" />
        static nint IMultiplyOperators<nint, nint, nint>.operator checked *(nint left, nint right) => checked(left * right);

        //
        // INumber
        //

        /// <inheritdoc cref="INumber{TSelf}.Clamp(TSelf, TSelf, TSelf)" />
        public static nint Clamp(nint value, nint min, nint max) => Math.Clamp(value, min, max);

        /// <inheritdoc cref="INumber{TSelf}.CopySign(TSelf, TSelf)" />
        public static nint CopySign(nint value, nint sign)
        {
            nint absValue = value;

            if (absValue < 0)
            {
                absValue = -absValue;
            }

            if (sign >= 0)
            {
                if (absValue < 0)
                {
                    Math.ThrowNegateTwosCompOverflow();
                }

                return absValue;
            }

            return -absValue;
        }

        /// <inheritdoc cref="INumber{TSelf}.Max(TSelf, TSelf)" />
        public static nint Max(nint x, nint y) => Math.Max(x, y);

        /// <inheritdoc cref="INumber{TSelf}.MaxNumber(TSelf, TSelf)" />
        static nint INumber<nint>.MaxNumber(nint x, nint y) => Max(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Min(TSelf, TSelf)" />
        public static nint Min(nint x, nint y) => Math.Min(x, y);

        /// <inheritdoc cref="INumber{TSelf}.MinNumber(TSelf, TSelf)" />
        static nint INumber<nint>.MinNumber(nint x, nint y) => Min(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Sign(TSelf)" />
        public static int Sign(nint value) => Math.Sign(value);

        //
        // INumberBase
        //

        /// <inheritdoc cref="INumberBase{TSelf}.One" />
        static nint INumberBase<nint>.One => 1;

        /// <inheritdoc cref="INumberBase{TSelf}.Radix" />
        static int INumberBase<nint>.Radix => 2;

        /// <inheritdoc cref="INumberBase{TSelf}.Zero" />
        static nint INumberBase<nint>.Zero => 0;

        /// <inheritdoc cref="INumberBase{TSelf}.Abs(TSelf)" />
        public static nint Abs(nint value) => Math.Abs(value);

        /// <inheritdoc cref="INumberBase{TSelf}.CreateChecked{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint CreateChecked<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            nint result;

            if (typeof(TOther) == typeof(nint))
            {
                result = (nint)(object)value;
            }
            else if (!TryConvertFromChecked(value, out result) && !TOther.TryConvertToChecked(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateSaturating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint CreateSaturating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            nint result;

            if (typeof(TOther) == typeof(nint))
            {
                result = (nint)(object)value;
            }
            else if (!TryConvertFromSaturating(value, out result) && !TOther.TryConvertToSaturating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateTruncating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint CreateTruncating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            nint result;

            if (typeof(TOther) == typeof(nint))
            {
                result = (nint)(object)value;
            }
            else if (!TryConvertFromTruncating(value, out result) && !TOther.TryConvertToTruncating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsCanonical(TSelf)" />
        static bool INumberBase<nint>.IsCanonical(nint value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsComplexNumber(TSelf)" />
        static bool INumberBase<nint>.IsComplexNumber(nint value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsEvenInteger(TSelf)" />
        public static bool IsEvenInteger(nint value) => (value & 1) == 0;

        /// <inheritdoc cref="INumberBase{TSelf}.IsFinite(TSelf)" />
        static bool INumberBase<nint>.IsFinite(nint value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsImaginaryNumber(TSelf)" />
        static bool INumberBase<nint>.IsImaginaryNumber(nint value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsInfinity(TSelf)" />
        static bool INumberBase<nint>.IsInfinity(nint value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsInteger(TSelf)" />
        static bool INumberBase<nint>.IsInteger(nint value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsNaN(TSelf)" />
        static bool INumberBase<nint>.IsNaN(nint value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsNegative(TSelf)" />
        public static bool IsNegative(nint value) => value < 0;

        /// <inheritdoc cref="INumberBase{TSelf}.IsNegativeInfinity(TSelf)" />
        static bool INumberBase<nint>.IsNegativeInfinity(nint value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsNormal(TSelf)" />
        static bool INumberBase<nint>.IsNormal(nint value) => value != 0;

        /// <inheritdoc cref="INumberBase{TSelf}.IsOddInteger(TSelf)" />
        public static bool IsOddInteger(nint value) => (value & 1) != 0;

        /// <inheritdoc cref="INumberBase{TSelf}.IsPositive(TSelf)" />
        public static bool IsPositive(nint value) => value >= 0;

        /// <inheritdoc cref="INumberBase{TSelf}.IsPositiveInfinity(TSelf)" />
        static bool INumberBase<nint>.IsPositiveInfinity(nint value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsRealNumber(TSelf)" />
        static bool INumberBase<nint>.IsRealNumber(nint value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsSubnormal(TSelf)" />
        static bool INumberBase<nint>.IsSubnormal(nint value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsZero(TSelf)" />
        static bool INumberBase<nint>.IsZero(nint value) => (value == 0);

        /// <inheritdoc cref="INumberBase{TSelf}.MaxMagnitude(TSelf, TSelf)" />
        public static nint MaxMagnitude(nint x, nint y)
        {
            nint absX = x;

            if (absX < 0)
            {
                absX = -absX;

                if (absX < 0)
                {
                    return x;
                }
            }

            nint absY = y;

            if (absY < 0)
            {
                absY = -absY;

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
        static nint INumberBase<nint>.MaxMagnitudeNumber(nint x, nint y) => MaxMagnitude(x, y);

        /// <inheritdoc cref="INumberBase{TSelf}.MinMagnitude(TSelf, TSelf)" />
        public static nint MinMagnitude(nint x, nint y)
        {
            nint absX = x;

            if (absX < 0)
            {
                absX = -absX;

                if (absX < 0)
                {
                    return y;
                }
            }

            nint absY = y;

            if (absY < 0)
            {
                absY = -absY;

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
        static nint INumberBase<nint>.MinMagnitudeNumber(nint x, nint y) => MinMagnitude(x, y);

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromChecked{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<nint>.TryConvertFromChecked<TOther>(TOther value, out nint result) => TryConvertFromChecked(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvertFromChecked<TOther>(TOther value, out nint result)
            where TOther : INumberBase<TOther>
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `nint` will handle the other signed types and
            // `ConvertTo` will handle the unsigned types

            if (typeof(TOther) == typeof(double))
            {
                double actualValue = (double)(object)value;
                result = checked((nint)actualValue);
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualValue = (Half)(object)value;
                result = checked((nint)actualValue);
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualValue = (short)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualValue = (int)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualValue = (long)(object)value;
                result = checked((nint)actualValue);
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualValue = (Int128)(object)value;
                result = checked((nint)actualValue);
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
                result = checked((nint)actualValue);
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
        static bool INumberBase<nint>.TryConvertFromSaturating<TOther>(TOther value, out nint result) => TryConvertFromSaturating(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvertFromSaturating<TOther>(TOther value, out nint result)
            where TOther : INumberBase<TOther>
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `nint` will handle the other signed types and
            // `ConvertTo` will handle the unsigned types

            if (typeof(TOther) == typeof(double))
            {
                double actualValue = (double)(object)value;
                result = (actualValue >= nint_t.MaxValue) ? unchecked((nint)nint_t.MaxValue) :
                         (actualValue <= nint_t.MinValue) ? unchecked((nint)nint_t.MinValue) : (nint)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualValue = (Half)(object)value;
                result = (actualValue == Half.PositiveInfinity) ? unchecked((nint)nint_t.MaxValue) :
                         (actualValue == Half.NegativeInfinity) ? unchecked((nint)nint_t.MinValue) : (nint)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualValue = (short)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualValue = (int)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualValue = (long)(object)value;
                result = (actualValue >= nint_t.MaxValue) ? unchecked((nint)nint_t.MaxValue) :
                         (actualValue <= nint_t.MinValue) ? unchecked((nint)nint_t.MinValue) : (nint)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualValue = (Int128)(object)value;
                result = (actualValue >= nint_t.MaxValue) ? unchecked((nint)nint_t.MaxValue) :
                         (actualValue <= nint_t.MinValue) ? unchecked((nint)nint_t.MinValue) : (nint)actualValue;
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
                result = (actualValue >= nint_t.MaxValue) ? unchecked((nint)nint_t.MaxValue) :
                         (actualValue <= nint_t.MinValue) ? unchecked((nint)nint_t.MinValue) : (nint)actualValue;
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
        static bool INumberBase<nint>.TryConvertFromTruncating<TOther>(TOther value, out nint result) => TryConvertFromTruncating(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvertFromTruncating<TOther>(TOther value, out nint result)
            where TOther : INumberBase<TOther>
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `nint` will handle the other signed types and
            // `ConvertTo` will handle the unsigned types

            if (typeof(TOther) == typeof(double))
            {
                double actualValue = (double)(object)value;
                result = (actualValue >= nint_t.MaxValue) ? unchecked((nint)nint_t.MaxValue) :
                         (actualValue <= nint_t.MinValue) ? unchecked((nint)nint_t.MinValue) : (nint)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualValue = (Half)(object)value;
                result = (actualValue == Half.PositiveInfinity) ? unchecked((nint)nint_t.MaxValue) :
                         (actualValue == Half.NegativeInfinity) ? unchecked((nint)nint_t.MinValue) : (nint)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualValue = (short)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualValue = (int)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualValue = (long)(object)value;
                result = (nint)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualValue = (Int128)(object)value;
                result = (nint)actualValue;
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
                result = (actualValue >= nint_t.MaxValue) ? unchecked((nint)nint_t.MaxValue) :
                         (actualValue <= nint_t.MinValue) ? unchecked((nint)nint_t.MinValue) : (nint)actualValue;
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
        static bool INumberBase<nint>.TryConvertToChecked<TOther>(nint value, [NotNullWhen(true)] out TOther result)
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `nint` will handle the other signed types and
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
                result = default!;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToSaturating{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<nint>.TryConvertToSaturating<TOther>(nint value, [NotNullWhen(true)] out TOther result)
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `nint` will handle the other signed types and
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
                char actualResult = (value >= char.MaxValue) ? char.MaxValue :
                                    (value <= char.MinValue) ? char.MinValue : (char)value;
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
                ushort actualResult = (value >= ushort.MaxValue) ? ushort.MaxValue :
                                      (value <= ushort.MinValue) ? ushort.MinValue : (ushort)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualResult = (value >= uint.MaxValue) ? uint.MaxValue :
                                    (value <= uint.MinValue) ? uint.MinValue : (uint)value;
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
                result = default!;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToTruncating{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<nint>.TryConvertToTruncating<TOther>(nint value, [NotNullWhen(true)] out TOther result)
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `nint` will handle the other signed types and
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
                result = default!;
                return false;
            }
        }

        //
        // IShiftOperators
        //

        /// <inheritdoc cref="IShiftOperators{TSelf, TOther, TResult}.op_LeftShift(TSelf, TOther)" />
        static nint IShiftOperators<nint, int, nint>.operator <<(nint value, int shiftAmount) => value << shiftAmount;

        /// <inheritdoc cref="IShiftOperators{TSelf, TOther, TResult}.op_RightShift(TSelf, TOther)" />
        static nint IShiftOperators<nint, int, nint>.operator >>(nint value, int shiftAmount) => value >> shiftAmount;

        /// <inheritdoc cref="IShiftOperators{TSelf, TOther, TResult}.op_UnsignedRightShift(TSelf, TOther)" />
        static nint IShiftOperators<nint, int, nint>.operator >>>(nint value, int shiftAmount) => value >>> shiftAmount;

        //
        // ISignedNumber
        //

        /// <inheritdoc cref="ISignedNumber{TSelf}.NegativeOne" />
        static nint ISignedNumber<nint>.NegativeOne => -1;

        //
        // ISubtractionOperators
        //

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_Subtraction(TSelf, TOther)" />
        static nint ISubtractionOperators<nint, nint, nint>.operator -(nint left, nint right) => left - right;

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_CheckedSubtraction(TSelf, TOther)" />
        static nint ISubtractionOperators<nint, nint, nint>.operator checked -(nint left, nint right) => checked(left - right);

        //
        // IUnaryNegationOperators
        //

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_UnaryNegation(TSelf)" />
        static nint IUnaryNegationOperators<nint, nint>.operator -(nint value) => -value;

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_CheckedUnaryNegation(TSelf)" />
        static nint IUnaryNegationOperators<nint, nint>.operator checked -(nint value) => checked(-value);

        //
        // IUnaryPlusOperators
        //

        /// <inheritdoc cref="IUnaryPlusOperators{TSelf, TResult}.op_UnaryPlus(TSelf)" />
        static nint IUnaryPlusOperators<nint, nint>.operator +(nint value) => +value;
    }
}
