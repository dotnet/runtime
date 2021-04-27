// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// MetaModelENC.cpp
//

//
// Implementation for applying ENC deltas to a MiniMd.
//
//*****************************************************************************
#include "stdafx.h"
#include <limits.h>
#include <posterror.h>
#include <metamodelrw.h>
#include <stgio.h>
#include <stgtiggerstorage.h>
#include "mdlog.h"
#include "rwutil.h"

ULONG CMiniMdRW::m_SuppressedDeltaColumns[TBL_COUNT] = {0};

//*****************************************************************************
// Copy the data from one MiniMd to another.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::ApplyRecordDelta(
    CMiniMdRW   &mdDelta,               // The delta MetaData.
    ULONG       ixTbl,                  // The table with the data.
    void        *pDelta,                // The delta MetaData record.
    void        *pRecord)               // The record to update.
{
    HRESULT hr = S_OK;
    ULONG   mask = m_SuppressedDeltaColumns[ixTbl];

    for (ULONG ixCol = 0; ixCol<m_TableDefs[ixTbl].m_cCols; ++ixCol, mask >>= 1)
    {   // Skip certain pointer columns.
        if (mask & 0x01)
            continue;

        ULONG val = mdDelta.GetCol(ixTbl, ixCol, pDelta);
        IfFailRet(PutCol(ixTbl, ixCol, pRecord, val));
    }
    return hr;
} // CMiniMdRW::ApplyRecordDelta

//*****************************************************************************
// Apply a delta record to a table, generically.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::ApplyTableDelta(
    CMiniMdRW &mdDelta,     // Interface to MD with the ENC delta.
    ULONG      ixTbl,       // Table index to update.
    RID        iRid,        // RID of the changed item.
    int        fc)          // Function code of update.
{
    HRESULT hr = S_OK;
    void   *pRec;           // Record in existing MetaData.
    void   *pDeltaRec;      // Record if Delta MetaData.
    RID     newRid;         // Rid of new record.

    // Get the delta record.
    IfFailGo(mdDelta.GetDeltaRecord(ixTbl, iRid, &pDeltaRec));
    // Get the record from the base metadata.
    if (iRid > m_Schema.m_cRecs[ixTbl])
    {   // Added record.  Each addition is the next one.
        _ASSERTE(iRid == m_Schema.m_cRecs[ixTbl] + 1);
        switch (ixTbl)
        {
        case TBL_TypeDef:
            IfFailGo(AddTypeDefRecord(reinterpret_cast<TypeDefRec **>(&pRec), &newRid));
            break;
        case TBL_Method:
            IfFailGo(AddMethodRecord(reinterpret_cast<MethodRec **>(&pRec), &newRid));
            break;
        case TBL_EventMap:
            IfFailGo(AddEventMapRecord(reinterpret_cast<EventMapRec **>(&pRec), &newRid));
            break;
        case TBL_PropertyMap:
            IfFailGo(AddPropertyMapRecord(reinterpret_cast<PropertyMapRec **>(&pRec), &newRid));
            break;
        default:
            IfFailGo(AddRecord(ixTbl, &pRec, &newRid));
            break;
        }
        IfNullGo(pRec);
        _ASSERTE(iRid == newRid);
    }
    else
    {   // Updated record.
        IfFailGo(getRow(ixTbl, iRid, &pRec));
    }

    // Copy the record info.
    IfFailGo(ApplyRecordDelta(mdDelta, ixTbl, pDeltaRec, pRec));

ErrExit:
    return hr;
} // CMiniMdRW::ApplyTableDelta

//*****************************************************************************
// Get the record from a Delta MetaData that corresponds to the actual record.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::GetDeltaRecord(
    ULONG  ixTbl,       // Table.
    ULONG  iRid,        // Record in the table.
    void **ppRecord)
{
    HRESULT    hr;
    ULONG      iMap;    // RID in map table.
    ENCMapRec *pMap;    // Row in map table.

    *ppRecord = NULL;
    // If no remap, just return record directly.
    if ((m_Schema.m_cRecs[TBL_ENCMap] == 0) || (ixTbl == TBL_Module) || !IsMinimalDelta())
    {
        return getRow(ixTbl, iRid, ppRecord);
    }

    // Use the remap table to find the physical row containing this logical row.
    iMap = (*m_rENCRecs)[ixTbl];
    IfFailRet(GetENCMapRecord(iMap, &pMap));

    // Search for desired record.
    while ((TblFromRecId(pMap->GetToken()) == ixTbl) && (RidFromRecId(pMap->GetToken()) < iRid))
    {
        IfFailRet(GetENCMapRecord(++iMap, &pMap));
    }

    _ASSERTE((TblFromRecId(pMap->GetToken()) == ixTbl) && (RidFromRecId(pMap->GetToken()) == iRid));

    // Relative position within table's group in map is physical rid.
    iRid = iMap - (*m_rENCRecs)[ixTbl] + 1;

    return getRow(ixTbl, iRid, ppRecord);
} // CMiniMdRW::GetDeltaRecord

