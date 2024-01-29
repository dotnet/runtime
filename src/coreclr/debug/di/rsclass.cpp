// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************

//
// File: class.cpp
//
//*****************************************************************************
#include "stdafx.h"

// We have an assert in ceemain.cpp that validates this assumption
#define FIELD_OFFSET_NEW_ENC_DB          0x07FFFFFB

#include "winbase.h"
#include "corpriv.h"



//-----------------------------------------------------------------------------
// class CordbClass
// Represents a IL-Class in the debuggee, eg: List<T>, System.Console, etc
//
// Parameters:
//  m - module that the class is contained in.
//  classMetadataToken - metadata token for the class, scoped to module m.
//-----------------------------------------------------------------------------
CordbClass::CordbClass(CordbModule *m, mdTypeDef classMetadataToken)
  : CordbBase(m->GetProcess(), classMetadataToken, enumCordbClass),
    m_loadLevel(Constructed),
    m_fLoadEventSent(FALSE),
    m_fHasBeenUnloaded(false),
    m_pModule(m),
    m_token(classMetadataToken),
    m_fIsValueClassKnown(false),
    m_fIsValueClass(false),
    m_fHasTypeParams(false),
    m_continueCounterLastSync(0),
    m_fCustomNotificationsEnabled(false)
{
    m_classInfo.Clear();
}


/*
    A list of which resources owned by this object are accounted for.

    HANDLED:
        CordbModule*            m_module; // Assigned w/o AddRef()
        FieldData *m_fields;              // Deleted in ~CordbClass
        CordbHangingFieldTable  m_hangingFieldsStatic; // by value, ~CHashTableAndData frees
*/


//-----------------------------------------------------------------------------
//  Destructor for CordbClass
//-----------------------------------------------------------------------------
CordbClass::~CordbClass()
{
    // We should have been explicitly neutered before our internal ref went to 0.
    _ASSERTE(IsNeutered());
}

//-----------------------------------------------------------------------------
// Neutered by CordbModule
// See CordbBase::Neuter for semantics.
//-----------------------------------------------------------------------------
void CordbClass::Neuter()
{
    // Reduce the reference count on the type object for this class
    m_type.Clear();
    CordbBase::Neuter();
}




//-----------------------------------------------------------------------------
// Standard IUnknown::QI implementation.
// See IUnknown::QI  for standard semantics.
//-----------------------------------------------------------------------------
HRESULT CordbClass::QueryInterface(REFIID id, void **pInterface)
{
    if (id == IID_ICorDebugClass)
    {
        *pInterface = static_cast<ICorDebugClass*>(this);
    }
    else if (id == IID_ICorDebugClass2)
    {
        *pInterface = static_cast<ICorDebugClass2*>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugClass*>(this));
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
// Get a ICorDebugValue for a static field on this class.
//
// Parameters:
//   fieldDef - metadata token for field on this class. Can not be from an
//      inherited class.
//   pFrame - frame used to resolve Thread-static, AppDomain-static, etc.
//   ppValue - OUT: gets value of the field.
//
// Returns:
//    S_OK on success.
//    CORDBG_E_STATIC_VAR_NOT_AVAILABLE
//-----------------------------------------------------------------------------
HRESULT CordbClass::GetStaticFieldValue(mdFieldDef fieldDef,
                                        ICorDebugFrame *pFrame,
                                        ICorDebugValue **ppValue)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppValue, ICorDebugValue **);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    HRESULT          hr = S_OK;
    *ppValue = NULL;
    BOOL             fEnCHangingField = FALSE;


    IMetaDataImport * pImport = NULL;
    EX_TRY
    {
        RSLockHolder lockHolder(GetProcess()->GetProcessLock());
        pImport = GetModule()->GetMetaDataImporter(); // throws

        // Validate the token.
        if (!pImport->IsValidToken(fieldDef) || (TypeFromToken(fieldDef) != mdtFieldDef))
        {
            ThrowHR(E_INVALIDARG);
        }

        // Make sure we have enough info about the class.
        Init();

        // Uninstantiated generics (eg, Foo<T>) don't have static data. Must use instantiated (eg Foo<int>)
        // But all CordbClass instances are uninstantiated. So this should fail for all generic types.
        // Normally, debuggers should be using ICorDebugType instead.
        // Though in the forward compat case, they'll hit this.
        if (HasTypeParams())
        {
            ThrowHR(CORDBG_E_STATIC_VAR_NOT_AVAILABLE);
        }


        // Lookup the field given its metadata token.
        FieldData *pFieldData;

        hr = GetFieldInfo(fieldDef, &pFieldData);

        // This field was added by EnC, need to use EnC specific code path
        if (hr == CORDBG_E_ENC_HANGING_FIELD)
        {
            // Static fields added with EnC hang off the EnCFieldDesc
            hr = GetEnCHangingField(fieldDef,
                &pFieldData,
                NULL);

            if (SUCCEEDED(hr))
            {
                fEnCHangingField = TRUE;
            }
            // Note: the FieldOffset in pFieldData has been cooked to produce
            // the correct address of the field in the syncBlock.
            // @todo: extend Debugger_IPCEFieldData so we don't have to cook the offset here
        }

        IfFailThrow(hr);

        {
            Instantiation emptyInst;

            hr = CordbClass::GetStaticFieldValue2(GetModule(),
                pFieldData,
                fEnCHangingField,
                &emptyInst,
                pFrame,
                ppValue);
            // Let hr fall through
        }
    }
    EX_CATCH_HRESULT(hr);

    // Translate Failure HRs.
    if (pImport != NULL)
    {
        hr = CordbClass::PostProcessUnavailableHRESULT(hr, pImport, fieldDef);
    }

    return hr;

}

