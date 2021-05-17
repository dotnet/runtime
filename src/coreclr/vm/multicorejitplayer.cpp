// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: MultiCoreJITPlayer.cpp
//

// ===========================================================================
// This file contains the implementation for MultiCore JIT profile playing back
// ===========================================================================
//

#include "common.h"
#include "vars.hpp"
#include "eeconfig.h"
#include "dllimport.h"
#include "comdelegate.h"
#include "dbginterface.h"
#include "stubgen.h"
#include "eventtrace.h"
#include "array.h"
#include "fstream.h"
#include "hash.h"
#include "clrex.h"

#include "appdomain.hpp"

#include "multicorejit.h"
#include "multicorejitimpl.h"

// Options for controlling multicore JIT

unsigned g_MulticoreJitDelay      = 0;          // Delay in StartProfile

bool     g_MulticoreJitEnabled    = true;       // Enable/Disable feature

///////////////////////////////////////////////////////////////////////////////////
//
//            class MulticoreJitCodeStorage
//
///////////////////////////////////////////////////////////////////////////////////


void MulticoreJitCodeStorage::Init()
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;   // called from BaseDomain::Init which is MODE_ANY
    }
    CONTRACTL_END;

    m_nStored   = 0;
    m_nReturned = 0;
    m_crstCodeMap.Init(CrstMulticoreJitHash);
}


// Destructor
MulticoreJitCodeStorage::~MulticoreJitCodeStorage()
{
    LIMITED_METHOD_CONTRACT;

    m_crstCodeMap.Destroy();
}


// Callback from MakeJitWorker to store compiled code, under MethodDesc lock
void MulticoreJitCodeStorage::StoreMethodCode(MethodDesc * pMD, MulticoreJitCodeInfo codeInfo)
{
    STANDARD_VM_CONTRACT;

#ifdef PROFILING_SUPPORTED
    if (CORProfilerTrackJITInfo())
    {
        return;
    }
#endif

    if (!codeInfo.IsNull())
    {
        CrstHolder holder(& m_crstCodeMap);

#ifdef MULTICOREJIT_LOGGING
        if (Logging2On(LF2_MULTICOREJIT, LL_INFO1000))
        {
            MulticoreJitTrace((
                "%p %p %d %d StoredMethodCode",
                pMD,
                codeInfo.GetEntryPoint(),
                (int)codeInfo.WasTier0Jit(),
                (int)codeInfo.JitSwitchedToOptimized()));
        }
#endif

        MulticoreJitCodeInfo existingCodeInfo;

        if (! m_nativeCodeMap.Lookup(pMD, & existingCodeInfo))
        {
            m_nativeCodeMap.Add(pMD, codeInfo);

            m_nStored ++;
        }
    }
}


// Query from MakeJitWorker: Lookup stored JITted methods
MulticoreJitCodeInfo MulticoreJitCodeStorage::QueryAndRemoveMethodCode(MethodDesc * pMethod)
{
    STANDARD_VM_CONTRACT;

    MulticoreJitCodeInfo codeInfo;

    if (m_nStored > m_nReturned) // Quick check before taking lock
    {
        CrstHolder holder(& m_crstCodeMap);

        if (m_nativeCodeMap.Lookup(pMethod, & codeInfo))
        {
            _ASSERTE(!codeInfo.IsNull());

            m_nReturned ++;

            // Remove it to keep storage small (hopefully flat)
            m_nativeCodeMap.Remove(pMethod);

#ifdef MULTICOREJIT_LOGGING
            if (Logging2On(LF2_MULTICOREJIT, LL_INFO1000))
            {
                MulticoreJitTrace((
                    "%p %p %d %d QueryAndRemoveMethodCode",
                    pMethod,
                    codeInfo.GetEntryPoint(),
                    (int)codeInfo.WasTier0Jit(),
                    (int)codeInfo.JitSwitchedToOptimized()));
            }
#endif
        }
    }

    return codeInfo;
}


///////////////////////////////////////////////////////////////////////////////////
//
//               class PlayerModuleInfo
//
///////////////////////////////////////////////////////////////////////////////////

// Per module information kept for mapping to Module object

class PlayerModuleInfo
{
public:

    const ModuleRecord * m_pRecord;
    Module             * m_pModule;
    int                  m_needLevel;
    int                  m_curLevel;
    bool                 m_enableJit;

    PlayerModuleInfo()
    {
        LIMITED_METHOD_CONTRACT;

        m_pRecord     = NULL;
        m_pModule     = NULL;
        m_needLevel   = -1;
        m_curLevel    = -1;
        m_enableJit   = true;
    }

    bool MeetLevel(FileLoadLevel level) const
    {
        LIMITED_METHOD_CONTRACT;

        return (m_pModule != NULL) && (m_curLevel >= (int) level);
    }

    bool IsModuleLoaded() const
    {
        LIMITED_METHOD_CONTRACT;

        return m_pModule != NULL;
    }

    // UpdateNeedLevel called
    bool IsDependency() const
    {
        LIMITED_METHOD_CONTRACT;

        return m_needLevel > -1;
    }

    bool IsLowerLevel() const
    {
        LIMITED_METHOD_CONTRACT;

        return m_curLevel < m_needLevel;
    }

