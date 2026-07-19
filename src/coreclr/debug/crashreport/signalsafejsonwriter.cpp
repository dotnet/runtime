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

bool
SignalSafeJsonWriter::WriteHexAsString(
    const char* key,
    uint64_t value)
{
    return WriteString(key, m_formatter.FormatHex(value));
}

bool
SignalSafeJsonWriter::WriteDecimalAsString(
    const char* key,
    uint64_t value)
{
    return WriteString(key, m_formatter.FormatUnsignedDecimal(value));
}

bool
SignalSafeJsonWriter::WriteSignedDecimalAsString(
    const char* key,
    int64_t value)
{
    return WriteString(key, m_formatter.FormatSignedDecimal(value));
}
