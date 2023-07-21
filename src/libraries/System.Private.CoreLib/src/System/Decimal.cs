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

namespace System
{
    // Implements the Decimal data type. The Decimal data type can
    // represent values ranging from -79,228,162,514,264,337,593,543,950,335 to
    // 79,228,162,514,264,337,593,543,950,335 with 28 significant digits. The
    // Decimal data type is ideally suited to financial calculations that
    // require a large number of significant digits and no round-off errors.
    //
    // The finite set of values of type Decimal are of the form m
    // / 10e, where m is an integer such that
    // -296 <; m <; 296, and e is an integer
    // between 0 and 28 inclusive.
    //
    // Contrary to the float and double data types, decimal
    // fractional numbers such as 0.1 can be represented exactly in the
    // Decimal representation. In the float and double
    // representations, such numbers are often infinite fractions, making those
    // representations more prone to round-off errors.
    //
    // The Decimal class implements widening conversions from the
    // ubyte, char, short, int, and long types
    // to Decimal. These widening conversions never lose any information
    // and never throw exceptions. The Decimal class also implements
    // narrowing conversions from Decimal to ubyte, char,
    // short, int, and long. These narrowing conversions round
    // the Decimal value towards zero to the nearest integer, and then
    // converts that integer to the destination type. An OverflowException
    // is thrown if the result is not within the range of the destination type.
    //
    // The Decimal class provides a widening conversion from
    // Currency to Decimal. This widening conversion never loses any
    // information and never throws exceptions. The Currency class provides
    // a narrowing conversion from Decimal to Currency. This
    // narrowing conversion rounds the Decimal to four decimals and then
    // converts that number to a Currency. An OverflowException
    // is thrown if the result is not within the range of the Currency type.
    //
    // The Decimal class provides narrowing conversions to and from the
    // float and double types. A conversion from Decimal to
    // float or double may lose precision, but will not lose
    // information about the overall magnitude of the numeric value, and will never
    // throw an exception. A conversion from float or double to
    // Decimal throws an OverflowException if the value is not within
    // the range of the Decimal type.
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [NonVersionable] // This only applies to field layout
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public readonly partial struct Decimal
        : ISpanFormattable,
          IComparable,
          IConvertible,
          IComparable<decimal>,
          IEquatable<decimal>,
          ISerializable,
          IDeserializationCallback,
          IFloatingPoint<decimal>,
          IMinMaxValue<decimal>,
          IUtf8SpanFormattable
    {
        // Sign mask for the flags field. A value of zero in this bit indicates a
        // positive Decimal value, and a value of one in this bit indicates a
        // negative Decimal value.
        //
        // Look at OleAut's DECIMAL_NEG constant to check for negative values
        // in native code.
        private const int SignMask = unchecked((int)0x80000000);

        // Scale mask for the flags field. This byte in the flags field contains
        // the power of 10 to divide the Decimal value by. The scale byte must
        // contain a value between 0 and 28 inclusive.
        private const int ScaleMask = 0x00FF0000;

        // Number of bits scale is shifted by.
        private const int ScaleShift = 16;

        // Constant representing the Decimal value 0.
        public const decimal Zero = 0m;

        // Constant representing the Decimal value 1.
        public const decimal One = 1m;

        // Constant representing the Decimal value -1.
        public const decimal MinusOne = -1m;

        // Constant representing the largest possible Decimal value. The value of
        // this constant is 79,228,162,514,264,337,593,543,950,335.
        public const decimal MaxValue = 79228162514264337593543950335m;

        // Constant representing the smallest possible Decimal value. The value of
        // this constant is -79,228,162,514,264,337,593,543,950,335.
        public const decimal MinValue = -79228162514264337593543950335m;

        /// <summary>Represents the additive identity (0).</summary>
        private const decimal AdditiveIdentity = 0m;

        /// <summary>Represents the multiplicative identity (1).</summary>
        private const decimal MultiplicativeIdentity = 1m;

        /// <summary>Represents the number negative one (-1).</summary>
        private const decimal NegativeOne = -1m;

        // The lo, mid, hi, and flags fields contain the representation of the
        // Decimal value. The lo, mid, and hi fields contain the 96-bit integer
        // part of the Decimal. Bits 0-15 (the lower word) of the flags field are
        // unused and must be zero; bits 16-23 contain must contain a value between
        // 0 and 28, indicating the power of 10 to divide the 96-bit integer part
        // by to produce the Decimal value; bits 24-30 are unused and must be zero;
        // and finally bit 31 indicates the sign of the Decimal value, 0 meaning
        // positive and 1 meaning negative.
        //
        // NOTE: Do not change the order and types of these fields. The layout has to
        // match Win32 DECIMAL type.
        private readonly int _flags;
        private readonly uint _hi32;
        private readonly ulong _lo64;

        // Constructs a Decimal from an integer value.
        //
        public Decimal(int value)
        {
            if (value >= 0)
            {
                _flags = 0;
            }
            else
            {
                _flags = SignMask;
                value = -value;
            }
            _lo64 = (uint)value;
            _hi32 = 0;
        }

        // Constructs a Decimal from an unsigned integer value.
        //
        [CLSCompliant(false)]
        public Decimal(uint value)
        {
            _flags = 0;
            _lo64 = value;
            _hi32 = 0;
        }

        // Constructs a Decimal from a long value.
        //
        public Decimal(long value)
        {
            if (value >= 0)
            {
                _flags = 0;
            }
            else
            {
                _flags = SignMask;
                value = -value;
            }
            _lo64 = (ulong)value;
            _hi32 = 0;
        }

        // Constructs a Decimal from an unsigned long value.
        //
        [CLSCompliant(false)]
        public Decimal(ulong value)
        {
            _flags = 0;
            _lo64 = value;
            _hi32 = 0;
        }

        // Constructs a Decimal from a float value.
        //
        public Decimal(float value)
        {
            DecCalc.VarDecFromR4(value, out AsMutable(ref this));
        }

        // Constructs a Decimal from a double value.
        //
        public Decimal(double value)
        {
            DecCalc.VarDecFromR8(value, out AsMutable(ref this));
        }

        private Decimal(SerializationInfo info, StreamingContext context)
        {
            ArgumentNullException.ThrowIfNull(info);

            _flags = info.GetInt32("flags");
            _hi32 = (uint)info.GetInt32("hi");
            _lo64 = (uint)info.GetInt32("lo") + ((ulong)info.GetInt32("mid") << 32);
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            ArgumentNullException.ThrowIfNull(info);

            // Serialize both the old and the new format
            info.AddValue("flags", _flags);
            info.AddValue("hi", (int)High);
            info.AddValue("lo", (int)Low);
            info.AddValue("mid", (int)Mid);
        }

        //
        // Decimal <==> Currency conversion.
        //
        // A Currency represents a positive or negative decimal value with 4 digits past the decimal point. The actual Int64 representation used by these methods
        // is the currency value multiplied by 10,000. For example, a currency value of $12.99 would be represented by the Int64 value 129,900.
        //
        public static decimal FromOACurrency(long cy)
        {
            ulong absoluteCy; // has to be ulong to accommodate the case where cy == long.MinValue.
            bool isNegative = false;
            if (cy < 0)
            {
                isNegative = true;
                absoluteCy = (ulong)(-cy);
            }
            else
            {
                absoluteCy = (ulong)cy;
            }

            // In most cases, FromOACurrency() produces a Decimal with Scale set to 4. Unless, that is, some of the trailing digits past the decimal point are zero,
            // in which case, for compatibility with .Net, we reduce the Scale by the number of zeros. While the result is still numerically equivalent, the scale does
            // affect the ToString() value. In particular, it prevents a converted currency value of $12.95 from printing uglily as "12.9500".
            int scale = 4;
            if (absoluteCy != 0)  // For compatibility, a currency of 0 emits the Decimal "0.0000" (scale set to 4).
            {
                while (scale != 0 && ((absoluteCy % 10) == 0))
                {
                    scale--;
                    absoluteCy /= 10;
                }
            }

            return new decimal((int)absoluteCy, (int)(absoluteCy >> 32), 0, isNegative, (byte)scale);
        }

        public static long ToOACurrency(decimal value)
        {
            return DecCalc.VarCyFromDec(ref AsMutable(ref value));
        }

        private static bool IsValid(int flags) => (flags & ~(SignMask | ScaleMask)) == 0 && ((uint)(flags & ScaleMask) <= (28 << ScaleShift));

        // Constructs a Decimal from an integer array containing a binary
        // representation. The bits argument must be a non-null integer
        // array with four elements. bits[0], bits[1], and
        // bits[2] contain the low, middle, and high 32 bits of the 96-bit
        // integer part of the Decimal. bits[3] contains the scale factor
        // and sign of the Decimal: bits 0-15 (the lower word) are unused and must
        // be zero; bits 16-23 must contain a value between 0 and 28, indicating
        // the power of 10 to divide the 96-bit integer part by to produce the
        // Decimal value; bits 24-30 are unused and must be zero; and finally bit
        // 31 indicates the sign of the Decimal value, 0 meaning positive and 1
        // meaning negative.
        //
        // Note that there are several possible binary representations for the
        // same numeric value. For example, the value 1 can be represented as {1,
        // 0, 0, 0} (integer value 1 with a scale factor of 0) and equally well as
        // {1000, 0, 0, 0x30000} (integer value 1000 with a scale factor of 3).
        // The possible binary representations of a particular value are all
        // equally valid, and all are numerically equivalent.
        //
        public Decimal(int[] bits) :
            this((ReadOnlySpan<int>)(bits ?? throw new ArgumentNullException(nameof(bits))))
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="decimal"/> to a decimal value represented in binary and contained in the specified span.
        /// </summary>
        /// <param name="bits">A span of four <see cref="int"/>s containing a binary representation of a decimal value.</param>
        /// <exception cref="ArgumentException">The length of <paramref name="bits"/> is not 4, or the representation of the decimal value in <paramref name="bits"/> is not valid.</exception>
        public Decimal(ReadOnlySpan<int> bits)
        {
            if (bits.Length == 4)
            {
                int f = bits[3];
                if (IsValid(f))
                {
                    _lo64 = (uint)bits[0] + ((ulong)(uint)bits[1] << 32);
                    _hi32 = (uint)bits[2];
                    _flags = f;
                    return;
                }
            }
            throw new ArgumentException(SR.Arg_DecBitCtor);
        }

        // Constructs a Decimal from its constituent parts.
        //
        public Decimal(int lo, int mid, int hi, bool isNegative, byte scale)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(scale, 28);
            _lo64 = (uint)lo + ((ulong)(uint)mid << 32);
            _hi32 = (uint)hi;
            _flags = ((int)scale) << 16;
            if (isNegative)
                _flags |= SignMask;
        }

