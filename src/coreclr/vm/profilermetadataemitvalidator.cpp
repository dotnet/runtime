// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



#include "common.h"
#include "profilermetadataemitvalidator.h"

ProfilerMetadataEmitValidator::ProfilerMetadataEmitValidator(IMetaDataEmit* pInnerEmit) :
m_cRefCount(0)
{
    LIMITED_METHOD_CONTRACT;

    ReleaseHolder<IGetIMDInternalImport> pGetIMDInternalImport;
    pInnerEmit->QueryInterface(IID_IGetIMDInternalImport, (void**)&pGetIMDInternalImport);
    pGetIMDInternalImport->GetIMDInternalImport(&m_pInnerInternalImport);

    pInnerEmit->QueryInterface(IID_IMetaDataImport2, (void**)&m_pInnerImport);
    pInnerEmit->QueryInterface(IID_IMetaDataAssemblyImport, (void**)&m_pInnerAssemblyImport);
    pInnerEmit->QueryInterface(IID_IMetaDataEmit2, (void**) &m_pInner);
    pInnerEmit->QueryInterface(IID_IMetaDataAssemblyEmit, (void**) &m_pInnerAssembly);

    // GetCountWithTokenType does not count the 0 RID token, thus the max valid RID = count
    // Confusingly the method treats TypeDef specially by ignoring 0x02000001 as well. For TypeDef max RID is count+1
    maxInitialTypeDef = TokenFromRid(m_pInnerInternalImport->GetCountWithTokenKind(mdtTypeDef) + 1, mdtTypeDef);
    maxInitialMethodDef = TokenFromRid(m_pInnerInternalImport->GetCountWithTokenKind(mdtMethodDef), mdtMethodDef);
    maxInitialFieldDef = TokenFromRid(m_pInnerInternalImport->GetCountWithTokenKind(mdtFieldDef), mdtFieldDef);
    maxInitialMemberRef = TokenFromRid(m_pInnerInternalImport->GetCountWithTokenKind(mdtMemberRef), mdtMemberRef);
    maxInitialParamDef = TokenFromRid(m_pInnerInternalImport->GetCountWithTokenKind(mdtParamDef), mdtParamDef);
    maxInitialCustomAttribute = TokenFromRid(m_pInnerInternalImport->GetCountWithTokenKind(mdtCustomAttribute), mdtCustomAttribute);
    maxInitialEvent = TokenFromRid(m_pInnerInternalImport->GetCountWithTokenKind(mdtEvent), mdtEvent);
    maxInitialProperty = TokenFromRid(m_pInnerInternalImport->GetCountWithTokenKind(mdtProperty), mdtProperty);
    maxInitialGenericParam = TokenFromRid(m_pInnerInternalImport->GetCountWithTokenKind(mdtGenericParam), mdtGenericParam);
}

ProfilerMetadataEmitValidator::~ProfilerMetadataEmitValidator()
{
    LIMITED_METHOD_CONTRACT;
}

  //IUnknown
HRESULT ProfilerMetadataEmitValidator::QueryInterface(REFIID riid, void** ppInterface)
{
    if(riid == IID_IUnknown)
    {
        *ppInterface = static_cast<IUnknown*>(static_cast<IMetaDataEmit*>(this));
        AddRef();
    }
    else if(riid == IID_IMetaDataEmit)
    {
        *ppInterface = static_cast<IMetaDataEmit*>(this);
        AddRef();
    }
    else if(riid == IID_IMetaDataEmit2)
    {
        *ppInterface = static_cast<IMetaDataEmit2*>(this);
        AddRef();
    }
    else if(riid == IID_IMetaDataAssemblyEmit)
    {
        *ppInterface = static_cast<IMetaDataAssemblyEmit*>(this);
        AddRef();
    }
    else if (riid == IID_IMetaDataImport)
    {
        *ppInterface = static_cast<IMetaDataImport*>(this);
        AddRef();
    }
    else if (riid == IID_IMetaDataImport2)
    {
        *ppInterface = static_cast<IMetaDataImport2*>(this);
        AddRef();
    }
    else if (riid == IID_IMetaDataAssemblyImport)
    {
        *ppInterface = static_cast<IMetaDataAssemblyImport*>(this);
        AddRef();
    }
    else
    {
        return E_NOINTERFACE;
    }

    return S_OK;
}

ULONG ProfilerMetadataEmitValidator::AddRef()
{
    return InterlockedIncrement(&m_cRefCount);
}

ULONG ProfilerMetadataEmitValidator::Release()
{
    ULONG ret = InterlockedDecrement(&m_cRefCount);
    if(ret == 0)
    {
        delete this;
    }
    return ret;
}

  //IMetaDataEmit
HRESULT ProfilerMetadataEmitValidator::SetModuleProps(
        LPCWSTR     szName)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::Save(
        LPCWSTR     szFile,
        DWORD       dwSaveFlags)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::SaveToStream(
        IStream     *pIStream,
        DWORD       dwSaveFlags)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::GetSaveSize(
        CorSaveSize fSave,
        DWORD       *pdwSaveSize)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::DefineTypeDef(
        LPCWSTR     szTypeDef,
        DWORD       dwTypeDefFlags,
        mdToken     tkExtends,
        mdToken     rtkImplements[],
        mdTypeDef   *ptd)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInner->DefineTypeDef(szTypeDef, dwTypeDefFlags, tkExtends, rtkImplements, ptd);
}

HRESULT ProfilerMetadataEmitValidator::DefineNestedType(
        LPCWSTR     szTypeDef,
        DWORD       dwTypeDefFlags,
        mdToken     tkExtends,
        mdToken     rtkImplements[],
        mdTypeDef   tdEncloser,
        mdTypeDef   *ptd)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInner->DefineNestedType(szTypeDef, dwTypeDefFlags, tkExtends, rtkImplements, tdEncloser, ptd);
}

HRESULT ProfilerMetadataEmitValidator::SetHandler(
        IUnknown    *pUnk)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::DefineMethod(
        mdTypeDef   td,
        LPCWSTR     szName,
        DWORD       dwMethodFlags,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        ULONG       ulCodeRVA,
        DWORD       dwImplFlags,
        mdMethodDef *pmd)
{
    LIMITED_METHOD_CONTRACT;
    //modifying pre-existing types is not allowed
    if (td <= maxInitialTypeDef)
    {
        return COR_E_NOTSUPPORTED;
    }
    return m_pInner->DefineMethod(td, szName, dwMethodFlags, pvSigBlob, cbSigBlob, ulCodeRVA, dwImplFlags, pmd);
}

HRESULT ProfilerMetadataEmitValidator::DefineMethodImpl(
        mdTypeDef   td,
        mdToken     tkBody,
        mdToken     tkDecl)
{
    LIMITED_METHOD_CONTRACT;
    //modifying pre-existing types is not allowed
    if (td <= maxInitialTypeDef)
    {
        return COR_E_NOTSUPPORTED;
    }
    return m_pInner->DefineMethodImpl(td, tkBody, tkDecl);
}

