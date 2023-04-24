// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Buffers.Text
{
    public static partial class Utf8Formatter
    {
        /// <summary>
        /// Formats a DateTimeOffset as a UTF8 string.
        /// </summary>
        /// <param name="value">Value to format</param>
        /// <param name="destination">Buffer to write the UTF8-formatted value to</param>
        /// <param name="bytesWritten">Receives the length of the formatted text in bytes</param>
        /// <param name="format">The standard format to use</param>
        /// <returns>
        /// true for success. "bytesWritten" contains the length of the formatted text in bytes.
        /// false if buffer was too short. Iteratively increase the size of the buffer and retry until it succeeds.
        /// </returns>
        /// <exceptions>
        /// <remarks>
        /// Formats supported:
        ///     default       05/25/2017 10:30:15 -08:00
        ///     G             05/25/2017 10:30:15
        ///     R             Tue, 03 Jan 2017 08:08:05 GMT       (RFC 1123)
        ///     l             tue, 03 jan 2017 08:08:05 gmt       (Lowercase RFC 1123)
        ///     O             2017-06-12T05:30:45.7680000-07:00   (Round-trippable)
        /// </remarks>
        /// <cref>System.FormatException</cref> if the format is not valid for this data type.
        /// </exceptions>
        public static bool TryFormat(DateTimeOffset value, Span<byte> destination, out int bytesWritten, StandardFormat format = default)
        {
            TimeSpan offset = Utf8Constants.NullUtcOffset;
            char symbol = format.Symbol;
            if (format.IsDefault)
            {
                symbol = 'G';
                offset = value.Offset;
            }

            switch (symbol)
            {
                case 'R':
                    return DateTimeFormat.TryFormatR(value.UtcDateTime, new TimeSpan(DateTimeFormat.NullOffset), destination, out bytesWritten);

                case 'O':
                    return DateTimeFormat.TryFormatO(value.DateTime, value.Offset, destination, out bytesWritten);

                case 'l':
                    return TryFormatDateTimeL(value.UtcDateTime, destination, out bytesWritten);

                case 'G':
                    return TryFormatDateTimeG(value.DateTime, offset, destination, out bytesWritten);

                default:
                    ThrowHelper.ThrowFormatException_BadFormatSpecifier();
                    goto case 'R';
            };
        }

        /// <summary>
        /// Formats a DateTime as a UTF8 string.
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
        ///     G  (default)  05/25/2017 10:30:15
        ///     R             Tue, 03 Jan 2017 08:08:05 GMT       (RFC 1123)
        ///     l             tue, 03 jan 2017 08:08:05 gmt       (Lowercase RFC 1123)
        ///     O             2017-06-12T05:30:45.7680000-07:00   (Round-trippable)
        /// </remarks>
        /// <exceptions>
        /// <cref>System.FormatException</cref> if the format is not valid for this data type.
        /// </exceptions>
        public static bool TryFormat(DateTime value, Span<byte> destination, out int bytesWritten, StandardFormat format = default)
        {
            switch (FormattingHelpers.GetSymbolOrDefault(format, 'G'))
            {
                case 'R':
                    return DateTimeFormat.TryFormatR(value, new TimeSpan(DateTimeFormat.NullOffset), destination, out bytesWritten);

                case 'O':
                    return DateTimeFormat.TryFormatO(value, Utf8Constants.NullUtcOffset, destination, out bytesWritten);

                case 'l':
                    return TryFormatDateTimeL(value, destination, out bytesWritten);

                case 'G':
                    return TryFormatDateTimeG(value, Utf8Constants.NullUtcOffset, destination, out bytesWritten);

                default:
                    ThrowHelper.ThrowFormatException_BadFormatSpecifier();
                    goto case 'R'; // unreachable
            }
        }
    }
}
