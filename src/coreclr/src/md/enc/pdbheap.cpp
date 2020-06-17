// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    if (memcpy_s(m_data + offset, m_size, data->typeSystemTableRows, sizeof(ULONG) * data->typeSystemTableRowsSize))
        return E_FAIL;
    offset += sizeof(ULONG) * data->typeSystemTableRowsSize;

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
