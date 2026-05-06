// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "signalsafejsonwriter.h"

#include <stdint.h>
#include <string.h>

static
char
ToHexChar(unsigned value)
{
    return (value < 10) ? static_cast<char>('0' + value) : static_cast<char>('a' + (value - 10));
}

void
SignalSafeJsonWriter::Init(
    SignalSafeJsonOutputCallback outputCallback,
    void* outputContext)
{
    m_pos = 0;
    m_commaNeeded = false;
    m_writeFailed = false;
    m_outputCallback = outputCallback;
    m_outputContext = outputContext;
}

bool
SignalSafeJsonWriter::OpenObject(
    const char* key)
{
    WriteSeparator();
    if (key != nullptr)
    {
        WriteEscapedString(key);
        AppendStr(": ");
    }
    AppendChar('{');
    m_commaNeeded = false;
    return !m_writeFailed;
}

bool
SignalSafeJsonWriter::OpenObject()
{
    return OpenObject(nullptr);
}

bool
SignalSafeJsonWriter::CloseObject()
{
    AppendChar('}');
    m_commaNeeded = true;
    return !m_writeFailed;
}

bool
SignalSafeJsonWriter::OpenArray(
    const char* key)
{
    WriteSeparator();
    if (key != nullptr)
    {
        WriteEscapedString(key);
        AppendStr(": ");
    }
    AppendChar('[');
    m_commaNeeded = false;
    return !m_writeFailed;
}

bool
SignalSafeJsonWriter::OpenArray()
{
    return OpenArray(nullptr);
}

bool
SignalSafeJsonWriter::CloseArray()
{
    AppendChar(']');
    m_commaNeeded = true;
    return !m_writeFailed;
}

bool
SignalSafeJsonWriter::WriteString(
    const char* key,
    const char* value)
{
    WriteSeparator();
    WriteEscapedString(key);
    AppendStr(": ");
    WriteEscapedString(value);
    return !m_writeFailed;
}

bool
SignalSafeJsonWriter::Finish()
{
    return Flush();
}

bool
SignalSafeJsonWriter::Flush()
{
    if (m_writeFailed)
    {
        return false;
    }

    if (m_pos == 0)
    {
        return true;
    }

    if (m_outputCallback != nullptr && !m_outputCallback(m_buffer, m_pos, m_outputContext))
    {
        m_writeFailed = true;
        return false;
    }

    m_pos = 0;
    return true;
}

bool
SignalSafeJsonWriter::Append(
    const char* str,
    size_t len)
{
    if (m_writeFailed)
    {
        return false;
    }

    if (str == nullptr)
    {
        // Invalid input mid-document would corrupt the JSON. Latch the
        // failure so subsequent writes become no-ops, matching the
        // behavior when the output callback reports an I/O failure.
        m_writeFailed = true;
        return false;
    }

    if (len == 0)
    {
        return true;
    }

    size_t offset = 0;
    size_t remaining = SIGNAL_SAFE_JSON_BUFFER_SIZE - m_pos;
    while (offset < len)
    {
        if (remaining == 0)
        {
            if (!Flush())
            {
                return false;
            }
            remaining = SIGNAL_SAFE_JSON_BUFFER_SIZE;
        }

        size_t chunk = len - offset;
        if (chunk > remaining)
        {
            chunk = remaining;
        }

        memcpy(m_buffer + m_pos, str + offset, chunk);
        m_pos += chunk;
        offset += chunk;
        remaining -= chunk;
    }

    return true;
}

bool
SignalSafeJsonWriter::AppendChar(char c)
{
    if (m_writeFailed)
    {
        return false;
    }

    if (m_pos == SIGNAL_SAFE_JSON_BUFFER_SIZE && !Flush())
    {
        return false;
    }

    m_buffer[m_pos++] = c;
    return true;
}

bool
SignalSafeJsonWriter::AppendStr(
    const char* str)
{
    if (str == nullptr)
    {
        m_writeFailed = true;
        return false;
    }

    return Append(str, strlen(str));
}

void
SignalSafeJsonWriter::WriteSeparator()
{
    if (m_commaNeeded)
        AppendChar(',');

    m_commaNeeded = true;
}

