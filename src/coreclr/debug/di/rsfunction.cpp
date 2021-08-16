// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: rsfunction.cpp
//

//
//*****************************************************************************
#include "stdafx.h"

// We have an assert in ceemain.cpp that validates this assumption
#define FIELD_OFFSET_NEW_ENC_DB          0x07FFFFFB

#include "winbase.h"
#include "corpriv.h"

/* ------------------------------------------------------------------------- *
 * Function class
 * ------------------------------------------------------------------------- */

//-----------------------------------------------------------------------------
// Constructor for CordbFunction class.
// This represents an IL Function in the debuggee.
// CordbFunction is 1:1 with IL method bodies.
//
// Parameters:
//   m - module containing this function. All functions live in a single module.
//   funcMetadataToken - the metadata token for this function (scoped to the module).
//   enCVersion - Enc Version number of this function (in sync with the module's
//     EnC version). Each edit to a function means a whole new IL method body,
//     and since CordbFunction is 1:1 with IL, that means a new CordbFunction instance.
//-----------------------------------------------------------------------------
CordbFunction::CordbFunction(CordbModule * m,
                             mdMethodDef funcMetadataToken,
                             SIZE_T enCVersion)
  : CordbBase(m->GetProcess(), funcMetadataToken, enumCordbFunction), m_pModule(m), m_pClass(NULL),
    m_pILCode(NULL),
    m_nativeCode(NULL),
    m_MDToken(funcMetadataToken),
    m_dwEnCVersionNumber(enCVersion),
    m_pPrevVersion(NULL),
    m_fIsNativeImpl(kUnknownImpl),
    m_fCachedMethodValuesValid(FALSE),
    m_argCountCached(0),
    m_fIsStaticCached(FALSE),
    m_reJitILCodes(1)
{
    m_methodSigParserCached = SigParser(NULL, 0);

    _ASSERTE(enCVersion >= CorDB_DEFAULT_ENC_FUNCTION_VERSION);
    _ASSERTE(TypeFromToken(m_MDToken) == mdtMethodDef);
}



/*
    A list of which resources owned by this object are accounted for.

    UNKNOWN:
        ICorDebugInfo::NativeVarInfo *m_nativeInfo;

    HANDLED:
        CordbModule             *m_module; // Assigned w/o AddRef()
        CordbClass              *m_class; // Assigned w/o AddRef()
*/

//-----------------------------------------------------------------------------
// CordbFunction destructor
// All external resources, including references counts, should have been
// released in Neuter(), so this should literally just delete memory or
// or check that the object is already dead.
//-----------------------------------------------------------------------------
CordbFunction::~CordbFunction()
{
    // We should have been explicitly neutered before our internal ref went to 0.
    _ASSERTE(IsNeutered());

    // Since we've been neutered, we shouldn't have any References to release and
    // our hash of JitInfos should be empty.
    _ASSERTE(m_pILCode == NULL);
    _ASSERTE(m_pPrevVersion == NULL);
}

//-----------------------------------------------------------------------------
// CordbFunction::Neuter
//    Neuter releases all of the resources this object holds. CordbFunction
//    lives in a CordbModule, so Module neuter will neuter this.
//    See CordbBase::Neuter for further semantics.
//
//-----------------------------------------------------------------------------
void CordbFunction::Neuter()
{
    // Neuter any/all CordbNativeCode & CordbILCode objects
    if (m_pILCode != NULL)
    {
        m_pILCode->Neuter();
        m_pILCode.Clear(); // this will internal release.
    }

    // Neuter & Release the Prev-Function list.
    if (m_pPrevVersion != NULL)
    {
        m_pPrevVersion->Neuter();
        m_pPrevVersion.Clear(); // this will internal release.
    }

    m_pModule = NULL;
    m_pClass = NULL;

    m_nativeCode.Clear();
    m_reJitILCodes.NeuterAndClear(GetProcess()->GetProcessLock());

    CordbBase::Neuter();
}

//-----------------------------------------------------------------------------
// CordbFunction::QueryInterface
// Public method to implement IUnknown::QueryInterface.
// Has standard QI semantics.
//-----------------------------------------------------------------------------
HRESULT CordbFunction::QueryInterface(REFIID id, void **pInterface)
{
    if (id == IID_ICorDebugFunction)
    {
        *pInterface = static_cast<ICorDebugFunction*>(this);
    }
    else if (id == IID_ICorDebugFunction2)
    {
        *pInterface = static_cast<ICorDebugFunction2*>(this);
    }
    else if (id == IID_ICorDebugFunction3)
    {
        *pInterface = static_cast<ICorDebugFunction3*>(this);
    }
    else if (id == IID_ICorDebugFunction4)
    {
        *pInterface = static_cast<ICorDebugFunction4*>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugFunction*>(this));
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
}

