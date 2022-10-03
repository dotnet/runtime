// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
#include <pthread.h>
#include <signal.h>

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
    memset(windowSize, 0, sizeof(WinSize)); // managed out param must be initialized
    errno = ENOTSUP;
    return -1;
#endif
}

int32_t SystemNative_IsATty(intptr_t fd)
{
    return isatty(ToFileDescriptor(fd));
}

static char* g_keypadXmit = NULL; // string used to enable application mode, from terminfo

static void WriteKeypadXmit()
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

static pthread_mutex_t g_lock = PTHREAD_MUTEX_INITIALIZER; // prevents races when initializing and changing the terminal.

static bool g_signalForBreak = true;          // tracks whether the terminal should send signals for breaks, such that attributes have been changed

static struct termios g_initTermios;          // the initial attributes captured

static bool g_hasCurrentTermios = false;      // tracks whether g_currentTermios is valid
static struct termios g_currentTermios;       // the latest attributes set

// The terminal can be used by the .NET application via the Console class.
// It may also be used by child processes that are started via the Process class.
// The terminal needs to be configured differently depending on the user.
// ConfigureTerminalForXXX are called to change the configuration.
// When it is ambiguous whether we should configure for Console/a child Process,
// we prefer configuring for the Console.
static bool g_reading = false;                // tracks whether the application is performing a Console.Read operation
static bool g_childUsesTerminal = false;      // tracks whether a child process is using the terminal
static bool g_terminalUninitialized = false;  // tracks whether the application is terminating
static bool g_terminalConfigured = false;     // tracks whether the application configured the terminal.

static bool g_hasTty = false;                  // cache we are not a tty

static volatile bool g_receivedSigTtou = false;

static void ttou_handler()
{
    g_receivedSigTtou = true;
}

static bool TcSetAttr(struct termios* termios, bool blockIfBackground)
{
    if (g_terminalUninitialized)
    {
        // The application is exiting, we mustn't change terminal settings.
        return true;
    }

    if (!blockIfBackground)
    {
        // When the process is running in background, changing terminal settings
        // will stop it (default SIGTTOU action).
        // We change SIGTTOU's disposition to get EINTR instead.
        // This thread may be used to run a signal handler, which may write to
        // stdout. We set SA_RESETHAND to avoid that handler's write loops infinitely
        // on EINTR when the process is running in background and the terminal
        // configured with TOSTOP.
        InstallTTOUHandlerForConsole(ttou_handler);

        g_receivedSigTtou = false;
    }

    bool rv = tcsetattr(STDIN_FILENO, TCSANOW, termios) >= 0;

    if (!blockIfBackground)
    {
        if (!rv && errno == EINTR && g_receivedSigTtou)
        {
            // Operation failed because we are background
            // pretend it went fine.
            rv = true;
        }

        UninstallTTOUHandlerForConsole();
    }

    // On success, update the cached value.
    if (rv)
    {
        g_terminalConfigured = true;
        g_hasCurrentTermios = true;
        g_currentTermios = *termios;
    }

    return rv;
}

static bool ConfigureTerminal(bool signalForBreak, bool forChild, uint8_t minChars, uint8_t decisecondsTimeout, bool blockIfBackground, bool distinguishNewLines)
{
    if (!g_hasTty)
    {
        errno = ENOTTY;
        return false;
    }

    g_childUsesTerminal = forChild;

    struct termios termios = g_initTermios;

    if (signalForBreak)
        termios.c_lflag |= (uint32_t)ISIG;
    else
        termios.c_lflag &= (uint32_t)(~ISIG);

    if (!forChild)
    {
        if (distinguishNewLines)
        {
            termios.c_iflag &= (uint32_t)(~(IXON | IXOFF | ICRNL | INLCR | IGNCR));
        }
        else
        {
            termios.c_iflag &= (uint32_t)(~(IXON | IXOFF));
        }

        termios.c_lflag &= (uint32_t)(~(ECHO | ICANON | IEXTEN));
    }

    termios.c_cc[VMIN] = minChars;
    termios.c_cc[VTIME] = decisecondsTimeout;

    // Check if the settings have changed.
    if (g_hasCurrentTermios)
    {
        if (g_currentTermios.c_lflag == termios.c_lflag &&
            g_currentTermios.c_iflag == termios.c_iflag &&
            g_currentTermios.c_cc[VMIN] == termios.c_cc[VMIN] &&
            g_currentTermios.c_cc[VTIME] == termios.c_cc[VTIME])
        {
            return true;
        }
    }

    return TcSetAttr(&termios, blockIfBackground);
}

