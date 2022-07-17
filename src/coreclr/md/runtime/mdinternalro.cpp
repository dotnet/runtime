// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
//  File: MDInternalRO.CPP
//

//  Notes:
//
//
// ===========================================================================
#include "stdafx.h"
#include "mdinternalro.h"
#include "metamodelro.h"
#include "liteweightstgdb.h"
#include "corhlpr.h"
#include "../compiler/regmeta.h"
#include "caparser.h"

#ifdef FEATURE_METADATA_INTERNAL_APIS

__checkReturn
HRESULT _FillMDDefaultValue(
    BYTE        bType,
    void const *pValue,
    ULONG       cbValue,
    MDDefaultValue  *pMDDefaultValue);

#ifndef DACCESS_COMPILE
__checkReturn
HRESULT TranslateSigHelper(                 // S_OK or error.
    IMDInternalImport       *pImport,       // [IN] import scope.
    IMDInternalImport       *pAssemImport,  // [IN] import assembly scope.
    const void              *pbHashValue,   // [IN] hash value for the import assembly.
    ULONG                   cbHashValue,    // [IN] count of bytes in the hash value.
    PCCOR_SIGNATURE         pbSigBlob,      // [IN] signature in the importing scope
    ULONG                   cbSigBlob,      // [IN] count of bytes of signature
    IMetaDataAssemblyEmit   *pAssemEmit,    // [IN] assembly emit scope.
    IMetaDataEmit           *emit,          // [IN] emit interface
    CQuickBytes             *pqkSigEmit,    // [OUT] buffer to hold translated signature
    ULONG                   *pcbSig);       // [OUT] count of bytes in the translated signature
#endif //!DACCESS_COMPILE

__checkReturn
HRESULT GetInternalWithRWFormat(
    LPVOID      pData,
    ULONG       cbData,
    DWORD       flags,                  // [IN] MDInternal_OpenForRead or MDInternal_OpenForENC
    REFIID      riid,                   // [in] The interface desired.
    void        **ppIUnk);              // [out] Return interface on success.

// forward declaration
__checkReturn
HRESULT MDApplyEditAndContinue(         // S_OK or error.
    IMDInternalImport **ppIMD,          // [in, out] The metadata to be updated.
    IMDInternalImportENC *pDeltaMD);    // [in] The delta metadata.


//*****************************************************************************
// Constructor
//*****************************************************************************
MDInternalRO::MDInternalRO()
 :  m_pMethodSemanticsMap(0),
    m_cRefs(1)
{
} // MDInternalRO::MDInternalRO



//*****************************************************************************
// Destructor
//*****************************************************************************
MDInternalRO::~MDInternalRO()
{
    m_LiteWeightStgdb.Uninit();
    if (m_pMethodSemanticsMap)
        delete[] m_pMethodSemanticsMap;
    m_pMethodSemanticsMap = 0;
} // MDInternalRO::~MDInternalRO

//*****************************************************************************
// IUnknown
//*****************************************************************************
ULONG MDInternalRO::AddRef()
{
    return InterlockedIncrement(&m_cRefs);
} // MDInternalRO::AddRef

ULONG MDInternalRO::Release()
{
    ULONG cRef = InterlockedDecrement(&m_cRefs);
    if (cRef == 0)
        delete this;
    return cRef;
} // MDInternalRO::Release

__checkReturn
HRESULT MDInternalRO::QueryInterface(REFIID riid, void **ppUnk)
{
    *ppUnk = 0;

    if (riid == IID_IUnknown)
        *ppUnk = this;        // ! QI for IID_IUnknown must return MDInternalRO. ConvertRO2RW() has dependency on this.
    else if (riid == IID_IMDInternalImport)
        *ppUnk = (IMDInternalImport *)this;
    else if (riid == IID_IMDCommon)
        *ppUnk = (IMDCommon *)this;
    else
        return E_NOINTERFACE;
    AddRef();
    return S_OK;
} // MDInternalRO::QueryInterface


//*****************************************************************************
// Initialize
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::Init(
    LPVOID      pData,                  // points to meta data section in memory
    ULONG       cbData)                 // count of bytes in pData
{
    m_tdModule = COR_GLOBAL_PARENT_TOKEN;

    extern HRESULT _CallInitOnMemHelper(CLiteWeightStgdb<CMiniMd> *pStgdb, ULONG cbData, LPCVOID pData);

    return _CallInitOnMemHelper(&m_LiteWeightStgdb, cbData, (BYTE*) pData);
} // MDInternalRO::Init

#ifndef DACCESS_COMPILE
//*****************************************************************************
// Given a scope, determine whether imported from a typelib.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::TranslateSigWithScope(
    IMDInternalImport*      pAssemImport,   // [IN] import assembly scope.
    const void*             pbHashValue,    // [IN] hash value for the import assembly.
    ULONG                   cbHashValue,    // [IN] count of bytes in the hash value.
    PCCOR_SIGNATURE         pbSigBlob,      // [IN] signature in the importing scope
    ULONG                   cbSigBlob,      // [IN] count of bytes of signature
    IMetaDataAssemblyEmit*  pAssemEmit,     // [IN] assembly emit scope.
    IMetaDataEmit*          emit,           // [IN] emit interface
    CQuickBytes*            pqkSigEmit,     // [OUT] buffer to hold translated signature
    ULONG*                  pcbSig)         // [OUT] count of bytes in the translated signature
{
    return TranslateSigHelper(
                this,
                pAssemImport,
                pbHashValue,
                cbHashValue,
                pbSigBlob,
                cbSigBlob,
                pAssemEmit,
                emit,
                pqkSigEmit,
                pcbSig);
} // MDInternalRO::TranslateSigWithScope
#endif // DACCESS_COMPILE

__checkReturn
HRESULT MDInternalRO::GetTypeDefRefTokenInTypeSpec(// return S_FALSE if enclosing type does not have a token
                                                    mdTypeSpec  tkTypeSpec,             // [IN] TypeSpec token to look at
                                                    mdToken    *tkEnclosedToken)       // [OUT] The enclosed type token
{
    return m_LiteWeightStgdb.m_MiniMd.GetTypeDefRefTokenInTypeSpec(tkTypeSpec, tkEnclosedToken);
} // MDInternalRO::GetTypeDefRefTokenInTypeSpec

#ifndef DACCESS_COMPILE
//*****************************************************************************
// Given a scope, return the number of tokens in a given table
//*****************************************************************************
ULONG MDInternalRO::GetCountWithTokenKind(     // return hresult
    DWORD       tkKind)                 // [IN] pass in the kind of token.
{
    ULONG ulCount = m_LiteWeightStgdb.m_MiniMd.CommonGetRowCount(tkKind);
    if (tkKind == mdtTypeDef)
    {
        // Remove global typedef from the count of typedefs (and handle the case where there is no global typedef)
        if (ulCount > 0)
            ulCount--;
    }
    return ulCount;
} // MDInternalRO::GetCountWithTokenKind
#endif //!DACCESS_COMPILE

//*******************************************************************************
// Enumerator helpers
//*******************************************************************************


//*****************************************************************************
// enumerator init for typedef
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::EnumTypeDefInit( // return hresult
    HENUMInternal *phEnum)              // [OUT] buffer to fill for enumerator data
{
    HRESULT hr = NOERROR;

    _ASSERTE(phEnum);

    HENUMInternal::ZeroEnum(phEnum);
    phEnum->m_tkKind = mdtTypeDef;
    phEnum->m_EnumType = MDSimpleEnum;
    phEnum->m_ulCount = m_LiteWeightStgdb.m_MiniMd.getCountTypeDefs();

    // Skip over the global model typedef
    //
    // phEnum->u.m_ulCur : the current rid that is not yet enumerated
    // phEnum->u.m_ulStart : the first rid that will be returned by enumerator
    // phEnum->u.m_ulEnd : the last rid that will be returned by enumerator
    phEnum->u.m_ulStart = phEnum->u.m_ulCur = 2;
    phEnum->u.m_ulEnd = phEnum->m_ulCount + 1;
    if (phEnum->m_ulCount > 0)
        phEnum->m_ulCount --;

    return hr;
} // MDInternalRO::EnumTypeDefInit

//*****************************************************************************
// Enumerator init for MethodImpl.  The second HENUMInternal* parameter is
// only used for the R/W version of the MetaData.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::EnumMethodImplInit( // return hresult
    mdTypeDef       td,                   // [IN] TypeDef over which to scope the enumeration.
    HENUMInternal   *phEnumBody,          // [OUT] buffer to fill for enumerator data for MethodBody tokens.
    HENUMInternal   *phEnumDecl)          // [OUT] buffer to fill for enumerator data for MethodDecl tokens.
{
    return EnumInit(TBL_MethodImpl << 24, td, phEnumBody);
} // MDInternalRO::EnumMethodImplInit

//*****************************************************************************
// get the number of MethodImpls in a scope
//*****************************************************************************
ULONG MDInternalRO::EnumMethodImplGetCount(
    HENUMInternal   *phEnumBody,        // [IN] MethodBody enumerator.
    HENUMInternal   *phEnumDecl)        // [IN] MethodDecl enumerator.
{
    _ASSERTE(phEnumBody && ((phEnumBody->m_tkKind >> 24) == TBL_MethodImpl));
    return phEnumBody->m_ulCount;
} // MDInternalRO::EnumMethodImplGetCount


//*****************************************************************************
// enumerator for MethodImpl.
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRO::EnumMethodImplNext(  // return hresult
    HENUMInternal   *phEnumBody,        // [IN] input enum for MethodBody
    HENUMInternal   *phEnumDecl,        // [IN] input enum for MethodDecl
    mdToken         *ptkBody,           // [OUT] return token for MethodBody
    mdToken         *ptkDecl)           // [OUT] return token for MethodDecl
{
    HRESULT hr;
    MethodImplRec   *pRecord;

    _ASSERTE(phEnumBody && ((phEnumBody->m_tkKind >> 24) == TBL_MethodImpl));
    _ASSERTE(ptkBody && ptkDecl);

    if (phEnumBody->u.m_ulCur >= phEnumBody->u.m_ulEnd)
    {
        return S_FALSE;
    }

    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetMethodImplRecord(phEnumBody->u.m_ulCur, &pRecord));
    *ptkBody = m_LiteWeightStgdb.m_MiniMd.getMethodBodyOfMethodImpl(pRecord);
    *ptkDecl = m_LiteWeightStgdb.m_MiniMd.getMethodDeclarationOfMethodImpl(pRecord);
    phEnumBody->u.m_ulCur++;

    return S_OK;
} // MDInternalRO::EnumMethodImplNext

//*****************************************
// Reset the enumerator to the beginning.
//*****************************************
void MDInternalRO::EnumMethodImplReset(
    HENUMInternal   *phEnumBody,        // [IN] MethodBody enumerator.
    HENUMInternal   *phEnumDecl)        // [IN] MethodDecl enumerator.
{
    _ASSERTE(phEnumBody && ((phEnumBody->m_tkKind >> 24) == TBL_MethodImpl));
    _ASSERTE(phEnumBody->m_EnumType == MDSimpleEnum);

    phEnumBody->u.m_ulCur = phEnumBody->u.m_ulStart;
} // MDInternalRO::EnumMethodImplReset


//*****************************************
// Close the enumerator.
//*****************************************
void MDInternalRO::EnumMethodImplClose(
    HENUMInternal   *phEnumBody,        // [IN] MethodBody enumerator.
    HENUMInternal   *phEnumDecl)        // [IN] MethodDecl enumerator.
{
    _ASSERTE(phEnumBody && ((phEnumBody->m_tkKind >> 24) == TBL_MethodImpl));
    _ASSERTE(phEnumBody->m_EnumType == MDSimpleEnum);
} // MDInternalRO::EnumMethodImplClose


//******************************************************************************
// enumerator for global functions
//******************************************************************************
__checkReturn
HRESULT MDInternalRO::EnumGlobalFunctionsInit(  // return hresult
    HENUMInternal   *phEnum)            // [OUT] buffer to fill for enumerator data
{
    return EnumInit(mdtMethodDef, m_tdModule, phEnum);
}


//******************************************************************************
// enumerator for global Fields
//******************************************************************************
__checkReturn
HRESULT MDInternalRO::EnumGlobalFieldsInit(  // return hresult
    HENUMInternal   *phEnum)            // [OUT] buffer to fill for enumerator data
{
    return EnumInit(mdtFieldDef, m_tdModule, phEnum);
}


