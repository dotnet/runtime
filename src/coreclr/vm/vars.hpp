// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// vars.hpp
//
// Global variables
//


#ifndef _VARS_HPP
#define _VARS_HPP

// This will need ifdefs for non-x86 processors (ia64 is pointer to 128bit instructions)!
#define SLOT    PBYTE
typedef DPTR(SLOT) PTR_SLOT;

typedef LPVOID  DictionaryEntry;

/* Define the implementation dependent size types */

#ifndef _INTPTR_T_DEFINED
#ifdef  HOST_64BIT
typedef __int64             intptr_t;
#else
typedef int                 intptr_t;
#endif
#define _INTPTR_T_DEFINED
#endif

#ifndef _UINTPTR_T_DEFINED
#ifdef  HOST_64BIT
typedef unsigned __int64    uintptr_t;
#else
typedef unsigned int        uintptr_t;
#endif
#define _UINTPTR_T_DEFINED
#endif

#ifndef _PTRDIFF_T_DEFINED
#ifdef  HOST_64BIT
typedef __int64             ptrdiff_t;
#else
typedef int                 ptrdiff_t;
#endif
#define _PTRDIFF_T_DEFINED
#endif


#ifndef _SIZE_T_DEFINED
#ifdef  HOST_64BIT
typedef unsigned __int64 size_t;
#else
typedef unsigned int     size_t;
#endif
#define _SIZE_T_DEFINED
#endif


#include "util.hpp"
#include <corpriv.h>
#include <cordbpriv.h>


#include "eeprofinterfaces.h"
#include "eehash.h"

#include "profilepriv.h"

#include "gcinterface.h"

class ClassLoader;
class LoaderHeap;
class IGCHeap;
class Object;
class StringObject;
class ArrayClass;
class MethodTable;
class MethodDesc;
class SyncBlockCache;
class SyncTableEntry;
class ThreadStore;
namespace ETW { class CEtwTracer; };
class DebugInterface;
class DebugInfoManager;
class EEDbgInterfaceImpl;
class EECodeManager;
class Crst;
#ifdef FEATURE_COMINTEROP
class RCWCleanupList;
#endif // FEATURE_COMINTEROP

//
// loader handles are opaque types that track object pointers that have a lifetime
// that matches that of a loader allocator
//
struct LOADERHANDLE__
{
    void* unused;
};
typedef TADDR LOADERHANDLE;


#ifdef DACCESS_COMPILE
void OBJECTHANDLE_EnumMemoryRegions(OBJECTHANDLE handle);
void OBJECTREF_EnumMemoryRegions(OBJECTREF ref);
#endif


#ifdef USE_CHECKED_OBJECTREFS


//=========================================================================
// In the retail build, OBJECTREF is typedef'd to "Object*".
// In the debug build, we use operator overloading to detect
// common programming mistakes that create GC holes. The critical
// rules are:
//
//   1. Your thread must have disabled preemptive GC before
//      reading or writing any OBJECTREF. When preemptive GC is enabled,
//      another other thread can suspend you at any time and
//      move or discard objects.
//   2. You must guard your OBJECTREF's using a root pointer across
//      any code that might trigger a GC.
//
// Each of the overloads validate that:
//
//   1. Preemptive GC is currently disabled
//   2. The object looks consistent (checked by comparing the
//      object's methodtable pointer with that of the class.)
//
// Limitations:
//    - Can't say
//
//          if (or) {}
//
//      must say
//
//          if (or != NULL) {}
//
//
//=========================================================================
class OBJECTREF {
    private:
        // Holds the real object pointer.
        // The union gives us better debugger pretty printing
    union {
        Object *m_asObj;
        class StringObject* m_asString;
        class ArrayBase* m_asArray;
        class PtrArray* m_asPtrArray;
        class DelegateObject* m_asDelegate;

        class ReflectClassBaseObject* m_asReflectClass;
        class ExecutionContextObject* m_asExecutionContext;
        class AssemblyLoadContextBaseObject* m_asAssemblyLoadContextBase;
    };

    public:
        enum class tagVolatileLoadWithoutBarrier { tag };

