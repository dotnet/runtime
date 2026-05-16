// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// POSIX watchdog for in-proc crash reporting.

#include "inproccrashreportwatchdog.h"

#include "pal.h"

#include <errno.h>
#include <fcntl.h>
#include <limits>
#include <poll.h>
#include <pthread.h>
#include <signal.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>
#include <unistd.h>
#include <minipal/thread.h>

// Single-byte pipe protocol: the crash-reporting thread writes Started when
// CreateReport begins and Finished when it leaves.
static constexpr char CrashReportWatchdogStartedCommand = 'S';
static constexpr char CrashReportWatchdogFinishedCommand = 'F';
static constexpr long CrashReportWatchdogNanosecondsPerMillisecond = 1000000;
static constexpr long CrashReportWatchdogNanosecondsPerSecond = 1000000000;
static constexpr int CrashReportWatchdogMillisecondsPerSecond = 1000;
static constexpr int CrashReportWatchdogSignalExitCodeOffset = 128;
static constexpr clockid_t CrashReportWatchdogClock = CLOCK_MONOTONIC;

// Process-lifetime watchdog state. Successful initialization intentionally keeps
// the detached thread and pipe open until process exit; failed initialization
// paths close the pipe before returning.
static pthread_t s_crashReportWatchdogThread;
static uint32_t s_crashReportTimeoutSeconds;
static LONG s_crashReportWatchdogInitializationStarted;
static int s_crashReportWatchdogPipe[2] = { -1, -1 };
// Crash-path publication point. The signal handler reads only this fd, so it
// cannot observe an enabled flag while still seeing a stale pipe descriptor.
static volatile sig_atomic_t s_crashReportWatchdogWriteFd = -1;

// Signal and timeout helpers.

static void
CrashReportWatchdogBuildFatalSignalSet(sigset_t* signalSet)
{
    sigemptyset(signalSet);
    sigaddset(signalSet, SIGABRT);
    sigaddset(signalSet, SIGBUS);
    sigaddset(signalSet, SIGFPE);
    sigaddset(signalSet, SIGILL);
    sigaddset(signalSet, SIGSEGV);
    sigaddset(signalSet, SIGTRAP);
}

static void
CrashReportWatchdogAbort()
{
    struct sigaction action;
    memset(&action, 0, sizeof(action));
    action.sa_handler = SIG_DFL;
    sigemptyset(&action.sa_mask);
    (void)sigaction(SIGABRT, &action, nullptr);

    sigset_t signalSet;
    sigemptyset(&signalSet);
    sigaddset(&signalSet, SIGABRT);
    (void)pthread_sigmask(SIG_UNBLOCK, &signalSet, nullptr);

    abort();
    _exit(CrashReportWatchdogSignalExitCodeOffset + SIGABRT);
}

static bool
CrashReportWatchdogBuildDeadline(struct timespec* deadline)
{
    if (clock_gettime(CrashReportWatchdogClock, deadline) != 0)
    {
        return false;
    }

    const time_t maxTime = std::numeric_limits<time_t>::max();
    if (static_cast<unsigned long long>(s_crashReportTimeoutSeconds) > static_cast<unsigned long long>(maxTime))
    {
        return false;
    }

    time_t timeoutSeconds = static_cast<time_t>(s_crashReportTimeoutSeconds);
    if (deadline->tv_sec > maxTime - timeoutSeconds)
    {
        return false;
    }

    deadline->tv_sec += timeoutSeconds;
    return true;
}

// Pipe channel helpers.

static void
CrashReportWatchdogClosePipe()
{
    if (s_crashReportWatchdogPipe[0] != -1)
    {
        close(s_crashReportWatchdogPipe[0]);
        s_crashReportWatchdogPipe[0] = -1;
    }

    if (s_crashReportWatchdogPipe[1] != -1)
    {
        close(s_crashReportWatchdogPipe[1]);
        s_crashReportWatchdogPipe[1] = -1;
    }
}

