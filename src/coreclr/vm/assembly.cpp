// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Header: Assembly.cpp
**
**
** Purpose: Implements assembly (loader domain) architecture
**
**
===========================================================*/

#include "common.h"

#include <stdlib.h>

#include "assembly.hpp"
#include "appdomain.hpp"

#include "eeprofinterfaces.h"
#include "reflectclasswriter.h"
#include "comdynamic.h"

#include "sha1.h"

#include "eeconfig.h"

#include "assemblynative.hpp"
#include "threadsuspend.h"

#include "appdomainnative.hpp"
#include "customattribute.h"

#include "caparser.h"
#include "../md/compiler/custattr.h"

#include "peimagelayout.inl"


// Define these macro's to do strict validation for jit lock and class init entry leaks.
// This defines determine if the asserts that verify for these leaks are defined or not.
// These asserts can sometimes go off even if no entries have been leaked so this defines
// should be used with caution.
//
// If we are inside a .cctor when the application shut's down then the class init lock's
// head will be set and this will cause the assert to go off.,
//
// If we are jitting a method when the application shut's down then the jit lock's head
// will be set causing the assert to go off.

//#define STRICT_JITLOCK_ENTRY_LEAK_DETECTION
//#define STRICT_CLSINITLOCK_ENTRY_LEAK_DETECTION

LPCWSTR s_wszDiagnosticStartupHookPaths = nullptr;

#ifndef DACCESS_COMPILE

volatile uint32_t g_cAssemblies = 0;
static CrstStatic g_friendAssembliesCrst;

namespace
{
    void DefineEmitScope(GUID iid, void** ppEmit)
    {
        CONTRACT_VOID
        {
            PRECONDITION(CheckPointer(ppEmit));
            POSTCONDITION(CheckPointer(*ppEmit));
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
            INJECT_FAULT(COMPlusThrowOM(););
        }
        CONTRACT_END;

        SafeComHolder<IMetaDataDispenserEx> pDispenser;

        // Get the Dispenser interface.
        MetaDataGetDispenser(
            CLSID_CorMetaDataDispenser,
            IID_IMetaDataDispenserEx,
            (void**)&pDispenser);
        if (pDispenser == NULL)
        {
            ThrowOutOfMemory();
        }

        // Set the option on the dispenser turn on duplicate check for TypeDef and moduleRef
        VARIANT varOption;
        V_VT(&varOption) = VT_UI4;
        V_I4(&varOption) = MDDupDefault | MDDupTypeDef | MDDupModuleRef | MDDupExportedType | MDDupAssemblyRef | MDDupPermission | MDDupFile;
        IfFailThrow(pDispenser->SetOption(MetaDataCheckDuplicatesFor, &varOption));

        // Set minimal MetaData size
        V_VT(&varOption) = VT_UI4;
        V_I4(&varOption) = MDInitialSizeMinimal;
        IfFailThrow(pDispenser->SetOption(MetaDataInitialSize, &varOption));

        // turn on the thread safety!
        V_I4(&varOption) = MDThreadSafetyOn;
        IfFailThrow(pDispenser->SetOption(MetaDataThreadSafetyOptions, &varOption));

        IfFailThrow(pDispenser->DefineScope(CLSID_CorMetaDataRuntime, 0, iid, (IUnknown**)ppEmit));

        RETURN;
    }
}

void Assembly::Initialize()
{
    g_friendAssembliesCrst.Init(CrstLeafLock);
}

//----------------------------------------------------------------------------------------------
// The ctor's job is to initialize the Assembly enough so that the dtor can safely run.
// It cannot do any allocations or operations that might fail. Those operations should be done
// in Assembly::Init()
//----------------------------------------------------------------------------------------------
Assembly::Assembly(AppDomain *pDomain, PEAssembly* pPEAssembly, DebuggerAssemblyControlFlags debuggerFlags, BOOL fIsCollectible) :
    m_pClassLoader(NULL),
    m_pEntryPoint(NULL),
    m_pModule(NULL),
    m_pPEAssembly(clr::SafeAddRef(pPEAssembly)),
    m_pFriendAssemblyDescriptor(NULL),
    m_isDynamic(false),
#ifdef FEATURE_COLLECTIBLE_TYPES
    m_isCollectible(fIsCollectible),
#endif
    m_pLoaderAllocator(NULL),
#ifdef FEATURE_COMINTEROP
    m_pITypeLib(NULL),
#endif // FEATURE_COMINTEROP
#ifdef FEATURE_COMINTEROP
    m_InteropAttributeStatus(INTEROP_ATTRIBUTE_UNSET),
#endif
    m_debuggerFlags(debuggerFlags),
    m_fTerminated(FALSE)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(pDomain == AppDomain::GetCurrentDomain());
}

// This name needs to stay in sync with AssemblyBuilder.ManifestModuleName
// which is used in AssemblyBuilder.InitManifestModule
#define REFEMIT_MANIFEST_MODULE_NAME W("RefEmit_InMemoryManifestModule")


//----------------------------------------------------------------------------------------------
// Does most Assembly initialization tasks. It can assume the ctor has already run
// and the assembly is safely destructable. Whether this function throws or succeeds,
// it must leave the Assembly in a safely destructable state.
//----------------------------------------------------------------------------------------------
void Assembly::Init(AllocMemTracker *pamTracker, LoaderAllocator *pLoaderAllocator)
{
    STANDARD_VM_CONTRACT;

    if (IsSystem())
    {
        _ASSERTE(pLoaderAllocator == NULL); // pLoaderAllocator may only be non-null for collectible types
        m_pLoaderAllocator = SystemDomain::GetGlobalLoaderAllocator();
    }
    else
    {
        if (!IsCollectible())
        {
            // pLoaderAllocator will only be non-null for reflection emit assemblies
            _ASSERTE((pLoaderAllocator == NULL) || (pLoaderAllocator == AppDomain::GetCurrentDomain()->GetLoaderAllocator()));
            m_pLoaderAllocator = AppDomain::GetCurrentDomain()->GetLoaderAllocator();
        }
        else
        {
            _ASSERTE(pLoaderAllocator != NULL); // ppLoaderAllocator must be non-null for collectible assemblies

            m_pLoaderAllocator = pLoaderAllocator;
        }
    }
    _ASSERTE(m_pLoaderAllocator != NULL);

    m_pClassLoader = new ClassLoader(this);
    m_pClassLoader->Init(pamTracker);

    PEAssembly* pPEAssembly = GetPEAssembly();

    // "Module::Create" will initialize R2R support, if there is an R2R header.
    // make sure the PE is loaded or R2R will be disabled.
    pPEAssembly->EnsureLoaded();

    if (pPEAssembly->IsDynamic())
        // manifest modules of dynamic assemblies are always transient
        m_pModule = ReflectionModule::Create(this, pPEAssembly, pamTracker, REFEMIT_MANIFEST_MODULE_NAME);
    else
        m_pModule = Module::Create(this, pPEAssembly, pamTracker);

    InterlockedIncrement((LONG*)&g_cAssemblies);

    PrepareModuleForAssembly(m_pModule, pamTracker);

    // We'll load the friend assembly information lazily.  For the ngen case we should avoid
    //  loading it entirely.
    //CacheFriendAssemblyInfo();

    if (IsCollectible() && !pPEAssembly->IsDynamic())
    {
        COUNT_T size;
        BYTE* start = (BYTE*)pPEAssembly->GetLoadedImageContents(&size);

        // We should have the content loaded at this time. There will be no other attempt to associate memory.
        _ASSERTE(start != NULL);

        GCX_COOP();
        LoaderAllocator::AssociateMemoryWithLoaderAllocator(start, start + size, m_pLoaderAllocator);
    }

    {
        CANNOTTHROWCOMPLUSEXCEPTION();
        FAULT_FORBID();
        //Cannot fail after this point.

        InterlockedIncrement((LONG*)&m_pClassLoader->m_cUnhashedModules);

        return;  // Explicit return to let you know you are NOT welcome to add code after the CANNOTTHROW/FAULT_FORBID expires
    }
}

Assembly::~Assembly()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        DISABLED(FORBID_FAULT); //Must clean up some profiler stuff
    }
    CONTRACTL_END

    Terminate();

    if (m_pFriendAssemblyDescriptor != NULL)
        m_pFriendAssemblyDescriptor->Release();

    if (m_pPEAssembly)
    {
        m_pPEAssembly->Release();
    }

#ifdef FEATURE_COMINTEROP
    if (m_pITypeLib != nullptr && m_pITypeLib != Assembly::InvalidTypeLib)
    {
        m_pITypeLib->Release();
    }
