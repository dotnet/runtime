// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
using nuint_t = System.UInt64;
#else
using nuint_t = System.UInt32;
#endif

namespace System
{
    [Serializable]
    [CLSCompliant(false)]
    [StructLayout(LayoutKind.Sequential)]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public readonly struct UIntPtr
        : IEquatable<nuint>,
          IComparable,
          IComparable<nuint>,
          ISpanFormattable,
          ISerializable,
          IBinaryInteger<nuint>,
          IMinMaxValue<nuint>,
          IUnsignedNumber<nuint>
    {
        private readonly unsafe void* _value; // Do not rename (binary serialization)

        [Intrinsic]
        public static readonly UIntPtr Zero;

        [NonVersionable]
        public unsafe UIntPtr(uint value)
        {
            _value = (void*)value;
        }

        [NonVersionable]
        public unsafe UIntPtr(ulong value)
        {
#if TARGET_64BIT
            _value = (void*)value;
#else
            _value = (void*)checked((uint)value);
#endif
        }

        [NonVersionable]
        public unsafe UIntPtr(void* value)
        {
            _value = value;
        }

        private unsafe UIntPtr(SerializationInfo info, StreamingContext context)
        {
            ulong l = info.GetUInt64("value");

            if (Size == 4 && l > uint.MaxValue)
                throw new ArgumentException(SR.Serialization_InvalidPtrValue);

            _value = (void*)l;
        }

        void ISerializable.GetObjectData(SerializationInfo info!!, StreamingContext context)
        {
            info.AddValue("value", ToUInt64());
        }

        public override unsafe bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is UIntPtr)
            {
                return _value == ((UIntPtr)obj)._value;
            }
            return false;
        }

        public override unsafe int GetHashCode()
        {
#if TARGET_64BIT
            ulong l = (ulong)_value;
            return unchecked((int)l) ^ (int)(l >> 32);
#else
            return unchecked((int)_value);
#endif
        }

        [NonVersionable]
        public unsafe uint ToUInt32()
        {
#if TARGET_64BIT
            return checked((uint)_value);
#else
            return (uint)_value;
#endif
        }

        [NonVersionable]
        public unsafe ulong ToUInt64() => (ulong)_value;

        [NonVersionable]
        public static explicit operator UIntPtr(uint value) =>
            new UIntPtr(value);

        [NonVersionable]
        public static explicit operator UIntPtr(ulong value) =>
            new UIntPtr(value);

        [NonVersionable]
        public static unsafe explicit operator UIntPtr(void* value) =>
            new UIntPtr(value);

        [NonVersionable]
        public static unsafe explicit operator void*(UIntPtr value) =>
            value._value;

        [NonVersionable]
        public static unsafe explicit operator uint(UIntPtr value) =>
#if TARGET_64BIT
            checked((uint)value._value);
#else
            (uint)value._value;
