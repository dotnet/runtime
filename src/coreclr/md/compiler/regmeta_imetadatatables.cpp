// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: RegMeta_IMetaDataTables.cpp
//

//
// Methods of code:RegMeta class which implement public API interfaces code:IMetaDataTables:
//  * code:RegMeta::GetStringHeapSize
//  * code:RegMeta::GetBlobHeapSize
//  * code:RegMeta::GetGuidHeapSize
//  * code:RegMeta::GetUserStringHeapSize
//
//  * code:RegMeta#GetString_IMetaDataTables
//  * code:RegMeta#GetBlob_IMetaDataTables
//  * code:RegMeta#GetGuid_IMetaDataTables
//  * code:RegMeta#GetUserString_IMetaDataTables
//
//  * code:RegMeta::GetNextString
//  * code:RegMeta::GetNextBlob
//  * code:RegMeta::GetNextGuid
//  * code:RegMeta::GetNextUserString
//
//  * code:RegMeta::GetNumTables
//  * code:RegMeta::GetTableIndex
//  * code:RegMeta::GetTableInfo
//  * code:RegMeta::GetColumnInfo
//  * code:RegMeta::GetCodedTokenInfo
//  * code:RegMeta::GetRow
//  * code:RegMeta::GetColumn
//
// Methods of code:RegMeta class which implement public API interfaces code:IMetaDataTables2:
//  * code:RegMeta::GetMetaDataStorage
//  * code:RegMeta::GetMetaDataStreamInfo
//
// ======================================================================================

#include "stdafx.h"
#include "regmeta.h"

// --------------------------------------------------------------------------------------
//
// Fills size (*pcbStringsHeapSize) of internal strings heap (#String).
// Returns S_OK or error code. Fills *pcbStringsHeapSize with 0 on error.
// Implements public API code:IMetaDataTables::GetStringHeapSize.
//
HRESULT
RegMeta::GetStringHeapSize(
    _Out_ ULONG *pcbStringsHeapSize)    // [OUT] Size of the string heap.
{
    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    *pcbStringsHeapSize = m_pStgdb->m_MiniMd.m_StringHeap.GetUnalignedSize();

    END_ENTRYPOINT_NOTHROW;
    return hr;
} // RegMeta::GetStringHeapSize

// --------------------------------------------------------------------------------------
//
// Fills size (*pcbBlobsHeapSize) of blobs heap (#Blob).
// Returns S_OK or error code. Fills *pcbBlobsHeapSize with 0 on error.
// Implements public API code:IMetaDataTables::GetBlobHeapSize.
//
HRESULT
RegMeta::GetBlobHeapSize(
    _Out_ ULONG *pcbBlobsHeapSize)  // [OUT] Size of the blob heap.
{
    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    *pcbBlobsHeapSize = m_pStgdb->m_MiniMd.m_BlobHeap.GetUnalignedSize();

    END_ENTRYPOINT_NOTHROW;
    return hr;
} // RegMeta::GetBlobHeapSize

// --------------------------------------------------------------------------------------
//
// Fills size (*pcbGuidsHeapSize) of guids heap (#GUID).
// Returns S_OK or error code. Fills *pcbGuidsHeapSize with 0 on error.
// Implements public API code:IMetaDataTables::GetGuidHeapSize.
//
HRESULT
RegMeta::GetGuidHeapSize(
    _Out_ ULONG *pcbGuidsHeapSize)      // [OUT] Size of the Guid heap.
{
    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    *pcbGuidsHeapSize = m_pStgdb->m_MiniMd.m_GuidHeap.GetSize();

    END_ENTRYPOINT_NOTHROW;
    return hr;
} // RegMeta::GetGuidHeapSize

// --------------------------------------------------------------------------------------
//
// Fills size (*pcbUserStringsHeapSize) of user strings heap (#US) (referenced from IL).
// Returns S_OK or error code. Fills *pcbUserStringsHeapSize with 0 on error.
// Implements public API code:IMetaDataTables::GetUserStringHeapSize.
//
HRESULT
RegMeta::GetUserStringHeapSize(
    _Out_ ULONG *pcbUserStringsHeapSize)    // [OUT] Size of the user string heap.
{
    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    *pcbUserStringsHeapSize = m_pStgdb->m_MiniMd.m_UserStringHeap.GetUnalignedSize();

    END_ENTRYPOINT_NOTHROW;
    return hr;
} // RegMeta::GetUserStringHeapSize

