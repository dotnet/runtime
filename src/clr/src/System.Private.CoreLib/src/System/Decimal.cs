// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Versioning;
using System.Runtime.Serialization;
using System.Diagnostics;

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
    // to Decimal. These widening conversions never loose any information
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
    // float or double may loose precision, but will not loose
    // information about the overall magnitude of the numeric value, and will never
    // throw an exception. A conversion from float or double to
    // Decimal throws an OverflowException if the value is not within
    // the range of the Decimal type.
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [System.Runtime.Versioning.NonVersionable] // This only applies to field layout
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public partial struct Decimal : IFormattable, IComparable, IConvertible, IComparable<decimal>, IEquatable<decimal>, IDeserializationCallback, ISpanFormattable
    {
        // Sign mask for the flags field. A value of zero in this bit indicates a
        // positive Decimal value, and a value of one in this bit indicates a
        // negative Decimal value.
        // 
        // Look at OleAut's DECIMAL_NEG constant to check for negative values
        // in native code.
        private const int SignMask = unchecked((int)0x80000000);
        private const byte DECIMAL_NEG = 0x80;
        private const byte DECIMAL_ADD = 0x00;

        // Scale mask for the flags field. This byte in the flags field contains
        // the power of 10 to divide the Decimal value by. The scale byte must
        // contain a value between 0 and 28 inclusive.
        private const int ScaleMask = 0x00FF0000;

        // Number of bits scale is shifted by.
        private const int ScaleShift = 16;

        // The maximum power of 10 that a 32 bit integer can store
        private const int MaxInt32Scale = 9;

        // Fast access for 10^n where n is 0-9        
        private static uint[] Powers10 = new uint[] {
            1,
            10,
            100,
            1000,
            10000,
            100000,
            1000000,
            10000000,
            100000000,
            1000000000
        };

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


        // Constant representing the negative number that is the closest possible
        // Decimal value to -0m.
        private const decimal NearNegativeZero = -0.0000000000000000000000000001m;

        // Constant representing the positive number that is the closest possible
        // Decimal value to +0m.
        private const decimal NearPositiveZero = +0.0000000000000000000000000001m;

        // The lo, mid, hi, and flags fields contain the representation of the
        // Decimal value. The lo, mid, and hi fields contain the 96-bit integer
        // part of the Decimal. Bits 0-15 (the lower word) of the flags field are
        // unused and must be zero; bits 16-23 contain must contain a value between
        // 0 and 28, indicating the power of 10 to divide the 96-bit integer part
        // by to produce the Decimal value; bits 24-30 are unused and must be zero;
        // and finally bit 31 indicates the sign of the Decimal value, 0 meaning
        // positive and 1 meaning negative.
        //
        // NOTE: Do not change the order in which these fields are declared. The
        // native methods in this class rely on this particular order. 
        // Do not rename (binary serialization).
        private int flags;
        private int hi;
        private int lo;
        private int mid;

        internal uint High => (uint)hi;
        internal uint Low => (uint)lo;
        internal uint Mid => (uint)mid;

        // Constructs a zero Decimal.
        //public Decimal() {
        //    lo = 0;
        //    mid = 0;
        //    hi = 0;
        //    flags = 0;
        //}

        // Constructs a Decimal from an integer value.
        //
        public Decimal(int value)
        {
            if (value >= 0)
            {
                flags = 0;
            }
            else
            {
                flags = SignMask;
                value = -value;
            }
            lo = value;
            mid = 0;
            hi = 0;
        }

        // Constructs a Decimal from an unsigned integer value.
        //
        [CLSCompliant(false)]
        public Decimal(uint value)
        {
            flags = 0;
            lo = (int)value;
            mid = 0;
            hi = 0;
        }

        // Constructs a Decimal from a long value.
        //
        public Decimal(long value)
        {
            if (value >= 0)
            {
                flags = 0;
            }
            else
            {
                flags = SignMask;
                value = -value;
            }
            lo = (int)value;
            mid = (int)(value >> 32);
            hi = 0;
        }

        // Constructs a Decimal from an unsigned long value.
        //
        [CLSCompliant(false)]
        public Decimal(ulong value)
        {
            flags = 0;
            lo = (int)value;
            mid = (int)(value >> 32);
            hi = 0;
        }

        // Constructs a Decimal from a float value.
        //
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern Decimal(float value);

        // Constructs a Decimal from a double value.
        //
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern Decimal(double value);

        // Constructs a Decimal from a Currency value.
        //
        internal Decimal(Currency value)
        {
            this = Currency.ToDecimal(value);
        }

        // Don't remove these 2 methods below. They are required by the fx when the are dealing with Currency in their
        // databases
        public static long ToOACurrency(decimal value)
        {
            return new Currency(value).ToOACurrency();
        }

        public static decimal FromOACurrency(long cy)
        {
            return Currency.ToDecimal(Currency.FromOACurrency(cy));
        }


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
        public Decimal(int[] bits)
        {
            if (bits == null)
                throw new ArgumentNullException(nameof(bits));
            if (bits.Length == 4)
            {
                int f = bits[3];
                if ((f & ~(SignMask | ScaleMask)) == 0 && (f & ScaleMask) <= (28 << 16))
                {
                    lo = bits[0];
                    mid = bits[1];
                    hi = bits[2];
                    flags = f;
                    return;
                }
            }
            throw new ArgumentException(SR.Arg_DecBitCtor);
        }

        // Constructs a Decimal from its constituent parts.
        // 
        public Decimal(int lo, int mid, int hi, bool isNegative, byte scale)
        {
            if (scale > 28)
                throw new ArgumentOutOfRangeException(nameof(scale), SR.ArgumentOutOfRange_DecimalScale);
            this.lo = lo;
            this.mid = mid;
            this.hi = hi;
            flags = ((int)scale) << 16;
            if (isNegative)
                flags |= SignMask;
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext ctx)
        {
            // OnSerializing is called before serialization of an object
            try
            {
                // Run the constructor to validate the decimal
                new decimal(lo, mid, hi, flags);
            }
            catch (ArgumentException e)
            {
                throw new SerializationException(SR.Overflow_Decimal, e);
            }
        }

        void IDeserializationCallback.OnDeserialization(object sender)
        {
            // OnDeserialization is called after each instance of this class is deserialized.
            // This callback method performs decimal validation after being deserialized.
            try
            {
                // Run the constructor to validate the decimal
                new decimal(lo, mid, hi, flags);
            }
            catch (ArgumentException e)
            {
                throw new SerializationException(SR.Overflow_Decimal, e);
            }
        }

        // Constructs a Decimal from its constituent parts.
        private Decimal(int lo, int mid, int hi, int flags)
        {
            if ((flags & ~(SignMask | ScaleMask)) == 0 && (flags & ScaleMask) <= (28 << 16))
            {
                this.lo = lo;
                this.mid = mid;
                this.hi = hi;
                this.flags = flags;
                return;
            }
            throw new ArgumentException(SR.Arg_DecBitCtor);
        }

        // Returns the absolute value of the given Decimal. If d is
        // positive, the result is d. If d is negative, the result
        // is -d.
        //
        internal static decimal Abs(ref decimal d)
        {
            return new decimal(d.lo, d.mid, d.hi, d.flags & ~SignMask);
        }

        // Adds two Decimal values.
        //
        public static decimal Add(decimal d1, decimal d2)
        {
            FCallAddSub(ref d1, ref d2, DECIMAL_ADD);
            return d1;
        }

        internal bool IsNegative => (flags & SignMask) != 0;

        internal int Scale => (byte)((uint)flags >> ScaleShift);

        // FCallAddSub adds or subtracts two decimal values.  On return, d1 contains the result
        // of the operation.  Passing in DECIMAL_ADD or DECIMAL_NEG for bSign indicates
        // addition or subtraction, respectively.
        //
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void FCallAddSub(ref decimal d1, ref decimal d2, byte bSign);

        // Rounds a Decimal to an integer value. The Decimal argument is rounded
        // towards positive infinity.
        public static decimal Ceiling(decimal d)
        {
            return (-(decimal.Floor(-d)));
        }

        // Compares two Decimal values, returning an integer that indicates their
        // relationship.
        //
        public static int Compare(decimal d1, decimal d2)
        {
            return FCallCompare(ref d1, ref d2);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int FCallCompare(ref decimal d1, ref decimal d2);

        // Compares this object to another object, returning an integer that
        // indicates the relationship. 
        // Returns a value less than zero if this  object
        // null is considered to be less than any instance.
        // If object is not of type Decimal, this method throws an ArgumentException.
        // 
        public int CompareTo(object value)
        {
            if (value == null)
                return 1;
            if (!(value is decimal))
                throw new ArgumentException(SR.Arg_MustBeDecimal);

            decimal other = (decimal)value;
            return FCallCompare(ref this, ref other);
        }

        public int CompareTo(decimal value)
        {
            return FCallCompare(ref this, ref value);
        }

        // Divides two Decimal values.
        //
        public static decimal Divide(decimal d1, decimal d2)
        {
            FCallDivide(ref d1, ref d2);
            return d1;
        }

        // FCallDivide divides two decimal values.  On return, d1 contains the result
        // of the operation.
        //
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void FCallDivide(ref decimal d1, ref decimal d2);


        // Checks if this Decimal is equal to a given object. Returns true
        // if the given object is a boxed Decimal and its value is equal to the
        // value of this Decimal. Returns false otherwise.
        //
        public override bool Equals(object value)
        {
            if (value is decimal)
            {
                decimal other = (decimal)value;
                return FCallCompare(ref this, ref other) == 0;
            }
            return false;
        }

        public bool Equals(decimal value)
        {
            return FCallCompare(ref this, ref value) == 0;
        }

        // Returns the hash code for this Decimal.
        //
        public override int GetHashCode() => GetHashCode(ref this);

        // Compares two Decimal values for equality. Returns true if the two
        // Decimal values are equal, or false if they are not equal.
        //
        public static bool Equals(decimal d1, decimal d2)
        {
            return FCallCompare(ref d1, ref d2) == 0;
        }

        // Rounds a Decimal to an integer value. The Decimal argument is rounded
        // towards negative infinity.
        //
        public static decimal Floor(decimal d)
        {
            FCallFloor(ref d);
            return d;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void FCallFloor(ref decimal d);

        // Converts this Decimal to a string. The resulting string consists of an
        // optional minus sign ("-") followed to a sequence of digits ("0" - "9"),
        // optionally followed by a decimal point (".") and another sequence of
        // digits.
        //
        public override string ToString()
        {
            return Number.FormatDecimal(this, null, NumberFormatInfo.CurrentInfo);
        }

        public string ToString(string format)
        {
            return Number.FormatDecimal(this, format, NumberFormatInfo.CurrentInfo);
        }

        public string ToString(IFormatProvider provider)
        {
            return Number.FormatDecimal(this, null, NumberFormatInfo.GetInstance(provider));
        }

        public string ToString(string format, IFormatProvider provider)
        {
            return Number.FormatDecimal(this, format, NumberFormatInfo.GetInstance(provider));
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider provider = null)
        {
            return Number.TryFormatDecimal(this, format, NumberFormatInfo.GetInstance(provider), destination, out charsWritten);
        }

        // Converts a string to a Decimal. The string must consist of an optional
        // minus sign ("-") followed by a sequence of digits ("0" - "9"). The
        // sequence of digits may optionally contain a single decimal point (".")
        // character. Leading and trailing whitespace characters are allowed.
        // Parse also allows a currency symbol, a trailing negative sign, and
        // parentheses in the number.
        //
        public static decimal Parse(string s)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseDecimal(s, NumberStyles.Number, NumberFormatInfo.CurrentInfo);
        }

        public static decimal Parse(string s, NumberStyles style)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseDecimal(s, style, NumberFormatInfo.CurrentInfo);
        }

        public static decimal Parse(string s, IFormatProvider provider)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseDecimal(s, NumberStyles.Number, NumberFormatInfo.GetInstance(provider));
        }

        public static decimal Parse(string s, NumberStyles style, IFormatProvider provider)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseDecimal(s, style, NumberFormatInfo.GetInstance(provider));
        }

        public static decimal Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Number, IFormatProvider provider = null)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Number.ParseDecimal(s, style, NumberFormatInfo.GetInstance(provider));
        }

        public static bool TryParse(string s, out decimal result)
        {
            if (s == null)
            {
                result = 0;
                return false;
            }

            return Number.TryParseDecimal(s, NumberStyles.Number, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse(ReadOnlySpan<char> s, out decimal result)
        {
            return Number.TryParseDecimal(s, NumberStyles.Number, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse(string s, NumberStyles style, IFormatProvider provider, out decimal result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);

            if (s == null)
            {
                result = 0;
                return false;
            }

            return Number.TryParseDecimal(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out decimal result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Number.TryParseDecimal(s, style, NumberFormatInfo.GetInstance(provider), out result);
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
            return new int[] { d.lo, d.mid, d.hi, d.flags };
        }

        internal static void GetBytes(decimal d, byte[] buffer)
        {
            Debug.Assert((buffer != null && buffer.Length >= 16), "[GetBytes]buffer != null && buffer.Length >= 16");
            buffer[0] = (byte)d.lo;
            buffer[1] = (byte)(d.lo >> 8);
            buffer[2] = (byte)(d.lo >> 16);
            buffer[3] = (byte)(d.lo >> 24);

            buffer[4] = (byte)d.mid;
            buffer[5] = (byte)(d.mid >> 8);
            buffer[6] = (byte)(d.mid >> 16);
            buffer[7] = (byte)(d.mid >> 24);

            buffer[8] = (byte)d.hi;
            buffer[9] = (byte)(d.hi >> 8);
            buffer[10] = (byte)(d.hi >> 16);
            buffer[11] = (byte)(d.hi >> 24);

            buffer[12] = (byte)d.flags;
            buffer[13] = (byte)(d.flags >> 8);
            buffer[14] = (byte)(d.flags >> 16);
            buffer[15] = (byte)(d.flags >> 24);
        }

        internal static decimal ToDecimal(byte[] buffer)
        {
            Debug.Assert((buffer != null && buffer.Length >= 16), "[ToDecimal]buffer != null && buffer.Length >= 16");
            int lo = ((int)buffer[0]) | ((int)buffer[1] << 8) | ((int)buffer[2] << 16) | ((int)buffer[3] << 24);
            int mid = ((int)buffer[4]) | ((int)buffer[5] << 8) | ((int)buffer[6] << 16) | ((int)buffer[7] << 24);
            int hi = ((int)buffer[8]) | ((int)buffer[9] << 8) | ((int)buffer[10] << 16) | ((int)buffer[11] << 24);
            int flags = ((int)buffer[12]) | ((int)buffer[13] << 8) | ((int)buffer[14] << 16) | ((int)buffer[15] << 24);
            return new decimal(lo, mid, hi, flags);
        }

        // This method does a 'raw' and 'unchecked' addition of a UInt32 to a Decimal in place. 
        // 'raw' means that it operates on the internal 96-bit unsigned integer value and 
        // ingores the sign and scale. This means that it is not equivalent to just adding
        // that number, as the sign and scale are effectively applied to the UInt32 value also.
        // 'unchecked' means that it does not fail if you overflow the 96 bit value.
        private static void InternalAddUInt32RawUnchecked(ref decimal value, uint i)
        {
            uint v;
            uint sum;
            v = (uint)value.lo;
            sum = v + i;
            value.lo = (int)sum;
            if (sum < v || sum < i)
            {
                v = (uint)value.mid;
                sum = v + 1;
                value.mid = (int)sum;
                if (sum < v || sum < 1)
                {
                    value.hi = (int)((uint)value.hi + 1);
                }
            }
        }

        // This method does an in-place division of a decimal by a UInt32, returning the remainder. 
        // Although it does not operate on the sign or scale, this does not result in any 
        // caveat for the result. It is equivalent to dividing by that number.
        private static uint InternalDivRemUInt32(ref decimal value, uint divisor)
        {
            uint remainder = 0;
            ulong n;
            if (value.hi != 0)
            {
                n = ((uint)value.hi);
                value.hi = (int)((uint)(n / divisor));
                remainder = (uint)(n % divisor);
            }
            if (value.mid != 0 || remainder != 0)
            {
                n = ((ulong)remainder << 32) | (uint)value.mid;
                value.mid = (int)((uint)(n / divisor));
                remainder = (uint)(n % divisor);
            }
            if (value.lo != 0 || remainder != 0)
            {
                n = ((ulong)remainder << 32) | (uint)value.lo;
                value.lo = (int)((uint)(n / divisor));
                remainder = (uint)(n % divisor);
            }
            return remainder;
        }

        // Does an in-place round the specified number of digits, rounding mid-point values
        // away from zero
        private static void InternalRoundFromZero(ref decimal d, int decimalCount)
        {
            int scale = (d.flags & ScaleMask) >> ScaleShift;
            int scaleDifference = scale - decimalCount;
            if (scaleDifference <= 0)
            {
                return;
            }
            // Divide the value by 10^scaleDifference
            uint lastRemainder;
            uint lastDivisor;
            do
            {
                int diffChunk = (scaleDifference > MaxInt32Scale) ? MaxInt32Scale : scaleDifference;
                lastDivisor = Powers10[diffChunk];
                lastRemainder = InternalDivRemUInt32(ref d, lastDivisor);
                scaleDifference -= diffChunk;
            } while (scaleDifference > 0);

            // Round away from zero at the mid point
            if (lastRemainder >= (lastDivisor >> 1))
            {
                InternalAddUInt32RawUnchecked(ref d, 1);
            }

            // the scale becomes the desired decimal count
            d.flags = ((decimalCount << ScaleShift) & ScaleMask) | (d.flags & SignMask);
        }

        // Returns the larger of two Decimal values.
        //
        internal static decimal Max(ref decimal d1, ref decimal d2)
        {
            return FCallCompare(ref d1, ref d2) >= 0 ? d1 : d2;
        }

        // Returns the smaller of two Decimal values.
        //
        internal static decimal Min(ref decimal d1, ref decimal d2)
        {
            return FCallCompare(ref d1, ref d2) < 0 ? d1 : d2;
        }

        public static decimal Remainder(decimal d1, decimal d2)
        {
            // OleAut doesn't provide a VarDecMod.            

            // In the operation x % y the sign of y does not matter. Result will have the sign of x.
            d2.flags = (d2.flags & ~SignMask) | (d1.flags & SignMask);


            // This piece of code is to work around the fact that Dividing a decimal with 28 digits number by decimal which causes
            // causes the result to be 28 digits, can cause to be incorrectly rounded up.
            // eg. Decimal.MaxValue / 2 * Decimal.MaxValue will overflow since the division by 2 was rounded instead of being truncked.
            if (Math.Abs(d1) < Math.Abs(d2))
            {
                return d1;
            }
            d1 -= d2;

            if (d1 == 0)
            {
                // The sign of D1 will be wrong here. Fall through so that we still get a DivideByZeroException
                d1.flags = (d1.flags & ~SignMask) | (d2.flags & SignMask);
            }

            // Formula:  d1 - (RoundTowardsZero(d1 / d2) * d2)            
            decimal dividedResult = Truncate(d1 / d2);
            decimal multipliedResult = dividedResult * d2;
            decimal result = d1 - multipliedResult;
            // See if the result has crossed 0
            if ((d1.flags & SignMask) != (result.flags & SignMask))
            {
                if (NearNegativeZero <= result && result <= NearPositiveZero)
                {
                    // Certain Remainder operations on decimals with 28 significant digits round
                    // to [+-]0.0000000000000000000000000001m instead of [+-]0m during the intermediate calculations. 
                    // 'zero' results just need their sign corrected.
                    result.flags = (result.flags & ~SignMask) | (d1.flags & SignMask);
                }
                else
                {
                    // If the division rounds up because it runs out of digits, the multiplied result can end up with a larger
                    // absolute value and the result of the formula crosses 0. To correct it can add the divisor back.
                    result += d2;
                }
            }

            return result;
        }

        // Multiplies two Decimal values.
        //
        public static decimal Multiply(decimal d1, decimal d2)
        {
            FCallMultiply(ref d1, ref d2);
            return d1;
        }

        // FCallMultiply multiples two decimal values.  On return, d1 contains the result
        // of the operation.
        //
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void FCallMultiply(ref decimal d1, ref decimal d2);

        // Returns the negated value of the given Decimal. If d is non-zero,
        // the result is -d. If d is zero, the result is zero.
        //
        public static decimal Negate(decimal d)
        {
            return new decimal(d.lo, d.mid, d.hi, d.flags ^ SignMask);
        }

        // Rounds a Decimal value to a given number of decimal places. The value
        // given by d is rounded to the number of decimal places given by
        // decimals. The decimals argument must be an integer between
        // 0 and 28 inclusive.
        //
        // By default a mid-point value is rounded to the nearest even number. If the mode is
        // passed in, it can also round away from zero.

        public static decimal Round(decimal d)
        {
            return Round(d, 0);
        }

        public static decimal Round(decimal d, int decimals)
        {
            FCallRound(ref d, decimals);
            return d;
        }

        public static decimal Round(decimal d, MidpointRounding mode)
        {
            return Round(d, 0, mode);
        }

        public static decimal Round(decimal d, int decimals, MidpointRounding mode)
        {
            if ((decimals < 0) || (decimals > 28))
                throw new ArgumentOutOfRangeException(nameof(decimals), SR.ArgumentOutOfRange_DecimalRound);
            if (mode < MidpointRounding.ToEven || mode > MidpointRounding.AwayFromZero)
            {
                throw new ArgumentException(SR.Format(SR.Argument_InvalidEnumValue, mode, nameof(MidpointRounding)), nameof(mode));
            }

            if (mode == MidpointRounding.ToEven)
            {
                FCallRound(ref d, decimals);
            }
            else
            {
                InternalRoundFromZero(ref d, decimals);
            }
            return d;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void FCallRound(ref decimal d, int decimals);

        internal static int Sign(ref decimal d) => (d.lo | d.mid | d.hi) == 0 ? 0 : (d.flags >> 31) | 1;

        // Subtracts two Decimal values.
        //
        public static decimal Subtract(decimal d1, decimal d2)
        {
            FCallAddSub(ref d1, ref d2, DECIMAL_NEG);
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
            catch (OverflowException e)
            {
                throw new OverflowException(SR.Overflow_Byte, e);
            }
            if (temp < byte.MinValue || temp > byte.MaxValue) throw new OverflowException(SR.Overflow_Byte);
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
            catch (OverflowException e)
            {
                throw new OverflowException(SR.Overflow_SByte, e);
            }
            if (temp < sbyte.MinValue || temp > sbyte.MaxValue) throw new OverflowException(SR.Overflow_SByte);
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
            catch (OverflowException e)
            {
                throw new OverflowException(SR.Overflow_Int16, e);
            }
            if (temp < short.MinValue || temp > short.MaxValue) throw new OverflowException(SR.Overflow_Int16);
            return (short)temp;
        }


        // Converts a Decimal to a Currency. Since a Currency
        // has fewer significant digits than a Decimal, this operation may
        // produce round-off errors.
        //
        internal static Currency ToCurrency(decimal d)
        {
            Currency result = new Currency();
            FCallToCurrency(ref result, d);
            return result;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void FCallToCurrency(ref Currency result, decimal d);

        // Converts a Decimal to a double. Since a double has fewer significant
        // digits than a Decimal, this operation may produce round-off errors.
        //
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern double ToDouble(decimal d);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int FCallToInt32(decimal d);

        // Converts a Decimal to an integer. The Decimal value is rounded towards
        // zero to the nearest integer value, and the result of this operation is
        // returned as an integer.
        //
        public static int ToInt32(decimal d)
        {
            if ((d.flags & ScaleMask) != 0) FCallTruncate(ref d);
            if (d.hi == 0 && d.mid == 0)
            {
                int i = d.lo;
                if (d.flags >= 0)
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
            if ((d.flags & ScaleMask) != 0) FCallTruncate(ref d);
            if (d.hi == 0)
            {
                long l = d.lo & 0xFFFFFFFFL | (long)d.mid << 32;
                if (d.flags >= 0)
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
            catch (OverflowException e)
            {
                throw new OverflowException(SR.Overflow_UInt16, e);
            }
            if (temp < ushort.MinValue || temp > ushort.MaxValue) throw new OverflowException(SR.Overflow_UInt16);
            return (ushort)temp;
        }

        // Converts a Decimal to an unsigned integer. The Decimal 
        // value is rounded towards zero to the nearest integer value, and the 
        // result of this operation is returned as an unsigned integer.
        //
        [CLSCompliant(false)]
        public static uint ToUInt32(decimal d)
        {
            if ((d.flags & ScaleMask) != 0) FCallTruncate(ref d);
            if (d.hi == 0 && d.mid == 0)
            {
                uint i = (uint)d.lo;
                if (d.flags >= 0 || i == 0)
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
            if ((d.flags & ScaleMask) != 0) FCallTruncate(ref d);
            if (d.hi == 0)
            {
                ulong l = ((ulong)(uint)d.lo) | ((ulong)(uint)d.mid << 32);
                if (d.flags >= 0 || l == 0)
                    return l;
            }
            throw new OverflowException(SR.Overflow_UInt64);
        }

        // Converts a Decimal to a float. Since a float has fewer significant
        // digits than a Decimal, this operation may produce round-off errors.
        //
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern float ToSingle(decimal d);

        // Truncates a Decimal to an integer value. The Decimal argument is rounded
        // towards zero to the nearest integer value, corresponding to removing all
        // digits after the decimal point.
        //
        public static decimal Truncate(decimal d)
        {
            FCallTruncate(ref d);
            return d;
        }


        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void FCallTruncate(ref decimal d);


        public static implicit operator decimal(byte value)
        {
            return new decimal(value);
        }

        [CLSCompliant(false)]
        public static implicit operator decimal(sbyte value)
        {
            return new decimal(value);
        }

        public static implicit operator decimal(short value)
        {
            return new decimal(value);
        }

        [CLSCompliant(false)]
        public static implicit operator decimal(ushort value)
        {
            return new decimal(value);
        }

        public static implicit operator decimal(char value)
        {
            return new decimal(value);
        }

        public static implicit operator decimal(int value)
        {
            return new decimal(value);
        }

        [CLSCompliant(false)]
        public static implicit operator decimal(uint value)
        {
            return new decimal(value);
        }

        public static implicit operator decimal(long value)
        {
            return new decimal(value);
        }

        [CLSCompliant(false)]
        public static implicit operator decimal(ulong value)
        {
            return new decimal(value);
        }


        public static explicit operator decimal(float value)
        {
            return new decimal(value);
        }

        public static explicit operator decimal(double value)
        {
            return new decimal(value);
        }

        public static explicit operator byte(decimal value)
        {
            return ToByte(value);
        }

        [CLSCompliant(false)]
        public static explicit operator sbyte(decimal value)
        {
            return ToSByte(value);
        }

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

        public static explicit operator short(decimal value)
        {
            return ToInt16(value);
        }

        [CLSCompliant(false)]
        public static explicit operator ushort(decimal value)
        {
            return ToUInt16(value);
        }

        public static explicit operator int(decimal value)
        {
            return ToInt32(value);
        }

        [CLSCompliant(false)]
        public static explicit operator uint(decimal value)
        {
            return ToUInt32(value);
        }

        public static explicit operator long(decimal value)
        {
            return ToInt64(value);
        }

        [CLSCompliant(false)]
        public static explicit operator ulong(decimal value)
        {
            return ToUInt64(value);
        }

        public static explicit operator float(decimal value)
        {
            return ToSingle(value);
        }

        public static explicit operator double(decimal value)
        {
            return ToDouble(value);
        }

        public static decimal operator +(decimal d)
        {
            return d;
        }

        public static decimal operator -(decimal d)
        {
            return Negate(d);
        }

        public static decimal operator ++(decimal d)
        {
            return Add(d, One);
        }

        public static decimal operator --(decimal d)
        {
            return Subtract(d, One);
        }

        public static decimal operator +(decimal d1, decimal d2)
        {
            FCallAddSub(ref d1, ref d2, DECIMAL_ADD);
            return d1;
        }

        public static decimal operator -(decimal d1, decimal d2)
        {
            FCallAddSub(ref d1, ref d2, DECIMAL_NEG);
            return d1;
        }

        public static decimal operator *(decimal d1, decimal d2)
        {
            FCallMultiply(ref d1, ref d2);
            return d1;
        }

        public static decimal operator /(decimal d1, decimal d2)
        {
            FCallDivide(ref d1, ref d2);
            return d1;
        }

        public static decimal operator %(decimal d1, decimal d2)
        {
            return Remainder(d1, d2);
        }

        public static bool operator ==(decimal d1, decimal d2)
        {
            return FCallCompare(ref d1, ref d2) == 0;
        }

        public static bool operator !=(decimal d1, decimal d2)
        {
            return FCallCompare(ref d1, ref d2) != 0;
        }

        public static bool operator <(decimal d1, decimal d2)
        {
            return FCallCompare(ref d1, ref d2) < 0;
        }

        public static bool operator <=(decimal d1, decimal d2)
        {
            return FCallCompare(ref d1, ref d2) <= 0;
        }

        public static bool operator >(decimal d1, decimal d2)
        {
            return FCallCompare(ref d1, ref d2) > 0;
        }

        public static bool operator >=(decimal d1, decimal d2)
        {
            return FCallCompare(ref d1, ref d2) >= 0;
        }

        //
        // IConvertible implementation
        //

        public TypeCode GetTypeCode()
        {
            return TypeCode.Decimal;
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            return Convert.ToBoolean(this);
        }


        char IConvertible.ToChar(IFormatProvider provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Decimal", "Char"));
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            return Convert.ToSByte(this);
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            return Convert.ToByte(this);
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            return Convert.ToInt16(this);
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            return Convert.ToUInt16(this);
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            return Convert.ToInt32(this);
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            return Convert.ToUInt32(this);
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            return Convert.ToInt64(this);
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            return Convert.ToUInt64(this);
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            return Convert.ToSingle(this);
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            return Convert.ToDouble(this);
        }

        decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            return this;
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Decimal", "DateTime"));
        }

        object IConvertible.ToType(Type type, IFormatProvider provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }
    }
}
