// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_console.h"
#include "pal_signal.h"
#include "pal_io.h"
#include "pal_utilities.h"

#include <assert.h>
#include <errno.h>
#include <pthread.h>
#include <signal.h>
#include <stdlib.h>
#include <sys/types.h>
#include <sys/wait.h>
#include <unistd.h>

static pthread_mutex_t lock = PTHREAD_MUTEX_INITIALIZER;

// Saved signal handlers
static struct sigaction* g_origSigHandler;
static bool* g_handlerIsInstalled;

// Callback invoked for SIGCHLD/SIGCONT/SIGWINCH
static volatile TerminalInvalidationCallback g_terminalInvalidationCallback = NULL;
// Callback invoked for SIGCHLD
static volatile SigChldCallback g_sigChldCallback = NULL;
static volatile bool g_sigChldConsoleConfigurationDelayed;
static void (*g_sigChldConsoleConfigurationCallback)(void);
// Callback invoked for SIGTTOU while terminal settings are changed.
static volatile ConsoleSigTtouHandler g_consoleTtouHandler;

// Callback invoked for PosixSignal handling.
static PosixSignalHandler g_posixSignalHandler = NULL;
// Tracks whether there are PosixSignal handlers registered.
static volatile bool* g_hasPosixSignalRegistrations;

static int g_signalPipe[2] = {-1, -1}; // Pipe used between signal handler and worker

static pid_t g_pid;

static int GetSignalMax() // Returns the highest usable signal number.
{
#ifdef SIGRTMAX
    return SIGRTMAX;
#else
    return NSIG;
#endif
}

static bool IsCancelableTerminationSignal(int sig)
{
    return sig == SIGINT ||
           sig == SIGQUIT ||
           sig == SIGTERM;
}

static bool IsSaSigInfo(struct sigaction* action)
{
    assert(action);
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wsign-conversion" // sa_flags is unsigned on Android.
    return (action->sa_flags & SA_SIGINFO) != 0;
#pragma clang diagnostic pop
}

static bool IsSigDfl(struct sigaction* action)
{
    assert(action);
    // macOS can return sigaction with SIG_DFL and SA_SIGINFO.
    // SA_SIGINFO means we should use sa_sigaction, but here we want to check sa_handler.
    // So we ignore SA_SIGINFO when sa_sigaction and sa_handler are at the same address.
    return (&action->sa_handler == (void*)&action->sa_sigaction || !IsSaSigInfo(action)) &&
            action->sa_handler == SIG_DFL;
}

static bool IsSigIgn(struct sigaction* action)
{
    assert(action);
    return (&action->sa_handler == (void*)&action->sa_sigaction || !IsSaSigInfo(action)) &&
            action->sa_handler == SIG_IGN;
}

static bool TryConvertSignalCodeToPosixSignal(int signalCode, PosixSignal* posixSignal)
{
    assert(posixSignal != NULL);

    switch (signalCode)
    {
        case SIGHUP:
            *posixSignal = PosixSignalSIGHUP;
            return true;

        case SIGINT:
            *posixSignal = PosixSignalSIGINT;
            return true;

        case SIGQUIT:
            *posixSignal = PosixSignalSIGQUIT;
            return true;

        case SIGTERM:
            *posixSignal = PosixSignalSIGTERM;
            return true;

        case SIGCHLD:
            *posixSignal = PosixSignalSIGCHLD;
            return true;

        case SIGWINCH:
            *posixSignal = PosixSignalSIGWINCH;
            return true;

        case SIGCONT:
            *posixSignal = PosixSignalSIGCONT;
            return true;

        case SIGTTIN:
            *posixSignal = PosixSignalSIGTTIN;
            return true;

        case SIGTTOU:
            *posixSignal = PosixSignalSIGTTOU;
            return true;

        case SIGTSTP:
            *posixSignal = PosixSignalSIGTSTP;
            return true;

        default:
            *posixSignal = signalCode;
            return false;
    }
}

