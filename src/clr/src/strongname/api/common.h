// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// common.h - precompiled headers include for the COM+ Execution Engine
//

#ifndef _common_h_ 
#define _common_h_

#if defined(_MSC_VER) && defined(_X86_) && !defined(FPO_ON)
#pragma optimize("y", on)       // Small critical routines, don't put in EBP frame 
#define FPO_ON 1
#define COMMON_TURNED_FPO_ON 1
#endif

#if STRONGNAME_IN_VM

#define USE_COM_CONTEXT_DEF

#ifdef _DEBUG 
#define DEBUG_REGDISPLAY
#endif

#ifdef _MSC_VER 

    // These don't seem useful, so turning them off is no big deal
#pragma warning(disable:4244)   // loss of data int -> char ..

#ifndef DEBUG 
#pragma warning(disable:4189)   // local variable initialized but not used
#pragma warning(disable:4505)   // unreferenced local function has been removed
#pragma warning(disable:4313)   // 'format specifier' in format string conflicts with argument %d of type 'type'
#endif // !DEBUG


#endif // _MSC_VER

#endif // STRONGNAME_IN_VM

#define _CRT_DEPENDENCY_   //this code depends on the crt file functions


#include <stdint.h>
#include <winwrap.h>

#include <windows.h>
#include <wincrypt.h> 
#include <winnt.h>
#include <crosscomp.h>
#include <clrnt.h>
#include <stdlib.h>
#include <wchar.h>
#include <objbase.h>
#include <stddef.h>
#include <float.h>
#include <math.h>
#include <time.h>
#include <limits.h>

#include <olectl.h>

#ifdef _MSC_VER 
//non inline intrinsics are faster
#pragma function(memcpy,memcmp,strcmp,strcpy,strlen,strcat)
#endif // _MSC_VER

//-----------------------------------------------------------------------------------------------------------

#include "lazycow.h"

#include "strongname.h"
#include "stdmacros.h"

#define POISONC ((UINT_PTR)((sizeof(int *) == 4)?0xCCCCCCCCL:I64(0xCCCCCCCCCCCCCCCC)))

#include "ndpversion.h"
#include "switches.h"
#include "holder.h"

#if STRONGNAME_IN_VM
#include "classnames.h"
#include "util.hpp"
#endif // STRONGNAME_IN_VM

#include "corpriv.h"

#include <daccess.h>

#if STRONGNAME_IN_VM

