// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace System.Globalization
{
    internal static partial class FormatProvider
    {
        internal static void FormatBigInteger(ref ValueStringBuilder sb, int precision, int scale, bool sign, ReadOnlySpan<char> format, NumberFormatInfo numberFormatInfo, char[] digits, int startIndex)
        {
            unsafe
            {
                fixed (char* overrideDigits = digits)
                {
                    Number.NumberBuffer numberBuffer = default;
                    numberBuffer.overrideDigits = overrideDigits + startIndex;
                    numberBuffer.precision = precision;
                    numberBuffer.scale = scale;
                    numberBuffer.sign = sign;

                    char fmt = Number.ParseFormatSpecifier(format, out int maxDigits);
                    if (fmt != 0)
                    {
                        Number.NumberToString(ref sb, ref numberBuffer, fmt, maxDigits, numberFormatInfo, isDecimal: false);
                    }
                    else
                    {
                        Number.NumberToStringFormat(ref sb, ref numberBuffer, format, numberFormatInfo);
                    }
                }
            }
        }
    }
}
