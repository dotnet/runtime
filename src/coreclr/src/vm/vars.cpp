// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// vars.cpp - Global Var definitions
//



#include "common.h"
#include "vars.hpp"
#include "cordbpriv.h"
#include "eeprofinterfaces.h"
#include "bbsweep.h"

#ifndef DACCESS_COMPILE
//
// Allow use of native images?
//
bool g_fAllowNativeImages = true;

//
// Default install library
//
const WCHAR g_pwBaseLibrary[]     = CoreLibName_IL_W;
const WCHAR g_pwBaseLibraryName[] = CoreLibName_W;
const char g_psBaseLibrary[]      = CoreLibName_IL_A;
const char g_psBaseLibraryName[]  = CoreLibName_A;
const char g_psBaseLibrarySatelliteAssemblyName[]  = CoreLibSatelliteName_A;

#ifdef FEATURE_COMINTEROP
const WCHAR g_pwBaseLibraryTLB[]  = CoreLibName_TLB_W;
const char g_psBaseLibraryTLB[]   = CoreLibName_TLB_A;
#endif  // FEATURE_COMINTEROP

Volatile<LONG>       g_TrapReturningThreads;

HINSTANCE            g_pMSCorEE;
BBSweep              g_BBSweep;

#ifdef _DEBUG
// next two variables are used to enforce an ASSERT in Thread::DbgFindThread
// that does not allow g_TrapReturningThreads to creep up unchecked.
Volatile<LONG>       g_trtChgStamp = 0;
Volatile<LONG>       g_trtChgInFlight = 0;

char *               g_ExceptionFile;   // Source of the last thrown exception (COMPLUSThrow())
DWORD                g_ExceptionLine;   // ... ditto ...
void *               g_ExceptionEIP;    // Managed EIP of the last guy to call JITThrow.
#endif // _DEBUG
void *               g_LastAccessViolationEIP;  // The EIP of the place we last threw an AV.   Used to diagnose stress issues.  

#endif // #ifndef DACCESS_COMPILE
GPTR_IMPL(IdDispenser,       g_pThinLockThreadIdDispenser);

GPTR_IMPL(IdDispenser,       g_pModuleIndexDispenser);

IBCLogger                    g_IBCLogger;

// For [<I1, etc. up to and including [Object
GARY_IMPL(PTR_ArrayTypeDesc, g_pPredefinedArrayTypes, ELEMENT_TYPE_MAX);

GPTR_IMPL(EEConfig, g_pConfig);     // configuration data (from the registry)

GPTR_IMPL(MethodTable,      g_pObjectClass);
GPTR_IMPL(MethodTable,      g_pRuntimeTypeClass);
GPTR_IMPL(MethodTable,      g_pCanonMethodTableClass);  // System.__Canon
GPTR_IMPL(MethodTable,      g_pStringClass);
GPTR_IMPL(MethodTable,      g_pArrayClass);
GPTR_IMPL(MethodTable,      g_pSZArrayHelperClass);
GPTR_IMPL(MethodTable,      g_pNullableClass);
GPTR_IMPL(MethodTable,      g_pExceptionClass);
GPTR_IMPL(MethodTable,      g_pThreadAbortExceptionClass);
GPTR_IMPL(MethodTable,      g_pOutOfMemoryExceptionClass);
GPTR_IMPL(MethodTable,      g_pStackOverflowExceptionClass);
GPTR_IMPL(MethodTable,      g_pExecutionEngineExceptionClass);
GPTR_IMPL(MethodTable,      g_pDelegateClass);
GPTR_IMPL(MethodTable,      g_pMulticastDelegateClass);
GPTR_IMPL(MethodTable,      g_pValueTypeClass);
GPTR_IMPL(MethodTable,      g_pEnumClass);
GPTR_IMPL(MethodTable,      g_pThreadClass);
GPTR_IMPL(MethodTable,      g_pCriticalFinalizerObjectClass);
GPTR_IMPL(MethodTable,      g_pAsyncFileStream_AsyncResultClass);
GPTR_IMPL(MethodTable,      g_pFreeObjectMethodTable);
GPTR_IMPL(MethodTable,      g_pOverlappedDataClass);

GPTR_IMPL(MethodTable,      g_TypedReferenceMT);

GPTR_IMPL(MethodTable,      g_pByteArrayMT);

#ifdef FEATURE_COMINTEROP
GPTR_IMPL(MethodTable,      g_pBaseCOMObject);
GPTR_IMPL(MethodTable,      g_pBaseRuntimeClass);
#endif

#ifdef FEATURE_ICASTABLE
GPTR_IMPL(MethodTable,      g_pICastableInterface);
#endif // FEATURE_ICASTABLE


