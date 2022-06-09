// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: RsAppDomain.cpp
//

//
//*****************************************************************************
#include "stdafx.h"
#include "primitives.h"
#include "safewrap.h"

#include "check.h"

#ifndef SM_REMOTESESSION
#define SM_REMOTESESSION 0x1000
#endif

#include "corpriv.h"
#include "../../dlls/mscorrc/resource.h"
#include <limits.h>


/* ------------------------------------------------------------------------- *
 * AppDomain class methods
 * ------------------------------------------------------------------------- */

//
// Create a CordbAppDomain object based on a pointer to the AppDomain instance
// in the CLR.  Pre-populates some cached information about the AppDomain
// from the CLR using DAC.
//
// Arguments:
//    pProcess    - the CordbProcess object that this AppDomain is part of
//    vmAppDomain - the address in the CLR of the AppDomain object this corresponds to.
//                  This will be used to read any additional information about the AppDomain.
//
// Assumptions:
//    The IMetaSig object should have been allocated by
//    IMDInternal on a valid metadata blob
//
//
CordbAppDomain::CordbAppDomain(CordbProcess *  pProcess, VMPTR_AppDomain vmAppDomain)
  : CordbBase(pProcess, LsPtrToCookie(vmAppDomain.ToLsPtr()), enumCordbAppDomain),
    m_AppDomainId(0),
    m_breakpoints(17),
    m_sharedtypes(3),
    m_modules(17),
    m_assemblies(9),
    m_vmAppDomain(vmAppDomain)
{
    // This may throw out of the Ctor on error.

    // @dbgtodo  reliability: we should probably tolerate failures here and keep track
    // of whether our ADID is valid or not, and requery if necessary.
    m_AppDomainId = m_pProcess->GetDAC()->GetAppDomainId(m_vmAppDomain);

    LOG((LF_CORDB,LL_INFO10000, "CAD::CAD: this:0x%x (void*)this:0x%x<%d>\n", this, (void *)this, m_AppDomainId));

#ifdef _DEBUG
    m_assemblies.DebugSetRSLock(pProcess->GetProcessLock());
    m_modules.DebugSetRSLock(pProcess->GetProcessLock());
    m_breakpoints.DebugSetRSLock(pProcess->GetProcessLock());
    m_sharedtypes.DebugSetRSLock(pProcess->GetProcessLock());
#endif

}

/*
    A list of which resources owened by this object are accounted for.

    RESOLVED:
        // AddRef() in CordbHashTable::GetBase for a special InProc case
        // AddRef() on the DB_IPCE_CREATE_APP_DOMAIN event from the LS
        // Release()ed in Neuter
        CordbProcess        *m_pProcess;

        WCHAR               *m_szAppDomainName; // Deleted in ~CordbAppDomain

        // Cleaned up in Neuter
        CordbHashTable      m_assemblies;
        CordbHashTable      m_sharedtypes;
        CordbHashTable      m_modules;
        CordbHashTable      m_breakpoints; // Disconnect()ed in ~CordbAppDomain

    private:
*/

CordbAppDomain::~CordbAppDomain()
{

    // We expect to be Neutered before being released. Neutering will release our process ref
    _ASSERTE(IsNeutered());
}


// Neutered by process. Once we're neutered, we lose our backpointer to the CordbProcess object, and
// thus can't do things like call GetProcess() or Continue().
void CordbAppDomain::Neuter()
{
    // This check prevents us from calling this twice and underflowing the internal ref count!
    if (IsNeutered())
    {
        return;
    }
    _ASSERTE(GetProcess()->ThreadHoldsProcessLock());

    //
    // Disconnect any active breakpoints
    //
    {
        CordbBreakpoint* entry;
        HASHFIND find;

        for (entry =  m_breakpoints.FindFirst(&find);
             entry != NULL;
             entry =  m_breakpoints.FindNext(&find))
        {
            entry->Disconnect();
        }
    }

    // Mark as neutered so that our children can tell the appdomain has now
    // exited.
    CordbBase::Neuter();

    //
    // Purge neuter lists.
    //
    m_TypeNeuterList.NeuterAndClear(GetProcess());
    m_SweepableNeuterList.NeuterAndClear(GetProcess());


    m_assemblies.NeuterAndClear(GetProcess()->GetProcessLock());
    m_modules.NeuterAndClear(GetProcess()->GetProcessLock());
    m_sharedtypes.NeuterAndClear(GetProcess()->GetProcessLock());
    m_breakpoints.NeuterAndClear(GetProcess()->GetProcessLock());

}


