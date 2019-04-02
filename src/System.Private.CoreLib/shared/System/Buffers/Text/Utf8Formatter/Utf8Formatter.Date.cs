// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Buffers.Text
{
    public static partial class Utf8Formatter
    {
        private const byte TimeMarker = (byte)'T';
        private const byte UtcMarker = (byte)'Z';

        private const byte GMT1 = (byte)'G';
        private const byte GMT2 = (byte)'M';
        private const byte GMT3 = (byte)'T';

        private const byte GMT1Lowercase = (byte)'g';
        private const byte GMT2Lowercase = (byte)'m';
        private const byte GMT3Lowercase = (byte)'t';

        // The three-letter abbreviation is packed into a 24-bit unsigned integer
        // where the least significant byte represents the first letter.
        private static readonly uint[] s_dayAbbreviations = new uint[]
        {
            'S' + ('u' << 8) + ('n' << 16),
            'M' + ('o' << 8) + ('n' << 16),
            'T' + ('u' << 8) + ('e' << 16),
            'W' + ('e' << 8) + ('d' << 16),
            'T' + ('h' << 8) + ('u' << 16),
            'F' + ('r' << 8) + ('i' << 16),
            'S' + ('a' << 8) + ('t' << 16),
        };

        private static readonly uint[] s_dayAbbreviationsLowercase = new uint[]
        {
            's' + ('u' << 8) + ('n' << 16),
            'm' + ('o' << 8) + ('n' << 16),
            't' + ('u' << 8) + ('e' << 16),
            'w' + ('e' << 8) + ('d' << 16),
            't' + ('h' << 8) + ('u' << 16),
            'f' + ('r' << 8) + ('i' << 16),
            's' + ('a' << 8) + ('t' << 16)
        };

        private static readonly uint[] s_monthAbbreviations = new uint[]
        {
            'J' + ('a' << 8) + ('n' << 16),
            'F' + ('e' << 8) + ('b' << 16),
            'M' + ('a' << 8) + ('r' << 16),
            'A' + ('p' << 8) + ('r' << 16),
            'M' + ('a' << 8) + ('y' << 16),
            'J' + ('u' << 8) + ('n' << 16),
            'J' + ('u' << 8) + ('l' << 16),
            'A' + ('u' << 8) + ('g' << 16),
            'S' + ('e' << 8) + ('p' << 16),
            'O' + ('c' << 8) + ('t' << 16),
            'N' + ('o' << 8) + ('v' << 16),
            'D' + ('e' << 8) + ('c' << 16),
        };

        private static readonly uint[] s_monthAbbreviationsLowercase = new uint[]
        {
            'j' + ('a' << 8) + ('n' << 16),
            'f' + ('e' << 8) + ('b' << 16),
            'm' + ('a' << 8) + ('r' << 16),
            'a' + ('p' << 8) + ('r' << 16),
            'm' + ('a' << 8) + ('y' << 16),
            'j' + ('u' << 8) + ('n' << 16),
            'j' + ('u' << 8) + ('l' << 16),
            'a' + ('u' << 8) + ('g' << 16),
            's' + ('e' << 8) + ('p' << 16),
            'o' + ('c' << 8) + ('t' << 16),
            'n' + ('o' << 8) + ('v' << 16),
            'd' + ('e' << 8) + ('c' << 16),
        };

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
                    return TryFormatDateTimeR(value.UtcDateTime, destination, out bytesWritten);

                case 'l':
                    return TryFormatDateTimeL(value.UtcDateTime, destination, out bytesWritten);

                case 'O':
                    return TryFormatDateTimeO(value.DateTime, value.Offset, destination, out bytesWritten);

                case 'G':
                    return TryFormatDateTimeG(value.DateTime, offset, destination, out bytesWritten);

                default:
                    return FormattingHelpers.TryFormatThrowFormatException(out bytesWritten);
            }
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
            char symbol = FormattingHelpers.GetSymbolOrDefault(format, 'G');

            switch (symbol)
            {
                case 'R':
                    return TryFormatDateTimeR(value, destination, out bytesWritten);

                case 'l':
                    return TryFormatDateTimeL(value, destination, out bytesWritten);

                case 'O':
                    return TryFormatDateTimeO(value, Utf8Constants.NullUtcOffset, destination, out bytesWritten);

                case 'G':
                    return TryFormatDateTimeG(value, Utf8Constants.NullUtcOffset, destination, out bytesWritten);

                default:
                    return FormattingHelpers.TryFormatThrowFormatException(out bytesWritten);
            }
        }
    }
}
