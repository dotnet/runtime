// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Shared, allocation-free, bounds-safe string helpers used by both the in-proc
// crash reporter and the crash report lifecycle. These are safe to call from the
// signal/crash path: they perform no heap allocation and never call into the
// runtime.

#pragma once

#include <stddef.h>

namespace CrashReportStringUtils
{
    // Copies value into buffer, truncating if necessary, and always
    // null-terminates. A null value yields an empty string. No-op if buffer is
    // null or bufferSize is 0.
    void CopyString(
        char* buffer,
        size_t bufferSize,
        const char* value);

    // Appends value to buffer starting at *pos, copying as much as fits, advancing
    // *pos past the characters actually written, and always null-terminating.
    // Returns true if the entire value fit; returns false if the value was
    // truncated, the arguments are invalid, or bufferSize is 0.
    bool AppendString(
        char* buffer,
        size_t bufferSize,
        size_t* pos,
        const char* value);
}
