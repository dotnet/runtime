// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <errno.h>
#include <limits.h>
#include <string.h>
#include <inttypes.h>

#include <minipal/utf8.h>

#ifdef _MSC_VER
#define BYTESWAP16 _byteswap_ushort
#else
#define BYTESWAP16 __builtin_bswap16
#endif // MSC_VER

size_t minipal_get_length_utf8_to_utf16(const char* source, size_t sourceLength, uint32_t flags)
{
    errno = 0;

    if (sourceLength == 0)
        return 0;

    size_t sourceIndex = 0;
    size_t destinationLength = 0;
    bool useFallback = !(flags & MINIPAL_MB_NO_REPLACE_INVALID_CHARS);

    while (sourceIndex < sourceLength)
    {
        unsigned char currentChar = source[sourceIndex];
        size_t sequenceLength = 0;
        uint32_t codePoint = 0;

        if (currentChar <= 0x7F)
        {
            destinationLength++;
            sourceIndex++;
            continue;
        }

        if ((currentChar & 0xC0) == 0x80 || (currentChar & 0xFE) == 0xFE || (currentChar & 0xFF) == 0xFF)
        {
            if (useFallback)
            {
                destinationLength++;
                sourceIndex++;
            }
            else
            {
                errno = MINIPAL_ERROR_NO_UNICODE_TRANSLATION;
                return 0;
            }
            continue;
        }

        if ((currentChar & 0xE0) == 0xC0)
        {
            sequenceLength = 2;
        }
        else if ((currentChar & 0xF0) == 0xE0)
        {
            sequenceLength = 3;
        }
        else if ((currentChar & 0xF8) == 0xF0)
        {
            sequenceLength = 4;
        }
        else
        {
            if (useFallback)
            {
                destinationLength++;
                sourceIndex++;
            }
            else
            {
                errno = MINIPAL_ERROR_NO_UNICODE_TRANSLATION;
                return 0;
            }
            continue;
        }

        if (sourceIndex + sequenceLength > sourceLength)
        {
            if (useFallback)
            {
                destinationLength += sequenceLength >= 3 ? 2 : 1;
                sourceIndex = sourceLength;
            }
            else
            {
                errno = MINIPAL_ERROR_NO_UNICODE_TRANSLATION;
                return 0;
            }
            continue;
        }

        bool validSequence = true;
        for (size_t i = 1; i < sequenceLength; ++i)
        {
            if ((source[sourceIndex + i] & 0xC0) != 0x80)
            {
                validSequence = false;
                break;
            }
        }

        if (!validSequence)
        {
            if (useFallback)
            {
                destinationLength++;

                do
                {
                    sourceIndex++;
                }
                while (sourceIndex < sourceLength && (source[sourceIndex] & 0xC0) == 0x80);
                continue;
            }
            else
            {
                errno = MINIPAL_ERROR_NO_UNICODE_TRANSLATION;
                return 0;
            }
        }

        if ((currentChar == 0xC0 && sourceIndex + 1 < sourceLength && (unsigned char)source[sourceIndex + 1] == 0x80) ||
            (currentChar == 0xE0 && sourceIndex + 2 < sourceLength &&
            (unsigned char)source[sourceIndex + 1] == 0x80 && (unsigned char)source[sourceIndex + 2] == 0x80) ||
            (currentChar == 0xF0 && sourceIndex + 3 < sourceLength &&
            (unsigned char)source[sourceIndex + 1] == 0x80 && (unsigned char)source[sourceIndex + 2] == 0x80 &&
            (unsigned char)source[sourceIndex + 3] == 0x80))
        {
            if (useFallback)
            {
                if (sequenceLength < 3)
                    destinationLength++;
                sourceIndex++;  // Move to the end of the invalid sequence
                continue;
            }
            else
            {
                errno = MINIPAL_ERROR_NO_UNICODE_TRANSLATION;
                return 0;
            }
        }

        if (sequenceLength == 2)
        {
            codePoint = ((currentChar & 0x1F) << 6) | (source[sourceIndex + 1] & 0x3F);
        }
        else if (sequenceLength == 3)
        {
            codePoint = ((currentChar & 0x0F) << 12) | ((source[sourceIndex + 1] & 0x3F) << 6) | (source[sourceIndex + 2] & 0x3F);
        }
        else if (sequenceLength == 4)
        {
            codePoint = ((currentChar & 0x07) << 18) | ((source[sourceIndex + 1] & 0x3F) << 12) | ((source[sourceIndex + 2] & 0x3F) << 6) | (source[sourceIndex + 3] & 0x3F);
        }

        if (sequenceLength < 4 || codePoint <= 0xFFFF)
        {
            destinationLength++;
        }
        else
        {
            destinationLength += 2;
        }

        sourceIndex += sequenceLength;
    }

    return destinationLength;
}