#endif // FEATURE_COMINTEROP
}

#ifdef PROFILING_SUPPORTED
void ProfilerCallAssemblyUnloadStarted(Assembly* assemblyUnloaded)
{
    WRAPPER_NO_CONTRACT;
    {
        BEGIN_PROFILER_CALLBACK(CORProfilerPresent());
        GCX_PREEMP();
        (&g_profControlBlock)->AssemblyUnloadStarted((AssemblyID)assemblyUnloaded);
        END_PROFILER_CALLBACK();
    }
}

void ProfilerCallAssemblyUnloadFinished(Assembly* assemblyUnloaded)
{
    WRAPPER_NO_CONTRACT;
    {
        BEGIN_PROFILER_CALLBACK(CORProfilerPresent());
        GCX_PREEMP();
        (&g_profControlBlock)->AssemblyUnloadFinished((AssemblyID) assemblyUnloaded, S_OK);
        END_PROFILER_CALLBACK();
    }
}
#endif

void Assembly::StartUnload()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_FORBID_FAULT;

#ifdef PROFILING_SUPPORTED
    if (CORProfilerTrackAssemblyLoads())
    {
        ProfilerCallAssemblyUnloadStarted(this);
    }
#endif
}

void Assembly::Terminate( BOOL signalProfiler )
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;

    STRESS_LOG1(LF_LOADER, LL_INFO100, "Assembly::Terminate (this = 0x%p)\n", reinterpret_cast<void *>(this));

    if (this->m_fTerminated)
        return;

    if (m_pClassLoader != NULL)
    {
        GCX_PREEMP();
        delete m_pClassLoader;
        m_pClassLoader = NULL;
    }

    InterlockedDecrement((LONG*)&g_cAssemblies);

#ifdef PROFILING_SUPPORTED
    if (CORProfilerTrackAssemblyLoads())
    {
        ProfilerCallAssemblyUnloadFinished(this);
    }
#endif // PROFILING_SUPPORTED

    this->m_fTerminated = TRUE;
}

Assembly * Assembly::Create(
    AppDomain *                  pDomain,
    PEAssembly *                 pPEAssembly,
    DebuggerAssemblyControlFlags debuggerFlags,
    BOOL                         fIsCollectible,
    AllocMemTracker *            pamTracker,
    LoaderAllocator *            pLoaderAllocator)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(pDomain == AppDomain::GetCurrentDomain());
    NewHolder<Assembly> pAssembly (new Assembly(pDomain, pPEAssembly, debuggerFlags, fIsCollectible));

#ifdef PROFILING_SUPPORTED
    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackAssemblyLoads());
        GCX_COOP();
        (&g_profControlBlock)->AssemblyLoadStarted((AssemblyID)(Assembly *) pAssembly);
        END_PROFILER_CALLBACK();
    }

    // Need TRY/HOOK instead of holder so we can get HR of exception thrown for profiler callback
    EX_TRY
#endif
    {
        pAssembly->Init(pamTracker, pLoaderAllocator);
    }
#ifdef PROFILING_SUPPORTED
    EX_HOOK
    {
        {
            BEGIN_PROFILER_CALLBACK(CORProfilerTrackAssemblyLoads());
            GCX_COOP();
            (&g_profControlBlock)->AssemblyLoadFinished((AssemblyID)(Assembly *) pAssembly,
                                                                    GET_EXCEPTION()->GetHR());
            END_PROFILER_CALLBACK();
        }
    }
    EX_END_HOOK;
#endif
    pAssembly.SuppressRelease();

    return pAssembly;
} // Assembly::Create

Assembly *Assembly::CreateDynamic(AssemblyBinder* pBinder, NativeAssemblyNameParts* pAssemblyNameParts, INT32 hashAlgorithm, INT32 access, LOADERALLOCATORREF* pKeepAlive)
{
    // WARNING: not backout clean
    CONTRACT(Assembly *)
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        MODE_COOPERATIVE;
    }
    CONTRACT_END;

    // This must be before creation of the AllocMemTracker so that the destructor for the AllocMemTracker happens before the destructor for pLoaderAllocator.
    // That is necessary as the allocation of Assembly objects and other related details is done on top of heaps located in
    // the loader allocator objects.
    NewHolder<LoaderAllocator> pLoaderAllocator;

    AllocMemTracker amTracker;
    AllocMemTracker *pamTracker = &amTracker;

    Assembly *pRetVal = NULL;

    // First, we set up a pseudo-manifest file for the assembly.

    // Set up the assembly name

    if (pAssemblyNameParts->_pName == NULL || pAssemblyNameParts->_pName[0] == '\0')
        COMPlusThrow(kArgumentException, W("ArgumentNull_AssemblyNameName"));

    // Set up the assembly manifest metadata
    // When we create dynamic assembly, we always use a working copy of IMetaDataAssemblyEmit
    // to store temporary runtime assembly information. This is to preserve the invariant that
    // an assembly must have a PEAssembly with proper metadata.
    // This working copy of IMetaDataAssemblyEmit will store every AssemblyRef as a simple name
    // reference as we must have an instance of Assembly(can be dynamic assembly) before we can
    // add such a reference. Also because the referenced assembly if dynamic strong name, it may
    // not be ready to be hashed!

    SafeComHolder<IMetaDataAssemblyEmit> pAssemblyEmit;
    DefineEmitScope(
        IID_IMetaDataAssemblyEmit,
        &pAssemblyEmit);

    // Now create a dynamic PE file out of the name & metadata
    PEAssemblyHolder pPEAssembly;

    {
        GCX_PREEMP();

        ASSEMBLYMETADATA assemData;
        memset(&assemData, 0, sizeof(assemData));

        assemData.usMajorVersion = pAssemblyNameParts->_major;
        assemData.usMinorVersion = pAssemblyNameParts->_minor;
        assemData.usBuildNumber = pAssemblyNameParts->_build;
        assemData.usRevisionNumber = pAssemblyNameParts->_revision;

        assemData.szLocale = (LPWSTR)pAssemblyNameParts->_pCultureName;

        if (hashAlgorithm == 0)
            hashAlgorithm = CALG_SHA1;

        mdAssembly ma;
        IfFailThrow(pAssemblyEmit->DefineAssembly(pAssemblyNameParts->_pPublicKeyOrToken, pAssemblyNameParts->_cbPublicKeyOrToken, hashAlgorithm,
                                                   pAssemblyNameParts->_pName, &assemData, pAssemblyNameParts->_flags,
                                                   &ma));
        pPEAssembly = PEAssembly::Create(pAssemblyEmit);

        // Set it as the fallback load context binder for the dynamic assembly being created
        pPEAssembly->SetFallbackBinder(pBinder);
    }

    AppDomain* pDomain = GetAppDomain();

    NewHolder<DomainAssembly> pDomainAssembly;
    BOOL                      createdNewAssemblyLoaderAllocator = FALSE;

    {
        GCX_PREEMP();

        AssemblyLoaderAllocator* pBinderLoaderAllocator = nullptr;
        if (pBinder != nullptr)
        {
            pBinderLoaderAllocator = pBinder->GetLoaderAllocator();
        }

        // Create a new LoaderAllocator if appropriate
        if ((access & ASSEMBLY_ACCESS_COLLECT) != 0)
        {
            AssemblyLoaderAllocator *pCollectibleLoaderAllocator = new AssemblyLoaderAllocator();
            pLoaderAllocator = pCollectibleLoaderAllocator;

            // Some of the initialization functions are not virtual. Call through the derived class
            // to prevent calling the base class version.
            pCollectibleLoaderAllocator->Init(pDomain);

            // Setup the managed proxy now, but do not actually transfer ownership to it.
            // Once everything is setup and nothing can fail anymore, the ownership will be
            // atomically transferred by call to LoaderAllocator::ActivateManagedTracking().
            pCollectibleLoaderAllocator->SetupManagedTracking(pKeepAlive);
            createdNewAssemblyLoaderAllocator = TRUE;

            if(pBinderLoaderAllocator != nullptr)
            {
                pCollectibleLoaderAllocator->EnsureReference(pBinderLoaderAllocator);
            }
        }
        else
        {
            pLoaderAllocator = pBinderLoaderAllocator == nullptr ? pDomain->GetLoaderAllocator() : pBinderLoaderAllocator;
        }

        if (!createdNewAssemblyLoaderAllocator)
        {
            pLoaderAllocator.SuppressRelease();
        }

        // Create a domain assembly
        pDomainAssembly = new DomainAssembly(pDomain, pPEAssembly, pLoaderAllocator);
        if (pDomainAssembly->IsCollectible())
        {
            // We add the assembly to the LoaderAllocator only when we are sure that it can be added
            // and won't be deleted in case of a concurrent load from the same ALC
            ((AssemblyLoaderAllocator *)(LoaderAllocator *)pLoaderAllocator)->AddDomainAssembly(pDomainAssembly);
        }
    }

    // Start loading process
    {
        // Create a concrete assembly
        // (!Do not remove scoping brace: order is important here: the Assembly holder must destruct before the AllocMemTracker!)
        NewHolder<Assembly> pAssem;

        {
            GCX_PREEMP();
            // Assembly::Create will call SuppressRelease on the NewHolder that holds the LoaderAllocator when it transfers ownership
            pAssem = Assembly::Create(pDomain, pPEAssembly, pDomainAssembly->GetDebuggerInfoBits(), pLoaderAllocator->IsCollectible(), pamTracker, pLoaderAllocator);

            ReflectionModule* pModule = (ReflectionModule*) pAssem->GetModule();

            if (createdNewAssemblyLoaderAllocator)
            {
                // Initializing the virtual call stub manager is delayed to remove the need for the LoaderAllocator destructor to properly handle
                // uninitializing the VSD system. (There is a need to suspend the runtime, and that's tricky)
                pLoaderAllocator->InitVirtualCallStubManager(pDomain);
            }
        }

        pAssem->m_isDynamic = true;

        //we need to suppress release for pAssem to avoid double release
        pAssem.SuppressRelease ();

        {
            GCX_PREEMP();

            // Finish loading process
            // <TODO> would be REALLY nice to unify this with main loading loop </TODO>
            pDomainAssembly->Begin();
            pDomainAssembly->SetAssembly(pAssem);
            pDomainAssembly->m_level = FILE_LOAD_ALLOCATE;
            pDomainAssembly->DeliverSyncEvents();
            pDomainAssembly->DeliverAsyncEvents();
            pDomainAssembly->FinishLoad();
            pDomainAssembly->ClearLoading();
            pDomainAssembly->m_level = FILE_ACTIVE;
        }

        {
            CANNOTTHROWCOMPLUSEXCEPTION();
            FAULT_FORBID();

            //Cannot fail after this point

            pDomainAssembly.SuppressRelease(); // This also effectively suppresses the release of the pAssem
            pamTracker->SuppressRelease();

            // Once we reach this point, the loader allocator lifetime is controlled by the Assembly object.
            if (createdNewAssemblyLoaderAllocator)
            {
                // Atomically transfer ownership to the managed heap
                pLoaderAllocator->ActivateManagedTracking();
                pLoaderAllocator.SuppressRelease();
            }

            pAssem->SetIsTenured();
            pRetVal = pAssem;
        }
    }

    RETURN pRetVal;
} // Assembly::CreateDynamic



