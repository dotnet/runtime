// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_compiler.h"
#include "pal_types.h"

/**
 * Initializes the signal handling, called by InitializeTerminalAndSignalHandling.
 *
 * Returns 1 on success; otherwise returns 0 and sets errno.
 */
int32_t InitializeSignalHandlingCore(void);

/**
 * Hooks up the specified callback for notifications when SIGINT or SIGQUIT is received.
 *
 * Not thread safe.  Caller must provide its owns synchronization to ensure RegisterForCtrl
 * is not called concurrently with itself or with UnregisterForCtrl.
 *
 * Should only be called when a callback is not currently registered.
 */
DLLEXPORT void SystemNative_RegisterForCtrl(CtrlCallback callback);

/**
 * Unregisters the previously registered ctrlCCallback.
 *
 * Not thread safe.  Caller must provide its owns synchronization to ensure UnregisterForCtrl
 * is not called concurrently with itself or with RegisterForCtrl.
 *
 * Should only be called when a callback is currently registered. The pointer
 * previously registered must remain valid until all ctrl handling activity
 * has quiesced.
 */
DLLEXPORT void SystemNative_UnregisterForCtrl(void);

typedef void (*SigChldCallback)(int reapAll);

/**
 * Hooks up the specified callback for notifications when SIGCHLD is received.
 *
 * Should only be called when a callback is not currently registered.
 */
DLLEXPORT void SystemNative_RegisterForSigChld(SigChldCallback callback);

/**
 * Remove our handler and reissue the signal to be picked up by the previously registered handler.
 *
 * In the most common case, this will be the default handler, causing the process to be torn down.
 * It could also be a custom handler registered by other code before us.
 */
DLLEXPORT void SystemNative_RestoreAndHandleCtrl(CtrlCode ctrlCode);

typedef void (*TerminalInvalidationCallback)(void);

/**
 * Hooks up the specified callback for notifications when SIGCHLD, SIGCONT, SIGWINCH are received.
  *
 */
DLLEXPORT void SystemNative_SetTerminalInvalidationHandler(TerminalInvalidationCallback callback);
