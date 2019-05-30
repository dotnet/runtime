// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
#ifdef  _WIN64
typedef __int64             intptr_t;
#else
typedef int                 intptr_t;
#endif
#define _INTPTR_T_DEFINED
#endif

#ifndef _UINTPTR_T_DEFINED
#ifdef  _WIN64
typedef unsigned __int64    uintptr_t;
#else
typedef unsigned int        uintptr_t;
#endif
#define _UINTPTR_T_DEFINED
#endif

#ifndef _PTRDIFF_T_DEFINED
#ifdef  _WIN64
typedef __int64             ptrdiff_t;
#else
typedef int                 ptrdiff_t;
#endif
#define _PTRDIFF_T_DEFINED
#endif


#ifndef _SIZE_T_DEFINED
#ifdef  _WIN64
typedef unsigned __int64 size_t;
#else
typedef unsigned int     size_t;
#endif
#define _SIZE_T_DEFINED
#endif


#ifndef _WCHAR_T_DEFINED
typedef unsigned short wchar_t;
#define _WCHAR_T_DEFINED
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
#ifdef FEATURE_UTF8STRING
class Utf8StringObject;
#endif // FEATURE_UTF8STRING
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
class BBSweep;

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

            // allow explict casts
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

// the while (0) syntax below is to force a trailing semicolon on users of the macro
#define VALIDATEOBJECTREF(objref) do {if ((objref) != NULL) (objref).Validate();} while (0)
#define VALIDATEOBJECT(obj) do {if ((obj) != NULL) (obj)->Validate();} while (0)

#define ObjectToOBJECTREF(obj)     (OBJECTREF(obj))
#define OBJECTREFToObject(objref)  ((objref).operator-> ())
#define ObjectToSTRINGREF(obj)     (STRINGREF(obj))
#define STRINGREFToObject(objref)  (*( (StringObject**) &(objref) ))
#ifdef FEATURE_UTF8STRING
#define ObjectToUTF8STRINGREF(obj)   (UTF8STRINGREF(obj))
#define UTF8STRINGREFToObject(objref) (*( (Utf8StringObject**) &(objref) ))
#endif // FEATURE_UTF8STRING

#else   // _DEBUG_IMPL

#define VALIDATEOBJECTREF(objref)
#define VALIDATEOBJECT(obj)

#define ObjectToOBJECTREF(obj)    ((PTR_Object) (obj))
#define OBJECTREFToObject(objref) ((PTR_Object) (objref))
#define ObjectToSTRINGREF(obj)    ((PTR_StringObject) (obj))
#define STRINGREFToObject(objref) ((PTR_StringObject) (objref))
#ifdef FEATURE_UTF8STRING
#define ObjectToUTF8STRINGREF(obj)    ((PTR_Utf8StringObject) (obj))
#define UTF8STRINGREFToObject(objref) ((PTR_Utf8StringObject) (objref))
#endif // FEATURE_UTF8STRING

#endif // _DEBUG_IMPL


// <TODO> Get rid of these!  Don't use them any more!</TODO>
#define MAX_CLASSNAME_LENGTH    1024
#define MAX_NAMESPACE_LENGTH    1024

class EEConfig;
class ClassLoaderList;
class Module;
class ArrayTypeDesc;

#define EXTERN extern

// For [<I1, etc. up to and including [Object
GARY_DECL(PTR_ArrayTypeDesc, g_pPredefinedArrayTypes, ELEMENT_TYPE_MAX);

extern "C" Volatile<LONG>   g_TrapReturningThreads;

EXTERN HINSTANCE            g_pMSCorEE;
EXTERN BBSweep              g_BBSweep;
EXTERN IBCLogger            g_IBCLogger;

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
#ifdef FEATURE_UTF8STRING
GPTR_DECL(MethodTable,      g_pUtf8StringClass);
#endif // FEATURE_UTF8STRING
GPTR_DECL(MethodTable,      g_pArrayClass);
GPTR_DECL(MethodTable,      g_pSZArrayHelperClass);
GPTR_DECL(MethodTable,      g_pNullableClass);
GPTR_DECL(MethodTable,      g_pByReferenceClass);
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

GPTR_DECL(MethodTable,      g_pByteArrayMT);

