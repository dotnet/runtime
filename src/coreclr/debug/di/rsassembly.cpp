// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: RsAssembly.cpp
//

//
//*****************************************************************************
#include "stdafx.h"
#include "primitives.h"
#include "safewrap.h"

#include "check.h"

#include <tlhelp32.h>
#include "wtsapi32.h"

#ifndef SM_REMOTESESSION
#define SM_REMOTESESSION 0x1000
#endif

#include "corpriv.h"
#include "../../dlls/mscorrc/resource.h"
#include <limits.h>


/* ------------------------------------------------------------------------- *
 * Assembly class
 * ------------------------------------------------------------------------- */
CordbAssembly::CordbAssembly(CordbAppDomain *       pAppDomain,
                             VMPTR_Assembly         vmAssembly,
                             VMPTR_DomainAssembly   vmDomainAssembly)

    : CordbBase(pAppDomain->GetProcess(),
                vmDomainAssembly.IsNull() ? VmPtrToCookie(vmAssembly) : VmPtrToCookie(vmDomainAssembly),
                enumCordbAssembly),
      m_vmAssembly(vmAssembly),
      m_vmDomainAssembly(vmDomainAssembly),
      m_pAppDomain(pAppDomain)
{
    _ASSERTE(!vmAssembly.IsNull());
}

/*
    A list of which resources owned by this object are accounted for.

    public:
        CordbAppDomain      *m_pAppDomain; // Assigned w/o addRef(), Deleted in ~CordbAssembly
*/

CordbAssembly::~CordbAssembly()
{
}

HRESULT CordbAssembly::QueryInterface(REFIID id, void **ppInterface)
{
    if (id == IID_ICorDebugAssembly)
        *ppInterface = static_cast<ICorDebugAssembly*>(this);
    else if (id == IID_ICorDebugAssembly2)
        *ppInterface = static_cast<ICorDebugAssembly2*>(this);
    else if (id == IID_IUnknown)
        *ppInterface = static_cast<IUnknown*>( static_cast<ICorDebugAssembly*>(this) );
    else
    {
        *ppInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
}

// Neutered by AppDomain
void CordbAssembly::Neuter()
{
    m_pAppDomain = NULL;
    CordbBase::Neuter();
}


#ifdef _DEBUG
//---------------------------------------------------------------------------------------
// Callback helper for code:CordbAssembly::DbgAssertAssemblyDeleted
//
// Arguments
//    vmDomainAssembly - domain file in the enumeration
//    pUserData - pointer to the CordbAssembly that we just got an exit event for.
//

// static
void CordbAssembly::DbgAssertAssemblyDeletedCallback(VMPTR_DomainAssembly vmDomainAssembly, void * pUserData)
{
    CordbAssembly * pThis = reinterpret_cast<CordbAssembly * >(pUserData);
    INTERNAL_DAC_CALLBACK(pThis->GetProcess());

    VMPTR_DomainAssembly vmAssemblyDeleted = pThis->m_vmDomainAssembly;

    CONSISTENCY_CHECK_MSGF((vmAssemblyDeleted != vmDomainAssembly),
        ("An Assembly Unload event was sent, but the assembly still shows up in the enumeration.\n vmAssemblyDeleted=%p\n",
        VmPtrToCookie(vmAssemblyDeleted)));
}

//---------------------------------------------------------------------------------------
// Assert that a assembly is no longer discoverable via enumeration.
//
// Notes:
//   See code:IDacDbiInterface#Enumeration for rules that we're asserting.
//   This is a debug only method. It's conceptually similar to
//   code:CordbProcess::DbgAssertAppDomainDeleted.
//
void CordbAssembly::DbgAssertAssemblyDeleted()
{
    GetProcess()->GetDAC()->EnumerateAssembliesInAppDomain(
        GetAppDomain()->GetADToken(),
        CordbAssembly::DbgAssertAssemblyDeletedCallback,
        this);
}
#endif // _DEBUG

/*
 * GetProcess returns the process containing the assembly
 */
HRESULT CordbAssembly::GetProcess(ICorDebugProcess **ppProcess)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppProcess, ICorDebugProcess **);

    return (m_pAppDomain->GetProcess (ppProcess));
}

//
// Returns the AppDomain that this assembly belongs to.
//
// Arguments:
//    ppAppDomain - a non-NULL pointer to store the AppDomain in.
//
// Return Value:
//    S_OK
//
// Notes:
//   On the debugger right-side we currently consider every assembly to belong
//   to a single AppDomain, and create multiple CordbAssembly instances (one
//   per AppDomain) to represent domain-neutral assemblies.
//
HRESULT CordbAssembly::GetAppDomain(ICorDebugAppDomain **ppAppDomain)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppAppDomain, ICorDebugAppDomain **);

    _ASSERTE(m_pAppDomain != NULL);

    *ppAppDomain = static_cast<ICorDebugAppDomain *> (m_pAppDomain);
    m_pAppDomain->ExternalAddRef();

    return S_OK;
}



/*
 * EnumerateModules enumerates all modules in the assembly
 */