    // If module is loaded, lower then needed level, update its level
    void UpdateCurrentLevel()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        if (m_pModule != NULL)
        {
            if (m_curLevel < m_needLevel)
            {
                m_curLevel = (int) MulticoreJitManager::GetModuleFileLoadLevel(m_pModule);
            }
        }
    }

    bool UpdateNeedLevel(FileLoadLevel level)
    {
        LIMITED_METHOD_CONTRACT;

        if (m_needLevel < (int) level)
        {
            m_needLevel = (int) level;

            return true;
        }

        return false;
    }

    bool MatchWith(ModuleVersion & version, bool & gotVersion, Module * pModule, bool & shortAbort, bool fAppx);

#ifdef MULTICOREJIT_LOGGING
    void Dump(const WCHAR * prefix, int index);
#endif

};


bool PlayerModuleInfo::MatchWith(ModuleVersion & version, bool & gotVersion, Module * pModule, bool & shortAbort, bool fAppx)
{
    STANDARD_VM_CONTRACT;

    if ((m_pModule == NULL) && m_pRecord->MatchWithModule(version, gotVersion, pModule, shortAbort, fAppx))
    {
        m_pModule   = pModule;
        m_curLevel  = (int) MulticoreJitManager::GetModuleFileLoadLevel(pModule);

        if (m_pRecord->jitMethodCount == 0)
        {
            m_enableJit = false;    // No method to JIT for this module, not really needed; just to be correct
        }
        else if (CORDebuggerEnCMode(pModule->GetDebuggerInfoBits()))
        {
            m_enableJit = false;
            MulticoreJitTrace(("Jit disable for module due to EnC"));
            _FireEtwMulticoreJit(W("FILTERMETHOD-EnC"), W(""), 0, 0, 0);
        }

        return true;
    }

    return false;
}


#ifdef MULTICOREJIT_LOGGING

void PlayerModuleInfo::Dump(const WCHAR * prefix, int index)
{
    WRAPPER_NO_CONTRACT;

#ifdef LOGGING
    if (!Logging2On(LF2_MULTICOREJIT, LL_INFO100))
        return;

    DEBUG_ONLY_FUNCTION;
#endif

    StackSString ssBuff;

    ssBuff.Append(prefix);
    ssBuff.AppendPrintf(W("[%2d]: "), index);

    const ModuleVersion & ver = m_pRecord->version;

    ssBuff.AppendPrintf(W(" %d.%d.%05d.%04d.%d level %2d, need %2d"), ver.major, ver.minor, ver.build, ver.revision, ver.versionFlags, m_curLevel, m_needLevel);

    ssBuff.AppendPrintf(W(" pModule: %p "), m_pModule);

    unsigned i;

    for (i = 0; i < m_pRecord->ModuleNameLen(); i ++)
    {
        ssBuff.Append((WCHAR) m_pRecord->GetModuleName()[i]);
    }

    while (i < 32)
    {
        ssBuff.Append(' ');
        i ++;
    }

    MulticoreJitTrace(("%S", ssBuff.GetUnicode()));
}

#endif



///////////////////////////////////////////////////////////////////////////////////
//
//                  MulticoreJitProfilePlayer
//
///////////////////////////////////////////////////////////////////////////////////

const unsigned EmptyToken = 0xFFFFFFFF;

bool ModuleRecord::MatchWithModule(ModuleVersion & modVersion, bool & gotVersion, Module * pModule, bool & shouldAbort, bool fAppx) const
{
    STANDARD_VM_CONTRACT;

    LPCUTF8 pModuleName = pModule->GetSimpleName();
    const char * pName  = GetModuleName();

    size_t len = strlen(pModuleName);

    if ((len == lenModuleName) && (memcmp(pModuleName, pName, lenModuleName) == 0))
    {
        // Ignore version check on play back when running under Appx (also GetModuleVersion is expensive)

        // For Appx, multicore JIT profile is pre-generated by application vendor, installed together with the package.
        // So it may not have exact match with its own assemblies, and assemblies installed on the system.
        if (fAppx)
        {
            return true;
        }

        if (! gotVersion) // Calling expensive GetModuleVersion only when simple name matches
        {
            gotVersion = true;

            if (! modVersion.GetModuleVersion(pModule))
            {
                return false;
            }
        }

        if (version.MatchWith(modVersion))
        {
            // If matching image with different native image flag is detected, mark and abort playing profile back
            if (version.NativeImageFlagDiff(modVersion))
            {
                MulticoreJitTrace(("    Module with different native image flag: %s", pName));

                shouldAbort = true;
            }

            return true;
        }
    }

    return false;
}


MulticoreJitProfilePlayer::MulticoreJitProfilePlayer(ICLRPrivBinder * pBinderContext, LONG nSession, bool fAppxMode)
    : m_stats(::GetAppDomain()->GetMulticoreJitManager().GetStats()), m_appdomainSession(::GetAppDomain()->GetMulticoreJitManager().GetProfileSession())
{
    LIMITED_METHOD_CONTRACT;

    m_pBinderContext     = pBinderContext;
    m_nMySession         = nSession;
    m_moduleCount        = 0;
    m_headerModuleCount  = 0;
    m_pModules           = NULL;
    m_nBlockingCount     = 0;
    m_nMissingModule     = 0;
    m_nLoadedModuleCount = 0;
    m_shouldAbort        = false;
    m_fAppxMode          = fAppxMode;

    m_pThread            = NULL;
    m_pFileBuffer        = NULL;
    m_nFileSize          = 0;

    m_nStartTime         = GetTickCount();
}


