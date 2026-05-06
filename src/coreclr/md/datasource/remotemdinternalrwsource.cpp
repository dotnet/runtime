// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// RemoteMDInternalRWSource.cpp
//

//
//*****************************************************************************

#include "stdafx.h"
#include "remotemdinternalrwsource.h"

RemoteMDInternalRWSource::RemoteMDInternalRWSource() :
m_cRef(0)
{
    memset(&m_TableDefs, 0, sizeof(CMiniTableDef)*TBL_COUNT);
    memset(&m_bSortable, 0, sizeof(BOOL)*TBL_COUNT);
}

RemoteMDInternalRWSource::~RemoteMDInternalRWSource()
{
    for (int i = 0; i < TBL_COUNT; i++)
    {
        delete[] m_TableDefs[i].m_pColDefs;
    }
}

ULONG RemoteMDInternalRWSource::AddRef()
{
    return InterlockedIncrement(&m_cRef);
}

ULONG RemoteMDInternalRWSource::Release()
{
    ULONG cRef = InterlockedDecrement(&m_cRef);
    if (cRef == 0)
        delete this;
    return cRef;
}

HRESULT RemoteMDInternalRWSource::QueryInterface(REFIID  riid, void ** ppUnk)
{
    *ppUnk = 0;
    if (riid == IID_IUnknown)
    {
        *ppUnk = static_cast<IUnknown*>(this);
    }
    else if (riid == IID_IMDCustomDataSource)
    {
        *ppUnk = static_cast<IMDCustomDataSource*>(this);
    }
    else
    {
        return E_NOINTERFACE;
    }
    AddRef();
    return S_OK;
}

HRESULT _MarshalDataFromTargetStgPool(DataTargetReader & reader, const Target_StgPool & pool, MetaData::DataBlob* pBlob)
{
    HRESULT hr = S_OK;
    ULONG32 dataSize = 0;
    ULONG32 segmentCount = 0;
    Target_StgPoolSeg curSeg = (Target_StgPoolSeg)pool;

    // The storage pool grows exponentially, and should reach 2GB in at most 64 segments. Allowing for 1000
    // adds a sizable risk mitigation factor in case my analysis was inaccurate or the algorithm changes in the future
    // without corresponding changes here.
    CORDB_ADDRESS segmentData[1000];
    ULONG32 segmentSize[1000];
    for (; segmentCount < 1000; segmentCount++)
    {
        // sanity check that each segment and the sum of all segments is less than 100M bytes
        if (curSeg.m_cbSegNext > 100000000)
            return CLDB_E_FILE_CORRUPT;
        dataSize += curSeg.m_cbSegNext;
        if (dataSize > 100000000)
            return CLDB_E_FILE_CORRUPT;
        segmentData[segmentCount] = curSeg.m_pSegData;
        segmentSize[segmentCount] = curSeg.m_cbSegNext;
        if (curSeg.m_pNextSeg == 0)
            break;

        DataTargetReader segReader = reader.CreateReaderAt(curSeg.m_pNextSeg);
        IfFailRet(segReader.Read(&curSeg));
    }

    //we exited the loop with a break, count should be one more than the last index
    segmentCount++;

    // sanity check, no more than 1000 segments allowed
    if (segmentCount > 1000)
        return CLDB_E_FILE_CORRUPT;

    // things looked reasonable enough, marshal over the data
    NewArrayHolder<BYTE> pData = new (nothrow) BYTE[dataSize];
    if (pData == NULL)
        return E_OUTOFMEMORY;
    BYTE* pCursor = pData;
    for (ULONG32 i = 0; i < segmentCount; i++)
    {
        DataTargetReader segDataReader = reader.CreateReaderAt(segmentData[i]);
        hr = segDataReader.ReadBytes(pCursor, segmentSize[i]);
        if (FAILED(hr))
        {
            return hr;
        }
        else
        {
            pCursor += segmentSize[i];
        }
    }
    pBlob->Init(pData, dataSize);
    pData.SuppressRelease(); // our caller owns the buffer now
    return S_OK;

}