GPTR_IMPL(MethodDesc,       g_pPrepareConstrainedRegionsMethod);
GPTR_IMPL(MethodDesc,       g_pExecuteBackoutCodeHelperMethod);

GPTR_IMPL(MethodDesc,       g_pObjectCtorMD);
GPTR_IMPL(MethodDesc,       g_pObjectFinalizerMD);

GPTR_IMPL(Thread,g_pFinalizerThread);
GPTR_IMPL(Thread,g_pSuspensionThread);

// Global SyncBlock cache
GPTR_IMPL(SyncTableEntry,g_pSyncTable);

#if defined(ENABLE_PERF_COUNTERS) || defined(FEATURE_EVENT_TRACE)
DWORD g_dwHandles = 0;
#endif // ENABLE_PERF_COUNTERS || FEATURE_EVENT_TRACE

#ifdef STRESS_LOG
GPTR_IMPL_INIT(StressLog, g_pStressLog, &StressLog::theLog);
#endif

#ifdef FEATURE_COMINTEROP
// Global RCW cleanup list
GPTR_IMPL(RCWCleanupList,g_pRCWCleanupList);
#endif // FEATURE_COMINTEROP


#ifndef DACCESS_COMPILE

// <TODO> @TODO Remove eventually - </TODO> determines whether the verifier throws an exception when something fails
bool                g_fVerifierOff;

#ifndef FEATURE_CORECLR
IAssemblyUsageLog   *g_pIAssemblyUsageLogGac;
#endif

// <TODO> @TODO - PROMOTE. </TODO>
OBJECTHANDLE         g_pPreallocatedOutOfMemoryException;
OBJECTHANDLE         g_pPreallocatedStackOverflowException;
OBJECTHANDLE         g_pPreallocatedExecutionEngineException;
OBJECTHANDLE         g_pPreallocatedRudeThreadAbortException;
OBJECTHANDLE         g_pPreallocatedThreadAbortException;
OBJECTHANDLE         g_pPreallocatedSentinelObject;
OBJECTHANDLE         g_pPreallocatedBaseException;

#ifdef FEATURE_CAS_POLICY
CertificateCache *g_pCertificateCache = NULL;
#endif

// 
//
// Global System Info
//
SYSTEM_INFO g_SystemInfo;

// Configurable constants used across our spin locks
// Initialization here is necessary so that we have meaningful values before the runtime is started
// These initial values were selected to match the defaults, but anything reasonable is close enough
SpinConstants g_SpinConstants = { 
    50,        // dwInitialDuration 
    40000,     // dwMaximumDuration - ideally (20000 * max(2, numProc))
    3,         // dwBackoffFactor
    10         // dwRepetitions
};

// support for Event Tracing for Windows (ETW)
ETW::CEtwTracer * g_pEtwTracer = NULL;

#endif // #ifndef DACCESS_COMPILE

#ifdef FEATURE_IPCMAN
// support for IPCManager 
GPTR_IMPL(IPCWriterInterface, g_pIPCManagerInterface);
#endif // FEATURE_IPCMAN

//
// Support for the COM+ Debugger.
//
GPTR_IMPL(DebugInterface,     g_pDebugInterface);
// A managed debugger may set this flag to high from out of process.
GVAL_IMPL_INIT(DWORD,         g_CORDebuggerControlFlags, DBCF_NORMAL_OPERATION);

#ifdef DEBUGGING_SUPPORTED
GPTR_IMPL(EEDbgInterfaceImpl, g_pEEDbgInterfaceImpl);
#endif // DEBUGGING_SUPPORTED

#if defined(PROFILING_SUPPORTED_DATA) || defined(PROFILING_SUPPPORTED)
// Profiling support
HINSTANCE           g_pDebuggerDll = NULL;

GVAL_IMPL(ProfControlBlock, g_profControlBlock);
#endif // defined(PROFILING_SUPPORTED_DATA) || defined(PROFILING_SUPPPORTED)

#ifndef DACCESS_COMPILE

// Global default for Concurrent GC. The default is value is 1
int g_IGCconcurrent = 1;

int g_IGCHoardVM = 0;

#ifdef GCTRIMCOMMIT

int g_IGCTrimCommit = 0;

#endif

BOOL g_fEnableETW = FALSE;

BOOL g_fEnableARM = FALSE;

//
// Global state variable indicating if the EE is in its init phase.
//
bool g_fEEInit = false;

//
// Global state variables indicating which stage of shutdown we are in
//

#endif // #ifndef DACCESS_COMPILE

// See comments at code:EEShutDown for details on how and why this gets set.  Use
// code:IsAtProcessExit to read this.
GVAL_IMPL(bool, g_fProcessDetach);

GVAL_IMPL_INIT(DWORD, g_fEEShutDown, 0);

