// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// AssemblyMD.cpp
// 

// 
// Implementation for the assembly meta data import code (code:IMetaDataAssemblyImport).
// 
//*****************************************************************************
#include "stdafx.h"
#include "regmeta.h"
#include "mdutil.h"
#include "rwutil.h"
#include "mdlog.h"
#include "importhelper.h"

#include <strongname.h>

#ifdef _MSC_VER
#pragma warning(disable: 4102)
#endif

//*******************************************************************************
// Get the properties for the given Assembly token.
//*******************************************************************************
STDMETHODIMP RegMeta::GetAssemblyProps(       // S_OK or error.
    mdAssembly  mda,                    // [IN] The Assembly for which to get the properties.
    const void  **ppbPublicKey,         // [OUT] Pointer to the public key.
    ULONG       *pcbPublicKey,          // [OUT] Count of bytes in the public key.
    ULONG       *pulHashAlgId,          // [OUT] Hash Algorithm.
    __out_ecount_part_opt(cchName, *pchName) LPWSTR szName, // [OUT] Buffer to fill with name.
    ULONG       cchName,                // [IN] Size of buffer in wide chars.
    ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
    ASSEMBLYMETADATA *pMetaData,         // [OUT] Assembly MetaData.
    DWORD       *pdwAssemblyFlags)      // [OUT] Flags.
{
    HRESULT     hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    AssemblyRec *pRecord;
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);

    LOG((LOGMD, "RegMeta::GetAssemblyProps(0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        mda, ppbPublicKey, pcbPublicKey, pulHashAlgId, szName, cchName, pchName, pMetaData,
        pdwAssemblyFlags));
   
    START_MD_PERF();
    LOCKREAD();

    _ASSERTE(TypeFromToken(mda) == mdtAssembly && RidFromToken(mda));
    IfFailGo(pMiniMd->GetAssemblyRecord(RidFromToken(mda), &pRecord));

    if (ppbPublicKey != NULL)
    {
        IfFailGo(pMiniMd->getPublicKeyOfAssembly(pRecord, (const BYTE **)ppbPublicKey, pcbPublicKey));
    }
    if (pulHashAlgId)
        *pulHashAlgId = pMiniMd->getHashAlgIdOfAssembly(pRecord);
    if (pMetaData)
    {
        pMetaData->usMajorVersion = pMiniMd->getMajorVersionOfAssembly(pRecord);
        pMetaData->usMinorVersion = pMiniMd->getMinorVersionOfAssembly(pRecord);
        pMetaData->usBuildNumber = pMiniMd->getBuildNumberOfAssembly(pRecord);
        pMetaData->usRevisionNumber = pMiniMd->getRevisionNumberOfAssembly(pRecord);
        IfFailGo(pMiniMd->getLocaleOfAssembly(pRecord, pMetaData->szLocale,
                                              pMetaData->cbLocale, &pMetaData->cbLocale));
        pMetaData->ulProcessor = 0;
        pMetaData->ulOS = 0;
    }
    if (pdwAssemblyFlags)
    {
        *pdwAssemblyFlags = pMiniMd->getFlagsOfAssembly(pRecord);

        // Turn on the afPublicKey if PublicKey blob is not empty
        DWORD cbPublicKey;
        const BYTE *pbPublicKey;
        IfFailGo(pMiniMd->getPublicKeyOfAssembly(pRecord, &pbPublicKey, &cbPublicKey));
        if (cbPublicKey != 0)
            *pdwAssemblyFlags |= afPublicKey;
    }
    // This call has to be last to set 'hr', so CLDB_S_TRUNCATION is not rewritten with S_OK
    if (szName || pchName)
        IfFailGo(pMiniMd->getNameOfAssembly(pRecord, szName, cchName, pchName));
ErrExit:
    
    STOP_MD_PERF(GetAssemblyProps);

    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::GetAssemblyProps

