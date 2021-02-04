// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// EEConfig.CPP
//

//
// Fetched configuration data from the registry (should we Jit, run GC checks ...)
//
//


#include "common.h"
#include "eeconfig.h"
#include "method.hpp"
#include "eventtrace.h"
#include "eehash.h"
#include "eemessagebox.h"
#include "corhost.h"
#include "regex_util.h"
#include "clr/fs/path.h"
#include "configuration.h"

using namespace clr;

#define DEFAULT_ZAP_SET W("")

#define DEFAULT_APP_DOMAIN_LEAKS 0


#ifdef STRESS_HEAP
// Global counter to disable GCStress. This is needed so we can inhibit
// GC stres collections without resetting the global GCStressLevel, which
// is relied on by the EH code and the JIT code (for handling patched
// managed code, and GC stress exception) after GC stress is dynamically
// turned off.
Volatile<DWORD> GCStressPolicy::InhibitHolder::s_nGcStressDisabled = 0;
#endif // STRESS_HEAP

/**************************************************************/
// Poor mans narrow
LPUTF8 NarrowWideChar(__inout_z LPCWSTR str)
{
    CONTRACT (LPUTF8)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(str, NULL_OK));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    } CONTRACT_END;

    if (str != 0) {
        LPCWSTR fromPtr = str;
        LPUTF8 toPtr = (LPUTF8) str;
        LPUTF8 result = toPtr;
        while(*fromPtr != 0)
            *toPtr++ = (char) *fromPtr++;
        *toPtr = 0;
        RETURN result;
    }
    RETURN NULL;
}

/**************************************************************/
static EEConfig g_EEConfig;

HRESULT EEConfig::Setup()
{
    STANDARD_VM_CONTRACT;

    ETWOnStartup (EEConfigSetup_V1,EEConfigSetupEnd_V1);

    _ASSERTE(g_pConfig == NULL && "EEConfig::Setup called multiple times!");

    EEConfig *pConfig = &g_EEConfig;

    HRESULT hr = pConfig->Init();

    if (FAILED(hr))
        return hr;

    g_pConfig = pConfig;

    return S_OK;
}

/**************************************************************/
HRESULT EEConfig::Init()
{
    STANDARD_VM_CONTRACT;

    fInited = false;

#ifdef VERIFY_HEAP
    iGCHeapVerify = 0;          // Heap Verification OFF by default
#endif

#if defined(STRESS_HEAP) || defined(_DEBUG)
    iGCStress     = 0;
#endif

    fGCBreakOnOOM = false;
    iGCconcurrent = 0;
    iGCHoardVM = 0;
    iGCLOHThreshold = 0;

    m_fFreepZapSet = false;

    dwSpinInitialDuration = 0x32;
    dwSpinBackoffFactor = 0x3;
    dwSpinLimitProcCap = 0xFFFFFFFF;
    dwSpinLimitProcFactor = 0x4E20;
    dwSpinLimitConstant = 0x0;
    dwSpinRetryCount = 0xA;
    dwMonitorSpinCount = 0;

    dwJitHostMaxSlabCache = 0;

    iJitOptimizeType = OPT_DEFAULT;
    fJitFramed = false;
    fJitMinOpts = false;
    fPInvokeRestoreEsp = (DWORD)-1;

    fNgenBindOptimizeNonGac = false;
    fStressLog = false;
    fProbeForStackOverflow = true;

    INDEBUG(fStressLog = true;)

#ifdef _DEBUG
    fExpandAllOnLoad = false;
    fDebuggable = false;
    pPrestubHalt = 0;
    pPrestubGC = 0;
    pszBreakOnClassLoad = 0;
    pszBreakOnClassBuild = 0;
    pszBreakOnMethodName = 0;
    pszDumpOnClassLoad = 0;
    pszBreakOnInteropStubSetup = 0;
    pszBreakOnComToClrNativeInfoInit = 0;
    pszBreakOnStructMarshalSetup = 0;
    fJitVerificationDisable= false;
    fVerifierOff           = false;

#ifdef ENABLE_STARTUP_DELAY
    iStartupDelayMS = 0;
#endif
    iPerfNumAllocsThreshold = 0;
    iPerfAllocsSizeThreshold = 0;
    pPerfTypesToLog = NULL;
    iFastGCStress = 0;
    iInjectFatalError = 0;
    fSaveThreadInfo = FALSE;
    dwSaveThreadInfoMask = (DWORD)-1;
#ifdef TEST_DATA_CONSISTENCY
    // indicates whether to run the self test to determine that we are detecting when a lock is held by the
    // LS in DAC builds. Initialized via the environment variable TestDataConsistency
    fTestDataConsistency = false;
#endif

    // In Thread::SuspendThread(), default the timeout to 2 seconds.  If the suspension
    // takes longer, assert (but keep trying).
    m_SuspendThreadDeadlockTimeoutMs = 2000;

    // For now, give our suspension attempts 40 seconds to succeed before trapping to
    // the debugger.   Note that we should probably lower this when the JIT is run in
    // preemtive mode, as we really should not be starving the GC for 10's of seconds
    m_SuspendDeadlockTimeout = 40000;
#endif // _DEBUG

#ifdef FEATURE_COMINTEROP
    bLogCCWRefCountChange = false;
    pszLogCCWRefCountChange = NULL;
#endif // FEATURE_COMINTEROP

#ifdef _DEBUG
    m_fAssertOnBadImageFormat = false;
    m_fAssertOnFailFast = true;

    fSuppressChecks = false;
    fConditionalContracts = false;
    fEnableFullDebug = false;
#endif

#ifdef FEATURE_DOUBLE_ALIGNMENT_HINT
    DoubleArrayToLargeObjectHeapThreshold = 1000;
#endif

    iRequireZaps = REQUIRE_ZAPS_NONE;

    pZapSet = DEFAULT_ZAP_SET;

#if defined(TARGET_X86) || defined(TARGET_AMD64)
    dwDisableStackwalkCache = 0;
#else // TARGET_X86
    dwDisableStackwalkCache = 1;
#endif // TARGET_X86

    szZapBBInstr     = NULL;
    szZapBBInstrDir  = NULL;

#ifdef _DEBUG
    // interop logging
    m_pTraceIUnknown = NULL;
    m_TraceWrapper = 0;
#endif

#ifdef _DEBUG
    dwNgenForceFailureMask  = 0;
    dwNgenForceFailureCount = 0;
    dwNgenForceFailureKind  = 0;
#endif

#ifdef _DEBUG
    fShouldInjectFault = 0;
    testThreadAbort = 0;
#endif

    m_fInteropValidatePinnedObjects = false;
    m_fInteropLogArguments = false;

#if defined(_DEBUG) && defined(STUBLINKER_GENERATES_UNWIND_INFO)
    fStubLinkerUnwindInfoVerificationOn = FALSE;
#endif

#if defined(_DEBUG) && defined(FEATURE_EH_FUNCLETS)
    fSuppressLockViolationsOnReentryFromOS = false;
#endif

#if defined(_DEBUG) && defined(TARGET_AMD64)
    // For determining if we should force generation of long jump dispatch stubs.
    m_cGenerateLongJumpDispatchStubRatio = (size_t)(-1);
    m_cDispatchStubsGenerated = 0;
#endif

#if defined(_DEBUG)
    bDiagnosticSuspend = false;
#endif

#if defined(FEATURE_TIERED_COMPILATION)
    fTieredCompilation = false;
    fTieredCompilation_QuickJit = false;
    fTieredCompilation_QuickJitForLoops = false;
    fTieredCompilation_CallCounting = false;
    fTieredCompilation_UseCallCountingStubs = false;
    tieredCompilation_CallCountThreshold = 1;
    tieredCompilation_BackgroundWorkerTimeoutMs = 0;
    tieredCompilation_CallCountingDelayMs = 0;
    tieredCompilation_DeleteCallCountingStubsAfter = 0;
#endif

#if defined(FEATURE_ON_STACK_REPLACEMENT)
    dwOSR_HitLimit = 10;
    dwOSR_CounterBump = 5000;
#endif

#ifndef CROSSGEN_COMPILE
    backpatchEntryPointSlots = false;
#endif

#if defined(FEATURE_GDBJIT) && defined(_DEBUG)
    pszGDBJitElfDump = NULL;
#endif // FEATURE_GDBJIT && _DEBUG

#if defined(FEATURE_GDBJIT_FRAME)
    fGDBJitEmitDebugFrame = false;
#endif

    return S_OK;
}

