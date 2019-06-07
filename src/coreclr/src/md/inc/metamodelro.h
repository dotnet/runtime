// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// MetaModelRO.h -- header file for Read-Only compressed COM+ metadata.
// 

//
// Used by the EE.
//
//*****************************************************************************
#ifndef _METAMODELRO_H_
#define _METAMODELRO_H_

#if _MSC_VER >= 1100
 # pragma once
#endif

#include "metamodel.h"

#include "../heaps/export.h"
#include "../tables/export.h"

//*****************************************************************************
// A read-only MiniMd.  This is the fastest and smallest possible MiniMd,
//  and as such, is the preferred EE metadata provider.
//*****************************************************************************

template <class MiniMd> class CLiteWeightStgdb;
class CMiniMdRW;
class MDInternalRO;
class CMiniMd final: public CMiniMdTemplate<CMiniMd>
{
public:
    friend class CLiteWeightStgdb<CMiniMd>;
    friend class CMiniMdTemplate<CMiniMd>;
    friend class CMiniMdRW;
    friend class MDInternalRO;

    __checkReturn 
    HRESULT InitOnMem(void *pBuf, ULONG ulBufLen);
    __checkReturn 
    HRESULT PostInit(int iLevel);  // higher number : more checking
    
    // Returns TRUE if token (tk) is valid.
    // For user strings, consideres 0 as valid token.
    BOOL _IsValidToken(
        mdToken tk)     // [IN] token to be checked
    {
        if (TypeFromToken(tk) == mdtString)
        {
            return m_UserStringHeap.IsValidIndex(RidFromToken(tk));
        }
        // Base type doesn't know about user string blob (yet)
        return _IsValidTokenBase(tk);
    } // CMiniMdRO::_IsValidToken
    
    __checkReturn 
    FORCEINLINE HRESULT GetUserString(ULONG nIndex, MetaData::DataBlob *pData)
    {
        MINIMD_POSSIBLE_INTERNAL_POINTER_EXPOSED();
        return m_UserStringHeap.GetBlob(nIndex, pData);
    }

#ifdef FEATURE_PREJIT
    void DisableHotDataUsage()
    {
        MetaData::HotHeap emptyHotHeap;
        // Initialize hot data again with empty heap to disable their usage
        m_StringHeap.InitializeHotData(emptyHotHeap);
        m_BlobHeap.InitializeHotData(emptyHotHeap);
        m_UserStringHeap.InitializeHotData(emptyHotHeap);
        m_GuidHeap.InitializeHotData(emptyHotHeap);
        // Disable usage of hot table data (throw it away)
        m_pHotTablesDirectory = NULL;
    }
#endif //FEATURE_PREJIT

protected:
    // Table info.
    MetaData::TableRO m_Tables[TBL_COUNT];
#ifdef FEATURE_PREJIT
    struct MetaData::HotTablesDirectory * m_pHotTablesDirectory;
#endif //FEATURE_PREJIT
    
    __checkReturn 
    HRESULT InitializeTables(MetaData::DataBlob tablesData);

    __checkReturn 
    virtual HRESULT vSearchTable(ULONG ixTbl, CMiniColDef sColumn, ULONG ulTarget, RID *pRid);
    __checkReturn 
    virtual HRESULT vSearchTableNotGreater(ULONG ixTbl, CMiniColDef sColumn, ULONG ulTarget, RID *pRid);
    
    // Heaps
    MetaData::StringHeapRO m_StringHeap;
    MetaData::BlobHeapRO   m_BlobHeap;
    MetaData::BlobHeapRO   m_UserStringHeap;
    MetaData::GuidHeapRO   m_GuidHeap;
    
protected:
    
    //*************************************************************************
    // Overridables -- must be provided in derived classes.
    __checkReturn 
    FORCEINLINE HRESULT Impl_GetString(UINT32 nIndex, __out LPCSTR *pszString)
    { return m_StringHeap.GetString(nIndex, pszString); }
    __checkReturn 
    HRESULT Impl_GetStringW(ULONG ix, __inout_ecount (cchBuffer) LPWSTR szOut, ULONG cchBuffer, ULONG *pcchBuffer);
    __checkReturn 
    FORCEINLINE HRESULT Impl_GetGuid(UINT32 nIndex, GUID *pTargetGuid)
    {
        HRESULT         hr;
        GUID UNALIGNED *pSourceGuid;
        IfFailRet(m_GuidHeap.GetGuid(
            nIndex, 
            &pSourceGuid));
        // Add void* casts so that the compiler can't make assumptions about alignment.
        CopyMemory((void *)pTargetGuid, (void *)pSourceGuid, sizeof(GUID));
        SwapGuid(pTargetGuid);
        return S_OK;
    }
    __checkReturn 
    FORCEINLINE HRESULT Impl_GetBlob(UINT32 nIndex, __out MetaData::DataBlob *pData)
    { return m_BlobHeap.GetBlob(nIndex, pData); }
    
