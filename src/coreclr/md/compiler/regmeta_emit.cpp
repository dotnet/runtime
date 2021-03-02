// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: RegMeta_IMetaDataImport.cpp
//

//
// Some methods of code:RegMeta class which implement public API interfaces:
//  * code:IMetaDataEmit
//  * code:IMetaDataEmit2
//
// ======================================================================================

#include "stdafx.h"
#include "regmeta.h"
#include "metadata.h"
#include "corerror.h"
#include "mdutil.h"
#include "rwutil.h"
#include "mdlog.h"
#include "importhelper.h"
#include "filtermanager.h"
#include "mdperf.h"
#include "switches.h"
#include "posterror.h"
#include "stgio.h"
#include "sstring.h"

#include <metamodelrw.h>

#define DEFINE_CUSTOM_NODUPCHECK    1
#define DEFINE_CUSTOM_DUPCHECK      2
#define SET_CUSTOM                  3

#if defined(_DEBUG) && defined(_TRACE_REMAPS)
#define LOGGING
#endif
#include <log.h>

#ifdef _MSC_VER
#pragma warning(disable: 4102)
#endif

#ifdef FEATURE_METADATA_EMIT

//*****************************************************************************
// Set module properties on a scope.
//*****************************************************************************
STDMETHODIMP RegMeta::SetModuleProps(   // S_OK or error.
    LPCWSTR     szName)                 // [IN] If not NULL, the name to set.
{
    HRESULT     hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    ModuleRec   *pModule;               // The module record to modify.

    LOG((LOGMD, "RegMeta::SetModuleProps(%S)\n", MDSTR(szName)));


    START_MD_PERF()
    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    IfFailGo(m_pStgdb->m_MiniMd.GetModuleRecord(1, &pModule));
    if (szName != NULL)
    {
        LPCWSTR szFile = NULL;
        size_t  cchFile;

        SplitPathInterior(szName, NULL, 0, NULL, 0, &szFile, &cchFile, NULL, 0);
        IfFailGo(m_pStgdb->m_MiniMd.PutStringW(TBL_Module, ModuleRec::COL_Name, pModule, szFile));
    }

    IfFailGo(UpdateENCLog(TokenFromRid(1, mdtModule)));

ErrExit:

    STOP_MD_PERF(SetModuleProps);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::SetModuleProps()

//*****************************************************************************
// Saves a scope to a file of a given name.
//*****************************************************************************
STDMETHODIMP RegMeta::Save(                     // S_OK or error.
    LPCWSTR     szFile,                 // [IN] The filename to save to.
    DWORD       dwSaveFlags)            // [IN] Flags for the save.
{
    HRESULT     hr=S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    LOG((LOGMD, "RegMeta::Save(%S, 0x%08x)\n", MDSTR(szFile), dwSaveFlags));
    START_MD_PERF()
    LOCKWRITE();

    // Check reserved param..
    if (dwSaveFlags != 0)
        IfFailGo (E_INVALIDARG);
    IfFailGo(PreSave());
    IfFailGo(m_pStgdb->Save(szFile, dwSaveFlags));

    // Reset m_bSaveOptimized, this is to handle the incremental and ENC
    // scenerios where one may do multiple saves.
    _ASSERTE(m_bSaveOptimized && !m_pStgdb->m_MiniMd.IsPreSaveDone());
    m_bSaveOptimized = false;

#if defined(_DEBUG)
    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_MD_RegMetaDump))
    {
        int DumpMD_impl(RegMeta *pMD);
        DumpMD_impl(this);
    }
#endif // _DEBUG

ErrExit:

    STOP_MD_PERF(Save);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::Save()

//*****************************************************************************
// Saves a scope to a stream.
//*****************************************************************************
STDMETHODIMP RegMeta::SaveToStream(     // S_OK or error.
    IStream     *pIStream,              // [IN] A writable stream to save to.
    DWORD       dwSaveFlags)            // [IN] Flags for the save.
{
    HRESULT     hr=S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    LOCKWRITE();

    LOG((LOGMD, "RegMeta::SaveToStream(0x%08x, 0x%08x)\n", pIStream, dwSaveFlags));
    START_MD_PERF()

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    hr = _SaveToStream(pIStream, dwSaveFlags);

    STOP_MD_PERF(SaveToStream);

#if defined(_DEBUG)
    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_MD_RegMetaDump))
    {
        int DumpMD_impl(RegMeta *pMD);
        DumpMD_impl(this);
    }
#endif // _DEBUG

ErrExit:

    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::SaveToStream()

//*****************************************************************************
// Saves a scope to a stream.
//*****************************************************************************
HRESULT RegMeta::_SaveToStream(         // S_OK or error.
    IStream     *pIStream,              // [IN] A writable stream to save to.
    DWORD       dwSaveFlags)            // [IN] Flags for the save.
{
    HRESULT     hr=S_OK;

    IfFailGo(PreSave());
    IfFailGo( m_pStgdb->SaveToStream(pIStream, m_ReorderingOptions, m_pCorProfileData) );

    // Reset m_bSaveOptimized, this is to handle the incremental and ENC
    // scenerios where one may do multiple saves.
    _ASSERTE(m_bSaveOptimized && !m_pStgdb->m_MiniMd.IsPreSaveDone());
    m_bSaveOptimized = false;

ErrExit:

    return hr;
} // STDMETHODIMP RegMeta::_SaveToStream()

//*****************************************************************************
// Saves a copy of the scope into the memory buffer provided.  The buffer size
// must be at least as large as the GetSaveSize value.
//*****************************************************************************
STDMETHODIMP RegMeta::SaveToMemory(           // S_OK or error.
    void        *pbData,                // [OUT] Location to write data.
    ULONG       cbData)                 // [IN] Max size of data buffer.
{
    HRESULT     hr;

    BEGIN_ENTRYPOINT_NOTHROW;

    IStream     *pStream = 0;           // Working pointer for save.

    LOG((LOGMD, "MD RegMeta::SaveToMemory(0x%08x, 0x%08x)\n",
        pbData, cbData));
    START_MD_PERF();

#ifdef _DEBUG
    ULONG       cbActual;               // Size of the real data.
    IfFailGo(GetSaveSize(cssAccurate, &cbActual));
    _ASSERTE(cbData >= cbActual);
#endif

    { // cannot lock before the debug statement. Because GetSaveSize is also a public API which will take the Write lock.
        LOCKWRITE();
        IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());
        // Create a stream interface on top of the user's data buffer, then simply
        // call the save to stream method.
        IfFailGo(CInMemoryStream::CreateStreamOnMemory(pbData, cbData, &pStream));
        IfFailGo(_SaveToStream(pStream, 0));
    }
ErrExit:
    if (pStream)
        pStream->Release();
    STOP_MD_PERF(SaveToMemory);
    END_ENTRYPOINT_NOTHROW;

    return (hr);
} // STDMETHODIMP RegMeta::SaveToMemory()

//*****************************************************************************
// As the Stgdb object to get the save size for the scope.
//*****************************************************************************
STDMETHODIMP RegMeta::GetSaveSize(      // S_OK or error.
    CorSaveSize fSave,                  // [IN] cssAccurate or cssQuick.
    DWORD      *pdwSaveSize)            // [OUT] Put the size here.
{
    HRESULT     hr=S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    FilterTable *ft = NULL;

    LOG((LOGMD, "RegMeta::GetSaveSize(0x%08x, 0x%08x)\n", fSave, pdwSaveSize));
    START_MD_PERF();
    LOCKWRITE();

    ft = m_pStgdb->m_MiniMd.GetFilterTable();
    IfNullGo(ft);

    if (m_pStgdb->m_MiniMd.m_UserStringHeap.GetUnalignedSize() == 0)
    {
        if (!IsENCDelta(m_pStgdb->m_MiniMd.m_OptionValue.m_UpdateMode) &&
            !m_pStgdb->m_MiniMd.IsMinimalDelta())
        {
            BYTE   rgData[] = {' ', 0, 0};
            UINT32 nIndex;
            IfFailGo(m_pStgdb->m_MiniMd.PutUserString(
                MetaData::DataBlob(rgData, sizeof(rgData)),
                &nIndex));
            // Make sure this user string is marked
            if (ft->Count() != 0)
            {
                IfFailGo( m_pFilterManager->MarkNewUserString(TokenFromRid(nIndex, mdtString)));
            }
        }
    }


    if (ft->Count() != 0)
    {
        int iCount;

        // There is filter table. Linker is using /opt:ref.
        // Make sure that we are marking the AssemblyDef token!
        iCount = m_pStgdb->m_MiniMd.getCountAssemblys();
        _ASSERTE(iCount <= 1);

        if (iCount)
        {
            IfFailGo(m_pFilterManager->Mark(TokenFromRid(iCount, mdtAssembly)));
        }
    }

    IfFailGo(PreSave());

    hr = m_pStgdb->GetSaveSize(fSave, (UINT32 *)pdwSaveSize, m_ReorderingOptions, m_pCorProfileData);

ErrExit:
    STOP_MD_PERF(GetSaveSize);

    END_ENTRYPOINT_NOTHROW;

    return hr;
} // RegMeta::GetSaveSize

#ifdef FEATURE_METADATA_EMIT_ALL