//*******************************************************************************
// Get the properties for the given AssemblyRef token.
//*******************************************************************************
STDMETHODIMP RegMeta::GetAssemblyRefProps(    // S_OK or error.
    mdAssemblyRef mdar,                 // [IN] The AssemblyRef for which to get the properties.
    const void  **ppbPublicKeyOrToken,  // [OUT] Pointer to the public key or token.
    ULONG       *pcbPublicKeyOrToken,   // [OUT] Count of bytes in the public key or token.
    __out_ecount_part_opt(cchName, *pchName) LPWSTR szName, // [OUT] Buffer to fill with name.
    ULONG       cchName,                // [IN] Size of buffer in wide chars.
    ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
    ASSEMBLYMETADATA *pMetaData,        // [OUT] Assembly MetaData.
    const void  **ppbHashValue,         // [OUT] Hash blob.
    ULONG       *pcbHashValue,          // [OUT] Count of bytes in the hash blob.
    DWORD       *pdwAssemblyRefFlags)   // [OUT] Flags.
{
    HRESULT     hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    AssemblyRefRec  *pRecord;
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);

    LOG((LOGMD, "RegMeta::GetAssemblyRefProps(0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        mdar, ppbPublicKeyOrToken, pcbPublicKeyOrToken, szName, cchName,
        pchName, pMetaData, ppbHashValue, pdwAssemblyRefFlags));

    START_MD_PERF();
    LOCKREAD();

    _ASSERTE(TypeFromToken(mdar) == mdtAssemblyRef && RidFromToken(mdar));
    IfFailGo(pMiniMd->GetAssemblyRefRecord(RidFromToken(mdar), &pRecord));

    if (ppbPublicKeyOrToken != NULL)
    {
        IfFailGo(pMiniMd->getPublicKeyOrTokenOfAssemblyRef(pRecord, (const BYTE **)ppbPublicKeyOrToken, pcbPublicKeyOrToken));
    }
    if (pMetaData)
    {
        pMetaData->usMajorVersion = pMiniMd->getMajorVersionOfAssemblyRef(pRecord);
        pMetaData->usMinorVersion = pMiniMd->getMinorVersionOfAssemblyRef(pRecord);
        pMetaData->usBuildNumber = pMiniMd->getBuildNumberOfAssemblyRef(pRecord);
        pMetaData->usRevisionNumber = pMiniMd->getRevisionNumberOfAssemblyRef(pRecord);
        IfFailGo(pMiniMd->getLocaleOfAssemblyRef(pRecord, pMetaData->szLocale,
                                    pMetaData->cbLocale, &pMetaData->cbLocale));
        pMetaData->ulProcessor = 0;
        pMetaData->ulOS = 0;
    }
    if (ppbHashValue != NULL)
    {
        IfFailGo(pMiniMd->getHashValueOfAssemblyRef(pRecord, (const BYTE **)ppbHashValue, pcbHashValue));
    }
    if (pdwAssemblyRefFlags)
        *pdwAssemblyRefFlags = pMiniMd->getFlagsOfAssemblyRef(pRecord);
    // This call has to be last to set 'hr', so CLDB_S_TRUNCATION is not rewritten with S_OK
    if (szName || pchName)
        IfFailGo(pMiniMd->getNameOfAssemblyRef(pRecord, szName, cchName, pchName));
ErrExit:
    
    STOP_MD_PERF(GetAssemblyRefProps);
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::GetAssemblyRefProps

