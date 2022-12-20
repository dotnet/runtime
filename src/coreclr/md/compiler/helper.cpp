// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// Helper.cpp
//

//
// Implementation of some internal APIs from code:IMetaDataHelper and code:IMetaDataEmitHelper.
//
//*****************************************************************************
#include "stdafx.h"
#include "regmeta.h"
#include "importhelper.h"
#include "mdlog.h"

#if defined(FEATURE_METADATA_EMIT) || defined(FEATURE_METADATA_INTERNAL_APIS)

//*****************************************************************************
// translating signature from one scope to another scope
//
// Implements public API code:IMetaDataEmit::TranslateSigWithScope.
// Implements internal API code:IMetaDataHelper::TranslateSigWithScope.
//*****************************************************************************
STDMETHODIMP RegMeta::TranslateSigWithScope(    // S_OK or error.
    IMetaDataAssemblyImport *pAssemImport, // [IN] importing assembly interface
    const void  *pbHashValue,           // [IN] Hash Blob for Assembly.
    ULONG       cbHashValue,            // [IN] Count of bytes.
    IMetaDataImport *pImport,           // [IN] importing interface
    PCCOR_SIGNATURE pbSigBlob,          // [IN] signature in the importing scope
    ULONG       cbSigBlob,              // [IN] count of bytes of signature
    IMetaDataAssemblyEmit   *pAssemEmit,// [IN] emit assembly interface
    IMetaDataEmit *pEmit,               // [IN] emit interface
    PCOR_SIGNATURE pvTranslatedSig,     // [OUT] buffer to hold translated signature
    ULONG       cbTranslatedSigMax,
    ULONG       *pcbTranslatedSig)      // [OUT] count of bytes in the translated signature
{
#ifdef FEATURE_METADATA_EMIT
    HRESULT     hr = S_OK;

    IMDCommon   *pAssemImportMDCommon = NULL;
    IMDCommon   *pImportMDCommon = NULL;

    RegMeta     *pRegMetaAssemEmit = static_cast<RegMeta*>(pAssemEmit);
    RegMeta     *pRegMetaEmit = NULL;

    CQuickBytes qkSigEmit;
    ULONG       cbEmit;

    pRegMetaEmit = static_cast<RegMeta*>(pEmit);

    {
        // This function can cause new TypeRef being introduced.
        LOCKWRITE();

        IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

        _ASSERTE(pvTranslatedSig && pcbTranslatedSig);

        if (pAssemImport)
        {
            IfFailGo(pAssemImport->QueryInterface(IID_IMDCommon, (void**)&pAssemImportMDCommon));
        }
        IMetaModelCommon *pAssemImportMetaModelCommon = pAssemImportMDCommon ? pAssemImportMDCommon->GetMetaModelCommon() : 0;

        IfFailGo(pImport->QueryInterface(IID_IMDCommon, (void**)&pImportMDCommon));
        IMetaModelCommon *pImportMetaModelCommon = pImportMDCommon->GetMetaModelCommon();

        IfFailGo( ImportHelper::MergeUpdateTokenInSig(  // S_OK or error.
                pRegMetaAssemEmit ? &(pRegMetaAssemEmit->m_pStgdb->m_MiniMd) : 0, // The assembly emit scope.
                &(pRegMetaEmit->m_pStgdb->m_MiniMd),    // The emit scope.
                pAssemImportMetaModelCommon,            // Assembly where the signature is from.
                pbHashValue,                            // Hash value for the import assembly.
                cbHashValue,                            // Size in bytes.
                pImportMetaModelCommon,                 // The scope where signature is from.
                pbSigBlob,                              // signature from the imported scope
                NULL,                                   // Internal OID mapping structure.
                &qkSigEmit,                             // [OUT] translated signature
                0,                                      // start from first byte of the signature
                0,                                      // don't care how many bytes consumed
                &cbEmit));                              // [OUT] total number of bytes write to pqkSigEmit
        memcpy(pvTranslatedSig, qkSigEmit.Ptr(), cbEmit > cbTranslatedSigMax ? cbTranslatedSigMax :cbEmit );
        *pcbTranslatedSig = cbEmit;
        if (cbEmit > cbTranslatedSigMax)
            hr = CLDB_S_TRUNCATION;
    }

ErrExit:
    if (pAssemImportMDCommon)
        pAssemImportMDCommon->Release();
    if (pImportMDCommon)
        pImportMDCommon->Release();

    return hr;
#else //!FEATURE_METADATA_EMIT
    return E_NOTIMPL;
#endif //!FEATURE_METADATA_EMIT
} // RegMeta::TranslateSigWithScope

