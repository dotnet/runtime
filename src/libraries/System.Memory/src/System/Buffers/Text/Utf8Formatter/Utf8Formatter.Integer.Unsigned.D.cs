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
        private static bool TryFormatUInt64D(ulong value, byte precision, Span<byte> buffer, bool insertNegationSign, out int bytesWritten)
        {
            // Calculate the actual digit count and the number of padding zeroes requested.
            // From all of this we can get the required buffer length.

            int digitCount = FormattingHelpers.CountDigits(value);
            int leadingZeroCount = ((precision == StandardFormat.NoPrecision) ? 0 : (int)precision) - digitCount;
            if (leadingZeroCount < 0)
            {
                leadingZeroCount = 0;
            }

            int requiredBufferLength = digitCount + leadingZeroCount;

            if (insertNegationSign)
            {
                requiredBufferLength++;
            }

            if (requiredBufferLength > buffer.Length)
            {
                bytesWritten = 0;
                return false;
            }

            bytesWritten = requiredBufferLength;

            if (insertNegationSign)
            {
                buffer[0] = Utf8Constants.Minus;
                buffer = buffer.Slice(1);
            }

            if (leadingZeroCount > 0)
            {
                FormattingHelpers.FillWithAsciiZeros(buffer.Slice(0, leadingZeroCount));
            }
            FormattingHelpers.WriteDigits(value, buffer.Slice(leadingZeroCount, digitCount));

            return true;
        }
    }
}
