// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

#if !netstandard
using Internal.Runtime.CompilerServices;
#else
using System.Runtime.CompilerServices;
#endif

using System.Runtime.InteropServices;

namespace System.Buffers.Text
{
    /// <summary>
    /// Methods to format common data types as Utf8 strings.
    /// </summary>
    public static partial class Utf8Formatter
    {
        private static bool TryFormatInt64N(long value, byte precision, Span<byte> buffer, out int bytesWritten)
        {
            int digitCount = FormattingHelpers.CountDigits(value);
            int groupSeparators = (int)FormattingHelpers.DivMod(digitCount, Utf8Constants.GroupSize, out long firstGroup);
            if (firstGroup == 0)
            {
                firstGroup = 3;
                groupSeparators--;
            }

            int trailingZeros = (precision == StandardFormat.NoPrecision) ? 2 : precision;
            int idx = (int)((value >> 63) & 1) + digitCount + groupSeparators;

            bytesWritten = idx;
            if (trailingZeros > 0)
                bytesWritten += trailingZeros + 1; // +1 for period.

            if (buffer.Length < bytesWritten)
            {
                bytesWritten = 0;
                return false;
            }

            ref byte utf8Bytes = ref MemoryMarshal.GetReference(buffer);
            long v = value;

            if (v < 0)
            {
                Unsafe.Add(ref utf8Bytes, 0) = Utf8Constants.Minus;

                // Abs(long.MinValue) == long.MaxValue + 1, so we need to re-route to unsigned to handle value
                if (v == long.MinValue)
                {
                    bool success = TryFormatUInt64N((ulong)long.MaxValue + 1, precision, buffer.Slice(1), out bytesWritten);
                    Debug.Assert(success, "TryFormatInt64N already did a full buffer length check so this subcall should never have failed.");

                    bytesWritten += 1; // Add the minus sign
                    return true;
                }

                v = -v;
            }

            // Write out the trailing zeros
            if (trailingZeros > 0)
            {
                Unsafe.Add(ref utf8Bytes, idx) = Utf8Constants.Period;
                FormattingHelpers.WriteDigits(0, trailingZeros, ref utf8Bytes, idx + 1);
            }

            // Starting from the back, write each group of digits except the first group
            while (digitCount > 3)
            {
                digitCount -= 3;
                idx -= 3;
                v = FormattingHelpers.DivMod(v, 1000, out long groupValue);
                FormattingHelpers.WriteDigits(groupValue, 3, ref utf8Bytes, idx);
                Unsafe.Add(ref utf8Bytes, --idx) = Utf8Constants.Separator;
            }

            // Write the first group of digits.
            FormattingHelpers.WriteDigits(v, (int)firstGroup, ref utf8Bytes, idx - (int)firstGroup);

            return true;
        }
    }
}