/**************************************************************/
HRESULT EEConfig::Cleanup()
{
    CONTRACTL {
        FORBID_FAULT;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    if (m_fFreepZapSet)
        delete[] pZapSet;
    delete[] szZapBBInstr;

    if (pRequireZapsList)
        delete pRequireZapsList;

    if (pRequireZapsExcludeList)
        delete pRequireZapsExcludeList;

    if (pReadyToRunExcludeList)
        delete pReadyToRunExcludeList;

#ifdef _DEBUG
    if (pForbidZapsList)
        delete pForbidZapsList;

    if (pForbidZapsExcludeList)
        delete pForbidZapsExcludeList;
#endif

#ifdef FEATURE_COMINTEROP
    if (pszLogCCWRefCountChange)
        delete [] pszLogCCWRefCountChange;
#endif // FEATURE_COMINTEROP

#ifdef _DEBUG
    if (pPrestubHalt)
    {
        DestroyMethList(pPrestubHalt);
        pPrestubHalt = NULL;
    }
    if (pPrestubGC)
    {
        DestroyMethList(pPrestubGC);
        pPrestubGC = NULL;
    }
    if (pSkipGCCoverageList)
    {
        delete pSkipGCCoverageList;
        pSkipGCCoverageList = NULL;
    }

    delete [] pszBreakOnClassLoad;
    delete [] pszBreakOnClassBuild;
    delete [] pszBreakOnInstantiation;
    delete [] pszBreakOnMethodName;
    delete [] pszDumpOnClassLoad;
    delete [] pszBreakOnInteropStubSetup;
    delete [] pszBreakOnComToClrNativeInfoInit;
    delete [] pszBreakOnStructMarshalSetup;
    delete [] pszGcCoverageOnMethod;
#endif
#ifdef _DEBUG
    if (pPerfTypesToLog)
    {
        DestroyTypeList(pPerfTypesToLog);
        pPerfTypesToLog = NULL;
    }
#endif

    return S_OK;
}


//
// NOTE: This function is deprecated; use the CLRConfig class instead.
// To use the CLRConfig class, add an entry in file:../inc/CLRConfigValues.h.
//
HRESULT EEConfig::GetConfigString_DontUse_(__in_z LPCWSTR name, __deref_out_z LPWSTR *outVal, BOOL fPrependCOMPLUS)
{
    CONTRACT(HRESULT) {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT (CONTRACT_RETURN E_OUTOFMEMORY);
        PRECONDITION(CheckPointer(name));
        POSTCONDITION(CheckPointer(outVal, NULL_OK));
    } CONTRACT_END;

    *outVal = REGUTIL::GetConfigString_DontUse_(name, fPrependCOMPLUS);

    RETURN S_OK;
}


//
// NOTE: This function is deprecated; use the CLRConfig class instead.
// To use the CLRConfig class, add an entry in file:../inc/CLRConfigValues.h.
//
DWORD EEConfig::GetConfigDWORD_DontUse_(__in_z LPCWSTR name, DWORD defValue, DWORD level, BOOL fPrependCOMPLUS)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(name));
    } CONTRACTL_END;

    // <TODO>@TODO: After everyone has moved off registry, key remove the following line in golden</TODO>
    return REGUTIL::GetConfigDWORD_DontUse_(name, defValue, (REGUTIL::CORConfigLevel)level, fPrependCOMPLUS);
}

