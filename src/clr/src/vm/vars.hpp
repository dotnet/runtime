//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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

#ifndef CLR_STANDALONE_BINDER
#include "util.hpp"
#include <corpriv.h>
#include <cordbpriv.h>

#ifndef FEATURE_CORECLR
#include <metahost.h>
#endif // !FEATURE_CORECLR

#include "eeprofinterfaces.h"
#include "eehash.h"

#ifdef FEATURE_CAS_POLICY
#include "certificatecache.h"
#endif

#endif //CLR_STANDALONE_BINDER

#include "profilepriv.h"

class ClassLoader;
class LoaderHeap;
class GCHeap;
class Object;
class StringObject;
class TransparentProxyObject;
class ArrayClass;
class MethodTable;
class MethodDesc;
class SyncBlockCache;
class SyncTableEntry;
class ThreadStore;
class IPCWriterInterface;
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
struct IAssemblyUsageLog;

//
// object handles are opaque types that track object pointers
//
#ifndef DACCESS_COMPILE

struct OBJECTHANDLE__
{
    void* unused;
};
typedef struct OBJECTHANDLE__* OBJECTHANDLE;

#else

typedef TADDR OBJECTHANDLE;

#endif

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
        class TransparentProxyObject* m_asTP;

        class ReflectClassBaseObject* m_asReflectClass;
#ifdef FEATURE_COMPRESSEDSTACK        
        class CompressedStackObject* m_asCompressedStack;
#endif // #ifdef FEATURE_COMPRESSEDSTACK
#if defined(FEATURE_IMPERSONATION) || defined(FEATURE_COMPRESSEDSTACK)
        class SecurityContextObject* m_asSecurityContext;
#endif // #if defined(FEATURE_IMPERSONATION) || defined(FEATURE_COMPRESSEDSTACK)
        class ExecutionContextObject* m_asExecutionContext;
        class AppDomainBaseObject* m_asAppDomainBase;
        class PermissionSetObject* m_asPermissionSetObject;
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
#define ObjectToSTRINGBUFFERREF(obj)    (STRINGBUFFERREF(obj))
#define STRINGBUFFERREFToObject(objref) (*( (StringBufferObject**) &(objref) ))

#else   // _DEBUG_IMPL

#define VALIDATEOBJECTREF(objref)
#define VALIDATEOBJECT(obj)

#define ObjectToOBJECTREF(obj)    ((PTR_Object) (obj))
#define OBJECTREFToObject(objref) ((PTR_Object) (objref))
#define ObjectToSTRINGREF(obj)    ((PTR_StringObject) (obj))
#define STRINGREFToObject(objref) ((PTR_StringObject) (objref))
#define ObjectToSTRINGBUFFERREF(obj)    ((Ptr_StringBufferObject) (obj))
#define STRINGBUFFERREFToObject(objref) ((Ptr_StringBufferObject) (objref))

#endif // _DEBUG_IMPL


// <TODO> Get rid of these!  Don't use them any more!</TODO>
#define MAX_CLASSNAME_LENGTH    1024
#define MAX_NAMESPACE_LENGTH    1024

class EEConfig;
class ClassLoaderList;
class Module;
class ArrayTypeDesc;

#ifndef BINDER

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
EXTERN char *               g_ExceptionFile;
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
GPTR_DECL(MethodTable,      g_pCriticalFinalizerObjectClass);
GPTR_DECL(MethodTable,      g_pAsyncFileStream_AsyncResultClass);
GPTR_DECL(MethodTable,      g_pOverlappedDataClass);

GPTR_DECL(MethodTable,      g_ArgumentHandleMT);
GPTR_DECL(MethodTable,      g_ArgIteratorMT);
GPTR_DECL(MethodTable,      g_TypedReferenceMT);

#ifdef FEATURE_COMINTEROP
GPTR_DECL(MethodTable,      g_pBaseCOMObject);
GPTR_DECL(MethodTable,      g_pBaseRuntimeClass);
#endif

#ifdef FEATURE_ICASTABLE
GPTR_DECL(MethodTable,      g_pICastableInterface);
#endif // FEATURE_ICASTABLE

GPTR_DECL(MethodDesc,       g_pPrepareConstrainedRegionsMethod);
GPTR_DECL(MethodDesc,       g_pExecuteBackoutCodeHelperMethod);

GPTR_DECL(MethodDesc,       g_pObjectCtorMD);
GPTR_DECL(MethodDesc,       g_pObjectFinalizerMD);

//<TODO> @TODO Remove eventually - determines whether the verifier throws an exception when something fails</TODO>
EXTERN bool                 g_fVerifierOff;

#ifndef FEATURE_CORECLR
EXTERN IAssemblyUsageLog   *g_pIAssemblyUsageLogGac;
#endif

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
#endif // !BINDER

GPTR_DECL(Thread,g_pFinalizerThread);
GPTR_DECL(Thread,g_pSuspensionThread);

// Global SyncBlock cache
typedef DPTR(SyncTableEntry) PTR_SyncTableEntry;
GPTR_DECL(SyncTableEntry, g_pSyncTable);

#if !defined(BINDER)

#ifdef FEATURE_COMINTEROP
// Global RCW cleanup list
typedef DPTR(RCWCleanupList) PTR_RCWCleanupList;
GPTR_DECL(RCWCleanupList,g_pRCWCleanupList);
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_CAS_POLICY
EXTERN CertificateCache *g_pCertificateCache;
#endif 