//*****************************************************************************
// Unmark everything in this module
//
// Implements public API code:IMetaDataFilter::UnmarkAll.
//*****************************************************************************
HRESULT RegMeta::UnmarkAll()
{
    HRESULT         hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;
    int             i;
    int             iCount;
    TypeDefRec      *pRec;
    RID             ulEncloser;
    NestedClassRec  *pNestedClass;
    CustomAttributeRec  *pCARec;
    mdToken         tkParent;
    int             iStart, iEnd;

    LOG((LOGMD, "RegMeta::UnmarkAll\n"));

    START_MD_PERF();
    LOCKWRITE();

#if 0
    // We cannot enable this check. Because our tests are depending on this.. Sigh..
    if (m_pFilterManager != NULL)
    {
        // UnmarkAll has been called before
        IfFailGo( META_E_HAS_UNMARKALL );
    }
#endif // 0

    // calculate the TypeRef and TypeDef mapping here
    //
    IfFailGo( RefToDefOptimization() );

    // unmark everything in the MiniMd.
    IfFailGo( m_pStgdb->m_MiniMd.UnmarkAll() );

    // instantiate the filter manager
    m_pFilterManager = new (nothrow) FilterManager( &(m_pStgdb->m_MiniMd) );
    IfNullGo( m_pFilterManager );

    // Mark all public typedefs.
    iCount = m_pStgdb->m_MiniMd.getCountTypeDefs();

    // Mark all of the public TypeDef. We need to skip over the <Module> typedef
    for (i = 2; i <= iCount; i++)
    {
        IfFailGo(m_pStgdb->m_MiniMd.GetTypeDefRecord(i, &pRec));
        if (m_OptionValue.m_LinkerOption == MDNetModule)
        {
            // Client is asking us to keep private type as well.
            IfFailGo( m_pFilterManager->Mark(TokenFromRid(i, mdtTypeDef)) );
        }
        else if (i != 1)
        {
            // when client is not set to MDNetModule, global functions/fields won't be keep by default
            //
            if (IsTdPublic(pRec->GetFlags()))
            {
                IfFailGo( m_pFilterManager->Mark(TokenFromRid(i, mdtTypeDef)) );
            }
            else if ( IsTdNestedPublic(pRec->GetFlags()) ||
                      IsTdNestedFamily(pRec->GetFlags()) ||
                      IsTdNestedFamORAssem(pRec->GetFlags()) )
            {
                // This nested class would potentially be visible outside, either
                // directly or through inheritence.  If the enclosing class is
                // marked, this nested class must be marked.
                //
                IfFailGo(m_pStgdb->m_MiniMd.FindNestedClassHelper(TokenFromRid(i, mdtTypeDef), &ulEncloser));
                _ASSERTE( !InvalidRid(ulEncloser) &&
                          "Bad metadata for nested type!" );
                IfFailGo(m_pStgdb->m_MiniMd.GetNestedClassRecord(ulEncloser, &pNestedClass));
                tkParent = m_pStgdb->m_MiniMd.getEnclosingClassOfNestedClass(pNestedClass);
                if ( m_pStgdb->m_MiniMd.GetFilterTable()->IsTypeDefMarked(tkParent))
                    IfFailGo( m_pFilterManager->Mark(TokenFromRid(i, mdtTypeDef)) );
            }
        }
    }

    if (m_OptionValue.m_LinkerOption == MDNetModule)
    {
        // Mark global function if NetModule. We will not keep _Delete method.
        IfFailGo(m_pStgdb->m_MiniMd.GetTypeDefRecord(1, &pRec));
        iStart = m_pStgdb->m_MiniMd.getMethodListOfTypeDef(pRec);
        IfFailGo(m_pStgdb->m_MiniMd.getEndMethodListOfTypeDef(1, (RID *)&iEnd));
        for ( i = iStart; i < iEnd; i ++ )
        {
            RID rid;
            MethodRec *pMethodRec;
            IfFailGo(m_pStgdb->m_MiniMd.GetMethodRid(i, &rid));
            IfFailGo(m_pStgdb->m_MiniMd.GetMethodRecord(rid, &pMethodRec));

            // check the name
            if (IsMdRTSpecialName(pMethodRec->GetFlags()))
            {
                LPCUTF8 szName;
                IfFailGo(m_pStgdb->m_MiniMd.getNameOfMethod(pMethodRec, &szName));

                // Only mark method if not a _Deleted method
                if (strcmp(szName, COR_DELETED_NAME_A) != 0)
                    IfFailGo( m_pFilterManager->Mark( TokenFromRid( rid, mdtMethodDef) ) );
            }
            else
            {
                //
            if (!IsMiForwardRef(pMethodRec->GetImplFlags()) ||
                    IsMiRuntime(pMethodRec->GetImplFlags())    ||
                    IsMdPinvokeImpl(pMethodRec->GetFlags()) )

                IfFailGo( m_pFilterManager->Mark( TokenFromRid( rid, mdtMethodDef) ) );
            }
        }
    }

    // mark the module property
    IfFailGo( m_pFilterManager->Mark(TokenFromRid(1, mdtModule)) );

    // We will also keep all of the TypeRef that has any CustomAttribute hang off it.
    iCount = m_pStgdb->m_MiniMd.getCountCustomAttributes();

    // Mark all of the TypeRef used by CA's
    for (i = 1; i <= iCount; i++)
    {
        IfFailGo(m_pStgdb->m_MiniMd.GetCustomAttributeRecord(i, &pCARec));
        tkParent = m_pStgdb->m_MiniMd.getParentOfCustomAttribute(pCARec);
        if (TypeFromToken(tkParent) == mdtTypeRef)
        {
            m_pFilterManager->Mark(tkParent);
        }
    }
ErrExit:

    STOP_MD_PERF(UnmarkAll);

    END_ENTRYPOINT_NOTHROW;

    return hr;
} // RegMeta::UnmarkAll

#endif //FEATURE_METADATA_EMIT_ALL

//*****************************************************************************
// Mark everything in this module
//*****************************************************************************
HRESULT RegMeta::MarkAll()
{
    HRESULT hr = NOERROR;

    // mark everything in the MiniMd.
    IfFailGo( m_pStgdb->m_MiniMd.MarkAll() );

    // instantiate the filter manager if not instantiated
    if (m_pFilterManager == NULL)
    {
        m_pFilterManager = new (nothrow) FilterManager( &(m_pStgdb->m_MiniMd) );
        IfNullGo( m_pFilterManager );
    }
ErrExit:

    return hr;
}   // HRESULT RegMeta::MarkAll

#ifdef FEATURE_METADATA_EMIT_ALL