//
// NOTE: This function is deprecated; use the CLRConfig class instead.
// To use the CLRConfig class, add an entry in file:../inc/CLRConfigValues.h.
//
// Note for PAL: right now PAL does not have a _wcstoui64 API, so I am temporarily reading in all numbers as
// a 32-bit number. When we have the _wcstoui64 API on MAC we will use that instead of wcstoul.
ULONGLONG EEConfig::GetConfigULONGLONG_DontUse_(__in_z LPCWSTR name, ULONGLONG defValue, DWORD level, BOOL fPrependCOMPLUS)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(name));
    } CONTRACTL_END;

    // <TODO>@TODO: After everyone has moved off registry, key remove the following line in golden</TODO>
    return REGUTIL::GetConfigULONGLONG_DontUse_(name, defValue, (REGUTIL::CORConfigLevel)level, fPrependCOMPLUS);
}

//
// NOTE: This function is deprecated; use the CLRConfig class instead.
// To use the CLRConfig class, add an entry in file:../inc/CLRConfigValues.h.
//
DWORD EEConfig::GetConfigDWORDInternal_DontUse_(__in_z LPCWSTR name, DWORD defValue, DWORD level, BOOL fPrependCOMPLUS)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(name));
    } CONTRACTL_END;

    // <TODO>@TODO: After everyone has moved off registry, key remove the following line in golden</TODO>
    return REGUTIL::GetConfigDWORD_DontUse_(name, defValue, (REGUTIL::CORConfigLevel)level, fPrependCOMPLUS);
}

/**************************************************************/

HRESULT EEConfig::sync()
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT (return E_OUTOFMEMORY);
    } CONTRACTL_END;

    ETWOnStartup (EEConfigSync_V1, EEConfigSyncEnd_V1);

    HRESULT hr = S_OK;

    // Note the global variable is not updated directly by the GetRegKey function
    // so we only update it once (to avoid reentrancy windows)

fTrackDynamicMethodDebugInfo = CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_TrackDynamicMethodDebugInfo);

#ifdef _DEBUG
    iFastGCStress       = GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_FastGCStress, iFastGCStress);

    IfFailRet(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_GcCoverage, (LPWSTR*)&pszGcCoverageOnMethod));
    pszGcCoverageOnMethod = NarrowWideChar((LPWSTR)pszGcCoverageOnMethod);
#endif

    bool gcConcurrentWasForced = false;
    gcConcurrentWasForced = Configuration::GetKnobBooleanValue(W("System.GC.Concurrent"), false);

    int gcConcurrentConfigVal = 0;
    if (!gcConcurrentWasForced)
    {
        // The CLRConfig value for UNSUPPORTED_gcConcurrent defaults to -1, and treats any
        // positive value as 'forcing' concurrent GC to be on. Because the standard logic
        // for mapping a DWORD CLRConfig to a boolean configuration treats -1 as true (just
        // like any other nonzero value), we will explicitly check the DWORD later if this
        // check returns false.
        gcConcurrentConfigVal = CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_gcConcurrent);
        gcConcurrentWasForced = (gcConcurrentConfigVal > 0);
    }

    if (gcConcurrentWasForced || (gcConcurrentConfigVal == -1 && g_IGCconcurrent))
        iGCconcurrent = TRUE;

    // Disable concurrent GC during ngen for the rare case a GC gets triggered, causing problems
    if (IsCompilationProcess())
        iGCconcurrent = FALSE;

#if defined(STRESS_HEAP) || defined(_DEBUG)
    iGCStress           =  CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_GCStress);
#endif

#ifdef STRESS_HEAP
    BOOL bGCStressAndHeapVerifyAllowed = true;

    if (iGCStress)
    {
        LPWSTR pszGCStressExe = NULL;

        IfFailRet(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_RestrictedGCStressExe, &pszGCStressExe));
        if (pszGCStressExe != NULL)
        {
            if (*pszGCStressExe != W('\0'))
            {
                bGCStressAndHeapVerifyAllowed = false;

                PathString wszFileName;
                if (WszGetModuleFileName(NULL, wszFileName) != 0)
                {
                    // just keep the name
                    LPCWSTR pwszName = wcsrchr(wszFileName, W('\\'));
                    pwszName = (pwszName == NULL) ? wszFileName.GetUnicode() : (pwszName + 1);

                    if (SString::_wcsicmp(pwszName,pszGCStressExe) == 0)
                    {
                        bGCStressAndHeapVerifyAllowed = true;
                    }
                }
            }
            delete [] pszGCStressExe;
        }

        if (bGCStressAndHeapVerifyAllowed)
        {
            if (gcConcurrentWasForced)
            {
#ifdef _DEBUG
                iFastGCStress = 0;
#endif
                iGCStress |= int(GCSTRESS_ALLOC) | int(GCSTRESS_TRANSITION);
            }
            else
            {
                // If GCStress was enabled, and
                // If GcConcurrent was NOT explicitly specified in the environment,
                // then let's turn off concurrent GC since it make objects move less
                iGCconcurrent = g_IGCconcurrent = 0;
            }
        }
        else
        {
            iGCStress = 0;
        }
    }

#ifdef VERIFY_HEAP
    if (bGCStressAndHeapVerifyAllowed)
    {
        iGCHeapVerify       =  GetConfigDWORD_DontUse_(CLRConfig::UNSUPPORTED_HeapVerify, iGCHeapVerify);
    }
#endif

#endif //STRESS_HEAP

    if (g_IGCHoardVM)
        iGCHoardVM = g_IGCHoardVM;
    else
        iGCHoardVM = CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_GCRetainVM);

    if (!iGCLOHThreshold)
    {
        iGCLOHThreshold = Configuration::GetKnobDWORDValue(W("System.GC.LOHThreshold"), CLRConfig::EXTERNAL_GCLOHThreshold);
        iGCLOHThreshold = max (iGCLOHThreshold, LARGE_OBJECT_SIZE);
    }