#ifndef FEATURE_PAL
GVAL_IMPL(SIZE_T, g_runtimeLoadedBaseAddress);
GVAL_IMPL(SIZE_T, g_runtimeVirtualSize);
#endif // !FEATURE_PAL

#ifndef DACCESS_COMPILE

Volatile<LONG> g_fForbidEnterEE = false;
bool g_fFinalizerRunOnShutDown = false;
bool g_fManagedAttach = false;
bool g_fNoExceptions = false;
#ifdef FEATURE_COMINTEROP
bool g_fShutDownCOM = false;
#endif //FEATURE_COMINTEROP

DWORD g_FinalizerWaiterStatus = 0;

const WCHAR g_pwzClickOnceEnv_FullName[] = W("__COR_COMMAND_LINE_APP_FULL_NAME__");
const WCHAR g_pwzClickOnceEnv_Manifest[] = W("__COR_COMMAND_LINE_MANIFEST__");
const WCHAR g_pwzClickOnceEnv_Parameter[] = W("__COR_COMMAND_LINE_PARAMETER__");

#ifdef FEATURE_LOADER_OPTIMIZATION
DWORD g_dwGlobalSharePolicy = AppDomain::SHARE_POLICY_UNSPECIFIED;
#endif

//
// Do we own the lifetime of the process, ie. is it an EXE?
//
bool g_fWeControlLifetime = false;

#ifdef _DEBUG
// The following should only be used for assertions.  (Famous last words).
bool dbg_fDrasticShutdown = false;
#endif
bool g_fInControlC = false;

//
// Cached command line file provided by the host.
//
LPWSTR g_pCachedCommandLine = NULL;
LPWSTR g_pCachedModuleFileName = 0;

// host configuration file. If set, it is added to every AppDomain (fusion context)
LPCWSTR  g_pszHostConfigFile = NULL;
SIZE_T  g_dwHostConfigFile = 0;

// AppDomainManager assembly and type names provided as environment variables.
LPWSTR g_wszAppDomainManagerAsm = NULL;
LPWSTR g_wszAppDomainManagerType = NULL;
bool g_fDomainManagerInitialized = false;

//
// IJW needs the shim HINSTANCE
//
HINSTANCE g_hInstShim = NULL;

char g_Version[] = VER_PRODUCTVERSION_STR;

#endif // #ifndef DACCESS_COMPILE

#ifdef DACCESS_COMPILE

void OBJECTHANDLE_EnumMemoryRegions(OBJECTHANDLE handle)
{
    SUPPORTS_DAC;
    PTR_TADDR ref = PTR_TADDR(handle);
    if (ref.IsValid())
    {
        ref.EnumMem();
        
        PTR_Object obj = PTR_Object(*ref);
        if (obj.IsValid())
        {
            obj->EnumMemoryRegions();
        }
    }
}

void OBJECTREF_EnumMemoryRegions(OBJECTREF ref)
{
    if (ref.IsValid())
    {
        ref->EnumMemoryRegions();
    }
}

#endif // #ifdef DACCESS_COMPILE

#ifndef DACCESS_COMPILE
//
// We need the following to be the compiler's notion of volatile.
//
extern "C" RAW_KEYWORD(volatile) const GSCookie s_gsCookie = 0;

#else
__GlobalVal< GSCookie > s_gsCookie(&g_dacGlobals.dac__s_gsCookie);
#endif //!DACCESS_COMPILE

BOOL IsCompilationProcess()
{
    LIMITED_METHOD_DAC_CONTRACT;
#if defined(FEATURE_NATIVE_IMAGE_GENERATION) && !defined(DACCESS_COMPILE)
    return g_pCEECompileInfo != NULL;
#else
    return FALSE;
#endif
}

//==============================================================================

enum NingenState
{
    kNotInitialized = 0,
    kNingenEnabled = 1,
    kNingenDisabled = 2,
};

extern int g_ningenState;
int g_ningenState = kNotInitialized;

// Removes all execution of managed or third-party code in the ngen compilation process.
BOOL NingenEnabled()
{
    LIMITED_METHOD_CONTRACT;

#ifdef CROSSGEN_COMPILE
    // Always enable ningen for cross-compile
    return TRUE;
#else // CROSSGEN_COMPILE

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
    // Note that ningen is enabled by default to get byte-to-byte identical NGen images between native compile and cross-compile
    if (g_ningenState == kNotInitialized)
    {
        // This code must be idempotent as we don't have a lock to prevent a race to initialize g_ningenState.
        g_ningenState = (IsCompilationProcess() && (0 != CLRConfig::GetConfigValue(CLRConfig::INTERNAL_Ningen))) ? kNingenEnabled : kNingenDisabled;
    }

    return g_ningenState == kNingenEnabled;
#else
    return FALSE;
#endif

#endif // CROSSGEN_COMPILE
}