HRESULT ProfilerMetadataEmitValidator::DefineTypeRefByName(
        mdToken     tkResolutionScope,
        LPCWSTR     szName,
        mdTypeRef   *ptr)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInner->DefineTypeRefByName(tkResolutionScope, szName, ptr);
}

HRESULT ProfilerMetadataEmitValidator::DefineImportType(
        IMetaDataAssemblyImport *pAssemImport,
        const void  *pbHashValue,
        ULONG       cbHashValue,
        IMetaDataImport *pImport,
        mdTypeDef   tdImport,
        IMetaDataAssemblyEmit *pAssemEmit,
        mdTypeRef   *ptr)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::DefineMemberRef(
        mdToken     tkImport,
        LPCWSTR     szName,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        mdMemberRef *pmr)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInner->DefineMemberRef(tkImport, szName, pvSigBlob, cbSigBlob, pmr);
}

HRESULT ProfilerMetadataEmitValidator::DefineImportMember(
        IMetaDataAssemblyImport *pAssemImport,
        const void  *pbHashValue,
        ULONG       cbHashValue,
        IMetaDataImport *pImport,
        mdToken     mbMember,
        IMetaDataAssemblyEmit *pAssemEmit,
        mdToken     tkParent,
        mdMemberRef *pmr)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::DefineEvent(
        mdTypeDef   td,
        LPCWSTR     szEvent,
        DWORD       dwEventFlags,
        mdToken     tkEventType,
        mdMethodDef mdAddOn,
        mdMethodDef mdRemoveOn,
        mdMethodDef mdFire,
        mdMethodDef rmdOtherMethods[],
        mdEvent     *pmdEvent)
{
    LIMITED_METHOD_CONTRACT;
    //modifying pre-existing types is not allowed
    if (td <= maxInitialTypeDef)
    {
        return COR_E_NOTSUPPORTED;
    }
    return m_pInner->DefineEvent(td, szEvent, dwEventFlags, tkEventType, mdAddOn, mdRemoveOn, mdFire, rmdOtherMethods, pmdEvent);
}

HRESULT ProfilerMetadataEmitValidator::SetClassLayout(
        mdTypeDef   td,
        DWORD       dwPackSize,
        COR_FIELD_OFFSET rFieldOffsets[],
        ULONG       ulClassSize)
{
    LIMITED_METHOD_CONTRACT;
    //modifying pre-existing types is not allowed
    if (td <= maxInitialTypeDef)
    {
        return COR_E_NOTSUPPORTED;
    }
    return m_pInner->SetClassLayout(td, dwPackSize, rFieldOffsets, ulClassSize);
}

HRESULT ProfilerMetadataEmitValidator::DeleteClassLayout(
        mdTypeDef   td)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::SetFieldMarshal(
        mdToken     tk,
        PCCOR_SIGNATURE pvNativeType,
        ULONG       cbNativeType)
{
    LIMITED_METHOD_CONTRACT;
    //modifying pre-existing field/property is not allowed
    if ((TypeFromToken(tk) == mdtProperty && tk <= maxInitialProperty) ||
        (TypeFromToken(tk) == mdtFieldDef && tk <= maxInitialFieldDef))
    {
        return COR_E_NOTSUPPORTED;
    }
    //if the token wasn't a field/param we let it through just to get
    //the appropriate error behavior from the inner emitter
    return m_pInner->SetFieldMarshal(tk, pvNativeType, cbNativeType);
}

HRESULT ProfilerMetadataEmitValidator::DeleteFieldMarshal(
        mdToken     tk)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::DefinePermissionSet(
        mdToken     tk,
        DWORD       dwAction,
        void const  *pvPermission,
        ULONG       cbPermission,
        mdPermission *ppm)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::SetRVA(
        mdMethodDef md,
        ULONG       ulRVA)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::GetTokenFromSig(
        PCCOR_SIGNATURE pvSig,
        ULONG       cbSig,
        mdSignature *pmsig)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInner->GetTokenFromSig(pvSig, cbSig, pmsig);
}

HRESULT ProfilerMetadataEmitValidator::DefineModuleRef(
        LPCWSTR     szName,
        mdModuleRef *pmur)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInner->DefineModuleRef(szName, pmur);
}

HRESULT ProfilerMetadataEmitValidator::SetParent(
        mdMemberRef mr,
        mdToken     tk)
{
    LIMITED_METHOD_CONTRACT;
    //modifying pre-existing memberref is not allowed
    if (mr <= maxInitialMemberRef)
    {
        return COR_E_NOTSUPPORTED;
    }
    return m_pInner->SetParent(mr, tk);
}

HRESULT ProfilerMetadataEmitValidator::GetTokenFromTypeSpec(
        PCCOR_SIGNATURE pvSig,
        ULONG       cbSig,
        mdTypeSpec *ptypespec)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInner->GetTokenFromTypeSpec(pvSig, cbSig, ptypespec);
}

HRESULT ProfilerMetadataEmitValidator::SaveToMemory(
        void        *pbData,
        ULONG       cbData)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::DefineUserString(
        LPCWSTR szString,
        ULONG       cchString,
        mdString    *pstk)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInner->DefineUserString(szString, cchString, pstk);
}

HRESULT ProfilerMetadataEmitValidator::DeleteToken(
        mdToken     tkObj)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::SetMethodProps(
        mdMethodDef md,
        DWORD       dwMethodFlags,
        ULONG       ulCodeRVA,
        DWORD       dwImplFlags)
{
    LIMITED_METHOD_CONTRACT;
    //modifying pre-existing methods is not allowed
    if (md <= maxInitialMethodDef)
    {
        return COR_E_NOTSUPPORTED;
    }
    return m_pInner->SetMethodProps(md, dwMethodFlags, ulCodeRVA, dwImplFlags);
}

HRESULT ProfilerMetadataEmitValidator::SetTypeDefProps(
        mdTypeDef   td,
        DWORD       dwTypeDefFlags,
        mdToken     tkExtends,
        mdToken     rtkImplements[])
{
    LIMITED_METHOD_CONTRACT;
    //modifying pre-existing types is not allowed
    if (td <= maxInitialTypeDef)
    {
        return COR_E_NOTSUPPORTED;
    }
    return m_pInner->SetTypeDefProps(td, dwTypeDefFlags, tkExtends, rtkImplements);
}

HRESULT ProfilerMetadataEmitValidator::SetEventProps(
        mdEvent     ev,
        DWORD       dwEventFlags,
        mdToken     tkEventType,
        mdMethodDef mdAddOn,
        mdMethodDef mdRemoveOn,
        mdMethodDef mdFire,
        mdMethodDef rmdOtherMethods[])
{
    LIMITED_METHOD_CONTRACT;
    //modifying pre-existing events is not allowed
    if (ev <= maxInitialEvent)
    {
        return COR_E_NOTSUPPORTED;
    }
    return m_pInner->SetEventProps(ev, dwEventFlags, tkEventType, mdAddOn, mdRemoveOn, mdFire, rmdOtherMethods);
}

