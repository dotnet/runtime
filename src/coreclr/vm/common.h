// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// common.h - precompiled headers include for the CLR Execution Engine
//

#ifndef _common_h_
#define _common_h_

#if defined(_MSC_VER) && defined(HOST_X86) && !defined(FPO_ON)
#pragma optimize("y", on)       // Small critical routines, don't put in EBP frame
#define FPO_ON 1
#define COMMON_TURNED_FPO_ON 1
#endif

#if defined(_DEBUG)
#define DEBUG_REGDISPLAY
#endif

#include <stdint.h>
#include <stddef.h>
#include <winwrap.h>
#include <algorithm>

#include <windef.h>
#include <winnt.h>
#include <stdlib.h>
#include <wchar.h>
#include <objbase.h>
#include <float.h>
#include <cmath>
#include <time.h>
#include <limits.h>
#include <assert.h>
#include <cstdint>
#include <functional>

#include <olectl.h>

#if defined(HOST_AMD64) || defined(HOST_X86)
#include <xmmintrin.h>
#endif

using std::max;
using std::min;

#ifdef _MSC_VER
//non inline intrinsics are faster
#pragma function(memcpy,memcmp,strcmp,strcpy,strlen,strcat)
#endif // _MSC_VER

#include "volatile.h"

#include <../../debug/inc/dbgtargetcontext.h>

//-----------------------------------------------------------------------------------------------------------

#include "stdmacros.h"

#define POISONC ((UINT_PTR)((sizeof(int *) == 4)?0xCCCCCCCCL:0xCCCCCCCCCCCCCCCCLL))

#include "switches.h"
#include "holder.h"
#include "classnames.h"
#include "util.hpp"
#include "corpriv.h"

#include <daccess.h>

typedef VPTR(class LoaderAllocator)     PTR_LoaderAllocator;
typedef DPTR(PTR_LoaderAllocator)       PTR_PTR_LoaderAllocator;
typedef DPTR(class AppDomain)           PTR_AppDomain;
typedef DPTR(class ArrayBase)           PTR_ArrayBase;
typedef DPTR(class Assembly)            PTR_Assembly;
typedef DPTR(class AssemblyBaseObject)  PTR_AssemblyBaseObject;
typedef DPTR(class AssemblyLoadContextBaseObject) PTR_AssemblyLoadContextBaseObject;
typedef DPTR(class AssemblyBinder)      PTR_AssemblyBinder;
typedef DPTR(class AssemblyNameBaseObject) PTR_AssemblyNameBaseObject;
typedef DPTR(class ClassLoader)         PTR_ClassLoader;
typedef DPTR(class ComCallMethodDesc)   PTR_ComCallMethodDesc;
typedef DPTR(class CLRToCOMCallMethodDesc) PTR_CLRToCOMCallMethodDesc;
typedef VPTR(class DebugInterface)      PTR_DebugInterface;
typedef DPTR(class Dictionary)          PTR_Dictionary;
typedef DPTR(struct FailedAssembly)     PTR_FailedAssembly;
typedef VPTR(class EditAndContinueModule) PTR_EditAndContinueModule;
typedef DPTR(class EEClass)             PTR_EEClass;
typedef DPTR(class DelegateEEClass)     PTR_DelegateEEClass;
typedef VPTR(class EECodeManager)       PTR_EECodeManager;
#ifdef FEATURE_INTERPRETER
typedef VPTR(class InterpreterCodeManager) PTR_InterpreterCodeManager;
typedef DPTR(struct InterpThreadContext) PTR_InterpThreadContext;
#endif
typedef DPTR(class RangeSectionMap)     PTR_RangeSectionMap;
typedef DPTR(class EEConfig)            PTR_EEConfig;
typedef VPTR(class EEDbgInterfaceImpl)  PTR_EEDbgInterfaceImpl;
typedef VPTR(class DebugInfoManager)    PTR_DebugInfoManager;
typedef DPTR(class FieldDesc)           PTR_FieldDesc;
typedef DPTR(class Frame)               PTR_Frame;
typedef DPTR(class GCFrame)             PTR_GCFrame;
typedef VPTR(class ICodeManager)        PTR_ICodeManager;
typedef VPTR(class IJitManager)         PTR_IJitManager;
typedef VPTR(struct IUnknown)           PTR_IUnknown;
typedef DPTR(class InstMethodHashTable) PTR_InstMethodHashTable;
typedef DPTR(class MetaSig)             PTR_MetaSig;
typedef DPTR(class MethodDesc)          PTR_MethodDesc;
typedef DPTR(class MethodDescChunk)     PTR_MethodDescChunk;
typedef DPTR(class MethodImpl)          PTR_MethodImpl;
typedef DPTR(class MethodTable)         PTR_MethodTable;
typedef DPTR(class CoreLibBinder)      PTR_CoreLibBinder;
typedef VPTR(class Module)              PTR_Module;
typedef DPTR(class PInvokeMethodDesc)   PTR_PInvokeMethodDesc;
typedef DPTR(class Thread)              PTR_Thread;
typedef DPTR(class Object)              PTR_Object;
typedef DPTR(PTR_Object)                PTR_PTR_Object;
typedef DPTR(class DelegateObject)      PTR_DelegateObject;
typedef DPTR(class ObjHeader)           PTR_ObjHeader;
typedef DPTR(class Precode)             PTR_Precode;
typedef VPTR(class ReflectionModule)    PTR_ReflectionModule;
typedef DPTR(class ReflectClassBaseObject) PTR_ReflectClassBaseObject;
typedef DPTR(class ReflectMethodObject) PTR_ReflectMethodObject;
typedef DPTR(class ReflectFieldObject)  PTR_ReflectFieldObject;
typedef DPTR(class ReflectModuleBaseObject) PTR_ReflectModuleBaseObject;
typedef DPTR(class ReJitManager)        PTR_ReJitManager;
typedef DPTR(struct ReJitInfo)          PTR_ReJitInfo;
typedef DPTR(struct SharedReJitInfo)    PTR_SharedReJitInfo;
typedef DPTR(class StringObject)        PTR_StringObject;
typedef DPTR(class TypeHandle)          PTR_TypeHandle;
typedef VPTR(class VirtualCallStubManager) PTR_VirtualCallStubManager;
typedef VPTR(class VirtualCallStubManagerManager) PTR_VirtualCallStubManagerManager;
typedef VPTR(class IGCHeap)             PTR_IGCHeap;
typedef VPTR(class ModuleBase)          PTR_ModuleBase;

