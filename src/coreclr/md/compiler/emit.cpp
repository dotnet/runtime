// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// Emit.cpp
//

//
// Implementation for the meta data emit code.
//
//*****************************************************************************
#include "stdafx.h"
#include "regmeta.h"
#include "mdutil.h"
#include "rwutil.h"
#include "mdlog.h"
#include "importhelper.h"

#ifdef FEATURE_METADATA_EMIT

#ifdef _MSC_VER
#pragma warning(disable: 4102)
#endif

//*****************************************************************************
// Create and set a new MethodDef record.
//*****************************************************************************
STDMETHODIMP RegMeta::DefineMethod(           // S_OK or error.
    mdTypeDef   td,                     // Parent TypeDef
    LPCWSTR     szName,                 // Name of member
    DWORD       dwMethodFlags,          // Member attributes
    PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of COM+ signature
    ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
    ULONG       ulCodeRVA,
    DWORD       dwImplFlags,
    mdMethodDef *pmd)                   // Put member token here
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    MethodRec   *pRecord = NULL;        // The new record.
    RID         iRecord;                // The new record's RID.
    LPUTF8      szNameUtf8;
    UTF8STR(szName, szNameUtf8);

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    _ASSERTE(pmd);

    // Make sure no one sets the reserved bits on the way in.
    dwMethodFlags &= (~mdReservedMask);

    IsGlobalMethodParent(&td);

    // See if this method has already been defined.
    if (CheckDups(MDDupMethodDef))
    {
        hr = ImportHelper::FindMethod(
            &(m_pStgdb->m_MiniMd),
            td,
            szNameUtf8,
            pvSigBlob,
            cbSigBlob,
            pmd);

        if (SUCCEEDED(hr))
        {
            if (IsENCOn())
            {
                IfFailGo(m_pStgdb->m_MiniMd.GetMethodRecord(RidFromToken(*pmd), &pRecord));
            }
            else
            {
                hr = META_S_DUPLICATE;
                goto ErrExit;
            }
        }
        else if (hr != CLDB_E_RECORD_NOTFOUND)
        {
            IfFailGo(hr);
        }
    }

    // Create the new record.
    if (pRecord == NULL)
    {
        IfFailGo(m_pStgdb->m_MiniMd.AddMethodRecord(&pRecord, &iRecord));

        // Give token back to caller.
        *pmd = TokenFromRid(iRecord, mdtMethodDef);

        // Add to parent's list of child records.
        IfFailGo(m_pStgdb->m_MiniMd.AddMethodToTypeDef(RidFromToken(td), iRecord));

        IfFailGo(UpdateENCLog(td, CMiniMdRW::eDeltaMethodCreate));

        // record the more defs are introduced.
        SetMemberDefDirty(true);
    }

    // Set the method properties.
    IfFailGo(m_pStgdb->m_MiniMd.PutString(TBL_Method, MethodRec::COL_Name, pRecord, szNameUtf8));
    IfFailGo(m_pStgdb->m_MiniMd.PutBlob(TBL_Method, MethodRec::COL_Signature, pRecord, pvSigBlob, cbSigBlob));

    // <TODO>@FUTURE: possible performance improvement here to check _ first of all.</TODO>
    // .ctor and .cctor below are defined in corhdr.h.  However, corhdr.h does not have the
    // the W() macro we need (since it's distributed to windows).  We substitute the values of each
    // macro in the code below to work around this issue.
    // #define COR_CTOR_METHOD_NAME_W      L".ctor"
    // #define COR_CCTOR_METHOD_NAME_W     L".cctor"

    if (!u16_strcmp(szName, W(".ctor")) || // COR_CTOR_METHOD_NAME_W
        !u16_strcmp(szName, W(".cctor")) || // COR_CCTOR_METHOD_NAME_W
        !u16_strncmp(szName, W("_VtblGap"), 8) ) // All methods that begin with the characters "_VtblGap" are considered to be VTable Gap methods
    {
        dwMethodFlags |= mdRTSpecialName | mdSpecialName;
    }
    SetCallerDefine();
    IfFailGo(_SetMethodProps(*pmd, dwMethodFlags, ulCodeRVA, dwImplFlags));

    IfFailGo(m_pStgdb->m_MiniMd.AddMemberDefToHash(*pmd, td) );

ErrExit:
    SetCallerExternal();

    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::DefineMethod

//*****************************************************************************
// Create and set a MethodImpl Record.
//*****************************************************************************
STDMETHODIMP RegMeta::DefineMethodImpl(       // S_OK or error.
    mdTypeDef   td,                     // [IN] The class implementing the method
    mdToken     tkBody,                 // [IN] Method body, MethodDef or MethodRef
    mdToken     tkDecl)                 // [IN] Method declaration, MethodDef or MethodRef
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    MethodImplRec   *pMethodImplRec = NULL;
    RID             iMethodImplRec;

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    _ASSERTE(TypeFromToken(td) == mdtTypeDef);
    _ASSERTE(TypeFromToken(tkBody) == mdtMemberRef || TypeFromToken(tkBody) == mdtMethodDef);
    _ASSERTE(TypeFromToken(tkDecl) == mdtMemberRef || TypeFromToken(tkDecl) == mdtMethodDef);
    _ASSERTE(!IsNilToken(td) && !IsNilToken(tkBody) && !IsNilToken(tkDecl));

    // Check for duplicates.
    if (CheckDups(MDDupMethodDef))
    {
        hr = ImportHelper::FindMethodImpl(&m_pStgdb->m_MiniMd, td, tkBody, tkDecl, NULL);
        if (SUCCEEDED(hr))
        {
            hr = META_S_DUPLICATE;
            goto ErrExit;
        }
        else if (hr != CLDB_E_RECORD_NOTFOUND)
            IfFailGo(hr);
    }

    // Create the MethodImpl record.
    IfFailGo(m_pStgdb->m_MiniMd.AddMethodImplRecord(&pMethodImplRec, &iMethodImplRec));

    // Set the values.
    IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_MethodImpl, MethodImplRec::COL_Class,
                                         pMethodImplRec, td));
    IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_MethodImpl, MethodImplRec::COL_MethodBody,
                                         pMethodImplRec, tkBody));
    IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_MethodImpl, MethodImplRec::COL_MethodDeclaration,
                                         pMethodImplRec, tkDecl));

    IfFailGo( m_pStgdb->m_MiniMd.AddMethodImplToHash(iMethodImplRec) );

    IfFailGo(UpdateENCLog2(TBL_MethodImpl, iMethodImplRec));
ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::DefineMethodImpl


//*****************************************************************************
// Set or update RVA and ImplFlags for the given MethodDef or FieldDef record.
//*****************************************************************************
STDMETHODIMP RegMeta::SetMethodImplFlags(     // [IN] S_OK or error.
    mdMethodDef md,                     // [IN] Method for which to set impl flags
    DWORD       dwImplFlags)
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    MethodRec   *pMethodRec;

    LOCKWRITE();

    _ASSERTE(TypeFromToken(md) == mdtMethodDef && dwImplFlags != UINT32_MAX);

    // Get the record.
    IfFailGo(m_pStgdb->m_MiniMd.GetMethodRecord(RidFromToken(md), &pMethodRec));
    pMethodRec->SetImplFlags(static_cast<USHORT>(dwImplFlags));

    IfFailGo(UpdateENCLog(md));

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::SetMethodImplFlags


//*****************************************************************************
// Set or update RVA and ImplFlags for the given MethodDef or FieldDef record.
//*****************************************************************************
STDMETHODIMP RegMeta::SetFieldRVA(            // [IN] S_OK or error.
    mdFieldDef  fd,                     // [IN] Field for which to set offset
    ULONG       ulRVA)                  // [IN] The offset
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    FieldRVARec     *pFieldRVARec;
    RID             iFieldRVA;
    FieldRec        *pFieldRec;

    LOCKWRITE();

    _ASSERTE(TypeFromToken(fd) == mdtFieldDef);

    IfFailGo(m_pStgdb->m_MiniMd.FindFieldRVAHelper(fd, &iFieldRVA));

    if (InvalidRid(iFieldRVA))
    {
        // turn on the has field RVA bit
        IfFailGo(m_pStgdb->m_MiniMd.GetFieldRecord(RidFromToken(fd), &pFieldRec));
        pFieldRec->AddFlags(fdHasFieldRVA);

        // Create a new record.
        IfFailGo(m_pStgdb->m_MiniMd.AddFieldRVARecord(&pFieldRVARec, &iFieldRVA));

        // Set the data.
        IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_FieldRVA, FieldRVARec::COL_Field,
                                            pFieldRVARec, fd));
        IfFailGo( m_pStgdb->m_MiniMd.AddFieldRVAToHash(iFieldRVA) );
    }
    else
    {
        // Get the record.
        IfFailGo(m_pStgdb->m_MiniMd.GetFieldRVARecord(iFieldRVA, &pFieldRVARec));
    }

    // Set the data.
    pFieldRVARec->SetRVA(ulRVA);

    IfFailGo(UpdateENCLog2(TBL_FieldRVA, iFieldRVA));

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::SetFieldRVA


//*****************************************************************************
// Helper: Set or update RVA and ImplFlags for the given MethodDef or MethodImpl record.
//*****************************************************************************
HRESULT RegMeta::_SetRVA(               // [IN] S_OK or error.
    mdToken     tk,                     // [IN] Member for which to set offset
    ULONG       ulCodeRVA,              // [IN] The offset
    DWORD       dwImplFlags)
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    _ASSERTE(TypeFromToken(tk) == mdtMethodDef || TypeFromToken(tk) == mdtFieldDef);
    _ASSERTE(!IsNilToken(tk));

    if (TypeFromToken(tk) == mdtMethodDef)
    {
        MethodRec   *pMethodRec;

        // Get the record.
        IfFailGo(m_pStgdb->m_MiniMd.GetMethodRecord(RidFromToken(tk), &pMethodRec));

        // Set the data.
        pMethodRec->SetRVA(ulCodeRVA);

        // Do not set the flag value unless its valid.
        if (dwImplFlags != UINT32_MAX)
            pMethodRec->SetImplFlags(static_cast<USHORT>(dwImplFlags));

        IfFailGo(UpdateENCLog(tk));
    }
    else            // TypeFromToken(tk) == mdtFieldDef
    {
        _ASSERTE(dwImplFlags==0 || dwImplFlags==UINT32_MAX);

        FieldRVARec     *pFieldRVARec;
        RID             iFieldRVA;
        FieldRec        *pFieldRec;

        IfFailGo(m_pStgdb->m_MiniMd.FindFieldRVAHelper(tk, &iFieldRVA));

        if (InvalidRid(iFieldRVA))
        {
            // turn on the has field RVA bit
            IfFailGo(m_pStgdb->m_MiniMd.GetFieldRecord(RidFromToken(tk), &pFieldRec));
            pFieldRec->AddFlags(fdHasFieldRVA);

            // Create a new record.
            IfFailGo(m_pStgdb->m_MiniMd.AddFieldRVARecord(&pFieldRVARec, &iFieldRVA));

            // Set the data.
            IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_FieldRVA, FieldRVARec::COL_Field,
                                                pFieldRVARec, tk));

            IfFailGo( m_pStgdb->m_MiniMd.AddFieldRVAToHash(iFieldRVA) );

        }
        else
        {
            // Get the record.
            IfFailGo(m_pStgdb->m_MiniMd.GetFieldRVARecord(iFieldRVA, &pFieldRVARec));
        }

        // Set the data.
        pFieldRVARec->SetRVA(ulCodeRVA);

        IfFailGo(UpdateENCLog2(TBL_FieldRVA, iFieldRVA));
    }

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::_SetRVA

//*****************************************************************************
// Given a name, create a TypeRef.
//*****************************************************************************
STDMETHODIMP RegMeta::DefineTypeRefByName(    // S_OK or error.
    mdToken     tkResolutionScope,      // [IN] ModuleRef or AssemblyRef.
    LPCWSTR     szName,                 // [IN] Name of the TypeRef.
    mdTypeRef   *ptr)                   // [OUT] Put TypeRef token here.
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    // Common helper function does all of the work.
    IfFailGo(_DefineTypeRef(tkResolutionScope, szName, TRUE, ptr));

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::DefineTypeRefByName

