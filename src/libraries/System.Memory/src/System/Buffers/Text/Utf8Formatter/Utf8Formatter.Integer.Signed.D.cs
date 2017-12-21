// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private static bool TryFormatInt64D(long value, byte precision, Span<byte> buffer, out int bytesWritten)
        {
            int digitCount = FormattingHelpers.CountDigits(value);
            int bytesNeeded = digitCount + (int)((value >> 63) & 1);
            if (precision != StandardFormat.NoPrecision && precision > digitCount)
            {
                bytesNeeded += (precision - digitCount);
            }

            if (buffer.Length < bytesNeeded)
            {
                bytesWritten = 0;
                return false;
            }

            ref byte utf8Bytes = ref MemoryMarshal.GetReference(buffer);
            int idx = 0;

            if (value < 0)
            {
                Unsafe.Add(ref utf8Bytes, idx++) = Utf8Constants.Minus;

                // Abs(long.MinValue) == long.MaxValue + 1, so we need to handle this specially.
                if (value == long.MinValue)
                {
                    if (precision != StandardFormat.NoPrecision)
                    {
                        int leadingZeros = (int)precision - 19;
                        while (leadingZeros-- > 0)
                            Unsafe.Add(ref utf8Bytes, idx++) = (byte)'0';
                    }

                    Unsafe.Add(ref utf8Bytes, idx++) = (byte)'9';
                    FormattingHelpers.WriteDigits(223372036854775808L, 18, ref utf8Bytes, idx);

                    bytesWritten = idx + 18;
                    return true;
                }

                value = -value;
            }

            if (precision != StandardFormat.NoPrecision)
            {
                int leadingZeros = (int)precision - digitCount;
                while (leadingZeros-- > 0)
                    Unsafe.Add(ref utf8Bytes, idx++) = (byte)'0';
            }

            FormattingHelpers.WriteDigits(value, digitCount, ref utf8Bytes, idx);

            bytesWritten = digitCount + idx;
            return true;
        }
    }
}
