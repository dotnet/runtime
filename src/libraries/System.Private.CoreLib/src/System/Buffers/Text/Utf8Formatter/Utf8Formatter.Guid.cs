// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Buffers.Text
{
    public static partial class Utf8Formatter
    {
        /// <summary>
        /// Formats a Guid as a UTF8 string.
        /// </summary>
        /// <param name="value">Value to format</param>
        /// <param name="destination">Buffer to write the UTF8-formatted value to</param>
        /// <param name="bytesWritten">Receives the length of the formatted text in bytes</param>
        /// <param name="format">The standard format to use</param>
        /// <returns>
        /// true for success. "bytesWritten" contains the length of the formatted text in bytes.
        /// false if buffer was too short. Iteratively increase the size of the buffer and retry until it succeeds.
        /// </returns>
        /// <remarks>
        /// Formats supported:
        ///     D (default)     nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn
        ///     B               {nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn}
        ///     P               (nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn)
        ///     N               nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn
        /// </remarks>
        /// <exceptions>
        /// <cref>System.FormatException</cref> if the format is not valid for this data type.
        /// </exceptions>
        public static bool TryFormat(Guid value, Span<byte> destination, out int bytesWritten, StandardFormat format = default)
        {
            int flags;

            switch (FormattingHelpers.GetSymbolOrDefault(format, 'D'))
            {
                case 'D': // nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn
                    flags = 36 + Guid.TryFormatFlags_UseDashes;
                    break;

                case 'B': // {nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn}
                    flags = 38 + Guid.TryFormatFlags_UseDashes + Guid.TryFormatFlags_CurlyBraces;
                    break;

                case 'P': // (nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn)
                    flags = 38 + Guid.TryFormatFlags_UseDashes + Guid.TryFormatFlags_Parens;
                    break;

                case 'N': // nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn
                    flags = 32;
                    break;

                default:
                    ThrowHelper.ThrowFormatException_BadFormatSpecifier();
                    goto case 'D'; // unreachable
            }

            return value.TryFormatCore(destination, out bytesWritten, flags);
        }
    }
}
