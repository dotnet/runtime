// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Bounded JSON writer for crash reports.
// Streams content through a small fixed-size buffer using bounded low-level
// string and memory operations so file output does not require materializing
// the whole report at once.

#pragma once

#include <stddef.h>

typedef int (*CrashJsonOutputCallback)(const char* buffer, int len, void* ctx);

// Small streaming buffer used when serializing the crash report JSON.
#define CRASH_JSON_BUFFER_SIZE (4 * 1024)

struct CrashJsonWriter
{
    char buffer[CRASH_JSON_BUFFER_SIZE];
    int pos;
    bool commaNeeded;
    bool writeFailed;
    CrashJsonOutputCallback outputCallback;
    void* outputContext;
};

void CrashJsonInit(CrashJsonWriter* w, CrashJsonOutputCallback outputCallback, void* outputContext);
void CrashJsonOpenObject(CrashJsonWriter* w, const char* key);
void CrashJsonCloseObject(CrashJsonWriter* w);
void CrashJsonOpenArray(CrashJsonWriter* w, const char* key);
void CrashJsonCloseArray(CrashJsonWriter* w);
void CrashJsonWriteString(CrashJsonWriter* w, const char* key, const char* value);
void CrashJsonFinish(CrashJsonWriter* w);
int CrashJsonHasFailed(CrashJsonWriter* w);
