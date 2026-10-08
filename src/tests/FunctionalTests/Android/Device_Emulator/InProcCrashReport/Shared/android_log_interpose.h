// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Shared declarations for the synthetic in-proc crash reporter test's console
// capture. The compact console report is written by the reporter via
// __android_log_write (Android logcat); android_log_interpose.c intercepts those
// writes so the report can be validated in-process alongside the JSON file.

#ifndef ANDROID_LOG_INTERPOSE_H
#define ANDROID_LOG_INTERPOSE_H

#ifdef __cplusplus
extern "C"
{
#endif

int __wrap___android_log_write(int prio, const char* tag, const char* text);

// Returns captured lines since the last reset, or NULL if the buffer overflowed.
const char* InProcCrashReportTest_GetConsoleCapture(void);

// Clears the captured console-report text.
void InProcCrashReportTest_ResetConsoleCapture(void);

#ifdef __cplusplus
}
#endif

#endif // ANDROID_LOG_INTERPOSE_H
