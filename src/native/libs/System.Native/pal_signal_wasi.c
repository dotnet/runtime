// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_console.h"
#include "pal_signal.h"
#include "pal_io.h"
#include "pal_utilities.h"

#include <assert.h>
#include <errno.h>
#include <signal.h>
#include <stdlib.h>
#include <sys/types.h>
#include <unistd.h>

__attribute__((noreturn))
void SystemNative_SetPosixSignalHandler(PosixSignalHandler signalHandler)
{
    assert(signalHandler);
    (void)signalHandler;
    assert_msg(false, "Not supported on WASI", 0);
}

__attribute__((noreturn))
void SystemNative_HandleNonCanceledPosixSignal(int32_t signalCode)
{
    (void)signalCode;
    assert_msg(false, "Not supported on WASI", 0);
}


__attribute__((noreturn))
void SystemNative_SetTerminalInvalidationHandler(TerminalInvalidationCallback callback)
{
    assert(callback != NULL);
    assert_msg(false, "Not supported on WASI", 0);
    (void)callback;
}

__attribute__((noreturn))
void SystemNative_RegisterForSigChld(SigChldCallback callback)
{
    assert(callback != NULL);
    assert_msg(false, "Not supported on WASI", 0);
    (void)callback;
}

__attribute__((noreturn))
void SystemNative_SetDelayedSigChildConsoleConfigurationHandler(void (*callback)(void))
{
    assert(callback == NULL);
    assert_msg(false, "Not supported on WASI", 0);
    (void)callback;
}

int32_t SystemNative_EnablePosixSignalHandling(int signalCode)
{
    (void)signalCode;
    return false;
}

void SystemNative_DisablePosixSignalHandling(int signalCode)
{
    (void)signalCode;
}
