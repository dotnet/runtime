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
        private static bool TryFormatInt64Default(long value, Span<byte> buffer, out int bytesWritten)
        {
            if ((ulong)value < 10)
            {
                if (buffer.Length == 0) goto FalseExit;
                buffer[0] = (byte)('0' + value);
                bytesWritten = 1;
                return true;
            }
            
            if (value < 0)
            {
                value = -value;
                int digitCount = FormattingHelpers.CountDigits((ulong)value);
                if (digitCount >= buffer.Length) goto FalseExit;
                bytesWritten = digitCount + 1;
                buffer[0] = Utf8Constants.Minus;
                buffer = buffer.Slice(1, digitCount);
            }
            else
            {
                int digitCount = FormattingHelpers.CountDigits((ulong)value);
                if (digitCount > buffer.Length) goto FalseExit;
                bytesWritten = digitCount;
                buffer = buffer.Slice(0, digitCount);
            }

            // WriteDigits does not do bounds checks
            FormattingHelpers.WriteDigits((ulong)value, buffer);
            return true;

        FalseExit:
            bytesWritten = 0;
            return false;
        }
    }
}