        //-------------------------------------------------------------
        // Default constructor, for non-initializing declarations:
        //
        //      OBJECTREF or;
        //-------------------------------------------------------------
        OBJECTREF();

        //-------------------------------------------------------------
        // Copy constructor, for passing OBJECTREF's as function arguments.
        //-------------------------------------------------------------
        OBJECTREF(const OBJECTREF & objref);

        //-------------------------------------------------------------
        // Copy constructor, for passing OBJECTREF's as function arguments
        // using a volatile without barrier load
        //-------------------------------------------------------------
        OBJECTREF(const OBJECTREF * pObjref, tagVolatileLoadWithoutBarrier tag);

        //-------------------------------------------------------------
        // To allow NULL to be used as an OBJECTREF.
        //-------------------------------------------------------------
        OBJECTREF(TADDR nul);

        //-------------------------------------------------------------
        // Test against NULL.
        //-------------------------------------------------------------
        int operator!() const;

        //-------------------------------------------------------------
        // Compare two OBJECTREF's.
        //-------------------------------------------------------------
        int operator==(const OBJECTREF &objref) const;

        //-------------------------------------------------------------
        // Compare two OBJECTREF's.
        //-------------------------------------------------------------
        int operator!=(const OBJECTREF &objref) const;

        //-------------------------------------------------------------
        // Forward method calls.
        //-------------------------------------------------------------
        Object* operator->();
        const Object* operator->() const;

        //-------------------------------------------------------------
        // Assignment. We don't validate the destination so as not
        // to break the sequence:
        //
        //      OBJECTREF or;
        //      or = ...;
        //-------------------------------------------------------------
        OBJECTREF& operator=(const OBJECTREF &objref);
        OBJECTREF& operator=(TADDR nul);

            // allow explicit casts
        explicit OBJECTREF(Object *pObject);

        void Validate(BOOL bDeep = TRUE, BOOL bVerifyNextHeader = TRUE, BOOL bVerifySyncBlock = TRUE);

};

//-------------------------------------------------------------
//  template class REF for different types of REF class to be used
//  in the debug mode
//  Template type should be a class that extends Object
//-------------------------------------------------------------



template <class T>
class REF : public OBJECTREF
{
    public:

        //-------------------------------------------------------------
        // Default constructor, for non-initializing declarations:
        //
        //      OBJECTREF or;
        //-------------------------------------------------------------
      REF() :OBJECTREF ()
        {
            LIMITED_METHOD_CONTRACT;
            // no op
        }

        //-------------------------------------------------------------
        // Copy constructor, for passing OBJECTREF's as function arguments.
        //-------------------------------------------------------------
      explicit REF(const OBJECTREF& objref) : OBJECTREF(objref)
        {
            LIMITED_METHOD_CONTRACT;
            //no op
        }


        //-------------------------------------------------------------
        // To allow NULL to be used as an OBJECTREF.
        //-------------------------------------------------------------
      REF(TADDR nul) : OBJECTREF (nul)
        {
            LIMITED_METHOD_CONTRACT;
            // no op
        }

      explicit REF(T* pObject) : OBJECTREF(pObject)
        {
            LIMITED_METHOD_CONTRACT;
            // no op
        }

        //-------------------------------------------------------------
        // Forward method calls.
        //-------------------------------------------------------------
        T* operator->()
        {
            // What kind of statement can we make about member methods on Object
            // except that we need to be in COOPERATIVE when touching them?
            STATIC_CONTRACT_MODE_COOPERATIVE;
            return (T *)OBJECTREF::operator->();
        }

        const T* operator->() const
        {
            // What kind of statement can we make about member methods on Object
            // except that we need to be in COOPERATIVE when touching them?
            STATIC_CONTRACT_MODE_COOPERATIVE;
            return (const T *)OBJECTREF::operator->();
        }

        //-------------------------------------------------------------
        // Assignment. We don't validate the destination so as not
        // to break the sequence:
        //
        //      OBJECTREF or;
        //      or = ...;
        //-------------------------------------------------------------
        REF<T> &operator=(OBJECTREF &objref)
        {
            STATIC_CONTRACT_NOTHROW;
            STATIC_CONTRACT_GC_NOTRIGGER;
            STATIC_CONTRACT_CANNOT_TAKE_LOCK;
            STATIC_CONTRACT_MODE_COOPERATIVE;
            return (REF<T>&)OBJECTREF::operator=(objref);
        }

};