//*****************************************************************************
// Create a reference, in an emit scope, to a TypeDef in another scope.
//*****************************************************************************
STDMETHODIMP RegMeta::DefineImportType(       // S_OK or error.
    IMetaDataAssemblyImport *pAssemImport,  // [IN] Assembly containing the TypeDef.
    const void  *pbHashValue,           // [IN] Hash Blob for Assembly.
    ULONG    cbHashValue,           // [IN] Count of bytes.
    IMetaDataImport *pImport,           // [IN] Scope containing the TypeDef.
    mdTypeDef   tdImport,               // [IN] The imported TypeDef.
    IMetaDataAssemblyEmit *pAssemEmit,  // [IN] Assembly into which the TypeDef is imported.
    mdTypeRef   *ptr)                   // [OUT] Put TypeRef token here.
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    IMetaDataImport2 *pImport2 = NULL;
    IMDCommon        *pImport2MDCommon = NULL;

    IMDCommon        *pAssemImportMDCommon = NULL;

    RegMeta     *pAssemEmitRM = NULL;
    CMiniMdRW   *pMiniMdAssemEmit =  NULL;
    CMiniMdRW   *pMiniMdEmit = NULL;

    IMetaModelCommon *pAssemImportMetaModelCommon;
    IMetaModelCommon *pImport2MetaModelCommon;

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());
    IfFailGo(pImport->QueryInterface(IID_IMetaDataImport2, (void**)&pImport2));

    if (pAssemImport)
    {
        IfFailGo(pAssemImport->QueryInterface(IID_IMDCommon, (void**)&pAssemImportMDCommon));
    }

    pAssemImportMetaModelCommon = pAssemImportMDCommon ? pAssemImportMDCommon->GetMetaModelCommon() : 0;

    IfFailGo(pImport2->QueryInterface(IID_IMDCommon, (void**)&pImport2MDCommon));
    pImport2MetaModelCommon = pImport2MDCommon->GetMetaModelCommon();

    pAssemEmitRM = static_cast<RegMeta*>(pAssemEmit);
    pMiniMdAssemEmit =  pAssemEmitRM ? static_cast<CMiniMdRW*>(&pAssemEmitRM->m_pStgdb->m_MiniMd) : 0;
    pMiniMdEmit = &m_pStgdb->m_MiniMd;

    IfFailGo(ImportHelper::ImportTypeDef(
                        pMiniMdAssemEmit,
                        pMiniMdEmit,
                        pAssemImportMetaModelCommon,
                        pbHashValue, cbHashValue,
                        pImport2MetaModelCommon,
                        tdImport,
                        false,  // Do not optimize to TypeDef if import and emit scopes are identical.
                        ptr));

ErrExit:
    if (pImport2)
        pImport2->Release();
    if (pImport2MDCommon)
        pImport2MDCommon->Release();
    if (pAssemImportMDCommon)
        pAssemImportMDCommon->Release();

    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::DefineImportType

//*****************************************************************************
// Create and set a MemberRef record.
//*****************************************************************************
STDMETHODIMP RegMeta::DefineMemberRef(        // S_OK or error
    mdToken     tkImport,               // [IN] ClassRef or ClassDef importing a member.
    LPCWSTR     szName,                 // [IN] member's name
    PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of COM+ signature
    ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
    mdMemberRef *pmr)                   // [OUT] memberref token
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    MemberRefRec    *pRecord = 0;       // The MemberRef record.
    RID             iRecord;            // RID of new MemberRef record.
    LPUTF8          szNameUtf8;
    UTF8STR(szName, szNameUtf8);

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    _ASSERTE(TypeFromToken(tkImport) == mdtTypeRef ||
             TypeFromToken(tkImport) == mdtModuleRef ||
             TypeFromToken(tkImport) == mdtMethodDef ||
             TypeFromToken(tkImport) == mdtTypeSpec ||
             (TypeFromToken(tkImport) == mdtTypeDef) ||
             IsNilToken(tkImport));

    _ASSERTE(szName && pvSigBlob && cbSigBlob && pmr);

    // _ASSERTE(_IsValidToken(tkImport));

    // Set token to m_tdModule if referring to a global function.
    if (IsNilToken(tkImport))
        tkImport = m_tdModule;

    // If the MemberRef already exists, just return the token, else
    // create a new record.
    if (CheckDups(MDDupMemberRef))
    {
        hr = ImportHelper::FindMemberRef(&(m_pStgdb->m_MiniMd), tkImport, szNameUtf8, pvSigBlob, cbSigBlob, pmr);
        if (SUCCEEDED(hr))
        {
            if (IsENCOn())
                IfFailGo(m_pStgdb->m_MiniMd.GetMemberRefRecord(RidFromToken(*pmr), &pRecord));
            else
            {
                hr = META_S_DUPLICATE;
                goto ErrExit;
            }
        }
        else if (hr != CLDB_E_RECORD_NOTFOUND)      // MemberRef exists
            IfFailGo(hr);
    }

    if (!pRecord)
    {   // Create the record.
        IfFailGo(m_pStgdb->m_MiniMd.AddMemberRefRecord(&pRecord, &iRecord));

        // record the more defs are introduced.
        SetMemberDefDirty(true);

        // Give token to caller.
        *pmr = TokenFromRid(iRecord, mdtMemberRef);
    }

    // Save row data.
    IfFailGo(m_pStgdb->m_MiniMd.PutString(TBL_MemberRef, MemberRefRec::COL_Name, pRecord, szNameUtf8));
    IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_MemberRef, MemberRefRec::COL_Class, pRecord, tkImport));
    IfFailGo(m_pStgdb->m_MiniMd.PutBlob(TBL_MemberRef, MemberRefRec::COL_Signature, pRecord,
                                pvSigBlob, cbSigBlob));

    IfFailGo(m_pStgdb->m_MiniMd.AddMemberRefToHash(*pmr) );

    IfFailGo(UpdateENCLog(*pmr));

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::DefineMemberRef

//*****************************************************************************
// Create a MemberRef record based on a member in an import scope.
//*****************************************************************************
STDMETHODIMP RegMeta::DefineImportMember(     // S_OK or error.
    IMetaDataAssemblyImport *pAssemImport,  // [IN] Assembly containing the Member.
    const void  *pbHashValue,           // [IN] Hash Blob for Assembly.
    ULONG        cbHashValue,           // [IN] Count of bytes.
    IMetaDataImport *pImport,           // [IN] Import scope, with member.
    mdToken     mbMember,               // [IN] Member in import scope.
    IMetaDataAssemblyEmit *pAssemEmit,  // [IN] Assembly into which the Member is imported.
    mdToken     tkImport,               // [IN] Classref or classdef in emit scope.
    mdMemberRef *pmr)                   // [OUT] Put member ref here.
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    // No need to lock this function. All the functions that it calls are public APIs.

    _ASSERTE(pImport && pmr);
    _ASSERTE(TypeFromToken(tkImport) == mdtTypeRef || TypeFromToken(tkImport) == mdtModuleRef ||
                IsNilToken(tkImport) || TypeFromToken(tkImport) == mdtTypeSpec);
    _ASSERTE((TypeFromToken(mbMember) == mdtMethodDef && mbMember != mdMethodDefNil) ||
             (TypeFromToken(mbMember) == mdtFieldDef && mbMember != mdFieldDefNil));

    CQuickArray<WCHAR> qbMemberName;    // Name of the imported member.
    CQuickArray<WCHAR> qbScopeName;     // Name of the imported member's scope.
    GUID        mvidImport;             // MVID of the import module.
    GUID        mvidEmit;               // MVID of the emit module.
    ULONG       cchName;                // Length of a name, in wide chars.
    PCCOR_SIGNATURE pvSig;              // Member's signature.
    ULONG       cbSig;                  // Length of member's signature.
    CQuickBytes cqbTranslatedSig;       // Buffer for signature translation.
    ULONG       cbTranslatedSig;        // Length of translated signature.

    if (TypeFromToken(mbMember) == mdtMethodDef)
    {
        do {
            hr = pImport->GetMethodProps(mbMember, 0, qbMemberName.Ptr(),(DWORD)qbMemberName.MaxSize(),&cchName,
                0, &pvSig,&cbSig, 0,0);
            if (hr == CLDB_S_TRUNCATION)
            {
                IfFailGo(qbMemberName.ReSizeNoThrow(cchName));
                continue;
            }
            break;
        } while (1);
    }
    else    // TypeFromToken(mbMember) == mdtFieldDef
    {
        do {
            hr = pImport->GetFieldProps(mbMember, 0, qbMemberName.Ptr(),(DWORD)qbMemberName.MaxSize(),&cchName,
                0, &pvSig,&cbSig, 0,0, 0);
            if (hr == CLDB_S_TRUNCATION)
            {
                IfFailGo(qbMemberName.ReSizeNoThrow(cchName));
                continue;
            }
            break;
        } while (1);
    }
    IfFailGo(hr);

    IfFailGo(cqbTranslatedSig.ReSizeNoThrow(cbSig * 3));       // Set size conservatively.

    IfFailGo(TranslateSigWithScope(
        pAssemImport,
        pbHashValue,
        cbHashValue,
        pImport,
        pvSig,
        cbSig,
        pAssemEmit,
        static_cast<IMetaDataEmit*>(static_cast<IMetaDataEmit2*>(this)),
        (COR_SIGNATURE *)cqbTranslatedSig.Ptr(),
        cbSig * 3,
        &cbTranslatedSig));

    // Define ModuleRef for imported Member functions

    // Check if the Member being imported is a global function.
    IfFailGo(GetScopeProps(0, 0, 0, &mvidEmit));
    IfFailGo(pImport->GetScopeProps(0, 0,&cchName, &mvidImport));
    if (mvidEmit != mvidImport && IsNilToken(tkImport))
    {
        IfFailGo(qbScopeName.ReSizeNoThrow(cchName));
        IfFailGo(pImport->GetScopeProps(qbScopeName.Ptr(),(DWORD)qbScopeName.MaxSize(),
                                        0, 0));
        IfFailGo(DefineModuleRef(qbScopeName.Ptr(), &tkImport));
    }

    // Define MemberRef base on the name, sig, and parent
    IfFailGo(DefineMemberRef(
        tkImport,
        qbMemberName.Ptr(),
        reinterpret_cast<PCCOR_SIGNATURE>(cqbTranslatedSig.Ptr()),
        cbTranslatedSig,
        pmr));

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::DefineImportMember

//*****************************************************************************
// Define and set a Event record.
//*****************************************************************************
STDMETHODIMP RegMeta::DefineEvent(
    mdTypeDef   td,                     // [IN] the class/interface on which the event is being defined
    LPCWSTR     szEvent,                // [IN] Name of the event
    DWORD       dwEventFlags,           // [IN] CorEventAttr
    mdToken     tkEventType,            // [IN] a reference (mdTypeRef or mdTypeRef(to the Event class
    mdMethodDef mdAddOn,                // [IN] required add method
    mdMethodDef mdRemoveOn,             // [IN] required remove method
    mdMethodDef mdFire,                 // [IN] optional fire method
    mdMethodDef rmdOtherMethods[],      // [IN] optional array of other methods associate with the event
    mdEvent     *pmdEvent)              // [OUT] output event token
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    _ASSERTE(TypeFromToken(td) == mdtTypeDef && td != mdTypeDefNil);
    _ASSERTE(IsNilToken(tkEventType) || TypeFromToken(tkEventType) == mdtTypeDef ||
                TypeFromToken(tkEventType) == mdtTypeRef || TypeFromToken(tkEventType) == mdtTypeSpec);
    _ASSERTE(TypeFromToken(mdAddOn) == mdtMethodDef && mdAddOn != mdMethodDefNil);
    _ASSERTE(TypeFromToken(mdRemoveOn) == mdtMethodDef && mdRemoveOn != mdMethodDefNil);
    _ASSERTE(IsNilToken(mdFire) || TypeFromToken(mdFire) == mdtMethodDef);
    _ASSERTE(szEvent && pmdEvent);

    hr = _DefineEvent(td, szEvent, dwEventFlags, tkEventType, pmdEvent);
    if (hr != S_OK)
        goto ErrExit;

    IfFailGo(_SetEventProps2(*pmdEvent, mdAddOn, mdRemoveOn, mdFire, rmdOtherMethods, IsENCOn()));
    IfFailGo(UpdateENCLog(*pmdEvent));
ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::DefineEvent

//*****************************************************************************
// Set the ClassLayout information.
//
// If a row already exists for this class in the layout table, the layout
// information is overwritten.
//*****************************************************************************
STDMETHODIMP RegMeta::SetClassLayout(
    mdTypeDef   td,                     // [IN] typedef
    DWORD       dwPackSize,             // [IN] packing size specified as 1, 2, 4, 8, or 16
    COR_FIELD_OFFSET rFieldOffsets[],   // [IN] array of layout specification
    ULONG       ulClassSize)            // [IN] size of the class
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;              // A result.

    int         index = 0;              // Loop control.

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    _ASSERTE(TypeFromToken(td) == mdtTypeDef);

    // Create entries in the FieldLayout table.
    if (rFieldOffsets)
    {
        mdFieldDef tkfd;
        // Iterate the list of fields...
        for (index = 0; rFieldOffsets[index].ridOfField != mdFieldDefNil; index++)
        {
            if (rFieldOffsets[index].ulOffset != UINT32_MAX)
            {
                tkfd = TokenFromRid(rFieldOffsets[index].ridOfField, mdtFieldDef);

                IfFailGo(_SetFieldOffset(tkfd, rFieldOffsets[index].ulOffset));
            }
        }
    }

    IfFailGo(_SetClassLayout(td, dwPackSize, ulClassSize));

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::SetClassLayout

