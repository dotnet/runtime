// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "PalCreateDump.h"

// WebAssembly does not support crash dumps.
void PalCreateCrashDumpIfEnabled()
{
}

void PalCreateCrashDumpIfEnabled(void* pExceptionRecord)
{
}

bool PalCreateDumpInitialize()
{
    return true;
}
