// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security;
using System.Text;

namespace System.Globalization
{
    internal partial class FormatProvider
    {
        [SecurityCritical]
        internal static string FormatBigInteger(int precision, int scale, bool sign, string format, NumberFormatInfo numberFormatInfo, char[] digits, int startIndex)
        {
            unsafe
            {
                int maxDigits;
                char fmt = Number.ParseFormatSpecifier(format, out maxDigits);

                fixed (char* overrideDigits = digits)
                {
                    FormatProvider.Number.NumberBuffer numberBuffer = new FormatProvider.Number.NumberBuffer();
                    numberBuffer.overrideDigits = overrideDigits + startIndex;
                    numberBuffer.precision = precision;
                    numberBuffer.scale = scale;
                    numberBuffer.sign = sign;
                    if (fmt != 0)
                        return Number.NumberToString(numberBuffer, fmt, maxDigits, numberFormatInfo, isDecimal: false);
                    return Number.NumberToStringFormat(numberBuffer, format, numberFormatInfo);
                }
            }
        }

        [SecurityCritical]
        internal static bool TryStringToBigInteger(
            string s,
            NumberStyles styles,
            NumberFormatInfo numberFormatInfo,
            StringBuilder receiver,  // Receives the decimal digits
            out int precision,
            out int scale,
            out bool sign
            )
        {
            FormatProvider.Number.NumberBuffer numberBuffer = new FormatProvider.Number.NumberBuffer();

            unsafe
            {
                // Note: because we passed a non-null StringBuilder (receiver) to TryStringToNumber, it streams the digits into
                // that instead of numberBuffer.digits. This is quite important since numberBuffer.digits is a fixed-buffer size
                // and BigNumbers can have an arbitrary number of digits.
                //
                // Just in case a bug is ever introduced into TryStringToNumber that violates this, set the pointer tha numberBuffer.digits returns
                // to something that will AV.
                numberBuffer.overrideDigits = (char*)0x1;
            }
            if (!Number.TryStringToNumber(s, styles, ref numberBuffer, receiver, numberFormatInfo, parseDecimal: false))
            {
                precision = default(int);
                scale = default(int);
                sign = default(bool);
                return false;
            }
            else
            {
                precision = numberBuffer.precision;
                scale = numberBuffer.scale;
                sign = numberBuffer.sign;
                return true;
            }
        }
    }
}
