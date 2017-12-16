// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !netstandard
using Internal.Runtime.CompilerServices;
#else
using System.Runtime.CompilerServices;
#endif

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
        /// <param name="text">The Utf8 string to parse</param>
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
        public static bool TryParse(ReadOnlySpan<byte> text, out sbyte value, out int bytesConsumed, char standardFormat = default)
        {
            switch (standardFormat)
            {
                case (default):
                case 'g':
                case 'G':
                case 'd':
                case 'D':
                    return TryParseSByteD(text, out value, out bytesConsumed);

                case 'n':
                case 'N':
                    return TryParseSByteN(text, out value, out bytesConsumed);

                case 'x':
                case 'X':
                    value = default;
                    return TryParseByteX(text, out Unsafe.As<sbyte, byte>(ref value), out bytesConsumed);

                default:
                    return ThrowHelper.TryParseThrowFormatException(out value, out bytesConsumed);
            }
        }

        /// <summary>
        /// Parses an Int16 at the start of a Utf8 string.
        /// </summary>
        /// <param name="text">The Utf8 string to parse</param>
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
        public static bool TryParse(ReadOnlySpan<byte> text, out short value, out int bytesConsumed, char standardFormat = default)
        {
            switch (standardFormat)
            {
                case (default):
                case 'g':
                case 'G':
                case 'd':
                case 'D':
                    return TryParseInt16D(text, out value, out bytesConsumed);

                case 'n':
                case 'N':
                    return TryParseInt16N(text, out value, out bytesConsumed);

                case 'x':
                case 'X':
                    value = default;
                    return TryParseUInt16X(text, out Unsafe.As<short, ushort>(ref value), out bytesConsumed);

                default:
                    return ThrowHelper.TryParseThrowFormatException(out value, out bytesConsumed);
            }
        }

        /// <summary>
        /// Parses an Int32 at the start of a Utf8 string.
        /// </summary>
        /// <param name="text">The Utf8 string to parse</param>
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
        public static bool TryParse(ReadOnlySpan<byte> text, out int value, out int bytesConsumed, char standardFormat = default)
        {
            switch (standardFormat)
            {
                case (default):
                case 'g':
                case 'G':
                case 'd':
                case 'D':
                    return TryParseInt32D(text, out value, out bytesConsumed);

                case 'n':
                case 'N':
                    return TryParseInt32N(text, out value, out bytesConsumed);

                case 'x':
                case 'X':
                    value = default;
                    return TryParseUInt32X(text, out Unsafe.As<int, uint>(ref value), out bytesConsumed);

                default:
                    return ThrowHelper.TryParseThrowFormatException(out value, out bytesConsumed);
            }
        }

        /// <summary>
        /// Parses an Int64 at the start of a Utf8 string.
        /// </summary>
        /// <param name="text">The Utf8 string to parse</param>
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
        public static bool TryParse(ReadOnlySpan<byte> text, out long value, out int bytesConsumed, char standardFormat = default)
        {
            switch (standardFormat)
            {
                case (default):
                case 'g':
                case 'G':
                case 'd':
                case 'D':
                    return TryParseInt64D(text, out value, out bytesConsumed);

                case 'n':
                case 'N':
                    return TryParseInt64N(text, out value, out bytesConsumed);

                case 'x':
                case 'X':
                    value = default;
                    return TryParseUInt64X(text, out Unsafe.As<long, ulong>(ref value), out bytesConsumed);

                default:
                    return ThrowHelper.TryParseThrowFormatException(out value, out bytesConsumed);
            }
        }
    }
}
