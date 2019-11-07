// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Buffers.Text
{
    /// <summary>
    /// Methods to format common data types as Utf8 strings.
    /// </summary>
    public static partial class Utf8Formatter
    {
        private static bool TryFormatUInt64N(ulong value, byte precision, Span<byte> destination, bool insertNegationSign, out int bytesWritten)
        {
            // Calculate the actual digit count, number of group separators required, and the
            // number of trailing zeros requested. From all of this we can get the required
            // buffer length.

            int digitCount = FormattingHelpers.CountDigits(value);
            int commaCount = (digitCount - 1) / 3;
            int trailingZeroCount = (precision == StandardFormat.NoPrecision) ? 2 /* default for 'N' */ : precision;

            int requiredBufferLength = digitCount + commaCount;
            if (trailingZeroCount > 0)
            {
                requiredBufferLength += trailingZeroCount + 1;
            }

            if (insertNegationSign)
            {
                requiredBufferLength++;
            }

            if (requiredBufferLength > destination.Length)
            {
                bytesWritten = 0;
                return false;
            }

            bytesWritten = requiredBufferLength;

            if (insertNegationSign)
            {
                destination[0] = Utf8Constants.Minus;
                destination = destination.Slice(1);
            }

            FormattingHelpers.WriteDigitsWithGroupSeparator(value, destination.Slice(0, digitCount + commaCount));

            if (trailingZeroCount > 0)
            {
                destination[digitCount + commaCount] = Utf8Constants.Period;
                FormattingHelpers.FillWithAsciiZeros(destination.Slice(digitCount + commaCount + 1, trailingZeroCount));
            }

            return true;
        }
    }
}
