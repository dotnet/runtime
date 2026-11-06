// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace SuperPmi;

// C printf-style formatting, in which the output formats of the tools are defined.
// Integer arguments are converted to the width given by the length modifier, as a C variadic call would.
public static class CFormat
{
    public static void Printf(TextWriter writer, string format, params object[] args) => writer.Write(Sprintf(format, args));

    public static string Sprintf(string format, params object[] args)
    {
        var sb = new StringBuilder(format.Length + 16);
        int argIndex = 0;
        int i = 0;
        while (i < format.Length)
        {
            char c = format[i++];
            if (c != '%')
            {
                sb.Append(c);
                continue;
            }

            if (i < format.Length && format[i] == '%')
            {
                sb.Append('%');
                i++;
                continue;
            }

            bool leftAlign = false, zeroPad = false, plus = false, space = false, alternate = false;
            for (; i < format.Length; i++)
            {
                switch (format[i])
                {
                    case '-': leftAlign = true; continue;
                    case '0': zeroPad = true; continue;
                    case '+': plus = true; continue;
                    case ' ': space = true; continue;
                    case '#': alternate = true; continue;
                }
                break;
            }

            int width = -1;
            if (i < format.Length && format[i] == '*')
            {
                width = Convert.ToInt32(args[argIndex++], CultureInfo.InvariantCulture);
                if (width < 0)
                {
                    leftAlign = true;
                    width = -width;
                }
                i++;
            }
            else
            {
                width = ParseNumber(format, ref i, -1);
            }

            int precision = -1;
            if (i < format.Length && format[i] == '.')
            {
                i++;
                if (i < format.Length && format[i] == '*')
                {
                    precision = Convert.ToInt32(args[argIndex++], CultureInfo.InvariantCulture);
                    i++;
                }
                else
                {
                    precision = ParseNumber(format, ref i, 0);
                }
            }

            int bits = 32;
            if (Match(format, ref i, "hh")) bits = 8;
            else if (Match(format, ref i, "h")) bits = 16;
            else if (Match(format, ref i, "ll") || Match(format, ref i, "I64") || Match(format, ref i, "l") ||
                     Match(format, ref i, "j") || Match(format, ref i, "z") || Match(format, ref i, "t") || Match(format, ref i, "q")) bits = 64;
            else if (Match(format, ref i, "I32")) bits = 32;
            else if (Match(format, ref i, "L")) bits = 64;

            char conversion = format[i++];
            object arg = conversion == 'n' ? null : args[argIndex++];

            string body;
            string prefix = "";
            bool numeric = true;
            switch (conversion)
            {
                case 'd':
                case 'i':
                {
                    long value = ToSigned(arg, bits);
                    body = FormatDigits((ulong)Math.Abs((decimal)value), 10, false, precision);
                    prefix = value < 0 ? "-" : plus ? "+" : space ? " " : "";
                    break;
                }
                case 'u':
                    body = FormatDigits(ToUnsigned(arg, bits), 10, false, precision);
                    break;
                case 'x':
                case 'X':
                {
                    ulong value = ToUnsigned(arg, bits);
                    body = FormatDigits(value, 16, conversion == 'X', precision);
                    if (alternate && value != 0)
                        prefix = conversion == 'X' ? "0X" : "0x";
                    break;
                }
                case 'o':
                {
                    ulong value = ToUnsigned(arg, bits);
                    body = FormatDigits(value, 8, false, precision);
                    if (alternate && !body.StartsWith('0'))
                        body = "0" + body;
                    break;
                }
                case 'c':
                    body = ((char)(byte)ToUnsigned(arg, 8)).ToString();
                    numeric = false;
                    break;
                case 's':
                    body = arg switch
                    {
                        null => "(null)",
                        byte[] bytes => Encoding.UTF8.GetString(bytes),
                        _ => arg.ToString(),
                    };
                    if (precision >= 0 && body.Length > precision)
                        body = body.Substring(0, precision);
                    numeric = false;
                    break;
                case 'p':
                    body = "0x" + ToUnsigned(arg, 64).ToString("x", CultureInfo.InvariantCulture);
                    numeric = false;
                    break;
                case 'f':
                case 'F':
                case 'e':
                case 'E':
                case 'g':
                case 'G':
                {
                    double value = Convert.ToDouble(arg, CultureInfo.InvariantCulture);
                    body = FormatDouble(Math.Abs(value), conversion, precision < 0 ? 6 : precision, alternate);
                    prefix = (value < 0 || (value == 0 && double.IsNegative(value))) ? "-" : plus ? "+" : space ? " " : "";
                    if (double.IsNaN(value) || double.IsInfinity(value))
                        zeroPad = false;
                    break;
                }
                default:
                    throw new FormatException($"Unsupported printf conversion '%{conversion}' in \"{format}\"");
            }

            // With a precision, '0' padding doesn't apply to integers.
            if (precision >= 0 && conversion is 'd' or 'i' or 'u' or 'x' or 'X' or 'o')
                zeroPad = false;

            int length = prefix.Length + body.Length;
            if (width <= length)
            {
                sb.Append(prefix).Append(body);
            }
            else if (leftAlign)
            {
                sb.Append(prefix).Append(body).Append(' ', width - length);
            }
            else if (zeroPad && numeric)
            {
                sb.Append(prefix).Append('0', width - length).Append(body);
            }
            else
            {
                sb.Append(' ', width - length).Append(prefix).Append(body);
            }
        }

        return sb.ToString();
    }