//*****************************************************************************
// Mark the transitive closure of a token
//@todo GENERICS: What about GenericParam, MethodSpec?
//
// Implements public API code:IMetaDataFilter::MarkToken.
//*****************************************************************************
STDMETHODIMP RegMeta::MarkToken(        // Return code.
    mdToken     tk)                     // [IN] token to be Marked
{
    HRESULT     hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    // LOG((LOGMD, "RegMeta::MarkToken(0x%08x)\n", tk));
    START_MD_PERF();
    LOCKWRITE();

    if (m_pStgdb->m_MiniMd.GetFilterTable() == NULL || m_pFilterManager == NULL)
    {
        // UnmarkAll has not been called. Everything is considered marked.
        // No need to do anything extra!
        IfFailGo( META_E_MUST_CALL_UNMARKALL );
    }

    switch ( TypeFromToken(tk) )
    {
    case mdtTypeDef:
    case mdtMethodDef:
    case mdtFieldDef:
    case mdtMemberRef:
    case mdtTypeRef:
    case mdtTypeSpec:
    case mdtMethodSpec:
    case mdtSignature:
    case mdtString:
#if _DEBUG
        if (TypeFromToken(tk) == mdtTypeDef)
        {
            TypeDefRec   *pType;
            IfFailGo(m_pStgdb->m_MiniMd.GetTypeDefRecord(RidFromToken(tk), &pType));
            LPCSTR szTypeDefName;
            if (m_pStgdb->m_MiniMd.getNameOfTypeDef(pType, &szTypeDefName) == S_OK)
            {
                LOG((LOGMD, "MarkToken: Host is marking typetoken 0x%08x with name <%s>\n", tk, szTypeDefName));
            }
        }
        else
        if (TypeFromToken(tk) == mdtMethodDef)
        {
            MethodRec   *pMeth;
            IfFailGo(m_pStgdb->m_MiniMd.GetMethodRecord(RidFromToken(tk), &pMeth));
            LPCSTR szMethodName;
            if (m_pStgdb->m_MiniMd.getNameOfMethod(pMeth, &szMethodName) == S_OK)
            {
                LOG((LOGMD, "MarkToken: Host is marking methodtoken 0x%08x with name <%s>\n", tk, szMethodName));
            }
        }
        else
        if (TypeFromToken(tk) == mdtFieldDef)
        {
            FieldRec   *pField;
            IfFailGo(m_pStgdb->m_MiniMd.GetFieldRecord(RidFromToken(tk), &pField));
            LPCSTR szFieldName;
            if (m_pStgdb->m_MiniMd.getNameOfField(pField, &szFieldName) == S_OK)
            {
                LOG((LOGMD, "MarkToken: Host is marking field token 0x%08x with name <%s>\n", tk, szFieldName));
            }
        }
        else
        {
            LOG((LOGMD, "MarkToken: Host is marking token 0x%08x\n", tk));
        }
#endif // _DEBUG
        if (!IsValidToken(tk))
            IfFailGo( E_INVALIDARG );

        IfFailGo( m_pFilterManager->Mark(tk) );
        break;

    case mdtBaseType:
        // no need to mark base type
        goto ErrExit;

    default:
        _ASSERTE(!"Bad token type!");
        hr = E_INVALIDARG;
        break;
    }
ErrExit:

    STOP_MD_PERF(MarkToken);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // RegMeta::MarkToken

//*****************************************************************************
// Unmark everything in this module
//@todo GENERICS: What about GenericParam, MethodSpec?
//
// Implements public API code:IMetaDataFilter::IsTokenMarked.
//*****************************************************************************
HRESULT RegMeta::IsTokenMarked(
    mdToken     tk,                 // [IN] Token to check if marked or not
    BOOL        *pIsMarked)         // [OUT] true if token is marked
{
    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    FilterTable *pFilter = NULL;

    LOG((LOGMD, "RegMeta::IsTokenMarked(0x%08x)\n", tk));
    START_MD_PERF();
    LOCKREAD();

    pFilter = m_pStgdb->m_MiniMd.GetFilterTable();
    IfNullGo( pFilter );

    if (!IsValidToken(tk))
        IfFailGo( E_INVALIDARG );

    switch ( TypeFromToken(tk) )
    {
    case mdtTypeRef:
        *pIsMarked = pFilter->IsTypeRefMarked(tk);
        break;
    case mdtTypeDef:
        *pIsMarked = pFilter->IsTypeDefMarked(tk);
        break;
    case mdtFieldDef:
        *pIsMarked = pFilter->IsFieldMarked(tk);
        break;
    case mdtMethodDef:
        *pIsMarked = pFilter->IsMethodMarked(tk);
        break;
    case mdtParamDef:
        *pIsMarked = pFilter->IsParamMarked(tk);
        break;
    case mdtMemberRef:
        *pIsMarked = pFilter->IsMemberRefMarked(tk);
        break;
    case mdtCustomAttribute:
        *pIsMarked = pFilter->IsCustomAttributeMarked(tk);
        break;
    case mdtPermission:
        *pIsMarked = pFilter->IsDeclSecurityMarked(tk);
        break;
    case mdtSignature:
        *pIsMarked = pFilter->IsSignatureMarked(tk);
        break;
    case mdtEvent:
        *pIsMarked = pFilter->IsEventMarked(tk);
        break;
    case mdtProperty:
        *pIsMarked = pFilter->IsPropertyMarked(tk);
        break;
    case mdtModuleRef:
        *pIsMarked = pFilter->IsModuleRefMarked(tk);
        break;
    case mdtTypeSpec:
        *pIsMarked = pFilter->IsTypeSpecMarked(tk);
        break;
    case mdtInterfaceImpl:
        *pIsMarked = pFilter->IsInterfaceImplMarked(tk);
        break;
    case mdtString:
    default:
        _ASSERTE(!"Bad token type!");
        hr = E_INVALIDARG;
        break;
    }
ErrExit:

    STOP_MD_PERF(IsTokenMarked);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // RegMeta::IsTokenMarked

#endif //FEATURE_METADATA_EMIT_ALL

//*****************************************************************************
// Create and populate a new TypeDef record.
//*****************************************************************************
STDMETHODIMP RegMeta::DefineTypeDef(                // S_OK or error.
    LPCWSTR     szTypeDef,              // [IN] Name of TypeDef
    DWORD       dwTypeDefFlags,         // [IN] CustomAttribute flags
    mdToken     tkExtends,              // [IN] extends this TypeDef or typeref
    mdToken     rtkImplements[],        // [IN] Implements interfaces
    mdTypeDef   *ptd)                   // [OUT] Put TypeDef token here
{
    HRESULT     hr = S_OK;              // A result.

    BEGIN_ENTRYPOINT_NOTHROW;

    LOG((LOGMD, "RegMeta::DefineTypeDef(%S, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
            MDSTR(szTypeDef), dwTypeDefFlags, tkExtends,
            rtkImplements, ptd));
    START_MD_PERF();
    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    _ASSERTE(!IsTdNested(dwTypeDefFlags));

    IfFailGo(_DefineTypeDef(szTypeDef, dwTypeDefFlags,
                tkExtends, rtkImplements, mdTokenNil, ptd));
ErrExit:
    STOP_MD_PERF(DefineTypeDef);

    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::DefineTypeDef()


//*****************************************************************************
// Implements public API code:IMetaDataFilter::SetHandler.
//*****************************************************************************
STDMETHODIMP RegMeta::SetHandler(       // S_OK.
    IUnknown    *pUnk)                  // [IN] The new error handler.
{
    HRESULT     hr = S_OK;              // A result.

    BEGIN_ENTRYPOINT_NOTHROW;

    IMapToken *pIMap = NULL;

    LOG((LOGMD, "RegMeta::SetHandler(0x%08x)\n", pUnk));
    START_MD_PERF();
    LOCKWRITE();

    m_pHandler = pUnk;

    // Ignore the error return by SetHandler
    IfFailGo(m_pStgdb->m_MiniMd.SetHandler(pUnk));

    // Figure out up front if remap is supported.
    if (pUnk)
        pUnk->QueryInterface(IID_IMapToken, (PVOID *) &pIMap);
    m_bRemap = (pIMap != 0);
    if (pIMap)
        pIMap->Release();

ErrExit:

    STOP_MD_PERF(SetHandler);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::SetHandler()

//*******************************************************************************
// Internal helper functions.
//*******************************************************************************

//*******************************************************************************
// Perform optimizations of the metadata prior to saving.
//*******************************************************************************
HRESULT RegMeta::PreSave()              // Return code.
{
    HRESULT     hr = S_OK;              // A result.
    CMiniMdRW   *pMiniMd;               // The MiniMd with the data.
    unsigned    bRemapOld = m_bRemap;

    // For convenience.
    pMiniMd = &(m_pStgdb->m_MiniMd);

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    // If the code has already been optimized there is nothing to do.
    if (m_bSaveOptimized)
        goto ErrExit;

    IfFailGo(RefToDefOptimization());

    // we need to update MethodImpl table here with ref to def result
    if (pMiniMd->GetMemberRefToMemberDefMap() != NULL)
    {
        MethodImplRec *pMethodImplRec;
        mdToken        tkMethodBody;
        mdToken        tkMethodDecl;
        mdToken        newTK;
        ULONG          cMethodImplRecs;    // Count of MemberRefs.
        ULONG          iMI;

        cMethodImplRecs = pMiniMd->getCountMethodImpls();
        // Enum through all member ref's looking for ref's to internal things.
        for (iMI = 1; iMI <= cMethodImplRecs; iMI++)
        {   // Get a MethodImpl.
            IfFailGo(pMiniMd->GetMethodImplRecord(iMI, &pMethodImplRec));
            tkMethodBody = pMiniMd->getMethodBodyOfMethodImpl(pMethodImplRec);
            if (TypeFromToken(tkMethodBody) == mdtMemberRef)
            {
                // did it get remapped to a def
                newTK = *(pMiniMd->GetMemberRefToMemberDefMap()->Get(RidFromToken(tkMethodBody)));
                if (!IsNilToken(newTK))
                {
                    // yes... fix up the value...
                    IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_MethodImpl,
                        MethodImplRec::COL_MethodBody,
                        pMethodImplRec,
                        newTK));
                }
            }
            // do the same thing for MethodDecl
            tkMethodDecl = pMiniMd->getMethodDeclarationOfMethodImpl(pMethodImplRec);
            if (TypeFromToken(tkMethodDecl) == mdtMemberRef)
            {
                // did it get remapped to a def
                newTK = *(pMiniMd->GetMemberRefToMemberDefMap()->Get(RidFromToken(tkMethodDecl)));
                if (!IsNilToken(newTK))
                {
                    // yes... fix up the value...
                    IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_MethodImpl,
                        MethodImplRec::COL_MethodDeclaration,
                        pMethodImplRec,
                        newTK));
                }
            }
        }
    }

    // reget the minimd because it can be swapped in the call of ProcessFilter
    pMiniMd = &(m_pStgdb->m_MiniMd);

    // Don't repeat this process again.
    m_bSaveOptimized = true;

    // call get save size to trigger the PreSaveXXX on MetaModelRW class.
    IfFailGo(m_pStgdb->m_MiniMd.PreSave(m_ReorderingOptions, m_pCorProfileData));

ErrExit:
    m_bRemap =  bRemapOld;

    return hr;
} // RegMeta::PreSave