HRESULT CordbAppDomain::QueryInterface(REFIID id, void **ppInterface)
{
    if (id == IID_ICorDebugAppDomain)
    {
        *ppInterface = (ICorDebugAppDomain*)this;
    }
    else if (id == IID_ICorDebugAppDomain2)
    {
        *ppInterface = (ICorDebugAppDomain2*)this;
    }
    else if (id == IID_ICorDebugAppDomain3)
    {
        *ppInterface = (ICorDebugAppDomain3*)this;
    }
    else if (id == IID_ICorDebugAppDomain4)
    {
        *ppInterface = (ICorDebugAppDomain4*)this;
    }
    else if (id == IID_ICorDebugController)
        *ppInterface = (ICorDebugController*)(ICorDebugAppDomain*)this;
    else if (id == IID_IUnknown)
        *ppInterface = (IUnknown*)(ICorDebugAppDomain*)this;
    else
    {
        *ppInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
}


//---------------------------------------------------------------------------------------
//
// Ensure the AppDomain friendly name has been set.
//
// Return value:
//    S_OK on success, or a failure code if we couldn't read the name for some reason.
//    There shouldn't be any reason in practice for this to fail other than a corrupt
//    process image.
//
// Assumptions:
//    The AppDomain object has already been initialized to know about
//    it's corresponding VM appdomain.
//    InvalidateName is called whenever the name may have changed to prompt us to re-fetch.
//
//---------------------------------------------------------------------------------------
HRESULT CordbAppDomain::RefreshName()
{
    if (m_strAppDomainName.IsSet())
    {
        // If we already have a valid name, we're done.
        return S_OK;
    }

    // Use DAC to get the name.

    _ASSERTE(!m_vmAppDomain.IsNull());
    IDacDbiInterface * pDac = NULL;
    HRESULT hr = S_OK;
    EX_TRY
    {
        pDac = m_pProcess->GetDAC();

    #ifdef _DEBUG
        // For debug, double-check the cached value against getting the AD via an AppDomainId.
        VMPTR_AppDomain pAppDomain = pDac->GetAppDomainFromId(m_AppDomainId);
        _ASSERTE(m_vmAppDomain == pAppDomain);
    #endif

        // Get the actual string contents.
        pDac->GetAppDomainFullName(m_vmAppDomain, &m_strAppDomainName);

        // Now that m_strAppDomainName is set, don't fail without clearing it.
    }
    EX_CATCH_HRESULT(hr);

    _ASSERTE(SUCCEEDED(hr) == m_strAppDomainName.IsSet());

    return hr;
}


HRESULT CordbAppDomain::Stop(DWORD dwTimeout)
{
    FAIL_IF_NEUTERED(this);
    PUBLIC_API_ENTRY(this);
    return (m_pProcess->StopInternal(dwTimeout, this->GetADToken()));
}

HRESULT CordbAppDomain::Continue(BOOL fIsOutOfBand)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    return m_pProcess->ContinueInternal(fIsOutOfBand);
}

HRESULT CordbAppDomain::IsRunning(BOOL *pbRunning)
{
    PUBLIC_API_ENTRY(this);
    VALIDATE_POINTER_TO_OBJECT(pbRunning, BOOL *);
    FAIL_IF_NEUTERED(this);

    *pbRunning = !m_pProcess->GetSynchronized();

    return S_OK;
}

HRESULT CordbAppDomain::HasQueuedCallbacks(ICorDebugThread *pThread, BOOL *pbQueued)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    VALIDATE_POINTER_TO_OBJECT_OR_NULL(pThread,ICorDebugThread *);
    VALIDATE_POINTER_TO_OBJECT(pbQueued,BOOL *);

    return m_pProcess->HasQueuedCallbacks (pThread, pbQueued);
}