typedef VPTR(class LoaderAllocator)     PTR_LoaderAllocator;
typedef VPTR(class AppDomain)           PTR_AppDomain;
typedef DPTR(class ArrayBase)           PTR_ArrayBase;
typedef DPTR(class ArrayTypeDesc)       PTR_ArrayTypeDesc;
typedef DPTR(class Assembly)            PTR_Assembly;
typedef DPTR(class AssemblyBaseObject)  PTR_AssemblyBaseObject;
typedef DPTR(class AssemblyLoadContextBaseObject) PTR_AssemblyLoadContextBaseObject;
typedef DPTR(class AssemblyNameBaseObject) PTR_AssemblyNameBaseObject;
typedef VPTR(class BaseDomain)          PTR_BaseDomain;
typedef DPTR(class MscorlibBinder)      PTR_MscorlibBinder;
typedef DPTR(class ClassLoader)         PTR_ClassLoader;
typedef DPTR(class ComCallMethodDesc)   PTR_ComCallMethodDesc;
typedef VPTR(class CompilationDomain)   PTR_CompilationDomain;
typedef DPTR(class ComPlusCallMethodDesc) PTR_ComPlusCallMethodDesc;
typedef VPTR(class DebugInterface)      PTR_DebugInterface;
typedef DPTR(class Dictionary)          PTR_Dictionary;
typedef VPTR(class DomainAssembly)      PTR_DomainAssembly;
typedef VPTR(class DomainFile)          PTR_DomainFile;
typedef VPTR(class DomainModule)        PTR_DomainModule;
typedef DPTR(struct FailedAssembly)     PTR_FailedAssembly;
typedef VPTR(class EditAndContinueModule) PTR_EditAndContinueModule;
typedef DPTR(class EEClass)             PTR_EEClass;
typedef DPTR(class DelegateEEClass)     PTR_DelegateEEClass;
typedef DPTR(struct DomainLocalModule)  PTR_DomainLocalModule;
typedef VPTR(class EECodeManager)       PTR_EECodeManager;
typedef DPTR(class EEConfig)            PTR_EEConfig;
typedef VPTR(class EEDbgInterfaceImpl)  PTR_EEDbgInterfaceImpl;
typedef VPTR(class DebugInfoManager)    PTR_DebugInfoManager;
typedef DPTR(class FieldDesc)           PTR_FieldDesc;
typedef VPTR(class Frame)               PTR_Frame;
typedef VPTR(class ICodeManager)        PTR_ICodeManager;
typedef VPTR(class IJitManager)         PTR_IJitManager;
typedef DPTR(class InstMethodHashTable) PTR_InstMethodHashTable;
typedef DPTR(class MetaSig)             PTR_MetaSig;
typedef DPTR(class MethodDesc)          PTR_MethodDesc;
typedef DPTR(class MethodDescChunk)     PTR_MethodDescChunk;
typedef DPTR(class MethodImpl)          PTR_MethodImpl;
typedef DPTR(class MethodTable)         PTR_MethodTable;
typedef VPTR(class Module)              PTR_Module;
typedef DPTR(class NDirectMethodDesc)   PTR_NDirectMethodDesc;
typedef VPTR(class Thread)              PTR_Thread;
typedef DPTR(class Object)              PTR_Object;
typedef DPTR(PTR_Object)                PTR_PTR_Object;
typedef DPTR(class ObjHeader)           PTR_ObjHeader;
typedef DPTR(class Precode)             PTR_Precode;
typedef VPTR(class ReflectionModule)    PTR_ReflectionModule;
typedef DPTR(class ReflectClassBaseObject) PTR_ReflectClassBaseObject;
typedef DPTR(class ReflectModuleBaseObject) PTR_ReflectModuleBaseObject;
typedef DPTR(class ReflectMethodObject) PTR_ReflectMethodObject;
typedef DPTR(class ReflectFieldObject)  PTR_ReflectFieldObject;
typedef DPTR(class ReJitManager)        PTR_ReJitManager;
typedef DPTR(struct ReJitInfo)          PTR_ReJitInfo;
typedef DPTR(struct SharedReJitInfo)    PTR_SharedReJitInfo;
typedef DPTR(class StringObject)        PTR_StringObject;
#ifdef FEATURE_UTF8STRING
typedef DPTR(class Utf8StringObject)    PTR_Utf8StringObject;
#endif // FEATURE_UTF8STRING
typedef DPTR(class TypeHandle)          PTR_TypeHandle;
#ifdef STUB_DISPATCH
typedef VPTR(class VirtualCallStubManager) PTR_VirtualCallStubManager;
typedef VPTR(class VirtualCallStubManagerManager) PTR_VirtualCallStubManagerManager;
#endif
typedef VPTR(class IGCHeap)              PTR_IGCHeap;

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


EXTERN_C Thread* STDCALL GetThread();

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
EXTERN_C AppDomain* STDCALL GetAppDomain();
#endif //!DACCESS_COMPILE

inline void RetailBreak()  
{
#if defined(_TARGET_X86_)
    __asm int 3
#else
    DebugBreak();
#endif
}

extern BOOL isMemoryReadable(const TADDR start, unsigned len);

#ifndef memcpyUnsafe_f 
#define memcpyUnsafe_f

