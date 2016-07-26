// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
#include "security.h"
#include "perfcounters.h"
#include "assemblyname.hpp"

#ifdef FEATURE_FUSION
#include "fusion.h"
#include "assemblysink.h"
#include "ngenoptout.h"
#endif

#if !defined(FEATURE_CORECLR) && !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
#include "assemblyusagelogmanager.h"
#include "policy.h"
#endif

#include "eeprofinterfaces.h"
#include "reflectclasswriter.h"
#include "comdynamic.h"

#include <wincrypt.h>
#include "urlmon.h"
#include "sha1.h"

#include "eeconfig.h"
#include "strongname.h"

#include "ceefilegenwriter.h"
#include "assemblynative.hpp"
#include "threadsuspend.h"

#ifdef FEATURE_PREJIT
#include "corcompile.h"
#endif

#include "appdomainnative.hpp"
#ifdef FEATURE_REMOTING
#include "remoting.h"
#include "appdomainhelper.h"
#endif
#include "customattribute.h"
#include "winnls.h"

#include "constrainedexecutionregion.h"
#include "caparser.h"
#include "../md/compiler/custattr.h"
#include "mdaassistants.h"

#include "peimagelayout.inl"

#if !defined(FEATURE_CORECLR) && !defined(CROSSGEN_COMPILE)
#include <shlobj.h>
#include "eventmsg.h"
#endif

#ifdef FEATURE_TRACELOGGING
#include "clrtracelogging.h"
#endif // FEATURE_TRACELOGGING


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


#ifndef DACCESS_COMPILE

// This value is to make it easier to diagnose Assembly Loader "grant set" crashes.
// See Dev11 bug 358184 for more details.

// This value is not thread safe and is not intended to be. It is just a best
// effort to collect more data on the problem. Is is possible, though unlikely,
// that thread A would record a reason for an upcoming crash,
// thread B would then record a different reason, and we would then
// crash on thread A, thus ending up with the recorded reason not matching
// the thread we crash in. Be aware of this when using this value
// to help your debugging.
DWORD g_dwLoaderReasonForNotSharing = 0; // See code:DomainFile::m_dwReasonForRejectingNativeImage for a similar variable.

// These will sometimes result in a crash with error code 0x80131401 SECURITY_E_INCOMPATIBLE_SHARE
// "Loading this assembly would produce a different grant set from other instances."
enum ReasonForNotSharing
{
    ReasonForNotSharing_NoInfoRecorded = 0x1,
    ReasonForNotSharing_NullDomainassembly = 0x2,
    ReasonForNotSharing_DebuggerFlagMismatch = 0x3,
    ReasonForNotSharing_NullPeassembly = 0x4,
    ReasonForNotSharing_MissingAssemblyClosure1 = 0x5,
    ReasonForNotSharing_MissingAssemblyClosure2 = 0x6,
    ReasonForNotSharing_MissingDependenciesResolved = 0x7,
    ReasonForNotSharing_ClosureComparisonFailed = 0x8,
};

#define NO_FRIEND_ASSEMBLIES_MARKER ((FriendAssemblyDescriptor *)S_FALSE)

//----------------------------------------------------------------------------------------------
// The ctor's job is to initialize the Assembly enough so that the dtor can safely run.
// It cannot do any allocations or operations that might fail. Those operations should be done
// in Assembly::Init()
//----------------------------------------------------------------------------------------------
Assembly::Assembly(BaseDomain *pDomain, PEAssembly* pFile, DebuggerAssemblyControlFlags debuggerFlags, BOOL fIsCollectible) :
    m_FreeFlag(0),
#ifdef FEATURE_MULTIMODULE_ASSEMBLIES
    m_pAllowedFiles(NULL),
    m_crstAllowedFiles(CrstAllowedFiles),
#endif
    m_pDomain(pDomain),
    m_pClassLoader(NULL),
    m_pEntryPoint(NULL),
    m_pManifest(NULL),
    m_pManifestFile(clr::SafeAddRef(pFile)),
    m_pOnDiskManifest(NULL),
    m_pFriendAssemblyDescriptor(NULL),
    m_pbStrongNameKeyPair(NULL),
    m_pwStrongNameKeyContainer(NULL),
    m_isDynamic(false),
#ifdef FEATURE_COLLECTIBLE_TYPES
    m_isCollectible(fIsCollectible),
#endif
    m_needsToHideManifestForEmit(FALSE),
    m_dwDynamicAssemblyAccess(ASSEMBLY_ACCESS_RUN),
    m_nextAvailableModuleIndex(1),
    m_pLoaderAllocator(NULL),
    m_isDisabledPrivateReflection(0),
#ifdef FEATURE_COMINTEROP
    m_pITypeLib(NULL),
    m_winMDStatus(WinMDStatus_Unknown),
    m_pManifestWinMDImport(NULL),
#endif // FEATURE_COMINTEROP
    m_pSharedSecurityDesc(NULL),
    m_pTransparencyBehavior(NULL),
    m_fIsDomainNeutral(pDomain == SharedDomain::GetDomain()),
#ifdef FEATURE_LOADER_OPTIMIZATION
    m_bMissingDependenciesCheckDone(FALSE),
#ifdef FEATURE_FUSION
    m_pBindingClosure(NULL),
#endif
#endif // FEATURE_LOADER_OPTIMIZATION
    m_debuggerFlags(debuggerFlags),
    m_fTerminated(FALSE),
    m_HostAssemblyId(0)
#ifdef FEATURE_COMINTEROP
    , m_InteropAttributeStatus(INTEROP_ATTRIBUTE_UNSET)
#endif
#ifndef FEATURE_CORECLR
    , m_fSupportsAutoNGen(FALSE)
#endif
{
    STANDARD_VM_CONTRACT;
}

// This name needs to stay in sync with AssemblyBuilder.MANIFEST_MODULE_NAME
// which is used in AssemblyBuilder.InitManifestModule
#define REFEMIT_MANIFEST_MODULE_NAME W("RefEmit_InMemoryManifestModule")


#ifdef FEATURE_TRACELOGGING
//----------------------------------------------------------------------------------------------
// Reads and logs the TargetFramework attribute for an assembly. For example: [assembly: TargetFramework(".NETFramework,Version=v4.0")]
//----------------------------------------------------------------------------------------------
void Assembly::TelemetryLogTargetFrameworkAttribute()
{
    const BYTE  *pbAttr;                // Custom attribute data as a BYTE*.
    ULONG       cbAttr;                 // Size of custom attribute data.
    HRESULT hr = GetManifestImport()->GetCustomAttributeByName(GetManifestToken(), TARGET_FRAMEWORK_TYPE, (const void**)&pbAttr, &cbAttr);
    bool dataLogged = false; 
    if (hr == S_OK)
    {
        CustomAttributeParser cap(pbAttr, cbAttr);
        LPCUTF8 lpTargetFramework;
        ULONG cbTargetFramework;
        if (SUCCEEDED(cap.ValidateProlog()))
        {
            if (SUCCEEDED(cap.GetString(&lpTargetFramework, &cbTargetFramework)))
            {
                if ((lpTargetFramework != NULL) && (cbTargetFramework != 0))
                {
                    SString s(SString::Utf8, lpTargetFramework, cbTargetFramework);
                    CLRTraceLog::Logger::LogTargetFrameworkAttribute(s.GetUnicode(), GetSimpleName());
                    dataLogged = true;
                }
            }
        }
    }
    if (!dataLogged) 
    { 
        CLRTraceLog::Logger::LogTargetFrameworkAttribute(L"", GetSimpleName());
    }
}

#endif // FEATURE_TRACELOGGING

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
        if (!IsDomainNeutral())
        {
            if (!IsCollectible())
            {
                // pLoaderAllocator will only be non-null for reflection emit assemblies
                _ASSERTE((pLoaderAllocator == NULL) || (pLoaderAllocator == GetDomain()->AsAppDomain()->GetLoaderAllocator()));
                m_pLoaderAllocator = GetDomain()->AsAppDomain()->GetLoaderAllocator();
            }
            else
            {
                _ASSERTE(pLoaderAllocator != NULL); // ppLoaderAllocator must be non-null for collectible assemblies

                m_pLoaderAllocator = pLoaderAllocator;
            }
        }
        else
        {
            _ASSERTE(pLoaderAllocator == NULL); // pLoaderAllocator may only be non-null for collectible types
            // use global loader heaps
            m_pLoaderAllocator = SystemDomain::GetGlobalLoaderAllocator();
        }
    }
    _ASSERTE(m_pLoaderAllocator != NULL);

    m_pClassLoader = new ClassLoader(this);
    m_pClassLoader->Init(pamTracker);

    m_pSharedSecurityDesc = Security::CreateSharedSecurityDescriptor(this);

#ifdef FEATURE_MULTIMODULE_ASSEMBLIES
    m_pAllowedFiles = new EEUtf8StringHashTable();
#endif

    COUNTER_ONLY(GetPerfCounters().m_Loading.cAssemblies++);

#ifndef CROSSGEN_COMPILE
    if (GetManifestFile()->IsDynamic())
        // manifest modules of dynamic assemblies are always transient
        m_pManifest = ReflectionModule::Create(this, GetManifestFile(), pamTracker, REFEMIT_MANIFEST_MODULE_NAME, TRUE);
    else
#endif
        m_pManifest = Module::Create(this, mdFileNil, GetManifestFile(), pamTracker);

    PrepareModuleForAssembly(m_pManifest, pamTracker);

    CacheManifestFiles();

    if (!m_pManifest->IsReadyToRun())
        CacheManifestExportedTypes(pamTracker);

#if !defined(FEATURE_CORECLR) && !defined(CROSSGEN_COMPILE)
    GenerateBreadcrumbForServicing();

    m_fSupportsAutoNGen = SupportsAutoNGenWorker();

    ReportAssemblyUse();
#endif

#ifdef FEATURE_TRACELOGGING

    TelemetryLogTargetFrameworkAttribute();

#endif // FEATURE_TRACELOGGING


    // Check for the assemblies that contain SIMD Vector types.
    // If we encounter a non-trusted assembly with these names, we will simply not recognize any of its
    // methods as intrinsics.
    LPCUTF8 assemblyName = GetSimpleName();
    const int length = sizeof("System.Numerics") - 1;
    if ((strncmp(assemblyName, "System.Numerics", length) == 0) &&
        ((assemblyName[length] == '\0') || (strcmp(assemblyName+length, ".Vectors") == 0)))
    {
        m_fIsSIMDVectorAssembly = true;
    }
    else
    {
        m_fIsSIMDVectorAssembly = false;
    }

    // We'll load the friend assembly information lazily.  For the ngen case we should avoid
    //  loading it entirely.
    //CacheFriendAssemblyInfo();

    {
        CANNOTTHROWCOMPLUSEXCEPTION();
        FAULT_FORBID();
        //Cannot fail after this point.

        PublishModuleIntoAssembly(m_pManifest);

        return;  // Explicit return to let you know you are NOT welcome to add code after the CANNOTTHROW/FAULT_FORBID expires
    }
}

BOOL Assembly::IsDisabledPrivateReflection()
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    enum { UNINITIALIZED, ENABLED, DISABLED};

    if (m_isDisabledPrivateReflection == UNINITIALIZED)
    {
        IMDInternalImport *pImport = GetManifestImport();
        HRESULT hr = pImport->GetCustomAttributeByName(GetManifestToken(), DISABLED_PRIVATE_REFLECTION_TYPE, NULL, 0);
        IfFailThrow(hr);

        if (hr == S_OK)
        {
            m_isDisabledPrivateReflection = DISABLED;
        }
        else
        {
            m_isDisabledPrivateReflection = ENABLED;
        }
    }

    return m_isDisabledPrivateReflection == DISABLED;
}

#ifndef CROSSGEN_COMPILE
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

    if (m_pFriendAssemblyDescriptor != NULL && m_pFriendAssemblyDescriptor != NO_FRIEND_ASSEMBLIES_MARKER)
        delete m_pFriendAssemblyDescriptor;

    if (m_pbStrongNameKeyPair && (m_FreeFlag & FREE_KEY_PAIR))
        delete[] m_pbStrongNameKeyPair;
    if (m_pwStrongNameKeyContainer && (m_FreeFlag & FREE_KEY_CONTAINER))
        delete[] m_pwStrongNameKeyContainer;

#ifdef FEATURE_MULTIMODULE_ASSEMBLIES
    if (m_pAllowedFiles)
        delete(m_pAllowedFiles);
#endif 
#ifdef FEATURE_FUSION
    if (m_pBindingClosure) 
    {
        m_pBindingClosure->Release();
    }
#endif
    if (IsDynamic()) {
        if (m_pOnDiskManifest)
            // clear the on disk manifest if it is not cleared yet.
            m_pOnDiskManifest = NULL;
    }

    if (m_pManifestFile)
    {
        m_pManifestFile->Release();
    }

#ifdef FEATURE_COMINTEROP
    if (m_pManifestWinMDImport)
    {
        m_pManifestWinMDImport->Release();
    }
#endif // FEATURE_COMINTEROP
}

#ifdef  FEATURE_PREJIT
void Assembly::DeleteNativeCodeRanges()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        FORBID_FAULT;
    }
    CONTRACTL_END

    ModuleIterator i = IterateModules();
    while (i.Next()) 
            i.GetModule()->DeleteNativeCodeRanges();
}
#endif

#ifdef PROFILING_SUPPORTED
void ProfilerCallAssemblyUnloadStarted(Assembly* assemblyUnloaded)
{
    WRAPPER_NO_CONTRACT;
    {
        BEGIN_PIN_PROFILER(CORProfilerPresent());
        GCX_PREEMP();
        g_profControlBlock.pProfInterface->AssemblyUnloadStarted((AssemblyID)assemblyUnloaded);
        END_PIN_PROFILER();
    }
}

void ProfilerCallAssemblyUnloadFinished(Assembly* assemblyUnloaded)
{
    WRAPPER_NO_CONTRACT;
    {
        BEGIN_PIN_PROFILER(CORProfilerPresent());
        GCX_PREEMP();
        g_profControlBlock.pProfInterface->AssemblyUnloadFinished((AssemblyID) assemblyUnloaded, S_OK);
        END_PIN_PROFILER();
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

    // we need to release tlb files eagerly
#ifdef FEATURE_COMINTEROP
    if(g_fProcessDetach == FALSE)
    {
        DefaultCatchFilterParam param; param.pv = COMPLUS_EXCEPTION_EXECUTE_HANDLER;
        PAL_TRY(Assembly *, pThis, this)
        {
            if (pThis->m_pITypeLib && pThis->m_pITypeLib != (ITypeLib*)-1) {
                pThis->m_pITypeLib->Release();
                pThis->m_pITypeLib = NULL;
            }
        }
        PAL_EXCEPT_FILTER(DefaultCatchFilter)
        {
        }
        PAL_ENDTRY
    }
#endif // FEATURE_COMINTEROP

}

void Assembly::Terminate( BOOL signalProfiler )
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;

    STRESS_LOG1(LF_LOADER, LL_INFO100, "Assembly::Terminate (this = 0x%p)\n", reinterpret_cast<void *>(this));

    if (this->m_fTerminated)
        return;
    
    Security::DeleteSharedSecurityDescriptor(m_pSharedSecurityDesc);
    m_pSharedSecurityDesc = NULL;

    if (m_pClassLoader != NULL)
    {
        GCX_PREEMP();
        delete m_pClassLoader;
        m_pClassLoader = NULL;
    }

    if (m_pLoaderAllocator != NULL)
    {
        if (IsCollectible())
        {
            // This cleanup code starts resembling parts of AppDomain::Terminate too much.
            // It would be useful to reduce duplication and also establish clear responsibilites
            // for LoaderAllocator::Destroy, Assembly::Terminate, LoaderAllocator::Terminate
            // and LoaderAllocator::~LoaderAllocator. We need to establish how these
            // cleanup paths interact with app-domain unload and process tear-down, too.

            if (!IsAtProcessExit())
            {
                // Suspend the EE to do some clean up that can only occur
                // while no threads are running.
                GCX_COOP (); // SuspendEE may require current thread to be in Coop mode
                // SuspendEE cares about the reason flag only when invoked for a GC
                // Other values are typically ignored. If using SUSPEND_FOR_APPDOMAIN_SHUTDOWN
                // is inappropriate, we can introduce a new flag or hijack an unused one.
                ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_FOR_APPDOMAIN_SHUTDOWN);
            }

            ExecutionManager::Unload(m_pLoaderAllocator);

            m_pLoaderAllocator->UninitVirtualCallStubManager();
            MethodTable::ClearMethodDataCache();
            _ASSERTE(m_pDomain->IsAppDomain());
            AppDomain *pAppDomain = m_pDomain->AsAppDomain();
            ClearJitGenericHandleCache(pAppDomain);

            if (!IsAtProcessExit())
            {
                // Resume the EE.
                ThreadSuspend::RestartEE(FALSE, TRUE);
            }
            
            // Once the manifest file is tenured, the managed LoaderAllocatorScout is responsible for cleanup.
            if (m_pManifest != NULL && m_pManifest->IsTenured())
            {
                pAppDomain->RegisterLoaderAllocatorForDeletion(m_pLoaderAllocator);
            }
        }
        m_pLoaderAllocator = NULL;
    }

    COUNTER_ONLY(GetPerfCounters().m_Loading.cAssemblies--);


#ifdef PROFILING_SUPPORTED
    if (CORProfilerTrackAssemblyLoads())
    {
        ProfilerCallAssemblyUnloadFinished(this);
    }    
#endif // PROFILING_SUPPORTED

    this->m_fTerminated = TRUE;
}
#endif // CROSSGEN_COMPILE