//*****************************************************************************
// Given a MetaData with ENC changes, apply those changes to this MetaData.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::ApplyHeapDeltas(
    CMiniMdRW &mdDelta)     // Interface to MD with the ENC delta.
{
    if (mdDelta.IsMinimalDelta())
    {
        return ApplyHeapDeltasWithMinimalDelta(mdDelta);
    }
    else
    {
        return ApplyHeapDeltasWithFullDelta(mdDelta);
    }
}// CMiniMdRW::ApplyHeapDeltas

__checkReturn
HRESULT
CMiniMdRW::ApplyHeapDeltasWithMinimalDelta(
    CMiniMdRW &mdDelta)     // Interface to MD with the ENC delta.
{
    HRESULT hr = S_OK;

    // Extend the heaps with EnC minimal delta
    IfFailGo(m_StringHeap.AddStringHeap(
        &(mdDelta.m_StringHeap),
        0));                    // Start offset in the mdDelta
    IfFailGo(m_BlobHeap.AddBlobHeap(
        &(mdDelta.m_BlobHeap),
        0));                    // Start offset in the mdDelta
    IfFailGo(m_UserStringHeap.AddBlobHeap(
        &(mdDelta.m_UserStringHeap),
        0));                    // Start offset in the mdDelta
    // We never do a minimal delta with the guid heap
    IfFailGo(m_GuidHeap.AddGuidHeap(
        &(mdDelta.m_GuidHeap),
        m_GuidHeap.GetSize())); // Starting offset in the full delta guid heap

ErrExit:
    return hr;
} // CMiniMdRW::ApplyHeapDeltasWithMinimalDelta

__checkReturn
HRESULT
CMiniMdRW::ApplyHeapDeltasWithFullDelta(
    CMiniMdRW &mdDelta)     // Interface to MD with the ENC delta.
{
    HRESULT hr = S_OK;

    // Extend the heaps with EnC full delta
    IfFailRet(m_StringHeap.AddStringHeap(
        &(mdDelta.m_StringHeap),
        m_StringHeap.GetUnalignedSize()));      // Starting offset in the full delta string heap
    IfFailRet(m_BlobHeap.AddBlobHeap(
        &(mdDelta.m_BlobHeap),
        m_BlobHeap.GetUnalignedSize()));        // Starting offset in the full delta blob heap
    IfFailRet(m_UserStringHeap.AddBlobHeap(
        &(mdDelta.m_UserStringHeap),
        m_UserStringHeap.GetUnalignedSize()));  // Starting offset in the full delta user string heap
    IfFailRet(m_GuidHeap.AddGuidHeap(
        &(mdDelta.m_GuidHeap),
        m_GuidHeap.GetSize()));                 // Starting offset in the full delta guid heap

    return hr;
} // CMiniMdRW::ApplyHeapDeltasWithFullDelta

//*****************************************************************************
// Driver for the delta process.
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::ApplyDelta(
    CMiniMdRW &mdDelta) // Interface to MD with the ENC delta.
{
    HRESULT hr = S_OK;
    ULONG   iENC;       // Loop control.
    RID     iRid;       // RID of some record.
    RID     iNew;       // RID of a new record.
    int     i;          // Loop control.
    ULONG   ixTbl;      // A table.

#ifdef _DEBUG
    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_MD_ApplyDeltaBreak))
    {
        _ASSERTE(!"CMiniMDRW::ApplyDelta()");
    }
