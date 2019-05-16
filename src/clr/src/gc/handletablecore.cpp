// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
 * Generational GC handle manager.  Core Table Implementation.
 *
 * Implementation of core table management routines.
 *

 *
 */

#include "common.h"

#include "gcenv.h"
#include "gcenv.inl"
#include "gc.h"
#include "handletablepriv.h"

/****************************************************************************
 *
 * RANDOM HELPERS
 *
 ****************************************************************************/

const uint8_t c_rgLowBitIndex[256] =
{
    0xff, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x03, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x04, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x03, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x05, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x03, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x04, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x03, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x06, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x03, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x04, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x03, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x05, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x03, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x04, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x03, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x07, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x03, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x04, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x03, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x05, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x03, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x04, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x03, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x06, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x03, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x04, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x03, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x05, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x03, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x04, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x03, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
};

#ifndef DACCESS_COMPILE

/*
 * A 32/64 neutral quicksort
 */
//<TODO>@TODO: move/merge into common util file</TODO>
typedef int (*PFNCOMPARE)(uintptr_t p, uintptr_t q);
void QuickSort(uintptr_t *pData, int left, int right, PFNCOMPARE pfnCompare)
{
    WRAPPER_NO_CONTRACT;

    do
    {
        int i = left;
        int j = right;

        uintptr_t x = pData[(i + j + 1) / 2];

        do
        {
            while (pfnCompare(pData[i], x) < 0)
                i++;

            while (pfnCompare(x, pData[j]) < 0)
                j--;

            if (i > j)
                break;

            if (i < j)
            {
                uintptr_t t = pData[i];
                pData[i] = pData[j];
                pData[j] = t;
            }

            i++;
            j--;

        } while (i <= j);

        if ((j - left) <= (right - i))
        {
            if (left < j)
                QuickSort(pData, left, j, pfnCompare);

            left = i;
        }
        else
        {
            if (i < right)
                QuickSort(pData, i, right, pfnCompare);

            right = j;
        }

    } while (left < right);
}


/*
 * CompareHandlesByFreeOrder
 *
 * Returns:
 *  <0 - handle P should be freed before handle Q
 *  =0 - handles are eqivalent for free order purposes
 *  >0 - handle Q should be freed before handle P
 *
 */
int CompareHandlesByFreeOrder(uintptr_t p, uintptr_t q)
{
    LIMITED_METHOD_CONTRACT;

    // compute the segments for the handles
    TableSegment *pSegmentP = (TableSegment *)(p & HANDLE_SEGMENT_ALIGN_MASK);
    TableSegment *pSegmentQ = (TableSegment *)(q & HANDLE_SEGMENT_ALIGN_MASK);

    // are the handles in the same segment?
    if (pSegmentP == pSegmentQ)
    {
        // return the in-segment handle free order
        return (int)((intptr_t)q - (intptr_t)p);
    }
    else if (pSegmentP)
    {
        // do we have two valid segments?
        if (pSegmentQ)
        {
            // return the sequence order of the two segments
            return (int)(uint32_t)pSegmentQ->bSequence - (int)(uint32_t)pSegmentP->bSequence;
        }
        else
        {
            // only the P handle is valid - free Q first
            return 1;
        }
    }
    else if (pSegmentQ)
    {
        // only the Q handle is valid - free P first
        return -1;
    }

    // neither handle is valid
    return 0;
}


/*
 * ZeroHandles
 *
 * Zeroes the object pointers for an array of handles.
 *
 */
void ZeroHandles(OBJECTHANDLE *pHandleBase, uint32_t uCount)
{
    LIMITED_METHOD_CONTRACT;

    // compute our stopping point
    OBJECTHANDLE *pLastHandle = pHandleBase + uCount;

    // loop over the array, zeroing as we go
    while (pHandleBase < pLastHandle)
    {
        // get the current handle from the array
        OBJECTHANDLE handle = *pHandleBase;

        // advance to the next handle
        pHandleBase++;

        // zero the handle's object pointer
        *(_UNCHECKED_OBJECTREF *)handle = NULL;
    }
}

#ifdef _DEBUG
void CALLBACK DbgCountEnumeratedBlocks(TableSegment *pSegment, uint32_t uBlock, uint32_t uCount, ScanCallbackInfo *pInfo)
{
    LIMITED_METHOD_CONTRACT;
    UNREFERENCED_PARAMETER(pSegment);
    UNREFERENCED_PARAMETER(uBlock);

    // accumulate the block count in pInfo->param1
    pInfo->param1 += uCount;
}
#endif

/*--------------------------------------------------------------------------*/



/****************************************************************************
 *
 * CORE TABLE MANAGEMENT
 *
 ****************************************************************************/

/*
 * TableCanFreeSegmentNow
 *
 * Determines if it is OK to free the specified segment at this time.
 *
 */
BOOL TableCanFreeSegmentNow(HandleTable *pTable, TableSegment *pSegment)
{
    LIMITED_METHOD_CONTRACT;

    // sanity
    _ASSERTE(pTable);
    _ASSERTE(pSegment);
#ifdef _DEBUG
    // there have been cases in the past where the original assert would
    // fail but by the time a dump was created the lock was unowned so
    // there was no way to tell who the previous owner was.
    EEThreadId threadId = pTable->Lock.GetHolderThreadId();
    _ASSERTE(threadId.IsCurrentThread());
#endif // _DEBUG

    // deterine if any segment is currently being scanned asynchronously
    TableSegment *pSegmentAsync = NULL;

    // do we have async info?
    AsyncScanInfo *pAsyncInfo = pTable->pAsyncScanInfo;
    if (pAsyncInfo)
    {
        // must always have underlying callback info in an async scan
        _ASSERTE(pAsyncInfo->pCallbackInfo);

        // yes - if a segment is being scanned asynchronously it is listed here
        pSegmentAsync = pAsyncInfo->pCallbackInfo->pCurrentSegment;
    }

    // we can free our segment if it isn't being scanned asynchronously right now
    return (pSegment != pSegmentAsync);
}

#endif // !DACCESS_COMPILE

/*
 * BlockFetchUserDataPointer
 *
 * Gets the user data pointer for the first handle in a block.
 *
 */
PTR_uintptr_t BlockFetchUserDataPointer(PTR__TableSegmentHeader pSegment, uint32_t uBlock, BOOL fAssertOnError)
{
    LIMITED_METHOD_DAC_CONTRACT;

    // assume NULL until we actually find the data
    PTR_uintptr_t pUserData = NULL;
    // get the user data index for this block
    uint32_t blockIndex = pSegment->rgUserData[uBlock];

    // is there user data for the block?
    if (blockIndex != BLOCK_INVALID)
    {
        // In DAC builds, we may not have the entire segment table mapped and in any case it will be quite
        // large. Since we only need one element, we'll retrieve just that one element.  
        pUserData = PTR_uintptr_t(PTR_TO_TADDR(pSegment) + offsetof(TableSegment, rgValue) + 
                               (blockIndex * HANDLE_BYTES_PER_BLOCK));
    }
    else if (fAssertOnError)
    {
        // no user data is associated with this block
        //
        // we probably got here for one of the following reasons:
        //  1) an outside caller tried to do a user data operation on an incompatible handle
        //  2) the user data map in the segment is corrupt
        //  3) the global type flags are corrupt
        //
        _ASSERTE(FALSE);
    }

    // return the result
    return pUserData;
}


/*
 * HandleFetchSegmentPointer
 *
 * Computes the segment pointer for a given handle.
 *
 */
__inline PTR__TableSegmentHeader HandleFetchSegmentPointer(OBJECTHANDLE handle)
{
    LIMITED_METHOD_DAC_CONTRACT;

    // find the segment for this handle
    PTR__TableSegmentHeader pSegment = PTR__TableSegmentHeader((uintptr_t)handle & HANDLE_SEGMENT_ALIGN_MASK);

    // sanity
    _ASSERTE(pSegment);

    // return the segment pointer
    return pSegment;
}


/*
 * HandleValidateAndFetchUserDataPointer
 *
 * Gets the user data pointer for the specified handle.
 * ASSERTs and returns NULL if handle is not of the expected type.
 *
 */
