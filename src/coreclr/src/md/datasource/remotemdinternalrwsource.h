// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// RemoteMDInternalRWSource.h
//

//
//*****************************************************************************

#ifndef _REMOTE_MDINTERNALRW_SOURCE_
#define _REMOTE_MDINTERNALRW_SOURCE_

#include "targettypes.h"

class RemoteMDInternalRWSource : IMDCustomDataSource
{
public:
    RemoteMDInternalRWSource();
    virtual ~RemoteMDInternalRWSource();

    //*****************************************************************************
    // IUnknown methods
    //*****************************************************************************
    STDMETHODIMP    QueryInterface(REFIID riid, void** ppv);
    STDMETHODIMP_(ULONG) AddRef(void);
    STDMETHODIMP_(ULONG) Release(void);

    //*****************************************************************************
    // IMDCustomDataSource methods
    //*****************************************************************************
    STDMETHODIMP GetSchema(CMiniMdSchema* pSchema);
    STDMETHODIMP GetTableDef(ULONG32 tableIndex, CMiniTableDef* pTableDef);
    STDMETHODIMP GetBlobHeap(MetaData::DataBlob* pBlobHeapData);
    STDMETHODIMP GetGuidHeap(MetaData::DataBlob* pGuidHeapData);
    STDMETHODIMP GetStringHeap(MetaData::DataBlob* pStringHeapData);
    STDMETHODIMP GetUserStringHeap(MetaData::DataBlob* pUserStringHeapData);
    STDMETHODIMP GetTableRecords(ULONG32 tableIndex, MetaData::DataBlob* pTableRecordData);
    STDMETHODIMP GetTableSortable(ULONG32 tableIndex, BOOL* pSortable);
    STDMETHODIMP GetStorageSignature(MetaData::DataBlob* pStorageSignature);

    //*****************************************************************************
    // public non-COM methods
    //*****************************************************************************
    HRESULT InitFromTarget(TADDR remoteMDInternalRWAddress, ICorDebugDataTarget* pDataTarget, DWORD defines, DWORD dataStructureVersion);

private:
    Target_MDInternalRW m_targetData;
    CMiniMdSchema m_Schema;
    CMiniTableDef m_TableDefs[TBL_COUNT];
    MetaData::DataBlob m_StringHeap;
    MetaData::DataBlob m_UserStringHeap;
    MetaData::DataBlob m_BlobHeap;
    MetaData::DataBlob m_GuidHeap;
    MetaData::DataBlob m_TableRecords[TBL_COUNT];
    BOOL m_bSortable[TBL_COUNT];
    MetaData::DataBlob m_Sig;

    NewArrayHolder<BYTE> m_StringHeapStorage;
    NewArrayHolder<BYTE> m_UserStringHeapStorage;
    NewArrayHolder<BYTE> m_BlobHeapStorage;
    NewArrayHolder<BYTE> m_GuidHeapStorage;
    NewArrayHolder<BYTE> m_TableRecordsStorage[TBL_COUNT];
    NewArrayHolder<BYTE> m_SigStorage;

    volatile LONG m_cRef;
};


#endif
