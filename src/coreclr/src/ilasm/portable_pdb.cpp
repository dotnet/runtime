// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "portable_pdb.h"
#include <time.h>

PortablePdbWritter::PortablePdbWritter()
{
    m_pdbStream.id.pdbGuid = { 0 };
    m_pdbStream.id.pdbTimeStamp = 0;
    m_pdbStream.entryPoint = mdMethodDefNil;
    m_pdbStream.referencedTypeSystemTables = 0UL;
    m_pdbStream.typeSystemTableRows = new ULONG[TBL_COUNT];
    m_pdbStream.typeSystemTableRowsSize = 0;
}

PortablePdbWritter::~PortablePdbWritter()
{
    if (m_pdbEmitter != NULL)
    {
        m_pdbEmitter->Release();
        m_pdbEmitter = NULL;
    }
    if (m_pdbStream.typeSystemTableRows != NULL)
    {
        delete[] m_pdbStream.typeSystemTableRows;
        m_pdbStream.typeSystemTableRows = NULL;
    }
}

HRESULT PortablePdbWritter::Init(IMetaDataEmit2* pdbEmitter)
{
    m_pdbEmitter = pdbEmitter;
    memset(m_pdbStream.typeSystemTableRows, 0, sizeof(ULONG) * TBL_COUNT);
    time_t now;
    time(&now);
    m_pdbStream.id.pdbTimeStamp = (ULONG)now;
    return CoCreateGuid(&m_pdbStream.id.pdbGuid);
}

IMetaDataEmit2* PortablePdbWritter::GetEmitter()
{
    return m_pdbEmitter;
}

GUID* PortablePdbWritter::GetGuid()
{
    return &m_pdbStream.id.pdbGuid;
}

ULONG PortablePdbWritter::GetTimestamp()
{
    return m_pdbStream.id.pdbTimeStamp;
}

HRESULT PortablePdbWritter::BuildPdbStream(IMetaDataEmit2* peEmitter, mdMethodDef entryPoint)
{
    HRESULT hr = S_OK;

    m_pdbStream.entryPoint = entryPoint;

    if (FAILED(hr = peEmitter->GetReferencedTypeSysTables(
        &m_pdbStream.referencedTypeSystemTables,
        m_pdbStream.typeSystemTableRows,
        TBL_COUNT,
        &m_pdbStream.typeSystemTableRowsSize))) goto exit;

    if (FAILED(hr = m_pdbEmitter->DefinePdbStream(&m_pdbStream))) goto exit;

exit:
    return hr;
}