uintptr_t *HandleValidateAndFetchUserDataPointer(OBJECTHANDLE handle, uint32_t uTypeExpected)
{
    WRAPPER_NO_CONTRACT;

    // get the segment for this handle
    PTR__TableSegmentHeader pSegment = HandleFetchSegmentPointer(handle);

    // find the offset of this handle into the segment
    uintptr_t offset = (uintptr_t)handle & HANDLE_SEGMENT_CONTENT_MASK;

    // make sure it is in the handle area and not the header
    _ASSERTE(offset >= HANDLE_HEADER_SIZE);

    // convert the offset to a handle index
    uint32_t uHandle = (uint32_t)((offset - HANDLE_HEADER_SIZE) / HANDLE_SIZE);

    // compute the block this handle resides in
    uint32_t uBlock = uHandle / HANDLE_HANDLES_PER_BLOCK;

    // fetch the user data for this block
    PTR_uintptr_t pUserData = BlockFetchUserDataPointer(pSegment, uBlock, TRUE);

    // did we get the user data block?
    if (pUserData)
    {
        // yup - adjust the pointer to be handle-specific
        pUserData += (uHandle - (uBlock * HANDLE_HANDLES_PER_BLOCK));

        // validate the block type before returning the pointer
        if (pSegment->rgBlockType[uBlock] != uTypeExpected)
        {
            // type mismatch - caller error
            _ASSERTE(FALSE);

            // don't return a pointer to the caller
            pUserData = NULL;
        }
    }

    // return the result
    return pUserData;
}

/*
 * HandleQuickFetchUserDataPointer
 *
 * Gets the user data pointer for a handle.
 * Less validation is performed.
 *
 */
PTR_uintptr_t HandleQuickFetchUserDataPointer(OBJECTHANDLE handle)
{
    WRAPPER_NO_CONTRACT;

    /*
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    */
    SUPPORTS_DAC;
    
    // get the segment for this handle
    PTR__TableSegmentHeader pSegment = HandleFetchSegmentPointer(handle);

    // find the offset of this handle into the segment
    uintptr_t offset = (uintptr_t)handle & HANDLE_SEGMENT_CONTENT_MASK;

    // make sure it is in the handle area and not the header
    _ASSERTE(offset >= HANDLE_HEADER_SIZE);

    // convert the offset to a handle index
    uint32_t uHandle = (uint32_t)((offset - HANDLE_HEADER_SIZE) / HANDLE_SIZE);

    // compute the block this handle resides in
    uint32_t uBlock = uHandle / HANDLE_HANDLES_PER_BLOCK;

    // fetch the user data for this block
    PTR_uintptr_t pUserData = BlockFetchUserDataPointer(pSegment, uBlock, TRUE);

    // if we got the user data block then adjust the pointer to be handle-specific
    if (pUserData)
        pUserData += (uHandle - (uBlock * HANDLE_HANDLES_PER_BLOCK));

    // return the result
    return pUserData;
}

#ifndef DACCESS_COMPILE
/*
 * HandleQuickSetUserData
 *
 * Stores user data with a handle.
 *
 */
void HandleQuickSetUserData(OBJECTHANDLE handle, uintptr_t lUserData)
{
    WRAPPER_NO_CONTRACT;

    /*
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    */
    
    // fetch the user data slot for this handle
    uintptr_t *pUserData = HandleQuickFetchUserDataPointer(handle);

    // is there a slot?
    if (pUserData)
    {
        // yes - store the info
        *pUserData = lUserData;
    }
}

#endif // !DACCESS_COMPILE

/*
 * HandleFetchType
 *
 * Computes the type index for a given handle.
 *
 */
uint32_t HandleFetchType(OBJECTHANDLE handle)
{
    WRAPPER_NO_CONTRACT;

    // get the segment for this handle
    PTR__TableSegmentHeader pSegment = HandleFetchSegmentPointer(handle);

    // find the offset of this handle into the segment
    uintptr_t offset = (uintptr_t)handle & HANDLE_SEGMENT_CONTENT_MASK;

    // make sure it is in the handle area and not the header
    _ASSERTE(offset >= HANDLE_HEADER_SIZE);

    // convert the offset to a handle index
    uint32_t uHandle = (uint32_t)((offset - HANDLE_HEADER_SIZE) / HANDLE_SIZE);

    // compute the block this handle resides in
    uint32_t uBlock = uHandle / HANDLE_HANDLES_PER_BLOCK;

    // return the block's type
    return pSegment->rgBlockType[uBlock];
}
    
/*
 * HandleFetchHandleTable
 *
 * Computes the type index for a given handle.
 *
 */
PTR_HandleTable HandleFetchHandleTable(OBJECTHANDLE handle)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    // get the segment for this handle
    PTR__TableSegmentHeader pSegment = HandleFetchSegmentPointer(handle);

    // return the table
    return pSegment->pHandleTable;
}

#ifndef DACCESS_COMPILE
/*
 * SegmentInitialize
 *
 * Initializes a segment.
 *
 */
BOOL SegmentInitialize(TableSegment *pSegment, HandleTable *pTable)
{
    LIMITED_METHOD_CONTRACT;

    /*
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    */
    
    // we want to commit enough for the header PLUS some handles
    size_t dwCommit = ALIGN_UP(HANDLE_HEADER_SIZE, OS_PAGE_SIZE);

    // commit the header
    if (!GCToOSInterface::VirtualCommit(pSegment, dwCommit))
    {
        //_ASSERTE(FALSE);
        return FALSE;
    }

    // remember how many blocks we commited
    pSegment->bCommitLine = (uint8_t)((dwCommit - HANDLE_HEADER_SIZE) / HANDLE_BYTES_PER_BLOCK);

    // now preinitialize the 0xFF guys
    memset(pSegment->rgGeneration, 0xFF,            sizeof(pSegment->rgGeneration));
    memset(pSegment->rgTail,       BLOCK_INVALID,   sizeof(pSegment->rgTail));
    memset(pSegment->rgHint,       BLOCK_INVALID,   sizeof(pSegment->rgHint));
    memset(pSegment->rgFreeMask,   0xFF,            sizeof(pSegment->rgFreeMask));
    memset(pSegment->rgBlockType,  TYPE_INVALID,    sizeof(pSegment->rgBlockType));
    memset(pSegment->rgUserData,   BLOCK_INVALID,   sizeof(pSegment->rgUserData));

    // prelink the free chain
    _ASSERTE(FitsInU1(HANDLE_BLOCKS_PER_SEGMENT));
    uint8_t u = 0;
    while (u < (HANDLE_BLOCKS_PER_SEGMENT - 1))
    {
        uint8_t next = u + 1;
        pSegment->rgAllocation[u] = next;
        u = next;
    }

    // and terminate the last node
    pSegment->rgAllocation[u] = BLOCK_INVALID;

    // store the back pointer from our new segment to its owning table
    pSegment->pHandleTable = pTable;

    // all done
    return TRUE;
}


/*
 * SegmentFree
 *
 * Frees the specified segment.
 *
 */
void SegmentFree(TableSegment *pSegment)
{
    WRAPPER_NO_CONTRACT;

    /*
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    */
    
    // free the segment's memory
    GCToOSInterface::VirtualRelease(pSegment, HANDLE_SEGMENT_SIZE);
}


/*
 * SegmentAlloc
 *
 * Allocates a new segment.
 *
 */
TableSegment *SegmentAlloc(HandleTable *pTable)
{
    LIMITED_METHOD_CONTRACT;

    /*
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    */
    
    // allocate the segment's address space
    TableSegment *pSegment = NULL;

    // All platforms currently require 64Kb aligned table segments, which is what VirtualAlloc guarantees.
    // The actual requirement is that the alignment of the reservation equals or exceeds the size of the
    // reservation. This requirement stems from the method the handle table uses to map quickly from a handle
    // address back to the handle table segment header.
    _ASSERTE(HANDLE_SEGMENT_ALIGNMENT >= HANDLE_SEGMENT_SIZE);
    _ASSERTE(HANDLE_SEGMENT_ALIGNMENT == 0x10000);

    pSegment = (TableSegment *)GCToOSInterface::VirtualReserve(HANDLE_SEGMENT_SIZE, HANDLE_SEGMENT_ALIGNMENT, VirtualReserveFlags::None);
    _ASSERTE(((size_t)pSegment % HANDLE_SEGMENT_ALIGNMENT) == 0);
    
    // bail out if we couldn't get any memory
    if (!pSegment)
    {
        return NULL;
    }

    // initialize the header
    if (!SegmentInitialize(pSegment, pTable))
    {
        SegmentFree(pSegment);
        pSegment = NULL;
    }

    // all done
    return pSegment;
}

/*
 * Check if a handle is part of a HandleTable
 */