//-----------------------------------------------------------------------------
// Common helper for accessing statics from both CordbClass and CordbType.
//
// Arguments:
//   pModule - module containing the class
//   pFieldData - field data describing the field (this is correlated to a
//         mdFieldDef, but has more specific data)
//   fEnCHangingField - field storage hangs off the FieldDesc for EnC
//   pInst - generic instantiation.
//   pFrame - frame used for context for Thread-static, AD-static, etc.
//   ppValue - OUT: out parameter to get value.
//
// Returns:
//   S_OK on success.
//   CORDBG_E_FIELD_NOT_STATIC - if field isn't static.
//   CORDBG_E_STATIC_VAR_NOT_AVAILABLE - if field storage is not available.
//   Else some other failure.
/* static */
HRESULT CordbClass::GetStaticFieldValue2(CordbModule * pModule,
                                         FieldData * pFieldData,
                                         BOOL fEnCHangingField,
                                         const Instantiation * pInst,
                                         ICorDebugFrame * pFrame,
                                         ICorDebugValue ** ppValue)
{
    FAIL_IF_NEUTERED(pModule);
    INTERNAL_SYNC_API_ENTRY(pModule->GetProcess());
    _ASSERTE((pModule->GetProcess()->GetShim() == NULL) || pModule->GetProcess()->GetSynchronized());
    HRESULT hr = S_OK;

    if (!pFieldData->m_fFldIsStatic)
    {
        return CORDBG_E_FIELD_NOT_STATIC;
    }

    CORDB_ADDRESS pRmtStaticValue = NULL;
    CordbProcess * pProcess = pModule->GetProcess();

    if (!pFieldData->m_fFldIsTLS)
    {
        if (pFieldData->m_fFldIsCollectibleStatic)
        {
            EX_TRY
            {
                pRmtStaticValue = pProcess->GetDAC()->GetCollectibleTypeStaticAddress(pFieldData->m_vmFieldDesc,
                                                                                      pModule->GetAppDomain()->GetADToken());
            }
            EX_CATCH_HRESULT(hr);
            if(FAILED(hr))
            {
                return hr;
            }
        }
        else
        {
            // Statics never move, so we always address them using their absolute address.
            _ASSERTE(pFieldData->OkToGetOrSetStaticAddress());
            pRmtStaticValue = pFieldData->GetStaticAddress();
        }
    }
    else
    {
        // We've got a thread local static

        if( fEnCHangingField )
        {
            // fEnCHangingField is set for fields added with EnC which hang off the FieldDesc.
            // Thread-local statics cannot be added with EnC, so we shouldn't be here
            // if this is an EnC field is thread-local.
            _ASSERTE(!pFieldData->m_fFldIsTLS );
        }
        else
        {
            if (pFrame == NULL)
            {
                return E_INVALIDARG;
            }

            CordbFrame * pRealFrame = CordbFrame::GetCordbFrameFromInterface(pFrame);
            _ASSERTE(pRealFrame != NULL);

            // Get the thread we are working on
            CordbThread *  pThread  = pRealFrame->m_pThread;

            EX_TRY
            {
                pRmtStaticValue = pProcess->GetDAC()->GetThreadStaticAddress(pFieldData->m_vmFieldDesc,
                                                                             pThread->m_vmThreadToken);
            }
            EX_CATCH_HRESULT(hr);
            if(FAILED(hr))
            {
                return hr;
            }

        }
    }

    if (pRmtStaticValue == NULL)
    {
        // type probably wasn't loaded yet.
        // The debugger may chose to func-eval the creation of an instance of this type and try again.
        return CORDBG_E_STATIC_VAR_NOT_AVAILABLE;
    }

    SigParser sigParser;
    hr = S_OK;
    EX_TRY
    {
        hr = pFieldData->GetFieldSignature(pModule, &sigParser);
    }
    EX_CATCH_HRESULT(hr);
    IfFailRet(hr);

    CordbType * pType;
    IfFailRet (CordbType::SigToType(pModule, &sigParser, pInst, &pType));

    bool fIsValueClass = false;
    EX_TRY
    {
        fIsValueClass = pType->IsValueType(); // throws
    }
    EX_CATCH_HRESULT(hr);
    IfFailRet(hr);

    // Static value classes are stored as handles so that GC can deal with them properly.  Thus, we need to follow the
    // handle like an objectref.  Do this by forcing CreateValueByType to think this is an objectref. Note: we don't do
    // this for value classes that have an RVA, since they're laid out at the RVA with no handle.
    bool fIsBoxed = (fIsValueClass &&
                     !pFieldData->m_fFldIsRVA &&
                     !pFieldData->m_fFldIsPrimitive &&
                     !pFieldData->m_fFldIsTLS);

    TargetBuffer remoteValue(pRmtStaticValue, CordbValue::GetSizeForType(pType, fIsBoxed ? kBoxed : kUnboxed));
    ICorDebugValue * pValue = NULL;

    EX_TRY
    {
        CordbValue::CreateValueByType(pModule->GetAppDomain(),
                                      pType,
                                      fIsBoxed,
                                      remoteValue,
                                      MemoryRange(NULL, 0),
                                      NULL,
                                      &pValue);  // throws
    }
    EX_CATCH_HRESULT(hr);

    if (SUCCEEDED(hr))
    {
        *ppValue = pValue;
    }

    return hr;
}


