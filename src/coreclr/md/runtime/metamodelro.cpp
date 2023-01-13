// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// MetaModelRO.cpp -- Read-only implementation of compressed COM+ metadata.
//

//
//*****************************************************************************
#include "stdafx.h"

#include "metamodelro.h"
#include <posterror.h>
#include <corerror.h>

//*****************************************************************************
// Set the pointers to consecutive areas of a large buffer.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMd::InitializeTables(
    MetaData::DataBlob tablesData)
{
    HRESULT hr;

    for (int i = 0; i < TBL_COUNT; i++)
    {
        // This table data
        MetaData::DataBlob tableData;

        S_UINT32 cbTableSize =
            S_UINT32(m_TableDefs[i].m_cbRec) *
            S_UINT32(m_Schema.m_cRecs[i]);
        if (cbTableSize.IsOverflow())
        {
            Debug_ReportError("Table is too large - size overflow.");
            return CLDB_E_FILE_CORRUPT;
        }
        if (!tablesData.GetDataOfSize(cbTableSize.Value(), &tableData))
        {
            Debug_ReportError("Table is not within MetaData tables block.");
            return CLDB_E_FILE_CORRUPT;
        }
        _ASSERTE(cbTableSize.Value() == tableData.GetSize());

        IfFailRet(m_Tables[i].Initialize(
            m_TableDefs[i].m_cbRec,
            tableData,
            FALSE));    // fCopyData
    }

    return S_OK;
} // CMiniMd::SetTablePointers

//*****************************************************************************
// Given a buffer that contains a MiniMd, init to read it.
//*****************************************************************************
HRESULT
CMiniMd::InitOnMem(
    void *pvBuf,        // The buffer.
    ULONG ulBufLen)     // Size of the buffer..
{
    HRESULT hr = S_OK;
    ULONG   cbData;
    BYTE   *pBuf = reinterpret_cast<BYTE*>(pvBuf);

    // Uncompress the schema from the buffer into our structures.
    IfFailGo(SchemaPopulate(pvBuf, ulBufLen, &cbData));
    PREFAST_ASSUME(cbData <= ulBufLen);

    // There shouldn't be any pointer tables.
    if ((m_Schema.m_cRecs[TBL_MethodPtr] != 0) || (m_Schema.m_cRecs[TBL_FieldPtr] != 0))
    {
        Debug_ReportError("MethodPtr and FieldPtr tables are not allowed in Read-Only format.");
        return PostError(CLDB_E_FILE_CORRUPT);
    }

    // initialize the pointers to the rest of the data.
    IfFailGo(InitializeTables(MetaData::DataBlob(pBuf + Align4(cbData), ulBufLen-cbData)));

ErrExit:
    return hr;
} // CMiniMd::InitOnMem

//*****************************************************************************
// Validate cross-stream consistency.
//*****************************************************************************
HRESULT
CMiniMd::PostInit(
    int iLevel)
{
    return S_OK;
} // CMiniMd::PostInit

//*****************************************************************************
// converting a ANSI heap string to unicode string to an output buffer
//*****************************************************************************
HRESULT
CMiniMd::Impl_GetStringW(
    ULONG  ix,
    __inout_ecount (cchBuffer) LPWSTR szOut,
    ULONG  cchBuffer,
    ULONG *pcchBuffer)
{
    LPCSTR  szString;       // Single byte version.
    int     iSize;          // Size of resulting string, in wide chars.
    HRESULT hr = NOERROR;

    IfFailGo(getString(ix, &szString));

    if (*szString == 0)
    {
        // If empty string "", return pccBuffer 0
        if ((szOut != NULL) && (cchBuffer != 0))
            szOut[0] = W('\0');
        if (pcchBuffer != NULL)
            *pcchBuffer = 0;
        goto ErrExit;
    }
    iSize = ::WszMultiByteToWideChar(CP_UTF8, 0, szString, -1, szOut, cchBuffer);
    if (iSize == 0)
    {
        // What was the problem?
        DWORD dwNT = GetLastError();

        // Not truncation?
        if (dwNT != ERROR_INSUFFICIENT_BUFFER)
            IfFailGo(HRESULT_FROM_NT(dwNT));

        // Truncation error; get the size required.
        if (pcchBuffer != NULL)
            *pcchBuffer = ::WszMultiByteToWideChar(CP_UTF8, 0, szString, -1, NULL, 0);

        if ((szOut != NULL) && (cchBuffer > 0))
        {   // null-terminate the truncated output string
            szOut[cchBuffer - 1] = W('\0');
        }

        hr = CLDB_S_TRUNCATION;
        goto ErrExit;
    }
    if (pcchBuffer != NULL)
        *pcchBuffer = iSize;

ErrExit:
    return hr;
} // CMiniMd::Impl_GetStringW