int32_t SystemNative_GetPlatformSignalNumber(PosixSignal signal)
{
    switch (signal)
    {
        case PosixSignalSIGHUP:
            return SIGHUP;

        case PosixSignalSIGINT:
            return SIGINT;

        case PosixSignalSIGQUIT:
            return SIGQUIT;

        case PosixSignalSIGTERM:
            return SIGTERM;

        case PosixSignalSIGCHLD:
            return SIGCHLD;

        case PosixSignalSIGWINCH:
            return SIGWINCH;

        case PosixSignalSIGCONT:
            return SIGCONT;

        case PosixSignalSIGTTIN:
            return SIGTTIN;

        case PosixSignalSIGTTOU:
            return SIGTTOU;

        case PosixSignalSIGTSTP:
            return SIGTSTP;

        case PosixSignalInvalid:
            break;
    }

    if (signal > 0 && signal <= GetSignalMax())
    {
        return signal;
    }

    return 0;
}

void SystemNative_SetPosixSignalHandler(PosixSignalHandler signalHandler)
{
    assert(signalHandler);
    assert(g_posixSignalHandler == NULL || g_posixSignalHandler == signalHandler);

    g_posixSignalHandler = signalHandler;
}

static struct sigaction* OrigActionFor(int sig)
{
    return &g_origSigHandler[sig - 1];
}

static void RestoreSignalHandler(int sig)
{
    g_handlerIsInstalled[sig - 1] = false;
    sigaction(sig, OrigActionFor(sig), NULL);
}

static void SignalHandler(int sig, siginfo_t* siginfo, void* context)
{
    if (sig == SIGCONT)
    {
        ConsoleSigTtouHandler consoleTtouHandler = g_consoleTtouHandler;
        if (consoleTtouHandler != NULL)
        {
            consoleTtouHandler();
        }
    }

    // For these signals, the runtime original sa_sigaction/sa_handler will terminate the app.
    // This termination can be canceled using the PosixSignal API.
    // For other signals, we immediately invoke the original handler.
    if (!IsCancelableTerminationSignal(sig))
    {
        struct sigaction* origHandler = OrigActionFor(sig);
        if (!IsSigDfl(origHandler) && !IsSigIgn(origHandler))
        {
            if (IsSaSigInfo(origHandler))
            {
                assert(origHandler->sa_sigaction);
                origHandler->sa_sigaction(sig, siginfo, context);
            }
            else
            {
                assert(origHandler->sa_handler);
                origHandler->sa_handler(sig);
            }

        }
    }

    // Perform further processing on background thread.
    // Write the signal code to a pipe that's read by the thread.
    uint8_t signalCodeByte = (uint8_t)sig;
    ssize_t writtenBytes;
    while ((writtenBytes = write(g_signalPipe[1], &signalCodeByte, 1)) < 0 && errno == EINTR);

    if (writtenBytes != 1)
    {
        abort(); // fatal error
    }
}

void SystemNative_HandleNonCanceledPosixSignal(int32_t signalCode)
{
    switch (signalCode)
    {
        case SIGCONT:
            // Default disposition is Continue.
#ifdef HAS_CONSOLE_SIGNALS
            ReinitializeTerminal();
#endif
            break;
        case SIGTSTP:
        case SIGTTIN:
        case SIGTTOU:
            // Default disposition is Stop.
            // no-op.
            break;
        case SIGCHLD:
            // Default disposition is Ignore.
            if (g_sigChldConsoleConfigurationDelayed)
            {
                g_sigChldConsoleConfigurationDelayed = false;

                assert(g_sigChldConsoleConfigurationCallback);
                g_sigChldConsoleConfigurationCallback();
            }
            break;
        case SIGURG:
        case SIGWINCH:
            // Default disposition is Ignore.
            // no-op.
            break;
        default:
            // Default disposition is Terminate.
            if (!IsCancelableTerminationSignal(signalCode) && !IsSigDfl(OrigActionFor(signalCode)))
            {
                // We've already called the original handler in SignalHandler.
                break;
            }
            if (IsSigIgn(OrigActionFor(signalCode)))
            {
                // Original handler doesn't do anything.
                break;
            }
            // Restore and invoke the original handler.
            pthread_mutex_lock(&lock);
            {
                RestoreSignalHandler(signalCode);
            }
            pthread_mutex_unlock(&lock);
#ifdef HAS_CONSOLE_SIGNALS
            UninitializeTerminal();
#endif
            kill(g_pid, signalCode);
            break;
    }
}

