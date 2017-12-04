// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: A representation of an IEEE double precision
**          floating point number.
**
**
===========================================================*/

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public struct Double : IComparable, IConvertible, IFormattable, IComparable<Double>, IEquatable<Double>, ISpanFormattable
    {
        private double m_value; // Do not rename (binary serialization)

        //
        // Public Constants
        //
        public const double MinValue = -1.7976931348623157E+308;
        public const double MaxValue = 1.7976931348623157E+308;

        // Note Epsilon should be a double whose hex representation is 0x1
        // on little endian machines.
        public const double Epsilon = 4.9406564584124654E-324;
        public const double NegativeInfinity = (double)-1.0 / (double)(0.0);
        public const double PositiveInfinity = (double)1.0 / (double)(0.0);
        public const double NaN = (double)0.0 / (double)0.0;

        // We use this explicit definition to avoid the confusion between 0.0 and -0.0.
        internal const double NegativeZero = -0.0;

        /// <summary>Determines whether the specified value is finite (zero, subnormal, or normal).</summary>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static bool IsFinite(double d)
        {
            var bits = BitConverter.DoubleToInt64Bits(d);
            return (bits & 0x7FFFFFFFFFFFFFFF) < 0x7FF0000000000000;
        }

        /// <summary>Determines whether the specified value is infinite.</summary>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static bool IsInfinity(double d)
        {
            var bits = BitConverter.DoubleToInt64Bits(d);
            return (bits & 0x7FFFFFFFFFFFFFFF) == 0x7FF0000000000000;
        }

        /// <summary>Determines whether the specified value is NaN.</summary>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static bool IsNaN(double d)
        {
            var bits = BitConverter.DoubleToInt64Bits(d);
            return (bits & 0x7FFFFFFFFFFFFFFF) > 0x7FF0000000000000;
        }

        /// <summary>Determines whether the specified value is negative.</summary>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static bool IsNegative(double d)
        {
            var bits = unchecked((ulong)BitConverter.DoubleToInt64Bits(d));
            return (bits & 0x8000000000000000) == 0x8000000000000000;
        }

        /// <summary>Determines whether the specified value is negative infinity.</summary>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNegativeInfinity(double d)
        {
            return (d == double.NegativeInfinity);
        }

        /// <summary>Determines whether the specified value is normal.</summary>
        [NonVersionable]
        // This is probably not worth inlining, it has branches and should be rarely called
        public unsafe static bool IsNormal(double d)
        {
            var bits = BitConverter.DoubleToInt64Bits(d);
            bits &= 0x7FFFFFFFFFFFFFFF;
            return (bits < 0x7FF0000000000000) && (bits != 0) && ((bits & 0x7FF0000000000000) != 0);
        }

        /// <summary>Determines whether the specified value is positive infinity.</summary>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPositiveInfinity(double d)
        {
            return (d == double.PositiveInfinity);
        }

        /// <summary>Determines whether the specified value is subnormal.</summary>
        [NonVersionable]
        // This is probably not worth inlining, it has branches and should be rarely called
        public unsafe static bool IsSubnormal(double d)
        {
            var bits = BitConverter.DoubleToInt64Bits(d);
            bits &= 0x7FFFFFFFFFFFFFFF;
            return (bits < 0x7FF0000000000000) && (bits != 0) && ((bits & 0x7FF0000000000000) == 0);
        }

        // Compares this object to another object, returning an instance of System.Relation.
        // Null is considered less than any instance.
        //
        // If object is not of type Double, this method throws an ArgumentException.
        //
        // Returns a value less than zero if this  object
        //
        public int CompareTo(Object value)
        {
            if (value == null)
            {
                return 1;
            }
            if (value is Double)
            {
                double d = (double)value;
                if (m_value < d) return -1;
                if (m_value > d) return 1;
                if (m_value == d) return 0;

                // At least one of the values is NaN.
                if (IsNaN(m_value))
                    return (IsNaN(d) ? 0 : -1);
                else
                    return 1;
            }
            throw new ArgumentException(SR.Arg_MustBeDouble);
        }

        public int CompareTo(Double value)
        {
            if (m_value < value) return -1;
            if (m_value > value) return 1;
            if (m_value == value) return 0;

            // At least one of the values is NaN.
            if (IsNaN(m_value))
                return (IsNaN(value) ? 0 : -1);
            else
                return 1;
        }

        // True if obj is another Double with the same value as the current instance.  This is
        // a method of object equality, that only returns true if obj is also a double.
        public override bool Equals(Object obj)
        {
            if (!(obj is Double))
            {
                return false;
            }
            double temp = ((Double)obj).m_value;
            // This code below is written this way for performance reasons i.e the != and == check is intentional.
            if (temp == m_value)
            {
                return true;
            }
            return IsNaN(temp) && IsNaN(m_value);
        }

        [NonVersionable]
        public static bool operator ==(Double left, Double right)
        {
            return left == right;
        }

        [NonVersionable]
        public static bool operator !=(Double left, Double right)
        {
            return left != right;
        }

        [NonVersionable]
        public static bool operator <(Double left, Double right)
        {
            return left < right;
        }

        [NonVersionable]
        public static bool operator >(Double left, Double right)
        {
            return left > right;
        }

        [NonVersionable]
        public static bool operator <=(Double left, Double right)
        {
            return left <= right;
        }

        [NonVersionable]
        public static bool operator >=(Double left, Double right)
        {
            return left >= right;
        }

        public bool Equals(Double obj)
        {
            if (obj == m_value)
            {
                return true;
            }
            return IsNaN(obj) && IsNaN(m_value);
        }

        //The hashcode for a double is the absolute value of the integer representation
        //of that double.
        //
        public unsafe override int GetHashCode()
        {
            double d = m_value;
            if (d == 0)
            {
                // Ensure that 0 and -0 have the same hash code
                return 0;
            }
            long value = *(long*)(&d);
            return unchecked((int)value) ^ ((int)(value >> 32));
        }

        public override String ToString()
        {
            return Number.FormatDouble(m_value, null, NumberFormatInfo.CurrentInfo);
        }

        public String ToString(String format)
        {
            return Number.FormatDouble(m_value, format, NumberFormatInfo.CurrentInfo);
        }

        public String ToString(IFormatProvider provider)
        {
            return Number.FormatDouble(m_value, null, NumberFormatInfo.GetInstance(provider));
        }

        public String ToString(String format, IFormatProvider provider)
        {
            return Number.FormatDouble(m_value, format, NumberFormatInfo.GetInstance(provider));
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider provider = null)
        {
            return Number.TryFormatDouble(m_value, format, NumberFormatInfo.GetInstance(provider), destination, out charsWritten);
        }

        public static double Parse(String s)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseDouble(s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.CurrentInfo);
        }

        public static double Parse(String s, NumberStyles style)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseDouble(s, style, NumberFormatInfo.CurrentInfo);
        }

        public static double Parse(String s, IFormatProvider provider)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseDouble(s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.GetInstance(provider));
        }

        public static double Parse(String s, NumberStyles style, IFormatProvider provider)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseDouble(s, style, NumberFormatInfo.GetInstance(provider));
        }

        // Parses a double from a String in the given style.  If
        // a NumberFormatInfo isn't specified, the current culture's
        // NumberFormatInfo is assumed.
        //
        // This method will not throw an OverflowException, but will return
        // PositiveInfinity or NegativeInfinity for a number that is too
        // large or too small.

        public static double Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Number.ParseDouble(s, style, NumberFormatInfo.GetInstance(provider));
        }



        public static bool TryParse(String s, out double result)
        {
            if (s == null)
            {
                result = 0;
                return false;
            }

            return TryParse((ReadOnlySpan<char>)s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse(ReadOnlySpan<char> s, out double result)
        {
            return TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse(String s, NumberStyles style, IFormatProvider provider, out double result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);

            if (s == null)
            {
                result = 0;
                return false;
            }

            return TryParse((ReadOnlySpan<char>)s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out double result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return TryParse(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        private static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info, out double result)
        {
            bool success = Number.TryParseDouble(s, style, info, out result);
            if (!success)
            {
                ReadOnlySpan<char> sTrim = StringSpanHelpers.Trim(s);
                if (StringSpanHelpers.Equals(sTrim, info.PositiveInfinitySymbol))
                {
                    result = PositiveInfinity;
                }
                else if (StringSpanHelpers.Equals(sTrim, info.NegativeInfinitySymbol))
                {
                    result = NegativeInfinity;
                }
                else if (StringSpanHelpers.Equals(sTrim, info.NaNSymbol))
                {
                    result = NaN;
                }
                else
                {
                    return false; // We really failed
                }
            }
            return true;
        }

        //
        // IConvertible implementation
        //

        public TypeCode GetTypeCode()
        {
            return TypeCode.Double;
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            return Convert.ToBoolean(m_value);
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Double", "Char"));
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            return Convert.ToSByte(m_value);
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            return Convert.ToByte(m_value);
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            return Convert.ToInt16(m_value);
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            return Convert.ToUInt16(m_value);
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            return Convert.ToInt32(m_value);
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            return Convert.ToUInt32(m_value);
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            return Convert.ToInt64(m_value);
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            return Convert.ToUInt64(m_value);
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            return Convert.ToSingle(m_value);
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            return m_value;
        }

        Decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            return Convert.ToDecimal(m_value);
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Double", "DateTime"));
        }

        Object IConvertible.ToType(Type type, IFormatProvider provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }
    }
}