BOOL TableContainHandle(HandleTable *pTable, OBJECTHANDLE handle)
{
    _ASSERTE (handle);

    // get the segment for this handle
    TableSegment *pSegment = (TableSegment *)HandleFetchSegmentPointer(handle);

    CrstHolder ch(&pTable->Lock);
    TableSegment *pWorkerSegment = pTable->pSegmentList;
    while (pWorkerSegment)
    {
        if (pWorkerSegment == pSegment)
        {
            return TRUE;
        }
        pWorkerSegment = pWorkerSegment->pNextSegment;
    }
    return FALSE;
}

/*
 * SegmentRemoveFreeBlocks
 *
 * Scans a segment for free blocks of the specified type
 * and moves them to the segment's free list.
 *
 */
void SegmentRemoveFreeBlocks(TableSegment *pSegment, uint32_t uType, BOOL *pfScavengeLater)
{
    WRAPPER_NO_CONTRACT;

    /*
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    */
    
    // fetch the tail block for the specified chain
    uint32_t uPrev = pSegment->rgTail[uType];

    // if it's a terminator then there are no blocks in the chain
    if (uPrev == BLOCK_INVALID)
        return;

    // we may need to clean up user data blocks later
    BOOL fCleanupUserData = FALSE;

    // start iterating with the head block
    uint32_t uStart = pSegment->rgAllocation[uPrev];
    uint32_t uBlock = uStart;

    // keep track of how many blocks we removed
    uint32_t uRemoved = 0;

    // we want to preserve the relative order of any blocks we free
    // this is the best we can do until the free list is resorted
    uint32_t uFirstFreed = BLOCK_INVALID;
    uint32_t uLastFreed  = BLOCK_INVALID;

    // loop until we've processed the whole chain
    for (;;)
    {
        // fetch the next block index
        uint32_t uNext = pSegment->rgAllocation[uBlock];

#ifdef HANDLE_OPTIMIZE_FOR_64_HANDLE_BLOCKS
        // determine whether this block is empty
        if (((uint64_t*)pSegment->rgFreeMask)[uBlock] == UI64(0xFFFFFFFFFFFFFFFF))
#else
        // assume this block is empty until we know otherwise
        BOOL fEmpty = TRUE;

        // get the first mask for this block
        uint32_t *pdwMask     = pSegment->rgFreeMask + (uBlock * HANDLE_MASKS_PER_BLOCK);
        uint32_t *pdwMaskLast = pdwMask              + HANDLE_MASKS_PER_BLOCK;

        // loop through the masks until we've processed them all or we've found handles
        do
        {
            // is this mask empty?
            if (*pdwMask != MASK_EMPTY)
            {
                // nope - this block still has handles in it
                fEmpty = FALSE;
                break;
            }

            // on to the next mask
            pdwMask++;

        } while (pdwMask < pdwMaskLast);

        // is this block empty?
        if (fEmpty)
#endif
        {
            // is this block currently locked?
            if (BlockIsLocked(pSegment, uBlock))
            {
                // block cannot be freed, if we were passed a scavenge flag then set it
                if (pfScavengeLater)
                    *pfScavengeLater = TRUE;
            }
            else
            {
                // safe to free - did it have user data associated?
                uint32_t uData = pSegment->rgUserData[uBlock];
                if (uData != BLOCK_INVALID)
                {
                    // data blocks are 'empty' so we keep them locked
                    // unlock the block so it can be reclaimed below
                    BlockUnlock(pSegment, uData);

                    // unlink the data block from the handle block
                    pSegment->rgUserData[uBlock] = BLOCK_INVALID;

                    // remember that we need to scavenge the data block chain
                    fCleanupUserData = TRUE;
                }

                // mark the block as free
                pSegment->rgBlockType[uBlock] = TYPE_INVALID;

                // have we freed any other blocks yet?
                if (uFirstFreed == BLOCK_INVALID)
                {
                    // no - this is the first one - remember it as the new head
                    uFirstFreed = uBlock;
                }
                else
                {
                    // yes - link this block to the other ones in order
                    pSegment->rgAllocation[uLastFreed] = (uint8_t)uBlock;
                }

                // remember this block for later
                uLastFreed = uBlock;

                // are there other blocks in the chain?
                if (uPrev != uBlock)
                {
                    // yes - unlink this block from the chain
                    pSegment->rgAllocation[uPrev] = (uint8_t)uNext;

                    // if we are removing the tail then pick a new tail
                    if (pSegment->rgTail[uType] == uBlock)
                        pSegment->rgTail[uType] = (uint8_t)uPrev;

                    // if we are removing the hint then pick a new hint
                    if (pSegment->rgHint[uType] == uBlock)
                        pSegment->rgHint[uType] = (uint8_t)uNext;

                    // we removed the current block - reset uBlock to a valid block
                    uBlock = uPrev;

                    // N.B. we'll check if we freed uStart later when it's safe to recover
                }
                else
                {
                    // we're removing last block - sanity check the loop condition
                    _ASSERTE(uNext == uStart);

                    // mark this chain as completely empty
                    pSegment->rgAllocation[uBlock] = BLOCK_INVALID;
                    pSegment->rgTail[uType]        = BLOCK_INVALID;
                    pSegment->rgHint[uType]        = BLOCK_INVALID;
                }

                // update the number of blocks we've removed
                uRemoved++;
            }
        }

        // if we are back at the beginning then it is time to stop
        if (uNext == uStart)
            break;

        // now see if we need to reset our start block
        if (uStart == uLastFreed)
            uStart = uNext;

        // on to the next block
        uPrev = uBlock;
        uBlock = uNext;
    }

    // did we remove any blocks?
    if (uRemoved)
    {
        // yes - link the new blocks into the free list
        pSegment->rgAllocation[uLastFreed] = pSegment->bFreeList;
        pSegment->bFreeList = (uint8_t)uFirstFreed;

        // update the free count for this chain
        pSegment->rgFreeCount[uType] -= (uRemoved * HANDLE_HANDLES_PER_BLOCK);

        // mark for a resort - the free list (and soon allocation chains) may be out of order
        pSegment->fResortChains = TRUE;

        // if we removed blocks that had user data then we need to reclaim those too
        if (fCleanupUserData)
            SegmentRemoveFreeBlocks(pSegment, HNDTYPE_INTERNAL_DATABLOCK, NULL);
    }
}


/*
 * SegmentInsertBlockFromFreeListWorker
 *
 * Inserts a block into a block list within a segment.  Blocks are obtained from the
 * segment's free list.  Returns the index of the block inserted, or BLOCK_INVALID
 * if no blocks were avaliable.
 *
 * This routine is the core implementation for SegmentInsertBlockFromFreeList.
 *
 */
uint32_t SegmentInsertBlockFromFreeListWorker(TableSegment *pSegment, uint32_t uType, BOOL fUpdateHint)
{
    WRAPPER_NO_CONTRACT;

    /*
        NOTHROW
        GC_NOTRIGGER;
        MODE_ANY;
    */
    

    // fetch the next block from the free list
    uint8_t uBlock = pSegment->bFreeList;

    // if we got the terminator then there are no more blocks
    if (uBlock != BLOCK_INVALID)
    {
        // are we eating out of the last empty range of blocks?
        if (uBlock >= pSegment->bEmptyLine)
        {
            // get the current commit line
            uint32_t uCommitLine = pSegment->bCommitLine;

            // if this block is uncommitted then commit some memory now
            if (uBlock >= uCommitLine)
            {
                // figure out where to commit next
                void * pvCommit = pSegment->rgValue + (uCommitLine * HANDLE_HANDLES_PER_BLOCK);

                // we should commit one more page of handles
                size_t dwCommit = OS_PAGE_SIZE;

                // commit the memory
                if (!GCToOSInterface::VirtualCommit(pvCommit, dwCommit))
                    return BLOCK_INVALID;

                // use the previous commit line as the new decommit line
                pSegment->bDecommitLine = (uint8_t)uCommitLine;

                // adjust the commit line by the number of blocks we commited
                pSegment->bCommitLine = (uint8_t)(uCommitLine + (dwCommit / HANDLE_BYTES_PER_BLOCK));
            }

            // update our empty line
            pSegment->bEmptyLine = uBlock + 1;
        }

        // unlink our block from the free list
        pSegment->bFreeList = pSegment->rgAllocation[uBlock];

        // link our block into the specified chain
        uint32_t uOldTail = pSegment->rgTail[uType];
        if (uOldTail == BLOCK_INVALID)
        {
            // first block, set as head and link to itself
            pSegment->rgAllocation[uBlock] = (uint8_t)uBlock;

            // there are no other blocks - update the hint anyway
            fUpdateHint = TRUE;
        }
        else
        {
            // not first block - link circularly
            pSegment->rgAllocation[uBlock] = pSegment->rgAllocation[uOldTail];
            pSegment->rgAllocation[uOldTail] = (uint8_t)uBlock;
        
            // chain may need resorting depending on what we added
            pSegment->fResortChains = TRUE;
        }

        // mark this block with the type we're using it for
        pSegment->rgBlockType[uBlock] = (uint8_t)uType;

        // update the chain tail
        pSegment->rgTail[uType] = (uint8_t)uBlock;

        // if we are supposed to update the hint, then point it at the new block
        if (fUpdateHint)
            pSegment->rgHint[uType] = (uint8_t)uBlock;

        // increment the chain's free count to reflect the additional block
        pSegment->rgFreeCount[uType] += HANDLE_HANDLES_PER_BLOCK;
    }

    // all done
    return uBlock;
}


