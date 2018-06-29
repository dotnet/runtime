// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_config.h"
#include "pal_console.h"
#include "pal_utilities.h"
#include "pal_signal.h"

#include <assert.h>
#include <errno.h>
#include <fcntl.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/ioctl.h>
#include <termios.h>
#include <unistd.h>
#include <poll.h>

int32_t SystemNative_GetWindowSize(WinSize* windowSize)
{
    assert(windowSize != NULL);

#if HAVE_IOCTL && HAVE_TIOCGWINSZ
    int error = ioctl(STDOUT_FILENO, TIOCGWINSZ, windowSize);

    if (error != 0)
    {
        memset(windowSize, 0, sizeof(WinSize)); // managed out param must be initialized
    }

    return error;
#else
    errno = ENOTSUP;
    return -1;
#endif
}

int32_t SystemNative_IsATty(intptr_t fd)
{
    return isatty(ToFileDescriptor(fd));
}

static char* g_keypadXmit = NULL; // string used to enable application mode, from terminfo

static void WriteKeypadXmit() // used in a signal handler, must be signal-safe
{
    // If a terminfo "application mode" keypad_xmit string has been supplied,
    // write it out to the terminal to enter the mode.
    if (g_keypadXmit != NULL)
    {
        ssize_t ret;
        while (CheckInterrupted(ret = write(STDOUT_FILENO, g_keypadXmit, (size_t)(sizeof(char) * strlen(g_keypadXmit)))));
        assert(ret >= 0); // failure to change the mode should not prevent app from continuing
    }
}

void SystemNative_SetKeypadXmit(const char* terminfoString)
{
    assert(terminfoString != NULL);

    if (g_keypadXmit != NULL) // should only happen if initialization initially failed
    {
        free(g_keypadXmit);
        assert(false && "g_keypadXmit was already initialized");
    }

    // Store the string to use to enter application mode, then enter
    g_keypadXmit = strdup(terminfoString);
    WriteKeypadXmit();
}

static bool g_readInProgress = false;   // tracks whether a read is currently in progress, such that attributes have been changed
static bool g_signalForBreak = true;    // tracks whether the terminal should send signals for breaks, such that attributes have been changed
static bool g_haveInitTermios = false;  // whether g_initTermios has been initialized
static struct termios g_initTermios;    // the initial attributes captured when Console was initialized
static struct termios g_preReadTermios; // the original attributes captured before a read; valid if g_readInProgress is true
static struct termios g_currTermios;    // the current attributes set during a read; valid if g_readInProgress is true

void UninitializeConsole()
{
    // pal_signal.cpp calls this on SIGQUIT/SIGINT.
    // This can happen when SystemNative_InitializeConsole was not called.

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
    assert(termios != NULL);
    assert(signalForBreak == 0 || signalForBreak == 1);

    if (signalForBreak)
        termios->c_lflag |= (uint32_t)ISIG;
    else
        termios->c_lflag &= (uint32_t)(~ISIG);
}

// In order to support Console.ReadKey(intercept: true), we need to disable echo and canonical mode.
// We have two main choices: do so for the entire app, or do so only while in the Console.ReadKey(true).
// The former has a huge downside: the terminal is in a non-echo state, so anything else that runs
// in the same terminal won't echo even if it expects to, e.g. using Process.Start to launch an interactive,
// program, or P/Invoking to a native library that reads from stdin rather than using Console.  The second
// also has a downside, in that any typing which occurs prior to invoking Console.ReadKey(true) will
// be visible even though it wasn't supposed to be.  The downsides of the former approach are so large
// and the cons of the latter minimal and constrained to the one API that we've chosen the second approach.
// Thus, InitializeConsoleBeforeRead is called to set up the state of the console, then a read is done,
// and then UninitializeConsoleAfterRead is called.
void SystemNative_InitializeConsoleBeforeRead(uint8_t minChars, uint8_t decisecondsTimeout)
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

        newTermios.c_iflag &= (uint32_t)(~(IXON | IXOFF));
        newTermios.c_lflag &= (uint32_t)(~(ECHO | ICANON | IEXTEN));
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

void SystemNative_UninitializeConsoleAfterRead()
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

void SystemNative_GetControlCharacters(
    int32_t* controlCharacterNames, uint8_t* controlCharacterValues, int32_t controlCharacterLength,
    uint8_t* posixDisableValue)
{
    assert(controlCharacterNames != NULL);
    assert(controlCharacterValues != NULL);
    assert(controlCharacterLength >= 0);
    assert(posixDisableValue != NULL);

#ifdef _POSIX_VDISABLE
    *posixDisableValue = _POSIX_VDISABLE;
#else
    *posixDisableValue = 0;
#endif

    memset(controlCharacterValues, *posixDisableValue, sizeof(uint8_t) * Int32ToSizeT(controlCharacterLength));

    if (controlCharacterLength > 0)
    {
        struct termios newTermios;
        memset(&newTermios, 0, sizeof(struct termios));

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

int32_t SystemNative_StdinReady()
{
    SystemNative_InitializeConsoleBeforeRead(1, 0);
    struct pollfd fd = { .fd = STDIN_FILENO, .events = POLLIN };
    int rv = poll(&fd, 1, 0) > 0 ? 1 : 0;
    SystemNative_UninitializeConsoleAfterRead();
    return rv;
}

int32_t SystemNative_ReadStdin(void* buffer, int32_t bufferSize)
{
    assert(buffer != NULL || bufferSize == 0);
    assert(bufferSize >= 0);

     if (bufferSize < 0)
    {
        errno = EINVAL;
        return -1;
    }

    ssize_t count;
    while (CheckInterrupted(count = read(STDIN_FILENO, buffer, Int32ToSizeT(bufferSize))));
    return (int32_t)count;
}

int32_t SystemNative_GetSignalForBreak()
{
    return g_signalForBreak;
}

int32_t SystemNative_SetSignalForBreak(int32_t signalForBreak)
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

void ReinitializeConsole()
{
    // pal_signal.cpp calls this on SIGCONT/SIGCHLD.
    // This can happen when SystemNative_InitializeConsole was not called.
    // This gets called on a signal handler, we may only use async-signal-safe functions.

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
}

int32_t SystemNative_InitializeConsole()
{
    if (!InitializeSignalHandling())
    {
        return 0;
    }

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

    return 1;
}
