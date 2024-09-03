// plist-cil - An open source library to parse and generate property lists for .NET
// Copyright (C) 2015 Natalia Portillo
//
// This code is based on:
// plist - An open source library to parse and generate property lists
// Copyright (C) 2014 Daniel Dreibrodt
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Globalization;
using System.Text;

namespace Claunia.PropertyList
{
    /// <summary>A number whose value is either an integer, a real number or bool.</summary>
    /// @author Daniel Dreibrodt
    /// @author Natalia Portillo
    public class NSNumber : NSObject, IComparable
    {
        /// <summary>
        ///     Indicates that the number's value is an integer. The number is stored as a .NET <see cref="long" />. Its
        ///     original value could have been char, short, int, long or even long long.
        /// </summary>
        public const int INTEGER = 0;

        /// <summary>
        ///     Indicates that the number's value is a real number. The number is stored as a .NET <see cref="double" />. Its
        ///     original value could have been float or double.
        /// </summary>
        public const int REAL = 1;

        /// <summary>Indicates that the number's value is bool.</summary>
        public const int BOOLEAN = 2;
        readonly bool   boolValue;
        readonly double doubleValue;

        readonly long longValue;

        //Holds the current type of this number
        readonly int type;

        /// <summary>
        ///     Parses integers and real numbers from their binary representation.
        ///     <i>Note: real numbers are not yet supported.</i>
        /// </summary>
        /// <param name="bytes">The binary representation</param>
        /// <param name="type">The type of number</param>
        /// <seealso cref="INTEGER" />
        /// <seealso cref="REAL" />
        public NSNumber(ReadOnlySpan<byte> bytes, int type)
        {
            switch(type)
            {
                case INTEGER:
                    doubleValue = longValue = BinaryPropertyListParser.ParseLong(bytes);

                    break;

                case REAL:
                    doubleValue = BinaryPropertyListParser.ParseDouble(bytes);
                    longValue   = (long)Math.Round(doubleValue);

                    break;

                default: throw new ArgumentException("Type argument is not valid.", nameof(type));
            }

            this.type = type;
        }

        public NSNumber(string text, int type)
        {
            switch(type)
            {
                case INTEGER:
                {
                    doubleValue = longValue = long.Parse(text, CultureInfo.InvariantCulture);

                    break;
                }
                case REAL:
                {
                    doubleValue = double.Parse(text, CultureInfo.InvariantCulture);
                    longValue   = (long)Math.Round(doubleValue);

                    break;
                }
                default:
                {
                    throw new ArgumentException("Type argument is not valid.");
                }
            }

            this.type = type;
        }

