// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "signalsafeformat.h"

const char*
SignalSafeFormatter::FormatHex(uint64_t value)
{
    FormatHex(m_hexBuffer, sizeof(m_hexBuffer), value);
    return m_hexBuffer;
}

const char*
SignalSafeFormatter::FormatUnsignedDecimal(uint64_t value)
{
    (void)FormatUnsignedDecimal(m_unsignedDecimalBuffer, sizeof(m_unsignedDecimalBuffer), value);
    return m_unsignedDecimalBuffer;
}

const char*
SignalSafeFormatter::FormatSignedDecimal(int64_t value)
{
    (void)FormatSignedDecimal(m_signedDecimalBuffer, sizeof(m_signedDecimalBuffer), value);
    return m_signedDecimalBuffer;
}

void
SignalSafeFormatter::FormatHex(
    char* buffer,
    size_t bufferSize,
    uint64_t value)
{
    if (buffer == nullptr || bufferSize == 0)
    {
        return;
    }

    size_t reverseLength = 0;
    do
    {
        unsigned digit = static_cast<unsigned>(value & 0xf);
        m_reverse[reverseLength++] = static_cast<char>(digit < 10 ? ('0' + digit) : ('a' + digit - 10));
        value >>= 4;
    } while (value != 0 && reverseLength < MAX_HEX_DIGITS_UINT64);

    if (bufferSize < HEX_PREFIX_LEN + reverseLength + NULL_TERMINATOR_LEN)
    {
        buffer[0] = '\0';
        return;
    }

    buffer[0] = '0';
    buffer[1] = 'x';

    size_t index = HEX_PREFIX_LEN;
    while (reverseLength > 0)
    {
        buffer[index++] = m_reverse[--reverseLength];
    }
    buffer[index] = '\0';
}

size_t
SignalSafeFormatter::FormatUnsignedDecimal(
    char* buffer,
    size_t bufferSize,
    uint64_t value)
{
    if (buffer == nullptr || bufferSize == 0)
    {
        return 0;
    }

    size_t reverseLength = 0;
    do
    {
        m_reverse[reverseLength++] = static_cast<char>('0' + (value % 10));
        value /= 10;
    } while (value != 0 && reverseLength < sizeof(m_reverse));

    if (bufferSize < reverseLength + NULL_TERMINATOR_LEN)
    {
        buffer[0] = '\0';
        return 0;
    }

    size_t pos = 0;
    while (reverseLength > 0)
    {
        buffer[pos++] = m_reverse[--reverseLength];
    }
    buffer[pos] = '\0';
    return pos;
}

size_t
SignalSafeFormatter::FormatSignedDecimal(
    char* buffer,
    size_t bufferSize,
    int64_t value)
{
    if (buffer == nullptr || bufferSize == 0)
    {
        return 0;
    }

    if (value >= 0)
    {
        return FormatUnsignedDecimal(buffer, bufferSize, static_cast<uint64_t>(value));
    }

    if (bufferSize < SIGN_LEN + NULL_TERMINATOR_LEN)
    {
        buffer[0] = '\0';
        return 0;
    }

    buffer[0] = '-';
    // Cast to unsigned first to handle INT64_MIN without signed overflow.
    uint64_t absValue = static_cast<uint64_t>(-(value + 1)) + 1;
    size_t written = FormatUnsignedDecimal(buffer + 1, bufferSize - 1, absValue);
    if (written == 0)
    {
        buffer[0] = '\0';
        return 0;
    }
    return written + 1;
}
