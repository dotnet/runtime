// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: rstype.cpp
//

//
// Define implementation of ICorDebugType
//*****************************************************************************


#include "stdafx.h"
#include "winbase.h"
#include "corpriv.h"


//-----------------------------------------------------------------------------
// Public method to get the static field from a type.
//
// Parameters:
//   fieldDef - metadata token for which field on this type to retrieve.
//   pFrame - context for Thread/AppDomains statics.
//   ppValue - OUT: out-parameter to get value.
//
// Returns:
//   S_OK on success.
//
HRESULT CordbType::GetStaticFieldValue(mdFieldDef fieldDef,
                                       ICorDebugFrame * pFrame,
                                       ICorDebugValue ** ppValue)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppValue, ICorDebugValue **);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    HRESULT hr = S_OK;

    IMetaDataImport * pImport = NULL;

    EX_TRY
    {
        // Ensure we were actually passed a mdFieldDef. This is especially useful to protect
        // against an accidental mdPropertyDef, because properties look like fields.
        //
        if (TypeFromToken(fieldDef) != mdtFieldDef)
        {
            ThrowHR(E_INVALIDARG);
        }

        pImport = m_pClass->GetModule()->GetMetaDataImporter(); // throws

        if (((m_elementType != ELEMENT_TYPE_CLASS) && (m_elementType != ELEMENT_TYPE_VALUETYPE)) || (m_pClass == NULL))
        {
            ThrowHR(E_INVALIDARG);
        }



        BOOL fSyncBlockField = FALSE;

        // If non generic type, then degenerate to CordbClass implementation.
        if (m_inst.m_cInst == 0)
        {
            hr = m_pClass->GetStaticFieldValue(fieldDef, pFrame, ppValue);
        }
        else
        {
            *ppValue = NULL;

            // Validate the token.
            if (!pImport->IsValidToken(fieldDef))
            {
                ThrowHR(hr = E_INVALIDARG);
            }

            // Make sure we have enough info about the class.
            hr = Init(FALSE);
            IfFailThrow(hr);

            // Lookup the field given its metadata token.
            FieldData * pFieldData;

            hr = GetFieldInfo(fieldDef, &pFieldData);

            if (hr == CORDBG_E_ENC_HANGING_FIELD)
            {
                // Generics + EnC is Not supported.
                hr = CORDBG_E_STATIC_VAR_NOT_AVAILABLE;
            }

            IfFailThrow(hr);

            hr = CordbClass::GetStaticFieldValue2(m_pClass->GetModule(),
                                                  pFieldData,
                                                  fSyncBlockField,
                                                  &m_inst,
                                                  pFrame,
                                                  ppValue);
            // fall through to translate HR
        }

    }
    EX_CATCH_HRESULT(hr);
    if (pImport != NULL)
    {
        hr = CordbClass::PostProcessUnavailableHRESULT(hr, pImport, fieldDef);
    }
    return hr;

}

// Combine E_T_s and rank together to get an id for the m_sharedtypes table
#define CORDBTYPE_ID(elementType,rank) (((unsigned int) (elementType)) * ((rank) + 1) + 1)


//-----------------------------------------------------------------------------
// Constructor
// Builds a CordbType around a primitive.
//-----------------------------------------------------------------------------
CordbType::CordbType(CordbAppDomain *appdomain, CorElementType et, unsigned int rank)
: CordbBase(appdomain->GetProcess(), CORDBTYPE_ID(et,rank) , enumCordbType),
  m_elementType(et),
  m_appdomain(appdomain),
  m_pClass(NULL),
  m_rank(rank),
  m_spinetypes(2),
  m_objectSize(0),
  m_fieldInfoNeedsInit(TRUE)
{
    m_typeHandleExact = VMPTR_TypeHandle::NullPtr();

    _ASSERTE(m_elementType != ELEMENT_TYPE_VALUETYPE);

    HRESULT hr = S_OK;
    EX_TRY
    {
        m_appdomain->AddToTypeList(this);
    }
    EX_CATCH_HRESULT(hr);
    SetUnrecoverableIfFailed(GetProcess(), hr);
}

//-----------------------------------------------------------------------------
// Constructor
// Builds a CordbType around a class. This is an Open CordbType.
// For a generic type, this CordbType will not have the generic parameters,
// but it will be a subordinate type to another Closed (instantiated) CordbType
//-----------------------------------------------------------------------------
CordbType::CordbType(CordbAppDomain *appdomain, CorElementType et, CordbClass *cls)
: CordbBase(appdomain->GetProcess(), et, enumCordbType),
  m_elementType(et),
  m_appdomain(appdomain),
  m_pClass(cls),
  m_rank(0),
  m_spinetypes(2),
  m_objectSize(0),
  m_fieldInfoNeedsInit(TRUE)
{
    m_typeHandleExact = VMPTR_TypeHandle::NullPtr();
    _ASSERTE(m_elementType != ELEMENT_TYPE_VALUETYPE);

    HRESULT hr = S_OK;
    EX_TRY
    {
        m_appdomain->AddToTypeList(this);
    }
    EX_CATCH_HRESULT(hr);
    SetUnrecoverableIfFailed(GetProcess(), hr);
}

//-----------------------------------------------------------------------------
// Constructor
// Builds a Partial-Type, instantiation is tycon's instantation plus tyarg.
// Eg, if tycon is "Dict<int>", and tyarg is "string", then this yields
// "Dict<int, string>"
//-----------------------------------------------------------------------------
CordbType::CordbType(CordbType *tycon, CordbType *tyarg)
: CordbBase(tycon->GetProcess(), (UINT_PTR)tyarg, enumCordbType),
  m_elementType(tycon->m_elementType),
  m_appdomain(tycon->m_appdomain),
  m_pClass(tycon->m_pClass),
  m_rank(tycon->m_rank),
  m_spinetypes(2),
  m_objectSize(0),
  m_fieldInfoNeedsInit(TRUE)
    // tyarg is added as part of instantiation -see below...
{
    m_typeHandleExact = VMPTR_TypeHandle::NullPtr();
    _ASSERTE(m_elementType != ELEMENT_TYPE_VALUETYPE);

    HRESULT hr = S_OK;
    EX_TRY
    {
        m_appdomain->AddToTypeList(this);
    }
    EX_CATCH_HRESULT(hr);
    SetUnrecoverableIfFailed(GetProcess(), hr);
}


ULONG STDMETHODCALLTYPE CordbType::AddRef()
{
    // This AddRef/Release pair creates a weak ref-counted reference to the class for this
    // type.  This avoids a circularity in ref-counted references between
    // classes and types - if we had a circularity the objects would never get
    // collected at all...
    //if (m_class)
    //  m_class->AddRef();
    return (BaseAddRef());
}
ULONG STDMETHODCALLTYPE CordbType::Release()
{
    //  if (m_class)
    //  m_class->Release();
    return (BaseRelease());
}

/*
    A list of which resources owened by this object are accounted for.

    HANDLED:
        CordbClass *m_class;  Weakly referenced by increasing count directly in AddRef() and Release()
        Instantiation   m_inst; // Internal pointers to CordbClass released in CordbClass::Neuter
        CordbHashTable   m_spinetypes; // Neutered
        CordbHashTable   m_fields; // Deleted in ~CordbType
*/

//-----------------------------------------------------------------------------
// Cleanup memory for CordbTypes.
//-----------------------------------------------------------------------------
CordbType::~CordbType()
{
    _ASSERTE(IsNeutered());
}

//-----------------------------------------------------------------------------
// Neutered by CordbModule
// See CordbBase::Neuter for neuter semantics.
//-----------------------------------------------------------------------------
void CordbType::Neuter()
{
    _ASSERTE(GetProcess()->GetProcessLock()->HasLock());

    // We have some direct releases below. If we call Neuter twice, that could
    // result in double-releases. So check if we're already neutered, and
    // if so, no work left to do.
    if (IsNeutered())
    {
        return;
    }

    for (unsigned int i = 0; i < m_inst.m_cInst; i++)
    {
        m_inst.m_ppInst[i]->Release();
    }

    m_spinetypes.NeuterAndClear(GetProcess()->GetProcessLock());

    if(m_inst.m_ppInst)
    {
        delete [] m_inst.m_ppInst;
        m_inst.m_ppInst = NULL;
    }
    m_fieldList.Dealloc();

    CordbBase::Neuter();
}

//-----------------------------------------------------------------------------
// Public method for IUnknown::QueryInterface.
// Has standard QI semantics.
//-----------------------------------------------------------------------------
HRESULT CordbType::QueryInterface(REFIID id, void **pInterface)
{
    if (id == IID_ICorDebugType)
        *pInterface = static_cast<ICorDebugType*>(this);
    else if (id == IID_ICorDebugType2)
        *pInterface = static_cast<ICorDebugType2*>(this);
    else if (id == IID_IUnknown)
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugType*>(this));
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
}


//-----------------------------------------------------------------------------
// Make a simple type with no type arguments by specifying a CorElementType,
// e.g. ELEMENT_TYPE_I1
//
// CordbType's are effectively a full representation of
// structured types.  They are hashed via a combination of their constituent
// elements (e.g. CordbClass's or CordbType's) and the element type that is used to
// combine the elements, or if they have no elements then via
// the element type alone.  The following  is used to create all CordbTypes.
//
// An AppDomain holds a cache of CordbTypes for each of the basic CorElementTypes.
//
// Arguments:
//   pAppDomain - the AppDomain that the type lives in.
//   elementType - element_type to create the CordbType around.
//   ppResultType - OUT: out-parameter to get the CordbType.
//
// Returns:
//   S_OK on success.
//
//
HRESULT CordbType::MkType(CordbAppDomain * pAppDomain,
                          CorElementType elementType,
                          CordbType ** ppResultType)
{
    _ASSERTE(pAppDomain != NULL);
    _ASSERTE(ppResultType != NULL);

    RSLockHolder lockHolder(pAppDomain->GetProcess()->GetProcessLock());

    // Some points in the code create types via element types that are clearly objects but where
    // no further information is given.  This is always done when creating a CordbValue, prior
    // to actually going over to the EE to discover what kind of value it is.  In all these
    // cases we can just use the type for "Object" - the code for dereferencing the value
    // will update the type correctly once it has been determined.  We don't do this for ELEMENT_TYPE_STRING
    // as that is actually a NullaryType and at other places in the code we will want exactly that type!
    if ((elementType == ELEMENT_TYPE_CLASS) ||
        (elementType == ELEMENT_TYPE_SZARRAY) ||
        (elementType == ELEMENT_TYPE_ARRAY))
    {
        elementType = ELEMENT_TYPE_OBJECT;
    }

    switch (elementType)
    {
    // this one is included because we need a "seed" type to uniquely hash FNPTR types,
    // i.e. the nullary FNPTR type is used as the type constructor for all function pointer types,
    // when combined with an appropriate instantiation.
    case ELEMENT_TYPE_FNPTR:
        // fall through ...

    case ELEMENT_TYPE_VOID:
    case ELEMENT_TYPE_BOOLEAN:
    case ELEMENT_TYPE_CHAR:
    case ELEMENT_TYPE_I1:
    case ELEMENT_TYPE_U1:
    case ELEMENT_TYPE_I2:
    case ELEMENT_TYPE_U2:
    case ELEMENT_TYPE_I4:
    case ELEMENT_TYPE_U4:
    case ELEMENT_TYPE_I8:
    case ELEMENT_TYPE_U8:
    case ELEMENT_TYPE_R4:
    case ELEMENT_TYPE_R8:
    case ELEMENT_TYPE_STRING:
    case ELEMENT_TYPE_OBJECT:
    case ELEMENT_TYPE_TYPEDBYREF:
    case ELEMENT_TYPE_I:
    case ELEMENT_TYPE_U:

        *ppResultType = pAppDomain->m_sharedtypes.GetBase(CORDBTYPE_ID(elementType, 0));

        if (*ppResultType == NULL)
        {
            CordbType * pNewType = new (nothrow) CordbType(pAppDomain, elementType, (unsigned int) 0);

            if (pNewType == NULL)
            {
                return E_OUTOFMEMORY;
            }

            HRESULT hr = pAppDomain->m_sharedtypes.AddBase(pNewType);

            if (SUCCEEDED(hr))
            {
                *ppResultType = pNewType;
            }
            else
            {
                _ASSERTE(!"unexpected failure!");
                delete pNewType;
            }

            return hr;
        }
        return S_OK;

    default:
        _ASSERTE(!"unexpected element type!");
        return E_FAIL;
    }

}

