// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <windows.h>

// No-op DllMain. The purpose is to reserve DllMain for our purposes.
//
// Some users also try to define a DllMain in managed code using an UnmanagedCallersOnly
// attribute. This will not work correctly because the entire runtime initialization would run
// as part of such DllMain, before the first line of users DllMain executes. We don't support
// initializing the runtime under loader lock.
//
BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
    return TRUE;
}