// Entrypoint for the thread that handles signals where our handling
// isn't signal-safe.  Those signal handlers write the signal to a pipe,
// which this loop reads and processes.
static void* SignalHandlerLoop(void* arg)
{
    // Passed in argument is a ptr to the file descriptor
    // for the read end of the pipe.
    assert(arg != NULL);
    int pipeFd = *(int*)arg;
    free(arg);
    assert(pipeFd >= 0);

    // Continually read a signal code from the signal pipe and process it,
    // until the pipe is closed.
    while (true)
    {
        // Read the next signal, trying again if we were interrupted
        uint8_t signalCode;
        ssize_t bytesRead;
        while ((bytesRead = read(pipeFd, &signalCode, 1)) < 0 && errno == EINTR);

        if (bytesRead <= 0)
        {
            // Write end of pipe was closed or another error occurred.
            // Regardless, no more data is available, so we close the read
            // end of the pipe and exit.
            close(pipeFd);
            return NULL;
        }

        if (signalCode == SIGCHLD || signalCode == SIGCONT || signalCode == SIGWINCH)
        {
            TerminalInvalidationCallback callback = g_terminalInvalidationCallback;
            if (callback != NULL)
            {
                callback();
            }
        }

        bool usePosixSignalHandler = g_hasPosixSignalRegistrations[signalCode - 1];
        if (signalCode == SIGCHLD)
        {
            // By default we only reap managed processes started using the 'Process' class.
            // This allows other code to start processes without .NET reaping them.
            //
            // In two cases we reap all processes (and may inadvertently reap non-managed processes):
            // - When the original disposition is SIG_IGN, children that terminated did not become zombies.
            //   Because overwrote the disposition, we have become responsible for reaping those processes.
            // - pid 1 (the init daemon) is responsible for reaping orphaned children.
            //   Because containers usually don't have an init daemon .NET may be pid 1.
            bool reapAll = g_pid == 1 || IsSigIgn(OrigActionFor(signalCode));
            SigChldCallback callback = g_sigChldCallback;

            // double-checked locking
            if (callback == NULL && reapAll)
            {
                // avoid race with SystemNative_RegisterForSigChld
                pthread_mutex_lock(&lock);
                {
                    callback = g_sigChldCallback;
                    if (callback == NULL)
                    {
                        pid_t pid;
                        do
                        {
                            int status;
                            while ((pid = waitpid(-1, &status, WNOHANG)) < 0 && errno == EINTR);
                        } while (pid > 0);
                    }
                }
                pthread_mutex_unlock(&lock);
            }

            if (callback != NULL)
            {
                if (callback(reapAll ? 1 : 0, usePosixSignalHandler ? 0 : 1 /* configureConsole */))
                {
                    g_sigChldConsoleConfigurationDelayed = true;
                }
            }
        }

        if (usePosixSignalHandler)
        {
            assert(g_posixSignalHandler != NULL);
            PosixSignal signal;
            if (!TryConvertSignalCodeToPosixSignal(signalCode, &signal))
            {
                signal = PosixSignalInvalid;
            }
            usePosixSignalHandler = g_posixSignalHandler(signalCode, signal) != 0;
        }

        if (!usePosixSignalHandler)
        {
            SystemNative_HandleNonCanceledPosixSignal(signalCode);
        }
    }
}

static void CloseSignalHandlingPipe()
{
    assert(g_signalPipe[0] >= 0);
    assert(g_signalPipe[1] >= 0);
    close(g_signalPipe[0]);
    close(g_signalPipe[1]);
    g_signalPipe[0] = -1;
    g_signalPipe[1] = -1;
}

static bool InstallSignalHandler(int sig, int flags)
{
    int rv;
    struct sigaction* orig = OrigActionFor(sig);
    bool* isInstalled = &g_handlerIsInstalled[sig - 1];

    if (*isInstalled)
    {
        // Already installed.
        return true;
    }

    // We respect ignored signals.
    // Setting up a handler for them causes child processes to reset to the
    // default handler on exec, which means they will terminate on some signals
    // which were set to ignore.
    rv = sigaction(sig, NULL, orig);
    if (rv != 0)
    {
        return false;
    }
    if (IsSigIgn(orig))
    {
        *isInstalled = true;
        return true;
    }

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wsign-conversion" // sa_flags is unsigned on Android.
    struct sigaction newAction;
    if (!IsSigDfl(orig))
    {
        // Maintain flags and mask of original handler.
        newAction = *orig;
        newAction.sa_flags = orig->sa_flags & ~(SA_RESTART | SA_RESETHAND);
    }
    else
    {
        memset(&newAction, 0, sizeof(struct sigaction));
    }
    newAction.sa_flags |= flags | SA_SIGINFO;
#pragma clang diagnostic pop
    newAction.sa_sigaction = &SignalHandler;

    rv = sigaction(sig, &newAction, orig);
    if (rv != 0)
    {
        return false;
    }
    *isInstalled = true;
    return true;
}