HRESULT ProfilerMetadataEmitValidator::SetPermissionSetProps(
        mdToken     tk,
        DWORD       dwAction,
        void const  *pvPermission,
        ULONG       cbPermission,
        mdPermission *ppm)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::DefinePinvokeMap(
        mdToken     tk,
        DWORD       dwMappingFlags,
        LPCWSTR     szImportName,
        mdModuleRef mrImportDLL)
{
    LIMITED_METHOD_CONTRACT;
    //modifying pre-existing methods is not allowed
    if (tk <= maxInitialMethodDef)
    {
        return COR_E_NOTSUPPORTED;
    }
    return m_pInner->DefinePinvokeMap(tk, dwMappingFlags, szImportName, mrImportDLL);
}

HRESULT ProfilerMetadataEmitValidator::SetPinvokeMap(
        mdToken     tk,
        DWORD       dwMappingFlags,
        LPCWSTR     szImportName,
        mdModuleRef mrImportDLL)
{
    LIMITED_METHOD_CONTRACT;
    //modifying pre-existing types is not allowed
    if (tk <= maxInitialTypeDef)
    {
        return COR_E_NOTSUPPORTED;
    }
    return m_pInner->SetPinvokeMap(tk, dwMappingFlags, szImportName, mrImportDLL);
}

HRESULT ProfilerMetadataEmitValidator::DeletePinvokeMap(
        mdToken     tk)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::DefineCustomAttribute(
        mdToken     tkOwner,
        mdToken     tkCtor,
        void const  *pCustomAttribute,
        ULONG       cbCustomAttribute,
        mdCustomAttribute *pcv)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInner->DefineCustomAttribute(tkOwner, tkCtor, pCustomAttribute, cbCustomAttribute, pcv);
}

HRESULT ProfilerMetadataEmitValidator::SetCustomAttributeValue(
        mdCustomAttribute pcv,
        void const  *pCustomAttribute,
        ULONG       cbCustomAttribute)
{
    LIMITED_METHOD_CONTRACT;
    //modifying pre-existing CAs is not allowed
    if (pcv <= maxInitialCustomAttribute)
    {
        return COR_E_NOTSUPPORTED;
    }
    return m_pInner->SetCustomAttributeValue(pcv, pCustomAttribute, cbCustomAttribute);
}

HRESULT ProfilerMetadataEmitValidator::DefineField(
        mdTypeDef   td,
        LPCWSTR     szName,
        DWORD       dwFieldFlags,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue,
        mdFieldDef  *pmd)
{
    LIMITED_METHOD_CONTRACT;
    //modifying pre-existing types is not allowed
    if (td <= maxInitialTypeDef)
    {
        return COR_E_NOTSUPPORTED;
    }
    return m_pInner->DefineField(td, szName, dwFieldFlags, pvSigBlob, cbSigBlob, dwCPlusTypeFlag, pValue, cchValue, pmd);
}

HRESULT ProfilerMetadataEmitValidator::DefineProperty(
        mdTypeDef   td,
        LPCWSTR     szProperty,
        DWORD       dwPropFlags,
        PCCOR_SIGNATURE pvSig,
        ULONG       cbSig,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue,
        mdMethodDef mdSetter,
        mdMethodDef mdGetter,
        mdMethodDef rmdOtherMethods[],
        mdProperty  *pmdProp)
{
    LIMITED_METHOD_CONTRACT;
    //modifying pre-existing types is not allowed
    if (td <= maxInitialTypeDef)
    {
        return COR_E_NOTSUPPORTED;
    }
    return m_pInner->DefineProperty(td, szProperty, dwPropFlags, pvSig, cbSig, dwCPlusTypeFlag, pValue, cchValue, mdSetter, mdGetter, rmdOtherMethods, pmdProp);
}

HRESULT ProfilerMetadataEmitValidator::DefineParam(
        mdMethodDef md,
        ULONG       ulParamSeq,
        LPCWSTR     szName,
        DWORD       dwParamFlags,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue,
        mdParamDef  *ppd)
{
    LIMITED_METHOD_CONTRACT;
    //modifying pre-existing methods is not allowed
    if (md <= maxInitialMethodDef)
    {
        return COR_E_NOTSUPPORTED;
    }
    return m_pInner->DefineParam(md, ulParamSeq, szName, dwParamFlags, dwCPlusTypeFlag, pValue, cchValue, ppd);
}

HRESULT ProfilerMetadataEmitValidator::SetFieldProps(
        mdFieldDef  fd,
        DWORD       dwFieldFlags,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue)
{
    LIMITED_METHOD_CONTRACT;
    //modifying pre-existing fields is not allowed
    if (fd <= maxInitialFieldDef)
    {
        return COR_E_NOTSUPPORTED;
    }
    return m_pInner->SetFieldProps(fd, dwFieldFlags, dwCPlusTypeFlag, pValue, cchValue);
}

HRESULT ProfilerMetadataEmitValidator::SetPropertyProps(
        mdProperty  pr,
        DWORD       dwPropFlags,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue,
        mdMethodDef mdSetter,
        mdMethodDef mdGetter,
        mdMethodDef rmdOtherMethods[])
{
    LIMITED_METHOD_CONTRACT;
    //modifying pre-existing properties is not allowed
    if (pr <= maxInitialProperty)
    {
        return COR_E_NOTSUPPORTED;
    }
    return m_pInner->SetPropertyProps(pr, dwPropFlags, dwCPlusTypeFlag, pValue, cchValue, mdSetter, mdGetter, rmdOtherMethods);
}

HRESULT ProfilerMetadataEmitValidator::SetParamProps(
        mdParamDef  pd,
        LPCWSTR     szName,
        DWORD       dwParamFlags,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue)
{
    LIMITED_METHOD_CONTRACT;
    //modifying pre-existing params is not allowed
    if (pd <= maxInitialParamDef)
    {
        return COR_E_NOTSUPPORTED;
    }
    return m_pInner->SetParamProps(pd, szName, dwParamFlags, dwCPlusTypeFlag, pValue, cchValue);
}

HRESULT ProfilerMetadataEmitValidator::DefineSecurityAttributeSet(
        mdToken     tkObj,
        COR_SECATTR rSecAttrs[],
        ULONG       cSecAttrs,
        ULONG       *pulErrorAttr)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::ApplyEditAndContinue(
        IUnknown    *pImport)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::TranslateSigWithScope(
        IMetaDataAssemblyImport *pAssemImport,
        const void  *pbHashValue,
        ULONG       cbHashValue,
        IMetaDataImport *import,
        PCCOR_SIGNATURE pbSigBlob,
        ULONG       cbSigBlob,
        IMetaDataAssemblyEmit *pAssemEmit,
        IMetaDataEmit *emit,
        PCOR_SIGNATURE pvTranslatedSig,
        ULONG       cbTranslatedSigMax,
        ULONG       *pcbTranslatedSig)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::SetMethodImplFlags(
        mdMethodDef md,
        DWORD       dwImplFlags)
{
    LIMITED_METHOD_CONTRACT;
    //modifying pre-existing methods is not supported
    if (md <= maxInitialMethodDef)
    {
        return COR_E_NOTSUPPORTED;
    }
    return m_pInner->SetMethodImplFlags(md, dwImplFlags);
}