/*
 * SegmentInsertBlockFromFreeList
 *
 * Inserts a block into a block list within a segment.  Blocks are obtained from the
 * segment's free list.  Returns the index of the block inserted, or BLOCK_INVALID
 * if no blocks were avaliable.
 *
 * This routine does the work of securing a parallel user data block if required.
 *
 */
uint32_t SegmentInsertBlockFromFreeList(TableSegment *pSegment, uint32_t uType, BOOL fUpdateHint)
{
    LIMITED_METHOD_CONTRACT;

    /*
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    */
    
    uint32_t uBlock, uData = 0;

    // does this block type require user data?
    BOOL fUserData = TypeHasUserData(pSegment->pHandleTable, uType);

    // if we need user data then we need to make sure it can go in the same segment as the handles
    if (fUserData)
    {
        // if we can't also fit the user data in this segment then bail
        uBlock = pSegment->bFreeList;
        if ((uBlock == BLOCK_INVALID) || (pSegment->rgAllocation[uBlock] == BLOCK_INVALID))
            return BLOCK_INVALID;

        // allocate our user data block (we do it in this order so that free order is nicer)
        uData = SegmentInsertBlockFromFreeListWorker(pSegment, HNDTYPE_INTERNAL_DATABLOCK, FALSE);
    }

    // now allocate the requested block
    uBlock = SegmentInsertBlockFromFreeListWorker(pSegment, uType, fUpdateHint);

    // should we have a block for user data too?
    if (fUserData)
    {
        // did we get them both?
        if ((uBlock != BLOCK_INVALID) && (uData != BLOCK_INVALID))
        {
            // link the data block to the requested block
            pSegment->rgUserData[uBlock] = (uint8_t)uData;

            // no handles are ever allocated out of a data block
            // lock the block so it won't be reclaimed accidentally
            BlockLock(pSegment, uData);
        }
        else
        {
            // NOTE: We pre-screened that the blocks exist above, so we should only
            //       get here under heavy load when a MEM_COMMIT operation fails.

            // if the type block allocation succeeded then scavenge the type block list
            if (uBlock != BLOCK_INVALID)
                SegmentRemoveFreeBlocks(pSegment, uType, NULL);

            // if the user data allocation succeeded then scavenge the user data list
            if (uData != BLOCK_INVALID)
                SegmentRemoveFreeBlocks(pSegment, HNDTYPE_INTERNAL_DATABLOCK, NULL);

            // make sure we return failure
            uBlock = BLOCK_INVALID;
        }
    }

    // all done
    return uBlock;
}


/*
 * SegmentResortChains
 *
 * Sorts the block chains for optimal scanning order.
 * Sorts the free list to combat fragmentation.
 *
 */
void SegmentResortChains(TableSegment *pSegment)
{
    WRAPPER_NO_CONTRACT;

    // clear the sort flag for this segment
    pSegment->fResortChains = FALSE;
    BOOL fScavengingOccurred = FALSE;

    // first, do we need to scavenge any blocks?
    if (pSegment->fNeedsScavenging)
    {
        // clear the scavenge flag
        pSegment->fNeedsScavenging = FALSE;

        fScavengingOccurred = TRUE;

        // we may need to explicitly scan the user data chain too
        BOOL fCleanupUserData = FALSE;

        // fetch the empty line for this segment
        uint32_t uLast = pSegment->bEmptyLine;

        // loop over all active blocks, scavenging the empty ones as we go
        for (uint32_t uBlock = 0; uBlock < uLast; uBlock++)
        {
            // fetch the block type of this block
            uint32_t uType = pSegment->rgBlockType[uBlock];

            // only process public block types - we handle data blocks separately
            if (uType < HANDLE_MAX_PUBLIC_TYPES)
            {
#ifdef HANDLE_OPTIMIZE_FOR_64_HANDLE_BLOCKS
                // determine whether this block is empty
                if (((uint64_t*)pSegment->rgFreeMask)[uBlock] == UI64(0xFFFFFFFFFFFFFFFF))
#else
                // assume this block is empty until we know otherwise
                BOOL fEmpty = TRUE;
    
                // get the first mask for this block
                uint32_t *pdwMask     = pSegment->rgFreeMask + (uBlock * HANDLE_MASKS_PER_BLOCK);
                uint32_t *pdwMaskLast = pdwMask              + HANDLE_MASKS_PER_BLOCK;

                // loop through the masks until we've processed them all or we've found handles
                do
                {
                    // is this mask empty?
                    if (*pdwMask != MASK_EMPTY)
                    {
                        // nope - this block still has handles in it
                        fEmpty = FALSE;
                        break;
                    }

                    // on to the next mask
                    pdwMask++;

                } while (pdwMask < pdwMaskLast);

                // is this block empty?
                if (fEmpty)
#endif
                {
                    // is the block unlocked?
                    if (!BlockIsLocked(pSegment, uBlock))
                    {
                        // safe to free - did it have user data associated?
                        uint32_t uData = pSegment->rgUserData[uBlock];
                        if (uData != BLOCK_INVALID)
                        {
                            // data blocks are 'empty' so we keep them locked
                            // unlock the block so it can be reclaimed below
                            BlockUnlock(pSegment, uData);

                            // unlink the data block from the handle block
                            pSegment->rgUserData[uBlock] = BLOCK_INVALID;

                            // remember that we need to scavenge the data block chain
                            fCleanupUserData = TRUE;
                        }

                        // mark the block as free
                        pSegment->rgBlockType[uBlock] = TYPE_INVALID;

                        // fix up the free count for the block's type
                        pSegment->rgFreeCount[uType] -= HANDLE_HANDLES_PER_BLOCK;

                        // N.B. we don't update the list linkages here since they are rebuilt below
                    }
                }
            }
        }

        // if we have to clean up user data then do that now
        if (fCleanupUserData)
            SegmentRemoveFreeBlocks(pSegment, HNDTYPE_INTERNAL_DATABLOCK, NULL);
    }

    // keep some per-chain data
    uint8_t rgChainCurr[HANDLE_MAX_INTERNAL_TYPES];
    uint8_t rgChainHigh[HANDLE_MAX_INTERNAL_TYPES];
    uint8_t bChainFree = BLOCK_INVALID;
    uint32_t uEmptyLine = BLOCK_INVALID;
    BOOL fContiguousWithFreeList = TRUE;

    // preinit the chain data to no blocks
    uint32_t uType;
    for (uType = 0; uType < HANDLE_MAX_INTERNAL_TYPES; uType++)
        rgChainHigh[uType] = rgChainCurr[uType] = BLOCK_INVALID;

    // scan back through the block types
    uint8_t uBlock = HANDLE_BLOCKS_PER_SEGMENT;
    while (uBlock > 0)
    {
        // decrement the block index
        uBlock--;

        // fetch the type for this block
        uType = pSegment->rgBlockType[uBlock];

        // is this block allocated?
        if (uType != TYPE_INVALID)
        {
            // looks allocated
            fContiguousWithFreeList = FALSE;
             
            // hope the segment's not corrupt :)
            _ASSERTE(uType < HANDLE_MAX_INTERNAL_TYPES);

            // remember the first block we see for each type
            if (rgChainHigh[uType] == BLOCK_INVALID)
                rgChainHigh[uType] = uBlock;

            // link this block to the last one we saw of this type
            pSegment->rgAllocation[uBlock] = rgChainCurr[uType];

            // remember this block in type chain
            rgChainCurr[uType] = (uint8_t)uBlock;
        }
        else
        {
            // block is free - is it also contiguous with the free list?
            if (fContiguousWithFreeList)
                uEmptyLine = uBlock;

            // link this block to the last one in the free chain
            pSegment->rgAllocation[uBlock] = bChainFree;

            // add this block to the free list
            bChainFree = (uint8_t)uBlock;
        }
    }

    // now close the loops and store the tails
    for (uType = 0; uType < HANDLE_MAX_INTERNAL_TYPES; uType++)
    {
        // get the first block in the list
        uint8_t bBlock = rgChainCurr[uType];

        // if there is a list then make it circular and save it
        if (bBlock != BLOCK_INVALID)
        {
            // highest block we saw becomes tail
            uint32_t uTail = rgChainHigh[uType];

            // store tail in segment
            pSegment->rgTail[uType] = (uint8_t)uTail;

            // link tail to head
            pSegment->rgAllocation[uTail] = bBlock;

            // If we scavenged blocks above then we might have left the hint pointing at the free chain. Reset
            // it back into this chain if so (the choice of block is arbitrary, this case is very rare).
            if (pSegment->rgBlockType[pSegment->rgHint[uType]] != uType)
                pSegment->rgHint[uType] = bBlock;
        }
        else
        {
            // No blocks of this type were found in the rgBlockType array, meaning either there were no
            // such blocks on entry to this function (in which case the associated tail is guaranteed
            // to already be marked invalid) OR that there were blocks but all of them were reclaimed
            // by the scavenging logic above (in which case the associated tail is guaranteed to point
            // to one of the scavenged blocks). In the latter case, the tail is currently "stale"
            // and therefore needs to be manually updated.
            if (pSegment->rgTail[uType] != BLOCK_INVALID)
            {
                _ASSERTE(fScavengingOccurred);
                pSegment->rgTail[uType] = BLOCK_INVALID;
                pSegment->rgHint[uType] = BLOCK_INVALID;
            }
        }
    }

    // store the new free list head
    pSegment->bFreeList = bChainFree;

    // compute the new empty line
    if (uEmptyLine > HANDLE_BLOCKS_PER_SEGMENT)
        uEmptyLine = HANDLE_BLOCKS_PER_SEGMENT;

    // store the updated empty line
    pSegment->bEmptyLine = (uint8_t)uEmptyLine;
}