#ifdef FEATURE_IPCMAN
// support for IPCManager
typedef DPTR(IPCWriterInterface) PTR_IPCWriterInterface;
GPTR_DECL(IPCWriterInterface,  g_pIPCManagerInterface);
#endif // FEATURE_IPCMAN

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
extern BOOL g_fEnableARM;

// Returns a BOOL to indicate if the runtime is active or not
BOOL IsRuntimeActive(); 

//
// Can we run managed code?
//
struct LoaderLockCheck
{
    enum kind
    {
        ForMDA,
        ForCorrectness,
        None,
    };
};
BOOL CanRunManagedCode(LoaderLockCheck::kind checkKind, HINSTANCE hInst = 0);
inline BOOL CanRunManagedCode(HINSTANCE hInst = 0)
{
    return CanRunManagedCode(LoaderLockCheck::ForMDA, hInst);
}

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

#if !defined(FEATURE_CORECLR) && !defined(CROSSGEN_COMPILE)
//
// Pointer to the activated CLR interface provided by the shim.
//
EXTERN ICLRRuntimeInfo *g_pCLRRuntime;
#endif

//
// Global state variables indicating which stage of shutdown we are in
//
GVAL_DECL(DWORD, g_fEEShutDown);
EXTERN DWORD g_fFastExitProcess;
#ifndef DACCESS_COMPILE
EXTERN BOOL g_fSuspendOnShutdown;
EXTERN BOOL g_fSuspendFinalizerOnShutdown;
#endif // DACCESS_COMPILE
EXTERN Volatile<LONG> g_fForbidEnterEE;
EXTERN bool g_fFinalizerRunOnShutDown;
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

#ifdef FEATURE_COMINTEROP
EXTERN const WCHAR g_pwBaseLibraryTLB[];
EXTERN const char g_psBaseLibraryTLB[];
#endif  // FEATURE_COMINTEROP
#endif // DACCESS_COMPILE

EXTERN const WCHAR g_pwzClickOnceEnv_FullName[];
EXTERN const WCHAR g_pwzClickOnceEnv_Manifest[];
EXTERN const WCHAR g_pwzClickOnceEnv_Parameter[];

#ifdef FEATURE_LOADER_OPTIMIZATION
EXTERN DWORD g_dwGlobalSharePolicy;
#endif

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
// Cached command line file provided by the host.
//
extern LPWSTR g_pCachedCommandLine;
extern LPWSTR g_pCachedModuleFileName;

//
// Host configuration file. One per process.
//
extern LPCWSTR g_pszHostConfigFile;
extern SIZE_T  g_dwHostConfigFile;

// AppDomainManager type
extern LPWSTR g_wszAppDomainManagerAsm;
extern LPWSTR g_wszAppDomainManagerType;
extern bool g_fDomainManagerInitialized;

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

#endif /* !BINDER */

#ifndef MAXULONG
#define MAXULONG    0xffffffff
#endif

#ifndef MAXULONGLONG
#define MAXULONGLONG                     UI64(0xffffffffffffffff)
#endif

// #ADID_vs_ADIndex
// code:ADID is an ID for an appdomain that is sparse and remains unique within the process for the lifetime of the process.
// Remoting and (I believe) the thread pool use the former as a way of referring to appdomains outside of their normal lifetime safely.
// Interop also uses ADID to handle issues involving unloaded domains.
// 
// code:ADIndex is an ID for an appdomain that's dense and may be reused once the appdomain is unloaded.
// This is useful for fast array based lookup from a number to an appdomain property.  
struct ADIndex
{
    DWORD m_dwIndex;
    ADIndex ()
    : m_dwIndex(0)
    {}
    explicit ADIndex (DWORD id)
    : m_dwIndex(id)
    {
        SUPPORTS_DAC;
    }
    BOOL operator==(const ADIndex& ad) const
    {
        return m_dwIndex == ad.m_dwIndex;
    }
    BOOL operator!=(const ADIndex& ad) const
    {
        return m_dwIndex != ad.m_dwIndex;
    }
};

// An ADID is a number that represents an appdomain.  They are allcoated with code:SystemDomain::GetNewAppDomainId
// ADIDs are NOT reused today, so they are unique even after the appdomain dies.  
// 
// see also code:BaseDomain::m_dwId 
// see also code:ADIndex
// see also code:ADIndex#ADID_vs_ADIndex
struct ADID
{
    DWORD m_dwId;
    ADID ()
    : m_dwId(0)
    {LIMITED_METHOD_CONTRACT;}
    explicit ADID (DWORD id)
    : m_dwId(id)
    {LIMITED_METHOD_CONTRACT;}
    BOOL operator==(const ADID& ad) const
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_dwId == ad.m_dwId;
    }
    BOOL operator!=(const ADID& ad) const
    {
        LIMITED_METHOD_CONTRACT;
        return m_dwId != ad.m_dwId;
    }
};

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

#ifndef CLR_STANDALONE_BINDER
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
#endif

// Returns true if this is NGen compilation process.
// This is a superset of CompilationDomain::IsCompilationDomain() as there is more
// than one AppDomain in ngen (the DefaultDomain)
BOOL IsCompilationProcess();

// Flag for cross-platform ngen: Removes all execution of managed or third-party code in the ngen compilation process.
BOOL NingenEnabled();

// Passed to JitManager APIs to determine whether to avoid calling into the host. 
// The profiling API stackwalking uses this to ensure to avoid re-entering the host 
// (particularly SQL) from a hijacked thread.
enum HostCallPreference
{
    AllowHostCalls,
    NoHostCalls,
};

#endif /* _VARS_HPP */
#endif /* !CLR_STANDALONE_BINDER */