//*****************************************************************************
// Helper function to set a class layout for a given class.
//*****************************************************************************
HRESULT RegMeta::_SetClassLayout(       // S_OK or error.
    mdTypeDef   td,                     // [IN] The class.
    ULONG       dwPackSize,             // [IN] The packing size.
    ULONG       ulClassSize)            // [IN, OPTIONAL] The class size.
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT     hr = S_OK;              // A result.
    ClassLayoutRec  *pClassLayout;      // A classlayout record.
    RID         iClassLayout = 0;       // RID of classlayout record.

    // See if a ClassLayout record already exists for the given TypeDef.
    IfFailGo(m_pStgdb->m_MiniMd.FindClassLayoutHelper(td, &iClassLayout));

    if (InvalidRid(iClassLayout))
    {
        IfFailGo(m_pStgdb->m_MiniMd.AddClassLayoutRecord(&pClassLayout, &iClassLayout));
        // Set the Parent entry.
        IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_ClassLayout, ClassLayoutRec::COL_Parent,
                                            pClassLayout, td));
        IfFailGo( m_pStgdb->m_MiniMd.AddClassLayoutToHash(iClassLayout) );
    }
    else
    {
        IfFailGo(m_pStgdb->m_MiniMd.GetClassLayoutRecord(iClassLayout, &pClassLayout));
    }

    // Set the data.
    if (dwPackSize != UINT32_MAX)
        pClassLayout->SetPackingSize(static_cast<USHORT>(dwPackSize));
    if (ulClassSize != UINT32_MAX)
        pClassLayout->SetClassSize(ulClassSize);

    // Create the log record for the non-token record.
    IfFailGo(UpdateENCLog2(TBL_ClassLayout, iClassLayout));

ErrExit:

    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::_SetClassLayout

//*****************************************************************************
// Helper function to set a field offset for a given field def.
//*****************************************************************************
HRESULT RegMeta::_SetFieldOffset(       // S_OK or error.
    mdFieldDef  fd,                     // [IN] The field.
    ULONG       ulOffset)               // [IN] The offset of the field.
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT          hr;
    FieldLayoutRec * pFieldLayoutRec=0;     // A FieldLayout record.
    RID              iFieldLayoutRec=0;     // RID of a FieldLayout record.

    // See if an entry already exists for the Field in the FieldLayout table.
    IfFailGo(m_pStgdb->m_MiniMd.FindFieldLayoutHelper(fd, &iFieldLayoutRec));
    if (InvalidRid(iFieldLayoutRec))
    {
        IfFailGo(m_pStgdb->m_MiniMd.AddFieldLayoutRecord(&pFieldLayoutRec, &iFieldLayoutRec));
        // Set the Field entry.
        IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_FieldLayout, FieldLayoutRec::COL_Field,
                    pFieldLayoutRec, fd));
        IfFailGo( m_pStgdb->m_MiniMd.AddFieldLayoutToHash(iFieldLayoutRec) );
    }
    else
    {
        IfFailGo(m_pStgdb->m_MiniMd.GetFieldLayoutRecord(iFieldLayoutRec, &pFieldLayoutRec));
    }

    // Set the offset.
    pFieldLayoutRec->SetOffSet(ulOffset);

    // Create the log record for the non-token record.
    IfFailGo(UpdateENCLog2(TBL_FieldLayout, iFieldLayoutRec));

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::_SetFieldOffset

//*****************************************************************************
// Delete the ClassLayout information.
//*****************************************************************************
STDMETHODIMP RegMeta::DeleteClassLayout(
    mdTypeDef   td)                     // [IN] typdef token
{
#ifdef FEATURE_METADATA_EMIT_ALL
    HRESULT hr = S_OK;

    ClassLayoutRec  *pClassLayoutRec;
    TypeDefRec  *pTypeDefRec;
    FieldLayoutRec *pFieldLayoutRec;
    RID         iClassLayoutRec;
    RID         iFieldLayoutRec;
    RID         ridStart;
    RID         ridEnd;
    RID         ridCur;
    ULONG       index;

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    _ASSERTE(!m_bSaveOptimized && "Cannot change records after PreSave() and before Save().");
    _ASSERTE(TypeFromToken(td) == mdtTypeDef && !IsNilToken(td));

    // Get the ClassLayout record.
    IfFailGo(m_pStgdb->m_MiniMd.FindClassLayoutHelper(td, &iClassLayoutRec));
    if (InvalidRid(iClassLayoutRec))
    {
        hr = CLDB_E_RECORD_NOTFOUND;
        goto ErrExit;
    }
    IfFailGo(m_pStgdb->m_MiniMd.GetClassLayoutRecord(iClassLayoutRec, &pClassLayoutRec));

    // Clear the parent.
    IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_ClassLayout,
                                         ClassLayoutRec::COL_Parent,
                                         pClassLayoutRec, mdTypeDefNil));

    // Create the log record for the non-token record.
    IfFailGo(UpdateENCLog2(TBL_ClassLayout, iClassLayoutRec));

    // Delete all the corresponding FieldLayout records if there are any.
    IfFailGo(m_pStgdb->m_MiniMd.GetTypeDefRecord(RidFromToken(td), &pTypeDefRec));
    ridStart = m_pStgdb->m_MiniMd.getFieldListOfTypeDef(pTypeDefRec);
    IfFailGo(m_pStgdb->m_MiniMd.getEndFieldListOfTypeDef(RidFromToken(td), &ridEnd));

    for (index = ridStart; index < ridEnd; index++)
    {
        IfFailGo(m_pStgdb->m_MiniMd.GetFieldRid(index, &ridCur));
        IfFailGo(m_pStgdb->m_MiniMd.FindFieldLayoutHelper(TokenFromRid(ridCur, mdtFieldDef), &iFieldLayoutRec));
        if (InvalidRid(iFieldLayoutRec))
            continue;
        else
        {
            IfFailGo(m_pStgdb->m_MiniMd.GetFieldLayoutRecord(iFieldLayoutRec, &pFieldLayoutRec));
            // Set the Field entry.
            IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_FieldLayout, FieldLayoutRec::COL_Field,
                            pFieldLayoutRec, mdFieldDefNil));
            // Create the log record for the non-token record.
            IfFailGo(UpdateENCLog2(TBL_FieldLayout, iFieldLayoutRec));
        }
    }
ErrExit:
    return hr;
#else //!FEATURE_METADATA_EMIT_ALL
    return E_NOTIMPL;
#endif //!FEATURE_METADATA_EMIT_ALL
} // RegMeta::DeleteClassLayout

//*****************************************************************************
// Set the field's native type.
//*****************************************************************************
STDMETHODIMP RegMeta::SetFieldMarshal(
    mdToken     tk,                     // [IN] given a fieldDef or paramDef token
    PCCOR_SIGNATURE pvNativeType,       // [IN] native type specification
    ULONG       cbNativeType)           // [IN] count of bytes of pvNativeType
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    hr = _SetFieldMarshal(tk, pvNativeType, cbNativeType);

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::SetFieldMarshal

HRESULT RegMeta::_SetFieldMarshal(
    mdToken     tk,                     // [IN] given a fieldDef or paramDef token
    PCCOR_SIGNATURE pvNativeType,       // [IN] native type specification
    ULONG       cbNativeType)           // [IN] count of bytes of pvNativeType
{
    HRESULT     hr = S_OK;
    FieldMarshalRec *pFieldMarshRec;
    RID         iFieldMarshRec = 0;     // initialize to invalid rid

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    _ASSERTE(TypeFromToken(tk) == mdtFieldDef || TypeFromToken(tk) == mdtParamDef);
    _ASSERTE(!IsNilToken(tk));

    // turn on the HasFieldMarshal
    if (TypeFromToken(tk) == mdtFieldDef)
    {
        FieldRec    *pFieldRec;

        IfFailGo(m_pStgdb->m_MiniMd.GetFieldRecord(RidFromToken(tk), &pFieldRec));
        pFieldRec->AddFlags(fdHasFieldMarshal);
    }
    else // TypeFromToken(tk) == mdtParamDef
    {
        ParamRec    *pParamRec;

        IfFailGo(m_pStgdb->m_MiniMd.GetParamRecord(RidFromToken(tk), &pParamRec));
        pParamRec->AddFlags(pdHasFieldMarshal);
    }
    IfFailGo(UpdateENCLog(tk));

    IfFailGo(m_pStgdb->m_MiniMd.FindFieldMarshalHelper(tk, &iFieldMarshRec));
    if (InvalidRid(iFieldMarshRec))
    {
        IfFailGo(m_pStgdb->m_MiniMd.AddFieldMarshalRecord(&pFieldMarshRec, &iFieldMarshRec));
        IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_FieldMarshal, FieldMarshalRec::COL_Parent, pFieldMarshRec, tk));
        IfFailGo( m_pStgdb->m_MiniMd.AddFieldMarshalToHash(iFieldMarshRec) );
    }
    else
    {
        IfFailGo(m_pStgdb->m_MiniMd.GetFieldMarshalRecord(iFieldMarshRec, &pFieldMarshRec));
    }

    // Set data.
    IfFailGo(m_pStgdb->m_MiniMd.PutBlob(TBL_FieldMarshal, FieldMarshalRec::COL_NativeType, pFieldMarshRec,
                                pvNativeType, cbNativeType));

    // Create the log record for the non-token record.
    IfFailGo(UpdateENCLog2(TBL_FieldMarshal, iFieldMarshRec));

ErrExit:

    return hr;
} // RegMeta::_SetFieldMarshal


//*****************************************************************************
// Delete the FieldMarshal record for the given token.
//*****************************************************************************
STDMETHODIMP RegMeta::DeleteFieldMarshal(
    mdToken tk)     // [IN] fieldDef or paramDef token to be deleted.
{
#ifdef FEATURE_METADATA_EMIT_ALL
    HRESULT hr = S_OK;

    FieldMarshalRec *pFieldMarshRec;
    RID         iFieldMarshRec;

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    _ASSERTE(TypeFromToken(tk) == mdtFieldDef || TypeFromToken(tk) == mdtParamDef);
    _ASSERTE(!IsNilToken(tk));
    _ASSERTE(!m_bSaveOptimized && "Cannot delete records after PreSave() and before Save().");

    // Get the FieldMarshal record.
    IfFailGo(m_pStgdb->m_MiniMd.FindFieldMarshalHelper(tk, &iFieldMarshRec));
    if (InvalidRid(iFieldMarshRec))
    {
        hr = CLDB_E_RECORD_NOTFOUND;
        goto ErrExit;
    }
    IfFailGo(m_pStgdb->m_MiniMd.GetFieldMarshalRecord(iFieldMarshRec, &pFieldMarshRec));
    // Clear the parent token from the FieldMarshal record.
    IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_FieldMarshal,
                FieldMarshalRec::COL_Parent, pFieldMarshRec, mdFieldDefNil));

    // turn off the HasFieldMarshal
    if (TypeFromToken(tk) == mdtFieldDef)
    {
        FieldRec    *pFieldRec;

        IfFailGo(m_pStgdb->m_MiniMd.GetFieldRecord(RidFromToken(tk), &pFieldRec));
        pFieldRec->RemoveFlags(fdHasFieldMarshal);
    }
    else // TypeFromToken(tk) == mdtParamDef
    {
        ParamRec    *pParamRec;

        IfFailGo(m_pStgdb->m_MiniMd.GetParamRecord(RidFromToken(tk), &pParamRec));
        pParamRec->RemoveFlags(pdHasFieldMarshal);
    }

    // Update the ENC log for the parent token.
    IfFailGo(UpdateENCLog(tk));
    // Create the log record for the non-token record.
    IfFailGo(UpdateENCLog2(TBL_FieldMarshal, iFieldMarshRec));

ErrExit:
    return hr;
#else //!FEATURE_METADATA_EMIT_ALL
    return E_NOTIMPL;
#endif //!FEATURE_METADATA_EMIT_ALL
} // RegMeta::DeleteFieldMarshal

//*****************************************************************************
// Define a new permission set for an object.
//*****************************************************************************
STDMETHODIMP RegMeta::DefinePermissionSet(
    mdToken     tk,                     // [IN] the object to be decorated.
    DWORD       dwAction,               // [IN] CorDeclSecurity.
    void const  *pvPermission,          // [IN] permission blob.
    ULONG       cbPermission,           // [IN] count of bytes of pvPermission.
    mdPermission *ppm)                  // [OUT] returned permission token.
{
#ifdef FEATURE_METADATA_EMIT_ALL
    HRESULT hr = S_OK;

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    IfFailGo(_DefinePermissionSet(tk, dwAction, pvPermission, cbPermission, ppm));

ErrExit:
    return hr;
#else //!FEATURE_METADATA_EMIT_ALL
    return E_NOTIMPL;
#endif //!FEATURE_METADATA_EMIT_ALL
} // RegMeta::DefinePermissionSet


