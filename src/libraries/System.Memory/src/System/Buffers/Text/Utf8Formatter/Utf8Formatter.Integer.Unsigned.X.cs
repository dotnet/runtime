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
        private static bool TryFormatUInt64X(ulong value, byte precision, bool useLower, Span<byte> buffer, out int bytesWritten)
        {
            const string HexTableLower = "0123456789abcdef";
            const string HexTableUpper = "0123456789ABCDEF";

            int digits = 1;
            ulong v = value;
            if (v > 0xFFFFFFFF)
            {
                digits += 8;
                v >>= 0x20;
            }
            if (v > 0xFFFF)
            {
                digits += 4;
                v >>= 0x10;
            }
            if (v > 0xFF)
            {
                digits += 2;
                v >>= 0x8;
            }
            if (v > 0xF)
                digits++;

            int paddingCount = (precision == StandardFormat.NoPrecision) ? 0 : precision - digits;
            if (paddingCount < 0)
                paddingCount = 0;

            bytesWritten = digits + paddingCount;
            if (buffer.Length < bytesWritten)
            {
                bytesWritten = 0;
                return false;
            }

            string hexTable = useLower ? HexTableLower : HexTableUpper;
            ref byte utf8Bytes = ref buffer.DangerousGetPinnableReference();
            int idx = bytesWritten;

            for (v = value; digits-- > 0; v >>= 4)
                Unsafe.Add(ref utf8Bytes, --idx) = (byte)hexTable[(int)(v & 0xF)];

            while (paddingCount-- > 0)
                Unsafe.Add(ref utf8Bytes, --idx) = (byte)'0';

            return true;
        }
    }
}
