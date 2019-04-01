// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_compiler.h"
#include "pal_types.h"

/**
* Constants for terminal control codes.
*/
enum
{
    PAL_VINTR = 0,
    PAL_VQUIT = 1,
    PAL_VERASE = 2,
    PAL_VKILL = 3,
    PAL_VEOF = 4,
    PAL_VTIME = 5,
    PAL_VMIN = 6,
    PAL_VSWTC = 7,
    PAL_VSTART = 8,
    PAL_VSTOP = 9,
    PAL_VSUSP = 10,
    PAL_VEOL = 11,
    PAL_VREPRINT = 12,
    PAL_VDISCARD = 13,
    PAL_VWERASE = 14,
    PAL_VLNEXT = 15,
    PAL_VEOL2 = 16
};

/*
 * Window Size of the terminal
 */
typedef struct
{
    uint16_t Row;
    uint16_t Col;
    uint16_t XPixel;
    uint16_t YPixel;
} WinSize;

/**
 * Gets the windows size of the terminal
 *
 * Returns 0 on success; otherwise, returns errorNo.
 */
DLLEXPORT int32_t SystemNative_GetWindowSize(WinSize* windowsSize);

/**
 * Gets whether the specified file descriptor is for a terminal.
 *
 * Returns 1 if the file descriptor is referring to a terminal;
 * otherwise returns 0 and sets errno.
 */
DLLEXPORT int32_t SystemNative_IsATty(intptr_t fd);

/**
 * Initializes signal handling and terminal for use by System.Console and System.Diagnostics.Process.
 *
 * Returns 1 on success; otherwise returns 0 and sets errno.
 */
DLLEXPORT int32_t SystemNative_InitializeTerminalAndSignalHandling(void);

/**
 * Stores the string that can be written to stdout to transition
 * into "application mode".
 *
 * Returns 1 on success; otherwise returns 0 and sets errno.
 */
DLLEXPORT void SystemNative_SetKeypadXmit(const char* terminfoString);

/**
 * Gets the special control character codes for the requested control characters.
 *
 * controlCharacterLength is the length of the input controlCharacterNames array and the output controlCharacterValues array.
 * The controlCharacterValues array is filled with the control codes for the corresponding control character names,
 * or 0 if a particular name is unsupported or disabled. posixDisableValue is the special sentinel used in the output
 * controlCharacterValues array to indicate no value is available.
 */
DLLEXPORT void SystemNative_GetControlCharacters(
    int32_t* controlCharacterNames, uint8_t* controlCharacterValues, int32_t controlCharacterLength,
    uint8_t* posixDisableValue);

/**
 * Returns 1 if any input is waiting on stdin; otherwise, 0.
 */
DLLEXPORT int32_t SystemNative_StdinReady(void);

/**
 * Configures the terminal for System.Console Read.
 */
DLLEXPORT void SystemNative_InitializeConsoleBeforeRead(uint8_t minChars, uint8_t decisecondsTimeout);

/**
 * Configures the terminal after System.Console Read.
 */
DLLEXPORT void SystemNative_UninitializeConsoleAfterRead(void);

/**
 * Configures the terminal for child processes.
 */
DLLEXPORT void SystemNative_ConfigureTerminalForChildProcess(int32_t enable);

/**
 * Reads the number of bytes specified into the provided buffer from stdin.
 * Returns the number of bytes read on success; otherwise, -1 is returned an errno is set.
 */
DLLEXPORT int32_t SystemNative_ReadStdin(void* buffer, int32_t bufferSize);

/**
 * Gets the terminal's break mode.
 */
DLLEXPORT int32_t SystemNative_GetSignalForBreak(void);

/**
 * Configures the terminal's break mode.
 *
 * signalForBreak should be 1 to treat break as signals, or 0 to treat break as input.
 *
 * Returns 1 on success, 0 on failure, in which case errno is set.
 */
DLLEXPORT int32_t SystemNative_SetSignalForBreak(int32_t signalForBreak);

typedef enum
{
    Interrupt = 0,
    Break = 1
} CtrlCode;

typedef void (*CtrlCallback)(CtrlCode signalCode);
/**
 * Called by pal_signal.cpp to reinitialize the console on SIGCONT/SIGCHLD.
 */
void ReinitializeTerminal(void);

/**
 * Called by pal_signal.cpp to uninitialize the console on SIGINT/SIGQUIT.
 */
void UninitializeTerminal(void);