Assembly * Assembly::Create(
    BaseDomain *                 pDomain, 
    PEAssembly *                 pFile, 
    DebuggerAssemblyControlFlags debuggerFlags, 
    BOOL                         fIsCollectible, 
    AllocMemTracker *            pamTracker, 
    LoaderAllocator *            pLoaderAllocator)
{
    STANDARD_VM_CONTRACT;

    NewHolder<Assembly> pAssembly (new Assembly(pDomain, pFile, debuggerFlags, fIsCollectible));

    // If there are problems that arise from this call stack, we'll chew up a lot of stack
    // with the various EX_TRY/EX_HOOKs that we will encounter.
    INTERIOR_STACK_PROBE_FOR(GetThread(), DEFAULT_ENTRY_PROBE_SIZE); 
#ifdef PROFILING_SUPPORTED
    {
        BEGIN_PIN_PROFILER(CORProfilerTrackAssemblyLoads());
        GCX_COOP();
        g_profControlBlock.pProfInterface->AssemblyLoadStarted((AssemblyID)(Assembly *) pAssembly);
        END_PIN_PROFILER();
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
            BEGIN_PIN_PROFILER(CORProfilerTrackAssemblyLoads());
            GCX_COOP();
            g_profControlBlock.pProfInterface->AssemblyLoadFinished((AssemblyID)(Assembly *) pAssembly,
                                                                    GET_EXCEPTION()->GetHR());
            END_PIN_PROFILER();
        }
    }
    EX_END_HOOK;
#endif
    pAssembly.SuppressRelease();
    END_INTERIOR_STACK_PROBE;
    
    return pAssembly;
} // Assembly::Create


#ifndef CROSSGEN_COMPILE
Assembly *Assembly::CreateDynamic(AppDomain *pDomain, CreateDynamicAssemblyArgs *args)
{
    // WARNING: not backout clean
    CONTRACT(Assembly *)
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(args));
    }
    CONTRACT_END;
    
    // This must be before creation of the AllocMemTracker so that the destructor for the AllocMemTracker happens before the destructor for pLoaderAllocator.
    // That is necessary as the allocation of Assembly objects and other related details is done on top of heaps located in
    // the loader allocator objects.
    NewHolder<LoaderAllocator> pLoaderAllocator;

    AllocMemTracker amTracker;
    AllocMemTracker *pamTracker = &amTracker;
    
    Assembly *pRetVal = NULL;
    
    AppDomain  *pCallersDomain;
    MethodDesc *pmdEmitter = SystemDomain::GetCallersMethod(args->stackMark, &pCallersDomain);

    // Called either from interop or async delegate invocation. Rejecting because we don't
    // know how to set the correct permission on the new dynamic assembly.
    if (!pmdEmitter)
        COMPlusThrow(kInvalidOperationException);

    Assembly   *pCallerAssembly = pmdEmitter->GetAssembly();
    
    // First, we set up a pseudo-manifest file for the assembly.
    
    // Set up the assembly name
    
    STRINGREF strRefName = (STRINGREF) args->assemblyName->GetSimpleName();
    
    if (strRefName == NULL)
        COMPlusThrow(kArgumentException, W("ArgumentNull_AssemblyNameName"));
    
    StackSString name;
    strRefName->GetSString(name);
    
    if (name.GetCount() == 0)
        COMPlusThrow(kArgumentException, W("ArgumentNull_AssemblyNameName"));
    
    SString::Iterator i = name.Begin();
    if (COMCharacter::nativeIsWhiteSpace(*i)
        || name.Find(i, '\\')
        || name.Find(i, ':')
        || name.Find(i, '/'))
    {
        COMPlusThrow(kArgumentException, W("Argument_InvalidAssemblyName"));
    }
    
    // Set up the assembly manifest metadata
    // When we create dynamic assembly, we always use a working copy of IMetaDataAssemblyEmit
    // to store temporary runtime assembly information. This is to preserve the invariant that
    // an assembly must have a PEFile with proper metadata.
    // This working copy of IMetaDataAssemblyEmit will store every AssemblyRef as a simple name
    // reference as we must have an instance of Assembly(can be dynamic assembly) before we can
    // add such a reference. Also because the referenced assembly if dynamic strong name, it may
    // not be ready to be hashed!
    
    SafeComHolder<IMetaDataAssemblyEmit> pAssemblyEmit;
    PEFile::DefineEmitScope(
        IID_IMetaDataAssemblyEmit, 
        &pAssemblyEmit);
    
    // remember the hash algorithm
    ULONG ulHashAlgId = args->assemblyName->GetAssemblyHashAlgorithm();
    if (ulHashAlgId == 0)
        ulHashAlgId = CALG_SHA1;
    
    ASSEMBLYMETADATA assemData;
    memset(&assemData, 0, sizeof(assemData));
    
    // get the version info (default to 0.0.0.0 if none)
    VERSIONREF versionRef = (VERSIONREF) args->assemblyName->GetVersion();
    if (versionRef != NULL)
    {
        assemData.usMajorVersion = (USHORT)versionRef->GetMajor();
        assemData.usMinorVersion = (USHORT)versionRef->GetMinor();
        assemData.usBuildNumber = (USHORT)versionRef->GetBuild();
        assemData.usRevisionNumber = (USHORT)versionRef->GetRevision();
    }
    
    struct _gc
    {
        OBJECTREF granted;
        OBJECTREF denied;
        OBJECTREF cultureinfo;
        STRINGREF pString;
        OBJECTREF orArrayOrContainer;
        OBJECTREF throwable;
        OBJECTREF strongNameKeyPair;
    } gc;
    ZeroMemory(&gc, sizeof(gc));
    
    GCPROTECT_BEGIN(gc);
    
    StackSString culture;
    
    gc.cultureinfo = args->assemblyName->GetCultureInfo();
    if (gc.cultureinfo != NULL)
    {
        MethodDescCallSite getName(METHOD__CULTURE_INFO__GET_NAME, &gc.cultureinfo);
        
        ARG_SLOT args2[] = 
        {
            ObjToArgSlot(gc.cultureinfo)
        };
        
        // convert culture info into a managed string form
        gc.pString = getName.Call_RetSTRINGREF(args2);
        gc.pString->GetSString(culture);
        
        assemData.szLocale = (LPWSTR) (LPCWSTR) culture;
    }
    
    SBuffer publicKey;
    if (args->assemblyName->GetPublicKey() != NULL)
    {
        publicKey.Set(args->assemblyName->GetPublicKey()->GetDataPtr(),
                      args->assemblyName->GetPublicKey()->GetNumComponents());
    }
    

    // get flags
    DWORD dwFlags = args->assemblyName->GetFlags();
    
    // Now create a dynamic PE file out of the name & metadata
    PEAssemblyHolder pFile;

    {
        GCX_PREEMP();

        mdAssembly ma;
        IfFailThrow(pAssemblyEmit->DefineAssembly(publicKey, publicKey.GetSize(), ulHashAlgId,
                                                   name, &assemData, dwFlags,
                                                   &ma));
        pFile = PEAssembly::Create(pCallerAssembly->GetManifestFile(), pAssemblyEmit, args->access & ASSEMBLY_ACCESS_REFLECTION_ONLY);

#if defined(FEATURE_HOST_ASSEMBLY_RESOLVER)
        // Dynamically created modules (aka RefEmit assemblies) do not have a LoadContext associated with them since they are not bound
        // using an actual binder. As a result, we will assume the same binding/loadcontext information for the dynamic assembly as its
        // caller/creator to ensure that any assembly loads triggered by the dynamic assembly are resolved using the intended load context.
        //
        // If the creator assembly has a HostAssembly associated with it, then use it for binding. Otherwise, ithe creator is dynamic
        // and will have a fallback load context binder associated with it.
        ICLRPrivBinder *pFallbackLoadContextBinder = nullptr;
        
        // There is always a manifest file - wehther working with static or dynamic assemblies.
        PEFile *pCallerAssemblyManifestFile = pCallerAssembly->GetManifestFile();
        _ASSERTE(pCallerAssemblyManifestFile != NULL);

        if (!pCallerAssemblyManifestFile->IsDynamic())
        {
            // Static assemblies with do not have fallback load context
            _ASSERTE(pCallerAssemblyManifestFile->GetFallbackLoadContextBinder() == nullptr);

            if (pCallerAssemblyManifestFile->IsSystem())
            {
                // CoreLibrary is always bound to TPA binder
                pFallbackLoadContextBinder = pDomain->GetTPABinderContext();
            }
            else
            {
                // Fetch the binder from the host assembly
                PTR_ICLRPrivAssembly pCallerAssemblyHostAssembly = pCallerAssemblyManifestFile->GetHostAssembly();
                _ASSERTE(pCallerAssemblyHostAssembly != nullptr);

                UINT_PTR assemblyBinderID = 0;
                IfFailThrow(pCallerAssemblyHostAssembly->GetBinderID(&assemblyBinderID));
                pFallbackLoadContextBinder = reinterpret_cast<ICLRPrivBinder *>(assemblyBinderID);
            }
        }
        else
        {
            // Creator assembly is dynamic too, so use its fallback load context for the one
            // we are creating.
            pFallbackLoadContextBinder = pCallerAssemblyManifestFile->GetFallbackLoadContextBinder(); 
        }

        // At this point, we should have a fallback load context binder to work with
        _ASSERTE(pFallbackLoadContextBinder != nullptr);

        // Set it as the fallback load context binder for the dynamic assembly being created
        pFile->SetFallbackLoadContextBinder(pFallbackLoadContextBinder);
#endif // defined(FEATURE_HOST_ASSEMBLY_RESOLVER)

    }            
    
    AssemblyLoadSecurity loadSecurity;
#ifndef FEATURE_CORECLR
    DWORD dwSpecialFlags = 0xFFFFFFFF;
    
    // Don't bother with setting up permissions if this isn't allowed to run
    // This doesn't apply in CoreCLR because you cannot specify evidence when creating a dynamic assembly
    if ((args->identity != NULL) &&
        (args->access & ASSEMBLY_ACCESS_RUN))
    {
        loadSecurity.m_pAdditionalEvidence = &args->identity;
    }
    else
    {
        if (pCallerAssembly != NULL) // can be null if caller is interop
        {
            if (args->securityContextSource == kCurrentAssembly)
            {
                IAssemblySecurityDescriptor *pCallerSecDesc = pCallerAssembly->GetSecurityDescriptor(pCallersDomain);
                gc.granted = pCallerSecDesc->GetGrantedPermissionSet(&(gc.denied));
                dwSpecialFlags = pCallerSecDesc->GetSpecialFlags();
            }
            else
            {
                IApplicationSecurityDescriptor *pCallersDomainSecDesc = pCallersDomain->GetSecurityDescriptor();

#ifdef FEATURE_CAS_POLICY
                // We only want to propigate the identity of homogenous domains, since heterogenous domains tend
                // to be fully trusted even if they are housing partially trusted code - which could lead to an
                // elevation of privilege if we allow the grant set to be pushed to assemblies partially trusted
                // code is loading.
                if (!pCallersDomainSecDesc->IsHomogeneous())
                {
                    COMPlusThrow(kNotSupportedException, W("NotSupported_SecurityContextSourceAppDomainInHeterogenous"));
                }
#endif // FEATURE_CAS_POLICY

                gc.granted = pCallersDomainSecDesc->GetGrantedPermissionSet();
                dwSpecialFlags = pCallersDomainSecDesc->GetSpecialFlags();
            }

            // Caller may be in another appdomain context, in which case we'll
            // need to marshal/unmarshal the grant and deny sets across.
#ifdef FEATURE_REMOTING  // should not happen without remoting              
            if (pCallersDomain != ::GetAppDomain())
            {
                gc.granted = AppDomainHelper::CrossContextCopyFrom(pCallersDomain->GetId(), &(gc.granted));
                if (gc.denied != NULL)
                {
                    gc.denied = AppDomainHelper::CrossContextCopyFrom(pCallersDomain->GetId(), &(gc.denied));
                }
            }
#else // !FEATURE_REMOTING
            _ASSERTE(pCallersDomain == ::GetAppDomain());
#endif // FEATURE_REMOTING
        }
    }
#else // FEATURE_CORECLR
    // In SilverLight all dynamic assemblies should be transparent and partially trusted, even if they are
    // created by platform assemblies. Thus they should inherit the grant sets from the appdomain not the 
    // parent assembly.
    IApplicationSecurityDescriptor *pCurrentDomainSecDesc = ::GetAppDomain()->GetSecurityDescriptor();
    gc.granted = pCurrentDomainSecDesc->GetGrantedPermissionSet();
    DWORD dwSpecialFlags = pCurrentDomainSecDesc->GetSpecialFlags();
#endif // !FEATURE_CORECLR

    // If the dynamic assembly creator did not specify evidence for the newly created assembly, then it
    // should inherit the grant set of the creation assembly.
    if (loadSecurity.m_pAdditionalEvidence == NULL)
    {   
#ifdef FEATURE_CAS_POLICY
        // If we're going to inherit the grant set of an anonymously hosted dynamic method, it will be
        // full trust/transparent. In that case, we should demand full trust.
        if(args->securityContextSource == kCurrentAssembly &&
            pCallerAssembly != NULL &&
            pCallersDomain != NULL &&
            pCallerAssembly->GetDomainAssembly(pCallersDomain) == pCallersDomain->GetAnonymouslyHostedDynamicMethodsAssembly())
        {
            loadSecurity.m_fPropagatingAnonymouslyHostedDynamicMethodGrant = true;
        }
#endif // FEATURE_CAS_POLICY

        loadSecurity.m_pGrantSet = &gc.granted;
        loadSecurity.m_pRefusedSet = &gc.denied;
        loadSecurity.m_dwSpecialFlags = dwSpecialFlags;
    }

    NewHolder<DomainAssembly> pDomainAssembly;

    {
        GCX_PREEMP();

        // Create a new LoaderAllocator if appropriate
        if ((args->access & ASSEMBLY_ACCESS_COLLECT) != 0)
        {
            AssemblyLoaderAllocator *pAssemblyLoaderAllocator = new AssemblyLoaderAllocator();
            pLoaderAllocator = pAssemblyLoaderAllocator;

            // Some of the initialization functions are not virtual. Call through the derived class
            // to prevent calling the base class version.
            pAssemblyLoaderAllocator->Init(pDomain);

            // Setup the managed proxy now, but do not actually transfer ownership to it.
            // Once everything is setup and nothing can fail anymore, the ownership will be
            // atomically transfered by call to LoaderAllocator::ActivateManagedTracking().
            pAssemblyLoaderAllocator->SetupManagedTracking(&args->loaderAllocator);
        }
        else
        {
            pLoaderAllocator = pDomain->GetLoaderAllocator();
            pLoaderAllocator.SuppressRelease();
        }

        // Create a domain assembly
        pDomainAssembly = new DomainAssembly(pDomain, pFile, &loadSecurity, pLoaderAllocator);
    }

    // Start loading process

#ifdef FEATURE_CAS_POLICY
    // Get the security descriptor for the assembly.
    IAssemblySecurityDescriptor *pSecDesc = pDomainAssembly->GetSecurityDescriptor();

    // Propagate identity and permission request information into the assembly's
    // security descriptor. Then when policy is resolved we'll end up with the
    // correct grant set.
    // If identity has not been provided then the caller's assembly will be
    // calculated instead and we'll just copy the granted permissions from the
    // caller to the new assembly and mark policy as resolved (done
    // automatically by SetGrantedPermissionSet).
    pSecDesc->SetRequestedPermissionSet(args->requiredPset,
                                        args->optionalPset,
                                        args->refusedPset);
