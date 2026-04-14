// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// In-proc crash report generation.
//
// Emits a minimal createdump-shaped JSON payload to logcat / stderr.

#pragma once

#include <signal.h>

// Generate an in-proc crash report. Called from PROCCreateCrashDumpIfEnabled.
// All arguments come from the signal handler and are signal-safe to read.
void InProcCrashReportGenerate(int signal, siginfo_t* siginfo, void* context);
