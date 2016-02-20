// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// EEConfig.CPP
//

//
// Fetched configuration data from the registry (should we Jit, run GC checks ...)
//
//


#include "common.h"
#ifdef FEATURE_COMINTEROP
#include <appxutil.h>
#endif
#include "eeconfig.h"
#include "method.hpp"
#ifndef FEATURE_CORECLR
#include <xmlparser.h>
#include <mscorcfg.h>
#include "eeconfigfactory.h"
#endif
#ifdef FEATURE_FUSION
#include "fusionsetup.h"
#endif
#include "eventtrace.h"
#include "eehash.h"
#include "eemessagebox.h"
#include "corhost.h"
#include "regex_util.h"
#include "clr/fs/path.h"
#ifdef FEATURE_WIN_DB_APPCOMPAT
#include "QuirksApi.h"
#endif

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


ConfigSource::ConfigSource()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
    } CONTRACTL_END;
        
    m_pNext = this;
    m_pPrev = this;
}// ConfigSource::ConfigSource

ConfigSource::~ConfigSource()
{
    CONTRACTL {
        NOTHROW;
        FORBID_FAULT;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    for(ConfigStringHashtable::Iterator iter = m_Table.Begin(), end = m_Table.End(); iter != end; iter++)
    {
        ConfigStringKeyValuePair * pair = *(iter);
        delete[] pair->key;
        delete[] pair->value;
        delete pair;
    }
}// ConfigSource::~ConfigSource

ConfigStringHashtable * ConfigSource::Table()
{   
    LIMITED_METHOD_CONTRACT;
    return &(m_Table);
}// ConfigSource::Table

void ConfigSource::Add(ConfigSource* prev)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(prev));
        PRECONDITION(CheckPointer(prev->m_pNext));
    } CONTRACTL_END;
    
    m_pPrev = prev;
    m_pNext = prev->m_pNext;

    m_pNext->m_pPrev = this;
    prev->m_pNext = this;
}// ConfigSource::Add



/**************************************************************/
// Poor mans narrow
LPUTF8 NarrowWideChar(__inout_z LPWSTR str)
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
        LPWSTR fromPtr = str;
        LPUTF8 toPtr = (LPUTF8) str;
        LPUTF8 result = toPtr;
        while(*fromPtr != 0)
            *toPtr++ = (char) *fromPtr++;
        *toPtr = 0;
        RETURN result;
    }
    RETURN NULL;
}

extern void UpdateGCSettingFromHost ();

HRESULT EEConfig::Setup()
{
    STANDARD_VM_CONTRACT;

    ETWOnStartup (EEConfigSetup_V1,EEConfigSetupEnd_V1);
        
    // This 'new' uses EEConfig's overloaded new, which uses a static memory buffer and will
    // not fail
    EEConfig *pConfig = new EEConfig();

    HRESULT hr = pConfig->Init();

    if (FAILED(hr))
        return hr;

    EEConfig *pConfigOld = NULL;
    pConfigOld = InterlockedCompareExchangeT(&g_pConfig, pConfig, NULL);

    _ASSERTE(pConfigOld == NULL && "EEConfig::Setup called multiple times!");
    
    UpdateGCSettingFromHost();

    return S_OK;
}

/**************************************************************/
// For in-place constructor
BYTE g_EEConfigMemory[sizeof(EEConfig)];

void *EEConfig::operator new(size_t size)
{
    CONTRACT(void*) {
        FORBID_FAULT;
        GC_NOTRIGGER;
        NOTHROW;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
    } CONTRACT_END;

    RETURN g_EEConfigMemory;
}

#ifdef FEATURE_WIN_DB_APPCOMPAT
void InitWinAppCompatDBApis()
{
    STANDARD_VM_CONTRACT;

    HMODULE hMod = WszLoadLibraryEx(QUIRKSAPI_DLL, NULL, LOAD_LIBRARY_SEARCH_SYSTEM32);

    PFN_CptQuirkIsEnabled3 pfnIsQuirkEnabled = NULL;
    PFN_CptQuirkGetData2 pfnQuirkGetData = NULL;

    if(hMod != NULL) 
    {
        pfnIsQuirkEnabled = (PFN_CptQuirkIsEnabled3)GetProcAddress(hMod, "QuirkIsEnabled3");
        pfnQuirkGetData   = (PFN_CptQuirkGetData2)GetProcAddress(hMod, "QuirkGetData2");
    }

    if(pfnIsQuirkEnabled != NULL && pfnQuirkGetData != NULL)
    {
        CLRConfig::RegisterWinDbQuirkApis(pfnIsQuirkEnabled,pfnQuirkGetData);
    }
}
#endif // FEATURE_WIN_DB_APPCOMPAT