//*****************************************************************************
// Given a table with a pointer (index) to a sequence of rows in another
//  table, get the RID of the end row.  This is the STL-ish end; the first row
//  not in the list.  Thus, for a list of 0 elements, the start and end will
//  be the same.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMd::Impl_GetEndRidForColumn(   // The End rid.
    UINT32       nTableIndex,
    RID          nRowIndex,
    CMiniColDef &def,                   // Column containing the RID into other table.
    UINT32       nTargetTableIndex,     // The other table.
    RID         *pEndRid)
{
    HRESULT hr;
    _ASSERTE(nTableIndex < TBL_COUNT);
    RID nLastRowIndex = m_Schema.m_cRecs[nTableIndex];

    // Last rid in range from NEXT record, or count of table, if last record.
    if (nRowIndex < nLastRowIndex)
    {
        BYTE *pRow;
        IfFailRet(Impl_GetRow(nTableIndex, nRowIndex + 1, &pRow));
        *pEndRid = getIX(pRow, def);
    }
    else    // Convert count to 1-based rid.
    {
        if (nRowIndex != nLastRowIndex)
        {
            Debug_ReportError("Invalid table row index.");
            IfFailRet(METADATA_E_INDEX_NOTFOUND);
        }
        _ASSERTE(nTargetTableIndex < TBL_COUNT);
        *pEndRid = m_Schema.m_cRecs[nTargetTableIndex] + 1;
    }

    return S_OK;
} // CMiniMd::Impl_GetEndRidForColumn


//*****************************************************************************
// return all found CAs in an enumerator
//*****************************************************************************
HRESULT
CMiniMd::CommonEnumCustomAttributeByName(
    mdToken        tkObj,               // [IN] Object with Custom Attribute.
    LPCUTF8        szName,              // [IN] Name of desired Custom Attribute.
    bool           fStopAtFirstFind,    // [IN] just find the first one
    HENUMInternal *phEnum)              // enumerator to fill up
{
    HRESULT hr = S_OK;
    HRESULT hrRet = S_FALSE;    // Assume that we won't find any
    RID     ridStart, ridEnd;   // Loop start and endpoints.

    _ASSERTE(phEnum != NULL);

    HENUMInternal::ZeroEnum(phEnum);

    HENUMInternal::InitDynamicArrayEnum(phEnum);

    phEnum->m_tkKind = mdtCustomAttribute;

    // Get the list of custom values for the parent object.

    IfFailGo(getCustomAttributeForToken(tkObj, &ridEnd, &ridStart));
    if (ridStart == 0)
        return S_FALSE;

    // Look for one with the given name.
    for (; ridStart < ridEnd; ++ridStart)
    {
        IfFailGoto(CompareCustomAttribute( tkObj, szName, ridStart), ErrExit);
        if (hr == S_OK)
        {
            // If here, found a match.
            hrRet = S_OK;
            IfFailGo(HENUMInternal::AddElementToEnum(
                phEnum,
                TokenFromRid(ridStart, mdtCustomAttribute)));
            if (fStopAtFirstFind)
                goto ErrExit;
        }
    }

ErrExit:
    if (FAILED(hr))
        return hr;
    return hrRet;
} // CMiniMd::CommonEnumCustomAttributeByName


//*****************************************************************************
// Search a table for the row containing the given key value.
//  EG. Constant table has pointer back to Param or Field.
//
//*****************************************************************************
__checkReturn
HRESULT
CMiniMd::vSearchTable(
    ULONG       ixTbl,      // Table to search.
    CMiniColDef sColumn,    // Sorted key column, containing search value.
    ULONG       ulTarget,   // Target for search.
    RID        *pRid)       // RID of matching row, or 0.
{
    HRESULT hr;
    void   *pRow = NULL;    // Row from a table.
    ULONG   val;            // Value from a row.
    int     lo, mid, hi;    // binary search indices.

    // Start with entire table.
    lo = 1;
    hi = GetCountRecs(ixTbl);
    // While there are rows in the range...
    while (lo <= hi)
    {   // Look at the one in the middle.
        mid = (lo + hi) / 2;
        IfFailRet(getRow(ixTbl, mid, &pRow));
        val = getIX_NoLogging(pRow, sColumn);
        // If equal to the target, done.
        if (val == ulTarget)
        {
            *pRid = mid;
            return S_OK;
        }
        // If middle item is too small, search the top half.
        if (val < ulTarget)
            lo = mid + 1;
        else // but if middle is to big, search bottom half.
            hi = mid - 1;
    }
    // Didn't find anything that matched.
    *pRid = 0;

    return S_OK;
} // CMiniMd::vSearchTable