//*****************************************************************************
// Define a new permission set for an object.
//*****************************************************************************
HRESULT RegMeta::_DefinePermissionSet(
    mdToken     tk,                     // [IN] the object to be decorated.
    DWORD       dwAction,               // [IN] CorDeclSecurity.
    void const  *pvPermission,          // [IN] permission blob.
    ULONG       cbPermission,           // [IN] count of bytes of pvPermission.
    mdPermission *ppm)                  // [OUT] returned permission token.
{
#ifdef FEATURE_METADATA_EMIT_ALL
    HRESULT     hr  = S_OK;
    DeclSecurityRec *pDeclSec = NULL;
    RID         iDeclSec;
    short       sAction = static_cast<short>(dwAction); // To match with the type in DeclSecurityRec.
    mdPermission tkPerm = mdTokenNil;   // New permission token.

    _ASSERTE(TypeFromToken(tk) == mdtTypeDef || TypeFromToken(tk) == mdtMethodDef ||
             TypeFromToken(tk) == mdtAssembly);

    // Check for valid Action.
    if (sAction == 0 || sAction > dclMaximumValue)
        IfFailGo(E_INVALIDARG);

    if (CheckDups(MDDupPermission))
    {
        hr = ImportHelper::FindPermission(&(m_pStgdb->m_MiniMd), tk, sAction, &tkPerm);

        if (SUCCEEDED(hr))
        {
            // Set output parameter.
            if (ppm)
                *ppm = tkPerm;
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
        if (ppm)
            *ppm = tkPerm;

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

    IfFailGo(_SetPermissionSetProps(tkPerm, sAction, pvPermission, cbPermission));
    IfFailGo(UpdateENCLog(tkPerm));
ErrExit:

    return hr;
#else //!FEATURE_METADATA_EMIT_ALL
    return E_NOTIMPL;
#endif //!FEATURE_METADATA_EMIT_ALL
} // RegMeta::_DefinePermissionSet



//*****************************************************************************
// Set the RVA of a methoddef
//*****************************************************************************
STDMETHODIMP RegMeta::SetRVA(                 // [IN] S_OK or error.
    mdToken     md,                     // [IN] Member for which to set offset
    ULONG       ulRVA)                  // [IN] The offset#endif
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());
    IfFailGo(_SetRVA(md, ulRVA, UINT32_MAX));    // 0xbaad

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::SetRVA

//*****************************************************************************
// Given a signature, return a token to the user.  If there isn't an existing
// token, create a new record.  This should more appropriately be called
// DefineSignature.
//*****************************************************************************
STDMETHODIMP RegMeta::GetTokenFromSig(        // [IN] S_OK or error.
    PCCOR_SIGNATURE pvSig,              // [IN] Signature to define.
    ULONG       cbSig,                  // [IN] Size of signature data.
    mdSignature *pmsig)                 // [OUT] returned signature token.
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    LOCKWRITE();

    _ASSERTE(pmsig);

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());
    IfFailGo(_GetTokenFromSig(pvSig, cbSig, pmsig));

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::GetTokenFromSig

//*****************************************************************************
// Define and set a ModuleRef record.
//*****************************************************************************
STDMETHODIMP RegMeta::DefineModuleRef(        // S_OK or error.
    LPCWSTR     szName,                 // [IN] DLL name
    mdModuleRef *pmur)                  // [OUT] returned module ref token
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    hr = _DefineModuleRef(szName, pmur);

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::DefineModuleRef

#if !defined(FEATURE_METADATA_EMIT_IN_DEBUGGER)
HRESULT RegMeta::_DefineModuleRef(        // S_OK or error.
    LPCWSTR     szName,                 // [IN] DLL name
    mdModuleRef *pmur)                  // [OUT] returned module ref token
{
    HRESULT     hr = S_OK;
    ModuleRefRec *pModuleRef = 0;       // The ModuleRef record.
    RID         iModuleRef;             // Rid of new ModuleRef record.
    LPUTF8      szUTF8Name;
    UTF8STR((LPCWSTR)szName, szUTF8Name);

    _ASSERTE(szName && pmur);

    // See if the given ModuleRef already exists.  If it exists just return.
    // Else create a new record.
    if (CheckDups(MDDupModuleRef))
    {
        hr = ImportHelper::FindModuleRef(&(m_pStgdb->m_MiniMd), szUTF8Name, pmur);
        if (SUCCEEDED(hr))
        {
            if (IsENCOn())
                IfFailGo(m_pStgdb->m_MiniMd.GetModuleRefRecord(RidFromToken(*pmur), &pModuleRef));
            else
            {
                hr = META_S_DUPLICATE;
                goto ErrExit;
            }
        }
        else if (hr != CLDB_E_RECORD_NOTFOUND)
            IfFailGo(hr);
    }

    if (!pModuleRef)
    {
        // Create new record and set the values.
        IfFailGo(m_pStgdb->m_MiniMd.AddModuleRefRecord(&pModuleRef, &iModuleRef));

        // Set the output parameter.
        *pmur = TokenFromRid(iModuleRef, mdtModuleRef);
    }

    // Save the data.
    IfFailGo(m_pStgdb->m_MiniMd.PutString(TBL_ModuleRef, ModuleRefRec::COL_Name,
                                            pModuleRef, szUTF8Name));
    IfFailGo(UpdateENCLog(*pmur));

ErrExit:

    return hr;
} // RegMeta::_DefineModuleRef
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER

//*****************************************************************************
// Set the parent for the specified MemberRef.
//*****************************************************************************
STDMETHODIMP RegMeta::SetParent(                      // S_OK or error.
    mdMemberRef mr,                     // [IN] Token for the ref to be fixed up.
    mdToken     tk)                     // [IN] The ref parent.
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    MemberRefRec *pMemberRef;

    LOCKWRITE();

    _ASSERTE(TypeFromToken(mr) == mdtMemberRef);
    _ASSERTE(IsNilToken(tk) || TypeFromToken(tk) == mdtTypeRef || TypeFromToken(tk) == mdtTypeDef ||
                TypeFromToken(tk) == mdtModuleRef || TypeFromToken(tk) == mdtMethodDef);

    IfFailGo(m_pStgdb->m_MiniMd.GetMemberRefRecord(RidFromToken(mr), &pMemberRef));

    // If the token is nil set it to m_tdModule.
    tk = IsNilToken(tk) ? m_tdModule : tk;

    // Set the parent.
    IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_MemberRef, MemberRefRec::COL_Class, pMemberRef, tk));

    // Add the updated MemberRef to the hash.
    IfFailGo(m_pStgdb->m_MiniMd.AddMemberRefToHash(mr) );

    IfFailGo(UpdateENCLog(mr));

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::SetParent

//*****************************************************************************
// Define an TypeSpec token given a type description.
//*****************************************************************************
STDMETHODIMP RegMeta::GetTokenFromTypeSpec(   // [IN] S_OK or error.
    PCCOR_SIGNATURE pvSig,              // [IN] Signature to define.
    ULONG       cbSig,                  // [IN] Size of signature data.
    mdTypeSpec *ptypespec)              // [OUT] returned signature token.
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    TypeSpecRec *pTypeSpecRec;
    RID         iRec;

    LOCKWRITE();

    _ASSERTE(ptypespec);

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    if (CheckDups(MDDupTypeSpec))
    {
        hr = ImportHelper::FindTypeSpec(&(m_pStgdb->m_MiniMd), pvSig, cbSig, ptypespec);
        if (SUCCEEDED(hr))
        {
            //@GENERICS: Generalizing from similar code in this file, should we not set
            //  hr = META_S_DUPLICATE;
            // here?
            goto ErrExit;
        }
        else if (hr != CLDB_E_RECORD_NOTFOUND)
            IfFailGo(hr);
    }

    // Create a new record.
    IfFailGo(m_pStgdb->m_MiniMd.AddTypeSpecRecord(&pTypeSpecRec, &iRec));

    // Set output parameter.
    *ptypespec = TokenFromRid(iRec, mdtTypeSpec);

    // Set the signature field
    IfFailGo(m_pStgdb->m_MiniMd.PutBlob(
        TBL_TypeSpec,
        TypeSpecRec::COL_Signature,
        pTypeSpecRec,
        pvSig,
        cbSig));
    IfFailGo(UpdateENCLog(*ptypespec));

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::GetTokenFromTypeSpec

//*****************************************************************************
// This API defines a user literal string to be stored in the MetaData section.
// The token for this string has embedded in it the offset into the BLOB pool
// where the string is stored in UNICODE format.  An additional byte is padded
// at the end to indicate whether the string has any characters that are >= 0x80.
//*****************************************************************************
STDMETHODIMP RegMeta::DefineUserString(       // S_OK or error.
    LPCWSTR     szString,               // [IN] User literal string.
    ULONG       cchString,              // [IN] Length of string.
    mdString    *pstk)                  // [OUT] String token.
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    UINT32      nIndex;                 // Index into the user string heap.
    CQuickBytes qb;                     // For storing the string with the byte prefix.
    ULONG       ulMemSize;              // Size of memory taken by the string passed in.
    PBYTE       pb;                     // Pointer into memory allocated by qb.

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    _ASSERTE(pstk && szString && cchString != UINT32_MAX);


    // Copy over the string to memory.
    ulMemSize = cchString * sizeof(WCHAR);
    IfFailGo(qb.ReSizeNoThrow(ulMemSize + 1));
    pb = reinterpret_cast<PBYTE>(qb.Ptr());
    memcpy(pb, szString, ulMemSize);
    SwapStringLength((WCHAR *) pb, cchString);
    // Always set the last byte of memory to indicate that there may be a 80+ or special character.
    // This byte is not used by the runtime.
    *(pb + ulMemSize) = 1;

    IfFailGo(m_pStgdb->m_MiniMd.PutUserString(
        MetaData::DataBlob(pb, ulMemSize + 1),
        &nIndex));

    // Fail if the offset requires the high byte which is reserved for the token ID.
    if (nIndex & 0xff000000)
        IfFailGo(META_E_STRINGSPACE_FULL);
    else
        *pstk = TokenFromRid(nIndex, mdtString);

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::DefineUserString

//*****************************************************************************
// Delete a token.
// We only allow deleting a subset of tokens at this moment. These are TypeDef,
//  MethodDef, FieldDef, Event, Property, and CustomAttribute. Except
//  CustomAttribute, all the other tokens are named. We reserved a special
//  name COR_DELETED_NAME_A to indicating a named record is deleted when
//  xxRTSpecialName is set.
//*****************************************************************************

STDMETHODIMP RegMeta::DeleteToken(
    mdToken tkObj)  // [IN] The token to be deleted
{
#ifdef FEATURE_METADATA_EMIT_ALL
    HRESULT hr = NOERROR;

    LOCKWRITE();

    if (!IsValidToken(tkObj))
        IfFailGo( E_INVALIDARG );

    // make sure that MetaData scope is opened for incremental compilation
    if (!m_pStgdb->m_MiniMd.HasDelete())
    {
        _ASSERTE( !"You cannot call delete token when you did not open the scope with proper Update flags in the SetOption!");
        IfFailGo( E_INVALIDARG );
    }

    _ASSERTE(!m_bSaveOptimized && "Cannot delete records after PreSave() and before Save().");

    switch ( TypeFromToken(tkObj) )
    {
    case mdtTypeDef:
        {
            TypeDefRec      *pRecord;
            IfFailGo(m_pStgdb->m_MiniMd.GetTypeDefRecord(RidFromToken(tkObj), &pRecord));
            IfFailGo(m_pStgdb->m_MiniMd.PutString(TBL_TypeDef, TypeDefRec::COL_Name, pRecord, COR_DELETED_NAME_A));
            pRecord->AddFlags(tdSpecialName | tdRTSpecialName);
            break;
        }
    case mdtMethodDef:
        {
            MethodRec      *pRecord;
            IfFailGo(m_pStgdb->m_MiniMd.GetMethodRecord(RidFromToken(tkObj), &pRecord));
            IfFailGo(m_pStgdb->m_MiniMd.PutString(TBL_Method, MethodRec::COL_Name, pRecord, COR_DELETED_NAME_A));
            pRecord->AddFlags(mdSpecialName | mdRTSpecialName);
            break;
        }
    case mdtFieldDef:
        {
            FieldRec      *pRecord;
            IfFailGo(m_pStgdb->m_MiniMd.GetFieldRecord(RidFromToken(tkObj), &pRecord));
            IfFailGo(m_pStgdb->m_MiniMd.PutString(TBL_Field, FieldRec::COL_Name, pRecord, COR_DELETED_NAME_A));
            pRecord->AddFlags(fdSpecialName | fdRTSpecialName);
            break;
        }
    case mdtEvent:
        {
            EventRec      *pRecord;
            IfFailGo(m_pStgdb->m_MiniMd.GetEventRecord(RidFromToken(tkObj), &pRecord));
            IfFailGo(m_pStgdb->m_MiniMd.PutString(TBL_Event, EventRec::COL_Name, pRecord, COR_DELETED_NAME_A));
            pRecord->AddEventFlags(evSpecialName | evRTSpecialName);
            break;
        }
    case mdtProperty:
        {
            PropertyRec      *pRecord;
            IfFailGo(m_pStgdb->m_MiniMd.GetPropertyRecord(RidFromToken(tkObj), &pRecord));
            IfFailGo(m_pStgdb->m_MiniMd.PutString(TBL_Property, PropertyRec::COL_Name, pRecord, COR_DELETED_NAME_A));
            pRecord->AddPropFlags(prSpecialName | prRTSpecialName);
            break;
        }
    case mdtExportedType:
        {
            ExportedTypeRec      *pRecord;
            IfFailGo(m_pStgdb->m_MiniMd.GetExportedTypeRecord(RidFromToken(tkObj), &pRecord));
            IfFailGo(m_pStgdb->m_MiniMd.PutString(TBL_ExportedType, ExportedTypeRec::COL_TypeName, pRecord, COR_DELETED_NAME_A));
            break;
        }
    case mdtCustomAttribute:
        {
            mdToken         tkParent;
            CustomAttributeRec  *pRecord;
            IfFailGo(m_pStgdb->m_MiniMd.GetCustomAttributeRecord(RidFromToken(tkObj), &pRecord));

            // replace the parent column of the custom value record to a nil token.
            tkParent = m_pStgdb->m_MiniMd.getParentOfCustomAttribute(pRecord);
            tkParent = TokenFromRid( mdTokenNil, TypeFromToken(tkParent) );
            IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_CustomAttribute, CustomAttributeRec::COL_Parent, pRecord, tkParent));

            // now the CustomAttribute table is no longer sorted
            m_pStgdb->m_MiniMd.SetSorted(TBL_CustomAttribute, false);
            break;
        }
    case mdtGenericParam:
        {
            mdToken         tkParent;
            GenericParamRec  *pRecord;
            IfFailGo(m_pStgdb->m_MiniMd.GetGenericParamRecord(RidFromToken(tkObj), &pRecord));

            // replace the Parent column of the GenericParam record with a nil token.
            tkParent = m_pStgdb->m_MiniMd.getOwnerOfGenericParam(pRecord);
            tkParent = TokenFromRid( mdTokenNil, TypeFromToken(tkParent) );
            IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_GenericParam, GenericParamRec::COL_Owner,
                                                 pRecord, tkParent));

            // now the GenericParam table is no longer sorted
            m_pStgdb->m_MiniMd.SetSorted(TBL_GenericParam, false);
            break;
        }
    case mdtGenericParamConstraint:
        {
            GenericParamConstraintRec  *pRecord;
            IfFailGo(m_pStgdb->m_MiniMd.GetGenericParamConstraintRecord(RidFromToken(tkObj), &pRecord));

            // replace the Param column of the GenericParamConstraint record with zero RID.
            IfFailGo(m_pStgdb->m_MiniMd.PutCol(TBL_GenericParamConstraint,
                                               GenericParamConstraintRec::COL_Owner,pRecord, 0));
            // now the GenericParamConstraint table is no longer sorted
            m_pStgdb->m_MiniMd.SetSorted(TBL_GenericParamConstraint, false);
            break;
        }
    case mdtPermission:
        {
            mdToken         tkParent;
            mdToken         tkNil;
            DeclSecurityRec *pRecord;
            IfFailGo(m_pStgdb->m_MiniMd.GetDeclSecurityRecord(RidFromToken(tkObj), &pRecord));

            // Replace the parent column of the permission record with a nil tokne.
            tkParent = m_pStgdb->m_MiniMd.getParentOfDeclSecurity(pRecord);
            tkNil = TokenFromRid( mdTokenNil, TypeFromToken(tkParent) );
            IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_DeclSecurity, DeclSecurityRec::COL_Parent, pRecord, tkNil ));

            // The table is no longer sorted.
            m_pStgdb->m_MiniMd.SetSorted(TBL_DeclSecurity, false);

            // If the parent has no more security attributes, turn off the "has security" bit.
            HCORENUM        hEnum = 0;
            mdPermission    rPerms[1];
            ULONG           cPerms = 0;
            EnumPermissionSets(&hEnum, tkParent, 0 /* all actions */, rPerms, 1, &cPerms);
            CloseEnum(hEnum);
            if (cPerms == 0)
            {
                void    *pRow;
                ULONG   ixTbl;
                // Get the row for the parent object.
                ixTbl = m_pStgdb->m_MiniMd.GetTblForToken(tkParent);
                _ASSERTE(ixTbl >= 0 && ixTbl <= m_pStgdb->m_MiniMd.GetCountTables());
                IfFailGo(m_pStgdb->m_MiniMd.getRow(ixTbl, RidFromToken(tkParent), &pRow));

                switch (TypeFromToken(tkParent))
                {
                case mdtTypeDef:
                    reinterpret_cast<TypeDefRec*>(pRow)->RemoveFlags(tdHasSecurity);
                    break;
                case mdtMethodDef:
                    reinterpret_cast<MethodRec*>(pRow)->RemoveFlags(mdHasSecurity);
                    break;
                case mdtAssembly:
                    // No security bit.
                    break;
                }
            }
            break;
        }
    default:
        _ASSERTE(!"Bad token type!");
        IfFailGo( E_INVALIDARG );
        break;
    }