HRESULT ProfilerMetadataEmitValidator::SetFieldRVA(
        mdFieldDef  fd,
        ULONG       ulRVA)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::Merge(
        IMetaDataImport *pImport,
        IMapToken   *pHostMapToken,
        IUnknown    *pHandler)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::MergeEnd()
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

    // IMetaDataEmit2
HRESULT ProfilerMetadataEmitValidator::DefineMethodSpec(
        mdToken     tkParent,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        mdMethodSpec *pmi)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInner->DefineMethodSpec(tkParent, pvSigBlob, cbSigBlob, pmi);
}

HRESULT ProfilerMetadataEmitValidator::GetDeltaSaveSize(
        CorSaveSize fSave,
        DWORD       *pdwSaveSize)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::SaveDelta(
        LPCWSTR     szFile,
        DWORD       dwSaveFlags)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::SaveDeltaToStream(
        IStream     *pIStream,
        DWORD       dwSaveFlags)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::SaveDeltaToMemory(
        void        *pbData,
        ULONG       cbData)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::DefineGenericParam(
        mdToken      tk,
        ULONG        ulParamSeq,
        DWORD        dwParamFlags,
        LPCWSTR      szname,
        DWORD        reserved,
        mdToken      rtkConstraints[],
        mdGenericParam *pgp)
{
    LIMITED_METHOD_CONTRACT;
    //modifying pre-existing methods/types is not allowed
    if ((TypeFromToken(tk) == mdtTypeDef && tk <= maxInitialTypeDef) ||
        (TypeFromToken(tk) == mdtMethodDef && tk <= maxInitialMethodDef))
    {
        return COR_E_NOTSUPPORTED;
    }
    return m_pInner->DefineGenericParam(tk, ulParamSeq, dwParamFlags, szname, reserved, rtkConstraints, pgp);
}

HRESULT ProfilerMetadataEmitValidator::SetGenericParamProps(
        mdGenericParam gp,
        DWORD        dwParamFlags,
        LPCWSTR      szName,
        DWORD        reserved,
        mdToken      rtkConstraints[])
{
    LIMITED_METHOD_CONTRACT;
    //modifying pre-existing generic param is not allowed
    if (gp <= maxInitialGenericParam)
    {
        return COR_E_NOTSUPPORTED;
    }
    return m_pInner->SetGenericParamProps(gp, dwParamFlags, szName, reserved, rtkConstraints);
}

HRESULT ProfilerMetadataEmitValidator::ResetENCLog()
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

    //IMetaDataAssemblyEmit
HRESULT ProfilerMetadataEmitValidator::DefineAssembly(
        const void  *pbPublicKey,
        ULONG       cbPublicKey,
        ULONG       ulHashAlgId,
        LPCWSTR     szName,
        const ASSEMBLYMETADATA *pMetaData,
        DWORD       dwAssemblyFlags,
        mdAssembly  *pma)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::DefineAssemblyRef(
        const void  *pbPublicKeyOrToken,
        ULONG       cbPublicKeyOrToken,
        LPCWSTR     szName,
        const ASSEMBLYMETADATA *pMetaData,
        const void  *pbHashValue,
        ULONG       cbHashValue,
        DWORD       dwAssemblyRefFlags,
        mdAssemblyRef *pmdar)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::DefineFile(
        LPCWSTR     szName,
        const void  *pbHashValue,
        ULONG       cbHashValue,
        DWORD       dwFileFlags,
        mdFile      *pmdf)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::DefineExportedType(
        LPCWSTR     szName,
        mdToken     tkImplementation,
        mdTypeDef   tkTypeDef,
        DWORD       dwExportedTypeFlags,
        mdExportedType   *pmdct)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::DefineManifestResource(
        LPCWSTR     szName,
        mdToken     tkImplementation,
        DWORD       dwOffset,
        DWORD       dwResourceFlags,
        mdManifestResource  *pmdmr)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::SetAssemblyProps(
        mdAssembly  pma,
        const void  *pbPublicKey,
        ULONG       cbPublicKey,
        ULONG       ulHashAlgId,
        LPCWSTR     szName,
        const ASSEMBLYMETADATA *pMetaData,
        DWORD       dwAssemblyFlags)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::SetAssemblyRefProps(
        mdAssemblyRef ar,
        const void  *pbPublicKeyOrToken,
        ULONG       cbPublicKeyOrToken,
        LPCWSTR     szName,
        const ASSEMBLYMETADATA *pMetaData,
        const void  *pbHashValue,
        ULONG       cbHashValue,
        DWORD       dwAssemblyRefFlags)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::SetFileProps(
        mdFile      file,
        const void  *pbHashValue,
        ULONG       cbHashValue,
        DWORD       dwFileFlags)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::SetExportedTypeProps(
        mdExportedType   ct,
        mdToken     tkImplementation,
        mdTypeDef   tkTypeDef,
        DWORD       dwExportedTypeFlags)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

HRESULT ProfilerMetadataEmitValidator::SetManifestResourceProps(
        mdManifestResource  mr,
        mdToken     tkImplementation,
        DWORD       dwOffset,
        DWORD       dwResourceFlags)
{
    LIMITED_METHOD_CONTRACT;
    return COR_E_NOTSUPPORTED;
}

//IMetaDataImport
void ProfilerMetadataEmitValidator::CloseEnum(HCORENUM hEnum)
{
    LIMITED_METHOD_CONTRACT;
    m_pInnerImport->CloseEnum(hEnum);
}

HRESULT ProfilerMetadataEmitValidator::CountEnum(HCORENUM hEnum, ULONG *pulCount)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->CountEnum(hEnum, pulCount);
}

HRESULT ProfilerMetadataEmitValidator::ResetEnum(HCORENUM hEnum, ULONG ulPos)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->ResetEnum(hEnum, ulPos);
}

HRESULT ProfilerMetadataEmitValidator::EnumTypeDefs(HCORENUM *phEnum, mdTypeDef rTypeDefs[],
    ULONG cMax, ULONG *pcTypeDefs)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->EnumTypeDefs(phEnum, rTypeDefs, cMax, pcTypeDefs);
}

HRESULT ProfilerMetadataEmitValidator::EnumInterfaceImpls(HCORENUM *phEnum, mdTypeDef td,
    mdInterfaceImpl rImpls[], ULONG cMax,
    ULONG* pcImpls)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->EnumInterfaceImpls(phEnum, td, rImpls, cMax, pcImpls);
}

HRESULT ProfilerMetadataEmitValidator::EnumTypeRefs(HCORENUM *phEnum, mdTypeRef rTypeRefs[],
    ULONG cMax, ULONG* pcTypeRefs)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->EnumTypeRefs(phEnum, rTypeRefs, cMax, pcTypeRefs);
}

HRESULT ProfilerMetadataEmitValidator::FindTypeDefByName(
    LPCWSTR     szTypeDef,
    mdToken     tkEnclosingClass,
    mdTypeDef   *ptd)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->FindTypeDefByName(szTypeDef, tkEnclosingClass, ptd);
}