//-----------------------------------------------------------------------------
// Internal method to make a type with exactly one type argument by specifying
// ELEMENT_TYPE_PTR, ELEMENT_TYPE_BYREF, ELEMENT_TYPE_SZARRAY or
// ELEMENT_TYPE_ARRAY.
//
// Arguments:
//   pAppDomain - appdomain containing the type.
//   elementType - element type to create around. This is limited to: ELEMENT_TYPE_PTR,
//          ELEMENT_TYPE_BYREF, ELEMENT_TYPE_SZARRAY or ELEMENT_TYPE_ARRAY.
//   rank - for non-arrays, this must be 0. For szarray, this must be 1.
//          For multi-dimensional arrays, this is the rank.
//   pType - the single input type-parameter required for the specified element type.
//   ppResultType - OUT: the output parameter to get the corresponding CordbType
//
// Returns:
//   S_OK on success.
//
HRESULT CordbType::MkType(CordbAppDomain *pAppDomain,
                          CorElementType elementType,
                          ULONG rank,
                          CordbType * pType,
                          CordbType ** ppResultType)
{
    _ASSERTE(pAppDomain != NULL);
    _ASSERTE(ppResultType != NULL);

    RSLockHolder lockHolder(pAppDomain->GetProcess()->GetProcessLock());

    switch (elementType)
    {

    case ELEMENT_TYPE_PTR:
    case ELEMENT_TYPE_BYREF:
        _ASSERTE(rank == 0);
        goto LUnary;

    case ELEMENT_TYPE_SZARRAY:
        _ASSERTE(rank == 1);
        goto LUnary;

    case ELEMENT_TYPE_ARRAY:
LUnary:
        {
            CordbType * pFoundType = pAppDomain->m_sharedtypes.GetBase(CORDBTYPE_ID(elementType, rank));

            if (pFoundType == NULL)
            {
                pFoundType = new (nothrow) CordbType(pAppDomain, elementType, rank);

                if (pFoundType == NULL)
                {
                    return E_OUTOFMEMORY;
                }

                HRESULT hr = pAppDomain->m_sharedtypes.AddBase(pFoundType);

                if (FAILED(hr))
                {
                    _ASSERTE(!"unexpected failure!");
                    delete pFoundType;
                    return hr;
                }
            }

            Instantiation inst(1, &pType);

            return MkTyAppType(pAppDomain, pFoundType, &inst, ppResultType);

        }

    default:
        _ASSERTE(!"unexpected element type!");
        return E_FAIL;
    }

}

//-----------------------------------------------------------------------------
// Internal method to make a type for an instantiation of a class or value type, or just for the
// class or value type if it accepts no type parameters.
// Creates a CordbType instantiation around an uninstantiated CordbType and TypeParameter list.
// In other words, this does:
// CordbType(List<T>) + Instantiation({T=int}) --> CordbType(List<int>)
//
// This will create the subordinate types. Eg, for Triple<x,y,z>, it will create:
// CordbType(Triple<x>), CordbType(Triple<x,y>), and CordbType(Triple<x,y,z)).
// The fully instantiated one (the last one) is returned via the out parameter *pRes.
//
// Arguments:
//   pAppDomain - the appdomain that the type lives in.
//   pType - the open type to instantiate. Eg, CordbType(List<T>)
//   pInst - instantiation parameters.
//   ppResultType - OUT: out parameter to hold resulting type.
//
// Returns:
//  S_OK on success.
//
HRESULT CordbType::MkTyAppType(CordbAppDomain * pAppDomain,
                               CordbType * pType,
                               const Instantiation * pInst,
                               CordbType ** ppResultType)
{
    _ASSERTE(pAppDomain == pType->GetAppDomain());

    CordbType * pCordbType = pType;

    // Loop through and create each of the subordinate types, building up to the final fully Closed type.
    for (unsigned int i = 0; i < pInst->m_cClassTyPars; i++)
    {

        CordbType * pCordbBaseType = pCordbType->m_spinetypes.GetBase((UINT_PTR) (pInst->m_ppInst[i]));

        if (pCordbBaseType == NULL)
        {
            pCordbBaseType = new (nothrow) CordbType(pCordbType, pInst->m_ppInst[i]);

            if (pCordbBaseType == NULL)
            {
                return E_OUTOFMEMORY;
            }

            HRESULT hr = pCordbType->m_spinetypes.AddBase(pCordbBaseType);

            if (FAILED(hr))
            {
                _ASSERTE(!"unexpected failure!");
                delete pCordbBaseType;
                // @dbgtodo Microsoft leaks: Release the previously created types if this fails later in the loop
                return hr;
            }

            pCordbBaseType->m_inst.m_cInst = i + 1;
            pCordbBaseType->m_inst.m_cClassTyPars = i + 1;
            pCordbBaseType->m_inst.m_ppInst = new (nothrow) CordbType *[i+1];

            if (pCordbBaseType->m_inst.m_ppInst == NULL)
            {
                delete pCordbBaseType;
                // @dbgtodo Microsoft leaks: Doesn't release the previously created types if this fails later in the loop
                return E_OUTOFMEMORY;
            }

            for (unsigned int j = 0; j < (i + 1); j++)
            {
                // Constructed types include pointers across to other types - increase
                // the reference counts on these....
                pInst->m_ppInst[j]->AddRef();

                pCordbBaseType->m_inst.m_ppInst[j] = pInst->m_ppInst[j];
            }
        }
        pCordbType = pCordbBaseType;
    }

    *ppResultType = pCordbType;
    return S_OK;
}

//-----------------------------------------------------------------------------
// Creates a CordbType instantation around a cordbClass and TypeParameter list.
// In other words, this does:
// CordbClass(List<T>) + Instantiation({T=int}) --> CordbType(List<int>)
//
// This really just converts CordbClass(List<T>) --> CordbType(List<T>), and then calls CordbType::MkTyAppType
//
// Arguments:
//    pAppDomain - the AD that the class lives in.
//    elementType - element type of the class. Either ELEMENT_TYPE_CLASS or ELEMENT_TYPE_VALUETYPE
//    pClass - the uninstantiated class (eg, List<T>). This function will fill out the tycon->m_type field
//             to an uninstantiated CordbType (eg CordbType(List<T>))
//    pInst - the list of type parameters to instantiate with.
//    ppResultType - OUT: the CordbType instantiated with the type parameters (eg, CordbType(List<int>))
//
// Returns:
//   S_OK on success.
//
HRESULT CordbType::MkType(CordbAppDomain * pAppDomain,
                          CorElementType elementType,
                          CordbClass * pClass,
                          const Instantiation * pInst,
                          CordbType ** ppResultType)
{
    _ASSERTE(pAppDomain != NULL);
    _ASSERTE(ppResultType != NULL);

    switch (elementType)
    {
      // Normalize E_T_VALUETYPE away, so types do not record whether they are VCs or not, but CorDebugClass does.
      // Update our view of whether a class is a VC based on the evidence we have here.
    case ELEMENT_TYPE_VALUETYPE:

      _ASSERTE(((pClass != NULL) && (!pClass->IsValueClassKnown() || pClass->IsValueClassNoInit())) ||
               !"A non-value class is being used with ELEMENT_TYPE_VALUETYPE");

      pClass->SetIsValueClass(true);
      pClass->SetIsValueClassKnown(true);
      FALLTHROUGH;

    case ELEMENT_TYPE_CLASS:
        {
            // This probably isn't needed...
            if (pClass == NULL)
            {
                elementType = ELEMENT_TYPE_OBJECT;
                goto LReallyObject;
            }

            CordbType * pType = NULL;

            pType = pClass->GetType();

            if (pType == NULL)
            {
                pType = new (nothrow) CordbType(pAppDomain, ELEMENT_TYPE_CLASS, pClass);

                if (pType == NULL)
                {
                    return E_OUTOFMEMORY;
                }

                pClass->SetType(pType);
            }

            _ASSERTE(pClass->GetType() != NULL);

            return CordbType::MkTyAppType(pAppDomain, pType, pInst, ppResultType);
        }

    default:
LReallyObject:

        _ASSERTE(pInst->m_cInst == 0);
        return MkType(pAppDomain, elementType, ppResultType);

    }
}

//-----------------------------------------------------------------------------
// Make a CordbType for a function pointer type (ELEMENT_TYPE_FNPTR).
//
// Arguments:
//   pAppDomain - the Appdomain the type lives in.
//   elementType - must be ELEMENT_TYPE_FNPTR.
//   pInst - instantiation information.
//   ppResultType - OUT: out-parameter to hold resulting CordbType
//
// Return:
//   S_OK on success.
//
HRESULT CordbType::MkType(CordbAppDomain * pAppDomain,
                          CorElementType elementType,
                          const Instantiation * pInst,
                          CordbType ** ppResultType)
{
    CordbType * pType;

    _ASSERTE(elementType == ELEMENT_TYPE_FNPTR);

    HRESULT hr = MkType(pAppDomain, elementType, &pType);

    if (!SUCCEEDED(hr))
    {
        return hr;
    }
    return CordbType::MkTyAppType(pAppDomain, pType, pInst, ppResultType);
}


//-----------------------------------------------------------------------------
// Public API to get the CorElementType of the type.
//
// Parameters:
//    pType - OUT: on return, gets the CorElementType
//
// Returns:
//    S_OK on success. CORDBG_E_CLASS_NOT_LOADED or synchronization errors on failure
//-----------------------------------------------------------------------------
HRESULT CordbType::GetType(CorElementType *pType)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    // See if this E_T_CLASS is really a value type?
    if (m_elementType == ELEMENT_TYPE_CLASS)
    {
        _ASSERTE(m_pClass);
        bool isVC = false;
        // Determining if something is a VC or not can involve asking the EE.
        // We could do it ourselves based on the metadata but it's non-trivial
        // determining if a class has System.ValueType as a parent (we have
        // to find and OpenScope the System.Private.CoreLib.dll which we don't currently do
        // on the right-side).  But the IsValueClass call can fail if the
        // class is not yet loaded on the right side.  In that case we
        // ignore the failure and return ELEMENT_TYPE_CLASS
        HRESULT hr = S_OK;
        EX_TRY
        {
            isVC = m_pClass->IsValueClass();
        }
        EX_CATCH_HRESULT(hr);
        if (!FAILED(hr) && isVC)
        {
            *pType = ELEMENT_TYPE_VALUETYPE;
            return S_OK;
        }
    }
    *pType = m_elementType;
    return S_OK;
}

//-----------------------------------------------------------------------------
// Public method to get the ICorDebugClass that matches this type.
// ICorDebugType has instantiated type-params (eg, List<int>), whereas
// ICorDebugClass is open (eg, List<T>).
//
// Parameters:
//    pClass - OUT: gets class on return.
// Returns:
//    S_OK on success. CORDBG_E_CLASS_NOT_LOADED if the class is not loaded.
//    Else some other error.
//-----------------------------------------------------------------------------
HRESULT CordbType::GetClass(ICorDebugClass **pClass)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    if ((m_pClass == NULL) && (m_elementType == ELEMENT_TYPE_STRING ||
                               m_elementType == ELEMENT_TYPE_OBJECT))
    {
        Init(FALSE);
    }
    if (m_pClass == NULL)
    {
        *pClass = NULL;
        return CORDBG_E_CLASS_NOT_LOADED;
    }
    *pClass = m_pClass;
    m_pClass->ExternalAddRef();
    return S_OK;
}

//-----------------------------------------------------------------------------
// Public method to get array rank. This is only valid for arrays.
//
// Parameters:
//   pnRank - OUT: *pnRank is set to rank on return
//
// Return:
//   S_OK if success. E_INVALIDARG is this Type doesn't have a rank.
//-----------------------------------------------------------------------------
HRESULT CordbType::GetRank(ULONG32 *pnRank)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    VALIDATE_POINTER_TO_OBJECT(pnRank, ULONG32 *);

    if (m_elementType != ELEMENT_TYPE_SZARRAY &&
        m_elementType != ELEMENT_TYPE_ARRAY)
        return E_INVALIDARG;

    *pnRank = (ULONG32) m_rank;

    return S_OK;
}