#endif //FEATURE_METADATA_EMIT || FEATURE_METADATA_INTERNAL_APIS

#if defined(FEATURE_METADATA_EMIT) && defined(FEATURE_METADATA_INTERNAL_APIS)

//*****************************************************************************
// Helper : Set ResolutionScope of a TypeRef
//
// Implements internal API code:IMetaDataEmitHelper::SetResolutionScopeHelper.
//*****************************************************************************
HRESULT RegMeta::SetResolutionScopeHelper(  // Return hresult.
    mdTypeRef   tr,                     // [IN] TypeRef record to update
    mdToken     rs)                     // [IN] new ResolutionScope
{
    HRESULT      hr = NOERROR;
    TypeRefRec * pTypeRef;

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.GetTypeRefRecord(RidFromToken(tr), &pTypeRef));
    IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_TypeRef, TypeRefRec::COL_ResolutionScope, pTypeRef, rs));

ErrExit:
    return hr;
} // RegMeta::SetResolutionScopeHelper


//*****************************************************************************
// Helper : Set offset of a ManifestResource
//
// Implements internal API code:IMetaDataEmitHelper::SetManifestResourceOffsetHelper.
//*****************************************************************************
HRESULT
RegMeta::SetManifestResourceOffsetHelper(
    mdManifestResource mr,          // [IN] The manifest token
    ULONG              ulOffset)    // [IN] new offset
{
    HRESULT hr = NOERROR;
    ManifestResourceRec * pRec;

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.GetManifestResourceRecord(RidFromToken(mr), &pRec));
    pRec->SetOffset(ulOffset);

ErrExit:
    return hr;
} // RegMeta::SetManifestResourceOffsetHelper

//*******************************************************************************
//
// Following APIs are used by reflection emit.
//
//*******************************************************************************

//*******************************************************************************
// helper to define method semantics
//
// Implements internal API code:IMetaDataEmitHelper::DefineMethodSemanticsHelper.
//*******************************************************************************
HRESULT RegMeta::DefineMethodSemanticsHelper(
    mdToken     tkAssociation,          // [IN] property or event token
    DWORD       dwFlags,                // [IN] semantics
    mdMethodDef md)                     // [IN] method to associated with
{
    HRESULT     hr;
    LOCKWRITE();
    hr = _DefineMethodSemantics((USHORT) dwFlags, md, tkAssociation, false);

ErrExit:
    return hr;
} // RegMeta::DefineMethodSemantics

//*******************************************************************************
// helper to set field layout
//
// Implements internal API code:IMetaDataEmitHelper::SetFieldLayoutHelper.
//*******************************************************************************
HRESULT RegMeta::SetFieldLayoutHelper(  // Return hresult.
    mdFieldDef  fd,                     // [IN] field to associate the layout info
    ULONG       ulOffset)               // [IN] the offset for the field
{
    HRESULT     hr;
    FieldLayoutRec *pFieldLayoutRec;
    RID         iFieldLayoutRec;

    LOCKWRITE();

    if (ulOffset == UINT32_MAX)
    {
        // invalid argument
        IfFailGo( E_INVALIDARG );
    }

    // create a field layout record
    IfFailGo(m_pStgdb->m_MiniMd.AddFieldLayoutRecord(&pFieldLayoutRec, &iFieldLayoutRec));

    // Set the Field entry.
    IfFailGo(m_pStgdb->m_MiniMd.PutToken(
        TBL_FieldLayout,
        FieldLayoutRec::COL_Field,
        pFieldLayoutRec,
        fd));
    pFieldLayoutRec->SetOffSet(ulOffset);
    IfFailGo( m_pStgdb->m_MiniMd.AddFieldLayoutToHash(iFieldLayoutRec) );

ErrExit:

    return hr;
} // RegMeta::SetFieldLayout

//*******************************************************************************
// helper to define event
//
// Implements internal API code:IMetaDataEmitHelper::DefineEventHelper.
//*******************************************************************************
STDMETHODIMP RegMeta::DefineEventHelper(    // Return hresult.
    mdTypeDef   td,                     // [IN] the class/interface on which the event is being defined
    LPCWSTR     szEvent,                // [IN] Name of the event
    DWORD       dwEventFlags,           // [IN] CorEventAttr
    mdToken     tkEventType,            // [IN] a reference (mdTypeRef or mdTypeRef) to the Event class
    mdEvent     *pmdEvent)              // [OUT] output event token
{
    HRESULT     hr = S_OK;
    LOG((LOGMD, "MD RegMeta::DefineEventHelper(0x%08x, %S, 0x%08x, 0x%08x, 0x%08x)\n",
        td, szEvent, dwEventFlags, tkEventType, pmdEvent));

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    hr = _DefineEvent(td, szEvent, dwEventFlags, tkEventType, pmdEvent);

ErrExit:
    return hr;
} // RegMeta::DefineEvent


