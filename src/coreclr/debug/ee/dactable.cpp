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

template<typename T, template<typename> typename U>
struct is_type_template_instantiation
{
    constexpr static bool m_value = false;
};
template<typename T, template<typename> typename U>
struct is_type_template_instantiation<U<T>, U>
{
    constexpr static bool m_value = true;
};

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

// __ImageBase will always be the base of the RVAs in whatever PE this file is compiled into.
extern "C" BYTE __ImageBase;

// Re-export the static dac table as the global g_dacTable symbol.
// This allows us to not have to change how any of the "friend struct" relationships with the DAC table work
// while also still statically initializing the dac table.
#pragma comment(linker, "/EXPORT:g_dacTable=?s_dacGlobals@_DacGlobals@@0U1@B")
const DacGlobals _DacGlobals::s_dacGlobals = 
{
#define DEFINE_DACVAR(size, id, var)                   PTR_TO_TADDR(&var) - PTR_TO_TADDR(&__ImageBase),
#define DEFINE_DACVAR_VOLATILE(size, id, var)          PTR_TO_TADDR(&var.m_val) - PTR_TO_TADDR(&__ImageBase),
#define DEFINE_DACVAR_NO_DUMP(size, id, var)           PTR_TO_TADDR(&var) - PTR_TO_TADDR(&__ImageBase),
#include "dacvars.h"
#undef DEFINE_DACVAR
#undef DEFINE_DACVAR_VOLATILE
#undef DEFINE_DACVAR_NO_DUMP
#define DEFINE_DACGFN(func) PTR_TO_TADDR(&func) - PTR_TO_TADDR(&__ImageBase),
#define DEFINE_DACGFN_STATIC(class, func) PTR_TO_TADDR(&class::func) - PTR_TO_TADDR(&__ImageBase),
#include "gfunc_list.h"
#undef DEFINE_DACGFN
#undef DEFINE_DACGFN_STATIC
#define VPTR_CLASS(type) PTR_TO_TADDR(&vtable_ ## type) - PTR_TO_TADDR(&__ImageBase),
#define VPTR_MULTI_CLASS(type, keyBase) PTR_TO_TADDR(&vtable_ ## type ## _ ## keyBase) - PTR_TO_TADDR(&__ImageBase),
#include "vptr_list.h"
};

// DacGlobals::Initialize is a no-op on MSVC builds as we statically initialize the table,
// however, it provides a nice mechansim for us to get back into the right scope to validate the usage of DEFINE_DACVAR and family
// without needing to make all of the DAC varables public or include all of the headers in daccess.h.
void DacGlobals::Initialize()
{
#define DEFINE_DACVAR(size, id, var) static_assert(!is_type_template_instantiation<decltype(var), Volatile>::m_value, "DAC variables defined with DEFINE_DACVAR must not be instantiations of Volatile<T>.");
#define DEFINE_DACVAR_NODUMP(size, id, var) static_assert(!is_type_template_instantiation<decltype(var), Volatile>::m_value, "DAC variables defined with DEFINE_DACVAR_NODUMP must not be instantiations of Volatile<T>.");
#define DEFINE_DACVAR_VOLATILE(size, id, var) static_assert(is_type_template_instantiation<decltype(var), Volatile>::m_value, "DAC variables defined with DEFINE_DACVAR_VOLATILE must be instantiations of Volatile<T>.");
#include "dacvars.h"
#undef DEFINE_DACVAR_VOLATILE
#undef DEFINE_DACVAR_NODUMP
#undef DEFINE_DACVAR
}
#else
// Only dynamically initialize on non-MSVC builds since we can't handle symbol aliasing
// the same way we do with MSVC to statically initialize the DAC table.
DLLEXPORT DacGlobals g_DacTable;

void DacGlobals::Initialize()
{
    TADDR baseAddress = PTR_TO_TADDR(PAL_GetSymbolModuleBase((void *)DacGlobals::Initialize));
    
#define DEFINE_DACVAR(size, id, var) static_assert(!is_type_template_instantiation<decltype(var), Volatile>::m_value, "DAC variables defined with DEFINE_DACVAR must not be instantiations of Volatile<T>.");
#define DEFINE_DACVAR_NODUMP(size, id, var) static_assert(!is_type_template_instantiation<decltype(var), Volatile>::m_value, "DAC variables defined with DEFINE_DACVAR_NODUMP must not be instantiations of Volatile<T>.");
#define DEFINE_DACVAR_VOLATILE(size, id, var) static_assert(is_type_template_instantiation<decltype(var), Volatile>::m_value, "DAC variables defined with DEFINE_DACVAR_VOLATILE must be instantiations of Volatile<T>.");
#include "dacvars.h"
#undef DEFINE_DACVAR_VOLATILE
#undef DEFINE_DACVAR_NODUMP
#undef DEFINE_DACVAR

#define DEFINE_DACVAR(size, id, var)                   id = PTR_TO_TADDR(&var) - baseAddress;
#define DEFINE_DACVAR_VOLATILE(size, id, var)          id = PTR_TO_TADDR(&var.m_val) - baseAddress;
#define DEFINE_DACVAR_NO_DUMP(size, id, var)           id = PTR_TO_TADDR(&var) - baseAddress;
#undef DEFINE_DACVAR_VOLATILE
#undef DEFINE_DACVAR_NODUMP
#undef DEFINE_DACVAR
#include "dacvars.h"
#undef DEFINE_DACVAR
#undef DEFINE_DACVAR_VOLATILE
#undef DEFINE_DACVAR_NO_DUMP
#define DEFINE_DACGFN(func) id = PTR_TO_TADDR(&func)  - baseAddress;
#define DEFINE_DACGFN_STATIC(class, func) id = PTR_TO_TADDR(&class::func)  - baseAddress;
#include "gfunc_list.h"
#undef DEFINE_DACGFN
#undef DEFINE_DACGFN_STATIC
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
#endif
#endif // DEBUGGER_SUPPORTED
