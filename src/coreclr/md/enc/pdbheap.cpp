// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"
#include "pdbheap.h"

PdbHeap::PdbHeap() : m_data(NULL), m_size(0)
{
}

PdbHeap::~PdbHeap()
{
    if (m_data != NULL)
    {
        delete[] m_data;
        m_data = NULL;
    }
}

__checkReturn
HRESULT PdbHeap::SetData(PORT_PDB_STREAM* data)
{
    m_size = sizeof(data->id) +
        sizeof(data->entryPoint) +
        sizeof(data->referencedTypeSystemTables) +
        (sizeof(ULONG) * data->typeSystemTableRowsSize);
    m_data = new BYTE[m_size];

#if BIGENDIAN
    PORT_PDB_STREAM swappedData = *data;
    SwapGuid(&swappedData.id.pdbGuid);
    swappedData.id.pdbTimeStamp = VAL32(swappedData.id.pdbTimeStamp);
    swappedData.entryPoint = VAL32(swappedData.entryPoint);
    swappedData.referencedTypeSystemTables = VAL64(swappedData.referencedTypeSystemTables);
    // typeSystemTableRows and typeSystemTableRowsSize handled below
    data = &swappedData;
#endif

    ULONG offset = 0;
    if (memcpy_s(m_data + offset, m_size, &data->id, sizeof(data->id)))
        return E_FAIL;
    offset += sizeof(data->id);

    if (memcpy_s(m_data + offset, m_size, &data->entryPoint, sizeof(data->entryPoint)))
        return E_FAIL;
    offset += sizeof(data->entryPoint);

    if (memcpy_s(m_data + offset, m_size, &data->referencedTypeSystemTables, sizeof(data->referencedTypeSystemTables)))
        return E_FAIL;
    offset += sizeof(data->referencedTypeSystemTables);

#if !BIGENDIAN
    if (memcpy_s(m_data + offset, m_size, data->typeSystemTableRows, sizeof(ULONG) * data->typeSystemTableRowsSize))
        return E_FAIL;
    offset += sizeof(ULONG) * data->typeSystemTableRowsSize;
#else
    for (int i = 0; i < data->typeSystemTableRowsSize; i++)
    {
        SET_UNALIGNED_VAL32(m_data + offset, data->typeSystemTableRows[i]);
        offset += sizeof(ULONG);
    }
#endif

    _ASSERTE(offset == m_size);

    return S_OK;
}

__checkReturn
HRESULT PdbHeap::SaveToStream(IStream* stream)
{
    HRESULT hr = S_OK;
    if (!IsEmpty())
    {
        ULONG written = 0;
        hr = stream->Write(m_data, m_size, &written);
        _ASSERTE(m_size == written);
    }
    return hr;
}

BOOL PdbHeap::IsEmpty()
{
    return m_size == 0;
}

ULONG PdbHeap::GetSize()
{
    return m_size;
}