//*****************************************
// Enumerator initializer
//*****************************************
__checkReturn
HRESULT MDInternalRO::EnumInit(     // return S_FALSE if record not found
    DWORD       tkKind,                 // [IN] which table to work on
    mdToken     tkParent,               // [IN] token to scope the search
    HENUMInternal *phEnum)              // [OUT] the enumerator to fill
{
    HRESULT     hr = S_OK;
    ULONG       ulMax = 0;

    // Vars for query.
    _ASSERTE(phEnum);
    HENUMInternal::ZeroEnum(phEnum);

    // cache the tkKind and the scope
    phEnum->m_tkKind = TypeFromToken(tkKind);

    TypeDefRec  *pRec;

    phEnum->m_EnumType = MDSimpleEnum;

    switch (TypeFromToken(tkKind))
    {
    case mdtFieldDef:
        IfFailGo(m_LiteWeightStgdb.m_MiniMd.GetTypeDefRecord(RidFromToken(tkParent), &pRec));
        phEnum->u.m_ulStart = m_LiteWeightStgdb.m_MiniMd.getFieldListOfTypeDef(pRec);
        IfFailGo(m_LiteWeightStgdb.m_MiniMd.getEndFieldListOfTypeDef(RidFromToken(tkParent), &(phEnum->u.m_ulEnd)));
        break;

    case mdtMethodDef:
        IfFailGo(m_LiteWeightStgdb.m_MiniMd.GetTypeDefRecord(RidFromToken(tkParent), &pRec));
        phEnum->u.m_ulStart = m_LiteWeightStgdb.m_MiniMd.getMethodListOfTypeDef(pRec);
        IfFailGo(m_LiteWeightStgdb.m_MiniMd.getEndMethodListOfTypeDef(RidFromToken(tkParent), &(phEnum->u.m_ulEnd)));
        break;

    case mdtGenericParam:
        _ASSERTE(TypeFromToken(tkParent) == mdtTypeDef || TypeFromToken(tkParent) == mdtMethodDef);

        if (TypeFromToken(tkParent) != mdtTypeDef && TypeFromToken(tkParent) != mdtMethodDef)
            IfFailGo(CLDB_E_FILE_CORRUPT);

        if (TypeFromToken(tkParent) == mdtTypeDef)
        {
            IfFailGo(m_LiteWeightStgdb.m_MiniMd.getGenericParamsForTypeDef(
                RidFromToken(tkParent),
                &phEnum->u.m_ulEnd,
                &(phEnum->u.m_ulStart)));
        }
        else
        {
            IfFailGo(m_LiteWeightStgdb.m_MiniMd.getGenericParamsForMethodDef(
                RidFromToken(tkParent),
                &phEnum->u.m_ulEnd,
                &(phEnum->u.m_ulStart)));
        }
        break;

    case mdtGenericParamConstraint:
        IfFailGo(m_LiteWeightStgdb.m_MiniMd.getGenericParamConstraintsForGenericParam(
            RidFromToken(tkParent),
            &phEnum->u.m_ulEnd,
            &phEnum->u.m_ulStart));
        break;

    case mdtInterfaceImpl:
        IfFailGo(m_LiteWeightStgdb.m_MiniMd.getInterfaceImplsForTypeDef(RidFromToken(tkParent), &phEnum->u.m_ulEnd, &phEnum->u.m_ulStart));
        break;

    case mdtProperty:
        RID         ridPropertyMap;
        PropertyMapRec *pPropertyMapRec;

        // get the starting/ending rid of properties of this typedef
        IfFailGo(m_LiteWeightStgdb.m_MiniMd.FindPropertyMapFor(RidFromToken(tkParent), &ridPropertyMap));
        if (!InvalidRid(ridPropertyMap))
        {
            IfFailGo(m_LiteWeightStgdb.m_MiniMd.GetPropertyMapRecord(ridPropertyMap, &pPropertyMapRec));
            phEnum->u.m_ulStart = m_LiteWeightStgdb.m_MiniMd.getPropertyListOfPropertyMap(pPropertyMapRec);
            IfFailGo(m_LiteWeightStgdb.m_MiniMd.getEndPropertyListOfPropertyMap(ridPropertyMap, &(phEnum->u.m_ulEnd)));
            ulMax = m_LiteWeightStgdb.m_MiniMd.getCountPropertys() + 1;
            if(phEnum->u.m_ulStart == 0) phEnum->u.m_ulStart = 1;
            if(phEnum->u.m_ulEnd > ulMax) phEnum->u.m_ulEnd = ulMax;
            if(phEnum->u.m_ulStart > phEnum->u.m_ulEnd) phEnum->u.m_ulStart = phEnum->u.m_ulEnd;
        }
        break;

    case mdtEvent:
        RID         ridEventMap;
        EventMapRec *pEventMapRec;

        // get the starting/ending rid of events of this typedef
        IfFailGo(m_LiteWeightStgdb.m_MiniMd.FindEventMapFor(RidFromToken(tkParent), &ridEventMap));
        if (!InvalidRid(ridEventMap))
        {
            IfFailGo(m_LiteWeightStgdb.m_MiniMd.GetEventMapRecord(ridEventMap, &pEventMapRec));
            phEnum->u.m_ulStart = m_LiteWeightStgdb.m_MiniMd.getEventListOfEventMap(pEventMapRec);
            IfFailGo(m_LiteWeightStgdb.m_MiniMd.getEndEventListOfEventMap(ridEventMap, &(phEnum->u.m_ulEnd)));
            ulMax = m_LiteWeightStgdb.m_MiniMd.getCountEvents() + 1;
            if(phEnum->u.m_ulStart == 0) phEnum->u.m_ulStart = 1;
            if(phEnum->u.m_ulEnd > ulMax) phEnum->u.m_ulEnd = ulMax;
            if(phEnum->u.m_ulStart > phEnum->u.m_ulEnd) phEnum->u.m_ulStart = phEnum->u.m_ulEnd;
        }
        break;

    case mdtParamDef:
        _ASSERTE(TypeFromToken(tkParent) == mdtMethodDef);

        MethodRec *pMethodRec;
        IfFailGo(m_LiteWeightStgdb.m_MiniMd.GetMethodRecord(RidFromToken(tkParent), &pMethodRec));

        // figure out the start rid and end rid of the parameter list of this methoddef
        phEnum->u.m_ulStart = m_LiteWeightStgdb.m_MiniMd.getParamListOfMethod(pMethodRec);
        IfFailGo(m_LiteWeightStgdb.m_MiniMd.getEndParamListOfMethod(RidFromToken(tkParent), &(phEnum->u.m_ulEnd)));
        break;
    case mdtCustomAttribute:
        IfFailGo(m_LiteWeightStgdb.m_MiniMd.getCustomAttributeForToken(tkParent, &phEnum->u.m_ulEnd, &phEnum->u.m_ulStart));
        break;
    case mdtAssemblyRef:
        _ASSERTE(IsNilToken(tkParent));
        phEnum->u.m_ulStart = 1;
        phEnum->u.m_ulEnd = m_LiteWeightStgdb.m_MiniMd.getCountAssemblyRefs() + 1;
        break;
    case mdtFile:
        _ASSERTE(IsNilToken(tkParent));
        phEnum->u.m_ulStart = 1;
        phEnum->u.m_ulEnd = m_LiteWeightStgdb.m_MiniMd.getCountFiles() + 1;
        break;
    case mdtExportedType:
        _ASSERTE(IsNilToken(tkParent));
        phEnum->u.m_ulStart = 1;
        phEnum->u.m_ulEnd = m_LiteWeightStgdb.m_MiniMd.getCountExportedTypes() + 1;
        break;
    case mdtManifestResource:
        _ASSERTE(IsNilToken(tkParent));
        phEnum->u.m_ulStart = 1;
        phEnum->u.m_ulEnd = m_LiteWeightStgdb.m_MiniMd.getCountManifestResources() + 1;
        break;
    case mdtModuleRef:
        _ASSERTE(IsNilToken(tkParent));
        phEnum->u.m_ulStart = 1;
        phEnum->u.m_ulEnd = m_LiteWeightStgdb.m_MiniMd.getCountModuleRefs() + 1;
        break;
    case (TBL_MethodImpl << 24):
        _ASSERTE(! IsNilToken(tkParent));
        IfFailGo(m_LiteWeightStgdb.m_MiniMd.getMethodImplsForClass(
            RidFromToken(tkParent),
            &phEnum->u.m_ulEnd,
            &phEnum->u.m_ulStart));
        break;
    default:
        _ASSERTE(!"ENUM INIT not implemented for the compressed format!");
        IfFailGo(E_NOTIMPL);
        break;
    }

    // If the count is negative, the metadata is corrupted somehow.
    if (phEnum->u.m_ulEnd < phEnum->u.m_ulStart)
        IfFailGo(CLDB_E_FILE_CORRUPT);

    phEnum->m_ulCount = phEnum->u.m_ulEnd - phEnum->u.m_ulStart;
    phEnum->u.m_ulCur = phEnum->u.m_ulStart;

ErrExit:
    // we are done
    return hr;
}


//*****************************************
// Enumerator initializer
//*****************************************
__checkReturn
HRESULT MDInternalRO::EnumAllInit(      // return S_FALSE if record not found
    DWORD       tkKind,                 // [IN] which table to work on
    HENUMInternal *phEnum)              // [OUT] the enumerator to fill
{
    HRESULT hr = S_OK;

    // Vars for query.
    _ASSERTE(phEnum);
    HENUMInternal::ZeroEnum(phEnum);

    // cache the tkKind and the scope
    phEnum->m_tkKind = TypeFromToken(tkKind);
    phEnum->m_EnumType = MDSimpleEnum;

    switch (TypeFromToken(tkKind))
    {
    case mdtTypeRef:
        phEnum->m_ulCount = m_LiteWeightStgdb.m_MiniMd.getCountTypeRefs();
        break;

    case mdtMemberRef:
        phEnum->m_ulCount = m_LiteWeightStgdb.m_MiniMd.getCountMemberRefs();
        break;

    case mdtSignature:
        phEnum->m_ulCount = m_LiteWeightStgdb.m_MiniMd.getCountStandAloneSigs();
        break;

    case mdtMethodDef:
        phEnum->m_ulCount = m_LiteWeightStgdb.m_MiniMd.getCountMethods();
        break;

    case mdtMethodSpec:
        phEnum->m_ulCount = m_LiteWeightStgdb.m_MiniMd.getCountMethodSpecs();
        break;

    case mdtFieldDef:
        phEnum->m_ulCount = m_LiteWeightStgdb.m_MiniMd.getCountFields();
        break;

    case mdtTypeSpec:
        phEnum->m_ulCount = m_LiteWeightStgdb.m_MiniMd.getCountTypeSpecs();
        break;

    case mdtAssemblyRef:
        phEnum->m_ulCount = m_LiteWeightStgdb.m_MiniMd.getCountAssemblyRefs();
        break;

    case mdtModuleRef:
        phEnum->m_ulCount = m_LiteWeightStgdb.m_MiniMd.getCountModuleRefs();
        break;

    case mdtTypeDef:
        phEnum->m_ulCount = m_LiteWeightStgdb.m_MiniMd.getCountTypeDefs();
        break;

    case mdtFile:
        phEnum->m_ulCount = m_LiteWeightStgdb.m_MiniMd.getCountFiles();
        break;

    case mdtCustomAttribute:
        phEnum->m_ulCount = m_LiteWeightStgdb.m_MiniMd.getCountCustomAttributes();
        break;

    default:
        _ASSERTE(!"Bad token kind!");
        break;
    }
    phEnum->u.m_ulStart = phEnum->u.m_ulCur = 1;
    phEnum->u.m_ulEnd = phEnum->m_ulCount + 1;

    // we are done
    return hr;
} // MDInternalRO::EnumAllInit

//*****************************************
// Enumerator initializer for CustomAttributes
//*****************************************
__checkReturn
HRESULT MDInternalRO::EnumCustomAttributeByNameInit(// return S_FALSE if record not found
    mdToken     tkParent,               // [IN] token to scope the search
    LPCSTR      szName,                 // [IN] CustomAttribute's name to scope the search
    HENUMInternal *phEnum)              // [OUT] the enumerator to fill
{
    return m_LiteWeightStgdb.m_MiniMd.CommonEnumCustomAttributeByName(tkParent, szName, false, phEnum);
}   // MDInternalRO::EnumCustomAttributeByNameInit

//*****************************************
// Nagivator helper to navigate back to the parent token given a token.
// For example, given a memberdef token, it will return the containing typedef.
//
// the mapping is as following:
//  ---given child type---------parent type
//  mdMethodDef                 mdTypeDef
//  mdFieldDef                  mdTypeDef
//  mdInterfaceImpl             mdTypeDef
//  mdParam                     mdMethodDef
//  mdProperty                  mdTypeDef
//  mdEvent                     mdTypeDef
//
//*****************************************
__checkReturn
HRESULT MDInternalRO::GetParentToken(
    mdToken     tkChild,                // [IN] given child token
    mdToken     *ptkParent)             // [OUT] returning parent
{
    HRESULT hr = NOERROR;

    _ASSERTE(ptkParent);

    switch (TypeFromToken(tkChild))
    {
    case mdtTypeDef:
        hr = GetNestedClassProps(tkChild, ptkParent);
        // If not found, the *ptkParent has to be left unchanged! (callers depend on that)
        if (hr == CLDB_E_RECORD_NOTFOUND)
        {
            hr = S_OK;
        }
        break;

    case mdtMethodDef:
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.FindParentOfMethod(RidFromToken(tkChild), (RID *)ptkParent));
        RidToToken(*ptkParent, mdtTypeDef);
        break;

    case mdtMethodSpec:
        {
            MethodSpecRec    *pRec;
            IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetMethodSpecRecord(RidFromToken(tkChild), &pRec));
            *ptkParent = m_LiteWeightStgdb.m_MiniMd.getMethodOfMethodSpec(pRec);
            break;
        }

    case mdtFieldDef:
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.FindParentOfField(RidFromToken(tkChild), (RID *)ptkParent));
        RidToToken(*ptkParent, mdtTypeDef);
        break;

    case mdtParamDef:
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.FindParentOfParam(RidFromToken(tkChild), (RID *)ptkParent));
        RidToToken(*ptkParent, mdtMethodDef);
        break;

    case mdtMemberRef:
        {
            MemberRefRec    *pRec;
            IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetMemberRefRecord(RidFromToken(tkChild), &pRec));
            *ptkParent = m_LiteWeightStgdb.m_MiniMd.getClassOfMemberRef(pRec);
            break;
        }

    case mdtCustomAttribute:
        {
            CustomAttributeRec    *pRec;
            IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetCustomAttributeRecord(RidFromToken(tkChild), &pRec));
            *ptkParent = m_LiteWeightStgdb.m_MiniMd.getParentOfCustomAttribute(pRec);
            break;
        }

    case mdtEvent:
        hr = m_LiteWeightStgdb.m_MiniMd.FindParentOfEventHelper(tkChild, ptkParent);
        break;

    case mdtProperty:
        hr = m_LiteWeightStgdb.m_MiniMd.FindParentOfPropertyHelper(tkChild, ptkParent);
        break;

    default:
        _ASSERTE(!"NYI: for compressed format!");
        break;
    }
    return hr;
} // MDInternalRO::GetParentToken



//*****************************************************************************
// Get information about a CustomAttribute.
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRO::GetCustomAttributeProps(  // S_OK or error.
    mdCustomAttribute at,               // The attribute.
    mdToken     *ptkType)               // Put attribute type here.
{
    HRESULT hr;
    _ASSERTE(TypeFromToken(at) == mdtCustomAttribute);

    // Do a linear search on compressed version as we do not want to
    // depends on ICR.
    //
    CustomAttributeRec *pCustomAttributeRec;

    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetCustomAttributeRecord(RidFromToken(at), &pCustomAttributeRec));
    *ptkType = m_LiteWeightStgdb.m_MiniMd.getTypeOfCustomAttribute(pCustomAttributeRec);
    return S_OK;
} // MDInternalRO::GetCustomAttributeProps

//*****************************************************************************
// return custom value
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRO::GetCustomAttributeAsBlob(
    mdCustomAttribute cv,               // [IN] given custom attribute token
    void const  **ppBlob,               // [OUT] return the pointer to internal blob
    ULONG       *pcbSize)               // [OUT] return the size of the blob
{
    HRESULT hr;
    _ASSERTE(ppBlob && pcbSize && TypeFromToken(cv) == mdtCustomAttribute);

    CustomAttributeRec *pCustomAttributeRec;

    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetCustomAttributeRecord(RidFromToken(cv), &pCustomAttributeRec));

    IfFailRet(m_LiteWeightStgdb.m_MiniMd.getValueOfCustomAttribute(pCustomAttributeRec, (const BYTE **)ppBlob, pcbSize));
    return S_OK;
} // MDInternalRO::GetCustomAttributeAsBlob

//*****************************************************************************
// Helper function to lookup and retrieve a CustomAttribute.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::GetCustomAttributeByName( // S_OK or error.
    mdToken     tkObj,                  // [IN] Object with Custom Attribute.
    LPCUTF8     szName,                 // [IN] Name of desired Custom Attribute.
    _Outptr_result_bytebuffer_(*pcbData) const void  **ppData, // [OUT] Put pointer to data here.
    _Out_ ULONG *pcbData)               // [OUT] Put size of data here.
{
    return m_LiteWeightStgdb.m_MiniMd.CommonGetCustomAttributeByNameEx(tkObj, szName, NULL, ppData, pcbData);
} // MDInternalRO::GetCustomAttributeByName


//*****************************************************************************
// return the name of a custom attribute
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::GetNameOfCustomAttribute( // S_OK or error.
    mdCustomAttribute mdAttribute,      // [IN] The Custom Attribute
    LPCUTF8          *pszNamespace,     // [OUT] Namespace of Custom Attribute.
    LPCUTF8          *pszName)          // [OUT] Name of  Custom Attribute.
{
    _ASSERTE(TypeFromToken(mdAttribute) == mdtCustomAttribute);

    HRESULT hr = m_LiteWeightStgdb.m_MiniMd.CommonGetNameOfCustomAttribute(RidFromToken(mdAttribute), pszNamespace, pszName);
    return (hr == S_FALSE) ? E_FAIL : hr;
} // MDInternalRO::GetNameOfCustomAttribute

//*****************************************************************************
// return scope properties
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRO::GetScopeProps(
    LPCSTR *pszName,    // [OUT] scope name
    GUID   *pmvid)      // [OUT] version id
{
    HRESULT hr;

    ModuleRec *pModuleRec;

    // there is only one module record
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetModuleRecord(1, &pModuleRec));

    if (pmvid != NULL)
    {
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.getMvidOfModule(pModuleRec, pmvid));
    }
    if (pszName != NULL)
    {
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.getNameOfModule(pModuleRec, pszName));
    }

    return S_OK;
} // MDInternalRO::GetScopeProps


//*****************************************************************************
// Compare two signatures from the same scope. Varags signatures need to be
// preprocessed so they only contain the fixed part.
//*****************************************************************************
BOOL  MDInternalRO::CompareSignatures(PCCOR_SIGNATURE           pvFirstSigBlob,       // First signature
                                      DWORD                     cbFirstSigBlob,       //
                                      PCCOR_SIGNATURE           pvSecondSigBlob,      // Second signature
                                      DWORD                     cbSecondSigBlob,      //
                                      void *                    SigArguments)         // No additional arguments required
{
    if (cbFirstSigBlob != cbSecondSigBlob || memcmp(pvFirstSigBlob, pvSecondSigBlob, cbSecondSigBlob))
        return FALSE;
    else
        return TRUE;
}

//*****************************************************************************
// Find a given member in a TypeDef (typically a class).
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::FindMethodDef(    // S_OK or error.
    mdTypeDef   classdef,               // The owning class of the member.
    LPCSTR      szName,                 // Name of the member in utf8.
    PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of COM+ signature
    ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
    mdMethodDef *pmethoddef)            // Put MemberDef token here.
{

    return FindMethodDefUsingCompare(classdef,
                                     szName,
                                     pvSigBlob,
                                     cbSigBlob,
                                     CompareSignatures,
                                     NULL,
                                     pmethoddef);
}

