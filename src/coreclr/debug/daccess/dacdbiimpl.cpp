// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: DacDbiImpl.cpp
//

//
// Implement DAC/DBI interface
//
//*****************************************************************************


#include "stdafx.h"

#include "dacdbiinterface.h"

#include "typestring.h"
#include "holder.h"
#include "debuginfostore.h"
#include "peimagelayout.inl"
#include "encee.h"
#include "switches.h"
#include "generics.h"
#include "stackwalk.h"
#include "virtualcallstub.h"

#include "dacdbiimpl.h"

#ifdef FEATURE_COMINTEROP
#include "runtimecallablewrapper.h"
#include "comcallablewrapper.h"
#endif // FEATURE_COMINTEROP

#include "request_common.h"

//-----------------------------------------------------------------------------
// Have standard enter and leave macros at the DacDbi boundary to enforce
// standard behavior.
// 1. catch exceptions and convert them at the boundary.
// 2. provide a space to hook logging and transitions.
// 3. provide a hook to verify return values.
//
// Usage notes:
// - use this at the DacDbi boundary; but not at internal functions
// - it's ok to Return from the middle.
//
// Expected usage is:
//  Foo()
//  {
//      DD_ENTER_MAY_THROW
//      ...
//      if (...) { ThrowHr(E_SOME_FAILURE); }
//      ...
//      if (...) { return; } // early success case
//      ...
//  }
//-----------------------------------------------------------------------------




// Global allocator for DD. Access is protected under the g_dacCritSec lock.
IDacDbiInterface::IAllocator * g_pAllocator = NULL;

//---------------------------------------------------------------------------------------
//
// Extra sugar for wrapping IAllocator under friendly New/Delete operators.
//
// Sample usage:
//     void Foo(TestClass ** ppOut)
//     {
//        *ppOut = NULL;
//        TestClass * p = new (forDbi) TestClass();
//        ...
//        if (ok)
//        {
//            *ppOut = p;
//            return; // DBI will then free this memory.
//        }
//        ...
//        DeleteDbiMemory(p); // DeleteDbiMemory(p, len); if it was an array allocation.
//     }
//
//     Be very careful when using this on classes since Dbi and DAC may be in
//     separate dlls. This is best used when operating on blittable data-structures.
//     (no ctor/dtor, plain data fields) to guarantee the proper DLL isolation.
//     You don't want to call the ctor in DAC's context and the dtor in DBI's context
//     unless you really know what you're doing and that it's safe.
//

// Need a class to serve as a tag that we can use to overload New/Delete.
forDbiWorker forDbi;

void * operator new(size_t lenBytes, const forDbiWorker &)
{
    _ASSERTE(g_pAllocator != NULL);
    void *result = g_pAllocator->Alloc(lenBytes);
    if (result == NULL)
    {
        ThrowOutOfMemory();
    }
    return result;
}

void * operator new[](size_t lenBytes, const forDbiWorker &)
{
    _ASSERTE(g_pAllocator != NULL);
    void *result = g_pAllocator->Alloc(lenBytes);
    if (result == NULL)
    {
        ThrowOutOfMemory();
    }
    return result;
}

// Note: there is no C++ syntax for manually invoking this, but if a constructor throws an exception I understand that
// this delete operator will be invoked automatically to destroy the object.
void operator delete(void *p, const forDbiWorker &)
{
    if (p == NULL)
    {
        return;
    }

    _ASSERTE(g_pAllocator != NULL);
    g_pAllocator->Free((BYTE*) p);

}

// Note: there is no C++ syntax for manually invoking this, but if a constructor throws an exception I understand that
// this delete operator will be invoked automatically to destroy the object.
void operator delete[](void *p, const forDbiWorker &)
{
    if (p == NULL)
    {
        return;
    }

    _ASSERTE(g_pAllocator != NULL);
    g_pAllocator->Free((BYTE*) p);
}

// @dbgtodo  dac support: determine how to handle an array of class instances to ensure the dtors get
// called correctly or document that they won't
// Delete memory and invoke dtor for memory allocated with 'operator (forDbi) new'
template<class T> void DeleteDbiMemory(T *p)
{
    if (p == NULL)
    {
        return;
    }
    p->~T();

    _ASSERTE(g_pAllocator != NULL);
    g_pAllocator->Free((BYTE*) p);
}

void* AllocDbiMemory(size_t size)
{
    void *result;
    if (g_pAllocator != nullptr)
    {
        result = g_pAllocator->Alloc(size);
    }
    else
    {
        result = new (nothrow) BYTE[size];
    }
    if (result == NULL)
    {
        ThrowOutOfMemory();
    }
    return result;
}

void DeleteDbiMemory(void* p)
{
    if (p == NULL)
    {
        return;
    }
    if (g_pAllocator != nullptr)
    {
        g_pAllocator->Free((BYTE*)p);
    }
    else
    {
        ::delete [] (BYTE*)p;
    }
}

// Delete memory and invoke dtor for memory allocated with 'operator (forDbi) new[]'
// There's an inherent risk here - where each element's destructor will get called within
// the context of the DAC. If the destructor tries to use the CRT allocator logic expecting
// to hit the DBI's, we could be in trouble. Those objects need to use an export allocator like this.
template<class T> void DeleteDbiArrayMemory(T *p, int count)
{
    if (p == NULL)
    {
        return;
    }

    for (T *cur = p; cur < p + count; cur++)
    {
        cur->~T();
    }

    _ASSERTE(g_pAllocator != NULL);
    g_pAllocator->Free((BYTE*) p);
}

//---------------------------------------------------------------------------------------
// Creates the DacDbiInterface object, used by Dbi.
//
// Arguments:
//    pTarget     - pointer to a Data-Target
//    baseAddress - non-zero base address of mscorwks in target to debug.
//    pAllocator  - pointer to client allocator object. This lets DD allocate objects and
//                  pass them out back to the client, which can then delete them.
//                  DD takes a weak ref to this, so client must keep it alive until it
//                  calls Destroy.
//    pMetadataLookup - callback interface to do internal metadata lookup. This is because
//                  metadata is not dac-ized.
//    ppInterface - mandatory out-parameter
//
// Return Value:
//    S_OK on success.
//
//
// Notes:
//    On Windows, this is public function that can be retrieved by GetProcAddress.

//    On Mac, this is used internally by DacDbiMarshalStubInstance below
//    This will yield an IDacDbiInterface to provide structured access to the
//    data-target.
//
//    Must call Destroy to on interface to free its resources.
//
//---------------------------------------------------------------------------------------
STDAPI
DLLEXPORT
DacDbiInterfaceInstance(
    ICorDebugDataTarget * pTarget,
    CORDB_ADDRESS baseAddress,
    IDacDbiInterface::IAllocator * pAllocator,
    IDacDbiInterface::IMetaDataLookup * pMetaDataLookup,
    IDacDbiInterface ** ppInterface)
{
    // No marshalling is done by the instantiationf function - we just need to setup the infrastructure.
    // We don't want to warn if this involves creating and accessing undacized data structures,
    // because it's for the infrastructure, not DACized code itself.
    SUPPORTS_DAC_HOST_ONLY;

    // Since this is public, verify it.
    if ((ppInterface == NULL) || (pTarget == NULL) || (baseAddress == 0))
    {
        return E_INVALIDARG;
    }

    *ppInterface = NULL;

    //
    // Actually allocate the real object and initialize it.
    //
    DacDbiInterfaceImpl * pDac = new (nothrow) DacDbiInterfaceImpl(pTarget, baseAddress, pAllocator, pMetaDataLookup);
    if (!pDac)
    {
        return E_OUTOFMEMORY;
    }

    HRESULT hrStatus = pDac->Initialize();

    if (SUCCEEDED(hrStatus))
    {
        *ppInterface = pDac;
    }
    else
    {
        delete pDac;
    }
    return hrStatus;
}


//---------------------------------------------------------------------------------------
// Constructor. Instantiates a DAC/DBI interface around a DataTarget.
//
// Arguments:
//    pTarget     - pointer to a Data-Target
//    baseAddress - non-zero base address of mscorwks in target to debug.
//    pAllocator  - pointer to client allocator object. This lets DD allocate objects and
//                  pass them out back to the client, which can then delete them.
//                  DD takes a weak ref to this, so client must keep it alive until it
//                  calls Destroy.
//    pMetadataLookup - callback interface to do internal metadata lookup. This is because
//                  metadata is not dac-ized.
//
// Notes:
//    pAllocator is a weak reference.
//---------------------------------------------------------------------------------------
DacDbiInterfaceImpl::DacDbiInterfaceImpl(
    ICorDebugDataTarget* pTarget,
    CORDB_ADDRESS baseAddress,
    IAllocator * pAllocator,
    IMetaDataLookup * pMetaDataLookup
) : ClrDataAccess(pTarget),
    m_pAllocator(pAllocator),
    m_pMetaDataLookup(pMetaDataLookup),
    m_pCachedPEAssembly(VMPTR_PEAssembly::NullPtr()),
    m_pCachedImporter(NULL),
    m_isCachedHijackFunctionValid(FALSE)
{
    _ASSERTE(baseAddress != NULL);
    m_globalBase = CORDB_ADDRESS_TO_TADDR(baseAddress);

    _ASSERTE(pMetaDataLookup != NULL);
    _ASSERTE(pAllocator != NULL);
    _ASSERTE(pTarget != NULL);

#ifdef _DEBUG
    // Enable verification asserts in ICorDebug scenarios.  ICorDebug never guesses at the DAC path, so any
    // mismatch should be fatal, and so always of interest to the user.
    // This overrides the assignment in the base class ctor (which runs first).
    m_fEnableDllVerificationAsserts = true;
#endif
}

//-----------------------------------------------------------------------------
// Destructor.
//
// Notes:
//    This gets invoked after Destroy().
//-----------------------------------------------------------------------------
DacDbiInterfaceImpl::~DacDbiInterfaceImpl()
{
    SUPPORTS_DAC_HOST_ONLY;
    // This will automatically chain to the base class dtor
}

//-----------------------------------------------------------------------------
// Called from DAC-ized code to get a IMDInternalImport
//
// Arguments:
//    pPEAssembly - PE file for which to get importer for
//    fThrowEx - if true, throw instead of returning NULL.
//
// Returns:
//    an Internal importer object for this file.
//    May return NULL or throw (depending on fThrowEx).
//    May throw in exceptional circumstances (eg, corrupt debuggee).
//
// Assumptions:
//    This is called from DAC-ized code within the VM, which
//    was in turn called from some DD primitive. The returned importer will
//    be used by the DAC-ized code in the callstack, but it won't be cached.
//
// Notes:
//    This is an Internal importer, not a public Metadata importer.
//
interface IMDInternalImport* DacDbiInterfaceImpl::GetMDImport(
    const PEAssembly* pPEAssembly,
    const ReflectionModule * pReflectionModule,
    bool fThrowEx)
{
    // Since this is called from an existing DAC-primitive, we already hold the g_dacCritSec lock.
    // The lock conveniently protects our cache.
    SUPPORTS_DAC;

    IDacDbiInterface::IMetaDataLookup * pLookup = m_pMetaDataLookup;
    _ASSERTE(pLookup != NULL);

    VMPTR_PEAssembly vmPEAssembly = VMPTR_PEAssembly::NullPtr();

    if (pPEAssembly != NULL)
    {
        vmPEAssembly.SetHostPtr(pPEAssembly);
    }
    else if (pReflectionModule != NULL)
    {
        // SOS and ClrDataAccess rely on special logic to find the metadata for methods in dynamic modules.
        // We don't need to.  The RS has already taken care of the special logic for us.
        // So here we just grab the PEAssembly off of the ReflectionModule and continue down the normal
        // code path.  See code:ClrDataAccess::GetMDImport for comparison.
        vmPEAssembly.SetHostPtr(pReflectionModule->GetPEAssembly());
    }

    // Optimize for the case where the VM queries the same Importer many times in a row.
    if (m_pCachedPEAssembly == vmPEAssembly)
    {
        return m_pCachedImporter;
    }

    // Go to DBI to find the metadata.
    IMDInternalImport * pInternal = NULL;
    bool isILMetaDataForNI = false;
    EX_TRY
    {
        // If test needs it in the future, prop isILMetaDataForNI back up to
        // ClrDataAccess.m_mdImports.Add() call.
        // example in code:ClrDataAccess::GetMDImport
        // CordbModule::GetMetaDataInterface also looks up MetaData and would need attention.

        // This is the new codepath that uses ICorDebugMetaDataLookup.
        // To get the old codepath that uses the v2 metadata lookup methods,
        // you'd have to load DAC only and then you'll get ClrDataAccess's implementation
        // of this function.
        pInternal = pLookup->LookupMetaData(vmPEAssembly, isILMetaDataForNI);
    }
    EX_CATCH
    {
        // Any expected error we should ignore.
        if ((GET_EXCEPTION()->GetHR() != HRESULT_FROM_WIN32(ERROR_PARTIAL_COPY)) &&
            (GET_EXCEPTION()->GetHR() != CORDBG_E_READVIRTUAL_FAILURE) &&
            (GET_EXCEPTION()->GetHR() != CORDBG_E_SYMBOLS_NOT_AVAILABLE) &&
            (GET_EXCEPTION()->GetHR() != CORDBG_E_MODULE_LOADED_FROM_DISK))
        {
            EX_RETHROW;
        }
    }
    EX_END_CATCH(SwallowAllExceptions)

    if (pInternal == NULL)
    {
        SIMPLIFYING_ASSUMPTION(!"MD lookup failed");
        if (fThrowEx)
        {
            ThrowHR(E_FAIL);
        }
        return NULL;
    }
    else
    {
        // Cache it such that it we look for the exact same Importer again, we'll return it.
        m_pCachedPEAssembly   = vmPEAssembly;
        m_pCachedImporter = pInternal;
    }

    return pInternal;
}

//-----------------------------------------------------------------------------
// Implementation of IDacDbiInterface
// See DacDbiInterface.h for full descriptions of all of these functions
//-----------------------------------------------------------------------------

// Destroy the connection, freeing up any resources.
void DacDbiInterfaceImpl::Destroy()
{
    m_pAllocator = NULL;

    this->Release();
    // Memory is deleted, don't access this object any more
}

// Check whether the version of the DBI matches the version of the runtime.
// See code:CordbProcess::CordbProcess#DBIVersionChecking for more information regarding version checking.
HRESULT DacDbiInterfaceImpl::CheckDbiVersion(const DbiVersion * pVersion)
{
    DD_ENTER_MAY_THROW;

    if (pVersion->m_dwFormat != kCurrentDbiVersionFormat)
    {
        return CORDBG_E_INCOMPATIBLE_PROTOCOL;
    }

    if ((pVersion->m_dwProtocolBreakingChangeCounter != kCurrentDacDbiProtocolBreakingChangeCounter) ||
        (pVersion->m_dwReservedMustBeZero1 != 0))
    {
        return CORDBG_E_INCOMPATIBLE_PROTOCOL;
    }

    return S_OK;
}

// Flush the DAC cache. This should be called when target memory changes.
HRESULT DacDbiInterfaceImpl::FlushCache()
{
    // Non-reentrant. We don't want to flush cached instances from a callback.
    // That would remove host DAC instances while they're being used.
    DD_NON_REENTRANT_MAY_THROW;

    m_pCachedPEAssembly = VMPTR_PEAssembly::NullPtr();
    m_pCachedImporter = NULL;
    m_isCachedHijackFunctionValid = FALSE;

    HRESULT hr  = ClrDataAccess::Flush();

    // Current impl of Flush() should always succeed. If it ever fails, we want to know.
    _ASSERTE(SUCCEEDED(hr));
    return hr;
}

// enable or disable DAC target consistency checks
void DacDbiInterfaceImpl::DacSetTargetConsistencyChecks(bool fEnableAsserts)
{
    // forward on to our ClrDataAccess base class
    ClrDataAccess::SetTargetConsistencyChecks(fEnableAsserts);
}

// Query if Left-side is started up?
BOOL DacDbiInterfaceImpl::IsLeftSideInitialized()
{
    DD_ENTER_MAY_THROW;

    if (g_pDebugger != NULL)
    {
        // This check is "safe".
        // The initialize order in the left-side is:
        // 1) g_pDebugger is an RVA based global initialized to NULL when the module is loaded.
        // 2) Allocate a "Debugger" object.
        // 3) run the ctor, which will set m_fLeftSideInitialized = FALSE.
        // 4) assign the object to g_pDebugger.
        // 5) later, LS initialization code will assign g_pDebugger->m_fLeftSideInitialized = TRUE.
        //
        // The memory write in #5 is atomic.  There is no window where we're reading uninitialized data.

        return (g_pDebugger->m_fLeftSideInitialized != 0);
    }

    return FALSE;
}


// Determines if a given address is a CLR stub.
BOOL DacDbiInterfaceImpl::IsTransitionStub(CORDB_ADDRESS address)
{
    DD_ENTER_MAY_THROW;

    BOOL fIsStub = FALSE;

#if defined(TARGET_UNIX)
    // Currently IsIPInModule() is not implemented in the PAL.  Rather than skipping the check, we should
    // either E_NOTIMPL this API or implement IsIPInModule() in the PAL.  Since ICDProcess::IsTransitionStub()
    // is only called by VS in mixed-mode debugging scenarios, and mixed-mode debugging is not supported on
    // POSIX systems, there is really no incentive to implement this API at this point.
    ThrowHR(E_NOTIMPL);

#else // !TARGET_UNIX

    TADDR ip = (TADDR)address;

    if (ip == NULL)
    {
        fIsStub = FALSE;
    }
    else
    {
        fIsStub = StubManager::IsStub(ip);
    }

    // If it's in Mscorwks, count that as a stub too.
    if (fIsStub == FALSE)
    {
        fIsStub = IsIPInModule(m_globalBase, ip);
    }

#endif // TARGET_UNIX

    return fIsStub;
}

// Gets the type of 'address'.
IDacDbiInterface::AddressType DacDbiInterfaceImpl::GetAddressType(CORDB_ADDRESS address)
{
    DD_ENTER_MAY_THROW;
    TADDR taAddr = CORDB_ADDRESS_TO_TADDR(address);

    if (IsPossibleCodeAddress(taAddr) == S_OK)
    {
        if (ExecutionManager::IsManagedCode(taAddr))
        {
            return kAddressManagedMethod;
        }

        if (StubManager::IsStub(taAddr))
        {
            return kAddressRuntimeUnmanagedStub;
        }
    }

    return kAddressUnrecognized;
}


// Get a VM appdomain pointer that matches the appdomain ID
VMPTR_AppDomain DacDbiInterfaceImpl::GetAppDomainFromId(ULONG appdomainId)
{
    DD_ENTER_MAY_THROW;

    VMPTR_AppDomain vmAppDomain;

    // @dbgtodo   dac support - We would like to wean ourselves off the IXClrData interfaces.
    IXCLRDataProcess *   pDAC = this;
    ReleaseHolder<IXCLRDataAppDomain> pDacAppDomain;

    HRESULT hrStatus = pDAC->GetAppDomainByUniqueID(appdomainId, &pDacAppDomain);
    IfFailThrow(hrStatus);

    IXCLRDataAppDomain * pIAppDomain = pDacAppDomain;
    AppDomain * pAppDomain = (static_cast<ClrDataAppDomain *> (pIAppDomain))->GetAppDomain();
    SIMPLIFYING_ASSUMPTION(pAppDomain != NULL);
    if (pAppDomain == NULL)
    {
        ThrowHR(E_FAIL); // corrupted left-side?
    }

    TADDR addrAppDomain = PTR_HOST_TO_TADDR(pAppDomain);
    vmAppDomain.SetDacTargetPtr(addrAppDomain);

    return vmAppDomain;
}


// Get the AppDomain ID for an AppDomain.
ULONG DacDbiInterfaceImpl::GetAppDomainId(VMPTR_AppDomain   vmAppDomain)
{
    DD_ENTER_MAY_THROW;

    if (vmAppDomain.IsNull())
    {
        return 0;
    }
    else
    {
        AppDomain * pAppDomain = vmAppDomain.GetDacPtr();
        return DefaultADID;
    }
}

// Get the managed AppDomain object for an AppDomain.
VMPTR_OBJECTHANDLE DacDbiInterfaceImpl::GetAppDomainObject(VMPTR_AppDomain vmAppDomain)
{
    DD_ENTER_MAY_THROW;

    AppDomain* pAppDomain = vmAppDomain.GetDacPtr();
    OBJECTHANDLE hAppDomainManagedObject = pAppDomain->GetRawExposedObjectHandleForDebugger();
    VMPTR_OBJECTHANDLE vmObj = VMPTR_OBJECTHANDLE::NullPtr();
    vmObj.SetDacTargetPtr(hAppDomainManagedObject);
    return vmObj;

}

// Get the full AD friendly name for the given EE AppDomain.
void DacDbiInterfaceImpl::GetAppDomainFullName(
    VMPTR_AppDomain   vmAppDomain,
    IStringHolder *   pStrName )
{
    DD_ENTER_MAY_THROW;
    AppDomain * pAppDomain = vmAppDomain.GetDacPtr();

    // Get the AppDomain name from the VM without changing anything
    // We might be able to simplify this, eg. by returning an SString.
    bool fIsUtf8;
    PVOID pRawName = pAppDomain->GetFriendlyNameNoSet(&fIsUtf8);

    if (!pRawName)
    {
        ThrowHR(E_NOINTERFACE);
    }

    HRESULT hrStatus = S_OK;
    if (fIsUtf8)
    {
        // we have to allocate a temporary string
        // we could avoid this by adding a version of IStringHolder::AssignCopy that takes a UTF8 string
        // We should also probably check to see when fIsUtf8 is ever true (it looks like it should normally be false).
        ULONG32 dwNameLen = 0;
        hrStatus = ConvertUtf8((LPCUTF8)pRawName, 0, &dwNameLen, NULL);
        if (SUCCEEDED( hrStatus ))
        {
            NewArrayHolder<WCHAR> pwszName(new WCHAR[dwNameLen]);
            hrStatus = ConvertUtf8((LPCUTF8)pRawName, dwNameLen, &dwNameLen, pwszName );
            IfFailThrow(hrStatus);

            hrStatus =  pStrName->AssignCopy(pwszName);
        }
    }
    else
    {
        hrStatus =  pStrName->AssignCopy(static_cast<PCWSTR>(pRawName));
    }

    // Very important that this either sets pStrName or Throws.
    IfFailThrow(hrStatus);
}

//- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
// JIT Compiler Flags
//- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

// Get the values of the JIT Optimization and EnC flags.
void DacDbiInterfaceImpl::GetCompilerFlags (
    VMPTR_DomainAssembly vmDomainAssembly,
    BOOL *pfAllowJITOpts,
    BOOL *pfEnableEnC)
{
    DD_ENTER_MAY_THROW;

    DomainAssembly * pDomainAssembly = vmDomainAssembly.GetDacPtr();

    if (pDomainAssembly == NULL)
    {
        ThrowHR(E_FAIL);
    }

    // Get the underlying module - none of this is AppDomain specific
    Module * pModule = pDomainAssembly->GetModule();
    DWORD dwBits = pModule->GetDebuggerInfoBits();
    *pfAllowJITOpts = !CORDisableJITOptimizations(dwBits);
    *pfEnableEnC = pModule->IsEditAndContinueEnabled();


} //GetCompilerFlags

//-----------------------------------------------------------------------------
// Helper function for SetCompilerFlags to set EnC status.
// Arguments:
//     Input:
//         pModule - The runtime module for which flags are being set.
//
// Return value:
//   true if the Enc bits can be set on this module
//-----------------------------------------------------------------------------

bool DacDbiInterfaceImpl::CanSetEnCBits(Module * pModule)
{
    _ASSERTE(pModule != NULL);
#ifdef FEATURE_METADATA_UPDATER
    // If we're using explicit sequence points (from the PDB), then we can't do EnC
    // because EnC won't get updated pdbs and so the sequence points will be wrong.
    bool fIgnorePdbs = ((pModule->GetDebuggerInfoBits() & DACF_IGNORE_PDBS) != 0);

    bool fAllowEnc = pModule->IsEditAndContinueCapable() &&

#ifdef PROFILING_SUPPORTED_DATA
        !CORProfilerPresent() && // this queries target
#endif
        fIgnorePdbs;
#else   // ! FEATURE_METADATA_UPDATER
    // Enc not supported on any other platforms.
    bool fAllowEnc = false;
#endif

    return fAllowEnc;
} // DacDbiInterfaceImpl::SetEnCBits

// Set the values of the JIT optimization and EnC flags.
HRESULT DacDbiInterfaceImpl::SetCompilerFlags(VMPTR_DomainAssembly vmDomainAssembly,
                                           BOOL             fAllowJitOpts,
                                           BOOL             fEnableEnC)
{
    DD_ENTER_MAY_THROW;

    DWORD        dwBits      = 0;
    DomainAssembly * pDomainAssembly = vmDomainAssembly.GetDacPtr();
    Module *     pModule     = pDomainAssembly->GetModule();
    HRESULT      hr          = S_OK;


    _ASSERTE(pModule != NULL);

    // Initialize dwBits.
    dwBits = (pModule->GetDebuggerInfoBits() & ~(DACF_ALLOW_JIT_OPTS | DACF_ENC_ENABLED));
    dwBits &= DACF_CONTROL_FLAGS_MASK;

    if (fAllowJitOpts)
    {
        dwBits |= DACF_ALLOW_JIT_OPTS;
    }
    if (fEnableEnC)
    {
        if (CanSetEnCBits(pModule))
        {
            dwBits |= DACF_ENC_ENABLED;
        }
        else
        {
            hr = CORDBG_S_NOT_ALL_BITS_SET;
        }
    }
    // Settings from the debugger take precedence over all other settings.
    dwBits |= DACF_USER_OVERRIDE;

    // set flags. This will write back to the target
    pModule->SetDebuggerInfoBits((DebuggerAssemblyControlFlags)dwBits);


    LOG((LF_CORDB, LL_INFO100, "D::HIPCE, Changed Jit-Debug-Info: fOpt=%d, fEnableEnC=%d, new bits=0x%08x\n",
           (dwBits & DACF_ALLOW_JIT_OPTS) != 0,
           (dwBits & DACF_ENC_ENABLED) != 0,
            dwBits));

    _ASSERTE(SUCCEEDED(hr));
    return hr;

} // DacDbiInterfaceImpl::SetCompilerFlags


//- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
// sequence points and var info
//- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

// Initialize the native/IL sequence points and native var info for a function.
void DacDbiInterfaceImpl::GetNativeCodeSequencePointsAndVarInfo(VMPTR_MethodDesc  vmMethodDesc,
                                                                CORDB_ADDRESS     startAddr,
                                                                BOOL              fCodeAvailable,
                                                                NativeVarData *   pNativeVarData,
                                                                SequencePoints *  pSequencePoints)
{
    DD_ENTER_MAY_THROW;

    _ASSERTE(!vmMethodDesc.IsNull());

    MethodDesc * pMD = vmMethodDesc.GetDacPtr();

    _ASSERTE(fCodeAvailable != 0);

    // get information about the locations of arguments and local variables
    GetNativeVarData(pMD, startAddr, GetArgCount(pMD), pNativeVarData);

    // get the sequence points
    GetSequencePoints(pMD, startAddr, pSequencePoints);

} // GetNativeCodeSequencePointsAndVarInfo

//-----------------------------------------------------------------------------
// Get the number of fixed arguments to a function, i.e., the explicit args and the "this" pointer.
// This does not include other implicit arguments or varargs. This is used to compute a variable ID
// (see comment in CordbJITILFrame::ILVariableToNative for more detail)
// Arguments:
//    input:  pMD    pointer to the method desc for the function
//    output: none
// Return value:
//    the number of fixed arguments to the function
//-----------------------------------------------------------------------------
SIZE_T DacDbiInterfaceImpl::GetArgCount(MethodDesc * pMD)
{

    // Create a MetaSig for the given method's sig. (Easier than
    // picking the sig apart ourselves.)
    PCCOR_SIGNATURE pCallSig;
    DWORD cbCallSigSize;

    pMD->GetSig(&pCallSig, &cbCallSigSize);

    if (pCallSig == NULL)
    {
        // Sig should only be null if the image is corrupted. (Even for lightweight-codegen)
        // We expect the jit+verifier to catch this, so that we never land here.
        // But just in case ...
        CONSISTENCY_CHECK_MSGF(false, ("Corrupted image, null sig.(%s::%s)",
                               pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName));
        return 0;
    }

    MetaSig msig(pCallSig, cbCallSigSize, pMD->GetModule(), NULL, MetaSig::sigMember);

    // Get the arg count.
    UINT32 NumArguments = msig.NumFixedArgs();

    // Account for the 'this' argument.
    if (!pMD->IsStatic())
    {
        NumArguments++;
    }
/*
    SigParser sigParser(pCallSig, cbCallSigSize);
    sigParser.SkipMethodHeaderSignature(&m_allArgsCount);
*/
    return NumArguments;
} //GetArgCount

// Allocator to pass to the debug-info-stores...
BYTE* InfoStoreNew(void * pData, size_t cBytes)
{
    return new BYTE[cBytes];
}

//-----------------------------------------------------------------------------
// Get locations and code offsets for local variables and arguments in a function
// This information is used to find the location of a value at a given IP.
// Arguments:
//    input:
//        pMethodDesc   pointer to the method desc for the function
//        startAddr     starting address of the function--used to differentiate
//                      EnC versions
//        fixedArgCount number of fixed arguments to the function
//    output:
//        pVarInfo      data structure containing a list of variable and
//                      argument locations by range of IP offsets
// Note: this function may throw
//-----------------------------------------------------------------------------
void DacDbiInterfaceImpl::GetNativeVarData(MethodDesc *    pMethodDesc,
                                           CORDB_ADDRESS   startAddr,
                                           SIZE_T          fixedArgCount,
                                           NativeVarData * pVarInfo)
{
    // make sure we haven't done this already
    if (pVarInfo->IsInitialized())
    {
        return;
    }

    NewArrayHolder<ICorDebugInfo::NativeVarInfo> nativeVars(NULL);

    DebugInfoRequest request;
    request.InitFromStartingAddr(pMethodDesc, CORDB_ADDRESS_TO_TADDR(startAddr));

    ULONG32 entryCount;

    BOOL success = DebugInfoManager::GetBoundariesAndVars(request,
                                                InfoStoreNew, NULL, // allocator
                                                NULL, NULL,
                                                &entryCount, &nativeVars);

    if (!success)
        ThrowHR(E_FAIL);

    // set key fields of pVarInfo
    pVarInfo->InitVarDataList(nativeVars, (int)fixedArgCount, (int)entryCount);
} // GetNativeVarData


//-----------------------------------------------------------------------------
// Given a instrumented IL map from the profiler that maps:
//   Original offset IL_A -> Instrumentend offset IL_B
// And a native mapping from the JIT that maps:
//   Instrumented offset IL_B -> native offset Native_C
// This function merges the two maps and stores the result back into the nativeMap.
// The nativeMap now maps:
//   Original offset IL_A -> native offset Native_C
// pEntryCount is the number of valid entries in nativeMap, and it may be adjusted downwards
// as part of the composition.
//-----------------------------------------------------------------------------
void DacDbiInterfaceImpl::ComposeMapping(const InstrumentedILOffsetMapping * pProfilerILMap, ICorDebugInfo::OffsetMapping nativeMap[], ULONG32* pEntryCount)
{
    // Translate the IL offset if the profiler has provided us with a mapping.
    // The ICD public API should always expose the original IL offsets, but GetBoundaries()
    // directly accesses the debug info, which stores the instrumented IL offsets.

    ULONG32 entryCount = *pEntryCount;
    // The map pointer could be NULL or there could be no entries in the map, in either case no work to do
    if (pProfilerILMap && !pProfilerILMap->IsNull())
    {
        // If we did instrument, then we can't have any sequence points that
        // are "in-between" the old-->new map that the profiler gave us.
        // Ex, if map is:
        // (6 old -> 36 new)
        // (8 old -> 50 new)
        // And the jit gives us an entry for 44 new, that will map back to 6 old.
        // Since the map can only have one entry for 6 old, we remove 44 new.

        // First Pass: invalidate all the duplicate entries by setting their IL offset to MAX_ILNUM
        ULONG32 cDuplicate = 0;
        ULONG32 prevILOffset = (ULONG32)(ICorDebugInfo::MAX_ILNUM);
        for (ULONG32 i = 0; i < entryCount; i++)
        {
            ULONG32 origILOffset = TranslateInstrumentedILOffsetToOriginal(nativeMap[i].ilOffset, pProfilerILMap);

            if (origILOffset == prevILOffset)
            {
                // mark this sequence point as invalid; refer to the comment above
                nativeMap[i].ilOffset = (ULONG32)(ICorDebugInfo::MAX_ILNUM);
                cDuplicate += 1;
            }
            else
            {
                // overwrite the instrumented IL offset with the original IL offset
                nativeMap[i].ilOffset = origILOffset;
                prevILOffset = origILOffset;
            }
        }

        // Second Pass: move all the valid entries up front
        ULONG32 realIndex = 0;
        for (ULONG32 curIndex = 0; curIndex < entryCount; curIndex++)
        {
            if (nativeMap[curIndex].ilOffset != (ULONG32)(ICorDebugInfo::MAX_ILNUM))
            {
                // This is a valid entry.  Move it up front.
                nativeMap[realIndex] = nativeMap[curIndex];
                realIndex += 1;
            }
        }

        // make sure we have done the bookkeeping correctly
        _ASSERTE((realIndex + cDuplicate) == entryCount);

        // Final Pass: derecement entryCount
        entryCount -= cDuplicate;
        *pEntryCount = entryCount;
    }
}


//-----------------------------------------------------------------------------
// Get the native/IL sequence points for a function
// Arguments:
//    input:
//        pMethodDesc   pointer to the method desc for the function
//        startAddr     starting address of the function--used to differentiate
//    output:
//        pNativeMap    data structure containing a list of sequence points
// Note: this function may throw
//-----------------------------------------------------------------------------
void DacDbiInterfaceImpl::GetSequencePoints(MethodDesc *     pMethodDesc,
                                            CORDB_ADDRESS    startAddr,
                                            SequencePoints * pSeqPoints)
{

    // make sure we haven't done this already
    if (pSeqPoints->IsInitialized())
    {
        return;
    }

    // Use the DebugInfoStore to get IL->Native maps.
    // It doesn't matter whether we're jitted, ngenned etc.
    DebugInfoRequest request;
    request.InitFromStartingAddr(pMethodDesc, CORDB_ADDRESS_TO_TADDR(startAddr));


    // Bounds info.
    NewArrayHolder<ICorDebugInfo::OffsetMapping> mapCopy(NULL);

    ULONG32 entryCount;
    BOOL success = DebugInfoManager::GetBoundariesAndVars(request,
                                                      InfoStoreNew, NULL, // allocator
                                                      &entryCount, &mapCopy,
                                                      NULL, NULL);
    if (!success)
        ThrowHR(E_FAIL);

#ifdef FEATURE_REJIT
    CodeVersionManager * pCodeVersionManager = pMethodDesc->GetCodeVersionManager();
    ILCodeVersion ilVersion;
    NativeCodeVersion nativeCodeVersion = pCodeVersionManager->GetNativeCodeVersion(dac_cast<PTR_MethodDesc>(pMethodDesc), (PCODE)startAddr);
    if (!nativeCodeVersion.IsNull())
    {
        ilVersion = nativeCodeVersion.GetILCodeVersion();
    }

    // if there is a rejit IL map for this function, apply that in preference to load-time mapping
    if (!ilVersion.IsNull() && !ilVersion.IsDefaultVersion())
    {
        const InstrumentedILOffsetMapping * pRejitMapping = ilVersion.GetInstrumentedILMap();
        ComposeMapping(pRejitMapping, mapCopy, &entryCount);
    }
    else
    {
#endif
        // if there is a profiler load-time mapping and not a rejit mapping, apply that instead
        InstrumentedILOffsetMapping loadTimeMapping =
            pMethodDesc->GetModule()->GetInstrumentedILOffsetMapping(pMethodDesc->GetMemberDef());
        ComposeMapping(&loadTimeMapping, mapCopy, &entryCount);
#ifdef FEATURE_REJIT
    }
#endif

    pSeqPoints->InitSequencePoints(entryCount);

    // mapCopy and pSeqPoints have elements of different types. Thus, we
    // need to copy the individual members from the elements of mapCopy to the
    // elements of pSeqPoints. Once we're done, we can release mapCopy
    pSeqPoints->CopyAndSortSequencePoints(mapCopy);

} // GetSequencePoints

// ----------------------------------------------------------------------------
// DacDbiInterfaceImpl::TranslateInstrumentedILOffsetToOriginal
//
// Description:
//    Helper function to convert an instrumented IL offset to the corresponding original IL offset.
//
// Arguments:
//    * ilOffset - offset to be translated
//    * pMapping - the profiler-provided mapping between original IL offsets and instrumented IL offsets
//
// Return Value:
//    Return the translated offset.
//