//-----------------------------------------------------------------------------
// CordbFunction::GetModule
// Public method (implements ICorDebugFunction::GetModule).
// Get the ICorDebugModule (external representation of a module) that this
// Function is contained in. All functions live in exactly 1 module.
// This is related to the 'CordbModule* GetModule()' method which returns the
// internal module representation for the containing module.
//
// Parameters:
//    ppModule - out parameter to hold module.
//
// Return values:
//    S_OK iff *ppModule is set.
//-----------------------------------------------------------------------------
HRESULT CordbFunction::GetModule(ICorDebugModule **ppModule)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppModule, ICorDebugModule **);

    HRESULT hr = S_OK;

    // Module is set on creation, so just return it.
    *ppModule = static_cast<ICorDebugModule*> (m_pModule);
    m_pModule->ExternalAddRef();

    return hr;
}

//-----------------------------------------------------------------------------
// CordbFunction::GetClass
// Public function to get ICorDebugClass that this function is in.
//
// Parameters:
//     ppClass - out parameter holding which class this function lives in.
//
// Return value:
//   S_OK iff *ppClass is set.
//-----------------------------------------------------------------------------
HRESULT CordbFunction::GetClass(ICorDebugClass **ppClass)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppClass, ICorDebugClass **);
    ATT_ALLOW_LIVE_DO_STOPGO(GetProcess());
    *ppClass = NULL;

    HRESULT hr = S_OK;

    if (m_pClass == NULL)
    {
        // We're not looking for any particular version, just
        // the class info.  This seems like the best version to request
        hr = InitParentClassOfFunction();

        if (FAILED(hr))
            goto LExit;
    }

    *ppClass = static_cast<ICorDebugClass*> (m_pClass);

LExit:
    if (FAILED(hr))
        return hr;

    if (*ppClass)
    {
        m_pClass->ExternalAddRef();
        return S_OK;
    }
    else
        return S_FALSE;
}

//-----------------------------------------------------------------------------
// CordbFunction::GetToken
// Public function to get the metadata token for this function.
// This is a MethodDef, which is scoped to a module.
//
// Parameters:
//   pMemberDef - out parameter to hold token.
//
// Return values:
//   S_OK if pMemberDef is set.
//-----------------------------------------------------------------------------
HRESULT CordbFunction::GetToken(mdMethodDef *pMemberDef)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pMemberDef, mdMethodDef *);


    // Token is set on creation, so no updating needed.
    CONSISTENCY_CHECK_MSGF((TypeFromToken(m_MDToken) == mdtMethodDef),
        ("CordbFunction token (%08x) is not a mdtMethodDef. This=%p", m_MDToken, this));

    *pMemberDef = m_MDToken;
    return S_OK;
}

//-----------------------------------------------------------------------------
// CordbFunction::GetILCode
//  Public function to get an ICorDebugCode object for the IL code in
//  this function.
//  If we EnC, we get a new ICorDebugFunction, so the IL code & function
//  should be 1:1.
//
// Parameters:
//   ppCode - out parameter to hold the code object.
//
// Return value:
//   S_OK iff *ppCode != NULL. Else error.
//-----------------------------------------------------------------------------
HRESULT CordbFunction::GetILCode(ICorDebugCode ** ppCode)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppCode, ICorDebugCode **);
    ATT_ALLOW_LIVE_DO_STOPGO(GetProcess());

    *ppCode = NULL;
    HRESULT hr = S_OK;

    // Get the code object.
    CordbILCode * pCode = NULL;
    hr = GetILCode(&pCode);
    _ASSERTE((pCode == NULL) == FAILED(hr));

    if (FAILED(hr))
        return hr;

    *ppCode = (ICorDebugCode *)pCode;

    return hr;
}

//-----------------------------------------------------------------------------
// CordbFunction::GetNativeCode
// Public API (ICorDebugFunction::GetNativeCode) to get the native code for
// this function.
// Note that this gets a pretty much random version of the native code when the
// function is a generic method that gets JITted more than once, e.g. for generics.
// Use EnumerateNativeCode instead in that case.
//
// Parameters:
//   ppCode - out parameter yeilding the native code object.
//
// Returns:
//   S_OK iff *ppCode is set.
//   CORDBG_E_CODE_NOT_AVAILABLE if there is no native code. This is common
//    if the function is not yet jitted.
//-----------------------------------------------------------------------------
HRESULT CordbFunction::GetNativeCode(ICorDebugCode **ppCode)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppCode, ICorDebugCode **);
    ATT_ALLOW_LIVE_DO_STOPGO(GetProcess());

    HRESULT hr = S_OK;

    // Make sure native code is updated before we go searching it.
    hr = InitNativeCodeInfo();
    if (FAILED(hr))
        return hr;

    // Generic methods may be jitted multiple times for different native instantiations,
    // and so have 1:n relationship between IL:Native. CordbFunction is 1:1 with IL,
    // CordbNativeCode is 1:1 with native.
    // The interface here only lets us return 1 CordbNativeCode object, so we are
    // returning an arbitrary one
    RSLockHolder lockHolder(GetProcess()->GetProcessLock());
    _ASSERTE(m_nativeCode == NULL || m_nativeCode->GetVersion() == m_dwEnCVersionNumber);

    if (m_nativeCode == NULL)
    {
        hr = CORDBG_E_CODE_NOT_AVAILABLE;   // This is the case for an unjitted function,
                                            // and so it will be very common.
    }
    else
    {
        m_nativeCode->ExternalAddRef();
        *ppCode = m_nativeCode;
        hr = S_OK;
    }

    return hr;
}