MulticoreJitProfilePlayer::~MulticoreJitProfilePlayer()
{
    LIMITED_METHOD_CONTRACT;

    if (m_pModules != NULL)
    {
        delete [] m_pModules;
        m_pModules = NULL;
    }

    if (m_pFileBuffer != NULL)
    {
        delete [] m_pFileBuffer;
    }
}


// static
bool MulticoreJitManager::ModuleHasNoCode(Module * pModule)
{
    LIMITED_METHOD_CONTRACT;

    if (pModule->IsResource())
    {
        return true;
    }

    IMDInternalImport * pImport = pModule->GetMDImport();

    if (pImport != NULL)
    {
        if ((pImport->GetCountWithTokenKind(mdtTypeDef)   == 0) &&
            (pImport->GetCountWithTokenKind(mdtMethodDef) == 0) &&
            (pImport->GetCountWithTokenKind(mdtFieldDef)  == 0)
            )
        {
            return true;
        }
    }

    return false;
}


// We only support default load context, non dynamic module, non domain neutral (needed for dependency)
bool MulticoreJitManager::IsSupportedModule(Module * pModule, bool fMethodJit, bool fAppx)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (pModule == NULL)
    {
        return false;
    }

    PEFile * pFile = pModule->GetFile();

    // dynamic module.
    if (pFile->IsDynamic()) // Ignore dynamic modules
    {
        return false;
    }

    if (pFile->GetPath().IsEmpty()) // Ignore in-memory modules
    {
        return false;
    }


    if (! fMethodJit)
    {
        if (ModuleHasNoCode(pModule))
        {
            return false;
        }
    }

    Assembly * pAssembly = pModule->GetAssembly();


    return true;

}



// static
Module * MulticoreJitManager::DecodeModuleFromIndex(void * pModuleContext, DWORD ix)
{
    STANDARD_VM_CONTRACT

    if (pModuleContext == NULL)
        return NULL;

    MulticoreJitProfilePlayer * pPlayer = (MulticoreJitProfilePlayer *)pModuleContext;
    return pPlayer->GetModuleFromIndex(ix);
}


// ModuleRecord handling: add to m_ModuleList

HRESULT MulticoreJitProfilePlayer::HandleModuleRecord(const ModuleRecord * pMod)
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = S_OK;

    PlayerModuleInfo & info = m_pModules[m_moduleCount];

    info.m_pModule = NULL;
    info.m_pRecord = pMod;

#ifdef MULTICOREJIT_LOGGING
    info.Dump(W("ModuleRecord"), m_moduleCount);
#endif

    m_moduleCount ++;

    return hr;
}

#ifndef DACCESS_COMPILE
MulticoreJitPrepareCodeConfig::MulticoreJitPrepareCodeConfig(MethodDesc* pMethod) :
    PrepareCodeConfig(NativeCodeVersion(pMethod), FALSE, FALSE), m_wasTier0Jit(false)
{
    WRAPPER_NO_CONTRACT;

#ifdef FEATURE_MULTICOREJIT
    SetIsForMulticoreJit();
#endif
}

BOOL MulticoreJitPrepareCodeConfig::SetNativeCode(PCODE pCode, PCODE * ppAlternateCodeToUse)
{
    WRAPPER_NO_CONTRACT;

    MulticoreJitManager & mcJitManager = GetAppDomain()->GetMulticoreJitManager();
    mcJitManager.GetMulticoreJitCodeStorage().StoreMethodCode(GetMethodDesc(), MulticoreJitCodeInfo(pCode, this));
    return TRUE;
}

MulticoreJitCodeInfo::MulticoreJitCodeInfo(PCODE entryPoint, const MulticoreJitPrepareCodeConfig *pConfig)
{
    WRAPPER_NO_CONTRACT;

    m_entryPointAndTierInfo = PCODEToPINSTR(entryPoint);
    _ASSERTE(m_entryPointAndTierInfo != NULL);
    _ASSERTE((m_entryPointAndTierInfo & (TADDR)TierInfo::Mask) == 0);

#ifdef FEATURE_TIERED_COMPILATION
    if (pConfig->WasTier0Jit())
    {
        m_entryPointAndTierInfo |= (TADDR)TierInfo::WasTier0Jit;
    }

    if (pConfig->JitSwitchedToOptimized())
    {
        m_entryPointAndTierInfo |= (TADDR)TierInfo::JitSwitchedToOptimized;
    }
#endif
}
#endif // !DACCESS_COMPILE

void MulticoreJitCodeInfo::VerifyIsNotNull() const
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(!IsNull());
}

// Call JIT to compile a method

bool MulticoreJitProfilePlayer::CompileMethodDesc(Module * pModule, MethodDesc * pMD)
{
    STANDARD_VM_CONTRACT;

    COR_ILMETHOD_DECODER::DecoderStatus status;

    COR_ILMETHOD_DECODER header(pMD->GetILHeader(), pModule->GetMDImport(), & status);

    if (status == COR_ILMETHOD_DECODER::SUCCESS)
    {
        if (m_stats.m_nTryCompiling == 0)
        {
            MulticoreJitTrace(("First call to MakeJitWorker"));
        }

        m_stats.m_nTryCompiling ++;

        // Reset the flag to allow managed code to be called in multicore JIT background thread from this routine
        ThreadStateNCStackHolder holder(-1, Thread::TSNC_CallingManagedCodeDisabled);

        // PrepareCode calls back to MulticoreJitCodeStorage::StoreMethodCode under MethodDesc lock
        MulticoreJitPrepareCodeConfig config(pMD);
        pMD->PrepareCode(&config);

        return true;
    }

    return false;
}