ErrExit:
    return hr;
#else //!FEATURE_METADATA_EMIT_ALL
    return E_NOTIMPL;
#endif //!FEATURE_METADATA_EMIT_ALL
} // RegMeta::DeleteToken

//*****************************************************************************
// Set the properties on the given TypeDef token.
//*****************************************************************************
STDMETHODIMP RegMeta::SetTypeDefProps(        // S_OK or error.
    mdTypeDef   td,                     // [IN] The TypeDef.
    DWORD       dwTypeDefFlags,         // [IN] TypeDef flags.
    mdToken     tkExtends,              // [IN] Base TypeDef or TypeRef.
    mdToken     rtkImplements[])        // [IN] Implemented interfaces.
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    hr = _SetTypeDefProps(td, dwTypeDefFlags, tkExtends, rtkImplements);

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::SetTypeDefProps


//*****************************************************************************
// Define a Nested Type.
//*****************************************************************************
STDMETHODIMP RegMeta::DefineNestedType(       // S_OK or error.
    LPCWSTR     szTypeDef,              // [IN] Name of TypeDef
    DWORD       dwTypeDefFlags,         // [IN] CustomAttribute flags
    mdToken     tkExtends,              // [IN] extends this TypeDef or typeref
    mdToken     rtkImplements[],        // [IN] Implements interfaces
    mdTypeDef   tdEncloser,             // [IN] TypeDef token of the enclosing type.
    mdTypeDef   *ptd)                   // [OUT] Put TypeDef token here
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    _ASSERTE(TypeFromToken(tdEncloser) == mdtTypeDef && !IsNilToken(tdEncloser));
    _ASSERTE(IsTdNested(dwTypeDefFlags));

    IfFailGo(_DefineTypeDef(szTypeDef, dwTypeDefFlags,
                tkExtends, rtkImplements, tdEncloser, ptd));

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::DefineNestedType

//*****************************************************************************
// Define a formal type parameter for the given TypeDef or MethodDef token.
//*****************************************************************************
STDMETHODIMP RegMeta::DefineGenericParam(   // S_OK or error.
        mdToken      tkOwner,               // [IN] TypeDef or MethodDef
        ULONG        ulParamSeq,            // [IN] Index of the type parameter
        DWORD        dwParamFlags,          // [IN] Flags, for future use (e.g. variance)
        LPCWSTR      szName,                // [IN] Name
        DWORD        reserved,              // [IN] For future use
        mdToken      rtkConstraints[],      // [IN] Array of type constraints (TypeDef,TypeRef,TypeSpec)
        mdGenericParam *pgp)                // [OUT] Put GenericParam token here
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    mdToken     tkRet = mdGenericParamNil;
    mdToken tkOwnerType = TypeFromToken(tkOwner);

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    if (reserved != 0)
        IfFailGo(META_E_BAD_INPUT_PARAMETER);

    // See if this version of the metadata can do Generics
    if (!m_pStgdb->m_MiniMd.SupportsGenerics())
        IfFailGo(CLDB_E_INCOMPATIBLE);

    if ((tkOwnerType == mdtTypeDef) || (tkOwnerType == mdtMethodDef))
    {
        // 1. Find/create GP (unique tkOwner+ulParamSeq)  = tkRet
        GenericParamRec *pGenericParam = NULL;
        RID         iGenericParam,rid;
        RID         ridStart;
        RID         ridEnd;

        // See if this GenericParam has already been defined.
        if (CheckDups(MDDupGenericParam))
        {
            // Enumerate any GenericParams for the parent, looking for this sequence number.
            IfFailGo(m_pStgdb->m_MiniMd.GetGenericParamsForToken(tkOwner, &ridStart, &ridEnd));
            for (rid = ridStart; rid < ridEnd; rid++)
            {
                iGenericParam = m_pStgdb->m_MiniMd.GetGenericParamRid(rid);
                IfFailGo(m_pStgdb->m_MiniMd.GetGenericParamRecord(iGenericParam, &pGenericParam));
                // Is this the desired GenericParam #?
                if (pGenericParam->GetNumber() == (USHORT)ulParamSeq)
                {
                    tkRet = TokenFromRid(iGenericParam,mdtGenericParam);
                    // This is a duplicate.  If not ENC, just return 'DUPLICATE'.  If ENC, overwrite.
                    if (!IsENCOn())
                    {
                        IfFailGo(META_S_DUPLICATE);
                    }
                    break;
                }
            }
        }
        else
        {   // Clear rid, ridStart, ridEnd, so we no we didn't find one.
            rid = ridStart = ridEnd = 0;
        }

        // If none was found, create one.
        if(rid >= ridEnd)
        {
            IfFailGo(m_pStgdb->m_MiniMd.AddGenericParamRecord(&pGenericParam, &iGenericParam));
            pGenericParam->SetNumber((USHORT)ulParamSeq);
            IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_GenericParam, GenericParamRec::COL_Owner,
                                                pGenericParam, tkOwner));
            tkRet = TokenFromRid(iGenericParam,mdtGenericParam);
        }

        // 2. Set its props
        IfFailGo(_SetGenericParamProps(tkRet, pGenericParam, dwParamFlags, szName, reserved ,rtkConstraints));
        IfFailGo(UpdateENCLog(tkRet));
    }
    else
        hr =  META_E_BAD_INPUT_PARAMETER;

ErrExit:

    if(pgp != NULL)
        *pgp = tkRet;

    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::DefineGenericParam

//*****************************************************************************
// Set props of a formal type parameter.
//*****************************************************************************
STDMETHODIMP RegMeta::SetGenericParamProps(      // S_OK or error.
        mdGenericParam gp,                  // [IN] GenericParam
        DWORD        dwParamFlags,          // [IN] Flags, for future use (e.g. variance)
        LPCWSTR      szName,                // [IN] Optional name
        DWORD        reserved,              // [IN] For future use (e.g. non-type parameters)
        mdToken      rtkConstraints[])     // [IN] Array of type constraints (TypeDef,TypeRef,TypeSpec)
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    if (reserved != 0)
        IfFailGo(META_E_BAD_INPUT_PARAMETER);

    // See if this version of the metadata can do Generics
    if (!m_pStgdb->m_MiniMd.SupportsGenerics())
        IfFailGo(CLDB_E_INCOMPATIBLE);

    if (TypeFromToken(gp) == mdtGenericParam)
    {
        GenericParamRec *pGenericParam;

        IfFailGo(m_pStgdb->m_MiniMd.GetGenericParamRecord(RidFromToken(gp), &pGenericParam));
        IfFailGo(_SetGenericParamProps(gp,pGenericParam,dwParamFlags,szName,reserved,rtkConstraints));
        IfFailGo(UpdateENCLog(gp));
    }
    else
        hr =  META_E_BAD_INPUT_PARAMETER;

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::SetGenericParamProps

//*****************************************************************************
// Set props of a formal type parameter (internal).
//*****************************************************************************
HRESULT RegMeta::_SetGenericParamProps(     // S_OK or error.
        mdGenericParam  tkGP,               // [IN] Formal parameter token
        GenericParamRec *pGenericParam,     // [IN] GenericParam record ptr
        DWORD        dwParamFlags,          // [IN] Flags, for future use (e.g. variance)
        LPCWSTR      szName,                // [IN] Optional name
        DWORD        reserved,              // [IN] For future use (e.g. non-type parameters)
        mdToken      rtkConstraints[])      // [IN] Array of type constraints (TypeDef,TypeRef,TypeSpec)
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    if (pGenericParam != NULL)
    {
        // If there is a name, set it.
        if ((szName != NULL) && (*szName != 0))
            IfFailGo(m_pStgdb->m_MiniMd.PutStringW(TBL_GenericParam, GenericParamRec::COL_Name,
                                                pGenericParam, szName));

        // If there are new flags, set them.
        if (dwParamFlags != (DWORD) -1)
            pGenericParam->SetFlags((USHORT)dwParamFlags);

        // If there is a new array of constraints, apply it.
        if (rtkConstraints != NULL)
        {
            //Clear existing constraints
            GenericParamConstraintRec* pGPCRec;
            RID     ridGPC;
            RID     rid;
            RID     ridStart;
            RID     ridEnd;

            IfFailGo(m_pStgdb->m_MiniMd.GetGenericParamConstraintsForToken(tkGP, &ridStart, &ridEnd));
            for (rid = ridStart; rid < ridEnd; rid++)
            {
                ridGPC = m_pStgdb->m_MiniMd.GetGenericParamConstraintRid(rid);
                IfFailGo(m_pStgdb->m_MiniMd.GetGenericParamConstraintRecord(ridGPC, &pGPCRec));
                IfFailGo(m_pStgdb->m_MiniMd.PutCol(TBL_GenericParamConstraint,
                                                    GenericParamConstraintRec::COL_Owner,
                                                    pGPCRec, 0));
                IfFailGo(UpdateENCLog(TokenFromRid(ridGPC,mdtGenericParamConstraint)));
            }

            //Emit new constraints
            mdToken* ptk;
            for (ptk = rtkConstraints; (ptk != NULL)&&(RidFromToken(*ptk)!=0); ptk++)
            {
                IfFailGo(m_pStgdb->m_MiniMd.AddGenericParamConstraintRecord(&pGPCRec, &ridGPC));
                IfFailGo(m_pStgdb->m_MiniMd.PutCol(TBL_GenericParamConstraint,
                                                     GenericParamConstraintRec::COL_Owner,
                                                    pGPCRec, RidFromToken(tkGP)));
                IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_GenericParamConstraint,
                                                     GenericParamConstraintRec::COL_Constraint,
                                                    pGPCRec, *ptk));
                IfFailGo(UpdateENCLog(TokenFromRid(ridGPC,mdtGenericParamConstraint)));
            }
        }
    }
    else
        hr =  META_E_BAD_INPUT_PARAMETER;

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::_SetGenericParamProps

#ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB
//*****************************************************************************
// Get referenced type system metadata tables.
//*****************************************************************************
STDMETHODIMP RegMeta::GetReferencedTypeSysTables(   // S_OK or error.
    ULONG64       *refTables,                       // [OUT] Bit vector of referenced type system metadata tables.
    ULONG         refTableRows[],                   // [OUT] Array of number of rows for each referenced type system table.
    const ULONG   maxTableRowsSize,                 // [IN]  Max size of the rows array.
    ULONG         *tableRowsSize)                   // [OUT] Size of the rows array.
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    ULONG64 refTablesBitVector = 0;
    ULONG count = 0;
    ULONG* ptr = NULL;
    ULONG rowsSize = 0;

    for (ULONG i = 0; i < TBL_COUNT; i++)
    {
        if (m_pStgdb->m_MiniMd.m_Tables[i].GetRecordCount() > 0)
        {
            refTablesBitVector |= (ULONG64)1UL << i;
            count++;
        }
    }

    _ASSERTE(count <= maxTableRowsSize);
    if (count > maxTableRowsSize)
    {
        hr = META_E_BADMETADATA;
        goto ErrExit;
    }

    *refTables = refTablesBitVector;
    *tableRowsSize = count;

    ptr = refTableRows;
    for (ULONG i = 0; i < TBL_COUNT; i++)
    {
        rowsSize = m_pStgdb->m_MiniMd.m_Tables[i].GetRecordCount();
        if (rowsSize > 0)
            *ptr++ = rowsSize;
    }

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::GetReferencedTypeSysTables


//*****************************************************************************
// Defines PDB stream data for portable PDB metadata
//*****************************************************************************
STDMETHODIMP RegMeta::DefinePdbStream(      // S_OK or error.
    PORT_PDB_STREAM* pdbStream)             // [IN] Portable pdb stream data.
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    IfFailGo(m_pStgdb->m_pPdbHeap->SetData(pdbStream));

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::DefinePdbStream

//*****************************************************************************
// Defines a document for portable PDB metadata
//*****************************************************************************
STDMETHODIMP RegMeta::DefineDocument(       // S_OK or error.
    char    *docName,                       // [IN] Document name (string will be tokenized).
    GUID    *hashAlg,                       // [IN] Hash algorithm GUID.
    BYTE    *hashVal,                       // [IN] Hash value.
    ULONG   hashValSize,                    // [IN] Hash value size.
    GUID    *lang,                          // [IN] Language GUID.
    mdDocument  *docMdToken)                // [OUT] Token of the defined document.
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;
    char delim[2] = "";
    ULONG docNameBlobSize = 0;
    ULONG docNameBlobMaxSize = 0;
    BYTE* docNameBlob = NULL;
    BYTE* docNameBlobPtr = NULL;
    ULONG partsCount = 0;
    ULONG partsIndexesCount = 0;
    UINT32* partsIndexes = NULL;
    UINT32* partsIndexesPtr = NULL;
    char* stringToken = NULL;

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    // determine separator and number of separated parts
    GetPathSeparator(docName, delim, &partsCount);
    delim[1] = '\0';

    // allocate the maximum size of a document blob
    // treating each compressed index to take maximum of 4 bytes.
    // the actual size will be calculated once we compress each index.
    docNameBlobMaxSize = sizeof(char) + sizeof(ULONG) * partsCount; // (delim + 4 * partsCount)
    docNameBlob = new BYTE[docNameBlobMaxSize];
    partsIndexes = new UINT32[partsCount];

    // add path parts to blob heap and store their indexes
    partsIndexesPtr = partsIndexes;
    if (*delim == *docName)
    {
        // if the path starts with the delimiter (e.g. /home/user/...) store an empty string
        *partsIndexesPtr++ = 0;
        partsIndexesCount++;
    }
    stringToken = strtok(docName, (const char*)delim);
    while (stringToken != NULL)
    {
        IfFailGo(m_pStgdb->m_MiniMd.m_BlobHeap.AddBlob(MetaData::DataBlob((BYTE*)stringToken, (ULONG)strlen(stringToken)), partsIndexesPtr++));
        stringToken = strtok(NULL, (const char*)delim);
        partsIndexesCount++;
    }

    _ASSERTE(partsIndexesCount == partsCount);

    // build up the documentBlob ::= separator part+
    docNameBlobPtr = docNameBlob;
    // put separator
    *docNameBlobPtr = delim[0];
    docNameBlobPtr++;
    docNameBlobSize++;
    // put part+: compress and put each part index
    for (ULONG i = 0; i < partsCount; i++)
    {
        ULONG cnt = CorSigCompressData(partsIndexes[i], docNameBlobPtr);
        docNameBlobPtr += cnt;
        docNameBlobSize += cnt;
    }

    _ASSERTE(docNameBlobSize <= docNameBlobMaxSize);

    // Add record
    RID docRecord;
    DocumentRec* pDocument;
    IfFailGo(m_pStgdb->m_MiniMd.AddDocumentRecord(&pDocument, &docRecord));
    // Name column
    IfFailGo(m_pStgdb->m_MiniMd.PutBlob(TBL_Document, DocumentRec::COL_Name, pDocument, docNameBlob, docNameBlobSize));
    // HashAlgorithm column
    IfFailGo(m_pStgdb->m_MiniMd.PutGuid(TBL_Document, DocumentRec::COL_HashAlgorithm, pDocument, *hashAlg));
    // HashValue column
    IfFailGo(m_pStgdb->m_MiniMd.PutBlob(TBL_Document, DocumentRec::COL_Hash, pDocument, hashVal, hashValSize));
    // Language column
    IfFailGo(m_pStgdb->m_MiniMd.PutGuid(TBL_Document, DocumentRec::COL_Language, pDocument, *lang));

    *docMdToken = TokenFromRid(docRecord, mdtDocument);

ErrExit:
    if (docNameBlob != NULL)
        delete[] docNameBlob;

    if (partsIndexes != NULL)
        delete[] partsIndexes;

    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::DefineDocument

//*****************************************************************************
// Defines sequence points for portable PDB metadata
//*****************************************************************************
STDMETHODIMP RegMeta::DefineSequencePoints(     // S_OK or error.
    ULONG       docRid,                         // [IN] Document RID.
    BYTE        *sequencePtsBlob,               // [IN] Sequence point blob.
    ULONG       sequencePtsBlobSize)            // [IN] Sequence point blob size.
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    RID methodDbgInfoRec;
    MethodDebugInformationRec* pMethodDbgInfo;
    IfFailGo(m_pStgdb->m_MiniMd.AddMethodDebugInformationRecord(&pMethodDbgInfo, &methodDbgInfoRec));
    // Document column
    IfFailGo(m_pStgdb->m_MiniMd.PutCol(TBL_MethodDebugInformation,
        MethodDebugInformationRec::COL_Document, pMethodDbgInfo, docRid));
    // Sequence points column
    IfFailGo(m_pStgdb->m_MiniMd.PutBlob(TBL_MethodDebugInformation,
        MethodDebugInformationRec::COL_SequencePoints, pMethodDbgInfo, sequencePtsBlob, sequencePtsBlobSize));

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::DefineSequencePoints

//*****************************************************************************
// Defines a local scope for portable PDB metadata
//*****************************************************************************
STDMETHODIMP RegMeta::DefineLocalScope(     // S_OK or error.
    ULONG       methodDefRid,               // [IN] Method RID.
    ULONG       importScopeRid,             // [IN] Import scope RID.
    ULONG       firstLocalVarRid,           // [IN] First local variable RID (of the continous run).
    ULONG       firstLocalConstRid,         // [IN] First local constant RID (of the continous run).
    ULONG       startOffset,                // [IN] Start offset of the scope.
    ULONG       length)                     // [IN] Scope length.
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    RID localScopeRecord;
    LocalScopeRec* pLocalScope;
    IfFailGo(m_pStgdb->m_MiniMd.AddLocalScopeRecord(&pLocalScope, &localScopeRecord));
    IfFailGo(m_pStgdb->m_MiniMd.PutCol(TBL_LocalScope, LocalScopeRec::COL_Method, pLocalScope, methodDefRid));
    IfFailGo(m_pStgdb->m_MiniMd.PutCol(TBL_LocalScope, LocalScopeRec::COL_ImportScope, pLocalScope, importScopeRid));
    IfFailGo(m_pStgdb->m_MiniMd.PutCol(TBL_LocalScope, LocalScopeRec::COL_VariableList, pLocalScope, firstLocalVarRid));
    IfFailGo(m_pStgdb->m_MiniMd.PutCol(TBL_LocalScope, LocalScopeRec::COL_ConstantList, pLocalScope, firstLocalConstRid));
    IfFailGo(m_pStgdb->m_MiniMd.PutCol(TBL_LocalScope, LocalScopeRec::COL_StartOffset, pLocalScope, startOffset));
    IfFailGo(m_pStgdb->m_MiniMd.PutCol(TBL_LocalScope, LocalScopeRec::COL_Length, pLocalScope, length));

    // TODO: Force set sorted tables flag, do this properly
    m_pStgdb->m_MiniMd.SetSorted(TBL_LocalScope, true);

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::DefineLocalScope

//*****************************************************************************
// Defines a local variable for portable PDB metadata
//*****************************************************************************
STDMETHODIMP RegMeta::DefineLocalVariable(      // S_OK or error.
    USHORT      attribute,                      // [IN] Variable attribute.
    USHORT      index,                          // [IN] Variable index (slot).
    char        *name,                          // [IN] Variable name.
    mdLocalVariable* locVarToken)               // [OUT] Token of the defined variable.
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    RID localVariableRecord;
    LocalVariableRec* pLocalVariable;
    IfFailGo(m_pStgdb->m_MiniMd.AddLocalVariableRecord(&pLocalVariable, &localVariableRecord));
    IfFailGo(m_pStgdb->m_MiniMd.PutString(TBL_LocalVariable, LocalVariableRec::COL_Name, pLocalVariable, name));

    pLocalVariable->SetAttributes(attribute);
    pLocalVariable->SetIndex(index);

    *locVarToken = TokenFromRid(localVariableRecord, mdtLocalVariable);

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::DefineLocalVariable
#endif // FEATURE_METADATA_EMIT_PORTABLE_PDB

//*****************************************************************************
// Create and set a MethodSpec record.
//*****************************************************************************
STDMETHODIMP RegMeta::DefineMethodSpec( // S_OK or error
    mdToken     tkImport,               // [IN] MethodDef or MemberRef
    PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of COM+ signature
    ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
    mdMethodSpec *pmi)                  // [OUT] method instantiation token
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    MethodSpecRec   *pRecord = 0;       // The MethodSpec record.
    RID             iRecord;            // RID of new MethodSpec record.

    LOCKWRITE();

    // See if this version of the metadata can do Generics
    if (!m_pStgdb->m_MiniMd.SupportsGenerics())
        IfFailGo(CLDB_E_INCOMPATIBLE);

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    // Check that it is a method, or at least memberref.
    if ((TypeFromToken(tkImport) != mdtMethodDef) && (TypeFromToken(tkImport) != mdtMemberRef))
        IfFailGo(META_E_BAD_INPUT_PARAMETER);

    // Must have a signature, and someplace to return the token.
    if ((pvSigBlob == NULL) || (cbSigBlob == 0) || (pmi == NULL))
        IfFailGo(META_E_BAD_INPUT_PARAMETER);

    // If the MethodSpec already exists, just return the token, else
    // create a new record.
    if (CheckDups(MDDupMethodSpec))
    {
        hr = ImportHelper::FindMethodSpecByMethodAndInstantiation(&(m_pStgdb->m_MiniMd), tkImport,pvSigBlob, cbSigBlob, pmi);
        if (SUCCEEDED(hr))
        {
            if (IsENCOn()) //GENERICS: is this correct? Do we really want to support ENC of MethodSpecs?
                IfFailGo(m_pStgdb->m_MiniMd.GetMethodSpecRecord(RidFromToken(*pmi), &pRecord));
            else
            {
                hr = META_S_DUPLICATE;
                goto ErrExit;
            }
        }
        else if (hr != CLDB_E_RECORD_NOTFOUND)      // MemberRef exists
            IfFailGo(hr);
    }


    if (!pRecord)
    {   // Create the record.
        IfFailGo(m_pStgdb->m_MiniMd.AddMethodSpecRecord(&pRecord, &iRecord));

    /*GENERICS: do we need to do anything like this?
      Probably not, since SetMemberDefDirty is for ref to def optimization, and there are no method spec "refs".
        // record the more defs are introduced.
        SetMemberDefDirty(true);
    */

        // Give token to caller.
        *pmi = TokenFromRid(iRecord, mdtMethodSpec);
    }

    // Save row data.
    IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_MethodSpec, MethodSpecRec::COL_Method, pRecord, tkImport));
    IfFailGo(m_pStgdb->m_MiniMd.PutBlob(TBL_MethodSpec, MethodSpecRec::COL_Instantiation, pRecord,
                                pvSigBlob, cbSigBlob));
    /*@GENERICS: todo: update MethodSpec hash table  */
    /* IfFailGo(m_pStgdb->m_MiniMd.AddMemberRefToHash(*pmi) ); */

    IfFailGo(UpdateENCLog(*pmi));

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::DefineMethodSpec