#endif

        [NonVersionable]
        public static unsafe explicit operator ulong(UIntPtr value) =>
            (ulong)value._value;

        [NonVersionable]
        public static unsafe bool operator ==(UIntPtr value1, UIntPtr value2) =>
            value1._value == value2._value;

        [NonVersionable]
        public static unsafe bool operator !=(UIntPtr value1, UIntPtr value2) =>
            value1._value != value2._value;

        [NonVersionable]
        public static UIntPtr Add(UIntPtr pointer, int offset) =>
            pointer + offset;

        [NonVersionable]
        public static unsafe UIntPtr operator +(UIntPtr pointer, int offset) =>
            (nuint)pointer._value + (nuint)offset;

        [NonVersionable]
        public static UIntPtr Subtract(UIntPtr pointer, int offset) =>
            pointer - offset;

        [NonVersionable]
        public static unsafe UIntPtr operator -(UIntPtr pointer, int offset) =>
            (nuint)pointer._value - (nuint)offset;

        public static int Size
        {
            [NonVersionable]
            get => sizeof(nuint_t);
        }

        [NonVersionable]
        public unsafe void* ToPointer() => _value;

        /// <inheritdoc cref="INumber{TSelf}.Max(TSelf, TSelf)" />
        public static UIntPtr MaxValue
        {
            [NonVersionable]
            get => (UIntPtr)nuint_t.MaxValue;
        }

        /// <inheritdoc cref="INumber{TSelf}.Min(TSelf, TSelf)" />
        public static UIntPtr MinValue
        {
            [NonVersionable]
            get => (UIntPtr)nuint_t.MinValue;
        }

        public unsafe int CompareTo(object? value)
        {
            if (value is null)
            {
                return 1;
            }
            if (value is nuint i)
            {
                if ((nuint)_value < i) return -1;
                if ((nuint)_value > i) return 1;
                return 0;
            }

            throw new ArgumentException(SR.Arg_MustBeUIntPtr);
        }

        public unsafe int CompareTo(UIntPtr value) => ((nuint_t)_value).CompareTo((nuint_t)value);

        [NonVersionable]
        public unsafe bool Equals(UIntPtr other) => (nuint)_value == (nuint)other;

        public unsafe override string ToString() => ((nuint_t)_value).ToString();
        public unsafe string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format) => ((nuint_t)_value).ToString(format);
        public unsafe string ToString(IFormatProvider? provider) => ((nuint_t)_value).ToString(provider);
        public unsafe string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider) => ((nuint_t)_value).ToString(format, provider);

        public unsafe bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null) =>
            ((nuint_t)_value).TryFormat(destination, out charsWritten, format, provider);

        public static UIntPtr Parse(string s) => (UIntPtr)nuint_t.Parse(s);
        public static UIntPtr Parse(string s, NumberStyles style) => (UIntPtr)nuint_t.Parse(s, style);
        public static UIntPtr Parse(string s, IFormatProvider? provider) => (UIntPtr)nuint_t.Parse(s, provider);
        public static UIntPtr Parse(string s, NumberStyles style, IFormatProvider? provider) => (UIntPtr)nuint_t.Parse(s, style, provider);
        public static UIntPtr Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => (UIntPtr)nuint_t.Parse(s, provider);
        public static UIntPtr Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null) => (UIntPtr)nuint_t.Parse(s, style, provider);

        public static bool TryParse([NotNullWhen(true)] string? s, out UIntPtr result)
        {
            Unsafe.SkipInit(out result);
            return nuint_t.TryParse(s, out Unsafe.As<UIntPtr, nuint_t>(ref result));
        }

        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out UIntPtr result)
        {
            Unsafe.SkipInit(out result);
            return nuint_t.TryParse(s, provider, out Unsafe.As<UIntPtr, nuint_t>(ref result));
        }

        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out UIntPtr result)
        {
            Unsafe.SkipInit(out result);
            return nuint_t.TryParse(s, style, provider, out Unsafe.As<UIntPtr, nuint_t>(ref result));
        }

        public static bool TryParse(ReadOnlySpan<char> s, out UIntPtr result)
        {
            Unsafe.SkipInit(out result);
            return nuint_t.TryParse(s, out Unsafe.As<UIntPtr, nuint_t>(ref result));
        }

        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out UIntPtr result)
        {
            Unsafe.SkipInit(out result);
            return nuint_t.TryParse(s, provider, out Unsafe.As<UIntPtr, nuint_t>(ref result));
        }

        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out UIntPtr result)
        {
            Unsafe.SkipInit(out result);
            return nuint_t.TryParse(s, style, provider, out Unsafe.As<UIntPtr, nuint_t>(ref result));
        }

        //
        // IAdditionOperators
        //

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        static nuint IAdditionOperators<nuint, nuint, nuint>.operator +(nuint left, nuint right) => (nuint)(left + right);

        // /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        // static nuint IAdditionOperators<nuint, nuint, nuint>.operator checked +(nuint left, nuint right) => checked((nuint)(left + right));

        //
        // IAdditiveIdentity
        //

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static nuint IAdditiveIdentity<nuint, nuint>.AdditiveIdentity => 0;

        //
        // IBinaryInteger
        //

        /// <inheritdoc cref="IBinaryInteger{TSelf}.DivRem(TSelf, TSelf)" />
        static (nuint Quotient, nuint Remainder) IBinaryInteger<nuint>.DivRem(nuint left, nuint right) => Math.DivRem(left, right);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.LeadingZeroCount(TSelf)" />
        static nuint IBinaryInteger<nuint>.LeadingZeroCount(nuint value)
        {
            if (Environment.Is64BitProcess)
            {
                return (nuint)BitOperations.LeadingZeroCount((ulong)value);
            }
            else
            {
                return (nuint)BitOperations.LeadingZeroCount((uint)value);
            }
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.PopCount(TSelf)" />
        static nuint IBinaryInteger<nuint>.PopCount(nuint value)
        {
            if (Environment.Is64BitProcess)
            {
                return (nuint)BitOperations.PopCount((ulong)value);
            }
            else
            {
                return (nuint)BitOperations.PopCount((uint)value);
            }
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.RotateLeft(TSelf, int)" />
        static nuint IBinaryInteger<nuint>.RotateLeft(nuint value, int rotateAmount)
        {
            if (Environment.Is64BitProcess)
            {
                return (nuint)BitOperations.RotateLeft((ulong)value, rotateAmount);
            }
            else
            {
                return (nuint)BitOperations.RotateLeft((uint)value, rotateAmount);
            }
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.RotateRight(TSelf, int)" />
        static nuint IBinaryInteger<nuint>.RotateRight(nuint value, int rotateAmount)
        {
            if (Environment.Is64BitProcess)
            {
                return (nuint)BitOperations.RotateRight((ulong)value, rotateAmount);
            }
            else
            {
                return (nuint)BitOperations.RotateRight((uint)value, rotateAmount);
            }
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TrailingZeroCount(TSelf)" />
        static nuint IBinaryInteger<nuint>.TrailingZeroCount(nuint value)
        {
            if (Environment.Is64BitProcess)
            {
                return (nuint)BitOperations.TrailingZeroCount((ulong)value);
            }
            else
            {
                return (nuint)BitOperations.TrailingZeroCount((uint)value);
            }
        }

        //
        // IBinaryNumber
        //

        /// <inheritdoc cref="IBinaryNumber{TSelf}.IsPow2(TSelf)" />
        static bool IBinaryNumber<nuint>.IsPow2(nuint value)
        {
            if (Environment.Is64BitProcess)
            {
                return BitOperations.IsPow2((ulong)value);
            }
            else
            {
                return BitOperations.IsPow2((uint)value);
            }
        }

        /// <inheritdoc cref="IBinaryNumber{TSelf}.Log2(TSelf)" />
        static nuint IBinaryNumber<nuint>.Log2(nuint value)
        {
            if (Environment.Is64BitProcess)
            {
                return (nuint)BitOperations.Log2((ulong)value);
            }
            else
            {
                return (nuint)BitOperations.Log2((uint)value);
            }
        }

        //
        // IBitwiseOperators
        //

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseAnd(TSelf, TOther)" />
        static nuint IBitwiseOperators<nuint, nuint, nuint>.operator &(nuint left, nuint right) => left & right;

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseOr(TSelf, TOther)" />
        static nuint IBitwiseOperators<nuint, nuint, nuint>.operator |(nuint left, nuint right) => left | right;

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_ExclusiveOr(TSelf, TOther)" />
        static nuint IBitwiseOperators<nuint, nuint, nuint>.operator ^(nuint left, nuint right) => left ^ right;

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_OnesComplement(TSelf)" />
        static nuint IBitwiseOperators<nuint, nuint, nuint>.operator ~(nuint value) => ~value;

        //
        // IComparisonOperators
        //

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_LessThan(TSelf, TOther)" />
        static bool IComparisonOperators<nuint, nuint>.operator <(nuint left, nuint right) => left < right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_LessThanOrEqual(TSelf, TOther)" />
        static bool IComparisonOperators<nuint, nuint>.operator <=(nuint left, nuint right) => left <= right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_GreaterThan(TSelf, TOther)" />
        static bool IComparisonOperators<nuint, nuint>.operator >(nuint left, nuint right) => left > right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_GreaterThanOrEqual(TSelf, TOther)" />
        static bool IComparisonOperators<nuint, nuint>.operator >=(nuint left, nuint right) => left >= right;

        //
        // IDecrementOperators
        //

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        static nuint IDecrementOperators<nuint>.operator --(nuint value) => --value;

        // /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        // static nuint IDecrementOperators<nuint>.operator checked --(nuint value) => checked(--value);

        //
        // IDivisionOperators
        //

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_Division(TSelf, TOther)" />
        static nuint IDivisionOperators<nuint, nuint, nuint>.operator /(nuint left, nuint right) => left / right;

        // /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_CheckedDivision(TSelf, TOther)" />
        // static nuint IDivisionOperators<nuint, nuint, nuint>.operator checked /(nuint left, nuint right) => checked(left / right);

        //
        // IIncrementOperators
        //

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_Increment(TSelf)" />
        static nuint IIncrementOperators<nuint>.operator ++(nuint value) => ++value;

        // /// <inheritdoc cref="IIncrementOperators{TSelf}.op_CheckedIncrement(TSelf)" />
        // static nuint IIncrementOperators<nuint>.operator checked ++(nuint value) => checked(++value);

        //
        // IMinMaxValue
        //

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MinValue" />
        static nuint IMinMaxValue<nuint>.MinValue => MinValue;

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MaxValue" />
        static nuint IMinMaxValue<nuint>.MaxValue => MaxValue;

        //
        // IModulusOperators
        //

        /// <inheritdoc cref="IModulusOperators{TSelf, TOther, TResult}.op_Modulus(TSelf, TOther)" />
        static nuint IModulusOperators<nuint, nuint, nuint>.operator %(nuint left, nuint right) => left % right;

        //
        // IMultiplicativeIdentity
        //

        /// <inheritdoc cref="IMultiplicativeIdentity{TSelf, TResult}.MultiplicativeIdentity" />
        static nuint IMultiplicativeIdentity<nuint, nuint>.MultiplicativeIdentity => 1;

        //
        // IMultiplyOperators
        //

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_Multiply(TSelf, TOther)" />
        static nuint IMultiplyOperators<nuint, nuint, nuint>.operator *(nuint left, nuint right) => left * right;

        // /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_CheckedMultiply(TSelf, TOther)" />
        // static nuint IMultiplyOperators<nuint, nuint, nuint>.operator checked *(nuint left, nuint right) => checked(left * right);

        //
        // INumber
        //

        /// <inheritdoc cref="INumber{TSelf}.Abs(TSelf)" />
        static nuint INumber<nuint>.Abs(nuint value) => value;

        /// <inheritdoc cref="INumber{TSelf}.Clamp(TSelf, TSelf, TSelf)" />
        static nuint INumber<nuint>.Clamp(nuint value, nuint min, nuint max) => Math.Clamp(value, min, max);

        /// <inheritdoc cref="INumber{TSelf}.CopySign(TSelf, TSelf)" />
        static nuint INumber<nuint>.CopySign(nuint value, nuint sign) => value;

        /// <inheritdoc cref="INumber{TSelf}.CreateChecked{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static nuint INumber<nuint>.CreateChecked<TOther>(TOther value)
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
                return checked((nuint)(decimal)(object)value);
            }
            else if (typeof(TOther) == typeof(double))
            {
                return checked((nuint)(double)(object)value);
            }
            else if (typeof(TOther) == typeof(short))
            {
                return checked((nuint)(short)(object)value);
            }
            else if (typeof(TOther) == typeof(int))
            {
                return checked((nuint)(int)(object)value);
            }
            else if (typeof(TOther) == typeof(long))
            {
                return checked((nuint)(long)(object)value);
            }
            else if (typeof(TOther) == typeof(nint))
            {
                return checked((nuint)(nint)(object)value);
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                return checked((nuint)(sbyte)(object)value);
            }
            else if (typeof(TOther) == typeof(float))
            {
                return checked((nuint)(float)(object)value);
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
                return checked((nuint)(ulong)(object)value);
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
        static nuint INumber<nuint>.CreateSaturating<TOther>(TOther value)
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
                return (actualValue > nuint.MaxValue) ? MaxValue :
                       (actualValue < 0) ? MinValue : (nuint)actualValue;
            }
            else if (typeof(TOther) == typeof(double))
            {
                var actualValue = (double)(object)value;
                return (actualValue > nuint.MaxValue) ? MaxValue :
                       (actualValue < 0) ? MinValue : (nuint)actualValue;
            }
            else if (typeof(TOther) == typeof(short))
            {
                var actualValue = (short)(object)value;
                return (actualValue < 0) ? MinValue : (nuint)actualValue;
            }
            else if (typeof(TOther) == typeof(int))
            {
                var actualValue = (int)(object)value;
                return (actualValue < 0) ? MinValue : (nuint)actualValue;
            }
            else if (typeof(TOther) == typeof(long))
            {
                var actualValue = (long)(object)value;

                return ((Size == 4) && (actualValue > uint.MaxValue)) ? MaxValue :
                       (actualValue < 0) ? MinValue : (nuint)actualValue;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                var actualValue = (nint)(object)value;
                return (actualValue < 0) ? MinValue : (nuint)actualValue;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                var actualValue = (sbyte)(object)value;
                return (actualValue < 0) ? MinValue : (nuint)actualValue;
            }
            else if (typeof(TOther) == typeof(float))
            {
                var actualValue = (float)(object)value;
                return (actualValue > nuint.MaxValue) ? MaxValue :
                       (actualValue < 0) ? MinValue : (nuint)actualValue;
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
                return (actualValue > nuint.MaxValue) ? MaxValue : (nuint)actualValue;
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
        static nuint INumber<nuint>.CreateTruncating<TOther>(TOther value)
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
                return (nuint)(decimal)(object)value;
            }
            else if (typeof(TOther) == typeof(double))
            {
                return (nuint)(double)(object)value;
            }
            else if (typeof(TOther) == typeof(short))
            {
                return (nuint)(short)(object)value;
            }
            else if (typeof(TOther) == typeof(int))
            {
                return (nuint)(int)(object)value;
            }
            else if (typeof(TOther) == typeof(long))
            {
                return (nuint)(long)(object)value;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                return (nuint)(nint)(object)value;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                return (nuint)(sbyte)(object)value;
            }
            else if (typeof(TOther) == typeof(float))
            {
                return (nuint)(float)(object)value;
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
                return (nuint)(ulong)(object)value;
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
        static bool INumber<nuint>.IsNegative(nuint value) => false;

        /// <inheritdoc cref="INumber{TSelf}.Max(TSelf, TSelf)" />
        static nuint INumber<nuint>.Max(nuint x, nuint y) => Math.Max(x, y);

        /// <inheritdoc cref="INumber{TSelf}.MaxMagnitude(TSelf, TSelf)" />
        static nuint INumber<nuint>.MaxMagnitude(nuint x, nuint y) => Math.Max(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Min(TSelf, TSelf)" />
        static nuint INumber<nuint>.Min(nuint x, nuint y) => Math.Min(x, y);

        /// <inheritdoc cref="INumber{TSelf}.MinMagnitude(TSelf, TSelf)" />
        static nuint INumber<nuint>.MinMagnitude(nuint x, nuint y) => Math.Min(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Sign(TSelf)" />
        static int INumber<nuint>.Sign(nuint value) => (value == 0) ? 0 : 1;

        /// <inheritdoc cref="INumber{TSelf}.TryCreate{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumber<nuint>.TryCreate<TOther>(TOther value, out nuint result)
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

                if ((actualValue < 0) || (actualValue > nuint.MaxValue))
                {
                    result = default;
                    return false;
                }

                result = (nuint)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                var actualValue = (double)(object)value;

                if ((actualValue < 0) || (actualValue > nuint.MaxValue))
                {
                    result = default;
                    return false;
                }

                result = (nuint)actualValue;
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

                result = (nuint)actualValue;
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

                result = (nuint)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                var actualValue = (long)(object)value;

                if ((actualValue < 0) || ((Size == 4) && (actualValue > uint.MaxValue)))
                {
                    result = default;
                    return false;
                }

                result = (nuint)actualValue;
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

                result = (nuint)actualValue;
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

                result = (nuint)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                var actualValue = (float)(object)value;

                if ((actualValue < 0) || (actualValue > nuint.MaxValue))
                {
                    result = default;
                    return false;
                }

                result = (nuint)actualValue;
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

                if (actualValue > nuint.MaxValue)
                {
                    result = default;
                    return false;
                }

                result = (nuint)actualValue;
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
        static nuint INumberBase<nuint>.One => 1;

        /// <inheritdoc cref="INumberBase{TSelf}.Zero" />
        static nuint INumberBase<nuint>.Zero => 0;

        //
        // IShiftOperators
        //

        /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_LeftShift(TSelf, int)" />
        static nuint IShiftOperators<nuint, nuint>.operator <<(nuint value, int shiftAmount) => value << (int)shiftAmount;

        /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_RightShift(TSelf, int)" />
        static nuint IShiftOperators<nuint, nuint>.operator >>(nuint value, int shiftAmount) => value >> (int)shiftAmount;

        // /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_UnsignedRightShift(TSelf, int)" />
        // static nuint IShiftOperators<nuint, nuint>.operator >>>(nuint value, int shiftAmount) => value >> (int)shiftAmount;

        //
        // ISubtractionOperators
        //

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_Subtraction(TSelf, TOther)" />
        static nuint ISubtractionOperators<nuint, nuint, nuint>.operator -(nuint left, nuint right) => left - right;

        // /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_CheckedSubtraction(TSelf, TOther)" />
        // static nuint ISubtractionOperators<nuint, nuint, nuint>.operator checked -(nuint left, nuint right) => checked(left - right);

        //
        // IUnaryNegationOperators
        //

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_UnaryNegation(TSelf)" />
        static nuint IUnaryNegationOperators<nuint, nuint>.operator -(nuint value) => (nuint)0 - value;

        // /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_CheckedUnaryNegation(TSelf)" />
        // static nuint IUnaryNegationOperators<nuint, nuint>.operator checked -(nuint value) => checked((nuint)0 - value);

        //
        // IUnaryPlusOperators
        //

        /// <inheritdoc cref="IUnaryPlusOperators{TSelf, TResult}.op_UnaryPlus(TSelf)" />
        static nuint IUnaryPlusOperators<nuint, nuint>.operator +(nuint value) => +value;
    }
}