        void IDeserializationCallback.OnDeserialization(object? sender)
        {
            // OnDeserialization is called after each instance of this class is deserialized.
            // This callback method performs decimal validation after being deserialized.
            if (!IsValid(_flags))
                throw new SerializationException(SR.Overflow_Decimal);
        }

        // Constructs a Decimal from its constituent parts.
        private Decimal(int lo, int mid, int hi, int flags)
        {
            if (IsValid(flags))
            {
                _lo64 = (uint)lo + ((ulong)(uint)mid << 32);
                _hi32 = (uint)hi;
                _flags = flags;
                return;
            }
            throw new ArgumentException(SR.Arg_DecBitCtor);
        }

        private Decimal(in decimal d, int flags)
        {
            this = d;
            _flags = flags;
        }

        /// <summary>
        /// Gets the scaling factor of the decimal, which is a number from 0 to 28 that represents the number of decimal digits.
        /// </summary>
        public byte Scale => (byte)(_flags >> ScaleShift);

        private sbyte Exponent
        {
            get
            {
                // Decimal tracks its exponent as a scale between 0 and 28. This scale is used
                // with the significand as `significand / 10^scale`
                //
                // The IFloatingPoint contract however follows the general IEEE 754 algorithm
                // which is `-1^s * b^e * (b^(1-p) * m)`
                //
                // In this algorithm
                // * `s` is the sign
                // * `b` is the radix (10 for decimal)
                // * `e` is the exponent
                // * `p` is the number of bits in the significand
                // * `m` is the significand itself
                //
                // For a value such as decimal.MaxValue, the significand is 79228162514264337593543950335
                // and the scale is 0. Since decimal tracks 96 significand bits, the required algorithm (simplified)
                // gives us 7.9228162514264337593543950335 * 10^-67 * 10^e. To get back to our original value we
                // then need the exponent to be 95.
                //
                // For a value such as 1E-28, the significand is 1 and the scale is 28. The required algorithm (simplified)
                // gives us 1.0 * 10^-95 * 10^e. To get back to our original value we need the exponent to be 67.
                //
                // Given that scale is bound by 0 and 28, inclusive, the returned exponent will be between 95
                // and 67, inclusive. That is between `(p - 1)` and `(p - 1) - MaxScale`.
                //
                // The generalized algorithm for converting from scale to exponent is then `exponent = 95 - scale`.

                sbyte exponent = (sbyte)(95 - Scale);
                Debug.Assert((exponent >= 67) && (exponent <= 95));
                return exponent;
            }
        }