//*****************************************************************************
// Set the properties on the given Method token.
//*****************************************************************************
STDMETHODIMP RegMeta::SetMethodProps(         // S_OK or error.
    mdMethodDef md,                     // [IN] The MethodDef.
    DWORD       dwMethodFlags,          // [IN] Method attributes.
    ULONG       ulCodeRVA,              // [IN] Code RVA.
    DWORD       dwImplFlags)            // [IN] MethodImpl flags.
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    LOCKWRITE();

    if (dwMethodFlags != UINT32_MAX)
    {
        // Make sure no one sets the reserved bits on the way in.
        _ASSERTE((dwMethodFlags & (mdReservedMask&~mdRTSpecialName)) == 0);
        dwMethodFlags &= (~mdReservedMask);
    }

    hr = _SetMethodProps(md, dwMethodFlags, ulCodeRVA, dwImplFlags);

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::SetMethodProps

//*****************************************************************************
// Set the properties on the given Event token.
//*****************************************************************************
STDMETHODIMP RegMeta::SetEventProps(    // S_OK or error.
    mdEvent     ev,                     // [IN] The event token.
    DWORD       dwEventFlags,           // [IN] CorEventAttr.
    mdToken     tkEventType,            // [IN] A reference (mdTypeRef or mdTypeRef) to the Event class.
    mdMethodDef mdAddOn,                // [IN] Add method.
    mdMethodDef mdRemoveOn,             // [IN] Remove method.
    mdMethodDef mdFire,                 // [IN] Fire method.
    mdMethodDef rmdOtherMethods[])      // [IN] Array of other methods associate with the event.
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    IfFailGo(_SetEventProps1(ev, dwEventFlags, tkEventType));
    IfFailGo(_SetEventProps2(ev, mdAddOn, mdRemoveOn, mdFire, rmdOtherMethods, true));

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::SetEventProps

//*****************************************************************************
// Set the properties on the given Permission token.
//*****************************************************************************
STDMETHODIMP RegMeta::SetPermissionSetProps(  // S_OK or error.
    mdToken     tk,                     // [IN] The object to be decorated.
    DWORD       dwAction,               // [IN] CorDeclSecurity.
    void const  *pvPermission,          // [IN] Permission blob.
    ULONG       cbPermission,           // [IN] Count of bytes of pvPermission.
    mdPermission *ppm)                  // [OUT] Permission token.
{
#ifdef FEATURE_METADATA_EMIT_ALL
    HRESULT hr = S_OK;

    USHORT      sAction = static_cast<USHORT>(dwAction);    // Corresponding DeclSec field is a USHORT.
    mdPermission tkPerm;

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    _ASSERTE(TypeFromToken(tk) == mdtTypeDef || TypeFromToken(tk) == mdtMethodDef ||
             TypeFromToken(tk) == mdtAssembly);

    // Check for valid Action.
    if (dwAction == UINT32_MAX || dwAction == 0 || dwAction > dclMaximumValue)
        IfFailGo(E_INVALIDARG);

    IfFailGo(ImportHelper::FindPermission(&(m_pStgdb->m_MiniMd), tk, sAction, &tkPerm));
    if (ppm)
        *ppm = tkPerm;
    IfFailGo(_SetPermissionSetProps(tkPerm, dwAction, pvPermission, cbPermission));
ErrExit:
    return hr;
#else //!FEATURE_METADATA_EMIT_ALL
    return E_NOTIMPL;
#endif //!FEATURE_METADATA_EMIT_ALL
} // RegMeta::SetPermissionSetProps

//*****************************************************************************
// This routine sets the p-invoke information for the specified Field or Method.
//*****************************************************************************
STDMETHODIMP RegMeta::DefinePinvokeMap(       // Return code.
    mdToken     tk,                     // [IN] FieldDef or MethodDef.
    DWORD       dwMappingFlags,         // [IN] Flags used for mapping.
    LPCWSTR     szImportName,           // [IN] Import name.
    mdModuleRef mrImportDLL)            // [IN] ModuleRef token for the target DLL.
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    hr = _DefinePinvokeMap(tk, dwMappingFlags, szImportName, mrImportDLL);

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::DefinePinvokeMap

//*****************************************************************************
// Internal worker function for setting p-invoke info.
//*****************************************************************************
HRESULT RegMeta::_DefinePinvokeMap(     // Return hresult.
    mdToken     tk,                     // [IN] FieldDef or MethodDef.
    DWORD       dwMappingFlags,         // [IN] Flags used for mapping.
    LPCWSTR     szImportName,           // [IN] Import name.
    mdModuleRef mrImportDLL)            // [IN] ModuleRef token for the target DLL.
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    ImplMapRec  *pRecord;
    RID         iRecord = 0;
    bool        bDupFound = false;
    HRESULT     hr = S_OK;

    _ASSERTE(TypeFromToken(tk) == mdtFieldDef || TypeFromToken(tk) == mdtMethodDef);
    _ASSERTE(TypeFromToken(mrImportDLL) == mdtModuleRef);
    _ASSERTE(RidFromToken(tk) && RidFromToken(mrImportDLL) && szImportName);

    // Turn on the quick lookup flag.
    if (TypeFromToken(tk) == mdtMethodDef)
    {
        if (CheckDups(MDDupMethodDef))
        {
            IfFailGo(m_pStgdb->m_MiniMd.FindImplMapHelper(tk, &iRecord));
            if (! InvalidRid(iRecord))
                bDupFound = true;
        }
        MethodRec *pMethod;
        IfFailGo(m_pStgdb->m_MiniMd.GetMethodRecord(RidFromToken(tk), &pMethod));
        pMethod->AddFlags(mdPinvokeImpl);
    }
    else    // TypeFromToken(tk) == mdtFieldDef
    {
        if (CheckDups(MDDupFieldDef))
        {
            IfFailGo(m_pStgdb->m_MiniMd.FindImplMapHelper(tk, &iRecord));
            if (!InvalidRid(iRecord))
                bDupFound = true;
        }
        FieldRec *pField;
        IfFailGo(m_pStgdb->m_MiniMd.GetFieldRecord(RidFromToken(tk), &pField));
        pField->AddFlags(fdPinvokeImpl);
    }

    // Create a new record.
    if (bDupFound)
    {
        if (IsENCOn())
            IfFailGo(m_pStgdb->m_MiniMd.GetImplMapRecord(RidFromToken(iRecord), &pRecord));
        else
        {
            hr = META_S_DUPLICATE;
            goto ErrExit;
        }
    }
    else
    {
        IfFailGo(UpdateENCLog(tk));
        IfFailGo(m_pStgdb->m_MiniMd.AddImplMapRecord(&pRecord, &iRecord));
        IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_ImplMap,
                                             ImplMapRec::COL_MemberForwarded, pRecord, tk));
        IfFailGo( m_pStgdb->m_MiniMd.AddImplMapToHash(iRecord) );

    }

    // If no module, create a dummy, empty module.
    if (IsNilToken(mrImportDLL))
    {
        hr = ImportHelper::FindModuleRef(&m_pStgdb->m_MiniMd, "", &mrImportDLL);
        if (hr == CLDB_E_RECORD_NOTFOUND)
            IfFailGo(_DefineModuleRef(W(""), &mrImportDLL));
    }

    // Set the data.
    if (dwMappingFlags != UINT32_MAX)
        pRecord->SetMappingFlags(static_cast<USHORT>(dwMappingFlags));
    IfFailGo(m_pStgdb->m_MiniMd.PutStringW(TBL_ImplMap, ImplMapRec::COL_ImportName,
                                           pRecord, szImportName));
    IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_ImplMap,
                                         ImplMapRec::COL_ImportScope, pRecord, mrImportDLL));

    IfFailGo(UpdateENCLog2(TBL_ImplMap, iRecord));

ErrExit:

    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::DefinePinvokeMap

//*****************************************************************************
// This routine sets the p-invoke information for the specified Field or Method.
//*****************************************************************************
STDMETHODIMP RegMeta::SetPinvokeMap(          // Return code.
    mdToken     tk,                     // [IN] FieldDef or MethodDef.
    DWORD       dwMappingFlags,         // [IN] Flags used for mapping.
    LPCWSTR     szImportName,           // [IN] Import name.
    mdModuleRef mrImportDLL)            // [IN] ModuleRef token for the target DLL.
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    ImplMapRec  *pRecord;
    RID         iRecord;

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    _ASSERTE(TypeFromToken(tk) == mdtFieldDef || TypeFromToken(tk) == mdtMethodDef);
    _ASSERTE(RidFromToken(tk));

    IfFailGo(m_pStgdb->m_MiniMd.FindImplMapHelper(tk, &iRecord));

    if (InvalidRid(iRecord))
        IfFailGo(CLDB_E_RECORD_NOTFOUND);

    IfFailGo(m_pStgdb->m_MiniMd.GetImplMapRecord(iRecord, &pRecord));

    // Set the data.
    if (dwMappingFlags != UINT32_MAX)
        pRecord->SetMappingFlags(static_cast<USHORT>(dwMappingFlags));
    if (szImportName)
        IfFailGo(m_pStgdb->m_MiniMd.PutStringW(TBL_ImplMap, ImplMapRec::COL_ImportName,
                                               pRecord, szImportName));
    if (! IsNilToken(mrImportDLL))
        IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_ImplMap, ImplMapRec::COL_ImportScope,
                                               pRecord, mrImportDLL));

    IfFailGo(UpdateENCLog2(TBL_ImplMap, iRecord));

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::SetPinvokeMap

//*****************************************************************************
// This routine deletes the p-invoke record for the specified Field or Method.
//*****************************************************************************
STDMETHODIMP RegMeta::DeletePinvokeMap(       // Return code.
    mdToken     tk)                     // [IN]FieldDef or MethodDef.
{
#ifdef FEATURE_METADATA_EMIT_ALL
    HRESULT hr = S_OK;

    ImplMapRec  *pRecord;
    RID         iRecord;

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    _ASSERTE(TypeFromToken(tk) == mdtFieldDef || TypeFromToken(tk) == mdtMethodDef);
    _ASSERTE(!IsNilToken(tk));
    _ASSERTE(!m_bSaveOptimized && "Cannot delete records after PreSave() and before Save().");

    // Get the PinvokeMap record.
    IfFailGo(m_pStgdb->m_MiniMd.FindImplMapHelper(tk, &iRecord));
    if (InvalidRid(iRecord))
    {
        IfFailGo(CLDB_E_RECORD_NOTFOUND);
    }
    IfFailGo(m_pStgdb->m_MiniMd.GetImplMapRecord(iRecord, &pRecord));

    // Clear the MemberForwarded token from the PinvokeMap record.
    IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_ImplMap,
                    ImplMapRec::COL_MemberForwarded, pRecord, mdFieldDefNil));

    // turn off the PinvokeImpl bit.
    if (TypeFromToken(tk) == mdtFieldDef)
    {
        FieldRec    *pFieldRec;

        IfFailGo(m_pStgdb->m_MiniMd.GetFieldRecord(RidFromToken(tk), &pFieldRec));
        pFieldRec->RemoveFlags(fdPinvokeImpl);
    }
    else // TypeFromToken(tk) == mdtMethodDef
    {
        MethodRec   *pMethodRec;

        IfFailGo(m_pStgdb->m_MiniMd.GetMethodRecord(RidFromToken(tk), &pMethodRec));
        pMethodRec->RemoveFlags(mdPinvokeImpl);
    }

    // Update the ENC log for the parent token.
    IfFailGo(UpdateENCLog(tk));
    // Create the log record for the non-token record.
    IfFailGo(UpdateENCLog2(TBL_ImplMap, iRecord));

ErrExit:
    return hr;
#else //!FEATURE_METADATA_EMIT_ALL
    return E_NOTIMPL;
#endif //!FEATURE_METADATA_EMIT_ALL
} // RegMeta::DeletePinvokeMap

