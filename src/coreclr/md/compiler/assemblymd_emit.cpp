// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// AssemblyMD.cpp
//

//
// Implementation for the assembly meta data emit code (code:IMetaDataAssemblyEmit).
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

#ifdef FEATURE_METADATA_EMIT

//*******************************************************************************
// Define an Assembly and set the attributes.
//*******************************************************************************
STDMETHODIMP RegMeta::DefineAssembly(         // S_OK or error.
    const void  *pbPublicKey,           // [IN] Public key of the assembly.
    ULONG       cbPublicKey,            // [IN] Count of bytes in the public key.
    ULONG       ulHashAlgId,            // [IN] Hash Algorithm.
    LPCWSTR     szName,                 // [IN] Name of the assembly.
    const ASSEMBLYMETADATA *pMetaData,  // [IN] Assembly MetaData.
    DWORD       dwAssemblyFlags,        // [IN] Flags.
    mdAssembly  *pma)                   // [OUT] Returned Assembly token.
{
    HRESULT     hr = S_OK;

    AssemblyRec *pRecord = NULL;        // The assembly record.
    RID          iRecord;               // RID of the assembly record.

    if (szName == NULL || pMetaData == NULL || pma == NULL)
        return E_INVALIDARG;

    BEGIN_ENTRYPOINT_NOTHROW;

    LOG((LOGMD, "RegMeta::DefineAssembly(0x%08x, 0x%08x, 0x%08x, %S, 0x%08x, 0x%08x, 0x%08x)\n",
        pbPublicKey, cbPublicKey, ulHashAlgId, MDSTR(szName), pMetaData,
        dwAssemblyFlags, pma));

    START_MD_PERF();
    LOCKWRITE();

    _ASSERTE(szName && pMetaData && pma);

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    // Assembly defs always contain a full public key (assuming they're strong
    // named) rather than the tokenized version. Force the flag on to indicate
    // this, and this way blindly copying public key & flags from a def to a ref
    // will work (though the ref will be bulkier than strictly necessary).
    if (cbPublicKey != 0)
        dwAssemblyFlags |= afPublicKey;

    if (CheckDups(MDDupAssembly))
    {   // Should be no more than one -- just check count of records.
        if (m_pStgdb->m_MiniMd.getCountAssemblys() > 0)
        {   // S/b only one, so we know the rid.
            iRecord = 1;
            // If ENC, let them update the existing record.
            if (IsENCOn())
                IfFailGo(m_pStgdb->m_MiniMd.GetAssemblyRecord(iRecord, &pRecord));
            else
            {   // Not ENC, so it is a duplicate.
                *pma = TokenFromRid(iRecord, mdtAssembly);
                hr = META_S_DUPLICATE;
                goto ErrExit;
            }
        }
    }
    else
    {   // Not ENC, not duplicate checking, so shouldn't already have one.
        _ASSERTE(m_pStgdb->m_MiniMd.getCountAssemblys() == 0);
    }

    // Create a new record, if needed.
    if (pRecord == NULL)
    {
        IfFailGo(m_pStgdb->m_MiniMd.AddAssemblyRecord(&pRecord, &iRecord));
    }

    // Set the output parameter.
    *pma = TokenFromRid(iRecord, mdtAssembly);

    IfFailGo(_SetAssemblyProps(*pma, pbPublicKey, cbPublicKey, ulHashAlgId, szName, pMetaData, dwAssemblyFlags));

ErrExit:

    STOP_MD_PERF(DefineAssembly);
    END_ENTRYPOINT_NOTHROW;

    return hr;
}   // RegMeta::DefineAssembly