//*******************************************************************************
// Perform optimizations of ref to def
//*******************************************************************************
HRESULT RegMeta::RefToDefOptimization()
{
    mdToken     mfdef;                  // Method or Field Def.
    LPCSTR      szName;                 // MemberRef or TypeRef name.
    const COR_SIGNATURE *pvSig;         // Signature of the MemberRef.
    ULONG       cbSig;                  // Size of the signature blob.
    HRESULT     hr = S_OK;              // A result.
    ULONG       iMR;                    // For iterating MemberRefs.
    CMiniMdRW   *pMiniMd;               // The MiniMd with the data.
    ULONG       cMemberRefRecs;         // Count of MemberRefs.
    MemberRefRec *pMemberRefRec;        // A MemberRefRec.



    START_MD_PERF();

    // the Ref to Def map is still up-to-date
    if (IsMemberDefDirty() == false && IsTypeDefDirty() == false && m_hasOptimizedRefToDef == true)
        goto ErrExit;

    pMiniMd = &(m_pStgdb->m_MiniMd);

    // The basic algorithm here is:
    //
    //      calculate all of the TypeRef to TypeDef map and store it at TypeRefToTypeDefMap
    //      for each MemberRef mr
    //      {
    //          get the parent of mr
    //          if (parent of mr is a TypeRef and has been mapped to a TypeDef)
    //          {
    //              Remap MemberRef to MemberDef
    //          }
    //      }
    //
    // There are several places where errors are eaten, since this whole thing is
    // an optimization step and not doing it would still be valid.
    //

    // Ensure the size
    // initialize the token remap manager. This class will track all of the Refs to Defs map and also
    // token movements due to removing pointer tables or sorting.
    //
    if ( pMiniMd->GetTokenRemapManager() == NULL)
    {

        IfFailGo( pMiniMd->InitTokenRemapManager() );
    }
    else
    {
        IfFailGo( pMiniMd->GetTokenRemapManager()->ClearAndEnsureCapacity(pMiniMd->getCountTypeRefs(), pMiniMd->getCountMemberRefs()));
    }

    // If this is the first time or more TypeDef has been introduced, recalculate the TypeRef to TypeDef map
    if (IsTypeDefDirty() || m_hasOptimizedRefToDef == false)
    {
        IfFailGo( pMiniMd->CalculateTypeRefToTypeDefMap() );
    }

    // If this is the first time or more memberdefs has been introduced, recalculate the TypeRef to TypeDef map
    if (IsMemberDefDirty() || m_hasOptimizedRefToDef == false)
    {
        mdToken     tkParent;
        cMemberRefRecs = pMiniMd->getCountMemberRefs();

        // Enum through all member ref's looking for ref's to internal things.
        for (iMR = 1; iMR<=cMemberRefRecs; iMR++)
        {   // Get a MemberRef.
            IfFailGo(pMiniMd->GetMemberRefRecord(iMR, &pMemberRefRec));

            // If not member of the TypeRef, skip it.
            tkParent = pMiniMd->getClassOfMemberRef(pMemberRefRec);

            if ( TypeFromToken(tkParent) == mdtMethodDef )
            {
                // always track the map even though it is already in the original scope
                *(pMiniMd->GetMemberRefToMemberDefMap()->Get(iMR)) =  tkParent;
                continue;
            }

            if ( TypeFromToken(tkParent) != mdtTypeRef && TypeFromToken(tkParent) != mdtTypeDef )
            {
                // this has been either optimized to mdtMethodDef, mdtFieldDef or referring to
                // ModuleRef
                continue;
            }

            // In the case of global function, we have tkParent as m_tdModule.
            // We will always do the optmization.
            if (TypeFromToken(tkParent) == mdtTypeRef)
            {
                // If we're preserving local typerefs, skip this token
                if (PreserveLocalRefs(MDPreserveLocalTypeRef))
                {
                    continue;
                }

                // The parent is a TypeRef. We need to check to see if this TypeRef is optimized to a TypeDef
                tkParent = *(pMiniMd->GetTypeRefToTypeDefMap()->Get(RidFromToken(tkParent)) );
                // tkParent = pMapTypeRefToTypeDef[RidFromToken(tkParent)];
                if ( RidFromToken(tkParent) == 0)
                {
                    continue;
                }
            }

            // If we're preserving local memberrefs, skip this token
            if (PreserveLocalRefs(MDPreserveLocalMemberRef))
            {
                continue;
            }

            // Get the name and signature of this mr.
            IfFailGo(pMiniMd->getNameOfMemberRef(pMemberRefRec, &szName));
            IfFailGo(pMiniMd->getSignatureOfMemberRef(pMemberRefRec, &pvSig, &cbSig));

            // Look for a member with the same def.  Might not be found if it is
            // inherited from a base class.
            //<TODO>@future: this should support inheritence checking.
            // Look for a member with the same name and signature.</TODO>
            hr = ImportHelper::FindMember(pMiniMd, tkParent, szName, pvSig, cbSig, &mfdef);
            if (hr != S_OK)
            {
    #if _TRACE_REMAPS
            // Log the failure.
            LOG((LF_METADATA, LL_INFO10, "Member %S//%S.%S not found\n", szNamespace, szTDName, rcMRName));
    #endif
                continue;
            }

            // We will only record this if mfdef is a methoddef. We don't support
            // parent of MemberRef as fielddef. As if we can optimize MemberRef to FieldDef,
            // we can remove this row.
            //
            if ( (TypeFromToken(mfdef) == mdtMethodDef) &&
                  (m_bRemap || tkParent == m_tdModule ) )
            {
                // Always change the parent if it is the global function.
                // Or change the parent if we have a remap that we can send notification.
                //
                IfFailGo(pMiniMd->PutToken(TBL_MemberRef, MemberRefRec::COL_Class, pMemberRefRec, mfdef));
            }

            // We will always track the changes. In MiniMd::PreSaveFull, we will use this map to send
            // notification to our host if there is any IMapToken provided.
            //
            *(pMiniMd->GetMemberRefToMemberDefMap()->Get(iMR)) =  mfdef;

        } // EnumMemberRefs
    }

    // Reset return code from likely search failures.
    hr = S_OK;

    SetMemberDefDirty(false);
    SetTypeDefDirty(false);
    m_hasOptimizedRefToDef = true;
ErrExit:
    STOP_MD_PERF(RefToDefOptimization);

    return hr;
} // RegMeta::RefToDefOptimization

//*****************************************************************************
// Define a TypeRef given the fully qualified name.
//*****************************************************************************
HRESULT RegMeta::_DefineTypeRef(
    mdToken     tkResolutionScope,          // [IN] ModuleRef or AssemblyRef.
    const void  *szName,                    // [IN] Name of the TypeRef.
    BOOL        isUnicode,                  // [IN] Specifies whether the URL is unicode.
    mdTypeRef   *ptk,                       // [OUT] Put mdTypeRef here.
    eCheckDups  eCheck)                     // [IN] Specifies whether to check for duplicates.
{
    HRESULT     hr = S_OK;
    LPUTF8      szUTF8FullQualName;
    CQuickBytes qbNamespace;
    CQuickBytes qbName;
    int         bSuccess;
    ULONG       ulStringLen;




    _ASSERTE(ptk && szName);
    _ASSERTE (TypeFromToken(tkResolutionScope) == mdtModule ||
              TypeFromToken(tkResolutionScope) == mdtModuleRef ||
              TypeFromToken(tkResolutionScope) == mdtAssemblyRef ||
              TypeFromToken(tkResolutionScope) == mdtTypeRef ||
              tkResolutionScope == mdTokenNil);

    if (isUnicode)
    {
        UTF8STR((LPCWSTR)szName, szUTF8FullQualName);
    }
    else
    {
        szUTF8FullQualName = (LPUTF8)szName;
    }
    PREFIX_ASSUME(szUTF8FullQualName != NULL);

    ulStringLen = (ULONG)(strlen(szUTF8FullQualName) + 1);
    IfFailGo(qbNamespace.ReSizeNoThrow(ulStringLen));
    IfFailGo(qbName.ReSizeNoThrow(ulStringLen));
    bSuccess = ns::SplitPath(szUTF8FullQualName,
                             (LPUTF8)qbNamespace.Ptr(),
                             ulStringLen,
                             (LPUTF8)qbName.Ptr(),
                             ulStringLen);
    _ASSERTE(bSuccess);

    // Search for existing TypeRef record.
    if (eCheck==eCheckYes || (eCheck==eCheckDefault && CheckDups(MDDupTypeRef)))
    {
        hr = ImportHelper::FindTypeRefByName(&(m_pStgdb->m_MiniMd), tkResolutionScope,
                                             (LPCUTF8)qbNamespace.Ptr(),
                                             (LPCUTF8)qbName.Ptr(), ptk);
        if (SUCCEEDED(hr))
        {
            if (IsENCOn())
            {
                hr = S_OK;
                goto NormalExit;
            }
            else
            {
                hr = META_S_DUPLICATE;
                goto NormalExit;
            }
        }
        else if (hr != CLDB_E_RECORD_NOTFOUND)
            IfFailGo(hr);
    }

    // Create TypeRef record.
    TypeRefRec      *pRecord;
    RID             iRecord;

    IfFailGo(m_pStgdb->m_MiniMd.AddTypeRefRecord(&pRecord, &iRecord));

    // record the more defs are introduced.
    SetTypeDefDirty(true);

    // Give token back to caller.
    *ptk = TokenFromRid(iRecord, mdtTypeRef);

    // Set the fields of the TypeRef record.
    IfFailGo(m_pStgdb->m_MiniMd.PutString(TBL_TypeRef, TypeRefRec::COL_Namespace,
                        pRecord, (LPUTF8)qbNamespace.Ptr()));

    IfFailGo(m_pStgdb->m_MiniMd.PutString(TBL_TypeRef, TypeRefRec::COL_Name,
                        pRecord, (LPUTF8)qbName.Ptr()));

    if (!IsNilToken(tkResolutionScope))
        IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_TypeRef, TypeRefRec::COL_ResolutionScope,
                        pRecord, tkResolutionScope));
    IfFailGo(UpdateENCLog(*ptk));

    // Hash the name.
    IfFailGo(m_pStgdb->m_MiniMd.AddNamedItemToHash(TBL_TypeRef, *ptk, (LPUTF8)qbName.Ptr(), 0));

ErrExit:
    ;
NormalExit:

    return hr;
} // HRESULT RegMeta::_DefineTypeRef()

//*******************************************************************************
// Define MethodSemantics
//*******************************************************************************
HRESULT RegMeta::_DefineMethodSemantics(    // S_OK or error.
    USHORT      usAttr,                     // [IN] CorMethodSemanticsAttr.
    mdMethodDef md,                         // [IN] Method.
    mdToken     tkAssoc,                    // [IN] Association.
    BOOL        bClear)                     // [IN] Specifies whether to delete the exisiting entries.
{
    HRESULT      hr = S_OK;
    MethodSemanticsRec *pRecord = 0;
    MethodSemanticsRec *pRecord1;           // Use this to recycle a MethodSemantics record.
    RID         iRecord;
    HENUMInternal hEnum;



    _ASSERTE(TypeFromToken(md) == mdtMethodDef || IsNilToken(md));
    _ASSERTE(RidFromToken(tkAssoc));
    HENUMInternal::ZeroEnum(&hEnum);

    // Clear all matching records by setting association to a Nil token.
    if (bClear)
    {
        RID         i;

        IfFailGo( m_pStgdb->m_MiniMd.FindMethodSemanticsHelper(tkAssoc, &hEnum) );
        while (HENUMInternal::EnumNext(&hEnum, (mdToken *)&i))
        {
            IfFailGo(m_pStgdb->m_MiniMd.GetMethodSemanticsRecord(i, &pRecord1));
            if (usAttr == pRecord1->GetSemantic())
            {
                pRecord = pRecord1;
                iRecord = i;
                IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_MethodSemantics,
                    MethodSemanticsRec::COL_Association, pRecord, mdPropertyNil));
                // In Whidbey, we should create ENC log record here.
            }
        }
    }
    // If setting (not just clearing) the association, do that now.
    if (!IsNilToken(md))
    {
        // Create a new record required
        if (pRecord == NULL)
        {
            IfFailGo(m_pStgdb->m_MiniMd.AddMethodSemanticsRecord(&pRecord, &iRecord));
        }

        // Save the data.
        pRecord->SetSemantic(usAttr);
        IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_MethodSemantics,
                                             MethodSemanticsRec::COL_Method, pRecord, md));
        IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_MethodSemantics,
                                             MethodSemanticsRec::COL_Association, pRecord, tkAssoc));

        // regardless if we reuse the record or create the record, add the MethodSemantics to the hash
        IfFailGo( m_pStgdb->m_MiniMd.AddMethodSemanticsToHash(iRecord) );

        // Create log record for non-token table.
        IfFailGo(UpdateENCLog2(TBL_MethodSemantics, iRecord));
    }