HRESULT ProfilerMetadataEmitValidator::GetScopeProps(
    LPWSTR      szName,
    ULONG       cchName,
    ULONG       *pchName,
    GUID        *pmvid)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetScopeProps(szName, cchName, pchName, pmvid);
}

HRESULT ProfilerMetadataEmitValidator::GetModuleFromScope(
    mdModule    *pmd)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetModuleFromScope(pmd);
}

HRESULT ProfilerMetadataEmitValidator::GetTypeDefProps(
    mdTypeDef   td,
    LPWSTR      szTypeDef,
    ULONG       cchTypeDef,
    ULONG       *pchTypeDef,
    DWORD       *pdwTypeDefFlags,
    mdToken     *ptkExtends)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetTypeDefProps(td, szTypeDef, cchTypeDef, pchTypeDef, pdwTypeDefFlags, ptkExtends);
}

HRESULT ProfilerMetadataEmitValidator::GetInterfaceImplProps(
    mdInterfaceImpl iiImpl,
    mdTypeDef   *pClass,
    mdToken     *ptkIface)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetInterfaceImplProps(iiImpl, pClass, ptkIface);
}

HRESULT ProfilerMetadataEmitValidator::GetTypeRefProps(
    mdTypeRef   tr,
    mdToken     *ptkResolutionScope,
    LPWSTR      szName,
    ULONG       cchName,
    ULONG       *pchName)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetTypeRefProps(tr, ptkResolutionScope, szName, cchName, pchName);
}

HRESULT ProfilerMetadataEmitValidator::ResolveTypeRef(mdTypeRef tr, REFIID riid, IUnknown **ppIScope, mdTypeDef *ptd)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->ResolveTypeRef(tr, riid, ppIScope, ptd);
}

HRESULT ProfilerMetadataEmitValidator::EnumMembers(
    HCORENUM    *phEnum,
    mdTypeDef   cl,
    mdToken     rMembers[],
    ULONG       cMax,
    ULONG       *pcTokens)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->EnumMembers(phEnum, cl, rMembers, cMax, pcTokens);
}

HRESULT ProfilerMetadataEmitValidator::EnumMembersWithName(
    HCORENUM    *phEnum,
    mdTypeDef   cl,
    LPCWSTR     szName,
    mdToken     rMembers[],
    ULONG       cMax,
    ULONG       *pcTokens)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->EnumMembersWithName(phEnum, cl, szName, rMembers, cMax, pcTokens);
}

HRESULT ProfilerMetadataEmitValidator::EnumMethods(
    HCORENUM    *phEnum,
    mdTypeDef   cl,
    mdMethodDef rMethods[],
    ULONG       cMax,
    ULONG       *pcTokens)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->EnumMethods(phEnum, cl, rMethods, cMax, pcTokens);
}

HRESULT ProfilerMetadataEmitValidator::EnumMethodsWithName(
    HCORENUM    *phEnum,
    mdTypeDef   cl,
    LPCWSTR     szName,
    mdMethodDef rMethods[],
    ULONG       cMax,
    ULONG       *pcTokens)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->EnumMethodsWithName(phEnum, cl, szName, rMethods, cMax, pcTokens);
}

HRESULT ProfilerMetadataEmitValidator::EnumFields(
    HCORENUM    *phEnum,
    mdTypeDef   cl,
    mdFieldDef  rFields[],
    ULONG       cMax,
    ULONG       *pcTokens)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->EnumFields(phEnum, cl, rFields, cMax, pcTokens);
}

HRESULT ProfilerMetadataEmitValidator::EnumFieldsWithName(
    HCORENUM    *phEnum,
    mdTypeDef   cl,
    LPCWSTR     szName,
    mdFieldDef  rFields[],
    ULONG       cMax,
    ULONG       *pcTokens)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->EnumFieldsWithName(phEnum, cl, szName, rFields, cMax, pcTokens);
}


HRESULT ProfilerMetadataEmitValidator::EnumParams(
    HCORENUM    *phEnum,
    mdMethodDef mb,
    mdParamDef  rParams[],
    ULONG       cMax,
    ULONG       *pcTokens)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->EnumParams(phEnum, mb, rParams, cMax, pcTokens);
}

HRESULT ProfilerMetadataEmitValidator::EnumMemberRefs(
    HCORENUM    *phEnum,
    mdToken     tkParent,
    mdMemberRef rMemberRefs[],
    ULONG       cMax,
    ULONG       *pcTokens)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->EnumMemberRefs(phEnum, tkParent, rMemberRefs, cMax, pcTokens);
}

HRESULT ProfilerMetadataEmitValidator::EnumMethodImpls(
    HCORENUM    *phEnum,
    mdTypeDef   td,
    mdToken     rMethodBody[],
    mdToken     rMethodDecl[],
    ULONG       cMax,
    ULONG       *pcTokens)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->EnumMethodImpls(phEnum, td, rMethodBody, rMethodDecl, cMax, pcTokens);
}

HRESULT ProfilerMetadataEmitValidator::EnumPermissionSets(
    HCORENUM    *phEnum,
    mdToken     tk,
    DWORD       dwActions,
    mdPermission rPermission[],
    ULONG       cMax,
    ULONG       *pcTokens)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->EnumPermissionSets(phEnum, tk, dwActions, rPermission, cMax, pcTokens);
}

HRESULT ProfilerMetadataEmitValidator::FindMember(
    mdTypeDef   td,
    LPCWSTR     szName,
    PCCOR_SIGNATURE pvSigBlob,
    ULONG       cbSigBlob,
    mdToken     *pmb)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->FindMember(td, szName, pvSigBlob, cbSigBlob, pmb);
}

HRESULT ProfilerMetadataEmitValidator::FindMethod(
    mdTypeDef   td,
    LPCWSTR     szName,
    PCCOR_SIGNATURE pvSigBlob,
    ULONG       cbSigBlob,
    mdMethodDef *pmb)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->FindMethod(td, szName, pvSigBlob, cbSigBlob, pmb);
}

HRESULT ProfilerMetadataEmitValidator::FindField(
    mdTypeDef   td,
    LPCWSTR     szName,
    PCCOR_SIGNATURE pvSigBlob,
    ULONG       cbSigBlob,
    mdFieldDef  *pmb)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->FindField(td, szName, pvSigBlob, cbSigBlob, pmb);
}

HRESULT ProfilerMetadataEmitValidator::FindMemberRef(
    mdTypeRef   td,
    LPCWSTR     szName,
    PCCOR_SIGNATURE pvSigBlob,
    ULONG       cbSigBlob,
    mdMemberRef *pmr)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->FindMemberRef(td, szName, pvSigBlob, cbSigBlob, pmr);
}

HRESULT ProfilerMetadataEmitValidator::GetMethodProps(
    mdMethodDef mb,
    mdTypeDef   *pClass,
    LPWSTR      szMethod,
    ULONG       cchMethod,
    ULONG       *pchMethod,
    DWORD       *pdwAttr,
    PCCOR_SIGNATURE *ppvSigBlob,
    ULONG       *pcbSigBlob,
    ULONG       *pulCodeRVA,
    DWORD       *pdwImplFlags)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetMethodProps(mb, pClass, szMethod, cchMethod, pchMethod, pdwAttr, ppvSigBlob, pcbSigBlob, pulCodeRVA, pdwImplFlags);
}