    private static int ParseNumber(string format, ref int i, int defaultValue)
    {
        int start = i;
        int value = 0;
        while (i < format.Length && char.IsAsciiDigit(format[i]))
            value = value * 10 + (format[i++] - '0');
        return i == start ? defaultValue : value;
    }

    private static bool Match(string format, ref int i, string text)
    {
        if (string.CompareOrdinal(format, i, text, 0, text.Length) != 0)
            return false;
        i += text.Length;
        return true;
    }

    private static ulong RawBits(object arg) => arg switch
    {
        sbyte v => (ulong)(long)v,
        short v => (ulong)(long)v,
        int v => (ulong)(long)v,
        long v => (ulong)v,
        nint v => (ulong)(long)v,
        byte v => v,
        ushort v => v,
        char v => v,
        uint v => v,
        ulong v => v,
        nuint v => (ulong)v,
        bool v => v ? 1UL : 0UL,
        Enum e => RawBits(Convert.ChangeType(e, Enum.GetUnderlyingType(e.GetType()), CultureInfo.InvariantCulture)),
        _ => throw new FormatException($"Unsupported printf integer argument type {arg?.GetType()}"),
    };

    private static ulong ToUnsigned(object arg, int bits)
    {
        ulong raw = RawBits(arg);
        return bits == 64 ? raw : raw & ((1UL << bits) - 1);
    }

    private static long ToSigned(object arg, int bits)
    {
        ulong raw = RawBits(arg);
        return bits switch
        {
            8 => (sbyte)raw,
            16 => (short)raw,
            32 => (int)raw,
            _ => (long)raw,
        };
    }

    private static string FormatDigits(ulong value, int radix, bool upper, int precision)
    {
        string digits = radix switch
        {
            10 => value.ToString(CultureInfo.InvariantCulture),
            16 => value.ToString(upper ? "X" : "x", CultureInfo.InvariantCulture),
            _ => Convert.ToString((long)value, 8),
        };
        if (precision == 0 && value == 0)
            return "";
        return precision > digits.Length ? new string('0', precision - digits.Length) + digits : digits;
    }

    private static string FormatDouble(double value, char conversion, int precision, bool alternate)
    {
        bool upper = char.IsUpper(conversion);
        if (double.IsNaN(value))
            return upper ? "NAN" : "nan";
        if (double.IsInfinity(value))
            return upper ? "INF" : "inf";

        switch (char.ToLowerInvariant(conversion))
        {
            case 'f':
                return value.ToString("F" + precision, CultureInfo.InvariantCulture);
            case 'e':
                return FormatExponent(value, precision, upper);
            default:
            {
                // %g: %e if the exponent is < -4 or >= the precision, else %f; trailing zeros removed.
                int p = precision == 0 ? 1 : precision;
                int exponent = value == 0 ? 0 : (int)Math.Floor(Math.Log10(value));
                string rounded = value.ToString("E" + (p - 1), CultureInfo.InvariantCulture);
                exponent = int.Parse(rounded.Substring(rounded.IndexOf('E') + 1), CultureInfo.InvariantCulture);
                string result = (exponent < -4 || exponent >= p)
                    ? FormatExponent(value, p - 1, upper)
                    : value.ToString("F" + (p - 1 - exponent), CultureInfo.InvariantCulture);
                if (!alternate)
                    result = StripTrailingZeros(result);
                return result;
            }
        }
    }

    // C uses at least two exponent digits; .NET uses three.
    private static string FormatExponent(double value, int precision, bool upper)
    {
        string s = value.ToString((upper ? "E" : "e") + precision, CultureInfo.InvariantCulture);
        int e = s.IndexOfAny(['e', 'E']);
        string mantissa = s.Substring(0, e);
        int exponent = int.Parse(s.Substring(e + 1), CultureInfo.InvariantCulture);
        return $"{mantissa}{s[e]}{(exponent < 0 ? '-' : '+')}{Math.Abs(exponent):00}";
    }

    private static string StripTrailingZeros(string s)
    {
        int e = s.IndexOfAny(['e', 'E']);
        string mantissa = e >= 0 ? s.Substring(0, e) : s;
        string exponent = e >= 0 ? s.Substring(e) : "";
        if (mantissa.Contains('.'))
            mantissa = mantissa.TrimEnd('0').TrimEnd('.');
        return mantissa + exponent;
    }
}