HRESULT CordbAppDomain::EnumerateThreads(ICorDebugThreadEnum **ppThreads)
{
    // @TODO E_NOIMPL this
    //
    // (use Process::EnumerateThreads and let users filter their own data)
    HRESULT hr = S_OK;
    PUBLIC_API_BEGIN(this);
    {
        ValidateOrThrow(ppThreads);

        RSInitHolder<CordbEnumFilter> pThreadEnum(
                new CordbEnumFilter(GetProcess(), GetProcess()->GetContinueNeuterList()));

        GetProcess()->PrepopulateThreadsOrThrow();

        RSInitHolder<CordbHashTableEnum> pEnum;
        GetProcess()->BuildThreadEnum(this, NULL, pEnum.GetAddr());

        // This builds up auxillary list. don't need pEnum after this.
        hr = pThreadEnum->Init(pEnum, this);
        IfFailThrow(hr);

        pThreadEnum.TransferOwnershipExternal(ppThreads);
    }
    PUBLIC_API_END(hr);
    return hr;
}


HRESULT CordbAppDomain::SetAllThreadsDebugState(CorDebugThreadState state,
                                   ICorDebugThread *pExceptThisThread)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    return m_pProcess->SetAllThreadsDebugState(state, pExceptThisThread);
}

HRESULT CordbAppDomain::Detach()
{
    PUBLIC_REENTRANT_API_ENTRY(this); // may be called from IMDA::Detach
    FAIL_IF_NEUTERED(this);

    return E_NOTIMPL;
}

HRESULT CordbAppDomain::Terminate(unsigned int exitCode)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    return E_NOTIMPL;
}

void CordbAppDomain::AddToTypeList(CordbBase *pObject)
{
    INTERNAL_API_ENTRY(this);
    _ASSERTE(pObject != NULL);
    RSLockHolder lockHolder(GetProcess()->GetProcessLock());
    this->m_TypeNeuterList.Add(GetProcess(), pObject);
}


HRESULT CordbAppDomain::CanCommitChanges(
    ULONG cSnapshots,
    ICorDebugEditAndContinueSnapshot *pSnapshots[],
    ICorDebugErrorInfoEnum **pError)
{
    return E_NOTIMPL;
}

HRESULT CordbAppDomain::CommitChanges(
    ULONG cSnapshots,
    ICorDebugEditAndContinueSnapshot *pSnapshots[],
    ICorDebugErrorInfoEnum **pError)
{
    return E_NOTIMPL;
}


/*
 * GetProcess returns the process containing the app domain
 */
HRESULT CordbAppDomain::GetProcess(ICorDebugProcess **ppProcess)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    VALIDATE_POINTER_TO_OBJECT(ppProcess,ICorDebugProcess **);

    _ASSERTE (m_pProcess != NULL);

    *ppProcess = static_cast<ICorDebugProcess *> (m_pProcess);
    m_pProcess->ExternalAddRef();

    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// Callback for assembly enumeration.
//
// Arguments:
//      vmDomainAssembly - new assembly to add
//      pThis - user data for CordbAppDomain to add assembly too
//
//
// Assumptions:
//    Invoked as callback from code:CordbAppDomain::PrepopulateAssemblies
//
// Notes:
//

// static
void CordbAppDomain::AssemblyEnumerationCallback(VMPTR_DomainAssembly vmDomainAssembly, void * pThis)
{
    CordbAppDomain * pAppDomain = static_cast<CordbAppDomain *> (pThis);
    INTERNAL_DAC_CALLBACK(pAppDomain->GetProcess());

    // This lookup will cause the cache to be populated if we haven't seen this assembly before.
    pAppDomain->LookupOrCreateAssembly(vmDomainAssembly);
}


