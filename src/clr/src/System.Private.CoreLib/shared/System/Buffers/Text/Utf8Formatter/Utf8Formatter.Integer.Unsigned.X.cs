// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Buffers.Text
{
    /// <summary>
    /// Methods to format common data types as Utf8 strings.
    /// </summary>
    public static partial class Utf8Formatter
    {
        private static bool TryFormatUInt64X(ulong value, byte precision, bool useLower, Span<byte> destination, out int bytesWritten)
        {
            int actualDigitCount = FormattingHelpers.CountHexDigits(value);
            int computedOutputLength = (precision == StandardFormat.NoPrecision)
                ? actualDigitCount
                : Math.Max(precision, actualDigitCount);

            if (destination.Length < computedOutputLength)
            {
                bytesWritten = 0;
                return false;
            }

            bytesWritten = computedOutputLength;
            string hexTable = (useLower) ? FormattingHelpers.HexTableLower : FormattingHelpers.HexTableUpper;

            // Writing the output backward in this manner allows the JIT to elide
            // bounds checking on the output buffer. The JIT won't elide the bounds
            // check on the hex table lookup, but we can live with that for now.

            // It doesn't quite make sense to use the fast hex conversion functionality
            // for this method since that routine works on bytes, and here we're working
            // directly with nibbles. There may be opportunity for improvement by special-
            // casing output lengths of 2, 4, 8, and 16 and running them down optimized
            // code paths.

            while ((uint)(--computedOutputLength) < (uint)destination.Length)
            {
                destination[computedOutputLength] = (byte)hexTable[(int)value & 0xf];
                value >>= 4;
            }
            return true;
        }
    }
}