//-----------------------------------------------------------------------------
// CordbFunction::GetCode
// Internal method to get the IL code for this function. Each CordbFunction is
//  1:1 with IL, so there is a unique IL Code object to hand out.
//
// Parameters:
//    ppCode - out parameter, the IL code object for this function. This should
//       be set to NULL on entry.
// Return value:
//    S_OK iff *ppCode is set. Else error.
//-----------------------------------------------------------------------------
HRESULT CordbFunction::GetILCode(CordbILCode ** ppCode)
{
    FAIL_IF_NEUTERED(this);
    INTERNAL_SYNC_API_ENTRY(GetProcess()); //
    VALIDATE_POINTER_TO_OBJECT(ppCode, ICorDebugCode **);

    _ASSERTE(*ppCode == NULL && "Common source of errors is getting addref'd copy here and never Release()ing it");
    *ppCode = NULL;

    // Its okay to do this if the process is not sync'd.
    CORDBRequireProcessStateOK(GetProcess());

    // Fetch all information about this function.
    HRESULT hr = S_OK;
    CordbILCode * pCode = NULL;

    hr = GetILCodeAndSigToken();
    if (FAILED(hr))
    {
        return hr;
    }

    // It's possible that m_ILCode will still be NULL.
    pCode = m_pILCode;

    if (pCode != NULL)
    {
        pCode->ExternalAddRef();
        *ppCode = pCode;

        return hr;
    }
    else
    {
        return CORDBG_E_CODE_NOT_AVAILABLE;
    }
} // CordbFunction::GetCode

//-----------------------------------------------------------------------------
// CordbFunction::CreateBreakpoint
//   Implements ICorDebugFunction::CreateBreakpoint
//   Creates a breakpoint at IL offset 0 (which is after the prolog) of the function.
//   The function does not need to be jitted yet.
//
// Parameters:
//    ppBreakpoint - out parameter for newly created breakpoint object.
//
// Return:
//   S_OK - on success. Else error.
//----------------------------------------------------------------------------
HRESULT CordbFunction::CreateBreakpoint(ICorDebugFunctionBreakpoint **ppBreakpoint)
{
    HRESULT hr = S_OK;

    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppBreakpoint, ICorDebugFunctionBreakpoint **);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    RSExtSmartPtr<ICorDebugCode> pCode;

    // Use the IL code so that we stop after the prolog
    hr = GetILCode(&pCode);

    if (SUCCEEDED(hr))
    {
        hr = pCode->CreateBreakpoint(0, ppBreakpoint);
    }

    return hr;
}

#ifdef EnC_SUPPORTED
//-----------------------------------------------------------------------------
// CordbFunction::MakeOld
// Internal method to do any cleanup necessary when a Function is no longer
// the most current.
//-----------------------------------------------------------------------------
void CordbFunction::MakeOld()
{
    if (m_pILCode != NULL)
    {
        m_pILCode->MakeOld();
    }
}
#endif

//-----------------------------------------------------------------------------
// CordbFunction::GetLocalVarSigToken
// Public function (implements ICorDebugFunction::GetLocalVarSigToken) to
// get signature token.
//
// Parameters:
//  pmdSig - out parameter to hold signature token, which is scoped to the
//     function's module.
//
// Return value:
//   S_OK if pmdSig is set.
//-----------------------------------------------------------------------------
HRESULT CordbFunction::GetLocalVarSigToken(mdSignature *pmdSig)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pmdSig, mdSignature *);

    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    // This will initialize the token.
    HRESULT hr = GetILCodeAndSigToken();
    if (FAILED(hr))
        return hr;

    *pmdSig = GetILCode()->GetLocalVarSigToken();

    return S_OK;
}

//-----------------------------------------------------------------------------
// CordbFunction::GetCurrentVersionNumber
//  Public method for ICorDebugFunction::GetCurrentVersionNumber.
//   Gets the most recent (highest) EnC version number of this Function.
//   See CordbModule for EnC version number semantics.
//
// Parameters
//   pnCurrentVersion - out parameter to hold the version number.
//
// Returns:
//   S_OK on success.
//-----------------------------------------------------------------------------
HRESULT CordbFunction::GetCurrentVersionNumber(ULONG32 *pnCurrentVersion)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pnCurrentVersion, ULONG32 *);

    HRESULT hr = S_OK;
    RSLockHolder lockHolder(GetProcess()->GetProcessLock());

    // the most current version will always be the one found.
    CordbFunction* curFunc = m_pModule->LookupFunctionLatestVersion(m_MDToken);

    // will always find at least ourself
    PREFIX_ASSUME(curFunc != NULL);

    *pnCurrentVersion = (ULONG32)(curFunc->m_dwEnCVersionNumber);

#ifdef EnC_SUPPORTED
    _ASSERTE( *pnCurrentVersion >= this->m_dwEnCVersionNumber );
#else
    _ASSERTE(*pnCurrentVersion == CorDB_DEFAULT_ENC_FUNCTION_VERSION);
#endif

    return hr;
}

