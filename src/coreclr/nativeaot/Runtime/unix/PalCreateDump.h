// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

// Forward declare for the benefit of Wasi.  We can't use this for browser/Emscripten as this struct is not tagged in 
// Emscripten's headers.
#if defined(TARGET_WASI)
typedef struct siginfo_s siginfo_t;
#endif

#include <signal.h>

extern bool PalCreateDumpInitialize();
extern void PalCreateCrashDumpIfEnabled();
extern void PalCreateCrashDumpIfEnabled(int signal, siginfo_t* siginfo = nullptr, void* context = nullptr, void* exceptionRecord = nullptr);
extern void PalCreateCrashDumpIfEnabled(void* pExceptionRecord);
