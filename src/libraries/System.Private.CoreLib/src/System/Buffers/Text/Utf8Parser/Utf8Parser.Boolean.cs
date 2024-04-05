// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;

namespace System.Buffers.Text
{
    public static partial class Utf8Parser
    {
        /// <summary>
        /// Parses a Boolean at the start of a Utf8 string.
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
        ///     G (default)   True/False
        ///     l             true/false
        /// </remarks>
        /// <exceptions>
        /// <cref>System.FormatException</cref> if the format is not valid for this data type.
        /// </exceptions>
        public static bool TryParse(ReadOnlySpan<byte> source, out bool value, out int bytesConsumed, char standardFormat = default)
        {
            if (!(standardFormat == default(char) || standardFormat == 'G' || standardFormat == 'l'))
                ThrowHelper.ThrowFormatException_BadFormatSpecifier();

            if (source.Length >= 4)
            {
                int dw = BinaryPrimitives.ReadInt32LittleEndian(source);
                if (standardFormat == default(char) || standardFormat == 'G')
                {
                    if (dw == 0x65757254 /* 'eurT' */)
                    {
                        bytesConsumed = 4;
                        value = true;
                        return true;
                    }
    
                    if (source.Length > 4)
                    {
                        if (dw == 0x736C6146 /* 'slaF' */ && source[4] == 'e')
                        {
                            bytesConsumed = 5;
                            value = false;
                            return true;
                        }
                    }
                }
                else if (standardFormat == 'l')
                {
                    if (dw == 0x65757274 /* 'eurt' */)
                    {
                        bytesConsumed = 4;
                        value = true;
                        return true;
                    }
    
                    if (source.Length > 4)
                    {
                        if (dw == 0x736C6166 /* 'slaf' */ && source[4] == 'e')
                        {
                            bytesConsumed = 5;
                            value = false;
                            return true;
                        }
                    }
                }
            }

            bytesConsumed = 0;
            value = default;
            return false;
        }
    }
}
