// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Async-signal-safe format primitives shared across the
// signal-safe writer family (SignalSafeJsonWriter, SignalSafeConsoleWriter,
// and any other consumer that needs to render values without stdio,
// locale, or heap allocation). Bounded buffer-size constants document the
// minimum buffer required for each formatter.

#pragma once

#include <stddef.h>
#include <stdint.h>

#include <minipal/guid.h>

class SignalSafeFormatter
{
public:
    static constexpr size_t MAX_HEX_DIGITS_UINT64 = 16;
    static constexpr size_t MAX_DECIMAL_DIGITS_UINT64 = 20;

    static constexpr size_t MAX_HEX_BUFFER_SIZE = 2 + MAX_HEX_DIGITS_UINT64 + 1; // "0x" + hex + '\0'
    static constexpr size_t MAX_UNSIGNED_DECIMAL_BUFFER_SIZE = MAX_DECIMAL_DIGITS_UINT64 + 1; // digits + '\0'
    static constexpr size_t MAX_SIGNED_DECIMAL_BUFFER_SIZE = 1 + MAX_DECIMAL_DIGITS_UINT64 + 1; // '-' + digits + '\0'
    static constexpr size_t MAX_GUID_BUFFER_SIZE = MINIPAL_GUID_BUFFER_LEN;

    SignalSafeFormatter() = default;
    SignalSafeFormatter(const SignalSafeFormatter&) = delete;
    SignalSafeFormatter& operator=(const SignalSafeFormatter&) = delete;

    const char* FormatHex(uint64_t value);
    const char* FormatUnsignedDecimal(uint64_t value);
    const char* FormatSignedDecimal(int64_t value);
    const char* FormatGuid(const GUID& guid);

private:
    // Writes "0x"-prefixed hex (lowercase) of `value` into `buffer`. On
    // success the buffer is null-terminated. If `buffer` is null, `bufferSize`
    // is zero, or the buffer is too small to hold the formatted value, the
    // buffer is left empty (or null-terminated at index 0 when possible).
    void FormatHex(char* buffer, size_t bufferSize, uint64_t value);

    // Writes the unsigned-decimal representation of `value` into `buffer` and
    // returns the number of bytes written (excluding the null terminator).
    // Returns 0 on failure with the buffer null-terminated at index 0 when possible.
    size_t FormatUnsignedDecimal(char* buffer, size_t bufferSize, uint64_t value);

    // Writes the signed-decimal representation of `value` into `buffer` and
    // returns the number of bytes written (excluding the null terminator).
    // Returns 0 on failure. Handles INT64_MIN without signed overflow.
    size_t FormatSignedDecimal(char* buffer, size_t bufferSize, int64_t value);

    void FormatGuid(char* buffer, size_t bufferSize, const GUID& guid);
    static char GetHexDigit(uint32_t value);
    static void AppendFixedHex(char* buffer, size_t* pos, uint32_t value, uint32_t digits);

    char m_hexBuffer[MAX_HEX_BUFFER_SIZE];
    char m_unsignedDecimalBuffer[MAX_UNSIGNED_DECIMAL_BUFFER_SIZE];
    char m_signedDecimalBuffer[MAX_SIGNED_DECIMAL_BUFFER_SIZE];
    char m_guidBuffer[MAX_GUID_BUFFER_SIZE];
    char m_reverse[MAX_DECIMAL_DIGITS_UINT64];
};