static bool
CrashReportWatchdogConfigurePipeFd(int fd)
{
    int descriptorFlags = fcntl(fd, F_GETFD);
    if (descriptorFlags == -1 || fcntl(fd, F_SETFD, descriptorFlags | FD_CLOEXEC) != 0)
    {
        return false;
    }

    int statusFlags = fcntl(fd, F_GETFL);
    return statusFlags != -1 && fcntl(fd, F_SETFL, statusFlags | O_NONBLOCK) == 0;
}

static bool
CrashReportWatchdogInitializePipe()
{
    if (pipe(s_crashReportWatchdogPipe) != 0)
    {
        return false;
    }

    if (!CrashReportWatchdogConfigurePipeFd(s_crashReportWatchdogPipe[0]) ||
        !CrashReportWatchdogConfigurePipeFd(s_crashReportWatchdogPipe[1]))
    {
        CrashReportWatchdogClosePipe();
        return false;
    }

    return true;
}

// Watchdog thread wait loop.

static int
CrashReportWatchdogGetRemainingMilliseconds(const struct timespec* deadline)
{
    struct timespec now;
    if (clock_gettime(CrashReportWatchdogClock, &now) != 0)
    {
        return 0;
    }

    if (now.tv_sec > deadline->tv_sec ||
        (now.tv_sec == deadline->tv_sec && now.tv_nsec >= deadline->tv_nsec))
    {
        return 0;
    }

    time_t remainingSeconds = deadline->tv_sec - now.tv_sec;
    long remainingNanoseconds = deadline->tv_nsec - now.tv_nsec;
    if (remainingNanoseconds < 0)
    {
        remainingSeconds--;
        remainingNanoseconds += CrashReportWatchdogNanosecondsPerSecond;
    }

    const int maxMilliseconds = std::numeric_limits<int>::max();
    if (static_cast<unsigned long long>(remainingSeconds) >
        static_cast<unsigned long long>(maxMilliseconds / CrashReportWatchdogMillisecondsPerSecond))
    {
        return maxMilliseconds;
    }

    unsigned long long remainingMilliseconds =
        static_cast<unsigned long long>(remainingSeconds) * CrashReportWatchdogMillisecondsPerSecond +
        static_cast<unsigned long long>(
            (remainingNanoseconds + CrashReportWatchdogNanosecondsPerMillisecond - 1) /
            CrashReportWatchdogNanosecondsPerMillisecond);

    return remainingMilliseconds > static_cast<unsigned long long>(maxMilliseconds)
        ? maxMilliseconds
        : static_cast<int>(remainingMilliseconds);
}

static bool
CrashReportWatchdogWaitForCommand(char expectedCommand, const struct timespec* deadline)
{
    while (true)
    {
        int timeoutMilliseconds = -1;
        if (deadline != nullptr)
        {
            timeoutMilliseconds = CrashReportWatchdogGetRemainingMilliseconds(deadline);
            if (timeoutMilliseconds == 0)
            {
                return false;
            }
        }

        struct pollfd pollFd;
        pollFd.fd = s_crashReportWatchdogPipe[0];
        pollFd.events = POLLIN;
        pollFd.revents = 0;

        int pollResult = poll(&pollFd, 1, timeoutMilliseconds);
        if (pollResult == -1)
        {
            if (errno == EINTR)
            {
                continue;
            }

            return false;
        }

        if (pollResult == 0 || (pollFd.revents & POLLIN) == 0)
        {
            return false;
        }

        char command;
        ssize_t readResult = read(s_crashReportWatchdogPipe[0], &command, sizeof(command));
        if (readResult == sizeof(command))
        {
            if (command == expectedCommand)
            {
                return true;
            }

            continue;
        }

        if (readResult == -1 && (errno == EINTR || errno == EAGAIN || errno == EWOULDBLOCK))
        {
            continue;
        }

        return false;
    }
}

