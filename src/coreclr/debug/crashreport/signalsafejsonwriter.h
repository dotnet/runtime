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

// Value type describing where the JSON writer flushes buffered document chunks.
// Mirrors SignalSafeConsoleOutputSink so both signal-safe writers share the same
// bool-returning callback shape. Copyable so it can be assigned into the writer.
class SignalSafeJsonOutputSink
{
public:
    // Sink for a buffered document chunk. Returns false on write failure, which
    // the writer latches to make subsequent writes no-ops. Must be
    // async-signal-safe.
    using Callback = bool (*)(const char* buffer, size_t len, void* context);

    SignalSafeJsonOutputSink(Callback callback, void* context)
        : m_callback(callback)
        , m_context(context)
    {
    }

    SignalSafeJsonOutputSink(const SignalSafeJsonOutputSink&) = default;
    SignalSafeJsonOutputSink& operator=(const SignalSafeJsonOutputSink&) = default;

    // A null callback drops output and reports success.
    bool Write(const char* buffer, size_t len) const
    {
        return m_callback == nullptr || m_callback(buffer, len, m_context);
    }

private:
    Callback m_callback;
    void* m_context;
};

static constexpr size_t SIGNAL_SAFE_JSON_BUFFER_SIZE = 4 * 1024;

class SignalSafeJsonWriter
{
public:
    // Default-constructed writers drop all output (the drop-all default sink).
    SignalSafeJsonWriter()
        : m_pos(0),
          m_commaNeeded(false),
          m_writeFailed(false),
          m_sink(DropAllOutputSink())
    {
    }

    SignalSafeJsonWriter(const SignalSafeJsonWriter&) = delete;
    SignalSafeJsonWriter& operator=(const SignalSafeJsonWriter&) = delete;

    // Re-points the sink and resets writer state for a fresh document. Pass
    // DropAllOutputSink() to suppress output.
    void SetOutputSink(const SignalSafeJsonOutputSink& sink);

    // A drop-all sink that discards output and reports success.
    static const SignalSafeJsonOutputSink& DropAllOutputSink();

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
    SignalSafeJsonOutputSink m_sink;
};