//-----------------------------------------------------------------------------
// Public method to build a CordbType from a CordbClass.
// This is used to build up generic types. Eg, build:
//    List<T>  + { int  } --> List<int>
//
// Arguments:
//  elementType - element type. Either ELEMENT_TYPE_CLASS, or ELEMENT_TYPE_VALUETYPE.
//      We could technically figure this out from the metadata (by looking if it derives
//      from System.ValueType).
//  cTypeArgs - number of elements in rgpTypeArgs array
//  rgpTypeArgs - array for type args.
//  ppType - OUT: out parameter to hold resulting type.
//
// Returns:
//   S_OK on success. Else false.
//
HRESULT CordbClass::GetParameterizedType(CorElementType elementType,
                                         ULONG32 cTypeArgs,
                                         ICorDebugType * rgpTypeArgs[],
                                         ICorDebugType ** ppType)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppType, ICorDebugType **);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

  // Note: Do not call Init() to find out if its a VC or not.
  // Rather expect the client to tell us. This means the debug client
  // can describe type instantiations not yet seen in the EE.

    if ((elementType != ELEMENT_TYPE_CLASS) && (elementType != ELEMENT_TYPE_VALUETYPE))
    {
        return E_INVALIDARG;
    }

    // Prefast overflow check:
    S_UINT32 allocSize = S_UINT32( cTypeArgs ) * S_UINT32( sizeof(CordbType *) );

    if (allocSize.IsOverflow())
    {
        return E_INVALIDARG;
    }

    CordbAppDomain * pClassAppDomain = GetAppDomain();

    // Note: casting from (ICorDebugType **) to (CordbType **) is not valid.
    // Offsets may differ.  Copy and validate the type array.
    CordbType ** ppArgTypes = reinterpret_cast<CordbType **>(_alloca( allocSize.Value()));

    for (unsigned int i = 0; i < cTypeArgs; i++)
    {
        ppArgTypes[i] = static_cast<CordbType *>( rgpTypeArgs[i] );

        CordbAppDomain * pArgAppDomain = ppArgTypes[i]->GetAppDomain();

        if ((pArgAppDomain != NULL) && (pArgAppDomain != pClassAppDomain))
        {
            return CORDBG_E_APPDOMAIN_MISMATCH;
        }
    }

    {
        CordbType * pResultType;

        Instantiation typeInstantiation(cTypeArgs, ppArgTypes);

        HRESULT hr = CordbType::MkType(pClassAppDomain, elementType, this, &typeInstantiation, &pResultType);

        if (FAILED(hr))
        {
            return hr;
        }

        *ppType = pResultType;
    }

    _ASSERTE(*ppType);

    if (*ppType)
    {
        (*ppType)->AddRef();
    }
    return S_OK;
}

//-----------------------------------------------------------------------------
// Returns true if the field is a static literal.
// In this case, the debugger should get the value from the metadata.
//-----------------------------------------------------------------------------
bool IsFieldStaticLiteral(IMetaDataImport *pImport, mdFieldDef fieldDef)
{
    DWORD dwFieldAttr;
    HRESULT hr2 = pImport->GetFieldProps(
        fieldDef,
        NULL,
        NULL,
        0,
        NULL,
        &dwFieldAttr,
        NULL,
        0,
        NULL,
        NULL,
        0);

    if (SUCCEEDED(hr2) && IsFdLiteral(dwFieldAttr))
    {
        return true;
    }

    return false;
}


//-----------------------------------------------------------------------------
// Filter to determine a more descriptive failing HResult for a field lookup.
//
// Parameters:
//   hr - incoming ambiguous HR.
//   pImport - metadata importer for this class.
//   feildDef - field being looked up.
//
// Returns:
//  hr - the incoming HR if no further HR can be determined.
//  else another failing HR that it judged to be more specific that the incoming HR.
//-----------------------------------------------------------------------------
HRESULT CordbClass::PostProcessUnavailableHRESULT(HRESULT hr,
                                       IMetaDataImport *pImport,
                                       mdFieldDef fieldDef)
{
    CONTRACTL
    {
        NOTHROW; // just translates an HR. shouldn't need to throw.
    }
    CONTRACTL_END;

    if (hr == CORDBG_E_FIELD_NOT_AVAILABLE)
    {
        if (IsFieldStaticLiteral(pImport, fieldDef))
        {
            return CORDBG_E_VARIABLE_IS_ACTUALLY_LITERAL;
        }
    }

    return hr;
}

