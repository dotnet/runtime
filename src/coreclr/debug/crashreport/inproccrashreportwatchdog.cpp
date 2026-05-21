// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// POSIX watchdog for in-proc crash reporting.

#include "inproccrashreportwatchdog.h"

#include <errno.h>
#include <fcntl.h>
#include <limits>
#include <new>
#include <poll.h>
#include <pthread.h>
#include <signal.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <minipal/log.h>
#include <minipal/time.h>
#include <minipal/thread.h>

static constexpr uint32_t CRASH_REPORT_WATCHDOG_SECONDS_TO_MILLISECONDS = 1000;
static constexpr int CRASH_REPORT_WATCHDOG_SIGNAL_EXIT_CODE_OFFSET = 128;

LONG CrashReportWatchdog::s_initializationStarted;
CrashReportWatchdog* CrashReportWatchdog::s_instance;
volatile sig_atomic_t CrashReportWatchdog::s_writeFd = -1;

uint32_t
CrashReportWatchdog::ClampTimeoutSeconds(uint32_t timeoutSeconds)
{
    uint32_t maxTimeoutSeconds = static_cast<uint32_t>(std::numeric_limits<int>::max() / CRASH_REPORT_WATCHDOG_SECONDS_TO_MILLISECONDS);
    if (timeoutSeconds > maxTimeoutSeconds)
    {
        return maxTimeoutSeconds;
    }

    return timeoutSeconds;
}

CrashReportWatchdog::CrashReportWatchdog(uint32_t timeoutSeconds)
    : m_timeoutSeconds(ClampTimeoutSeconds(timeoutSeconds)),
      m_timeoutMs(static_cast<int>(m_timeoutSeconds * CRASH_REPORT_WATCHDOG_SECONDS_TO_MILLISECONDS)),
      m_thread()
{
    m_pipe[0] = -1;
    m_pipe[1] = -1;
}

CrashReportWatchdog::~CrashReportWatchdog()
{
    ClosePipe();
}

bool
CrashReportWatchdog::TryInitialize(uint32_t timeoutSeconds)
{
    if (timeoutSeconds == 0)
    {
        return false;
    }

    if (InterlockedCompareExchange(&s_initializationStarted, 1, 0) != 0)
    {
        return s_writeFd != -1;
    }

    // The watchdog is best-effort. The one-time flag prevents duplicate
    // watchdog threads, but setup failures reset it so a later init can retry.
    CrashReportWatchdog* watchdog = new (std::nothrow) CrashReportWatchdog(timeoutSeconds);
    if (watchdog == nullptr)
    {
        InterlockedExchange(&s_initializationStarted, 0);
        return false;
    }

    if (!watchdog->Initialize())
    {
        delete watchdog;
        InterlockedExchange(&s_initializationStarted, 0);
        return false;
    }

    // Keep the watchdog alive for process lifetime; the detached thread and
    // pipe remain available after initialization succeeds.
    s_instance = watchdog;
    s_writeFd = watchdog->m_pipe[1];
    return true;
}

bool
CrashReportWatchdog::Initialize()
{
    if (!InitializePipe())
    {
        return false;
    }

    // Block fatal signals before pthread_create so the watchdog inherits the
    // mask; restore this thread's mask immediately after creation. This keeps
    // process-directed fatal signals from landing on the watchdog thread.
    sigset_t signalSet;
    sigset_t previousSignalSet;
    BuildFatalSignalSet(&signalSet);
    int maskResult = pthread_sigmask(SIG_BLOCK, &signalSet, &previousSignalSet);
    if (maskResult != 0)
    {
        ClosePipe();
        return false;
    }

    if (pthread_create(&m_thread, nullptr, ThreadEntry, this) != 0)
    {
        (void)pthread_sigmask(SIG_SETMASK, &previousSignalSet, nullptr);
        ClosePipe();
        return false;
    }

    (void)pthread_sigmask(SIG_SETMASK, &previousSignalSet, nullptr);

    (void)pthread_detach(m_thread);
    return true;
}

bool
CrashReportWatchdog::InitializePipe()
{
    if (pipe(m_pipe) != 0)
    {
        return false;
    }

    if (!ConfigurePipeFd(m_pipe[0]) ||
        !ConfigurePipeFd(m_pipe[1]))
    {
        ClosePipe();
        return false;
    }

    return true;
}

bool
CrashReportWatchdog::ConfigurePipeFd(int fd)
{
#ifdef FD_CLOEXEC
    int descriptorFlags = fcntl(fd, F_GETFD);
    if (descriptorFlags == -1 || fcntl(fd, F_SETFD, descriptorFlags | FD_CLOEXEC) != 0)
    {
        return false;
    }
#endif

    int statusFlags = fcntl(fd, F_GETFL);
    return statusFlags != -1 && fcntl(fd, F_SETFL, statusFlags | O_NONBLOCK) == 0;
}

void
CrashReportWatchdog::ClosePipe()
{
    if (m_pipe[0] != -1)
    {
        close(m_pipe[0]);
        m_pipe[0] = -1;
    }

    if (m_pipe[1] != -1)
    {
        close(m_pipe[1]);
        m_pipe[1] = -1;
    }
}