//*****************************************************************************
// Create and define a new FieldDef record.
//*****************************************************************************
HRESULT RegMeta::DefineField(           // S_OK or error.
    mdTypeDef   td,                     // Parent TypeDef
    LPCWSTR     szName,                 // Name of member
    DWORD       dwFieldFlags,           // Member attributes
    PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of COM+ signature
    ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
    DWORD       dwCPlusTypeFlag,        // [IN] flag for value type. selected ELEMENT_TYPE_*
    void const  *pValue,                // [IN] constant value
    ULONG       cchValue,               // [IN] size of constant value (string, in wide chars).
    mdFieldDef  *pmd)                   // [OUT] Put member token here
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    FieldRec    *pRecord = NULL;        // The new record.
    RID         iRecord;                // RID of new record.
    LPUTF8      szNameUtf8;
    UTF8STR(szName, szNameUtf8);

    LOCKWRITE();

    _ASSERTE(pmd);

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());
    IsGlobalMethodParent(&td);

    // Validate flags.
    if (dwFieldFlags != UINT32_MAX)
    {
        // fdHasFieldRVA is settable, but not re-settable by applications.
        _ASSERTE((dwFieldFlags & (fdReservedMask&~(fdHasFieldRVA|fdRTSpecialName))) == 0);
        dwFieldFlags &= ~(fdReservedMask&~fdHasFieldRVA);
    }

    // See if this field has already been defined as a forward reference
    // from a MemberRef.  If so, then update the data to match what we know now.
    if (CheckDups(MDDupFieldDef))
    {

        hr = ImportHelper::FindField(&(m_pStgdb->m_MiniMd),
            td,
            szNameUtf8,
            pvSigBlob,
            cbSigBlob,
            pmd);
        if (SUCCEEDED(hr))
        {
            if (IsENCOn())
            {
                IfFailGo(m_pStgdb->m_MiniMd.GetFieldRecord(RidFromToken(*pmd), &pRecord));
            }
            else
            {
                hr = META_S_DUPLICATE;
                goto ErrExit;
            }
        }
        else if (hr != CLDB_E_RECORD_NOTFOUND)
        {
            IfFailGo(hr);
        }
    }

    // Create a new record.
    if (pRecord == NULL)
    {
        // Create the field record.
        IfFailGo(m_pStgdb->m_MiniMd.AddFieldRecord(&pRecord, &iRecord));

        // Set output parameter pmd.
        *pmd = TokenFromRid(iRecord, mdtFieldDef);

        // Add to parent's list of child records.
        IfFailGo(m_pStgdb->m_MiniMd.AddFieldToTypeDef(RidFromToken(td), iRecord));

        IfFailGo(UpdateENCLog(td, CMiniMdRW::eDeltaFieldCreate));

        // record the more defs are introduced.
        SetMemberDefDirty(true);
    }

    // Set the Field properties.
    IfFailGo(m_pStgdb->m_MiniMd.PutString(TBL_Field, FieldRec::COL_Name, pRecord, szNameUtf8));
    IfFailGo(m_pStgdb->m_MiniMd.PutBlob(TBL_Field, FieldRec::COL_Signature, pRecord,
                                        pvSigBlob, cbSigBlob));

    // Check to see if it is value__ for enum type
    // <TODO>@FUTURE: shouldn't we have checked the type containing the field to be a Enum type first of all?</TODO>
    // value__ is defined in corhdr.h.  However, corhdr.h does not have the
    // the W() macro we need (since it's distributed to windows).  We substitute the values of the
    // macro in the code below to work around this issue.
    // #define COR_ENUM_FIELD_NAME_W       L"value__"

    if (!u16_strcmp(szName, W("value__")))
    {
        dwFieldFlags |= fdRTSpecialName | fdSpecialName;
    }
    SetCallerDefine();
    IfFailGo(_SetFieldProps(*pmd, dwFieldFlags, dwCPlusTypeFlag, pValue, cchValue));
    IfFailGo(m_pStgdb->m_MiniMd.AddMemberDefToHash(*pmd, td) );

ErrExit:
    SetCallerExternal();

    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::DefineField

//*****************************************************************************
// Define and set a Property record.
//*****************************************************************************
HRESULT RegMeta::DefineProperty(
    mdTypeDef   td,                     // [IN] the class/interface on which the property is being defined
    LPCWSTR     szProperty,             // [IN] Name of the property
    DWORD       dwPropFlags,            // [IN] CorPropertyAttr
    PCCOR_SIGNATURE pvSig,              // [IN] the required type signature
    ULONG       cbSig,                  // [IN] the size of the type signature blob
    DWORD       dwCPlusTypeFlag,        // [IN] flag for value type. selected ELEMENT_TYPE_*
    void const  *pValue,                // [IN] constant value
    ULONG       cchValue,               // [IN] size of constant value (string, in wide chars).
    mdMethodDef mdSetter,               // [IN] optional setter of the property
    mdMethodDef mdGetter,               // [IN] optional getter of the property
    mdMethodDef rmdOtherMethods[],      // [IN] an optional array of other methods
    mdProperty  *pmdProp)               // [OUT] output property token
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    PropertyRec *pPropRec = NULL;
    RID         iPropRec;
    PropertyMapRec *pPropMap;
    RID         iPropMap;
    LPUTF8      szUTF8Property;
    UTF8STR(szProperty, szUTF8Property);

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    _ASSERTE(TypeFromToken(td) == mdtTypeDef && td != mdTypeDefNil &&
            szProperty && pvSig && cbSig && pmdProp);

    if (CheckDups(MDDupProperty))
    {
        hr = ImportHelper::FindProperty(&(m_pStgdb->m_MiniMd), td, szUTF8Property, pvSig, cbSig, pmdProp);
        if (SUCCEEDED(hr))
        {
            if (IsENCOn())
                IfFailGo(m_pStgdb->m_MiniMd.GetPropertyRecord(RidFromToken(*pmdProp), &pPropRec));
            else
            {
                hr = META_S_DUPLICATE;
                goto ErrExit;
            }
        }
        else if (hr != CLDB_E_RECORD_NOTFOUND)
            IfFailGo(hr);
    }

    if (! pPropRec)
    {
        // Create a new map if one doesn't exist already, else retrieve the existing one.
        // The property map must be created before the PropertyRecord, the new property
        // map will be pointing past the first property record.
        IfFailGo(m_pStgdb->m_MiniMd.FindPropertyMapFor(RidFromToken(td), &iPropMap));
        if (InvalidRid(iPropMap))
        {
            // Create new record.
            IfFailGo(m_pStgdb->m_MiniMd.AddPropertyMapRecord(&pPropMap, &iPropMap));
            // Set parent.
            IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_PropertyMap,
                                                PropertyMapRec::COL_Parent, pPropMap, td));
            IfFailGo(UpdateENCLog2(TBL_PropertyMap, iPropMap));
        }
        else
        {
            IfFailGo(m_pStgdb->m_MiniMd.GetPropertyMapRecord(iPropMap, &pPropMap));
        }

        // Create a new record.
        IfFailGo(m_pStgdb->m_MiniMd.AddPropertyRecord(&pPropRec, &iPropRec));

        // Set output parameter.
        *pmdProp = TokenFromRid(iPropRec, mdtProperty);

        // Add Property to the PropertyMap.
        IfFailGo(m_pStgdb->m_MiniMd.AddPropertyToPropertyMap(RidFromToken(iPropMap), iPropRec));

        IfFailGo(UpdateENCLog2(TBL_PropertyMap, iPropMap, CMiniMdRW::eDeltaPropertyCreate));
    }

    // Save the data.
    IfFailGo(m_pStgdb->m_MiniMd.PutBlob(TBL_Property, PropertyRec::COL_Type, pPropRec,
                                        pvSig, cbSig));
    IfFailGo( m_pStgdb->m_MiniMd.PutString(TBL_Property, PropertyRec::COL_Name,
                                            pPropRec, szUTF8Property) );

    SetCallerDefine();
    IfFailGo(_SetPropertyProps(*pmdProp, dwPropFlags, dwCPlusTypeFlag, pValue, cchValue, mdSetter,
                              mdGetter, rmdOtherMethods));

    // Add the <property token, typedef token> to the lookup table
    if (m_pStgdb->m_MiniMd.HasIndirectTable(TBL_Property))
        IfFailGo( m_pStgdb->m_MiniMd.AddPropertyToLookUpTable(*pmdProp, td) );

ErrExit:
    SetCallerExternal();

    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::DefineProperty

//*****************************************************************************
// Create a record in the Param table. Any set of name, flags, or default value
// may be set.
//*****************************************************************************
HRESULT RegMeta::DefineParam(
    mdMethodDef md,                     // [IN] Owning method
    ULONG       ulParamSeq,             // [IN] Which param
    LPCWSTR     szName,                 // [IN] Optional param name
    DWORD       dwParamFlags,           // [IN] Optional param flags
    DWORD       dwCPlusTypeFlag,        // [IN] flag for value type. selected ELEMENT_TYPE_*
    void const  *pValue,                // [IN] constant value
    ULONG       cchValue,               // [IN] size of constant value (string, in wide chars).
    mdParamDef  *ppd)                   // [OUT] Put param token here
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    RID         iRecord;
    ParamRec    *pRecord = 0;

    LOCKWRITE();

    _ASSERTE(TypeFromToken(md) == mdtMethodDef && md != mdMethodDefNil &&
             ulParamSeq != UINT32_MAX && ppd);

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    // Retrieve or create the Param row.
    if (CheckDups(MDDupParamDef))
    {
        hr = _FindParamOfMethod(md, ulParamSeq, ppd);
        if (SUCCEEDED(hr))
        {
            if (IsENCOn())
                IfFailGo(m_pStgdb->m_MiniMd.GetParamRecord(RidFromToken(*ppd), &pRecord));
            else
            {
                hr = META_S_DUPLICATE;
                goto ErrExit;
            }
        }
        else if (hr != CLDB_E_RECORD_NOTFOUND)
            IfFailGo(hr);
    }

    if (!pRecord)
    {
        // Create the Param record.
        IfFailGo(m_pStgdb->m_MiniMd.AddParamRecord(&pRecord, &iRecord));

        // Set the output parameter.
        *ppd = TokenFromRid(iRecord, mdtParamDef);

        // Set sequence number.
        pRecord->SetSequence(static_cast<USHORT>(ulParamSeq));

        // Add to the parent's list of child records.
        IfFailGo(m_pStgdb->m_MiniMd.AddParamToMethod(RidFromToken(md), iRecord));

        IfFailGo(UpdateENCLog(md, CMiniMdRW::eDeltaParamCreate));
    }

    SetCallerDefine();
    // Set the properties.
    IfFailGo(_SetParamProps(*ppd, szName, dwParamFlags, dwCPlusTypeFlag, pValue, cchValue));

ErrExit:
    ;
    SetCallerExternal();

    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::DefineParam

//*****************************************************************************
// Set the properties on the given Field token.
//*****************************************************************************
HRESULT RegMeta::SetFieldProps(           // S_OK or error.
    mdFieldDef  fd,                     // [IN] The FieldDef.
    DWORD       dwFieldFlags,           // [IN] Field attributes.
    DWORD       dwCPlusTypeFlag,        // [IN] Flag for the value type, selected ELEMENT_TYPE_*
    void const  *pValue,                // [IN] Constant value.
    ULONG       cchValue)               // [IN] size of constant value (string, in wide chars).
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    // Validate flags.
    if (dwFieldFlags != UINT32_MAX)
    {
        // fdHasFieldRVA is settable, but not re-settable by applications.
        _ASSERTE((dwFieldFlags & (fdReservedMask&~(fdHasFieldRVA|fdRTSpecialName))) == 0);
        dwFieldFlags &= ~(fdReservedMask&~fdHasFieldRVA);
    }

    hr = _SetFieldProps(fd, dwFieldFlags, dwCPlusTypeFlag, pValue, cchValue);

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::SetFieldProps

//*****************************************************************************
// Set the properties on the given Property token.
//*****************************************************************************
HRESULT RegMeta::SetPropertyProps(      // S_OK or error.
    mdProperty  pr,                     // [IN] Property token.
    DWORD       dwPropFlags,            // [IN] CorPropertyAttr.
    DWORD       dwCPlusTypeFlag,        // [IN] Flag for value type, selected ELEMENT_TYPE_*
    void const  *pValue,                // [IN] Constant value.
    ULONG       cchValue,               // [IN] size of constant value (string, in wide chars).
    mdMethodDef mdSetter,               // [IN] Setter of the property.
    mdMethodDef mdGetter,               // [IN] Getter of the property.
    mdMethodDef rmdOtherMethods[])      // [IN] Array of other methods.
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    hr = _SetPropertyProps(pr, dwPropFlags, dwCPlusTypeFlag, pValue, cchValue, mdSetter, mdGetter, rmdOtherMethods);

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::SetPropertyProps


//*****************************************************************************
// This routine sets properties on the given Param token.
//*****************************************************************************
HRESULT RegMeta::SetParamProps(         // Return code.
    mdParamDef  pd,                     // [IN] Param token.
    LPCWSTR     szName,                 // [IN] Param name.
    DWORD       dwParamFlags,           // [IN] Param flags.
    DWORD       dwCPlusTypeFlag,        // [IN] Flag for value type. selected ELEMENT_TYPE_*.
    void const  *pValue,                // [OUT] Constant value.
    ULONG       cchValue)               // [IN] size of constant value (string, in wide chars).
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    HRESULT hr = S_OK;

    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    hr = _SetParamProps(pd, szName, dwParamFlags, dwCPlusTypeFlag, pValue, cchValue);

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::SetParamProps

//*****************************************************************************
// Apply edit and continue changes to this metadata.
//*****************************************************************************
STDMETHODIMP RegMeta::ApplyEditAndContinue(   // S_OK or error.
    IUnknown    *pUnk)                  // [IN] Metadata from the delta PE.
{
#ifdef FEATURE_METADATA_EMIT_ALL
    HRESULT hr;

    IMetaDataImport2 *pImport=0;        // Interface on the delta metadata.
    RegMeta     *pDeltaMD=0;            // The delta metadata.
    CMiniMdRW   *mdDelta = NULL;
    CMiniMdRW   *mdBase = NULL;

    // Get the MiniMd on the delta.
    IfFailGo(pUnk->QueryInterface(IID_IMetaDataImport2, (void**)&pImport));

    pDeltaMD = static_cast<RegMeta*>(pImport);

    mdDelta = &(pDeltaMD->m_pStgdb->m_MiniMd);
    mdBase = &(m_pStgdb->m_MiniMd);

    IfFailGo(mdBase->ConvertToRW());
    IfFailGo(mdBase->ApplyDelta(*mdDelta));

ErrExit:
    if (pImport)
        pImport->Release();

    return hr;
#else //!FEATURE_METADATA_EMIT_ALL
    return E_NOTIMPL;
#endif //!FEATURE_METADATA_EMIT_ALL
} // RegMeta::ApplyEditAndContinue

#endif //FEATURE_METADATA_EMIT