#define ObjectToOBJECTREF(obj)     (OBJECTREF(obj))
#define OBJECTREFToObject(objref)  ((objref).operator-> ())
#define ObjectToSTRINGREF(obj)     (STRINGREF(obj))
#define STRINGREFToObject(objref)  (*( (StringObject**) &(objref) ))
#define VolatileLoadWithoutBarrierOBJECTREF(pObj) (OBJECTREF(pObj, OBJECTREF::tagVolatileLoadWithoutBarrier::tag))

// the while (0) syntax below is to force a trailing semicolon on users of the macro
#define VALIDATEOBJECT(obj) do {if ((obj) != NULL) (obj)->Validate();} while (0)
#define VALIDATEOBJECTREF(objref) do { Object* validateObjectRefObj = OBJECTREFToObject(objref); VALIDATEOBJECT(validateObjectRefObj); } while (0)

#else   // _DEBUG_IMPL

#define VALIDATEOBJECTREF(objref)
#define VALIDATEOBJECT(obj)

#define ObjectToOBJECTREF(obj)    ((PTR_Object) (obj))
#define OBJECTREFToObject(objref) ((PTR_Object) (objref))
#define ObjectToSTRINGREF(obj)    ((PTR_StringObject) (obj))
#define STRINGREFToObject(objref) ((PTR_StringObject) (objref))
#define VolatileLoadWithoutBarrierOBJECTREF(pObj) VolatileLoadWithoutBarrier(pObj)

#endif // _DEBUG_IMPL


// <TODO> Get rid of these!  Don't use them any more!</TODO>
#define MAX_CLASSNAME_LENGTH    1024
#define MAX_NAMESPACE_LENGTH    1024

class EEConfig;
class ClassLoaderList;
class Module;

#define EXTERN extern

// For [<I1, etc. up to and including [Object
GARY_DECL(TypeHandle, g_pPredefinedArrayTypes, ELEMENT_TYPE_MAX);

extern "C" Volatile<int32_t>   g_TrapReturningThreads;

#ifdef _DEBUG
// next two variables are used to enforce an ASSERT in Thread::DbgFindThread
// that does not allow g_TrapReturningThreads to creep up unchecked.
EXTERN Volatile<LONG>       g_trtChgStamp;
EXTERN Volatile<LONG>       g_trtChgInFlight;
EXTERN const char *         g_ExceptionFile;
EXTERN DWORD                g_ExceptionLine;
EXTERN void *               g_ExceptionEIP;
#endif
EXTERN void *               g_LastAccessViolationEIP;

GPTR_DECL(EEConfig,         g_pConfig);             // configuration data (from the registry)
GPTR_DECL(MethodTable,      g_pObjectClass);
GPTR_DECL(MethodTable,      g_pRuntimeTypeClass);
GPTR_DECL(MethodTable,      g_pCanonMethodTableClass);  // System.__Canon
GPTR_DECL(MethodTable,      g_pStringClass);
GPTR_DECL(MethodTable,      g_pArrayClass);
GPTR_DECL(MethodTable,      g_pSZArrayHelperClass);
GPTR_DECL(MethodTable,      g_pNullableClass);
GPTR_DECL(MethodTable,      g_pExceptionClass);
GPTR_DECL(MethodTable,      g_pThreadAbortExceptionClass);
GPTR_DECL(MethodTable,      g_pOutOfMemoryExceptionClass);
GPTR_DECL(MethodTable,      g_pStackOverflowExceptionClass);
GPTR_DECL(MethodTable,      g_pExecutionEngineExceptionClass);
GPTR_DECL(MethodTable,      g_pThreadAbortExceptionClass);
GPTR_DECL(MethodTable,      g_pDelegateClass);
GPTR_DECL(MethodTable,      g_pMulticastDelegateClass);
GPTR_DECL(MethodTable,      g_pFreeObjectMethodTable);
GPTR_DECL(MethodTable,      g_pValueTypeClass);
GPTR_DECL(MethodTable,      g_pEnumClass);
GPTR_DECL(MethodTable,      g_pThreadClass);
GPTR_DECL(MethodTable,      g_pOverlappedDataClass);

