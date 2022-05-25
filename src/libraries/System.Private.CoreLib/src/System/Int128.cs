// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    /// <summary>Represents a 128-bit signed integer.</summary>
    [Intrinsic]
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Int128
        : IBinaryInteger<Int128>,
          IMinMaxValue<Int128>,
          ISignedNumber<Int128>
    {
        internal const int Size = 16;

        // Unix System V ABI actually requires this to be little endian
        // order and not `upper, lower` on big endian systems.

        private readonly ulong _lower;
        private readonly ulong _upper;

        /// <summary>Initializes a new instance of the <see cref="Int128" /> struct.</summary>
        /// <param name="upper">The upper 64-bits of the 128-bit value.</param>
        /// <param name="lower">The lower 64-bits of the 128-bit value.</param>
        [CLSCompliant(false)]
        public Int128(ulong upper, ulong lower)
        {
            _lower = lower;
            _upper = upper;
        }

        internal ulong Lower => _lower;

        internal ulong Upper => _upper;

        /// <inheritdoc cref="IComparable.CompareTo(object)" />
        public int CompareTo(object? value)
        {
            if (value is Int128 other)
            {
                return CompareTo(other);
            }
            else if (value is null)
            {
                return 1;
            }
            else
            {
                throw new ArgumentException(SR.Arg_MustBeInt128);
            }
        }

        /// <inheritdoc cref="IComparable{T}.CompareTo(T)" />
        public int CompareTo(Int128 value)
        {
            if (this < value)
            {
                return -1;
            }
            else if (this > value)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        /// <inheritdoc cref="object.Equals(object?)" />
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return (obj is Int128 other) && Equals(other);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(Int128 other)
        {
            return this == other;
        }

        /// <inheritdoc cref="object.GetHashCode()" />
        public override int GetHashCode() => HashCode.Combine(_lower, _upper);

        /// <inheritdoc cref="object.ToString()" />
        public override string ToString()
        {
            return Number.Int128ToDecStr(this);
        }

        public string ToString(IFormatProvider? provider)
        {
            return Number.FormatInt128(this, null, provider);
        }

        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
        {
            return Number.FormatInt128(this, format, null);
        }

        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider)
        {
            return Number.FormatInt128(this, format, provider);
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatInt128(this, format, provider, destination, out charsWritten);
        }

        public static Int128 Parse(string s)
        {
            ArgumentNullException.ThrowIfNull(s);
            return Number.ParseInt128(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);
        }

        public static Int128 Parse(string s, NumberStyles style)
        {
            ArgumentNullException.ThrowIfNull(s);
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.ParseInt128(s, style, NumberFormatInfo.CurrentInfo);
        }

        public static Int128 Parse(string s, IFormatProvider? provider)
        {
            ArgumentNullException.ThrowIfNull(s);
            return Number.ParseInt128(s, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
        }

        public static Int128 Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            ArgumentNullException.ThrowIfNull(s);
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.ParseInt128(s, style, NumberFormatInfo.GetInstance(provider));
        }

        public static Int128 Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.ParseInt128(s, style, NumberFormatInfo.GetInstance(provider));
        }

        public static bool TryParse([NotNullWhen(true)] string? s, out Int128 result)
        {
            if (s is not null)
            {
                return Number.TryParseInt128IntegerStyle(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result) == Number.ParsingStatus.OK;
            }
            else
            {
                result = default;
                return false;
            }
        }

        public static bool TryParse(ReadOnlySpan<char> s, out Int128 result)
        {
            return Number.TryParseInt128IntegerStyle(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result) == Number.ParsingStatus.OK;
        }

        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out Int128 result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);

            if (s is not null)
            {
                return Number.TryParseInt128(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
            }
            else
            {
                result = default;
                return false;
            }
        }

        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out Int128 result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.TryParseInt128(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
        }

        //
        // Explicit Conversions From Int128
        //

        /// <summary>Explicitly converts a 128-bit signed integer to a <see cref="byte" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="byte" />.</returns>
        public static explicit operator byte(Int128 value) => (byte)value._lower;

        /// <summary>Explicitly converts a 128-bit signed integer to a <see cref="byte" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="byte" />.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="Int128" />.</exception>
        public static explicit operator checked byte(Int128 value)
        {
            if (value._upper != 0)
            {
                ThrowHelper.ThrowOverflowException();
            }
            return checked((byte)value._lower);
        }

        /// <summary>Explicitly converts a 128-bit signed integer to a <see cref="char" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="char" />.</returns>
        public static explicit operator char(Int128 value) => (char)value._lower;

        /// <summary>Explicitly converts a 128-bit signed integer to a <see cref="char" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="char" />.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="Int128" />.</exception>
        public static explicit operator checked char(Int128 value)
        {
            if (value._upper != 0)
            {
                ThrowHelper.ThrowOverflowException();
            }
            return checked((char)value._lower);
        }

        /// <summary>Explicitly converts a 128-bit signed integer to a <see cref="decimal" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="decimal" />.</returns>
        public static explicit operator decimal(Int128 value)
        {
            if (IsNegative(value))
            {
                value = -value;
                return -(decimal)(UInt128)(value);
            }
            return (decimal)(UInt128)(value);
        }

        /// <summary>Explicitly converts a 128-bit signed integer to a <see cref="double" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="double" />.</returns>
        public static explicit operator double(Int128 value)
        {
            if (IsNegative(value))
            {
                value = -value;
                return -(double)(UInt128)(value);
            }
            return (double)(UInt128)(value);
        }

        /// <summary>Explicitly converts a 128-bit signed integer to a <see cref="Half" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="Half" />.</returns>
        public static explicit operator Half(Int128 value)
        {
            if (IsNegative(value))
            {
                value = -value;
                return -(Half)(UInt128)(value);
            }
            return (Half)(UInt128)(value);
        }

        /// <summary>Explicitly converts a 128-bit signed integer to a <see cref="short" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="short" />.</returns>
        public static explicit operator short(Int128 value) => (short)value._lower;

        /// <summary>Explicitly converts a 128-bit signed integer to a <see cref="short" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="short" />.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="Int128" />.</exception>
        public static explicit operator checked short(Int128 value)
        {
            if (~value._upper == 0)
            {
                long lower = (long)value._lower;
                return checked((short)lower);
            }

            if (value._upper != 0)
            {
                ThrowHelper.ThrowOverflowException();
            }
            return checked((short)value._lower);
        }

        /// <summary>Explicitly converts a 128-bit signed integer to a <see cref="int" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="int" />.</returns>
        public static explicit operator int(Int128 value) => (int)value._lower;

        /// <summary>Explicitly converts a 128-bit signed integer to a <see cref="int" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="int" />.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="Int128" />.</exception>
        public static explicit operator checked int(Int128 value)
        {
            if (~value._upper == 0)
            {
                long lower = (long)value._lower;
                return checked((int)lower);
            }

            if (value._upper != 0)
            {
                ThrowHelper.ThrowOverflowException();
            }
            return checked((int)value._lower);
        }

        /// <summary>Explicitly converts a 128-bit signed integer to a <see cref="long" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="long" />.</returns>
        public static explicit operator long(Int128 value) => (long)value._lower;

        /// <summary>Explicitly converts a 128-bit signed integer to a <see cref="long" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="long" />.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="Int128" />.</exception>
        public static explicit operator checked long(Int128 value)
        {
            if (~value._upper == 0)
            {
                long lower = (long)value._lower;
                return lower;
            }

            if (value._upper != 0)
            {
                ThrowHelper.ThrowOverflowException();
            }
            return checked((long)value._lower);
        }

        /// <summary>Explicitly converts a 128-bit signed integer to a <see cref="IntPtr" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="IntPtr" />.</returns>
        public static explicit operator nint(Int128 value) => (nint)value._lower;

        /// <summary>Explicitly converts a 128-bit signed integer to a <see cref="IntPtr" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="IntPtr" />.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="Int128" />.</exception>
        public static explicit operator checked nint(Int128 value)
        {
            if (~value._upper == 0)
            {
                long lower = (long)value._lower;
                return checked((nint)lower);
            }

            if (value._upper != 0)
            {
                ThrowHelper.ThrowOverflowException();
            }
            return checked((nint)value._lower);
        }

        /// <summary>Explicitly converts a 128-bit signed integer to a <see cref="NFloat" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="NFloat" />.</returns>
        public static explicit operator NFloat(Int128 value)
        {
            if (IsNegative(value))
            {
                value = -value;
                return -(NFloat)(UInt128)(value);
            }
            return (NFloat)(UInt128)(value);
        }

        /// <summary>Explicitly converts a 128-bit signed integer to a <see cref="sbyte" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="sbyte" />.</returns>
        [CLSCompliant(false)]
        public static explicit operator sbyte(Int128 value) => (sbyte)value._lower;

        /// <summary>Explicitly converts a 128-bit signed integer to a <see cref="sbyte" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="sbyte" />.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="Int128" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked sbyte(Int128 value)
        {
            if (~value._upper == 0)
            {
                long lower = (long)value._lower;
                return checked((sbyte)lower);
            }

            if (value._upper != 0)
            {
                ThrowHelper.ThrowOverflowException();
            }
            return checked((sbyte)value._lower);
        }

        /// <summary>Explicitly converts a 128-bit signed integer to a <see cref="float" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="float" />.</returns>
        public static explicit operator float(Int128 value)
        {
            if (IsNegative(value))
            {
                value = -value;
                return -(float)(UInt128)(value);
            }
            return (float)(UInt128)(value);
        }

        /// <summary>Explicitly converts a 128-bit signed integer to a <see cref="ushort" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="ushort" />.</returns>
        [CLSCompliant(false)]
        public static explicit operator ushort(Int128 value) => (ushort)value._lower;

        /// <summary>Explicitly converts a 128-bit signed integer to a <see cref="ushort" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="ushort" />.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="Int128" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked ushort(Int128 value)
        {
            if (value._upper != 0)
            {
                ThrowHelper.ThrowOverflowException();
            }
            return checked((ushort)value._lower);
        }

        /// <summary>Explicitly converts a 128-bit signed integer to a <see cref="uint" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="uint" />.</returns>
        [CLSCompliant(false)]
        public static explicit operator uint(Int128 value) => (uint)value._lower;

        /// <summary>Explicitly converts a 128-bit signed integer to a <see cref="uint" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="uint" />.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="Int128" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked uint(Int128 value)
        {
            if (value._upper != 0)
            {
                ThrowHelper.ThrowOverflowException();
            }
            return checked((uint)value._lower);
        }

        /// <summary>Explicitly converts a 128-bit signed integer to a <see cref="ulong" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="ulong" />.</returns>
        [CLSCompliant(false)]
        public static explicit operator ulong(Int128 value) => value._lower;

        /// <summary>Explicitly converts a 128-bit signed integer to a <see cref="ulong" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="ulong" />.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="Int128" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked ulong(Int128 value)
        {
            if (value._upper != 0)
            {
                ThrowHelper.ThrowOverflowException();
            }
            return value._lower;
        }

        /// <summary>Explicitly converts a 128-bit signed integer to a <see cref="UInt128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="UInt128" />.</returns>
        [CLSCompliant(false)]
        public static explicit operator UInt128(Int128 value) => new UInt128(value._upper, value._lower);

        /// <summary>Explicitly converts a 128-bit signed integer to a <see cref="UInt128" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="UInt128" />.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="Int128" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked UInt128(Int128 value)
        {
            if ((long)value._upper < 0)
            {
                ThrowHelper.ThrowOverflowException();
            }
            return new UInt128(value._upper, value._lower);
        }

        /// <summary>Explicitly converts a 128-bit signed integer to a <see cref="UIntPtr" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="UIntPtr" />.</returns>
        [CLSCompliant(false)]
        public static explicit operator nuint(Int128 value) => (nuint)value._lower;

        /// <summary>Explicitly converts a 128-bit signed integer to a <see cref="UIntPtr" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="UIntPtr" />.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="Int128" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked nuint(Int128 value)
        {
            if (value._upper != 0)
            {
                ThrowHelper.ThrowOverflowException();
            }
            return checked((nuint)value._lower);
        }

        //
        // Explicit Conversions To Int128
        //

        /// <summary>Explicitly converts a <see cref="decimal" /> value to a 128-bit signed integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit signed integer.</returns>
        public static explicit operator Int128(decimal value)
        {
            value = decimal.Truncate(value);
            Int128 result = new Int128(value.High, value.Low64);

            if (decimal.IsNegative(value))
            {
                result = -result;
            }
            return result;
        }

        /// <summary>Explicitly converts a <see cref="double" /> value to a 128-bit signed integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit signed integer.</returns>
        public static explicit operator Int128(double value)
        {
            const double TwoPow127 = 170141183460469231731687303715884105728.0;

            if (value <= -TwoPow127)
            {
                return MinValue;
            }
            else if (double.IsNaN(value))
            {
                return 0;
            }
            else if (value >= +TwoPow127)
            {
                return MaxValue;
            }

            return ToInt128(value);
        }

        /// <summary>Explicitly converts a <see cref="double" /> value to a 128-bit signed integer, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit signed integer.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="Int128" />.</exception>
        public static explicit operator checked Int128(double value)
        {
            const double TwoPow127 = 170141183460469231731687303715884105728.0;

            if ((value < -TwoPow127) || double.IsNaN(value) || (value >= +TwoPow127))
            {
                ThrowHelper.ThrowOverflowException();
            }

            return ToInt128(value);
        }

        internal static Int128 ToInt128(double value)
        {
            const double TwoPow127 = 170141183460469231731687303715884105728.0;

            Debug.Assert(value >= -TwoPow127);
            Debug.Assert(double.IsFinite(value));
            Debug.Assert(value < TwoPow127);

            // This code is based on `f64_to_i128` from m-ou-se/floatconv
            // Copyright (c) 2020 Mara Bos <m-ou.se@m-ou.se>. All rights reserved.
            //
            // Licensed under the BSD 2 - Clause "Simplified" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            bool isNegative = double.IsNegative(value);

            if (isNegative)
            {
                value = -value;
            }

            if (value >= 1.0)
            {
                // In order to convert from double to int128 we first need to extract the signficand,
                // including the implicit leading bit, as a full 128-bit significand. We can then adjust
                // this down to the represented integer by right shifting by the unbiased exponent, taking
                // into account the significand is now represented as 128-bits.

                ulong bits = BitConverter.DoubleToUInt64Bits(value);
                Int128 result = new Int128((bits << 12) >> 1 | 0x8000_0000_0000_0000, 0x0000_0000_0000_0000);

                result >>>= (1023 + 128 - 1 - (int)(bits >> 52));

                if (isNegative)
                {
                    result = -result;
                }
                return result;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>Explicitly converts a <see cref="float" /> value to a 128-bit signed integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit signed integer.</returns>
        public static explicit operator Int128(float value) => (Int128)(double)(value);

        /// <summary>Explicitly converts a <see cref="float" /> value to a 128-bit signed integer, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit signed integer.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="Int128" />.</exception>
        public static explicit operator checked Int128(float value) => checked((Int128)(double)(value));

        //
        // Implicit Conversions To Int128
        //

        /// <summary>Implicitly converts a <see cref="byte" /> value to a 128-bit signed integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit signed integer.</returns>
        public static implicit operator Int128(byte value) => new Int128(0, value);

        /// <summary>Implicitly converts a <see cref="char" /> value to a 128-bit signed integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit signed integer.</returns>
        public static implicit operator Int128(char value) => new Int128(0, value);

        /// <summary>Implicitly converts a <see cref="short" /> value to a 128-bit signed integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit signed integer.</returns>
        public static implicit operator Int128(short value)
        {
            long lower = value;
            return new Int128((ulong)(lower >> 63), (ulong)lower);
        }

        /// <summary>Implicitly converts a <see cref="int" /> value to a 128-bit signed integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit signed integer.</returns>
        public static implicit operator Int128(int value)
        {
            long lower = value;
            return new Int128((ulong)(lower >> 63), (ulong)lower);
        }

        /// <summary>Implicitly converts a <see cref="long" /> value to a 128-bit signed integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit signed integer.</returns>
        public static implicit operator Int128(long value)
        {
            long lower = value;
            return new Int128((ulong)(lower >> 63), (ulong)lower);
        }

        /// <summary>Implicitly converts a <see cref="IntPtr" /> value to a 128-bit signed integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit signed integer.</returns>
        public static implicit operator Int128(nint value)
        {
            long lower = value;
            return new Int128((ulong)(lower >> 63), (ulong)lower);
        }

        /// <summary>Implicitly converts a <see cref="sbyte" /> value to a 128-bit signed integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit signed integer.</returns>
        [CLSCompliant(false)]
        public static implicit operator Int128(sbyte value)
        {
            long lower = value;
            return new Int128((ulong)(lower >> 63), (ulong)lower);
        }

        /// <summary>Implicitly converts a <see cref="ushort" /> value to a 128-bit signed integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit signed integer.</returns>
        [CLSCompliant(false)]
        public static implicit operator Int128(ushort value) => new Int128(0, value);

        /// <summary>Implicitly converts a <see cref="uint" /> value to a 128-bit signed integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit signed integer.</returns>
        [CLSCompliant(false)]
        public static implicit operator Int128(uint value) => new Int128(0, value);

        /// <summary>Implicitly converts a <see cref="ulong" /> value to a 128-bit signed integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit signed integer.</returns>
        [CLSCompliant(false)]
        public static implicit operator Int128(ulong value) => new Int128(0, value);

        /// <summary>Implicitly converts a <see cref="UIntPtr" /> value to a 128-bit signed integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit signed integer.</returns>
        [CLSCompliant(false)]
        public static implicit operator Int128(nuint value) => new Int128(0, value);

        //
        // IAdditionOperators
        //

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        public static Int128 operator +(Int128 left, Int128 right)
        {
            // For unsigned addition, we can detect overflow by checking `(x + y) < x`
            // This gives us the carry to add to upper to compute the correct result

            ulong lower = left._lower + right._lower;
            ulong carry = (lower < left._lower) ? 1UL : 0UL;

            ulong upper = left._upper + right._upper + carry;
            return new Int128(upper, lower);
        }

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        public static Int128 operator checked +(Int128 left, Int128 right)
        {
            // For signed addition, we can detect overflow by checking if the sign of
            // both inputs are the same and then if that differs from the sign of the
            // output.

            Int128 result = left + right;

            uint sign = (uint)(left._upper >> 63);

            if (sign == (uint)(right._upper >> 63))
            {
                if (sign != (uint)(result._upper >> 63))
                {
                    ThrowHelper.ThrowOverflowException();
                }
            }
            return result;
        }

        //
        // IAdditiveIdentity
        //

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static Int128 IAdditiveIdentity<Int128, Int128>.AdditiveIdentity => default;

        //
        // IBinaryInteger
        //

        /// <inheritdoc cref="IBinaryInteger{TSelf}.DivRem(TSelf, TSelf)" />
        public static (Int128 Quotient, Int128 Remainder) DivRem(Int128 left, Int128 right)
        {
            Int128 quotient = left / right;
            return (quotient, left - (quotient * right));
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.LeadingZeroCount(TSelf)" />
        public static Int128 LeadingZeroCount(Int128 value)
        {
            if (value._upper == 0)
            {
                return 64 + ulong.LeadingZeroCount(value._lower);
            }
            return ulong.LeadingZeroCount(value._upper);
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.PopCount(TSelf)" />
        public static Int128 PopCount(Int128 value)
            => ulong.PopCount(value._lower) + ulong.PopCount(value._upper);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.RotateLeft(TSelf, int)" />
        public static Int128 RotateLeft(Int128 value, int rotateAmount)
            => (value << rotateAmount) | (value >>> (128 - rotateAmount));

        /// <inheritdoc cref="IBinaryInteger{TSelf}.RotateRight(TSelf, int)" />
        public static Int128 RotateRight(Int128 value, int rotateAmount)
            => (value >>> rotateAmount) | (value << (128 - rotateAmount));

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TrailingZeroCount(TSelf)" />
        public static Int128 TrailingZeroCount(Int128 value)
        {
            if (value._lower == 0)
            {
                return 64 + ulong.TrailingZeroCount(value._upper);
            }
            return ulong.TrailingZeroCount(value._lower);
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.GetShortestBitLength()" />
        int IBinaryInteger<Int128>.GetShortestBitLength()
        {
            Int128 value = this;

            if (IsPositive(value))
            {
                return (Size * 8) - BitOperations.LeadingZeroCount(value);
            }
            else
            {
                return (Size * 8) + 1 - BitOperations.LeadingZeroCount(~value);
            }
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.GetByteCount()" />
        int IBinaryInteger<Int128>.GetByteCount() => Size;

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryWriteBigEndian(Span{byte}, out int)" />
        bool IBinaryInteger<Int128>.TryWriteBigEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= Size)
            {
                ulong lower = _lower;
                ulong upper = _upper;

                if (BitConverter.IsLittleEndian)
                {
                    lower = BinaryPrimitives.ReverseEndianness(lower);
                    upper = BinaryPrimitives.ReverseEndianness(upper);
                }

                ref byte address = ref MemoryMarshal.GetReference(destination);

                Unsafe.WriteUnaligned(ref address, upper);
                Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref address, sizeof(ulong)), lower);

                bytesWritten = Size;
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryWriteLittleEndian(Span{byte}, out int)" />
        bool IBinaryInteger<Int128>.TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= Size)
            {
                ulong lower = _lower;
                ulong upper = _upper;

                if (!BitConverter.IsLittleEndian)
                {
                    lower = BinaryPrimitives.ReverseEndianness(lower);
                    upper = BinaryPrimitives.ReverseEndianness(upper);
                }

                ref byte address = ref MemoryMarshal.GetReference(destination);

                Unsafe.WriteUnaligned(ref address, lower);
                Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref address, sizeof(ulong)), upper);

                bytesWritten = Size;
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
        public static bool IsPow2(Int128 value) => (PopCount(value) == 1U) && IsPositive(value);

        /// <inheritdoc cref="IBinaryNumber{TSelf}.Log2(TSelf)" />
        public static Int128 Log2(Int128 value)
        {
            if (IsNegative(value))
            {
                ThrowHelper.ThrowValueArgumentOutOfRange_NeedNonNegNumException();
            }

            if (value._upper == 0)
            {
                return ulong.Log2(value._lower);
            }
            return 64 + ulong.Log2(value._upper);
        }

        //
        // IBitwiseOperators
        //

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseAnd(TSelf, TOther)" />
        public static Int128 operator &(Int128 left, Int128 right) => new Int128(left._upper & right._upper, left._lower & right._lower);

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseOr(TSelf, TOther)" />
        public static Int128 operator |(Int128 left, Int128 right) => new Int128(left._upper | right._upper, left._lower | right._lower);

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_ExclusiveOr(TSelf, TOther)" />
        public static Int128 operator ^(Int128 left, Int128 right) => new Int128(left._upper ^ right._upper, left._lower ^ right._lower);

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_OnesComplement(TSelf)" />
        public static Int128 operator ~(Int128 value) => new Int128(~value._upper, ~value._lower);

        //
        // IComparisonOperators
        //

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_LessThan(TSelf, TOther)" />
        public static bool operator <(Int128 left, Int128 right)
        {
            if (IsNegative(left) == IsNegative(right))
            {
                return (left._upper < right._upper)
                    || ((left._upper == right._upper) && (left._lower < right._lower));
            }
            else
            {
                return IsNegative(left);
            }
        }

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_LessThanOrEqual(TSelf, TOther)" />
        public static bool operator <=(Int128 left, Int128 right)
        {
            if (IsNegative(left) == IsNegative(right))
            {
                return (left._upper < right._upper)
                    || ((left._upper == right._upper) && (left._lower <= right._lower));
            }
            else
            {
                return IsNegative(left);
            }
        }

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_GreaterThan(TSelf, TOther)" />
        public static bool operator >(Int128 left, Int128 right)
        {
            if (IsNegative(left) == IsNegative(right))
            {
                return (left._upper > right._upper)
                    || ((left._upper == right._upper) && (left._lower > right._lower));
            }
            else
            {
                return IsNegative(right);
            }
        }

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_GreaterThanOrEqual(TSelf, TOther)" />
        public static bool operator >=(Int128 left, Int128 right)
        {
            if (IsNegative(left) == IsNegative(right))
            {
                return (left._upper > right._upper)
                    || ((left._upper == right._upper) && (left._lower >= right._lower));
            }
            else
            {
                return IsNegative(right);
            }
        }

        //
        // IDecrementOperators
        //

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        public static Int128 operator --(Int128 value) => value - One;

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        public static Int128 operator checked --(Int128 value) => checked(value - One);

        //
        // IDivisionOperators
        //

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_Division(TSelf, TOther)" />
        public static Int128 operator /(Int128 left, Int128 right)
        {
            if ((right == -1) && (left._upper == 0x8000_0000_0000_0000) && (left._lower == 0))
            {
                ThrowHelper.ThrowOverflowException();
            }

            // We simplify the logic here by just doing unsigned division on the
            // one's complement representation and then taking the correct sign.

            ulong sign = (left._upper ^ right._upper) & (1UL << 63);

            if (IsNegative(left))
            {
                left = ~left + 1U;
            }

            if (IsNegative(right))
            {
                right = ~right + 1U;
            }

            UInt128 result = (UInt128)(left) / (UInt128)(right);

            if (result == 0U)
            {
                sign = 0;
            }
            else if (sign != 0)
            {
                result = ~result + 1U;
            }

            return new Int128(
                result.Upper | sign,
                result.Lower
            );
        }

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_CheckedDivision(TSelf, TOther)" />
        public static Int128 operator checked /(Int128 left, Int128 right) => left / right;

        //
        // IEqualityOperators
        //

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther}.op_Equality(TSelf, TOther)" />
        public static bool operator ==(Int128 left, Int128 right) => (left._lower == right._lower) && (left._upper == right._upper);

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther}.op_Inequality(TSelf, TOther)" />
        public static bool operator !=(Int128 left, Int128 right) => (left._lower != right._lower) || (left._upper != right._upper);

        //
        // IIncrementOperators
        //

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_Increment(TSelf)" />
        public static Int128 operator ++(Int128 value) => value + One;

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_CheckedIncrement(TSelf)" />
        public static Int128 operator checked ++(Int128 value) => checked(value + One);

        //
        // IMinMaxValue
        //

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MinValue" />
        public static Int128 MinValue => new Int128(0x8000_0000_0000_0000, 0);

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MaxValue" />
        public static Int128 MaxValue => new Int128(0x7FFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF);

        //
        // IModulusOperators
        //

        /// <inheritdoc cref="IModulusOperators{TSelf, TOther, TResult}.op_Modulus(TSelf, TOther)" />
        public static Int128 operator %(Int128 left, Int128 right)
        {
            // We simplify the logic here by just doing unsigned modulus on the
            // one's complement representation and then taking the correct sign.

            ulong sign = (left._upper ^ right._upper) & (1UL << 63);

            if (IsNegative(left))
            {
                left = ~left + 1U;
            }

            if (IsNegative(right))
            {
                right = ~right + 1U;
            }

            UInt128 result = (UInt128)(left) % (UInt128)(right);

            if (result == 0U)
            {
                sign = 0;
            }
            else if (sign != 0)
            {
                result = ~result + 1U;
            }

            return new Int128(
                result.Upper | sign,
                result.Lower
            );
        }

        //
        // IMultiplicativeIdentity
        //

        /// <inheritdoc cref="IMultiplicativeIdentity{TSelf, TResult}.MultiplicativeIdentity" />
        static Int128 IMultiplicativeIdentity<Int128, Int128>.MultiplicativeIdentity => One;

        //
        // IMultiplyOperators
        //

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_Multiply(TSelf, TOther)" />
        public static Int128 operator *(Int128 left, Int128 right)
        {
            // We simplify the logic here by just doing unsigned multiplication on
            // the one's complement representation and then taking the correct sign.

            ulong sign = (left._upper ^ right._upper) & (1UL << 63);

            if (IsNegative(left))
            {
                left = ~left + 1U;
            }

            if (IsNegative(right))
            {
                right = ~right + 1U;
            }

            UInt128 result = (UInt128)(left) * (UInt128)(right);

            if (result == 0U)
            {
                sign = 0;
            }
            else if (sign != 0)
            {
                result = ~result + 1U;
            }

            return new Int128(
                result.Upper | sign,
                result.Lower
            );
        }

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_CheckedMultiply(TSelf, TOther)" />
        public static Int128 operator checked *(Int128 left, Int128 right)
        {
            // We simplify the logic here by just doing unsigned multiplication on
            // the one's complement representation and then taking the correct sign.

            ulong sign = (left._upper ^ right._upper) & (1UL << 63);

            if (IsNegative(left))
            {
                left = ~left + 1U;
            }

            if (IsNegative(right))
            {
                right = ~right + 1U;
            }

            UInt128 result = checked((UInt128)(left) * (UInt128)(right));

            if ((long)(result.Upper) < 0)
            {
                ThrowHelper.ThrowOverflowException();
            }

            if (result == 0U)
            {
                sign = 0;
            }
            else if (sign != 0)
            {
                result = ~result + 1U;
            }

            return new Int128(
                result.Upper | sign,
                result.Lower
            );
        }

        //
        // INumber
        //

        /// <inheritdoc cref="INumber{TSelf}.Clamp(TSelf, TSelf, TSelf)" />
        public static Int128 Clamp(Int128 value, Int128 min, Int128 max)
        {
            if (min > max)
            {
                Math.ThrowMinMaxException(min, max);
            }

            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;
        }

        /// <inheritdoc cref="INumber{TSelf}.CopySign(TSelf, TSelf)" />
        public static Int128 CopySign(Int128 value, Int128 sign)
        {
            Int128 absValue = value;

            if (IsNegative(absValue))
            {
                absValue = -absValue;
            }

            if (IsPositive(sign))
            {
                if (IsNegative(absValue))
                {
                    Math.ThrowNegateTwosCompOverflow();
                }
                return absValue;
            }
            return -absValue;
        }

        /// <inheritdoc cref="INumber{TSelf}.Max(TSelf, TSelf)" />
        public static Int128 Max(Int128 x, Int128 y) => (x >= y) ? x : y;

        /// <inheritdoc cref="INumber{TSelf}.MaxNumber(TSelf, TSelf)" />
        static Int128 INumber<Int128>.MaxNumber(Int128 x, Int128 y) => Max(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Min(TSelf, TSelf)" />
        public static Int128 Min(Int128 x, Int128 y) => (x <= y) ? x : y;

        /// <inheritdoc cref="INumber{TSelf}.MinNumber(TSelf, TSelf)" />
        static Int128 INumber<Int128>.MinNumber(Int128 x, Int128 y) => Min(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Sign(TSelf)" />
        public static int Sign(Int128 value)
        {
            if (IsNegative(value))
            {
                return -1;
            }
            else if (value != default)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        //
        // INumberBase
        //

        /// <inheritdoc cref="INumberBase{TSelf}.One" />
        public static Int128 One => new Int128(0, 1);

        /// <inheritdoc cref="INumberBase{TSelf}.Radix" />
        static int INumberBase<Int128>.Radix => 2;

        /// <inheritdoc cref="INumberBase{TSelf}.Zero" />
        public static Int128 Zero => default;

        /// <inheritdoc cref="INumberBase{TSelf}.Abs(TSelf)" />
        public static Int128 Abs(Int128 value)
        {
            if (IsNegative(value))
            {
                value = -value;

                if (IsNegative(value))
                {
                    Math.ThrowNegateTwosCompOverflow();
                }
            }
            return value;
        }

        /// <summary>Creates an instance of the current type from a value, throwing an overflow exception for any values that fall outside the representable range of the current type.</summary>
        /// <typeparam name="TOther">The type of <paramref name="value" />.</typeparam>
        /// <param name="value">The value which is used to create the instance of <see cref="Int128" />.</param>
        /// <returns>An instance of <see cref="Int128" /> created from <paramref name="value" />.</returns>
        /// <exception cref="NotSupportedException"><typeparamref name="TOther" /> is not supported.</exception>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="Int128" />.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int128 CreateChecked<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            Int128 result;

            if (typeof(TOther) == typeof(Int128))
            {
                result = (Int128)(object)value;
            }
            else if (!NumberBase<Int128>.TryConvertFromChecked(value, out result) && !TOther.TryConvertToChecked(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <summary>Creates an instance of the current type from a value, saturating any values that fall outside the representable range of the current type.</summary>
        /// <typeparam name="TOther">The type of <paramref name="value" />.</typeparam>
        /// <param name="value">The value which is used to create the instance of <see cref="Int128" />.</param>
        /// <returns>An instance of <see cref="Int128" /> created from <paramref name="value" />, saturating if <paramref name="value" /> falls outside the representable range of <see cref="Int128" />.</returns>
        /// <exception cref="NotSupportedException"><typeparamref name="TOther" /> is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int128 CreateSaturating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            Int128 result;

            if (typeof(TOther) == typeof(Int128))
            {
                result = (Int128)(object)value;
            }
            else if (!NumberBase<Int128>.TryConvertFromSaturating(value, out result) && !TOther.TryConvertToSaturating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <summary>Creates an instance of the current type from a value, truncating any values that fall outside the representable range of the current type.</summary>
        /// <typeparam name="TOther">The type of <paramref name="value" />.</typeparam>
        /// <param name="value">The value which is used to create the instance of <see cref="Int128" />.</param>
        /// <returns>An instance of <see cref="Int128" /> created from <paramref name="value" />, truncating if <paramref name="value" /> falls outside the representable range of <see cref="Int128" />.</returns>
        /// <exception cref="NotSupportedException"><typeparamref name="TOther" /> is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int128 CreateTruncating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            Int128 result;

            if (typeof(TOther) == typeof(Int128))
            {
                result = (Int128)(object)value;
            }
            else if (!NumberBase<Int128>.TryConvertFromTruncating(value, out result) && !TOther.TryConvertToTruncating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsCanonical(TSelf)" />
        static bool INumberBase<Int128>.IsCanonical(Int128 value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsComplexNumber(TSelf)" />
        static bool INumberBase<Int128>.IsComplexNumber(Int128 value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsEvenInteger(TSelf)" />
        public static bool IsEvenInteger(Int128 value) => (value._lower & 1) == 0;

        /// <inheritdoc cref="INumberBase{TSelf}.IsFinite(TSelf)" />
        static bool INumberBase<Int128>.IsFinite(Int128 value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsImaginaryNumber(TSelf)" />
        static bool INumberBase<Int128>.IsImaginaryNumber(Int128 value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsInfinity(TSelf)" />
        static bool INumberBase<Int128>.IsInfinity(Int128 value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsInteger(TSelf)" />
        static bool INumberBase<Int128>.IsInteger(Int128 value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsNaN(TSelf)" />
        static bool INumberBase<Int128>.IsNaN(Int128 value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsNegative(TSelf)" />
        public static bool IsNegative(Int128 value) => (long)value._upper < 0;

        /// <inheritdoc cref="INumberBase{TSelf}.IsNegativeInfinity(TSelf)" />
        static bool INumberBase<Int128>.IsNegativeInfinity(Int128 value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsNormal(TSelf)" />
        static bool INumberBase<Int128>.IsNormal(Int128 value) => value != 0;

        /// <inheritdoc cref="INumberBase{TSelf}.IsOddInteger(TSelf)" />
        public static bool IsOddInteger(Int128 value) => (value._lower & 1) != 0;

        /// <inheritdoc cref="INumberBase{TSelf}.IsPositive(TSelf)" />
        public static bool IsPositive(Int128 value) => (long)value._upper >= 0;

        /// <inheritdoc cref="INumberBase{TSelf}.IsPositiveInfinity(TSelf)" />
        static bool INumberBase<Int128>.IsPositiveInfinity(Int128 value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsRealNumber(TSelf)" />
        static bool INumberBase<Int128>.IsRealNumber(Int128 value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsSubnormal(TSelf)" />
        static bool INumberBase<Int128>.IsSubnormal(Int128 value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsZero(TSelf)" />
        static bool INumberBase<Int128>.IsZero(Int128 value) => (value == 0);

        /// <inheritdoc cref="INumberBase{TSelf}.MaxMagnitude(TSelf, TSelf)" />
        public static Int128 MaxMagnitude(Int128 x, Int128 y)
        {
            Int128 absX = x;

            if (IsNegative(absX))
            {
                absX = -absX;

                if (IsNegative(absX))
                {
                    return x;
                }
            }

            Int128 absY = y;

            if (IsNegative(absY))
            {
                absY = -absY;

                if (IsNegative(absY))
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
        static Int128 INumberBase<Int128>.MaxMagnitudeNumber(Int128 x, Int128 y) => MaxMagnitude(x, y);

        /// <inheritdoc cref="INumberBase{TSelf}.MinMagnitude(TSelf, TSelf)" />
        public static Int128 MinMagnitude(Int128 x, Int128 y)
        {
            Int128 absX = x;

            if (IsNegative(absX))
            {
                absX = -absX;

                if (IsNegative(absX))
                {
                    return y;
                }
            }

            Int128 absY = y;

            if (IsNegative(absY))
            {
                absY = -absY;

                if (IsNegative(absY))
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
        static Int128 INumberBase<Int128>.MinMagnitudeNumber(Int128 x, Int128 y) => MinMagnitude(x, y);

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromChecked{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Int128>.TryConvertFromChecked<TOther>(TOther value, out Int128 result)
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the the one that handles unsigned will also
            // handle `Decimal` and `NFloat`.
            //
            // That is, `ConvertFrom` for `Int128` will handle the other signed types and
            // `ConvertTo` will handle the unsigned types

            if (typeof(TOther) == typeof(double))
            {
                double actualValue = (double)(object)value;
                result = checked((Int128)actualValue);
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualValue = (Half)(object)value;
                result = checked((Int128)actualValue);
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
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualValue = (nint)(object)value;
                result = actualValue;
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
                result = checked((Int128)actualValue);
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
        static bool INumberBase<Int128>.TryConvertFromSaturating<TOther>(TOther value, out Int128 result)
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the the one that handles unsigned will also
            // handle `Decimal` and `NFloat`.
            //
            // That is, `ConvertFrom` for `Int128` will handle the other signed types and
            // `ConvertTo` will handle the unsigned types

            if (typeof(TOther) == typeof(double))
            {
                double actualValue = (double)(object)value;
                result = (actualValue >= +170141183460469231731687303715884105727.0) ? MaxValue :
                         (actualValue <= -170141183460469231731687303715884105728.0) ? MinValue : (Int128)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualValue = (Half)(object)value;
                result = (actualValue == Half.PositiveInfinity) ? MaxValue :
                         (actualValue == Half.NegativeInfinity) ? MinValue : (Int128)actualValue;
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
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualValue = (nint)(object)value;
                result = actualValue;
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
                result = (actualValue >= +170141183460469231731687303715884105727.0f) ? MaxValue :
                         (actualValue <= -170141183460469231731687303715884105728.0f) ? MinValue : (Int128)actualValue;
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
        static bool INumberBase<Int128>.TryConvertFromTruncating<TOther>(TOther value, out Int128 result)
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the the one that handles unsigned will also
            // handle `Decimal` and `NFloat`.
            //
            // That is, `ConvertFrom` for `Int128` will handle the other signed types and
            // `ConvertTo` will handle the unsigned types

            if (typeof(TOther) == typeof(double))
            {
                double actualValue = (double)(object)value;
                result = (actualValue >= +170141183460469231731687303715884105727.0) ? MaxValue :
                         (actualValue <= -170141183460469231731687303715884105728.0) ? MinValue : (Int128)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualValue = (Half)(object)value;
                result = (actualValue == Half.PositiveInfinity) ? MaxValue :
                         (actualValue == Half.NegativeInfinity) ? MinValue : (Int128)actualValue;
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
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualValue = (nint)(object)value;
                result = actualValue;
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
                result = (actualValue >= +170141183460469231731687303715884105727.0f) ? MaxValue :
                         (actualValue <= -170141183460469231731687303715884105728.0f) ? MinValue : (Int128)actualValue;
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
        static bool INumberBase<Int128>.TryConvertToChecked<TOther>(Int128 value, [NotNullWhen(true)] out TOther result)
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the the one that handles unsigned will also
            // handle `Decimal` and `NFloat`.
            //
            // That is, `ConvertFrom` for `Int128` will handle the other signed types and
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
                decimal actualResult = checked((decimal)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(NFloat))
            {
                NFloat actualResult = (NFloat)value;
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
        static bool INumberBase<Int128>.TryConvertToSaturating<TOther>(Int128 value, [NotNullWhen(true)] out TOther result)
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the the one that handles unsigned will also
            // handle `Decimal` and `NFloat`.
            //
            // That is, `ConvertFrom` for `Int128` will handle the other signed types and
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
                decimal actualResult = (value >= new Int128(0x0000_0000_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)) ? decimal.MaxValue :
                                       (value <= new Int128(0xFFFF_FFFF_0000_0000, 0x0000_0000_0000_0001)) ? decimal.MinValue : (decimal)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(NFloat))
            {
                NFloat actualResult = (NFloat)value;
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
                nuint actualResult = (value >= nuint.MaxValue) ? nuint.MaxValue :
                                     (value <= nuint.MinValue) ? nuint.MinValue : (nuint)value;
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
        static bool INumberBase<Int128>.TryConvertToTruncating<TOther>(Int128 value, [NotNullWhen(true)] out TOther result)
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the the one that handles unsigned will also
            // handle `Decimal` and `NFloat`.
            //
            // That is, `ConvertFrom` for `Int128` will handle the other signed types and
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
                decimal actualResult;

                if (IsNegative(value))
                {
                    value = (-value) & new Int128(0x0000_0000_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF);
                    actualResult = -(decimal)value;
                }
                else
                {
                    value &= new Int128(0x0000_0000_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF);
                    actualResult = (decimal)value;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(NFloat))
            {
                NFloat actualResult = (NFloat)value;
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
        // IParsable
        //

        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Int128 result) => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // IShiftOperators
        //

        /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_LeftShift(TSelf, int)" />
        public static Int128 operator <<(Int128 value, int shiftAmount)
        {
            // C# automatically masks the shift amount for UInt64 to be 0x3F. So we
            // need to specially handle things if the 7th bit is set.

            shiftAmount &= 0x7F;

            if ((shiftAmount & 0x40) != 0)
            {
                // In the case it is set, we know the entire lower bits must be zero
                // and so the upper bits are just the lower shifted by the remaining
                // masked amount

                ulong upper = value._lower << shiftAmount;
                return new Int128(upper, 0);
            }
            else if (shiftAmount != 0)
            {
                // Otherwise we need to shift both upper and lower halves by the masked
                // amount and then or that with whatever bits were shifted "out" of lower

                ulong lower = value._lower << shiftAmount;
                ulong upper = (value._upper << shiftAmount) | (value._lower >> (64 - shiftAmount));

                return new Int128(upper, lower);
            }
            else
            {
                return value;
            }
        }

        /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_RightShift(TSelf, int)" />
        public static Int128 operator >>(Int128 value, int shiftAmount)
        {
            // C# automatically masks the shift amount for UInt64 to be 0x3F. So we
            // need to specially handle things if the 7th bit is set.

            shiftAmount &= 0x7F;

            if ((shiftAmount & 0x40) != 0)
            {
                // In the case it is set, we know the entire upper bits must be the sign
                // and so the lower bits are just the upper shifted by the remaining
                // masked amount

                ulong lower = value._upper >> shiftAmount;
                ulong upper = (ulong)((long)value._upper >> 63);

                return new Int128(upper, lower);
            }
            else if (shiftAmount != 0)
            {
                // Otherwise we need to shift both upper and lower halves by the masked
                // amount and then or that with whatever bits were shifted "out" of upper

                ulong lower = (value._lower >> shiftAmount) | (value._upper << (64 - shiftAmount));
                ulong upper = (ulong)((long)value._upper >> shiftAmount);

                return new Int128(upper, lower);
            }
            else
            {
                return value;
            }
        }

        /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_UnsignedRightShift(TSelf, int)" />
        public static Int128 operator >>>(Int128 value, int shiftAmount)
        {
            // C# automatically masks the shift amount for UInt64 to be 0x3F. So we
            // need to specially handle things if the 7th bit is set.

            shiftAmount &= 0x7F;

            if ((shiftAmount & 0x40) != 0)
            {
                // In the case it is set, we know the entire upper bits must be zero
                // and so the lower bits are just the upper shifted by the remaining
                // masked amount

                ulong lower = value._upper >> shiftAmount;
                return new Int128(0, lower);
            }
            else if (shiftAmount != 0)
            {
                // Otherwise we need to shift both upper and lower halves by the masked
                // amount and then or that with whatever bits were shifted "out" of upper

                ulong lower = (value._lower >> shiftAmount) | (value._upper << (64 - shiftAmount));
                ulong upper = value._upper >> shiftAmount;

                return new Int128(upper, lower);
            }
            else
            {
                return value;
            }
        }

        //
        // ISignedNumber
        //

        /// <inheritdoc cref="ISignedNumber{TSelf}.NegativeOne" />
        public static Int128 NegativeOne => new Int128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF);

        //
        // ISpanParsable
        //

        /// <inheritdoc cref="ISpanParsable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static Int128 Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Integer, provider);

        /// <inheritdoc cref="ISpanParsable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Int128 result) => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // ISubtractionOperators
        //

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_Subtraction(TSelf, TOther)" />
        public static Int128 operator -(Int128 left, Int128 right)
        {
            // For unsigned subtract, we can detect overflow by checking `(x - y) > x`
            // This gives us the borrow to subtract from upper to compute the correct result

            ulong lower = left._lower - right._lower;
            ulong borrow = (lower > left._lower) ? 1UL : 0UL;

            ulong upper = left._upper - right._upper - borrow;
            return new Int128(upper, lower);
        }

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_CheckedSubtraction(TSelf, TOther)" />
        public static Int128 operator checked -(Int128 left, Int128 right)
        {
            // For signed subtraction, we can detect overflow by checking if the sign of
            // both inputs are different and then if that differs from the sign of the
            // output.

            Int128 result = left - right;

            uint sign = (uint)(left._upper >> 63);

            if (sign != (uint)(right._upper >> 63))
            {
                if (sign != (uint)(result._upper >> 63))
                {
                    ThrowHelper.ThrowOverflowException();
                }
            }
            return result;
        }

        //
        // IUnaryNegationOperators
        //

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_UnaryNegation(TSelf)" />
        public static Int128 operator -(Int128 value) => Zero - value;

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_CheckedUnaryNegation(TSelf)" />
        public static Int128 operator checked -(Int128 value) => checked(Zero - value);

        //
        // IUnaryPlusOperators
        //

        /// <inheritdoc cref="IUnaryPlusOperators{TSelf, TResult}.op_UnaryPlus(TSelf)" />
        public static Int128 operator +(Int128 value) => value;
    }
}