#ifdef FEATURE_CONSERVATIVE_GC
    iGCConservative =  (CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_gcConservative) != 0);
#endif // FEATURE_CONSERVATIVE_GC

#ifdef HOST_64BIT
    iGCAllowVeryLargeObjects = (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_gcAllowVeryLargeObjects) != 0);
#endif

    fGCBreakOnOOM   =  (GetConfigDWORD_DontUse_(CLRConfig::UNSUPPORTED_GCBreakOnOOM, fGCBreakOnOOM) != 0);

#ifdef TRACE_GC
    iGCtraceStart       =  GetConfigDWORD_DontUse_(CLRConfig::UNSUPPORTED_GCtraceStart, iGCtraceStart);
    iGCtraceEnd         =  GetConfigDWORD_DontUse_(CLRConfig::UNSUPPORTED_GCtraceEnd, iGCtraceEnd);
    iGCtraceFac         =  GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_GCtraceFacility, iGCtraceFac);
    iGCprnLvl           =  GetConfigDWORD_DontUse_(CLRConfig::UNSUPPORTED_GCprnLvl, iGCprnLvl);
#endif

#ifdef _DEBUG
    iInjectFatalError   = GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_InjectFatalError, iInjectFatalError);

    fSaveThreadInfo     =  (GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_SaveThreadInfo, fSaveThreadInfo) != 0);

    dwSaveThreadInfoMask     =  GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_SaveThreadInfoMask, dwSaveThreadInfoMask);

    {
        LPWSTR wszSkipGCCoverageList = NULL;
        IfFailRet(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_SkipGCCoverage, &wszSkipGCCoverageList));

        EX_TRY
        {
            if (wszSkipGCCoverageList)
                pSkipGCCoverageList = new AssemblyNamesList(wszSkipGCCoverageList);
        }
        EX_CATCH_HRESULT(hr);
        IfFailRet(hr);
    }
#endif
    fStressLog        =  GetConfigDWORD_DontUse_(CLRConfig::UNSUPPORTED_StressLog, fStressLog) != 0;
    fForceEnc         =  GetConfigDWORD_DontUse_(CLRConfig::UNSUPPORTED_ForceEnc, fForceEnc) != 0;

    iRequireZaps        = RequireZapsType(GetConfigDWORD_DontUse_(CLRConfig::EXTERNAL_ZapRequire, iRequireZaps));
    if (IsCompilationProcess() || iRequireZaps >= REQUIRE_ZAPS_COUNT)
        iRequireZaps = REQUIRE_ZAPS_NONE;

    if (iRequireZaps != REQUIRE_ZAPS_NONE)
    {
        {
            NewArrayHolder<WCHAR> wszZapRequireList;
            IfFailRet(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_ZapRequireList, &wszZapRequireList));
            if (wszZapRequireList)
                pRequireZapsList = new AssemblyNamesList(wszZapRequireList);
        }

        {
            NewArrayHolder<WCHAR> wszZapRequireExcludeList;
            IfFailRet(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_ZapRequireExcludeList, &wszZapRequireExcludeList));
            if (wszZapRequireExcludeList)
                pRequireZapsExcludeList = new AssemblyNamesList(wszZapRequireExcludeList);
        }
    }

    pReadyToRunExcludeList = NULL;
#if defined(FEATURE_READYTORUN)
    if (ReadyToRunInfo::IsReadyToRunEnabled())
    {
        NewArrayHolder<WCHAR> wszReadyToRunExcludeList;
        IfFailRet(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_ReadyToRunExcludeList, &wszReadyToRunExcludeList));
        if (wszReadyToRunExcludeList)
            pReadyToRunExcludeList = new AssemblyNamesList(wszReadyToRunExcludeList);
    }
#endif // defined(FEATURE_READYTORUN)

#ifdef _DEBUG
    iForbidZaps     = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_NgenBind_ZapForbid) != 0;
    if (iForbidZaps != 0)
    {
        {
            NewArrayHolder<WCHAR> wszZapForbidList;
            IfFailRet(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_NgenBind_ZapForbidList, &wszZapForbidList));
            if (wszZapForbidList)
                pForbidZapsList = new AssemblyNamesList(wszZapForbidList);
        }

        {
            NewArrayHolder<WCHAR> wszZapForbidExcludeList;
            IfFailRet(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_NgenBind_ZapForbidExcludeList, &wszZapForbidExcludeList));
            if (wszZapForbidExcludeList)
                pForbidZapsExcludeList = new AssemblyNamesList(wszZapForbidExcludeList);
        }
    }
#endif

#ifdef FEATURE_DOUBLE_ALIGNMENT_HINT
    DoubleArrayToLargeObjectHeapThreshold = GetConfigDWORD_DontUse_(CLRConfig::UNSUPPORTED_DoubleArrayToLargeObjectHeap, DoubleArrayToLargeObjectHeapThreshold);
#endif

    IfFailRet(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_ZapBBInstr, (LPWSTR*)&szZapBBInstr));
    if (szZapBBInstr)
    {
        szZapBBInstr = NarrowWideChar((LPWSTR)szZapBBInstr);

        // If szZapBBInstr only contains white space, then there's nothing to instrument (this
        // is the case with some test cases, and it's easier to fix all of them here).
        LPWSTR pStr = (LPWSTR) szZapBBInstr;
        while (*pStr == W(' ')) pStr++;
        if (*pStr == 0)
            szZapBBInstr = NULL;
    }

    if (szZapBBInstr != NULL)
    {
        IfFailRet(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_ZapBBInstrDir, &szZapBBInstrDir));
        g_IBCLogger.EnableAllInstr();
    }
    else
        g_IBCLogger.DisableAllInstr();


    dwDisableStackwalkCache = GetConfigDWORD_DontUse_(CLRConfig::EXTERNAL_DisableStackwalkCache, dwDisableStackwalkCache);


