// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Bounded, signal-safe JSON writer.
// Streams content through a small fixed-size buffer using bounded low-level
// string and memory operations so file output does not require materializing
// the whole document at once. All public members are async-signal-safe: no
// heap allocation, no stdio, no locale or variadic formatting.

#pragma once

#include <stddef.h>
#include <stdint.h>

#include "signalsafeformatter.h"

using SignalSafeJsonOutputCallback = bool (*)(const char* buffer, size_t len, void* ctx);

static constexpr size_t SIGNAL_SAFE_JSON_BUFFER_SIZE = 4 * 1024;

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
    }

    SignalSafeJsonWriter(const SignalSafeJsonWriter&) = delete;
    SignalSafeJsonWriter& operator=(const SignalSafeJsonWriter&) = delete;

    void Init(SignalSafeJsonOutputCallback outputCallback, void* outputContext);
    bool OpenObject(const char* key);
    bool OpenObject();
    bool CloseObject();
    bool OpenArray(const char* key);
    bool OpenArray();
    bool CloseArray();
    bool WriteString(const char* key, const char* value);
    bool WriteHexAsString(const char* key, uint64_t value);
    bool WriteDecimalAsString(const char* key, uint64_t value);
    bool WriteSignedDecimalAsString(const char* key, int64_t value);
    bool Finish();
    bool Flush();

private:
    bool Append(const char* str, size_t len);
    bool AppendChar(char c);
    bool AppendStr(const char* str);
    void WriteSeparator();
    void WriteEscapedString(const char* str);

    SignalSafeFormatter m_formatter;
    char m_buffer[SIGNAL_SAFE_JSON_BUFFER_SIZE];
    size_t m_pos;
    bool m_commaNeeded;
    bool m_writeFailed;
    SignalSafeJsonOutputCallback m_outputCallback;
    void* m_outputContext;
};
