// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;

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
            if (format.IsDefault)
            {
                return DateTimeFormat.TryFormatInvariantG(value.DateTime, value.Offset, destination, out bytesWritten);
            }

            switch (format.Symbol)
            {
                case 'R':
                    return DateTimeFormat.TryFormatR(value.UtcDateTime, NullOffset, destination, out bytesWritten);

                case 'O':
                    return DateTimeFormat.TryFormatO(value.DateTime, value.Offset, destination, out bytesWritten);

                case 'l':
                    return TryFormatDateTimeL(value.UtcDateTime, destination, out bytesWritten);

                case 'G':
                    return DateTimeFormat.TryFormatInvariantG(value.DateTime, NullOffset, destination, out bytesWritten);

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
                    return DateTimeFormat.TryFormatR(value, NullOffset, destination, out bytesWritten);

                case 'O':
                    return DateTimeFormat.TryFormatO(value, NullOffset, destination, out bytesWritten);

                case 'l':
                    return TryFormatDateTimeL(value, destination, out bytesWritten);

                case 'G':
                    return DateTimeFormat.TryFormatInvariantG(value, NullOffset, destination, out bytesWritten);

                default:
                    ThrowHelper.ThrowFormatException_BadFormatSpecifier();
                    goto case 'R'; // unreachable
            }
        }

        // Rfc1123 lowercased
        private static bool TryFormatDateTimeL(DateTime value, Span<byte> destination, out int bytesWritten)
        {
            if (DateTimeFormat.TryFormatR(value, NullOffset, destination, out bytesWritten))
            {
                Debug.Assert(bytesWritten == DateTimeFormat.FormatRLength);
                Ascii.ToLowerInPlace(destination.Slice(0, bytesWritten), out bytesWritten);
                return true;
            }

            return false;
        }

        private static TimeSpan NullOffset => new TimeSpan(DateTimeFormat.NullOffset);
    }
}
