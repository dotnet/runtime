// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

#if !netstandard
using Internal.Runtime.CompilerServices;
#endif

namespace System.Buffers.Text
{
    /// <summary>
    /// Methods to format common data types as Utf8 strings.
    /// </summary>
    public static partial class Utf8Formatter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryFormatInt64N(long value, byte precision, Span<byte> buffer, out int bytesWritten)
        {
            bool insertNegationSign = false;
            if (value < 0)
            {
                insertNegationSign = true;
                value = -value;
                if (value < 0)
                {
                    Debug.Assert(value == Int64.MinValue);
                    return TryFormatInt64N_MinValue(precision, buffer, out bytesWritten);
                }
            }

            return TryFormatUInt64N((ulong)value, precision, buffer, insertNegationSign, out bytesWritten);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool TryFormatInt64N_MinValue(byte precision, Span<byte> buffer, out int bytesWritten)
        {
            // Int64.MinValue must be treated specially since its two's complement negation doesn't fit into 64 bits.
            // Instead, we'll perform one's complement negation and fix up the +1 later (-x := ~x + 1).
            // Int64.MinValue = -9,223,372,036,854,775,808 (26 digits, including minus and commas)
            // Int64.MaxValue =  9,223,372,036,854,775,807

            bool retVal = TryFormatUInt64N((ulong)Int64.MaxValue, precision, buffer, insertNegationSign: true, out int tempBytesWritten);
            if (retVal)
            {
                buffer[25]++; // bump the last ASCII '7' to an '8'
            }

            bytesWritten = tempBytesWritten;
            return retVal;
        }
    }
}
