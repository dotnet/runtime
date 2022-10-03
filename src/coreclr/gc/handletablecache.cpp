// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
 * Generational GC handle manager.  Handle Caching Routines.
 *
 * Implementation of handle table allocation cache.
 *

 *
 */

#include "common.h"

#include "gcenv.h"

#ifdef Sleep // TODO(segilles)
#undef Sleep
#endif // Sleep

#include "gc.h"

#include "handletablepriv.h"

/****************************************************************************
 *
 * RANDOM HELPERS
 *
 ****************************************************************************/

/*
 * SpinUntil
 *
 * Spins on a variable until its state matches a desired state.
 *
 * This routine will assert if it spins for a very long time.
 *
 */
void SpinUntil(void *pCond, BOOL fNonZero)
{
    WRAPPER_NO_CONTRACT;

    /*
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    */

    // if we have to sleep then we will keep track of a sleep period
    uint32_t dwThisSleepPeriod = 1;    // first just give up our timeslice
    uint32_t dwNextSleepPeriod = 10;   // next try a real delay

#ifdef _DEBUG
    uint32_t dwTotalSlept = 0;
    uint32_t dwNextComplain = 1000;
#endif //_DEBUG

    // on MP machines, allow ourselves some spin time before sleeping
    static uint32_t uNonSleepSpins = 8 * (GCToEEInterface::GetCurrentProcessCpuCount() - 1);

    // spin until the specified condition is met
    while ((*(uintptr_t *)pCond != 0) != (fNonZero != 0))
    {
        // have we exhausted the non-sleep spin count?
        if (!uNonSleepSpins)
        {
#ifdef _DEBUG
            // yes, missed again - before sleeping, check our current sleep time
            if (dwTotalSlept >= dwNextComplain)
            {
                //
                // THIS SHOULD NOT NORMALLY HAPPEN
                //
                // The only time this assert can be ignored is if you have
                // another thread intentionally suspended in a way that either
                // directly or indirectly leaves a thread suspended in the
                // handle table while the current thread (this assert) is
                // running normally.
                //
                // Otherwise, this assert should be investigated as a bug.
                //
                _ASSERTE(FALSE);

                // slow down the assert rate so people can investigate
                dwNextComplain = 3 * dwNextComplain;
            }

            // now update our total sleep time
            dwTotalSlept += dwThisSleepPeriod;
#endif //_DEBUG

            // sleep for a little while
            GCToOSInterface::Sleep(dwThisSleepPeriod);

            // now update our sleep period
            dwThisSleepPeriod = dwNextSleepPeriod;

            // now increase the next sleep period if it is still small
            if (dwNextSleepPeriod < 1000)
                dwNextSleepPeriod += 10;
        }
        else
        {
            // nope - just spin again
            YieldProcessor();           // indicate to the processor that we are spinning
            uNonSleepSpins--;
        }
    }
}


/*
 * ReadAndZeroCacheHandles
 *
 * Reads a set of handles from a bank in the handle cache, zeroing them as they are taken.
 *
 * This routine will assert if a requested handle is missing.
 *
 */
OBJECTHANDLE *ReadAndZeroCacheHandles(OBJECTHANDLE *pDst, OBJECTHANDLE *pSrc, uint32_t uCount)
{
    LIMITED_METHOD_CONTRACT;

    // set up to loop
    OBJECTHANDLE *pLast = pDst + uCount;

    // loop until we've copied all of them
    while (pDst < pLast)
    {
        // this version assumes we have handles to read
        _ASSERTE(*pSrc);

        // copy the handle and zero it from the source
        *pDst = *pSrc;
        *pSrc = 0;

        // set up for another handle
        pDst++;
        pSrc++;
    }

    // return the next unfilled slot after what we filled in
    return pLast;
}


/*
 * SyncReadAndZeroCacheHandles
 *
 * Reads a set of handles from a bank in the handle cache, zeroing them as they are taken.
 *
 * This routine will spin until all requested handles are obtained.
 *
 */
