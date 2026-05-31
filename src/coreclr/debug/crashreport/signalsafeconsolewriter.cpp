// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "signalsafeconsolewriter.h"
#include "inproccrashreporter.h"

#include <minipal/log.h>
#include <string.h>

#if defined(__ANDROID__)
#include <android/log.h>
#endif

static const char CRASHREPORT_LINE_SEPARATOR[] = "*** *** *** *** *** *** *** *** *** *** *** *** *** *** *** ***";

void
SignalSafeConsoleWriter::AppendStr(const char* s)
{
    if (s == nullptr || m_pos + 1 >= sizeof(m_buffer))
    {
        return;
    }

    size_t available = sizeof(m_buffer) - 1 - m_pos;
    size_t toCopy = strnlen(s, available);
    if (toCopy != 0)
    {
        memcpy(m_buffer + m_pos, s, toCopy);
        m_pos += toCopy;
    }
}

void
SignalSafeConsoleWriter::AppendChar(char c)
{
    if (m_pos + 1 < sizeof(m_buffer))
    {
        m_buffer[m_pos++] = c;
    }
}

void
SignalSafeConsoleWriter::AppendHex(uint64_t v)
{
    // Skip the leading "0x" so callers control whether the prefix appears
    // (the compact format inserts it verbatim around the value).
    const char* p = m_formatter.FormatHex(v);
    if (p[0] == '0' && p[1] == 'x')
    {
        p += 2;
    }
    AppendStr(p);
}

void
SignalSafeConsoleWriter::AppendDecimal(uint64_t v)
{
    AppendStr(m_formatter.FormatUnsignedDecimal(v));
}

void
SignalSafeConsoleWriter::AppendSignedDecimal(int64_t v)
{
    AppendStr(m_formatter.FormatSignedDecimal(v));
}

void
SignalSafeConsoleWriter::EndLine()
{
    // On Android, __android_log_write in Flush() adds its own line discipline.
    // For other platforms, we still need to add a newline.
#if !defined(__ANDROID__)
    if (m_pos + 1 < sizeof(m_buffer))
    {
        m_buffer[m_pos++] = '\n';
    }
    else
    {
        m_buffer[sizeof(m_buffer) - 2] = '\n';
        m_pos = sizeof(m_buffer) - 1;
    }
#endif // !__ANDROID__
    Flush();
}

void
SignalSafeConsoleWriter::WriteLine(const char* s)
{
    AppendStr(s);
    EndLine();
}

void
SignalSafeConsoleWriter::WriteKeyValueStr(const char* key, const char* value)
{
    AppendStr(key);
    AppendStr(": ");
    AppendStr(value != nullptr ? value : "");
    EndLine();
}

void
SignalSafeConsoleWriter::WriteKeyValueDecimal(const char* key, uint64_t value)
{
    AppendStr(key);
    AppendStr(": ");
    AppendDecimal(value);
    EndLine();
}

void
SignalSafeConsoleWriter::WriteSeparator()
{
    WriteLine(CRASHREPORT_LINE_SEPARATOR);
}

void
SignalSafeConsoleWriter::Flush()
{
    // Always null-terminate so the platform write APIs see a proper C string.
    if (m_pos < sizeof(m_buffer))
    {
        m_buffer[m_pos] = '\0';
    }
    else
    {
        m_buffer[sizeof(m_buffer) - 1] = '\0';
    }

#if defined(__ANDROID__)
    // __android_log_write expects a tag + null-terminated message; it adds its
    // own line discipline so we deliberately do not append '\n'. Each call
    // becomes one logcat entry, which is what makes per-line filtering useful.
    __android_log_write(ANDROID_LOG_FATAL, CRASHREPORT_LOG_TAG, m_buffer);
#else
    minipal_log_write_error(m_buffer);
#endif

    m_pos = 0;
    m_buffer[0] = '\0';
}