#endif // FEATURE_CAS_POLICY

    {
        // Create a concrete assembly
        // (!Do not remove scoping brace: order is important here: the Assembly holder must destruct before the AllocMemTracker!)
        NewHolder<Assembly> pAssem;
        
        {
            GCX_PREEMP();
            // Assembly::Create will call SuppressRelease on the NewHolder that holds the LoaderAllocator when it transfers ownership
            pAssem = Assembly::Create(pDomain, pFile, pDomainAssembly->GetDebuggerInfoBits(), args->access & ASSEMBLY_ACCESS_COLLECT ? TRUE : FALSE, pamTracker, pLoaderAllocator);
            
            ReflectionModule* pModule = (ReflectionModule*) pAssem->GetManifestModule();
            pModule->SetCreatingAssembly( pCallerAssembly );


            if ((args->access & ASSEMBLY_ACCESS_COLLECT) != 0)
            {
                // Initializing the virtual call stub manager is delayed to remove the need for the LoaderAllocator destructor to properly handle
                // uninitializing the VSD system. (There is a need to suspend the runtime, and that's tricky)
                pLoaderAllocator->InitVirtualCallStubManager(pDomain, TRUE);
            }
        }

        pAssem->m_isDynamic = true;

        pAssem->m_dwDynamicAssemblyAccess = args->access;

#ifdef FEATURE_CAS_POLICY
        // If a legacy assembly is emitting an assembly, then we implicitly add the legacy attribute. If the legacy
        // assembly is also in partial trust, we implicitly make the emitted assembly transparent.
        ModuleSecurityDescriptor *pEmittingMSD = ModuleSecurityDescriptor::GetModuleSecurityDescriptor(pCallerAssembly);
        if (pEmittingMSD->GetSecurityRuleSet() == SecurityRuleSet_Level1)
        {
            IAssemblySecurityDescriptor *pCallerSecDesc = pCallerAssembly->GetSecurityDescriptor(pCallersDomain);
            if (!pCallerSecDesc->IsFullyTrusted())
            {
                args->flags = kTransparentAssembly;
            }
        }

        // If the code emitting the dynamic assembly is transparent and it is attempting to emit a non-transparent
        // assembly, then we need to do a demand for the grant set of the emitting assembly (which should also be
        // is the grant set of the dynamic assembly).
        if (Security::IsMethodTransparent(pmdEmitter) && !(args->flags & kTransparentAssembly))
        {
            Security::DemandGrantSet(pCallerAssembly->GetSecurityDescriptor(pCallersDomain));
        }
#else // FEATURE_CORECLR
        // Making the dynamic assembly opportunistically critical in full trust CoreCLR and transparent otherwise.
        if (!GetAppDomain()->GetSecurityDescriptor()->IsFullyTrusted())
        {
            args->flags = kTransparentAssembly;
        }
#endif //!FEATURE_CORECLR

        // Fake up a module security descriptor for the assembly.
        TokenSecurityDescriptorFlags tokenFlags = TokenSecurityDescriptorFlags_None;
        if (args->flags & kAllCriticalAssembly)
            tokenFlags |= TokenSecurityDescriptorFlags_AllCritical;
        if (args->flags & kAptcaAssembly)
            tokenFlags |= TokenSecurityDescriptorFlags_APTCA;
        if (args->flags & kCriticalAssembly)
            tokenFlags |= TokenSecurityDescriptorFlags_Critical;
        if (args->flags & kTransparentAssembly)
            tokenFlags |= TokenSecurityDescriptorFlags_Transparent;
        if (args->flags & kTreatAsSafeAssembly)
            tokenFlags |= TokenSecurityDescriptorFlags_TreatAsSafe;

#ifdef FEATURE_APTCA
        if (args->aptcaBlob != NULL)
        {
            tokenFlags |= ParseAptcaAttribute(args->aptcaBlob->GetDirectPointerToNonObjectElements(),
                                              args->aptcaBlob->GetNumComponents());
        }

#endif // FEATURE_APTCA

#ifndef FEATURE_CORECLR
        // Use the security rules given to us if the emitting code has selected a specific one. Otherwise,
        // inherit the security rules of the emitting assembly.
        if (args->securityRulesBlob != NULL)
        {
            tokenFlags |= ParseSecurityRulesAttribute(args->securityRulesBlob->GetDirectPointerToNonObjectElements(),
                                                      args->securityRulesBlob->GetNumComponents());
        }
        else
        {
            // Ensure that dynamic assemblies created by mscorlib always specify a rule set, since we want to
            // make sure that creating a level 2 assembly was an explicit decision by the emitting code,
            // rather than an implicit decision because mscorlib is level 2 itself.
            //
            // If you're seeing this assert, it means that you've created a dynamic assembly from mscorlib,
            // but did not pass a CustomAttributeBuilder for the SecurityRulesAttribute to the 
            // DefineDynamicAssembly call.
           _ASSERTE(!pCallerAssembly->IsSystem());

            // Use the creating assembly's security rule set for the emitted assembly
            SecurityRuleSet callerRuleSet = 
                ModuleSecurityDescriptor::GetModuleSecurityDescriptor(pCallerAssembly)->GetSecurityRuleSet();
            tokenFlags |= EncodeSecurityRuleSet(callerRuleSet);

            tokenFlags |= TokenSecurityDescriptorFlags_SecurityRules;
        }
#endif // !FEATURE_CORECLR

        _ASSERTE(pAssem->GetManifestModule()->m_pModuleSecurityDescriptor != NULL);
        pAssem->GetManifestModule()->m_pModuleSecurityDescriptor->OverrideTokenFlags(tokenFlags); 

        // Set the additional strong name information

        pAssem->SetStrongNameLevel(Assembly::SN_NONE);

        if (publicKey.GetSize() > 0)
        {
            pAssem->SetStrongNameLevel(Assembly::SN_PUBLIC_KEY);
#ifndef FEATURE_CORECLR
            gc.strongNameKeyPair = args->assemblyName->GetStrongNameKeyPair();
            // If there's a public key, there might be a strong name key pair.
            if (gc.strongNameKeyPair != NULL) 
            {
                MethodDescCallSite getKeyPair(METHOD__STRONG_NAME_KEY_PAIR__GET_KEY_PAIR, &gc.strongNameKeyPair);

                ARG_SLOT arglist[] = 
                {
                    ObjToArgSlot(gc.strongNameKeyPair),
                    PtrToArgSlot(&gc.orArrayOrContainer)
                };

                BOOL bKeyInArray;
                bKeyInArray = (BOOL)getKeyPair.Call_RetBool(arglist);

                if (bKeyInArray)
                {
                    U1ARRAYREF orArray = (U1ARRAYREF)gc.orArrayOrContainer;
                    pAssem->m_cbStrongNameKeyPair = orArray->GetNumComponents();
                    pAssem->m_pbStrongNameKeyPair = new BYTE[pAssem->m_cbStrongNameKeyPair];

                    pAssem->m_FreeFlag |= pAssem->FREE_KEY_PAIR;
                    memcpy(pAssem->m_pbStrongNameKeyPair, orArray->GetDataPtr(), pAssem->m_cbStrongNameKeyPair);
                    pAssem->SetStrongNameLevel(Assembly::SN_FULL_KEYPAIR_IN_ARRAY);
                }
                else
                {
                    STRINGREF orContainer = (STRINGREF)gc.orArrayOrContainer;
                    DWORD cchContainer = orContainer->GetStringLength();
                    pAssem->m_pwStrongNameKeyContainer = new WCHAR[cchContainer + 1];

                    pAssem->m_FreeFlag |= pAssem->FREE_KEY_CONTAINER;
                    memcpy(pAssem->m_pwStrongNameKeyContainer, orContainer->GetBuffer(), cchContainer * sizeof(WCHAR));
                    pAssem->m_pwStrongNameKeyContainer[cchContainer] = W('\0');

                    pAssem->SetStrongNameLevel(Assembly::SN_FULL_KEYPAIR_IN_CONTAINER);
                }
            }
            else
#endif // FEATURE_CORECLR
            {
                // Since we have no way to validate the public key of a dynamic assembly we don't allow 
                // partial trust code to emit a dynamic assembly with an arbitrary public key.
                // Ideally we shouldn't allow anyone to emit a dynamic assembly with only a public key,
                // but we allow a couple of exceptions to reduce the compat risk: full trust, caller's own key.
                // As usual we treat anonymously hosted dynamic methods as partial trust code.
                DomainAssembly* pCallerDomainAssembly = pCallerAssembly->GetDomainAssembly(pCallersDomain);
                if (!pCallerDomainAssembly->GetSecurityDescriptor()->IsFullyTrusted() ||
                    pCallerDomainAssembly == pCallersDomain->GetAnonymouslyHostedDynamicMethodsAssembly())
                {
                    DWORD cbKey = 0;
                    const void* pKey = pCallerAssembly->GetPublicKey(&cbKey);

                    if (!publicKey.Equals((const BYTE *)pKey, cbKey))
                        COMPlusThrow(kInvalidOperationException, W("InvalidOperation_StrongNameKeyPairRequired"));
                }
            }
        }

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

        // Force the transparency of the module to be computed now, so that we can catch any errors due to
        // inconsistent assembly level attributes during the assembly creation call, rather than at some
        // later point.
        pAssem->GetManifestModule()->m_pModuleSecurityDescriptor->VerifyDataComputed();

        {
            CANNOTTHROWCOMPLUSEXCEPTION();
            FAULT_FORBID();

            //Cannot fail after this point

            pDomainAssembly.SuppressRelease(); // This also effectively suppresses the release of the pAssem 
            pamTracker->SuppressRelease();

            // Once we reach this point, the loader allocator lifetime is controlled by the Assembly object.
            if ((args->access & ASSEMBLY_ACCESS_COLLECT) != 0)
            {
                // Atomically transfer ownership to the managed heap
                pLoaderAllocator->ActivateManagedTracking();
                pLoaderAllocator.SuppressRelease();
            }

            pAssem->SetIsTenured();
            pRetVal = pAssem;
        }
    }
    GCPROTECT_END();

    RETURN pRetVal;
} // Assembly::CreateDynamic

#ifdef FEATURE_MULTIMODULE_ASSEMBLIES
ReflectionModule *Assembly::CreateDynamicModule(LPCWSTR wszModuleName, LPCWSTR wszFileName, BOOL fIsTransient, INT32* ptkFile)
{
    CONTRACT(ReflectionModule *)
    {
        STANDARD_VM_CHECK;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;
    
    AllocMemTracker amTracker;
    
    // Add a manifest entry for the module
    mdFile token;
    IMetaDataAssemblyEmit *pAssemblyEmit = GetManifestFile()->GetAssemblyEmitter();
    IfFailThrow(pAssemblyEmit->DefineFile(wszFileName, NULL, 0, 0, &token));

    if (ptkFile)
        *ptkFile = (INT32)token;
    
    GetManifestModule()->UpdateDynamicMetadataIfNeeded();
    
    // Define initial metadata for the module
    SafeComHolder<IMetaDataEmit> pEmit;
    PEFile::DefineEmitScope(IID_IMetaDataEmit, (void **)&pEmit);
    
    // the module name will be set later when we create the ReflectionModule
    
    // Create the PEFile for the module
    PEModuleHolder pFile(PEModule::Create(GetManifestFile(), token, pEmit));
    
    // Create the DomainModule
    NewHolder<DomainModule> pDomainModule(new DomainModule(::GetAppDomain(), GetDomainAssembly(), pFile));
    
    // Create the module itself
    ReflectionModuleHolder pWrite(ReflectionModule::Create(this, pFile, &amTracker, wszModuleName, fIsTransient));
    
    amTracker.SuppressRelease(); //@todo: OOM: is this the right place to commit the tracker?
    pWrite->SetIsTenured();
    
    // Modules take the DebuggerAssemblyControlFlags down from its parent Assembly initially.
    // By default, this turns on JIT optimization.
    
    pWrite->SetDebuggerInfoBits(GetDebuggerInfoBits());
    
    // Associate the two
    pDomainModule->SetModule(pWrite);
    m_pManifest->StoreFileThrowing(token, pWrite);
    
    // Simulate loading process
    pDomainModule->Begin();
    pDomainModule->DeliverSyncEvents();
    pDomainModule->DeliverAsyncEvents();
    pDomainModule->FinishLoad();
    pDomainModule->ClearLoading();
    pDomainModule->m_level = FILE_ACTIVE;
    
    pDomainModule.SuppressRelease();
    ReflectionModule *pModule = pWrite.Extract();

    LPCSTR szUTF8FileName;
    CQuickBytes qbLC;

    // Get the UTF8 file name
    IfFailThrow(m_pManifest->GetMDImport()->GetFileProps(token, &szUTF8FileName, NULL, NULL, NULL));
    UTF8_TO_LOWER_CASE(szUTF8FileName, qbLC);
    LPCSTR szUTF8FileNameLower = (LPUTF8) qbLC.Ptr();

    CrstHolder lock(&m_crstAllowedFiles);

    // insert the value into manifest's look up table.
    // Need to perform case insensitive hashing as well.    
    m_pAllowedFiles->InsertValue(szUTF8FileName, (HashDatum)(size_t)token, TRUE);
    m_pAllowedFiles->InsertValue(szUTF8FileNameLower, (HashDatum)(size_t)token, TRUE);
    
    // Now make file token associate with the loaded module
    m_pManifest->StoreFileThrowing(token, pModule);

    RETURN pModule;
} // Assembly::CreateDynamicModule
#endif //  FEATURE_MULTIMODULE_ASSEMBLIES

#endif // CROSSGEN_COMPILE

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

    GetManifestModule()->SetDomainFile(pDomainAssembly);

    IAssemblySecurityDescriptor *pSec = pDomainAssembly->GetSecurityDescriptor();

    GCX_COOP();
    pSec->ResolvePolicy(GetSharedSecurityDescriptor(), pDomainAssembly->ShouldSkipPolicyResolution());

} // Assembly::SetDomainAssembly

#endif // #ifndef DACCESS_COMPILE

DomainAssembly *Assembly::GetDomainAssembly(AppDomain *pDomain)
{
    CONTRACT(DomainAssembly *)
    {
        PRECONDITION(CheckPointer(pDomain, NULL_NOT_OK));
        POSTCONDITION(CheckPointer(RETVAL));
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACT_END;

    RETURN GetManifestModule()->GetDomainAssembly(pDomain);
}

DomainAssembly *Assembly::FindDomainAssembly(AppDomain *pDomain)
{
    CONTRACT(DomainAssembly *)
    {
        PRECONDITION(CheckPointer(pDomain));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SO_TOLERANT;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    PREFIX_ASSUME (GetManifestModule() !=NULL); 
    RETURN GetManifestModule()->FindDomainAssembly(pDomain);
}

BOOL Assembly::IsIntrospectionOnly()
{
    WRAPPER_NO_CONTRACT;
    return m_pManifestFile->IsIntrospectionOnly();
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


PTR_BaseDomain Assembly::GetDomain()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    _ASSERTE(m_pDomain);
    return (m_pDomain);
}
IAssemblySecurityDescriptor *Assembly::GetSecurityDescriptor(AppDomain *pDomain)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END

    IAssemblySecurityDescriptor* pSecDesc;

    if (pDomain == NULL)
    {
#ifndef DACCESS_COMPILE
        pDomain = ::GetAppDomain();
#else //DACCESS_COMPILE
        DacNotImpl();
#endif //DACCESS_COMPILE
    }

    PREFIX_ASSUME(FindDomainAssembly(pDomain) != NULL);
    pSecDesc = FindDomainAssembly(pDomain)->GetSecurityDescriptor();

    CONSISTENCY_CHECK(pSecDesc != NULL);

    return pSecDesc;
}

#ifndef DACCESS_COMPILE

const SecurityTransparencyBehavior *Assembly::GetSecurityTransparencyBehavior()
{
    CONTRACT(const SecurityTransparencyBehavior *)
    {
        THROWS;
        GC_TRIGGERS;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    if (m_pTransparencyBehavior == NULL)
    {
        ModuleSecurityDescriptor *pModuleSecurityDescriptor = ModuleSecurityDescriptor::GetModuleSecurityDescriptor(this);
        SetSecurityTransparencyBehavior(SecurityTransparencyBehavior::GetTransparencyBehavior(pModuleSecurityDescriptor->GetSecurityRuleSet()));
    }

    RETURN(m_pTransparencyBehavior);
}

// This method is like GetTransparencyBehavior, but will not attempt to get the transparency behavior if we
// don't already know it, and therefore may return NULL
const SecurityTransparencyBehavior *Assembly::TryGetSecurityTransparencyBehavior()
{
    LIMITED_METHOD_CONTRACT;
    return m_pTransparencyBehavior;
}


// The transparency behavior object passed to this method must have a lifetime of at least as long
// as the assembly itself.
void Assembly::SetSecurityTransparencyBehavior(const SecurityTransparencyBehavior *pTransparencyBehavior)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pTransparencyBehavior));
        PRECONDITION(m_pTransparencyBehavior == NULL || m_pTransparencyBehavior == pTransparencyBehavior);
    }
    CONTRACTL_END;

    m_pTransparencyBehavior = pTransparencyBehavior;
}

void Assembly::SetParent(BaseDomain* pParent)
{
    LIMITED_METHOD_CONTRACT;

    m_pDomain = pParent;
}

#endif // !DACCCESS_COMPILE

mdFile Assembly::GetManifestFileToken(LPCSTR name)
{

#ifdef FEATURE_MULTIMODULE_ASSEMBLIES
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    HashDatum datum;
    // Note: We're doing a case sensitive lookup
    // This is OK because the lookup string and the string we insert into the hashtable
    // are obtained from the same place.

    // m_pAllowedFiles only grows - entries are never deleted from it. So we do not take
    // a lock around GetValue. If the code is modified such that we delete entries from m_pAllowedFiles,
    // reconsider whether the callers that consume the mdFile should take the m_crstAllowedFiles lock.
    if (m_pAllowedFiles->GetValue(name, &datum)) {

        if (datum != NULL) // internal module
            return (mdFile)(size_t)PTR_TO_TADDR(datum);
        else // manifest file
            return mdFileNil;
    }
    else
        return mdTokenNil; // not found
#else
    return mdFileNil;
#endif 
}

mdFile Assembly::GetManifestFileToken(IMDInternalImport *pImport, mdFile kFile)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    LPCSTR name;
    if ((TypeFromToken(kFile) != mdtFile) || 
        !pImport->IsValidToken(kFile))
    {
        BAD_FORMAT_NOTHROW_ASSERT(!"Invalid File token");
        return mdTokenNil;
    }
    
    if (FAILED(pImport->GetFileProps(kFile, &name, NULL, NULL, NULL)))
    {
        BAD_FORMAT_NOTHROW_ASSERT(!"Invalid File token");
        return mdTokenNil;
    }
    
    return GetManifestFileToken(name);
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
    
    IMDInternalImport *pManifestImport = GetManifestImport();
    
    IfFailThrow(pManifestImport->GetExportedTypeProps(
        mdType, 
        NULL, 
        NULL, 
        &mdLinkRef,     // Impl
        &mdBinding,     // Hint
        NULL));         // dwflags
    
    // Don't trust the returned tokens.
    if (!pManifestImport->IsValidToken(mdLinkRef))
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
                        GetManifestModule()->LoadAssembly(::GetAppDomain(), mdLinkRef);
                    PREFIX_ASSUME(pDomainAssembly != NULL);

                    RETURN pDomainAssembly->GetCurrentModule();
#else
                    _ASSERTE(!"DAC shouldn't attempt to trigger loading");
                    return NULL;
#endif // !DACCESS_COMPILE
                };
                case Loader::DontLoad: 
                    pAssembly = GetManifestModule()->GetAssemblyIfLoaded(mdLinkRef);
                    break;
                case Loader::SafeLookup:
                    pAssembly = GetManifestModule()->LookupAssemblyRef(mdLinkRef);
                    break;
                default:
                    _ASSERTE(FALSE);
            }  
            
            if (pAssembly)
                RETURN pAssembly->GetManifestModule();
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
            Module *pModule = GetManifestModule()->GetModuleIfLoaded(mdLinkRef, TRUE, FALSE);
#ifdef DACCESS_COMPILE
            return pModule;