//*******************************************************************************
// Define an AssemblyRef and set the attributes.
//*******************************************************************************
STDMETHODIMP RegMeta::DefineAssemblyRef(      // S_OK or error.
    const void  *pbPublicKeyOrToken,    // [IN] Public key or token of the assembly.
    ULONG       cbPublicKeyOrToken,     // [IN] Count of bytes in the public key or token.
    LPCWSTR     szName,                 // [IN] Name of the assembly being referenced.
    const ASSEMBLYMETADATA *pMetaData,  // [IN] Assembly MetaData.
    const void  *pbHashValue,           // [IN] Hash Blob.
    ULONG       cbHashValue,            // [IN] Count of bytes in the Hash Blob.
    DWORD       dwAssemblyRefFlags,     // [IN] Flags.
    mdAssemblyRef *pmar)                // [OUT] Returned AssemblyRef token.
{
    HRESULT hr = S_OK;

    AssemblyRefRec *pRecord = NULL;
    RID             iRecord;

    if (szName == NULL || pmar == NULL || pMetaData == NULL)
        return E_INVALIDARG;

    BEGIN_ENTRYPOINT_NOTHROW;

    LOG((LOGMD, "RegMeta::DefineAssemblyRef(0x%08x, 0x%08x, %S, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        pbPublicKeyOrToken, cbPublicKeyOrToken, MDSTR(szName), pMetaData, pbHashValue,
        cbHashValue, dwAssemblyRefFlags, pmar));

    START_MD_PERF();
    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    _ASSERTE(szName && pmar);

    if (CheckDups(MDDupAssemblyRef))
    {
        LPUTF8 szUTF8Name, szUTF8Locale;
        UTF8STR(szName, szUTF8Name);
        UTF8STR(pMetaData->szLocale, szUTF8Locale);
        hr = ImportHelper::FindAssemblyRef(&m_pStgdb->m_MiniMd,
                                           szUTF8Name,
                                           szUTF8Locale,
                                           pbPublicKeyOrToken,
                                           cbPublicKeyOrToken,
                                           pMetaData->usMajorVersion,
                                           pMetaData->usMinorVersion,
                                           pMetaData->usBuildNumber,
                                           pMetaData->usRevisionNumber,
                                           dwAssemblyRefFlags,
                                           pmar);
        if (SUCCEEDED(hr))
        {
            if (IsENCOn())
            {
                IfFailGo(m_pStgdb->m_MiniMd.GetAssemblyRefRecord(RidFromToken(*pmar), &pRecord));
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

    // Create a new record if needed.
    if (pRecord == NULL)
    {
        // Create a new record.
        IfFailGo(m_pStgdb->m_MiniMd.AddAssemblyRefRecord(&pRecord, &iRecord));

        // Set the output parameter.
        *pmar = TokenFromRid(iRecord, mdtAssemblyRef);
    }

    // Set rest of the attributes.
    SetCallerDefine();
    IfFailGo(_SetAssemblyRefProps(*pmar, pbPublicKeyOrToken, cbPublicKeyOrToken, szName, pMetaData,
                                 pbHashValue, cbHashValue,
                                 dwAssemblyRefFlags));
ErrExit:
    SetCallerExternal();

    STOP_MD_PERF(DefineAssemblyRef);
    END_ENTRYPOINT_NOTHROW;

    return hr;
}   // RegMeta::DefineAssemblyRef

//*******************************************************************************
// Define a File and set the attributes.
//*******************************************************************************
STDMETHODIMP RegMeta::DefineFile(             // S_OK or error.
    LPCWSTR     szName,                 // [IN] Name of the file.
    const void  *pbHashValue,           // [IN] Hash Blob.
    ULONG       cbHashValue,            // [IN] Count of bytes in the Hash Blob.
    DWORD       dwFileFlags,            // [IN] Flags.
    mdFile      *pmf)                   // [OUT] Returned File token.
{
    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    FileRec     *pRecord = NULL;
    RID         iRecord;

    LOG((LOGMD, "RegMeta::DefineFile(%S, %#08x, %#08x, %#08x, %#08x)\n",
        MDSTR(szName), pbHashValue, cbHashValue, dwFileFlags, pmf));

    START_MD_PERF();
    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    _ASSERTE(szName && pmf);

    if (CheckDups(MDDupFile))
    {
        LPUTF8 szUTF8Name;
        UTF8STR(szName, szUTF8Name);
        hr = ImportHelper::FindFile(&m_pStgdb->m_MiniMd, szUTF8Name, pmf);
        if (SUCCEEDED(hr))
        {
            if (IsENCOn())
            {
                IfFailGo(m_pStgdb->m_MiniMd.GetFileRecord(RidFromToken(*pmf), &pRecord));
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

    // Create a new record if needed.
    if (pRecord == NULL)
    {
        // Create a new record.
        IfFailGo(m_pStgdb->m_MiniMd.AddFileRecord(&pRecord, &iRecord));

        // Set the output parameter.
        *pmf = TokenFromRid(iRecord, mdtFile);

        // Set the name.
        IfFailGo(m_pStgdb->m_MiniMd.PutStringW(TBL_File, FileRec::COL_Name, pRecord, szName));
    }

    // Set rest of the attributes.
    IfFailGo(_SetFileProps(*pmf, pbHashValue, cbHashValue, dwFileFlags));
ErrExit:

    STOP_MD_PERF(DefineFile);
    END_ENTRYPOINT_NOTHROW;

    return hr;
}   // RegMeta::DefineFile

//*******************************************************************************
// Define a ExportedType and set the attributes.
//*******************************************************************************
STDMETHODIMP RegMeta::DefineExportedType(     // S_OK or error.
    LPCWSTR     szName,                 // [IN] Name of the Com Type.
    mdToken     tkImplementation,       // [IN] mdFile or mdAssemblyRef that provides the ExportedType.
    mdTypeDef   tkTypeDef,              // [IN] TypeDef token within the file.
    DWORD       dwExportedTypeFlags,    // [IN] Flags.
    mdExportedType   *pmct)             // [OUT] Returned ExportedType token.
{
    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    ExportedTypeRec  *pRecord = NULL;
    RID         iRecord;
    LPSTR       szNameUTF8;
    LPCSTR      szTypeNameUTF8;
    LPCSTR      szTypeNamespaceUTF8;

    LOG((LOGMD, "RegMeta::DefineExportedType(%S, %#08x, %08x, %#08x, %#08x)\n",
        MDSTR(szName), tkImplementation, tkTypeDef,
         dwExportedTypeFlags, pmct));

    START_MD_PERF();
    LOCKWRITE();

    // Validate name for prefix.
    if (szName == NULL)
        IfFailGo(E_INVALIDARG);

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    //SLASHES2DOTS_NAMESPACE_BUFFER_UNICODE(szName, szName);

    UTF8STR(szName, szNameUTF8);
    // Split the name into name/namespace pair.
    ns::SplitInline(szNameUTF8, szTypeNamespaceUTF8, szTypeNameUTF8);

    _ASSERTE(szName && dwExportedTypeFlags != UINT32_MAX && pmct);
    _ASSERTE(TypeFromToken(tkImplementation) == mdtFile ||
              TypeFromToken(tkImplementation) == mdtAssemblyRef ||
              TypeFromToken(tkImplementation) == mdtExportedType ||
              tkImplementation == mdTokenNil);

    if (CheckDups(MDDupExportedType))
    {
        hr = ImportHelper::FindExportedType(&m_pStgdb->m_MiniMd,
                                       szTypeNamespaceUTF8,
                                       szTypeNameUTF8,
                                       tkImplementation,
                                       pmct);
        if (SUCCEEDED(hr))
        {
            if (IsENCOn())
            {
                IfFailGo(m_pStgdb->m_MiniMd.GetExportedTypeRecord(RidFromToken(*pmct), &pRecord));
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

    // Create a new record if needed.
    if (pRecord == NULL)
    {
        // Create a new record.
        IfFailGo(m_pStgdb->m_MiniMd.AddExportedTypeRecord(&pRecord, &iRecord));

        // Set the output parameter.
        *pmct = TokenFromRid(iRecord, mdtExportedType);

        // Set the TypeName and TypeNamespace.
        IfFailGo(m_pStgdb->m_MiniMd.PutString(TBL_ExportedType,
                ExportedTypeRec::COL_TypeName, pRecord, szTypeNameUTF8));
        if (szTypeNamespaceUTF8)
        {
            IfFailGo(m_pStgdb->m_MiniMd.PutString(TBL_ExportedType,
                    ExportedTypeRec::COL_TypeNamespace, pRecord, szTypeNamespaceUTF8));
        }
    }

    // Set rest of the attributes.
    IfFailGo(_SetExportedTypeProps(*pmct, tkImplementation, tkTypeDef,
                             dwExportedTypeFlags));
ErrExit:

    STOP_MD_PERF(DefineExportedType);
    END_ENTRYPOINT_NOTHROW;

    return hr;
}   // RegMeta::DefineExportedType

//*******************************************************************************
// Define a Resource and set the attributes.
//*******************************************************************************
STDMETHODIMP RegMeta::DefineManifestResource( // S_OK or error.
    LPCWSTR     szName,                 // [IN] Name of the ManifestResource.
    mdToken     tkImplementation,       // [IN] mdFile or mdAssemblyRef that provides the resource.
    DWORD       dwOffset,               // [IN] Offset to the beginning of the resource within the file.
    DWORD       dwResourceFlags,        // [IN] Flags.
    mdManifestResource  *pmmr)          // [OUT] Returned ManifestResource token.
{
    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    ManifestResourceRec *pRecord = NULL;
    RID       iRecord;

    LOG((LOGMD, "RegMeta::DefineManifestResource(%S, %#08x, %#08x, %#08x, %#08x)\n",
        MDSTR(szName), tkImplementation, dwOffset, dwResourceFlags, pmmr));

    START_MD_PERF();
    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    _ASSERTE(szName && dwResourceFlags != UINT32_MAX && pmmr);
    _ASSERTE(TypeFromToken(tkImplementation) == mdtFile ||
              TypeFromToken(tkImplementation) == mdtAssemblyRef ||
              tkImplementation == mdTokenNil);

    if (CheckDups(MDDupManifestResource))
    {
        LPUTF8 szUTF8Name;
        UTF8STR(szName, szUTF8Name);
        hr = ImportHelper::FindManifestResource(&m_pStgdb->m_MiniMd, szUTF8Name, pmmr);
        if (SUCCEEDED(hr))
        {
            if (IsENCOn())
            {
                IfFailGo(m_pStgdb->m_MiniMd.GetManifestResourceRecord(RidFromToken(*pmmr), &pRecord));
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

    // Create a new record if needed.
    if (pRecord == NULL)
    {
        // Create a new record.
        IfFailGo(m_pStgdb->m_MiniMd.AddManifestResourceRecord(&pRecord, &iRecord));

        // Set the output parameter.
        *pmmr = TokenFromRid(iRecord, mdtManifestResource);

        // Set the name.
        IfFailGo(m_pStgdb->m_MiniMd.PutStringW(TBL_ManifestResource,
                                    ManifestResourceRec::COL_Name, pRecord, szName));
    }

    // Set the rest of the attributes.
    IfFailGo(_SetManifestResourceProps(*pmmr, tkImplementation,
                                dwOffset, dwResourceFlags));

ErrExit:

    STOP_MD_PERF(DefineManifestResource);
    END_ENTRYPOINT_NOTHROW;

    return hr;
}   // RegMeta::DefineManifestResource

//*******************************************************************************
// Set the specified attributes on the given Assembly token.
//*******************************************************************************
STDMETHODIMP RegMeta::SetAssemblyProps(       // S_OK or error.
    mdAssembly  ma,                     // [IN] Assembly token.
    const void  *pbPublicKey,           // [IN] Public key of the assembly.
    ULONG       cbPublicKey,            // [IN] Count of bytes in the public key.
    ULONG       ulHashAlgId,            // [IN] Hash Algorithm.
    LPCWSTR     szName,                 // [IN] Name of the assembly.
    const ASSEMBLYMETADATA *pMetaData,  // [IN] Assembly MetaData.
    DWORD       dwAssemblyFlags)        // [IN] Flags.
{
    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    _ASSERTE(TypeFromToken(ma) == mdtAssembly && RidFromToken(ma));

    LOG((LOGMD, "RegMeta::SetAssemblyProps(%#08x, %#08x, %#08x, %#08x %S, %#08x, %#08x)\n",
        ma, pbPublicKey, cbPublicKey, ulHashAlgId, MDSTR(szName), pMetaData, dwAssemblyFlags));

    START_MD_PERF();
    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    IfFailGo(_SetAssemblyProps(ma, pbPublicKey, cbPublicKey, ulHashAlgId, szName, pMetaData, dwAssemblyFlags));

ErrExit:
    STOP_MD_PERF(SetAssemblyProps);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP SetAssemblyProps()

//*******************************************************************************
// Set the specified attributes on the given AssemblyRef token.
//*******************************************************************************
STDMETHODIMP RegMeta::SetAssemblyRefProps(    // S_OK or error.
    mdAssemblyRef ar,                   // [IN] AssemblyRefToken.
    const void  *pbPublicKeyOrToken,    // [IN] Public key or token of the assembly.
    ULONG       cbPublicKeyOrToken,     // [IN] Count of bytes in the public key or token.
    LPCWSTR     szName,                 // [IN] Name of the assembly being referenced.
    const ASSEMBLYMETADATA *pMetaData,  // [IN] Assembly MetaData.
    const void  *pbHashValue,           // [IN] Hash Blob.
    ULONG       cbHashValue,            // [IN] Count of bytes in the Hash Blob.
    DWORD       dwAssemblyRefFlags)     // [IN] Flags.
{
    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    _ASSERTE(TypeFromToken(ar) == mdtAssemblyRef && RidFromToken(ar));

    LOG((LOGMD, "RegMeta::SetAssemblyRefProps(0x%08x, 0x%08x, 0x%08x, %S, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        ar, pbPublicKeyOrToken, cbPublicKeyOrToken, MDSTR(szName), pMetaData, pbHashValue, cbHashValue,
        dwAssemblyRefFlags));

    START_MD_PERF();
    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    IfFailGo(_SetAssemblyRefProps(
        ar,
        pbPublicKeyOrToken,
        cbPublicKeyOrToken,
        szName,
        pMetaData,
        pbHashValue,
        cbHashValue,
        dwAssemblyRefFlags));

ErrExit:
    STOP_MD_PERF(SetAssemblyRefProps);
    END_ENTRYPOINT_NOTHROW;

    return hr;
}   // RegMeta::SetAssemblyRefProps

//*******************************************************************************
// Set the specified attributes on the given File token.
//*******************************************************************************
STDMETHODIMP RegMeta::SetFileProps(           // S_OK or error.
    mdFile      file,                   // [IN] File token.
    const void  *pbHashValue,           // [IN] Hash Blob.
    ULONG       cbHashValue,            // [IN] Count of bytes in the Hash Blob.
    DWORD       dwFileFlags)            // [IN] Flags.
{
    HRESULT     hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;


    _ASSERTE(TypeFromToken(file) == mdtFile && RidFromToken(file));

    LOG((LOGMD, "RegMeta::SetFileProps(%#08x, %#08x, %#08x, %#08x)\n",
        file, pbHashValue, cbHashValue, dwFileFlags));
    START_MD_PERF();
    LOCKWRITE();

    IfFailGo(m_pStgdb->m_MiniMd.PreUpdate());

    IfFailGo( _SetFileProps(file, pbHashValue, cbHashValue, dwFileFlags) );

ErrExit:

    STOP_MD_PERF(SetFileProps);
    END_ENTRYPOINT_NOTHROW;

    return hr;
}   // RegMeta::SetFileProps

//*******************************************************************************
// Set the specified attributes on the given ExportedType token.
//*******************************************************************************
STDMETHODIMP RegMeta::SetExportedTypeProps(        // S_OK or error.
    mdExportedType   ct,                     // [IN] ExportedType token.
    mdToken     tkImplementation,       // [IN] mdFile or mdAssemblyRef that provides the ExportedType.
    mdTypeDef   tkTypeDef,              // [IN] TypeDef token within the file.
    DWORD       dwExportedTypeFlags)         // [IN] Flags.
{
    HRESULT     hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;


    LOG((LOGMD, "RegMeta::SetExportedTypeProps(%#08x, %#08x, %#08x, %#08x)\n",
        ct, tkImplementation, tkTypeDef, dwExportedTypeFlags));

    START_MD_PERF();
    LOCKWRITE();

    IfFailGo( _SetExportedTypeProps( ct, tkImplementation, tkTypeDef, dwExportedTypeFlags) );

ErrExit:

    STOP_MD_PERF(SetExportedTypeProps);
    END_ENTRYPOINT_NOTHROW;

    return hr;
}   // RegMeta::SetExportedTypeProps

//*******************************************************************************
// Set the specified attributes on the given ManifestResource token.
//*******************************************************************************
STDMETHODIMP RegMeta::SetManifestResourceProps(// S_OK or error.
    mdManifestResource  mr,             // [IN] ManifestResource token.
    mdToken     tkImplementation,       // [IN] mdFile or mdAssemblyRef that provides the resource.
    DWORD       dwOffset,               // [IN] Offset to the beginning of the resource within the file.
    DWORD       dwResourceFlags)        // [IN] Flags.
{
    HRESULT     hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    LOG((LOGMD, "RegMeta::SetManifestResourceProps(%#08x, %#08x, %#08x, %#08x)\n",
        mr, tkImplementation, dwOffset,
        dwResourceFlags));

    _ASSERTE(TypeFromToken(tkImplementation) == mdtFile ||
              TypeFromToken(tkImplementation) == mdtAssemblyRef ||
              tkImplementation == mdTokenNil);

    START_MD_PERF();
    LOCKWRITE();

    IfFailGo( _SetManifestResourceProps( mr, tkImplementation, dwOffset, dwResourceFlags) );

ErrExit:

    STOP_MD_PERF(SetManifestResourceProps);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::SetManifestResourceProps()

//*******************************************************************************
// Helper: Set the specified attributes on the given Assembly token.
//*******************************************************************************
HRESULT RegMeta::_SetAssemblyProps(     // S_OK or error.
    mdAssembly  ma,                     // [IN] Assembly token.
    const void  *pbPublicKey,          // [IN] Originator of the assembly.
    ULONG       cbPublicKey,           // [IN] Count of bytes in the Originator blob.
    ULONG       ulHashAlgId,            // [IN] Hash Algorithm.
    LPCWSTR     szName,                 // [IN] Name of the assembly.
    const ASSEMBLYMETADATA *pMetaData,  // [IN] Assembly MetaData.
    DWORD       dwAssemblyFlags)        // [IN] Flags.
{
    AssemblyRec *pRecord = NULL;        // The assembly record.
    HRESULT     hr = S_OK;

    IfFailGo(m_pStgdb->m_MiniMd.GetAssemblyRecord(RidFromToken(ma), &pRecord));

    // Set the data.
    if (pbPublicKey)
        IfFailGo(m_pStgdb->m_MiniMd.PutBlob(TBL_Assembly, AssemblyRec::COL_PublicKey,
                                pRecord, pbPublicKey, cbPublicKey));
    if (ulHashAlgId != UINT32_MAX)
        pRecord->SetHashAlgId(ulHashAlgId);
    IfFailGo(m_pStgdb->m_MiniMd.PutStringW(TBL_Assembly, AssemblyRec::COL_Name, pRecord, szName));
    if (pMetaData->usMajorVersion != USHRT_MAX)
        pRecord->SetMajorVersion(pMetaData->usMajorVersion);
    if (pMetaData->usMinorVersion != USHRT_MAX)
        pRecord->SetMinorVersion(pMetaData->usMinorVersion);
    if (pMetaData->usBuildNumber != USHRT_MAX)
        pRecord->SetBuildNumber(pMetaData->usBuildNumber);
    if (pMetaData->usRevisionNumber != USHRT_MAX)
        pRecord->SetRevisionNumber(pMetaData->usRevisionNumber);
    if (pMetaData->szLocale)
        IfFailGo(m_pStgdb->m_MiniMd.PutStringW(TBL_Assembly, AssemblyRec::COL_Locale,
                                pRecord, pMetaData->szLocale));

    dwAssemblyFlags = (dwAssemblyFlags & ~afPublicKey) | (cbPublicKey ? afPublicKey : 0);
    pRecord->SetFlags(dwAssemblyFlags);
    IfFailGo(UpdateENCLog(ma));

ErrExit:


    return hr;
} // HRESULT RegMeta::_SetAssemblyProps()

//*******************************************************************************
// Helper: Set the specified attributes on the given AssemblyRef token.
//*******************************************************************************
HRESULT RegMeta::_SetAssemblyRefProps(  // S_OK or error.
    mdAssemblyRef ar,                   // [IN] AssemblyRefToken.
    const void  *pbPublicKeyOrToken,    // [IN] Public key or token of the assembly.
    ULONG       cbPublicKeyOrToken,     // [IN] Count of bytes in the public key or token.
    LPCWSTR     szName,                 // [IN] Name of the assembly being referenced.
    const ASSEMBLYMETADATA *pMetaData,  // [IN] Assembly MetaData.
    const void  *pbHashValue,           // [IN] Hash Blob.
    ULONG       cbHashValue,            // [IN] Count of bytes in the Hash Blob.
    DWORD       dwAssemblyRefFlags)     // [IN] Flags.
{
    AssemblyRefRec *pRecord;
    HRESULT     hr = S_OK;

    IfFailGo(m_pStgdb->m_MiniMd.GetAssemblyRefRecord(RidFromToken(ar), &pRecord));

    if (pbPublicKeyOrToken)
        IfFailGo(m_pStgdb->m_MiniMd.PutBlob(TBL_AssemblyRef, AssemblyRefRec::COL_PublicKeyOrToken,
                                pRecord, pbPublicKeyOrToken, cbPublicKeyOrToken));
    if (szName)
        IfFailGo(m_pStgdb->m_MiniMd.PutStringW(TBL_AssemblyRef, AssemblyRefRec::COL_Name,
                                pRecord, szName));
    if (pMetaData)
    {
        if (pMetaData->usMajorVersion != USHRT_MAX)
            pRecord->SetMajorVersion(pMetaData->usMajorVersion);
        if (pMetaData->usMinorVersion != USHRT_MAX)
            pRecord->SetMinorVersion(pMetaData->usMinorVersion);
        if (pMetaData->usBuildNumber != USHRT_MAX)
            pRecord->SetBuildNumber(pMetaData->usBuildNumber);
        if (pMetaData->usRevisionNumber != USHRT_MAX)
            pRecord->SetRevisionNumber(pMetaData->usRevisionNumber);
        if (pMetaData->szLocale)
            IfFailGo(m_pStgdb->m_MiniMd.PutStringW(TBL_AssemblyRef,
                    AssemblyRefRec::COL_Locale, pRecord, pMetaData->szLocale));

    }
    if (pbHashValue)
        IfFailGo(m_pStgdb->m_MiniMd.PutBlob(TBL_AssemblyRef, AssemblyRefRec::COL_HashValue,
                                    pRecord, pbHashValue, cbHashValue));
    if (dwAssemblyRefFlags != UINT32_MAX)
        pRecord->SetFlags(PrepareForSaving(dwAssemblyRefFlags));

    IfFailGo(UpdateENCLog(ar));

ErrExit:


    return hr;
}   // RegMeta::_SetAssemblyRefProps

//*******************************************************************************
// Helper: Set the specified attributes on the given File token.
//*******************************************************************************
HRESULT RegMeta::_SetFileProps(         // S_OK or error.
    mdFile      file,                   // [IN] File token.
    const void  *pbHashValue,           // [IN] Hash Blob.
    ULONG       cbHashValue,            // [IN] Count of bytes in the Hash Blob.
    DWORD       dwFileFlags)            // [IN] Flags.
{
    FileRec     *pRecord;
    HRESULT     hr = S_OK;

    IfFailGo(m_pStgdb->m_MiniMd.GetFileRecord(RidFromToken(file), &pRecord));

    if (pbHashValue)
        IfFailGo(m_pStgdb->m_MiniMd.PutBlob(TBL_File, FileRec::COL_HashValue, pRecord,
                                    pbHashValue, cbHashValue));
    if (dwFileFlags != UINT32_MAX)
        pRecord->SetFlags(dwFileFlags);

    IfFailGo(UpdateENCLog(file));
ErrExit:
    return hr;
}   // RegMeta::_SetFileProps

//*******************************************************************************
// Helper: Set the specified attributes on the given ExportedType token.
//*******************************************************************************
HRESULT RegMeta::_SetExportedTypeProps( // S_OK or error.
    mdExportedType   ct,                // [IN] ExportedType token.
    mdToken     tkImplementation,       // [IN] mdFile or mdAssemblyRef that provides the ExportedType.
    mdTypeDef   tkTypeDef,              // [IN] TypeDef token within the file.
    DWORD       dwExportedTypeFlags)    // [IN] Flags.
{
    ExportedTypeRec  *pRecord;
    HRESULT     hr = S_OK;

    IfFailGo(m_pStgdb->m_MiniMd.GetExportedTypeRecord(RidFromToken(ct), &pRecord));

    if(! IsNilToken(tkImplementation))
        IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_ExportedType, ExportedTypeRec::COL_Implementation,
                                        pRecord, tkImplementation));
    if (! IsNilToken(tkTypeDef))
    {
        _ASSERTE(TypeFromToken(tkTypeDef) == mdtTypeDef);
        pRecord->SetTypeDefId(tkTypeDef);
    }
    if (dwExportedTypeFlags != UINT32_MAX)
        pRecord->SetFlags(dwExportedTypeFlags);

    IfFailGo(UpdateENCLog(ct));
ErrExit:
    return hr;
}   // RegMeta::_SetExportedTypeProps

//*******************************************************************************
// Helper: Set the specified attributes on the given ManifestResource token.
//*******************************************************************************
HRESULT RegMeta::_SetManifestResourceProps(// S_OK or error.
    mdManifestResource  mr,             // [IN] ManifestResource token.
    mdToken     tkImplementation,       // [IN] mdFile or mdAssemblyRef that provides the resource.
    DWORD       dwOffset,               // [IN] Offset to the beginning of the resource within the file.
    DWORD       dwResourceFlags)        // [IN] Flags.
{
    ManifestResourceRec *pRecord = NULL;
    HRESULT     hr = S_OK;

    IfFailGo(m_pStgdb->m_MiniMd.GetManifestResourceRecord(RidFromToken(mr), &pRecord));

    // Set the attributes.
    if (tkImplementation != mdTokenNil)
        IfFailGo(m_pStgdb->m_MiniMd.PutToken(TBL_ManifestResource,
                    ManifestResourceRec::COL_Implementation, pRecord, tkImplementation));
    if (dwOffset != UINT32_MAX)
        pRecord->SetOffset(dwOffset);
    if (dwResourceFlags != UINT32_MAX)
        pRecord->SetFlags(dwResourceFlags);

    IfFailGo(UpdateENCLog(mr));

ErrExit:
    return hr;
} // RegMeta::_SetManifestResourceProps

#endif //FEATURE_METADATA_EMIT
