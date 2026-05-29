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

#include "pal/signal.hpp"

static constexpr int CRASH_REPORT_WATCHDOG_SECONDS_TO_MILLISECONDS = 1000;
static constexpr int CRASH_REPORT_WATCHDOG_SIGNAL_EXIT_CODE_OFFSET = 128;

pthread_mutex_t CrashReportWatchdog::s_initializationMutex = PTHREAD_MUTEX_INITIALIZER;
CrashReportWatchdog* CrashReportWatchdog::s_instance;

CrashReportWatchdog::CrashReportWatchdog(int timeoutSeconds)
    : m_timeoutSeconds(timeoutSeconds),
      m_timeoutMs(static_cast<int>(m_timeoutSeconds * CRASH_REPORT_WATCHDOG_SECONDS_TO_MILLISECONDS))
{
    m_pipe[0] = -1;
    m_pipe[1] = -1;
}

bool
CrashReportWatchdog::TryInitialize(int timeoutSeconds)
{
    if (timeoutSeconds <= 0)
    {
        return false;
    }

    if (GetInstance() != nullptr)
    {
        return true;
    }

    if (pthread_mutex_lock(&s_initializationMutex) != 0)
    {
        return false;
    }

    if (GetInstance() != nullptr)
    {
        (void)pthread_mutex_unlock(&s_initializationMutex);
        return true;
    }

    CrashReportWatchdog* watchdog = new (std::nothrow) CrashReportWatchdog(timeoutSeconds);
    if (watchdog == nullptr)
    {
        (void)pthread_mutex_unlock(&s_initializationMutex);
        return false;
    }

    if (!watchdog->Initialize())
    {
        delete watchdog;
        (void)pthread_mutex_unlock(&s_initializationMutex);
        return false;
    }

    // Keep the watchdog object alive for process lifetime. The detached thread
    // exits after report completion or aborts the process on timeout; closing
    // its pipe during teardown would add a second failure mode on the crash path.
    VolatileStore(&s_instance, watchdog);
    (void)pthread_mutex_unlock(&s_initializationMutex);
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

    pthread_t thread;
    if (pthread_create(&thread, nullptr, WatchdogThreadProc, this) != 0)
    {
        (void)pthread_sigmask(SIG_SETMASK, &previousSignalSet, nullptr);
        ClosePipe();
        return false;
    }

    (void)pthread_sigmask(SIG_SETMASK, &previousSignalSet, nullptr);

    (void)pthread_detach(thread);
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
CrashReportWatchdog::WatchdogThreadProc(void* context)
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
        "In-proc crash report watchdog started monitoring with a %d second timeout.\n",
        m_timeoutSeconds);

    if (!WaitForCommand(Command::Finished, m_timeoutMs))
    {
        minipal_log_write_error(
            "In-proc crash report watchdog did not receive a finish notification before the timeout; aborting process.\n");
        Abort();
    }

    minipal_log_write_info(
        "In-proc crash report watchdog received a finish notification; exiting watchdog thread.\n");
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
CrashReportWatchdog::StartCrashReport()
{
    WriteCommand(Command::Started);
}

void
CrashReportWatchdog::StopCrashReport()
{
    WriteCommand(Command::Finished);
}

void
CrashReportWatchdog::WriteCommand(Command command)
{
    int writeFd = m_pipe[1];
    if (writeFd == -1)
    {
        return;
    }

    int savedErrno = errno;
    char commandValue = static_cast<char>(command);
    while (true)
    {
        ssize_t writeResult = write(writeFd, &commandValue, sizeof(commandValue));
        if (writeResult == sizeof(commandValue))
        {
            break;
        }

        if (writeResult != -1 || errno != EINTR)
        {
            break;
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

// Terminate from the watchdog thread after restoring the original SIGABRT
// handler, matching the normal post-crash-report abort path without trying to
// create another crash report from the watchdog thread.
void
CrashReportWatchdog::Abort()
{
    SEHCleanupSignals(false /* isChildProcess */);

    sigset_t signalSet;
    sigemptyset(&signalSet);
    sigaddset(&signalSet, SIGABRT);
    (void)pthread_sigmask(SIG_UNBLOCK, &signalSet, nullptr);

    abort();
    _exit(CRASH_REPORT_WATCHDOG_SIGNAL_EXIT_CODE_OFFSET + SIGABRT);
}

CrashReportWatchdogScope::CrashReportWatchdogScope()
{
    CrashReportWatchdog* watchdog = CrashReportWatchdog::GetInstance();
    if (watchdog != nullptr)
    {
        watchdog->StartCrashReport();
    }
}

CrashReportWatchdogScope::~CrashReportWatchdogScope()
{
    CrashReportWatchdog* watchdog = CrashReportWatchdog::GetInstance();
    if (watchdog != nullptr)
    {
        watchdog->StopCrashReport();
    }
}