GPTR_DECL(MethodTable,      g_TypedReferenceMT);

#ifdef FEATURE_COMINTEROP
GPTR_DECL(MethodTable,      g_pBaseCOMObject);
#endif

GPTR_DECL(MethodTable,      g_pIDynamicInterfaceCastableInterface);

#ifdef FEATURE_ICASTABLE
GPTR_DECL(MethodTable,      g_pICastableInterface);
#endif // FEATURE_ICASTABLE

GPTR_DECL(MethodDesc,       g_pObjectFinalizerMD);

#ifdef FEATURE_INTEROP_DEBUGGING
GVAL_DECL(DWORD,            g_debuggerWordTLSIndex);
#endif
GVAL_DECL(DWORD,            g_TlsIndex);

// Global System Information
extern SYSTEM_INFO g_SystemInfo;

// <TODO>@TODO - PROMOTE.</TODO>
// <TODO>@TODO - I'd like to make these private members of CLRException some day.</TODO>
EXTERN OBJECTHANDLE         g_pPreallocatedOutOfMemoryException;
EXTERN OBJECTHANDLE         g_pPreallocatedStackOverflowException;
EXTERN OBJECTHANDLE         g_pPreallocatedExecutionEngineException;

// we use this as a dummy object to indicate free space in the handle tables -- this object is never visible to the world
EXTERN OBJECTHANDLE         g_pPreallocatedSentinelObject;

GPTR_DECL(Thread,g_pFinalizerThread);
GPTR_DECL(Thread,g_pSuspensionThread);

// Global SyncBlock cache
typedef DPTR(SyncTableEntry) PTR_SyncTableEntry;
GPTR_DECL(SyncTableEntry, g_pSyncTable);

#ifdef FEATURE_COMINTEROP
// Global RCW cleanup list
typedef DPTR(RCWCleanupList) PTR_RCWCleanupList;
GPTR_DECL(RCWCleanupList,g_pRCWCleanupList);
#endif // FEATURE_COMINTEROP

// support for Event Tracing for Windows (ETW)
EXTERN ETW::CEtwTracer* g_pEtwTracer;

#ifdef STRESS_LOG
class StressLog;
typedef DPTR(StressLog) PTR_StressLog;
GPTR_DECL(StressLog, g_pStressLog);
#endif


//
// Support for the COM+ Debugger.
//
GPTR_DECL(DebugInterface,     g_pDebugInterface);
GVAL_DECL(DWORD,              g_CORDebuggerControlFlags);
#ifdef DEBUGGING_SUPPORTED
GPTR_DECL(EEDbgInterfaceImpl, g_pEEDbgInterfaceImpl);
#endif // DEBUGGING_SUPPORTED

#ifdef PROFILING_SUPPORTED
EXTERN HINSTANCE            g_pDebuggerDll;
#endif

// Global default for Concurrent GC. The default is on (value 1)
EXTERN int g_IGCconcurrent;
extern int g_IGCHoardVM;

// Returns a BOOL to indicate if the runtime is active or not
BOOL IsRuntimeActive();

//
// Global state variable indicating if the EE is in its init phase.
//
EXTERN bool g_fEEInit;

//
// Global state variable indicating if the EE has been started up.
//
EXTERN Volatile<BOOL> g_fEEStarted;

#ifdef FEATURE_COMINTEROP
//
// Global state variable indicating if COM has been started up.
//
EXTERN BOOL g_fComStarted;
#endif


//
// Global state variables indicating which stage of shutdown we are in
//
GVAL_DECL(DWORD, g_fEEShutDown);
EXTERN DWORD g_fFastExitProcess;
EXTERN BOOL g_fFatalErrorOccurredOnGCThread;
EXTERN Volatile<LONG> g_fForbidEnterEE;
GVAL_DECL(bool, g_fProcessDetach);
#ifdef EnC_SUPPORTED
GVAL_DECL(bool, g_metadataUpdatesApplied);
#endif
EXTERN bool g_fManagedAttach;
EXTERN bool g_fNoExceptions;