size_t minipal_get_length_utf16_to_utf8(const CHAR16_T* source, size_t sourceLength, uint32_t flags)
{
    errno = 0;

    if (sourceLength == 0)
        return 0;

    size_t sourceIndex = 0;
    size_t destinationLength = 0;
    bool useFallback = !(flags & MINIPAL_MB_NO_REPLACE_INVALID_CHARS);

    while (sourceIndex < sourceLength)
    {
        CHAR16_T currentChar = source[sourceIndex];
        size_t sequenceLength = 0;

        if (currentChar <= 0x7F)
        {
            sequenceLength = 1;
        }
        else if (currentChar <= 0x7FF)
        {
            sequenceLength = 2;
        }
        else if (currentChar >= 0xD800 && currentChar <= 0xDBFF)
        {
            if (sourceIndex + 1 >= sourceLength || (source[sourceIndex + 1] < 0xDC00 || source[sourceIndex + 1] > 0xDFFF))
            {
                if (useFallback)
                {
                    sequenceLength = 3; // Replacement character
                }
                else
                {
                    errno = MINIPAL_ERROR_NO_UNICODE_TRANSLATION;
                    return 0;
                }
            }
            else
            {
                sequenceLength = 4;
                sourceIndex++; // Skip the second part of the surrogate pair
            }
        }
        else if (currentChar >= 0xDC00 && currentChar <= 0xDFFF)
        {
            if (useFallback)
            {
                sequenceLength = 3; // Replacement character
            }
            else
            {
                errno = MINIPAL_ERROR_NO_UNICODE_TRANSLATION;
                return 0;
            }
        }
        else
        {
            sequenceLength = 3;
        }

        destinationLength += sequenceLength;
        sourceIndex++;
    }

    return destinationLength;
}