//*******************************************************************************
// Get the properties for the given File token.
//*******************************************************************************
STDMETHODIMP RegMeta::GetFileProps(     // S_OK or error.
    mdFile      mdf,                    // [IN] The File for which to get the properties.
    __out_ecount_part_opt(cchName, *pchName) LPWSTR szName, // [OUT] Buffer to fill with name.
    ULONG        cchName,               // [IN] Size of buffer in wide chars.
    ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
    const void **ppbHashValue,          // [OUT] Pointer to the Hash Value Blob.
    ULONG       *pcbHashValue,          // [OUT] Count of bytes in the Hash Value Blob.
    DWORD       *pdwFileFlags)          // [OUT] Flags.
{
    HRESULT hr = S_OK;
    
    BEGIN_ENTRYPOINT_NOTHROW;
    
    FileRec   *pRecord;
    CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);
    
    LOG((LOGMD, "RegMeta::GetFileProps(%#08x, %#08x, %#08x, %#08x, %#08x, %#08x, %#08x)\n",
        mdf, szName, cchName, pchName, ppbHashValue, pcbHashValue, pdwFileFlags));
    
    START_MD_PERF();
    LOCKREAD();
    
    _ASSERTE(TypeFromToken(mdf) == mdtFile && RidFromToken(mdf));
    IfFailGo(pMiniMd->GetFileRecord(RidFromToken(mdf), &pRecord));
    
    if (ppbHashValue != NULL)
    {
        IfFailGo(pMiniMd->getHashValueOfFile(pRecord, (const BYTE **)ppbHashValue, pcbHashValue));
    }
    if (pdwFileFlags != NULL)
        *pdwFileFlags = pMiniMd->getFlagsOfFile(pRecord);
    // This call has to be last to set 'hr', so CLDB_S_TRUNCATION is not rewritten with S_OK
    if ((szName != NULL) || (pchName != NULL))
    {
        IfFailGo(pMiniMd->getNameOfFile(pRecord, szName, cchName, pchName));
    }
    
ErrExit:
    STOP_MD_PERF(GetFileProps);
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
} // RegMeta::GetFileProps

//*******************************************************************************
// Get the properties for the given ExportedType token.
//*******************************************************************************
STDMETHODIMP RegMeta::GetExportedTypeProps(   // S_OK or error.
    mdExportedType   mdct,              // [IN] The ExportedType for which to get the properties.
    __out_ecount_part_opt(cchName, *pchName) LPWSTR      szName, // [OUT] Buffer to fill with name.
    ULONG       cchName,                // [IN] Size of buffer in wide chars.
    ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
    mdToken     *ptkImplementation,     // [OUT] mdFile or mdAssemblyRef that provides the ExportedType.
    mdTypeDef   *ptkTypeDef,            // [OUT] TypeDef token within the file.
    DWORD       *pdwExportedTypeFlags)  // [OUT] Flags.
{
    HRESULT     hr = S_OK;              // A result.

    BEGIN_ENTRYPOINT_NOTHROW;

    ExportedTypeRec  *pRecord;          // The exported type.
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);
    int         bTruncation=0;          // Was there name truncation?

    LOG((LOGMD, "RegMeta::GetExportedTypeProps(%#08x, %#08x, %#08x, %#08x, %#08x, %#08x, %#08x)\n",
        mdct, szName, cchName, pchName, 
        ptkImplementation, ptkTypeDef, pdwExportedTypeFlags));

    START_MD_PERF();
    LOCKREAD();

    _ASSERTE(TypeFromToken(mdct) == mdtExportedType && RidFromToken(mdct));
    IfFailGo(pMiniMd->GetExportedTypeRecord(RidFromToken(mdct), &pRecord));

    if (szName || pchName)
    {
        LPCSTR  szTypeNamespace;
        LPCSTR  szTypeName;
        
        IfFailGo(pMiniMd->getTypeNamespaceOfExportedType(pRecord, &szTypeNamespace));
        PREFIX_ASSUME(szTypeNamespace != NULL);
        MAKE_WIDEPTR_FROMUTF8_NOTHROW(wzTypeNamespace, szTypeNamespace);
        IfNullGo(wzTypeNamespace);

        IfFailGo(pMiniMd->getTypeNameOfExportedType(pRecord, &szTypeName));
        _ASSERTE(*szTypeName);
        MAKE_WIDEPTR_FROMUTF8_NOTHROW(wzTypeName, szTypeName);
        IfNullGo(wzTypeName);

        if (szName)
            bTruncation = ! (ns::MakePath(szName, cchName, wzTypeNamespace, wzTypeName));
        if (pchName)
        {
            if (bTruncation || !szName)
                *pchName = ns::GetFullLength(wzTypeNamespace, wzTypeName);
            else
                *pchName = (ULONG)(wcslen(szName) + 1);
        }
    }
    if (ptkImplementation)
        *ptkImplementation = pMiniMd->getImplementationOfExportedType(pRecord);
    if (ptkTypeDef)
        *ptkTypeDef = pMiniMd->getTypeDefIdOfExportedType(pRecord);
    if (pdwExportedTypeFlags)
        *pdwExportedTypeFlags = pMiniMd->getFlagsOfExportedType(pRecord);

    if (bTruncation && hr == S_OK)
    {
        if ((szName != NULL) && (cchName > 0))
        {   // null-terminate the truncated output string
            szName[cchName - 1] = W('\0');
        }
        hr = CLDB_S_TRUNCATION;
    }