/**************************************************************/
HRESULT EEConfig::Init()
{
    STANDARD_VM_CONTRACT;

    fInited = false;

#ifdef VERIFY_HEAP
    iGCHeapVerify = 0;          // Heap Verification OFF by default
#endif

#ifdef _DEBUG // TRACE_GC
    iGCtraceStart = INT_MAX; // Set to huge value so GCtrace is off by default
    iGCtraceEnd   = INT_MAX;
    iGCtraceFac   = 0;
    iGCprnLvl     = DEFAULT_GC_PRN_LVL;
    
#endif

#if defined(STRESS_HEAP) || defined(_DEBUG)
    iGCStress     = 0;
#endif

#ifdef STRESS_HEAP
    iGCStressMix  = 0;
    iGCStressStep = 1;
#endif

    fGCBreakOnOOM = false;
    iGCgen0size = 0;
    iGCSegmentSize = 0;
    iGCconcurrent = 0;
#ifdef _DEBUG
    iGCLatencyMode = -1;
#endif //_DEBUG
    iGCForceCompact = 0;
    iGCHoardVM = 0;
    iGCLOHCompactionMode = 0;

#ifdef GCTRIMCOMMIT
    iGCTrimCommit = 0;
#endif

    m_fFreepZapSet = false;

    dwSpinInitialDuration = 0x32;
    dwSpinBackoffFactor = 0x3;
    dwSpinLimitProcCap = 0xFFFFFFFF;
    dwSpinLimitProcFactor = 0x4E20;
    dwSpinLimitConstant = 0x0;
    dwSpinRetryCount = 0xA;

    iJitOptimizeType = OPT_DEFAULT;
    fJitFramed = false;
    fJitAlignLoops = false;
    fAddRejitNops = false;
    fJitMinOpts = false;
    fPInvokeRestoreEsp = (DWORD)-1;

    fLegacyNullReferenceExceptionPolicy = false;
    fLegacyUnhandledExceptionPolicy = false;
    fLegacyApartmentInitPolicy = false;
    fLegacyComHierarchyVisibility = false;
    fLegacyComVTableLayout = false;
    fLegacyVirtualMethodCallVerification = false;
    fNewComVTableLayout = false;
    iImpersonationPolicy = IMP_DEFAULT;

#ifdef FEATURE_CORRUPTING_EXCEPTIONS
    // By default, there is not pre-V4 CSE policy
    fLegacyCorruptedStateExceptionsPolicy = false;
#endif // FEATURE_CORRUPTING_EXCEPTIONS

#ifdef _DEBUG
    fLogTransparencyErrors = false;
#endif // _DEBUG
    fLegacyLoadMscorsnOnStartup = false;
    fBypassStrongNameVerification = true;
    fGeneratePublisherEvidence = true;
    fEnforceFIPSPolicy = true;
    fLegacyHMACMode = false;
    fNgenBindOptimizeNonGac = false;
    fStressLog = false;
    fCacheBindingFailures = true;
    fDisableFusionUpdatesFromADManager = false;
    fDisableCommitThreadStack = false;
    fProbeForStackOverflow = true;
    
    INDEBUG(fStressLog = true;)

#ifdef FEATURE_CORECLR
    fVerifyAllOnLoad = false;
#endif
#ifdef _DEBUG
    fExpandAllOnLoad = false;
    fDebuggable = false;
    fStressOn = false;
    apiThreadStressCount = 0;
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

    fDoAllowUntrustedCallerChecks = true;
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
    
    // TlbImp Stuff
    fTlbImpSkipLoading = false;

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

#ifdef _TARGET_AMD64_
    pDisableNativeImageLoadList = NULL;
#endif

    // new loader behavior switches

    m_fDeveloperInstallation = false;

    pZapSet = DEFAULT_ZAP_SET;

#ifdef FEATURE_LOADER_OPTIMIZATION
    dwSharePolicy = AppDomain::SHARE_POLICY_UNSPECIFIED;
#endif

#if defined(_TARGET_X86_) || defined(_TARGET_AMD64_)
    dwDisableStackwalkCache = 0;
#else // _TARGET_X86_
    dwDisableStackwalkCache = 1;
#endif // _TARGET_X86_

    fUseNewCrossDomainRemoting = 1;
    
    szZapBBInstr     = NULL;
    szZapBBInstrDir  = NULL;

    fAppDomainUnload = true;
    dwADURetryCount=1000;

#ifdef _DEBUG
    fAppDomainLeaks = DEFAULT_APP_DOMAIN_LEAKS;

    // interop logging
    m_pTraceIUnknown = NULL;
    m_TraceWrapper = 0;
#endif

    iNgenHardBind = NGEN_HARD_BIND_DEFAULT;
#ifdef _DEBUG
    dwNgenForceFailureMask  = 0;
    dwNgenForceFailureCount = 0;
    dwNgenForceFailureKind  = 0;
#endif

    iGCPollType = GCPOLL_TYPE_DEFAULT;

#ifdef _DEBUG
    fGenerateStubForHost = FALSE;
    fShouldInjectFault = 0;
    testThreadAbort = 0;
    testADUnload = 0;
#endif

#ifdef FEATURE_COMINTEROP
    m_fComInsteadOfManagedRemoting = false;
#endif
    m_fInteropValidatePinnedObjects = false;
    m_fInteropLogArguments = false;

#if defined(_DEBUG) && defined(STUBLINKER_GENERATES_UNWIND_INFO)
    fStubLinkerUnwindInfoVerificationOn = FALSE;
#endif

#if defined(_DEBUG) && defined(WIN64EXCEPTIONS)
    fSuppressLockViolationsOnReentryFromOS = false;
#endif

#if defined(_DEBUG) && defined(_TARGET_AMD64_)
    // For determining if we should force generation of long jump dispatch stubs.
    m_cGenerateLongJumpDispatchStubRatio = (size_t)(-1);
    m_cDispatchStubsGenerated = 0;
#endif

#if defined(_DEBUG)
    bDiagnosticSuspend = false;
#endif
    
    // After initialization, register the code:#GetConfigValueCallback method with code:CLRConfig to let
    // CLRConfig access config files. This is needed because CLRConfig lives outside the VM and can't
    // statically link to EEConfig.
    CLRConfig::RegisterGetConfigValueCallback(&GetConfigValueCallback);

#ifdef FEATURE_WIN_DB_APPCOMPAT
    InitWinAppCompatDBApis();
#endif // FEATURE_WIN_DB_APPCOMPAT

    return S_OK;
}

#ifdef _DEBUG
static int DumpConfigTable(ConfigStringHashtable* table, __in_z LPCSTR label, int count)
{
    LIMITED_METHOD_CONTRACT;
    LOG((LF_ALWAYS, LL_ALWAYS, label, count++));
    LOG((LF_ALWAYS, LL_ALWAYS, "*********************************\n", count++));
    for(ConfigStringHashtable::Iterator iter = table->Begin(), end = table->End(); iter != end; iter++)
    {
        ConfigStringKeyValuePair * pair = *(iter);
        LPCWSTR keyString = pair->key;
        LPCWSTR data = pair->value;
        LOG((LF_ALWAYS, LL_ALWAYS, "%S = %S\n", keyString, data));
    }
    LOG((LF_ALWAYS, LL_ALWAYS, "\n"));
    return count;
}
#endif

