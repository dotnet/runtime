// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
static struct sigaction g_origSigIntHandler, g_origSigQuitHandler; // saved signal handlers for ctrl handling
static struct sigaction g_origSigContHandler, g_origSigChldHandler; // saved signal handlers for reinitialization
static volatile CtrlCallback g_ctrlCallback = NULL; // Callback invoked for SIGINT/SIGQUIT
static volatile SigChldCallback g_sigChldCallback = NULL; // Callback invoked for SIGCHLD
static int g_signalPipe[2] = {-1, -1}; // Pipe used between signal handler and worker

static struct sigaction* OrigActionFor(int sig)
{
    switch (sig)
    {
        case SIGINT:  return &g_origSigIntHandler;
        case SIGQUIT: return &g_origSigQuitHandler;
        case SIGCONT: return &g_origSigContHandler;
        case SIGCHLD: return &g_origSigChldHandler;
    }

    assert(false);
    return NULL;
}

static void SignalHandler(int sig, siginfo_t* siginfo, void* context)
{
    if (sig == SIGCONT || sig == SIGCHLD)
    {
        // SIGCONT will be sent when we're resumed after suspension, at which point
        // we need to set the terminal back up.  Similarly, SIGCHLD will be sent after
        // a child process completes, and that child could have left things in a bad state,
        // so we similarly need to reinitialize.
        ReinitializeConsole();
    }

    // Signal handler for signals where we want our background thread to do the real processing.
    // It simply writes the signal code to a pipe that's read by the thread.
    if (sig == SIGQUIT || sig == SIGINT || sig == SIGCHLD)
    {
        // Write the signal code to the pipe
        uint8_t signalCodeByte = (uint8_t)sig;
        ssize_t writtenBytes;
        while ((writtenBytes = write(g_signalPipe[1], &signalCodeByte, 1)) < 0 && errno == EINTR);

        if (writtenBytes != 1)
        {
            abort(); // fatal error
        }
    }

    // Delegate to any saved handler we may have
    // We assume the original SIGCHLD handler will not reap our children.
    if (sig == SIGCONT || sig == SIGCHLD)
    {
        struct sigaction* origHandler = OrigActionFor(sig);
        if (origHandler->sa_sigaction != NULL &&
            (void*)origHandler->sa_sigaction != (void*)SIG_DFL &&
            (void*)origHandler->sa_sigaction != (void*)SIG_IGN)
        {
            origHandler->sa_sigaction(sig, siginfo, context);
        }
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

        if (signalCode == SIGQUIT || signalCode == SIGINT)
        {
            // We're now handling SIGQUIT and SIGINT. Invoke the callback, if we have one.
            CtrlCallback callback = g_ctrlCallback;
            CtrlCode ctrlCode = signalCode == SIGQUIT ? Break : Interrupt;
            if (callback != NULL)
            {
                callback(ctrlCode);
            }
            else
            {
                SystemNative_RestoreAndHandleCtrl(ctrlCode);
            }
        }
        else if (signalCode == SIGCHLD)
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
                callback(reapAll ? 1 : 0);
            }
        }
        else
        {
            assert_msg(false, "invalid signalCode", (int)signalCode);
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

void SystemNative_RegisterForCtrl(CtrlCallback callback)
{
    assert(callback != NULL);
    assert(g_ctrlCallback == NULL);
    g_ctrlCallback = callback;
}

void SystemNative_UnregisterForCtrl()
{
    assert(g_ctrlCallback != NULL);
    g_ctrlCallback = NULL;
}

void SystemNative_RestoreAndHandleCtrl(CtrlCode ctrlCode)
{
    int signalCode = ctrlCode == Break ? SIGQUIT : SIGINT;
    UninitializeConsole();
    sigaction(signalCode, OrigActionFor(signalCode), NULL);
    kill(getpid(), signalCode);
}

uint32_t SystemNative_RegisterForSigChld(SigChldCallback callback)
{
    if (!InitializeSignalHandling())
    {
        return 0;
    }

    assert(callback != NULL);
    assert(g_sigChldCallback == NULL);

    pthread_mutex_lock(&lock);
    {
        g_sigChldCallback = callback;
    }
    pthread_mutex_unlock(&lock);

    return 1;
}

static void InstallSignalHandler(int sig, bool skipWhenSigIgn)
{
    int rv;
    struct sigaction* orig = OrigActionFor(sig);

    if (skipWhenSigIgn)
    {
        rv = sigaction(sig, NULL, orig);
        assert(rv == 0);
        if ((void*)orig->sa_sigaction == (void*)SIG_IGN)
        {
            return;
        }
    }

    struct sigaction newAction;
    memset(&newAction, 0, sizeof(struct sigaction));
    newAction.sa_flags = SA_RESTART | SA_SIGINFO;
    sigemptyset(&newAction.sa_mask);
    newAction.sa_sigaction = &SignalHandler;

    rv = sigaction(sig, &newAction, orig);
    assert(rv == 0);
}

static bool InitializeSignalHandlingCore()
{
    // Create a pipe we'll use to communicate with our worker
    // thread.  We can't do anything interesting in the signal handler,
    // so we instead send a message to another thread that'll do
    // the handling work.
    if (SystemNative_Pipe(g_signalPipe, PAL_O_CLOEXEC) != 0)
    {
        return false;
    }
    assert(g_signalPipe[0] >= 0);
    assert(g_signalPipe[1] >= 0);

    // Create a small object to pass the read end of the pipe to the worker.
    int* readFdPtr = (int*)malloc(sizeof(int));
    if (readFdPtr == NULL)
    {
        CloseSignalHandlingPipe();
        errno = ENOMEM;
        return false;
    }
    *readFdPtr = g_signalPipe[0];

    // The pipe is created.  Create the worker thread.
    pthread_t handlerThread;
    if (pthread_create(&handlerThread, NULL, SignalHandlerLoop, readFdPtr) != 0)
    {
        int err = errno;
        free(readFdPtr);
        CloseSignalHandlingPipe();
        errno = err;
        return false;
    }

    // Finally, register our signal handlers
    // We don't handle ignored SIGINT/SIGQUIT signals. If we'd setup a handler, our child
    // processes would reset to the default on exec causing them to terminate on these signals.
    InstallSignalHandler(SIGINT , /* skipWhenSigIgn */ true);
    InstallSignalHandler(SIGQUIT, /* skipWhenSigIgn */ true);
    InstallSignalHandler(SIGCONT, /* skipWhenSigIgn */ false);
    InstallSignalHandler(SIGCHLD, /* skipWhenSigIgn */ false);

    return true;
}

uint32_t InitializeSignalHandling()
{
    static bool initialized = false;

    pthread_mutex_lock(&lock);
    {
        if (!initialized)
        {
            initialized = InitializeSignalHandlingCore();
        }
    }
    pthread_mutex_unlock(&lock);

    return initialized ? 1 : 0;
}