ErrExit:
    STOP_MD_PERF(GetExportedTypeProps);
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::GetExportedTypeProps

//*******************************************************************************
// Get the properties for the given Resource token.
//*******************************************************************************
STDMETHODIMP RegMeta::GetManifestResourceProps(   // S_OK or error.
    mdManifestResource  mdmr,           // [IN] The ManifestResource for which to get the properties.
    __out_ecount_part_opt(cchName, *pchName)LPWSTR      szName,  // [OUT] Buffer to fill with name.
    ULONG       cchName,                // [IN] Size of buffer in wide chars.
    ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
    mdToken     *ptkImplementation,     // [OUT] mdFile or mdAssemblyRef that provides the ExportedType.
    DWORD       *pdwOffset,             // [OUT] Offset to the beginning of the resource within the file.
    DWORD       *pdwResourceFlags)      // [OUT] Flags.
{
    HRESULT     hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    ManifestResourceRec *pRecord;
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);

    LOG((LOGMD, "RegMeta::GetManifestResourceProps("
        "%#08x, %#08x, %#08x, %#08x, %#08x, %#08x, %#08x)\n",
        mdmr, szName, cchName, pchName, 
        ptkImplementation, pdwOffset, 
        pdwResourceFlags));
   
    START_MD_PERF();
    LOCKREAD();

    _ASSERTE(TypeFromToken(mdmr) == mdtManifestResource && RidFromToken(mdmr));
    IfFailGo(pMiniMd->GetManifestResourceRecord(RidFromToken(mdmr), &pRecord));

    if (ptkImplementation)
        *ptkImplementation = pMiniMd->getImplementationOfManifestResource(pRecord);
    if (pdwOffset)
        *pdwOffset = pMiniMd->getOffsetOfManifestResource(pRecord);
    if (pdwResourceFlags)
        *pdwResourceFlags = pMiniMd->getFlagsOfManifestResource(pRecord);
    // This call has to be last to set 'hr', so CLDB_S_TRUNCATION is not rewritten with S_OK
    if (szName || pchName)
        IfFailGo(pMiniMd->getNameOfManifestResource(pRecord, szName, cchName, pchName));
ErrExit:
    
    STOP_MD_PERF(GetManifestResourceProps);
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::GetManifestResourceProps


//*******************************************************************************
// Enumerating through all of the AssemblyRefs.
//*******************************************************************************
STDMETHODIMP RegMeta::EnumAssemblyRefs(       // S_OK or error
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdAssemblyRef rAssemblyRefs[],      // [OUT] Put AssemblyRefs here.
    ULONG       cMax,                   // [IN] Max AssemblyRefs to put.
    ULONG       *pcTokens)              // [OUT] Put # put here.
{
    HRESULT             hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    HENUMInternal       **ppmdEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    HENUMInternal       *pEnum;

    LOG((LOGMD, "MD RegMeta::EnumAssemblyRefs(%#08x, %#08x, %#08x, %#08x)\n", 
        phEnum, rAssemblyRefs, cMax, pcTokens));
    START_MD_PERF();

    LOCKREAD();
       
    if (*ppmdEnum == 0)
    {
        // instantiate a new ENUM.
        CMiniMdRW       *pMiniMd = &(m_pStgdb->m_MiniMd);

        // create the enumerator.
        IfFailGo(HENUMInternal::CreateSimpleEnum(
            mdtAssemblyRef,
            1,
            pMiniMd->getCountAssemblyRefs() + 1,
            &pEnum) );

        // set the output parameter.
        *ppmdEnum = pEnum;
    }
    else
        pEnum = *ppmdEnum;

    // we can only fill the minimum of what the caller asked for or what we have left.
    IfFailGo(HENUMInternal::EnumWithCount(pEnum, cMax, rAssemblyRefs, pcTokens));
ErrExit:
    HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);
    
    STOP_MD_PERF(EnumAssemblyRefs);
    
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::EnumAssemblyRefs

