// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
    _Out_writes_to_opt_(cchName, *pchName) LPWSTR szName, // [OUT] Buffer to fill with name.
    ULONG       cchName,                // [IN] Size of buffer in wide chars.
    ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
    ASSEMBLYMETADATA *pMetaData,         // [OUT] Assembly MetaData.
    DWORD       *pdwAssemblyFlags)      // [OUT] Flags.
{
    HRESULT     hr = S_OK;

    AssemblyRec *pRecord;
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);

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
    return hr;
}   // RegMeta::GetAssemblyProps

//*******************************************************************************
// Get the properties for the given AssemblyRef token.
//*******************************************************************************
STDMETHODIMP RegMeta::GetAssemblyRefProps(    // S_OK or error.
    mdAssemblyRef mdar,                 // [IN] The AssemblyRef for which to get the properties.
    const void  **ppbPublicKeyOrToken,  // [OUT] Pointer to the public key or token.
    ULONG       *pcbPublicKeyOrToken,   // [OUT] Count of bytes in the public key or token.
    _Out_writes_to_opt_(cchName, *pchName) LPWSTR szName, // [OUT] Buffer to fill with name.
    ULONG       cchName,                // [IN] Size of buffer in wide chars.
    ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
    ASSEMBLYMETADATA *pMetaData,        // [OUT] Assembly MetaData.
    const void  **ppbHashValue,         // [OUT] Hash blob.
    ULONG       *pcbHashValue,          // [OUT] Count of bytes in the hash blob.
    DWORD       *pdwAssemblyRefFlags)   // [OUT] Flags.
{
    HRESULT     hr = S_OK;

    AssemblyRefRec  *pRecord;
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);

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
    return hr;
}   // RegMeta::GetAssemblyRefProps

//*******************************************************************************
// Get the properties for the given File token.
//*******************************************************************************
STDMETHODIMP RegMeta::GetFileProps(     // S_OK or error.
    mdFile      mdf,                    // [IN] The File for which to get the properties.
    _Out_writes_to_opt_(cchName, *pchName) LPWSTR szName, // [OUT] Buffer to fill with name.
    ULONG        cchName,               // [IN] Size of buffer in wide chars.
    ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
    const void **ppbHashValue,          // [OUT] Pointer to the Hash Value Blob.
    ULONG       *pcbHashValue,          // [OUT] Count of bytes in the Hash Value Blob.
    DWORD       *pdwFileFlags)          // [OUT] Flags.
{
    HRESULT hr = S_OK;

    FileRec   *pRecord;
    CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);

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
    return hr;
} // RegMeta::GetFileProps

//*******************************************************************************
// Get the properties for the given ExportedType token.
//*******************************************************************************
STDMETHODIMP RegMeta::GetExportedTypeProps(   // S_OK or error.
    mdExportedType   mdct,              // [IN] The ExportedType for which to get the properties.
    _Out_writes_to_opt_(cchName, *pchName) LPWSTR      szName, // [OUT] Buffer to fill with name.
    ULONG       cchName,                // [IN] Size of buffer in wide chars.
    ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
    mdToken     *ptkImplementation,     // [OUT] mdFile or mdAssemblyRef that provides the ExportedType.
    mdTypeDef   *ptkTypeDef,            // [OUT] TypeDef token within the file.
    DWORD       *pdwExportedTypeFlags)  // [OUT] Flags.
{
    HRESULT     hr = S_OK;              // A result.

    ExportedTypeRec  *pRecord;          // The exported type.
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);
    int         bTruncation=0;          // Was there name truncation?

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
                *pchName = (ULONG)(u16_strlen(szName) + 1);
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
    return hr;
}   // RegMeta::GetExportedTypeProps

//*******************************************************************************
// Get the properties for the given Resource token.
//*******************************************************************************
STDMETHODIMP RegMeta::GetManifestResourceProps(   // S_OK or error.
    mdManifestResource  mdmr,           // [IN] The ManifestResource for which to get the properties.
    _Out_writes_to_opt_(cchName, *pchName)LPWSTR      szName,  // [OUT] Buffer to fill with name.
    ULONG       cchName,                // [IN] Size of buffer in wide chars.
    ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
    mdToken     *ptkImplementation,     // [OUT] mdFile or mdAssemblyRef that provides the ExportedType.
    DWORD       *pdwOffset,             // [OUT] Offset to the beginning of the resource within the file.
    DWORD       *pdwResourceFlags)      // [OUT] Flags.
{
    HRESULT     hr = S_OK;

    ManifestResourceRec *pRecord;
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);

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

    HENUMInternal       **ppmdEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    HENUMInternal       *pEnum;

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

    HENUMInternal       **ppmdEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    HENUMInternal       *pEnum;

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

    HENUMInternal       **ppmdEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    HENUMInternal       *pEnum = NULL;

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
        pEnum = NULL;
    }

    // we can only fill the minimum of what the caller asked for or what we have left.
    IfFailGo(HENUMInternal::EnumWithCount(*ppmdEnum, cMax, rExportedTypes, pcTokens));
ErrExit:
    HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);
    HENUMInternal::DestroyEnum(pEnum);

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

    HENUMInternal       **ppmdEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    HENUMInternal       *pEnum;

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

    CMiniMdRW   *pMiniMd = NULL;
    LPSTR       szNameUTF8 = NULL;

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

    LPCUTF8     szNameTmp = NULL;
    CMiniMdRW   *pMiniMd = NULL;

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
    return hr;
}   // RegMeta::FindManifestResourceByName

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
    return COR_E_NOTSUPPORTED;
#else //!FEATURE_METADATA_IN_VM
    // Calls to fusion are not supported outside VM
    return E_NOTIMPL;
#endif //!FEATURE_METADATA_IN_VM
} // RegMeta::FindAssembliesByName