void Assembly::SetDomainAssembly(DomainAssembly *pDomainAssembly)
{
    CONTRACTL
    {
        PRECONDITION(CheckPointer(pDomainAssembly));
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    GetModule()->SetDomainAssembly(pDomainAssembly);

} // Assembly::SetDomainAssembly

#endif // #ifndef DACCESS_COMPILE

DomainAssembly *Assembly::GetDomainAssembly()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return GetModule()->GetDomainAssembly();
}

PTR_LoaderHeap Assembly::GetLowFrequencyHeap()
{
    WRAPPER_NO_CONTRACT;

    return GetLoaderAllocator()->GetLowFrequencyHeap();
}

PTR_LoaderHeap Assembly::GetHighFrequencyHeap()
{
    WRAPPER_NO_CONTRACT;

    return GetLoaderAllocator()->GetHighFrequencyHeap();
}


PTR_LoaderHeap Assembly::GetStubHeap()
{
    WRAPPER_NO_CONTRACT;

    return GetLoaderAllocator()->GetStubHeap();
}

Module *Assembly::FindModuleByExportedType(mdExportedType mdType,
                                           Loader::LoadFlag loadFlag,
                                           mdTypeDef mdNested,
                                           mdTypeDef* pCL)
{
    CONTRACT(Module *)
    {
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else INJECT_FAULT(COMPlusThrowOM(););
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, loadFlag==Loader::Load ? NULL_NOT_OK : NULL_OK));
        SUPPORTS_DAC;
    }
    CONTRACT_END

    mdToken mdLinkRef;
    mdToken mdBinding;

    IMDInternalImport *pMDImport = GetMDImport();

    IfFailThrow(pMDImport->GetExportedTypeProps(
        mdType,
        NULL,
        NULL,
        &mdLinkRef,     // Impl
        &mdBinding,     // Hint
        NULL));         // dwflags

    // Don't trust the returned tokens.
    if (!pMDImport->IsValidToken(mdLinkRef))
    {
        if (loadFlag != Loader::Load)
        {
            RETURN NULL;
        }
        else
        {
            ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_TOKEN);
        }
    }

    switch(TypeFromToken(mdLinkRef)) {
    case mdtAssemblyRef:
        {
            *pCL = mdTypeDefNil;  // We don't trust the mdBinding token

            Assembly *pAssembly = NULL;
            switch(loadFlag)
            {
                case Loader::Load:
                {
#ifndef DACCESS_COMPILE
                    // LoadAssembly never returns NULL
                    DomainAssembly * pDomainAssembly =
                        GetModule()->LoadAssembly(mdLinkRef);
                    PREFIX_ASSUME(pDomainAssembly != NULL);

                    RETURN pDomainAssembly->GetModule();
#else
                    _ASSERTE(!"DAC shouldn't attempt to trigger loading");
                    return NULL;
#endif // !DACCESS_COMPILE
                };
                case Loader::DontLoad:
                    pAssembly = GetModule()->GetAssemblyIfLoaded(mdLinkRef);
                    break;
                case Loader::SafeLookup:
                    pAssembly = GetModule()->LookupAssemblyRef(mdLinkRef);
                    break;
                default:
                    _ASSERTE(FALSE);
            }

            if (pAssembly)
                RETURN pAssembly->GetModule();
            else
                RETURN NULL;

        }

    case mdtFile:
        {
            // We may not want to trust this TypeDef token, since it
            // was saved in a scope other than the one it was defined in
            if (mdNested == mdTypeDefNil)
                *pCL = mdBinding;
            else
                *pCL = mdNested;

            // Note that we don't want to attempt a LoadModule if a GetModuleIfLoaded will
            // succeed, because it has a stronger contract.
            Module *pModule = GetModule()->GetModuleIfLoaded(mdLinkRef);
#ifdef DACCESS_COMPILE
            return pModule;
#else
            if (pModule != NULL)
                RETURN pModule;

            if(loadFlag==Loader::SafeLookup)
                return NULL;

            // We should never get here in the GC case - the above should have succeeded.
            CONSISTENCY_CHECK(!FORBIDGC_LOADER_USE_ENABLED());

            DomainAssembly* pDomainModule = NULL;
            if (loadFlag == Loader::Load)
            {
                pDomainModule = GetModule()->LoadModule(mdLinkRef);
            }

            if (pDomainModule == NULL)
                RETURN NULL;
            else
            {
                pModule = pDomainModule->GetModule();
                if (pModule == NULL)
                {
                    _ASSERTE(loadFlag!=Loader::Load);
                }

                RETURN pModule;
            }
#endif // DACCESS_COMPILE
        }

    case mdtExportedType:
        // Only override the nested type token if it hasn't been set yet.
        if (mdNested != mdTypeDefNil)
            mdBinding = mdNested;

        RETURN FindModuleByExportedType(mdLinkRef, loadFlag, mdBinding, pCL);

    default:
        ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_TOKEN_TYPE);
    }
} // Assembly::FindModuleByExportedType


