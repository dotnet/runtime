// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "portable_pdb.h"
#include <time.h>

PortablePdbWritter::PortablePdbWritter()
{
}

PortablePdbWritter::~PortablePdbWritter()
{
    if (m_pdbEmitter != NULL)
    {
        m_pdbEmitter->Release();
        m_pdbEmitter = NULL;
    }
}

HRESULT PortablePdbWritter::Init(IMetaDataEmit2* pdbEmitter)
{
    m_pdbEmitter = pdbEmitter;
    time_t now;
    time(&now);
    m_timestamp = (ULONG)now;
    return CoCreateGuid(&m_guid);
}

IMetaDataEmit2* PortablePdbWritter::GetEmitter()
{
    return m_pdbEmitter;
}

GUID* PortablePdbWritter::GetGuid()
{
    return &m_guid;
}

ULONG PortablePdbWritter::GetTimestamp()
{
    return m_timestamp;
}