#ifdef _DEBUG
    IfFailRet (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_BreakOnClassLoad, (LPWSTR*) &pszBreakOnClassLoad));
    pszBreakOnClassLoad = NarrowWideChar((LPWSTR)pszBreakOnClassLoad);
#endif

    dwSpinInitialDuration = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_SpinInitialDuration);
    if (dwSpinInitialDuration < 1)
    {
        dwSpinInitialDuration = 1;
    }
    dwSpinBackoffFactor = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_SpinBackoffFactor);
    if (dwSpinBackoffFactor < 2)
    {
        dwSpinBackoffFactor = 2;
    }
    dwSpinLimitProcCap = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_SpinLimitProcCap);
    dwSpinLimitProcFactor = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_SpinLimitProcFactor);
    dwSpinLimitConstant = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_SpinLimitConstant);
    dwSpinRetryCount = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_SpinRetryCount);
    dwMonitorSpinCount = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_Monitor_SpinCount);

    dwJitHostMaxSlabCache = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_JitHostMaxSlabCache);

    fJitFramed = (GetConfigDWORD_DontUse_(CLRConfig::UNSUPPORTED_JitFramed, fJitFramed) != 0);
    fJitMinOpts = (GetConfigDWORD_DontUse_(CLRConfig::UNSUPPORTED_JITMinOpts, fJitMinOpts) == 1);
    iJitOptimizeType      =  GetConfigDWORD_DontUse_(CLRConfig::EXTERNAL_JitOptimizeType, iJitOptimizeType);
    if (iJitOptimizeType > OPT_RANDOM)     iJitOptimizeType = OPT_DEFAULT;

#ifdef TARGET_X86
    fPInvokeRestoreEsp = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_Jit_NetFx40PInvokeStackResilience);
#endif


#ifdef _DEBUG
    fDebuggable         = (GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_JitDebuggable,      fDebuggable)         != 0);

    LPWSTR wszPreStubStuff = NULL;

    IfFailRet(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_PrestubHalt, &wszPreStubStuff));
    IfFailRet(ParseMethList(wszPreStubStuff, &pPrestubHalt));

    LPWSTR wszInvokeStuff = NULL;
    IfFailRet(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_InvokeHalt, &wszInvokeStuff));
    IfFailRet(ParseMethList(wszInvokeStuff, &pInvokeHalt));

    IfFailRet(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_PrestubGC, &wszPreStubStuff));
    IfFailRet(ParseMethList(wszPreStubStuff, &pPrestubGC));

    IfFailRet(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_BreakOnClassBuild, (LPWSTR*)&pszBreakOnClassBuild));
    pszBreakOnClassBuild = NarrowWideChar((LPWSTR)pszBreakOnClassBuild);

    IfFailRet(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_BreakOnInstantiation, (LPWSTR*)&pszBreakOnInstantiation));
    pszBreakOnInstantiation = NarrowWideChar((LPWSTR)pszBreakOnInstantiation);

    IfFailRet(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_BreakOnMethodName, (LPWSTR*)&pszBreakOnMethodName));
    pszBreakOnMethodName = NarrowWideChar((LPWSTR)pszBreakOnMethodName);

    IfFailRet(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DumpOnClassLoad, (LPWSTR*)&pszDumpOnClassLoad));
    pszDumpOnClassLoad = NarrowWideChar((LPWSTR)pszDumpOnClassLoad);

    IfFailRet(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_BreakOnInteropStubSetup, (LPWSTR*)&pszBreakOnInteropStubSetup));
    pszBreakOnInteropStubSetup = NarrowWideChar((LPWSTR)pszBreakOnInteropStubSetup);

    IfFailRet(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_BreakOnComToClrNativeInfoInit, (LPWSTR*)&pszBreakOnComToClrNativeInfoInit));
    pszBreakOnComToClrNativeInfoInit = NarrowWideChar((LPWSTR)pszBreakOnComToClrNativeInfoInit);

    IfFailRet(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_BreakOnStructMarshalSetup, (LPWSTR*)&pszBreakOnStructMarshalSetup));
    pszBreakOnStructMarshalSetup = NarrowWideChar((LPWSTR)pszBreakOnStructMarshalSetup);

    m_fAssertOnBadImageFormat = (GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_AssertOnBadImageFormat, m_fAssertOnBadImageFormat) != 0);
    m_fAssertOnFailFast = (GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_AssertOnFailFast, m_fAssertOnFailFast) != 0);

    fSuppressChecks = (GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_SuppressChecks, fSuppressChecks) != 0);
    CHECK::SetAssertEnforcement(!fSuppressChecks);

    fConditionalContracts = (GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_ConditionalContracts, fConditionalContracts) != 0);

#ifdef ENABLE_CONTRACTS_IMPL
    Contract::SetUnconditionalContractEnforcement(!fConditionalContracts);
#endif

    fEnableFullDebug = (GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_EnableFullDebug, fEnableFullDebug) != 0);

    fVerifierOff    = (GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_VerifierOff, fVerifierOff) != 0);

    fJitVerificationDisable = (GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_JitVerificationDisable, fJitVerificationDisable)         != 0);

    iExposeExceptionsInCOM = GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_ExposeExceptionsInCOM, iExposeExceptionsInCOM);
#endif

#ifdef FEATURE_COMINTEROP
    IfFailRet(CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_LogCCWRefCountChange, (LPWSTR*)&pszLogCCWRefCountChange));
    pszLogCCWRefCountChange = NarrowWideChar((LPWSTR)pszLogCCWRefCountChange);
    if (pszLogCCWRefCountChange != NULL)
        bLogCCWRefCountChange = true;

    fEnableRCWCleanupOnSTAShutdown = (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_EnableRCWCleanupOnSTAShutdown) != 0);
#endif // FEATURE_COMINTEROP

#ifdef _DEBUG
    fExpandAllOnLoad = (GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_ExpandAllOnLoad, fExpandAllOnLoad) != 0);
#endif //_DEBUG