// The returned Module is non-NULL unless you prevented the load by setting loadFlag=Loader::DontLoad.
/* static */
Module * Assembly::FindModuleByTypeRef(
    ModuleBase *     pModule,
    mdTypeRef        tkType,
    Loader::LoadFlag loadFlag,
    BOOL *           pfNoResolutionScope)
{
    CONTRACT(Module *)
    {
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM();); }

        MODE_ANY;

        PRECONDITION(CheckPointer(pModule));
        PRECONDITION(TypeFromToken(tkType) == mdtTypeRef);
        PRECONDITION(CheckPointer(pfNoResolutionScope));
        POSTCONDITION( CheckPointer(RETVAL, loadFlag==Loader::Load ? NULL_NOT_OK : NULL_OK) );
        SUPPORTS_DAC;
    }
    CONTRACT_END

    // WARNING! Correctness of the type forwarder detection algorithm in code:ClassLoader::ResolveTokenToTypeDefThrowing
    // relies on this function not performing any form of type forwarding itself.

    IMDInternalImport * pImport;
    mdTypeRef           tkTopLevelEncloserTypeRef;

    pImport = pModule->GetMDImport();
    if (TypeFromToken(tkType) != mdtTypeRef)
    {
        ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_TOKEN_TYPE);
    }

    {
        // Find the top level encloser
        GCX_NOTRIGGER();

        // If nested, get top level encloser's impl
        int iter = 0;
        int maxIter = 1000;
        do
        {
            _ASSERTE(TypeFromToken(tkType) == mdtTypeRef);
            tkTopLevelEncloserTypeRef = tkType;

            if (!pImport->IsValidToken(tkType) || iter >= maxIter)
            {
                break;
            }

            IfFailThrow(pImport->GetResolutionScopeOfTypeRef(tkType, &tkType));

            // nil-scope TR okay if there's an ExportedType
            // Return manifest file
            if (IsNilToken(tkType))
            {
                *pfNoResolutionScope = TRUE;
                if (!pModule->IsFullModule())
                {
                    // The ModuleBase scenarios should never need this
                    COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
                }
                RETURN(static_cast<Module*>(pModule));
            }
            iter++;
        }
        while (TypeFromToken(tkType) == mdtTypeRef);
    }

    *pfNoResolutionScope = FALSE;

#ifndef DACCESS_COMPILE
    if (!pImport->IsValidToken(tkType)) // redundant check only when invalid token already found.
    {
        THROW_BAD_FORMAT(BFA_BAD_TYPEREF_TOKEN, pModule);
    }
#endif //!DACCESS_COMPILE

    switch (TypeFromToken(tkType))
    {
        case mdtModule:
        {
            if (!pModule->IsFullModule())
            {
                // The ModuleBase scenarios should never need this
                COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
            }

            // Type is in the referencing module.
            GCX_NOTRIGGER();
            CANNOTTHROWCOMPLUSEXCEPTION();
            RETURN( static_cast<Module*>(pModule) );
        }

        case mdtModuleRef:
        {
            if ((loadFlag != Loader::Load) || IsGCThread() || IsStackWalkerThread())
            {
                // Either we're not supposed to load, or we're doing a GC or stackwalk
                // in which case we shouldn't need to load.  So just look up the module
                // and return what we find.
                RETURN(pModule->LookupModule(tkType));
            }

#ifndef DACCESS_COMPILE
            if (loadFlag == Loader::Load)
            {
                DomainAssembly* pActualDomainAssembly = pModule->LoadModule(tkType);
                RETURN(pActualDomainAssembly->GetModule());
            }
            else
            {
                RETURN NULL;
            }

#else //DACCESS_COMPILE
            _ASSERTE(loadFlag!=Loader::Load);
            DacNotImpl();
            RETURN NULL;
#endif //DACCESS_COMPILE
        }
        break;

        case mdtAssemblyRef:
        {
            if(IsAfContentType_WindowsRuntime(pModule->GetAssemblyRefFlags(tkType)))
            {
                ThrowHR(COR_E_PLATFORMNOTSUPPORTED);
            }

            // Do this first because it has a strong contract
            Assembly * pAssembly = NULL;

            if (loadFlag == Loader::SafeLookup)
            {
                pAssembly = pModule->LookupAssemblyRef(tkType);
            }
            else
            {
                pAssembly = pModule->GetAssemblyIfLoaded(tkType);
            }

            if (pAssembly != NULL)
            {
                RETURN pAssembly->m_pModule;
            }

#ifdef DACCESS_COMPILE
            RETURN NULL;
#else
            if (loadFlag != Loader::Load)
            {
                RETURN NULL;
            }


            DomainAssembly * pDomainAssembly = pModule->LoadAssembly(tkType);


            if (pDomainAssembly == NULL)
                RETURN NULL;

            pAssembly = pDomainAssembly->GetAssembly();
            if (pAssembly == NULL)
            {
                RETURN NULL;
            }
            else
            {
                RETURN pAssembly->m_pModule;
            }
#endif //!DACCESS_COMPILE
        }

    default:
        ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_TOKEN_TYPE);
    }
} // Assembly::FindModuleByTypeRef

#ifndef DACCESS_COMPILE

void Assembly::PrepareModuleForAssembly(Module* module, AllocMemTracker *pamTracker)
{
    STANDARD_VM_CONTRACT;

    if (module->m_pAvailableClasses != NULL)
    {
        // ! We intentionally do not take the AvailableClass lock here. It creates problems at
        // startup and we haven't yet published the module yet so nobody should be searching it.
        m_pClassLoader->PopulateAvailableClassHashTable(module, pamTracker);
    }


#ifdef DEBUGGING_SUPPORTED
    // Modules take the DebuggerAssemblyControlFlags down from its
    // parent Assembly initially.
    module->SetDebuggerInfoBits(GetDebuggerInfoBits());

    LOG((LF_CORDB, LL_INFO10, "Module %s: bits=0x%x\n",
         module->GetPEAssembly()->GetSimpleName(),
         module->GetDebuggerInfoBits()));
#endif // DEBUGGING_SUPPORTED
}

//*****************************************************************************
// Set up the list of names of any friend assemblies
void Assembly::CacheFriendAssemblyInfo()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    if (m_pFriendAssemblyDescriptor == NULL)
    {
        ReleaseHolder<FriendAssemblyDescriptor> pFriendAssemblies = FriendAssemblyDescriptor::CreateFriendAssemblyDescriptor(this->GetPEAssembly());
        _ASSERTE(pFriendAssemblies != NULL);

        CrstHolder friendDescriptorLock(&g_friendAssembliesCrst);

        if (m_pFriendAssemblyDescriptor == NULL)
        {
            m_pFriendAssemblyDescriptor = pFriendAssemblies.Extract();
        }
    }
} // void Assembly::CacheFriendAssemblyInfo()

void Assembly::UpdateCachedFriendAssemblyInfo()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    ReleaseHolder<FriendAssemblyDescriptor> pOldFriendAssemblyDescriptor;

    {
        CrstHolder friendDescriptorLock(&g_friendAssembliesCrst);
        if (m_pFriendAssemblyDescriptor != NULL)
        {
            m_pFriendAssemblyDescriptor->AddRef();
            pOldFriendAssemblyDescriptor = m_pFriendAssemblyDescriptor;
        }
    }

    while (true)
    {
        ReleaseHolder<FriendAssemblyDescriptor> pFriendAssemblies = FriendAssemblyDescriptor::CreateFriendAssemblyDescriptor(this->GetPEAssembly());
        FriendAssemblyDescriptor* pFriendAssemblyDescriptorNextLoop = NULL;

        {
            CrstHolder friendDescriptorLock(&g_friendAssembliesCrst);

            if (m_pFriendAssemblyDescriptor == pOldFriendAssemblyDescriptor)
            {
                if (m_pFriendAssemblyDescriptor != NULL)
                    m_pFriendAssemblyDescriptor->Release();

                m_pFriendAssemblyDescriptor = pFriendAssemblies.Extract();
                return;
            }
            else
            {
                m_pFriendAssemblyDescriptor->AddRef();
                pFriendAssemblyDescriptorNextLoop = m_pFriendAssemblyDescriptor;
            }
        }

        // Initialize this here to avoid calling Release on the previous value of pOldFriendAssemblyDescriptor while holding the lock
        pOldFriendAssemblyDescriptor = pFriendAssemblyDescriptorNextLoop;
    }
}

ReleaseHolder<FriendAssemblyDescriptor> Assembly::GetFriendAssemblyInfo()
{
    CacheFriendAssemblyInfo();

    CrstHolder friendDescriptorLock(&g_friendAssembliesCrst);
    m_pFriendAssemblyDescriptor->AddRef();
    ReleaseHolder<FriendAssemblyDescriptor> friendAssemblyDescriptor(m_pFriendAssemblyDescriptor);

    return friendAssemblyDescriptor;
}

//*****************************************************************************
// Is the given assembly a friend of this assembly?
bool Assembly::GrantsFriendAccessTo(Assembly *pAccessingAssembly, FieldDesc *pFD)
{
    WRAPPER_NO_CONTRACT;

    return GetFriendAssemblyInfo()->GrantsFriendAccessTo(pAccessingAssembly, pFD);
}

bool Assembly::GrantsFriendAccessTo(Assembly *pAccessingAssembly, MethodDesc *pMD)
{
    WRAPPER_NO_CONTRACT;

    return GetFriendAssemblyInfo()->GrantsFriendAccessTo(pAccessingAssembly, pMD);
}