ULONG DacDbiInterfaceImpl::TranslateInstrumentedILOffsetToOriginal(ULONG                               ilOffset,
                                                                   const InstrumentedILOffsetMapping * pMapping)
{
    SIZE_T               cMap  = pMapping->GetCount();
    ARRAY_PTR_COR_IL_MAP rgMap = pMapping->GetOffsets();

    _ASSERTE((cMap == 0) == (rgMap == NULL));

    // Early out if there is no mapping, or if we are dealing with a special IL offset such as
    // prolog, epilog, etc.
    if ((cMap == 0) || ((int)ilOffset < 0))
    {
        return ilOffset;
    }

    SIZE_T i = 0;
    for (i = 1; i < cMap; i++)
    {
        if (ilOffset < rgMap[i].newOffset)
        {
            return rgMap[i - 1].oldOffset;
        }
    }
    return rgMap[i - 1].oldOffset;
}

//- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
// Function Data
//- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -


// GetILCodeAndSig returns the function's ILCode and SigToken given
// a module and a token. The info will come from a MethodDesc, if
// one exists or from metadata.
//
void DacDbiInterfaceImpl::GetILCodeAndSig(VMPTR_DomainAssembly vmDomainAssembly,
                                          mdToken          functionToken,
                                          TargetBuffer *   pCodeInfo,
                                          mdToken *        pLocalSigToken)
{
    DD_ENTER_MAY_THROW;

    DomainAssembly * pDomainAssembly = vmDomainAssembly.GetDacPtr();
    Module *     pModule     = pDomainAssembly->GetModule();
    RVA          methodRVA   = 0;
    DWORD        implFlags;

    // preinitialize out params
    pCodeInfo->Clear();
    *pLocalSigToken = mdSignatureNil;

    // Get the RVA and impl flags for this method.
    IfFailThrow(pModule->GetMDImport()->GetMethodImplProps(functionToken,
                                                           &methodRVA,
                                                           &implFlags));

    MethodDesc* pMethodDesc =
        FindLoadedMethodRefOrDef(pModule, functionToken);

    // If the RVA is 0 or it's native, then the method is not IL
    if (methodRVA == 0)
    {
        LOG((LF_CORDB,LL_INFO100000, "DDI::GICAS: Function is not IL - methodRVA == NULL!\n"));
        // return (CORDBG_E_FUNCTION_NOT_IL);
        // Sanity check this....

        if(!pMethodDesc || !pMethodDesc->IsIL())
        {
            LOG((LF_CORDB,LL_INFO100000, "DDI::GICAS: And the MD agrees..\n"));
            ThrowHR(CORDBG_E_FUNCTION_NOT_IL);
        }
        else
        {
            LOG((LF_CORDB,LL_INFO100000, "DDI::GICAS: But the MD says it's IL..\n"));
        }

        if (pMethodDesc != NULL && pMethodDesc->GetRVA() == 0)
        {
            LOG((LF_CORDB,LL_INFO100000, "DDI::GICAS: Actually, MD says RVA is 0 too - keep going...!\n"));
        }
    }
    if (IsMiNative(implFlags))
    {
        LOG((LF_CORDB,LL_INFO100000, "DDI::GICAS: Function is not IL - IsMiNative!\n"));
        ThrowHR(CORDBG_E_FUNCTION_NOT_IL);
    }

    *pLocalSigToken = GetILCodeAndSigHelper(pModule, pMethodDesc, functionToken, methodRVA, pCodeInfo);

} // GetILCodeAndSig

//---------------------------------------------------------------------------------------
//
// This is just a worker function for GetILCodeAndSig.  It returns the function's ILCode and SigToken
// given a module, a token, and the RVA.  If a MethodDesc is provided, it has to be consistent with
// the token and the RVA.
//
// Arguments:
//    pModule       - the Module containing the specified method
//    pMD           - the specified method; can be NULL
//    mdMethodToken - the MethodDef token of the specified method
//    methodRVA     - the RVA of the IL for the specified method
//    pIL           - out parameter; return the target address and size of the IL of the specified method
//
// Return Value:
//    Return the local variable signature token of the specified method.  Can be mdSignatureNil.
//

mdSignature DacDbiInterfaceImpl::GetILCodeAndSigHelper(Module *       pModule,
                                                       MethodDesc *   pMD,
                                                       mdMethodDef    mdMethodToken,
                                                       RVA            methodRVA,
                                                       TargetBuffer * pIL)
{
    _ASSERTE(pModule != NULL);

    // If a MethodDesc is provided, it has to be consistent with the MethodDef token and the RVA.
    _ASSERTE((pMD == NULL) || ((pMD->GetMemberDef() == mdMethodToken) && (pMD->GetRVA() == methodRVA)));

    TADDR pTargetIL; // target address of start of IL blob

    // This works for methods in dynamic modules, and methods overridden by a profiler.
    pTargetIL = pModule->GetDynamicIL(mdMethodToken, TRUE);

    // Method not overridden - get the original copy of the IL by going to the PE file/RVA
    // If this is in a dynamic module then don't even attempt this since ReflectionModule::GetIL isn't
    // implemented for DAC.
    if (pTargetIL == 0 && !pModule->IsReflection())
    {
        pTargetIL = (TADDR)pModule->GetIL(methodRVA);
    }

    mdSignature mdSig = mdSignatureNil;
    if (pTargetIL == 0)
    {
        // Currently this should only happen for LCG methods (including IL stubs).
        // LCG methods have a 0 RVA, and so we don't currently have any way to get the IL here.
        _ASSERTE(pMD->IsDynamicMethod());
        _ASSERTE(pMD->AsDynamicMethodDesc()->IsLCGMethod()||
                 pMD->AsDynamicMethodDesc()->IsILStub());

        // Clear the buffer.
        pIL->Clear();
    }
    else
    {
        // Now we have the target address of the IL blob, we need to bring it over to the host.
        // DacGetILMethod will copy the COR_ILMETHOD information that we need
        COR_ILMETHOD * pHostIL = DacGetIlMethod(pTargetIL);     // host address of start of IL blob
        COR_ILMETHOD_DECODER header(pHostIL);                   // host address of header


        // Get the IL code info. We need the address of the IL itself, which will be beyond the header
        // at the beginning of the blob. We ultimately need the target address. To get this, we take
        // target address of the target IL blob and add the offset from the beginning of the host IL blob
        // (the header) to the beginning of the IL itself (we get this information from the header).
        pIL->pAddress = pTargetIL + ((SIZE_T)(header.Code) - (SIZE_T)pHostIL);
        pIL->cbSize = header.GetCodeSize();

        // Now we get the signature token
        if (header.LocalVarSigTok != NULL)
        {
            mdSig = header.GetLocalVarSigTok();
        }
        else
        {
            mdSig = mdSignatureNil;
        }
    }

    return mdSig;
}


bool DacDbiInterfaceImpl::GetMetaDataFileInfoFromPEFile(VMPTR_PEAssembly vmPEAssembly,
                                                        DWORD &dwTimeStamp,
                                                        DWORD &dwSize,
                                                        bool  &isNGEN,
                                                        IStringHolder* pStrFilename)
{
    DD_ENTER_MAY_THROW;

    DWORD dwDataSize;
    DWORD dwRvaHint;
    PEAssembly * pPEAssembly = vmPEAssembly.GetDacPtr();
    _ASSERTE(pPEAssembly != NULL);
    if (pPEAssembly == NULL)
        return false;

    WCHAR wszFilePath[MAX_LONGPATH] = {0};
    DWORD cchFilePath = MAX_LONGPATH;
    bool ret = ClrDataAccess::GetMetaDataFileInfoFromPEFile(pPEAssembly,
                                                            dwTimeStamp,
                                                            dwSize,
                                                            dwDataSize,
                                                            dwRvaHint,
                                                            isNGEN,
                                                            wszFilePath,
                                                            cchFilePath);

    pStrFilename->AssignCopy(wszFilePath);
    return ret;
}


bool DacDbiInterfaceImpl::GetILImageInfoFromNgenPEFile(VMPTR_PEAssembly vmPEAssembly,
                                                       DWORD &dwTimeStamp,
                                                       DWORD &dwSize,
                                                       IStringHolder* pStrFilename)
{

    return false;

}

// Get start addresses and sizes for hot and cold regions for a native code blob.
// Arguments:
//    Input:
//        pMethodDesc - method desc for the function we are inspecting
//    Output (required):
//        pCodeInfo   - initializes the m_rgCodeRegions field of this structure
//                      if the native code is available. Otherwise,
//                      pCodeInfo->IsValid() is false.

void DacDbiInterfaceImpl::GetMethodRegionInfo(MethodDesc *             pMethodDesc,
                                              NativeCodeFunctionData * pCodeInfo)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pCodeInfo));
    }
    CONTRACTL_END;

    IJitManager::MethodRegionInfo methodRegionInfo = {NULL, 0, NULL, 0};
    PCODE functionAddress = pMethodDesc->GetNativeCode();

    // get the start address of the hot region and initialize the jit manager
    pCodeInfo->m_rgCodeRegions[kHot].pAddress = CORDB_ADDRESS(PCODEToPINSTR(functionAddress));

    // if the start address is NULL, the code isn't available yet, so just return
    if (functionAddress != NULL)
    {
        EECodeInfo codeInfo(functionAddress);
        _ASSERTE(codeInfo.IsValid());

        codeInfo.GetMethodRegionInfo(&methodRegionInfo);

        // now get the rest of the region information
        pCodeInfo->m_rgCodeRegions[kHot].cbSize = (ULONG)methodRegionInfo.hotSize;
        pCodeInfo->m_rgCodeRegions[kCold].Init(PCODEToPINSTR(methodRegionInfo.coldStartAddress),
                                               (ULONG)methodRegionInfo.coldSize);
        _ASSERTE(pCodeInfo->IsValid());
    }
    else
    {
        _ASSERTE(!pCodeInfo->IsValid());
    }
} // GetMethodRegionInfo


// Gets the following information about a native code blob:
//    - its method desc
//    - whether it's an instantiated generic
//    - its EnC version number
//    - hot and cold region information.
// If the hot region start address is NULL at the end, it means the native code
// isn't currently available. In this case, all values in pCodeInfo will be
// cleared.

void DacDbiInterfaceImpl::GetNativeCodeInfo(VMPTR_DomainAssembly         vmDomainAssembly,
                                            mdToken                  functionToken,
                                            NativeCodeFunctionData * pCodeInfo)
{
    DD_ENTER_MAY_THROW;

    _ASSERTE(pCodeInfo != NULL);

    // pre-initialize:
    pCodeInfo->Clear();

    DomainAssembly * pDomainAssembly = vmDomainAssembly.GetDacPtr();
    Module *     pModule     = pDomainAssembly->GetModule();

    MethodDesc* pMethodDesc = FindLoadedMethodRefOrDef(pModule, functionToken);
    pCodeInfo->vmNativeCodeMethodDescToken.SetHostPtr(pMethodDesc);

    // if we are loading a module and trying to bind a previously set breakpoint, we may not have
    // a method desc yet, so check for that situation
    if(pMethodDesc != NULL)
    {
        GetMethodRegionInfo(pMethodDesc, pCodeInfo);
        if (pCodeInfo->m_rgCodeRegions[kHot].pAddress != NULL)
        {
            pCodeInfo->isInstantiatedGeneric = pMethodDesc->HasClassOrMethodInstantiation();
            LookupEnCVersions(pModule,
                              pCodeInfo->vmNativeCodeMethodDescToken,
                              functionToken,
                              pCodeInfo->m_rgCodeRegions[kHot].pAddress,
                              &(pCodeInfo->encVersion));
        }
    }
} // GetNativeCodeInfo

// Gets the following information about a native code blob:
//    - its method desc
//    - whether it's an instantiated generic
//    - its EnC version number
//    - hot and cold region information.
void DacDbiInterfaceImpl::GetNativeCodeInfoForAddr(VMPTR_MethodDesc         vmMethodDesc,
                                                   CORDB_ADDRESS            hotCodeStartAddr,
                                                   NativeCodeFunctionData * pCodeInfo)
{
    DD_ENTER_MAY_THROW;

    _ASSERTE(pCodeInfo != NULL);

    if (hotCodeStartAddr == NULL)
    {
        // if the start address is NULL, the code isn't available yet, so just return
        _ASSERTE(!pCodeInfo->IsValid());
        return;
    }

    IJitManager::MethodRegionInfo methodRegionInfo = {NULL, 0, NULL, 0};
    TADDR codeAddr = CORDB_ADDRESS_TO_TADDR(hotCodeStartAddr);

#ifdef TARGET_ARM
    // TADDR should not have the thumb code bit set.
    _ASSERTE((codeAddr & THUMB_CODE) == 0);
    codeAddr &= ~THUMB_CODE;
#endif

    EECodeInfo codeInfo(codeAddr);
    _ASSERTE(codeInfo.IsValid());

    // We may not have the memory for the cold code region in a minidump.
    // Do not fail stackwalking because of this.
    EX_TRY_ALLOW_DATATARGET_MISSING_MEMORY
    {
        codeInfo.GetMethodRegionInfo(&methodRegionInfo);
    }
    EX_END_CATCH_ALLOW_DATATARGET_MISSING_MEMORY;

    // Even if GetMethodRegionInfo() fails to retrieve the cold code region info,
    // we should still be able to get the hot code region info.  We are counting on this for
    // stackwalking to work in dump debugging scenarios.
    _ASSERTE(methodRegionInfo.hotStartAddress == codeAddr);

    // now get the rest of the region information
    pCodeInfo->m_rgCodeRegions[kHot].Init(PCODEToPINSTR(methodRegionInfo.hotStartAddress),
                                          (ULONG)methodRegionInfo.hotSize);
    pCodeInfo->m_rgCodeRegions[kCold].Init(PCODEToPINSTR(methodRegionInfo.coldStartAddress),
                                               (ULONG)methodRegionInfo.coldSize);
    _ASSERTE(pCodeInfo->IsValid());

    MethodDesc* pMethodDesc = vmMethodDesc.GetDacPtr();
    pCodeInfo->isInstantiatedGeneric = pMethodDesc->HasClassOrMethodInstantiation();
    pCodeInfo->vmNativeCodeMethodDescToken = vmMethodDesc;

    SIZE_T unusedLatestEncVersion;
    Module * pModule = pMethodDesc->GetModule();
    _ASSERTE(pModule != NULL);
    LookupEnCVersions(pModule,
                      vmMethodDesc,
                      pMethodDesc->GetMemberDef(),
                      codeAddr,
                      &unusedLatestEncVersion, //unused by caller
                      &(pCodeInfo->encVersion));

} // GetNativeCodeInfo


//- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
//
// Functions to get Type and Class information
//
//- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
//-----------------------------------------------------------------------------
//DacDbiInterfaceImpl::GetTypeHandles
// Get the approximate and exact type handles for a type
// Arguments:
//     input:
//         vmThExact            - VMPTR of the exact type handle. If this method is called
//                                to get information for a new generic instantiation, this will already
//                                be initialized. If it's called to get type information for an arbitrary
//                                 type (i.e., called to initialize an instance of CordbClass), it will be NULL
//         vmThApprox           - VMPTR of the approximate type handle. If this method is called
//                                to get information for a new generic instantiation, this will already
//                                be initialized. If it's called to get type information for an arbitrary
//                                type (i.e., called to initialize an instance of CordbClass), it will be NULL
//     output:
//         pThExact             - handle for exact type information for a generic instantiation
//         pThApprox            - handle for type information
// Notes:
//    pThExact and pTHApprox must be pointers to existing memory.
//-----------------------------------------------------------------------------
void DacDbiInterfaceImpl::GetTypeHandles(VMPTR_TypeHandle  vmThExact,
                                         VMPTR_TypeHandle  vmThApprox,
                                         TypeHandle *      pThExact,
                                         TypeHandle *      pThApprox)
 {
     _ASSERTE((pThExact != NULL) && (pThApprox != NULL));

     *pThExact = TypeHandle::FromPtr(vmThExact.GetDacPtr());
     *pThApprox = TypeHandle::FromPtr(vmThApprox.GetDacPtr());

    // If we can't find the class, return the proper HR to the right side.
    if (pThApprox->IsNull())
    {
        LOG((LF_CORDB, LL_INFO10000, "D::GASCI: class isn't loaded.\n"));
        ThrowHR(CORDBG_E_CLASS_NOT_LOADED);
    }
 }  // DacDbiInterfaceImpl::GetTypeHandles

//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::GetTotalFieldCount
// Gets the total number of fields for a type.
// Input Argument: thApprox - type handle used to determine the number of fields
// Return Value:   count of the total fields of the type.
//-----------------------------------------------------------------------------
unsigned int DacDbiInterfaceImpl::GetTotalFieldCount(TypeHandle thApprox)
{
    MethodTable *pMT = thApprox.GetMethodTable();

    // Count the instance and static fields for this class (not including parent).
    // This will not include any newly added EnC fields.
    unsigned int IFCount = pMT->GetNumIntroducedInstanceFields();
    unsigned int SFCount = pMT->GetNumStaticFields();

#ifdef FEATURE_METADATA_UPDATER
    PTR_Module pModule = pMT->GetModule();

    // Stats above don't include EnC fields. So add them now.
    if (pModule->IsEditAndContinueEnabled())
    {
        PTR_EnCEEClassData pEncData =
            (dac_cast<PTR_EditAndContinueModule>(pModule))->GetEnCEEClassData(pMT, TRUE);

        if (pEncData != NULL)
        {
            _ASSERTE(pEncData->GetMethodTable() == pMT);

            // EnC only adds fields, never removes them.
            IFCount += pEncData->GetAddedInstanceFields();
            SFCount += pEncData->GetAddedStaticFields();
        }
    }
#endif
    return IFCount + SFCount;
} // DacDbiInterfaceImpl::GetTotalFieldCount

//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::InitClassData
// initializes various values of the ClassInfo data structure, including the
// field count, generic args count, size and value class flag
// Arguments:
//     input:  thApprox            - used to get access to all the necessary values
//             fIsInstantiatedType - used to determine how to compute the size
//     output: pData               - contains fields to be initialized
//-----------------------------------------------------------------------------
void DacDbiInterfaceImpl::InitClassData(TypeHandle  thApprox,
                                        BOOL        fIsInstantiatedType,
                                        ClassInfo * pData)
{
    pData->m_fieldList.Alloc(GetTotalFieldCount(thApprox));

    // For Generic classes you must get the object size via the type handle, which
    // will get you to the right information for the particular instantiation
    // you're working with...
    pData->m_objectSize = 0;
    if ((!thApprox.GetNumGenericArgs()) || fIsInstantiatedType)
    {
        pData->m_objectSize = thApprox.GetMethodTable()->GetNumInstanceFieldBytes();
    }

} // DacDbiInterfaceImpl::InitClassData

//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::GetStaticsBases
// Gets the base table addresses for both GC and non-GC statics
// Arguments:
//     input:  thExact            - exact type handle for the class
//             pAppDomain         - AppDomain in which the class is loaded
//     output: ppGCStaticsBase    - base pointer for GC statics
//             ppNonGCStaticsBase - base pointer for non GC statics
// Notes:
// If this is a non-generic type, or an instantiated type, then we'll be able to get the static var bases
// If the typeHandle represents a generic type constructor (i.e. an uninstantiated generic class), then
// the static bases will be null (since statics are per-instantiation).
//-----------------------------------------------------------------------------
void DacDbiInterfaceImpl::GetStaticsBases(TypeHandle thExact,
                                         AppDomain * pAppDomain,
                                         PTR_BYTE *  ppGCStaticsBase,
                                         PTR_BYTE *  ppNonGCStaticsBase)
 {
    MethodTable * pMT = thExact.GetMethodTable();
    Module * pModuleForStatics = pMT->GetModuleForStatics();
    if (pModuleForStatics != NULL)
    {
        PTR_DomainLocalModule pLocalModule = pModuleForStatics->GetDomainLocalModule();
        if (pLocalModule != NULL)
        {
            *ppGCStaticsBase = pLocalModule->GetGCStaticsBasePointer(pMT);
            *ppNonGCStaticsBase = pLocalModule->GetNonGCStaticsBasePointer(pMT);
        }
    }
} // DacDbiInterfaceImpl::GetStaticsBases

//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::ComputeFieldData
// Computes the field info for pFD and stores it in pcurrentFieldData
// Arguments:
//     input:  pFD               - FieldDesc used to get necessary information
//             pGCStaticsBase    - base table address for GC statics
//             pNonGCStaticsBase - base table address for non-GC statics
//     output: pCurrentFieldData - contains fields to be initialized
//-----------------------------------------------------------------------------
void DacDbiInterfaceImpl::ComputeFieldData(PTR_FieldDesc pFD,
                                           PTR_BYTE    pGCStaticsBase,
                                           PTR_BYTE    pNonGCStaticsBase,
                                           FieldData * pCurrentFieldData)
{
    pCurrentFieldData->Initialize(pFD->IsStatic(), pFD->IsPrimitive(), pFD->GetMemberDef());

#ifdef FEATURE_METADATA_UPDATER
    // If the field was newly introduced via EnC, and hasn't yet
    // been fixed up, then we'll send back a marker indicating
    // that it isn't yet available.
    if (pFD->IsEnCNew())
    {
        // @dbgtodo Microsoft inspection: eliminate the debugger token when ICDClass and ICDType are
        // completely DACized
        pCurrentFieldData->m_vmFieldDesc.SetHostPtr(pFD);
        pCurrentFieldData->m_fFldStorageAvailable = FALSE;
        pCurrentFieldData->m_fFldIsTLS = FALSE;
        pCurrentFieldData->m_fFldIsRVA = FALSE;
        pCurrentFieldData->m_fFldIsCollectibleStatic = FALSE;
    }
    else
#endif // FEATURE_METADATA_UPDATER
    {
        // Otherwise, we'll compute the info & send it back.
        pCurrentFieldData->m_fFldStorageAvailable = TRUE;
        // @dbgtodo Microsoft inspection: eliminate the debugger token when ICDClass and ICDType are
        // completely DACized
        pCurrentFieldData->m_vmFieldDesc.SetHostPtr(pFD);
        pCurrentFieldData->m_fFldIsTLS = (pFD->IsThreadStatic() == TRUE);
        pCurrentFieldData->m_fFldIsRVA = (pFD->IsRVA() == TRUE);
        pCurrentFieldData->m_fFldIsCollectibleStatic = (pFD->IsStatic() == TRUE &&
            pFD->GetEnclosingMethodTable()->Collectible());

        // Compute the address of the field
        if (pFD->IsStatic())
        {
            // statics are addressed using an absolute address.
            if (pFD->IsRVA())
            {
                // RVA statics are relative to a base module address
                DWORD offset = pFD->GetOffset();
                PTR_VOID addr = pFD->GetModule()->GetRvaField(offset);
                if (pCurrentFieldData->OkToGetOrSetStaticAddress())
                {
                    pCurrentFieldData->SetStaticAddress(PTR_TO_TADDR(addr));
                }
            }
            else if (pFD->IsThreadStatic() ||
                pCurrentFieldData->m_fFldIsCollectibleStatic)
            {
                // this is a special type of static that must be queried using DB_IPCE_GET_SPECIAL_STATIC
            }
            else
            {
                // This is a normal static variable in the GC or Non-GC static base table
                PTR_BYTE base = pFD->IsPrimitive() ? pNonGCStaticsBase : pGCStaticsBase;
                if (base == NULL)
                {
                    // static var not available.  This may be an open generic class (not an instantiated type),
                    // or we might only have approximate type information because the type hasn't been
                    // initialized yet.

                    if (pCurrentFieldData->OkToGetOrSetStaticAddress())
                    {
                        pCurrentFieldData->SetStaticAddress(NULL);
                    }
                }
                else
                {
                    if (pCurrentFieldData->OkToGetOrSetStaticAddress())
                    {
                        // calculate the absolute address using the base and the offset from the base
                        pCurrentFieldData->SetStaticAddress(PTR_TO_TADDR(base) + pFD->GetOffset());
                    }
                }
            }
        }
        else
        {
            // instance variables are addressed using an offset within the instance
            if (pCurrentFieldData->OkToGetOrSetInstanceOffset())
            {
                pCurrentFieldData->SetInstanceOffset(pFD->GetOffset());
            }
        }
    }

} // DacDbiInterfaceImpl::ComputeFieldData

//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::CollectFields
// Gets information for all the fields for a given type
// Arguments:
//     input:  thExact         - used to determine whether we need to get statics base tables
//             thApprox        - used to get the field desc iterator
//             pAppDomain      - used to get statics base tables
//     output:
//             pFieldList      - contains fields to be initialized
// Note: the caller must ensure that *ppFields is NULL (i.e., any previously allocated memory
// must have been deallocated.
//-----------------------------------------------------------------------------
void DacDbiInterfaceImpl::CollectFields(TypeHandle                   thExact,
                                        TypeHandle                   thApprox,
                                        AppDomain *                  pAppDomain,
                                        DacDbiArrayList<FieldData> * pFieldList)
{
    PTR_BYTE pGCStaticsBase = NULL;
    PTR_BYTE pNonGCStaticsBase = NULL;
    if (!thExact.IsNull() && !thExact.GetMethodTable()->Collectible())
    {
        // get base tables for static fields
        GetStaticsBases(thExact, pAppDomain, &pGCStaticsBase, &pNonGCStaticsBase);
    }

    unsigned int fieldCount = 0;

    // <TODO> we are losing exact type information for static fields in generic types. We have
    // field desc iterators only for approximate types, but statics are per instantiation, so we
    // need an exact type to be able to handle these correctly. We need to use
    // FieldDesc::GetExactDeclaringType to get at the correct field. This requires the exact
    // TypeHandle. </TODO>
    EncApproxFieldDescIterator fdIterator(thApprox.GetMethodTable(),
                                          ApproxFieldDescIterator::ALL_FIELDS); // don't fixup EnC (we can't, we're stopped)

    PTR_FieldDesc pCurrentFD;
    unsigned int index = 0;
    while (((pCurrentFD = fdIterator.Next()) != NULL) && (index < pFieldList->Count()))
    {
        // fill in the pCurrentEntry structure
        ComputeFieldData(pCurrentFD, pGCStaticsBase, pNonGCStaticsBase, &((*pFieldList)[index]));

        // Bump our counts and pointers.
        fieldCount++;
        index++;
    }
    _ASSERTE(fieldCount == (unsigned int)pFieldList->Count());

} // DacDbiInterfaceImpl::CollectFields


// Determine if a type is a ValueType
BOOL DacDbiInterfaceImpl::IsValueType (VMPTR_TypeHandle vmTypeHandle)
{
    DD_ENTER_MAY_THROW;

    TypeHandle th = TypeHandle::FromPtr(vmTypeHandle.GetDacPtr());
    return th.IsValueType();
}

// Determine if a type has generic parameters
BOOL DacDbiInterfaceImpl::HasTypeParams (VMPTR_TypeHandle vmTypeHandle)
{
    DD_ENTER_MAY_THROW;

    TypeHandle th = TypeHandle::FromPtr(vmTypeHandle.GetDacPtr());
    return th.ContainsGenericVariables();
}

// DacDbi API: Get type information for a class
void DacDbiInterfaceImpl::GetClassInfo(VMPTR_AppDomain  vmAppDomain,
                                       VMPTR_TypeHandle vmThExact,
                                       ClassInfo *      pData)
{
    DD_ENTER_MAY_THROW;

    AppDomain * pAppDomain = vmAppDomain.GetDacPtr();

    TypeHandle  thExact;
    TypeHandle  thApprox;

    GetTypeHandles(vmThExact, vmThExact, &thExact, &thApprox);

    // initialize field count, generic args count, size and value class flag
    InitClassData(thApprox, false, pData);

    if (pAppDomain != NULL)
        CollectFields(thExact, thApprox, pAppDomain, &(pData->m_fieldList));
} // DacDbiInterfaceImpl::GetClassInfo

// DacDbi API: Get field information and object size for an instantiated generic type
void DacDbiInterfaceImpl::GetInstantiationFieldInfo (VMPTR_DomainAssembly             vmDomainAssembly,
                                                     VMPTR_TypeHandle             vmThExact,
                                                     VMPTR_TypeHandle             vmThApprox,
                                                     DacDbiArrayList<FieldData> * pFieldList,
                                                     SIZE_T *                     pObjectSize)
{
    DD_ENTER_MAY_THROW;

    DomainAssembly * pDomainAssembly = vmDomainAssembly.GetDacPtr();
    _ASSERTE(pDomainAssembly != NULL);
    AppDomain * pAppDomain = pDomainAssembly->GetAppDomain();
    TypeHandle  thExact;
    TypeHandle  thApprox;

    GetTypeHandles(vmThExact, vmThApprox, &thExact, &thApprox);

    *pObjectSize = thApprox.GetMethodTable()->GetNumInstanceFieldBytes();

    pFieldList->Alloc(GetTotalFieldCount(thApprox));

    CollectFields(thExact, thApprox, pAppDomain, pFieldList);

} // DacDbiInterfaceImpl::GetInstantiationFieldInfo

//-----------------------------------------------------------------------------------
// DacDbiInterfaceImpl::TypeDataWalk member functions
//-----------------------------------------------------------------------------------

//-----------------------------------------------------------------------------
// TypeDataWalk constructor--initialize the buffer and number of remaining items from input data
// Arguments: pData - pointer to a list of records containing information about type parameters for an
//                    instantiated type
//            nData - number of entries in pData
//-----------------------------------------------------------------------------
DacDbiInterfaceImpl::TypeDataWalk::TypeDataWalk(DebuggerIPCE_TypeArgData * pData, unsigned int nData)
{
    m_pCurrentData = pData;
    m_nRemaining = nData;
} // DacDbiInterfaceImpl::TypeDataWalk::TypeDataWalk

//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::TypeDataWalk::ReadOne
// read and return a single node from the list of type parameters
// Arguments: none (uses internal state)
// Return value: information about the next type parameter in m_pCurrentData
//-----------------------------------------------------------------------------
DebuggerIPCE_TypeArgData * DacDbiInterfaceImpl::TypeDataWalk::ReadOne()
{
    LIMITED_METHOD_CONTRACT;
    if (m_nRemaining)
    {
        m_nRemaining--;
        return m_pCurrentData++;
    }
    else
    {
        return NULL;
    }
} // DacDbiInterfaceImpl::TypeDataWalk::ReadOne

//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::TypeDataWalk::Skip
// Skip a single node from the list of type handles along with any children it might have
// Arguments: none  (uses internal state)
// Return value: none (updates internal state)
//-----------------------------------------------------------------------------
void DacDbiInterfaceImpl::TypeDataWalk::Skip()
{
    LIMITED_METHOD_CONTRACT;

    DebuggerIPCE_TypeArgData * pData = ReadOne();
    if (pData)
    {
        for (unsigned int i = 0; i < pData->numTypeArgs; i++)
        {
            Skip();
        }
    }
} // DacDbiInterfaceImpl::TypeDataWalk::Skip

//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::TypeDataWalk::ReadLoadedTypeArg
// Read a type handle when it is used in the position of a generic argument or
// argument of an array or address type.  Take into account generic code sharing if we
// have been requested to find the canonical representation amongst a set of shared-
// code generic types.  That is, if generics code sharing is enabled then return "Object"
// for all reference types, and canonicalize underneath value types, e.g. V<string> --> V<object>.
// Return TypeHandle() if any of the type handles are not loaded.
//
// Arguments: retrieveWhich - indicates whether to retrieve a canonical representation or
//                            an exact representation
// Return value: the type handle for the type parameter
//-----------------------------------------------------------------------------
TypeHandle DacDbiInterfaceImpl::TypeDataWalk::ReadLoadedTypeArg(TypeHandleReadType retrieveWhich)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#if !defined(FEATURE_SHARE_GENERIC_CODE)
    return ReadLoadedTypeHandle(kGetExact);
#else

    if (retrieveWhich == kGetExact)
        return ReadLoadedTypeHandle(kGetExact);

    // This nasty bit of code works out what the "canonicalization" of a
    // parameter to a generic is once we take into account generics code sharing.
    //
    // This logic is somewhat a duplication of logic in vm\typehandle.cpp, though
    // that logic operates on a TypeHandle format, i.e. assumes we're finding the
    // canonical form of a type that has already been loaded.  Here we are finding
    // the canonical form of a type that may not have been loaded (but where we expect
    // its canonical form to have been loaded).
    //
    // Ideally this logic would not be duplicated in this way, but it is difficult
    // to arrange for that.
    DebuggerIPCE_TypeArgData * pData = ReadOne();
    if (!pData)
        return TypeHandle();

    // If we have code sharing then the process of canonicalizing is trickier.
    // unfortunately we have to include the exact specification of compatibility at
    // this point.
    CorElementType elementType = pData->data.elementType;

    switch (elementType)
    {
        case ELEMENT_TYPE_PTR:
            _ASSERTE(pData->numTypeArgs == 1);
            return PtrOrByRefTypeArg(pData, retrieveWhich);
            break;

        case ELEMENT_TYPE_CLASS:
        case ELEMENT_TYPE_VALUETYPE:
            return ClassTypeArg(pData, retrieveWhich);
            break;

        case ELEMENT_TYPE_FNPTR:
            return FnPtrTypeArg(pData, retrieveWhich);
            break;

        default:
            return ObjRefOrPrimitiveTypeArg(pData, elementType);
            break;
    }

#endif // FEATURE_SHARE_GENERIC_CODE
} // DacDbiInterfaceImpl::TypeDataWalk::ReadLoadedTypeArg

//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::TypeDataWalk::ReadLoadedTypeHandles
// Iterate through the type argument data, creating type handles as we go.
//
// Arguments:
//     input:  retrieveWhich - indicates whether we can return a canonical type handle
//                             or we must return an exact type handle
//             nTypeArgs     - number of type arguments to be read
//     output: ppResults     - pointer to a list of TypeHandles that will hold the type handles
//                             for each type parameter
//
// Return Value: FALSE iff any of the type handles are not loaded.
//-----------------------------------------------------------------------------
BOOL DacDbiInterfaceImpl::TypeDataWalk::ReadLoadedTypeHandles(TypeHandleReadType retrieveWhich,
                                                              unsigned int       nTypeArgs,
                                                              TypeHandle *       ppResults)
{
    WRAPPER_NO_CONTRACT;

    BOOL allOK = true;
    for (unsigned int i = 0; i < nTypeArgs; i++)
    {
        ppResults[i] = ReadLoadedTypeArg(retrieveWhich);
        allOK &= !ppResults[i].IsNull();
    }
    return allOK;
} // DacDbiInterfaceImpl::TypeDataWalk::ReadLoadedTypeHandles

//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::TypeDataWalk::ReadLoadedInstantiation
// Read an instantiation of a generic type if it has already been created.
//
// Arguments:
//     input:  retrieveWhich - indicates whether we can return a canonical type handle
//                             or we must return an exact type handle
//             pModule       - module in which the instantiated type is loaded
//             mdToken       - metadata token for the type
//             nTypeArgs     - number of type arguments to be read
// Return value: the type handle for the instantiated type
//-----------------------------------------------------------------------------
TypeHandle DacDbiInterfaceImpl::TypeDataWalk::ReadLoadedInstantiation(TypeHandleReadType retrieveWhich,
                                                                      Module *           pModule,
                                                                      mdTypeDef          mdToken,
                                                                      unsigned int       nTypeArgs)
{
    WRAPPER_NO_CONTRACT;

    NewArrayHolder<TypeHandle> pInst(new TypeHandle[nTypeArgs]);

    // get the type handle for each of the type parameters
    if (!ReadLoadedTypeHandles(retrieveWhich, nTypeArgs, pInst))
    {
        return TypeHandle();
    }

    // get the type handle for the particular instantiation that corresponds to
    // the given type parameters
    return FindLoadedInstantiation(pModule, mdToken, nTypeArgs, pInst);

} // DacDbiInterfaceImpl::TypeDataWalk::ReadLoadedInstantiation