        // Adds two Decimal values.
        //
        public static decimal Add(decimal d1, decimal d2)
        {
            DecCalc.DecAddSub(ref AsMutable(ref d1), ref AsMutable(ref d2), false);
            return d1;
        }

        // Rounds a Decimal to an integer value. The Decimal argument is rounded
        // towards positive infinity.
        public static decimal Ceiling(decimal d)
        {
            int flags = d._flags;
            if ((flags & ScaleMask) != 0)
                DecCalc.InternalRound(ref AsMutable(ref d), (byte)(flags >> ScaleShift), MidpointRounding.ToPositiveInfinity);
            return d;
        }

        // Compares two Decimal values, returning an integer that indicates their
        // relationship.
        //
        public static int Compare(decimal d1, decimal d2)
        {
            return DecCalc.VarDecCmp(in d1, in d2);
        }

        // Compares this object to another object, returning an integer that
        // indicates the relationship.
        // Returns a value less than zero if this  object
        // null is considered to be less than any instance.
        // If object is not of type Decimal, this method throws an ArgumentException.
        //
        public int CompareTo(object? value)
        {
            if (value == null)
                return 1;
            if (!(value is decimal))
                throw new ArgumentException(SR.Arg_MustBeDecimal);

            decimal other = (decimal)value;
            return DecCalc.VarDecCmp(in this, in other);
        }

        public int CompareTo(decimal value)
        {
            return DecCalc.VarDecCmp(in this, in value);
        }

        // Divides two Decimal values.
        //
        public static decimal Divide(decimal d1, decimal d2)
        {
            DecCalc.VarDecDiv(ref AsMutable(ref d1), ref AsMutable(ref d2));
            return d1;
        }

        // Checks if this Decimal is equal to a given object. Returns true
        // if the given object is a boxed Decimal and its value is equal to the
        // value of this Decimal. Returns false otherwise.
        //
        public override bool Equals([NotNullWhen(true)] object? value) =>
            value is decimal other &&
            DecCalc.VarDecCmp(in this, in other) == 0;

        public bool Equals(decimal value) =>
            DecCalc.VarDecCmp(in this, in value) == 0;

        // Returns the hash code for this Decimal.
        //
        public override int GetHashCode() => DecCalc.GetHashCode(in this);

        // Compares two Decimal values for equality. Returns true if the two
        // Decimal values are equal, or false if they are not equal.
        //
        public static bool Equals(decimal d1, decimal d2)
        {
            return DecCalc.VarDecCmp(in d1, in d2) == 0;
        }

        // Rounds a Decimal to an integer value. The Decimal argument is rounded
        // towards negative infinity.
        //
        public static decimal Floor(decimal d)
        {
            int flags = d._flags;
            if ((flags & ScaleMask) != 0)
                DecCalc.InternalRound(ref AsMutable(ref d), (byte)(flags >> ScaleShift), MidpointRounding.ToNegativeInfinity);
            return d;
        }

        // Converts this Decimal to a string. The resulting string consists of an
        // optional minus sign ("-") followed to a sequence of digits ("0" - "9"),
        // optionally followed by a decimal point (".") and another sequence of
        // digits.
        //
        public override string ToString()
        {
            return Number.FormatDecimal(this, null, NumberFormatInfo.CurrentInfo);
        }

        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
        {
            return Number.FormatDecimal(this, format, NumberFormatInfo.CurrentInfo);
        }