OBJECTHANDLE *SyncReadAndZeroCacheHandles(OBJECTHANDLE *pDst, OBJECTHANDLE *pSrc, uint32_t uCount)
{
    WRAPPER_NO_CONTRACT;

    /*
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    */

    // set up to loop
    // we loop backwards since that is the order handles are added to the bank
    // this is designed to reduce the chance that we will have to spin on a handle
    OBJECTHANDLE *pBase = pDst;
    pSrc += uCount;
    pDst += uCount;

    // remember the end of the array
    OBJECTHANDLE *pLast = pDst;

    // loop until we've copied all of them
    while (pDst > pBase)
    {
        // advance to the next slot
        pDst--;
        pSrc--;

        // this version spins if there is no handle to read
        if (!*pSrc)
            SpinUntil(pSrc, TRUE);

        // copy the handle and zero it from the source
        *pDst = *pSrc;
        *pSrc = 0;
    }

    // return the next unfilled slot after what we filled in
    return pLast;
}


/*
 * WriteCacheHandles
 *
 * Writes a set of handles to a bank in the handle cache.
 *
 * This routine will assert if it is about to clobber an existing handle.
 *
 */
void WriteCacheHandles(OBJECTHANDLE *pDst, OBJECTHANDLE *pSrc, uint32_t uCount)
{
    LIMITED_METHOD_CONTRACT;

    // set up to loop
    OBJECTHANDLE *pLimit = pSrc + uCount;

    // loop until we've copied all of them
    while (pSrc < pLimit)
    {
        // this version assumes we have space to store the handles
        _ASSERTE(!*pDst);

        // copy the handle
        *pDst = *pSrc;

        // set up for another handle
        pDst++;
        pSrc++;
    }
}


/*
 * SyncWriteCacheHandles
 *
 * Writes a set of handles to a bank in the handle cache.
 *
 * This routine will spin until lingering handles in the cache bank are gone.
 *
 */
void SyncWriteCacheHandles(OBJECTHANDLE *pDst, OBJECTHANDLE *pSrc, uint32_t uCount)
{
    WRAPPER_NO_CONTRACT;

    /*
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    */

    // set up to loop
    // we loop backwards since that is the order handles are removed from the bank
    // this is designed to reduce the chance that we will have to spin on a handle
    OBJECTHANDLE *pBase = pSrc;
    pSrc += uCount;
    pDst += uCount;

    // loop until we've copied all of them
    while (pSrc > pBase)
    {
        // set up for another handle
        pDst--;
        pSrc--;

        // this version spins if there is no handle to read
        if (*pDst)
            SpinUntil(pDst, FALSE);

        // copy the handle
        *pDst = *pSrc;
    }
}


/*
 * SyncTransferCacheHandles
 *
 * Transfers a set of handles from one bank of the handle cache to another,
 * zeroing the source bank as the handles are removed.
 *
 * The routine will spin until all requested handles can be transferred.
 *
 * This routine is equivalent to SyncReadAndZeroCacheHandles + SyncWriteCacheHandles
 *
 */
void SyncTransferCacheHandles(OBJECTHANDLE *pDst, OBJECTHANDLE *pSrc, uint32_t uCount)
{
    WRAPPER_NO_CONTRACT;

    /*
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    */

    // set up to loop
    // we loop backwards since that is the order handles are added to the bank
    // this is designed to reduce the chance that we will have to spin on a handle
    OBJECTHANDLE *pBase = pDst;
    pSrc += uCount;
    pDst += uCount;

    // loop until we've copied all of them
    while (pDst > pBase)
    {
        // advance to the next slot
        pDst--;
        pSrc--;

        // this version spins if there is no handle to read or no place to write it
        if (*pDst || !*pSrc)
        {
            SpinUntil(pSrc, TRUE);
            SpinUntil(pDst, FALSE);
        }

        // copy the handle and zero it from the source
        *pDst = *pSrc;
        *pSrc = 0;
    }
}

/*--------------------------------------------------------------------------*/



/****************************************************************************
 *
 * HANDLE CACHE
 *
 ****************************************************************************/

/*
 * TableFullRebalanceCache
 *
 * Rebalances a handle cache by transferring handles from the cache's
 * free bank to its reserve bank.  If the free bank does not provide
 * enough handles to replenish the reserve bank, handles are allocated
 * in bulk from the main handle table.  If too many handles remain in
 * the free bank, the extra handles are returned in bulk to the main
 * handle table.
 *
 * This routine attempts to reduce fragmentation in the main handle
 * table by sorting the handles according to table order, preferring to
 * refill the reserve bank with lower handles while freeing higher ones.
 * The sorting also allows the free routine to operate more efficiently,
 * as it can optimize the case where handles near each other are freed.
 *
 */