//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::TypeDataWalk::ReadLoadedTypeHandle
//
// Compute the type handle for a given type.
// This is the top-level function that will return the type handle  for an
// arbitrary type. It uses mutual recursion with ReadLoadedTypeArg to get
// the type handle for a (possibly parameterized) type. Note that the referent of
// address types or the element type of an array type are viewed as type parameters.
//
// For example, assume that we are retrieving only exact types, and we have as our
// top level type an array defined as int [][].
// We start by noting that the type is an array type, so we call ReadLoadedTypeArg to
// get the element type. We find that the element type is also an array:int [].
// ReadLoadedTypeArg will call ReadLoadedTypeHandle with this type information.
// Again, we determine that the top-level type is an array, so we call ReadLoadedTypeArg
// to get the element type, int. ReadLoadedTypeArg will again call ReadLoadedTypeHandle
// which will find that this time, the top-level type is a primitive type. It will request
// the loaded type handle from the loader and return it. On return, we get the type handle
// for an array of int from the loader. We return again and request the type handle for an
// array of arrays of int. This is the type handle we will return.
//
// Arguments:
//      input: retrieveWhich - determines whether we can return the type handle for
//                             a canonical type or only for an exact type
//             we use the list of type data stored in the TypeDataWalk data members
//             for other input information
// Return value:  type handle for the current type.
//-----------------------------------------------------------------------------
TypeHandle DacDbiInterfaceImpl::TypeDataWalk::ReadLoadedTypeHandle(TypeHandleReadType retrieveWhich)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // get the type information at the head of the list m_pCurrentData
    DebuggerIPCE_TypeArgData * pData = ReadOne();
    if (!pData)
      return TypeHandle();

    // get the type handle that corresponds to its elementType
    TypeHandle typeHandle;
    switch (pData->data.elementType)
    {
        case ELEMENT_TYPE_ARRAY:
        case ELEMENT_TYPE_SZARRAY:
            typeHandle = ArrayTypeArg(pData, retrieveWhich);
            break;

        case ELEMENT_TYPE_PTR:
        case ELEMENT_TYPE_BYREF:
            typeHandle = PtrOrByRefTypeArg(pData, retrieveWhich);
            break;
        case ELEMENT_TYPE_CLASS:
        case ELEMENT_TYPE_VALUETYPE:
            {
                Module *     pModule = pData->data.ClassTypeData.vmModule.GetDacPtr();
                typeHandle = ReadLoadedInstantiation(retrieveWhich,
                                                     pModule,
                                                     pData->data.ClassTypeData.metadataToken,
                                                     pData->numTypeArgs);
            }
            break;

        case ELEMENT_TYPE_FNPTR:
            {
                typeHandle = FnPtrTypeArg(pData, retrieveWhich);
            }
            break;

    default:
            typeHandle = FindLoadedElementType(pData->data.elementType);
        break;
    }
    return typeHandle;
} // DacDbiInterfaceImpl::TypeDataWalk::ReadLoadedTypeHandle

//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::TypeDataWalk::ArrayTypeArg
// get a loaded type handle for an array type (E_T_ARRAY or E_T_SZARRAY)
//
// Arguments:
//     input: pArrayTypeInfo - type information for an array type
//                             Although this is in fact a pointer (in)to a list, we treat it here
//                             simply as a pointer to a single instance of DebuggerIPCE_TypeArgData
//                             which holds type information for an array.
//                             This is the most recent type node (for an array type) retrieved
//                             by TypeDataWalk::ReadOne(). The call to ReadLoadedTypeArg will
//                             result in call(s) to ReadOne to retrieve one or more type nodes
//                             that are needed to compute the type handle for the
//                             element type of the array. When we return from that call, we pass
//                             pArrayTypeInfo along with arrayElementTypeArg to FindLoadedArrayType
//                             to get the type handle for this particular array type.
//                             Note:
//                             On entry, we know that pArrayTypeInfo is the same as m_pCurrentData - 1,
//                             but by the time we need to use it, this is no longer true. Because
//                             we can't predict how many nodes will be consumed by the call to
//                             ReadLoadedTypeArg, we can't compute this value from the member fields
//                             of TypeDataWalk and therefore pass it as a parameter.
//            retrieveWhich -  determines whether we can return the type handle for
//                             a canonical type or only for an exact type
// Return value: the type handle corresponding to the array type
//-----------------------------------------------------------------------------

TypeHandle DacDbiInterfaceImpl::TypeDataWalk::ArrayTypeArg(DebuggerIPCE_TypeArgData * pArrayTypeInfo,
                                                           TypeHandleReadType         retrieveWhich)
{
    TypeHandle arrayElementTypeArg = ReadLoadedTypeArg(retrieveWhich);
    if (!arrayElementTypeArg.IsNull())
    {
        return FindLoadedArrayType(pArrayTypeInfo->data.elementType,
                                   arrayElementTypeArg,
                                   pArrayTypeInfo->data.ArrayTypeData.arrayRank);
    }
    return TypeHandle();
} // DacDbiInterfaceImpl::TypeDataWalk::ArrayTypeArg


//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::TypeDataWalk::PtrOrByRefTypeArg
// get a loaded type handle for an address type (E_T_PTR or E_T_BYREF)
//
// Arguments:
//     input: pPtrOrByRefTypeInfo - type information for a pointer or byref type
//                             Although this is in fact a pointer (in)to a list, we treat it here
//                             simply as a pointer to a single instance of DebuggerIPCE_TypeArgData
//                             which holds type information for a pointer or byref type.
//                             This is the most recent type node (for a pointer or byref type) retrieved
//                             by TypeDataWalk::ReadOne(). The call to ReadLoadedTypeArg will
//                             result in call(s) to ReadOne to retrieve one or more type nodes
//                             that are needed to compute the type handle for the
//                             referent type of the pointer. When we return from that call, we pass
//                             pPtrOrByRefTypeInfo along with referentTypeArg to FindLoadedPointerOrByrefType
//                             to get the type handle for this particular pointer or byref type.
//                             Note:
//                             On entry, we know that pPtrOrByRefTypeInfo is the same as m_pCurrentData - 1,
//                             but by the time we need to use it, this is no longer true. Because
//                             we can't predict how many nodes will be consumed by the call to
//                             ReadLoadedTypeArg, we can't compute this value from the member fields
//                             of TypeDataWalk and therefore pass it as a parameter.
//            retrieveWhich - determines whether we can return the type handle for
//                            a canonical type or only for an exact type
// Return value: the type handle corresponding to the address type
//-----------------------------------------------------------------------------
TypeHandle DacDbiInterfaceImpl::TypeDataWalk::PtrOrByRefTypeArg(DebuggerIPCE_TypeArgData * pPtrOrByRefTypeInfo,
                                                                TypeHandleReadType         retrieveWhich)
{
    TypeHandle referentTypeArg = ReadLoadedTypeArg(retrieveWhich);
    if (!referentTypeArg.IsNull())
    {
        return FindLoadedPointerOrByrefType(pPtrOrByRefTypeInfo->data.elementType, referentTypeArg);
    }

    return TypeHandle();

} // DacDbiInterfaceImpl::TypeDataWalk::PtrOrByRefTypeArg

//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::TypeDataWalk::ClassTypeArg
// get a loaded type handle for a class type (E_T_CLASS or E_T_VALUETYPE)
//
// Arguments:
//     input: pClassTypeInfo - type information for a class type
//                             Although this is in fact a pointer (in)to a list, we treat it here
//                             simply as a pointer to a single instance of DebuggerIPCE_TypeArgData
//                             which holds type information for a pointer or byref type.
//                             This is the most recent type node (for a pointer or byref type) retrieved
//                             by TypeDataWalk::ReadOne(). The call to ReadLoadedInstantiation will
//                             result in call(s) to ReadOne to retrieve one or more type nodes
//                             that are needed to compute the type handle for the type parameters
//                             for the class. If we can't find an exact loaded type for the class, we will
//                             instead return a canonical method table. In this case, we need to skip
//                             the type parameter information for each actual parameter to the class.
//                             This is necessary because we may be getting a type handle for a class which is
//                             in turn an argument to a parent type. If the parent type has more arguments, we
//                             need to be at the right place in the list when we return. We use
//                             pClassTypeInfo to get the number of type arguments that we need to skip.
//            retrieveWhich - determines whether we can return the type handle for
//                            a canonical type or only for an exact type
// Return value: the type handle corresponding to the class type
//-----------------------------------------------------------------------------
TypeHandle DacDbiInterfaceImpl::TypeDataWalk::ClassTypeArg(DebuggerIPCE_TypeArgData * pClassTypeInfo,
                                                           TypeHandleReadType         retrieveWhich)
{
    Module *     pModule = pClassTypeInfo->data.ClassTypeData.vmModule.GetDacPtr();
    TypeHandle   typeDef = ClassLoader::LookupTypeDefOrRefInModule(pModule,
                                                                   pClassTypeInfo->data.ClassTypeData.metadataToken);

    if ((!typeDef.IsNull() && typeDef.IsValueType()) || (pClassTypeInfo->data.elementType == ELEMENT_TYPE_VALUETYPE))
    {
        return ReadLoadedInstantiation(retrieveWhich,
                                       pModule,
                                       pClassTypeInfo->data.ClassTypeData.metadataToken,
                                       pClassTypeInfo->numTypeArgs);
    }
    else
    {
        _ASSERTE(retrieveWhich == kGetCanonical);
        // skip the instantiation - no need to look at it since the type canonicalizes to "Object"
        for (unsigned int i = 0; i < pClassTypeInfo->numTypeArgs; i++)
        {
            Skip();
        }
        return TypeHandle(g_pCanonMethodTableClass);
    }
}// DacDbiInterfaceImpl::TypeDataWalk::ClassTypeArg

//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::TypeDataWalk::FnPtrTypeArg
// get a loaded type handle for a function pointer type (E_T_FNPTR)
//
// Arguments:
//     input: pFnPtrTypeInfo - type information for a pointer or byref type
//                             Although this is in fact a pointer (in)to a list, we treat it here
//                             simply as a pointer to a single instance of DebuggerIPCE_TypeArgData
//                             which holds type information for a function pointer type.
//                             This is the most recent type node (for a function pointer type) retrieved
//                             by TypeDataWalk::ReadOne(). The call to ReadLoadedTypeHandles will
//                             result in call(s) to ReadOne to retrieve one or more type nodes
//                             that are needed to compute the type handle for the return type and
//                             parameter types of the function. When we return from that call, we pass
//                             pFnPtrTypeInfo along with pInst to FindLoadedFnptrType
//                             to get the type handle for this particular function pointer type.
//            retrieveWhich - determines whether we can return the type handle for
//                            a canonical type or only for an exact type
// Return value: the type handle corresponding to the function pointer type
//-----------------------------------------------------------------------------
TypeHandle DacDbiInterfaceImpl::TypeDataWalk::FnPtrTypeArg(DebuggerIPCE_TypeArgData * pFnPtrTypeInfo,
                                                           TypeHandleReadType         retrieveWhich)
{
    // allocate space to store a list of type handles, one for the return type and one for each
    // of the parameter types of the function to which the FnPtr type refers.
    NewArrayHolder<TypeHandle> pInst(new TypeHandle[sizeof(TypeHandle) * pFnPtrTypeInfo->numTypeArgs]);

    if (ReadLoadedTypeHandles(retrieveWhich, pFnPtrTypeInfo->numTypeArgs, pInst))
    {
        return FindLoadedFnptrType(pFnPtrTypeInfo->numTypeArgs, pInst);
    }

    return TypeHandle();

} // DacDbiInterfaceImpl::TypeDataWalk::FnPtrTypeArg

//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::TypeDataWalk::ObjRefOrPrimitiveTypeArg
// get a loaded type handle for a primitive type or ObjRef
//
// Arguments:
//     input: pArgInfo      - type information for an objref or primitive type.
//                            This is called only when the objref or primitive type
//                            is a type argument for a parent type. In this case,
//                            we treat all objrefs the same, that is, we don't care
//                            about type parameters for the referent. Instead, we will
//                            simply return the canonical object type handle as the type
//                            of the referent. <@dbgtodo Microsoft: why is this?>
//                            If this is a primitive type, we'll simply get the
//                            type handle for that type.
//            elementType   - type of the argument
// Return value: the type handle corresponding to the elementType
//-----------------------------------------------------------------------------
TypeHandle DacDbiInterfaceImpl::TypeDataWalk::ObjRefOrPrimitiveTypeArg(DebuggerIPCE_TypeArgData * pArgInfo,
                                                                       CorElementType             elementType)
{
    // If there are any type args (e.g. for arrays) they can be skipped.  The thing
    // is a reference type anyway.
    for (unsigned int i = 0; i < pArgInfo->numTypeArgs; i++)
    {
        Skip();
    }

    // for an ObjRef, just return the CLASS____CANON type handle
    if (CorTypeInfo::IsObjRef_NoThrow(elementType))
    {
        return TypeHandle(g_pCanonMethodTableClass);
    }
    else
    {
        return FindLoadedElementType(elementType);
    }
} // DacDbiInterfaceImpl::TypeDataWalk::ObjRefOrPrimitiveTypeArg


//-------------------------------------------------------------------------
// end of TypeDataWalk implementations
//-------------------------------------------------------------------------
//-------------------------------------------------------------------------
// functions to use loader to get type handles
// ------------------------------------------------------------------------

// Note, in these functions, the use of ClassLoader::DontLoadTypes was chosen
// instead of FailIfNotLoaded because, although we may want to debug unrestored
// VCs, we can't do it because the debug API is not set up to handle them.
//
//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::FindLoadedArrayType
// Use ClassLoader to find a loaded type handle for an array type (E_T_ARRAY or E_T_SZARRAY)
// Arguments:
//     input: arrayType - type of the array
//            TypeArg   - type handle for the base type
//            rank      - array rank
// Return Value: type handle for the array type
//-----------------------------------------------------------------------------
// static
TypeHandle DacDbiInterfaceImpl::FindLoadedArrayType(CorElementType arrayType,
                                                    TypeHandle     typeArg,
                                                    unsigned       rank)
{
    // Lookup operations run the class loader in non-load mode.
    ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

    if (typeArg.IsNull())
    {
        return TypeHandle();
    }
    else
    {
        return ClassLoader::LoadArrayTypeThrowing(typeArg,
                                                  arrayType,
                                                  rank,
                                                  ClassLoader::DontLoadTypes );
    }
} // DacDbiInterfaceImpl::FindLoadedArrayType;

//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::FindLoadedPointerOrByrefType
// Use ClassLoader to find a loaded type handle for an address type (E_T_PTR or E_T_BYREF)
// Arguments:
//     input: addressType - type of the address type
//            TypeArg     - type handle for the base type
// Return Value: type handle for the address type
//-----------------------------------------------------------------------------
// static
TypeHandle DacDbiInterfaceImpl::FindLoadedPointerOrByrefType(CorElementType addressType, TypeHandle typeArg)
{
    // Lookup operations run the class loader in non-load mode.
    ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

    return ClassLoader::LoadPointerOrByrefTypeThrowing(addressType,
                                                       typeArg,
                                                       ClassLoader::DontLoadTypes);
} // DacDbiInterfaceImpl::FindLoadedPointerOrByrefType

//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::FindLoadedFnptrType
// Use ClassLoader to find a loaded type handle for a function pointer type (E_T_FNPTR)
// Arguments:
//     input: pInst       - type handles of the function's return value and arguments
//            numTypeArgs - number of type handles in pInst
// Return Value: type handle for the function pointer type
//-----------------------------------------------------------------------------
// static
TypeHandle DacDbiInterfaceImpl::FindLoadedFnptrType(DWORD numTypeArgs, TypeHandle * pInst)
{
    // Lookup operations run the class loader in non-load mode.
    ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

    // @dbgtodo : Do we need to worry about calling convention here?
    // LoadFnptrTypeThrowing expects the count of arguments, not
    // including return value, so we subtract 1 from numTypeArgs.
    return  ClassLoader::LoadFnptrTypeThrowing(0,
                                               numTypeArgs - 1,
                                               pInst,
                                               ClassLoader::DontLoadTypes);
} // DacDbiInterfaceImpl::FindLoadedFnptrType

//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::FindLoadedInstantiation
// Use ClassLoader to find a loaded type handle for a particular instantiation of a
// class type (E_T_CLASS or E_T_VALUECLASS)
//
// Arguments:
//     input: pModule   - module in which the type is loaded
//            mdToken   - metadata token for the type
//            nTypeArgs - number of type arguments in pInst
//            pInst     - list of type handles for the type parameters
// Return value: type handle for the instantiated class type
//-----------------------------------------------------------------------------
// static
TypeHandle DacDbiInterfaceImpl::FindLoadedInstantiation(Module *     pModule,
                                                        mdTypeDef    mdToken,
                                                        DWORD        nTypeArgs,
                                                        TypeHandle * pInst)
{
    // Lookup operations run the class loader in non-load mode.
    ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

    return ClassLoader::LoadGenericInstantiationThrowing(pModule,
                                                         mdToken,
                                                         Instantiation(pInst,nTypeArgs),
                                                         ClassLoader::DontLoadTypes);

} // DacDbiInterfaceImpl::FindLoadedInstantiation

//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::FindLoadedElementType
// Get the type handle for a primitive type
// Arguments:
//     input: elementType - type of the primitive type
// Return Value: Type handle for the primitive type
//-----------------------------------------------------------------------------
// static
TypeHandle DacDbiInterfaceImpl::FindLoadedElementType(CorElementType elementType)
{
    // Lookup operations run the class loader in non-load mode.
    ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

    MethodTable * pMethodTable = (&g_CoreLib)->GetElementType(elementType);

    return TypeHandle(pMethodTable);
} // DacDbiInterfaceImpl::FindLoadedElementType


//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::GetArrayTypeInfo
// Gets additional information to convert a type handle to an instance of CordbType if the type is E_T_ARRAY.
// Specifically, we get the rank and the type of the array elements
//
// Arguments:
//     input:  typeHandle - type handle for the array type
//             pAppDomain - AppDomain into which the type is loaded
//     output: pTypeInfo  - information for the array rank and element type
//
//-----------------------------------------------------------------------------
void DacDbiInterfaceImpl::GetArrayTypeInfo(TypeHandle                      typeHandle,
                                           DebuggerIPCE_ExpandedTypeData * pTypeInfo,
                                           AppDomain *                     pAppDomain)
{
    _ASSERTE(typeHandle.IsArray());
    pTypeInfo->ArrayTypeData.arrayRank = typeHandle.GetRank();
    TypeHandleToBasicTypeInfo(typeHandle.GetArrayElementTypeHandle(),
                              &(pTypeInfo->ArrayTypeData.arrayTypeArg),
                              pAppDomain);
} // DacDbiInterfaceImpl::GetArrayTypeInfo

//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::GetPtrTypeInfo
// Gets additional information to convert a type handle to an instance of CordbType if the type is
// E_T_PTR or E_T_BYREF. Specifically, we get the type for the referent of the address type
//
// Arguments:
//     input:  boxed      - indicates what, if anything, is boxed (see code:AreValueTypesBoxed for
//                          more specific information)
//             typeHandle - type handle for the address type
//             pAppDomain - AppDomain into which the type is loaded
//     output: pTypeInfo  - information for the referent type
//
//-----------------------------------------------------------------------------
void DacDbiInterfaceImpl::GetPtrTypeInfo(AreValueTypesBoxed              boxed,
                                         TypeHandle                      typeHandle,
                                         DebuggerIPCE_ExpandedTypeData * pTypeInfo,
                                         AppDomain *                     pAppDomain)
{
    if (boxed == AllBoxed)
    {
        GetClassTypeInfo(typeHandle, pTypeInfo, pAppDomain);
    }
    else
    {
        _ASSERTE(typeHandle.IsTypeDesc());
        TypeHandleToBasicTypeInfo(typeHandle.AsTypeDesc()->GetTypeParam(),
                                  &(pTypeInfo->UnaryTypeData.unaryTypeArg),
                                  pAppDomain);
    }
} // DacDbiInterfaceImpl::GetPtrTypeInfo

//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::GetFnPtrTypeInfo
// Gets additional information to convert a type handle to an instance of CordbType if the type is
// E_T_FNPTR, specifically the typehandle for the referent.
//
// Arguments
//     input:  boxed      - indicates what, if anything, is boxed (see code:AreValueTypesBoxed for
//                          more specific information)
//             typeHandle - type handle for the address type
//             pAppDomain - AppDomain into which the type is loaded
//     output: pTypeInfo  - information for the referent type
//
//-----------------------------------------------------------------------------
void DacDbiInterfaceImpl::GetFnPtrTypeInfo(AreValueTypesBoxed              boxed,
                                           TypeHandle                      typeHandle,
                                           DebuggerIPCE_ExpandedTypeData * pTypeInfo,
                                           AppDomain *                     pAppDomain)
{
    if (boxed == AllBoxed)
    {
        GetClassTypeInfo(typeHandle, pTypeInfo, pAppDomain);
    }
    else
    {
        pTypeInfo->NaryTypeData.typeHandle.SetDacTargetPtr(typeHandle.AsTAddr());
    }
} // DacDbiInterfaceImpl::GetFnPtrTypeInfo


//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::GetClassTypeInfo
// Gets additional information to convert a type handle to an instance of CordbType if the type is
// E_T_CLASS or E_T_VALUETYPE
//
// Arguments
//     input:  typeHandle - type handle for the address type
//             pAppDomain - AppDomain into which the type is loaded
//     output: pTypeInfo  - information for the referent type
//
//-----------------------------------------------------------------------------
void DacDbiInterfaceImpl::GetClassTypeInfo(TypeHandle                      typeHandle,
                                           DebuggerIPCE_ExpandedTypeData * pTypeInfo,
                                           AppDomain *                     pAppDomain)
{
    Module * pModule = typeHandle.GetModule();

    if (typeHandle.HasInstantiation()) // the type handle represents a generic instantiation
    {
        pTypeInfo->ClassTypeData.typeHandle.SetDacTargetPtr(typeHandle.AsTAddr());
    }
    else // non-generic
    {
        pTypeInfo->ClassTypeData.typeHandle = VMPTR_TypeHandle::NullPtr();
    }

    pTypeInfo->ClassTypeData.metadataToken = typeHandle.GetCl();

    _ASSERTE(pModule);
    pTypeInfo->ClassTypeData.vmModule.SetDacTargetPtr(PTR_HOST_TO_TADDR(pModule));
    if (pAppDomain)
    {
        pTypeInfo->ClassTypeData.vmDomainAssembly.SetDacTargetPtr(PTR_HOST_TO_TADDR(pModule->GetDomainAssembly()));
    }
    else
    {
        pTypeInfo->ClassTypeData.vmDomainAssembly = VMPTR_DomainAssembly::NullPtr();
    }
} // DacDbiInterfaceImpl::GetClassTypeInfo

//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::GetElementType
// Gets the correct CorElementType value from a type handle
//
// Arguments
//     input:  typeHandle - type handle for the address type
// Return Value: the CorElementType enum value for the type handle
//-----------------------------------------------------------------------------
CorElementType DacDbiInterfaceImpl::GetElementType (TypeHandle typeHandle)
{
    if (typeHandle.IsNull())
    {
        return ELEMENT_TYPE_VOID;
    }
    else if (typeHandle.GetMethodTable() == g_pObjectClass)
    {
       return ELEMENT_TYPE_OBJECT;
    }
    else if (typeHandle.GetMethodTable() == g_pStringClass)
    {
        return ELEMENT_TYPE_STRING;
    }
    else
    {
        // GetSignatureCorElementType returns E_T_CLASS for E_T_STRING... :-(
        return typeHandle.GetSignatureCorElementType();
    }

} // DacDbiInterfaceImpl::GetElementType

//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::TypeHandleToBasicTypeInfo
// Gets additional information to convert a type handle to an instance of CordbType for the referent of an
// E_T_BYREF or E_T_PTR or for the element type of an E_T_ARRAY or E_T_SZARRAY
//
// Arguments:
//     input:  typeHandle - type handle for the address type
//             pAppDomain - AppDomain into which the type is loaded
//     output: pTypeInfo  - information for the referent type
//
//-----------------------------------------------------------------------------
void DacDbiInterfaceImpl::TypeHandleToBasicTypeInfo(TypeHandle                   typeHandle,
                                                    DebuggerIPCE_BasicTypeData * pTypeInfo,
                                                    AppDomain *                  pAppDomain)
{
    pTypeInfo->elementType = GetElementType(typeHandle);

    switch (pTypeInfo->elementType)
    {
        case ELEMENT_TYPE_ARRAY:
        case ELEMENT_TYPE_SZARRAY:
        case ELEMENT_TYPE_FNPTR:
        case ELEMENT_TYPE_PTR:
        case ELEMENT_TYPE_BYREF:
            pTypeInfo->vmTypeHandle.SetDacTargetPtr(typeHandle.AsTAddr());
            pTypeInfo->metadataToken = mdTokenNil;
            pTypeInfo->vmDomainAssembly = VMPTR_DomainAssembly::NullPtr();
            break;

        case ELEMENT_TYPE_CLASS:
        case ELEMENT_TYPE_VALUETYPE:
        {
            Module * pModule = typeHandle.GetModule();

            if (typeHandle.HasInstantiation())   // only set if instantiated
            {
                pTypeInfo->vmTypeHandle.SetDacTargetPtr(typeHandle.AsTAddr());
            }
            else
            {
                pTypeInfo->vmTypeHandle = VMPTR_TypeHandle::NullPtr();
            }

            pTypeInfo->metadataToken = typeHandle.GetCl();
            _ASSERTE(pModule);

            pTypeInfo->vmModule.SetDacTargetPtr(PTR_HOST_TO_TADDR(pModule));
            if (pAppDomain)
            {
                pTypeInfo->vmDomainAssembly.SetDacTargetPtr(PTR_HOST_TO_TADDR(pModule->GetDomainAssembly()));
            }
            else
            {
                pTypeInfo->vmDomainAssembly = VMPTR_DomainAssembly::NullPtr();
            }
            break;
        }

        default:
            pTypeInfo->vmTypeHandle = VMPTR_TypeHandle::NullPtr();
            pTypeInfo->metadataToken = mdTokenNil;
            pTypeInfo->vmDomainAssembly = VMPTR_DomainAssembly::NullPtr();
            break;
    }
    return;
} // DacDbiInterfaceImpl::TypeHandleToBasicTypeInfo


void DacDbiInterfaceImpl::GetObjectExpandedTypeInfoFromID(AreValueTypesBoxed boxed,
                                       VMPTR_AppDomain vmAppDomain,
                                       COR_TYPEID id,
                                       DebuggerIPCE_ExpandedTypeData *pTypeInfo)
{
    DD_ENTER_MAY_THROW;

    TypeHandleToExpandedTypeInfoImpl(boxed, vmAppDomain, TypeHandle::FromPtr(TO_TADDR(id.token1)), pTypeInfo);
}

void DacDbiInterfaceImpl::GetObjectExpandedTypeInfo(AreValueTypesBoxed boxed,
                                       VMPTR_AppDomain vmAppDomain,
                                       CORDB_ADDRESS addr,
                                       DebuggerIPCE_ExpandedTypeData *pTypeInfo)
{
    DD_ENTER_MAY_THROW;

    PTR_Object obj(TO_TADDR(addr));
    TypeHandleToExpandedTypeInfoImpl(boxed, vmAppDomain, obj->GetGCSafeTypeHandle(), pTypeInfo);
}

// DacDbi API: use a type handle to get the information needed to create the corresponding RS CordbType instance
void DacDbiInterfaceImpl::TypeHandleToExpandedTypeInfo(AreValueTypesBoxed              boxed,
                                                       VMPTR_AppDomain                 vmAppDomain,
                                                       VMPTR_TypeHandle                vmTypeHandle,
                                                       DebuggerIPCE_ExpandedTypeData * pTypeInfo)
{
    DD_ENTER_MAY_THROW;


    TypeHandle typeHandle = TypeHandle::FromPtr(vmTypeHandle.GetDacPtr());
    TypeHandleToExpandedTypeInfoImpl(boxed, vmAppDomain, typeHandle, pTypeInfo);
}


void DacDbiInterfaceImpl::TypeHandleToExpandedTypeInfoImpl(AreValueTypesBoxed              boxed,
                                                       VMPTR_AppDomain                 vmAppDomain,
                                                       TypeHandle                      typeHandle,
                                                       DebuggerIPCE_ExpandedTypeData * pTypeInfo)
{
    AppDomain * pAppDomain = vmAppDomain.GetDacPtr();
    pTypeInfo->elementType = GetElementType(typeHandle);

    switch (pTypeInfo->elementType)
    {
        case ELEMENT_TYPE_ARRAY:
        case ELEMENT_TYPE_SZARRAY:
            GetArrayTypeInfo(typeHandle, pTypeInfo, pAppDomain);
            break;

        case ELEMENT_TYPE_PTR:
        case ELEMENT_TYPE_BYREF:
            GetPtrTypeInfo(boxed, typeHandle, pTypeInfo, pAppDomain);
            break;

        case ELEMENT_TYPE_VALUETYPE:
            if (boxed == OnlyPrimitivesUnboxed || boxed == AllBoxed)
            {
                pTypeInfo->elementType = ELEMENT_TYPE_CLASS;
            }
            GetClassTypeInfo(typeHandle, pTypeInfo, pAppDomain);
           break;

        case ELEMENT_TYPE_CLASS:
            GetClassTypeInfo(typeHandle, pTypeInfo, pAppDomain);
            break;

        case ELEMENT_TYPE_FNPTR:
                GetFnPtrTypeInfo(boxed, typeHandle, pTypeInfo, pAppDomain);
                break;
        default:
            if (boxed == AllBoxed)
            {
                pTypeInfo->elementType = ELEMENT_TYPE_CLASS;
                GetClassTypeInfo(typeHandle, pTypeInfo, pAppDomain);
            }
            // else the element type is sufficient
            break;
    }
    LOG((LF_CORDB, LL_INFO10000, "D::THTETI: converted left-side type handle to expanded right-side type info, pTypeInfo->ClassTypeData.typeHandle = 0x%08x.\n", pTypeInfo->ClassTypeData.typeHandle.GetRawPtr()));
    return;
} // DacDbiInterfaceImpl::TypeHandleToExpandedTypeInfo

// Get type handle for a TypeDef token, if one exists. For generics this returns the open type.
VMPTR_TypeHandle DacDbiInterfaceImpl::GetTypeHandle(VMPTR_Module vmModule,
                                                    mdTypeDef metadataToken)
{
    DD_ENTER_MAY_THROW;
    Module* pModule = vmModule.GetDacPtr();
    VMPTR_TypeHandle vmTypeHandle = VMPTR_TypeHandle::NullPtr();

    TypeHandle th = ClassLoader::LookupTypeDefOrRefInModule(pModule, metadataToken);
    if (th.IsNull())
    {
        LOG((LF_CORDB, LL_INFO10000, "D::GTH: class isn't loaded.\n"));
        ThrowHR(CORDBG_E_CLASS_NOT_LOADED);
    }

    vmTypeHandle.SetDacTargetPtr(th.AsTAddr());
    return vmTypeHandle;
}

// DacDbi API: GetAndSendApproxTypeHandle finds the type handle for the layout of the instance fields of an
// instantiated type if it is available.
VMPTR_TypeHandle DacDbiInterfaceImpl::GetApproxTypeHandle(TypeInfoList * pTypeData)
{
    DD_ENTER_MAY_THROW;

    LOG((LF_CORDB, LL_INFO10000, "D::GATH: getting info.\n"));


    TypeDataWalk walk(&((*pTypeData)[0]), pTypeData->Count());
    TypeHandle typeHandle = walk.ReadLoadedTypeHandle(TypeDataWalk::kGetCanonical);
    VMPTR_TypeHandle vmTypeHandle = VMPTR_TypeHandle::NullPtr();

    vmTypeHandle.SetDacTargetPtr(typeHandle.AsTAddr());
    if (!typeHandle.IsNull())
    {
        vmTypeHandle.SetDacTargetPtr(typeHandle.AsTAddr());
    }
    else
    {
        ThrowHR(CORDBG_E_CLASS_NOT_LOADED);
    }

    LOG((LF_CORDB, LL_INFO10000,
        "D::GATH: sending result, result = 0x%0x8\n",
        typeHandle));
    return vmTypeHandle;
} // DacDbiInterfaceImpl::GetApproxTypeHandle

// DacDbiInterface API: Get the exact type handle from type data
HRESULT DacDbiInterfaceImpl::GetExactTypeHandle(DebuggerIPCE_ExpandedTypeData * pTypeData,
                                                ArgInfoList *   pArgInfo,
                                                VMPTR_TypeHandle& vmTypeHandle)
{
    DD_ENTER_MAY_THROW;

    LOG((LF_CORDB, LL_INFO10000, "D::GETH: getting info.\n"));

    HRESULT hr = S_OK;

    EX_TRY
    {
        vmTypeHandle = vmTypeHandle.NullPtr();

        // convert the type information to a type handle
        TypeHandle typeHandle = ExpandedTypeInfoToTypeHandle(pTypeData, pArgInfo);
        _ASSERTE(!typeHandle.IsNull());
        vmTypeHandle.SetDacTargetPtr(typeHandle.AsTAddr());
    }
    EX_CATCH_HRESULT(hr);

    return hr;
} // DacDbiInterfaceImpl::GetExactTypeHandle

// Retrieve the generic type params for a given MethodDesc.  This function is specifically
// for stackwalking because it requires the generic type token on the stack.
void DacDbiInterfaceImpl::GetMethodDescParams(
    VMPTR_AppDomain     vmAppDomain,
    VMPTR_MethodDesc    vmMethodDesc,
    GENERICS_TYPE_TOKEN genericsToken,
    UINT32 *            pcGenericClassTypeParams,
    TypeParamsList *    pGenericTypeParams)
{
    DD_ENTER_MAY_THROW;

    if (vmAppDomain.IsNull() || vmMethodDesc.IsNull())
    {
        ThrowHR(E_INVALIDARG);
    }

    _ASSERTE((pcGenericClassTypeParams != NULL) && (pGenericTypeParams != NULL));

    MethodDesc * pMD = vmMethodDesc.GetDacPtr();

    // Retrieve the number of type parameters for the class and
    // the number of type parameters for the method itself.
    // For example, the method Foo<T, U>::Bar<V>() has 2 class type parameters and 1 method type parameters.
    UINT32 cGenericClassTypeParams  = pMD->GetNumGenericClassArgs();
    UINT32 cGenericMethodTypeParams = pMD->GetNumGenericMethodArgs();
    UINT32 cTotalGenericTypeParams  = cGenericClassTypeParams + cGenericMethodTypeParams;

    // Set the out parameter.
    *pcGenericClassTypeParams = cGenericClassTypeParams;

    TypeHandle   thSpecificClass;
    MethodDesc * pSpecificMethod = NULL;

    // Try to retrieve a more specific MethodDesc and TypeHandle via the generics type token.
    // The generics token is not always guaranteed to be available.
    // For example, it may be unavailable in prologs and epilogs.
    // In dumps, not available can also mean a thrown exception for missing memory.
    BOOL fExact = FALSE;
    ALLOW_DATATARGET_MISSING_MEMORY(
        fExact = Generics::GetExactInstantiationsOfMethodAndItsClassFromCallInformation(
            pMD,
            PTR_VOID((TADDR)genericsToken),
            &thSpecificClass,
            &pSpecificMethod);
            );
    if (!fExact ||
        !thSpecificClass.GetMethodTable()->SanityCheck() ||
        !pSpecificMethod->GetMethodTable()->SanityCheck())
    {
        // Use the canonical MethodTable and MethodDesc if the exact generics token is not available.
        thSpecificClass = TypeHandle(pMD->GetMethodTable());
        pSpecificMethod = pMD;
    }

    // Retrieve the array of class type parameters and the array of method type parameters.
    Instantiation classInst  = pSpecificMethod->GetExactClassInstantiation(thSpecificClass);
    Instantiation methodInst = pSpecificMethod->GetMethodInstantiation();

    _ASSERTE((classInst.IsEmpty())  == (cGenericClassTypeParams  == 0));
    _ASSERTE((methodInst.IsEmpty()) == (cGenericMethodTypeParams == 0));

    // allocate memory for the return array
    pGenericTypeParams->Alloc(cTotalGenericTypeParams);

    for (UINT32 i = 0; i < cTotalGenericTypeParams; i++)
    {
        // Retrieve the current type parameter depending on the index.
        TypeHandle thCurrent;
        if (i < cGenericClassTypeParams)
        {
            thCurrent = classInst[i];
        }
        else
        {
            thCurrent = methodInst[i - cGenericClassTypeParams];
        }

        // There is the possibility that we'll get this far with a dump and not fail, but still
        // not be able to get full info for a particular param.
        EX_TRY_ALLOW_DATATARGET_MISSING_MEMORY_WITH_HANDLER
        {
            // Fill in the struct using the TypeHandle of the current type parameter if we can.
            VMPTR_TypeHandle vmTypeHandle = VMPTR_TypeHandle::NullPtr();
            vmTypeHandle.SetDacTargetPtr(thCurrent.AsTAddr());
            TypeHandleToExpandedTypeInfo(NoValueTypeBoxing,
                                         vmAppDomain,
                                         vmTypeHandle,
                                         &((*pGenericTypeParams)[i]));
        }
        EX_CATCH_ALLOW_DATATARGET_MISSING_MEMORY_WITH_HANDLER
        {
            // On failure for a particular type, default it back to System.__Canon.
            VMPTR_TypeHandle vmTHCanon = VMPTR_TypeHandle::NullPtr();
            TypeHandle thCanon = TypeHandle(g_pCanonMethodTableClass);
            vmTHCanon.SetDacTargetPtr(thCanon.AsTAddr());
            TypeHandleToExpandedTypeInfo(NoValueTypeBoxing,
                                         vmAppDomain,
                                         vmTHCanon,
                                         &((*pGenericTypeParams)[i]));
        }
        EX_END_CATCH_ALLOW_DATATARGET_MISSING_MEMORY_WITH_HANDLER
    }
}

