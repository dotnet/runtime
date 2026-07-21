// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Bounded, signal-safe line-oriented console writer. Paired with
// SignalSafeJsonWriter as the second crash-report output sink:
// SignalSafeJsonWriter streams JSON to a file callback (compact, no
// line concept); SignalSafeConsoleWriter emits one logical line at a
// time to the platform console (Android logcat under CRASHREPORT_LOG_TAG
// tag, stderr on Apple mobile platforms). All public members are async-signal-safe: no
// heap allocation, no stdio, no locale or variadic formatting.
//
// Design choices below are driven by the prescribed compact crash report
// log format (specified at the top of inproccrashreporter.cpp):
//
// * One Flush per logical line (triggered by EndLine() / WriteLine())
//   instead of stream-buffer-fill flushing. Each call becomes exactly one
//   __android_log_write entry on Android, so the format's line-oriented
//   "header / per-thread block / modules / footer" structure maps 1:1
//   to logcat entries that filter cleanly under the crash-report tag without
//   cutting fields in half. On
//   iOS, tvOS, and MacCatalyst each EndLine adds an explicit '\n' for the same reason.
//
// * Unique crash-report logcat tag (distinct from the runtime's general
//   "DOTNET" tag) so consumers can isolate the crash report from
//   an otherwise noisy logcat with a single per-tag filter.
//
// * Best-effort silent truncation on per-line buffer overflow (Append*
//   helpers all guard with `m_pos + 1 < sizeof(m_buffer)`). 512 bytes
//   leaves comfortable headroom over the longest line the format
//   produces (a fully-qualified Class.Method line at roughly
//   CRASHREPORT_STRING_BUFFER_SIZE + line decoration), so truncation is
//   reserved for unforeseen overrun and never fails any other
//   crash-report output.

#pragma once

#include <stddef.h>
#include <stdint.h>

#include "signalsafeformatter.h"

static constexpr size_t SIGNAL_SAFE_CONSOLE_BUFFER_SIZE = 512;

// Value type describing where flushed lines go and whether the writer appends a
// '\n' before handing the line to the callback. Sinks that provide their own
// line discipline (e.g. Android logcat) set appendNewline to false;
// newline-delimited sinks (stderr, caller-supplied callbacks) set it to true.
// Copyable so it can be assigned into the writer's internal sink.
class SignalSafeConsoleOutputSink
{
public:
    // Sink for a single flushed line. Receives the null-terminated line buffer
    // and its byte size (excluding the terminator). Returns false on write failure.
    // Shares the bool-returning shape of SignalSafeJsonOutputSink::Callback so a
    // single caller-supplied callback type can drive either writer. Must be
    // async-signal-safe.
    using Callback = bool(*)(const char* buffer, size_t len, void* context);

    SignalSafeConsoleOutputSink(Callback callback, void* context, bool appendNewline)
        : m_callback(callback)
        , m_context(context)
        , m_appendNewline(appendNewline)
    {
    }

    SignalSafeConsoleOutputSink(const SignalSafeConsoleOutputSink&) = default;
    SignalSafeConsoleOutputSink& operator=(const SignalSafeConsoleOutputSink&) = default;

    bool AppendNewline() const { return m_appendNewline; }

    // A null callback drops output and reports success.
    bool Write(const char* buffer, size_t len) const
    {
        return m_callback == nullptr || m_callback(buffer, len, m_context);
    }

private:
    Callback m_callback;
    void* m_context;
    bool m_appendNewline;
};

class SignalSafeConsoleWriter
{
public:
    // Default-constructed writers emit to the platform console (Android logcat
    // under CRASHREPORT_LOG_TAG, stderr on Apple mobile platforms).
    SignalSafeConsoleWriter()
        : m_pos(0)
        , m_sink(PlatformConsoleOutputSink())
    {
        m_buffer[0] = '\0';
    }

    // Routes output to a caller-supplied sink.
    explicit SignalSafeConsoleWriter(const SignalSafeConsoleOutputSink& sink)
        : m_pos(0)
        , m_sink(sink)
    {
        m_buffer[0] = '\0';
    }

    SignalSafeConsoleWriter(const SignalSafeConsoleWriter&) = delete;
    SignalSafeConsoleWriter& operator=(const SignalSafeConsoleWriter&) = delete;

    // Re-points the sink for reuse across reports and drops any partially
    // buffered line. Pass PlatformConsoleOutputSink() to emit to the platform
    // console or DropAllOutputSink() to suppress output.
    void SetOutputSink(const SignalSafeConsoleOutputSink& sink);

    // The default platform console sink (Android logcat / stderr).
    static const SignalSafeConsoleOutputSink& PlatformConsoleOutputSink();

    // A drop-all sink that discards output and reports success.
    static const SignalSafeConsoleOutputSink& DropAllOutputSink();

    void AppendStr(const char* s);
    void AppendChar(char c);
    void AppendHex(uint64_t v);
    void AppendDecimal(uint64_t v);
    void AppendSignedDecimal(int64_t v);
    void EndLine();

    // Convenience for the many fixed strings emitted during the report.
    void WriteLine(const char* s);
    // "key: value" line shortcut (no string-escaping; values are trusted CLR strings).
    void WriteKeyValueStr(const char* key, const char* value);
    void WriteKeyValueDecimal(const char* key, uint64_t value);

    void WriteSeparator();
    void WriteBlank() { WriteLine(""); }

private:
    void Flush();

    SignalSafeFormatter m_formatter;
    char m_buffer[SIGNAL_SAFE_CONSOLE_BUFFER_SIZE];
    size_t m_pos;
    SignalSafeConsoleOutputSink m_sink;
};