/*
 * DoesSegmentNeedsToTrimExcessPages
 *
 * Checks to see if any pages can be decommitted from the segment
 *
 */
BOOL DoesSegmentNeedsToTrimExcessPages(TableSegment *pSegment)
{
    WRAPPER_NO_CONTRACT;

    // fetch the empty and decommit lines
    uint32_t uEmptyLine    = pSegment->bEmptyLine;
    uint32_t uDecommitLine = pSegment->bDecommitLine;

    // check to see if we can decommit some handles
    // NOTE: we use '<' here to avoid playing ping-pong on page boundaries
    //       this is OK since the zero case is handled elsewhere (segment gets freed)
    if (uEmptyLine < uDecommitLine)
    {
        // derive some useful info about the page size
        uintptr_t dwPageRound = OS_PAGE_SIZE - 1;
        uintptr_t dwPageMask  = ~dwPageRound;

        // compute the address corresponding to the empty line
        uintptr_t dwLo = (uintptr_t)pSegment->rgValue + (uEmptyLine  * HANDLE_BYTES_PER_BLOCK);

        // adjust the empty line address to the start of the nearest whole empty page
        dwLo = (dwLo + dwPageRound) & dwPageMask;

        // compute the address corresponding to the old commit line
        uintptr_t dwHi = (uintptr_t)pSegment->rgValue + ((uint32_t)pSegment->bCommitLine * HANDLE_BYTES_PER_BLOCK);

        // is there anything to decommit?
        if (dwHi > dwLo)
        {
            return TRUE;
        }
    }

    return FALSE;
}


/*
 * SegmentTrimExcessPages
 *
 * Checks to see if any pages can be decommitted from the segment.
 * In case there any unused pages it goes and decommits them.
 *
 */
void SegmentTrimExcessPages(TableSegment *pSegment)
{
    WRAPPER_NO_CONTRACT;

    // fetch the empty and decommit lines
    uint32_t uEmptyLine    = pSegment->bEmptyLine;
    uint32_t uDecommitLine = pSegment->bDecommitLine;

    // check to see if we can decommit some handles
    // NOTE: we use '<' here to avoid playing ping-pong on page boundaries
    //       this is OK since the zero case is handled elsewhere (segment gets freed)
    if (uEmptyLine < uDecommitLine)
    {
        // derive some useful info about the page size
        uintptr_t dwPageRound = (uintptr_t)OS_PAGE_SIZE - 1;
        uintptr_t dwPageMask  = ~dwPageRound;

        // compute the address corresponding to the empty line
        uintptr_t dwLo = (uintptr_t)pSegment->rgValue + (uEmptyLine  * HANDLE_BYTES_PER_BLOCK);

        // adjust the empty line address to the start of the nearest whole empty page
        dwLo = (dwLo + dwPageRound) & dwPageMask;

        // compute the address corresponding to the old commit line
        uintptr_t dwHi = (uintptr_t)pSegment->rgValue + ((uint32_t)pSegment->bCommitLine * HANDLE_BYTES_PER_BLOCK);

        // is there anything to decommit?
        if (dwHi > dwLo)
        {
            // decommit the memory
            GCToOSInterface::VirtualDecommit((void *)dwLo, dwHi - dwLo);

            // update the commit line
            pSegment->bCommitLine = (uint8_t)((dwLo - (size_t)pSegment->rgValue) / HANDLE_BYTES_PER_BLOCK);

            // compute the address for the new decommit line
            size_t dwDecommitAddr = dwLo - OS_PAGE_SIZE;

            // assume a decommit line of zero until we know otherwise
            uDecommitLine = 0;

            // if the address is within the handle area then compute the line from the address
            if (dwDecommitAddr > (size_t)pSegment->rgValue)
                uDecommitLine = (uint32_t)((dwDecommitAddr - (size_t)pSegment->rgValue) / HANDLE_BYTES_PER_BLOCK);

            // update the decommit line
            pSegment->bDecommitLine = (uint8_t)uDecommitLine;
        }
    }
}


/*
 * BlockAllocHandlesInMask
 *
 * Attempts to allocate the requested number of handes of the specified type,
 * from the specified mask of the specified handle block.
 *
 * Returns the number of available handles actually allocated.
 *
 */
uint32_t BlockAllocHandlesInMask(TableSegment *pSegment, uint32_t uBlock,
                             uint32_t *pdwMask, uint32_t uHandleMaskDisplacement,
                             OBJECTHANDLE *pHandleBase, uint32_t uCount)
{
    LIMITED_METHOD_CONTRACT;
    UNREFERENCED_PARAMETER(uBlock);

    // keep track of how many handles we have left to allocate
    uint32_t uRemain = uCount;

    // fetch the free mask into a local so we can play with it
    uint32_t dwFree = *pdwMask;

    // keep track of our displacement within the mask
    uint32_t uByteDisplacement = 0;

    // examine the mask byte by byte for free handles
    do
    {
        // grab the low byte of the mask
        uint32_t dwLowByte = (dwFree & MASK_LOBYTE);

        // are there any free handles here?
        if (dwLowByte)
        {
            // remember which handles we've taken
            uint32_t dwAlloc = 0;

            // loop until we've allocated all the handles we can from here
            do
            {
                // get the index of the next handle
                uint32_t uIndex = c_rgLowBitIndex[dwLowByte];

                // compute the mask for the handle we chose
                dwAlloc |= (1 << uIndex);

                // remove this handle from the mask byte
                dwLowByte &= ~dwAlloc;

                // compute the index of this handle in the segment
                uIndex += uHandleMaskDisplacement + uByteDisplacement;

                // store the allocated handle in the handle array
                *pHandleBase = (OBJECTHANDLE)(pSegment->rgValue + uIndex);

                // adjust our count and array pointer
                uRemain--;
                pHandleBase++;

            } while (dwLowByte && uRemain);

            // shift the allocation mask into position
            dwAlloc <<= uByteDisplacement;

            // update the mask to account for the handles we allocated
            *pdwMask &= ~dwAlloc;
        }

        // on to the next byte in the mask
        dwFree >>= BITS_PER_BYTE;
        uByteDisplacement += BITS_PER_BYTE;

    } while (uRemain && dwFree);

    // return the number of handles we got
    return (uCount - uRemain);

}


/*
 * BlockAllocHandlesInitial
 *
 * Allocates a specified number of handles from a newly committed (empty) block.
 *
 */