//---------------------------------------------------------------------------------------
//
// Cache a new assembly
//
// Arguments:
//      vmDomainAssembly - new assembly to add to cache
//
// Return Value:
//    Pointer to Assembly in cache.
//    NULL on failure, and sets unrecoverable error.
//
// Assumptions:
//    Caller guarantees assembly is not already added.
//    Called under the stop-go lock.
//
// Notes:
//
CordbAssembly * CordbAppDomain::CacheAssembly(VMPTR_DomainAssembly vmDomainAssembly)
{
    INTERNAL_API_ENTRY(GetProcess());

    VMPTR_Assembly vmAssembly;
    GetProcess()->GetDAC()->GetAssemblyFromDomainAssembly(vmDomainAssembly, &vmAssembly);

    RSInitHolder<CordbAssembly> pAssembly(new CordbAssembly(this, vmAssembly, vmDomainAssembly));

    return pAssembly.TransferOwnershipToHash(&m_assemblies);
}

CordbAssembly * CordbAppDomain::CacheAssembly(VMPTR_Assembly vmAssembly)
{
    INTERNAL_API_ENTRY(GetProcess());

    RSInitHolder<CordbAssembly> pAssembly(new CordbAssembly(this, vmAssembly, VMPTR_DomainAssembly()));

    return pAssembly.TransferOwnershipToHash(&m_assemblies);
}

//---------------------------------------------------------------------------------------
//
// Build up cache of assmeblies
//
// Arguments:
//
// Return Value:
//    Throws on error.
//
// Assumptions:
//    This is an non-invasive inspection operation called when the debuggee is stopped.
//
// Notes:
//    This can safely be called multiple times.
//

void CordbAppDomain::PrepopulateAssembliesOrThrow()
{
    INTERNAL_API_ENTRY(GetProcess());

    RSLockHolder lockHolder(GetProcess()->GetProcessLock());

    if (!GetProcess()->IsDacInitialized())
    {
        return;
    }

    // DD-primitive  that invokes a callback.
    GetProcess()->GetDAC()->EnumerateAssembliesInAppDomain(
        this->m_vmAppDomain,
        CordbAppDomain::AssemblyEnumerationCallback,
        this); // user data
}

//---------------------------------------------------------------------------------------
//
// Public API tp EnumerateAssemblies enumerates all assemblies in the app domain
//
// Arguments:
//      ppAssemblies - OUT: get enumerator
//
// Return Value:
//    S_OK on success.
//
//
// Notes:
//    This will prepopulate the list of assemblies (useful for non-invasive case
//    where we don't get debug event).
//

HRESULT CordbAppDomain::EnumerateAssemblies(ICorDebugAssemblyEnum **ppAssemblies)
{
    HRESULT hr = S_OK;
    PUBLIC_API_BEGIN(this);
    {
        ValidateOrThrow(ppAssemblies);
        *ppAssemblies = NULL;

        PrepopulateAssembliesOrThrow();

        RSInitHolder<CordbHashTableEnum> pEnum;
        CordbHashTableEnum::BuildOrThrow(
            this,
            GetProcess()->GetContinueNeuterList(), // ownership
            &m_assemblies,
            IID_ICorDebugAssemblyEnum,
            pEnum.GetAddr());
        pEnum.TransferOwnershipExternal(ppAssemblies);

    }
    PUBLIC_API_END(hr);
    return hr;
}

// Implement public interface
HRESULT CordbAppDomain::GetModuleFromMetaDataInterface(
                                                  IUnknown *pIMetaData,
                                                  ICorDebugModule **ppModule)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pIMetaData, IUnknown *);
    VALIDATE_POINTER_TO_OBJECT(ppModule, ICorDebugModule **);



    HRESULT hr = S_OK;

    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    *ppModule = NULL;

    EX_TRY
    {
        CordbModule * pModule = GetModuleFromMetaDataInterface(pIMetaData);
        _ASSERTE(pModule != NULL); // thrown on error

        *ppModule = static_cast<ICorDebugModule*> (pModule);
        pModule->ExternalAddRef();
    }
    EX_CATCH_HRESULT(hr);


    return hr;
}