#else
            if (pModule != NULL)
                RETURN pModule;

            if(loadFlag==Loader::SafeLookup)
                return NULL;

            // We should never get here in the GC case - the above should have succeeded.
            CONSISTENCY_CHECK(!FORBIDGC_LOADER_USE_ENABLED());

            DomainFile * pDomainModule = GetManifestModule()->LoadModule(::GetAppDomain(), mdLinkRef, FALSE, loadFlag!=Loader::Load);

            if (pDomainModule == NULL)
                RETURN NULL;
            else
            {
                pModule = pDomainModule->GetCurrentModule();
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
    Module *         pModule, 
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
                RETURN(pModule);
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
            // Type is in the referencing module.
            GCX_NOTRIGGER();
            CANNOTTHROWCOMPLUSEXCEPTION();
            RETURN( pModule );
        }

        case mdtModuleRef:
        {
            if ((loadFlag != Loader::Load) || IsGCThread() || IsStackWalkerThread())
            {
                // Either we're not supposed to load, or we're doing a GC or stackwalk
                // in which case we shouldn't need to load.  So just look up the module
                // and return what we find.
                RETURN(pModule->LookupModule(tkType,FALSE));
            }
            
#ifndef DACCESS_COMPILE
            DomainFile * pActualDomainFile = pModule->LoadModule(::GetAppDomain(), tkType, FALSE, loadFlag!=Loader::Load);
            if (pActualDomainFile == NULL)
            {
                RETURN NULL;
            }
            else
            {
                RETURN(pActualDomainFile->GetModule());
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
            // Do this first because it has a strong contract
            Assembly * pAssembly = NULL;
            
#if defined(FEATURE_COMINTEROP) || !defined(DACCESS_COMPILE)
            LPCUTF8 szNamespace = NULL;
            LPCUTF8 szClassName = NULL;
#endif
            
#ifdef FEATURE_COMINTEROP
            if (pModule->HasBindableIdentity(tkType))
#endif// FEATURE_COMINTEROP
            {
                _ASSERTE(!IsAfContentType_WindowsRuntime(pModule->GetAssemblyRefFlags(tkType)));
                if (loadFlag == Loader::SafeLookup)
                {
                    pAssembly = pModule->LookupAssemblyRef(tkType);
                }
                else
                {
                    pAssembly = pModule->GetAssemblyIfLoaded(tkType);
                }
            }
#ifdef FEATURE_COMINTEROP
            else
            {
                _ASSERTE(IsAfContentType_WindowsRuntime(pModule->GetAssemblyRefFlags(tkType)));
                
                if (FAILED(pImport->GetNameOfTypeRef(
                    tkTopLevelEncloserTypeRef, 
                    &szNamespace, 
                    &szClassName)))
                {
                    THROW_BAD_FORMAT(BFA_BAD_TYPEREF_TOKEN, pModule);
                }
                
                pAssembly = pModule->GetAssemblyIfLoaded(
                        tkType, 
                        szNamespace, 
                        szClassName, 
                        NULL);  // pMDImportOverride                
            }
#endif // FEATURE_COMINTEROP
            
            if (pAssembly != NULL)
            {
                RETURN pAssembly->m_pManifest;
            }

#ifdef DACCESS_COMPILE
            RETURN NULL;
#else
            if (loadFlag != Loader::Load)
            {
                RETURN NULL;
            }

#ifndef FEATURE_CORECLR
            // Event Tracing for Windows is used to log data for performance and functional testing purposes.
            // The events below are used to help measure the performance of assembly loading of a static reference.
            FireEtwLoaderPhaseStart((::GetAppDomain() ? ::GetAppDomain()->GetId().m_dwId : ETWAppDomainIdNotAvailable), ETWLoadContextNotAvailable, ETWFieldUnused, ETWLoaderStaticLoad, NULL, NULL, GetClrInstanceId());
#endif //!FEATURE_CORECLR
            
            DomainAssembly * pDomainAssembly = pModule->LoadAssembly(
                    ::GetAppDomain(), 
                    tkType, 
                    szNamespace, 
                    szClassName);

#ifndef FEATURE_CORECLR
            if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, TRACE_LEVEL_INFORMATION, CLR_PRIVATEBINDING_KEYWORD))
            {
                StackSString assemblySimpleName;
                EX_TRY
                {
                    if ((pDomainAssembly != NULL) && (pDomainAssembly->GetCurrentAssembly() != NULL))
                    {
                        assemblySimpleName.AppendUTF8(pDomainAssembly->GetCurrentAssembly()->GetSimpleName());
                        assemblySimpleName.Normalize(); // Ensures that the later cast to LPCWSTR does not throw.
                    }
                }
                EX_CATCH
                {
                    assemblySimpleName.Clear();
                }
                EX_END_CATCH(RethrowTransientExceptions)
                
                FireEtwLoaderPhaseEnd(::GetAppDomain() ? ::GetAppDomain()->GetId().m_dwId : ETWAppDomainIdNotAvailable, ETWLoadContextNotAvailable, ETWFieldUnused, ETWLoaderStaticLoad, NULL, assemblySimpleName.IsEmpty() ? NULL : (LPCWSTR)assemblySimpleName, GetClrInstanceId());
            }
#endif //!FEATURE_CORECLR

            if (pDomainAssembly == NULL)
                RETURN NULL;
            
            pAssembly = pDomainAssembly->GetCurrentAssembly();
            if (pAssembly == NULL)
            {
                RETURN NULL;
            }
            else
            {
                RETURN pAssembly->m_pManifest;
            }
#endif //!DACCESS_COMPILE
        }

    default:
        ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_TOKEN_TYPE);
    }
} // Assembly::FindModuleByTypeRef

#ifndef DACCESS_COMPILE

Module *Assembly::FindModuleByName(LPCSTR pszModuleName)
{
    CONTRACT(Module *)
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    CQuickBytes qbLC;

    // Need to perform case insensitive hashing.
    UTF8_TO_LOWER_CASE(pszModuleName, qbLC);
    pszModuleName = (LPUTF8) qbLC.Ptr();

    mdFile kFile = GetManifestFileToken(pszModuleName);
    if (kFile == mdTokenNil)
        ThrowHR(COR_E_UNAUTHORIZEDACCESS);

    if (this == SystemDomain::SystemAssembly())
        RETURN m_pManifest->GetModuleIfLoaded(kFile, TRUE, TRUE);
    else
        RETURN m_pManifest->LoadModule(::GetAppDomain(), kFile)->GetModule();
}

void Assembly::CacheManifestExportedTypes(AllocMemTracker *pamTracker)
{
    CONTRACT_VOID
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    // Prejitted assemblies are expected to have their table prebuilt.
    // If not, we do it here at load time (as if we would jit the assembly).

    if (m_pManifest->IsPersistedObject(m_pManifest->m_pAvailableClasses))
        RETURN;

    mdToken mdExportedType;

    HENUMInternalHolder phEnum(GetManifestImport());
    phEnum.EnumInit(mdtExportedType,
                    mdTokenNil);

    ClassLoader::AvailableClasses_LockHolder lh(m_pClassLoader);

    for(int i = 0; GetManifestImport()->EnumNext(&phEnum, &mdExportedType); i++)
        m_pClassLoader->AddExportedTypeHaveLock(GetManifestModule(),
                                                mdExportedType,
                                                pamTracker);

    RETURN;
}
void Assembly::CacheManifestFiles()
{
#ifdef FEATURE_MULTIMODULE_ASSEMBLIES
    CONTRACT_VOID
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    mdToken tkFile;
    LPCSTR pszFileName;
    CQuickBytes qbLC;
    
    HENUMInternalHolder phEnum(GetManifestImport());
    phEnum.EnumInit(mdtFile,
                    mdTokenNil);
    
    
    DWORD dwCount = GetManifestImport()->EnumGetCount(&phEnum);
    LockOwner lockOwner  = { &m_crstAllowedFiles, IsOwnerOfCrst };
    if (!m_pAllowedFiles->Init(dwCount+1, &lockOwner))
        ThrowOutOfMemory();

    CrstHolder lock(&m_crstAllowedFiles);

    m_nextAvailableModuleIndex = dwCount+1;
    
    while (GetManifestImport()->EnumNext(&phEnum, &tkFile))
    {
        if (TypeFromToken(tkFile) == mdtFile)
        {
            IfFailThrow(GetManifestImport()->GetFileProps(
                tkFile, 
                &pszFileName, 
                NULL,           // hash
                NULL,           // hash len
                NULL));         // flags

            // Add to hash table
            m_pAllowedFiles->InsertValue(pszFileName, (HashDatum)(size_t)tkFile, TRUE);
            
            // Need to perform case insensitive hashing as well.
            {
                UTF8_TO_LOWER_CASE(pszFileName, qbLC);
                pszFileName = (LPUTF8) qbLC.Ptr();
            }
            
            // Add each internal module
            m_pAllowedFiles->InsertValue(pszFileName, (HashDatum)(size_t)tkFile, TRUE);
        }
    }
    
    HENUMInternalHolder phEnumModules(GetManifestImport());
    phEnumModules.EnumInit(mdtModuleRef, mdTokenNil);
    mdToken tkModuleRef;
    
    while (GetManifestImport()->EnumNext(&phEnumModules, &tkModuleRef))
    {
        LPCSTR pszModuleRefName, pszModuleRefNameLower;
        
        if (TypeFromToken(tkModuleRef) == mdtModuleRef)
        {
            IfFailThrow(GetManifestImport()->GetModuleRefProps(tkModuleRef, &pszModuleRefName));
            
            // Convert to lower case and lookup
            {
                UTF8_TO_LOWER_CASE(pszModuleRefName, qbLC);
                pszModuleRefNameLower = (LPUTF8) qbLC.Ptr();
            }

            HashDatum datum;
            if (m_pAllowedFiles->GetValue(pszModuleRefNameLower, &datum))
            {
                mdFile tkFileForModuleRef = (mdFile)(size_t)datum;
                m_pAllowedFiles->InsertValue(pszModuleRefName, (HashDatum)(size_t)tkFileForModuleRef);
            }
        }
    }
    
    // Add the manifest file
    if (!GetManifestImport()->IsValidToken(GetManifestImport()->GetModuleFromScope()))
    {
        ThrowHR(COR_E_BADIMAGEFORMAT);
    }
    IfFailThrow(GetManifestImport()->GetScopeProps(&pszFileName, NULL));

    // Add to hash table
    m_pAllowedFiles->InsertValue(pszFileName, NULL, TRUE);
    
    // Need to perform case insensitive hashing as well.
    {
        UTF8_TO_LOWER_CASE(pszFileName, qbLC);
        pszFileName = (LPUTF8) qbLC.Ptr();
    }
    
    m_pAllowedFiles->InsertValue(pszFileName, NULL, TRUE);
    
    RETURN;
#endif
}


//<TODO>@TODO: if module is not signed it needs to acquire the
//permissions from the assembly.</TODO>
void Assembly::PrepareModuleForAssembly(Module* module, AllocMemTracker *pamTracker)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(CheckPointer(module));
    }
    CONTRACTL_END;
    
    if (module->m_pAvailableClasses != NULL && !module->IsPersistedObject(module->m_pAvailableClasses)) 
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
         module->GetFile()->GetSimpleName(),
         module->GetDebuggerInfoBits()));
#endif // DEBUGGING_SUPPORTED

    m_pManifest->EnsureFileCanBeStored(module->GetModuleRef());
}

// This is the final step of publishing a Module into an Assembly. This step cannot fail.
void Assembly::PublishModuleIntoAssembly(Module *module)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        FORBID_FAULT;
    }
    CONTRACTL_END

    GetManifestModule()->EnsuredStoreFile(module->GetModuleRef(), module);
    FastInterlockIncrement((LONG*)&m_pClassLoader->m_cUnhashedModules);
}



#ifdef FEATURE_MULTIMODULE_ASSEMBLIES
Module* Assembly::FindModule(PEFile *pFile, BOOL includeLoading)
{
    CONTRACT(Module *)
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    DomainFile *pModule = GetDomainAssembly()->FindModule(pFile, includeLoading);

    if (pModule == NULL)
        RETURN NULL;
    else
        RETURN pModule->GetModule();
}
#endif // FEATURE_MULTIMODULE_ASSEMBLIES

#ifdef FEATURE_MIXEDMODE
DomainFile* Assembly::FindIJWDomainFile(HMODULE hMod, const SString &path)
{
    CONTRACT (DomainFile*)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(GetManifestModule()));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    ModuleIterator i = IterateModules();
    while (i.Next())
    {
        PEFile *pFile = i.GetModule()->GetFile();

        if (   !pFile->IsResource()
            && !pFile->IsDynamic()
            && !pFile->IsILOnly())
        {
            if ( (pFile->GetLoadedIL()!= NULL && pFile->GetIJWBase() == hMod)
                || PEImage::PathEquals(pFile->GetPath(), path))
                RETURN i.GetModule()->GetDomainFile();
        }
    }
    RETURN NULL;
}
#endif // FEATURE_MIXEDMODE

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
        FriendAssemblyDescriptor *pFriendAssemblies = FriendAssemblyDescriptor::CreateFriendAssemblyDescriptor(this->GetManifestFile());
        if (pFriendAssemblies == NULL)
        {
            pFriendAssemblies = NO_FRIEND_ASSEMBLIES_MARKER;
        }

        void *pvPreviousDescriptor = InterlockedCompareExchangeT(&m_pFriendAssemblyDescriptor,
                                                                 pFriendAssemblies,
                                                                 NULL);

        if (pvPreviousDescriptor != NULL && pFriendAssemblies != NO_FRIEND_ASSEMBLIES_MARKER)
        {
            if (pFriendAssemblies != NO_FRIEND_ASSEMBLIES_MARKER)
            {
                delete pFriendAssemblies;
            }
        }
    }
} // void Assembly::CacheFriendAssemblyInfo()

//*****************************************************************************
// Is the given assembly a friend of this assembly?
bool Assembly::GrantsFriendAccessTo(Assembly *pAccessingAssembly, FieldDesc *pFD)
{
    WRAPPER_NO_CONTRACT;

    CacheFriendAssemblyInfo();

    if (m_pFriendAssemblyDescriptor == NO_FRIEND_ASSEMBLIES_MARKER)
    {
        return false;
    }

    return m_pFriendAssemblyDescriptor->GrantsFriendAccessTo(pAccessingAssembly, pFD);
}

bool Assembly::GrantsFriendAccessTo(Assembly *pAccessingAssembly, MethodDesc *pMD)
{
    WRAPPER_NO_CONTRACT;

    CacheFriendAssemblyInfo();

    if (m_pFriendAssemblyDescriptor == NO_FRIEND_ASSEMBLIES_MARKER)
    {
        return false;
    }

    return m_pFriendAssemblyDescriptor->GrantsFriendAccessTo(pAccessingAssembly, pMD);
}

bool Assembly::GrantsFriendAccessTo(Assembly *pAccessingAssembly, MethodTable *pMT)
{
    WRAPPER_NO_CONTRACT;

    CacheFriendAssemblyInfo();

    if (m_pFriendAssemblyDescriptor == NO_FRIEND_ASSEMBLIES_MARKER)
    {
        return false;
    }

    return m_pFriendAssemblyDescriptor->GrantsFriendAccessTo(pAccessingAssembly, pMT);
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

    CacheFriendAssemblyInfo();

    if (m_pFriendAssemblyDescriptor == NO_FRIEND_ASSEMBLIES_MARKER)
    {
        return false;
    }

    if (pAccessedAssembly->IsDisabledPrivateReflection())
    {
        return false;
    }

    if (!m_fIsDomainNeutral && !GetSecurityDescriptor(GetDomain()->AsAppDomain())->IsFullyTrusted())
    {
        return false;
    }

    return m_pFriendAssemblyDescriptor->IgnoresAccessChecksTo(pAccessedAssembly);
}


#ifndef CROSSGEN_COMPILE

enum CorEntryPointType
{
    EntryManagedMain,                   // void main(String[])
    EntryCrtMain                        // unsigned main(void)
};

#ifdef STRESS_THREAD

struct Stress_Thread_Param
{
    MethodDesc *pFD;
    GlobalStrongHandleHolder argHandle;
    short numSkipArgs;
    CorEntryPointType EntryType;
    Thread* pThread;

public:
    Stress_Thread_Param()
        : pFD(NULL),
          argHandle(),
          numSkipArgs(0),
          EntryType(EntryManagedMain),
          pThread(NULL)
    { LIMITED_METHOD_CONTRACT; }

    Stress_Thread_Param* Clone ()
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

        NewHolder<Stress_Thread_Param> retVal= new Stress_Thread_Param;

        retVal->pFD = pFD;
        if (argHandle.GetValue()!=NULL)
        {
            GCX_COOP();
            retVal->argHandle.Assign(CreateDuplicateHandle(argHandle.GetValue()));
        }
        retVal->numSkipArgs = numSkipArgs;
        retVal->EntryType = EntryType;
        retVal->pThread = pThread;
        return retVal.Extract();
    }
};

struct Stress_Thread_Worker_Param
{
    Stress_Thread_Param *lpParameter;
    ULONG retVal;
};

static void Stress_Thread_Proc_Worker_Impl(Stress_Thread_Worker_Param * args)
{
    STATIC_CONTRACT_THROWS;

    args->retVal = E_FAIL;

    Stress_Thread_Param* lpParam =  (Stress_Thread_Param *)args->lpParameter;

    ARG_SLOT stackVar = 0;

    MethodDescCallSite threadStart(lpParam->pFD);

    // Build the parameter array and invoke the method.
    if (lpParam->EntryType == EntryManagedMain) 
    {
        PTRARRAYREF StrArgArray = (PTRARRAYREF)ObjectFromHandle(lpParam->argHandle.GetValue());
        stackVar = ObjToArgSlot(StrArgArray);
    }

    if (lpParam->pFD->IsVoid())
    {
        threadStart.Call(&stackVar);
        args->retVal = GetLatchedExitCode();
    }
    else
    {
        // We are doing the same cast as in RunMain.  Main is required to return INT32 if it returns.
        ARG_SLOT retVal = (INT32)threadStart.Call_RetArgSlot(&stackVar);
        args->retVal = static_cast<ULONG>(retVal);
    }
}

// wrap into EX_TRY_NOCATCH and call the real thing
static void Stress_Thread_Proc_Worker (LPVOID ptr)
{
    STATIC_CONTRACT_THROWS;

    EX_TRY_NOCATCH(Stress_Thread_Worker_Param *, args, (Stress_Thread_Worker_Param *) ptr)
    {
        Stress_Thread_Proc_Worker_Impl(args);
        //<TODO>
        // When we get mainCRTStartup from the C++ then this should be able to go away.</TODO>
        fflush(stdout);
        fflush(stderr);
    }
    EX_END_NOCATCH
}

