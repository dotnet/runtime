// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "stdafx.h"

#include "arraylist.h"
#include "utilcode.h"
#include "ex.h"

//
// ArrayList is a simple class which is used to contain a growable
// list of pointers, stored in chunks.  Modification is by appending
// only currently.  Access is by index (efficient if the number of
// elements stays small) and iteration (efficient in all cases).
//
// An important property of an ArrayList is that the list remains
// coherent while it is being modified (appended to). This means that readers
// never need to lock when accessing it. (Locking is necessary among multiple
// writers, however.)
//

void ArrayListBase::Clear()
{
    CONTRACTL
    {
        NOTHROW;
        FORBID_FAULT;
    }
    CONTRACTL_END

    ArrayListBlock *block = m_firstBlock.m_next;
    while (block != NULL)
    {
        ArrayListBlock *next = block->m_next;
        delete [] block;
        block = next;
    }
    m_firstBlock.m_next = 0;
    m_count = 0;
}

PTR_VOID * ArrayListBase::GetPtr(DWORD index) const
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_SO_TOLERANT;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;
    SUPPORTS_DAC;

    _ASSERTE(index < m_count);

    ArrayListBlock *b = (ArrayListBlock*)&m_firstBlock;
    while (index >= b->m_blockSize)
    {
        PREFIX_ASSUME(b->m_next != NULL);
        index -= b->m_blockSize;
        b = b->m_next;
    }

    return b->m_array + index;
}

#ifndef DACCESS_COMPILE

HRESULT ArrayListBase::Append(void *element)
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    ArrayListBlock *b = (ArrayListBlock*)&m_firstBlock;
    DWORD           count = m_count;

    while (count >= b->m_blockSize)
    {
        count -= b->m_blockSize;

        if (b->m_next == NULL)
        {
            _ASSERTE(count == 0);

            DWORD nextSize = b->m_blockSize * 2;

            ArrayListBlock *bNew = (ArrayListBlock *)
              new (nothrow) BYTE [sizeof(ArrayListBlock) + nextSize * sizeof(void*)];

            if (bNew == NULL)
                return E_OUTOFMEMORY;

            bNew->m_next = NULL;
            bNew->m_blockSize = nextSize;

            b->m_next = bNew;
        }

        b = b->m_next;
    }

    b->m_array[count] = element;

    m_count++;

    return S_OK;
}

#endif // #ifndef DACCESS_COMPILE

DWORD ArrayListBase::FindElement(DWORD start, PTR_VOID element) const
{
    CONTRACTL
    {
        NOTHROW;
        FORBID_FAULT;
    }
    CONTRACTL_END

    DWORD index = start;

    _ASSERTE(index <= m_count);

    ArrayListBlock *b = (ArrayListBlock*)&m_firstBlock;

    //
    // Skip to the block containing start.
    // index should be the index of start in the block.
    //

    while (b != NULL && index >= b->m_blockSize)
    {
        index -= b->m_blockSize;
        b = b->m_next;
    }

    //
    // Adjust start to be the index of the start of the block
    //

    start -= index;

    //
    // Compute max number of entries from the start of the block
    //

    DWORD max = m_count - start;

    while (b != NULL)
    {
        //
        // Compute end of search in this block - either end of the block
        // or end of the array
        //

        DWORD blockMax;
        if (max < b->m_blockSize)
            blockMax = max;
        else
            blockMax = b->m_blockSize;

        //
        // Scan for element, until the end.
        //

        while (index < blockMax)
        {
            if (b->m_array[index] == element)
                return start + index;
            index++;
        }

        //
        // Otherwise, increment block start index, decrement max count,
        // reset index, and go to the next block (if any)
        //

        start += b->m_blockSize;
        max -= b->m_blockSize;
        index = 0;
        b = b->m_next;
    }

    return (DWORD) NOT_FOUND;
}

BOOL ArrayListBase::Iterator::Next()
{
    LIMITED_METHOD_DAC_CONTRACT;

    ++m_index;

    if (m_index >= m_remaining)
        return FALSE;

    if (m_index >= m_block->m_blockSize)
    {
        m_remaining -= m_block->m_blockSize;
        m_index -= m_block->m_blockSize;
        m_total += m_block->m_blockSize;
        m_block = m_block->m_next;
    }

    return TRUE;
}

#ifdef DACCESS_COMPILE

void
ArrayListBase::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;

    // Assume that 'this' is enumerated, either explicitly
    // or because this class is embedded in another.

    PTR_ArrayListBlock block = m_firstBlock.m_next;
    while (block.IsValid())
    {
        block.EnumMem();
        block = block->m_next;
    }
}

#endif // #ifdef DACCESS_COMPILE


void StructArrayListBase::Destruct (FreeProc *pfnFree)
{
    WRAPPER_NO_CONTRACT;
    
    StructArrayListEntryBase *pList = m_pChunkListHead;
    while (pList)
    {
        StructArrayListEntryBase *pTrash = pList;
        pList = pList->pNext;
        pfnFree(this, pTrash);
    }
}

// Copied from jit.h
inline
size_t              roundUp(size_t size, size_t mult = sizeof(size_t))
{
    assert(mult && ((mult & (mult-1)) == 0));   // power of two test

    return  (size + (mult - 1)) & ~(mult - 1);
}

void StructArrayListBase::CreateNewChunk (SIZE_T InitialChunkLength, SIZE_T ChunkLengthGrowthFactor, SIZE_T cbElement, AllocProc *pfnAlloc, SIZE_T alignment)
{
    CONTRACTL {
        THROWS;
        PRECONDITION(!m_pChunkListHead || m_nItemsInLastChunk == m_nLastChunkCapacity);
    } CONTRACTL_END;

    SIZE_T nChunkCapacity;
    if (!m_pChunkListHead)
        nChunkCapacity = InitialChunkLength;
    else
        nChunkCapacity = m_nLastChunkCapacity * ChunkLengthGrowthFactor;
    
    S_SIZE_T cbBaseSize = S_SIZE_T(roundUp(sizeof(StructArrayListEntryBase), alignment));
    S_SIZE_T cbChunk = cbBaseSize + 
        S_SIZE_T(cbElement) * S_SIZE_T(nChunkCapacity);
    _ASSERTE(!cbChunk.IsOverflow());
    if(cbChunk.IsOverflow())
    {
        ThrowWin32(ERROR_ARITHMETIC_OVERFLOW);
    }

    StructArrayListEntryBase *pNewChunk = (StructArrayListEntryBase*)pfnAlloc(this, cbChunk.Value());

    if (m_pChunkListTail)
    {
        _ASSERTE(m_pChunkListHead);
        m_pChunkListTail->pNext = pNewChunk;
    }
    else
    {
        _ASSERTE(!m_pChunkListHead);
        m_pChunkListHead = pNewChunk;
    }
    
    pNewChunk->pNext = NULL;
    m_pChunkListTail = pNewChunk;

    m_nItemsInLastChunk = 0;
    m_nLastChunkCapacity = nChunkCapacity;
}


void StructArrayListBase::ArrayIteratorBase::SetCurrentChunk (StructArrayListEntryBase *pChunk, SIZE_T nChunkCapacity)
{
    LIMITED_METHOD_CONTRACT;

    m_pCurrentChunk = pChunk;

    if (pChunk)
    {
        if (pChunk == m_pArrayList->m_pChunkListTail)
            m_nItemsInCurrentChunk = m_pArrayList->m_nItemsInLastChunk;
        else
            m_nItemsInCurrentChunk = nChunkCapacity;

        m_nCurrentChunkCapacity = nChunkCapacity;
    }
}