bool Assembly::GrantsFriendAccessTo(Assembly *pAccessingAssembly, MethodTable *pMT)
{
    WRAPPER_NO_CONTRACT;

    return GetFriendAssemblyInfo()->GrantsFriendAccessTo(pAccessingAssembly, pMT);
}

bool Assembly::IgnoresAccessChecksTo(Assembly *pAccessedAssembly)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(pAccessedAssembly));
    }
    CONTRACTL_END;

    return GetFriendAssemblyInfo()->IgnoresAccessChecksTo(pAccessedAssembly);
}

void Assembly::AddDiagnosticStartupHookPath(LPCWSTR wszPath)
{
    LPCWSTR wszDiagnosticStartupHookPathsLocal = s_wszDiagnosticStartupHookPaths;

    size_t cchPath = u16_strlen(wszPath);
    size_t cchDiagnosticStartupHookPathsNew = cchPath;
    size_t cchDiagnosticStartupHookPathsLocal = 0;
    if (nullptr != wszDiagnosticStartupHookPathsLocal)
    {
        cchDiagnosticStartupHookPathsLocal = u16_strlen(wszDiagnosticStartupHookPathsLocal);
        // Add 1 for the path separator
        cchDiagnosticStartupHookPathsNew += cchDiagnosticStartupHookPathsLocal + 1;
    }

    size_t currentSize = cchDiagnosticStartupHookPathsNew + 1;
    LPWSTR wszDiagnosticStartupHookPathsNew = new WCHAR[currentSize];
    LPWSTR wszCurrent = wszDiagnosticStartupHookPathsNew;

    u16_strcpy_s(wszCurrent, currentSize, wszPath);
    wszCurrent += cchPath;
    currentSize -= cchPath;

    if (cchDiagnosticStartupHookPathsLocal > 0)
    {
        u16_strcpy_s(wszCurrent, currentSize, PATH_SEPARATOR_STR_W);
        wszCurrent += 1;
        currentSize -= 1;

        u16_strcpy_s(wszCurrent, currentSize, wszDiagnosticStartupHookPathsLocal);
        wszCurrent += cchDiagnosticStartupHookPathsLocal;
        currentSize -= cchDiagnosticStartupHookPathsLocal;
    }

    // Expect null terminating character
    _ASSERTE(currentSize == 1);
    _ASSERTE(wszCurrent[0] == W('\0'));

    s_wszDiagnosticStartupHookPaths = wszDiagnosticStartupHookPathsNew;

    delete [] wszDiagnosticStartupHookPathsLocal;
}

enum CorEntryPointType
{
    EntryManagedMain,                   // void main(String[])
    EntryCrtMain                        // unsigned main(void)
};

void DECLSPEC_NORETURN ThrowMainMethodException(MethodDesc* pMD, UINT resID)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    DefineFullyQualifiedNameForClassW();
    LPCWSTR szClassName = GetFullyQualifiedNameForClassW(pMD->GetMethodTable());
    LPCUTF8 szUTFMethodName;
    if (FAILED(pMD->GetMDImport()->GetNameOfMethodDef(pMD->GetMemberDef(), &szUTFMethodName)))
    {
        szUTFMethodName = "Invalid MethodDef record";
    }
    PREFIX_ASSUME(szUTFMethodName!=NULL);
    MAKE_WIDEPTR_FROMUTF8(szMethodName, szUTFMethodName);
    COMPlusThrowHR(COR_E_METHODACCESS, resID, szClassName, szMethodName);
}

// Returns true if this is a valid main method?
void ValidateMainMethod(MethodDesc * pFD, CorEntryPointType *pType)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());

        PRECONDITION(CheckPointer(pType));
    }
    CONTRACTL_END;

    // Must be static, but we don't care about accessibility
    if ((pFD->GetAttrs() & mdStatic) == 0)
        ThrowMainMethodException(pFD, IDS_EE_MAIN_METHOD_MUST_BE_STATIC);

    if (pFD->GetNumGenericClassArgs() != 0 || pFD->GetNumGenericMethodArgs() != 0)
        ThrowMainMethodException(pFD, IDS_EE_LOAD_BAD_MAIN_SIG);

    // Check for types
    SigPointer sig(pFD->GetSigPointer());

    uint32_t nCallConv;
    if (FAILED(sig.GetData(&nCallConv)))
        ThrowMainMethodException(pFD, BFA_BAD_SIGNATURE);

    if (nCallConv != IMAGE_CEE_CS_CALLCONV_DEFAULT)
        ThrowMainMethodException(pFD, IDS_EE_LOAD_BAD_MAIN_SIG);

    uint32_t nParamCount;
    if (FAILED(sig.GetData(&nParamCount)))
        ThrowMainMethodException(pFD, BFA_BAD_SIGNATURE);


    CorElementType nReturnType;
    if (FAILED(sig.GetElemType(&nReturnType)))
        ThrowMainMethodException(pFD, BFA_BAD_SIGNATURE);

    if ((nReturnType != ELEMENT_TYPE_VOID) && (nReturnType != ELEMENT_TYPE_I4) && (nReturnType != ELEMENT_TYPE_U4))
         ThrowMainMethodException(pFD, IDS_EE_MAIN_METHOD_HAS_INVALID_RTN);

    if (nParamCount == 0)
        *pType = EntryCrtMain;
    else {
        *pType = EntryManagedMain;

        if (nParamCount != 1)
            ThrowMainMethodException(pFD, IDS_EE_TO_MANY_ARGUMENTS_IN_MAIN);

        CorElementType argType;
        CorElementType argType2 = ELEMENT_TYPE_END;

        if (FAILED(sig.GetElemType(&argType)))
            ThrowMainMethodException(pFD, BFA_BAD_SIGNATURE);

        if (argType == ELEMENT_TYPE_SZARRAY)
            if (FAILED(sig.GetElemType(&argType2)))
                ThrowMainMethodException(pFD, BFA_BAD_SIGNATURE);

        if (argType != ELEMENT_TYPE_SZARRAY || argType2 != ELEMENT_TYPE_STRING)
            ThrowMainMethodException(pFD, IDS_EE_LOAD_BAD_MAIN_SIG);
    }
}

struct Param
{
    MethodDesc *pFD;
    short numSkipArgs;
    INT32 *piRetVal;
    PTRARRAYREF *stringArgs;
    CorEntryPointType EntryType;
    DWORD cCommandArgs;
    LPWSTR *wzArgs;
} param;

static void RunMainInternal(Param* pParam)
{
    MethodDescCallSite  threadStart(pParam->pFD);

    PTRARRAYREF StrArgArray = NULL;
    GCPROTECT_BEGIN(StrArgArray);

    // Build the parameter array and invoke the method.
    if (pParam->EntryType == EntryManagedMain) {
        if (pParam->stringArgs == NULL) {
            // Allocate a COM Array object with enough slots for cCommandArgs - 1
            StrArgArray = (PTRARRAYREF) AllocateObjectArray((pParam->cCommandArgs - pParam->numSkipArgs), g_pStringClass);

            // Create Stringrefs for each of the args
            for (DWORD arg = pParam->numSkipArgs; arg < pParam->cCommandArgs; arg++) {
                STRINGREF sref = StringObject::NewString(pParam->wzArgs[arg]);
                StrArgArray->SetAt(arg - pParam->numSkipArgs, (OBJECTREF) sref);
            }
        }
        else
            StrArgArray = *pParam->stringArgs;
    }

    ARG_SLOT stackVar = ObjToArgSlot(StrArgArray);

    if (pParam->pFD->IsVoid())
    {
        // Set the return value to 0 instead of returning random junk
        *pParam->piRetVal = 0;
        threadStart.Call(&stackVar);
    }
    else
    {
        *pParam->piRetVal = (INT32)threadStart.Call_RetArgSlot(&stackVar);
        SetLatchedExitCode(*pParam->piRetVal);
    }

    GCPROTECT_END();

    //<TODO>
    // When we get mainCRTStartup from the C++ then this should be able to go away.</TODO>
    fflush(stdout);
    fflush(stderr);
}