//-----------------------------------------------------------------------------
// Public method to get the Module that this class lives in.
//
// Parameters:
//    ppModule - OUT: holds module that this class gets in.
//
// Returns:
//   S_OK on success.
//-----------------------------------------------------------------------------
HRESULT CordbClass::GetModule(ICorDebugModule **ppModule)
{
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppModule, ICorDebugModule **);

    *ppModule = static_cast<ICorDebugModule*> (m_pModule);
    m_pModule->ExternalAddRef();

    return S_OK;
}

//-----------------------------------------------------------------------------
// Get the mdTypeDef token that this class corresponds to.
//
// Parameters:
//   pTypeDef - OUT: out param to get typedef token.
//
// Returns:
//   S_OK - on success.
//-----------------------------------------------------------------------------
HRESULT CordbClass::GetToken(mdTypeDef *pTypeDef)
{
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pTypeDef, mdTypeDef *);

    _ASSERTE(TypeFromToken(m_token) == mdtTypeDef);

    *pTypeDef = m_token;

    return S_OK;
}

//-----------------------------------------------------------------------------
// Set the JMC status on all of our member functions.
// The current implementation just uses the metadata to enumerate all
// methods and then calls SetJMCStatus on each method.
// This isn't great perf, but this should never be needed in a
// perf-critical situation.
//
// Parameters:
//    fIsUserCode - true to set entire class to user code. False to set to
//       non-user code.
//
// Returns:
//    S_OK on success. On failure, the user-code status of the methods in the
//      class is random.
//-----------------------------------------------------------------------------
HRESULT CordbClass::SetJMCStatus(BOOL fIsUserCode)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    // Get the member functions via a meta data interface
    CordbModule * pModule = GetModule();

    // Ensure that our process is in a sane state.
    CordbProcess * pProcess = pModule->GetProcess();
    _ASSERTE(pProcess != NULL);

    IMetaDataImport * pImport = NULL;
    HCORENUM phEnum = 0;

    HRESULT hr = S_OK;

    mdMethodDef rTokens[100];
    ULONG i;
    ULONG count;

    EX_TRY
    {
        pImport = pModule->GetMetaDataImporter();
        do
        {
            hr = pImport->EnumMethods(&phEnum, m_token, rTokens, ARRAY_SIZE(rTokens), &count);
            IfFailThrow(hr);

            for (i = 0; i < count; i++)
            {
                RSLockHolder lockHolder(pProcess->GetProcessLock());
                // Need the ICorDebugFunction to query for JMC status.
                CordbFunction * pFunction = pModule->LookupOrCreateFunctionLatestVersion(rTokens[i]);

                lockHolder.Release(); // Must release before sending an IPC event
                hr = pFunction->SetJMCStatus(fIsUserCode);
                IfFailThrow(hr);
            }
        }
        while (count > 0);

        _ASSERTE(SUCCEEDED(hr));
    }
    EX_CATCH_HRESULT(hr);

    if ((pImport != NULL) && (phEnum != 0))
    {
        pImport->CloseEnum(phEnum);
    }

    return hr;

}

//-----------------------------------------------------------------------------
// We have to go the EE to find out if a class is a value
// class or not.  This is because there is no flag for this, but rather
// it depends on whether the class subclasses System.ValueType (apart
// from System.Enum...).  Replicating all that resolution logic
// does not seem like a good plan.
//
// We also accept other "evidence" that the class is or isn't a VC, in
// particular:
//   - It is definitely a VC if it has been used after a
//     E_T_VALUETYPE in a signature.
//   - It is definitely not a VC if it has been used after a
//     E_T_CLASS in a signature.
//   - It is definitely a VC if it has been used in combination with
//     E_T_VALUETYPE in one of COM API operations that take both
//     a ICorDebugClass and a CorElementType (e.g. GetParameterizedType)
//
// !!!Note the following!!!!
//   - A class may still be a VC even if it has been
//     used in combination with E_T_CLASS in one of COM API operations that take both
//     a ICorDebugClass and a CorElementType (e.g. GetParameterizedType).
//     We allow the user of the API to specify E_T_CLASS when the VC status
//     is not known or is not important.
//
// Return Value:
//   indicates whether this is a value-class
//
// Notes:
//   Throws CORDBG_E_CLASS_NOT_LOADED or synchronization errors on failure
//-----------------------------------------------------------------------------
bool CordbClass::IsValueClass()
{
    INTERNAL_API_ENTRY(this);
    THROW_IF_NEUTERED(this);

	if (!m_fIsValueClassKnown)
    {
        ATT_REQUIRE_STOPPED_MAY_FAIL_OR_THROW(GetProcess(), ThrowHR);
        Init();
    }
    return m_fIsValueClass;
}

