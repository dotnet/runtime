// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: REJIT.INL
//

//
// Inline definitions of various items declared in REJIT.H
// ===========================================================================
#ifndef _REJIT_INL_
#define _REJIT_INL_

#ifdef FEATURE_REJIT

// static
inline void ReJitManager::InitStatic()
{
    STANDARD_VM_CONTRACT;

    s_csGlobalRequest.Init(CrstReJITGlobalRequest);
}

// static
inline BOOL ReJitManager::IsReJITEnabled()
{
    LIMITED_METHOD_CONTRACT;

    static bool profilerStartupRejit = CORProfilerEnableRejit() != FALSE;
    static ConfigDWORD rejitOnAttachEnabled;

    return  profilerStartupRejit || (rejitOnAttachEnabled.val(CLRConfig::EXTERNAL_ProfAPI_RejitOnAttach) != 0);
}

inline BOOL ReJitManager::IsReJITInlineTrackingEnabled()
{
    LIMITED_METHOD_CONTRACT;

    static ConfigDWORD rejitInliningEnabled;
    return rejitInliningEnabled.val(CLRConfig::EXTERNAL_ProfAPI_RejitOnAttach) != 0;
}

#ifndef DACCESS_COMPILE
//static
inline void ReJitManager::ReportReJITError(CodeVersionManager::CodePublishError* pErrorRecord)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        CAN_TAKE_LOCK;
        MODE_ANY;
    }
    CONTRACTL_END;
    ReportReJITError(pErrorRecord->pModule, pErrorRecord->methodDef, pErrorRecord->pMethodDesc, pErrorRecord->hrStatus);
}

// static
inline void ReJitManager::ReportReJITError(Module* pModule, mdMethodDef methodDef, MethodDesc* pMD, HRESULT hrStatus)
{
#ifdef PROFILING_SUPPORTED
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        CAN_TAKE_LOCK;
        MODE_ANY;
    }
    CONTRACTL_END;

    {
        BEGIN_PROFILER_CALLBACK(CORProfilerEnableRejit());
        _ASSERTE(CORProfilerEnableRejit());
        {
            GCX_PREEMP();
            (&g_profControlBlock)->mainProfilerInfo.pProfInterface->ReJITError(
                reinterpret_cast< ModuleID > (pModule),
                methodDef,
                reinterpret_cast< FunctionID > (pMD),
                hrStatus);
        }
        END_PROFILER_CALLBACK();
    }
#endif // PROFILING_SUPPORTED
}
#endif // DACCESS_COMPILE

#else // FEATURE_REJIT

// On architectures that don't support rejit, just keep around some do-nothing
// stubs so the rest of the VM doesn't have to be littered with #ifdef FEATURE_REJIT

// static
inline BOOL ReJitManager::IsReJITEnabled()
{
    return FALSE;
}

// static
inline void ReJitManager::InitStatic()
{
}

inline BOOL ReJitManager::IsReJITInlineTrackingEnabled()
{
    return FALSE;
}

#endif // FEATURE_REJIT


#endif // _REJIT_INL_