// Gets a CordbModule that has the given metadata interface
//
// Arguments:
//     pIMetaData - metadata interface
//
// Returns:
//     CordbModule whose associated metadata matches the metadata interface provided here
//     Throws on error. Returns non-null
//
CordbModule * CordbAppDomain::GetModuleFromMetaDataInterface(IUnknown *pIMetaData)
{
    HRESULT hr = S_OK;

    RSExtSmartPtr<IMetaDataImport> pImport;
    RSLockHolder lockHolder(GetProcess()->GetProcessLock());  // need for module enumeration

    // Grab the interface we need...
    hr = pIMetaData->QueryInterface(IID_IMetaDataImport, (void**)&pImport);
    if (FAILED(hr))
    {
        ThrowHR(E_INVALIDARG);
    }

    // Get the mvid of the given module.
    GUID matchMVID;
    hr = pImport->GetScopeProps(NULL, 0, 0, &matchMVID);
    IfFailThrow(hr);

    CordbModule* pModule;
    HASHFIND findmodule;

    PrepopulateModules();

    for (pModule =  m_modules.FindFirst(&findmodule);
         pModule != NULL;
         pModule =  m_modules.FindNext(&findmodule))
    {
        IMetaDataImport * pImportCurrent = pModule->GetMetaDataImporter(); // throws
        _ASSERTE(pImportCurrent != NULL);

        // Get the mvid of this module
        GUID MVID;
        hr = pImportCurrent->GetScopeProps(NULL, 0, 0, &MVID);
        IfFailThrow(hr);

        if (MVID == matchMVID)
        {
            return pModule;
        }
    }

    ThrowHR(E_INVALIDARG);
}

HRESULT CordbAppDomain::EnumerateBreakpoints(ICorDebugBreakpointEnum **ppBreakpoints)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());
    VALIDATE_POINTER_TO_OBJECT(ppBreakpoints, ICorDebugBreakpointEnum **);

    HRESULT hr = S_OK;
    EX_TRY
    {
        RSInitHolder<CordbHashTableEnum> pEnum;
        CordbHashTableEnum::BuildOrThrow(
            this,
            GetProcess()->GetContinueNeuterList(), // ownership
            &m_breakpoints,
            IID_ICorDebugBreakpointEnum,
            pEnum.GetAddr());

        pEnum.TransferOwnershipExternal(ppBreakpoints);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT CordbAppDomain::EnumerateSteppers(ICorDebugStepperEnum **ppSteppers)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());
    VALIDATE_POINTER_TO_OBJECT(ppSteppers,ICorDebugStepperEnum **);

    HRESULT hr = S_OK;
    EX_TRY
    {
        //
        // !!! m_steppers may be modified while user is enumerating,
        // if steppers complete (if process is running)
        //

        RSInitHolder<CordbHashTableEnum> pEnum;
        CordbHashTableEnum::BuildOrThrow(
            GetProcess(),
            GetProcess()->GetContinueNeuterList(),  // ownership
            &(m_pProcess->m_steppers),
            IID_ICorDebugStepperEnum,
            pEnum.GetAddr());

        pEnum.TransferOwnershipExternal(ppSteppers);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}


//---------------------------------------------------------------------------------------
//
// CordbAppDomain::IsAttached - always returns true
//
// Arguments:
//    pfAttached - out parameter, will be set to TRUE
//
// Return Value:
//    CORDB_E_OBJECT_NEUTERED if the AppDomain has been neutered
//    E_INVALIDARG if pbAttached is null
//    Otherwise always returns S_OK.
//
// Notes:
//    Prior to V3, we used to keep track of a per-appdomain attached status.
//    Debuggers were required to explicitly attach to every AppDomain, so this
//    did not provide any actual functionality.  In V3, there is no longer any
//    concept of per-AppDomain attach/detach.  This API is provided for compatibility.
//

HRESULT CordbAppDomain::IsAttached(BOOL *pfAttached)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pfAttached, BOOL *);

    *pfAttached = TRUE;

    return S_OK;
}


//---------------------------------------------------------------------------------------
//
// CordbAppDomain::Attach - does nothing
//
// Arguments:
//
// Return Value:
//    CORDB_E_OBJECT_NEUTERED if the AppDomain has been neutered
//    Otherwise always returns S_OK.
//
// Notes:
//    Prior to V3, we used to keep track of a per-appdomain attached status.
//    Debuggers were required to explicitly attach to every AppDomain, so this
//    did not provide any actual functionality.  In V3, there is no longer any
//    concept of per-AppDomain attach/detach.  This API is provided for compatibility.
//

HRESULT CordbAppDomain::Attach()
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(m_pProcess);

    return S_OK;
}