//-----------------------------------------------------------------------------
// Get a CordbType for the 'this' pointer of a method in a CordbClass.
// The 'this' pointer is argument #0 in an instance method.
//
// For ReferenceTypes (ELEMENT_TYPE_CLASS), the 'this' pointer is just a
// normal reference, and so GetThisType() behaves like GetParameterizedType().
// For ValueTypes, the 'this' pointer is a byref.
//
// Arguments:
//   pInst - instantiation info (eg, the type parameters) to produce CordbType
//   ppResultType - OUT: out parameter to hold outgoing CordbType.
//
// Returns:
//   S_OK on success. Else failure.
//
HRESULT CordbClass::GetThisType(const Instantiation * pInst, CordbType ** ppResultType)
{
    FAIL_IF_NEUTERED(this);

    HRESULT hr = S_OK;
    // Note: We have to call Init() here to find out if it really a VC or not.
    bool fIsValueClass = false;
    EX_TRY
    {
        fIsValueClass = IsValueClass();
    }
    EX_CATCH_HRESULT(hr);

    if (FAILED(hr))
    {
        return hr;
    }

    if (fIsValueClass)
    {
        CordbType *pType;

        hr = CordbType::MkType(GetAppDomain(),   // OK: this E_T_VALUETYPE will be normalized by MkType
                               ELEMENT_TYPE_VALUETYPE,
                               this,
                               pInst,
                               &pType);

        if (!SUCCEEDED(hr))
        {
            return hr;
        }

        hr = CordbType::MkType(GetAppDomain(), ELEMENT_TYPE_BYREF, 0, pType, ppResultType);

        if (!SUCCEEDED(hr))
        {
            return hr;
        }
    }
    else
    {
        hr = CordbType::MkType(GetAppDomain(), ELEMENT_TYPE_CLASS, this, pInst, ppResultType);

        if (!SUCCEEDED(hr))
        {
            return hr;
        }
    }

    return hr;
}


//-----------------------------------------------------------------------------
// Initialize the CordbClass.
// This will collect all the field information via the DAC, and also determine
// whether this Type is a ReferenceType or ValueType.
//
// Parameters:
//   fForceInit - if true, always reinitialize. If false, may skip
//     initialization if we believe we already have the info.
//
// Note:
//   Throws CORDBG_E_CLASS_NOT_LOADED on failure
//-----------------------------------------------------------------------------
void CordbClass::Init(ClassLoadLevel desiredLoadLevel)
{
    INTERNAL_SYNC_API_ENTRY(this->GetProcess());

    CordbProcess * pProcess = GetProcess();
    IDacDbiInterface* pDac = pProcess->GetDAC();

    // If we've done a continue since the last time we got hanging static fields,
    // we should clear out our cache, since everything may have moved.
    if (m_continueCounterLastSync < GetProcess()->m_continueCounter)
    {
        m_hangingFieldsStatic.Clear();
        m_continueCounterLastSync = GetProcess()->m_continueCounter;
    }

    if (m_loadLevel < desiredLoadLevel)
    {
        // reset everything
        m_loadLevel = Constructed;
        m_fIsValueClass = false;
        m_fIsValueClassKnown = false;
        m_fHasTypeParams = false;
        m_classInfo.Clear();
        // @dbgtodo Microsoft inspection: declare a constant to replace badbad
        m_classInfo.m_objectSize = 0xbadbad;
        VMPTR_TypeHandle vmTypeHandle = VMPTR_TypeHandle::NullPtr();

        // basic info load level
        if(desiredLoadLevel >= BasicInfo)
        {
            vmTypeHandle = pDac->GetTypeHandle(m_pModule->GetRuntimeModule(), GetToken());
            SetIsValueClass(pDac->IsValueType(vmTypeHandle));
            m_fHasTypeParams = !!pDac->HasTypeParams(vmTypeHandle);
            m_loadLevel = BasicInfo;
        }

        // full info load level
        if(desiredLoadLevel == FullInfo)
        {
            VMPTR_AppDomain vmAppDomain = VMPTR_AppDomain::NullPtr();
            VMPTR_DomainAssembly vmDomainAssembly = m_pModule->GetRuntimeDomainAssembly();
            if (!vmDomainAssembly.IsNull())
            {
                DomainAssemblyInfo info;
                pDac->GetDomainAssemblyData(vmDomainAssembly, &info);
                vmAppDomain = info.vmAppDomain;
            }
            pDac->GetClassInfo(vmAppDomain, vmTypeHandle, &m_classInfo);

            BOOL fGotUnallocatedStatic = GotUnallocatedStatic(&m_classInfo.m_fieldList);

            // if we have an unallocated static don't record that we reached FullInfo stage
            // this seems pretty ugly but I don't want to bite off cleaning this up just yet
            // Not saving the FullInfo stage effectively means future calls to Init() will
            // re-init everything and some parts of DBI may be depending on that re-initialization
            // with alternate data in order to operate correctly
            if(!fGotUnallocatedStatic)
                m_loadLevel = FullInfo;
        }
    }
} // CordbClass::Init