uint32_t BlockAllocHandlesInitial(TableSegment *pSegment, uint32_t uType, uint32_t uBlock,
                                  OBJECTHANDLE *pHandleBase, uint32_t uCount)
{
    LIMITED_METHOD_CONTRACT;
    UNREFERENCED_PARAMETER(uType);

    // sanity check
    _ASSERTE(uCount);

    // validate the number of handles we were asked to allocate
    if (uCount > HANDLE_HANDLES_PER_BLOCK)
    {
        _ASSERTE(FALSE);
        uCount = HANDLE_HANDLES_PER_BLOCK;
    }

    // keep track of how many handles we have left to mark in masks
    uint32_t uRemain = uCount;

    // get the first mask for this block
    uint32_t *pdwMask = pSegment->rgFreeMask + (uBlock * HANDLE_MASKS_PER_BLOCK);

    // loop through the masks, zeroing the appropriate free bits
    do
    {
        // this is a brand new block - all masks we encounter should be totally free
        _ASSERTE(*pdwMask == MASK_EMPTY);

        // pick an initial guess at the number to allocate
        uint32_t uAlloc = uRemain;

        // compute the default mask based on that count
        uint32_t dwNewMask;
        // are we allocating all of them?
        if (uAlloc >= HANDLE_HANDLES_PER_MASK)
        {
            dwNewMask = MASK_FULL; // avoid unpredictable shift
            uAlloc = HANDLE_HANDLES_PER_MASK;
        }
        else
        {
            dwNewMask = (MASK_EMPTY << uAlloc);
        }

        // set the free mask
        *pdwMask = dwNewMask;

        // update our count and mask pointer
        uRemain -= uAlloc;
        pdwMask++;

    } while (uRemain);

    // compute the bounds for allocation so we can copy the handles
    _UNCHECKED_OBJECTREF *pValue = pSegment->rgValue + (uBlock * HANDLE_HANDLES_PER_BLOCK);
    _UNCHECKED_OBJECTREF *pLast  = pValue + uCount;

    // loop through filling in the output array with handles
    do
    {
        // store the next handle in the next array slot
        *pHandleBase = (OBJECTHANDLE)pValue;

        // increment our source and destination
        pValue++;
        pHandleBase++;

    } while (pValue < pLast);

    // return the number of handles we allocated
    return uCount;
}


/*
 * BlockAllocHandles
 *
 * Attempts to allocate the requested number of handes of the specified type,
 * from the specified handle block.
 *
 * Returns the number of available handles actually allocated.
 *
 */
uint32_t BlockAllocHandles(TableSegment *pSegment, uint32_t uBlock, OBJECTHANDLE *pHandleBase, uint32_t uCount)
{
    WRAPPER_NO_CONTRACT;

    /*
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    */
    
    // keep track of how many handles we have left to allocate
    uint32_t uRemain = uCount;

    // set up our loop and limit mask pointers
    uint32_t *pdwMask     = pSegment->rgFreeMask + (uBlock * HANDLE_MASKS_PER_BLOCK);
    uint32_t *pdwMaskLast = pdwMask + HANDLE_MASKS_PER_BLOCK;

    // keep track of the handle displacement for the mask we're scanning
    uint32_t uDisplacement = uBlock * HANDLE_HANDLES_PER_BLOCK;

    // loop through all the masks, allocating handles as we go
    do
    {
        // if this mask indicates free handles then grab them
        if (*pdwMask)
        {
            // allocate as many handles as we need from this mask
            uint32_t uSatisfied = BlockAllocHandlesInMask(pSegment, uBlock, pdwMask, uDisplacement, pHandleBase, uRemain);

            // adjust our count and array pointer
            uRemain     -= uSatisfied;
            pHandleBase += uSatisfied;
    
            // if there are no remaining slots to be filled then we are done
            if (!uRemain)
                break;
        }

        // on to the next mask
        pdwMask++;
        uDisplacement += HANDLE_HANDLES_PER_MASK;

    } while (pdwMask < pdwMaskLast);

    // return the number of handles we got
    return (uCount - uRemain);
}


/*
 * SegmentAllocHandlesFromTypeChain
 *
 * Attempts to allocate the requested number of handes of the specified type,
 * from the specified segment's block chain for the specified type.  This routine
 * ONLY scavenges existing blocks in the type chain.  No new blocks are committed.
 *
 * Returns the number of available handles actually allocated.
 *
 */
uint32_t SegmentAllocHandlesFromTypeChain(TableSegment *pSegment, uint32_t uType, OBJECTHANDLE *pHandleBase, uint32_t uCount)
{
    WRAPPER_NO_CONTRACT;

    /*
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    */
    
    // fetch the number of handles available in this chain
    uint32_t uAvail = pSegment->rgFreeCount[uType];

    // is the available count greater than the requested count?
    if (uAvail > uCount)
    {
        // yes - all requested handles are available
        uAvail = uCount;
    }
    else
    {
        // no - we can only satisfy some of the request
        uCount = uAvail;
    }

    // did we find that any handles are available?
    if (uAvail)
    {
        // yes - fetch the head of the block chain and set up a loop limit
        uint32_t uBlock = pSegment->rgHint[uType];
        uint32_t uLast = uBlock;

        // loop until we have found all handles known to be available
        for (;;)
        {
            // try to allocate handles from the current block
            uint32_t uSatisfied = BlockAllocHandles(pSegment, uBlock, pHandleBase, uAvail);

            // did we get everything we needed?
            if (uSatisfied == uAvail)
            {
                // yes - update the hint for this type chain and get out
                pSegment->rgHint[uType] = (uint8_t)uBlock;
                break;
            }

            // adjust our count and array pointer
            uAvail      -= uSatisfied;
            pHandleBase += uSatisfied;

            // fetch the next block in the type chain
            uBlock = pSegment->rgAllocation[uBlock];

            // are we out of blocks?
            if (uBlock == uLast)
            {
                // free count is corrupt
                _ASSERTE(FALSE);

                // avoid making the problem any worse
                uCount -= uAvail;
                break;
            }
        }

        // update the free count
        pSegment->rgFreeCount[uType] -= uCount;
    }

    // return the number of handles we got
    return uCount;
}


/*
 * SegmentAllocHandlesFromFreeList
 *
 * Attempts to allocate the requested number of handes of the specified type,
 * by committing blocks from the free list to that type's type chain.
 *
 * Returns the number of available handles actually allocated.
 *
 */
uint32_t SegmentAllocHandlesFromFreeList(TableSegment *pSegment, uint32_t uType, OBJECTHANDLE *pHandleBase, uint32_t uCount)
{
    LIMITED_METHOD_CONTRACT;

    /*
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    */
    
    // keep track of how many handles we have left to allocate
    uint32_t uRemain = uCount;

    // loop allocating handles until we are done or we run out of free blocks
    do
    {
        // start off assuming we can allocate all the handles
        uint32_t uAlloc = uRemain;

        // we can only get a block-full of handles at a time
        if (uAlloc > HANDLE_HANDLES_PER_BLOCK)
            uAlloc = HANDLE_HANDLES_PER_BLOCK;

        // try to get a block from the free list
        uint32_t uBlock = SegmentInsertBlockFromFreeList(pSegment, uType, (uRemain == uCount));

        // if there are no free blocks left then we are done
        if (uBlock == BLOCK_INVALID)
            break;

        // initialize the block by allocating the required handles into the array
        uAlloc = BlockAllocHandlesInitial(pSegment, uType, uBlock, pHandleBase, uAlloc);

        // adjust our count and array pointer
        uRemain     -= uAlloc;
        pHandleBase += uAlloc;

    } while (uRemain);

    // compute the number of handles we took
    uCount -= uRemain;

    // update the free count by the number of handles we took
    pSegment->rgFreeCount[uType] -= uCount;

    // return the number of handles we got
    return uCount;
}


/*
 * SegmentAllocHandles
 *
 * Attempts to allocate the requested number of handes of the specified type,
 * from the specified segment.
 *
 * Returns the number of available handles actually allocated.
 *
 */
uint32_t SegmentAllocHandles(TableSegment *pSegment, uint32_t uType, OBJECTHANDLE *pHandleBase, uint32_t uCount)
{
    LIMITED_METHOD_CONTRACT;

    /*
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    */
    
    // first try to get some handles from the existing type chain
    uint32_t uSatisfied = SegmentAllocHandlesFromTypeChain(pSegment, uType, pHandleBase, uCount);

    // if there are still slots to be filled then we need to commit more blocks to the type chain
    if (uSatisfied < uCount)
    {
        // adjust our count and array pointer
        uCount      -= uSatisfied;
        pHandleBase += uSatisfied;

        // get remaining handles by committing blocks from the free list
        uSatisfied += SegmentAllocHandlesFromFreeList(pSegment, uType, pHandleBase, uCount);
    }

    // return the number of handles we got
    return uSatisfied;
}


