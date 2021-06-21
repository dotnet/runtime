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
static struct sigaction g_origSigHandler[NSIG];
static bool g_handlerIsInstalled[NSIG];

// Callback invoked for SIGCHLD/SIGCONT/SIGWINCH
static volatile TerminalInvalidationCallback g_terminalInvalidationCallback = NULL;
// Callback invoked for SIGCHLD
static volatile SigChldCallback g_sigChldCallback = NULL;
static volatile bool g_sigChldConsoleConfigurationDelayed;
static void (*g_sigChldConsoleConfigurationCallback)(void);
// Callback invoked for for SIGTTOU while terminal settings are changed.
static volatile ConsoleSigTtouHandler g_consoleTtouHandler;

// Callback invoked for PosixSignal handling.
static PosixSignalHandler g_posixSignalHandler = NULL;
// Tracks whether there are PosixSignal handlers registered.
static volatile bool g_hasPosixSignalRegistrations[NSIG];

static int g_signalPipe[2] = {-1, -1}; // Pipe used between signal handler and worker

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
    }

    if (   signal > 0
        && signal <= NSIG      // Ensure we stay within the static arrays.
#ifdef SIGRTMAX
        && signal <= SIGRTMAX  // Runtime check for highest value.
#endif
       )
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
    bool* isInstalled = &g_handlerIsInstalled[sig - 1];
    *isInstalled = false;
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

    // Signals that can be canceled through the PosixSignal API are handled later.
    // For other signals, we immediately invoke the original handler.
    if (sig != SIGQUIT &&
        sig != SIGTERM &&
        sig != SIGINT)
    {
        struct sigaction* origHandler = OrigActionFor(sig);
        if (origHandler->sa_flags & SA_SIGINFO)
        {
            assert(origHandler->sa_sigaction);
            origHandler->sa_sigaction(sig, siginfo, context);
        }
        else if (origHandler->sa_handler != SIG_IGN &&
                 origHandler->sa_handler != SIG_DFL)
        {
            origHandler->sa_handler(sig);
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

void SystemNative_HandleNonCanceledPosixSignal(int signalCode)
{
    // Performs action the runtime performs on signal that is cancelable
    // using the PosixSignal API.

    if (signalCode == SIGQUIT ||
        signalCode == SIGTERM ||
        signalCode == SIGINT)
    {
        // Terminate the process using the original handler.
#ifdef HAS_CONSOLE_SIGNALS
        UninitializeTerminal();
#endif
        // Restore the original signal handler and invoke it.
        RestoreSignalHandler(signalCode);
        kill(getpid(), signalCode);
    }
    else if (signalCode == SIGCHLD)
    {
        if (g_sigChldConsoleConfigurationDelayed)
        {
            g_sigChldConsoleConfigurationDelayed = false;

            assert(g_sigChldConsoleConfigurationCallback);
            g_sigChldConsoleConfigurationCallback();
        }
    }
    else if (signalCode == SIGCONT)
    {
#ifdef HAS_CONSOLE_SIGNALS
        ReinitializeTerminal();
#endif
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
            // When the original disposition is SIG_IGN, children that terminated did not become zombies.
            // Since we overwrote the disposition, we have become responsible for reaping those processes.
            bool reapAll = (void*)OrigActionFor(signalCode)->sa_sigaction == (void*)SIG_IGN;
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
                signal = (PosixSignal)0;
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

static void InstallSignalHandler(int sig, int flags)
{
    int rv;
    (void)rv; // only used for assert
    struct sigaction* orig = OrigActionFor(sig);
    bool* isInstalled = &g_handlerIsInstalled[sig - 1];

    if (*isInstalled)
    {
        // Already installed.
        return;
    }

    // We respect ignored signals.
    // Setting up a handler for them causes child processes to reset to the
    // default handler on exec, which means they will terminate on some signals
    // which were set to ignore.
    rv = sigaction(sig, NULL, orig);
    assert(rv == 0);
    if ((void*)orig->sa_sigaction == (void*)SIG_IGN)
    {
        *isInstalled = true;
        return;
    }

    struct sigaction newAction;
    memset(&newAction, 0, sizeof(struct sigaction));
    newAction.sa_flags = flags | SA_SIGINFO;
    sigemptyset(&newAction.sa_mask);
    newAction.sa_sigaction = &SignalHandler;

    rv = sigaction(sig, &newAction, orig);
    assert(rv == 0);
    *isInstalled = true;
}

void SystemNative_SetTerminalInvalidationHandler(TerminalInvalidationCallback callback)
{
    assert(callback != NULL);
    assert(g_terminalInvalidationCallback == NULL);

    pthread_mutex_lock(&lock);
    {
        g_terminalInvalidationCallback = callback;

        InstallSignalHandler(SIGCONT, SA_RESTART);
        InstallSignalHandler(SIGCHLD, SA_RESTART);
        InstallSignalHandler(SIGWINCH, SA_RESTART);
    }
    pthread_mutex_unlock(&lock);
}

void SystemNative_RegisterForSigChld(SigChldCallback callback)
{
    assert(callback != NULL);
    assert(g_sigChldCallback == NULL);

    pthread_mutex_lock(&lock);
    {
        g_sigChldCallback = callback;

        InstallSignalHandler(SIGCHLD, SA_RESTART);
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

    return 1;
}

void SystemNative_EnablePosixSignalHandling(int signalCode)
{
    assert(g_posixSignalHandler != NULL);
    assert(signalCode > 0 && signalCode <= NSIG);

    pthread_mutex_lock(&lock);
    {
        InstallSignalHandler(signalCode, SA_RESTART);

        g_hasPosixSignalRegistrations[signalCode - 1] = true;
    }
    pthread_mutex_unlock(&lock);
}

void SystemNative_DisablePosixSignalHandling(int signalCode)
{
    assert(signalCode > 0 && signalCode <= NSIG);

    pthread_mutex_lock(&lock);
    {
        g_hasPosixSignalRegistrations[signalCode - 1] = false;

        if (!(g_consoleTtouHandler && signalCode == SIGTTOU) &&
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
        InstallSignalHandler(SIGTTOU, (int)SA_RESETHAND);
    }
    pthread_mutex_unlock(&lock);
}

void UninstallTTOUHandlerForConsole(void)
{
    pthread_mutex_lock(&lock);
    {
        g_consoleTtouHandler = NULL;

        RestoreSignalHandler(SIGTTOU);
        if (g_hasPosixSignalRegistrations[SIGTTOU - 1])
        {
            InstallSignalHandler(SIGTTOU, SA_RESTART);
        }
    }
    pthread_mutex_unlock(&lock);
}

#ifndef HAS_CONSOLE_SIGNALS

int32_t SystemNative_InitializeTerminalAndSignalHandling()
{
    errno = ENOSYS;
    return 0;
}

#endif
