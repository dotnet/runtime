// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Buffers.Text
{
    public static partial class Utf8Parser
    {
        /// <summary>
        /// Parses a Byte at the start of a Utf8 string.
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
        ///     G/g (default)
        ///     D/d             32767  
        ///     N/n             32,767       
        ///     X/x             7fff
        /// </remarks>
        /// <exceptions>
        /// <cref>System.FormatException</cref> if the format is not valid for this data type.
        /// </exceptions>
        public static bool TryParse(ReadOnlySpan<byte> source, out byte value, out int bytesConsumed, char standardFormat = default)
        {
            switch (standardFormat)
            {
                case default(char):
                case 'g':
                case 'G':
                case 'd':
                case 'D':
                    return TryParseByteD(source, out value, out bytesConsumed);

                case 'n':
                case 'N':
                    return TryParseByteN(source, out value, out bytesConsumed);

                case 'x':
                case 'X':
                    return TryParseByteX(source, out value, out bytesConsumed);

                default:
                    return ParserHelpers.TryParseThrowFormatException(out value, out bytesConsumed);
            }
        }

        /// <summary>
        /// Parses a UInt16 at the start of a Utf8 string.
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
        ///     G/g (default)
        ///     D/d             32767  
        ///     N/n             32,767       
        ///     X/x             7fff
        /// </remarks>
        /// <exceptions>
        /// <cref>System.FormatException</cref> if the format is not valid for this data type.
        /// </exceptions>
        [CLSCompliant(false)]
        public static bool TryParse(ReadOnlySpan<byte> source, out ushort value, out int bytesConsumed, char standardFormat = default)
        {
            switch (standardFormat)
            {
                case default(char):
                case 'g':
                case 'G':
                case 'd':
                case 'D':
                    return TryParseUInt16D(source, out value, out bytesConsumed);

                case 'n':
                case 'N':
                    return TryParseUInt16N(source, out value, out bytesConsumed);

                case 'x':
                case 'X':
                    return TryParseUInt16X(source, out value, out bytesConsumed);

                default:
                    return ParserHelpers.TryParseThrowFormatException(out value, out bytesConsumed);
            }
        }

        /// <summary>
        /// Parses a UInt32 at the start of a Utf8 string.
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
        ///     G/g (default)
        ///     D/d             32767  
        ///     N/n             32,767       
        ///     X/x             7fff
        /// </remarks>
        /// <exceptions>
        /// <cref>System.FormatException</cref> if the format is not valid for this data type.
        /// </exceptions>
        [CLSCompliant(false)]
        public static bool TryParse(ReadOnlySpan<byte> source, out uint value, out int bytesConsumed, char standardFormat = default)
        {
            switch (standardFormat)
            {
                case default(char):
                case 'g':
                case 'G':
                case 'd':
                case 'D':
                    return TryParseUInt32D(source, out value, out bytesConsumed);

                case 'n':
                case 'N':
                    return TryParseUInt32N(source, out value, out bytesConsumed);

                case 'x':
                case 'X':
                    return TryParseUInt32X(source, out value, out bytesConsumed);

                default:
                    return ParserHelpers.TryParseThrowFormatException(out value, out bytesConsumed);
            }
        }

        /// <summary>
        /// Parses a UInt64 at the start of a Utf8 string.
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
        ///     G/g (default)
        ///     D/d             32767  
        ///     N/n             32,767       
        ///     X/x             7fff
        /// </remarks>
        /// <exceptions>
        /// <cref>System.FormatException</cref> if the format is not valid for this data type.
        /// </exceptions>
        [CLSCompliant(false)]
        public static bool TryParse(ReadOnlySpan<byte> source, out ulong value, out int bytesConsumed, char standardFormat = default)
        {
            switch (standardFormat)
            {
                case default(char):
                case 'g':
                case 'G':
                case 'd':
                case 'D':
                    return TryParseUInt64D(source, out value, out bytesConsumed);

                case 'n':
                case 'N':
                    return TryParseUInt64N(source, out value, out bytesConsumed);

                case 'x':
                case 'X':
                    return TryParseUInt64X(source, out value, out bytesConsumed);

                default:
                    return ParserHelpers.TryParseThrowFormatException(out value, out bytesConsumed);
            }
        }
    }
}