/* static */
HRESULT RunMain(MethodDesc *pFD ,
                short numSkipArgs,
                INT32 *piRetVal,
                PTRARRAYREF *stringArgs /*=NULL*/)
{
    STATIC_CONTRACT_THROWS;
    _ASSERTE(piRetVal);

    DWORD       cCommandArgs = 0;  // count of args on command line
    LPWSTR      *wzArgs = NULL; // command line args
    HRESULT     hr = S_OK;

    *piRetVal = -1;

    // The exit code for the process is communicated in one of two ways.  If the
    // entrypoint returns an 'int' we take that.  Otherwise we take a latched
    // process exit code.  This can be modified by the app via setting
    // Environment's ExitCode property.
    //
    // When we're executing the default exe main in the default domain, set the latched exit code to
    // zero as a default.  If it gets set to something else by user code then that value will be returned.
    //
    // StringArgs appears to be non-null only when the main method is explicitly invoked via the hosting api
    // or through creating a subsequent domain and running an exe within it.  In those cases we don't
    // want to reset the (global) latched exit code.
    if (stringArgs == NULL)
        SetLatchedExitCode(0);

    if (!pFD) {
        _ASSERTE(!"Must have a function to call!");
        return E_FAIL;
    }

    CorEntryPointType EntryType = EntryManagedMain;
    ValidateMainMethod(pFD, &EntryType);

    if ((EntryType == EntryManagedMain) &&
        (stringArgs == NULL)) {
        return E_INVALIDARG;
    }

    ETWFireEvent(Main_V1);

    Param param;

    param.pFD = pFD;
    param.numSkipArgs = numSkipArgs;
    param.piRetVal = piRetVal;
    param.stringArgs = stringArgs;
    param.EntryType = EntryType;
    param.cCommandArgs = cCommandArgs;
    param.wzArgs = wzArgs;

    EX_TRY_NOCATCH(Param *, pParam, &param)
    {
        RunMainInternal(pParam);
    }
    EX_END_NOCATCH

    ETWFireEvent(MainEnd_V1);

    return hr;
}

static void RunMainPre()
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(GetThreadNULLOk() != 0);
    g_fWeControlLifetime = TRUE;
}

static void RunMainPost()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(CheckPointer(GetThreadNULLOk()));
    }
    CONTRACTL_END

    GCX_PREEMP();
    ThreadStore::s_pThreadStore->WaitForOtherThreads();

    DWORD dwSecondsToSleep = g_pConfig->GetSleepOnExit();

    // if dwSeconds is non-zero then we will sleep for that many seconds
    // before we exit this allows the vaDumpCmd to detect that our process
    // has gone idle and this allows us to get a vadump of our process at
    // this point in it's execution
    //
    if (dwSecondsToSleep != 0)
    {
        ClrSleepEx(dwSecondsToSleep * 1000, FALSE);
    }
}

void RunManagedStartup()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    MethodDescCallSite managedStartup(METHOD__STARTUP_HOOK_PROVIDER__MANAGED_STARTUP);

    ARG_SLOT args[1];
    args[0] = PtrToArgSlot(s_wszDiagnosticStartupHookPaths);

    managedStartup.Call(args);
}

INT32 Assembly::ExecuteMainMethod(PTRARRAYREF *stringArgs, BOOL waitForOtherThreads)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        ENTRY_POINT;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    // reset the error code for std C
    errno=0;

    HRESULT hr = S_OK;
    INT32   iRetVal = 0;

    Thread *pThread = GetThread();
    MethodDesc *pMeth;
    {
        // This thread looks like it wandered in -- but actually we rely on it to keep the process alive.
        pThread->SetBackground(FALSE);

        GCX_COOP();

        pMeth = GetEntryPoint();

        if (pMeth) {
            {
#ifdef FEATURE_COMINTEROP
                GCX_PREEMP();

                Thread::ApartmentState state = Thread::AS_Unknown;
                state = SystemDomain::GetEntryPointThreadAptState(pMeth->GetMDImport(), pMeth->GetMemberDef());
                SystemDomain::SetThreadAptState(state);
#endif // FEATURE_COMINTEROP
            }

            RunMainPre();

            // Set the root assembly as the assembly that is containing the main method
            // The root assembly is used in the GetEntryAssembly method that on CoreCLR is used
            // to get the TargetFrameworkMoniker for the app
            AppDomain * pDomain = pThread->GetDomain();
            Assembly* pRootAssembly = pMeth->GetAssembly();
            pDomain->SetRootAssembly(pRootAssembly);
#ifdef FEATURE_READYTORUN
            {
                if (pRootAssembly->GetModule()->IsReadyToRun())
                {
                    pRootAssembly->GetModule()->GetReadyToRunInfo()->RegisterUnrelatedR2RModule();
                }
            }
#endif

            // Perform additional managed thread initialization.
            // This would is normally done in the runtime when a managed
            // thread is started, but is done here instead since the
            // Main thread wasn't started by the runtime.
            Thread::InitializationForManagedThreadInNative(pThread);

            RunManagedStartup();

            hr = RunMain(pMeth, 1, &iRetVal, stringArgs);

            Thread::CleanUpForManagedThreadInNative(pThread);
        }
    }

    //RunMainPost is supposed to be called on the main thread of an EXE,
    //after that thread has finished doing useful work.  It contains logic
    //to decide when the process should get torn down.  So, don't call it from
    // AppDomain.ExecuteAssembly()
    if (pMeth) {
        if (waitForOtherThreads)
            RunMainPost();
    }
    else {
        StackSString displayName;
        GetDisplayName(displayName);
        COMPlusThrowHR(COR_E_MISSINGMETHOD, IDS_EE_FAILED_TO_FIND_MAIN, displayName);
    }

    IfFailThrow(hr);

    return iRetVal;
}

MethodDesc* Assembly::GetEntryPoint()
{
    CONTRACT(MethodDesc*)
    {
        THROWS;
        INJECT_FAULT(COMPlusThrowOM(););
        MODE_ANY;

        // Can return NULL if no entry point.
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    if (m_pEntryPoint)
        RETURN m_pEntryPoint;

    mdToken mdEntry = m_pPEAssembly->GetEntryPointToken();
    if (IsNilToken(mdEntry))
        RETURN NULL;

    Module *pModule = NULL;
    switch(TypeFromToken(mdEntry)) {
    case mdtFile:
        pModule = m_pModule->LoadModule(mdEntry)->GetModule();

        mdEntry = pModule->GetEntryPointToken();
        if ( (TypeFromToken(mdEntry) != mdtMethodDef) ||
             (!pModule->GetMDImport()->IsValidToken(mdEntry)) )
            pModule = NULL;
        break;

    case mdtMethodDef:
        if (m_pPEAssembly->GetMDImport()->IsValidToken(mdEntry))
            pModule = m_pModule;
        break;
    }

    // May be unmanaged entrypoint
    if (!pModule)
        RETURN NULL;

    // We need to get its properties and the class token for this MethodDef token.
    mdToken mdParent;
    if (FAILED(pModule->GetMDImport()->GetParentToken(mdEntry, &mdParent))) {
        StackSString displayName;
        GetDisplayName(displayName);
        COMPlusThrowHR(COR_E_BADIMAGEFORMAT, IDS_EE_ILLEGAL_TOKEN_FOR_MAIN, displayName);
    }

    // For the entrypoint, also validate if the paramList is valid or not. We do this check
    // by asking for the return-value  (sequence 0) parameter to MDInternalRO::FindParamOfMethod.
    // Incase the parameter list is invalid, CLDB_E_FILE_CORRUPT will be returned
    // byMDInternalRO::FindParamOfMethod and we will bail out.
    //
    // If it does not exist (return value CLDB_E_RECORD_NOTFOUND) or if it is found (S_OK),
    // we do not bother as the values would have come upon ensuring a valid parameter record
    // list.
    mdParamDef pdParam;
    HRESULT hrValidParamList = pModule->GetMDImport()->FindParamOfMethod(mdEntry, 0, &pdParam);
    if (hrValidParamList == CLDB_E_FILE_CORRUPT)
    {
        // Throw an exception for bad_image_format (because of corrupt metadata)
        StackSString displayName;
        GetDisplayName(displayName);
        COMPlusThrowHR(COR_E_BADIMAGEFORMAT, IDS_EE_ILLEGAL_TOKEN_FOR_MAIN, displayName);
    }

    if (mdParent != COR_GLOBAL_PARENT_TOKEN) {
        GCX_COOP();
        // This code needs a class init frame, because without it, the
        // debugger will assume any code that results from searching for a
        // type handle (ie, loading an assembly) is the first line of a program.
        FrameWithCookie<DebuggerClassInitMarkFrame> __dcimf;

        MethodTable * pInitialMT = ClassLoader::LoadTypeDefOrRefThrowing(pModule, mdParent,
                                                                       ClassLoader::ThrowIfNotFound,
                                                                       ClassLoader::FailIfUninstDefOrRef).GetMethodTable();

        m_pEntryPoint = MemberLoader::FindMethod(pInitialMT, mdEntry);

        __dcimf.Pop();
    }
    else
    {
        m_pEntryPoint = pModule->FindMethod(mdEntry);
    }

    RETURN m_pEntryPoint;
}

OBJECTREF Assembly::GetExposedObject()
{
    CONTRACT(OBJECTREF)
    {
        GC_TRIGGERS;
        THROWS;
        INJECT_FAULT(COMPlusThrowOM(););
        MODE_COOPERATIVE;
    }
    CONTRACT_END;

    RETURN GetDomainAssembly()->GetExposedAssemblyObject();
}

/* static */
BOOL Assembly::FileNotFound(HRESULT hr)
{
    LIMITED_METHOD_CONTRACT;
    if (IsHRESULTForExceptionKind(hr, kFileNotFoundException))
        return TRUE;

#ifdef FEATURE_COMINTEROP
    return hr == RO_E_METADATA_NAME_NOT_FOUND;
#else
    return FALSE;
#endif //FEATURE_COMINTEROP
}


BOOL Assembly::GetResource(LPCSTR szName, DWORD *cbResource,
                              PBYTE *pbInMemoryResource, Assembly** pAssemblyRef,
                              LPCSTR *szFileName, DWORD *dwLocation,
                              BOOL fSkipRaiseResolveEvent)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    DomainAssembly *pAssembly = NULL;
    BOOL result = GetDomainAssembly()->GetResource(szName, cbResource,
                                                   pbInMemoryResource, &pAssembly,
                                                   szFileName, dwLocation,
                                                   fSkipRaiseResolveEvent);
    if (result && pAssemblyRef != NULL && pAssembly != NULL)
        *pAssemblyRef = pAssembly->GetAssembly();

    return result;
}

