// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "signalsafeformatter.h"

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

const char*
SignalSafeFormatter::FormatGuid(const GUID& guid)
{
    FormatGuid(m_guidBuffer, sizeof(m_guidBuffer), guid);
    return m_guidBuffer;
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

    if (bufferSize < 2 + reverseLength + 1) // "0x" + digits + '\0'
    {
        buffer[0] = '\0';
        return;
    }

    buffer[0] = '0';
    buffer[1] = 'x';

    size_t index = 2; // Skip past "0x" prefix
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

    if (bufferSize < reverseLength + 1) // digits + '\0'
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

    if (bufferSize < 1 + 1) // '-' + '\0' minimum
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

void
SignalSafeFormatter::FormatGuid(
    char* buffer,
    size_t bufferSize,
    const GUID& guid)
{
    if (buffer == nullptr || bufferSize == 0)
    {
        return;
    }

    if (bufferSize < MAX_GUID_BUFFER_SIZE)
    {
        buffer[0] = '\0';
        return;
    }

    size_t pos = 0;
    buffer[pos++] = '{';
    AppendFixedHex(buffer, &pos, guid.Data1, 8);
    buffer[pos++] = '-';
    AppendFixedHex(buffer, &pos, guid.Data2, 4);
    buffer[pos++] = '-';
    AppendFixedHex(buffer, &pos, guid.Data3, 4);
    buffer[pos++] = '-';
    AppendFixedHex(buffer, &pos, guid.Data4[0], 2);
    AppendFixedHex(buffer, &pos, guid.Data4[1], 2);
    buffer[pos++] = '-';
    AppendFixedHex(buffer, &pos, guid.Data4[2], 2);
    AppendFixedHex(buffer, &pos, guid.Data4[3], 2);
    AppendFixedHex(buffer, &pos, guid.Data4[4], 2);
    AppendFixedHex(buffer, &pos, guid.Data4[5], 2);
    AppendFixedHex(buffer, &pos, guid.Data4[6], 2);
    AppendFixedHex(buffer, &pos, guid.Data4[7], 2);
    buffer[pos++] = '}';
    buffer[pos] = '\0';
}

char
SignalSafeFormatter::GetHexDigit(uint32_t value)
{
    value &= 0xf;
    return static_cast<char>(value < 10 ? ('0' + value) : ('a' + value - 10));
}

void
SignalSafeFormatter::AppendFixedHex(
    char* buffer,
    size_t* pos,
    uint32_t value,
    uint32_t digits)
{
    for (uint32_t i = digits; i != 0; --i)
    {
        buffer[(*pos)++] = GetHexDigit(value >> ((i - 1) * 4));
    }
}