/**************************************************************/
HRESULT EEConfig::Cleanup()
{
    CONTRACTL {
        FORBID_FAULT;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;
    
#ifdef _DEBUG
    if (g_pConfig) {
        // TODO: Do we even need this? CLRConfig::GetConfigValue has FORBID_FAULT in its contract. 
        FAULT_NOT_FATAL();  // If GetConfigValue fails the alloc, that's ok. 
        
        DWORD setting = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DumpConfiguration);
        if (setting != 0) 
       {
            ConfigList::ConfigIter iter(&m_Configuration);
            int count = 0;
            for(ConfigStringHashtable* table = iter.Next();table; table = iter.Next()) 
            {
                count = DumpConfigTable(table, "\nSystem Configuration Table: %d\n", count);
            }
            ConfigList::ConfigIter iter2(&m_Configuration);
            count = 0;
            for (ConfigStringHashtable* table = iter2.Previous();table; table = iter2.Previous()) 
            {
                count = DumpConfigTable(table, "\nApplication Configuration Table: %d\n", count);
            }
        }
    }
#endif

    if (m_fFreepZapSet)
        delete[] pZapSet;
    delete[] szZapBBInstr;
    
    if (pRequireZapsList)
        delete pRequireZapsList;
    
    if (pRequireZapsExcludeList)
        delete pRequireZapsExcludeList;

#ifdef _DEBUG
    if (pForbidZapsList)
        delete pForbidZapsList;
    
    if (pForbidZapsExcludeList)
        delete pForbidZapsExcludeList;
#endif

#ifdef _TARGET_AMD64_
    if (pDisableNativeImageLoadList)
        delete pDisableNativeImageLoadList;
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
HRESULT EEConfig::GetConfigString_DontUse_(__in_z LPCWSTR name, __deref_out_z LPWSTR *outVal, BOOL fPrependCOMPLUS, ConfigSearch direction)
{ 
    CONTRACT(HRESULT) {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT (CONTRACT_RETURN E_OUTOFMEMORY);
        PRECONDITION(CheckPointer(name));
        POSTCONDITION(CheckPointer(outVal, NULL_OK));
    } CONTRACT_END;

    LPWSTR pvalue = REGUTIL::GetConfigString_DontUse_(name, fPrependCOMPLUS); 
    if(pvalue == NULL && g_pConfig != NULL)
    {
        LPCWSTR pResult;
        if(SUCCEEDED(g_pConfig->GetConfiguration_DontUse_(name, direction, &pResult)) && pResult != NULL)
        {
            size_t len = wcslen(pResult) + 1;
            pvalue = new (nothrow) WCHAR[len];
            if (pvalue == NULL)
            {
                RETURN E_OUTOFMEMORY;
            }
            
            wcscpy_s(pvalue,len,pResult);
        }
    }

    *outVal = pvalue;
        
    RETURN S_OK;
}


//
// NOTE: This function is deprecated; use the CLRConfig class instead. 
// To use the CLRConfig class, add an entry in file:../inc/CLRConfigValues.h.
// 
DWORD EEConfig::GetConfigDWORD_DontUse_(__in_z LPCWSTR name, DWORD defValue, DWORD level, BOOL fPrependCOMPLUS, ConfigSearch direction)
{    
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(name));
    } CONTRACTL_END;

    // <TODO>@TODO: After everyone has moved off registry, key remove the following line in golden</TODO>
    DWORD result = REGUTIL::GetConfigDWORD_DontUse_(name, defValue, (REGUTIL::CORConfigLevel)level, fPrependCOMPLUS); 
    if(result == defValue && g_pConfig != NULL)
    {
        LPCWSTR pvalue;
        if(SUCCEEDED(g_pConfig->GetConfiguration_DontUse_(name, direction, &pvalue)) && pvalue != NULL)
        {
            WCHAR *end;
            errno = 0;
            result = wcstoul(pvalue, &end, 0);
            // errno is ERANGE if the number is out of range, and end is set to pvalue if
            // no valid conversion exists.
            if (errno == ERANGE || end == pvalue)
            {
                result = defValue;
            }
        }
    }

    return result;
}

//
// NOTE: This function is deprecated; use the CLRConfig class instead. 
// To use the CLRConfig class, add an entry in file:../inc/CLRConfigValues.h.
//
// Note for PAL: right now PAL does not have a _wcstoui64 API, so I am temporarily reading in all numbers as 
// a 32-bit number. When we have the _wcstoui64 API on MAC we will use that instead of wcstoul.
ULONGLONG EEConfig::GetConfigULONGLONG_DontUse_(__in_z LPCWSTR name, ULONGLONG defValue, DWORD level, BOOL fPrependCOMPLUS, ConfigSearch direction)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(name));
    } CONTRACTL_END;

    // <TODO>@TODO: After everyone has moved off registry, key remove the following line in golden</TODO>
    ULONGLONG result = REGUTIL::GetConfigULONGLONG_DontUse_(name, defValue, (REGUTIL::CORConfigLevel)level, fPrependCOMPLUS); 
    if(result == defValue && g_pConfig != NULL)
    {
        LPCWSTR pvalue;
        if(SUCCEEDED(g_pConfig->GetConfiguration_DontUse_(name, direction, &pvalue)) && pvalue != NULL)
        {
            WCHAR *end;
            errno = 0;
            result = _wcstoui64(pvalue, &end, 0);
            // errno is ERANGE if the number is out of range, and end is set to pvalue if
            // no valid conversion exists.
            if (errno == ERANGE || end == pvalue)
            {
                result = defValue;
            }
        }
    }

    return result;
}

//
// NOTE: This function is deprecated; use the CLRConfig class instead. 
// To use the CLRConfig class, add an entry in file:../inc/CLRConfigValues.h.
// 
// This is very similar to GetConfigDWORD, except that it favors the settings in config files over those in the
// registry. This is the Shim's policy with configuration flags, and there are a few flags in EEConfig that adhere
// to this policy.
//
DWORD EEConfig::GetConfigDWORDFavoringConfigFile_DontUse_(__in_z LPCWSTR name,
                                                 DWORD defValue,
                                                 DWORD level,
                                                 BOOL fPrependCOMPLUS,
                                                 ConfigSearch direction)
{    
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(name));
    } CONTRACTL_END;

    DWORD result = defValue;

    if (g_pConfig != NULL)
    {
        LPCWSTR pvalue;
        if (SUCCEEDED(g_pConfig->GetConfiguration_DontUse_(name, direction, &pvalue)) && pvalue != NULL)
        {
            WCHAR *end = NULL;
            errno = 0;
            result = wcstoul(pvalue, &end, 0);
            // errno is ERANGE if the number is out of range, and end is set to pvalue if
            // no valid conversion exists.
            if (errno == ERANGE || end == pvalue)
            {
                result = defValue;
            }
        }
        else
        {
            result = REGUTIL::GetConfigDWORD_DontUse_(name, defValue, (REGUTIL::CORConfigLevel)level, fPrependCOMPLUS);
        }
    }

    return result;
}

//
// NOTE: This function is deprecated; use the CLRConfig class instead. 
// To use the CLRConfig class, add an entry in file:../inc/CLRConfigValues.h.
// 
DWORD EEConfig::GetConfigDWORDInternal_DontUse_(__in_z LPCWSTR name, DWORD defValue, DWORD level, BOOL fPrependCOMPLUS, ConfigSearch direction)
{    
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(name));
    } CONTRACTL_END;

    // <TODO>@TODO: After everyone has moved off registry, key remove the following line in golden</TODO>
    DWORD result = REGUTIL::GetConfigDWORD_DontUse_(name, defValue, (REGUTIL::CORConfigLevel)level, fPrependCOMPLUS); 
    if(result == defValue)
    {
        LPCWSTR pvalue;
        if(SUCCEEDED(GetConfiguration_DontUse_(name, direction, &pvalue)) && pvalue != NULL)
        {
            WCHAR *end = NULL;
            errno = 0;
            result = wcstoul(pvalue, &end, 0);
            // errno is ERANGE if the number is out of range, and end is set to pvalue if
            // no valid conversion exists.
            if (errno == ERANGE || end == pvalue)
            {
                result = defValue;
            }
        }
    }
    return result;
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

#ifdef _DEBUG
    iFastGCStress       = GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_FastGCStress, iFastGCStress);

    IfFailRet(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_GcCoverage, (LPWSTR*)&pszGcCoverageOnMethod));
    pszGcCoverageOnMethod = NarrowWideChar((LPWSTR)pszGcCoverageOnMethod);
    iGCLatencyMode = GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_GCLatencyMode, iGCLatencyMode);
