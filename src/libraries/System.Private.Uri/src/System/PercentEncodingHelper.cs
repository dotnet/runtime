// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Text;

namespace System
{
    internal static class PercentEncodingHelper
    {
        public static unsafe int UnescapePercentEncodedUTF8Sequence(char* input, int length, ref ValueStringBuilder dest, bool isQuery, bool iriParsing)
        {
            // The following assertions rely on the input not mutating mid-operation, as is the case currently since callers are working with strings
            // If we start accepting input such as spans, this method must be audited to ensure no buffer overruns/infinite loops could occur

            // As an optimization, this method should only be called after the first character is known to be a part of a non-ascii UTF8 sequence
            Debug.Assert(length >= 3);
            Debug.Assert(input[0] == '%');
            Debug.Assert(UriHelper.DecodeHexChars(input[1], input[2]) != Uri.c_DummyChar);
            Debug.Assert(UriHelper.DecodeHexChars(input[1], input[2]) >= 128);

            uint fourByteBuffer = 0;
            int bytesLeftInBuffer = 0;

            int totalCharsConsumed = 0;
            int charsToCopy = 0;
            int bytesConsumed = 0;

        RefillBuffer:
            int i = totalCharsConsumed + (bytesLeftInBuffer * 3);

        ReadByteFromInput:
            if ((uint)(length - i) <= 2 || input[i] != '%')
                goto NoMoreOrInvalidInput;

            uint value = input[i + 1];
            if ((uint)((value - 'A') & ~0x20) <= ('F' - 'A'))
            {
                value = (value | 0x20) - 'a' + 10;
            }
            else if ((value - '8') <= ('9' - '8'))
            {
                value -= '0';
            }
            else goto NoMoreOrInvalidInput; // First character wasn't hex or was <= 7F (Ascii)

            uint second = (uint)input[i + 2] - '0';
            if (second <= 9)
            {
                // second is already [0, 9]
            }
            else if ((uint)((second - ('A' - '0')) & ~0x20) <= ('F' - 'A'))
            {
                second = ((second + '0') | 0x20) - 'a' + 10;
            }
            else goto NoMoreOrInvalidInput; // Second character wasn't Hex

            value = (value << 4) | second;

            Debug.Assert(value >= 128);

            // Rotate the buffer and overwrite the last byte
            if (BitConverter.IsLittleEndian)
            {
                fourByteBuffer = (fourByteBuffer >> 8) | (value << 24);
            }
            else
            {
                fourByteBuffer = (fourByteBuffer << 8) | value;
            }

            if (++bytesLeftInBuffer != 4)
            {
                i += 3;
                goto ReadByteFromInput;
            }

        DecodeRune:
            Debug.Assert(totalCharsConsumed % 3 == 0);
            Debug.Assert(bytesLeftInBuffer == 2 || bytesLeftInBuffer == 3 || bytesLeftInBuffer == 4);
            Debug.Assert((fourByteBuffer & (BitConverter.IsLittleEndian ? 0x00000080 : 0x80000000)) != 0);
            Debug.Assert((fourByteBuffer & (BitConverter.IsLittleEndian ? 0x00008000 : 0x00800000)) != 0);
            Debug.Assert(bytesLeftInBuffer < 3 || (fourByteBuffer & (BitConverter.IsLittleEndian ? 0x00800000 : 0x00008000)) != 0);
            Debug.Assert(bytesLeftInBuffer < 4 || (fourByteBuffer & (BitConverter.IsLittleEndian ? 0x80000000 : 0x00000080)) != 0);

            uint temp = fourByteBuffer; // make a copy so that the *copy* (not the original) is marked address-taken
            if (Rune.DecodeFromUtf8(new ReadOnlySpan<byte>(&temp, bytesLeftInBuffer), out Rune rune, out bytesConsumed) == OperationStatus.Done)
            {
                Debug.Assert(bytesConsumed >= 2, $"Rune.DecodeFromUtf8 consumed {bytesConsumed} bytes, likely indicating input was modified concurrently during UnescapePercentEncodedUTF8Sequence's execution");

                if (!iriParsing || IriHelper.CheckIriUnicodeRange((uint)rune.Value, isQuery))
                {
                    if (charsToCopy != 0)
                    {
                        dest.Append(input + totalCharsConsumed - charsToCopy, charsToCopy);
                        charsToCopy = 0;
                    }

                    dest.Append(rune);
                    goto AfterDecodeRune;
                }
            }
            else
            {
                Debug.Assert(bytesConsumed > 0, $"Rune.DecodeFromUtf8 consumed {bytesConsumed} bytes when decoding {bytesLeftInBuffer} bytes");
            }
            charsToCopy += bytesConsumed * 3;

        AfterDecodeRune:
            bytesLeftInBuffer -= bytesConsumed;
            totalCharsConsumed += bytesConsumed * 3;
            goto RefillBuffer;

        NoMoreOrInvalidInput:
            Debug.Assert(bytesLeftInBuffer < 4);

            // If we have more than 1 byte left, we try to decode it
            if (bytesLeftInBuffer > 1)
            {
                Debug.Assert(bytesLeftInBuffer == 2 || bytesLeftInBuffer == 3);

                // We reach this branch if we don't have 4 valid bytes to consume
                // We have to allign the read bytes to the start of fourByteBuffer memory
                // We do this by shifting the fourByteBuffer, the shift direction is determined by system endianness

                // If we read 3 bytes, we shift by 1; if we read 2, we shift by 2
                // (32 - (bytesLeftInBuffer << 3)) calculates this offset:
                // bytesLeftInBuffer == 3 => (32 - (3 << 3)) => 32 - 24 => 8 bits
                // bytesLeftInBuffer == 2 => (32 - (2 << 3)) => 32 - 16 => 16 bits

                // For invalid input we tried to decode in DecodeRune, we may return here if we have more than 1 byte left
                // If bytesConsumed is 1, shift by 1 byte
                // If bytesConsumed is 2:
                // a) We had 4 bytes in the buffer and now only have 2 => Shift by 2 bytes
                // b) We read 1 more byte, leaving us with 3 bytes in the buffer => Shift by 1 byte
                // The case for bytesConsumed == 2 is handled by the else block as the offsets are the same as for valid input described above

                if (bytesConsumed == 1)
                {
                    if (BitConverter.IsLittleEndian)
                    {
                        fourByteBuffer >>= 8;
                    }
                    else
                    {
                        fourByteBuffer <<= 8;
                    }
                }
                else
                {
                    if (BitConverter.IsLittleEndian)
                    {
                        fourByteBuffer >>= (32 - (bytesLeftInBuffer << 3));
                    }
                    else
                    {
                        fourByteBuffer <<= (32 - (bytesLeftInBuffer << 3));
                    }
                }
                goto DecodeRune;
            }

            Debug.Assert(bytesLeftInBuffer == 0 || bytesLeftInBuffer == 1);

            if ((bytesLeftInBuffer | charsToCopy) == 0)
                return totalCharsConsumed;

            bytesLeftInBuffer *= 3;
            dest.Append(input + totalCharsConsumed - charsToCopy, charsToCopy + bytesLeftInBuffer);
            return totalCharsConsumed + bytesLeftInBuffer;
        }
    }
}