//-----------------------------------------------------------------------------
// CordbFunction::GetVersionNumber
//  Public method for ICorDebugFunction2::GetVersionNumber.
//   Gets the EnC version number of this specific Function instance.
//   See CordbModule for EnC version number semantics.
//
// Parameters
//   pnVersion - out parameter to hold the version number.
//
// Returns:
//   S_OK on success.
//-----------------------------------------------------------------------------
HRESULT CordbFunction::GetVersionNumber(ULONG32 *pnVersion)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pnVersion, ULONG32 *);

    // This API existed in V1.0 but wasn't implemented.  It needs V2 support to work.
    if (! this->GetProcess()->SupportsVersion(ver_ICorDebugFunction2))
    {
        return E_NOTIMPL;
    }

    *pnVersion = (ULONG32)m_dwEnCVersionNumber;

#ifdef EnC_SUPPORTED
    _ASSERTE(*pnVersion >= CorDB_DEFAULT_ENC_FUNCTION_VERSION);
#else
    _ASSERTE(*pnVersion == CorDB_DEFAULT_ENC_FUNCTION_VERSION);
#endif

    return S_OK;
}

//-----------------------------------------------------------------------------
// CordbFunction::GetVersionNumber
//  Public method for ICorDebugFunction2::GetVersionNumber.
//   Gets the EnC version number of this specific Function instance.
//   See CordbModule for EnC version number semantics.
//
// Parameters
//   pnVersion - out parameter to hold the version number.
//
// Returns:
//   S_OK on success.
//-----------------------------------------------------------------------------
HRESULT CordbFunction::GetActiveReJitRequestILCode(ICorDebugILCode **ppReJitedILCode)
{
    HRESULT hr = S_OK;
    VALIDATE_POINTER_TO_OBJECT(ppReJitedILCode, ICorDebugILCode **);
    PUBLIC_API_BEGIN(this);
    {
        *ppReJitedILCode = NULL;

        VMPTR_ILCodeVersionNode vmILCodeVersionNode = VMPTR_ILCodeVersionNode::NullPtr();
        GetProcess()->GetDAC()->GetActiveRejitILCodeVersionNode(GetModule()->m_vmModule, m_MDToken, &vmILCodeVersionNode);
        if (!vmILCodeVersionNode.IsNull())
        {
            RSSmartPtr<CordbReJitILCode> pILCode;
            IfFailThrow(LookupOrCreateReJitILCode(vmILCodeVersionNode, &pILCode));
            IfFailThrow(pILCode->QueryInterface(IID_ICorDebugILCode, (void**)ppReJitedILCode));
        }
    }
    PUBLIC_API_END(hr);
    return hr;
}

//-----------------------------------------------------------------------------
// CordbFunction::CreateNativeBreakpoint
//  Public method for ICorDebugFunction4::CreateNativeBreakpoint.
//   Sets a breakpoint at native offset 0 for all native code versions of a method.
//
// Parameters
//   pnVersion - out parameter to hold the version number.
//
// Returns:
//   S_OK on success.
//-----------------------------------------------------------------------------
HRESULT CordbFunction::CreateNativeBreakpoint(ICorDebugFunctionBreakpoint **ppBreakpoint)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppBreakpoint, ICorDebugFunctionBreakpoint **);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    HRESULT hr = S_OK;

    RSExtSmartPtr<CordbILCode> pCode;

    hr = GetILCode(&pCode);

    if (SUCCEEDED(hr))
    {
        hr = pCode->CreateNativeBreakpoint(ppBreakpoint);
    }

    return hr;
}

// determine whether we have a native-only implementation
// Arguments:
//     Input: none (we use information in various data members of this instance of CordbFunction: m_isNativeImpl,
//                  m_pIMImport, m_EnCCount)
//     Output none, although we will set m_isNativeImpl to true iff the function has a native-only implementation

void CordbFunction::InitNativeImpl()
{
    INTERNAL_SYNC_API_ENTRY(GetProcess());

    // Bail now if we've already discovered that this function is implemented natively as part of the Runtime.
    if (m_fIsNativeImpl != kUnknownImpl)
    {
        return;
    }

    // If we don't have a methodToken then we can't figure out what kind of function this is. This includes functions
    // such as LCG and ILStubs. In the past we created codepaths that avoided ever calling in here in the common case
    // and there would have been asserts and exceptions in the uncommon cases. Now I have just officially let the
    // function handle staying as an 'unknown' impl. In such a state it provides no IL, no sigtoken, no native code, and
    // no parent class.
    if (m_MDToken == mdMethodDefNil)
    {
        return;
    }

    // Figure out if this function is implemented as a native part of the Runtime. If it is, then this ICorDebugFunction
    // is just a container for certain Right Side bits of info, i.e., module, class, token, etc.
    DWORD attrs;
    DWORD implAttrs;
    ULONG ulRVA;
    BOOL  isDynamic;

    IfFailThrow(GetModule()->GetMetaDataImporter()->GetMethodProps(m_MDToken, NULL, NULL, 0, NULL,
                                                         &attrs, NULL, NULL, &ulRVA, &implAttrs));
    isDynamic = GetModule()->IsDynamic();

    // A method has associated IL if its RVA is non-zero, unless it is a dynamic module
    // @todo : if RVA is 0 and function has been EnC'd then it isn't native. Remove isEnC
    // condition when the compilers stop generating 0 for an RVA.
    BOOL isEnC = (GetModule()->m_EnCCount != 0);
    if (IsMiNative(implAttrs) || ((isDynamic == FALSE) && (isEnC == FALSE) && (ulRVA == 0)))
    {

        m_fIsNativeImpl = kNativeOnly;
    }
    else
    {
        m_fIsNativeImpl = kHasIL;
    }

} // CordbFunction::GetProcessAndCheckForNativeImpl