class MulticoreJitPlayerModuleEnumerator : public MulticoreJitModuleEnumerator
{
    MulticoreJitProfilePlayer * m_pPlayer;

    // Implementation of MulticoreJitModuleEnumerator::OnModule
    HRESULT OnModule(Module * pModule)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_PREEMPTIVE;
            CAN_TAKE_LOCK;
        }
        CONTRACTL_END;

        return m_pPlayer->OnModule(pModule);
    }

public:

    MulticoreJitPlayerModuleEnumerator(MulticoreJitProfilePlayer * pPlayer)
    {
        m_pPlayer = pPlayer;
    }
};


HRESULT MulticoreJitProfilePlayer::OnModule(Module * pModule)
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = S_OK;

    // Check if already matched
    for (unsigned i = 0; i < m_moduleCount; i ++)
    {
        if (m_pModules[i].m_pModule == pModule)
        {
            return hr;
        }
    }

    ModuleVersion version; // GetModuleVersion is called on-demand when simple names matches

    bool gotVersion = false;

    // Match with simple name, and then version/flag/guid
    for (unsigned i = 0; i < m_moduleCount; i ++)
    {
        if (m_pModules[i].MatchWith(version, gotVersion, pModule, m_shouldAbort, m_fAppxMode))
        {
            m_nLoadedModuleCount ++;
            return hr;
        }
    }

    return hr;
}


HRESULT MulticoreJitProfilePlayer::UpdateModuleInfo()
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = S_OK;

    MulticoreJitTrace(("UpdateModuleInfo"));

    // Enumerate module if there is a module needed, but not loaded yet
    for (unsigned i = 0; i < m_moduleCount; i ++)
    {
        PlayerModuleInfo & info = m_pModules[i];

        if (info.IsDependency() && ! info.IsModuleLoaded())
        {
            MulticoreJitTrace(("  Enumerate modules for player"));

            MulticoreJitPlayerModuleEnumerator enumerator(this);

            enumerator.EnumerateLoadedModules(GetAppDomain()); // Enumerate modules, hope to find new matches

            break;
        }
    }

    // Update load level, re-calculate blocking count
    m_nBlockingCount = 0;
    m_nMissingModule = 0;

    if (m_shouldAbort)
    {
        hr = E_ABORT;
    }
    else
    {
        // Check for blocking level
        for (unsigned i = 0; i < m_moduleCount; i ++)
        {
            PlayerModuleInfo & info = m_pModules[i];

            if (info.IsLowerLevel())
            {
                if (info.IsModuleLoaded())
                {
                    info.UpdateCurrentLevel();
                }
                else
                {
                    m_nMissingModule ++;
                }

                if (info.IsLowerLevel())
                {
    #ifdef MULTICOREJIT_LOGGING
                    info.Dump(W("    BlockingModule"), i);
    #endif

                    if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_DOTNET_Context, TRACE_LEVEL_VERBOSE, CLR_PRIVATEMULTICOREJIT_KEYWORD))
                    {
                        _FireEtwMulticoreJitA(W("BLOCKINGMODULE"), info.m_pRecord->GetModuleName(), i, info.m_curLevel, info.m_needLevel);
                    }

                    m_nBlockingCount ++;
                }
            }
        }
    }

    MulticoreJitTrace(("Blocking count: %d, missing module: %d, hr=%x", m_nBlockingCount, m_nMissingModule, hr));

    return hr;
}


bool MulticoreJitProfilePlayer::ShouldAbort(bool fast) const
{
    LIMITED_METHOD_CONTRACT;

    if (m_nMySession != m_appdomainSession.GetValue())
    {
        MulticoreJitTrace(("MulticoreJitProfilePlayer::ShouldAbort session over"));
        _FireEtwMulticoreJit(W("ABORTPLAYER"), W("Session over"), 0, 0, 0);
        return true;
    }

    if (fast)
    {
        return false;
    }

    if (GetTickCount() - m_nStartTime > MULTICOREJITLIFE)
    {
        MulticoreJitTrace(("MulticoreJitProfilePlayer::ShouldAbort time over"));

        _FireEtwMulticoreJit(W("ABORTPLAYER"), W("Time out"), 0, 0, 0);

        return true;
    }

    return false;
}

