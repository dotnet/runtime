// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// ProfilingHelper.h
// 

//
// Declaration of helper classes used for miscellaneous purposes within the
// profiling API
//

// ======================================================================================

#ifndef __PROFILING_HELPER_H__
#define __PROFILING_HELPER_H__

#ifndef PROFILING_SUPPORTED
#error PROFILING_SUPPORTED is not set. Do not include ProfilingHelper.h.
#endif

#include <windows.h>

#include "corprof.h"
#include "eeprofinterfaces.h"

#define COM_METHOD HRESULT STDMETHODCALLTYPE

#ifdef _DEBUG
// On DEBUG builds, setting the COMPLUS_ProfAPIFault to a bitmask of the flags
// below forces the Profiling API to return failures at various points.
// Useful for event log testing.  Also see code:ProfilingAPIUtility.ShouldInjectProfAPIFault
enum ProfAPIFaultFlags
{
    // Forces the startup path to log an IDS_E_PROF_INTERNAL_INIT error
    kProfAPIFault_StartupInternal   = 0x00001,
};
#endif // _DEBUG

class SidBuffer;

//---------------------------------------------------------------------------------------
// Static-only class to coordinate initialization of the various profiling API
// structures, plus other utility stuff.
//
class ProfilingAPIUtility
{
public:
    static HRESULT InitializeProfiling();
    static HRESULT LoadProfilerForAttach(
        const CLSID * pClsid,
        LPCWSTR wszProfilerDLL,
        LPVOID pvClientData,
        UINT cbClientData,
        DWORD dwConcurrentGCWaitTimeoutInMs);

    static void TerminateProfiling();
    static void LogProfError(int iStringResourceID, ...);
    static void LogProfInfo(int iStringResourceID, ...);
    static void LogNoInterfaceError(REFIID iidRequested, LPCWSTR wszClsid);
    INDEBUG(static BOOL ShouldInjectProfAPIFault(ProfAPIFaultFlags faultFlag);)
#ifndef FEATURE_PAL
    static HRESULT GetCurrentProcessUserSid(PSID * ppsid);
#endif // !FEATURE_PAL

    // helper functions for profiler evacuation counter holder
    static void IncEvacuationCounter(Thread * pThread);
    static void DecEvacuationCounter(Thread * pThread);

    // See code:ProfilingAPIUtility::InitializeProfiling#LoadUnloadCallbackSynchronization
    static CRITSEC_COOKIE GetStatusCrst();

private:
    // ---------------------------------------------------------------------------------------
    // Enum used in LoadProfiler() to differentiate whether we're loading the profiler
    // for startup or for attach
    enum LoadType
    {
        kStartupLoad,
        kAttachLoad,
    };

    // Allocated lazily the first time it's needed, and then remains allocated until the
    // process exits.
    static SidBuffer * s_pSidBuffer;

    // See code:ProfilingAPIUtility::InitializeProfiling#LoadUnloadCallbackSynchronization
    static CRITSEC_COOKIE s_csStatus;

    // Static-only class.  Private constructor enforces you don't try to make an instance
    ProfilingAPIUtility() {}

    static HRESULT PerformDeferredInit();
    static HRESULT LoadProfiler(
        LoadType loadType,
        const CLSID * pClsid,
        LPCWSTR wszClsid,
        LPCWSTR wszProfilerDLL,
        LPVOID pvClientData,
        UINT cbClientData,
        DWORD dwConcurrentGCWaitTimeoutInMs = INFINITE);
    static HRESULT ProfilerCLSIDFromString(__inout_z LPWSTR wszClsid, CLSID * pClsid);
    static HRESULT AttemptLoadProfilerForStartup();

    static void AppendSupplementaryInformation(int iStringResource, SString * pString);

    static void LogProfEventVA(
        int iStringResourceID, 
        WORD wEventType,
        va_list insertionArgs);
};


//---------------------------------------------------------------------------------------
// When we call into profiler code, we push one of these babies onto the stack to
// remember on the Thread how the profiler was called.  If the profiler calls back into us,
// we use the flags that this set to authorize.
//
class SetCallbackStateFlagsHolder
{
public:
    SetCallbackStateFlagsHolder(DWORD dwFlags);
    ~SetCallbackStateFlagsHolder();
    
private:
    Thread *   m_pThread;
    DWORD      m_dwOriginalFullState;
};

#endif //__PROFILING_HELPER_H__
