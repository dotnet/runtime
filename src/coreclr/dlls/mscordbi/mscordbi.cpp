// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// MSCorDBI.cpp
//
// COM+ Debugging Services -- Debugger Interface DLL
//
// Dll* routines for entry points, and support for COM framework.
//
//*****************************************************************************
#include "stdafx.h"

#if defined(HOST_ARM64) && defined(TARGET_UNIX)
// Flag to check if atomics feature is available on
// the machine
bool g_arm64_atomics_present = false;
#endif

extern BOOL WINAPI DbgDllMain(HINSTANCE hInstance, DWORD dwReason,
                                         LPVOID lpReserved);

//*****************************************************************************
// The main dll entry point for this module.  This routine is called by the
// OS when the dll gets loaded.  Control is simply deferred to the main code.
//*****************************************************************************
extern "C"
#ifdef TARGET_UNIX
DLLEXPORT // For Win32 PAL LoadLibrary emulation
#endif
BOOL WINAPI DllMain(HINSTANCE hInstance, DWORD dwReason, LPVOID lpReserved)
{
	// Defer to the main debugging code.
    return DbgDllMain(hInstance, dwReason, lpReserved);
}