    __checkReturn 
    FORCEINLINE HRESULT Impl_GetRow(
                        UINT32 nTableIndex, 
                        UINT32 nRowIndex, 
        __deref_out_opt BYTE **ppRecord)
    {
        _ASSERTE(nTableIndex < TBL_COUNT);
        return m_Tables[nTableIndex].GetRecord(
            nRowIndex, 
            ppRecord, 
            m_TableDefs[nTableIndex].m_cbRec, 
            m_Schema.m_cRecs[nTableIndex], 
#ifdef FEATURE_PREJIT
            m_pHotTablesDirectory, 
#endif //FEATURE_PREJIT
            nTableIndex);
    }
    
    // Count of rows in tbl2, pointed to by the column in tbl.
    __checkReturn 
    HRESULT Impl_GetEndRidForColumn(
        UINT32       nTableIndex, 
        RID          nRowIndex, 
        CMiniColDef &def,                   // Column containing the RID into other table.
        UINT32       nTargetTableIndex,     // The other table.
        RID         *pEndRid);
    
    __checkReturn 
    FORCEINLINE HRESULT Impl_SearchTable(ULONG ixTbl, CMiniColDef sColumn, ULONG ixCol, ULONG ulTarget, RID *pFoundRid)
    {
        return vSearchTable(ixTbl, sColumn, ulTarget, pFoundRid);
    }

    // given a rid to the Property table, find an entry in PropertyMap table that contains the back pointer
    // to its typedef parent
    __checkReturn 
    HRESULT FindPropertyMapParentOfProperty(RID rid, RID *pFoundRid)
    {
        return vSearchTableNotGreater(TBL_PropertyMap, _COLDEF(PropertyMap,PropertyList), rid, pFoundRid);
    }
    
    __checkReturn 
    HRESULT FindParentOfPropertyHelper(
        mdProperty  pr,
        mdTypeDef   *ptd)
    {
        HRESULT     hr = NOERROR;

        RID         ridPropertyMap;
        PropertyMapRec *pRec;

        IfFailRet(FindPropertyMapParentOfProperty(RidFromToken(pr), &ridPropertyMap));
        IfFailRet(GetPropertyMapRecord(ridPropertyMap, &pRec));
        *ptd = getParentOfPropertyMap( pRec );

        RidToToken(*ptd, mdtTypeDef);

        return hr;
    } // HRESULT CMiniMdRW::FindParentOfPropertyHelper()
    
    // given a rid to the Event table, find an entry in EventMap table that contains the back pointer
    // to its typedef parent
    __checkReturn 
    HRESULT FindEventMapParentOfEvent(RID rid, RID *pFoundRid)
    {
        return vSearchTableNotGreater(TBL_EventMap, _COLDEF(EventMap, EventList), rid, pFoundRid);
    }
    
    __checkReturn 
    HRESULT FindParentOfEventHelper(
        mdEvent  pr,
        mdTypeDef   *ptd)
    {
        HRESULT     hr = NOERROR;

        RID         ridEventMap;
        EventMapRec *pRec;

        IfFailRet(FindEventMapParentOfEvent(RidFromToken(pr), &ridEventMap));
        IfFailRet(GetEventMapRecord(ridEventMap, &pRec));
        *ptd = getParentOfEventMap( pRec );

        RidToToken(*ptd, mdtTypeDef);

        return hr;
    } // HRESULT CMiniMdRW::FindParentOfEventHelper()
    
    FORCEINLINE int Impl_IsRo() 
    { return 1; }
    //*************************************************************************

    __checkReturn 
    HRESULT CommonEnumCustomAttributeByName( // S_OK or error.
        mdToken     tkObj,                  // [IN] Object with Custom Attribute.
        LPCUTF8     szName,                 // [IN] Name of desired Custom Attribute.
        bool        fStopAtFirstFind,       // [IN] just find the first one
        HENUMInternal* phEnum);             // enumerator to fill up

    __checkReturn 
    HRESULT CommonGetCustomAttributeByNameEx( // S_OK or error.
        mdToken            tkObj,             // [IN] Object with Custom Attribute.
        LPCUTF8            szName,            // [IN] Name of desired Custom Attribute.
        mdCustomAttribute *ptkCA,             // [OUT] put custom attribute token here
        const void       **ppData,            // [OUT] Put pointer to data here.
        ULONG             *pcbData);          // [OUT] Put size of data here.

    public:
      virtual BOOL IsWritable()
      {
          return FALSE;
      }

};  // class CMiniMd

#endif // _METAMODELRO_H_
