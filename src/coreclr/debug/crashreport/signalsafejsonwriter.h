// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Bounded JSON writer for crash reports.
// Streams content through a small fixed-size buffer using bounded low-level
// string and memory operations so file output does not require materializing
// the whole report at once. All public members are async-signal-safe: no
// heap allocation, no stdio, no locale or variadic formatting.

#pragma once

#include <stddef.h>

using CrashJsonOutputCallback = bool (*)(const char* buffer, size_t len, void* ctx);

// Small streaming buffer used when serializing the crash report JSON.
static constexpr size_t CRASH_JSON_BUFFER_SIZE = 4 * 1024;

class SignalSafeJsonWriter
{
public:
    SignalSafeJsonWriter()
        : m_pos(0),
          m_commaNeeded(false),
          m_writeFailed(false),
          m_outputCallback(nullptr),
          m_outputContext(nullptr)
    {
        m_buffer[0] = '\0';
    }

    SignalSafeJsonWriter(const SignalSafeJsonWriter&) = delete;
    SignalSafeJsonWriter& operator=(const SignalSafeJsonWriter&) = delete;

    void Init(CrashJsonOutputCallback outputCallback, void* outputContext);
    void OpenObject(const char* key);
    void CloseObject();
    void OpenArray(const char* key);
    void CloseArray();
    void WriteString(const char* key, const char* value);
    void Finish();
    bool Flush();
    bool HasError() const;

private:
    bool Append(const char* str, size_t len);
    bool AppendStr(const char* str);
    void WriteSeparator();
    void WriteEscapedString(const char* str);

    char m_buffer[CRASH_JSON_BUFFER_SIZE];
    size_t m_pos;
    bool m_commaNeeded;
    bool m_writeFailed;
    CrashJsonOutputCallback m_outputCallback;
    void* m_outputContext;
};
