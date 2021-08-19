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
    public readonly struct UInt16 : IComparable, IConvertible, ISpanFormattable, IComparable<ushort>, IEquatable<ushort>
#if FEATURE_GENERIC_MATH
#pragma warning disable SA1001
        , IBinaryInteger<ushort>,
          IMinMaxValue<ushort>,
          IUnsignedNumber<ushort>
#pragma warning restore SA1001
#endif // FEATURE_GENERIC_MATH
    {
        private readonly ushort m_value; // Do not rename (binary serialization)

        public const ushort MaxValue = (ushort)0xFFFF;
        public const ushort MinValue = 0;

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

        public string ToString(string? format)
        {
            return Number.FormatUInt32(m_value, format, null);
        }

        public string ToString(string? format, IFormatProvider? provider)
        {
            return Number.FormatUInt32(m_value, format, provider);
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatUInt32(m_value, format, provider, destination, out charsWritten);
        }

        public static ushort Parse(string s)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Parse((ReadOnlySpan<char>)s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);
        }

        public static ushort Parse(string s, NumberStyles style)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Parse((ReadOnlySpan<char>)s, style, NumberFormatInfo.CurrentInfo);
        }

        public static ushort Parse(string s, IFormatProvider? provider)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Parse((ReadOnlySpan<char>)s, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
        }

        public static ushort Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Parse((ReadOnlySpan<char>)s, style, NumberFormatInfo.GetInstance(provider));
        }

        public static ushort Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Parse(s, style, NumberFormatInfo.GetInstance(provider));
        }

        private static ushort Parse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info)
        {
            Number.ParsingStatus status = Number.TryParseUInt32(s, style, info, out uint i);
            if (status != Number.ParsingStatus.OK)
            {
                Number.ThrowOverflowOrFormatException(status, TypeCode.UInt16);
            }

            if (i > MaxValue) Number.ThrowOverflowException(TypeCode.UInt16);
            return (ushort)i;
        }

        public static bool TryParse([NotNullWhen(true)] string? s, out ushort result)
        {
            if (s == null)
            {
                result = 0;
                return false;
            }

            return TryParse((ReadOnlySpan<char>)s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse(ReadOnlySpan<char> s, out ushort result)
        {
            return TryParse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out ushort result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);

            if (s == null)
            {
                result = 0;
                return false;
            }

            return TryParse((ReadOnlySpan<char>)s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out ushort result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return TryParse(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        private static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info, out ushort result)
        {
            if (Number.TryParseUInt32(s, style, info, out uint i) != Number.ParsingStatus.OK
                || i > MaxValue)
            {
                result = 0;
                return false;
            }
            result = (ushort)i;
            return true;
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

#if FEATURE_GENERIC_MATH
        //
        // IAdditionOperators
        //

        [RequiresPreviewFeatures]
        static ushort IAdditionOperators<ushort, ushort, ushort>.operator +(ushort left, ushort right)
            => (ushort)(left + right);

        // [RequiresPreviewFeatures]
        // static checked ushort IAdditionOperators<ushort, ushort, ushort>.operator +(ushort left, ushort right)
        //     => checked((ushort)(left + right));

        //
        // IAdditiveIdentity
        //

        [RequiresPreviewFeatures]
        static ushort IAdditiveIdentity<ushort, ushort>.AdditiveIdentity => 0;

        //
        // IBinaryInteger
        //

        [RequiresPreviewFeatures]
        static ushort IBinaryInteger<ushort>.LeadingZeroCount(ushort value)
            => (ushort)(BitOperations.LeadingZeroCount(value) - 16);

        [RequiresPreviewFeatures]
        static ushort IBinaryInteger<ushort>.PopCount(ushort value)
            => (ushort)BitOperations.PopCount(value);

        [RequiresPreviewFeatures]
        static ushort IBinaryInteger<ushort>.RotateLeft(ushort value, int rotateAmount)
            => (ushort)((value << (rotateAmount & 15)) | (value >> ((16 - rotateAmount) & 15)));

        [RequiresPreviewFeatures]
        static ushort IBinaryInteger<ushort>.RotateRight(ushort value, int rotateAmount)
            => (ushort)((value >> (rotateAmount & 15)) | (value << ((16 - rotateAmount) & 15)));

        [RequiresPreviewFeatures]
        static ushort IBinaryInteger<ushort>.TrailingZeroCount(ushort value)
            => (ushort)(BitOperations.TrailingZeroCount(value << 16) - 16);

        //
        // IBinaryNumber
        //

        [RequiresPreviewFeatures]
        static bool IBinaryNumber<ushort>.IsPow2(ushort value)
            => BitOperations.IsPow2((uint)value);

        [RequiresPreviewFeatures]
        static ushort IBinaryNumber<ushort>.Log2(ushort value)
            => (ushort)BitOperations.Log2(value);

        //
        // IBitwiseOperators
        //

        [RequiresPreviewFeatures]
        static ushort IBitwiseOperators<ushort, ushort, ushort>.operator &(ushort left, ushort right)
            => (ushort)(left & right);

        [RequiresPreviewFeatures]
        static ushort IBitwiseOperators<ushort, ushort, ushort>.operator |(ushort left, ushort right)
            => (ushort)(left | right);

        [RequiresPreviewFeatures]
        static ushort IBitwiseOperators<ushort, ushort, ushort>.operator ^(ushort left, ushort right)
            => (ushort)(left ^ right);

        [RequiresPreviewFeatures]
        static ushort IBitwiseOperators<ushort, ushort, ushort>.operator ~(ushort value)
            => (ushort)(~value);

        //
        // IComparisonOperators
        //

        [RequiresPreviewFeatures]
        static bool IComparisonOperators<ushort, ushort>.operator <(ushort left, ushort right)
            => left < right;

        [RequiresPreviewFeatures]
        static bool IComparisonOperators<ushort, ushort>.operator <=(ushort left, ushort right)
            => left <= right;

        [RequiresPreviewFeatures]
        static bool IComparisonOperators<ushort, ushort>.operator >(ushort left, ushort right)
            => left > right;

        [RequiresPreviewFeatures]
        static bool IComparisonOperators<ushort, ushort>.operator >=(ushort left, ushort right)
            => left >= right;

        //
        // IDecrementOperators
        //

        [RequiresPreviewFeatures]
        static ushort IDecrementOperators<ushort>.operator --(ushort value)
            => --value;

        // [RequiresPreviewFeatures]
        // static checked ushort IDecrementOperators<ushort>.operator --(ushort value)
        //     => checked(--value);

        //
        // IDivisionOperators
        //

        [RequiresPreviewFeatures]
        static ushort IDivisionOperators<ushort, ushort, ushort>.operator /(ushort left, ushort right)
            => (ushort)(left / right);

        // [RequiresPreviewFeatures]
        // static checked ushort IDivisionOperators<ushort, ushort, ushort>.operator /(ushort left, ushort right)
        //     => checked((ushort)(left / right));

        //
        // IEqualityOperators
        //

        [RequiresPreviewFeatures]
        static bool IEqualityOperators<ushort, ushort>.operator ==(ushort left, ushort right)
            => left == right;

        [RequiresPreviewFeatures]
        static bool IEqualityOperators<ushort, ushort>.operator !=(ushort left, ushort right)
            => left != right;

        //
        // IIncrementOperators
        //

        [RequiresPreviewFeatures]
        static ushort IIncrementOperators<ushort>.operator ++(ushort value)
            => ++value;

        // [RequiresPreviewFeatures]
        // static checked ushort IIncrementOperators<ushort>.operator ++(ushort value)
        //     => checked(++value);

        //
        // IMinMaxValue
        //

        [RequiresPreviewFeatures]
        static ushort IMinMaxValue<ushort>.MinValue => MinValue;

        [RequiresPreviewFeatures]
        static ushort IMinMaxValue<ushort>.MaxValue => MaxValue;

        //
        // IModulusOperators
        //

        [RequiresPreviewFeatures]
        static ushort IModulusOperators<ushort, ushort, ushort>.operator %(ushort left, ushort right)
            => (ushort)(left % right);

        // [RequiresPreviewFeatures]
        // static checked ushort IModulusOperators<ushort, ushort, ushort>.operator %(ushort left, ushort right)
        //     => checked((ushort)(left % right));

        //
        // IMultiplicativeIdentity
        //

        [RequiresPreviewFeatures]
        static ushort IMultiplicativeIdentity<ushort, ushort>.MultiplicativeIdentity => 1;

        //
        // IMultiplyOperators
        //

        [RequiresPreviewFeatures]
        static ushort IMultiplyOperators<ushort, ushort, ushort>.operator *(ushort left, ushort right)
            => (ushort)(left * right);

        // [RequiresPreviewFeatures]
        // static checked ushort IMultiplyOperators<ushort, ushort, ushort>.operator *(ushort left, ushort right)
        //     => checked((ushort)(left * right));

        //
        // INumber
        //

        [RequiresPreviewFeatures]
        static ushort INumber<ushort>.One => 1;

        [RequiresPreviewFeatures]
        static ushort INumber<ushort>.Zero => 0;

        [RequiresPreviewFeatures]
        static ushort INumber<ushort>.Abs(ushort value)
            => value;

        [RequiresPreviewFeatures]
        static ushort INumber<ushort>.Clamp(ushort value, ushort min, ushort max)
            => Math.Clamp(value, min, max);

        [RequiresPreviewFeatures]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ushort INumber<ushort>.Create<TOther>(TOther value)
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
                return checked((ushort)(decimal)(object)value);
            }
            else if (typeof(TOther) == typeof(double))
            {
                return checked((ushort)(double)(object)value);
            }
            else if (typeof(TOther) == typeof(short))
            {
                return checked((ushort)(short)(object)value);
            }
            else if (typeof(TOther) == typeof(int))
            {
                return checked((ushort)(int)(object)value);
            }
            else if (typeof(TOther) == typeof(long))
            {
                return checked((ushort)(long)(object)value);
            }
            else if (typeof(TOther) == typeof(nint))
            {
                return checked((ushort)(nint)(object)value);
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                return checked((ushort)(sbyte)(object)value);
            }
            else if (typeof(TOther) == typeof(float))
            {
                return checked((ushort)(float)(object)value);
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                return (ushort)(object)value;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                return checked((ushort)(uint)(object)value);
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                return checked((ushort)(ulong)(object)value);
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                return checked((ushort)(nuint)(object)value);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        [RequiresPreviewFeatures]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ushort INumber<ushort>.CreateSaturating<TOther>(TOther value)
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
                       (actualValue < 0) ? MinValue : (ushort)actualValue;
            }
            else if (typeof(TOther) == typeof(double))
            {
                var actualValue = (double)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < 0) ? MinValue : (ushort)actualValue;
            }
            else if (typeof(TOther) == typeof(short))
            {
                var actualValue = (short)(object)value;
                return (actualValue < 0) ? MinValue : (ushort)actualValue;
            }
            else if (typeof(TOther) == typeof(int))
            {
                var actualValue = (int)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < 0) ? MinValue : (ushort)actualValue;
            }
            else if (typeof(TOther) == typeof(long))
            {
                var actualValue = (long)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < 0) ? MinValue : (ushort)actualValue;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                var actualValue = (nint)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < 0) ? MinValue : (ushort)actualValue;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                var actualValue = (sbyte)(object)value;
                return (actualValue < 0) ? MinValue : (ushort)actualValue;
            }
            else if (typeof(TOther) == typeof(float))
            {
                var actualValue = (float)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < 0) ? MinValue : (ushort)actualValue;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                return (ushort)(object)value;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                var actualValue = (uint)(object)value;
                return (actualValue > MaxValue) ? MaxValue : (ushort)actualValue;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                var actualValue = (ulong)(object)value;
                return (actualValue > MaxValue) ? MaxValue : (ushort)actualValue;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                var actualValue = (nuint)(object)value;
                return (actualValue > MaxValue) ? MaxValue : (ushort)actualValue;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        [RequiresPreviewFeatures]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ushort INumber<ushort>.CreateTruncating<TOther>(TOther value)
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
                return (ushort)(decimal)(object)value;
            }
            else if (typeof(TOther) == typeof(double))
            {
                return (ushort)(double)(object)value;
            }
            else if (typeof(TOther) == typeof(short))
            {
                return (ushort)(short)(object)value;
            }
            else if (typeof(TOther) == typeof(int))
            {
                return (ushort)(int)(object)value;
            }
            else if (typeof(TOther) == typeof(long))
            {
                return (ushort)(long)(object)value;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                return (ushort)(nint)(object)value;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                return (ushort)(sbyte)(object)value;
            }
            else if (typeof(TOther) == typeof(float))
            {
                return (ushort)(float)(object)value;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                return (ushort)(object)value;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                return (ushort)(uint)(object)value;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                return (ushort)(ulong)(object)value;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                return (ushort)(nuint)(object)value;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        [RequiresPreviewFeatures]
        static (ushort Quotient, ushort Remainder) INumber<ushort>.DivRem(ushort left, ushort right)
            => Math.DivRem(left, right);

        [RequiresPreviewFeatures]
        static ushort INumber<ushort>.Max(ushort x, ushort y)
            => Math.Max(x, y);

        [RequiresPreviewFeatures]
        static ushort INumber<ushort>.Min(ushort x, ushort y)
            => Math.Min(x, y);

        [RequiresPreviewFeatures]
        static ushort INumber<ushort>.Parse(string s, NumberStyles style, IFormatProvider? provider)
            => Parse(s, style, provider);

        [RequiresPreviewFeatures]
        static ushort INumber<ushort>.Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider)
            => Parse(s, style, provider);

        [RequiresPreviewFeatures]
        static ushort INumber<ushort>.Sign(ushort value)
            => (ushort)((value == 0) ? 0 : 1);

        [RequiresPreviewFeatures]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumber<ushort>.TryCreate<TOther>(TOther value, out ushort result)
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

                result = (ushort)actualValue;
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

                result = (ushort)actualValue;
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

                result = (ushort)actualValue;
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

                result = (ushort)actualValue;
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

                result = (ushort)actualValue;
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

                result = (ushort)actualValue;
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

                result = (ushort)actualValue;
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

                result = (ushort)actualValue;
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

                if (actualValue > MaxValue)
                {
                    result = default;
                    return false;
                }

                result = (ushort)actualValue;
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

                result = (ushort)actualValue;
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

                result = (ushort)actualValue;
                return true;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                result = default;
                return false;
            }
        }

        [RequiresPreviewFeatures]
        static bool INumber<ushort>.TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out ushort result)
            => TryParse(s, style, provider, out result);

        [RequiresPreviewFeatures]
        static bool INumber<ushort>.TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out ushort result)
            => TryParse(s, style, provider, out result);

        //
        // IParseable
        //

        [RequiresPreviewFeatures]
        static ushort IParseable<ushort>.Parse(string s, IFormatProvider? provider)
            => Parse(s, provider);

        [RequiresPreviewFeatures]
        static bool IParseable<ushort>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out ushort result)
            => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // IShiftOperators
        //

        [RequiresPreviewFeatures]
        static ushort IShiftOperators<ushort, ushort>.operator <<(ushort value, int shiftAmount)
            => (ushort)(value << shiftAmount);

        [RequiresPreviewFeatures]
        static ushort IShiftOperators<ushort, ushort>.operator >>(ushort value, int shiftAmount)
            => (ushort)(value >> shiftAmount);

        // [RequiresPreviewFeatures]
        // static ushort IShiftOperators<ushort, ushort>.operator >>>(ushort value, int shiftAmount)
        //     => (ushort)(value >> shiftAmount);

        //
        // ISpanParseable
        //

        [RequiresPreviewFeatures]
        static ushort ISpanParseable<ushort>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
            => Parse(s, NumberStyles.Integer, provider);

        [RequiresPreviewFeatures]
        static bool ISpanParseable<ushort>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out ushort result)
            => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // ISubtractionOperators
        //

        [RequiresPreviewFeatures]
        static ushort ISubtractionOperators<ushort, ushort, ushort>.operator -(ushort left, ushort right)
            => (ushort)(left - right);

        // [RequiresPreviewFeatures]
        // static checked ushort ISubtractionOperators<ushort, ushort, ushort>.operator -(ushort left, ushort right)
        //     => checked((ushort)(left - right));

        //
        // IUnaryNegationOperators
        //

        [RequiresPreviewFeatures]
        static ushort IUnaryNegationOperators<ushort, ushort>.operator -(ushort value)
            => (ushort)(-value);

        // [RequiresPreviewFeatures]
        // static checked ushort IUnaryNegationOperators<ushort, ushort>.operator -(ushort value)
        //     => checked((ushort)(-value));

        //
        // IUnaryPlusOperators
        //

        [RequiresPreviewFeatures]
        static ushort IUnaryPlusOperators<ushort, ushort>.operator +(ushort value)
            => (ushort)(+value);

        // [RequiresPreviewFeatures]
        // static checked ushort IUnaryPlusOperators<ushort, ushort>.operator +(ushort value)
        //     => checked((ushort)(+value));
#endif // FEATURE_GENERIC_MATH
    }
}
