// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_config.h"
#include "pal_console.h"
#include "pal_io.h"
#include "pal_utilities.h"

#include <assert.h>
#include <errno.h>
#include <fcntl.h>
#include <pthread.h>
#include <signal.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
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

static char* g_keypadXmit = nullptr; // string used to enable application mode, from terminfo

static void WriteKeypadXmit() // used in a signal handler, must be signal-safe
{
    // If a terminfo "application mode" keypad_xmit string has been supplied,
    // write it out to the terminal to enter the mode.
    if (g_keypadXmit != nullptr)
    {
        ssize_t ret;
        while (CheckInterrupted(ret = write(STDOUT_FILENO, g_keypadXmit, static_cast<size_t>(sizeof(char) * strlen(g_keypadXmit)))));
        assert(ret >= 0); // failure to change the mode should not prevent app from continuing
    }
}

extern "C" void SystemNative_SetKeypadXmit(const char* terminfoString)
{
    assert(terminfoString != nullptr);

    if (g_keypadXmit != nullptr) // should only happen if initialization initially failed
    {
        free(g_keypadXmit);
        assert(false && "g_keypadXmit was already initialized");
    }

    // Store the string to use to enter application mode, then enter
    g_keypadXmit = strdup(terminfoString);
    WriteKeypadXmit();
}

static bool g_readInProgress = false;        // tracks whether a read is currently in progress, such that attributes have been changed
static bool g_signalForBreak = true;         // tracks whether the terminal should send signals for breaks, such that attributes have been changed
static bool g_haveInitTermios = false;       // whether g_initTermios has been initialized
static struct termios g_initTermios = {};    // the initial attributes captured when Console was initialized
static struct termios g_preReadTermios = {}; // the original attributes captured before a read; valid if g_readInProgress is true
static struct termios g_currTermios = {};    // the current attributes set during a read; valid if g_readInProgress is true

static void UninitializeConsole()
{
    // Put the attributes back to what they were when the console was initially initialized.
    // We only do so, however, if we have explicitly modified the termios; doing so always
    // can result in problems if the app is in the background, as then attempting to call
    // tcsetattr on STDIN_FILENO will suspend the app and prevent its shutdown. We also don't
    // want to, for example, just compare g_currTermios with g_initTermios, as we'd then be
    // factoring in changes made by other apps or by user code.
    if (g_haveInitTermios &&                     // we successfully initialized the console
        (g_readInProgress || !g_signalForBreak)) // we modified attributes
    {
        tcsetattr(STDIN_FILENO, TCSANOW, &g_initTermios);
        // ignore any failure
    }
}

static void IncorporateBreak(struct termios *termios, int32_t signalForBreak)
{
    assert(termios != nullptr);
    assert(signalForBreak == 0 || signalForBreak == 1);

    if (signalForBreak)
        termios->c_lflag |= static_cast<uint32_t>(ISIG);
    else
        termios->c_lflag &= static_cast<uint32_t>(~ISIG);
}

// In order to support Console.ReadKey(intecept: true), we need to disable echo and canonical mode.
// We have two main choices: do so for the entire app, or do so only while in the Console.ReadKey(true).
// The former has a huge downside: the terminal is in a non-echo state, so anything else that runs
// in the same terminal won't echo even if it expects to, e.g. using Process.Start to launch an interactive,
// program, or P/Invoking to a native library that reads from stdin rather than using Console.  The second
// also has a downside, in that any typing which occurs prior to invoking Console.ReadKey(true) will
// be visible even though it wasn't supposed to be.  The downsides of the former approach are so large
// and the cons of the latter minimal and constrained to the one API that we've chosen the second approach.
// Thus, InitializeConsoleBeforeRead is called to set up the state of the console, then a read is done,
// and then UninitializeConsoleAfterRead is called.
extern "C" void SystemNative_InitializeConsoleBeforeRead(uint8_t minChars, uint8_t decisecondsTimeout)
{
    struct termios newTermios;
    if (tcgetattr(STDIN_FILENO, &newTermios) >= 0)
    {
        if (!g_readInProgress)
        {
            // Store the original settings, but only if we didn't already.  This function
            // may be called when the process is resumed after being suspended, and if
            // that happens during a read, we'll call this function to reset the attrs.
            g_preReadTermios = newTermios;
        }

        newTermios.c_iflag &= static_cast<uint32_t>(~(IXON | IXOFF));
        newTermios.c_lflag &= static_cast<uint32_t>(~(ECHO | ICANON | IEXTEN));
        newTermios.c_cc[VMIN] = minChars;
        newTermios.c_cc[VTIME] = decisecondsTimeout;
        IncorporateBreak(&newTermios, g_signalForBreak);

        if (tcsetattr(STDIN_FILENO, TCSANOW, &newTermios) >= 0)
        {
            g_currTermios = newTermios;
            g_readInProgress = true;
        }
    }
}