/*
 * GetName returns the name of the app domain.
 */
HRESULT CordbAppDomain::GetName(ULONG32 cchName,
                                ULONG32 *pcchName,
                                _Out_writes_to_opt_(cchName, *pcchName) WCHAR szName[])
{
    HRESULT hr = S_OK;
    PUBLIC_API_BEGIN(this)
    {
        // Some reasonable defaults
        if (szName)
            *szName = 0;

        if (pcchName)
            *pcchName = 0;


        // Lazily refresh.
        IfFailThrow(RefreshName());

        const WCHAR * pName = m_strAppDomainName;
        _ASSERTE(pName != NULL);

        hr = CopyOutString(pName, cchName, pcchName, szName);
    }
    PUBLIC_API_END(hr);
    return hr;
}

/*
 * GetObject returns the runtime app domain object.
 * Note: this is lazily initialized and may be NULL
 */
HRESULT CordbAppDomain::GetObject(ICorDebugValue **ppObject)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppObject,ICorDebugObjectValue **);

    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    _ASSERTE(!m_vmAppDomain.IsNull());
    IDacDbiInterface * pDac = NULL;
    HRESULT hr = S_OK;
    EX_TRY
    {
        pDac = m_pProcess->GetDAC();
        VMPTR_OBJECTHANDLE vmObjHandle = pDac->GetAppDomainObject(m_vmAppDomain);
        if (!vmObjHandle.IsNull())
        {
            ICorDebugReferenceValue * pRefValue = NULL;
            hr = CordbReferenceValue::BuildFromGCHandle(this, vmObjHandle, &pRefValue);
            *ppObject = pRefValue;
        }
        else
        {
            *ppObject = NULL;
            hr = S_FALSE;
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

/*
 * Get the ID of the app domain.
 */
HRESULT CordbAppDomain::GetID (ULONG32 *pId)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    OK_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pId, ULONG32 *);

    *pId = m_AppDomainId;

    return S_OK;
}

//---------------------------------------------------------------------------------------
//  Remove an assembly from the ICorDebug cache.
//
//  Arguments:
//     vmDomainAssembly - token to remove.
//
//  Notes:
//     This is the opposite of code:CordbAppDomain::LookupOrCreateAssembly.
//     This only need to be called at assembly unload events.
void CordbAppDomain::RemoveAssemblyFromCache(VMPTR_DomainAssembly vmDomainAssembly)
{
    // This will handle if the assembly is not in the hash.
    // This could happen if we attach right before an assembly-unload event.
    m_assemblies.RemoveBase(VmPtrToCookie(vmDomainAssembly));
}

//---------------------------------------------------------------------------------------
// Lookup (or create) the CordbAssembly for the given VMPTR_DomainAssembly
//
// Arguments:
//     vmDomainAssembly - CLR token for the Assembly.
//
// Returns:
//     a CordbAssembly object for the given CLR assembly. This may be from the cache,
//     or newly created if not yet in the cache.
//     Never returns NULL. Throws on error (eg, oom).
//
CordbAssembly * CordbAppDomain::LookupOrCreateAssembly(VMPTR_DomainAssembly vmDomainAssembly)
{
    CordbAssembly * pAssembly = m_assemblies.GetBase(VmPtrToCookie(vmDomainAssembly));
    if (pAssembly != NULL)
    {
        return pAssembly;
    }
    return CacheAssembly(vmDomainAssembly);
}


//
CordbAssembly * CordbAppDomain::LookupOrCreateAssembly(VMPTR_Assembly vmAssembly)
{
    CordbAssembly * pAssembly = m_assemblies.GetBase(VmPtrToCookie(vmAssembly));
    if (pAssembly != NULL)
    {
        return pAssembly;
    }
    return CacheAssembly(vmAssembly);
}