//-----------------------------------------------------------------------------
// Public convenience method to get the first type parameter.
// This is purely to avoid needing to call EnumerateTypeParameters for
// the set of types that only have 1 type-parameter.
//
// Parameters:
//    pType - OUT: get the ICorDebugType for the first type-parameter.
// Returns:
//    S_OK on success.
//-----------------------------------------------------------------------------
HRESULT CordbType::GetFirstTypeParameter(ICorDebugType **pType)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    VALIDATE_POINTER_TO_OBJECT(pType, ICorDebugType **);

    // Since this is a public API, make sure there actually is at least 1 type-parameter.
    if (m_inst.m_cInst == 0)
    {
        return E_INVALIDARG;
    }

    _ASSERTE(m_inst.m_ppInst != NULL);
    _ASSERTE(m_inst.m_ppInst[0] != NULL);

    *pType = m_inst.m_ppInst[0];
    if (*pType)
        (*pType)->AddRef();
    return S_OK;
}

//-----------------------------------------------------------------------------
// Internal worker to create a CordbType around a CordbClass.
// Parameters:
//   appdomain - AD that the type lives in.
//   et - CorElementType of the incoming CordbClass
//   cl - CordbClass representing the type to build a CordbType for
//   pRes - OUT: out parameter to return the newly created CordbType object.
//
// Return:
//   S_OK on success.
//-----------------------------------------------------------------------------
HRESULT CordbType::MkUnparameterizedType(CordbAppDomain *appdomain, CorElementType et, CordbClass *cl,CordbType **pRes)
{
    // Pass in empty instantiation since CordbClass has no generic info.
    // We should make some assert between et and cl->GetType().
    Instantiation emptyInstantiation;

    return CordbType::MkType(appdomain, et, cl, &emptyInstantiation, pRes);
}


//-----------------------------------------------------------------------------
// Internal helper to get the First type parameter.
// This is an internal convenience function for the public GetFirstTypeParameter.
//
// Parameters:
//   pRes - OUT: out-param to get the unary type-parameter.
//-----------------------------------------------------------------------------
void
CordbType::DestUnaryType(CordbType **pRes)
{
    _ASSERTE(m_elementType == ELEMENT_TYPE_PTR
        || m_elementType == ELEMENT_TYPE_BYREF
        || m_elementType == ELEMENT_TYPE_ARRAY
        || m_elementType == ELEMENT_TYPE_SZARRAY);
    _ASSERTE(m_inst.m_cInst == 1);
    _ASSERTE(m_inst.m_ppInst != NULL);
    *pRes = m_inst.m_ppInst[0];
}



//-----------------------------------------------------------------------------
// Internal method to get the Class and type-parameters from a CordbType.
//-----------------------------------------------------------------------------
void
CordbType::DestConstructedType(CordbClass **cls, Instantiation *inst)
{
    ASSERT(m_elementType == ELEMENT_TYPE_CLASS);
    *cls = m_pClass;
    *inst = m_inst;
}

//-----------------------------------------------------------------------------
// Internal method to get all the type-parameters for a FnPtr
//-----------------------------------------------------------------------------
void
CordbType::DestNaryType(Instantiation *inst)
{
    ASSERT(m_elementType == ELEMENT_TYPE_FNPTR);
    *inst = m_inst;
}


//-----------------------------------------------------------------------------
// CordbType::SigToType
// Internal helper to create a CordbType from a Metadata signature (SigParser)
//
// This parses a metadata signature in the context of a module to return a CordbType.
// This heavily relies on the metadata and signature format. See ECMA Partition II for details.
// Since signatures may be recursive, this function can be called recursively.
// Since metadata signatures exist all over, this can be called in many different scenarios, including
// resolving a TypeSpec, looking up a field.
//
// pModule - module that the signature lives in.
// pSigParse - Signature, positioned at the point to read the Type from.
//          This will not change the SigParser's current position.
// inst - instantiation containing Type Params for the context of the SigParser.
//            For a local var or argument lookup, this would be the type-params from the Frame.
//            For a field lookup, this would be the type-params for the containing type.
// pRes - OUT: yields the CordbType for this signature.
//
// Returns:
//   S_OK on success
//-----------------------------------------------------------------------------
HRESULT
CordbType::SigToType(CordbModule * pModule,
                     SigParser * pSigParser,
                     const Instantiation * pInst,
                     CordbType ** ppResultType)
{
    FAIL_IF_NEUTERED(pModule);
    INTERNAL_SYNC_API_ENTRY(pModule->GetProcess());

    _ASSERTE(pSigParser != NULL);


    //
    // Make a local copy of the SigParser since we are going to mutate it.
    //
    SigParser sigParser = *pSigParser;

    CorElementType elementType;
    HRESULT hr;

    IfFailRet(sigParser.GetElemType(&elementType));

    switch (elementType)
    {
    case ELEMENT_TYPE_VAR:
    case ELEMENT_TYPE_MVAR:
        {
            uint32_t tyvar_num;

            IfFailRet(sigParser.GetData(&tyvar_num));


            if (elementType == ELEMENT_TYPE_VAR)
            {
                // ELEMENT_TYPE_VAR refers to an indexed type-parameter in the containing Type.
                // Eg, we may be doing a field lookup on 'List<T> { T m_head}', and the field's return type 'T' is Type-parameter #0.
                // Or this maybe part of a base class's TypeSpec.
                _ASSERTE (tyvar_num < (pInst->m_cClassTyPars));
                if (tyvar_num >= (pInst->m_cClassTyPars))
                    return E_FAIL;

                _ASSERTE (pInst->m_ppInst != NULL);
                *ppResultType = pInst->m_ppInst[tyvar_num];
            }
            else
            {
                //ELEMENT_TYPE_MVAR refers to an indexed type-parameter in the containing Method.
                // Eg, we may be in Class::Func<T> and refering to T.
                // The Instantiation array has Type type-parameters first, and then any Method Type-parameters.
                // The m_cClassTyPars field indicats where the split is between Type and Method type-parameters. Type type-params
                // come first.
                _ASSERTE(elementType == ELEMENT_TYPE_MVAR);


                _ASSERTE (tyvar_num < (pInst->m_cInst - pInst->m_cClassTyPars));
                if (tyvar_num >= (pInst->m_cInst - pInst->m_cClassTyPars))
                    return E_FAIL;

                _ASSERTE (pInst->m_ppInst != NULL);
                *ppResultType = pInst->m_ppInst[tyvar_num + pInst->m_cClassTyPars];
            }

            return S_OK;
        }
    case ELEMENT_TYPE_GENERICINST:
        {
            //ELEMENT_TYPE_GENERICINST is that start of a instantiated generic type.
            //Format for the signature blob is:
            //   1) CorElementType, Token - this is the uninstantiated type (eg, for Pair<int, string>, it would be token for Pair<T,U>)
            //   2) int - Count of generic args - eg, for Pair<T,U>, it would be "2".
            //   3) type1,type2, ... - meteadata representation for generic args. For example above, it would be Type(int), Type(string).


            // ignore "WITH", look at next ELEMENT_TYPE to get CLASS or VALUE

            IfFailRet(sigParser.GetElemType(&elementType));

            mdToken token;

            IfFailRet(sigParser.GetToken(&token));

            CordbClass * pClass;

            IfFailRet( pModule->ResolveTypeRefOrDef(token, &pClass));

            // The use of a class in a signature provides definite evidence as to whether it is a VC or not.
            _ASSERTE(!pClass->IsValueClassKnown() ||
                     (pClass->IsValueClassNoInit() ==  (elementType == ELEMENT_TYPE_VALUETYPE)) ||
                     !"A value class is being used with ELEMENT_TYPE_GENERICINST");

            pClass->SetIsValueClass(elementType ==  ELEMENT_TYPE_VALUETYPE);
            pClass->SetIsValueClassKnown(true);

            // Build up the array of generic arguments.
            uint32_t cArgs; // number of generic arguments in the type.

            IfFailRet(sigParser.GetData(&cArgs));

            S_UINT32 allocSize = S_UINT32( cArgs ) * S_UINT32( sizeof(CordbType *) );

            if (allocSize.IsOverflow())
            {
                IfFailRet(E_OUTOFMEMORY);
            }

            CordbType ** ppTypeInstantiations = reinterpret_cast<CordbType **>(_alloca( allocSize.Value()));

            for (unsigned int i = 0; i < cArgs;i++)
            {
                IfFailRet(CordbType::SigToType(pModule, &sigParser, pInst, &ppTypeInstantiations[i]));

                IfFailRet(sigParser.SkipExactlyOne());
            }

            // Now we have the Open type (eg, Pair<T,U>) and the instantiation list, so create the Closed CordbType..
            Instantiation typeInstantiation(cArgs, ppTypeInstantiations);

            return CordbType::MkType(pModule->GetAppDomain(), elementType, pClass, &typeInstantiation, ppResultType);
        }
    case ELEMENT_TYPE_CLASS:
    case ELEMENT_TYPE_VALUETYPE:  // OK: this E_T_VALUETYPE comes from signature
        {
            // Path for non-generic types

            mdToken token;

            IfFailRet(sigParser.GetToken(&token));

            CordbClass * pClass;

            IfFailRet(pModule->ResolveTypeRefOrDef(token, &pClass));

            // The use of a class in a signature provides definite evidence as to whether it is a VC or not.

            _ASSERTE(!pClass->IsValueClassKnown() ||
                     (pClass->IsValueClassNoInit() ==  (elementType == ELEMENT_TYPE_VALUETYPE)) ||
                     !"A non-value class is being used with ELEMENT_TYPE_VALUETYPE");

            pClass->SetIsValueClass(elementType ==  ELEMENT_TYPE_VALUETYPE);
            pClass->SetIsValueClassKnown(true);

            return CordbType::MkUnparameterizedType(pModule->GetAppDomain(), elementType, pClass, ppResultType);
        }
    case ELEMENT_TYPE_SENTINEL:
    case ELEMENT_TYPE_MODIFIER:
    case ELEMENT_TYPE_PINNED:
        {
            IfFailRet(CordbType::SigToType(pModule, &sigParser, pInst, ppResultType));
            // Throw away SENTINELS on all CordbTypes...
            return S_OK;
        }
    case ELEMENT_TYPE_CMOD_REQD:
    case ELEMENT_TYPE_CMOD_OPT:
        {
            mdToken token;

            IfFailRet(sigParser.GetToken(&token));

            IfFailRet(CordbType::SigToType(pModule, &sigParser, pInst, ppResultType));
            // Throw away CMOD on all CordbTypes...
            return S_OK;
        }

    case ELEMENT_TYPE_ARRAY:
        {
            CordbType * pType;

            IfFailRet(CordbType::SigToType(pModule, &sigParser, pInst, &pType));

            IfFailRet(sigParser.SkipExactlyOne());

            uint32_t rank;

            IfFailRet(sigParser.GetData(&rank));

            return CordbType::MkType(pModule->GetAppDomain(), elementType, rank, pType, ppResultType);
        }
    case ELEMENT_TYPE_SZARRAY:
        {
            CordbType * pType;

            IfFailRet(CordbType::SigToType(pModule, &sigParser, pInst, &pType));

            return CordbType::MkType(pModule->GetAppDomain(), elementType, 1, pType, ppResultType);
        }

    case ELEMENT_TYPE_PTR:
    case ELEMENT_TYPE_BYREF:
        {
            CordbType * pType;

            IfFailRet(CordbType::SigToType(pModule, &sigParser, pInst, &pType));

            return CordbType::MkType(pModule->GetAppDomain(),elementType, 0, pType, ppResultType);
        }

    case ELEMENT_TYPE_FNPTR:
        {
            uint32_t cArgs;

            IfFailRet(sigParser.GetData(&cArgs)); // Skip callingConv

            IfFailRet(sigParser.GetData(&cArgs)); // Get number of parameters

            S_UINT32 allocSize = ( S_UINT32(cArgs) + S_UINT32(1) ) * S_UINT32( sizeof(CordbType *) );

            if (allocSize.IsOverflow())
            {
                IfFailRet(E_OUTOFMEMORY);
            }

            CordbType ** ppTypeInstantiations = (CordbType **) _alloca( allocSize.Value() );

            for (unsigned int i = 0; i <= cArgs; i++)
            {
                IfFailRet(CordbType::SigToType(pModule, &sigParser, pInst, &ppTypeInstantiations[i]));

                IfFailRet(sigParser.SkipExactlyOne());
            }

            Instantiation typeInstantiation(cArgs + 1, ppTypeInstantiations);

            return CordbType::MkType(pModule->GetAppDomain(), elementType, &typeInstantiation, ppResultType);
        }

    case ELEMENT_TYPE_VOID:
    case ELEMENT_TYPE_BOOLEAN:
    case ELEMENT_TYPE_CHAR:
    case ELEMENT_TYPE_I1:
    case ELEMENT_TYPE_U1:
    case ELEMENT_TYPE_I2:
    case ELEMENT_TYPE_U2:
    case ELEMENT_TYPE_I4:
    case ELEMENT_TYPE_U4:
    case ELEMENT_TYPE_I8:
    case ELEMENT_TYPE_U8:
    case ELEMENT_TYPE_R4:
    case ELEMENT_TYPE_R8:
    case ELEMENT_TYPE_STRING:
    case ELEMENT_TYPE_TYPEDBYREF:
    case ELEMENT_TYPE_OBJECT:
    case ELEMENT_TYPE_I:
    case ELEMENT_TYPE_U:
        return CordbType::MkType(pModule->GetAppDomain(), elementType, ppResultType);

    default:
        _ASSERTE(!"unexpected element type!");
        return E_FAIL;
    }
} // CordbType::SigToType

