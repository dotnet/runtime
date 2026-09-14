// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"

#define INITGUID
#include <guiddef.h>

DEFINE_GUID(IID_IUnknown, 0x00000000, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);

bool g_initialized = false;

bool
InitializePAL()
{
    if (g_initialized)
    {
        return true;
    }
    g_initialized = true;

    if (PAL_InitializeDLL() != 0)
    {
        printf_error("InitializePAL: PAL initialization FAILED\n");
        return false;
    }
    return true;
}

void
UninitializePAL(
    int exitCode)
{
    if (g_initialized)
    {
        PAL_TerminateEx(exitCode);
    }
}