/*
 * TableAllocBulkHandles
 *
 * Attempts to allocate the requested number of handes of the specified type.
 *
 * Returns the number of handles that were actually allocated.  This is always
 * the same as the number of handles requested except in out-of-memory conditions,
 * in which case it is the number of handles that were successfully allocated.
 *
 */
uint32_t TableAllocBulkHandles(HandleTable *pTable, uint32_t uType, OBJECTHANDLE *pHandleBase, uint32_t uCount)
{
    WRAPPER_NO_CONTRACT;

    /*
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    */
    
    // keep track of how many handles we have left to allocate
    uint32_t uRemain = uCount;

    // start with the first segment and loop until we are done
    TableSegment *pSegment = pTable->pSegmentList;

    uint8_t bLastSequence = 0;
    BOOL fNewSegment = FALSE;

    for (;;)
    {
        // get some handles from the current segment
        uint32_t uSatisfied = SegmentAllocHandles(pSegment, uType, pHandleBase, uRemain);

        // adjust our count and array pointer
        uRemain     -= uSatisfied;
        pHandleBase += uSatisfied;

        // if there are no remaining slots to be filled then we are done
        if (!uRemain)
            break;

        // fetch the next segment in the chain.
        TableSegment *pNextSegment = NULL;
        
        if (!fNewSegment)
        {
            pNextSegment = pSegment->pNextSegment;
            if (!pNextSegment)
            {
                bLastSequence = pSegment->bSequence;
                fNewSegment = TRUE;
            }
        }

        // if are no more segments then allocate another
        if (fNewSegment)
        {
            // ok if this fails then we're out of luck
            pNextSegment = SegmentAlloc(pTable);
            if (!pNextSegment)
            {
                // we ran out of memory allocating a new segment.
                // this may not be catastrophic - if there are still some
                // handles in the cache then some allocations may succeed.
                break;
            }

            // set up the correct sequence number for the new segment
            pNextSegment->bSequence = (uint8_t)(((uint32_t)bLastSequence + 1) % 0x100);
            bLastSequence = pNextSegment->bSequence;

            // link the new segment into the list by the order of segment address
            TableSegment* pWalk = pTable->pSegmentList;
            if ((uintptr_t)pNextSegment < (uintptr_t)pWalk)
            {
                pNextSegment->pNextSegment = pWalk;
                pTable->pSegmentList = pNextSegment;
            }
            else
            {
                while (pWalk)
                {
                    if (pWalk->pNextSegment == NULL)
                    {
                        pWalk->pNextSegment = pNextSegment;
                        break;
                    }
                    else if ((uintptr_t)pWalk->pNextSegment > (uintptr_t)pNextSegment)
                    {
                        pNextSegment->pNextSegment = pWalk->pNextSegment;
                        pWalk->pNextSegment = pNextSegment;
                        break;
                    }
                    pWalk = pWalk->pNextSegment;
                }
            }
        }

        // try again with new segment
        pSegment = pNextSegment;
    }

    // compute the number of handles we actually got
    uint32_t uAllocated = (uCount - uRemain);

    // update the count of handles marked as "used"
    pTable->dwCount += uAllocated;

    // return the number of handles we actually got
    return uAllocated;
}


/*
 * BlockFreeHandlesInMask
 *
 * Frees some portion of an array of handles of the specified type.
 * The array is scanned forward and handles are freed until a handle
 * from a different mask is encountered.
 *
 * Returns the number of handles that were freed from the front of the array.
 *
 */
uint32_t BlockFreeHandlesInMask(TableSegment *pSegment, uint32_t uBlock, uint32_t uMask, OBJECTHANDLE *pHandleBase, uint32_t uCount,
                                uintptr_t *pUserData, uint32_t *puActualFreed, BOOL *pfAllMasksFree)
{
    LIMITED_METHOD_CONTRACT;

    // keep track of how many handles we have left to free
    uint32_t uRemain = uCount;

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:6305) // "This code deals with a bit vector mapped piece of code, so there is no mismatch between sizeof and countof"
#endif

    // if this block has user data, convert the pointer to be mask-relative
    if (pUserData)
        pUserData += (uMask * HANDLE_HANDLES_PER_MASK);

    // convert our mask index to be segment-relative
    uMask += (uBlock * HANDLE_MASKS_PER_BLOCK);

    // compute the handle bounds for our mask
    OBJECTHANDLE firstHandle = (OBJECTHANDLE)(pSegment->rgValue + (uMask * HANDLE_HANDLES_PER_MASK));
    OBJECTHANDLE lastHandle  = (OBJECTHANDLE)((_UNCHECKED_OBJECTREF *)firstHandle + HANDLE_HANDLES_PER_MASK);

#ifdef _PREFAST_ 
#pragma warning(pop)
#endif

    // keep a local copy of the free mask to update as we free handles
    uint32_t dwFreeMask = pSegment->rgFreeMask[uMask];

    // keep track of how many bogus frees we are asked to do
    uint32_t uBogus = 0;

    // loop freeing handles until we encounter one outside our block or there are none left
    do
    {
        // fetch the next handle in the array
        OBJECTHANDLE handle = *pHandleBase;

        // if the handle is outside our segment then we are done
        if ((handle < firstHandle) || (handle >= lastHandle))
            break;

        // sanity check - the handle should no longer refer to an object here
        _ASSERTE(HndIsNullOrDestroyedHandle(*(_UNCHECKED_OBJECTREF *)handle));

        // compute the handle index within the mask
        uint32_t uHandle = (uint32_t)(handle - firstHandle);

        // if there is user data then clear the user data for this handle
        if (pUserData)
            pUserData[uHandle] = 0L;

        // compute the mask bit for this handle
        uint32_t dwFreeBit = (1 << uHandle);

        // the handle should not already be free
        if ((dwFreeMask & dwFreeBit) != 0)
        {
            // SOMEONE'S FREEING A HANDLE THAT ISN'T ALLOCATED
            uBogus++;
            _ASSERTE(FALSE);
        }

        // add this handle to the tally of freed handles
        dwFreeMask |= dwFreeBit;

        // adjust our count and array pointer
        uRemain--;
        pHandleBase++;

    } while (uRemain);

    // update the mask to reflect the handles we changed
    pSegment->rgFreeMask[uMask] = dwFreeMask;

    // if not all handles in this mask are free then tell our caller not to check the block
    if (dwFreeMask != MASK_EMPTY)
        *pfAllMasksFree = FALSE;

    // compute the number of handles we processed from the array
    uint32_t uFreed = (uCount - uRemain);

    // tell the caller how many handles we actually freed
    *puActualFreed += (uFreed - uBogus);

    // return the number of handles we actually freed
    return uFreed;
}


/*
 * BlockFreeHandles
 *
 * Frees some portion of an array of handles of the specified type.
 * The array is scanned forward and handles are freed until a handle
 * from a different block is encountered.
 *
 * Returns the number of handles that were freed from the front of the array.
 *
 */
