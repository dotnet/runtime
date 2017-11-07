// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Buffers.Text
{
    /// <summary>
    /// Methods to format common data types as Utf8 strings.
    /// </summary>
    public static partial class Utf8Formatter
    {
        private static bool TryFormatUInt64N(ulong value, byte precision, Span<byte> buffer, out int bytesWritten)
        {
            if (value <= long.MaxValue)
                return TryFormatInt64N((long)value, precision, buffer, out bytesWritten);

            // The ulong path is much slower than the long path here, so we are doing the last group
            // inside this method plus the zero padding but routing to the long version for the rest.
            value = FormattingHelpers.DivMod(value, 1000, out ulong lastGroup);

            if (!TryFormatInt64N((long)value, 0, buffer, out bytesWritten))
                return false;

            if (precision == StandardFormat.NoPrecision)
                precision = 2;

            int idx = bytesWritten;

            // Since this method routes entirely to the long version if the number is smaller than
            // long.MaxValue, we are guaranteed to need to write 3 more digits here before the set
            // of trailing zeros.

            bytesWritten += 4; // 3 digits + group separator
            if (precision > 0)
                bytesWritten += precision + 1; // +1 for period.

            if (buffer.Length < bytesWritten)
            {
                bytesWritten = 0;
                return false;
            }

            ref byte utf8Bytes = ref buffer.DangerousGetPinnableReference();

            // Write the last group
            Unsafe.Add(ref utf8Bytes, idx++) = Utf8Constants.Separator;
            idx += FormattingHelpers.WriteDigits(lastGroup, 3, ref utf8Bytes, idx);

            // Write out the trailing zeros
            if (precision > 0)
            {
                Unsafe.Add(ref utf8Bytes, idx) = Utf8Constants.Period;
                FormattingHelpers.WriteDigits(0, precision, ref utf8Bytes, idx + 1);
            }

            return true;
        }
    }
}