// Returns the function's ILCode and SigToken
// Arguments:
//    Input: none (required info comes from various data members of this instance of CordbFunction
//    Output (required):
//      none explicit, but this will:
//         construct a new instance of CordbILCode and assign it to m_pILCode

HRESULT CordbFunction::GetILCodeAndSigToken()
{
    INTERNAL_SYNC_API_ENTRY(GetProcess());

    CordbProcess * pProcess = m_pModule->GetProcess();
    HRESULT        hr = S_OK;

    EX_TRY
    {

        // ensure that we're not trying to get information about a native-only function
        InitNativeImpl();
        if (m_fIsNativeImpl == kNativeOnly || m_fIsNativeImpl == kUnknownImpl)
        {
            ThrowHR(CORDBG_E_FUNCTION_NOT_IL);
        }

        if (m_pILCode == NULL)
        {
            // we haven't gotten the information previously

            _ASSERTE(pProcess != NULL);

            // This target buffer and mdSignature might never have their values changed from the
            // initial ones if the dump target is missing memory. TargetBuffer has a default
            // constructor to zero its data and localVarSigToken is explicitly inited.
            TargetBuffer codeInfo;
            mdSignature  localVarSigToken = mdSignatureNil;
            SIZE_T       currentEnCVersion;

            {
                RSLockHolder lockHolder(GetProcess()->GetProcessLock());

                // In the dump case we may not have the backing memory for this. In such a case
                // we construct an empty ILCode object and leave the signatureToken as mdSignatureNil.
                // It may also be the case that the memory we read from the dump be inconsistent (huge method size)
                // and we also fallback on creating an empty ILCode object.
                // See issue DD 273199 for cases where IL and NGEN metadata mismatch (different RVAs).
                ALLOW_DATATARGET_MISSING_OR_INCONSISTENT_MEMORY(
                    pProcess->GetDAC()->GetILCodeAndSig(m_pModule->GetRuntimeDomainFile(),
                                                            m_MDToken,
                                                            &codeInfo,
                                                            &localVarSigToken);
                );

                currentEnCVersion = m_pModule->LookupFunctionLatestVersion(m_MDToken)->m_dwEnCVersionNumber;
            }

            LOG((LF_CORDB,LL_INFO10000,"R:CF::GICAST: looking for IL code, version 0x%x\n", currentEnCVersion));

            if (m_pILCode == NULL)
            {
                LOG((LF_CORDB,LL_INFO10000,"R:CF::GICAST: not found, creating...\n"));
                if(codeInfo.pAddress == 0)
                {
                    LOG((LF_CORDB,LL_INFO10000,"R:CF::GICAST: memory was missing - empty ILCode being created\n"));
                }

                // If everything succeeded, we set the IL code object (it's an outparam here).
                _ASSERTE(m_pILCode == NULL);
                m_pILCode.Assign(new(nothrow)CordbILCode(this,
                                                        codeInfo,
                                                        currentEnCVersion,
                                                        localVarSigToken));

                if (m_pILCode == NULL)
                {
                    ThrowHR(E_OUTOFMEMORY);
                }
            }
        }
    }
    EX_CATCH_HRESULT(hr);
    return hr;
} // CordbFunction::GetILCodeAndSigToken


// Get the metadata token for the class to which a function belongs.
// Arguments:
//    Input:
//       funcMetadataToken - the metadata token for the method
//    Output (required):
//       classMetadataToken - the metadata token for the class to which the method belongs
mdTypeDef CordbFunction::InitParentClassOfFunctionHelper(mdToken funcMetadataToken)
{
    // Get the class this method is in.
    mdToken tkParent = mdTypeDefNil;
    IfFailThrow(GetModule()->GetInternalMD()->GetParentToken(funcMetadataToken, &tkParent));
    _ASSERTE(TypeFromToken(tkParent) == mdtTypeDef);

    return tkParent;
} // CordbFunction::InitParentClassOfFunctionHelper

