// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Interpreter-FIXME: We have to put this up here because of some sort of interaction between precompiled headers
//  and an indirect dependency on safemath.h before the #include "safemath.h" further down - that one isn't including it
#undef _ASSERTE_SAFEMATH
#define _ASSERTE_SAFEMATH(expr) GCINFO_ASSERT(expr)

#include <stdint.h>
#include "gcinfohelpers.h"
#include "iallocator.h"
#include "gcinfoarraylist.h"
#include "safemath.h"

inline size_t roundUp(size_t size, size_t alignment)
{
    // `alignment` must be a power of two
    GCINFO_ASSERT(alignment != 0);
    GCINFO_ASSERT((alignment & (alignment - 1)) == 0);

    return (size + (alignment - 1)) & ~(alignment - 1);
}

GcInfoArrayListBase::GcInfoArrayListBase(IAllocator* allocator)
    : m_allocator(allocator),
      m_firstChunk(nullptr),
      m_lastChunk(nullptr),
      m_lastChunkCount(0),
      m_lastChunkCapacity(0),
      m_itemCount(0)
{
    GCINFO_ASSERT(m_allocator != nullptr);
}

GcInfoArrayListBase::~GcInfoArrayListBase()
{
    for (ChunkBase* list = m_firstChunk, *chunk; list != nullptr; list = chunk)
    {
        chunk = list->m_next;
        m_allocator->Free(list);
    }
}

void GcInfoArrayListBase::AppendNewChunk(size_t firstChunkCapacity, size_t elementSize, size_t chunkAlignment)
{
    size_t chunkCapacity = (m_firstChunk == nullptr) ? firstChunkCapacity : (m_lastChunkCapacity * GrowthFactor);

    S_SIZE_T chunkSize = S_SIZE_T(roundUp(sizeof(ChunkBase), chunkAlignment)) + (S_SIZE_T(elementSize) * S_SIZE_T(chunkCapacity));
    GCINFO_ASSERT(!chunkSize.IsOverflow());

    ChunkBase* chunk = reinterpret_cast<ChunkBase*>(m_allocator->Alloc(chunkSize.Value()));
    chunk->m_next = nullptr;

    if (m_lastChunk != nullptr)
    {
        GCINFO_ASSERT(m_firstChunk != nullptr);
        m_lastChunk->m_next = chunk;
    }
    else
    {
        GCINFO_ASSERT(m_lastChunk == nullptr);
        m_firstChunk = chunk;
    }

    m_lastChunk = chunk;
    m_lastChunkCount = 0;
    m_lastChunkCapacity = chunkCapacity;
}

GcInfoArrayListBase::IteratorBase::IteratorBase(GcInfoArrayListBase* list, size_t firstChunkCapacity)
    : m_list(list)
{
    GCINFO_ASSERT(m_list != nullptr);

    // Note: if the list is empty, m_list->firstChunk == nullptr == m_list->lastChunk and m_lastChunkCount == 0.
    //       In that case, the next two lines will set m_currentChunk to nullptr and m_currentChunkCount to 0.
    m_currentChunk = m_list->m_firstChunk;
    m_currentChunkCount = (m_currentChunk == m_list->m_lastChunk) ? m_list->m_lastChunkCount : firstChunkCapacity;
}

GcInfoArrayListBase::ChunkBase* GcInfoArrayListBase::IteratorBase::GetNextChunk(size_t& elementCount)
{
    if (m_currentChunk == nullptr)
    {
        elementCount = 0;
        return nullptr;
    }

    ChunkBase* chunk = m_currentChunk;
    elementCount = m_currentChunkCount;

    m_currentChunk = m_currentChunk->m_next;
    if (m_currentChunk == nullptr)
    {
        m_currentChunkCount = 0;
    }
    else if (m_currentChunk == m_list->m_lastChunk)
    {
        m_currentChunkCount = m_list->m_lastChunkCount;
    }
    else
    {
        m_currentChunkCount *= GrowthFactor;
    }

    return chunk;
}