//---------------------------------------------------------------------------------------
// Lookup or create a module within the appdomain
//
// Arguments:
//    vmDomainAssembly - non-null module to lookup
//
// Returns:
//    a CordbModule object for the given cookie. Object may be from the cache, or created
//    lazily.
//    Never returns null.  Throws on error.
//
// Notes:
//    If you don't know which appdomain the module is in, use code:CordbProcess::LookupOrCreateModule.
//
CordbModule* CordbAppDomain::LookupOrCreateModule(VMPTR_Module vmModule, VMPTR_DomainAssembly vmDomainAssembly)
{
    INTERNAL_API_ENTRY(this);
    CordbModule * pModule;

    RSLockHolder lockHolder(GetProcess()->GetProcessLock()); // @dbgtodo  locking: push this up.

    _ASSERTE(!vmDomainAssembly.IsNull() || !vmModule.IsNull());

    // check to see if the module is present in this app domain
    pModule = m_modules.GetBase(vmDomainAssembly.IsNull() ? VmPtrToCookie(vmModule) : VmPtrToCookie(vmDomainAssembly));
    if (pModule != NULL)
    {
        return pModule;
    }

    if (vmModule.IsNull())
        GetProcess()->GetDAC()->GetModuleForDomainAssembly(vmDomainAssembly, &vmModule);

    RSInitHolder<CordbModule> pModuleInit(new CordbModule(GetProcess(), vmModule, vmDomainAssembly));
    pModule = pModuleInit.TransferOwnershipToHash(&m_modules);

    // The appdomains should match.
    GetProcess()->TargetConsistencyCheck(pModule->GetAppDomain() == this);

    return pModule;
}


CordbModule* CordbAppDomain::LookupOrCreateModule(VMPTR_DomainAssembly vmDomainAssembly)
{
    INTERNAL_API_ENTRY(this);

    _ASSERTE(!vmDomainAssembly.IsNull());
    return LookupOrCreateModule(VMPTR_Module::NullPtr(), vmDomainAssembly);
}



//---------------------------------------------------------------------------------------
// Callback invoked by DAC for each module in an assembly. Used to populate RS module cache.
//
// Arguments:
//     vmModule - module from enumeration
//     pUserData - user data, a 'this' pointer to the CordbAssembly to add to.
//
// Notes:
//    This is called from code:CordbAppDomain::PrepopulateModules invoking DAC, which
//    invokes this callback.

// static
void CordbAppDomain::ModuleEnumerationCallback(VMPTR_DomainAssembly vmModule, void * pUserData)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    CordbAppDomain * pAppDomain = static_cast<CordbAppDomain *> (pUserData);
    INTERNAL_DAC_CALLBACK(pAppDomain->GetProcess());

    pAppDomain->LookupOrCreateModule(vmModule);
}


//
// Use DAC to preopulate the list of modules for this assembly
//
// Notes:
//     This may pick up modules for which a load notification has not yet been dispatched.
void CordbAppDomain::PrepopulateModules()
{
    INTERNAL_API_ENTRY(GetProcess());

    if (!GetProcess()->IsDacInitialized())
    {
        return;
    }

    RSLockHolder lockHolder(GetProcess()->GetProcessLock());

    // Want to make sure we don't double-add modules.
    // Modules for all assemblies are stored in 1 giant hash in the AppDomain, so
    // we don't have a good way of querying if this specific assembly needs to be prepopulated.
    // We'll check before adding each module that it's unique.

    PrepopulateAssembliesOrThrow();

    HASHFIND hashfind;

    for (CordbAssembly * pAssembly = m_assemblies.FindFirst(&hashfind);
         pAssembly != NULL;
         pAssembly = m_assemblies.FindNext(&hashfind))
    {

        // DD-primitive  that invokes a callback.
        GetProcess()->GetDAC()->EnumerateModulesInAssembly(
            pAssembly->GetDomainAssemblyPtr(),
            CordbAppDomain::ModuleEnumerationCallback,
            this); // user data

    }
}

