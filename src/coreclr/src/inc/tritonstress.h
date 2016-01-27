// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef TRITON_STRESS_H
#define TRITON_STRESS_H

// Triton Stress
//
// This stress infrastructure is designed to stress the ability of the triton 
// toolchain to handle failure to load certain types/methods/field/assemblies, 
// and jit restrictions. The goal of this system is to inject failures into the 
// mdil binder in a way that is consistent with the failure modes that can 
// exist with customer codebases. It is expected that this sort of stress will 
// be injected in a system that compiles/binds/runs tests. This system is 
// currently debug only.
//
// There are two basic strategies that this system allows.
//
// 1. Modification of the MDIL file to contain partial data.
//    - COMPLUS_TritonStressPartialCTL will cause a generated MDIL file to 
//       fail to contain CTL for some types. 
//    - COMPLUS_TritonStressPartialMDIL will cause a generated MDIL file to 
//       fail to contain MDIL for some method.
// 
// 2. Induced failures within the mdil bind tool that mimic load failures that 
//    could be caused by applcations of various structures. While this approach 
//    is more dissimilar from the actual customer experience, this allows finer 
//    grain control stressing various failure modes within the binder and 
//    should be able to identify issues that are harder to trigger via partial 
//    files, but can actually happen in practice.
//
//    - generating NI's in the presence of type load failures (COMPLUS_TritonStressTypeLoad)
//    - generating NI's in the presence of method load failures (COMPLUS_TritonStressMethodLoad)
//    - generating NI's in the presence of field load failures (COMPLUS_TritonStressFieldLoad)
//    - generating NI's in the presence of assembly load failures (COMPLUS_TritonStressAssemblyLoad)
//
// Internally in the stress infrastructure, a cache of successes/failures is kept,
// and there is logging to indicate which failures being injected. The cache exists 
// so that the stress behavior is able to more closely resemble actual failures, but 
// as this is a seperate scheme from the normal runtime caching, there may be issues 
// if this becomes out of sync with the caches contained in the binder. Failure 
// injection is random, and driven by a random number generator, but the seed of the 
// random number generator can be manually controlled for reproducibility. Logging 
// exists to more easily identify the injected failures.
// 
// Each potential type of failure mode is controlled via the COMPLUS variables 
// described above. 
//
// The format of the values that effect the mdil binder specified in those variables is as follows.
//  TritonStressFlag_None = 0,
//    Location to stress loads from {These flags may be combined}
//  TritonStressFlag_MainModule = 0x1, // - Stress loads where the element to load is in the main module
//  TritonStressFlag_NotMainModule = 0x2, // - Stress loads where the element to load is in modules that are not the main module (but not mscorlib)
//  TritonStressFlag_Mscorlib = 0x4, // - Stress loads where the element to load is in mscorlib
//  TritonStressFlag_AllLocations = 0x7, // - Stress loads from everywhere.
//    Timing when stress will occur {Only one of these may be specified}
//  TritonStressFlag_TimingMask = 0x30, // - Stress timing mask.
//  TritonStressFlag_MDILCompilation = 0x0, // - Stress during MDIL to machine conversion phase
//  TritonStressFlag_CTLLoad = 0x10, // - Stress during main module CTL load phase
//  TritonStressFlag_SavingImage = 0x20, // - Stress during saving image phase
//  TritonStressFlag_EntireRun = 0x30, // - Stress during entire binder run
//    Parameter to control stress
//  TritonStressFlagParam = 0xFFFFFF00 // - Mask of parameter to control stress.
//    The TritonStressFlagParam that of the stress setting controls the likelihood of failure. It is a percentage from 1-100. (Specification of 0 shall be treated as 1.)
//
// The format of the variable which effect the MDIL file generation is only that of a percentage of failures to be induced.
//
// Global control COMPLUS variables
// In addition to these knobs, there are a number of knobs that control the general behavior of triton stress
//  COMPLUS_TritonStressSeed which allows a specific random number seed to be used instead of the random logic used by default.
//  COMPLUS_TritonStressLogFlags which controls the logging performed by the stress system. (The default logging value is 3, which causes the seed and failures to be logged.)
//    TritonStressLog_LogSeed = 0x1,
//    TritonStressLog_RecordFailure = 0x2,
//    TritonStressLog_RecordSuccess = 0x4,
//    TritonStressLog_RepeatFailure = 0x8,


// Triton Stress Mode
enum TritonStressMode
{
    TritonStress_GenerateMDIL,
    TritonStress_GenerateCTL,
    TritonStress_TypeLoad,
    TritonStress_MethodLoad,
    TritonStress_FieldLoad,
    TritonStress_AssemblyLoad,
    TritonStress_Count
};