//-----------------------------------------------------------------------------
// Marshal a DebuggerIPCE_BasicTypeData --> CordbType.
//
// This will build up a DebuggerIPCE_ExpandedTypeData and convert that into
//  a CordbType. This may send additional IPC events if needed to
// go from Basic --> Expanded data. Note that this is designed to handle generics.
//
// Parameters:
//   pAppDomain - the AppDomain the type lives in.
//   data - DebuggerIPCE_BasicTypeData from Left-Side containing type description.
//   pRes - OUT: out-parameter to hold built type.
//
// Returns:
//    S_OK on success.
//-----------------------------------------------------------------------------
HRESULT CordbType::TypeDataToType(CordbAppDomain *pAppDomain, DebuggerIPCE_BasicTypeData *data, CordbType **pRes)
{
    FAIL_IF_NEUTERED(pAppDomain);
    INTERNAL_SYNC_API_ENTRY(pAppDomain->GetProcess()); //



    HRESULT hr = S_OK;
    CorElementType et = data->elementType;
    switch (et)
    {
        case ELEMENT_TYPE_ARRAY:
        case ELEMENT_TYPE_SZARRAY:
        case ELEMENT_TYPE_PTR:
        case ELEMENT_TYPE_BYREF:
            // For these element types the "Basic" type data only contains the type handle.
            // So we fetch some more data, and the go onto the "Expanded" case...
            {
                EX_TRY
                {
                    DebuggerIPCE_ExpandedTypeData typeInfo;
                    CordbProcess * pProcess = pAppDomain->GetProcess();

                    {
                        RSLockHolder lockHolder(pProcess->GetProcessLock());
                        pProcess->GetDAC()->TypeHandleToExpandedTypeInfo(NoValueTypeBoxing,  // could be generics
                                                                                             // which are never boxed
                                                                         pAppDomain->GetADToken(),
                                                                         data->vmTypeHandle,
                                                                         &typeInfo);
                    }

                    IfFailThrow(CordbType::TypeDataToType(pAppDomain,&typeInfo, pRes));
                }
                EX_CATCH_HRESULT(hr);
                return hr;

            }

        case ELEMENT_TYPE_FNPTR:
            {
                DebuggerIPCE_ExpandedTypeData e;
                e.elementType = et;
                e.NaryTypeData.typeHandle = data->vmTypeHandle;
                return CordbType::TypeDataToType(pAppDomain, &e, pRes);
            }
        default:
            // For all other element types the "Basic" view of a type
            // contains the same information as the "expanded"
            // view, so just reuse the code for the Expanded view...
            DebuggerIPCE_ExpandedTypeData e;
            e.elementType = et;
            e.ClassTypeData.metadataToken = data->metadataToken;
            e.ClassTypeData.vmDomainAssembly = data->vmDomainAssembly;
            e.ClassTypeData.vmModule = data->vmModule;
            e.ClassTypeData.typeHandle = data->vmTypeHandle;
            return CordbType::TypeDataToType(pAppDomain, &e, pRes);
    }
}

//-----------------------------------------------------------------------------
// Marshal DebuggerIPCE_ExpandedTypeData --> CordbType
// The ExpandedTypeData just contains top level generic info, and so
// the RS may need to send more IPC events to fill out details.
//
// Parameters:
//   pAppDomain - the appdomain that all the types live in.
//   data - data used to build up CordbType
//   pRes - OUT: out param to get back CordbType on return.
//
// Returns:
//   S_OK on success.
//-----------------------------------------------------------------------------
HRESULT CordbType::TypeDataToType(CordbAppDomain *pAppDomain, DebuggerIPCE_ExpandedTypeData *data, CordbType **pRes)
{
    INTERNAL_SYNC_API_ENTRY(pAppDomain->GetProcess()); //

    CorElementType et = data->elementType;
    HRESULT hr = S_OK;
    switch (et)
    {

    case ELEMENT_TYPE_OBJECT:
    case ELEMENT_TYPE_VOID:
    case ELEMENT_TYPE_BOOLEAN:
    case ELEMENT_TYPE_CHAR:
    case ELEMENT_TYPE_I1:
    case ELEMENT_TYPE_U1:
    case ELEMENT_TYPE_I2:
    case ELEMENT_TYPE_U2:
    case ELEMENT_TYPE_I4:
    case ELEMENT_TYPE_U4:
    case ELEMENT_TYPE_I8:
    case ELEMENT_TYPE_U8:
    case ELEMENT_TYPE_R4:
    case ELEMENT_TYPE_R8:
    case ELEMENT_TYPE_STRING:
    case ELEMENT_TYPE_TYPEDBYREF:
    case ELEMENT_TYPE_I:
    case ELEMENT_TYPE_U:
ETObject:
        // It's a primitive (therefore non-generic) type, so we can just create it immediately.
        IfFailRet (CordbType::MkType(pAppDomain, et, pRes));
        break;

    case ELEMENT_TYPE_CLASS:
    case ELEMENT_TYPE_VALUETYPE:  // OK: this E_T_VALUETYPE comes from the EE
    {
        //
        if (data->ClassTypeData.metadataToken == mdTokenNil) {
            et = ELEMENT_TYPE_OBJECT;
            goto ETObject;
        }
        CordbModule * pClassModule = NULL;
        EX_TRY
        {
            pClassModule = pAppDomain->LookupOrCreateModule(data->ClassTypeData.vmModule, data->ClassTypeData.vmDomainAssembly);
        }
        EX_CATCH_HRESULT(hr);
        if( pClassModule == NULL )
        {
            // We don't know anything about this module - shouldn't happen.
            // <TODO>This can be hit by the issue described in VSWhidbey 465120</TODO>
            _ASSERTE(!"Unrecognized module");
            return CORDBG_E_MODULE_NOT_LOADED;
        }

        CordbClass *tycon;
        IfFailRet (pClassModule->LookupOrCreateClass(data->ClassTypeData.metadataToken,&tycon));
        if (!(data->ClassTypeData.typeHandle.IsNull()))
        {
            // It's a generic type. We have the typehandle, use that to query for the rest of
            // the tyeparameters and build up the instantiation for the CordbType.

            IfFailRet (CordbType::InstantiateFromTypeHandle(pAppDomain, data->ClassTypeData.typeHandle, et, tycon, pRes));
            // Set the type handle regardless of how we found
            // the type.  For example if type was already
            // constructed without the type handle still set
            // it here.
            if (*pRes)
            {
                (*pRes)->m_typeHandleExact = data->ClassTypeData.typeHandle;
            }
            break;
        }
        else
        {
            // Non generic type. Since we already have the CordbClass for it, we can trivially create the CordbType
            IfFailRet (CordbType::MkUnparameterizedType(pAppDomain, et,tycon,pRes));
            break;
        }

    }
    case ELEMENT_TYPE_ARRAY:
    case ELEMENT_TYPE_SZARRAY:
    {
        CordbType *argty;
        IfFailRet (CordbType::TypeDataToType(pAppDomain, &(data->ArrayTypeData.arrayTypeArg), &argty));
        IfFailRet (CordbType::MkType(pAppDomain, et, data->ArrayTypeData.arrayRank, argty, pRes));
        break;
    }

    case ELEMENT_TYPE_PTR:
    case ELEMENT_TYPE_BYREF:
    {
        CordbType *argty;
        IfFailRet (CordbType::TypeDataToType(pAppDomain, &(data->UnaryTypeData.unaryTypeArg), &argty));
        IfFailRet (CordbType::MkType(pAppDomain, et, 0, argty, pRes));
        break;
    }
    case ELEMENT_TYPE_FNPTR:
    {
        IfFailRet (CordbType::InstantiateFromTypeHandle(pAppDomain, data->NaryTypeData.typeHandle, et, NULL, pRes));
        if (*pRes)
        {
            (*pRes)->m_typeHandleExact = data->NaryTypeData.typeHandle;
        }
        break;
    }
    case ELEMENT_TYPE_END:
        *pRes = NULL;
        return E_FAIL;

    default:
        _ASSERTE(!"unexpected element type!");
        return E_FAIL;

    }
    return S_OK;
}

//-----------------------------------------------------------------------------
// CordbType::InstantiateFromTypeHandle
// Internal helper method.
// Builds (Left-Side) TypeHandle --> (Right-Side) CordbType
// This is very useful when we get a typehandle from the LeftSide. A common
// scenario is when we get an Object back from the LS, which happens when
// we build the CordbType corresponding to a Cordb*Value.
//
// Parameters:
//   pAppdomain      - the appdomain the type lives in.
//   vmTypeHandle    - a Left-Side typehandle describing the type.
//   elementType     - convenient way to indicate whether we've got ELEMENT_TYPE_FNPTR or
//                     something else. We should be able to retrieve this from the TypeHandle,
//                     but our caller already has it available.
//   typeConstructor - CordbClass corresponding to the typeHandle. This could be built
//                     up from typehandle, but our caller already has it.
//                     Will be NULL for ELEMENT_TYPE_FNPTR
//   pResultType     - OUT: out parameter to yield CordbType for the TypeHandle.
//
// Returns:
//    S_OK on success.
//-----------------------------------------------------------------------------
HRESULT CordbType::InstantiateFromTypeHandle(CordbAppDomain * pAppDomain,
                                             VMPTR_TypeHandle vmTypeHandle,
                                             CorElementType   elementType,
                                             CordbClass *     typeConstructor,
                                             CordbType **     pResultType)
{
    HRESULT hr = S_OK;

    // Should already by synced by caller.
    INTERNAL_SYNC_API_ENTRY(pAppDomain->GetProcess()); //
    _ASSERTE((pAppDomain->GetProcess()->GetShim() == NULL) || (pAppDomain->GetProcess()->GetSynchronized()));

    EX_TRY
    {
        CordbProcess * pProcess = pAppDomain->GetProcess();
        //
        // Step 1) Ask DacDbi interface for a list of type-parameters given a TypeHandle.
        //

        TypeParamsList params;
        {
            RSLockHolder lockHolder(pProcess->GetProcessLock());
            pProcess->GetDAC()->GetTypeHandleParams(pAppDomain->GetADToken(), vmTypeHandle, &params);
        }

        // convert the parameter type information to a list of CordbTypeInstances (one for each parameter)
        // note: typeList will be destroyed on exit, running destructors for each element. In this case, that
        // means it will simply assert IsNeutered.
        DacDbiArrayList<CordbType *> typeList;
        typeList.Alloc(params.Count());
        for (unsigned int i = 0; i < params.Count(); ++i)
        {
            IfFailThrow(TypeDataToType(pAppDomain, &(params[i]), &(typeList[i])));
        }

        // now make an instance of CordbType from an instantiation
        Instantiation instantiation(params.Count(), &(typeList[0]));
        if (elementType == ELEMENT_TYPE_FNPTR)
        {
            IfFailThrow(CordbType::MkType(pAppDomain, elementType, &instantiation, pResultType));
        }
        else
        {
            IfFailThrow(CordbType::MkType(pAppDomain, elementType, typeConstructor, &instantiation, pResultType));
        }
    }
    EX_CATCH_HRESULT(hr);
    return hr;
} // CordbType::InstantiateFromTypeHandle

