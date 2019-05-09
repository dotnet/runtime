// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Buffers.Text
{
    public static partial class Utf8Parser
    {
        private static bool TryParseByteX(ReadOnlySpan<byte> source, out byte value, out int bytesConsumed)
        {
            if (source.Length < 1)
            {
                bytesConsumed = 0;
                value = default;
                return false;
            }
            byte nextCharacter;
            byte nextDigit;

            // Cache Parsers.s_HexLookup in order to avoid static constructor checks
            byte[] hexLookup = ParserHelpers.s_hexLookup;

            // Parse the first digit separately. If invalid here, we need to return false.
            nextCharacter = source[0];
            nextDigit = hexLookup[nextCharacter];
            if (nextDigit == 0xFF)
            {
                bytesConsumed = 0;
                value = default;
                return false;
            }
            uint parsedValue = nextDigit;

            if (source.Length <= ParserHelpers.ByteOverflowLengthHex)
            {
                // Length is less than or equal to Parsers.ByteOverflowLengthHex; overflow is not possible
                for (int index = 1; index < source.Length; index++)
                {
                    nextCharacter = source[index];
                    nextDigit = hexLookup[nextCharacter];
                    if (nextDigit == 0xFF)
                    {
                        bytesConsumed = index;
                        value = (byte)(parsedValue);
                        return true;
                    }
                    parsedValue = (parsedValue << 4) + nextDigit;
                }
            }
            else
            {
                // Length is greater than Parsers.ByteOverflowLengthHex; overflow is only possible after Parsers.ByteOverflowLengthHex
                // digits. There may be no overflow after Parsers.ByteOverflowLengthHex if there are leading zeroes.
                for (int index = 1; index < ParserHelpers.ByteOverflowLengthHex; index++)
                {
                    nextCharacter = source[index];
                    nextDigit = hexLookup[nextCharacter];
                    if (nextDigit == 0xFF)
                    {
                        bytesConsumed = index;
                        value = (byte)(parsedValue);
                        return true;
                    }
                    parsedValue = (parsedValue << 4) + nextDigit;
                }
                for (int index = ParserHelpers.ByteOverflowLengthHex; index < source.Length; index++)
                {
                    nextCharacter = source[index];
                    nextDigit = hexLookup[nextCharacter];
                    if (nextDigit == 0xFF)
                    {
                        bytesConsumed = index;
                        value = (byte)(parsedValue);
                        return true;
                    }
                    // If we try to append a digit to anything larger than byte.MaxValue / 0x10, there will be overflow
                    if (parsedValue > byte.MaxValue / 0x10)
                    {
                        bytesConsumed = 0;
                        value = default;
                        return false;
                    }
                    parsedValue = (parsedValue << 4) + nextDigit;
                }
            }

            bytesConsumed = source.Length;
            value = (byte)(parsedValue);
            return true;
        }

        private static bool TryParseUInt16X(ReadOnlySpan<byte> source, out ushort value, out int bytesConsumed)
        {
            if (source.Length < 1)
            {
                bytesConsumed = 0;
                value = default;
                return false;
            }
            byte nextCharacter;
            byte nextDigit;

            // Cache Parsers.s_HexLookup in order to avoid static constructor checks
            byte[] hexLookup = ParserHelpers.s_hexLookup;

            // Parse the first digit separately. If invalid here, we need to return false.
            nextCharacter = source[0];
            nextDigit = hexLookup[nextCharacter];
            if (nextDigit == 0xFF)
            {
                bytesConsumed = 0;
                value = default;
                return false;
            }
            uint parsedValue = nextDigit;

            if (source.Length <= ParserHelpers.Int16OverflowLengthHex)
            {
                // Length is less than or equal to Parsers.Int16OverflowLengthHex; overflow is not possible
                for (int index = 1; index < source.Length; index++)
                {
                    nextCharacter = source[index];
                    nextDigit = hexLookup[nextCharacter];
                    if (nextDigit == 0xFF)
                    {
                        bytesConsumed = index;
                        value = (ushort)(parsedValue);
                        return true;
                    }
                    parsedValue = (parsedValue << 4) + nextDigit;
                }
            }
            else
            {
                // Length is greater than Parsers.Int16OverflowLengthHex; overflow is only possible after Parsers.Int16OverflowLengthHex
                // digits. There may be no overflow after Parsers.Int16OverflowLengthHex if there are leading zeroes.
                for (int index = 1; index < ParserHelpers.Int16OverflowLengthHex; index++)
                {
                    nextCharacter = source[index];
                    nextDigit = hexLookup[nextCharacter];
                    if (nextDigit == 0xFF)
                    {
                        bytesConsumed = index;
                        value = (ushort)(parsedValue);
                        return true;
                    }
                    parsedValue = (parsedValue << 4) + nextDigit;
                }
                for (int index = ParserHelpers.Int16OverflowLengthHex; index < source.Length; index++)
                {
                    nextCharacter = source[index];
                    nextDigit = hexLookup[nextCharacter];
                    if (nextDigit == 0xFF)
                    {
                        bytesConsumed = index;
                        value = (ushort)(parsedValue);
                        return true;
                    }
                    // If we try to append a digit to anything larger than ushort.MaxValue / 0x10, there will be overflow
                    if (parsedValue > ushort.MaxValue / 0x10)
                    {
                        bytesConsumed = 0;
                        value = default;
                        return false;
                    }
                    parsedValue = (parsedValue << 4) + nextDigit;
                }
            }

            bytesConsumed = source.Length;
            value = (ushort)(parsedValue);
            return true;
        }

        private static bool TryParseUInt32X(ReadOnlySpan<byte> source, out uint value, out int bytesConsumed)
        {
            if (source.Length < 1)
            {
                bytesConsumed = 0;
                value = default;
                return false;
            }
            byte nextCharacter;
            byte nextDigit;

            // Cache Parsers.s_HexLookup in order to avoid static constructor checks
            byte[] hexLookup = ParserHelpers.s_hexLookup;

            // Parse the first digit separately. If invalid here, we need to return false.
            nextCharacter = source[0];
            nextDigit = hexLookup[nextCharacter];
            if (nextDigit == 0xFF)
            {
                bytesConsumed = 0;
                value = default;
                return false;
            }
            uint parsedValue = nextDigit;

            if (source.Length <= ParserHelpers.Int32OverflowLengthHex)
            {
                // Length is less than or equal to Parsers.Int32OverflowLengthHex; overflow is not possible
                for (int index = 1; index < source.Length; index++)
                {
                    nextCharacter = source[index];
                    nextDigit = hexLookup[nextCharacter];
                    if (nextDigit == 0xFF)
                    {
                        bytesConsumed = index;
                        value = parsedValue;
                        return true;
                    }
                    parsedValue = (parsedValue << 4) + nextDigit;
                }
            }
            else
            {
                // Length is greater than Parsers.Int32OverflowLengthHex; overflow is only possible after Parsers.Int32OverflowLengthHex
                // digits. There may be no overflow after Parsers.Int32OverflowLengthHex if there are leading zeroes.
                for (int index = 1; index < ParserHelpers.Int32OverflowLengthHex; index++)
                {
                    nextCharacter = source[index];
                    nextDigit = hexLookup[nextCharacter];
                    if (nextDigit == 0xFF)
                    {
                        bytesConsumed = index;
                        value = parsedValue;
                        return true;
                    }
                    parsedValue = (parsedValue << 4) + nextDigit;
                }
                for (int index = ParserHelpers.Int32OverflowLengthHex; index < source.Length; index++)
                {
                    nextCharacter = source[index];
                    nextDigit = hexLookup[nextCharacter];
                    if (nextDigit == 0xFF)
                    {
                        bytesConsumed = index;
                        value = parsedValue;
                        return true;
                    }
                    // If we try to append a digit to anything larger than uint.MaxValue / 0x10, there will be overflow
                    if (parsedValue > uint.MaxValue / 0x10)
                    {
                        bytesConsumed = 0;
                        value = default;
                        return false;
                    }
                    parsedValue = (parsedValue << 4) + nextDigit;
                }
            }

            bytesConsumed = source.Length;
            value = parsedValue;
            return true;
        }

        private static bool TryParseUInt64X(ReadOnlySpan<byte> source, out ulong value, out int bytesConsumed)
        {
            if (source.Length < 1)
            {
                bytesConsumed = 0;
                value = default;
                return false;
            }
            byte nextCharacter;
            byte nextDigit;

            // Cache Parsers.s_HexLookup in order to avoid static constructor checks
            byte[] hexLookup = ParserHelpers.s_hexLookup;

            // Parse the first digit separately. If invalid here, we need to return false.
            nextCharacter = source[0];
            nextDigit = hexLookup[nextCharacter];
            if (nextDigit == 0xFF)
            {
                bytesConsumed = 0;
                value = default;
                return false;
            }
            ulong parsedValue = nextDigit;

            if (source.Length <= ParserHelpers.Int64OverflowLengthHex)
            {
                // Length is less than or equal to Parsers.Int64OverflowLengthHex; overflow is not possible
                for (int index = 1; index < source.Length; index++)
                {
                    nextCharacter = source[index];
                    nextDigit = hexLookup[nextCharacter];
                    if (nextDigit == 0xFF)
                    {
                        bytesConsumed = index;
                        value = parsedValue;
                        return true;
                    }
                    parsedValue = (parsedValue << 4) + nextDigit;
                }
            }
            else
            {
                // Length is greater than Parsers.Int64OverflowLengthHex; overflow is only possible after Parsers.Int64OverflowLengthHex
                // digits. There may be no overflow after Parsers.Int64OverflowLengthHex if there are leading zeroes.
                for (int index = 1; index < ParserHelpers.Int64OverflowLengthHex; index++)
                {
                    nextCharacter = source[index];
                    nextDigit = hexLookup[nextCharacter];
                    if (nextDigit == 0xFF)
                    {
                        bytesConsumed = index;
                        value = parsedValue;
                        return true;
                    }
                    parsedValue = (parsedValue << 4) + nextDigit;
                }
                for (int index = ParserHelpers.Int64OverflowLengthHex; index < source.Length; index++)
                {
                    nextCharacter = source[index];
                    nextDigit = hexLookup[nextCharacter];
                    if (nextDigit == 0xFF)
                    {
                        bytesConsumed = index;
                        value = parsedValue;
                        return true;
                    }
                    // If we try to append a digit to anything larger than ulong.MaxValue / 0x10, there will be overflow
                    if (parsedValue > ulong.MaxValue / 0x10)
                    {
                        bytesConsumed = 0;
                        value = default;
                        return false;
                    }
                    parsedValue = (parsedValue << 4) + nextDigit;
                }
            }

            bytesConsumed = source.Length;
            value = parsedValue;
            return true;
        }
    }
}