//*****************************************************************************
// Find a given member in a TypeDef (typically a class).
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::FindMethodDefUsingCompare(    // S_OK or error.
    mdTypeDef   classdef,               // The owning class of the member.
    LPCSTR      szName,                 // Name of the member in utf8.
    PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of COM+ signature
    ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
    PSIGCOMPARE SigCompare,            // [IN] Signature comparison routine
    void*       pSigArgs,               // [IN] Additional arguments passed to signature compare
    mdMethodDef *pmethoddef)            // Put MemberDef token here.
{
    HRESULT     hr = NOERROR;
    PCCOR_SIGNATURE pvSigTemp = pvSigBlob;
    CQuickBytes qbSig;

    _ASSERTE(szName && pmethoddef);

    // initialize the output parameter
    *pmethoddef = mdMethodDefNil;

    // check to see if this is a vararg signature
    if ( isCallConv(CorSigUncompressCallingConv(pvSigTemp), IMAGE_CEE_CS_CALLCONV_VARARG) )
    {
        // Get the fix part of VARARG signature
        IfFailGo( _GetFixedSigOfVarArg(pvSigBlob, cbSigBlob, &qbSig, &cbSigBlob) );
        pvSigBlob = (PCCOR_SIGNATURE) qbSig.Ptr();
    }

    // Do a linear search on compressed version
    //
    RID         ridMax;
    MethodRec   *pMethodRec;
    LPCUTF8     szCurMethodName;
    void const  *pvCurMethodSig;
    ULONG       cbSig;
    TypeDefRec  *pRec;
    RID         ridStart;

    // get the typedef record
    IfFailGo(m_LiteWeightStgdb.m_MiniMd.GetTypeDefRecord(RidFromToken(classdef), &pRec));

    // get the range of methoddef rids given the classdef
    ridStart = m_LiteWeightStgdb.m_MiniMd.getMethodListOfTypeDef(pRec);
    IfFailGo(m_LiteWeightStgdb.m_MiniMd.getEndMethodListOfTypeDef(RidFromToken(classdef), &ridMax));

    // loop through each methoddef
    for (; ridStart < ridMax; ridStart++)
    {
        IfFailGo(m_LiteWeightStgdb.m_MiniMd.GetMethodRecord(ridStart, &pMethodRec));
        IfFailGo(m_LiteWeightStgdb.m_MiniMd.getNameOfMethod(pMethodRec, &szCurMethodName));
        if (strcmp(szCurMethodName, szName) == 0)
        {
            // name match, now check the signature if specified.
            if (cbSigBlob && SigCompare)
            {
                IfFailGo(m_LiteWeightStgdb.m_MiniMd.getSignatureOfMethod(pMethodRec, (PCCOR_SIGNATURE *)&pvCurMethodSig, &cbSig));
                // Signature comparison is required
                // Note that if pvSigBlob is vararg, we already preprocess it so that
                // it only contains the fix part. Therefore, it still should be an exact
                // match!!!.
                //
                if(SigCompare((PCCOR_SIGNATURE) pvCurMethodSig, cbSig, pvSigBlob, cbSigBlob, pSigArgs) == FALSE)
                    continue;
            }
            // Ignore PrivateScope methods.
            if (IsMdPrivateScope(m_LiteWeightStgdb.m_MiniMd.getFlagsOfMethod(pMethodRec)))
               continue;
                    // found the match
                    *pmethoddef = TokenFromRid(ridStart, mdtMethodDef);
                    goto ErrExit;
                }
            }
    hr = CLDB_E_RECORD_NOTFOUND;

ErrExit:
    return hr;
}

//*****************************************************************************
// Find a given param of a Method.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::FindParamOfMethod(// S_OK or error.
    mdMethodDef md,                     // [IN] The owning method of the param.
    ULONG       iSeq,                   // [IN] The sequence # of the param.
    mdParamDef  *pparamdef)             // [OUT] Put ParamDef token here.
{
    HRESULT   hr;
    ParamRec *pParamRec;
    RID       ridStart, ridEnd;

    _ASSERTE(TypeFromToken(md) == mdtMethodDef && pparamdef);

    // get the methoddef record
    MethodRec *pMethodRec;
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetMethodRecord(RidFromToken(md), &pMethodRec));

    // figure out the start rid and end rid of the parameter list of this methoddef
    ridStart = m_LiteWeightStgdb.m_MiniMd.getParamListOfMethod(pMethodRec);
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.getEndParamListOfMethod(RidFromToken(md), &ridEnd));

    // Ensure that the paramList is valid. If the count is negative, the metadata
    // is corrupted somehow. Thus, return CLDB_E_FILE_CORRUPT.
    if (ridEnd < ridStart)
        return CLDB_E_FILE_CORRUPT;

    // loop through each param
    //<TODO>@consider: parameters are sorted by sequence. Maybe a binary search?
    //</TODO>
    for (; ridStart < ridEnd; ridStart++)
    {
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetParamRecord(ridStart, &pParamRec));
        if (iSeq == m_LiteWeightStgdb.m_MiniMd.getSequenceOfParam(pParamRec))
        {
            // parameter has the sequence number matches what we are looking for
            *pparamdef = TokenFromRid(ridStart, mdtParamDef);
            return S_OK;
        }
    }
    return CLDB_E_RECORD_NOTFOUND;
}



//*****************************************************************************
// return a pointer which points to meta data's internal string
// return the type name in utf8
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRO::GetNameOfTypeDef(     // return hresult
    mdTypeDef   classdef,           // given typedef
    LPCSTR*     pszname,            // pointer to an internal UTF8 string
    LPCSTR*     psznamespace)       // pointer to the namespace.
{
    HRESULT hr;

    if (pszname != NULL)
    {
        *pszname = NULL;
    }
    if (psznamespace != NULL)
    {
        *psznamespace = NULL;
    }

    if (TypeFromToken(classdef) == mdtTypeDef)
    {
        TypeDefRec *pTypeDefRec;
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetTypeDefRecord(RidFromToken(classdef), &pTypeDefRec));

        if (pszname != NULL)
        {
            IfFailRet(m_LiteWeightStgdb.m_MiniMd.getNameOfTypeDef(pTypeDefRec, pszname));
        }

        if (psznamespace != NULL)
        {
            IfFailRet(m_LiteWeightStgdb.m_MiniMd.getNamespaceOfTypeDef(pTypeDefRec, psznamespace));
        }
        return S_OK;
    }

    _ASSERTE(!"Invalid argument(s) of GetNameOfTypeDef");
    return CLDB_E_INTERNALERROR;
} // MDInternalRO::GetNameOfTypeDef


__checkReturn
HRESULT MDInternalRO::GetIsDualOfTypeDef(// return hresult
    mdTypeDef   classdef,               // given classdef
    ULONG       *pDual)                 // [OUT] return dual flag here.
{
    ULONG       iFace=0;                // Iface type.
    HRESULT     hr;                     // A result.

    hr = GetIfaceTypeOfTypeDef(classdef, &iFace);
    if (hr == S_OK)
        *pDual = (iFace == ifDual);
    else
        *pDual = 1;

    return hr;
} // MDInternalRO::GetIsDualOfTypeDef

__checkReturn
HRESULT MDInternalRO::GetIfaceTypeOfTypeDef(
    mdTypeDef   classdef,               // [IN] given classdef.
    ULONG       *pIface)                // [OUT] 0=dual, 1=vtable, 2=dispinterface
{
    HRESULT     hr;                     // A result.
    const BYTE  *pVal;                  // The custom value.
    ULONG       cbVal;                  // Size of the custom value.
    ULONG       ItfType = DEFAULT_COM_INTERFACE_TYPE;    // Set the interface type to the default.

    // If the value is not present, the class is assumed dual.
    hr = GetCustomAttributeByName(classdef, INTEROP_INTERFACETYPE_TYPE, (const void**)&pVal, &cbVal);
    if (hr == S_OK)
    {
        CustomAttributeParser cap(pVal, cbVal);
        BYTE u1;
        if (SUCCEEDED(cap.SkipProlog()) &&
            SUCCEEDED(cap.GetU1(&u1)))
        {
            ItfType = u1;
        }
        if (ItfType >= ifLast)
            ItfType = DEFAULT_COM_INTERFACE_TYPE;
    }

    // Set the return value.
    *pIface = ItfType;

    return hr;
} // MDInternalRO::GetIfaceTypeOfTypeDef

//*****************************************************************************
// Given a methoddef, return a pointer to methoddef's name
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRO::GetNameOfMethodDef(
    mdMethodDef md,
    LPCSTR     *pszMethodName)
{
    HRESULT hr;
    MethodRec *pMethodRec;
    *pszMethodName = NULL;
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetMethodRecord(RidFromToken(md), &pMethodRec));
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.getNameOfMethod(pMethodRec, pszMethodName));
    return S_OK;
} // MDInternalRO::GetNameOfMethodDef

//*****************************************************************************
// Given a methoddef, return a pointer to methoddef's signature and methoddef's name
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRO::GetNameAndSigOfMethodDef(
    mdMethodDef      methoddef,         // [IN] given memberdef
    PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to a blob value of COM+ signature
    ULONG           *pcbSigBlob,        // [OUT] count of bytes in the signature blob
    LPCSTR          *pszMethodName)
{
    HRESULT hr;
    // Output parameter should not be NULL
    _ASSERTE(ppvSigBlob && pcbSigBlob);
    _ASSERTE(TypeFromToken(methoddef) == mdtMethodDef);

    MethodRec *pMethodRec;
    *pszMethodName = NULL;
    *ppvSigBlob = NULL;
    *pcbSigBlob = 0;
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetMethodRecord(RidFromToken(methoddef), &pMethodRec));
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.getSignatureOfMethod(pMethodRec, (PCCOR_SIGNATURE *)ppvSigBlob, pcbSigBlob));

    return GetNameOfMethodDef(methoddef, pszMethodName);
} // MDInternalRO::GetNameAndSigOfMethodDef

//*****************************************************************************
// Given a FieldDef, return a pointer to FieldDef's name in UTF8
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRO::GetNameOfFieldDef(    // return hresult
    mdFieldDef fd,                  // given field
    LPCSTR    *pszFieldName)
{
    HRESULT hr;
    FieldRec *pFieldRec;
    *pszFieldName = NULL;
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetFieldRecord(RidFromToken(fd), &pFieldRec));
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.getNameOfField(pFieldRec, pszFieldName));
    return S_OK;
} // MDInternalRO::GetNameOfFieldDef


//*****************************************************************************
// Given a classdef, return the name and namespace of the typeref
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRO::GetNameOfTypeRef(     // return TypeDef's name
    mdTypeRef   classref,           // [IN] given typeref
    LPCSTR      *psznamespace,      // [OUT] return typeref name
    LPCSTR      *pszname)           // [OUT] return typeref namespace

{
    _ASSERTE(TypeFromToken(classref) == mdtTypeRef);

    HRESULT hr;
    TypeRefRec *pTypeRefRec;

    *psznamespace = NULL;
    *pszname = NULL;

    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetTypeRefRecord(RidFromToken(classref), &pTypeRefRec));
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.getNamespaceOfTypeRef(pTypeRefRec, psznamespace));
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.getNameOfTypeRef(pTypeRefRec, pszname));
    return S_OK;
}

//*****************************************************************************
// return the resolutionscope of typeref
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRO::GetResolutionScopeOfTypeRef(
    mdTypeRef classref,               // given classref
    mdToken  *ptkResolutionScope)
{
    _ASSERTE(TypeFromToken(classref) == mdtTypeRef && RidFromToken(classref));
    HRESULT hr;

    TypeRefRec *pTypeRefRec;

    *ptkResolutionScope = mdTokenNil;
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetTypeRefRecord(RidFromToken(classref), &pTypeRefRec));
    *ptkResolutionScope = m_LiteWeightStgdb.m_MiniMd.getResolutionScopeOfTypeRef(pTypeRefRec);
    return S_OK;
} // MDInternalRO::GetResolutionScopeOfTypeRef

//*****************************************************************************
// Given a name, find the corresponding TypeRef.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::FindTypeRefByName(  // S_OK or error.
        LPCSTR      szNamespace,            // [IN] Namespace for the TypeRef.
        LPCSTR      szName,                 // [IN] Name of the TypeRef.
        mdToken     tkResolutionScope,      // [IN] Resolution Scope fo the TypeRef.
        mdTypeRef   *ptk)                   // [OUT] TypeRef token returned.
{
    HRESULT     hr = NOERROR;

    _ASSERTE(ptk);

    // initialize the output parameter
    *ptk = mdTypeRefNil;

    // Treat no namespace as empty string.
    if (!szNamespace)
        szNamespace = "";

    // Do a linear search on compressed version as we do not want to
    // depends on ICR.
    //
    ULONG       cTypeRefRecs = m_LiteWeightStgdb.m_MiniMd.getCountTypeRefs();
    TypeRefRec *pTypeRefRec;
    LPCUTF8     szNamespaceTmp;
    LPCUTF8     szNameTmp;
    mdToken     tkRes;

    for (ULONG i = 1; i <= cTypeRefRecs; i++)
    {
        IfFailGo(m_LiteWeightStgdb.m_MiniMd.GetTypeRefRecord(i, &pTypeRefRec));
        tkRes = m_LiteWeightStgdb.m_MiniMd.getResolutionScopeOfTypeRef(pTypeRefRec);

        if (IsNilToken(tkRes))
        {
            if (!IsNilToken(tkResolutionScope))
                continue;
        }
        else if (tkRes != tkResolutionScope)
            continue;

        IfFailGo(m_LiteWeightStgdb.m_MiniMd.getNamespaceOfTypeRef(pTypeRefRec, &szNamespaceTmp));
        if (strcmp(szNamespace, szNamespaceTmp))
            continue;

        IfFailGo(m_LiteWeightStgdb.m_MiniMd.getNameOfTypeRef(pTypeRefRec, &szNameTmp));
        if (!strcmp(szNameTmp, szName))
        {
            *ptk = TokenFromRid(i, mdtTypeRef);
            goto ErrExit;
        }
    }

    // cannot find the typedef
    hr = CLDB_E_RECORD_NOTFOUND;
ErrExit:
    return hr;
}

//*****************************************************************************
// return flags for a given class
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::GetTypeDefProps(
    mdTypeDef   td,                     // given classdef
    DWORD       *pdwAttr,               // return flags on class
    mdToken     *ptkExtends)            // [OUT] Put base class TypeDef/TypeRef here.
{
    HRESULT hr;
    TypeDefRec *pTypeDefRec;
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetTypeDefRecord(RidFromToken(td), &pTypeDefRec));

    if (ptkExtends)
    {
        *ptkExtends = m_LiteWeightStgdb.m_MiniMd.getExtendsOfTypeDef(pTypeDefRec);
    }
    if (pdwAttr)
    {
        *pdwAttr = m_LiteWeightStgdb.m_MiniMd.getFlagsOfTypeDef(pTypeDefRec);
    }

    return S_OK;
}


//*****************************************************************************
// return guid pointer to MetaData internal guid pool given a given class
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::GetItemGuid(      // return hresult
    mdToken     tkObj,                  // given item
    CLSID       *pGuid)
{

    HRESULT     hr;                     // A result.
    const BYTE  *pBlob = NULL;          // Blob with dispid.
    ULONG       cbBlob;                 // Length of blob.
    int         ix;                     // Loop control.

    // Get the GUID, if any.
    hr = GetCustomAttributeByName(tkObj, INTEROP_GUID_TYPE, (const void**)&pBlob, &cbBlob);
    if (hr != S_FALSE)
    {
        // Should be in format.  Total length == 41
        // <0x0001><0x24>01234567-0123-0123-0123-001122334455<0x0000>
        if ((cbBlob != 41) || (GET_UNALIGNED_VAL16(pBlob) != 1))
            IfFailGo(E_INVALIDARG);

        WCHAR wzBlob[40];             // Wide char format of guid.
        for (ix=1; ix<=36; ++ix)
            wzBlob[ix] = pBlob[ix+2];
        wzBlob[0] = '{';
        wzBlob[37] = '}';
        wzBlob[38] = 0;
        hr = IIDFromString(wzBlob, pGuid);
    }
    else
        *pGuid = GUID_NULL;

ErrExit:
    return hr;
} // MDInternalRO::GetItemGuid


//*****************************************************************************
// // get enclosing class of NestedClass
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::GetNestedClassProps(  // S_OK or error
    mdTypeDef   tkNestedClass,      // [IN] NestedClass token.
    mdTypeDef   *ptkEnclosingClass) // [OUT] EnclosingClass token.
{
    HRESULT hr;
    _ASSERTE(TypeFromToken(tkNestedClass) == mdtTypeDef && ptkEnclosingClass);

    RID rid;
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.FindNestedClassFor(RidFromToken(tkNestedClass), &rid));

    if (InvalidRid(rid))
    {
        return CLDB_E_RECORD_NOTFOUND;
    }
    else
    {
        NestedClassRec *pRecord;
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetNestedClassRecord(rid, &pRecord));
        *ptkEnclosingClass = m_LiteWeightStgdb.m_MiniMd.getEnclosingClassOfNestedClass(pRecord);
        return S_OK;
    }
}

