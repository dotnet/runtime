// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using System.Xml;
using System.Xml.Schema;

namespace System.Xml
{
    internal static class XmlConverter
    {
        public const int MaxDateTimeChars = 64;
        public const int MaxInt32Chars = 16;
        public const int MaxInt64Chars = 32;
        public const int MaxBoolChars = 5;
        public const int MaxFloatChars = 16;
        public const int MaxDoubleChars = 32;
        public const int MaxDecimalChars = 40;
        public const int MaxUInt64Chars = 32;
        public const int MaxPrimitiveChars = MaxDateTimeChars;

        // Matches IsWhitespace below
        private static readonly SearchValues<char> s_whitespaceChars = SearchValues.Create(" \t\r\n");
        private static readonly SearchValues<byte> s_whitespaceBytes = SearchValues.Create(" \t\r\n"u8);

        public static bool ToBoolean(string value)
        {
            try
            {
                return XmlConvert.ToBoolean(value);
            }
            catch (ArgumentException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "Boolean", exception);
            }
            catch (FormatException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "Boolean", exception);
            }
        }

        public static bool ToBoolean(byte[] buffer, int offset, int count)
        {
            if (count == 1)
            {
                byte ch = buffer[offset];
                if (ch == (byte)'1')
                    return true;
                else if (ch == (byte)'0')
                    return false;
            }
            return ToBoolean(ToString(buffer, offset, count));
        }

        public static int ToInt32(string value)
        {
            try
            {
                return XmlConvert.ToInt32(value);
            }
            catch (ArgumentException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "Int32", exception);
            }
            catch (FormatException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "Int32", exception);
            }
            catch (OverflowException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "Int32", exception);
            }
        }

        public static int ToInt32(byte[] buffer, int offset, int count)
        {
            int value;
            if (TryParseInt32(buffer, offset, count, out value))
                return value;
            return ToInt32(ToString(buffer, offset, count));
        }

        public static long ToInt64(string value)
        {
            try
            {
                return XmlConvert.ToInt64(value);
            }
            catch (ArgumentException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "Int64", exception);
            }
            catch (FormatException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "Int64", exception);
            }
            catch (OverflowException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "Int64", exception);
            }
        }

        public static long ToInt64(byte[] buffer, int offset, int count)
        {
            long value;
            if (TryParseInt64(buffer, offset, count, out value))
                return value;
            return ToInt64(ToString(buffer, offset, count));
        }

        public static float ToSingle(string value)
        {
            try
            {
                return XmlConvert.ToSingle(value);
            }
            catch (ArgumentException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "float", exception);
            }
            catch (FormatException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "float", exception);
            }
            catch (OverflowException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "float", exception);
            }
        }

        public static float ToSingle(byte[] buffer, int offset, int count)
        {
            float value;
            if (TryParseSingle(buffer, offset, count, out value))
                return value;
            return ToSingle(ToString(buffer, offset, count));
        }

        public static double ToDouble(string value)
        {
            try
            {
                return XmlConvert.ToDouble(value);
            }
            catch (ArgumentException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "double", exception);
            }
            catch (FormatException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "double", exception);
            }
            catch (OverflowException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "double", exception);
            }
        }

        public static double ToDouble(byte[] buffer, int offset, int count)
        {
            double value;
            if (TryParseDouble(buffer, offset, count, out value))
                return value;
            return ToDouble(ToString(buffer, offset, count));
        }

        public static decimal ToDecimal(string value)
        {
            try
            {
                return XmlConvert.ToDecimal(value);
            }
            catch (ArgumentException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "decimal", exception);
            }
            catch (FormatException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "decimal", exception);
            }
            catch (OverflowException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "decimal", exception);
            }
        }

        public static decimal ToDecimal(byte[] buffer, int offset, int count)
        {
            return ToDecimal(ToString(buffer, offset, count));
        }

        public static DateTime ToDateTime(long value)
        {
            try
            {
                return DateTime.FromBinary(value);
            }
            catch (ArgumentException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(ToString(value), "DateTime", exception);
            }
        }

        public static DateTime ToDateTime(string value)
        {
            try
            {
                return XmlConvert.ToDateTime(value, XmlDateTimeSerializationMode.RoundtripKind);
            }
            catch (ArgumentException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "DateTime", exception);
            }
            catch (FormatException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "DateTime", exception);
            }
        }

        public static DateTime ToDateTime(byte[] buffer, int offset, int count)
        {
            DateTime value;
            if (TryParseDateTime(buffer, offset, count, out value))
                return value;
            return ToDateTime(ToString(buffer, offset, count));
        }

        public static UniqueId ToUniqueId(string value)
        {
            try
            {
                return new UniqueId(value.Trim());
            }
            catch (ArgumentException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "UniqueId", exception);
            }
            catch (FormatException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "UniqueId", exception);
            }
        }

        public static UniqueId ToUniqueId(byte[] buffer, int offset, int count)
        {
            return ToUniqueId(ToString(buffer, offset, count));
        }

        public static TimeSpan ToTimeSpan(string value)
        {
            try
            {
                return XmlConvert.ToTimeSpan(value);
            }
            catch (ArgumentException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "TimeSpan", exception);
            }
            catch (FormatException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "TimeSpan", exception);
            }
            catch (OverflowException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "TimeSpan", exception);
            }
        }

        public static TimeSpan ToTimeSpan(byte[] buffer, int offset, int count)
        {
            return ToTimeSpan(ToString(buffer, offset, count));
        }

        public static Guid ToGuid(string value)
        {
            try
            {
                return new Guid(value.Trim());
            }
            catch (FormatException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "Guid", exception);
            }
            catch (ArgumentException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "Guid", exception);
            }
            catch (OverflowException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "Guid", exception);
            }
        }

        public static Guid ToGuid(byte[] buffer, int offset, int count)
        {
            return ToGuid(ToString(buffer, offset, count));
        }

        public static ulong ToUInt64(string value)
        {
            try
            {
                return ulong.Parse(value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo);
            }
            catch (ArgumentException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "UInt64", exception);
            }
            catch (FormatException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "UInt64", exception);
            }
            catch (OverflowException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "UInt64", exception);
            }
        }

        public static ulong ToUInt64(byte[] buffer, int offset, int count)
        {
            return ToUInt64(ToString(buffer, offset, count));
        }

        public static string ToString(byte[] buffer, int offset, int count)
        {
            try
            {
                return DataContractSerializer.ValidatingUTF8.GetString(buffer, offset, count);
            }
            catch (DecoderFallbackException exception)
            {
                throw XmlExceptionHelper.CreateEncodingException(buffer, offset, count, exception);
            }
        }

        public static string ToStringUnicode(byte[] buffer, int offset, int count)
        {
            try
            {
                return DataContractSerializer.ValidatingUTF16.GetString(buffer, offset, count);
            }
            catch (DecoderFallbackException exception)
            {
                throw XmlExceptionHelper.CreateEncodingException(buffer, offset, count, exception);
            }
        }


        public static byte[] ToBytes(string value)
        {
            try
            {
                return DataContractSerializer.ValidatingUTF8.GetBytes(value);
            }
            catch (DecoderFallbackException exception)
            {
                throw XmlExceptionHelper.CreateEncodingException(value, exception);
            }
        }

        public static int ToChars(byte[] buffer, int offset, int count, char[] chars, int charOffset)
        {
            try
            {
                return DataContractSerializer.ValidatingUTF8.GetChars(buffer, offset, count, chars, charOffset);
            }
            catch (DecoderFallbackException exception)
            {
                throw XmlExceptionHelper.CreateEncodingException(buffer, offset, count, exception);
            }
        }

        public static string ToString(bool value) { return value ? "true" : "false"; }
        public static string ToString(int value) { return XmlConvert.ToString(value); }
        public static string ToString(long value) { return XmlConvert.ToString(value); }
        public static string ToString(float value) { return XmlConvert.ToString(value); }
        public static string ToString(double value) { return XmlConvert.ToString(value); }
        public static string ToString(decimal value) { return XmlConvert.ToString(value); }
        public static string ToString(TimeSpan value) { return XmlConvert.ToString(value); }
        public static string ToString(UniqueId value) { return value.ToString(); }
        public static string ToString(Guid value) { return value.ToString(); }
        public static string ToString(ulong value) { return value.ToString(NumberFormatInfo.InvariantInfo); }

        public static string ToString(DateTime value)
        {
            byte[] dateChars = new byte[MaxDateTimeChars];
            int count = ToChars(value, dateChars, 0);
            return ToString(dateChars, 0, count);
        }

        private static string ToString(object value)
        {
            if (value is int)
                return ToString((int)value);
            else if (value is long)
                return ToString((long)value);
            else if (value is float)
                return ToString((float)value);
            else if (value is double)
                return ToString((double)value);
            else if (value is decimal)
                return ToString((decimal)value);
            else if (value is TimeSpan)
                return ToString((TimeSpan)value);
            else if (value is UniqueId)
                return ToString((UniqueId)value);
            else if (value is Guid)
                return ToString((Guid)value);
            else if (value is ulong)
                return ToString((ulong)value);
            else if (value is DateTime)
                return ToString((DateTime)value);
            else if (value is bool)
                return ToString((bool)value);
            else
                return value.ToString()!; // value can only be an object created by ToList()
        }

        public static string ToString(object[] objects)
        {
            if (objects.Length == 0)
                return string.Empty;
            string value = ToString(objects[0]);
            if (objects.Length > 1)
            {
                StringBuilder sb = new StringBuilder(value);
                for (int i = 1; i < objects.Length; i++)
                {
                    sb.Append(' ');
                    sb.Append(ToString(objects[i]));
                }
                value = sb.ToString();
            }
            return value;
        }

        public static void ToQualifiedName(string qname, out string prefix, out string localName)
        {
            int index = qname.IndexOf(':');
            if (index < 0)
            {
                prefix = string.Empty;
                localName = qname.Trim();
            }
            else
            {
                if (index == qname.Length - 1)
                    throw new XmlException(SR.Format(SR.XmlInvalidQualifiedName, qname));
                prefix = qname.Substring(0, index).Trim();
                localName = qname.Substring(index + 1).Trim();
            }
        }

        private static bool TryParseInt32(byte[] chars, int offset, int count, out int result)
        {
            result = 0;
            if (count == 0)
                return false;
            int value = 0;
            int offsetMax = offset + count;
            if (chars[offset] == '-')
            {
                if (count == 1)
                    return false;
                for (int i = offset + 1; i < offsetMax; i++)
                {
                    int digit = (chars[i] - '0');
                    if ((uint)digit > 9)
                        return false;
                    if (value < int.MinValue / 10)
                        return false;
                    value *= 10;
                    if (value < int.MinValue + digit)
                        return false;
                    value -= digit;
                }
            }
            else
            {
                for (int i = offset; i < offsetMax; i++)
                {
                    int digit = (chars[i] - '0');
                    if ((uint)digit > 9)
                        return false;
                    if (value > int.MaxValue / 10)
                        return false;
                    value *= 10;
                    if (value > int.MaxValue - digit)
                        return false;
                    value += digit;
                }
            }
            result = value;
            return true;
        }

        private static bool TryParseInt64(byte[] chars, int offset, int count, out long result)
        {
            result = 0;
            if (count < 11)
            {
                int value;
                if (!TryParseInt32(chars, offset, count, out value))
                    return false;
                result = value;
                return true;
            }
            else
            {
                long value = 0;
                int offsetMax = offset + count;
                if (chars[offset] == '-')
                {
                    for (int i = offset + 1; i < offsetMax; i++)
                    {
                        int digit = (chars[i] - '0');
                        if ((uint)digit > 9)
                            return false;
                        if (value < long.MinValue / 10)
                            return false;
                        value *= 10;
                        if (value < long.MinValue + digit)
                            return false;
                        value -= digit;
                    }
                }
                else
                {
                    for (int i = offset; i < offsetMax; i++)
                    {
                        int digit = (chars[i] - '0');
                        if ((uint)digit > 9)
                            return false;
                        if (value > long.MaxValue / 10)
                            return false;
                        value *= 10;
                        if (value > long.MaxValue - digit)
                            return false;
                        value += digit;
                    }
                }
                result = value;
                return true;
            }
        }

        private static bool TryParseSingle(byte[] chars, int offset, int count, out float result)
        {
            result = 0;
            int offsetMax = offset + count;
            bool negative = false;
            if (offset < offsetMax && chars[offset] == '-')
            {
                negative = true;
                offset++;
                count--;
            }
            if (count < 1 || count > 10)
                return false;
            int value = 0;
            int ch;
            while (offset < offsetMax)
            {
                ch = (chars[offset] - '0');
                if (ch == ('.' - '0'))
                {
                    offset++;
                    int pow10 = 1;
                    while (offset < offsetMax)
                    {
                        ch = chars[offset] - '0';
                        if (((uint)ch) >= 10)
                            return false;
                        pow10 *= 10;
                        value = value * 10 + ch;
                        offset++;
                    }
                    // More than 8 characters (7 sig figs and a decimal) and int -> float conversion is lossy, so use double
                    if (count > 8)
                    {
                        result = (float)((double)value / (double)pow10);
                    }
                    else
                    {
                        result = (float)value / (float)pow10;
                    }
                    if (negative)
                        result = -result;
                    return true;
                }
                else if (((uint)ch) >= 10)
                    return false;
                value = value * 10 + ch;
                offset++;
            }
            // Ten digits w/out a decimal point might have overflowed the int
            if (count == 10)
                return false;
            if (negative)
                result = -(float)value;
            else
                result = value;
            return true;
        }

        private static bool TryParseDouble(byte[] chars, int offset, int count, out double result)
        {
            result = 0;
            int offsetMax = offset + count;
            bool negative = false;
            if (offset < offsetMax && chars[offset] == '-')
            {
                negative = true;
                offset++;
                count--;
            }
            if (count < 1 || count > 10)
                return false;
            int value = 0;
            int ch;
            while (offset < offsetMax)
            {
                ch = (chars[offset] - '0');
                if (ch == ('.' - '0'))
                {
                    offset++;
                    int pow10 = 1;
                    while (offset < offsetMax)
                    {
                        ch = chars[offset] - '0';
                        if (((uint)ch) >= 10)
                            return false;
                        pow10 *= 10;
                        value = value * 10 + ch;
                        offset++;
                    }
                    if (negative)
                        result = -(double)value / pow10;
                    else
                        result = (double)value / pow10;
                    return true;
                }
                else if (((uint)ch) >= 10)
                    return false;
                value = value * 10 + ch;
                offset++;
            }
            // Ten digits w/out a decimal point might have overflowed the int
            if (count == 10)
                return false;
            if (negative)
                result = -(double)value;
            else
                result = value;
            return true;
        }

        public static int ToChars(int value, byte[] chars, int offset)
        {
            int count = ToCharsR(value, chars, offset + MaxInt32Chars);
            Buffer.BlockCopy(chars, offset + MaxInt32Chars - count, chars, offset, count);
            return count;
        }

        public static int ToChars(long value, byte[] chars, int offset)
        {
            int count = ToCharsR(value, chars, offset + MaxInt64Chars);
            Buffer.BlockCopy(chars, offset + MaxInt64Chars - count, chars, offset, count);
            return count;
        }

        public static int ToCharsR(long value, byte[] chars, int offset)
        {
            int count = 0;
            if (value >= 0)
            {
                while (value > int.MaxValue)
                {
                    long valueDiv10 = value / 10;
                    count++;
                    chars[--offset] = (byte)('0' + (int)(value - valueDiv10 * 10));
                    value = valueDiv10;
                }
            }
            else
            {
                while (value < int.MinValue)
                {
                    long valueDiv10 = value / 10;
                    count++;
                    chars[--offset] = (byte)('0' - (int)(value - valueDiv10 * 10));
                    value = valueDiv10;
                }
            }
            Debug.Assert(value >= int.MinValue && value <= int.MaxValue);
            return count + ToCharsR((int)value, chars, offset);
        }

        private static unsafe bool IsNegativeZero(float value)
        {
            // Simple equals function will report that -0 is equal to +0, so compare bits instead
            return BitConverter.SingleToUInt32Bits(value) == 0x8000_0000U;
        }

        private static unsafe bool IsNegativeZero(double value)
        {
            // Simple equals function will report that -0 is equal to +0, so compare bits instead
            return BitConverter.DoubleToUInt64Bits(value) == 0x8000_0000_0000_0000UL;
        }

        private static int ToInfinity(bool isNegative, byte[] buffer, int offset)
        {
            if (isNegative)
            {
                buffer[offset + 0] = (byte)'-';
                buffer[offset + 1] = (byte)'I';
                buffer[offset + 2] = (byte)'N';
                buffer[offset + 3] = (byte)'F';
                return 4;
            }
            else
            {
                buffer[offset + 0] = (byte)'I';
                buffer[offset + 1] = (byte)'N';
                buffer[offset + 2] = (byte)'F';
                return 3;
            }
        }

        private static int ToZero(bool isNegative, byte[] buffer, int offset)
        {
            if (isNegative)
            {
                buffer[offset + 0] = (byte)'-';
                buffer[offset + 1] = (byte)'0';
                return 2;
            }
            else
            {
                buffer[offset] = (byte)'0';
                return 1;
            }
        }

        public static int ToChars(double value, byte[] buffer, int offset)
        {
            if (double.IsInfinity(value))
                return ToInfinity(double.IsNegativeInfinity(value), buffer, offset);
            if (value == 0.0)
                return ToZero(IsNegativeZero(value), buffer, offset);
            return ToAsciiChars(value.ToString("R", NumberFormatInfo.InvariantInfo), buffer, offset);
        }

        public static int ToChars(float value, byte[] buffer, int offset)
        {
            if (float.IsInfinity(value))
                return ToInfinity(float.IsNegativeInfinity(value), buffer, offset);
            if (value == 0.0)
                return ToZero(IsNegativeZero(value), buffer, offset);
            return ToAsciiChars(value.ToString("R", NumberFormatInfo.InvariantInfo), buffer, offset);
        }

        public static int ToChars(decimal value, byte[] buffer, int offset)
        {
            return ToAsciiChars(value.ToString(null, NumberFormatInfo.InvariantInfo), buffer, offset);
        }

        public static int ToChars(ulong value, byte[] buffer, int offset)
        {
            return ToAsciiChars(value.ToString(null, NumberFormatInfo.InvariantInfo), buffer, offset);
        }

        private static int ToAsciiChars(string s, byte[] buffer, int offset)
        {
            for (int i = 0; i < s.Length; i++)
            {
                Debug.Assert(s[i] < 128);
                buffer[offset++] = (byte)s[i];
            }
            return s.Length;
        }

        public static int ToChars(bool value, byte[] buffer, int offset)
        {
            if (value)
            {
                buffer[offset + 0] = (byte)'t';
                buffer[offset + 1] = (byte)'r';
                buffer[offset + 2] = (byte)'u';
                buffer[offset + 3] = (byte)'e';
                return 4;
            }
            else
            {
                buffer[offset + 0] = (byte)'f';
                buffer[offset + 1] = (byte)'a';
                buffer[offset + 2] = (byte)'l';
                buffer[offset + 3] = (byte)'s';
                buffer[offset + 4] = (byte)'e';
                return 5;
            }
        }

        private static int ToInt32D2(byte[] chars, int offset)
        {
            byte ch1 = (byte)(chars[offset + 0] - '0');
            byte ch2 = (byte)(chars[offset + 1] - '0');
            if (ch1 > 9 || ch2 > 9)
                return -1;
            return 10 * ch1 + ch2;
        }

        private static int ToInt32D4(byte[] chars, int offset, int count)
        {
            return ToInt32D7(chars, offset, count);
        }

        private static int ToInt32D7(byte[] chars, int offset, int count)
        {
            int value = 0;
            for (int i = 0; i < count; i++)
            {
                byte ch = (byte)(chars[offset + i] - '0');
                if (ch > 9)
                    return -1;
                value = value * 10 + ch;
            }
            return value;
        }

        private static bool TryParseDateTime(byte[] chars, int offset, int count, out DateTime result)
        {
            int offsetMax = offset + count;
            result = DateTime.MaxValue;

            if (count < 19)
                return false;

            //            1         2         3
            //  012345678901234567890123456789012
            // "yyyy-MM-ddTHH:mm:ss"
            // "yyyy-MM-ddTHH:mm:ss.fffffff"
            // "yyyy-MM-ddTHH:mm:ss.fffffffZ"
            // "yyyy-MM-ddTHH:mm:ss.fffffff+xx:yy"
            // "yyyy-MM-ddTHH:mm:ss.fffffff-xx:yy"
            if (chars[offset + 4] != '-' || chars[offset + 7] != '-' || chars[offset + 10] != 'T' ||
                chars[offset + 13] != ':' || chars[offset + 16] != ':')
                return false;

            int year = ToInt32D4(chars, offset + 0, 4);
            int month = ToInt32D2(chars, offset + 5);
            int day = ToInt32D2(chars, offset + 8);
            int hour = ToInt32D2(chars, offset + 11);
            int minute = ToInt32D2(chars, offset + 14);
            int second = ToInt32D2(chars, offset + 17);

            if ((year | month | day | hour | minute | second) < 0)
                return false;

            DateTimeKind kind = DateTimeKind.Unspecified;
            offset += 19;

            int ticks = 0;
            if (offset < offsetMax && chars[offset] == '.')
            {
                offset++;
                int digitOffset = offset;
                while (offset < offsetMax)
                {
                    byte ch = chars[offset];
                    if (ch < '0' || ch > '9')
                        break;
                    offset++;
                }
                int digitCount = offset - digitOffset;
                if (digitCount < 1 || digitCount > 7)
                    return false;
                ticks = ToInt32D7(chars, digitOffset, digitCount);
                if (ticks < 0)
                    return false;
                for (int i = digitCount; i < 7; ++i)
                    ticks *= 10;
            }

            bool isLocal = false;
            int hourDelta = 0;
            int minuteDelta = 0;
            if (offset < offsetMax)
            {
                byte ch = chars[offset];
                if (ch == 'Z')
                {
                    offset++;
                    kind = DateTimeKind.Utc;
                }
                else if (ch == '+' || ch == '-')
                {
                    offset++;
                    if (offset + 5 > offsetMax || chars[offset + 2] != ':')
                        return false;
                    kind = DateTimeKind.Utc;
                    isLocal = true;
                    hourDelta = ToInt32D2(chars, offset);
                    minuteDelta = ToInt32D2(chars, offset + 3);
                    if ((hourDelta | minuteDelta) < 0)
                        return false;
                    if (ch == '+')
                    {
                        hourDelta = -hourDelta;
                        minuteDelta = -minuteDelta;
                    }
                    offset += 5;
                }
            }
            if (offset < offsetMax)
                return false;

            DateTime value;
            try
            {
                value = new DateTime(year, month, day, hour, minute, second, kind);
            }
            catch (ArgumentException)
            {
                return false;
            }

            if (ticks > 0)
            {
                value = value.AddTicks(ticks);
            }
            if (isLocal)
            {
                try
                {
                    TimeSpan ts = new TimeSpan(hourDelta, minuteDelta, 0);
                    if (hourDelta >= 0 && (value < DateTime.MaxValue - ts) ||
                        hourDelta < 0 && (value > DateTime.MinValue - ts))
                    {
                        value = value.Add(ts).ToLocalTime();
                    }
                    else
                    {
                        value = value.ToLocalTime().Add(ts);
                    }
                }
                catch (ArgumentOutOfRangeException) // Overflow
                {
                    return false;
                }
            }

            result = value;
            return true;
        }


        // Works left from offset
        public static int ToCharsR(int value, byte[] chars, int offset)
        {
            int count = 0;
            if (value >= 0)
            {
                while (value >= 10)
                {
                    int valueDiv10 = value / 10;
                    count++;
                    chars[--offset] = (byte)('0' + (value - valueDiv10 * 10));
                    value = valueDiv10;
                }
                chars[--offset] = (byte)('0' + value);
                count++;
            }
            else
            {
                while (value <= -10)
                {
                    int valueDiv10 = value / 10;
                    count++;
                    chars[--offset] = (byte)('0' - (value - valueDiv10 * 10));
                    value = valueDiv10;
                }
                chars[--offset] = (byte)('0' - value);
                chars[--offset] = (byte)'-';
                count += 2;
            }
            return count;
        }


        private static int ToCharsD2(int value, byte[] chars, int offset)
        {
            Debug.Assert(value >= 0 && value < 100);
            if (value < 10)
            {
                chars[offset + 0] = (byte)'0';
                chars[offset + 1] = (byte)('0' + value);
            }
            else
            {
                int valueDiv10 = value / 10;
                chars[offset + 0] = (byte)('0' + valueDiv10);
                chars[offset + 1] = (byte)('0' + value - valueDiv10 * 10);
            }
            return 2;
        }

        private static int ToCharsD4(int value, byte[] chars, int offset)
        {
            Debug.Assert(value >= 0 && value < 10000);
            ToCharsD2(value / 100, chars, offset + 0);
            ToCharsD2(value % 100, chars, offset + 2);
            return 4;
        }

        private static int ToCharsD7(int value, byte[] chars, int offset)
        {
            Debug.Assert(value >= 0 && value < 10000000);
            int zeroCount = 7 - ToCharsR(value, chars, offset + 7);
            for (int i = 0; i < zeroCount; i++)
                chars[offset + i] = (byte)'0';
            int count = 7;
            while (count > 0 && chars[offset + count - 1] == '0')
                count--;
            return count;
        }

        public static int ToChars(DateTime value, byte[] chars, int offset)
        {
            const long TicksPerMillisecond = 10000;
            const long TicksPerSecond = TicksPerMillisecond * 1000;
            int offsetMin = offset;
            // "yyyy-MM-ddTHH:mm:ss.fffffff";
            offset += ToCharsD4(value.Year, chars, offset);
            chars[offset++] = (byte)'-';
            offset += ToCharsD2(value.Month, chars, offset);
            chars[offset++] = (byte)'-';
            offset += ToCharsD2(value.Day, chars, offset);
            chars[offset++] = (byte)'T';
            offset += ToCharsD2(value.Hour, chars, offset);
            chars[offset++] = (byte)':';
            offset += ToCharsD2(value.Minute, chars, offset);
            chars[offset++] = (byte)':';
            offset += ToCharsD2(value.Second, chars, offset);
            int ms = (int)(value.Ticks % TicksPerSecond);
            if (ms != 0)
            {
                chars[offset++] = (byte)'.';
                offset += ToCharsD7(ms, chars, offset);
            }
            switch (value.Kind)
            {
                case DateTimeKind.Unspecified:
                    break;
                case DateTimeKind.Local:
                    // +"zzzzzz";
                    TimeSpan ts = TimeZoneInfo.Local.GetUtcOffset(value);
                    if (ts.Ticks < 0)
                        chars[offset++] = (byte)'-';
                    else
                        chars[offset++] = (byte)'+';
                    offset += ToCharsD2(Math.Abs(ts.Hours), chars, offset);
                    chars[offset++] = (byte)':';
                    offset += ToCharsD2(Math.Abs(ts.Minutes), chars, offset);
                    break;
                case DateTimeKind.Utc:
                    // +"Z"
                    chars[offset++] = (byte)'Z';
                    break;
                default:
                    throw new InvalidOperationException();
            }
            return offset - offsetMin;
        }

        public static bool IsWhitespace(ReadOnlySpan<char> chars) =>
            !chars.ContainsAnyExcept(s_whitespaceChars);

        public static bool IsWhitespace(ReadOnlySpan<byte> bytes) =>
            !bytes.ContainsAnyExcept(s_whitespaceBytes);

        public static bool IsWhitespace(char ch) =>
            ch is <= ' ' and (' ' or '\t' or '\r' or '\n');

        public static int StripWhitespace(Span<char> chars)
        {
            int count = chars.IndexOfAny(s_whitespaceChars);
            if (count < 0)
            {
                return chars.Length;
            }

            foreach (char c in chars.Slice(count + 1))
            {
                if (!IsWhitespace(c))
                {
                    chars[count++] = c;
                }
            }

            return count;
        }

        public static string StripWhitespace(string s)
        {
            int indexOfWhitespace = s.AsSpan().IndexOfAny(s_whitespaceChars);
            if (indexOfWhitespace < 0)
            {
                return s;
            }

            int count = s.Length - 1;
            foreach (char c in s.AsSpan(indexOfWhitespace + 1))
            {
                if (IsWhitespace(c))
                {
                    count--;
                }
            }

            return string.Create(count, s, static (chars, s) =>
            {
                int count = 0;
                foreach (char c in s)
                {
                    if (!IsWhitespace(c))
                    {
                        chars[count++] = c;
                    }
                }
                Debug.Assert(count == chars.Length);
            });
        }
    }
}
