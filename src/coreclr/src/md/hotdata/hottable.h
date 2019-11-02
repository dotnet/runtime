// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: HotTable.h
//

//
// Class code:MetaData::HotTable stores hot table records cache, a cache of often-accessed
// table records stored only in NGEN images.
// The cache is created using IBC logging.
//
// ======================================================================================

#pragma once

#include "external.h"

#include "hotdataformat.h"

namespace MetaData
{

// --------------------------------------------------------------------------------------
//
// This class stores hot table records cache, a cache of often-accessed table records stored only in NGEN
// images.
// The cache is created using IBC logging.
//
class HotTable
{
public:
    __checkReturn
    static HRESULT GetData(
                        UINT32 nRowIndex,
        __deref_out_opt BYTE **ppRecord,
                        UINT32 cbRecordSize,
                        struct HotTableHeader *pHotTableHeader);

    inline static struct HotTableHeader *GetTableHeader(
        struct HotTablesDirectory *pHotTablesDirectory,
        UINT32                     nTableIndex)
    {
        _ASSERTE(pHotTablesDirectory != NULL);

        INT32 nTableOffset = pHotTablesDirectory->m_rgTableHeader_SignedOffset[nTableIndex];
        _ASSERTE(nTableOffset != 0);

        BYTE *pHotTableHeaderData = ((BYTE *)pHotTablesDirectory) + nTableOffset;
        return (struct HotTableHeader *)pHotTableHeaderData;
    }

    static void CheckTables(struct HotTablesDirectory *pHotTablesDirectory);

};  // class HotTable

};  // namespace MetaData