ErrExit:
    HENUMInternal::ClearEnum(&hEnum);

    return hr;
} // HRESULT RegMeta::_DefineMethodSemantics()

//*******************************************************************************
// Turn the specified internal flags on.
//*******************************************************************************
HRESULT RegMeta::_TurnInternalFlagsOn(  // S_OK or error.
    mdToken     tkObj,                  // [IN] Target object whose internal flags are targetted.
    DWORD       flags)                  // [IN] Specifies flags to be turned on.
{
    HRESULT     hr;
    MethodRec  *pMethodRec;
    FieldRec   *pFieldRec;
    TypeDefRec *pTypeDefRec;

    switch (TypeFromToken(tkObj))
    {
    case mdtMethodDef:
        IfFailRet(m_pStgdb->m_MiniMd.GetMethodRecord(RidFromToken(tkObj), &pMethodRec));
        pMethodRec->AddFlags(flags);
        break;
    case mdtFieldDef:
        IfFailRet(m_pStgdb->m_MiniMd.GetFieldRecord(RidFromToken(tkObj), &pFieldRec));
        pFieldRec->AddFlags(flags);
        break;
    case mdtTypeDef:
        IfFailRet(m_pStgdb->m_MiniMd.GetTypeDefRecord(RidFromToken(tkObj), &pTypeDefRec));
        pTypeDefRec->AddFlags(flags);
        break;
    default:
        _ASSERTE(!"Not supported token type!");
        return E_INVALIDARG;
    }
    return S_OK;
} // RegMeta::_TurnInternalFlagsOn

//*****************************************************************************
// Helper: Set the properties on the given TypeDef token.
//*****************************************************************************
HRESULT RegMeta::_SetTypeDefProps(      // S_OK or error.
    mdTypeDef   td,                     // [IN] The TypeDef.
    DWORD       dwTypeDefFlags,         // [IN] TypeDef flags.
    mdToken     tkExtends,              // [IN] Base TypeDef or TypeRef.
    mdToken     rtkImplements[])        // [IN] Implemented interfaces.
{
    HRESULT     hr = S_OK;              // A result.
    BOOL        bClear = IsENCOn() || IsCallerExternal();   // Specifies whether to clear the InterfaceImpl records.
    TypeDefRec  *pRecord;               // New TypeDef record.

    _ASSERTE(TypeFromToken(td) == mdtTypeDef);
    _ASSERTE(TypeFromToken(tkExtends) == mdtTypeDef || TypeFromToken(tkExtends) == mdtTypeRef || TypeFromToken(tkExtends) == mdtTypeSpec ||
                IsNilToken(tkExtends) || tkExtends == UINT32_MAX);

    // Get the record.
    IfFailGo(m_pStgdb->m_MiniMd.GetTypeDefRecord(RidFromToken(td), &pRecord));

    if (dwTypeDefFlags != UINT32_MAX)
    {
        // No one should try to set the reserved flags explicitly.
        _ASSERTE((dwTypeDefFlags & (tdReservedMask&~tdRTSpecialName)) == 0);
        // Clear the reserved flags from the flags passed in.
        dwTypeDefFlags &= (~tdReservedMask);
        // Preserve the reserved flags stored.
        dwTypeDefFlags |= (pRecord->GetFlags() & tdReservedMask);
        // Set the flags.
        pRecord->SetFlags(dwTypeDefFlags);
    }
    if (tkExtends != UINT32_MAX)
    {
        if (IsNilToken(tkExtends))
            tkExtends = mdTypeDefNil;
        IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_TypeDef, TypeDefRec::COL_Extends,
                                             pRecord, tkExtends));
    }

    // Implemented interfaces.
    if (rtkImplements)
        IfFailGo(_SetImplements(rtkImplements, td, bClear));

    IfFailGo(UpdateENCLog(td));
ErrExit:
    return hr;
} // HRESULT RegMeta::_SetTypeDefProps()

//******************************************************************************
// Creates and sets a row in the InterfaceImpl table.  Optionally clear
// pre-existing records for the owning class.
//******************************************************************************
HRESULT RegMeta::_SetImplements(        // S_OK or error.
    mdToken     rTk[],                  // Array of TypeRef or TypeDef or TypeSpec tokens for implemented interfaces.
    mdTypeDef   td,                     // Implementing TypeDef.
    BOOL        bClear)                 // Specifies whether to clear the existing records.
{
    HRESULT     hr = S_OK;
    ULONG       i = 0;
    ULONG       j;
    InterfaceImplRec *pInterfaceImpl;
    RID         iInterfaceImpl;
    RID         ridStart;
    RID         ridEnd;
    CQuickBytes cqbTk;
    const mdToken *pTk;
    bool fIsTableVirtualSortValid;


    _ASSERTE(TypeFromToken(td) == mdtTypeDef && rTk);
    _ASSERTE(!m_bSaveOptimized && "Cannot change records after PreSave() and before Save().");

    // Clear all exising InterfaceImpl records by setting the parent to Nil.
    if (bClear)
    {
        IfFailGo(m_pStgdb->m_MiniMd.GetInterfaceImplsForTypeDef(
                                        RidFromToken(td), &ridStart, &ridEnd));
        for (j = ridStart; j < ridEnd; j++)
        {
            IfFailGo(m_pStgdb->m_MiniMd.GetInterfaceImplRecord(
                m_pStgdb->m_MiniMd.GetInterfaceImplRid(j),
                &pInterfaceImpl));
            _ASSERTE (td == m_pStgdb->m_MiniMd.getClassOfInterfaceImpl(pInterfaceImpl));
            IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_InterfaceImpl, InterfaceImplRec::COL_Class,
                                                 pInterfaceImpl, mdTypeDefNil));
        }
    }

    // Eliminate duplicates from the array passed in.
    if (CheckDups(MDDupInterfaceImpl))
    {
        IfFailGo(_InterfaceImplDupProc(rTk, td, &cqbTk));
        pTk = (mdToken *)cqbTk.Ptr();
    }
    else
        pTk = rTk;

    // Get the state of InterfaceImpl table's VirtualSort
    fIsTableVirtualSortValid = m_pStgdb->m_MiniMd.IsTableVirtualSorted(TBL_InterfaceImpl);
    // Loop for each implemented interface.
    while (!IsNilToken(pTk[i]))
    {
        _ASSERTE(TypeFromToken(pTk[i]) == mdtTypeRef || TypeFromToken(pTk[i]) == mdtTypeDef
               || TypeFromToken(pTk[i]) == mdtTypeSpec);

        // Create the interface implementation record.
        IfFailGo(m_pStgdb->m_MiniMd.AddInterfaceImplRecord(&pInterfaceImpl, &iInterfaceImpl));

        // Set data.
        IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_InterfaceImpl, InterfaceImplRec::COL_Class,
                                            pInterfaceImpl, td));
        IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_InterfaceImpl, InterfaceImplRec::COL_Interface,
                                            pInterfaceImpl, pTk[i]));
        // Had the table valid VirtualSort?
        if (fIsTableVirtualSortValid)
        {   // Validate table's VistualSort after adding 1 record and store its
            // new validation state
            IfFailGo(m_pStgdb->m_MiniMd.ValidateVirtualSortAfterAddRecord(
                TBL_InterfaceImpl,
                &fIsTableVirtualSortValid));
        }

        i++;

        IfFailGo(UpdateENCLog(TokenFromRid(mdtInterfaceImpl, iInterfaceImpl)));
    }
ErrExit:

    return hr;
} // HRESULT RegMeta::_SetImplements()

//******************************************************************************
// This routine eliminates duplicates from the given list of InterfaceImpl tokens
// to be defined.  It checks for duplicates against the database only if the
// TypeDef for which these tokens are being defined is not a new one.
//******************************************************************************
HRESULT RegMeta::_InterfaceImplDupProc( // S_OK or error.
    mdToken     rTk[],                  // Array of TypeRef or TypeDef or TypeSpec tokens for implemented interfaces.
    mdTypeDef   td,                     // Implementing TypeDef.
    CQuickBytes *pcqbTk)                // Quick Byte object for placing the array of unique tokens.
{
    HRESULT     hr = S_OK;
    ULONG       i = 0;
    ULONG       iUniqCount = 0;
    BOOL        bDupFound;

    while (!IsNilToken(rTk[i]))
    {
        _ASSERTE(TypeFromToken(rTk[i]) == mdtTypeRef || TypeFromToken(rTk[i]) == mdtTypeDef
              || TypeFromToken(rTk[i]) == mdtTypeSpec);
        bDupFound = false;

        // Eliminate duplicates from the input list of tokens by looking within the list.
        for (ULONG j = 0; j < iUniqCount; j++)
        {
            if (rTk[i] == ((mdToken *)pcqbTk->Ptr())[j])
            {
                bDupFound = true;
                break;
            }
        }

        // If no duplicate is found record it in the list.
        if (!bDupFound)
        {
            IfFailGo(pcqbTk->ReSizeNoThrow((iUniqCount+1) * sizeof(mdToken)));
            ((mdToken *)pcqbTk->Ptr())[iUniqCount] = rTk[i];
            iUniqCount++;
        }
        i++;
    }

    // Create a Nil token to signify the end of list.
    IfFailGo(pcqbTk->ReSizeNoThrow((iUniqCount+1) * sizeof(mdToken)));
    ((mdToken *)pcqbTk->Ptr())[iUniqCount] = mdTokenNil;
ErrExit:

    return hr;
} // HRESULT RegMeta::_InterfaceImplDupProc()