//*******************************************************************************
// Enumerating through all of the Files.
//*******************************************************************************
STDMETHODIMP RegMeta::EnumFiles(              // S_OK or error
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdFile      rFiles[],               // [OUT] Put Files here.
    ULONG       cMax,                   // [IN] Max Files to put.
    ULONG       *pcTokens)              // [OUT] Put # put here.
{
    HRESULT             hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    HENUMInternal       **ppmdEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    HENUMInternal       *pEnum;

    LOG((LOGMD, "MD RegMeta::EnumFiles(%#08x, %#08x, %#08x, %#08x)\n", 
        phEnum, rFiles, cMax, pcTokens));
    START_MD_PERF();
    LOCKREAD();

    if (*ppmdEnum == 0)
    {
        // instantiate a new ENUM.
        CMiniMdRW       *pMiniMd = &(m_pStgdb->m_MiniMd);

        // create the enumerator.
        IfFailGo(HENUMInternal::CreateSimpleEnum(
            mdtFile,
            1,
            pMiniMd->getCountFiles() + 1,
            &pEnum) );

        // set the output parameter.
        *ppmdEnum = pEnum;
    }
    else
        pEnum = *ppmdEnum;

    // we can only fill the minimum of what the caller asked for or what we have left.
    IfFailGo(HENUMInternal::EnumWithCount(pEnum, cMax, rFiles, pcTokens));
ErrExit:
    HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);
    
    STOP_MD_PERF(EnumFiles);
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::EnumFiles

//*******************************************************************************
// Enumerating through all of the ExportedTypes.
//*******************************************************************************
STDMETHODIMP RegMeta::EnumExportedTypes(           // S_OK or error
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdExportedType   rExportedTypes[],            // [OUT] Put ExportedTypes here.
    ULONG       cMax,                   // [IN] Max ExportedTypes to put.
    ULONG       *pcTokens)              // [OUT] Put # put here.
{
    HRESULT             hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    HENUMInternal       **ppmdEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    HENUMInternal       *pEnum;

    LOG((LOGMD, "MD RegMeta::EnumExportedTypes(%#08x, %#08x, %#08x, %#08x)\n", 
        phEnum, rExportedTypes, cMax, pcTokens));
   
    START_MD_PERF();
    LOCKREAD();
    
    if (*ppmdEnum == 0)
    {
        // instantiate a new ENUM.
        CMiniMdRW       *pMiniMd = &(m_pStgdb->m_MiniMd);

        if (pMiniMd->HasDelete() && 
            ((m_OptionValue.m_ImportOption & MDImportOptionAllExportedTypes) == 0))
        {
            IfFailGo( HENUMInternal::CreateDynamicArrayEnum( mdtExportedType, &pEnum) );

            // add all Types to the dynamic array if name is not _Delete
            for (ULONG index = 1; index <= pMiniMd->getCountExportedTypes(); index ++ )
            {
                ExportedTypeRec *pRec;
                IfFailGo(pMiniMd->GetExportedTypeRecord(index, &pRec));
                LPCSTR szTypeName;
                IfFailGo(pMiniMd->getTypeNameOfExportedType(pRec, &szTypeName));
                if (IsDeletedName(szTypeName))
                {
                    continue;
                }
                IfFailGo( HENUMInternal::AddElementToEnum(pEnum, TokenFromRid(index, mdtExportedType) ) );
            }
        }
        else
        {
            // create the enumerator.
            IfFailGo(HENUMInternal::CreateSimpleEnum(
                mdtExportedType,
                1,
                pMiniMd->getCountExportedTypes() + 1,
                &pEnum) );
        }

        // set the output parameter.
        *ppmdEnum = pEnum;
    }
    else
        pEnum = *ppmdEnum;

    // we can only fill the minimum of what the caller asked for or what we have left.
    IfFailGo(HENUMInternal::EnumWithCount(pEnum, cMax, rExportedTypes, pcTokens));
ErrExit:
    HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);
    
    STOP_MD_PERF(EnumExportedTypes);
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::EnumExportedTypes