// --------------------------------------------------------------------------------------
//
//#GetString_IMetaDataTables
//
// Fills internal null-terminated string (*pszString) at index ixString from string heap (#String).
// Returns S_OK (even for index 0) or error code (if index is invalid, fills *pszString with NULL then).
// Implements public API code:IMetaDataTables::GetString.
//
HRESULT RegMeta::GetString(
    ULONG        ixString,      // [IN] Value from a string column.
    const char **pszString)     // [OUT] Put a pointer to the string here.
{
    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    IfFailGo(m_pStgdb->m_MiniMd.getString(
        ixString,
        pszString));

    _ASSERTE(hr == S_OK);
    goto Exit;
ErrExit:
    *pszString = NULL;
Exit:
    END_ENTRYPOINT_NOTHROW;
    return hr;
} // RegMeta::GetString

// --------------------------------------------------------------------------------------
//
//#GetBlob_IMetaDataTables
//
// Fills blob entry (*ppvData of size *pcbDataSize) at index ixBlob from blob heap (#Blob).
// Returns S_OK (even for index 0) or error code (if index is invalid, fills NULL and o then).
// Implements public API code:IMetaDataTables::GetBlob.
//
HRESULT RegMeta::GetBlob(
                ULONG        ixBlob,        // [IN] Value from a blob column.
    _Out_       ULONG       *pcbDataSize,   // [OUT] Put size of the blob here.
    _Outptr_ const void **ppvData)       // [OUT] Put a pointer to the blob here.
{
    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    MetaData::DataBlob dataBlob;
    IfFailGo(m_pStgdb->m_MiniMd.getBlob(ixBlob, &dataBlob));

    *ppvData = (const void *)dataBlob.GetDataPointer();
    *pcbDataSize = dataBlob.GetSize();

    _ASSERTE(hr == S_OK);
    goto Exit;
ErrExit:
    *ppvData = NULL;
    *pcbDataSize = 0;
Exit:
    END_ENTRYPOINT_NOTHROW;
    return hr;
} // RegMeta::GetBlob

// --------------------------------------------------------------------------------------
//
//#GetGuid_IMetaDataTables
//
// Fills guid (*ppGuid) at index ixGuid from guid heap (#GUID).
// Returns S_OK and fills *ppGuid. Returns S_OK even for (invalid) index 0 (fills *ppGuid with pointer to
// zeros then).
// Retruns error code (if index is invalid except 0, fills NULL and o then).
// Implements public API code:IMetaDataTables::GetGuid.
//
// Backward compatibility: returns S_OK even if the index is 0 which is invalid as specified in CLI ECMA
// specification. In that case returns pointer to GUID from zeros.
//
HRESULT RegMeta::GetGuid(
    ULONG        ixGuid,    // [IN] Value from a guid column.
    const GUID **ppGuid)    // [OUT] Put a pointer to the GUID here.
{
    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    REGMETA_POSSIBLE_INTERNAL_POINTER_EXPOSED();

    if (ixGuid == 0)
    {
        // Return zeros
        *ppGuid = GetPublicApiCompatibilityZeros<const GUID>();
        hr = S_OK;
    }
    else
    {
        IfFailGo(m_pStgdb->m_MiniMd.m_GuidHeap.GetGuid(
            ixGuid,
            ppGuid));
    }

ErrExit:
    END_ENTRYPOINT_NOTHROW;
    return hr;
} // RegMeta::GetGuid

// --------------------------------------------------------------------------------------
//
//#GetUserString_IMetaDataTables
//
// Fills user string (*ppvData of size *pcbDataSize) at index ixUserString.
// Returns S_OK (even for index 0) or error code (if index is invalid, fills NULL and o then).
// Implements public API code:IMetaDataTables::GetUserString.
//
HRESULT
RegMeta::GetUserString(
                    ULONG        ixUserString,      // [IN] Value from a UserString column.
    _Out_           ULONG       *pcbDataSize,       // [OUT] Put size of the UserString here.
    _Outptr_opt_ const void **ppvData)           // [OUT] Put a pointer to the UserString here.
{
    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    MetaData::DataBlob userString;
    IfFailGo(m_pStgdb->m_MiniMd.GetUserString(ixUserString, &userString));
    _ASSERTE(hr == S_OK);

    *ppvData = (const void *)userString.GetDataPointer();
    *pcbDataSize = userString.GetSize();

    goto Exit;
ErrExit:
    *ppvData = NULL;
    *pcbDataSize = 0;
Exit:
    END_ENTRYPOINT_NOTHROW;
    return hr;
} // RegMeta::GetUserString

