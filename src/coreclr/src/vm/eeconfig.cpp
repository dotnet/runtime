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
    iGCLOHThreshold = 0;
    iGCHeapCount = 0;
    iGCNoAffinitize = 0;
    iGCAffinityMask = 0;

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
    dwMonitorSpinCount = 0;

    dwJitHostMaxSlabCache = 0;

    iJitOptimizeType = OPT_DEFAULT;
    fJitFramed = false;
    fJitAlignLoops = false;
    fAddRejitNops = false;
    fJitMinOpts = false;
    fPInvokeRestoreEsp = (DWORD)-1;

    fLegacyNullReferenceExceptionPolicy = false;
    fLegacyUnhandledExceptionPolicy = false;

#ifdef FEATURE_CORRUPTING_EXCEPTIONS
    // By default, there is not pre-V4 CSE policy
    fLegacyCorruptedStateExceptionsPolicy = false;
#endif // FEATURE_CORRUPTING_EXCEPTIONS

    fNgenBindOptimizeNonGac = false;
    fStressLog = false;
    fProbeForStackOverflow = true;
    
    INDEBUG(fStressLog = true;)

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

#if defined(_TARGET_X86_) || defined(_TARGET_AMD64_)
    dwDisableStackwalkCache = 0;
#else // _TARGET_X86_
    dwDisableStackwalkCache = 1;
#endif // _TARGET_X86_

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

    iGCPollType = GCPOLL_TYPE_DEFAULT;

#ifdef _DEBUG
    fShouldInjectFault = 0;
    testThreadAbort = 0;
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

#if defined(FEATURE_TIERED_COMPILATION)
    fTieredCompilation = false;
    fTieredCompilation_QuickJit = false;
    fTieredCompilation_QuickJitForLoops = false;
    fTieredCompilation_CallCounting = false;
    tieredCompilation_CallCountThreshold = 1;
    tieredCompilation_CallCountingDelayMs = 0;
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

    // After initialization, register the code:#GetConfigValueCallback method with code:CLRConfig to let
    // CLRConfig access config files. This is needed because CLRConfig lives outside the VM and can't
    // statically link to EEConfig.
    CLRConfig::RegisterGetConfigValueCallback(&GetConfigValueCallback);

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

    bool gcConcurrentWasForced = false;
    // The CLRConfig value for UNSUPPORTED_gcConcurrent defaults to -1, and treats any
    // positive value as 'forcing' concurrent GC to be on. Because the standard logic
    // for mapping a DWORD CLRConfig to a boolean configuration treats -1 as true (just
    // like any other nonzero value), we will explicitly check the DWORD later if this
    // check returns false.
    gcConcurrentWasForced = Configuration::GetKnobBooleanValue(W("System.GC.Concurrent"), false);

    int gcConcurrentConfigVal = 0;
    if (!gcConcurrentWasForced)
    {
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
    iGCAffinityMask = GetConfigULONGLONG_DontUse_(CLRConfig::EXTERNAL_GCHeapAffinitizeMask, iGCAffinityMask);
    if (!iGCAffinityMask) iGCAffinityMask =  Configuration::GetKnobULONGLONGValue(W("System.GC.HeapAffinitizeMask"));
    if (!iGCSegmentSize) iGCSegmentSize =  GetConfigULONGLONG_DontUse_(CLRConfig::UNSUPPORTED_GCSegmentSize, iGCSegmentSize);
    if (!iGCgen0size) iGCgen0size = GetConfigULONGLONG_DontUse_(CLRConfig::UNSUPPORTED_GCgen0size, iGCgen0size);
#else
    iGCAffinityMask = GetConfigDWORD_DontUse_(CLRConfig::EXTERNAL_GCHeapAffinitizeMask, iGCAffinityMask);
    if (!iGCAffinityMask) iGCAffinityMask = Configuration::GetKnobDWORDValue(W("System.GC.HeapAffinitizeMask"), 0);
    if (!iGCSegmentSize) iGCSegmentSize =  GetConfigDWORD_DontUse_(CLRConfig::UNSUPPORTED_GCSegmentSize, iGCSegmentSize);
    if (!iGCgen0size) iGCgen0size = GetConfigDWORD_DontUse_(CLRConfig::UNSUPPORTED_GCgen0size, iGCgen0size);
#endif //_WIN64

    if (g_IGCHoardVM)
        iGCHoardVM = g_IGCHoardVM;
    else
        iGCHoardVM = CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_GCRetainVM);

    if (!iGCLOHThreshold)
    {
        iGCLOHThreshold = Configuration::GetKnobDWORDValue(W("System.GC.LOHThreshold"), CLRConfig::EXTERNAL_GCLOHThreshold);
        iGCLOHThreshold = max (iGCLOHThreshold, LARGE_OBJECT_SIZE);
    }

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
    iGCNoAffinitize = Configuration::GetKnobBooleanValue(W("System.GC.NoAffinitize"), 
                                                         CLRConfig::EXTERNAL_GCNoAffinitize);
    iGCHeapCount = Configuration::GetKnobDWORDValue(W("System.GC.HeapCount"), CLRConfig::EXTERNAL_GCHeapCount);

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


#if defined(_DEBUG) && defined(_TARGET_AMD64_)
    m_cGenerateLongJumpDispatchStubRatio = GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_GenerateLongJumpDispatchStubRatio,
                                                          static_cast<DWORD>(m_cGenerateLongJumpDispatchStubRatio));
#endif

#if defined(_DEBUG)
    bDiagnosticSuspend = (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DiagnosticSuspend) != 0);
#endif

    dwSleepOnExit = CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_SleepOnExit);

#if defined(FEATURE_TIERED_COMPILATION)
    fTieredCompilation = Configuration::GetKnobBooleanValue(W("System.Runtime.TieredCompilation"), CLRConfig::EXTERNAL_TieredCompilation);

    fTieredCompilation_QuickJit =
        Configuration::GetKnobBooleanValue(
            W("System.Runtime.TieredCompilation.QuickJit"),
            CLRConfig::EXTERNAL_TC_QuickJit);
    fTieredCompilation_QuickJitForLoops =
        Configuration::GetKnobBooleanValue(
            W("System.Runtime.TieredCompilation.QuickJitForLoops"),
            CLRConfig::UNSUPPORTED_TC_QuickJitForLoops);

    fTieredCompilation_CallCounting = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_TC_CallCounting) != 0;

    tieredCompilation_CallCountThreshold = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_TC_CallCountThreshold);
    if (tieredCompilation_CallCountThreshold < 1)
    {
        tieredCompilation_CallCountThreshold = 1;
    }
    else if (tieredCompilation_CallCountThreshold > INT_MAX) // CallCounter uses 'int'
    {
        tieredCompilation_CallCountThreshold = INT_MAX;
    }

    tieredCompilation_CallCountingDelayMs = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_TC_CallCountingDelayMs);

#ifndef FEATURE_PAL
    bool hadSingleProcessorAtStartup = CPUGroupInfo::HadSingleProcessorAtStartup();
#else // !FEATURE_PAL
    bool hadSingleProcessorAtStartup = g_SystemInfo.dwNumberOfProcessors == 1;
#endif // !FEATURE_PAL

    if (hadSingleProcessorAtStartup)
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
            pair = table->Lookup(pKey);
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
            pair = table->Lookup(pKey);
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
            pair = table->Lookup(pKey);
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
