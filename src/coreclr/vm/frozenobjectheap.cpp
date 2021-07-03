// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "frozenobjectheap.h"
#include "memorypool.h"

void FrozenObjectHeap::Init(void* start, size_t size)
{
    m_pStart = static_cast<uint8_t*>(start);
    m_pCurrent = m_pStart;
    m_Size = size;
}

void* FrozenObjectHeap::Alloc(size_t size)
{
    uint8_t* obj = ALIGN_UP(m_pCurrent, DATA_ALIGNMENT);
    if ((obj + size + OBJHEADER_SIZE) > (m_pStart + m_Size))
    {
        return nullptr;
    }
    m_pCurrent = obj + OBJECT_BASESIZE + size;
    ZeroMemory(obj, size);
    return obj + OBJECT_BASESIZE;
}