HRESULT RemoteMDInternalRWSource::InitFromTarget(TADDR remoteMDInternalRWAddr, ICorDebugDataTarget* pDataTarget, DWORD defines, DWORD dataStructureVersion)
{
    HRESULT hr = S_OK;
    DataTargetReader reader(remoteMDInternalRWAddr, pDataTarget, defines, dataStructureVersion);
    IfFailRet(reader.Read(&m_targetData));

    Target_CMiniMdSchema targetSchema = m_targetData.m_pStgdb.m_MiniMd.m_Schema;
    m_Schema.m_ulReserved = targetSchema.m_ulReserved;
    m_Schema.m_major = targetSchema.m_major;
    m_Schema.m_minor = targetSchema.m_minor;
    m_Schema.m_heaps = targetSchema.m_heaps;
    m_Schema.m_rid = targetSchema.m_rid;
    m_Schema.m_maskvalid = targetSchema.m_maskvalid;
    m_Schema.m_sorted = targetSchema.m_sorted;
    memcpy(m_Schema.m_cRecs, targetSchema.m_cRecs, sizeof(ULONG32)*TBL_COUNT);
    m_Schema.m_ulExtra = targetSchema.m_ulExtra;

    for (int i = 0; i < TBL_COUNT; i++)
    {
        Target_CMiniTableDef* pTargetTableDef = &(m_targetData.m_pStgdb.m_MiniMd.m_TableDefs[i]);
        m_TableDefs[i].m_cCols = pTargetTableDef->m_cCols;
        m_TableDefs[i].m_iKey = pTargetTableDef->m_iKey;
        m_TableDefs[i].m_cbRec = pTargetTableDef->m_cbRec;
        m_TableDefs[i].m_pColDefs = new (nothrow) CMiniColDef[m_TableDefs[i].m_cCols];
        if (m_TableDefs[i].m_pColDefs == NULL)
            return E_OUTOFMEMORY;
        for (int j = 0; j < m_TableDefs[i].m_cCols; j++)
        {
            m_TableDefs[i].m_pColDefs[j].m_Type = pTargetTableDef->m_pColDefs[j].m_Type;
            m_TableDefs[i].m_pColDefs[j].m_oColumn = pTargetTableDef->m_pColDefs[j].m_oColumn;
            m_TableDefs[i].m_pColDefs[j].m_cbColumn = pTargetTableDef->m_pColDefs[j].m_cbColumn;
        }
    }

    IfFailRet(_MarshalDataFromTargetStgPool(reader, (Target_StgPool)m_targetData.m_pStgdb.m_MiniMd.m_StringHeap, &m_StringHeap));
    m_StringHeapStorage = m_StringHeap.GetDataPointer();
    IfFailRet(_MarshalDataFromTargetStgPool(reader, (Target_StgPool)m_targetData.m_pStgdb.m_MiniMd.m_BlobHeap, &m_BlobHeap));
    m_BlobHeapStorage = m_BlobHeap.GetDataPointer();
    IfFailRet(_MarshalDataFromTargetStgPool(reader, (Target_StgPool)m_targetData.m_pStgdb.m_MiniMd.m_UserStringHeap, &m_UserStringHeap));
    m_UserStringHeapStorage = m_UserStringHeap.GetDataPointer();
    IfFailRet(_MarshalDataFromTargetStgPool(reader, (Target_StgPool)m_targetData.m_pStgdb.m_MiniMd.m_GuidHeap, &m_GuidHeap));
    m_GuidHeapStorage = m_GuidHeap.GetDataPointer();

    for (int i = 0; i < TBL_COUNT; i++)
    {
        IfFailRet(_MarshalDataFromTargetStgPool(reader, (Target_StgPool)m_targetData.m_pStgdb.m_MiniMd.m_Tables[i], &m_TableRecords[i]));
        m_TableRecordsStorage[i] = m_TableRecords[i].GetDataPointer();
        m_bSortable[i] = m_targetData.m_pStgdb.m_MiniMd.m_bSortable[i];
    }

    if (m_targetData.m_pStgdb.m_pvMd != 0)
    {
        STORAGESIGNATURE sig = { 0 };
        DataTargetReader storageSigReader = reader.CreateReaderAt(m_targetData.m_pStgdb.m_pvMd);
        storageSigReader.ReadBytes((BYTE*)&sig, sizeof(sig));
        if (sig.GetVersionStringLength() > 1000)
            return CLDB_E_FILE_CORRUPT;
        ULONG32 totalSigSize = offsetof(STORAGESIGNATURE, pVersion) + sig.GetVersionStringLength();
        m_SigStorage = new (nothrow)BYTE[totalSigSize];
        if (m_SigStorage == NULL)
            return E_OUTOFMEMORY;
        memcpy_s(m_SigStorage, totalSigSize, &sig, sizeof(sig));
        storageSigReader.ReadBytes(m_SigStorage + sizeof(sig), totalSigSize - sizeof(sig));
        m_Sig.Init(m_SigStorage, totalSigSize);
    }

    return S_OK;
}

STDMETHODIMP RemoteMDInternalRWSource::GetSchema(CMiniMdSchema* pSchema)
{
    *pSchema = m_Schema;
    return S_OK;
}
STDMETHODIMP RemoteMDInternalRWSource::GetTableDef(ULONG32 tableIndex, CMiniTableDef* pTableDef)
{
    *pTableDef = m_TableDefs[tableIndex];
    return S_OK;
}

STDMETHODIMP RemoteMDInternalRWSource::GetBlobHeap(MetaData::DataBlob* pBlobHeapData)
{
    *pBlobHeapData = m_BlobHeap;
    return S_OK;
}

STDMETHODIMP RemoteMDInternalRWSource::GetGuidHeap(MetaData::DataBlob* pGuidHeapData)
{
    *pGuidHeapData = m_GuidHeap;
    return S_OK;
}

STDMETHODIMP RemoteMDInternalRWSource::GetStringHeap(MetaData::DataBlob* pStringHeapData)
{
    *pStringHeapData = m_StringHeap;
    return S_OK;
}

STDMETHODIMP RemoteMDInternalRWSource::GetUserStringHeap(MetaData::DataBlob* pUserStringHeapData)
{
    *pUserStringHeapData = m_UserStringHeap;
    return S_OK;
}

STDMETHODIMP RemoteMDInternalRWSource::GetTableRecords(ULONG32 tableIndex, MetaData::DataBlob* pTableRecordData)
{
    *pTableRecordData = m_TableRecords[tableIndex];
    return S_OK;
}

STDMETHODIMP RemoteMDInternalRWSource::GetTableSortable(ULONG32 tableIndex, BOOL* pSortable)
{
    *pSortable = TRUE;
    return S_OK;
}

STDMETHODIMP RemoteMDInternalRWSource::GetStorageSignature(MetaData::DataBlob* pStorageSignature)
{
    *pStorageSignature = m_Sig;
    return S_OK;
}