// --------------------------------------------------------------------------------------
//
//#GetNextString_IMetaDataTables
//
// Fills index of string (*pixNextString) from the internal strings heap (#String) starting behind string
// at index ixString.
// Returns S_OK or S_FALSE (if either index is invalid). Fills *pixNextString with 0 on S_FALSE.
// Implements public API code:IMetaDataTables::GetNextString.
//
HRESULT
RegMeta::GetNextString(
          ULONG  ixString,        // [IN] Value from a string column.
    _Out_ ULONG *pixNextString)   // [OUT] Put the index of the next string here.
{
    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    REGMETA_POSSIBLE_INTERNAL_POINTER_EXPOSED();

    // Get string at index ixString
    LPCSTR szString;
    IfFailGo(m_pStgdb->m_MiniMd.m_StringHeap.GetString(
        ixString,
        &szString));
    _ASSERTE(hr == S_OK);

    // Get index behind the string - doesn't overflow, because string heap was verified
    UINT32 ixNextString;
    ixNextString = ixString + (UINT32)(strlen(szString) + 1);

    // Verify that the next index is in the string heap
    if (!m_pStgdb->m_MiniMd.m_StringHeap.IsValidIndex(ixNextString))
    {   // The next index is invalid
        goto ErrExit;
    }
    // The next index is valid
    *pixNextString = ixNextString;
    goto Exit;

ErrExit:
    // Fill output parameters on error
    *pixNextString = 0;
    // Return S_FALSE if either of the string indexes is invalid (backward API compatibility)
    hr = S_FALSE;
Exit:
    END_ENTRYPOINT_NOTHROW;
    return hr;
} // RegMeta::GetNextString

// --------------------------------------------------------------------------------------
//
//#GetNextBlob_IMetaDataTables
//
// Fills index of blob (*pixNextBlob) from the blobs heap (#Blob) starting behind blob at index ixBlob.
// Returns S_OK or S_FALSE (if either index is invalid). Fills *pixNextBlob with 0 on S_FALSE.
// Implements public API code:IMetaDataTables::GetNextString.
//
HRESULT
RegMeta::GetNextBlob(
          ULONG  ixBlob,        // [IN] Value from a blob column.
    _Out_ ULONG *pixNextBlob)   // [OUT] Put the index of the next blob here.
{
    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    REGMETA_POSSIBLE_INTERNAL_POINTER_EXPOSED();

    // Get blob at index ixBlob (verifies that the blob is in the blob heap)
    MetaData::DataBlob blob;
    IfFailGo(m_pStgdb->m_MiniMd.m_BlobHeap.GetBlobWithSizePrefix(
        ixBlob,
        &blob));
    _ASSERTE(hr == S_OK);

    // Get index behind the blob - doesn't overflow, because the blob is in the blob heap
    UINT32 ixNextBlob;
    ixNextBlob = ixBlob + blob.GetSize();

    // Verify that the next index is in the blob heap
    if (!m_pStgdb->m_MiniMd.m_BlobHeap.IsValidIndex(ixNextBlob))
    {   // The next index is invalid
        goto ErrExit;
    }
    // The next index is valid
    *pixNextBlob = ixNextBlob;
    goto Exit;

ErrExit:
    // Fill output parameters on error
    *pixNextBlob = 0;
    // Return S_FALSE if either of the string indexes is invalid (backward API compatibility)
    hr = S_FALSE;
Exit:
    END_ENTRYPOINT_NOTHROW;
    return hr;
} // RegMeta::GetNextBlob

