// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <windows.h>

#ifndef DLLEXPORT
#ifdef _MSC_VER
#define DLLEXPORT __declspec(dllexport)
#else
#define DLLEXPORT __attribute__((visibility("default")))
#endif
#endif

// Entrypoint jumped to by IJW dlls when their dllmain is called
extern "C" DLLEXPORT BOOL WINAPI _CorDllMain(HINSTANCE hInst, DWORD dwReason, LPVOID lpReserved)
{
    return TRUE;
}
