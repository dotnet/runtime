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
    /// <summary>Represents a 128-bit unsigned integer.</summary>
    [CLSCompliant(false)]
    [Intrinsic]
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct UInt128
        : IBinaryInteger<UInt128>,
          IMinMaxValue<UInt128>,
          IUnsignedNumber<UInt128>
    {
        internal const int Size = 16;

        // Unix System V ABI actually requires this to be little endian
        // order and not `upper, lower` on big endian systems.

        private readonly ulong _lower;
        private readonly ulong _upper;

        /// <summary>Initializes a new instance of the <see cref="UInt128" /> struct.</summary>
        /// <param name="upper">The upper 64-bits of the 128-bit value.</param>
        /// <param name="lower">The lower 64-bits of the 128-bit value.</param>
        [CLSCompliant(false)]
        public UInt128(ulong upper, ulong lower)
        {
            _lower = lower;
            _upper = upper;
        }

        internal ulong Lower => _lower;

        internal ulong Upper => _upper;

        /// <inheritdoc cref="IComparable.CompareTo(object)" />
        public int CompareTo(object? value)
        {
            if (value is UInt128 other)
            {
                return CompareTo(other);
            }
            else if (value is null)
            {
                return 1;
            }
            else
            {
                throw new ArgumentException(SR.Arg_MustBeUInt128);
            }
        }

        /// <inheritdoc cref="IComparable{T}.CompareTo(T)" />
        public int CompareTo(UInt128 value)
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
            return (obj is UInt128 other) && Equals(other);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(UInt128 other)
        {
            return this == other;
        }

        /// <inheritdoc cref="object.GetHashCode()" />
        public override int GetHashCode() => HashCode.Combine(_lower, _upper);

        /// <inheritdoc cref="object.ToString()" />
        public override string ToString()
        {
            return Number.UInt128ToDecStr(this);
        }

        public string ToString(IFormatProvider? provider)
        {
            return Number.FormatUInt128(this, null, provider);
        }

        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
        {
            return Number.FormatUInt128(this, format, null);
        }

        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider)
        {
            return Number.FormatUInt128(this, format, provider);
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatUInt128(this, format, provider, destination, out charsWritten);
        }

        public static UInt128 Parse(string s)
        {
            ArgumentNullException.ThrowIfNull(s);
            return Number.ParseUInt128(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);
        }

        public static UInt128 Parse(string s, NumberStyles style)
        {
            ArgumentNullException.ThrowIfNull(s);
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.ParseUInt128(s, style, NumberFormatInfo.CurrentInfo);
        }

        public static UInt128 Parse(string s, IFormatProvider? provider)
        {
            ArgumentNullException.ThrowIfNull(s);
            return Number.ParseUInt128(s, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
        }

        public static UInt128 Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            ArgumentNullException.ThrowIfNull(s);
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.ParseUInt128(s, style, NumberFormatInfo.GetInstance(provider));
        }

        public static UInt128 Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.ParseUInt128(s, style, NumberFormatInfo.GetInstance(provider));
        }

        public static bool TryParse([NotNullWhen(true)] string? s, out UInt128 result)
        {
            if (s is not null)
            {
                return Number.TryParseUInt128IntegerStyle(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result) == Number.ParsingStatus.OK;
            }
            else
            {
                result = default;
                return false;
            }
        }

        public static bool TryParse(ReadOnlySpan<char> s, out UInt128 result)
        {
            return Number.TryParseUInt128IntegerStyle(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result) == Number.ParsingStatus.OK;
        }

        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out UInt128 result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);

            if (s is not null)
            {
                return Number.TryParseUInt128(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
            }
            else
            {
                result = default;
                return false;
            }
        }

        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out UInt128 result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.TryParseUInt128(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
        }

        //
        // Explicit Conversions From UInt128
        //

        /// <summary>Explicitly converts a 128-bit unsigned integer to a <see cref="byte" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="byte" />.</returns>
        public static explicit operator byte(UInt128 value) => (byte)value._lower;

        /// <summary>Explicitly converts a 128-bit unsigned integer to a <see cref="byte" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="byte" />.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="UInt128" />.</exception>
        public static explicit operator checked byte(UInt128 value)
        {
            if (value._upper != 0)
            {
                ThrowHelper.ThrowOverflowException();
            }
            return checked((byte)value._lower);
        }

        /// <summary>Explicitly converts a 128-bit unsigned integer to a <see cref="char" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="char" />.</returns>
        public static explicit operator char(UInt128 value) => (char)value._lower;

        /// <summary>Explicitly converts a 128-bit unsigned integer to a <see cref="char" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="char" />.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="UInt128" />.</exception>
        public static explicit operator checked char(UInt128 value)
        {
            if (value._upper != 0)
            {
                ThrowHelper.ThrowOverflowException();
            }
            return checked((char)value._lower);
        }

        /// <summary>Explicitly converts a 128-bit unsigned integer to a <see cref="decimal" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="decimal" />.</returns>
        public static explicit operator decimal(UInt128 value)
        {
            ulong lo64 = value._lower;

            if (value._upper > uint.MaxValue)
            {
                // The default behavior of decimal conversions is to always throw on overflow
                Number.ThrowOverflowException(TypeCode.Decimal);
            }

            uint hi32 = (uint)(value._upper);

            return new decimal((int)(lo64), (int)(lo64 >> 32), (int)(hi32), isNegative: false, scale: 0);
        }

        /// <summary>Explicitly converts a 128-bit unsigned integer to a <see cref="double" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="double" />.</returns>
        public static explicit operator double(UInt128 value)
        {
            // This code is based on `u128_to_f64_round` from m-ou-se/floatconv
            // Copyright (c) 2020 Mara Bos <m-ou.se@m-ou.se>. All rights reserved.
            //
            // Licensed under the BSD 2 - Clause "Simplified" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            const double TwoPow52 = 4503599627370496.0;
            const double TwoPow76 = 75557863725914323419136.0;
            const double TwoPow104 = 20282409603651670423947251286016.0;
            const double TwoPow128 = 340282366920938463463374607431768211456.0;

            const ulong TwoPow52Bits = 0x4330000000000000;
            const ulong TwoPow76Bits = 0x44B0000000000000;
            const ulong TwoPow104Bits = 0x4670000000000000;
            const ulong TwoPow128Bits = 0x47F0000000000000;

            if (value._upper == 0)
            {
                // For values between 0 and ulong.MaxValue, we just use the existing conversion
                return (double)(value._lower);
            }
            else if ((value._upper >> 24) == 0) // value < (2^104)
            {
                // For values greater than ulong.MaxValue but less than 2^104 this takes advantage
                // that we can represent both "halves" of the uint128 within the 52-bit mantissa of
                // a pair of doubles.

                double lower = BitConverter.UInt64BitsToDouble(TwoPow52Bits | ((value._lower << 12) >> 12)) - TwoPow52;
                double upper = BitConverter.UInt64BitsToDouble(TwoPow104Bits | (ulong)(value >> 52)) - TwoPow104;

                return lower + upper;
            }
            else
            {
                // For values greater than than 2^104 we basically do the same as before but we need to account
                // for the precision loss that double will have. As such, the lower value effectively drops the
                // lowest 24 bits and then or's them back to ensure rounding stays correct.

                double lower = BitConverter.UInt64BitsToDouble(TwoPow76Bits | ((ulong)(value >> 12) >> 12) | (value._lower & 0xFFFFFF)) - TwoPow76;
                double upper = BitConverter.UInt64BitsToDouble(TwoPow128Bits | (ulong)(value >> 76)) - TwoPow128;

                return lower + upper;
            }
        }

        /// <summary>Explicitly converts a 128-bit unsigned integer to a <see cref="Half" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="Half" />.</returns>
        public static explicit operator Half(UInt128 value) => (Half)(double)(value);

        /// <summary>Explicitly converts a 128-bit unsigned integer to a <see cref="short" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="short" />.</returns>
        public static explicit operator short(UInt128 value) => (short)value._lower;

        /// <summary>Explicitly converts a 128-bit unsigned integer to a <see cref="short" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="short" />.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="UInt128" />.</exception>
        public static explicit operator checked short(UInt128 value)
        {
            if (value._upper != 0)
            {
                ThrowHelper.ThrowOverflowException();
            }
            return checked((short)value._lower);
        }

        /// <summary>Explicitly converts a 128-bit unsigned integer to a <see cref="int" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="int" />.</returns>
        public static explicit operator int(UInt128 value) => (int)value._lower;

        /// <summary>Explicitly converts a 128-bit unsigned integer to a <see cref="int" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="int" />.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="UInt128" />.</exception>
        public static explicit operator checked int(UInt128 value)
        {
            if (value._upper != 0)
            {
                ThrowHelper.ThrowOverflowException();
            }
            return checked((int)value._lower);
        }

        /// <summary>Explicitly converts a 128-bit unsigned integer to a <see cref="long" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="long" />.</returns>
        public static explicit operator long(UInt128 value) => (long)value._lower;

        /// <summary>Explicitly converts a 128-bit unsigned integer to a <see cref="long" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="long" />.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="UInt128" />.</exception>
        public static explicit operator checked long(UInt128 value)
        {
            if (value._upper != 0)
            {
                ThrowHelper.ThrowOverflowException();
            }
            return checked((long)value._lower);
        }

        /// <summary>Explicitly converts a 128-bit unsigned integer to a <see cref="Int128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="Int128" />.</returns>
        [CLSCompliant(false)]
        public static explicit operator Int128(UInt128 value) => new Int128(value._upper, value._lower);

        /// <summary>Explicitly converts a 128-bit unsigned integer to a <see cref="Int128" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="Int128" />.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="UInt128" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked Int128(UInt128 value)
        {
            if ((long)value._upper < 0)
            {
                ThrowHelper.ThrowOverflowException();
            }
            return new Int128(value._upper, value._lower);
        }

        /// <summary>Explicitly converts a 128-bit unsigned integer to a <see cref="IntPtr" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="IntPtr" />.</returns>
        public static explicit operator nint(UInt128 value) => (nint)value._lower;

        /// <summary>Explicitly converts a 128-bit unsigned integer to a <see cref="IntPtr" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="IntPtr" />.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="UInt128" />.</exception>
        public static explicit operator checked nint(UInt128 value)
        {
            if (value._upper != 0)
            {
                ThrowHelper.ThrowOverflowException();
            }
            return checked((nint)value._lower);
        }

        /// <summary>Explicitly converts a 128-bit unsigned integer to a <see cref="sbyte" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="sbyte" />.</returns>
        [CLSCompliant(false)]
        public static explicit operator sbyte(UInt128 value) => (sbyte)value._lower;

        /// <summary>Explicitly converts a 128-bit unsigned integer to a <see cref="sbyte" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="sbyte" />.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="UInt128" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked sbyte(UInt128 value)
        {
            if (value._upper != 0)
            {
                ThrowHelper.ThrowOverflowException();
            }
            return checked((sbyte)value._lower);
        }

        /// <summary>Explicitly converts a 128-bit unsigned integer to a <see cref="float" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="float" />.</returns>
        public static explicit operator float(UInt128 value) => (float)(double)(value);

        /// <summary>Explicitly converts a 128-bit unsigned integer to a <see cref="ushort" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="ushort" />.</returns>
        [CLSCompliant(false)]
        public static explicit operator ushort(UInt128 value) => (ushort)value._lower;

        /// <summary>Explicitly converts a 128-bit unsigned integer to a <see cref="ushort" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="ushort" />.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="UInt128" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked ushort(UInt128 value)
        {
            if (value._upper != 0)
            {
                ThrowHelper.ThrowOverflowException();
            }
            return checked((ushort)value._lower);
        }

        /// <summary>Explicitly converts a 128-bit unsigned integer to a <see cref="uint" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="uint" />.</returns>
        [CLSCompliant(false)]
        public static explicit operator uint(UInt128 value) => (uint)value._lower;

        /// <summary>Explicitly converts a 128-bit unsigned integer to a <see cref="uint" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="uint" />.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="UInt128" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked uint(UInt128 value)
        {
            if (value._upper != 0)
            {
                ThrowHelper.ThrowOverflowException();
            }
            return checked((uint)value._lower);
        }

        /// <summary>Explicitly converts a 128-bit unsigned integer to a <see cref="ulong" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="ulong" />.</returns>
        [CLSCompliant(false)]
        public static explicit operator ulong(UInt128 value) => value._lower;

        /// <summary>Explicitly converts a 128-bit unsigned integer to a <see cref="ulong" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="ulong" />.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="UInt128" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked ulong(UInt128 value)
        {
            if (value._upper != 0)
            {
                ThrowHelper.ThrowOverflowException();
            }
            return value._lower;
        }

        /// <summary>Explicitly converts a 128-bit unsigned integer to a <see cref="UIntPtr" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="UIntPtr" />.</returns>
        [CLSCompliant(false)]
        public static explicit operator nuint(UInt128 value) => (nuint)value._lower;

        /// <summary>Explicitly converts a 128-bit unsigned integer to a <see cref="UIntPtr" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="UIntPtr" />.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="UInt128" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked nuint(UInt128 value)
        {
            if (value._upper != 0)
            {
                ThrowHelper.ThrowOverflowException();
            }
            return checked((nuint)value._lower);
        }

        //
        // Explicit Conversions To UInt128
        //

        /// <summary>Explicitly converts a <see cref="decimal" /> value to a 128-bit unsigned integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit unsigned integer.</returns>
        public static explicit operator UInt128(decimal value)
        {
            value = decimal.Truncate(value);

            if (value < 0.0m)
            {
                ThrowHelper.ThrowOverflowException();
            }
            return new UInt128(value.High, value.Low64);
        }

        /// <summary>Explicitly converts a <see cref="double" /> value to a 128-bit unsigned integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit unsigned integer.</returns>
        public static explicit operator UInt128(double value)
        {
            const double TwoPow128 = 340282366920938463463374607431768211456.0;

            if (double.IsNegative(value) || double.IsNaN(value))
            {
                return MinValue;
            }
            else if (value >= TwoPow128)
            {
                return MaxValue;
            }

            return ToUInt128(value);
        }

        /// <summary>Explicitly converts a <see cref="double" /> value to a 128-bit unsigned integer, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit unsigned integer.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="UInt128" />.</exception>
        public static explicit operator checked UInt128(double value)
        {
            const double TwoPow128 = 340282366920938463463374607431768211456.0;

            // We need to convert -0.0 to 0 and not throw, so we compare
            // value against 0 rather than checking IsNegative

            if ((value < 0.0) || double.IsNaN(value) || (value >= TwoPow128))
            {
                ThrowHelper.ThrowOverflowException();
            }

            return ToUInt128(value);
        }

        internal static UInt128 ToUInt128(double value)
        {
            const double TwoPow128 = 340282366920938463463374607431768211456.0;

            Debug.Assert(value >= 0);
            Debug.Assert(double.IsFinite(value));
            Debug.Assert(value < TwoPow128);

            // This code is based on `f64_to_u128` from m-ou-se/floatconv
            // Copyright (c) 2020 Mara Bos <m-ou.se@m-ou.se>. All rights reserved.
            //
            // Licensed under the BSD 2 - Clause "Simplified" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            if (value >= 1.0)
            {
                // In order to convert from double to uint128 we first need to extract the signficand,
                // including the implicit leading bit, as a full 128-bit significand. We can then adjust
                // this down to the represented integer by right shifting by the unbiased exponent, taking
                // into account the significand is now represented as 128-bits.

                ulong bits = BitConverter.DoubleToUInt64Bits(value);
                UInt128 result = new UInt128((bits << 12) >> 1 | 0x8000_0000_0000_0000, 0x0000_0000_0000_0000);

                result >>= (1023 + 128 - 1 - (int)(bits >> 52));
                return result;
            }
            else
            {
                return MinValue;
            }
        }

        /// <summary>Explicitly converts a <see cref="Half" /> value to a 128-bit unsigned integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit unsigned integer.</returns>
        public static explicit operator UInt128(Half value) => (UInt128)(double)(value);

        /// <summary>Explicitly converts a <see cref="Half" /> value to a 128-bit unsigned integer, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit unsigned integer.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="UInt128" />.</exception>
        public static explicit operator checked UInt128(Half value) => checked((UInt128)(double)(value));

        /// <summary>Explicitly converts a <see cref="short" /> value to a 128-bit unsigned integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit unsigned integer.</returns>
        public static explicit operator UInt128(short value)
        {
            long lower = value;
            return new UInt128((ulong)(lower >> 63), (ulong)lower);
        }

        /// <summary>Explicitly converts a <see cref="short" /> value to a 128-bit unsigned integer, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit unsigned integer.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="UInt128" />.</exception>
        public static explicit operator checked UInt128(short value)
        {
            if (value < 0)
            {
                ThrowHelper.ThrowOverflowException();
            }
            return new UInt128(0, (ushort)value);
        }

        /// <summary>Explicitly converts a <see cref="int" /> value to a 128-bit unsigned integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit unsigned integer.</returns>
        public static explicit operator UInt128(int value)
        {
            long lower = value;
            return new UInt128((ulong)(lower >> 63), (ulong)lower);
        }

        /// <summary>Explicitly converts a <see cref="int" /> value to a 128-bit unsigned integer, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit unsigned integer.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="UInt128" />.</exception>
        public static explicit operator checked UInt128(int value)
        {
            if (value < 0)
            {
                ThrowHelper.ThrowOverflowException();
            }
            return new UInt128(0, (uint)value);
        }

        /// <summary>Explicitly converts a <see cref="long" /> value to a 128-bit unsigned integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit unsigned integer.</returns>
        public static explicit operator UInt128(long value)
        {
            long lower = value;
            return new UInt128((ulong)(lower >> 63), (ulong)lower);
        }

        /// <summary>Explicitly converts a <see cref="long" /> value to a 128-bit unsigned integer, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit unsigned integer.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="UInt128" />.</exception>
        public static explicit operator checked UInt128(long value)
        {
            if (value < 0)
            {
                ThrowHelper.ThrowOverflowException();
            }
            return new UInt128(0, (ulong)value);
        }

        /// <summary>Explicitly converts a <see cref="IntPtr" /> value to a 128-bit unsigned integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit unsigned integer.</returns>
        public static explicit operator UInt128(nint value)
        {
            long lower = value;
            return new UInt128((ulong)(lower >> 63), (ulong)lower);
        }

        /// <summary>Explicitly converts a <see cref="IntPtr" /> value to a 128-bit unsigned integer, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit unsigned integer.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="UInt128" />.</exception>
        public static explicit operator checked UInt128(nint value)
        {
            if (value < 0)
            {
                ThrowHelper.ThrowOverflowException();
            }
            return new UInt128(0, (nuint)value);
        }

        /// <summary>Explicitly converts a <see cref="sbyte" /> value to a 128-bit unsigned integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit unsigned integer.</returns>
        [CLSCompliant(false)]
        public static explicit operator UInt128(sbyte value)
        {
            long lower = value;
            return new UInt128((ulong)(lower >> 63), (ulong)lower);
        }

        /// <summary>Explicitly converts a <see cref="sbyte" /> value to a 128-bit unsigned integer, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit unsigned integer.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="UInt128" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked UInt128(sbyte value)
        {
            if (value < 0)
            {
                ThrowHelper.ThrowOverflowException();
            }
            return new UInt128(0, (byte)value);
        }

        /// <summary>Explicitly converts a <see cref="float" /> value to a 128-bit unsigned integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit unsigned integer.</returns>
        public static explicit operator UInt128(float value) => (UInt128)(double)(value);

        /// <summary>Explicitly converts a <see cref="float" /> value to a 128-bit unsigned integer, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit unsigned integer.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="UInt128" />.</exception>
        public static explicit operator checked UInt128(float value) => checked((UInt128)(double)(value));

        //
        // Implicit Conversions To UInt128
        //

        /// <summary>Implicitly converts a <see cref="byte" /> value to a 128-bit unsigned integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit unsigned integer.</returns>
        public static implicit operator UInt128(byte value) => new UInt128(0, value);

        /// <summary>Implicitly converts a <see cref="char" /> value to a 128-bit unsigned integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit unsigned integer.</returns>
        public static implicit operator UInt128(char value) => new UInt128(0, value);

        /// <summary>Implicitly converts a <see cref="ushort" /> value to a 128-bit unsigned integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit unsigned integer.</returns>
        [CLSCompliant(false)]
        public static implicit operator UInt128(ushort value) => new UInt128(0, value);

        /// <summary>Implicitly converts a <see cref="uint" /> value to a 128-bit unsigned integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit unsigned integer.</returns>
        [CLSCompliant(false)]
        public static implicit operator UInt128(uint value) => new UInt128(0, value);

        /// <summary>Implicitly converts a <see cref="ulong" /> value to a 128-bit unsigned integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit unsigned integer.</returns>
        [CLSCompliant(false)]
        public static implicit operator UInt128(ulong value) => new UInt128(0, value);

        /// <summary>Implicitly converts a <see cref="UIntPtr" /> value to a 128-bit unsigned integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit unsigned integer.</returns>
        [CLSCompliant(false)]
        public static implicit operator UInt128(nuint value) => new UInt128(0, value);

        private void WriteLittleEndianUnsafe(Span<byte> destination)
        {
            Debug.Assert(destination.Length >= Size);

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
        }

        //
        // IAdditionOperators
        //

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        public static UInt128 operator +(UInt128 left, UInt128 right)
        {
            // For unsigned addition, we can detect overflow by checking `(x + y) < x`
            // This gives us the carry to add to upper to compute the correct result

            ulong lower = left._lower + right._lower;
            ulong carry = (lower < left._lower) ? 1UL : 0UL;

            ulong upper = left._upper + right._upper + carry;
            return new UInt128(upper, lower);
        }

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        public static UInt128 operator checked +(UInt128 left, UInt128 right)
        {
            // For unsigned addition, we can detect overflow by checking `(x + y) < x`
            // This gives us the carry to add to upper to compute the correct result

            ulong lower = left._lower + right._lower;
            ulong carry = (lower < left._lower) ? 1UL : 0UL;

            ulong upper = checked(left._upper + right._upper + carry);
            return new UInt128(upper, lower);
        }

        //
        // IAdditiveIdentity
        //

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static UInt128 IAdditiveIdentity<UInt128, UInt128>.AdditiveIdentity => default;

        //
        // IBinaryInteger
        //

        /// <inheritdoc cref="IBinaryInteger{TSelf}.DivRem(TSelf, TSelf)" />
        public static (UInt128 Quotient, UInt128 Remainder) DivRem(UInt128 left, UInt128 right)
        {
            UInt128 quotient = left / right;
            return (quotient, left - (quotient * right));
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.LeadingZeroCount(TSelf)" />
        public static UInt128 LeadingZeroCount(UInt128 value)
        {
            if (value._upper == 0)
            {
                return 64 + ulong.LeadingZeroCount(value._lower);
            }
            return ulong.LeadingZeroCount(value._upper);
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.PopCount(TSelf)" />
        public static UInt128 PopCount(UInt128 value)
            => ulong.PopCount(value._lower) + ulong.PopCount(value._upper);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.RotateLeft(TSelf, int)" />
        public static UInt128 RotateLeft(UInt128 value, int rotateAmount)
            => (value << rotateAmount) | (value >>> (128 - rotateAmount));

        /// <inheritdoc cref="IBinaryInteger{TSelf}.RotateRight(TSelf, int)" />
        public static UInt128 RotateRight(UInt128 value, int rotateAmount)
            => (value >>> rotateAmount) | (value << (128 - rotateAmount));

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TrailingZeroCount(TSelf)" />
        public static UInt128 TrailingZeroCount(UInt128 value)
        {
            if (value._lower == 0)
            {
                return 64 + ulong.TrailingZeroCount(value._upper);
            }
            return ulong.TrailingZeroCount(value._lower);
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.GetShortestBitLength()" />
        int IBinaryInteger<UInt128>.GetShortestBitLength()
        {
            UInt128 value = this;
            return (Size * 8) - BitOperations.LeadingZeroCount(value);
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.GetByteCount()" />
        int IBinaryInteger<UInt128>.GetByteCount() => Size;

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryWriteBigEndian(Span{byte}, out int)" />
        bool IBinaryInteger<UInt128>.TryWriteBigEndian(Span<byte> destination, out int bytesWritten)
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
        bool IBinaryInteger<UInt128>.TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= Size)
            {
                WriteLittleEndianUnsafe(destination);
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
        public static bool IsPow2(UInt128 value) => PopCount(value) == 1U;

        /// <inheritdoc cref="IBinaryNumber{TSelf}.Log2(TSelf)" />
        public static UInt128 Log2(UInt128 value)
        {
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
        public static UInt128 operator &(UInt128 left, UInt128 right) => new UInt128(left._upper & right._upper, left._lower & right._lower);

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseOr(TSelf, TOther)" />
        public static UInt128 operator |(UInt128 left, UInt128 right) => new UInt128(left._upper | right._upper, left._lower | right._lower);

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_ExclusiveOr(TSelf, TOther)" />
        public static UInt128 operator ^(UInt128 left, UInt128 right) => new UInt128(left._upper ^ right._upper, left._lower ^ right._lower);

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_OnesComplement(TSelf)" />
        public static UInt128 operator ~(UInt128 value) => new UInt128(~value._upper, ~value._lower);

        //
        // IComparisonOperators
        //

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_LessThan(TSelf, TOther)" />
        public static bool operator <(UInt128 left, UInt128 right)
        {
            return (left._upper < right._upper)
                || (left._upper == right._upper) && (left._lower < right._lower);
        }

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_LessThanOrEqual(TSelf, TOther)" />
        public static bool operator <=(UInt128 left, UInt128 right)
        {
            return (left._upper < right._upper)
                || (left._upper == right._upper) && (left._lower <= right._lower);
        }

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_GreaterThan(TSelf, TOther)" />
        public static bool operator >(UInt128 left, UInt128 right)
        {
            return (left._upper > right._upper)
                || (left._upper == right._upper) && (left._lower > right._lower);
        }

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_GreaterThanOrEqual(TSelf, TOther)" />
        public static bool operator >=(UInt128 left, UInt128 right)
        {
            return (left._upper > right._upper)
                || (left._upper == right._upper) && (left._lower >= right._lower);
        }

        //
        // IDecrementOperators
        //

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        public static UInt128 operator --(UInt128 value) => value - One;

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        public static UInt128 operator checked --(UInt128 value) => checked(value - One);

        //
        // IDivisionOperators
        //

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_Division(TSelf, TOther)" />
        public static UInt128 operator /(UInt128 left, UInt128 right)
        {
            if ((right._upper == 0) && (left._upper == 0))
            {
                // left and right are both uint64
                return left._lower / right._lower;
            }

            if (right >= left)
            {
                return (right == left) ? One : Zero;
            }

            return DivideSlow(left, right);

            static uint AddDivisor(Span<uint> left, ReadOnlySpan<uint> right)
            {
                Debug.Assert(left.Length >= right.Length);

                // Repairs the dividend, if the last subtract was too much

                ulong carry = 0UL;

                for (int i = 0; i < right.Length; i++)
                {
                    ref uint leftElement = ref left[i];
                    ulong digit = (leftElement + carry) + right[i];

                    leftElement = unchecked((uint)digit);
                    carry = digit >> 32;
                }

                return (uint)carry;
            }

            static bool DivideGuessTooBig(ulong q, ulong valHi, uint valLo, uint divHi, uint divLo)
            {
                Debug.Assert(q <= 0xFFFFFFFF);

                // We multiply the two most significant limbs of the divisor
                // with the current guess for the quotient. If those are bigger
                // than the three most significant limbs of the current dividend
                // we return true, which means the current guess is still too big.

                ulong chkHi = divHi * q;
                ulong chkLo = divLo * q;

                chkHi += (chkLo >> 32);
                chkLo = (uint)(chkLo);

                return (chkHi > valHi) || ((chkHi == valHi) && (chkLo > valLo));
            }

            unsafe static UInt128 DivideSlow(UInt128 quotient, UInt128 divisor)
            {
                // This is the same algorithm currently used by BigInteger so
                // we need to get a Span<uint> containing the value represented
                // in the least number of elements possible.

                uint* pLeft = stackalloc uint[Size / sizeof(uint)];
                quotient.WriteLittleEndianUnsafe(new Span<byte>(pLeft, Size));
                Span<uint> left = new Span<uint>(pLeft, (Size / sizeof(uint)) - (BitOperations.LeadingZeroCount(quotient) / 32));

                uint* pRight = stackalloc uint[Size / sizeof(uint)];
                divisor.WriteLittleEndianUnsafe(new Span<byte>(pRight, Size));
                Span<uint> right = new Span<uint>(pRight, (Size / sizeof(uint)) - (BitOperations.LeadingZeroCount(divisor) / 32));

                Span<uint> rawBits = stackalloc uint[Size / sizeof(uint)];
                rawBits.Clear();
                Span<uint> bits = rawBits.Slice(0, left.Length - right.Length + 1);

                Debug.Assert(left.Length >= 1);
                Debug.Assert(right.Length >= 1);
                Debug.Assert(left.Length >= right.Length);

                // Executes the "grammar-school" algorithm for computing q = a / b.
                // Before calculating q_i, we get more bits into the highest bit
                // block of the divisor. Thus, guessing digits of the quotient
                // will be more precise. Additionally we'll get r = a % b.

                uint divHi = right[right.Length - 1];
                uint divLo = right.Length > 1 ? right[right.Length - 2] : 0;

                // We measure the leading zeros of the divisor
                int shift = BitOperations.LeadingZeroCount(divHi);
                int backShift = 32 - shift;

                // And, we make sure the most significant bit is set
                if (shift > 0)
                {
                    uint divNx = right.Length > 2 ? right[right.Length - 3] : 0;

                    divHi = (divHi << shift) | (divLo >> backShift);
                    divLo = (divLo << shift) | (divNx >> backShift);
                }

                // Then, we divide all of the bits as we would do it using
                // pen and paper: guessing the next digit, subtracting, ...
                for (int i = left.Length; i >= right.Length; i--)
                {
                    int n = i - right.Length;
                    uint t = ((uint)(i) < (uint)(left.Length)) ? left[i] : 0;

                    ulong valHi = ((ulong)(t) << 32) | left[i - 1];
                    uint valLo = (i > 1) ? left[i - 2] : 0;

                    // We shifted the divisor, we shift the dividend too
                    if (shift > 0)
                    {
                        uint valNx = i > 2 ? left[i - 3] : 0;

                        valHi = (valHi << shift) | (valLo >> backShift);
                        valLo = (valLo << shift) | (valNx >> backShift);
                    }

                    // First guess for the current digit of the quotient,
                    // which naturally must have only 32 bits...
                    ulong digit = valHi / divHi;

                    if (digit > 0xFFFFFFFF)
                    {
                        digit = 0xFFFFFFFF;
                    }

                    // Our first guess may be a little bit to big
                    while (DivideGuessTooBig(digit, valHi, valLo, divHi, divLo))
                    {
                        --digit;
                    }

                    if (digit > 0)
                    {
                        // Now it's time to subtract our current quotient
                        uint carry = SubtractDivisor(left.Slice(n), right, digit);

                        if (carry != t)
                        {
                            Debug.Assert(carry == (t + 1));

                            // Our guess was still exactly one too high
                            carry = AddDivisor(left.Slice(n), right);

                            --digit;
                            Debug.Assert(carry == 1);
                        }
                    }

                    // We have the digit!
                    if ((uint)(n) < (uint)(bits.Length))
                    {
                        bits[n] = (uint)(digit);
                    }

                    if ((uint)(i) < (uint)(left.Length))
                    {
                        left[i] = 0;
                    }
                }

                return new UInt128(
                    ((ulong)(rawBits[3]) << 32) | rawBits[2],
                    ((ulong)(rawBits[1]) << 32) | rawBits[0]
                );
            }

            static uint SubtractDivisor(Span<uint> left, ReadOnlySpan<uint> right, ulong q)
            {
                Debug.Assert(left.Length >= right.Length);
                Debug.Assert(q <= 0xFFFFFFFF);

                // Combines a subtract and a multiply operation, which is naturally
                // more efficient than multiplying and then subtracting...

                ulong carry = 0UL;

                for (int i = 0; i < right.Length; i++)
                {
                    carry += right[i] * q;

                    uint digit = (uint)(carry);
                    carry >>= 32;

                    ref uint leftElement = ref left[i];

                    if (leftElement < digit)
                    {
                        ++carry;
                    }
                    leftElement -= digit;
                }

                return (uint)(carry);
            }
        }

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_CheckedDivision(TSelf, TOther)" />
        public static UInt128 operator checked /(UInt128 left, UInt128 right) => left / right;

        //
        // IEqualityOperators
        //

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther}.op_Equality(TSelf, TOther)" />
        public static bool operator ==(UInt128 left, UInt128 right) => (left._lower == right._lower) && (left._upper == right._upper);

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther}.op_Inequality(TSelf, TOther)" />
        public static bool operator !=(UInt128 left, UInt128 right) => (left._lower != right._lower) || (left._upper != right._upper);

        //
        // IIncrementOperators
        //

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_Increment(TSelf)" />
        public static UInt128 operator ++(UInt128 value) => value + One;

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_CheckedIncrement(TSelf)" />
        public static UInt128 operator checked ++(UInt128 value) => checked(value + One);

        //
        // IMinMaxValue
        //

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MinValue" />
        public static UInt128 MinValue => new UInt128(0, 0);

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MaxValue" />
        public static UInt128 MaxValue => new UInt128(0xFFFF_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF);

        //
        // IModulusOperators
        //

        /// <inheritdoc cref="IModulusOperators{TSelf, TOther, TResult}.op_Modulus(TSelf, TOther)" />
        public static UInt128 operator %(UInt128 left, UInt128 right)
        {
            UInt128 quotient = left / right;
            return left - (quotient * right);
        }

        //
        // IMultiplicativeIdentity
        //

        /// <inheritdoc cref="IMultiplicativeIdentity{TSelf, TResult}.MultiplicativeIdentity" />
        static UInt128 IMultiplicativeIdentity<UInt128, UInt128>.MultiplicativeIdentity => One;

        //
        // IMultiplyOperators
        //

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_Multiply(TSelf, TOther)" />
        public static UInt128 operator *(UInt128 left, UInt128 right)
        {
            ulong upper = Math.BigMul(left._lower, right._lower, out ulong lower);
            upper += (left._upper * right._lower) + (left._lower * right._upper);
            return new UInt128(upper, lower);
        }

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_CheckedMultiply(TSelf, TOther)" />
        public static UInt128 operator checked *(UInt128 left, UInt128 right)
        {
            UInt128 upper = BigMul(left, right, out UInt128 lower);

            if (upper != 0U)
            {
                ThrowHelper.ThrowOverflowException();
            }

            return lower;
        }

        internal static UInt128 BigMul(UInt128 left, UInt128 right, out UInt128 lower)
        {
            // Adaptation of algorithm for multiplication
            // of 32-bit unsigned integers described
            // in Hacker's Delight by Henry S. Warren, Jr. (ISBN 0-201-91465-4), Chapter 8
            // Basically, it's an optimized version of FOIL method applied to
            // low and high qwords of each operand

            UInt128 al = left._lower;
            UInt128 ah = left._upper;

            UInt128 bl = right._lower;
            UInt128 bh = right._upper;

            UInt128 mull = al * bl;
            UInt128 t = ah * bl + mull._upper;
            UInt128 tl = al * bh + t._lower;

            lower = new UInt128(tl._lower, mull._lower);
            return ah * bh + t._upper + tl._upper;
        }

        //
        // INumber
        //

        /// <inheritdoc cref="INumber{TSelf}.Abs(TSelf)" />
        static UInt128 INumber<UInt128>.Abs(UInt128 value) => value;

        /// <inheritdoc cref="INumber{TSelf}.Clamp(TSelf, TSelf, TSelf)" />
        public static UInt128 Clamp(UInt128 value, UInt128 min, UInt128 max)
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
        static UInt128 INumber<UInt128>.CopySign(UInt128 value, UInt128 sign) => value;

        /// <inheritdoc cref="INumber{TSelf}.CreateChecked{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt128 CreateChecked<TOther>(TOther value)
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
                return checked((UInt128)(decimal)(object)value);
            }
            else if (typeof(TOther) == typeof(double))
            {
                return checked((UInt128)(double)(object)value);
            }
            else if (typeof(TOther) == typeof(Half))
            {
                return checked((UInt128)(Half)(object)value);
            }
            else if (typeof(TOther) == typeof(short))
            {
                return checked((UInt128)(short)(object)value);
            }
            else if (typeof(TOther) == typeof(int))
            {
                return checked((UInt128)(int)(object)value);
            }
            else if (typeof(TOther) == typeof(long))
            {
                return checked((UInt128)(long)(object)value);
            }
            else if (typeof(TOther) == typeof(nint))
            {
                return checked((UInt128)(nint)(object)value);
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                return checked((UInt128)(sbyte)(object)value);
            }
            else if (typeof(TOther) == typeof(float))
            {
                return checked((UInt128)(float)(object)value);
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
        public static UInt128 CreateSaturating<TOther>(TOther value)
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
                return (actualValue < 0) ? MinValue : (UInt128)actualValue;
            }
            else if (typeof(TOther) == typeof(double))
            {
                return (UInt128)(double)(object)value;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                return (UInt128)(Half)(object)value;
            }
            else if (typeof(TOther) == typeof(short))
            {
                var actualValue = (short)(object)value;
                return (actualValue < 0) ? MinValue : (UInt128)actualValue;
            }
            else if (typeof(TOther) == typeof(int))
            {
                var actualValue = (int)(object)value;
                return (actualValue < 0) ? MinValue : (UInt128)actualValue;
            }
            else if (typeof(TOther) == typeof(long))
            {
                var actualValue = (long)(object)value;
                return (actualValue < 0) ? MinValue : (UInt128)actualValue;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                var actualValue = (nint)(object)value;
                return (actualValue < 0) ? MinValue : (UInt128)actualValue;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                var actualValue = (sbyte)(object)value;
                return (actualValue < 0) ? MinValue : (UInt128)actualValue;
            }
            else if (typeof(TOther) == typeof(float))
            {
                return (UInt128)(float)(object)value;
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
        public static UInt128 CreateTruncating<TOther>(TOther value)
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
                return (actualValue < 0) ? MinValue : (UInt128)actualValue;
            }
            else if (typeof(TOther) == typeof(double))
            {
                return (UInt128)(double)(object)value;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                return (UInt128)(Half)(object)value;
            }
            else if (typeof(TOther) == typeof(short))
            {
                return (UInt128)(short)(object)value;
            }
            else if (typeof(TOther) == typeof(int))
            {
                return (UInt128)(int)(object)value;
            }
            else if (typeof(TOther) == typeof(long))
            {
                return (UInt128)(long)(object)value;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                return (UInt128)(nint)(object)value;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                return (UInt128)(sbyte)(object)value;
            }
            else if (typeof(TOther) == typeof(float))
            {
                return (UInt128)(float)(object)value;
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
        static bool INumber<UInt128>.IsNegative(UInt128 value) => false;

        /// <inheritdoc cref="INumber{TSelf}.Max(TSelf, TSelf)" />
        public static UInt128 Max(UInt128 x, UInt128 y) => (x >= y) ? x : y;

        /// <inheritdoc cref="INumber{TSelf}.MaxMagnitude(TSelf, TSelf)" />
        static UInt128 INumber<UInt128>.MaxMagnitude(UInt128 x, UInt128 y) => Max(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Min(TSelf, TSelf)" />
        public static UInt128 Min(UInt128 x, UInt128 y) => (x <= y) ? x : y;

        /// <inheritdoc cref="INumber{TSelf}.MinMagnitude(TSelf, TSelf)" />
        static UInt128 INumber<UInt128>.MinMagnitude(UInt128 x, UInt128 y) => Min(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Sign(TSelf)" />
        public static int Sign(UInt128 value) => (value == 0U) ? 0 : 1;

        /// <inheritdoc cref="INumber{TSelf}.TryCreate{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryCreate<TOther>(TOther value, out UInt128 result)
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

                if (actualValue < 0.0m)
                {
                    result = default;
                    return false;
                }

                result = (UInt128)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                var actualValue = (double)(object)value;

                if ((actualValue < 0.0) || (actualValue >= +340282366920938463463374607431768211456.0) || double.IsNaN(actualValue))
                {
                    result = default;
                    return false;
                }

                result = (UInt128)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                var actualValue = (Half)(object)value;

                if ((actualValue < Half.Zero) || (actualValue > Half.MaxValue) || Half.IsNaN(actualValue))
                {
                    result = default;
                    return false;
                }

                result = (UInt128)actualValue;
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

                result = (UInt128)actualValue;
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

                result = (UInt128)actualValue;
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

                result = (UInt128)actualValue;
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

                result = (UInt128)actualValue;
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

                result = (UInt128)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                var actualValue = (float)(object)value;

                if ((actualValue < 0.0f) || (actualValue > float.MaxValue) || float.IsNaN(actualValue))
                {
                    result = default;
                    return false;
                }

                result = (UInt128)actualValue;
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
        public static UInt128 One => new UInt128(0, 1);

        /// <inheritdoc cref="INumberBase{TSelf}.Zero" />
        public static UInt128 Zero => default;

        //
        // IParsable
        //

        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out UInt128 result) => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // IShiftOperators
        //

        /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_LeftShift(TSelf, int)" />
        public static UInt128 operator <<(UInt128 value, int shiftAmount)
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
                return new UInt128(upper, 0);
            }
            else if (shiftAmount != 0)
            {
                // Otherwise we need to shift both upper and lower halves by the masked
                // amount and then or that with whatever bits were shifted "out" of lower

                ulong lower = value._lower << shiftAmount;
                ulong upper = (value._upper << shiftAmount) | (value._lower >> (64 - shiftAmount));

                return new UInt128(upper, lower);
            }
            else
            {
                return value;
            }
        }

        /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_RightShift(TSelf, int)" />
        public static UInt128 operator >>(UInt128 value, int shiftAmount) => value >>> shiftAmount;

        /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_UnsignedRightShift(TSelf, int)" />
        public static UInt128 operator >>>(UInt128 value, int shiftAmount)
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
                return new UInt128(0, lower);
            }
            else if (shiftAmount != 0)
            {
                // Otherwise we need to shift both upper and lower halves by the masked
                // amount and then or that with whatever bits were shifted "out" of upper

                ulong lower = (value._lower >> shiftAmount) | (value._upper << (64 - shiftAmount));
                ulong upper = value._upper >> shiftAmount;

                return new UInt128(upper, lower);
            }
            else
            {
                return value;
            }
        }

        //
        // ISpanParsable
        //

        /// <inheritdoc cref="ISpanParsable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static UInt128 Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Integer, provider);

        /// <inheritdoc cref="ISpanParsable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out UInt128 result) => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // ISubtractionOperators
        //

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_Subtraction(TSelf, TOther)" />
        public static UInt128 operator -(UInt128 left, UInt128 right)
        {
            // For unsigned subtract, we can detect overflow by checking `(x - y) > x`
            // This gives us the borrow to subtract from upper to compute the correct result

            ulong lower = left._lower - right._lower;
            ulong borrow = (lower > left._lower) ? 1UL : 0UL;

            ulong upper = left._upper - right._upper - borrow;
            return new UInt128(upper, lower);
        }

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_CheckedSubtraction(TSelf, TOther)" />
        public static UInt128 operator checked -(UInt128 left, UInt128 right)
        {
            // For unsigned subtract, we can detect overflow by checking `(x - y) > x`
            // This gives us the borrow to subtract from upper to compute the correct result

            ulong lower = left._lower - right._lower;
            ulong borrow = (lower > left._lower) ? 1UL : 0UL;

            ulong upper = checked(left._upper - right._upper - borrow);
            return new UInt128(upper, lower);
        }

        //
        // IUnaryNegationOperators
        //

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_UnaryNegation(TSelf)" />
        public static UInt128 operator -(UInt128 value) => Zero - value;

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_CheckedUnaryNegation(TSelf)" />
        public static UInt128 operator checked -(UInt128 value) => checked(Zero - value);

        //
        // IUnaryPlusOperators
        //

        /// <inheritdoc cref="IUnaryPlusOperators{TSelf, TResult}.op_UnaryPlus(TSelf)" />
        public static UInt128 operator +(UInt128 value) => value;
    }
}
