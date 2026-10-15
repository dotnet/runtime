// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Captures the in-proc crash reporter's compact console report in-process.
//
// On Android the reporter emits each console line via __android_log_write under
// CRASHREPORT_LOG_TAG ("DOTNET_CRASH"), routed to logcat rather than a file
// descriptor -- so it cannot be captured by redirecting stdout/stderr. This
// translation unit is linked into the same shared library (libmonodroid.so) as
// the PAL-linked reporter, with --wrap=__android_log_write redirecting its calls
// to this wrapper. Lines tagged DOTNET_CRASH are accumulated for later
// validation; every call is also forwarded to the real liblog implementation so
// the report still appears in logcat for debugging.

#include "android_log_interpose.h"

#include <android/log.h>
#include <stdbool.h>
#include <stddef.h>
#include <string.h>

// Must match CRASHREPORT_LOG_TAG in inproccrashreporter.h.
static const char s_crashTag[] = "DOTNET_CRASH";

// The fixed scenarios currently produce at most 1 KiB. Keep ample headroom
// without allocating from the signal-shaped reporting path.
static char s_capture[16 * 1024];
static size_t s_captureLen;
static bool s_overflowed;

extern int __real___android_log_write(int prio, const char* tag, const char* text);

int __wrap___android_log_write(int prio, const char* tag, const char* text)
{
    if (tag != NULL && text != NULL && strcmp(tag, s_crashTag) == 0)
    {
        size_t textLen = strlen(text);
        if (textLen < sizeof(s_capture) - s_captureLen - 1)
        {
            memcpy(s_capture + s_captureLen, text, textLen);
            s_captureLen += textLen;
            s_capture[s_captureLen++] = '\n';
            s_capture[s_captureLen] = '\0';
        }
        else
        {
            s_overflowed = true;
        }
    }

    return __real___android_log_write(prio, tag, text);
}

const char* InProcCrashReportTest_GetConsoleCapture(void)
{
    return s_overflowed ? NULL : s_capture;
}

void InProcCrashReportTest_ResetConsoleCapture(void)
{
    s_captureLen = 0;
    s_capture[0] = '\0';
    s_overflowed = false;
}
