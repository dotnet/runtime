// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_compiler.h"

BEGIN_EXTERN_C

#include "pal_types.h"

/**
 * Initializes the signal handling for use by System.Console and System.Process.
 *
 * Returns 1 on success; otherwise returns 0 and sets errno.
 */
uint32_t InitializeSignalHandling(void);

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
 * Not thread safe.  Caller must provide its owns synchronization to ensure RegisterForSigChld
 * is not called concurrently with itself.
 *
 * Should only be called when a callback is not currently registered.
 */
DLLEXPORT uint32_t SystemNative_RegisterForSigChld(SigChldCallback callback);

END_EXTERN_C