#ifdef ENABLE_STARTUP_DELAY
    {
        //I want this string in decimal
        WCHAR * end;
        WCHAR * str;
        IfFailRet(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_StartupDelayMS, &str));
        if( str )
        {
            errno = 0;
            iStartupDelayMS = wcstoul(str, &end, 10);
            if (errno == ERANGE || end == str)
                iStartupDelayMS = 0;
        }
    }
#endif

#ifdef _DEBUG

#ifdef TEST_DATA_CONSISTENCY
    fTestDataConsistency = (CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_TestDataConsistency) !=0);
#endif

    m_SuspendThreadDeadlockTimeoutMs = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_SuspendThreadDeadlockTimeoutMs);
    m_SuspendDeadlockTimeout = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_SuspendDeadlockTimeout);
#endif // _DEBUG
    fInited = true;

#ifdef _DEBUG
    m_pTraceIUnknown = (IUnknown*)(DWORD_PTR)(GetConfigDWORD_DontUse_(CLRConfig::EXTERNAL_TraceIUnknown, (DWORD)(DWORD_PTR)(m_pTraceIUnknown))); // <TODO> WIN64 - conversion from DWORD to IUnknown* of greater size</TODO>
    m_TraceWrapper = GetConfigDWORD_DontUse_(CLRConfig::EXTERNAL_TraceWrap, m_TraceWrapper);

    // can't have both
    if (m_pTraceIUnknown != 0)
    {
        m_TraceWrapper = 0;
    }
    else
    if (m_TraceWrapper != 0)
    {
        m_pTraceIUnknown = (IUnknown*)-1;
    }
#endif

#ifdef _DEBUG

    LPWSTR wszPerfTypes = NULL;
    IfFailRet(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_PerfTypesToLog, &wszPerfTypes));
    IfFailRet(ParseTypeList(wszPerfTypes, &pPerfTypesToLog));

    iPerfNumAllocsThreshold = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_PerfNumAllocsThreshold);
    iPerfAllocsSizeThreshold = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_PerfAllocsSizeThreshold);

    fShouldInjectFault = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_InjectFault);

    testThreadAbort = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_HostTestThreadAbort);

#endif //_DEBUG

    m_fInteropValidatePinnedObjects = (CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_InteropValidatePinnedObjects) != 0);
    m_fInteropLogArguments = (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_InteropLogArguments) != 0);

#ifdef FEATURE_PREJIT
#ifdef _DEBUG
    dwNgenForceFailureMask  = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_NgenForceFailureMask);
    dwNgenForceFailureCount = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_NgenForceFailureCount);
    dwNgenForceFailureKind  = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_NgenForceFailureKind);
#endif
#endif // FEATURE_PREJIT

#if defined(_DEBUG) && defined(FEATURE_EH_FUNCLETS)
    fSuppressLockViolationsOnReentryFromOS = (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_SuppressLockViolationsOnReentryFromOS) != 0);
#endif

#if defined(_DEBUG) && defined(STUBLINKER_GENERATES_UNWIND_INFO)
    fStubLinkerUnwindInfoVerificationOn = (GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_StubLinkerUnwindInfoVerificationOn, fStubLinkerUnwindInfoVerificationOn) != 0);
#endif

    if (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_UseMethodDataCache) != 0) {
        MethodTable::AllowMethodDataCaching();
    }

    if (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_UseParentMethodData) != 0) {
        MethodTable::AllowParentMethodDataCopy();
    }


#if defined(_DEBUG) && defined(TARGET_AMD64)
    m_cGenerateLongJumpDispatchStubRatio = GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_GenerateLongJumpDispatchStubRatio,
                                                          static_cast<DWORD>(m_cGenerateLongJumpDispatchStubRatio));
#endif

#if defined(_DEBUG)
    bDiagnosticSuspend = (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DiagnosticSuspend) != 0);
#endif

    dwSleepOnExit = CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_SleepOnExit);

#if defined(FEATURE_TIERED_COMPILATION)
    fTieredCompilation = Configuration::GetKnobBooleanValue(W("System.Runtime.TieredCompilation"), CLRConfig::EXTERNAL_TieredCompilation);
    if (fTieredCompilation)
    {
        fTieredCompilation_QuickJit =
            Configuration::GetKnobBooleanValue(
                W("System.Runtime.TieredCompilation.QuickJit"),
                CLRConfig::EXTERNAL_TC_QuickJit);
        if (fTieredCompilation_QuickJit)
        {
            fTieredCompilation_QuickJitForLoops =
                Configuration::GetKnobBooleanValue(
                    W("System.Runtime.TieredCompilation.QuickJitForLoops"),
                    CLRConfig::UNSUPPORTED_TC_QuickJitForLoops);
        }

        tieredCompilation_BackgroundWorkerTimeoutMs =
            CLRConfig::GetConfigValue(CLRConfig::INTERNAL_TC_BackgroundWorkerTimeoutMs);

        fTieredCompilation_CallCounting = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_TC_CallCounting) != 0;

        DWORD tieredCompilation_ConfiguredCallCountThreshold =
            CLRConfig::GetConfigValue(CLRConfig::INTERNAL_TC_CallCountThreshold);
        if (tieredCompilation_ConfiguredCallCountThreshold == 0)
        {
            tieredCompilation_CallCountThreshold = 1;
        }
        else if (tieredCompilation_ConfiguredCallCountThreshold > UINT16_MAX)
        {
            tieredCompilation_CallCountThreshold = UINT16_MAX;
        }
        else
        {
            tieredCompilation_CallCountThreshold = (UINT16)tieredCompilation_ConfiguredCallCountThreshold;
        }

        tieredCompilation_CallCountingDelayMs = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_TC_CallCountingDelayMs);

        bool hasSingleProcessor;
#ifndef TARGET_UNIX
        if (CPUGroupInfo::CanEnableThreadUseAllCpuGroups())
        {
            hasSingleProcessor = CPUGroupInfo::GetNumActiveProcessors() == 1;
        }
        else
