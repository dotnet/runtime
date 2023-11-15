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
#include <unistd.h>
#include <poll.h>
#include <signal.h>

#ifdef DEBUG
#define DEBUGNOTRETURN __attribute__((noreturn))
#else
#define DEBUGNOTRETURN
#endif

int32_t SystemNative_GetWindowSize(intptr_t fd, WinSize* windowSize)
{
    (void)fd;
    assert(windowSize != NULL);
    memset(windowSize, 0, sizeof(WinSize)); // managed out param must be initialized
    errno = ENOTSUP;
    return -1;
}

int32_t SystemNative_SetWindowSize(WinSize* windowSize)
{
    assert(windowSize != NULL);
    errno = ENOTSUP;
    return -1;
}

int32_t SystemNative_IsATty(intptr_t fd)
{
    return isatty(ToFileDescriptor(fd));
}

DEBUGNOTRETURN
void SystemNative_SetKeypadXmit(intptr_t fd, const char* terminfoString)
{
    (void)fd;
    assert(terminfoString != NULL);
    assert_msg(false, "Not supported on WASI", 0);
}

DEBUGNOTRETURN
void SystemNative_InitializeConsoleBeforeRead(uint8_t minChars, uint8_t decisecondsTimeout)
{
    assert_msg(false, "Not supported on WASI", 0);
}

DEBUGNOTRETURN
void SystemNative_UninitializeConsoleAfterRead(void)
{
    assert_msg(false, "Not supported on WASI", 0);
}

DEBUGNOTRETURN
void SystemNative_ConfigureTerminalForChildProcess(int32_t childUsesTerminal)
{
    assert_msg(false, "Not supported on WASI", 0);
}

DEBUGNOTRETURN
void SystemNative_GetControlCharacters(
    int32_t* controlCharacterNames, uint8_t* controlCharacterValues, int32_t controlCharacterLength,
    uint8_t* posixDisableValue)
{
    assert(controlCharacterNames != NULL);
    assert(controlCharacterValues != NULL);
    assert(controlCharacterLength >= 0);
    assert(posixDisableValue != NULL);

    assert_msg(false, "Not supported on WASI", 0);
}

int32_t SystemNative_StdinReady(void)
{
    struct pollfd fd = { .fd = STDIN_FILENO, .events = POLLIN };
    int rv = poll(&fd, 1, 0) > 0 ? 1 : 0;
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

int32_t SystemNative_GetSignalForBreak(void)
{
    return false;
}

int32_t SystemNative_SetSignalForBreak(int32_t signalForBreak)
{
    assert(signalForBreak == 0 || signalForBreak == 1);
    assert_msg(false, "Not supported on WASI", 0);
    return -1;
}



int32_t SystemNative_InitializeTerminalAndSignalHandling(void)
{
    return true;
}