//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::GetClassOrValueTypeHandle
// get a typehandle for a class or valuetype from basic type data (metadata token
// and domain file).
// Arguments:
//     input: pData - contains the metadata token and domain file
// Return value: the type handle for the corresponding type
//-----------------------------------------------------------------------------
TypeHandle DacDbiInterfaceImpl::GetClassOrValueTypeHandle(DebuggerIPCE_BasicTypeData * pData)
{
    TypeHandle typeHandle;

    // if we already have a type handle, just return it
    if (!pData->vmTypeHandle.IsNull())
    {
        typeHandle = TypeHandle::FromPtr(pData->vmTypeHandle.GetDacPtr());
    }
    // otherwise, have the loader look it up using the metadata token and domain file
    else
    {
        DomainAssembly * pDomainAssembly = pData->vmDomainAssembly.GetDacPtr();
        Module *     pModule = pDomainAssembly->GetModule();

        typeHandle = ClassLoader::LookupTypeDefOrRefInModule(pModule, pData->metadataToken);
        if (typeHandle.IsNull())
        {
            LOG((LF_CORDB, LL_INFO10000, "D::BTITTH: class isn't loaded.\n"));
            ThrowHR(CORDBG_E_CLASS_NOT_LOADED);
        }

        _ASSERTE(typeHandle.GetNumGenericArgs() == 0);
    }

    return typeHandle;

} // DacDbiInterfaceImpl::GetClassOrValueTypeHandle

//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::GetExactArrayTypeHandle
// get an exact type handle for an array type
// Arguments:
//     input: pTopLevelTypeData - type information for a top-level array type
//            pArgInfo           - contains the following information:
//                 m_genericArgsCount  - number of generic parameters for the element type--this should be 1
//                 m_pGenericArgs      - pointer to the generic parameter for the element type--this is
//                                       effectively a one-element list. These are the actual parameters
// Return Value: the exact type handle for the type
//-----------------------------------------------------------------------------
TypeHandle DacDbiInterfaceImpl::GetExactArrayTypeHandle(DebuggerIPCE_ExpandedTypeData * pTopLevelTypeData,
                                                        ArgInfoList *                   pArgInfo)
{
    TypeHandle typeArg;

    _ASSERTE(pArgInfo->Count() == 1);

    // get the type handle for the element type
    typeArg = BasicTypeInfoToTypeHandle(&((*pArgInfo)[0]));

    // get the exact type handle for the array type
    return FindLoadedArrayType(pTopLevelTypeData->elementType,
                               typeArg,
                               pTopLevelTypeData->ArrayTypeData.arrayRank);

} // DacDbiInterfaceImpl::GetExactArrayTypeHandle

//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::GetExactPtrOrByRefTypeHandle
// get an exact type handle for a PTR or BYREF type
// Arguments:
//     input: pTopLevelTypeData - type information for the PTR or BYREF type
//            pArgInfo           - contains the following information:
//                 m_genericArgsCount  - number of generic parameters for the element type--this should be 1
//                 m_pGenericArgs      - pointer to the generic parameter for the element type--this is
//                                       effectively a one-element list. These are the actual parameters
// Return Value: the exact type handle for the type
//-----------------------------------------------------------------------------
TypeHandle DacDbiInterfaceImpl::GetExactPtrOrByRefTypeHandle(DebuggerIPCE_ExpandedTypeData * pTopLevelTypeData,
                                                             ArgInfoList *                   pArgInfo)
{
    TypeHandle typeArg;
    _ASSERTE(pArgInfo->Count() == 1);

    // get the type handle for the referent
    typeArg = BasicTypeInfoToTypeHandle(&((*pArgInfo)[0]));

    // get the exact type handle for the PTR or BYREF type
    return FindLoadedPointerOrByrefType(pTopLevelTypeData->elementType, typeArg);

} // DacDbiInterfaceImpl::GetExactPtrOrByRefTypeHandle

//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::GetExactClassTypeHandle
// get an exact type handle for a CLASS or VALUETYPE type
// Arguments:
//     input: pTopLevelTypeData - type information for the CLASS or VALUETYPE type
//            pArgInfo           - contains the following information:
//                 m_genericArgsCount  - number of generic parameters for the class
//                 m_pGenericArgs      - list of generic parameters for the class--these
//                                       are the actual parameters
// Return Value: the exact type handle for the type
//-----------------------------------------------------------------------------
TypeHandle DacDbiInterfaceImpl::GetExactClassTypeHandle(DebuggerIPCE_ExpandedTypeData * pTopLevelTypeData,
                                                        ArgInfoList *                   pArgInfo)
{
    Module *     pModule = pTopLevelTypeData->ClassTypeData.vmModule.GetDacPtr();
    int          argCount = pArgInfo->Count();

    TypeHandle typeConstructor =
        ClassLoader::LookupTypeDefOrRefInModule(pModule, pTopLevelTypeData->ClassTypeData.metadataToken);

    // If we can't find the class, throw the appropriate HR.
    if (typeConstructor.IsNull())
    {
        LOG((LF_CORDB, LL_INFO10000, "D::ETITTH: class isn't loaded.\n"));
        ThrowHR(CORDBG_E_CLASS_NOT_LOADED);
    }

    // if there are no generic parameters, we already have the correct type handle
    if (argCount == 0)
    {
        return typeConstructor;
    }

    // we have generic parameters--first validate we have a number consistent with the list
    // of parameters we received
    if ((unsigned int)argCount != typeConstructor.GetNumGenericArgs())
    {
        LOG((LF_CORDB, LL_INFO10000,
            "D::ETITTH: wrong number of type parameters, %d given, %d expected\n",
            argCount, typeConstructor.GetNumGenericArgs()));
        _ASSERTE((unsigned int)argCount == typeConstructor.GetNumGenericArgs());
        ThrowHR(E_FAIL);
    }

    // now we allocate a list to store the type handles for each parameter
    S_UINT32 allocSize = S_UINT32(argCount) * S_UINT32(sizeof(TypeHandle));
    if (allocSize.IsOverflow())
    {
        ThrowHR(E_OUTOFMEMORY);
    }

    NewArrayHolder<TypeHandle> pInst(new TypeHandle[allocSize.Value()]);

    // convert the type information for each parameter to its corresponding type handle
    // and store it in the list
    for (unsigned int i = 0; i < (unsigned int)argCount; i++)
    {
        pInst[i] = BasicTypeInfoToTypeHandle(&((*pArgInfo)[i]));
    }

    // Finally, we find the type handle corresponding to this particular instantiation
    return FindLoadedInstantiation(typeConstructor.GetModule(),
                                   typeConstructor.GetCl(),
                                   argCount,
                                   pInst);

} // DacDbiInterfaceImpl::GetExactClassTypeHandle

//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::GetExactFnPtrTypeHandle
// get an exact type handle for a FNPTR type
// Arguments:
//     input: pArgInfo - Contains the following information:
//                 m_genericArgsCount  - number of generic parameters for the referent
//                 m_pGenericArgs      - list of generic parameters for the referent--these
//                                       are the actual parameters for the function signature
// Return Value: the exact type handle for the type
//-----------------------------------------------------------------------------
TypeHandle DacDbiInterfaceImpl::GetExactFnPtrTypeHandle(ArgInfoList * pArgInfo)
{
    // allocate a list to store the type handles for each parameter
    S_UINT32 allocSize = S_UINT32(pArgInfo->Count()) * S_UINT32(sizeof(TypeHandle));
    if( allocSize.IsOverflow() )
    {
        ThrowHR(E_OUTOFMEMORY);
    }
    NewArrayHolder<TypeHandle> pInst(new TypeHandle[allocSize.Value()]);

    // convert the type information for each parameter to its corresponding type handle
    // and store it in the list
    for (unsigned int i = 0; i < pArgInfo->Count(); i++)
    {
        pInst[i] = BasicTypeInfoToTypeHandle(&((*pArgInfo)[i]));
    }

    // find the type handle corresponding to this particular FNPTR
    return FindLoadedFnptrType(pArgInfo->Count(), pInst);
} // DacDbiInterfaceImpl::GetExactFnPtrTypeHandle

//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::BasicTypeInfoToTypeHandle
// Convert basic type info for a type parameter that came from a top-level type to
// the corresponding type handle. If the type parameter is an array or pointer
// type, we simply extract the LS type handle from the VMPTR_TypeHandle that is
// part of the type information. If the type parameter is a class or value type,
// we use the metadata token and domain file in the type info to look up the
// appropriate type handle. If the type parameter is any other types, we get the
// type handle by having the loader look up the type handle for the element type.
// Arguments:
//     input: pArgTypeData - basic type information for the type.
// Return Value: the type handle for the type.
//-----------------------------------------------------------------------------
TypeHandle DacDbiInterfaceImpl::BasicTypeInfoToTypeHandle(DebuggerIPCE_BasicTypeData * pArgTypeData)
{
    LOG((LF_CORDB, LL_INFO10000,
        "D::BTITTH: expanding basic right-side type to left-side type, ELEMENT_TYPE: %d.\n",
        pArgTypeData->elementType));
    TypeHandle typeHandle = TypeHandle();

    switch (pArgTypeData->elementType)
    {
        case ELEMENT_TYPE_ARRAY:
        case ELEMENT_TYPE_SZARRAY:
        case ELEMENT_TYPE_PTR:
        case ELEMENT_TYPE_BYREF:
        case ELEMENT_TYPE_FNPTR:
            _ASSERTE(!pArgTypeData->vmTypeHandle.IsNull());
            typeHandle = TypeHandle::FromPtr(pArgTypeData->vmTypeHandle.GetDacPtr());
            break;

        case ELEMENT_TYPE_CLASS:
        case ELEMENT_TYPE_VALUETYPE:
            typeHandle = GetClassOrValueTypeHandle(pArgTypeData);
            break;

        default:
            typeHandle = FindLoadedElementType(pArgTypeData->elementType);
            break;
    }
    if (typeHandle.IsNull())
    {
        ThrowHR(CORDBG_E_CLASS_NOT_LOADED);
    }
    return typeHandle;
} // DacDbiInterfaceImpl::BasicTypeInfoToTypeHandle


//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::ExpandedTypeInfoToTypeHandle
// Convert type information for a top-level type to an exact type handle. This
// information includes information about the element type if the top-level type is
// an array type, the referent if the top-level type is a pointer type, or actual
// parameters if the top-level type is a generic class or value type.
// Arguments:
//     input: pTopLevelTypeData - type information for the top-level type
//            pArgInfo           - contains the following information:
//                 m_genericArtsCount  - number of parameters
//                 m_pGenericArgs      - list of actual parameters
// Return Value: the exact type handle corresponding to the type represented by
//            pTopLevelTypeData
//-----------------------------------------------------------------------------
TypeHandle DacDbiInterfaceImpl::ExpandedTypeInfoToTypeHandle(DebuggerIPCE_ExpandedTypeData * pTopLevelTypeData,
                                                             ArgInfoList *                   pArgInfo)
{
    WRAPPER_NO_CONTRACT;

    LOG((LF_CORDB, LL_INFO10000,
        "D::ETITTH: expanding right-side type to left-side type, ELEMENT_TYPE: %d.\n",
        pData->elementType));

    TypeHandle typeHandle = TypeHandle();
    // depending on the top-level type, get the type handle incorporating information about any type arguments
    switch (pTopLevelTypeData->elementType)
    {
        case ELEMENT_TYPE_ARRAY:
        case ELEMENT_TYPE_SZARRAY:
            typeHandle = GetExactArrayTypeHandle(pTopLevelTypeData, pArgInfo);
            break;

        case ELEMENT_TYPE_PTR:
        case ELEMENT_TYPE_BYREF:
            typeHandle = GetExactPtrOrByRefTypeHandle(pTopLevelTypeData, pArgInfo);
            break;

        case ELEMENT_TYPE_CLASS:
        case ELEMENT_TYPE_VALUETYPE:
            typeHandle = GetExactClassTypeHandle(pTopLevelTypeData, pArgInfo);
            break;
        case ELEMENT_TYPE_FNPTR:
            typeHandle = GetExactFnPtrTypeHandle(pArgInfo);
            break;
        default:
            typeHandle = FindLoadedElementType(pTopLevelTypeData->elementType);
            break;
    } // end switch (pData->elementType)

    if (typeHandle.IsNull())
    {
        // This may fail because there are cases when a type can be used (and so visible to the
        // debugger), but not yet loaded to the point of being available in the EETypeHashTable.
        // For example, generic value types (without explicit constructors) may not need their
        // exact instantiation type to be loaded in order to be used as a field of an object
        // created on the heap
        LOG((LF_CORDB, LL_INFO10000, "D::ETITTH: type isn't loaded.\n"));
        ThrowHR(CORDBG_E_CLASS_NOT_LOADED);
    }
    return typeHandle;
} // DacDbiInterfaceImpl::ExpandedTypeInfoToTypeHandle

// ----------------------------------------------------------------------------
// DacDbi API: GetThreadStaticAddress
// Get the target field address of a thread local static.
//
// Notes:
// The address is constant and could be cached.
//
// This can commonly fail, in which case, it will return NULL.
// ----------------------------------------------------------------------------
CORDB_ADDRESS DacDbiInterfaceImpl::GetThreadStaticAddress(VMPTR_FieldDesc vmField,
                                                          VMPTR_Thread    vmRuntimeThread)
{
    DD_ENTER_MAY_THROW;

    Thread * pRuntimeThread = vmRuntimeThread.GetDacPtr();
    PTR_FieldDesc pFieldDesc = vmField.GetDacPtr();
    TADDR fieldAddress = NULL;

    _ASSERTE(pRuntimeThread != NULL);

    // Find out whether the field is thread local and get its address.
    if (pFieldDesc->IsThreadStatic())
    {
        fieldAddress = pRuntimeThread->GetStaticFieldAddrNoCreate(pFieldDesc);
    }
    else
    {
        // In case we have more special cases added later, this will allow us to notice the need to
        // update this function.
        ThrowHR(E_NOTIMPL);
    }
    return fieldAddress;

} // DacDbiInterfaceImpl::GetThreadStaticAddress

    // Get the target field address of a collectible types static.
CORDB_ADDRESS DacDbiInterfaceImpl::GetCollectibleTypeStaticAddress(VMPTR_FieldDesc vmField,
                                                                   VMPTR_AppDomain vmAppDomain)
{
    DD_ENTER_MAY_THROW;

    AppDomain * pAppDomain = vmAppDomain.GetDacPtr();
    PTR_FieldDesc pFieldDesc = vmField.GetDacPtr();
    _ASSERTE(pAppDomain != NULL);

    //
    // Verify this field is of the right type
    //
    if(!pFieldDesc->IsStatic() ||
       pFieldDesc->IsSpecialStatic())
    {
        _ASSERTE(!"BUG: Unsupported static field type for collectible types");
    }

    //
    // Check that the data is available
    //
    /* TODO: Ideally we should be checking if the class is allocated first, however
             we don't appear to be doing this even for non-collectible statics and
             we have never seen an issue.
    */

    //
    // Get the address
    //
    PTR_VOID base = pFieldDesc->GetBase();
    if (base == PTR_NULL)
    {
        return PTR_HOST_TO_TADDR(NULL);
    }

    //
    // Store the result and return
    //
    PTR_VOID addr = pFieldDesc->GetStaticAddressHandle(base);
    return PTR_TO_TADDR(addr);

} // DacDbiInterfaceImpl::GetCollectibleTypeStaticAddress

// DacDbi API: GetTypeHandleParams
// - gets the necessary data for a type handle, i.e. its type parameters, e.g. "String" and "List<int>" from the type handle
//   for "Dict<String,List<int>>", and sends it back to the right side.
// - pParams is allocated and initialized by this function
// - This should not fail except for OOM
void DacDbiInterfaceImpl::GetTypeHandleParams(VMPTR_AppDomain  vmAppDomain,
                                              VMPTR_TypeHandle vmTypeHandle,
                                              TypeParamsList * pParams)
{
    DD_ENTER_MAY_THROW

    TypeHandle typeHandle = TypeHandle::FromPtr(vmTypeHandle.GetDacPtr());
    LOG((LF_CORDB, LL_INFO10000, "D::GTHP: getting type parameters for 0x%08x 0x%0x8.\n",
         vmAppDomain.GetDacPtr(), typeHandle.AsPtr()));


    // Find the class given its type handle.
    _ASSERTE(pParams->IsEmpty());
    pParams->Alloc(typeHandle.GetNumGenericArgs());

    // collect type information for each type parameter
    for (unsigned int i = 0; i < pParams->Count(); ++i)
    {
        VMPTR_TypeHandle thInst = VMPTR_TypeHandle::NullPtr();
        thInst.SetDacTargetPtr(typeHandle.GetInstantiation()[i].AsTAddr());

        TypeHandleToExpandedTypeInfo(NoValueTypeBoxing,
                                     vmAppDomain,
                                     thInst,
                                     &((*pParams)[i]));
    }

    LOG((LF_CORDB, LL_INFO10000, "D::GTHP: sending  result"));
} // DacDbiInterfaceImpl::GetTypeHandleParams

//-----------------------------------------------------------------------------
// DacDbi API: GetSimpleType
// gets the metadata token and domain file corresponding to a simple type
//-----------------------------------------------------------------------------
void DacDbiInterfaceImpl::GetSimpleType(VMPTR_AppDomain    vmAppDomain,
                                        CorElementType     simpleType,
                                        mdTypeDef         *pMetadataToken,
                                        VMPTR_Module      *pVmModule,
                                        VMPTR_DomainAssembly  *pVmDomainAssembly)
{
    DD_ENTER_MAY_THROW;

    AppDomain *pAppDomain = vmAppDomain.GetDacPtr();

    // if we fail to get either a valid type handle or module, we will want to send back
    // a NULL domain file too, so we'll to preinitialize this here.
    _ASSERTE(pVmDomainAssembly != NULL);
    *pVmDomainAssembly = VMPTR_DomainAssembly::NullPtr();
    // FindLoadedElementType will return NULL if the type hasn't been loaded yet.
    TypeHandle typeHandle =  FindLoadedElementType(simpleType);

    if (typeHandle.IsNull())
    {
        ThrowHR(CORDBG_E_CLASS_NOT_LOADED);
    }
    else
    {
        _ASSERTE(pMetadataToken != NULL);
        *pMetadataToken = typeHandle.GetCl();

        Module * pModule = typeHandle.GetModule();
        if (pModule == NULL)
            ThrowHR(CORDBG_E_TARGET_INCONSISTENT);

        pVmModule->SetHostPtr(pModule);

        if (pAppDomain)
        {
            pVmDomainAssembly->SetHostPtr(pModule->GetDomainAssembly());
            if (pVmDomainAssembly->IsNull())
                ThrowHR(CORDBG_E_TARGET_INCONSISTENT);
        }
    }

    LOG((LF_CORDB, LL_INFO10000, "D::STI: sending result.\n"));
} // DacDbiInterfaceImpl::GetSimpleType

BOOL DacDbiInterfaceImpl::IsExceptionObject(VMPTR_Object vmObject)
{
    DD_ENTER_MAY_THROW;

    Object* objPtr = vmObject.GetDacPtr();
    MethodTable* pMT = objPtr->GetMethodTable();

    return IsExceptionObject(pMT);
}

BOOL DacDbiInterfaceImpl::IsExceptionObject(MethodTable* pMT)
{
    PTR_MethodTable pExMT = g_pExceptionClass;

    TADDR targetMT = dac_cast<TADDR>(pMT);
    TADDR exceptionMT = dac_cast<TADDR>(pExMT);

    do
    {
        if (targetMT == exceptionMT)
            return TRUE;

        pMT = pMT->GetParentMethodTable();
        targetMT = dac_cast<TADDR>(pMT);
    } while (pMT);

    return FALSE;
}

HRESULT DacDbiInterfaceImpl::GetMethodDescPtrFromIpEx(TADDR funcIp, VMPTR_MethodDesc* ppMD)
{
    DD_ENTER_MAY_THROW;

    // The fast path is check if the code is jitted and the code manager has it available.
    CLRDATA_ADDRESS mdAddr;
    HRESULT hr = g_dacImpl->GetMethodDescPtrFromIP(TO_CDADDR(funcIp), &mdAddr);
    if (S_OK == hr)
    {
        ppMD->SetDacTargetPtr(CLRDATA_ADDRESS_TO_TADDR(mdAddr));
        return hr;
    }

    // Otherwise try to see if a method desc is available for the method that isn't jitted by walking the code stubs.
    MethodDesc* pMD = MethodTable::GetMethodDescForSlotAddress(PINSTRToPCODE(funcIp));

    if (pMD == NULL)
        return E_INVALIDARG;

    ppMD->SetDacTargetPtr(PTR_HOST_TO_TADDR(pMD));
    return S_OK;
}

BOOL DacDbiInterfaceImpl::IsDelegate(VMPTR_Object vmObject)
{
    DD_ENTER_MAY_THROW;

    if (vmObject.IsNull())
        return FALSE;

    Object *pObj = vmObject.GetDacPtr();
    return pObj->GetGCSafeMethodTable()->IsDelegate();
}


//-----------------------------------------------------------------------------
// DacDbi API: GetDelegateType
// Given a delegate pointer, compute the type of delegate according to the data held in it.
//-----------------------------------------------------------------------------
HRESULT DacDbiInterfaceImpl::GetDelegateType(VMPTR_Object delegateObject, DelegateType *delegateType)
{
    DD_ENTER_MAY_THROW;

    _ASSERTE(!delegateObject.IsNull());
    _ASSERTE(delegateType != NULL);

#ifdef _DEBUG
    // ensure we have a Delegate object
    IsDelegate(delegateObject);
#endif

    // Ideally, we would share the implementation of this method with the runtime, or get the same information
    // we are getting from here from other EE methods. Nonetheless, currently the implementation is sharded across
    // several pieces of logic so this replicates the logic mostly due to time constraints. The Mainly from:
    // - System.Private.CoreLib!System.Delegate.GetMethodImpl and System.Private.CoreLib!System.MulticastDelegate.GetMethodImpl
    // - System.Private.CoreLib!System.Delegate.GetTarget and System.Private.CoreLib!System.MulticastDelegate.GetTarget
    // - coreclr!COMDelegate::GetMethodDesc and coreclr!COMDelegate::FindMethodHandle
    // - coreclr!COMDelegate::DelegateConstruct and the delegate type table in
    // - DELEGATE KINDS TABLE in comdelegate.cpp

    *delegateType = DelegateType::kUnknownDelegateType;
    PTR_DelegateObject pDelObj = dac_cast<PTR_DelegateObject>(delegateObject.GetDacPtr());
    INT_PTR invocationCount = pDelObj->GetInvocationCount();

    if (invocationCount == -1)
    {
        // We could get a native code for this case from _methodPtr, but not a methodDef as we'll need.
        // We can also get the shuffling thunk. However, this doesn't have a token and there's
        // no easy way to expose through the DBI now.
        *delegateType = kUnmanagedFunctionDelegate;
        return S_OK;
    }

    PTR_Object pInvocationList = OBJECTREFToObject(pDelObj->GetInvocationList());

    if (invocationCount == NULL)
    {
        if (pInvocationList == NULL)
        {
            // If this delegate points to a static function or this is a open virtual delegate, this should be non-null
            // Special case: This might fail in a VSD delegate (instance open virtual)...
            // TODO: There is the special signatures cases missing.
            TADDR targetMethodPtr = PCODEToPINSTR(pDelObj->GetMethodPtrAux());
            if (targetMethodPtr == NULL)
            {
                // Static extension methods, other closed static delegates, and instance delegates fall into this category.
                *delegateType = kClosedDelegate;
            }
            else {
                *delegateType = kOpenDelegate;
            }

            return S_OK;
        }
    }
    else
    {
        if (pInvocationList != NULL)
        {
            PTR_MethodTable invocationListMT = pInvocationList->GetGCSafeMethodTable();

            if (invocationListMT->IsArray())
                *delegateType = kTrueMulticastDelegate;

            if (invocationListMT->IsDelegate())
                *delegateType = kWrapperDelegate;

            // Cases missing: Loader allocator, or dynamic resolver.
            return S_OK;
        }

        // According to the table in comdelegates.cpp, there shouldn't be a case where .
        // Multicast falls outside of the table, so not
    }

    _ASSERT(FALSE);
    *delegateType = kUnknownDelegateType;
    return CORDBG_E_UNSUPPORTED_DELEGATE;
}

HRESULT DacDbiInterfaceImpl::GetDelegateFunctionData(
    DelegateType delegateType,
    VMPTR_Object delegateObject,
    OUT VMPTR_DomainAssembly *ppFunctionDomainAssembly,
    OUT mdMethodDef *pMethodDef)
{
    DD_ENTER_MAY_THROW;

#ifdef _DEBUG
    // ensure we have a Delegate object
    IsDelegate(delegateObject);
#endif

    HRESULT hr = S_OK;
    PTR_DelegateObject pDelObj = dac_cast<PTR_DelegateObject>(delegateObject.GetDacPtr());
    TADDR targetMethodPtr = NULL;
    VMPTR_MethodDesc pMD;

    switch (delegateType)
    {
    case kClosedDelegate:
        targetMethodPtr = PCODEToPINSTR(pDelObj->GetMethodPtr());
        break;
    case kOpenDelegate:
        targetMethodPtr = PCODEToPINSTR(pDelObj->GetMethodPtrAux());
        break;
    default:
        return E_FAIL;
    }

    hr = GetMethodDescPtrFromIpEx(targetMethodPtr, &pMD);
    if (hr != S_OK)
        return hr;

    ppFunctionDomainAssembly->SetDacTargetPtr(dac_cast<TADDR>(pMD.GetDacPtr()->GetModule()->GetDomainAssembly()));
    *pMethodDef = pMD.GetDacPtr()->GetMemberDef();

    return hr;
}

HRESULT DacDbiInterfaceImpl::GetDelegateTargetObject(
    DelegateType delegateType,
    VMPTR_Object delegateObject,
    OUT VMPTR_Object *ppTargetObj,
    OUT VMPTR_AppDomain *ppTargetAppDomain)
{
    DD_ENTER_MAY_THROW;

#ifdef _DEBUG
    // ensure we have a Delegate object
    IsDelegate(delegateObject);
#endif

    HRESULT hr = S_OK;
    PTR_DelegateObject pDelObj = dac_cast<PTR_DelegateObject>(delegateObject.GetDacPtr());

    switch (delegateType)
    {
        case kClosedDelegate:
        {
            PTR_Object pRemoteTargetObj = OBJECTREFToObject(pDelObj->GetTarget());
            ppTargetObj->SetDacTargetPtr(pRemoteTargetObj.GetAddr());
            ppTargetAppDomain->SetDacTargetPtr(dac_cast<TADDR>(pRemoteTargetObj->GetGCSafeMethodTable()->GetDomain()->AsAppDomain()));
            break;
        }

        default:
            ppTargetObj->SetDacTargetPtr(NULL);
            ppTargetAppDomain->SetDacTargetPtr(dac_cast<TADDR>(pDelObj->GetGCSafeMethodTable()->GetDomain()->AsAppDomain()));
            break;
    }

    return hr;
}

static bool TrackMemoryRangeHelper(PTR_VOID pvArgs, PTR_VOID pvAllocationBase, SIZE_T cbReserved)
{
    // The pvArgs is really pointing to a debugger-side container. Sadly the callback only takes a PTR_VOID.
    CQuickArrayList<COR_MEMORY_RANGE> *rangeCollection =
                                        (CQuickArrayList<COR_MEMORY_RANGE>*)(dac_cast<TADDR>(pvArgs));
    TADDR rangeStart = dac_cast<TADDR>(pvAllocationBase);
    TADDR rangeEnd = rangeStart + cbReserved;
    rangeCollection->Push({rangeStart, rangeEnd});

    // This is a tracking function, not a search callback. Pretend we never found what we were looking for
    // to get all possible ranges.
    return false;
}

void DacDbiInterfaceImpl::EnumerateMemRangesForLoaderAllocator(PTR_LoaderAllocator pLoaderAllocator, CQuickArrayList<COR_MEMORY_RANGE> *rangeAcummulator)
{
    CQuickArrayList<PTR_LoaderHeap> heapsToEnumerate;

    // We always expect to see these three heaps
    _ASSERTE(pLoaderAllocator->GetLowFrequencyHeap() != NULL);
    heapsToEnumerate.Push(pLoaderAllocator->GetLowFrequencyHeap());

    _ASSERTE(pLoaderAllocator->GetHighFrequencyHeap() != NULL);
    heapsToEnumerate.Push(pLoaderAllocator->GetHighFrequencyHeap());

    _ASSERTE(pLoaderAllocator->GetStubHeap() != NULL);
    heapsToEnumerate.Push(pLoaderAllocator->GetStubHeap());

    // GetVirtualCallStubManager returns VirtualCallStubManager*, but it's really an address to target as
    // pLoaderAllocator is DACized. Cast it so we don't try to a Host to Target translation.
    VirtualCallStubManager *pVcsMgr = pLoaderAllocator->GetVirtualCallStubManager();
    LOG((LF_CORDB, LL_INFO10000, "DDBII::EMRFLA: VirtualCallStubManager 0x%x\n", PTR_HOST_TO_TADDR(pVcsMgr)));
    if (pVcsMgr)
    {
        if (pVcsMgr->indcell_heap != NULL) heapsToEnumerate.Push(pVcsMgr->indcell_heap);
        if (pVcsMgr->cache_entry_heap != NULL) heapsToEnumerate.Push(pVcsMgr->cache_entry_heap);
    }

    TADDR rangeAccumAsTaddr = TO_TADDR(rangeAcummulator);
    for (uint32_t i = 0; i < (uint32_t)heapsToEnumerate.Size(); i++)
    {
        LOG((LF_CORDB, LL_INFO10000, "DDBII::EMRFLA: LoaderHeap 0x%x\n", heapsToEnumerate[i].GetAddr()));
        heapsToEnumerate[i]->EnumPageRegions(TrackMemoryRangeHelper, rangeAccumAsTaddr);
    }
}

void DacDbiInterfaceImpl::EnumerateMemRangesForJitCodeHeaps(CQuickArrayList<COR_MEMORY_RANGE> *rangeAcummulator)
{
    // We should always have a valid EEJitManager with at least one code heap.
    EEJitManager *pEM = ExecutionManager::GetEEJitManager();
    _ASSERTE(pEM != NULL && pEM->m_pCodeHeap.IsValid());

    PTR_HeapList pHeapList = pEM->m_pCodeHeap;
    while (pHeapList != NULL)
    {
        CodeHeap *pHeap = pHeapList->pHeap;
        DacpJitCodeHeapInfo jitCodeHeapInfo = DACGetHeapInfoForCodeHeap(pHeap);

        switch (jitCodeHeapInfo.codeHeapType)
        {
            case CODEHEAP_LOADER:
            {
                TADDR targetLoaderHeap = CLRDATA_ADDRESS_TO_TADDR(jitCodeHeapInfo.LoaderHeap);
                LOG((LF_CORDB, LL_INFO10000,
                    "DDBII::EMRFJCH: LoaderCodeHeap 0x%x with LoaderHeap at 0x%x\n",
                    PTR_HOST_TO_TADDR(pHeap), targetLoaderHeap));
                PTR_ExplicitControlLoaderHeap pLoaderHeap = PTR_ExplicitControlLoaderHeap(targetLoaderHeap);
                pLoaderHeap->EnumPageRegions(TrackMemoryRangeHelper, TO_TADDR(rangeAcummulator));
                break;
            }

            case CODEHEAP_HOST:
            {
                LOG((LF_CORDB, LL_INFO10000,
                    "DDBII::EMRFJCH: HostCodeHeap 0x%x\n",
                    PTR_HOST_TO_TADDR(pHeap)));
                rangeAcummulator->Push({
                    CLRDATA_ADDRESS_TO_TADDR(jitCodeHeapInfo.HostData.baseAddr),
                    CLRDATA_ADDRESS_TO_TADDR(jitCodeHeapInfo.HostData.currentAddr)
                });
                break;
            }

            default:
            {
                LOG((LF_CORDB, LL_INFO10000, "DDBII::EMRFJCH: unknown heap type at 0x%x\n\n", pHeap));
                _ASSERTE("Unknown heap type enumerating code ranges.");
                break;
            }
        }

        pHeapList = pHeapList->GetNext();
    }
}