//*******************************************************************************
// helper to define event
//*******************************************************************************
HRESULT RegMeta::_DefineEvent(          // Return hresult.
    mdTypeDef   td,                     // [IN] the class/interface on which the event is being defined
    LPCWSTR     szEvent,                // [IN] Name of the event
    DWORD       dwEventFlags,           // [IN] CorEventAttr
    mdToken     tkEventType,            // [IN] a reference (mdTypeRef or mdTypeRef) to the Event class
    mdEvent     *pmdEvent)              // [OUT] output event token
{
    HRESULT     hr = S_OK;
    EventRec    *pEventRec = NULL;
    RID         iEventRec;
    EventMapRec *pEventMap;
    RID         iEventMap;
    mdEvent     mdEv;
    LPUTF8      szUTF8Event;
    UTF8STR(szEvent, szUTF8Event);
    PREFIX_ASSUME(szUTF8Event != NULL);



    _ASSERTE(TypeFromToken(td) == mdtTypeDef && td != mdTypeDefNil);
    _ASSERTE(IsNilToken(tkEventType) || TypeFromToken(tkEventType) == mdtTypeDef ||
                TypeFromToken(tkEventType) == mdtTypeRef || TypeFromToken(tkEventType) == mdtTypeSpec);
    _ASSERTE(szEvent && pmdEvent);

    if (CheckDups(MDDupEvent))
    {
        hr = ImportHelper::FindEvent(&(m_pStgdb->m_MiniMd), td, szUTF8Event, pmdEvent);
        if (SUCCEEDED(hr))
        {
            if (IsENCOn())
                IfFailGo(m_pStgdb->m_MiniMd.GetEventRecord(RidFromToken(*pmdEvent), &pEventRec));
            else
            {
                hr = META_S_DUPLICATE;
                goto ErrExit;
            }
        }
        else if (hr != CLDB_E_RECORD_NOTFOUND)
            IfFailGo(hr);
    }

    if (! pEventRec)
    {
        // Create a new map if one doesn't exist already, else retrieve the existing one.
        // The event map must be created before the EventRecord, the new event map will
        // be pointing past the first event record.
        IfFailGo(m_pStgdb->m_MiniMd.FindEventMapFor(RidFromToken(td), &iEventMap));
        if (InvalidRid(iEventMap))
        {
            // Create new record.
            IfFailGo(m_pStgdb->m_MiniMd.AddEventMapRecord(&pEventMap, &iEventMap));
            // Set parent.
            IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_EventMap,
                                            EventMapRec::COL_Parent, pEventMap, td));
            IfFailGo(UpdateENCLog2(TBL_EventMap, iEventMap));
        }
        else
        {
            IfFailGo(m_pStgdb->m_MiniMd.GetEventMapRecord(iEventMap, &pEventMap));
        }

        // Create a new event record.
        IfFailGo(m_pStgdb->m_MiniMd.AddEventRecord(&pEventRec, &iEventRec));

        // Set output parameter.
        *pmdEvent = TokenFromRid(iEventRec, mdtEvent);

        // Add Event to EventMap.
        IfFailGo(m_pStgdb->m_MiniMd.AddEventToEventMap(RidFromToken(iEventMap), iEventRec));

        IfFailGo(UpdateENCLog2(TBL_EventMap, iEventMap, CMiniMdRW::eDeltaEventCreate));
    }

    mdEv = *pmdEvent;

    // Set data
    IfFailGo(m_pStgdb->m_MiniMd.PutString(TBL_Event, EventRec::COL_Name, pEventRec, szUTF8Event));
    IfFailGo(_SetEventProps1(*pmdEvent, dwEventFlags, tkEventType));

    // Add the <Event token, typedef token> to the lookup table
    if (m_pStgdb->m_MiniMd.HasIndirectTable(TBL_Event))
        IfFailGo( m_pStgdb->m_MiniMd.AddEventToLookUpTable(*pmdEvent, td) );

    IfFailGo(UpdateENCLog(*pmdEvent));

ErrExit:

    return hr;
} // HRESULT RegMeta::_DefineEvent()


//******************************************************************************
// Set the specified properties on the Event Token.
//******************************************************************************
HRESULT RegMeta::_SetEventProps1(                // Return hresult.
    mdEvent     ev,                     // [IN] Event token.
    DWORD       dwEventFlags,           // [IN] Event flags.
    mdToken     tkEventType)            // [IN] Event type class.
{
    EventRec    *pRecord;
    HRESULT     hr = S_OK;

    _ASSERTE(TypeFromToken(ev) == mdtEvent && RidFromToken(ev));

    IfFailGo(m_pStgdb->m_MiniMd.GetEventRecord(RidFromToken(ev), &pRecord));
    if (dwEventFlags != UINT32_MAX)
    {
        // Don't let caller set reserved bits
        dwEventFlags &= ~evReservedMask;
        // Preserve reserved bits.
        dwEventFlags |= (pRecord->GetEventFlags() & evReservedMask);

        pRecord->SetEventFlags(static_cast<USHORT>(dwEventFlags));
    }
    if (!IsNilToken(tkEventType))
        IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_Event, EventRec::COL_EventType,
                                             pRecord, tkEventType));
ErrExit:
    return hr;
} // HRESULT RegMeta::_SetEventProps1()

//******************************************************************************
// Set the specified properties on the given Event token.
//******************************************************************************
HRESULT RegMeta::_SetEventProps2(                // Return hresult.
    mdEvent     ev,                     // [IN] Event token.
    mdMethodDef mdAddOn,                // [IN] Add method.
    mdMethodDef mdRemoveOn,             // [IN] Remove method.
    mdMethodDef mdFire,                 // [IN] Fire method.
    mdMethodDef rmdOtherMethods[],      // [IN] An array of other methods.
    BOOL        bClear)                 // [IN] Specifies whether to clear the existing MethodSemantics records.
{
    EventRec    *pRecord;
    HRESULT     hr = S_OK;



    _ASSERTE(TypeFromToken(ev) == mdtEvent && RidFromToken(ev));

    IfFailGo(m_pStgdb->m_MiniMd.GetEventRecord(RidFromToken(ev), &pRecord));

    // Remember the AddOn method.
    if (!IsNilToken(mdAddOn))
    {
        _ASSERTE(TypeFromToken(mdAddOn) == mdtMethodDef);
        IfFailGo(_DefineMethodSemantics(msAddOn, mdAddOn, ev, bClear));
    }

    // Remember the RemoveOn method.
    if (!IsNilToken(mdRemoveOn))
    {
        _ASSERTE(TypeFromToken(mdRemoveOn) == mdtMethodDef);
        IfFailGo(_DefineMethodSemantics(msRemoveOn, mdRemoveOn, ev, bClear));
    }

    // Remember the fire method.
    if (!IsNilToken(mdFire))
    {
        _ASSERTE(TypeFromToken(mdFire) == mdtMethodDef);
        IfFailGo(_DefineMethodSemantics(msFire, mdFire, ev, bClear));
    }

    // Store all of the other methods.
    if (rmdOtherMethods)
    {
        int         i = 0;
        mdMethodDef mb;

        while (1)
        {
            mb = rmdOtherMethods[i++];
            if (IsNilToken(mb))
                break;
            _ASSERTE(TypeFromToken(mb) == mdtMethodDef);
            IfFailGo(_DefineMethodSemantics(msOther, mb, ev, bClear));

            // The first call would've cleared all the existing ones.
            bClear = false;
        }
    }
ErrExit:

    return hr;
} // HRESULT RegMeta::_SetEventProps2()

//******************************************************************************
// Set Permission on the given permission token.
//******************************************************************************
HRESULT RegMeta::_SetPermissionSetProps(         // Return hresult.
    mdPermission tkPerm,                // [IN] Permission token.
    DWORD       dwAction,               // [IN] CorDeclSecurity.
    void const  *pvPermission,          // [IN] Permission blob.
    ULONG       cbPermission)           // [IN] Count of bytes of pvPermission.
{
    DeclSecurityRec *pRecord;
    HRESULT     hr = S_OK;

    _ASSERTE(TypeFromToken(tkPerm) == mdtPermission && cbPermission != UINT32_MAX);
    _ASSERTE(dwAction && dwAction <= dclMaximumValue);

    IfFailGo(m_pStgdb->m_MiniMd.GetDeclSecurityRecord(RidFromToken(tkPerm), &pRecord));

    IfFailGo(m_pStgdb->m_MiniMd.PutBlob(TBL_DeclSecurity, DeclSecurityRec::COL_PermissionSet,
                                        pRecord, pvPermission, cbPermission));
ErrExit:
    return hr;
} // HRESULT RegMeta::_SetPermissionSetProps()