#ifdef FEATURE_COMINTEROP
GPTR_DECL(MethodTable,      g_pBaseCOMObject);
GPTR_DECL(MethodTable,      g_pBaseRuntimeClass);
#endif

#ifdef FEATURE_ICASTABLE
GPTR_DECL(MethodTable,      g_pICastableInterface);
#endif // FEATURE_ICASTABLE

GPTR_DECL(MethodDesc,       g_pExecuteBackoutCodeHelperMethod);

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
EXTERN OBJECTHANDLE         g_pPreallocatedRudeThreadAbortException;

// We may not be able to create a normal thread abort exception if OOM or StackOverFlow.
// When this happens, we will use our pre-allocated thread abort exception.
EXTERN OBJECTHANDLE         g_pPreallocatedThreadAbortException;

// we use this as a dummy object to indicate free space in the handle tables -- this object is never visible to the world
EXTERN OBJECTHANDLE         g_pPreallocatedSentinelObject;

// We use this object to return a preallocated System.Exception instance when we have nothing
// better to return.
EXTERN OBJECTHANDLE         g_pPreallocatedBaseException;

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

#ifdef GCTRIMCOMMIT
extern int g_IGCTrimCommit;
#endif

extern BOOL g_fEnableETW;

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
EXTERN bool g_fManagedAttach;
EXTERN bool g_fNoExceptions;
#ifdef FEATURE_COMINTEROP
EXTERN bool g_fShutDownCOM;
#endif // FEATURE_COMINTEROP

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
extern ULONGLONG g_ObjFinalizeStartTime;
extern Volatile<BOOL> g_FinalizerIsRunning;
extern Volatile<ULONG> g_FinalizerLoopCount;

#if defined(FEATURE_PAL) && defined(FEATURE_EVENT_TRACE)
extern Volatile<BOOL> g_TriggerHeapDump;
#endif // FEATURE_PAL

extern LONG GetProcessedExitProcessEventCount();

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




//
// IJW needs the shim HINSTANCE
//
EXTERN HINSTANCE g_hInstShim;

#ifndef FEATURE_PAL
GVAL_DECL(SIZE_T, g_runtimeLoadedBaseAddress);
GVAL_DECL(SIZE_T, g_runtimeVirtualSize);
#endif // !FEATURE_PAL


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

#ifndef DACCESS_COMPILE
// const is so that it gets placed in the .text section (which is read-only)
// volatile is so that accesses to it do not get optimized away because of the const
//

extern "C" RAW_KEYWORD(volatile) const GSCookie s_gsCookie;

inline
GSCookie * GetProcessGSCookiePtr() { return  const_cast<GSCookie *>(&s_gsCookie); }

#else

extern __GlobalVal< GSCookie > s_gsCookie;

inline
PTR_GSCookie GetProcessGSCookiePtr() { return  PTR_GSCookie(&s_gsCookie); }

#endif //!DACCESS_COMPILE

inline
GSCookie GetProcessGSCookie() { return *(RAW_KEYWORD(volatile) GSCookie *)(&s_gsCookie); }

class CEECompileInfo;
extern CEECompileInfo *g_pCEECompileInfo;

#ifdef FEATURE_READYTORUN_COMPILER
extern bool g_fReadyToRunCompilation;
extern bool g_fLargeVersionBubble;
#endif

// Returns true if this is NGen compilation process.
// This is a superset of CompilationDomain::IsCompilationDomain() as there is more
// than one AppDomain in ngen (the DefaultDomain)
inline BOOL IsCompilationProcess()
{
#ifdef CROSSGEN_COMPILE
    return TRUE;
#else
    return FALSE;
#endif
}

// Flag for cross-platform ngen: Removes all execution of managed or third-party code in the ngen compilation process.
inline BOOL NingenEnabled()
{
#ifdef CROSSGEN_COMPILE
    return TRUE;
#else
    return FALSE;
#endif
}

// Passed to JitManager APIs to determine whether to avoid calling into the host. 
// The profiling API stackwalking uses this to ensure to avoid re-entering the host 
// (particularly SQL) from a hijacked thread.
enum HostCallPreference
{
    AllowHostCalls,
    NoHostCalls,
};

#endif /* _VARS_HPP */