void SystemNative_SetTerminalInvalidationHandler(TerminalInvalidationCallback callback)
{
    assert(callback != NULL);
    assert(g_terminalInvalidationCallback == NULL);
    bool installed;
    (void)installed; // only used for assert

    pthread_mutex_lock(&lock);
    {
        g_terminalInvalidationCallback = callback;

        installed = InstallSignalHandler(SIGCONT, SA_RESTART);
        assert(installed);
        installed = InstallSignalHandler(SIGCHLD, SA_RESTART);
        assert(installed);
        installed = InstallSignalHandler(SIGWINCH, SA_RESTART);
        assert(installed);
    }
    pthread_mutex_unlock(&lock);
}

void SystemNative_RegisterForSigChld(SigChldCallback callback)
{
    assert(callback != NULL);
    assert(g_sigChldCallback == NULL);
    bool installed;
    (void)installed; // only used for assert

    pthread_mutex_lock(&lock);
    {
        g_sigChldCallback = callback;

        installed = InstallSignalHandler(SIGCHLD, SA_RESTART);
        assert(installed);
    }
    pthread_mutex_unlock(&lock);
}

void SystemNative_SetDelayedSigChildConsoleConfigurationHandler(void (*callback)(void))
{
    assert(g_sigChldConsoleConfigurationCallback == NULL);

    g_sigChldConsoleConfigurationCallback = callback;
}

static bool CreateSignalHandlerThread(int* readFdPtr)
{
    pthread_attr_t attr;
    if (pthread_attr_init(&attr) != 0)
    {
        return false;
    }

    bool success = false;
#ifdef DEBUG
    // Set the thread stack size to 512kB. This is to fix a problem on Alpine
    // Linux where the default secondary thread stack size is just about 85kB
    // and our testing have hit cases when that was not enough in debug
    // and checked builds due to some large frames in JIT code.
    if (pthread_attr_setstacksize(&attr, 512 * 1024) == 0)
#endif
    {
        pthread_t handlerThread;
        if (pthread_create(&handlerThread, &attr, SignalHandlerLoop, readFdPtr) == 0)
        {
            success = true;
        }
    }

    int err = errno;
    pthread_attr_destroy(&attr);
    errno = err;

    return success;
}

int32_t InitializeSignalHandlingCore()
{
    size_t signalMax = (size_t)GetSignalMax();
    g_origSigHandler = (struct sigaction*)calloc(sizeof(struct sigaction), signalMax);
    g_handlerIsInstalled = (bool*)calloc(sizeof(bool), signalMax);
    g_hasPosixSignalRegistrations = (bool*)calloc(sizeof(bool), signalMax);
    if (g_origSigHandler == NULL ||
        g_handlerIsInstalled == NULL ||
        g_hasPosixSignalRegistrations == NULL)
    {
        free(g_origSigHandler);
        free(g_handlerIsInstalled);
        free((void*)(size_t)g_hasPosixSignalRegistrations);
        g_origSigHandler = NULL;
        g_handlerIsInstalled = NULL;
        g_hasPosixSignalRegistrations = NULL;
        errno = ENOMEM;
        return 0;
    }

    g_pid = getpid();

    // Create a pipe we'll use to communicate with our worker
    // thread.  We can't do anything interesting in the signal handler,
    // so we instead send a message to another thread that'll do
    // the handling work.
    if (SystemNative_Pipe(g_signalPipe, PAL_O_CLOEXEC) != 0)
    {
        return 0;
    }
    assert(g_signalPipe[0] >= 0);
    assert(g_signalPipe[1] >= 0);

    // Create a small object to pass the read end of the pipe to the worker.
    int* readFdPtr = (int*)malloc(sizeof(int));
    if (readFdPtr == NULL)
    {
        CloseSignalHandlingPipe();
        errno = ENOMEM;
        return 0;
    }
    *readFdPtr = g_signalPipe[0];

    // The pipe is created.  Create the worker thread.

    if (!CreateSignalHandlerThread(readFdPtr))
    {
        int err = errno;
        free(readFdPtr);
        CloseSignalHandlingPipe();
        errno = err;
        return 0;
    }

#ifdef HAS_CONSOLE_SIGNALS
    // Unconditionally register signals for terminal configuration.
    bool installed = InstallSignalHandler(SIGINT, SA_RESTART);
    assert(installed);
    installed = InstallSignalHandler(SIGQUIT, SA_RESTART);
    assert(installed);
    installed = InstallSignalHandler(SIGCONT, SA_RESTART);
    assert(installed);
#endif

    return 1;
}

