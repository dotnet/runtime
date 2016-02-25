// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_config.h"
#include "pal_console.h"
#include "pal_utilities.h"

#include <assert.h>
#include <errno.h>
#include <pthread.h>
#include <signal.h>
#include <stdio.h>
#include <stdlib.h>
#include <sys/ioctl.h>
#include <termios.h>
#include <unistd.h>
#include <poll.h>

extern "C" int32_t SystemNative_GetWindowSize(WinSize* windowSize)
{
    assert(windowSize != nullptr);

#if HAVE_IOCTL && HAVE_TIOCGWINSZ
    int error = ioctl(STDOUT_FILENO, TIOCGWINSZ, windowSize);

    if (error != 0)
    {
        *windowSize = {}; // managed out param must be initialized
    }

    return error;
#else
    errno = ENOTSUP;
    return -1;
#endif
}

extern "C" int32_t SystemNative_IsATty(intptr_t fd)
{
    return isatty(ToFileDescriptor(fd));
}

static bool g_initialized = false;
static termios g_originalTermios = { };

static void UninitializeConsole()
{
    assert(g_initialized);
    if (g_initialized)
    {
        g_initialized = false;
        tcsetattr(STDIN_FILENO, TCSANOW, &g_originalTermios);
    }
}

extern "C" void SystemNative_InitializeConsole()
{
    assert(!g_initialized);

#if HAVE_TCGETATTR && HAVE_TCSETATTR && HAVE_ECHO && HAVE_ICANON && HAVE_TCSANOW
    struct termios newtermios = {};
    if (tcgetattr(STDIN_FILENO, &newtermios) >= 0)
    {
        g_originalTermios = newtermios;
        newtermios.c_lflag &= static_cast<uint32_t>(~(ECHO | ICANON));
        newtermios.c_cc[VMIN] = 1;
        newtermios.c_cc[VTIME] = 0;
        if (tcsetattr(STDIN_FILENO, TCSANOW, &newtermios) >= 0)
        {
            g_initialized = true;
            atexit(UninitializeConsole);
        }
    }
#endif
}

static int TranslatePalControlCharacterName(int name)
{
    switch (name)
    {
#ifdef VINTR
        case PAL_VINTR: return VINTR;
#endif
#ifdef VQUIT
        case PAL_VQUIT: return VQUIT;
#endif
#ifdef VERASE
        case PAL_VERASE: return VERASE;
#endif
#ifdef VKILL
        case PAL_VKILL: return VKILL;
#endif
#ifdef VEOF
        case PAL_VEOF: return VEOF;
#endif
#ifdef VTIME
        case PAL_VTIME: return VTIME;
#endif
#ifdef VMIN
        case PAL_VMIN: return VMIN;
#endif
#ifdef VSWTC
        case PAL_VSWTC: return VSWTC;
#endif
#ifdef VSTART
        case PAL_VSTART: return VSTART;
#endif
#ifdef VSTOP
        case PAL_VSTOP: return VSTOP;
#endif
#ifdef VSUSP
        case PAL_VSUSP: return VSUSP;
#endif
#ifdef VEOL
        case PAL_VEOL: return VEOL;
#endif
#ifdef VREPRINT
        case PAL_VREPRINT: return VREPRINT;
#endif
#ifdef VDISCARD
        case PAL_VDISCARD: return VDISCARD;
#endif
#ifdef VWERASE
        case PAL_VWERASE: return VWERASE;
#endif
#ifdef VLNEXT
        case PAL_VLNEXT: return VLNEXT;
#endif
#ifdef VEOL2
        case PAL_VEOL2: return VEOL2;
#endif
        default: return -1;
    }
}

extern "C" void SystemNative_GetControlCharacters(
    int32_t* controlCharacterNames, uint8_t* controlCharacterValues, 
    int32_t controlCharacterLength)
{
    assert(controlCharacterNames != nullptr);
    assert(controlCharacterValues != nullptr);
    assert(controlCharacterLength >= 0);

    memset(controlCharacterValues, 0, sizeof(uint8_t) * UnsignedCast(controlCharacterLength));

#if HAVE_TCGETATTR
    struct termios newtermios = {};
    if (tcgetattr(STDIN_FILENO, &newtermios) >= 0)
    {
        for (int i = 0; i < controlCharacterLength; i++)
        {
            int name = TranslatePalControlCharacterName(controlCharacterNames[i]);
            if (name >= 0)
            {
                controlCharacterValues[i] = newtermios.c_cc[name];
            }
        }
    }
#endif
}

extern "C" int32_t SystemNative_StdinReady()
{
    struct pollfd fd;
    fd.fd = STDIN_FILENO;
    fd.events = POLLIN;
    return poll(&fd, 1, 0) > 0 ? 1 : 0;
}

extern "C" int32_t SystemNative_ReadStdinUnbuffered(void* buffer, int32_t bufferSize)
{
    assert(buffer != nullptr || bufferSize == 0);
    assert(bufferSize >= 0);

    if (bufferSize < 0)
    {
        errno = EINVAL;
        return -1;
    }

    ssize_t count;
    while (CheckInterrupted(count = read(STDIN_FILENO, buffer, UnsignedCast(bufferSize))));
    return static_cast<int32_t>(count);
}