//*******************************************************************************
// helper to add a declarative security blob to a class or method
//
// Implements internal API code:IMetaDataEmitHelper::AddDeclarativeSecurityHelper.
//*******************************************************************************
STDMETHODIMP RegMeta::AddDeclarativeSecurityHelper(
    mdToken     tk,                     // [IN] Parent token (typedef/methoddef)
    DWORD       dwAction,               // [IN] Security action (CorDeclSecurity)
    void const  *pValue,                // [IN] Permission set blob
    DWORD       cbValue,                // [IN] Byte count of permission set blob
    mdPermission*pmdPermission)         // [OUT] Output permission token
{
    HRESULT         hr = S_OK;
    DeclSecurityRec *pDeclSec = NULL;
    RID             iDeclSec;
    short           sAction = static_cast<short>(dwAction);
    mdPermission    tkPerm  = mdTokenNil;

    LOG((LOGMD, "MD RegMeta::AddDeclarativeSecurityHelper(0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        tk, dwAction, pValue, cbValue, pmdPermission));

    LOCKWRITE();
    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    _ASSERTE(TypeFromToken(tk) == mdtTypeDef || TypeFromToken(tk) == mdtMethodDef || TypeFromToken(tk) == mdtAssembly);

    // Check for valid Action.
    if (sAction == 0 || sAction > dclMaximumValue)
        IfFailGo(E_INVALIDARG);

    if (CheckDups(MDDupPermission))
    {
        hr = ImportHelper::FindPermission(&(m_pStgdb->m_MiniMd), tk, sAction, &tkPerm);

        if (SUCCEEDED(hr))
        {
            // Set output parameter.
            if (pmdPermission)
                *pmdPermission = tkPerm;
            if (IsENCOn())
                IfFailGo(m_pStgdb->m_MiniMd.GetDeclSecurityRecord(RidFromToken(tkPerm), &pDeclSec));
            else
            {
                hr = META_S_DUPLICATE;
                goto ErrExit;
            }
        }
        else if (hr != CLDB_E_RECORD_NOTFOUND)
            IfFailGo(hr);
    }

    // Create a new record.
    if (!pDeclSec)
    {
        IfFailGo(m_pStgdb->m_MiniMd.AddDeclSecurityRecord(&pDeclSec, &iDeclSec));
        tkPerm = TokenFromRid(iDeclSec, mdtPermission);

        // Set output parameter.
        if (pmdPermission)
            *pmdPermission = tkPerm;

        // Save parent and action information.
        IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_DeclSecurity, DeclSecurityRec::COL_Parent, pDeclSec, tk));
        pDeclSec->SetAction(sAction);

        // Turn on the internal security flag on the parent.
        if (TypeFromToken(tk) == mdtTypeDef)
            IfFailGo(_TurnInternalFlagsOn(tk, tdHasSecurity));
        else if (TypeFromToken(tk) == mdtMethodDef)
            IfFailGo(_TurnInternalFlagsOn(tk, mdHasSecurity));
        IfFailGo(UpdateENCLog(tk));
    }

    // Write the blob into the record.
    IfFailGo(m_pStgdb->m_MiniMd.PutBlob(TBL_DeclSecurity, DeclSecurityRec::COL_PermissionSet,
                                        pDeclSec, pValue, cbValue));

    IfFailGo(UpdateENCLog(tkPerm));

ErrExit:

    return hr;
} // RegMeta::AddDeclarativeSecurityHelper


//*******************************************************************************
// helper to set type's extends column
//
// Implements internal API code:IMetaDataEmitHelper::SetTypeParent.
//*******************************************************************************
HRESULT RegMeta::SetTypeParent(         // Return hresult.
    mdTypeDef   td,                     // [IN] Type definition
    mdToken     tkExtends)              // [IN] parent type
{
    HRESULT     hr;
    TypeDefRec  *pRec;

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.GetTypeDefRecord(RidFromToken(td), &pRec));
    IfFailGo( m_pStgdb->m_MiniMd.PutToken(TBL_TypeDef, TypeDefRec::COL_Extends, pRec, tkExtends) );

ErrExit:
    return hr;
} // RegMeta::SetTypeParent