// use this when you want to memcpy something that contains GC refs
inline void* memcpyUnsafe(void *dest, const void *src, size_t len)
{
    WRAPPER_NO_CONTRACT;
    return memcpy(dest, src, len);
}

#endif // !memcpyUnsafe_f

//
// By default logging, and debug GC are enabled under debug
//
// These can be enabled in non-debug by removing the #ifdef _DEBUG
// allowing one to log/check_gc a free build.
//
#if defined(_DEBUG) && !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)

    //If memcpy has been defined to PAL_memcpy, we undefine it so that this case
    //can be covered by the if !defined(memcpy) block below
    #ifdef FEATURE_PAL
    #if IS_REDEFINED_IN_PAL(memcpy)
    #undef memcpy
    #endif //IS_REDEFINED_IN_PAL
    #endif //FEATURE_PAL

        // You should be using CopyValueClass if you are doing an memcpy
        // in the CG heap.
    #if !defined(memcpy)
    inline void* memcpyNoGCRefs(void * dest, const void * src, size_t len) {
            WRAPPER_NO_CONTRACT;

            #ifndef FEATURE_PAL
                return memcpy(dest, src, len);
            #else //FEATURE_PAL
                return PAL_memcpy(dest, src, len);
            #endif //FEATURE_PAL
            
        }
    extern "C" void *  __cdecl GCSafeMemCpy(void *, const void *, size_t);
    #define memcpy(dest, src, len) GCSafeMemCpy(dest, src, len)
    #endif // !defined(memcpy)
#else // !_DEBUG && !DACCESS_COMPILE && !CROSSGEN_COMPILE
    inline void* memcpyNoGCRefs(void * dest, const void * src, size_t len) {
            WRAPPER_NO_CONTRACT;
            return memcpy(dest, src, len);
        }
#endif // !_DEBUG && !DACCESS_COMPILE && !CROSSGEN_COMPILE

namespace Loader
{
    typedef enum 
    {
        Load, //should load
        DontLoad, //should not load
        SafeLookup  //take no locks, no allocations
    } LoadFlag;
}

#endif // STRONGNAME_IN_VM

// src/inc
#include "utilcode.h"
#include "log.h"
#include "loaderheap.h"

#if STRONGNAME_IN_VM

// src/vm
#include "gcenv.interlocked.h"
#include "gcenv.interlocked.inl"

#include "util.hpp"
#include "ibclogger.h"
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
#include "cgensys.h"

#ifdef FEATURE_COMINTEROP 
#include "stdinterfaces.h"
#endif

#include "typehandle.h"
#include "methodtable.h"
#include "typectxt.h"

#include "eehash.h"

#include "vars.hpp"
#include "eventstore.hpp"

#include "synch.h"
#include "regdisp.h"
#include "stackframe.h"
#include "gms.h"
#include "fcall.h"
#include "syncblk.h"
#include "gcdesc.h"
#include "specialstatics.h"
#include "object.h"  // <NICE> We should not really need to put this so early... </NICE>
#include "gchelpers.h"
#include "pefile.h"
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
#include "appdomain.hpp"
#include "assembly.hpp"
#include "pefile.inl"
#include "excep.h"
#include "method.hpp"
#include "frames.h"

#include "stackwalk.h"
#include "stackingallocator.h"
#include "interoputil.h"
#include "wrappers.h"
#include "dynamicmethod.h"


HRESULT EnsureRtlFunctions();
HINSTANCE GetModuleInst();


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
#include "ceeload.inl"
#include "clsload.inl"
#include "domainfile.inl"
#include "clsload.inl"
#include "method.inl"
#include "syncblk.inl"
#include "threads.inl"
#include "eehash.inl"

#endif // STRONGNAME_IN_VM

#include "pedecoder.h"

#if defined(COMMON_TURNED_FPO_ON)
#pragma optimize("", on)        // Go back to command line default optimizations
#undef COMMON_TURNED_FPO_ON
#undef FPO_ON
#endif

#endif // !_common_h_
