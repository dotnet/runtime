// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"

#include "memorypool.h"
#include "ex.h"

size_t MemoryPool::GetSize()
{
    LIMITED_METHOD_CONTRACT;
    size_t retval=0;

    Block *block = m_blocks;
    while (block != NULL)
    {
        retval+=(BYTE*)block->elementsEnd-(BYTE*)block->elements;
        block = block->next;
    }
    return retval;
}

#ifndef DACCESS_COMPILE

BOOL MemoryPool::AddBlock(SIZE_T elementCount)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        INJECT_FAULT(return FALSE;);
    } CONTRACTL_END;

    //
    // Check for arithmetic overflow
    //
    S_SIZE_T cbBlockSize = S_SIZE_T(elementCount) * S_SIZE_T(m_elementSize);
    S_SIZE_T cbAllocSize = S_SIZE_T(sizeof(Block)) + cbBlockSize;
    if (cbBlockSize.IsOverflow() || cbAllocSize.IsOverflow())
        return FALSE;

    //
    // Allocate the new block.
    //

    Block *block = (Block *) new (nothrow) BYTE [cbAllocSize.Value()];

    if (block == NULL)
        return FALSE;

    //
    // Chain all elements together for the free list
    //

    _ASSERTE(m_freeList == NULL);
    Element **prev = &m_freeList;

    Element *e = block->elements;
    Element *eEnd = (Element *) ((BYTE*) block->elements + elementCount*m_elementSize);
    while (e < eEnd)
    {
        *prev = e;
        prev = &e->next;
#if _DEBUG
        DeadBeef(e);
#endif
        e = (Element*) ((BYTE*)e + m_elementSize);
    }

    *prev = NULL;

    //
    // Initialize the other block fields & link the block into the block list
    //

    block->elementsEnd = e;
    block->next = m_blocks;
    m_blocks = block;

    return TRUE;
}

void MemoryPool::DeadBeef(Element *element)
{
#if _DEBUG
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    } CONTRACTL_END;

    int *i = &element->deadBeef;
    int *iEnd = (int*) ((BYTE*)element + m_elementSize);
    while (i < iEnd)
        *i++ = (int) 0xdeadbeef;
#else
    LIMITED_METHOD_CONTRACT;
#endif
}

MemoryPool::MemoryPool(SIZE_T elementSize, SIZE_T initGrowth, SIZE_T initCount)
    : m_elementSize(elementSize),
      m_growCount(initGrowth),
      m_blocks(NULL),
      m_freeList(NULL)
{
    CONTRACTL {
        if (initCount) THROWS; else NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    _ASSERTE(elementSize >= sizeof(Element));
    _ASSERTE((elementSize & ((sizeof(PVOID)-1))) == 0);

    if (initCount > 0)
        AddBlock(initCount);
}

MemoryPool::~MemoryPool()
{
    LIMITED_METHOD_CONTRACT;
    Block *block = m_blocks;
    while (block != NULL)
    {
        Block *next = block->next;
        delete [] (BYTE*) block;
        block = next;
    }
}

BOOL MemoryPool::IsElement(void *element)
{
    LIMITED_METHOD_CONTRACT;

    Block *block = m_blocks;
    while (block != NULL)
    {
        if (element >= block->elements    &&
            element <  block->elementsEnd  )
        {
            return ((BYTE *)element - (BYTE*)block->elements) % m_elementSize == 0;
        }
        block = block->next;
    }

    return FALSE;
}

BOOL MemoryPool::IsAllocatedElement(void *element)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    } CONTRACTL_END;

    if (!IsElement(element))
        return FALSE;

    //
    // Now, make sure the element isn't
    // in the free list.
    //

#if _DEBUG
    //
    // In a debug build, all objects on the free list
    // will be marked with deadbeef.  This means that
    // if the object is not deadbeef, it's not on the
    // free list.
    //
    // This check will give us decent performance in
    // a debug build for FreeElement, since we
    // always expect to return TRUE in that case.
    //

    if (((Element*)element)->deadBeef != (int) 0xdeadBeef)
        return TRUE;
#endif

    Element *f = m_freeList;
    while (f != NULL)
    {
        if (f == element)
            return FALSE;
        f = f->next;
    }

#if _DEBUG
    //
    // We should never get here in a debug build, because
    // all free elements should be deadbeefed.
    //
    _ASSERTE(!"Unreachable");
#endif

    return TRUE;
}

void *MemoryPool::AllocateElement()
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    void *element = AllocateElementNoThrow();
    if (element == NULL)
        ThrowOutOfMemory();

    return element;
}

void *MemoryPool::AllocateElementNoThrow()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        INJECT_FAULT( return FALSE; );
    } CONTRACTL_END;

    void *element = m_freeList;

    if (element == NULL)
    {
        if (!AddBlock(m_growCount))
            return NULL;

        m_growCount *= 2;
        element = m_freeList;
    }

    // if we come there means that addblock succeeded and m_freelist isn't null anymore
    PREFIX_ASSUME(m_freeList!= NULL);
    m_freeList = m_freeList->next;

    return element;
}

void MemoryPool::FreeElement(void *element)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    } CONTRACTL_END;

#if _DEBUG // don't want to do this assert in a non-debug build; it is expensive
    _ASSERTE(IsAllocatedElement(element));
#endif

    Element *e = (Element *) element;

#if _DEBUG
    DeadBeef(e);
#endif

    e->next = m_freeList;
    m_freeList = e;
}

void MemoryPool::FreeAllElements()
{
    LIMITED_METHOD_CONTRACT;

    Block *block = m_blocks;
    while (block != NULL)
    {
        Block *next = block->next;
        delete [] block;
        block = next;
    }

    m_freeList = NULL;
    m_blocks = NULL;
}



MemoryPool::Iterator::Iterator(MemoryPool *pool)
{
    LIMITED_METHOD_CONTRACT;

    //
    // Warning!  This only works if you haven't freed
    // any elements.
    //

    m_next = pool->m_blocks;
    m_e = NULL;
    m_eEnd = NULL;
    m_end = (BYTE*) pool->m_freeList;
    m_size = pool->m_elementSize;
}

BOOL MemoryPool::Iterator::Next()
{
    LIMITED_METHOD_CONTRACT;

    if (m_e == m_eEnd
        || (m_e == m_end && m_end != NULL))
    {
        if (m_next == NULL)
            return FALSE;
        m_e = (BYTE*) m_next->elements;
        m_eEnd = (BYTE*) m_next->elementsEnd;
        m_next = m_next->next;
        if (m_e == m_end)
            return FALSE;
    }

    m_e += m_size;

    return TRUE;
}

#endif
