// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Streaming JSON writer implementation for crash reports.

#include "crashjsonwriter.h"

#include <string.h>

static
char
ToHexChar(
    unsigned value);

void
CrashJsonWriter::Init(
    CrashJsonOutputCallback outputCallback,
    void* outputContext)
{
    m_pos = 0;
    m_commaNeeded = false;
    m_writeFailed = false;
    m_outputCallback = outputCallback;
    m_outputContext = outputContext;
    m_buffer[0] = '\0';
}

void
CrashJsonWriter::OpenObject(
    const char* key)
{
    WriteSeparator();
    if (key != nullptr)
    {
        WriteEscapedString(key);
        AppendStr(": ");
    }
    AppendStr("{");
    m_commaNeeded = false;
}

void
CrashJsonWriter::CloseObject()
{
    AppendStr("}");
    m_commaNeeded = true;
}

void
CrashJsonWriter::OpenArray(
    const char* key)
{
    WriteSeparator();
    if (key != nullptr)
    {
        WriteEscapedString(key);
        AppendStr(": ");
    }
    AppendStr("[");
    m_commaNeeded = false;
}

void
CrashJsonWriter::CloseArray()
{
    AppendStr("]");
    m_commaNeeded = true;
}

void
CrashJsonWriter::WriteString(
    const char* key,
    const char* value)
{
    WriteSeparator();
    WriteEscapedString(key);
    AppendStr(": ");
    WriteEscapedString(value);
}

void
CrashJsonWriter::Finish()
{
    (void)Flush();
}

bool
CrashJsonWriter::HasFailed() const
{
    return m_writeFailed;
}

bool
CrashJsonWriter::Flush()
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
    m_buffer[0] = '\0';
    return true;
}

bool
CrashJsonWriter::Append(
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
    while (offset < len)
    {
        size_t remaining = (CRASH_JSON_BUFFER_SIZE - 1) - m_pos;
        if (remaining == 0 && !Flush())
        {
            return false;
        }

        remaining = (CRASH_JSON_BUFFER_SIZE - 1) - m_pos;
        size_t chunk = len - offset;
        if (chunk > remaining)
        {
            chunk = remaining;
        }

        memcpy(m_buffer + m_pos, str + offset, chunk);
        m_pos += chunk;
        offset += chunk;
    }

    return true;
}

bool
CrashJsonWriter::AppendStr(
    const char* str)
{
    if (str == nullptr)
    {
        m_writeFailed = true;
        return false;
    }

    return Append(str, strlen(str));
}

char
ToHexChar(
    unsigned value)
{
    return (value < 10) ? static_cast<char>('0' + value) : static_cast<char>('a' + (value - 10));
}

void
CrashJsonWriter::WriteSeparator()
{
    if (m_commaNeeded)
        AppendStr(",");

    m_commaNeeded = true;
}

// Escape a string value for JSON. Handles \, ", and control characters.
void
CrashJsonWriter::WriteEscapedString(
    const char* str)
{
    AppendStr("\"");
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
                char esc[7];
                esc[0] = '\\';
                esc[1] = 'u';
                esc[2] = '0';
                esc[3] = '0';
                esc[4] = ToHexChar((static_cast<unsigned char>(c) >> 4) & 0xF);
                esc[5] = ToHexChar(static_cast<unsigned char>(c) & 0xF);
                esc[6] = '\0';
                AppendStr(esc);
            }
            else
            {
                Append(&c, 1);
            }
        }
    }

    AppendStr("\"");
}