//*******************************************************************************
// Enumerating through all of the Resources.
//*******************************************************************************
STDMETHODIMP RegMeta::EnumManifestResources(  // S_OK or error
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdManifestResource  rManifestResources[],   // [OUT] Put ManifestResources here.
    ULONG       cMax,                   // [IN] Max Resources to put.
    ULONG       *pcTokens)              // [OUT] Put # put here.
{
    HRESULT             hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    HENUMInternal       **ppmdEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    HENUMInternal       *pEnum;

    LOG((LOGMD, "MD RegMeta::EnumManifestResources(%#08x, %#08x, %#08x, %#08x)\n", 
        phEnum, rManifestResources, cMax, pcTokens));
   
    START_MD_PERF();
    LOCKREAD();

    if (*ppmdEnum == 0)
    {
        // instantiate a new ENUM.
        CMiniMdRW       *pMiniMd = &(m_pStgdb->m_MiniMd);

        // create the enumerator.
        IfFailGo(HENUMInternal::CreateSimpleEnum(
            mdtManifestResource,
            1,
            pMiniMd->getCountManifestResources() + 1,
            &pEnum) );

        // set the output parameter.
        *ppmdEnum = pEnum;
    }
    else
        pEnum = *ppmdEnum;

    // we can only fill the minimum of what the caller asked for or what we have left.
    IfFailGo(HENUMInternal::EnumWithCount(pEnum, cMax, rManifestResources, pcTokens));
ErrExit:
    HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);
    
    STOP_MD_PERF(EnumManifestResources);
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::EnumManifestResources

//*******************************************************************************
// Get the Assembly token for the given scope..
//*******************************************************************************
STDMETHODIMP RegMeta::GetAssemblyFromScope(   // S_OK or error
    mdAssembly  *ptkAssembly)           // [OUT] Put token here.
{
    HRESULT     hr = NOERROR;
    CMiniMdRW   *pMiniMd = NULL;

    BEGIN_ENTRYPOINT_NOTHROW;

    LOG((LOGMD, "MD RegMeta::GetAssemblyFromScope(%#08x)\n", ptkAssembly));
    START_MD_PERF();
    LOCKREAD();
    _ASSERTE(ptkAssembly);

    pMiniMd = &(m_pStgdb->m_MiniMd);
    if (pMiniMd->getCountAssemblys())
    {
        *ptkAssembly = TokenFromRid(1, mdtAssembly);
    }
    else
    {
        IfFailGo( CLDB_E_RECORD_NOTFOUND );
    }
ErrExit:
    STOP_MD_PERF(GetAssemblyFromScope);
    END_ENTRYPOINT_NOTHROW;

    return hr;
}   // RegMeta::GetAssemblyFromScope