// Get the class to which a given function belongs
// Arguments:
//    Input: none (required information comes from data members of this instance of CordbFunction)
//    Output (required): none, but sets m_pClass
HRESULT CordbFunction::InitParentClassOfFunction()
{
    INTERNAL_SYNC_API_ENTRY(GetProcess());

    CordbProcess * pProcess = m_pModule->GetProcess();
    (void)pProcess; //prevent "unused variable" error from GCC
    HRESULT        hr = S_OK;

    EX_TRY
    {

        // ensure that we're not trying to get information about a native-only function
        InitNativeImpl();
        if (m_fIsNativeImpl == kNativeOnly || m_fIsNativeImpl == kUnknownImpl)
        {
            ThrowHR(CORDBG_E_FUNCTION_NOT_IL);
        }

        mdTypeDef classMetadataToken;
        VMPTR_DomainFile vmDomainFile = m_pModule->GetRuntimeDomainFile();

        classMetadataToken = InitParentClassOfFunctionHelper(m_MDToken);

        if ((m_pClass == NULL) && (classMetadataToken != mdTypeDefNil))
        {
             // we haven't gotten the information previously but we have it now

            _ASSERTE(pProcess != NULL);

            CordbAssembly *pAssembly = m_pModule->GetCordbAssembly();
            PREFIX_ASSUME(pAssembly != NULL);

            CordbModule* pClassModule = pAssembly->GetAppDomain()->LookupOrCreateModule(vmDomainFile);
            PREFIX_ASSUME(pClassModule != NULL);

            CordbClass *pClass;
            hr = pClassModule->LookupOrCreateClass(classMetadataToken, &pClass);

            IfFailThrow(hr);

            _ASSERTE(pClass != NULL);
            m_pClass = pClass;
        }
    }
    EX_CATCH_HRESULT(hr);
    return hr;

} // CordbFunction::InitParentClassOfFunction

// Get information about the native code blob for a function and add it to m_nativeCodeTable
// Arguments:
//     Input: none, but we use some data members of this instance of CordbFunction
//     Output: standard HRESULT value
// Notes: Apart from the HRESULT, this function will build a new instance of CordbNativeCode and
//        add it to the hash table of CordbNativeCodes for this function, unless we have done that
//        previously

HRESULT CordbFunction::InitNativeCodeInfo()
{
    INTERNAL_SYNC_API_ENTRY(GetProcess());

    CordbProcess * pProcess = m_pModule->GetProcess();
    HRESULT        hr = S_OK;

    EX_TRY
    {

        // ensure that we're not trying to get information about a native-only function
        InitNativeImpl();
        if (m_fIsNativeImpl == kNativeOnly || m_fIsNativeImpl == kUnknownImpl)
        {
            ThrowHR(CORDBG_E_FUNCTION_NOT_IL);
        }

        _ASSERTE(pProcess != NULL);

        // storage for information retrieved from the DAC. This is cleared in the constructor, so it
        // won't contain garbage if we don't use the DAC to retrieve information we already got before.
        NativeCodeFunctionData codeInfo;

        if (m_nativeCode == NULL)
        {
            // Get the native code information from the DAC
            // PERF: this call is potentially more costly than it needs to be
            // All we actually need is the start address and method desc which are cheap to get relative
            // to some of the other members. So far this doesn't appear to be a perf hotspot, but if it
            // shows up in some scenario it wouldn't be too hard to improve it
            pProcess->GetDAC()->GetNativeCodeInfo(m_pModule->GetRuntimeDomainFile(), m_MDToken, &codeInfo);
        }

        // populate the m_nativeCode pointer with the code info we found
        if (codeInfo.IsValid())
        {
            m_nativeCode.Assign(m_pModule->LookupOrCreateNativeCode(m_MDToken, codeInfo.vmNativeCodeMethodDescToken,
                codeInfo.m_rgCodeRegions[kHot].pAddress));
        }

     }
    EX_CATCH_HRESULT(hr);
    return hr;
} // CordbFunction::InitNativeCodeInfo

//-----------------------------------------------------------------------------
// CordbFunction::SetJMCStatus
// Public method (implements ICorDebugFunction2::SetJMCStatus).
// Set the JMC (eg, "User code" vs. "Non-user code") status of this function.
//
// Parameters:
//   fIsUserCode - true to set this Function to JMC, else False.
//
// Returns:
//   S_OK if successfully updated JMC status.
//-----------------------------------------------------------------------------
HRESULT CordbFunction::SetJMCStatus(BOOL fIsUserCode)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    HRESULT hr = S_OK;

    LOG((LF_CORDB,LL_INFO10000,"CordbFunction::SetJMCStatus to %d, (token=0x%08x, module=%p)\n",
        fIsUserCode, m_MDToken, m_pModule));

    // Make sure the Left-Side is in a good state.
    FAIL_IF_NEUTERED(this);
    CordbProcess* pProcess = m_pModule->GetProcess();
    ATT_REQUIRE_STOPPED_MAY_FAIL(pProcess);



    // Send an event to the LS to keep it updated.

    // Validation - JMC Steppers don't have defined behavior if
    // JMC method status gets toggled underneath them. However, we don't have
    // a good way of verifying which methods are of interest to a JMC stepper.
    // Having outstanding JMC steppers is dangerous here, but still can be
    // done safely.
    // Furthermore, debuggers may want to lazily set JMC status (such as when
    // code is loaded), which may happen while we have outstanding steppers.


    DebuggerIPCEvent event;
    pProcess->InitIPCEvent(&event, DB_IPCE_SET_METHOD_JMC_STATUS, true, m_pModule->GetAppDomain()->GetADToken());
    event.SetJMCFunctionStatus.vmDomainFile = m_pModule->GetRuntimeDomainFile();
    event.SetJMCFunctionStatus.funcMetadataToken   = m_MDToken;
    event.SetJMCFunctionStatus.dwStatus            = fIsUserCode;


    // Note: two-way event here...
    hr = pProcess->m_cordb->SendIPCEvent(pProcess, &event, sizeof(DebuggerIPCEvent));

    // Stop now if we can't even send the event.
    if (!SUCCEEDED(hr))
        return hr;

    _ASSERTE(event.type == DB_IPCE_SET_METHOD_JMC_STATUS_RESULT);

    return event.hr;
}