// determine if any fields for a type are unallocated statics
BOOL CordbClass::GotUnallocatedStatic(DacDbiArrayList<FieldData> * pFieldList)
{
    BOOL fGotUnallocatedStatic = FALSE;
    unsigned int count = 0;
    while ((count < pFieldList->Count()) && !fGotUnallocatedStatic )
    {
        if ((*pFieldList)[count].OkToGetOrSetStaticAddress() &&
            (*pFieldList)[count].GetStaticAddress() == NULL )
        {
            // The address for a regular static field isn't available yet
            // How can this happen?  Statics appear to get allocated during domain load.
            // There may be some laziness or a race-condition involved.
            fGotUnallocatedStatic = TRUE;
        }
        ++count;
    }
    return fGotUnallocatedStatic;
} // CordbClass::GotUnallocatedStatic

/*
 * FieldData::GetFieldSignature
 *
 * Get the field's full metadata signature. This may be cached, but for dynamic modules we'll always read it from
 * the metadata.
 *
 * Parameters:
 *    pModule - pointer to the module that contains the field
 *
 *    pSigParser - OUT: the full signature for the field.
 *
 * Returns:
 *    HRESULT for success or failure.
 *
 */
HRESULT FieldData::GetFieldSignature(CordbModule *pModule,
                                                  SigParser *pSigParser)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    INTERNAL_SYNC_API_ENTRY(pModule->GetProcess());

    HRESULT hr = S_OK;

    IMetaDataImport * pImport = pModule->GetMetaDataImporter(); // throws;

    PCCOR_SIGNATURE fieldSignature = NULL;
    ULONG size = ((ULONG) -1);

    _ASSERTE(pSigParser != NULL);

    // If the module is dynamic, there had better not be a cached field signature.
    _ASSERTE(!pModule->IsDynamic() || (m_fldSignatureCache == NULL));

    // If the field signature cache is null, or if this is a dynamic module, then go read the signature from the
    // matadata. We always read from the metadata for dynamic modules because our metadata blob is constantly
    // getting deleted and re-allocated. If we kept a pointer to the signature, we'd end up pointing to bad data.
    if (m_fldSignatureCache == NULL)
    {
        // Go to the metadata for all fields: previously the left-side tranferred over
        // single-byte signatures as part of the field info.  Since the left-side
        // goes to the metadata anyway, and we already fetch plenty of other metadata,
        // I don't believe that fetching it here instead of transferring it over
        // is going to slow things down at all, and
        // in any case will not be where the primary optimizations lie...

        IfFailRet(pImport->GetFieldProps(m_fldMetadataToken, NULL, NULL, 0, NULL, NULL,
                                                      &fieldSignature,
                                                      &size,
                                                      NULL, NULL, NULL));

        // Point past the calling convention
        CorCallingConvention conv;

        // Move pointer,
        BYTE * pOldPtr = (BYTE*) fieldSignature;
        conv = (CorCallingConvention) CorSigUncompressData(fieldSignature);
        _ASSERTE(conv == IMAGE_CEE_CS_CALLCONV_FIELD);
        size -= (ULONG) (((BYTE*) fieldSignature) - pOldPtr); // since we updated filedSignature, adjust size

        // Although the pointer will keep updating, the size should be the same. So we assert that.
        _ASSERTE((m_fldSignatureCacheSize == 0) || (m_fldSignatureCacheSize == size));

        // Cache the value for non-dynamic modules, so this is faster later.
        // Since we're caching in a FieldData, we can't store the actual SigParser object.
        if (!pModule->IsDynamic())
        {
            m_fldSignatureCache = fieldSignature;
            m_fldSignatureCacheSize = size;
        }
    }
    else
    {
        // We have a cached value, so return it. Note: we should never have a cached value for a field in a dynamic
        // module.
        CONSISTENCY_CHECK_MSGF((!pModule->IsDynamic()),
                               ("We should never cache a field signature in a dynamic module! Module=%p This=%p",
                                pModule, this));

        fieldSignature  = m_fldSignatureCache;
        size            = m_fldSignatureCacheSize;
    }

    _ASSERTE(fieldSignature != NULL);
    _ASSERTE(size != ((ULONG) -1));
    *pSigParser = SigParser(fieldSignature, size);
    return hr;
}