size_t minipal_convert_utf8_to_utf16(const char* source, size_t sourceLength, CHAR16_T* destination, size_t destinationLength, uint32_t flags)
{
    errno = 0;

    if (sourceLength == 0)
    {
        return 0;
    }

    size_t sourceIndex = 0;
    size_t destinationIndex = 0;
    bool useFallback = !(flags & MINIPAL_MB_NO_REPLACE_INVALID_CHARS);
#if BIGENDIAN
    bool treatAsLE = !!(flags & MINIPAL_TREAT_AS_LITTLE_ENDIAN);
#endif

    while (sourceIndex < sourceLength && destinationIndex < destinationLength)
    {
        uint32_t currentChar = (unsigned char)source[sourceIndex];

        if (currentChar < 0x80)
        {
            // 1-byte sequence
            sourceIndex++;
        }
        else if (currentChar < 0xE0)
        {
            // 2-byte sequence - based on ABNF (Augmented Backus-Naur Form) syntax described at
            // https://datatracker.ietf.org/doc/html/rfc3629#section-4
            if (sourceIndex + 1 >= sourceLength ||
                (currentChar == 0xC0 || currentChar == 0xC1) ||
                (source[sourceIndex + 1] & 0xC0) != 0x80)
            {
                if (useFallback)
                {
                    currentChar = 0xFFFD;
                    sourceIndex++;
                }
                else
                {
                    errno = MINIPAL_ERROR_NO_UNICODE_TRANSLATION;
                    return 0;
                }
            }
            else
            {
                currentChar = ((currentChar & 0x1F) << 6) | (source[sourceIndex + 1] & 0x3F);
                sourceIndex += 2;
            }
        }
        else if (currentChar < 0xF0)
        {
            // 3-byte sequence - based on ABNF (Augmented Backus-Naur Form) syntax described at
            // https://datatracker.ietf.org/doc/html/rfc3629#section-4
            if (sourceIndex + 2 >= sourceLength ||
                (currentChar == 0xE0 && (source[sourceIndex + 1] & 0xE0) != 0xA0) ||
                (currentChar >= 0xE1 && currentChar <= 0xEC && (source[sourceIndex + 1] & 0xC0) != 0x80) ||
                (currentChar == 0xED && (source[sourceIndex + 1] & 0xF0) != 0x80) ||
                (currentChar >= 0xEE && currentChar <= 0xEF && (source[sourceIndex + 1] & 0xC0) != 0x80) ||
                (source[sourceIndex + 2] & 0xC0) != 0x80)
            {
                if (useFallback)
                {
                    uint32_t nextChar = 0;
                    uint8_t missingBytes = 0;

                    // Check second byte validity
                    if (sourceIndex + 1 < sourceLength)
                    {
                        nextChar = (unsigned char)source[sourceIndex + 1];
                    }
                    if ((nextChar & 0xC0) != 0x80)
                    {
                        missingBytes = 1;
                    }
                    else
                    {
                        // Check third byte validity
                        if (sourceIndex + 2 < sourceLength)
                        {
                            nextChar = (unsigned char)source[sourceIndex + 2];
                            if ((nextChar & 0xC0) != 0x80 ||
                                (currentChar == 0xE0 && (nextChar < 0xA0 || nextChar > 0xBF)) ||
                                (currentChar == 0xED && (nextChar < 0x80 || nextChar > 0x9F)))
                            {
                                missingBytes = 2;
                            }
                            else
                            {
                                missingBytes = 3;
                            }
                        }
                        else
                        {
                            missingBytes = 2; // Only one byte is guaranteed missing
                        }
                    }

                    currentChar = 0xFFFD;  // Replacement character for invalid sequences
                    sourceIndex += missingBytes;
                }
                else
                {
                    errno = MINIPAL_ERROR_NO_UNICODE_TRANSLATION;
                    return 0;
                }
            }
            else
            {
                currentChar = ((currentChar & 0x0F) << 12) | ((source[sourceIndex + 1] & 0x3F) << 6) | (source[sourceIndex + 2] & 0x3F);
                sourceIndex += 3;
            }
        }
        else if (currentChar >= 0xF0)
        {
            // 4-byte sequence - based on ABNF (Augmented Backus-Naur Form) syntax described at
            // https://datatracker.ietf.org/doc/html/rfc3629#section-4
            if ((sourceIndex + 3 >= sourceLength) ||
                ((unsigned char)source[sourceIndex] < 0xF0 || (unsigned char)source[sourceIndex] > 0xF4) ||
                ((unsigned char)source[sourceIndex] == 0xF0 &&
                    ((sourceIndex + 1 >= sourceLength || (unsigned char)source[sourceIndex + 1] < 0x90 || (unsigned char)source[sourceIndex + 1] > 0xBF) ||
                    (sourceIndex + 2 >= sourceLength || (unsigned char)source[sourceIndex + 2] < 0x80 || (unsigned char)source[sourceIndex + 2] > 0xBF) ||
                    (sourceIndex + 3 >= sourceLength || (unsigned char)source[sourceIndex + 3] < 0x80 || (unsigned char)source[sourceIndex + 3] > 0xBF))) ||
                ((unsigned char)source[sourceIndex] >= 0xF1 && (unsigned char)source[sourceIndex] <= 0xF3 &&
                    ((sourceIndex + 1 >= sourceLength || (unsigned char)source[sourceIndex + 1] < 0x80 || (unsigned char)source[sourceIndex + 1] > 0xBF) ||
                    (sourceIndex + 2 >= sourceLength || (unsigned char)source[sourceIndex + 2] < 0x80 || (unsigned char)source[sourceIndex + 2] > 0xBF) ||
                    (sourceIndex + 3 >= sourceLength || (unsigned char)source[sourceIndex + 3] < 0x80 || (unsigned char)source[sourceIndex + 3] > 0xBF))) ||
                ((unsigned char)source[sourceIndex] == 0xF4 &&
                    ((sourceIndex + 1 >= sourceLength || (unsigned char)source[sourceIndex + 1] < 0x80 || (unsigned char)source[sourceIndex + 1] > 0x8F) ||
                    (sourceIndex + 2 >= sourceLength || (unsigned char)source[sourceIndex + 2] < 0x80 || (unsigned char)source[sourceIndex + 2] > 0xBF) ||
                    (sourceIndex + 3 >= sourceLength || (unsigned char)source[sourceIndex + 3] < 0x80 || (unsigned char)source[sourceIndex + 3] > 0xBF))))
            {
                if (useFallback)
                {
                    uint8_t missingBytes = 0;

                    // Check second byte validity
                    if (sourceIndex + 1 < sourceLength && ((unsigned char)source[sourceIndex + 1] & 0xC0) == 0x80)
                    {
                        missingBytes++;
                    }

                    // Check third byte validity
                    if (sourceIndex + 2 < sourceLength && ((unsigned char)source[sourceIndex + 2] & 0xC0) == 0x80)
                    {
                        missingBytes++;
                    }

                    // Check fourth byte validity
                    if (sourceIndex + 3 < sourceLength)
                    {
                        uint8_t fourthByte = source[sourceIndex + 3];
                        if ((fourthByte & 0xC0) == 0x80)
                        {
                            missingBytes++;
                        }

                        // Increment missingBytes for each invalid byte
                        missingBytes = 5 - missingBytes;
                        if (fourthByte > 0)
                        {
                            missingBytes += 3;
                        }
                    }
                    else
                    {
                        missingBytes += 1;
                    }

                    if(missingBytes > 4)
                    {
                        destination[destinationIndex++] = 0xFFFD;
                        destination[destinationIndex++] = 0xFFFD;
                        destination[destinationIndex++] = 0xFFFD;

                        #if BIGENDIAN
                        if (!treatAsLE)
                        {
                            destination[destinationIndex - 3] = BYTESWAP16(destination[destinationIndex - 2]);
                            destination[destinationIndex - 2] = BYTESWAP16(destination[destinationIndex - 2]);
                            destination[destinationIndex - 1] = BYTESWAP16(destination[destinationIndex - 1]);
                        }
                        #endif

                        destinationIndex++;
                        sourceIndex += missingBytes;

                        continue;
                    }

                    currentChar = 0xFFFD;  // Replacement character for invalid sequences
                    sourceIndex += missingBytes;
                }
                else
                {
                    errno = MINIPAL_ERROR_NO_UNICODE_TRANSLATION;
                    return 0;
                }
            }
            else
            {
                currentChar = ((currentChar & 0x07) << 18) | ((source[sourceIndex + 1] & 0x3F) << 12) | ((source[sourceIndex + 2] & 0x3F) << 6) | (source[sourceIndex + 3] & 0x3F);
                sourceIndex += 4;

                if (currentChar >= 0x10000)
                {
                    // Surrogate pair
                    currentChar -= 0x10000;
                    if (destinationIndex + 1 >= destinationLength)
                    {
                        errno = MINIPAL_ERROR_INSUFFICIENT_BUFFER;
                        return 0;
                    }

                    destination[destinationIndex++] = (CHAR16_T)(0xD800 | (currentChar >> 10));

                    #if BIGENDIAN
                    if (!treatAsLE)
                    {
                        destination[destinationIndex - 1] = BYTESWAP16(destination[destinationIndex - 1]);
                    }
                    #endif
                    currentChar = 0xDC00 | (currentChar & 0x3FF);
                }
            }
        }

        if (destinationIndex >= destinationLength)
        {
            errno = MINIPAL_ERROR_INSUFFICIENT_BUFFER;
            return 0;
        }

        #if BIGENDIAN
        if (!treatAsLE)
        {
            currentChar = BYTESWAP16(currentChar);
        }
        #endif

        destination[destinationIndex++] = (CHAR16_T)currentChar;
    }

    if (sourceIndex < sourceLength && (destinationIndex > destinationLength || (destinationIndex == destinationLength && sourceIndex + 1 < sourceLength)))
    {
        errno = MINIPAL_ERROR_INSUFFICIENT_BUFFER;
        return 0;
    }

    return destinationIndex;
}