//-----------------------------------------------------------------------------
// CordbFunction::GetJMCStatus
// Public function (implements ICorDebugFunction2::GetJMCStatus)
// Get the JMC status of this function.
//
// Parameters:
//   pfIsUserCode - out parameter describing whether this method is user code.
//   true iff this function is user code, else false.
//
// Return:
//   returns S_OK if *pfIsUserCode is set.
//-----------------------------------------------------------------------------
HRESULT CordbFunction::GetJMCStatus(BOOL * pfIsUserCode)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());
    VALIDATE_POINTER_TO_OBJECT(pfIsUserCode, BOOL*);


    _ASSERTE(pfIsUserCode != NULL);
    if (pfIsUserCode == NULL)
        return E_INVALIDARG;

    // <TODO> @perf - If we know that we haven't updated the JMC status on anything
    // in this module (we could keep a dirty flag), then we can just cache
    // the jmc status and not send an event to query each time. </TODO>

    // Make sure the process is in a sane state.
    CordbProcess* pProcess = m_pModule->GetProcess();
    _ASSERTE(pProcess != NULL);

    // Ask the left-side if a method is user code or not.
    DebuggerIPCEvent event;
    pProcess->InitIPCEvent(&event, DB_IPCE_GET_METHOD_JMC_STATUS, true, m_pModule->GetAppDomain()->GetADToken());
    event.SetJMCFunctionStatus.vmDomainFile = m_pModule->GetRuntimeDomainFile();
    event.SetJMCFunctionStatus.funcMetadataToken   = m_MDToken;


    // Note: two-way event here...
    HRESULT hr = pProcess->m_cordb->SendIPCEvent(pProcess, &event, sizeof(DebuggerIPCEvent));

    // Stop now if we can't even send the event.
    if (!SUCCEEDED(hr))
        return hr;

    _ASSERTE(event.type == DB_IPCE_GET_METHOD_JMC_STATUS_RESULT);

    // update our internal copy of the status.
    BOOL fIsUserCode = event.SetJMCFunctionStatus.dwStatus;

    *pfIsUserCode = fIsUserCode;

    return event.hr;
}


/*
 * CordbFunction::GetSig
 *
 * Get the method's full metadata signature. This may be cached, but for dynamic modules we'll always read it from
 * the metadata. This function also returns the argument count and whether or not the method is static.
 *
 * Parameters:
 *    pMethodSigParser - OUT: the signature parser class to use for all signature parsing.
 *    pFunctionArgCount - OUT: the number of arguments the method takes.
 *    pFunctionIsStatic - OUT: TRUE if the method is static, FALSE if it is not..
 *
 * Returns:
 *    HRESULT for success or failure.
 *
 */
HRESULT CordbFunction::GetSig(SigParser *pMethodSigParser,
                              ULONG *pFunctionArgCount,
                              BOOL *pFunctionIsStatic)
{
    INTERNAL_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    HRESULT hr = S_OK;

    // If the module is dynamic, there had better not be a cached locals signature.
    _ASSERTE(!GetModule()->IsDynamic() || !m_fCachedMethodValuesValid);

    // If the method signature cache is null, then go read the signature from the
    // matadata. For dynamic methods we never cache the parser because the method
    // may change and the cached value will not match.
    if (!m_fCachedMethodValuesValid)
    {
        PCCOR_SIGNATURE functionSignature;
        ULONG size;
        DWORD methodAttr = 0;
        uint32_t argCount;

        EX_TRY // @dbgtotod - push this up
        {
            hr = GetModule()->GetMetaDataImporter()->GetMethodProps(m_MDToken, NULL, NULL, 0, NULL,
                                                           &methodAttr, &functionSignature, &size, NULL, NULL);
        }
        EX_CATCH_HRESULT(hr);
        IfFailRet(hr);

        SigParser sigParser = SigParser(functionSignature, size);

        IfFailRet(sigParser.SkipMethodHeaderSignature(&argCount));

        // If this function is not static, then we've got one extra arg.
        BOOL isStatic = (methodAttr & mdStatic) != 0;

        if (!isStatic)
        {
            argCount++;
        }

        // Cache the value for non-dynamic modules, so this is faster later.
        if (!GetModule()->IsDynamic())
        {
            m_methodSigParserCached = sigParser;
            m_argCountCached = argCount;
            m_fIsStaticCached = isStatic;
            m_fCachedMethodValuesValid = TRUE;
        }
        else
        {
            // This is the Dynamic method case, so we can't cache. Just leave fields blank
            // and set out-parameters based off locals.
            if (pMethodSigParser != NULL)
            {
                *pMethodSigParser = sigParser;
            }

            if (pFunctionArgCount != NULL)
            {
                *pFunctionArgCount = argCount;
            }

            if (pFunctionIsStatic != NULL)
            {
                *pFunctionIsStatic = isStatic;
            }
        }
    }

    if (m_fCachedMethodValuesValid)
    {
        //
        // Retrieve values from cache
        //

        if (pMethodSigParser != NULL)
        {
            //
            // Give them a new instance of the cached value
            //
            *pMethodSigParser = m_methodSigParserCached;
        }

        if (pFunctionArgCount != NULL)
        {
            *pFunctionArgCount = m_argCountCached;
        }

        if (pFunctionIsStatic != NULL)
        {
            *pFunctionIsStatic = m_fIsStaticCached;
        }

    }

    //
    // We should never have a cached value for in a dynamic module.
    //
    CONSISTENCY_CHECK_MSGF(((GetModule()->IsDynamic() && !m_fCachedMethodValuesValid) ||
                            (!GetModule()->IsDynamic() && m_fCachedMethodValuesValid)),
                           ("No dynamic modules should be cached! Module=%p This=%p", GetModule(), this));

    return hr;
}