//-----------------------------------------------------------------------------
//
// Get a type that represents an array or pointer of the given type.
//
// Arguments:
//   elementType - determines if this will be an array or a pointer
//   nRank - Rank of the array to make
//   pTypeArg - The type of array element or pointer.
//   ppResultType - OUT: out parameter to hold outgoing CordbType.
//
// Returns:
//   S_OK on success. Else failure.
//
HRESULT CordbAppDomain::GetArrayOrPointerType(CorElementType elementType,
                                              ULONG32 nRank,
                                              ICorDebugType * pTypeArg,
                                              ICorDebugType ** ppResultType)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppResultType, ICorDebugType **);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    CordbType * pResultType = NULL;

    if (!(elementType == ELEMENT_TYPE_PTR && nRank == 0) &&
        !(elementType == ELEMENT_TYPE_BYREF && nRank == 0) &&
        !(elementType == ELEMENT_TYPE_SZARRAY && nRank == 1) &&
        !(elementType == ELEMENT_TYPE_ARRAY))
    {
        return E_INVALIDARG;
    }

    HRESULT hr = CordbType::MkType(
        this,
        elementType,
        (ULONG) nRank,
        static_cast<CordbType *>(pTypeArg),
        &pResultType);

    if (FAILED(hr))
    {
        return hr;
    }

    _ASSERTE(pResultType != NULL);

    pResultType->ExternalAddRef();

    *ppResultType = pResultType;
    return hr;

}


//-----------------------------------------------------------------------------
//
// Get a type that represents a function pointer with signature of the types given.
//
// Arguments:
//   cTypeArgs - count of the number of entries in rgpTypeArgs
//   rgpTypeArgs - Array of types
//   ppResultType - OUT: out parameter to hold outgoing CordbType.
//
// Returns:
//   S_OK on success. Else failure.
//
HRESULT CordbAppDomain::GetFunctionPointerType(ULONG32 cTypeArgs,
                                               ICorDebugType * rgpTypeArgs[],
                                               ICorDebugType ** ppResultType)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppResultType, ICorDebugType **);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    // Prefast overflow check:
    S_UINT32 allocSize = S_UINT32(cTypeArgs) * S_UINT32(sizeof(CordbType *));

    if (allocSize.IsOverflow())
    {
        return E_INVALIDARG;
    }

    CordbType ** ppTypeInstantiations = reinterpret_cast<CordbType **>(_alloca(allocSize.Value()));

    for (unsigned int i = 0; i < cTypeArgs; i++)
    {
        ppTypeInstantiations[i] = (CordbType *) rgpTypeArgs[i];
    }


    Instantiation typeInstantiation(cTypeArgs, ppTypeInstantiations);

    CordbType * pType;

    HRESULT hr = CordbType::MkType(this, ELEMENT_TYPE_FNPTR, &typeInstantiation, &pType);

    if (FAILED(hr))
    {
        return hr;
    }

    _ASSERTE(pType != NULL);

    pType->ExternalAddRef();

    *ppResultType = static_cast<ICorDebugType *>(pType);

    return hr;

}

//
// ICorDebugAppDomain3
//

HRESULT CordbAppDomain::GetCachedWinRTTypesForIIDs(
                        ULONG32               cGuids,
                        GUID                * iids,
                        ICorDebugTypeEnum * * ppTypesEnum)
{
    return E_NOTIMPL;
}

HRESULT CordbAppDomain::GetCachedWinRTTypes(
                        ICorDebugGuidToTypeEnum * * ppTypesEnum)
{
    return E_NOTIMPL;
}

//-----------------------------------------------------------
// ICorDebugAppDomain4
//-----------------------------------------------------------

HRESULT CordbAppDomain::GetObjectForCCW(CORDB_ADDRESS ccwPointer, ICorDebugValue **ppManagedObject)
{
#if defined(FEATURE_COMINTEROP)

    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    VALIDATE_POINTER_TO_OBJECT(ppManagedObject, ICorDebugValue **);
    HRESULT hr = S_OK;

    *ppManagedObject = NULL;

    EX_TRY
    {
        VMPTR_OBJECTHANDLE vmObjHandle = GetProcess()->GetDAC()->GetObjectForCCW(ccwPointer);
        if (vmObjHandle.IsNull())
        {
            hr = E_INVALIDARG;
        }
        else
        {
            ICorDebugReferenceValue *pRefValue = NULL;
            hr = CordbReferenceValue::BuildFromGCHandle(this, vmObjHandle, &pRefValue);
            *ppManagedObject = pRefValue;
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;

#else

    return E_NOTIMPL;

#endif // defined(FEATURE_COMINTEROP)
}
