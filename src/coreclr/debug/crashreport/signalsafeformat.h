// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Async-signal-safe integer-to-string format primitives shared across the
// signal-safe writer family (SignalSafeJsonWriter, SignalSafeConsoleWriter,
// and any other consumer that needs to render integers without stdio,
// locale, or heap allocation). Bounded buffer-size constants document the
// minimum buffer required for each formatter.

#pragma once

#include <stddef.h>
#include <stdint.h>

namespace SignalSafeFormat
{
    constexpr size_t MAX_HEX_DIGITS_UINT64 = 16;
    constexpr size_t MAX_DECIMAL_DIGITS_UINT64 = 20;
    constexpr size_t HEX_PREFIX_LEN = 2;  // "0x"
    constexpr size_t SIGN_LEN = 1;        // '-' for signed decimals
    constexpr size_t NULL_TERMINATOR_LEN = 1;

    constexpr size_t MAX_HEX_BUFFER_SIZE = HEX_PREFIX_LEN + MAX_HEX_DIGITS_UINT64 + NULL_TERMINATOR_LEN;
    constexpr size_t MAX_UNSIGNED_DECIMAL_BUFFER_SIZE = MAX_DECIMAL_DIGITS_UINT64 + NULL_TERMINATOR_LEN;
    constexpr size_t MAX_SIGNED_DECIMAL_BUFFER_SIZE = SIGN_LEN + MAX_DECIMAL_DIGITS_UINT64 + NULL_TERMINATOR_LEN;

    // Writes "0x"-prefixed hex (lowercase) of `value` into `buffer`. On
    // success the buffer is null-terminated. If `buffer` is null, `bufferSize`
    // is zero, or the buffer is too small to hold the formatted value, the
    // buffer is left empty (or null-terminated at index 0 when possible).
    void FormatHex(char* buffer, size_t bufferSize, uint64_t value);

    // Writes the unsigned-decimal representation of `value` into `buffer` and
    // returns the number of bytes written (excluding the null terminator).
    // Returns 0 on failure with the same buffer-state guarantees as FormatHex.
    size_t FormatUnsignedDecimal(char* buffer, size_t bufferSize, uint64_t value);

    // Writes the signed-decimal representation of `value` into `buffer` and
    // returns the number of bytes written (excluding the null terminator).
    // Returns 0 on failure. Handles INT64_MIN without signed overflow.
    size_t FormatSignedDecimal(char* buffer, size_t bufferSize, int64_t value);
}