//******************************************************************************
// Define or set value on a constant record.
//******************************************************************************
HRESULT RegMeta::_DefineSetConstant(    // Return hresult.
    mdToken     tk,                     // [IN] Parent token.
    DWORD       dwCPlusTypeFlag,        // [IN] Flag for the value type, selected ELEMENT_TYPE_*
    void const  *pValue,                // [IN] Constant value.
    ULONG       cchString,              // [IN] Size of string in wide chars, or -1 for default.
    BOOL        bSearch)                // [IN] Specifies whether to search for an existing record.
{
    HRESULT     hr = S_OK;



    if ((dwCPlusTypeFlag != ELEMENT_TYPE_VOID && dwCPlusTypeFlag != ELEMENT_TYPE_END &&
         dwCPlusTypeFlag != UINT32_MAX) &&
        (pValue || (pValue == 0 && (dwCPlusTypeFlag == ELEMENT_TYPE_STRING ||
                                    dwCPlusTypeFlag == ELEMENT_TYPE_CLASS))))
    {
        ConstantRec *pConstRec = 0;
        RID         iConstRec;
        ULONG       cbBlob;
        ULONG       ulValue = 0;

        if (bSearch)
        {
            IfFailGo(m_pStgdb->m_MiniMd.FindConstantHelper(tk, &iConstRec));
            if (!InvalidRid(iConstRec))
                IfFailGo(m_pStgdb->m_MiniMd.GetConstantRecord(iConstRec, &pConstRec));
        }
        if (! pConstRec)
        {
            IfFailGo(m_pStgdb->m_MiniMd.AddConstantRecord(&pConstRec, &iConstRec));
            IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_Constant, ConstantRec::COL_Parent,
                                                 pConstRec, tk));
            IfFailGo( m_pStgdb->m_MiniMd.AddConstantToHash(iConstRec) );
        }

        // Add values to the various columns of the constant value row.
        pConstRec->SetType(static_cast<BYTE>(dwCPlusTypeFlag));
        if (!pValue)
            pValue = &ulValue;
        cbBlob = _GetSizeOfConstantBlob(dwCPlusTypeFlag, (void *)pValue, cchString);
        if (cbBlob > 0)
        {
#if BIGENDIAN
            void *pValueTemp;
            pValueTemp = (void *)alloca(cbBlob);
            IfFailGo(m_pStgdb->m_MiniMd.SwapConstant(pValue, dwCPlusTypeFlag, pValueTemp, cbBlob));
            pValue = pValueTemp;
#endif
            IfFailGo(m_pStgdb->m_MiniMd.PutBlob(TBL_Constant, ConstantRec::COL_Value,
                                                pConstRec, pValue, cbBlob));
        }


        // Create log record for non-token record.
        IfFailGo(UpdateENCLog2(TBL_Constant, iConstRec));
    }
ErrExit:

    return hr;
} // HRESULT RegMeta::_DefineSetConstant()


//*****************************************************************************
// Helper: Set the properties on the given Method token.
//*****************************************************************************
HRESULT RegMeta::_SetMethodProps(       // S_OK or error.
    mdMethodDef md,                     // [IN] The MethodDef.
    DWORD       dwMethodFlags,          // [IN] Method attributes.
    ULONG       ulCodeRVA,              // [IN] Code RVA.
    DWORD       dwImplFlags)            // [IN] MethodImpl flags.
{
    MethodRec   *pRecord;
    HRESULT     hr = S_OK;

    _ASSERTE(TypeFromToken(md) == mdtMethodDef && RidFromToken(md));

    // Get the Method record.
    IfFailGo(m_pStgdb->m_MiniMd.GetMethodRecord(RidFromToken(md), &pRecord));

    // Set the data.
    if (dwMethodFlags != UINT32_MAX)
    {
        // Preserve the reserved flags stored already and always keep the mdRTSpecialName
        dwMethodFlags |= (pRecord->GetFlags() & mdReservedMask);

        // Set the flags.
        pRecord->SetFlags(static_cast<USHORT>(dwMethodFlags));
    }
    if (ulCodeRVA != UINT32_MAX)
        pRecord->SetRVA(ulCodeRVA);
    if (dwImplFlags != UINT32_MAX)
        pRecord->SetImplFlags(static_cast<USHORT>(dwImplFlags));

    IfFailGo(UpdateENCLog(md));
ErrExit:
    return hr;
} // HRESULT RegMeta::_SetMethodProps()


//*****************************************************************************
// Helper: Set the properties on the given Field token.
//*****************************************************************************
HRESULT RegMeta::_SetFieldProps(        // S_OK or error.
    mdFieldDef  fd,                     // [IN] The FieldDef.
    DWORD       dwFieldFlags,           // [IN] Field attributes.
    DWORD       dwCPlusTypeFlag,        // [IN] Flag for the value type, selected ELEMENT_TYPE_*
    void const  *pValue,                // [IN] Constant value.
    ULONG       cchValue)               // [IN] size of constant value (string, in wide chars).
{
#ifdef FEATURE_METADATA_EMIT_IN_DEBUGGER
    return E_NOTIMPL;
#else //!FEATURE_METADATA_EMIT_IN_DEBUGGER
    FieldRec    *pRecord;
    HRESULT     hr = S_OK;
    int         bHasDefault = false;    // If defining a constant, in this call.

    _ASSERTE (TypeFromToken(fd) == mdtFieldDef && RidFromToken(fd));

    // Get the Field record.
    IfFailGo(m_pStgdb->m_MiniMd.GetFieldRecord(RidFromToken(fd), &pRecord));

    // See if there is a Constant.
    if ((dwCPlusTypeFlag != ELEMENT_TYPE_VOID && dwCPlusTypeFlag != ELEMENT_TYPE_END &&
         dwCPlusTypeFlag != UINT32_MAX) &&
        (pValue || (pValue == 0 && (dwCPlusTypeFlag == ELEMENT_TYPE_STRING ||
                                    dwCPlusTypeFlag == ELEMENT_TYPE_CLASS))))
    {
        if (dwFieldFlags == UINT32_MAX)
            dwFieldFlags = pRecord->GetFlags();
        dwFieldFlags |= fdHasDefault;

        bHasDefault = true;
    }

    // Set the flags.
    if (dwFieldFlags != UINT32_MAX)
    {
        if ( IsFdHasFieldRVA(dwFieldFlags) && !IsFdHasFieldRVA(pRecord->GetFlags()) )
        {
            // This will trigger field RVA to be created if it is not yet created!
            _SetRVA(fd, 0, 0);
        }

        // Preserve the reserved flags stored.
        dwFieldFlags |= (pRecord->GetFlags() & fdReservedMask);
        // Set the flags.
        pRecord->SetFlags(static_cast<USHORT>(dwFieldFlags));
    }

    IfFailGo(UpdateENCLog(fd));

    // Set the Constant.
    if (bHasDefault)
    {
        BOOL bSearch = IsCallerExternal() || IsENCOn();
        IfFailGo(_DefineSetConstant(fd, dwCPlusTypeFlag, pValue, cchValue, bSearch));
    }

ErrExit:
    return hr;
#endif //!FEATURE_METADATA_EMIT_IN_DEBUGGER
} // RegMeta::_SetFieldProps

//*****************************************************************************
// Helper: Set the properties on the given Property token.
//*****************************************************************************
HRESULT RegMeta::_SetPropertyProps(      // S_OK or error.
    mdProperty  pr,                     // [IN] Property token.
    DWORD       dwPropFlags,            // [IN] CorPropertyAttr.
    DWORD       dwCPlusTypeFlag,        // [IN] Flag for value type, selected ELEMENT_TYPE_*
    void const  *pValue,                // [IN] Constant value.
    ULONG       cchValue,               // [IN] size of constant value (string, in wide chars).
    mdMethodDef mdSetter,               // [IN] Setter of the property.
    mdMethodDef mdGetter,               // [IN] Getter of the property.
    mdMethodDef rmdOtherMethods[])      // [IN] Array of other methods.
{
    PropertyRec *pRecord;
    BOOL        bClear = IsCallerExternal() || IsENCOn() || IsIncrementalOn();
    HRESULT     hr = S_OK;
    int         bHasDefault = false;    // If true, constant value this call.



    _ASSERTE(TypeFromToken(pr) == mdtProperty && RidFromToken(pr));

    IfFailGo(m_pStgdb->m_MiniMd.GetPropertyRecord(RidFromToken(pr), &pRecord));

    if (dwPropFlags != UINT32_MAX)
    {
        // Clear the reserved flags from the flags passed in.
        dwPropFlags &= (~prReservedMask);
    }
    // See if there is a constant.
    if ((dwCPlusTypeFlag != ELEMENT_TYPE_VOID && dwCPlusTypeFlag != ELEMENT_TYPE_END &&
         dwCPlusTypeFlag != UINT32_MAX) &&
        (pValue || (pValue == 0 && (dwCPlusTypeFlag == ELEMENT_TYPE_STRING ||
                                    dwCPlusTypeFlag == ELEMENT_TYPE_CLASS))))
    {
        if (dwPropFlags == UINT32_MAX)
            dwPropFlags = pRecord->GetPropFlags();
        dwPropFlags |= prHasDefault;

        bHasDefault = true;
    }
    if (dwPropFlags != UINT32_MAX)
    {
        // Preserve the reserved flags.
        dwPropFlags |= (pRecord->GetPropFlags() & prReservedMask);
        // Set the flags.
        pRecord->SetPropFlags(static_cast<USHORT>(dwPropFlags));
    }

    // store the getter (or clear out old one).
    if (mdGetter != UINT32_MAX)
    {
        _ASSERTE(TypeFromToken(mdGetter) == mdtMethodDef || IsNilToken(mdGetter));
        IfFailGo(_DefineMethodSemantics(msGetter, mdGetter, pr, bClear));
    }

    // Store the setter (or clear out old one).
    if (mdSetter != UINT32_MAX)
    {
        _ASSERTE(TypeFromToken(mdSetter) == mdtMethodDef || IsNilToken(mdSetter));
        IfFailGo(_DefineMethodSemantics(msSetter, mdSetter, pr, bClear));
    }

    // Store all of the other methods.
    if (rmdOtherMethods)
    {
        int         i = 0;
        mdMethodDef mb;

        while (1)
        {
            mb = rmdOtherMethods[i++];
            if (IsNilToken(mb))
                break;
            _ASSERTE(TypeFromToken(mb) == mdtMethodDef);
            IfFailGo(_DefineMethodSemantics(msOther, mb, pr, bClear));

            // The first call to _DefineMethodSemantics would've cleared all the records
            // that match with msOther and pr.
            bClear = false;
        }
    }

    IfFailGo(UpdateENCLog(pr));

    // Set the constant.
    if (bHasDefault)
    {
        BOOL bSearch = IsCallerExternal() || IsENCOn() || IsIncrementalOn();
        IfFailGo(_DefineSetConstant(pr, dwCPlusTypeFlag, pValue, cchValue, bSearch));
    }

ErrExit:

    return hr;
} // HRESULT RegMeta::_SetPropertyProps()