//*******************************************************************************
// Get count of Nested classes given the enclosing class.
//*******************************************************************************
__checkReturn
HRESULT
MDInternalRO::GetCountNestedClasses(    // return count of Nested classes.
    mdTypeDef   tkEnclosingClass,       // [IN]Enclosing class.
    ULONG      *pcNestedClassesCount)
{
    HRESULT hr;
    ULONG       ulCount;
    ULONG       ulRetCount = 0;
    NestedClassRec *pRecord;

    _ASSERTE(TypeFromToken(tkEnclosingClass) == mdtTypeDef && !IsNilToken(tkEnclosingClass));

    *pcNestedClassesCount = 0;

    ulCount = m_LiteWeightStgdb.m_MiniMd.getCountNestedClasss();

    for (ULONG i = 1; i <= ulCount; i++)
    {
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetNestedClassRecord(i, &pRecord));
        if (tkEnclosingClass == m_LiteWeightStgdb.m_MiniMd.getEnclosingClassOfNestedClass(pRecord))
            ulRetCount++;
    }
    *pcNestedClassesCount = ulRetCount;
    return S_OK;
} // MDInternalRO::GetCountNestedClasses

//*******************************************************************************
// Return array of Nested classes given the enclosing class.
//*******************************************************************************
__checkReturn
HRESULT
MDInternalRO::GetNestedClasses(     // Return actual count.
    mdTypeDef   tkEnclosingClass,       // [IN] Enclosing class.
    mdTypeDef   *rNestedClasses,        // [OUT] Array of nested class tokens.
    ULONG       ulNestedClasses,        // [IN] Size of array.
    ULONG      *pcNestedClasses)
{
    HRESULT hr;
    ULONG       ulCount;
    ULONG       ulRetCount = 0;
    NestedClassRec *pRecord;

    _ASSERTE(TypeFromToken(tkEnclosingClass) == mdtTypeDef &&
             !IsNilToken(tkEnclosingClass));

    *pcNestedClasses = 0;

    ulCount = m_LiteWeightStgdb.m_MiniMd.getCountNestedClasss();

    for (ULONG i = 1; i <= ulCount; i++)
    {
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetNestedClassRecord(i, &pRecord));
        if (tkEnclosingClass == m_LiteWeightStgdb.m_MiniMd.getEnclosingClassOfNestedClass(pRecord))
        {
            if (ovadd_le(ulRetCount, 1, ulNestedClasses))  // ulRetCount is 0 based.
                rNestedClasses[ulRetCount] = m_LiteWeightStgdb.m_MiniMd.getNestedClassOfNestedClass(pRecord);
            ulRetCount++;
        }
    }
    *pcNestedClasses = ulRetCount;
    return S_OK;
} // MDInternalRO::GetNestedClasses

//*******************************************************************************
// return the ModuleRef properties
//*******************************************************************************
__checkReturn
HRESULT MDInternalRO::GetModuleRefProps(   // return hresult
    mdModuleRef mur,                    // [IN] moduleref token
    LPCSTR      *pszName)               // [OUT] buffer to fill with the moduleref name
{
    _ASSERTE(TypeFromToken(mur) == mdtModuleRef);
    _ASSERTE(pszName);

    HRESULT hr;

    // Is it a valid token?
    if (!IsValidToken(mur))
    {
        *pszName = NULL;             // Not every caller checks returned HRESULT, allow to fail fast in that case
        return COR_E_BADIMAGEFORMAT; // Invalid Token
    }

    ModuleRefRec *pModuleRefRec;
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetModuleRefRecord(RidFromToken(mur), &pModuleRefRec));
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.getNameOfModuleRef(pModuleRefRec, pszName));

    return S_OK;
}



//*****************************************************************************
// Given a scope and a methoddef, return a pointer to methoddef's signature
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRO::GetSigOfMethodDef(
    mdMethodDef      methoddef,     // given a methoddef
    ULONG           *pcbSigBlob,    // [OUT] count of bytes in the signature blob
    PCCOR_SIGNATURE *ppSig)
{
    // Output parameter should not be NULL
    _ASSERTE(pcbSigBlob);
    _ASSERTE(TypeFromToken(methoddef) == mdtMethodDef);

    HRESULT hr;
    MethodRec *pMethodRec;
    *ppSig = NULL;
    *pcbSigBlob = 0;
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetMethodRecord(RidFromToken(methoddef), &pMethodRec));
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.getSignatureOfMethod(pMethodRec, ppSig, pcbSigBlob));
    return S_OK;
} // MDInternalRO::GetSigOfMethodDef


//*****************************************************************************
// Given a scope and a fielddef, return a pointer to fielddef's signature
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRO::GetSigOfFieldDef(
    mdFieldDef       fielddef,      // given a methoddef
    ULONG           *pcbSigBlob,    // [OUT] count of bytes in the signature blob
    PCCOR_SIGNATURE *ppSig)
{
    _ASSERTE(pcbSigBlob);
    _ASSERTE(TypeFromToken(fielddef) == mdtFieldDef);

    HRESULT hr;
    FieldRec *pFieldRec;
    *ppSig = NULL;
    *pcbSigBlob = 0;
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetFieldRecord(RidFromToken(fielddef), &pFieldRec));
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.getSignatureOfField(pFieldRec, ppSig, pcbSigBlob));
    return S_OK;
} // MDInternalRO::GetSigOfFieldDef

//*****************************************************************************
// Get signature for the token (FieldDef, MethodDef, Signature, or TypeSpec).
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRO::GetSigFromToken(
    mdToken           tk,
    ULONG *           pcbSig,
    PCCOR_SIGNATURE * ppSig)
{
    HRESULT hr;

    *ppSig = NULL;
    *pcbSig = 0;
    switch (TypeFromToken(tk))
    {
    case mdtSignature:
        {
            StandAloneSigRec * pRec;
            IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetStandAloneSigRecord(RidFromToken(tk), &pRec));
            IfFailRet(m_LiteWeightStgdb.m_MiniMd.getSignatureOfStandAloneSig(pRec, ppSig, pcbSig));
            return S_OK;
        }
    case mdtTypeSpec:
        {
            TypeSpecRec * pRec;
            IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetTypeSpecRecord(RidFromToken(tk), &pRec));
            IfFailRet(m_LiteWeightStgdb.m_MiniMd.getSignatureOfTypeSpec(pRec, ppSig, pcbSig));
            return S_OK;
        }
    case mdtMethodDef:
        {
            IfFailRet(GetSigOfMethodDef(tk, pcbSig, ppSig));
            return S_OK;
        }
    case mdtFieldDef:
        {
            IfFailRet(GetSigOfFieldDef(tk, pcbSig, ppSig));
            return S_OK;
        }
    }

    // not a known token type.
    *pcbSig = 0;
    return META_E_INVALID_TOKEN_TYPE;
} // MDInternalRO::GetSigFromToken


//*****************************************************************************
// Given methoddef, return the flags
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRO::GetMethodDefProps(
    mdMethodDef md,
    DWORD      *pdwFlags)   // return mdPublic, mdAbstract, etc
{
    HRESULT hr;
    MethodRec *pMethodRec;

    *pdwFlags = (DWORD)-1;
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetMethodRecord(RidFromToken(md), &pMethodRec));
    *pdwFlags = m_LiteWeightStgdb.m_MiniMd.getFlagsOfMethod(pMethodRec);

    return S_OK;
} // MDInternalRO::GetMethodDefProps

//*****************************************************************************
// Given a scope and a methoddef/methodimpl, return RVA and impl flags
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::GetMethodImplProps(
    mdMethodDef tk,                     // [IN] MethodDef
    ULONG       *pulCodeRVA,            // [OUT] CodeRVA
    DWORD       *pdwImplFlags)          // [OUT] Impl. Flags
{
    HRESULT hr;
    _ASSERTE(TypeFromToken(tk) == mdtMethodDef);

    MethodRec *pMethodRec;
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetMethodRecord(RidFromToken(tk), &pMethodRec));

    if (pulCodeRVA)
    {
        *pulCodeRVA = m_LiteWeightStgdb.m_MiniMd.getRVAOfMethod(pMethodRec);
    }

    if (pdwImplFlags)
    {
        *pdwImplFlags = m_LiteWeightStgdb.m_MiniMd.getImplFlagsOfMethod(pMethodRec);
    }

    return S_OK;
} // MDInternalRO::GetMethodImplProps


//*****************************************************************************
// return the field RVA
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::GetFieldRVA(
    mdToken     fd,                     // [IN] FieldDef
    ULONG       *pulCodeRVA)            // [OUT] CodeRVA
{
    HRESULT hr;
    _ASSERTE(TypeFromToken(fd) == mdtFieldDef);
    _ASSERTE(pulCodeRVA);

    RID iRecord;
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.FindFieldRVAFor(RidFromToken(fd), &iRecord));

    if (InvalidRid(iRecord))
    {
        if (pulCodeRVA)
            *pulCodeRVA = 0;
        return CLDB_E_RECORD_NOTFOUND;
    }

    FieldRVARec *pFieldRVARec;
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetFieldRVARecord(iRecord, &pFieldRVARec));

    *pulCodeRVA = m_LiteWeightStgdb.m_MiniMd.getRVAOfFieldRVA(pFieldRVARec);
    return NOERROR;
}

//*****************************************************************************
// Given a fielddef, return the flags. Such as fdPublic, fdStatic, etc
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRO::GetFieldDefProps(
    mdFieldDef fd,          // given memberdef
    DWORD     *pdwFlags)    // [OUT] return fdPublic, fdPrive, etc flags
{
    HRESULT hr;
    _ASSERTE(TypeFromToken(fd) == mdtFieldDef);

    FieldRec *pFieldRec;

    *pdwFlags = (DWORD)-1;
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetFieldRecord(RidFromToken(fd), &pFieldRec));
    *pdwFlags = m_LiteWeightStgdb.m_MiniMd.getFlagsOfField(pFieldRec);

    return S_OK;
} // MDInternalRO::GetFieldDefProps

//*****************************************************************************
// return default value of a token(could be paramdef, fielddef, or property)
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::GetDefaultValue(   // return hresult
    mdToken     tk,                     // [IN] given FieldDef, ParamDef, or Property
    MDDefaultValue  *pMDDefaultValue)   // [OUT] default value
{
    _ASSERTE(pMDDefaultValue);

    HRESULT     hr;
    BYTE        bType;
    const VOID  *pValue;
    ULONG       cbValue;
    RID         rid;
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.FindConstantFor(RidFromToken(tk), TypeFromToken(tk), &rid));
    if (InvalidRid(rid))
    {
        pMDDefaultValue->m_bType = ELEMENT_TYPE_VOID;
        return S_OK;
    }
    ConstantRec *pConstantRec;
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetConstantRecord(rid, &pConstantRec));

    // get the type of constant value
    bType = m_LiteWeightStgdb.m_MiniMd.getTypeOfConstant(pConstantRec);

    // get the value blob
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.getValueOfConstant(pConstantRec, reinterpret_cast<const BYTE **>(&pValue), &cbValue));
    // convert it to our internal default value representation
    hr = _FillMDDefaultValue(bType, pValue, cbValue, pMDDefaultValue);
    return hr;
} // MDInternalRO::GetDefaultValue

//*****************************************************************************
// Given a scope and a methoddef/fielddef, return the dispid
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::GetDispIdOfMemberDef(     // return hresult
    mdToken     tk,                     // given methoddef or fielddef
    ULONG       *pDispid)               // Put the dispid here.
{
#ifdef FEATURE_COMINTEROP
    HRESULT     hr;                     // A result.
    const BYTE  *pBlob;                 // Blob with dispid.
    ULONG       cbBlob;                 // Length of blob.
    UINT32      dispid;                 // temporary for dispid.

    // Get the DISPID, if any.
    _ASSERTE(pDispid);

    *pDispid = DISPID_UNKNOWN;
    hr = GetCustomAttributeByName(tk, INTEROP_DISPID_TYPE, (const void**)&pBlob, &cbBlob);
    if (hr == S_OK)
    {
        CustomAttributeParser cap(pBlob, cbBlob);
        IfFailGo(cap.SkipProlog());
        IfFailGo(cap.GetU4(&dispid));
        *pDispid = dispid;
    }

ErrExit:
    return hr;
#else // FEATURE_COMINTEROP
    _ASSERTE(false);
    return E_NOTIMPL;
#endif // FEATURE_COMINTEROP
} // MDInternalRO::GetDispIdOfMemberDef

//*****************************************************************************
// Given interfaceimpl, return the TypeRef/TypeDef and flags
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRO::GetTypeOfInterfaceImpl( // return hresult
    mdInterfaceImpl iiImpl,             // given a interfaceimpl
    mdToken        *ptkType)
{
    HRESULT hr;
    _ASSERTE(TypeFromToken(iiImpl) == mdtInterfaceImpl);

    *ptkType = mdTypeDefNil;

    InterfaceImplRec *pIIRec;
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetInterfaceImplRecord(RidFromToken(iiImpl), &pIIRec));
    *ptkType = m_LiteWeightStgdb.m_MiniMd.getInterfaceOfInterfaceImpl(pIIRec);
    return S_OK;
} // MDInternalRO::GetTypeOfInterfaceImpl

//*****************************************************************************
// This routine gets the properties for the given MethodSpec token.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::GetMethodSpecProps(         // S_OK or error.
        mdMethodSpec mi,           // [IN] The method instantiation
        mdToken *tkParent,                  // [OUT] MethodDef or MemberRef
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to the blob value of meta data
        ULONG       *pcbSigBlob)            // [OUT] actual size of signature blob
{
    HRESULT         hr = NOERROR;
    MethodSpecRec  *pMethodSpecRec;

    LOG((LOGMD, "MD RegMeta::GetMethodSpecProps(0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        mi, tkParent, ppvSigBlob, pcbSigBlob));

    _ASSERTE(TypeFromToken(mi) == mdtMethodSpec);

    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetMethodSpecRecord(RidFromToken(mi), &pMethodSpecRec));

    if (tkParent)
        *tkParent = m_LiteWeightStgdb.m_MiniMd.getMethodOfMethodSpec(pMethodSpecRec);

    if (ppvSigBlob || pcbSigBlob)
    {
        // caller wants signature information
        PCCOR_SIGNATURE pvSigTmp;
        ULONG           cbSig;
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.getInstantiationOfMethodSpec(pMethodSpecRec, &pvSigTmp, &cbSig));
        if ( ppvSigBlob )
            *ppvSigBlob = pvSigTmp;
        if ( pcbSigBlob)
            *pcbSigBlob = cbSig;
    }


    return hr;
} // MDInternalRO::GetMethodSpecProps



