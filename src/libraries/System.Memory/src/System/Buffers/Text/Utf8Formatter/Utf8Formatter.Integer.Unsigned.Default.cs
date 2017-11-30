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
        private static bool TryFormatUInt64Default(ulong value, Span<byte> buffer, out int bytesWritten)
        {
            if (value < 10)
            {
                if (buffer.Length == 0) goto FalseExit;
                buffer[0] = (byte)('0' + value);
                bytesWritten = 1;
                return true;
            }

            int digitCount = FormattingHelpers.CountDigits(value);
            if (digitCount > buffer.Length) goto FalseExit;
            bytesWritten = digitCount;
            // WriteDigits does not do bounds checks
            FormattingHelpers.WriteDigits(value, buffer.Slice(0, digitCount));
            return true;

        FalseExit:
            bytesWritten = 0;
            return false;
        }
    }
}
