// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: CEELOAD.CPP
//

//

// CEELOAD reads in the PE file format using LoadLibrary
// ===========================================================================


#include "common.h"

#include "array.h"
#include "ceeload.h"
#include "hash.h"
#include "vars.hpp"
#include "reflectclasswriter.h"
#include "method.hpp"
#include "stublink.h"
#include "cgensys.h"
#include "excep.h"
#include "dbginterface.h"
#include "dllimport.h"
#include "eeprofinterfaces.h"
#include "encee.h"
#include "jitinterface.h"
#include "eeconfig.h"
#include "dllimportcallback.h"
#include "contractimpl.h"
#include "typehash.h"
#include "instmethhash.h"
#include "virtualcallstub.h"
#include "typestring.h"
#include "stringliteralmap.h"
#include <formattype.h>
#include "fieldmarshaler.h"
#include "sigbuilder.h"
#include "metadataexports.h"
#include "inlinetracking.h"
#include "threads.h"
#include "nativeimage.h"

#ifdef FEATURE_PREJIT
#include "exceptionhandling.h"
#include "corcompile.h"
#include "compile.h"
#include "nibblestream.h"
#include "zapsig.h"
#endif //FEATURE_PREJIT

#ifdef FEATURE_COMINTEROP
#include "runtimecallablewrapper.h"
#include "comcallablewrapper.h"
#endif //FEATURE_COMINTEROP

#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable:4724)
#endif // _MSC_VER

#include "ngenhash.inl"

#ifdef _MSC_VER
#pragma warning(pop)
#endif // _MSC_VER


#include "ecall.h"
#include "../md/compiler/custattr.h"
#include "typekey.h"
#include "peimagelayout.inl"
#include "ildbsymlib.h"

#if defined(PROFILING_SUPPORTED)
#include "profilermetadataemitvalidator.h"
#endif

#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable:4244)
#endif // _MSC_VER

#ifdef TARGET_64BIT
#define COR_VTABLE_PTRSIZED     COR_VTABLE_64BIT
#define COR_VTABLE_NOT_PTRSIZED COR_VTABLE_32BIT
#else // !TARGET_64BIT
#define COR_VTABLE_PTRSIZED     COR_VTABLE_32BIT
#define COR_VTABLE_NOT_PTRSIZED COR_VTABLE_64BIT
#endif // !TARGET_64BIT

#define CEE_FILE_GEN_GROWTH_COLLECTIBLE 2048

#define NGEN_STATICS_ALLCLASSES_WERE_LOADED -1

BOOL Module::HasNativeOrReadyToRunInlineTrackingMap()
{
    LIMITED_METHOD_DAC_CONTRACT;
#ifdef FEATURE_READYTORUN
    if (IsReadyToRun() && GetReadyToRunInfo()->GetInlineTrackingMap() != NULL)
    {
        return TRUE;
    }
#endif
    return (m_pPersistentInlineTrackingMapNGen != NULL);
}

COUNT_T Module::GetNativeOrReadyToRunInliners(PTR_Module inlineeOwnerMod, mdMethodDef inlineeTkn, COUNT_T inlinersSize, MethodInModule inliners[], BOOL *incompleteData)
{
    WRAPPER_NO_CONTRACT;
#ifdef FEATURE_READYTORUN
    if(IsReadyToRun() && GetReadyToRunInfo()->GetInlineTrackingMap() != NULL)
    {
        return GetReadyToRunInfo()->GetInlineTrackingMap()->GetInliners(inlineeOwnerMod, inlineeTkn, inlinersSize, inliners, incompleteData);
    }
#endif
    if(m_pPersistentInlineTrackingMapNGen != NULL)
    {
        return m_pPersistentInlineTrackingMapNGen->GetInliners(inlineeOwnerMod, inlineeTkn, inlinersSize, inliners, incompleteData);
    }
    return 0;
}

#if defined(PROFILING_SUPPORTED) && !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
BOOL Module::HasJitInlineTrackingMap()
{
    LIMITED_METHOD_CONTRACT;

    return m_pJitInlinerTrackingMap != NULL;
}

void Module::AddInlining(MethodDesc *inliner, MethodDesc *inlinee)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(inliner != NULL && inlinee != NULL);
    _ASSERTE(inlinee->GetModule() == this);

    if (m_pJitInlinerTrackingMap != NULL)
    {
        m_pJitInlinerTrackingMap->AddInlining(inliner, inlinee);
    }
}
#endif // defined(PROFILING_SUPPORTED) && !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)

#ifndef DACCESS_COMPILE
// ===========================================================================
// Module
// ===========================================================================

//---------------------------------------------------------------------------------------------------
// This wrapper just invokes the real initialization inside a try/hook.
// szName is not null only for dynamic modules
//---------------------------------------------------------------------------------------------------
void Module::DoInit(AllocMemTracker *pamTracker, LPCWSTR szName)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;

#ifdef PROFILING_SUPPORTED
    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackModuleLoads());
        GCX_COOP();
        (&g_profControlBlock)->ModuleLoadStarted((ModuleID) this);
        END_PROFILER_CALLBACK();
    }
    // Need TRY/HOOK instead of holder so we can get HR of exception thrown for profiler callback
    EX_TRY
#endif
    {
        Initialize(pamTracker, szName);
    }
#ifdef PROFILING_SUPPORTED


    EX_HOOK
    {
        {
            BEGIN_PROFILER_CALLBACK(CORProfilerTrackModuleLoads());
            (&g_profControlBlock)->ModuleLoadFinished((ModuleID) this, GET_EXCEPTION()->GetHR());
            END_PROFILER_CALLBACK();
        }
    }
    EX_END_HOOK;

#endif
}

// Set the given bit on m_dwTransientFlags. Return true if we won the race to set the bit.
BOOL Module::SetTransientFlagInterlocked(DWORD dwFlag)
{
    LIMITED_METHOD_CONTRACT;

    for (;;)
    {
        DWORD dwTransientFlags = m_dwTransientFlags;
        if ((dwTransientFlags & dwFlag) != 0)
            return FALSE;
        if ((DWORD)FastInterlockCompareExchange((LONG*)&m_dwTransientFlags, dwTransientFlags | dwFlag, dwTransientFlags) == dwTransientFlags)
            return TRUE;
    }
}

#if PROFILING_SUPPORTED
void Module::UpdateNewlyAddedTypes()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    DWORD countTypesAfterProfilerUpdate = GetMDImport()->GetCountWithTokenKind(mdtTypeDef);
    DWORD countExportedTypesAfterProfilerUpdate = GetMDImport()->GetCountWithTokenKind(mdtExportedType);
    DWORD countCustomAttributeCount = GetMDImport()->GetCountWithTokenKind(mdtCustomAttribute);

    if (m_dwTypeCount == countTypesAfterProfilerUpdate
        && m_dwExportedTypeCount == countExportedTypesAfterProfilerUpdate
        && m_dwCustomAttributeCount == countCustomAttributeCount)
    {
        // The profiler added no new types, do not create the in memory hashes
        return;
    }

    // R2R pre-computes an export table and tries to avoid populating a class hash at runtime. However the profiler can
    // still add new types on the fly by calling here. If that occurs we fallback to the slower path of creating the
    // in memory hashtable as usual.
    if (!IsResource() && GetAvailableClassHash() == NULL)
    {
        // This call will populate the hash tables with anything that is in metadata already.
        GetClassLoader()->LazyPopulateCaseSensitiveHashTablesDontHaveLock();
    }
    else
    {
        // If the hash tables already exist (either R2R and we've previously populated the ) we need to manually add the types.

        // typeDefs rids 0 and 1 aren't included in the count, thus X typeDefs before means rid X+1 was valid and our incremental addition should start at X+2
        for (DWORD typeDefRid = m_dwTypeCount + 2; typeDefRid < countTypesAfterProfilerUpdate + 2; typeDefRid++)
        {
            GetAssembly()->AddType(this, TokenFromRid(typeDefRid, mdtTypeDef));
        }

        // exportedType rid 0 isn't included in the count, thus X exportedTypes before means rid X was valid and our incremental addition should start at X+1
        for (DWORD exportedTypeDef = m_dwExportedTypeCount + 1; exportedTypeDef < countExportedTypesAfterProfilerUpdate + 1; exportedTypeDef++)
        {
            GetAssembly()->AddExportedType(TokenFromRid(exportedTypeDef, mdtExportedType));
        }

        if ((countCustomAttributeCount != m_dwCustomAttributeCount) && IsReadyToRun())
        {
            // Set of custom attributes has changed. Disable the cuckoo filter from ready to run, and do normal custom attribute parsing
            GetReadyToRunInfo()->DisableCustomAttributeFilter();
        }
    }

    m_dwTypeCount = countTypesAfterProfilerUpdate;
    m_dwExportedTypeCount = countExportedTypesAfterProfilerUpdate;
    m_dwCustomAttributeCount = countCustomAttributeCount;
}

void Module::NotifyProfilerLoadFinished(HRESULT hr)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_ANY;
    }
    CONTRACTL_END;

    // Note that in general we wil reuse shared modules.  So we need to make sure we only notify
    // the profiler once.
    if (SetTransientFlagInterlocked(IS_PROFILER_NOTIFIED))
    {
        // Record how many types are already present
        if (!IsResource())
        {
            m_dwTypeCount = GetMDImport()->GetCountWithTokenKind(mdtTypeDef);
            m_dwExportedTypeCount = GetMDImport()->GetCountWithTokenKind(mdtExportedType);
            m_dwCustomAttributeCount = GetMDImport()->GetCountWithTokenKind(mdtCustomAttribute);
        }

        BOOL profilerCallbackHappened = FALSE;
        // Notify the profiler, this may cause metadata to be updated
        {
            BEGIN_PROFILER_CALLBACK(CORProfilerTrackModuleLoads());
            {
                GCX_PREEMP();
                (&g_profControlBlock)->ModuleLoadFinished((ModuleID) this, hr);

                if (SUCCEEDED(hr))
                {
                    (&g_profControlBlock)->ModuleAttachedToAssembly((ModuleID) this,
                                                                                (AssemblyID)m_pAssembly);
                }

                profilerCallbackHappened = TRUE;
            }
            END_PROFILER_CALLBACK();
        }

        // If there are more types than before, add these new types to the
        // assembly
        if (profilerCallbackHappened && !IsResource())
        {
            UpdateNewlyAddedTypes();
        }

        {
            BEGIN_PROFILER_CALLBACK(CORProfilerTrackAssemblyLoads());
            if (IsManifest())
            {
                GCX_COOP();
                (&g_profControlBlock)->AssemblyLoadFinished((AssemblyID) m_pAssembly, hr);
            }
            END_PROFILER_CALLBACK();
        }
    }
}

#ifndef CROSSGEN_COMPILE
IMetaDataEmit *Module::GetValidatedEmitter()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_pValidatedEmitter.Load() == NULL)
    {
        // In the past profilers could call any API they wanted on the the IMetaDataEmit interface and we didn't
        // verify anything. To ensure we don't break back-compat the verifications are not enabled by default.
        // Right now I have only added verifications for NGEN images, but in the future we might want verifications
        // for all modules.
        IMetaDataEmit* pEmit = NULL;
        if (CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_ProfAPI_ValidateNGENInstrumentation) && HasNativeImage())
        {
            ProfilerMetadataEmitValidator* pValidator = new ProfilerMetadataEmitValidator(GetEmitter());
            pValidator->QueryInterface(IID_IMetaDataEmit, (void**)&pEmit);
        }
        else
        {
            pEmit = GetEmitter();
            pEmit->AddRef();
        }
        // Atomically swap it into the field (release it if we lose the race)
        if (FastInterlockCompareExchangePointer(&m_pValidatedEmitter, pEmit, NULL) != NULL)
        {
            pEmit->Release();
        }
    }
    return m_pValidatedEmitter.Load();
}
#endif // CROSSGEN_COMPILE
#endif // PROFILING_SUPPORTED

void Module::NotifyEtwLoadFinished(HRESULT hr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END

    // we report only successful loads
    if (SUCCEEDED(hr) &&
        ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
                                     TRACE_LEVEL_INFORMATION,
                                     KEYWORDZERO))
    {
        BOOL fSharedModule = !SetTransientFlagInterlocked(IS_ETW_NOTIFIED);
        ETW::LoaderLog::ModuleLoad(this, fSharedModule);
    }
}

// Module initialization occurs in two phases: the constructor phase and the Initialize phase.
//
// The constructor phase initializes just enough so that Destruct() can be safely called.
// It cannot throw or fail.
//
Module::Module(Assembly *pAssembly, mdFile moduleRef, PEFile *file)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        FORBID_FAULT;
    }
    CONTRACTL_END

    PREFIX_ASSUME(pAssembly != NULL);

    m_pAssembly = pAssembly;
    m_moduleRef = moduleRef;
    m_file      = file;
    m_dwTransientFlags = CLASSES_FREED;

    if (!m_file->HasNativeImage())
    {
        // Memory allocated on LoaderHeap is zero-filled. Spot-check it here.
        _ASSERTE(m_pBinder == NULL);
        _ASSERTE(m_symbolFormat == eSymbolFormatNone);
    }

    file->AddRef();
}

void Module::InitializeForProfiling()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(HasNativeOrReadyToRunImage());
    }
    CONTRACTL_END;

    COUNT_T  cbProfileList = 0;

    m_nativeImageProfiling = FALSE;

#ifdef FEATURE_PREJIT
    if (HasNativeImage())
    {
        PEImageLayout * pNativeImage = GetNativeImage();
        CORCOMPILE_VERSION_INFO * pNativeVersionInfo = pNativeImage->GetNativeVersionInfoMaybeNull();
        if ((pNativeVersionInfo != NULL) && (pNativeVersionInfo->wConfigFlags & CORCOMPILE_CONFIG_INSTRUMENTATION))
        {
            m_nativeImageProfiling = GetAssembly()->IsInstrumented();
        }

        // Link the module to the profile data list if available.
        m_methodProfileList = pNativeImage->GetNativeProfileDataList(&cbProfileList);
    }
    else // ReadyToRun image
#endif
    {
#ifdef FEATURE_READYTORUN
        // We already setup the m_methodProfileList in the ReadyToRunInfo constructor
        if (m_methodProfileList != nullptr)
        {
            ReadyToRunInfo * pInfo = GetReadyToRunInfo();
            PEImageLayout *  pImage = pInfo->GetImage();

            // Enable profiling if the ZapBBInstr value says to
            m_nativeImageProfiling = GetAssembly()->IsInstrumented();
        }
#endif
    }
}

#ifdef FEATURE_PREJIT

void Module::InitializeNativeImage(AllocMemTracker* pamTracker)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(HasNativeImage());
    }
    CONTRACTL_END;

    PEImageLayout * pNativeImage = GetNativeImage();

    ExecutionManager::AddNativeImageRange(dac_cast<TADDR>(pNativeImage->GetBase()), pNativeImage->GetVirtualSize(), this);

#ifndef CROSSGEN_COMPILE
    LoadTokenTables();
    LoadHelperTable();
#endif // CROSSGEN_COMPILE

#if defined(HAVE_GCCOVER)
    if (GCStress<cfg_instr_ngen>::IsEnabled())
    {
        // Setting up gc coverage requires the base system classes
        //  to be initialized. So we must defer this for CoreLib.
        if(!IsSystem())
        {
            SetupGcCoverageForNativeImage(this);
        }
    }
#endif // defined(HAVE_GCCOVER)
}
#else // FEATURE_PREJIT
BOOL Module::IsPersistedObject(void *address)
{
    LIMITED_METHOD_CONTRACT;
    return FALSE;
}
#endif // FEATURE_PREJIT

uint32_t Module::GetNativeMetadataAssemblyCount()
{
    if (m_pNativeImage != NULL)
    {
        return m_pNativeImage->GetManifestAssemblyCount();
    }
    else
    {
        return GetNativeAssemblyImport()->GetCountWithTokenKind(mdtAssemblyRef);
    }
}

void Module::SetNativeMetadataAssemblyRefInCache(DWORD rid, PTR_Assembly pAssembly)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_NativeMetadataAssemblyRefMap == NULL)
    {
        uint32_t dwMaxRid = GetNativeMetadataAssemblyCount();
        _ASSERTE(dwMaxRid > 0);

        S_SIZE_T dwAllocSize = S_SIZE_T(sizeof(PTR_Assembly)) * S_SIZE_T(dwMaxRid);

        AllocMemTracker amTracker;
        PTR_Assembly* NativeMetadataAssemblyRefMap = (PTR_Assembly*)amTracker.Track(GetLoaderAllocator()->GetLowFrequencyHeap()->AllocMem(dwAllocSize));

        // Note: Memory allocated on loader heap is zero filled

        if (InterlockedCompareExchangeT<PTR_Assembly*>(&m_NativeMetadataAssemblyRefMap, NativeMetadataAssemblyRefMap, NULL) == NULL)
            amTracker.SuppressRelease();
    }
    _ASSERTE(m_NativeMetadataAssemblyRefMap != NULL);

    _ASSERTE(rid <= GetNativeMetadataAssemblyCount());
    m_NativeMetadataAssemblyRefMap[rid - 1] = pAssembly;
}

// Module initialization occurs in two phases: the constructor phase and the Initialize phase.
//
// The Initialize() phase completes the initialization after the constructor has run.
// It can throw exceptions but whether it throws or succeeds, it must leave the Module
// in a state where Destruct() can be safely called.
//
// szName is only used by dynamic modules, see ReflectionModule::Initialize
//
//
void Module::Initialize(AllocMemTracker *pamTracker, LPCWSTR szName)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        STANDARD_VM_CHECK;
        PRECONDITION(szName == NULL);
    }
    CONTRACTL_END;

    m_pSimpleName = m_file->GetSimpleName();

    m_Crst.Init(CrstModule);
    m_LookupTableCrst.Init(CrstModuleLookupTable, CrstFlags(CRST_UNSAFE_ANYMODE | CRST_DEBUGGER_THREAD));
    m_FixupCrst.Init(CrstModuleFixup, (CrstFlags)(CRST_HOST_BREAKABLE|CRST_REENTRANCY));
    m_InstMethodHashTableCrst.Init(CrstInstMethodHashTable, CRST_REENTRANCY);
    m_ISymUnmanagedReaderCrst.Init(CrstISymUnmanagedReader, CRST_DEBUGGER_THREAD);
    m_DictionaryCrst.Init(CrstDomainLocalBlock);

    if (!m_file->HasNativeImage())
    {
        AllocateMaps();

        if (IsSystem() ||
            (strcmp(m_pSimpleName, "System") == 0) ||
            (strcmp(m_pSimpleName, "System.Core") == 0))
        {
            FastInterlockOr(&m_dwPersistedFlags, LOW_LEVEL_SYSTEM_ASSEMBLY_BY_NAME);
        }
    }

    m_dwTransientFlags &= ~((DWORD)CLASSES_FREED);  // Set flag indicating LookupMaps are now in a consistent and destructable state

#ifdef FEATURE_COLLECTIBLE_TYPES
    if (GetAssembly()->IsCollectible())
    {
        FastInterlockOr(&m_dwPersistedFlags, COLLECTIBLE_MODULE);
    }
#endif // FEATURE_COLLECTIBLE_TYPES

#ifdef FEATURE_READYTORUN
    m_pNativeImage = NULL;
    if (!HasNativeImage() && !IsResource())
    {
        if ((m_pReadyToRunInfo = ReadyToRunInfo::Initialize(this, pamTracker)) != NULL)
        {
            m_pNativeImage = m_pReadyToRunInfo->GetNativeImage();
            if (m_pNativeImage != NULL)
            {
                m_NativeMetadataAssemblyRefMap = m_pNativeImage->GetManifestMetadataAssemblyRefMap();
            }
            else
            {
                // For composite images, manifest metadata gets loaded as part of the native image
                COUNT_T cMeta = 0;
                if (GetFile()->GetOpenedILimage()->GetNativeManifestMetadata(&cMeta) != NULL)
                {
                    // Load the native assembly import
                    GetNativeAssemblyImport(TRUE /* loadAllowed */);
                }
            }
        }
    }
#endif

    // Initialize the instance fields that we need for all non-Resource Modules
    if (!IsResource())
    {
        if (m_pAvailableClasses == NULL && !IsReadyToRun())
        {
            m_pAvailableClasses = EEClassHashTable::Create(this,
                GetAssembly()->IsCollectible() ? AVAILABLE_CLASSES_HASH_BUCKETS_COLLECTIBLE : AVAILABLE_CLASSES_HASH_BUCKETS,
                                                           FALSE /* bCaseInsensitive */, pamTracker);
        }

        if (m_pAvailableParamTypes == NULL)
        {
            m_pAvailableParamTypes = EETypeHashTable::Create(GetLoaderAllocator(), this, PARAMTYPES_HASH_BUCKETS, pamTracker);
        }

        if (m_pInstMethodHashTable == NULL)
        {
            m_pInstMethodHashTable = InstMethodHashTable::Create(GetLoaderAllocator(), this, PARAMMETHODS_HASH_BUCKETS, pamTracker);
        }

        if(m_pMemberRefToDescHashTable == NULL)
        {
            if (IsReflection())
            {
                m_pMemberRefToDescHashTable = MemberRefToDescHashTable::Create(this, MEMBERREF_MAP_INITIAL_SIZE, pamTracker);
            }
            else
            {
				IMDInternalImport * pImport = GetMDImport();

                // Get #MemberRefs and create memberrefToDesc hash table
                m_pMemberRefToDescHashTable = MemberRefToDescHashTable::Create(this, pImport->GetCountWithTokenKind(mdtMemberRef)+1, pamTracker);
            }
        }
    }

    // this will be initialized a bit later.
    m_ModuleID = NULL;
    m_ModuleIndex.m_dwIndex = (SIZE_T)-1;

    // These will be initialized in NotifyProfilerLoadFinished, set them to
    // a safe initial value now.
    m_dwTypeCount = 0;
    m_dwExportedTypeCount = 0;
    m_dwCustomAttributeCount = 0;

    // Prepare statics that are known at module load time
    AllocateStatics(pamTracker);

#ifdef FEATURE_PREJIT
    // Set up native image
    if (HasNativeImage())
    {
        InitializeNativeImage(pamTracker);
    }
#endif // FEATURE_PREJIT

    if (HasNativeOrReadyToRunImage())
    {
        InitializeForProfiling();
    }

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
    if (g_CorCompileVerboseLevel)
        m_pNgenStats = new NgenStats();
#endif

    if (!IsResource() && (m_AssemblyRefByNameTable == NULL))
    {
        Module::CreateAssemblyRefByNameTable(pamTracker);
    }

#if defined(PROFILING_SUPPORTED) && !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
    m_pJitInlinerTrackingMap = NULL;
    if (ReJitManager::IsReJITInlineTrackingEnabled())
    {
        m_pJitInlinerTrackingMap = new JITInlineTrackingMap(GetLoaderAllocator());
    }
#endif // defined (PROFILING_SUPPORTED) &&!defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)

    LOG((LF_CLASSLOADER, LL_INFO10, "Loaded pModule: \"%ws\".\n", GetDebugName()));
}

#endif // DACCESS_COMPILE


#ifdef FEATURE_COMINTEROP

#ifndef DACCESS_COMPILE

// static
GuidToMethodTableHashTable* GuidToMethodTableHashTable::Create(Module* pModule, DWORD cInitialBuckets,
                        AllocMemTracker *pamTracker)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(!FORBIDGC_LOADER_USE_ENABLED());
    }
    CONTRACTL_END;

    LoaderHeap *pHeap = pModule->GetAssembly()->GetLowFrequencyHeap();
    GuidToMethodTableHashTable *pThis = (GuidToMethodTableHashTable*)pamTracker->Track(pHeap->AllocMem((S_SIZE_T)sizeof(GuidToMethodTableHashTable)));

    // The base class get initialized through chaining of constructors. We allocated the hash instance via the
    // loader heap instead of new so use an in-place new to call the constructors now.
    new (pThis) GuidToMethodTableHashTable(pModule, pHeap, cInitialBuckets);

    return pThis;
}

GuidToMethodTableEntry *GuidToMethodTableHashTable::InsertValue(PTR_GUID pGuid, PTR_MethodTable pMT,
                        BOOL bReplaceIfFound, AllocMemTracker *pamTracker)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(!FORBIDGC_LOADER_USE_ENABLED());
    }
    CONTRACTL_END;

    GuidToMethodTableEntry *pEntry = NULL;

    if (bReplaceIfFound)
    {
        pEntry = FindItem(pGuid, NULL);
    }

    if (pEntry != NULL)
    {
        pEntry->m_pMT = pMT;
    }
    else
    {
        pEntry = BaseAllocateEntry(pamTracker);
        pEntry->m_Guid = pGuid;
        pEntry->m_pMT = pMT;

        DWORD hash = Hash(pGuid);
        BaseInsertEntry(hash, pEntry);
    }

    return pEntry;
}

#endif // !DACCESS_COMPILE

PTR_MethodTable GuidToMethodTableHashTable::GetValue(const GUID * pGuid, LookupContext *pContext)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
        PRECONDITION(CheckPointer(pGuid));
    }
    CONTRACTL_END;

    GuidToMethodTableEntry * pEntry = FindItem(pGuid, pContext);
    if (pEntry != NULL)
    {
        return pEntry->m_pMT;
    }

    return NULL;
}

GuidToMethodTableEntry *GuidToMethodTableHashTable::FindItem(const GUID * pGuid, LookupContext *pContext)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
        PRECONDITION(CheckPointer(pGuid));
    }
    CONTRACTL_END;

    // It's legal for the caller not to pass us a LookupContext, but we might need to iterate
    // internally (since we lookup via hash and hashes may collide). So substitute our own
    // private context if one was not provided.
    LookupContext sAltContext;
    if (pContext == NULL)
        pContext = &sAltContext;

    // The base class provides the ability to enumerate all entries with the same hash code.
    // We further check which of these entries actually match the full key.
    PTR_GuidToMethodTableEntry pSearch = BaseFindFirstEntryByHash(Hash(pGuid), pContext);
    while (pSearch)
    {
        if (CompareKeys(pSearch, pGuid))
        {
            return pSearch;
        }

        pSearch = BaseFindNextEntryByHash(pContext);
    }

    return NULL;
}

BOOL GuidToMethodTableHashTable::CompareKeys(PTR_GuidToMethodTableEntry pEntry, const GUID * pGuid)
{
    LIMITED_METHOD_DAC_CONTRACT;
    return *pGuid == *(pEntry->m_Guid);
}

DWORD GuidToMethodTableHashTable::Hash(const GUID * pGuid)
{
    LIMITED_METHOD_DAC_CONTRACT;
    static_assert_no_msg(sizeof(GUID) % sizeof(DWORD) == 0);
    static_assert_no_msg(sizeof(GUID) / sizeof(DWORD) == 4);
    DWORD * pSlice = (DWORD*) pGuid;
    return pSlice[0] ^ pSlice[1] ^ pSlice[2] ^ pSlice[3];
}


BOOL GuidToMethodTableHashTable::FindNext(Iterator *it, GuidToMethodTableEntry **ppEntry)
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (!it->m_fIterating)
    {
        BaseInitIterator(&it->m_sIterator);
        it->m_fIterating = true;
    }

    *ppEntry = it->m_sIterator.Next();
    return *ppEntry ? TRUE : FALSE;
}

DWORD GuidToMethodTableHashTable::GetCount()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return BaseGetElementCount();
}

#if defined(FEATURE_NATIVE_IMAGE_GENERATION) && !defined(DACCESS_COMPILE)

void GuidToMethodTableHashTable::Save(DataImage *pImage, CorProfileData *pProfileData)
{
    WRAPPER_NO_CONTRACT;
    Base_t::BaseSave(pImage, pProfileData);
}

void GuidToMethodTableHashTable::Fixup(DataImage *pImage)
{
    WRAPPER_NO_CONTRACT;
    Base_t::BaseFixup(pImage);
}

bool GuidToMethodTableHashTable::SaveEntry(DataImage *pImage, CorProfileData *pProfileData,
                    GuidToMethodTableEntry *pOldEntry, GuidToMethodTableEntry *pNewEntry,
                    EntryMappingTable *pMap)
{
    LIMITED_METHOD_CONTRACT;
    return false;
}

void GuidToMethodTableHashTable::FixupEntry(DataImage *pImage, GuidToMethodTableEntry *pEntry, void *pFixupBase, DWORD cbFixupOffset)
{
    WRAPPER_NO_CONTRACT;
    pImage->FixupField(pFixupBase, cbFixupOffset + offsetof(GuidToMethodTableEntry, m_pMT), pEntry->m_pMT);
    pImage->FixupField(pFixupBase, cbFixupOffset + offsetof(GuidToMethodTableEntry, m_Guid), pEntry->m_Guid);
}

#endif // FEATURE_NATIVE_IMAGE_GENERATION && !DACCESS_COMPILE

#endif // FEATURE_COMINTEROP

#ifndef DACCESS_COMPILE
MemberRefToDescHashTable* MemberRefToDescHashTable::Create(Module *pModule, DWORD cInitialBuckets, AllocMemTracker *pamTracker)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(!FORBIDGC_LOADER_USE_ENABLED());
    }
    CONTRACTL_END;

    LoaderHeap *pHeap = pModule->GetAssembly()->GetLowFrequencyHeap();
    MemberRefToDescHashTable *pThis = (MemberRefToDescHashTable*)pamTracker->Track(pHeap->AllocMem((S_SIZE_T)sizeof(MemberRefToDescHashTable)));

    // The base class get initialized through chaining of constructors. We allocated the hash instance via the
    // loader heap instead of new so use an in-place new to call the constructors now.
    new (pThis) MemberRefToDescHashTable(pModule, pHeap, cInitialBuckets);

    return pThis;
}

//Inserts FieldRef
MemberRefToDescHashEntry* MemberRefToDescHashTable::Insert(mdMemberRef token , FieldDesc *value)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(!FORBIDGC_LOADER_USE_ENABLED());
    }
    CONTRACTL_END;

    LookupContext sAltContext;

    _ASSERTE((dac_cast<TADDR>(value) & IS_FIELD_MEMBER_REF) == 0);

    MemberRefToDescHashEntry *pEntry = (PTR_MemberRefToDescHashEntry) BaseFindFirstEntryByHash(RidFromToken(token), &sAltContext);
    if (pEntry != NULL)
    {
        // If memberRef is hot token in that case entry for memberref is already persisted in ngen image. So entry for it will already be present in hash table.
        // However its value will be null. We need to set its actual value.
        if(pEntry->m_value == dac_cast<TADDR>(NULL))
        {
            pEntry->m_value = dac_cast<TADDR>(value)|IS_FIELD_MEMBER_REF;
        }

        _ASSERTE(pEntry->m_value == (dac_cast<TADDR>(value)|IS_FIELD_MEMBER_REF));
        return pEntry;
    }

    // For non hot tokens insert new entry in hashtable
    pEntry = BaseAllocateEntry(NULL);
    pEntry->m_value = dac_cast<TADDR>(value)|IS_FIELD_MEMBER_REF;
    BaseInsertEntry(RidFromToken(token), pEntry);

    return pEntry;
}

// Insert MethodRef
MemberRefToDescHashEntry* MemberRefToDescHashTable::Insert(mdMemberRef token , MethodDesc *value)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(!FORBIDGC_LOADER_USE_ENABLED());
    }
    CONTRACTL_END;

    LookupContext sAltContext;

    MemberRefToDescHashEntry *pEntry = (PTR_MemberRefToDescHashEntry) BaseFindFirstEntryByHash(RidFromToken(token), &sAltContext);
    if (pEntry != NULL)
    {
        // If memberRef is hot token in that case entry for memberref is already persisted in ngen image. So entry for it will already be present in hash table.
        // However its value will be null. We need to set its actual value.
        if(pEntry->m_value == dac_cast<TADDR>(NULL))
        {
            pEntry->m_value = dac_cast<TADDR>(value);
        }

        _ASSERTE(pEntry->m_value == dac_cast<TADDR>(value));
        return pEntry;
    }

    // For non hot tokens insert new entry in hashtable
    pEntry = BaseAllocateEntry(NULL);
    pEntry->m_value = dac_cast<TADDR>(value);
    BaseInsertEntry(RidFromToken(token), pEntry);

    return pEntry;
}

#if defined(FEATURE_NATIVE_IMAGE_GENERATION)
void MemberRefToDescHashTable::Save(DataImage *pImage, CorProfileData *pProfileData)
{
    STANDARD_VM_CONTRACT;

    // Mark if the tokens are hot
    if (pProfileData)
    {
        DWORD numInTokenList = pProfileData->GetHotTokens(mdtMemberRef>>24, 1<<RidMap, 1<<RidMap, NULL, 0);

        if (numInTokenList > 0)
        {
            LookupContext sAltContext;

            mdToken *tokenList = (mdToken*)(void*)pImage->GetModule()->GetLoaderAllocator()->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(mdToken)) * S_SIZE_T(numInTokenList));

            pProfileData->GetHotTokens(mdtMemberRef>>24, 1<<RidMap, 1<<RidMap, tokenList, numInTokenList);
            for (DWORD i = 0; i < numInTokenList; i++)
            {
                DWORD rid = RidFromToken(tokenList[i]);
                MemberRefToDescHashEntry *pEntry = (PTR_MemberRefToDescHashEntry) BaseFindFirstEntryByHash(RidFromToken(tokenList[i]), &sAltContext);
                if (pEntry != NULL)
                {
                    _ASSERTE((pEntry->m_value & 0x1) == 0);
                    pEntry->m_value |= 0x1;
                }
            }
        }
    }

    BaseSave(pImage, pProfileData);
}

void MemberRefToDescHashTable::FixupEntry(DataImage *pImage, MemberRefToDescHashEntry *pEntry, void *pFixupBase, DWORD cbFixupOffset)
{
    //As there is no more hard binding initialize MemberRef* to NULL
    pImage->ZeroPointerField(pFixupBase, cbFixupOffset + offsetof(MemberRefToDescHashEntry, m_value));
}

#endif // FEATURE_NATIVE_IMAGE_GENERATION

#endif // !DACCESS_COMPILE

PTR_MemberRef MemberRefToDescHashTable::GetValue(mdMemberRef token, BOOL *pfIsMethod)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    LookupContext sAltContext;

    MemberRefToDescHashEntry *pEntry = (PTR_MemberRefToDescHashEntry) BaseFindFirstEntryByHash(RidFromToken(token), &sAltContext);
    if (pEntry != NULL)
    {
        if(pEntry->m_value & IS_FIELD_MEMBER_REF)
            *pfIsMethod = FALSE;
        else
            *pfIsMethod = TRUE;
        return (PTR_MemberRef)(pEntry->m_value & (~MEMBER_REF_MAP_ALL_FLAGS));
    }

    return NULL;
}


void Module::SetDebuggerInfoBits(DebuggerAssemblyControlFlags newBits)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    _ASSERTE(((newBits << DEBUGGER_INFO_SHIFT_PRIV) &
              ~DEBUGGER_INFO_MASK_PRIV) == 0);

    m_dwTransientFlags &= ~DEBUGGER_INFO_MASK_PRIV;
    m_dwTransientFlags |= (newBits << DEBUGGER_INFO_SHIFT_PRIV);

#ifdef DEBUGGING_SUPPORTED
    if (IsEditAndContinueCapable())
    {
        BOOL setEnC = (newBits & DACF_ENC_ENABLED) != 0 || g_pConfig->ForceEnc() || (g_pConfig->DebugAssembliesModifiable() && CORDisableJITOptimizations(GetDebuggerInfoBits()));
        if (setEnC)
        {
            EnableEditAndContinue();
        }
    }
#endif // DEBUGGING_SUPPORTED

#if defined(DACCESS_COMPILE)
    // Now that we've changed m_dwTransientFlags, update that in the target too.
    // This will fail for read-only target.
    // If this fails, it will throw an exception.
    // @dbgtodo dac write: finalize on plans for how DAC writes to the target.
    HRESULT hrDac;
    hrDac = DacWriteHostInstance(this, true);
    _ASSERTE(SUCCEEDED(hrDac)); // would throw if there was an error.
#endif // DACCESS_COMPILE
}

#ifndef DACCESS_COMPILE
/* static */
Module *Module::Create(Assembly *pAssembly, mdFile moduleRef, PEFile *file, AllocMemTracker *pamTracker)
{
    CONTRACT(Module *)
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pAssembly));
        PRECONDITION(CheckPointer(file));
        PRECONDITION(!IsNilToken(moduleRef) || file->IsAssembly());
        POSTCONDITION(CheckPointer(RETVAL));
        POSTCONDITION(RETVAL->GetFile() == file);
    }
    CONTRACT_END;

    // Hoist CONTRACT into separate routine because of EX incompatibility

    Module *pModule = NULL;

    // Create the module

#ifdef FEATURE_PREJIT

    if (file->HasNativeImage())
    {
        pModule = file->GetLoadedNative()->GetPersistedModuleImage();
        PREFIX_ASSUME(pModule != NULL);
        CONSISTENCY_CHECK_MSG(pModule->m_pAssembly == NULL || !pModule->IsTenured(), // if the module is not tenured it could be our previous attempt
                              "Native image can only be used once per process\n");
        pModule = new ((void*) pModule) Module(pAssembly, moduleRef, file);
        PREFIX_ASSUME(pModule != NULL);
    }

#endif // FEATURE_PREJIT

    if (pModule == NULL)
    {
#ifdef EnC_SUPPORTED
        if (IsEditAndContinueCapable(pAssembly, file))
        {
            // if file is EnCCapable, always create an EnC-module, but EnC won't necessarily be enabled.
            // Debugger enables this by calling SetJITCompilerFlags on LoadModule callback.

            void* pMemory = pamTracker->Track(pAssembly->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(EditAndContinueModule))));
            pModule = new (pMemory) EditAndContinueModule(pAssembly, moduleRef, file);
        }
        else
#endif // EnC_SUPPORTED
        {
            void* pMemory = pamTracker->Track(pAssembly->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(Module))));
            pModule = new (pMemory) Module(pAssembly, moduleRef, file);
        }
    }

    PREFIX_ASSUME(pModule != NULL);
    ModuleHolder pModuleSafe(pModule);
    pModuleSafe->DoInit(pamTracker, NULL);

    RETURN pModuleSafe.Extract();
}

void Module::ApplyMetaData()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    LOG((LF_CLASSLOADER, LL_INFO100, "Module::ApplyNewMetaData %x\n", this));

    HRESULT hr = S_OK;
    ULONG ulCount;

#if PROFILING_SUPPORTED
    if (!IsResource())
    {
        UpdateNewlyAddedTypes();
    }
#endif // PROFILING_SUPPORTED

    // Ensure for TypeRef
    ulCount = GetMDImport()->GetCountWithTokenKind(mdtTypeRef) + 1;
    EnsureTypeRefCanBeStored(TokenFromRid(ulCount, mdtTypeRef));

    // Ensure for AssemblyRef
    ulCount = GetMDImport()->GetCountWithTokenKind(mdtAssemblyRef) + 1;
    EnsureAssemblyRefCanBeStored(TokenFromRid(ulCount, mdtAssemblyRef));

    // Ensure for MethodDef
    ulCount = GetMDImport()->GetCountWithTokenKind(mdtMethodDef) + 1;
    EnsureMethodDefCanBeStored(TokenFromRid(ulCount, mdtMethodDef));
}

//
// Destructor for Module
//

void Module::Destruct()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    LOG((LF_EEMEM, INFO3, "Deleting module %x\n", this));
#ifdef PROFILING_SUPPORTED
    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackModuleLoads());
        if (!IsBeingUnloaded())
        {
            // Profiler is causing some peripheral class loads. Probably this just needs
            // to be turned into a Fault_not_fatal and moved to a specific place inside the profiler.
            EX_TRY
            {
                GCX_PREEMP();
                (&g_profControlBlock)->ModuleUnloadStarted((ModuleID) this);
            }
            EX_CATCH
            {
            }
            EX_END_CATCH(SwallowAllExceptions);
        }
        END_PROFILER_CALLBACK();
    }
#endif // PROFILING_SUPPORTED


    DACNotify::DoModuleUnloadNotification(this);

    // Free classes in the class table
    FreeClassTables();



#ifdef DEBUGGING_SUPPORTED
    if (g_pDebugInterface)
    {
        GCX_PREEMP();
        g_pDebugInterface->DestructModule(this);
    }

#endif // DEBUGGING_SUPPORTED

    ReleaseISymUnmanagedReader();

    // Clean up sig cookies
    VASigCookieBlock    *pVASigCookieBlock = m_pVASigCookieBlock;
    while (pVASigCookieBlock)
    {
        VASigCookieBlock    *pNext = pVASigCookieBlock->m_Next;
        delete pVASigCookieBlock;

        pVASigCookieBlock = pNext;
    }

    // Clean up the IL stub cache
    if (m_pILStubCache != NULL)
    {
        delete m_pILStubCache;
    }



#ifdef PROFILING_SUPPORTED
    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackModuleLoads());
        // Profiler is causing some peripheral class loads. Probably this just needs
        // to be turned into a Fault_not_fatal and moved to a specific place inside the profiler.
        EX_TRY
        {
            GCX_PREEMP();
            (&g_profControlBlock)->ModuleUnloadFinished((ModuleID) this, S_OK);
        }
        EX_CATCH
        {
        }
        EX_END_CATCH(SwallowAllExceptions);
        END_PROFILER_CALLBACK();
    }

    if (m_pValidatedEmitter.Load() != NULL)
    {
        m_pValidatedEmitter->Release();
    }
#endif // PROFILING_SUPPORTED

    //
    // Warning - deleting the zap file will cause the module to be unmapped
    //
    ClearInMemorySymbolStream();

    m_Crst.Destroy();
    m_FixupCrst.Destroy();
    m_LookupTableCrst.Destroy();
    m_InstMethodHashTableCrst.Destroy();
    m_ISymUnmanagedReaderCrst.Destroy();


    if (m_debuggerSpecificData.m_pDynamicILCrst)
    {
        delete m_debuggerSpecificData.m_pDynamicILCrst;
    }

    if (m_debuggerSpecificData.m_pDynamicILBlobTable)
    {
        delete m_debuggerSpecificData.m_pDynamicILBlobTable;
    }

    if (m_debuggerSpecificData.m_pTemporaryILBlobTable)
    {
        delete m_debuggerSpecificData.m_pTemporaryILBlobTable;
    }

    if (m_debuggerSpecificData.m_pILOffsetMappingTable)
    {
        for (ILOffsetMappingTable::Iterator pCurElem = m_debuggerSpecificData.m_pILOffsetMappingTable->Begin(),
                                            pEndElem = m_debuggerSpecificData.m_pILOffsetMappingTable->End();
             pCurElem != pEndElem;
             pCurElem++)
        {
            ILOffsetMappingEntry entry = *pCurElem;
            entry.m_mapping.Clear();
        }
        delete m_debuggerSpecificData.m_pILOffsetMappingTable;
    }

#ifdef FEATURE_PREJIT

    if (HasNativeImage())
    {
        m_file->Release();
    }
    else
#endif // FEATURE_PREJIT
    {
        m_file->Release();
    }

    // If this module was loaded as domain-specific, then
    // we must free its ModuleIndex so that it can be reused
    FreeModuleIndex();
}

#ifdef  FEATURE_PREJIT
void Module::DeleteNativeCodeRanges()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    if (HasNativeImage())
    {
        PEImageLayout * pNativeImage = GetNativeImage();

        ExecutionManager::DeleteRange(dac_cast<TADDR>(pNativeImage->GetBase()));
    }
}
#endif

bool Module::NeedsGlobalMethodTable()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    IMDInternalImport * pImport = GetMDImport();
    if (!IsResource() && pImport->IsValidToken(COR_GLOBAL_PARENT_TOKEN))
    {
        {
            HENUMInternalHolder funcEnum(pImport);
            funcEnum.EnumGlobalFunctionsInit();
            if (pImport->EnumGetCount(&funcEnum) != 0)
                return true;
        }

        {
            HENUMInternalHolder fieldEnum(pImport);
            fieldEnum.EnumGlobalFieldsInit();
            if (pImport->EnumGetCount(&fieldEnum) != 0)
                return true;
        }
    }

    // resource module or no global statics nor global functions
    return false;
}


MethodTable *Module::GetGlobalMethodTable()
{
    CONTRACT (MethodTable *)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(CONTRACT_RETURN NULL;);
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;


    if ((m_dwPersistedFlags & COMPUTED_GLOBAL_CLASS) == 0)
    {
        MethodTable *pMT = NULL;

        if (NeedsGlobalMethodTable())
        {
            pMT = ClassLoader::LoadTypeDefThrowing(this, COR_GLOBAL_PARENT_TOKEN,
                                                   ClassLoader::ThrowIfNotFound,
                                                   ClassLoader::FailIfUninstDefOrRef).AsMethodTable();
        }

        FastInterlockOr(&m_dwPersistedFlags, COMPUTED_GLOBAL_CLASS);
        RETURN pMT;
    }
    else
    {
        RETURN LookupTypeDef(COR_GLOBAL_PARENT_TOKEN).AsMethodTable();
    }
}


#endif // !DACCESS_COMPILE

#ifdef FEATURE_PREJIT

/*static*/
BOOL Module::IsAlwaysSavedInPreferredZapModule(Instantiation classInst,     // the type arguments to the type (if any)
                                               Instantiation methodInst)    // the type arguments to the method (if any)
{
    LIMITED_METHOD_CONTRACT;

    return ClassLoader::IsTypicalSharedInstantiation(classInst) &&
           ClassLoader::IsTypicalSharedInstantiation(methodInst);
}

//this gets called recursively for generics, so do a probe.
PTR_Module Module::ComputePreferredZapModule(Module * pDefinitionModule,
                                             Instantiation classInst,
                                             Instantiation methodInst)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    PTR_Module  ret = NULL;

    ret = Module::ComputePreferredZapModuleHelper( pDefinitionModule,
                                                   classInst,
                                                   methodInst );
    return ret;
}

//
// Is pModule likely a dependency of pOtherModule? Heuristic used by preferred zap module algorithm.
// It can return both false positives and negatives.
//
static bool IsLikelyDependencyOf(Module * pModule, Module * pOtherModule)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        MODE_ANY;
        SUPPORTS_DAC;
        PRECONDITION(CheckPointer(pOtherModule));
    }
    CONTRACTL_END

    // Every module has a dependency with itself
    if (pModule == pOtherModule)
        return true;

    //
    // Explicit check for low level system assemblies is working around Win8P facades introducing extra layer between low level system assemblies
    // (System.dll or System.Core.dll) and the app assemblies. Because of this extra layer, the check below won't see the direct
    // reference between these low level system assemblies and the app assemblies. The prefererred zap module for instantiations of generic
    // collections from these low level system assemblies (like LinkedList<AppType>) should be module of AppType. It would be module of the generic
    // collection without this check.
    //
    if (pModule->IsLowLevelSystemAssemblyByName())
    {
        if (!pOtherModule->IsLowLevelSystemAssemblyByName())
            return true;

        // Every module depends upon CoreLib
        if (pModule->IsSystem())
            return true;

        // CoreLib does not depend upon any other module
        if (pOtherModule->IsSystem())
            return false;
    }
    else
    {
        if (pOtherModule->IsLowLevelSystemAssemblyByName())
            return false;
    }

    // At this point neither pModule or pOtherModule is CoreLib

#ifndef DACCESS_COMPILE
    //
    // We will check to see if the pOtherModule has a reference to pModule
    //

    // If we can match the assembly ref in the ManifestModuleReferencesMap we can early out.
    // This early out kicks in less than half of the time. It hurts performance on average.
    // if (!IsNilToken(pOtherModule->FindAssemblyRef(pModule->GetAssembly())))
    //     return true;

    if (pOtherModule->HasReferenceByName(pModule->GetSimpleName()))
        return true;
#endif // DACCESS_COMPILE

    return false;
}

// Determine the "preferred ngen home" for an instantiated type or method
// * This is the first ngen module that the loader will look in;
// * Also, we only hard bind to a type or method that lives in its preferred module
// The following properties must hold of the preferred module:
// - it must be one of the component type's declaring modules
// - if the type or method is open then the preferred module must be that of one of the type parameters
//   (this ensures that we can always hard bind to open types and methods created during ngen)
// - for always-saved instantiations it must be the declaring module of the generic definition
// Otherwise, we try to pick a module that is likely to reference the type or method
//
/* static */
PTR_Module Module::ComputePreferredZapModuleHelper(
    Module * pDefinitionModule,    // the module that declares the generic type or method
    Instantiation classInst,       // the type arguments to the type (if any)
    Instantiation methodInst)      // the type arguments to the method (if any)
{
    CONTRACT(PTR_Module)
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        MODE_ANY;
        PRECONDITION(CheckPointer(pDefinitionModule, NULL_OK));
        // One of them will be non-null... Note we don't use CheckPointer
        // because that raises a breakpoint in the debugger
        PRECONDITION(pDefinitionModule != NULL || !classInst.IsEmpty() || !methodInst.IsEmpty());
        POSTCONDITION(CheckPointer(RETVAL));
        SUPPORTS_DAC;
    }
    CONTRACT_END

    DWORD totalArgs = classInst.GetNumArgs() + methodInst.GetNumArgs();

    // The open type parameters takes precendence over closed type parameters since
    // we always hardbind to open types.
    for (DWORD i = 0; i < totalArgs; i++)
    {
        TypeHandle thArg = (i < classInst.GetNumArgs()) ? classInst[i] : methodInst[i - classInst.GetNumArgs()];

        // Encoded types are never open
        _ASSERTE(!thArg.IsEncodedFixup());
        Module * pOpenModule = thArg.GetDefiningModuleForOpenType();
        if (pOpenModule != NULL)
            RETURN dac_cast<PTR_Module>(pOpenModule);
    }

    // The initial value of pCurrentPZM is the pDefinitionModule or CoreLib
    Module* pCurrentPZM = (pDefinitionModule != NULL) ? pDefinitionModule : CoreLibBinder::GetModule();
    bool preferredZapModuleBasedOnValueType = false;

    for (DWORD i = 0; i < totalArgs; i++)
    {
        TypeHandle pTypeParam = (i < classInst.GetNumArgs()) ? classInst[i] : methodInst[i - classInst.GetNumArgs()];

        _ASSERTE(pTypeParam != NULL);
        _ASSERTE(!pTypeParam.IsEncodedFixup());

        Module * pParamPZM = GetPreferredZapModuleForTypeHandle(pTypeParam);

        //
        // If pCurrentPZM is not a dependency of pParamPZM
        // then we aren't going to update pCurrentPZM
        //
        if (IsLikelyDependencyOf(pCurrentPZM, pParamPZM))
        {
            // If we have a type parameter that is a value type
            // and we don't yet have a value type based pCurrentPZM
            // then we will select it's module as the new pCurrentPZM.
            //
            if (pTypeParam.IsValueType() && !preferredZapModuleBasedOnValueType)
            {
                pCurrentPZM = pParamPZM;
                preferredZapModuleBasedOnValueType = true;
            }
            else
            {
                // The normal rule is to replace the pCurrentPZM only when
                // both of the following are true:
                //     pCurrentPZM is a dependency of pParamPZM
                // and pParamPZM is not a dependency of pCurrentPZM
                //
                // note that the second condition is alway true when pCurrentPZM is CoreLib
                //
                if (!IsLikelyDependencyOf(pParamPZM, pCurrentPZM))
                {
                    pCurrentPZM = pParamPZM;
                }
            }
        }
    }

    RETURN dac_cast<PTR_Module>(pCurrentPZM);
}

PTR_Module Module::ComputePreferredZapModule(TypeKey *pKey)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    if (pKey->GetKind() == ELEMENT_TYPE_CLASS)
    {
        return Module::ComputePreferredZapModule(pKey->GetModule(),
                                                 pKey->GetInstantiation());
    }
    else if (pKey->GetKind() != ELEMENT_TYPE_FNPTR)
        return Module::GetPreferredZapModuleForTypeHandle(pKey->GetElementType());
    else
        return NULL;

}

/* see code:Module::ComputePreferredZapModuleHelper for more */
/*static*/
PTR_Module Module::GetPreferredZapModuleForMethodTable(MethodTable *pMT)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    PTR_Module pRet=NULL;

    if (pMT->IsArray())
    {
        TypeHandle elemTH = pMT->GetArrayElementTypeHandle();
        pRet= ComputePreferredZapModule(NULL, Instantiation(&elemTH, 1));
    }
    else if (pMT->HasInstantiation() && !pMT->IsGenericTypeDefinition())
    {
        pRet= ComputePreferredZapModule(pMT->GetModule(),
                                        pMT->GetInstantiation());
    }
    else
    {
        // If it is uninstantiated or it is the generic type definition itself
        // then its loader module is simply the module containing its TypeDef
        pRet= pMT->GetModule();
    }
    return pRet;
}


/*static*/
PTR_Module Module::GetPreferredZapModuleForTypeDesc(PTR_TypeDesc pTD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    SUPPORTS_DAC;
    if (pTD->HasTypeParam())
        return GetPreferredZapModuleForTypeHandle(pTD->GetTypeParam());
    else if (pTD->IsGenericVariable())
        return pTD->GetModule();

    _ASSERTE(pTD->GetInternalCorElementType() == ELEMENT_TYPE_FNPTR);
    PTR_FnPtrTypeDesc pFnPtrTD = dac_cast<PTR_FnPtrTypeDesc>(pTD);

    // Result type of function type is used for preferred zap module
    return GetPreferredZapModuleForTypeHandle(pFnPtrTD->GetRetAndArgTypesPointer()[0]);
}

/*static*/
PTR_Module Module::GetPreferredZapModuleForTypeHandle(TypeHandle t)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    SUPPORTS_DAC;
    if (t.IsTypeDesc())
        return GetPreferredZapModuleForTypeDesc(t.AsTypeDesc());
    else
        return GetPreferredZapModuleForMethodTable(t.AsMethodTable());
}

/*static*/
PTR_Module Module::GetPreferredZapModuleForMethodDesc(const MethodDesc *pMD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (pMD->IsTypicalMethodDefinition())
    {
        return PTR_Module(pMD->GetModule());
    }
    else if (pMD->IsGenericMethodDefinition())
    {
        return GetPreferredZapModuleForMethodTable(pMD->GetMethodTable());
    }
    else
    {
        return ComputePreferredZapModule(pMD->GetModule(),
                                         pMD->GetClassInstantiation(),
                                         pMD->GetMethodInstantiation());
    }
}

/* see code:Module::ComputePreferredZapModuleHelper for more */
/*static*/
PTR_Module Module::GetPreferredZapModuleForFieldDesc(FieldDesc * pFD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // The approx MT is sufficient: it's always the one that owns the FieldDesc
    // data structure
    return GetPreferredZapModuleForMethodTable(pFD->GetApproxEnclosingMethodTable());
}
#endif // FEATURE_PREJIT

/*static*/
BOOL Module::IsEditAndContinueCapable(Assembly *pAssembly, PEFile *file)
{
    CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            SUPPORTS_DAC;
        }
    CONTRACTL_END;

    _ASSERTE(pAssembly != NULL && file != NULL);

    // Some modules are never EnC-capable
    return ! (pAssembly->GetDebuggerInfoBits() & DACF_ALLOW_JIT_OPTS ||
              file->IsSystem() ||
              file->IsResource() ||
              file->HasNativeImage() ||
              file->IsDynamic());
}

BOOL Module::IsManifest()
{
    WRAPPER_NO_CONTRACT;
    return dac_cast<TADDR>(GetAssembly()->GetManifestModule()) ==
           dac_cast<TADDR>(this);
}

DomainAssembly* Module::GetDomainAssembly()
{
    LIMITED_METHOD_DAC_CONTRACT;

    return dac_cast<PTR_DomainAssembly>(m_ModuleID->GetDomainFile());
}

DomainFile *Module::GetDomainFile()
{
    LIMITED_METHOD_DAC_CONTRACT;

    return dac_cast<PTR_DomainFile>(m_ModuleID->GetDomainFile());
}

#ifndef DACCESS_COMPILE
#include "staticallocationhelpers.inl"

// Parses metadata and initializes offsets of per-class static blocks.
void Module::BuildStaticsOffsets(AllocMemTracker *pamTracker)
{
    STANDARD_VM_CONTRACT;

    // Trade off here. We want a slot for each type. That way we can get to 2 bits per class and
    // index directly and not need a mapping from ClassID to MethodTable (we will use the RID
    // as the mapping)
    IMDInternalImport *pImport = GetMDImport();

    DWORD * pRegularStaticOffsets = NULL;
    DWORD * pThreadStaticOffsets = NULL;

    // Get the number of types/classes defined in this module. Add 1 to count the module itself
    DWORD dwNumTypes   = pImport->GetCountWithTokenKind(mdtTypeDef) + 1; // +1 for module type

    // [0] covers regular statics, [1] covers thread statics
    DWORD      dwGCHandles[2] = { 0, 0 };

    // Organization in memory of the static block
    //
    //
    //                                       |  GC Statics             |
    //                                                  |
    //                                                  |
    //    | Class Data (one byte per class)  |   pointer to gc statics | primitive type statics |
    //
    //
#ifndef CROSSBITNESS_COMPILE
    // The assertions must hold in every non-crossbitness scenario
    _ASSERTE(OFFSETOF__DomainLocalModule__m_pDataBlob_ == DomainLocalModule::OffsetOfDataBlob());
    _ASSERTE(OFFSETOF__ThreadLocalModule__m_pDataBlob  == ThreadLocalModule::OffsetOfDataBlob());
#endif

    DWORD      dwNonGCBytes[2] = {
        DomainLocalModule::OffsetOfDataBlob() + (DWORD)(sizeof(BYTE)*dwNumTypes),
        ThreadLocalModule::OffsetOfDataBlob() + (DWORD)(sizeof(BYTE)*dwNumTypes)
    };

    HENUMInternalHolder hTypeEnum(pImport);
    hTypeEnum.EnumAllInit(mdtTypeDef);

    mdTypeDef type;
    // Parse each type of the class
    while (pImport->EnumNext(&hTypeEnum, &type))
    {
        // Set offset for this type
        DWORD dwIndex = RidFromToken(type) - 1;

        // [0] covers regular statics, [1] covers thread statics
        DWORD dwAlignment[2] = { 1, 1 };
        DWORD dwClassNonGCBytes[2] = { 0, 0 };
        DWORD dwClassGCHandles[2]  = { 0, 0 };

        // need to check if the type is generic and if so exclude it from iteration as we don't know the size
        HENUMInternalHolder hGenericEnum(pImport);
        hGenericEnum.EnumInit(mdtGenericParam, type);
        ULONG cGenericParams = pImport->EnumGetCount(&hGenericEnum);
        if (cGenericParams == 0)
        {
            HENUMInternalHolder hFieldEnum(pImport);
            hFieldEnum.EnumInit(mdtFieldDef, type);

            mdFieldDef field;
            // Parse each field of the type
            while (pImport->EnumNext(&hFieldEnum, &field))
            {
                BOOL fSkip = FALSE;

                CorElementType ElementType = ELEMENT_TYPE_END;
                mdToken tkValueTypeToken = 0;
                int kk; // Use one set of variables for regular statics, and the other set for thread statics

                fSkip = GetStaticFieldElementTypeForFieldDef(this, pImport, field, &ElementType, &tkValueTypeToken, &kk);
                if (fSkip)
                    continue;

                // We account for "regular statics" and "thread statics" separately.
                // Currently we are lumping RVA into "regular statics",
                // but we probably shouldn't.
                switch (ElementType)
                {
                    case ELEMENT_TYPE_I1:
                    case ELEMENT_TYPE_U1:
                    case ELEMENT_TYPE_BOOLEAN:
                        dwClassNonGCBytes[kk] += 1;
                        break;
                    case ELEMENT_TYPE_I2:
                    case ELEMENT_TYPE_U2:
                    case ELEMENT_TYPE_CHAR:
                        dwAlignment[kk] =  max(2, dwAlignment[kk]);
                        dwClassNonGCBytes[kk] += 2;
                        break;
                    case ELEMENT_TYPE_I4:
                    case ELEMENT_TYPE_U4:
                    case ELEMENT_TYPE_R4:
                        dwAlignment[kk] =  max(4, dwAlignment[kk]);
                        dwClassNonGCBytes[kk] += 4;
                        break;
                    case ELEMENT_TYPE_FNPTR:
                    case ELEMENT_TYPE_PTR:
                    case ELEMENT_TYPE_I:
                    case ELEMENT_TYPE_U:
                        dwAlignment[kk] =  max((1 << LOG2_PTRSIZE), dwAlignment[kk]);
                        dwClassNonGCBytes[kk] += (1 << LOG2_PTRSIZE);
                        break;
                    case ELEMENT_TYPE_I8:
                    case ELEMENT_TYPE_U8:
                    case ELEMENT_TYPE_R8:
                        dwAlignment[kk] =  max(8, dwAlignment[kk]);
                        dwClassNonGCBytes[kk] += 8;
                        break;
                    case ELEMENT_TYPE_VAR:
                    case ELEMENT_TYPE_MVAR:
                    case ELEMENT_TYPE_STRING:
                    case ELEMENT_TYPE_SZARRAY:
                    case ELEMENT_TYPE_ARRAY:
                    case ELEMENT_TYPE_CLASS:
                    case ELEMENT_TYPE_OBJECT:
                        dwClassGCHandles[kk]  += 1;
                        break;
                    case ELEMENT_TYPE_VALUETYPE:
                        // Statics for valuetypes where the valuetype is defined in this module are handled here. Other valuetype statics utilize the pessimistic model below.
                        dwClassGCHandles[kk]  += 1;
                        break;
                    case ELEMENT_TYPE_END:
                    default:
                        // The actual element type was ELEMENT_TYPE_VALUETYPE, but the as we don't want to load additional assemblies
                        // to determine these static offsets, we've fallen back to a pessimistic model.
                        if (tkValueTypeToken != 0)
                        {
                            // We'll have to be pessimistic here
                            dwClassNonGCBytes[kk] += MAX_PRIMITIVE_FIELD_SIZE;
                            dwAlignment[kk] = max(MAX_PRIMITIVE_FIELD_SIZE, dwAlignment[kk]);

                            dwClassGCHandles[kk]  += 1;
                            break;
                        }
                        else
                        {
                            // field has an unexpected type
                            ThrowHR(VER_E_FIELD_SIG);
                            break;
                        }
                }
            }

            if (pRegularStaticOffsets == NULL && (dwClassGCHandles[0] != 0 || dwClassNonGCBytes[0] != 0))
            {
                // Lazily allocate table for offsets. We need offsets for GC and non GC areas. We add +1 to use as a sentinel.
                pRegularStaticOffsets = (PTR_DWORD)pamTracker->Track(
                    GetLoaderAllocator()->GetHighFrequencyHeap()->AllocMem(
                        (S_SIZE_T(2 * sizeof(DWORD))*(S_SIZE_T(dwNumTypes)+S_SIZE_T(1)))));

                for (DWORD i = 0; i < dwIndex; i++) {
                    pRegularStaticOffsets[i * 2    ] = dwGCHandles[0]*TARGET_POINTER_SIZE;
                    pRegularStaticOffsets[i * 2 + 1] = dwNonGCBytes[0];
                }
            }

            if (pThreadStaticOffsets == NULL && (dwClassGCHandles[1] != 0 || dwClassNonGCBytes[1] != 0))
            {
                // Lazily allocate table for offsets. We need offsets for GC and non GC areas. We add +1 to use as a sentinel.
                pThreadStaticOffsets = (PTR_DWORD)pamTracker->Track(
                    GetLoaderAllocator()->GetHighFrequencyHeap()->AllocMem(
                        (S_SIZE_T(2 * sizeof(DWORD))*(S_SIZE_T(dwNumTypes)+S_SIZE_T(1)))));

                for (DWORD i = 0; i < dwIndex; i++) {
                    pThreadStaticOffsets[i * 2    ] = dwGCHandles[1]*TARGET_POINTER_SIZE;
                    pThreadStaticOffsets[i * 2 + 1] = dwNonGCBytes[1];
                }
            }
        }

        if (pRegularStaticOffsets != NULL)
        {
            // Align the offset of non gc statics
            dwNonGCBytes[0] = (DWORD) ALIGN_UP(dwNonGCBytes[0], dwAlignment[0]);

            // Save current offsets
            pRegularStaticOffsets[dwIndex*2]     = dwGCHandles[0]*TARGET_POINTER_SIZE;
            pRegularStaticOffsets[dwIndex*2 + 1] = dwNonGCBytes[0];

            // Increment for next class
            dwGCHandles[0]  += dwClassGCHandles[0];
            dwNonGCBytes[0] += dwClassNonGCBytes[0];
        }

        if (pThreadStaticOffsets != NULL)
        {
            // Align the offset of non gc statics
            dwNonGCBytes[1] = (DWORD) ALIGN_UP(dwNonGCBytes[1], dwAlignment[1]);

            // Save current offsets
            pThreadStaticOffsets[dwIndex*2]     = dwGCHandles[1]*TARGET_POINTER_SIZE;
            pThreadStaticOffsets[dwIndex*2 + 1] = dwNonGCBytes[1];

            // Increment for next class
            dwGCHandles[1]  += dwClassGCHandles[1];
            dwNonGCBytes[1] += dwClassNonGCBytes[1];
        }
    }

    m_maxTypeRidStaticsAllocated = dwNumTypes;

    if (pRegularStaticOffsets != NULL)
    {
        pRegularStaticOffsets[dwNumTypes*2]     = dwGCHandles[0]*TARGET_POINTER_SIZE;
        pRegularStaticOffsets[dwNumTypes*2 + 1] = dwNonGCBytes[0];
    }

    if (pThreadStaticOffsets != NULL)
    {
        pThreadStaticOffsets[dwNumTypes*2]     = dwGCHandles[1]*TARGET_POINTER_SIZE;
        pThreadStaticOffsets[dwNumTypes*2 + 1] = dwNonGCBytes[1];
    }

    m_pRegularStaticOffsets = pRegularStaticOffsets;
    m_pThreadStaticOffsets = pThreadStaticOffsets;

    m_dwMaxGCRegularStaticHandles = dwGCHandles[0];
    m_dwMaxGCThreadStaticHandles = dwGCHandles[1];

    m_dwRegularStaticsBlockSize = dwNonGCBytes[0];
    m_dwThreadStaticsBlockSize = dwNonGCBytes[1];
}

void  Module::GetOffsetsForRegularStaticData(
                    mdToken cl,
                    BOOL bDynamic, DWORD dwGCStaticHandles,
                    DWORD dwNonGCStaticBytes,
                    DWORD * pOutStaticHandleOffset,
                    DWORD * pOutNonGCStaticOffset)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END

    *pOutStaticHandleOffset = 0;
    *pOutNonGCStaticOffset  = 0;

    if (!dwGCStaticHandles && !dwNonGCStaticBytes)
    {
        return;
    }

#ifndef CROSSBITNESS_COMPILE
    _ASSERTE(OFFSETOF__DomainLocalModule__NormalDynamicEntry__m_pDataBlob == DomainLocalModule::DynamicEntry::GetOffsetOfDataBlob());
#endif
    // Statics for instantiated types are allocated dynamically per-instantiation
    if (bDynamic)
    {
        // Non GC statics are embedded in the Dynamic Entry.
        *pOutNonGCStaticOffset  = OFFSETOF__DomainLocalModule__NormalDynamicEntry__m_pDataBlob;
        return;
    }

    if (m_pRegularStaticOffsets == NULL)
    {
        THROW_BAD_FORMAT(BFA_METADATA_CORRUPT, this);
    }
    _ASSERTE(m_pRegularStaticOffsets != (PTR_DWORD) NGEN_STATICS_ALLCLASSES_WERE_LOADED);

    // We allocate in the big blob.
    DWORD index = RidFromToken(cl) - 1;

    *pOutStaticHandleOffset = m_pRegularStaticOffsets[index*2];

    *pOutNonGCStaticOffset  = m_pRegularStaticOffsets[index*2 + 1];
#ifdef CROSSBITNESS_COMPILE
    *pOutNonGCStaticOffset += OFFSETOF__DomainLocalModule__m_pDataBlob_ - DomainLocalModule::OffsetOfDataBlob();
#endif

    // Check we didnt go out of what we predicted we would need for the class
    if (*pOutStaticHandleOffset + TARGET_POINTER_SIZE*dwGCStaticHandles >
                m_pRegularStaticOffsets[(index+1)*2] ||
        *pOutNonGCStaticOffset + dwNonGCStaticBytes >
                m_pRegularStaticOffsets[(index+1)*2 + 1])
    {   // It's most likely that this is due to bad metadata, thus the exception. However, the
        // previous comments for this bit of code mentioned that this could be a corner case bug
        // with static field size estimation, though this is entirely unlikely since the code has
        // been this way for at least two releases.
        THROW_BAD_FORMAT(BFA_METADATA_CORRUPT, this);
    }
}


void  Module::GetOffsetsForThreadStaticData(
                    mdToken cl,
                    BOOL bDynamic, DWORD dwGCStaticHandles,
                    DWORD dwNonGCStaticBytes,
                    DWORD * pOutStaticHandleOffset,
                    DWORD * pOutNonGCStaticOffset)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END

    *pOutStaticHandleOffset = 0;
    *pOutNonGCStaticOffset  = 0;

    if (!dwGCStaticHandles && !dwNonGCStaticBytes)
    {
        return;
    }

#ifndef CROSSBITNESS_COMPILE
    _ASSERTE(OFFSETOF__ThreadLocalModule__DynamicEntry__m_pDataBlob == ThreadLocalModule::DynamicEntry::GetOffsetOfDataBlob());
#endif
    // Statics for instantiated types are allocated dynamically per-instantiation
    if (bDynamic)
    {
        // Non GC thread statics are embedded in the Dynamic Entry.
        *pOutNonGCStaticOffset  = OFFSETOF__ThreadLocalModule__DynamicEntry__m_pDataBlob;
        return;
    }

    if (m_pThreadStaticOffsets == NULL)
    {
        THROW_BAD_FORMAT(BFA_METADATA_CORRUPT, this);
    }
    _ASSERTE(m_pThreadStaticOffsets != (PTR_DWORD) NGEN_STATICS_ALLCLASSES_WERE_LOADED);

    // We allocate in the big blob.
    DWORD index = RidFromToken(cl) - 1;

    *pOutStaticHandleOffset = m_pThreadStaticOffsets[index*2];

    *pOutNonGCStaticOffset  = m_pThreadStaticOffsets[index*2 + 1];
#ifdef CROSSBITNESS_COMPILE
    *pOutNonGCStaticOffset += OFFSETOF__ThreadLocalModule__m_pDataBlob - ThreadLocalModule::GetOffsetOfDataBlob();
#endif

    // Check we didnt go out of what we predicted we would need for the class
    if (*pOutStaticHandleOffset + TARGET_POINTER_SIZE*dwGCStaticHandles >
                m_pThreadStaticOffsets[(index+1)*2] ||
        *pOutNonGCStaticOffset + dwNonGCStaticBytes >
                m_pThreadStaticOffsets[(index+1)*2 + 1])
    {
        // It's most likely that this is due to bad metadata, thus the exception. However, the
        // previous comments for this bit of code mentioned that this could be a corner case bug
        // with static field size estimation, though this is entirely unlikely since the code has
        // been this way for at least two releases.
        THROW_BAD_FORMAT(BFA_METADATA_CORRUPT, this);
    }
}


// initialize Crst controlling the Dynamic IL hashtable
void Module::InitializeDynamicILCrst()
{
    Crst * pCrst = new Crst(CrstDynamicIL, CrstFlags(CRST_UNSAFE_ANYMODE | CRST_DEBUGGER_THREAD));
    if (InterlockedCompareExchangeT(
            &m_debuggerSpecificData.m_pDynamicILCrst, pCrst, NULL) != NULL)
    {
        delete pCrst;
    }
}

// Add a (token, address) pair to the table of IL blobs for reflection/dynamics
// Arguments:
//     Input:
//         token        method token
//         blobAddress  address of the start of the IL blob address, including the header
//         fTemporaryOverride
//                      is this a permanent override that should go in the
//                      DynamicILBlobTable, or a temporary one?
//     Output: not explicit, but if the pair was not already in the table it will be added.
//             Does not add duplicate tokens to the table.

void Module::SetDynamicIL(mdToken token, TADDR blobAddress, BOOL fTemporaryOverride)
{
    DynamicILBlobEntry entry = {mdToken(token), TADDR(blobAddress)};

    // Lazily allocate a Crst to serialize update access to the info structure.
    // Carefully synchronize to ensure we don't leak a Crst in race conditions.
    if (m_debuggerSpecificData.m_pDynamicILCrst == NULL)
    {
        InitializeDynamicILCrst();
    }

    CrstHolder ch(m_debuggerSpecificData.m_pDynamicILCrst);

    // Figure out which table to fill in
    PTR_DynamicILBlobTable &table(fTemporaryOverride ? m_debuggerSpecificData.m_pTemporaryILBlobTable
                                                     : m_debuggerSpecificData.m_pDynamicILBlobTable);

    // Lazily allocate the hash table.
    if (table == NULL)
    {
        table = PTR_DynamicILBlobTable(new DynamicILBlobTable);
    }
    table->AddOrReplace(entry);
}

#endif // !DACCESS_COMPILE

// Get the stored address of the IL blob for reflection/dynamics
// Arguments:
//     Input:
//         token        method token
//         fAllowTemporary also check the temporary overrides
// Return Value: starting (target) address of the IL blob corresponding to the input token

TADDR Module::GetDynamicIL(mdToken token, BOOL fAllowTemporary)
{
    SUPPORTS_DAC;

#ifndef DACCESS_COMPILE
    // The Crst to serialize update access to the info structure is lazily allocated.
    // If it hasn't been allocated yet, then we don't have any IL blobs (temporary or otherwise)
    if (m_debuggerSpecificData.m_pDynamicILCrst == NULL)
    {
        return TADDR(NULL);
    }

    CrstHolder ch(m_debuggerSpecificData.m_pDynamicILCrst);
#endif

    // Both hash tables are lazily allocated, so if they're NULL
    // then we have no IL blobs

    if (fAllowTemporary && m_debuggerSpecificData.m_pTemporaryILBlobTable != NULL)
    {
        DynamicILBlobEntry entry = m_debuggerSpecificData.m_pTemporaryILBlobTable->Lookup(token);

        // Only return a value if the lookup succeeded
        if (!DynamicILBlobTraits::IsNull(entry))
        {
            return entry.m_il;
        }
    }

    if (m_debuggerSpecificData.m_pDynamicILBlobTable == NULL)
    {
        return TADDR(NULL);
    }

    DynamicILBlobEntry entry = m_debuggerSpecificData.m_pDynamicILBlobTable->Lookup(token);
    // If the lookup fails, it returns the 'NULL' entry
    // The 'NULL' entry has m_il set to NULL, so either way we're safe
    return entry.m_il;
}

#if !defined(DACCESS_COMPILE)
//---------------------------------------------------------------------------------------
//
// Add instrumented IL offset mapping for the specified method.
//
// Arguments:
//    token   - the MethodDef token of the method in question
//    mapping - the mapping information between original IL offsets and instrumented IL offsets
//
// Notes:
//    * Once added, the mapping stays valid until the Module containing the method is destructed.
//    * The profiler may potentially update the mapping more than once.
//

void Module::SetInstrumentedILOffsetMapping(mdMethodDef token, InstrumentedILOffsetMapping mapping)
{
    ILOffsetMappingEntry entry(token, mapping);

    // Lazily allocate a Crst to serialize update access to the hash table.
    // Carefully synchronize to ensure we don't leak a Crst in race conditions.
    if (m_debuggerSpecificData.m_pDynamicILCrst == NULL)
    {
        InitializeDynamicILCrst();
    }

    CrstHolder ch(m_debuggerSpecificData.m_pDynamicILCrst);

    // Lazily allocate the hash table.
    if (m_debuggerSpecificData.m_pILOffsetMappingTable == NULL)
    {
        m_debuggerSpecificData.m_pILOffsetMappingTable = PTR_ILOffsetMappingTable(new ILOffsetMappingTable);
    }

    ILOffsetMappingEntry currentEntry = m_debuggerSpecificData.m_pILOffsetMappingTable->Lookup(ILOffsetMappingTraits::GetKey(entry));
    if (!ILOffsetMappingTraits::IsNull(currentEntry))
        currentEntry.m_mapping.Clear();

    m_debuggerSpecificData.m_pILOffsetMappingTable->AddOrReplace(entry);
}
#endif // DACCESS_COMPILE

//---------------------------------------------------------------------------------------
//
// Retrieve the instrumented IL offset mapping for the specified method.
//
// Arguments:
//    token   - the MethodDef token of the method in question
//
// Return Value:
//    Return the mapping information between original IL offsets and instrumented IL offsets.
//    Check InstrumentedILOffsetMapping::IsNull() to see if any mapping is available.
//
// Notes:
//    * Once added, the mapping stays valid until the Module containing the method is destructed.
//    * The profiler may potentially update the mapping more than once.
//

InstrumentedILOffsetMapping Module::GetInstrumentedILOffsetMapping(mdMethodDef token)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    // Lazily allocate a Crst to serialize update access to the hash table.
    // If the Crst is NULL, then we couldn't possibly have added any mapping yet, so just return NULL.
    if (m_debuggerSpecificData.m_pDynamicILCrst == NULL)
    {
        InstrumentedILOffsetMapping emptyMapping;
        return emptyMapping;
    }

    CrstHolder ch(m_debuggerSpecificData.m_pDynamicILCrst);

    // If the hash table hasn't been created, then we couldn't possibly have added any mapping yet,
    // so just return NULL.
    if (m_debuggerSpecificData.m_pILOffsetMappingTable == NULL)
    {
        InstrumentedILOffsetMapping emptyMapping;
        return emptyMapping;
    }

    ILOffsetMappingEntry entry = m_debuggerSpecificData.m_pILOffsetMappingTable->Lookup(token);
    return entry.m_mapping;
}

#undef DECODE_TYPEID
#undef ENCODE_TYPEID
#undef IS_ENCODED_TYPEID



#ifndef DACCESS_COMPILE


BOOL Module::IsNoStringInterning()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END

    if (!(m_dwPersistedFlags & COMPUTED_STRING_INTERNING))
    {
        // The flags should be precomputed in native images
        _ASSERTE(!HasNativeImage());

        // Default is string interning
        BOOL fNoStringInterning = FALSE;

        HRESULT hr;

        // This flag applies to assembly, but it is stored on module so it can be cached in ngen image
        // Thus, we should ever need it for manifest module only.
        IMDInternalImport *mdImport = GetAssembly()->GetManifestImport();
        _ASSERTE(mdImport);

        mdToken token;
        IfFailThrow(mdImport->GetAssemblyFromScope(&token));

        const BYTE *pVal;
        ULONG       cbVal;

        hr = mdImport->GetCustomAttributeByName(token,
                        COMPILATIONRELAXATIONS_TYPE,
                        (const void**)&pVal, &cbVal);

        // Parse the attribute
        if (hr == S_OK)
        {
            CustomAttributeParser cap(pVal, cbVal);
            IfFailThrow(cap.SkipProlog());

            // Get Flags
            UINT32 flags;
            IfFailThrow(cap.GetU4(&flags));

            if (flags & CompilationRelaxations_NoStringInterning)
            {
                fNoStringInterning = TRUE;
            }
        }

#ifdef _DEBUG
        static ConfigDWORD g_NoStringInterning;
        DWORD dwOverride = g_NoStringInterning.val(CLRConfig::INTERNAL_NoStringInterning);

        if (dwOverride == 0)
        {
            // Disabled
            fNoStringInterning = FALSE;
        }
        else if (dwOverride == 2)
        {
            // Always true (testing)
            fNoStringInterning = TRUE;
        }
#endif // _DEBUG

        FastInterlockOr(&m_dwPersistedFlags, COMPUTED_STRING_INTERNING |
            (fNoStringInterning ? NO_STRING_INTERNING : 0));
    }

    return !!(m_dwPersistedFlags & NO_STRING_INTERNING);
}

BOOL Module::HasDefaultDllImportSearchPathsAttribute()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if(IsDefaultDllImportSearchPathsAttributeCached())
    {
        return (m_dwPersistedFlags & DEFAULT_DLL_IMPORT_SEARCH_PATHS_STATUS) != 0 ;
    }

    BOOL attributeIsFound = FALSE;
    attributeIsFound = GetDefaultDllImportSearchPathsAttributeValue(this, TokenFromRid(1, mdtAssembly),&m_DefaultDllImportSearchPathsAttributeValue);
    if(attributeIsFound)
    {
        FastInterlockOr(&m_dwPersistedFlags, DEFAULT_DLL_IMPORT_SEARCH_PATHS_IS_CACHED | DEFAULT_DLL_IMPORT_SEARCH_PATHS_STATUS);
    }
    else
    {
        FastInterlockOr(&m_dwPersistedFlags, DEFAULT_DLL_IMPORT_SEARCH_PATHS_IS_CACHED);
    }

    return (m_dwPersistedFlags & DEFAULT_DLL_IMPORT_SEARCH_PATHS_STATUS) != 0 ;
}

// Returns a BOOL to indicate if we have computed whether compiler has instructed us to
// wrap the non-CLS compliant exceptions or not.
BOOL Module::IsRuntimeWrapExceptionsStatusComputed()
{
    LIMITED_METHOD_CONTRACT;

    return (m_dwPersistedFlags & COMPUTED_WRAP_EXCEPTIONS);
}

BOOL Module::IsRuntimeWrapExceptions()
{
    CONTRACTL
    {
        THROWS;
        if (IsRuntimeWrapExceptionsStatusComputed()) GC_NOTRIGGER; else GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END

    if (!(IsRuntimeWrapExceptionsStatusComputed()))
    {
        // The flags should be precomputed in native images
        _ASSERTE(!HasNativeImage());

        HRESULT hr;
        BOOL fRuntimeWrapExceptions = FALSE;

        // This flag applies to assembly, but it is stored on module so it can be cached in ngen image
        // Thus, we should ever need it for manifest module only.
        IMDInternalImport *mdImport = GetAssembly()->GetManifestImport();

        mdToken token;
        IfFailGo(mdImport->GetAssemblyFromScope(&token));

        const BYTE *pVal;
        ULONG       cbVal;

        hr = mdImport->GetCustomAttributeByName(token,
                        RUNTIMECOMPATIBILITY_TYPE,
                        (const void**)&pVal, &cbVal);

        // Parse the attribute
        if (hr == S_OK)
        {
            CustomAttributeParser ca(pVal, cbVal);
            CaNamedArg namedArgs[1] = {{0}};

            // First, the void constructor:
            IfFailGo(ParseKnownCaArgs(ca, NULL, 0));

            // Then, find the named argument
            namedArgs[0].InitBoolField("WrapNonExceptionThrows");

            IfFailGo(ParseKnownCaNamedArgs(ca, namedArgs, lengthof(namedArgs)));

            if (namedArgs[0].val.boolean)
                fRuntimeWrapExceptions = TRUE;
        }
ErrExit:
        FastInterlockOr(&m_dwPersistedFlags, COMPUTED_WRAP_EXCEPTIONS |
            (fRuntimeWrapExceptions ? WRAP_EXCEPTIONS : 0));
    }

    return !!(m_dwPersistedFlags & WRAP_EXCEPTIONS);
}

BOOL Module::IsPreV4Assembly()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END

    if (!(m_dwPersistedFlags & COMPUTED_IS_PRE_V4_ASSEMBLY))
    {
        // The flags should be precomputed in native images
        _ASSERTE(!HasNativeImage());

        IMDInternalImport *pImport = GetAssembly()->GetManifestImport();
        _ASSERTE(pImport);

        BOOL fIsPreV4Assembly = FALSE;
        LPCSTR szVersion = NULL;
        if (SUCCEEDED(pImport->GetVersionString(&szVersion)))
        {
            if (szVersion != NULL && strlen(szVersion) > 2)
            {
                fIsPreV4Assembly = (szVersion[0] == 'v' || szVersion[0] == 'V') &&
                                   (szVersion[1] == '1' || szVersion[1] == '2');
            }
        }

        FastInterlockOr(&m_dwPersistedFlags, COMPUTED_IS_PRE_V4_ASSEMBLY |
            (fIsPreV4Assembly ? IS_PRE_V4_ASSEMBLY : 0));
    }

    return !!(m_dwPersistedFlags & IS_PRE_V4_ASSEMBLY);
}


ArrayDPTR(RelativeFixupPointer<PTR_MethodTable>) ModuleCtorInfo::GetGCStaticMTs(DWORD index)
{
    LIMITED_METHOD_CONTRACT;

    if (index < numHotGCStaticsMTs)
    {
        _ASSERTE(ppHotGCStaticsMTs != NULL);

        return ppHotGCStaticsMTs + index;
    }
    else
    {
        _ASSERTE(ppColdGCStaticsMTs != NULL);

        // shift the start of the cold table because all cold offsets are also shifted
        return ppColdGCStaticsMTs + (index - numHotGCStaticsMTs);
    }
}

DWORD Module::AllocateDynamicEntry(MethodTable *pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(pMT->GetModuleForStatics() == this);
        PRECONDITION(pMT->IsDynamicStatics());
        PRECONDITION(!pMT->ContainsGenericVariables());
    }
    CONTRACTL_END;

    DWORD newId = FastInterlockExchangeAdd((LONG*)&m_cDynamicEntries, 1);

    if (newId >= VolatileLoad(&m_maxDynamicEntries))
    {
        CrstHolder ch(&m_Crst);

        if (newId >= m_maxDynamicEntries)
        {
            SIZE_T maxDynamicEntries = max(16, m_maxDynamicEntries);
            while (maxDynamicEntries <= newId)
            {
                maxDynamicEntries *= 2;
            }

            DynamicStaticsInfo* pNewDynamicStaticsInfo = (DynamicStaticsInfo*)
                (void*)GetLoaderAllocator()->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(DynamicStaticsInfo)) * S_SIZE_T(maxDynamicEntries));

            if (m_pDynamicStaticsInfo)
                memcpy(pNewDynamicStaticsInfo, m_pDynamicStaticsInfo, sizeof(DynamicStaticsInfo) * m_maxDynamicEntries);

            m_pDynamicStaticsInfo = pNewDynamicStaticsInfo;
            VolatileStore(&m_maxDynamicEntries, maxDynamicEntries);
        }
    }

    m_pDynamicStaticsInfo[newId].pEnclosingMT = pMT;

    LOG((LF_CLASSLOADER, LL_INFO10000, "STATICS: Assigned dynamic ID %d to %s\n", newId, pMT->GetDebugClassName()));

    return newId;
}

void Module::FreeModuleIndex()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    if (m_ModuleID != NULL)
    {
        _ASSERTE(m_ModuleIndex == m_ModuleID->GetModuleIndex());

#ifndef CROSSGEN_COMPILE
        if (IsCollectible())
        {
            ThreadStoreLockHolder tsLock;
            Thread *pThread = NULL;
            while ((pThread = ThreadStore::GetThreadList(pThread)) != NULL)
            {
                pThread->DeleteThreadStaticData(m_ModuleIndex);
            }
        }
#endif // CROSSGEN_COMPILE

        // Get the ModuleIndex from the DLM and free it
        Module::FreeModuleIndex(m_ModuleIndex);
    }
    else
    {
        // This was an empty, short-lived Module object that
        // was never assigned a ModuleIndex...
    }
}




ModuleIndex Module::AllocateModuleIndex()
{
    DWORD val;
    g_pModuleIndexDispenser->NewId(NULL, val);

    // For various reasons, the IDs issued by the IdDispenser start at 1.
    // Domain neutral module IDs have historically started at 0, and we
    // have always assigned ID 0 to CoreLib. Thus, to make it so that
    // domain neutral module IDs start at 0, we will subtract 1 from the
    // ID that we got back from the ID dispenser.
    ModuleIndex index((SIZE_T)(val-1));

    return index;
}

void Module::FreeModuleIndex(ModuleIndex index)
{
    WRAPPER_NO_CONTRACT;
    // We subtracted 1 after we allocated this ID, so we need to
    // add 1 before we free it.
    DWORD val = index.m_dwIndex + 1;

    g_pModuleIndexDispenser->DisposeId(val);
}


void Module::AllocateRegularStaticHandles(AppDomain* pDomain)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

#ifndef CROSSGEN_COMPILE
    if (NingenEnabled())
        return;

    // Allocate the handles we will need. Note that AllocateStaticFieldObjRefPtrs will only
    // allocate if pModuleData->GetGCStaticsBasePointerAddress(pMT) != 0, avoiding creating
    // handles more than once for a given MT or module

    DomainLocalModule *pModuleData = GetDomainLocalModule();

    _ASSERTE(pModuleData->GetPrecomputedGCStaticsBasePointerAddress() != NULL);
    if (this->m_dwMaxGCRegularStaticHandles > 0)
    {
        pDomain->AllocateStaticFieldObjRefPtrs(this->m_dwMaxGCRegularStaticHandles,
                                               pModuleData->GetPrecomputedGCStaticsBasePointerAddress());

        // We should throw if we fail to allocate and never hit this assert
        _ASSERTE(pModuleData->GetPrecomputedGCStaticsBasePointer() != NULL);
    }
#endif // CROSSGEN_COMPILE
}

BOOL Module::IsStaticStoragePrepared(mdTypeDef tkType)
{
    LIMITED_METHOD_CONTRACT;

    // Right now the design is that we do one static allocation pass during NGEN,
    // and a 2nd pass for it at module init time for modules that weren't NGENed or the NGEN
    // pass was unsucessful. If we are loading types after that then we must use dynamic
    // static storage. These dynamic statics require an additional indirection so they
    // don't perform quite as well.
    //
    // This check was created for the scenario where a profiler adds additional types
    // however it seems likely this check would also accurately handle other dynamic
    // scenarios such as ref.emit and EnC as long as they are adding new types and
    // not new statics to existing types.
    _ASSERTE(TypeFromToken(tkType) == mdtTypeDef);
    return m_maxTypeRidStaticsAllocated >= RidFromToken(tkType);
}

void Module::AllocateStatics(AllocMemTracker *pamTracker)
{
    STANDARD_VM_CONTRACT;

    if (IsResource())
    {
        m_dwRegularStaticsBlockSize = DomainLocalModule::OffsetOfDataBlob();
        m_dwThreadStaticsBlockSize = ThreadLocalModule::OffsetOfDataBlob();

        // If it has no code, we don't have to allocate anything
        LOG((LF_CLASSLOADER, LL_INFO10000, "STATICS: Resource module %s. No statics needed\n", GetSimpleName()));
        _ASSERTE(m_maxTypeRidStaticsAllocated == 0);
        return;
    }
#ifdef FEATURE_PREJIT
    if (m_pRegularStaticOffsets == (PTR_DWORD) NGEN_STATICS_ALLCLASSES_WERE_LOADED)
    {
        _ASSERTE(HasNativeImage());

        // This is an ngen image and all the classes were loaded at ngen time, so we're done.
        LOG((LF_CLASSLOADER, LL_INFO10000, "STATICS: 'Complete' Native image found, no statics parsing needed for module %s.\n", GetSimpleName()));
        // typeDefs rids 0 and 1 aren't included in the count, thus X typeDefs means rid X+1 is valid
        _ASSERTE(m_maxTypeRidStaticsAllocated == GetMDImport()->GetCountWithTokenKind(mdtTypeDef) + 1);
        return;
    }
#endif
    LOG((LF_CLASSLOADER, LL_INFO10000, "STATICS: Allocating statics for module %s\n", GetSimpleName()));

    // Build the offset table, which will tell us what the offsets for the statics of each class are (one offset for gc handles, one offset
    // for non gc types)
    BuildStaticsOffsets(pamTracker);
}

void Module::SetDomainFile(DomainFile *pDomainFile)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(pDomainFile));
        PRECONDITION(IsManifest() == pDomainFile->IsAssembly());
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    DomainLocalModule* pModuleData = 0;

    // Do we need to allocate memory for the non GC statics?
    if (m_ModuleID == NULL)
    {
        // Allocate memory for the module statics.
        LoaderAllocator *pLoaderAllocator = NULL;
        if (GetAssembly()->IsCollectible())
        {
            pLoaderAllocator = GetAssembly()->GetLoaderAllocator();
        }
        else
        {
            pLoaderAllocator = pDomainFile->GetAppDomain()->GetLoaderAllocator();
        }

        SIZE_T size = GetDomainLocalModuleSize();

        LOG((LF_CLASSLOADER, LL_INFO10, "STATICS: Allocating %i bytes for precomputed statics in module %S in LoaderAllocator %p\n",
            size, this->GetDebugName(), pLoaderAllocator));

        // We guarantee alignment for 64-bit regular statics on 32-bit platforms even without FEATURE_64BIT_ALIGNMENT for performance reasons.

        _ASSERTE(size >= DomainLocalModule::OffsetOfDataBlob());

        pModuleData = (DomainLocalModule*)(void*)
            pLoaderAllocator->GetHighFrequencyHeap()->AllocAlignedMem(
                size, MAX_PRIMITIVE_FIELD_SIZE);

        // Note: Memory allocated on loader heap is zero filled
        // memset(pModuleData, 0, size);

        // Verify that the space is really zero initialized
        _ASSERTE(pModuleData->GetPrecomputedGCStaticsBasePointer() == NULL);

        // If the module was loaded as domain-specific, then we need to assign
        // this module a domain-neutral module ID.
        pModuleData->m_ModuleIndex = Module::AllocateModuleIndex();
        m_ModuleIndex = pModuleData->m_ModuleIndex;
    }
    else
    {
        pModuleData = this->m_ModuleID;
        LOG((LF_CLASSLOADER, LL_INFO10, "STATICS: Allocation not needed for ngened non shared module %s in Appdomain %08x\n"));
    }

    // Non shared case, module points directly to the statics. In ngen case
    // m_pDomainModule is already set for the non shared case
    if (m_ModuleID == NULL)
    {
        m_ModuleID = pModuleData;
    }

    m_ModuleID->SetDomainFile(pDomainFile);

    // Allocate static handles now.
    // NOTE: Bootstrapping issue with CoreLib - we will manually allocate later
    // If the assembly is collectible, we don't initialize static handles for them
    // as it is currently initialized through the DomainLocalModule::PopulateClass in MethodTable::CheckRunClassInitThrowing
    // (If we don't do this, it would allocate here unused regular static handles that will be overridden later)
    if (g_pPredefinedArrayTypes[ELEMENT_TYPE_OBJECT] != NULL && !GetAssembly()->IsCollectible())
        AllocateRegularStaticHandles(pDomainFile->GetAppDomain());
}

#ifndef CROSSGEN_COMPILE
OBJECTREF Module::GetExposedObject()
{
    CONTRACT(OBJECTREF)
    {
        INSTANCE_CHECK;
        POSTCONDITION(RETVAL != NULL);
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACT_END;

    RETURN GetDomainFile()->GetExposedModuleObject();
}
#endif // CROSSGEN_COMPILE

//
// AllocateMap allocates the RID maps based on the size of the current
// metadata (if any)
//

void Module::AllocateMaps()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    enum
    {
        TYPEDEF_MAP_INITIAL_SIZE = 5,
        TYPEREF_MAP_INITIAL_SIZE = 5,
        MEMBERDEF_MAP_INITIAL_SIZE = 10,
        GENERICPARAM_MAP_INITIAL_SIZE = 5,
        GENERICTYPEDEF_MAP_INITIAL_SIZE = 5,
        FILEREFERENCES_MAP_INITIAL_SIZE = 5,
        ASSEMBLYREFERENCES_MAP_INITIAL_SIZE = 5,
    };

    PTR_TADDR pTable = NULL;

    if (IsResource())
        return;

    if (IsReflection())
    {
        // For dynamic modules, it is essential that we at least have a TypeDefToMethodTable
        // map with an initial block.  Otherwise, all the iterators will abort on an
        // initial empty table and we will e.g. corrupt the backpatching chains during
        // an appdomain unload.
        m_TypeDefToMethodTableMap.dwCount = TYPEDEF_MAP_INITIAL_SIZE;

        // The above is essential.  The following ones are precautionary.
        m_TypeRefToMethodTableMap.dwCount = TYPEREF_MAP_INITIAL_SIZE;
        m_MethodDefToDescMap.dwCount = MEMBERDEF_MAP_INITIAL_SIZE;
        m_FieldDefToDescMap.dwCount = MEMBERDEF_MAP_INITIAL_SIZE;
        m_GenericParamToDescMap.dwCount = GENERICPARAM_MAP_INITIAL_SIZE;
        m_GenericTypeDefToCanonMethodTableMap.dwCount = TYPEDEF_MAP_INITIAL_SIZE;
        m_FileReferencesMap.dwCount = FILEREFERENCES_MAP_INITIAL_SIZE;
        m_ManifestModuleReferencesMap.dwCount = ASSEMBLYREFERENCES_MAP_INITIAL_SIZE;
        m_MethodDefToPropertyInfoMap.dwCount = MEMBERDEF_MAP_INITIAL_SIZE;
    }
    else
    {
        IMDInternalImport * pImport = GetMDImport();

        // Get # TypeDefs (add 1 for COR_GLOBAL_PARENT_TOKEN)
        m_TypeDefToMethodTableMap.dwCount = pImport->GetCountWithTokenKind(mdtTypeDef)+2;

        // Get # TypeRefs
        m_TypeRefToMethodTableMap.dwCount = pImport->GetCountWithTokenKind(mdtTypeRef)+1;

        // Get # MethodDefs
        m_MethodDefToDescMap.dwCount = pImport->GetCountWithTokenKind(mdtMethodDef)+1;

        // Get # FieldDefs
        m_FieldDefToDescMap.dwCount = pImport->GetCountWithTokenKind(mdtFieldDef)+1;

        // Get # GenericParams
        m_GenericParamToDescMap.dwCount = pImport->GetCountWithTokenKind(mdtGenericParam)+1;

        // Get the number of FileReferences in the map
        m_FileReferencesMap.dwCount = pImport->GetCountWithTokenKind(mdtFile)+1;

        // Get the number of AssemblyReferences in the map
        m_ManifestModuleReferencesMap.dwCount = pImport->GetCountWithTokenKind(mdtAssemblyRef)+1;

        // These maps are only added to during NGen, so for other scenarios leave them empty
        if (IsCompilationProcess())
        {
            m_GenericTypeDefToCanonMethodTableMap.dwCount = m_TypeDefToMethodTableMap.dwCount;
            m_MethodDefToPropertyInfoMap.dwCount = m_MethodDefToDescMap.dwCount;
        }
        else
        {
            m_GenericTypeDefToCanonMethodTableMap.dwCount = 0;
            m_MethodDefToPropertyInfoMap.dwCount = 0;
        }
    }

    S_SIZE_T nTotal;

    nTotal += m_TypeDefToMethodTableMap.dwCount;
    nTotal += m_TypeRefToMethodTableMap.dwCount;
    nTotal += m_MethodDefToDescMap.dwCount;
    nTotal += m_FieldDefToDescMap.dwCount;
    nTotal += m_GenericParamToDescMap.dwCount;
    nTotal += m_GenericTypeDefToCanonMethodTableMap.dwCount;
    nTotal += m_FileReferencesMap.dwCount;
    nTotal += m_ManifestModuleReferencesMap.dwCount;
    nTotal += m_MethodDefToPropertyInfoMap.dwCount;

    _ASSERTE (m_pAssembly && m_pAssembly->GetLowFrequencyHeap());
    pTable = (PTR_TADDR)(void*)m_pAssembly->GetLowFrequencyHeap()->AllocMem(nTotal * S_SIZE_T(sizeof(TADDR)));

    // Note: Memory allocated on loader heap is zero filled
    // memset(pTable, 0, nTotal * sizeof(void*));

    m_TypeDefToMethodTableMap.pNext  = NULL;
    m_TypeDefToMethodTableMap.supportedFlags = TYPE_DEF_MAP_ALL_FLAGS;
    m_TypeDefToMethodTableMap.pTable = pTable;

    m_TypeRefToMethodTableMap.pNext  = NULL;
    m_TypeRefToMethodTableMap.supportedFlags = TYPE_REF_MAP_ALL_FLAGS;
    m_TypeRefToMethodTableMap.pTable = &pTable[m_TypeDefToMethodTableMap.dwCount];

    m_MethodDefToDescMap.pNext  = NULL;
    m_MethodDefToDescMap.supportedFlags = METHOD_DEF_MAP_ALL_FLAGS;
    m_MethodDefToDescMap.pTable = &m_TypeRefToMethodTableMap.pTable[m_TypeRefToMethodTableMap.dwCount];

    m_FieldDefToDescMap.pNext  = NULL;
    m_FieldDefToDescMap.supportedFlags = FIELD_DEF_MAP_ALL_FLAGS;
    m_FieldDefToDescMap.pTable = &m_MethodDefToDescMap.pTable[m_MethodDefToDescMap.dwCount];

    m_GenericParamToDescMap.pNext  = NULL;
    m_GenericParamToDescMap.supportedFlags = GENERIC_PARAM_MAP_ALL_FLAGS;
    m_GenericParamToDescMap.pTable = &m_FieldDefToDescMap.pTable[m_FieldDefToDescMap.dwCount];

    m_GenericTypeDefToCanonMethodTableMap.pNext  = NULL;
    m_GenericTypeDefToCanonMethodTableMap.supportedFlags = GENERIC_TYPE_DEF_MAP_ALL_FLAGS;
    m_GenericTypeDefToCanonMethodTableMap.pTable = &m_GenericParamToDescMap.pTable[m_GenericParamToDescMap.dwCount];

    m_FileReferencesMap.pNext  = NULL;
    m_FileReferencesMap.supportedFlags = FILE_REF_MAP_ALL_FLAGS;
    m_FileReferencesMap.pTable = &m_GenericTypeDefToCanonMethodTableMap.pTable[m_GenericTypeDefToCanonMethodTableMap.dwCount];

    m_ManifestModuleReferencesMap.pNext  = NULL;
    m_ManifestModuleReferencesMap.supportedFlags = MANIFEST_MODULE_MAP_ALL_FLAGS;
    m_ManifestModuleReferencesMap.pTable = &m_FileReferencesMap.pTable[m_FileReferencesMap.dwCount];

    m_MethodDefToPropertyInfoMap.pNext = NULL;
    m_MethodDefToPropertyInfoMap.supportedFlags = PROPERTY_INFO_MAP_ALL_FLAGS;
    m_MethodDefToPropertyInfoMap.pTable = &m_ManifestModuleReferencesMap.pTable[m_ManifestModuleReferencesMap.dwCount];
}


//
// FreeClassTables frees the classes in the module
//

void Module::FreeClassTables()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_dwTransientFlags & CLASSES_FREED)
        return;

    FastInterlockOr(&m_dwTransientFlags, CLASSES_FREED);

    // disable ibc here because it can cause errors during the destruction of classes
    IBCLoggingDisabler disableLogging;

#if _DEBUG
    DebugLogRidMapOccupancy();
#endif

    //
    // Free the types filled out in the TypeDefToEEClass map
    //

    // Go through each linked block
    LookupMap<PTR_MethodTable>::Iterator typeDefIter(&m_TypeDefToMethodTableMap);
    while (typeDefIter.Next())
    {
        MethodTable * pMT = typeDefIter.GetElement();

        if (pMT != NULL && pMT->IsRestored())
        {
            pMT->GetClass()->Destruct(pMT);
        }
    }

    // Now do the same for constructed types (arrays and instantiated generic types)
    if (IsTenured())  // If we're destructing because of an error during the module's creation, we'll play it safe and not touch this table as its memory is freed by a
    {                 // separate AllocMemTracker. Though you're supposed to destruct everything else before destructing the AllocMemTracker, this is an easy invariant to break so
                      // we'll play extra safe on this end.
        if (m_pAvailableParamTypes != NULL)
        {
            EETypeHashTable::Iterator it(m_pAvailableParamTypes);
            EETypeHashEntry *pEntry;
            while (m_pAvailableParamTypes->FindNext(&it, &pEntry))
            {
                TypeHandle th = pEntry->GetTypeHandle();

                if (!th.IsRestored())
                    continue;

                // We need to call destruct on instances of EEClass whose "canonical" dependent lives in this table
                // There is nothing interesting to destruct on array EEClass
                if (!th.IsTypeDesc())
                {
                    MethodTable * pMT = th.AsMethodTable();
                    if (pMT->IsCanonicalMethodTable() && (!pMT->IsZapped() || pMT->GetZapModule() == this))
                        pMT->GetClass()->Destruct(pMT);
                }
            }
        }
    }
}

#endif // !DACCESS_COMPILE

ClassLoader *Module::GetClassLoader()
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;
    _ASSERTE(m_pAssembly != NULL);
    return m_pAssembly->GetLoader();
}

PTR_BaseDomain Module::GetDomain()
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;
    _ASSERTE(m_pAssembly != NULL);
    return m_pAssembly->GetDomain();
}

#ifndef DACCESS_COMPILE

#ifndef CROSSGEN_COMPILE
void Module::StartUnload()
{
    WRAPPER_NO_CONTRACT;
#ifdef PROFILING_SUPPORTED
    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackModuleLoads());
        if (!IsBeingUnloaded())
        {
            // Profiler is causing some peripheral class loads. Probably this just needs
            // to be turned into a Fault_not_fatal and moved to a specific place inside the profiler.
            EX_TRY
            {
                GCX_PREEMP();
                (&g_profControlBlock)->ModuleUnloadStarted((ModuleID) this);
            }
            EX_CATCH
            {
            }
            EX_END_CATCH(SwallowAllExceptions);
        }
        END_PROFILER_CALLBACK();
    }
#endif // PROFILING_SUPPORTED

    if (g_IBCLogger.InstrEnabled())
    {
        Thread * pThread = GetThread();
        ThreadLocalIBCInfo* pInfo = pThread->GetIBCInfo();

        // Acquire the Crst lock before creating the IBCLoggingDisabler object.
        // Only one thread at a time can be processing an IBC logging event.
        CrstHolder lock(IBCLogger::GetSync());
        {
            IBCLoggingDisabler disableLogging( pInfo );  // runs IBCLoggingDisabler::DisableLogging

            // Write out the method profile data
            /*hr=*/WriteMethodProfileDataLogFile(true);
        }
    }

    SetBeingUnloaded();
}
#endif // CROSSGEN_COMPILE

BOOL Module::IsInCurrentVersionBubble()
{
    LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
    if (!IsCompilationProcess())
        return TRUE;

    // The module being compiled is always part of the current version bubble
    AppDomain * pAppDomain = GetAppDomain();
    if (pAppDomain->IsCompilationDomain() && pAppDomain->ToCompilationDomain()->GetTargetModule() == this)
        return TRUE;

    if (IsReadyToRunCompilation())
        return IsLargeVersionBubbleEnabled();

    return TRUE;
#else // FEATURE_NATIVE_IMAGE_GENERATION
    return TRUE;
#endif // FEATURE_NATIVE_IMAGE_GENERATION
}

#if defined(FEATURE_READYTORUN) && !defined(FEATURE_READYTORUN_COMPILER)
//---------------------------------------------------------------------------------------
// Check if the target module is in the same version bubble as this one
// The current implementation uses the presence of an AssemblyRef for the target module's assembly in
// the native manifest metadata.
//
// Arguments:
//      * target - target module to check
//
// Return Value:
//      TRUE if the target module is in the same version bubble as this one
//
BOOL Module::IsInSameVersionBubble(Module *target)
{
    STANDARD_VM_CONTRACT;

    if (this == target)
    {
        return TRUE;
    }

    if (!HasNativeOrReadyToRunImage())
    {
        return FALSE;
    }

    NativeImage *nativeImage = this->GetCompositeNativeImage();
    IMDInternalImport* pMdImport = NULL;

    if (nativeImage != NULL)
    {
        if (nativeImage == target->GetCompositeNativeImage())
        {
            // Fast path for modules contained within the same native image
            return TRUE;
        }
        pMdImport = nativeImage->GetManifestMetadata();
    }
    else
    {
        // Check if the current module's image has native manifest metadata, otherwise the current->GetNativeAssemblyImport() asserts.
        COUNT_T cMeta=0;
        const void* pMeta = GetFile()->GetOpenedILimage()->GetNativeManifestMetadata(&cMeta);
        if (pMeta == NULL)
        {
            return FALSE;
        }
        pMdImport = GetNativeAssemblyImport();
    }

    LPCUTF8 targetName = target->GetAssembly()->GetSimpleName();

    HENUMInternal assemblyEnum;
    HRESULT hr = pMdImport->EnumAllInit(mdtAssemblyRef, &assemblyEnum);
    mdAssemblyRef assemblyRef;
    while (pMdImport->EnumNext(&assemblyEnum, &assemblyRef))
    {
        LPCSTR assemblyName;
        hr = pMdImport->GetAssemblyRefProps(assemblyRef, NULL, NULL, &assemblyName, NULL, NULL, NULL, NULL);
        if (strcmp(assemblyName, targetName) == 0)
        {
            return TRUE;
        }
    }

    return FALSE;
}
#endif // FEATURE_READYTORUN && !FEATURE_READYTORUN_COMPILER

//---------------------------------------------------------------------------------------
//
// Wrapper for Module::GetRWImporter + QI when writing is not needed.
//
// Arguments:
//      * dwOpenFlags - Combo from CorOpenFlags. Better not contain ofWrite!
//      * riid - Public IID requested
//      * ppvInterface - [out] Requested interface. On success, *ppvInterface is returned
//          refcounted; caller responsible for Release.
//
// Return Value:
//      HRESULT indicating success or failure.
//
HRESULT Module::GetReadablePublicMetaDataInterface(DWORD dwOpenFlags, REFIID riid, LPVOID * ppvInterface)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CAN_TAKE_LOCK;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE((dwOpenFlags & ofWrite) == 0);

    // Temporary place to store the IUnknown from which we'll do the final QI to get the
    // requested public interface.  Any assignment to pIUnk assumes pIUnk does not need
    // to do a Release() (either the interface was internal and not AddRef'd, or was
    // public and will be released by the above holder).
    IUnknown * pIUnk = NULL;

    HRESULT hr = S_OK;

    // Normally, we just get an RWImporter to do the QI on, and we're on our way.
    EX_TRY
    {
        pIUnk = GetRWImporter();
    }
    EX_CATCH_HRESULT_NO_ERRORINFO(hr);

    // Get the requested interface
    if (SUCCEEDED(hr) && (ppvInterface != NULL))
    {
        _ASSERTE(pIUnk != NULL);
        hr = pIUnk->QueryInterface(riid, (void **) ppvInterface);
    }

    return hr;
}

// a special token that indicates no reader could be created - don't try again
static ISymUnmanagedReader* const k_pInvalidSymReader = (ISymUnmanagedReader*)0x1;

#if defined(FEATURE_ISYM_READER) && !defined(CROSSGEN_COMPILE)
ISymUnmanagedReader *Module::GetISymUnmanagedReaderNoThrow(void)
{
    CONTRACT(ISymUnmanagedReader *)
    {
        INSTANCE_CHECK;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        NOTHROW;
        WRAPPER(GC_TRIGGERS);
        MODE_ANY;
    }
    CONTRACT_END;

    ISymUnmanagedReader *ret = NULL;

    EX_TRY
    {
        ret = GetISymUnmanagedReader();
    }
    EX_CATCH
    {
        // We swallow any exception and say that we simply couldn't get a reader by returning NULL.
        // The only type of error that should be possible here is OOM.
        /* DISABLED due to Dev10 bug 619495
        CONSISTENCY_CHECK_MSG(
            GET_EXCEPTION()->GetHR() == E_OUTOFMEMORY,
            "Exception from GetISymUnmanagedReader");
         */
    }
    EX_END_CATCH(RethrowTerminalExceptions);

    RETURN (ret);
}

ISymUnmanagedReader *Module::GetISymUnmanagedReader(void)
{
    CONTRACT(ISymUnmanagedReader *)
    {
        INSTANCE_CHECK;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        THROWS;
        WRAPPER(GC_TRIGGERS);
        MODE_ANY;
    }
    CONTRACT_END;

    // No symbols for resource modules
    if (IsResource())
        RETURN NULL;

    if (g_fEEShutDown)
        RETURN NULL;

    // Verify that symbol reading is permitted for this module.
    // If we know we've already created a symbol reader, don't bother checking.  There is
    // no advantage to allowing symbol reading to be turned off if we've already created the reader.
    // Note that we can't just put this code in the creation block below because we might have to
    // call managed code to resolve security policy, and we can't do that while holding a lock.
    // There is no disadvantage other than a minor perf cost to calling this unnecessarily, so the
    // race on m_pISymUnmanagedReader here is OK.  The perf cost is minor because the only real
    // work is done by the security system which caches the result.
    if( m_pISymUnmanagedReader == NULL && !IsSymbolReadingEnabled() )
        RETURN NULL;

    // Take the lock for the m_pISymUnmanagedReader
    // This ensures that we'll only ever attempt to create one reader at a time, and we won't
    // create a reader if we're in the middle of destroying one that has become stale.
    // Actual access to the reader can safely occur outside the lock as long as it has its own
    // AddRef which we take inside the lock at the bottom of this method.
    CrstHolder holder(&m_ISymUnmanagedReaderCrst);

    UINT lastErrorMode = 0;

    // If we haven't created a reader yet, do so now
    if (m_pISymUnmanagedReader == NULL)
    {
        // Mark our reader as invalid so that if we fail to create the reader
        // (including if an exception is thrown), we won't keep trying.
        m_pISymUnmanagedReader = k_pInvalidSymReader;

        // There are 4 main cases here:
        //  1. Assembly is on disk and we'll get the symbols from a file next to the assembly
        //  2. Assembly is provided by the host and we'll get the symbols from the host
        //  3. Assembly was loaded in-memory (by byte array or ref-emit), and symbols were
        //      provided along with it.
        //  4. Assembly was loaded in-memory but no symbols were provided.

        // Determine whether we should be looking in memory for the symbols (cases 2 & 3)
        bool fInMemorySymbols = ( m_file->IsIStream() || GetInMemorySymbolStream() );
        if( !fInMemorySymbols && m_file->GetPath().IsEmpty() )
        {
            // Case 4.  We don't have a module path, an IStream or an in memory symbol stream,
            // so there is no-where to try and get symbols from.
            RETURN (NULL);
        }

        // Create a binder to find the reader.
        //
        // <REVISIT_TODO>@perf: this is slow, creating and destroying the binder every
        // time. We should cache this somewhere, but I'm not 100% sure
        // where right now...</REVISIT_TODO>
        HRESULT hr = S_OK;

        SafeComHolder<ISymUnmanagedBinder> pBinder;

        if (g_pDebugInterface == NULL)
        {
            // @TODO: this is reachable when debugging!
            UNREACHABLE_MSG("About to CoCreateInstance!  This code should not be "
                            "reachable or needs to be reimplemented for CoreCLR!");
        }

        if (this->GetInMemorySymbolStreamFormat() == eSymbolFormatILDB)
        {
            // We've got in-memory ILDB symbols, create the ILDB symbol binder
            // Note that in this case, we must be very careful not to use diasymreader.dll
            // at all - we don't trust it, and shouldn't run any code in it
            IfFailThrow(IldbSymbolsCreateInstance(CLSID_CorSymBinder_SxS, IID_ISymUnmanagedBinder, (void**)&pBinder));
        }
        else
        {
            // We're going to be working with Windows PDB format symbols. Attempt to CoCreate the symbol binder.
            // CoreCLR supports not having a symbol reader installed, so CoCreate searches the PATH env var
            // and then tries coreclr dll location.
            // On desktop, the framework installer is supposed to install diasymreader.dll as well
            // and so this shouldn't happen.
            hr = FakeCoCreateInstanceEx(CLSID_CorSymBinder_SxS, NATIVE_SYMBOL_READER_DLL, IID_ISymUnmanagedBinder, (void**)&pBinder, NULL);
            if (FAILED(hr))
            {
                PathString symbolReaderPath;
                hr = GetClrModuleDirectory(symbolReaderPath);
                if (FAILED(hr))
                {
                    RETURN (NULL);
                }
                symbolReaderPath.Append(NATIVE_SYMBOL_READER_DLL);
                hr = FakeCoCreateInstanceEx(CLSID_CorSymBinder_SxS, symbolReaderPath.GetUnicode(), IID_ISymUnmanagedBinder, (void**)&pBinder, NULL);
                if (FAILED(hr))
                {
                    RETURN (NULL);
                }
            }
        }

        LOG((LF_CORDB, LL_INFO10, "M::GISUR: Created binder\n"));

        // Note: we change the error mode here so we don't get any popups as the PDB symbol reader attempts to search the
        // hard disk for files.
        lastErrorMode = SetErrorMode(SEM_NOOPENFILEERRORBOX|SEM_FAILCRITICALERRORS);

        SafeComHolder<ISymUnmanagedReader> pReader;

        if (fInMemorySymbols)
        {
            SafeComHolder<IStream> pIStream( NULL );

            // If debug stream is already specified, don't bother to go through fusion
            // This is the common case for case 2 (hosted modules) and case 3 (Ref.Emit).
            if (GetInMemorySymbolStream() )
            {

                if( IsReflection() )
                {
                    // If this is Reflection.Emit, we must clone the stream because another thread may
                    // update it when someone is using the reader we create here leading to AVs.
                    // Note that the symbol stream should be up to date since we flush the writer
                    // after every addition in Module::AddClass.
                    IfFailThrow(GetInMemorySymbolStream()->Clone(&pIStream));
                }
                else
                {
                    // The stream is not changing. Just add-ref to it.
                    pIStream = GetInMemorySymbolStream();
                    pIStream->AddRef();
                }
            }
            if (SUCCEEDED(hr))
            {
                hr = pBinder->GetReaderFromStream(GetRWImporter(), pIStream, &pReader);
            }
        }
        else
        {
            // The assembly is on disk, so try and load symbols based on the path to the assembly (case 1)
            const SString &path = m_file->GetPath();

            // Call Fusion to ensure that any PDB's are shadow copied before
            // trying to get a symbol reader. This has to be done once per
            // Assembly.
            ReleaseHolder<IUnknown> pUnk = NULL;
            hr = GetReadablePublicMetaDataInterface(ofReadOnly, IID_IMetaDataImport, &pUnk);
            if (SUCCEEDED(hr))
                hr = pBinder->GetReaderForFile(pUnk, path, NULL, &pReader);
        }

        SetErrorMode(lastErrorMode);

        if (SUCCEEDED(hr))
        {
            m_pISymUnmanagedReader = pReader.Extract();
            LOG((LF_CORDB, LL_INFO10, "M::GISUR: Loaded symbols for module %S\n", GetDebugName()));
        }
        else
        {
            // We failed to create the reader, don't try again next time
            LOG((LF_CORDB, LL_INFO10, "M::GISUR: Failed to load symbols for module %S\n", GetDebugName()));
            _ASSERTE( m_pISymUnmanagedReader == k_pInvalidSymReader );
        }

    } // if( m_pISymUnmanagedReader == NULL )

    // If we previously failed to create the reader, return NULL
    if (m_pISymUnmanagedReader == k_pInvalidSymReader)
    {
        RETURN (NULL);
    }

    // Success - return an AddRef'd copy of the reader
    m_pISymUnmanagedReader->AddRef();
    RETURN (m_pISymUnmanagedReader);
}
#endif // FEATURE_ISYM_READER && !CROSSGEN_COMPILE

BOOL Module::IsSymbolReadingEnabled()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // If the module has symbols in-memory (eg. RefEmit) that are in ILDB
    // format, then there isn't any reason not to supply them.  The reader
    // code is always available, and we trust it's security.
    if (this->GetInMemorySymbolStreamFormat() == eSymbolFormatILDB)
    {
        return TRUE;
    }

#ifdef DEBUGGING_SUPPORTED
    if (!g_pDebugInterface)
    {
        // if debugging is disabled (no debug pack installed), do not load symbols
        // This is done for two reasons.  We don't completely trust the security of
        // the diasymreader.dll code, so we don't want to use it in mainline scenarios.
        // Secondly, there's not reason that diasymreader.dll will even necssarily be
        // be on the machine if the debug pack isn't installed.
        return FALSE;
    }
#endif // DEBUGGING_SUPPORTED


    return TRUE;
}

// At this point, this is only called when we're creating an appdomain
// out of an array of bytes, so we'll keep the IStream that we create
// around in case the debugger attaches later (including detach & re-attach!)
void Module::SetSymbolBytes(LPCBYTE pbSyms, DWORD cbSyms)
{
    STANDARD_VM_CONTRACT;

    // Create a IStream from the memory for the syms.
    SafeComHolder<CGrowableStream> pStream(new CGrowableStream());

    // Do not need to AddRef the CGrowableStream because the constructor set it to 1
    // ref count already. The Module will keep a copy for its own use.

    // Make sure to set the symbol stream on the module before
    // attempting to send UpdateModuleSyms messages up for it.
    SetInMemorySymbolStream(pStream, eSymbolFormatPDB);

    // This can only be called when the module is being created.  No-one should have
    // tried to use the symbols yet, and so there should not be a reader.
    // If instead, we wanted to call this when a reader could have been created, we need to
    // serialize access by taking the reader lock, and flush the old reader by calling
    // code:Module.ReleaseISymUnmanagedReader
    _ASSERTE( m_pISymUnmanagedReader == NULL );

#ifdef LOGGING
    LPCWSTR pName = NULL;
    pName = GetDebugName();
#endif // LOGGING

    ULONG cbWritten;
    DWORD dwError = pStream->Write((const void *)pbSyms,
                               (ULONG)cbSyms,
                                                &cbWritten);
    IfFailThrow(HRESULT_FROM_WIN32(dwError));

#if PROFILING_SUPPORTED && !defined(CROSSGEN_COMPILE)
    BEGIN_PROFILER_CALLBACK(CORProfilerInMemorySymbolsUpdatesEnabled());
    {
        (&g_profControlBlock)->ModuleInMemorySymbolsUpdated((ModuleID) this);
    }
    END_PROFILER_CALLBACK();
#endif //PROFILING_SUPPORTED && !defined(CROSSGEN_COMPILE)

    ETW::CodeSymbolLog::EmitCodeSymbols(this);

    // Tell the debugger that symbols have been loaded for this
    // module.  We iterate through all domains which contain this
    // module's assembly, and send a debugger notify for each one.
    // <REVISIT_TODO>@perf: it would scale better if we directly knew which domains
    // the assembly was loaded in.</REVISIT_TODO>
    if (CORDebuggerAttached())
    {
        AppDomainIterator i(FALSE);

        while (i.Next())
        {
            AppDomain *pDomain = i.GetDomain();

            if (pDomain->IsDebuggerAttached() && (GetDomain() == SystemDomain::System() ||
                                                  pDomain->ContainsAssembly(m_pAssembly)))
            {
                g_pDebugInterface->SendUpdateModuleSymsEventAndBlock(this, pDomain);
            }
        }
    }
}

// Clear any cached symbol reader
void Module::ReleaseISymUnmanagedReader(void)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    // Caller is responsible for taking the reader lock if the call could occur when
    // other threads are using or creating the reader
    if( m_pISymUnmanagedReader != NULL )
    {
        // If we previously failed to create a reader, don't attempt to release it
        // but do clear it out so that we can try again (eg. symbols may have changed)
        if( m_pISymUnmanagedReader != k_pInvalidSymReader )
        {
            m_pISymUnmanagedReader->Release();
        }
        m_pISymUnmanagedReader = NULL;
    }
}

// Lazily creates a new IL stub cache for this module.
ILStubCache* Module::GetILStubCache()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    // Use per-LoaderAllocator cache for modules when not NGENing
    BaseDomain *pDomain = GetDomain();
    if (!IsSystem() && !pDomain->AsAppDomain()->IsCompilationDomain())
        return GetLoaderAllocator()->GetILStubCache();

    if (m_pILStubCache == NULL)
    {
        ILStubCache *pILStubCache = new ILStubCache(GetLoaderAllocator()->GetHighFrequencyHeap());

        if (FastInterlockCompareExchangePointer(&m_pILStubCache, pILStubCache, NULL) != NULL)
        {
            // some thread swooped in and set the field
            delete pILStubCache;
        }
    }
    _ASSERTE(m_pILStubCache != NULL);
    return m_pILStubCache;
}

// Called to finish the process of adding a new class with Reflection.Emit
void Module::AddClass(mdTypeDef classdef)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(!IsResource());
    }
    CONTRACTL_END;

    // The fake class associated with the module (global fields & functions) needs to be initialized here
    // Normal classes are added to the available class hash when their typedef is first created.
    if (RidFromToken(classdef) == 0)
    {
        BuildClassForModule();
    }

    // Since the module is being modified, the in-memory symbol stream
    // (if any) has probably also been modified. If we support reading the symbols
    // then we need to commit the changes to the writer and flush any old readers
    // However if we don't support reading then we can skip this which will give
    // a substantial perf improvement. See DDB 671107.
    if(IsSymbolReadingEnabled())
    {
        CONSISTENCY_CHECK(IsReflection());   // this is only used for dynamic modules
        ISymUnmanagedWriter * pWriter = GetReflectionModule()->GetISymUnmanagedWriter();
        if (pWriter != NULL)
        {
            // Serialize with any concurrent reader creations
            // Specifically, if we started creating a reader on one thread, and then updated the
            // symbols on another thread, we need to wait until the initial reader creation has
            // completed and release it so we don't get stuck with a stale reader.
            // Also, if we commit to the stream while we're in the process of creating a reader,
            // the reader will get corrupted/incomplete data.
            // Note that we must also be in co-operative mode here to ensure the debugger helper
            // thread can't be simultaneously reading this stream while the process is synchronized
            // (code:Debugger::GetSymbolBytes)
            CrstHolder holder(&m_ISymUnmanagedReaderCrst);

            // Flush writes to the symbol store to the symbol stream
            // Note that we do this when finishing the addition of the class, instead of
            // on-demand in GetISymUnmanagedReader because the writer is not thread-safe.
            // Here, we're inside the lock of TypeBuilder.CreateType, and so it's safe to
            // manipulate the writer.
            SafeComHolderPreemp<ISymUnmanagedWriter3> pWriter3;
            HRESULT thr = pWriter->QueryInterface(IID_ISymUnmanagedWriter3, (void**)&pWriter3);
            CONSISTENCY_CHECK(SUCCEEDED(thr));
            if (SUCCEEDED(thr))
            {
                thr = pWriter3->Commit();
                if (SUCCEEDED(thr))
                {
                    // Flush any cached symbol reader to ensure we pick up any new symbols
                    ReleaseISymUnmanagedReader();
                }
            }

            // If either the QI or Commit failed
            if (FAILED(thr))
            {
                // The only way we expect this might fail is out-of-memory.  In that
                // case we silently fail to update the symbol stream with new data, but
                // we leave the existing reader intact.
                CONSISTENCY_CHECK(thr==E_OUTOFMEMORY);
            }
        }
    }
}

//---------------------------------------------------------------------------
// For the global class this builds the table of MethodDescs an adds the rids
// to the MethodDef map.
//---------------------------------------------------------------------------
void Module::BuildClassForModule()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    IMDInternalImport * pImport = GetMDImport();
    DWORD           cFunctions, cFields;

    {
        // Obtain count of global functions
        HENUMInternalHolder hEnum(pImport);
        hEnum.EnumGlobalFunctionsInit();
        cFunctions = pImport->EnumGetCount(&hEnum);
    }

    {
        // Obtain count of global fields
        HENUMInternalHolder hEnum(pImport);
        hEnum.EnumGlobalFieldsInit();
        cFields = pImport->EnumGetCount(&hEnum);
    }

    // If we have any work to do...
    if (cFunctions > 0 || cFields > 0)
    {
        TypeKey typeKey(this, COR_GLOBAL_PARENT_TOKEN);
        TypeHandle typeHnd = GetClassLoader()->LoadTypeHandleForTypeKeyNoLock(&typeKey);
    }
}

#endif // !DACCESS_COMPILE

// Returns true iff the debugger should be notified about this module
//
// Notes:
//   Debugger doesn't need to be notified about modules that can't be executed,
//   like inspection and resource only. These are just pure data.
//
//   This should be immutable for an instance of a module. That ensures that the debugger gets consistent
//   notifications about it. It this value mutates, than the debugger may miss relevant notifications.
BOOL Module::IsVisibleToDebugger()
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    if (IsResource())
    {
        return FALSE;
    }

    return TRUE;
}

BOOL            Module::HasNativeOrReadyToRunImage()
{
#ifdef FEATURE_READYTORUN
    if (IsReadyToRun())
        return TRUE;
#endif

    return HasNativeImage();
}

PEImageLayout * Module::GetNativeOrReadyToRunImage()
{
    LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_READYTORUN
    if (IsReadyToRun())
        return GetReadyToRunInfo()->GetImage();
#endif

    return GetNativeImage();
}

PTR_CORCOMPILE_IMPORT_SECTION Module::GetImportSections(COUNT_T *pCount)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifdef FEATURE_PREJIT
    if (!IsReadyToRun())
        return GetNativeImage()->GetNativeImportSections(pCount);
#endif

    return GetReadyToRunInfo()->GetImportSections(pCount);
}

PTR_CORCOMPILE_IMPORT_SECTION Module::GetImportSectionFromIndex(COUNT_T index)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifdef FEATURE_PREJIT
    if (!IsReadyToRun())
        return GetNativeImage()->GetNativeImportSectionFromIndex(index);
#endif

    return GetReadyToRunInfo()->GetImportSectionFromIndex(index);
}

PTR_CORCOMPILE_IMPORT_SECTION Module::GetImportSectionForRVA(RVA rva)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifdef FEATURE_PREJIT
    if (!IsReadyToRun())
        return GetNativeImage()->GetNativeImportSectionForRVA(rva);
#endif

    return GetReadyToRunInfo()->GetImportSectionForRVA(rva);
}

TADDR Module::GetIL(DWORD target)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    if (target == 0)
        return NULL;

    return m_file->GetIL(target);
}

PTR_VOID Module::GetRvaField(DWORD rva, BOOL fZapped)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

#ifdef FEATURE_PREJIT
    if (fZapped && m_file->IsILOnly())
    {
        return dac_cast<PTR_VOID>(m_file->GetLoadedNative()->GetRvaData(rva,NULL_OK));
    }
#endif // FEATURE_PREJIT

    return m_file->GetRvaField(rva);
}

#ifndef DACCESS_COMPILE

CHECK Module::CheckRvaField(RVA field)
{
    WRAPPER_NO_CONTRACT;
    if (!IsReflection())
        CHECK(m_file->CheckRvaField(field));
    CHECK_OK;
}

CHECK Module::CheckRvaField(RVA field, COUNT_T size)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    if (!IsReflection())
        CHECK(m_file->CheckRvaField(field, size));
    CHECK_OK;
}

#endif // !DACCESS_COMPILE

BOOL Module::HasTls()
{
    WRAPPER_NO_CONTRACT;

    return m_file->HasTls();
}

BOOL Module::IsRvaFieldTls(DWORD rva)
{
    WRAPPER_NO_CONTRACT;

    return m_file->IsRvaFieldTls(rva);
}

UINT32 Module::GetFieldTlsOffset(DWORD rva)
{
    WRAPPER_NO_CONTRACT;

    return m_file->GetFieldTlsOffset(rva);
}

UINT32 Module::GetTlsIndex()
{
    WRAPPER_NO_CONTRACT;

    return m_file->GetTlsIndex();
}


// In DAC builds this function was being called on host addresses which may or may not
// have been marshalled from the target. Such addresses can't be reliably mapped back to
// target addresses, which means we can't tell whether they came from the IL or not
//
// Security note: Any security which you might wish to gain by verifying the origin of
// a signature isn't available in DAC. The attacker can provide a dump which spoofs all
// module ranges. In other words the attacker can make the signature appear to come from
// anywhere, but still violate all the rules that a signature from that location would
// otherwise follow. I am removing this function from DAC in order to prevent anyone from
// getting a false sense of security (in addition to its functional shortcomings)

#ifndef DACCESS_COMPILE
BOOL Module::IsSigInIL(PCCOR_SIGNATURE signature)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        FORBID_FAULT;
        MODE_ANY;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return m_file->IsPtrInILImage(signature);
}

#ifdef FEATURE_PREJIT
StubMethodHashTable *Module::GetStubMethodHashTable()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END

    if (m_pStubMethodHashTable == NULL && SystemDomain::GetCurrentDomain()->IsCompilationDomain())
    {
        // we only need to create the hash table when NGENing, it is read-only at run-time
        AllocMemTracker amTracker;
        m_pStubMethodHashTable = StubMethodHashTable::Create(GetLoaderAllocator(), this, METHOD_STUBS_HASH_BUCKETS, &amTracker);
        amTracker.SuppressRelease();
    }

    return m_pStubMethodHashTable;
}
#endif // FEATURE_PREJIT

void Module::InitializeStringData(DWORD token, EEStringData *pstrData, CQuickBytes *pqb)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(TypeFromToken(token) == mdtString);
    }
    CONTRACTL_END;

    BOOL fIs80Plus;
    DWORD dwCharCount;
    LPCWSTR pString;
    if (FAILED(GetMDImport()->GetUserString(token, &dwCharCount, &fIs80Plus, &pString)) ||
        (pString == NULL))
    {
        THROW_BAD_FORMAT(BFA_BAD_STRING_TOKEN_RANGE, this);
    }

#if !BIGENDIAN
    pstrData->SetStringBuffer(pString);
#else // !!BIGENDIAN
    _ASSERTE(pqb != NULL);

    LPWSTR pSwapped;

    pSwapped = (LPWSTR) pqb->AllocThrows(dwCharCount * sizeof(WCHAR));
    memcpy((void*)pSwapped, (void*)pString, dwCharCount*sizeof(WCHAR));
    SwapStringLength(pSwapped, dwCharCount);

    pstrData->SetStringBuffer(pSwapped);
#endif // !!BIGENDIAN

        // MD and String look at this bit in opposite ways.  Here's where we'll do the conversion.
        // MD sets the bit to true if the string contains characters greater than 80.
        // String sets the bit to true if the string doesn't contain characters greater than 80.

    pstrData->SetCharCount(dwCharCount);
    pstrData->SetIsOnlyLowChars(!fIs80Plus);
}

#ifndef CROSSGEN_COMPILE

#ifdef FEATURE_PREJIT
OBJECTHANDLE Module::ResolveStringRefHelper(DWORD token, BaseDomain *pDomain, PTR_CORCOMPILE_IMPORT_SECTION pSection, EEStringData *pStrData)
{
    PEImageLayout *pNativeImage = GetNativeImage();

    // Get the table
    COUNT_T tableSize;
    TADDR tableBase = pNativeImage->GetDirectoryData(&pSection->Section, &tableSize);

    // Walk the handle table.
    // @TODO: If we ever care about the perf of this function, we could sort the tokens
    // using as a key the string they point to, so we could do a binary search
    for (SIZE_T * pEntry = (SIZE_T *)tableBase ; pEntry < (SIZE_T *)(tableBase + tableSize); pEntry++)
    {
        // Ensure that the compiler won't fetch the value twice
        SIZE_T entry = VolatileLoadWithoutBarrier(pEntry);

        if (CORCOMPILE_IS_POINTER_TAGGED(entry))
        {
            BYTE * pBlob = (BYTE *) pNativeImage->GetRvaData(CORCOMPILE_UNTAG_TOKEN(entry));

            // Note that we only care about strings from current module, and so we do not check ENCODE_MODULE_OVERRIDE
            if (*pBlob++ == ENCODE_STRING_HANDLE &&
                    TokenFromRid(CorSigUncompressData((PCCOR_SIGNATURE&) pBlob), mdtString) ==  token)
            {
                // This string hasn't been fixed up. Synchronize the update with the normal
                // fixup logic
                {
                    CrstHolder ch(this->GetFixupCrst());

                    if (!CORCOMPILE_IS_POINTER_TAGGED(*pEntry))
                    {
                        // We lost the race, just return current entry
                    }
                    else
                    {
                        *pEntry = (SIZE_T) ResolveStringRef(token, pDomain, false);
                    }
                }

                return (OBJECTHANDLE) *pEntry;
            }
        }
        else
        {
            OBJECTREF* pRef = (OBJECTREF*) entry;
            _ASSERTE((*pRef)->GetMethodTable() == g_pStringClass);

            STRINGREF stringRef = (STRINGREF) *pRef;

            // Is this the string we are trying to resolve?
            if (pStrData->GetCharCount() == stringRef->GetStringLength() &&
                memcmp((void*)pStrData->GetStringBuffer(),
                        (void*) stringRef->GetBuffer(),
                        pStrData->GetCharCount()*sizeof(WCHAR)) == 0)
            {
                // We found it, so we just have to return this instance
                return (OBJECTHANDLE) entry;
            }
        }
    }
    return NULL;
}
#endif // FEATURE_PREJIT

OBJECTHANDLE Module::ResolveStringRef(DWORD token, BaseDomain *pDomain, bool bNeedToSyncWithFixups)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(TypeFromToken(token) == mdtString);
    }
    CONTRACTL_END;

    EEStringData strData;
    OBJECTHANDLE string = NULL;

#if !BIGENDIAN
    InitializeStringData(token, &strData, NULL);
#else // !!BIGENDIAN
    CQuickBytes qb;
    InitializeStringData(token, &strData, &qb);
#endif // !!BIGENDIAN

    GCX_COOP();

    // We can only do this for native images as they guarantee that resolvestringref will be
    // called only once per string from this module. @TODO: We really dont have any way of asserting
    // this, which would be nice... (and is needed to guarantee correctness)
#ifdef FEATURE_PREJIT
    if (HasNativeImage() && IsNoStringInterning())
    {
        if (bNeedToSyncWithFixups)
        {
            // In an ngen image, it is possible that we get here but not be coming from a fixup,
            // (FixupNativeEntry case). In that unfortunate case (ngen partial images, dynamic methods,
            // lazy string inits) we will have to troll through the fixup list, and in the case the string is there,
            // reuse it, if it's  there but hasn't been fixed up, fix it up now, and in the case it isn't
            // there at all, then go to our old style string interning. Going through this code path is
            // guaranteed to be slow. If necessary, we can further optimize it by sorting the token table,
            // Another way of solving this would be having a token to string table (would require knowing
            // all our posible stings in the ngen case (this is possible by looking at the IL))

            PEImageLayout * pNativeImage = GetNativeImage();

            COUNT_T nSections;
            PTR_CORCOMPILE_IMPORT_SECTION pSections = pNativeImage->GetNativeImportSections(&nSections);

            for (COUNT_T iSection = 0; iSection < nSections; iSection++)
            {
                PTR_CORCOMPILE_IMPORT_SECTION pSection = pSections + iSection;

                if (pSection->Type != CORCOMPILE_IMPORT_TYPE_STRING_HANDLE)
                    continue;

                OBJECTHANDLE oh = ResolveStringRefHelper(token, pDomain, pSection, &strData);
                if (oh != NULL)
                    return oh;
            }

            // The string is not in our fixup list, so just intern it old style (using hashtable)
            goto INTERN_OLD_STYLE;

        }
        /* Unfortunately, this assert won't work in some cases of generics, consider the following scenario:

            1) Generic type in CoreLib.
            2) Instantiation of generic (1) (via valuetype) in another module
            3) other module now holds a copy of the code of the generic for that particular instantiation
               however, it is resolving the string literals against CoreLib, which breaks the invariant
               this assert was based on (no string fixups against other modules). In fact, with NoStringInterning,
               our behavior is not very intuitive.
        */
        /*
        _ASSERTE(pDomain == GetAssembly()->GetDomain() && "If your are doing ldstr for a string"
        "in another module, either the JIT is very smart or you have a bug, check INLINE_NO_CALLEE_LDSTR");

        */
        /*
        Dev10 804385 bugfix -
           We should be using appdomain that the string token lives in (GetAssembly->GetDomain())
           to allocate the System.String object instead of the appdomain that first uses the ldstr <token> (pDomain).

           Otherwise, it is possible to get into the situation that pDomain is unloaded but GetAssembly->GetDomain() is
           still kicking around. Anything else that is still using that string will now be pointing to an object
           that will be freed when the next GC happens.
        */
        pDomain = GetAssembly()->GetDomain();

        // The caller is going to update an ngen fixup entry. The fixup entry
        // is used to reference the string and to ensure that the string is
        // allocated only once. Hence, this operation needs to be done under a lock.
        _ASSERTE(GetFixupCrst()->OwnedByCurrentThread());

        // Allocate handle
        OBJECTREF* pRef = pDomain->AllocateObjRefPtrsInLargeTable(1);

        STRINGREF str = AllocateStringObject(&strData);
        SetObjectReference(pRef, str);

        #ifdef LOGGING
        int length = strData.GetCharCount();
        length = min(length, 100);
        WCHAR *szString = (WCHAR *)_alloca((length + 1) * sizeof(WCHAR));
        memcpyNoGCRefs((void*)szString, (void*)strData.GetStringBuffer(), length * sizeof(WCHAR));
        szString[length] = '\0';
        LOG((LF_APPDOMAIN, LL_INFO10000, "String literal \"%S\" won't be interned due to NoInterningAttribute\n", szString));
        #endif // LOGGING

        return (OBJECTHANDLE) pRef;
    }


INTERN_OLD_STYLE:
#endif
    // Retrieve the string from the either the appropriate LoaderAllocator
    LoaderAllocator *pLoaderAllocator;

    if (this->IsCollectible())
        pLoaderAllocator = this->GetLoaderAllocator();
    else
        pLoaderAllocator = pDomain->GetLoaderAllocator();

    string = (OBJECTHANDLE)pLoaderAllocator->GetStringObjRefPtrFromUnicodeString(&strData);

    return string;
}
#endif // CROSSGEN_COMPILE

//
// Used by the verifier.  Returns whether this stringref is valid.
//
CHECK Module::CheckStringRef(DWORD token)
{
    LIMITED_METHOD_CONTRACT;
    CHECK(TypeFromToken(token)==mdtString);
    CHECK(!IsNilToken(token));
    CHECK(GetMDImport()->IsValidToken(token));
    CHECK_OK;
}

mdToken Module::GetEntryPointToken()
{
    WRAPPER_NO_CONTRACT;

    return m_file->GetEntryPointToken();
}

BYTE *Module::GetProfilerBase()
{
    CONTRACT(BYTE*)
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACT_END;

    if (m_file == NULL)  // I'd rather assert this is not the case...
    {
        RETURN NULL;
    }
    else if (HasNativeImage())
    {
        RETURN (BYTE*)(GetNativeImage()->GetBase());
    }
    else if (m_file->IsLoaded())
    {
        RETURN  (BYTE*)(m_file->GetLoadedIL()->GetBase());
    }
    else
    {
        RETURN NULL;
    }
}

void Module::AddActiveDependency(Module *pModule, BOOL unconditional)
{
    CONTRACT_VOID
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(pModule));
        PRECONDITION(pModule != this);
        PRECONDITION(!IsSystem());
        // Postcondition about activation
    }
    CONTRACT_END;

    pModule->EnsureActive();
    RETURN;
}

#endif //!DACCESS_COMPILE

#if !defined(DACCESS_COMPILE) && defined(FEATURE_PREJIT)
// This function, given an AssemblyRef into the ngen generated native metadata section, will find the assembly referenced if
// 1. The Assembly is defined with a different name than the AssemblyRef provides
// 2. The Assembly has reached the stage of being loaded.
// This function is used as a helper function to assist GetAssemblyIfLoaded with its tasks in the conditions
// where GetAssemblyIfLoaded must succeed (or we violate various invariants in the system required for
// correct implementation of GC, Stackwalking, and generic type loading.
Assembly * Module::GetAssemblyIfLoadedFromNativeAssemblyRefWithRefDefMismatch(mdAssemblyRef kAssemblyRef, BOOL *pfDiscoveredAssemblyRefMatchesTargetDefExactly)
{
    CONTRACT(Assembly *)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    _ASSERTE(HasNativeImage());

    Assembly *pAssembly = NULL;
    IMDInternalImport *pImportFoundNativeImage = this->GetNativeAssemblyImport(FALSE);

    if (!pImportFoundNativeImage)
    {
        RETURN NULL;
    }

    if (kAssemblyRef != mdAssemblyRefNil)
    {
        // Scan CORCOMPILE_DEPENDENCIES tables
        PEImageLayout* pNativeLayout = this->GetNativeImage();
        COUNT_T dependencyCount;
        CORCOMPILE_DEPENDENCY *pDependencies = pNativeLayout->GetNativeDependencies(&dependencyCount);

        // Find the assemblyDef that defines the exact target
        mdAssemblyRef foundAssemblyDef = mdAssemblyRefNil;

        for (COUNT_T i = 0; i < dependencyCount; ++i)
        {
            CORCOMPILE_DEPENDENCY* pDependency = &(pDependencies[i]);
            if (pDependency->dwAssemblyRef == kAssemblyRef)
            {
                foundAssemblyDef = pDependency->dwAssemblyDef;
                break;
            }
        }

        // In this case we know there is no assembly redirection involved. Skip any additional work.
        if (kAssemblyRef == foundAssemblyDef)
        {
            *pfDiscoveredAssemblyRefMatchesTargetDefExactly = true;
            RETURN NULL;
        }

        if (foundAssemblyDef != mdAssemblyRefNil)
        {
            // Find out if THIS reference is satisfied
            // Specify fDoNotUtilizeExtraChecks to prevent recursion
            Assembly *pAssemblyCandidate = this->GetAssemblyIfLoaded(foundAssemblyDef, pImportFoundNativeImage, TRUE /*fDoNotUtilizeExtraChecks*/);

            // This extended check is designed only to find assemblies loaded via an AssemblySpecBindingCache based binder. Verify that's what we found.
            if(pAssemblyCandidate != NULL)
            {
                if (!pAssemblyCandidate->GetManifestFile()->HasHostAssembly())
                {
                    pAssembly = pAssemblyCandidate;
                }
                else
                {
                    // This should only happen in the generic instantiation case when multiple threads are racing and
                    // the assembly found is one which we will determine is the wrong assembly.
                    //
                    // We can't assert that (as its possible under stress); however it shouldn't happen in the stack walk or GC case, so we assert in those cases.
                    _ASSERTE("Non-AssemblySpecBindingCache based assembly found with extended search" && !(IsStackWalkerThread() || IsGCThread()) && IsGenericInstantiationLookupCompareThread());
                }
            }
        }
    }

    RETURN pAssembly;
}
#endif // !defined(DACCESS_COMPILE) && defined(FEATURE_PREJIT)

Assembly *
Module::GetAssemblyIfLoaded(
    mdAssemblyRef       kAssemblyRef,
    IMDInternalImport * pMDImportOverride,  // = NULL
    BOOL                fDoNotUtilizeExtraChecks, // = FALSE
    ICLRPrivBinder      *pBindingContextForLoadedAssembly // = NULL
)
{
    CONTRACT(Assembly *)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    Assembly * pAssembly = NULL;
    BOOL fCanUseRidMap = pMDImportOverride == NULL;

#ifdef _DEBUG
    fCanUseRidMap = fCanUseRidMap && (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_GetAssemblyIfLoadedIgnoreRidMap) == 0);
#endif

    // If we're here due to a generic instantiation, then we should only be querying information from the ngen image we're finding the generic instantiation in.
#if !defined(DACCESS_COMPILE) && defined(FEATURE_PREJIT)
    _ASSERTE(!IsGenericInstantiationLookupCompareThread() || HasNativeImage());
#endif

    // Don't do a lookup if an override IMDInternalImport is provided, since the lookup is for the
    // standard IMDInternalImport and might result in an incorrect result.
    if (fCanUseRidMap)
    {
        pAssembly = LookupAssemblyRef(kAssemblyRef);
    }

#ifndef DACCESS_COMPILE
    // Check if actually loaded, unless a GC is in progress or the current thread is
    // walking the stack (either its own stack, or another thread's stack) as that works
    // only with loaded assemblies
    //
    // NOTE: The case where the current thread is walking a stack can be problematic for
    // other reasons, as the remaining code of this function uses "GetAppDomain()", when
    // in fact the right AppDomain to use is the one corresponding to the frame being
    // traversed on the walked thread. Dev10 TFS bug# 762348 tracks that issue.
    if ((pAssembly != NULL) && !IsGCThread() && !IsStackWalkerThread())
    {
        _ASSERTE(::GetAppDomain() != NULL);
        DomainAssembly * pDomainAssembly = pAssembly->GetDomainAssembly();
        if ((pDomainAssembly == NULL) || !pDomainAssembly->IsLoaded())
            pAssembly = NULL;
    }
#endif //!DACCESS_COMPILE

    if (pAssembly == NULL)
    {
        do
        {
            AppDomain * pAppDomainExamine = AppDomain::GetCurrentDomain();

            DomainAssembly * pCurAssemblyInExamineDomain = GetAssembly()->GetDomainAssembly();
            if (pCurAssemblyInExamineDomain == NULL)
            {
                continue;
            }

#ifndef DACCESS_COMPILE
            {
                IMDInternalImport * pMDImport = (pMDImportOverride == NULL) ? (GetMDImport()) : (pMDImportOverride);

                //we have to be very careful here.
                //we are using InitializeSpecInternal so we need to make sure that under no condition
                //the data we pass to it can outlive the assembly spec.
                AssemblySpec spec;
                if (FAILED(spec.InitializeSpecInternal(kAssemblyRef,
                                                       pMDImport,
                                                       pCurAssemblyInExamineDomain,
                                                       FALSE /*fAllowAllocation*/)))
                {
                    continue;
                }

                // If we have been passed the binding context for the loaded assembly that is being looked up in the
                // cache, then set it up in the AssemblySpec for the cache lookup to use it below.
                if (pBindingContextForLoadedAssembly != NULL)
                {
                    _ASSERTE(spec.GetBindingContext() == NULL);
                    spec.SetBindingContext(pBindingContextForLoadedAssembly);
                }
                DomainAssembly * pDomainAssembly = nullptr;

                {
                    pDomainAssembly = pAppDomainExamine->FindCachedAssembly(&spec, FALSE /*fThrow*/);
                }

                if (pDomainAssembly && pDomainAssembly->IsLoaded())
                    pAssembly = pDomainAssembly->GetCurrentAssembly(); // <NOTE> Do not use GetAssembly - that may force the completion of a load

                // Only store in the rid map if working with the current AppDomain.
                if (fCanUseRidMap && pAssembly)
                    StoreAssemblyRef(kAssemblyRef, pAssembly);

                if (pAssembly != NULL)
                    break;
            }
#endif //!DACCESS_COMPILE
        } while (false);
    }

#if !defined(DACCESS_COMPILE) && defined(FEATURE_PREJIT)
    if (pAssembly == NULL && (IsStackWalkerThread() || IsGCThread() || IsGenericInstantiationLookupCompareThread()) && !fDoNotUtilizeExtraChecks)
    {
        // The GetAssemblyIfLoaded function must succeed in finding assemblies which have already been loaded in a series of interesting cases
        // (GC, Stackwalking, GenericInstantiationLookup). This logic is used to handle cases where the normal lookup done above
        // may fail, and more extensive (and slow) lookups are necessary. This logic is gated by a long series of checks to ensure it doesn't
        // run in cases which are not known to be problematic, or would not benefit from the logic here.
        //
        // This is logic which tries extra possibilities to find an assembly. It is believed this logic can only be hit in cases where an ngen
        // image depends on an assembly through some sort of binding version/public key token adjustment (due to binding policy, unification, or portability rules)
        // and the assembly depended on was loaded through a binder that utilizes the AssemblySpecBindingCache for binder caching. (The cache's in the other
        // binder's successfully answer the GetAssemblyIfLoaded question in the case of non-exact matches where the match was discovered during
        // ngen resolution.)
        // This restricts the scenario to a somewhat restricted case.

        BOOL eligibleForAdditionalChecks = TRUE;

        AssemblySpec specSearchAssemblyRef;

        // Get the assembly ref information that we are attempting to satisfy.
        if (eligibleForAdditionalChecks)
        {
            IMDInternalImport * pMDImport = (pMDImportOverride == NULL) ? (GetMDImport()) : (pMDImportOverride);

            if (FAILED(specSearchAssemblyRef.InitializeSpecInternal(kAssemblyRef,
                                                    pMDImport,
                                                    NULL,
                                                    FALSE /*fAllowAllocation*/)))
            {
                eligibleForAdditionalChecks = FALSE; // If an assemblySpec can't be constructed then we're not going to succeed
                                                     // This should not ever happen, due to the above checks, but this logic
                                                     // is intended to be defensive against unexpected behavior.
            }
        }

        if (eligibleForAdditionalChecks)
        {
            BOOL abortAdditionalChecks = false;

            // When working with an ngenn'd assembly, as an optimization we can scan only that module for dependency info.
            bool onlyScanCurrentModule = HasNativeImage() && GetFile()->IsAssembly();
            mdAssemblyRef foundAssemblyRef = mdAssemblyRefNil;

            // In each AppDomain that might be interesting, scan for an ngen image that is loaded that has a dependency on the same
            // assembly that is now being looked up. If that ngen image has the same dependency, then we can use the CORCOMPILE_DEPENDENCIES
            // table to find the exact AssemblyDef that defines the assembly, and attempt a load based on that information.
            // As this logic is expected to be used only in exceedingly rare situations, this code has not been tuned for performance
            // in any way.
            do
            {
                AppDomain * pAppDomainExamine = ::GetAppDomain(); // There is only 1 AppDomain on CoreCLR

                DomainAssembly * pCurAssemblyInExamineDomain = GetAssembly()->GetDomainAssembly();
                if (pCurAssemblyInExamineDomain == NULL)
                {
                    continue;
                }

                DomainFile *pDomainFileNativeImage;

                if (onlyScanCurrentModule)
                {
                    pDomainFileNativeImage = pCurAssemblyInExamineDomain;
                    // Do not reset foundAssemblyRef.
                    // This will allow us to avoid scanning for foundAssemblyRef in each domain we iterate through
                }
                else
                {
                    foundAssemblyRef = mdAssemblyRefNil;
                    pDomainFileNativeImage = pAppDomainExamine->GetDomainFilesWithNativeImagesList();
                }

                while (!abortAdditionalChecks && (pDomainFileNativeImage != NULL) && (pAssembly == NULL))
                {
                    Module *pNativeImageModule = pDomainFileNativeImage->GetCurrentModule();
                    _ASSERTE(pNativeImageModule->HasNativeImage());
                    IMDInternalImport *pImportFoundNativeImage = pNativeImageModule->GetNativeAssemblyImport(FALSE);
                    if (pImportFoundNativeImage != NULL)
                    {
                        if (IsNilToken(foundAssemblyRef))
                        {
                            // Enumerate assembly refs in nmd space, and compare against held ref.
                            HENUMInternalHolder hAssemblyRefEnum(pImportFoundNativeImage);
                            if (FAILED(hAssemblyRefEnum.EnumInitNoThrow(mdtAssemblyRef, mdAssemblyRefNil)))
                            {
                                continue;
                            }

                            mdAssemblyRef assemblyRef = mdAssemblyRefNil;

                            // Find if the native image has a matching assembly ref in its compile dependencies.
                            while (pImportFoundNativeImage->EnumNext(&hAssemblyRefEnum, &assemblyRef) && (pAssembly == NULL))
                            {
                                AssemblySpec specFoundAssemblyRef;
                                if (FAILED(specFoundAssemblyRef.InitializeSpecInternal(assemblyRef,
                                                                        pImportFoundNativeImage,
                                                                        NULL,
                                                                        FALSE /*fAllowAllocation*/)))
                                {
                                    continue; // If the spec cannot be loaded, it isn't the one we're looking for
                                }

                                // Check for AssemblyRef equality
                                if (specSearchAssemblyRef.CompareEx(&specFoundAssemblyRef))
                                {
                                    foundAssemblyRef = assemblyRef;
                                    break;
                                }
                            }
                        }

                        pAssembly = pNativeImageModule->GetAssemblyIfLoadedFromNativeAssemblyRefWithRefDefMismatch(foundAssemblyRef, &abortAdditionalChecks);

                        if (fCanUseRidMap && pAssembly)
                            StoreAssemblyRef(kAssemblyRef, pAssembly);
                    }

                    // If we're only scanning one module for accurate dependency information, break the loop here.
                    if (onlyScanCurrentModule)
                        break;

                    pDomainFileNativeImage = pDomainFileNativeImage->FindNextDomainFileWithNativeImage();
                }
            } while (false);
        }
    }
#endif // !defined(DACCESS_COMPILE) && defined(FEATURE_PREJIT)

    // When walking the stack or computing GC information this function should never fail.
    _ASSERTE((pAssembly != NULL) || !(IsStackWalkerThread() || IsGCThread()));

#ifdef DACCESS_COMPILE

    // Note: In rare cases when debugger walks the stack, we could actually have pAssembly=NULL here.
    // To fix that we should DACize the AppDomain-iteration code above (especially AssemblySpec).
    _ASSERTE(pAssembly != NULL);

#endif //DACCESS_COMPILE

    RETURN pAssembly;
} // Module::GetAssemblyIfLoaded

DWORD
Module::GetAssemblyRefFlags(
    mdAssemblyRef tkAssemblyRef)
{
    CONTRACTL
    {
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(TypeFromToken(tkAssemblyRef) == mdtAssemblyRef);

    LPCSTR      pszAssemblyName;
    const void *pbPublicKeyOrToken;
    DWORD cbPublicKeyOrToken;

    DWORD dwAssemblyRefFlags;
    IfFailThrow(GetMDImport()->GetAssemblyRefProps(
            tkAssemblyRef,
            &pbPublicKeyOrToken,
            &cbPublicKeyOrToken,
            &pszAssemblyName,
            NULL,
            NULL,
            NULL,
            &dwAssemblyRefFlags));

    return dwAssemblyRefFlags;
} // Module::GetAssemblyRefFlags

#ifndef DACCESS_COMPILE
DomainAssembly * Module::LoadAssembly(mdAssemblyRef kAssemblyRef)
{
    CONTRACT(DomainAssembly *)
    {
        INSTANCE_CHECK;
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM();); }
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_NOT_OK));
    }
    CONTRACT_END;

    ETWOnStartup (LoaderCatchCall_V1, LoaderCatchCallEnd_V1);

    DomainAssembly * pDomainAssembly;

    //
    // Early out quickly if the result is cached
    //
    Assembly * pAssembly = LookupAssemblyRef(kAssemblyRef);
    if (pAssembly != NULL)
    {
        pDomainAssembly = pAssembly->GetDomainAssembly();
        ::GetAppDomain()->LoadDomainFile(pDomainAssembly, FILE_LOADED);

        RETURN pDomainAssembly;
    }

    {
        PEAssemblyHolder pFile = GetDomainAssembly()->GetFile()->LoadAssembly(
                kAssemblyRef,
                NULL);
        AssemblySpec spec;
        spec.InitializeSpec(kAssemblyRef, GetMDImport(), GetDomainAssembly());
        // Set the binding context in the AssemblySpec if one is available. This can happen if the LoadAssembly ended up
        // invoking the custom AssemblyLoadContext implementation that returned a reference to an assembly bound to a different
        // AssemblyLoadContext implementation.
        ICLRPrivBinder *pBindingContext = pFile->GetBindingContext();
        if (pBindingContext != NULL)
        {
            spec.SetBindingContext(pBindingContext);
        }
        pDomainAssembly = GetAppDomain()->LoadDomainAssembly(&spec, pFile, FILE_LOADED);
    }

    if (pDomainAssembly != NULL)
    {
        _ASSERTE(
            pDomainAssembly->IsSystem() ||                  // GetAssemblyIfLoaded will not find CoreLib (see AppDomain::FindCachedFile)
            !pDomainAssembly->IsLoaded() ||                 // GetAssemblyIfLoaded will not find not-yet-loaded assemblies
            GetAssemblyIfLoaded(kAssemblyRef, NULL, FALSE, pDomainAssembly->GetFile()->GetHostAssembly()) != NULL);     // GetAssemblyIfLoaded should find all remaining cases

        if (pDomainAssembly->GetCurrentAssembly() != NULL)
        {
            StoreAssemblyRef(kAssemblyRef, pDomainAssembly->GetCurrentAssembly());
        }
    }

    RETURN pDomainAssembly;
}

#endif // !DACCESS_COMPILE

Module *Module::GetModuleIfLoaded(mdFile kFile, BOOL onlyLoadedInAppDomain, BOOL permitResources)
{
    CONTRACT(Module *)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(TypeFromToken(kFile) == mdtFile
                     || TypeFromToken(kFile) == mdtModuleRef);
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

    // Handle the module ref case
    if (TypeFromToken(kFile) == mdtModuleRef)
    {
        LPCSTR moduleName;
        if (FAILED(GetMDImport()->GetModuleRefProps(kFile, &moduleName)))
        {
            RETURN NULL;
        }

        // This is required only because of some lower casing on the name
        kFile = GetAssembly()->GetManifestFileToken(moduleName);
        if (kFile == mdTokenNil)
            RETURN NULL;

        RETURN GetAssembly()->GetManifestModule()->GetModuleIfLoaded(kFile, onlyLoadedInAppDomain, permitResources);
    }

    Module *pModule = LookupFile(kFile);
    if (pModule == NULL)
    {
        if (IsManifest())
        {
            if (kFile == mdFileNil)
                pModule = GetAssembly()->GetManifestModule();
        }
        else
        {
            // If we didn't find it there, look at the "master rid map" in the manifest file
            Assembly *pAssembly = GetAssembly();
            mdFile kMatch;

            // This is required only because of some lower casing on the name
            kMatch = pAssembly->GetManifestFileToken(GetMDImport(), kFile);
            if (IsNilToken(kMatch))
            {
                if (kMatch == mdFileNil)
                {
                    pModule = pAssembly->GetManifestModule();
                }
                else
                {
                    RETURN NULL;
                }
            }
            else
            pModule = pAssembly->GetManifestModule()->LookupFile(kMatch);
        }

#ifndef DACCESS_COMPILE
        if (pModule != NULL)
            StoreFileNoThrow(kFile, pModule);
#endif
    }

    // We may not want to return a resource module
    if (!permitResources && pModule && pModule->IsResource())
        pModule = NULL;

#ifndef DACCESS_COMPILE
#endif // !DACCESS_COMPILE
    RETURN pModule;
}

#ifndef DACCESS_COMPILE

DomainFile *Module::LoadModule(AppDomain *pDomain, mdFile kFile,
                               BOOL permitResources/*=TRUE*/, BOOL bindOnly/*=FALSE*/)
{
    CONTRACT(DomainFile *)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(TypeFromToken(kFile) == mdtFile
                     || TypeFromToken(kFile) == mdtModuleRef);
        POSTCONDITION(CheckPointer(RETVAL, !permitResources || bindOnly ? NULL_OK : NULL_NOT_OK));
    }
    CONTRACT_END;

    if (bindOnly)
    {
        RETURN  NULL;
    }
    else
    {
        LPCSTR psModuleName=NULL;
        if (TypeFromToken(kFile) == mdtModuleRef)
        {
            // This is a moduleRef
            IfFailThrow(GetMDImport()->GetModuleRefProps(kFile, &psModuleName));
        }
        else
        {
           // This is mdtFile
           IfFailThrow(GetAssembly()->GetManifestImport()->GetFileProps(kFile,
                                      &psModuleName,
                                      NULL,
                                      NULL,
                                      NULL));
        }
        SString name(SString::Utf8, psModuleName);
        EEFileLoadException::Throw(name, COR_E_MULTIMODULEASSEMBLIESDIALLOWED, NULL);
    }
}
#endif // !DACCESS_COMPILE

PTR_Module Module::LookupModule(mdToken kFile,BOOL permitResources/*=TRUE*/)
{
    CONTRACT(PTR_Module)
    {
        INSTANCE_CHECK;
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT;
        else { INJECT_FAULT(COMPlusThrowOM()); }
        MODE_ANY;
        PRECONDITION(TypeFromToken(kFile) == mdtFile
                     || TypeFromToken(kFile) == mdtModuleRef);
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    if (TypeFromToken(kFile) == mdtModuleRef)
    {
        LPCSTR moduleName;
        IfFailThrow(GetMDImport()->GetModuleRefProps(kFile, &moduleName));
        mdFile kFileLocal = GetAssembly()->GetManifestFileToken(moduleName);

        if (kFileLocal == mdTokenNil)
            COMPlusThrowHR(COR_E_BADIMAGEFORMAT);

        RETURN GetAssembly()->GetManifestModule()->LookupModule(kFileLocal, permitResources);
    }

    PTR_Module pModule = LookupFile(kFile);
    if (pModule == NULL && !IsManifest())
    {
        // If we didn't find it there, look at the "master rid map" in the manifest file
        Assembly *pAssembly = GetAssembly();
        mdFile kMatch = pAssembly->GetManifestFileToken(GetMDImport(), kFile);
        if (IsNilToken(kMatch)) {
            if (kMatch == mdFileNil)
                pModule = pAssembly->GetManifestModule();
            else
            COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
        }
        else
            pModule = pAssembly->GetManifestModule()->LookupFile(kMatch);
    }
    RETURN pModule;
}


TypeHandle Module::LookupTypeRef(mdTypeRef token)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    SUPPORTS_DAC;

    _ASSERTE(TypeFromToken(token) == mdtTypeRef);

    g_IBCLogger.LogRidMapAccess( MakePair( this, token ) );

    TypeHandle entry = TypeHandle::FromTAddr(dac_cast<TADDR>(m_TypeRefToMethodTableMap.GetElement(RidFromToken(token))));

    if (entry.IsNull())
        return TypeHandle();

    // Cannot do this in a NOTHROW function.
    // Note that this could be called while doing GC from the prestub of
    // a method to resolve typerefs in a signature. We cannot THROW
    // during GC.

    // @PERF: Enable this so that we do not need to touch metadata
    // to resolve typerefs

#ifdef FIXUPS_ALL_TYPEREFS

    if (CORCOMPILE_IS_POINTER_TAGGED((SIZE_T) entry.AsPtr()))
    {
#ifndef DACCESS_COMPILE
        Module::RestoreTypeHandlePointer(&entry, TRUE);
        m_TypeRefToMethodTableMap.SetElement(RidFromToken(token), dac_cast<PTR_TypeRef>(value.AsTAddr()));
#else // DACCESS_COMPILE
        DacNotImpl();
#endif // DACCESS_COMPILE
    }

#endif // FIXUPS_ALL_TYPEREFS

    return entry;
}

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
mdTypeRef Module::LookupTypeRefByMethodTable(MethodTable *pMT)
{
    STANDARD_VM_CONTRACT;

    HENUMInternalHolder hEnumTypeRefs(GetMDImport());
    mdTypeRef token;
    hEnumTypeRefs.EnumAllInit(mdtTypeRef);
    while (hEnumTypeRefs.EnumNext(&token))
    {
        TypeHandle thRef = LookupTypeRef(token);
        if (thRef.IsNull() || thRef.IsTypeDesc())
        {
            continue;
        }

        MethodTable *pMTRef = thRef.AsMethodTable();
        if (pMT->HasSameTypeDefAs(pMTRef))
        {
            _ASSERTE(pMTRef->IsTypicalTypeDefinition());
            return token;
        }
    }

#ifdef FEATURE_READYTORUN_COMPILER
    if (IsReadyToRunCompilation())
    {
        if (pMT->GetClass()->IsEquivalentType())
        {
            if (g_CorCompileVerboseLevel >= CORCOMPILE_VERBOSE)
                GetSvcLogger()->Log(W("ReadyToRun: Type reference to equivalent type cannot be encoded\n"));
            ThrowHR(E_NOTIMPL);
        }

        // FUTURE: Encoding of new cross-module references for ReadyToRun
        // This warning is hit for recursive cross-module inlining. It is commented out to avoid noise.
        // if (g_CorCompileVerboseLevel >= CORCOMPILE_VERBOSE)
        //     GetSvcLogger()->Log(W("ReadyToRun: Type reference outside of current version bubble cannot be encoded\n"));
        ThrowHR(E_NOTIMPL);
    }
    else
#endif // FEATURE_READYTORUN_COMPILER
    {
        // FUTURE TODO: Version resilience
        _ASSERTE(!"Cross module type reference not found");
    }
    ThrowHR(E_FAIL);
}

mdMemberRef Module::LookupMemberRefByMethodDesc(MethodDesc *pMD)
{
    STANDARD_VM_CONTRACT;

    HENUMInternalHolder hEnumMemberRefs(GetMDImport());
    mdMemberRef token;
    hEnumMemberRefs.EnumAllInit(mdtMemberRef);
    while (hEnumMemberRefs.EnumNext(&token))
    {
        BOOL fIsMethod = FALSE;
        TADDR addr = LookupMemberRef(token, &fIsMethod);
        if (fIsMethod)
        {
            MethodDesc *pCurMD = dac_cast<PTR_MethodDesc>(addr);
            if (pCurMD == pMD)
            {
                return token;
            }
        }
    }

    // FUTURE TODO: Version resilience
    _ASSERTE(!"Cross module method reference not found");
    ThrowHR(E_FAIL);
}
#endif // FEATURE_NATIVE_IMAGE_GENERATION

#ifndef DACCESS_COMPILE

//
// Increase the size of one of the maps, such that it can handle a RID of at least "rid".
//
// This function must also check that another thread didn't already add a LookupMap capable
// of containing the same RID.
//
PTR_TADDR LookupMapBase::GrowMap(Module * pModule, DWORD rid)
{
    CONTRACT(PTR_TADDR)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(ThrowOutOfMemory(););
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    LookupMapBase *pMap = this;
    LookupMapBase *pPrev = NULL;
    LookupMapBase *pNewMap = NULL;

    // Initial block size
    DWORD dwIndex = rid;
    DWORD dwBlockSize = 16;

    {
        CrstHolder ch(pModule->GetLookupTableCrst());
        // Check whether we can already handle this RID index
        do
        {
            if (dwIndex < pMap->dwCount)
            {
                // Already there - some other thread must have added it
                RETURN pMap->GetIndexPtr(dwIndex);
            }

            dwBlockSize *= 2;

            dwIndex -= pMap->dwCount;

            pPrev = pMap;
            pMap = pMap->pNext;
        } while (pMap != NULL);

        _ASSERTE(pPrev != NULL); // should never happen, because there's always at least one map

        DWORD dwSizeToAllocate = max(dwIndex + 1, dwBlockSize);

        pNewMap = (LookupMapBase *) (void*)pModule->GetLoaderAllocator()->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(LookupMapBase)) + S_SIZE_T(dwSizeToAllocate)*S_SIZE_T(sizeof(TADDR)));

        // Note: Memory allocated on loader heap is zero filled
        // memset(pNewMap, 0, sizeof(LookupMap) + dwSizeToAllocate*sizeof(void*));

        pNewMap->pNext          = NULL;
        pNewMap->dwCount        = dwSizeToAllocate;

        pNewMap->pTable         = dac_cast<ArrayDPTR(TADDR)>(pNewMap + 1);

        // Link ourselves in
        VolatileStore<LookupMapBase*>(&(pPrev->pNext), pNewMap);
    }

    RETURN pNewMap->GetIndexPtr(dwIndex);
}

#endif // DACCESS_COMPILE

PTR_TADDR LookupMapBase::GetElementPtr(DWORD rid)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    LookupMapBase * pMap = this;

#ifdef FEATURE_PREJIT
    if (pMap->dwNumHotItems > 0)
    {
#ifdef _DEBUG_IMPL
        static  DWORD counter = 0;
        counter++;
        if (counter >= pMap->dwNumHotItems)
        {
            CheckConsistentHotItemList();
            counter = 0;
        }
#endif // _DEBUG_IMPL

        PTR_TADDR pHotItemValue = pMap->FindHotItemValuePtr(rid);
        if (pHotItemValue)
        {
            return pHotItemValue;
        }
    }
#endif // FEATURE_PREJIT

    DWORD dwIndex = rid;
    do
    {
        if (dwIndex < pMap->dwCount)
        {
            return pMap->GetIndexPtr(dwIndex);
        }

        dwIndex -= pMap->dwCount;
        pMap = pMap->pNext;
    } while (pMap != NULL);

    return NULL;
}


#ifdef FEATURE_PREJIT

// This method can only be called on a compressed map (MapIsCompressed() == true). Compressed rid maps store
// the array of values as packed deltas (each value is based on the accumulated of all the previous entries).
// So this method takes the bit stream of compressed data we're navigating and the value of the last entry
// retrieved allowing us to calculate the full value of the next entry. Note that the values passed in and out
// here aren't the final values the top-level caller sees. In order to avoid having to touch the compressed
// data on image base relocations we actually store a form of RVA (though relative to the map base rather than
// the module base).
INT32 LookupMapBase::GetNextCompressedEntry(BitStreamReader *pTableStream, INT32 iLastValue)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
        PRECONDITION(MapIsCompressed());
    }
    CONTRACTL_END;

    // The next kLookupMapLengthBits bits in the stream are an index into a per-map table that tells us the
    // length of the encoded delta.
    DWORD dwValueLength = rgEncodingLengths[pTableStream->Read(kLookupMapLengthBits)];

    // Then follows a single bit that indicates whether the delta should be added (1) or subtracted (0) from
    // the previous entry value to recover the current entry value.
    // Once we've read that bit we read the delta (encoded as an unsigned integer using the number of bits
    // that we read from the encoding lengths table above).
    if (pTableStream->ReadOneFast())
        return iLastValue + (INT32)(pTableStream->Read(dwValueLength));
    else
        return iLastValue - (INT32)(pTableStream->Read(dwValueLength));
}

// This method can only be called on a compressed map (MapIsCompressed() == true). Retrieves the final value
// (e.g. MethodTable*, MethodDesc* etc. based on map type) given the rid of the entry.
TADDR LookupMapBase::GetValueFromCompressedMap(DWORD rid)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
        PRECONDITION(MapIsCompressed());
    }
    CONTRACTL_END;

    // Normally to extract the nth entry in the table we have to linearly parse all (n - 1) preceding entries
    // (since entries are stored as the delta from the previous entry). Obviously this can yield exceptionally
    // poor performance for the later entries in large tables. So we also build an index of the compressed
    // stream. This index has an entry for every kLookupMapIndexStride entries in the compressed table. Each
    // index entry contains the full RVA (relative to the map) of the corresponding table entry plus the bit
    // offset in the stream from which to start parsing the next entry's data.
    // In this fashion we can get to within kLookupMapIndexStride entries of our target entry and then decode
    // our way to the final target.

    // Ensure that index does not go beyond end of the saved table
    if (rid >= dwCount)
        return 0;

    // Calculate the nearest entry in the index that is lower than our target index in the full table.
    DWORD dwIndexEntry = rid / kLookupMapIndexStride;

    // Then calculate how many additional entries we'll need to decode from the compressed streams to recover
    // the target entry.
    DWORD dwSubIndex = rid % kLookupMapIndexStride;

    // Open a bit stream reader on the index and skip all the entries prior to the one we're interested in.
    BitStreamReader sIndexStream(pIndex);
    sIndexStream.Skip(dwIndexEntry * cIndexEntryBits);

    // The first kBitsPerRVA of the index entry contain the RVA of the corresponding entry in the compressed
    // table. If this is exactly the entry we want (dwSubIndex == 0) then we can use this RVA to recover the
    // value the caller wants. Our RVAs are based on the map address rather than the module base (simply
    // because we don't record the module base in LookupMapBase). A delta of zero encodes a null value,
    // otherwise we simply add the RVA to the our map address to recover the full pointer.
    // Note that most LookupMaps are embedded structures (in Module) so we can't directly dac_cast<TADDR> our
    // "this" pointer for DAC builds. Instead we have to use the slightly slower (in DAC) but more flexible
    // PTR_HOST_INT_TO_TADDR() which copes with interior host pointers.
    INT32 iValue = (INT32)sIndexStream.Read(kBitsPerRVA);
    if (dwSubIndex == 0)
        return iValue ? PTR_HOST_INT_TO_TADDR(this) + iValue : 0;

    // Otherwise we must parse one or more entries in the compressed table to accumulate more deltas to the
    // base RVA we read above. The remaining portion of the index entry has the bit offset into the compressed
    // table at which to begin parsing.
    BitStreamReader sTableStream(dac_cast<PTR_CBYTE>(pTable));
    sTableStream.Skip(sIndexStream.Read(cIndexEntryBits - kBitsPerRVA));

    // Parse all the entries up to our target entry. Each step takes the RVA from the previous cycle (or from
    // the index entry we read above) and applies the compressed delta of the next table entry to it.
    for (DWORD i = 0; i < dwSubIndex; i++)
        iValue = GetNextCompressedEntry(&sTableStream, iValue);

    // We have the final RVA so recover the actual pointer from it (a zero RVA encodes a NULL pointer). Note
    // the use of PTR_HOST_INT_TO_TADDR() rather than dac_cast<TADDR>, see previous comment on
    // PTR_HOST_INT_TO_TADDR for an explanation.
    return iValue ? PTR_HOST_INT_TO_TADDR(this) + iValue : 0;
}

PTR_TADDR LookupMapBase::FindHotItemValuePtr(DWORD rid)
{
    LIMITED_METHOD_DAC_CONTRACT;

    if  (dwNumHotItems < 5)
    {
        // do simple linear search if there are only a few hot items
        for (DWORD i = 0; i < dwNumHotItems; i++)
        {
            if (hotItemList[i].rid == rid)
                return dac_cast<PTR_TADDR>(
                    dac_cast<TADDR>(hotItemList) + i * sizeof(HotItem) + offsetof(HotItem, value));
        }
    }
    else
    {
        // otherwise do binary search
        if (hotItemList[0].rid <= rid && rid <= hotItemList[dwNumHotItems-1].rid)
        {
            DWORD l = 0;
            DWORD r = dwNumHotItems;
            while (l + 1 < r)
            {
                // loop invariant:
                _ASSERTE(hotItemList[l].rid <= rid && (r >= dwNumHotItems || rid < hotItemList[r].rid));

                DWORD m = (l + r)/2;
                // loop condition implies l < m < r, hence interval shrinks every iteration, hence loop terminates
                _ASSERTE(l < m && m < r);
                if (rid < hotItemList[m].rid)
                    r = m;
                else
                    l = m;
            }
            // now we know l + 1 == r && hotItemList[l].rid <= rid < hotItemList[r].rid
            // loop invariant:
            _ASSERTE(hotItemList[l].rid <= rid && (r >= dwNumHotItems || rid < hotItemList[r].rid));
            if (hotItemList[l].rid == rid)
                return dac_cast<PTR_TADDR>(
                    dac_cast<TADDR>(hotItemList) + l * sizeof(HotItem) + offsetof(HotItem, value));
        }
    }
    return NULL;
}

#ifdef _DEBUG
void LookupMapBase::CheckConsistentHotItemList()
{
    LIMITED_METHOD_DAC_CONTRACT;

    for (DWORD i = 0; i < dwNumHotItems; i++)
    {
        DWORD rid = hotItemList[i].rid;

        PTR_TADDR pHotValue = dac_cast<PTR_TADDR>(
            dac_cast<TADDR>(hotItemList) + i * sizeof(HotItem) + offsetof(HotItem, value));
        TADDR hotValue = RelativePointer<TADDR>::GetValueMaybeNullAtPtr(dac_cast<TADDR>(pHotValue));

        TADDR value;
        if (MapIsCompressed())
        {
            value = GetValueFromCompressedMap(rid);
        }
        else
        {
            PTR_TADDR pValue = GetIndexPtr(rid);
            value = RelativePointer<TADDR>::GetValueMaybeNullAtPtr(dac_cast<TADDR>(pValue));
        }

        _ASSERTE(hotValue == value || value == NULL);
    }
}
#endif // _DEBUG

#endif // FEATURE_PREJIT

// Get number of RIDs that this table can store
DWORD LookupMapBase::GetSize()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    LookupMapBase * pMap = this;
    DWORD dwSize = 0;
    do
    {
        dwSize += pMap->dwCount;
        pMap = pMap->pNext;
    } while (pMap != NULL);

    return dwSize;
}

#ifndef DACCESS_COMPILE

#ifdef _DEBUG
void LookupMapBase::DebugGetRidMapOccupancy(DWORD *pdwOccupied, DWORD *pdwSize)
{
    LIMITED_METHOD_CONTRACT;

    *pdwOccupied = 0;
    *pdwSize     = 0;

    LookupMapBase * pMap = this;

    // Go through each linked block
    for (; pMap != NULL; pMap = pMap->pNext)
    {
        DWORD dwIterCount = pMap->dwCount;

        for (DWORD i = 0; i < dwIterCount; i++)
        {
#ifdef FEATURE_PREJIT
            if (pMap->MapIsCompressed())
            {
                if (pMap->GetValueFromCompressedMap(i))
                    (*pdwOccupied)++;
            }
            else
#endif // FEATURE_PREJIT
            if (pMap->pTable[i] != NULL)
                (*pdwOccupied)++;
        }

        (*pdwSize) += dwIterCount;
    }
}

void Module::DebugLogRidMapOccupancy()
{
    WRAPPER_NO_CONTRACT;

#define COMPUTE_RID_MAP_OCCUPANCY(var_suffix, map)                                        \
    DWORD dwOccupied##var_suffix, dwSize##var_suffix, dwPercent##var_suffix;              \
    map.DebugGetRidMapOccupancy(&dwOccupied##var_suffix, &dwSize##var_suffix);            \
    dwPercent##var_suffix = dwOccupied##var_suffix ? ((dwOccupied##var_suffix * 100) / dwSize##var_suffix) : 0;

    COMPUTE_RID_MAP_OCCUPANCY(1, m_TypeDefToMethodTableMap);
    COMPUTE_RID_MAP_OCCUPANCY(2, m_TypeRefToMethodTableMap);
    COMPUTE_RID_MAP_OCCUPANCY(3, m_MethodDefToDescMap);
    COMPUTE_RID_MAP_OCCUPANCY(4, m_FieldDefToDescMap);
    COMPUTE_RID_MAP_OCCUPANCY(5, m_GenericParamToDescMap);
    COMPUTE_RID_MAP_OCCUPANCY(6, m_GenericTypeDefToCanonMethodTableMap);
    COMPUTE_RID_MAP_OCCUPANCY(7, m_FileReferencesMap);
    COMPUTE_RID_MAP_OCCUPANCY(8, m_ManifestModuleReferencesMap);
    COMPUTE_RID_MAP_OCCUPANCY(9, m_MethodDefToPropertyInfoMap);

    LOG((
        LF_EEMEM,
        INFO3,
        "   Map occupancy:\n"
        "      TypeDefToMethodTable map: %4d/%4d (%2d %%)\n"
        "      TypeRefToMethodTable map: %4d/%4d (%2d %%)\n"
        "      MethodDefToDesc map:  %4d/%4d (%2d %%)\n"
        "      FieldDefToDesc map:  %4d/%4d (%2d %%)\n"
        "      GenericParamToDesc map:  %4d/%4d (%2d %%)\n"
        "      GenericTypeDefToCanonMethodTable map:  %4d/%4d (%2d %%)\n"
        "      FileReferences map:  %4d/%4d (%2d %%)\n"
        "      AssemblyReferences map:  %4d/%4d (%2d %%)\n"
        "      MethodDefToPropInfo map: %4d/%4d (%2d %%)\n"
        ,
        dwOccupied1, dwSize1, dwPercent1,
        dwOccupied2, dwSize2, dwPercent2,
        dwOccupied3, dwSize3, dwPercent3,
        dwOccupied4, dwSize4, dwPercent4,
        dwOccupied5, dwSize5, dwPercent5,
        dwOccupied6, dwSize6, dwPercent6,
        dwOccupied7, dwSize7, dwPercent7,
        dwOccupied8, dwSize8, dwPercent8,
        dwOccupied9, dwSize9, dwPercent9
    ));

#undef COMPUTE_RID_MAP_OCCUPANCY
}
#endif // _DEBUG

//
// FindMethod finds a MethodDesc for a global function methoddef or ref
//

MethodDesc *Module::FindMethodThrowing(mdToken pMethod)
{
    CONTRACT (MethodDesc *)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END

    SigTypeContext typeContext;  /* empty type context: methods will not be generic */
    RETURN MemberLoader::GetMethodDescFromMemberDefOrRefOrSpec(this, pMethod,
                                                               &typeContext,
                                                               TRUE, /* strictMetadataChecks */
                                                               FALSE /* dont get code shared between generic instantiations */);
}

//
// FindMethod finds a MethodDesc for a global function methoddef or ref
//

MethodDesc *Module::FindMethod(mdToken pMethod)
{
    CONTRACT (MethodDesc *) {
        INSTANCE_CHECK;
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    } CONTRACT_END;

    MethodDesc *pMDRet = NULL;

    EX_TRY
    {
        pMDRet = FindMethodThrowing(pMethod);
    }
    EX_CATCH
    {
#ifdef _DEBUG
        CONTRACT_VIOLATION(ThrowsViolation);
        char szMethodName [MAX_CLASSNAME_LENGTH];
        CEEInfo::findNameOfToken(this, pMethod, szMethodName, COUNTOF (szMethodName));
        // This used to be IJW, but changed to LW_INTEROP to reclaim a bit in our log facilities
        LOG((LF_INTEROP, LL_INFO10, "Failed to find Method: %s for Vtable Fixup\n", szMethodName));
#endif // _DEBUG
    }
    EX_END_CATCH(SwallowAllExceptions)

    RETURN pMDRet;
}

//
// PopulatePropertyInfoMap precomputes property information during NGen
// that is expensive to look up from metadata at runtime.
//

void Module::PopulatePropertyInfoMap()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(IsCompilationProcess());
    }
    CONTRACTL_END;

    IMDInternalImport* mdImport = GetMDImport();
    HENUMInternalHolder   hEnum(mdImport);
    hEnum.EnumAllInit(mdtMethodDef);

    mdMethodDef md;
    while (hEnum.EnumNext(&md))
    {
        mdProperty prop = 0;
        ULONG semantic = 0;
        if (mdImport->GetPropertyInfoForMethodDef(md, &prop, NULL, &semantic) == S_OK)
        {
            // Store the Rid in the lower 24 bits and the semantic in the upper 8
            _ASSERTE((semantic & 0xFFFFFF00) == 0);
            SIZE_T value = RidFromToken(prop) | (semantic << 24);

            // We need to make sure a value of zero indicates an empty LookupMap entry
            // Fortunately the semantic will prevent value from being zero
            _ASSERTE(value != 0);

            m_MethodDefToPropertyInfoMap.AddElement(this, RidFromToken(md), value);
        }
    }
    FastInterlockOr(&m_dwPersistedFlags, COMPUTED_METHODDEF_TO_PROPERTYINFO_MAP);
}

//
// GetPropertyInfoForMethodDef wraps the metadata function of the same name,
// first trying to use the information stored in m_MethodDefToPropertyInfoMap.
//

HRESULT Module::GetPropertyInfoForMethodDef(mdMethodDef md, mdProperty *ppd, LPCSTR *pName, ULONG *pSemantic)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    HRESULT hr;

    if ((m_dwPersistedFlags & COMPUTED_METHODDEF_TO_PROPERTYINFO_MAP) != 0)
    {
        SIZE_T value = m_MethodDefToPropertyInfoMap.GetElement(RidFromToken(md));
        if (value == 0)
        {
            _ASSERTE(GetMDImport()->GetPropertyInfoForMethodDef(md, ppd, pName, pSemantic) == S_FALSE);
            return S_FALSE;
        }
        else
        {
            // Decode the value into semantic and mdProperty as described in PopulatePropertyInfoMap
            ULONG semantic = (value & 0xFF000000) >> 24;
            mdProperty prop = TokenFromRid(value & 0x00FFFFFF, mdtProperty);

#ifdef _DEBUG
            mdProperty dbgPd;
            LPCSTR dbgName;
            ULONG dbgSemantic;
            _ASSERTE(GetMDImport()->GetPropertyInfoForMethodDef(md, &dbgPd, &dbgName, &dbgSemantic) == S_OK);
#endif

            if (ppd != NULL)
            {
                *ppd = prop;
                _ASSERTE(*ppd == dbgPd);
            }

            if (pSemantic != NULL)
            {
                *pSemantic = semantic;
                _ASSERTE(*pSemantic == dbgSemantic);
            }

            if (pName != NULL)
            {
                IfFailRet(GetMDImport()->GetPropertyProps(prop, pName, NULL, NULL, NULL));

#ifdef _DEBUG
                HRESULT hr = GetMDImport()->GetPropertyProps(prop, pName, NULL, NULL, NULL);
                _ASSERTE(hr == S_OK);
                _ASSERTE(strcmp(*pName, dbgName) == 0);
#endif
            }

            return S_OK;
        }
    }

    return GetMDImport()->GetPropertyInfoForMethodDef(md, ppd, pName, pSemantic);
}

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
// Fill the m_propertyNameSet hash filter with data that represents every
// property and its name in the module.
void Module::PrecomputeMatchingProperties(DataImage *image)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(IsCompilationProcess());
    }
    CONTRACTL_END;

    IMDInternalImport* mdImport = GetMDImport();

    m_nPropertyNameSet = mdImport->GetCountWithTokenKind(mdtProperty);

    if (m_nPropertyNameSet == 0)
    {
        return;
    }

    m_propertyNameSet = new (image->GetHeap()) BYTE[m_nPropertyNameSet];

    DWORD nEnumeratedProperties = 0;

    HENUMInternalHolder   hEnumTypes(mdImport);
    hEnumTypes.EnumAllInit(mdtTypeDef);

    // Enumerate all properties of all types
    mdTypeDef tkType;
    while (hEnumTypes.EnumNext(&tkType))
    {
        HENUMInternalHolder   hEnumPropertiesForType(mdImport);
        hEnumPropertiesForType.EnumInit(mdtProperty, tkType);

        mdProperty tkProperty;
        while (hEnumPropertiesForType.EnumNext(&tkProperty))
        {
            LPCSTR name;
            HRESULT hr = GetMDImport()->GetPropertyProps(tkProperty, &name, NULL, NULL, NULL);
            IfFailThrow(hr);

            ++nEnumeratedProperties;

            // Use a case-insensitive hash so that we can use this value for
            // both case-sensitive and case-insensitive name lookups
            SString ssName(SString::Utf8Literal, name);
            ULONG nameHashValue = ssName.HashCaseInsensitive();

            // Set one bit in m_propertyNameSet per iteration
            // This will allow lookup to ensure that the bit from each iteration is set
            // and if any are not set, know that the (tkProperty,name) pair is not valid
            for (DWORD i = 0; i < NUM_PROPERTY_SET_HASHES; ++i)
            {
                DWORD currentHashValue = HashThreeToOne(tkProperty, nameHashValue, i);
                DWORD bitPos = currentHashValue % (m_nPropertyNameSet * 8);
                m_propertyNameSet[bitPos / 8] |= (1 << bitPos % 8);
            }
        }
    }

    _ASSERTE(nEnumeratedProperties == m_nPropertyNameSet);
}
#endif // FEATURE_NATIVE_IMAGE_GENERATION

// Check whether the module might possibly have a property with a name with
// the passed hash value without accessing the property's name.  This is done
// by consulting a hash filter populated at NGen time.
BOOL Module::MightContainMatchingProperty(mdProperty tkProperty, ULONG nameHash)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_propertyNameSet)
    {
        _ASSERTE(HasNativeImage());

        // if this property was added after the name set was computed, conservatively
        // assume we might have it. This is known to occur in scenarios where a profiler
        // injects additional metadata at module load time for an NGEN'ed module. In the
        // future other dynamic additions to the module might produce a similar result.
        if (RidFromToken(tkProperty) > m_nPropertyNameSet)
            return TRUE;

        // Check one bit per iteration, failing if any are not set
        // We know that all will have been set for any valid (tkProperty,name) pair
        for (DWORD i = 0; i < NUM_PROPERTY_SET_HASHES; ++i)
        {
            DWORD currentHashValue = HashThreeToOne(tkProperty, nameHash, i);
            DWORD bitPos = currentHashValue % (m_nPropertyNameSet * 8);
            if ((m_propertyNameSet[bitPos / 8] & (1 << bitPos % 8)) == 0)
            {
                return FALSE;
            }
        }
    }

    return TRUE;
}

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
// Ensure that all elements and flags that we want persisted in the LookupMaps are present
void Module::FinalizeLookupMapsPreSave(DataImage *image)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(IsCompilationProcess());
    }
    CONTRACTL_END;

    // For each typedef, if it does not need a restore, add the ZAPPED_TYPE_NEEDS_NO_RESTORE flag
    {
        LookupMap<PTR_MethodTable>::Iterator typeDefIter(&m_TypeDefToMethodTableMap);

        while (typeDefIter.Next())
        {
            MethodTable * pMT = typeDefIter.GetElement();

            if (pMT != NULL && !pMT->NeedsRestore(image))
            {
                m_TypeDefToMethodTableMap.AddFlag(RidFromToken(pMT->GetCl()), ZAPPED_TYPE_NEEDS_NO_RESTORE);
            }
        }
    }

    // For each canonical instantiation of a generic type def, if it does not need a restore, add the ZAPPED_GENERIC_TYPE_NEEDS_NO_RESTORE flag
    {
        LookupMap<PTR_MethodTable>::Iterator genericTypeDefIter(&m_GenericTypeDefToCanonMethodTableMap);

        while (genericTypeDefIter.Next())
        {
            MethodTable * pMT = genericTypeDefIter.GetElement();

            if (pMT != NULL && !pMT->NeedsRestore(image))
            {
                m_GenericTypeDefToCanonMethodTableMap.AddFlag(RidFromToken(pMT->GetCl()), ZAPPED_GENERIC_TYPE_NEEDS_NO_RESTORE);
            }
        }
    }

}
#endif // FEATURE_NATIVE_IMAGE_GENERATION

// Return true if this module has any live (jitted) JMC functions.
// If a module has no jitted JMC functions, then it's as if it's a
// non-user module.
bool Module::HasAnyJMCFunctions()
{
    LIMITED_METHOD_CONTRACT;

    // If we have any live JMC funcs in us, then we're a JMC module.
    // We count JMC functions when we either explicitly toggle their status
    // or when we get the code:DebuggerMethodInfo for them (which happens in a jit-complete).
    // Since we don't get the jit-completes for ngen modules, we also check the module's
    // "default" status. This means we may err on the side of believing we have
    // JMC methods.
    return ((m_debuggerSpecificData.m_cTotalJMCFuncs > 0) || m_debuggerSpecificData.m_fDefaultJMCStatus);
}

// Alter our module's count of JMC functions.
// Since these may be called on multiple threads (say 2 threads are jitting
// methods within a module), make it thread safe.
void Module::IncJMCFuncCount()
{
    LIMITED_METHOD_CONTRACT;

    InterlockedIncrement(&m_debuggerSpecificData.m_cTotalJMCFuncs);
}

void Module::DecJMCFuncCount()
{
    LIMITED_METHOD_CONTRACT;

    InterlockedDecrement(&m_debuggerSpecificData.m_cTotalJMCFuncs);
}

// code:DebuggerMethodInfo are lazily created. Let them lookup what the default is.
bool Module::GetJMCStatus()
{
    LIMITED_METHOD_CONTRACT;

    return m_debuggerSpecificData.m_fDefaultJMCStatus;
}

// Set the default JMC status of this module.
void Module::SetJMCStatus(bool fStatus)
{
    LIMITED_METHOD_CONTRACT;

    m_debuggerSpecificData.m_fDefaultJMCStatus = fStatus;
}

// Update the dynamic metadata if needed. Nop for non-dynamic modules
void Module::UpdateDynamicMetadataIfNeeded()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    // Only need to serializing metadata for dynamic modules. For non-dynamic modules, metadata is already available.
    if (!IsReflection())
    {
        return;
    }

    // Since serializing metadata to an auxillary buffer is only needed by the debugger,
    // we should only be doing this for modules that the debugger can see.
    if (!IsVisibleToDebugger())
    {
        return;
    }


    HRESULT hr = S_OK;
    EX_TRY
    {
        GetReflectionModule()->CaptureModuleMetaDataToMemory();
    }
    EX_CATCH_HRESULT(hr);

    // This Metadata buffer is only used for the debugger, so it's a non-fatal exception for regular CLR execution.
    // Just swallow it and keep going. However, with the exception of out-of-memory, we do expect it to
    // succeed, so assert on failures.
    if (hr != E_OUTOFMEMORY)
    {
        SIMPLIFYING_ASSUMPTION_SUCCEEDED(hr);
    }

}

#ifdef DEBUGGING_SUPPORTED


#endif // DEBUGGING_SUPPORTED

BOOL Module::NotifyDebuggerLoad(AppDomain *pDomain, DomainFile * pDomainFile, int flags, BOOL attaching)
{
    WRAPPER_NO_CONTRACT;

    // We don't notify the debugger about modules that don't contain any code.
    if (!IsVisibleToDebugger())
        return FALSE;

    // Always capture metadata, even if no debugger is attached. If a debugger later attaches, it will use
    // this data.
    {
        Module * pModule = pDomainFile->GetModule();
        pModule->UpdateDynamicMetadataIfNeeded();
    }


    //
    // Remaining work is only needed if a debugger is attached
    //
    if (!attaching && !pDomain->IsDebuggerAttached())
        return FALSE;


    BOOL result = FALSE;

    if (flags & ATTACH_MODULE_LOAD)
    {
        g_pDebugInterface->LoadModule(this,
                                      m_file->GetPath(),
                                      m_file->GetPath().GetCount(),
                                      GetAssembly(),
                                      pDomain,
                                      pDomainFile,
                                      attaching);

        result = TRUE;
    }

    if (flags & ATTACH_CLASS_LOAD)
    {
        LookupMap<PTR_MethodTable>::Iterator typeDefIter(&m_TypeDefToMethodTableMap);
        while (typeDefIter.Next())
        {
            MethodTable * pMT = typeDefIter.GetElement();

            if (pMT != NULL && pMT->IsRestored())
            {
                result = TypeHandle(pMT).NotifyDebuggerLoad(pDomain, attaching) || result;
            }
        }
    }

    return result;
}

void Module::NotifyDebuggerUnload(AppDomain *pDomain)
{
    LIMITED_METHOD_CONTRACT;

    if (!pDomain->IsDebuggerAttached())
        return;

    // We don't notify the debugger about modules that don't contain any code.
    if (!IsVisibleToDebugger())
        return;

    LookupMap<PTR_MethodTable>::Iterator typeDefIter(&m_TypeDefToMethodTableMap);
    while (typeDefIter.Next())
    {
        MethodTable * pMT = typeDefIter.GetElement();

        if (pMT != NULL && pMT->IsRestored())
        {
            TypeHandle(pMT).NotifyDebuggerUnload(pDomain);
        }
    }

    g_pDebugInterface->UnloadModule(this, pDomain);
}

#if !defined(CROSSGEN_COMPILE)
using GetTokenForVTableEntry_t = mdToken(STDMETHODCALLTYPE*)(HMODULE module, BYTE**ppVTEntry);

static HMODULE GetIJWHostForModule(Module* module)
{
#if !defined(TARGET_UNIX)
    PEDecoder* pe = module->GetFile()->GetLoadedIL();

    BYTE* baseAddress = (BYTE*)module->GetFile()->GetIJWBase();

    IMAGE_IMPORT_DESCRIPTOR* importDescriptor = (IMAGE_IMPORT_DESCRIPTOR*)pe->GetDirectoryData(pe->GetDirectoryEntry(IMAGE_DIRECTORY_ENTRY_IMPORT));

    if (importDescriptor == nullptr)
    {
        return nullptr;
    }

    for(; importDescriptor->Characteristics != 0; importDescriptor++)
    {
        IMAGE_THUNK_DATA* importNameTable = (IMAGE_THUNK_DATA*)pe->GetRvaData(importDescriptor->OriginalFirstThunk);

        IMAGE_THUNK_DATA* importAddressTable = (IMAGE_THUNK_DATA*)pe->GetRvaData(importDescriptor->FirstThunk);

        for (int thunkIndex = 0; importNameTable[thunkIndex].u1.AddressOfData != 0; thunkIndex++)
        {
            // The most significant bit will be set if the entry points to an ordinal.
            if ((importNameTable[thunkIndex].u1.Ordinal & (1LL << (sizeof(importNameTable[thunkIndex].u1.Ordinal) * CHAR_BIT - 1))) == 0)
            {
                IMAGE_IMPORT_BY_NAME* nameImport = (IMAGE_IMPORT_BY_NAME*)(baseAddress + importNameTable[thunkIndex].u1.AddressOfData);
                if (strcmp("_CorDllMain", nameImport->Name) == 0)
                {
                    HMODULE ijwHost;

                    if (WszGetModuleHandleEx(
                        GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
                        (LPCWSTR)importAddressTable[thunkIndex].u1.Function,
                        &ijwHost))
                    {
                        return ijwHost;
                    }

                }
            }
        }
    }
#endif
    return nullptr;
}

static GetTokenForVTableEntry_t GetTokenGetterFromHostModule(HMODULE ijwHost)
{
    if (ijwHost != nullptr)
    {
        return (GetTokenForVTableEntry_t)GetProcAddress(ijwHost, "GetTokenForVTableEntry");
    }

    return nullptr;
}

//=================================================================================
mdToken GetTokenForVTableEntry(HINSTANCE hInst, BYTE **ppVTEntry)
{
    CONTRACTL{
        NOTHROW;
    } CONTRACTL_END;

    mdToken tok =(mdToken)(UINT_PTR)*ppVTEntry;
    _ASSERTE(TypeFromToken(tok) == mdtMethodDef || TypeFromToken(tok) == mdtMemberRef);
    return tok;
}

//=================================================================================
void SetTargetForVTableEntry(HINSTANCE hInst, BYTE **ppVTEntry, BYTE *pTarget)
{
    CONTRACTL{
        THROWS;
    } CONTRACTL_END;

    DWORD oldProtect;
    if (!ClrVirtualProtect(ppVTEntry, sizeof(BYTE*), PAGE_READWRITE, &oldProtect))
    {
        // This is very bad.  We are not going to be able to update header.
        _ASSERTE(!"SetTargetForVTableEntry(): VirtualProtect() changing IJW thunk vtable to R/W failed.\n");
        ThrowLastError();
    }

    *ppVTEntry = pTarget;

    DWORD ignore;
    if (!ClrVirtualProtect(ppVTEntry, sizeof(BYTE*), oldProtect, &ignore))
    {
        // This is not so bad, we're already done the update, we just didn't return the thunk table to read only
        _ASSERTE(!"SetTargetForVTableEntry(): VirtualProtect() changing IJW thunk vtable back to RO failed.\n");
    }
}

//=================================================================================
BYTE * GetTargetForVTableEntry(HINSTANCE hInst, BYTE **ppVTEntry)
{
    CONTRACTL{
        NOTHROW;
    } CONTRACTL_END;

    return *ppVTEntry;
}

//======================================================================================
// Fixup vtables stored in the header to contain pointers to method desc
// prestubs rather than metadata method tokens.
void Module::FixupVTables()
{
    CONTRACTL{
        INSTANCE_CHECK;
        STANDARD_VM_CHECK;
    } CONTRACTL_END;


    // If we've already fixed up, or this is not an IJW module, just return.
    // NOTE: This relies on ILOnly files not having fixups. If this changes,
    //       we need to change this conditional.
    if (IsIJWFixedUp() || m_file->IsILOnly()) {
        return;
    }

    // Try getting a callback to the IJW host if it is loaded.
    // The IJW host substitutes in special shims in the vtfixup table
    // so if it is loaded, we need to query it for the tokens that were in the slots.
    // If it is not loaded, then we know that the vtfixup table entries are tokens,
    // so we can resolve them ourselves.
    GetTokenForVTableEntry_t GetTokenForVTableEntryCallback = GetTokenGetterFromHostModule(GetIJWHostForModule(this));

    if (GetTokenForVTableEntryCallback == nullptr)
    {
        GetTokenForVTableEntryCallback = GetTokenForVTableEntry;
    }

    HINSTANCE hInstThis = GetFile()->GetIJWBase();

    // <REVISIT_TODO>@todo: workaround!</REVISIT_TODO>
    // If we are compiling in-process, we don't want to fixup the vtables - as it
    // will have side effects on the other copy of the module!
    if (SystemDomain::GetCurrentDomain()->IsCompilationDomain()) {
        return;
    }

#ifdef FEATURE_PREJIT
    // We delayed filling in this value until the LoadLibrary occurred
    if (HasTls() && HasNativeImage()) {
        CORCOMPILE_EE_INFO_TABLE *pEEInfo = GetNativeImage()->GetNativeEEInfoTable();
        pEEInfo->rvaStaticTlsIndex = GetTlsIndex();
    }
#endif
    // Get vtable fixup data
    COUNT_T cFixupRecords;
    IMAGE_COR_VTABLEFIXUP *pFixupTable = m_file->GetVTableFixups(&cFixupRecords);

    // No records then return
    if (cFixupRecords == 0) {
        return;
    }

    // Now, we need to take a lock to serialize fixup.
    PEImage::IJWFixupData *pData = PEImage::GetIJWData(m_file->GetIJWBase());

    // If it's already been fixed (in some other appdomain), record the fact and return
    if (pData->IsFixedUp()) {
        SetIsIJWFixedUp();
        return;
    }

    //////////////////////////////////////////////////////
    //
    // This is done in three stages:
    //  1. We enumerate the types we'll need to load
    //  2. We load the types
    //  3. We create and install the thunks
    //

    COUNT_T cVtableThunks = 0;
    struct MethodLoadData
    {
        mdToken     token;
        MethodDesc *pMD;
    };
    MethodLoadData *rgMethodsToLoad = NULL;
    COUNT_T cMethodsToLoad = 0;

    //
    // Stage 1
    //

    // Each fixup entry describes a vtable, so iterate the vtables and sum their counts
    {
        DWORD iFixup;
        for (iFixup = 0; iFixup < cFixupRecords; iFixup++)
            cVtableThunks += pFixupTable[iFixup].Count;
    }

    ACQUIRE_STACKING_ALLOCATOR(pAlloc);

    // Allocate the working array of tokens.
    cMethodsToLoad = cVtableThunks;

    rgMethodsToLoad = new (pAlloc) MethodLoadData[cMethodsToLoad];
    memset(rgMethodsToLoad, 0, cMethodsToLoad * sizeof(MethodLoadData));

    // Now take the IJW module lock and get all the tokens
    {
        // Take the lock
        CrstHolder lockHolder(pData->GetLock());

        // If someone has beaten us, just return
        if (pData->IsFixedUp())
        {
            SetIsIJWFixedUp();
            return;
        }

        COUNT_T iCurMethod = 0;

        if (cFixupRecords != 0)
        {
            for (COUNT_T iFixup = 0; iFixup < cFixupRecords; iFixup++)
            {
                // Vtables can be 32 or 64 bit.
                if ((pFixupTable[iFixup].Type == (COR_VTABLE_PTRSIZED)) ||
                    (pFixupTable[iFixup].Type == (COR_VTABLE_PTRSIZED | COR_VTABLE_FROM_UNMANAGED)) ||
                    (pFixupTable[iFixup].Type == (COR_VTABLE_PTRSIZED | COR_VTABLE_FROM_UNMANAGED_RETAIN_APPDOMAIN)))
                {
                    const BYTE** pPointers = (const BYTE **)m_file->GetVTable(pFixupTable[iFixup].RVA);
                    for (int iMethod = 0; iMethod < pFixupTable[iFixup].Count; iMethod++)
                    {
                        if (pData->IsMethodFixedUp(iFixup, iMethod))
                            continue;
                        mdToken mdTok = GetTokenForVTableEntryCallback(hInstThis, (BYTE**)(pPointers + iMethod));
                        CONSISTENCY_CHECK(mdTok != mdTokenNil);
                        rgMethodsToLoad[iCurMethod++].token = mdTok;
                    }
                }
            }
        }

    }

    //
    // Stage 2 - Load the types
    //

    {
        for (COUNT_T iCurMethod = 0; iCurMethod < cMethodsToLoad; iCurMethod++)
        {
            mdToken curTok = rgMethodsToLoad[iCurMethod].token;
            if (!GetMDImport()->IsValidToken(curTok))
            {
                _ASSERTE(!"Invalid token in v-table fix-up table");
                ThrowHR(COR_E_BADIMAGEFORMAT);
            }


            // Find the method desc
            MethodDesc *pMD;

            {
                CONTRACT_VIOLATION(LoadsTypeViolation);
                pMD = FindMethodThrowing(curTok);
            }

            CONSISTENCY_CHECK(CheckPointer(pMD));

            rgMethodsToLoad[iCurMethod].pMD = pMD;
        }
    }

    //
    // Stage 3 - Create the thunk data
    //
    {
        // Take the lock
        CrstHolder lockHolder(pData->GetLock());

        // If someone has beaten us, just return
        if (pData->IsFixedUp())
        {
            SetIsIJWFixedUp();
            return;
        }

        // This phase assumes there is only one AppDomain and that thunks
        // can all safely point directly to the method in the current AppDomain

        AppDomain *pAppDomain = GetAppDomain();

        // Used to index into rgMethodsToLoad
        COUNT_T iCurMethod = 0;


        // Each fixup entry describes a vtable (each slot contains a metadata token
        // at this stage).
        DWORD iFixup;
        for (iFixup = 0; iFixup < cFixupRecords; iFixup++)
            cVtableThunks += pFixupTable[iFixup].Count;

        DWORD dwIndex = 0;
        DWORD dwThunkIndex = 0;

        // Now to fill in the thunk table.
        for (iFixup = 0; iFixup < cFixupRecords; iFixup++)
        {
            // Tables may contain zero fixups, in which case the RVA is null, which triggers an assert
            if (pFixupTable[iFixup].Count == 0)
                continue;

            const BYTE** pPointers = (const BYTE **)
                m_file->GetVTable(pFixupTable[iFixup].RVA);

            // Vtables can be 32 or 64 bit.
            if (pFixupTable[iFixup].Type == COR_VTABLE_PTRSIZED)
            {
                for (int iMethod = 0; iMethod < pFixupTable[iFixup].Count; iMethod++)
                {
                    if (pData->IsMethodFixedUp(iFixup, iMethod))
                        continue;

                    mdToken mdTok = rgMethodsToLoad[iCurMethod].token;
                    MethodDesc *pMD = rgMethodsToLoad[iCurMethod].pMD;
                    iCurMethod++;

#ifdef _DEBUG
                    if (pMD->IsNDirect())
                    {
                        LOG((LF_INTEROP, LL_INFO10, "[0x%lx] <-- PINV thunk for \"%s\" (target = 0x%lx)\n",
                            (size_t)&(pPointers[iMethod]), pMD->m_pszDebugMethodName,
                            (size_t)(((NDirectMethodDesc*)pMD)->GetNDirectTarget())));
                    }
#endif // _DEBUG

                    CONSISTENCY_CHECK(dwThunkIndex < cVtableThunks);

                    // Point the local vtable slot to the thunk we created
                    SetTargetForVTableEntry(hInstThis, (BYTE **)&pPointers[iMethod], (BYTE *)pMD->GetMultiCallableAddrOfCode());

                    pData->MarkMethodFixedUp(iFixup, iMethod);

                    dwThunkIndex++;
                }

            }
            else if (pFixupTable[iFixup].Type == (COR_VTABLE_PTRSIZED | COR_VTABLE_FROM_UNMANAGED) ||
                    (pFixupTable[iFixup].Type == (COR_VTABLE_PTRSIZED | COR_VTABLE_FROM_UNMANAGED_RETAIN_APPDOMAIN)))
            {
                for (int iMethod = 0; iMethod < pFixupTable[iFixup].Count; iMethod++)
                {
                    if (pData->IsMethodFixedUp(iFixup, iMethod))
                        continue;

                    mdToken mdTok = rgMethodsToLoad[iCurMethod].token;
                    MethodDesc *pMD = rgMethodsToLoad[iCurMethod].pMD;
                    iCurMethod++;
                    LOG((LF_INTEROP, LL_INFO10, "[0x%p] <-- VTable  thunk for \"%s\" (pMD = 0x%p)\n",
                        (UINT_PTR)&(pPointers[iMethod]), pMD->m_pszDebugMethodName, pMD));

                    UMEntryThunk *pUMEntryThunk = (UMEntryThunk*)(void*)(GetDllThunkHeap()->AllocAlignedMem(sizeof(UMEntryThunk), CODE_SIZE_ALIGN)); // UMEntryThunk contains code
                    ExecutableWriterHolder<UMEntryThunk> uMEntryThunkWriterHolder(pUMEntryThunk, sizeof(UMEntryThunk));
                    FillMemory(uMEntryThunkWriterHolder.GetRW(), sizeof(UMEntryThunk), 0);

                    UMThunkMarshInfo *pUMThunkMarshInfo = (UMThunkMarshInfo*)(void*)(GetThunkHeap()->AllocAlignedMem(sizeof(UMThunkMarshInfo), CODE_SIZE_ALIGN));
                    ExecutableWriterHolder<UMThunkMarshInfo> uMThunkMarshInfoWriterHolder(pUMThunkMarshInfo, sizeof(UMThunkMarshInfo));
                    FillMemory(uMThunkMarshInfoWriterHolder.GetRW(), sizeof(UMThunkMarshInfo), 0);

                    uMThunkMarshInfoWriterHolder.GetRW()->LoadTimeInit(pMD);
                    uMEntryThunkWriterHolder.GetRW()->LoadTimeInit(pUMEntryThunk, NULL, NULL, pUMThunkMarshInfo, pMD);

                    SetTargetForVTableEntry(hInstThis, (BYTE **)&pPointers[iMethod], (BYTE *)pUMEntryThunk->GetCode());

                    pData->MarkMethodFixedUp(iFixup, iMethod);
                }
            }
            else if ((pFixupTable[iFixup].Type & COR_VTABLE_NOT_PTRSIZED) == COR_VTABLE_NOT_PTRSIZED)
            {
                // fixup type doesn't match the platform
                THROW_BAD_FORMAT(BFA_FIXUP_WRONG_PLATFORM, this);
            }
            else
            {
                _ASSERTE(!"Unknown vtable fixup type");
            }
        }

        // Indicate that this module has been fixed before releasing the lock
        pData->SetIsFixedUp();  // On the data
        SetIsIJWFixedUp();      // On the module
    } // End of Stage 3
}

// Self-initializing accessor for m_pThunkHeap
LoaderHeap *Module::GetDllThunkHeap()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    return PEImage::GetDllThunkHeap(GetFile()->GetIJWBase());

}

LoaderHeap *Module::GetThunkHeap()
{
    CONTRACT(LoaderHeap *)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END

    if (!m_pThunkHeap)
    {
        LoaderHeap *pNewHeap = new LoaderHeap(VIRTUAL_ALLOC_RESERVE_GRANULARITY, // DWORD dwReserveBlockSize
            0,                                 // DWORD dwCommitBlockSize
            ThunkHeapStubManager::g_pManager->GetRangeList(),
            TRUE);                             // BOOL fMakeExecutable

        if (FastInterlockCompareExchangePointer(&m_pThunkHeap, pNewHeap, 0) != 0)
        {
            delete pNewHeap;
        }
    }

    RETURN m_pThunkHeap;
}
#endif // !CROSSGEN_COMPILE

#ifdef FEATURE_NATIVE_IMAGE_GENERATION

// These helpers are used in Module::ExpandAll
// to avoid EX_TRY/EX_CATCH in a loop (uses _alloca and guzzles stack)

static TypeHandle LoadTypeDefOrRefHelper(DataImage * image, Module * pModule, mdToken tk)
{
    STANDARD_VM_CONTRACT;

    TypeHandle th;

    EX_TRY
    {
        th = ClassLoader::LoadTypeDefOrRefThrowing(pModule, tk,
                                              ClassLoader::ThrowIfNotFound,
                                              ClassLoader::PermitUninstDefOrRef);
    }
    EX_CATCH
    {
        image->GetPreloader()->Error(tk, GET_EXCEPTION());
    }
    EX_END_CATCH(SwallowAllExceptions)

    return th;
}

static TypeHandle LoadTypeSpecHelper(DataImage * image, Module * pModule, mdToken tk,
                                     PCCOR_SIGNATURE pSig, ULONG cSig)
{
    STANDARD_VM_CONTRACT;

    TypeHandle th;

    EX_TRY
    {
        SigPointer p(pSig, cSig);
        SigTypeContext typeContext;
        th = p.GetTypeHandleThrowing(pModule, &typeContext);
    }
    EX_CATCH
    {
        image->GetPreloader()->Error(tk, GET_EXCEPTION());
    }
    EX_END_CATCH(SwallowAllExceptions)

    return th;
}

static TypeHandle LoadGenericInstantiationHelper(DataImage * image, Module * pModule, mdToken tk, Instantiation inst)
{
    STANDARD_VM_CONTRACT;

    TypeHandle th;

    EX_TRY
    {
        th = ClassLoader::LoadGenericInstantiationThrowing(pModule, tk, inst);
    }
    EX_CATCH
    {
        image->GetPreloader()->Error(tk, GET_EXCEPTION());
    }
    EX_END_CATCH(SwallowAllExceptions)

    return th;
}

static void GetDescFromMemberRefHelper(DataImage * image, Module * pModule, mdToken tk)
{
    STANDARD_VM_CONTRACT;

    EX_TRY
    {
        MethodDesc * pMD = NULL;
        FieldDesc * pFD = NULL;
        TypeHandle th;

        // Note: using an empty type context is now OK, because even though the token is a MemberRef
        // neither the token nor its parent will directly refer to type variables.
        // @TODO GENERICS: want to allow loads of generic methods here but need strict metadata checks on parent
        SigTypeContext typeContext;
        MemberLoader::GetDescFromMemberRef(pModule, tk, &pMD, &pFD,
                &typeContext,
                FALSE /* strict metadata checks */, &th);
    }
    EX_CATCH
    {
        image->GetPreloader()->Error(tk, GET_EXCEPTION());
    }
    EX_END_CATCH(SwallowAllExceptions)
}

void Module::SetProfileData(CorProfileData * profileData)
{
    LIMITED_METHOD_CONTRACT;
    m_pProfileData = profileData;
}

CorProfileData * Module::GetProfileData()
{
    LIMITED_METHOD_CONTRACT;
    return m_pProfileData;
}

mdTypeDef Module::LookupIbcTypeToken(Module *  pExternalModule, mdToken ibcToken, SString* optionalFullNameOut)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END

    _ASSERTE(TypeFromToken(ibcToken) == ibcExternalType);

    CorProfileData *  profileData  = this->GetProfileData();

    CORBBTPROF_BLOB_TYPE_DEF_ENTRY *  blobTypeDefEntry;
    blobTypeDefEntry = profileData->GetBlobExternalTypeDef(ibcToken);

    if (blobTypeDefEntry == NULL)
        return mdTypeDefNil;

    IbcNameHandle  ibcName;
    ibcName.szName           = &blobTypeDefEntry->name[0];
    ibcName.tkIbcNameSpace   = blobTypeDefEntry->nameSpaceToken;
    ibcName.tkIbcNestedClass = blobTypeDefEntry->nestedClassToken;
    ibcName.szNamespace      = NULL;
    ibcName.tkEnclosingClass = mdTypeDefNil;

    if (!IsNilToken(blobTypeDefEntry->nameSpaceToken))
    {
        _ASSERTE(IsNilToken(blobTypeDefEntry->nestedClassToken));

        idExternalNamespace nameSpaceToken = blobTypeDefEntry->nameSpaceToken;
        _ASSERTE(TypeFromToken(nameSpaceToken) == ibcExternalNamespace);

        CORBBTPROF_BLOB_NAMESPACE_DEF_ENTRY *  blobNamespaceDefEntry;
        blobNamespaceDefEntry = profileData->GetBlobExternalNamespaceDef(nameSpaceToken);

        if (blobNamespaceDefEntry == NULL)
            return mdTypeDefNil;

        ibcName.szNamespace = &blobNamespaceDefEntry->name[0];

        if (optionalFullNameOut != NULL)
        {
            optionalFullNameOut->Append(W("["));
            optionalFullNameOut->AppendUTF8(pExternalModule->GetSimpleName());
            optionalFullNameOut->Append(W("]"));

            if ((ibcName.szNamespace != NULL) && ((*ibcName.szNamespace) != W('\0')))
            {
                optionalFullNameOut->AppendUTF8(ibcName.szNamespace);
                optionalFullNameOut->Append(W("."));
            }
            optionalFullNameOut->AppendUTF8(ibcName.szName);
        }
    }
    else if (!IsNilToken(blobTypeDefEntry->nestedClassToken))
    {
        idExternalType nestedClassToken = blobTypeDefEntry->nestedClassToken;
        _ASSERTE(TypeFromToken(nestedClassToken) == ibcExternalType);

        ibcName.tkEnclosingClass = LookupIbcTypeToken(pExternalModule, nestedClassToken, optionalFullNameOut);

        if (optionalFullNameOut != NULL)
        {
            optionalFullNameOut->Append(W("+"));
            optionalFullNameOut->AppendUTF8(ibcName.szName);
        }

        if (IsNilToken(ibcName.tkEnclosingClass))
            return  mdTypeDefNil;
    }

    //*****************************************
    // look up function for TypeDef
    //*****************************************
    // STDMETHOD(FindTypeDef)(
    //     LPCSTR      szNamespace,            // [IN] Namespace for the TypeDef.
    //     LPCSTR      szName,                 // [IN] Name of the TypeDef.
    //     mdToken     tkEnclosingClass,       // [IN] TypeRef/TypeDef Token for the enclosing class.
    //     mdTypeDef   *ptypedef) PURE;        // [IN] return typedef

    IMDInternalImport *pInternalImport = pExternalModule->GetMDImport();

    mdTypeDef mdResult = mdTypeDefNil;

    HRESULT hr = pInternalImport->FindTypeDef(ibcName.szNamespace, ibcName.szName, ibcName.tkEnclosingClass, &mdResult);

    if(FAILED(hr))
        mdResult = mdTypeDefNil;

    return mdResult;
}

struct IbcCompareContext
{
    Module *         pModule;
    TypeHandle       enclosingType;
    DWORD            cMatch;      // count of methods that had a matching method name
    bool             useBestSig;  // if true we should use the BestSig when we don't find an exact match
    PCCOR_SIGNATURE  pvBestSig;   // Current Best matching signature
    DWORD            cbBestSig;   //
};

//---------------------------------------------------------------------------------------
//
// Compare two signatures from the same scope.
//
BOOL
CompareIbcMethodSigs(
    PCCOR_SIGNATURE pvCandidateSig, // Candidate signature
    DWORD           cbCandidateSig, //
    PCCOR_SIGNATURE pvIbcSignature, // The Ibc signature that we want to match
    DWORD           cbIbcSignature, //
    void *          pvContext)      // void pointer to IbcCompareContext
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END

    //
    // Same pointer return TRUE
    //
    if (pvCandidateSig == pvIbcSignature)
    {
        _ASSERTE(cbCandidateSig == cbIbcSignature);
        return TRUE;
    }

    //
    // Check for exact match
    //
    if (cbCandidateSig == cbIbcSignature)
    {
        if (memcmp(pvCandidateSig, pvIbcSignature, cbIbcSignature) == 0)
        {
            return TRUE;
        }
    }

    IbcCompareContext * context = (IbcCompareContext *) pvContext;

    //
    // No exact match, we will return FALSE and keep looking at other matching method names
    //
    // However since the method name was an exact match we will remember this signature,
    // so that if it is the best match we can look it up again and return it's methodDef token
    //
    if (context->cMatch == 0)
    {
        context->pvBestSig = pvCandidateSig;
        context->cbBestSig = cbCandidateSig;
        context->cMatch = 1;
        context->useBestSig = true;
    }
    else
    {
        context->cMatch++;

        SigTypeContext emptyTypeContext;
        SigTypeContext ibcTypeContext =  SigTypeContext(context->enclosingType);
        MetaSig ibcSignature (pvIbcSignature, cbIbcSignature, context->pModule, &ibcTypeContext);

        MetaSig candidateSig (pvCandidateSig, cbCandidateSig, context->pModule, &emptyTypeContext);
        MetaSig bestSignature(context->pvBestSig, context->cbBestSig, context->pModule, &emptyTypeContext);
        //
        // Is candidateSig a better match than bestSignature?
        //
        // First check the calling convention
        //
        if (candidateSig.GetCallingConventionInfo() != bestSignature.GetCallingConventionInfo())
        {
            if (bestSignature.GetCallingConventionInfo() == ibcSignature.GetCallingConventionInfo())
                goto LEAVE_BEST;
            if (candidateSig.GetCallingConventionInfo() == ibcSignature.GetCallingConventionInfo())
                goto SELECT_CANDIDATE;
            //
            // Neither one is a match
            //
            goto USE_NEITHER;
        }

        //
        // Next check the number of arguments
        //
        if (candidateSig.NumFixedArgs() != bestSignature.NumFixedArgs())
        {
            //
            // Does one of the two have the same number of args?
            //
            if (bestSignature.NumFixedArgs() == ibcSignature.NumFixedArgs())
                goto LEAVE_BEST;
            if (candidateSig.NumFixedArgs() == ibcSignature.NumFixedArgs())
                goto SELECT_CANDIDATE;
            //
            // Neither one is a match
            //
            goto USE_NEITHER;
        }
        else if (candidateSig.NumFixedArgs() != ibcSignature.NumFixedArgs())
        {
            //
            // Neither one is a match
            //
            goto USE_NEITHER;
        }

        CorElementType  etIbc;
        CorElementType  etCandidate;
        CorElementType  etBest;
        //
        // Next get the return element type
        //
        // etIbc = ibcSignature.GetReturnProps().PeekElemTypeClosed(ibcSignature.GetSigTypeContext());
        IfFailThrow(ibcSignature.GetReturnProps().PeekElemType(&etIbc));
        IfFailThrow(candidateSig.GetReturnProps().PeekElemType(&etCandidate));
        IfFailThrow(bestSignature.GetReturnProps().PeekElemType(&etBest));
        //
        // Do they have different return types?
        //
        if (etCandidate != etBest)
        {
            if (etBest == etIbc)
                goto LEAVE_BEST;

            if (etCandidate == etIbc)
                goto SELECT_CANDIDATE;
        }

        //
        // Now iterate over the method argument types to see which signature
        // is the better match
        //
        for (DWORD i = 0; (i < ibcSignature.NumFixedArgs()); i++)
        {
            ibcSignature.SkipArg();
            IfFailThrow(ibcSignature.GetArgProps().PeekElemType(&etIbc));

            candidateSig.SkipArg();
            IfFailThrow(candidateSig.GetArgProps().PeekElemType(&etCandidate));

            bestSignature.SkipArg();
            IfFailThrow(bestSignature.GetArgProps().PeekElemType(&etBest));

            //
            // Do they have different argument types?
            //
            if (etCandidate != etBest)
            {
                if (etBest == etIbc)
                    goto LEAVE_BEST;

                if (etCandidate == etIbc)
                    goto SELECT_CANDIDATE;
            }
        }
        // When we fall though to here we did not find any differences
        // that we could base a choice on
        //
         context->useBestSig = true;

SELECT_CANDIDATE:
        context->pvBestSig = pvCandidateSig;
        context->cbBestSig = cbCandidateSig;
        context->useBestSig = true;
        return FALSE;

USE_NEITHER:
        context->useBestSig = false;
        return FALSE;
    }

LEAVE_BEST:
    return FALSE;
} // CompareIbcMethodSigs

mdMethodDef Module::LookupIbcMethodToken(TypeHandle enclosingType, mdToken ibcToken, SString* optionalFullNameOut)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END

    _ASSERTE(TypeFromToken(ibcToken) == ibcExternalMethod);

    CorProfileData *  profileData  = this->GetProfileData();

    CORBBTPROF_BLOB_METHOD_DEF_ENTRY *  blobMethodDefEntry;
    blobMethodDefEntry = profileData->GetBlobExternalMethodDef(ibcToken);

    if (blobMethodDefEntry == NULL)
        return mdMethodDefNil;

    idExternalType signatureToken = blobMethodDefEntry->signatureToken;
    _ASSERTE(!IsNilToken(signatureToken));
    _ASSERTE(TypeFromToken(signatureToken) == ibcExternalSignature);

    CORBBTPROF_BLOB_SIGNATURE_DEF_ENTRY *  blobSignatureDefEntry;
    blobSignatureDefEntry = profileData->GetBlobExternalSignatureDef(signatureToken);

    if (blobSignatureDefEntry == NULL)
        return mdMethodDefNil;

    IbcNameHandle    ibcName;
    ibcName.szName                    = &blobMethodDefEntry->name[0];
    ibcName.tkIbcNestedClass          = blobMethodDefEntry->nestedClassToken;
    ibcName.tkIbcNameSpace            = idExternalNamespaceNil;
    ibcName.szNamespace               = NULL;
    ibcName.tkEnclosingClass          = mdTypeDefNil;

    Module *         pExternalModule  = enclosingType.GetModule();
    PCCOR_SIGNATURE  pvSig            = NULL;
    ULONG            cbSig            = 0;

    _ASSERTE(!IsNilToken(ibcName.tkIbcNestedClass));
    _ASSERTE(TypeFromToken(ibcName.tkIbcNestedClass) == ibcExternalType);

    ibcName.tkEnclosingClass = LookupIbcTypeToken(pExternalModule, ibcName.tkIbcNestedClass, optionalFullNameOut);

    if (IsNilToken(ibcName.tkEnclosingClass))
    {
        COMPlusThrow(kTypeLoadException, IDS_IBC_MISSING_EXTERNAL_TYPE);
    }

    if (optionalFullNameOut != NULL)
    {
        optionalFullNameOut->Append(W("."));
        optionalFullNameOut->AppendUTF8(ibcName.szName);    // MethodName
        optionalFullNameOut->Append(W("()"));
    }

    pvSig = blobSignatureDefEntry->sig;
    cbSig = blobSignatureDefEntry->cSig;

    //*****************************************
    // look up functions for TypeDef
    //*****************************************
    // STDMETHOD(FindMethodDefUsingCompare)(
    //     mdTypeDef   classdef,               // [IN] given typedef
    //     LPCSTR      szName,                 // [IN] member name
    //     PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of CLR signature
    //     ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
    //     PSIGCOMPARE pSignatureCompare,      // [IN] Routine to compare signatures
    //     void*       pSignatureArgs,         // [IN] Additional info to supply the compare function
    //     mdMethodDef *pmd) PURE;             // [OUT] matching memberdef
    //

    IMDInternalImport *  pInternalImport = pExternalModule->GetMDImport();

    IbcCompareContext context;
    memset(&context, 0, sizeof(IbcCompareContext));
    context.pModule = this;
    context.enclosingType = enclosingType;
    context.cMatch = 0;
    context.useBestSig = false;

    mdMethodDef mdResult = mdMethodDefNil;
    HRESULT hr = pInternalImport->FindMethodDefUsingCompare(ibcName.tkEnclosingClass, ibcName.szName,
                                                            pvSig, cbSig,
                                                            CompareIbcMethodSigs, (void *) &context,
                                                            &mdResult);
    if (SUCCEEDED(hr))
    {
        _ASSERTE(mdResult != mdMethodDefNil);
    }
    else if (context.useBestSig)
    {
        hr = pInternalImport->FindMethodDefUsingCompare(ibcName.tkEnclosingClass, ibcName.szName,
                                                        context.pvBestSig, context.cbBestSig,
                                                        CompareIbcMethodSigs, (void *) &context,
                                                        &mdResult);
        _ASSERTE(SUCCEEDED(hr));
        _ASSERTE(mdResult != mdMethodDefNil);
    }
    else
    {
        mdResult = mdMethodDefNil;
    }

    return mdResult;
}

TypeHandle Module::LoadIBCTypeHelper(DataImage *image, CORBBTPROF_BLOB_PARAM_SIG_ENTRY *pBlobSigEntry)
{
    CONTRACT(TypeHandle)
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pBlobSigEntry));
    }
    CONTRACT_END

    TypeHandle         loadedType;

    PCCOR_SIGNATURE    pSig = pBlobSigEntry->sig;
    ULONG              cSig = pBlobSigEntry->cSig;

    SigPointer         p(pSig, cSig);

    ZapSig::Context    zapSigContext(this, (void *)this, ZapSig::IbcTokens);
    ZapSig::Context *  pZapSigContext = &zapSigContext;

    EX_TRY
    {
        // This is what ZapSig::FindTypeHandleFromSignature does...
        //
        SigTypeContext typeContext;  // empty type context

        loadedType = p.GetTypeHandleThrowing( this,
                                              &typeContext,
                                              ClassLoader::LoadTypes,
                                              CLASS_LOADED,
                                              FALSE,
                                              NULL,
                                              pZapSigContext);
#if defined(_DEBUG) && !defined(DACCESS_COMPILE)
        g_pConfig->DebugCheckAndForceIBCFailure(EEConfig::CallSite_1);
#endif
    }
    EX_CATCH
    {
        if (g_CorCompileVerboseLevel >= CORCOMPILE_VERBOSE)
            image->GetPreloader()->Error(pBlobSigEntry->blob.token, GET_EXCEPTION());

        loadedType = TypeHandle();
    }
    EX_END_CATCH(SwallowAllExceptions)

    RETURN loadedType;
}

//---------------------------------------------------------------------------------------
//
MethodDesc* Module::LoadIBCMethodHelper(DataImage *image, CORBBTPROF_BLOB_PARAM_SIG_ENTRY * pBlobSigEntry)
{
    CONTRACT(MethodDesc*)
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pBlobSigEntry));
    }
    CONTRACT_END

    MethodDesc* pMethod = NULL;

    PCCOR_SIGNATURE    pSig = pBlobSigEntry->sig;
    ULONG              cSig = pBlobSigEntry->cSig;

    SigPointer p(pSig, cSig);

    ZapSig::Context    zapSigContext(this, (void *)this, ZapSig::IbcTokens);
    ZapSig::Context *  pZapSigContext = &zapSigContext;

    TypeHandle         enclosingType;

    //
    //  First Decode and Load the enclosing type for this method
    //
    EX_TRY
    {
        // This is what ZapSig::FindTypeHandleFromSignature does...
        //
        SigTypeContext typeContext;   // empty type context

        enclosingType = p.GetTypeHandleThrowing( this,
                                  &typeContext,
                                  ClassLoader::LoadTypes,
                                  CLASS_LOADED,
                                  FALSE,
                                  NULL,
                                  pZapSigContext);
        IfFailThrow(p.SkipExactlyOne());
#if defined(_DEBUG) && !defined(DACCESS_COMPILE)
        g_pConfig->DebugCheckAndForceIBCFailure(EEConfig::CallSite_2);
#endif
    }
    EX_CATCH
    {
        if (g_CorCompileVerboseLevel >= CORCOMPILE_VERBOSE)
            image->GetPreloader()->Error(pBlobSigEntry->blob.token, GET_EXCEPTION());

        enclosingType = TypeHandle();
    }
    EX_END_CATCH(SwallowAllExceptions)

    if (enclosingType.IsNull())
        return NULL;

    //
    //  Now Decode and Load the method
    //
    EX_TRY
    {
        MethodTable *pOwnerMT = enclosingType.GetMethodTable();
        _ASSERTE(pOwnerMT != NULL);

        // decode flags
        uint32_t methodFlags;
        IfFailThrow(p.GetData(&methodFlags));
        BOOL isInstantiatingStub = ((methodFlags & ENCODE_METHOD_SIG_InstantiatingStub) == ENCODE_METHOD_SIG_InstantiatingStub);
        BOOL isUnboxingStub = ((methodFlags & ENCODE_METHOD_SIG_UnboxingStub) == ENCODE_METHOD_SIG_UnboxingStub);
        BOOL fMethodNeedsInstantiation = ((methodFlags & ENCODE_METHOD_SIG_MethodInstantiation) == ENCODE_METHOD_SIG_MethodInstantiation);
        BOOL fMethodUsesSlotEncoding = ((methodFlags & ENCODE_METHOD_SIG_SlotInsteadOfToken) == ENCODE_METHOD_SIG_SlotInsteadOfToken);

        if ( fMethodUsesSlotEncoding )
        {
            // get the method desc using slot number
            uint32_t slot;
            IfFailThrow(p.GetData(&slot));

            if (slot >= pOwnerMT->GetNumVtableSlots())
            {
                COMPlusThrow(kTypeLoadException, IDS_IBC_MISSING_EXTERNAL_METHOD);
            }

            pMethod = pOwnerMT->GetMethodDescForSlot(slot);
        }
        else  // otherwise we use the normal metadata MethodDef token encoding and we handle ibc tokens.
        {
            //
            // decode method token
            //
            RID methodRid;
            IfFailThrow(p.GetData(&methodRid));

            mdMethodDef methodToken;

            //
            //  Is our enclosingType from another module?
            //
            if (this == enclosingType.GetModule())
            {
                //
                // The enclosing type is from our module
                // The method token is a normal MethodDef token
                //
                methodToken = TokenFromRid(methodRid, mdtMethodDef);
            }
            else
            {
                //
                // The enclosing type is from an external module
                // The method token is a ibcExternalMethod token
                //
                idExternalType ibcToken = RidToToken(methodRid, ibcExternalMethod);
                methodToken = this->LookupIbcMethodToken(enclosingType, ibcToken);

                if (IsNilToken(methodToken))
                {
                    COMPlusThrow(kTypeLoadException, IDS_IBC_MISSING_EXTERNAL_METHOD);
                }
            }

            SigTypeContext methodTypeContext( enclosingType );
            pMethod = MemberLoader::GetMethodDescFromMemberDefOrRefOrSpec(
                                                  pOwnerMT->GetModule(),
                                                  methodToken,
                                                  &methodTypeContext,
                                                  FALSE,
                                                  FALSE );
        }

        Instantiation inst;

        // Instantiate the method if needed, or create a stub to a static method in a generic class.
        if (fMethodNeedsInstantiation && pMethod->HasMethodInstantiation())
        {
            DWORD nargs = pMethod->GetNumGenericMethodArgs();
            SIZE_T cbMem;

            if (!ClrSafeInt<SIZE_T>::multiply(nargs, sizeof(TypeHandle), cbMem/* passed by ref */))
                ThrowHR(COR_E_OVERFLOW);

            TypeHandle * pInst = (TypeHandle*) _alloca(cbMem);
            SigTypeContext typeContext;   // empty type context

            for (DWORD i = 0; i < nargs; i++)
            {
                TypeHandle curInst;

                curInst = p.GetTypeHandleThrowing( this,
                              &typeContext,
                              ClassLoader::LoadTypes,
                              CLASS_LOADED,
                              FALSE,
                              NULL,
                              pZapSigContext);

                // curInst will be nullptr when the type fails the versioning bubble check
                if (curInst.IsNull() && IsReadyToRunCompilation())
                {
                    COMPlusThrow(kTypeLoadException, IDS_IBC_MISSING_EXTERNAL_TYPE);
                }

                pInst[i] = curInst;
                IfFailThrow(p.SkipExactlyOne());
            }

            inst = Instantiation(pInst, nargs);
        }
        else
        {
            inst = pMethod->LoadMethodInstantiation();
        }

        // We should now be able to create an instantiation for this generic method

        // This must be called even if nargs == 0, in order to create an instantiating
        // stub for static methods in generic classees if needed, also for BoxedEntryPointStubs
        // in non-generic structs.
        const bool allowInstParam = !(isInstantiatingStub || isUnboxingStub);

        pMethod = MethodDesc::FindOrCreateAssociatedMethodDesc(pMethod, pOwnerMT,
                                                               isUnboxingStub,
                                                               inst, allowInstParam);

#if defined(_DEBUG) && !defined(DACCESS_COMPILE)
        g_pConfig->DebugCheckAndForceIBCFailure(EEConfig::CallSite_3);
#endif
    }
    EX_CATCH
    {
        // Catch any kTypeLoadException that we may have thrown above
        //
        image->GetPreloader()->Error(pBlobSigEntry->blob.token, GET_EXCEPTION());
        pMethod = NULL;
    }
    EX_END_CATCH(SwallowAllExceptions)

    RETURN pMethod;
} // Module::LoadIBCMethodHelper

//---------------------------------------------------------------------------------------
//
void Module::ExpandAll(DataImage *image)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(!IsResource());
    }
    CONTRACTL_END

    mdToken tk;
    DWORD assemblyFlags = GetAssembly()->GetFlags();

    //
    // Explicitly load the global class.
    //

    MethodTable *pGlobalMT = GetGlobalMethodTable();

    //
    // Load all classes.  This also fills out the
    // RID maps for the typedefs, method defs,
    // and field defs.
    //

    IMDInternalImport *pInternalImport = GetMDImport();
    {
        HENUMInternalHolder hEnum(pInternalImport);
        hEnum.EnumTypeDefInit();

        while (pInternalImport->EnumNext(&hEnum, &tk))
        {
            TypeHandle t = LoadTypeDefOrRefHelper(image, this, tk);

            if (t.IsNull()) // Skip this type
                continue;

            if (!t.HasInstantiation())
            {
                EEClassHashEntry_t *pBucket = NULL;
                HashDatum           data;
                StackSString        ssFullyQualifiedName;
                mdToken             mdEncloser;
                EEClassHashTable   *pTable = GetAvailableClassHash();

                _ASSERTE(pTable != NULL);

                t.GetName(ssFullyQualifiedName);

                // Convert to UTF8
                StackScratchBuffer scratch;
                LPCUTF8 szFullyQualifiedName = ssFullyQualifiedName.GetUTF8(scratch);

                BOOL isNested = ClassLoader::IsNested(this, tk, &mdEncloser);
                EEClassHashTable::LookupContext sContext;
                pBucket = pTable->GetValue(szFullyQualifiedName, &data, isNested, &sContext);

                if (isNested)
                {
                    while (pBucket != NULL)
                    {
                        _ASSERTE (TypeFromToken(tk) == mdtTypeDef);
                        BOOL match = GetClassLoader()->CompareNestedEntryWithTypeDef( pInternalImport,
                                                                                      mdEncloser,
                                                                                      GetAvailableClassHash(),
                                                                                      pBucket->GetEncloser());
                        if (match)
                            break;

                        pBucket = pTable->FindNextNestedClass(szFullyQualifiedName, &data, &sContext);
                    }
                }

                // Save the typehandle instead of the token in the hash entry so that ngen'ed images
                // don't have to lookup based on token and update this entry
                if ((pBucket != NULL) && !t.IsNull() && t.IsRestored())
                    pBucket->SetData(t.AsPtr());
            }

            DWORD nGenericClassParams = t.GetNumGenericArgs();
            if (nGenericClassParams != 0)
            {
                // For generic types, load the instantiation at Object
                SIZE_T cbMem;
                if (!ClrSafeInt<SIZE_T>::multiply(sizeof(TypeHandle), nGenericClassParams, cbMem/* passed by ref */))
                {
                    ThrowHR(COR_E_OVERFLOW);
                }
                CQuickBytes qbGenericClassArgs;
                TypeHandle *genericClassArgs = reinterpret_cast<TypeHandle*>(qbGenericClassArgs.AllocThrows(cbMem));
                for (DWORD i = 0; i < nGenericClassParams; i++)
                {
                    genericClassArgs[i] = TypeHandle(g_pCanonMethodTableClass);
                }

                TypeHandle thCanonInst = LoadGenericInstantiationHelper(image, this, tk, Instantiation(genericClassArgs, nGenericClassParams));

                // If successful, add the instantiation to the Module's map of generic types instantiated at Object
                if (!thCanonInst.IsNull() && !thCanonInst.IsTypeDesc())
                {
                    MethodTable * pCanonMT = thCanonInst.AsMethodTable();
                    m_GenericTypeDefToCanonMethodTableMap.AddElement(this, RidFromToken(pCanonMT->GetCl()), pCanonMT);
                }
            }
        }
    }

    //
    // Fill out TypeRef RID map
    //

    {
        HENUMInternalHolder hEnum(pInternalImport);
        hEnum.EnumAllInit(mdtTypeRef);

        while (pInternalImport->EnumNext(&hEnum, &tk))
        {
            mdToken tkResolutionScope = mdTokenNil;
            pInternalImport->GetResolutionScopeOfTypeRef(tk, &tkResolutionScope);
            TypeHandle t = LoadTypeDefOrRefHelper(image, this, tk);

            if (t.IsNull()) // Skip this type
                continue;
        }
    }

    //
    // Load all type specs
    //

    {
        HENUMInternalHolder hEnum(pInternalImport);
        hEnum.EnumAllInit(mdtTypeSpec);

        while (pInternalImport->EnumNext(&hEnum, &tk))
        {
            ULONG cSig;
            PCCOR_SIGNATURE pSig;

            IfFailThrow(pInternalImport->GetTypeSpecFromToken(tk, &pSig, &cSig));

            // Load all types specs that do not contain variables
            if (SigPointer(pSig, cSig).IsPolyType(NULL) == hasNoVars)
            {
                LoadTypeSpecHelper(image, this, tk, pSig, cSig);
            }
        }
    }

    //
    // Load all the reported parameterized types and methods
    //
    CorProfileData *  profileData = this->GetProfileData();
    CORBBTPROF_BLOB_ENTRY *pBlobEntry = profileData ? profileData->GetBlobStream() : NULL;

    if (pBlobEntry != NULL)
    {
        while (pBlobEntry->TypeIsValid())
        {
            if (TypeFromToken(pBlobEntry->token) == ibcTypeSpec)
            {
                _ASSERTE(pBlobEntry->type == ParamTypeSpec);
                CORBBTPROF_BLOB_PARAM_SIG_ENTRY *pBlobSigEntry = (CORBBTPROF_BLOB_PARAM_SIG_ENTRY *) pBlobEntry;

                TypeHandle th = LoadIBCTypeHelper(image, pBlobSigEntry);

                if (!th.IsNull())
                {
                    image->GetPreloader()->TriageTypeForZap(th, TRUE);
                }
            }
            else if (TypeFromToken(pBlobEntry->token) == ibcMethodSpec)
            {
                _ASSERTE(pBlobEntry->type == ParamMethodSpec);
                CORBBTPROF_BLOB_PARAM_SIG_ENTRY *pBlobSigEntry = (CORBBTPROF_BLOB_PARAM_SIG_ENTRY *) pBlobEntry;

                MethodDesc *pMD = LoadIBCMethodHelper(image, pBlobSigEntry);
            }
            pBlobEntry = pBlobEntry->GetNextEntry();
        }
        _ASSERTE(pBlobEntry->type == EndOfBlobStream);
    }

    if (profileData)
    {
        //
        // Record references to all of the hot methods specifiled by MethodProfilingData array
        // We call MethodReferencedByCompiledCode to indicate that we plan on compiling this method
        //
        CORBBTPROF_TOKEN_INFO * pMethodProfilingData = profileData->GetTokenFlagsData(MethodProfilingData);
        DWORD                   cMethodProfilingData = profileData->GetTokenFlagsCount(MethodProfilingData);

        for (unsigned int i = 0; (i < cMethodProfilingData); i++)
        {
            mdToken token = pMethodProfilingData[i].token;
            DWORD   profilingFlags = pMethodProfilingData[i].flags;

            // We call MethodReferencedByCompiledCode only when the profile data indicates that
            // we executed (i.e read) the code for the method
            //
            if (profilingFlags & (1 << ReadMethodCode))
            {
                if (TypeFromToken(token) == mdtMethodDef)
                {
                    MethodDesc *  pMD = LookupMethodDef(token);

                    if (pMD != NULL)
                    {
                        //
                        // Record a reference to a hot non-generic method
                        //
                        image->GetPreloader()->MethodReferencedByCompiledCode((CORINFO_METHOD_HANDLE)pMD);
                    }
                }
                else if (TypeFromToken(token) == ibcMethodSpec)
                {
                    CORBBTPROF_BLOB_PARAM_SIG_ENTRY *pBlobSigEntry = profileData->GetBlobSigEntry(token);

                    if (pBlobSigEntry != NULL)
                    {
                        _ASSERTE(pBlobSigEntry->blob.token == token);
                        MethodDesc * pMD = LoadIBCMethodHelper(image, pBlobSigEntry);

                        if (pMD != NULL)
                        {
                            // Occasionally a non-instantiated generic method shows up in the IBC data, we should NOT compile it.
                            if (!pMD->IsTypicalMethodDefinition())
                            {
                                //
                                // Record a reference to a hot instantiated generic method
                                //
                                image->GetPreloader()->MethodReferencedByCompiledCode((CORINFO_METHOD_HANDLE)pMD);
                            }
                        }
                    }
                }
            }
        }
    }

    {
        //
        // Fill out MemberRef RID map and va sig cookies for
        // varargs member refs.
        //

        HENUMInternalHolder hEnum(pInternalImport);
        hEnum.EnumAllInit(mdtMemberRef);

        while (pInternalImport->EnumNext(&hEnum, &tk))
        {
            mdTypeRef parent;
            IfFailThrow(pInternalImport->GetParentOfMemberRef(tk, &parent));

            // If the MethodRef has a TypeSpec as a parent (i.e. refers to a method on an array type
            // or on a generic class), then it could in turn refer to type variables of
            // an unknown class/method. So we don't preresolve any MemberRefs which have TypeSpecs as
            // parents.  The RID maps are not filled out for such tokens anyway.
            if (TypeFromToken(parent) != mdtTypeSpec)
            {
                GetDescFromMemberRefHelper(image, this, tk);
            }
        }
    }

    //
    // Fill out binder
    //

    if (m_pBinder != NULL)
    {
        m_pBinder->BindAll();
    }

} // Module::ExpandAll

/* static */
void Module::SaveMethodTable(DataImage *    image,
                             MethodTable *  pMT,
                             DWORD          profilingFlags)
{
    STANDARD_VM_CONTRACT;

    if (image->IsStored(pMT))
        return;

    pMT->Save(image, profilingFlags);
}


/* static */
void Module::SaveTypeHandle(DataImage *  image,
                            TypeHandle   t,
                            DWORD        profilingFlags)
{
    STANDARD_VM_CONTRACT;

    t.CheckRestore();
    if (t.IsTypeDesc())
    {
        TypeDesc *pTD = t.AsTypeDesc();
        if (!image->IsStored(pTD))
        {
            pTD->Save(image);
        }
    }
    else
    {
        MethodTable *pMT = t.AsMethodTable();
        if (pMT != NULL && !image->IsStored(pMT))
        {
            SaveMethodTable(image, pMT, profilingFlags);
            _ASSERTE(image->IsStored(pMT));
        }
    }
#ifdef _DEBUG
    if (LoggingOn(LF_JIT, LL_INFO100))
    {
        Module *pPrefModule = Module::GetPreferredZapModuleForTypeHandle(t);
        if (image->GetModule() != pPrefModule)
        {
            StackSString typeName;
            t.CheckRestore();
            TypeString::AppendTypeDebug(typeName, t);
            LOG((LF_ZAP, LL_INFO100, "The type %S was saved outside its preferred module %S\n", typeName.GetUnicode(), pPrefModule->GetPath().GetUnicode()));
        }
    }
#endif // _DEBUG
}

#ifndef DACCESS_COMPILE

void ModuleCtorInfo::Save(DataImage *image, CorProfileData *profileData)
{
    STANDARD_VM_CONTRACT;

    if (!numElements)
        return;

    DWORD i = 0;
    DWORD totalBoxedStatics = 0;

    // sort the tables so that
    // - the hot ppMT entries are at the beginning of the ppMT table
    // - the hot cctor entries are at the beginning of the cctorInfoHot table
    // - the cold cctor entries are at the end, and we make cctorInfoCold point
    //   the first cold entry
    //
    // the invariant in this loop is:
    // items 0...numElementsHot-1 are hot
    // items numElementsHot...i-1 are cold
    for (i = 0; i < numElements; i++)
    {
        MethodTable *ppMTTemp = ppMT[i].GetValue();

        // Count the number of boxed statics along the way
        totalBoxedStatics += ppMTTemp->GetNumBoxedRegularStatics();

        bool hot = true; // if there's no profiling data, assume the entries are all hot.
        if (profileData && profileData->GetTokenFlagsData(TypeProfilingData))
        {
            if ((profileData->GetTypeProfilingFlagsOfToken(ppMTTemp->GetCl()) & (1 << ReadCCtorInfo)) == 0)
                hot = false;
        }
        if (hot)
        {
            // swap ppMT[i] and ppMT[numElementsHot] to maintain the loop invariant
            ppMT[i].SetValue(ppMT[numElementsHot].GetValue());
            ppMT[numElementsHot].SetValue(ppMTTemp);

            numElementsHot++;
        }
    }

    numHotHashes = numElementsHot ? RoundUpToPower2((numElementsHot * sizeof(PTR_MethodTable)) / CACHE_LINE_SIZE) : 0;
    numColdHashes = (numElements - numElementsHot) ? RoundUpToPower2(((numElements - numElementsHot) *
                                                                    sizeof(PTR_MethodTable)) / CACHE_LINE_SIZE) : 0;

    LOG((LF_ZAP, LL_INFO10, "ModuleCtorInfo::numHotHashes:  0x%4x\n", numHotHashes));
    if (numColdHashes != 0)
    {
        LOG((LF_ZAP, LL_INFO10, "ModuleCtorInfo::numColdHashes: 0x%4x\n", numColdHashes));
    }

    // The "plus one" is so we can store the offset to the end of the array at the end of
    // the hashoffsets arrays, enabling faster lookups.
    hotHashOffsets = new DWORD[numHotHashes + 1];
    coldHashOffsets = new DWORD[numColdHashes + 1];

    DWORD *hashArray = new DWORD[numElements];

    for (i = 0; i < numElementsHot; i++)
    {
        hashArray[i] = GenerateHash(ppMT[i].GetValue(), HOT);
    }
    for (i = numElementsHot; i < numElements; i++)
    {
        hashArray[i] = GenerateHash(ppMT[i].GetValue(), COLD);
    }

    // Sort the two arrays by hash values to create regions with the same hash values.
    ClassCtorInfoEntryArraySort cctorInfoHotSort(hashArray, ppMT, numElementsHot);
    ClassCtorInfoEntryArraySort cctorInfoColdSort(hashArray + numElementsHot, ppMT + numElementsHot,
                                                    numElements - numElementsHot);
    cctorInfoHotSort.Sort();
    cctorInfoColdSort.Sort();

    // Generate the indices that index into the correct "hash region" in the hot part of the ppMT array, and store
    // them in the hotHashOffests arrays.
    DWORD curHash = 0;
    i = 0;
    while (i < numElementsHot)
    {
        if (curHash < hashArray[i])
        {
            hotHashOffsets[curHash++] = i;
        }
        else if (curHash == hashArray[i])
        {
            hotHashOffsets[curHash++] = i++;
        }
        else
        {
            i++;
        }
    }
    while (curHash <= numHotHashes)
    {
        hotHashOffsets[curHash++] = numElementsHot;
    }

    // Generate the indices that index into the correct "hash region" in the hot part of the ppMT array, and store
    // them in the coldHashOffsets arrays.
    curHash = 0;
    i = numElementsHot;
    while (i < numElements)
    {
        if (curHash < hashArray[i])
        {
            coldHashOffsets[curHash++] = i;
        }
        else if (curHash == hashArray[i])
        {
            coldHashOffsets[curHash++] = i++;
        }
        else i++;
    }
    while (curHash <= numColdHashes)
    {
        coldHashOffsets[curHash++] = numElements;
    }

    delete[] hashArray;


    cctorInfoHot    = new ClassCtorInfoEntry[numElements];

    // make cctorInfoCold point to the first cold element
    cctorInfoCold   = cctorInfoHot + numElementsHot;

    ppHotGCStaticsMTs   = (totalBoxedStatics != 0) ? new RelativeFixupPointer<PTR_MethodTable>[totalBoxedStatics] : NULL;
    numHotGCStaticsMTs  = totalBoxedStatics;

    DWORD iGCStaticMT = 0;

    for (i = 0; i < numElements; i++)
    {
        if (numElements == numElementsHot)
        {
            numHotGCStaticsMTs  = iGCStaticMT;
            numColdGCStaticsMTs = (totalBoxedStatics - iGCStaticMT);

            // make ppColdGCStaticsMTs point to the first cold element
            ppColdGCStaticsMTs = ppHotGCStaticsMTs + numHotGCStaticsMTs;
        }

        MethodTable* pMT = ppMT[i].GetValue();
        ClassCtorInfoEntry* pEntry = &cctorInfoHot[i];

        WORD numBoxedStatics = pMT->GetNumBoxedRegularStatics();
        pEntry->numBoxedStatics = numBoxedStatics;
        pEntry->hasFixedAddressVTStatics = !!pMT->HasFixedAddressVTStatics();

        FieldDesc *pField = pMT->HasGenericsStaticsInfo() ?
            pMT->GetGenericsStaticFieldDescs() : (pMT->GetApproxFieldDescListRaw() + pMT->GetNumIntroducedInstanceFields());
        FieldDesc *pFieldEnd = pField + pMT->GetNumStaticFields();

        pEntry->firstBoxedStaticOffset = (DWORD)-1;
        pEntry->firstBoxedStaticMTIndex = (DWORD)-1;

        DWORD numFoundBoxedStatics = 0;
        while (pField < pFieldEnd)
        {
            _ASSERTE(pField->IsStatic());

            if (!pField->IsSpecialStatic() && pField->IsByValue())
            {
                if (pEntry->firstBoxedStaticOffset == (DWORD)-1)
                {
                    pEntry->firstBoxedStaticOffset = pField->GetOffset();
                    pEntry->firstBoxedStaticMTIndex = iGCStaticMT;
                }
                _ASSERTE(pField->GetOffset() - pEntry->firstBoxedStaticOffset
                    == (iGCStaticMT - pEntry->firstBoxedStaticMTIndex) * sizeof(MethodTable*));

                TypeHandle th = pField->GetFieldTypeHandleThrowing();
                ppHotGCStaticsMTs[iGCStaticMT++].SetValueMaybeNull(th.GetMethodTable());

                numFoundBoxedStatics++;
            }
            pField++;
        }
        _ASSERTE(numBoxedStatics == numFoundBoxedStatics);
    }
    _ASSERTE(iGCStaticMT == totalBoxedStatics);

    if (numElementsHot > 0)
    {
        image->StoreStructure(cctorInfoHot,
                                sizeof(ClassCtorInfoEntry) * numElementsHot,
                                DataImage::ITEM_MODULE_CCTOR_INFO_HOT);

        image->StoreStructure(hotHashOffsets,
                                sizeof(DWORD) * (numHotHashes + 1),
                                DataImage::ITEM_MODULE_CCTOR_INFO_HOT);
    }

    if (numElements > 0)
        image->StoreStructure(ppMT,
                                sizeof(RelativePointer<MethodTable *>) * numElements,
                                DataImage::ITEM_MODULE_CCTOR_INFO_HOT);

    if (numElements > numElementsHot)
    {
        image->StoreStructure(cctorInfoCold,
                                sizeof(ClassCtorInfoEntry) * (numElements - numElementsHot),
                                DataImage::ITEM_MODULE_CCTOR_INFO_COLD);

        image->StoreStructure(coldHashOffsets,
                                sizeof(DWORD) * (numColdHashes + 1),
                                DataImage::ITEM_MODULE_CCTOR_INFO_COLD);
    }

    if ( numHotGCStaticsMTs )
    {
        // Save the mt templates
        image->StoreStructure( ppHotGCStaticsMTs, numHotGCStaticsMTs * sizeof(RelativeFixupPointer<MethodTable*>),
                                DataImage::ITEM_GC_STATIC_HANDLES_HOT);
    }
    else
    {
        ppHotGCStaticsMTs = NULL;
    }

    if ( numColdGCStaticsMTs )
    {
        // Save the hot mt templates
        image->StoreStructure( ppColdGCStaticsMTs, numColdGCStaticsMTs * sizeof(RelativeFixupPointer<MethodTable*>),
                                DataImage::ITEM_GC_STATIC_HANDLES_COLD);
    }
    else
    {
        ppColdGCStaticsMTs = NULL;
    }
}

#endif // !DACCESS_COMPILE

bool Module::AreAllClassesFullyLoaded()
{
    STANDARD_VM_CONTRACT;

    // Adjust for unused space
    IMDInternalImport *pImport = GetMDImport();

    HENUMInternalHolder hEnum(pImport);
    hEnum.EnumAllInit(mdtTypeDef);

    mdTypeDef token;
    while (pImport->EnumNext(&hEnum, &token))
    {
        _ASSERTE(TypeFromToken(token) == mdtTypeDef);

        // Special care has to been taken with COR_GLOBAL_PARENT_TOKEN, as the class
        // may not be needed, (but we have to distinguish between not needed and threw error).
        if (token == COR_GLOBAL_PARENT_TOKEN &&
            !NeedsGlobalMethodTable())
        {
            // No EEClass for this token if there was no need for a global method table
            continue;
        }

        TypeHandle th = LookupTypeDef(token);
        if (th.IsNull())
            return false;

        if (!th.AsMethodTable()->IsFullyLoaded())
            return false;
    }

    return true;
}

void Module::PrepareTypesForSave(DataImage *image)
{
    STANDARD_VM_CONTRACT;

    //
    // Prepare typedefs
    //
    {
        LookupMap<PTR_MethodTable>::Iterator typeDefIter(&m_TypeDefToMethodTableMap);
        while (typeDefIter.Next())
        {
            MethodTable * pMT = typeDefIter.GetElement();

            if (pMT == NULL || !pMT->IsFullyLoaded())
                continue;


        }
    }

    //
    // Prepare typespecs
    //
    {
        // Create a local copy in case the new elements are added to the hashtable during population
        InlineSArray<TypeHandle, 20> pTypes;

        // Make sure the iterator is destroyed before there is a chance of loading new types
        {
            EETypeHashTable::Iterator it(m_pAvailableParamTypes);
            EETypeHashEntry *pEntry;
            while (m_pAvailableParamTypes->FindNext(&it, &pEntry))
            {
                TypeHandle t = pEntry->GetTypeHandle();

                if (t.IsTypeDesc())
                    continue;

                if (!image->GetPreloader()->IsTypeInTransitiveClosureOfInstantiations(CORINFO_CLASS_HANDLE(t.AsPtr())))
                    continue;

                pTypes.Append(t);
            }
        }

    }

    image->GetPreloader()->TriageForZap(FALSE, FALSE);
}

static const char* const MethodTableRestoreReasonDescription[TotalMethodTables + 1] =
{
    #undef RESTORE_REASON_FUNC
    #define RESTORE_REASON_FUNC(s) #s,

    METHODTABLE_RESTORE_REASON()

    #undef RESTORE_REASON

    "TotalMethodTablesEvaluated"
};


// MethodDescByMethodTableTraits could be a local class in Module::Save(), but g++ doesn't like
// instantiating templates with private classes.
class MethodDescByMethodTableTraits : public NoRemoveSHashTraits< DefaultSHashTraits<MethodDesc *> >
{
public:
    typedef MethodTable * key_t;
    static MethodDesc * Null() { return NULL; }
    static bool IsNull(MethodDesc * pMD) { return pMD == NULL; }
    static MethodTable * GetKey(MethodDesc * pMD) { return pMD->GetMethodTable_NoLogging(); }
    static count_t Hash(MethodTable * pMT) { LIMITED_METHOD_CONTRACT; return (count_t) (UINT_PTR) pMT->GetTypeDefRid_NoLogging(); }
    static BOOL Equals(MethodTable * pMT1, MethodTable * pMT2)
    {
        return pMT1 == pMT2;
    }
};

void Module::Save(DataImage *image)
{
    STANDARD_VM_CONTRACT;

    // Precompute type specific auxiliary information saved into NGen image
    // Note that this operation can load new types.
    PrepareTypesForSave(image);

    // Cache values of all persisted flags computed from custom attributes
    IsNoStringInterning();
    IsRuntimeWrapExceptions();
    IsPreV4Assembly();

    HasDefaultDllImportSearchPathsAttribute();

    // Precompute property information to avoid runtime metadata lookup
    PopulatePropertyInfoMap();

    // Any any elements and compute values of any LookupMap flags that were not available previously
    FinalizeLookupMapsPreSave(image);

    //
    // Save the module
    //

    ZapStoredStructure * pModuleNode = image->StoreStructure(this, sizeof(Module),
                                    DataImage::ITEM_MODULE);

    m_pNGenLayoutInfo = (NGenLayoutInfo *)(void *)image->GetModule()->GetLoaderAllocator()->
        GetHighFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(NGenLayoutInfo)));
    image->StoreStructure(m_pNGenLayoutInfo, sizeof(NGenLayoutInfo), DataImage::ITEM_BINDER_ITEMS);

    //
    // If we are NGening, we don't need to keep a list of va
    // sig cookies, as we already have a complete set (of course we do
    // have to persist the cookies themselves, though.
    //

    //
    // Initialize maps of child data structures.  Note that each tables's blocks are
    // concantentated to a single block in the process.
    //
    CorProfileData * profileData = GetProfileData();

    m_TypeRefToMethodTableMap.Save(image, DataImage::ITEM_TYPEREF_MAP, profileData, mdtTypeRef);
    image->BindPointer(&m_TypeRefToMethodTableMap, pModuleNode, offsetof(Module, m_TypeRefToMethodTableMap));

    if(m_pMemberRefToDescHashTable)
        m_pMemberRefToDescHashTable->Save(image, profileData);

    m_TypeDefToMethodTableMap.Save(image, DataImage::ITEM_TYPEDEF_MAP, profileData, mdtTypeDef);
    image->BindPointer(&m_TypeDefToMethodTableMap, pModuleNode, offsetof(Module, m_TypeDefToMethodTableMap));

    m_MethodDefToDescMap.Save(image, DataImage::ITEM_METHODDEF_MAP, profileData, mdtMethodDef);
    image->BindPointer(&m_MethodDefToDescMap, pModuleNode, offsetof(Module, m_MethodDefToDescMap));

    m_FieldDefToDescMap.Save(image, DataImage::ITEM_FIELDDEF_MAP, profileData, mdtFieldDef);
    image->BindPointer(&m_FieldDefToDescMap, pModuleNode, offsetof(Module, m_FieldDefToDescMap));

    m_GenericParamToDescMap.Save(image, DataImage::ITEM_GENERICPARAM_MAP, profileData, mdtGenericParam);
    image->BindPointer(&m_GenericParamToDescMap, pModuleNode, offsetof(Module, m_GenericParamToDescMap));

    m_GenericTypeDefToCanonMethodTableMap.Save(image, DataImage::ITEM_GENERICTYPEDEF_MAP, profileData, mdtTypeDef);
    image->BindPointer(&m_GenericTypeDefToCanonMethodTableMap, pModuleNode, offsetof(Module, m_GenericTypeDefToCanonMethodTableMap));

    if (m_pAvailableClasses)
        m_pAvailableClasses->Save(image, profileData);

    //
    // Also save the parent maps; the contents will
    // need to be rewritten, but we can allocate the
    // space in the image.
    //

    // these items have no hot list and no attribution
    m_FileReferencesMap.Save(image, DataImage::ITEM_FILEREF_MAP, profileData, 0);
    image->BindPointer(&m_FileReferencesMap, pModuleNode, offsetof(Module, m_FileReferencesMap));

    m_ManifestModuleReferencesMap.Save(image, DataImage::ITEM_ASSEMREF_MAP, profileData, 0);
    image->BindPointer(&m_ManifestModuleReferencesMap, pModuleNode, offsetof(Module, m_ManifestModuleReferencesMap));

    m_MethodDefToPropertyInfoMap.Save(image, DataImage::ITEM_PROPERTYINFO_MAP, profileData, 0, TRUE /*fCopyValues*/);
    image->BindPointer(&m_MethodDefToPropertyInfoMap, pModuleNode, offsetof(Module, m_MethodDefToPropertyInfoMap));

    if (m_pBinder != NULL)
        m_pBinder->Save(image);

    if (profileData)
    {
        // Store types.

        // Saving hot things first is a very good thing, because we place items
        // in the order they are saved and things that have hot items are also
        // more likely to have their other structures touched, hence these should
        // also be placed together, at least if we don't have any further information to go on.
        // Note we place particular hot items with more care in the Arrange phase.
        //
        CORBBTPROF_TOKEN_INFO * pTypeProfilingData = profileData->GetTokenFlagsData(TypeProfilingData);
        DWORD                   cTypeProfilingData = profileData->GetTokenFlagsCount(TypeProfilingData);

        for (unsigned int i = 0; i < cTypeProfilingData; i++)
        {
            CORBBTPROF_TOKEN_INFO *entry = &pTypeProfilingData[i];
            mdToken token = entry->token;
            DWORD   flags = entry->flags;
#if defined(_DEBUG) && !defined(DACCESS_COMPILE)
            g_pConfig->DebugCheckAndForceIBCFailure(EEConfig::CallSite_4);
#endif

            if ((flags & (1 << ReadMethodTable)) == 0)
                continue;

            if (TypeFromToken(token) == mdtTypeDef)
            {
                MethodTable *pMT = LookupTypeDef(token).GetMethodTable();
                if (pMT && pMT->IsFullyLoaded())
                {
                    SaveMethodTable(image, pMT, flags);
                }
            }
            else  if (TypeFromToken(token) == ibcTypeSpec)
            {
                CORBBTPROF_BLOB_ENTRY *pBlobEntry = profileData->GetBlobStream();
                if (pBlobEntry)
                {
                    while (pBlobEntry->TypeIsValid())
                    {
                        if (TypeFromToken(pBlobEntry->token) == ibcTypeSpec)
                        {
                            _ASSERTE(pBlobEntry->type == ParamTypeSpec);

                            if (pBlobEntry->token == token)
                            {
                                CORBBTPROF_BLOB_PARAM_SIG_ENTRY *pBlobSigEntry = (CORBBTPROF_BLOB_PARAM_SIG_ENTRY *) pBlobEntry;
                                TypeHandle th = LoadIBCTypeHelper(image, pBlobSigEntry);

                                if (!th.IsNull())
                                {
                                    // When we have stale IBC data the type could have been rejected from this image.
                                    if (image->GetPreloader()->IsTypeInTransitiveClosureOfInstantiations(CORINFO_CLASS_HANDLE(th.AsPtr())))
                                    {
                                        SaveTypeHandle(image, th, flags);
                                    }
                                }
                            }
                        }
                        pBlobEntry = pBlobEntry->GetNextEntry();
                    }
                    _ASSERTE(pBlobEntry->type == EndOfBlobStream);
                }
            }
        }

        if (m_pAvailableParamTypes != NULL)
        {
            // If we have V1 IBC data then we save the hot
            //  out-of-module generic instantiations here

            CORBBTPROF_TOKEN_INFO * tokens_begin = profileData ? profileData->GetTokenFlagsData(GenericTypeProfilingData) : NULL;
            CORBBTPROF_TOKEN_INFO * tokens_end = tokens_begin + (profileData ? profileData->GetTokenFlagsCount(GenericTypeProfilingData) : 0);

            if (tokens_begin != tokens_end)
            {
                SArray<CORBBTPROF_TOKEN_INFO> tokens(tokens_begin, tokens_end);
                tokens_begin = &tokens[0];
                tokens_end = tokens_begin + tokens.GetCount();

                util::sort(tokens_begin, tokens_end);

                // enumerate AvailableParamTypes map and find all hot generic instantiations
                EETypeHashTable::Iterator it(m_pAvailableParamTypes);
                EETypeHashEntry *pEntry;
                while (m_pAvailableParamTypes->FindNext(&it, &pEntry))
                {
                    TypeHandle t = pEntry->GetTypeHandle();
#if defined(_DEBUG) && !defined(DACCESS_COMPILE)
                    g_pConfig->DebugCheckAndForceIBCFailure(EEConfig::CallSite_5);
#endif

                    if (t.HasInstantiation())
                    {
                        SString tokenName;
                        t.GetName(tokenName);
                        unsigned cur_token = tokenName.Hash() & 0xffff;

                        CORBBTPROF_TOKEN_INFO * found = util::lower_bound(tokens_begin, tokens_end, CORBBTPROF_TOKEN_INFO(cur_token));
                        if (found != tokens_end && found->token == cur_token && (found->flags & (1 << ReadMethodTable)))
                        {
                            // When we have stale IBC data the type could have been rejected from this image.
                            if (image->GetPreloader()->IsTypeInTransitiveClosureOfInstantiations(CORINFO_CLASS_HANDLE(t.AsPtr())))
                                SaveTypeHandle(image, t, found->flags);
                        }
                    }
                }
            }
        }
    }

    //
    // Now save any types in the TypeDefToMethodTableMap map

    {
        LookupMap<PTR_MethodTable>::Iterator typeDefIter(&m_TypeDefToMethodTableMap);

        while (typeDefIter.Next())
        {
            MethodTable * pMT = typeDefIter.GetElement();

            if (pMT != NULL &&
                !image->IsStored(pMT) && pMT->IsFullyLoaded())
            {
                image->BeginAssociatingStoredObjectsWithMethodTable(pMT);
                SaveMethodTable(image, pMT, 0);
                image->EndAssociatingStoredObjectsWithMethodTable();
            }
        }
    }

    //
    // Now save any TypeDescs in m_GenericParamToDescMap map

    {
        LookupMap<PTR_TypeVarTypeDesc>::Iterator genericParamIter(&m_GenericParamToDescMap);

        while (genericParamIter.Next())
        {
            TypeVarTypeDesc *pTD = genericParamIter.GetElement();

            if (pTD != NULL)
            {
                pTD->Save(image);
            }
        }
    }

#ifdef _DEBUG
    SealGenericTypesAndMethods();
#endif

    //
    // Now save any  types in the AvailableParamTypes map
    //
    if (m_pAvailableParamTypes != NULL)
    {
        EETypeHashTable::Iterator it(m_pAvailableParamTypes);
        EETypeHashEntry *pEntry;
        while (m_pAvailableParamTypes->FindNext(&it, &pEntry))
        {
            TypeHandle t = pEntry->GetTypeHandle();

            if (image->GetPreloader()->IsTypeInTransitiveClosureOfInstantiations(CORINFO_CLASS_HANDLE(t.AsPtr())))
            {
                if (t.GetCanonicalMethodTable() != NULL)
                {
                    image->BeginAssociatingStoredObjectsWithMethodTable(t.GetCanonicalMethodTable());
                    SaveTypeHandle(image, t, 0);
                    image->EndAssociatingStoredObjectsWithMethodTable();
                }
                else
                {
                    SaveTypeHandle(image, t, 0);
                }
            }
        }
    }

    //
    // Now save any methods in the InstMethodHashTable
    //
    if (m_pInstMethodHashTable != NULL)
    {
        //
        // Find all MethodDescs that we are going to save, and hash them with MethodTable as the key
        //

        typedef SHash<MethodDescByMethodTableTraits> MethodDescByMethodTableHash;

        MethodDescByMethodTableHash methodDescs;

        InstMethodHashTable::Iterator it(m_pInstMethodHashTable);
        InstMethodHashEntry *pEntry;
        while (m_pInstMethodHashTable->FindNext(&it, &pEntry))
        {
            MethodDesc *pMD = pEntry->GetMethod();

            _ASSERTE(!pMD->IsTightlyBoundToMethodTable());

            if (!image->IsStored(pMD) &&
                image->GetPreloader()->IsMethodInTransitiveClosureOfInstantiations(CORINFO_METHOD_HANDLE(pMD)))
            {
                methodDescs.Add(pMD);
            }
        }

        //
        // Save all MethodDescs on the same MethodTable using one chunk builder
        //

        for (MethodDescByMethodTableHash::Iterator i1 = methodDescs.Begin(), end1 = methodDescs.End(); i1 != end1; i1++)
        {
            MethodDesc * pMD = *(i1);
            if (image->IsStored(pMD))
                continue;

            MethodTable * pMT = pMD->GetMethodTable();

            MethodDesc::SaveChunk methodDescSaveChunk(image);

            for (MethodDescByMethodTableHash::KeyIterator i2 = methodDescs.Begin(pMT), end2 = methodDescs.End(pMT); i2 != end2; i2++)
            {
                _ASSERTE(!image->IsStored(*i2));
                methodDescSaveChunk.Append(*i2);
            }

            methodDescSaveChunk.Save();
        }
    }

    // Now save the tables themselves
    if (m_pAvailableParamTypes != NULL)
    {
        m_pAvailableParamTypes->Save(image, this, profileData);
    }

    if (m_pInstMethodHashTable != NULL)
    {
        m_pInstMethodHashTable->Save(image, profileData);
    }

    {
        MethodTable * pStubMT = GetILStubCache()->GetStubMethodTable();
        if (pStubMT != NULL)
        {
            SaveMethodTable(image, pStubMT, 0);
        }
    }

    if (m_pStubMethodHashTable != NULL)
    {
        m_pStubMethodHashTable->Save(image, profileData);
    }

    // Compute and save the property name set
    PrecomputeMatchingProperties(image);
    image->StoreStructure(m_propertyNameSet,
                          m_nPropertyNameSet * sizeof(BYTE),
                          DataImage::ITEM_PROPERTY_NAME_SET);


    // Sort the list of RVA statics in an ascending order wrt the RVA
    // and save them.
    image->SaveRvaStructure();

    // Save static data
    LOG((LF_CLASSLOADER, LL_INFO10000, "STATICS: Saving module static data\n"));

    // We have this scenario where ngen will fail to load some classes but will generate
    // a valid exe, or it will choose not to save some loaded classes due to some error
    // conditions, where statics will be committed at runtime for the classes that ngen
    // wasn't able to load or save. So we can't cut down the static block size blindly if we've
    // failed to load or save any class. We don't think this scenario deserves complicated code
    // paths to get the extra working set perf (you would be pulling in the jitter if
    // you need any of these classes), So we are basically simplifying this down, if we failed
    // to load or save any class we won't compress the statics block and will persist the original
    // estimation.

    // All classes were loaded and saved, cut down the block
    if (AreAllClassesFullyLoaded())
    {
        // Set a mark indicating we had all our classes loaded
        m_pRegularStaticOffsets = (PTR_DWORD) NGEN_STATICS_ALLCLASSES_WERE_LOADED;
        m_pThreadStaticOffsets = (PTR_DWORD) NGEN_STATICS_ALLCLASSES_WERE_LOADED;
    }
    else
    {
        // Since not all of the classes loaded we want to zero the pointers to the offset tables so they'll be
        // recalculated at runtime. But we can't do that here since we might try to reload some of the failed
        // types during the arrange phase (as the result of trying to parse profiling data). So we'll defer
        // zero'ing anything until the fixup phase.

        // Not all classes were stored, revert to uncompressed maps to support run-time changes
        m_TypeDefToMethodTableMap.ConvertSavedMapToUncompressed(image, DataImage::ITEM_TYPEDEF_MAP);
        m_MethodDefToDescMap.ConvertSavedMapToUncompressed(image, DataImage::ITEM_METHODDEF_MAP);
    }

    m_ModuleCtorInfo.Save(image, profileData);
    image->BindPointer(&m_ModuleCtorInfo, pModuleNode, offsetof(Module, m_ModuleCtorInfo));

    if (m_pDynamicStaticsInfo)
    {
        image->StoreStructure(m_pDynamicStaticsInfo, m_maxDynamicEntries*sizeof(DynamicStaticsInfo),
                                          DataImage::ITEM_DYNAMIC_STATICS_INFO_TABLE);
    }

    InlineTrackingMap *inlineTrackingMap = image->GetInlineTrackingMap();
    if (inlineTrackingMap)
    {
        m_pPersistentInlineTrackingMapNGen = new (image->GetHeap()) PersistentInlineTrackingMapNGen(this);
        m_pPersistentInlineTrackingMapNGen->Save(image, inlineTrackingMap);
    }

    if (m_pNgenStats && g_CorCompileVerboseLevel >= CORCOMPILE_STATS)
    {
        GetSvcLogger()->Printf ("%-35s: %s\n", "MethodTable Restore Reason", "Count");
        DWORD dwTotal = 0;
        for (int i=0; i<TotalMethodTables; i++)
        {
            GetSvcLogger()->Printf ("%-35s: %d\n", MethodTableRestoreReasonDescription[i], m_pNgenStats->MethodTableRestoreNumReasons[i]);
            dwTotal += m_pNgenStats->MethodTableRestoreNumReasons[i];
        }
        GetSvcLogger()->Printf ("%-35s: %d\n", "TotalMethodTablesNeedRestore", dwTotal);
        GetSvcLogger()->Printf ("%-35s: %d\n", MethodTableRestoreReasonDescription[TotalMethodTables], m_pNgenStats->MethodTableRestoreNumReasons[TotalMethodTables]);
    }
}


#ifdef _DEBUG
//
// We call these methods to seal the
// lists: m_pAvailableClasses and m_pAvailableParamTypes
//
void Module::SealGenericTypesAndMethods()
{
    LIMITED_METHOD_CONTRACT;
    // Enforce that after this point in ngen that no more types or methods will be loaded.
    //
    // We increment the seal count here and only decrement it after we have completed the ngen image
    //
    if (m_pAvailableParamTypes != NULL)
    {
        m_pAvailableParamTypes->Seal();
    }
    if (m_pInstMethodHashTable != NULL)
    {
        m_pInstMethodHashTable->Seal();
    }
}
//
// We call these methods to unseal the
// lists: m_pAvailableClasses and m_pAvailableParamTypes
//
void Module::UnsealGenericTypesAndMethods()
{
    LIMITED_METHOD_CONTRACT;
    // Allow us to create generic types and methods again
    //
    // We only decrement it after we have completed the ngen image
    //
    if (m_pAvailableParamTypes != NULL)
    {
        m_pAvailableParamTypes->Unseal();
    }
    if (m_pInstMethodHashTable != NULL)
    {
        m_pInstMethodHashTable->Unseal();
    }
}
#endif


void Module::PrepopulateDictionaries(DataImage *image, BOOL nonExpansive)
{
    STANDARD_VM_CONTRACT;

    // Prepopulating the dictionaries for instantiated types
    // is in theory an iteraive process, i.e. filling in
    // a dictionary slot may result in a class load of a new type whose
    // dictionary may itself need to be prepopulated.  The type expressions
    // involved can get larger, so there's no a-priori reason to expect this
    // process to terminate.
    //
    // Given a starting set of instantiated types, several strategies are
    // thus possible - no prepopulation (call this PP0), or
    // prepopulate only the dictionaries of the types that are in the initial
    // set (call this PP1), or do two iterations (call this PP2) etc. etc.
    // Whichever strategy we choose we can always afford to do
    // one round of prepopulation where we populate slots
    // whose corresponding resulting method/types are already loaded.
    // Call this PPn+PP-FINAL.
    //
    // Below we implement PP1+PP-FINAL for instantiated types and PP0+PP-FINAL
    // for instantiations of generic methods.  We use PP1 because most collection
    // classes (List, Dictionary etc.) only require one pass of prepopulation in order
    // to fully prepopulate the dictionary.

    // Do PP1 for instantiated types... Do one iteration where we force type loading...
    // Because this phase may cause new entries to appear in the hash table we
    // copy the array of types to the stack before we do anything else.
    if (!nonExpansive && CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_Prepopulate1))
    {
        if (m_pAvailableParamTypes != NULL)
        {
            // Create a local copy in case the new elements are added to the hashtable during population
            InlineSArray<TypeHandle, 20> pTypes;

            EETypeHashTable::Iterator it(m_pAvailableParamTypes);
            EETypeHashEntry *pEntry;
            while (m_pAvailableParamTypes->FindNext(&it, &pEntry))
            {
                TypeHandle th = pEntry->GetTypeHandle();
                if (th.IsTypeDesc())
                    continue;

                // Don't do prepopulation for open types - they shouldn't really have dictionaries anyway.
                MethodTable * pMT = th.AsMethodTable();
                if (pMT->ContainsGenericVariables())
                    continue;

                // Only do PP1 on things that land in their preferred Zap module.
                // Forcing the load of dictionary entries in the case where we are
                // speculatively saving a copy of an instantiation outside its preferred
                // zap module is too expensive for the common collection class cases.
                ///
                // Invalid generic instantiations will not be fully loaded.
                // We want to ignore them as touching them will re-raise the TypeLoadException
                if (pMT->IsFullyLoaded() && image->GetModule() == GetPreferredZapModuleForMethodTable(pMT))
                {
                    pTypes.Append(th);
                }
            }
            it.Reset();

            for(COUNT_T i = 0; i < pTypes.GetCount(); i ++)
            {
                TypeHandle th = pTypes[i];
                _ASSERTE(image->GetModule() == GetPreferredZapModuleForTypeHandle(th) );
                _ASSERTE(!th.IsTypeDesc() && !th.ContainsGenericVariables());
                th.AsMethodTable()->PrepopulateDictionary(image, FALSE /* not nonExpansive, i.e. can load types */);
            }
        }
    }

    // PP-FINAL for instantiated types.
    // This is the final stage where we hardbind any remaining entries that map
    // to results that have already been loaded...
    // Thus we set the "nonExpansive" flag on PrepopulateDictionary
    // below, which may in turn greatly limit the amount of prepopulating we do
    // (partly because it's quite difficult to determine if some potential entries
    // in the dictionary are already loaded)

    if (m_pAvailableParamTypes != NULL)
    {
        INDEBUG(DWORD nTypes = m_pAvailableParamTypes->GetCount());

        EETypeHashTable::Iterator it(m_pAvailableParamTypes);
        EETypeHashEntry *pEntry;
        while (m_pAvailableParamTypes->FindNext(&it, &pEntry))
        {
            TypeHandle th = pEntry->GetTypeHandle();
            if (th.IsTypeDesc())
                continue;

            MethodTable * pMT = th.AsMethodTable();
            if (pMT->ContainsGenericVariables())
                continue;

            pMT->PrepopulateDictionary(image, TRUE /* nonExpansive */);
        }

        // No new instantiations should be added by nonExpansive prepopulation
        _ASSERTE(nTypes == m_pAvailableParamTypes->GetCount());
    }

    // PP-FINAL for instantiations of generic methods.
    if (m_pInstMethodHashTable != NULL)
    {
        INDEBUG(DWORD nMethods = m_pInstMethodHashTable->GetCount());

        InstMethodHashTable::Iterator it(m_pInstMethodHashTable);
        InstMethodHashEntry *pEntry;
        while (m_pInstMethodHashTable->FindNext(&it, &pEntry))
        {
            MethodDesc *pMD = pEntry->GetMethod();
            if (!pMD->ContainsGenericVariables())
            {
                pMD->PrepopulateDictionary(image, TRUE /* nonExpansive */);
            }
        }

        // No new instantiations should be added by nonExpansive prepopulation
        _ASSERTE(nMethods == m_pInstMethodHashTable->GetCount());
    }
}

void Module::PlaceType(DataImage *image, TypeHandle th, DWORD profilingFlags)
{
    STANDARD_VM_CONTRACT;

    if (th.IsNull())
        return;

    MethodTable *pMT = th.GetMethodTable();

    if (pMT && pMT->GetLoaderModule() == this)
    {
        EEClass *pClass = pMT->GetClass();

        if (profilingFlags & (1 << WriteMethodTableWriteableData))
        {
            image->PlaceStructureForAddress(pMT->GetWriteableData(),CORCOMPILE_SECTION_WRITE);
        }

        if (profilingFlags & (1 << ReadMethodTable))
        {
            CorCompileSection section = CORCOMPILE_SECTION_READONLY_HOT;
            if (pMT->IsWriteable())
                section = CORCOMPILE_SECTION_HOT_WRITEABLE;
            image->PlaceStructureForAddress(pMT, section);

            if (pMT->HasInterfaceMap())
                image->PlaceInternedStructureForAddress(pMT->GetInterfaceMap(), CORCOMPILE_SECTION_READONLY_SHARED_HOT, CORCOMPILE_SECTION_READONLY_HOT);

            MethodTable::VtableIndirectionSlotIterator it = pMT->IterateVtableIndirectionSlots();
            while (it.Next())
            {
                image->PlaceInternedStructureForAddress(it.GetIndirectionSlot(), CORCOMPILE_SECTION_READONLY_SHARED_HOT, CORCOMPILE_SECTION_READONLY_HOT);
            }

            image->PlaceStructureForAddress(pMT->GetWriteableData(), CORCOMPILE_SECTION_HOT);
        }

        if (profilingFlags & (1 << ReadNonVirtualSlots))
        {
            if (pMT->HasNonVirtualSlotsArray())
                image->PlaceStructureForAddress(pMT->GetNonVirtualSlotsArray(), CORCOMPILE_SECTION_READONLY_HOT);
        }

        if (profilingFlags & (1 << ReadDispatchMap) && pMT->HasDispatchMapSlot())
        {
            image->PlaceInternedStructureForAddress(pMT->GetDispatchMap(), CORCOMPILE_SECTION_READONLY_SHARED_HOT, CORCOMPILE_SECTION_READONLY_HOT);
        }

        if (profilingFlags & (1 << WriteEEClass))
        {
            image->PlaceStructureForAddress(pClass, CORCOMPILE_SECTION_WRITE);

            if (pClass->HasOptionalFields())
                image->PlaceStructureForAddress(pClass->GetOptionalFields(), CORCOMPILE_SECTION_WRITE);
        }

        else if (profilingFlags & (1 << ReadEEClass))
        {
            image->PlaceStructureForAddress(pClass, CORCOMPILE_SECTION_HOT);

            if (pClass->HasOptionalFields())
                image->PlaceStructureForAddress(pClass->GetOptionalFields(), CORCOMPILE_SECTION_HOT);

            if (pClass->GetVarianceInfo() != NULL)
                image->PlaceInternedStructureForAddress(pClass->GetVarianceInfo(), CORCOMPILE_SECTION_READONLY_WARM, CORCOMPILE_SECTION_READONLY_WARM);

#ifdef FEATURE_COMINTEROP
            if (pClass->GetSparseCOMInteropVTableMap() != NULL)
            {
                image->PlaceStructureForAddress(pClass->GetSparseCOMInteropVTableMap(), CORCOMPILE_SECTION_WARM);
                image->PlaceInternedStructureForAddress(pClass->GetSparseCOMInteropVTableMap()->GetMapList(), CORCOMPILE_SECTION_READONLY_WARM, CORCOMPILE_SECTION_READONLY_WARM);
            }
#endif
        }

        if (profilingFlags & (1 << ReadFieldDescs))
        {
            image->PlaceStructureForAddress(pMT->GetApproxFieldDescListRaw(), CORCOMPILE_SECTION_READONLY_HOT);
        }

        if (profilingFlags != 0)
        {
            if (pMT->HasPerInstInfo())
            {
                DPTR(MethodTable::PerInstInfoElem_t) pPerInstInfo = pMT->GetPerInstInfo();

                BOOL fIsEagerBound = pMT->CanEagerBindToParentDictionaries(image, NULL);

                if (fIsEagerBound)
                {
                    if (MethodTable::PerInstInfoElem_t::isRelative)
                    {
                        image->PlaceStructureForAddress(pPerInstInfo, CORCOMPILE_SECTION_READONLY_HOT);
                    }
                    else
                    {
                        image->PlaceInternedStructureForAddress(pPerInstInfo, CORCOMPILE_SECTION_READONLY_SHARED_HOT, CORCOMPILE_SECTION_READONLY_HOT);
                    }
                }
                else
                {
                    image->PlaceStructureForAddress(pPerInstInfo, CORCOMPILE_SECTION_WRITE);
                }
            }

            Dictionary * pDictionary = pMT->GetDictionary();
            if (pDictionary != NULL)
            {
                BOOL fIsWriteable;

                if (!pMT->IsCanonicalMethodTable())
                {
                    // CanEagerBindToMethodTable would not work for targeted patching here. The dictionary
                    // layout is sensitive to compilation order that can be changed by TP compatible changes.
                    BOOL canSaveSlots = (image->GetModule() == pMT->GetCanonicalMethodTable()->GetLoaderModule());

                    fIsWriteable = pDictionary->IsWriteable(image, canSaveSlots,
                                           pMT->GetNumGenericArgs(),
                                           pMT->GetModule(),
                                           pClass->GetDictionaryLayout());
                }
                else
                {
                    fIsWriteable = FALSE;
                }

                if (fIsWriteable)
                {
                    image->PlaceStructureForAddress(pDictionary, CORCOMPILE_SECTION_HOT_WRITEABLE);
                    image->PlaceStructureForAddress(pClass->GetDictionaryLayout(), CORCOMPILE_SECTION_WARM);
                }
                else
                {
                    image->PlaceInternedStructureForAddress(pDictionary, CORCOMPILE_SECTION_READONLY_SHARED_HOT, CORCOMPILE_SECTION_READONLY_HOT);
                }
            }
        }
    }
    if (th.IsTypeDesc())
    {
        if (profilingFlags & (1 << WriteTypeDesc))
            image->PlaceStructureForAddress(th.AsTypeDesc(), CORCOMPILE_SECTION_WRITE);
        else if  (profilingFlags & (1 << ReadTypeDesc))
            image->PlaceStructureForAddress(th.AsTypeDesc(), CORCOMPILE_SECTION_HOT);
        else
            image->PlaceStructureForAddress(th.AsTypeDesc(), CORCOMPILE_SECTION_WARM);
    }
}

void Module::PlaceMethod(DataImage *image, MethodDesc *pMD, DWORD profilingFlags)
{
    STANDARD_VM_CONTRACT;

    if (pMD == NULL)
        return;

    if (pMD->GetLoaderModule() != this)
        return;

    if (profilingFlags & (1 << ReadMethodCode))
    {
        if (pMD->IsNDirect())
        {
            NDirectMethodDesc *pNMD = (NDirectMethodDesc *)pMD;
            image->PlaceStructureForAddress((void*) pNMD->GetWriteableData(), CORCOMPILE_SECTION_WRITE);

#ifdef HAS_NDIRECT_IMPORT_PRECODE
            // The NDirect import thunk glue is used only if no marshaling is required
            if (!pNMD->MarshalingRequired())
            {
                image->PlaceStructureForAddress((void*) pNMD->GetNDirectImportThunkGlue(), CORCOMPILE_SECTION_METHOD_PRECODE_HOT);
            }
#endif // HAS_NDIRECT_IMPORT_PRECODE

            // Late bound NDirect methods require their LibName at startup.
            if (!pNMD->IsQCall())
            {
                image->PlaceStructureForAddress((void*) pNMD->GetLibName(), CORCOMPILE_SECTION_READONLY_HOT);
                image->PlaceStructureForAddress((void*) pNMD->GetEntrypointName(), CORCOMPILE_SECTION_READONLY_HOT);
            }
        }

#ifdef FEATURE_COMINTEROP
        if (pMD->IsComPlusCall())
        {
            ComPlusCallMethodDesc *pCMD = (ComPlusCallMethodDesc *)pMD;

            // If the ComPlusCallMethodDesc was actually used for interop, its ComPlusCallInfo should be hot.
            image->PlaceStructureForAddress((void*) pCMD->m_pComPlusCallInfo, CORCOMPILE_SECTION_HOT);
       }
#endif // FEATURE_COMINTEROP

        // Stubs-as-IL have writeable signatures sometimes, so can't place them
        // into read-only section. We should not get here for stubs-as-il anyway,
        // but we will filter them out just to be sure.
        if (pMD->HasStoredSig() && !pMD->IsILStub())
        {
            StoredSigMethodDesc *pSMD = (StoredSigMethodDesc*) pMD;

            if (pSMD->HasStoredMethodSig())
            {
                image->PlaceInternedStructureForAddress((void*) pSMD->GetStoredMethodSig(), CORCOMPILE_SECTION_READONLY_SHARED_HOT, CORCOMPILE_SECTION_READONLY_HOT);
            }
        }
    }

    // We store the entire hot chunk in the SECTION_WRITE section
    if (profilingFlags & (1 << WriteMethodDesc))
    {
        image->PlaceStructureForAddress(pMD, CORCOMPILE_SECTION_WRITE);
    }


    if (profilingFlags & (1 << WriteMethodPrecode))
    {
        Precode* pPrecode = pMD->GetSavedPrecodeOrNull(image);
        // protect against stale IBC data
        if (pPrecode != NULL)
        {
            CorCompileSection section = CORCOMPILE_SECTION_METHOD_PRECODE_WRITE;
            if (pPrecode->IsPrebound(image))
                section = CORCOMPILE_SECTION_METHOD_PRECODE_HOT;
            // Note: This is going to place the entire PRECODE_FIXUP chunk if we have one
            image->PlaceStructureForAddress(pPrecode, section);
        }
    }
    else if (profilingFlags & (1 << ReadMethodPrecode))
    {
        Precode* pPrecode = pMD->GetSavedPrecodeOrNull(image);
        // protect against stale IBC data
        if (pPrecode != NULL)
        {
            // Note: This is going to place the entire PRECODE_FIXUP chunk if we have one
            image->PlaceStructureForAddress(pPrecode, CORCOMPILE_SECTION_METHOD_PRECODE_HOT);
        }
    }
}

void Module::Arrange(DataImage *image)
{
    STANDARD_VM_CONTRACT;

    // We collect IBC logging profiling data and use that to guide the layout of the image.
    image->PlaceStructureForAddress(this, CORCOMPILE_SECTION_MODULE);

    // The stub method table is shared by all IL stubs in the module, so place it into the hot section
    MethodTable * pStubMT = GetILStubCache()->GetStubMethodTable();
    if (pStubMT != NULL)
        PlaceType(image, pStubMT, ReadMethodTable);

    CorProfileData * profileData = GetProfileData();
    if (profileData)
    {
        //
        // Place hot type structues in the order specifiled by TypeProfilingData array
        //
        CORBBTPROF_TOKEN_INFO * pTypeProfilingData = profileData->GetTokenFlagsData(TypeProfilingData);
        DWORD                   cTypeProfilingData = profileData->GetTokenFlagsCount(TypeProfilingData);
        for (unsigned int i = 0; (i < cTypeProfilingData); i++)
        {
            CORBBTPROF_TOKEN_INFO * entry = &pTypeProfilingData[i];
            mdToken                 token = entry->token;
            DWORD                   flags = entry->flags;
#if defined(_DEBUG) && !defined(DACCESS_COMPILE)
            g_pConfig->DebugCheckAndForceIBCFailure(EEConfig::CallSite_6);
#endif

            if (TypeFromToken(token) == mdtTypeDef)
            {
                TypeHandle th = LookupTypeDef(token);
                //
                // Place a hot normal type and it's data
                //
                PlaceType(image, th, flags);
            }
            else if (TypeFromToken(token) == ibcTypeSpec)
            {
                CORBBTPROF_BLOB_PARAM_SIG_ENTRY *pBlobSigEntry = profileData->GetBlobSigEntry(token);

                if (pBlobSigEntry == NULL)
                {
                    //
                    // Print an error message for the type load failure
                    //
                    StackSString msg(W("Did not find definition for type token "));

                    char buff[16];
                    sprintf_s(buff, COUNTOF(buff), "%08x", token);
                    StackSString szToken(SString::Ascii, &buff[0]);
                    msg += szToken;
                    msg += W(" in profile data.\n");

                    GetSvcLogger()->Log(msg, LogLevel_Info);
                }
                else // (pBlobSigEntry  != NULL)
                {
                    _ASSERTE(pBlobSigEntry->blob.token == token);
                    //
                    // decode generic type signature
                    //
                    TypeHandle th = LoadIBCTypeHelper(image, pBlobSigEntry);

                    //
                    // Place a hot instantiated type and it's data
                    //
                    PlaceType(image, th, flags);
                }
            }
            else if (TypeFromToken(token) == mdtFieldDef)
            {
                FieldDesc *pFD = LookupFieldDef(token);
                if (pFD && pFD->IsRVA())
                {
                    if (entry->flags & (1 << RVAFieldData))
                    {
                        BYTE *pRVAData = (BYTE*) pFD->GetStaticAddressHandle(NULL);
                        //
                        // Place a hot RVA static field
                        //
                        image->PlaceStructureForAddress(pRVAData, CORCOMPILE_SECTION_RVA_STATICS_HOT);
                    }
                }
            }
        }

        //
        // Place hot methods and method data in the order specifiled by MethodProfilingData array
        //
        CORBBTPROF_TOKEN_INFO * pMethodProfilingData = profileData->GetTokenFlagsData(MethodProfilingData);
        DWORD                   cMethodProfilingData = profileData->GetTokenFlagsCount(MethodProfilingData);
        for (unsigned int i = 0; (i < cMethodProfilingData); i++)
        {
            mdToken token          = pMethodProfilingData[i].token;
            DWORD   profilingFlags = pMethodProfilingData[i].flags;
#if defined(_DEBUG) && !defined(DACCESS_COMPILE)
            g_pConfig->DebugCheckAndForceIBCFailure(EEConfig::CallSite_7);
#endif

            if (TypeFromToken(token) == mdtMethodDef)
            {
                MethodDesc *  pMD = LookupMethodDef(token);
                //
                // Place a hot normal method and it's data
                //
                PlaceMethod(image, pMD, profilingFlags);
            }
            else if (TypeFromToken(token) == ibcMethodSpec)
            {
                CORBBTPROF_BLOB_PARAM_SIG_ENTRY *pBlobSigEntry = profileData->GetBlobSigEntry(token);

                if (pBlobSigEntry == NULL)
                {
                    //
                    // Print an error message for the type load failure
                    //
                    StackSString msg(W("Did not find definition for method token "));

                    char buff[16];
                    sprintf_s(buff, COUNTOF(buff), "%08x", token);
                    StackSString szToken(SString::Ascii, &buff[0]);
                    msg += szToken;
                    msg += W(" in profile data.\n");

                    GetSvcLogger()->Log(msg, LogLevel_Info);
                }
                else // (pBlobSigEntry  != NULL)
                {
                    _ASSERTE(pBlobSigEntry->blob.token == token);
                    MethodDesc * pMD = LoadIBCMethodHelper(image, pBlobSigEntry);

                    if (pMD != NULL)
                    {
                        //
                        // Place a hot instantiated method and it's data
                        //
                        PlaceMethod(image, pMD, profilingFlags);
                    }
                }
            }
        }
    }

    // Now place all remaining items
    image->PlaceRemainingStructures();
}

void ModuleCtorInfo::Fixup(DataImage *image)
{
    STANDARD_VM_CONTRACT;

    if (numElementsHot > 0)
    {
        image->FixupPointerField(this, offsetof(ModuleCtorInfo, cctorInfoHot));
        image->FixupPointerField(this, offsetof(ModuleCtorInfo, hotHashOffsets));
    }
    else
    {
        image->ZeroPointerField(this, offsetof(ModuleCtorInfo, cctorInfoHot));
        image->ZeroPointerField(this, offsetof(ModuleCtorInfo, hotHashOffsets));
    }

    _ASSERTE(numElements > numElementsHot || numElements == numElementsHot);
    if (numElements > numElementsHot)
    {
        image->FixupPointerField(this, offsetof(ModuleCtorInfo, cctorInfoCold));
        image->FixupPointerField(this, offsetof(ModuleCtorInfo, coldHashOffsets));
    }
    else
    {
        image->ZeroPointerField(this, offsetof(ModuleCtorInfo, cctorInfoCold));
        image->ZeroPointerField(this, offsetof(ModuleCtorInfo, coldHashOffsets));
    }

    if (numElements > 0)
    {
        image->FixupPointerField(this, offsetof(ModuleCtorInfo, ppMT));

        for (DWORD i=0; i<numElements; i++)
        {
            image->FixupRelativePointerField(ppMT, i * sizeof(ppMT[0]));
        }
    }
    else
    {
        image->ZeroPointerField(this, offsetof(ModuleCtorInfo, ppMT));
    }

    if (numHotGCStaticsMTs > 0)
    {
        image->FixupPointerField(this, offsetof(ModuleCtorInfo, ppHotGCStaticsMTs));

        image->BeginRegion(CORINFO_REGION_HOT);
        for (DWORD i=0; i < numHotGCStaticsMTs; i++)
        {
            image->FixupMethodTablePointer(ppHotGCStaticsMTs, &ppHotGCStaticsMTs[i]);
        }
        image->EndRegion(CORINFO_REGION_HOT);
    }
    else
    {
        image->ZeroPointerField(this, offsetof(ModuleCtorInfo, ppHotGCStaticsMTs));
    }

    if (numColdGCStaticsMTs > 0)
    {
        image->FixupPointerField(this, offsetof(ModuleCtorInfo, ppColdGCStaticsMTs));

        image->BeginRegion(CORINFO_REGION_COLD);
        for (DWORD i=0; i < numColdGCStaticsMTs; i++)
        {
            image->FixupMethodTablePointer(ppColdGCStaticsMTs, &ppColdGCStaticsMTs[i]);
        }
        image->EndRegion(CORINFO_REGION_COLD);
    }
    else
    {
        image->ZeroPointerField(this, offsetof(ModuleCtorInfo, ppColdGCStaticsMTs));
    }
}

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
void Module::Fixup(DataImage *image)
{
    STANDARD_VM_CONTRACT;

    // Propagate all changes to the image copy
    memcpy(image->GetImagePointer(this), (void*)this, sizeof(Module));

    //
    // Zero out VTable
    //

    image->ZeroPointerField(this, 0);

    image->FixupPointerField(this, offsetof(Module, m_pNGenLayoutInfo));

    image->ZeroField(this, offsetof(Module, m_pSimpleName), sizeof(m_pSimpleName));

    image->ZeroField(this, offsetof(Module, m_file), sizeof(m_file));

    image->ZeroField(this, offsetof(Module, m_dwTransientFlags), sizeof(m_dwTransientFlags));

    image->ZeroField(this, offsetof(Module, m_pVASigCookieBlock), sizeof(m_pVASigCookieBlock));
    image->ZeroField(this, offsetof(Module, m_pAssembly), sizeof(m_pAssembly));
    image->ZeroField(this, offsetof(Module, m_moduleRef), sizeof(m_moduleRef));

    image->ZeroField(this, offsetof(Module, m_Crst), sizeof(m_Crst));
    image->ZeroField(this, offsetof(Module, m_FixupCrst), sizeof(m_FixupCrst));

    image->ZeroField(this, offsetof(Module, m_pProfilingBlobTable), sizeof(m_pProfilingBlobTable));
    image->ZeroField(this, offsetof(Module, m_pProfileData), sizeof(m_pProfileData));

    image->ZeroPointerField(this, offsetof(Module, m_pNgenStats));

    // Fixup the property name set
    image->FixupPointerField(this, offsetof(Module, m_propertyNameSet));

    //
    // Fixup the method table
    //

    image->ZeroField(this, offsetof(Module, m_pISymUnmanagedReader), sizeof(m_pISymUnmanagedReader));
    image->ZeroField(this, offsetof(Module, m_ISymUnmanagedReaderCrst), sizeof(m_ISymUnmanagedReaderCrst));

    image->ZeroField(this, offsetof(Module, m_LookupTableCrst), sizeof(m_LookupTableCrst));

    m_TypeDefToMethodTableMap.Fixup(image);
    m_TypeRefToMethodTableMap.Fixup(image, FALSE);
    m_MethodDefToDescMap.Fixup(image);
    m_FieldDefToDescMap.Fixup(image);
    if(m_pMemberRefToDescHashTable != NULL)
    {
        image->FixupPointerField(this, offsetof(Module, m_pMemberRefToDescHashTable));
        m_pMemberRefToDescHashTable->Fixup(image);
    }
    m_GenericParamToDescMap.Fixup(image);
    m_GenericTypeDefToCanonMethodTableMap.Fixup(image);
    m_FileReferencesMap.Fixup(image, FALSE);
    m_ManifestModuleReferencesMap.Fixup(image, FALSE);
    m_MethodDefToPropertyInfoMap.Fixup(image, FALSE);

    image->ZeroPointerField(this, offsetof(Module, m_pILStubCache));

    if (m_pAvailableClasses != NULL) {
        image->FixupPointerField(this, offsetof(Module, m_pAvailableClasses));
        m_pAvailableClasses->Fixup(image);
    }

    image->ZeroField(this, offsetof(Module, m_pAvailableClassesCaseIns), sizeof(m_pAvailableClassesCaseIns));
    image->ZeroField(this, offsetof(Module, m_InstMethodHashTableCrst), sizeof(m_InstMethodHashTableCrst));

    image->BeginRegion(CORINFO_REGION_COLD);

    if (m_pAvailableParamTypes) {
        image->FixupPointerField(this, offsetof(Module, m_pAvailableParamTypes));
        m_pAvailableParamTypes->Fixup(image);
    }

    if (m_pInstMethodHashTable) {
        image->FixupPointerField(this, offsetof(Module, m_pInstMethodHashTable));
        m_pInstMethodHashTable->Fixup(image);
    }

    {
        MethodTable * pStubMT = GetILStubCache()->GetStubMethodTable();
        if (pStubMT != NULL)
            pStubMT->Fixup(image);
    }

    if (m_pStubMethodHashTable) {
        image->FixupPointerField(this, offsetof(Module, m_pStubMethodHashTable));
        m_pStubMethodHashTable->Fixup(image);
    }

    image->EndRegion(CORINFO_REGION_COLD);

#ifdef _DEBUG
    //
    // Unseal the generic tables:
    //
    // - We need to run managed code to serialize the Security attributes of the ngen image
    //   and we are now using generic types in the Security/Reflection code.
    // - Compilation of other modules of multimodule assemblies may add more types
    //   to the generic tables.
    //
    UnsealGenericTypesAndMethods();
#endif

    m_ModuleCtorInfo.Fixup(image);

    //
    // Fixup binder
    //

    if (m_pBinder != NULL)
    {
        image->FixupPointerField(this, offsetof(Module, m_pBinder));
        m_pBinder->Fixup(image);
    }


    //
    // Fixup classes
    //

    {
        LookupMap<PTR_MethodTable>::Iterator typeDefIter(&m_TypeDefToMethodTableMap);

        image->BeginRegion(CORINFO_REGION_COLD);
        while (typeDefIter.Next())
        {
            MethodTable * t = typeDefIter.GetElement();
            if (image->IsStored(t))
                t->Fixup(image);
        }
        image->EndRegion(CORINFO_REGION_COLD);
    }

    {
        LookupMap<PTR_TypeRef>::Iterator typeRefIter(&m_TypeRefToMethodTableMap);
        DWORD rid = 0;

        image->BeginRegion(CORINFO_REGION_HOT);
        while (typeRefIter.Next())
        {
            TADDR flags;
            TypeHandle th = TypeHandle::FromTAddr(dac_cast<TADDR>(typeRefIter.GetElementAndFlags(&flags)));

            if (!th.IsNull())
            {
                if (th.GetLoaderModule() != this || image->IsStored(th.AsPtr()))
                {
                    PTR_TADDR hotItemValuePtr = m_TypeRefToMethodTableMap.FindHotItemValuePtr(rid);
                    BOOL fSet = FALSE;

                    if (image->CanEagerBindToTypeHandle(th))
                    {
                        if (image->CanHardBindToZapModule(th.GetLoaderModule()))
                        {
                            PVOID pTarget = th.IsTypeDesc() ? th.AsTypeDesc() : th.AsPtr();
                            SSIZE_T offset = th.IsTypeDesc() ? 2 : 0;

                            _ASSERTE((flags & offset) == 0);

                            image->FixupField(m_TypeRefToMethodTableMap.pTable, rid * sizeof(TADDR),
                                pTarget, flags | offset, IMAGE_REL_BASED_RelativePointer);

                            // In case this item is also in the hot item subtable, fix it up there as well
                            if (hotItemValuePtr != NULL)
                            {
                                image->FixupField(m_TypeRefToMethodTableMap.hotItemList,
                                    (BYTE *)hotItemValuePtr - (BYTE *)m_TypeRefToMethodTableMap.hotItemList,
                                    pTarget, flags | offset, IMAGE_REL_BASED_RelativePointer);
                            }
                            fSet = TRUE;
                        }
                        else
                        // Create the indirection only if the entry is hot or we do have indirection cell already
                        if (hotItemValuePtr != NULL || image->GetExistingTypeHandleImport(th) != NULL)
                        {
                            _ASSERTE((flags & FIXUP_POINTER_INDIRECTION) == 0);

                            ZapNode * pImport = image->GetTypeHandleImport(th);
                            image->FixupFieldToNode(m_TypeRefToMethodTableMap.pTable, rid * sizeof(TADDR),
                                pImport, flags | FIXUP_POINTER_INDIRECTION, IMAGE_REL_BASED_RelativePointer);
                            if (hotItemValuePtr != NULL)
                            {
                                image->FixupFieldToNode(m_TypeRefToMethodTableMap.hotItemList,
                                    (BYTE *)hotItemValuePtr - (BYTE *)m_TypeRefToMethodTableMap.hotItemList,
                                    pImport, flags | FIXUP_POINTER_INDIRECTION, IMAGE_REL_BASED_RelativePointer);
                            }
                            fSet = TRUE;
                        }
                    }

                    if (!fSet)
                    {
                        image->ZeroPointerField(m_TypeRefToMethodTableMap.pTable, rid * sizeof(TADDR));
                        // In case this item is also in the hot item subtable, fix it up there as well
                        if (hotItemValuePtr != NULL)
                        {
                            image->ZeroPointerField(m_TypeRefToMethodTableMap.hotItemList,
                                (BYTE *)hotItemValuePtr - (BYTE *)m_TypeRefToMethodTableMap.hotItemList);
                        }
                    }
                }
            }

            rid++;
        }
        image->EndRegion(CORINFO_REGION_HOT);
    }

    {
        LookupMap<PTR_TypeVarTypeDesc>::Iterator genericParamIter(&m_GenericParamToDescMap);

        while (genericParamIter.Next())
        {
            TypeVarTypeDesc * pTypeDesc = genericParamIter.GetElement();

            if (pTypeDesc != NULL)
            {
                _ASSERTE(image->IsStored(pTypeDesc));
                pTypeDesc->Fixup(image);
            }
        }
    }

    //
    // Fixup the assembly reference map table
    //

    {
        LookupMap<PTR_Module>::Iterator manifestModuleIter(&m_ManifestModuleReferencesMap);
        DWORD rid = 0;

        while (manifestModuleIter.Next())
        {
            TADDR flags;
            Module * pModule = manifestModuleIter.GetElementAndFlags(&flags);

            if (pModule != NULL)
            {
                if (image->CanEagerBindToModule(pModule))
                {
                    if (image->CanHardBindToZapModule(pModule))
                    {
                        image->FixupField(m_ManifestModuleReferencesMap.pTable, rid * sizeof(TADDR),
                            pModule, flags, IMAGE_REL_BASED_RelativePointer);
                    }
                    else
                    {
                        image->ZeroPointerField(m_ManifestModuleReferencesMap.pTable, rid * sizeof(TADDR));
                    }
                }
                else
                {
                    image->ZeroPointerField(m_ManifestModuleReferencesMap.pTable, rid * sizeof(TADDR));
                }
            }

            rid++;
        }
    }

    //
    // Zero out file references table.
    //
    image->ZeroField(m_FileReferencesMap.pTable, 0,
                     m_FileReferencesMap.GetSize() * sizeof(void*));


    image->ZeroField(this, offsetof(Module, m_debuggerSpecificData), sizeof(m_debuggerSpecificData));

    image->ZeroField(this, offsetof(Module, m_AssemblyRefByNameCount), sizeof(m_AssemblyRefByNameCount));
    image->ZeroPointerField(this, offsetof(Module, m_AssemblyRefByNameTable));

#ifdef FEATURE_READYTORUN
    // For composite ready-to-run images, the manifest assembly ref map is stored in the native image
    // and shared by all its component images.
    if (m_pNativeImage == NULL)
#endif
    {
        image->ZeroPointerField(this,offsetof(Module, m_NativeMetadataAssemblyRefMap));
    }

    //
    // Fixup statics
    //
    LOG((LF_CLASSLOADER, LL_INFO10000, "STATICS: fixing up module static data\n"));

    image->ZeroPointerField(this, offsetof(Module, m_ModuleID));
    image->ZeroField(this, offsetof(Module, m_ModuleIndex), sizeof(m_ModuleIndex));

    image->FixupPointerField(this, offsetof(Module, m_pDynamicStaticsInfo));

    DynamicStaticsInfo* pDSI = m_pDynamicStaticsInfo;
    for (DWORD i = 0; i < m_cDynamicEntries; i++, pDSI++)
    {
        if (pDSI->pEnclosingMT->GetLoaderModule() == this &&
            // CEEPreloader::TriageTypeForZap() could have rejected this type
            image->IsStored(pDSI->pEnclosingMT))
        {
            image->FixupPointerField(m_pDynamicStaticsInfo, (BYTE *)&pDSI->pEnclosingMT - (BYTE *)m_pDynamicStaticsInfo);
        }
        else
        {
            // Some other (mutually-recursive) dependency must have loaded
            // a generic instantiation whose static were pumped into the
            // assembly being ngenned.
            image->ZeroPointerField(m_pDynamicStaticsInfo, (BYTE *)&pDSI->pEnclosingMT - (BYTE *)m_pDynamicStaticsInfo);
        }
    }

    // If we failed to load some types we need to reset the pointers to the static offset tables so they'll be
    // rebuilt at runtime.
    if (m_pRegularStaticOffsets != (PTR_DWORD)NGEN_STATICS_ALLCLASSES_WERE_LOADED)
    {
        _ASSERTE(m_pThreadStaticOffsets != (PTR_DWORD)NGEN_STATICS_ALLCLASSES_WERE_LOADED);
        image->ZeroPointerField(this, offsetof(Module, m_pRegularStaticOffsets));
        image->ZeroPointerField(this, offsetof(Module, m_pThreadStaticOffsets));
    }

    // Fix up inlining data
    if(m_pPersistentInlineTrackingMapNGen)
    {
        image->FixupPointerField(this, offsetof(Module, m_pPersistentInlineTrackingMapNGen));
        m_pPersistentInlineTrackingMapNGen->Fixup(image);
    }
    else
    {
        image->ZeroPointerField(this, offsetof(Module, m_pPersistentInlineTrackingMapNGen));
    }

    SetIsModuleSaved();
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

#endif // FEATURE_NATIVE_IMAGE_GENERATION

#ifdef FEATURE_PREJIT
//
// Is "address" a data-structure in the native image?
//

BOOL Module::IsPersistedObject(void *address)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    if (!HasNativeImage())
        return FALSE;

    PEImageLayout *pLayout = GetNativeImage();
    _ASSERTE(pLayout->IsMapped());

    return (address >= pLayout->GetBase()
            && address < (BYTE*)pLayout->GetBase() + pLayout->GetVirtualSize());
}
#endif // FEATURE_PREJIT

Module *Module::GetModuleFromIndex(DWORD ix)
{
    CONTRACT(Module*)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    if (HasNativeOrReadyToRunImage())
    {
        RETURN ZapSig::DecodeModuleFromIndex(this, ix);
    }
    else
    {
        mdAssemblyRef mdAssemblyRefToken = TokenFromRid(ix, mdtAssemblyRef);
        Assembly *pAssembly = this->LookupAssemblyRef(mdAssemblyRefToken);
        if (pAssembly)
        {
            RETURN pAssembly->GetManifestModule();
        }
        else
        {
            // GetModuleFromIndex failed
            RETURN NULL;
        }
    }
}

#endif // !DACCESS_COMPILE

Module *Module::GetModuleFromIndexIfLoaded(DWORD ix)
{
    CONTRACT(Module*)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(HasNativeOrReadyToRunImage());
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

#ifndef DACCESS_COMPILE
    RETURN ZapSig::DecodeModuleFromIndexIfLoaded(this, ix);
#else // DACCESS_COMPILE
    DacNotImpl();
    RETURN NULL;
#endif // DACCESS_COMPILE
}

#ifndef DACCESS_COMPILE
IMDInternalImport* Module::GetNativeAssemblyImport(BOOL loadAllowed)
{
    CONTRACT(IMDInternalImport*)
    {
        INSTANCE_CHECK;
        if (loadAllowed) GC_TRIGGERS;                    else GC_NOTRIGGER;
        if (loadAllowed) THROWS;                         else NOTHROW;
        if (loadAllowed) INJECT_FAULT(COMPlusThrowOM()); else FORBID_FAULT;
        MODE_ANY;
        PRECONDITION(HasNativeOrReadyToRunImage());
        POSTCONDITION(loadAllowed ?
            CheckPointer(RETVAL) :
            CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

#ifdef FEATURE_PREJIT
    // Check if image is R2R
    if (!GetFile()->IsILImageReadyToRun())
    {
        RETURN GetFile()->GetPersistentNativeImage()->GetNativeMDImport(loadAllowed);
    }
#endif

    RETURN GetFile()->GetOpenedILimage()->GetNativeMDImport(loadAllowed);
}

BYTE* Module::GetNativeFixupBlobData(RVA rva)
{
    CONTRACT(BYTE*)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    RETURN(BYTE*) GetNativeOrReadyToRunImage()->GetRvaData(rva);
}
#endif // DACCESS_COMPILE

#ifdef FEATURE_PREJIT

#ifndef DACCESS_COMPILE

/*static*/
void Module::RestoreMethodTablePointerRaw(MethodTable ** ppMT,
                                          Module *pContainingModule,
                                          ClassLoadLevel level)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Ensure that the compiler won't fetch the value twice
    TADDR fixup = VolatileLoadWithoutBarrier((TADDR *)ppMT);

#ifdef _DEBUG
    if (pContainingModule != NULL)
    {
        Module * dbg_pZapModule = ExecutionManager::FindZapModule(dac_cast<TADDR>(ppMT));
        _ASSERTE((dbg_pZapModule == NULL) || (pContainingModule == dbg_pZapModule));
    }
#endif //_DEBUG

    if (CORCOMPILE_IS_POINTER_TAGGED(fixup))
    {
#ifdef HOST_64BIT
        CONSISTENCY_CHECK((CORCOMPILE_UNTAG_TOKEN(fixup)>>32) == 0);
#endif

        RVA fixupRva = (RVA) CORCOMPILE_UNTAG_TOKEN(fixup);

        if (pContainingModule == NULL)
            pContainingModule = ExecutionManager::FindZapModule(dac_cast<TADDR>(ppMT));
        PREFIX_ASSUME(pContainingModule != NULL);

        _ASSERTE((*pContainingModule->GetNativeFixupBlobData(fixupRva) & ~ENCODE_MODULE_OVERRIDE) == ENCODE_TYPE_HANDLE);

        Module * pInfoModule;
        PCCOR_SIGNATURE pBlobData = pContainingModule->GetEncodedSig(fixupRva, &pInfoModule);

        TypeHandle  th          = ZapSig::DecodeType(pContainingModule,
                                                             pInfoModule,
                                                             pBlobData,
                                                             level);
        *ppMT = th.AsMethodTable();
    }
    else if (*ppMT)
    {
        ClassLoader::EnsureLoaded(*ppMT, level);
    }
}

/*static*/
void Module::RestoreMethodTablePointer(FixupPointer<PTR_MethodTable> * ppMT,
                                       Module *pContainingModule,
                                       ClassLoadLevel level)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (ppMT->IsNull())
        return;

    if (ppMT->IsTagged())
    {
        RestoreMethodTablePointerRaw(ppMT->GetValuePtr(), pContainingModule, level);
    }
    else
    {
        ClassLoader::EnsureLoaded(ppMT->GetValue(), level);
    }
}

/*static*/
void Module::RestoreMethodTablePointer(RelativeFixupPointer<PTR_MethodTable> * ppMT,
                                       Module *pContainingModule,
                                       ClassLoadLevel level)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (ppMT->IsNull())
        return;

    if (ppMT->IsTagged((TADDR)ppMT))
    {
        RestoreMethodTablePointerRaw(ppMT->GetValuePtr(), pContainingModule, level);
    }
    else
    {
        ClassLoader::EnsureLoaded(ppMT->GetValue(), level);
    }
}

#endif // !DACCESS_COMPILE

BOOL Module::IsZappedCode(PCODE code)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    if (!HasNativeImage())
        return FALSE;

    PEImageLayout *pNativeImage = GetNativeImage();

    UINT32 cCode = 0;
    PCODE pCodeSection;

    pCodeSection = pNativeImage->GetNativeHotCode(&cCode);
    if ((pCodeSection <= code) && (code < pCodeSection + cCode))
    {
        return TRUE;
    }

    pCodeSection = pNativeImage->GetNativeCode(&cCode);
    if ((pCodeSection <= code) && (code < pCodeSection + cCode))
    {
        return TRUE;
    }

    return FALSE;
}

BOOL Module::IsZappedPrecode(PCODE code)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    if (m_pNGenLayoutInfo == NULL)
        return FALSE;

    for (SIZE_T i = 0; i < COUNTOF(m_pNGenLayoutInfo->m_Precodes); i++)
    {
        if (m_pNGenLayoutInfo->m_Precodes[i].IsInRange(code))
            return TRUE;
    }

    return FALSE;
}

PCCOR_SIGNATURE Module::GetEncodedSig(RVA fixupRva, Module **ppDefiningModule)
{
    CONTRACT(PCCOR_SIGNATURE)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
        SUPPORTS_DAC;
    }
    CONTRACT_END;

#ifndef DACCESS_COMPILE
    PCCOR_SIGNATURE pBuffer = GetNativeFixupBlobData(fixupRva);

    BYTE kind = *pBuffer++;

    *ppDefiningModule = (kind & ENCODE_MODULE_OVERRIDE) ? GetModuleFromIndex(CorSigUncompressData(pBuffer)) : this;

    RETURN pBuffer;
#else
    RETURN NULL;
#endif // DACCESS_COMPILE
}

PCCOR_SIGNATURE Module::GetEncodedSigIfLoaded(RVA fixupRva, Module **ppDefiningModule)
{
    CONTRACT(PCCOR_SIGNATURE)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
        SUPPORTS_DAC;
    }
    CONTRACT_END;

#ifndef DACCESS_COMPILE
    PCCOR_SIGNATURE pBuffer = GetNativeFixupBlobData(fixupRva);

    BYTE kind = *pBuffer++;

    *ppDefiningModule = (kind & ENCODE_MODULE_OVERRIDE) ? GetModuleFromIndexIfLoaded(CorSigUncompressData(pBuffer)) : this;

    RETURN pBuffer;
#else
    *ppDefiningModule = NULL;
    RETURN NULL;
#endif // DACCESS_COMPILE
}

/*static*/
PTR_Module Module::RestoreModulePointerIfLoaded(DPTR(RelativeFixupPointer<PTR_Module>) ppModule, Module *pContainingModule)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    if (!ppModule->IsTagged(dac_cast<TADDR>(ppModule)))
        return ppModule->GetValue(dac_cast<TADDR>(ppModule));

#ifndef DACCESS_COMPILE
    PTR_Module * ppValue = ppModule->GetValuePtr();

    // Ensure that the compiler won't fetch the value twice
    TADDR fixup = VolatileLoadWithoutBarrier((TADDR *)ppValue);

    if (CORCOMPILE_IS_POINTER_TAGGED(fixup))
    {

#ifdef HOST_64BIT
        CONSISTENCY_CHECK((CORCOMPILE_UNTAG_TOKEN(fixup)>>32) == 0);
#endif

        RVA fixupRva = (RVA) CORCOMPILE_UNTAG_TOKEN(fixup);

        _ASSERTE((*pContainingModule->GetNativeFixupBlobData(fixupRva) & ~ENCODE_MODULE_OVERRIDE) == ENCODE_MODULE_HANDLE);

        Module * pInfoModule;
        PCCOR_SIGNATURE pBlobData = pContainingModule->GetEncodedSigIfLoaded(fixupRva, &pInfoModule);

        if (pInfoModule)
        {
            *ppValue = pInfoModule;
        }
        return pInfoModule;
    }
    else
    {
        return PTR_Module(fixup);
    }
#else
    DacNotImpl();
    return NULL;
#endif
}

#ifndef DACCESS_COMPILE

/*static*/
void Module::RestoreModulePointer(RelativeFixupPointer<PTR_Module> * ppModule, Module *pContainingModule)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    if (!ppModule->IsTagged((TADDR)ppModule))
        return;

    PTR_Module * ppValue = ppModule->GetValuePtr();

    // Ensure that the compiler won't fetch the value twice
    TADDR fixup = VolatileLoadWithoutBarrier((TADDR *)ppValue);

    if (CORCOMPILE_IS_POINTER_TAGGED(fixup))
    {
#ifdef HOST_64BIT
        CONSISTENCY_CHECK((CORCOMPILE_UNTAG_TOKEN(fixup)>>32) == 0);
#endif

        RVA fixupRva = (RVA) CORCOMPILE_UNTAG_TOKEN(fixup);

        _ASSERTE((*pContainingModule->GetNativeFixupBlobData(fixupRva) & ~ENCODE_MODULE_OVERRIDE) == ENCODE_MODULE_HANDLE);

        Module * pInfoModule;
        PCCOR_SIGNATURE pBlobData = pContainingModule->GetEncodedSig(fixupRva, &pInfoModule);

        *ppValue = pInfoModule;
    }
}

/*static*/
void Module::RestoreTypeHandlePointerRaw(TypeHandle *pHandle, Module* pContainingModule, ClassLoadLevel level)
{
    CONTRACTL
    {
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else {INJECT_FAULT(COMPlusThrowOM(););}
        MODE_ANY;
    }
    CONTRACTL_END;

#ifdef _DEBUG
    if (pContainingModule != NULL)
    {
        Module * dbg_pZapModule = ExecutionManager::FindZapModule(dac_cast<TADDR>(pHandle));
        _ASSERTE((dbg_pZapModule == NULL) || (pContainingModule == dbg_pZapModule));
    }
#endif //_DEBUG

    TADDR fixup;

    if (IS_ALIGNED(pHandle, sizeof(TypeHandle)))
    {
        // Ensure that the compiler won't fetch the value twice
        fixup = VolatileLoadWithoutBarrier((TADDR *)pHandle);
    }
    else
    {
        // This is necessary to handle in-place fixups (see by FixupTypeHandlePointerInplace)
        // in stubs-as-il signatures.

        //
        // protect this unaligned read with the Module Crst for the rare case that
        // the TypeHandle to fixup is in a signature and unaligned.
        //
        if (NULL == pContainingModule)
        {
            pContainingModule = ExecutionManager::FindZapModule(dac_cast<TADDR>(pHandle));
        }
        CrstHolder ch(&pContainingModule->m_Crst);
        fixup = *(TADDR UNALIGNED *)pHandle;
    }

    if (CORCOMPILE_IS_POINTER_TAGGED(fixup))
    {
#ifdef HOST_64BIT
        CONSISTENCY_CHECK((CORCOMPILE_UNTAG_TOKEN(fixup)>>32) == 0);
#endif

        RVA fixupRva = (RVA) CORCOMPILE_UNTAG_TOKEN(fixup);

        if (NULL == pContainingModule)
        {
            pContainingModule = ExecutionManager::FindZapModule(dac_cast<TADDR>(pHandle));
        }
        PREFIX_ASSUME(pContainingModule != NULL);

        _ASSERTE((*pContainingModule->GetNativeFixupBlobData(fixupRva) & ~ENCODE_MODULE_OVERRIDE) == ENCODE_TYPE_HANDLE);

        Module * pInfoModule;
        PCCOR_SIGNATURE pBlobData = pContainingModule->GetEncodedSig(fixupRva, &pInfoModule);

        TypeHandle thResolved = ZapSig::DecodeType(pContainingModule,
                                                           pInfoModule,
                                                           pBlobData,
                                                           level);
        if (IS_ALIGNED(pHandle, sizeof(TypeHandle)))
        {
            *pHandle = thResolved;
        }
        else
        {
            //
            // protect this unaligned write with the Module Crst for the rare case that
            // the TypeHandle to fixup is in a signature and unaligned.
            //
            CrstHolder ch(&pContainingModule->m_Crst);
            *(TypeHandle UNALIGNED *)pHandle = thResolved;
        }
    }
    else if (fixup != NULL)
    {
        ClassLoader::EnsureLoaded(TypeHandle::FromTAddr(fixup), level);
    }
}

/*static*/
void Module::RestoreTypeHandlePointer(FixupPointer<TypeHandle> * pHandle,
                                      Module *pContainingModule,
                                      ClassLoadLevel level)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (pHandle->IsNull())
        return;

    if (pHandle->IsTagged())
    {
        RestoreTypeHandlePointerRaw(pHandle->GetValuePtr(), pContainingModule, level);
    }
    else
    {
        ClassLoader::EnsureLoaded(pHandle->GetValue(), level);
    }
}

/*static*/
void Module::RestoreTypeHandlePointer(RelativeFixupPointer<TypeHandle> * pHandle,
                                      Module *pContainingModule,
                                      ClassLoadLevel level)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (pHandle->IsNull())
        return;

    if (pHandle->IsTagged((TADDR)pHandle))
    {
        RestoreTypeHandlePointerRaw(pHandle->GetValuePtr(), pContainingModule, level);
    }
    else
    {
        ClassLoader::EnsureLoaded(pHandle->GetValue((TADDR)pHandle), level);
    }
}

/*static*/
void Module::RestoreMethodDescPointerRaw(PTR_MethodDesc * ppMD, Module *pContainingModule, ClassLoadLevel level)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Ensure that the compiler won't fetch the value twice
    TADDR fixup = VolatileLoadWithoutBarrier((TADDR *)ppMD);

#ifdef _DEBUG
    if (pContainingModule != NULL)
    {
        Module * dbg_pZapModule = ExecutionManager::FindZapModule(dac_cast<TADDR>(ppMD));
        _ASSERTE((dbg_pZapModule == NULL) || (pContainingModule == dbg_pZapModule));
    }
#endif //_DEBUG

    if (CORCOMPILE_IS_POINTER_TAGGED(fixup))
    {
        GCX_PREEMP();

#ifdef HOST_64BIT
        CONSISTENCY_CHECK((CORCOMPILE_UNTAG_TOKEN(fixup)>>32) == 0);
#endif

        RVA fixupRva = (RVA) CORCOMPILE_UNTAG_TOKEN(fixup);

        if (pContainingModule == NULL)
            pContainingModule = ExecutionManager::FindZapModule(dac_cast<TADDR>(ppMD));
        PREFIX_ASSUME(pContainingModule != NULL);

        _ASSERTE((*pContainingModule->GetNativeFixupBlobData(fixupRva) & ~ENCODE_MODULE_OVERRIDE) == ENCODE_METHOD_HANDLE);

        Module * pInfoModule;
        PCCOR_SIGNATURE pBlobData = pContainingModule->GetEncodedSig(fixupRva, &pInfoModule);

        *ppMD =  ZapSig::DecodeMethod(pContainingModule,
                                      pInfoModule,
                                      pBlobData);
    }
    else if (*ppMD) {
        (*ppMD)->CheckRestore(level);
    }
}

/*static*/
void Module::RestoreMethodDescPointer(FixupPointer<PTR_MethodDesc> * ppMD,
                                      Module *pContainingModule,
                                      ClassLoadLevel level)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (ppMD->IsNull())
        return;

    if (ppMD->IsTagged())
    {
        RestoreMethodDescPointerRaw(ppMD->GetValuePtr(), pContainingModule, level);
    }
    else
    {
        ppMD->GetValue()->CheckRestore(level);
    }
}

/*static*/
void Module::RestoreMethodDescPointer(RelativeFixupPointer<PTR_MethodDesc> * ppMD,
                                      Module *pContainingModule,
                                      ClassLoadLevel level)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (ppMD->IsNull())
        return;

    if (ppMD->IsTagged((TADDR)ppMD))
    {
        RestoreMethodDescPointerRaw(ppMD->GetValuePtr(), pContainingModule, level);
    }
    else
    {
        ppMD->GetValue((TADDR)ppMD)->CheckRestore(level);
    }
}

/*static*/
void Module::RestoreFieldDescPointer(RelativeFixupPointer<PTR_FieldDesc> * ppFD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!ppFD->IsTagged())
        return;

    PTR_FieldDesc * ppValue = ppFD->GetValuePtr();

    // Ensure that the compiler won't fetch the value twice
    TADDR fixup = VolatileLoadWithoutBarrier((TADDR *)ppValue);

    if (CORCOMPILE_IS_POINTER_TAGGED(fixup))
    {
#ifdef HOST_64BIT
        CONSISTENCY_CHECK((CORCOMPILE_UNTAG_TOKEN(fixup)>>32) == 0);
#endif

        Module * pContainingModule = ExecutionManager::FindZapModule((TADDR)ppValue);
        PREFIX_ASSUME(pContainingModule != NULL);

        RVA fixupRva = (RVA) CORCOMPILE_UNTAG_TOKEN(fixup);

        _ASSERTE((*pContainingModule->GetNativeFixupBlobData(fixupRva) & ~ENCODE_MODULE_OVERRIDE) == ENCODE_FIELD_HANDLE);

        Module * pInfoModule;
        PCCOR_SIGNATURE pBlobData = pContainingModule->GetEncodedSig(fixupRva, &pInfoModule);

        *ppValue =  ZapSig::DecodeField(pContainingModule,
                                        pInfoModule,
                                        pBlobData);
    }
}


//-----------------------------------------------------------------------------

#if 0

        This diagram illustrates the layout of fixups in the ngen image.
        This is the case where function foo2 has a class-restore fixup
        for class C1 in b.dll.

                                  zapBase+curTableVA+rva /         FixupList (see Fixup Encoding below)
                                  m_pFixupBlobs
                                                            +-------------------+
                  pEntry->VA +--------------------+         |     non-NULL      | foo1
                             |Handles             |         +-------------------+
ZapHeader.ImportTable        |                    |         |     non-NULL      |
                             |                    |         +-------------------+
   +------------+            +--------------------+         |     non-NULL      |
   |a.dll       |            |Class cctors        |<---+    +-------------------+
   |            |            |                    |     \   |         0         |
   |            |     p->VA/ |                    |<---+ \  +===================+
   |            |      blobs +--------------------+     \ +-------non-NULL      | foo2
   +------------+            |Class restore       |      \  +-------------------+
   |b.dll       |            |                    |       +-------non-NULL      |
   |            |            |                    |         +-------------------+
   |  token_C1  |<--------------blob(=>fixedUp/0) |<--pBlob--------index        |
   |            | \          |                    |         +-------------------+
   |            |  \         +--------------------+         |     non-NULL      |
   |            |   \        |                    |         +-------------------+
   |            |    \       |        .           |         |         0         |
   |            |     \      |        .           |         +===================+
   +------------+      \     |        .           |         |         0         | foo3
                        \    |                    |         +===================+
                         \   +--------------------+         |     non-NULL      | foo4
                          \  |Various fixups that |         +-------------------+
                           \ |need too happen     |         |         0         |
                            \|                    |         +===================+
                             |(CorCompileTokenTable)
                             |                    |
               pEntryEnd->VA +--------------------+



#endif // 0

#endif
#endif

#ifndef DACCESS_COMPILE
//-----------------------------------------------------------------------------

void Module::RunEagerFixups()
{
    STANDARD_VM_CONTRACT;

    COUNT_T nSections;
    PTR_CORCOMPILE_IMPORT_SECTION pSections = GetImportSections(&nSections);

    if (nSections == 0)
        return;

#ifdef _DEBUG
    // Loading types during eager fixup is not a tested scenario. Make bugs out of any attempts to do so in a
    // debug build. Use holder to recover properly in case of exception.
    class ForbidTypeLoadHolder
    {
    public:
        ForbidTypeLoadHolder()
        {
            BEGIN_FORBID_TYPELOAD();
        }

        ~ForbidTypeLoadHolder()
        {
            END_FORBID_TYPELOAD();
        }
    }
    forbidTypeLoad;
#endif

    // TODO: Verify that eager fixup dependency graphs can contain no cycles
    OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);

    NativeImage *compositeNativeImage = GetCompositeNativeImage();
    if (compositeNativeImage != NULL)
    {
        // For composite images, multiple modules may request initializing eager fixups
        // from multiple threads so we need to lock their resolution.
        if (compositeNativeImage->EagerFixupsHaveRun())
        {
            return;
        }
        CrstHolder compositeEagerFixups(compositeNativeImage->EagerFixupsLock());
        if (compositeNativeImage->EagerFixupsHaveRun())
        {
            return;
        }
        RunEagerFixupsUnlocked();
        compositeNativeImage->SetEagerFixupsHaveRun();
    }
    else
    {
        // Per-module eager fixups don't need locking
        RunEagerFixupsUnlocked();
    }
}

void Module::RunEagerFixupsUnlocked()
{
    COUNT_T nSections;
    PTR_CORCOMPILE_IMPORT_SECTION pSections = GetImportSections(&nSections);
    PEImageLayout *pNativeImage = GetNativeOrReadyToRunImage();

    for (COUNT_T iSection = 0; iSection < nSections; iSection++)
    {
        PTR_CORCOMPILE_IMPORT_SECTION pSection = pSections + iSection;

        if ((pSection->Flags & CORCOMPILE_IMPORT_FLAGS_EAGER) == 0)
            continue;

        COUNT_T tableSize;
        TADDR tableBase = pNativeImage->GetDirectoryData(&pSection->Section, &tableSize);

        if (pSection->Signatures != NULL)
        {
            PTR_DWORD pSignatures = dac_cast<PTR_DWORD>(pNativeImage->GetRvaData(pSection->Signatures));

            for (SIZE_T * fixupCell = (SIZE_T *)tableBase; fixupCell < (SIZE_T *)(tableBase + tableSize); fixupCell++)
            {
                SIZE_T fixupIndex = fixupCell - (SIZE_T *)tableBase;
                if (!LoadDynamicInfoEntry(this, pSignatures[fixupIndex], fixupCell))
                {
                    if (IsReadyToRun())
                    {
                        GetReadyToRunInfo()->DisableAllR2RCode();
                    }
                    else
                    {
                        _ASSERTE(!"LoadDynamicInfoEntry failed");
                        ThrowHR(COR_E_BADIMAGEFORMAT);
                    }
                }
                else
                {
                    _ASSERTE(*fixupCell != NULL);
                }
            }
        }
        else
        {
            for (SIZE_T * fixupCell = (SIZE_T *)tableBase; fixupCell < (SIZE_T *)(tableBase + tableSize); fixupCell++)
            {
                // Ensure that the compiler won't fetch the value twice
                SIZE_T fixup = VolatileLoadWithoutBarrier(fixupCell);

                // This method may execute multiple times in multi-domain scenarios. Check that the fixup has not been
                // fixed up yet.
                if (CORCOMPILE_IS_FIXUP_TAGGED(fixup, pSection))
                {
                    if (!LoadDynamicInfoEntry(this, (RVA)CORCOMPILE_UNTAG_TOKEN(fixup), fixupCell))
                    {
                        if (IsReadyToRun())
                        {
                            GetReadyToRunInfo()->DisableAllR2RCode();
                        }
                        else
                        {
                            _ASSERTE(!"LoadDynamicInfoEntry failed");
                            ThrowHR(COR_E_BADIMAGEFORMAT);
                        }
                    }
                    _ASSERTE(!CORCOMPILE_IS_FIXUP_TAGGED(*fixupCell, pSection));
                }
            }
        }
    }
}
#endif // !DACCESS_COMPILE

#ifdef FEATURE_PREJIT

#ifndef DACCESS_COMPILE
void Module::LoadTokenTables()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(HasNativeImage());
    }
    CONTRACTL_END;

#ifndef CROSSGEN_COMPILE
    if (NingenEnabled())
        return;

    CORCOMPILE_EE_INFO_TABLE *pEEInfo = GetNativeImage()->GetNativeEEInfoTable();
    PREFIX_ASSUME(pEEInfo != NULL);

    pEEInfo->inlinedCallFrameVptr = InlinedCallFrame::GetMethodFrameVPtr();
    pEEInfo->addrOfCaptureThreadGlobal = (LONG *)&g_TrapReturningThreads;

    //CoreClr doesn't always have the debugger loaded
    //patch up the ngen image to point to this address so that the JIT bypasses JMC if there is no debugger
    static DWORD g_dummyJMCFlag = 0;
    pEEInfo->addrOfJMCFlag = g_pDebugInterface ? g_pDebugInterface->GetJMCFlagAddr(this) : &g_dummyJMCFlag;

    pEEInfo->gsCookie = GetProcessGSCookie();

    if (!IsSystem())
    {
        pEEInfo->emptyString = (CORINFO_Object **)StringObject::GetEmptyStringRefPtr();
    }

    pEEInfo->threadTlsIndex = TLS_OUT_OF_INDEXES;
    pEEInfo->rvaStaticTlsIndex = NULL;
#endif // CROSSGEN_COMPILE
}

#endif // !DACCESS_COMPILE

// Returns the RVA to the compressed debug information blob for the given method

CORCOMPILE_DEBUG_ENTRY Module::GetMethodDebugInfoOffset(MethodDesc *pMD)
{
    CONTRACT(CORCOMPILE_DEBUG_ENTRY)
    {
        INSTANCE_CHECK;
        PRECONDITION(HasNativeImage());
        PRECONDITION(CheckPointer(pMD) && pMD->IsPreImplemented());
        POSTCONDITION(GetNativeImage()->CheckRva(RETVAL, NULL_OK));
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    if (!GetNativeImage()->HasNativeDebugMap() || pMD->IsRuntimeSupplied())
        RETURN 0;

    COUNT_T size;
    PTR_CORCOMPILE_DEBUG_RID_ENTRY ridTable =
        dac_cast<PTR_CORCOMPILE_DEBUG_RID_ENTRY>(GetNativeImage()->GetNativeDebugMap(&size));

    COUNT_T count = size / sizeof(CORCOMPILE_DEBUG_RID_ENTRY);
    // The size should be odd for better hashing
    _ASSERTE((count & 1) != 0);

    CORCOMPILE_DEBUG_RID_ENTRY ridEntry = ridTable[GetDebugRidEntryHash(pMD->GetMemberDef()) % count];

    // Do we have multiple code corresponding to the same RID
    if (!IsMultipleLabelledEntries(ridEntry))
    {
        RETURN(ridEntry);
    }

    PTR_CORCOMPILE_DEBUG_LABELLED_ENTRY pLabelledEntry =
        PTR_CORCOMPILE_DEBUG_LABELLED_ENTRY
        (GetNativeImage()->GetRvaData(ridEntry &
                                      ~CORCOMPILE_DEBUG_MULTIPLE_ENTRIES));

    DWORD codeRVA = GetNativeImage()->
        GetDataRva((const TADDR)pMD->GetNativeCode());
#if defined(TARGET_ARM)
    // Since the Thumb Bit is set on ARM, the RVA calculated above will have it set as well
    // and will result in the failure of checks in the loop below. Hence, mask off the
    // bit before proceeding ahead.
    codeRVA = ThumbCodeToDataPointer<DWORD, DWORD>(codeRVA);
#endif // TARGET_ARM

    for (;;)
    {
        if (pLabelledEntry->nativeCodeRVA == codeRVA)
        {
            RETURN (pLabelledEntry->debugInfoOffset & ~CORCOMPILE_DEBUG_MULTIPLE_ENTRIES);
        }

        if (!IsMultipleLabelledEntries(pLabelledEntry->debugInfoOffset))
        {
            break;
        }

        pLabelledEntry++;
    }

    _ASSERTE(!"Debug info not found - corrupted ngen image?");
    RETURN (0);
}

PTR_BYTE Module::GetNativeDebugInfo(MethodDesc * pMD)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(HasNativeImage());
        PRECONDITION(CheckPointer(pMD));
        PRECONDITION(pMD->GetZapModule() == this);
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    CORCOMPILE_DEBUG_ENTRY debugInfoOffset = GetMethodDebugInfoOffset(pMD);

    if (debugInfoOffset == 0)
        return NULL;

    return dac_cast<PTR_BYTE>(GetNativeImage()->GetRvaData(debugInfoOffset));
}
#endif //FEATURE_PREJIT

#ifndef DACCESS_COMPILE

//-----------------------------------------------------------------------------

BOOL Module::FixupNativeEntry(CORCOMPILE_IMPORT_SECTION* pSection, SIZE_T fixupIndex, SIZE_T* fixupCell)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(fixupCell));
    }
    CONTRACTL_END;

    // Ensure that the compiler won't fetch the value twice
    SIZE_T fixup = VolatileLoadWithoutBarrier(fixupCell);

    if (pSection->Signatures != NULL)
    {
        if (fixup == NULL)
        {
            PTR_DWORD pSignatures = dac_cast<PTR_DWORD>(GetNativeOrReadyToRunImage()->GetRvaData(pSection->Signatures));

            if (!LoadDynamicInfoEntry(this, pSignatures[fixupIndex], fixupCell))
                return FALSE;

            _ASSERTE(*fixupCell != NULL);
        }
    }
    else
    {
        if (CORCOMPILE_IS_FIXUP_TAGGED(fixup, pSection))
        {
            // Fixup has not been fixed up yet
            if (!LoadDynamicInfoEntry(this, (RVA)CORCOMPILE_UNTAG_TOKEN(fixup), fixupCell))
                return FALSE;

            _ASSERTE(!CORCOMPILE_IS_FIXUP_TAGGED(*fixupCell, pSection));
        }
        else
        {
            //
            // Handle tables are special. We may need to restore static handle or previous
            // attempts to load handle could have been partial.
            //
            if (pSection->Type == CORCOMPILE_IMPORT_TYPE_TYPE_HANDLE)
            {
                TypeHandle::FromPtr((void*)fixup).CheckRestore();
            }
            else
                if (pSection->Type == CORCOMPILE_IMPORT_TYPE_METHOD_HANDLE)
                {
                    ((MethodDesc*)(fixup))->CheckRestore();
                }
        }
    }

    return TRUE;
}

//
// Profile data management
//

ICorJitInfo::BlockCounts * Module::AllocateMethodBlockCounts(mdToken _token, DWORD _count, DWORD _ILSize)
{
    CONTRACT (ICorJitInfo::BlockCounts*)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(CONTRACT_RETURN NULL;);
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    assert(_ILSize != 0);

    DWORD   listSize   = sizeof(CORCOMPILE_METHOD_PROFILE_LIST);
    DWORD   headerSize = sizeof(CORBBTPROF_METHOD_HEADER);
    DWORD   blockSize  = _count * sizeof(CORBBTPROF_BLOCK_DATA);
    DWORD   totalSize  = listSize + headerSize + blockSize;

    BYTE *  memory     = (BYTE *) (void *) this->m_pAssembly->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(totalSize));

    CORCOMPILE_METHOD_PROFILE_LIST * methodProfileList = (CORCOMPILE_METHOD_PROFILE_LIST *) (memory + 0);
    CORBBTPROF_METHOD_HEADER *       methodProfileData = (CORBBTPROF_METHOD_HEADER *)       (memory + listSize);

    // Note: Memory allocated on the LowFrequencyHeap is zero filled

    methodProfileData->size          = headerSize + blockSize;
    methodProfileData->method.token  = _token;
    methodProfileData->method.ILSize = _ILSize;
    methodProfileData->method.cBlock = _count;

    assert(methodProfileData->size == methodProfileData->Size());

    // Link it to the per module list of profile data buffers

    methodProfileList->next = m_methodProfileList;
    m_methodProfileList     = methodProfileList;

    RETURN ((ICorJitInfo::BlockCounts *) &methodProfileData->method.block[0]);
}

HANDLE Module::OpenMethodProfileDataLogFile(GUID mvid)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    HANDLE profileDataFile = INVALID_HANDLE_VALUE;

    SString path;
    LPCWSTR assemblyPath = m_file->GetPath();
    LPCWSTR ibcDir = g_pConfig->GetZapBBInstrDir();     // should we put the ibc data into a particular directory?
    if (ibcDir == 0) {
        path.Set(assemblyPath);                         // no, then put it beside the IL dll
    }
    else {
        LPCWSTR assemblyFileName = wcsrchr(assemblyPath, DIRECTORY_SEPARATOR_CHAR_W);
        if (assemblyFileName)
            assemblyFileName++;                         // skip past the \ char
        else
            assemblyFileName = assemblyPath;

        path.Set(ibcDir);                               // yes, put it in the directory, named with the assembly name.
        path.Append(DIRECTORY_SEPARATOR_CHAR_W);
        path.Append(assemblyFileName);
    }

    SString::Iterator ext = path.End();                 // remove the extension
    if (path.FindBack(ext, '.'))
        path.Truncate(ext);
    path.Append(W(".ibc"));               // replace with .ibc extension

    profileDataFile = WszCreateFile(path, GENERIC_READ | GENERIC_WRITE, 0, NULL,
                                    OPEN_ALWAYS,
                                    FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN,
                                    NULL);

    if (profileDataFile == INVALID_HANDLE_VALUE) COMPlusThrowWin32();

    DWORD count;
    CORBBTPROF_FILE_HEADER fileHeader;

    SetFilePointer(profileDataFile, 0, NULL, FILE_BEGIN);
    BOOL result = ReadFile(profileDataFile, &fileHeader, sizeof(fileHeader), &count, NULL);
    if (result                                                    &&
        (count                 == sizeof(fileHeader))             &&
        (fileHeader.HeaderSize == sizeof(CORBBTPROF_FILE_HEADER)) &&
        (fileHeader.Magic      == CORBBTPROF_MAGIC)               &&
        (fileHeader.Version    == CORBBTPROF_CURRENT_VERSION)     &&
        (fileHeader.MVID       == mvid))
    {
        //
        // The existing file was from the same assembly version - just append to it.
        //

        SetFilePointer(profileDataFile, 0, NULL, FILE_END);
    }
    else
    {
        //
        // Either this is a new file, or it's from a previous version.  Replace the contents.
        //

        SetFilePointer(profileDataFile, 0, NULL, FILE_BEGIN);
    }

    return profileDataFile;
}

// Note that this method cleans up the profile buffers, so it's crucial that
// no managed code in the module is allowed to run once this method has
// been called!

class ProfileMap
{
public:
    SIZE_T getCurrentOffset() {WRAPPER_NO_CONTRACT; return buffer.Size();}

    void * getOffsetPtr(SIZE_T offset)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(offset <= buffer.Size());
        return ((void *) (((char *) buffer.Ptr()) + offset));
    }

    void *Allocate(SIZE_T size)
    {
        CONTRACT(void *)
        {
            INSTANCE_CHECK;
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
            INJECT_FAULT(CONTRACT_RETURN NULL;);
            POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        }
        CONTRACT_END;

        SIZE_T oldSize = buffer.Size();
        buffer.ReSizeThrows(oldSize + size);
        RETURN getOffsetPtr(oldSize);
    }

private:
    CQuickBytes buffer;
};

class ProfileEmitter
{
public:

    ProfileEmitter()
    {
        LIMITED_METHOD_CONTRACT;
        pSectionList = NULL;
    }

    ~ProfileEmitter()
    {
        WRAPPER_NO_CONTRACT;
        while (pSectionList)
        {
            SectionList *temp = pSectionList->next;
            delete pSectionList;
            pSectionList = temp;
        }
    }

    ProfileMap *EmitNewSection(SectionFormat format)
    {
        WRAPPER_NO_CONTRACT;
        SectionList *s = new SectionList();

        s->format    = format;
        s->next      = pSectionList;
        pSectionList = s;

        return &s->profileMap;
    }

    //
    // Serialize the profile sections into pMap
    //

    void Serialize(ProfileMap *profileMap, GUID mvid)
    {
        CONTRACTL
        {
            INSTANCE_CHECK;
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
            INJECT_FAULT(COMPlusThrowOM());
        }
        CONTRACTL_END;

        //
        // Allocate the file header
        //
        {
            CORBBTPROF_FILE_HEADER *fileHeader;
            fileHeader = (CORBBTPROF_FILE_HEADER *) profileMap->Allocate(sizeof(CORBBTPROF_FILE_HEADER));

            fileHeader->HeaderSize = sizeof(CORBBTPROF_FILE_HEADER);
            fileHeader->Magic      = CORBBTPROF_MAGIC;
            fileHeader->Version    = CORBBTPROF_CURRENT_VERSION;
            fileHeader->MVID       = mvid;
        }

        //
        // Count the number of sections
        //
        ULONG32 numSections = 0;
        for (SectionList *p = pSectionList; p; p = p->next)
        {
            numSections++;
        }

        //
        // Allocate the section table
        //
        SIZE_T tableEntryOffset;
        {
            CORBBTPROF_SECTION_TABLE_HEADER *tableHeader;
            tableHeader = (CORBBTPROF_SECTION_TABLE_HEADER *)
                profileMap->Allocate(sizeof(CORBBTPROF_SECTION_TABLE_HEADER));

            tableHeader->NumEntries = numSections;
            tableEntryOffset = profileMap->getCurrentOffset();

            CORBBTPROF_SECTION_TABLE_ENTRY *tableEntry;
            tableEntry = (CORBBTPROF_SECTION_TABLE_ENTRY *)
                profileMap->Allocate(sizeof(CORBBTPROF_SECTION_TABLE_ENTRY) * numSections);
        }

        //
        // Allocate the data sections
        //
        {
            ULONG secCount = 0;
            for (SectionList *pSec = pSectionList; pSec; pSec = pSec->next, secCount++)
            {
                SIZE_T offset = profileMap->getCurrentOffset();
                assert((offset & 0x3) == 0);

                SIZE_T actualSize  = pSec->profileMap.getCurrentOffset();
                SIZE_T alignUpSize = AlignUp(actualSize, sizeof(DWORD));

                profileMap->Allocate(alignUpSize);

                memcpy(profileMap->getOffsetPtr(offset), pSec->profileMap.getOffsetPtr(0), actualSize);
                if (alignUpSize > actualSize)
                {
                    memset(((BYTE*)profileMap->getOffsetPtr(offset))+actualSize, 0, (alignUpSize - actualSize));
                }

                CORBBTPROF_SECTION_TABLE_ENTRY *tableEntry;
                tableEntry = (CORBBTPROF_SECTION_TABLE_ENTRY *) profileMap->getOffsetPtr(tableEntryOffset);
                tableEntry += secCount;
                tableEntry->FormatID    = pSec->format;
                tableEntry->Data.Offset = offset;
                tableEntry->Data.Size   = alignUpSize;
            }
        }

        //
        // Allocate the end token marker
        //
        {
            ULONG *endToken;
            endToken = (ULONG *) profileMap->Allocate(sizeof(ULONG));

            *endToken = CORBBTPROF_END_TOKEN;
        }
    }

private:
    struct SectionList
    {
        SectionFormat format;
        ProfileMap    profileMap;
        SectionList   *next;
    };
    SectionList *  pSectionList;
};


/*static*/ idTypeSpec          TypeSpecBlobEntry::s_lastTypeSpecToken                   = idTypeSpecNil;
/*static*/ idMethodSpec        MethodSpecBlobEntry::s_lastMethodSpecToken               = idMethodSpecNil;
/*static*/ idExternalNamespace ExternalNamespaceBlobEntry::s_lastExternalNamespaceToken = idExternalNamespaceNil;
/*static*/ idExternalType      ExternalTypeBlobEntry::s_lastExternalTypeToken           = idExternalTypeNil;
/*static*/ idExternalSignature ExternalSignatureBlobEntry::s_lastExternalSignatureToken = idExternalSignatureNil;
/*static*/ idExternalMethod    ExternalMethodBlobEntry::s_lastExternalMethodToken       = idExternalMethodNil;


inline static size_t HashCombine(size_t h1, size_t h2)
{
    LIMITED_METHOD_CONTRACT;

    size_t result = (h1 * 129) ^ h2;
    return result;
}

bool        TypeSpecBlobEntry::IsEqual(const ProfilingBlobEntry *  other) const
{
    WRAPPER_NO_CONTRACT;

    if (this->kind() != other->kind())
        return false;

    const TypeSpecBlobEntry *  other2 = static_cast<const TypeSpecBlobEntry *>(other);

    if (this->cbSig() != other2->cbSig())
        return false;

    PCCOR_SIGNATURE  p1 = this->pSig();
    PCCOR_SIGNATURE  p2 = other2->pSig();

    for (DWORD i=0; (i < this->cbSig()); i++)
        if (p1[i] != p2[i])
            return false;

    return true;
}

size_t     TypeSpecBlobEntry::Hash() const
{
    WRAPPER_NO_CONTRACT;

    size_t hashValue = HashInit();

    PCCOR_SIGNATURE  p1 = pSig();
    for (DWORD i=0; (i < cbSig()); i++)
        hashValue = HashCombine(hashValue, p1[i]);

    return hashValue;
}

TypeSpecBlobEntry::TypeSpecBlobEntry(DWORD _cbSig, PCCOR_SIGNATURE _pSig)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(_cbSig > 0);
        PRECONDITION(CheckPointer(_pSig));
    }
    CONTRACTL_END;

    m_token  = idTypeSpecNil;
    m_flags  = 0;
    m_cbSig  = 0;

    COR_SIGNATURE * pNewSig = (COR_SIGNATURE *) new (nothrow) BYTE[_cbSig];
    if (pNewSig != NULL)
    {
        m_flags  = 0;
        m_cbSig  = _cbSig;
        memcpy(pNewSig, _pSig, _cbSig);
    }
    m_pSig = const_cast<PCCOR_SIGNATURE>(pNewSig);
}

/* static */ const TypeSpecBlobEntry *  TypeSpecBlobEntry::FindOrAdd(PTR_Module      pModule,
                                                                     DWORD           _cbSig,
                                                                     PCCOR_SIGNATURE _pSig)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if ((_cbSig == 0) || (_pSig == NULL))
        return NULL;

    TypeSpecBlobEntry sEntry(_cbSig, _pSig);

    const ProfilingBlobEntry *  pEntry = pModule->GetProfilingBlobTable()->Lookup(&sEntry);
    if (pEntry == NULL)
    {
        //
        // Not Found, add a new type spec profiling blob entry
        //
        TypeSpecBlobEntry * newEntry = new (nothrow) TypeSpecBlobEntry(_cbSig, _pSig);
        if (newEntry == NULL)
            return NULL;

        newEntry->newToken();                 // Assign a new ibc type spec token
        CONTRACT_VIOLATION(ThrowsViolation);
        pModule->GetProfilingBlobTable()->Add(newEntry);
        pEntry = newEntry;
    }

    //
    // Return the type spec entry that we found or the new one that we just created
    //
    _ASSERTE(pEntry->kind() == ParamTypeSpec);
    return static_cast<const TypeSpecBlobEntry *>(pEntry);
}

bool        MethodSpecBlobEntry::IsEqual(const ProfilingBlobEntry *  other) const
{
    WRAPPER_NO_CONTRACT;

    if (this->kind() != other->kind())
        return false;

    const MethodSpecBlobEntry *  other2 = static_cast<const MethodSpecBlobEntry *>(other);

    if (this->cbSig() != other2->cbSig())
        return false;

    PCCOR_SIGNATURE  p1 = this->pSig();
    PCCOR_SIGNATURE  p2 = other2->pSig();

    for (DWORD i=0; (i < this->cbSig()); i++)
        if (p1[i] != p2[i])
            return false;

    return true;
}

size_t     MethodSpecBlobEntry::Hash() const
{
    WRAPPER_NO_CONTRACT;

    size_t hashValue = HashInit();

    PCCOR_SIGNATURE  p1 = pSig();
    for (DWORD i=0; (i < cbSig()); i++)
        hashValue = HashCombine(hashValue, p1[i]);

    return hashValue;
}

MethodSpecBlobEntry::MethodSpecBlobEntry(DWORD _cbSig, PCCOR_SIGNATURE _pSig)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(_cbSig > 0);
        PRECONDITION(CheckPointer(_pSig));
    }
    CONTRACTL_END;

    m_token  = idMethodSpecNil;
    m_flags  = 0;
    m_cbSig  = 0;

    COR_SIGNATURE * pNewSig = (COR_SIGNATURE *) new (nothrow) BYTE[_cbSig];
    if (pNewSig != NULL)
    {
        m_flags  = 0;
        m_cbSig  = _cbSig;
        memcpy(pNewSig, _pSig, _cbSig);
    }
    m_pSig = const_cast<PCCOR_SIGNATURE>(pNewSig);
}

/* static */ const MethodSpecBlobEntry *  MethodSpecBlobEntry::FindOrAdd(PTR_Module      pModule,
                                                                         DWORD           _cbSig,
                                                                         PCCOR_SIGNATURE _pSig)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if ((_cbSig == 0) || (_pSig == NULL))
        return NULL;

    MethodSpecBlobEntry sEntry(_cbSig, _pSig);

    const ProfilingBlobEntry * pEntry = pModule->GetProfilingBlobTable()->Lookup(&sEntry);
    if (pEntry == NULL)
    {
        //
        // Not Found, add a new method spec profiling blob entry
        //
        MethodSpecBlobEntry * newEntry = new (nothrow) MethodSpecBlobEntry(_cbSig, _pSig);
        if (newEntry == NULL)
            return NULL;

        newEntry->newToken();                 // Assign a new ibc method spec token
        CONTRACT_VIOLATION(ThrowsViolation);
        pModule->GetProfilingBlobTable()->Add(newEntry);
        pEntry = newEntry;
    }

    //
    // Return the method spec entry that we found or the new one that we just created
    //
    _ASSERTE(pEntry->kind() == ParamMethodSpec);
    return static_cast<const MethodSpecBlobEntry *>(pEntry);
}

bool        ExternalNamespaceBlobEntry::IsEqual(const ProfilingBlobEntry *  other) const
{
    WRAPPER_NO_CONTRACT;

    if (this->kind() != other->kind())
        return false;

    const ExternalNamespaceBlobEntry *  other2 = static_cast<const ExternalNamespaceBlobEntry *>(other);

    if (this->cbName() != other2->cbName())
        return false;

    LPCSTR p1 = this->pName();
    LPCSTR p2 = other2->pName();

    for (DWORD i=0; (i < this->cbName()); i++)
        if (p1[i] != p2[i])
            return false;

    return true;
}

size_t     ExternalNamespaceBlobEntry::Hash() const
{
    WRAPPER_NO_CONTRACT;

    size_t hashValue = HashInit();

    LPCSTR p1 = pName();
    for (DWORD i=0; (i < cbName()); i++)
        hashValue = HashCombine(hashValue, p1[i]);

    return hashValue;
}

ExternalNamespaceBlobEntry::ExternalNamespaceBlobEntry(LPCSTR _pName)
{
   CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(_pName));
    }
    CONTRACTL_END;

    m_token  = idExternalNamespaceNil;
    m_cbName = 0;
    m_pName  = NULL;

    DWORD _cbName = (DWORD) strlen(_pName) + 1;
    LPSTR * pName = (LPSTR *) new (nothrow) CHAR[_cbName];
    if (pName != NULL)
    {
        m_cbName = _cbName;
        memcpy(pName, _pName, _cbName);
        m_pName  = (LPCSTR) pName;
    }
}

/* static */ const ExternalNamespaceBlobEntry *  ExternalNamespaceBlobEntry::FindOrAdd(PTR_Module pModule, LPCSTR _pName)
{
   CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if ((_pName == NULL) || (::strlen(_pName) == 0))
        return NULL;

    ExternalNamespaceBlobEntry sEntry(_pName);

    const ProfilingBlobEntry *  pEntry = pModule->GetProfilingBlobTable()->Lookup(&sEntry);
    if (pEntry == NULL)
    {
        //
        // Not Found, add a new external namespace blob entry
        //
        ExternalNamespaceBlobEntry * newEntry = new (nothrow) ExternalNamespaceBlobEntry(_pName);
        if (newEntry == NULL)
            return NULL;

        newEntry->newToken();                 // Assign a new ibc external namespace token
        CONTRACT_VIOLATION(ThrowsViolation);
        pModule->GetProfilingBlobTable()->Add(newEntry);
        pEntry = newEntry;
    }

    //
    // Return the external namespace entry that we found or the new one that we just created
    //
    _ASSERTE(pEntry->kind() == ExternalNamespaceDef);
    return static_cast<const ExternalNamespaceBlobEntry *>(pEntry);
}

bool        ExternalTypeBlobEntry::IsEqual(const ProfilingBlobEntry *  other) const
{
    WRAPPER_NO_CONTRACT;

    if (this->kind() != other->kind())
        return false;

    const ExternalTypeBlobEntry *  other2 = static_cast<const ExternalTypeBlobEntry *>(other);

    if (this->assemblyRef() != other2->assemblyRef())
        return false;

    if (this->nestedClass() != other2->nestedClass())
        return false;

    if (this->nameSpace() != other2->nameSpace())
        return false;

    if (this->cbName() != other2->cbName())
        return false;

    LPCSTR p1 = this->pName();
    LPCSTR p2 = other2->pName();

    for (DWORD i=0; (i < this->cbName()); i++)
        if (p1[i] != p2[i])
            return false;

    return true;
}

size_t     ExternalTypeBlobEntry::Hash() const
{
    WRAPPER_NO_CONTRACT;

    size_t hashValue = HashInit();

    hashValue = HashCombine(hashValue, assemblyRef());
    hashValue = HashCombine(hashValue, nestedClass());
    hashValue = HashCombine(hashValue, nameSpace());

    LPCSTR p1 = pName();

    for (DWORD i=0; (i < cbName()); i++)
        hashValue = HashCombine(hashValue, p1[i]);

    return hashValue;
}

ExternalTypeBlobEntry::ExternalTypeBlobEntry(mdToken _assemblyRef,
                                             mdToken _nestedClass,
                                             mdToken _nameSpace,
                                             LPCSTR  _pName)
{
   CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(_pName));
    }
    CONTRACTL_END;

    m_token  = idExternalTypeNil;
    m_assemblyRef = mdAssemblyRefNil;
    m_nestedClass = idExternalTypeNil;
    m_nameSpace   = idExternalNamespaceNil;
    m_cbName = 0;
    m_pName  = NULL;

    DWORD _cbName = (DWORD) strlen(_pName) + 1;
    LPSTR * pName = (LPSTR *) new (nothrow) CHAR[_cbName];
    if (pName != NULL)
    {
        m_assemblyRef = _assemblyRef;
        m_nestedClass = _nestedClass;
        m_nameSpace   = _nameSpace;
        m_cbName      = _cbName;
        memcpy(pName, _pName, _cbName);
        m_pName       = (LPCSTR) pName;
    }
}

/* static */ const ExternalTypeBlobEntry *  ExternalTypeBlobEntry::FindOrAdd(PTR_Module pModule,
                                                                             mdToken    _assemblyRef,
                                                                             mdToken    _nestedClass,
                                                                             mdToken    _nameSpace,
                                                                             LPCSTR     _pName)
{
   CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if ((_pName == NULL) || (::strlen(_pName) == 0))
        return NULL;

    ExternalTypeBlobEntry sEntry(_assemblyRef, _nestedClass, _nameSpace, _pName);

    const ProfilingBlobEntry *  pEntry = pModule->GetProfilingBlobTable()->Lookup(&sEntry);
    if (pEntry == NULL)
    {
        //
        // Not Found, add a new external type blob entry
        //
        ExternalTypeBlobEntry *  newEntry = new (nothrow) ExternalTypeBlobEntry(_assemblyRef, _nestedClass, _nameSpace, _pName);
        if (newEntry == NULL)
            return NULL;

        newEntry->newToken();                 // Assign a new ibc external type token
        CONTRACT_VIOLATION(ThrowsViolation);
        pModule->GetProfilingBlobTable()->Add(newEntry);
        pEntry = newEntry;
    }

    //
    // Return the external type entry that we found or the new one that we just created
    //
    _ASSERTE(pEntry->kind() == ExternalTypeDef);
    return static_cast<const ExternalTypeBlobEntry *>(pEntry);
}

bool        ExternalSignatureBlobEntry::IsEqual(const ProfilingBlobEntry *  other) const
{
    WRAPPER_NO_CONTRACT;

    if (this->kind() != other->kind())
        return false;

    const ExternalSignatureBlobEntry *  other2 = static_cast<const ExternalSignatureBlobEntry *>(other);

    if (this->cbSig() != other2->cbSig())
        return false;

    PCCOR_SIGNATURE  p1 = this->pSig();
    PCCOR_SIGNATURE  p2 = other2->pSig();

    for (DWORD i=0; (i < this->cbSig()); i++)
        if (p1[i] != p2[i])
            return false;

    return true;
}

size_t     ExternalSignatureBlobEntry::Hash() const
{
    WRAPPER_NO_CONTRACT;

    size_t hashValue = HashInit();

    hashValue = HashCombine(hashValue, cbSig());

    PCCOR_SIGNATURE  p1 = pSig();

    for (DWORD i=0; (i < cbSig()); i++)
        hashValue = HashCombine(hashValue, p1[i]);

    return hashValue;
}

ExternalSignatureBlobEntry::ExternalSignatureBlobEntry(DWORD _cbSig, PCCOR_SIGNATURE _pSig)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(_cbSig > 0);
        PRECONDITION(CheckPointer(_pSig));
    }
    CONTRACTL_END;

    m_token  = idExternalSignatureNil;
    m_cbSig  = 0;

    COR_SIGNATURE *  pNewSig = (COR_SIGNATURE *) new (nothrow) BYTE[_cbSig];
    if (pNewSig != NULL)
    {
        m_cbSig  = _cbSig;
        memcpy(pNewSig, _pSig, _cbSig);
    }
    m_pSig = const_cast<PCCOR_SIGNATURE>(pNewSig);
}

/* static */ const ExternalSignatureBlobEntry *  ExternalSignatureBlobEntry::FindOrAdd(PTR_Module      pModule,
                                                                                       DWORD           _cbSig,
                                                                                       PCCOR_SIGNATURE _pSig)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if ((_cbSig == 0) || (_pSig == NULL))
        return NULL;

    ExternalSignatureBlobEntry sEntry(_cbSig, _pSig);

    const ProfilingBlobEntry *  pEntry = pModule->GetProfilingBlobTable()->Lookup(&sEntry);
    if (pEntry == NULL)
    {
        //
        // Not Found, add a new external signature blob entry
        //
        ExternalSignatureBlobEntry * newEntry = new (nothrow) ExternalSignatureBlobEntry(_cbSig, _pSig);
        if (newEntry == NULL)
            return NULL;

        newEntry->newToken();                 // Assign a new ibc external signature token
        CONTRACT_VIOLATION(ThrowsViolation);
        pModule->GetProfilingBlobTable()->Add(newEntry);
        pEntry = newEntry;
    }

    //
    // Return the external signature entry that we found or the new one that we just created
    //
    _ASSERTE(pEntry->kind() == ExternalSignatureDef);
    return static_cast<const ExternalSignatureBlobEntry *>(pEntry);
}

bool        ExternalMethodBlobEntry::IsEqual(const ProfilingBlobEntry *  other) const
{
    WRAPPER_NO_CONTRACT;

    if (this->kind() != other->kind())
        return false;

    const ExternalMethodBlobEntry *  other2 = static_cast<const ExternalMethodBlobEntry *>(other);

    if (this->nestedClass() != other2->nestedClass())
        return false;

    if (this->signature() != other2->signature())
        return false;

    if (this->cbName() != other2->cbName())
        return false;

    LPCSTR p1 = this->pName();
    LPCSTR p2 = other2->pName();

    for (DWORD i=0; (i < this->cbName()); i++)
        if (p1[i] != p2[i])
            return false;

    return true;
}

size_t     ExternalMethodBlobEntry::Hash() const
{
    WRAPPER_NO_CONTRACT;

    size_t hashValue = HashInit();

    hashValue = HashCombine(hashValue, nestedClass());
    hashValue = HashCombine(hashValue, signature());

    LPCSTR p1 = pName();

    for (DWORD i=0; (i < cbName()); i++)
        hashValue = HashCombine(hashValue, p1[i]);

    return hashValue;
}

ExternalMethodBlobEntry::ExternalMethodBlobEntry(mdToken _nestedClass,
                                                 mdToken _signature,
                                                 LPCSTR  _pName)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(_pName));
    }
    CONTRACTL_END;

    m_token       = idExternalMethodNil;
    m_nestedClass = idExternalTypeNil;
    m_signature   = idExternalSignatureNil;
    m_cbName      = 0;

    DWORD _cbName = (DWORD) strlen(_pName) + 1;
    LPSTR * pName = (LPSTR *) new (nothrow) CHAR[_cbName];
    if (pName != NULL)
        {
        m_nestedClass = _nestedClass;
        m_signature   = _signature;
        m_cbName      = _cbName;
        memcpy(pName, _pName, _cbName);
        m_pName       = (LPSTR) pName;
    }
        }

/* static */ const ExternalMethodBlobEntry *  ExternalMethodBlobEntry::FindOrAdd(
                                                             PTR_Module pModule,
                                                             mdToken    _nestedClass,
                                                             mdToken    _signature,
                                                             LPCSTR     _pName)
{
    CONTRACTL
        {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(_pName));
        }
    CONTRACTL_END;

    if ((_pName == NULL) || (::strlen(_pName) == 0))
        return NULL;

    ExternalMethodBlobEntry sEntry(_nestedClass, _signature, _pName);

    const ProfilingBlobEntry *  pEntry = pModule->GetProfilingBlobTable()->Lookup(&sEntry);
    if (pEntry == NULL)
    {
        //
        // Not Found, add a new external type blob entry
        //
        ExternalMethodBlobEntry *  newEntry;
        newEntry = new (nothrow) ExternalMethodBlobEntry(_nestedClass, _signature, _pName);
        if (newEntry == NULL)
            return NULL;

        newEntry->newToken();                 // Assign a new ibc external method token
        CONTRACT_VIOLATION(ThrowsViolation);
        pModule->GetProfilingBlobTable()->Add(newEntry);
        pEntry = newEntry;
    }

    //
    // Return the external method entry that we found or the new one that we just created
    //
    _ASSERTE(pEntry->kind() == ExternalMethodDef);
    return static_cast<const ExternalMethodBlobEntry *>(pEntry);
}

static bool GetBasename(LPCWSTR _src, __out_ecount(dstlen) __out_z LPWSTR _dst, int dstlen)
{
    LIMITED_METHOD_CONTRACT;
    LPCWSTR src = _src;
    LPWSTR  dst = _dst;

    if ((src == NULL) || (dstlen <= 0))
        return false;

    bool   inQuotes = false;
    LPWSTR dstLast = dst + (dstlen - 1);
    while (dst < dstLast)
    {
        WCHAR wch = *src++;
        if (wch == W('"'))
        {
            inQuotes = !inQuotes;
            continue;
        }

        if (wch == 0)
            break;

        *dst++ = wch;

        if (!inQuotes)
        {
            if ((wch == W('\\')) || (wch == W(':')))
            {
                dst = _dst;
            }
            else if (wch == W(' '))
            {
                dst--;
                break;
            }
        }
    }
    *dst++ = 0;
    return true;
}

static LPCWSTR s_pCommandLine = NULL;

// Retrieve the full command line for the current process.
LPCWSTR GetManagedCommandLine()
{
    LIMITED_METHOD_CONTRACT;
    return s_pCommandLine;
}

LPCWSTR GetCommandLineForDiagnostics()
{
    // Get the managed command line.
    LPCWSTR pCmdLine = GetManagedCommandLine();

    // Checkout https://github.com/dotnet/coreclr/pull/24433 for more information about this fall back.
    if (pCmdLine == nullptr)
    {
        // Use the result from GetCommandLineW() instead
        pCmdLine = GetCommandLineW();
    }

    return pCmdLine;
}

void Append_Next_Item(LPWSTR* ppCursor, SIZE_T* pRemainingLen, LPCWSTR pItem, bool addSpace)
{
    // read the writeback args and setup pCursor and remainingLen
    LPWSTR pCursor      = *ppCursor;
    SIZE_T remainingLen = *pRemainingLen;

    // Calculate the length of pItem
    SIZE_T itemLen = wcslen(pItem);

    // Append pItem at pCursor
    wcscpy_s(pCursor, remainingLen, pItem);
    pCursor      += itemLen;
    remainingLen -= itemLen;

    // Also append a space after pItem, if requested
    if (addSpace)
    {
        // Append a space at pCursor
        wcscpy_s(pCursor, remainingLen, W(" "));
        pCursor      += 1;
        remainingLen -= 1;
    }

    // writeback and update ppCursor and pRemainingLen
    *ppCursor      = pCursor;
    *pRemainingLen = remainingLen;
}

void SaveManagedCommandLine(LPCWSTR pwzAssemblyPath, int argc, LPCWSTR *argv)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Get the command line.
    LPCWSTR osCommandLine = GetCommandLineW();

#ifndef TARGET_UNIX
    // On Windows, osCommandLine contains the executable and all arguments.
    s_pCommandLine = osCommandLine;
#else
    // On UNIX, the PAL doesn't have the command line arguments, so we must build the command line.
    // osCommandLine contains the full path to the executable.
    SIZE_T  commandLineLen = (wcslen(osCommandLine) + 1);

    // We will append pwzAssemblyPath to the 'corerun' osCommandLine
    commandLineLen += (wcslen(pwzAssemblyPath) + 1);

    for (int i = 0; i < argc; i++)
    {
        commandLineLen += (wcslen(argv[i]) + 1);
    }
    commandLineLen++;  // Add 1 for the null-termination

    // Allocate a new string for the command line.
    LPWSTR pNewCommandLine = new WCHAR[commandLineLen];
    SIZE_T remainingLen    = commandLineLen;
    LPWSTR pCursor         = pNewCommandLine;

    Append_Next_Item(&pCursor, &remainingLen, osCommandLine,   true);
    Append_Next_Item(&pCursor, &remainingLen, pwzAssemblyPath, (argc > 0));

    for (int i = 0; i < argc; i++)
    {
        bool moreArgs = (i < (argc-1));
        Append_Next_Item(&pCursor, &remainingLen, argv[i], moreArgs);
    }

    s_pCommandLine = pNewCommandLine;
#endif
}

static void ProfileDataAllocateScenarioInfo(ProfileEmitter * pEmitter, LPCSTR scopeName, GUID* pMvid)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    ProfileMap *profileMap = pEmitter->EmitNewSection(ScenarioInfo);

    //
    // Allocate and initialize the scenario info section
    //
    {
        CORBBTPROF_SCENARIO_INFO_SECTION_HEADER *siHeader;
        siHeader = (CORBBTPROF_SCENARIO_INFO_SECTION_HEADER *) profileMap->Allocate(sizeof(CORBBTPROF_SCENARIO_INFO_SECTION_HEADER));

        siHeader->NumScenarios = 1;
        siHeader->TotalNumRuns = 1;
    }

    //
    // Allocate and initialize the scenario header section
    //
    {
        // Get the managed command line.
        LPCWSTR pCmdLine = GetCommandLineForDiagnostics();

        S_SIZE_T cCmdLine = S_SIZE_T(wcslen(pCmdLine));
        cCmdLine += 1;
        if (cCmdLine.IsOverflow())
        {
            ThrowHR(COR_E_OVERFLOW);
        }

        LPCWSTR  pSystemInfo = W("<machine,OS>");
        S_SIZE_T cSystemInfo = S_SIZE_T(wcslen(pSystemInfo));
        cSystemInfo += 1;
        if (cSystemInfo.IsOverflow())
        {
            ThrowHR(COR_E_OVERFLOW);
        }

        FILETIME runTime, unused1, unused2, unused3;
        GetProcessTimes(GetCurrentProcess(), &runTime, &unused1, &unused2, &unused3);

        WCHAR    scenarioName[256];
        GetBasename(pCmdLine, &scenarioName[0], 256);

        LPCWSTR  pName      = &scenarioName[0];
        S_SIZE_T cName      = S_SIZE_T(wcslen(pName));
        cName += 1;
        if (cName.IsOverflow())
        {
            ThrowHR(COR_E_OVERFLOW);
        }

        S_SIZE_T sizeHeader = S_SIZE_T(sizeof(CORBBTPROF_SCENARIO_HEADER));
        sizeHeader += cName * S_SIZE_T(sizeof(WCHAR));
        if (sizeHeader.IsOverflow())
        {
            ThrowHR(COR_E_OVERFLOW);
        }

        S_SIZE_T sizeRun    = S_SIZE_T(sizeof(CORBBTPROF_SCENARIO_RUN));
        sizeRun += cCmdLine * S_SIZE_T(sizeof(WCHAR));
        sizeRun += cSystemInfo * S_SIZE_T(sizeof(WCHAR));
        if (sizeRun.IsOverflow())
        {
            ThrowHR(COR_E_OVERFLOW);
        }

        //
        // Allocate the Scenario Header struct
        //
        SIZE_T sHeaderOffset;
        {
            CORBBTPROF_SCENARIO_HEADER *sHeader;
            S_SIZE_T sHeaderSize = sizeHeader + sizeRun;
            if (sHeaderSize.IsOverflow())
            {
                ThrowHR(COR_E_OVERFLOW);
            }

            sHeaderOffset = profileMap->getCurrentOffset();
            sHeader = (CORBBTPROF_SCENARIO_HEADER *) profileMap->Allocate(sizeHeader.Value());

            sHeader->size              = sHeaderSize.Value();
            sHeader->scenario.ordinal  = 1;
            sHeader->scenario.mask     = 1;
            sHeader->scenario.priority = 0;
            sHeader->scenario.numRuns  = 1;
            sHeader->scenario.cName    = cName.Value();
            wcscpy_s(sHeader->scenario.name, cName.Value(), pName);
        }

        //
        // Allocate the Scenario Run struct
        //
        {
            CORBBTPROF_SCENARIO_RUN *sRun;
            sRun = (CORBBTPROF_SCENARIO_RUN *)  profileMap->Allocate(sizeRun.Value());

            sRun->runTime     = runTime;
            sRun->mvid        = *pMvid;
            sRun->cCmdLine    = cCmdLine.Value();
            sRun->cSystemInfo = cSystemInfo.Value();
            wcscpy_s(sRun->cmdLine, cCmdLine.Value(), pCmdLine);
            wcscpy_s(sRun->cmdLine+cCmdLine.Value(), cSystemInfo.Value(), pSystemInfo);
        }
#ifdef _DEBUG
        {
            CORBBTPROF_SCENARIO_HEADER * sHeader;
            sHeader = (CORBBTPROF_SCENARIO_HEADER *) profileMap->getOffsetPtr(sHeaderOffset);
            assert(sHeader->size == sHeader->Size());
        }
#endif
    }
}

static void ProfileDataAllocateMethodBlockCounts(ProfileEmitter * pEmitter, CORCOMPILE_METHOD_PROFILE_LIST * pMethodProfileListHead)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    ProfileMap *profileMap = pEmitter->EmitNewSection(MethodBlockCounts);

    //
    // Allocate and initialize the method block count section
    //
    SIZE_T mbcHeaderOffset;
    {
        CORBBTPROF_METHOD_BLOCK_COUNTS_SECTION_HEADER *mbcHeader;
        mbcHeaderOffset = profileMap->getCurrentOffset();
        mbcHeader = (CORBBTPROF_METHOD_BLOCK_COUNTS_SECTION_HEADER *)
            profileMap->Allocate(sizeof(CORBBTPROF_METHOD_BLOCK_COUNTS_SECTION_HEADER));
        mbcHeader->NumMethods = 0;  // This gets filled in later
    }

    ULONG numMethods = 0;   // We count the number of methods that were executed

    for (CORCOMPILE_METHOD_PROFILE_LIST * methodProfileList = pMethodProfileListHead;
         methodProfileList;
         methodProfileList = methodProfileList->next)
    {
        CORBBTPROF_METHOD_HEADER * pInfo = methodProfileList->GetInfo();

        assert(pInfo->size == pInfo->Size());

        //
        // We set methodWasExecuted based upon the ExecutionCount of the very first block
        //
        bool methodWasExecuted = (pInfo->method.block[0].ExecutionCount > 0);

        //
        // If the method was not executed then we don't need to output this methods block counts
        //
        SIZE_T methodHeaderOffset;
        if (methodWasExecuted)
        {
            DWORD profileDataSize = pInfo->size;
            methodHeaderOffset = profileMap->getCurrentOffset();
            CORBBTPROF_METHOD_HEADER *methodHeader = (CORBBTPROF_METHOD_HEADER *) profileMap->Allocate(profileDataSize);
            memcpy(methodHeader, pInfo, profileDataSize);
            numMethods++;
        }

        // Reset all of the basic block counts to zero
        for (UINT32 i=0; (i <  pInfo->method.cBlock); i++ )
        {
            //
            // If methodWasExecuted is false then every block's ExecutionCount should also be zero
            //
            _ASSERTE(methodWasExecuted  || (pInfo->method.block[i].ExecutionCount == 0));

            pInfo->method.block[i].ExecutionCount = 0;
        }
    }

    {
        CORBBTPROF_METHOD_BLOCK_COUNTS_SECTION_HEADER *mbcHeader;
        // We have to refetch the mbcHeader as calls to Allocate will resize and thus move the mbcHeader
        mbcHeader = (CORBBTPROF_METHOD_BLOCK_COUNTS_SECTION_HEADER *) profileMap->getOffsetPtr(mbcHeaderOffset);
        mbcHeader->NumMethods = numMethods;
    }
}

/*static*/ void Module::ProfileDataAllocateTokenLists(ProfileEmitter * pEmitter, Module::TokenProfileData* pTokenProfileData)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    //
    // Allocate and initialize the token list sections
    //
    if (pTokenProfileData)
    {
        for (int format = 0; format < (int)SectionFormatCount; format++)
        {
            CQuickArray<CORBBTPROF_TOKEN_INFO> *pTokenArray = &(pTokenProfileData->m_formats[format].tokenArray);

            if (pTokenArray->Size() != 0)
            {
                ProfileMap *  profileMap = pEmitter->EmitNewSection((SectionFormat) format);

                CORBBTPROF_TOKEN_LIST_SECTION_HEADER *header;
                header = (CORBBTPROF_TOKEN_LIST_SECTION_HEADER *)
                    profileMap->Allocate(sizeof(CORBBTPROF_TOKEN_LIST_SECTION_HEADER) +
                                         pTokenArray->Size() * sizeof(CORBBTPROF_TOKEN_INFO));

                header->NumTokens = pTokenArray->Size();
                memcpy( (header + 1), &((*pTokenArray)[0]), pTokenArray->Size() * sizeof(CORBBTPROF_TOKEN_INFO));

                // Reset the collected tokens
                for (unsigned i = 0; i < CORBBTPROF_TOKEN_MAX_NUM_FLAGS; i++)
                {
                    pTokenProfileData->m_formats[format].tokenBitmaps[i].Reset();
                }
                pTokenProfileData->m_formats[format].tokenArray.ReSizeNoThrow(0);
            }
        }
    }
}

static void ProfileDataAllocateTokenDefinitions(ProfileEmitter * pEmitter, Module * pModule)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    //
    // Allocate and initialize the ibc token definition section (aka the Blob stream)
    //
    ProfileMap *  profileMap = pEmitter->EmitNewSection(BlobStream);

    // Compute the size of the metadata section:
    // It is the sum of all of the Metadata Profile pool entries
    //  plus the sum of all of the Param signature entries
    //
    size_t totalSize = 0;

    for (ProfilingBlobTable::Iterator cur = pModule->GetProfilingBlobTable()->Begin(),
                                      end = pModule->GetProfilingBlobTable()->End();
         (cur != end);
         cur++)
    {
        const ProfilingBlobEntry * pEntry = *cur;
        size_t blobElementSize = pEntry->varSize();
        switch (pEntry->kind()) {
        case ParamTypeSpec:
        case ParamMethodSpec:
            blobElementSize += sizeof(CORBBTPROF_BLOB_PARAM_SIG_ENTRY);
            break;

        case ExternalNamespaceDef:
            blobElementSize += sizeof(CORBBTPROF_BLOB_NAMESPACE_DEF_ENTRY);
            break;

        case ExternalTypeDef:
            blobElementSize += sizeof(CORBBTPROF_BLOB_TYPE_DEF_ENTRY);
            break;

        case ExternalSignatureDef:
            blobElementSize += sizeof(CORBBTPROF_BLOB_SIGNATURE_DEF_ENTRY);
            break;

        case ExternalMethodDef:
            blobElementSize += sizeof(CORBBTPROF_BLOB_METHOD_DEF_ENTRY);
            break;

        default:
            _ASSERTE(!"Unexpected blob type");
            break;
        }
        totalSize += blobElementSize;
    }

    profileMap->Allocate(totalSize);

    size_t currentPos = 0;

    // Traverse each element and record it
    size_t blobElementSize = 0;
    for (ProfilingBlobTable::Iterator cur = pModule->GetProfilingBlobTable()->Begin(),
                                      end = pModule->GetProfilingBlobTable()->End();
         (cur != end);
         cur++, currentPos += blobElementSize)
    {
        const ProfilingBlobEntry * pEntry = *cur;
        blobElementSize = pEntry->varSize();
        void *profileData = profileMap->getOffsetPtr(currentPos);

        switch (pEntry->kind()) {
        case ParamTypeSpec:
        {
            CORBBTPROF_BLOB_PARAM_SIG_ENTRY *  bProfileData      = (CORBBTPROF_BLOB_PARAM_SIG_ENTRY*) profileData;
            const TypeSpecBlobEntry *          typeSpecBlobEntry = static_cast<const TypeSpecBlobEntry *>(pEntry);

            blobElementSize         += sizeof(CORBBTPROF_BLOB_PARAM_SIG_ENTRY);
            bProfileData->blob.size  = static_cast<DWORD>(blobElementSize);
            bProfileData->blob.type  = typeSpecBlobEntry->kind();
            bProfileData->blob.token = typeSpecBlobEntry->token();
            _ASSERTE(typeSpecBlobEntry->cbSig() > 0);
            bProfileData->cSig       = typeSpecBlobEntry->cbSig();
            memcpy(&bProfileData->sig[0], typeSpecBlobEntry->pSig(), typeSpecBlobEntry->cbSig());
            break;
        }

        case ParamMethodSpec:
        {
            CORBBTPROF_BLOB_PARAM_SIG_ENTRY *  bProfileData        = (CORBBTPROF_BLOB_PARAM_SIG_ENTRY*) profileData;
            const MethodSpecBlobEntry *        methodSpecBlobEntry = static_cast<const MethodSpecBlobEntry *>(pEntry);

            blobElementSize         += sizeof(CORBBTPROF_BLOB_PARAM_SIG_ENTRY);
            bProfileData->blob.size  = static_cast<DWORD>(blobElementSize);
            bProfileData->blob.type  = methodSpecBlobEntry->kind();
            bProfileData->blob.token = methodSpecBlobEntry->token();
            _ASSERTE(methodSpecBlobEntry->cbSig() > 0);
            bProfileData->cSig       = methodSpecBlobEntry->cbSig();
            memcpy(&bProfileData->sig[0], methodSpecBlobEntry->pSig(), methodSpecBlobEntry->cbSig());
            break;
        }

        case ExternalNamespaceDef:
        {
            CORBBTPROF_BLOB_NAMESPACE_DEF_ENTRY *  bProfileData        = (CORBBTPROF_BLOB_NAMESPACE_DEF_ENTRY*) profileData;
            const ExternalNamespaceBlobEntry *     namespaceBlobEntry  = static_cast<const ExternalNamespaceBlobEntry *>(pEntry);

            blobElementSize         += sizeof(CORBBTPROF_BLOB_NAMESPACE_DEF_ENTRY);
            bProfileData->blob.size  = static_cast<DWORD>(blobElementSize);
            bProfileData->blob.type  = namespaceBlobEntry->kind();
            bProfileData->blob.token = namespaceBlobEntry->token();
            _ASSERTE(namespaceBlobEntry->cbName() > 0);
            bProfileData->cName      = namespaceBlobEntry->cbName();
            memcpy(&bProfileData->name[0], namespaceBlobEntry->pName(), namespaceBlobEntry->cbName());
            break;
        }

        case ExternalTypeDef:
        {
            CORBBTPROF_BLOB_TYPE_DEF_ENTRY *       bProfileData        = (CORBBTPROF_BLOB_TYPE_DEF_ENTRY*) profileData;
            const ExternalTypeBlobEntry *          typeBlobEntry       = static_cast<const ExternalTypeBlobEntry *>(pEntry);

            blobElementSize         += sizeof(CORBBTPROF_BLOB_TYPE_DEF_ENTRY);
            bProfileData->blob.size  = static_cast<DWORD>(blobElementSize);
            bProfileData->blob.type  = typeBlobEntry->kind();
            bProfileData->blob.token = typeBlobEntry->token();
            bProfileData->assemblyRefToken = typeBlobEntry->assemblyRef();
            bProfileData->nestedClassToken = typeBlobEntry->nestedClass();
            bProfileData->nameSpaceToken   = typeBlobEntry->nameSpace();
            _ASSERTE(typeBlobEntry->cbName() > 0);
            bProfileData->cName            = typeBlobEntry->cbName();
            memcpy(&bProfileData->name[0], typeBlobEntry->pName(), typeBlobEntry->cbName());
            break;
        }

        case ExternalSignatureDef:
        {
            CORBBTPROF_BLOB_SIGNATURE_DEF_ENTRY *  bProfileData        = (CORBBTPROF_BLOB_SIGNATURE_DEF_ENTRY*) profileData;
            const ExternalSignatureBlobEntry *     signatureBlobEntry  = static_cast<const ExternalSignatureBlobEntry *>(pEntry);

            blobElementSize         += sizeof(CORBBTPROF_BLOB_SIGNATURE_DEF_ENTRY);
            bProfileData->blob.size  = static_cast<DWORD>(blobElementSize);
            bProfileData->blob.type  = signatureBlobEntry->kind();
            bProfileData->blob.token = signatureBlobEntry->token();
            _ASSERTE(signatureBlobEntry->cbSig() > 0);
            bProfileData->cSig       = signatureBlobEntry->cbSig();
            memcpy(&bProfileData->sig[0], signatureBlobEntry->pSig(), signatureBlobEntry->cbSig());
            break;
        }

        case ExternalMethodDef:
        {
            CORBBTPROF_BLOB_METHOD_DEF_ENTRY *     bProfileData        = (CORBBTPROF_BLOB_METHOD_DEF_ENTRY*) profileData;
            const ExternalMethodBlobEntry *        methodBlobEntry     = static_cast<const ExternalMethodBlobEntry *>(pEntry);

            blobElementSize         += sizeof(CORBBTPROF_BLOB_METHOD_DEF_ENTRY);
            bProfileData->blob.size  = static_cast<DWORD>(blobElementSize);
            bProfileData->blob.type  = methodBlobEntry->kind();
            bProfileData->blob.token = methodBlobEntry->token();
            bProfileData->nestedClassToken = methodBlobEntry->nestedClass();
            bProfileData->signatureToken   = methodBlobEntry->signature();
            _ASSERTE(methodBlobEntry->cbName() > 0);
            bProfileData->cName            = methodBlobEntry->cbName();
            memcpy(&bProfileData->name[0], methodBlobEntry->pName(), methodBlobEntry->cbName());
            break;
        }

        default:
            _ASSERTE(!"Unexpected blob type");
            break;
        }
    }

    _ASSERTE(currentPos == totalSize);

    // Emit a terminating entry with type EndOfBlobStream to mark the end
    DWORD mdElementSize = sizeof(CORBBTPROF_BLOB_ENTRY);
    void *profileData = profileMap->Allocate(mdElementSize);
    memset(profileData, 0, mdElementSize);

    CORBBTPROF_BLOB_ENTRY* mdProfileData = (CORBBTPROF_BLOB_ENTRY*) profileData;
    mdProfileData->type = EndOfBlobStream;
    mdProfileData->size = sizeof(CORBBTPROF_BLOB_ENTRY);
}

// Responsible for writing out the profile data if the COMPlus_BBInstr
// environment variable is set.  This is called when the module is unloaded
// (usually at shutdown).
HRESULT Module::WriteMethodProfileDataLogFile(bool cleanup)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    if (IsResource())
        return S_OK;

    EX_TRY
    {
        if (GetAssembly()->IsInstrumented() && (m_pProfilingBlobTable != NULL) && (m_tokenProfileData != NULL))
        {
            NewHolder<ProfileEmitter> pEmitter(new ProfileEmitter());

            // Get this ahead of time - metadata access may be logged, which will
            // take the m_tokenProfileData->crst, which we take a couple lines below
            LPCSTR pszName;
            GUID mvid;
            IfFailThrow(GetMDImport()->GetScopeProps(&pszName, &mvid));

            CrstHolder ch(&m_tokenProfileData->crst);

            //
            // Create the scenario info section
            //
            ProfileDataAllocateScenarioInfo(pEmitter, pszName, &mvid);

            //
            // Create the method block count section
            //
            ProfileDataAllocateMethodBlockCounts(pEmitter, m_methodProfileList);

            //
            // Create the token list sections
            //
            ProfileDataAllocateTokenLists(pEmitter, m_tokenProfileData);

            //
            // Create the ibc token definition section (aka the Blob stream)
            //
            ProfileDataAllocateTokenDefinitions(pEmitter, this);

            //
            // Now store the profile data in the ibc file
            //
            ProfileMap profileImage;
            pEmitter->Serialize(&profileImage, mvid);

            HandleHolder profileDataFile(OpenMethodProfileDataLogFile(mvid));

            ULONG count;
            BOOL result = WriteFile(profileDataFile, profileImage.getOffsetPtr(0), profileImage.getCurrentOffset(), &count, NULL);
            if (!result || (count != profileImage.getCurrentOffset()))
            {
                DWORD lasterror = GetLastError();
                _ASSERTE(!"Error writing ibc profile data to file");
                hr = HRESULT_FROM_WIN32(lasterror);
            }
        }

        if (cleanup)
        {
            DeleteProfilingData();
        }
    }
    EX_CATCH
    {
        hr = E_FAIL;
    }
    EX_END_CATCH(SwallowAllExceptions)

    return hr;
}


/* static */
void Module::WriteAllModuleProfileData(bool cleanup)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Iterate over all the app domains; for each one iterator over its
    // assemblies; for each one iterate over its modules.
    EX_TRY
    {
        AppDomainIterator appDomainIterator(FALSE);
        while(appDomainIterator.Next())
        {
            AppDomain * appDomain = appDomainIterator.GetDomain();
            AppDomain::AssemblyIterator assemblyIterator = appDomain->IterateAssembliesEx(
                (AssemblyIterationFlags)(kIncludeLoaded | kIncludeExecution));
            CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;

            while (assemblyIterator.Next(pDomainAssembly.This()))
            {
                DomainModuleIterator i = pDomainAssembly->IterateModules(kModIterIncludeLoaded);
                while (i.Next())
                {
                    /*hr=*/i.GetModule()->WriteMethodProfileDataLogFile(cleanup);
                }
            }
        }
    }
    EX_CATCH
    { }
    EX_END_CATCH(SwallowAllExceptions);
}

PTR_ProfilingBlobTable Module::GetProfilingBlobTable()
{
    LIMITED_METHOD_CONTRACT;
    return m_pProfilingBlobTable;
}

void Module::CreateProfilingData()
{
    TokenProfileData *tpd = TokenProfileData::CreateNoThrow();

    PVOID pv = InterlockedCompareExchangeT(&m_tokenProfileData, tpd, NULL);
    if (pv != NULL)
    {
        delete tpd;
    }

    PTR_ProfilingBlobTable ppbt = new (nothrow) ProfilingBlobTable();

    if (ppbt != NULL)
    {
        pv = InterlockedCompareExchangeT(&m_pProfilingBlobTable, ppbt, NULL);
        if (pv != NULL)
        {
            delete ppbt;
        }
    }
}

void Module::DeleteProfilingData()
{
    if (m_pProfilingBlobTable != NULL)
    {
        for (ProfilingBlobTable::Iterator cur = m_pProfilingBlobTable->Begin(),
                                          end = m_pProfilingBlobTable->End();
             (cur != end);
             cur++)
        {
            const ProfilingBlobEntry *  pCurrentEntry = *cur;
            delete pCurrentEntry;
        }
        delete m_pProfilingBlobTable;
        m_pProfilingBlobTable = NULL;
    }

    if (m_tokenProfileData != NULL)
    {
        delete m_tokenProfileData;
        m_tokenProfileData = NULL;
    }

    // the metadataProfileData is free'ed in destructor of the corresponding MetaDataTracker
}

void Module::SetIsIJWFixedUp()
{
    LIMITED_METHOD_CONTRACT;
    FastInterlockOr(&m_dwTransientFlags, IS_IJW_FIXED_UP);
}

/* static */
Module::TokenProfileData *Module::TokenProfileData::CreateNoThrow(void)
{
    STATIC_CONTRACT_NOTHROW;

    TokenProfileData *tpd = NULL;

    EX_TRY
    {
        //
        // This constructor calls crst.Init(), which may throw.  So putting (nothrow) doesn't
        // do what we would want it to.  Thus I wrap it here in a TRY/CATCH and revert to NULL
        // if it fails.
        //
        tpd = new TokenProfileData();
    }
    EX_CATCH
    {
        tpd = NULL;
    }
    EX_END_CATCH(SwallowAllExceptions);

    return tpd;
}

#endif // !DACCESS_COMPILE

#ifndef DACCESS_COMPILE
void Module::SetBeingUnloaded()
{
    LIMITED_METHOD_CONTRACT;
    FastInterlockOr((ULONG*)&m_dwTransientFlags, IS_BEING_UNLOADED);
}
#endif

void Module::LogTokenAccess(mdToken token, SectionFormat format, ULONG flagnum)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(g_IBCLogger.InstrEnabled());
        PRECONDITION(flagnum < CORBBTPROF_TOKEN_MAX_NUM_FLAGS);
    }
    CONTRACTL_END;

#ifndef DACCESS_COMPILE

    //
    // If we are in ngen instrumentation mode, then we should record this token.
    //

    if (!m_nativeImageProfiling)
        return;

    if (flagnum >= CORBBTPROF_TOKEN_MAX_NUM_FLAGS)
    {
        return;
    }

    mdToken rid = RidFromToken(token);
    CorTokenType  tkType  = (CorTokenType) TypeFromToken(token);
    SectionFormat tkKind  = (SectionFormat) (tkType >> 24);

    if ((rid == 0) && (tkKind < (SectionFormat) TBL_COUNT))
        return;

    FAULT_NOT_FATAL();

    _ASSERTE(TypeProfilingData   == FirstTokenFlagSection + TBL_TypeDef);
    _ASSERTE(MethodProfilingData == FirstTokenFlagSection + TBL_Method);
    _ASSERTE(SectionFormatCount  >= FirstTokenFlagSection + TBL_COUNT + 4);

    if (!m_tokenProfileData)
    {
        CreateProfilingData();
    }

    if (!m_tokenProfileData)
    {
        return;
    }

    if (tkKind == (SectionFormat) (ibcTypeSpec >> 24))
        tkKind = IbcTypeSpecSection;
    else if (tkKind == (SectionFormat) (ibcMethodSpec >> 24))
        tkKind = IbcMethodSpecSection;

    _ASSERTE(tkKind >= 0);
    _ASSERTE(tkKind < SectionFormatCount);
    if (tkKind < 0 || tkKind >= SectionFormatCount)
    {
        return;
    }

    CQuickArray<CORBBTPROF_TOKEN_INFO> * pTokenArray  = &m_tokenProfileData->m_formats[format].tokenArray;
    RidBitmap *                          pTokenBitmap = &m_tokenProfileData->m_formats[tkKind].tokenBitmaps[flagnum];

    // Have we seen this token with this flag already?
    if (pTokenBitmap->IsTokenInBitmap(token))
    {
        return;
    }

    // Insert the token to the bitmap
    if (FAILED(pTokenBitmap->InsertToken(token)))
    {
        return;
    }

    ULONG flag = 1 << flagnum;

    // [ToDo] Fix: this is a sequential search and can be very slow
    for (unsigned int i = 0; i < pTokenArray->Size(); i++)
    {
        if ((*pTokenArray)[i].token == token)
        {
            _ASSERTE(! ((*pTokenArray)[i].flags & flag));
            (*pTokenArray)[i].flags |= flag;
            return;
        }
    }

    if (FAILED(pTokenArray->ReSizeNoThrow(pTokenArray->Size() + 1)))
    {
        return;
    }

    (*pTokenArray)[pTokenArray->Size() - 1].token = token;
    (*pTokenArray)[pTokenArray->Size() - 1].flags = flag;
    (*pTokenArray)[pTokenArray->Size() - 1].scenarios = 0;

#endif // !DACCESS_COMPILE
}

void Module::LogTokenAccess(mdToken token, ULONG flagNum)
{
    WRAPPER_NO_CONTRACT;
    SectionFormat format = (SectionFormat)((TypeFromToken(token)>>24) + FirstTokenFlagSection);
    if (FirstTokenFlagSection <= format && format < SectionFormatCount)
    {
        LogTokenAccess(token, format, flagNum);
    }
}

#ifndef DACCESS_COMPILE

//
// Encoding callbacks
//

/*static*/ DWORD Module::EncodeModuleHelper(void * pModuleContext, Module *pReferencedModule)
{
    Module* pReferencingModule = (Module *) pModuleContext;
    _ASSERTE(pReferencingModule != pReferencedModule);

    Assembly *pReferencingAssembly = pReferencingModule->GetAssembly();
    Assembly *pReferencedAssembly  = pReferencedModule->GetAssembly();

    _ASSERTE(pReferencingAssembly != pReferencedAssembly);

    if (pReferencedAssembly == pReferencingAssembly)
    {
        return 0;
    }

    mdAssemblyRef token = pReferencingModule->FindAssemblyRef(pReferencedAssembly);

    if (IsNilToken(token))
    {
        return ENCODE_MODULE_FAILED;
    }

    return RidFromToken(token);
}

/*static*/ void Module::TokenDefinitionHelper(void* pModuleContext, Module *pReferencedModule, DWORD index, mdToken* pToken)
{
    LIMITED_METHOD_CONTRACT;
    HRESULT              hr;
    Module *             pReferencingModule = (Module *) pModuleContext;
    mdAssemblyRef        mdAssemblyRef      = TokenFromRid(index, mdtAssemblyRef);
    IMDInternalImport *  pImport            = pReferencedModule->GetMDImport();
    LPCUTF8              szName             = NULL;

    if (TypeFromToken(*pToken) == mdtTypeDef)
    {
        //
        // Compute nested type (if any)
        //
        mdTypeDef mdEnclosingType = idExternalTypeNil;
        hr = pImport->GetNestedClassProps(*pToken, &mdEnclosingType);
        // If there's not enclosing type, then hr=CLDB_E_RECORD_NOTFOUND and mdEnclosingType is unchanged
        _ASSERTE((hr == S_OK) || (hr == CLDB_E_RECORD_NOTFOUND));

        if (!IsNilToken(mdEnclosingType))
        {
            _ASSERT(TypeFromToken(mdEnclosingType) ==  mdtTypeDef);
            TokenDefinitionHelper(pModuleContext, pReferencedModule, index, &mdEnclosingType);
        }
        _ASSERT(TypeFromToken(mdEnclosingType) == ibcExternalType);

        //
        // Compute type name and namespace.
        //
        LPCUTF8 szNamespace = NULL;
        hr = pImport->GetNameOfTypeDef(*pToken, &szName, &szNamespace);
        _ASSERTE(hr == S_OK);

        //
        // Transform namespace string into ibc external namespace token
        //
        idExternalNamespace idNamespace = idExternalNamespaceNil;
        if (szNamespace != NULL)
        {
            const ExternalNamespaceBlobEntry *  pNamespaceEntry;
            pNamespaceEntry = ExternalNamespaceBlobEntry::FindOrAdd(pReferencingModule, szNamespace);
            if (pNamespaceEntry != NULL)
            {
                idNamespace = pNamespaceEntry->token();
            }
        }
        _ASSERTE(TypeFromToken(idNamespace) == ibcExternalNamespace);

        //
        // Transform type name into ibc external type token
        //
        idExternalType idType = idExternalTypeNil;
        _ASSERTE(szName != NULL);
        const ExternalTypeBlobEntry *  pTypeEntry = NULL;
        pTypeEntry = ExternalTypeBlobEntry::FindOrAdd(pReferencingModule,
                                                      mdAssemblyRef,
                                                      mdEnclosingType,
                                                      idNamespace,
                                                      szName);
        if (pTypeEntry != NULL)
        {
            idType = pTypeEntry->token();
        }
        _ASSERTE(TypeFromToken(idType) == ibcExternalType);

        *pToken = idType;   // Remap pToken to our idExternalType token
    }
    else if (TypeFromToken(*pToken) == mdtMethodDef)
    {
        //
        // Compute nested type (if any)
        //
        mdTypeDef mdEnclosingType = idExternalTypeNil;
        hr = pImport->GetParentToken(*pToken, &mdEnclosingType);
        _ASSERTE(!FAILED(hr));

        if (!IsNilToken(mdEnclosingType))
        {
            _ASSERT(TypeFromToken(mdEnclosingType) ==  mdtTypeDef);
            TokenDefinitionHelper(pModuleContext, pReferencedModule, index, &mdEnclosingType);
        }
        _ASSERT(TypeFromToken(mdEnclosingType) == ibcExternalType);

        //
        // Compute the method name and signature
        //
        PCCOR_SIGNATURE pSig = NULL;
        DWORD           cbSig = 0;
        hr = pImport->GetNameAndSigOfMethodDef(*pToken, &pSig, &cbSig, &szName);
        _ASSERTE(hr == S_OK);

        //
        // Transform signature into ibc external signature token
        //
        idExternalSignature idSignature = idExternalSignatureNil;
        if (pSig != NULL)
        {
            const ExternalSignatureBlobEntry *  pSignatureEntry;
            pSignatureEntry = ExternalSignatureBlobEntry::FindOrAdd(pReferencingModule, cbSig, pSig);
            if (pSignatureEntry != NULL)
            {
                idSignature = pSignatureEntry->token();
            }
        }
        _ASSERTE(TypeFromToken(idSignature) == ibcExternalSignature);

        //
        // Transform method name into ibc external method token
        //
        idExternalMethod idMethod = idExternalMethodNil;
        _ASSERTE(szName != NULL);
        const ExternalMethodBlobEntry *  pMethodEntry = NULL;
        pMethodEntry = ExternalMethodBlobEntry::FindOrAdd(pReferencingModule,
                                                          mdEnclosingType,
                                                          idSignature,
                                                          szName);
        if (pMethodEntry != NULL)
        {
            idMethod = pMethodEntry->token();
        }
        _ASSERTE(TypeFromToken(idMethod) == ibcExternalMethod);

        *pToken = idMethod;   // Remap pToken to our idMethodSpec token
    }
    else
    {
        _ASSERTE(!"Unexpected token type");
    }
}

idTypeSpec Module::LogInstantiatedType(TypeHandle typeHnd, ULONG flagNum)
{
    CONTRACT(idTypeSpec)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(g_IBCLogger.InstrEnabled());
        PRECONDITION(!typeHnd.HasUnrestoredTypeKey());
        // We want to report the type only in its own loader module as a type's
        // MethodTable can only live in its own loader module.
        // We can relax this if we allow a (duplicate) MethodTable to live
        // in any module (which might be needed for ngen of generics)
#ifdef FEATURE_PREJIT
        // All callsites already do this...
        // PRECONDITION(this == GetPreferredZapModuleForTypeHandle(typeHnd));
#endif
    }
    CONTRACT_END;

    idTypeSpec result = idTypeSpecNil;

    if (m_nativeImageProfiling)
    {
        CONTRACT_VIOLATION(ThrowsViolation|FaultViolation|GCViolation);

        SigBuilder sigBuilder;

        ZapSig zapSig(this, this, ZapSig::IbcTokens,
                        Module::EncodeModuleHelper, Module::TokenDefinitionHelper);
        BOOL fSuccess = zapSig.GetSignatureForTypeHandle(typeHnd, &sigBuilder);

        // a return value of 0 indicates a failure to create the signature
        if (fSuccess)
        {
            DWORD cbSig;
            PCCOR_SIGNATURE pSig = (PCCOR_SIGNATURE)sigBuilder.GetSignature(&cbSig);

            ULONG flag = (1 << flagNum);
            TypeSpecBlobEntry *  pEntry = const_cast<TypeSpecBlobEntry *>(TypeSpecBlobEntry::FindOrAdd(this, cbSig, pSig));
            if (pEntry != NULL)
            {
                // Update the flags with any new bits
                pEntry->orFlag(flag);
                result = pEntry->token();
            }
        }
    }
    _ASSERTE(TypeFromToken(result) == ibcTypeSpec);

    RETURN result;
}

idMethodSpec Module::LogInstantiatedMethod(const MethodDesc * md, ULONG flagNum)
{
    CONTRACT(idMethodSpec)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION( md != NULL );
    }
    CONTRACT_END;

    idMethodSpec result = idMethodSpecNil;

    if (m_nativeImageProfiling)
    {
        CONTRACT_VIOLATION(ThrowsViolation|FaultViolation|GCViolation);

        if (!m_tokenProfileData)
        {
            CreateProfilingData();
        }

        if (!m_tokenProfileData)
        {
            return idMethodSpecNil;
        }

        // get data
        SigBuilder sigBuilder;

        BOOL fSuccess;
        fSuccess = ZapSig::EncodeMethod(const_cast<MethodDesc *>(md), this, &sigBuilder,
                                      (LPVOID) this,
                       (ENCODEMODULE_CALLBACK) Module::EncodeModuleHelper,
                        (DEFINETOKEN_CALLBACK) Module::TokenDefinitionHelper);

        if (fSuccess)
        {
            DWORD dataSize;
            BYTE * pBlob = (BYTE *)sigBuilder.GetSignature(&dataSize);

            ULONG flag = (1 << flagNum);
            MethodSpecBlobEntry *  pEntry = const_cast<MethodSpecBlobEntry *>(MethodSpecBlobEntry::FindOrAdd(this, dataSize, pBlob));
            if (pEntry != NULL)
            {
                // Update the flags with any new bits
                pEntry->orFlag(flag);
                result = pEntry->token();
            }
        }
    }

    _ASSERTE(TypeFromToken(result) == ibcMethodSpec);
    RETURN result;
}
#endif // DACCESS_COMPILE

#ifndef DACCESS_COMPILE

#ifndef CROSSGEN_COMPILE
// ===========================================================================
// ReflectionModule
// ===========================================================================

/* static */
ReflectionModule *ReflectionModule::Create(Assembly *pAssembly, PEFile *pFile, AllocMemTracker *pamTracker, LPCWSTR szName, BOOL fIsTransient)
{
    CONTRACT(ReflectionModule *)
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pAssembly));
        PRECONDITION(CheckPointer(pFile));
        PRECONDITION(pFile->IsDynamic());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    // Hoist CONTRACT into separate routine because of EX incompatibility

    mdFile token;
    _ASSERTE(pFile->IsAssembly());
    token = mdFileNil;

    // Initial memory block for Modules must be zero-initialized (to make it harder
    // to introduce Destruct crashes arising from OOM's during initialization.)

    void* pMemory = pamTracker->Track(pAssembly->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(ReflectionModule))));
    ReflectionModuleHolder pModule(new (pMemory) ReflectionModule(pAssembly, token, pFile));

    pModule->DoInit(pamTracker, szName);

    // Set this at module creation time. The m_fIsTransient field should never change during the lifetime of this ReflectionModule.
    pModule->SetIsTransient(fIsTransient ? true : false);

    RETURN pModule.Extract();
}


// Module initialization occurs in two phases: the constructor phase and the Initialize phase.
//
// The constructor phase initializes just enough so that Destruct() can be safely called.
// It cannot throw or fail.
//
ReflectionModule::ReflectionModule(Assembly *pAssembly, mdFile token, PEFile *pFile)
  : Module(pAssembly, token, pFile)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        FORBID_FAULT;
    }
    CONTRACTL_END

    m_pInMemoryWriter = NULL;
    m_sdataSection = NULL;
    m_pISymUnmanagedWriter = NULL;
    m_pCreatingAssembly = NULL;
    m_pCeeFileGen = NULL;
    m_pDynamicMetadata = NULL;
    m_fSuppressMetadataCapture = false;
    m_fIsTransient = false;
}

HRESULT STDMETHODCALLTYPE CreateICeeGen(REFIID riid, void **pCeeGen);

// Module initialization occurs in two phases: the constructor phase and the Initialize phase.
//
// The Initialize() phase completes the initialization after the constructor has run.
// It can throw exceptions but whether it throws or succeeds, it must leave the Module
// in a state where Destruct() can be safely called.
//
void ReflectionModule::Initialize(AllocMemTracker *pamTracker, LPCWSTR szName)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        STANDARD_VM_CHECK;
        PRECONDITION(szName != NULL);
    }
    CONTRACTL_END;

    Module::Initialize(pamTracker);

    IfFailThrow(CreateICeeGen(IID_ICeeGenInternal, (void **)&m_pCeeFileGen));

    // Collectible modules should try to limit the growth of their associate IL section, as common scenarios for collectible
    // modules include single type modules
    if (IsCollectible())
    {
        ReleaseHolder<ICeeGenInternal> pCeeGenInternal(NULL);
        IfFailThrow(m_pCeeFileGen->QueryInterface(IID_ICeeGenInternal, (void **)&pCeeGenInternal));
        IfFailThrow(pCeeGenInternal->SetInitialGrowth(CEE_FILE_GEN_GROWTH_COLLECTIBLE));
    }

    m_pInMemoryWriter = new RefClassWriter();

    IfFailThrow(m_pInMemoryWriter->Init(GetCeeGen(), GetEmitter(), szName));

    m_CrstLeafLock.Init(CrstLeafLock);
}

void ReflectionModule::Destruct()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    delete m_pInMemoryWriter;

    if (m_pISymUnmanagedWriter)
    {
        m_pISymUnmanagedWriter->Close();
        m_pISymUnmanagedWriter->Release();
        m_pISymUnmanagedWriter = NULL;
    }

    if (m_pCeeFileGen)
        m_pCeeFileGen->Release();

    Module::Destruct();

    delete m_pDynamicMetadata;
    m_pDynamicMetadata = NULL;

    m_CrstLeafLock.Destroy();
}

// Returns true iff metadata capturing is suppressed.
//
// Notes:
//   This is during the window after code:ReflectionModule.SuppressMetadataCapture and before
//   code:ReflectionModule.ResumeMetadataCapture.
//
//   If metadata updates are suppressed, then class-load notifications should be suppressed too.
bool ReflectionModule::IsMetadataCaptureSuppressed()
{
    return m_fSuppressMetadataCapture;
}
//
// Holder of changed value of MDUpdateMode via IMDInternalEmit::SetMDUpdateMode.
// Returns back the original value on release.
//
class MDUpdateModeHolder
{
public:
    MDUpdateModeHolder()
    {
        m_pInternalEmitter = NULL;
        m_OriginalMDUpdateMode = UINT32_MAX;
    }
    ~MDUpdateModeHolder()
    {
        WRAPPER_NO_CONTRACT;
        (void)Release();
    }
    HRESULT SetMDUpdateMode(IMetaDataEmit *pEmitter, ULONG updateMode)
    {
        LIMITED_METHOD_CONTRACT;
        HRESULT hr = S_OK;

        _ASSERTE(updateMode != UINT32_MAX);

        IfFailRet(pEmitter->QueryInterface(IID_IMDInternalEmit, (void **)&m_pInternalEmitter));
        _ASSERTE(m_pInternalEmitter != NULL);

        IfFailRet(m_pInternalEmitter->SetMDUpdateMode(updateMode, &m_OriginalMDUpdateMode));
        _ASSERTE(m_OriginalMDUpdateMode != UINT32_MAX);

        return hr;
    }
    HRESULT Release(ULONG expectedPreviousUpdateMode = UINT32_MAX)
    {
        HRESULT hr = S_OK;

        if (m_OriginalMDUpdateMode != UINT32_MAX)
        {
            _ASSERTE(m_pInternalEmitter != NULL);
            ULONG previousUpdateMode;
            // Ignore the error when releasing
            hr = m_pInternalEmitter->SetMDUpdateMode(m_OriginalMDUpdateMode, &previousUpdateMode);
            m_OriginalMDUpdateMode = UINT32_MAX;

            if (expectedPreviousUpdateMode != UINT32_MAX)
            {
                if ((hr == S_OK) && (expectedPreviousUpdateMode != previousUpdateMode))
                {
                    hr = S_FALSE;
                }
            }
        }
        if (m_pInternalEmitter != NULL)
        {
            (void)m_pInternalEmitter->Release();
            m_pInternalEmitter = NULL;
        }
        return hr;
    }
    ULONG GetOriginalMDUpdateMode()
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(m_OriginalMDUpdateMode != UINT32_MAX);
        return m_OriginalMDUpdateMode;
    }
private:
    IMDInternalEmit *m_pInternalEmitter;
    ULONG            m_OriginalMDUpdateMode;
};

// Called in live paths to fetch metadata for dynamic modules. This makes the metadata available to the
// debugger from out-of-process.
//
// Notes:
//    This buffer can be retrieved by the debugger via code:ReflectionModule.GetDynamicMetadataBuffer
//
//    Threading:
//    - Callers must ensure nobody else is adding to the metadata.
//    - This function still takes its own locks to cooperate with the Debugger's out-of-process access.
//      The debugger can slip this thread outside the locks to ensure the data is consistent.
//
//    This does not raise a debug notification to invalidate the metadata. Reasoning is that this only
//    happens in two cases:
//    1) manifest module is updated with the name of a new dynamic module.
//    2) on each class load, in which case we already send a debug event. In this case, we already send a
//    class-load notification, so sending a separate "metadata-refresh" would make the eventing twice as
//    chatty. Class-load events are high-volume and events are slow.
//    Thus we can avoid the chatiness by ensuring the debugger knows that Class-load also means "refresh
//    metadata".
//
void ReflectionModule::CaptureModuleMetaDataToMemory()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    // If we've suppresed metadata capture, then skip this. We'll recapture when we enable it. This allows
    // for batching up capture.
    // If a debugger is attached, then the CLR will still send ClassLoad notifications for dynamic modules,
    // which mean we still need to keep the metadata available. This is the same as Whidbey.
    // An alternative (and better) design would be to suppress ClassLoad notifications too, but then we'd
    // need some way of sending a "catchup" notification to the debugger after we re-enable notifications.
    if (IsMetadataCaptureSuppressed() && !CORDebuggerAttached())
    {
        return;
    }

    // Do not release the emitter. This is a weak reference.
    IMetaDataEmit *pEmitter = this->GetEmitter();
    _ASSERTE(pEmitter != NULL);

    HRESULT hr;

    MDUpdateModeHolder hMDUpdateMode;
    IfFailThrow(hMDUpdateMode.SetMDUpdateMode(pEmitter, MDUpdateExtension));
    _ASSERTE(hMDUpdateMode.GetOriginalMDUpdateMode() == MDUpdateFull);

    DWORD numBytes;
    hr = pEmitter->GetSaveSize(cssQuick, &numBytes);
    IfFailThrow(hr);

    // Operate on local data, and then persist it into the module once we know it's valid.
    NewHolder<SBuffer> pBuffer(new SBuffer());
    _ASSERTE(pBuffer != NULL); // allocation would throw first

    // ReflectionModule is still in a consistent state, and now we're just operating on local data to
    // assemble the new metadata buffer. If this fails, then worst case is that metadata does not include
    // recently generated classes.

    // Caller ensures serialization that guarantees that the metadata doesn't grow underneath us.
    BYTE * pRawData = pBuffer->OpenRawBuffer(numBytes);
    hr = pEmitter->SaveToMemory(pRawData, numBytes);
    pBuffer->CloseRawBuffer();

    IfFailThrow(hr);

    // Now that we're successful, transfer ownership back into the module.
    {
        CrstHolder ch(&m_CrstLeafLock);

        delete m_pDynamicMetadata;

        m_pDynamicMetadata = pBuffer.Extract();
    }

    //

    hr = hMDUpdateMode.Release(MDUpdateExtension);
    // Will be S_FALSE if someone changed the MDUpdateMode (from MDUpdateExtension) meanwhile
    _ASSERTE(hr == S_OK);
}

// Suppress the eager metadata serialization.
//
// Notes:
//    This casues code:ReflectionModule.CaptureModuleMetaDataToMemory to be a nop.
//    This is not nestable.
//    This exists purely for performance reasons.
//
//    Don't call this directly. Use a SuppressMetadataCaptureHolder holder to ensure it's
//    balanced with code:ReflectionModule.ResumeMetadataCapture
//
//    Types generating while eager metadata-capture is suppressed should not actually be executed until
//    after metadata capture is restored.
void ReflectionModule::SuppressMetadataCapture()
{
    LIMITED_METHOD_CONTRACT;
    // If this fires, then you probably missed a call to ResumeMetadataCapture.
    CONSISTENCY_CHECK_MSG(!m_fSuppressMetadataCapture, "SuppressMetadataCapture is not nestable");
    m_fSuppressMetadataCapture = true;
}

// Resumes eager metadata serialization.
//
// Notes:
//    This casues code:ReflectionModule.CaptureModuleMetaDataToMemory to resume eagerly serializing metadata.
//    This must be called after code:ReflectionModule.SuppressMetadataCapture.
//
void ReflectionModule::ResumeMetadataCapture()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(m_fSuppressMetadataCapture);
    m_fSuppressMetadataCapture = false;

    CaptureModuleMetaDataToMemory();
}

#endif // !CROSSGEN_COMPILE

#endif // !DACCESS_COMPILE

#ifdef DACCESS_COMPILE
// Accessor to expose m_pDynamicMetadata to debugger.
//
// Returns:
//    Pointer to SBuffer  containing metadata buffer. May be null.
//
// Notes:
//    Only used by the debugger, so only accessible via DAC.
//    The buffer is updated via code:ReflectionModule.CaptureModuleMetaDataToMemory
PTR_SBuffer ReflectionModule::GetDynamicMetadataBuffer() const
{
    SUPPORTS_DAC;

    // If we ask for metadata, but have been suppressing capture, then we're out of date.
    // However, the debugger may be debugging already baked types in the module and so may need the metadata
    // for that. So we return what we do have.
    //
    // Debugger will get the next metadata update:
    // 1) with the next load class
    // 2) or if this is right after the last class, see code:ReflectionModule.CaptureModuleMetaDataToMemory

    return m_pDynamicMetadata;
}
#endif

TADDR ReflectionModule::GetIL(RVA il) // virtual
{
#ifndef DACCESS_COMPILE
    WRAPPER_NO_CONTRACT;

    BYTE* pByte = NULL;
    m_pCeeFileGen->GetMethodBuffer(il, &pByte);
    return TADDR(pByte);
#else // DACCESS_COMPILE
    SUPPORTS_DAC;
    DacNotImpl();
    return NULL;
#endif // DACCESS_COMPILE
}

PTR_VOID ReflectionModule::GetRvaField(RVA field, BOOL fZapped) // virtual
{
    _ASSERTE(!fZapped);
#ifndef DACCESS_COMPILE
    WRAPPER_NO_CONTRACT;
    // This function should be call only if the target is a field or a field with RVA.
    PTR_BYTE pByte = NULL;
    m_pCeeFileGen->ComputePointer(m_sdataSection, field, &pByte);
    return dac_cast<PTR_VOID>(pByte);
#else // DACCESS_COMPILE
    SUPPORTS_DAC;
    DacNotImpl();
    return NULL;
#endif // DACCESS_COMPILE
}

#ifndef DACCESS_COMPILE

// ===========================================================================
// VASigCookies
// ===========================================================================

//==========================================================================
// Enregisters a VASig.
//==========================================================================
VASigCookie *Module::GetVASigCookie(Signature vaSignature)
{
    CONTRACT(VASigCookie*)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACT_END;

    VASigCookieBlock *pBlock;
    VASigCookie      *pCookie;

    pCookie = NULL;

    // First, see if we already enregistered this sig.
    // Note that we're outside the lock here, so be a bit careful with our logic
    for (pBlock = m_pVASigCookieBlock; pBlock != NULL; pBlock = pBlock->m_Next)
    {
        for (UINT i = 0; i < pBlock->m_numcookies; i++)
        {
            if (pBlock->m_cookies[i].signature.GetRawSig() == vaSignature.GetRawSig())
            {
                pCookie = &(pBlock->m_cookies[i]);
                break;
            }
        }
    }

    if (!pCookie)
    {
        // If not, time to make a new one.

        // Compute the size of args first, outside of the lock.

        // @TODO GENERICS: We may be calling a varargs method from a
        // generic type/method. Using an empty context will make such a
        // case cause an unexpected exception. To make this work,
        // we need to create a specialized signature for every instantiation
        SigTypeContext typeContext;

        MetaSig metasig(vaSignature, this, &typeContext);
        ArgIterator argit(&metasig);

        // Upper estimate of the vararg size
        DWORD sizeOfArgs = argit.SizeOfArgStack();

        // enable gc before taking lock
        {
            CrstHolder ch(&m_Crst);

            // Note that we were possibly racing to create the cookie, and another thread
            // may have already created it.  We could put another check
            // here, but it's probably not worth the effort, so we'll just take an
            // occasional duplicate cookie instead.

            // Is the first block in the list full?
            if (m_pVASigCookieBlock && m_pVASigCookieBlock->m_numcookies
                < VASigCookieBlock::kVASigCookieBlockSize)
            {
                // Nope, reserve a new slot in the existing block.
                pCookie = &(m_pVASigCookieBlock->m_cookies[m_pVASigCookieBlock->m_numcookies]);
            }
            else
            {
                // Yes, create a new block.
                VASigCookieBlock *pNewBlock = new VASigCookieBlock();

                pNewBlock->m_Next = m_pVASigCookieBlock;
                pNewBlock->m_numcookies = 0;
                m_pVASigCookieBlock = pNewBlock;
                pCookie = &(pNewBlock->m_cookies[0]);
            }

            // Now, fill in the new cookie (assuming we had enough memory to create one.)
            pCookie->pModule = this;
            pCookie->pNDirectILStub = NULL;
            pCookie->sizeOfArgs = sizeOfArgs;
            pCookie->signature = vaSignature;

            // Finally, now that it's safe for asynchronous readers to see it,
            // update the count.
            m_pVASigCookieBlock->m_numcookies++;
        }
    }

    RETURN pCookie;
}

// ===========================================================================
// LookupMap
// ===========================================================================
#ifdef FEATURE_NATIVE_IMAGE_GENERATION

int __cdecl LookupMapBase::HotItem::Cmp(const void* a_, const void* b_)
{
    LIMITED_METHOD_CONTRACT;
    const HotItem *a = (const HotItem *)a_;
    const HotItem *b = (const HotItem *)b_;

    if (a->rid < b->rid)
        return -1;
    else if (a->rid > b->rid)
        return 1;
    else
        return 0;
}

void LookupMapBase::CreateHotItemList(DataImage *image, CorProfileData *profileData, int table, BOOL fSkipNullEntries /*= FALSE*/)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(!MapIsCompressed());

    if (profileData)
    {
        DWORD numInTokenList = profileData->GetHotTokens(table, 1<<RidMap, 1<<RidMap, NULL, 0);

        if (numInTokenList > 0)
        {
            HotItem *itemList = (HotItem*)(void*)image->GetModule()->GetLoaderAllocator()->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(HotItem)) * S_SIZE_T(numInTokenList));
            mdToken *tokenList = (mdToken*)(void*)image->GetModule()->GetLoaderAllocator()->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(mdToken)) * S_SIZE_T(numInTokenList));

            profileData->GetHotTokens(table, 1<<RidMap, 1<<RidMap, tokenList, numInTokenList);
            DWORD numItems = 0;
            for (DWORD i = 0; i < numInTokenList; i++)
            {
                DWORD rid = RidFromToken(tokenList[i]);
                TADDR value = RelativePointer<TADDR>::GetValueMaybeNullAtPtr(dac_cast<TADDR>(GetElementPtr(RidFromToken(tokenList[i]))));
                if (!fSkipNullEntries || value != NULL)
                {
                    itemList[numItems].rid = rid;
                    itemList[numItems].value = value;
                    ++numItems;
                }
            }

            if (numItems > 0)
            {
                qsort(itemList,          // start of array
                      numItems,          // array size in elements
                      sizeof(HotItem),      // element size in bytes
                      HotItem::Cmp);        // comparer function

                // Eliminate any duplicates in the list. Due to the qsort, they must be adjacent now.
                // We do this by walking the array and copying entries that are not duplicates of the previous one.
                // We can start the loop at +1, because 0 is not a duplicate of the previous entry, and does not
                // need to be copied either.
                DWORD j = 1;
                for (DWORD i = 1; i < numItems; i++)
                {
                    if (itemList[i].rid != itemList[i-1].rid)
                    {
                        itemList[j].rid   = itemList[i].rid;
                        itemList[j].value = itemList[i].value;
                        j++;
                    }
                }
                _ASSERTE(j <= numItems);
                numItems = j;

                // We have treated the values as normal TADDRs to let qsort move them around freely.
                // Fix them up to be the relative pointers now.
                for (DWORD ii = 0; ii < numItems; ii++)
                {
                    if (itemList[ii].value != NULL)
                    {
                        RelativePointer<TADDR> *pRelPtr = (RelativePointer<TADDR> *)&itemList[ii].value;
                        pRelPtr->SetValueMaybeNull(itemList[ii].value);
                    }
                }

                if (itemList != NULL)
                    image->StoreStructure(itemList, sizeof(HotItem)*numItems,
                                          DataImage::ITEM_RID_MAP_HOT);

                hotItemList = itemList;
                dwNumHotItems = numItems;
            }
        }
    }
}

void LookupMapBase::Save(DataImage *image, DataImage::ItemKind kind, CorProfileData *profileData, int table, BOOL fCopyValues /*= FALSE*/)
{
    STANDARD_VM_CONTRACT;

    // the table index that comes in is a token mask, the upper 8 bits are the table type for the tokens, that's all we want
    table >>= 24;

    dwNumHotItems = 0;
    hotItemList = NULL;

    if (table != 0)
    {
        // Because we use the same IBC encoding to record a touch to the m_GenericTypeDefToCanonMethodTableMap as
        // to the m_TypeDefToMethodTableMap, the hot items we get in both will be the union of the touches.  This limitation
        // in the IBC infrastructure does not hurt us much because touching an entry for a generic type in one map often if
        // not always implies touching the corresponding entry in the other.  But when saving the GENERICTYPEDEF_MAP it
        // does mean that we need to be prepared to see "hot" items whose data is NULL in this map (specifically, the non-
        // generic types).  We don't want the hot list to be unnecessarily big with these entries, so tell CreateHotItemList to
        // skip them.
        BOOL fSkipNullEntries = (kind == DataImage::ITEM_GENERICTYPEDEF_MAP);
        CreateHotItemList(image, profileData, table, fSkipNullEntries);
    }

    // Determine whether we want to compress this lookup map (to improve density of cold pages in the map on
    // hot item cache misses). We only enable this optimization for the TypeDefToMethodTable, the
    // GenericTypeDefToCanonMethodTable, and the MethodDefToDesc maps since (a) they're the largest and
    // as a result reap the most space savings and (b) these maps are fully populated in an ngen image and immutable
    // at runtime, something that's important when dealing with a compressed version of the table.
    if (kind == DataImage::ITEM_TYPEDEF_MAP || kind == DataImage::ITEM_GENERICTYPEDEF_MAP || kind == DataImage::ITEM_METHODDEF_MAP)
    {
        // The bulk of the compression work is done in the later stages of ngen image generation (since it
        // relies on knowing the final RVAs of each value stored in the table). So we create a specialzed
        // ZapNode that knows how to perform the compression for us.
        image->StoreCompressedLayoutMap(this, DataImage::ITEM_COMPRESSED_MAP);

        // We need to know we decided to compress during the Fixup stage but the table kind is not available
        // there. So we use the cIndexEntryBits field as a flag (this will be initialized to zero and is only
        // set to a meaningful value near the end of ngen image generation, during the compression of the
        // table itself).
        cIndexEntryBits = 1;

        // The ZapNode we allocated above takes care of all the rest of the processing for this map, so we're
        // done here.
        return;
    }

    SaveUncompressedMap(image, kind, fCopyValues);
}

void LookupMapBase::SaveUncompressedMap(DataImage *image, DataImage::ItemKind kind, BOOL fCopyValues /*= FALSE*/)
{
    STANDARD_VM_CONTRACT;

    // We should only be calling this once per map
    _ASSERTE(!image->IsStored(pTable));

    //
    // We will only store one (big) node instead of the full list,
    // and make the one node large enough to fit all the RIDs
    //

    ZapStoredStructure * pTableNode = image->StoreStructure(NULL, GetSize() * sizeof(TADDR), kind);

    LookupMapBase *map = this;
    DWORD offsetIntoCombo = 0;
    while (map != NULL)
    {
        DWORD len = map->dwCount * sizeof(void*);

        if (fCopyValues)
            image->CopyDataToOffset(pTableNode, offsetIntoCombo, map->pTable, len);

        image->BindPointer(map->pTable,pTableNode,offsetIntoCombo);
        offsetIntoCombo += len;
        map = map->pNext;
    }
}

void LookupMapBase::ConvertSavedMapToUncompressed(DataImage *image, DataImage::ItemKind kind)
{
    STANDARD_VM_CONTRACT;

    // Check whether we decided to compress this map (see Save() above).
    if (cIndexEntryBits == 0)
        return;

    cIndexEntryBits = 0;
    SaveUncompressedMap(image, kind);
}

void LookupMapBase::Fixup(DataImage *image, BOOL fFixupEntries /*=TRUE*/)
{
    STANDARD_VM_CONTRACT;

    if (hotItemList != NULL)
        image->FixupPointerField(this, offsetof(LookupMapBase, hotItemList));

    // Find the biggest RID supported by the entire list of LookupMaps.
    // We will only store one LookupMap node instead of the full list,
    // and make it big enough to fit all RIDs.
    *(DWORD *)image->GetImagePointer(this, offsetof(LookupMapBase, dwCount)) = GetSize();

    // Persist the supportedFlags that this particular instance was created with.
    *(TADDR *)image->GetImagePointer(this, offsetof(LookupMapBase, supportedFlags)) = supportedFlags;

    image->ZeroPointerField(this, offsetof(LookupMapBase, pNext));

    // Check whether we've decided to compress this map (see Save() above).
    if (cIndexEntryBits == 1)
    {
        // In the compressed case most of the Fixup logic is performed by the specialized ZapNode we allocated
        // during Save(). But we still have to record fixups for any hot items we've cached (these aren't
        // compressed).
        for (DWORD i = 0; i < dwNumHotItems; i++)
        {
            TADDR *pHotValueLoc = &hotItemList[i].value;
            TADDR pHotValue = RelativePointer<TADDR>::GetValueMaybeNullAtPtr((TADDR)pHotValueLoc);
            TADDR flags = pHotValue & supportedFlags;
            pHotValue -= flags;

            if (image->IsStored((PVOID)pHotValue))
            {
                image->FixupField(hotItemList,
                                  (BYTE *)pHotValueLoc - (BYTE *)hotItemList,
                                  (PVOID)pHotValue, flags, IMAGE_REL_BASED_RelativePointer);
            }
            else
            {
                image->ZeroPointerField(hotItemList, (BYTE *)pHotValueLoc - (BYTE *)hotItemList);
            }
        }

        // The ZapNode will handle everything else so we're done.
        return;
    }

    // Note that the caller is responsible for calling FixupPointerField()
    // or zeroing out the contents of pTable as appropriate
    image->FixupPointerField(this, offsetof(LookupMapBase, pTable));

    if (fFixupEntries)
    {
        LookupMap<PVOID>::Iterator iter((LookupMap<PVOID> *)this);
        DWORD rid = 0;

        while (iter.Next())
        {
            TADDR flags;
            PVOID p = iter.GetElementAndFlags(&flags);
            PTR_TADDR hotItemValuePtr = FindHotItemValuePtr(rid);

            if (image->IsStored(p))
            {
                image->FixupField(pTable, rid * sizeof(TADDR),
                    p, flags, IMAGE_REL_BASED_RelativePointer);

                // In case this item is also in the hot item subtable, fix it up there as well
                if (hotItemValuePtr != NULL)
                    image->FixupField(hotItemList,
                        (BYTE *)hotItemValuePtr - (BYTE *)hotItemList,
                        p, flags, IMAGE_REL_BASED_RelativePointer);
            }
            else
            {
                image->ZeroPointerField(pTable, rid * sizeof(TADDR));
                // In case this item is also in the hot item subtable, zero it there as well
                if (hotItemValuePtr != NULL)
                    image->ZeroPointerField(hotItemList,
                        (BYTE *)hotItemValuePtr - (BYTE *)hotItemList);
            }

            rid++;
        }
    }
}
#endif // FEATURE_NATIVE_IMAGE_GENERATION

#endif // !DACCESS_COMPILE

#ifdef DACCESS_COMPILE

void
LookupMapBase::EnumMemoryRegions(CLRDataEnumMemoryFlags flags,
                             bool enumThis)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    if (enumThis)
    {
        DacEnumHostDPtrMem(this);
    }
    if (pTable.IsValid())
    {
#ifdef FEATURE_PREJIT
        if (MapIsCompressed())
        {
            // Compressed maps have tables whose size cannot be calculated cheaply. Plus they have an
            // additional index blob.
            DacEnumMemoryRegion(dac_cast<TADDR>(pTable),
                                cbTable);
            DacEnumMemoryRegion(dac_cast<TADDR>(pIndex),
                                cbIndex);
        }
        else
#endif // FEATURE_PREJIT
            DacEnumMemoryRegion(dac_cast<TADDR>(pTable),
                                dwCount * sizeof(TADDR));
    }
#ifdef FEATURE_PREJIT
    if (dwNumHotItems && hotItemList.IsValid())
    {
        DacEnumMemoryRegion(dac_cast<TADDR>(hotItemList),
                            dwNumHotItems * sizeof(HotItem));
    }
#endif // FEATURE_PREJIT
}


/* static */
void
LookupMapBase::ListEnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    LookupMapBase * headMap = this;
    bool enumHead = false;
    while (headMap)
    {
        headMap->EnumMemoryRegions(flags, enumHead);

        if (!headMap->pNext.IsValid())
        {
            break;
        }

        headMap = headMap->pNext;
        enumHead = true;
    }
}

#endif // DACCESS_COMPILE


// Optimization intended for Module::EnsureActive only
#include <optsmallperfcritical.h>

#ifndef DACCESS_COMPILE
VOID Module::EnsureActive()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    GetDomainFile()->EnsureActive();
}
#endif // DACCESS_COMPILE

#include <optdefault.h>


#ifndef DACCESS_COMPILE

VOID Module::EnsureAllocated()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    GetDomainFile()->EnsureAllocated();
}

VOID Module::EnsureLibraryLoaded()
{
    STANDARD_VM_CONTRACT;
    GetDomainFile()->EnsureLibraryLoaded();
}
#endif // !DACCESS_COMPILE

CHECK Module::CheckActivated()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifndef DACCESS_COMPILE
    DomainFile *pDomainFile = GetDomainFile();
    CHECK(pDomainFile != NULL);
    PREFIX_ASSUME(pDomainFile != NULL);
    CHECK(pDomainFile->CheckActivated());
#endif
    CHECK_OK;
}

#ifdef DACCESS_COMPILE

void
ModuleCtorInfo::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;

    // This class is contained so do not enumerate 'this'.
    DacEnumMemoryRegion(dac_cast<TADDR>(ppMT), numElements *
                        sizeof(RelativePointer<MethodTable *>));
    DacEnumMemoryRegion(dac_cast<TADDR>(cctorInfoHot), numElementsHot *
                        sizeof(ClassCtorInfoEntry));
    DacEnumMemoryRegion(dac_cast<TADDR>(cctorInfoCold),
                        (numElements - numElementsHot) *
                        sizeof(ClassCtorInfoEntry));
    DacEnumMemoryRegion(dac_cast<TADDR>(hotHashOffsets), numHotHashes *
                        sizeof(DWORD));
    DacEnumMemoryRegion(dac_cast<TADDR>(coldHashOffsets), numColdHashes *
                        sizeof(DWORD));
}

void Module::EnumMemoryRegions(CLRDataEnumMemoryFlags flags,
                               bool enumThis)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    if (enumThis)
    {
        DAC_ENUM_VTHIS();
        EMEM_OUT(("MEM: %p Module\n", dac_cast<TADDR>(this)));
    }

    //Save module id data only if it a real pointer, not a tagged sugestion to use ModuleIndex.
    if (m_ModuleID.IsValid())
    {
        m_ModuleID->EnumMemoryRegions(flags);
    }

    if (m_file.IsValid())
    {
        m_file->EnumMemoryRegions(flags);
    }
    if (m_pAssembly.IsValid())
    {
        m_pAssembly->EnumMemoryRegions(flags);
    }

    m_TypeRefToMethodTableMap.ListEnumMemoryRegions(flags);
    m_TypeDefToMethodTableMap.ListEnumMemoryRegions(flags);

    if (flags != CLRDATA_ENUM_MEM_MINI && flags != CLRDATA_ENUM_MEM_TRIAGE)
    {
        if (m_pAvailableClasses.IsValid())
        {
            m_pAvailableClasses->EnumMemoryRegions(flags);
        }
        if (m_pAvailableParamTypes.IsValid())
        {
            m_pAvailableParamTypes->EnumMemoryRegions(flags);
        }
        if (m_pInstMethodHashTable.IsValid())
        {
            m_pInstMethodHashTable->EnumMemoryRegions(flags);
        }
        if (m_pAvailableClassesCaseIns.IsValid())
        {
            m_pAvailableClassesCaseIns->EnumMemoryRegions(flags);
        }
#ifdef FEATURE_PREJIT
        if (m_pStubMethodHashTable.IsValid())
        {
            m_pStubMethodHashTable->EnumMemoryRegions(flags);
        }
#endif // FEATURE_PREJIT
        if (m_pBinder.IsValid())
        {
            m_pBinder->EnumMemoryRegions(flags);
        }
#ifdef FEATURE_PREJIT
        m_ModuleCtorInfo.EnumMemoryRegions(flags);
#endif

        // Save the LookupMap structures.
        m_MethodDefToDescMap.ListEnumMemoryRegions(flags);
        m_FieldDefToDescMap.ListEnumMemoryRegions(flags);
        m_pMemberRefToDescHashTable->EnumMemoryRegions(flags);
        m_GenericParamToDescMap.ListEnumMemoryRegions(flags);
        m_GenericTypeDefToCanonMethodTableMap.ListEnumMemoryRegions(flags);
        m_FileReferencesMap.ListEnumMemoryRegions(flags);
        m_ManifestModuleReferencesMap.ListEnumMemoryRegions(flags);
        m_MethodDefToPropertyInfoMap.ListEnumMemoryRegions(flags);

        LookupMap<PTR_MethodTable>::Iterator typeDefIter(&m_TypeDefToMethodTableMap);
        while (typeDefIter.Next())
        {
            if (typeDefIter.GetElement())
            {
                typeDefIter.GetElement()->EnumMemoryRegions(flags);
            }
        }

        LookupMap<PTR_TypeRef>::Iterator typeRefIter(&m_TypeRefToMethodTableMap);
        while (typeRefIter.Next())
        {
            if (typeRefIter.GetElement())
            {
                TypeHandle th = TypeHandle::FromTAddr(dac_cast<TADDR>(typeRefIter.GetElement()));
                th.EnumMemoryRegions(flags);
            }
        }

        LookupMap<PTR_MethodDesc>::Iterator methodDefIter(&m_MethodDefToDescMap);
        while (methodDefIter.Next())
        {
            if (methodDefIter.GetElement())
            {
                methodDefIter.GetElement()->EnumMemoryRegions(flags);
            }
        }

        LookupMap<PTR_FieldDesc>::Iterator fieldDefIter(&m_FieldDefToDescMap);
        while (fieldDefIter.Next())
        {
            if (fieldDefIter.GetElement())
            {
                fieldDefIter.GetElement()->EnumMemoryRegions(flags);
            }
        }

        LookupMap<PTR_TypeVarTypeDesc>::Iterator genericParamIter(&m_GenericParamToDescMap);
        while (genericParamIter.Next())
        {
            if (genericParamIter.GetElement())
            {
                genericParamIter.GetElement()->EnumMemoryRegions(flags);
            }
        }

        LookupMap<PTR_MethodTable>::Iterator genericTypeDefIter(&m_GenericTypeDefToCanonMethodTableMap);
        while (genericTypeDefIter.Next())
        {
            if (genericTypeDefIter.GetElement())
            {
                genericTypeDefIter.GetElement()->EnumMemoryRegions(flags);
            }
        }

    }   // !CLRDATA_ENUM_MEM_MINI && !CLRDATA_ENUM_MEM_TRIAGE


    LookupMap<PTR_Module>::Iterator fileRefIter(&m_FileReferencesMap);
    while (fileRefIter.Next())
    {
        if (fileRefIter.GetElement())
        {
            fileRefIter.GetElement()->EnumMemoryRegions(flags, true);
        }
    }

    LookupMap<PTR_Module>::Iterator asmRefIter(&m_ManifestModuleReferencesMap);
    while (asmRefIter.Next())
    {
        if (asmRefIter.GetElement())
        {
            asmRefIter.GetElement()->GetAssembly()->EnumMemoryRegions(flags);
        }
    }

    ECall::EnumFCallMethods();
}

FieldDesc *Module::LookupFieldDef(mdFieldDef token)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(TypeFromToken(token) == mdtFieldDef);
    g_IBCLogger.LogRidMapAccess( MakePair( this, token ) );
    return m_FieldDefToDescMap.GetElement(RidFromToken(token));
}

#endif // DACCESS_COMPILE





//-------------------------------------------------------------------------------
// Make best-case effort to obtain an image name for use in an error message.
//
// This routine must expect to be called before the this object is fully loaded.
// It can return an empty if the name isn't available or the object isn't initialized
// enough to get a name, but it mustn't crash.
//-------------------------------------------------------------------------------
LPCWSTR Module::GetPathForErrorMessages()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
    }
    CONTRACTL_END

    PEFile *pFile = GetFile();

    if (pFile)
    {
        return pFile->GetPathForErrorMessages();
    }
    else
    {
        return W("");
    }
}

#if defined(_DEBUG) && !defined(DACCESS_COMPILE) && !defined(CROSS_COMPILE)
void Module::ExpandAll()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    //If the EE isn't started yet, it's not safe to jit.  We fail in COM jitting a p/invoke.
    if (!g_fEEStarted)
        return;
    struct Local
    {
        static void CompileMethodDesc(MethodDesc * pMD)
        {
            //Must have a method body
            if (pMD->HasILHeader()
                //Can't jit open instantiations
                && !pMD->IsGenericMethodDefinition()
                //These are the only methods we can jit
                && (pMD->IsStatic() || pMD->GetNumGenericMethodArgs() == 0
                    || pMD->HasClassInstantiation())
                && (pMD->MayHaveNativeCode() && !pMD->IsFCallOrIntrinsic()))
            {
                pMD->PrepareInitialCode();
            }
        }
        static void CompileMethodsForMethodTable(MethodTable * pMT)
        {
            MethodTable::MethodIterator it(pMT);
            for (; it.IsValid(); it.Next())
            {
                MethodDesc * pMD = it.GetMethodDesc();
                CompileMethodDesc(pMD);
            }
        }
#if 0
        static void CompileMethodsForTypeDef(Module * pModule, mdTypeDef td)
        {
            TypeHandle th = ClassLoader::LoadTypeDefThrowing(pModule, td, ClassLoader::ThrowIfNotFound,
                                                             ClassLoader::PermitUninstDefOrRef);

            MethodTable * pMT = th.GetMethodTable();
            CompileMethodsForMethodTable(pMT);
        }
#endif
        static void CompileMethodsForTypeDefRefSpec(Module * pModule, mdToken tok)
        {
            TypeHandle th;
            HRESULT hr = S_OK;

            EX_TRY
            {
                th = ClassLoader::LoadTypeDefOrRefOrSpecThrowing(
                    pModule,
                    tok,
                    NULL /*SigTypeContext*/);
            }
            EX_CATCH
            {
                hr = GET_EXCEPTION()->GetHR();
            }
            EX_END_CATCH(SwallowAllExceptions);

            //Only do this for non-generic types and unshared generic types
            //(canonical generics and value type generic instantiations).
            if (SUCCEEDED(hr) && !th.IsTypeDesc()
                            && th.AsMethodTable()->IsCanonicalMethodTable())
            {
                CompileMethodsForMethodTable(th.AsMethodTable());
            }
        }
        static void CompileMethodsForMethodDefRefSpec(Module * pModule, mdToken tok)
        {
            HRESULT hr = S_OK;
            EX_TRY
            {
                MethodDesc * pMD =
                    MemberLoader::GetMethodDescFromMemberDefOrRefOrSpec(pModule, tok,
                                                                        /*SigTypeContext*/NULL,
                                                                        TRUE, TRUE);
                CompileMethodDesc(pMD);
            }
            EX_CATCH
            {
                hr = GET_EXCEPTION()->GetHR();
                //@telesto what should we do with this HR?  the Silverlight code doesn't seem
                //to do anything...but that doesn't seem safe...
            }
            EX_END_CATCH(SwallowAllExceptions);
        }
    };
    //Jit all methods eagerly

    IMDInternalImport * pMDI = GetMDImport();
    HENUMTypeDefInternalHolder hEnum(pMDI);
    mdTypeDef td;
    hEnum.EnumTypeDefInit();

    //verify global methods
    if (GetGlobalMethodTable())
    {
        //jit everything in the MT.
        Local::CompileMethodsForTypeDefRefSpec(this, COR_GLOBAL_PARENT_TOKEN);
    }
    while (pMDI->EnumNext(&hEnum, &td))
    {
        //jit everything
        Local::CompileMethodsForTypeDefRefSpec(this, td);
    }

    //Get the type refs.  They're always awesome.
    HENUMInternalHolder hEnumTypeRefs(pMDI);
    mdToken tr;

    hEnumTypeRefs.EnumAllInit(mdtTypeRef);
    while (hEnumTypeRefs.EnumNext(&tr))
    {
        Local::CompileMethodsForTypeDefRefSpec(this, tr);
    }

    //make sure to get the type specs
    HENUMInternalHolder hEnumTypeSpecs(pMDI);
    mdToken ts;

    hEnumTypeSpecs.EnumAllInit(mdtTypeSpec);
    while (hEnumTypeSpecs.EnumNext(&ts))
    {
        Local::CompileMethodsForTypeDefRefSpec(this, ts);
    }


    //And now for the interesting generic methods
    HENUMInternalHolder hEnumMethodSpecs(pMDI);
    mdToken ms;

    hEnumMethodSpecs.EnumAllInit(mdtMethodSpec);
    while (hEnumMethodSpecs.EnumNext(&ms))
    {
        Local::CompileMethodsForMethodDefRefSpec(this, ms);
    }
}
#endif //_DEBUG && !DACCESS_COMPILE && !CROSS_COMPILE

//-------------------------------------------------------------------------------

// Verify consistency of asmconstants.h

// Wrap all C_ASSERT's in asmconstants.h with a class definition.  Many of the
// fields referenced below are private, and this class is a friend of the
// enclosing type.  (A C_ASSERT isn't a compiler intrinsic, just a magic
// typedef that produces a compiler error when the condition is false.)
#include "clrvarargs.h" /* for VARARG C_ASSERTs in asmconstants.h */
class CheckAsmOffsets
{
#ifndef CROSSBITNESS_COMPILE
#define ASMCONSTANTS_C_ASSERT(cond) static_assert(cond, #cond);
#include "asmconstants.h"
#endif // CROSSBITNESS_COMPILE
};

//-------------------------------------------------------------------------------

#ifndef DACCESS_COMPILE

void Module::CreateAssemblyRefByNameTable(AllocMemTracker *pamTracker)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    LoaderHeap *        pHeap       = GetLoaderAllocator()->GetLowFrequencyHeap();
    IMDInternalImport * pImport     = GetMDImport();

    DWORD               dwMaxRid    = pImport->GetCountWithTokenKind(mdtAssemblyRef);
    if (dwMaxRid == 0)
        return;

    S_SIZE_T            dwAllocSize = S_SIZE_T(sizeof(LPWSTR)) * S_SIZE_T(dwMaxRid);
    m_AssemblyRefByNameTable = (LPCSTR *) pamTracker->Track( pHeap->AllocMem(dwAllocSize) );

    DWORD dwCount = 0;
    for (DWORD rid=1; rid <= dwMaxRid; rid++)
    {
        mdAssemblyRef mdToken = TokenFromRid(rid,mdtAssemblyRef);
        LPCSTR        szName;
        HRESULT       hr;

        hr = pImport->GetAssemblyRefProps(mdToken, NULL, NULL, &szName, NULL, NULL, NULL, NULL);

        if (SUCCEEDED(hr))
        {
            m_AssemblyRefByNameTable[dwCount++] = szName;
        }
    }
    m_AssemblyRefByNameCount = dwCount;
}

bool Module::HasReferenceByName(LPCUTF8 pModuleName)
{
    LIMITED_METHOD_CONTRACT;

    for (DWORD i=0; i < m_AssemblyRefByNameCount; i++)
    {
        if (0 == strcmp(pModuleName, m_AssemblyRefByNameTable[i]))
            return true;
    }

    return false;
}
#endif

#ifdef _MSC_VER
#pragma warning(pop)
#endif // _MSC_VER: warning C4244

#if defined(_DEBUG) && !defined(DACCESS_COMPILE)
NOINLINE void NgenForceFailure_AV()
{
    LIMITED_METHOD_CONTRACT;
    static int* alwaysNull = 0;
    *alwaysNull = 0;
}

NOINLINE void NgenForceFailure_TypeLoadException()
{
    WRAPPER_NO_CONTRACT;
    ::ThrowTypeLoadException("ForceIBC", "Failure", W("Assembly"), NULL, IDS_CLASSLOAD_BADFORMAT);
}

void EEConfig::DebugCheckAndForceIBCFailure(BitForMask bitForMask)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    static DWORD s_ibcCheckCount = 0;

    // Both of these must be set to non-zero values for us to force a failure
    //
    if ((NgenForceFailureCount() == 0) || (NgenForceFailureKind() == 0))
        return;

    // The bitForMask value must also beset in the FailureMask
    //
    if ((((DWORD) bitForMask) & NgenForceFailureMask()) == 0)
        return;

    s_ibcCheckCount++;
    if (s_ibcCheckCount < NgenForceFailureCount())
        return;

    // We force one failure every NgenForceFailureCount()
    //
    s_ibcCheckCount = 0;
    switch (NgenForceFailureKind())
    {
    case 1:
        NgenForceFailure_TypeLoadException();
        break;
    case 2:
        NgenForceFailure_AV();
        break;
    }
}
#endif // defined(_DEBUG) && !defined(DACCESS_COMPILE)