#endif

    if (CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_ARMEnabled))
    {
        g_fEnableARM = TRUE;
    }

    int forceGCconcurrent = CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_gcConcurrent);
    if ((forceGCconcurrent > 0) || (forceGCconcurrent == -1 && g_IGCconcurrent))
        iGCconcurrent = TRUE;
    
    // Disable concurrent GC during ngen for the rare case a GC gets triggered, causing problems
    if (IsCompilationProcess())
        iGCconcurrent = FALSE;
    
#ifdef _DEBUG
    fAppDomainLeaks = GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_AppDomainAgilityChecked, DEFAULT_APP_DOMAIN_LEAKS) == 1;
#endif

#if defined(STRESS_HEAP) || defined(_DEBUG)
    iGCStress           =  CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_GCStress);
#endif

#ifdef STRESS_HEAP
    BOOL bGCStressAndHeapVerifyAllowed = true;
    iGCStressMix        =  CLRConfig::GetConfigValue(CLRConfig::INTERNAL_GCStressMix);
    iGCStressStep       =  CLRConfig::GetConfigValue(CLRConfig::INTERNAL_GCStressStep);

    // For GC stress mix mode ensure reasonable defaults
    if (iGCStressMix != 0)
    {
        if (iGCStress == 0)
            iGCStress |= int(GCSTRESS_ALLOC) | int(GCSTRESS_TRANSITION);
        if (iGCStressStep == 0 || iGCStressStep == 1)
            iGCStressStep = 0x10;
    }

    if (iGCStress)
    {
        LPWSTR pszGCStressExe = NULL;

#ifdef _DEBUG
        // If GCStress is turned on, then perform AppDomain agility checks in debug builds
        fAppDomainLeaks = 1;
#endif

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
            if (forceGCconcurrent > 0)
            {
#ifdef _DEBUG
                iFastGCStress = 0;
#endif
                iGCStress |= int(GCSTRESS_ALLOC) | int(GCSTRESS_TRANSITION);
            }
            else
            {
                // If GCStress was enabled, and 
                // If GcConcurrent was NOT explicitly specified in the environment, and
                // If GSCtressMix was NOT specified
                // Then let's turn off concurrent GC since it make objects move less
                if (iGCStressMix == 0)
                {
                    iGCconcurrent   =
                    g_IGCconcurrent = 0;
                }
            }
        }
        else
        {
            iGCStress = 0;
            iGCStressMix  = 0;
            iGCStressStep = 1;
        }
    }

#ifdef VERIFY_HEAP
    if (bGCStressAndHeapVerifyAllowed)
    {
        iGCHeapVerify       =  GetConfigDWORD_DontUse_(CLRConfig::UNSUPPORTED_HeapVerify, iGCHeapVerify);
    }
#endif

#endif //STRESS_HEAP

#ifdef _WIN64
    if (!iGCSegmentSize) iGCSegmentSize =  GetConfigULONGLONG_DontUse_(CLRConfig::UNSUPPORTED_GCSegmentSize, iGCSegmentSize);
    if (!iGCgen0size) iGCgen0size = GetConfigULONGLONG_DontUse_(CLRConfig::UNSUPPORTED_GCgen0size, iGCgen0size);
#else
    if (!iGCSegmentSize) iGCSegmentSize =  GetConfigDWORD_DontUse_(CLRConfig::UNSUPPORTED_GCSegmentSize, iGCSegmentSize);
    if (!iGCgen0size) iGCgen0size = GetConfigDWORD_DontUse_(CLRConfig::UNSUPPORTED_GCgen0size, iGCgen0size);
#endif //_WIN64

    if (g_IGCHoardVM)
        iGCHoardVM = g_IGCHoardVM;
    else
        iGCHoardVM = GetConfigDWORD_DontUse_(CLRConfig::UNSUPPORTED_GCRetainVM, iGCHoardVM);

    if (!iGCLOHCompactionMode) iGCLOHCompactionMode = GetConfigDWORD_DontUse_(CLRConfig::UNSUPPORTED_GCLOHCompact, iGCLOHCompactionMode);

#ifdef GCTRIMCOMMIT
    if (g_IGCTrimCommit)
        iGCTrimCommit = g_IGCTrimCommit;
    else
        iGCTrimCommit = GetConfigDWORD_DontUse_(CLRConfig::EXTERNAL_gcTrimCommitOnLowMemory, iGCTrimCommit);
#endif

#ifdef FEATURE_CONSERVATIVE_GC
    iGCConservative =  (CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_gcConservative) != 0);
#endif // FEATURE_CONSERVATIVE_GC

#ifdef _WIN64
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

    iGCForceCompact     =  GetConfigDWORD_DontUse_(CLRConfig::UNSUPPORTED_gcForceCompact, iGCForceCompact);

    fStressLog        =  GetConfigDWORD_DontUse_(CLRConfig::UNSUPPORTED_StressLog, fStressLog) != 0;
    fForceEnc         =  GetConfigDWORD_DontUse_(CLRConfig::UNSUPPORTED_ForceEnc, fForceEnc) != 0;
    
#ifdef STRESS_THREAD
    dwStressThreadCount =  GetConfigDWORD_DontUse_(CLRConfig::EXTERNAL_StressThreadCount, dwStressThreadCount);
#endif

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

#ifdef _TARGET_AMD64_
    if (!IsCompilationProcess())
    {
        NewArrayHolder<WCHAR> wszDisableNativeImageLoadList;
        IfFailRet(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_DisableNativeImageLoadList, &wszDisableNativeImageLoadList));
        if (wszDisableNativeImageLoadList)
            pDisableNativeImageLoadList = new AssemblyNamesList(wszDisableNativeImageLoadList);
    }
#endif

#ifdef FEATURE_LOADER_OPTIMIZATION
    dwSharePolicy           = GetConfigDWORD_DontUse_(CLRConfig::EXTERNAL_LoaderOptimization, dwSharePolicy);
#endif

#ifdef FEATURE_DOUBLE_ALIGNMENT_HINT
    DoubleArrayToLargeObjectHeapThreshold = GetConfigDWORD_DontUse_(CLRConfig::UNSUPPORTED_DoubleArrayToLargeObjectHeap, DoubleArrayToLargeObjectHeapThreshold);
#endif

#ifdef FEATURE_PREJIT
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
#endif

#ifdef FEATURE_FUSION
    IfFailRet(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_ZapSet, (LPWSTR*)&pZapSet));

    m_fFreepZapSet = true;
    
    if (pZapSet == NULL)
    {
        m_fFreepZapSet = false;
        pZapSet = W("");
    }
    if (wcslen(pZapSet) > 3)
    {
        _ASSERTE(!"Zap Set String must be less than 3 chars");
        delete[] pZapSet;
        m_fFreepZapSet = false;
        pZapSet = W("");
    }

    fNgenBindOptimizeNonGac = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_NgenBind_OptimizeNonGac) != 0;