//*****************************************************************************
// Given a classname, return the typedef
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRO::FindTypeDef(
    LPCSTR      szTypeDefNamespace, // [IN] Namespace for the TypeDef.
    LPCSTR      szTypeDefName,      // [IN] Name of the TypeDef.
    mdToken     tkEnclosingClass,   // [IN] TypeDef/TypeRef of enclosing class.
    mdTypeDef * ptkTypeDef)         // [OUT] return typedef
{
    HRESULT hr = S_OK;

    _ASSERTE((szTypeDefName != NULL) && (ptkTypeDef != NULL));
    _ASSERTE((TypeFromToken(tkEnclosingClass) == mdtTypeRef) ||
             (TypeFromToken(tkEnclosingClass) == mdtTypeDef) ||
             IsNilToken(tkEnclosingClass));

    // initialize the output parameter
    *ptkTypeDef = mdTypeDefNil;

    // Treat no namespace as empty string.
    if (szTypeDefNamespace == NULL)
        szTypeDefNamespace = "";

    // Do a linear search
    ULONG        cTypeDefRecs = m_LiteWeightStgdb.m_MiniMd.getCountTypeDefs();
    TypeDefRec * pTypeDefRec;
    LPCUTF8      szName;
    LPCUTF8      szNamespace;
    DWORD        dwFlags;

    // Get TypeDef of the tkEnclosingClass passed in
    if (TypeFromToken(tkEnclosingClass) == mdtTypeRef)
    {
        TypeRefRec * pTypeRefRec;
        mdToken      tkResolutionScope;

        IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetTypeRefRecord(RidFromToken(tkEnclosingClass), &pTypeRefRec));
        tkResolutionScope = m_LiteWeightStgdb.m_MiniMd.getResolutionScopeOfTypeRef(pTypeRefRec);
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.getNamespaceOfTypeRef(pTypeRefRec, &szNamespace));
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.getNameOfTypeRef(pTypeRefRec, &szName));

        // Update tkEnclosingClass to TypeDef
        IfFailRet(FindTypeDef(
                    szNamespace,
                    szName,
                    (TypeFromToken(tkResolutionScope) == mdtTypeRef) ? tkResolutionScope : mdTokenNil,
                    &tkEnclosingClass));
        _ASSERTE(TypeFromToken(tkEnclosingClass) == mdtTypeDef);
    }

    // Search for the TypeDef
    for (ULONG i = 1; i <= cTypeDefRecs; i++)
    {
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetTypeDefRecord(i, &pTypeDefRec));

        dwFlags = m_LiteWeightStgdb.m_MiniMd.getFlagsOfTypeDef(pTypeDefRec);

        if (!IsTdNested(dwFlags) && !IsNilToken(tkEnclosingClass))
        {
            // If the class is not Nested and EnclosingClass passed in is not nil
            continue;
        }
        else if (IsTdNested(dwFlags) && IsNilToken(tkEnclosingClass))
        {
            // If the class is nested and EnclosingClass passed is nil
            continue;
        }
        else if (!IsNilToken(tkEnclosingClass))
        {
            _ASSERTE(TypeFromToken(tkEnclosingClass) == mdtTypeDef);

            RID              iNestedClassRec;
            NestedClassRec * pNestedClassRec;
            mdTypeDef        tkEnclosingClassTmp;

            IfFailRet(m_LiteWeightStgdb.m_MiniMd.FindNestedClassFor(i, &iNestedClassRec));
            if (InvalidRid(iNestedClassRec))
                continue;
            IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetNestedClassRecord(iNestedClassRec, &pNestedClassRec));
            tkEnclosingClassTmp = m_LiteWeightStgdb.m_MiniMd.getEnclosingClassOfNestedClass(pNestedClassRec);
            if (tkEnclosingClass != tkEnclosingClassTmp)
                continue;
        }

        IfFailRet(m_LiteWeightStgdb.m_MiniMd.getNameOfTypeDef(pTypeDefRec, &szName));
        if (strcmp(szTypeDefName, szName) == 0)
        {
            IfFailRet(m_LiteWeightStgdb.m_MiniMd.getNamespaceOfTypeDef(pTypeDefRec, &szNamespace));
            if (strcmp(szTypeDefNamespace, szNamespace) == 0)
            {
                *ptkTypeDef = TokenFromRid(i, mdtTypeDef);
                return S_OK;
            }
        }
    }
    // Cannot find the TypeDef by name
    return CLDB_E_RECORD_NOTFOUND;
} // MDInternalRO::FindTypeDef

//*****************************************************************************
// Given a memberref, return a pointer to memberref's name and signature
//*****************************************************************************
// Warning: Even when the return value is ok, *ppvSigBlob could be NULL if
// the metadata is corrupted! (e.g. if CPackedLen::GetLength returned -1).
// TODO: consider returning a HRESULT to make errors evident to the caller.
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRO::GetNameAndSigOfMemberRef( // meberref's name
    mdMemberRef      memberref,         // given a memberref
    PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to a blob value of COM+ signature
    ULONG           *pcbSigBlob,        // [OUT] count of bytes in the signature blob
    LPCSTR          *pszMemberRefName)
{
    _ASSERTE(TypeFromToken(memberref) == mdtMemberRef);

    HRESULT       hr;
    MemberRefRec *pMemberRefRec;
    *pszMemberRefName = NULL;
    if (ppvSigBlob != NULL)
    {
        _ASSERTE(pcbSigBlob != NULL);
        *ppvSigBlob = NULL;
        *pcbSigBlob = 0;
    }
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetMemberRefRecord(RidFromToken(memberref), &pMemberRefRec));
    if (ppvSigBlob != NULL)
    {
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.getSignatureOfMemberRef(pMemberRefRec, ppvSigBlob, pcbSigBlob));
    }
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.getNameOfMemberRef(pMemberRefRec, pszMemberRefName));
    return S_OK;
} // MDInternalRO::GetNameAndSigOfMemberRef

//*****************************************************************************
// Given a memberref, return parent token. It can be a TypeRef, ModuleRef, or a MethodDef
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRO::GetParentOfMemberRef(
    mdMemberRef memberref,      // given a typedef
    mdToken    *ptkParent)      // return the parent token
{
    HRESULT hr;
    _ASSERTE(TypeFromToken(memberref) == mdtMemberRef);

    MemberRefRec *pMemberRefRec;

    *ptkParent = mdTokenNil;
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetMemberRefRecord(RidFromToken(memberref), &pMemberRefRec));
    *ptkParent = m_LiteWeightStgdb.m_MiniMd.getClassOfMemberRef(pMemberRefRec);

    return S_OK;
} // MDInternalRO::GetParentOfMemberRef

//*****************************************************************************
// return properties of a paramdef
//*****************************************************************************/
__checkReturn
HRESULT
MDInternalRO::GetParamDefProps (
    mdParamDef paramdef,        // given a paramdef
    USHORT    *pusSequence,     // [OUT] slot number for this parameter
    DWORD     *pdwAttr,         // [OUT] flags
    LPCSTR    *pszName)         // [OUT] return the name of the parameter
{
    _ASSERTE(TypeFromToken(paramdef) == mdtParamDef);
    HRESULT   hr;
    ParamRec *pParamRec;

    *pszName = NULL;
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetParamRecord(RidFromToken(paramdef), &pParamRec));
    if (pdwAttr != NULL)
    {
        *pdwAttr = m_LiteWeightStgdb.m_MiniMd.getFlagsOfParam(pParamRec);
    }
    if (pusSequence != NULL)
    {
        *pusSequence = m_LiteWeightStgdb.m_MiniMd.getSequenceOfParam(pParamRec);
    }
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.getNameOfParam(pParamRec, pszName));

    return S_OK;
} // MDInternalRO::GetParamDefProps

//*****************************************************************************
// Get property info for the method.
//*****************************************************************************
int MDInternalRO::CMethodSemanticsMapSearcher::Compare(
    const CMethodSemanticsMap *psFirst,
    const CMethodSemanticsMap *psSecond)
{
    if (psFirst->m_mdMethod < psSecond->m_mdMethod)
        return -1;
    if (psFirst->m_mdMethod > psSecond->m_mdMethod)
        return 1;
    return 0;
} // MDInternalRO::CMethodSemanticsMapSearcher::Compare

#ifndef DACCESS_COMPILE
int MDInternalRO::CMethodSemanticsMapSorter::Compare(
    CMethodSemanticsMap *psFirst,
    CMethodSemanticsMap *psSecond)
{
    if (psFirst->m_mdMethod < psSecond->m_mdMethod)
        return -1;
    if (psFirst->m_mdMethod > psSecond->m_mdMethod)
        return 1;
    return 0;
} // MDInternalRO::CMethodSemanticsMapSorter::Compare

__checkReturn
HRESULT MDInternalRO::GetPropertyInfoForMethodDef(  // Result.
    mdMethodDef md,                     // [IN] memberdef
    mdProperty  *ppd,                   // [OUT] put property token here
    LPCSTR      *pName,                 // [OUT] put pointer to name here
    ULONG       *pSemantic)             // [OUT] put semantic here
{
    HRESULT   hr;
    MethodSemanticsRec *pSemantics;     // A MethodSemantics record.
    MethodSemanticsRec *pFound=0;       // A MethodSemantics record that is a property for the desired function.
    RID         ridCur;                 // loop control.
    RID         ridMax;                 // Count of entries in table.
    USHORT      usSemantics = 0;        // A method's semantics.
    mdToken     tk;                     // A method def.

    ridMax = m_LiteWeightStgdb.m_MiniMd.getCountMethodSemantics();

    // Lazy initialization of m_pMethodSemanticsMap
    if ((ridMax > 10) && (m_pMethodSemanticsMap == NULL))
    {
        NewArrayHolder<CMethodSemanticsMap> pMethodSemanticsMap = new (nothrow) CMethodSemanticsMap[ridMax];
        if (pMethodSemanticsMap != NULL)
        {
            // Fill the table in MethodSemantics order.
            for (ridCur = 1; ridCur <= ridMax; ridCur++)
            {
                IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetMethodSemanticsRecord(ridCur, &pSemantics));
                tk = m_LiteWeightStgdb.m_MiniMd.getMethodOfMethodSemantics(pSemantics);
                pMethodSemanticsMap[ridCur-1].m_mdMethod = tk;
                pMethodSemanticsMap[ridCur-1].m_ridSemantics = ridCur;
            }
            // Sort to MethodDef order.
            CMethodSemanticsMapSorter sorter(pMethodSemanticsMap, ridMax);
            sorter.Sort();

            if (InterlockedCompareExchangeT<CMethodSemanticsMap *>(
                &m_pMethodSemanticsMap, pMethodSemanticsMap, NULL) == NULL)
            {   // The exchange did happen, supress of the allocated map
                pMethodSemanticsMap.SuppressRelease();
            }
        }
    }

    // Use m_pMethodSemanticsMap if it has been built.
    if (m_pMethodSemanticsMap != NULL)
    {
        CMethodSemanticsMapSearcher searcher(m_pMethodSemanticsMap, ridMax);
        CMethodSemanticsMap target;
        const CMethodSemanticsMap * pMatchedMethod;
        target.m_mdMethod = md;
        pMatchedMethod = searcher.Find(&target);

        // Was there at least one match?
        if (pMatchedMethod != NULL)
        {
            _ASSERTE(pMatchedMethod >= m_pMethodSemanticsMap);
            _ASSERTE(pMatchedMethod < m_pMethodSemanticsMap+ridMax);
            _ASSERTE(pMatchedMethod->m_mdMethod == md);

            ridCur = pMatchedMethod->m_ridSemantics;
            IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetMethodSemanticsRecord(ridCur, &pSemantics));
            usSemantics = m_LiteWeightStgdb.m_MiniMd.getSemanticOfMethodSemantics(pSemantics);

            // If the semantics record is a getter or setter for the method, that's what we want.
            if (usSemantics == msGetter || usSemantics == msSetter)
                pFound = pSemantics;
            else
            {   // The semantics record was neither getter or setter.  Because there can be
                //  multiple semantics records for a given method, look for other semantics
                //  records that match this record.
                const CMethodSemanticsMap *pScan;
                const CMethodSemanticsMap *pLo=m_pMethodSemanticsMap;
                const CMethodSemanticsMap *pHi=pLo+ridMax-1;
                for (pScan = pMatchedMethod-1; pScan >= pLo; --pScan)
                {
                    if (pScan->m_mdMethod == md)
                    {
                        ridCur = pScan->m_ridSemantics;
                        IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetMethodSemanticsRecord(ridCur, &pSemantics));
                        usSemantics = m_LiteWeightStgdb.m_MiniMd.getSemanticOfMethodSemantics(pSemantics);

                        if (usSemantics == msGetter || usSemantics == msSetter)
                        {
                            pFound = pSemantics;
                            break;
                        }
                    }
                    else
                        break;
                }

                if (pFound == 0)
                {   // Not found looking down, try looking up.
                    for (pScan = pMatchedMethod+1; pScan <= pHi; ++pScan)
                    {
                        if (pScan->m_mdMethod == md)
                        {
                            ridCur = pScan->m_ridSemantics;
                            IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetMethodSemanticsRecord(ridCur, &pSemantics));
                            usSemantics = m_LiteWeightStgdb.m_MiniMd.getSemanticOfMethodSemantics(pSemantics);

                            if (usSemantics == msGetter || usSemantics == msSetter)
                            {
                                pFound = pSemantics;
                                break;
                            }
                        }
                        else
                            break;
                    }

                }
            }
        }
    }
    else
    {   // Scan entire table.
        for (ridCur = 1; ridCur <= ridMax; ridCur++)
        {
            IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetMethodSemanticsRecord(ridCur, &pSemantics));
            if (md == m_LiteWeightStgdb.m_MiniMd.getMethodOfMethodSemantics(pSemantics))
            {   // The method matched, is this a property?
                usSemantics = m_LiteWeightStgdb.m_MiniMd.getSemanticOfMethodSemantics(pSemantics);
                if (usSemantics == msGetter || usSemantics == msSetter)
                {   // found a match.
                    pFound = pSemantics;
                    break;
                }
            }
        }
    }

    // Did the search find anything?
    if (pFound)
    {   // found a match. Fill out the output parameters
        PropertyRec     *pProperty;
        mdProperty      prop;
        prop = m_LiteWeightStgdb.m_MiniMd.getAssociationOfMethodSemantics(pFound);

        if (ppd)
            *ppd = prop;
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetPropertyRecord(RidFromToken(prop), &pProperty));

        if (pName != NULL)
        {
            IfFailRet(m_LiteWeightStgdb.m_MiniMd.getNameOfProperty(pProperty, pName));
        }

        if (pSemantic)
            *pSemantic =  usSemantics;
        return S_OK;
    }
    return S_FALSE;
} // MDInternalRO::GetPropertyInfoForMethodDef
#endif //!DACCESS_COMPILE

//*****************************************************************************
// return the pack size of a class
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::GetClassPackSize(
    mdTypeDef   td,                     // [IN] give typedef
    DWORD       *pdwPackSize)           // [OUT]
{
    HRESULT     hr = NOERROR;

    _ASSERTE(TypeFromToken(td) == mdtTypeDef && pdwPackSize);

    ClassLayoutRec *pRec;
    RID         ridClassLayout;

    IfFailGo(m_LiteWeightStgdb.m_MiniMd.FindClassLayoutFor(RidFromToken(td), &ridClassLayout));
    if (InvalidRid(ridClassLayout))
    {
        hr = CLDB_E_RECORD_NOTFOUND;
        goto ErrExit;
    }

    IfFailGo(m_LiteWeightStgdb.m_MiniMd.GetClassLayoutRecord(RidFromToken(ridClassLayout), &pRec));
    *pdwPackSize = m_LiteWeightStgdb.m_MiniMd.getPackingSizeOfClassLayout(pRec);
ErrExit:
    return hr;
} // MDInternalRO::GetClassPackSize


//*****************************************************************************
// return the total size of a value class
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::GetClassTotalSize( // return error if a class does not have total size info
    mdTypeDef   td,                     // [IN] give typedef
    ULONG       *pulClassSize)          // [OUT] return the total size of the class
{
    _ASSERTE(TypeFromToken(td) == mdtTypeDef && pulClassSize);

    ClassLayoutRec *pRec;
    HRESULT     hr = NOERROR;
    RID         ridClassLayout;

    IfFailGo(m_LiteWeightStgdb.m_MiniMd.FindClassLayoutFor(RidFromToken(td), &ridClassLayout));
    if (InvalidRid(ridClassLayout))
    {
        hr = CLDB_E_RECORD_NOTFOUND;
        goto ErrExit;
    }

    IfFailGo(m_LiteWeightStgdb.m_MiniMd.GetClassLayoutRecord(RidFromToken(ridClassLayout), &pRec));
    *pulClassSize = m_LiteWeightStgdb.m_MiniMd.getClassSizeOfClassLayout(pRec);
ErrExit:
    return hr;
} // MDInternalRO::GetClassTotalSize


//*****************************************************************************
// init the layout enumerator of a class
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::GetClassLayoutInit(
    mdTypeDef   td,                     // [IN] give typedef
    MD_CLASS_LAYOUT *pmdLayout)         // [OUT] set up the status of query here
{
    HRESULT     hr = NOERROR;
    _ASSERTE(TypeFromToken(td) == mdtTypeDef);

    // initialize the output parameter
    _ASSERTE(pmdLayout);
    memset(pmdLayout, 0, sizeof(MD_CLASS_LAYOUT));

    TypeDefRec  *pTypeDefRec;

    // record for this typedef in TypeDef Table
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetTypeDefRecord(RidFromToken(td), &pTypeDefRec));

    // find the starting and end field for this typedef
    pmdLayout->m_ridFieldCur = m_LiteWeightStgdb.m_MiniMd.getFieldListOfTypeDef(pTypeDefRec);
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.getEndFieldListOfTypeDef(RidFromToken(td), &(pmdLayout->m_ridFieldEnd)));
    return hr;
} // MDInternalRO::GetClassLayoutInit