// Indicates whether we're executing shut down as a result of DllMain
// (DLL_PROCESS_DETACH). See comments at code:EEShutDown for details.
inline BOOL    IsAtProcessExit()
{
    SUPPORTS_DAC;
    return g_fProcessDetach;
}

enum FWStatus
{
    FWS_WaitInterrupt = 0x00000001,
};

EXTERN DWORD g_FinalizerWaiterStatus;

#if defined(TARGET_UNIX) && defined(FEATURE_EVENT_TRACE)
extern Volatile<BOOL> g_TriggerHeapDump;
#endif // TARGET_UNIX

#ifndef DACCESS_COMPILE
//
// Allow use of native images?
//
extern bool g_fAllowNativeImages;

//
// Default install library
//
EXTERN const WCHAR g_pwBaseLibrary[];
EXTERN const WCHAR g_pwBaseLibraryName[];
EXTERN const char g_psBaseLibrary[];
EXTERN const char g_psBaseLibraryName[];
EXTERN const char g_psBaseLibrarySatelliteAssemblyName[];

#endif // DACCESS_COMPILE

//
// Do we own the lifetime of the process, ie. is it an EXE?
//
EXTERN bool g_fWeControlLifetime;

#ifdef _DEBUG
// The following should only be used for assertions.  (Famous last words).
EXTERN bool dbg_fDrasticShutdown;
#endif
EXTERN bool g_fInControlC;

// There is a global table of prime numbers that's available for e.g. hashing
extern const DWORD g_rgPrimes[71];

//
// Macros to check debugger and profiler settings.
//
inline bool CORDebuggerPendingAttach()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;
    // If we're in rude shutdown, then pretend the debugger is detached.
    // We want shutdown to be as simple as possible, so this avoids
    // us trying to do elaborate operations while exiting.
    return (g_CORDebuggerControlFlags & DBCF_PENDING_ATTACH) && !IsAtProcessExit();
}

inline bool CORDebuggerAttached()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;
    // If we're in rude shutdown, then pretend the debugger is detached.
    // We want shutdown to be as simple as possible, so this avoids
    // us trying to do elaborate operations while exiting.
    return (g_CORDebuggerControlFlags & DBCF_ATTACHED) && !IsAtProcessExit();
}

#define CORDebuggerAllowJITOpts(dwDebuggerBits)           \
    (((dwDebuggerBits) & DACF_ALLOW_JIT_OPTS)             \
     ||                                                   \
     ((g_CORDebuggerControlFlags & DBCF_ALLOW_JIT_OPT) && \
      !((dwDebuggerBits) & DACF_USER_OVERRIDE)))

#define CORDebuggerEnCMode(dwDebuggerBits)                         \
    ((dwDebuggerBits) & DACF_ENC_ENABLED)

#define CORDebuggerTraceCall() \
    (CORDebuggerAttached() && GetThread()->IsTraceCall())



//
// Define stuff for precedence between profiling and debugging
// flags that can both be set.
//

#if defined(PROFILING_SUPPORTED) || defined(PROFILING_SUPPORTED_DATA)

#ifdef DEBUGGING_SUPPORTED

#define CORDisableJITOptimizations(dwDebuggerBits)        \
         (CORProfilerDisableOptimizations() ||            \
          !CORDebuggerAllowJITOpts(dwDebuggerBits))

#else // !DEBUGGING_SUPPORTED

#define CORDisableJITOptimizations(dwDebuggerBits)        \
         CORProfilerDisableOptimizations()

#endif// DEBUGGING_SUPPORTED

#else // !defined(PROFILING_SUPPORTED) && !defined(PROFILING_SUPPORTED_DATA)

#ifdef DEBUGGING_SUPPORTED

#define CORDisableJITOptimizations(dwDebuggerBits)        \
          !CORDebuggerAllowJITOpts(dwDebuggerBits)

#else // DEBUGGING_SUPPORTED

#define CORDisableJITOptimizations(dwDebuggerBits) FALSE

#endif// DEBUGGING_SUPPORTED

#endif// defined(PROFILING_SUPPORTED) || defined(PROFILING_SUPPORTED_DATA)



#ifndef TARGET_UNIX
GVAL_DECL(SIZE_T, g_runtimeLoadedBaseAddress);
GVAL_DECL(SIZE_T, g_runtimeVirtualSize);
#endif // !TARGET_UNIX