extern "C" void SystemNative_UninitializeConsoleAfterRead()
{
    if (g_readInProgress)
    {
        g_readInProgress = false;

        int tmpErrno = errno; // preserve any errors from before uninitializing
        IncorporateBreak(&g_preReadTermios, g_signalForBreak);
        int ret = tcsetattr(STDIN_FILENO, TCSANOW, &g_preReadTermios);
        assert(ret >= 0); // shouldn't fail, but if it does we don't want to fail in release
        (void)ret;
        errno = tmpErrno;
    }
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
    int32_t* controlCharacterNames, uint8_t* controlCharacterValues, int32_t controlCharacterLength,
    uint8_t* posixDisableValue)
{
    assert(controlCharacterNames != nullptr);
    assert(controlCharacterValues != nullptr);
    assert(controlCharacterLength >= 0);
    assert(posixDisableValue != nullptr);

#ifdef _POSIX_VDISABLE
    *posixDisableValue = _POSIX_VDISABLE;
#else
    *posixDisableValue = 0;
#endif

    memset(controlCharacterValues, *posixDisableValue, sizeof(uint8_t) * UnsignedCast(controlCharacterLength));

    if (controlCharacterLength > 0)
    {
        struct termios newTermios = {};
        if (tcgetattr(STDIN_FILENO, &newTermios) >= 0)
        {
            for (int i = 0; i < controlCharacterLength; i++)
            {
                int name = TranslatePalControlCharacterName(controlCharacterNames[i]);
                if (name >= 0)
                {
                    controlCharacterValues[i] = newTermios.c_cc[name];
                }
            }
        }
    }
}

extern "C" int32_t SystemNative_StdinReady()
{
    SystemNative_InitializeConsoleBeforeRead(1, 0);
    struct pollfd fd = { .fd = STDIN_FILENO, .events = POLLIN };
    int rv = poll(&fd, 1, 0) > 0 ? 1 : 0;
    SystemNative_UninitializeConsoleAfterRead();
    return rv;
}

extern "C" int32_t SystemNative_ReadStdin(void* buffer, int32_t bufferSize)
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

extern "C" int32_t SystemNative_GetSignalForBreak()
{
    return g_signalForBreak;
}

extern "C" int32_t SystemNative_SetSignalForBreak(int32_t signalForBreak)
{
    assert(signalForBreak == 0 || signalForBreak == 1);

    struct termios current;
    if (tcgetattr(STDIN_FILENO, &current) >= 0)
    {
        IncorporateBreak(&current, signalForBreak);
        if (tcsetattr(STDIN_FILENO, TCSANOW, &current) >= 0)
        {
            g_signalForBreak = signalForBreak;
            return 1;
        }
    }

    return 0;
}

static struct sigaction g_origSigIntHandler, g_origSigQuitHandler; // saved signal handlers for ctrl handling
static struct sigaction g_origSigContHandler, g_origSigChldHandler; // saved signal handlers for reinitialization
static volatile CtrlCallback g_ctrlCallback = nullptr; // Callback invoked for SIGINT/SIGQUIT
static int g_signalPipe[2] = {-1, -1}; // Pipe used between signal handler and worker

// Signal handler for signals where we want our background thread to do the real processing.
// It simply writes the signal code to a pipe that's read by the thread.
static void TransferSignalToHandlerLoop(int sig, siginfo_t* siginfo, void* context)
{
    (void)siginfo; // unused
    (void)context; // unused

    // Write the signal code to the pipe
    uint8_t signalCodeByte = static_cast<uint8_t>(sig);
    ssize_t writtenBytes;
    while (CheckInterrupted(writtenBytes = write(g_signalPipe[1], &signalCodeByte, 1)));

    if (writtenBytes != 1)
    {
        abort(); // fatal error
    }
}

static void HandleSignalForReinitialize(int sig, siginfo_t* siginfo, void* context)
{
    // SIGCONT will be sent when we're resumed after suspension, at which point
    // we need to set the terminal back up.  Similarly, SIGCHLD will be sent after
    // a child process completes, and that child could have left things in a bad state,
    // so we similarly need to reinitialize.
    assert(sig == SIGCONT || sig == SIGCHLD);

    // If the process was suspended while reading, we need to
    // re-initialize the console for the read, as the attributes
    // previously set were likely overwritten.
    if (g_readInProgress)
    {
        IncorporateBreak(&g_currTermios, g_signalForBreak);
        tcsetattr(STDIN_FILENO, TCSANOW, &g_currTermios);
    }

    // "Application mode" will also have been reset and needs to be redone.
    WriteKeypadXmit();

    // Delegate to any saved handler we may have
    struct sigaction origHandler = sig == SIGCONT ? g_origSigContHandler : g_origSigChldHandler;
    if (origHandler.sa_sigaction != nullptr &&
        reinterpret_cast<void*>(origHandler.sa_sigaction) != reinterpret_cast<void*>(SIG_DFL) &&
        reinterpret_cast<void*>(origHandler.sa_sigaction) != reinterpret_cast<void*>(SIG_IGN))
    {
        origHandler.sa_sigaction(sig, siginfo, context);
    }
}