#endif

    dwDisableStackwalkCache = GetConfigDWORD_DontUse_(CLRConfig::EXTERNAL_DisableStackwalkCache, dwDisableStackwalkCache);

#ifdef FEATURE_REMOTING
    fUseNewCrossDomainRemoting = GetConfigDWORD_DontUse_(CLRConfig::EXTERNAL_UseNewCrossDomainRemoting, fUseNewCrossDomainRemoting);
#endif

#ifdef _DEBUG
    IfFailRet (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_BreakOnClassLoad, (LPWSTR*) &pszBreakOnClassLoad));
    pszBreakOnClassLoad = NarrowWideChar((LPWSTR)pszBreakOnClassLoad);
#endif

    dwSpinInitialDuration = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_SpinInitialDuration);
    dwSpinBackoffFactor = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_SpinBackoffFactor);
    dwSpinLimitProcCap = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_SpinLimitProcCap);
    dwSpinLimitProcFactor = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_SpinLimitProcFactor);
    dwSpinLimitConstant = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_SpinLimitConstant);
    dwSpinRetryCount = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_SpinRetryCount);

    fJitFramed = (GetConfigDWORD_DontUse_(CLRConfig::UNSUPPORTED_JitFramed, fJitFramed) != 0);
    fJitAlignLoops = (GetConfigDWORD_DontUse_(CLRConfig::UNSUPPORTED_JitAlignLoops, fJitAlignLoops) != 0);
    fJitMinOpts = (GetConfigDWORD_DontUse_(CLRConfig::UNSUPPORTED_JITMinOpts, fJitMinOpts) == 1);
    iJitOptimizeType      =  GetConfigDWORD_DontUse_(CLRConfig::EXTERNAL_JitOptimizeType, iJitOptimizeType);
    if (iJitOptimizeType > OPT_RANDOM)     iJitOptimizeType = OPT_DEFAULT;

#ifdef FEATURE_REJIT
    fAddRejitNops = (GetConfigDWORD_DontUse_(CLRConfig::UNSUPPORTED_AddRejitNops, fAddRejitNops) != 0);
#endif

#ifdef _TARGET_X86_
    fPInvokeRestoreEsp = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_Jit_NetFx40PInvokeStackResilience);
#endif

#ifndef FEATURE_CORECLR
    // These two values respect the Shim's policy of favoring config files over registry settings.
    fLegacyNullReferenceExceptionPolicy = (GetConfigDWORDFavoringConfigFile_DontUse_(CLRConfig::UNSUPPORTED_legacyNullReferenceExceptionPolicy,
                                                                            fLegacyNullReferenceExceptionPolicy) != 0);
    fLegacyUnhandledExceptionPolicy = (GetConfigDWORDFavoringConfigFile_DontUse_(CLRConfig::UNSUPPORTED_legacyUnhandledExceptionPolicy,
                                                                        fLegacyUnhandledExceptionPolicy) != 0);

#ifdef FEATURE_CORRUPTING_EXCEPTIONS
    // Check if the user has overriden how Corrupted State Exceptions (CSE) will be handled. If the 
    // <runtime> section of app.exe.config has "legacyCorruptedStateExceptionsPolicy" set to 1, then
    // V4 runtime will treat CSE in the same fashion as V2.
    fLegacyCorruptedStateExceptionsPolicy = (CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_legacyCorruptedStateExceptionsPolicy) != 0);
#endif // FEATURE_CORRUPTING_EXCEPTIONS

    fLegacyVirtualMethodCallVerification = (GetConfigDWORDFavoringConfigFile_DontUse_(CLRConfig::EXTERNAL_legacyVirtualMethodCallVerification,
                                                                             fLegacyVirtualMethodCallVerification,
                                                                             REGUTIL::COR_CONFIG_ALL, TRUE,
                                                                             CONFIG_SYSTEMONLY) != 0);

    fLegacyApartmentInitPolicy = (GetConfigDWORDFavoringConfigFile_DontUse_(CLRConfig::EXTERNAL_legacyApartmentInitPolicy, 
                                                                    fLegacyApartmentInitPolicy) != 0);

    fLegacyComHierarchyVisibility = (GetConfigDWORDFavoringConfigFile_DontUse_(CLRConfig::EXTERNAL_legacyComHierarchyVisibility, 
                                                                    fLegacyComHierarchyVisibility) != 0);

    fLegacyComVTableLayout = (GetConfigDWORDFavoringConfigFile_DontUse_(CLRConfig::EXTERNAL_legacyComVTableLayout, 
                                                                    fLegacyComVTableLayout) != 0);
    fNewComVTableLayout = (GetConfigDWORDFavoringConfigFile_DontUse_(CLRConfig::EXTERNAL_newComVTableLayout, 
                                                                    fNewComVTableLayout) != 0);
        
    if (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_legacyImpersonationPolicy) != 0)
        iImpersonationPolicy = IMP_NOFLOW;
    else if (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_alwaysFlowImpersonationPolicy) != 0)
        iImpersonationPolicy = IMP_ALWAYSFLOW;

    fLegacyLoadMscorsnOnStartup = (GetConfigDWORDFavoringConfigFile_DontUse_(CLRConfig::UNSUPPORTED_legacyLoadMscorsnOnStartup, 
                                                                    fLegacyLoadMscorsnOnStartup) != 0);
    fBypassStrongNameVerification = (GetConfigDWORDFavoringConfigFile_DontUse_(W("bypassTrustedAppStrongNames"), fBypassStrongNameVerification) != 0) &&  // App opted in
                                    (GetConfigDWORD_DontUse_(SN_CONFIG_BYPASS_POLICY_W, TRUE, REGUTIL::COR_CONFIG_MACHINE) != 0);                        // And the machine policy allows for bypass
    fGeneratePublisherEvidence = (GetConfigDWORDFavoringConfigFile_DontUse_(CLRConfig::EXTERNAL_generatePublisherEvidence, fGeneratePublisherEvidence) != 0);
    fEnforceFIPSPolicy = (GetConfigDWORDFavoringConfigFile_DontUse_(CLRConfig::EXTERNAL_enforceFIPSPolicy, fEnforceFIPSPolicy) != 0);
    fLegacyHMACMode = (GetConfigDWORDFavoringConfigFile_DontUse_(CLRConfig::EXTERNAL_legacyHMACMode, fLegacyHMACMode) != 0);

    fCacheBindingFailures = !(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_disableCachingBindingFailures));
    fUseLegacyIdentityFormat = 
#ifdef FEATURE_APPX
                               AppX::IsAppXProcess() ||