HRESULT ProfilerMetadataEmitValidator::GetMemberRefProps(
    mdMemberRef mr,
    mdToken     *ptk,
    LPWSTR      szMember,
    ULONG       cchMember,
    ULONG       *pchMember,
    PCCOR_SIGNATURE *ppvSigBlob,
    ULONG       *pbSig)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetMemberRefProps(mr, ptk, szMember, cchMember, pchMember, ppvSigBlob, pbSig);
}

HRESULT ProfilerMetadataEmitValidator::EnumProperties(
    HCORENUM    *phEnum,
    mdTypeDef   td,
    mdProperty  rProperties[],
    ULONG       cMax,
    ULONG       *pcProperties)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->EnumProperties(phEnum, td, rProperties, cMax, pcProperties);
}

HRESULT ProfilerMetadataEmitValidator::EnumEvents(
    HCORENUM    *phEnum,
    mdTypeDef   td,
    mdEvent     rEvents[],
    ULONG       cMax,
    ULONG       *pcEvents)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->EnumEvents(phEnum, td, rEvents, cMax, pcEvents);
}

HRESULT ProfilerMetadataEmitValidator::GetEventProps(
    mdEvent     ev,
    mdTypeDef   *pClass,
    LPCWSTR     szEvent,
    ULONG       cchEvent,
    ULONG       *pchEvent,
    DWORD       *pdwEventFlags,
    mdToken     *ptkEventType,
    mdMethodDef *pmdAddOn,
    mdMethodDef *pmdRemoveOn,
    mdMethodDef *pmdFire,
    mdMethodDef rmdOtherMethod[],
    ULONG       cMax,
    ULONG       *pcOtherMethod)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetEventProps(ev, pClass, szEvent, cchEvent, pchEvent, pdwEventFlags, ptkEventType, pmdAddOn, pmdRemoveOn, pmdFire, rmdOtherMethod, cMax, pcOtherMethod);
}

HRESULT ProfilerMetadataEmitValidator::EnumMethodSemantics(
    HCORENUM    *phEnum,
    mdMethodDef mb,
    mdToken     rEventProp[],
    ULONG       cMax,
    ULONG       *pcEventProp)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->EnumMethodSemantics(phEnum, mb, rEventProp, cMax, pcEventProp);
}

HRESULT ProfilerMetadataEmitValidator::GetMethodSemantics(
    mdMethodDef mb,
    mdToken     tkEventProp,
    DWORD       *pdwSemanticsFlags)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetMethodSemantics(mb, tkEventProp, pdwSemanticsFlags);
}

HRESULT ProfilerMetadataEmitValidator::GetClassLayout(
    mdTypeDef   td,
    DWORD       *pdwPackSize,
    COR_FIELD_OFFSET rFieldOffset[],
    ULONG       cMax,
    ULONG       *pcFieldOffset,
    ULONG       *pulClassSize)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetClassLayout(td, pdwPackSize, rFieldOffset, cMax, pcFieldOffset, pulClassSize);
}

HRESULT ProfilerMetadataEmitValidator::GetFieldMarshal(
    mdToken     tk,
    PCCOR_SIGNATURE *ppvNativeType,
    ULONG       *pcbNativeType)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetFieldMarshal(tk, ppvNativeType, pcbNativeType);
}

HRESULT ProfilerMetadataEmitValidator::GetRVA(
    mdToken     tk,
    ULONG       *pulCodeRVA,
    DWORD       *pdwImplFlags)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetRVA(tk, pulCodeRVA, pdwImplFlags);
}

HRESULT ProfilerMetadataEmitValidator::GetPermissionSetProps(
    mdPermission pm,
    DWORD       *pdwAction,
    void const  **ppvPermission,
    ULONG       *pcbPermission)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetPermissionSetProps(pm, pdwAction, ppvPermission, pcbPermission);
}

HRESULT ProfilerMetadataEmitValidator::GetSigFromToken(
    mdSignature mdSig,
    PCCOR_SIGNATURE *ppvSig,
    ULONG       *pcbSig)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetSigFromToken(mdSig, ppvSig, pcbSig);
}

HRESULT ProfilerMetadataEmitValidator::GetModuleRefProps(
    mdModuleRef mur,
    LPWSTR      szName,
    ULONG       cchName,
    ULONG       *pchName)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetModuleRefProps(mur, szName, cchName, pchName);
}

HRESULT ProfilerMetadataEmitValidator::EnumModuleRefs(
    HCORENUM    *phEnum,
    mdModuleRef rModuleRefs[],
    ULONG       cmax,
    ULONG       *pcModuleRefs)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->EnumModuleRefs(phEnum, rModuleRefs, cmax, pcModuleRefs);
}

HRESULT ProfilerMetadataEmitValidator::GetTypeSpecFromToken(
    mdTypeSpec typespec,
    PCCOR_SIGNATURE *ppvSig,
    ULONG       *pcbSig)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetTypeSpecFromToken(typespec, ppvSig, pcbSig);
}

HRESULT ProfilerMetadataEmitValidator::GetNameFromToken(
    mdToken     tk,
    MDUTF8CSTR  *pszUtf8NamePtr)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetNameFromToken(tk, pszUtf8NamePtr);
}

HRESULT ProfilerMetadataEmitValidator::EnumUnresolvedMethods(
    HCORENUM    *phEnum,
    mdToken     rMethods[],
    ULONG       cMax,
    ULONG       *pcTokens)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->EnumUnresolvedMethods(phEnum, rMethods, cMax, pcTokens);
}

HRESULT ProfilerMetadataEmitValidator::GetUserString(
    mdString    stk,
    LPWSTR      szString,
    ULONG       cchString,
    ULONG       *pchString)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetUserString(stk, szString, cchString, pchString);
}

HRESULT ProfilerMetadataEmitValidator::GetPinvokeMap(
    mdToken     tk,
    DWORD       *pdwMappingFlags,
    LPWSTR      szImportName,
    ULONG       cchImportName,
    ULONG       *pchImportName,
    mdModuleRef *pmrImportDLL)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetPinvokeMap(tk, pdwMappingFlags, szImportName, cchImportName, pchImportName, pmrImportDLL);
}

HRESULT ProfilerMetadataEmitValidator::EnumSignatures(
    HCORENUM    *phEnum,
    mdSignature rSignatures[],
    ULONG       cmax,
    ULONG       *pcSignatures)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->EnumSignatures(phEnum, rSignatures, cmax, pcSignatures);
}

HRESULT ProfilerMetadataEmitValidator::EnumTypeSpecs(
    HCORENUM    *phEnum,
    mdTypeSpec  rTypeSpecs[],
    ULONG       cmax,
    ULONG       *pcTypeSpecs)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->EnumTypeSpecs(phEnum, rTypeSpecs, cmax, pcTypeSpecs);
}

