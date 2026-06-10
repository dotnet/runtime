// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// POSIX watchdog for in-proc crash reporting.

#pragma once

#include "pal.h"
#include "volatile.h"

#include <pthread.h>
#include <stdint.h>
#include <signal.h>

class CrashReportWatchdogScope;

class CrashReportWatchdog
{
public:
    // Attempts to initialize the watchdog during normal runtime startup. This is
    // not async-signal-safe and must not be called from the crash-reporting path.
    static bool TryInitialize(int timeoutSeconds);

    static CrashReportWatchdog* GetInstance()
    {
        return VolatileLoad(&s_instance);
    }

    void StartCrashReport();
    void StopCrashReport();

private:
    static constexpr int CRASH_REPORT_WATCHDOG_INFINITE_TIMEOUT_MS = -1;

    enum class Command : char
    {
        Started = 'S',
        Finished = 'F',
    };

    explicit CrashReportWatchdog(int timeoutSeconds);

    bool Initialize();
    bool InitializePipe();
    bool ConfigurePipeFd(int fd);
    void ClosePipe();

    void BuildFatalSignalSet(sigset_t* signalSet);
    static void* WatchdogThreadProc(void* context);

    void ThreadLoop();
    bool WaitForCommand(Command expectedCommand, int timeoutMs = CRASH_REPORT_WATCHDOG_INFINITE_TIMEOUT_MS);
    void WriteCommand(Command command);
    int GetRemainingTimeoutMs(int64_t deadlineMs);
    void Abort();

    int m_timeoutSeconds;
    int m_timeoutMs;
    int m_pipe[2];

    static pthread_mutex_t s_initializationMutex;
    static CrashReportWatchdog* s_instance;
};

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