//*****************************************************************************
// return the field offset for a given field
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::GetFieldOffset(
    mdFieldDef  fd,                     // [IN] fielddef
    ULONG       *pulOffset)             // [OUT] FieldOffset
{
    HRESULT     hr = S_OK;
    FieldLayoutRec *pRec;

    _ASSERTE(pulOffset);

    RID iLayout;

    IfFailGo(m_LiteWeightStgdb.m_MiniMd.FindFieldLayoutFor(RidFromToken(fd), &iLayout));

    if (InvalidRid(iLayout))
    {
        hr = S_FALSE;
        goto ErrExit;
    }

    IfFailGo(m_LiteWeightStgdb.m_MiniMd.GetFieldLayoutRecord(iLayout, &pRec));
    *pulOffset = m_LiteWeightStgdb.m_MiniMd.getOffSetOfFieldLayout(pRec);
    _ASSERTE(*pulOffset != UINT32_MAX);

ErrExit:
    return hr;
}


//*****************************************************************************
// enum the next the field layout
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::GetClassLayoutNext(
    MD_CLASS_LAYOUT *pLayout,           // [IN|OUT] set up the status of query here
    mdFieldDef  *pfd,                   // [OUT] field def
    ULONG       *pulOffset)             // [OUT] field offset or sequence
{
    HRESULT     hr = S_OK;

    _ASSERTE(pfd && pulOffset && pLayout);

    RID     iLayout2;
    FieldLayoutRec *pRec;

    // Make sure no one is messing with pLayout->m_ridFieldLayoutCur, since this doesn't
    // mean anything if we are using FieldLayout table.
    while (pLayout->m_ridFieldCur < pLayout->m_ridFieldEnd)
    {
        IfFailGo(m_LiteWeightStgdb.m_MiniMd.FindFieldLayoutFor(pLayout->m_ridFieldCur, &iLayout2));
        pLayout->m_ridFieldCur++;
        if (!InvalidRid(iLayout2))
        {
            IfFailGo(m_LiteWeightStgdb.m_MiniMd.GetFieldLayoutRecord(iLayout2, &pRec));
            *pulOffset = m_LiteWeightStgdb.m_MiniMd.getOffSetOfFieldLayout(pRec);
            _ASSERTE(*pulOffset != UINT32_MAX);
            *pfd = TokenFromRid(pLayout->m_ridFieldCur - 1, mdtFieldDef);
            goto ErrExit;
        }
    }

    *pfd = mdFieldDefNil;
    hr = S_FALSE;

    // fall through

ErrExit:
    return hr;
} // MDInternalRO::GetClassLayoutNext


//*****************************************************************************
// return the field's native type signature
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::GetFieldMarshal(  // return error if no native type associate with the token
    mdToken     tk,                     // [IN] given fielddef or paramdef
    PCCOR_SIGNATURE *pSigNativeType,    // [OUT] the native type signature
    ULONG       *pcbNativeType)         // [OUT] the count of bytes of *ppvNativeType
{
    // output parameters have to be supplied
    _ASSERTE(pcbNativeType);

    RID         rid;
    FieldMarshalRec *pFieldMarshalRec;
    HRESULT     hr = NOERROR;

    // find the row containing the marshal definition for tk
    IfFailGo(m_LiteWeightStgdb.m_MiniMd.FindFieldMarshalFor(RidFromToken(tk), TypeFromToken(tk), &rid));
    if (InvalidRid(rid))
    {
        *pSigNativeType = NULL;
        *pcbNativeType = 0;
        hr = CLDB_E_RECORD_NOTFOUND;
        goto ErrExit;
    }
    IfFailGo(m_LiteWeightStgdb.m_MiniMd.GetFieldMarshalRecord(rid, &pFieldMarshalRec));

    // get the native type
    IfFailGo(m_LiteWeightStgdb.m_MiniMd.getNativeTypeOfFieldMarshal(pFieldMarshalRec, pSigNativeType, pcbNativeType));
ErrExit:
    return hr;
} // MDInternalRO::GetFieldMarshal



//*****************************************
// property APIs
//*****************************************

//*****************************************************************************
// Find property by name
//*****************************************************************************
__checkReturn
HRESULT  MDInternalRO::FindProperty(
    mdTypeDef   td,                     // [IN] given a typdef
    LPCSTR      szPropName,             // [IN] property name
    mdProperty  *pProp)                 // [OUT] return property token
{
    HRESULT     hr = NOERROR;

    // output parameters have to be supplied
    _ASSERTE(TypeFromToken(td) == mdtTypeDef && pProp);

    PropertyMapRec *pRec;
    PropertyRec *pProperty;
    RID         ridPropertyMap;
    RID         ridCur;
    RID         ridEnd;
    LPCUTF8     szName;

    IfFailGo(m_LiteWeightStgdb.m_MiniMd.FindPropertyMapFor(RidFromToken(td), &ridPropertyMap));
    if (InvalidRid(ridPropertyMap))
    {
        // not found!
        hr = CLDB_E_RECORD_NOTFOUND;
        goto ErrExit;
    }

    IfFailGo(m_LiteWeightStgdb.m_MiniMd.GetPropertyMapRecord(ridPropertyMap, &pRec));

    // get the starting/ending rid of properties of this typedef
    ridCur = m_LiteWeightStgdb.m_MiniMd.getPropertyListOfPropertyMap(pRec);
    IfFailGo(m_LiteWeightStgdb.m_MiniMd.getEndPropertyListOfPropertyMap(ridPropertyMap, &ridEnd));

    for (; ridCur < ridEnd; ridCur ++)
    {
        IfFailGo(m_LiteWeightStgdb.m_MiniMd.GetPropertyRecord(ridCur, &pProperty));
        IfFailGo(m_LiteWeightStgdb.m_MiniMd.getNameOfProperty(pProperty, &szName));
        if (strcmp(szName, szPropName) ==0)
        {
            // Found the match. Set the output parameter and we are done.
            *pProp = TokenFromRid(ridCur, mdtProperty);
            goto ErrExit;
        }
    }

    // not found
    hr = CLDB_E_RECORD_NOTFOUND;
ErrExit:
    return hr;

} // MDInternalRO::FindProperty



//*****************************************************************************
// return the properties of a property
//*****************************************************************************
__checkReturn
HRESULT  MDInternalRO::GetPropertyProps(
    mdProperty  prop,                   // [IN] property token
    LPCSTR      *pszProperty,           // [OUT] property name
    DWORD       *pdwPropFlags,          // [OUT] property flags.
    PCCOR_SIGNATURE *ppvSig,            // [OUT] property type. pointing to meta data internal blob
    ULONG       *pcbSig)                // [OUT] count of bytes in *ppvSig
{
    // output parameters have to be supplied
    _ASSERTE(TypeFromToken(prop) == mdtProperty);

    HRESULT hr;

    PropertyRec     *pProperty;
    ULONG           cbSig;

    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetPropertyRecord(RidFromToken(prop), &pProperty));

    // get name of the property
    if (pszProperty != NULL)
    {
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.getNameOfProperty(pProperty, pszProperty));
    }

    // get the flags of property
    if (pdwPropFlags)
        *pdwPropFlags = m_LiteWeightStgdb.m_MiniMd.getPropFlagsOfProperty(pProperty);

    // get the type of the property
    if (ppvSig != NULL)
    {
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.getTypeOfProperty(pProperty, ppvSig, &cbSig));
        if (pcbSig != NULL)
        {
            *pcbSig = cbSig;
        }
    }

    return S_OK;
} // MDInternalRO::GetPropertyProps


//**********************************
//
// Event APIs
//
//**********************************

//*****************************************************************************
// return an event by given the name
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::FindEvent(
    mdTypeDef   td,                     // [IN] given a typdef
    LPCSTR      szEventName,            // [IN] event name
    mdEvent     *pEvent)                // [OUT] return event token
{
    HRESULT     hr = NOERROR;

    // output parameters have to be supplied
    _ASSERTE(TypeFromToken(td) == mdtTypeDef && pEvent);

    EventMapRec *pRec;
    EventRec    *pEventRec;
    RID         ridEventMap;
    RID         ridCur;
    RID         ridEnd;
    LPCUTF8     szName;

    IfFailGo(m_LiteWeightStgdb.m_MiniMd.FindEventMapFor(RidFromToken(td), &ridEventMap));
    if (InvalidRid(ridEventMap))
    {
        // not found!
        hr = CLDB_E_RECORD_NOTFOUND;
        goto ErrExit;
    }
    IfFailGo(m_LiteWeightStgdb.m_MiniMd.GetEventMapRecord(ridEventMap, &pRec));

    // get the starting/ending rid of properties of this typedef
    ridCur = m_LiteWeightStgdb.m_MiniMd.getEventListOfEventMap(pRec);
    IfFailGo(m_LiteWeightStgdb.m_MiniMd.getEndEventListOfEventMap(ridEventMap, &ridEnd));

    for (; ridCur < ridEnd; ridCur ++)
    {
        IfFailGo(m_LiteWeightStgdb.m_MiniMd.GetEventRecord(ridCur, &pEventRec));
        IfFailGo(m_LiteWeightStgdb.m_MiniMd.getNameOfEvent(pEventRec, &szName));
        if (strcmp(szName, szEventName) ==0)
        {
            // Found the match. Set the output parameter and we are done.
            *pEvent = TokenFromRid(ridCur, mdtEvent);
            goto ErrExit;
        }
    }

    // not found
    hr = CLDB_E_RECORD_NOTFOUND;
ErrExit:
    return hr;
} // MDInternalRO::FindEvent


//*****************************************************************************
// return the properties of an event
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::GetEventProps(           // S_OK, S_FALSE, or error.
    mdEvent     ev,                     // [IN] event token
    LPCSTR      *pszEvent,                // [OUT] Event name
    DWORD       *pdwEventFlags,         // [OUT] Event flags.
    mdToken     *ptkEventType)         // [OUT] EventType class
{
    // output parameters have to be supplied
    _ASSERTE(TypeFromToken(ev) == mdtEvent);

    HRESULT   hr;
    EventRec *pEvent;

    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetEventRecord(RidFromToken(ev), &pEvent));
    if (pszEvent != NULL)
    {
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.getNameOfEvent(pEvent, pszEvent));
    }
    if (pdwEventFlags)
        *pdwEventFlags = m_LiteWeightStgdb.m_MiniMd.getEventFlagsOfEvent(pEvent);
    if (ptkEventType)
        *ptkEventType = m_LiteWeightStgdb.m_MiniMd.getEventTypeOfEvent(pEvent);

    return S_OK;
} // MDInternalRO::GetEventProps

//*****************************************************************************
// return the properties of a generic param
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::GetGenericParamProps(        // S_OK or error.
        mdGenericParam rd,                  // [IN] The type parameter
        ULONG* pulSequence,                 // [OUT] Parameter sequence number
        DWORD* pdwAttr,                     // [OUT] Type parameter flags (for future use)
        mdToken *ptOwner,                   // [OUT] The owner (TypeDef or MethodDef)
        DWORD *reserved,                    // [OUT] The kind (TypeDef/Ref/Spec, for future use)
        LPCSTR *szName)                      // [OUT] The name
{
    HRESULT           hr = NOERROR;
    GenericParamRec * pGenericParamRec = NULL;

    // See if this version of the metadata can do Generics
    if (!m_LiteWeightStgdb.m_MiniMd.SupportsGenerics())
        IfFailGo(CLDB_E_INCOMPATIBLE);

    _ASSERTE(TypeFromToken(rd) == mdtGenericParam);
    if (TypeFromToken(rd) != mdtGenericParam)
    {
        IfFailGo(CLDB_E_FILE_CORRUPT);
    }

    IfFailGo(m_LiteWeightStgdb.m_MiniMd.GetGenericParamRecord(RidFromToken(rd), &pGenericParamRec));

    if (pulSequence)
        *pulSequence = m_LiteWeightStgdb.m_MiniMd.getNumberOfGenericParam(pGenericParamRec);
    if (pdwAttr)
        *pdwAttr = m_LiteWeightStgdb.m_MiniMd.getFlagsOfGenericParam(pGenericParamRec);
    if (ptOwner)
        *ptOwner = m_LiteWeightStgdb.m_MiniMd.getOwnerOfGenericParam(pGenericParamRec);
    if (szName != NULL)
    {
        IfFailGo(m_LiteWeightStgdb.m_MiniMd.getNameOfGenericParam(pGenericParamRec, szName));
    }
ErrExit:
    return hr;
} // MDInternalRO::GetGenericParamProps

//*****************************************************************************
// This routine gets the properties for the given GenericParamConstraint token.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::GetGenericParamConstraintProps(      // S_OK or error.
        mdGenericParamConstraint rd,        // [IN] The constraint token
        mdGenericParam *ptGenericParam,     // [OUT] GenericParam that is constrained
        mdToken      *ptkConstraintType)    // [OUT] TypeDef/Ref/Spec constraint
{
    HRESULT         hr = NOERROR;
    GenericParamConstraintRec  *pGPCRec;
    RID             ridRD = RidFromToken(rd);

    // See if this version of the metadata can do Generics
    if (!m_LiteWeightStgdb.m_MiniMd.SupportsGenerics())
        IfFailGo(CLDB_E_INCOMPATIBLE);

    if((TypeFromToken(rd) == mdtGenericParamConstraint) && (ridRD != 0))
    {
        IfFailGo(m_LiteWeightStgdb.m_MiniMd.GetGenericParamConstraintRecord(ridRD, &pGPCRec));

        if (ptGenericParam)
            *ptGenericParam = TokenFromRid(m_LiteWeightStgdb.m_MiniMd.getOwnerOfGenericParamConstraint(pGPCRec),mdtGenericParam);
        if (ptkConstraintType)
            *ptkConstraintType = m_LiteWeightStgdb.m_MiniMd.getConstraintOfGenericParamConstraint(pGPCRec);
    }
    else
        hr =  META_E_BAD_INPUT_PARAMETER;

ErrExit:
    return hr;
} // MDInternalRO::GetGenericParamConstraintProps

//*****************************************************************************
// Find methoddef of a particular associate with a property or an event
//*****************************************************************************
__checkReturn
HRESULT  MDInternalRO::FindAssociate(
    mdToken     evprop,                 // [IN] given a property or event token
    DWORD       dwSemantics,            // [IN] given a associate semantics(setter, getter, testdefault, reset)
    mdMethodDef *pmd)                   // [OUT] return method def token
{
    HRESULT     hr = NOERROR;

    // output parameters have to be supplied
    _ASSERTE(pmd);
    _ASSERTE(TypeFromToken(evprop) == mdtEvent || TypeFromToken(evprop) == mdtProperty);

    MethodSemanticsRec *pSemantics;
    RID         ridCur;
    RID         ridEnd;

    IfFailGo(m_LiteWeightStgdb.m_MiniMd.getAssociatesForToken(evprop, &ridEnd, &ridCur));
    for (; ridCur < ridEnd; ridCur++)
    {
        IfFailGo(m_LiteWeightStgdb.m_MiniMd.GetMethodSemanticsRecord(ridCur, &pSemantics));
        if (dwSemantics == m_LiteWeightStgdb.m_MiniMd.getSemanticOfMethodSemantics(pSemantics))
        {
            // found a match
            *pmd = m_LiteWeightStgdb.m_MiniMd.getMethodOfMethodSemantics(pSemantics);
            goto ErrExit;
        }
    }

    // not found
    hr = CLDB_E_RECORD_NOTFOUND;
ErrExit:
    return hr;
} // MDInternalRO::FindAssociate


//*****************************************************************************
// get counts of methodsemantics associated with a particular property/event
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::EnumAssociateInit(
    mdToken     evprop,                 // [IN] given a property or an event token
    HENUMInternal *phEnum)              // [OUT] cursor to hold the query result
{
    HRESULT hr;
    _ASSERTE(phEnum);

    HENUMInternal::ZeroEnum(phEnum);

    // There is no token kind!!!
    phEnum->m_tkKind = UINT32_MAX;

    // output parameters have to be supplied
    _ASSERTE(TypeFromToken(evprop) == mdtEvent || TypeFromToken(evprop) == mdtProperty);

    phEnum->m_EnumType = MDSimpleEnum;
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.getAssociatesForToken(evprop, &phEnum->u.m_ulEnd, &phEnum->u.m_ulStart));
    phEnum->u.m_ulCur = phEnum->u.m_ulStart;
    phEnum->m_ulCount = phEnum->u.m_ulEnd - phEnum->u.m_ulStart;

    return S_OK;
} // MDInternalRO::EnumAssociateInit