// Escape a string value for JSON. Handles \, ", and control characters.
void
SignalSafeJsonWriter::WriteEscapedString(
    const char* str)
{
    AppendChar('"');
    if (str != nullptr)
    {
        for (size_t i = 0; str[i]; i++)
        {
            char c = str[i];
            if (c == '"')
                AppendStr("\\\"");
            else if (c == '\\')
                AppendStr("\\\\");
            else if (c == '\n')
                AppendStr("\\n");
            else if (c == '\r')
                AppendStr("\\r");
            else if (c == '\t')
                AppendStr("\\t");
            else if (static_cast<unsigned char>(c) < 0x20)
            {
                char esc[6];
                esc[0] = '\\';
                esc[1] = 'u';
                esc[2] = '0';
                esc[3] = '0';
                esc[4] = ToHexChar((static_cast<unsigned char>(c) >> 4) & 0xF);
                esc[5] = ToHexChar(static_cast<unsigned char>(c) & 0xF);
                Append(esc, sizeof(esc));
            }
            else
            {
                AppendChar(c);
            }
        }
    }

    AppendChar('"');
}

// Bounded, async-signal-safe integer-to-string formatters. They write into the
// caller-supplied buffer and never allocate or call into stdio/locale code.
// If the buffer is too small to hold the maximum-width output (per the
// MAX_*_BUFFER_SIZE constants on SignalSafeJsonWriter), they leave only a null
// terminator and return early.

void
SignalSafeJsonWriter::FormatHexValue(
    char* buffer,
    size_t bufferSize,
    uint64_t value)
{
    if (buffer == nullptr || bufferSize == 0)
    {
        return;
    }

    char reverse[MAX_HEX_DIGITS_UINT64];
    size_t reverseLength = 0;
    do
    {
        unsigned digit = static_cast<unsigned>(value & 0xf);
        reverse[reverseLength++] = static_cast<char>(digit < 10 ? ('0' + digit) : ('a' + digit - 10));
        value >>= 4;
    } while (value != 0 && reverseLength < sizeof(reverse));

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
        buffer[index++] = reverse[--reverseLength];
    }
    buffer[index] = '\0';
}

size_t
SignalSafeJsonWriter::FormatUnsignedDecimal(
    char* buffer,
    size_t bufferSize,
    uint64_t value)
{
    if (buffer == nullptr || bufferSize == 0)
    {
        return 0;
    }

    char reverse[MAX_DECIMAL_DIGITS_UINT64];
    size_t reverseLength = 0;
    do
    {
        reverse[reverseLength++] = static_cast<char>('0' + (value % 10));
        value /= 10;
    } while (value != 0 && reverseLength < sizeof(reverse));

    if (bufferSize < reverseLength + NULL_TERMINATOR_LEN)
    {
        buffer[0] = '\0';
        return 0;
    }

    size_t pos = 0;
    while (reverseLength > 0)
    {
        buffer[pos++] = reverse[--reverseLength];
    }
    buffer[pos] = '\0';
    return pos;
}

size_t
SignalSafeJsonWriter::FormatSignedDecimal(
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

bool
SignalSafeJsonWriter::WriteHexAsString(
    const char* key,
    uint64_t value)
{
    char scratch[MAX_HEX_FORMAT_BUFFER_SIZE];
    FormatHexValue(scratch, sizeof(scratch), value);
    return WriteString(key, scratch);
}

bool
SignalSafeJsonWriter::WriteDecimalAsString(
    const char* key,
    uint64_t value)
{
    char scratch[MAX_UNSIGNED_DECIMAL_BUFFER_SIZE];
    (void)FormatUnsignedDecimal(scratch, sizeof(scratch), value);
    return WriteString(key, scratch);
}

bool
SignalSafeJsonWriter::WriteSignedDecimalAsString(
    const char* key,
    int64_t value)
{
    char scratch[MAX_SIGNED_DECIMAL_BUFFER_SIZE];
    (void)FormatSignedDecimal(scratch, sizeof(scratch), value);
    return WriteString(key, scratch);
}