void TableFullRebalanceCache(HandleTable *pTable,
                             HandleTypeCache *pCache,
                             uint32_t uType,
                             int32_t lMinReserveIndex,
                             int32_t lMinFreeIndex,
                             OBJECTHANDLE *pExtraOutHandle,
                             OBJECTHANDLE extraInHandle)
{
    LIMITED_METHOD_CONTRACT;

    /*
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    */

    // we need a temporary space to sort our free handles in
    OBJECTHANDLE rgHandles[HANDLE_CACHE_TYPE_SIZE];

    // set up a base handle pointer to keep track of where we are
    OBJECTHANDLE *pHandleBase = rgHandles;

    // do we have a spare incoming handle?
    if (extraInHandle)
    {
        // remember the extra handle now
        *pHandleBase = extraInHandle;
        pHandleBase++;
    }

    // if there are handles in the reserve bank then gather them up
    // (we don't need to wait on these since they are only put there by this
    //  function inside our own lock)
    if (lMinReserveIndex > 0)
        pHandleBase = ReadAndZeroCacheHandles(pHandleBase, pCache->rgReserveBank, (uint32_t)lMinReserveIndex);
    else
        lMinReserveIndex = 0;

    // if there are handles in the free bank then gather them up
    if (lMinFreeIndex < HANDLES_PER_CACHE_BANK)
    {
        // this may have underflowed
        if (lMinFreeIndex < 0)
            lMinFreeIndex = 0;

        // here we need to wait for all pending freed handles to be written by other threads
        pHandleBase = SyncReadAndZeroCacheHandles(pHandleBase,
                                                  pCache->rgFreeBank + lMinFreeIndex,
                                                  HANDLES_PER_CACHE_BANK - (uint32_t)lMinFreeIndex);
    }

    // compute the number of handles we have
    uint32_t uHandleCount = (uint32_t) (pHandleBase - rgHandles);

    // do we have enough handles for a balanced cache?
    if (uHandleCount < REBALANCE_LOWATER_MARK)
    {
        // nope - allocate some more
        uint32_t uAlloc = HANDLES_PER_CACHE_BANK - uHandleCount;

        // if we have an extra outgoing handle then plan for that too
        if (pExtraOutHandle)
            uAlloc++;

        {
            // allocate the new handles - we intentionally don't check for success here
            FAULT_NOT_FATAL();

            uHandleCount += TableAllocBulkHandles(pTable, uType, pHandleBase, uAlloc);
        }
    }

    // reset the base handle pointer
    pHandleBase = rgHandles;

    // by default the whole free bank is available
    lMinFreeIndex = HANDLES_PER_CACHE_BANK;

    // if we have handles left over then we need to do some more work
    if (uHandleCount)
    {
        // do we have too many handles for a balanced cache?
        if (uHandleCount > REBALANCE_HIWATER_MARK)
        {
            //
            // sort the array by reverse handle order - this does two things:
            //  (1) combats handle fragmentation by preferring low-address handles to high ones
            //  (2) allows the free routine to run much more efficiently over the ones we free
            //
            QuickSort((uintptr_t *)pHandleBase, 0, uHandleCount - 1, CompareHandlesByFreeOrder);

            // yup, we need to free some - calculate how many
            uint32_t uFree = uHandleCount - HANDLES_PER_CACHE_BANK;

            // free the handles - they are already 'prepared' (eg zeroed and sorted)
            TableFreeBulkPreparedHandles(pTable, uType, pHandleBase, uFree);

            // update our array base and length
            uHandleCount -= uFree;
            pHandleBase += uFree;
        }

        // if we have an extra outgoing handle then fill it now
        if (pExtraOutHandle)
        {
            // account for the handle we're giving away
            uHandleCount--;

            // now give it away
            *pExtraOutHandle = pHandleBase[uHandleCount];
        }

        // if we have more than a reserve bank of handles then put some in the free bank
        if (uHandleCount > HANDLES_PER_CACHE_BANK)
        {
            // compute the number of extra handles we need to save away
            uint32_t uStore = uHandleCount - HANDLES_PER_CACHE_BANK;

            // compute the index to start writing the handles to
            lMinFreeIndex = HANDLES_PER_CACHE_BANK - uStore;

            // store the handles
            // (we don't need to wait on these since we already waited while reading them)
            WriteCacheHandles(pCache->rgFreeBank + lMinFreeIndex, pHandleBase, uStore);

            // update our array base and length
            uHandleCount -= uStore;
            pHandleBase += uStore;
        }
    }

    // update the write index for the free bank
    // NOTE: we use an interlocked exchange here to guarantee relative store order on MP
    // AFTER THIS POINT THE FREE BANK IS LIVE AND COULD RECEIVE NEW HANDLES
    Interlocked::Exchange(&pCache->lFreeIndex, lMinFreeIndex);

    // now if we have any handles left, store them in the reserve bank
    if (uHandleCount)
    {
        // store the handles
        // (here we need to wait for all pending allocated handles to be taken
        //  before we set up new ones in their places)
        SyncWriteCacheHandles(pCache->rgReserveBank, pHandleBase, uHandleCount);
    }

    // compute the index to start serving handles from
    lMinReserveIndex = (int32_t)uHandleCount;

    // update the read index for the reserve bank
    // NOTE: we use an interlocked exchange here to guarantee relative store order on MP
    // AT THIS POINT THE RESERVE BANK IS LIVE AND HANDLES COULD BE ALLOCATED FROM IT
    Interlocked::Exchange(&pCache->lReserveIndex, lMinReserveIndex);
}


