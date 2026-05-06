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

using SignalSafeJsonOutputCallback = bool (*)(const char* buffer, size_t len, void* ctx);

static constexpr size_t SIGNAL_SAFE_JSON_BUFFER_SIZE = 4 * 1024;

class SignalSafeJsonWriter
{
public:
    // Maximum digit counts and required buffer sizes for the static format helpers below.
    static constexpr size_t MAX_HEX_DIGITS_UINT64 = 16;
    static constexpr size_t MAX_DECIMAL_DIGITS_UINT64 = 20;
    static constexpr size_t HEX_PREFIX_LEN = 2;  // "0x"
    static constexpr size_t SIGN_LEN = 1;        // '-' for signed decimals
    static constexpr size_t NULL_TERMINATOR_LEN = 1;
    static constexpr size_t MAX_HEX_FORMAT_BUFFER_SIZE = HEX_PREFIX_LEN + MAX_HEX_DIGITS_UINT64 + NULL_TERMINATOR_LEN;
    static constexpr size_t MAX_UNSIGNED_DECIMAL_BUFFER_SIZE = MAX_DECIMAL_DIGITS_UINT64 + NULL_TERMINATOR_LEN;
    static constexpr size_t MAX_SIGNED_DECIMAL_BUFFER_SIZE = SIGN_LEN + MAX_DECIMAL_DIGITS_UINT64 + NULL_TERMINATOR_LEN;

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

    // Async-signal-safe integer-to-string formatters used by the Write* members
    // above and by the few non-writer call sites that need the raw text (e.g.
    // dump-name pattern expansion). All are bounded and never allocate.
    static void FormatHexValue(char* buffer, size_t bufferSize, uint64_t value);
    static size_t FormatUnsignedDecimal(char* buffer, size_t bufferSize, uint64_t value);
    static size_t FormatSignedDecimal(char* buffer, size_t bufferSize, int64_t value);

private:
    bool Append(const char* str, size_t len);
    bool AppendChar(char c);
    bool AppendStr(const char* str);
    void WriteSeparator();
    void WriteEscapedString(const char* str);

    char m_buffer[SIGNAL_SAFE_JSON_BUFFER_SIZE];
    size_t m_pos;
    bool m_commaNeeded;
    bool m_writeFailed;
    SignalSafeJsonOutputCallback m_outputCallback;
    void* m_outputContext;
};
