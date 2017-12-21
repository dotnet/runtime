// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Buffers.Text
{
    /// <summary>
    /// Methods to format common data types as Utf8 strings.
    /// </summary>
    public static partial class Utf8Formatter
    {
        private static bool TryFormatUInt64D(ulong value, byte precision, Span<byte> buffer, out int bytesWritten)
        {
            if (value <= long.MaxValue)
                return TryFormatInt64D((long)value, precision, buffer, out bytesWritten);

            // Remove a single digit from the number. This will get it below long.MaxValue
            // Then we call the faster long version and follow-up with writing the last
            // digit. This ends up being faster by a factor of 2 than to just do the entire
            // operation using the unsigned versions.
            value = FormattingHelpers.DivMod(value, 10, out ulong lastDigit);

            if (precision != StandardFormat.NoPrecision && precision > 0)
                precision -= 1;

            if (!TryFormatInt64D((long)value, precision, buffer, out bytesWritten))
                return false;

            if (buffer.Length - 1 < bytesWritten)
            {
                bytesWritten = 0;
                return false;
            }

            ref byte utf8Bytes = ref MemoryMarshal.GetReference(buffer);
            FormattingHelpers.WriteDigits(lastDigit, 1, ref utf8Bytes, bytesWritten);
            bytesWritten += 1;
            return true;
        }
    }
}