/*
 * TableQuickRebalanceCache
 *
 * Rebalances a handle cache by transferring handles from the cache's free bank
 * to its reserve bank.  If the free bank does not provide enough handles to
 * replenish the reserve bank or too many handles remain in the free bank, the
 * routine just punts and calls TableFullRebalanceCache.
 *
 */
void TableQuickRebalanceCache(HandleTable *pTable,
                              HandleTypeCache *pCache,
                              uint32_t uType,
                              int32_t lMinReserveIndex,
                              int32_t lMinFreeIndex,
                              OBJECTHANDLE *pExtraOutHandle,
                              OBJECTHANDLE extraInHandle)
{
    WRAPPER_NO_CONTRACT;

    /*
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    */

    // clamp the min free index to be non-negative
    if (lMinFreeIndex < 0)
        lMinFreeIndex = 0;

    // clamp the min reserve index to be non-negative
    if (lMinReserveIndex < 0)
        lMinReserveIndex = 0;

    // compute the number of slots in the free bank taken by handles
    uint32_t uFreeAvail = HANDLES_PER_CACHE_BANK - (uint32_t)lMinFreeIndex;

    // compute the number of handles we have to fiddle with
    uint32_t uHandleCount = (uint32_t)lMinReserveIndex + uFreeAvail + (extraInHandle != 0);

    // can we rebalance these handles in place?
    if ((uHandleCount < REBALANCE_LOWATER_MARK) ||
        (uHandleCount > REBALANCE_HIWATER_MARK))
    {
        // nope - perform a full rebalance of the handle cache
        TableFullRebalanceCache(pTable, pCache, uType, lMinReserveIndex, lMinFreeIndex,
                                pExtraOutHandle, extraInHandle);

        // all done
        return;
    }

    // compute the number of empty slots in the reserve bank
    uint32_t uEmptyReserve = HANDLES_PER_CACHE_BANK - lMinReserveIndex;

    // we want to transfer as many handles as we can from the free bank
    uint32_t uTransfer = uFreeAvail;

    // but only as many as we have room to store in the reserve bank
    if (uTransfer > uEmptyReserve)
        uTransfer = uEmptyReserve;

    // transfer the handles
    SyncTransferCacheHandles(pCache->rgReserveBank + lMinReserveIndex,
                             pCache->rgFreeBank    + lMinFreeIndex,
                             uTransfer);

    // adjust the free and reserve indices to reflect the transfer
    lMinFreeIndex    += uTransfer;
    lMinReserveIndex += uTransfer;

    // do we have an extra incoming handle to store?
    if (extraInHandle)
    {
        //
        // Workaround: For code size reasons, we don't handle all cases here.
        // We assume an extra IN handle means a cache overflow during a free.
        //
        // After the rebalance above, the reserve bank should be full, and
        // there may be a few handles sitting in the free bank.  The HIWATER
        // check above guarantees that we have room to store the handle.
        //
        _ASSERTE(!pExtraOutHandle);

        // store the handle in the next available free bank slot
        pCache->rgFreeBank[--lMinFreeIndex] = extraInHandle;
    }
    else if (pExtraOutHandle)   // do we have an extra outgoing handle to satisfy?
    {
        //
        // For code size reasons, we don't handle all cases here.
        // We assume an extra OUT handle means a cache underflow during an alloc.
        //
        // After the rebalance above, the free bank should be empty, and
        // the reserve bank may not be fully populated.  The LOWATER check above
        // guarantees that the reserve bank has at least one handle we can steal.
        //

        // take the handle from the reserve bank and update the reserve index
        *pExtraOutHandle = pCache->rgReserveBank[--lMinReserveIndex];

        // zero the cache slot we chose
        pCache->rgReserveBank[lMinReserveIndex] = NULL;
    }

    // update the write index for the free bank
    // NOTE: we use an interlocked exchange here to guarantee relative store order on MP
    // AFTER THIS POINT THE FREE BANK IS LIVE AND COULD RECEIVE NEW HANDLES
    Interlocked::Exchange(&pCache->lFreeIndex, lMinFreeIndex);

    // update the read index for the reserve bank
    // NOTE: we use an interlocked exchange here to guarantee relative store order on MP
    // AT THIS POINT THE RESERVE BANK IS LIVE AND HANDLES COULD BE ALLOCATED FROM IT
    Interlocked::Exchange(&pCache->lReserveIndex, lMinReserveIndex);
}


