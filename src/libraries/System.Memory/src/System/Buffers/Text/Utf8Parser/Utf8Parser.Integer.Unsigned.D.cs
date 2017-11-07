// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Buffers.Text
{
    public static partial class Utf8Parser
    {
        private static bool TryParseByteD(ReadOnlySpan<byte> text, out byte value, out int bytesConsumed)
        {
            if (text.Length < 1)
            {
                bytesConsumed = 0;
                value = default;
                return false;
            }

            // Parse the first digit separately. If invalid here, we need to return false.
            uint firstDigit = text[0] - 48u; // '0'
            if (firstDigit > 9)
            {
                bytesConsumed = 0;
                value = default;
                return false;
            }
            uint parsedValue = firstDigit;

            if (text.Length < ParserHelpers.ByteOverflowLength)
            {
                // Length is less than Parsers.ByteOverflowLength; overflow is not possible
                for (int index = 1; index < text.Length; index++)
                {
                    uint nextDigit = text[index] - 48u; // '0'
                    if (nextDigit > 9)
                    {
                        bytesConsumed = index;
                        value = (byte)(parsedValue);
                        return true;
                    }
                    parsedValue = parsedValue * 10 + nextDigit;
                }
            }
            else
            {
                // Length is greater than Parsers.ByteOverflowLength; overflow is only possible after Parsers.ByteOverflowLength
                // digits. There may be no overflow after Parsers.ByteOverflowLength if there are leading zeroes.
                for (int index = 1; index < ParserHelpers.ByteOverflowLength - 1; index++)
                {
                    uint nextDigit = text[index] - 48u; // '0'
                    if (nextDigit > 9)
                    {
                        bytesConsumed = index;
                        value = (byte)(parsedValue);
                        return true;
                    }
                    parsedValue = parsedValue * 10 + nextDigit;
                }
                for (int index = ParserHelpers.ByteOverflowLength - 1; index < text.Length; index++)
                {
                    uint nextDigit = text[index] - 48u; // '0'
                    if (nextDigit > 9)
                    {
                        bytesConsumed = index;
                        value = (byte)(parsedValue);
                        return true;
                    }
                    // If parsedValue > (byte.MaxValue / 10), any more appended digits will cause overflow.
                    // if parsedValue == (byte.MaxValue / 10), any nextDigit greater than 5 implies overflow.
                    if (parsedValue > byte.MaxValue / 10 || (parsedValue == byte.MaxValue / 10 && nextDigit > 5))
                    {
                        bytesConsumed = 0;
                        value = default;
                        return false;
                    }
                    parsedValue = parsedValue * 10 + nextDigit;
                }
            }

            bytesConsumed = text.Length;
            value = (byte)(parsedValue);
            return true;
        }

        private static bool TryParseUInt16D(ReadOnlySpan<byte> text, out ushort value, out int bytesConsumed)
        {
            if (text.Length < 1)
            {
                bytesConsumed = 0;
                value = default;
                return false;
            }

            // Parse the first digit separately. If invalid here, we need to return false.
            uint firstDigit = text[0] - 48u; // '0'
            if (firstDigit > 9)
            {
                bytesConsumed = 0;
                value = default;
                return false;
            }
            uint parsedValue = firstDigit;

            if (text.Length < ParserHelpers.Int16OverflowLength)
            {
                // Length is less than Parsers.Int16OverflowLength; overflow is not possible
                for (int index = 1; index < text.Length; index++)
                {
                    uint nextDigit = text[index] - 48u; // '0'
                    if (nextDigit > 9)
                    {
                        bytesConsumed = index;
                        value = (ushort)(parsedValue);
                        return true;
                    }
                    parsedValue = parsedValue * 10 + nextDigit;
                }
            }
            else
            {
                // Length is greater than Parsers.Int16OverflowLength; overflow is only possible after Parsers.Int16OverflowLength
                // digits. There may be no overflow after Parsers.Int16OverflowLength if there are leading zeroes.
                for (int index = 1; index < ParserHelpers.Int16OverflowLength - 1; index++)
                {
                    uint nextDigit = text[index] - 48u; // '0'
                    if (nextDigit > 9)
                    {
                        bytesConsumed = index;
                        value = (ushort)(parsedValue);
                        return true;
                    }
                    parsedValue = parsedValue * 10 + nextDigit;
                }
                for (int index = ParserHelpers.Int16OverflowLength - 1; index < text.Length; index++)
                {
                    uint nextDigit = text[index] - 48u; // '0'
                    if (nextDigit > 9)
                    {
                        bytesConsumed = index;
                        value = (ushort)(parsedValue);
                        return true;
                    }
                    // If parsedValue > (ushort.MaxValue / 10), any more appended digits will cause overflow.
                    // if parsedValue == (ushort.MaxValue / 10), any nextDigit greater than 5 implies overflow.
                    if (parsedValue > ushort.MaxValue / 10 || (parsedValue == ushort.MaxValue / 10 && nextDigit > 5))
                    {
                        bytesConsumed = 0;
                        value = default;
                        return false;
                    }
                    parsedValue = parsedValue * 10 + nextDigit;
                }
            }

            bytesConsumed = text.Length;
            value = (ushort)(parsedValue);
            return true;
        }

        private static bool TryParseUInt32D(ReadOnlySpan<byte> text, out uint value, out int bytesConsumed)
        {
            if (text.Length < 1)
            {
                bytesConsumed = 0;
                value = default;
                return false;
            }

            // Parse the first digit separately. If invalid here, we need to return false.
            uint firstDigit = text[0] - 48u; // '0'
            if (firstDigit > 9)
            {
                bytesConsumed = 0;
                value = default;
                return false;
            }
            uint parsedValue = firstDigit;

            if (text.Length < ParserHelpers.Int32OverflowLength)
            {
                // Length is less than Parsers.Int32OverflowLength; overflow is not possible
                for (int index = 1; index < text.Length; index++)
                {
                    uint nextDigit = text[index] - 48u; // '0'
                    if (nextDigit > 9)
                    {
                        bytesConsumed = index;
                        value = parsedValue;
                        return true;
                    }
                    parsedValue = parsedValue * 10 + nextDigit;
                }
            }
            else
            {
                // Length is greater than Parsers.Int32OverflowLength; overflow is only possible after Parsers.Int32OverflowLength
                // digits. There may be no overflow after Parsers.Int32OverflowLength if there are leading zeroes.
                for (int index = 1; index < ParserHelpers.Int32OverflowLength - 1; index++)
                {
                    uint nextDigit = text[index] - 48u; // '0'
                    if (nextDigit > 9)
                    {
                        bytesConsumed = index;
                        value = parsedValue;
                        return true;
                    }
                    parsedValue = parsedValue * 10 + nextDigit;
                }
                for (int index = ParserHelpers.Int32OverflowLength - 1; index < text.Length; index++)
                {
                    uint nextDigit = text[index] - 48u; // '0'
                    if (nextDigit > 9)
                    {
                        bytesConsumed = index;
                        value = parsedValue;
                        return true;
                    }
                    // If parsedValue > (uint.MaxValue / 10), any more appended digits will cause overflow.
                    // if parsedValue == (uint.MaxValue / 10), any nextDigit greater than 5 implies overflow.
                    if (parsedValue > uint.MaxValue / 10 || (parsedValue == uint.MaxValue / 10 && nextDigit > 5))
                    {
                        bytesConsumed = 0;
                        value = default;
                        return false;
                    }
                    parsedValue = parsedValue * 10 + nextDigit;
                }
            }

            bytesConsumed = text.Length;
            value = parsedValue;
            return true;
        }

        private static bool TryParseUInt64D(ReadOnlySpan<byte> text, out ulong value, out int bytesConsumed)
        {
            if (text.Length < 1)
            {
                bytesConsumed = 0;
                value = default;
                return false;
            }

            // Parse the first digit separately. If invalid here, we need to return false.
            ulong firstDigit = text[0] - 48u; // '0'
            if (firstDigit > 9)
            {
                bytesConsumed = 0;
                value = default;
                return false;
            }
            ulong parsedValue = firstDigit;

            if (text.Length < ParserHelpers.Int64OverflowLength)
            {
                // Length is less than Parsers.Int64OverflowLength; overflow is not possible
                for (int index = 1; index < text.Length; index++)
                {
                    ulong nextDigit = text[index] - 48u; // '0'
                    if (nextDigit > 9)
                    {
                        bytesConsumed = index;
                        value = parsedValue;
                        return true;
                    }
                    parsedValue = parsedValue * 10 + nextDigit;
                }
            }
            else
            {
                // Length is greater than Parsers.Int64OverflowLength; overflow is only possible after Parsers.Int64OverflowLength
                // digits. There may be no overflow after Parsers.Int64OverflowLength if there are leading zeroes.
                for (int index = 1; index < ParserHelpers.Int64OverflowLength - 1; index++)
                {
                    ulong nextDigit = text[index] - 48u; // '0'
                    if (nextDigit > 9)
                    {
                        bytesConsumed = index;
                        value = parsedValue;
                        return true;
                    }
                    parsedValue = parsedValue * 10 + nextDigit;
                }
                for (int index = ParserHelpers.Int64OverflowLength - 1; index < text.Length; index++)
                {
                    ulong nextDigit = text[index] - 48u; // '0'
                    if (nextDigit > 9)
                    {
                        bytesConsumed = index;
                        value = parsedValue;
                        return true;
                    }
                    // If parsedValue > (ulong.MaxValue / 10), any more appended digits will cause overflow.
                    // if parsedValue == (ulong.MaxValue / 10), any nextDigit greater than 5 implies overflow.
                    if (parsedValue > ulong.MaxValue / 10 || (parsedValue == ulong.MaxValue / 10 && nextDigit > 5))
                    {
                        bytesConsumed = 0;
                        value = default;
                        return false;
                    }
                    parsedValue = parsedValue * 10 + nextDigit;
                }
            }

            bytesConsumed = text.Length;
            value = parsedValue;
            return true;
        }
    }
}
