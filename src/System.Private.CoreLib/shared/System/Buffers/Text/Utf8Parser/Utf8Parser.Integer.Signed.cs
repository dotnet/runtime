// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using Internal.Runtime.CompilerServices;

namespace System.Buffers.Text
{
    /// <summary>
    /// Methods to parse common data types to Utf8 strings.
    /// </summary>
    public static partial class Utf8Parser
    {
        /// <summary>
        /// Parses a SByte at the start of a Utf8 string.
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
        public static bool TryParse(ReadOnlySpan<byte> source, out sbyte value, out int bytesConsumed, char standardFormat = default)
        {
            switch (standardFormat)
            {
                case default(char):
                case 'g':
                case 'G':
                case 'd':
                case 'D':
                    return TryParseSByteD(source, out value, out bytesConsumed);

                case 'n':
                case 'N':
                    return TryParseSByteN(source, out value, out bytesConsumed);

                case 'x':
                case 'X':
                    value = default;
                    return TryParseByteX(source, out Unsafe.As<sbyte, byte>(ref value), out bytesConsumed);

                default:
                    return ParserHelpers.TryParseThrowFormatException(out value, out bytesConsumed);
            }
        }

        /// <summary>
        /// Parses an Int16 at the start of a Utf8 string.
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
        public static bool TryParse(ReadOnlySpan<byte> source, out short value, out int bytesConsumed, char standardFormat = default)
        {
            switch (standardFormat)
            {
                case default(char):
                case 'g':
                case 'G':
                case 'd':
                case 'D':
                    return TryParseInt16D(source, out value, out bytesConsumed);

                case 'n':
                case 'N':
                    return TryParseInt16N(source, out value, out bytesConsumed);

                case 'x':
                case 'X':
                    value = default;
                    return TryParseUInt16X(source, out Unsafe.As<short, ushort>(ref value), out bytesConsumed);

                default:
                    return ParserHelpers.TryParseThrowFormatException(out value, out bytesConsumed);
            }
        }

        /// <summary>
        /// Parses an Int32 at the start of a Utf8 string.
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
        public static bool TryParse(ReadOnlySpan<byte> source, out int value, out int bytesConsumed, char standardFormat = default)
        {
            switch (standardFormat)
            {
                case default(char):
                case 'g':
                case 'G':
                case 'd':
                case 'D':
                    return TryParseInt32D(source, out value, out bytesConsumed);

                case 'n':
                case 'N':
                    return TryParseInt32N(source, out value, out bytesConsumed);

                case 'x':
                case 'X':
                    value = default;
                    return TryParseUInt32X(source, out Unsafe.As<int, uint>(ref value), out bytesConsumed);

                default:
                    return ParserHelpers.TryParseThrowFormatException(out value, out bytesConsumed);
            }
        }

        /// <summary>
        /// Parses an Int64 at the start of a Utf8 string.
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
        public static bool TryParse(ReadOnlySpan<byte> source, out long value, out int bytesConsumed, char standardFormat = default)
        {
            switch (standardFormat)
            {
                case default(char):
                case 'g':
                case 'G':
                case 'd':
                case 'D':
                    return TryParseInt64D(source, out value, out bytesConsumed);

                case 'n':
                case 'N':
                    return TryParseInt64N(source, out value, out bytesConsumed);

                case 'x':
                case 'X':
                    value = default;
                    return TryParseUInt64X(source, out Unsafe.As<long, ulong>(ref value), out bytesConsumed);

                default:
                    return ParserHelpers.TryParseThrowFormatException(out value, out bytesConsumed);
            }
        }
    }
}