uint32_t BlockFreeHandles(TableSegment *pSegment, uint32_t uBlock, OBJECTHANDLE *pHandleBase, uint32_t uCount,
                          uint32_t *puActualFreed, BOOL *pfScanForFreeBlocks)
{
    WRAPPER_NO_CONTRACT;

    /*
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    */
    
    // keep track of how many handles we have left to free
    uint32_t uRemain = uCount;

    // fetch the user data for this block, if any
    uintptr_t *pBlockUserData = BlockFetchUserDataPointer(pSegment, uBlock, FALSE);

    // compute the handle bounds for our block
    OBJECTHANDLE firstHandle = (OBJECTHANDLE)(pSegment->rgValue + (uBlock * HANDLE_HANDLES_PER_BLOCK));
    OBJECTHANDLE lastHandle  = (OBJECTHANDLE)((_UNCHECKED_OBJECTREF *)firstHandle + HANDLE_HANDLES_PER_BLOCK);

    // this variable will only stay TRUE if all masks we touch end up in the free state
    BOOL fAllMasksWeTouchedAreFree = TRUE;

    // loop freeing handles until we encounter one outside our block or there are none left
    do
    {
        // fetch the next handle in the array
        OBJECTHANDLE handle = *pHandleBase;

        // if the handle is outside our segment then we are done
        if ((handle < firstHandle) || (handle >= lastHandle))
            break;

        // compute the mask that this handle resides in
        uint32_t uMask = (uint32_t)((handle - firstHandle) / HANDLE_HANDLES_PER_MASK);

        // free as many handles as this mask owns from the front of the array
        uint32_t uFreed = BlockFreeHandlesInMask(pSegment, uBlock, uMask, pHandleBase, uRemain,
                                             pBlockUserData, puActualFreed, &fAllMasksWeTouchedAreFree);

        // adjust our count and array pointer
        uRemain     -= uFreed;
        pHandleBase += uFreed;

    } while (uRemain);

    // are all masks we touched free?
    if (fAllMasksWeTouchedAreFree)
    {
        // is the block unlocked?
        // NOTE: This check is incorrect and defeats the intended purpose of scavenging. If the
        // current block is locked and has just been emptied, then it cannot be removed right now
        // and therefore will nominally need to be scavenged. The only code that triggers
        // scavenging is in SegmentRemoveFreeBlocks, and setting the flag is the only way to
        // trigger a call into SegmentRemoveFreeBlocks call. As a result, by NOT setting the flag
        // this code is generally PREVENTING scavenging in exactly the cases where scavenging is
        // needed. The code is not being changed because it has always been this way and scavenging
        // itself generally has extremely low value.
        if (!BlockIsLocked(pSegment, uBlock))
        {
            // tell the caller it might be a good idea to scan for free blocks
            *pfScanForFreeBlocks = TRUE;
        }
    }

    // return the number of handles we actually freed
    return (uCount - uRemain);
}


/*
 * SegmentFreeHandles
 *
 * Frees some portion of an array of handles of the specified type.
 * The array is scanned forward and handles are freed until a handle
 * from a different segment is encountered.
 *
 * Returns the number of handles that were freed from the front of the array.
 *
 */
uint32_t SegmentFreeHandles(TableSegment *pSegment, uint32_t uType, OBJECTHANDLE *pHandleBase, uint32_t uCount)
{
    WRAPPER_NO_CONTRACT;

    /*
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    */
    
    // keep track of how many handles we have left to free
    uint32_t uRemain = uCount;

    // compute the handle bounds for our segment
    OBJECTHANDLE firstHandle = (OBJECTHANDLE)pSegment->rgValue;
    OBJECTHANDLE lastHandle  = (OBJECTHANDLE)((_UNCHECKED_OBJECTREF *)firstHandle + HANDLE_HANDLES_PER_SEGMENT);

    // the per-block free routines will set this if there is a chance some blocks went free
    BOOL fScanForFreeBlocks = FALSE;

    // track the number of handles we actually free
    uint32_t uActualFreed = 0;

    // loop freeing handles until we encounter one outside our segment or there are none left
    do
    {
        // fetch the next handle in the array
        OBJECTHANDLE handle = *pHandleBase;

        // if the handle is outside our segment then we are done
        if ((handle < firstHandle) || (handle >= lastHandle))
            break;

        // compute the block that this handle resides in
        uint32_t uBlock = (uint32_t)(((uintptr_t)handle - (uintptr_t)firstHandle) / (HANDLE_SIZE * HANDLE_HANDLES_PER_BLOCK));

        // sanity check that this block is the type we expect to be freeing
        _ASSERTE(pSegment->rgBlockType[uBlock] == uType);

        // free as many handles as this block owns from the front of the array
        uint32_t uFreed = BlockFreeHandles(pSegment, uBlock, pHandleBase, uRemain, &uActualFreed, &fScanForFreeBlocks);

        // adjust our count and array pointer
        uRemain     -= uFreed;
        pHandleBase += uFreed;

    } while (uRemain);

    // compute the number of handles we actually freed
    uint32_t uFreed = (uCount - uRemain);

    // update the free count
    pSegment->rgFreeCount[uType] += uActualFreed;

    // if we saw blocks that may have gone totally free then do a free scan
    if (fScanForFreeBlocks)
    {
        // assume we no scavenging is required
        BOOL fNeedsScavenging = FALSE;

        // try to remove any free blocks we may have created
        SegmentRemoveFreeBlocks(pSegment, uType, &fNeedsScavenging);

        // did SegmentRemoveFreeBlocks have to skip over any free blocks?
        if (fNeedsScavenging)
        {
            // yup, arrange to scavenge them later
            pSegment->fResortChains    = TRUE;
            pSegment->fNeedsScavenging = TRUE;
        }
    }

    // return the total number of handles we freed
    return uFreed;
}


/*
 * TableFreeBulkPreparedHandles
 *
 * Frees an array of handles of the specified type.
 *
 * This routine is optimized for a sorted array of handles but will accept any order.
 *
 */
void TableFreeBulkPreparedHandles(HandleTable *pTable, uint32_t uType, OBJECTHANDLE *pHandleBase, uint32_t uCount)
{
    //Update the count of handles marked as "used"
    pTable->dwCount -= uCount;

    WRAPPER_NO_CONTRACT;

    /*
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    */
    
    // loop until all handles are freed
    do
    {
        // get the segment for the first handle
        TableSegment * pSegment = (TableSegment *)HandleFetchSegmentPointer(*pHandleBase);

        // sanity
        _ASSERTE(pSegment->pHandleTable == pTable);

        // free as many handles as this segment owns from the front of the array
        uint32_t uFreed = SegmentFreeHandles(pSegment, uType, pHandleBase, uCount);

        // adjust our count and array pointer
        uCount      -= uFreed;
        pHandleBase += uFreed;

    } while (uCount);
}


/*
 * TableFreeBulkUnpreparedHandlesWorker
 *
 * Frees an array of handles of the specified type by preparing them and calling TableFreeBulkPreparedHandles.
 * Uses the supplied scratch buffer to prepare the handles.
 *
 */
void TableFreeBulkUnpreparedHandlesWorker(HandleTable *pTable, uint32_t uType, const OBJECTHANDLE *pHandles, uint32_t uCount,
                                          OBJECTHANDLE *pScratchBuffer)
{
    WRAPPER_NO_CONTRACT;

    // copy the handles into the destination buffer
    memcpy(pScratchBuffer, pHandles, uCount * sizeof(OBJECTHANDLE));
 
    // sort them for optimal free order
    QuickSort((uintptr_t *)pScratchBuffer, 0, uCount - 1, CompareHandlesByFreeOrder);
 
    // make sure the handles are zeroed too
    ZeroHandles(pScratchBuffer, uCount);
 
    // prepare and free these handles
    TableFreeBulkPreparedHandles(pTable, uType, pScratchBuffer, uCount);
}
 

/*
 * TableFreeBulkUnpreparedHandles
 *
 * Frees an array of handles of the specified type by preparing them and calling
 * TableFreeBulkPreparedHandlesWorker one or more times.
 *
 */
void TableFreeBulkUnpreparedHandles(HandleTable *pTable, uint32_t uType, const OBJECTHANDLE *pHandles, uint32_t uCount)
{
    CONTRACTL
    {
        NOTHROW;
        WRAPPER(GC_TRIGGERS);
    }
    CONTRACTL_END;

    // preparation / free buffer
    OBJECTHANDLE rgStackHandles[HANDLE_HANDLES_PER_BLOCK];
    OBJECTHANDLE *pScratchBuffer  = rgStackHandles;
    OBJECTHANDLE *pLargeScratchBuffer  = NULL;
    uint32_t     uFreeGranularity = _countof(rgStackHandles);
 
    // if there are more handles than we can put on the stack then try to allocate a sorting buffer
    if (uCount > uFreeGranularity)
    {
        // try to allocate a bigger buffer to work in
        pLargeScratchBuffer = new (nothrow) OBJECTHANDLE[uCount];
 
        // did we get it?
        if (pLargeScratchBuffer)
        {
            // yes - use this buffer to prepare and free the handles
            pScratchBuffer   = pLargeScratchBuffer;
            uFreeGranularity = uCount;
        }
    }
 
    // loop freeing handles until we have freed them all
    while (uCount)
    {
        // decide how many we can process in this iteration
        if (uFreeGranularity > uCount)
            uFreeGranularity = uCount;
 
        // prepare and free these handles
        TableFreeBulkUnpreparedHandlesWorker(pTable, uType, pHandles, uFreeGranularity, pScratchBuffer);
 
        // adjust our pointers and move on
        uCount   -= uFreeGranularity;
        pHandles += uFreeGranularity;
    }
 
    // if we allocated a sorting buffer then free it now
    if (pLargeScratchBuffer)
        delete [] pLargeScratchBuffer;
}

#endif // !DACCESS_COMPILE

/*--------------------------------------------------------------------------*/


