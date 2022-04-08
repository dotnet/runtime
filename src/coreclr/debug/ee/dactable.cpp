// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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

#ifdef DEBUGGING_SUPPORTED

extern PTR_ECHash gFCallMethods[FCALL_HASH_SIZE];
extern TADDR gLowestFCall;
extern TADDR gHighestFCall;
extern "C" PCODE g_FCDynamicallyAssignedImplementations;
extern DWORD gThreadTLSIndex;
extern DWORD gAppDomainTLSIndex;
extern "C" void STDCALL ThePreStubPatchLabel(void);

namespace ABI
{
    struct ComInterfaceDispatch;
}
HRESULT STDMETHODCALLTYPE ManagedObjectWrapper_QueryInterface(
    _In_ ABI::ComInterfaceDispatch* disp,
    /* [in] */ REFIID riid,
    /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR* __RPC_FAR* ppvObject);
HRESULT STDMETHODCALLTYPE TrackerTarget_QueryInterface(
    _In_ ABI::ComInterfaceDispatch* disp,
    /* [in] */ REFIID riid,
    /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR* __RPC_FAR* ppvObject);

#ifdef _MSC_VER
// Based on the MSVC name mangling convention, use the /ALTERNATENAME linker switch to provide C-friendly symbol names
// for each vtable we care about.
#define DEFINE_ALTERNATENAME_3(part) _Pragma(#part)
#define DEFINE_ALTERNATENAME_2(part) DEFINE_ALTERNATENAME_3(comment(linker, part))
#define DEFINE_ALTERNATENAME_1(part) DEFINE_ALTERNATENAME_2(#part)
#define DEFINE_ALTERNATENAME(alias, func) DEFINE_ALTERNATENAME_1(/ALTERNATENAME:alias=func)
#define VPTR_CLASS(type) DEFINE_ALTERNATENAME(vtable_ ## type, ??_7 ## type ## @@6B@) extern "C" void* vtable_ ## type;
#define VPTR_MULTI_CLASS(type, keyBase) DEFINE_ALTERNATENAME(vtable_ ## type, ??_7 ## type ## @@6B@ ## keyBase ## @@@) extern "C" void* vtable_ ## type ## _ ## keyBase;
#include "vptr_list.h"
#undef VPTR_CLASS
#undef VPTR_MULTI_CLASS

// Re-export the static dac table as the global g_dacTable symbol.
// This allows us to not have to change how any of the "friend struct" relationships with the DAC table work
// while also still statically initializing the dac table.
#pragma comment(linker, "/EXPORT:g_dacTable=?s_dacGlobals@_DacGlobals@@0U1@B")
const DacGlobals _DacGlobals::s_dacGlobals = 
{
#define DEFINE_DACVAR(id_type, size, id, var)                   PTR_TO_TADDR(&var),
#define DEFINE_DACVAR_NO_DUMP(id_type, size, id, var)           PTR_TO_TADDR(&var),
#include "dacvars.h"
#undef DEFINE_DACVAR
#undef DEFINE_DACVAR_NO_DUMP
#define DEFINE_DACGFN(func) PTR_TO_TADDR(&func),
#define DEFINE_DACGFN_STATIC(class, func) PTR_TO_TADDR(&class::func),
#include "gfunc_list.h"
#define VPTR_CLASS(type) PTR_TO_TADDR(&vtable_ ## type),
#define VPTR_MULTI_CLASS(type, keyBase) PTR_TO_TADDR(&vtable_ ## type ## _ ## keyBase),
#include "vptr_list.h"
};
#else
// Only dynamically initialize on non-MSVC builds since we can't handle symbol aliasing
// the same way we do with MSVC to statically initialize the DAC table.
DLLEXPORT DacGlobals g_DacTable;

void DacGlobals::Initialize()
{
#define DEFINE_DACVAR(id_type, size, id, var)                   id = PTR_TO_TADDR(&var);
#define DEFINE_DACVAR_NO_DUMP(id_type, size, id, var)           id = PTR_TO_TADDR(&var);
#include "dacvars.h"
#define VPTR_CLASS(name) \
    { \
        void *pBuf = _alloca(sizeof(name)); \
        name *dummy = new (pBuf) name(0); \
        name##__vtAddr = PTR_TO_TADDR(*((PVOID*)dummy)); \
    }
#define VPTR_MULTI_CLASS(name, keyBase) \
    { \
        void *pBuf = _alloca(sizeof(name)); \
        name *dummy = new (pBuf) name(0); \
        name##__##keyBase##__mvtAddr = PTR_TO_TADDR(*((PVOID*)dummy)); \
    }
#include <vptr_list.h>
#undef VPTR_CLASS
#undef VPTR_MULTI_CLASS
}
#endif
#endif // DEBUGGER_SUPPORTED