#endif // _DEBUG

    // Init the suppressed column table.  We know this one isn't zero...
    if (m_SuppressedDeltaColumns[TBL_TypeDef] == 0)
    {
        m_SuppressedDeltaColumns[TBL_EventMap]      = (1 << EventMapRec::COL_EventList);
        m_SuppressedDeltaColumns[TBL_PropertyMap]   = (1 << PropertyMapRec::COL_PropertyList);
        m_SuppressedDeltaColumns[TBL_EventMap]      = (1 << EventMapRec::COL_EventList);
        m_SuppressedDeltaColumns[TBL_Method]        = (1 << MethodRec::COL_ParamList);
        m_SuppressedDeltaColumns[TBL_TypeDef]       = (1 << TypeDefRec::COL_FieldList)|(1<<TypeDefRec::COL_MethodList);
    }

    // Verify the version of the MD.
    if (m_Schema.m_major != mdDelta.m_Schema.m_major ||
        m_Schema.m_minor != mdDelta.m_Schema.m_minor)
    {
        _ASSERTE(!"Version of Delta MetaData is a incompatible with current MetaData.");
        //<TODO>@FUTURE: unique error in the future since we are not shipping ENC.</TODO>
        return E_INVALIDARG;
    }

    // verify MVIDs.
    ModuleRec *pModDelta;
    ModuleRec *pModBase;
    IfFailGo(mdDelta.GetModuleRecord(1, &pModDelta));
    IfFailGo(GetModuleRecord(1, &pModBase));
    GUID GuidDelta;
    GUID GuidBase;
    IfFailGo(mdDelta.getMvidOfModule(pModDelta, &GuidDelta));
    IfFailGo(getMvidOfModule(pModBase, &GuidBase));
    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_MD_DeltaCheck) && (GuidDelta != GuidBase))
    {
        _ASSERTE(!"Delta MetaData has different base than current MetaData.");
        return E_INVALIDARG;
    }


    // Let the other md prepare for sparse records.
    IfFailGo(mdDelta.StartENCMap());

    // Fix the heaps.
    IfFailGo(ApplyHeapDeltas(mdDelta));

    // Truncate some tables in preparation to copy in new ENCLog data.
    for (i = 0; (ixTbl = m_TruncatedEncTables[i]) != (ULONG)-1; ++i)
    {
        m_Tables[ixTbl].Delete();
        IfFailGo(m_Tables[ixTbl].InitializeEmpty_WithRecordCount(
            m_TableDefs[ixTbl].m_cbRec,
            mdDelta.m_Schema.m_cRecs[ixTbl]
            COMMA_INDEBUG_MD(TRUE)));       // fIsReadWrite
        INDEBUG_MD(m_Tables[ixTbl].Debug_SetTableInfo(NULL, ixTbl));
        m_Schema.m_cRecs[ixTbl] = 0;
    }

    // For each record in the ENC log...
    for (iENC = 1; iENC <= mdDelta.m_Schema.m_cRecs[TBL_ENCLog]; ++iENC)
    {
        // Get the record, and the updated token.
        ENCLogRec *pENC;
        IfFailGo(mdDelta.GetENCLogRecord(iENC, &pENC));
        ENCLogRec *pENC2;
        IfFailGo(AddENCLogRecord(&pENC2, &iNew));
        IfNullGo(pENC2);
        ENCLogRec *pENC3;
        _ASSERTE(iNew == iENC);
        ULONG func = pENC->GetFuncCode();
        pENC2->SetFuncCode(pENC->GetFuncCode());
        pENC2->SetToken(pENC->GetToken());

        // What kind of record is this?
        if (IsRecId(pENC->GetToken()))
        {   // Non-token table
            iRid = RidFromRecId(pENC->GetToken());
            ixTbl = TblFromRecId(pENC->GetToken());
        }
        else
        {   // Token table.
            iRid = RidFromToken(pENC->GetToken());
            ixTbl = GetTableForToken(pENC->GetToken());
        }

        RID rid_Ignore;
        // Switch based on the function code.
        switch (func)
        {
        case eDeltaMethodCreate:
            // Next ENC record will define the new Method.
            MethodRec *pMethodRecord;
            IfFailGo(AddMethodRecord(&pMethodRecord, &rid_Ignore));
            IfFailGo(AddMethodToTypeDef(iRid, m_Schema.m_cRecs[TBL_Method]));
            break;

        case eDeltaParamCreate:
            // Next ENC record will define the new Param.  This record is
            //  tricky because params will be re-ordered based on their sequence,
            //  but the sequence isn't set until the NEXT record is applied.
            //  So, for ParamCreate only, apply the param record delta before
            //  adding the parent-child linkage.
            ParamRec *pParamRecord;
            IfFailGo(AddParamRecord(&pParamRecord, &rid_Ignore));

            // Should have recorded a Param delta after the Param add.
            _ASSERTE(iENC<mdDelta.m_Schema.m_cRecs[TBL_ENCLog]);
            IfFailGo(mdDelta.GetENCLogRecord(iENC+1, &pENC3));
            _ASSERTE(pENC3->GetFuncCode() == 0);
            _ASSERTE(GetTableForToken(pENC3->GetToken()) == TBL_Param);
            IfFailGo(ApplyTableDelta(mdDelta, TBL_Param, RidFromToken(pENC3->GetToken()), eDeltaFuncDefault));

            // Now that Param record is OK, set up linkage.
            IfFailGo(AddParamToMethod(iRid, m_Schema.m_cRecs[TBL_Param]));
            break;

        case eDeltaFieldCreate:
            // Next ENC record will define the new Field.
            FieldRec *pFieldRecord;
            IfFailGo(AddFieldRecord(&pFieldRecord, &rid_Ignore));
            IfFailGo(AddFieldToTypeDef(iRid, m_Schema.m_cRecs[TBL_Field]));
            break;

        case eDeltaPropertyCreate:
            // Next ENC record will define the new Property.
            PropertyRec *pPropertyRecord;
            IfFailGo(AddPropertyRecord(&pPropertyRecord, &rid_Ignore));
            IfFailGo(AddPropertyToPropertyMap(iRid, m_Schema.m_cRecs[TBL_Property]));
            break;

        case eDeltaEventCreate:
            // Next ENC record will define the new Event.
            EventRec *pEventRecord;
            IfFailGo(AddEventRecord(&pEventRecord, &rid_Ignore));
            IfFailGo(AddEventToEventMap(iRid, m_Schema.m_cRecs[TBL_Event]));
            break;

        case eDeltaFuncDefault:
            IfFailGo(ApplyTableDelta(mdDelta, ixTbl, iRid, func));
            break;

        default:
            _ASSERTE(!"Unexpected function in ApplyDelta");
            IfFailGo(E_UNEXPECTED);
            break;
        }
    }
    m_Schema.m_cRecs[TBL_ENCLog] = mdDelta.m_Schema.m_cRecs[TBL_ENCLog];