enum TritonStressPhase
{
    TritonStressPhase_None,
    TritonStressPhase_Startup,
    TritonStressPhase_LoadingInitialCTL,
    TritonStressPhase_MDILCompilation,
    TritonStressPhase_SavingImage,
};

enum TritonStressLogFlags
{
    TritonStressLog_LogSeed       = 0x1,
    TritonStressLog_RecordFailure = 0x2,
    TritonStressLog_RecordSuccess = 0x4,
    TritonStressLog_RepeatFailure = 0x8,
};

enum TritonStressFlags
{
    TritonStressFlag_None = 0,
// Location to stress loads from {These flags may be combined}
    TritonStressFlag_MainModule = 0x1,          // - Stress loads where the element to load is in the main module
    TritonStressFlag_NotMainModule = 0x2,       // - Stress loads where the element to load is in modules that are not the main module (but not mscorlib)
    TritonStressFlag_Mscorlib = 0x4,            // - Stress loads where the element to load is in mscorlib
    TritonStressFlag_AllLocations = 0x7,        // - Stress loads from everywhere.
// Timing when stress will occur {Only one of these may be specified}
    TritonStressFlag_TimingMask = 0x30,         // - Stress timing mask.
    TritonStressFlag_MDILCompilation = 0x0,     // - Stress during MDIL to machine conversion phase
    TritonStressFlag_CTLLoad = 0x10,            // - Stress during main module CTL load phase
    TritonStressFlag_SavingImage = 0x20,        // - Stress during saving image phase
    TritonStressFlag_EntireRun = 0x30,          // - Stress during entire binder run
// Parameter to control stress
    TritonStressFlagParam = 0xFFFFFF00          // - Mask of parameter to control stress.
};

typedef int (*TritonStressLogFunction)(LPCWSTR format, ...);

#if defined(_DEBUG) && defined(MDIL)
void TritonStress(TritonStressMode typeOfStress, 
                  DWORD factorA /* Typically token of stress element */, 
                  LPCWSTR factorB /* Typically assembly identification */,
                  TritonStressFlags stressElementLocation);

void TritonStressStartup(TritonStressLogFunction logger);
void SetTritonStressPhase(TritonStressPhase phase);
#define TRITON_STRESS_NEED_IMPL
#else
#define TritonStressStartup(logger) do { } while(false)
#define TritonStress(typeOfStress, factorA, factorB, stressElementLocation) do { } while(false)
#define SetTritonStressPhase(phase) do { } while(false)
#endif

#endif // TRITON_STRESS_H

#ifdef TRITON_STRESS_IMPL
#ifdef TRITON_STRESS_NEED_IMPL
#include "random.h"

    // Entry in SHash table that maps namespace to list of files
struct TritonStressFactorsToHRESULTMapKey
{
    DWORD                              m_factorA;
    LPCWSTR                           m_factorB;
};

struct TritonStressFactorsToHRESULTMapEntry
{
    TritonStressFactorsToHRESULTMapKey m_key;
    HRESULT                            m_hr;
};
    
// SHash traits for Namespace -> FileNameList hash
class TritonStressFactorsToHRESULTMapTraits : public NoRemoveSHashTraits< DefaultSHashTraits< TritonStressFactorsToHRESULTMapEntry > >
{
public:
    typedef TritonStressFactorsToHRESULTMapKey key_t;
    static const TritonStressFactorsToHRESULTMapEntry Null() 
    { 
        LIMITED_METHOD_CONTRACT;

        TritonStressFactorsToHRESULTMapEntry e; 
        e.m_key.m_factorA = 0; 
        e.m_key.m_factorB = nullptr; 
        e.m_hr = S_OK; 
        return e; 
    }

    static bool IsNull(const TritonStressFactorsToHRESULTMapEntry & e) 
    {
        LIMITED_METHOD_CONTRACT;
        return (e.m_key.m_factorA == 0) && (e.m_key.m_factorB == nullptr);
    }

    static TritonStressFactorsToHRESULTMapKey GetKey(const TritonStressFactorsToHRESULTMapEntry & e) 
    { 
        LIMITED_METHOD_CONTRACT;
        return e.m_key; 
    }

    static count_t Hash(TritonStressFactorsToHRESULTMapKey key) 
    { 
        LIMITED_METHOD_CONTRACT;
        count_t hash = 0;
        hash += key.m_factorA;
        if (key.m_factorB != nullptr)
        {
            hash += HashString(key.m_factorB);
        }
        return hash; 
    }

    static BOOL Equals(TritonStressFactorsToHRESULTMapKey lhs, TritonStressFactorsToHRESULTMapKey rhs)
    {
        LIMITED_METHOD_CONTRACT; 

        if (lhs.m_factorA != rhs.m_factorA)
            return FALSE;

        if ((lhs.m_factorB == nullptr) != (rhs.m_factorB == nullptr))
            return FALSE;

        if (lhs.m_factorB != nullptr)
        {
            return wcscmp(lhs.m_factorB, rhs.m_factorB) == 0;
        }
        return TRUE; 
    }
        
