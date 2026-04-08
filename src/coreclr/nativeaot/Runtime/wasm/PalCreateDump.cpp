// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// We do not support crash dumps in Wasm yet, so just stub.

#include "PalCreateDump.h"

/*++
Function:
  PalCreateCrashDumpIfEnabled

  Creates crash dump of the process (if enabled). Can be called from the unhandled native exception handler.

Parameters:
    signal - POSIX signal number or 0
    siginfo - signal info or nullptr
    exceptionRecord - address of exception record or nullptr

(no return value)
--*/
void
PalCreateCrashDumpIfEnabled(int signal, siginfo_t* siginfo, void* exceptionRecord)
{
}

void
PalCreateCrashDumpIfEnabled()
{
}

void
PalCreateCrashDumpIfEnabled(void* pExceptionRecord)
{
}

/*++
Function
  PalCreateDumpInitialize()

Abstract
  Initialize the process abort crash dump program file path and
  name. Doing all of this ahead of time so nothing is allocated
  or copied in abort/signal handler.

Return
  true - succeeds, false - fails

--*/
bool
PalCreateDumpInitialize()
{
    return true;
}