size_t minipal_convert_utf16_to_utf8(const CHAR16_T* source, size_t sourceLength, char* destination, size_t destinationLength, uint32_t flags)
{
    errno = 0;

    if (sourceLength == 0)
        return 0;

    size_t sourceIndex = 0;
    size_t destinationIndex = 0;
    bool useFallback = !(flags & MINIPAL_MB_NO_REPLACE_INVALID_CHARS);
#if BIGENDIAN
    bool treatAsLE = !!(flags & MINIPAL_TREAT_AS_LITTLE_ENDIAN);
#endif

    while (sourceIndex < sourceLength && destinationIndex < destinationLength)
    {
        uint32_t currentChar = source[sourceIndex];
        #if BIGENDIAN
        if (!treatAsLE)
        {
            currentChar = BYTESWAP16(currentChar);
        }
        #endif

        if (currentChar >= 0xD800 && currentChar <= 0xDBFF)
        {
            if (sourceIndex + 1 >= sourceLength)
            {
                if (!useFallback)
                {
                    errno = MINIPAL_ERROR_NO_UNICODE_TRANSLATION;
                    return 0;
                }
                else
                {
                    if (destinationIndex + 3 >= destinationLength)
                    {
                        errno = MINIPAL_ERROR_INSUFFICIENT_BUFFER;
                        return 0;
                    }

                    // Replacement characters
                    destination[destinationIndex++] = 0xEF;
                    destination[destinationIndex++] = 0xBF;
                    destination[destinationIndex++] = 0xBD;

                    sourceIndex++;

                    continue;
                }
            }

            uint32_t nextChar = source[sourceIndex + 1];
            #if BIGENDIAN
            if (!treatAsLE)
            {
                nextChar = BYTESWAP16(nextChar);
            }
            #endif

            if (nextChar < 0xDC00 || nextChar > 0xDFFF)
            {
                if (!useFallback)
                {
                    errno = MINIPAL_ERROR_NO_UNICODE_TRANSLATION;
                    return 0;
                }
                else
                {
                    if (destinationIndex + 3 >= destinationLength)
                    {
                        errno = MINIPAL_ERROR_INSUFFICIENT_BUFFER;
                        return 0;
                    }

                    // Replacement characters
                    destination[destinationIndex++] = 0xEF;
                    destination[destinationIndex++] = 0xBF;
                    destination[destinationIndex++] = 0xBD;

                    sourceIndex++;

                    continue;
                }
            }

            // Combine the surrogate pair into one code point
            if (nextChar >= 0xDC00 && nextChar <= 0xDFFF)
            {
                currentChar = (((currentChar - 0xD800) << 10) | (nextChar - 0xDC00)) + 0x10000;
                sourceIndex++;
            }
        }
        else if (currentChar >= 0xDC00 && currentChar <= 0xDFFF)
        {
            if (!useFallback)
            {
                errno = MINIPAL_ERROR_NO_UNICODE_TRANSLATION;
                return 0;
            }
            else
            {
                if (destinationIndex + 3 >= destinationLength)
                {
                    errno = MINIPAL_ERROR_INSUFFICIENT_BUFFER;
                    return 0;
                }

                // Replacement characters
                destination[destinationIndex++] = 0xEF;
                destination[destinationIndex++] = 0xBF;
                destination[destinationIndex++] = 0xBD;

                sourceIndex++;

                continue;
            }
        }

        if (currentChar < 0x80)
        {
            if ((short)(destinationIndex - 1) > (int)destinationLength)
            {
                errno = MINIPAL_ERROR_INSUFFICIENT_BUFFER;
                return 0;
            }
            destination[destinationIndex++] = (char)currentChar;
        }
        else if (currentChar < 0x800)
        {
            if (destinationIndex > destinationLength)
            {
                errno = MINIPAL_ERROR_INSUFFICIENT_BUFFER;
                return 0;
            }
            destination[destinationIndex++] = (char)(0xC0 | (currentChar >> 6));
            destination[destinationIndex++] = (char)(0x80 | (currentChar & 0x3F));
        }
        else if (currentChar < 0x10000)
        {
            if (destinationIndex + 1 > destinationLength)
            {
                errno = MINIPAL_ERROR_INSUFFICIENT_BUFFER;
                return 0;
            }
            destination[destinationIndex++] = (char)(0xE0 | (currentChar >> 12));
            destination[destinationIndex++] = (char)(0x80 | ((currentChar >> 6) & 0x3F));
            destination[destinationIndex++] = (char)(0x80 | (currentChar & 0x3F));
        }
        else
        {
            if (destinationIndex + 2 > destinationLength)
            {
                errno = MINIPAL_ERROR_INSUFFICIENT_BUFFER;
                return 0;
            }
            destination[destinationIndex++] = (char)(0xF0 | (currentChar >> 18));
            destination[destinationIndex++] = (char)(0x80 | ((currentChar>> 12) & 0x3F));
            destination[destinationIndex++] = (char)(0x80 | ((currentChar >> 6) & 0x3F));
            destination[destinationIndex++] = (char)(0x80 | (currentChar & 0x3F));
        }

        sourceIndex++;
    }

    if (sourceIndex < sourceLength && destinationIndex >= destinationLength)
    {
        errno = MINIPAL_ERROR_INSUFFICIENT_BUFFER;
        return 0;
    }

    return destinationIndex;
}