#endif
        {
            hasSingleProcessor = GetCurrentProcessCpuCount() == 1;
        }
        if (hasSingleProcessor)
        {
            DWORD delayMultiplier = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_TC_DelaySingleProcMultiplier);
            if (delayMultiplier > 1)
            {
                DWORD newDelay = tieredCompilation_CallCountingDelayMs * delayMultiplier;
                if (newDelay / delayMultiplier == tieredCompilation_CallCountingDelayMs)
                {
                    tieredCompilation_CallCountingDelayMs = newDelay;
                }
            }
        }

        if (fTieredCompilation_CallCounting)
        {
            fTieredCompilation_UseCallCountingStubs =
                CLRConfig::GetConfigValue(CLRConfig::INTERNAL_TC_UseCallCountingStubs) != 0;
            if (fTieredCompilation_UseCallCountingStubs)
            {
                tieredCompilation_DeleteCallCountingStubsAfter =
                    CLRConfig::GetConfigValue(CLRConfig::INTERNAL_TC_DeleteCallCountingStubsAfter);
            }
        }

        if (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_TC_AggressiveTiering) != 0)
        {
            // TC_AggressiveTiering may be used in some benchmarks to have methods be tiered up more quickly, for example when
            // the measurement is sensitive to GC allocations or activity. Methods tiered up more quickly may have different
            // performance characteristics, as timing of the rejit may play a role. If there are multiple tiers before the final
            // tier, the expectation is that the method progress through all tiers as quickly as possible, ideally running the
            // code for each tier at least once before progressing to the next tier.
            tieredCompilation_CallCountThreshold = 1;
            tieredCompilation_CallCountingDelayMs = 0;
        }

        if (ETW::CompilationLog::TieredCompilation::Runtime::IsEnabled())
        {
            ETW::CompilationLog::TieredCompilation::Runtime::SendSettings();
        }
    }
#endif

#if defined(FEATURE_ON_STACK_REPLACEMENT)
    dwOSR_HitLimit = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_OSR_HitLimit);
    dwOSR_CounterBump = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_OSR_CounterBump);
#endif

#if defined(FEATURE_ON_STACK_REPLACEMENT) && defined(_DEBUG)
    dwOSR_LowId = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_OSR_LowId);
    dwOSR_HighId = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_OSR_HighId);
#endif

#ifndef CROSSGEN_COMPILE
    backpatchEntryPointSlots = CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_BackpatchEntryPointSlots) != 0;
#endif

#if defined(FEATURE_GDBJIT) && defined(_DEBUG)
    {
        LPWSTR pszGDBJitElfDumpW = NULL;
        CLRConfig::GetConfigValue(CLRConfig::INTERNAL_GDBJitElfDump, &pszGDBJitElfDumpW);
        pszGDBJitElfDump = NarrowWideChar(pszGDBJitElfDumpW);
    }
#endif // FEATURE_GDBJIT && _DEBUG

#if defined(FEATURE_GDBJIT_FRAME)
    fGDBJitEmitDebugFrame = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_GDBJitEmitDebugFrame) != 0;
#endif
    return hr;
}

bool EEConfig::RequireZap(LPCUTF8 assemblyName) const
{
    LIMITED_METHOD_CONTRACT;
    if (iRequireZaps == REQUIRE_ZAPS_NONE)
        return false;

    if (pRequireZapsExcludeList != NULL && pRequireZapsExcludeList->IsInList(assemblyName))
        return false;

    if (pRequireZapsList == NULL || pRequireZapsList->IsInList(assemblyName))
        return true;

    return false;
}

#ifdef _DEBUG
bool EEConfig::ForbidZap(LPCUTF8 assemblyName) const
{
    LIMITED_METHOD_CONTRACT;
    if (iForbidZaps == 0)
        return false;

    if (pForbidZapsExcludeList != NULL && pForbidZapsExcludeList->IsInList(assemblyName))
        return false;

    if (pForbidZapsList == NULL || pForbidZapsList->IsInList(assemblyName))
        return true;

    return false;
}
#endif

bool EEConfig::ExcludeReadyToRun(LPCUTF8 assemblyName) const
{
    LIMITED_METHOD_CONTRACT;

    if (pReadyToRunExcludeList != NULL && pReadyToRunExcludeList->IsInList(assemblyName))
        return true;

    return false;
}

/**************************************************************/
#ifdef _DEBUG
/**************************************************************/

// Ownership of the string buffer passes to ParseMethList

/* static */
HRESULT EEConfig::ParseMethList(__in_z LPWSTR str, MethodNamesList** out) {
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(return E_OUTOFMEMORY);
        PRECONDITION(CheckPointer(str, NULL_OK));
        PRECONDITION(CheckPointer(out));
    } CONTRACTL_END;

    HRESULT hr = S_OK;

    *out = NULL;

        // we are now done with the string passed in
    if (str == NULL)
    {
        return S_OK;
    }

    EX_TRY
    {
        *out = new MethodNamesList(str);
    } EX_CATCH_HRESULT(hr);

    delete [] str;

    return hr;
}

/**************************************************************/
/* static */
void EEConfig::DestroyMethList(MethodNamesList* list) {
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(list));
    } CONTRACTL_END;

    if (list == 0)
        return;
    delete list;
}

/**************************************************************/
/* static */
bool EEConfig::IsInMethList(MethodNamesList* list, MethodDesc* pMD)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(list, NULL_OK));
        PRECONDITION(CheckPointer(pMD));
    } CONTRACTL_END;

    if (list == 0)
        return(false);
    else
    {
        DefineFullyQualifiedNameForClass();

        LPCUTF8 name = pMD->GetName();
        if (name == NULL)
        {
            return false;
        }
        LPCUTF8 className = GetFullyQualifiedNameForClass(pMD->GetMethodTable());
        if (className == NULL)
        {
            return false;
        }
        PCCOR_SIGNATURE sig = pMD->GetSig();

        return list->IsInList(name, className, sig);
    }
}