static void*
CrashReportWatchdogThread(void*)
{
    sigset_t signalSet;
    CrashReportWatchdogBuildFatalSignalSet(&signalSet);
    (void)pthread_sigmask(SIG_BLOCK, &signalSet, nullptr);

    // Keep within minipal's portable 15-character limit to avoid truncation.
    (void)minipal_set_thread_name(pthread_self(), ".NET CrashWdg");

    while (true)
    {
        if (!CrashReportWatchdogWaitForCommand(CrashReportWatchdogStartedCommand, nullptr))
        {
            continue;
        }

        struct timespec deadline;
        if (!CrashReportWatchdogBuildDeadline(&deadline))
        {
            CrashReportWatchdogAbort();
        }

        if (!CrashReportWatchdogWaitForCommand(CrashReportWatchdogFinishedCommand, &deadline))
        {
            CrashReportWatchdogAbort();
        }
    }

    return nullptr;
}

// Watchdog entry points.

bool
CrashReportWatchdogTryInitialize(uint32_t timeoutSeconds)
{
    if (timeoutSeconds == 0)
    {
        return false;
    }

    if (static_cast<unsigned long long>(timeoutSeconds) > static_cast<unsigned long long>(std::numeric_limits<time_t>::max()))
    {
        return false;
    }

    if (InterlockedCompareExchange(&s_crashReportWatchdogInitializationStarted, 1, 0) != 0)
    {
        return s_crashReportWatchdogWriteFd != -1;
    }

    // The watchdog is best-effort. The one-time flag prevents duplicate
    // watchdog threads, but setup failures reset it so a later init can retry.
    if (!CrashReportWatchdogInitializePipe())
    {
        InterlockedExchange(&s_crashReportWatchdogInitializationStarted, 0);
        return false;
    }

    s_crashReportTimeoutSeconds = timeoutSeconds;

    // Block fatal signals before pthread_create so the watchdog inherits the
    // mask; restore this thread's mask immediately after creation. This keeps
    // process-directed fatal signals from landing on the watchdog thread.
    sigset_t signalSet;
    sigset_t previousSignalSet;
    CrashReportWatchdogBuildFatalSignalSet(&signalSet);
    int maskResult = pthread_sigmask(SIG_BLOCK, &signalSet, &previousSignalSet);
    if (maskResult != 0)
    {
        CrashReportWatchdogClosePipe();
        InterlockedExchange(&s_crashReportWatchdogInitializationStarted, 0);
        return false;
    }

    if (pthread_create(&s_crashReportWatchdogThread, nullptr, CrashReportWatchdogThread, nullptr) != 0)
    {
        (void)pthread_sigmask(SIG_SETMASK, &previousSignalSet, nullptr);
        CrashReportWatchdogClosePipe();
        InterlockedExchange(&s_crashReportWatchdogInitializationStarted, 0);
        return false;
    }

    (void)pthread_sigmask(SIG_SETMASK, &previousSignalSet, nullptr);

    (void)pthread_detach(s_crashReportWatchdogThread);
    s_crashReportWatchdogWriteFd = s_crashReportWatchdogPipe[1];
    return true;
}

CrashReportWatchdogScope::CrashReportWatchdogScope()
{
    // This runs from the crash-reporting path. Keep this and any future callees
    // async-signal-safe.
    sig_atomic_t writeFd = s_crashReportWatchdogWriteFd;
    if (writeFd != -1)
    {
        char command = CrashReportWatchdogStartedCommand;
        (void)write(static_cast<int>(writeFd), &command, sizeof(command));
    }
}

CrashReportWatchdogScope::~CrashReportWatchdogScope()
{
    // This runs from the crash-reporting path. Keep this and any future callees
    // async-signal-safe.
    sig_atomic_t writeFd = s_crashReportWatchdogWriteFd;
    if (writeFd != -1)
    {
        char command = CrashReportWatchdogFinishedCommand;
        (void)write(static_cast<int>(writeFd), &command, sizeof(command));
    }
}