//*******************************************************************************
// helper to set type's extends column
//
// Implements internal API code:IMetaDataEmitHelper::AddInterfaceImpl.
//*******************************************************************************
HRESULT RegMeta::AddInterfaceImpl(      // Return hresult.
    mdTypeDef   td,                     // [IN] Type definition
    mdToken     tkInterface)            // [IN] interface type
{
    HRESULT             hr;
    InterfaceImplRec    *pRec;
    RID                 ii;

    LOCKWRITE();
    hr = ImportHelper::FindInterfaceImpl(&(m_pStgdb->m_MiniMd), td, tkInterface, (mdInterfaceImpl *)&ii);
    if (hr == S_OK)
        goto ErrExit;
    IfFailGo(m_pStgdb->m_MiniMd.AddInterfaceImplRecord(&pRec, &ii));
    IfFailGo(m_pStgdb->m_MiniMd.PutToken( TBL_InterfaceImpl, InterfaceImplRec::COL_Class, pRec, td));
    IfFailGo(m_pStgdb->m_MiniMd.PutToken( TBL_InterfaceImpl, InterfaceImplRec::COL_Interface, pRec, tkInterface));

ErrExit:
    return hr;
} // RegMeta::AddInterfaceImpl

#ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB
//*******************************************************************************
// Helper to determine path separator and number of separated parts.
//
// Implements internal API code:IMDInternalEmit::GetPathSeparator.
//*******************************************************************************
HRESULT
RegMeta::GetPathSeparator(
    char    *path,
    char    *separator,
    ULONG   *partsCount)
{
    const char delimiters[] = { '\\', '/', '\0'};
    ULONG tokens = 1;

    // try first
    char delim = delimiters[0];
    char* charPtr = strchr(path, delim);

    if (charPtr != NULL)
    {
        // count tokens
        while (charPtr != NULL)
        {
            tokens++;
            charPtr = strchr(charPtr + 1, delim);
        }
    }
    else
    {
        // try second
        delim = delimiters[1];
        charPtr = strchr(path, delim);
        if (charPtr != NULL)
        {
            // count tokens
            while (charPtr != NULL)
            {
                tokens++;
                charPtr = strchr(charPtr + 1, delim);
            }
        }
        else
        {
            // delimiter not found - set to \0;
            delim = delimiters[2];
        }
    }

    *separator = delim;
    *partsCount = tokens;

    return S_OK;
} // RegMeta::GetPathSeparator
#endif // FEATURE_METADATA_EMIT_PORTABLE_PDB

#endif //FEATURE_METADATA_EMIT && FEATURE_METADATA_INTERNAL_APIS

#ifdef FEATURE_METADATA_INTERNAL_APIS

//*****************************************************************************
// Helper : get metadata information
//
// Implements internal API code:IMetaDataHelper::GetMetadata.
//*****************************************************************************
STDMETHODIMP
RegMeta::GetMetadata(
    ULONG   ulSelect,   // [IN] Selector.
    void ** ppData)     // [OUT] Put pointer to data here.
{

    REGMETA_POSSIBLE_INTERNAL_POINTER_EXPOSED();

    switch (ulSelect)
    {
    case 0:
        *ppData = &m_pStgdb->m_MiniMd;
        break;
    case 1:
        *ppData = (void*)g_CodedTokens;
        break;
    case 2:
        *ppData = (void*)g_Tables;
        break;
    default:
        *ppData = 0;
        break;
    }

    return S_OK;
} // RegMeta::GetMetadata

//*******************************************************************************
// helper to change MVID
//
// Implements internal API code:IMDInternalEmit::ChangeMvid.
//*******************************************************************************
HRESULT RegMeta::ChangeMvid(            // S_OK or error.
    REFGUID newMvid)                    // GUID to use as the MVID
{
    return GetMiniMd()->ChangeMvid(newMvid);
}

//*******************************************************************************
// Helper to change MDUpdateMode value to updateMode.
//
// Implements internal API code:IMDInternalEmit::SetMDUpdateMode.
//*******************************************************************************
HRESULT
RegMeta::SetMDUpdateMode(
    ULONG   updateMode,
    ULONG * pPreviousUpdateMode)
{
    HRESULT hr;

    OptionValue optionValue;
    IfFailGo(m_pStgdb->m_MiniMd.GetOption(&optionValue));
    if (pPreviousUpdateMode != NULL)
    {
        *pPreviousUpdateMode = optionValue.m_UpdateMode;
    }
    optionValue.m_UpdateMode = updateMode;
    IfFailGo(m_pStgdb->m_MiniMd.SetOption(&optionValue));

ErrExit:
    return hr;
} // RegMeta::SetMDUpdateMode

#endif //FEATURE_METADATA_INTERNAL_APIS