#endif
                               (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_useLegacyIdentityFormat) != 0);
    fDisableFusionUpdatesFromADManager = (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_disableFusionUpdatesFromADManager) != 0);
    fDisableCommitThreadStack = (GetConfigDWORDFavoringConfigFile_DontUse_(CLRConfig::EXTERNAL_disableCommitThreadStack, fDisableCommitThreadStack) != 0);
    fProbeForStackOverflow = !(CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_disableStackOverflowProbing));
#endif // FEATURE_CORECLR
    
#ifdef _DEBUG
    fDebuggable         = (GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_JitDebuggable,      fDebuggable)         != 0);
    fStressOn           = (GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_StressOn,           fStressOn)           != 0);
    apiThreadStressCount = GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_APIThreadStress,     apiThreadStressCount);

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

    fLogTransparencyErrors = CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_Security_LogTransparencyErrors) != 0;

    // TlbImp stuff
    fTlbImpSkipLoading = (GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_TlbImpSkipLoading, fTlbImpSkipLoading) != 0);

    iExposeExceptionsInCOM = GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_ExposeExceptionsInCOM, iExposeExceptionsInCOM);
#endif

#ifdef FEATURE_COMINTEROP
    IfFailRet(CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_LogCCWRefCountChange, (LPWSTR*)&pszLogCCWRefCountChange));
    pszLogCCWRefCountChange = NarrowWideChar((LPWSTR)pszLogCCWRefCountChange);
    if (pszLogCCWRefCountChange != NULL)
        bLogCCWRefCountChange = true;

    fEnableRCWCleanupOnSTAShutdown = (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_EnableRCWCleanupOnSTAShutdown) != 0);
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_CORECLR
    //Eager verification of all assemblies.
    fVerifyAllOnLoad = (GetConfigDWORD_DontUse_(CLRConfig::EXTERNAL_VerifyAllOnLoad, fVerifyAllOnLoad) != 0);
#endif //FEATURE_CORECLR

#ifdef _DEBUG
    fExpandAllOnLoad = (GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_ExpandAllOnLoad, fExpandAllOnLoad) != 0);
#endif //_DEBUG

#ifdef FEATURE_FUSION
    if(g_pConfig) {
        LPCWSTR result = NULL;
        if(SUCCEEDED(g_pConfig->GetConfiguration_DontUse_(CLRConfig::EXTERNAL_developerInstallation, CONFIG_SYSTEM, &result)) && result)
        {
            // <TODO> CTS, add addtional checks to ensure this is an SDK installation </TODO>
            if(SString::_wcsicmp(result, W("true")) == 0)
                m_fDeveloperInstallation = true;
        }
    }
#endif

#ifdef AD_NO_UNLOAD
    fAppDomainUnload = (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_AppDomainNoUnload) == 0);
#endif
    dwADURetryCount=GetConfigDWORD_DontUse_(CLRConfig::EXTERNAL_ADURetryCount, dwADURetryCount);
    if (dwADURetryCount==(DWORD)-1)
    {
        _ASSERTE(!"Reserved value");
        dwADURetryCount=(DWORD)-2;
    }

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

    fDoAllowUntrustedCallerChecks =  
        (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_SupressAllowUntrustedCallerChecks) != 1);


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
    iPerfAllocsSizeThreshold    = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_PerfAllocsSizeThreshold);

    fGenerateStubForHost = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_GenerateStubForHost);

    fShouldInjectFault = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_InjectFault);

    testThreadAbort = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_HostTestThreadAbort);
    testADUnload = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_HostTestADUnload);

#endif //_DEBUG

#ifdef FEATURE_COMINTEROP
    m_fComInsteadOfManagedRemoting = (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_ComInsteadOfManagedRemoting) != 0);
#endif // FEATURE_COMINTEROP
    m_fInteropValidatePinnedObjects = (CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_InteropValidatePinnedObjects) != 0);
    m_fInteropLogArguments = (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_InteropLogArguments) != 0);

#ifdef FEATURE_PREJIT
#ifndef FEATURE_CORECLR
    DWORD iNgenHardBindOverride = GetConfigDWORD_DontUse_(CLRConfig::EXTERNAL_HardPrejitEnabled, iNgenHardBind);
    _ASSERTE(iNgenHardBindOverride < NGEN_HARD_BIND_COUNT);
    if (iNgenHardBindOverride < NGEN_HARD_BIND_COUNT)
        iNgenHardBind = NgenHardBindType(iNgenHardBindOverride);
#endif
#ifdef _DEBUG
    dwNgenForceFailureMask  = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_NgenForceFailureMask);
    dwNgenForceFailureCount = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_NgenForceFailureCount);
    dwNgenForceFailureKind  = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_NgenForceFailureKind);
#endif
#endif // FEATURE_PREJIT

    DWORD iGCPollTypeOverride = GetConfigDWORD_DontUse_(CLRConfig::EXTERNAL_GCPollType, iGCPollType);

#ifndef FEATURE_HIJACK
    // Platforms that do not support hijacking MUST support GC polling.
    // Reject attempts by the user to configure the GC polling type as 
    // GCPOLL_TYPE_HIJACK.
    _ASSERTE(EEConfig::GCPOLL_TYPE_HIJACK != iGCPollTypeOverride);
    if (EEConfig::GCPOLL_TYPE_HIJACK == iGCPollTypeOverride)
        iGCPollTypeOverride = EEConfig::GCPOLL_TYPE_DEFAULT;
#endif

    _ASSERTE(iGCPollTypeOverride < GCPOLL_TYPE_COUNT);
    if (iGCPollTypeOverride < GCPOLL_TYPE_COUNT)
        iGCPollType = GCPollType(iGCPollTypeOverride);

#if defined(_DEBUG) && defined(WIN64EXCEPTIONS)
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

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    // Get the symbol reading policy setting which is maintained by the hosting API (since it can be overridden there)
    const DWORD notSetToken = 0xFFFFFFFF;
    DWORD iSymbolReadingConfig = GetConfigDWORDFavoringConfigFile_DontUse_(CLRConfig::EXTERNAL_SymbolReadingPolicy, notSetToken );
    if( iSymbolReadingConfig != notSetToken &&
        iSymbolReadingConfig <= eSymbolReadingFullTrustOnly )
    {
        ESymbolReadingPolicy policy = ESymbolReadingPolicy(iSymbolReadingConfig);
        CCLRDebugManager::SetSymbolReadingPolicy( policy, eSymbolReadingSetByConfig );
    }
#endif // FEATURE_INCLUDE_ALL_INTERFACES

#if defined(_DEBUG) && defined(_TARGET_AMD64_)
    m_cGenerateLongJumpDispatchStubRatio = GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_GenerateLongJumpDispatchStubRatio,
                                                          static_cast<DWORD>(m_cGenerateLongJumpDispatchStubRatio));
#endif

#if defined(_DEBUG)
    bDiagnosticSuspend = (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DiagnosticSuspend) != 0);
#endif

    dwSleepOnExit = CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_SleepOnExit);

