// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// StgPool.cpp
//
// Pools fold duplicate string and binary values before storing them in
// metadata streams.
//*****************************************************************************
#include "stdafx.h"
#include <stgpool.h>

int CStringPoolHash::Cmp(const void *pData, void *pItem)
{
    STATIC_CONTRACT_NOTHROW;

    LPCSTR p1 = reinterpret_cast<LPCSTR>(pData);
    LPCSTR p2;
    if (FAILED(m_Pool->GetString(reinterpret_cast<STRINGHASH*>(pItem)->iOffset, &p2)))
        return -1;
    return strcmp(p1, p2);
}

int CBlobPoolHash::Cmp(const void *pData, void *pItem)
{
    STATIC_CONTRACT_NOTHROW;

    ULONG ul1 = CPackedLen::GetLength(pData);
    ul1 += CPackedLen::Size(ul1);

    MetaData::DataBlob data2;
    if (FAILED(m_Pool->GetData(reinterpret_cast<BLOBHASH*>(pItem)->iOffset, &data2)))
        return -1;

    ULONG ul2 = CPackedLen::GetLength(data2.GetDataPointer());
    ul2 += CPackedLen::Size(ul2);

    if (ul1 < ul2)
        return -1;
    if (ul1 > ul2)
        return 1;
    return memcmp(pData, data2.GetDataPointer(), ul1);
}

int CGuidPoolHash::Cmp(const void *pData, void *pItem)
{
    STATIC_CONTRACT_NOTHROW;

    GUID *p2;
    if (FAILED(m_Pool->GetGuid(reinterpret_cast<GUIDHASH*>(pItem)->iIndex, &p2)))
        return -1;
    return memcmp(pData, p2, sizeof(GUID));
}
