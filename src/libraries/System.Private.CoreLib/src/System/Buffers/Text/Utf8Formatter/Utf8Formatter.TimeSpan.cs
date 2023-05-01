// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System.Buffers.Text
{
    public static partial class Utf8Formatter
    {
        /// <summary>
        /// Formats a TimeSpan as a UTF8 string.
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
        ///     c/t/T (default) [-][d.]hh:mm:ss[.fffffff]              (constant format)
        ///     G               [-]d:hh:mm:ss.fffffff                  (general long)
        ///     g               [-][d:][h]h:mm:ss[.f[f[f[f[f[f[f]]]]]] (general short)
        /// </remarks>
        /// <exceptions>
        /// <cref>System.FormatException</cref> if the format is not valid for this data type.
        /// </exceptions>
        public static bool TryFormat(TimeSpan value, Span<byte> destination, out int bytesWritten, StandardFormat format = default)
        {
            TimeSpanFormat.StandardFormat sf = TimeSpanFormat.StandardFormat.C;
            ReadOnlySpan<byte> decimalSeparator = default;

            char symbol = FormattingHelpers.GetSymbolOrDefault(format, 'c');
            if (symbol != 'c' && (symbol | 0x20) != 't')
            {
                decimalSeparator = DateTimeFormatInfo.InvariantInfo.DecimalSeparatorTChar<byte>();
                if (symbol == 'g')
                {
                    sf = TimeSpanFormat.StandardFormat.g;
                }
                else
                {
                    sf = TimeSpanFormat.StandardFormat.G;
                    if (symbol != 'G')
                    {
                        ThrowHelper.ThrowFormatException_BadFormatSpecifier();
                    }
                }
            }

            return TimeSpanFormat.TryFormatStandard(value, sf, decimalSeparator, destination, out bytesWritten);
        }
    }
}