//
// _UNCHECKED_OBJECTREF is for code that can't deal with DEBUG OBJECTREFs
//
typedef PTR_Object _UNCHECKED_OBJECTREF;
typedef DPTR(PTR_Object) PTR_UNCHECKED_OBJECTREF;

#ifdef USE_CHECKED_OBJECTREFS
class OBJECTREF;
#else
typedef PTR_Object OBJECTREF;
#endif
typedef DPTR(OBJECTREF) PTR_OBJECTREF;
typedef DPTR(PTR_OBJECTREF) PTR_PTR_OBJECTREF;

Thread* GetThread();
Thread* GetThreadNULLOk();

EXTERN_C Thread* STDCALL GetThreadHelper();

void SetThread(Thread*);

// This is a mechanism by which macros can make the Thread pointer available to inner scopes
// that is robust to code changes.  If the outer Thread no longer is available for some reason
// (e.g. code refactoring), this GET_THREAD() macro will fall back to calling GetThread().
const bool CURRENT_THREAD_AVAILABLE = false;
Thread * const CURRENT_THREAD = NULL;
#define GET_THREAD() (CURRENT_THREAD_AVAILABLE ? CURRENT_THREAD : GetThread())

#define MAKE_CURRENT_THREAD_AVAILABLE() \
    Thread * __pThread = GET_THREAD(); \
    MAKE_CURRENT_THREAD_AVAILABLE_EX(__pThread)

#define MAKE_CURRENT_THREAD_AVAILABLE_EX(__pThread) \
    Thread * CURRENT_THREAD = __pThread; \
    const bool CURRENT_THREAD_AVAILABLE = true; \
    (void)CURRENT_THREAD_AVAILABLE; /* silence "local variable initialized but not used" warning */ \

#ifndef DACCESS_COMPILE
AppDomain* GetAppDomain();
#endif //!DACCESS_COMPILE

extern BOOL isMemoryReadable(const TADDR start, unsigned len);

FORCEINLINE void* memcpyNoGCRefs(void * dest, const void * src, size_t len)
{
    WRAPPER_NO_CONTRACT;
    return memcpy(dest, src, len);
}

namespace Loader
{
    typedef enum
    {
        Load, //should load
        DontLoad, //should not load
        SafeLookup  //take no locks, no allocations
    } LoadFlag;
}