void
CrashReportWatchdog::BuildFatalSignalSet(sigset_t* signalSet)
{
    sigemptyset(signalSet);
    sigaddset(signalSet, SIGABRT);
    sigaddset(signalSet, SIGBUS);
    sigaddset(signalSet, SIGFPE);
    sigaddset(signalSet, SIGILL);
    sigaddset(signalSet, SIGSEGV);
    sigaddset(signalSet, SIGTRAP);
}

void*
CrashReportWatchdog::ThreadEntry(void* context)
{
    CrashReportWatchdog* watchdog = static_cast<CrashReportWatchdog*>(context);
    if (watchdog != nullptr)
    {
        watchdog->ThreadLoop();
    }

    return nullptr;
}

void
CrashReportWatchdog::ThreadLoop()
{
    sigset_t signalSet;
    BuildFatalSignalSet(&signalSet);
    (void)pthread_sigmask(SIG_BLOCK, &signalSet, nullptr);

    // Keep within minipal's portable 15-character limit to avoid truncation.
    (void)minipal_set_thread_name(pthread_self(), ".NET CrashWdg");

    if (!WaitForCommand(Command::Started))
    {
        // The watchdog is best-effort: if the notification pipe is broken, the
        // watchdog can no longer observe crash-report progress. Retrying would
        // spin forever, so leave termination to the platform's normal handling.
        minipal_log_write_error(
            "In-proc crash report watchdog failed while waiting for a start notification; exiting watchdog thread.\n");
        return;
    }

    minipal_log_print_info(
        "In-proc crash report watchdog started monitoring with a %lu second timeout.\n",
        static_cast<unsigned long>(m_timeoutSeconds));

    if (!WaitForCommand(Command::Finished, m_timeoutMs))
    {
        minipal_log_write_error(
            "In-proc crash report watchdog did not receive a finish notification before the timeout; aborting process.\n");
        Abort();
    }
}

bool
CrashReportWatchdog::WaitForCommand(Command expectedCommand, int timeoutMs)
{
    int readFd = m_pipe[0];
    if (readFd == -1)
    {
        return false;
    }

    int64_t deadlineMs = 0;
    if (timeoutMs != CRASH_REPORT_WATCHDOG_INFINITE_TIMEOUT_MS)
    {
        deadlineMs = minipal_lowres_ticks() + timeoutMs;
    }

    while (true)
    {
        int currentTimeoutMs = timeoutMs;
        if (timeoutMs != CRASH_REPORT_WATCHDOG_INFINITE_TIMEOUT_MS)
        {
            currentTimeoutMs = GetRemainingTimeoutMs(deadlineMs);
            if (currentTimeoutMs == 0)
            {
                return false;
            }
        }

        struct pollfd pollFd;
        pollFd.fd = readFd;
        pollFd.events = POLLIN;
        pollFd.revents = 0;

        int pollResult = poll(&pollFd, 1, currentTimeoutMs);
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
        ssize_t readResult = read(readFd, &command, sizeof(command));
        if (readResult == sizeof(command))
        {
            if (command == static_cast<char>(expectedCommand))
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

// Called from CrashReportWatchdogScope on the crash-reporting path. Keep this
// async-signal-safe and preserve errno so watchdog notification does not
// perturb the failing thread's crash-reporting state.
void
CrashReportWatchdog::WriteCommand(Command command)
{
    int savedErrno = errno;
    sig_atomic_t writeFd = s_writeFd;
    if (writeFd != -1)
    {
        char commandValue = static_cast<char>(command);
        while (true)
        {
            ssize_t writeResult = write(static_cast<int>(writeFd), &commandValue, sizeof(commandValue));
            if (writeResult == sizeof(commandValue))
            {
                break;
            }

            if (writeResult != -1 || errno != EINTR)
            {
                break;
            }
        }
    }

    errno = savedErrno;
}

int
CrashReportWatchdog::GetRemainingTimeoutMs(int64_t deadlineMs)
{
    int64_t remainingMs = deadlineMs - minipal_lowres_ticks();
    if (remainingMs <= 0)
    {
        return 0;
    }

    int maxTimeoutMs = std::numeric_limits<int>::max();
    if (remainingMs > maxTimeoutMs)
    {
        return maxTimeoutMs;
    }

    return static_cast<int>(remainingMs);
}

// Terminate from the watchdog thread using the default SIGABRT action. The
// watchdog only gets here after the crash reporter started but did not finish
// before its configured timeout.
void
CrashReportWatchdog::Abort()
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
    _exit(CRASH_REPORT_WATCHDOG_SIGNAL_EXIT_CODE_OFFSET + SIGABRT);
}

CrashReportWatchdogScope::CrashReportWatchdogScope()
{
    CrashReportWatchdog::WriteCommand(CrashReportWatchdog::Command::Started);
}

CrashReportWatchdogScope::~CrashReportWatchdogScope()
{
    CrashReportWatchdog::WriteCommand(CrashReportWatchdog::Command::Finished);
}
