// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Async-signal-safe JSON writer for crash reports.
// Writes to a pre-allocated fixed-size buffer using only signal-safe operations.
// No malloc, no stdio, no locks.

#pragma once

#include <stddef.h>

// Fixed buffer size for the JSON crash report.
// 32KB leaves room for multiple thread/frame entries while staying heap-free.
#define CRASH_JSON_BUFFER_SIZE (32 * 1024)  // 32KB

struct CrashJsonWriter
{
    char buffer[CRASH_JSON_BUFFER_SIZE];
    int pos;
    bool commaNeeded;

    // All methods below are async-signal-safe (no malloc, no locks)
};

void CrashJsonInit(CrashJsonWriter* w);
void CrashJsonOpenObject(CrashJsonWriter* w, const char* key);
void CrashJsonCloseObject(CrashJsonWriter* w);
void CrashJsonOpenArray(CrashJsonWriter* w, const char* key);
void CrashJsonCloseArray(CrashJsonWriter* w);
void CrashJsonWriteString(CrashJsonWriter* w, const char* key, const char* value);
int  CrashJsonGetLength(CrashJsonWriter* w);
const char* CrashJsonGetBuffer(CrashJsonWriter* w);