#ifdef FEATURE_APPX
    dwWindows8ProfileAPICheckFlag = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_Windows8ProfileAPICheckFlag);
#endif

    return hr;
}

//
// #GetConfigValueCallback
// Provides a way for code:CLRConfig to access configuration file values.
// 
// static
HRESULT EEConfig::GetConfigValueCallback(__in_z LPCWSTR pKey, __deref_out_opt LPCWSTR* pValue, BOOL systemOnly, BOOL applicationFirst)
{
    CONTRACT (HRESULT) {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT; 
        PRECONDITION(CheckPointer(pValue)); 
        PRECONDITION(CheckPointer(pKey)); 
    } CONTRACT_END;
    
    // Ensure that both options aren't set.
    _ASSERTE(!(systemOnly && applicationFirst));

    if(g_pConfig != NULL)
    {        
        ConfigSearch direction = CONFIG_SYSTEM;
        if(systemOnly)
        {
            direction = CONFIG_SYSTEMONLY;
        }
        else if(applicationFirst)
        {
            direction = CONFIG_APPLICATION;
        }

        RETURN g_pConfig->GetConfiguration_DontUse_(pKey, direction, pValue);
    }
    else
    {
        RETURN E_FAIL;
    }
}

HRESULT EEConfig::GetConfiguration_DontUse_(__in_z LPCWSTR pKey, ConfigSearch direction, __deref_out_opt LPCWSTR* pValue)
{
    CONTRACT (HRESULT) {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT; // TODO: Verify this does not do anything that would make it so_intolerant
        PRECONDITION(CheckPointer(pValue)); 
        PRECONDITION(CheckPointer(pKey)); 
    } CONTRACT_END;

    Thread *pThread = GetThread();
    ConfigStringKeyValuePair * pair = NULL;

    *pValue = NULL;
    ConfigList::ConfigIter iter(&m_Configuration);
    
    switch(direction) {
    case CONFIG_SYSTEMONLY:
    {
        // for things that only admin should be able to set
        ConfigStringHashtable* table = iter.Next();
        if(table != NULL)
        {
            BEGIN_SO_INTOLERANT_CODE_NOTHROW(pThread, RETURN E_FAIL;)
            pair = table->Lookup(pKey);
            END_SO_INTOLERANT_CODE
            if(pair != NULL)
            {
                *pValue = pair->value;
                RETURN S_OK;
            }
        }
        RETURN E_FAIL;
    }
    case CONFIG_SYSTEM:
    {
        for(ConfigStringHashtable* table = iter.Next();
            table != NULL;
            table = iter.Next())
        {
            BEGIN_SO_INTOLERANT_CODE_NOTHROW(pThread, RETURN E_FAIL;)
            pair = table->Lookup(pKey);
            END_SO_INTOLERANT_CODE
            if(pair != NULL)
            {
                *pValue = pair->value;
                RETURN S_OK;
            }
        }
        RETURN E_FAIL;
    }
    case CONFIG_APPLICATION: {
        for(ConfigStringHashtable* table = iter.Previous();
            table != NULL;
            table = iter.Previous())
        {
            BEGIN_SO_INTOLERANT_CODE_NOTHROW(pThread, RETURN E_FAIL;)
            pair = table->Lookup(pKey);
            END_SO_INTOLERANT_CODE
            if(pair != NULL)
            {
                *pValue = pair->value;
                RETURN S_OK;
            }
        }
        RETURN E_FAIL;
    }
    default:
        RETURN E_FAIL;
    }
}        

LPCWSTR EEConfig::GetProcessBindingFile()
{
    LIMITED_METHOD_CONTRACT;
    return g_pszHostConfigFile;
}

SIZE_T EEConfig::GetSizeOfProcessBindingFile()
{
    LIMITED_METHOD_CONTRACT;
    return g_dwHostConfigFile;
}

#if !defined(FEATURE_CORECLR) && !defined(CROSSGEN_COMPILE) // unimpactful install --> no config files

/**************************************************************/
static void MessageBoxParseError(HRESULT hr, __in_z LPCWSTR wszFile);

#define IfFailParseError(FILE, ISAPPCONFIG, ...) \
    do \
    { \
        /* On error, always show an error dialog and return an error result when process is immersive; */ \
        /* otherwise show dialog (conditionally for App config) and swallow error. */ \
        if (FAILED(hr = (__VA_ARGS__)) && (!(ISAPPCONFIG) || AppX::IsAppXProcess() || GetConfigDWORDInternal_DontUse_(CLRConfig::EXTERNAL_NotifyBadAppCfg,false))) \
        { \
            MessageBoxParseError(hr, FILE); \
            if (AppX::IsAppXProcess()) \
            { /* Fail on bad config in AppX process. */ \
                return hr; \
            } \
            else \
            { \
                hr = S_FALSE; \
            } \
        } \
    } while (false)

