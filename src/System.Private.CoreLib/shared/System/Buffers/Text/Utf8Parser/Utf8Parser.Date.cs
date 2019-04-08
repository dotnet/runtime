// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Diagnostics;

namespace System.Buffers.Text
{
    public static partial class Utf8Parser
    {
        /// <summary>
        /// Parses a DateTime at the start of a Utf8 string.
        /// </summary>
        /// <param name="source">The Utf8 string to parse</param>
        /// <param name="value">Receives the parsed value</param>
        /// <param name="bytesConsumed">On a successful parse, receives the length in bytes of the substring that was parsed </param>
        /// <param name="standardFormat">Expected format of the Utf8 string</param>
        /// <returns>
        /// true for success. "bytesConsumed" contains the length in bytes of the substring that was parsed.
        /// false if the string was not syntactically valid or an overflow or underflow occurred. "bytesConsumed" is set to 0. 
        /// </returns>
        /// <remarks>
        /// Formats supported:
        ///     default       05/25/2017 10:30:15 -08:00
        ///     G             05/25/2017 10:30:15
        ///     R             Tue, 03 Jan 2017 08:08:05 GMT       (RFC 1123)
        ///     l             tue, 03 jan 2017 08:08:05 gmt       (Lowercase RFC 1123)
        ///     O             2017-06-12T05:30:45.7680000-07:00   (Round-trippable)
        /// </remarks>
        /// <exceptions>
        /// <cref>System.FormatException</cref> if the format is not valid for this data type.
        /// </exceptions>
        public static bool TryParse(ReadOnlySpan<byte> source, out DateTime value, out int bytesConsumed, char standardFormat = default)
        {
            switch (standardFormat)
            {
                case 'R':
                    {
                        if (!TryParseDateTimeOffsetR(source, NoFlipCase, out DateTimeOffset dateTimeOffset, out bytesConsumed))
                        {
                            value = default;
                            return false;
                        }
                        value = dateTimeOffset.DateTime;  // (returns a DateTimeKind.Unspecified to match DateTime.ParseExact(). Maybe better to return UtcDateTime instead?)
                        return true;
                    }

                case 'l':
                    {
                        if (!TryParseDateTimeOffsetR(source, FlipCase, out DateTimeOffset dateTimeOffset, out bytesConsumed))
                        {
                            value = default;
                            return false;
                        }
                        value = dateTimeOffset.DateTime;  // (returns a DateTimeKind.Unspecified to match DateTime.ParseExact(). Maybe better to return UtcDateTime instead?)
                        return true;
                    }

                case 'O':
                    {
                        // Emulates DateTime.ParseExact(text, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
                        // In particular, the formatted string "encodes" the DateTimeKind according to the following table:
                        //
                        //         2017-06-12T05:30:45.7680000       - Unspecified
                        //         2017-06-12T05:30:45.7680000+00:00 - Local
                        //         2017-06-12T05:30:45.7680000Z      - Utc

                        if (!TryParseDateTimeOffsetO(source, out DateTimeOffset dateTimeOffset, out bytesConsumed, out DateTimeKind kind))
                        {
                            value = default;
                            bytesConsumed = 0;
                            return false;
                        }

                        switch (kind)
                        {
                            case DateTimeKind.Local:
                                value = dateTimeOffset.LocalDateTime;
                                break;
                            case DateTimeKind.Utc:
                                value = dateTimeOffset.UtcDateTime;
                                break;
                            default:
                                Debug.Assert(kind == DateTimeKind.Unspecified);
                                value = dateTimeOffset.DateTime;
                                break;
                        }

                        return true;
                    }

                case default(char):
                case 'G':
                    return TryParseDateTimeG(source, out value, out _, out bytesConsumed);

                default:
                    return ParserHelpers.TryParseThrowFormatException(out value, out bytesConsumed);
            }
        }

        /// <summary>
        /// Parses a DateTimeOffset at the start of a Utf8 string.
        /// </summary>
        /// <param name="source">The Utf8 string to parse</param>
        /// <param name="value">Receives the parsed value</param>
        /// <param name="bytesConsumed">On a successful parse, receives the length in bytes of the substring that was parsed </param>
        /// <param name="standardFormat">Expected format of the Utf8 string</param>
        /// <returns>
        /// true for success. "bytesConsumed" contains the length in bytes of the substring that was parsed.
        /// false if the string was not syntactically valid or an overflow or underflow occurred. "bytesConsumed" is set to 0. 
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
        public static bool TryParse(ReadOnlySpan<byte> source, out DateTimeOffset value, out int bytesConsumed, char standardFormat = default)
        {
            switch (standardFormat)
            {
                case 'R':
                    return TryParseDateTimeOffsetR(source, NoFlipCase, out value, out bytesConsumed);

                case 'l':
                    return TryParseDateTimeOffsetR(source, FlipCase, out value, out bytesConsumed);

                case 'O':
                    return TryParseDateTimeOffsetO(source, out value, out bytesConsumed, out _);

                case default(char):
                    return TryParseDateTimeOffsetDefault(source, out value, out bytesConsumed);

                case 'G':
                    return TryParseDateTimeG(source, out DateTime _, out value, out bytesConsumed);

                default:
                    return ParserHelpers.TryParseThrowFormatException(out value, out bytesConsumed);
            }
        }

        private const uint FlipCase = 0x00000020u;  // XOR mask to flip the case of a letter.
        private const uint NoFlipCase = 0x00000000u;
    }
}