// Entrypoint for the thread that handles signals where our handling
// isn't signal-safe.  Those signal handlers write the signal to a pipe,
// which this loop reads and processes.
void* SignalHandlerLoop(void* arg)
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
        while (CheckInterrupted(bytesRead = read(pipeFd, &signalCode, 1)));

        if (bytesRead <= 0)
        {
            // Write end of pipe was closed or another error occurred.
            // Regardless, no more data is available, so we close the read
            // end of the pipe and exit.
            close(pipeFd);
            return nullptr;
        }

        assert_msg(signalCode == SIGQUIT || signalCode == SIGINT, "invalid signalCode", static_cast<int>(signalCode));

        // We're now handling SIGQUIT and SIGINT. Invoke the callback, if we have one.
        CtrlCallback callback = g_ctrlCallback;
        int rv = callback != nullptr ? callback(signalCode == SIGQUIT ? Break : Interrupt) : 0;
        if (rv == 0) // callback removed or was invoked and didn't handle the signal
        {
            // In general, we now want to remove our handler and reissue the signal to
            // be picked up by the previously registered handler.  In the most common case,
            // this will be the default handler, causing the process to be torn down.
            // It could also be a custom handle registered by other code before us.

            if (signalCode == SIGINT)
            {
                UninitializeConsole();
                sigaction(SIGINT, &g_origSigIntHandler, NULL);
                kill(getpid(), SIGINT);
            } 
            else if (signalCode == SIGQUIT)
            {
                UninitializeConsole();
                sigaction(SIGQUIT, &g_origSigQuitHandler, NULL);
                kill(getpid(), SIGQUIT);
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
    if (pthread_create(&handlerThread, nullptr, SignalHandlerLoop, readFdPtr) != 0)
    {
        int err = errno;
        free(readFdPtr);
        CloseSignalHandlingPipe();
        errno = err;
        return false;
    }

    // Finally, register our signal handlers
    struct sigaction newAction;
    memset(&newAction, 0, sizeof(struct sigaction));
    newAction.sa_flags = SA_RESTART | SA_SIGINFO;
    
    sigemptyset(&newAction.sa_mask);
    int rv;

    // Hook up signal handlers for use with ctrl-C / ctrl-Break handling
    // We don't handle ignored signals. If we'd setup a handler, our child processes
    // would reset to the default on exec causing them to terminate on these signals.
    newAction.sa_sigaction = &TransferSignalToHandlerLoop;
    rv = sigaction(SIGINT, NULL, &g_origSigIntHandler);
    assert(rv == 0);
    if (reinterpret_cast<void*>(g_origSigIntHandler.sa_sigaction) != reinterpret_cast<void*>(SIG_IGN))
    {
        rv = sigaction(SIGINT, &newAction, NULL);
        assert(rv == 0);
    }
    rv = sigaction(SIGQUIT, NULL, &g_origSigQuitHandler);
    assert(rv == 0);
    if (reinterpret_cast<void*>(g_origSigQuitHandler.sa_sigaction) != reinterpret_cast<void*>(SIG_IGN))
    {
        rv = sigaction(SIGQUIT, &newAction, NULL);
        assert(rv == 0);
    }

    // Hook up signal handlers for use with signals that require us to reinitialize the terminal
    newAction.sa_sigaction = &HandleSignalForReinitialize;
    rv = sigaction(SIGCONT, &newAction, &g_origSigContHandler);
    assert(rv == 0);
    rv = sigaction(SIGCHLD, &newAction, &g_origSigChldHandler);
    assert(rv == 0);

    return true;
}

extern "C" void SystemNative_RegisterForCtrl(CtrlCallback callback)
{
    assert(callback != nullptr);
    assert(g_ctrlCallback == nullptr);
    g_ctrlCallback = callback;
}

extern "C" void SystemNative_UnregisterForCtrl()
{
    assert(g_ctrlCallback != nullptr);
    g_ctrlCallback = nullptr;
}

extern "C" int32_t SystemNative_InitializeConsole()
{
    if (tcgetattr(STDIN_FILENO, &g_initTermios) >= 0)
    {
        g_haveInitTermios = true;
        g_signalForBreak = (g_initTermios.c_lflag & ISIG) != 0;
    }
    else
    {
        g_haveInitTermios = false;
        g_signalForBreak = true;
    }
    atexit(UninitializeConsole);

    // Do all initialization needed for the console.  Right now that's just
    // initializing the signal handling thread.
    return InitializeSignalHandling() ? 1 : 0;
}