/*
 * TableCacheMissOnAlloc
 *
 * Gets a single handle of the specified type from the handle table,
 * making the assumption that the reserve cache for that type was
 * recently emptied.  This routine acquires the handle manager lock and
 * attempts to get a handle from the reserve cache again.  If this second
 * get operation also fails, the handle is allocated by means of a cache
 * rebalance.
 *
 */
OBJECTHANDLE TableCacheMissOnAlloc(HandleTable *pTable, HandleTypeCache *pCache, uint32_t uType)
{
    WRAPPER_NO_CONTRACT;

    // assume we get no handle
    OBJECTHANDLE handle = NULL;

    // acquire the handle manager lock
    CrstHolder ch(&pTable->Lock);

    // try again to take a handle (somebody else may have rebalanced)
    int32_t lReserveIndex = Interlocked::Decrement(&pCache->lReserveIndex);

    // are we still waiting for handles?
    if (lReserveIndex < 0)
    {
        // yup, suspend free list usage...
        int32_t lFreeIndex = Interlocked::Exchange(&pCache->lFreeIndex, 0);

        // ...and rebalance the cache...
        TableQuickRebalanceCache(pTable, pCache, uType, lReserveIndex, lFreeIndex, &handle, NULL);
    }
    else
    {
        // somebody else rebalanced the cache for us - take the handle
        handle = pCache->rgReserveBank[lReserveIndex];

        // zero the handle slot
        pCache->rgReserveBank[lReserveIndex] = 0;
    }

    // return the handle we got
    return handle;
}


/*
 * TableCacheMissOnFree
 *
 * Returns a single handle of the specified type to the handle table,
 * making the assumption that the free cache for that type was recently
 * filled.  This routine acquires the handle manager lock and attempts
 * to store the handle in the free cache again.  If this second store
 * operation also fails, the handle is freed by means of a cache
 * rebalance.
 *
 */
void TableCacheMissOnFree(HandleTable *pTable, HandleTypeCache *pCache, uint32_t uType, OBJECTHANDLE handle)
{
    WRAPPER_NO_CONTRACT;

    /*
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    */

    // acquire the handle manager lock
    CrstHolder ch(&pTable->Lock);

    // try again to take a slot (somebody else may have rebalanced)
    int32_t lFreeIndex = Interlocked::Decrement(&pCache->lFreeIndex);

    // are we still waiting for free slots?
    if (lFreeIndex < 0)
    {
        // yup, suspend reserve list usage...
        int32_t lReserveIndex = Interlocked::Exchange(&pCache->lReserveIndex, 0);

        // ...and rebalance the cache...
        TableQuickRebalanceCache(pTable, pCache, uType, lReserveIndex, lFreeIndex, NULL, handle);
    }
    else
    {
        // somebody else rebalanced the cache for us - free the handle
        pCache->rgFreeBank[lFreeIndex] = handle;
    }
}


/*
 * TableAllocSingleHandleFromCache
 *
 * Gets a single handle of the specified type from the handle table by
 * trying to fetch it from the reserve cache for that handle type.  If the
 * reserve cache is empty, this routine calls TableCacheMissOnAlloc.
 *
 */