//*****************************************************************************
// get all methodsemantics associated with a particular property/event
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::GetAllAssociates(
    HENUMInternal *phEnum,              // [OUT] cursor to hold the query result
    ASSOCIATE_RECORD *pAssociateRec,    // [OUT] struct to fill for output
    ULONG       cAssociateRec)          // [IN] size of the buffer
{
    _ASSERTE(phEnum && pAssociateRec);

    HRESULT hr;
    MethodSemanticsRec *pSemantics;
    RID         ridCur;
    _ASSERTE(cAssociateRec == phEnum->m_ulCount);

    // Convert from row pointers to RIDs.
    for (ridCur = phEnum->u.m_ulStart; ridCur < phEnum->u.m_ulEnd; ++ridCur)
    {
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetMethodSemanticsRecord(ridCur, &pSemantics));

        pAssociateRec[ridCur-phEnum->u.m_ulStart].m_memberdef = m_LiteWeightStgdb.m_MiniMd.getMethodOfMethodSemantics(pSemantics);
        pAssociateRec[ridCur-phEnum->u.m_ulStart].m_dwSemantics = m_LiteWeightStgdb.m_MiniMd.getSemanticOfMethodSemantics(pSemantics);
    }

    return S_OK;
} // MDInternalRO::GetAllAssociates


//*****************************************************************************
// Get the Action and Permissions blob for a given PermissionSet.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::GetPermissionSetProps(
    mdPermission pm,                    // [IN] the permission token.
    DWORD       *pdwAction,             // [OUT] CorDeclSecurity.
    void const  **ppvPermission,        // [OUT] permission blob.
    ULONG       *pcbPermission)         // [OUT] count of bytes of pvPermission.
{
    HRESULT hr;
    _ASSERTE(TypeFromToken(pm) == mdtPermission);
    _ASSERTE(pdwAction && ppvPermission && pcbPermission);

    DeclSecurityRec *pPerm;

    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetDeclSecurityRecord(RidFromToken(pm), &pPerm));
    *pdwAction = m_LiteWeightStgdb.m_MiniMd.getActionOfDeclSecurity(pPerm);
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.getPermissionSetOfDeclSecurity(pPerm, (const BYTE **)ppvPermission, pcbPermission));

    return S_OK;
} // MDInternalRO::GetPermissionSetProps

//*****************************************************************************
// Get the String given the String token.
// Return a pointer to the string, or NULL in case of error.
//*****************************************************************************
__checkReturn
HRESULT
MDInternalRO::GetUserString(    // Offset into the string blob heap.
    mdString stk,               // [IN] the string token.
    ULONG   *pcchStringSize,    // [OUT] count of characters in the string.
    BOOL    *pfIs80Plus,        // [OUT] specifies where there are extended characters >= 0x80.
    LPCWSTR *pwszUserString)
{
    HRESULT hr;
    LPWSTR  wszTmp;

    if (pfIs80Plus != NULL)
    {
        *pfIs80Plus = FALSE;
    }
    *pwszUserString = NULL;
    *pcchStringSize = 0;

    _ASSERTE(pcchStringSize != NULL);
    MetaData::DataBlob userString;
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetUserString(RidFromToken(stk), &userString));

    wszTmp = reinterpret_cast<LPWSTR>(userString.GetDataPointer());

    *pcchStringSize = userString.GetSize() / sizeof(WCHAR);

    if (userString.IsEmpty())
    {
        *pwszUserString = NULL;
        return S_OK;
    }

    if (pfIs80Plus != NULL)
    {
        if (userString.GetSize() % sizeof(WCHAR) == 0)
        {
            *pfIs80Plus = TRUE; // no indicator, presume the worst
        }
        // Return the user string terminator (contains value fIs80Plus)
        *pfIs80Plus = *(reinterpret_cast<PBYTE>(wszTmp + *pcchStringSize));
    }

    *pwszUserString = wszTmp;
    return S_OK;
} // MDInternalRO::GetUserString

//*****************************************************************************
// Return contents of Pinvoke given the forwarded member token.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::GetPinvokeMap(
    mdToken     tk,                     // [IN] FieldDef or MethodDef.
    DWORD       *pdwMappingFlags,       // [OUT] Flags used for mapping.
    LPCSTR      *pszImportName,         // [OUT] Import name.
    mdModuleRef *pmrImportDLL)          // [OUT] ModuleRef token for the target DLL.
{
    HRESULT     hr;
    ImplMapRec *pRecord;
    RID         iRecord;

    IfFailRet(m_LiteWeightStgdb.m_MiniMd.FindImplMapFor(RidFromToken(tk), TypeFromToken(tk), &iRecord));
    if (InvalidRid(iRecord))
    {
        return CLDB_E_RECORD_NOTFOUND;
    }
    else
    {
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetImplMapRecord(iRecord, &pRecord));
    }

    if (pdwMappingFlags)
        *pdwMappingFlags = m_LiteWeightStgdb.m_MiniMd.getMappingFlagsOfImplMap(pRecord);
    if (pszImportName != NULL)
    {
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.getImportNameOfImplMap(pRecord, pszImportName));
    }
    if (pmrImportDLL)
        *pmrImportDLL = m_LiteWeightStgdb.m_MiniMd.getImportScopeOfImplMap(pRecord);

    return S_OK;
} // MDInternalRO::GetPinvokeMap

//*****************************************************************************
// Get the properties for the given Assembly token.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::GetAssemblyProps(
    mdAssembly  mda,                    // [IN] The Assembly for which to get the properties.
    const void  **ppbPublicKey,         // [OUT] Pointer to the public key.
    ULONG       *pcbPublicKey,          // [OUT] Count of bytes in the public key.
    ULONG       *pulHashAlgId,          // [OUT] Hash Algorithm.
    LPCSTR      *pszName,               // [OUT] Buffer to fill with name.
    AssemblyMetaDataInternal *pMetaData,// [OUT] Assembly MetaData.
    DWORD       *pdwAssemblyFlags)      // [OUT] Flags.
{
    HRESULT      hr;
    AssemblyRec *pRecord;

    _ASSERTE(TypeFromToken(mda) == mdtAssembly && RidFromToken(mda));
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetAssemblyRecord(RidFromToken(mda), &pRecord));

    if (ppbPublicKey != NULL)
    {
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.getPublicKeyOfAssembly(pRecord, reinterpret_cast<const BYTE **>(ppbPublicKey), pcbPublicKey));
    }
    if (pulHashAlgId)
        *pulHashAlgId = m_LiteWeightStgdb.m_MiniMd.getHashAlgIdOfAssembly(pRecord);
    if (pszName != NULL)
    {
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.getNameOfAssembly(pRecord, pszName));
    }
    if (pMetaData)
    {
        pMetaData->usMajorVersion = m_LiteWeightStgdb.m_MiniMd.getMajorVersionOfAssembly(pRecord);
        pMetaData->usMinorVersion = m_LiteWeightStgdb.m_MiniMd.getMinorVersionOfAssembly(pRecord);
        pMetaData->usBuildNumber = m_LiteWeightStgdb.m_MiniMd.getBuildNumberOfAssembly(pRecord);
        pMetaData->usRevisionNumber = m_LiteWeightStgdb.m_MiniMd.getRevisionNumberOfAssembly(pRecord);
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.getLocaleOfAssembly(pRecord, &pMetaData->szLocale));
    }
    if (pdwAssemblyFlags)
    {
        *pdwAssemblyFlags = m_LiteWeightStgdb.m_MiniMd.getFlagsOfAssembly(pRecord);

        // Turn on the afPublicKey if PublicKey blob is not empty
        DWORD cbPublicKey;
        const BYTE *pbPublicKey;
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.getPublicKeyOfAssembly(pRecord, &pbPublicKey, &cbPublicKey));
        if (cbPublicKey != 0)
            *pdwAssemblyFlags |= afPublicKey;
    }

    return S_OK;
} // MDInternalRO::GetAssemblyProps

//*****************************************************************************
// Get the properties for the given AssemblyRef token.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::GetAssemblyRefProps(
    mdAssemblyRef mdar,                 // [IN] The AssemblyRef for which to get the properties.
    const void  **ppbPublicKeyOrToken,  // [OUT] Pointer to the public key or token.
    ULONG       *pcbPublicKeyOrToken,   // [OUT] Count of bytes in the public key or token.
    LPCSTR      *pszName,               // [OUT] Buffer to fill with name.
    AssemblyMetaDataInternal *pMetaData,// [OUT] Assembly MetaData.
    const void  **ppbHashValue,         // [OUT] Hash blob.
    ULONG       *pcbHashValue,          // [OUT] Count of bytes in the hash blob.
    DWORD       *pdwAssemblyRefFlags)   // [OUT] Flags.
{
    HRESULT         hr;
    AssemblyRefRec *pRecord;

    _ASSERTE(TypeFromToken(mdar) == mdtAssemblyRef && RidFromToken(mdar));
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetAssemblyRefRecord(RidFromToken(mdar), &pRecord));

    if (ppbPublicKeyOrToken != NULL)
    {
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.getPublicKeyOrTokenOfAssemblyRef(pRecord, reinterpret_cast<const BYTE **>(ppbPublicKeyOrToken), pcbPublicKeyOrToken));
    }
    if (pszName != NULL)
    {
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.getNameOfAssemblyRef(pRecord, pszName));
    }
    if (pMetaData)
    {
        pMetaData->usMajorVersion = m_LiteWeightStgdb.m_MiniMd.getMajorVersionOfAssemblyRef(pRecord);
        pMetaData->usMinorVersion = m_LiteWeightStgdb.m_MiniMd.getMinorVersionOfAssemblyRef(pRecord);
        pMetaData->usBuildNumber = m_LiteWeightStgdb.m_MiniMd.getBuildNumberOfAssemblyRef(pRecord);
        pMetaData->usRevisionNumber = m_LiteWeightStgdb.m_MiniMd.getRevisionNumberOfAssemblyRef(pRecord);
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.getLocaleOfAssemblyRef(pRecord, &pMetaData->szLocale));
    }
    if (ppbHashValue != NULL)
    {
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.getHashValueOfAssemblyRef(pRecord, reinterpret_cast<const BYTE **>(ppbHashValue), pcbHashValue));
    }
    if (pdwAssemblyRefFlags != NULL)
    {
        *pdwAssemblyRefFlags = m_LiteWeightStgdb.m_MiniMd.getFlagsOfAssemblyRef(pRecord);
    }

    return S_OK;
} // MDInternalRO::GetAssemblyRefProps

//*****************************************************************************
// Get the properties for the given File token.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::GetFileProps(
    mdFile      mdf,                    // [IN] The File for which to get the properties.
    LPCSTR      *pszName,               // [OUT] Buffer to fill with name.
    const void  **ppbHashValue,         // [OUT] Pointer to the Hash Value Blob.
    ULONG       *pcbHashValue,          // [OUT] Count of bytes in the Hash Value Blob.
    DWORD       *pdwFileFlags)          // [OUT] Flags.
{
    HRESULT  hr;
    FileRec *pRecord;

    _ASSERTE(TypeFromToken(mdf) == mdtFile && RidFromToken(mdf));
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetFileRecord(RidFromToken(mdf), &pRecord));

    if (pszName != NULL)
    {
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.getNameOfFile(pRecord, pszName));
    }
    if (ppbHashValue != NULL)
    {
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.getHashValueOfFile(pRecord, reinterpret_cast<const BYTE **>(ppbHashValue), pcbHashValue));
    }
    if (pdwFileFlags != NULL)
    {
        *pdwFileFlags = m_LiteWeightStgdb.m_MiniMd.getFlagsOfFile(pRecord);
    }

    return S_OK;
} // MDInternalRO::GetFileProps

//*****************************************************************************
// Get the properties for the given ExportedType token.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::GetExportedTypeProps(
    mdExportedType   mdct,                   // [IN] The ExportedType for which to get the properties.
    LPCSTR      *pszNamespace,          // [OUT] Buffer to fill with namespace.
    LPCSTR      *pszName,               // [OUT] Buffer to fill with name.
    mdToken     *ptkImplementation,     // [OUT] mdFile or mdAssemblyRef that provides the ExportedType.
    mdTypeDef   *ptkTypeDef,            // [OUT] TypeDef token within the file.
    DWORD       *pdwExportedTypeFlags)       // [OUT] Flags.
{
    HRESULT          hr;
    ExportedTypeRec *pRecord;

    _ASSERTE(TypeFromToken(mdct) == mdtExportedType && RidFromToken(mdct));
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetExportedTypeRecord(RidFromToken(mdct), &pRecord));

    if (pszNamespace != NULL)
    {
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.getTypeNamespaceOfExportedType(pRecord, pszNamespace));
    }
    if (pszName != NULL)
    {
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.getTypeNameOfExportedType(pRecord, pszName));
    }
    if (ptkImplementation)
        *ptkImplementation = m_LiteWeightStgdb.m_MiniMd.getImplementationOfExportedType(pRecord);
    if (ptkTypeDef)
        *ptkTypeDef = m_LiteWeightStgdb.m_MiniMd.getTypeDefIdOfExportedType(pRecord);
    if (pdwExportedTypeFlags)
        *pdwExportedTypeFlags = m_LiteWeightStgdb.m_MiniMd.getFlagsOfExportedType(pRecord);

    return S_OK;
} // MDInternalRO::GetExportedTypeProps

//*****************************************************************************
// Get the properties for the given Resource token.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::GetManifestResourceProps(
    mdManifestResource  mdmr,           // [IN] The ManifestResource for which to get the properties.
    LPCSTR      *pszName,               // [OUT] Buffer to fill with name.
    mdToken     *ptkImplementation,     // [OUT] mdFile or mdAssemblyRef that provides the ExportedType.
    DWORD       *pdwOffset,             // [OUT] Offset to the beginning of the resource within the file.
    DWORD       *pdwResourceFlags)      // [OUT] Flags.
{
    HRESULT              hr;
    ManifestResourceRec *pRecord;

    _ASSERTE(TypeFromToken(mdmr) == mdtManifestResource && RidFromToken(mdmr));
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetManifestResourceRecord(RidFromToken(mdmr), &pRecord));

    if (pszName != NULL)
    {
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.getNameOfManifestResource(pRecord, pszName));
    }
    if (ptkImplementation)
        *ptkImplementation = m_LiteWeightStgdb.m_MiniMd.getImplementationOfManifestResource(pRecord);
    if (pdwOffset)
        *pdwOffset = m_LiteWeightStgdb.m_MiniMd.getOffsetOfManifestResource(pRecord);
    if (pdwResourceFlags)
        *pdwResourceFlags = m_LiteWeightStgdb.m_MiniMd.getFlagsOfManifestResource(pRecord);

    return S_OK;
} // MDInternalRO::GetManifestResourceProps

//*****************************************************************************
// Find the ExportedType given the name.
//*****************************************************************************
__checkReturn
STDMETHODIMP MDInternalRO::FindExportedTypeByName( // S_OK or error
    LPCSTR      szNamespace,            // [IN] Namespace of the ExportedType.
    LPCSTR      szName,                 // [IN] Name of the ExportedType.
    mdExportedType   tkEnclosingType,        // [IN] Token for the Enclosing Type.
    mdExportedType   *pmct)                  // [OUT] Put ExportedType token here.
{
    IMetaModelCommon *pCommon = static_cast<IMetaModelCommon*>(&m_LiteWeightStgdb.m_MiniMd);
    return pCommon->CommonFindExportedType(szNamespace, szName, tkEnclosingType, pmct);
} // MDInternalRO::FindExportedTypeByName

//*****************************************************************************
// Find the ManifestResource given the name.
//*****************************************************************************
__checkReturn
STDMETHODIMP MDInternalRO::FindManifestResourceByName(  // S_OK or error
    LPCSTR      szName,                 // [IN] Name of the resource.
    mdManifestResource *pmmr)           // [OUT] Put ManifestResource token here.
{
    _ASSERTE(szName && pmmr);

    HRESULT     hr;
    ManifestResourceRec *pRecord;
    ULONG       cRecords;               // Count of records.
    LPCUTF8     szNameTmp = 0;          // Name obtained from the database.
    ULONG       i;

    cRecords = m_LiteWeightStgdb.m_MiniMd.getCountManifestResources();

    // Search for the ExportedType.
    for (i = 1; i <= cRecords; i++)
    {
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetManifestResourceRecord(i, &pRecord));
        IfFailRet(m_LiteWeightStgdb.m_MiniMd.getNameOfManifestResource(pRecord, &szNameTmp));
        if (! strcmp(szName, szNameTmp))
        {
            *pmmr = TokenFromRid(i, mdtManifestResource);
            return S_OK;
        }
    }
    return CLDB_E_RECORD_NOTFOUND;
} // MDInternalRO::FindManifestResourceByName