static DWORD WINAPI __stdcall Stress_Thread_Proc (LPVOID lpParameter)
{
    STATIC_CONTRACT_THROWS;

    Stress_Thread_Worker_Param args = {(Stress_Thread_Param*)lpParameter,0};
    Stress_Thread_Param *lpParam = (Stress_Thread_Param *)lpParameter;
    Thread *pThread = lpParam->pThread;
    if (!pThread->HasStarted())
        return 0;

    _ASSERTE(::GetAppDomain() != NULL);
    BEGIN_SO_INTOLERANT_CODE_NO_THROW_CHECK_THREAD(return E_FAIL);
    EX_TRY
    {

        ADID KickOffDomain = pThread->GetKickOffDomainId();

        // should always have a kickoff domain - a thread should never start in a domain that is unloaded
        // because otherwise it would have been collected because nobody can hold a reference to thread object
        // in a domain that has been unloaded. But it is possible that we started the unload, in which
        // case this thread wouldn't be allowed in or would be punted anyway.
        if (KickOffDomain != lpParam->pThread->GetDomain()->GetId())
            pThread->DoADCallBack(KickOffDomain, Stress_Thread_Proc_Worker, &args);
        else
            Stress_Thread_Proc_Worker(&args);
       
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);

    delete (Stress_Thread_Param *) lpParameter;
    // Enable preemptive GC so a GC thread can suspend me.
    GCX_PREEMP_NO_DTOR();
    DestroyThread(pThread);

    END_SO_INTOLERANT_CODE;  
    return args.retVal;
}

static void Stress_Thread_Start (LPVOID lpParameter)
{
    CONTRACT_VOID
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_ANY;
    }
    CONTRACT_END;

    Thread *pCurThread = GetThread();
    if (pCurThread->m_stressThreadCount == -1) {
        pCurThread->m_stressThreadCount = g_pConfig->GetStressThreadCount();
    }
    DWORD dwThreads = pCurThread->m_stressThreadCount;
    if (dwThreads <= 1)
        RETURN;

    Thread ** threads = new Thread* [dwThreads-1];

    DWORD n;
    for (n = 0; n < dwThreads-1; n ++)
    {
        threads[n] = SetupUnstartedThread();

        threads[n]->m_stressThreadCount = dwThreads/2;
        Stress_Thread_Param *param = ((Stress_Thread_Param*)lpParameter)->Clone();
        param->pThread = threads[n];
        if (!threads[n]->CreateNewThread(0, Stress_Thread_Proc, param))
        {
            delete param;
            threads[n]->DecExternalCount(FALSE);
            ThrowOutOfMemory();
        }
        threads[n]->SetThreadPriority (THREAD_PRIORITY_NORMAL);
    }

    for (n = 0; n < dwThreads-1; n ++)
    {
        threads[n]->StartThread();
    }
    __SwitchToThread (0, CALLER_LIMITS_SPINNING);

    RETURN;
}

void Stress_Thread_RunMain(MethodDesc* pFD, CorEntryPointType EntryType, short numSkipArgs, OBJECTHANDLE argHandle)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    Stress_Thread_Param Param;
    Param.pFD = pFD;
    Param.argHandle.Assign(argHandle);
    Param.numSkipArgs = numSkipArgs;
    Param.EntryType = EntryType;
    Param.pThread = NULL;
    Stress_Thread_Start (&Param);
}


#endif // STRESS_THREAD

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

    ULONG nCallConv;
    if (FAILED(sig.GetData(&nCallConv)))
        ThrowMainMethodException(pFD, BFA_BAD_SIGNATURE);
    
    if (nCallConv != IMAGE_CEE_CS_CALLCONV_DEFAULT)
        ThrowMainMethodException(pFD, IDS_EE_LOAD_BAD_MAIN_SIG);

    ULONG nParamCount;
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
 #ifndef FEATURE_CORECLR
       // If you look at the DIFF on this code then you will see a major change which is that we
        // no longer accept all the different types of data arguments to main.  We now only accept
        // an array of strings.

        wzArgs = CorCommandLine::GetArgvW(&cCommandArgs);
        // In the WindowsCE case where the app has additional args the count will come back zero.
        if (cCommandArgs > 0) {
            if (!wzArgs)
                return E_INVALIDARG;
        }
#else // !FEATURE_CORECLR
        return E_INVALIDARG;
#endif // !FEATURE_CORECLR
    }

    ETWFireEvent(Main_V1);

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
    param.pFD = pFD;
    param.numSkipArgs = numSkipArgs;
    param.piRetVal = piRetVal;
    param.stringArgs = stringArgs;
    param.EntryType = EntryType;
    param.cCommandArgs = cCommandArgs;
    param.wzArgs = wzArgs;

    EX_TRY_NOCATCH(Param *, pParam, &param)
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

#ifdef STRESS_THREAD
        OBJECTHANDLE argHandle = (StrArgArray != NULL) ? CreateGlobalStrongHandle (StrArgArray) : NULL;
        Stress_Thread_RunMain(pParam->pFD, pParam->EntryType, pParam->numSkipArgs, argHandle);
#endif

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
            if (pParam->stringArgs == NULL) 
            {
                SetLatchedExitCode(*pParam->piRetVal);
            }
        }

        GCPROTECT_END();

        //<TODO>
        // When we get mainCRTStartup from the C++ then this should be able to go away.</TODO>
        fflush(stdout);
        fflush(stderr);
    }
    EX_END_NOCATCH

    ETWFireEvent(MainEnd_V1);

    return hr;
}

static void RunMainPre()
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(GetThread() != 0);
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
        PRECONDITION(CheckPointer(GetThread()));
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

    BEGIN_ENTRYPOINT_THROWS;

    Thread *pThread = GetThread();
    MethodDesc *pMeth;
    {
        // This thread looks like it wandered in -- but actually we rely on it to keep the process alive.
        pThread->SetBackground(FALSE);
    
        GCX_COOP();

        pMeth = GetEntryPoint();
        if (pMeth) {
            RunMainPre();

#if defined(FEATURE_APPX_BINDER) && defined(FEATURE_MULTICOREJIT)
            if (AppX::IsAppXProcess())
            {
                GCX_PREEMP();
                
                // we call this to obtain and cache the PRAID value which is used
                // by multicore JIT manager and watson bucket params generation.

                // NOTE: this makes a COM call into WinRT so we must do this after we've
                //       set the thread's apartment state which will do CoInitializeEx().
                LPCWSTR praid;
                hr = AppX::GetApplicationId(praid);
                _ASSERTE(SUCCEEDED(hr));

                if (!pMeth->GetModule()->HasNativeImage())
                {
                    // For Appx, multicore JIT is only needed when root assembly does not have NI image
                    // When it has NI image, we can't generate profile, and do not need to playback profile
                    AppDomain * pDomain = pThread->GetDomain();
                    pDomain->GetMulticoreJitManager().AutoStartProfileAppx(pDomain);
                }
            }
#endif // FEATURE_APPX_BINDER && FEATURE_MULTICOREJIT
            
#ifdef FEATURE_CORECLR
            // Set the root assembly as the assembly that is containing the main method
            // The root assembly is used in the GetEntryAssembly method that on CoreCLR is used
            // to get the TargetFrameworkMoniker for the app
            AppDomain * pDomain = pThread->GetDomain();
            pDomain->SetRootAssembly(pMeth->GetAssembly());
#endif
            hr = RunMain(pMeth, 1, &iRetVal, stringArgs);
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
    
    END_ENTRYPOINT_THROWS;
    return iRetVal;
}
#endif // CROSSGEN_COMPILE

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

    mdToken mdEntry = m_pManifestFile->GetEntryPointToken();
    if (IsNilToken(mdEntry))
        RETURN NULL;

    Module *pModule = NULL;
    switch(TypeFromToken(mdEntry)) {
    case mdtFile:
        pModule = m_pManifest->LoadModule(::GetAppDomain(), mdEntry, FALSE)->GetModule();
        
        mdEntry = pModule->GetEntryPointToken();
        if ( (TypeFromToken(mdEntry) != mdtMethodDef) ||
             (!pModule->GetMDImport()->IsValidToken(mdEntry)) )
            pModule = NULL;
        break;
        
    case mdtMethodDef:
        if (m_pManifestFile->GetPersistentMDImport()->IsValidToken(mdEntry))
            pModule = m_pManifest;
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
    // we do not bother as the values would have come upon ensurin a valid parameter record
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

#ifndef CROSSGEN_COMPILE
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
#endif // CROSSGEN_COMPILE

/* static */
BOOL Assembly::FileNotFound(HRESULT hr)
{
    LIMITED_METHOD_CONTRACT;
    return IsHRESULTForExceptionKind(hr, kFileNotFoundException) || 
#ifdef FEATURE_COMINTEROP
           (hr == RO_E_METADATA_NAME_NOT_FOUND) || 
#endif //FEATURE_COMINTEROP
           (hr == CLR_E_BIND_TYPE_NOT_FOUND);
}

#ifdef FEATURE_MULTIMODULE_ASSEMBLIES
PEModule * Assembly::LoadModule_AddRef(mdFile kFile, BOOL fLoadResource)
{
    CONTRACT(PEModule *) 
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, fLoadResource ? NULL_NOT_OK : NULL_OK));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END
    
    if (! ((TypeFromToken(kFile) == mdtFile) &&
           GetManifestImport()->IsValidToken(kFile)) )
    {
        ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_FILE_TOKEN);
    }
    
    LPCSTR psModuleName;
    DWORD dwFlags;
    IfFailThrow(GetManifestImport()->GetFileProps(
        kFile, 
        &psModuleName, 
        NULL, 
        NULL, 
        &dwFlags));
    
    if (! (IsFfContainsMetaData(dwFlags) || fLoadResource) ) 
        RETURN NULL;
    
    SString name(SString::Utf8, psModuleName);
    PEModule * pModule = NULL;
    
    if (AssemblySpec::VerifyBindingString((LPCWSTR)name))
    {
        EX_TRY
        {
            GCX_PREEMP();

#ifdef FEATURE_FUSION    // specific to remote modules
            if (GetFusionAssembly()) {
                StackSString path;
                ::GetAppDomain()->GetFileFromFusion(GetFusionAssembly(),
                                                  (LPCWSTR)name, path);
                pModule = PEModule::Open(m_pManifestFile, kFile, path);
                goto lDone;
            }
            
            if (GetIHostAssembly()) {
                pModule = PEModule::Open(m_pManifestFile, kFile, name);
                goto lDone;
            }
#endif
            if (!m_pManifestFile->GetPath().IsEmpty()) {
                StackSString path = m_pManifestFile->GetPath();
                
                SString::Iterator i = path.End()-1;
            
                if (PEAssembly::FindLastPathSeparator(path, i)) {
                    path.Truncate(++i);
                    path.Insert(i, name);
                }
                pModule = PEModule::Open(m_pManifestFile, kFile, path);
            }
#ifdef FEATURE_FUSION        
        lDone: ;
#endif
        }
        EX_CATCH
        {
            Exception *ex = GET_EXCEPTION();
            if (FileNotFound(ex->GetHR()) ||
                (ex->GetHR() == FUSION_E_INVALID_NAME))
                pModule = RaiseModuleResolveEvent_AddRef(psModuleName, kFile);

            if (pModule == NULL)
            {
                EEFileLoadException::Throw(name, ex->GetHR(), ex);
            }
        }
        EX_END_CATCH(SwallowAllExceptions)
    }

    if (pModule == NULL)
    {
        pModule = RaiseModuleResolveEvent_AddRef(psModuleName, kFile);
        if (pModule == NULL)
        {
            EEFileLoadException::Throw(name, HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND));
        }
    }

    RETURN pModule;    
}

PEModule * Assembly::RaiseModuleResolveEvent_AddRef(LPCSTR szName, mdFile kFile)
{
    CONTRACT(PEModule *)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;
        
    Module* pModule = NULL;

#ifndef CROSSGEN_COMPILE
    GCX_COOP();

    struct _gc {
        OBJECTREF AssemblyRef;
        STRINGREF str;
    } gc;
    ZeroMemory(&gc, sizeof(gc));
    
    GCPROTECT_BEGIN(gc);
    if ((gc.AssemblyRef = GetExposedObject()) != NULL) 
    {
        MethodDescCallSite onModuleResolve(METHOD__ASSEMBLY__ON_MODULE_RESOLVE, &gc.AssemblyRef);
        gc.str = StringObject::NewString(szName);
        ARG_SLOT args[2] = {
            ObjToArgSlot(gc.AssemblyRef),
            ObjToArgSlot(gc.str)
        };
        
        REFLECTMODULEBASEREF ResultingModuleRef = 
            (REFLECTMODULEBASEREF) onModuleResolve.Call_RetOBJECTREF(args);
        
        if (ResultingModuleRef != NULL)
        {
            pModule = ResultingModuleRef->GetModule();
        }
    }
    GCPROTECT_END();

    if (pModule && ( (!(pModule->IsIntrospectionOnly())) != !(IsIntrospectionOnly()) ))
    {
        COMPlusThrow(kFileLoadException, IDS_CLASSLOAD_MODULE_RESOLVE_INTROSPECTION_MISMATCH);
    }

    if ((pModule != NULL) && 
        (pModule == m_pManifest->LookupFile(kFile)))
    {
        RETURN clr::SafeAddRef((PEModule *)pModule->GetFile());
    }
#endif // CROSSGEN_COMPILE

    RETURN NULL;
}
#endif //  FEATURE_MULTIMODULE_ASSEMBLIES

BOOL Assembly::GetResource(LPCSTR szName, DWORD *cbResource,
                              PBYTE *pbInMemoryResource, Assembly** pAssemblyRef,
                              LPCSTR *szFileName, DWORD *dwLocation,
                              StackCrawlMark *pStackMark, BOOL fSkipSecurityCheck,
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
                                                   szFileName, dwLocation, pStackMark, fSkipSecurityCheck,
                                                   fSkipRaiseResolveEvent);
    if (result && pAssemblyRef != NULL && pAssembly!=NULL)
        *pAssemblyRef = pAssembly->GetAssembly();

    return result;
}

#ifdef FEATURE_PREJIT
BOOL Assembly::IsInstrumented()
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_FAULT;

    BOOL isInstrumented = false;

    EX_TRY
    {
        FAULT_NOT_FATAL();

        isInstrumented = IsInstrumentedHelper();
    }
    EX_CATCH
    {
        isInstrumented = false;
    }
    EX_END_CATCH(RethrowTerminalExceptions);

    return isInstrumented;
}

BOOL Assembly::IsInstrumentedHelper()
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_FAULT;

    // Dynamic Assemblies cannot be instrumented
    if (IsDynamic())
        return false;

    // We must have a native image in order to perform IBC instrumentation
    if (!GetManifestFile()->HasNativeImage())
        return false;
    
    // @Consider using the full name instead of the short form
    // (see GetFusionAssemblyName()->IsEqual).

    LPCUTF8 szZapBBInstr = g_pConfig->GetZapBBInstr();
    LPCUTF8 szAssemblyName = GetSimpleName();

    if (!szZapBBInstr || !szAssemblyName ||
        (*szZapBBInstr == '\0') || (*szAssemblyName == '\0'))
        return false;

    // Convert to unicode so that we can do a case insensitive comparison

    SString instrumentedAssemblyNamesList(SString::Utf8, szZapBBInstr);
    SString assemblyName(SString::Utf8, szAssemblyName);

    const WCHAR *wszInstrumentedAssemblyNamesList = instrumentedAssemblyNamesList.GetUnicode();
    const WCHAR *wszAssemblyName                  = assemblyName.GetUnicode();

    // wszInstrumentedAssemblyNamesList is a space separated list of assembly names. 
    // We need to determine if wszAssemblyName is in this list.
    // If there is a "*" in the list, then all assemblies match.

    const WCHAR * pCur = wszInstrumentedAssemblyNamesList;

    do
    {
        _ASSERTE(pCur[0] != W('\0'));
        const WCHAR * pNextSpace = wcschr(pCur, W(' '));
        _ASSERTE(pNextSpace == NULL || pNextSpace[0] == W(' '));
        
        if (pCur != pNextSpace)
        {
            // pCur is not pointing to a space
            _ASSERTE(pCur[0] != W(' '));
            
            if (pCur[0] == W('*') && (pCur[1] == W(' ') || pCur[1] == W('\0')))
                return true;

            if (pNextSpace == NULL)
            {
                // We have reached the last name in the list. There are no more spaces.
                return (SString::_wcsicmp(wszAssemblyName, pCur) == 0);
            }
            else
            {
                if (SString::_wcsnicmp(wszAssemblyName, pCur, static_cast<COUNT_T>(pNextSpace - pCur)) == 0)
                    return true;
            }
        }

        pCur = pNextSpace + 1;
    }
    while (pCur[0] != W('\0'));

    return false;    
}
#endif // FEATURE_PREJIT

//***********************************************************
// Add an assembly to the assemblyref list. pAssemEmitter specifies where
// the AssemblyRef is emitted to.
//***********************************************************
mdAssemblyRef Assembly::AddAssemblyRef(Assembly *refedAssembly, IMetaDataAssemblyEmit *pAssemEmitter, BOOL fUsePublicKeyToken)
{
    CONTRACT(mdAssemblyRef)
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(CheckPointer(refedAssembly));
#ifdef FEATURE_CORECLR
        PRECONDITION(CheckPointer(pAssemEmitter, NULL_NOT_OK));
#else
        PRECONDITION(CheckPointer(pAssemEmitter, NULL_OK));
#endif //FEATURE_CORECLR
        POSTCONDITION(!IsNilToken(RETVAL));
        POSTCONDITION(TypeFromToken(RETVAL) == mdtAssemblyRef);
    }
    CONTRACT_END;

    SafeComHolder<IMetaDataAssemblyEmit> emitHolder;
#if !defined(FEATURE_CORECLR) && !defined(CROSSGEN_COMPILE)
    if (pAssemEmitter == NULL)
    {
        pAssemEmitter = GetOnDiskMDAssemblyEmitter();
        emitHolder.Assign(pAssemEmitter);
    }