// src/inc
#include "utilcode.h"
#include "log.h"
#include "loaderheap.h"
#include "memorystreams.h"

// src/vm
#include "gcenv.interlocked.h"
#include "gcenv.interlocked.inl"

#include "util.hpp"
#include "eepolicy.h"

#include "vars.hpp"
#include "crst.h"
#include "argslot.h"
#include "stublink.h"
#include "cgensys.h"
#include "ceemain.h"
#include "hash.h"
#include "eecontract.h"
#include "pedecoder.h"
#include "sstring.h"
#include "slist.h"

#include "eeconfig.h"

#include "spinlock.h"

#ifdef FEATURE_COMINTEROP
#include "stdinterfaces.h"
#endif

#include "typehandle.h"
#include "methodtable.h"
#include "typectxt.h"

#include "eehash.h"

#include "vars.hpp"

#include "synch.h"
#include "regdisp.h"
#include "stackframe.h"
#include "fcall.h"
#include "syncblk.h"
#include "gcdesc.h"
#include "object.h"  // <NICE> We should not really need to put this so early... </NICE>
#include "gchelpers.h"
#include "peassembly.h"
#include "clrex.h"
#include "clsload.hpp"  // <NICE> We should not really need to put this so early... </NICE>
#include "siginfo.hpp"
#include "binder.h"
#include "jitinterface.h"  // <NICE> We should not really need to put this so early... </NICE>
#include "ceeload.h"
#include "memberload.h"
#include "genericdict.h"
#include "class.h"
#include "codeman.h"
#include "threads.h"
#include "clrex.inl"
#include "loaderallocator.hpp"
#include "callcounting.h"
#include "appdomain.hpp"
#include "appdomain.inl"
#include "assembly.hpp"
#include "peassembly.inl"
#include "excep.h"
#include "method.hpp"
#include "field.h"
#include "callingconvention.h"
#include "frames.h"
#include "qcall.h"
#include "callhelpers.h"

#include "stackwalk.h"
#include "stackingallocator.h"
#include "interoputil.h"
#include "wrappers.h"
#include "dynamicmethod.h"

#include "gcstress.h"
#include "cdacstress.h"

HRESULT EnsureRtlFunctions();

// Helper function returns the base of clr module.
void* GetClrModuleBase();

#if defined(TARGET_X86) || defined(TARGET_AMD64)
//
// Strong memory model. No memory barrier necessary before writing object references into GC heap.
//
#define GCHeapMemoryBarrier()
#else
//
// The weak memory model forces us to raise memory barriers before writing object references into GC heap. This is required
// for both security and to make most managed code written against strong memory model work. Under normal circumstances, this memory
// barrier is part of GC write barrier. However, there are a few places in the VM that set cards manually without going through
// regular GC write barrier. These places need to this macro. This macro is usually used before memcpy-like operation followed
// by SetCardsAfterBulkCopy.
//
#define GCHeapMemoryBarrier() MemoryBarrier()
#endif


// use this when you want to memcpy something that contains GC refs
void memmoveGCRefs(void *dest, const void *src, size_t len);

// Struct often used as a parameter to callbacks.
typedef struct
{
    promote_func*  f;
    ScanContext*   sc;
    CrawlFrame *   cf;
    SetSHash<Object**, PtrSetSHashTraits<Object**> > *pScannedSlots;
} GCCONTEXT;

#if defined(_DEBUG)

// This catches CANNOTTHROW macros that occur outside the scope of a CONTRACT.
// Note that it's important for m_CannotThrowLineNums to be NULL.
struct DummyGlobalContract
{
    int    *m_CannotThrowLineNums; //= NULL;
    LPVOID *m_CannotThrowRecords; //= NULL;
};

extern DummyGlobalContract ___contract;

#endif // defined(_DEBUG)

// All files get to see all of these .inl files to make sure all files
// get the benefit of inlining.
#include "ceeload.inl"
#include "typedesc.inl"
#include "class.inl"
#include "methodtable.inl"
#include "typehandle.inl"
#include "object.inl"
#include "clsload.inl"
#include "method.inl"
#include "threads.inl"
#include "eehash.inl"
#include "eventtrace.inl"

#if defined(COMMON_TURNED_FPO_ON)
#pragma optimize("", on)        // Go back to command line default optimizations
#undef COMMON_TURNED_FPO_ON
#undef FPO_ON
#endif

void LogErrorToHost(const char* format, ...);

#endif // !_common_h_


