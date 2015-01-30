//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//*****************************************************************************
// MSCorDBI.cpp
//
// COM+ Debugging Services -- Debugger Interface DLL
//
// Dll* routines for entry points, and support for COM framework.  
//
//*****************************************************************************
#include "stdafx.h"

extern BOOL STDMETHODCALLTYPE DbgDllMain(HINSTANCE hInstance, DWORD dwReason,
                                         LPVOID lpReserved);

//*****************************************************************************
// The main dll entry point for this module.  This routine is called by the
// OS when the dll gets loaded.  Control is simply deferred to the main code.
//*****************************************************************************
extern "C"
BOOL WINAPI DllMain(HINSTANCE hInstance, DWORD dwReason, LPVOID lpReserved)
{
	//<TODO> Shoud we call DisableThreadLibraryCalls?  Or does this code
	//	need native thread attatch/detatch notifications? </TODO>

	// Defer to the main debugging code.
    return DbgDllMain(hInstance, dwReason, lpReserved);
}
