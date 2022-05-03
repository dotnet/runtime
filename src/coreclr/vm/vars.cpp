// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// vars.cpp - Global Var definitions
//



#include "common.h"
#include "vars.hpp"
#include "cordbpriv.h"
#include "eeprofinterfaces.h"

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

Volatile<int32_t>       g_TrapReturningThreads;

#ifdef _DEBUG
// next two variables are used to enforce an ASSERT in Thread::DbgFindThread
// that does not allow g_TrapReturningThreads to creep up unchecked.
Volatile<LONG>       g_trtChgStamp = 0;
Volatile<LONG>       g_trtChgInFlight = 0;

const char *         g_ExceptionFile;   // Source of the last thrown exception (COMPLUSThrow())
DWORD                g_ExceptionLine;   // ... ditto ...
void *               g_ExceptionEIP;    // Managed EIP of the last JITThrow caller.
#endif // _DEBUG
void *               g_LastAccessViolationEIP;  // The EIP of the place we last threw an AV.   Used to diagnose stress issues.

#endif // #ifndef DACCESS_COMPILE
GPTR_IMPL(IdDispenser,       g_pThinLockThreadIdDispenser);

GPTR_IMPL(IdDispenser,       g_pModuleIndexDispenser);

IBCLogger                    g_IBCLogger;

// For [<I1, etc. up to and including [Object
GARY_IMPL(TypeHandle, g_pPredefinedArrayTypes, ELEMENT_TYPE_MAX);

GPTR_IMPL(EEConfig, g_pConfig);     // configuration data (from the registry)

GPTR_IMPL(MethodTable,      g_pObjectClass);
GPTR_IMPL(MethodTable,      g_pRuntimeTypeClass);
GPTR_IMPL(MethodTable,      g_pCanonMethodTableClass);  // System.__Canon
GPTR_IMPL(MethodTable,      g_pStringClass);
GPTR_IMPL(MethodTable,      g_pArrayClass);
GPTR_IMPL(MethodTable,      g_pSZArrayHelperClass);
GPTR_IMPL(MethodTable,      g_pNullableClass);
GPTR_IMPL(MethodTable,      g_pByReferenceClass);
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
GPTR_IMPL(MethodTable,      g_pFreeObjectMethodTable);
GPTR_IMPL(MethodTable,      g_pOverlappedDataClass);

GPTR_IMPL(MethodTable,      g_TypedReferenceMT);

#ifdef FEATURE_COMINTEROP
GPTR_IMPL(MethodTable,      g_pBaseCOMObject);
#endif

GPTR_IMPL(MethodTable,      g_pIDynamicInterfaceCastableInterface);

#ifdef FEATURE_ICASTABLE
GPTR_IMPL(MethodTable,      g_pICastableInterface);
#endif // FEATURE_ICASTABLE

GPTR_IMPL(MethodDesc,       g_pObjectFinalizerMD);

GPTR_IMPL(Thread,g_pFinalizerThread);
GPTR_IMPL(Thread,g_pSuspensionThread);

// Global SyncBlock cache
GPTR_IMPL(SyncTableEntry,g_pSyncTable);

#ifdef STRESS_LOG
GPTR_IMPL_INIT(StressLog, g_pStressLog, &StressLog::theLog);
#endif

#ifdef FEATURE_COMINTEROP
// Global RCW cleanup list
GPTR_IMPL(RCWCleanupList,g_pRCWCleanupList);
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_INTEROP_DEBUGGING
GVAL_IMPL_INIT(DWORD, g_debuggerWordTLSIndex, TLS_OUT_OF_INDEXES);
#endif
GVAL_IMPL_INIT(DWORD, g_TlsIndex, TLS_OUT_OF_INDEXES);

#ifndef DACCESS_COMPILE

// <TODO> @TODO - PROMOTE. </TODO>
OBJECTHANDLE         g_pPreallocatedOutOfMemoryException;
OBJECTHANDLE         g_pPreallocatedStackOverflowException;
OBJECTHANDLE         g_pPreallocatedExecutionEngineException;
OBJECTHANDLE         g_pPreallocatedSentinelObject;

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
    10,        // dwRepetitions
    0          // dwMonitorSpinCount
};

// support for Event Tracing for Windows (ETW)
ETW::CEtwTracer * g_pEtwTracer = NULL;

#endif // #ifndef DACCESS_COMPILE

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

#ifdef EnC_SUPPORTED
GVAL_IMPL_INIT(bool, g_metadataUpdatesApplied, false);
#endif

GVAL_IMPL_INIT(DWORD, g_fEEShutDown, 0);

#ifndef TARGET_UNIX
GVAL_IMPL(SIZE_T, g_runtimeLoadedBaseAddress);
GVAL_IMPL(SIZE_T, g_runtimeVirtualSize);
#endif // !TARGET_UNIX

#ifndef DACCESS_COMPILE

Volatile<LONG> g_fForbidEnterEE = false;
bool g_fManagedAttach = false;
bool g_fNoExceptions = false;

DWORD g_FinalizerWaiterStatus = 0;

//
// Do we own the lifetime of the process, ie. is it an EXE?
//
bool g_fWeControlLifetime = false;

#ifdef _DEBUG
// The following should only be used for assertions.  (Famous last words).
bool dbg_fDrasticShutdown = false;
#endif
bool g_fInControlC = false;

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

//==============================================================================