HRESULT CordbAssembly::EnumerateModules(ICorDebugModuleEnum **ppModules)
{
    HRESULT hr = S_OK;
    PUBLIC_API_BEGIN(this);
    {
        ValidateOrThrow(ppModules);
        *ppModules = NULL;

        m_pAppDomain->PrepopulateModules();

        RSInitHolder<CordbEnumFilter> pModEnum(
            new CordbEnumFilter(GetProcess(), GetProcess()->GetContinueNeuterList()));

        RSInitHolder<CordbHashTableEnum> pEnum;

        CordbHashTableEnum::BuildOrThrow(
            this,
            NULL,  // ownership
            &m_pAppDomain->m_modules,
            IID_ICorDebugModuleEnum,
            pEnum.GetAddr());

        // this will build up an auxillary list. Don't need pEnum after this.
        hr = pModEnum->Init(pEnum, this);
        IfFailThrow(hr);

        pModEnum.TransferOwnershipExternal(ppModules);

    }
    PUBLIC_API_END(hr);

    return hr;
}


/*
 * GetCodeBase returns the code base used to load the assembly
 */
HRESULT CordbAssembly::GetCodeBase(ULONG32 cchName,
                    ULONG32 *pcchName,
                    _Out_writes_to_opt_(cchName, *pcchName) WCHAR szName[])
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT_ARRAY(szName, WCHAR, cchName, true, true);
    VALIDATE_POINTER_TO_OBJECT_OR_NULL(pcchName, ULONG32 *);

    return E_NOTIMPL;
}

//
// Gets the filename of the assembly
//
// Arguments:
//   cchName - number of characters available in szName, or 0 to query length
//   pcchName - optional pointer to store the real length of the filename
//   szName - buffer in which to copy the filename, or NULL if cchName is 0.
//
// Return value:
//   S_OK on success (even if there is no filename).
//   An error code if the filename could not be read for the assembly.  This should
//   not happen unless the target is corrupt.
//
// Notes:
//   In-memory assemblies do not have a filename.  In that case, for compatibility
//   this returns success and the string "<unknown>".  We may want to change this
//   behavior in the future.
//
HRESULT CordbAssembly::GetName(ULONG32 cchName,
                               ULONG32 *pcchName,
                               _Out_writes_to_opt_(cchName, *pcchName) WCHAR szName[])
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT_ARRAY_OR_NULL(szName, WCHAR, cchName, true, true);
    VALIDATE_POINTER_TO_OBJECT_OR_NULL(pcchName, ULONG32 *);

    HRESULT hr = S_OK;
    EX_TRY
    {
        // Lazily initialize our cache of the assembly filename.
        // Note that if this fails, we'll try again next time this is called.
        // This can be convenient for transient errors and debugging purposes, but could cause a
        // performance problem if failure was common (it should not be).
        if (!m_strAssemblyFileName.IsSet())
        {
            IDacDbiInterface * pDac = m_pProcess->GetDAC(); // throws
            BOOL fNonEmpty = pDac->GetAssemblyPath(m_vmAssembly, &m_strAssemblyFileName); // throws
            _ASSERTE(m_strAssemblyFileName.IsSet());


            if (!fNonEmpty)
            {
                // File name is empty (eg. for an in-memory assembly)
                _ASSERTE(m_strAssemblyFileName.IsEmpty());

                // Construct a fake name
                // This seems unwise - the assembly doesn't have a filename, we should probably just return
                // an empty string and S_FALSE.  This is a common case (in-memory assemblies), I don't see any reason to
                // fake up a filename to pretend that it has a disk location when it doesn't.
                // But I don't want to break tests at the moment that expect this.
                // Note that all assemblies have a simple metadata name - perhaps we should have an additional API for that.
                m_strAssemblyFileName.AssignCopy(W("<unknown>"));
            }
        }

        // We should now have a non-empty string
        _ASSERTE(m_strAssemblyFileName.IsSet());
        _ASSERTE(!m_strAssemblyFileName.IsEmpty());

        // Copy it out to our caller
    }
    EX_CATCH_HRESULT(hr);
    if (FAILED(hr))
    {
        return hr;
    }
    return CopyOutString(m_strAssemblyFileName, cchName, pcchName, szName);
}

HRESULT CordbAssembly::IsFullyTrusted( BOOL *pbFullyTrusted )
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());
    VALIDATE_POINTER_TO_OBJECT(pbFullyTrusted, BOOL*);

    if (m_vmDomainAssembly.IsNull())
        return E_UNEXPECTED;

    // Check for cached result
    if( m_foptIsFullTrust.HasValue() )
    {
        *pbFullyTrusted = m_foptIsFullTrust.GetValue();
        return S_OK;
    }

    HRESULT hr = S_OK;
    EX_TRY
    {

        CordbProcess * pProcess = m_pAppDomain->GetProcess();
        IDacDbiInterface * pDac = pProcess->GetDAC();

        BOOL fIsFullTrust = pDac->IsAssemblyFullyTrusted(m_vmDomainAssembly);

        // Once the trust level of an assembly is known, it cannot change.
        m_foptIsFullTrust = fIsFullTrust;

        *pbFullyTrusted = fIsFullTrust;
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

