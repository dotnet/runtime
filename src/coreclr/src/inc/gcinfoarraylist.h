// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _GCINFOARRAYLIST_H_
#define _GCINFOARRAYLIST_H_

// GCInfoArrayList is basically a more efficient linked list--it's useful for accumulating
// lots of small fixed-size allocations into larger chunks which in a typical linked list
// would incur an unnecessarily high amount of overhead.

class GcInfoArrayListBase
{
private:
    static const size_t GrowthFactor = 2;

protected:
    friend class IteratorBase;

    struct ChunkBase
    {
        ChunkBase* m_next; // actually GcInfoArrayListChunk<ElementType>*
    };

    class IteratorBase
    {
    protected:
        IteratorBase(GcInfoArrayListBase* list, size_t firstChunkCapacity);
        ChunkBase* GetNextChunk(size_t& elementCount);
    
    private:
        GcInfoArrayListBase* m_list;
        ChunkBase* m_currentChunk;
        size_t m_currentChunkCount;
    };

    GcInfoArrayListBase(IAllocator* allocator);
    virtual ~GcInfoArrayListBase();

    void AppendNewChunk(size_t firstChunkCapacity, size_t elementSize, size_t chunkAlignment);

public:
    size_t Count()
    {
        return m_itemCount;
    }

protected:
    IAllocator* m_allocator;
    ChunkBase* m_firstChunk; // actually GcInfoArrayListChunk<ElementType>*
    ChunkBase* m_lastChunk; // actually GcInfoArrayListChunk<ElementType>*
    size_t m_lastChunkCount;
    size_t m_lastChunkCapacity;
    size_t m_itemCount;
};

template <typename ElementType, size_t FirstChunkCapacity>
class GcInfoArrayList : public GcInfoArrayListBase
{
private:
    struct Chunk : public ChunkBase
    {
        ElementType m_items[];
    };

public:
    friend class Iterator;

    struct Iterator : IteratorBase
    {
        Iterator(GcInfoArrayList* list)
            : IteratorBase(list, FirstChunkCapacity)
        {
        }

        ElementType* GetNext(size_t* elementCount)
        {
            Chunk* chunk = reinterpret_cast<Chunk*>(GetNextChunk(*elementCount));
            return chunk == nullptr ? nullptr : &chunk->m_items[0];
        }
    };

    GcInfoArrayList(IAllocator* allocator)
        : GcInfoArrayListBase(allocator)
    {
    }

    ElementType* Append()
    {
        if (m_lastChunk == nullptr || m_lastChunkCount == m_lastChunkCapacity)
        {
            AppendNewChunk(FirstChunkCapacity, sizeof(ElementType), __alignof(ElementType));
        }

        m_itemCount++;
        m_lastChunkCount++;
        return &reinterpret_cast<Chunk*>(m_lastChunk)->m_items[m_lastChunkCount - 1];
    }

    void CopyTo(ElementType* dest)
    {
        Iterator iter(this);
        ElementType* source;
        size_t elementCount;

        while (source = iter.GetNext(&elementCount), source != nullptr)
        {
            memcpy(dest, source, elementCount * sizeof(ElementType));
            dest += elementCount;
        }
    }
};

#endif