static struct sigaction g_origSigIntHandler, g_origSigQuitHandler; // saved signal handlers
static volatile CtrlCallback g_ctrlCallback = nullptr; // Callback invoked for SIGINT/SIGQUIT
static bool g_signalHandlingInitialized = false; // Whether signal handling is initialized
static int g_signalPipe[2] = {-1, -1}; // Pipe used between signal handler and worker

// Signal handler for SIGINT / SIGQUIT.
static void sigintquit_handler(int code)
{
    // Write the signal code to the pipe
    uint8_t signalCodeByte = static_cast<uint8_t>(code);
    ssize_t writtenBytes;
    do
    {
        writtenBytes = write(g_signalPipe[1], &signalCodeByte, 1);
    } while ((writtenBytes == -1) && (errno == EINTR));

    if (writtenBytes != 1)
    {
        abort(); // fatal error
    }
}

// Ctrl-handling worker thread entry point.
void* CtrlHandleLoop(void* arg)
{
    // Passed in argument is a ptr to the file descriptor
    // for the read end of the pipe.
    assert(arg != nullptr);
    int pipeFd = *reinterpret_cast<int*>(arg);
    free(arg);
    assert(pipeFd >= 0);

    // Continually read a signal code from the signal pipe and process it,
    // until the pipe is closed.
    while (true)
    {
        // Read the next signal, trying again if we were interrupted
        uint8_t signalCode;
        ssize_t bytesRead;
        do
        {
            bytesRead = read(pipeFd, &signalCode, 1);
        } while (bytesRead == -1 && errno == EINTR);

        if (bytesRead <= 0)
        {
            // Write end of pipe was closed or another error occurred.
            // Regardless, no more data is available, so we close the read
            // end of the pipe and exit.
            close(pipeFd);
            return nullptr;
        }

        // Invoke the callback.  We take the lock while calling it so as to prevent
        // it from being removed while we're still using it.
        CtrlCallback callback = g_ctrlCallback;
        int rv = callback != nullptr ? callback(signalCode == SIGQUIT ? Break : Interrupt) : 0;
        if (rv == 0) // callback removed or was invoked and didn't handle the signal
        {
            // In general, we now want to remove our handler and reissue the signal to
            // be picked up by the previously registered handler.  In the most common case,
            // this will be the default handler, causing the process to be torn down.
            // It could also be a custom handle registered by other code before us.
            // In the rare case where the signal is set to be ignored, though, we don't
            // want to do that, as we know our process will simply remain running yet our
            // handlers will never end up being invoked again. (It's possible that can
            // happen as well in the custom case, but we can't detect that or handle it well,
            // at which point we'll just stop responding to the relevant signal here if the
            // process does remain alive. We only unregister from the relevant handler, though,
            // so the handler(s) for the other signal(s) will still remain registered.)

            if (signalCode == SIGINT)
            {
                if (g_origSigIntHandler.sa_handler != SIG_IGN)
                {
                    sigaction(SIGINT, &g_origSigIntHandler, NULL);
                    kill(getpid(), SIGINT);
                }
            } 
            else if (signalCode == SIGQUIT)
            {
                if (g_origSigQuitHandler.sa_handler != SIG_IGN)
                {
                    sigaction(SIGQUIT, &g_origSigQuitHandler, NULL);
                    kill(getpid(), SIGQUIT);
                }
            }

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

static bool InitializeSignalHandling()
{
    assert(!g_signalHandlingInitialized);

    // Create a pipe we'll use to communicate with our worker
    // thread.  We can't do anything interesting in the signal handler,
    // so we instead send a message to another thread that'll do
    // the handling work.
    if (pipe(g_signalPipe) != 0)
    {
        return false;
    }
    assert(g_signalPipe[0] >= 0);
    assert(g_signalPipe[1] >= 0);

    // Create a small object to pass the read end of the pipe to the worker.
    int* readFdPtr = reinterpret_cast<int*>(malloc(sizeof(int)));
    if (readFdPtr == nullptr)
    {
        CloseSignalHandlingPipe();
        errno = ENOMEM;
        return false;
    }
    *readFdPtr = g_signalPipe[0];

    // The pipe is created.  Create the worker thread.
    pthread_t handlerThread;
    if (pthread_create(&handlerThread, nullptr, CtrlHandleLoop, readFdPtr) != 0)
    {
        int err = errno;
        free(readFdPtr);
        CloseSignalHandlingPipe();
        errno = err;
        return false;
    }

    // Finally, register the signal handlers for SIGINT and SIGQUIT.
    struct sigaction newAction = {
        .sa_flags = SA_RESTART, .sa_handler = &sigintquit_handler,
    };
    sigemptyset(&newAction.sa_mask);

    int rv = sigaction(SIGINT, &newAction, &g_origSigIntHandler);
    assert(rv == 0);
    rv = sigaction(SIGQUIT, &newAction, &g_origSigQuitHandler);
    assert(rv == 0);

    g_signalHandlingInitialized = true;
    return true;
}

extern "C" int32_t SystemNative_RegisterForCtrl(CtrlCallback callback)
{
    assert(callback != nullptr);
    assert(g_ctrlCallback == nullptr);

    g_ctrlCallback = callback;

    if (!g_signalHandlingInitialized && !InitializeSignalHandling())
    {
        g_ctrlCallback = nullptr;
        return 0;
    }

    return 1;
}

extern "C" void SystemNative_UnregisterForCtrl()
{
    assert(g_ctrlCallback != nullptr);
    g_ctrlCallback = nullptr;

    // Keep the signal handlers registered and the worker thread
    // up and running for when registration is done again.
}