OBJECTHANDLE TableAllocSingleHandleFromCache(HandleTable *pTable, uint32_t uType)
{
    WRAPPER_NO_CONTRACT;

    // we use this in two places
    OBJECTHANDLE handle;

    // first try to get a handle from the quick cache
    if (pTable->rgQuickCache[uType])
    {
        // try to grab the handle we saw
        handle = Interlocked::ExchangePointer(pTable->rgQuickCache + uType, (OBJECTHANDLE)NULL);

        // if it worked then we're done
        if (handle)
            return handle;
    }

    // ok, get the main handle cache for this type
    HandleTypeCache *pCache = pTable->rgMainCache + uType;

    // try to take a handle from the main cache
    int32_t lReserveIndex = Interlocked::Decrement(&pCache->lReserveIndex);

    // did we underflow?
    if (lReserveIndex < 0)
    {
        // yep - the cache is out of handles
        return TableCacheMissOnAlloc(pTable, pCache, uType);
    }

    // get our handle
    handle = pCache->rgReserveBank[lReserveIndex];

    // zero the handle slot
    pCache->rgReserveBank[lReserveIndex] = 0;

    // sanity
    _ASSERTE(handle);

    // return our handle
    return handle;
}


/*
 * TableFreeSingleHandleToCache
 *
 * Returns a single handle of the specified type to the handle table
 * by trying to store it in the free cache for that handle type.  If the
 * free cache is full, this routine calls TableCacheMissOnFree.
 *
 */
void TableFreeSingleHandleToCache(HandleTable *pTable, uint32_t uType, OBJECTHANDLE handle)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;         // because of TableCacheMissOnFree
    }
    CONTRACTL_END;

#ifdef DEBUG_DestroyedHandleValue
    *(_UNCHECKED_OBJECTREF *)handle = DEBUG_DestroyedHandleValue;
#else
    // zero the handle's object pointer
    *(_UNCHECKED_OBJECTREF *)handle = NULL;
#endif

    // if this handle type has user data then clear it - AFTER the referent is cleared!
    if (TypeHasUserData(pTable, uType))
        HandleQuickSetUserData(handle, 0L);

    // is there room in the quick cache?
    if (!pTable->rgQuickCache[uType])
    {
        // yup - try to stuff our handle in the slot we saw
        handle = Interlocked::ExchangePointer(&pTable->rgQuickCache[uType], handle);

        // if we didn't end up with another handle then we're done
        if (!handle)
            return;
    }

    // ok, get the main handle cache for this type
    HandleTypeCache *pCache = pTable->rgMainCache + uType;

    // try to take a free slot from the main cache
    int32_t lFreeIndex = Interlocked::Decrement(&pCache->lFreeIndex);

    // did we underflow?
    if (lFreeIndex < 0)
    {
        // yep - we're out of free slots
        TableCacheMissOnFree(pTable, pCache, uType, handle);
        return;
    }

    // we got a slot - save the handle in the free bank
    pCache->rgFreeBank[lFreeIndex] = handle;
}


/*
 * TableAllocHandlesFromCache
 *
 * Allocates multiple handles of the specified type by repeatedly
 * calling TableAllocSingleHandleFromCache.
 *
 */
uint32_t TableAllocHandlesFromCache(HandleTable *pTable, uint32_t uType, OBJECTHANDLE *pHandleBase, uint32_t uCount)
{
    WRAPPER_NO_CONTRACT;

    // loop until we have satisfied all the handles we need to allocate
    uint32_t uSatisfied = 0;
    while (uSatisfied < uCount)
    {
        // get a handle from the cache
        OBJECTHANDLE handle = TableAllocSingleHandleFromCache(pTable, uType);

        // if we can't get any more then bail out
        if (!handle)
            break;

        // store the handle in the caller's array
        *pHandleBase = handle;

        // on to the next one
        uSatisfied++;
        pHandleBase++;
    }

    // return the number of handles we allocated
    return uSatisfied;
}


/*
 * TableFreeHandlesToCache
 *
 * Frees multiple handles of the specified type by repeatedly
 * calling TableFreeSingleHandleToCache.
 *
 */
void TableFreeHandlesToCache(HandleTable *pTable, uint32_t uType, const OBJECTHANDLE *pHandleBase, uint32_t uCount)
{
    WRAPPER_NO_CONTRACT;

    // loop until we have freed all the handles
    while (uCount)
    {
        // get the next handle to free
        OBJECTHANDLE handle = *pHandleBase;

        // advance our state
        uCount--;
        pHandleBase++;

        // sanity
        _ASSERTE(handle);

        // return the handle to the cache
        TableFreeSingleHandleToCache(pTable, uType, handle);
    }
}

/*--------------------------------------------------------------------------*/