HRESULT ProfilerMetadataEmitValidator::EnumUserStrings(
    HCORENUM    *phEnum,
    mdString    rStrings[],
    ULONG       cmax,
    ULONG       *pcStrings)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->EnumUserStrings(phEnum, rStrings, cmax, pcStrings);
}

HRESULT ProfilerMetadataEmitValidator::GetParamForMethodIndex(
    mdMethodDef md,
    ULONG       ulParamSeq,
    mdParamDef  *ppd)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetParamForMethodIndex(md, ulParamSeq, ppd);
}

HRESULT ProfilerMetadataEmitValidator::EnumCustomAttributes(
    HCORENUM    *phEnum,
    mdToken     tk,
    mdToken     tkType,
    mdCustomAttribute rCustomAttributes[],
    ULONG       cMax,
    ULONG       *pcCustomAttributes)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->EnumCustomAttributes(phEnum, tk, tkType, rCustomAttributes, cMax, pcCustomAttributes);
}

HRESULT ProfilerMetadataEmitValidator::GetCustomAttributeProps(
    mdCustomAttribute cv,
    mdToken     *ptkObj,
    mdToken     *ptkType,
    void const  **ppBlob,
    ULONG       *pcbSize)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetCustomAttributeProps(cv, ptkObj, ptkType, ppBlob, pcbSize);
}

HRESULT ProfilerMetadataEmitValidator::FindTypeRef(
    mdToken     tkResolutionScope,
    LPCWSTR     szName,
    mdTypeRef   *ptr)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->FindTypeRef(tkResolutionScope, szName, ptr);
}

HRESULT ProfilerMetadataEmitValidator::GetMemberProps(
    mdToken     mb,
    mdTypeDef   *pClass,
    LPWSTR      szMember,
    ULONG       cchMember,
    ULONG       *pchMember,
    DWORD       *pdwAttr,
    PCCOR_SIGNATURE *ppvSigBlob,
    ULONG       *pcbSigBlob,
    ULONG       *pulCodeRVA,
    DWORD       *pdwImplFlags,
    DWORD       *pdwCPlusTypeFlag,
    UVCP_CONSTANT *ppValue,
    ULONG       *pcchValue)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetMemberProps(mb, pClass, szMember, cchMember, pchMember, pdwAttr, ppvSigBlob, pcbSigBlob, pulCodeRVA, pdwImplFlags, pdwCPlusTypeFlag, ppValue, pcchValue);
}

HRESULT ProfilerMetadataEmitValidator::GetFieldProps(
    mdFieldDef  mb,
    mdTypeDef   *pClass,
    LPWSTR      szField,
    ULONG       cchField,
    ULONG       *pchField,
    DWORD       *pdwAttr,
    PCCOR_SIGNATURE *ppvSigBlob,
    ULONG       *pcbSigBlob,
    DWORD       *pdwCPlusTypeFlag,
    UVCP_CONSTANT *ppValue,
    ULONG       *pcchValue)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetFieldProps(mb, pClass, szField, cchField, pchField, pdwAttr, ppvSigBlob, pcbSigBlob, pdwCPlusTypeFlag, ppValue, pcchValue);
}

HRESULT ProfilerMetadataEmitValidator::GetPropertyProps(
    mdProperty  prop,
    mdTypeDef   *pClass,
    LPCWSTR     szProperty,
    ULONG       cchProperty,
    ULONG       *pchProperty,
    DWORD       *pdwPropFlags,
    PCCOR_SIGNATURE *ppvSig,
    ULONG       *pbSig,
    DWORD       *pdwCPlusTypeFlag,
    UVCP_CONSTANT *ppDefaultValue,
    ULONG       *pcchDefaultValue,
    mdMethodDef *pmdSetter,
    mdMethodDef *pmdGetter,
    mdMethodDef rmdOtherMethod[],
    ULONG       cMax,
    ULONG       *pcOtherMethod)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetPropertyProps(prop, pClass, szProperty, cchProperty, pchProperty, pdwPropFlags, ppvSig, pbSig, pdwCPlusTypeFlag, ppDefaultValue, pcchDefaultValue, pmdSetter, pmdGetter,
        rmdOtherMethod, cMax, pcOtherMethod);
}

HRESULT ProfilerMetadataEmitValidator::GetParamProps(
    mdParamDef  tk,
    mdMethodDef *pmd,
    ULONG       *pulSequence,
    LPWSTR      szName,
    ULONG       cchName,
    ULONG       *pchName,
    DWORD       *pdwAttr,
    DWORD       *pdwCPlusTypeFlag,
    UVCP_CONSTANT *ppValue,
    ULONG       *pcchValue)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetParamProps(tk, pmd, pulSequence, szName, cchName, pchName, pdwAttr, pdwCPlusTypeFlag, ppValue, pcchValue);
}

HRESULT ProfilerMetadataEmitValidator::GetCustomAttributeByName(
    mdToken     tkObj,
    LPCWSTR     szName,
    const void  **ppData,
    ULONG       *pcbData)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetCustomAttributeByName(tkObj, szName, ppData, pcbData);
}

BOOL ProfilerMetadataEmitValidator::IsValidToken(
    mdToken     tk)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->IsValidToken(tk);
}

HRESULT ProfilerMetadataEmitValidator::GetNestedClassProps(
    mdTypeDef   tdNestedClass,
    mdTypeDef   *ptdEnclosingClass)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetNestedClassProps(tdNestedClass, ptdEnclosingClass);
}

HRESULT ProfilerMetadataEmitValidator::GetNativeCallConvFromSig(
    void const  *pvSig,
    ULONG       cbSig,
    ULONG       *pCallConv)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetNativeCallConvFromSig(pvSig, cbSig, pCallConv);
}

HRESULT ProfilerMetadataEmitValidator::IsGlobal(
    mdToken     pd,
    int         *pbGlobal)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->IsGlobal(pd, pbGlobal);
}

//IMetaDataImport2
HRESULT ProfilerMetadataEmitValidator::EnumGenericParams(
    HCORENUM    *phEnum,
    mdToken      tk,
    mdGenericParam rGenericParams[],
    ULONG       cMax,
    ULONG       *pcGenericParams)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->EnumGenericParams(phEnum, tk, rGenericParams, cMax, pcGenericParams);
}


HRESULT ProfilerMetadataEmitValidator::GetGenericParamProps(
    mdGenericParam gp,
    ULONG        *pulParamSeq,
    DWORD        *pdwParamFlags,
    mdToken      *ptOwner,
    DWORD       *reserved,
    LPWSTR       wzname,
    ULONG        cchName,
    ULONG        *pchName)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetGenericParamProps(gp, pulParamSeq, pdwParamFlags, ptOwner, reserved, wzname, cchName, pchName);
}

HRESULT ProfilerMetadataEmitValidator::GetMethodSpecProps(
    mdMethodSpec mi,
    mdToken *tkParent,
    PCCOR_SIGNATURE *ppvSigBlob,
    ULONG       *pcbSigBlob)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetMethodSpecProps(mi, tkParent, ppvSigBlob, pcbSigBlob);
}

