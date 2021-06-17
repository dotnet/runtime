// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_types.h"

/**
 * Initializes the signal handling, called by InitializeTerminalAndSignalHandling.
 *
 * Returns 1 on success; otherwise returns 0 and sets errno.
 */
int32_t InitializeSignalHandlingCore(void);

typedef void (*SigChldCallback)(int reapAll);

/**
 * Hooks up the specified callback for notifications when SIGCHLD is received.
 *
 * Should only be called when a callback is not currently registered.
 */
PALEXPORT void SystemNative_RegisterForSigChld(SigChldCallback callback);

typedef void (*TerminalInvalidationCallback)(void);

/**
 * Hooks up the specified callback for notifications when SIGCHLD, SIGCONT, SIGWINCH are received.
  *
 */
PALEXPORT void SystemNative_SetTerminalInvalidationHandler(TerminalInvalidationCallback callback);

typedef enum
{
    PosixSignalSIGHUP = -1,
    PosixSignalSIGINT = -2,
    PosixSignalSIGQUIT = -3,
    PosixSignalSIGTERM = -4,
    PosixSignalSIGCHLD = -5,
    PosixSignalSIGWINCH = -6,
    PosixSignalSIGCONT = -7,
    PosixSignalSIGTTIN = -8,
    PosixSignalSIGTTOU = -9,
    PosixSignalSIGTSTP = -10
} PosixSignal;

typedef int32_t (*PosixSignalHandler)(int32_t signalCode, PosixSignal signal);

PALEXPORT void SystemNative_SetPosixSignalHandler(PosixSignalHandler signalHandler);
PALEXPORT int32_t SystemNative_GetPlatformSignalNumber(PosixSignal signal);
PALEXPORT void SystemNative_EnablePosixSignalHandling(int signalCode);
PALEXPORT void SystemNative_DisablePosixSignalHandling(int signalCode);
PALEXPORT void SystemNative_DefaultSignalHandler(int signalCode);

typedef void (*ConsoleSigTtouHandler)(void);

void InstallTTOUHandlerForConsole(ConsoleSigTtouHandler handler);
void UninstallTTOUHandlerForConsole(void);

#ifndef HAS_CONSOLE_SIGNALS

/**
 * Initializes signal handling and terminal for use by System.Console and System.Diagnostics.Process.
 *
 * Returns 1 on success; otherwise returns 0 and sets errno.
 */
PALEXPORT int32_t SystemNative_InitializeTerminalAndSignalHandling(void);

#endif
