// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;

#pragma warning disable SA1121 // explicitly using type aliases instead of built-in types
#pragma warning disable CA1066 // Implement IEquatable when overriding Object.Equals

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
        // WARNING: We allow diagnostic tools to directly inspect this member (_value).
        // See https://github.com/dotnet/corert/blob/master/Documentation/design-docs/diagnostics/diagnostics-tools-contract.md for more details.
        // Please do not change the type, the name, or the semantic usage of this member without understanding the implication for tools.
        // Get in touch with the diagnostics team if you have questions.
        private readonly unsafe void* _value; // Do not rename (binary serialization)

        [Intrinsic]
        public static readonly IntPtr Zero;

        [NonVersionable]
        public unsafe IntPtr(int value)
        {
            _value = (void*)value;
        }

        [NonVersionable]
        public unsafe IntPtr(long value)
        {
#if TARGET_64BIT
            _value = (void*)value;
#else
            _value = (void*)checked((int)value);
#endif
        }

        [CLSCompliant(false)]
        [NonVersionable]
        public unsafe IntPtr(void* value)
        {
            _value = value;
        }

        private unsafe IntPtr(SerializationInfo info, StreamingContext context)
        {
            long l = info.GetInt64("value");

            if (Size == 4 && (l > int.MaxValue || l < int.MinValue))
                throw new ArgumentException(SR.Serialization_InvalidPtrValue);

            _value = (void*)l;
        }

        unsafe void ISerializable.GetObjectData(SerializationInfo info!!, StreamingContext context)
        {
            info.AddValue("value", ToInt64());
        }

        public override unsafe bool Equals([NotNullWhen(true)] object? obj) =>
            obj is IntPtr other &&
            _value == other._value;

        public override unsafe int GetHashCode()
        {
#if TARGET_64BIT
            long l = (long)_value;
            return unchecked((int)l) ^ (int)(l >> 32);
#else
            return unchecked((int)_value);
#endif
        }

        [NonVersionable]
        public unsafe int ToInt32()
        {
#if TARGET_64BIT
            long l = (long)_value;
            return checked((int)l);
#else
            return (int)_value;
#endif
        }

        [NonVersionable]
        public unsafe long ToInt64() =>
            (nint)_value;

        [NonVersionable]
        public static unsafe explicit operator IntPtr(int value) =>
            new IntPtr(value);

        [NonVersionable]
        public static unsafe explicit operator IntPtr(long value) =>
            new IntPtr(value);

        [CLSCompliant(false)]
        [NonVersionable]
        public static unsafe explicit operator IntPtr(void* value) =>
            new IntPtr(value);

        [CLSCompliant(false)]
        [NonVersionable]
        public static unsafe explicit operator void*(IntPtr value) =>
            value._value;

        [NonVersionable]
        public static unsafe explicit operator int(IntPtr value)
        {
#if TARGET_64BIT
            long l = (long)value._value;
            return checked((int)l);
#else
            return (int)value._value;
#endif
        }

        [NonVersionable]
        public static unsafe explicit operator long(IntPtr value) =>
            (nint)value._value;

        [NonVersionable]
        public static unsafe bool operator ==(IntPtr value1, IntPtr value2) =>
            value1._value == value2._value;

        [NonVersionable]
        public static unsafe bool operator !=(IntPtr value1, IntPtr value2) =>
            value1._value != value2._value;

        [NonVersionable]
        public static IntPtr Add(IntPtr pointer, int offset) =>
            pointer + offset;

        [NonVersionable]
        public static unsafe IntPtr operator +(IntPtr pointer, int offset) =>
            (nint)pointer._value + offset;

        [NonVersionable]
        public static IntPtr Subtract(IntPtr pointer, int offset) =>
            pointer - offset;

        [NonVersionable]
        public static unsafe IntPtr operator -(IntPtr pointer, int offset) =>
            (nint)pointer._value - offset;

        public static int Size
        {
            [NonVersionable]
            get => sizeof(nint_t);
        }

        [CLSCompliant(false)]
        [NonVersionable]
        public unsafe void* ToPointer() => _value;

        /// <inheritdoc cref="INumber{TSelf}.Max(TSelf, TSelf)" />
        public static IntPtr MaxValue
        {
            [NonVersionable]
            get => (IntPtr)nint_t.MaxValue;
        }

        /// <inheritdoc cref="INumber{TSelf}.Min(TSelf, TSelf)" />
        public static IntPtr MinValue
        {
            [NonVersionable]
            get => (IntPtr)nint_t.MinValue;
        }

        // Don't just delegate to nint_t.CompareTo as it needs to throw when not IntPtr
        public unsafe int CompareTo(object? value)
        {
            if (value is null)
            {
                return 1;
            }
            if (value is nint i)
            {
                if ((nint)_value < i) return -1;
                if ((nint)_value > i) return 1;
                return 0;
            }

            throw new ArgumentException(SR.Arg_MustBeIntPtr);
        }

        public unsafe int CompareTo(IntPtr value) => ((nint_t)_value).CompareTo((nint_t)value);

        [NonVersionable]
        public unsafe bool Equals(IntPtr other) => (nint_t)_value == (nint_t)other;

        public unsafe override string ToString() => ((nint_t)_value).ToString();
        public unsafe string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format) => ((nint_t)_value).ToString(format);
        public unsafe string ToString(IFormatProvider? provider) => ((nint_t)_value).ToString(provider);
        public unsafe string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider) => ((nint_t)_value).ToString(format, provider);

        public unsafe bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null) =>
            ((nint_t)_value).TryFormat(destination, out charsWritten, format, provider);

        public static IntPtr Parse(string s) => (IntPtr)nint_t.Parse(s);
        public static IntPtr Parse(string s, NumberStyles style) => (IntPtr)nint_t.Parse(s, style);
        public static IntPtr Parse(string s, IFormatProvider? provider) => (IntPtr)nint_t.Parse(s, provider);
        public static IntPtr Parse(string s, NumberStyles style, IFormatProvider? provider) => (IntPtr)nint_t.Parse(s, style, provider);
        public static IntPtr Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => (IntPtr)nint_t.Parse(s, provider);
        public static IntPtr Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null) => (IntPtr)nint_t.Parse(s, style, provider);

        public static bool TryParse([NotNullWhen(true)] string? s, out IntPtr result)
        {
            Unsafe.SkipInit(out result);
            return nint_t.TryParse(s, out Unsafe.As<IntPtr, nint_t>(ref result));
        }

        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out IntPtr result)
        {
            Unsafe.SkipInit(out result);
            return nint_t.TryParse(s, provider, out Unsafe.As<IntPtr, nint_t>(ref result));
        }

        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out IntPtr result)
        {
            Unsafe.SkipInit(out result);
            return nint_t.TryParse(s, style, provider, out Unsafe.As<IntPtr, nint_t>(ref result));
        }

        public static bool TryParse(ReadOnlySpan<char> s, out IntPtr result)
        {
            Unsafe.SkipInit(out result);
            return nint_t.TryParse(s, out Unsafe.As<IntPtr, nint_t>(ref result));
        }

        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out IntPtr result)
        {
            Unsafe.SkipInit(out result);
            return nint_t.TryParse(s, provider, out Unsafe.As<IntPtr, nint_t>(ref result));
        }

        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out IntPtr result)
        {
            Unsafe.SkipInit(out result);
            return nint_t.TryParse(s, style, provider, out Unsafe.As<IntPtr, nint_t>(ref result));
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
        static (nint Quotient, nint Remainder) IBinaryInteger<nint>.DivRem(nint left, nint right) => Math.DivRem(left, right);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.LeadingZeroCount(TSelf)" />
        static nint IBinaryInteger<nint>.LeadingZeroCount(nint value)
        {
            if (Environment.Is64BitProcess)
            {
                return BitOperations.LeadingZeroCount((ulong)value);
            }
            else
            {
                return BitOperations.LeadingZeroCount((uint)value);
            }
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.PopCount(TSelf)" />
        static nint IBinaryInteger<nint>.PopCount(nint value)
        {
            if (Environment.Is64BitProcess)
            {
                return BitOperations.PopCount((ulong)value);
            }
            else
            {
                return BitOperations.PopCount((uint)value);
            }
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.RotateLeft(TSelf, int)" />
        static nint IBinaryInteger<nint>.RotateLeft(nint value, int rotateAmount)
        {
            if (Environment.Is64BitProcess)
            {
                return (nint)BitOperations.RotateLeft((ulong)value, rotateAmount);
            }
            else
            {
                return (nint)BitOperations.RotateLeft((uint)value, rotateAmount);
            }
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.RotateRight(TSelf, int)" />
        static nint IBinaryInteger<nint>.RotateRight(nint value, int rotateAmount)
        {
            if (Environment.Is64BitProcess)
            {
                return (nint)BitOperations.RotateRight((ulong)value, rotateAmount);
            }
            else
            {
                return (nint)BitOperations.RotateRight((uint)value, rotateAmount);
            }
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TrailingZeroCount(TSelf)" />
        static nint IBinaryInteger<nint>.TrailingZeroCount(nint value)
        {
            if (Environment.Is64BitProcess)
            {
                return BitOperations.TrailingZeroCount((ulong)value);
            }
            else
            {
                return BitOperations.TrailingZeroCount((uint)value);
            }
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.GetShortestBitLength()" />
        unsafe long IBinaryInteger<nint>.GetShortestBitLength()
        {
            nint value = (nint)_value;

            if (value >= 0)
            {
                return (sizeof(nint) * 8) - BitOperations.LeadingZeroCount((nuint)value);
            }
            else
            {
                return (sizeof(nint) * 8) + 1 - BitOperations.LeadingZeroCount((nuint)(~value));
            }
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.GetByteCount()" />
        unsafe int IBinaryInteger<nint>.GetByteCount() => sizeof(nint);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryWriteLittleEndian(Span{byte}, out int)" />
        unsafe bool IBinaryInteger<nint>.TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(nint))
            {
                nint value = BitConverter.IsLittleEndian ? (nint)_value : (nint)BinaryPrimitives.ReverseEndianness((nint)_value);
                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);

                bytesWritten = sizeof(nint);
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
        static bool IBinaryNumber<nint>.IsPow2(nint value) => BitOperations.IsPow2(value);

        /// <inheritdoc cref="IBinaryNumber{TSelf}.Log2(TSelf)" />
        static nint IBinaryNumber<nint>.Log2(nint value)
        {
            if (value < 0)
            {
                ThrowHelper.ThrowValueArgumentOutOfRange_NeedNonNegNumException();
            }

            if (Environment.Is64BitProcess)
            {
                return BitOperations.Log2((ulong)value);
            }
            else
            {
                return BitOperations.Log2((uint)value);
            }
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

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_LessThan(TSelf, TOther)" />
        static bool IComparisonOperators<nint, nint>.operator <(nint left, nint right) => left < right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_LessThanOrEqual(TSelf, TOther)" />
        static bool IComparisonOperators<nint, nint>.operator <=(nint left, nint right) => left <= right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_GreaterThan(TSelf, TOther)" />
        static bool IComparisonOperators<nint, nint>.operator >(nint left, nint right) => left > right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_GreaterThanOrEqual(TSelf, TOther)" />
        static bool IComparisonOperators<nint, nint>.operator >=(nint left, nint right) => left >= right;

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

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_CheckedDivision(TSelf, TOther)" />
        static nint IDivisionOperators<nint, nint, nint>.operator checked /(nint left, nint right) => left / right;

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

        /// <inheritdoc cref="INumber{TSelf}.Abs(TSelf)" />
        static nint INumber<nint>.Abs(nint value) => Math.Abs(value);

        /// <inheritdoc cref="INumber{TSelf}.Clamp(TSelf, TSelf, TSelf)" />
        static nint INumber<nint>.Clamp(nint value, nint min, nint max) => Math.Clamp(value, min, max);

        /// <inheritdoc cref="INumber{TSelf}.CopySign(TSelf, TSelf)" />
        static nint INumber<nint>.CopySign(nint value, nint sign)
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

        /// <inheritdoc cref="INumber{TSelf}.CreateChecked{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static nint INumber<nint>.CreateChecked<TOther>(TOther value)
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
                return checked((nint)(decimal)(object)value);
            }
            else if (typeof(TOther) == typeof(double))
            {
                return checked((nint)(double)(object)value);
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
                return checked((nint)(long)(object)value);
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
                return checked((nint)(float)(object)value);
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                return (ushort)(object)value;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                return checked((nint)(uint)(object)value);
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                return checked((nint)(ulong)(object)value);
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                return checked((nint)(nuint)(object)value);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        /// <inheritdoc cref="INumber{TSelf}.CreateSaturating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static nint INumber<nint>.CreateSaturating<TOther>(TOther value)
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
                return (actualValue > nint.MaxValue) ? MaxValue :
                       (actualValue < nint.MinValue) ? MinValue : (nint)actualValue;
            }
            else if (typeof(TOther) == typeof(double))
            {
                var actualValue = (double)(object)value;
                return (actualValue > nint.MaxValue) ? MaxValue :
                       (actualValue < nint.MinValue) ? MinValue : (nint)actualValue;
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
                var actualValue = (long)(object)value;
                return (actualValue > nint.MaxValue) ? MaxValue :
                       (actualValue < nint.MinValue) ? MinValue : (nint)actualValue;
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
                return (actualValue > nint.MaxValue) ? MaxValue :
                       (actualValue < nint.MinValue) ? MinValue : (nint)actualValue;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                return (ushort)(object)value;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                var actualValue = (uint)(object)value;
                return (actualValue > nint.MaxValue) ? MaxValue : (nint)actualValue;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                var actualValue = (ulong)(object)value;
                return (actualValue > (nuint)nint.MaxValue) ? MaxValue : (nint)actualValue;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                var actualValue = (nuint)(object)value;
                return (actualValue > (nuint)nint.MaxValue) ? MaxValue : (nint)actualValue;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        /// <inheritdoc cref="INumber{TSelf}.CreateTruncating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static nint INumber<nint>.CreateTruncating<TOther>(TOther value)
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
                return (nint)(decimal)(object)value;
            }
            else if (typeof(TOther) == typeof(double))
            {
                return (nint)(double)(object)value;
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
                return (nint)(long)(object)value;
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
                return (nint)(float)(object)value;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                return (ushort)(object)value;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                return (nint)(uint)(object)value;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                return (nint)(ulong)(object)value;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                return (nint)(nuint)(object)value;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        /// <inheritdoc cref="INumber{TSelf}.IsNegative(TSelf)" />
        static bool INumber<nint>.IsNegative(nint value) => value < 0;

        /// <inheritdoc cref="INumber{TSelf}.Max(TSelf, TSelf)" />
        static nint INumber<nint>.Max(nint x, nint y) => Math.Max(x, y);

        /// <inheritdoc cref="INumber{TSelf}.MaxMagnitude(TSelf, TSelf)" />
        static nint INumber<nint>.MaxMagnitude(nint x, nint y)
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

            return (absX >= absY) ? x : y;
        }

        /// <inheritdoc cref="INumber{TSelf}.Min(TSelf, TSelf)" />
        static nint INumber<nint>.Min(nint x, nint y) => Math.Min(x, y);

        /// <inheritdoc cref="INumber{TSelf}.MinMagnitude(TSelf, TSelf)" />
        static nint INumber<nint>.MinMagnitude(nint x, nint y)
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

            return (absX <= absY) ? x : y;
        }

        /// <inheritdoc cref="INumber{TSelf}.Sign(TSelf)" />
        static int INumber<nint>.Sign(nint value) => Math.Sign(value);

        /// <inheritdoc cref="INumber{TSelf}.TryCreate{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumber<nint>.TryCreate<TOther>(TOther value, out nint result)
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

                if ((actualValue < nint.MinValue) || (actualValue > nint.MaxValue))
                {
                    result = default;
                    return false;
                }

                result = (nint)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                var actualValue = (double)(object)value;

                if ((actualValue < nint.MinValue) || (actualValue > nint.MaxValue))
                {
                    result = default;
                    return false;
                }

                result = (nint)actualValue;
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
                var actualValue = (long)(object)value;

                if ((actualValue < nint.MinValue) || (actualValue > nint.MaxValue))
                {
                    result = default;
                    return false;
                }

                result = (nint)actualValue;
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

                if ((actualValue < nint.MinValue) || (actualValue > nint.MaxValue))
                {
                    result = default;
                    return false;
                }

                result = (nint)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                result = (ushort)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                var actualValue = (uint)(object)value;

                if (actualValue > nint.MaxValue)
                {
                    result = default;
                    return false;
                }

                result = (nint)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                var actualValue = (ulong)(object)value;

                if (actualValue > (nuint)nint.MaxValue)
                {
                    result = default;
                    return false;
                }

                result = (nint)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                var actualValue = (nuint)(object)value;

                if (actualValue > (nuint)nint.MaxValue)
                {
                    result = default;
                    return false;
                }

                result = (nint)actualValue;
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
        static nint INumberBase<nint>.One => 1;

        /// <inheritdoc cref="INumberBase{TSelf}.Zero" />
        static nint INumberBase<nint>.Zero => 0;

        //
        // IShiftOperators
        //

        /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_LeftShift(TSelf, int)" />
        static nint IShiftOperators<nint, nint>.operator <<(nint value, int shiftAmount) => value << shiftAmount;

        /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_RightShift(TSelf, int)" />
        static nint IShiftOperators<nint, nint>.operator >>(nint value, int shiftAmount) => value >> shiftAmount;

        /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_UnsignedRightShift(TSelf, int)" />
        static nint IShiftOperators<nint, nint>.operator >>>(nint value, int shiftAmount) => value >>> shiftAmount;

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