#ifdef FEATURE_COMINTEROP

ITypeLib * const Assembly::InvalidTypeLib = (ITypeLib *)-1;

ITypeLib* Assembly::GetTypeLib()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        FORBID_FAULT;
    }
    CONTRACTL_END

    ITypeLib *pTlb = m_pITypeLib;
    if (pTlb != nullptr && pTlb != Assembly::InvalidTypeLib)
        pTlb->AddRef();

    return pTlb;
} // ITypeLib* Assembly::GetTypeLib()

bool Assembly::TrySetTypeLib(_In_ ITypeLib *pNew)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        FORBID_FAULT;
        PRECONDITION(CheckPointer(pNew));
    }
    CONTRACTL_END

    ITypeLib *pOld = InterlockedCompareExchangeT(&m_pITypeLib, pNew, nullptr);
    if (pOld != nullptr)
        return false;

    if (pNew != Assembly::InvalidTypeLib)
        pNew->AddRef();

    return true;
} // void Assembly::TrySetTypeLib()

#endif // FEATURE_COMINTEROP

//***********************************************************
// Add an assembly to the assemblyref list. pAssemEmitter specifies where
// the AssemblyRef is emitted to.
//***********************************************************
mdAssemblyRef Assembly::AddAssemblyRef(Assembly *refedAssembly, IMetaDataAssemblyEmit *pAssemEmitter)
{
    CONTRACT(mdAssemblyRef)
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(CheckPointer(refedAssembly));
        PRECONDITION(CheckPointer(pAssemEmitter, NULL_NOT_OK));
        POSTCONDITION(!IsNilToken(RETVAL));
        POSTCONDITION(TypeFromToken(RETVAL) == mdtAssemblyRef);
    }
    CONTRACT_END;

    SafeComHolder<IMetaDataAssemblyEmit> emitHolder;

    AssemblySpec spec;
    spec.InitializeSpec(refedAssembly->GetPEAssembly());

    if (refedAssembly->IsCollectible())
    {
        if (this->IsCollectible())
            this->GetLoaderAllocator()->EnsureReference(refedAssembly->GetLoaderAllocator());
        else
            COMPlusThrow(kNotSupportedException, W("NotSupported_CollectibleBoundNonCollectible"));
    }

    mdAssemblyRef ar;
    IfFailThrow(spec.EmitToken(pAssemEmitter, &ar));

    RETURN ar;
}   // Assembly::AddAssemblyRef

//***********************************************************
// Add a typedef to the runtime TypeDef table of this assembly
//***********************************************************
void Assembly::AddType(
    Module          *pModule,
    mdTypeDef       cl)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    AllocMemTracker amTracker;

    if (pModule->GetAssembly() != this)
    {
        // you cannot add a typedef outside of the assembly to the typedef table
        _ASSERTE(!"Bad usage!");
    }
    m_pClassLoader->AddAvailableClassDontHaveLock(pModule,
                                                  cl,
                                                  &amTracker);
    amTracker.SuppressRelease();
}

//***********************************************************
// Add an ExportedType to the runtime TypeDef table of this assembly
//***********************************************************
void Assembly::AddExportedType(mdExportedType cl)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    AllocMemTracker amTracker;
    m_pClassLoader->AddExportedTypeDontHaveLock(GetModule(),
        cl,
        &amTracker);
    amTracker.SuppressRelease();
}



void DECLSPEC_NORETURN Assembly::ThrowTypeLoadException(LPCUTF8 pszFullName, UINT resIDWhy)
{
    WRAPPER_NO_CONTRACT;
    ThrowTypeLoadException(NULL, pszFullName, NULL,
                           resIDWhy);
}

void DECLSPEC_NORETURN Assembly::ThrowTypeLoadException(LPCUTF8 pszNameSpace, LPCUTF8 pszTypeName,
                                                        UINT resIDWhy)
{
    WRAPPER_NO_CONTRACT;
    ThrowTypeLoadException(pszNameSpace, pszTypeName, NULL,
                           resIDWhy);

}

void DECLSPEC_NORETURN Assembly::ThrowTypeLoadException(NameHandle *pName, UINT resIDWhy)
{
    STATIC_CONTRACT_THROWS;

    if (pName->GetName()) {
        ThrowTypeLoadException(pName->GetNameSpace(),
                               pName->GetName(),
                               NULL,
                               resIDWhy);
    }
    else
        ThrowTypeLoadException(pName->GetTypeModule()->GetMDImport(),
                               pName->GetTypeToken(),
                               resIDWhy);

}

void DECLSPEC_NORETURN Assembly::ThrowTypeLoadException(IMDInternalImport *pInternalImport,
                                                        mdToken token,
                                                        UINT resIDWhy)
{
    WRAPPER_NO_CONTRACT;
    ThrowTypeLoadException(pInternalImport, token, NULL, resIDWhy);
}

void DECLSPEC_NORETURN Assembly::ThrowTypeLoadException(IMDInternalImport *pInternalImport,
                                                        mdToken token,
                                                        LPCUTF8 pszFieldOrMethodName,
                                                        UINT resIDWhy)
{
    STATIC_CONTRACT_THROWS;
    char pszBuff[32];
    LPCUTF8 pszClassName = (LPCUTF8)pszBuff;
    LPCUTF8 pszNameSpace = "Invalid_Token";

    if(pInternalImport->IsValidToken(token))
    {
        switch (TypeFromToken(token)) {
        case mdtTypeRef:
            if (FAILED(pInternalImport->GetNameOfTypeRef(token, &pszNameSpace, &pszClassName)))
            {
                pszNameSpace = pszClassName = "Invalid TypeRef record";
            }
            break;
        case mdtTypeDef:
            if (FAILED(pInternalImport->GetNameOfTypeDef(token, &pszClassName, &pszNameSpace)))
            {
                pszNameSpace = pszClassName = "Invalid TypeDef record";
            }
            break;
        case mdtTypeSpec:

            // If you see this assert, you need to make sure the message for
            // this resID is appropriate for TypeSpecs
            _ASSERTE((resIDWhy == IDS_CLASSLOAD_GENERAL) ||
                     (resIDWhy == IDS_CLASSLOAD_BADFORMAT) ||
                     (resIDWhy == IDS_CLASSLOAD_TYPESPEC));

            resIDWhy = IDS_CLASSLOAD_TYPESPEC;
        }
    }
    else
        sprintf_s(pszBuff, sizeof(pszBuff), "0x%8.8X", token);

    ThrowTypeLoadException(pszNameSpace, pszClassName,
                           pszFieldOrMethodName, resIDWhy);
}



void DECLSPEC_NORETURN Assembly::ThrowTypeLoadException(LPCUTF8 pszNameSpace,
                                                        LPCUTF8 pszTypeName,
                                                        LPCUTF8 pszMethodName,
                                                        UINT resIDWhy)
{
    STATIC_CONTRACT_THROWS;

    StackSString displayName;
    GetDisplayName(displayName);

    ::ThrowTypeLoadException(pszNameSpace, pszTypeName, displayName,
                             pszMethodName, resIDWhy);
}