//-----------------------------------------------------------------------------
// Initialize the CordbType.
// This will involve a lot of queries to the Left-side.
// This means finding the type-handle, getting / creating associated CordbClass,
// filling out the instantiation, getting field info, etc.
//
// Parameters:
//   fForceInit - if false, may skip initialization if TypeHandle already known.
//
// Returns:
//   S_OK if success, CORDBG_E_CLASS_NOT_LOADED, E_INVALIDARG, OOM on failure
//-----------------------------------------------------------------------------
HRESULT CordbType::Init(BOOL fForceInit)
{
    INTERNAL_SYNC_API_ENTRY(GetProcess()); //

    HRESULT hr = S_OK;

    if (m_pClass && m_pClass->GetLoadLevel() != CordbClass::FullInfo)
        fForceInit = TRUE;

    // Step 1. initialize the type constructor (if one exists)
    // and the (class) type parameters....
    if (m_elementType == ELEMENT_TYPE_CLASS)
    {

        // start by initing only enough so that we can determine whether
        // or not this is a generic class. When dealing with generic
        // type instantiations there is no guarantee the open generic
        // type is fully restored. If we load too eagerly it might fail
        // and we wouldn't actually need that extra data anyways.
        _ASSERTE(m_pClass != NULL);
        EX_TRY
        {
            m_pClass->Init(CordbClass::BasicInfo);
        }
        EX_CATCH_HRESULT(hr);
        IfFailRet(hr);

        // non-generic classes need the class object to be fully inited
        // in the generic case we won't ever use that data
        if (!m_pClass->HasTypeParams())
        {
            EX_TRY
            {
                m_pClass->Init(CordbClass::FullInfo);
            }
            EX_CATCH_HRESULT(hr);
            IfFailRet(hr);

            return S_OK; // Non-generic, that's all - no clean-up required
        }
    }

    _ASSERTE(m_elementType != ELEMENT_TYPE_CLASS || m_pClass->HasTypeParams());

    for (unsigned int i = 0; i<m_inst.m_cClassTyPars; i++)
    {
        _ASSERTE(m_inst.m_ppInst != NULL);
        _ASSERTE(m_inst.m_ppInst[i] != NULL);
        IfFailRet( m_inst.m_ppInst[i]->Init(fForceInit) );
    }

    // Step 2. Try to fetch the type handle if necessary (only
    // for instantiated class types, pointer types etc.)
    // We do this by preparing an event specifying the type and
    // then fetching the type handle from the left-side.  This
    // will not always succeed, as forcing the load of the type handle would be the
    // equivalent of doing a FuncEval, i.e. the instantiation may
    // not have been created.  But we try anyway to reduce the number of
    // failures.
    //
    // Note that in the normal case we will have the type handle from the EE
    // anyway, e.g. if the CordbType was created when reporting the type
    // of an actual object.

     // Initialize m_typeHandleExact if it needs it
     if (m_elementType == ELEMENT_TYPE_ARRAY ||
         m_elementType == ELEMENT_TYPE_SZARRAY ||
         m_elementType == ELEMENT_TYPE_BYREF ||
         m_elementType == ELEMENT_TYPE_PTR ||
         m_elementType == ELEMENT_TYPE_FNPTR ||
         (m_elementType == ELEMENT_TYPE_CLASS && m_pClass->HasTypeParams()))
      {
         // It is OK if getting an exact type handle
         // fails with CORDBG_E_CLASS_NOT_LOADED.  In that case we leave
         // the type information incomplete and subsequent operations
         // will try to call Init() again.  The immediate operation will fail later if
         // TypeToBasicTypeData requests the exact type information for this type.
         hr = InitInstantiationTypeHandle(fForceInit);
         if (hr != CORDBG_E_CLASS_NOT_LOADED)
             IfFailRet(hr);
      }


     // For OBJECT and STRING we may not have a value for m_class
     // object.  Go try and get it.
     if (m_elementType == ELEMENT_TYPE_STRING ||
         m_elementType == ELEMENT_TYPE_OBJECT)
     {
         IfFailRet(InitStringOrObjectClass(fForceInit));
     }

    // Step 3. Fetch the information that is specific to the type where necessary...
    // Now we have the type handle for the constructed type, we can ask for the size of
    // the object.  Only do this for constructed value types.
    //
    // Note that the exact and/or approximate type handles may not be available.
    if ((m_elementType == ELEMENT_TYPE_CLASS) && m_pClass->HasTypeParams())
    {
        IfFailRet(InitInstantiationFieldInfo(fForceInit));
    }

    return S_OK;
}