//-----------------------------------------------------------------------------
// CordbFunction::GetArgumentType
// Internal method. Given an 0-based IL argument number, return its type.
// This can't access hidden parameters.
//
// Parameters:
//   dwIndex - 0-based index for IL argument number. For instance types,
//           'this' argument is #0. For static types, first argument is #0.
//   pInst - instantiation information if this is a generic function. Eg,
//           if function is List<T>, inst describes T.
//   ppResultType - out parameter, yields to CordbType of the argument.
//
// Return:
//   S_OK on success.
//
HRESULT CordbFunction::GetArgumentType(DWORD dwIndex,
                                       const Instantiation * pInst,
                                       CordbType ** ppResultType)
{
    FAIL_IF_NEUTERED(this);
    INTERNAL_SYNC_API_ENTRY(GetProcess());

    HRESULT hr = S_OK;

    // Get the method's signature, which contains the types for all the arguments.
    SigParser sigParser;
    ULONG cMethodArgs;
    BOOL fMethodIsStatic;

    IfFailRet(GetSig(&sigParser, &cMethodArgs, &fMethodIsStatic));

    // Check the index
    if (dwIndex >= cMethodArgs)
    {
        return E_INVALIDARG;
    }

    if (!fMethodIsStatic)
    {
        if (dwIndex == 0)
        {
            // Return the signature for the 'this' pointer for the
            // class this method is in.
            return m_pClass->GetThisType(pInst, ppResultType);
        }
        else
        {
            dwIndex--;
        }
    }

    // Run the signature and find the required argument.
    for (unsigned int i = 0; i < dwIndex; i++)
    {
        IfFailRet(sigParser.SkipExactlyOne());
    }

    hr = CordbType::SigToType(m_pModule, &sigParser, pInst, ppResultType);

    return hr;
}

//-----------------------------------------------------------------------------
// CordbFunction::NotifyCodeCreated
// Internal method. Allows CordbFunctions to get access to a canonical native code entry
// that they will return when asked for native code. The 1:1 mapping between
// function and code was invalidated by generics but debuggers continue to use
// the old API. When they do we need to have some code to hand them back even
// though it is an arbitrary instantiation. Note that that the cannonical code
// here is merely the first one that a user inspects... it is not guaranteed to
// be the same in each debugging session but once set it will never change. It is
// also definately NOT guaranteed to be the instantation over the runtime type
// __Canon.
//
// Parameters:
//   nativeCode - the code which corresponds to this function
//
VOID CordbFunction::NotifyCodeCreated(CordbNativeCode* nativeCode)
{
    INTERNAL_SYNC_API_ENTRY(GetProcess());
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    // Grab this native code as the canonical one if we don't already
    // have a canonical entry
    if(m_nativeCode == NULL)
        m_nativeCode.Assign(nativeCode);
}


//-----------------------------------------------------------------------------
// LookupOrCreateReJitILCode finds an existing version of CordbReJitILCode in the given function.
// If the CordbReJitILCode doesn't exist, it creates it.
//
//
HRESULT CordbFunction::LookupOrCreateReJitILCode(VMPTR_ILCodeVersionNode vmILCodeVersionNode, CordbReJitILCode** ppILCode)
{
    INTERNAL_API_ENTRY(this);

    HRESULT hr = S_OK;
    _ASSERTE(GetProcess()->ThreadHoldsProcessLock());

    CordbReJitILCode * pILCode = m_reJitILCodes.GetBase(VmPtrToCookie(vmILCodeVersionNode));

    // special case non-existance as need to add to the hash table too
    if (pILCode == NULL)
    {
        // we don't yet support ENC and ReJIT together, so the version should be 1
        _ASSERTE(m_dwEnCVersionNumber == 1);
        RSInitHolder<CordbReJitILCode> pILCodeHolder(new CordbReJitILCode(this, 1, vmILCodeVersionNode));
        IfFailRet(m_reJitILCodes.AddBase(pILCodeHolder));
        pILCode = pILCodeHolder;
        pILCodeHolder.ClearAndMarkDontNeuter();
    }

    pILCode->InternalAddRef();
    *ppILCode = pILCode;
    return S_OK;
}