//*****************************************************************************
// Helper: This routine sets properties on the given Param token.
//*****************************************************************************
HRESULT RegMeta::_SetParamProps(        // Return code.
    mdParamDef  pd,                     // [IN] Param token.
    LPCWSTR     szName,                 // [IN] Param name.
    DWORD       dwParamFlags,           // [IN] Param flags.
    DWORD       dwCPlusTypeFlag,        // [IN] Flag for value type. selected ELEMENT_TYPE_*.
    void const  *pValue,                // [OUT] Constant value.
    ULONG       cchValue)               // [IN] size of constant value (string, in wide chars).
{
    HRESULT     hr = S_OK;
    ParamRec    *pRecord;
    int         bHasDefault = false;    // Is there a default for this call.

    _ASSERTE(TypeFromToken(pd) == mdtParamDef && RidFromToken(pd));

    IfFailGo(m_pStgdb->m_MiniMd.GetParamRecord(RidFromToken(pd), &pRecord));

    // Set the properties.
    if (szName != NULL)
    {
        IfFailGo(m_pStgdb->m_MiniMd.PutStringW(TBL_Param, ParamRec::COL_Name, pRecord, szName));
    }

    if (dwParamFlags != UINT32_MAX)
    {
        // No one should try to set the reserved flags explicitly.
        _ASSERTE((dwParamFlags & pdReservedMask) == 0);
        // Clear the reserved flags from the flags passed in.
        dwParamFlags &= (~pdReservedMask);
    }
    // See if there is a constant.
    if ((dwCPlusTypeFlag != ELEMENT_TYPE_VOID && dwCPlusTypeFlag != ELEMENT_TYPE_END &&
         dwCPlusTypeFlag != UINT32_MAX) &&
        (pValue || (pValue == 0 && (dwCPlusTypeFlag == ELEMENT_TYPE_STRING ||
                                    dwCPlusTypeFlag == ELEMENT_TYPE_CLASS))))
    {
        if (dwParamFlags == UINT32_MAX)
            dwParamFlags = pRecord->GetFlags();
        dwParamFlags |= pdHasDefault;

        bHasDefault = true;
    }
    // Set the flags.
    if (dwParamFlags != UINT32_MAX)
    {
        // Preserve the reserved flags stored.
        dwParamFlags |= (pRecord->GetFlags() & pdReservedMask);
        // Set the flags.
        pRecord->SetFlags(static_cast<USHORT>(dwParamFlags));
    }

    // ENC log for the param record.
    IfFailGo(UpdateENCLog(pd));

    // Defer setting the constant until after the ENC log for the param.  Due to the way that
    //  parameter records are re-ordered, ENC needs the param record log entry to be IMMEDIATELY
    //  after the param added function.

    // Set the constant.
    if (bHasDefault)
    {
        BOOL bSearch = IsCallerExternal() || IsENCOn();
        IfFailGo(_DefineSetConstant(pd, dwCPlusTypeFlag, pValue, cchValue, bSearch));
    }

ErrExit:
    return hr;
} // HRESULT RegMeta::_SetParamProps()

//*****************************************************************************
// Create and populate a new TypeDef record.
//*****************************************************************************
HRESULT RegMeta::_DefineTypeDef(        // S_OK or error.
    LPCWSTR     szTypeDef,              // [IN] Name of TypeDef
    DWORD       dwTypeDefFlags,         // [IN] CustomAttribute flags
    mdToken     tkExtends,              // [IN] extends this TypeDef or typeref
    mdToken     rtkImplements[],        // [IN] Implements interfaces
    mdTypeDef   tdEncloser,             // [IN] TypeDef token of the Enclosing Type.
    mdTypeDef   *ptd)                   // [OUT] Put TypeDef token here
{
    HRESULT     hr = S_OK;              // A result.
    TypeDefRec  *pRecord = NULL;        // New TypeDef record.
    RID         iRecord;                // New TypeDef RID.
    CQuickBytes qbNamespace;            // Namespace buffer.
    CQuickBytes qbName;                 // Name buffer.
    LPUTF8      szTypeDefUTF8;          // Full name in UTF8.
    ULONG       ulStringLen;            // Length of the TypeDef string.
    int         bSuccess;               // Return value for SplitPath().



    _ASSERTE(IsTdAutoLayout(dwTypeDefFlags) || IsTdSequentialLayout(dwTypeDefFlags) || IsTdExplicitLayout(dwTypeDefFlags));

    _ASSERTE(ptd);
    _ASSERTE(TypeFromToken(tkExtends) == mdtTypeRef || TypeFromToken(tkExtends) == mdtTypeDef || TypeFromToken(tkExtends) == mdtTypeSpec
              || IsNilToken(tkExtends));
    _ASSERTE(szTypeDef && *szTypeDef);
    _ASSERTE(IsNilToken(tdEncloser) || IsTdNested(dwTypeDefFlags));

    UTF8STR(szTypeDef, szTypeDefUTF8);
    PREFIX_ASSUME(szTypeDefUTF8 != NULL);

    ulStringLen = (ULONG)(strlen(szTypeDefUTF8) + 1);
    IfFailGo(qbNamespace.ReSizeNoThrow(ulStringLen));
    IfFailGo(qbName.ReSizeNoThrow(ulStringLen));
    bSuccess = ns::SplitPath(szTypeDefUTF8,
                             (LPUTF8)qbNamespace.Ptr(),
                             ulStringLen,
                             (LPUTF8)qbName.Ptr(),
                             ulStringLen);
    _ASSERTE(bSuccess);

    if (CheckDups(MDDupTypeDef))
    {
        // Check for existence.  Do a query by namespace and name.
        hr = ImportHelper::FindTypeDefByName(&(m_pStgdb->m_MiniMd),
                                             (LPCUTF8)qbNamespace.Ptr(),
                                             (LPCUTF8)qbName.Ptr(),
                                             tdEncloser,
                                             ptd);
        if (SUCCEEDED(hr))
        {
            if (IsENCOn())
            {
                IfFailGo(m_pStgdb->m_MiniMd.GetTypeDefRecord(RidFromToken(*ptd), &pRecord));
                // <TODO>@FUTURE: Should we check to see if the GUID passed is correct?</TODO>
            }
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
        // Create the new record.
        IfFailGo(m_pStgdb->m_MiniMd.AddTypeDefRecord(&pRecord, &iRecord));

        // Invalidate the ref to def optimization since more def is introduced
        SetTypeDefDirty(true);

        if (!IsNilToken(tdEncloser))
        {
            NestedClassRec  *pNestedClassRec;
            RID         iNestedClassRec;

            // Create a new NestedClass record.
            IfFailGo(m_pStgdb->m_MiniMd.AddNestedClassRecord(&pNestedClassRec, &iNestedClassRec));
            // Set the NestedClass value.
            IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_NestedClass, NestedClassRec::COL_NestedClass,
                                                 pNestedClassRec, TokenFromRid(iRecord, mdtTypeDef)));
            // Set the NestedClass value.
            IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_NestedClass, NestedClassRec::COL_EnclosingClass,
                                                 pNestedClassRec, tdEncloser));

            IfFailGo( m_pStgdb->m_MiniMd.AddNestedClassToHash(iNestedClassRec) );

            // Create the log record for the non-token record.
            IfFailGo(UpdateENCLog2(TBL_NestedClass, iNestedClassRec));
        }

        // Give token back to caller.
        *ptd = TokenFromRid(iRecord, mdtTypeDef);
    }

    // Set the namespace and name.
    IfFailGo(m_pStgdb->m_MiniMd.PutString(TBL_TypeDef, TypeDefRec::COL_Name,
                                          pRecord, (LPCUTF8)qbName.Ptr()));
    IfFailGo(m_pStgdb->m_MiniMd.PutString(TBL_TypeDef, TypeDefRec::COL_Namespace,
                                          pRecord, (LPCUTF8)qbNamespace.Ptr()));

    SetCallerDefine();
    IfFailGo(_SetTypeDefProps(*ptd, dwTypeDefFlags, tkExtends, rtkImplements));
ErrExit:
    SetCallerExternal();

    return hr;
} // RegMeta::_DefineTypeDef

#endif //FEATURE_METADATA_EMIT

#if defined(FEATURE_METADATA_IN_VM) && defined(FEATURE_PREJIT)

//******************************************************************************
//--- IMetaDataCorProfileData
//******************************************************************************

HRESULT RegMeta::SetCorProfileData(
        CorProfileData *pProfileData)         // [IN] Pointer to profile data
{
    m_pCorProfileData = pProfileData;

    return S_OK;
}

//******************************************************************************
//--- IMDInternalMetadataReorderingOptions
//******************************************************************************

HRESULT RegMeta::SetMetaDataReorderingOptions(
        MetaDataReorderingOptions options)         // [IN] Metadata reordering options
{
    m_ReorderingOptions = options;

    return S_OK;
}

#endif //defined(FEATURE_METADATA_IN_VM) && defined(FEATURE_PREJIT)