// CordbClass::InitEnCFieldInfo
// Initializes an instance of EnCHangingFieldInfo.
// Arguments:
//     input:  fStatic       - flag to indicate whether the EnC field is static
//             pObject       - For instance fields, the Object instance containing the sync-block.
//                             For static fields (if this is being called from GetStaticFieldValue) object is NULL.
//             fieldToken    - token for the EnC field
//             metadataToken - metadata token for this instance of CordbClass
//     output: pEncField     - the fields of this class will be appropriately initialized
void CordbClass::InitEnCFieldInfo(EnCHangingFieldInfo * pEncField,
                                  BOOL                  fStatic,
                                  CordbObjectValue *    pObject,
                                  mdFieldDef            fieldToken,
                                  mdTypeDef             classToken)
{
    IDacDbiInterface * pInterface = GetProcess()->GetDAC();

    if (fStatic)
    {
        // the field is static, we don't need any additional data
        pEncField->Init(VMPTR_Object::NullPtr(),      /* vmObject */
                        NULL,                         /* offsetToVars */
                        fieldToken,
                        ELEMENT_TYPE_MAX,
                        classToken,
                        m_pModule->GetRuntimeDomainAssembly());
    }
    else
    {
        // This is an instance field, we need to pass a bunch of type information back
        _ASSERTE(pObject != NULL);

        pEncField->Init(pInterface->GetObject(pObject->m_id),      // VMPTR to the object instance of interest.
                        pObject->GetInfo().objOffsetToVars,         // The offset from the beginning of the object
                                                                    // to the beginning of the fields. Fields added
                                                                    // with EnC don't actually reside in the object
                                                                    // (they hang off the sync block instead), so
                                                                    // this is used to compute the returned field
                                                                    // offset (fieldData.m_fldInstanceOffset). This
                                                                    // makes it appear to be an offset from the object.
                                                                    // Ideally we wouldn't do any of this, and just
                                                                    // explicitly deal with absolute addresses (instead
                                                                    // of "offsets") for EnC hanging instance fields.
                        fieldToken,                                 // Field token for the added field.
                        pObject->GetInfo().objTypeData.elementType, // An indication of the type of object to which
                                                                    // we're adding a field (specifically,
                                                                    // whether it's a value type or a class).
                                                                    // This is used only for log messages, and could
                                                                    // be removed.
                        classToken,                                 // metadata token for the class
                        m_pModule->GetRuntimeDomainAssembly());         // Domain file for the class
    }
} // CordbClass::InitFieldData

// CordbClass::GetEnCFieldFromDac
// Get information via the DAC about a field added with Edit and Continue.
// Arguments:
//     input: fStatic       - flag to indicate whether the EnC field is static
//            pObject       - For instance fields, the Object instance containing the sync-block.
//                            For static fields (if this is being called from GetStaticFieldValue) object is NULL.
//            fieldToken    - token for the EnC field
//     output: pointer to an initialized instance of FieldData that has been added to the appropriate table
//     in our cache
FieldData * CordbClass::GetEnCFieldFromDac(BOOL               fStatic,
                                           CordbObjectValue * pObject,
                                           mdFieldDef         fieldToken)
{
    EnCHangingFieldInfo encField;
    mdTypeDef           metadataToken;
    FieldData           fieldData,
                      * pInfo = NULL;
    BOOL                fDacStatic;
    CordbProcess *      pProcess = GetModule()->GetProcess();

    _ASSERTE(pProcess != NULL);
    IfFailThrow(GetToken(&metadataToken));
    InitEnCFieldInfo(&encField, fStatic, pObject, fieldToken, metadataToken);

    // Go get this particular field.
    pProcess->GetDAC()->GetEnCHangingFieldInfo(&encField, &fieldData, &fDacStatic);
    _ASSERTE(fStatic == fDacStatic);

    // Save the field results in our cache and get a stable pointer to the data
    if (fStatic)
    {
        pInfo = m_hangingFieldsStatic.AddFieldInfo(&fieldData);
    }
    else
    {
        pInfo = pObject->GetHangingFieldTable()->AddFieldInfo(&fieldData);
    }

    // We should have a fresh copy of the data (don't want to return a pointer to data on our stack)
    _ASSERTE((void *)pInfo != (void *)&fieldData);
    _ASSERTE(pInfo->m_fFldIsStatic == (fStatic == TRUE));
    _ASSERTE(pInfo->m_fldMetadataToken == fieldToken);

    // Pass a pointer to the data out.
    return pInfo;
} // CordbClass::GetEnCFieldFromDac

