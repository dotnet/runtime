// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Runtime.CompilerServices;

namespace System.Buffers.Text
{
    /// <summary>
    /// Methods to format common data types as Utf8 strings.
    /// </summary>
    public static partial class Utf8Formatter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryFormatInt64N(long value, byte precision, Span<byte> destination, out int bytesWritten)
        {
            bool insertNegationSign = false;
            if (value < 0)
            {
                insertNegationSign = true;
                value = -value;
            }

            return TryFormatUInt64N((ulong)value, precision, destination, insertNegationSign, out bytesWritten);
        }
    }
}
