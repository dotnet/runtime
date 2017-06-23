// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// File: dacglobals.cpp
//

//
// The DAC global pointer table
//
//*****************************************************************************

#include "stdafx.h"
#include <daccess.h>

// This header include will need to be rmeoved as part of GitHub#12170.
// The only reason it's here now is that this file references the GC-private
// variable g_HandleTableMap.
#include "../../gc/objecthandle.h"
#include "../../vm/virtualcallstub.h"
#include "../../vm/win32threadpool.h"
#include "../../vm/hillclimbing.h"
#include "../../vm/codeman.h"
#include "../../vm/eedbginterfaceimpl.h"
#include "../../vm/common.h"
#include "../../vm/gcenv.h"
#include "../../vm/ecall.h"
#include "../../vm/rcwwalker.h"

#ifdef DEBUGGING_SUPPORTED

extern PTR_ECHash gFCallMethods;
extern TADDR gLowestFCall;
extern TADDR gHighestFCall;
extern PCODE g_FCDynamicallyAssignedImplementations;
extern DWORD gThreadTLSIndex;
extern DWORD gAppDomainTLSIndex;

#ifdef FEATURE_APPX
extern BOOL g_fAppX;
#endif // FEATURE_APPX

DacGlobals g_dacTable;

// DAC global pointer table initialization
void DacGlobals::Initialize()
{
    TADDR baseAddress = PTR_TO_TADDR(PAL_GetSymbolModuleBase((void *)DacGlobals::Initialize));
    g_dacTable.InitializeEntries(baseAddress);
}

// Initializes the non-SVR table entries
void DacGlobals::InitializeEntries(TADDR baseAddress)
{
#define DEFINE_DACVAR(id_type, size, id, var)                   id = PTR_TO_TADDR(&var) - baseAddress;
#define DEFINE_DACVAR_NO_DUMP(id_type, size, id, var)           id = PTR_TO_TADDR(&var) - baseAddress;
#include "dacvars.h"

#define VPTR_CLASS(name) \
    { \
        void *pBuf = _alloca(sizeof(name)); \
        name *dummy = new (pBuf) name(0); \
        name##__vtAddr = PTR_TO_TADDR(*((PVOID*)dummy)) - baseAddress; \
    }
#define VPTR_MULTI_CLASS(name, keyBase) \
    { \
        void *pBuf = _alloca(sizeof(name)); \
        name *dummy = new (pBuf) name(0); \
        name##__##keyBase##__mvtAddr = PTR_TO_TADDR(*((PVOID*)dummy)) - baseAddress; \
    }
#include <vptr_list.h>
#undef VPTR_CLASS
#undef VPTR_MULTI_CLASS
}

#endif // DEBUGGER_SUPPORTED
