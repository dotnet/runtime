using System.Buffers;
using System.Diagnostics;
using System.Text;

namespace System
{
    internal static class PercentEncodingHelper
    {
        public static unsafe int UnescapePercentEncodedUTF8Sequence(char* input, int length, ref ValueStringBuilder dest, bool isQuery, bool iriParsing)
        {
            // As an optimization, this method should only be called after the first character is known to be a part of a non-ascii UTF8 sequence
            Debug.Assert(length >= 3);
            Debug.Assert(input[0] == '%');
            Debug.Assert(UriHelper.EscapedAscii(input[1], input[2]) != Uri.c_DummyChar);
            Debug.Assert(UriHelper.EscapedAscii(input[1], input[2]) >= 128);

            uint fourByteBuffer = 0;
            int bytesLeftInBuffer = 0;

            int totalCharsConsumed = 0;
            int charsToCopy = 0;
            int bytesConsumed = 0;

        RefillBuffer:
            int i = totalCharsConsumed + (bytesLeftInBuffer * 3);

        ReadByteFromInput:
            if ((uint)(i + 2) >= length || input[i] != '%')
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

            uint second = input[i + 2];
            if ((second - '0') <= 9)
            {
                value = ((value << 4) + second) - '0';
            }
            else if ((uint)((second - 'A') & ~0x20) <= ('F' - 'A'))
            {
                value = ((value << 4) + (second | 0x20)) - 'a' + 10;
            }
            else goto NoMoreOrInvalidInput; // Second character wasn't Hex

            Debug.Assert(value >= 128);

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
                Debug.Assert(bytesConsumed >= 2);

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
                Debug.Assert(bytesConsumed > 0);
            }
            charsToCopy += bytesConsumed * 3;

        AfterDecodeRune:
            bytesLeftInBuffer -= bytesConsumed;
            totalCharsConsumed += bytesConsumed * 3;
            goto RefillBuffer;

        NoMoreOrInvalidInput:
            Debug.Assert(bytesLeftInBuffer < 4);
            if (bytesLeftInBuffer > 1)
            {
                Debug.Assert(bytesLeftInBuffer == 2 || bytesLeftInBuffer == 3);
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