HRESULT ProfilerMetadataEmitValidator::EnumGenericParamConstraints(
    HCORENUM    *phEnum,
    mdGenericParam tk,
    mdGenericParamConstraint rGenericParamConstraints[],
    ULONG       cMax,
    ULONG       *pcGenericParamConstraints)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->EnumGenericParamConstraints(phEnum, tk, rGenericParamConstraints, cMax, pcGenericParamConstraints);
}

HRESULT ProfilerMetadataEmitValidator::GetGenericParamConstraintProps(
    mdGenericParamConstraint gpc,
    mdGenericParam *ptGenericParam,
    mdToken      *ptkConstraintType)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetGenericParamConstraintProps(gpc, ptGenericParam, ptkConstraintType);
}

HRESULT ProfilerMetadataEmitValidator::GetPEKind(
    DWORD* pdwPEKind,
    DWORD* pdwMachine)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetPEKind(pdwPEKind, pdwMachine);
}

HRESULT ProfilerMetadataEmitValidator::GetVersionString(
    LPWSTR      pwzBuf,
    DWORD       ccBufSize,
    DWORD       *pccBufSize)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->GetVersionString(pwzBuf, ccBufSize, pccBufSize);
}

HRESULT ProfilerMetadataEmitValidator::EnumMethodSpecs(
    HCORENUM    *phEnum,
    mdToken      tk,
    mdMethodSpec rMethodSpecs[],
    ULONG       cMax,
    ULONG       *pcMethodSpecs)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerImport->EnumMethodSpecs(phEnum, tk, rMethodSpecs, cMax, pcMethodSpecs);
}


// IMetaDataAssemblyImport
HRESULT ProfilerMetadataEmitValidator::GetAssemblyProps(
    mdAssembly  mda,
    const void  **ppbPublicKey,
    ULONG       *pcbPublicKey,
    ULONG       *pulHashAlgId,
    LPWSTR  szName,
    ULONG       cchName,
    ULONG       *pchName,
    ASSEMBLYMETADATA *pMetaData,
    DWORD       *pdwAssemblyFlags)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerAssemblyImport->GetAssemblyProps(mda, ppbPublicKey, pcbPublicKey, pulHashAlgId, szName, cchName, pchName, pMetaData, pdwAssemblyFlags);
}

HRESULT ProfilerMetadataEmitValidator::GetAssemblyRefProps(
    mdAssemblyRef mdar,
    const void  **ppbPublicKeyOrToken,
    ULONG       *pcbPublicKeyOrToken,
    LPWSTR szName,
    ULONG       cchName,
    ULONG       *pchName,
    ASSEMBLYMETADATA *pMetaData,
    const void  **ppbHashValue,
    ULONG       *pcbHashValue,
    DWORD       *pdwAssemblyRefFlags)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerAssemblyImport->GetAssemblyRefProps(mdar, ppbPublicKeyOrToken, pcbPublicKeyOrToken, szName, cchName, pchName, pMetaData, ppbHashValue, pcbHashValue, pdwAssemblyRefFlags);
}

HRESULT ProfilerMetadataEmitValidator::GetFileProps(
    mdFile      mdf,
    LPWSTR      szName,
    ULONG       cchName,
    ULONG       *pchName,
    const void  **ppbHashValue,
    ULONG       *pcbHashValue,
    DWORD       *pdwFileFlags)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerAssemblyImport->GetFileProps(mdf, szName, cchName, pchName, ppbHashValue, pcbHashValue, pdwFileFlags);
}

HRESULT ProfilerMetadataEmitValidator::GetExportedTypeProps(
    mdExportedType   mdct,
    LPWSTR      szName,
    ULONG       cchName,
    ULONG       *pchName,
    mdToken     *ptkImplementation,
    mdTypeDef   *ptkTypeDef,
    DWORD       *pdwExportedTypeFlags)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerAssemblyImport->GetExportedTypeProps(mdct, szName, cchName, pchName, ptkImplementation, ptkTypeDef, pdwExportedTypeFlags);
}

HRESULT ProfilerMetadataEmitValidator::GetManifestResourceProps(
    mdManifestResource  mdmr,
    LPWSTR      szName,
    ULONG       cchName,
    ULONG       *pchName,
    mdToken     *ptkImplementation,
    DWORD       *pdwOffset,
    DWORD       *pdwResourceFlags)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerAssemblyImport->GetManifestResourceProps(mdmr, szName, cchName, pchName, ptkImplementation, pdwOffset, pdwResourceFlags);
}

HRESULT ProfilerMetadataEmitValidator::EnumAssemblyRefs(
    HCORENUM    *phEnum,
    mdAssemblyRef rAssemblyRefs[],
    ULONG       cMax,
    ULONG       *pcTokens)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerAssemblyImport->EnumAssemblyRefs(phEnum, rAssemblyRefs, cMax, pcTokens);
}

HRESULT ProfilerMetadataEmitValidator::EnumFiles(
    HCORENUM    *phEnum,
    mdFile      rFiles[],
    ULONG       cMax,
    ULONG       *pcTokens)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerAssemblyImport->EnumFiles(phEnum, rFiles, cMax, pcTokens);
}

HRESULT ProfilerMetadataEmitValidator::EnumExportedTypes(
    HCORENUM    *phEnum,
    mdExportedType   rExportedTypes[],
    ULONG       cMax,
    ULONG       *pcTokens)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerAssemblyImport->EnumExportedTypes(phEnum, rExportedTypes, cMax, pcTokens);
}

HRESULT ProfilerMetadataEmitValidator::EnumManifestResources(
    HCORENUM    *phEnum,
    mdManifestResource  rManifestResources[],
    ULONG       cMax,
    ULONG       *pcTokens)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerAssemblyImport->EnumManifestResources(phEnum, rManifestResources, cMax, pcTokens);
}

HRESULT ProfilerMetadataEmitValidator::GetAssemblyFromScope(
    mdAssembly  *ptkAssembly)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerAssemblyImport->GetAssemblyFromScope(ptkAssembly);
}

HRESULT ProfilerMetadataEmitValidator::FindExportedTypeByName(
    LPCWSTR     szName,
    mdToken     mdtExportedType,
    mdExportedType   *ptkExportedType)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerAssemblyImport->FindExportedTypeByName(szName, mdtExportedType, ptkExportedType);
}

HRESULT ProfilerMetadataEmitValidator::FindManifestResourceByName(
    LPCWSTR     szName,
    mdManifestResource *ptkManifestResource)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerAssemblyImport->FindManifestResourceByName(szName, ptkManifestResource);
}

HRESULT ProfilerMetadataEmitValidator::FindAssembliesByName(
    LPCWSTR  szAppBase,
    LPCWSTR  szPrivateBin,
    LPCWSTR  szAssemblyName,
    IUnknown *ppIUnk[],
    ULONG    cMax,
    ULONG    *pcAssemblies)
{
    LIMITED_METHOD_CONTRACT;
    return m_pInnerAssemblyImport->FindAssembliesByName(szAppBase, szPrivateBin, szAssemblyName, ppIUnk, cMax, pcAssemblies);
}