// --------------------------------------------------------------------------------------
//
//#GetNextGuid_IMetaDataTables
//
// Fills index of guid (*pixNextGuid) from the guids heap (#GUID) starting behind guid at index ixGuid.
// Returns S_OK or S_FALSE (if the new index is invalid). Fills *pixNextGuid with 0 on S_FALSE.
// Implements public API code:IMetaDataTables::GetNextGuid.
//
// Backward compatibility: returns S_OK even if the guid index (ixGuid) is 0 which is invalid as
// specified in CLI ECMA specification.
//
HRESULT
RegMeta::GetNextGuid(
          ULONG  ixGuid,            // [IN] Value from a guid column.
    _Out_ ULONG *pixNextGuid)       // [OUT] Put the index of the next guid here.
{
    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    S_UINT32 ixNextGuid = S_UINT32(ixGuid) + S_UINT32(1);
    if (ixNextGuid.IsOverflow())
    {   // It's invalid index (UINT32_MAX)
        goto ErrExit;
    }
    if (!m_pStgdb->m_MiniMd.m_GuidHeap.IsValidIndex(ixNextGuid.Value()))
    {   // The next index is invalid
        goto ErrExit;
    }
    _ASSERTE(hr == S_OK);
    // The next index is valid
    *pixNextGuid = ixNextGuid.Value();
    goto Exit;

ErrExit:
    // Fill output parameters on error
    *pixNextGuid = 0;
    // Return S_FALSE if next guid index is invalid (backward API compatibility)
    hr = S_FALSE;
Exit:
    END_ENTRYPOINT_NOTHROW;
    return hr;
} // RegMeta::GetNextGuid

// --------------------------------------------------------------------------------------
//
//#GetNextUserString_IMetaDataTables
//
// Fills index of user string (*pixNextUserString) from the user strings heap (#US) starting behind string
// at index ixUserString.
// Returns S_OK or S_FALSE (if either index is invalid). Fills *pixNextUserString with 0 on S_FALSE.
// Implements public API code:IMetaDataTables::GetNextUserString.
//
// Backward compatibility: returns S_OK even if the string doesn't have odd number of bytes as specified
// in CLI ECMA specification.
//
HRESULT
RegMeta::GetNextUserString(
          ULONG  ixUserString,          // [IN] Value from a UserString column.
    _Out_ ULONG *pixNextUserString)     // [OUT] Put the index of the next user string here.
{
    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    REGMETA_POSSIBLE_INTERNAL_POINTER_EXPOSED();

    // Get user string at index ixUserString (verifies that the user string is in the heap)
    MetaData::DataBlob userString;
    IfFailGo(m_pStgdb->m_MiniMd.m_UserStringHeap.GetBlobWithSizePrefix(
        ixUserString,
        &userString));
    _ASSERTE(hr == S_OK);

    // Get index behind the user string - doesn't overflow, because the user string is in the heap
    UINT32 ixNextUserString;
    ixNextUserString = ixUserString + userString.GetSize();

    // Verify that the next index is in the user string heap
    if (!m_pStgdb->m_MiniMd.m_UserStringHeap.IsValidIndex(ixNextUserString))
    {   // The next index is invalid
        goto ErrExit;
    }
    // The next index is valid
    *pixNextUserString = ixNextUserString;
    goto Exit;

ErrExit:
    // Fill output parameters on error
    *pixNextUserString = 0;
    // Return S_FALSE if either of the string indexes is invalid (backward API compatibility)
    hr = S_FALSE;
Exit:
    END_ENTRYPOINT_NOTHROW;
    return hr;
} // RegMeta::GetNextUserString

// --------------------------------------------------------------------------------------
//
// Implements public API code:IMetaDataTables::GetNumTables.
//
HRESULT
RegMeta::GetNumTables(
    _Out_ ULONG *pcTables)  // [OUT] Count of tables.
{
    BEGIN_ENTRYPOINT_NOTHROW;

    // These are for dumping metadata information.
    // We probably don't need to do any lock here.

    *pcTables = m_pStgdb->m_MiniMd.GetCountTables();
    END_ENTRYPOINT_NOTHROW;

    return S_OK;
} // RegMeta::GetNumTables

// --------------------------------------------------------------------------------------
//
// Implements public API code:IMetaDataTables::GetTableIndex.
//
HRESULT
RegMeta::GetTableIndex(
          ULONG  token,     // [IN] Token for which to get table index.
    _Out_ ULONG *pixTbl)    // [OUT] Put table index here.
{
    BEGIN_ENTRYPOINT_NOTHROW;

    // These are for dumping metadata information.
    // We probably don't need to do any lock here.

    *pixTbl = CMiniMdRW::GetTableForToken(token);
    END_ENTRYPOINT_NOTHROW;

    return S_OK;
} // RegMeta::GetTableIndex