HRESULT MulticoreJitProfilePlayer::HandleModuleInfoRecord(unsigned moduleTo, unsigned level)
{
    STANDARD_VM_CONTRACT;
    
    HRESULT hr = S_OK;

    MulticoreJitTrace(("ModuleDependency(%u) start module load",
        moduleTo));

    if (moduleTo >= m_moduleCount)
    {
        m_stats.m_nMissingModuleSkip++;
        hr = COR_E_BADIMAGEFORMAT;
    }
    else
    {
        PlayerModuleInfo & mod = m_pModules[moduleTo];

        // Load the module if necessary.
        if (!mod.IsModuleLoaded())
        {
            // Update loaded module status.
            AppDomain * pAppDomain = GetAppDomain();
            _ASSERTE(pAppDomain != NULL);

            MulticoreJitPlayerModuleEnumerator moduleEnumerator(this);
            moduleEnumerator.EnumerateLoadedModules(pAppDomain);

            if (!mod.m_pModule)
            {
                // Get the assembly name.
                SString assemblyName;
                assemblyName.SetASCII(mod.m_pRecord->GetAssemblyName(), mod.m_pRecord->AssemblyNameLen());

                // Load the assembly.
                DomainAssembly * pDomainAssembly = LoadAssembly(assemblyName);

                if (pDomainAssembly)
                {
                    // If we successfully loaded the assembly, enumerate the modules in the assembly
                    // and update all modules status.
                    moduleEnumerator.HandleAssembly(pDomainAssembly);

                    if (mod.m_pModule == NULL)
                    {
                        // Unable to load the assembly, so abort.
                        m_stats.m_nMissingModuleSkip++;
                        hr = E_ABORT;
                    }
                }
                else
                {
                    // Unable to load the assembly, so abort.
                    m_stats.m_nMissingModuleSkip++;
                    hr = E_ABORT;
                }
            }
        }

        if ((SUCCEEDED(hr)) && mod.UpdateNeedLevel((FileLoadLevel) level))
        {
            m_nBlockingCount++;
        }
    }

    MulticoreJitTrace(("ModuleDependency(%d) end module load, hr=%x",
        moduleTo,
        hr));

    TraceSummary();

    return hr;
}

DomainAssembly * MulticoreJitProfilePlayer::LoadAssembly(SString & assemblyName)
{
    STANDARD_VM_CONTRACT;

    // Get the assembly name.
    StackScratchBuffer scratch;
    const ANSI* pAnsiAssemblyName = assemblyName.GetANSI(scratch);

    AssemblySpec spec;

    // Initialize the assembly spec.
    HRESULT hr = spec.Init(pAnsiAssemblyName);
    if (FAILED(hr))
    {
        return NULL;
    }

    // Set the binding context to the assembly load context.
    if (m_pBinderContext != NULL)
    {
        spec.SetBindingContext(m_pBinderContext);
    }

    // Bind and load the assembly.
    return spec.LoadDomainAssembly(
        FILE_LOADED,
        FALSE); // Don't throw on FileNotFound.
}

HRESULT MulticoreJitProfilePlayer::HandleNonGenericMethodInfoRecord(unsigned moduleIndex, unsigned token)
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = E_ABORT;

    MulticoreJitTrace(("NonGeneric MethodRecord(%d) start method compilation, %d mod loaded", m_stats.m_nTotalMethod, m_nLoadedModuleCount));

    if (moduleIndex >= m_moduleCount)
    {
        m_stats.m_nMissingModuleSkip++;
        hr = COR_E_BADIMAGEFORMAT;
    }
    else
    {
        PlayerModuleInfo & mod = m_pModules[moduleIndex];
        m_stats.m_nTotalMethod++;

        if (mod.IsModuleLoaded() && mod.m_enableJit)
        {
            Module * pModule = mod.m_pModule;

            // Similar to Module::FindMethod + Module::FindMethodThrowing,
            // except it calls GetMethodDescFromMemberDefOrRefOrSpec with strictMetadataChecks=FALSE to allow generic instantiation
            MethodDesc * pMethod = MemberLoader::GetMethodDescFromMemberDefOrRefOrSpec(pModule, token, NULL, FALSE, FALSE);

            CompileMethodInfoRecord(pModule, pMethod, false);
        }
        else
        {
            m_stats.m_nFilteredMethods++;
        }

        hr = S_OK;
    }

    MulticoreJitTrace(("NonGeneric MethodRecord(%d) end method compilation, filtered %d methods, hr=%x",
        m_stats.m_nTotalMethod,
        m_stats.m_nFilteredMethods,
        hr));

    TraceSummary();

    return hr;
}

HRESULT MulticoreJitProfilePlayer::HandleGenericMethodInfoRecord(unsigned moduleIndex, BYTE * signature, unsigned length)
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = E_ABORT;

    MulticoreJitTrace(("Generic MethodRecord(%d) start method compilation, %d mod loaded", m_stats.m_nTotalMethod, m_nLoadedModuleCount));

    if (moduleIndex >= m_moduleCount)
    {
        m_stats.m_nMissingModuleSkip++;
        hr = COR_E_BADIMAGEFORMAT;
    }
    else
    {
        PlayerModuleInfo & mod = m_pModules[moduleIndex];
        m_stats.m_nTotalMethod++;

        if (mod.IsModuleLoaded() && mod.m_enableJit)
        {
            Module * pModule = mod.m_pModule;

            SigTypeContext typeContext;   // empty type context
            ZapSig::Context zapSigContext(pModule, (void *)this, ZapSig::MulticoreJitTokens);
            MethodDesc * pMethod = NULL;
            EX_TRY
            {
                pMethod = ZapSig::DecodeMethod(pModule, (PCCOR_SIGNATURE)signature, &typeContext, &zapSigContext);
            }
            EX_CATCH
            {
            }
            EX_END_CATCH(SwallowAllExceptions);

            CompileMethodInfoRecord(pModule, pMethod, true);
        }
        else
        {
            m_stats.m_nFilteredMethods++;
        }

        hr = S_OK;
    }

    MulticoreJitTrace(("Generic MethodRecord(%d) end method compilation, filtered %d methods, hr=%x",
        m_stats.m_nTotalMethod,
        m_stats.m_nFilteredMethods,
        hr));

    TraceSummary();

    return hr;
}

