//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//*****************************************************************************
// File: dacglobals.cpp
//

//
// The DAC global pointer table
//
//*****************************************************************************

#include "stdafx.h"
#include <daccess.h>
#include "../../vm/virtualcallstub.h"
#include "../../vm/win32threadpool.h"
#include "../../vm/hillclimbing.h"
#include "../../vm/codeman.h"
#include "../../vm/eedbginterfaceimpl.h"
#include "../../vm/common.h"
#include "../../vm/gcenv.h"
#include "../../vm/ecall.h"
#include "../../vm/rcwwalker.h"
#include "../../gc/gc.h"
#include "../../gc/gcscan.h"

#undef SERVER_GC
namespace WKS {
#include "../../gc/gcimpl.h"
#include "../../gc/gcpriv.h"
}

#ifdef DEBUGGING_SUPPORTED

extern PTR_ECHash gFCallMethods;
extern TADDR gLowestFCall;
extern TADDR gHighestFCall;
extern PCODE g_FCDynamicallyAssignedImplementations;
extern DWORD gThreadTLSIndex;
extern DWORD gAppDomainTLSIndex;

#ifdef FEATURE_APPX
#if defined(FEATURE_CORECLR)
extern BOOL g_fAppX;
#else
extern PTR_AppXRTInfo g_pAppXRTInfo;
#endif
#endif // FEATURE_APPX

DacGlobals g_dacTable;

// DAC global pointer table initialization
void DacGlobals::Initialize()
{
    TADDR baseAddress = PTR_TO_TADDR(PAL_GetCoreClrModuleBase());
    g_dacTable.InitializeEntries(baseAddress);
#ifdef FEATURE_SVR_GC
    g_dacTable.InitializeSVREntries(baseAddress);
#endif
    PAL_PublishDacTableAddress(&g_dacTable, sizeof(g_dacTable));
}

// Initializes the non-SVR table entries
void DacGlobals::InitializeEntries(TADDR baseAddress)
{
#define DEFINE_DACVAR(id_type, size, id, var)                   id = PTR_TO_TADDR(&var) - baseAddress;
#define DEFINE_DACVAR_SVR(id_type, size, id, var) 
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