//-----------------------------------------------------------------------------
// Internal function to communicate with Left-Side to get an exact TypeHandle
// (runtime type representation) for this CordbType.
//
// Parameters:
//   fForceInit - if false, may skip initialization if TypeHandle already known.
//
// Returns:
//   S_OK on success or failure HR E_INVALIDARG, OOM, CORDBG_E_CLASS_NOT_LOADED
//   on failure
//-----------------------------------------------------------------------------
HRESULT CordbType::InitInstantiationTypeHandle(BOOL fForceInit)
{

    // Check if we've already done this Init
    if (!fForceInit && !m_typeHandleExact.IsNull())
        return S_OK;

    HRESULT hr = S_OK;

    // Create an array of DebuggerIPCE_BasicTypeData structures from the array of type parameters.
    // First, get a buffer to hold the information
    CordbProcess *pProcess = GetProcess();
    S_UINT32 bufferSize = S_UINT32(sizeof(DebuggerIPCE_BasicTypeData)) *
                                   S_UINT32(m_inst.m_cClassTyPars);
    EX_TRY
    {
        if( bufferSize.IsOverflow() )
        {
            ThrowHR(E_INVALIDARG);
        }
        NewArrayHolder<DebuggerIPCE_BasicTypeData> pArgTypeData(new DebuggerIPCE_BasicTypeData[bufferSize.Value()]);

        // We will have already called Init on each of the type parameters further above. Now we build a
        // list of type information for each type parameter.
        for (unsigned int i = 0; i < m_inst.m_cClassTyPars; i++)
        {
            _ASSERTE(m_inst.m_ppInst != NULL);
            _ASSERTE(m_inst.m_ppInst[i] != NULL);
            IfFailThrow(m_inst.m_ppInst[i]->TypeToBasicTypeData(&pArgTypeData[i]));
        }

        DebuggerIPCE_ExpandedTypeData typeData;

        // get the top-level type information
        TypeToExpandedTypeData(&typeData);

        ArgInfoList argInfo(pArgTypeData, m_inst.m_cClassTyPars);

        {
            // Get the TypeHandle based on the type data
            RSLockHolder lockHolder(GetProcess()->GetProcessLock());
            hr = pProcess->GetDAC()->GetExactTypeHandle(&typeData, &argInfo, m_typeHandleExact);
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
} // CordbType::InitInstantiationTypeHandle

//-----------------------------------------------------------------------------
// Internal helper for CordbType::Init to finish initialize types for
// System.String or System.Object.
//   This just needs to set the m_class field.
//
// Parameters:
//    fForceInit - force re-initialization if already initialized.
//
// Returns:
//    S_OK on success or CORDBG_E_CLASS_NOT_LOADED on failure.
//
// Note: verification with IPC result may assert
//-----------------------------------------------------------------------------

HRESULT CordbType::InitStringOrObjectClass(BOOL fForceInit)
{
    // This CordbType is a non-generic class, either System.String or System.Object.
    // Need to find the CordbClass instance (in the proper AppDomain) that matches that type.

    // Check if we've already done this Init
    if (!fForceInit && m_pClass != NULL)
    {
        return S_OK;
    }

    HRESULT hr = S_OK;

    EX_TRY
    {
        //
        // Step 1a) Send a request to the DAC to map: CorElementType --> {token, Module}
        //
        CordbProcess *pProcess = GetProcess();
        mdTypeDef metadataToken;
        VMPTR_DomainAssembly vmDomainAssembly = VMPTR_DomainAssembly::NullPtr();
        VMPTR_Module vmModule = VMPTR_Module::NullPtr();

        {
            RSLockHolder lockHolder(GetProcess()->GetProcessLock());
            pProcess->GetDAC()->GetSimpleType(m_appdomain->GetADToken(),
                                              m_elementType,
                                              &metadataToken,
                                              &vmModule,
                                              &vmDomainAssembly);
        }

        //
        // Step 2) Lookup CordbClass based off token + Module.
        //
        CordbModule * pTypeModule = m_appdomain->LookupOrCreateModule(vmModule, vmDomainAssembly);

        _ASSERTE(pTypeModule != NULL);
        IfFailThrow(pTypeModule->LookupOrCreateClass(metadataToken, &m_pClass));

        _ASSERTE(m_pClass != NULL);

        _ASSERTE(SUCCEEDED(hr));
        m_pClass->AddRef();

    }
    EX_CATCH_HRESULT(hr);
    return hr;
} // CordbType::InitStringOrObjectClass

//-----------------------------------------------------------------------------
// Internal helper for CordbType::Init to get FieldInfos for a generic Type.
// Non-generic types can use the FieldInfos off their associated CordbClass.
//
// Parameters:
//    fForceInit - force re-initialization if already initialized?
//
// Returns:
//    S_OK on success.
//-----------------------------------------------------------------------------
HRESULT CordbType::InitInstantiationFieldInfo(BOOL fForceInit)
{
    HRESULT hr = S_OK;

    // Check if we've already done this Init
    if (!m_fieldInfoNeedsInit && !fForceInit)
    {
        return hr;
    }

    _ASSERTE(m_elementType == ELEMENT_TYPE_CLASS);
    _ASSERTE(m_pClass->HasTypeParams());

    VMPTR_TypeHandle typeHandleApprox = m_typeHandleExact;

    // If the exact type handle is not available then get the approximate type handle.
    if (typeHandleApprox.IsNull())
    {
        // set up a buffer to hold type parameter information for the type. (See
        // code:CordbType::GatherTypeData for more information). First, compute its size.
        unsigned int typeDataNodeCount = 0;
        this->CountTypeDataNodes(&typeDataNodeCount);

        EX_TRY
        {
            // allocate a buffer to hold the parameter data
            TypeInfoList typeData;

            typeData.Alloc(typeDataNodeCount);

            // fill the buffer
            DebuggerIPCE_TypeArgData * pCurrent = &(typeData[0]);
            GatherTypeData(this, &pCurrent);

            // request the type handle from the DAC
            CordbProcess *pProcess = GetProcess();
            {
                RSLockHolder lockHolder(pProcess->GetProcessLock());
                typeHandleApprox = pProcess->GetDAC()->GetApproxTypeHandle(&typeData);
            }
        }
        EX_CATCH_HRESULT(hr);
        if(FAILED(hr)) return hr;
    }

    // OK, now get the field info if we can.
    CordbProcess *pProcess = GetProcess();
    EX_TRY
    {
        {
            // this may be called multiple times. Each call will discard previous values in m_fieldList and reinitialize
            // the list with updated information
            RSLockHolder lockHolder(pProcess->GetProcessLock());
            pProcess->GetDAC()->GetInstantiationFieldInfo(m_pClass->GetModule()->GetRuntimeDomainAssembly(),
                                                          m_typeHandleExact,
                                                          typeHandleApprox,
                                                          &m_fieldList,
                                                          &m_objectSize);
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

HRESULT CordbType::ReturnedByValue()
{
    HRESULT hr = S_OK;

    if (!IsValueType())
        return S_OK;


    ULONG32 unboxedSize = 0;
    IfFailRet(GetUnboxedObjectSize(&unboxedSize));

    if (unboxedSize > sizeof(SIZE_T))
        return S_FALSE;

    mdToken mdClass = m_pClass->GetToken();

    int fieldCount = 0;
    bool unsupported = false;

    HCORENUM fields = 0;
    ULONG fetched = 0;
    mdToken mdField;
    IMetaDataImport *pImport = m_pClass->GetModule()->GetMetaDataImporter();
    IfFailRet(pImport->EnumFields(&fields, mdClass, &mdField, 1, &fetched));

    while (hr == S_OK && fetched == 1)
    {
        DWORD attr = 0;
        PCCOR_SIGNATURE sigBlob = 0;
        ULONG sigLen = 0;
        hr = pImport->GetFieldProps(mdField, NULL, NULL, 0, NULL, &attr, &sigBlob, &sigLen, NULL, NULL, NULL);

        if (SUCCEEDED(hr))
        {
            // !static
            if ((attr & 0x10) == 0)
            {
                if (fieldCount++)
                    break;

                CorElementType et;
                SigParser parser(sigBlob, sigLen);
                parser.GetByte(NULL);           // 0x6, field signature
                parser.SkipCustomModifiers();
                hr = parser.GetElemType(&et);
                if (SUCCEEDED(hr))
                {
                    switch (et)
                    {
                    case ELEMENT_TYPE_R4:
                    case ELEMENT_TYPE_R8:
                        unsupported = true;
                        break;

                    case ELEMENT_TYPE_CLASS:
                    case ELEMENT_TYPE_STRING:
                    case ELEMENT_TYPE_PTR:
                        // OK
                        break;

                    default:
                        if (!CorIsPrimitiveType(et))
                            unsupported = true;
                        break;
                    }

                    if (unsupported)
                        break;
                }
            }

            hr = pImport->EnumFields(&fields, mdClass, &mdField, 1, &fetched);
        }

        if (FAILED(hr))
        {
            pImport->CloseEnum(fields);
            return hr;
        }
    }

    pImport->CloseEnum(fields);

    if (unsupported)
        return S_FALSE;

    return fieldCount <= 1 ? S_OK : S_FALSE;
}


//-----------------------------------------------------------------------------
// Internal helper to get the size (in bytes) of the unboxed object.
// For a generic type, the size of the type depends on the size of the
// type-parameters.
// This is commonly used by Cordb*Value in their Initialization when they
// need to cache the size of the Target object they refer to.
//
// This should only be called on Value-types and Primitives (eg, i4, FnPtr).
// It should not be called on Reference types.
//
// Parameters:
//   pObjectSize - OUT: out-parameter to get the size in bytes.
//
// Returns:
//    S_OK on success.
//-----------------------------------------------------------------------------
HRESULT
CordbType::GetUnboxedObjectSize(ULONG32 *pObjectSize)
{
    INTERNAL_SYNC_API_ENTRY(GetProcess()); //

    HRESULT hr = S_OK;
    bool isVC = false;

    EX_TRY
    {
        isVC = IsValueType();
    }
    EX_CATCH_HRESULT(hr);

    IfFailRet(hr);

    if (isVC)
    {
        *pObjectSize = 0;

        hr = Init(FALSE);

        if (!SUCCEEDED(hr))
            return hr;

        *pObjectSize = (ULONG) ((!m_pClass->HasTypeParams()) ? m_pClass->ObjectSize() : this->m_objectSize);

        return hr;
    }
    else
    {
        // Caller guarantees that we're not a class. And the check above guarantees we're not a value-type.
        // So we're some sort of primitive, and thus we can determine size from the signature.
        //
        // @dbgtodo inspection - We didn't have this assert in Whidbey, and it's firing in vararg
        // scenarios even though it's returning the right value for reference types (i.e. 4 on x86 and 8 on
        // 64-bit).  Commenting it out for now.
        //_ASSERTE(m_elementType != ELEMENT_TYPE_CLASS);

        // We need to use a temporary variable here -- attempting to cast among pointer types
        // (i.e., (PCCOR_SIGNATURE) &m_elementType) yields incorrect results on big-endian machines
        COR_SIGNATURE corSig = (COR_SIGNATURE) m_elementType;

        SigParser sigParser(&corSig, sizeof(corSig));

        uint32_t size;

        IfFailRet(sigParser.PeekElemTypeSize(&size));

        *pObjectSize = size;
        return hr;
    }
}

VMPTR_DomainAssembly CordbType::GetDomainAssembly()
{
    if (m_pClass != NULL)
    {
        CordbModule * pModule = m_pClass->GetModule();
        if (pModule)
        {
            return pModule->m_vmDomainAssembly;
        }
        else
        {
            return VMPTR_DomainAssembly::NullPtr();
        }
    }
    else
    {
        return VMPTR_DomainAssembly::NullPtr();
    }
}


VMPTR_Module CordbType::GetModule()
{
    if (m_pClass != NULL)
    {
        CordbModule * pModule = m_pClass->GetModule();
        if (pModule)
        {
            return pModule->GetRuntimeModule();
        }
        else
        {
            return VMPTR_Module::NullPtr();
        }
    }
    else
    {
        return VMPTR_Module::NullPtr();
    }
}
//-----------------------------------------------------------------------------
// Internal method to Marshal:  CordbType --> DebuggerIPCE_BasicTypeData
// Nb. CordbType::Init will call this.  The operation
// fails if the exact type information has been requested but was not available
//
// Parameters:
//   data - OUT: BasicTypeData instance to fill out.
//
// Returns:
//   S_OK on success, CORDBG_E_CLASS_NOT_LOADED on failure
//-----------------------------------------------------------------------------
HRESULT CordbType::TypeToBasicTypeData(DebuggerIPCE_BasicTypeData *data)
{
    switch (m_elementType)
    {
    case ELEMENT_TYPE_ARRAY:
    case ELEMENT_TYPE_SZARRAY:
    case ELEMENT_TYPE_BYREF:
    case ELEMENT_TYPE_PTR:
        data->elementType = m_elementType;
        data->metadataToken = mdTokenNil;
        data->vmDomainAssembly = VMPTR_DomainAssembly::NullPtr();
        data->vmTypeHandle = m_typeHandleExact;
        if (data->vmTypeHandle.IsNull())
        {
            return CORDBG_E_CLASS_NOT_LOADED;
        }
        _ASSERTE(!data->vmTypeHandle.IsNull());
        break;

    case ELEMENT_TYPE_CLASS:
        _ASSERTE(m_pClass != NULL);
        data->elementType = m_pClass->IsValueClassNoInit() ? ELEMENT_TYPE_VALUETYPE : ELEMENT_TYPE_CLASS;
        data->metadataToken = m_pClass->MDToken();
	    data->vmDomainAssembly = GetDomainAssembly();
        data->vmTypeHandle = m_typeHandleExact;
        if (m_pClass->HasTypeParams() && data->vmTypeHandle.IsNull())
        {
            return CORDBG_E_CLASS_NOT_LOADED;
        }
        break;
    default:
        // This includes all the "primitive" types, in which CorElementType is a sufficient description.
        data->elementType = m_elementType;
        data->metadataToken = mdTokenNil;
        data->vmDomainAssembly = VMPTR_DomainAssembly::NullPtr();
        data->vmTypeHandle = VMPTR_TypeHandle::NullPtr();
        break;
    }
    return S_OK;
}

//-----------------------------------------------------------------------------
// Internal method to marshal: CordbType --> ExpandedTypeData
//
// Nb. CordbType::Init need NOT have been called before this...
// Also, this does not write the type arguments.  How this is done depends
// depends on where this is called from.
//
// Parameters:
//     data - OUT: outgoing ExpandedTypeData to fill in with stats about CordbType.
//-----------------------------------------------------------------------------
void CordbType::TypeToExpandedTypeData(DebuggerIPCE_ExpandedTypeData *data)
{

    switch (m_elementType)
    {
    case ELEMENT_TYPE_ARRAY:
    case ELEMENT_TYPE_SZARRAY:

        data->ArrayTypeData.arrayRank = m_rank;
        data->elementType = m_elementType;
        break;

    case ELEMENT_TYPE_BYREF:
    case ELEMENT_TYPE_PTR:
    case ELEMENT_TYPE_FNPTR:

        data->elementType = m_elementType;
        break;

    case ELEMENT_TYPE_CLASS:
        {
            data->elementType = m_pClass->IsValueClassNoInit() ? ELEMENT_TYPE_VALUETYPE : ELEMENT_TYPE_CLASS;
            data->ClassTypeData.metadataToken = m_pClass->GetToken();
            data->ClassTypeData.vmDomainAssembly = GetDomainAssembly();
            data->ClassTypeData.vmModule = GetModule();
            data->ClassTypeData.typeHandle = VMPTR_TypeHandle::NullPtr();

            break;
        }
    case ELEMENT_TYPE_END:
        _ASSERTE(!"bad element type!");
        break;

    default:
        data->elementType = m_elementType;
        break;
    }
}


void CordbType::TypeToTypeArgData(DebuggerIPCE_TypeArgData *data)
{
  TypeToExpandedTypeData(&(data->data));
  data->numTypeArgs = m_inst.m_cClassTyPars;
}


//-----------------------------------------------------------------------------
// Query if this CordbType represents a ValueType (Does not include primitives).
// Since CordbType doesn't record ValueType status, this may involve querying
// the CordbClass or even asking the Left-Side (if the CordbClass is not init)
//
// Return Value:
//   indicates whether this is a value type
// Note:
//    Throws.
//-----------------------------------------------------------------------------
bool CordbType::IsValueType()
{
  if (m_elementType == ELEMENT_TYPE_CLASS)
  {
      return m_pClass->IsValueClass();
  }
  else
        return false;
}

//------------------------------------------------------------------------
// If this is a ptr type, get the CordbType that it points to.
// Eg, for CordbType("Int*") or CordbType("Int&"), returns CordbType("Int").
// If not a ptr type, returns null.
// Since it's all internal, no reference counting.
// This is effectively a specialized version of DestUnaryType.
//------------------------------------------------------------------------
CordbType * CordbType::GetPointerElementType()
{
    if ((m_elementType != ELEMENT_TYPE_PTR) && (m_elementType != ELEMENT_TYPE_BYREF))
    {
        return NULL;
    }

    CordbType * pOut;
    DestUnaryType(&pOut);

    _ASSERTE(pOut != NULL);
    return pOut;
}
//------------------------------------------------------------------------
// Helper for IsGcRoot.
// Determine if the element type is a non GC-root candidate.
// Updating GC-roots requires coordinating with the GC's write-barrier.
// Whereas non-GC roots can be updated more freely.
//
// Parameters:
//   et - An element type.
// Returns:
//   True if variables of et can be used as a GC root.
//------------------------------------------------------------------------
static inline bool IsElementTypeNonGcRoot(CorElementType et)
{
    // Functon ptrs are raw data, not GC-roots.
    if (et == ELEMENT_TYPE_FNPTR)
    {
        return true;
    }

    // This is almost exactly if we're a primitive, but
    // primitives include some things that could be GC-roots, so we strip those out,
    return CorIsPrimitiveType(et)
        && (et != ELEMENT_TYPE_STRING) && (et != ELEMENT_TYPE_VOID); // exlcude these from primitives

}
//------------------------------------------------------------------------
// Helper for IsGcRoot
// Non-gc roots include Value types + non-gc elemement types (like E_T_I4, E_T_FNPTR)
//
// Parameters:
//   pType - type to check whether it's a GC-root.
// Returns:
//   true if we know we're not a GC-root
//   false if we still might be (so caller must do further checkin)
//------------------------------------------------------------------------
static inline bool _IsNonGCRootHelper(CordbType * pType)
{
    _ASSERTE(pType != NULL);

    CorElementType et = pType->GetElementType();
    if (IsElementTypeNonGcRoot(et))
    {
        return true;
    }

    HRESULT hr = S_OK;
    bool fValueClass = false;

    // If we are a value-type, then we can't be a Gc-root.
    EX_TRY
    {
        fValueClass = pType->IsValueType();
    }
    EX_CATCH_HRESULT(hr);
    if (FAILED(hr) || fValueClass)
    {
        return true;
    }

    // Don't know
    return false;
}

//-----------------------------------------------------------------------------
// Is this type a GC-root. (Not to be confused w/ "does this contain embedded GC roots")
// All object references are GC-roots. E_T_PTR are actually not GC-roots.
//
// Returns:
//    True - if this is a GC-root.
//    False - not a GC root.
//-----------------------------------------------------------------------------
bool CordbType::IsGCRoot()
{
    // If it's a E_T_PTR type, then look at what it's a a pointer of.
    CordbType * pPtr = this->GetPointerElementType();
    if (pPtr == NULL)
    {
        // If non pointer, than we can just look at our current type.
        return !_IsNonGCRootHelper(this);
    }

    return !_IsNonGCRootHelper(pPtr);
}


//------------------------------------------------------------------------
// Public function to enumerate type-parameters.
// Parameters:
//    ppTypeParameterEnum - OUT: on return, get an enumerator.
// Returns:
//    S_OK on success.
//------------------------------------------------------------------------
HRESULT CordbType::EnumerateTypeParameters(ICorDebugTypeEnum **ppTypeParameterEnum)
{
    PUBLIC_API_ENTRY(this);
    VALIDATE_POINTER_TO_OBJECT(ppTypeParameterEnum, ICorDebugTypeEnum **);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());


    CordbTypeEnum *icdTPE = CordbTypeEnum::Build(m_appdomain, m_appdomain->GetLongExitNeuterList(), this->m_inst.m_cInst, this->m_inst.m_ppInst);
    if ( icdTPE == NULL )
    {
        (*ppTypeParameterEnum) = NULL;
        return E_OUTOFMEMORY;
    }

    (*ppTypeParameterEnum) = static_cast<ICorDebugTypeEnum*> (icdTPE);
    icdTPE->ExternalAddRef();
    return S_OK;
}


//-----------------------------------------------------------------------------
// CordbType::GetBase
// Public convenience method to get the instantiated base type.
//
// Parameters:
//   ppType - OUT: yields the base type for the current type.
//
// Returns:
//   S_OK if succeeded.
//
HRESULT CordbType::GetBase(ICorDebugType ** ppType)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    ATT_ALLOW_LIVE_DO_STOPGO(this->GetProcess()); // @todo - can this by RequiredStopped?

    HRESULT hr = S_OK;

    LOG((LF_CORDB, LL_EVERYTHING, "CordbType::GetBase called\n"));

    VALIDATE_POINTER_TO_OBJECT(ppType, ICorDebugType **);

    if (m_elementType != ELEMENT_TYPE_CLASS)
    {
        return E_INVALIDARG;
    }

    EX_TRY
    {
        CordbType * pType = NULL;

        _ASSERTE(m_pClass != NULL);

        // Get the supertype from metadata for m_class
        mdToken extendsToken;

        IMetaDataImport * pImport = m_pClass->GetModule()->GetMetaDataImporter(); // throws

        hr = pImport->GetTypeDefProps(m_pClass->MDToken(), NULL, 0, NULL, NULL, &extendsToken);
        IfFailThrow(hr);

        // Now create a CordbType instance for the base type that has the same type parameters as the derived type.
        if ((extendsToken == mdTypeDefNil) || (extendsToken == mdTypeRefNil) || (extendsToken == mdTokenNil))
        {
            // No base class.
            pType = NULL;
        }
        else if (TypeFromToken(extendsToken) == mdtTypeSpec)
        {
            // TypeSpec has a signature. So get the sig and convert it to a CordbType.
            // generic base class of a generic type is a TypeSpec.
            // If we have:
            //    class Triple<T,U,V> derives from Pair<T,V>,
            // then the base class for Triple would be a TypeSpec:
            //   Class(Pair<T,V>), 2 args, ELEMENT_TYPE_VAR #0, ELEMENT_TYPE_VAR#2.
            // m_inst provides the type-parameters to resolve the ELEMENT_TYPE_VAR types.

            PCCOR_SIGNATURE pSig;
            ULONG sigSize;

            // Get the signature for the constructed supertype...
            hr = pImport->GetTypeSpecFromToken(extendsToken, &pSig, &sigSize);
            IfFailThrow(hr);

            _ASSERTE(pSig != NULL);

            SigParser sigParser(pSig, sigSize);

            // Instantiate the signature of the supertype using the type instantiation for
            // the current type....
            hr = SigToType(m_pClass->GetModule(), &sigParser, &m_inst, &pType);
            IfFailThrow(hr);
        }
        else if ((TypeFromToken(extendsToken) == mdtTypeRef) || (TypeFromToken(extendsToken) == mdtTypeDef))
        {
            // TypeDef/TypeRef for non-generic base-class class.
            CordbClass * pSuperClass;

            hr = m_pClass->GetModule()->ResolveTypeRefOrDef(extendsToken, &pSuperClass);
            IfFailThrow(hr);

            _ASSERTE(pSuperClass != NULL);

            hr = MkUnparameterizedType(m_appdomain, ELEMENT_TYPE_CLASS, pSuperClass, &pType);
            IfFailThrow(hr);
        }
        else
        {
            pType = NULL;
            _ASSERTE(!"unexpected token!");
        }

        // At this point, we've succeeded
        _ASSERTE(SUCCEEDED(hr));

        (*ppType) = pType;

        if (*ppType)
        {
            pType->AddRef();
        }
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

//-----------------------------------------------------------------------------
// CordbType::GetTypeID
// Method to get the COR_TYPEID corresponding to this CordbType.
//
// Parameters:
//   pId - OUT: gives the COR_TYPEID for this CordbType
//
// Returns:
//   S_OK if succeeded.
//   CORDBG_E_CLASS_NOT_LOADED if the type which this CordbType represents has
//       not been loaded in the runtime.
//  E_POINTER if pId is NULL
//  CORDBG_E_UNSUPPORTED for unsupported types.
//
HRESULT CordbType::GetTypeID(COR_TYPEID *pId)
{
    LOG((LF_CORDB, LL_INFO1000, "GetTypeID\n"));
    if (pId == NULL)
        return E_POINTER;

    HRESULT hr = S_OK;

    PUBLIC_API_ENTRY(this);
    RSLockHolder stopGoLock(GetProcess()->GetStopGoLock());
    RSLockHolder procLock(GetProcess()->GetProcessLock());

    EX_TRY
    {
        hr = Init(FALSE);
        IfFailThrow(hr);

        VMPTR_TypeHandle vmTypeHandle = VMPTR_TypeHandle::NullPtr();

        CorElementType et = GetElementType();
        switch (et)
        {
        case ELEMENT_TYPE_OBJECT:
        case ELEMENT_TYPE_VOID:
        case ELEMENT_TYPE_BOOLEAN:
        case ELEMENT_TYPE_CHAR:
        case ELEMENT_TYPE_I1:
        case ELEMENT_TYPE_U1:
        case ELEMENT_TYPE_I2:
        case ELEMENT_TYPE_U2:
        case ELEMENT_TYPE_I4:
        case ELEMENT_TYPE_U4:
        case ELEMENT_TYPE_I8:
        case ELEMENT_TYPE_U8:
        case ELEMENT_TYPE_R4:
        case ELEMENT_TYPE_R8:
        case ELEMENT_TYPE_STRING:
        case ELEMENT_TYPE_TYPEDBYREF:
        case ELEMENT_TYPE_I:
        case ELEMENT_TYPE_U:
            {
                mdTypeDef mdToken;
                VMPTR_Module vmModule = VMPTR_Module::NullPtr();
                VMPTR_DomainAssembly vmDomainAssembly = VMPTR_DomainAssembly::NullPtr();

                // get module and token of the simple type
                GetProcess()->GetDAC()->GetSimpleType(GetAppDomain()->GetADToken(),
                                                      et,
                                                      &mdToken,
                                                      &vmModule,
                                                      &vmDomainAssembly);

                vmTypeHandle = GetProcess()->GetDAC()->GetTypeHandle(vmModule, mdToken);
            }
            break;
        case ELEMENT_TYPE_ARRAY:
        case ELEMENT_TYPE_SZARRAY:
            {
                LOG((LF_CORDB, LL_INFO1000, "GetTypeID: parameterized type\n"));
                if (m_typeHandleExact.IsNull())
                {
                    hr = InitInstantiationTypeHandle(FALSE);
                    IfFailThrow(hr);
                }
                vmTypeHandle = m_typeHandleExact;
            }
            break;
        case ELEMENT_TYPE_CLASS:
            {
                ICorDebugClass *pICDClass = NULL;
                hr = GetClass(&pICDClass);
                IfFailThrow(hr);
                CordbClass *pClass = (CordbClass*)pICDClass;
                _ASSERTE(pClass != NULL);

                if (pClass->HasTypeParams())
                {
                    vmTypeHandle = m_typeHandleExact;
                }
                else
                {
                    mdTypeDef mdToken;
                    hr = pClass->GetToken(&mdToken);
                    IfFailThrow(hr);

                    VMPTR_Module vmModule = GetModule();
                    vmTypeHandle = GetProcess()->GetDAC()->GetTypeHandle(vmModule, mdToken);
                }
            }
            break;
        case ELEMENT_TYPE_PTR:
        case ELEMENT_TYPE_BYREF:
        case ELEMENT_TYPE_FNPTR:
            IfFailThrow(CORDBG_E_UNSUPPORTED);
            break;
        default:
            _ASSERTE(!"unexpected element type!");
            IfFailThrow(CORDBG_E_UNSUPPORTED);
            break;
        }

        GetProcess()->GetDAC()->GetTypeIDForType(vmTypeHandle, pId);
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

//-----------------------------------------------------------------------------
// Get rich field information given a token.
//
// Parameters:
//    fldToken - metadata field token specifying a field on this Type.
//    ppFieldData - OUT: get the rich field information for the given field
//
// Returns:
//   S_OK on success. CORDBG_E_ENC_HANGING_FIELD for EnC fields (common case)
//   Other errors on failure case.
//-----------------------------------------------------------------------------
HRESULT CordbType::GetFieldInfo(mdFieldDef fldToken, FieldData ** ppFieldData)
{
    INTERNAL_SYNC_API_ENTRY(GetProcess()); //
    HRESULT hr = S_OK;

    *ppFieldData = NULL;

    EX_TRY
    {
        if (m_elementType != ELEMENT_TYPE_CLASS)
        {
            ThrowHR(E_INVALIDARG);
        }

        // Initialize so that the field information is up-to-date.
        hr = Init(FALSE);
        IfFailThrow(hr);

        if (m_pClass->HasTypeParams())
        {
            if (m_fieldList.IsEmpty())
            {
                ThrowHR(CORDBG_E_FIELD_NOT_AVAILABLE);
            }
            else
            {
                // Use a static helper function in CordbClass, though we're really
                // searching through this->m_fields
                hr = CordbClass::SearchFieldInfo(m_pClass->GetModule(),
                                                 &m_fieldList,
                                                 m_pClass->MDToken(),
                                                 fldToken,
                                                 ppFieldData);
                // fall through and return.
                // Let possible CORDBG_E_ENC_HANGING_FIELD errors propagate
            }
        }
        else
        {
            hr = m_pClass->GetFieldInfo(fldToken, ppFieldData); // this is for non-generic types....
            // Let possible CORDBG_E_ENC_HANGING_FIELD errors propagate
        }
    }
    EX_CATCH_HRESULT(hr);
    _ASSERTE(SUCCEEDED(hr) == (*ppFieldData != NULL));
    return hr;
}


//-----------------------------------------------------------------------------
// Class is a class somewhere on the hierarchy for m_type.  Search for
// a CordbType corresponding to the CordbClass, but which has the type-parameters
// from the current CordbType.
// In other words, instantiate a CordbType from baseClass, using the type-params
// in the current Type.
//
// For example, given:
//     class C<T>
//     class D : C<int>
// then if the CordbObjectValue is of type D and pClass is the class
// for "C", then searching will set relevantType to C<int>.  This
// type is then used to fetch fields from the object.
//
// Adds a reference to the resulting type.  Since this is for internal
// use only we probably don't need todo this...
//
// Parameters:
//   baseClass - open Type that needs to be instantiated with this CordbType's params.
//   ppRes - OUT: out-parameter to get CordbType. ppRes->GetClass() should equal baseClass.
//
// Returns:
//    S_OK on success. CORDBG_E_OBJECT_NEUTERED, CORDBG_E_CLASS_NOT_LOADED, E_INVALIDARG, OOM
//-----------------------------------------------------------------------------
HRESULT CordbType::GetParentType(CordbClass *baseClass, CordbType **ppRes)
{
    INTERNAL_SYNC_API_ENTRY(GetProcess()); //

    // Ensure that we're not trying to match up against a neutered class.
    if (baseClass->IsNeutered())
    {
        return CORDBG_E_OBJECT_NEUTERED;
    }

    HRESULT hr = S_OK;
    _ASSERTE(ppRes);
    *ppRes = NULL;
    CordbType *res = this;
    res->AddRef();
    int safety = 20000; // no inheritance hierarchy is 20000 deep... we include this just in case there's a issue below and we don't terminate
    while (safety--)
    {
        if (res->m_pClass == NULL)
        {
            if (FAILED(hr = res->Init(FALSE)))
            {
                res->Release();
                return hr;
            }
        }
        _ASSERTE(res->m_pClass);
        if (res->m_pClass == baseClass)
        {
            // Found it!
            break;
        }

        // Another way to determine if we're talking about the
        // same class...  Compare tokens and module.
        mdTypeDef tok;
        mdTypeDef targetTok;
        if (FAILED(hr = res->m_pClass->GetToken(&tok))
            || FAILED(hr = baseClass->GetToken(&targetTok)))
        {
            res->Release();
            return hr;
        }
        if (tok == targetTok && res->m_pClass->GetModule() == baseClass->GetModule())
        {
            // Found it!
            break;
        }

        // OK, this is not the right class so look up the inheritance chain
        ICorDebugType *nextType = NULL;
        if (FAILED(hr = res->GetBase(&nextType)))
        {
            res->Release();
            return hr;
        }

        res->Release(); // matches the AddRef above and/or the one implicit in GetBase, for all but last time around the loop
        res = static_cast<CordbType *> (nextType);
        if (!res || res->m_elementType == ELEMENT_TYPE_OBJECT)
        {
            // Did not find it...
            break;
        }
    }
    // We exit the loop above owning one reference to res.
    // Upon exit res will either be the appropriate type for the
    // class we're looking for or will be the CordbType for System.Object
    // or will be NULL

    // If it's System.Object then assume something's gone wrong with
    // the way we did the search and bail out to an old fashioned
    // MkUnparameterizedType on the class given originally
    if (!res || res->m_elementType == ELEMENT_TYPE_OBJECT)
    {
        if (res)
            res->Release();  // matches the one left over from the loop
        IfFailRet(CordbType::MkUnparameterizedType(baseClass->GetAppDomain(), ELEMENT_TYPE_CLASS, baseClass, &res));
        res->AddRef();
    }


    *ppRes = res;
    return hr;
}


//-----------------------------------------------------------------------------
// Walk a type tree, writing the number of type args including internal nodes.
//
// Parameters:
//    count - IN/OUT: counter to update.
//-----------------------------------------------------------------------------
void CordbType::CountTypeDataNodes(unsigned int *count)
{
  (*count)++;
  for (unsigned int i = 0; i < this->m_inst.m_cClassTyPars; i++)
  {
      this->m_inst.m_ppInst[i]->CountTypeDataNodes(count);
  }
}

//-----------------------------------------------------------------------------
// Internal helper method.
// Counts the total generic args (including sub-args) for an Instantiation.
// Eg, for List<int, Pair<string, float>>, it would return 3.
//
// Parameters:
//    genericArgsCount - size of the genericArgs array in elements.
//    genericArgs - array of type parameters.
//    count - IN/OUT - will increment with total number of generic args.
//        caller must intialize this (likely to 0).
//-----------------------------------------------------------------------------
void CordbType::CountTypeDataNodesForInstantiation(unsigned int genericArgsCount, ICorDebugType *genericArgs[], unsigned int *count)
{
    for (unsigned int i = 0; i < genericArgsCount; i++)
    {
        (static_cast<CordbType *>(genericArgs[i]))->CountTypeDataNodes(count);
    }
}

//-----------------------------------------------------------------------------
// Recursively walk a type tree, writing the type args into a linear.
// Eg, for List<A, Pair<B, C>>, this will write the TypeArgData buffer
// for { A, B, C }.
//
// Parameters:
//   curr_tyargData - IN/OUT: Pointer into buffer of TypeArgData structures.
//      Caller must ensure this buffer is large enough (probably by calling
//      CountTypeDataNodes).
//      On output, set to the next element in the buffer.
//-----------------------------------------------------------------------------
void CordbType::GatherTypeData(CordbType *type, DebuggerIPCE_TypeArgData **curr_tyargData)
{
  type->TypeToTypeArgData(*curr_tyargData);
  (*curr_tyargData)++;
  for (unsigned int i = 0; i < type->m_inst.m_cClassTyPars; i++)
  {
    GatherTypeData(type->m_inst.m_ppInst[i], curr_tyargData);
  }
}

//-----------------------------------------------------------------------------
// Flatten Instantiation into a linear buffer of TypeArgData
// Use CountTypeDataNodesForInstantiation on the instantiation to get a large
// enough buffer.
//
// Parameters:
//    genericArgsCount - size of genericArgs array in elements.
//    genericArgs - incoming array to walk
//    curr_tyargData - IN/OUT: Pointer into buffer of TypeArgData structures.
//      Caller must ensure this buffer is large enough (probably by calling
//      CountTypeDataNodes).
//      On output, set to the next element in the buffer.
//
//-----------------------------------------------------------------------------
void CordbType::GatherTypeDataForInstantiation(unsigned int genericArgsCount, ICorDebugType *genericArgs[], DebuggerIPCE_TypeArgData **curr_tyargData)
{
    for (unsigned int i = 0; i < genericArgsCount; i++)
    {
        GatherTypeData(static_cast<CordbType *> (genericArgs[i]), curr_tyargData);
    }
}

#ifdef FEATURE_64BIT_ALIGNMENT
// checks if the type requires 8-byte alignment. the algorithm used here
// was adapted from AdjustArgPtrForAlignment() in bcltype/VarArgsNative.cpp
HRESULT CordbType::RequiresAlign8(BOOL* isRequired)
{
    if (isRequired == NULL)
        return E_INVALIDARG;

    HRESULT hr = S_OK;

    EX_TRY
    {
        *isRequired = FALSE;

        ULONG32 size = 0;
        GetUnboxedObjectSize(&size);

        if (size >= 8)
        {
            CorElementType type;
            GetType(&type);

            if (type != ELEMENT_TYPE_TYPEDBYREF)
            {
                if (type == ELEMENT_TYPE_VALUETYPE)
                {
                    if (m_typeHandleExact.IsNull())
                        InitInstantiationTypeHandle(FALSE);

                    *isRequired = GetProcess()->GetDAC()->RequiresAlign8(m_typeHandleExact);
                }
                else
                {
                    *isRequired = TRUE;
                }
            }
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}
#endif

/* ------------------------------------------------------------------------- *
 * TypeParameter Enumerator class
 * ------------------------------------------------------------------------- */

// Factory methods
CordbTypeEnum* CordbTypeEnum::Build(CordbAppDomain * pAppDomain, NeuterList * pNeuterList, unsigned int cTypars, CordbType **ppTypars)
{
    return BuildImpl( pAppDomain, pNeuterList, cTypars, ppTypars );
}

CordbTypeEnum* CordbTypeEnum::Build(CordbAppDomain * pAppDomain, NeuterList * pNeuterList, unsigned int cTypars, RSSmartPtr<CordbType> *ppTypars)
{
    return BuildImpl( pAppDomain, pNeuterList, cTypars, ppTypars );
}

//-----------------------------------------------------------------------------
// We need to support taking both an array of CordbType* and an array of RSSmartPtr<CordbType>,
// but the code is identical in both cases.  Rather than duplicate any code explicity, it's better to
// have the compiler do it for us using this template method.
// Another option would be to create an IList<T> interface and implementations for both arrays
// of T* and arrays of RSSmartPtr<T>.  This would be more generally useful, but much more code.
//-----------------------------------------------------------------------------
template<class T> CordbTypeEnum* CordbTypeEnum::BuildImpl(CordbAppDomain * pAppDomain, NeuterList * pNeuterList, unsigned int cTypars, T* ppTypars)
{
    CordbTypeEnum* newEnum = new (nothrow) CordbTypeEnum( pAppDomain, pNeuterList );
    if( NULL == newEnum )
    {
        return NULL;
    }

    _ASSERTE( newEnum->m_ppTypars == NULL );
    newEnum->m_ppTypars = new (nothrow) RSSmartPtr<CordbType> [cTypars];
    if( newEnum->m_ppTypars == NULL )
    {
        delete newEnum;
        return NULL;
    }

    newEnum->m_iMax = cTypars;
    for (unsigned int i = 0; i < cTypars; i++)
    {
        newEnum->m_ppTypars[i].Assign(ppTypars[i]);
    }

    return newEnum;
}

// Private, called only by Build above
CordbTypeEnum::CordbTypeEnum(CordbAppDomain * pAppDomain, NeuterList * pNeuterList) :
    CordbBase(pAppDomain->GetProcess(), 0),
    m_ppTypars(NULL),
    m_iCurrent(0),
    m_iMax(0)
{
    _ASSERTE(pAppDomain != NULL);
    _ASSERTE(pNeuterList != NULL);

    m_pAppDomain =  pAppDomain;

    HRESULT hr = S_OK;
    EX_TRY
    {
        pNeuterList->Add(GetProcess(), this);
    }
    EX_CATCH_HRESULT(hr);
    SetUnrecoverableIfFailed(GetProcess(), hr);
}

CordbTypeEnum::~CordbTypeEnum()
{
    _ASSERTE(this->IsNeutered());
}

void CordbTypeEnum::Neuter()
{
    delete [] m_ppTypars;
    m_ppTypars = NULL;
    m_pAppDomain = NULL;

    CordbBase::Neuter();
}


HRESULT CordbTypeEnum::QueryInterface(REFIID id, void **pInterface)
{
    if (id == IID_ICorDebugEnum)
        *pInterface = static_cast<ICorDebugEnum*>(this);
    else if (id == IID_ICorDebugTypeEnum)
        *pInterface = static_cast<ICorDebugTypeEnum*>(this);
    else if (id == IID_IUnknown)
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugTypeEnum*>(this));
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
}

HRESULT CordbTypeEnum::Skip(ULONG celt)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    HRESULT hr = E_FAIL;
    if ( (m_iCurrent+celt) < m_iMax ||
         celt == 0)
    {
        m_iCurrent += celt;
        hr = S_OK;
    }

    return hr;
}

HRESULT CordbTypeEnum::Reset(void)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    m_iCurrent = 0;
    return S_OK;
}

HRESULT CordbTypeEnum::Clone(ICorDebugEnum **ppEnum)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());


    VALIDATE_POINTER_TO_OBJECT(ppEnum, ICorDebugEnum **);

    HRESULT hr = S_OK;

    CordbTypeEnum *pCVE = CordbTypeEnum::Build( m_pAppDomain, m_pAppDomain->GetLongExitNeuterList(), m_iMax, m_ppTypars );
    if ( pCVE == NULL )
    {
        (*ppEnum) = NULL;
        hr = E_OUTOFMEMORY;
        goto LExit;
    }

    pCVE->AddRef();
    (*ppEnum) = (ICorDebugEnum*)pCVE;

LExit:
    return hr;
}

HRESULT CordbTypeEnum::GetCount(ULONG *pcelt)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    VALIDATE_POINTER_TO_OBJECT(pcelt, ULONG *);

    if( pcelt == NULL)
        return E_INVALIDARG;

    (*pcelt) = m_iMax;
    return S_OK;
}

//
// In the event of failure, the current pointer will be left at
// one element past the troublesome element.  Thus, if one were
// to repeatedly ask for one element to iterate through the
// array, you would iterate exactly m_iMax times, regardless
// of individual failures.
HRESULT CordbTypeEnum::Next(ULONG celt, ICorDebugType *values[], ULONG *pceltFetched)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());


    VALIDATE_POINTER_TO_OBJECT_ARRAY(values, ICorDebugClass *,
        celt, true, true);
    VALIDATE_POINTER_TO_OBJECT_OR_NULL(pceltFetched, ULONG *);

    if ((pceltFetched == NULL) && (celt != 1))
    {
        return E_INVALIDARG;
    }

    if (celt == 0)
    {
        if (pceltFetched != NULL)
        {
            *pceltFetched = 0;
        }
        return S_OK;
    }

    HRESULT hr = S_OK;

    int iMax = min( m_iMax, m_iCurrent+celt);
    int i;

    for (i = m_iCurrent; i < iMax; i++)
    {
         //printf("CordbTypeEnum::Next, returning = 0x%08x.\n", m_ppTypars[i]);
        values[i-m_iCurrent] = m_ppTypars[i];
        values[i-m_iCurrent]->AddRef();
    }

    int count = (i - m_iCurrent);

    if ( FAILED( hr ) )
    {   //we failed: +1 pushes us past troublesome element
        m_iCurrent += 1 + count;
    }
    else
    {
        m_iCurrent += count;
    }

    if (pceltFetched != NULL)
    {
        *pceltFetched = count;
    }

    //
    // If we reached the end of the enumeration, but not the end
    // of the number of requested items, we return S_FALSE.
    //
    if (((ULONG)count) < celt)
    {
        return S_FALSE;
    }

    return hr;
}