#endif // FEATURE_CORECLR && !CROSSGEN_COMPILE

    AssemblySpec spec;
    spec.InitializeSpec(refedAssembly->GetManifestFile());

    if (refedAssembly->IsCollectible())
    {
        if (this->IsCollectible())
            this->GetLoaderAllocator()->EnsureReference(refedAssembly->GetLoaderAllocator());
        else
            COMPlusThrow(kNotSupportedException, W("NotSupported_CollectibleBoundNonCollectible"));
    }

    mdAssemblyRef ar;
    IfFailThrow(spec.EmitToken(pAssemEmitter, &ar, fUsePublicKeyToken));

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
    m_pClassLoader->AddExportedTypeDontHaveLock(GetManifestModule(),
        cl,
        &amTracker);
    amTracker.SuppressRelease();
}


#if !defined(FEATURE_CORECLR) && !defined(CROSSGEN_COMPILE)

//***********************************************************
//
// get the IMetaDataAssemblyEmit for the on disk manifest.
// Note that the pointer returned is AddRefed. It is the caller's
// responsibility to release the reference.
//
//***********************************************************
IMetaDataAssemblyEmit *Assembly::GetOnDiskMDAssemblyEmitter()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    IMetaDataAssemblyEmit *pAssemEmitter = NULL;
    IMetaDataEmit   *pEmitter;
    RefClassWriter  *pRCW;

    _ASSERTE(m_pOnDiskManifest);

    pRCW = m_pOnDiskManifest->GetClassWriter();
    _ASSERTE(pRCW);

    // If the RefClassWriter has a on disk emitter, then use it rather than the in-memory emitter.
    pEmitter = pRCW->GetOnDiskEmitter();

    if (pEmitter == NULL)
        pEmitter = m_pOnDiskManifest->GetEmitter();
    
    _ASSERTE(pEmitter != NULL);
    
    IfFailThrow(pEmitter->QueryInterface(IID_IMetaDataAssemblyEmit, (void**) &pAssemEmitter));
    
    if (pAssemEmitter == NULL)
    {
        // the manifest is not writable
        _ASSERTE(!"Bad usage!");
    }
    return pAssemEmitter;
}

//***********************************************************
//
// prepare saving manifest to disk.
//
//***********************************************************
void Assembly::PrepareSavingManifest(ReflectionModule *pAssemblyModule)
{
    STANDARD_VM_CONTRACT;

    if (pAssemblyModule)
    {
        // embedded assembly
        m_pOnDiskManifest = pAssemblyModule;
        m_fEmbeddedManifest = true;
    }
    else
    {
        m_fEmbeddedManifest = false;

        StackSString name(SString::Utf8, GetSimpleName());

        // Create the module
        m_pOnDiskManifest = CreateDynamicModule(name, name, FALSE /*fIsTransient*/);
        // store the fact this on disk manifest is temporary and can be hidden from the user
        m_needsToHideManifestForEmit = TRUE;
    }

    NonVMComHolder<IMetaDataAssemblyEmit> pAssemblyEmit(GetOnDiskMDAssemblyEmitter());

    // Copy assembly metadata to emit scope
    //<TODO>@todo: add Title, Description, Alias as CA</TODO>
    // <TODO>@todo: propagate all of the information</TODO>
    // <TODO>@todo: introduce a helper in metadata to take the ansi version of string.</TODO>

    IMetaDataAssemblyImport *pAssemblyImport = GetManifestFile()->GetAssemblyImporter();

    const void          *pbPublicKey;
    ULONG               cbPublicKey;
    ULONG               ulHashAlgId;
    LPWSTR              szName;
    ULONG               chName;
    ASSEMBLYMETADATA    MetaData;
    DWORD               dwAssemblyFlags;

    MetaData.cbLocale = 0;
    MetaData.ulProcessor = 0;
    MetaData.ulOS = 0;
    IfFailThrow(pAssemblyImport->GetAssemblyProps(TokenFromRid(1, mdtAssembly),
                                                  NULL, NULL, NULL,
                                                  NULL, 0, &chName,
                                                  &MetaData, NULL));
    StackSString name;
    szName = name.OpenUnicodeBuffer(chName);

    SString locale;
    MetaData.szLocale = locale.OpenUnicodeBuffer(MetaData.cbLocale);

    SBuffer proc;
    MetaData.rProcessor = (DWORD *) proc.OpenRawBuffer(MetaData.ulProcessor*sizeof(*MetaData.rProcessor));

    SBuffer os;
    MetaData.rOS = (OSINFO *) os.OpenRawBuffer(MetaData.ulOS*sizeof(*MetaData.rOS));

    IfFailThrow(pAssemblyImport->GetAssemblyProps(TokenFromRid(1, mdtAssembly),
                                                  &pbPublicKey, &cbPublicKey, &ulHashAlgId,
                                                  szName, chName, &chName,
                                                  &MetaData, &dwAssemblyFlags));

    mdAssembly ad;
    IfFailThrow(pAssemblyEmit->DefineAssembly(pbPublicKey, cbPublicKey, ulHashAlgId,
                                              szName, &MetaData, dwAssemblyFlags, &ad));

    SafeComHolder<IMetaDataImport> pImport;
    IfFailThrow(pAssemblyEmit->QueryInterface(IID_IMetaDataImport, (void**)&pImport));
    ULONG cExistingName = 0;
    if (FAILED(pImport->GetScopeProps(NULL, 0, &cExistingName, NULL)) || cExistingName == 0)
    {
        SafeComHolder<IMetaDataEmit> pEmit;
        IfFailThrow(pAssemblyEmit->QueryInterface(IID_IMetaDataEmit, (void**)&pEmit));
        IfFailThrow(pEmit->SetModuleProps(szName));
    }

    name.CloseBuffer();
    locale.CloseBuffer();
    proc.CloseRawBuffer();
    os.CloseRawBuffer();
}   // Assembly::PrepareSavingManifest


//***********************************************************
//
// add a file name to the file list of this assembly. On disk only.
//
//***********************************************************
mdFile Assembly::AddFile(LPCWSTR wszFileName)
{
    STANDARD_VM_CONTRACT;

    SafeComHolder<IMetaDataAssemblyEmit> pAssemEmitter(GetOnDiskMDAssemblyEmitter());
    mdFile          fl;

    // Define File.
    IfFailThrow( pAssemEmitter->DefineFile(
        wszFileName,                // [IN] Name of the file.
        0,                          // [IN] Hash Blob.
        0,                          // [IN] Count of bytes in the Hash Blob.
        0,                          // [IN] Flags.
        &fl) );                     // [OUT] Returned File token.

    return fl;
}   // Assembly::AddFile


//***********************************************************
//
// Set the hash value on a file table entry.
//
//***********************************************************
void Assembly::SetFileHashValue(mdFile tkFile, LPCWSTR wszFullFileName)
{
    STANDARD_VM_CONTRACT;

    SafeComHolder<IMetaDataAssemblyEmit> pAssemEmitter(GetOnDiskMDAssemblyEmitter());

    // Get the hash value.
    SBuffer buffer;
    PEImageHolder map(PEImage::OpenImage(StackSString(wszFullFileName)));
    map->ComputeHash(GetHashAlgId(), buffer);

    // Set the hash blob.
    IfFailThrow( pAssemEmitter->SetFileProps(
        tkFile,                 // [IN] File Token.
        buffer,                 // [IN] Hash Blob.
        buffer.GetSize(),       // [IN] Count of bytes in the Hash Blob.
        (DWORD) -1));           // [IN] Flags.

}   // Assembly::SetHashValue

//*****************************************************************************
// Add a Type name to the ExportedType table in the on-disk assembly manifest.
//*****************************************************************************
mdExportedType Assembly::AddExportedTypeOnDisk(LPCWSTR wszExportedType, mdToken tkImpl, mdToken tkTypeDef, CorTypeAttr flags)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(TypeFromToken(tkTypeDef) == mdtTypeDef);

    // The on-disk assembly manifest
    SafeComHolder<IMetaDataAssemblyEmit> pAssemEmitter(GetOnDiskMDAssemblyEmitter());

    mdExportedType ct;

    IfFailThrow( pAssemEmitter->DefineExportedType(
        wszExportedType,            // [IN] Name of the COMType.
        tkImpl,                     // [IN] mdFile or mdAssemblyRef that provides the ExportedType.
        tkTypeDef,                  // [IN] TypeDef token within the file.
        flags,                      // [IN] Flags.
        &ct) );                     // [OUT] Returned ExportedType token.

    return ct;
} // Assembly::AddExportedTypeOnDisk

//*******************************************************************************
// Add a Type name to the ExportedType table in the in-memory assembly manifest.
//*******************************************************************************
mdExportedType Assembly::AddExportedTypeInMemory(LPCWSTR wszExportedType, mdToken tkImpl, mdToken tkTypeDef, CorTypeAttr flags)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(TypeFromToken(tkTypeDef) == mdtTypeDef);

    // The in-memory assembly manifest
    IMetaDataAssemblyEmit* pAssemEmitter = GetManifestFile()->GetAssemblyEmitter();

    mdExportedType ct;

    IfFailThrow( pAssemEmitter->DefineExportedType(
        wszExportedType,            // [IN] Name of the COMType.
        tkImpl,                     // [IN] mdFile or mdAssemblyRef that provides the ExportedType.
        tkTypeDef,                  // [IN] TypeDef token within the file.
        flags,                      // [IN] Flags.
        &ct) );                     // [OUT] Returned ExportedType token.

    return ct;
} // Assembly::AddExportedTypeInMemory


//***********************************************************
// add an entry to ManifestResource table for a stand alone managed resource. On disk only.
//***********************************************************
void Assembly::AddStandAloneResource(LPCWSTR wszName, LPCWSTR wszDescription, LPCWSTR wszMimeType, LPCWSTR wszFileName, LPCWSTR wszFullFileName, int iAttribute)
{
    STANDARD_VM_CONTRACT;

    SafeComHolder<IMetaDataAssemblyEmit> pAssemEmitter(GetOnDiskMDAssemblyEmitter());
    mdFile          tkFile;
    mdManifestResource mr;
    SBuffer         hash;

    // Get the hash value;
    if (GetHashAlgId())
    {
        PEImageHolder pImage(PEImage::OpenImage(StackSString(wszFullFileName)));
        pImage->ComputeHash(GetHashAlgId(), hash);
    }

    IfFailThrow( pAssemEmitter->DefineFile(
        wszFileName,            // [IN] Name of the file.
        hash,                   // [IN] Hash Blob.
        hash.GetSize(),         // [IN] Count of bytes in the Hash Blob.
        ffContainsNoMetaData,   // [IN] Flags.
        &tkFile) );             // [OUT] Returned File token.


    IfFailThrow( pAssemEmitter->DefineManifestResource(
        wszName,                // [IN] Name of the resource.
        tkFile,                 // [IN] mdFile or mdAssemblyRef that provides the resource.
        0,                      // [IN] Offset to the beginning of the resource within the file.
        iAttribute,             // [IN] Flags.
        &mr) );                 // [OUT] Returned ManifestResource token.

}   // Assembly::AddStandAloneResource


//***********************************************************
// Save security permission requests.
//***********************************************************
void Assembly::AddDeclarativeSecurity(DWORD dwAction, void const *pValue, DWORD cbValue)
{
    STANDARD_VM_CONTRACT;

    mdAssembly tkAssembly = 0x20000001;

    SafeComHolder<IMetaDataAssemblyEmit> pAssemEmitter(GetOnDiskMDAssemblyEmitter());
    _ASSERTE( pAssemEmitter );

    SafeComHolder<IMetaDataEmitHelper> pEmitHelper;
    IfFailThrow( pAssemEmitter->QueryInterface(IID_IMetaDataEmitHelper, (void**)&pEmitHelper) );

    IfFailThrow(pEmitHelper->AddDeclarativeSecurityHelper(tkAssembly,
                                                          dwAction,
                                                          pValue,
                                                          cbValue,
                                                          NULL));
}


//***********************************************************
// Allocate space for a strong name signature in the manifest
//***********************************************************
HRESULT Assembly::AllocateStrongNameSignature(ICeeFileGen  *pCeeFileGen,
                                              HCEEFILE      ceeFile)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END
    
    HRESULT     hr;
    HCEESECTION TData;
    DWORD       dwDataOffset;
    DWORD       dwDataLength;
    DWORD       dwDataRVA;
    VOID       *pvBuffer;
    const void *pbPublicKey;
    ULONG       cbPublicKey;
    
    // Determine size of signature blob.
    
    IfFailRet(GetManifestImport()->GetAssemblyProps(TokenFromRid(1, mdtAssembly),
                                          &pbPublicKey, &cbPublicKey, NULL,
                                          NULL, NULL, NULL));
    
    if (!StrongNameSignatureSize((BYTE *) pbPublicKey, cbPublicKey, &dwDataLength)) {
        hr = StrongNameErrorInfo();
        return hr;
    }
    
    // Allocate space for the signature in the text section and update the COM+
    // header to point to the space.
    IfFailRet(pCeeFileGen->GetIlSection(ceeFile, &TData));
    IfFailRet(pCeeFileGen->GetSectionDataLen(TData, &dwDataOffset));
    IfFailRet(pCeeFileGen->GetSectionBlock(TData, dwDataLength, 4, &pvBuffer));
    IfFailRet(pCeeFileGen->GetMethodRVA(ceeFile, dwDataOffset, &dwDataRVA));
    IfFailRet(pCeeFileGen->SetStrongNameEntry(ceeFile, dwDataLength, dwDataRVA));
    
    return S_OK;
}


//***********************************************************
// Strong name sign a manifest already persisted to disk
//***********************************************************
HRESULT Assembly::SignWithStrongName(LPCWSTR wszFileName)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    HRESULT hr = S_OK;

    // If we're going to do a full signing we have a key pair either
    // in a key container or provided directly in a byte array.

    switch (m_eStrongNameLevel) {
    case SN_FULL_KEYPAIR_IN_ARRAY:
        if (!StrongNameSignatureGeneration(wszFileName, NULL, m_pbStrongNameKeyPair, m_cbStrongNameKeyPair, NULL, NULL))
            hr = StrongNameErrorInfo();
        break;

    case SN_FULL_KEYPAIR_IN_CONTAINER:
        if (!StrongNameSignatureGeneration(wszFileName, m_pwStrongNameKeyContainer, NULL, 0, NULL, NULL))
            hr = StrongNameErrorInfo();
        break;

    default:
        break;
    }

    return hr;
}


//***********************************************************
// save the manifest to disk!
//***********************************************************
void Assembly::SaveManifestToDisk(LPCWSTR wszFileName, int entrypoint, int fileKind, DWORD corhFlags, DWORD peFlags)
{
    STANDARD_VM_CONTRACT;

    HRESULT         hr = NOERROR;
    HCEEFILE        ceeFile = NULL;
    ICeeFileGen     *pCeeFileGen = NULL;
    RefClassWriter  *pRCW;
    IMetaDataEmit   *pEmitter;

    _ASSERTE( m_fEmbeddedManifest == false );

    pRCW = m_pOnDiskManifest->GetClassWriter();
    _ASSERTE(pRCW);

    IfFailGo( pRCW->EnsureCeeFileGenCreated(corhFlags, peFlags) );

    pCeeFileGen = pRCW->GetCeeFileGen();
    ceeFile = pRCW->GetHCEEFILE();
    _ASSERTE(ceeFile && pCeeFileGen);

    //Emit the MetaData
    pEmitter = m_pOnDiskManifest->GetClassWriter()->GetEmitter();
    IfFailGo( pCeeFileGen->EmitMetaDataEx(ceeFile, pEmitter) );

    // Allocate space for a strong name signature if a public key was supplied
    // (this doesn't strong name the assembly, but it makes it possible to do so
    // as a post processing step).
    if (IsStrongNamed())
        IfFailGo(AllocateStrongNameSignature(pCeeFileGen, ceeFile));

    IfFailGo( pCeeFileGen->SetOutputFileName(ceeFile, (LPWSTR)wszFileName) );

    // the entryPoint for an assembly is a tkFile token if exist.
    if (RidFromToken(entrypoint) != mdTokenNil)
        IfFailGo( pCeeFileGen->SetEntryPoint(ceeFile, entrypoint) );
    if (fileKind == Dll)
    {
        pCeeFileGen->SetDllSwitch(ceeFile, true);
    }
    else
    {
        // should have a valid entry point for applications
        if (fileKind == WindowApplication)
        {
            IfFailGo( pCeeFileGen->SetSubsystem(ceeFile, IMAGE_SUBSYSTEM_WINDOWS_GUI, CEE_IMAGE_SUBSYSTEM_MAJOR_VERSION, CEE_IMAGE_SUBSYSTEM_MINOR_VERSION) );
        }
        else
        {
            _ASSERTE(fileKind == ConsoleApplication);
            IfFailGo( pCeeFileGen->SetSubsystem(ceeFile, IMAGE_SUBSYSTEM_WINDOWS_CUI, CEE_IMAGE_SUBSYSTEM_MAJOR_VERSION, CEE_IMAGE_SUBSYSTEM_MINOR_VERSION) );
        }

    }

    //Generate the CeeFile
    IfFailGo(pCeeFileGen->GenerateCeeFile(ceeFile) );

    // Strong name sign the resulting assembly if required.
    if (IsStrongNamed())
        IfFailGo(SignWithStrongName(wszFileName));

    // now release the m_pOnDiskManifest
ErrExit:
    pRCW->DestroyCeeFileGen();

    // we keep the on disk manifest so that the GetModules code can skip over this ad-hoc module when modules are enumerated.
    // Need to see if we can remove the creation of this module alltogether
    //m_pOnDiskManifest = NULL;

    if (FAILED(hr))
    {
        if (HRESULT_FACILITY(hr) == FACILITY_WIN32)
        {
            if (IsWin32IOError(HRESULT_CODE(hr)))
            {
                COMPlusThrowHR(COR_E_IO);
            }
            else
            {
                COMPlusThrowHR(hr);
            }
        }
        if (hr == CEE_E_CVTRES_NOT_FOUND)
            COMPlusThrow(kIOException, W("Argument_cvtres_NotFound"));
        COMPlusThrowHR(hr);
    }
}   // Assembly::SaveManifestToDisk