// --------------------------------------------------------------------------------------
//
// Implements public API code:IMetaDataTables::GetTableInfo.
//
HRESULT
RegMeta::GetTableInfo(
    ULONG        ixTbl,     // [IN] Which table.
    ULONG       *pcbRow,    // [OUT] Size of a row, bytes.
    ULONG       *pcRows,    // [OUT] Number of rows.
    ULONG       *pcCols,    // [OUT] Number of columns in each row.
    ULONG       *piKey,     // [OUT] Key column, or -1 if none.
    const char **ppName)    // [OUT] Name of the table.
{
    HRESULT        hr = S_OK;
    CMiniTableDef *pTbl = NULL;

    BEGIN_ENTRYPOINT_NOTHROW;

    // These are for dumping metadata information.
    // We probably don't need to do any lock here.

    if (ixTbl >= m_pStgdb->m_MiniMd.GetCountTables())
        IfFailGo(E_INVALIDARG);
    pTbl = &m_pStgdb->m_MiniMd.m_TableDefs[ixTbl];
    if (pcbRow != NULL)
        *pcbRow = pTbl->m_cbRec;
    if (pcRows != NULL)
        *pcRows = m_pStgdb->m_MiniMd.GetCountRecs(ixTbl);
    if (pcCols != NULL)
        *pcCols = pTbl->m_cCols;
    if (piKey != NULL)
        *piKey = (pTbl->m_iKey == (BYTE) -1) ? -1 : pTbl->m_iKey;
    if (ppName != NULL)
        *ppName = g_Tables[ixTbl].m_pName;

ErrExit:
    END_ENTRYPOINT_NOTHROW;
    return hr;
} // RegMeta::GetTableInfo

// --------------------------------------------------------------------------------------
//
// Implements public API code:IMetaDataTables::GetColumnInfo.
//
HRESULT
RegMeta::GetColumnInfo(
    ULONG        ixTbl,     // [IN] Which Table
    ULONG        ixCol,     // [IN] Which Column in the table
    ULONG       *poCol,     // [OUT] Offset of the column in the row.
    ULONG       *pcbCol,    // [OUT] Size of a column, bytes.
    ULONG       *pType,     // [OUT] Type of the column.
    const char **ppName)    // [OUT] Name of the Column.
{
    HRESULT        hr = S_OK;
    CMiniTableDef *pTbl = NULL;
    CMiniColDef   *pCol = NULL;

    BEGIN_ENTRYPOINT_NOTHROW;

    // These are for dumping metadata information.
    // We probably don't need to do any lock here.

    if (ixTbl >= m_pStgdb->m_MiniMd.GetCountTables())
        IfFailGo(E_INVALIDARG);
    pTbl = &m_pStgdb->m_MiniMd.m_TableDefs[ixTbl];
    if (ixCol >= pTbl->m_cCols)
        IfFailGo(E_INVALIDARG);
    pCol = &pTbl->m_pColDefs[ixCol];
    if (poCol != NULL)
        *poCol = pCol->m_oColumn;
    if (pcbCol != NULL)
        *pcbCol = pCol->m_cbColumn;
    if (pType != NULL)
        *pType = pCol->m_Type;
    if (ppName != NULL)
        *ppName = g_Tables[ixTbl].m_pColNames[ixCol];

ErrExit:
    END_ENTRYPOINT_NOTHROW;
    return hr;
} // RegMeta::GetColumnInfo

// --------------------------------------------------------------------------------------
//
// Implements public API code:IMetaDataTables::GetCodedTokenInfo.
//
HRESULT
RegMeta::GetCodedTokenInfo(
    ULONG        ixCdTkn,       // [IN] Which kind of coded token.
    ULONG       *pcTokens,      // [OUT] Count of tokens.
    ULONG      **ppTokens,      // [OUT] List of tokens.
    const char **ppName)        // [OUT] Name of the CodedToken.
{
    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    // These are for dumping metadata information.
    // We probably don't need to do any lock here.

    // Validate arguments.
    if (ixCdTkn >= CDTKN_COUNT)
        IfFailGo(E_INVALIDARG);

    if (pcTokens != NULL)
        *pcTokens = g_CodedTokens[ixCdTkn].m_cTokens;
    if (ppTokens != NULL)
        *ppTokens = (ULONG*)g_CodedTokens[ixCdTkn].m_pTokens;
    if (ppName != NULL)
        *ppName = g_CodedTokens[ixCdTkn].m_pName;

ErrExit:
    END_ENTRYPOINT_NOTHROW;
    return hr;
} // RegMeta::GetCodedTokenInfo