void UninitializeTerminal()
{
    // This method is called on SIGQUIT/SIGINT from the signal dispatching thread
    // and on atexit.

    if (pthread_mutex_lock(&g_lock) == 0)
    {
        if (!g_terminalUninitialized)
        {
            // Avoid configuring the terminal: only reset terminal settings when our process has changed them.
            if (g_terminalConfigured)
            {
                TcSetAttr(&g_initTermios, /* blockIfBackground */ false);
            }

            g_terminalUninitialized = true;
        }

        pthread_mutex_unlock(&g_lock);
    }
}

void SystemNative_InitializeConsoleBeforeRead(int32_t distinguishNewLines, uint8_t minChars, uint8_t decisecondsTimeout)
{
    if (pthread_mutex_lock(&g_lock) == 0)
    {
        g_reading = true;

        ConfigureTerminal(g_signalForBreak, /* forChild */ false, minChars, decisecondsTimeout, /* blockIfBackground */ true, distinguishNewLines);

        pthread_mutex_unlock(&g_lock);
    }
}

void SystemNative_UninitializeConsoleAfterRead()
{
    if (pthread_mutex_lock(&g_lock) == 0)
    {
        g_reading = false;

        pthread_mutex_unlock(&g_lock);
    }
}

void SystemNative_ConfigureTerminalForChildProcess(int32_t childUsesTerminal)
{
    assert(childUsesTerminal == 0 || childUsesTerminal == 1);

    if (pthread_mutex_lock(&g_lock) == 0)
    {
        // If the application is performing a read, assume the child process won't use the terminal.
        if (g_reading)
        {
            pthread_mutex_unlock(&g_lock);
            return;
        }

        // If no more children are using the terminal, invalidate our cached termios.
        if (!childUsesTerminal)
        {
            g_hasCurrentTermios = false;
        }

        // Avoid configuring the terminal: only change terminal settings when our process has changed them.
        if (g_terminalConfigured)
        {
            ConfigureTerminal(g_signalForBreak, /* forChild */ childUsesTerminal, /* minChars */ 1, /* decisecondsTimeout */ 0, /* blockIfBackground */ false, /* distinguishNewLines */ false);
        }

        // Redo "Application mode" when there are no more children using the terminal.
        if (!childUsesTerminal)
        {
            WriteKeypadXmit();
        }

        pthread_mutex_unlock(&g_lock);
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

int32_t SystemNative_StdinReady(int32_t distinguishNewLines)
{
    SystemNative_InitializeConsoleBeforeRead(distinguishNewLines, /* minChars */ 1, /* decisecondsTimeout */ 0);
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

int32_t SystemNative_SetSignalForBreak(int32_t signalForBreak, int32_t distinguishNewLines)
{
    assert(signalForBreak == 0 || signalForBreak == 1);

    int rv = 0;

    if (pthread_mutex_lock(&g_lock) == 0)
    {
        if (ConfigureTerminal(signalForBreak, /* forChild */ false, /* minChars */ 1, /* decisecondsTimeout */ 0, /* blockIfBackground */ true, distinguishNewLines))
        {
            g_signalForBreak = signalForBreak;
            rv = 1;
        }

        pthread_mutex_unlock(&g_lock);
    }

    return rv;
}

void ReinitializeTerminal()
{
    // Restores the state of the terminal after being suspended.
    // pal_signal.cpp calls this on SIGCONT from the signal handling thread.

    if (pthread_mutex_lock(&g_lock) == 0)
    {
        if (!g_childUsesTerminal)
        {
            if (g_hasCurrentTermios)
            {
                TcSetAttr(&g_currentTermios, /* blockIfBackground */ false);
            }

            WriteKeypadXmit();
        }

        pthread_mutex_unlock(&g_lock);
    }
}

static void InitializeTerminalCore()
{
    bool haveInitTermios = tcgetattr(STDIN_FILENO, &g_initTermios) >= 0;

    if (haveInitTermios)
    {
        g_hasTty = true;
        g_hasCurrentTermios = true;
        g_currentTermios = g_initTermios;
        g_signalForBreak = g_initTermios.c_lflag & (uint32_t)ISIG;

        atexit(UninitializeTerminal);
    }
    else
    {
        g_signalForBreak = true;
    }
}

int32_t SystemNative_InitializeTerminalAndSignalHandling()
{
    static int32_t initialized = 0;

    // The Process, Console and PosixSignalRegistration classes call this method for initialization.
    if (pthread_mutex_lock(&g_lock) == 0)
    {
        if (initialized == 0)
        {
            InitializeTerminalCore();
            initialized = InitializeSignalHandlingCore();
        }
        pthread_mutex_unlock(&g_lock);
    }

    return initialized;
}