#endif // FEATURE_CORECLR && !CROSSGEN_COMPILE


HRESULT STDMETHODCALLTYPE
GetAssembliesByName(LPCWSTR  szAppBase,
                    LPCWSTR  szPrivateBin,
                    LPCWSTR  szAssemblyName,
                    IUnknown *ppIUnk[],
                    ULONG    cMax,
                    ULONG    *pcAssemblies)
{
    CONTRACTL
    {
        NOTHROW;
        MODE_PREEMPTIVE;
        GC_TRIGGERS;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    HRESULT hr = S_OK;

    if (g_fEEInit) {
        // Cannot call this during EE startup
        return MSEE_E_ASSEMBLYLOADINPROGRESS;
    }

    if (!(szAssemblyName && ppIUnk && pcAssemblies))
        return E_POINTER;

#if defined(FEATURE_CORECLR) || defined(CROSSGEN_COMPILE)
    hr = COR_E_NOTSUPPORTED;
#else
    AppDomain *pDomain = NULL;

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    if(szAppBase || szPrivateBin)
    {
        GCX_COOP_THREAD_EXISTS(GET_THREAD());
        MethodDescCallSite createDomainEx(METHOD__APP_DOMAIN__CREATE_DOMAINEX);
        struct _gc {
            STRINGREF pFriendlyName;
            STRINGREF pAppBase;
            STRINGREF pPrivateBin;
        } gc;
        ZeroMemory(&gc, sizeof(gc));

        GCPROTECT_BEGIN(gc);
        gc.pFriendlyName = StringObject::NewString(W("GetAssembliesByName"));

        if(szAppBase)
        {
            gc.pAppBase = StringObject::NewString(szAppBase);
        }
            
        if(szPrivateBin)
        {
            gc.pPrivateBin = StringObject::NewString(szPrivateBin);
        }

        ARG_SLOT args[5] = 
        {
            ObjToArgSlot(gc.pFriendlyName),
            NULL,
            ObjToArgSlot(gc.pAppBase),
            ObjToArgSlot(gc.pPrivateBin),
            BoolToArgSlot(false)
        };
        APPDOMAINREF pDom = (APPDOMAINREF) createDomainEx.Call_RetOBJECTREF(args);
        if (pDom == NULL)
        {
            hr = E_FAIL;
        }
        else 
        {
            Context *pContext = CRemotingServices::GetServerContextForProxy((OBJECTREF) pDom);
            _ASSERTE(pContext);
            pDomain = pContext->GetDomain();
        }

        GCPROTECT_END();
    }
    else
        pDomain = SystemDomain::System()->DefaultDomain();

    Assembly *pFoundAssembly;
    if (SUCCEEDED(hr)) {
        pFoundAssembly = pDomain->LoadAssemblyHelper(szAssemblyName,
                                                     NULL);
        if (SUCCEEDED(hr)) {
            if (cMax < 1)
                hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
            else {
                ppIUnk[0] = (IUnknown *)pFoundAssembly->GetManifestAssemblyImporter();
                ppIUnk[0]->AddRef();
            }
            *pcAssemblies = 1;
        }
    }

    END_EXTERNAL_ENTRYPOINT;
#endif // FEATURE_CORECLR

    return hr;
}// Used by the IMetadata API's to access an assemblies metadata.

#ifdef FEATURE_LOADER_OPTIMIZATION

void Assembly::SetMissingDependenciesCheckDone()
{
    LIMITED_METHOD_CONTRACT;
    m_bMissingDependenciesCheckDone=TRUE;
};

BOOL Assembly::MissingDependenciesCheckDone()
{
    LIMITED_METHOD_CONTRACT;
    return m_bMissingDependenciesCheckDone;
};


#ifdef FEATURE_FUSION
void Assembly::SetBindingClosure(IAssemblyBindingClosure* pClosure) // Addrefs. It is assumed the caller did not addref pClosure for us.
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    _ASSERTE(m_pBindingClosure == NULL);
    _ASSERTE(pClosure != NULL);

    m_pBindingClosure = pClosure;
    pClosure->AddRef(); // It is assumed the caller did not addref pBindingClosure for us.
}

IAssemblyBindingClosure * Assembly::GetBindingClosure()
{
    LIMITED_METHOD_CONTRACT;
    return m_pBindingClosure;
}


// The shared module list is effectively an extension of the shared domain assembly hash table.
// It is the canonical list and aribiter of modules loaded from this assembly by any app domain.
// Modules are stored here immediately on creating (to prevent duplicate creation), as opposed to
// in the rid map, where they are only placed upon load completion.

BOOL Assembly::CanBeShared(DomainAssembly *pDomainAssembly)
{
    CONTRACTL
    {
        PRECONDITION(CheckPointer(pDomainAssembly));
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    LOG((LF_CODESHARING,
         LL_INFO100,
         "Checking if we can share: \"%S\" in domain 0x%x.\n",
         GetDebugName(), pDomainAssembly->GetAppDomain()));

    STRESS_LOG2(LF_CODESHARING, LL_INFO1000,"Checking whether DomainAssembly %p is compatible with Assembly %p",
        pDomainAssembly,this);

    // We must always share the same system assemblies
    if (IsSystem())
    {
        STRESS_LOG0(LF_CODESHARING, LL_INFO1000,"System assembly - sharing");
        return TRUE;
    }

    if ((pDomainAssembly->GetDebuggerInfoBits()&~(DACF_PDBS_COPIED|DACF_IGNORE_PDBS|DACF_OBSOLETE_TRACK_JIT_INFO))
        != (m_debuggerFlags&~(DACF_PDBS_COPIED|DACF_IGNORE_PDBS|DACF_OBSOLETE_TRACK_JIT_INFO)))
    {
        LOG((LF_CODESHARING,
             LL_INFO100,
             "We can't share it, desired debugging flags %x are different than %x\n",
             pDomainAssembly->GetDebuggerInfoBits(), (m_debuggerFlags&~(DACF_PDBS_COPIED|DACF_IGNORE_PDBS|DACF_OBSOLETE_TRACK_JIT_INFO))));
        STRESS_LOG2(LF_CODESHARING, LL_INFO100,"Flags diff= %08x [%08x/%08x]",pDomainAssembly->GetDebuggerInfoBits(),
                    m_debuggerFlags);
        g_dwLoaderReasonForNotSharing = ReasonForNotSharing_DebuggerFlagMismatch;
        return FALSE;
    }

    PEAssembly * pDomainAssemblyFile = pDomainAssembly->GetFile();
    if (pDomainAssemblyFile == NULL)
    {
        g_dwLoaderReasonForNotSharing = ReasonForNotSharing_NullPeassembly;
        return FALSE;
    }
    
    IAssemblyBindingClosure * pContext = GetBindingClosure();
    if (pContext == NULL)
    {
        STRESS_LOG1(LF_CODESHARING, LL_INFO1000,"No context 1 - status=%d",pDomainAssemblyFile->IsSystem());
        if (pDomainAssemblyFile->IsSystem())
            return TRUE;
        else
        {
            g_dwLoaderReasonForNotSharing = ReasonForNotSharing_MissingAssemblyClosure1;
            return FALSE;
        }
    }

    IAssemblyBindingClosure * pCurrentContext = pDomainAssembly->GetAssemblyBindingClosure(LEVEL_STARTING);
    if (pCurrentContext == NULL)
    {
        STRESS_LOG1(LF_CODESHARING, LL_INFO1000,"No context 2 - status=%d",pDomainAssemblyFile->IsSystem());
        if (pDomainAssemblyFile->IsSystem())
            return TRUE;
        else
        {
            g_dwLoaderReasonForNotSharing = ReasonForNotSharing_MissingAssemblyClosure2;
            return FALSE;
        }
    }

    // ensure the closures are walked
    {
        ReleaseHolder<IBindResult> pWinRTBindResult;
        
        IUnknown * pUnk;
        if (pDomainAssembly->GetFile()->IsWindowsRuntime())
        {   // It is .winmd file (WinRT assembly)
            IfFailThrow(CLRPrivAssemblyWinRT::GetIBindResult(pDomainAssembly->GetFile()->GetHostAssembly(), &pWinRTBindResult));
            pUnk = pWinRTBindResult;
        }
        else
        {
            pUnk = pDomainAssembly->GetFile()->GetFusionAssembly();
        }
        
        GCX_PREEMP();
        IfFailThrow(pCurrentContext->EnsureWalked(pUnk, ::GetAppDomain()->GetFusionContext(), LEVEL_COMPLETE));
    }

    if ((pContext->HasBeenWalked(LEVEL_COMPLETE) != S_OK) || !MissingDependenciesCheckDone())
    {
        GCX_COOP();

        BOOL fMissingDependenciesResolved = FALSE;

        ENTER_DOMAIN_PTR(SystemDomain::System()->DefaultDomain(), ADV_DEFAULTAD);
        {
            {
                ReleaseHolder<IBindResult> pWinRTBindResult;
        
                IUnknown * pUnk;
                if (GetManifestFile()->IsWindowsRuntime())
                {   // It is .winmd file (WinRT assembly)
                    IfFailThrow(CLRPrivAssemblyWinRT::GetIBindResult(GetManifestFile()->GetHostAssembly(), &pWinRTBindResult));
                    pUnk = pWinRTBindResult;
                }
                else
                {
                    pUnk = GetManifestFile()->GetFusionAssembly();
                }
                
                GCX_PREEMP();
                IfFailThrow(pContext->EnsureWalked(pUnk, ::GetAppDomain()->GetFusionContext(), LEVEL_COMPLETE));
            }
            DomainAssembly * domainAssembly = ::GetAppDomain()->FindDomainAssembly(this);
            if (domainAssembly != NULL)
            {
                if (domainAssembly->CheckMissingDependencies() == CMD_Resolved)
                {
                    //cannot share
                    fMissingDependenciesResolved = TRUE;
                }
            }
        }
        END_DOMAIN_TRANSITION;

        if (fMissingDependenciesResolved)
        {
            STRESS_LOG0(LF_CODESHARING, LL_INFO1000,"Missing dependencies resolved - not sharing");
            g_dwLoaderReasonForNotSharing = ReasonForNotSharing_MissingDependenciesResolved;
            return FALSE;
        }
    }

    HRESULT hr = pContext->IsEqual(pCurrentContext);
    IfFailThrow(hr);
    if (hr != S_OK)
    {
        STRESS_LOG1(LF_CODESHARING, LL_INFO1000,"Closure comparison returned %08x - not sharing",hr);        
        g_dwLoaderReasonForNotSharing = ReasonForNotSharing_ClosureComparisonFailed;
        return FALSE;
    }

    LOG((LF_CODESHARING, LL_INFO100, "We can share it : \"%S\"\n", GetDebugName()));
    STRESS_LOG0(LF_CODESHARING, LL_INFO1000,"Everything is fine - sharing");                
    return TRUE;
}
#endif

#ifdef FEATURE_VERSIONING

BOOL Assembly::CanBeShared(DomainAssembly *pDomainAssembly)
{
    PTR_PEAssembly pFile=pDomainAssembly->GetFile();

    if(pFile == NULL)
        return FALSE;

    if(pFile->IsDynamic())
        return FALSE;

    if(IsSystem() && pFile->IsSystem())
        return TRUE;

    if ((pDomainAssembly->GetDebuggerInfoBits()&~(DACF_PDBS_COPIED|DACF_IGNORE_PDBS|DACF_OBSOLETE_TRACK_JIT_INFO))
        != (m_debuggerFlags&~(DACF_PDBS_COPIED|DACF_IGNORE_PDBS|DACF_OBSOLETE_TRACK_JIT_INFO)))
    {
        LOG((LF_CODESHARING,
             LL_INFO100,
             "We can't share it, desired debugging flags %x are different than %x\n",
             pDomainAssembly->GetDebuggerInfoBits(), (m_debuggerFlags&~(DACF_PDBS_COPIED|DACF_IGNORE_PDBS|DACF_OBSOLETE_TRACK_JIT_INFO))));
        STRESS_LOG2(LF_CODESHARING, LL_INFO100,"Flags diff= %08x [%08x/%08x]",pDomainAssembly->GetDebuggerInfoBits(),
                    m_debuggerFlags);
        return FALSE;
    }

    return TRUE;
}

#endif // FEATURE_VERSIONING

#endif // FEATURE_LOADER_OPTIMIZATION

#if defined(FEATURE_APTCA) || defined(FEATURE_CORESYSTEM)
BOOL Assembly::AllowUntrustedCaller()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    return ModuleSecurityDescriptor::GetModuleSecurityDescriptor(this)->IsAPTCA();
}
#endif // defined(FEATURE_APTCA) || defined(FEATURE_CORESYSTEM)

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


#ifdef FEATURE_COMINTEROP
//
// Manage an ITypeLib pointer for this Assembly.
//
ITypeLib* Assembly::GetTypeLib()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        FORBID_FAULT;
    }
    CONTRACTL_END

    // Get the value we are going to return.
    ITypeLib *pResult = m_pITypeLib;
    // If there is a value, AddRef() it.
    if (pResult && pResult != (ITypeLib*)-1)
        pResult->AddRef();
    return pResult;
} // ITypeLib* Assembly::GetTypeLib()

void Assembly::SetTypeLib(ITypeLib *pNew)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        FORBID_FAULT;
    }
    CONTRACTL_END

    ITypeLib *pOld;
    pOld = InterlockedExchangeT(&m_pITypeLib, pNew);
    // TypeLibs are refcounted pointers.
    if (pNew != pOld)
    {
        if (pNew && pNew != (ITypeLib*)-1)
            pNew->AddRef();
        if (pOld && pOld != (ITypeLib*)-1)
            pOld->Release();
    }
} // void Assembly::SetTypeLib()

Assembly::WinMDStatus Assembly::GetWinMDStatus()
{
    LIMITED_METHOD_CONTRACT;

    if (m_winMDStatus == WinMDStatus_Unknown)
    {
        IWinMDImport *pWinMDImport = GetManifestWinMDImport();
        if (pWinMDImport != NULL)
        {
            BOOL bIsWinMDExp;
            VERIFY(SUCCEEDED(pWinMDImport->IsScenarioWinMDExp(&bIsWinMDExp)));

            if (bIsWinMDExp)
            {
                // this is a managed backed WinMD
                m_winMDStatus = WinMDStatus_IsManagedWinMD;
            }
            else
            {
                // this is a pure WinMD
                m_winMDStatus = WinMDStatus_IsPureWinMD;
            }
        }
        else
        {
            // this is not a WinMD at all
            m_winMDStatus = WinMDStatus_IsNotWinMD;
        }
    }

    return m_winMDStatus;
}

bool Assembly::IsWinMD()
{
    LIMITED_METHOD_CONTRACT;
    return GetWinMDStatus() != WinMDStatus_IsNotWinMD;
}

bool Assembly::IsManagedWinMD()
{
    LIMITED_METHOD_CONTRACT;
    return GetWinMDStatus() == WinMDStatus_IsManagedWinMD;
}

IWinMDImport *Assembly::GetManifestWinMDImport()
{
    LIMITED_METHOD_CONTRACT;

    if (m_pManifestWinMDImport == NULL)
    {
        ReleaseHolder<IWinMDImport> pWinMDImport;
        if (SUCCEEDED(m_pManifest->GetMDImport()->QueryInterface(IID_IWinMDImport, (void **)&pWinMDImport)))
        {
            if (InterlockedCompareExchangeT<IWinMDImport *>(&m_pManifestWinMDImport, pWinMDImport, NULL) == NULL)
            {
                pWinMDImport.SuppressRelease();
            }
        }
    }

    return m_pManifestWinMDImport;
}

#endif // FEATURE_COMINTEROP

#if !defined(FEATURE_CORECLR) && !defined(CROSSGEN_COMPILE)
void Assembly::GenerateBreadcrumbForServicing()
{
    STANDARD_VM_CONTRACT;

    if (AppX::IsAppXProcess() || IsIntrospectionOnly() || GetManifestFile()->IsDynamic())
    {
        return;
    }

    if (HasServiceableAttribute() || IsExistingOobAssembly())
    {
        StackSString ssDisplayName;
        GetDisplayName(ssDisplayName);

        WriteBreadcrumb(ssDisplayName);
        CheckDenyList(ssDisplayName);
    }
}

void Assembly::WriteBreadcrumb(const SString &ssDisplayName)
{
    STANDARD_VM_CONTRACT;

    WCHAR path[MAX_LONGPATH];
    HRESULT hr = WszSHGetFolderPath(NULL, CSIDL_COMMON_APPDATA, NULL, SHGFP_TYPE_CURRENT, ARRAYSIZE(path), path);
    if (hr != S_OK)
    {
        return;
    }

    if (wcscat_s(path, W("\\Microsoft\\NetFramework\\BreadcrumbStore\\")) != 0)
    {
        return;
    }

    size_t dirPathLen = wcslen(path);

    // Validate the display name.  E.g., we don't want the display name to start with "..\\".
    bool inSimpleName = true;
    for (SString::CIterator it = ssDisplayName.Begin(); it != ssDisplayName.End(); ++it)
    {
        WCHAR c = *it;

        // The following characters are always allowed: a-zA-Z0-9_
        if (c >= W('a') && c <= W('z') || c >= W('A') && c <= W('Z') || c >= W('0') && c <= W('9') || c == W('_')) continue;

        // The period is allowed except as the first char.
        if (c == W('.') && it != ssDisplayName.Begin()) continue;

        // A comma terminates the assembly simple name, and we are in key=value portion of the display name.
        if (c == W(','))
        {
            inSimpleName = false;
            continue;
        }

        // In key=value portion, space and equal sign are also allowed.
        if (!inSimpleName && (c == W(' ') || c == W('='))) continue;

        // If we reach here, we have an invalid assembly display name. Return without writing breadcrumb.
        return;
    }

    // Log a breadcrumb using full display name.
    if (wcscat_s(path, ssDisplayName.GetUnicode()) == 0)
    {
        HandleHolder hFile = WszCreateFile(path, 0, 0, NULL, CREATE_NEW, FILE_ATTRIBUTE_NORMAL, NULL);
    }

    // Log another breadcrumb using display name without version.
    // First make a copy of the display name, and look for its version part.
    StackSString ssNoVersion(ssDisplayName);
    SString::Iterator itVersion = ssNoVersion.Begin();
    if (!ssNoVersion.Find(itVersion, W(", Version=")))
    {
        return;
    }

    // Start from the comma before Version=, advance past the comma, then look for the next comma.
    SString::Iterator itVersionEnd = itVersion;
    ++itVersionEnd;
    if (!ssNoVersion.Find(itVersionEnd, W(',')))
    {
        // Version is the last key=value pair.
        itVersionEnd = ssNoVersion.End();
    }

    // Erase the version.
    ssNoVersion.Delete(itVersion, itVersionEnd - itVersion);

    // Generate the full path string and create the file.
    path[dirPathLen] = W('\0');
    if (wcscat_s(path, ssNoVersion.GetUnicode()) == 0)
    {
        HandleHolder hFile = WszCreateFile(path, 0, 0, NULL, CREATE_NEW, FILE_ATTRIBUTE_NORMAL, NULL);
    }

}

