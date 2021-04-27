// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: HotTable.cpp
//

//
// Class code:MetaData::HotTable stores hot table records cache, a cache of often-accessed
// table records stored only in NGEN images.
// The cache is created using IBC logging.
//
// ======================================================================================

#include "external.h"

#include "hottable.h"
#include "hotdataformat.h"

#include <metamodelpub.h>

namespace MetaData
{

// --------------------------------------------------------------------------------------
//
// Returns S_OK if index (nIndex) is present in the hot table record cache and returns its value from
// cache (*ppRecord).
// Returns S_FALSE if offset is not in the hot table record cache, DOES NOT initialize *ppRecord in this
// case!
// Returns error code otherwise (and sets *pRecord to NULL).
//

//static
__checkReturn
HRESULT
HotTable::GetData(
                    UINT32 nRowIndex,
    __deref_out_opt BYTE **ppRecord,
                    UINT32 cbRecordSize,
                    struct HotTableHeader *pHotTableHeader)
{
    BYTE *pHotTableHeaderData = (BYTE *)pHotTableHeader;

    if (pHotTableHeader->m_nFirstLevelTable_PositiveOffset != 0)
    {   // Has first level table
        // fetch the first level table
        WORD *pFirstLevelTable = (WORD *)(pHotTableHeaderData + pHotTableHeader->m_nFirstLevelTable_PositiveOffset);

        // find the high order bits of the rid
        BYTE bRid = (BYTE)(nRowIndex >> pHotTableHeader->m_shiftCount);

        // use the low order bits of the rid to index into
        // the first level table.
        UINT32 nMask = (1 << pHotTableHeader->m_shiftCount) - 1;
        int i = pFirstLevelTable[nRowIndex & nMask];
        int end = pFirstLevelTable[(nRowIndex & nMask) + 1];

        // the generation logic should make sure either all tables are present
        // or all absent.
        _ASSERTE(pHotTableHeader->m_nSecondLevelTable_PositiveOffset != 0);
        _ASSERTE(pHotTableHeader->m_offsIndexMappingTable != 0);

        // fetch second level and index mapping tables
        BYTE *pSecondLevelTable = pHotTableHeaderData + pHotTableHeader->m_nSecondLevelTable_PositiveOffset;
        WORD *pIndexMappingTable = (WORD *)(pHotTableHeaderData + pHotTableHeader->m_offsIndexMappingTable);

        // look for the high order bits of the rid in the second level table.
        // search is linear, but should be short on average.
        for ( ; i < end; i++)
        {
            if (pSecondLevelTable[i] == bRid)
            {
                // we have found the hot rid we are looking for

                // now consult the index mapping table to find where the hot data is stored
                int index = pIndexMappingTable[i];

                *ppRecord = pHotTableHeaderData + pHotTableHeader->m_offsHotData + (index * cbRecordSize);
                return S_OK;
            }
        }
        // Not found in hot data
        return S_FALSE;
    }
    // no first level table - this implies the whole table is replicated
    // in the hot section. simply multiply and fetch the right record.
    // hot indices are 0-based, rids are 1-base, so need to subtract 1 from rid.

    *ppRecord = pHotTableHeaderData + pHotTableHeader->m_offsHotData + ((nRowIndex - 1) * cbRecordSize);
    return S_OK;
} // HotTable::GetData

// static
void
HotTable::CheckTables(struct HotTablesDirectory *pHotTablesDirectory)
{
#ifdef _DEBUG
    _ASSERTE(pHotTablesDirectory->m_nMagic == HotTablesDirectory::const_nMagic);

    for (UINT32 nTableIndex = 0; nTableIndex < TBL_COUNT; nTableIndex++)
    {
        if (pHotTablesDirectory->m_rgTableHeader_SignedOffset[nTableIndex] != 0)
        {
            struct HotTableHeader *pHotTableHeader = GetTableHeader(pHotTablesDirectory, nTableIndex);

            _ASSERTE((pHotTableHeader->m_cTableRecordCount > 0) && (pHotTableHeader->m_cTableRecordCount <= USHRT_MAX));
            if (pHotTableHeader->m_nFirstLevelTable_PositiveOffset == 0)
            {
                _ASSERTE(pHotTableHeader->m_nSecondLevelTable_PositiveOffset == 0);
                _ASSERTE(pHotTableHeader->m_offsIndexMappingTable == 0);
                _ASSERTE(pHotTableHeader->m_offsHotData == Align4(sizeof(struct HotTableHeader)));
            }
            else
            {
                UINT32 nFirstLevelTableOffset = sizeof(struct HotTableHeader);
                _ASSERTE(pHotTableHeader->m_nFirstLevelTable_PositiveOffset == nFirstLevelTableOffset);
                UINT32 cbFirstLevelTableSize = sizeof(WORD) * ((1 << pHotTableHeader->m_shiftCount) + 1);

                _ASSERTE(pHotTableHeader->m_nSecondLevelTable_PositiveOffset != 0);
                UINT32 nSecondLevelTableOffset = nFirstLevelTableOffset + cbFirstLevelTableSize;
                _ASSERTE(pHotTableHeader->m_nSecondLevelTable_PositiveOffset == nSecondLevelTableOffset);
                UINT32 cbSecondLevelTableSize = sizeof(BYTE) * pHotTableHeader->m_cTableRecordCount;

                _ASSERTE(pHotTableHeader->m_offsIndexMappingTable != 0);
                UINT32 nIndexMappingTableOffset = nSecondLevelTableOffset + cbSecondLevelTableSize;
                _ASSERTE(pHotTableHeader->m_offsIndexMappingTable == nIndexMappingTableOffset);
                UINT32 cbIndexMappingTableSize = sizeof(WORD) * pHotTableHeader->m_cTableRecordCount;

                UINT32 nHotDataOffset = nIndexMappingTableOffset + cbIndexMappingTableSize;
                _ASSERTE(pHotTableHeader->m_offsHotData == Align4(nHotDataOffset));
            }
        }
    }
#endif //_DEBUG
} // HotTable::CheckTables

};  // namespace MetaData
