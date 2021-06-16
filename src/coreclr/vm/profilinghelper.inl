// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// ProfilingHelper.inl
//

//
// Inlined implementation of some helper class methods used for
// miscellaneous purposes within the profiling API
//

// ======================================================================================

#ifndef __PROFILING_HELPER_INL__
#define __PROFILING_HELPER_INL__

FORCEINLINE SetCallbackStateFlagsHolder::SetCallbackStateFlagsHolder(DWORD dwFlags)
{
    // This is called before entering a profiler.  We set the specified dwFlags on
    // the Thread object, and remember the previous flags for later.
    m_pThread = GetThreadNULLOk();
    if (m_pThread != NULL)
    {
        m_dwOriginalFullState = m_pThread->SetProfilerCallbackStateFlags(dwFlags);
    }
    else
    {
        m_dwOriginalFullState = 0;
    }
}

FORCEINLINE SetCallbackStateFlagsHolder::~SetCallbackStateFlagsHolder()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    // This is called after the profiler returns to us.  We reinstate the
    // original flag set here.
    if (m_pThread != NULL)
    {
        m_pThread->SetProfilerCallbackFullState(m_dwOriginalFullState);
    }
}

#ifdef ENABLE_CONTRACTS
//---------------------------------------------------------------------------------------
//
// This function, used only on debug builds, fetches the triggers bits from the contract
// to help verify that the contract is compatible with the flags passed in to the
// entrypoint macros.
//
// Arguments:
//      * fTriggers - If nonzero, this function asserts the contract says GC_TRIGGERS,
//          else this function asserts the contract says GC_NOTRIGGER
//
inline void AssertTriggersContract(BOOL fTriggers)
{
    // NOTE: This function cannot have contract, as this function needs to inspect the
    // contract of the calling function

    ClrDebugState * pClrDbgState = GetClrDebugState(FALSE);
    if ((pClrDbgState == NULL) || (pClrDbgState->GetContractStackTrace() == NULL))
    {
        return;
    }

    UINT testMask = pClrDbgState->GetContractStackTrace()->m_testmask;

    if (fTriggers)
    {
        // If this assert fires, the contract says GC_NOTRIGGER (or is disabled), but the
        // PROFILER_TO_CLR_ENTRYPOINT* / CLR_TO_PROFILER_ENTRYPOINT* macro implies triggers
        _ASSERTE((testMask & Contract::GC_Mask) == Contract::GC_Triggers);
    }
    else
    {
        // If this assert fires, the contract says GC_TRIGGERS, but the
        // PROFILER_TO_CLR_ENTRYPOINT* / CLR_TO_PROFILER_ENTRYPOINT* macro implies no
        // trigger
        _ASSERTE(((testMask & Contract::GC_Mask) == Contract::GC_NoTrigger) ||
            ((testMask & Contract::GC_Disabled) != 0));

    }
}
#endif //ENABLE_CONTRACTS

// ----------------------------------------------------------------------------
// ProfilingAPIUtility::LogNoInterfaceError
//
// Description:
//    Simple helper to log an IDS_E_PROF_NO_CALLBACK_IFACE event
//
// Arguments:
//    * iidRequested - IID to convert to string and log (as insertion string)
//    * wszCLSID - CLSID to log (as insertion string)
//

// static
inline void ProfilingAPIUtility::LogNoInterfaceError(REFIID iidRequested, LPCWSTR wszCLSID)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    WCHAR wszIidRequested[39];
    if (StringFromGUID2(iidRequested, wszIidRequested, lengthof(wszIidRequested)) == 0)
    {
        // This is a little super-paranoid; but just use an empty string if GUIDs
        // get bigger than we expect.
        _ASSERTE(!"IID buffer too small.");
        wszIidRequested[0] = L'\0';
    }
    ProfilingAPIUtility::LogProfError(IDS_E_PROF_NO_CALLBACK_IFACE, wszCLSID, wszIidRequested);
}

#ifdef _DEBUG

// ----------------------------------------------------------------------------
// ProfilingAPIUtility::ShouldInjectProfAPIFault
//
// Description:
//    Determines whether COMPlus_ProfAPIFault is set to a bitmask value
//    with the specified flag set
//
// Return Value:
//    Nonzero if the specified fault flag is set; 0 otherwise.
//

// static
inline BOOL ProfilingAPIUtility::ShouldInjectProfAPIFault(ProfAPIFaultFlags faultFlag)
{
    return ((CLRConfig::GetConfigValue(CLRConfig::INTERNAL_ProfAPIFault) & faultFlag) != 0);
}

#endif // _DEBUG


// ----------------------------------------------------------------------------
// ProfilingAPIUtility::LoadProfilerForAttach
//
// Description:
//    Simple, public wrapper around code:ProfilingAPIUtility::LoadProfiler to load a
//    profiler in response to an Attach request.
//
// Arguments:
//    * pClsid - Profiler's CLSID
//    * wszProfilerDLL - Profiler's DLL
//    * pvClientData - Client data received from trigger, to send to profiler DLL
//    * cbClientData - Size of client data
//
// Return Value:
//    HRESULT indicating success or failure
//

// static
inline HRESULT ProfilingAPIUtility::LoadProfilerForAttach(
    const CLSID * pClsid,
    LPCWSTR wszProfilerDLL,
    LPVOID pvClientData,
    UINT cbClientData,
    DWORD dwConcurrentGCWaitTimeoutInMs)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;

        // This causes events to be logged, which loads resource strings,
        // which takes locks.
        CAN_TAKE_LOCK;

        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    // Need string version of CLSID for event log messages
    WCHAR wszClsid[40];
    if (StringFromGUID2(*pClsid, wszClsid, _countof(wszClsid)) == 0)
    {
        _ASSERTE(!"StringFromGUID2 failed!");
        return E_UNEXPECTED;
    }

    // Inform user we're about to try attaching the profiler
    ProfilingAPIUtility::LogProfInfo(IDS_PROF_ATTACH_REQUEST_RECEIVED, wszClsid);

    return LoadProfiler(
        kAttachLoad,
        pClsid,
        wszClsid,
        wszProfilerDLL,
        pvClientData,
        cbClientData,
        dwConcurrentGCWaitTimeoutInMs);
}

inline /* static */ CRITSEC_COOKIE ProfilingAPIUtility::GetStatusCrst()
{
    LIMITED_METHOD_CONTRACT;
    return s_csStatus;
}

#endif //__PROFILING_HELPER_INL__
