// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

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

        private static readonly byte[][] DayAbbreviations = new byte[][]
        {
            new byte[] { (byte)'S', (byte)'u', (byte)'n' },
            new byte[] { (byte)'M', (byte)'o', (byte)'n' },
            new byte[] { (byte)'T', (byte)'u', (byte)'e' },
            new byte[] { (byte)'W', (byte)'e', (byte)'d' },
            new byte[] { (byte)'T', (byte)'h', (byte)'u' },
            new byte[] { (byte)'F', (byte)'r', (byte)'i' },
            new byte[] { (byte)'S', (byte)'a', (byte)'t' },
        };

        private static readonly byte[][] DayAbbreviationsLowercase = new byte[][]
        {
            new byte[] { (byte)'s', (byte)'u', (byte)'n' },
            new byte[] { (byte)'m', (byte)'o', (byte)'n' },
            new byte[] { (byte)'t', (byte)'u', (byte)'e' },
            new byte[] { (byte)'w', (byte)'e', (byte)'d' },
            new byte[] { (byte)'t', (byte)'h', (byte)'u' },
            new byte[] { (byte)'f', (byte)'r', (byte)'i' },
            new byte[] { (byte)'s', (byte)'a', (byte)'t' },
        };

        private static readonly byte[][] MonthAbbreviations = new byte[][]
        {
            new byte[] { (byte)'J', (byte)'a', (byte)'n' },
            new byte[] { (byte)'F', (byte)'e', (byte)'b' },
            new byte[] { (byte)'M', (byte)'a', (byte)'r' },
            new byte[] { (byte)'A', (byte)'p', (byte)'r' },
            new byte[] { (byte)'M', (byte)'a', (byte)'y' },
            new byte[] { (byte)'J', (byte)'u', (byte)'n' },
            new byte[] { (byte)'J', (byte)'u', (byte)'l' },
            new byte[] { (byte)'A', (byte)'u', (byte)'g' },
            new byte[] { (byte)'S', (byte)'e', (byte)'p' },
            new byte[] { (byte)'O', (byte)'c', (byte)'t' },
            new byte[] { (byte)'N', (byte)'o', (byte)'v' },
            new byte[] { (byte)'D', (byte)'e', (byte)'c' },
        };

        private static readonly byte[][] MonthAbbreviationsLowercase = new byte[][]
        {
            new byte[] { (byte)'j', (byte)'a', (byte)'n' },
            new byte[] { (byte)'f', (byte)'e', (byte)'b' },
            new byte[] { (byte)'m', (byte)'a', (byte)'r' },
            new byte[] { (byte)'a', (byte)'p', (byte)'r' },
            new byte[] { (byte)'m', (byte)'a', (byte)'y' },
            new byte[] { (byte)'j', (byte)'u', (byte)'n' },
            new byte[] { (byte)'j', (byte)'u', (byte)'l' },
            new byte[] { (byte)'a', (byte)'u', (byte)'g' },
            new byte[] { (byte)'s', (byte)'e', (byte)'p' },
            new byte[] { (byte)'o', (byte)'c', (byte)'t' },
            new byte[] { (byte)'n', (byte)'o', (byte)'v' },
            new byte[] { (byte)'d', (byte)'e', (byte)'c' },
        };

        /// <summary>
        /// Formats a DateTimeOffset as a UTF8 string.
        /// </summary>
        /// <param name="value">Value to format</param>
        /// <param name="buffer">Buffer to write the UTF8-formatted value to</param>
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
        public static bool TryFormat(DateTimeOffset value, Span<byte> buffer, out int bytesWritten, StandardFormat format = default)
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
                    return TryFormatDateTimeR(value.UtcDateTime, buffer, out bytesWritten);

                case 'l':
                    return TryFormatDateTimeL(value.UtcDateTime, buffer, out bytesWritten);

                case 'O':
                    return TryFormatDateTimeO(value.DateTime, value.Offset, buffer, out bytesWritten);

                case 'G':
                    return TryFormatDateTimeG(value.DateTime, offset, buffer, out bytesWritten);

                default:
                    return ThrowHelper.TryFormatThrowFormatException(out bytesWritten);
            }
        }

        /// <summary>
        /// Formats a DateTime as a UTF8 string.
        /// </summary>
        /// <param name="value">Value to format</param>
        /// <param name="buffer">Buffer to write the UTF8-formatted value to</param>
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
        public static bool TryFormat(DateTime value, Span<byte> buffer, out int bytesWritten, StandardFormat format = default)
        {
            char symbol = format.IsDefault ? 'G' : format.Symbol;

            switch (symbol)
            {
                case 'R':
                    return TryFormatDateTimeR(value, buffer, out bytesWritten);

                case 'l':
                    return TryFormatDateTimeL(value, buffer, out bytesWritten);

                case 'O':
                    return TryFormatDateTimeO(value, Utf8Constants.NullUtcOffset, buffer, out bytesWritten);

                case 'G':
                    return TryFormatDateTimeG(value, Utf8Constants.NullUtcOffset, buffer, out bytesWritten);

                default:
                    return ThrowHelper.TryFormatThrowFormatException(out bytesWritten);
            }
        }
    }
}