//*******************************************************************************
// Find the ExportedType given the name.
//*******************************************************************************
STDMETHODIMP RegMeta::FindExportedTypeByName( // S_OK or error
    LPCWSTR     szName,                 // [IN] Name of the ExportedType.
    mdExportedType   tkEnclosingType,   // [IN] Enclosing ExportedType.
    mdExportedType   *ptkExportedType)  // [OUT] Put the ExportedType token here.
{
    HRESULT     hr = S_OK;              // A result.

    BEGIN_ENTRYPOINT_NOTHROW;

    CMiniMdRW   *pMiniMd = NULL;
    LPSTR       szNameUTF8 = NULL;

    LOG((LOGMD, "MD RegMeta::FindExportedTypeByName(%S, %#08x, %#08x)\n",
        MDSTR(szName), tkEnclosingType, ptkExportedType));

    START_MD_PERF();
    LOCKREAD();


    // Validate name for prefix.
    if (!szName)
        IfFailGo(E_INVALIDARG);

    _ASSERTE(szName && ptkExportedType);

    pMiniMd = &(m_pStgdb->m_MiniMd);
    UTF8STR(szName, szNameUTF8);
    LPCSTR      szTypeName;
    LPCSTR      szTypeNamespace;

    ns::SplitInline(szNameUTF8, szTypeNamespace, szTypeName);

    IfFailGo(ImportHelper::FindExportedType(pMiniMd,
                                       szTypeNamespace,
                                       szTypeName,
                                       tkEnclosingType,
                                       ptkExportedType));
ErrExit:
    STOP_MD_PERF(FindExportedTypeByName);
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::FindExportedTypeByName

//*******************************************************************************
// Find the ManifestResource given the name.
//*******************************************************************************
STDMETHODIMP RegMeta::FindManifestResourceByName( // S_OK or error
    LPCWSTR     szName,                 // [IN] Name of the ManifestResource.
    mdManifestResource *ptkManifestResource)    // [OUT] Put the ManifestResource token here.
{
    HRESULT     hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    LPCUTF8     szNameTmp = NULL;
    CMiniMdRW   *pMiniMd = NULL;

    LOG((LOGMD, "MD RegMeta::FindManifestResourceByName(%S, %#08x)\n",
        MDSTR(szName), ptkManifestResource));

    START_MD_PERF();
    LOCKREAD();


    // Validate name for prefix.
    if (!szName)
        IfFailGo(E_INVALIDARG);

    _ASSERTE(szName && ptkManifestResource);

    ManifestResourceRec *pRecord;
    ULONG       cRecords;               // Count of records.
    LPUTF8      szUTF8Name;             // UTF8 version of the name passed in.
    ULONG       i;

    pMiniMd = &(m_pStgdb->m_MiniMd);
    *ptkManifestResource = mdManifestResourceNil;
    cRecords = pMiniMd->getCountManifestResources();
    UTF8STR(szName, szUTF8Name);

    // Search for the TypeRef.
    for (i = 1; i <= cRecords; i++)
    {
        IfFailGo(pMiniMd->GetManifestResourceRecord(i, &pRecord));
        IfFailGo(pMiniMd->getNameOfManifestResource(pRecord, &szNameTmp));
        if (! strcmp(szUTF8Name, szNameTmp))
        {
            *ptkManifestResource = TokenFromRid(i, mdtManifestResource);
            goto ErrExit;
        }
    }
    IfFailGo( CLDB_E_RECORD_NOTFOUND );
ErrExit:
    
    STOP_MD_PERF(FindManifestResourceByName);
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
}   // RegMeta::FindManifestResourceByName

extern HRESULT STDMETHODCALLTYPE
    GetAssembliesByName(LPCWSTR  szAppBase,
                        LPCWSTR  szPrivateBin,
                        LPCWSTR  szAssemblyName,
                        IUnknown *ppIUnk[],
                        ULONG    cMax,
                        ULONG    *pcAssemblies);

//*******************************************************************************
// Used to find assemblies either in Fusion cache or on disk at build time.
//*******************************************************************************
STDMETHODIMP RegMeta::FindAssembliesByName( // S_OK or error
        LPCWSTR  szAppBase,           // [IN] optional - can be NULL
        LPCWSTR  szPrivateBin,        // [IN] optional - can be NULL
        LPCWSTR  szAssemblyName,      // [IN] required - this is the assembly you are requesting
        IUnknown *ppIUnk[],           // [OUT] put IMetaDataAssemblyImport pointers here
        ULONG    cMax,                // [IN] The max number to put
        ULONG    *pcAssemblies)       // [OUT] The number of assemblies returned.
{
#ifdef FEATURE_METADATA_IN_VM
    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    LOG((LOGMD, "RegMeta::FindAssembliesByName(0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        szAppBase, szPrivateBin, szAssemblyName, ppIUnk, cMax, pcAssemblies));
    START_MD_PERF();
   
    // No need to lock this function. It is going through fusion to find the matching Assemblies by name

    IfFailGo(GetAssembliesByName(szAppBase, szPrivateBin,
                                 szAssemblyName, ppIUnk, cMax, pcAssemblies));

ErrExit:
    STOP_MD_PERF(FindAssembliesByName);
    END_ENTRYPOINT_NOTHROW;
    
    return hr;
#else //!FEATURE_METADATA_IN_VM
    // Calls to fusion are not suported outside VM
    return E_NOTIMPL;
#endif //!FEATURE_METADATA_IN_VM
} // RegMeta::FindAssembliesByName
