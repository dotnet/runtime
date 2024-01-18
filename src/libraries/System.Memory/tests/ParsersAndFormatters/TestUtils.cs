// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Numerics;
using System.Globalization;

namespace System.Buffers.Text.Tests
{
    internal static class TestUtils
    {
        //
        // Generate test output that's unmabiguous and convenient for investigations.
        //
        public static string DisplayString(this object value)
        {
            if (value is StandardFormat format)
            {
                if (format.Precision == StandardFormat.NoPrecision)
                    return format.Symbol.ToString();
                else
                    return format.Symbol + format.Precision.ToString();
            }
            else if (value is double dbl)
            {
                return dbl.ToString("G17", CultureInfo.InvariantCulture);
            }
            else if (value is float flt)
            {
                return flt.ToString("G9", CultureInfo.InvariantCulture);
            }
            else if (value is DateTime dateTime)
            {
                return "[" + dateTime.ToString("O", CultureInfo.InvariantCulture) + ", Kind=" + dateTime.Kind + "]";
            }
            else if (value is DateTimeOffset dateTimeOffset)
            {
                return "[" + dateTimeOffset.ToString("O", CultureInfo.InvariantCulture) + "]";
            }
            else if (value is decimal dec)
            {
                bool isNegative = decimal.IsNegative(dec);
                int scale = dec.Scale;

                if (isNegative)
                {
                    dec = -dec;
                }

                string sign = isNegative ? "-" : "+";

                return "[" + sign + dec.ToString("G", CultureInfo.InvariantCulture) + ", scale=" + scale + "]";
            }
            else if (value is TimeSpan timeSpan)
            {
                return timeSpan.ToString("G", CultureInfo.InvariantCulture);
            }

            return value.ToString();
        }

        public static BigInteger GetMinValue<T>()
        {
            if (typeof(T) == typeof(sbyte))
                return sbyte.MinValue;

            if (typeof(T) == typeof(byte))
                return byte.MinValue;

            if (typeof(T) == typeof(short))
                return short.MinValue;

            if (typeof(T) == typeof(ushort))
                return ushort.MinValue;

            if (typeof(T) == typeof(int))
                return int.MinValue;

            if (typeof(T) == typeof(uint))
                return uint.MinValue;

            if (typeof(T) == typeof(long))
                return long.MinValue;

            if (typeof(T) == typeof(ulong))
                return ulong.MinValue;

            if (typeof(T) == typeof(decimal))
                return new BigInteger(decimal.MinValue);

            throw new Exception("Unsupported type: " + typeof(T));
        }

        public static BigInteger GetMaxValue<T>()
        {
            if (typeof(T) == typeof(sbyte))
                return sbyte.MaxValue;

            if (typeof(T) == typeof(byte))
                return byte.MaxValue;

            if (typeof(T) == typeof(short))
                return short.MaxValue;

            if (typeof(T) == typeof(ushort))
                return ushort.MaxValue;

            if (typeof(T) == typeof(int))
                return int.MaxValue;

            if (typeof(T) == typeof(uint))
                return uint.MaxValue;

            if (typeof(T) == typeof(long))
                return long.MaxValue;

            if (typeof(T) == typeof(ulong))
                return ulong.MaxValue;

            if (typeof(T) == typeof(decimal))
                return new BigInteger(decimal.MaxValue);

            throw new Exception("Unsupported type: " + typeof(T));
        }

        public static BigInteger ToBigInteger<T>(this T value)
        {
            if (typeof(T) == typeof(sbyte))
                return new BigInteger((sbyte)(object)value);

            if (typeof(T) == typeof(byte))
                return new BigInteger((byte)(object)value);

            if (typeof(T) == typeof(short))
                return new BigInteger((short)(object)value);

            if (typeof(T) == typeof(ushort))
                return new BigInteger((ushort)(object)value);

            if (typeof(T) == typeof(int))
                return new BigInteger((int)(object)value);

            if (typeof(T) == typeof(uint))
                return new BigInteger((uint)(object)value);

            if (typeof(T) == typeof(long))
                return new BigInteger((long)(object)value);

            if (typeof(T) == typeof(ulong))
                return new BigInteger((ulong)(object)value);

            if (typeof(T) == typeof(decimal))
                return new BigInteger((decimal)(object)value);

            throw new Exception("Unsupported type: " + typeof(T));
        }

        public static ReadOnlySpan<byte> ToUtf8Span(this string s) => Encoding.UTF8.GetBytes(s);
        public static string ToUtf16String(this Span<byte> span) => Encoding.UTF8.GetString(span.ToArray());
    }
}