        /// <summary>Creates a number from its textual representation.</summary>
        /// <param name="text">The textual representation of the number.</param>
        /// <seealso cref="bool.Parse(string)" />
        /// <seealso cref="long.Parse(string)" />
        /// <seealso cref="double.Parse(string, IFormatProvider)" />
        public NSNumber(string text)
        {
            if(text == null)
                throw new ArgumentException("The given string is null and cannot be parsed as number.");

            if(text.StartsWith("0x") &&
               long.TryParse(text.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture,
                             out long l))
            {
                doubleValue = longValue = l;
                type        = INTEGER;
            }
            else if(long.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out l))
            {
                doubleValue = longValue = l;
                type        = INTEGER;
            }
            else if(double.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out double d))
            {
                doubleValue = d;
                longValue   = (long)Math.Round(doubleValue);
                type        = REAL;
            }
            else
            {
                bool isTrue = string.Equals(text, "true", StringComparison.CurrentCultureIgnoreCase) ||
                              string.Equals(text, "yes", StringComparison.CurrentCultureIgnoreCase);

                bool isFalse = string.Equals(text, "false", StringComparison.CurrentCultureIgnoreCase) ||
                               string.Equals(text, "no", StringComparison.CurrentCultureIgnoreCase);

                if(isTrue || isFalse)
                {
                    type        = BOOLEAN;
                    boolValue   = isTrue;
                    doubleValue = longValue = boolValue ? 1 : 0;
                }
                else
                    throw new
                        ArgumentException("The given string neither represents a double, an int nor a bool value.");
            }
        }

        /// <summary>Creates an integer number.</summary>
        /// <param name="i">The integer value.</param>
        public NSNumber(int i)
        {
            doubleValue = longValue = i;
            type        = INTEGER;
        }

        /// <summary>Creates an integer number.</summary>
        /// <param name="l">The long integer value.</param>
        public NSNumber(long l)
        {
            doubleValue = longValue = l;
            type        = INTEGER;
        }

        /// <summary>Creates a real number.</summary>
        /// <param name="d">The real value.</param>
        public NSNumber(double d)
        {
            longValue = (long)(doubleValue = d);
            type      = REAL;
        }

        /// <summary>Creates a bool number.</summary>
        /// <param name="b">The bool value.</param>
        public NSNumber(bool b)
        {
            boolValue   = b;
            doubleValue = longValue = b ? 1 : 0;
            type        = BOOLEAN;
        }

        /// <summary>Compares the current <see cref="Claunia.PropertyList.NSNumber" /> to the specified object.</summary>
        /// <returns>
        ///     0 if the numbers are equal, 1 if the current <see cref="Claunia.PropertyList.NSNumber" /> is greater than the
        ///     argument and -1 if it is less, or the argument is not a number.
        /// </returns>
        /// <param name="o">Object to compare to the current <see cref="Claunia.PropertyList.NSNumber" />.</param>
        public int CompareTo(object o)
        {
            double x = ToDouble();
            double y;

            if(o is NSNumber num)
            {
                y = num.ToDouble();

                return x < y
                           ? -1
                           : x == y
                               ? 0
                               : 1;
            }

            if(!IsNumber(o))
                return -1;

            y = GetDoubleFromObject(o);

            return x < y
                       ? -1
                       : x == y
                           ? 0
                           : 1;
        }

        /// <summary>Gets the type of this number's value.</summary>
        /// <returns>The type flag.</returns>
        /// <seealso cref="BOOLEAN" />
        /// <seealso cref="INTEGER" />
        /// <seealso cref="REAL" />
        public int GetNSNumberType() => type;

        /// <summary>Checks whether the value of this NSNumber is a bool.</summary>
        /// <returns>Whether the number's value is a bool.</returns>
        public bool isBoolean() => type == BOOLEAN;

        /// <summary>Checks whether the value of this NSNumber is an integer.</summary>
        /// <returns>Whether the number's value is an integer.</returns>
        public bool isInteger() => type == INTEGER;

        /// <summary>Checks whether the value of this NSNumber is a real number.</summary>
        /// <returns>Whether the number's value is a real number.</returns>
        public bool isReal() => type == REAL;

        /// <summary>The number's bool value.</summary>
        /// <returns><c>true</c> if the value is true or non-zero, <c>false</c> otherwise.</returns>
        public bool ToBool()
        {
            if(type == BOOLEAN)
                return boolValue;

            return longValue != 0;
        }

        /// <summary>The number's long value.</summary>
        /// <returns>The value of the number as long</returns>
        public long ToLong() => longValue;

        /// <summary>
        ///     The number's int value.
        ///     <i>
        ///         Note: Even though the number's type might be INTEGER it can be larger than a Java int. Use intValue() only if
        ///         you are certain that it contains a number from the int range. Otherwise the value might be inaccurate.
        ///     </i>
        /// </summary>
        /// <returns>The value of the number as int.</returns>
        public int ToInt() => (int)longValue;

        /// <summary>The number's double value.</summary>
        /// <returns>The value of the number as double.</returns>
        public double ToDouble() => doubleValue;

        /// <summary>The number's float value. WARNING: Possible loss of precision if the value is outside the float range.</summary>
        /// <returns>The value of the number as float.</returns>
        public float floatValue() => (float)doubleValue;

        /// <summary>Checks whether the other object is a NSNumber of the same value.</summary>
        /// <param name="obj">The object to compare to.</param>
        /// <returns>Whether the objects are equal in terms of numeric value and type.</returns>
        public override bool Equals(object obj)
        {
            if(obj is not NSNumber number)
                return false;

            return type      == number.type && longValue == number.longValue && doubleValue == number.doubleValue &&
                   boolValue == number.boolValue;
        }

        /// <summary>Serves as a hash function for a <see cref="Claunia.PropertyList.NSNumber" /> object.</summary>
        /// <returns>
        ///     A hash code for this instance that is suitable for use in hashing algorithms and data structures such as a
        ///     hash table.
        /// </returns>
        public override int GetHashCode()
        {
            int hash = type;
            hash = (37 * hash) + (int)(longValue ^ ((uint)longValue >> 32));

            hash = (37 * hash) + (int)(BitConverter.DoubleToInt64Bits(doubleValue) ^
                                       (uint)(BitConverter.DoubleToInt64Bits(doubleValue) >> 32));

            hash = (37 * hash) + (ToBool() ? 1 : 0);

            return hash;
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents the current
        ///     <see cref="Claunia.PropertyList.NSNumber" />.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents the current <see cref="Claunia.PropertyList.NSNumber" />.</returns>
        public override string ToString() => type switch
        {
            INTEGER => ToLong().ToString(),
            REAL    => ToDouble().ToString("R", CultureInfo.InvariantCulture),
            BOOLEAN => ToBool().ToString(),
            _       => base.ToString()
        };

        internal override void ToXml(StringBuilder xml, int level)
        {
            Indent(xml, level);

            switch(type)
            {
                case INTEGER:
                {
                    xml.Append("<integer>");
                    xml.Append(ToLong());
                    xml.Append("</integer>");

                    break;
                }
                case REAL:
                {
                    xml.Append("<real>");

                    if(doubleValue == 0)
                        xml.Append("0.0");
                    else
                        xml.Append(ToDouble().ToString("R", CultureInfo.InvariantCulture));

                    xml.Append("</real>");

                    break;
                }
                case BOOLEAN:
                {
                    xml.Append(ToBool() ? "<true/>" : "<false/>");

                    break;
                }
            }
        }

        internal override void ToBinary(BinaryPropertyListWriter outPlist)
        {
            switch(GetNSNumberType())
            {
                case INTEGER:
                {
                    if(ToLong() < 0)
                    {
                        outPlist.Write(0x13);
                        outPlist.WriteBytes(ToLong(), 8);
                    }
                    else if(ToLong() <= 0xff)
                    {
                        outPlist.Write(0x10);
                        outPlist.WriteBytes(ToLong(), 1);
                    }
                    else if(ToLong() <= 0xffff)
                    {
                        outPlist.Write(0x11);
                        outPlist.WriteBytes(ToLong(), 2);
                    }
                    else if(ToLong() <= 0xffffffffL)
                    {
                        outPlist.Write(0x12);
                        outPlist.WriteBytes(ToLong(), 4);
                    }
                    else
                    {
                        outPlist.Write(0x13);
                        outPlist.WriteBytes(ToLong(), 8);
                    }

                    break;
                }
                case REAL:
                {
                    outPlist.Write(0x23);
                    outPlist.WriteDouble(ToDouble());

                    break;
                }
                case BOOLEAN:
                {
                    outPlist.Write(ToBool() ? 0x09 : 0x08);

                    break;
                }
            }
        }

        internal override void ToASCII(StringBuilder ascii, int level)
        {
            Indent(ascii, level);

            if(type == BOOLEAN)
                ascii.Append(boolValue ? "YES" : "NO");
            else
                ascii.Append(ToString());
        }

        internal override void ToASCIIGnuStep(StringBuilder ascii, int level)
        {
            Indent(ascii, level);

            switch(type)
            {
                case INTEGER:
                {
                    ascii.Append("<*I");
                    ascii.Append(ToString());
                    ascii.Append(">");

                    break;
                }
                case REAL:
                {
                    ascii.Append("<*R");
                    ascii.Append(ToString());
                    ascii.Append(">");

                    break;
                }
                case BOOLEAN:
                {
                    ascii.Append(boolValue ? "<*BY>" : "<*BN>");

                    break;
                }
            }
        }

        /// <summary>Determines if an object is a number. Substitutes .NET's Number class comparison</summary>
        /// <returns><c>true</c> if it is a number.</returns>
        /// <param name="o">Object.</param>
        static bool IsNumber(object o) =>
            o is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;

        static double GetDoubleFromObject(object o) => o switch
        {
            sbyte @sbyte     => @sbyte,
            byte b           => b,
            short s          => s,
            ushort @ushort   => @ushort,
            int i            => i,
            uint u           => u,
            long l           => l,
            ulong @ulong     => @ulong,
            float f          => f,
            double d         => d,
            decimal @decimal => (double)@decimal,
            _                => 0
        };

        /// <summary>
        ///     Determines whether the specified <see cref="Claunia.PropertyList.NSObject" /> is equal to the current
        ///     <see cref="Claunia.PropertyList.NSNumber" />.
        /// </summary>
        /// <param name="obj">
        ///     The <see cref="Claunia.PropertyList.NSObject" /> to compare with the current
        ///     <see cref="Claunia.PropertyList.NSNumber" />.
        /// </param>
        /// <returns>
        ///     <c>true</c> if the specified <see cref="Claunia.PropertyList.NSObject" /> is equal to the current
        ///     <see cref="Claunia.PropertyList.NSNumber" />; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(NSObject obj)
        {
            if(obj is not NSNumber number)
                return false;

            if(number.GetNSNumberType() != type)
                return false;

            return type switch
            {
                INTEGER => longValue   == number.ToLong(),
                REAL    => doubleValue == number.ToDouble(),
                BOOLEAN => boolValue   == number.ToBool(),
                _       => false
            };
        }

        public static explicit operator ulong(NSNumber value) => (ulong)value.longValue;

        public static explicit operator long(NSNumber value) => value.longValue;

        public static explicit operator uint(NSNumber value) => (uint)value.longValue;

        public static explicit operator int(NSNumber value) => (int)value.longValue;

        public static explicit operator ushort(NSNumber value) => (ushort)value.longValue;

        public static explicit operator short(NSNumber value) => (short)value.longValue;

        public static explicit operator byte(NSNumber value) => (byte)value.longValue;

        public static explicit operator sbyte(NSNumber value) => (sbyte)value.longValue;

        public static explicit operator double(NSNumber value) => value.doubleValue;

        public static explicit operator float(NSNumber value) => (float)value.doubleValue;

        public static explicit operator bool(NSNumber value) => value.boolValue;

        public static explicit operator NSNumber(ulong value) => new(value);

        public static explicit operator NSNumber(long value) => new(value);

        public static explicit operator NSNumber(uint value) => new(value);

        public static explicit operator NSNumber(int value) => new(value);

        public static explicit operator NSNumber(ushort value) => new(value);

        public static explicit operator NSNumber(short value) => new(value);

        public static explicit operator NSNumber(byte value) => new(value);

        public static explicit operator NSNumber(sbyte value) => new(value);

        public static explicit operator NSNumber(double value) => new(value);

        public static explicit operator NSNumber(float value) => new(value);

        public static explicit operator NSNumber(bool value) => new(value);
    }
}