void DECLSPEC_NORETURN Assembly::ThrowBadImageException(LPCUTF8 pszNameSpace,
                                                        LPCUTF8 pszTypeName,
                                                        UINT resIDWhy)
{
    STATIC_CONTRACT_THROWS;

    StackSString displayName;
    GetDisplayName(displayName);

    StackSString fullName;
    SString sNameSpace(SString::Utf8, pszNameSpace);
    SString sTypeName(SString::Utf8, pszTypeName);
    fullName.MakeFullNamespacePath(sNameSpace, sTypeName);

    COMPlusThrowHR(COR_E_BADIMAGEFORMAT, resIDWhy, fullName, displayName);
}

#endif // #ifndef DACCESS_COMPILE

#ifndef DACCESS_COMPILE
void Assembly::EnsureActive()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    GetDomainAssembly()->EnsureActive();
}
#endif //!DACCESS_COMPILE

CHECK Assembly::CheckActivated()
{
#ifndef DACCESS_COMPILE
    WRAPPER_NO_CONTRACT;

    CHECK(GetDomainAssembly()->CheckActivated());
#endif
    CHECK_OK;
}



#ifdef DACCESS_COMPILE

void
Assembly::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

     // We don't need Assembly info in triage dumps.
    if (flags == CLRDATA_ENUM_MEM_TRIAGE)
    {
        return;
    }

    DAC_ENUM_DTHIS();
    EMEM_OUT(("MEM: %p Assembly\n", dac_cast<TADDR>(this)));

    if (flags == CLRDATA_ENUM_MEM_HEAP2)
    {
        GetLoaderAllocator()->EnumMemoryRegions(flags);
    }
    else
    {
        if (m_pClassLoader.IsValid())
        {
            m_pClassLoader->EnumMemoryRegions(flags);
        }
        if (m_pModule.IsValid())
        {
            m_pModule->EnumMemoryRegions(flags, true);
        }
        if (m_pPEAssembly.IsValid())
        {
            m_pPEAssembly->EnumMemoryRegions(flags);
        }
    }
}

#endif

#ifndef DACCESS_COMPILE

FriendAssemblyDescriptor::FriendAssemblyDescriptor()
{
}

FriendAssemblyDescriptor::~FriendAssemblyDescriptor()
{
    CONTRACTL
    {
        DESTRUCTOR_CHECK;
    }
    CONTRACTL_END;

    ArrayList::Iterator itFullAccessAssemblies = m_alFullAccessFriendAssemblies.Iterate();
    while (itFullAccessAssemblies.Next())
    {
        FriendAssemblyName_t *pFriendAssemblyName = static_cast<FriendAssemblyName_t *>(itFullAccessAssemblies.GetElement());
        delete pFriendAssemblyName;
    }
}


//---------------------------------------------------------------------------------------
//
// Builds a FriendAssemblyDescriptor for a given assembly
//
// Arguments:
//    pAssembly - assembly to get friend assembly information for
//
// Return Value:
//    A friend assembly descriptor if the assembly declares any friend assemblies
//

// static
ReleaseHolder<FriendAssemblyDescriptor> FriendAssemblyDescriptor::CreateFriendAssemblyDescriptor(PEAssembly *pAssembly)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(pAssembly));
    }
    CONTRACTL_END

    ReleaseHolder<FriendAssemblyDescriptor> pFriendAssemblies = new FriendAssemblyDescriptor;

    // We're going to do this twice, once for InternalsVisibleTo and once for IgnoresAccessChecks
    IMDInternalImport* pImport = pAssembly->GetMDImport();
    for(int count = 0 ; count < 2 ; ++count)
    {
        _ASSERTE(pImport != NULL);
        MDEnumHolder hEnum(pImport);
        HRESULT hr = S_OK;

        if (count == 0)
        {
            hr = pImport->EnumCustomAttributeByNameInit(TokenFromRid(1, mdtAssembly), FRIEND_ASSEMBLY_TYPE, &hEnum);
        }
        else
        {
            hr = pImport->EnumCustomAttributeByNameInit(TokenFromRid(1, mdtAssembly), SUBJECT_ASSEMBLY_TYPE, &hEnum);
        }

        IfFailThrow(hr);

        // Nothing to do if there are no attributes
        if (hr == S_FALSE)
        {
            continue;
        }

        // Enumerate over the declared friends
        mdCustomAttribute tkAttribute;
        while (pImport->EnumNext(&hEnum, &tkAttribute))
        {
            // Get raw custom attribute.
            const BYTE  *pbAttr = NULL;         // Custom attribute data as a BYTE*.
            ULONG cbAttr = 0;                   // Size of custom attribute data.
            if (FAILED(pImport->GetCustomAttributeAsBlob(tkAttribute, reinterpret_cast<const void **>(&pbAttr), &cbAttr)))
            {
                THROW_BAD_FORMAT(BFA_INVALID_TOKEN, pAssembly);
            }

            CustomAttributeParser cap(pbAttr, cbAttr);
            if (FAILED(cap.ValidateProlog()))
            {
                THROW_BAD_FORMAT(BFA_BAD_CA_HEADER, pAssembly);
            }

            // Get the name of the friend assembly.
            LPCUTF8 szString;
            ULONG   cbString;
            if (FAILED(cap.GetNonNullString(&szString, &cbString)))
            {
                THROW_BAD_FORMAT(BFA_BAD_CA_HEADER, pAssembly);
            }

            // Convert the string to Unicode.
            StackSString displayName(SString::Utf8, szString, cbString);

            // Create an AssemblyNameObject from the string.
            FriendAssemblyNameHolder pFriendAssemblyName;
            pFriendAssemblyName = new FriendAssemblyName_t;
            hr = pFriendAssemblyName->InitNoThrow(displayName);

            if (SUCCEEDED(hr))
            {
                hr = pFriendAssemblyName->CheckFriendAssemblyName();
            }

            if (FAILED(hr))
            {
                THROW_HR_ERROR_WITH_INFO(hr, pAssembly);
            }

            if (count == 1)
            {
                pFriendAssemblies->AddSubjectAssembly(pFriendAssemblyName);
                pFriendAssemblyName.SuppressRelease();
                // Below checks are unnecessary for IgnoresAccessChecks
                continue;
            }

            // CoreCLR does not have a valid scenario for strong-named assemblies requiring their dependencies
            // to be strong-named as well.

            pFriendAssemblies->AddFriendAssembly(pFriendAssemblyName);

            pFriendAssemblyName.SuppressRelease();
        }
    }

    return pFriendAssemblies;
}

//---------------------------------------------------------------------------------------
//
// Adds an assembly to the list of friend assemblies for this descriptor
//
// Arguments:
//    pFriendAssembly      - friend assembly to add to the list
//    fAllInternalsVisible - true if all internals are visible to the friend, false if only specifically
//                           marked internals are visible
//
// Notes:
//    This method takes ownership of the friend assembly name. It is not thread safe and does not check to
//    see if an assembly has already been added to the friend assembly list.
//

void FriendAssemblyDescriptor::AddFriendAssembly(FriendAssemblyName_t *pFriendAssembly)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(pFriendAssembly));
    }
    CONTRACTL_END

    m_alFullAccessFriendAssemblies.Append(pFriendAssembly);
}

void FriendAssemblyDescriptor::AddSubjectAssembly(FriendAssemblyName_t *pFriendAssembly)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(pFriendAssembly));
    }
    CONTRACTL_END

    m_subjectAssemblies.Append(pFriendAssembly);
}

// static
bool FriendAssemblyDescriptor::IsAssemblyOnList(PEAssembly *pAssembly, const ArrayList &alAssemblyNames)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(pAssembly));
    }
    CONTRACTL_END;

    AssemblySpec asmDef;
    asmDef.InitializeSpec(pAssembly);

    ArrayList::ConstIterator itAssemblyNames = alAssemblyNames.Iterate();
    while (itAssemblyNames.Next())
    {
        const FriendAssemblyName_t *pFriendAssemblyName = static_cast<const FriendAssemblyName_t *>(itAssemblyNames.GetElement());
        HRESULT hr = AssemblySpec::RefMatchesDef(pFriendAssemblyName, &asmDef) ? S_OK : S_FALSE;

        if (hr == S_OK)
        {
            return true;
        }
    }

    return false;
}

#endif // !DACCESS_COMPILE