bool Assembly::HasServiceableAttribute()
{
    STANDARD_VM_CONTRACT;

    IMDInternalImport *pImport = GetManifestImport();
    MDEnumHolder hEnum(pImport);
    HRESULT hr = pImport->EnumCustomAttributeByNameInit(GetManifestToken(), ASSEMBLY_METADATA_TYPE, &hEnum);
    if (hr != S_OK)
    {
        return false;
    }

    mdCustomAttribute tkAttribute;
    while (pImport->EnumNext(&hEnum, &tkAttribute))
    {
        // Get raw custom attribute.
        const BYTE  *pbAttr = NULL;         // Custom attribute data as a BYTE*.
        ULONG cbAttr = 0;                   // Size of custom attribute data.
        if (FAILED(pImport->GetCustomAttributeAsBlob(tkAttribute, reinterpret_cast<const void **>(&pbAttr), &cbAttr)))
        {
            THROW_BAD_FORMAT(BFA_INVALID_TOKEN, GetManifestModule());
        }

        CustomAttributeParser cap(pbAttr, cbAttr);
        if (FAILED(cap.ValidateProlog()))
        {
            THROW_BAD_FORMAT(BFA_BAD_CA_HEADER, GetManifestModule());
        }

        // Get the metadata key. It is not null terminated.
        LPCUTF8 key;
        ULONG   cbKey;
        if (FAILED(cap.GetString(&key, &cbKey)))
        {
            THROW_BAD_FORMAT(BFA_BAD_CA_HEADER, GetManifestModule());
        }

        const LPCUTF8 szServiceable = "Serviceable";
        const ULONG cbServiceable = 11;
        if (cbKey != cbServiceable || strncmp(key, szServiceable, cbKey) != 0)
        {
            continue;
        }

        // Get the metadata value. It is not null terminated.
        if (FAILED(cap.GetString(&key, &cbKey)))
        {
            THROW_BAD_FORMAT(BFA_BAD_CA_HEADER, GetManifestModule());
        }

        const LPCUTF8 szTrue = "True";
        const ULONG cbTrue = 4;
        if (cbKey == cbTrue && strncmp(key, szTrue, cbKey) == 0)
        {
            return true;
        }
    }

    return false;
}

bool Assembly::IsExistingOobAssembly()
{
    WRAPPER_NO_CONTRACT;

    return ExistingOobAssemblyList::Instance()->IsOnlist(this);
}

void Assembly::CheckDenyList(const SString &ssDisplayName)
{
    STANDARD_VM_CONTRACT;

    StackSString ssKeyName(W("SOFTWARE\\Microsoft\\.NETFramework\\Policy\\DenyList\\"));

    ssKeyName.Append(ssDisplayName);

    RegKeyHolder hKey;
    LONG status = RegOpenKeyEx(HKEY_LOCAL_MACHINE, ssKeyName.GetUnicode(), 0, KEY_WOW64_64KEY | GENERIC_READ, &hKey);

    if (status != ERROR_SUCCESS)
    {
        return;
    }

    StackSString ssFwlink;
    HRESULT hr = Clr::Util::Reg::ReadStringValue(hKey, NULL, NULL, ssFwlink);
    if (FAILED(hr) || ssFwlink.GetCount() == 0)
    {
        ssFwlink.Set(W("http://go.microsoft.com/fwlink/?LinkID=286319"));
    }

    StackSString ssMessageTemplate;
    if(!ssMessageTemplate.LoadResource(CCompRC::Optional, IDS_EE_ASSEMBLY_ON_DENY_LIST))
    {
        ssMessageTemplate.Set(W("The assembly %1 that the application tried to load has a known vulnerability. Please go to %2 to find a fix for this issue."));
    }

    StackSString ssMessage;
    ssMessage.FormatMessage(FORMAT_MESSAGE_FROM_STRING, ssMessageTemplate.GetUnicode(), 0, 0, ssDisplayName, ssFwlink);

    ClrReportEvent(
        W(".NET Runtime"),            // Event source
        EVENTLOG_ERROR_TYPE,        // Type
        0,                          // Category
        SecurityConfig,             // Event ID
        NULL,                       // User SID
        ssMessage.GetUnicode());    // Message

    NewHolder<EEMessageException> pEx(new EEMessageException(kSecurityException, IDS_EE_ASSEMBLY_ON_DENY_LIST, ssDisplayName.GetUnicode(), ssFwlink.GetUnicode()));
    EEFileLoadException::Throw(m_pManifestFile, pEx->GetHR(), pEx);
}

BOOL IsReportableAssembly(PEAssembly *pPEAssembly)
{
    STANDARD_VM_CONTRACT;

    // If the assembly could have used a native image, but did not, report the IL image
    BOOL fCanUseNativeImage = (pPEAssembly->HasHostAssembly() || pPEAssembly->IsContextLoad()) && 
                               pPEAssembly->CanUseNativeImage() && 
                               !IsNativeImageOptedOut(pPEAssembly->GetFusionAssemblyName());

    return fCanUseNativeImage;
}

BOOL Assembly::SupportsAutoNGenWorker()
{
    STANDARD_VM_CONTRACT;

    PEAssembly *pPEAssembly = GetManifestFile();

    if (pPEAssembly->IsSourceGAC() && Fusion::Util::IsUnifiedAssembly(pPEAssembly->GetFusionAssemblyName()) == S_OK)
    {
        // Assemblies in the .NET Framework supports Auto NGen.
        return TRUE;
    }

    if (IsAfContentType_WindowsRuntime(GetFlags()))
    {
        // WinMD files support Auto NGen.
        return TRUE;
    }
    
    if (pPEAssembly->HasHostAssembly())
    {
        // Auto NGen is enabled on all Metro app assemblies.
        return TRUE;
    }

    if (pPEAssembly->IsSourceGAC())
    {
        // For non-framework assemblies in GAC, look for TargetFrameworkAttriute.
        const BYTE  *pbAttr;                // Custom attribute data as a BYTE*.
        ULONG       cbAttr;                 // Size of custom attribute data.
        HRESULT hr = GetManifestImport()->GetCustomAttributeByName(GetManifestToken(), TARGET_FRAMEWORK_TYPE, (const void**)&pbAttr, &cbAttr);
        if (hr != S_OK)
        {
            return FALSE;
        }

        CustomAttributeParser cap(pbAttr, cbAttr);
        if (FAILED(cap.ValidateProlog()))
        {
            THROW_BAD_FORMAT(BFA_BAD_CA_HEADER, GetManifestModule());
        }
        LPCUTF8 lpTargetFramework;
        ULONG cbTargetFramework;
        if (FAILED(cap.GetString(&lpTargetFramework, &cbTargetFramework)))
        {
            THROW_BAD_FORMAT(BFA_BAD_CA_HEADER, GetManifestModule());
        }

        if (lpTargetFramework == NULL || cbTargetFramework == 0)
        {
            return FALSE;
        }

        SString ssTargetFramework(SString::Utf8, lpTargetFramework, cbTargetFramework);

        // Look for two special TargetFramework values that disables AutoNGen.  To guard against future
        // variations of the string values, we do prefix matches.
        SString ssFramework40(SString::Literal, W(".NETFramework,Version=v4.0"));
        SString ssPortableLib(SString::Literal, W(".NETPortable,"));
        if (ssTargetFramework.BeginsWithCaseInsensitive(ssFramework40) || ssTargetFramework.BeginsWithCaseInsensitive(ssPortableLib))
        {
            return FALSE;
        }

        // If TargetFramework doesn't match one of the two special values, we enable Auto NGen.
        return TRUE;
    }

    return FALSE;
}

void Assembly::ReportAssemblyUse()
{
    STANDARD_VM_CONTRACT;

    // Do not log if we don't have a global gac logger object
    if (g_pIAssemblyUsageLogGac != NULL)
    {
        // Only consider reporting for loads that could possibly use native images.
        PEAssembly *pPEAssembly = this->GetManifestFile();
        if (IsReportableAssembly(pPEAssembly) && !pPEAssembly->IsReportedToUsageLog())
        {
            // Do not log repeatedly
            pPEAssembly->SetReportedToUsageLog();

            ReleaseHolder<IAssemblyUsageLog> pRefCountedUsageLog;
            IAssemblyUsageLog *pUsageLog = NULL;
            if (SupportsAutoNGen())
            {
                if (pPEAssembly->IsSourceGAC())
                {
                    pUsageLog = g_pIAssemblyUsageLogGac;
                }
                else if (pPEAssembly->HasHostAssembly())
                {
                    UINT_PTR binderId;
                    IfFailThrow(pPEAssembly->GetHostAssembly()->GetBinderID(&binderId));
                    pRefCountedUsageLog = AssemblyUsageLogManager::GetUsageLogForBinder(binderId);
                    pUsageLog = pRefCountedUsageLog;
                }
            }

            if (pUsageLog)
            {
                PEAssembly *pPEAssembly = GetManifestFile();
                StackSString name;
                // GAC Assemblies are reported by assembly name
                if (pUsageLog == g_pIAssemblyUsageLogGac)
                {
                    this->GetDisplayName(name);
                }
                // Other assemblies (AppX...) are reported by file path
                else
                {
                    name.Set(pPEAssembly->GetILimage()->GetPath().GetUnicode());
                }

                if (pPEAssembly->HasNativeImage())
                {
                    if(!IsSystem())
                    {
                        // If the assembly used a native image, report it
                        ReleaseHolder<PEImage> pNativeImage = pPEAssembly->GetNativeImageWithRef();
                        pUsageLog->LogFile(name.GetUnicode(), pNativeImage->GetPath().GetUnicode(), ASSEMBLY_USAGE_LOG_FLAGS_NI);
                    }
                }
                else
                {
                    // If the assembly could have used a native image, but did not, report the IL image
                    pUsageLog->LogFile(name.GetUnicode(), NULL, ASSEMBLY_USAGE_LOG_FLAGS_IL);
                }
            }
        }
    }
}
#endif // FEATURE_CORECLR && !CROSSGEN_COMPILE

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

    if (m_pDomain.IsValid())
    {
        m_pDomain->EnumMemoryRegions(flags, true);
    }
    if (m_pClassLoader.IsValid())
    {
        m_pClassLoader->EnumMemoryRegions(flags);
    }
    if (m_pManifest.IsValid())
    {
        m_pManifest->EnumMemoryRegions(flags, true);
    }
    if (m_pManifestFile.IsValid())
    {
        m_pManifestFile->EnumMemoryRegions(flags);
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
#ifdef FEATURE_FUSION
        pFriendAssemblyName->Release();
#else // FEATURE_FUSION
        delete pFriendAssemblyName;
#endif // FEATURE_FUSION
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
//    A friend assembly descriptor if the assembly declares any friend assemblies, otherwise NULL
//

// static
FriendAssemblyDescriptor *FriendAssemblyDescriptor::CreateFriendAssemblyDescriptor(PEAssembly *pAssembly)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(pAssembly));
    }
    CONTRACTL_END

    NewHolder<FriendAssemblyDescriptor> pFriendAssemblies = new FriendAssemblyDescriptor;

    // We're going to do this twice, once for InternalsVisibleTo and once for IgnoresAccessChecks
    ReleaseHolder<IMDInternalImport> pImport(pAssembly->GetMDImportWithRef());
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
#ifdef FEATURE_FUSION
            hr = CreateAssemblyNameObject(&pFriendAssemblyName, displayName.GetUnicode(), CANOF_PARSE_FRIEND_DISPLAY_NAME, NULL);
#else // FEATURE_FUSION
            StackScratchBuffer buffer;
            pFriendAssemblyName = new FriendAssemblyName_t;
            hr = pFriendAssemblyName->Init(displayName.GetUTF8(buffer));

            if (SUCCEEDED(hr))
            {
                hr = pFriendAssemblyName->CheckFriendAssemblyName();
            }
#endif // FEATURE_FUSION

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
#if !defined(FEATURE_CORECLR)        
            // If this assembly has a strong name, then its friends declarations need to have strong names too
            if (pAssembly->IsStrongNamed())
            {
#ifdef FEATURE_FUSION        
                DWORD dwSize = 0;
                if (SUCCEEDED(hr = pFriendAssemblyName->GetProperty(ASM_NAME_PUBLIC_KEY, NULL, &dwSize)))
                {
                    // If this call succeeds with an empty buffer, then the supplied name doesn't have a public key.
                    THROW_HR_ERROR_WITH_INFO(META_E_CA_FRIENDS_SN_REQUIRED, pAssembly);
                }
                else if (hr != HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
                {
                    IfFailThrow(hr);
                }
#else // FEATURE_FUSION
                // Desktop crossgen comes here
                if (!pFriendAssemblyName->IsStrongNamed())
                {
                    // If this call succeeds with an empty buffer, then the supplied name doesn't have a public key.
                    THROW_HR_ERROR_WITH_INFO(META_E_CA_FRIENDS_SN_REQUIRED, pAssembly);
                }
#endif // FEATURE_FUSION            
            }
#endif // !defined(FEATURE_CORECLR)

            pFriendAssemblies->AddFriendAssembly(pFriendAssemblyName);

            pFriendAssemblyName.SuppressRelease();
        }
    }

    pFriendAssemblies.SuppressRelease();
    return pFriendAssemblies.Extract();
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

#ifndef FEATURE_FUSION
    AssemblySpec asmDef;
    asmDef.InitializeSpec(pAssembly);
#endif        

    ArrayList::ConstIterator itAssemblyNames = alAssemblyNames.Iterate();
    while (itAssemblyNames.Next())
    {
        const FriendAssemblyName_t *pFriendAssemblyName = static_cast<const FriendAssemblyName_t *>(itAssemblyNames.GetElement());
#ifdef FEATURE_FUSION
        // This is a const operation on the pointer, but Fusion is not const-correct.
        //  @TODO - propigate const correctness through Fusion and remove this cast
        HRESULT hr = const_cast<FriendAssemblyName_t *>(pFriendAssemblyName)->IsEqual(pAssembly->GetFusionAssemblyName(), ASM_CMPF_DEFAULT);
        IfFailThrow(hr);
#else       
        HRESULT hr = AssemblySpec::RefMatchesDef(pFriendAssemblyName, &asmDef) ? S_OK : S_FALSE;
#endif

        if (hr == S_OK)
        {
            return true;
        }
    }

    return false;
}

#endif // !DACCESS_COMPILE


#if !defined(FEATURE_CORECLR) && !defined(CROSSGEN_COMPILE) && !defined(DACCESS_COMPILE)

ExistingOobAssemblyList::ExistingOobAssemblyList()
{
    STANDARD_VM_CONTRACT;

    RegKeyHolder hKey;
    LONG status = RegOpenKeyExW(HKEY_LOCAL_MACHINE, W("SOFTWARE\\Microsoft\\.NETFramework\\Policy\\Servicing"), 0, KEY_WOW64_64KEY | GENERIC_READ, &hKey);
    if (status != ERROR_SUCCESS)
    {
        return;
    }

    for (DWORD i = 0; ; i++)
    {
        WCHAR name[MAX_PATH_FNAME + 1];
        DWORD cchName = ARRAYSIZE(name);
        status = RegEnumKeyExW(hKey, i, name, &cchName, NULL, NULL, NULL, NULL);

        if (status == ERROR_NO_MORE_ITEMS)
        {
            break;
        }

        if (status == ERROR_SUCCESS)
        {
            NonVMComHolder<IAssemblyName> pAssemblyName;
            HRESULT hr = CreateAssemblyNameObject(&pAssemblyName, name, CANOF_PARSE_DISPLAY_NAME, NULL);
            if (SUCCEEDED(hr))
            {
                hr = m_alExistingOobAssemblies.Append(pAssemblyName.GetValue());
                if (SUCCEEDED(hr))
                {
                    pAssemblyName.SuppressRelease();
                }
            }
        }
    }
}

bool ExistingOobAssemblyList::IsOnlist(Assembly *pAssembly)
{
    STANDARD_VM_CONTRACT;

    ArrayList::Iterator itAssemblyNames = m_alExistingOobAssemblies.Iterate();
    while (itAssemblyNames.Next())
    {
        IAssemblyName *pAssemblyName = static_cast<IAssemblyName *>(itAssemblyNames.GetElement());
        HRESULT hr = pAssemblyName->IsEqual(pAssembly->GetFusionAssemblyName(), ASM_CMPF_DEFAULT);
        if (hr == S_OK)
        {
            return true;
        }
    }

    return false;
}

void ExistingOobAssemblyList::Init()
{
    STANDARD_VM_CONTRACT;

    s_pInstance = new ExistingOobAssemblyList();
}

ExistingOobAssemblyList *ExistingOobAssemblyList::s_pInstance;
#endif // !defined(FEATURE_CORECLR) && !defined(CROSSGEN_COMPILE) && !defined(DACCESS_COMPILE)