void MulticoreJitProfilePlayer::CompileMethodInfoRecord(Module *pModule, MethodDesc *pMethod, bool isGeneric)
{
    STANDARD_VM_CONTRACT;

    if (pMethod != NULL && MulticoreJitManager::IsMethodSupported(pMethod))
    {
        if (!isGeneric)
        {
            // MethodDesc::FindOrCreateTypicalSharedInstantiation is expensive, avoid calling it unless the method or class has generic arguments
            if (pMethod->HasClassOrMethodInstantiation())
            {
                pMethod = pMethod->FindOrCreateTypicalSharedInstantiation();

                if (pMethod == NULL)
                {
                    m_stats.m_nFilteredMethods++;
                    return;
                }

                pModule = pMethod->GetModule_NoLogging();
            }
        }

        if (pMethod->GetNativeCode() == NULL)
        {
            if (CompileMethodDesc(pModule, pMethod))
            {
                return;
            }
        }
        else
        {
            m_stats.m_nHasNativeCode++;
            return;
        }
    }

    m_stats.m_nFilteredMethods++;
}

void MulticoreJitProfilePlayer::TraceSummary()
{
    LIMITED_METHOD_CONTRACT;

    MulticoreJitCodeStorage & curStorage =  GetAppDomain()->GetMulticoreJitManager().GetMulticoreJitCodeStorage();

    unsigned returned = curStorage.GetReturned();

#ifdef MULTICOREJIT_LOGGING

    unsigned compiled =   curStorage.GetStored();

    MulticoreJitTrace(("PlayerSummary: %d total: %d no mod, %d filtered out, %d had code, %d other, %d tried, %d compiled, %d returned, %d%% efficiency, %d mod loaded, %d ms delay(%d)",
        m_stats.m_nTotalMethod,
        m_stats.m_nMissingModuleSkip,
        m_stats.m_nFilteredMethods,
        m_stats.m_nHasNativeCode,
        m_stats.m_nTotalMethod - m_stats.m_nMissingModuleSkip - m_stats.m_nFilteredMethods - m_stats.m_nHasNativeCode - m_stats.m_nTryCompiling,
        m_stats.m_nTryCompiling,
        compiled,
        returned,
        (m_stats.m_nTotalMethod == 0) ? 100 : returned * 100 / m_stats.m_nTotalMethod,
        m_nLoadedModuleCount,
        m_stats.m_nTotalDelay,
        m_stats.m_nDelayCount
        ));

#endif

    _FireEtwMulticoreJit(W("PLAYERSUMMARY"), W(""), m_stats.m_nTryCompiling, m_stats.m_nHasNativeCode, returned);
}