    void OnDestructPerEntryCleanupAction(const TritonStressFactorsToHRESULTMapEntry & e)
    {
        delete [] e.m_key.m_factorB;
    }
    static const bool s_DestructPerEntryCleanupAction = true;
};

TritonStressPhase s_tritonStressPhase = TritonStressPhase_None;
TritonStressFlags s_tritonStressModes[TritonStress_Count];
SHash<TritonStressFactorsToHRESULTMapTraits> *s_tritonStressPreviousResults[TritonStress_Count] = {0};
HRESULT s_tritonStressExceptions[TritonStress_Count] = {0};
DWORD s_tritonStressSeed = 0;
DWORD s_tritonStressValidThreadId = 0;
CLRRandom s_tritonStressRandom;
TritonStressLogFunction s_TritonStressLogger;
BOOL s_fIsAnyTritonStress = FALSE;

TritonStressLogFlags s_tritonStressLogFlags;

void TritonStressStartup(TritonStressLogFunction logger)
{
    STANDARD_VM_CONTRACT;

    DWORD seed;

    LARGE_INTEGER time;
    if (!QueryPerformanceCounter(&time))
        time.QuadPart = GetTickCount();
    seed = (int)time.u.LowPart ^ GetCurrentThreadId() ^ GetCurrentProcessId();

    s_tritonStressValidThreadId = GetCurrentThreadId();
    s_TritonStressLogger = logger;
    s_tritonStressLogFlags = (TritonStressLogFlags)CLRConfig::GetConfigValue(CLRConfig::INTERNAL_TritonStressLogFlags);

    s_tritonStressSeed = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_TritonStressSeed);
    // Config value parameter takes precedence
    if (s_tritonStressSeed == 0)
    {
        s_tritonStressSeed = seed;
    }

    for (int i = 0; i < TritonStress_Count; i++)
        s_tritonStressModes[i] = (TritonStressFlags)0;

    _ASSERTE(s_tritonStressPhase == TritonStressPhase_None);
    s_tritonStressPhase = TritonStressPhase_Startup;
    // Read Stress flags
    DWORD configVal;
    // PartialMDIL stress only includes stress parameter information
    configVal = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_TritonStressPartialMDIL);
    s_tritonStressModes[TritonStress_GenerateMDIL] = TritonStressFlag_None;
    if (configVal != 0)
    {
        s_tritonStressModes[TritonStress_GenerateMDIL] = (TritonStressFlags)((configVal << 8) | TritonStressFlag_AllLocations | TritonStressFlag_EntireRun);
    }
    s_tritonStressExceptions[TritonStress_GenerateMDIL] = COR_E_TYPELOAD;

    configVal = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_TritonStressPartialCTL);
    s_tritonStressModes[TritonStress_GenerateCTL] = TritonStressFlag_None;
    if (configVal != 0)
    {
        s_tritonStressModes[TritonStress_GenerateCTL] = (TritonStressFlags)((configVal << 8) | TritonStressFlag_AllLocations | TritonStressFlag_EntireRun);
    }
    s_tritonStressExceptions[TritonStress_GenerateCTL] = COR_E_TYPELOAD;

    s_tritonStressModes[TritonStress_TypeLoad] =     (TritonStressFlags)CLRConfig::GetConfigValue(CLRConfig::INTERNAL_TritonStressTypeLoad);
    s_tritonStressExceptions[TritonStress_TypeLoad] = COR_E_TYPELOAD;
    s_tritonStressModes[TritonStress_MethodLoad] =   (TritonStressFlags)CLRConfig::GetConfigValue(CLRConfig::INTERNAL_TritonStressMethodLoad);
    s_tritonStressExceptions[TritonStress_MethodLoad] = COR_E_TYPELOAD;
    s_tritonStressModes[TritonStress_FieldLoad] =    (TritonStressFlags)CLRConfig::GetConfigValue(CLRConfig::INTERNAL_TritonStressFieldLoad);
    s_tritonStressExceptions[TritonStress_FieldLoad] = COR_E_TYPELOAD;
    s_tritonStressModes[TritonStress_AssemblyLoad] = (TritonStressFlags)CLRConfig::GetConfigValue(CLRConfig::INTERNAL_TritonStressAssemblyLoad);
    s_tritonStressExceptions[TritonStress_AssemblyLoad] = E_FAIL;

    s_fIsAnyTritonStress = FALSE;
    for (int i = 0; i < TritonStress_Count; i++)
    {
        if (s_tritonStressModes[i] != 0)
        {
            s_fIsAnyTritonStress = TRUE;
            s_tritonStressPreviousResults[i] = new SHash<TritonStressFactorsToHRESULTMapTraits>();
        }
    }

    if (s_fIsAnyTritonStress && s_tritonStressLogFlags & TritonStressLog_LogSeed)
    {
        s_TritonStressLogger(W("TritonStress: Using seed 0x%x\n"), s_tritonStressSeed);
    }

    s_tritonStressRandom.Init(s_tritonStressSeed);
}