//*****************************************************************************
// Search a table for the highest-RID row containing a value that is less than
//  or equal to the target value.  EG.  TypeDef points to first Field, but if
//  a TypeDef has no fields, it points to first field of next TypeDef.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMd::vSearchTableNotGreater(
    ULONG       ixTbl,          // Table to search.
    CMiniColDef sColumn,        // the column def containing search value
    ULONG       ulTarget,       // target for search
    RID        *pRid)           // RID of matching row, or 0
{
    HRESULT hr;
    void   *pRow = NULL;        // Row from a table.
    ULONG  cRecs;               // Rows in the table.
    ULONG  val = 0;             // Value from a table.
    ULONG  lo, mid = 0, hi;     // binary search indices.

    cRecs = GetCountRecs(ixTbl);

    // Start with entire table.
    lo = 1;
    hi = cRecs;
    // If no recs, return.
    if (lo > hi)
    {
        *pRid = 0;
        return S_OK;
    }
    // While there are rows in the range...
    while (lo <= hi)
    {   // Look at the one in the middle.
        mid = (lo + hi) / 2;
        IfFailRet(getRow(ixTbl, mid, &pRow));
        val = getIX_NoLogging(pRow, sColumn);
        // If equal to the target, done searching.
        if (val == ulTarget)
            break;
        // If middle item is too small, search the top half.
        if (val < ulTarget)
            lo = mid + 1;
        else // but if middle is to big, search bottom half.
            hi = mid - 1;
    }

    // May or may not have found anything that matched.  Mid will be close, but may
    //  be to high or too low.  It should point to the highest acceptable
    //  record.

    // If the value is greater than the target, back up just until the value is
    //  less than or equal to the target.  SHOULD only be one step.
    if (val > ulTarget)
    {
        while (val > ulTarget)
        {
            // If there is nothing else to look at, we won't find it.
            if (--mid < 1)
                break;
            IfFailRet(getRow(ixTbl, mid, &pRow));
            val = getIX(pRow, sColumn);
        }
    }
    else
    {
        // Value is less than or equal to the target.  As long as the next
        //  record is also acceptable, move forward.
        while (mid < cRecs)
        {
            // There is another record.  Get its value.
            IfFailRet(getRow(ixTbl, mid+1, &pRow));
            val = getIX(pRow, sColumn);
            // If that record is too high, stop.
            if (val > ulTarget)
                break;
            mid++;
        }
    }

    // Return the value that's just less than the target.
    *pRid = mid;
    return S_OK;
} // CMiniMd::vSearchTableNotGreater

//*****************************************************************************
// return just the blob value of the first found CA matching the query.
// returns S_FALSE if there is no match
//*****************************************************************************
HRESULT
CMiniMd::CommonGetCustomAttributeByNameEx(
        mdToken            tkObj,            // [IN] Object with Custom Attribute.
        LPCUTF8            szName,           // [IN] Name of desired Custom Attribute.
        mdCustomAttribute *ptkCA,            // [OUT] put custom attribute token here
        const void       **ppData,           // [OUT] Put pointer to data here.
        ULONG             *pcbData)          // [OUT] Put size of data here.
{
    HRESULT             hr;

    ULONG               cbData;
    CustomAttributeRec *pRec;

    RID   ridStart, ridEnd;   // Loop start and endpoints.

    // Get the list of custom values for the parent object.

    IfFailGo(getCustomAttributeForToken(tkObj, &ridEnd, &ridStart));

    hr = S_FALSE;
    if (ridStart == 0)
    {
        goto ErrExit;
    }

    // Look for one with the given name.
    for (; ridStart < ridEnd; ++ridStart)
    {
        IfFailGoto(CompareCustomAttribute( tkObj, szName, ridStart), ErrExit);
        if (hr == S_OK)
        {
            if (ppData != NULL)
            {
                // now get the record out.
                if (pcbData == NULL)
                    pcbData = &cbData;

                IfFailGo(GetCustomAttributeRecord(ridStart, &pRec));
                IfFailGo(getValueOfCustomAttribute(pRec, reinterpret_cast<const BYTE **>(ppData), pcbData));
                if (ptkCA)
                    *ptkCA = TokenFromRid(mdtCustomAttribute, ridStart);
            }
            break;
        }
    }

ErrExit:
    return hr;
} // CMiniMd::CommonGetCustomAttributeByName