        public string ToString(IFormatProvider? provider)
        {
            return Number.FormatDecimal(this, null, NumberFormatInfo.GetInstance(provider));
        }

        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider)
        {
            return Number.FormatDecimal(this, format, NumberFormatInfo.GetInstance(provider));
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatDecimal(this, format, NumberFormatInfo.GetInstance(provider), destination, out charsWritten);
        }

        /// <inheritdoc cref="IUtf8SpanFormattable.TryFormat" />
        public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatDecimal(this, format, NumberFormatInfo.GetInstance(provider), utf8Destination, out bytesWritten);
        }

        // Converts a string to a Decimal. The string must consist of an optional
        // minus sign ("-") followed by a sequence of digits ("0" - "9"). The
        // sequence of digits may optionally contain a single decimal point (".")
        // character. Leading and trailing whitespace characters are allowed.
        // Parse also allows a currency symbol, a trailing negative sign, and
        // parentheses in the number.
        //
        public static decimal Parse(string s) => Parse(s, NumberStyles.Number, provider: null);

        public static decimal Parse(string s, NumberStyles style) => Parse(s, style, provider: null);

        public static decimal Parse(string s, IFormatProvider? provider) => Parse(s, NumberStyles.Number, provider);

        public static decimal Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            if (s is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            return Parse(s.AsSpan(), style, provider);
        }

        public static decimal Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Number, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Number.ParseDecimal(s, style, NumberFormatInfo.GetInstance(provider));
        }

        public static bool TryParse([NotNullWhen(true)] string? s, out decimal result) => TryParse(s, NumberStyles.Number, provider: null, out result);

        public static bool TryParse(ReadOnlySpan<char> s, out decimal result) => TryParse(s, NumberStyles.Number, provider: null, out result);

        /// <summary>Tries to convert a UTF-8 character span containing the string representation of a number to its signed decimal equivalent.</summary>
        /// <param name="utf8Text">A span containing the UTF-8 characters representing the number to convert.</param>
        /// <param name="result">When this method returns, contains the signed decimal value equivalent to the number contained in <paramref name="utf8Text" /> if the conversion succeeded, or zero if the conversion failed. This parameter is passed uninitialized; any value originally supplied in result will be overwritten.</param>
        /// <returns><c>true</c> if <paramref name="utf8Text" /> was converted successfully; otherwise, false.</returns>
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, out decimal result) => TryParse(utf8Text, NumberStyles.Number, provider: null, out result);

        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out decimal result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);

            if (s == null)
            {
                result = 0;
                return false;
            }
            return Number.TryParseDecimal(s.AsSpan(), style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
        }

        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out decimal result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Number.TryParseDecimal(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
        }

        // Returns a binary representation of a Decimal. The return value is an
        // integer array with four elements. Elements 0, 1, and 2 contain the low,
        // middle, and high 32 bits of the 96-bit integer part of the Decimal.
        // Element 3 contains the scale factor and sign of the Decimal: bits 0-15
        // (the lower word) are unused; bits 16-23 contain a value between 0 and
        // 28, indicating the power of 10 to divide the 96-bit integer part by to
        // produce the Decimal value; bits 24-30 are unused; and finally bit 31
        // indicates the sign of the Decimal value, 0 meaning positive and 1
        // meaning negative.
        //
        public static int[] GetBits(decimal d)
        {
            return new int[] { (int)d.Low, (int)d.Mid, (int)d.High, d._flags };
        }

        /// <summary>
        /// Converts the value of a specified instance of <see cref="decimal"/> to its equivalent binary representation.
        /// </summary>
        /// <param name="d">The value to convert.</param>
        /// <param name="destination">The span into which to store the four-integer binary representation.</param>
        /// <returns>Four, the number of integers in the binary representation.</returns>
        /// <exception cref="ArgumentException">The destination span was not long enough to store the binary representation.</exception>
        public static int GetBits(decimal d, Span<int> destination)
        {
            if (destination.Length <= 3)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            destination[0] = (int)d.Low;
            destination[1] = (int)d.Mid;
            destination[2] = (int)d.High;
            destination[3] = d._flags;
            return 4;
        }

        /// <summary>
        /// Tries to convert the value of a specified instance of <see cref="decimal"/> to its equivalent binary representation.
        /// </summary>
        /// <param name="d">The value to convert.</param>
        /// <param name="destination">The span into which to store the binary representation.</param>
        /// <param name="valuesWritten">The number of integers written to the destination.</param>
        /// <returns>true if the decimal's binary representation was written to the destination; false if the destination wasn't long enough.</returns>
        public static bool TryGetBits(decimal d, Span<int> destination, out int valuesWritten)
        {
            if (destination.Length <= 3)
            {
                valuesWritten = 0;
                return false;
            }

            destination[0] = (int)d.Low;
            destination[1] = (int)d.Mid;
            destination[2] = (int)d.High;
            destination[3] = d._flags;
            valuesWritten = 4;
            return true;
        }

        internal static void GetBytes(in decimal d, Span<byte> buffer)
        {
            Debug.Assert(buffer.Length >= 16, "buffer.Length >= 16");

            BinaryPrimitives.WriteInt32LittleEndian(buffer, (int)d.Low);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(4), (int)d.Mid);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(8), (int)d.High);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(12), d._flags);
        }

        internal static decimal ToDecimal(ReadOnlySpan<byte> span)
        {
            Debug.Assert(span.Length >= 16, "span.Length >= 16");
            int lo = BinaryPrimitives.ReadInt32LittleEndian(span);
            int mid = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(4));
            int hi = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(8));
            int flags = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(12));
            return new decimal(lo, mid, hi, flags);
        }

        public static decimal Remainder(decimal d1, decimal d2)
        {
            DecCalc.VarDecMod(ref AsMutable(ref d1), ref AsMutable(ref d2));
            return d1;
        }

        // Multiplies two Decimal values.
        //
        public static decimal Multiply(decimal d1, decimal d2)
        {
            DecCalc.VarDecMul(ref AsMutable(ref d1), ref AsMutable(ref d2));
            return d1;
        }

        // Returns the negated value of the given Decimal. If d is non-zero,
        // the result is -d. If d is zero, the result is zero.
        //
        public static decimal Negate(decimal d)
        {
            return new decimal(in d, d._flags ^ SignMask);
        }

        // Rounds a Decimal value to a given number of decimal places. The value
        // given by d is rounded to the number of decimal places given by
        // decimals. The decimals argument must be an integer between
        // 0 and 28 inclusive.
        //
        // By default a mid-point value is rounded to the nearest even number. If the mode is
        // passed in, it can also round away from zero.

        public static decimal Round(decimal d) => Round(ref d, 0, MidpointRounding.ToEven);
        public static decimal Round(decimal d, int decimals) => Round(ref d, decimals, MidpointRounding.ToEven);
        public static decimal Round(decimal d, MidpointRounding mode) => Round(ref d, 0, mode);
        public static decimal Round(decimal d, int decimals, MidpointRounding mode) => Round(ref d, decimals, mode);

        private static decimal Round(ref decimal d, int decimals, MidpointRounding mode)
        {
            if ((uint)decimals > 28)
                throw new ArgumentOutOfRangeException(nameof(decimals), SR.ArgumentOutOfRange_DecimalRound);
            if ((uint)mode > (uint)MidpointRounding.ToPositiveInfinity)
                throw new ArgumentException(SR.Format(SR.Argument_InvalidEnumValue, mode, nameof(MidpointRounding)), nameof(mode));

            int scale = d.Scale - decimals;
            if (scale > 0)
                DecCalc.InternalRound(ref AsMutable(ref d), (uint)scale, mode);
            return d;
        }

        // Subtracts two Decimal values.
        //
        public static decimal Subtract(decimal d1, decimal d2)
        {
            DecCalc.DecAddSub(ref AsMutable(ref d1), ref AsMutable(ref d2), true);
            return d1;
        }

        // Converts a Decimal to an unsigned byte. The Decimal value is rounded
        // towards zero to the nearest integer value, and the result of this
        // operation is returned as a byte.
        //
        public static byte ToByte(decimal value)
        {
            uint temp;
            try
            {
                temp = ToUInt32(value);
            }
            catch (OverflowException)
            {
                Number.ThrowOverflowException<byte>();
                throw;
            }
            if (temp != (byte)temp) Number.ThrowOverflowException<byte>();
            return (byte)temp;
        }

        // Converts a Decimal to a signed byte. The Decimal value is rounded
        // towards zero to the nearest integer value, and the result of this
        // operation is returned as a byte.
        //
        [CLSCompliant(false)]
        public static sbyte ToSByte(decimal value)
        {
            int temp;
            try
            {
                temp = ToInt32(value);
            }
            catch (OverflowException)
            {
                Number.ThrowOverflowException<sbyte>();
                throw;
            }
            if (temp != (sbyte)temp) Number.ThrowOverflowException<sbyte>();
            return (sbyte)temp;
        }

        // Converts a Decimal to a short. The Decimal value is
        // rounded towards zero to the nearest integer value, and the result of
        // this operation is returned as a short.
        //
        public static short ToInt16(decimal value)
        {
            int temp;
            try
            {
                temp = ToInt32(value);
            }
            catch (OverflowException)
            {
                Number.ThrowOverflowException<short>();
                throw;
            }
            if (temp != (short)temp) Number.ThrowOverflowException<short>();
            return (short)temp;
        }

        // Converts a Decimal to a double. Since a double has fewer significant
        // digits than a Decimal, this operation may produce round-off errors.
        //
        public static double ToDouble(decimal d)
        {
            return DecCalc.VarR8FromDec(in d);
        }

        // Converts a Decimal to an integer. The Decimal value is rounded towards
        // zero to the nearest integer value, and the result of this operation is
        // returned as an integer.
        //
        public static int ToInt32(decimal d)
        {
            Truncate(ref d);
            if ((d.High | d.Mid) == 0)
            {
                int i = (int)d.Low;
                if (!IsNegative(d))
                {
                    if (i >= 0) return i;
                }
                else
                {
                    i = -i;
                    if (i <= 0) return i;
                }
            }
            throw new OverflowException(SR.Overflow_Int32);
        }

        // Converts a Decimal to a long. The Decimal value is rounded towards zero
        // to the nearest integer value, and the result of this operation is
        // returned as a long.
        //
        public static long ToInt64(decimal d)
        {
            Truncate(ref d);
            if (d.High == 0)
            {
                long l = (long)d.Low64;
                if (!IsNegative(d))
                {
                    if (l >= 0) return l;
                }
                else
                {
                    l = -l;
                    if (l <= 0) return l;
                }
            }
            throw new OverflowException(SR.Overflow_Int64);
        }

        // Converts a Decimal to an ushort. The Decimal
        // value is rounded towards zero to the nearest integer value, and the
        // result of this operation is returned as an ushort.
        //
        [CLSCompliant(false)]
        public static ushort ToUInt16(decimal value)
        {
            uint temp;
            try
            {
                temp = ToUInt32(value);
            }
            catch (OverflowException)
            {
                Number.ThrowOverflowException<ushort>();
                throw;
            }
            if (temp != (ushort)temp) Number.ThrowOverflowException<ushort>();
            return (ushort)temp;
        }

        // Converts a Decimal to an unsigned integer. The Decimal
        // value is rounded towards zero to the nearest integer value, and the
        // result of this operation is returned as an unsigned integer.
        //
        [CLSCompliant(false)]
        public static uint ToUInt32(decimal d)
        {
            Truncate(ref d);
            if ((d.High| d.Mid) == 0)
            {
                uint i = d.Low;
                if (!IsNegative(d) || i == 0)
                    return i;
            }
            throw new OverflowException(SR.Overflow_UInt32);
        }

        // Converts a Decimal to an unsigned long. The Decimal
        // value is rounded towards zero to the nearest integer value, and the
        // result of this operation is returned as a long.
        //
        [CLSCompliant(false)]
        public static ulong ToUInt64(decimal d)
        {
            Truncate(ref d);
            if (d.High == 0)
            {
                ulong l = d.Low64;
                if (!IsNegative(d) || l == 0)
                    return l;
            }
            throw new OverflowException(SR.Overflow_UInt64);
        }

        // Converts a Decimal to a float. Since a float has fewer significant
        // digits than a Decimal, this operation may produce round-off errors.
        //
        public static float ToSingle(decimal d)
        {
            return DecCalc.VarR4FromDec(in d);
        }

        // Truncates a Decimal to an integer value. The Decimal argument is rounded
        // towards zero to the nearest integer value, corresponding to removing all
        // digits after the decimal point.
        //
        public static decimal Truncate(decimal d)
        {
            Truncate(ref d);
            return d;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Truncate(ref decimal d)
        {
            int flags = d._flags;
            if ((flags & ScaleMask) != 0)
                DecCalc.InternalRound(ref AsMutable(ref d), (byte)(flags >> ScaleShift), MidpointRounding.ToZero);
        }

        public static implicit operator decimal(byte value) => new decimal((uint)value);

        [CLSCompliant(false)]
        public static implicit operator decimal(sbyte value) => new decimal(value);

        public static implicit operator decimal(short value) => new decimal(value);

        [CLSCompliant(false)]
        public static implicit operator decimal(ushort value) => new decimal((uint)value);

        public static implicit operator decimal(char value) => new decimal((uint)value);

        public static implicit operator decimal(int value) => new decimal(value);

        [CLSCompliant(false)]
        public static implicit operator decimal(uint value) => new decimal(value);

        public static implicit operator decimal(long value) => new decimal(value);

        [CLSCompliant(false)]
        public static implicit operator decimal(ulong value) => new decimal(value);

        public static explicit operator decimal(float value) => new decimal(value);

        public static explicit operator decimal(double value) => new decimal(value);

        public static explicit operator byte(decimal value) => ToByte(value);

        [CLSCompliant(false)]
        public static explicit operator sbyte(decimal value) => ToSByte(value);

        public static explicit operator char(decimal value)
        {
            ushort temp;
            try
            {
                temp = ToUInt16(value);
            }
            catch (OverflowException e)
            {
                throw new OverflowException(SR.Overflow_Char, e);
            }
            return (char)temp;
        }

        public static explicit operator short(decimal value) => ToInt16(value);

        [CLSCompliant(false)]
        public static explicit operator ushort(decimal value) => ToUInt16(value);

        public static explicit operator int(decimal value) => ToInt32(value);

        [CLSCompliant(false)]
        public static explicit operator uint(decimal value) => ToUInt32(value);

        public static explicit operator long(decimal value) => ToInt64(value);

        [CLSCompliant(false)]
        public static explicit operator ulong(decimal value) => ToUInt64(value);

        public static explicit operator float(decimal value) => DecCalc.VarR4FromDec(in value);

        public static explicit operator double(decimal value) => DecCalc.VarR8FromDec(in value);

        public static decimal operator +(decimal d) => d;

        public static decimal operator -(decimal d) => new decimal(in d, d._flags ^ SignMask);

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_Increment(TSelf)" />
        public static decimal operator ++(decimal d) => Add(d, One);

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        public static decimal operator --(decimal d) => Subtract(d, One);

        public static decimal operator +(decimal d1, decimal d2)
        {
            DecCalc.DecAddSub(ref AsMutable(ref d1), ref AsMutable(ref d2), false);
            return d1;
        }

        public static decimal operator -(decimal d1, decimal d2)
        {
            DecCalc.DecAddSub(ref AsMutable(ref d1), ref AsMutable(ref d2), true);
            return d1;
        }

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_Multiply(TSelf, TOther)" />
        public static decimal operator *(decimal d1, decimal d2)
        {
            DecCalc.VarDecMul(ref AsMutable(ref d1), ref AsMutable(ref d2));
            return d1;
        }

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_Division(TSelf, TOther)" />
        public static decimal operator /(decimal d1, decimal d2)
        {
            DecCalc.VarDecDiv(ref AsMutable(ref d1), ref AsMutable(ref d2));
            return d1;
        }

        /// <inheritdoc cref="IModulusOperators{TSelf, TOther, TResult}.op_Modulus(TSelf, TOther)" />
        public static decimal operator %(decimal d1, decimal d2)
        {
            DecCalc.VarDecMod(ref AsMutable(ref d1), ref AsMutable(ref d2));
            return d1;
        }

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther, TResult}.op_Equality(TSelf, TOther)" />
        public static bool operator ==(decimal d1, decimal d2) => DecCalc.VarDecCmp(in d1, in d2) == 0;

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther, TResult}.op_Inequality(TSelf, TOther)" />
        public static bool operator !=(decimal d1, decimal d2) => DecCalc.VarDecCmp(in d1, in d2) != 0;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_LessThan(TSelf, TOther)" />
        public static bool operator <(decimal d1, decimal d2) => DecCalc.VarDecCmp(in d1, in d2) < 0;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_LessThanOrEqual(TSelf, TOther)" />
        public static bool operator <=(decimal d1, decimal d2) => DecCalc.VarDecCmp(in d1, in d2) <= 0;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_GreaterThan(TSelf, TOther)" />
        public static bool operator >(decimal d1, decimal d2) => DecCalc.VarDecCmp(in d1, in d2) > 0;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_GreaterThanOrEqual(TSelf, TOther)" />
        public static bool operator >=(decimal d1, decimal d2) => DecCalc.VarDecCmp(in d1, in d2) >= 0;

        //
        // IConvertible implementation
        //

        public TypeCode GetTypeCode()
        {
            return TypeCode.Decimal;
        }

        bool IConvertible.ToBoolean(IFormatProvider? provider)
        {
            return Convert.ToBoolean(this);
        }

        char IConvertible.ToChar(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Decimal", "Char"));
        }

        sbyte IConvertible.ToSByte(IFormatProvider? provider)
        {
            return Convert.ToSByte(this);
        }

        byte IConvertible.ToByte(IFormatProvider? provider)
        {
            return Convert.ToByte(this);
        }

        short IConvertible.ToInt16(IFormatProvider? provider)
        {
            return Convert.ToInt16(this);
        }

        ushort IConvertible.ToUInt16(IFormatProvider? provider)
        {
            return Convert.ToUInt16(this);
        }

        int IConvertible.ToInt32(IFormatProvider? provider)
        {
            return Convert.ToInt32(this);
        }

        uint IConvertible.ToUInt32(IFormatProvider? provider)
        {
            return Convert.ToUInt32(this);
        }

        long IConvertible.ToInt64(IFormatProvider? provider)
        {
            return Convert.ToInt64(this);
        }

        ulong IConvertible.ToUInt64(IFormatProvider? provider)
        {
            return Convert.ToUInt64(this);
        }

        float IConvertible.ToSingle(IFormatProvider? provider)
        {
            return DecCalc.VarR4FromDec(in this);
        }

        double IConvertible.ToDouble(IFormatProvider? provider)
        {
            return DecCalc.VarR8FromDec(in this);
        }

        decimal IConvertible.ToDecimal(IFormatProvider? provider)
        {
            return this;
        }

        DateTime IConvertible.ToDateTime(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Decimal", "DateTime"));
        }

        object IConvertible.ToType(Type type, IFormatProvider? provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }

        //
        // IAdditiveIdentity
        //

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static decimal IAdditiveIdentity<decimal, decimal>.AdditiveIdentity => AdditiveIdentity;

        //
        // IFloatingPoint
        //

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetExponentByteCount()" />
        int IFloatingPoint<decimal>.GetExponentByteCount() => sizeof(sbyte);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetExponentShortestBitLength()" />
        int IFloatingPoint<decimal>.GetExponentShortestBitLength()
        {
            sbyte exponent = Exponent;
            return (sizeof(sbyte) * 8) - sbyte.LeadingZeroCount(exponent);
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetSignificandByteCount()" />
        int IFloatingPoint<decimal>.GetSignificandByteCount() => sizeof(ulong) + sizeof(uint);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetSignificandBitLength()" />
        int IFloatingPoint<decimal>.GetSignificandBitLength() => 96;

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteExponentBigEndian(Span{byte}, out int)" />
        bool IFloatingPoint<decimal>.TryWriteExponentBigEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(sbyte))
            {
                sbyte exponent = Exponent;
                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), exponent);

                bytesWritten = sizeof(sbyte);
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteExponentLittleEndian(Span{byte}, out int)" />
        bool IFloatingPoint<decimal>.TryWriteExponentLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(sbyte))
            {
                sbyte exponent = Exponent;
                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), exponent);

                bytesWritten = sizeof(sbyte);
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteSignificandBigEndian(Span{byte}, out int)" />
        bool IFloatingPoint<decimal>.TryWriteSignificandBigEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= (sizeof(uint) + sizeof(ulong)))
            {
                uint hi32 = _hi32;
                ulong lo64 = _lo64;

                if (BitConverter.IsLittleEndian)
                {
                    hi32 = BinaryPrimitives.ReverseEndianness(hi32);
                    lo64 = BinaryPrimitives.ReverseEndianness(lo64);
                }

                ref byte address = ref MemoryMarshal.GetReference(destination);

                Unsafe.WriteUnaligned(ref address, hi32);
                Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref address, sizeof(uint)), lo64);

                bytesWritten = sizeof(uint) + sizeof(ulong);
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteSignificandLittleEndian(Span{byte}, out int)" />
        bool IFloatingPoint<decimal>.TryWriteSignificandLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= (sizeof(ulong) + sizeof(uint)))
            {
                ulong lo64 = _lo64;
                uint hi32 = _hi32;

                if (!BitConverter.IsLittleEndian)
                {
                    lo64 = BinaryPrimitives.ReverseEndianness(lo64);
                    hi32 = BinaryPrimitives.ReverseEndianness(hi32);
                }

                ref byte address = ref MemoryMarshal.GetReference(destination);

                Unsafe.WriteUnaligned(ref address, lo64);
                Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref address, sizeof(ulong)), hi32);

                bytesWritten = sizeof(ulong) + sizeof(uint);
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        //
        // IFloatingPointConstants
        //

        /// <inheritdoc cref="IFloatingPointConstants{TSelf}.E" />
        static decimal IFloatingPointConstants<decimal>.E => 2.7182818284590452353602874714m;

        /// <inheritdoc cref="IFloatingPointConstants{TSelf}.Pi" />
        static decimal IFloatingPointConstants<decimal>.Pi => 3.1415926535897932384626433833m;

        /// <inheritdoc cref="IFloatingPointConstants{TSelf}.Tau" />
        static decimal IFloatingPointConstants<decimal>.Tau => 6.2831853071795864769252867666m;

        //
        // IMinMaxValue
        //

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MinValue" />
        static decimal IMinMaxValue<decimal>.MinValue => MinValue;

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MaxValue" />
        static decimal IMinMaxValue<decimal>.MaxValue => MaxValue;

        //
        // IMultiplicativeIdentity
        //

        /// <inheritdoc cref="IMultiplicativeIdentity{TSelf, TResult}.MultiplicativeIdentity" />
        static decimal IMultiplicativeIdentity<decimal, decimal>.MultiplicativeIdentity => MultiplicativeIdentity;

        //
        // INumber
        //

        /// <inheritdoc cref="INumber{TSelf}.Clamp(TSelf, TSelf, TSelf)" />
        public static decimal Clamp(decimal value, decimal min, decimal max) => Math.Clamp(value, min, max);

        /// <inheritdoc cref="INumber{TSelf}.CopySign(TSelf, TSelf)" />
        public static decimal CopySign(decimal value, decimal sign)
        {
            return new decimal(in value, (value._flags & ~SignMask) | (sign._flags & SignMask));
        }

        /// <inheritdoc cref="INumber{TSelf}.Max(TSelf, TSelf)" />
        public static decimal Max(decimal x, decimal y)
        {
            return DecCalc.VarDecCmp(in x, in y) >= 0 ? x : y;
        }

        /// <inheritdoc cref="INumber{TSelf}.MaxNumber(TSelf, TSelf)" />
        static decimal INumber<decimal>.MaxNumber(decimal x, decimal y) => Max(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Min(TSelf, TSelf)" />
        public static decimal Min(decimal x, decimal y)
        {
            return DecCalc.VarDecCmp(in x, in y) < 0 ? x : y;
        }

        /// <inheritdoc cref="INumber{TSelf}.MinNumber(TSelf, TSelf)" />
        static decimal INumber<decimal>.MinNumber(decimal x, decimal y) => Min(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Sign(TSelf)" />
        public static int Sign(decimal d) => (d.Low64 | d.High) == 0 ? 0 : (d._flags >> 31) | 1;

        //
        // INumberBase
        //

        /// <inheritdoc cref="INumberBase{TSelf}.One" />
        static decimal INumberBase<decimal>.One => One;

        /// <inheritdoc cref="INumberBase{TSelf}.Radix" />
        static int INumberBase<decimal>.Radix => 10;

        /// <inheritdoc cref="INumberBase{TSelf}.Zero" />
        static decimal INumberBase<decimal>.Zero => Zero;

        /// <inheritdoc cref="INumberBase{TSelf}.Abs(TSelf)" />
        public static decimal Abs(decimal value)
        {
            return new decimal(in value, value._flags & ~SignMask);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateChecked{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal CreateChecked<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            decimal result;

            if (typeof(TOther) == typeof(decimal))
            {
                result = (decimal)(object)value;
            }
            else if (!TryConvertFromChecked(value, out result) && !TOther.TryConvertToChecked(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateSaturating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal CreateSaturating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            decimal result;

            if (typeof(TOther) == typeof(decimal))
            {
                result = (decimal)(object)value;
            }
            else if (!TryConvertFrom(value, out result) && !TOther.TryConvertToSaturating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateTruncating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal CreateTruncating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            decimal result;

            if (typeof(TOther) == typeof(decimal))
            {
                result = (decimal)(object)value;
            }
            else if (!TryConvertFrom(value, out result) && !TOther.TryConvertToTruncating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsCanonical(TSelf)" />
        public static bool IsCanonical(decimal value)
        {
            uint scale = (byte)(value._flags >> ScaleShift);

            if (scale == 0)
            {
                // We have an exact integer represented with no trailing zero
                return true;
            }

            // We have some value where some fractional part is specified. So,
            // if the least significant digit is 0, then we are not canonical

            if (value._hi32 == 0)
            {
                return (value._lo64 % 10) != 0;
            }

            var significand = new UInt128(value._hi32, value._lo64);
            return (significand % 10U) != 0U;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsComplexNumber(TSelf)" />
        static bool INumberBase<decimal>.IsComplexNumber(decimal value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsEvenInteger(TSelf)" />
        public static bool IsEvenInteger(decimal value)
        {
            decimal truncatedValue = Truncate(value);
            return (value == truncatedValue) && ((truncatedValue._lo64 & 1) == 0);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsFinite(TSelf)" />
        static bool INumberBase<decimal>.IsFinite(decimal value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsImaginaryNumber(TSelf)" />
        static bool INumberBase<decimal>.IsImaginaryNumber(decimal value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsInfinity(TSelf)" />
        static bool INumberBase<decimal>.IsInfinity(decimal value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsInteger(TSelf)" />
        public static bool IsInteger(decimal value) => value == Truncate(value);

        /// <inheritdoc cref="INumberBase{TSelf}.IsNaN(TSelf)" />
        static bool INumberBase<decimal>.IsNaN(decimal value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsNegative(TSelf)" />
        public static bool IsNegative(decimal value) => value._flags < 0;

        /// <inheritdoc cref="INumberBase{TSelf}.IsNegativeInfinity(TSelf)" />
        static bool INumberBase<decimal>.IsNegativeInfinity(decimal value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsNormal(TSelf)" />
        static bool INumberBase<decimal>.IsNormal(decimal value) => value != 0;

        /// <inheritdoc cref="INumberBase{TSelf}.IsOddInteger(TSelf)" />
        public static bool IsOddInteger(decimal value)
        {
            decimal truncatedValue = Truncate(value);
            return (value == truncatedValue) && ((truncatedValue._lo64 & 1) != 0);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsPositive(TSelf)" />
        public static bool IsPositive(decimal value) => value._flags >= 0;

        /// <inheritdoc cref="INumberBase{TSelf}.IsPositiveInfinity(TSelf)" />
        static bool INumberBase<decimal>.IsPositiveInfinity(decimal value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsRealNumber(TSelf)" />
        static bool INumberBase<decimal>.IsRealNumber(decimal value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsSubnormal(TSelf)" />
        static bool INumberBase<decimal>.IsSubnormal(decimal value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsZero(TSelf)" />
        static bool INumberBase<decimal>.IsZero(decimal value) => (value == 0);

        /// <inheritdoc cref="INumberBase{TSelf}.MaxMagnitude(TSelf, TSelf)" />
        public static decimal MaxMagnitude(decimal x, decimal y)
        {
            decimal ax = Abs(x);
            decimal ay = Abs(y);

            if (ax > ay)
            {
                return x;
            }

            if (ax == ay)
            {
                return IsNegative(x) ? y : x;
            }

            return y;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.MaxMagnitudeNumber(TSelf, TSelf)" />
        static decimal INumberBase<decimal>.MaxMagnitudeNumber(decimal x, decimal y) => MaxMagnitude(x, y);

        /// <inheritdoc cref="INumberBase{TSelf}.MinMagnitude(TSelf, TSelf)" />
        public static decimal MinMagnitude(decimal x, decimal y)
        {
            decimal ax = Abs(x);
            decimal ay = Abs(y);

            if (ax < ay)
            {
                return x;
            }

            if (ax == ay)
            {
                return IsNegative(x) ? x : y;
            }

            return y;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.MinMagnitudeNumber(TSelf, TSelf)" />
        static decimal INumberBase<decimal>.MinMagnitudeNumber(decimal x, decimal y) => MinMagnitude(x, y);

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromChecked{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<decimal>.TryConvertFromChecked<TOther>(TOther value, out decimal result) => TryConvertFromChecked(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvertFromChecked<TOther>(TOther value, out decimal result)
            where TOther : INumberBase<TOther>
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `decimal` will handle the other unsigned types and
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
            else if (typeof(TOther) == typeof(ushort))
            {
                ushort actualValue = (ushort)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualValue = (uint)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualValue = (ulong)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualValue = (UInt128)(object)value;
                result = checked((decimal)actualValue);
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                nuint actualValue = (nuint)(object)value;
                result = actualValue;
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
        static bool INumberBase<decimal>.TryConvertFromSaturating<TOther>(TOther value, out decimal result)
        {
            return TryConvertFrom(value, out result);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromTruncating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<decimal>.TryConvertFromTruncating<TOther>(TOther value, out decimal result)
        {
            return TryConvertFrom(value, out result);
        }

        private static bool TryConvertFrom<TOther>(TOther value, out decimal result)
            where TOther : INumberBase<TOther>
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `decimal` will handle the other unsigned types and
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
            else if (typeof(TOther) == typeof(ushort))
            {
                ushort actualValue = (ushort)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualValue = (uint)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualValue = (ulong)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualValue = (UInt128)(object)value;
                result = (actualValue >= new UInt128(0x0000_0000_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)) ? MaxValue : (decimal)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                nuint actualValue = (nuint)(object)value;
                result = actualValue;
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
        static bool INumberBase<decimal>.TryConvertToChecked<TOther>(decimal value, [MaybeNullWhen(false)] out TOther result)
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `decimal` will handle the other unsigned types and
            // `ConvertTo` will handle the signed types

            if (typeof(TOther) == typeof(double))
            {
                double actualResult = checked((double)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualResult = checked((Half)value);
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
                int actualResult = checked((int)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualResult = checked((long)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualResult = checked((Int128)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualResult = checked((nint)value);
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
                float actualResult = checked((float)value);
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
        static bool INumberBase<decimal>.TryConvertToSaturating<TOther>(decimal value, [MaybeNullWhen(false)] out TOther result)
        {
            return TryConvertTo(value, out result);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToTruncating{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<decimal>.TryConvertToTruncating<TOther>(decimal value, [MaybeNullWhen(false)] out TOther result)
        {
            return TryConvertTo(value, out result);
        }

        private static bool TryConvertTo<TOther>(decimal value, [MaybeNullWhen(false)] out TOther result)
            where TOther : INumberBase<TOther>
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `decimal` will handle the other unsigned types and
            // `ConvertTo` will handle the signed types

            if (typeof(TOther) == typeof(double))
            {
                double actualResult = (double)value;
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
                short actualResult = (value >= short.MaxValue) ? short.MaxValue :
                                     (value <= short.MinValue) ? short.MinValue : (short)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualResult = (value >= int.MaxValue) ? int.MaxValue :
                                   (value <= int.MinValue) ? int.MinValue : (int)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualResult = (value >= long.MaxValue) ? long.MaxValue :
                                    (value <= long.MinValue) ? long.MinValue : (long)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualResult = (Int128)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualResult = (value >= nint.MaxValue) ? nint.MaxValue :
                                    (value <= nint.MinValue) ? nint.MinValue : (nint)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualResult = (value >= sbyte.MaxValue) ? sbyte.MaxValue :
                                     (value <= sbyte.MinValue) ? sbyte.MinValue : (sbyte)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualResult = (float)value;
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
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out decimal result) => TryParse(s, NumberStyles.Number, provider, out result);

        //
        // ISignedNumber
        //

        /// <inheritdoc cref="ISignedNumber{TSelf}.NegativeOne" />
        static decimal ISignedNumber<decimal>.NegativeOne => NegativeOne;

        //
        // ISpanParsable
        //

        /// <inheritdoc cref="ISpanParsable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static decimal Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Number, provider);

        /// <inheritdoc cref="ISpanParsable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out decimal result) => TryParse(s, NumberStyles.Number, provider, out result);

        //
        // IUtf8SpanParsable
        //

        /// <inheritdoc cref="INumberBase{TSelf}.Parse(ReadOnlySpan{byte}, NumberStyles, IFormatProvider?)" />
        public static decimal Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style = NumberStyles.Number, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.ParseDecimal(utf8Text, style, NumberFormatInfo.GetInstance(provider));
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryParse(ReadOnlySpan{byte}, NumberStyles, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out decimal result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.TryParseDecimal(utf8Text, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
        }

        /// <inheritdoc cref="IUtf8SpanParsable{TSelf}.Parse(ReadOnlySpan{byte}, IFormatProvider?)" />
        public static decimal Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) => Parse(utf8Text, NumberStyles.Number, provider);

        /// <inheritdoc cref="IUtf8SpanParsable{TSelf}.TryParse(ReadOnlySpan{byte}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out decimal result) => TryParse(utf8Text, NumberStyles.Number, provider, out result);
    }
}