ErrExit:
    // Store the result for returning (IfFailRet will modify hr)
    HRESULT hrReturn = hr;
    IfFailRet(mdDelta.EndENCMap());


    return hrReturn;
} // CMiniMdRW::ApplyDelta

//*****************************************************************************
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::StartENCMap()        // S_OK or error.
{
    HRESULT hr = S_OK;
    ULONG   iENC;               // Loop control.
    ULONG   ixTbl;              // A table.
    int     ixTblPrev = -1;     // Table previously seen.

    _ASSERTE(m_rENCRecs == 0);

    if (m_Schema.m_cRecs[TBL_ENCMap] == 0)
        return S_OK;

    // Build an array of pointers into the ENCMap table for fast access to the ENCMap
    //  for each table.
    m_rENCRecs = new (nothrow) ULONGARRAY;
    IfNullGo(m_rENCRecs);
    if (!m_rENCRecs->AllocateBlock(TBL_COUNT))
        IfFailGo(E_OUTOFMEMORY);
    for (iENC = 1; iENC <= m_Schema.m_cRecs[TBL_ENCMap]; ++iENC)
    {
        ENCMapRec *pMap;
        IfFailGo(GetENCMapRecord(iENC, &pMap));
        ixTbl = TblFromRecId(pMap->GetToken());
        _ASSERTE((int)ixTbl >= ixTblPrev);
        _ASSERTE(ixTbl < TBL_COUNT);
        _ASSERTE(ixTbl != TBL_ENCMap);
        _ASSERTE(ixTbl != TBL_ENCLog);
        if ((int)ixTbl == ixTblPrev)
            continue;
        // Catch up on any skipped tables.
        while (ixTblPrev < (int)ixTbl)
        {
            (*m_rENCRecs)[++ixTblPrev] = iENC;
        }
    }
    while (ixTblPrev < TBL_COUNT-1)
    {
        (*m_rENCRecs)[++ixTblPrev] = iENC;
    }

ErrExit:
    return hr;
} // CMiniMdRW::StartENCMap

//*****************************************************************************
//*****************************************************************************
__checkReturn
HRESULT
CMiniMdRW::EndENCMap()
{
    if (m_rENCRecs != NULL)
    {
        delete m_rENCRecs;
        m_rENCRecs = NULL;
    }

    return S_OK;
} // CMiniMdRW::EndENCMap