//*****************************************************************************
// Get the Assembly token from the given scope.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::GetAssemblyFromScope( // S_OK or error
    mdAssembly  *ptkAssembly)           // [OUT] Put token here.
{
    _ASSERTE(ptkAssembly);

    if (m_LiteWeightStgdb.m_MiniMd.getCountAssemblys())
    {
        *ptkAssembly = TokenFromRid(1, mdtAssembly);
        return S_OK;
    }
    else
        return CLDB_E_RECORD_NOTFOUND;
} // MDInternalRO::GetAssemblyFromScope

//*******************************************************************************
// return properties regarding a TypeSpec
//*******************************************************************************
__checkReturn
HRESULT MDInternalRO::GetTypeSpecFromToken(   // S_OK or error.
    mdTypeSpec typespec,                // [IN] Signature token.
    PCCOR_SIGNATURE *ppvSig,            // [OUT] return pointer to token.
    ULONG       *pcbSig)                // [OUT] return size of signature.
{
    HRESULT             hr = NOERROR;

    _ASSERTE(TypeFromToken(typespec) == mdtTypeSpec);
    _ASSERTE(ppvSig && pcbSig);

    if (!IsValidToken(typespec))
    {
        *ppvSig = NULL;
        *pcbSig = 0;
        return E_INVALIDARG;
    }

    TypeSpecRec *pRec;
    IfFailRet(m_LiteWeightStgdb.m_MiniMd.GetTypeSpecRecord(RidFromToken(typespec), &pRec));

    if (pRec == NULL)
    {
        *ppvSig = NULL;
        *pcbSig = 0;
        return CLDB_E_FILE_CORRUPT;
    }

    IfFailRet(m_LiteWeightStgdb.m_MiniMd.getSignatureOfTypeSpec(pRec, ppvSig, pcbSig));

    return hr;
} // MDInternalRO::GetTypeSpecFromToken

//*****************************************************************************
// This function gets the "built for" version of a metadata scope.
//  NOTE: if the scope has never been saved, it will not have a built-for
//  version, and an empty string will be returned.
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::GetVersionString(
    LPCSTR * pVer)                      // [OUT] Put version string here.
{
    HRESULT hr = NOERROR;

    if (m_LiteWeightStgdb.m_pvMd != NULL)
    {
        // For convenience, get a pointer to the version string.
        // @todo: get from alternate locations when there is no STOREAGESIGNATURE.
        *pVer = reinterpret_cast<const char*>(reinterpret_cast<const STORAGESIGNATURE*>(m_LiteWeightStgdb.m_pvMd)->pVersion);
    }
    else
    {   // No string.
        *pVer = NULL;
    }

    return hr;
} // MDInternalRO::GetVersionString

//*****************************************************************************
// convert a text signature to com format
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::ConvertTextSigToComSig(// Return hresult.
    BOOL        fCreateTrIfNotFound,    // create typeref if not found or not
    LPCSTR      pSignature,             // class file format signature
    CQuickBytes *pqbNewSig,             // [OUT] place holder for COM+ signature
    ULONG       *pcbCount)              // [OUT] the result size of signature
{
    return E_NOTIMPL;
} // MDInternalRO::ConvertTextSigToComSig


//*****************************************************************************
// determine if a token is valid or not
//*****************************************************************************
BOOL MDInternalRO::IsValidToken(        // True or False.
    mdToken     tk)                     // [IN] Given token.
{
    RID rid = RidFromToken(tk);
    if (rid == 0)
    {
        return FALSE;
    }
    switch (TypeFromToken(tk))
    {
    case mdtModule:
        // can have only one module record
        return (rid <= m_LiteWeightStgdb.m_MiniMd.getCountModules());
    case mdtTypeRef:
        return (rid <= m_LiteWeightStgdb.m_MiniMd.getCountTypeRefs());
    case mdtTypeDef:
        return (rid <= m_LiteWeightStgdb.m_MiniMd.getCountTypeDefs());
    case mdtFieldDef:
        return (rid <= m_LiteWeightStgdb.m_MiniMd.getCountFields());
    case mdtMethodDef:
        return (rid <= m_LiteWeightStgdb.m_MiniMd.getCountMethods());
    case mdtParamDef:
        return (rid <= m_LiteWeightStgdb.m_MiniMd.getCountParams());
    case mdtInterfaceImpl:
        return (rid <= m_LiteWeightStgdb.m_MiniMd.getCountInterfaceImpls());
    case mdtMemberRef:
        return (rid <= m_LiteWeightStgdb.m_MiniMd.getCountMemberRefs());
    case mdtCustomAttribute:
        return (rid <= m_LiteWeightStgdb.m_MiniMd.getCountCustomAttributes());
    case mdtPermission:
        return (rid <= m_LiteWeightStgdb.m_MiniMd.getCountDeclSecuritys());
    case mdtSignature:
        return (rid <= m_LiteWeightStgdb.m_MiniMd.getCountStandAloneSigs());
    case mdtEvent:
        return (rid <= m_LiteWeightStgdb.m_MiniMd.getCountEvents());
    case mdtProperty:
        return (rid <= m_LiteWeightStgdb.m_MiniMd.getCountPropertys());
    case mdtModuleRef:
        return (rid <= m_LiteWeightStgdb.m_MiniMd.getCountModuleRefs());
    case mdtTypeSpec:
        return (rid <= m_LiteWeightStgdb.m_MiniMd.getCountTypeSpecs());
    case mdtAssembly:
        return (rid <= m_LiteWeightStgdb.m_MiniMd.getCountAssemblys());
    case mdtAssemblyRef:
        return (rid <= m_LiteWeightStgdb.m_MiniMd.getCountAssemblyRefs());
    case mdtFile:
        return (rid <= m_LiteWeightStgdb.m_MiniMd.getCountFiles());
    case mdtExportedType:
        return (rid <= m_LiteWeightStgdb.m_MiniMd.getCountExportedTypes());
    case mdtManifestResource:
        return (rid <= m_LiteWeightStgdb.m_MiniMd.getCountManifestResources());
    case mdtMethodSpec:
        return (rid <= m_LiteWeightStgdb.m_MiniMd.getCountMethodSpecs());
    case mdtString:
        // need to check the user string heap
        return m_LiteWeightStgdb.m_MiniMd.m_UserStringHeap.IsValidIndex(rid);
    default:
/* Don't  Assert here, this will break verifier tests.
        _ASSERTE(!"Unknown token kind!");
*/
        return FALSE;
    }
} // MDInternalRO::IsValidToken

mdModule MDInternalRO::GetModuleFromScope(void)
{
    return TokenFromRid(1, mdtModule);
} // MDInternalRO::GetModuleFromScope


//*****************************************************************************
// Fill a variant given a MDDefaultValue
// This routine will create a bstr if the ELEMENT_TYPE of default value is STRING
//*****************************************************************************
__checkReturn
HRESULT _FillMDDefaultValue(
    BYTE        bType,
    void const *pValue,
    ULONG       cbValue,
    MDDefaultValue  *pMDDefaultValue)
{
    HRESULT     hr = NOERROR;

    pMDDefaultValue->m_bType = bType;
    pMDDefaultValue->m_cbSize = cbValue;
    switch (bType)
    {
    case ELEMENT_TYPE_BOOLEAN:
        if (cbValue < 1)
        {
            IfFailGo(CLDB_E_FILE_CORRUPT);
        }
        pMDDefaultValue->m_bValue = *((BYTE *) pValue);
        break;
    case ELEMENT_TYPE_I1:
        if (cbValue < 1)
        {
            IfFailGo(CLDB_E_FILE_CORRUPT);
        }
        pMDDefaultValue->m_cValue = *((CHAR *) pValue);
        break;
    case ELEMENT_TYPE_U1:
        if (cbValue < 1)
        {
            IfFailGo(CLDB_E_FILE_CORRUPT);
        }
        pMDDefaultValue->m_byteValue = *((BYTE *) pValue);
        break;
    case ELEMENT_TYPE_I2:
        if (cbValue < 2)
        {
            IfFailGo(CLDB_E_FILE_CORRUPT);
        }
        pMDDefaultValue->m_sValue = GET_UNALIGNED_VAL16(pValue);
        break;
    case ELEMENT_TYPE_U2:
    case ELEMENT_TYPE_CHAR:
        if (cbValue < 2)
        {
            IfFailGo(CLDB_E_FILE_CORRUPT);
        }
        pMDDefaultValue->m_usValue = GET_UNALIGNED_VAL16(pValue);
        break;
    case ELEMENT_TYPE_I4:
        if (cbValue < 4)
        {
            IfFailGo(CLDB_E_FILE_CORRUPT);
        }
        pMDDefaultValue->m_lValue = GET_UNALIGNED_VAL32(pValue);
        break;
    case ELEMENT_TYPE_U4:
        if (cbValue < 4)
        {
            IfFailGo(CLDB_E_FILE_CORRUPT);
        }
        pMDDefaultValue->m_ulValue = GET_UNALIGNED_VAL32(pValue);
        break;
    case ELEMENT_TYPE_R4:
        {
            if (cbValue < 4)
            {
                IfFailGo(CLDB_E_FILE_CORRUPT);
            }
            __int32 Value = GET_UNALIGNED_VAL32(pValue);
            pMDDefaultValue->m_fltValue = (float &)Value;
        }
        break;
    case ELEMENT_TYPE_R8:
        {
            if (cbValue < 8)
            {
                IfFailGo(CLDB_E_FILE_CORRUPT);
            }
            __int64 Value = GET_UNALIGNED_VAL64(pValue);
            pMDDefaultValue->m_dblValue = (double &) Value;
        }
        break;
    case ELEMENT_TYPE_STRING:
        if (cbValue == 0)
            pValue = NULL;

#if BIGENDIAN
        {
            // We need to allocate and swap the string if we're on a big endian
            // This allocation will be freed by the MDDefaultValue destructor.
            pMDDefaultValue->m_wzValue = new WCHAR[(cbValue + 1) / sizeof (WCHAR)];
            IfNullGo(pMDDefaultValue->m_wzValue);
            memcpy(const_cast<WCHAR *>(pMDDefaultValue->m_wzValue), pValue, cbValue);
            _ASSERTE(cbValue % sizeof(WCHAR) == 0);
            SwapStringLength(const_cast<WCHAR *>(pMDDefaultValue->m_wzValue), cbValue / sizeof(WCHAR));
        }
#else
        pMDDefaultValue->m_wzValue = (LPWSTR) pValue;
#endif
        break;
    case ELEMENT_TYPE_CLASS:
        //
        // There is only a 4-byte quantity in the MetaData, and it must always
        // be zero.  So, we load an INT32 and zero-extend it to be pointer-sized.
        //
        if (cbValue < 4)
        {
            IfFailGo(CLDB_E_FILE_CORRUPT);
        }
        pMDDefaultValue->m_unkValue = (IUnknown *)(UINT_PTR)GET_UNALIGNED_VAL32(pValue);
        if (pMDDefaultValue->m_unkValue != NULL)
        {
            _ASSERTE(!"Non-NULL objectref's are not supported as default values!");
            IfFailGo(CLDB_E_FILE_CORRUPT);
        }
        break;
    case ELEMENT_TYPE_I8:
        if (cbValue < 8)
        {
            IfFailGo(CLDB_E_FILE_CORRUPT);
        }
        pMDDefaultValue->m_llValue = GET_UNALIGNED_VAL64(pValue);
        break;
    case ELEMENT_TYPE_U8:
        if (cbValue < 8)
        {
            IfFailGo(CLDB_E_FILE_CORRUPT);
        }
        pMDDefaultValue->m_ullValue = GET_UNALIGNED_VAL64(pValue);
        break;
    case ELEMENT_TYPE_VOID:
        break;
    default:
        _ASSERTE(!"BAD TYPE!");
        IfFailGo(CLDB_E_FILE_CORRUPT);
        break;
    }
ErrExit:
    return hr;
} // _FillMDDefaultValue

//*****************************************************************************
// Given a scope, return the table size and table ptr for a given index
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::GetTableInfoWithIndex(     // return size
    ULONG  index,                // [IN] pass in the index
    void **pTable,               // [OUT] pointer to table at index
    void **pTableSize)           // [OUT] size of table at index
{
    _ASSERTE(!"NYI");
    return E_NOTIMPL;
} // MDInternalRO::GetTableInfoWithIndex



//*****************************************************************************
// Given a delta metadata byte stream, apply the changes to the current metadata
// object returning the resulting metadata object in ppv
//*****************************************************************************
__checkReturn
HRESULT MDInternalRO::ApplyEditAndContinue(
    void        *pDeltaMD,              // [IN] the delta metadata
    ULONG       cbDeltaMD,              // [IN] length of pData
    IMDInternalImport **ppv)            // [OUT] the resulting metadata interface
{
    _ASSERTE(pDeltaMD);
    _ASSERTE(ppv);

    HRESULT hr = E_FAIL;

    IMDInternalImportENC *pDeltaMDImport = NULL;

    IfFailGo(GetInternalWithRWFormat(pDeltaMD, cbDeltaMD, 0, IID_IMDInternalImportENC, (void**)&pDeltaMDImport));

    *ppv = this;
    IfFailGo(MDApplyEditAndContinue(ppv, pDeltaMDImport));

ErrExit:
    if (pDeltaMDImport)
        pDeltaMDImport->Release();
    return hr;
}

HRESULT MDInternalRO::GetRvaOffsetData(
    DWORD   *pFirstMethodRvaOffset,     // [OUT] Offset (from start of metadata) to the first RVA field in MethodDef table.
    DWORD   *pMethodDefRecordSize,      // [OUT] Size of each record in MethodDef table.
    DWORD   *pMethodDefCount,           // [OUT] Number of records in MethodDef table.
    DWORD   *pFirstFieldRvaOffset,      // [OUT] Offset (from start of metadata) to the first RVA field in FieldRVA table.
    DWORD   *pFieldRvaRecordSize,       // [OUT] Size of each record in FieldRVA table.
    DWORD   *pFieldRvaCount)            // [OUT] Number of records in FieldRVA table.
{
    HRESULT hr = S_OK;
    DWORD methodDefCount = *pMethodDefCount = m_LiteWeightStgdb.m_MiniMd.getCountMethods();
    if (methodDefCount == 0)
        *pFirstMethodRvaOffset = *pMethodDefRecordSize = 0;
    else
    {
        MethodRec *pMethodRec;
        IfFailGo(m_LiteWeightStgdb.m_MiniMd.GetMethodRecord(1, &pMethodRec));

        // RVA is the first column of the MethodDef table, so the address of MethodRec is also address of RVA column.
        if ((const BYTE *)m_LiteWeightStgdb.m_pvMd > (const BYTE *)pMethodRec)
        {
            Debug_ReportError("Stream header is not within MetaData block.");
            IfFailGo(CLDB_E_FILE_CORRUPT);
        }
        *pFirstMethodRvaOffset = (DWORD)((const BYTE *)pMethodRec - (const BYTE *)m_LiteWeightStgdb.m_pvMd);
        *pMethodDefRecordSize = m_LiteWeightStgdb.m_MiniMd._CBREC(Method);
    }

    {
        DWORD fieldRvaCount = *pFieldRvaCount = m_LiteWeightStgdb.m_MiniMd.getCountFieldRVAs();
        if (fieldRvaCount == 0)
            *pFirstFieldRvaOffset = *pFieldRvaRecordSize = 0;
        else
        {

            // orig
            // FieldRVARec *pFieldRVARec = m_LiteWeightStgdb.m_MiniMd.getFieldRVA(1);
            FieldRVARec *pFieldRVARec;
            IfFailGo(m_LiteWeightStgdb.m_MiniMd.GetFieldRVARecord(1, &pFieldRVARec));

//FieldRVARec *pFieldRVARec;
//mdToken fakeTok = 1;
//RidToToken(&fakeTok, mdtFieldDef);
//GetFieldRVA(fakeTok, &pFieldRVARec);
            // RVA is the first column of the FieldRVA table, so the address of FieldRVARec is also address of RVA column.
            if ((const BYTE *)m_LiteWeightStgdb.m_pvMd > (const BYTE *)pFieldRVARec)
            {
                Debug_ReportError("Stream header is not within MetaData block.");
                IfFailGo(CLDB_E_FILE_CORRUPT);
            }
            *pFirstFieldRvaOffset = (DWORD)((const BYTE *)pFieldRVARec - (const BYTE *)m_LiteWeightStgdb.m_pvMd);
            *pFieldRvaRecordSize = m_LiteWeightStgdb.m_MiniMd._CBREC(FieldRVA);
        }
    }
    hr = S_OK;

ErrExit:
    return hr;
}

#endif //FEATURE_METADATA_INTERNAL_APIS