HRESULT MulticoreJitProfilePlayer::ReadCheckFile(const WCHAR * pFileName)
{
    CONTRACTL
    {
        THROWS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    {
        HANDLE hFile = WszCreateFile(pFileName, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);

        if (hFile == INVALID_HANDLE_VALUE)
        {
            return COR_E_FILENOTFOUND;
        }

        HeaderRecord header;

        DWORD cbRead = 0;

        if (! ::ReadFile(hFile, & header, sizeof(header), &cbRead, NULL))
        {
            hr = COR_E_BADIMAGEFORMAT;
        }
        else if (cbRead != sizeof(header))
        {
            hr = COR_E_BADIMAGEFORMAT;
        }
        else
        {
            m_headerModuleCount = header.moduleCount;

            MulticoreJitTrace(("HeaderRecord(version=%d, module=%d, method=%d)", header.version, m_headerModuleCount, header.methodCount));

            if ((header.version != MULTICOREJIT_PROFILE_VERSION) || (header.moduleCount > MAX_MODULES) || (header.methodCount > MAX_METHODS) ||
                (header.recordID != Pack8_24(MULTICOREJIT_HEADER_RECORD_ID, sizeof(HeaderRecord))))
            {
                hr = COR_E_BADIMAGEFORMAT;
            }
            else
            {
                m_pModules = new (nothrow) PlayerModuleInfo[m_headerModuleCount];

                if (m_pModules == NULL)
                {
                    hr = E_OUTOFMEMORY;
                }
            }
        }

        if (SUCCEEDED(hr))
        {
            m_nFileSize = SafeGetFileSize(hFile, 0);

            if (m_nFileSize > sizeof(header))
            {
                m_nFileSize -= sizeof(header);

                m_pFileBuffer = new (nothrow) BYTE[m_nFileSize];

                if (m_pFileBuffer == NULL)
                {
                    hr = E_OUTOFMEMORY;
                }
                else if (::ReadFile(hFile, m_pFileBuffer, m_nFileSize, & cbRead, NULL))
                {
                    if (cbRead != m_nFileSize)
                    {
                        hr = COR_E_BADIMAGEFORMAT;
                    }
                }
                else
                {
                    hr = CLDB_E_FILE_BADREAD;
                }
            }
            else
            {
                hr = COR_E_BADIMAGEFORMAT;
            }
        }

        CloseHandle(hFile);

        _FireEtwMulticoreJit(W("PLAYER"), W("Header"), hr, m_headerModuleCount, header.methodCount);
    }


    return hr;
}


HRESULT MulticoreJitProfilePlayer::PlayProfile()
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = S_OK;

    DWORD start = GetTickCount();

    Thread * pThread = GetThread();

    {
        // 1 marks background thread
        FireEtwThreadCreated((ULONGLONG) pThread, (ULONGLONG) GetAppDomain(), 1, pThread->GetThreadId(), pThread->GetOSThreadId(), GetClrInstanceId());
    }

    const BYTE * pBuffer = m_pFileBuffer;

    unsigned nSize = m_nFileSize;

    MulticoreJitTrace(("PlayProfile %d bytes in (%s)",
        nSize,
        GetAppDomain()->GetFriendlyNameForLogging()));

    while ((SUCCEEDED(hr)) && (nSize > sizeof(unsigned)))
    {
        unsigned data1 = * (const unsigned *) pBuffer;
        unsigned rcdTyp = data1 >> RECORD_TYPE_OFFSET;
        unsigned rcdLen = 0;

        if (rcdTyp == MULTICOREJIT_MODULE_RECORD_ID)
        {
            rcdLen = data1 & 0xFFFFFF;    
        }
        else if (rcdTyp == MULTICOREJIT_MODULEDEPENDENCY_RECORD_ID)
        {
            rcdLen = sizeof(unsigned);
        }
        else if (rcdTyp == MULTICOREJIT_METHOD_RECORD_ID)
        {
            rcdLen = 2 * sizeof(unsigned);
        }
        else if (rcdTyp == MULTICOREJIT_GENERICMETHOD_RECORD_ID)
        {
            if (nSize < sizeof(unsigned) + sizeof(unsigned short))
            {
                hr = COR_E_BADIMAGEFORMAT;
                break;
            }

            unsigned signatureLength = * (const unsigned short *) (((const unsigned *) pBuffer) + 1);
            DWORD dataSize = signatureLength + sizeof(DWORD) + sizeof(unsigned short);
            dataSize = AlignUp(dataSize, sizeof(DWORD));
            rcdLen = dataSize;
        }
        else
        {
            hr = COR_E_BADIMAGEFORMAT;
            break;
        }

        if ((rcdLen > nSize) || (rcdLen & 3)) // Better DWORD align
        {
            hr = COR_E_BADIMAGEFORMAT;
            break;
        }

        if (rcdTyp == MULTICOREJIT_MODULE_RECORD_ID)
        {
            const ModuleRecord * pRec = (const ModuleRecord * ) pBuffer;

            if (((unsigned)(pRec->lenModuleName
                + pRec->lenAssemblyName
                ) > (rcdLen - sizeof(ModuleRecord))) ||
                (m_moduleCount >= m_headerModuleCount))
            {
                hr = COR_E_BADIMAGEFORMAT;
            }
            else
            {
                hr = HandleModuleRecord(pRec);
            }
        }
        else if (rcdTyp == MULTICOREJIT_MODULEDEPENDENCY_RECORD_ID)
        {
            unsigned moduleIndex = data1 & MODULE_MASK;
            unsigned level = (data1 >> MODULE_LEVEL_OFFSET) & (MAX_MODULE_LEVELS - 1);

            hr = HandleModuleInfoRecord(moduleIndex, level);
        }
        else if (rcdTyp == MULTICOREJIT_METHOD_RECORD_ID || rcdTyp == MULTICOREJIT_GENERICMETHOD_RECORD_ID)
        {
            // Find all subsequent methods and jit/load them reversed
            bool isMethod = true;
            bool isGenericMethod = rcdTyp == MULTICOREJIT_GENERICMETHOD_RECORD_ID;
            const BYTE * pCurBuf = pBuffer;
            unsigned curSize = nSize;

            unsigned sizes[MAX_WALKBACK] = {0};
            int count = 0;

            do
            {
                unsigned currcdLen = 0;

                if (isGenericMethod)
                {
                    unsigned cursignatureLength = * (const unsigned short *) (((const unsigned *) pCurBuf) + 1);
                    DWORD dataSize = cursignatureLength + sizeof(DWORD) + sizeof(unsigned short);
                    dataSize = AlignUp(dataSize, sizeof(DWORD));
                    currcdLen = dataSize;
                }
                else
                {
                    currcdLen = 2 * sizeof(unsigned);
                }

                _ASSERTE(currcdLen > 0);

                if (currcdLen > curSize)
                {
                    hr = COR_E_BADIMAGEFORMAT;
                    break;
                }

                sizes[count] = currcdLen;
                count++;

                pCurBuf += currcdLen;
                curSize -= currcdLen;

                if (curSize == 0)
                {
                    break;
                }

                unsigned curdata1 = * (const unsigned *) pCurBuf;
                unsigned currcdTyp = curdata1 >> RECORD_TYPE_OFFSET;
                isGenericMethod = currcdTyp == MULTICOREJIT_GENERICMETHOD_RECORD_ID;
                isMethod = currcdTyp == MULTICOREJIT_METHOD_RECORD_ID || isGenericMethod;
            }
            while (isMethod && count < MAX_WALKBACK);

            if (SUCCEEDED(hr))
            {
                _ASSERTE(count > 0);
                if (count > 1)
                {
                    MulticoreJitTrace(("Jit backwards %d methods",  count));
                }
            }

            int i = count - 1;
            for (; (SUCCEEDED(hr)) && i >= 0; --i)
            {
                pCurBuf -= sizes[i];

                unsigned curdata1 = * (const unsigned *) pCurBuf;
                unsigned currcdTyp = curdata1 >> RECORD_TYPE_OFFSET;
                unsigned curmoduleIndex = curdata1 & MODULE_MASK;
                unsigned curflags = curdata1 & METHOD_FLAGS_MASK;

                if (currcdTyp == MULTICOREJIT_METHOD_RECORD_ID)
                {
                    unsigned token = * (((const unsigned *) pCurBuf) + 1);

                    hr = HandleNonGenericMethodInfoRecord(curmoduleIndex, token);
                }
                else
                {
                    _ASSERTE(currcdTyp == MULTICOREJIT_GENERICMETHOD_RECORD_ID);

                    unsigned cursignatureLength = * (const unsigned short *) (((const unsigned *) pCurBuf) + 1);

                    hr = HandleGenericMethodInfoRecord(curmoduleIndex, (BYTE *) (pCurBuf + sizeof(unsigned) + sizeof(unsigned short)), cursignatureLength);
                }

                if (SUCCEEDED(hr) && ShouldAbort(false))
                {
                    hr = E_ABORT;
                }
            }

            m_stats.m_nWalkBack += (short) count;
            m_stats.m_nFilteredMethods += (short) (i + 1);

            rcdLen = nSize - curSize;
        }
        else
        {
            hr = COR_E_BADIMAGEFORMAT;
        }
        
        pBuffer += rcdLen;
        nSize -= rcdLen;

        if (SUCCEEDED(hr) && ShouldAbort(false))
        {
            hr = E_ABORT;
        }
    }

    start = GetTickCount() - start;

    {
        FireEtwThreadTerminated((ULONGLONG) pThread, (ULONGLONG) GetAppDomain(), GetClrInstanceId());
    }

    MulticoreJitTrace(("Background thread running for %d ms, %d methods, hr=%x", start, m_stats.m_nTotalMethod, hr));

    TraceSummary();

    return hr;
}