HRESULT DacDbiInterfaceImpl::GetLoaderHeapMemoryRanges(DacDbiArrayList<COR_MEMORY_RANGE> *pRanges)
{
    LOG((LF_CORDB, LL_INFO10000, "DDBII::GLHMR\n"));
    DD_ENTER_MAY_THROW;

    HRESULT hr = S_OK;

    EX_TRY
    {
        CQuickArrayList<COR_MEMORY_RANGE> memoryRanges;

        // Anything that's loaded in the SystemDomain or into the main AppDomain's default context in .NET Core
        // and after uses only one global allocator. Enumerating that one is enough for most purposes.
        // This doesn't consider any uses of AssemblyLoadingContexts (Unloadable or not). Each context has
        // it's own LoaderAllocator, but there's no easy way of getting a hand at them other than going through
        // the heap, getting a managed LoaderAllocators, from there getting a Scout, and from there getting a native
        // pointer to the LoaderAllocator tos enumerate.
        PTR_LoaderAllocator pGlobalAllocator = SystemDomain::System()->GetLoaderAllocator();
        _ASSERTE(pGlobalAllocator);
        EnumerateMemRangesForLoaderAllocator(pGlobalAllocator, &memoryRanges);

        EnumerateMemRangesForJitCodeHeaps(&memoryRanges);

        // This code doesn't enumerate module thunk heaps to support IJW.
        // It's a fairly rare scenario and requires to enumerate all modules.
        // The return for such added time is minimal.

        _ASSERTE(memoryRanges.Size() < INT_MAX);
        pRanges->Init(memoryRanges.Ptr(), (UINT) memoryRanges.Size());
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

void DacDbiInterfaceImpl::GetStackFramesFromException(VMPTR_Object vmObject, DacDbiArrayList<DacExceptionCallStackData>& dacStackFrames)
{
    DD_ENTER_MAY_THROW;

    PTR_Object objPtr = vmObject.GetDacPtr();

#ifdef _DEBUG
    // ensure we have an Exception object
    MethodTable* pMT = objPtr->GetMethodTable();
    _ASSERTE(IsExceptionObject(pMT));
#endif

    OBJECTREF objRef = ObjectToOBJECTREF(objPtr);

    DebugStackTrace::GetStackFramesData stackFramesData;

    stackFramesData.pDomain = NULL;
    stackFramesData.skip = 0;
    stackFramesData.NumFramesRequested = 0;

    DebugStackTrace::GetStackFramesFromException(&objRef, &stackFramesData);

    INT32 dacStackFramesLength = stackFramesData.cElements;

    if (dacStackFramesLength > 0)
    {
        dacStackFrames.Alloc(dacStackFramesLength);

        for (INT32 index = 0; index < dacStackFramesLength; ++index)
        {
            DebugStackTrace::DebugStackTraceElement const& currentElement = stackFramesData.pElements[index];
            DacExceptionCallStackData& currentFrame = dacStackFrames[index];

            Module* pModule = currentElement.pFunc->GetModule();
            BaseDomain* pBaseDomain = currentElement.pFunc->GetAssembly()->GetDomain();

            AppDomain* pDomain = NULL;
            DomainAssembly* pDomainAssembly = NULL;

            pDomain = pBaseDomain->AsAppDomain();

            _ASSERTE(pDomain != NULL);

            pDomainAssembly = pModule->GetDomainAssembly();
            _ASSERTE(pDomainAssembly != NULL);

            currentFrame.vmAppDomain.SetHostPtr(pDomain);
            currentFrame.vmDomainAssembly.SetHostPtr(pDomainAssembly);
            currentFrame.ip = currentElement.ip;
            currentFrame.methodDef = currentElement.pFunc->GetMemberDef();
            currentFrame.isLastForeignExceptionFrame = (currentElement.flags & STEF_LAST_FRAME_FROM_FOREIGN_STACK_TRACE) != 0;
        }
    }
}

#ifdef FEATURE_COMINTEROP

PTR_RCW GetRcwFromVmptrObject(VMPTR_Object vmObject)
{
    PTR_RCW pRCW = NULL;

    Object* objPtr = vmObject.GetDacPtr();

    PTR_SyncBlock pSyncBlock = NULL;
    pSyncBlock = objPtr->PassiveGetSyncBlock();
    if (pSyncBlock == NULL)
        return pRCW;

    PTR_InteropSyncBlockInfo pInfo = NULL;
    pInfo = pSyncBlock->GetInteropInfoNoCreate();
    if (pInfo == NULL)
        return pRCW;

    pRCW = dac_cast<PTR_RCW>(pInfo->DacGetRawRCW());

    return pRCW;
}

#endif

BOOL DacDbiInterfaceImpl::IsRcw(VMPTR_Object vmObject)
{
#ifdef FEATURE_COMINTEROP
    DD_ENTER_MAY_THROW;
    return GetRcwFromVmptrObject(vmObject) != NULL;
#else
    return FALSE;
#endif // FEATURE_COMINTEROP

}

void DacDbiInterfaceImpl::GetRcwCachedInterfaceTypes(
                        VMPTR_Object vmObject,
                        VMPTR_AppDomain vmAppDomain,
                        BOOL bIInspectableOnly,
                        DacDbiArrayList<DebuggerIPCE_ExpandedTypeData> * pDacInterfaces)
{
    // Legacy WinRT API.
    pDacInterfaces->Alloc(0);
}

void DacDbiInterfaceImpl::GetRcwCachedInterfacePointers(
                    VMPTR_Object vmObject,
                    BOOL bIInspectableOnly,
                    DacDbiArrayList<CORDB_ADDRESS> * pDacItfPtrs)
{
#ifdef FEATURE_COMINTEROP

    DD_ENTER_MAY_THROW;

    Object* objPtr = vmObject.GetDacPtr();

    InlineSArray<TADDR, INTERFACE_ENTRY_CACHE_SIZE> rgUnks;

    PTR_RCW pRCW = GetRcwFromVmptrObject(vmObject);
    if (pRCW != NULL)
    {
        pRCW->GetCachedInterfacePointers(bIInspectableOnly, &rgUnks);

        pDacItfPtrs->Alloc(rgUnks.GetCount());

        for (COUNT_T i = 0; i < rgUnks.GetCount(); ++i)
        {
            (*pDacItfPtrs)[i] = (CORDB_ADDRESS)(rgUnks[i]);
        }

    }
    else
#endif // FEATURE_COMINTEROP
    {
        pDacItfPtrs->Alloc(0);
    }
}

void DacDbiInterfaceImpl::GetCachedWinRTTypesForIIDs(
                    VMPTR_AppDomain vmAppDomain,
					DacDbiArrayList<GUID> & iids,
    				OUT DacDbiArrayList<DebuggerIPCE_ExpandedTypeData> * pTypes)
{
    pTypes->Alloc(0);
}

void DacDbiInterfaceImpl::GetCachedWinRTTypes(
                    VMPTR_AppDomain vmAppDomain,
                    OUT DacDbiArrayList<GUID> * pGuids,
                    OUT DacDbiArrayList<DebuggerIPCE_ExpandedTypeData> * pTypes)
{
    pTypes->Alloc(0);
}

//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::FindField
// Finds information for a particular class field
// Arguments:
//     input: thApprox - type handle for the type to which the field belongs
//            fldToken - metadata token for the field
// Return Value: FieldDesc containing information for the field if found or NULL otherwise
//-----------------------------------------------------------------------------
PTR_FieldDesc  DacDbiInterfaceImpl::FindField(TypeHandle thApprox, mdFieldDef fldToken)
{
    EncApproxFieldDescIterator fdIterator(thApprox.GetMethodTable(),
                                          ApproxFieldDescIterator::ALL_FIELDS); // don't fixup EnC (we can't, we're stopped)

    PTR_FieldDesc pCurrentFD;

    while ((pCurrentFD = fdIterator.Next()) != NULL)
    {
        // We're looking for a specific fieldDesc, see if we got it.
        if (pCurrentFD->GetMemberDef() == fldToken)
        {
            return pCurrentFD;
        }
    }

    // we never found it...
    return NULL;
} // DacDbiInterfaceImpl::FindField

//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::GetEnCFieldDesc
// Get the FieldDesc corresponding to a particular EnC field token
// Arguments:
//     input:  pEnCFieldInfo
// Return Value: pointer to the FieldDesc that corresponds to the EnC field
// Note: this function may throw
//-----------------------------------------------------------------------------
FieldDesc * DacDbiInterfaceImpl::GetEnCFieldDesc(const EnCHangingFieldInfo * pEnCFieldInfo)
{
        FieldDesc * pFD = NULL;

        DomainAssembly * pDomainAssembly = pEnCFieldInfo->GetObjectTypeData().vmDomainAssembly.GetDacPtr();
        Module     * pModule     = pDomainAssembly->GetModule();

        // get the type handle for the object
        TypeHandle typeHandle = ClassLoader::LookupTypeDefOrRefInModule(pModule,
                                             pEnCFieldInfo->GetObjectTypeData().metadataToken);
        if (typeHandle == NULL)
        {
            ThrowHR(CORDBG_E_CLASS_NOT_LOADED);
        }
        // and find the field desc
        pFD = FindField(typeHandle, pEnCFieldInfo->GetFieldToken());
        if (pFD == NULL)
        {
            // FieldDesc is not yet available, so can't get EnC field info
            ThrowHR(CORDBG_E_ENC_HANGING_FIELD);
        }
        return pFD;

} // DacDbiInterfaceImpl::GetEnCFieldDesc

//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::GetPtrToEnCField
// Get the address of a field added with EnC.
// Arguments:
//     input: pFD           - field desc for the added field
//            pEnCFieldInfo - information about the new field
// Return Value: The field address if the field is available (i.e., it has been accessed)
//               or NULL otherwise
// Note: this function may throw
//-----------------------------------------------------------------------------
PTR_CBYTE DacDbiInterfaceImpl::GetPtrToEnCField(FieldDesc * pFD, const EnCHangingFieldInfo * pEnCFieldInfo)
{
#ifndef FEATURE_METADATA_UPDATER
    _ASSERTE(!"Trying to get the address of an EnC field where EnC is not supported! ");
    return NULL;
#else

    PTR_EditAndContinueModule pEnCModule;
    DomainAssembly * pDomainAssembly = pEnCFieldInfo->GetObjectTypeData().vmDomainAssembly.GetDacPtr();
    Module     * pModule     = pDomainAssembly->GetModule();

    // make sure we actually have an EditAndContinueModule
    _ASSERTE(pModule->IsEditAndContinueCapable());
    pEnCModule = dac_cast<PTR_EditAndContinueModule>(pModule);

    // we should also have an EnCFieldDesc
    _ASSERTE(pFD->IsEnCNew());
    EnCFieldDesc * pEnCFieldDesc;
    pEnCFieldDesc = dac_cast<PTR_EnCFieldDesc>(pFD);

    // If it hasn't been fixed up yet, then we can't return the pointer.
    if (pEnCFieldDesc->NeedsFixup())
    {
        ThrowHR(CORDBG_E_ENC_HANGING_FIELD);
    }
    // Get a pointer to the field
    PTR_CBYTE pORField = NULL;

    PTR_Object pObject = pEnCFieldInfo->GetVmObject().GetDacPtr();
    pORField = pEnCModule->ResolveField(ObjectToOBJECTREF(pObject),
                                        pEnCFieldDesc);

    // The field could be absent because the code hasn't accessed it yet. If so, we're not going to add it
    // since we can't allocate anyway.
    if (pORField == NULL)
    {
        ThrowHR(CORDBG_E_ENC_HANGING_FIELD);
    }
    return pORField;
#endif // FEATURE_METADATA_UPDATER
} // DacDbiInterfaceImpl::GetPtrToEnCField

//-----------------------------------------------------------------------------
// DacDbiInterfaceImpl::InitFieldData
// Initialize information about a field added with EnC
// Arguments :
//     input:
//             pFD           - provides information about whether the field is static,
//                             the metadata token, etc.
//             pORField      - provides the field address or offset
//             pEnCFieldData - provides the offset to the fields of the object
//     output: pFieldData    - initialized in accordance with the input information
//-----------------------------------------------------------------------------
void DacDbiInterfaceImpl::InitFieldData(const FieldDesc *           pFD,
                                        const PTR_CBYTE             pORField,
                                        const EnCHangingFieldInfo * pEnCFieldData,
                                        FieldData *           pFieldData)
{

    pFieldData->ClearFields();

    pFieldData->m_fFldIsStatic = (pFD->IsStatic() != 0);
    pFieldData->m_vmFieldDesc.SetHostPtr(pFD);
    pFieldData->m_fFldIsTLS = (pFD->IsThreadStatic() == TRUE);
    pFieldData->m_fldMetadataToken = pFD->GetMemberDef();
    pFieldData->m_fFldIsRVA = (pFD->IsRVA() == TRUE);
    pFieldData->m_fFldIsCollectibleStatic = FALSE;
    pFieldData->m_fFldStorageAvailable = true;

    if (pFieldData->m_fFldIsStatic)
    {
        //EnC is only supported on regular static fields
        _ASSERTE(!pFieldData->m_fFldIsTLS);
        _ASSERTE(!pFieldData->m_fFldIsRVA);

        // pORField contains the absolute address
        pFieldData->SetStaticAddress(PTR_TO_TADDR(pORField));
    }
    else
    {
        // fldInstanceOffset is computed to work correctly with GetFieldValue
        // which computes:
        // addr of pORField = object + pEnCFieldInfo->m_offsetToVars + offsetToFld
        pFieldData->SetInstanceOffset(PTR_TO_TADDR(pORField) -
                                      (PTR_TO_TADDR(pEnCFieldData->GetVmObject().GetDacPtr()) +
                                                   pEnCFieldData->GetOffsetToVars()));
    }
} // DacDbiInterfaceImpl::InitFieldData


// ----------------------------------------------------------------------------
// DacDbi API: GetEnCHangingFieldInfo
// After a class has been loaded, if a field has been added via EnC we'll have to jump through
// some hoops to get at it (it hangs off the sync block or FieldDesc).
//
// GENERICS: TODO: this method will need to be modified if we ever support EnC on
// generic classes.
//-----------------------------------------------------------------------------
void DacDbiInterfaceImpl::GetEnCHangingFieldInfo(const EnCHangingFieldInfo * pEnCFieldInfo,
                                                 FieldData *           pFieldData,
                                                 BOOL *                pfStatic)
{
    DD_ENTER_MAY_THROW;

    LOG((LF_CORDB, LL_INFO100000, "DDI::IEnCHFI: Obj:0x%x, objType"
        ":0x%x, offset:0x%x\n", pEnCFieldInfo->m_pObject, pEnCFieldInfo->m_objectTypeData.elementType,
        pEnCFieldInfo->m_offsetToVars));

    FieldDesc *  pFD      = NULL;
    PTR_CBYTE    pORField = NULL;

    pFD = GetEnCFieldDesc(pEnCFieldInfo);
    _ASSERTE(pFD->IsEnCNew()); // We shouldn't be here if it wasn't added to an
                               // already loaded class.

#ifdef FEATURE_METADATA_UPDATER
    pORField = GetPtrToEnCField(pFD, pEnCFieldInfo);
#else
    _ASSERTE(!"We shouldn't be here: EnC not supported");
#endif // FEATURE_METADATA_UPDATER

    InitFieldData(pFD, pORField, pEnCFieldInfo, pFieldData);
    *pfStatic = (pFD->IsStatic() != 0);

} // DacDbiInterfaceImpl::GetEnCHangingFieldInfo

//- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -


void DacDbiInterfaceImpl::GetAssemblyFromDomainAssembly(VMPTR_DomainAssembly vmDomainAssembly, VMPTR_Assembly *vmAssembly)
{
    DD_ENTER_MAY_THROW;

    _ASSERTE(vmAssembly != NULL);

    DomainAssembly * pDomainAssembly = vmDomainAssembly.GetDacPtr();
    vmAssembly->SetHostPtr(pDomainAssembly->GetAssembly());
}

// Determines whether the runtime security system has assigned full-trust to this assembly.
BOOL DacDbiInterfaceImpl::IsAssemblyFullyTrusted(VMPTR_DomainAssembly vmDomainAssembly)
{
    DD_ENTER_MAY_THROW;

    return TRUE;
}

// Get the full path and file name to the assembly's manifest module.
BOOL DacDbiInterfaceImpl::GetAssemblyPath(
    VMPTR_Assembly  vmAssembly,
    IStringHolder * pStrFilename)
{
    DD_ENTER_MAY_THROW;

    // Get the manifest module for this assembly
    Assembly * pAssembly = vmAssembly.GetDacPtr();
    Module * pManifestModule = pAssembly->GetModule();

    // Get the path for the manifest module.
    // since we no longer support Win9x, we assume all paths will be in unicode format already
    const WCHAR * szPath = pManifestModule->GetPath().DacGetRawUnicode();
    HRESULT hrStatus = pStrFilename->AssignCopy(szPath);
    IfFailThrow(hrStatus);

    if(szPath == NULL || *szPath=='\0')
    {
        // The asembly has no (and will never have a) file name, but we didn't really fail
        return FALSE;
    }

    return TRUE;
}

// DAC/DBI API
// Get a resolved type def from a type ref. The type ref may come from a module other than the
// referencing module.
void DacDbiInterfaceImpl::ResolveTypeReference(const TypeRefData * pTypeRefInfo,
                                               TypeRefData *       pTargetRefInfo)
{
    DD_ENTER_MAY_THROW;
    DomainAssembly * pDomainAssembly        = pTypeRefInfo->vmDomainAssembly.GetDacPtr();
    Module *     pReferencingModule = pDomainAssembly->GetModule();
    BOOL         fSuccess = FALSE;

    // Resolve the type ref
    // g_pEEInterface->FindLoadedClass is almost what we want, but it isn't guaranteed to work if
    // the typeRef was originally loaded from a different assembly.  Also, we need to ensure that
    // we can resolve even unloaded types in fully loaded assemblies, so APIs such as
    // LoadTypeDefOrRefThrowing aren't acceptable.

    Module * pTargetModule = NULL;
    mdTypeDef targetTypeDef = mdTokenNil;

    // The loader won't need to trigger a GC or throw because we've told it not to load anything
    ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

    fSuccess = ClassLoader::ResolveTokenToTypeDefThrowing(pReferencingModule,
                                                          pTypeRefInfo->typeToken,
                                                          &pTargetModule,
                                                          &targetTypeDef,
                                                          Loader::SafeLookup   //don't load, no locks/allocations
                                                          );
    if (fSuccess)
    {
        _ASSERTE(pTargetModule != NULL);
        _ASSERTE( TypeFromToken(targetTypeDef) == mdtTypeDef );

        AppDomain * pAppDomain = pDomainAssembly->GetAppDomain();

        pTargetRefInfo->vmDomainAssembly.SetDacTargetPtr(PTR_HOST_TO_TADDR(pTargetModule->GetDomainAssembly()));
        pTargetRefInfo->typeToken = targetTypeDef;
    }
    else
    {
        // failed - presumably because the target assembly isn't loaded
        ThrowHR(CORDBG_E_CLASS_NOT_LOADED);
    }
} // DacDbiInterfaceImpl::ResolveTypeReference


// Get the full path and file name to the module (if any).
BOOL DacDbiInterfaceImpl::GetModulePath(VMPTR_Module vmModule,
                                        IStringHolder *  pStrFilename)
{
    DD_ENTER_MAY_THROW;

    Module * pModule = vmModule.GetDacPtr();
    PEAssembly * pPEAssembly = pModule->GetPEAssembly();
    if (pPEAssembly != NULL)
    {
        if( !pPEAssembly->GetPath().IsEmpty() )
        {
            // Module has an on-disk path
            const WCHAR * szPath = pPEAssembly->GetPath().DacGetRawUnicode();
            if (szPath == NULL)
            {
                szPath = pPEAssembly->GetModuleFileNameHint().DacGetRawUnicode();
                if (szPath == NULL)
                {
                    goto NoFileName;
                }
            }
            IfFailThrow(pStrFilename->AssignCopy(szPath));
            return TRUE;
        }
    }

NoFileName:
    // no filename
    IfFailThrow(pStrFilename->AssignCopy(W("")));
    return FALSE;
}

// Get the full path and file name to the ngen image for the module (if any).
BOOL DacDbiInterfaceImpl::GetModuleNGenPath(VMPTR_Module vmModule,
                                            IStringHolder *  pStrFilename)
{
    DD_ENTER_MAY_THROW;

    // no ngen filename
    IfFailThrow(pStrFilename->AssignCopy(W("")));
    return FALSE;
}

// Implementation of IDacDbiInterface::GetModuleSimpleName
void DacDbiInterfaceImpl::GetModuleSimpleName(VMPTR_Module vmModule, IStringHolder * pStrFilename)
{
    DD_ENTER_MAY_THROW;

    _ASSERTE(pStrFilename != NULL);

    Module * pModule = vmModule.GetDacPtr();
    LPCUTF8 szNameUtf8 = pModule->GetSimpleName();

    SString convert(SString::Utf8, szNameUtf8);
    IfFailThrow(pStrFilename->AssignCopy(convert.GetUnicode()));
}

HRESULT DacDbiInterfaceImpl::IsModuleMapped(VMPTR_Module pModule, OUT BOOL *isModuleMapped)
{
    LOG((LF_CORDB, LL_INFO10000, "DDBII::IMM - TADDR 0x%x\n", pModule));
    DD_ENTER_MAY_THROW;

    HRESULT hr = S_FALSE;
    PTR_Module pTargetModule = pModule.GetDacPtr();

    EX_TRY
    {
        PTR_PEAssembly pPEAssembly = pTargetModule->GetPEAssembly();
        _ASSERTE(pPEAssembly != NULL);

        if (pPEAssembly->HasLoadedPEImage())
        {
            *isModuleMapped = pPEAssembly->GetLoadedLayout()->IsMapped();
            hr = S_OK;
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

bool DacDbiInterfaceImpl::MetadataUpdatesApplied()
{
    DD_ENTER_MAY_THROW;
#ifdef FEATURE_METADATA_UPDATER
    return g_metadataUpdatesApplied;
#else
    return false;
#endif
}

// Helper to initialize a TargetBuffer from a MemoryRange
//
// Arguments:
//    memoryRange - memory range.
//    pTargetBuffer - required out parameter  to be initialized to value of memory range.
//
// Notes:
//    MemoryRange and TargetBuffer both conceptually describe a single contiguous buffer of memory in the
//    target. MemoryRange is a VM structure, which can't bleed across the DacDbi boundary. TargetBuffer is
//    a DacDbi structure, which can cross the DacDbi boundary.
void InitTargetBufferFromMemoryRange(const MemoryRange memoryRange, TargetBuffer * pTargetBuffer)
{
    SUPPORTS_DAC;

    _ASSERTE(pTargetBuffer != NULL);
    PTR_CVOID p = memoryRange.StartAddress();
    CORDB_ADDRESS addr = PTR_TO_CORDB_ADDRESS(PTR_TO_TADDR(p));

    _ASSERTE(memoryRange.Size() <= 0xffffffff);
    pTargetBuffer->Init(addr, (ULONG)memoryRange.Size());
}

// Helper to initialize a TargetBuffer (host representation of target) from an SBuffer  (target)
//
// Arguments:
//   pBuffer - target pointer to a SBuffer structure. If pBuffer is NULL, then target buffer will be empty.
//   pTargetBuffer - required out pointer to hold buffer description.
//
// Notes:
//   PTR_SBuffer and TargetBuffer are both semantically equivalent structures. They both are a pointer and length
//   describing a buffer in the target address space. (SBufer also has ownership semantics, but for DAC's
//   read-only nature, that doesn't matter).
//   Neither of these will actually copy the target buffer into the host without explicit action.
//   The important difference is that TargetBuffer is a host datastructure and so easier to manipulate.
//
void InitTargetBufferFromTargetSBuffer(PTR_SBuffer pBuffer, TargetBuffer * pTargetBuffer)
{
    SUPPORTS_DAC;

    _ASSERTE(pTargetBuffer != NULL);

    SBuffer * pBufferHost = pBuffer;
    if (pBufferHost == NULL)
    {
        pTargetBuffer->Clear();
        return;
    }

    MemoryRange m = pBufferHost->DacGetRawBuffer();
    InitTargetBufferFromMemoryRange(m, pTargetBuffer);
}


// Implementation of IDacDbiInterface::GetMetadata
void DacDbiInterfaceImpl::GetMetadata(VMPTR_Module vmModule, TargetBuffer * pTargetBuffer)
{
    DD_ENTER_MAY_THROW;

    pTargetBuffer->Clear();

    Module     * pModule = vmModule.GetDacPtr();

    // Target should only be asking about modules that are visible to debugger.
    _ASSERTE(pModule->IsVisibleToDebugger());

    // For dynamic modules, metadata is stored as an eagerly-serialized buffer hanging off the Reflection Module.
    if (pModule->IsReflection())
    {
        // Here is the fetch.
        ReflectionModule * pReflectionModule = pModule->GetReflectionModule();
        InitTargetBufferFromTargetSBuffer(pReflectionModule->GetDynamicMetadataBuffer(), pTargetBuffer);
    }
    else
    {
        PEAssembly * pPEAssembly = pModule->GetPEAssembly();

        // For non-dynamic modules, metadata is in the pe-image.
        COUNT_T size;
        CORDB_ADDRESS address = PTR_TO_CORDB_ADDRESS(dac_cast<TADDR>(pPEAssembly->GetLoadedMetadata(&size)));

        pTargetBuffer->Init(address, (ULONG) size);
    }

    if (pTargetBuffer->IsEmpty())
    {
        // We never expect this to happen in a well-behaved scenario. But just in case.
        ThrowHR(CORDBG_E_MISSING_METADATA);
    }

}

// Implementation of IDacDbiInterface::GetSymbolsBuffer
void DacDbiInterfaceImpl::GetSymbolsBuffer(VMPTR_Module vmModule, TargetBuffer * pTargetBuffer, SymbolFormat * pSymbolFormat)
{
    DD_ENTER_MAY_THROW;

    pTargetBuffer->Clear();
    *pSymbolFormat = kSymbolFormatNone;

    Module * pModule = vmModule.GetDacPtr();

    // Target should only be asking about modules that are visible to debugger.
    _ASSERTE(pModule->IsVisibleToDebugger());

    PTR_CGrowableStream pStream = pModule->GetInMemorySymbolStream();
    if (pStream == NULL)
    {
        // Common case is to not have PDBs in-memory.
        return;
    }

    const MemoryRange m = pStream->GetRawBuffer();
    if (m.Size() == 0)
    {
        // We may be prepared to store symbols (in some particular format) but none are there yet.
        // We treat this the same as not having any symbols above.
        return;
    }
    InitTargetBufferFromMemoryRange(m, pTargetBuffer);

    *pSymbolFormat = kSymbolFormatPDB;
}



void DacDbiInterfaceImpl::GetModuleForDomainAssembly(VMPTR_DomainAssembly vmDomainAssembly, OUT VMPTR_Module * pModule)
{
    DD_ENTER_MAY_THROW;

    _ASSERTE(pModule != NULL);

    DomainAssembly * pDomainAssembly = vmDomainAssembly.GetDacPtr();
    pModule->SetHostPtr(pDomainAssembly->GetModule());
}


// Implement IDacDbiInterface::GetDomainAssemblyData
void DacDbiInterfaceImpl::GetDomainAssemblyData(VMPTR_DomainAssembly vmDomainAssembly, DomainAssemblyInfo * pData)
{
    DD_ENTER_MAY_THROW;

    _ASSERTE(pData != NULL);

    ZeroMemory(pData, sizeof(*pData));

    DomainAssembly * pDomainAssembly  = vmDomainAssembly.GetDacPtr();
    AppDomain  * pAppDomain   = pDomainAssembly->GetAppDomain();

    // @dbgtodo - is this efficient DAC usage (perhaps a dac-cop rule)? Are we round-tripping the pointer?
    pData->vmDomainAssembly.SetHostPtr(pDomainAssembly);
    pData->vmAppDomain.SetHostPtr(pAppDomain);
}

// Implement IDacDbiInterface::GetModuleData
void DacDbiInterfaceImpl::GetModuleData(VMPTR_Module vmModule, ModuleInfo * pData)
{
    DD_ENTER_MAY_THROW;

    _ASSERTE(pData != NULL);

    ZeroMemory(pData, sizeof(*pData));

    Module     * pModule      = vmModule.GetDacPtr();
    PEAssembly * pPEAssembly        = pModule->GetPEAssembly();

    pData->vmPEAssembly.SetHostPtr(pPEAssembly);
    pData->vmAssembly.SetHostPtr(pModule->GetAssembly());

    // Is it dynamic?
    BOOL fIsDynamic = pModule->IsReflection();
    pData->fIsDynamic = fIsDynamic;

    // Get PE BaseAddress and Size
    // For dynamic modules, these are 0. Else,
    pData->pPEBaseAddress = NULL;
    pData->nPESize = 0;

    if (!fIsDynamic)
    {
        COUNT_T size = 0;
        pData->pPEBaseAddress = PTR_TO_TADDR(pPEAssembly->GetDebuggerContents(&size));
        pData->nPESize = (ULONG) size;
    }

    // In-memory is determined by whether the module has a filename.
    pData->fInMemory = FALSE;
    if (pPEAssembly != NULL)
    {
        pData->fInMemory = pPEAssembly->GetPath().IsEmpty();
    }
}


// Enumerate all AppDomains in the process.
void DacDbiInterfaceImpl::EnumerateAppDomains(
    FP_APPDOMAIN_ENUMERATION_CALLBACK fpCallback,
    void * pUserData)
{
    DD_ENTER_MAY_THROW;

    _ASSERTE(fpCallback != NULL);

    // It's critical that we don't yield appdomains after the unload event has been sent.
    // See code:IDacDbiInterface#Enumeration for details.
    AppDomain * pAppDomain = AppDomain::GetCurrentDomain();

    VMPTR_AppDomain vmAppDomain = VMPTR_AppDomain::NullPtr();
    vmAppDomain.SetHostPtr(pAppDomain);
    fpCallback(vmAppDomain, pUserData);
}

// Enumerate all Assemblies in an appdomain.
void  DacDbiInterfaceImpl::EnumerateAssembliesInAppDomain(
    VMPTR_AppDomain vmAppDomain,
    FP_ASSEMBLY_ENUMERATION_CALLBACK fpCallback,
    void * pUserData
)
{
    DD_ENTER_MAY_THROW;

    _ASSERTE(fpCallback != NULL);

    // Iterate through all Assemblies (including shared) in the appdomain.
    AppDomain::AssemblyIterator iterator;

    // If the containing appdomain is unloading, then don't enumerate any assemblies
    // in the domain. This is to enforce rules at code:IDacDbiInterface#Enumeration.
    // See comment in code:DacDbiInterfaceImpl::EnumerateModulesInAssembly code for details.
    AppDomain * pAppDomain = vmAppDomain.GetDacPtr();

    if (pAppDomain == nullptr)
    {
        return;
    }

    // Pass the magical flags to the loader enumerator to get all Execution-only assemblies.
    iterator = pAppDomain->IterateAssembliesEx((AssemblyIterationFlags)(kIncludeLoading | kIncludeLoaded | kIncludeExecution));
    CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;

    while (iterator.Next(pDomainAssembly.This()))
    {
        if (!pDomainAssembly->IsVisibleToDebugger())
        {
            continue;
        }

        VMPTR_DomainAssembly vmDomainAssembly = VMPTR_DomainAssembly::NullPtr();
        vmDomainAssembly.SetHostPtr(pDomainAssembly);

        fpCallback(vmDomainAssembly, pUserData);
    }
}

// Implementation of IDacDbiInterface::EnumerateModulesInAssembly,
// Enumerate all the modules (non-resource) in an assembly.
void DacDbiInterfaceImpl::EnumerateModulesInAssembly(
    VMPTR_DomainAssembly vmAssembly,
    FP_MODULE_ENUMERATION_CALLBACK fpCallback,
    void * pUserData)
{
    DD_ENTER_MAY_THROW;

    _ASSERTE(fpCallback != NULL);

    DomainAssembly * pDomainAssembly = vmAssembly.GetDacPtr();

    // Debugger isn't notified of Resource / Inspection-only modules.
    if (pDomainAssembly->GetModule()->IsVisibleToDebugger())
    {
        // If domain assembly isn't yet loaded, just return
        if (!pDomainAssembly->IsLoaded())
            return;

        VMPTR_DomainAssembly vmDomainAssembly = VMPTR_DomainAssembly::NullPtr();
        vmDomainAssembly.SetHostPtr(pDomainAssembly);

        fpCallback(vmDomainAssembly, pUserData);
    }
}

// Implementation of IDacDbiInterface::ResolveAssembly
// Returns NULL if not found.
VMPTR_DomainAssembly DacDbiInterfaceImpl::ResolveAssembly(
    VMPTR_DomainAssembly vmScope,
    mdToken tkAssemblyRef)
{
    DD_ENTER_MAY_THROW;


    DomainAssembly * pDomainAssembly  = vmScope.GetDacPtr();
    AppDomain  * pAppDomain   = pDomainAssembly->GetAppDomain();
    Module     * pModule      = pDomainAssembly->GetModule();

    VMPTR_DomainAssembly vmDomainAssembly = VMPTR_DomainAssembly::NullPtr();

    Assembly * pAssembly = pModule->LookupAssemblyRef(tkAssemblyRef);
    if (pAssembly != NULL)
    {
        DomainAssembly * pDomainAssembly = pAssembly->GetDomainAssembly();
        vmDomainAssembly.SetHostPtr(pDomainAssembly);
    }
    return vmDomainAssembly;
}

// When stopped at an event, request a synchronization.
// See DacDbiInterface.h for full comments
void DacDbiInterfaceImpl::RequestSyncAtEvent()
{
    DD_ENTER_MAY_THROW;

    // To request a sync, we just need to set g_pDebugger->m_RSRequestedSync high.
    if (g_pDebugger != NULL)
    {
        TADDR addr = PTR_HOST_MEMBER_TADDR(Debugger, g_pDebugger, m_RSRequestedSync);

        BOOL fTrue = TRUE;
        SafeWriteStructOrThrow<BOOL>(addr, &fTrue);

    }
}

HRESULT DacDbiInterfaceImpl::SetSendExceptionsOutsideOfJMC(BOOL sendExceptionsOutsideOfJMC)
{
    DD_ENTER_MAY_THROW

    HRESULT hr = S_OK;
    EX_TRY
    {
        if (g_pDebugger != NULL)
        {
            TADDR addr = PTR_HOST_MEMBER_TADDR(Debugger, g_pDebugger, m_sendExceptionsOutsideOfJMC);
            SafeWriteStructOrThrow<BOOL>(addr, &sendExceptionsOutsideOfJMC);
        }
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

// Notify the debuggee that a debugger attach is pending.
// See DacDbiInterface.h for full comments
void DacDbiInterfaceImpl::MarkDebuggerAttachPending()
{
    DD_ENTER_MAY_THROW;

    if (g_pDebugger != NULL)
    {
        DWORD flags = g_CORDebuggerControlFlags;
        flags |= DBCF_PENDING_ATTACH;

        // Uses special DAC writing. PTR_TO_TADDR doesn't fetch for globals.
        // @dbgtodo  dac support - the exact mechanism of writing to the target needs to be flushed out,
        // especially as it relates to DAC cop and enforcing undac-ized writes.
        g_CORDebuggerControlFlags = flags;
    }
    else
    {
        // Caller should have guaranteed that the LS is loaded.
        // If we're detaching, then don't throw because we don't care.
        ThrowHR(CORDBG_E_NOTREADY);
    }
}


// Notify the debuggee that a debugger is attached.
// See DacDbiInterface.h for full comments
void DacDbiInterfaceImpl::MarkDebuggerAttached(BOOL fAttached)
{
    DD_ENTER_MAY_THROW;

    if (g_pDebugger != NULL)
    {
        // To be attached, we need to set the following
        //   g_CORDebuggerControlFlags |= DBCF_ATTACHED;
        // To detach (if !fAttached), we need to do the opposite.

        DWORD flags = g_CORDebuggerControlFlags;
        if (fAttached)
        {
            flags |= DBCF_ATTACHED;
        }
        else
        {
            flags &= ~ (DBCF_ATTACHED | DBCF_PENDING_ATTACH);
        }

        // Uses special DAC writing. PTR_TO_TADDR doesn't fetch for globals.
        // @dbgtodo  dac support - the exact mechanism of writing to the target needs to be flushed out,
        // especially as it relates to DAC cop and enforcing undac-ized writes.
        g_CORDebuggerControlFlags = flags;
    }
    else if (fAttached)
    {
        // Caller should have guaranteed that the LS is loaded.
        // If we're detaching, then don't throw because we don't care.
        ThrowHR(CORDBG_E_NOTREADY);
    }

}



// Enumerate all threads in the process.
void DacDbiInterfaceImpl::EnumerateThreads(FP_THREAD_ENUMERATION_CALLBACK fpCallback, void * pUserData)
{
    DD_ENTER_MAY_THROW;

    if (ThreadStore::s_pThreadStore == NULL)
    {
        return;
    }

    Thread *pThread = ThreadStore::GetThreadList(NULL);

    while (pThread != NULL)
    {

        // Don't want to publish threads via enumeration before they're ready to be inspected.
        // Use the same window that we used in whidbey.
        Thread::ThreadState threadState = pThread->GetSnapshotState();
        if (!((IsThreadMarkedDeadWorker(pThread)) || (threadState & Thread::TS_Unstarted)))
        {
            VMPTR_Thread vmThread = VMPTR_Thread::NullPtr();
            vmThread.SetHostPtr(pThread);
            fpCallback(vmThread, pUserData);
        }

        pThread = ThreadStore::GetThreadList(pThread);
    }
}

// public implementation of IsThreadMarkedDead
bool DacDbiInterfaceImpl::IsThreadMarkedDead(VMPTR_Thread vmThread)
{
    DD_ENTER_MAY_THROW;
    Thread * pThread = vmThread.GetDacPtr();
    return IsThreadMarkedDeadWorker(pThread);
}

// Private worker for IsThreadMarkedDead
//
// Arguments:
//    pThread - valid thread to check if dead
//
// Returns:
//    true iff thread is marked as dead.
//
// Notes:
//    This is an internal method that skips public validation.
//    See code:IDacDbiInterface::#IsThreadMarkedDead for purpose.
bool DacDbiInterfaceImpl::IsThreadMarkedDeadWorker(Thread * pThread)
{
    _ASSERTE(pThread != NULL);

    Thread::ThreadState threadState = pThread->GetSnapshotState();

    bool fIsDead = (threadState & Thread::TS_Dead) != 0;

    return fIsDead;
}


// Return the handle of the specified thread.
HANDLE DacDbiInterfaceImpl::GetThreadHandle(VMPTR_Thread vmThread)
{
    DD_ENTER_MAY_THROW;

    Thread * pThread = vmThread.GetDacPtr();
    return pThread->GetThreadHandle();
}

// Return the object handle for the managed Thread object corresponding to the specified thread.
VMPTR_OBJECTHANDLE DacDbiInterfaceImpl::GetThreadObject(VMPTR_Thread vmThread)
{
    DD_ENTER_MAY_THROW;

    Thread * pThread = vmThread.GetDacPtr();
    Thread::ThreadState threadState = pThread->GetSnapshotState();

    if ( (threadState & Thread::TS_Dead) ||
         (threadState & Thread::TS_Unstarted) ||
         (threadState & Thread::TS_Detached) ||
         g_fProcessDetach )
    {
        ThrowHR(CORDBG_E_BAD_THREAD_STATE);
    }
    else
    {
        VMPTR_OBJECTHANDLE vmObjHandle = VMPTR_OBJECTHANDLE::NullPtr();
        vmObjHandle.SetDacTargetPtr(pThread->GetExposedObjectHandleForDebugger());
        return vmObjHandle;
    }
}

void DacDbiInterfaceImpl::GetThreadAllocInfo(VMPTR_Thread        vmThread,
                                             DacThreadAllocInfo* threadAllocInfo)
{
    DD_ENTER_MAY_THROW;

    Thread * pThread = vmThread.GetDacPtr();
    gc_alloc_context* allocContext = pThread->GetAllocContext();
    threadAllocInfo->m_allocBytesSOH = allocContext->alloc_bytes - (allocContext->alloc_limit - allocContext->alloc_ptr);
    threadAllocInfo->m_allocBytesUOH = allocContext->alloc_bytes_uoh;
}

// Set and reset the TSNC_DebuggerUserSuspend bit on the state of the specified thread
// according to the CorDebugThreadState.
void DacDbiInterfaceImpl::SetDebugState(VMPTR_Thread        vmThread,
                                        CorDebugThreadState debugState)
{
    DD_ENTER_MAY_THROW;

    Thread * pThread = vmThread.GetDacPtr();

    // update the field on the host copy
    if (debugState == THREAD_SUSPEND)
    {
        pThread->SetThreadStateNC(Thread::TSNC_DebuggerUserSuspend);
    }
    else if (debugState == THREAD_RUN)
    {
        pThread->ResetThreadStateNC(Thread::TSNC_DebuggerUserSuspend);
    }
    else
    {
        ThrowHR(E_INVALIDARG);
    }

    // update the field on the target copy
    TADDR taThreadState = PTR_HOST_MEMBER_TADDR(Thread, pThread, m_StateNC);
    SafeWriteStructOrThrow<Thread::ThreadStateNoConcurrency>(taThreadState, &(pThread->m_StateNC));
}

// Gets the debugger unhandled exception threadstate flag
BOOL DacDbiInterfaceImpl::HasUnhandledException(VMPTR_Thread vmThread)
{
    DD_ENTER_MAY_THROW;

    Thread * pThread = vmThread.GetDacPtr();

    // some managed exceptions don't have any underlying
    // native exception processing going on. They just consist
    // of a managed throwable that we have stashed away followed
    // by a debugger notification and some form of failfast.
    // Everything that comes through EEFatalError is in this category
    if(pThread->IsLastThrownObjectUnhandled())
    {
        return TRUE;
    }

    // most managed exceptions are just a throwable bound to a
    // native exception. In that case this handle will be non-null
    OBJECTHANDLE ohException = pThread->GetThrowableAsHandle();
    if (ohException != NULL)
    {
        // during the UEF we set the unhandled bit, if it is set the exception
        // was unhandled
        // however if the exception has intercept info then we consider it handled
        // again
        return pThread->GetExceptionState()->GetFlags()->IsUnhandled() &&
            !(pThread->GetExceptionState()->GetFlags()->DebuggerInterceptInfo());
    }

    return FALSE;
}

// Return the user state of the specified thread.
CorDebugUserState DacDbiInterfaceImpl::GetUserState(VMPTR_Thread vmThread)
{
    DD_ENTER_MAY_THROW;

    UINT result = 0;
    result = GetPartialUserState(vmThread);

    if (!IsThreadAtGCSafePlace(vmThread))
    {
        result |= USER_UNSAFE_POINT;
    }

    return (CorDebugUserState)result;
}


// Return the connection ID of the specified thread.
CONNID DacDbiInterfaceImpl::GetConnectionID(VMPTR_Thread vmThread)
{
    DD_ENTER_MAY_THROW;

    return INVALID_CONNECTION_ID;
}

// Return the task ID of the specified thread.
TASKID DacDbiInterfaceImpl::GetTaskID(VMPTR_Thread vmThread)
{
    DD_ENTER_MAY_THROW;

    return INVALID_TASK_ID;
}

// Return the OS thread ID of the specified thread
DWORD DacDbiInterfaceImpl::TryGetVolatileOSThreadID(VMPTR_Thread vmThread)
{
    DD_ENTER_MAY_THROW;

    Thread * pThread = vmThread.GetDacPtr();
    _ASSERTE(pThread != NULL);

    DWORD dwThreadId = pThread->GetOSThreadIdForDebugger();

    // If the thread ID is a the magical cookie value, then this is really
    // a switched out thread and doesn't have an OS tid. In that case, the
    // DD contract is to return 0 (a much more sane value)
    const DWORD dwSwitchedOutThreadId = SWITCHED_OUT_FIBER_OSID;
    if (dwThreadId == dwSwitchedOutThreadId)
    {
        return 0;
    }
    return dwThreadId;
}

// Return the unique thread ID of the specified thread.
DWORD DacDbiInterfaceImpl::GetUniqueThreadID(VMPTR_Thread vmThread)
{
    DD_ENTER_MAY_THROW;

    Thread * pThread = vmThread.GetDacPtr();
    _ASSERTE(pThread != NULL);

    return pThread->GetOSThreadId();
}

// Return the object handle to the managed Exception object of the current exception
// on the specified thread.  The return value could be NULL if there is no current exception.
VMPTR_OBJECTHANDLE DacDbiInterfaceImpl::GetCurrentException(VMPTR_Thread vmThread)
{
    DD_ENTER_MAY_THROW;

    Thread * pThread = vmThread.GetDacPtr();

    // OBJECTHANDLEs are really just TADDRs.
    OBJECTHANDLE ohException = pThread->GetThrowableAsHandle();        // ohException can be NULL

    if (ohException == NULL)
    {
        if (pThread->IsLastThrownObjectUnhandled())
        {
            ohException = pThread->LastThrownObjectHandle();
        }
    }

    VMPTR_OBJECTHANDLE vmObjHandle;
    vmObjHandle.SetDacTargetPtr(ohException);
    return vmObjHandle;
}

// Return the object handle to the managed object for a given CCW pointer.
VMPTR_OBJECTHANDLE DacDbiInterfaceImpl::GetObjectForCCW(CORDB_ADDRESS ccwPtr)
{
    DD_ENTER_MAY_THROW;

    OBJECTHANDLE ohCCW = NULL;

#ifdef FEATURE_COMWRAPPERS
    if (DACTryGetComWrappersHandleFromCCW(ccwPtr, &ohCCW) != S_OK)
    {
#endif
#ifdef FEATURE_COMINTEROP
    ComCallWrapper *pCCW = DACGetCCWFromAddress(ccwPtr);
    if (pCCW)
    {
        ohCCW = pCCW->GetObjectHandle();
    }
#endif
#ifdef FEATURE_COMWRAPPERS
    }
#endif

    VMPTR_OBJECTHANDLE vmObjHandle;
    vmObjHandle.SetDacTargetPtr(ohCCW);
    return vmObjHandle;
}

// Return the object handle to the managed CustomNotification object of the current notification
// on the specified thread.  The return value could be NULL if there is no current notification.
// Arguments:
//     input: vmThread - the thread on which the notification occurred
// Return value: object handle for the current notification (if any) on the thread. This will return non-null
// if and only if we are currently inside a CustomNotification Callback (or a dump was generated while in this
// callback)
//
VMPTR_OBJECTHANDLE DacDbiInterfaceImpl::GetCurrentCustomDebuggerNotification(VMPTR_Thread vmThread)
{
    DD_ENTER_MAY_THROW;

    Thread * pThread = vmThread.GetDacPtr();

    // OBJECTHANDLEs are really just TADDRs.
    OBJECTHANDLE ohNotification = pThread->GetThreadCurrNotification();        // ohNotification can be NULL

    VMPTR_OBJECTHANDLE vmObjHandle;
    vmObjHandle.SetDacTargetPtr(ohNotification);
    return vmObjHandle;
}

// Return the current appdomain the specified thread is in.
VMPTR_AppDomain DacDbiInterfaceImpl::GetCurrentAppDomain(VMPTR_Thread vmThread)
{
    DD_ENTER_MAY_THROW;

    Thread *    pThread    = vmThread.GetDacPtr();
    AppDomain * pAppDomain = pThread->GetDomain();

    if (pAppDomain == NULL)
    {
        ThrowHR(E_FAIL);
    }

    VMPTR_AppDomain vmAppDomain = VMPTR_AppDomain::NullPtr();
    vmAppDomain.SetDacTargetPtr(PTR_HOST_TO_TADDR(pAppDomain));
    return vmAppDomain;
}


// Returns a bitfield reflecting the managed debugging state at the time of
// the jit attach.
CLR_DEBUGGING_PROCESS_FLAGS DacDbiInterfaceImpl::GetAttachStateFlags()
{
    DD_ENTER_MAY_THROW;

    CLR_DEBUGGING_PROCESS_FLAGS res = (CLR_DEBUGGING_PROCESS_FLAGS)0;
    if (g_pDebugger != NULL)
    {
        res = g_pDebugger->GetAttachStateFlags();
    }
    else
    {
        // When launching the process under a managed debugger we
        // request these flags when CLR is loaded (before g_pDebugger
        // had a chance to be initialized). In these cases simply
        // return 0
    }
    return res;
}

//---------------------------------------------------------------------------------------
// Helper to get the address of the 2nd-chance hijack function Or throw
//
// Returns:
//     Non-null Target Address of hijack function.
TADDR DacDbiInterfaceImpl::GetHijackAddress()
{
    TADDR addr = NULL;
    if (g_pDebugger != NULL)
    {
        // Get the start address of the redirect function for unhandled exceptions.
        addr = dac_cast<TADDR>(g_pDebugger->m_rgHijackFunction[Debugger::kUnhandledException].StartAddress());
    }
    if (addr == NULL)
    {
        ThrowHR(CORDBG_E_NOTREADY);
    }
    return addr;
}

//---------------------------------------------------------------------------------------
// Helper to determine whether a control PC is in any native stub which the runtime knows how to unwind.
//
// Arguments:
//    taControlPC - control PC to be checked
//
// Returns:
//    Returns true if the control PC is in a runtime unwindable stub.
//
// Notes:
//    Currently this function only recognizes the ExceptionHijack() stub,
//    which is used for unhandled exceptions.
//

bool DacDbiInterfaceImpl::IsRuntimeUnwindableStub(PCODE targetControlPC)
{

    TADDR controlPC = PCODEToPINSTR(targetControlPC);
    // we call this function a lot while walking the stack and the values here will never change
    // Getting the g_pDebugger and each entry in the m_rgHijackFunction is potentially ~7 DAC
    // accesses per frame. Caching the data into a single local array is much faster. This optimization
    // recovered a few % of DAC stackwalking time
    if(!m_isCachedHijackFunctionValid)
    {
        Debugger* pDebugger = g_pDebugger;
        if ((pDebugger == NULL) || (pDebugger->m_rgHijackFunction == NULL))
        {
            // The in-process debugging infrastructure hasn't been fully initialized, which means that we could
            // NOT have hijacked anything yet.
            return false;
        }

        // PERF NOTE: if needed this array copy could probably be made more efficient
        // hitting the DAC only once for a single memory block, or even better
        // put the array inline in the Debugger object so that we only do 1 DAC
        // access for this entire thing
        for (int i = 0; i < Debugger::kMaxHijackFunctions; i++)
        {
            InitTargetBufferFromMemoryRange(pDebugger->m_rgHijackFunction[i], &m_pCachedHijackFunction[i] );
        }
        m_isCachedHijackFunctionValid = TRUE;
    }

    // Check whether the control PC is in any of the thread redirection functions.
    for (int i = 0; i < Debugger::kMaxHijackFunctions; i++)
    {
        CORDB_ADDRESS start = m_pCachedHijackFunction[i].pAddress;
        CORDB_ADDRESS end = start + m_pCachedHijackFunction[i].cbSize;
        if ((start <= controlPC) && (controlPC < end))
        {
            return true;
        }
    }
    return false;
}

//---------------------------------------------------------------------------------------
// Align a stack pointer for the given architecture
//
// Arguments:
//    pEsp - in/out: pointer to stack pointer.
//
void DacDbiInterfaceImpl::AlignStackPointer(CORDB_ADDRESS * pEsp)
{
    SUPPORTS_DAC;

    // Nop on x86.
#if defined(HOST_64BIT)
    // on 64-bit, stack pointer must be 16-byte aligned.
    // Stacks grown down, so round down to nearest 0xF bits.
    *pEsp &= ~((CORDB_ADDRESS) 0xF);
#endif
}

//---------------------------------------------------------------------------------------
// Emulate pushing something on a thread's stack.
//
// Arguments:
//     pEsp - in/out: pointer to stack pointer to push object at. On output,
//            updated stack pointer.
//     pData  - object to push on the stack.
//     fAlignStack - whether to align the stack pointer before and after the push.
//                   Callers which specify FALSE must be very careful and know exactly
//                   what they are doing.
//
// Return:
//     address of pushed object. Throws on error.
template <class T>
CORDB_ADDRESS DacDbiInterfaceImpl::PushHelper(CORDB_ADDRESS * pEsp,
                                              const T * pData,
                                              BOOL fAlignStack)
{
    SUPPORTS_DAC;

    if (fAlignStack == TRUE)
    {
        AlignStackPointer(pEsp);
    }
    *pEsp -= sizeof(T);
    if (fAlignStack == TRUE)
    {
        AlignStackPointer(pEsp);
    }
    SafeWriteStructOrThrow(*pEsp, pData);
    return *pEsp;
}

//---------------------------------------------------------------------------------------
// Write an EXCEPTION_RECORD structure to the remote target at the specified address while taking
// into account the number of exception parameters.  On 64-bit OS and on the WOW64, the OS always
// pushes the entire EXCEPTION_RECORD onto the stack.  However, on native x86 OS, the OS only pushes
// enough of the EXCEPTION_RECORD to cover the specified number of exception parameters.  Thus we
// need to be extra careful when we overwrite an EXCEPTION_RECORD on the stack.
//
// Arguments:
//    pRemotePtr   - address of the EXCEPTION_RECORD in the remote target
//    pExcepRecord - EXCEPTION_RECORD to be written
//
// Notes:
//    This function is only used by the code which hijacks a therad when there's an unhandled exception.
//    It only works when we are actually debugging a live process, not a dump.
//

void DacDbiInterfaceImpl::WriteExceptionRecordHelper(CORDB_ADDRESS pRemotePtr,
                                                     const EXCEPTION_RECORD * pExcepRecord)
{
    // Calculate the correct size to push onto the stack.
    ULONG32 cbSize = offsetof(EXCEPTION_RECORD, ExceptionInformation);
    cbSize += pExcepRecord->NumberParameters * sizeof(pExcepRecord->ExceptionInformation[0]);

    // Use the data target to write to the remote target.  Here we are assuming that we are debugging a
    // live process, since this function is only called by the hijacking code for unhandled exceptions.
    HRESULT hr = m_pMutableTarget->WriteVirtual(pRemotePtr,
                                                reinterpret_cast<const BYTE *>(pExcepRecord),
                                                cbSize);

    if (FAILED(hr))
    {
        ThrowHR(hr);
    }
}

// Implement IDacDbiInterface::Hijack
void DacDbiInterfaceImpl::Hijack(
    VMPTR_Thread                 vmThread,
    ULONG32                      dwThreadId,
    const EXCEPTION_RECORD *     pRecord,
    T_CONTEXT *                  pOriginalContext,
    ULONG32                      cbSizeContext,
    EHijackReason::EHijackReason reason,
    void *                       pUserData,
    CORDB_ADDRESS *              pRemoteContextAddr)
{
    DD_ENTER_MAY_THROW;

    //
    // Validate parameters
    //

    // pRecord may be NULL if we're not hijacking at an exception
    // pOriginalContext may be NULL if caller doesn't want a copy of the context.
    // (The hijack function already has the context)
    _ASSERTE((pOriginalContext == NULL) == (cbSizeContext == 0));
    _ASSERTE(EHijackReason::IsValid(reason));
#ifdef TARGET_UNIX
    _ASSERTE(!"Not supported on this platform");
#endif

    //
    // If we hijack a thread which might not be managed we can set vmThread = NULL
    // The only side-effect in this case is that we can't reuse CONTEXT and
    // EXCEPTION_RECORD space on the stack by an already underway in-process exception
    // filter. If you depend on those being used and updated you must provide the vmThread
    //
    Thread* pThread = NULL;
    if(!vmThread.IsNull())
    {
        pThread = vmThread.GetDacPtr();
        _ASSERTE(pThread->GetOSThreadIdForDebugger() == dwThreadId);
    }

    TADDR pfnHijackFunction = GetHijackAddress();

    //
    // Setup context for hijack
    //
    T_CONTEXT ctx;
    HRESULT hr = m_pTarget->GetThreadContext(
        dwThreadId,
        CONTEXT_FULL,
        sizeof(DT_CONTEXT),
        (BYTE*) &ctx);
    IfFailThrow(hr);

    // If caller requested, copy back the original context that we're hijacking from.
    if (pOriginalContext != NULL)
    {
        // Since Dac + DBI are tightly coupled, context sizes should be the same.
        if (cbSizeContext != sizeof(T_CONTEXT))
        {
            ThrowHR(E_INVALIDARG);
        }

        memcpy(pOriginalContext, &ctx, cbSizeContext);
    }

    // Make sure the trace flag isn't on. This can happen if we were single stepping the thread when we faulted. This
    // will ensure that we don't try to single step through the OS's exception logic, which greatly confuses our second
    // chance hijack logic. This also mimics what the OS does for us automaically when single stepping in process, i.e.,
    // when you turn the trace flag on in-process and go, if there is a fault, the fault is reported and the trace flag
    // is automatically turned off.
    //
    // The debugger could always re-enable the single-step flag if it wants to.
#ifndef FEATURE_EMULATE_SINGLESTEP
    UnsetSSFlag(reinterpret_cast<DT_CONTEXT *>(&ctx));
#endif

    // Push pointers
    void* espContext = NULL;
    void* espRecord = NULL;
    const void* pData = pUserData;

    // @dbgtodo  cross-plat - this is not cross plat
    CORDB_ADDRESS esp = GetSP(&ctx);

    //
    // Find out where the OS exception dispatcher has pushed the EXCEPTION_RECORD and CONTEXT. The ExInfo and
    // ExceptionTracker have pointers to these data structures, but when we get the unhandled exception
    // notification, the OS exception dispatcher is no longer on the stack, so these pointers are no longer
    // valid.  We need to either update these pointers in the ExInfo/ExcepionTracker, or reuse the stack
    // space used by the OS exception dispatcher.  We are using the latter approach here.
    //

    CORDB_ADDRESS espOSContext = NULL;
    CORDB_ADDRESS espOSRecord  = NULL;
    if (pThread != NULL && pThread->IsExceptionInProgress())
    {
        espOSContext = (CORDB_ADDRESS)PTR_TO_TADDR(pThread->GetExceptionState()->GetContextRecord());
        espOSRecord  = (CORDB_ADDRESS)PTR_TO_TADDR(pThread->GetExceptionState()->GetExceptionRecord());

        // The managed exception may not be related to the unhandled exception for which we are trying to
        // hijack.  An example would be when a thread hits a managed exception, VS tries to do func eval on
        // the thread, but the func eval causes an unhandled exception (e.g. AV in mscorwks.dll).  In this
        // case, the pointers stored on the ExInfo/ExceptionTracker are closer to the root than the current
        // SP of the thread.  The check below makes sure we don't reuse the pointers in this case.
        if (espOSContext < esp)
        {
            SafeWriteStructOrThrow(espOSContext, &ctx);
            espContext = CORDB_ADDRESS_TO_PTR(espOSContext);

            // We should have an EXCEPTION_RECORD if we are hijacked at an exception.
            // We need to be careful when we overwrite the exception record.  On x86, the OS doesn't
            // always push the full record onto the stack, and so we can't blindly use sizeof(EXCEPTION_RECORD).
            // Instead, we have to look at the number of exception parameters and calculate the size.
            _ASSERTE(pRecord != NULL);
            WriteExceptionRecordHelper(espOSRecord, pRecord);
            espRecord  = CORDB_ADDRESS_TO_PTR(espOSRecord);

            esp = min(espOSContext, espOSRecord);
        }
    }

    // If we haven't reused the pointers, then push everything at the leaf of the stack.
    if (espContext == NULL)
    {
        _ASSERTE(espRecord == NULL);

        // Push on full Context and ExceptionRecord structures. We'll then push pointers to these,
        // and those pointers will serve as the actual args to the function.
        espContext = CORDB_ADDRESS_TO_PTR(PushHelper(&esp, &ctx, TRUE));

        // If caller didn't pass an exception-record, then we're not being hijacked at an exception.
        // We'll just pass NULL for the exception-record to the Hijack function.
        if (pRecord != NULL)
        {
            espRecord  = CORDB_ADDRESS_TO_PTR(PushHelper(&esp, pRecord, TRUE));
        }
    }

    if(pRemoteContextAddr != NULL)
    {
        *pRemoteContextAddr = PTR_TO_CORDB_ADDRESS(espContext);
    }

    //
    // Push args onto the stack to be able to call the hijack function
    //

    // Prototype of hijack is:
    //     void __stdcall ExceptionHijackWorker(CONTEXT * pContext, EXCEPTION_RECORD * pRecord, EHijackReason, void * pData)
    // Set up everything so that the hijack stub can just do a "call" instruction.
    //
    // Regarding stack overflow: We could do an explicit check against the thread's stack base limit.
    // However, we don't need an explicit overflow check because if the stack does overflow,
    // the hijack will just hit a regular stack-overflow exception.
#if defined(TARGET_X86)  // TARGET
    // X86 calling convention is to push args on the stack in reverse order.
    // If we fail here, the stack is written, but esp hasn't been committed yet so it shouldn't matter.
    PushHelper(&esp, &pData, TRUE);
    PushHelper(&esp, &reason, TRUE);
    PushHelper(&esp, &espRecord, TRUE);
    PushHelper(&esp, &espContext, TRUE);
#elif defined (TARGET_AMD64) // TARGET
    // AMD64 calling convention is to place first 4 parameters in: rcx, rdx, r8 and r9
    ctx.Rcx = (DWORD64) espContext;
    ctx.Rdx = (DWORD64) espRecord;
    ctx.R8  = (DWORD64) reason;
    ctx.R9  = (DWORD64) pData;

    // Caller must allocate stack space to spill for args.
    // Push the arguments onto the outgoing argument homes.
    // Make sure we push pointer-sized values to keep the stack aligned.
    PushHelper(&esp, reinterpret_cast<SIZE_T *>(&(ctx.R9)), FALSE);
    PushHelper(&esp, reinterpret_cast<SIZE_T *>(&(ctx.R8)), FALSE);
    PushHelper(&esp, reinterpret_cast<SIZE_T *>(&(ctx.Rdx)), FALSE);
    PushHelper(&esp, reinterpret_cast<SIZE_T *>(&(ctx.Rcx)), FALSE);
#elif defined(TARGET_ARM)
    ctx.R0 = (DWORD)espContext;
    ctx.R1 = (DWORD)espRecord;
    ctx.R2 = (DWORD)reason;
    ctx.R3 = (DWORD)pData;
#elif defined(TARGET_ARM64)
    ctx.X0 = (DWORD64)espContext;
    ctx.X1 = (DWORD64)espRecord;
    ctx.X2 = (DWORD64)reason;
    ctx.X3 = (DWORD64)pData;
#else
    PORTABILITY_ASSERT("CordbThread::HijackForUnhandledException is not implemented on this platform.");
#endif
    SetSP(&ctx, CORDB_ADDRESS_TO_TADDR(esp));

    // @dbgtodo  cross-plat - not cross-platform safe
    SetIP(&ctx, pfnHijackFunction);

    //
    // Commit the context.
    //
    hr = m_pMutableTarget->SetThreadContext(dwThreadId, sizeof(DT_CONTEXT), reinterpret_cast<BYTE*> (&ctx));
    IfFailThrow(hr);
}

// Return the filter CONTEXT on the LS.
VMPTR_CONTEXT DacDbiInterfaceImpl::GetManagedStoppedContext(VMPTR_Thread vmThread)
{
    DD_ENTER_MAY_THROW;

    VMPTR_CONTEXT vmContext = VMPTR_CONTEXT::NullPtr();

    Thread * pThread = vmThread.GetDacPtr();
    if (pThread->GetInteropDebuggingHijacked())
    {
        _ASSERTE(!ISREDIRECTEDTHREAD(pThread));
        vmContext = VMPTR_CONTEXT::NullPtr();
    }
    else
    {
        DT_CONTEXT * pLSContext = reinterpret_cast<DT_CONTEXT *>(pThread->GetFilterContext());
        if (pLSContext != NULL)
        {
            _ASSERTE(!ISREDIRECTEDTHREAD(pThread));
            vmContext.SetHostPtr(pLSContext);
        }
        else if (ISREDIRECTEDTHREAD(pThread))
        {
            pLSContext = reinterpret_cast<DT_CONTEXT *>(GETREDIRECTEDCONTEXT(pThread));
            _ASSERTE(pLSContext != NULL);

            if (pLSContext != NULL)
            {
                vmContext.SetHostPtr(pLSContext);
            }
        }
    }

    return vmContext;
}

// Return a TargetBuffer for the raw vararg signature.
TargetBuffer DacDbiInterfaceImpl::GetVarArgSig(CORDB_ADDRESS   VASigCookieAddr,
                                               CORDB_ADDRESS * pArgBase)
{
    DD_ENTER_MAY_THROW;

    _ASSERTE(pArgBase != NULL);
    *pArgBase = NULL;

    // First, read the VASigCookie pointer.
    TADDR taVASigCookie = NULL;
    SafeReadStructOrThrow(VASigCookieAddr, &taVASigCookie);

    // Now create a DAC copy of VASigCookie.
    VASigCookie * pVACookie = PTR_VASigCookie(taVASigCookie);

    // Figure out where the first argument is.
#if defined(TARGET_X86) // (STACK_GROWS_DOWN_ON_ARGS_WALK)
    *pArgBase = VASigCookieAddr + pVACookie->sizeOfArgs;
#else  // !TARGET_X86 (STACK_GROWS_UP_ON_ARGS_WALK)
    *pArgBase = VASigCookieAddr + sizeof(VASigCookie *);
#endif // !TARGET_X86 (STACK_GROWS_UP_ON_ARGS_WALK)

    return TargetBuffer(PTR_TO_CORDB_ADDRESS(pVACookie->signature.GetRawSig()),
                        pVACookie->signature.GetRawSigLen());
}

// returns TRUE if the type requires 8-byte alignment
BOOL DacDbiInterfaceImpl::RequiresAlign8(VMPTR_TypeHandle thExact)
{
    DD_ENTER_MAY_THROW;

#ifdef FEATURE_64BIT_ALIGNMENT
    TypeHandle th = TypeHandle::FromPtr(thExact.GetDacPtr());
    PTR_MethodTable mt = th.AsMethodTable();

    return mt->RequiresAlign8();
#else
    ThrowHR(E_NOTIMPL);
#endif
}

// Resolve the raw generics token to the real generics type token.  The resolution is based on the
// given index.
GENERICS_TYPE_TOKEN DacDbiInterfaceImpl::ResolveExactGenericArgsToken(DWORD               dwExactGenericArgsTokenIndex,
                                                                      GENERICS_TYPE_TOKEN rawToken)
{
    DD_ENTER_MAY_THROW;

    if (dwExactGenericArgsTokenIndex == 0)
    {
        // In a rare case of VS4Mac debugging VS4Mac ARM64 optimized code we get a null generics argument token. We aren't sure
        // why the token is null, it may be a bug or it may be by design in the runtime. In the interest of time we are working
        // around the issue rather than investigating the root cause. This workaround should only cause us to degrade generic
        // types from exact type parameters to approximate or canonical type parameters. In the future if we discover this issue
        // is happening more frequently than we expect or the workaround is more impactful than we expect we may need to remove
        // this workaround and resolve the underlying issue.
        if (rawToken == 0)
        {
            return rawToken;
        }
        // In this case the real generics type token is the MethodTable of the "this" object.
        // Note that we want the target address here.

        // Incoming rawToken is actually a PTR_Object for the 'this' pointer.
        // Need to do some casting to convert GENERICS_TYPE_TOKEN --> PTR_Object
        TADDR addrObjThis = CORDB_ADDRESS_TO_TADDR(rawToken);
        PTR_Object pObjThis = dac_cast<PTR_Object>(addrObjThis);


        PTR_MethodTable pMT = pObjThis->GetMethodTable();

        // Now package up the PTR_MethodTable back into a GENERICS_TYPE_TOKEN
        TADDR addrMT = dac_cast<TADDR>(pMT);
        GENERICS_TYPE_TOKEN realToken = (GENERICS_TYPE_TOKEN) addrMT;
        return realToken;
    }
    else if (dwExactGenericArgsTokenIndex == (DWORD)ICorDebugInfo::TYPECTXT_ILNUM)
    {
        // rawToken is already initialized correctly.  Nothing to do here.
        return  rawToken;
    }

    // The index of the generics type token should not be anything else.
    // This is indeed an error condition, and so we throw here.
    _ASSERTE(!"DDII::REGAT - Unexpected generics type token index.");
    ThrowHR(CORDBG_E_TARGET_INCONSISTENT);
}

// Check if the given method is an IL stub or an LCD method.
IDacDbiInterface::DynamicMethodType DacDbiInterfaceImpl::IsILStubOrLCGMethod(VMPTR_MethodDesc vmMethodDesc)
{
    DD_ENTER_MAY_THROW;

    MethodDesc * pMD = vmMethodDesc.GetDacPtr();

    if (pMD->IsILStub())
    {
        return kILStub;
    }
    else if (pMD->IsLCGMethod())
    {
        return kLCGMethod;
    }
    else
    {
        return kNone;
    }
}

//---------------------------------------------------------------------------------------
//
// Determine whether the specified thread is at a GC safe place.
//
// Arguments:
//    vmThread - the thread to be examined
//
// Return Value:
//    Return TRUE if the thread is at a GC safe place.
//    and under what conditions
//
// Notes:
//    This function basically does a one-frame stackwalk.
//    The logic is adopted from Debugger::IsThreadAtSafePlace().
//

BOOL DacDbiInterfaceImpl::IsThreadAtGCSafePlace(VMPTR_Thread vmThread)
{
    DD_ENTER_MAY_THROW;

    BOOL fIsGCSafe = FALSE;
    Thread * pThread = vmThread.GetDacPtr();

    // Check if the runtime has entered "Shutdown for Finalizer" mode.
    if ((g_fEEShutDown & ShutDown_Finalize2) != 0)
    {
        fIsGCSafe = TRUE;
    }
    else
    {
        T_CONTEXT ctx;
        REGDISPLAY rd;
        SetUpRegdisplayForStackWalk(pThread, &ctx, &rd);

        ULONG32 flags = (QUICKUNWIND | HANDLESKIPPEDFRAMES | DISABLE_MISSING_FRAME_DETECTION);

        StackFrameIterator iter;
        iter.Init(pThread, pThread->GetFrame(), &rd, flags);

        CrawlFrame * pCF = &(iter.m_crawl);
        if (pCF->IsFrameless() && pCF->IsActiveFunc())
        {
            if (pCF->IsGcSafe())
            {
                fIsGCSafe = TRUE;
            }
        }
    }

    return fIsGCSafe;
}

//---------------------------------------------------------------------------------------
//
// Return a partial user state of the specified thread.  The returned user state doesn't contain
// information about USER_UNSAFE_POINT.  The caller needs to call IsThreadAtGCSafePlace() to get
// the full user state.
//
// Arguments:
//    vmThread - the specified thread
//
// Return Value:
//    Return the partial user state except for USER_UNSAFE_POINT
//

CorDebugUserState DacDbiInterfaceImpl::GetPartialUserState(VMPTR_Thread vmThread)
{
    DD_ENTER_MAY_THROW;

    Thread * pThread = vmThread.GetDacPtr();
    Thread::ThreadState ts = pThread->GetSnapshotState();

    UINT result = 0;
    if (ts & Thread::TS_Background)
    {
        result |= USER_BACKGROUND;
    }

    if (ts & Thread::TS_Unstarted)
    {
        result |= USER_UNSTARTED;
    }

    // Don't report a StopRequested if the thread has actually stopped.
    if (ts & Thread::TS_Dead)
    {
        result |= USER_STOPPED;
    }

    // The interruptible flag is unreliable (see issue 699245)
    // The Debugger_SleepWaitJoin is always accurate when it is present, but it is still
    // just a band-aid fix to cover some of the race conditions interruptible has.

    if (ts & Thread::TS_Interruptible || pThread->HasThreadStateNC(Thread::TSNC_DebuggerSleepWaitJoin))
    {
        result |= USER_WAIT_SLEEP_JOIN;
    }

    if (pThread->IsThreadPoolThread())
    {
        result |= USER_THREADPOOL;
    }

    return (CorDebugUserState)result;
}

//---------------------------------------------------------------------------------------
//
// Look up the EnC version number of a particular jitted instance of a managed method.
//
// Arguments:
//    pModule             - the module containing the managed method
//    vmMethodDesc        - the MethodDesc of the managed method
//    mdMethod            - the MethodDef metadata token of the managed method
//    pNativeStartAddress - the native start address of the jitted code
//    pJittedInstanceEnCVersion - out parameter; the version number of the version
//                                corresponding to the specified native start address
//    pLatestEnCVersion         - out parameter; the version number of the latest version
//
// Assumptions:
//    vmMethodDesc and mdMethod must match (see below).
//
// Notes:
//    mdMethod is not strictly necessary, since we can always get that from vmMethodDesc.
//    It is just a perf optimization since the caller has the metadata token around already.
//
//    Today, there is no way to retrieve the EnC version number from the RS data structures.
//    This primitive uses DAC to retrieve it from the LS data structures.  This function may
//    very well be ripped out in the future if we DACize this information, but the current
//    thinking is that some of the RS data structures will remain, most likely in a reduced form.
//

void DacDbiInterfaceImpl::LookupEnCVersions(Module*          pModule,
                                            VMPTR_MethodDesc vmMethodDesc,
                                            mdMethodDef      mdMethod,
                                            CORDB_ADDRESS    pNativeStartAddress,
                                            SIZE_T *         pLatestEnCVersion,
                                            SIZE_T *         pJittedInstanceEnCVersion /* = NULL */)
{
    MethodDesc * pMD     = vmMethodDesc.GetDacPtr();

    // make sure the vmMethodDesc and mdMethod match
    _ASSERTE(pMD->GetMemberDef() == mdMethod);

    _ASSERTE(pLatestEnCVersion != NULL);

    // @dbgtodo  inspection - once we do EnC, stop using DMIs.
    // If the method wasn't EnCed, DMIs may not exist. And since this is DAC, we can't create them.

    // We may not have the memory for the DebuggerMethodInfos in a minidump.
    // When dump debugging EnC information isn't very useful so just fallback
    // to default version.
    DebuggerMethodInfo * pDMI = NULL;
    DebuggerJitInfo * pDJI = NULL;
    EX_TRY_ALLOW_DATATARGET_MISSING_MEMORY
    {
        pDMI = g_pDebugger->GetOrCreateMethodInfo(pModule, mdMethod);
        if (pDMI != NULL)
        {
            pDJI = pDMI->FindJitInfo(pMD, CORDB_ADDRESS_TO_TADDR(pNativeStartAddress));
        }
    }
    EX_END_CATCH_ALLOW_DATATARGET_MISSING_MEMORY;
    if (pDJI != NULL)
    {
        if (pJittedInstanceEnCVersion != NULL)
        {
            *pJittedInstanceEnCVersion = pDJI->m_encVersion;
        }
        *pLatestEnCVersion = pDMI->GetCurrentEnCVersion();
    }
    else
    {
        // If we have no DMI/DJI, then we must never have EnCed. So we can use default EnC info
        // Several cases where we don't have a DMI/DJI:
        // - LCG methods
        // - method was never "touched" by debugger. (DJIs are created lazily).
        if (pJittedInstanceEnCVersion != NULL)
        {
            *pJittedInstanceEnCVersion = CorDB_DEFAULT_ENC_FUNCTION_VERSION;
        }
        *pLatestEnCVersion = CorDB_DEFAULT_ENC_FUNCTION_VERSION;
    }
}

// Get the address of the Debugger control block on the helper thread
// Arguments: none
// Return Value: The remote address of the Debugger control block allocated on the helper thread
//               if it has been successfully allocated or NULL otherwise.
CORDB_ADDRESS DacDbiInterfaceImpl::GetDebuggerControlBlockAddress()
{
    DD_ENTER_MAY_THROW;

    if ((g_pDebugger != NULL) &&
        (g_pDebugger->m_pRCThread != NULL))
    {
    return CORDB_ADDRESS(dac_cast<TADDR>(g_pDebugger->m_pRCThread->GetDCB()));
    }

    return NULL;
}

// DacDbi API: Get the context for a particular thread of the target process
void DacDbiInterfaceImpl::GetContext(VMPTR_Thread vmThread, DT_CONTEXT * pContextBuffer)
{
    DD_ENTER_MAY_THROW

    _ASSERTE(pContextBuffer != NULL);

    Thread *  pThread  = vmThread.GetDacPtr();

    // @dbgtodo  Once the filter context is removed, then we should always
    // start with the leaf CONTEXT.
    DT_CONTEXT * pFilterContext = reinterpret_cast<DT_CONTEXT *>(pThread->GetFilterContext());

    if (pFilterContext == NULL)
    {
        // If the filter context is NULL, then we use the true context of the thread.
        pContextBuffer->ContextFlags = DT_CONTEXT_ALL;
        HRESULT hr = m_pTarget->GetThreadContext(pThread->GetOSThreadId(),
                                                pContextBuffer->ContextFlags,
                                                sizeof(DT_CONTEXT),
                                                reinterpret_cast<BYTE *>(pContextBuffer));
        if (hr == E_NOTIMPL)
        {
            // GetThreadContext is not implemented on this data target.
            // That's why we have to make do with context we can obtain from Frames explicitly stored in Thread object.
            // It suffices for managed debugging stackwalk.
            REGDISPLAY tmpRd = {};
            T_CONTEXT tmpContext = {};
            FillRegDisplay(&tmpRd, &tmpContext);

            // Going through thread Frames and looking for first (deepest one) one that
            // that has context available for stackwalking (SP and PC)
            // For example: RedirectedThreadFrame, InlinedCallFrame, HelperMethodFrame, ComPlusMethodFrame
            Frame *frame = pThread->GetFrame();
            while (frame != NULL && frame != FRAME_TOP)
            {
                frame->UpdateRegDisplay(&tmpRd);
                if (GetRegdisplaySP(&tmpRd) != 0 && GetControlPC(&tmpRd) != 0)
                {
                    UpdateContextFromRegDisp(&tmpRd, &tmpContext);
                    CopyMemory(pContextBuffer, &tmpContext, sizeof(*pContextBuffer));
                    pContextBuffer->ContextFlags = DT_CONTEXT_CONTROL;
                    return;
                }
                frame = frame->Next();
            }

            // It looks like this thread is not running managed code.
            ZeroMemory(pContextBuffer, sizeof(*pContextBuffer));
        }
        else
        {
            IfFailThrow(hr);
        }
    }
    else
    {
        *pContextBuffer = *pFilterContext;
    }

} // DacDbiInterfaceImpl::GetContext

// Create a VMPTR_Object from a target object address
// @dbgtodo validate the VMPTR_Object is in fact a object, possibly by DACizing
//          Object::Validate
VMPTR_Object DacDbiInterfaceImpl::GetObject(CORDB_ADDRESS ptr)
{
    DD_ENTER_MAY_THROW;

    VMPTR_Object vmObj = VMPTR_Object::NullPtr();
    vmObj.SetDacTargetPtr(CORDB_ADDRESS_TO_TADDR(ptr));
    return vmObj;
}

HRESULT DacDbiInterfaceImpl::EnableNGENPolicy(CorDebugNGENPolicy ePolicy)
{
    return E_NOTIMPL;
}

HRESULT DacDbiInterfaceImpl::SetNGENCompilerFlags(DWORD dwFlags)
{
    DD_ENTER_MAY_THROW;

    return CORDBG_E_NGEN_NOT_SUPPORTED;
}

HRESULT DacDbiInterfaceImpl::GetNGENCompilerFlags(DWORD *pdwFlags)
{
    DD_ENTER_MAY_THROW;

    return CORDBG_E_NGEN_NOT_SUPPORTED;
}

typedef DPTR(OBJECTREF) PTR_ObjectRef;

// Create a VMPTR_Object from an address which points to a reference to an object
// @dbgtodo validate the VMPTR_Object is in fact a object, possibly by DACizing
//          Object::Validate
VMPTR_Object DacDbiInterfaceImpl::GetObjectFromRefPtr(CORDB_ADDRESS ptr)
{
    DD_ENTER_MAY_THROW;

    VMPTR_Object vmObj = VMPTR_Object::NullPtr();
    PTR_ObjectRef objRef = PTR_ObjectRef(CORDB_ADDRESS_TO_TADDR(ptr));
    vmObj.SetDacTargetPtr(PTR_TO_TADDR(*objRef));

    return vmObj;
}

// Create a VMPTR_OBJECTHANDLE from a handle
VMPTR_OBJECTHANDLE DacDbiInterfaceImpl::GetVmObjectHandle(CORDB_ADDRESS handleAddress)
{
    DD_ENTER_MAY_THROW;

    VMPTR_OBJECTHANDLE vmObjHandle = VMPTR_OBJECTHANDLE::NullPtr();
    vmObjHandle.SetDacTargetPtr(CORDB_ADDRESS_TO_TADDR(handleAddress));

    return vmObjHandle;
}


// Validate that the VMPTR_OBJECTHANDLE refers to a legitimate managed object
BOOL DacDbiInterfaceImpl::IsVmObjectHandleValid(VMPTR_OBJECTHANDLE vmHandle)
{
    DD_ENTER_MAY_THROW;

    BOOL ret = FALSE;
    // this may cause unallocated debuggee memory to be read
    // SEH exceptions will be caught
    EX_TRY
    {
        OBJECTREF objRef = ObjectFromHandle((OBJECTHANDLE)vmHandle.GetDacPtr());

        // NULL is certainly valid...
        if (objRef != NULL)
        {
            if (objRef->ValidateObjectWithPossibleAV())
            {
                ret = TRUE;
            }
        }
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);

    return ret;
}

// determines if the specified module is a WinRT module
HRESULT DacDbiInterfaceImpl::IsWinRTModule(VMPTR_Module vmModule, BOOL& isWinRT)
{
    DD_ENTER_MAY_THROW;

    HRESULT hr = S_OK;
    isWinRT = FALSE;

    return hr;
}

// Determines the app domain id for the object referred to by a given VMPTR_OBJECTHANDLE
ULONG DacDbiInterfaceImpl::GetAppDomainIdFromVmObjectHandle(VMPTR_OBJECTHANDLE vmHandle)
{
    DD_ENTER_MAY_THROW;

    return DefaultADID;
}

// Get the target address from a VMPTR_OBJECTHANDLE, i.e., the handle address
CORDB_ADDRESS DacDbiInterfaceImpl::GetHandleAddressFromVmHandle(VMPTR_OBJECTHANDLE vmHandle)
{
    DD_ENTER_MAY_THROW;

    CORDB_ADDRESS handle = vmHandle.GetDacPtr();

    return handle;
}

// Create a TargetBuffer which describes the location of the object
TargetBuffer DacDbiInterfaceImpl::GetObjectContents(VMPTR_Object vmObj)
{
    DD_ENTER_MAY_THROW;
    PTR_Object objPtr = vmObj.GetDacPtr();

    _ASSERTE(objPtr->GetSize() <= 0xffffffff);
    return TargetBuffer(PTR_TO_TADDR(objPtr), (ULONG)objPtr->GetSize());
}

// ============================================================================
// functions to get information about objects referenced via an instance of CordbReferenceValue or
// CordbHandleValue
// ============================================================================

// DacDbiInterfaceImpl::FastSanityCheckObject
// Helper function for CheckRef. Sanity check an object.
// We use a fast and easy check to improve confidence that objPtr points to a valid object.
// We can't tell cheaply if this is really a valid object (that would require walking the GC heap), but at
// least we can check if we get an EEClass from the supposed method table and then get the method table from
// the class. If we can, we have improved the probability that the object is valid.
// Arguments:
//     input: objPtr - address of the object we are checking
// Return Value: E_INVALIDARG or S_OK.
HRESULT DacDbiInterfaceImpl::FastSanityCheckObject(PTR_Object objPtr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    EX_TRY
    {
        // NULL is certainly valid...
        if (objPtr != NULL)
        {
            if (!objPtr->ValidateObjectWithPossibleAV())
            {
                LOG((LF_CORDB, LL_INFO10000, "GOI: object methodtable-class invariant doesn't hold.\n"));
                hr = E_INVALIDARG;
            }
        }
    }
    EX_CATCH
    {
        LOG((LF_CORDB, LL_INFO10000, "GOI: exception indicated ref is bad.\n"));
        hr = E_INVALIDARG;
    }
    EX_END_CATCH(SwallowAllExceptions);

    return hr;
}   // DacDbiInterfaceImpl::FastSanityCheckObject

// Perform a sanity check on an object address to determine if this _could be_ a valid object.
// We can't tell this for certain without walking the GC heap, but we do some fast tests to rule
// out clearly invalid object addresses. See code:DacDbiInterfaceImpl::FastSanityCheckObject for more
// details.
// Arguments:
//     input:  objPtr    - address of the object we are checking
// Return Value:
//             objRefBad - true iff we have determined the address cannot be pointing to a valid object.
//                         Note that a value of false doesn't necessarily guarantee the object is really
//                         valid
bool DacDbiInterfaceImpl::CheckRef(PTR_Object objPtr)
{
    bool objRefBad = false;

    // Shortcut null references now...
    if (objPtr == NULL)
    {
        LOG((LF_CORDB, LL_INFO10000, "D::GOI: ref is NULL.\n"));

        objRefBad = true;
    }
    else
    {
        // Try to verify the integrity of the object. This is not fool proof.
        // @todo - this whole idea of expecting AVs is broken, but it does rule
        // out a fair bit of rubbish. Find another
        // way to test if the object is valid?
        if (FAILED(FastSanityCheckObject(objPtr)))
        {
            LOG((LF_CORDB, LL_INFO10000, "D::GOI: address is not a valid object.\n"));

            objRefBad = true;
        }
    }

    return objRefBad;
} // DacDbiInterfaceImpl::CheckRef

// DacDbiInterfaceImpl::InitObjectData
// Initialize basic object information: type handle, object size, offset to fields and expanded type
// information.
// Arguments:
//     input:  objPtr      - address of object of interest
//             vmAppDomain - AppDomain for the type f the object
//     output: pObjectData - object information
// Note: It is assumed that pObjectData is non-null.
void DacDbiInterfaceImpl::InitObjectData(PTR_Object                objPtr,
                                         VMPTR_AppDomain           vmAppDomain,
                                         DebuggerIPCE_ObjectData * pObjectData)
{
    _ASSERTE(pObjectData != NULL);
    // @todo - this is still dangerous because the object may still be invalid.
    VMPTR_TypeHandle vmTypeHandle = VMPTR_TypeHandle::NullPtr();
    vmTypeHandle.SetDacTargetPtr(objPtr->GetGCSafeTypeHandle().AsTAddr());

    // Save basic object info.
    pObjectData->objSize = objPtr->GetSize();
    pObjectData->objOffsetToVars = dac_cast<TADDR>((objPtr)->GetData()) - dac_cast<TADDR>(objPtr);

    TypeHandleToExpandedTypeInfo(AllBoxed, vmAppDomain, vmTypeHandle, &(pObjectData->objTypeData));

    // If this is a string object, set the type to ELEMENT_TYPE_STRING.
    if (objPtr->GetGCSafeMethodTable() == g_pStringClass)
    {
        pObjectData->objTypeData.elementType = ELEMENT_TYPE_STRING;
        if(pObjectData->objSize < MIN_OBJECT_SIZE)
        {
            pObjectData->objSize = PtrAlign(pObjectData->objSize);
        }
    }
} // DacDbiInterfaceImpl::InitObjectData

// DAC/DBI API

// Get object information for a TypedByRef object (System.TypedReference).

// These are objects that contain a managed pointer to a location and the type of the value at that location.
// They are most commonly used for varargs but also may be used for parameters and locals. They are
// stack-allocated. They provide a means for adding dynamic type information to a value type, whereas boxing
// provides only static type information. This means they can be passed as reference parameters to
// polymorphic methods that don't statically restrict the type of arguments they can receive.

// Although they are represented simply as an address, unlike other object references, they don't point
// directly to the object. Instead, there is an extra level of indirection. The reference points to a struct
// that contains the address of the object, so we need to treat them differently. They have their own
// CorElementType (ELEMENT_TYPE_TYPEDBYREF) which makes it possible to identify this special case.

// Example:
// static int AddABunchOfInts (__arglist)
// {
//     int result = 0;
//
//     System.ArgIterator iter = new System.ArgIterator (__arglist);
//     int argCount = iter.GetRemainingCount();
//
//     for (int i = 0; i < argCount; i++)
//     {
//         System.TypedReference typedRef = iter.GetNextArg();
//         result += (int)TypedReference.ToObject(typedRef);
//     }
//
//     return result;
// }
//
// static int Main (string[] args)
// {
//     int result = AddABunchOfInts (__arglist (2, 3, 4));
//     Console.WriteLine ("Answer: {0}", result);
//
//     if (result != 9)
//         return 1;
//
//     return 0;
// }

// Initializes the objRef and typedByRefType fields of pObjectData (type info for the referent).
void DacDbiInterfaceImpl::GetTypedByRefInfo(CORDB_ADDRESS             pTypedByRef,
                                            VMPTR_AppDomain           vmAppDomain,
                                            DebuggerIPCE_ObjectData * pObjectData)
{
     DD_ENTER_MAY_THROW;

    // pTypedByRef is really the address of a TypedByRef struct rather than of a normal object.
    // The data field of the TypedByRef struct is the actual object ref.
    PTR_TypedByRef refAddr = PTR_TypedByRef(TADDR(pTypedByRef));

    _ASSERTE(refAddr != NULL);
    _ASSERTE(pObjectData != NULL);

    // The type of the referent is in the type field of the TypedByRef. We need to initialize the object
    // data type information.
    TypeHandleToBasicTypeInfo(refAddr->type,
                              &(pObjectData->typedByrefInfo.typedByrefType),
                              vmAppDomain.GetDacPtr());

    // The reference to the object is in the data field of the TypedByRef.
    CORDB_ADDRESS tempRef = dac_cast<TADDR>(refAddr->data);
    pObjectData->objRef = CORDB_ADDRESS_TO_PTR(tempRef);

    LOG((LF_CORDB, LL_INFO10000, "D::GASOI: sending REFANY result: "
         "ref=0x%08x, cls=0x%08x, mod=0x%p\n",
         pObjectData->objRef,
         pObjectData->typedByrefType.metadataToken,
         pObjectData->typedByrefType.vmDomainAssembly.GetDacPtr()));
} // DacDbiInterfaceImpl::GetTypedByRefInfo

// Get the string data associated withn obj and put it into the pointers
// DAC/DBI API
// Get the string length and offset to string base for a string object
void DacDbiInterfaceImpl::GetStringData(CORDB_ADDRESS objectAddress, DebuggerIPCE_ObjectData * pObjectData)
{
    DD_ENTER_MAY_THROW;

    PTR_Object objPtr = PTR_Object(TADDR(objectAddress));
    LOG((LF_CORDB, LL_INFO10000, "D::GOI: The referent is a string.\n"));

    if (objPtr->GetGCSafeMethodTable() != g_pStringClass)
    {
        ThrowHR(CORDBG_E_TARGET_INCONSISTENT);
    }

    PTR_StringObject pStrObj = dac_cast<PTR_StringObject>(objPtr);

    _ASSERTE(pStrObj != NULL);
    pObjectData->stringInfo.length = pStrObj->GetStringLength();
    pObjectData->stringInfo.offsetToStringBase = (UINT_PTR) pStrObj->GetBufferOffset();

} // DacDbiInterfaceImpl::GetStringData


// DAC/DBI API
// Get information for an array type referent of an objRef, including rank, upper and lower
// bounds, element size and type, and the number of elements.
void DacDbiInterfaceImpl::GetArrayData(CORDB_ADDRESS objectAddress, DebuggerIPCE_ObjectData * pObjectData)
{
    DD_ENTER_MAY_THROW;

    PTR_Object objPtr = PTR_Object(TADDR(objectAddress));
    PTR_MethodTable pMT = objPtr->GetGCSafeMethodTable();

    if (!objPtr->GetGCSafeTypeHandle().IsArray())
    {
        LOG((LF_CORDB, LL_INFO10000,
             "D::GASOI: object should be an array.\n"));

        pObjectData->objRefBad = true;
    }
    else
    {
        PTR_ArrayBase arrPtr = dac_cast<PTR_ArrayBase>(objPtr);

        // this is also returned in the type information for the array - we return both for sanity checking...
        pObjectData->arrayInfo.rank = arrPtr->GetRank();
        pObjectData->arrayInfo.componentCount = arrPtr->GetNumComponents();
        pObjectData->arrayInfo.offsetToArrayBase = arrPtr->GetDataPtrOffset(pMT);

        if (arrPtr->IsMultiDimArray())
        {
            pObjectData->arrayInfo.offsetToUpperBounds = SIZE_T(arrPtr->GetBoundsOffset(pMT));

            pObjectData->arrayInfo.offsetToLowerBounds = SIZE_T(arrPtr->GetLowerBoundsOffset(pMT));
        }
        else
        {
            pObjectData->arrayInfo.offsetToUpperBounds = 0;
            pObjectData->arrayInfo.offsetToLowerBounds = 0;
        }

        pObjectData->arrayInfo.elementSize = arrPtr->GetComponentSize();

        LOG((LF_CORDB, LL_INFO10000, "D::GOI: array info: "
            "baseOff=%d, lowerOff=%d, upperOff=%d, cnt=%d, rank=%d, rank (2) = %d,"
             "eleSize=%d, eleType=0x%02x\n",
             pObjectData->arrayInfo.offsetToArrayBase,
             pObjectData->arrayInfo.offsetToLowerBounds,
             pObjectData->arrayInfo.offsetToUpperBounds,
             pObjectData->arrayInfo.componentCount,
             pObjectData->arrayInfo.rank,
             pObjectData->objTypeData.ArrayTypeData.arrayRank,
             pObjectData->arrayInfo.elementSize,
             pObjectData->objTypeData.ArrayTypeData.arrayTypeArg.elementType));
    }
} // DacDbiInterfaceImpl::GetArrayData

// DAC/DBI API: Get information about an object for which we have a reference, including the object size and
// type information.
void DacDbiInterfaceImpl::GetBasicObjectInfo(CORDB_ADDRESS             objectAddress,
                                             CorElementType            type,
                                             VMPTR_AppDomain           vmAppDomain,
                                             DebuggerIPCE_ObjectData * pObjectData)
{
    DD_ENTER_MAY_THROW;

    PTR_Object objPtr = PTR_Object(TADDR(objectAddress));
    pObjectData->objRefBad = CheckRef(objPtr);
    if (pObjectData->objRefBad != true)
    {
        // initialize object type, size, offset information. Note: We may have a different element type
        // after this. For example, we may start with E_T_CLASS but return with something more specific.
        InitObjectData (objPtr, vmAppDomain, pObjectData);
    }
} // DacDbiInterfaceImpl::GetBasicObjectInfo

// This is the data passed to EnumerateBlockingObjectsCallback below
struct BlockingObjectUserDataWrapper
{
    CALLBACK_DATA pUserData;
    IDacDbiInterface::FP_BLOCKINGOBJECT_ENUMERATION_CALLBACK fpCallback;
};

// The callback helper used by EnumerateBlockingObjects below, this
// callback in turn invokes the user's callback with the right arguments
void EnumerateBlockingObjectsCallback(PTR_DebugBlockingItem obj, VOID* pUserData)
{
    BlockingObjectUserDataWrapper* wrapper = (BlockingObjectUserDataWrapper*)pUserData;
    DacBlockingObject dacObj;

    // init to an arbitrary value to avoid mac compiler error about uninitialized use
    // it will be correctly set in the switch and is never used with only this init here
    dacObj.blockingReason = DacBlockReason_MonitorCriticalSection;

    dacObj.vmBlockingObject.SetDacTargetPtr(dac_cast<TADDR>(OBJECTREFToObject(obj->pMonitor->GetOwningObject())));
    dacObj.dwTimeout = obj->dwTimeout;
    dacObj.vmAppDomain.SetDacTargetPtr(dac_cast<TADDR>(obj->pAppDomain));
    switch(obj->type)
    {
        case DebugBlock_MonitorCriticalSection:
            dacObj.blockingReason = DacBlockReason_MonitorCriticalSection;
            break;
        case DebugBlock_MonitorEvent:
            dacObj.blockingReason = DacBlockReason_MonitorEvent;
            break;
        default:
            _ASSERTE(!"obj->type has an invalid value");
            return;
    }

    wrapper->fpCallback(dacObj, wrapper->pUserData);
}

// DAC/DBI API:
// Enumerate all monitors blocking a thread
void DacDbiInterfaceImpl::EnumerateBlockingObjects(VMPTR_Thread                           vmThread,
                                                   FP_BLOCKINGOBJECT_ENUMERATION_CALLBACK fpCallback,
                                                   CALLBACK_DATA                          pUserData)
{
    DD_ENTER_MAY_THROW;

    Thread * pThread = vmThread.GetDacPtr();
    _ASSERTE(pThread != NULL);

    BlockingObjectUserDataWrapper wrapper;
    wrapper.fpCallback = fpCallback;
    wrapper.pUserData = pUserData;

    pThread->DebugBlockingInfo.VisitBlockingItems((DebugBlockingItemVisitor)EnumerateBlockingObjectsCallback,
        (VOID*)&wrapper);
}

// DAC/DBI API:
// Returns the thread which owns the monitor lock on an object and the acquisition count
MonitorLockInfo DacDbiInterfaceImpl::GetThreadOwningMonitorLock(VMPTR_Object vmObject)
{
    DD_ENTER_MAY_THROW;
    MonitorLockInfo info;
    info.lockOwner = VMPTR_Thread::NullPtr();
    info.acquisitionCount = 0;

    Object* pObj = vmObject.GetDacPtr();
    DWORD threadId;
    DWORD acquisitionCount;
    if(!pObj->GetHeader()->GetThreadOwningMonitorLock(&threadId, &acquisitionCount))
    {
        return info;
    }

    Thread *pThread = ThreadStore::GetThreadList(NULL);
    while (pThread != NULL)
    {
        if(pThread->GetThreadId() == threadId)
        {
            info.lockOwner.SetDacTargetPtr(PTR_HOST_TO_TADDR(pThread));
            info.acquisitionCount = acquisitionCount;
            return info;
        }
        pThread = ThreadStore::GetThreadList(pThread);
    }
    _ASSERTE(!"A thread should have been found");
    return info;
}

// The data passed to EnumerateThreadsCallback below
struct ThreadUserDataWrapper
{
    CALLBACK_DATA pUserData;
    IDacDbiInterface::FP_THREAD_ENUMERATION_CALLBACK fpCallback;
};

// The callback helper used for EnumerateMonitorEventWaitList below. This callback
// invokes the user's callback with the correct arguments.
void EnumerateThreadsCallback(PTR_Thread pThread, VOID* pUserData)
{
    ThreadUserDataWrapper* wrapper = (ThreadUserDataWrapper*)pUserData;
    VMPTR_Thread vmThread = VMPTR_Thread::NullPtr();
    vmThread.SetDacTargetPtr(dac_cast<TADDR>(pThread));
    wrapper->fpCallback(vmThread, wrapper->pUserData);
}

// DAC/DBI API:
// Enumerate all threads waiting on the monitor event for an object
void DacDbiInterfaceImpl::EnumerateMonitorEventWaitList(VMPTR_Object                   vmObject,
                                                        FP_THREAD_ENUMERATION_CALLBACK fpCallback,
                                                        CALLBACK_DATA                  pUserData)
{
    DD_ENTER_MAY_THROW;

    Object* pObj = vmObject.GetDacPtr();
    SyncBlock* psb = pObj->PassiveGetSyncBlock();

    // no sync block means no wait list
    if(psb == NULL)
        return;

    ThreadUserDataWrapper wrapper;
    wrapper.fpCallback = fpCallback;
    wrapper.pUserData = pUserData;
    ThreadQueue::EnumerateThreads(psb, (FP_TQ_THREAD_ENUMERATION_CALLBACK)EnumerateThreadsCallback, (VOID*) &wrapper);
}


bool DacDbiInterfaceImpl::AreGCStructuresValid()
{
    return true;
}

HeapData::HeapData()
    : YoungestGenPtr(0), YoungestGenLimit(0), Gen0Start(0), Gen0End(0), SegmentCount(0), Segments(0)
{
}

HeapData::~HeapData()
{
    if (Segments)
        delete [] Segments;
}

LinearReadCache::LinearReadCache()
    : mCurrPageStart(0), mPageSize(0), mCurrPageSize(0), mPage(0)
{
    SYSTEM_INFO si;
	GetSystemInfo(&si);

    mPageSize = si.dwPageSize;
    mPage = new (nothrow) BYTE[mPageSize];
}

LinearReadCache::~LinearReadCache()
{
    if (mPage)
        delete [] mPage;
}

bool LinearReadCache::MoveToPage(CORDB_ADDRESS addr)
{
    mCurrPageStart = addr - (addr % mPageSize);
    HRESULT hr = g_dacImpl->m_pTarget->ReadVirtual(mCurrPageStart, mPage, mPageSize, &mCurrPageSize);

    if (hr != S_OK)
    {
        mCurrPageStart = 0;
        mCurrPageSize = 0;
        return false;
    }

    return true;
}


CORDB_ADDRESS DacHeapWalker::HeapStart = 0;
CORDB_ADDRESS DacHeapWalker::HeapEnd = ~0;

DacHeapWalker::DacHeapWalker()
    : mThreadCount(0), mAllocInfo(0), mHeapCount(0), mHeaps(0),
        mCurrObj(0), mCurrSize(0), mCurrMT(0),
        mCurrHeap(0), mCurrSeg(0), mStart((TADDR)HeapStart), mEnd((TADDR)HeapEnd)
{
}

DacHeapWalker::~DacHeapWalker()
{
    if (mAllocInfo)
        delete [] mAllocInfo;

    if (mHeaps)
        delete [] mHeaps;
}

SegmentData *DacHeapWalker::FindSegment(CORDB_ADDRESS obj)
{
    for (size_t i = 0; i < mHeapCount; ++i)
        for (size_t j = 0; j < mHeaps[i].SegmentCount; ++j)
            if (mHeaps[i].Segments[j].Start <= obj && obj <= mHeaps[i].Segments[j].End)
                return &mHeaps[i].Segments[j];

    return NULL;
}

HRESULT DacHeapWalker::Next(CORDB_ADDRESS *pValue, CORDB_ADDRESS *pMT, ULONG64 *pSize)
{
    if (!HasMoreObjects())
        return E_FAIL;

    if (pValue)
        *pValue = mCurrObj;

    if (pMT)
        *pMT = (CORDB_ADDRESS)mCurrMT;

    if (pSize)
        *pSize = (ULONG64)mCurrSize;

    HRESULT hr = MoveToNextObject();
    return FAILED(hr) ? hr : S_OK;
}



HRESULT DacHeapWalker::MoveToNextObject()
{
    do
    {
        // Move to the next object
        mCurrObj += mCurrSize;

        // Check to see if we are in the correct bounds.
        bool isGen0 = IsRegionGCEnabled() ? (mHeaps[mCurrHeap].Segments[mCurrSeg].Generation == 0) :
                                   (mHeaps[mCurrHeap].Gen0Start <= mCurrObj && mHeaps[mCurrHeap].Gen0End > mCurrObj);

        if (isGen0)
            CheckAllocAndSegmentRange();

        // Check to see if we've moved off the end of a segment
        if (mCurrObj >= mHeaps[mCurrHeap].Segments[mCurrSeg].End || mCurrObj > mEnd)
        {
            HRESULT hr = NextSegment();
            if (FAILED(hr) || hr == S_FALSE)
                return hr;
        }

        // Get the method table pointer
        if (!mCache.ReadMT(mCurrObj, &mCurrMT))
            return E_FAIL;

        if (!GetSize(mCurrMT, mCurrSize))
            return E_FAIL;
    } while (mCurrObj < mStart);

    _ASSERTE(mStart <= mCurrObj && mCurrObj <= mEnd);
    return S_OK;
}

bool DacHeapWalker::GetSize(TADDR tMT, size_t &size)
{
    // With heap corruption, it's entirely possible that the MethodTable
    // we get is bad.  This could cause exceptions, which we will catch
    // and return false.  This causes the heapwalker to move to the next
    // segment.
    bool ret = true;
    EX_TRY
    {
        MethodTable *mt = PTR_MethodTable(tMT);
        size_t cs = mt->GetComponentSize();

        if (cs)
        {
            DWORD tmp = 0;
            if (mCache.Read(mCurrObj + sizeof(TADDR), &tmp))
                cs *= tmp;
            else
                ret = false;
        }

        size = mt->GetBaseSize() + cs;

        // The size is not guaranteed to be aligned, we have to
        // do that ourself.
        if (mHeaps[mCurrHeap].Segments[mCurrSeg].Generation == 3
            || mHeaps[mCurrHeap].Segments[mCurrSeg].Generation == 4)
            size = AlignLarge(size);
        else
            size = Align(size);

        // If size == 0, it means we have a heap corruption and
        // we will stuck in an infinite loop, so better fail the call now.
        ret &= (0 < size);
        // Also guard for cases where the size reported is too large and exceeds the high allocation mark.
        ret &= ((mCurrObj + size) <= mHeaps[mCurrHeap].Segments[mCurrSeg].End);
    }
    EX_CATCH
    {
        ret = false;
    }
    EX_END_CATCH(SwallowAllExceptions)

    return ret;
}


HRESULT DacHeapWalker::NextSegment()
{
    mCurrObj = 0;
    mCurrMT = 0;
    mCurrSize = 0;

    do
    {
        do
        {
            mCurrSeg++;
            while (mCurrSeg >= mHeaps[mCurrHeap].SegmentCount)
            {
                mCurrSeg = 0;
                mCurrHeap++;

                if (mCurrHeap >= mHeapCount)
                {
                    return S_FALSE;
                }
            }
        } while (mHeaps[mCurrHeap].Segments[mCurrSeg].Start >= mHeaps[mCurrHeap].Segments[mCurrSeg].End);

        mCurrObj = mHeaps[mCurrHeap].Segments[mCurrSeg].Start;

        bool isGen0 = IsRegionGCEnabled() ? (mHeaps[mCurrHeap].Segments[mCurrSeg].Generation == 0) :
                                   (mHeaps[mCurrHeap].Gen0Start <= mCurrObj && mHeaps[mCurrHeap].Gen0End > mCurrObj);

        if (isGen0)
            CheckAllocAndSegmentRange();

        if (!mCache.ReadMT(mCurrObj, &mCurrMT))
        {
            return E_FAIL;
        }

        if (!GetSize(mCurrMT, mCurrSize))
        {
            return E_FAIL;
        }
    } while((mHeaps[mCurrHeap].Segments[mCurrSeg].Start > mEnd) || (mHeaps[mCurrHeap].Segments[mCurrSeg].End < mStart));

    return S_OK;
}

void DacHeapWalker::CheckAllocAndSegmentRange()
{
    const size_t MinObjSize = sizeof(TADDR)*3;

    for (int i = 0; i < mThreadCount; ++i)
        if (mCurrObj == mAllocInfo[i].Ptr)
        {
            mCurrObj = mAllocInfo[i].Limit + Align(MinObjSize);
            break;
        }

    if (mCurrObj == mHeaps[mCurrHeap].YoungestGenPtr)
    {
        mCurrObj = mHeaps[mCurrHeap].YoungestGenLimit + Align(MinObjSize);
    }
}

HRESULT DacHeapWalker::Init(CORDB_ADDRESS start, CORDB_ADDRESS end)
{
    // Collect information about the allocation contexts in the process.
    ThreadStore* threadStore = ThreadStore::s_pThreadStore;
    if (threadStore != NULL)
    {
        int count = (int)threadStore->ThreadCountInEE();
        mAllocInfo = new (nothrow) AllocInfo[count + 1];
        if (mAllocInfo == NULL)
            return E_OUTOFMEMORY;

        Thread *thread = NULL;
        int j = 0;
        for (int i = 0; i < count; ++i)
        {
            // The thread or allocation context being null is troubling, but not fatal.
            // We may have stopped the process where the thread list or thread's alloc
            // context was in an inconsistent state.  We will simply skip over affected
            // segments during the heap walk if we encounter problems due to this.
            thread = ThreadStore::GetThreadList(thread);
            if (thread == NULL)
                continue;

            gc_alloc_context *ctx = thread->GetAllocContext();
            if (ctx == NULL)
                continue;

            if ((CORDB_ADDRESS)ctx->alloc_ptr != NULL)
            {
                mAllocInfo[j].Ptr = (CORDB_ADDRESS)ctx->alloc_ptr;
                mAllocInfo[j].Limit = (CORDB_ADDRESS)ctx->alloc_limit;
                j++;
            }
        }
        if ((&g_global_alloc_context)->alloc_ptr != nullptr)
        {
            mAllocInfo[j].Ptr = (CORDB_ADDRESS)(&g_global_alloc_context)->alloc_ptr;
            mAllocInfo[j].Limit = (CORDB_ADDRESS)(&g_global_alloc_context)->alloc_limit;
        }

        mThreadCount = j;
    }

#ifdef FEATURE_SVR_GC
    HRESULT hr = GCHeapUtilities::IsServerHeap() ? InitHeapDataSvr(mHeaps, mHeapCount) : InitHeapDataWks(mHeaps, mHeapCount);
#else
    HRESULT hr = InitHeapDataWks(mHeaps, mHeapCount);
#endif

    // Set up mCurrObj/mCurrMT.
    if (SUCCEEDED(hr))
        hr = Reset(start, end);

    // Collect information about GC heaps
    return hr;
}

HRESULT DacHeapWalker::Reset(CORDB_ADDRESS start, CORDB_ADDRESS end)
{
    _ASSERTE(mHeaps);
    _ASSERTE(mHeapCount > 0);
    _ASSERTE(mHeaps[0].Segments);
    _ASSERTE(mHeaps[0].SegmentCount > 0);

    mStart = start;
    mEnd = end;

    // Set up first object
    mCurrObj = mHeaps[0].Segments[0].Start;
    mCurrMT = 0;
    mCurrSize = 0;
    mCurrHeap = 0;
    mCurrSeg = 0;

    HRESULT hr = S_OK;

    // it's possible the first segment is empty
    if (mCurrObj >= mHeaps[0].Segments[0].End)
        hr = MoveToNextObject();

    if (!mCache.ReadMT(mCurrObj, &mCurrMT))
        return E_FAIL;

    if (!GetSize(mCurrMT, mCurrSize))
        return E_FAIL;

    if (mCurrObj < mStart || mCurrObj > mEnd)
        hr = MoveToNextObject();

    return hr;
}

HRESULT DacHeapWalker::ListNearObjects(CORDB_ADDRESS obj, CORDB_ADDRESS *pPrev, CORDB_ADDRESS *pContaining, CORDB_ADDRESS *pNext)
{
    SegmentData *seg = FindSegment(obj);

    if (seg == NULL)
        return E_FAIL;

    HRESULT hr = Reset(seg->Start, seg->End);
    if (SUCCEEDED(hr))
    {
        CORDB_ADDRESS prev = 0;
        CORDB_ADDRESS curr = 0;
        ULONG64 size = 0;
        bool found = false;

        while (!found && HasMoreObjects())
        {
            prev = curr;
            hr = Next(&curr, NULL, &size);
            if (FAILED(hr))
                break;

            if (obj >= curr && obj < curr + size)
                found = true;
        }

        if (found)
        {
            if (pPrev)
                *pPrev = prev;

            if (pContaining)
                *pContaining = curr;

            if (pNext)
            {
                if (HasMoreObjects())
                {
                    hr = Next(&curr, NULL, NULL);
                    if (SUCCEEDED(hr))
                        *pNext = curr;
                }
                else
                {
                    *pNext = 0;
                }
            }

            hr = S_OK;
        }
        else if (SUCCEEDED(hr))
        {
            hr = E_FAIL;
        }
    }

    return hr;
}

HRESULT DacHeapWalker::InitHeapDataWks(HeapData *&pHeaps, size_t &pCount)
{
    bool regions = IsRegionGCEnabled();

    // Scrape basic heap details
    pCount = 1;
    pHeaps = new (nothrow) HeapData[1];
    if (pHeaps == NULL)
        return E_OUTOFMEMORY;

    dac_generation gen0 = GenerationTableIndex(g_gcDacGlobals->generation_table, 0);
    dac_generation gen1 = GenerationTableIndex(g_gcDacGlobals->generation_table, 1);
    dac_generation gen2 = GenerationTableIndex(g_gcDacGlobals->generation_table, 2);
    dac_generation loh  = GenerationTableIndex(g_gcDacGlobals->generation_table, 3);
    dac_generation poh  = GenerationTableIndex(g_gcDacGlobals->generation_table, 4);

    pHeaps[0].YoungestGenPtr = (CORDB_ADDRESS)gen0.allocation_context.alloc_ptr;
    pHeaps[0].YoungestGenLimit = (CORDB_ADDRESS)gen0.allocation_context.alloc_limit;

    if (!regions)
    {
        pHeaps[0].Gen0Start = (CORDB_ADDRESS)gen0.allocation_start;
        pHeaps[0].Gen0End = (CORDB_ADDRESS)*g_gcDacGlobals->alloc_allocated;
        pHeaps[0].Gen1Start = (CORDB_ADDRESS)gen1.allocation_start;
    }

    // Segments
    int count = GetSegmentCount(loh.start_segment);
    count += GetSegmentCount(poh.start_segment);
    count += GetSegmentCount(gen2.start_segment);
    if (regions)
    {
        count += GetSegmentCount(gen1.start_segment);
        count += GetSegmentCount(gen0.start_segment);
    }

    pHeaps[0].SegmentCount = count;
    pHeaps[0].Segments = new (nothrow) SegmentData[count];
    if (pHeaps[0].Segments == NULL)
        return E_OUTOFMEMORY;

    DPTR(dac_heap_segment) seg;
    int i = 0;

    // Small object heap segments
    if (regions)
    {
        seg = gen2.start_segment;
        for (; seg && (i < count); ++i)
        {
            pHeaps[0].Segments[i].Generation = seg->flags & HEAP_SEGMENT_FLAGS_READONLY ? CorDebug_NonGC : CorDebug_Gen2;
            pHeaps[0].Segments[i].Start = (CORDB_ADDRESS)seg->mem;
            pHeaps[0].Segments[i].End = (CORDB_ADDRESS)seg->allocated;

            seg = seg->next;
        }
        seg = gen1.start_segment;
        for (; seg && (i < count); ++i)
        {
            pHeaps[0].Segments[i].Generation = CorDebug_Gen1;
            pHeaps[0].Segments[i].Start = (CORDB_ADDRESS)seg->mem;
            pHeaps[0].Segments[i].End = (CORDB_ADDRESS)seg->allocated;

            seg = seg->next;
        }
        seg = gen0.start_segment;
        for (; seg && (i < count); ++i)
        {
            pHeaps[0].Segments[i].Start = (CORDB_ADDRESS)seg->mem;
            if (seg.GetAddr() == (TADDR)*g_gcDacGlobals->ephemeral_heap_segment)
            {
                pHeaps[0].Segments[i].End = (CORDB_ADDRESS)*g_gcDacGlobals->alloc_allocated;
                pHeaps[0].EphemeralSegment = i;
            }
            else
            {
                pHeaps[0].Segments[i].End = (CORDB_ADDRESS)seg->allocated;
            }
            pHeaps[0].Segments[i].Generation = CorDebug_Gen0;

            seg = seg->next;
        }
    }
    else
    {
        DPTR(dac_heap_segment) seg = gen2.start_segment;
        for (; seg && (i < count); ++i)
        {
            pHeaps[0].Segments[i].Start = (CORDB_ADDRESS)seg->mem;
            if (seg.GetAddr() == (TADDR)*g_gcDacGlobals->ephemeral_heap_segment)
            {
                pHeaps[0].Segments[i].End = (CORDB_ADDRESS)*g_gcDacGlobals->alloc_allocated;
                pHeaps[0].Segments[i].Generation = CorDebug_Gen1;
                pHeaps[0].EphemeralSegment = i;
            }
            else
            {
                pHeaps[0].Segments[i].End = (CORDB_ADDRESS)seg->allocated;
                pHeaps[0].Segments[i].Generation = seg->flags & HEAP_SEGMENT_FLAGS_READONLY ? CorDebug_NonGC : CorDebug_Gen2;
            }

            seg = seg->next;
        }
    }

    // Large object heap segments
    seg = loh.start_segment;
    for (; seg && (i < count); ++i)
    {
        pHeaps[0].Segments[i].Generation = CorDebug_LOH;
        pHeaps[0].Segments[i].Start = (CORDB_ADDRESS)seg->mem;
        pHeaps[0].Segments[i].End = (CORDB_ADDRESS)seg->allocated;

        seg = seg->next;
    }

    // Pinned object heap segments
    seg = poh.start_segment;
    for (; seg && (i < count); ++i)
    {
        pHeaps[0].Segments[i].Generation = CorDebug_POH;
        pHeaps[0].Segments[i].Start = (CORDB_ADDRESS)seg->mem;
        pHeaps[0].Segments[i].End = (CORDB_ADDRESS)seg->allocated;

        seg = seg->next;
    }

    _ASSERTE(count == i);

    return S_OK;
}

 HRESULT DacDbiInterfaceImpl::CreateHeapWalk(IDacDbiInterface::HeapWalkHandle *pHandle)
{
    DD_ENTER_MAY_THROW;

    DacHeapWalker *data = new (nothrow) DacHeapWalker;
    if (data == NULL)
        return E_OUTOFMEMORY;

    HRESULT hr = data->Init();
    if (SUCCEEDED(hr))
        *pHandle = reinterpret_cast<HeapWalkHandle>(data);
    else
        delete data;

    return hr;
}

void DacDbiInterfaceImpl::DeleteHeapWalk(HeapWalkHandle handle)
{
    DD_ENTER_MAY_THROW;

    DacHeapWalker *data = reinterpret_cast<DacHeapWalker*>(handle);
    if (data)
        delete data;
}

HRESULT DacDbiInterfaceImpl::WalkHeap(HeapWalkHandle handle,
                    ULONG count,
                    OUT COR_HEAPOBJECT * objects,
                    OUT ULONG *fetched)
{
    DD_ENTER_MAY_THROW;
    if (fetched == NULL)
        return E_INVALIDARG;

    DacHeapWalker *walk = reinterpret_cast<DacHeapWalker*>(handle);
    *fetched = 0;

    if (!walk->HasMoreObjects())
        return S_FALSE;

    CORDB_ADDRESS freeMT = (CORDB_ADDRESS)g_pFreeObjectMethodTable.GetAddr();

    HRESULT hr = S_OK;
    CORDB_ADDRESS addr, mt;
    ULONG64 size;

    ULONG i = 0;
    while (i < count && walk->HasMoreObjects())
    {
        hr = walk->Next(&addr, &mt, &size);

        if (FAILED(hr))
            break;

        if (mt != freeMT)
        {
            objects[i].address = addr;
            objects[i].type.token1 = mt;
            objects[i].type.token2 = NULL;
            objects[i].size = size;
            i++;
        }
    }

    if (SUCCEEDED(hr))
        hr = (i < count) ? S_FALSE : S_OK;

    *fetched = i;
    return hr;
}



HRESULT DacDbiInterfaceImpl::GetHeapSegments(OUT DacDbiArrayList<COR_SEGMENT> *pSegments)
{
    DD_ENTER_MAY_THROW;


    size_t heapCount = 0;
    HeapData *heaps = 0;

    bool region = IsRegionGCEnabled();

#ifdef FEATURE_SVR_GC
    HRESULT hr = GCHeapUtilities::IsServerHeap() ? DacHeapWalker::InitHeapDataSvr(heaps, heapCount) : DacHeapWalker::InitHeapDataWks(heaps, heapCount);
#else
    HRESULT hr = DacHeapWalker::InitHeapDataWks(heaps, heapCount);
#endif

    NewArrayHolder<HeapData> _heapHolder = heaps;

    // Count the number of segments to know how much to allocate.
    int total = 0;
    for (size_t i = 0; i < heapCount; ++i)
    {
        total += (int)heaps[i].SegmentCount;
        if (!region)
        {
            // SegmentCount is +1 due to the ephemeral segment containing more than one
            // generation (Gen1 + Gen0, and sometimes part of Gen2).
            total++;

            // It's possible that part of Gen2 lives on the ephemeral segment.  If so,
            // we need to add one more to the output.
            const size_t eph = heaps[i].EphemeralSegment;
            _ASSERTE(eph < heaps[i].SegmentCount);
            if (heaps[i].Segments[eph].Start != heaps[i].Gen1Start)
                total++;
        }
    }

    pSegments->Alloc(total);

    // Now walk all segments and write them to the array.
    int curr = 0;
    for (size_t i = 0; i < heapCount; ++i)
    {
        _ASSERTE(curr < total);
        if (!region)
        {
            // Generation 0 is not in the segment list.
            COR_SEGMENT &seg = (*pSegments)[curr++];
            seg.start = heaps[i].Gen0Start;
            seg.end = heaps[i].Gen0End;
            seg.type = CorDebug_Gen0;
            seg.heap = (ULONG)i;
        }

        for (size_t j = 0; j < heaps[i].SegmentCount; ++j)
        {
            if (region)
            {
                _ASSERTE(curr < total);
                COR_SEGMENT &seg = (*pSegments)[curr++];
                seg.start = heaps[i].Segments[j].Start;
                seg.end = heaps[i].Segments[j].End;
                seg.type = (CorDebugGenerationTypes)heaps[i].Segments[j].Generation;
                seg.heap = (ULONG)i;
            }
            else if (heaps[i].Segments[j].Generation == 1)
            {
                // This is the ephemeral segment.  We have already written Gen0,
                // now write Gen1.
                _ASSERTE(heaps[i].Segments[j].Start <= heaps[i].Gen1Start);
                _ASSERTE(heaps[i].Segments[j].End > heaps[i].Gen1Start);

                {
                    _ASSERTE(curr < total);
                    COR_SEGMENT &seg = (*pSegments)[curr++];
                    seg.start = heaps[i].Gen1Start;
                    seg.end = heaps[i].Gen0Start;
                    seg.type = CorDebug_Gen1;
                    seg.heap = (ULONG)i;
                }

                // It's possible for Gen2 to take up a portion of the ephemeral segment.
                // We test for that here.
                if (heaps[i].Segments[j].Start != heaps[i].Gen1Start)
                {
                    _ASSERTE(curr < total);
                    COR_SEGMENT &seg = (*pSegments)[curr++];
                    seg.start = heaps[i].Segments[j].Start;
                    seg.end = heaps[i].Gen1Start;
                    seg.type = CorDebug_Gen2;
                    seg.heap = (ULONG)i;
                }
            }
            else
            {
                // Otherwise, we have a gen2, POH, LOH or NonGC
                _ASSERTE(curr < total);
                COR_SEGMENT &seg = (*pSegments)[curr++];
                seg.start = heaps[i].Segments[j].Start;
                seg.end = heaps[i].Segments[j].End;

                _ASSERTE(heaps[i].Segments[j].Generation <= CorDebug_NonGC);
                seg.type = (CorDebugGenerationTypes)heaps[i].Segments[j].Generation;
                seg.heap = (ULONG)i;
            }
        }
    }

    _ASSERTE(total == curr);
    return hr;
}

bool DacDbiInterfaceImpl::IsValidObject(CORDB_ADDRESS addr)
{
    DD_ENTER_MAY_THROW;

    bool isValid = false;

    if (addr != 0 && addr != (CORDB_ADDRESS)-1)
    {
        EX_TRY
        {
            PTR_Object obj(TO_TADDR(addr));

            PTR_MethodTable mt = obj->GetMethodTable();
            PTR_EEClass cls = mt->GetClass();

            if (mt == cls->GetMethodTable())
                isValid = true;
            else if (!mt->IsCanonicalMethodTable())
                isValid = cls->GetMethodTable()->GetClass() == cls;
        }
        EX_CATCH
        {
            isValid = false;
        }
        EX_END_CATCH(SwallowAllExceptions)
    }

    return isValid;
}

bool DacDbiInterfaceImpl::GetAppDomainForObject(CORDB_ADDRESS addr, OUT VMPTR_AppDomain * pAppDomain,
                                                OUT VMPTR_Module *pModule, OUT VMPTR_DomainAssembly *pDomainAssembly)
{
    DD_ENTER_MAY_THROW;

    if (addr == 0 || addr == (CORDB_ADDRESS)-1)
    {
        return false;
    }

    PTR_Object obj(TO_TADDR(addr));
    MethodTable *mt = obj->GetMethodTable();

    PTR_Module module = mt->GetModule();
    PTR_Assembly assembly = module->GetAssembly();
    BaseDomain *baseDomain = assembly->GetDomain();

    if (baseDomain->IsAppDomain())
    {
        pAppDomain->SetDacTargetPtr(PTR_HOST_TO_TADDR(baseDomain->AsAppDomain()));
        pModule->SetDacTargetPtr(PTR_HOST_TO_TADDR(module));
        pDomainAssembly->SetDacTargetPtr(PTR_HOST_TO_TADDR(module->GetDomainAssembly()));
    }
    else
    {
        return false;
    }

    return true;
}

HRESULT DacDbiInterfaceImpl::CreateRefWalk(OUT RefWalkHandle * pHandle, BOOL walkStacks, BOOL walkFQ, UINT32 handleWalkMask)
{
    DD_ENTER_MAY_THROW;

    DacRefWalker *walker = new (nothrow) DacRefWalker(this, walkStacks, walkFQ, handleWalkMask, TRUE);

    if (walker == NULL)
        return E_OUTOFMEMORY;

    HRESULT hr = walker->Init();
    if (FAILED(hr))
    {
        delete walker;
    }
    else
    {
        *pHandle = reinterpret_cast<RefWalkHandle>(walker);
    }

    return hr;
}


void DacDbiInterfaceImpl::DeleteRefWalk(IN RefWalkHandle handle)
{
    DD_ENTER_MAY_THROW;

    DacRefWalker *walker = reinterpret_cast<DacRefWalker*>(handle);

    if (walker)
        delete walker;
}


HRESULT DacDbiInterfaceImpl::WalkRefs(RefWalkHandle handle, ULONG count, OUT DacGcReference * objects, OUT ULONG *pFetched)
{
    if (objects == NULL || pFetched == NULL)
        return E_POINTER;

    DD_ENTER_MAY_THROW;

    DacRefWalker *walker = reinterpret_cast<DacRefWalker*>(handle);
    if (!walker)
        return E_INVALIDARG;

   return walker->Next(count, objects, pFetched);
}

HRESULT DacDbiInterfaceImpl::GetTypeID(CORDB_ADDRESS dbgObj, COR_TYPEID *pID)
{
    DD_ENTER_MAY_THROW;

    TADDR obj[3];
    ULONG32 read = 0;
    HRESULT hr = g_dacImpl->m_pTarget->ReadVirtual(dbgObj, (BYTE*)obj, sizeof(obj), &read);
    if (FAILED(hr))
        return hr;

    pID->token1 = (UINT64)(obj[0] & ~1);
    pID->token2 = 0;

    return hr;
}

HRESULT DacDbiInterfaceImpl::GetTypeIDForType(VMPTR_TypeHandle vmTypeHandle, COR_TYPEID *pID)
{
    DD_ENTER_MAY_THROW;

    _ASSERTE(pID != NULL);
    _ASSERTE(!vmTypeHandle.IsNull());

    TypeHandle th = TypeHandle::FromPtr(vmTypeHandle.GetDacPtr());
    PTR_MethodTable pMT = th.GetMethodTable();
    pID->token1 = pMT.GetAddr();
    _ASSERTE(pID->token1 != 0);
    pID->token2 = 0;
    return S_OK;
}

HRESULT DacDbiInterfaceImpl::GetObjectFields(COR_TYPEID id, ULONG32 celt, COR_FIELD *layout, ULONG32 *pceltFetched)
{
    if (pceltFetched == NULL)
        return E_POINTER;

    if (id.token1 == 0)
        return CORDBG_E_CLASS_NOT_LOADED;

    DD_ENTER_MAY_THROW;

    HRESULT hr = S_OK;

    TypeHandle typeHandle = TypeHandle::FromPtr(TO_TADDR(id.token1));

    if (typeHandle.IsTypeDesc())
        return E_INVALIDARG;

    ApproxFieldDescIterator fieldDescIterator(typeHandle.AsMethodTable(), ApproxFieldDescIterator::INSTANCE_FIELDS);

    ULONG32 cFields = fieldDescIterator.Count();

    // Handle case where user only wanted to know the number of fields.
    if (layout == NULL)
    {
        *pceltFetched = cFields;
        return S_FALSE;
    }

    if (celt < cFields)
    {
        cFields = celt;

        // we are returning less than the total
        hr = S_FALSE;
    }

    // This must be non-null due to check at beginning of function.
    *pceltFetched = celt;

    CorElementType componentType = typeHandle.AsMethodTable()->GetInternalCorElementType();
    BOOL fReferenceType = CorTypeInfo::IsObjRef_NoThrow(componentType);
    for (ULONG32 i = 0; i < cFields; ++i)
    {
        FieldDesc *pField = fieldDescIterator.Next();

        COR_FIELD* corField = layout + i;
        corField->token = pField->GetMemberDef();
        corField->offset = (ULONG32)pField->GetOffset() + (fReferenceType ? Object::GetOffsetOfFirstField() : 0);

        TypeHandle fieldHandle = pField->LookupFieldTypeHandle();

        if (fieldHandle.IsNull())
        {
            corField->id = {};
            corField->fieldType = (CorElementType)0;
        }
        else if (fieldHandle.IsByRef())
        {
            corField->fieldType = ELEMENT_TYPE_BYREF;
            // All ByRefs intentionally return IntPtr's MethodTable.
            corField->id.token1 = CoreLibBinder::GetElementType(ELEMENT_TYPE_I).GetAddr();
            corField->id.token2 = 0;
        }
        else
        {
            // Note that pointer types are handled in this path.
            // IntPtr's MethodTable is set for all pointer types and is expected.
            PTR_MethodTable mt = fieldHandle.GetMethodTable();
            corField->fieldType = mt->GetInternalCorElementType();
            corField->id.token1 = (ULONG64)mt.GetAddr();
            corField->id.token2 = 0;
        }
    }

    return hr;
}


HRESULT DacDbiInterfaceImpl::GetTypeLayout(COR_TYPEID id, COR_TYPE_LAYOUT *pLayout)
{
    if (pLayout == NULL)
        return E_POINTER;

    if (id.token1 == 0)
        return CORDBG_E_CLASS_NOT_LOADED;

    DD_ENTER_MAY_THROW;

    PTR_MethodTable mt = PTR_MethodTable(TO_TADDR(id.token1));
    PTR_MethodTable parentMT = mt->GetParentMethodTable();

    COR_TYPEID parent = {parentMT.GetAddr(), 0};
    pLayout->parentID = parent;

    DWORD size = mt->GetBaseSize();
    ApproxFieldDescIterator fieldDescIterator(mt, ApproxFieldDescIterator::INSTANCE_FIELDS);

    pLayout->objectSize = size;
    pLayout->numFields = fieldDescIterator.Count();

    // Get type
    CorElementType componentType = mt->IsString() ? ELEMENT_TYPE_STRING : mt->GetInternalCorElementType();
    pLayout->type = componentType;
    pLayout->boxOffset = CorTypeInfo::IsObjRef_NoThrow(componentType) ? 0 : sizeof(TADDR);

    return S_OK;
}

HRESULT DacDbiInterfaceImpl::GetArrayLayout(COR_TYPEID id, COR_ARRAY_LAYOUT *pLayout)
{
    if (pLayout == NULL)
        return E_POINTER;

    if (id.token1 == 0)
        return CORDBG_E_CLASS_NOT_LOADED;

    DD_ENTER_MAY_THROW;

    PTR_MethodTable mt = PTR_MethodTable(TO_TADDR(id.token1));

    if (!mt->IsStringOrArray())
        return E_INVALIDARG;

    if (mt->IsString())
    {
        COR_TYPEID token;
        token.token1 = CoreLibBinder::GetElementType(ELEMENT_TYPE_CHAR).GetAddr();
        token.token2 = 0;

        pLayout->componentID = token;

        pLayout->rankSize = 4;
        pLayout->numRanks = 1;
        pLayout->rankOffset = sizeof(TADDR);
        pLayout->firstElementOffset = sizeof(TADDR) + 4;
        pLayout->countOffset = sizeof(TADDR);
        pLayout->componentType = ELEMENT_TYPE_CHAR;
        pLayout->elementSize = 2;
    }
    else
    {
        DWORD ranks = mt->GetRank();
        pLayout->rankSize = 4;
        pLayout->numRanks = ranks;
        bool multiDim = (ranks > 1);

        pLayout->rankOffset = multiDim ? sizeof(TADDR)*2 : sizeof(TADDR);
        pLayout->countOffset = sizeof(TADDR);
        pLayout->firstElementOffset = ArrayBase::GetDataPtrOffset(mt);


        TypeHandle hnd = mt->GetArrayElementTypeHandle();
        PTR_MethodTable cmt = hnd.GetMethodTable();

        CorElementType componentType = cmt->GetInternalCorElementType();
        if ((UINT64)cmt.GetAddr() == (UINT64)g_pStringClass.GetAddr())
            componentType = ELEMENT_TYPE_STRING;

        COR_TYPEID token;
        token.token1 = cmt.GetAddr();  // This could be type handle
        token.token2 = 0;
        pLayout->componentID = token;
        pLayout->componentType = componentType;

        if (CorTypeInfo::IsObjRef_NoThrow(componentType))
            pLayout->elementSize = sizeof(TADDR);
        else if (CorIsPrimitiveType(componentType))
            pLayout->elementSize = gElementTypeInfo[componentType].m_cbSize;
        else
            pLayout->elementSize = cmt->GetNumInstanceFieldBytes();
    }

    return S_OK;
}


void DacDbiInterfaceImpl::GetGCHeapInformation(COR_HEAPINFO * pHeapInfo)
{
    DD_ENTER_MAY_THROW;

    size_t heapCount = 0;
    pHeapInfo->areGCStructuresValid = *g_gcDacGlobals->gc_structures_invalid_cnt == 0;

#ifdef FEATURE_SVR_GC
    if (GCHeapUtilities::IsServerHeap())
    {
        pHeapInfo->gcType = CorDebugServerGC;
        pHeapInfo->numHeaps = DacGetNumHeaps();
    }
    else
#endif
    {
        pHeapInfo->gcType = CorDebugWorkstationGC;
        pHeapInfo->numHeaps = 1;
    }

    pHeapInfo->pointerSize = sizeof(TADDR);
    pHeapInfo->concurrent = g_pConfig->GetGCconcurrent() ? TRUE : FALSE;
}


HRESULT DacDbiInterfaceImpl::GetPEFileMDInternalRW(VMPTR_PEAssembly vmPEAssembly, OUT TADDR* pAddrMDInternalRW)
{
    DD_ENTER_MAY_THROW;
    if (pAddrMDInternalRW == NULL)
        return E_INVALIDARG;
    PEAssembly * pPEAssembly = vmPEAssembly.GetDacPtr();
    *pAddrMDInternalRW = pPEAssembly->GetMDInternalRWAddress();
    return S_OK;
}

HRESULT DacDbiInterfaceImpl::GetReJitInfo(VMPTR_Module vmModule, mdMethodDef methodTk, OUT VMPTR_ReJitInfo* pvmReJitInfo)
{
    DD_ENTER_MAY_THROW;
    _ASSERTE(!"You shouldn't be calling this - use GetActiveRejitILCodeVersionNode instead");
    return S_OK;
}

HRESULT DacDbiInterfaceImpl::GetActiveRejitILCodeVersionNode(VMPTR_Module vmModule, mdMethodDef methodTk, OUT VMPTR_ILCodeVersionNode* pVmILCodeVersionNode)
{
    DD_ENTER_MAY_THROW;
    if (pVmILCodeVersionNode == NULL)
        return E_INVALIDARG;
#ifdef FEATURE_REJIT
    PTR_Module pModule = vmModule.GetDacPtr();
    CodeVersionManager * pCodeVersionManager = pModule->GetCodeVersionManager();
    // Be careful, there are two different definitions of 'active' being used here
    // For the CodeVersionManager, the active IL version is whatever one should be used in the next invocation of the method
    // 'rejit active' narrows that to only include rejit IL bodies where the profiler has already provided the definition
    // for the new IL (ilCodeVersion.GetRejitState()==ILCodeVersion::kStateActive). It is possible that the code version
    // manager's active IL version hasn't yet asked the profiler for the IL body to use, in which case we want to filter it
    //  out from the return in this method.
    ILCodeVersion activeILVersion = pCodeVersionManager->GetActiveILCodeVersion(pModule, methodTk);
    if (activeILVersion.IsNull() || activeILVersion.IsDefaultVersion() || activeILVersion.GetRejitState() != ILCodeVersion::kStateActive)
    {
        pVmILCodeVersionNode->SetDacTargetPtr(0);
    }
    else
    {
        pVmILCodeVersionNode->SetDacTargetPtr(PTR_TO_TADDR(activeILVersion.AsNode()));
    }
#else
    _ASSERTE(!"You shouldn't be calling this - rejit is not supported in this build");
    pVmILCodeVersionNode->SetDacTargetPtr(0);
#endif
    return S_OK;
}

HRESULT DacDbiInterfaceImpl::GetReJitInfo(VMPTR_MethodDesc vmMethod, CORDB_ADDRESS codeStartAddress, OUT VMPTR_ReJitInfo* pvmReJitInfo)
{
    DD_ENTER_MAY_THROW;
    _ASSERTE(!"You shouldn't be calling this - use GetNativeCodeVersionNode instead");
    return S_OK;
}

HRESULT DacDbiInterfaceImpl::AreOptimizationsDisabled(VMPTR_Module vmModule, mdMethodDef methodTk, OUT BOOL* pOptimizationsDisabled)
{
    DD_ENTER_MAY_THROW;
#ifdef FEATURE_REJIT
    PTR_Module pModule = vmModule.GetDacPtr();
    if (pModule == NULL || pOptimizationsDisabled == NULL || TypeFromToken(methodTk) != mdtMethodDef)
    {
        return E_INVALIDARG;
    }
    {
        CodeVersionManager * pCodeVersionManager = pModule->GetCodeVersionManager();
        ILCodeVersion activeILVersion = pCodeVersionManager->GetActiveILCodeVersion(pModule, methodTk);
        *pOptimizationsDisabled = activeILVersion.IsDeoptimized();
    }
#else
    pOptimizationsDisabled->SetDacTargetPtr(0);
#endif

    return S_OK;
}

HRESULT DacDbiInterfaceImpl::GetNativeCodeVersionNode(VMPTR_MethodDesc vmMethod, CORDB_ADDRESS codeStartAddress, OUT VMPTR_NativeCodeVersionNode* pVmNativeCodeVersionNode)
{
    DD_ENTER_MAY_THROW;
    if (pVmNativeCodeVersionNode == NULL)
        return E_INVALIDARG;
#ifdef FEATURE_REJIT
    PTR_MethodDesc pMD = vmMethod.GetDacPtr();
    CodeVersionManager * pCodeVersionManager = pMD->GetCodeVersionManager();
    NativeCodeVersion codeVersion = pCodeVersionManager->GetNativeCodeVersion(pMD, (PCODE)codeStartAddress);
    pVmNativeCodeVersionNode->SetDacTargetPtr(PTR_TO_TADDR(codeVersion.AsNode()));
#else
    pVmNativeCodeVersionNode->SetDacTargetPtr(0);
#endif
    return S_OK;
}

HRESULT DacDbiInterfaceImpl::GetSharedReJitInfo(VMPTR_ReJitInfo vmReJitInfo, OUT VMPTR_SharedReJitInfo* pvmSharedReJitInfo)
{
    DD_ENTER_MAY_THROW;
    _ASSERTE(!"You shouldn't be calling this - use GetLCodeVersionNode instead");
    return S_OK;
}

HRESULT DacDbiInterfaceImpl::GetILCodeVersionNode(VMPTR_NativeCodeVersionNode vmNativeCodeVersionNode, VMPTR_ILCodeVersionNode* pVmILCodeVersionNode)
{
    DD_ENTER_MAY_THROW;
    if (pVmILCodeVersionNode == NULL)
        return E_INVALIDARG;
#ifdef FEATURE_REJIT
    NativeCodeVersionNode* pNativeCodeVersionNode = vmNativeCodeVersionNode.GetDacPtr();
    ILCodeVersion ilCodeVersion = pNativeCodeVersionNode->GetILCodeVersion();
    if (ilCodeVersion.IsDefaultVersion())
    {
        pVmILCodeVersionNode->SetDacTargetPtr(0);
    }
    else
    {
        pVmILCodeVersionNode->SetDacTargetPtr(PTR_TO_TADDR(ilCodeVersion.AsNode()));
    }

#else
    _ASSERTE(!"You shouldn't be calling this - rejit is not supported in this build");
    pVmILCodeVersionNode->SetDacTargetPtr(0);
#endif
    return S_OK;
}

HRESULT DacDbiInterfaceImpl::GetSharedReJitInfoData(VMPTR_SharedReJitInfo vmSharedReJitInfo, DacSharedReJitInfo* pData)
{
    DD_ENTER_MAY_THROW;
    _ASSERTE(!"You shouldn't be calling this - use GetILCodeVersionNodeData instead");
    return S_OK;
}

HRESULT DacDbiInterfaceImpl::GetILCodeVersionNodeData(VMPTR_ILCodeVersionNode vmILCodeVersionNode, DacSharedReJitInfo* pData)
{
    DD_ENTER_MAY_THROW;
#ifdef FEATURE_REJIT
    ILCodeVersion ilCode(vmILCodeVersionNode.GetDacPtr());
    pData->m_state = ilCode.GetRejitState();
    pData->m_pbIL = PTR_TO_CORDB_ADDRESS(dac_cast<ULONG_PTR>(ilCode.GetIL()));
    pData->m_dwCodegenFlags = ilCode.GetJitFlags();
    const InstrumentedILOffsetMapping* pMapping = ilCode.GetInstrumentedILMap();
    if (pMapping)
    {
        pData->m_cInstrumentedMapEntries = (ULONG)pMapping->GetCount();
        pData->m_rgInstrumentedMapEntries = PTR_TO_CORDB_ADDRESS(dac_cast<ULONG_PTR>(pMapping->GetOffsets()));
    }
    else
    {
        pData->m_cInstrumentedMapEntries = 0;
        pData->m_rgInstrumentedMapEntries = 0;
    }
#else
    _ASSERTE(!"You shouldn't be calling this - rejit isn't supported in this build");
#endif
    return S_OK;
}

HRESULT DacDbiInterfaceImpl::GetDefinesBitField(ULONG32 *pDefines)
{
    DD_ENTER_MAY_THROW;
    if (pDefines == NULL)
        return E_INVALIDARG;
    *pDefines = g_pDebugger->m_defines;
    return S_OK;
}

HRESULT DacDbiInterfaceImpl::GetMDStructuresVersion(ULONG32* pMDStructuresVersion)
{
    DD_ENTER_MAY_THROW;
    if (pMDStructuresVersion == NULL)
        return E_INVALIDARG;
    *pMDStructuresVersion = g_pDebugger->m_mdDataStructureVersion;
    return S_OK;
}

HRESULT DacDbiInterfaceImpl::EnableGCNotificationEvents(BOOL fEnable)
{
    DD_ENTER_MAY_THROW

    HRESULT hr = S_OK;
    EX_TRY
    {
        if (g_pDebugger != NULL)
        {
            TADDR addr = PTR_HOST_MEMBER_TADDR(Debugger, g_pDebugger, m_isGarbageCollectionEventsEnabled);
            SafeWriteStructOrThrow<BOOL>(addr, &fEnable);
        }
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

DacRefWalker::DacRefWalker(ClrDataAccess *dac, BOOL walkStacks, BOOL walkFQ, UINT32 handleMask, BOOL resolvePointers)
    : mDac(dac), mWalkStacks(walkStacks), mWalkFQ(walkFQ), mHandleMask(handleMask), mStackWalker(NULL),
      mResolvePointers(resolvePointers), mHandleWalker(NULL), mFQStart(PTR_NULL), mFQEnd(PTR_NULL), mFQCurr(PTR_NULL)
{
}

DacRefWalker::~DacRefWalker()
{
    Clear();
}

HRESULT DacRefWalker::Init()
{
    HRESULT hr = S_OK;
    if (mHandleMask)
    {
        // Will throw on OOM, which is fine.
        mHandleWalker = new DacHandleWalker();

        hr = mHandleWalker->Init(GetHandleWalkerMask());
    }

    if (mWalkStacks && SUCCEEDED(hr))
    {
        hr = NextThread();
    }

    return hr;
}

void DacRefWalker::Clear()
{
    if (mHandleWalker)
    {
        delete mHandleWalker;
        mHandleWalker = NULL;
    }

    if (mStackWalker)
    {
        delete mStackWalker;
        mStackWalker = NULL;
    }
}



UINT32 DacRefWalker::GetHandleWalkerMask()
{
    UINT32 result = 0;
    if (mHandleMask & CorHandleStrong)
        result |= (1 << HNDTYPE_STRONG);

    if (mHandleMask & CorHandleStrongPinning)
        result |= (1 << HNDTYPE_PINNED);

    if (mHandleMask & CorHandleWeakShort)
        result |= (1 << HNDTYPE_WEAK_SHORT);

    if (mHandleMask & CorHandleWeakLong)
        result |= (1 << HNDTYPE_WEAK_LONG);

#if defined(FEATURE_COMINTEROP) || defined(FEATURE_COMWRAPPERS) || defined(FEATURE_OBJCMARSHAL)
    if ((mHandleMask & CorHandleWeakRefCount) || (mHandleMask & CorHandleStrongRefCount))
        result |= (1 << HNDTYPE_REFCOUNTED);
#endif // FEATURE_COMINTEROP || FEATURE_COMWRAPPERS || FEATURE_OBJCMARSHAL

    if (mHandleMask & CorHandleStrongDependent)
        result |= (1 << HNDTYPE_DEPENDENT);

    if (mHandleMask & CorHandleStrongSizedByref)
        result |= (1 << HNDTYPE_SIZEDREF);

    return result;
}



HRESULT DacRefWalker::Next(ULONG celt, DacGcReference roots[], ULONG *pceltFetched)
{
    if (roots == NULL || pceltFetched == NULL)
        return E_POINTER;

    ULONG total = 0;
    HRESULT hr = S_OK;

    if (mHandleWalker)
    {
        hr = mHandleWalker->Next(celt, roots, &total);

        if (total == 0 || FAILED(hr))
        {
            delete mHandleWalker;
            mHandleWalker = NULL;

            if (FAILED(hr))
                return hr;
        }
    }

    if (total < celt)
    {
        while (total < celt && mFQCurr < mFQEnd)
        {
            DacGcReference &ref = roots[total++];

            ref.vmDomain = VMPTR_AppDomain::NullPtr();
            ref.objHnd.SetDacTargetPtr(mFQCurr.GetAddr());
            ref.dwType = (DWORD)CorReferenceFinalizer;
            ref.i64ExtraData = 0;

            mFQCurr++;
        }
    }

    while (total < celt && mStackWalker)
    {
        ULONG fetched = 0;
        hr = mStackWalker->Next(celt-total, roots+total, &fetched);

        if (FAILED(hr))
            return hr;

        if (fetched == 0)
        {
            hr = NextThread();

            if (FAILED(hr))
                return hr;
        }

        total += fetched;
    }

    *pceltFetched = total;

    return total < celt ? S_FALSE : S_OK;
}

HRESULT DacRefWalker::NextThread()
{
    Thread *pThread = NULL;
    if (mStackWalker)
    {
        pThread = mStackWalker->GetThread();
        delete mStackWalker;
        mStackWalker = NULL;
    }

    pThread = ThreadStore::GetThreadList(pThread);

    if (!pThread)
        return S_FALSE;

    mStackWalker = new DacStackReferenceWalker(mDac, pThread->GetOSThreadId(), mResolvePointers == TRUE);
    return mStackWalker->Init();
}

HRESULT DacHandleWalker::Next(ULONG count, DacGcReference roots[], ULONG *pFetched)
{
    SUPPORTS_DAC;

    if (roots == NULL || pFetched == NULL)
        return E_POINTER;

    if (!mEnumerated)
        WalkHandles();

    unsigned int i;
    for (i = 0; i < count && mIteratorIndex < mList.GetCount(); mIteratorIndex++, i++)
    {
        const SOSHandleData &handle = mList.Get(mIteratorIndex);

        roots[i].objHnd.SetDacTargetPtr(TO_TADDR(handle.Handle));
        roots[i].vmDomain.SetDacTargetPtr(TO_TADDR(handle.AppDomain));
        roots[i].i64ExtraData = 0;

        unsigned int refCnt = 0;
        switch (handle.Type)
        {
            case HNDTYPE_STRONG:
                roots[i].dwType = (DWORD)CorHandleStrong;
                break;

            case HNDTYPE_PINNED:
                roots[i].dwType = (DWORD)CorHandleStrongPinning;
                break;

            case HNDTYPE_WEAK_SHORT:
                roots[i].dwType = (DWORD)CorHandleWeakShort;
                break;

            case HNDTYPE_WEAK_LONG:
                roots[i].dwType = (DWORD)CorHandleWeakLong;
                break;

        #if defined(FEATURE_COMINTEROP) || defined(FEATURE_COMWRAPPERS) || defined(FEATURE_OBJCMARSHAL)
            case HNDTYPE_REFCOUNTED:
                GetRefCountedHandleInfo((OBJECTREF)CLRDATA_ADDRESS_TO_TADDR(handle.Handle), handle.Type, &refCnt, NULL, NULL, NULL);
                roots[i].i64ExtraData = refCnt;
                roots[i].dwType = (DWORD)(roots[i].i64ExtraData ? CorHandleStrongRefCount : CorHandleWeakRefCount);
                break;
        #endif // FEATURE_COMINTEROP || FEATURE_COMWRAPPERS || FEATURE_OBJCMARSHAL

            case HNDTYPE_DEPENDENT:
                roots[i].dwType = (DWORD)CorHandleStrongDependent;
                roots[i].i64ExtraData = GetDependentHandleSecondary(CLRDATA_ADDRESS_TO_TADDR(handle.Handle)).GetAddr();
                break;

            case HNDTYPE_SIZEDREF:
                roots[i].dwType = (DWORD)CorHandleStrongSizedByref;
                break;
        }
    }

    *pFetched = i;

    return (unsigned)mIteratorIndex < mList.GetCount() ? S_FALSE : S_OK;
}


HRESULT DacStackReferenceWalker::Next(ULONG count, DacGcReference stackRefs[], ULONG *pFetched)
{
    if (stackRefs == NULL || pFetched == NULL)
        return E_POINTER;

    if (!mEnumerated)
        WalkStack();

    TADDR domain = AppDomain::GetCurrentDomain().GetAddr();

    unsigned int i;
    for (i = 0; i < count && mIteratorIndex < mList.GetCount(); mIteratorIndex++, i++)
    {
        stackRefs[i].dwType = CorReferenceStack;
        stackRefs[i].vmDomain.SetDacTargetPtr(domain);
        stackRefs[i].i64ExtraData = 0;

        const SOSStackRefData &sosStackRef = mList.Get(i);
        if (sosStackRef.Flags & GC_CALL_INTERIOR || sosStackRef.Address == 0)
        {
            // Direct pointer case - interior pointer, Frame ref, or enregistered var.
            stackRefs[i].pObject = CLRDATA_ADDRESS_TO_TADDR(sosStackRef.Object) | 1;
        }
        else
        {
            stackRefs[i].objHnd.SetDacTargetPtr(CLRDATA_ADDRESS_TO_TADDR(sosStackRef.Address));
        }
    }

    *pFetched = i;

    return S_OK;
}