void TritonStress(TritonStressMode typeOfStress, 
                  DWORD factorA /* Typically token of stress element */, 
                  LPCWSTR factorB /* Typically assembly identification */,
                  TritonStressFlags stressElementLocation)
{
    STANDARD_VM_CONTRACT;

    BOOL fShouldStress = (stressElementLocation & s_tritonStressModes[typeOfStress]) != 0;
    if (!fShouldStress)
        return;

    // The random number approach is only valid if we only have one thread
    _ASSERTE(s_tritonStressValidThreadId == GetCurrentThreadId());

    // Check to see if we have a previous result.
    TritonStressFactorsToHRESULTMapKey stressKey;
    stressKey.m_factorA = factorA;
    stressKey.m_factorB = factorB;

    const TritonStressFactorsToHRESULTMapEntry *pEntry = s_tritonStressPreviousResults[typeOfStress]->LookupPtr(stressKey);

    if (pEntry != NULL)
    {
        if (FAILED(pEntry->m_hr) && (s_tritonStressLogFlags & TritonStressLog_RepeatFailure))
        {
            s_TritonStressLogger(W("TritonStress: Repeat Failure Type %d FactorA 0x%x, FactorB %s, HRESULT hr 0x%x\n"), typeOfStress, factorA, factorB, pEntry->m_hr);
        }

        // This may throw, or may be S_OK
        IfFailThrow(pEntry->m_hr);
        return;
    }

    fShouldStress = TRUE;

    switch (s_tritonStressModes[typeOfStress] & TritonStressFlag_TimingMask)
    {
    case TritonStressFlag_MDILCompilation:
        fShouldStress = s_tritonStressPhase == TritonStressPhase_MDILCompilation;
        break;

    case TritonStressFlag_CTLLoad:
        fShouldStress = s_tritonStressPhase == TritonStressPhase_LoadingInitialCTL;
        break;

    case TritonStressFlag_SavingImage:
        fShouldStress = s_tritonStressPhase == TritonStressPhase_SavingImage;
        break;

    case TritonStressFlag_EntireRun:
        fShouldStress = TRUE;
        break;

    default:
        fShouldStress = FALSE;
        _ASSERTE(FALSE);
        break;
    }

    HRESULT hrBehavior = S_OK;
    if (fShouldStress)
    {
        // Stress parameter is a percentage from 1-100
        int randomVal = s_tritonStressRandom.Next(100);
        int stressPercent = (s_tritonStressModes[typeOfStress] & TritonStressFlagParam) >> 8;
        if (stressPercent == 0)
            stressPercent = 1;
        if (randomVal < stressPercent)
        {
            hrBehavior = s_tritonStressExceptions[typeOfStress];
        }
    }

    // Store failure so we do the same thing next time.
    TritonStressFactorsToHRESULTMapEntry newEntry = {0};
    newEntry.m_key.m_factorA = factorA;
    if (factorB != nullptr)
    {
        newEntry.m_key.m_factorB = DuplicateStringThrowing(factorB);
    }
    newEntry.m_hr = hrBehavior;
    s_tritonStressPreviousResults[typeOfStress]->Add(newEntry);

    // Throw if appropriate
    if (FAILED(hrBehavior))
    {
        if  (s_tritonStressLogFlags & TritonStressLog_RecordFailure)
            s_TritonStressLogger(W("TritonStress: First Failure Type %d FactorA 0x%x, FactorB %s, HRESULT hr 0x%x\n"), typeOfStress, factorA, factorB, hrBehavior);
    }
    else if (s_tritonStressLogFlags & TritonStressLog_RecordSuccess)
    {
        s_TritonStressLogger(W("TritonStress: Success Type %d FactorA 0x%x, FactorB %s, HRESULT hr 0x%x\n"), typeOfStress, factorA, factorB, hrBehavior);
    }
    IfFailThrow(hrBehavior);
}

void SetTritonStressPhase(TritonStressPhase phase)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(s_tritonStressPhase != TritonStressPhase_None);
    if (s_fIsAnyTritonStress)
        s_TritonStressLogger(W("TritonStress: Phase set to %d\n"), phase);
    s_tritonStressPhase = phase;
}
#endif
#endif