// --------------------------------------------------------------------------------------
//
// Implements public API code:IMetaDataTables::GetRow.
//
HRESULT
RegMeta::GetRow(
    ULONG  ixTbl,       // [IN] Which table.
    ULONG  rid,         // [IN] Which row.
    void **ppRow)       // [OUT] Put pointer to row here.
{
    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    // These are for dumping metadata information.
    // We probably don't need to do any lock here.

    // Validate arguments.
    if (ixTbl >= m_pStgdb->m_MiniMd.GetCountTables())
        IfFailGo(E_INVALIDARG);
    if (rid == 0 || rid > m_pStgdb->m_MiniMd.m_Schema.m_cRecs[ixTbl])
        IfFailGo(E_INVALIDARG);

    // Get the row.
    IfFailGo(m_pStgdb->m_MiniMd.getRow(ixTbl, rid, ppRow));

ErrExit:
    END_ENTRYPOINT_NOTHROW;
    return hr;
} // RegMeta::GetRow

// --------------------------------------------------------------------------------------
//
// Implements public API code:IMetaDataTables::GetColumn.
//
HRESULT
RegMeta::GetColumn(
    ULONG  ixTbl,       // [IN] Which table.
    ULONG  ixCol,       // [IN] Which column.
    ULONG  rid,         // [IN] Which row.
    ULONG *pVal)        // [OUT] Put the column contents here.
{
    HRESULT        hr = S_OK;
    CMiniColDef   *pCol = NULL;
    CMiniTableDef *pTbl = NULL;

    BEGIN_ENTRYPOINT_NOTHROW;

    // These are for dumping metadata information.
    // We probably don't need to do any lock here.

    void *pRow = NULL;      // Row with data.

    // Validate arguments.
    if (ixTbl >= m_pStgdb->m_MiniMd.GetCountTables())
        IfFailGo(E_INVALIDARG);
    pTbl = &m_pStgdb->m_MiniMd.m_TableDefs[ixTbl];
    if (ixCol >= pTbl->m_cCols)
        IfFailGo(E_INVALIDARG);
    if (rid == 0 || rid > m_pStgdb->m_MiniMd.m_Schema.m_cRecs[ixTbl])
        IfFailGo(E_INVALIDARG);

    // Get the row.
    IfFailGo(m_pStgdb->m_MiniMd.getRow(ixTbl, rid, &pRow));

    // Is column a token column?
    pCol = &pTbl->m_pColDefs[ixCol];
    if (pCol->m_Type <= iCodedTokenMax)
    {
        *pVal = m_pStgdb->m_MiniMd.GetToken(ixTbl, ixCol, pRow);
    }
    else
    {
        *pVal = m_pStgdb->m_MiniMd.GetCol(ixTbl, ixCol, pRow);
    }

ErrExit:
    END_ENTRYPOINT_NOTHROW;
    return hr;
} // RegMeta::GetColumn

// --------------------------------------------------------------------------------------
//
// Implements public API code:IMetaDataTables2::GetMetaDataStorage.
//
HRESULT
RegMeta::GetMetaDataStorage(
    const void **ppvMd,     // [OUT] put pointer to MD section here (aka, 'BSJB').
    ULONG       *pcbMd)     // [OUT] put size of the stream here.
{
    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;
    REGMETA_POSSIBLE_INTERNAL_POINTER_EXPOSED();

    hr = m_pStgdb->GetRawData(ppvMd, pcbMd);

    END_ENTRYPOINT_NOTHROW;
    return hr;
} // RegMeta::GetMetaDataStorage

// --------------------------------------------------------------------------------------
//
// Implements public API code:IMetaDataTables2::GetMetaDataStreamInfo.
//
HRESULT
RegMeta::GetMetaDataStreamInfo(
    ULONG        ix,            // [IN] Stream ordinal desired.
    const char **ppchName,      // [OUT] put pointer to stream name here.
    const void **ppv,           // [OUT] put pointer to MD stream here.
    ULONG       *pcb)           // [OUT] put size of the stream here.
{
    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;
    REGMETA_POSSIBLE_INTERNAL_POINTER_EXPOSED();

    hr = m_pStgdb->GetRawStreamInfo(ix, ppchName, ppv, pcb);

    END_ENTRYPOINT_NOTHROW;
    return hr;
} // RegMeta::GetMetaDataStreamInfo
