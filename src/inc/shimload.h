//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/*============================================================
**
** Header:  ShimLoad.hpp
**
** Purpose: Delay load hook used to images to bind to 
**          dll's shim shipped with the EE
**
**
===========================================================*/
#ifndef _SHIMLOAD_H
#define _SHIMLOAD_H

#ifndef FEATURE_CORECLR
#include <delayimp.h>

extern FARPROC __stdcall ShimDelayLoadHook(unsigned        dliNotify,          // What event has occurred, dli* flag.
                                           DelayLoadInfo   *pdli);             // Description of the event.

//and one for safe mode
extern FARPROC __stdcall ShimSafeModeDelayLoadHook(unsigned        dliNotify,          // What event has occurred, dli* flag.
                                           DelayLoadInfo   *pdli);             // Description of the event.

#endif

//*****************************************************************************
// Sets/Gets the directory based on the location of the module. This routine
// is called at COR setup time. Set is called during EEStartup and by the 
// MetaData dispenser.
//*****************************************************************************
HRESULT SetInternalSystemDirectory();
HRESULT GetInternalSystemDirectory(__out_ecount_opt(*pdwLength) LPWSTR buffer, __inout DWORD* pdwLength);

#endif

