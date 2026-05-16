// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// POSIX watchdog for in-proc crash reporting.

#pragma once

#include <stdint.h>

// Attempts to initialize the watchdog during normal runtime startup. This is
// not async-signal-safe and must not be called from the crash-reporting path.
bool CrashReportWatchdogTryInitialize(uint32_t timeoutSeconds);

class CrashReportWatchdogScope
{
public:
    // The constructor and destructor run in the crash-reporting path. Keep them
    // async-signal-safe: they may only notify the pre-created watchdog channel.
    CrashReportWatchdogScope();
    ~CrashReportWatchdogScope();

    CrashReportWatchdogScope(const CrashReportWatchdogScope&) = delete;
    CrashReportWatchdogScope& operator=(const CrashReportWatchdogScope&) = delete;
};
