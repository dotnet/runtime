// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: delayloadhook.cpp
//

#include "stdafx.h"

#include <delayimp.h>

FARPROC WINAPI secureDelayHook(unsigned dliNotify, PDelayLoadInfo pdli)
{
    if (dliNotify == dliNotePreLoadLibrary)
    {
        // Use a safe search path to avoid delay load dll hijacking
        return (FARPROC)::LoadLibraryExA(pdli->szDll, NULL, LOAD_LIBRARY_SEARCH_SYSTEM32);
    }

    return nullptr;
}

// See https://docs.microsoft.com/en-us/cpp/build/reference/notification-hooks
// This global hook is called prior to all the delay load LoadLibrary/GetProcAddress/etc. calls
// Hooking this callback allows us to ensure that delay load LoadLibrary calls
// specify the LOAD_LIBRARY_SEARCH_SYSTEM32 search path
const PfnDliHook __pfnDliNotifyHook2 = secureDelayHook;