#ifndef MAXULONG
#define MAXULONG    0xffffffff
#endif

#ifndef MAXULONGLONG
#define MAXULONGLONG                     UI64(0xffffffffffffffff)
#endif

struct TPIndex
{
    DWORD m_dwIndex;
    TPIndex ()
    : m_dwIndex(0)
    {}
    explicit TPIndex (DWORD id)
    : m_dwIndex(id)
    {}
    BOOL operator==(const TPIndex& tpindex) const
    {
        return m_dwIndex == tpindex.m_dwIndex;
    }
    BOOL operator!=(const TPIndex& tpindex) const
    {
        return m_dwIndex != tpindex.m_dwIndex;
    }
};

// Every Module is assigned a ModuleIndex, regardless of whether the Module is domain
// neutral or domain specific. When a domain specific Module is unloaded, its ModuleIndex
// can be reused.

// ModuleIndexes are not the same as ModuleIDs. The main purpose of a ModuleIndex is
// to have a compact way to refer to any Module (domain neutral or domain specific).
// The main purpose of a ModuleID is to facilitate looking up the DomainLocalModule
// that corresponds to a given Module in a given AppDomain.

struct ModuleIndex
{
    SIZE_T m_dwIndex;
    ModuleIndex ()
    : m_dwIndex(0)
    {}
    explicit ModuleIndex (SIZE_T id)
    : m_dwIndex(id)
    { LIMITED_METHOD_DAC_CONTRACT; }
    BOOL operator==(const ModuleIndex& ad) const
    {
        return m_dwIndex == ad.m_dwIndex;
    }
    BOOL operator!=(const ModuleIndex& ad) const
    {
        return m_dwIndex != ad.m_dwIndex;
    }
};

//-----------------------------------------------------------------------------
// GSCookies (guard-stack cookies) for detecting buffer overruns
//-----------------------------------------------------------------------------

typedef DPTR(GSCookie) PTR_GSCookie;

#ifdef _MSC_VER
#define READONLY_ATTR
#else
#ifdef __APPLE__
#define READONLY_ATTR_ARGS section("__DATA,__const")
#else
#define READONLY_ATTR_ARGS section(".rodata")
#endif
#define READONLY_ATTR __attribute__((READONLY_ATTR_ARGS))
#endif

#ifndef DACCESS_COMPILE
// const is so that it gets placed in the .text section (which is read-only)
// volatile is so that accesses to it do not get optimized away because of the const
//

extern "C" RAW_KEYWORD(volatile) READONLY_ATTR const GSCookie s_gsCookie;

inline
GSCookie * GetProcessGSCookiePtr() { return  const_cast<GSCookie *>(&s_gsCookie); }

#else

extern __GlobalVal< GSCookie > s_gsCookie;

inline
PTR_GSCookie GetProcessGSCookiePtr() { return  PTR_GSCookie(&s_gsCookie); }

#endif //!DACCESS_COMPILE

inline
GSCookie GetProcessGSCookie() { return *(RAW_KEYWORD(volatile) GSCookie *)(&s_gsCookie); }

// Passed to JitManager APIs to determine whether to avoid calling into the host.
// The profiling API stackwalking uses this to ensure to avoid re-entering the host
// (particularly SQL) from a hijacked thread.
enum HostCallPreference
{
    AllowHostCalls,
    NoHostCalls,
};

#ifdef TARGET_WINDOWS
typedef BOOL(WINAPI* PINITIALIZECONTEXT2)(PVOID Buffer, DWORD ContextFlags, PCONTEXT* Context, PDWORD ContextLength, ULONG64 XStateCompactionMask);
extern PINITIALIZECONTEXT2 g_pfnInitializeContext2;

#ifdef TARGET_X86
typedef VOID(__cdecl* PRTLRESTORECONTEXT)(PCONTEXT ContextRecord, struct _EXCEPTION_RECORD* ExceptionRecord);
extern PRTLRESTORECONTEXT g_pfnRtlRestoreContext;
#endif // TARGET_X86

#endif // TARGET_WINDOWS

#endif /* _VARS_HPP */