int32_t SystemNative_EnablePosixSignalHandling(int signalCode)
{
    assert(g_posixSignalHandler != NULL);
    assert(signalCode > 0 && signalCode <= GetSignalMax());

    bool installed;
    pthread_mutex_lock(&lock);
    {
        installed = InstallSignalHandler(signalCode, SA_RESTART);

        g_hasPosixSignalRegistrations[signalCode - 1] = installed;
    }
    pthread_mutex_unlock(&lock);

    return installed ? 1 : 0;
}

void SystemNative_DisablePosixSignalHandling(int signalCode)
{
    assert(signalCode > 0 && signalCode <= GetSignalMax());

    pthread_mutex_lock(&lock);
    {
        g_hasPosixSignalRegistrations[signalCode - 1] = false;

        // Don't restore handler when something other than posix handling needs the signal.
        if (
#ifdef HAS_CONSOLE_SIGNALS
            signalCode != SIGINT && signalCode != SIGQUIT && signalCode != SIGCONT &&
#endif
            !(g_consoleTtouHandler && signalCode == SIGTTOU) &&
            !(g_sigChldCallback && signalCode == SIGCHLD) &&
            !(g_terminalInvalidationCallback && (signalCode == SIGCONT ||
                                                 signalCode == SIGCHLD ||
                                                 signalCode == SIGWINCH)))
        {
            RestoreSignalHandler(signalCode);
        }
    }
    pthread_mutex_unlock(&lock);
}

void InstallTTOUHandlerForConsole(ConsoleSigTtouHandler handler)
{
    bool installed;

    pthread_mutex_lock(&lock);
    {
        assert(g_consoleTtouHandler == NULL);
        g_consoleTtouHandler = handler;

        // When the process is running in background, changing terminal settings
        // will stop it (default SIGTTOU action).
        // We change SIGTTOU's disposition to get EINTR instead.
        // This thread may be used to run a signal handler, which may write to
        // stdout. We set SA_RESETHAND to avoid that handler's write loops infinitly
        // on EINTR when the process is running in background and the terminal
        // configured with TOSTOP.
        RestoreSignalHandler(SIGTTOU);
        installed = InstallSignalHandler(SIGTTOU, (int)SA_RESETHAND);
        assert(installed);
    }
    pthread_mutex_unlock(&lock);
}

void UninstallTTOUHandlerForConsole(void)
{
    bool installed;
    (void)installed; // only used for assert
    pthread_mutex_lock(&lock);
    {
        g_consoleTtouHandler = NULL;

        RestoreSignalHandler(SIGTTOU);
        if (g_hasPosixSignalRegistrations[SIGTTOU - 1])
        {
            installed = InstallSignalHandler(SIGTTOU, SA_RESTART);
            assert(installed);
        }
    }
    pthread_mutex_unlock(&lock);
}

#ifndef HAS_CONSOLE_SIGNALS

int32_t SystemNative_InitializeTerminalAndSignalHandling()
{
    static int32_t initialized = 0;

    // The Process, Console and PosixSignalRegistration classes call this method for initialization.
    if (pthread_mutex_lock(&lock) == 0)
    {
        if (initialized == 0)
        {
            initialized = InitializeSignalHandlingCore();
        }
        pthread_mutex_unlock(&lock);
    }

    return initialized;
}

#endif
