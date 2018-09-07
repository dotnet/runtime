// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <windows.h>

// Entrypoint jumped to by IJW dlls when their dllmain is called
extern "C" __declspec(dllexport) BOOL WINAPI _CorDllMain(HINSTANCE hInst, DWORD dwReason, LPVOID lpReserved)
{
    return TRUE;
}