/**************************************************************/
HRESULT EEConfig::SetupConfiguration()
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    WCHAR version[_MAX_PATH];
    DWORD dwVersion = _MAX_PATH;

    HRESULT hr = S_OK;
    // Get the version location
    IfFailRet(GetCORVersionInternal(version, _MAX_PATH, & dwVersion));

    // See if the environment has specified an XML file
    NewArrayHolder<WCHAR> file(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_CONFIG));
    if(file != NULL)
    {
        IfFailParseError(file, false, AppendConfigurationFile(file, version));
    }

    // We need to read configuration information from 3 sources... the app config file, the
    // host supplied config file, and the machine.config file. The order in which we
    // read them are very import. If the different config sources specify the same config
    // setting, we will use the setting of the first one read.
    //
    // In the pecking order, machine.config should always have the final say. The host supplied config
    // file should follow, and lastly, the app config file.
    //
    // Note: the order we read them is not the order they are published. Need to read the AppConfig
    // first so that we can decide if we should import machine.config (yes in Classic, typically no
    // in AppX). We still publish in the order required as described above.

    enum
    {
        MachineConfig = 0,
        HostConfig    = 1,
        AppConfig     = 2,
        NumConfig     = 3,
    };

    ConfigSource * rgpSources[NumConfig] = { nullptr };

    // Create ConfigSource objects for all config files.
    for (size_t i = 0; i < NumConfig; ++i)
    {
        rgpSources[i] = new (nothrow) ConfigSource();
        if (rgpSources[i] == NULL)
        {
            while (i != 0)
            {
                --i;
                delete rgpSources[i];
                rgpSources[i] = nullptr;
            }
            return E_OUTOFMEMORY;
        }
    }

    // Publish ConfigSource objects in required order. It's ok that the file contents are imported below,
    // since we're in EEStartup and this data cannot be accessed by any other threads yet.
    for (size_t i = 0; i < NumConfig; ++i)
    {
        m_Configuration.Append(rgpSources[i]);
    }

    // ----------------------------------------------------
    // Import the app.config file, or in the case of an
    // AppX process check to make sure no app.config file
    // exists unless launched with AO_DESIGNMODE.
    // ----------------------------------------------------
    
    do
    {
        size_t cchProcExe=0;
        PathString wzProcExe;
        EX_TRY
        {



            // Get name of file used to create process
            if (g_pCachedModuleFileName)
            {
                wzProcExe.Set(g_pCachedModuleFileName);
                cchProcExe = wzProcExe.GetCount();
            }
            else
            {
                cchProcExe = WszGetModuleFileName(NULL, wzProcExe);

                if (cchProcExe == 0)
                {
                    hr = HRESULT_FROM_GetLastError();
                    break;
                }
            }

            if (cchProcExe != 0)
            {
                wzProcExe.Append(CONFIGURATION_EXTENSION);

                if (AppX::IsAppXProcess() && !AppX::IsAppXDesignMode())
                {
                    if (clr::fs::Path::Exists(wzProcExe))
                    {
                        hr = CLR_E_APP_CONFIG_NOT_ALLOWED_IN_APPX_PROCESS;
                        break;
                    }
                }
            }
        }
        EX_CATCH_HRESULT(hr);
        if (cchProcExe != 0)
        {
            IfFailParseError(wzProcExe, true, AppendConfigurationFile(wzProcExe, version));

            // We really should return a failure hresult if the app config file is bad, but that
            // would be a breaking change. Not sure if it's worth it yet.
            hr = S_OK;
            break;
        }
    } while (false);
    

    if (hr != S_OK)
        return hr;
    // ----------------------------------------------------
    // Import machine.config, if needed.
    // ----------------------------------------------------
    if (!AppX::IsAppXProcess() || AppX::IsAppXDesignMode())
    {
        WCHAR wzSystemDir[_MAX_PATH];
        DWORD cchSystemDir = COUNTOF(wzSystemDir);
        IfFailRet(GetInternalSystemDirectory(wzSystemDir, &cchSystemDir));

        // cchSystemDir already includes the NULL
        if(cchSystemDir + StrLen(MACHINE_CONFIGURATION_FILE) <= _MAX_PATH)
        {
            IfFailRet(StringCchCat(wzSystemDir, COUNTOF(wzSystemDir), MACHINE_CONFIGURATION_FILE));

            // CLR_STARTUP_OPT:
            // The machine.config file can be very large.  We cannot afford
            // to parse all of it at CLR startup time.
            //
            // Accordingly, we instruct the XML parser to stop parsing the
            // machine.config file when it sees the end of the
            // <runtime>...</runtime> section that holds our data (if any).
            //
            // By construction, this section is now placed near the top
            // of machine.config.
            // 
            IfFailParseError(wzSystemDir, false, ImportConfigurationFile(
                rgpSources[MachineConfig]->Table(), wzSystemDir, version, stopAfterRuntimeSection));

            if (hr == S_FALSE) // means that we couldn't find machine.config
                hr = S_OK;
        }
    }

    // ----------------------------------------------------
    // Import the host supplied config file, if needed.
    // ----------------------------------------------------
    // Cannot host an AppX managed process, so no need to check devModeEnabled.
    if (!AppX::IsAppXProcess())
    {
        if (GetProcessBindingFile() != NULL && GetSizeOfProcessBindingFile() > 0)
        {
            IfFailRet(ImportConfigurationFile(
                rgpSources[HostConfig]->Table(), GetProcessBindingFile(), version));
        }
    }

    return hr;
}

//
// There was an error 'hr' parsing the file 'wszFile'.
// Pop up a MessageBox reporting the error, unless the config setting
// 'NoGuiFromShim' is in effect.
//
static void MessageBoxParseError(HRESULT hr, __in_z LPCWSTR wszFile)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(FAILED(hr)); 
    } CONTRACTL_END;

    if (!REGUTIL::GetConfigDWORD_DontUse_(CLRConfig::UNSUPPORTED_NoGuiFromShim, FALSE))
    {
        EEMessageBoxCatastrophic(IDS_EE_CONFIGPARSER_ERROR, IDS_EE_CONFIGPARSER_ERROR_CAPTION, wszFile, hr);
    }
}

/**************************************************************/

STDAPI GetXMLObjectEx(IXMLParser **ppv);

HRESULT EEConfig::ImportConfigurationFile(
    ConfigStringHashtable* pTable,
    LPCWSTR pszFileName,
    LPCWSTR version,
    ParseCtl parseCtl)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pTable));
        PRECONDITION(CheckPointer(pszFileName));
        PRECONDITION(CheckPointer(version));
        INJECT_FAULT(return E_OUTOFMEMORY);
    } CONTRACTL_END;

    NonVMComHolder<IXMLParser>         pIXMLParser(NULL);
    NonVMComHolder<IStream>            pFile(NULL);
    NonVMComHolder<EEConfigFactory>    factory(NULL); 

    HRESULT hr = CreateConfigStreamHelper(pszFileName, &pFile);
    if(FAILED(hr)) goto Exit;

    hr = GetXMLObjectEx(&pIXMLParser);
    if(FAILED(hr)) goto Exit;

    factory = new (nothrow) EEConfigFactory(pTable, version, parseCtl);
    
    if ( ! factory) { 
        hr = E_OUTOFMEMORY; 
        goto Exit; 
    }
    factory->AddRef(); // RefCount = 1 

    
    hr = pIXMLParser->SetInput(pFile); // filestream's RefCount=2
    if ( ! SUCCEEDED(hr)) 
        goto Exit;

    hr = pIXMLParser->SetFactory(factory); // factory's RefCount=2
    if ( ! SUCCEEDED(hr)) 
        goto Exit;

    {
        CONTRACT_VIOLATION(ThrowsViolation); // @todo: Run() throws!
        hr = pIXMLParser->Run(-1);
    }
    
Exit:  
    if (hr == (HRESULT) XML_E_MISSINGROOT)
        hr = S_OK;
    else if (Assembly::FileNotFound(hr))
        hr = S_FALSE;

    return hr;
}

HRESULT EEConfig::AppendConfigurationFile(
    LPCWSTR pszFileName,
    LPCWSTR version,
    ParseCtl parseCtl)
{
    LIMITED_METHOD_CONTRACT;
    HRESULT hr = S_OK;

    ConfigStringHashtable* pTable = m_Configuration.Append();
    IfNullRet(pTable);

    return ImportConfigurationFile(pTable, pszFileName, version, parseCtl);
}


#endif // FEATURE_CORECLR && !CROSSGEN_COMPILE

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

#ifdef _TARGET_AMD64_
bool EEConfig::DisableNativeImageLoad(LPCUTF8 assemblyName) const
{
    LIMITED_METHOD_CONTRACT;

    if (pDisableNativeImageLoadList != NULL && pDisableNativeImageLoadList->IsInList(assemblyName))
        return true;

    return false;
}
#endif

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
