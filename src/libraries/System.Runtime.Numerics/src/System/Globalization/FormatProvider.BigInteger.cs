// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization.Buffers;
using System.Text;

namespace System.Globalization
{
    internal static partial class FormatProvider
    {
        internal static void FormatBigInteger(
            ref ValueStringBuilder sb,
            int precision,
            int scale,
            bool sign,
            ReadOnlySpan<char> format,
            NumberFormatInfo numberFormatInfo,
            char[] digits,
            int startIndex)
        {
            unsafe
            {
                fixed (char* overrideDigits = digits)
                {
                    NumberBuffer numberBuffer = new NumberBuffer();
                    numberBuffer.Digits = overrideDigits + startIndex;
                    numberBuffer.Precision = precision;
                    numberBuffer.Scale = scale;
                    numberBuffer.IsNegativeSignExists = sign;

                    char fmt = Number.ParseFormatSpecifier(format, out int maxDigits);
                    if (fmt != 0)
                    {
                        Number.NumberToString(ref sb, numberBuffer, fmt, maxDigits, numberFormatInfo, isDecimal: false);
                    }
                    else
                    {
                        Number.NumberToStringFormat(ref sb, numberBuffer, format, numberFormatInfo);
                    }
                }
            }
        }

        internal static bool TryStringToBigInteger(
            ReadOnlySpan<char> s,
            NumberStyles styles,
            NumberFormatInfo numberFormatInfo,
            NumberBuffer numberBuffer,
            [NotNullWhen(true)] out StringBuilder? parsedNumber
            )
        {
            return Number.TryStringToNumber(s, styles, numberBuffer, numberFormatInfo, out parsedNumber);
        }
    }
}