//-----------------------------------------------------------------------------
// Internal helper to get a FieldData for fields added by EnC after the type
// was loaded.  Since object and MethodTable layout has already been fixed,
// such added fields are "hanging" off some other data structure.  For instance
// fields, they're stored in a syncblock off the object.  For static fields
// they're stored off the EnCFieldDesc.
//
// The caller must have already determined this is a hanging field (i.e.
// GetFieldInfo returned CORDBG_E_ENC_HANGING_FIELDF).
//
// Arguments:
//     input:  fldToken - field of interest to get.
//             pObject  - For instance fields, the Object instance containing the sync-block.
//                        For static fields (if this is being called from GetStaticFieldValue) object is NULL.
//     output: ppFieldData - the FieldData matching the fldToken.
//
// Returns:
//   S_OK on success, failure code otherwise.
//-----------------------------------------------------------------------------
HRESULT CordbClass::GetEnCHangingField(mdFieldDef fldToken,
                                      FieldData **ppFieldData,
                                       CordbObjectValue * pObject)
{
    FAIL_IF_NEUTERED(this);
    INTERNAL_SYNC_API_ENTRY(GetProcess());

    HRESULT hr = S_OK;
    _ASSERTE(pObject == NULL || !pObject->IsNeutered() );

    if (HasTypeParams())
    {
        _ASSERTE(!"EnC hanging field not yet implemented on constructed types!");
        return E_FAIL;
    }

    // This must be a static field if no object was supplied
    BOOL fStatic = (pObject == NULL);

    // Look for cached field information
    FieldData *pInfo = NULL;
    if (fStatic)
    {
        // Static fields should _NOT_ be cleared, since they stick around.  Thus
        // the separate tables.
        pInfo = m_hangingFieldsStatic.GetFieldInfo(fldToken);
    }
    else
    {
        // We must get new copies each time we call continue b/c we get the
        // actual Object ptr from the left side, which can move during a GC.
        pInfo = pObject->GetHangingFieldTable()->GetFieldInfo(fldToken);
    }

    // We've found a previously located entry
    if (pInfo != NULL)
    {
        *ppFieldData = pInfo;
        return S_OK;
    }

    // Field information not already available - go get it
    EX_TRY
    {

        // We're not going to be able to get the instance-specific field
        // if we can't get the instance.
        if (!fStatic && pObject->GetInfo().objRefBad)
        {
            ThrowHR(CORDBG_E_INVALID_OBJECT);
        }

        *ppFieldData = GetEnCFieldFromDac(fStatic, pObject, fldToken);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

//-----------------------------------------------------------------------------
// Get a FieldData (which rich information, including details about storage)
//  from a metadata token.
//
// Parameters:
//   fldToken - incoming metadata token specifying the field.
//   ppFieldData - OUT: resulting FieldData structure.
//
// Returns:
//   S_OK on success. else failure.
//-----------------------------------------------------------------------------
HRESULT CordbClass::GetFieldInfo(mdFieldDef fldToken, FieldData **ppFieldData)
{
    INTERNAL_SYNC_API_ENTRY(GetProcess());

    Init();
    return SearchFieldInfo(GetModule(), &m_classInfo.m_fieldList, m_token, fldToken, ppFieldData);
}


//-----------------------------------------------------------------------------
// Search an array of FieldData (pFieldList) for a field (fldToken).
// The FieldData array must match the class supplied by classToken, and live
// in the supplied module.
//
// Internal helper used by CordbType::GetFieldInfo, CordbClass::GetFieldInfo
//
// Parameters:
//    module -     module containing the class that the FieldData array matches.
//    pFieldList - array of fields to search through and the number of elements in
//                 the array.
//    classToken - class that the data array matches. class must live in
//                 the supplied moudle.
//    fldToken -   metadata token of the field to search for. This field should be
//                 on the class supplied by classToken.
//
// Returns:
//    CORDBG_E_ENC_HANGING_FIELD for "hanging fields" (fields added via Enc) (common error).
//    Returns S_OK, set ppFieldData = pointer into data array for matching field. (*retval)->m_fldMetadataToken == fldToken)
//    Throws on other errors.
//-----------------------------------------------------------------------------
/* static */
HRESULT CordbClass::SearchFieldInfo(
    CordbModule * pModule,
    DacDbiArrayList<FieldData> * pFieldList,
    mdTypeDef classToken,
    mdFieldDef fldToken,
    FieldData **ppFieldData
)
{
    unsigned int i;

    IMetaDataImport * pImport = pModule->GetMetaDataImporter(); // throws

    HRESULT hr = S_OK;
    for (i = 0; i < pFieldList->Count(); i++)
    {
        if ((*pFieldList)[i].m_fldMetadataToken == fldToken)
        {
            // If the storage for this field isn't yet available (i.e. it is newly added with EnC)
            if (!(*pFieldList)[i].m_fFldStorageAvailable)
            {
                // If we're a static literal, then return special HR to let
                // debugger know that it should look it up via the metadata.
                // Check m_fFldIsStatic first b/c that's fast.
                if ((*pFieldList)[i].m_fFldIsStatic)
                {
                    if (IsFieldStaticLiteral(pImport, fldToken))
                    {
                        ThrowHR(CORDBG_E_VARIABLE_IS_ACTUALLY_LITERAL);
                    }
                }

                // This is a field added by EnC, caller needs to get instance-specific info.
                return CORDBG_E_ENC_HANGING_FIELD;
            }

            *ppFieldData = &((*pFieldList)[i]);
            return S_OK;
        }
    }

    // Hmmm... we didn't find the field on this class. See if the field really belongs to this class or not.
    mdTypeDef classTok;

    hr = pImport->GetFieldProps(fldToken, &classTok, NULL, 0, NULL, NULL, NULL, 0, NULL, NULL, NULL);
    IfFailThrow(hr);

    if (classTok == (mdTypeDef) classToken)
    {
        // Well, the field belongs in this class. The assumption is that the Runtime optimized the field away.
        ThrowHR(CORDBG_E_FIELD_NOT_AVAILABLE);
    }

    // Well, the field doesn't even belong to this class...
    ThrowHR(E_INVALIDARG);
}