HRESULT MulticoreJitProfilePlayer::JITThreadProc(Thread * pThread)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    m_stats.m_hr = S_OK;

    EX_TRY
    {
        {
            // Go into preemptive mode
            GCX_PREEMP();

            m_stats.m_hr = PlayProfile();
        }
    }
    EX_CATCH
    {
        if (SUCCEEDED(m_stats.m_hr))
        {
            m_stats.m_hr = COR_E_EXCEPTION;
        }
    }
    EX_END_CATCH(SwallowAllExceptions);

    return (DWORD) m_stats.m_hr;
}


DWORD WINAPI MulticoreJitProfilePlayer::StaticJITThreadProc(void *args)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        ENTRY_POINT;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    MulticoreJitTrace(("StaticJITThreadProc starting"));

    // Mark the background thread via an ETW event for diagnostics.
    _FireEtwMulticoreJit(W("JITTHREAD"), W(""), 0, 0, 0);

    MulticoreJitProfilePlayer * pPlayer =  (MulticoreJitProfilePlayer *) args;

    if (pPlayer != NULL)
    {
        Thread * pThread = pPlayer->m_pThread;

        if ((pThread != NULL) && pThread->HasStarted())
        {
            // Disable calling managed code in background thread
            ThreadStateNCStackHolder holder(TRUE, Thread::TSNC_CallingManagedCodeDisabled);

            // Run as background thread, so ThreadStore::WaitForOtherThreads will not wait for it
            pThread->SetBackground(TRUE);

            hr = pPlayer->JITThreadProc(pThread);
        }

        // It needs to be deleted after GCX_PREEMP ends
        if (pThread != NULL)
        {
            DestroyThread(pThread);
        }

        // The background thread is reponsible for deleting the MulticoreJitProfilePlayer object once it's started
        // Actually after Thread::StartThread succeeds
        delete pPlayer;
    }

    MulticoreJitTrace(("StaticJITThreadProc endding(%x)", hr));

    END_ENTRYPOINT_NOTHROW;

    return (DWORD) hr;
}


HRESULT MulticoreJitProfilePlayer::ProcessProfile(const WCHAR * pFileName)
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = ReadCheckFile(pFileName);

    if (SUCCEEDED(hr))
    {
        _ASSERTE(m_pThread == NULL);

        m_pThread = SetupUnstartedThread();

        _ASSERTE(m_pThread != NULL);

        if (m_pThread->CreateNewThread(0, StaticJITThreadProc, this))
        {
            int t = (int) m_pThread->StartThread();

            if (t > 0)
            {
                hr = S_OK;
            }
        }
    }

    return hr;
}

Module * MulticoreJitProfilePlayer::GetModuleFromIndex(DWORD ix) const
{
    STANDARD_VM_CONTRACT;

    if (ix >= m_moduleCount)
    {
        return NULL;
    }

    PlayerModuleInfo & mod = m_pModules[ix];
    if (mod.IsModuleLoaded() && mod.m_enableJit)
    {
        return mod.m_pModule;
    }
    return NULL;
}