// Ownership of the string buffer passes to ParseTypeList
/* static */
HRESULT EEConfig::ParseTypeList(__in_z LPWSTR str, TypeNamesList** out)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(out));
        PRECONDITION(CheckPointer(str, NULL_OK));
        INJECT_FAULT(return E_OUTOFMEMORY);
    } CONTRACTL_END;

    HRESULT hr = S_OK;

    *out = NULL;

    if (str == NULL)
        return S_OK;

    NewHolder<TypeNamesList> newTypeNameList(new (nothrow) TypeNamesList());
    if (newTypeNameList != NULL)
        IfFailRet(newTypeNameList->Init(str));

    delete [] str;

    newTypeNameList.SuppressRelease();
    *out = newTypeNameList;

    return (*out != NULL)?S_OK:E_OUTOFMEMORY;
}

void EEConfig::DestroyTypeList(TypeNamesList* list) {

    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(list));
    } CONTRACTL_END;

    if (list == 0)
        return;
    delete list;
}

TypeNamesList::TypeNamesList()
{
    LIMITED_METHOD_CONTRACT;
}

bool EEConfig::RegexOrExactMatch(LPCUTF8 regex, LPCUTF8 input)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (regex == NULL || input == NULL)
        return false;

    if (*regex == '/')
    {
        // Debug only, so we can live with it.
        CONTRACT_VIOLATION(ThrowsViolation);

        regex::STRRegEx::GroupingContainer groups;
        if (regex::STRRegEx::Match("^/(.*)/(i?)$", regex, groups))
        {
            regex::STRRegEx::MatchFlags flags = regex::STRRegEx::DefaultMatchFlags;
            if (groups[2].Length() != 0)
                flags = (regex::STRRegEx::MatchFlags)(flags | regex::STRRegEx::MF_CASE_INSENSITIVE);

            return regex::STRRegEx::Matches(groups[1].Begin(), groups[1].End(),
                                            input, input + strlen(input), flags);
        }
    }
    return strcmp(regex, input) == 0;
}

HRESULT TypeNamesList::Init(__in_z LPCWSTR str)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(str));
        INJECT_FAULT(return E_OUTOFMEMORY);
    } CONTRACTL_END;

    pNames = NULL;

    LPCWSTR currentType = str;
    int length = 0;
    bool typeFound = false;

    for (; *str != '\0'; str++)
    {
        switch(*str)
        {
        case ' ':
            {
                if (!typeFound)
                    break;

                NewHolder<TypeName> tn(new (nothrow) TypeName());
                if (tn == NULL)
                    return E_OUTOFMEMORY;

                tn->typeName = new (nothrow) char[length + 1];
                if (tn->typeName == NULL)
                    return E_OUTOFMEMORY;

                tn.SuppressRelease();
                MAKE_UTF8PTR_FROMWIDE_NOTHROW(temp, currentType);
                if (temp == NULL)
                    return E_OUTOFMEMORY;

                memcpy(tn->typeName, temp, length * sizeof(char));
                tn->typeName[length] = '\0';

                tn->next = pNames;
                pNames = tn;

                typeFound = false;
                length = 0;

                break;
            }

        default:
            if (!typeFound)
                currentType = str;

            typeFound = true;
            length++;
            break;
        }
    }

    if (typeFound)
    {
        NewHolder<TypeName> tn(new (nothrow) TypeName());
        if (tn == NULL)
            return E_OUTOFMEMORY;

        tn->typeName = new (nothrow) char[length + 1];

        if (tn->typeName == NULL)
            return E_OUTOFMEMORY;

        tn.SuppressRelease();
        MAKE_UTF8PTR_FROMWIDE_NOTHROW(temp, currentType);
        if (temp == NULL)
            return E_OUTOFMEMORY;

        memcpy(tn->typeName, temp, length * sizeof(char));
        tn->typeName[length] = '\0';

        tn->next = pNames;
        pNames = tn;
    }
    return S_OK;
}

TypeNamesList::~TypeNamesList()
{
    CONTRACTL {
        NOTHROW;
        FORBID_FAULT;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;


    while (pNames)
    {
        delete [] pNames->typeName;

        TypeName *tmp = pNames;
        pNames = pNames->next;

        delete tmp;
    }
}

bool TypeNamesList::IsInList(LPCUTF8 typeName)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CANNOT_TAKE_LOCK;
        PRECONDITION(CheckPointer(typeName));
    } CONTRACTL_END;

    TypeName *tnTemp = pNames;
    while (tnTemp)
    {
        if (strstr(typeName, tnTemp->typeName) != typeName)
            tnTemp = tnTemp->next;
        else
            return true;
    }

    return false;
}
#endif // _DEBUG

#ifdef FEATURE_COMINTEROP
void EEConfig::SetLogCCWRefCountChangeEnabled(bool newVal)
{
    LIMITED_METHOD_CONTRACT;

    // logically we want pszLogCCWRefCountChange != NULL to force bLogCCWRefCountChange to be true
    bLogCCWRefCountChange = (newVal || pszLogCCWRefCountChange != NULL);
}

bool EEConfig::ShouldLogCCWRefCountChange(LPCUTF8 pszClassName, LPCUTF8 pszNamespace) const
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END

    if (pszLogCCWRefCountChange == NULL)
        return false;

    // check simple class name
    if (strcmp(pszLogCCWRefCountChange, "*") == 0 ||
        strcmp(pszLogCCWRefCountChange, pszClassName) == 0)
        return true;

    // check namespace DOT class name
    LPCUTF8 dot = strrchr(pszLogCCWRefCountChange, '.');
    if (dot != NULL)
    {
        if (strncmp(pszLogCCWRefCountChange, pszNamespace, dot - pszLogCCWRefCountChange) == 0 &&
            strcmp(dot + 1, pszClassName) == 0)
            return true;
    }
    return false;
}
#endif // FEATURE_COMINTEROP
